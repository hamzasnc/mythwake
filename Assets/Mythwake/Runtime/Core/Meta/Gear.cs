using System.Collections.Generic;
using Mythwake.Core.Content;
using Mythwake.Core.Math;

namespace Mythwake.Core.Meta
{
    /// <summary>Gear slots from the design doc. Relics are a system of their own.</summary>
    public enum GearSlot { Weapon, Armor, Boots, Accessory }

    /// <summary>One gear piece the player owns.</summary>
    public sealed class GearItem
    {
        public int Uid;
        public GearSlot Slot;
        public Grade Grade;
        /// <summary>Set = one of the doc's factions; wearing all four pieces of one set grants the set bonus.</summary>
        public Faction Set;
        /// <summary>Enhance level (0 = fresh).</summary>
        public int Level;
        /// <summary>Hero wearing it, or null.</summary>
        public string EquippedBy;
    }

    public sealed class GearGradeStats
    {
        /// <summary>Main stat at enhance level 0, per slot (Weapon: ATK, Armor: HP, Boots: DEF, Accessory: crit rate permille).</summary>
        public int Weapon;
        public int Armor;
        public int Boots;
        public int Accessory;
        public int MaxLevel;
        /// <summary>Set bonus (all four pieces) when this is the lowest grade in the set, permille of all gear stats.</summary>
        public int SetBonus;
        /// <summary>Gear materials returned when salvaged.</summary>
        public int Salvage;
    }

    public sealed class CraftTier
    {
        public int ForgeLevel;
        /// <summary>Grade weights (Common..Legendary).</summary>
        public int[] Weights = new int[0];
    }

    public sealed class GearConfig
    {
        /// <summary>Index = grade (Common..Legendary).</summary>
        public List<GearGradeStats> Grades = new List<GearGradeStats>();
        /// <summary>Each enhance level adds this permille of the base value.</summary>
        public int LevelGrowthPermille = 100;
        /// <summary>Enhance cost from level L: base × (L+1).</summary>
        public List<Amount> EnhanceCostBase = new List<Amount>();
        public List<Amount> CraftCost = new List<Amount>();
        public List<CraftTier> CraftTiers = new List<CraftTier>();
        /// <summary>Gear materials per first clear (per stage) and a guaranteed gear drop on boss stages.</summary>
        public long MaterialsPerStage = 2;
        public long MaterialsBase = 10;
        public int[] BossDropWeights = { 300, 450, 220, 30 };
    }

    public sealed class GearStats
    {
        public long Hp;
        public long Attack;
        public long Defense;
        public int CritRate;
        public bool SetBonusActive;
    }

    public static class Gear
    {
        public const string Materials = "gearMaterials";

        public static GearGradeStats GradeStats(GameContent content, Grade grade)
        {
            var list = content.Gear.Grades;
            return list[System.Math.Min((int)grade, list.Count - 1)];
        }

        public static long MainValue(GameContent content, GearItem item)
        {
            var g = GradeStats(content, item.Grade);
            long baseValue = item.Slot switch
            {
                GearSlot.Weapon => g.Weapon,
                GearSlot.Armor => g.Armor,
                GearSlot.Boots => g.Boots,
                _ => g.Accessory
            };
            return baseValue * (Permille.One + content.Gear.LevelGrowthPermille * item.Level) / Permille.One;
        }

        public static GearItem Find(PlayerState state, int uid)
        {
            foreach (var g in state.Gear)
                if (g.Uid == uid)
                    return g;
            return null;
        }

        public static GearItem Equipped(PlayerState state, string heroId, GearSlot slot)
        {
            foreach (var g in state.Gear)
                if (g.EquippedBy == heroId && g.Slot == slot)
                    return g;
            return null;
        }

        /// <summary>Flat stats a hero gets from worn gear, including the set bonus.</summary>
        public static GearStats StatsFor(PlayerState state, GameContent content, string heroId)
        {
            var s = new GearStats();
            var pieces = new List<GearItem>(4);
            foreach (var g in state.Gear)
                if (g.EquippedBy == heroId)
                    pieces.Add(g);
            foreach (var g in pieces)
            {
                var v = MainValue(content, g);
                switch (g.Slot)
                {
                    case GearSlot.Weapon: s.Attack += v; break;
                    case GearSlot.Armor: s.Hp += v; break;
                    case GearSlot.Boots: s.Defense += v; break;
                    default: s.CritRate += (int)v; break;
                }
            }
            // doc: all equipment from one set → bonus on all gear stats, scaling with rarity (the lowest piece counts)
            if (pieces.Count == 4 && pieces.TrueForAll(p => p.Set == pieces[0].Set))
            {
                var lowest = pieces[0].Grade;
                foreach (var p in pieces)
                    if (p.Grade < lowest)
                        lowest = p.Grade;
                var bonus = Permille.One + GradeStats(content, lowest).SetBonus;
                s.Hp = s.Hp * bonus / Permille.One;
                s.Attack = s.Attack * bonus / Permille.One;
                s.Defense = s.Defense * bonus / Permille.One;
                s.CritRate = (int)(s.CritRate * bonus / Permille.One);
                s.SetBonusActive = true;
            }
            return s;
        }

        public static List<Amount> EnhanceCost(GameContent content, GearItem item)
        {
            var list = new List<Amount>();
            foreach (var c in content.Gear.EnhanceCostBase)
                list.Add(new Amount(c.Id, c.Value * (item.Level + 1) * (1 + (int)item.Grade)));
            return list;
        }

