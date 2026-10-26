using System.Collections.Generic;
using Mythwake.Core.Content;
using Mythwake.Core.Math;

namespace Mythwake.Core.Meta
{
    /// <summary>One Relic of the doc's bosses (docs/systems/RELICS.md). Rarity is fixed, the effect grows with level.</summary>
    public sealed class RelicDefinition
    {
        public string Id;
        public string Name;
        /// <summary>"{0}" is replaced by the scaled headline number.</summary>
        public string Description;
        public Grade Grade;
        /// <summary>Boss (monster id) the Relic belongs to; its first defeat in the campaign grants the Relic.</summary>
        public string Source;
        /// <summary>Battle components, scaled by level.</summary>
        public List<FragmentComponent> Components = new List<FragmentComponent>();
        /// <summary>Doc: "boost Fragment effects" – the wearer's Fragments get this permille stronger (scaled by level).</summary>
        public int FragmentBoost;
        /// <summary>Doc: "bonus rewards from certain areas" – this dungeon pays this permille more while the wearer fights in it.</summary>
        public string RewardDungeon;
        public int RewardBonus;
        /// <summary>The headline number is shown as it is (energy) instead of permille → percent.</summary>
        public bool Raw;
    }

    public sealed class RelicGradeStats
    {
        public int MaxLevel;
        /// <summary>Relic materials returned when a copy is dissolved at level 0.</summary>
        public long Dissolve;
    }

    public sealed class RelicConfig
    {
        /// <summary>Index = grade (Common..Legendary).</summary>
        public List<RelicGradeStats> Grades = new List<RelicGradeStats>();
        public int LevelGrowthPermille = 80;
        /// <summary>Fragment Shrine level that opens the Relic slot.</summary>
        public int ShrineLevel = 2;
        /// <summary>Upgrade cost from level L: base × (L+1) × (grade+1).</summary>
        public List<Amount> UpgradeCostBase = new List<Amount>();
        public List<RelicDefinition> Relics = new List<RelicDefinition>();
    }

    public sealed class RelicItem
    {
        public int Uid;
        public string RelicId;
        public int Level;
        public string EquippedBy;
    }

    public static class Relics
    {
        public static RelicDefinition Get(GameContent content, string id)
        {
            foreach (var r in content.Relics.Relics)
                if (r.Id == id)
                    return r;
            return null;
        }

        public static RelicGradeStats GradeStats(GameContent content, Grade grade)
        {
            var list = content.Relics.Grades;
            return list[System.Math.Min((int)grade, list.Count - 1)];
        }

        public static int MaxLevel(GameContent content, RelicItem item) => GradeStats(content, Get(content, item.RelicId).Grade).MaxLevel;

        /// <summary>Level multiplier, permille.</summary>
        public static long Scale(GameContent content, RelicItem item) => Permille.One + content.Relics.LevelGrowthPermille * item.Level;

        public static RelicItem Find(PlayerState state, int uid)
        {
            foreach (var r in state.Relics)
                if (r.Uid == uid)
                    return r;
            return null;
        }

        public static RelicItem Equipped(PlayerState state, string heroId)
        {
            foreach (var r in state.Relics)
                if (r.EquippedBy == heroId)
                    return r;
            return null;
        }

        public static bool SlotOpen(PlayerState state, GameContent content) =>
            content.Relics.Relics.Count > 0 && Fragments.ShrineLevel(state, content) >= content.Relics.ShrineLevel;

        public static List<PassiveDefinition> Passives(GameContent content, RelicItem item)
        {
            var list = new List<PassiveDefinition>();
            var def = Get(content, item.RelicId);
            if (def == null)
                return list;
            var scale = Scale(content, item);
            foreach (var c in def.Components)
                list.Add(Fragments.Scaled(c, scale));
            return list;
        }

        public static int FragmentBoost(GameContent content, RelicItem item)
        {
            var def = Get(content, item?.RelicId);
            return def == null ? 0 : (int)(def.FragmentBoost * Scale(content, item) / Permille.One);
        }

        public static int RewardBonus(GameContent content, RelicItem item, string dungeonId)
        {
            var def = Get(content, item?.RelicId);
            if (def == null || def.RewardDungeon != dungeonId)
                return 0;
            return (int)(def.RewardBonus * Scale(content, item) / Permille.One);
        }

        /// <summary>The headline number of the Relic at its level (first component, Fragment boost or reward bonus).</summary>
        public static long MainValue(GameContent content, RelicItem item)
        {
            var def = Get(content, item.RelicId);
            if (def == null)
                return 0;
            if (def.FragmentBoost > 0)
                return FragmentBoost(content, item);
            if (def.RewardBonus > 0)
                return def.RewardBonus * Scale(content, item) / Permille.One;
            if (def.Components.Count == 0)
                return 0;
            var c = def.Components[0];
            var p = Fragments.Scaled(c, Scale(content, item));
            return c.Scale switch
            {
                FragmentScale.Multiplier => p.Effect.Multiplier,
                FragmentScale.StatusValue => p.Effect.Status.Value,
                FragmentScale.Chance => p.Chance,
                FragmentScale.Growth => p.Growth,
                _ => p.Value
            };
        }

