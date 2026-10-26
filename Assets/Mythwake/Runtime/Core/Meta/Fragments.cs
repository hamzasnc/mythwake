using System.Collections.Generic;
using Mythwake.Core.Content;
using Mythwake.Core.Math;

namespace Mythwake.Core.Meta
{
    /// <summary>Fragment slots from the design doc: Core (stats), Aspect (combat behaviour), Echo (passives, team synergies).</summary>
    public enum FragmentSlot { Core, Aspect, Echo }

    /// <summary>One Fragment the player owns (docs/systems/FRAGMENTS.md).</summary>
    public sealed class FragmentItem
    {
        public int Uid;
        public FragmentSlot Slot;
        public Aspect Aspect;
        public Grade Grade;
        public int Level;
        /// <summary>Effect id for Aspect and Echo Fragments; null for Core Fragments.</summary>
        public string Effect;
        /// <summary>Hero wearing it, or null.</summary>
        public string EquippedBy;
    }

    /// <summary>Which number of a passive component grows with the Fragment's grade and level.</summary>
    public enum FragmentScale { Value, Multiplier, StatusValue, Chance, Growth }

    public sealed class FragmentComponent
    {
        public PassiveDefinition Passive;
        public FragmentScale Scale;
    }

    public sealed class FragmentEffectDefinition
    {
        public string Id;
        public FragmentSlot Slot;
        /// <summary>Aspect Fragment effects belong to one Aspect; Echo effects to any.</summary>
        public Aspect? Aspect;
        public string Name;
        /// <summary>"{0}" is replaced by the scaled main number in percent.</summary>
        public string Description;
        public List<FragmentComponent> Components = new List<FragmentComponent>();
        /// <summary>Doc: "we can also add drawbacks". Never scaled.</summary>
        public List<PassiveDefinition> Drawbacks = new List<PassiveDefinition>();
    }

    public sealed class FragmentGradeStats
    {
        /// <summary>Effect multiplier of this grade, permille (Common = 1000).</summary>
        public int Factor = 1000;
        public int MaxLevel;
        /// <summary>Fragment Dust returned when dissolved at level 0.</summary>
        public long Dissolve;
    }

    public sealed class FragmentConfig
    {
        /// <summary>Index = grade (Common..Legendary).</summary>
        public List<FragmentGradeStats> Grades = new List<FragmentGradeStats>();
        /// <summary>Each level adds this permille of the grade value.</summary>
        public int LevelGrowthPermille = 60;
        /// <summary>Core Fragment at Common level 0: ATK and HP permille.</summary>
        public int CoreAttack = 30;
        public int CoreHp = 30;
        /// <summary>Core Fragment of the hero's own Aspect, permille multiplier.</summary>
        public int MatchingAspectPermille = 1500;
        /// <summary>Shrine level that opens each slot (Core, Aspect, Echo).</summary>
        public int[] SlotShrineLevels = { 1, 3, 5 };
        /// <summary>Upgrade cost from level L: base × (L+1) × (grade+1).</summary>
        public List<Amount> UpgradeCostBase = new List<Amount>();
        public List<Amount> StabilizeCost = new List<Amount>();
        public List<CraftTier> StabilizeTiers = new List<CraftTier>();
        public int FuseCount = 3;
        /// <summary>Campaign boss stages once the Shrine stands: Fragment Dust base + perStage × stage and one Fragment.</summary>
        public long BossDustBase = 50;
        public long BossDustPerStage = 2;
        public int[] DropWeights = { 550, 330, 100, 20 };
        public List<FragmentEffectDefinition> Effects = new List<FragmentEffectDefinition>();
    }

    /// <summary>What a hero's equipped Fragments add before battle.</summary>
    public sealed class FragmentBonus
    {
        public int AttackPermille;
        public int HpPermille;
        public List<PassiveDefinition> Passives = new List<PassiveDefinition>();
    }

    public static class Fragments
    {
        public static FragmentGradeStats GradeStats(GameContent content, Grade grade)
        {
            var list = content.Fragments.Grades;
            return list[System.Math.Min((int)grade, list.Count - 1)];
        }

        /// <summary>Grade × level multiplier, permille.</summary>
        public static long Scale(GameContent content, FragmentItem item) =>
            (long)GradeStats(content, item.Grade).Factor * (Permille.One + content.Fragments.LevelGrowthPermille * item.Level) / Permille.One;

        public static FragmentEffectDefinition Effect(GameContent content, string id)
        {
            if (id == null)
                return null;
            foreach (var e in content.Fragments.Effects)
                if (e.Id == id)
                    return e;
            return null;
        }

