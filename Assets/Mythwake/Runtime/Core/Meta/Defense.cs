using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Math;

namespace Mythwake.Core.Meta
{
    public sealed class DefenseConfig
    {
        public int RunsPerDay = 2;
        public int MaxWaves = 30;
        /// <summary>Enemy level = campaign enemy level × this permille + EnemyLevelPerWave × (wave - 1).</summary>
        public int EnemyLevelFromStagePermille = 900;
        public int EnemyLevelPerWave = 2;
        public int BossEvery = 5;
        /// <summary>Survivors heal this permille of max HP between waves.</summary>
        public int RecoverPermille = 100;
        /// <summary>Per cleared wave: base + perStage × wave.</summary>
        public List<AfkRate> WaveRewards = new List<AfkRate>();
        public int MilestoneEvery = 10;
        public List<Amount> MilestoneRewards = new List<Amount>();
        /// <summary>Reward bonus per Watchtower level above 1.</summary>
        public int RewardPerLevelPermille = 100;
    }

    /// <summary>A defense run in progress: next wave and the team's HP / energy per formation slot.</summary>
    public sealed class DefenseRun
    {
        public int Wave = 1;
        public ulong Seed;
        public string[] Formation = new string[5];
        /// <summary>HP per slot; -1 = full, 0 = fallen.</summary>
        public long[] Hp = { -1, -1, -1, -1, -1 };
        public int[] Energy = new int[5];
    }

    public sealed class DefenseState
    {
        public long Day = -1;
        public int RunsUsed;
        public DefenseRun Run;
        public int BestWave;
    }

    public sealed class DefenseResult
    {
        public BattleOutcome Outcome;
        public int Wave;
        public List<Amount> Rewards = new List<Amount>();
        /// <summary>The run is over (defeat or last wave).</summary>
        public bool RunOver;
    }

    /// <summary>Watchtower village defense (docs/systems/WATCHTOWER.md).</summary>
    public static class Defense
    {
        public static int TowerLevel(PlayerState state, GameContent content)
        {
            foreach (var b in content.Village.Buildings)
                if (b.Effect == BuildingEffect.Defense)
                    return Village.Level(state, b);
            return 0;
        }

        public static int RunsLeft(PlayerState state, GameContent content, long nowMs)
        {
            var used = state.Defense.Day == Quests.DayIndex(content, nowMs) ? state.Defense.RunsUsed : 0;
            return System.Math.Max(0, content.Defense.RunsPerDay - used);
        }

        public static int EnemyCount(int wave) => System.Math.Min(5, 3 + (wave - 1) / 4);

        public static bool IsBossWave(GameContent content, int wave) => content.Defense.BossEvery > 0 && wave % content.Defense.BossEvery == 0;

        /// <summary>Monsters attacking Dawnhollow in a wave: a pure function of the run seed and the wave.</summary>
        public static List<UnitSetup> Enemies(PlayerState state, GameContent content, ulong seed, int wave)
        {
            var pool = new List<HeroDefinition>();
            var bosses = new List<HeroDefinition>();
            foreach (var m in content.Monsters.Values)
                (m.Boss ? bosses : pool).Add(m);
            pool.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            bosses.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var rng = new Pcg32(seed + (ulong)wave * 7_919UL, 13);
            var picked = new List<HeroDefinition>();
            if (IsBossWave(content, wave) && bosses.Count > 0)
                picked.Add(bosses[rng.Range(bosses.Count)]);
            var count = EnemyCount(wave);
            while (picked.Count < count && pool.Count > 0)
                picked.Add(pool[rng.Range(pool.Count)]);
            var c = content.Defense;
            var stage = System.Math.Max(1, state.Campaign.HighestCleared);
            var level = System.Math.Max(1, Campaign.EnemyLevel(content, stage) * c.EnemyLevelFromStagePermille / Permille.One)
                        + c.EnemyLevelPerWave * (wave - 1);
            var team = Campaign.Arrange(picked, level, Campaign.EnemyStars(content, stage));
            foreach (var u in team)
                if (u.Hero.Boss)
                    u.Variant = "boss";
            return team;
        }

        public static List<Amount> WaveRewards(PlayerState state, GameContent content, int wave)
        {
            var c = content.Defense;
            var bonus = Permille.One + c.RewardPerLevelPermille * System.Math.Max(0, TowerLevel(state, content) - 1);
            var list = new List<Amount>();
            foreach (var r in c.WaveRewards)
                list.Add(new Amount(r.Id, r.At(wave) * bonus / Permille.One));
            if (c.MilestoneEvery > 0 && wave % c.MilestoneEvery == 0)
                list.AddRange(c.MilestoneRewards);
            return list;
        }

