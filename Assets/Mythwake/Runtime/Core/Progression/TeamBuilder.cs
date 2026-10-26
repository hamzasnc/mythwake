using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Math;

namespace Mythwake.Core.Progression
{
    /// <summary>A hero as the player owns it: which definition, how far it is upgraded.</summary>
    public sealed class HeroProgress
    {
        public HeroDefinition Hero;
        public int Level = 1;
        public int Stars = 1;
        /// <summary>Flat stats from equipment (HP, ATK, DEF, crit rate), added before percentage bonuses.</summary>
        public Stats Flat;
    }

    /// <summary>Account-wide combat bonuses from outside the formation (village buildings, later relics and guild buffs), permille.</summary>
    public sealed class TeamBonus
    {
        public int Attack;
        public int Hp;
        public int HealPower;
    }

    /// <summary>Turns owned heroes + a formation into battle-ready <see cref="UnitSetup"/>s (level, stars, synergies, Hero Leader).</summary>
    public static class TeamBuilder
    {
        public const int MaxSlots = 5;
        public const int StarBonusPermille = 150;
        public const int RaceSynergyPermille = 30;

        /// <summary>Faction bonus (ATK and HP, permille) for N heroes of the same faction in one team (doc: 2 / 3 / 5).</summary>
        public static int FactionBonus(int count) => count >= 5 ? 150 : count >= 3 ? 100 : count >= 2 ? 50 : 0;

        public static Stats StatsAt(HeroDefinition hero, int level, int stars)
        {
            var s = hero.BaseStats.Clone();
            var growth = Permille.One + hero.GrowthPermille * (System.Math.Max(1, level) - 1);
            var star = Permille.One + StarBonusPermille * (System.Math.Max(1, stars) - 1);
            s.Hp = s.Hp * growth / Permille.One * star / Permille.One;
            s.Attack = s.Attack * growth / Permille.One * star / Permille.One;
            s.Defense = s.Defense * growth / Permille.One * star / Permille.One;
            return s;
        }

        /// <summary>Level/star stats plus flat equipment stats.</summary>
        public static Stats StatsOf(HeroProgress h)
        {
            var s = StatsAt(h.Hero, h.Level, h.Stars);
            if (h.Flat != null)
            {
                s.Hp += h.Flat.Hp;
                s.Attack += h.Flat.Attack;
                s.Defense += h.Flat.Defense;
                s.CritRate += h.Flat.CritRate;
            }
            return s;
        }

        /// <summary>Power rating shown in the UI. Not used by the rules.</summary>
        public static long Power(Stats s) => s.Attack * 5 + s.Hp / 2 + s.Defense * 3 + (s.CritRate + s.CritDamage / 2) * s.Attack / Permille.One;

        /// <param name="formation">Slot index (0,1 front; 2..4 back) → hero, null for empty slots.</param>
        /// <param name="leader">Optional Hero Leader: never fights, grants its aura to the whole team.</param>
        /// <param name="bonus">Optional account-wide bonuses (village).</param>
        public static List<UnitSetup> Build(IReadOnlyList<HeroProgress> formation, HeroProgress leader = null, TeamBonus bonus = null)
        {
            var factions = new Dictionary<Faction, int>();
            var races = new Dictionary<Race, int>();
            foreach (var h in formation)
            {
                if (h == null)
                    continue;
                factions.TryGetValue(h.Hero.Faction, out var f);
                factions[h.Hero.Faction] = f + 1;
                races.TryGetValue(h.Hero.Race, out var r);
                races[h.Hero.Race] = r + 1;
            }

            PassiveDefinition aura = null;
            var leaderScale = Permille.One;
            if (leader != null)
            {
                foreach (var p in leader.Hero.PassiveComponents())
                    if (p.Kind == PassiveKind.LeaderAura)
                        aura = p;
                // the aura grows with the leader's level and stars (doc: "depending on his level/rarity")
                leaderScale = Permille.One + 20 * (leader.Level - 1) + StarBonusPermille * (leader.Stars - 1);
            }

            var result = new List<UnitSetup>();
            for (var slot = 0; slot < formation.Count && slot < MaxSlots; slot++)
            {
                var h = formation[slot];
                if (h == null)
                    continue;
                var stats = StatsOf(h);
                var synergy = FactionBonus(factions[h.Hero.Faction]) + (races[h.Hero.Race] >= 2 ? RaceSynergyPermille : 0);
                var atk = synergy + (bonus?.Attack ?? 0);
                var hp = synergy + (bonus?.Hp ?? 0);
                stats.HealPower += bonus?.HealPower ?? 0;
                if (aura != null)
                {
                    atk += aura.AttackBonus * leaderScale / Permille.One;
                    hp += aura.HpBonus * leaderScale / Permille.One;
                    stats.HealPower += aura.HealBonus * leaderScale / Permille.One;
                }
                stats.Attack = stats.Attack * (Permille.One + atk) / Permille.One;
                stats.Hp = stats.Hp * (Permille.One + hp) / Permille.One;
                result.Add(new UnitSetup { Hero = h.Hero, Level = h.Level, Slot = slot, Stats = stats });
            }
            return result;
        }
    }
}