        public static FragmentItem Find(PlayerState state, int uid)
        {
            foreach (var f in state.Fragments)
                if (f.Uid == uid)
                    return f;
            return null;
        }

        public static FragmentItem Equipped(PlayerState state, string heroId, FragmentSlot slot)
        {
            foreach (var f in state.Fragments)
                if (f.EquippedBy == heroId && f.Slot == slot)
                    return f;
            return null;
        }

        public static BuildingDefinition ShrineDefinition(GameContent content)
        {
            foreach (var b in content.Village.Buildings)
                if (b.Effect == BuildingEffect.Shrine)
                    return b;
            return null;
        }

        public static int ShrineLevel(PlayerState state, GameContent content)
        {
            var shrine = ShrineDefinition(content);
            return shrine == null ? 0 : Village.Level(state, shrine);
        }

        public static int SlotShrineLevel(GameContent content, FragmentSlot slot)
        {
            var levels = content.Fragments.SlotShrineLevels;
            return (int)slot < levels.Length ? levels[(int)slot] : int.MaxValue;
        }

        public static bool SlotOpen(PlayerState state, GameContent content, FragmentSlot slot) =>
            ShrineLevel(state, content) >= SlotShrineLevel(content, slot);

        /// <summary>Core Fragment stat bonus (ATK, HP permille) for a hero of <paramref name="heroAspect"/>.</summary>
        public static (int attack, int hp) CoreBonus(GameContent content, FragmentItem item, Aspect heroAspect)
        {
            var scale = Scale(content, item);
            if (item.Aspect == heroAspect)
                scale = scale * content.Fragments.MatchingAspectPermille / Permille.One;
            return ((int)(content.Fragments.CoreAttack * scale / Permille.One), (int)(content.Fragments.CoreHp * scale / Permille.One));
        }

        /// <summary>The battle components of an Aspect or Echo Fragment, scaled to its grade and level.</summary>
        public static List<PassiveDefinition> Passives(GameContent content, FragmentItem item, int boostPermille = 0)
        {
            var list = new List<PassiveDefinition>();
            var effect = Effect(content, item.Effect);
            if (effect == null)
                return list;
            var scale = Scale(content, item) * (Permille.One + boostPermille) / Permille.One;
            foreach (var c in effect.Components)
                list.Add(Scaled(c, scale));
            foreach (var d in effect.Drawbacks)
                list.Add(d.Clone());
            return list;
        }

        /// <summary>The effect's headline number at this grade and level (shown as "{0}" in its description), in permille.</summary>
        public static long MainValue(GameContent content, FragmentItem item)
        {
            if (item.Slot == FragmentSlot.Core)
                return CoreBonus(content, item, item.Aspect).attack;
            var effect = Effect(content, item.Effect);
            if (effect == null || effect.Components.Count == 0)
                return 0;
            var c = effect.Components[0];
            var p = Scaled(c, Scale(content, item));
            return c.Scale switch
            {
                FragmentScale.Multiplier => p.Effect.Multiplier,
                FragmentScale.StatusValue => p.Effect.Status.Value,
                FragmentScale.Chance => p.Chance,
                FragmentScale.Growth => p.Growth,
                _ => p.Value
            };
        }

        /// <summary>A component with its scaled number (shared with Relics).</summary>
        internal static PassiveDefinition Scaled(FragmentComponent c, long scale)
        {
            var p = c.Passive.Clone();
            int S(int v) => (int)(v * scale / Permille.One);
            switch (c.Scale)
            {
                case FragmentScale.Value:
                    p.Value = S(p.Value);
                    break;
                case FragmentScale.Multiplier:
                    p.Effect.Multiplier = S(p.Effect.Multiplier);
                    break;
                case FragmentScale.StatusValue:
                    p.Effect.Status.Value = S(p.Effect.Status.Value);
                    break;
                case FragmentScale.Chance:
                    // proc chance of on-hit / on-hit-taken components
                    p.Chance = System.Math.Min(Permille.One, S(p.Chance));
                    break;
                case FragmentScale.Growth:
                    p.Growth = S(p.Growth);
                    p.Cap = S(p.Cap);
                    break;
            }
            return p;
        }

        /// <summary>Everything a hero's equipped Fragments add. Slots the Shrine has not opened (yet) do nothing.</summary>
        /// <param name="boostPermille">Relic "boost Fragment effects": all numbers this much stronger.</param>
        public static FragmentBonus BonusFor(PlayerState state, GameContent content, string heroId, Aspect heroAspect, int boostPermille = 0)
        {
            var bonus = new FragmentBonus();
            if (content.Fragments.Grades.Count == 0)
                return bonus;
            foreach (var f in state.Fragments)
            {
                if (f.EquippedBy != heroId || !SlotOpen(state, content, f.Slot))
                    continue;
                if (f.Slot == FragmentSlot.Core)
                {
                    var (atk, hp) = CoreBonus(content, f, heroAspect);
                    bonus.AttackPermille += atk * (Permille.One + boostPermille) / Permille.One;
                    bonus.HpPermille += hp * (Permille.One + boostPermille) / Permille.One;
                }
                else
                    bonus.Passives.AddRange(Passives(content, f, boostPermille));
            }
            return bonus;
        }

