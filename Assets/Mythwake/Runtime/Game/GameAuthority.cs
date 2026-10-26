using System;
using Mythwake.Content;
using Mythwake.Core.Meta;

namespace Mythwake.Game
{
    public readonly struct CommandOutcome<T>
    {
        public readonly bool Ok;
        public readonly T Value;
        /// <summary>Rule violation code (see <see cref="RuleViolation.Code"/>) when not ok.</summary>
        public readonly string Error;

        private CommandOutcome(bool ok, T value, string error)
        {
            Ok = ok;
            Value = value;
            Error = error;
        }

        public static CommandOutcome<T> Success(T value) => new CommandOutcome<T>(true, value, null);
        public static CommandOutcome<T> Failure(string error) => new CommandOutcome<T>(false, default, error);
    }

    /// <summary>The only way the client changes player state. Local today; a remote implementation sends the same commands to a server.</summary>
    public interface IGameAuthority
    {
        PlayerState State { get; }
        GameContent Content { get; }
        long NowMs { get; }
        event Action StateChanged;
        CommandOutcome<T> Execute<T>(ICommand<T> command);
    }

    public interface IClock
    {
        long NowMs { get; }
    }

    public sealed class SystemClock : IClock
    {
        public long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>Runs commands in-process and saves after every successful change.</summary>
    public sealed class LocalAuthority : IGameAuthority
    {
        private readonly IClock _clock;
        private readonly SaveStore _save;

        public PlayerState State { get; private set; }
        public GameContent Content { get; }
        public long NowMs => _clock.NowMs;
        public event Action StateChanged;

        public LocalAuthority(GameContent content, IClock clock, SaveStore save)
        {
            Content = content;
            _clock = clock;
            _save = save;
            State = save.Load();
            if (State == null)
            {
                var seed = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() ^ (ulong)Guid.NewGuid().GetHashCode();
                Execute(new CreatePlayer { Seed = seed });
            }
        }

        public CommandOutcome<T> Execute<T>(ICommand<T> command)
        {
            // snapshot so that even an unexpected exception halfway through a command cannot leave a broken state behind
            var snapshot = State == null ? null : SaveJson.Write(State);
            var ctx = new CommandContext { State = State, Content = Content, NowMs = _clock.NowMs };
            try
            {
                var result = command.Apply(ctx);
                State = ctx.State;
                _save.Save(State);
                StateChanged?.Invoke();
                return CommandOutcome<T>.Success(result);
            }
            catch (RuleViolation v)
            {
                State = snapshot == null ? null : SaveJson.Read(snapshot);
                return CommandOutcome<T>.Failure(v.Code);
            }
            catch
            {
                State = snapshot == null ? null : SaveJson.Read(snapshot);
                throw;
            }
        }
    }
}
