using System.Linq;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Rebuilding Dawnhollow: production, building gates, hero slots and the bonuses they give.</summary>
    public sealed class VillageTests
    {
        private const long Start = 1_800_000_000_000;

        private static CommandContext NewPlayer()
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = Start };
            new CreatePlayer { Seed = 11 }.Apply(ctx);
            ctx.State.Add(Ids.Gold, 1_000_000);
            ctx.State.Add(Ids.Wood, 1_000_000);
            ctx.State.Add(Ids.Stone, 1_000_000);
            return ctx;
        }

        private static BuildingDefinition Def(CommandContext ctx, string id) => Village.Building(ctx.Content, id);

        [Test]
        public void SurvivorCampProducesFromTheStart()
        {
            var ctx = NewPlayer();
            Assert.That(Village.Level(ctx.State, Def(ctx, "survivorCamp")), Is.EqualTo(1));
            ctx.NowMs = Start + 2 * Afk.HourMs;
            var gold = ctx.State.Get(Ids.Gold);
            var collected = new CollectVillage().Apply(ctx);
            Assert.That(collected.First(a => a.Id == Ids.Gold).Value, Is.EqualTo(2 * 80));
            Assert.That(ctx.State.Get(Ids.Gold), Is.EqualTo(gold + 160));
            Assert.That(new CollectVillage().Apply(ctx), Is.Empty, "nothing left right after collecting");
        }

        [Test]
        public void StorageStopsAtTheCap()
        {
            var ctx = NewPlayer();
            ctx.NowMs = Start + 100 * Afk.HourMs;
            var collected = new CollectVillage().Apply(ctx);
            Assert.That(collected.First(a => a.Id == Ids.Wood).Value, Is.EqualTo(40 * ctx.Content.Village.StorageHours));
        }

        [Test]
        public void UpgradingPaysOutTheOldRateFirst()
        {
            var ctx = NewPlayer();
            ctx.NowMs = Start + Afk.HourMs;
            new UpgradeBuilding { BuildingId = "survivorCamp" }.Apply(ctx);
            ctx.NowMs = Start + 2 * Afk.HourMs;
            var gold = Village.Pending(ctx.State, ctx.Content, ctx.NowMs)[Ids.Gold];
            Assert.That(gold, Is.EqualTo(80 + 160), "first hour at level 1, second hour at level 2");
        }

        [Test]
        public void BuildingsUnlockWithCampaignProgress()
        {
            var ctx = NewPlayer();
            var error = Assert.Throws<RuleViolation>(() => new UpgradeBuilding { BuildingId = "summoningGate" }.Apply(ctx));
            Assert.That(error.Code, Is.EqualTo("building_locked"));
            ctx.State.Campaign.HighestCleared = 2;
            new UpgradeBuilding { BuildingId = "summoningGate" }.Apply(ctx);
            Assert.That(Village.SummonUnlocked(ctx.State, ctx.Content), Is.True);
            Assert.That(Assert.Throws<RuleViolation>(() => new UpgradeBuilding { BuildingId = "summoningGate" }.Apply(ctx)).Code, Is.EqualTo("building_max_level"));
        }

        [Test]
        public void PlannedBuildingsCannotBeBuiltYet()
        {
            var ctx = NewPlayer();
            ctx.State.Campaign.HighestCleared = 999;
            Assert.That(Assert.Throws<RuleViolation>(() => new UpgradeBuilding { BuildingId = "brokenForge" }.Apply(ctx)).Code, Is.EqualTo("building_planned"));
        }

        [Test]
        public void SummoningNeedsTheGate()
        {
            var ctx = NewPlayer();
            var error = Assert.Throws<RuleViolation>(() => new SummonHeroes { BannerId = "normal", Count = 1 }.Apply(ctx));
            Assert.That(error.Code, Is.EqualTo("summon_locked"));
            Assert.That(ctx.State.Get(Ids.SummonScroll), Is.EqualTo(10), "nothing paid");
        }

        [Test]
        public void HeroesInTheCampBoostProduction()
        {
            var ctx = NewPlayer();
            var before = Village.ProductionPerHour(ctx.State, ctx.Content)[Ids.Gold];
            new AssignVillageHero { BuildingId = "survivorCamp", Slot = 0, HeroId = "dorn" }.Apply(ctx);
            var after = Village.ProductionPerHour(ctx.State, ctx.Content)[Ids.Gold];
            Assert.That(after, Is.GreaterThan(before));
        }

        [Test]
        public void AHeroWorksInOneBuildingAtATime()
        {
            var ctx = NewPlayer();
            ctx.State.Campaign.HighestCleared = 5;
            new UpgradeBuilding { BuildingId = "healersTent" }.Apply(ctx);
            new AssignVillageHero { BuildingId = "survivorCamp", Slot = 0, HeroId = "maren" }.Apply(ctx);
            new AssignVillageHero { BuildingId = "healersTent", Slot = 0, HeroId = "maren" }.Apply(ctx);
            Assert.That(Village.WorkplaceOf(ctx.State, "maren"), Is.EqualTo("healersTent"));
            Assert.That(Village.Heroes(ctx.State, Def(ctx, "survivorCamp"))[0], Is.Null);
        }

        [Test]
        public void LockedSlotsAreRejected()
        {
            var ctx = NewPlayer();
            var error = Assert.Throws<RuleViolation>(() => new AssignVillageHero { BuildingId = "survivorCamp", Slot = 1, HeroId = "dorn" }.Apply(ctx));
            Assert.That(error.Code, Is.EqualTo("slot_locked"));
        }

        [Test]
        public void HealersTentStrengthensHealingInBattle()
        {
            var ctx = NewPlayer();
            var plain = Campaign.BuildBattle(ctx.State, ctx.Content, new PendingBattle { Stage = 1, Seed = 1, Formation = ctx.State.Formation });
            ctx.State.Campaign.HighestCleared = 5;
            new UpgradeBuilding { BuildingId = "healersTent" }.Apply(ctx);
            new AssignVillageHero { BuildingId = "healersTent", Slot = 0, HeroId = "maren" }.Apply(ctx);
            var boosted = Campaign.BuildBattle(ctx.State, ctx.Content, new PendingBattle { Stage = 1, Seed = 1, Formation = ctx.State.Formation });
            Assert.That(boosted.Allies[0].Stats.HealPower, Is.GreaterThan(plain.Allies[0].Stats.HealPower));
        }
    }
}
