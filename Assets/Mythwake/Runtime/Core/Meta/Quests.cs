using System.Collections.Generic;

namespace Mythwake.Core.Meta
{
    public enum QuestKind { Daily, Weekly }

    /// <summary>Lifetime counters that quests measure. See docs/systems/QUESTS.md.</summary>
    public static class Stat
    {
        public const string BattleWon = "battleWon";
        public const string StageCleared = "stageCleared";
        public const string AfkClaimed = "afkClaimed";
        public const string VillageCollected = "villageCollected";
        public const string HeroLevelUps = "heroLevelUps";
        public const string Summons = "summons";
        public const string GearEnhanced = "gearEnhanced";
        public const string GearCrafted = "gearCrafted";
        public const string BuildingUpgraded = "buildingUpgraded";
        public const string HuntCompleted = "huntCompleted";
        public const string DailyCompleted = "dailyCompleted";
        public const string DungeonCleared = "dungeonCleared";
        public const string FragmentUpgraded = "fragmentUpgraded";
        public const string FragmentStabilized = "fragmentStabilized";
        public const string RelicUpgraded = "relicUpgraded";
        public const string TrialCleared = "trialCleared";
        public const string RealmCleared = "realmCleared";
        public const string BossRefight = "bossRefight";
        public const string DefenseWave = "defenseWave";
    }

    public sealed class TaskDefinition
    {
        public string Id;
        public string Text;
        public string Stat;
        public long Target;
        public int Points;
    }

    public sealed class ChestDefinition
    {
        public int Points;
        public List<Amount> Rewards = new List<Amount>();
    }

    public enum MainCondition { StageCleared, BuildingLevel, HeroLevel, HeroesOwned, FullyEquipped }

    public sealed class MainQuestDefinition
    {
        public string Id;
        public string Text;
        public MainCondition Condition;
        /// <summary>Building id for BuildingLevel.</summary>
        public string Target;
        public long Value;
        public List<Amount> Rewards = new List<Amount>();
    }

    public sealed class QuestConfig
    {
        public int ResetHourUtc = 4;
        public List<TaskDefinition> Daily = new List<TaskDefinition>();
        public List<ChestDefinition> DailyChests = new List<ChestDefinition>();
        public List<TaskDefinition> Weekly = new List<TaskDefinition>();
        public List<ChestDefinition> WeeklyChests = new List<ChestDefinition>();
        public List<MainQuestDefinition> Main = new List<MainQuestDefinition>();
    }

    public sealed class PeriodState
    {
        /// <summary>Day or week index this period belongs to (-1 = never started).</summary>
        public long Index = -1;
        /// <summary>Stats at the first action of the period.</summary>
        public Dictionary<string, long> Start = new Dictionary<string, long>();
        public List<string> Claimed = new List<string>();
        public List<int> Chests = new List<int>();
        public int Points;
    }

    public sealed class QuestState
    {
        public PeriodState Daily = new PeriodState();
        public PeriodState Weekly = new PeriodState();
        /// <summary>Index of the current main quest in the chain.</summary>
        public int Main;
    }

    public static class Quests
    {
        public const long DayMs = 86_400_000;

        public static long DayIndex(GameContent content, long nowMs) =>
            FloorDiv(nowMs - content.Quests.ResetHourUtc * Afk.HourMs, DayMs);

        /// <summary>Weeks start on Monday (1970-01-01 was a Thursday).</summary>
        public static long WeekIndex(GameContent content, long nowMs) => FloorDiv(DayIndex(content, nowMs) + 3, 7);

        public static long NextDailyResetMs(GameContent content, long nowMs) =>
            (DayIndex(content, nowMs) + 1) * DayMs + content.Quests.ResetHourUtc * Afk.HourMs;

        public static long NextWeeklyResetMs(GameContent content, long nowMs) =>
            ((WeekIndex(content, nowMs) + 1) * 7 - 3) * DayMs + content.Quests.ResetHourUtc * Afk.HourMs;

