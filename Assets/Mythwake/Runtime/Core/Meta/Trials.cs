using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Math;
using Mythwake.Core.Progression;

namespace Mythwake.Core.Meta
{
    /// <summary>One tower of the Trial of Fragments: its Aspect and the mechanic its enemies carry.</summary>
    public sealed class TrialDefinition
    {
        public Aspect Aspect;
        public string Name;
        public string Description;
        /// <summary>Added to every enemy of this tower (the doc's trial mechanic).</summary>
        public List<PassiveDefinition> EnemyPassives = new List<PassiveDefinition>();
        public int EnemyDamageModifier;
    }

    public sealed class TrialGrade
    {
        public int FromFloor;
        public Grade Grade;
    }

    public sealed class TrialConfig
    {
        /// <summary>Fragment Shrine level that opens the trials.</summary>
        public int ShrineLevel = 4;
        public int MaxFloor = 100;
        public int EnemyLevelBase = 25;
        public int EnemyLevelPerFloor = 3;
        public int StarsEveryFloors = 20;
        public int BossEvery = 10;
        public int BossStatBonusPermille = 300;
        /// <summary>Enemy count by floor: index = count - 3 → first floor with that many (e.g. [1, 5, 10]).</summary>
        public int[] EnemyCountFloors = { 1, 5, 10 };
        /// <summary>Per floor (first clear): base + perStage × floor.</summary>
        public List<AfkRate> Rewards = new List<AfkRate>();
        public int MilestoneEvery = 10;
        public List<Amount> MilestoneRewards = new List<Amount>();
        /// <summary>Grade of the milestone Aspect Fragment by floor.</summary>
        public List<TrialGrade> MilestoneGrades = new List<TrialGrade>();
        public List<TrialDefinition> Trials = new List<TrialDefinition>();
    }

    public sealed class TrialState
    {
        /// <summary>Highest cleared floor per Aspect.</summary>
        public Dictionary<Aspect, int> Highest = new Dictionary<Aspect, int>();
        /// <summary>The last team sent into each tower.</summary>
        public Dictionary<Aspect, string[]> Teams = new Dictionary<Aspect, string[]>();
    }

    public sealed class TrialResult
    {
        public BattleOutcome Outcome;
        public Aspect Aspect;
        public int Floor;
        public List<Amount> Rewards = new List<Amount>();
        public List<FragmentItem> Fragments = new List<FragmentItem>();
    }

    /// <summary>Trial of Fragments (docs/systems/TRIALS.md).</summary>
    public static class Trials
    {
        public static TrialDefinition Get(GameContent content, Aspect aspect)
        {
            foreach (var t in content.Trials.Trials)
                if (t.Aspect == aspect)
                    return t;
            throw new RuleViolation("unknown_trial", aspect.ToString());
        }

        public static bool Open(PlayerState state, GameContent content) =>
            content.Trials.Trials.Count > 0 && Fragments.ShrineLevel(state, content) >= content.Trials.ShrineLevel;

        public static int Highest(PlayerState state, Aspect aspect) => state.Trials.Highest.TryGetValue(aspect, out var h) ? h : 0;

        public static int EnemyCount(GameContent content, int floor)
        {
            var count = 2;
            foreach (var from in content.Trials.EnemyCountFloors)
                if (floor >= from)
                    count++;
            return System.Math.Min(5, System.Math.Max(1, count));
        }

        public static bool IsBossFloor(GameContent content, int floor) => content.Trials.BossEvery > 0 && floor % content.Trials.BossEvery == 0;

