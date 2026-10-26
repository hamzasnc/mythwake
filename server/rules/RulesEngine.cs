using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mythwake.Content;
using Mythwake.Core.Combat;
using Mythwake.Core.Meta;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mythwake.Rules
{
    /// <summary>Request handlers of the rules service. Pure functions of their input plus the loaded content.</summary>
    public sealed class RulesEngine
    {
        public GameContent Content { get; }
        public string ContentVersion { get; }

        private RulesEngine(GameContent content, string version)
        {
            Content = content;
            ContentVersion = version;
        }

        public static RulesEngine Load(string contentDir)
        {
            var data = Path.Combine(contentDir, "Data");
            var heroFiles = Directory.GetFiles(Path.Combine(contentDir, "Heroes"), "*.hero.json", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal).ToArray();
            var monsterDir = Path.Combine(contentDir, "Monsters");
            var monsterFiles = Directory.Exists(monsterDir)
                ? Directory.GetFiles(monsterDir, "*.monster.json", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal).ToArray()
                : new string[0];
            var content = GameContentLoader.Load(name => File.ReadAllText(Path.Combine(data, name + ".json")),
                heroFiles.Select(File.ReadAllText), monsterFiles.Select(File.ReadAllText));
            return new RulesEngine(content, ContentVersionOf(contentDir));
        }

        /// <summary>Hash over every rules-relevant content file (data + hero definitions), independent of the OS path separator.</summary>
        public static string ContentVersionOf(string contentDir)
        {
            var files = Directory.GetFiles(Path.Combine(contentDir, "Data"), "*.json")
                .Concat(Directory.GetFiles(Path.Combine(contentDir, "Heroes"), "*.hero.json", SearchOption.AllDirectories))
                .Concat(Directory.Exists(Path.Combine(contentDir, "Monsters"))
                    ? Directory.GetFiles(Path.Combine(contentDir, "Monsters"), "*.monster.json", SearchOption.AllDirectories)
                    : new string[0])
                .Select(p => new System.Collections.Generic.KeyValuePair<string, string>(
                    Path.GetRelativePath(contentDir, p).Replace('\\', '/'), File.ReadAllText(p)));
            return Mythwake.Content.ContentVersion.Compute(files);
        }

        /// <summary>64-bit seeds travel as strings: JSON numbers above 2^63 do not survive every parser.</summary>
        private static ulong Seed(JToken token) =>
            token == null ? 0UL : ulong.Parse(token.ToString(), System.Globalization.CultureInfo.InvariantCulture);

        private JObject Ok(PlayerState state)
        {
            var json = SaveJson.Write(state);
            return new JObject { ["ok"] = true, ["state"] = json, ["stateHash"] = SaveJson.HashOf(json) };
        }

        private static JObject Fail(string error, int index = -1) =>
            new JObject { ["ok"] = false, ["error"] = error, ["index"] = index };

        /// <summary>{ seed, nowMs } → new player.</summary>
        public JObject Create(JObject body)
        {
            var ctx = new CommandContext { Content = Content, NowMs = body.Value<long>("nowMs") };
            new CreatePlayer { Seed = Seed(body["seed"]) }.Apply(ctx);
            return Ok(ctx.State);
        }

        /// <summary>
        /// { state, nowMs, trusted, commands: [{ type, data }] } → new state. All or nothing: the first rule violation
        /// returns its code and index and no state.
        /// </summary>
        public JObject Apply(JObject body)
        {
            PlayerState state;
            try
            {
                state = SaveJson.Read(body.Value<string>("state"));
            }
            catch (Exception e)
            {
                return Fail("bad_state: " + e.Message);
            }
            var trusted = body.Value<bool?>("trusted") ?? false;
            var ctx = new CommandContext { State = state, Content = Content, NowMs = body.Value<long>("nowMs") };
            var commands = body["commands"] as JArray ?? new JArray();
            for (var i = 0; i < commands.Count; i++)
            {
                var c = (JObject)commands[i];
                object command;
                try
                {
                    command = CommandCodec.Decode(c.Value<string>("type"), c["data"]?.ToString(Formatting.None));
                }
                catch (Exception e)
                {
                    return Fail("bad_command: " + e.Message, i);
                }
                if (!trusted && command.GetType().GetCustomAttribute<ServerOnlyAttribute>() != null)
                    return Fail("forbidden", i);
                if (command is CreatePlayer)
                    return Fail("forbidden", i);
                ctx.NowMs = c.Value<long?>("atMs") ?? body.Value<long>("nowMs");
                try
                {
                    CommandCodec.Run(command, ctx);
                }
                catch (RuleViolation v)
                {
                    return Fail(v.Code, i);
                }
            }
            return Ok(ctx.State);
        }

        /// <summary>{ state } → the player's current arena team snapshot.</summary>
        public JObject ArenaTeam(JObject body)
        {
            var state = SaveJson.Read(body.Value<string>("state"));
            var team = Arena.TeamOf(state, Content);
            return new JObject
            {
                ["ok"] = team.Units.Count > 0,
                ["error"] = team.Units.Count > 0 ? null : "formation_empty",
                ["team"] = JsonConvert.SerializeObject(team, SaveJson.Settings),
                ["power"] = team.Power
            };
        }

        /// <summary>{ attacker, defender (team snapshots as JSON), seed } → outcome and the setup the client replays.</summary>
        public JObject ArenaBattle(JObject body)
        {
            var attacker = JsonConvert.DeserializeObject<TeamSnapshot>(body.Value<string>("attacker"), SaveJson.Settings);
            var defender = JsonConvert.DeserializeObject<TeamSnapshot>(body.Value<string>("defender"), SaveJson.Settings);
            var seed = Seed(body["seed"]);
            var result = BattleSimulator.Run(Arena.Setup(Content, attacker, defender, seed));
            var victory = result.Outcome == BattleOutcome.Victory;
            return new JObject
            {
                ["ok"] = true,
                ["victory"] = victory,
                ["durationMs"] = result.DurationMs,
                ["ratingChange"] = Arena.RatingChange(Content, body.Value<int>("attackerRating"), body.Value<int>("defenderRating"), victory)
            };
        }

        /// <summary>Arena settings the API needs (limits, ranks, rewards) plus the daily reset hour.</summary>
        public JObject ArenaConfig()
        {
            var config = JObject.Parse(JsonConvert.SerializeObject(Content.Arena, SaveJson.Settings));
            config["ok"] = true;
            config["resetHourUtc"] = Content.Quests.ResetHourUtc;
            return config;
        }

        /// <summary>{ stage, seed } → a bot defense team as strong as the campaign enemies of that stage.</summary>
        public JObject ArenaBot(JObject body)
        {
            var stage = Math.Max(1, body.Value<int>("stage"));
            var team = TeamSnapshot.Of(Campaign.Enemies(Content, stage));
            foreach (var u in team.Units)
                u.Stars = 1;
            return new JObject
            {
                ["ok"] = true,
                ["team"] = JsonConvert.SerializeObject(team, SaveJson.Settings),
                ["power"] = team.Power
            };
        }
    }
}