        public static List<Amount> UpgradeCost(GameContent content, FragmentItem item)
        {
            var list = new List<Amount>();
            foreach (var c in content.Fragments.UpgradeCostBase)
                list.Add(new Amount(c.Id, c.Value * (item.Level + 1) * (1 + (int)item.Grade)));
            return list;
        }

        /// <summary>Fragment Dust spent on the levels of an item.</summary>
        public static long DustInLevels(GameContent content, FragmentItem item)
        {
            long dust = 0;
            foreach (var c in content.Fragments.UpgradeCostBase)
                if (c.Id == Ids.FragmentDust)
                    for (var l = 0; l < item.Level; l++)
                        dust += c.Value * (l + 1) * (1 + (int)item.Grade);
            return dust;
        }

        public static long DissolveValue(GameContent content, FragmentItem item) =>
            GradeStats(content, item.Grade).Dissolve + DustInLevels(content, item) / 2;

        /// <summary>Stabilize grade weights for the Shrine level, shifted towards better grades by Shrine heroes.</summary>
        public static int[] StabilizeWeights(PlayerState state, GameContent content)
        {
            var shrine = ShrineDefinition(content);
            var level = shrine == null ? 0 : Village.Level(state, shrine);
            int[] weights = null;
            foreach (var t in content.Fragments.StabilizeTiers)
                if (level >= t.ForgeLevel)
                    weights = t.Weights;
            if (weights == null)
                return null;
            var result = (int[])weights.Clone();
            var heroBonus = Village.TotalBonus(state, content, shrine) - shrine.EffectPerLevel * level;
            for (var g = 1; g < result.Length; g++)
                result[g] = result[g] * (Permille.One + heroBonus) / Permille.One;
            return result;
        }

        public static FragmentItem Create(PlayerState state, FragmentSlot slot, Aspect aspect, Grade grade, string effect)
        {
            var item = new FragmentItem { Uid = ++state.FragmentUidCounter, Slot = slot, Aspect = aspect, Grade = grade, Effect = effect };
            state.Fragments.Add(item);
            return item;
        }

        /// <summary>A random effect for a new Fragment of this slot and Aspect (null for Core).</summary>
        internal static string RollEffect(Pcg32 rng, GameContent content, FragmentSlot slot, Aspect aspect)
        {
            if (slot == FragmentSlot.Core)
                return null;
            var pool = new List<FragmentEffectDefinition>();
            foreach (var e in content.Fragments.Effects)
                if (e.Slot == slot && (slot == FragmentSlot.Echo || e.Aspect == aspect))
                    pool.Add(e);
            return pool.Count == 0 ? null : pool[rng.Range(pool.Count)].Id;
        }

        internal static FragmentItem Roll(CommandContext ctx, FragmentSlot slot, int[] weights)
        {
            var rng = ctx.NextRng();
            var aspect = (Aspect)rng.Range(5);
            var grade = Gear.Roll(rng, weights);
            return Create(ctx.State, slot, aspect, grade, RollEffect(rng, ctx.Content, slot, aspect));
        }

        /// <summary>Boss and dungeon drop: random slot, Aspect, effect; grade from the drop weights.</summary>
        internal static FragmentItem Drop(CommandContext ctx)
        {
            var slot = (FragmentSlot)ctx.NextRng().Range(3);
            return Roll(ctx, slot, ctx.Content.Fragments.DropWeights);
        }

        /// <summary>Campaign boss stage reward once the Shrine stands.</summary>
        internal static void BossReward(CommandContext ctx, int stage, List<Amount> rewards, List<FragmentItem> drops)
        {
            var f = ctx.Content.Fragments;
            if (f.Grades.Count == 0 || ShrineLevel(ctx.State, ctx.Content) <= 0)
                return;
            var dust = new Amount(Ids.FragmentDust, f.BossDustBase + f.BossDustPerStage * stage);
            ctx.State.Add(dust.Id, dust.Value);
            rewards.Add(dust);
            drops.Add(Drop(ctx));
        }
    }

    // ------------------------------------------------------------------ commands

    public sealed class EquipFragment : ICommand<bool>
    {
        public string HeroId;
        public int Uid;

