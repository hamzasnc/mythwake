using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Watchtower village defense (docs/systems/WATCHTOWER.md).</summary>
    public sealed class DefenseTests
    {
        private static CommandContext Player(int heroLevel = 80, int stars = 5, int tower = 1)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = 1_799_056_800_000 };
            new CreatePlayer { Seed = 71 }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = 40;
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone })
                ctx.State.Add(id, 50_000_000);
            for (var i = 0; i < tower; i++)
                new UpgradeBuilding { BuildingId = "watchtower" }.Apply(ctx);
            foreach (var h in ctx.State.Heroes)
            {
                h.Level = heroLevel;
                h.Stars = stars;
            }
            return ctx;
        }

        private static DefenseResult Wave(CommandContext ctx)
        {
            new BeginDefenseWave().Apply(ctx);
            return new FinishDefenseWave().Apply(ctx);
        }

        [Test]
        public void DefenseNeedsTheWatchtowerAndHasTwoRunsADay()
        {
            Assert.That(Assert.Throws<RuleViolation>(() => new StartDefense().Apply(Player(tower: 0))).Code, Is.EqualTo("defense_locked"));
            var ctx = Player();
            new StartDefense().Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new StartDefense().Apply(ctx)).Code, Is.EqualTo("defense_running"));
            new EndDefense().Apply(ctx);
            new StartDefense().Apply(ctx);
            new EndDefense().Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new StartDefense().Apply(ctx)).Code, Is.EqualTo("defense_no_runs"));
            ctx.NowMs += Quests.DayMs;
            Assert.That(Defense.RunsLeft(ctx.State, ctx.Content, ctx.NowMs), Is.EqualTo(2));
        }

        [Test]
        public void HpCarriesOverAndWavesPayAtOnce()
        {
            var ctx = Player();
            new StartDefense().Apply(ctx);
            var wood = ctx.State.Get(Ids.Wood);
            var first = Wave(ctx);
            Assert.That(first.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(ctx.State.Get(Ids.Wood), Is.GreaterThan(wood));
            Assert.That(ctx.State.Defense.Run.Wave, Is.EqualTo(2));
            Assert.That(ctx.State.Defense.BestWave, Is.EqualTo(1));

            var setup = new BeginDefenseWave().Apply(ctx);
            var run = ctx.State.Defense.Run;
            foreach (var u in setup.Allies)
                if (run.Hp[u.Slot] > 0)
                    Assert.That(u.StartHp, Is.EqualTo(run.Hp[u.Slot]), "wounded heroes start with their HP");
            new FinishDefenseWave().Apply(ctx);
        }

        [Test]
        public void FallenHeroesStayOutAndADefeatEndsTheRun()
        {
            var ctx = Player();
            new StartDefense().Apply(ctx);
            var run = ctx.State.Defense.Run;
            var slot = System.Array.FindIndex(run.Formation, h => h != null);
            run.Hp[slot] = 0;
            var setup = new BeginDefenseWave().Apply(ctx);
            Assert.That(setup.Allies.Any(u => u.Slot == slot), Is.False);
            new FinishDefenseWave().Apply(ctx);

            var weak = Player(heroLevel: 1, stars: 1);
            new StartDefense().Apply(weak);
            var r = Wave(weak);
            Assert.That(r.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(r.RunOver, Is.True);
            Assert.That(weak.State.Defense.Run, Is.Null);
        }

        [Test]
        public void WavesGrowAndEveryFifthHasABoss()
        {
            var ctx = Player();
            var w1 = Defense.Enemies(ctx.State, ctx.Content, 1, 1);
            var w9 = Defense.Enemies(ctx.State, ctx.Content, 1, 9);
            Assert.That(w1, Has.Count.EqualTo(3));
            Assert.That(w9, Has.Count.EqualTo(5));
            Assert.That(w9.Max(u => u.Level), Is.GreaterThan(w1.Max(u => u.Level)));
            Assert.That(Defense.Enemies(ctx.State, ctx.Content, 1, 5).Any(u => u.Hero.Boss), Is.True);
        }

        [Test]
        public void HigherWatchtowerLevelsPayMore()
        {
            var low = Player(tower: 1);
            var high = Player(tower: 3);
            long Gold(CommandContext c) => Defense.WaveRewards(c.State, c.Content, 5).Where(a => a.Id == Ids.Gold).Sum(a => a.Value);
            Assert.That(Gold(high), Is.GreaterThan(Gold(low)));
            Assert.That(Defense.WaveRewards(low.State, low.Content, 10).Any(a => a.Id == Ids.Gems), Is.True);
        }

        [Test]
        public void DefenseWavesDoNotMixWithStageBattles()
        {
            var ctx = Player();
            new StartDefense().Apply(ctx);
            new BeginDefenseWave().Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new FinishStageBattle().Apply(ctx)).Code, Is.EqualTo("no_battle"));
            Assert.That(Assert.Throws<RuleViolation>(() => new EndDefense().Apply(ctx)).Code, Is.EqualTo("defense_in_battle"));
            Assert.DoesNotThrow(() => new FinishDefenseWave().Apply(ctx));
        }
    }
}
