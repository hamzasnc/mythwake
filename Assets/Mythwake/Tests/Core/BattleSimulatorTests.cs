using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using NUnit.Framework;
using static Mythwake.Core.Tests.TestHeroes;

namespace Mythwake.Core.Tests
{
    public sealed class BattleSimulatorTests
    {
        private static BattleSetup Duel(ulong seed, HeroDefinition a, HeroDefinition b) =>
            Setup(seed, new[] { Unit(a, 0) }, new[] { Unit(b, 0) });

        private static bool UltimateCast(BattleResult r, int source) =>
            r.Events.Any(e => e.Type == BattleEventType.ActionStart && e.Skill == SkillSlot.Ultimate && e.Source == source);

        [Test]
        public void SameSeedProducesIdenticalLog()
        {
            BattleSetup Make() => Setup(42,
                new[] { Unit(Fighter("a1"), 0), Unit(Fighter("a2", Aspect.Somnara), 2) },
                new[] { Unit(Fighter("e1", Aspect.Gorath), 0), Unit(Fighter("e2", Aspect.Envyra), 3) });

            var first = BattleSimulator.Run(Make());
            var second = BattleSimulator.Run(Make());

            Assert.That(second.Events.Count, Is.EqualTo(first.Events.Count));
            for (var i = 0; i < first.Events.Count; i++)
                Assert.That(second.Events[i].ToString(), Is.EqualTo(first.Events[i].ToString()), $"event {i}");
            Assert.That(second.Outcome, Is.EqualTo(first.Outcome));
        }

        [Test]
        public void DifferentSeedsChangeTheLog()
        {
            var a = BattleSimulator.Run(Duel(1, Fighter("a"), Fighter("b")));
            var b = BattleSimulator.Run(Duel(2, Fighter("a"), Fighter("b")));
            var same = a.Events.Select(e => e.ToString()).SequenceEqual(b.Events.Select(e => e.ToString()));
            Assert.That(same, Is.False);
        }

        [Test]
        public void StrongerTeamWins()
        {
            var result = BattleSimulator.Run(Duel(7, Fighter("strong", atk: 3000), Fighter("weak", atk: 300)));
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(result.Events.Last().Type, Is.EqualTo(BattleEventType.BattleEnd));
        }

        [Test]
        public void TimeoutCountsAsDefeat()
        {
            var setup = Duel(3, Fighter("a", hp: 10_000_000, atk: 1), Fighter("b", hp: 10_000_000, atk: 1));
            setup.Rules.MaxDurationMs = 5_000;
            var result = BattleSimulator.Run(setup);
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(result.DurationMs, Is.EqualTo(5_000));
        }

        [Test]
        public void AspectAdvantageDealsMoreDamage()
        {
            // Ravak beats Gorath, Envyra beats Ravak
            long DamageAgainst(Aspect defender)
            {
                var setup = Duel(11, Fighter("atk", Aspect.Ravak), Fighter("def", defender, hp: 10_000_000, atk: 1));
                setup.Rules.MaxDurationMs = 10_000;
                return BattleSimulator.Run(setup).Units[0].DamageDealt;
            }

            var neutral = DamageAgainst(Aspect.Somnara);
            Assert.That(DamageAgainst(Aspect.Gorath), Is.GreaterThan(neutral * 115 / 100));
            Assert.That(DamageAgainst(Aspect.Envyra), Is.LessThan(neutral * 90 / 100));
        }

        [Test]
        public void UltimateFiresAutomaticallyWhenEnergyIsFull()
        {
            var setup = Duel(5, Fighter("a"), Fighter("b", hp: 10_000_000, atk: 1));
            setup.Rules.MaxDurationMs = 30_000;
            Assert.That(UltimateCast(BattleSimulator.Run(setup), 0), Is.True);
        }

        [Test]
        public void ManualUltimateWaitsForTheTap()
        {
            var setup = Duel(5, Fighter("a"), Fighter("b", hp: 10_000_000, atk: 1));
            setup.Rules.MaxDurationMs = 30_000;
            setup.AutoUltimates = false;
            Assert.That(UltimateCast(BattleSimulator.Run(setup), 0), Is.False);

            setup.UltimateRequests.Add(new UltimateRequest(20_000, 0));
            var tapped = BattleSimulator.Run(setup);
            var ult = tapped.Events.First(e => e.Type == BattleEventType.ActionStart && e.Skill == SkillSlot.Ultimate && e.Source == 0);
            Assert.That(ult.TimeMs, Is.GreaterThanOrEqualTo(20_000));
        }

