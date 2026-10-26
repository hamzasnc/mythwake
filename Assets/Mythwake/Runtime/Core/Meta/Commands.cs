using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Math;

namespace Mythwake.Core.Meta
{
    /// <summary>Everything a command may touch. Time and randomness come from here, never from the environment.</summary>
    public sealed class CommandContext
    {
        public PlayerState State;
        public GameContent Content;
        /// <summary>Unix ms, supplied by the authority (device clock locally, server clock remotely).</summary>
        public long NowMs;

        /// <summary>Next deterministic random generator from the player's RNG stream.</summary>
        public Pcg32 NextRng() => new Pcg32(State.RngSeed, State.RngCounter++);

        public void Require(bool condition, string code, string detail = null)
        {
            if (!condition)
                throw new RuleViolation(code, detail);
        }

        public void RequireFunds(IEnumerable<Amount> costs)
        {
            foreach (var c in costs)
                Require(State.Get(c.Id) >= c.Value, "not_enough:" + c.Id);
        }

        public void Pay(IEnumerable<Amount> costs)
        {
            foreach (var c in costs)
                State.Add(c.Id, -c.Value);
        }

        public void Grant(IEnumerable<Amount> rewards)
        {
            foreach (var r in rewards)
                State.Add(r.Id, r.Value);
        }
    }

    /// <summary>A player action. Commands are plain data (serialisable for a future server) plus the rule that applies them.</summary>
    public interface ICommand<out TResult>
    {
        /// <summary>Validates, then mutates <see cref="CommandContext.State"/>. Throws <see cref="RuleViolation"/> before mutating if invalid.</summary>
        TResult Apply(CommandContext ctx);
    }

    // ------------------------------------------------------------------ new player

    public sealed class CreatePlayer : ICommand<PlayerState>
    {
        public ulong Seed;

        public PlayerState Apply(CommandContext ctx)
        {
            var start = ctx.Content.Start;
            var state = new PlayerState { CreatedMs = ctx.NowMs, RngSeed = Seed };
            state.Afk.LastClaimMs = ctx.NowMs;
            state.Village.LastSettleMs = ctx.NowMs;
            foreach (var c in start.Currencies)
                state.Add(c.Id, c.Value);
            for (var i = 0; i < start.Heroes.Count; i++)
            {
                var hero = ctx.Content.Hero(start.Heroes[i]);
                state.Heroes.Add(new OwnedHero { HeroId = hero.Id });
            }
            Formations.AutoFill(state, ctx.Content);
            ctx.State = state;
            return state;
        }
    }

    // ------------------------------------------------------------------ formation

    public sealed class SetFormation : ICommand<bool>
    {
        public string[] Slots = new string[5];
        public string Leader;

        public bool Apply(CommandContext ctx)
        {
            var state = ctx.State;
            var unlocked = Formations.UnlockedSlots(state, ctx.Content);
            ctx.Require(Slots != null && Slots.Length == 5, "bad_formation");
            var seen = new HashSet<string>();
            var count = 0;
            for (var i = 0; i < 5; i++)
            {
                var id = Slots[i];
                if (string.IsNullOrEmpty(id))
                    continue;
                ctx.Require(state.FindHero(id) != null, "hero_not_owned", id);
                ctx.Require(seen.Add(id), "hero_twice", id);
                ctx.Require(ctx.Content.Hero(id).Role != Role.HeroLeader, "leader_cannot_fight", id);
                count++;
            }
            ctx.Require(count > 0, "formation_empty");
            ctx.Require(count <= unlocked, "slots_locked");
            if (!string.IsNullOrEmpty(Leader))
            {
                ctx.Require(state.FindHero(Leader) != null, "hero_not_owned", Leader);
                ctx.Require(ctx.Content.Hero(Leader).Role == Role.HeroLeader, "not_a_leader", Leader);
            }

            state.Formation = (string[])Slots.Clone();
            state.Leader = string.IsNullOrEmpty(Leader) ? null : Leader;
            return true;
        }
    }