        /// <summary>Enemies of a floor: monsters and heroes of the tower's Aspect, a pure function of Aspect and floor.</summary>
        public static List<UnitSetup> Enemies(GameContent content, Aspect aspect, int floor)
        {
            var pool = new List<HeroDefinition>();
            foreach (var m in content.Monsters.Values)
                if (m.Aspect == aspect && !m.Boss)
                    pool.Add(m);
            foreach (var h in content.Heroes.Values)
                if (h.Aspect == aspect && h.Role != Role.HeroLeader)
                    pool.Add(h);
            pool.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var bosses = new List<HeroDefinition>();
            foreach (var m in content.Monsters.Values)
                if (m.Aspect == aspect && m.Boss)
                    bosses.Add(m);
            bosses.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            var rng = new Pcg32((ulong)floor * 104_729UL + (ulong)aspect, 11);
            var picked = new List<HeroDefinition>();
            var count = EnemyCount(content, floor);
            var bossFloor = IsBossFloor(content, floor);
            if (bossFloor && bosses.Count > 0)
                picked.Add(bosses[(floor / content.Trials.BossEvery - 1) % bosses.Count]);
            while (picked.Count < count && pool.Count > 0)
                picked.Add(pool[rng.Range(pool.Count)]);

            var c = content.Trials;
            var level = c.EnemyLevelBase + c.EnemyLevelPerFloor * (floor - 1);
            var stars = 1 + (c.StarsEveryFloors > 0 ? (floor - 1) / c.StarsEveryFloors : 0);
            var team = Campaign.Arrange(picked, level, stars);
            if (bossFloor)
                foreach (var u in team)
                {
                    u.Variant = "boss";
                    if (u.Hero.Boss)
                        continue;
                    u.Stats.Hp = u.Stats.Hp * (Permille.One + c.BossStatBonusPermille) / Permille.One;
                    u.Stats.Attack = u.Stats.Attack * (Permille.One + c.BossStatBonusPermille) / Permille.One;
                }
            return team;
        }

        public static BattleSetup BuildBattle(PlayerState state, GameContent content, PendingBattle pending)
        {
            var aspect = pending.TrialAspect.Value;
            var trial = Get(content, aspect);
            var rules = new BattleRules { EnemyDamageModifier = trial.EnemyDamageModifier };
            foreach (var p in trial.EnemyPassives)
                rules.EnemyPassives.Add(p.Clone());
            return new BattleSetup
            {
                Seed = pending.Seed,
                Allies = Campaign.Allies(state, content, pending.Formation, withLeader: false),
                Enemies = Enemies(content, aspect, pending.Floor),
                Rules = rules
            };
        }

        public static List<Amount> FloorRewards(GameContent content, int floor)
        {
            var list = new List<Amount>();
            foreach (var r in content.Trials.Rewards)
                list.Add(new Amount(r.Id, r.At(floor)));
            if (content.Trials.MilestoneEvery > 0 && floor % content.Trials.MilestoneEvery == 0)
                list.AddRange(content.Trials.MilestoneRewards);
            return list;
        }

        public static Grade MilestoneGrade(GameContent content, int floor)
        {
            var grade = Grade.Rare;
            foreach (var g in content.Trials.MilestoneGrades)
                if (floor >= g.FromFloor)
                    grade = g.Grade;
            return grade;
        }

        /// <summary>A sensible starting team for a tower: the strongest owned heroes of its Aspect.</summary>
        public static string[] SuggestedTeam(PlayerState state, GameContent content, Aspect aspect, bool useSaved = true)
        {
            if (useSaved && state.Trials.Teams.TryGetValue(aspect, out var saved) && saved != null)
                return (string[])saved.Clone();
            var heroes = new List<OwnedHero>();
            foreach (var h in state.Heroes)
            {
                var def = content.Hero(h.HeroId);
                if (def.Aspect == aspect && def.Role != Role.HeroLeader)
                    heroes.Add(h);
            }
            heroes.Sort((a, b) =>
            {
                var pa = TeamBuilder.Power(TeamBuilder.StatsOf(Formations.Progress(state, content, a)));
                var pb = TeamBuilder.Power(TeamBuilder.StatsOf(Formations.Progress(state, content, b)));
                return pb.CompareTo(pa);
            });
            var slots = new string[5];
            var take = System.Math.Min(heroes.Count, Formations.UnlockedSlots(state, content));
            var front = new List<OwnedHero>();
            var back = new List<OwnedHero>();
            for (var i = 0; i < take; i++)
                (content.Hero(heroes[i].HeroId).Row == Row.Front ? front : back).Add(heroes[i]);
            void Put(List<OwnedHero> list, int[] order)
            {
                foreach (var h in list)
                    foreach (var s in order)
                        if (slots[s] == null)
                        {
                            slots[s] = h.HeroId;
                            break;
                        }
            }
            Put(front, new[] { 0, 1, 2, 3, 4 });
            Put(back, new[] { 2, 3, 4, 0, 1 });
            return slots;
        }
    }

