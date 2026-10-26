using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Math;

namespace Mythwake.Core.Meta
{
    /// <summary>The Fragment of a dungeon boss (docs/systems/BOSS_FRAGMENTS.md).</summary>
    public sealed class BossFragmentDefinition
    {
        public string Id;
        public string Name;
        /// <summary>"{0}" is replaced by the scaled number.</summary>
        public string Description;
        public string Dungeon;
        /// <summary>Map-wide ability with a cooldown, cast from outside the field (optional).</summary>
        public SkillDefinition Ability;
        /// <summary>Passive bonus for every ally (optional), scaled by level.</summary>
        public List<FragmentComponent> TeamPassives = new List<FragmentComponent>();
        /// <summary>Growth per level above 1, permille of the level-1 value.</summary>
        public int LevelGrowthPermille = 120;
    }

    public sealed class BossFragmentConfig
    {
        public int EntriesPerDay = 7;
        public int MaxLevel = 10;
        public int CopyChancePermille = 200;
        /// <summary>A copy at the latest every N chests of a boss.</summary>
        public int CopyPity = 5;
        /// <summary>Upgrade from level L: L copies and this × L Fragment Dust.</summary>
        public long UpgradeDustPerLevel = 500;
        /// <summary>Chest rewards: base + perStage × refight level.</summary>
        public List<AfkRate> ChestRewards = new List<AfkRate>();
        public List<BossFragmentDefinition> Fragments = new List<BossFragmentDefinition>();
    }

    public sealed class BossFragmentProgress
    {
        public int Level;
        public int Copies;
        public int ChestsSinceCopy;
    }

    public sealed class BossFragmentState
    {
        public Dictionary<string, BossFragmentProgress> Owned = new Dictionary<string, BossFragmentProgress>();
        public string Equipped;
        public long Day = -1;
        public int EntriesUsed;
    }

    public sealed class BossChest
    {
        public BattleOutcome Outcome;
        public List<Amount> Rewards = new List<Amount>();
        public bool Copy;
        /// <summary>The copy unlocked the Boss Fragment.</summary>
        public bool Unlocked;
        public string FragmentId;
    }

    public static class BossFragments
    {
        public static BossFragmentDefinition Get(GameContent content, string id)
        {
            foreach (var f in content.BossFragments.Fragments)
                if (f.Id == id)
                    return f;
            throw new RuleViolation("unknown_boss_fragment", id);
        }

        public static BossFragmentProgress Progress(PlayerState state, string id) =>
            state.BossFragments.Owned.TryGetValue(id, out var p) ? p : null;

        public static int Level(PlayerState state, string id) => Progress(state, id)?.Level ?? 0;

        public static int EntriesLeft(PlayerState state, GameContent content, long nowMs)
        {
            var used = state.BossFragments.Day == Quests.DayIndex(content, nowMs) ? state.BossFragments.EntriesUsed : 0;
            return System.Math.Max(0, content.BossFragments.EntriesPerDay - used);
        }

        /// <summary>The boss can be refought once its dungeon's first level is cleared.</summary>
        public static bool RefightOpen(PlayerState state, GameContent content, BossFragmentDefinition f) =>
            Dungeons.Highest(state, f.Dungeon) > 0;

        public static long Scale(GameContent content, BossFragmentDefinition f, int level) =>
            Permille.One + (long)f.LevelGrowthPermille * System.Math.Max(0, level - 1);

        /// <summary>The headline number at a level: ability multiplier or first team passive value, permille.</summary>
        public static long MainValue(GameContent content, BossFragmentDefinition f, int level)
        {
            var scale = Scale(content, f, System.Math.Max(1, level));
            if (f.Ability != null && f.Ability.Effects.Count > 0)
                return f.Ability.Effects[0].Multiplier * scale / Permille.One;
            if (f.TeamPassives.Count > 0)
                return Fragments.Scaled(f.TeamPassives[0], scale).Value;
            return 0;
        }

