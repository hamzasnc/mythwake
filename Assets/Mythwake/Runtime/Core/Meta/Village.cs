using System.Collections.Generic;
using Mythwake.Core.Content;
using Mythwake.Core.Math;
using Mythwake.Core.Progression;

namespace Mythwake.Core.Meta
{
    /// <summary>What a building does once built (doc: Building System).</summary>
    public enum BuildingEffect
    {
        /// <summary>The building's system is not in the game yet; shown as a ruin to rebuild later.</summary>
        None,
        /// <summary>Produces resources over time (Survivor Camp).</summary>
        Production,
        /// <summary>Improves healing of every hero (Healer's Tent).</summary>
        Healing,
        /// <summary>Unlocks hero recruitment (Summoning Gate).</summary>
        UnlockSummon,
        /// <summary>Unlocks gear crafting and upgrades; higher levels craft higher grades (Broken Forge).</summary>
        Forge,
        /// <summary>Unlocks the Hunt Board and Hunt Shop; higher levels add hunts, rewards and difficulty (Hunter's Hut).</summary>
        Hunting,
        /// <summary>Unlocks dungeons; higher levels open more dungeon types (Portal Chamber).</summary>
        Portal
    }

    public sealed class BuildingDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public BuildingEffect Effect;
        /// <summary>Highest campaign stage that must be cleared before the building can be (re)built.</summary>
        public int UnlockStage;
        public int StartLevel;
        public int MaxLevel = 1;
        /// <summary>Cost of the first level; each further level costs CostGrowthPermille / 1000 times the previous one.</summary>
        public List<Amount> BaseCost = new List<Amount>();
        public int CostGrowthPermille = 1450;
        /// <summary>Production per hour per building level (Production buildings).</summary>
        public List<Amount> ProductionPerLevel = new List<Amount>();
        /// <summary>Effect strength per building level, permille (e.g. healing).</summary>
        public int EffectPerLevel;
        /// <summary>Hero slots: entry i is the building level that opens slot i (doc: Village Style).</summary>
        public int[] SlotLevels = new int[0];
        /// <summary>Bonus per assigned hero, permille, before scaling by the hero's grade, stars and level.</summary>
        public int HeroBonus;
    }

    public sealed class VillageConfig
    {
        /// <summary>Production stops when the storage holds this many hours of output.</summary>
        public long StorageHours = 12;
        public List<BuildingDefinition> Buildings = new List<BuildingDefinition>();
    }

    public sealed class BuildingState
    {
        public int Level;
        /// <summary>Hero id per slot, null = empty.</summary>
        public string[] Heroes = new string[0];
    }

    public sealed class VillageState
    {
        public Dictionary<string, BuildingState> Buildings = new Dictionary<string, BuildingState>();
        /// <summary>Produced resources waiting to be collected.</summary>
        public Dictionary<string, long> Stored = new Dictionary<string, long>();
        public long LastSettleMs;
    }

    /// <summary>Dawnhollow: rebuilding, production, hero slots and the bonuses buildings grant.</summary>
    public static class Village
    {
        public static BuildingDefinition Building(GameContent content, string id)
        {
            foreach (var b in content.Village.Buildings)
                if (b.Id == id)
                    return b;
            throw new RuleViolation("unknown_building", id);
        }

        public static int Level(PlayerState state, BuildingDefinition def) =>
            state.Village.Buildings.TryGetValue(def.Id, out var b) ? b.Level : def.StartLevel;

        public static string[] Heroes(PlayerState state, BuildingDefinition def) =>
            state.Village.Buildings.TryGetValue(def.Id, out var b) ? b.Heroes : new string[0];

        public static int Slots(BuildingDefinition def, int level)
        {
            var n = 0;
            foreach (var l in def.SlotLevels)
                if (level >= l && level > 0)
                    n++;
            return n;
        }

        public static bool Unlocked(PlayerState state, BuildingDefinition def) =>
            def.Effect != BuildingEffect.None && state.Campaign.HighestCleared >= def.UnlockStage;

        public static List<Amount> UpgradeCost(BuildingDefinition def, int currentLevel)
        {
            var list = new List<Amount>();
            foreach (var c in def.BaseCost)
            {
                var v = c.Value;
                for (var i = 0; i < System.Math.Min(100, currentLevel - def.StartLevel); i++)
                    v = v * def.CostGrowthPermille / Permille.One;
                list.Add(new Amount(c.Id, v));
            }
            return list;
        }

        /// <summary>A hero's bonus in a building: stronger heroes help more (grade, stars, level).</summary>
        public static int HeroBonus(GameContent content, BuildingDefinition def, OwnedHero hero)
        {
            var grade = (int)content.Hero(hero.HeroId).Grade;
            var scale = Permille.One + 250 * grade + 100 * (hero.Stars - 1) + 5 * (hero.Level - 1);
            return def.HeroBonus * scale / Permille.One;
        }

        /// <summary>Building effect plus every assigned hero's bonus, permille.</summary>
        public static int TotalBonus(PlayerState state, GameContent content, BuildingDefinition def)
        {
            var level = Level(state, def);
            if (level <= 0)
                return 0;
            var total = def.EffectPerLevel * level;
            foreach (var id in Heroes(state, def))
            {
                var hero = string.IsNullOrEmpty(id) ? null : state.FindHero(id);
                if (hero != null)
                    total += HeroBonus(content, def, hero);
            }
            return total;
        }

        /// <summary>One building's output per hour, including the bonus of the heroes working there.</summary>
        public static Dictionary<string, long> ProductionOf(PlayerState state, GameContent content, BuildingDefinition def)
        {
            var rates = new Dictionary<string, long>();
            var level = Level(state, def);
            if (def.Effect != BuildingEffect.Production || level <= 0)
                return rates;
            var boost = Permille.One + TotalBonus(state, content, def) - def.EffectPerLevel * level;
            foreach (var p in def.ProductionPerLevel)
                rates[p.Id] = p.Value * level * boost / Permille.One;
            return rates;
        }

        public static Dictionary<string, long> ProductionPerHour(PlayerState state, GameContent content)
        {
            var rates = new Dictionary<string, long>();
            foreach (var def in content.Village.Buildings)
                foreach (var kv in ProductionOf(state, content, def))
                {
                    rates.TryGetValue(kv.Key, out var r);
                    rates[kv.Key] = r + kv.Value;
                }
            return rates;
        }

        /// <summary>Moves everything produced since the last settle into storage (capped). Call before any change to production.</summary>
        public static void Settle(PlayerState state, GameContent content, long nowMs)
        {
            var v = state.Village;
            var elapsed = System.Math.Max(0, nowMs - v.LastSettleMs);
            foreach (var kv in ProductionPerHour(state, content))
            {
                v.Stored.TryGetValue(kv.Key, out var stored);
                var cap = kv.Value * content.Village.StorageHours;
                var produced = kv.Value * elapsed / Afk.HourMs;
                v.Stored[kv.Key] = System.Math.Min(cap, stored + produced);
            }
            v.LastSettleMs = nowMs;
        }

        /// <summary>What Collect would give right now (storage plus unsettled production), without changing state.</summary>
        public static Dictionary<string, long> Pending(PlayerState state, GameContent content, long nowMs)
        {
            var v = state.Village;
            var elapsed = System.Math.Max(0, nowMs - v.LastSettleMs);
            var result = new Dictionary<string, long>(v.Stored);
            foreach (var kv in ProductionPerHour(state, content))
            {
                result.TryGetValue(kv.Key, out var stored);
                var cap = kv.Value * content.Village.StorageHours;
                result[kv.Key] = System.Math.Min(cap, stored + kv.Value * elapsed / Afk.HourMs);
            }
            return result;
        }

        public static TeamBonus CombatBonus(PlayerState state, GameContent content)
        {
            var bonus = new TeamBonus();
            foreach (var def in content.Village.Buildings)
                if (def.Effect == BuildingEffect.Healing)
                    bonus.HealPower += TotalBonus(state, content, def);
            return bonus;
        }

        public static bool SummonUnlocked(PlayerState state, GameContent content)
        {
            var any = false;
            foreach (var def in content.Village.Buildings)
            {
                if (def.Effect != BuildingEffect.UnlockSummon)
                    continue;
                any = true;
                if (Level(state, def) > 0)
                    return true;
            }
            return !any;
        }

        /// <summary>The building a hero works in, or null.</summary>
        public static string WorkplaceOf(PlayerState state, string heroId)
        {
            foreach (var kv in state.Village.Buildings)
                foreach (var h in kv.Value.Heroes)
                    if (h == heroId)
                        return kv.Key;
            return null;
        }

        internal static BuildingState Ensure(PlayerState state, BuildingDefinition def)
        {
            if (!state.Village.Buildings.TryGetValue(def.Id, out var b))
            {
                b = new BuildingState { Level = def.StartLevel };
                state.Village.Buildings[def.Id] = b;
            }
            var slots = Slots(def, b.Level);
            if (b.Heroes.Length < slots)
            {
                var grown = new string[slots];
                System.Array.Copy(b.Heroes, grown, b.Heroes.Length);
                b.Heroes = grown;
            }
            return b;
        }
    }

    // ------------------------------------------------------------------ commands

    public sealed class UpgradeBuilding : ICommand<int>
    {
        public string BuildingId;

        public int Apply(CommandContext ctx)
        {
            var def = Village.Building(ctx.Content, BuildingId);
            var state = ctx.State;
            ctx.Require(def.Effect != BuildingEffect.None, "building_planned");
            ctx.Require(state.Campaign.HighestCleared >= def.UnlockStage, "building_locked", def.UnlockStage.ToString());
            var level = Village.Level(state, def);
            ctx.Require(level < def.MaxLevel, "building_max_level");
            var cost = Village.UpgradeCost(def, level);
            ctx.RequireFunds(cost);

            // production so far is paid at the old rate
            Village.Settle(state, ctx.Content, ctx.NowMs);
            ctx.Pay(cost);
            var b = Village.Ensure(state, def);
            b.Level = level + 1;
            Village.Ensure(state, def);
            Quests.Track(ctx, Stat.BuildingUpgraded);
            return b.Level;
        }
    }

    public sealed class AssignVillageHero : ICommand<bool>
    {
        public string BuildingId;
        public int Slot;
        /// <summary>Null or empty clears the slot.</summary>
        public string HeroId;

        public bool Apply(CommandContext ctx)
        {
            var def = Village.Building(ctx.Content, BuildingId);
            var state = ctx.State;
            var level = Village.Level(state, def);
            ctx.Require(Slot >= 0 && Slot < Village.Slots(def, level), "slot_locked");
            if (!string.IsNullOrEmpty(HeroId))
                ctx.Require(state.FindHero(HeroId) != null, "hero_not_owned", HeroId);

            Village.Settle(state, ctx.Content, ctx.NowMs);
            // a hero works in one building at a time; moving them frees the old slot
            if (!string.IsNullOrEmpty(HeroId))
                foreach (var b in state.Village.Buildings.Values)
                    for (var i = 0; i < b.Heroes.Length; i++)
                        if (b.Heroes[i] == HeroId)
                            b.Heroes[i] = null;
            var building = Village.Ensure(state, def);
            building.Heroes[Slot] = string.IsNullOrEmpty(HeroId) ? null : HeroId;
            return true;
        }
    }

    public sealed class CollectVillage : ICommand<List<Amount>>
    {
        public List<Amount> Apply(CommandContext ctx)
        {
            var state = ctx.State;
            Village.Settle(state, ctx.Content, ctx.NowMs);
            var list = new List<Amount>();
            foreach (var kv in state.Village.Stored)
                if (kv.Value > 0)
                    list.Add(new Amount(kv.Key, kv.Value));
            list.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            ctx.Grant(list);
            state.Village.Stored.Clear();
            if (list.Count > 0)
                Quests.Track(ctx, Stat.VillageCollected);
            return list;
        }
    }
}