        [Test]
        public void ShieldAbsorbsDamageFirst()
        {
            var tank = Fighter("tank", hp: 50_000);
            tank.Special = new SkillDefinition
            {
                Id = "wall", Slot = SkillSlot.Special, Trigger = { Kind = TriggerKind.BattleStart }, Target = TargetSelector.Self,
                Effects = { new EffectDefinition { Kind = EffectKind.Shield, Multiplier = 100_000, ShieldDurationMs = 60_000 } }
            };
            var setup = Duel(9, tank, Fighter("b"));
            setup.Rules.MaxDurationMs = 8_000;
            var result = BattleSimulator.Run(setup);
            Assert.That(result.Events.Any(e => e.Type == BattleEventType.ShieldAbsorbed && e.Target == 0), Is.True);
            Assert.That(result.Units[0].DamageTaken, Is.EqualTo(0));
        }

        [Test]
        public void TauntPullsSingleTargetAttacks()
        {
            var taunter = Fighter("taunter", hp: 100_000);
            taunter.Special = new SkillDefinition
            {
                Id = "provoke", Slot = SkillSlot.Special, Trigger = { Kind = TriggerKind.BattleStart }, Target = TargetSelector.Self,
                Effects = { new EffectDefinition { Kind = EffectKind.ApplyStatus, Status = new StatusDefinition { Kind = StatusKind.Taunt, DurationMs = 60_000 } } }
            };
            // the taunter stands in the back row; without taunt the front-row ally would be hit
            var setup = Setup(4, new[] { Unit(Fighter("front", hp: 100_000), 0), Unit(taunter, 2) }, new[] { Unit(Fighter("e"), 0) });
            setup.Rules.MaxDurationMs = 6_000;
            var result = BattleSimulator.Run(setup);
            const int enemyIndex = 2;
            var hits = result.Events.Where(e => e.Type == BattleEventType.Hit && e.Source == enemyIndex && e.Skill == SkillSlot.Normal).ToList();
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits.All(e => e.Target == 1), Is.True);
        }

        [Test]
        public void StunStopsActions()
        {
            var stunner = Fighter("stunner");
            stunner.Special = new SkillDefinition
            {
                Id = "bash", Slot = SkillSlot.Special, Trigger = { Kind = TriggerKind.BattleStart }, Target = TargetSelector.EnemyFront,
                HitTimesMs = new[] { 0 },
                Effects = { new EffectDefinition { Kind = EffectKind.ApplyStatus, Status = new StatusDefinition { Kind = StatusKind.Stun, DurationMs = 5_000 } } }
            };
            var setup = Duel(8, stunner, Fighter("victim", hp: 10_000_000));
            setup.Rules.MaxDurationMs = 5_000;
            var result = BattleSimulator.Run(setup);
            var stunAt = result.Events.First(e => e.Type == BattleEventType.StatusApplied && e.Status == StatusKind.Stun).TimeMs;
            Assert.That(result.Events.Any(e => e.Type == BattleEventType.ActionStart && e.Source == 1 && e.TimeMs > stunAt), Is.False);
        }

        [Test]
        public void MultiHitSplitsDamageAndFlagsLastHit()
        {
            var setup = Duel(13, Fighter("a"), Fighter("b", hp: 10_000_000, atk: 1));
            setup.Rules.MaxDurationMs = 30_000;
            var result = BattleSimulator.Run(setup);
            var ultHits = result.Events.Where(e => e.Type == BattleEventType.Hit && e.Skill == SkillSlot.Ultimate && e.Source == 0).Take(3).ToList();
            Assert.That(ultHits.Select(e => e.HitIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(ultHits[2].Flags & HitFlags.LastHit, Is.EqualTo(HitFlags.LastHit));
        }

        [Test]
        public void AutoToggleMidFightStartsUltimatesFromThatMoment()
        {
            var setup = Duel(5, Fighter("a"), Fighter("b", hp: 10_000_000, atk: 1));
            setup.Rules.MaxDurationMs = 40_000;
            setup.AutoUltimates = false;
            setup.AutoToggleTimesMs.Add(25_000);
            var result = BattleSimulator.Run(setup);
            var ults = result.Events.Where(e => e.Type == BattleEventType.ActionStart && e.Skill == SkillSlot.Ultimate && e.Source == 0).ToList();
            Assert.That(ults, Is.Not.Empty);
            Assert.That(ults[0].TimeMs, Is.InRange(25_000, 26_500), "energy was full long before; the ultimate fires as soon as the hero is free after Auto turns on");
        }
    }
}