        /// <summary>Adds the equipped Boss Fragment to a battle: its ability as an off-field caster, its passives on every ally.</summary>
        public static void Apply(BattleSetup setup, PlayerState state, GameContent content)
        {
            var id = state.BossFragments.Equipped;
            if (string.IsNullOrEmpty(id) || setup.Allies.Count == 0)
                return;
            var level = Level(state, id);
            if (level <= 0)
                return;
            var f = Get(content, id);
            var scale = Scale(content, f, level);
            foreach (var c in f.TeamPassives)
                foreach (var ally in setup.Allies)
                    ally.Passives.Add(Fragments.Scaled(c, scale));
            if (f.Ability == null)
                return;
            long attack = 1;
            foreach (var ally in setup.Allies)
                attack = System.Math.Max(attack, ally.Stats.Attack);
            var skill = Clone(f.Ability, scale);
            var dungeon = Dungeons.Get(content, f.Dungeon);
            var boss = content.Hero(dungeon.Boss);
            var hero = new HeroDefinition { Id = f.Id, Name = f.Name, Aspect = boss.Aspect, Role = Role.Support, Special = skill };
            setup.BossFragment = new UnitSetup { Hero = hero, Level = 1, Slot = -1, Stats = new Stats { Hp = 1, Attack = attack } };
        }

        private static SkillDefinition Clone(SkillDefinition s, long scale)
        {
            var copy = new SkillDefinition
            {
                Id = s.Id, Name = s.Name, Description = s.Description, Slot = SkillSlot.Special, Trigger = s.Trigger,
                Target = s.Target, TargetCount = s.TargetCount, Hits = s.Hits, HitTimesMs = s.HitTimesMs, DurationMs = s.DurationMs,
                Animation = s.Animation
            };
            foreach (var e in s.Effects)
            {
                var c = e.Clone();
                c.Multiplier = (int)(c.Multiplier * scale / Permille.One);
                copy.Effects.Add(c);
            }
            return copy;
        }

        public static List<Amount> UpgradeCost(GameContent content, int level) =>
            new List<Amount> { new Amount(Ids.FragmentDust, content.BossFragments.UpgradeDustPerLevel * level) };

        public static BattleSetup BuildRefight(PlayerState state, GameContent content, PendingBattle pending)
        {
            var f = Get(content, pending.BossFragmentId);
            var dungeon = Dungeons.Get(content, f.Dungeon);
            var setup = new BattleSetup
            {
                Seed = pending.Seed,
                Allies = Campaign.Allies(state, content, pending.Formation),
                Leader = Campaign.LeaderUnit(state, content),
                Enemies = Dungeons.Enemies(content, dungeon, pending.Floor)
            };
            Apply(setup, state, content);
            return setup;
        }
    }

    // ------------------------------------------------------------------ commands

    /// <summary>Refights a dungeon boss at the highest cleared level of its dungeon (uses an entry only when won).</summary>
    public sealed class BeginBossRefight : ICommand<BattleSetup>
    {
        public string FragmentId;

        public BattleSetup Apply(CommandContext ctx)
        {
            var state = ctx.State;
            var f = BossFragments.Get(ctx.Content, FragmentId);
            ctx.Require(BossFragments.RefightOpen(state, ctx.Content, f), "boss_refight_locked");
            ctx.Require(BossFragments.EntriesLeft(state, ctx.Content, ctx.NowMs) > 0, "boss_no_entries");
            ctx.Require(Formations.Count(state.Formation) > 0, "formation_empty");
            var seed = ((ulong)ctx.NextRng().NextUInt() << 32) | ctx.NextRng().NextUInt();
            state.Battle = new PendingBattle
            {
                Stage = state.Campaign.HighestCleared, BossFragmentId = f.Id, Floor = Dungeons.Highest(state, f.Dungeon), Seed = seed,
                Formation = (string[])state.Formation.Clone()
            };
            return BossFragments.BuildRefight(state, ctx.Content, state.Battle);
        }
    }

