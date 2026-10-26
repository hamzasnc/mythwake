using System;
using System.Collections;
using System.Collections.Generic;
using Mythwake.Content;
using Mythwake.Core.Meta;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mythwake.Game
{
    /// <summary>
    /// Server-backed authority with client prediction (docs/SERVER.md): every command runs locally first with the same
    /// rules and the server clock, so the UI never waits; the commands then go to the server in order. When the
    /// server disagrees (conflict, rule error, different state hash) the server state replaces the prediction.
    /// </summary>
    public sealed class ServerAuthority : IGameAuthority
    {
        private sealed class Outgoing
        {
            public string Id;
            public string Type;
            public JToken Data;
            public long AtMs;
        }

        private readonly ServerApi _api;
        private readonly List<Outgoing> _outbox = new List<Outgoing>();
        private List<Outgoing> _inFlight;
        private long _confirmedVersion;
        private long _clockOffsetMs;
        private float _retryAt;
        private bool _resyncing;
        private bool _checking;

        public PlayerState State { get; private set; }
        public GameContent Content { get; }
        public long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _clockOffsetMs;
        public ServerApi Api => _api;
        public string DisplayName { get; private set; }
        /// <summary>Commands waiting for the server (0 = in sync).</summary>
        public int Pending => _outbox.Count + (_inFlight?.Count ?? 0) + (_resyncing || _checking ? 1 : 0);
        public event Action StateChanged;
        /// <summary>Raised when the server state replaced the prediction (the UI shows a short notice).</summary>
        public event Action<string> Resynced;

        private ServerAuthority(ServerApi api, GameContent content)
        {
            _api = api;
            Content = content;
        }

        /// <summary>Logs in and loads the player. Calls done with null and an error when the server is not reachable.</summary>
        public static IEnumerator Connect(string url, GameContent content, string contentVersion, Action<ServerAuthority, string> done)
        {
            var authority = new ServerAuthority(new ServerApi(url), content);
            ApiResponse ready = null;
            yield return authority._api.Send("GET", "/readyz", null, r => ready = r, 5);
            if (!ready.Ok)
            {
                done(null, ready.Error);
                yield break;
            }
            var serverVersion = ready.Body.Value<string>("contentVersion");
            if (serverVersion != contentVersion)
            {
                // different rules would make every prediction wrong: refuse instead of fighting the server
                done(null, $"content_mismatch (client {contentVersion}, server {serverVersion})");
                yield break;
            }
            ApiResponse login = null;
            yield return authority._api.Login(r => login = r);
            if (!login.Ok)
            {
                done(null, login.Error);
                yield break;
            }
            string error = null;
            yield return authority.Download(e => error = e);
            done(error == null ? authority : null, error);
        }

        private IEnumerator Download(Action<string> done)
        {
            var sentAt = Time.realtimeSinceStartup;
            ApiResponse r = null;
            yield return _api.Send("GET", "/v1/player", null, x => r = x);
            if (!r.Ok)
            {
                done(r.Error);
                yield break;
            }
            // half the round trip approximates when the server read its clock
            var latencyMs = (long)((Time.realtimeSinceStartup - sentAt) * 500f);
            _clockOffsetMs = r.Body.Value<long>("nowMs") + latencyMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            State = SaveJson.Read(r.Body["state"].ToString(Newtonsoft.Json.Formatting.None));
            _confirmedVersion = r.Body.Value<long>("version");
            DisplayName = r.Body.Value<string>("displayName");
            done(null);
        }

        public CommandOutcome<T> Execute<T>(ICommand<T> command)
        {
            var snapshot = SaveJson.Write(State);
            var ctx = new CommandContext { State = State, Content = Content, NowMs = NowMs };
            try
            {
                var result = command.Apply(ctx);
                State = ctx.State;
                _outbox.Add(new Outgoing
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = CommandCodec.NameOf(command),
                    Data = JToken.Parse(CommandCodec.Encode(command)),
                    AtMs = ctx.NowMs
                });
                StateChanged?.Invoke();
                return CommandOutcome<T>.Success(result);
            }
            catch (RuleViolation v)
            {
                State = SaveJson.Read(snapshot);
                return CommandOutcome<T>.Failure(v.Code);
            }
            catch
            {
                State = SaveJson.Read(snapshot);
                throw;
            }
        }

        /// <summary>Sends queued commands; call every frame (AppRoot runs it as a coroutine).</summary>
        public IEnumerator Pump()
        {
            while (true)
            {
                if (_inFlight == null && _outbox.Count > 0 && !_resyncing && Time.realtimeSinceStartup >= _retryAt)
                    yield return SendBatch();
                else
                    yield return null;
            }
        }

        private IEnumerator SendBatch()
        {
            _inFlight = _outbox.GetRange(0, Math.Min(50, _outbox.Count));
            _outbox.RemoveRange(0, _inFlight.Count);
            var commands = new JArray();
            foreach (var c in _inFlight)
                commands.Add(new JObject { ["id"] = c.Id, ["type"] = c.Type, ["data"] = c.Data, ["atMs"] = c.AtMs });
            ApiResponse r = null;
            yield return _api.Send("POST", "/v1/commands", new JObject { ["baseVersion"] = _confirmedVersion, ["commands"] = commands }, x => r = x);

            if (r.NetworkError != null && r.Body == null)
            {
                // offline or timeout: keep the batch (same ids, so a retry can never apply twice) and back off
                _outbox.InsertRange(0, _inFlight);
                _inFlight = null;
                _retryAt = Time.realtimeSinceStartup + 3f;
                yield break;
            }
            // keep counting as pending until the hash check below is done, so Flush() waits for it
            _checking = true;
            _inFlight = null;
            if (r.Ok)
            {
                _confirmedVersion = r.Body.Value<long>("version");
                if (_outbox.Count == 0 && r.Body.Value<string>("stateHash") != SaveJson.Hash(State))
                {
                    Debug.LogWarning("[Server] prediction differs from the server, resyncing");
                    yield return Resync("hash");
                }
                _checking = false;
                yield break;
            }
            _checking = false;
            Debug.LogWarning($"[Server] commands rejected: {r.Status} {r.Error}");
            yield return Resync(r.Error);
        }

        /// <summary>Drops the prediction and loads the server's state.</summary>
        public IEnumerator Resync(string reason)
        {
            _resyncing = true;
            _outbox.Clear();
            string error = null;
            yield return Download(e => error = e);
            _resyncing = false;
            if (error != null)
            {
                _retryAt = Time.realtimeSinceStartup + 3f;
                yield break;
            }
            StateChanged?.Invoke();
            Resynced?.Invoke(reason);
        }

        /// <summary>Waits until every queued command is confirmed (before server-side actions like arena fights).</summary>
        public IEnumerator Flush(float timeoutSeconds = 10f)
        {
            var until = Time.realtimeSinceStartup + timeoutSeconds;
            while (Pending > 0 && Time.realtimeSinceStartup < until)
                yield return null;
        }
    }
}