        /// <summary>The run's team for the next wave: fallen heroes stay out, the others keep their HP and energy.</summary>
        public static BattleSetup BuildBattle(PlayerState state, GameContent content, PendingBattle pending)
        {
            var run = state.Defense.Run;
            var formation = (string[])pending.Formation.Clone();
            for (var i = 0; i < 5; i++)
                if (run.Hp[i] == 0)
                    formation[i] = null;
            var allies = Campaign.Allies(state, content, formation);
            foreach (var u in allies)
            {
                if (run.Hp[u.Slot] > 0)
                    u.StartHp = run.Hp[u.Slot];
                u.StartEnergy = run.Energy[u.Slot];
            }
            var setup = new BattleSetup
            {
                Seed = pending.Seed, Allies = allies, Leader = Campaign.LeaderUnit(state, content),
                Enemies = Enemies(state, content, run.Seed, pending.Floor)
            };
            BossFragments.Apply(setup, state, content);
            return setup;
        }
    }

    // ------------------------------------------------------------------ commands

    /// <summary>Starts a defense run with the current formation (uses one of the day's runs).</summary>
    public sealed class StartDefense : ICommand<bool>
    {
        public bool Apply(CommandContext ctx)
        {
            var state = ctx.State;
            var d = state.Defense;
            ctx.Require(Defense.TowerLevel(state, ctx.Content) > 0, "defense_locked");
            ctx.Require(d.Run == null, "defense_running");
            ctx.Require(Defense.RunsLeft(state, ctx.Content, ctx.NowMs) > 0, "defense_no_runs");
            ctx.Require(Formations.Count(state.Formation) > 0, "formation_empty");
            var day = Quests.DayIndex(ctx.Content, ctx.NowMs);
            if (d.Day != day)
            {
                d.Day = day;
                d.RunsUsed = 0;
            }
            d.RunsUsed++;
            d.Run = new DefenseRun { Seed = ctx.NextRng().NextUInt(), Formation = (string[])state.Formation.Clone() };
            return true;
        }
    }

    public sealed class BeginDefenseWave : ICommand<BattleSetup>
    {
        public BattleSetup Apply(CommandContext ctx)
        {
            var state = ctx.State;
            var run = state.Defense.Run;
            ctx.Require(run != null, "defense_no_run");
            var seed = ((ulong)ctx.NextRng().NextUInt() << 32) | ctx.NextRng().NextUInt();
            state.Battle = new PendingBattle
            {
                Stage = state.Campaign.HighestCleared, DefenseWave = true, Floor = run.Wave, Seed = seed,
                Formation = (string[])run.Formation.Clone()
            };
            return Defense.BuildBattle(state, ctx.Content, state.Battle);
        }
    }

    public sealed class FinishDefenseWave : ICommand<DefenseResult>
    {
        public bool AutoUltimates = true;
        public List<int> AutoToggleTimesMs = new List<int>();
        public List<UltimateRequest> UltimateRequests = new List<UltimateRequest>();

        public DefenseResult Apply(CommandContext ctx)
        {
            var state = ctx.State;
            var content = ctx.Content;
            ctx.Require(state.Battle != null && state.Battle.DefenseWave, "no_battle");
            var pending = state.Battle;
            var setup = Defense.BuildBattle(state, content, pending);
            setup.AutoUltimates = AutoUltimates;
            setup.AutoToggleTimesMs.AddRange(AutoToggleTimesMs);
            setup.UltimateRequests.AddRange(UltimateRequests);
            var battle = BattleSimulator.Run(setup);
            state.Battle = null;

            var d = state.Defense;
            var run = d.Run;
            var result = new DefenseResult { Outcome = battle.Outcome, Wave = pending.Floor };
            if (battle.Outcome != BattleOutcome.Victory)
            {
                d.BestWave = System.Math.Max(d.BestWave, pending.Floor - 1);
                d.Run = null;
                result.RunOver = true;
                return result;
            }
            result.Rewards.AddRange(Defense.WaveRewards(state, content, pending.Floor));
            ctx.Grant(result.Rewards);
            d.BestWave = System.Math.Max(d.BestWave, pending.Floor);
            Quests.Track(ctx, Stat.BattleWon);
            Quests.Track(ctx, Stat.DefenseWave);

            // carry HP and energy; survivors recover a little, the fallen stay down
            foreach (var u in battle.Units)
            {
                if (u.Side != Side.Allies)
                    continue;
                if (u.FinalHp <= 0)
                {
                    run.Hp[u.Slot] = 0;
                    run.Energy[u.Slot] = 0;
                    continue;
                }
                var hp = u.FinalHp + u.MaxHp * content.Defense.RecoverPermille / Permille.One;
                run.Hp[u.Slot] = hp >= u.MaxHp ? -1 : hp;
                run.Energy[u.Slot] = u.FinalEnergy;
            }
            run.Wave++;
            if (run.Wave > content.Defense.MaxWaves)
            {
                d.Run = null;
                result.RunOver = true;
            }
            return result;
        }
    }

    /// <summary>Retreat: ends the running defense (rewards of cleared waves are already paid).</summary>
    public sealed class EndDefense : ICommand<bool>
    {
        public bool Apply(CommandContext ctx)
        {
            ctx.Require(ctx.State.Defense.Run != null, "defense_no_run");
            ctx.Require(ctx.State.Battle == null || !ctx.State.Battle.DefenseWave, "defense_in_battle");
            ctx.State.Defense.Run = null;
            return true;
        }
    }
}
