using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using NUnit.Framework;
using static Mythwake.Core.Tests.TestHeroes;

namespace Mythwake.Core.Tests
{
    /// <summary>Statuses and passive components used by the hero kits.</summary>
    public sealed class MechanicTests
    {
        private static int EnemyIndex(BattleSetup s, int enemy) => s.Allies.Count + enemy;

        [Test]
        public void ProvokeForcesTheVictimToAttackTheSource()
        {
            var tank = Fighter("tank", hp: 200_000);
            tank.Special = new SkillDefinition
            {
                Id = "challenge", Slot = SkillSlot.Special, Trigger = { Kind = TriggerKind.BattleStart }, Target = TargetSelector.EnemyFront,
                HitTimesMs = new[] { 0 },
                Effects = { new EffectDefinition { Kind = EffectKind.ApplyStatus, Status = new StatusDefinition { Kind = StatusKind.Provoke, DurationMs = 60_000 } } }
            };
            // the tank stands in the back; the provoked enemy must still hit it, not the front ally
            var setup = Setup(21, new[] { Unit(Fighter("front", hp: 200_000), 0), Unit(tank, 2) }, new[] { Unit(Fighter("e"), 0) });
            setup.Rules.MaxDurationMs = 6_000;
            var result = BattleSimulator.Run(setup);
            var hits = result.Events.Where(e => e.Type == BattleEventType.Hit && e.Source == EnemyIndex(setup, 0) && e.Skill == SkillSlot.Normal).ToList();
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits.All(e => e.Target == 1), Is.True);
        }

        [Test]
        public void EvadeDodgesTheNextHits()
        {
            var dodger = Fighter("dodger", hp: 200_000);
            dodger.Special = new SkillDefinition
            {
                Id = "mirror", Slot = SkillSlot.Special, Trigger = { Kind = TriggerKind.BattleStart }, Target = TargetSelector.Self,
                HitTimesMs = new[] { 0 },
                Effects = { new EffectDefinition { Kind = EffectKind.ApplyStatus, Status = new StatusDefinition { Kind = StatusKind.Evade, DurationMs = 60_000, Value = 2 } } }
            };
            var setup = Setup(22, new[] { Unit(dodger, 0) }, new[] { Unit(Fighter("e"), 0) });
            setup.Rules.MaxDurationMs = 8_000;
            var result = BattleSimulator.Run(setup);
            var incoming = result.Events.Where(e => e.Target == 0 && (e.Type == BattleEventType.Hit || e.Type == BattleEventType.Dodge)).ToList();
            Assert.That(incoming[0].Type, Is.EqualTo(BattleEventType.Dodge));
            Assert.That(incoming[1].Type, Is.EqualTo(BattleEventType.Dodge));
            Assert.That(incoming.Skip(2).Any(e => e.Type == BattleEventType.Hit), Is.True);
        }

        [Test]
        public void TeamAuraBoostsEveryAlly()
        {
            var bard = Fighter("bard", hp: 1);
            bard.Passive = new SkillDefinition
            {
                Id = "song", Slot = SkillSlot.Passive, Trigger = { Kind = TriggerKind.Passive },
                Passives = { new PassiveDefinition { Kind = PassiveKind.DamageBonus, Team = true, Value = 1000 } }
            };
            long FirstHit(HeroDefinition support)
            {
                var allies = support == null ? new[] { Unit(Fighter("a"), 0) } : new[] { Unit(Fighter("a"), 0), Unit(support, 2) };
                var setup = Setup(23, allies, new[] { Unit(Fighter("e", hp: 10_000_000, atk: 0), 0) });
                setup.Rules.MaxDurationMs = 2_000;
                return BattleSimulator.Run(setup).Events.First(e => e.Type == BattleEventType.Hit && e.Source == 0).Value;
            }
            Assert.That(FirstHit(bard), Is.GreaterThan(FirstHit(null) * 18 / 10));
        }

        [Test]
        public void OnHitDelayPushesTheTargetsNextAttack()
        {
            var inker = Fighter("inker");
            inker.Passive = new SkillDefinition
            {
                Id = "ink", Slot = SkillSlot.Passive, Trigger = { Kind = TriggerKind.Passive },
                Passives = { new PassiveDefinition { Kind = PassiveKind.OnHit, Effect = new EffectDefinition { Kind = EffectKind.DelayAction, Multiplier = 2_000 } } }
            };
            int EnemyAttacks(HeroDefinition hero)
            {
                var setup = Setup(24, new[] { Unit(hero, 0) }, new[] { Unit(Fighter("e", hp: 10_000_000, atk: 1), 0) });
                setup.Rules.MaxDurationMs = 15_000;
                return BattleSimulator.Run(setup).Events.Count(e => e.Type == BattleEventType.ActionStart && e.Source == 1 && e.Skill == SkillSlot.Normal);
            }
            Assert.That(EnemyAttacks(inker), Is.LessThan(EnemyAttacks(Fighter("plain")) / 2));
        }

        [Test]
        public void TargetCountPicksSeveralLowestHpUnits()
        {
            var archer = Fighter("archer");
            archer.Special = new SkillDefinition
            {
                Id = "double", Slot = SkillSlot.Special, Trigger = { Kind = TriggerKind.BattleStart }, Target = TargetSelector.EnemyLowestHp,
                TargetCount = 2, HitTimesMs = new[] { 0 },
                Effects = { new EffectDefinition { Kind = EffectKind.Damage, Multiplier = 100 } }
            };
            var setup = Setup(25, new[] { Unit(archer, 2) },
                new[] { Unit(Fighter("e0", hp: 100_000, atk: 0), 0), Unit(Fighter("e1", hp: 100_000, atk: 0), 1), Unit(Fighter("e2", hp: 100_000, atk: 0), 2) });
            var result = BattleSimulator.Run(setup);
            var first = result.Events.Where(e => e.Type == BattleEventType.Hit && e.Source == 0 && e.Skill == SkillSlot.Special).ToList();
            Assert.That(first.Select(e => e.Target).Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void HitPortionsAddUpToTheWholeSkill()
        {
            foreach (var hits in new[] { 1, 2, 3, 4, 5, 6 })
            {
                var total = Enumerable.Range(0, hits).Sum(i => BattleSimulator.HitPortion(i, hits));
                Assert.That(total, Is.InRange(990, 1000), $"{hits} hits");
            }
        }
    }
}