    // ------------------------------------------------------------------ commands

    /// <summary>Starts the next floor of a tower with a team of that tower's Aspect.</summary>
    public sealed class BeginTrial : ICommand<BattleSetup>
    {
        public Aspect Aspect;
        public string[] Slots = new string[5];

        public BattleSetup Apply(CommandContext ctx)
        {
            var state = ctx.State;
            var content = ctx.Content;
            ctx.Require(Trials.Open(state, content), "trial_locked");
            Trials.Get(content, Aspect);
            var floor = Trials.Highest(state, Aspect) + 1;
            ctx.Require(floor <= content.Trials.MaxFloor, "trial_complete");
            ctx.Require(Slots != null && Slots.Length == 5, "bad_formation");
            var seen = new HashSet<string>();
            var count = 0;
            foreach (var id in Slots)
            {
                if (string.IsNullOrEmpty(id))
                    continue;
                ctx.Require(state.FindHero(id) != null, "hero_not_owned", id);
                ctx.Require(seen.Add(id), "hero_twice", id);
                var hero = content.Hero(id);
                ctx.Require(hero.Role != Role.HeroLeader, "leader_cannot_fight", id);
                ctx.Require(hero.Aspect == Aspect, "trial_wrong_aspect", id);
                count++;
            }
            ctx.Require(count > 0, "formation_empty");
            ctx.Require(count <= Formations.UnlockedSlots(state, content), "slots_locked");

            state.Trials.Teams[Aspect] = (string[])Slots.Clone();
            var seed = ((ulong)ctx.NextRng().NextUInt() << 32) | ctx.NextRng().NextUInt();
            state.Battle = new PendingBattle
            {
                Stage = state.Campaign.HighestCleared, TrialAspect = Aspect, Floor = floor, Seed = seed,
                Formation = (string[])Slots.Clone()
            };
            return Trials.BuildBattle(state, content, state.Battle);
        }
    }

    public sealed class FinishTrial : ICommand<TrialResult>
    {
        public bool AutoUltimates = true;
        public List<int> AutoToggleTimesMs = new List<int>();
        public List<UltimateRequest> UltimateRequests = new List<UltimateRequest>();

        public TrialResult Apply(CommandContext ctx)
        {
            var state = ctx.State;
            ctx.Require(state.Battle != null && state.Battle.TrialAspect != null, "no_battle");
            var pending = state.Battle;
            var setup = Trials.BuildBattle(state, ctx.Content, pending);
            setup.AutoUltimates = AutoUltimates;
            setup.AutoToggleTimesMs.AddRange(AutoToggleTimesMs);
            setup.UltimateRequests.AddRange(UltimateRequests);
            var battle = BattleSimulator.Run(setup);
            state.Battle = null;

            var aspect = pending.TrialAspect.Value;
            var result = new TrialResult { Outcome = battle.Outcome, Aspect = aspect, Floor = pending.Floor };
            if (battle.Outcome != BattleOutcome.Victory)
                return result;
            state.Trials.Highest[aspect] = pending.Floor;
            result.Rewards.AddRange(Trials.FloorRewards(ctx.Content, pending.Floor));
            ctx.Grant(result.Rewards);
            var t = ctx.Content.Trials;
            if (t.MilestoneEvery > 0 && pending.Floor % t.MilestoneEvery == 0 && ctx.Content.Fragments.Grades.Count > 0)
            {
                var rng = ctx.NextRng();
                var effect = Fragments.RollEffect(rng, ctx.Content, FragmentSlot.Aspect, aspect);
                result.Fragments.Add(Fragments.Create(state, FragmentSlot.Aspect, aspect, Trials.MilestoneGrade(ctx.Content, pending.Floor), effect));
            }
            Quests.Track(ctx, Stat.BattleWon);
            Quests.Track(ctx, Stat.TrialCleared);
            return result;
        }
    }
}