    // ------------------------------------------------------------------ campaign

    /// <summary>Starts the next campaign stage. The client simulates and plays it, then sends <see cref="FinishStageBattle"/>.</summary>
    public sealed class BeginStageBattle : ICommand<BattleSetup>
    {
        public BattleSetup Apply(CommandContext ctx)
        {
            var state = ctx.State;
            var stage = state.Campaign.HighestCleared + 1;
            ctx.Require(stage <= ctx.Content.TotalStages, "campaign_complete");
            ctx.Require(Formations.Count(state.Formation) > 0, "formation_empty");

            var seed = ((ulong)ctx.NextRng().NextUInt() << 32) | ctx.NextRng().NextUInt();
            state.Battle = new PendingBattle { Stage = stage, Seed = seed, Formation = (string[])state.Formation.Clone() };
            return Campaign.BuildBattle(state, ctx.Content, state.Battle);
        }
    }

    public sealed class StageResult
    {
        public BattleOutcome Outcome;
        public int Stage;
        public List<Amount> Rewards = new List<Amount>();
        /// <summary>Gear dropped by a boss stage.</summary>
        public List<GearItem> Gear = new List<GearItem>();
        /// <summary>Fragments dropped by a boss stage (once the Fragment Shrine stands).</summary>
        public List<FragmentItem> Fragments = new List<FragmentItem>();
        /// <summary>The Relic of a chapter boss, on its first defeat.</summary>
        public List<RelicItem> Relics = new List<RelicItem>();
        public int UnlockedSlots;
    }

    /// <summary>
    /// Ends the pending stage battle. The authority re-simulates it from the stored seed and the player's ultimate taps,
    /// so the result cannot be forged by the client.
    /// </summary>
    public sealed class FinishStageBattle : ICommand<StageResult>
    {
        public bool AutoUltimates = true;
        public List<int> AutoToggleTimesMs = new List<int>();
        public List<UltimateRequest> UltimateRequests = new List<UltimateRequest>();

        public StageResult Apply(CommandContext ctx)
        {
            var state = ctx.State;
            ctx.Require(state.Battle != null && state.Battle.HuntId == null && state.Battle.DungeonId == null, "no_battle");
            var pending = state.Battle;
            var setup = Campaign.BuildBattle(state, ctx.Content, pending);
            setup.AutoUltimates = AutoUltimates;
            setup.AutoToggleTimesMs.AddRange(AutoToggleTimesMs);
            setup.UltimateRequests.AddRange(UltimateRequests);
            var battle = BattleSimulator.Run(setup);

            var result = new StageResult { Outcome = battle.Outcome, Stage = pending.Stage };
            state.Battle = null;
            if (battle.Outcome == BattleOutcome.Victory)
            {
                result.Rewards.AddRange(Campaign.ClearRewards(ctx.Content, pending.Stage));
                var g = ctx.Content.Gear;
                if (g.MaterialsBase + g.MaterialsPerStage > 0)
                    result.Rewards.Add(new Amount(Mythwake.Core.Meta.Gear.Materials, g.MaterialsBase + g.MaterialsPerStage * pending.Stage));
                ctx.Grant(result.Rewards);
                Quests.Track(ctx, Stat.BattleWon);
                Quests.Track(ctx, Stat.StageCleared);
                if (Campaign.IsBoss(ctx.Content, pending.Stage) && g.Grades.Count > 0)
                    result.Gear.Add(Mythwake.Core.Meta.Gear.Drop(ctx));
                if (Campaign.IsBoss(ctx.Content, pending.Stage))
                    Mythwake.Core.Meta.Fragments.BossReward(ctx, pending.Stage, result.Rewards, result.Fragments);
                var (chapter, index, _) = Campaign.Locate(ctx.Content, pending.Stage);
                if (chapter != null && index == chapter.Stages && chapter.Boss != null)
                {
                    var relic = Mythwake.Core.Meta.Relics.OfBoss(ctx.Content, chapter.Boss);
                    if (relic != null)
                        result.Relics.Add(Mythwake.Core.Meta.Relics.Create(state, relic.Id));
                }
                state.Campaign.HighestCleared = pending.Stage;
            }
            result.UnlockedSlots = Formations.UnlockedSlots(state, ctx.Content);
            return result;
        }
    }