        public bool Apply(CommandContext ctx)
        {
            var state = ctx.State;
            ctx.Require(state.FindHero(HeroId) != null, "hero_not_owned", HeroId);
            var item = Fragments.Find(state, Uid);
            ctx.Require(item != null, "unknown_fragment");
            ctx.Require(Fragments.SlotOpen(state, ctx.Content, item.Slot), "fragment_slot_locked");
            var current = Fragments.Equipped(state, HeroId, item.Slot);
            if (current != null)
                current.EquippedBy = null;
            item.EquippedBy = HeroId;
            return true;
        }
    }

    public sealed class UnequipFragment : ICommand<bool>
    {
        public int Uid;

        public bool Apply(CommandContext ctx)
        {
            var item = Fragments.Find(ctx.State, Uid);
            ctx.Require(item != null, "unknown_fragment");
            item.EquippedBy = null;
            return true;
        }
    }

    public sealed class UpgradeFragment : ICommand<int>
    {
        public int Uid;

        public int Apply(CommandContext ctx)
        {
            var item = Fragments.Find(ctx.State, Uid);
            ctx.Require(item != null, "unknown_fragment");
            ctx.Require(Fragments.ShrineLevel(ctx.State, ctx.Content) > 0, "shrine_locked");
            ctx.Require(item.Level < Fragments.GradeStats(ctx.Content, item.Grade).MaxLevel, "fragment_max_level");
            var cost = Fragments.UpgradeCost(ctx.Content, item);
            ctx.RequireFunds(cost);
            ctx.Pay(cost);
            item.Level++;
            Quests.Track(ctx, Stat.FragmentUpgraded);
            return item.Level;
        }
    }

    /// <summary>Doc: Fragments can be "fused into higher rarity versions". The first Fragment keeps its Aspect and effect.</summary>
    public sealed class FuseFragments : ICommand<FragmentItem>
    {
        public List<int> Uids = new List<int>();

        public FragmentItem Apply(CommandContext ctx)
        {
            var state = ctx.State;
            var content = ctx.Content;
            ctx.Require(Fragments.ShrineLevel(state, content) > 0, "shrine_locked");
            ctx.Require(Uids != null && Uids.Count == content.Fragments.FuseCount, "fuse_count");
            var items = new List<FragmentItem>();
            foreach (var uid in Uids)
            {
                var f = Fragments.Find(state, uid);
                ctx.Require(f != null, "unknown_fragment");
                ctx.Require(!items.Contains(f), "fuse_duplicate");
                ctx.Require(f.EquippedBy == null, "fragment_equipped");
                items.Add(f);
            }
            var main = items[0];
            foreach (var f in items)
                ctx.Require(f.Slot == main.Slot && f.Grade == main.Grade, "fuse_mismatch");
            ctx.Require((int)main.Grade + 1 < content.Fragments.Grades.Count, "fuse_max_grade");

            long refund = 0;
            foreach (var f in items)
            {
                refund += Fragments.DustInLevels(content, f) / 2;
                state.Fragments.Remove(f);
            }
            if (refund > 0)
                state.Add(Ids.FragmentDust, refund);
            return Fragments.Create(state, main.Slot, main.Aspect, main.Grade + 1, main.Effect);
        }
    }

    public sealed class DissolveFragment : ICommand<long>
    {
        public int Uid;

        public long Apply(CommandContext ctx)
        {
            var item = Fragments.Find(ctx.State, Uid);
            ctx.Require(item != null, "unknown_fragment");
            ctx.Require(item.EquippedBy == null, "fragment_equipped");
            var dust = Fragments.DissolveValue(ctx.Content, item);
            ctx.State.Fragments.Remove(item);
            ctx.State.Add(Ids.FragmentDust, dust);
            return dust;
        }
    }

    /// <summary>Fragment Shrine: stabilize raw Fragment energy into a Fragment of the chosen slot.</summary>
    public sealed class StabilizeFragment : ICommand<FragmentItem>
    {
        public FragmentSlot Slot;

        public FragmentItem Apply(CommandContext ctx)
        {
            var weights = Fragments.StabilizeWeights(ctx.State, ctx.Content);
            ctx.Require(weights != null, "shrine_locked");
            ctx.Require(Fragments.SlotOpen(ctx.State, ctx.Content, Slot), "fragment_slot_locked");
            ctx.RequireFunds(ctx.Content.Fragments.StabilizeCost);
            ctx.Pay(ctx.Content.Fragments.StabilizeCost);
            Quests.Track(ctx, Stat.FragmentStabilized);
            return Fragments.Roll(ctx, Slot, weights);
        }
    }
}