        /// <summary>Highest-tier craft weights for the forge level, shifted towards better grades by forge heroes.</summary>
        public static int[] CraftWeights(PlayerState state, GameContent content)
        {
            var forge = ForgeDefinition(content);
            var level = forge == null ? 0 : Village.Level(state, forge);
            int[] weights = null;
            foreach (var t in content.Gear.CraftTiers)
                if (level >= t.ForgeLevel)
                    weights = t.Weights;
            if (weights == null)
                return null;
            var result = (int[])weights.Clone();
            var heroBonus = forge == null ? 0 : Village.TotalBonus(state, content, forge) - forge.EffectPerLevel * level;
            for (var g = 1; g < result.Length; g++)
                result[g] = result[g] * (Permille.One + heroBonus) / Permille.One;
            return result;
        }

        public static BuildingDefinition ForgeDefinition(GameContent content)
        {
            foreach (var b in content.Village.Buildings)
                if (b.Effect == BuildingEffect.Forge)
                    return b;
            return null;
        }

        internal static Grade Roll(Pcg32 rng, int[] weights)
        {
            var total = 0;
            foreach (var w in weights)
                total += w;
            var roll = rng.Range(System.Math.Max(1, total));
            for (var g = 0; g < weights.Length; g++)
            {
                if (roll < weights[g])
                    return (Grade)g;
                roll -= weights[g];
            }
            return Grade.Common;
        }

        public static GearItem Create(PlayerState state, GearSlot slot, Grade grade, Faction set)
        {
            var item = new GearItem { Uid = ++state.GearUidCounter, Slot = slot, Grade = grade, Set = set };
            state.Gear.Add(item);
            return item;
        }

        /// <summary>Random piece for a boss drop (deterministic from the player's RNG stream).</summary>
        internal static GearItem Drop(CommandContext ctx)
        {
            var rng = ctx.NextRng();
            var slot = (GearSlot)rng.Range(4);
            var set = (Faction)rng.Range(6);
            return Create(ctx.State, slot, Roll(rng, ctx.Content.Gear.BossDropWeights), set);
        }
    }

    // ------------------------------------------------------------------ commands

    public sealed class EquipGear : ICommand<bool>
    {
        public string HeroId;
        public int Uid;

        public bool Apply(CommandContext ctx)
        {
            var state = ctx.State;
            ctx.Require(state.FindHero(HeroId) != null, "hero_not_owned", HeroId);
            var item = Gear.Find(state, Uid);
            ctx.Require(item != null, "unknown_gear");
            // the piece in that slot goes back to the bag; an item worn by someone else moves over
            var current = Gear.Equipped(state, HeroId, item.Slot);
            if (current != null)
                current.EquippedBy = null;
            item.EquippedBy = HeroId;
            return true;
        }
    }

    public sealed class UnequipGear : ICommand<bool>
    {
        public int Uid;

        public bool Apply(CommandContext ctx)
        {
            var item = Gear.Find(ctx.State, Uid);
            ctx.Require(item != null, "unknown_gear");
            item.EquippedBy = null;
            return true;
        }
    }

    public sealed class EnhanceGear : ICommand<int>
    {
        public int Uid;

        public int Apply(CommandContext ctx)
        {
            var item = Gear.Find(ctx.State, Uid);
            ctx.Require(item != null, "unknown_gear");
            ctx.Require(item.Level < Gear.GradeStats(ctx.Content, item.Grade).MaxLevel, "gear_max_level");
            var cost = Gear.EnhanceCost(ctx.Content, item);
            ctx.RequireFunds(cost);
            ctx.Pay(cost);
            item.Level++;
            return item.Level;
        }
    }

    public sealed class SalvageGear : ICommand<long>
    {
        public int Uid;

        public long Apply(CommandContext ctx)
        {
            var item = Gear.Find(ctx.State, Uid);
            ctx.Require(item != null, "unknown_gear");
            ctx.Require(item.EquippedBy == null, "gear_equipped");
            var g = Gear.GradeStats(ctx.Content, item.Grade);
            // part of the enhance materials come back
            long refund = g.Salvage;
            foreach (var c in ctx.Content.Gear.EnhanceCostBase)
                if (c.Id == Gear.Materials)
                    for (var l = 0; l < item.Level; l++)
                        refund += c.Value * (l + 1) * (1 + (int)item.Grade) / 2;
            ctx.State.Gear.Remove(item);
            ctx.State.Add(Gear.Materials, refund);
            return refund;
        }
    }

    /// <summary>Broken Forge: craft a piece for the chosen slot. Grade odds depend on the forge level and its heroes.</summary>
    public sealed class CraftGear : ICommand<GearItem>
    {
        public GearSlot Slot;

        public GearItem Apply(CommandContext ctx)
        {
            var weights = Gear.CraftWeights(ctx.State, ctx.Content);
            ctx.Require(weights != null, "forge_locked");
            ctx.RequireFunds(ctx.Content.Gear.CraftCost);
            ctx.Pay(ctx.Content.Gear.CraftCost);
            var rng = ctx.NextRng();
            var grade = Gear.Roll(rng, weights);
            var set = (Faction)rng.Range(6);
            return Gear.Create(ctx.State, Slot, grade, set);
        }
    }
}
