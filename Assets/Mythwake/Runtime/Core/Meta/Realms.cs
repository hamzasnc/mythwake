using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;

namespace Mythwake.Core.Meta
{
    /// <summary>Shattered Realms: shared daily entries, keys, realms built like dungeons (same definition shape).</summary>
    public sealed class RealmConfig
    {
        /// <summary>Portal Chamber level that opens the realms.</summary>
        public int PortalLevel = 5;
        /// <summary>Doc: "opens twice a day" – free entries for all realms together.</summary>
        public int FreeEntriesPerDay = 2;
        public List<DungeonDefinition> Realms = new List<DungeonDefinition>();
    }

    public sealed class RealmState
    {
        public Dictionary<string, int> Highest = new Dictionary<string, int>();
        public long Day = -1;
        /// <summary>Free entries used today (keys are spent from the currency).</summary>
        public int FreeUsed;
    }

    public sealed class RealmResult
    {
        public BattleOutcome Outcome;
        public List<Amount> Rewards = new List<Amount>();
        public bool FirstClear;
        public bool UsedKey;
    }

    /// <summary>docs/systems/SHATTERED_REALMS.md</summary>
    public static class Realms
    {
        public static DungeonDefinition Get(GameContent content, string id)
        {
            foreach (var r in content.Realms.Realms)
                if (r.Id == id)
                    return r;
            throw new RuleViolation("unknown_realm", id);
        }

        public static bool Open(PlayerState state, GameContent content) =>
            content.Realms.Realms.Count > 0 && Dungeons.PortalLevel(state, content) >= content.Realms.PortalLevel;

        public static int Highest(PlayerState state, string id) => state.Realms.Highest.TryGetValue(id, out var h) ? h : 0;

        public static int FreeLeft(PlayerState state, GameContent content, long nowMs)
        {
            var used = state.Realms.Day == Quests.DayIndex(content, nowMs) ? state.Realms.FreeUsed : 0;
            return System.Math.Max(0, content.Realms.FreeEntriesPerDay - used);
        }

        /// <summary>Entries available right now: free ones plus keys.</summary>
        public static long EntriesLeft(PlayerState state, GameContent content, long nowMs) =>
            FreeLeft(state, content, nowMs) + state.Get(Ids.RealmKey);

        public static BattleSetup BuildBattle(PlayerState state, GameContent content, DungeonDefinition realm, PendingBattle pending)
        {
            var rules = realm.Rules ?? new BattleRules();
            var copy = new BattleRules
            {
                HealingModifier = rules.HealingModifier, EnemyDamageModifier = rules.EnemyDamageModifier,
                EnergyGainModifier = rules.EnergyGainModifier, EnemyRegenPermille = rules.EnemyRegenPermille,
                MaxDurationMs = rules.MaxDurationMs
            };
            foreach (var p in rules.EnemyPassives)
                copy.EnemyPassives.Add(p.Clone());
            return new BattleSetup
            {
                Seed = pending.Seed,
                Allies = Campaign.Allies(state, content, pending.Formation),
                Leader = Campaign.LeaderUnit(state, content),
                Enemies = Dungeons.Enemies(content, realm, pending.Floor),
                Rules = copy
            };
        }

        /// <summary>Uses a free entry if one is left today, else a key. Returns true when a key was spent.</summary>
        internal static bool UseEntry(CommandContext ctx)
        {
            var s = ctx.State.Realms;
            var day = Quests.DayIndex(ctx.Content, ctx.NowMs);
            if (s.Day != day)
            {
                s.Day = day;
                s.FreeUsed = 0;
            }
            if (s.FreeUsed < ctx.Content.Realms.FreeEntriesPerDay)
            {
                s.FreeUsed++;
                return false;
            }
            ctx.Require(ctx.State.Get(Ids.RealmKey) > 0, "realm_no_entries");
            ctx.State.Add(Ids.RealmKey, -1);
            return true;
        }

        internal static void Validate(CommandContext ctx)
        {
            ctx.Require(Open(ctx.State, ctx.Content), "realm_locked");
            ctx.Require(EntriesLeft(ctx.State, ctx.Content, ctx.NowMs) > 0, "realm_no_entries");
        }
    }

    // ------------------------------------------------------------------ commands

    public sealed class BeginRealm : ICommand<BattleSetup>
    {
        public string RealmId;
        public int Level;

        public BattleSetup Apply(CommandContext ctx)
        {
            var realm = Realms.Get(ctx.Content, RealmId);
            Realms.Validate(ctx);
            ctx.Require(Level >= 1 && Level <= realm.MaxLevel && Level <= Realms.Highest(ctx.State, realm.Id) + 1, "realm_level_locked");
            ctx.Require(Formations.Count(ctx.State.Formation) > 0, "formation_empty");
            var seed = ((ulong)ctx.NextRng().NextUInt() << 32) | ctx.NextRng().NextUInt();
            ctx.State.Battle = new PendingBattle
            {
                Stage = ctx.State.Campaign.HighestCleared, RealmId = realm.Id, Floor = Level, Seed = seed,
                Formation = (string[])ctx.State.Formation.Clone()
            };
            return Realms.BuildBattle(ctx.State, ctx.Content, realm, ctx.State.Battle);
        }
    }

    public sealed class FinishRealm : ICommand<RealmResult>
    {
        public bool AutoUltimates = true;
        public List<int> AutoToggleTimesMs = new List<int>();
        public List<UltimateRequest> UltimateRequests = new List<UltimateRequest>();

        public RealmResult Apply(CommandContext ctx)
        {
            var state = ctx.State;
            ctx.Require(state.Battle != null && state.Battle.RealmId != null, "no_battle");
            var pending = state.Battle;
            var realm = Realms.Get(ctx.Content, pending.RealmId);
            var setup = Realms.BuildBattle(state, ctx.Content, realm, pending);
            setup.AutoUltimates = AutoUltimates;
            setup.AutoToggleTimesMs.AddRange(AutoToggleTimesMs);
            setup.UltimateRequests.AddRange(UltimateRequests);
            var battle = BattleSimulator.Run(setup);
            state.Battle = null;

            var result = new RealmResult { Outcome = battle.Outcome };
            if (battle.Outcome != BattleOutcome.Victory)
                return result;
            result.UsedKey = Realms.UseEntry(ctx);
            result.Rewards.AddRange(Dungeons.Rewards(realm.ClearRewards, pending.Floor));
            if (pending.Floor > Realms.Highest(state, realm.Id))
            {
                result.FirstClear = true;
                state.Realms.Highest[realm.Id] = pending.Floor;
                result.Rewards.AddRange(Dungeons.Rewards(realm.FirstClearRewards, pending.Floor));
            }
            ctx.Grant(result.Rewards);
            Quests.Track(ctx, Stat.BattleWon);
            Quests.Track(ctx, Stat.RealmCleared);
            return result;
        }
    }

    /// <summary>Clears the highest cleared level of a realm again without a fight (one entry, normal reward).</summary>
    public sealed class SweepRealm : ICommand<List<Amount>>
    {
        public string RealmId;

        public List<Amount> Apply(CommandContext ctx)
        {
            var realm = Realms.Get(ctx.Content, RealmId);
            Realms.Validate(ctx);
            var level = Realms.Highest(ctx.State, realm.Id);
            ctx.Require(level > 0, "realm_nothing_to_sweep");
            Realms.UseEntry(ctx);
            var rewards = Dungeons.Rewards(realm.ClearRewards, level);
            ctx.Grant(rewards);
            Quests.Track(ctx, Stat.RealmCleared);
            return rewards;
        }
    }
}
