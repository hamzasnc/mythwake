using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Math;

namespace Mythwake.Core.Meta
{
    public sealed class DungeonDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        /// <summary>Portal Chamber level that opens this dungeon.</summary>
        public int PortalLevel = 1;
        public string Boss;
        public List<string> Escort = new List<string>();
        public int MaxLevel = 100;
        public int EnemyLevelBase = 15;
        public int EnemyLevelPerFloor = 2;
        public int Escorts = 2;
        public int AttemptsPerDay = 3;
        /// <summary>Special rule of the dungeon (doc: healing reduced, enemies regenerate, …).</summary>
        public BattleRules Rules = new BattleRules();
        /// <summary>Per clear: base + perStage × difficulty level.</summary>
        public List<AfkRate> ClearRewards = new List<AfkRate>();
        public List<AfkRate> FirstClearRewards = new List<AfkRate>();
        /// <summary>Gear piece on first clear of every N-th level (0 = none).</summary>
        public int GearEveryLevels;
        /// <summary>Fragment on first clear of every N-th level (0 = none).</summary>
        public int FragmentEveryLevels;
        /// <summary>A Relic from RelicPool on first clear of every N-th level (0 = none).</summary>
        public int RelicEveryLevels;
        public List<string> RelicPool = new List<string>();
    }

    public sealed class DungeonConfig
    {
        public List<DungeonDefinition> Dungeons = new List<DungeonDefinition>();
    }

    public sealed class DungeonState
    {
        /// <summary>Highest cleared difficulty level per dungeon.</summary>
        public Dictionary<string, int> Highest = new Dictionary<string, int>();
        public long Day = -1;
        public Dictionary<string, int> Used = new Dictionary<string, int>();
    }

    public sealed class DungeonResult
    {
        public BattleOutcome Outcome;
        public List<Amount> Rewards = new List<Amount>();
        public List<GearItem> Gear = new List<GearItem>();
        public List<FragmentItem> Fragments = new List<FragmentItem>();
        public List<RelicItem> Relics = new List<RelicItem>();
        public bool FirstClear;
    }

    /// <summary>Portal Chamber dungeons (docs/systems/DUNGEONS.md).</summary>
    public static class Dungeons
    {
        public static DungeonDefinition Get(GameContent content, string id)
        {
            foreach (var d in content.Dungeons.Dungeons)
                if (d.Id == id)
                    return d;
            throw new RuleViolation("unknown_dungeon", id);
        }

        public static int PortalLevel(PlayerState state, GameContent content)
        {
            foreach (var b in content.Village.Buildings)
                if (b.Effect == BuildingEffect.Portal)
                    return Village.Level(state, b);
            return 0;
        }

        public static bool Open(PlayerState state, GameContent content, DungeonDefinition d) => PortalLevel(state, content) >= d.PortalLevel;

        public static int Highest(PlayerState state, string id) => state.Dungeons.Highest.TryGetValue(id, out var h) ? h : 0;

        public static int AttemptsLeft(PlayerState state, GameContent content, DungeonDefinition d, long nowMs)
        {
            var used = state.Dungeons.Day == Quests.DayIndex(content, nowMs) && state.Dungeons.Used.TryGetValue(d.Id, out var u) ? u : 0;
            return System.Math.Max(0, d.AttemptsPerDay - used);
        }

        public static int EnemyLevel(DungeonDefinition d, int level) => d.EnemyLevelBase + d.EnemyLevelPerFloor * (level - 1);

        public static List<UnitSetup> Enemies(GameContent content, DungeonDefinition d, int level)
        {
            var heroes = new List<HeroDefinition> { content.Hero(d.Boss) };
            for (var i = 0; i < d.Escorts && d.Escort.Count > 0; i++)
                heroes.Add(content.Hero(d.Escort[i % d.Escort.Count]));
            var stars = 1 + level / 25;
            var team = Campaign.Arrange(heroes, EnemyLevel(d, level), stars);
            foreach (var u in team)
                if (u.Hero.Boss)
                    u.Variant = "boss";
            return team;
        }

        /// <summary>Clear rewards with a Relic reward bonus (doc: "bonus rewards from certain areas").</summary>
        public static List<Amount> ClearRewards(PlayerState state, GameContent content, DungeonDefinition d, int level, string[] formation)
        {
            var bonus = Relics.TeamRewardBonus(state, content, formation, d.Id);
            var list = Rewards(d.ClearRewards, level);
            if (bonus > 0)
                for (var i = 0; i < list.Count; i++)
                    list[i] = new Amount(list[i].Id, list[i].Value * (Permille.One + bonus) / Permille.One);
            return list;
        }

        public static List<Amount> Rewards(List<AfkRate> table, int level)
        {
            var list = new List<Amount>();
            foreach (var r in table)
                list.Add(new Amount(r.Id, r.At(level)));
            return list;
        }

        public static BattleSetup BuildBattle(PlayerState state, GameContent content, DungeonDefinition d, PendingBattle pending)
        {
            var setup = new BattleSetup
            {
                Seed = pending.Seed,
                Allies = Campaign.Allies(state, content, pending.Formation),
                Enemies = Enemies(content, d, pending.Floor),
                Rules = d.Rules ?? new BattleRules()
            };
            return setup;
        }

        internal static void UseAttempt(CommandContext ctx, DungeonDefinition d)
        {
            var s = ctx.State.Dungeons;
            var day = Quests.DayIndex(ctx.Content, ctx.NowMs);
            if (s.Day != day)
            {
                s.Day = day;
                s.Used.Clear();
            }
            s.Used.TryGetValue(d.Id, out var used);
            s.Used[d.Id] = used + 1;
        }

        internal static void Validate(CommandContext ctx, DungeonDefinition d)
        {
            ctx.Require(Open(ctx.State, ctx.Content, d), "dungeon_locked");
            ctx.Require(AttemptsLeft(ctx.State, ctx.Content, d, ctx.NowMs) > 0, "dungeon_no_attempts");
        }
    }

    // ------------------------------------------------------------------ commands

    public sealed class BeginDungeon : ICommand<BattleSetup>
    {
        public string DungeonId;
        public int Level;

        public BattleSetup Apply(CommandContext ctx)
        {
            var d = Dungeons.Get(ctx.Content, DungeonId);
            Dungeons.Validate(ctx, d);
            ctx.Require(Level >= 1 && Level <= d.MaxLevel && Level <= Dungeons.Highest(ctx.State, d.Id) + 1, "dungeon_level_locked");
            ctx.Require(Formations.Count(ctx.State.Formation) > 0, "formation_empty");
            var seed = ((ulong)ctx.NextRng().NextUInt() << 32) | ctx.NextRng().NextUInt();
            ctx.State.Battle = new PendingBattle
            {
                Stage = ctx.State.Campaign.HighestCleared, DungeonId = d.Id, Floor = Level, Seed = seed,
                Formation = (string[])ctx.State.Formation.Clone()
            };
            return Dungeons.BuildBattle(ctx.State, ctx.Content, d, ctx.State.Battle);
        }
    }

    public sealed class FinishDungeon : ICommand<DungeonResult>
    {
        public bool AutoUltimates = true;
        public List<int> AutoToggleTimesMs = new List<int>();
        public List<UltimateRequest> UltimateRequests = new List<UltimateRequest>();

        public DungeonResult Apply(CommandContext ctx)
        {
            var state = ctx.State;
            ctx.Require(state.Battle != null && state.Battle.DungeonId != null && state.Battle.RealmId == null, "no_battle");
            var pending = state.Battle;
            var d = Dungeons.Get(ctx.Content, pending.DungeonId);
            var setup = Dungeons.BuildBattle(state, ctx.Content, d, pending);
            setup.AutoUltimates = AutoUltimates;
            setup.AutoToggleTimesMs.AddRange(AutoToggleTimesMs);
            setup.UltimateRequests.AddRange(UltimateRequests);
            var battle = BattleSimulator.Run(setup);
            state.Battle = null;

            var result = new DungeonResult { Outcome = battle.Outcome };
            if (battle.Outcome != BattleOutcome.Victory)
                return result;
            // attempts are checked again: the day may have rolled over during the fight, which is fine, but never go below zero
            if (Dungeons.AttemptsLeft(state, ctx.Content, d, ctx.NowMs) <= 0)
                throw new RuleViolation("dungeon_no_attempts");
            Dungeons.UseAttempt(ctx, d);
            result.Rewards.AddRange(Dungeons.ClearRewards(state, ctx.Content, d, pending.Floor, pending.Formation));
            if (pending.Floor > Dungeons.Highest(state, d.Id))
            {
                result.FirstClear = true;
                state.Dungeons.Highest[d.Id] = pending.Floor;
                result.Rewards.AddRange(Dungeons.Rewards(d.FirstClearRewards, pending.Floor));
                if (d.GearEveryLevels > 0 && pending.Floor % d.GearEveryLevels == 0)
                    result.Gear.Add(Gear.Drop(ctx));
                if (d.FragmentEveryLevels > 0 && pending.Floor % d.FragmentEveryLevels == 0)
                    result.Fragments.Add(Fragments.Drop(ctx));
                if (d.RelicEveryLevels > 0 && pending.Floor % d.RelicEveryLevels == 0)
                {
                    var relic = Relics.DropFrom(ctx, d.RelicPool);
                    if (relic != null)
                        result.Relics.Add(relic);
                }
            }
            ctx.Grant(result.Rewards);
            Quests.Track(ctx, Stat.BattleWon);
            Quests.Track(ctx, Stat.DungeonCleared);
            return result;
        }
    }

    /// <summary>Clears the highest cleared level again without a fight (one attempt, normal reward).</summary>
    public sealed class SweepDungeon : ICommand<List<Amount>>
    {
        public string DungeonId;

        public List<Amount> Apply(CommandContext ctx)
        {
            var d = Dungeons.Get(ctx.Content, DungeonId);
            Dungeons.Validate(ctx, d);
            var level = Dungeons.Highest(ctx.State, d.Id);
            ctx.Require(level > 0, "dungeon_nothing_to_sweep");
            Dungeons.UseAttempt(ctx, d);
            var rewards = Dungeons.ClearRewards(ctx.State, ctx.Content, d, level, ctx.State.Formation);
            ctx.Grant(rewards);
            Quests.Track(ctx, Stat.DungeonCleared);
            return rewards;
        }
    }
}