    // ------------------------------------------------------------------ idle rewards

    public sealed class ClaimAfkRewards : ICommand<List<Amount>>
    {
        public List<Amount> Apply(CommandContext ctx)
        {
            var rewards = Afk.Pending(ctx.State, ctx.Content, ctx.NowMs);
            ctx.Grant(rewards);
            ctx.State.Afk.LastClaimMs = ctx.NowMs;
            if (rewards.Count > 0)
                Quests.Track(ctx, Stat.AfkClaimed);
            return rewards;
        }
    }

    // ------------------------------------------------------------------ hero upgrades

    public sealed class LevelUpHero : ICommand<int>
    {
        public string HeroId;
        public int Levels = 1;

        public int Apply(CommandContext ctx)
        {
            var hero = ctx.State.FindHero(HeroId);
            ctx.Require(hero != null, "hero_not_owned", HeroId);
            ctx.Require(Levels >= 1 && Levels <= 100, "bad_levels");
            var cap = Progression.LevelCap(ctx.Content, hero);
            ctx.Require(hero.Level + Levels <= cap, "level_cap", cap.ToString());
            var costs = Progression.LevelCost(ctx.Content, hero.Level, Levels);
            ctx.RequireFunds(costs);
            ctx.Pay(costs);
            hero.Level += Levels;
            Quests.Track(ctx, Stat.HeroLevelUps, Levels);
            return hero.Level;
        }
    }

    public sealed class StarUpHero : ICommand<int>
    {
        public string HeroId;

        public int Apply(CommandContext ctx)
        {
            var hero = ctx.State.FindHero(HeroId);
            ctx.Require(hero != null, "hero_not_owned", HeroId);
            var def = ctx.Content.Hero(HeroId);
            var max = Progression.MaxStars(ctx.Content, def.Grade);
            ctx.Require(hero.Stars < max, "max_stars");
            var cost = new[] { Progression.StarCost(ctx.Content, hero.Stars, HeroId) };
            ctx.RequireFunds(cost);
            ctx.Pay(cost);
            hero.Stars++;
            return hero.Stars;
        }
    }

    // ------------------------------------------------------------------ summon

    public sealed class SummonResult
    {
        public string HeroId;
        public bool IsNew;
        /// <summary>Shards granted instead of the hero (duplicates).</summary>
        public int Shards;
    }

    public sealed class SummonHeroes : ICommand<List<SummonResult>>
    {
        public string BannerId;
        public int Count = 1;
        /// <summary>Pay with the alternative cost (gems) instead of Summon Scrolls.</summary>
        public bool UseAltCost;

        public List<SummonResult> Apply(CommandContext ctx)
        {
            ctx.Require(Count == 1 || Count == 10, "bad_count");
            ctx.Require(Village.SummonUnlocked(ctx.State, ctx.Content), "summon_locked");
            var banner = ctx.Content.Banner(BannerId);
            var unit = UseAltCost ? banner.AltCost : banner.Cost;
            ctx.Require(unit != null, "no_such_cost");
            // ten pulls with gems get a 10 % discount
            var total = unit.Value * Count;
            if (UseAltCost && Count == 10)
                total = total * 9 / 10;
            var cost = new[] { new Amount(unit.Id, total) };
            ctx.RequireFunds(cost);
            ctx.Pay(cost);
            var results = Summoning.Pull(ctx, banner, Count);
            Quests.Track(ctx, Stat.Summons, Count);
            return results;
        }
    }
}
