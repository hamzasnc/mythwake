using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Per-hero target priority (doc: "target mages first, target the weakest units first").</summary>
    public sealed class TargetPriorityTests
    {
        /// <summary>One attacker against a front tank and a back support; returns who the first normal attack hits.</summary>
        private static string FirstTarget(TargetPriority priority)
        {
            var content = ContentFixture.Content();
            var attacker = content.Heroes.Values.First(h => h.Role == Role.Dps && h.Normal?.Target == TargetSelector.EnemyFront);
            var tank = content.Heroes.Values.First(h => h.Role == Role.Tank);
            var support = content.Heroes.Values.First(h => h.Role == Role.Support);
            var setup = new BattleSetup { Seed = 9 };
            setup.Allies.Add(new UnitSetup { Hero = attacker, Slot = 2, Stats = attacker.BaseStats.Clone(), Priority = priority });
            setup.Enemies.Add(new UnitSetup { Hero = tank, Slot = 0, Stats = tank.BaseStats.Clone() });
            setup.Enemies.Add(new UnitSetup { Hero = support, Slot = 3, Stats = support.BaseStats.Clone() });
            foreach (var e in setup.Enemies)
                e.Stats.Hp *= 100;
            var result = BattleSimulator.Run(setup);
            var first = result.Events.First(e => e.Type == BattleEventType.ActionStart && e.Source == 0 && e.Skill == SkillSlot.Normal);
            return setup.Enemies[first.Target - 1].Hero.Id;
        }

        [Test]
        public void DefaultHitsTheFrontSupportsPriorityHitsTheSupport()
        {
            var content = ContentFixture.Content();
            var tank = content.Heroes.Values.First(h => h.Role == Role.Tank).Id;
            var support = content.Heroes.Values.First(h => h.Role == Role.Support).Id;
            Assert.That(FirstTarget(TargetPriority.Default), Is.EqualTo(tank));
            Assert.That(FirstTarget(TargetPriority.Supports), Is.EqualTo(support));
            Assert.That(FirstTarget(TargetPriority.BackRow), Is.EqualTo(support));
        }

        [Test]
        public void ThePlayersChoiceReachesEveryBattleAndTheArenaSnapshot()
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = 1_799_056_800_000 };
            new CreatePlayer { Seed = 3 }.Apply(ctx);
            var hero = ctx.State.Formation.First(h => h != null);
            new SetTargetPriority { HeroId = hero, Priority = TargetPriority.Weakest }.Apply(ctx);
            var allies = Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation);
            Assert.That(allies.First(u => u.Hero.Id == hero).Priority, Is.EqualTo(TargetPriority.Weakest));
            var snapshot = TeamSnapshot.Of(allies).ToSetups(ctx.Content);
            Assert.That(snapshot.First(u => u.Hero.Id == hero).Priority, Is.EqualTo(TargetPriority.Weakest));
            Assert.That(Assert.Throws<RuleViolation>(() => new SetTargetPriority { HeroId = "nobody", Priority = TargetPriority.Dps }.Apply(ctx)).Code,
                Is.EqualTo("hero_not_owned"));
        }
    }
}