        private static long FloorDiv(long a, long b) => a >= 0 ? a / b : (a - b + 1) / b;

        public static long StatValue(PlayerState state, string stat) => state.Stats.TryGetValue(stat, out var v) ? v : 0;

        /// <summary>Starts a new day/week if the reset passed. Called before every tracked action and claim.</summary>
        public static void Roll(PlayerState state, GameContent content, long nowMs)
        {
            RollPeriod(state, state.Quests.Daily, DayIndex(content, nowMs));
            RollPeriod(state, state.Quests.Weekly, WeekIndex(content, nowMs));
        }

        private static void RollPeriod(PlayerState state, PeriodState period, long index)
        {
            if (period.Index == index)
                return;
            period.Index = index;
            period.Start = new Dictionary<string, long>(state.Stats);
            period.Claimed.Clear();
            period.Chests.Clear();
            period.Points = 0;
        }

        public static void Track(CommandContext ctx, string stat, long amount = 1)
        {
            if (amount <= 0)
                return;
            Roll(ctx.State, ctx.Content, ctx.NowMs);
            ctx.State.Stats[stat] = StatValue(ctx.State, stat) + amount;
        }

        private static PeriodState Period(PlayerState state, QuestKind kind) =>
            kind == QuestKind.Daily ? state.Quests.Daily : state.Quests.Weekly;

        public static List<TaskDefinition> Tasks(GameContent content, QuestKind kind) =>
            kind == QuestKind.Daily ? content.Quests.Daily : content.Quests.Weekly;

        public static List<ChestDefinition> Chests(GameContent content, QuestKind kind) =>
            kind == QuestKind.Daily ? content.Quests.DailyChests : content.Quests.WeeklyChests;

        private static bool Current(GameContent content, PeriodState period, QuestKind kind, long nowMs) =>
            period.Index == (kind == QuestKind.Daily ? DayIndex(content, nowMs) : WeekIndex(content, nowMs));

        /// <summary>Progress of a task in the running period (0 if the period has not started yet). Read-only.</summary>
        public static long Progress(PlayerState state, GameContent content, QuestKind kind, TaskDefinition task, long nowMs)
        {
            var period = Period(state, kind);
            if (!Current(content, period, kind, nowMs))
                return 0;
            period.Start.TryGetValue(task.Stat, out var start);
            return System.Math.Min(task.Target, StatValue(state, task.Stat) - start);
        }

        public static bool IsClaimed(PlayerState state, GameContent content, QuestKind kind, string id, long nowMs)
        {
            var period = Period(state, kind);
            return Current(content, period, kind, nowMs) && period.Claimed.Contains(id);
        }

        public static int Points(PlayerState state, GameContent content, QuestKind kind, long nowMs)
        {
            var period = Period(state, kind);
            return Current(content, period, kind, nowMs) ? period.Points : 0;
        }

        public static bool ChestClaimed(PlayerState state, GameContent content, QuestKind kind, int points, long nowMs)
        {
            var period = Period(state, kind);
            return Current(content, period, kind, nowMs) && period.Chests.Contains(points);
        }

        public static MainQuestDefinition CurrentMain(PlayerState state, GameContent content) =>
            state.Quests.Main < content.Quests.Main.Count ? content.Quests.Main[state.Quests.Main] : null;

        /// <summary>Progress towards the condition, as (have, need).</summary>
        public static (long have, long need) MainProgress(PlayerState state, GameContent content, MainQuestDefinition q)
        {
            long have = 0;
            switch (q.Condition)
            {
                case MainCondition.StageCleared:
                    have = state.Campaign.HighestCleared;
                    break;
                case MainCondition.BuildingLevel:
                    have = Village.Level(state, Village.Building(content, q.Target));
                    break;
                case MainCondition.HeroLevel:
                    foreach (var h in state.Heroes)
                        have = System.Math.Max(have, h.Level);
                    break;
                case MainCondition.HeroesOwned:
                    have = state.Heroes.Count;
                    break;
                case MainCondition.FullyEquipped:
                    foreach (var h in state.Heroes)
                    {
                        var slots = 0;
                        foreach (var g in state.Gear)
                            if (g.EquippedBy == h.HeroId)
                                slots++;
                        if (slots >= 4)
                            have++;
                    }
                    break;
            }
            return (System.Math.Min(have, q.Value), q.Value);
        }