        /// <summary>Highest reward bonus for a dungeon among the heroes of a formation (only the wearer's own Relic counts).</summary>
        public static int TeamRewardBonus(PlayerState state, GameContent content, string[] formation, string dungeonId)
        {
            var best = 0;
            if (formation == null || !SlotOpen(state, content))
                return 0;
            foreach (var heroId in formation)
                if (!string.IsNullOrEmpty(heroId))
                    best = System.Math.Max(best, RewardBonus(content, Equipped(state, heroId), dungeonId));
            return best;
        }

        public static List<Amount> UpgradeCost(GameContent content, RelicItem item)
        {
            var grade = (int)Get(content, item.RelicId).Grade;
            var list = new List<Amount>();
            foreach (var c in content.Relics.UpgradeCostBase)
                list.Add(new Amount(c.Id, c.Value * (item.Level + 1) * (1 + grade)));
            return list;
        }

        public static long DissolveValue(GameContent content, RelicItem item)
        {
            var def = Get(content, item.RelicId);
            long materials = GradeStats(content, def.Grade).Dissolve;
            foreach (var c in content.Relics.UpgradeCostBase)
                if (c.Id == Ids.RelicMaterials)
                    for (var l = 0; l < item.Level; l++)
                        materials += c.Value * (l + 1) * (1 + (int)def.Grade) / 2;
            return materials;
        }

        public static RelicItem Create(PlayerState state, string relicId)
        {
            var item = new RelicItem { Uid = ++state.RelicUidCounter, RelicId = relicId };
            state.Relics.Add(item);
            return item;
        }

        /// <summary>The Relic of a boss, if any.</summary>
        public static RelicDefinition OfBoss(GameContent content, string bossId)
        {
            foreach (var r in content.Relics.Relics)
                if (r.Source == bossId)
                    return r;
            return null;
        }

        internal static RelicItem DropFrom(CommandContext ctx, List<string> pool)
        {
            if (pool == null || pool.Count == 0)
                return null;
            var id = pool[ctx.NextRng().Range(pool.Count)];
            return Get(ctx.Content, id) == null ? null : Create(ctx.State, id);
        }
    }

    // ------------------------------------------------------------------ commands

    public sealed class EquipRelic : ICommand<bool>
    {
        public string HeroId;
        public int Uid;

        public bool Apply(CommandContext ctx)
        {
            var state = ctx.State;
            ctx.Require(state.FindHero(HeroId) != null, "hero_not_owned", HeroId);
            var item = Relics.Find(state, Uid);
            ctx.Require(item != null, "unknown_relic");
            ctx.Require(Relics.SlotOpen(state, ctx.Content), "relic_slot_locked");
            var current = Relics.Equipped(state, HeroId);
            if (current != null)
                current.EquippedBy = null;
            item.EquippedBy = HeroId;
            return true;
        }
    }

    public sealed class UnequipRelic : ICommand<bool>
    {
        public int Uid;

        public bool Apply(CommandContext ctx)
        {
            var item = Relics.Find(ctx.State, Uid);
            ctx.Require(item != null, "unknown_relic");
            item.EquippedBy = null;
            return true;
        }
    }

    public sealed class UpgradeRelic : ICommand<int>
    {
        public int Uid;

        public int Apply(CommandContext ctx)
        {
            var item = Relics.Find(ctx.State, Uid);
            ctx.Require(item != null, "unknown_relic");
            ctx.Require(Relics.SlotOpen(ctx.State, ctx.Content), "relic_slot_locked");
            ctx.Require(item.Level < Relics.MaxLevel(ctx.Content, item), "relic_max_level");
            var cost = Relics.UpgradeCost(ctx.Content, item);
            ctx.RequireFunds(cost);
            ctx.Pay(cost);
            item.Level++;
            Quests.Track(ctx, Stat.RelicUpgraded);
            return item.Level;
        }
    }

    public sealed class DissolveRelic : ICommand<long>
    {
        public int Uid;

        public long Apply(CommandContext ctx)
        {
            var item = Relics.Find(ctx.State, Uid);
            ctx.Require(item != null, "unknown_relic");
            ctx.Require(item.EquippedBy == null, "relic_equipped");
            var materials = Relics.DissolveValue(ctx.Content, item);
            ctx.State.Relics.Remove(item);
            ctx.State.Add(Ids.RelicMaterials, materials);
            return materials;
        }
    }
}
