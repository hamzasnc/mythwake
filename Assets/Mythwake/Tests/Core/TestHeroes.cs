using System.Collections.Generic;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;

namespace Mythwake.Core.Tests
{
    /// <summary>Small hand-built heroes so rule tests do not depend on the content files.</summary>
    internal static class TestHeroes
    {
        public static HeroDefinition Fighter(string id, Aspect aspect = Aspect.Ravak, long hp = 10_000, long atk = 1_000, long def = 200,
            Faction faction = Faction.GuildsOfTheHunt, Race race = Race.Human)
        {
            return new HeroDefinition
            {
                Id = id, Name = id, Aspect = aspect, Faction = faction, Race = race, Role = Role.Dps,
                BaseStats = new Stats { Hp = hp, Attack = atk, Defense = def },
                Normal = new SkillDefinition
                {
                    Id = id + "_normal", Slot = SkillSlot.Normal, Trigger = { Kind = TriggerKind.AttackInterval },
                    Target = TargetSelector.EnemyFront, Effects = { new EffectDefinition { Kind = EffectKind.Damage, Multiplier = 1000 } }
                },
                Ultimate = new SkillDefinition
                {
                    Id = id + "_ult", Slot = SkillSlot.Ultimate, Trigger = { Kind = TriggerKind.Energy }, Animation = "ultimate",
                    Target = TargetSelector.EnemyAll, Hits = 3, HitTimesMs = new[] { 400, 600, 800 }, DurationMs = 1400,
                    Effects = { new EffectDefinition { Kind = EffectKind.Damage, Multiplier = 2500 } }
                }
            };
        }

        public static UnitSetup Unit(HeroDefinition hero, int slot) =>
            new UnitSetup { Hero = hero, Slot = slot, Stats = hero.BaseStats.Clone() };

        public static BattleSetup Setup(ulong seed, IEnumerable<UnitSetup> allies, IEnumerable<UnitSetup> enemies)
        {
            var s = new BattleSetup { Seed = seed };
            s.Allies.AddRange(allies);
            s.Enemies.AddRange(enemies);
            return s;
        }
    }
}
