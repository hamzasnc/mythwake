using System.Linq;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Faction Summon and Fragment Summon (doc: Summoning System; Summoning Gate upgrades).</summary>
    public sealed class SummonFocusTests
    {
        private static CommandContext Player(int gateLevel)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = 1_799_056_800_000 };
            new CreatePlayer { Seed = 5 }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = 30;
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone, Ids.SummonScroll, Ids.Gems })
                ctx.State.Add(id, 100_000_000);
            for (var i = 0; i < gateLevel; i++)
                new UpgradeBuilding { BuildingId = "summoningGate" }.Apply(ctx);
            return ctx;
        }

        [Test]
        public void FocusedBannersOpenWithGateLevels()
        {
            var one = Player(1);
            Assert.That(Assert.Throws<RuleViolation>(() => new SummonHeroes { BannerId = "faction", Focus = "guildsOfTheHunt" }.Apply(one)).Code, Is.EqualTo("banner_locked"));
            var two = Player(2);
            Assert.DoesNotThrow(() => new SummonHeroes { BannerId = "faction", Focus = "guildsOfTheHunt" }.Apply(two));
            Assert.That(Assert.Throws<RuleViolation>(() => new SummonHeroes { BannerId = "fragment", Focus = "ravak" }.Apply(two)).Code, Is.EqualTo("banner_locked"));
            Assert.DoesNotThrow(() => new SummonHeroes { BannerId = "fragment", Focus = "ravak" }.Apply(Player(3)));
        }

        [Test]
        public void FocusedBannersNeedAValidFocus()
        {
            var ctx = Player(3);
            Assert.That(Assert.Throws<RuleViolation>(() => new SummonHeroes { BannerId = "faction" }.Apply(ctx)).Code, Is.EqualTo("bad_focus"));
            Assert.That(Assert.Throws<RuleViolation>(() => new SummonHeroes { BannerId = "faction", Focus = "ravak" }.Apply(ctx)).Code, Is.EqualTo("bad_focus"));
            Assert.That(Assert.Throws<RuleViolation>(() => new SummonHeroes { BannerId = "fragment", Focus = "nothing" }.Apply(ctx)).Code, Is.EqualTo("bad_focus"));
        }

        [Test]
        public void MostPullsComeFromTheChosenFactionOrAspect()
        {
            var ctx = Player(3);
            var faction = Enumerable.Range(0, 20).SelectMany(_ => new SummonHeroes { BannerId = "faction", Count = 10, Focus = "orderOfTheVeil" }.Apply(ctx)).ToList();
            var share = faction.Count(r => ctx.Content.Hero(r.HeroId).Faction == Faction.OrderOfTheVeil) / (double)faction.Count;
            Assert.That(share, Is.GreaterThan(0.6), "about 70 % plus the random rest");
            var aspect = Enumerable.Range(0, 20).SelectMany(_ => new SummonHeroes { BannerId = "fragment", Count = 10, Focus = "somnara" }.Apply(ctx)).ToList();
            Assert.That(aspect.Count(r => ctx.Content.Hero(r.HeroId).Aspect == Aspect.Somnara) / (double)aspect.Count, Is.GreaterThan(0.6));
        }

        [Test]
        public void EachBannerHasItsOwnPity()
        {
            var ctx = Player(3);
            new SummonHeroes { BannerId = "faction", Count = 10, Focus = "theHollowed" }.Apply(ctx);
            Assert.That(ctx.State.Summon.PullsSinceLegendary.ContainsKey("faction"), Is.True);
            Assert.That(ctx.State.Summon.PullsSinceLegendary.ContainsKey("normal"), Is.False);
        }
    }
}