    public sealed class FinishBossRefight : ICommand<BossChest>
    {
        public bool AutoUltimates = true;
        public List<int> AutoToggleTimesMs = new List<int>();
        public List<UltimateRequest> UltimateRequests = new List<UltimateRequest>();

        public BossChest Apply(CommandContext ctx)
        {
            var state = ctx.State;
            var content = ctx.Content;
            ctx.Require(state.Battle != null && state.Battle.BossFragmentId != null, "no_battle");
            var pending = state.Battle;
            var setup = BossFragments.BuildRefight(state, content, pending);
            setup.AutoUltimates = AutoUltimates;
            setup.AutoToggleTimesMs.AddRange(AutoToggleTimesMs);
            setup.UltimateRequests.AddRange(UltimateRequests);
            var battle = BattleSimulator.Run(setup);
            state.Battle = null;

            var chest = new BossChest { Outcome = battle.Outcome, FragmentId = pending.BossFragmentId };
            if (battle.Outcome != BattleOutcome.Victory)
                return chest;
            var s = state.BossFragments;
            var day = Quests.DayIndex(content, ctx.NowMs);
            if (s.Day != day)
            {
                s.Day = day;
                s.EntriesUsed = 0;
            }
            ctx.Require(s.EntriesUsed < content.BossFragments.EntriesPerDay, "boss_no_entries");
            s.EntriesUsed++;

            foreach (var r in content.BossFragments.ChestRewards)
                chest.Rewards.Add(new Amount(r.Id, r.At(pending.Floor)));
            ctx.Grant(chest.Rewards);

            if (!s.Owned.TryGetValue(chest.FragmentId, out var p))
                s.Owned[chest.FragmentId] = p = new BossFragmentProgress();
            p.ChestsSinceCopy++;
            var cfg = content.BossFragments;
            if (p.ChestsSinceCopy >= cfg.CopyPity || ctx.NextRng().Range(Permille.One) < cfg.CopyChancePermille)
            {
                p.ChestsSinceCopy = 0;
                chest.Copy = true;
                if (p.Level == 0)
                {
                    p.Level = 1;
                    chest.Unlocked = true;
                }
                else
                    p.Copies++;
            }
            Quests.Track(ctx, Stat.BattleWon);
            Quests.Track(ctx, Stat.BossRefight);
            return chest;
        }
    }

    public sealed class UpgradeBossFragment : ICommand<int>
    {
        public string FragmentId;

        public int Apply(CommandContext ctx)
        {
            BossFragments.Get(ctx.Content, FragmentId);
            var p = BossFragments.Progress(ctx.State, FragmentId);
            ctx.Require(p != null && p.Level > 0, "boss_fragment_locked");
            ctx.Require(p.Level < ctx.Content.BossFragments.MaxLevel, "boss_fragment_max_level");
            ctx.Require(p.Copies >= p.Level, "boss_fragment_copies");
            var cost = BossFragments.UpgradeCost(ctx.Content, p.Level);
            ctx.RequireFunds(cost);
            ctx.Pay(cost);
            p.Copies -= p.Level;
            p.Level++;
            return p.Level;
        }
    }

    /// <summary>Equips a Boss Fragment for the account (doc: one at a time); null unequips.</summary>
    public sealed class EquipBossFragment : ICommand<bool>
    {
        public string FragmentId;

        public bool Apply(CommandContext ctx)
        {
            if (!string.IsNullOrEmpty(FragmentId))
            {
                BossFragments.Get(ctx.Content, FragmentId);
                ctx.Require(BossFragments.Level(ctx.State, FragmentId) > 0, "boss_fragment_locked");
            }
            ctx.State.BossFragments.Equipped = string.IsNullOrEmpty(FragmentId) ? null : FragmentId;
            return true;
        }
    }
}
