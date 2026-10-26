using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Portal Chamber dungeons (docs/systems/DUNGEONS.md).</summary>
    public sealed class DungeonTests
    {
        // Monday 2027-01-04 10:00 UTC
        private const long Monday = 1_799_056_800_000;

        private static CommandContext Player(int portalLevel = 2, int heroLevel = 60)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = Monday };
            new CreatePlayer { Seed = 51 }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = 20;
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone })
                ctx.State.Add(id, 10_000_000);
            for (var i = 0; i < portalLevel; i++)
                new UpgradeBuilding { BuildingId = "portalChamber" }.Apply(ctx);
            foreach (var h in ctx.State.Heroes)
            {
                h.Stars = 5;
                h.Level = heroLevel;
            }
            return ctx;
        }

        private static DungeonResult Fight(CommandContext ctx, string id, int level)
        {
            new BeginDungeon { DungeonId = id, Level = level }.Apply(ctx);
            return new FinishDungeon().Apply(ctx);
        }

        [Test]
        public void DungeonsNeedThePortalChamberLevel()
        {
            var none = Player(portalLevel: 0);
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginDungeon { DungeonId = "material", Level = 1 }.Apply(none)).Code, Is.EqualTo("dungeon_locked"));
            var one = Player(portalLevel: 1);
            Assert.DoesNotThrow(() => new BeginDungeon { DungeonId = "material", Level = 1 }.Apply(one));
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginDungeon { DungeonId = "gear", Level = 1 }.Apply(one)).Code, Is.EqualTo("dungeon_locked"));
        }

        [Test]
        public void LevelsOpenOneAfterAnother()
        {
            var ctx = Player();
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginDungeon { DungeonId = "material", Level = 2 }.Apply(ctx)).Code, Is.EqualTo("dungeon_level_locked"));
            Assert.That(Fight(ctx, "material", 1).Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.DoesNotThrow(() => new BeginDungeon { DungeonId = "material", Level = 2 }.Apply(ctx));
        }

        [Test]
        public void FirstClearPaysMoreThanRepeats()
        {
            var ctx = Player();
            var first = Fight(ctx, "material", 1);
            var repeat = Fight(ctx, "material", 1);
            Assert.That(first.FirstClear, Is.True);
            Assert.That(repeat.FirstClear, Is.False);
            long Gold(DungeonResult r) => r.Rewards.Where(a => a.Id == Ids.Gold).Sum(a => a.Value);
            Assert.That(Gold(first), Is.GreaterThan(Gold(repeat)));
        }

        [Test]
        public void ThreeAttemptsPerDayOnlyWinsCount()
        {
            var weak = Player(heroLevel: 1);
            Assert.That(Fight(weak, "material", 1).Outcome, Is.EqualTo(BattleOutcome.Defeat));
            var d = Dungeons.Get(weak.Content, "material");
            Assert.That(Dungeons.AttemptsLeft(weak.State, weak.Content, d, weak.NowMs), Is.EqualTo(3), "a lost fight costs no attempt");

            var ctx = Player();
            for (var i = 0; i < 3; i++)
                Fight(ctx, "material", 1);
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginDungeon { DungeonId = "material", Level = 1 }.Apply(ctx)).Code, Is.EqualTo("dungeon_no_attempts"));
            ctx.NowMs += Quests.DayMs;
            Assert.That(Dungeons.AttemptsLeft(ctx.State, ctx.Content, d, ctx.NowMs), Is.EqualTo(3), "attempts reset daily and do not stack");
        }

        [Test]
        public void SweepRepeatsTheHighestLevelWithoutAFight()
        {
            var ctx = Player();
            Assert.That(Assert.Throws<RuleViolation>(() => new SweepDungeon { DungeonId = "material" }.Apply(ctx)).Code, Is.EqualTo("dungeon_nothing_to_sweep"));
            Fight(ctx, "material", 1);
            var gold = ctx.State.Get(Ids.Gold);
            var rewards = new SweepDungeon { DungeonId = "material" }.Apply(ctx);
            Assert.That(ctx.State.Get(Ids.Gold), Is.EqualTo(gold + rewards.First(r => r.Id == Ids.Gold).Value));
            Assert.That(ctx.State.Battle, Is.Null);
        }

        [Test]
        public void SpecialRulesReachTheBattle()
        {
            var ctx = Player();
            var setup = new BeginDungeon { DungeonId = "gear", Level = 1 }.Apply(ctx);
            Assert.That(setup.Rules.HealingModifier, Is.EqualTo(-500));
            Assert.That(setup.Enemies.Count(u => u.Hero.Boss), Is.EqualTo(1));
            var material = new BeginDungeon { DungeonId = "material", Level = 1 }.Apply(Player());
            Assert.That(material.Rules.EnemyRegenPermille, Is.GreaterThan(0));
        }

        [Test]
        public void EnemyRegenerationHealsEnemies()
        {
            var ctx = Player();
            var setup = new BeginDungeon { DungeonId = "material", Level = 1 }.Apply(ctx);
            var result = BattleSimulator.Run(setup);
            var firstEnemy = setup.Allies.Count;
            Assert.That(result.Events.Any(e => e.Type == BattleEventType.Heal && e.Target == firstEnemy), Is.True);
        }

        [Test]
        public void GearDungeonDropsGearOnMilestones()
        {
            var ctx = Player(heroLevel: 100);
            var every = Dungeons.Get(ctx.Content, "gear").GearEveryLevels;
            for (var level = 1; level <= every; level++)
            {
                ctx.State.Dungeons.Used.Clear();
                var r = Fight(ctx, "gear", level);
                Assert.That(r.Outcome, Is.EqualTo(BattleOutcome.Victory), $"level {level}");
                Assert.That(r.Gear.Count, Is.EqualTo(level == every ? 1 : 0), $"level {level}");
            }
        }

        [Test]
        public void DungeonBattlesDoNotMixWithOtherBattles()
        {
            var ctx = Player();
            new BeginDungeon { DungeonId = "material", Level = 1 }.Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new FinishStageBattle().Apply(ctx)).Code, Is.EqualTo("no_battle"));
            Assert.That(Assert.Throws<RuleViolation>(() => new FinishHunt().Apply(ctx)).Code, Is.EqualTo("no_battle"));
        }
    }
}
