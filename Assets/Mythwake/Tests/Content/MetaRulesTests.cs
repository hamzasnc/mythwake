using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Player commands against the real content: new player, campaign, idle rewards, upgrades, summons, saves.</summary>
    public sealed class MetaRulesTests
    {
        private const long Start = 1_800_000_000_000;

        private static CommandContext NewPlayer(ulong seed = 7)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = Start };
            new CreatePlayer { Seed = seed }.Apply(ctx);
            return ctx;
        }

        [Test]
        public void NewPlayerGetsStarterHeroesInFormation()
        {
            var ctx = NewPlayer();
            Assert.That(ctx.State.Heroes.Select(h => h.HeroId), Is.EquivalentTo(ctx.Content.Start.Heroes));
            Assert.That(Formations.Count(ctx.State.Formation), Is.EqualTo(3));
            Assert.That(ctx.State.Formation[0], Is.EqualTo("dorn"), "the only front-row starter stands in front");
            Assert.That(ctx.State.Get(Ids.SummonScroll), Is.EqualTo(10));
        }

        [Test]
        public void StarterTeamClearsTheFirstStagesWithoutUpgrades()
        {
            // every new player (any seed) must win the first fights without touching upgrades
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var ctx = NewPlayer(seed);
                for (var stage = 1; stage <= 3; stage++)
                {
                    new BeginStageBattle().Apply(ctx);
                    var result = new FinishStageBattle().Apply(ctx);
                    Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Victory), $"seed {seed}, stage {stage}");
                    Assert.That(result.Rewards, Is.Not.Empty);
                }
            }
        }

        /// <summary>
        /// Plays the early game like a simple player: fight, on defeat wait an hour and claim idle rewards, spend everything on
        /// levels, pull with every scroll, auto-fill the formation. Guards the pacing of the first chapter.
        /// </summary>
        [Test]
        public void EarlyGamePacing()
        {
            var ctx = NewPlayer(seed: 2024);
            var hours = 0;
            var log = new System.Text.StringBuilder();
            while (ctx.State.Campaign.HighestCleared < 20 && hours < 200)
            {
                while (ctx.State.Get(Ids.SummonScroll) >= 10)
                    new SummonHeroes { BannerId = "normal", Count = 10 }.Apply(ctx);
                SpendOnLevels(ctx);
                Formations.AutoFill(ctx.State, ctx.Content);

                new BeginStageBattle().Apply(ctx);
                if (new FinishStageBattle().Apply(ctx).Outcome == BattleOutcome.Victory)
                {
                    if (ctx.State.Campaign.HighestCleared % 5 == 0)
                        log.AppendLine($"stage {ctx.State.Campaign.HighestCleared} after {hours} h");
                    continue;
                }
                hours++;
                ctx.NowMs += Afk.HourMs;
                new ClaimAfkRewards().Apply(ctx);
            }
            TestContext.WriteLine(log.ToString());
            Assert.That(ctx.State.Campaign.HighestCleared, Is.EqualTo(20), log.ToString());
            Assert.That(hours, Is.InRange(1, 24), "chapter 1 should take a few hours of idle time, not minutes and not days\n" + log);
        }

        private static void SpendOnLevels(CommandContext ctx)
        {
            while (true)
            {
                var hero = ctx.State.Heroes.Where(h => !string.IsNullOrEmpty(System.Array.Find(ctx.State.Formation, id => id == h.HeroId)))
                    .OrderBy(h => h.Level).FirstOrDefault();
                if (hero == null || hero.Level >= Progression.LevelCap(ctx.Content, hero))
                    return;
                var cost = Progression.LevelCost(ctx.Content, hero.Level, 1);
                if (cost.Any(c => ctx.State.Get(c.Id) < c.Value))
                    return;
                new LevelUpHero { HeroId = hero.HeroId }.Apply(ctx);
            }
        }

        [Test]
        public void FinishingWithoutABattleIsRejected()
        {
            var ctx = NewPlayer();
            var error = Assert.Throws<RuleViolation>(() => new FinishStageBattle().Apply(ctx));
            Assert.That(error.Code, Is.EqualTo("no_battle"));
        }

        [Test]
        public void ServerResimulationMatchesTheClientBattle()
        {
            // the client plays the setup it got; the authority re-simulates from the stored seed: same outcome
            var ctx = NewPlayer();
            var setup = new BeginStageBattle().Apply(ctx);
            var client = BattleSimulator.Run(setup);
            var server = new FinishStageBattle().Apply(ctx);
            Assert.That(server.Outcome, Is.EqualTo(client.Outcome));
        }

        [Test]
        public void StagesAreTheSameForEveryPlayer()
        {
            var content = ContentFixture.Content();
            var a = Campaign.Enemies(content, 25).Select(u => u.Hero.Id + u.Slot).ToArray();
            var b = Campaign.Enemies(content, 25).Select(u => u.Hero.Id + u.Slot).ToArray();
            Assert.That(b, Is.EqualTo(a));
            Assert.That(Campaign.Enemies(content, 1).Count, Is.EqualTo(2));
            Assert.That(Campaign.Enemies(content, 12).Count, Is.EqualTo(4));
            Assert.That(Campaign.Enemies(content, 30).Count, Is.EqualTo(5));
            Assert.That(Campaign.Enemies(content, 20).All(u => u.Variant == "boss"), Is.True);
        }

        [Test]
        public void IdleRewardsAccumulateUpToTheCap()
        {
            var ctx = NewPlayer();
            ctx.NowMs = Start + 2 * Afk.HourMs;
            var twoHours = Afk.Pending(ctx.State, ctx.Content, ctx.NowMs).First(a => a.Id == Ids.Gold).Value;
            Assert.That(twoHours, Is.EqualTo(2 * 500));

            ctx.NowMs = Start + 100 * Afk.HourMs;
            var gold = ctx.State.Get(Ids.Gold);
            var claimed = new ClaimAfkRewards().Apply(ctx);
            Assert.That(claimed.First(a => a.Id == Ids.Gold).Value, Is.EqualTo(12 * 500));
            Assert.That(ctx.State.Get(Ids.Gold), Is.EqualTo(gold + 12 * 500));
            Assert.That(Afk.Pending(ctx.State, ctx.Content, ctx.NowMs), Is.Empty);
        }

        [Test]
        public void LevelUpPaysAndRespectsTheCap()
        {
            var ctx = NewPlayer();
            var exp = ctx.State.Get(Ids.HeroExp);
            new LevelUpHero { HeroId = "dorn" }.Apply(ctx);
            Assert.That(ctx.State.FindHero("dorn").Level, Is.EqualTo(2));
            Assert.That(ctx.State.Get(Ids.HeroExp), Is.LessThan(exp));

            ctx.State.Add(Ids.HeroExp, 100_000_000);
            ctx.State.Add(Ids.Gold, 100_000_000);
            var cap = Progression.LevelCap(ctx.Content, ctx.State.FindHero("dorn"));
            var error = Assert.Throws<RuleViolation>(() => new LevelUpHero { HeroId = "dorn", Levels = cap }.Apply(ctx));
            Assert.That(error.Code, Is.EqualTo("level_cap"));
            Assert.That(ctx.State.FindHero("dorn").Level, Is.EqualTo(2), "a rejected command changes nothing");
        }

        [Test]
        public void NotEnoughCurrencyIsRejectedWithoutSideEffects()
        {
            var ctx = NewPlayer();
            ctx.State.Currencies.Remove(Ids.HeroExp);
            var gold = ctx.State.Get(Ids.Gold);
            var error = Assert.Throws<RuleViolation>(() => new LevelUpHero { HeroId = "dorn" }.Apply(ctx));
            Assert.That(error.Code, Is.EqualTo("not_enough:" + Ids.HeroExp));
            Assert.That(ctx.State.Get(Ids.Gold), Is.EqualTo(gold));
        }

        [Test]
        public void SummonPityGuaranteesALegendary()
        {
            var ctx = NewPlayer(seed: 12345);
            ctx.State.Add(Ids.SummonScroll, 1000);
            var banner = ctx.Content.Banner("normal");
            var sinceLegendary = 0;
            for (var i = 0; i < 30; i++)
            {
                foreach (var r in new SummonHeroes { BannerId = "normal", Count = 10 }.Apply(ctx))
                {
                    sinceLegendary++;
                    if (ctx.Content.Hero(r.HeroId).Grade >= Core.Content.Grade.Legendary)
                        sinceLegendary = 0;
                    Assert.That(sinceLegendary, Is.LessThan(banner.LegendaryPity));
                }
            }
            Assert.That(ctx.State.Summon.TotalPulls, Is.EqualTo(300));
        }

        [Test]
        public void EveryTenPullsContainAnEpicOrBetter()
        {
            var ctx = NewPlayer(seed: 99);
            ctx.State.Add(Ids.SummonScroll, 1000);
            for (var i = 0; i < 20; i++)
            {
                var pulls = new SummonHeroes { BannerId = "normal", Count = 10 }.Apply(ctx);
                Assert.That(pulls.Any(r => ctx.Content.Hero(r.HeroId).Grade >= Core.Content.Grade.Epic), Is.True, $"10-pull {i}");
            }
        }

        [Test]
        public void DuplicatesBecomeShardsThatBuyStars()
        {
            var ctx = NewPlayer(seed: 3);
            ctx.State.Add(Ids.SummonScroll, 1000);
            for (var i = 0; i < 10; i++)
                new SummonHeroes { BannerId = "normal", Count = 10 }.Apply(ctx);
            var withShards = ctx.State.Heroes.First(h => ctx.State.Get(Ids.Shards(h.HeroId)) >= 10);
            new StarUpHero { HeroId = withShards.HeroId }.Apply(ctx);
            Assert.That(withShards.Stars, Is.EqualTo(2));
        }

        [Test]
        public void SummonsAreDeterministicPerPlayerSeed()
        {
            string[] Pull(ulong seed)
            {
                var ctx = NewPlayer(seed);
                return new SummonHeroes { BannerId = "normal", Count = 10 }.Apply(ctx).Select(r => r.HeroId).ToArray();
            }
            Assert.That(Pull(5), Is.EqualTo(Pull(5)));
            Assert.That(Pull(5), Is.Not.EqualTo(Pull(6)));
        }

        [Test]
        public void FormationRulesAreEnforced()
        {
            var ctx = NewPlayer();
            Assert.That(Assert.Throws<RuleViolation>(() => new SetFormation { Slots = new[] { "dorn", "dorn", null, null, null } }.Apply(ctx)).Code, Is.EqualTo("hero_twice"));
            Assert.That(Assert.Throws<RuleViolation>(() => new SetFormation { Slots = new[] { "kaelen", null, null, null, null } }.Apply(ctx)).Code, Is.EqualTo("hero_not_owned"));
            new SetFormation { Slots = new[] { null, null, "tobias", "maren", "dorn" } }.Apply(ctx);
            Assert.That(ctx.State.Formation[4], Is.EqualTo("dorn"));
        }

        [Test]
        public void SaveRoundTripKeepsEverything()
        {
            var ctx = NewPlayer();
            new BeginStageBattle().Apply(ctx);
            new FinishStageBattle().Apply(ctx);
            new SummonHeroes { BannerId = "normal", Count = 10 }.Apply(ctx);
            var json = SaveJson.Write(ctx.State);
            Assert.That(SaveJson.Write(SaveJson.Read(json)), Is.EqualTo(json));
        }
    }
}