        /// <summary>Anything to claim right now (for the badge on the quest button).</summary>
        public static bool AnyClaimable(PlayerState state, GameContent content, long nowMs)
        {
            var main = CurrentMain(state, content);
            if (main != null)
            {
                var (have, need) = MainProgress(state, content, main);
                if (have >= need)
                    return true;
            }
            foreach (var kind in new[] { QuestKind.Daily, QuestKind.Weekly })
            {
                foreach (var t in Tasks(content, kind))
                    if (!IsClaimed(state, content, kind, t.Id, nowMs) && Progress(state, content, kind, t, nowMs) >= t.Target)
                        return true;
                var points = Points(state, content, kind, nowMs);
                foreach (var c in Chests(content, kind))
                    if (points >= c.Points && !ChestClaimed(state, content, kind, c.Points, nowMs))
                        return true;
            }
            return false;
        }
    }

    // ------------------------------------------------------------------ commands

    public sealed class ClaimQuest : ICommand<int>
    {
        public QuestKind Kind;
        public string QuestId;

        public int Apply(CommandContext ctx)
        {
            var content = ctx.Content;
            TaskDefinition task = null;
            foreach (var t in Quests.Tasks(content, Kind))
                if (t.Id == QuestId)
                    task = t;
            ctx.Require(task != null, "unknown_quest", QuestId);
            Quests.Roll(ctx.State, content, ctx.NowMs);
            var period = Kind == QuestKind.Daily ? ctx.State.Quests.Daily : ctx.State.Quests.Weekly;
            ctx.Require(!period.Claimed.Contains(task.Id), "quest_claimed");
            ctx.Require(Quests.Progress(ctx.State, content, Kind, task, ctx.NowMs) >= task.Target, "quest_not_done");

            var before = period.Points;
            period.Claimed.Add(task.Id);
            period.Points += task.Points;
            // finishing the whole daily board counts for the weekly quest
            if (Kind == QuestKind.Daily)
            {
                var full = 0;
                foreach (var c in content.Quests.DailyChests)
                    full = System.Math.Max(full, c.Points);
                if (before < full && period.Points >= full)
                    Quests.Track(ctx, Stat.DailyCompleted);
            }
            return period.Points;
        }
    }

    public sealed class ClaimQuestChest : ICommand<List<Amount>>
    {
        public QuestKind Kind;
        public int Points;

        public List<Amount> Apply(CommandContext ctx)
        {
            ChestDefinition chest = null;
            foreach (var c in Quests.Chests(ctx.Content, Kind))
                if (c.Points == Points)
                    chest = c;
            ctx.Require(chest != null, "unknown_chest");
            Quests.Roll(ctx.State, ctx.Content, ctx.NowMs);
            var period = Kind == QuestKind.Daily ? ctx.State.Quests.Daily : ctx.State.Quests.Weekly;
            ctx.Require(period.Points >= chest.Points, "chest_locked");
            ctx.Require(!period.Chests.Contains(chest.Points), "chest_claimed");
            period.Chests.Add(chest.Points);
            ctx.Grant(chest.Rewards);
            return chest.Rewards;
        }
    }

    public sealed class ClaimMainQuest : ICommand<List<Amount>>
    {
        public List<Amount> Apply(CommandContext ctx)
        {
            var quest = Quests.CurrentMain(ctx.State, ctx.Content);
            ctx.Require(quest != null, "main_quests_done");
            var (have, need) = Quests.MainProgress(ctx.State, ctx.Content, quest);
            ctx.Require(have >= need, "quest_not_done");
            ctx.State.Quests.Main++;
            ctx.Grant(quest.Rewards);
            return quest.Rewards;
        }
    }
}
