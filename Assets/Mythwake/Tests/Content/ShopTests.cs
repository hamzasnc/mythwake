using System.Linq;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Shops (docs/systems/SHOPS.md).</summary>
    public sealed class ShopTests
    {
        private static CommandContext Player(int stage = 40)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = 1_799_056_800_000 };
            new CreatePlayer { Seed = 81 }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = stage;
            foreach (var id in new[] { Ids.Gold, Ids.Gems, Ids.FragmentDust, Ids.ArenaCoins, Ids.Wood, Ids.Stone })
                ctx.State.Add(id, 10_000_000);
            return ctx;
        }

        private static ShopPurchase Buy(CommandContext ctx, string shop, string item) =>
            new BuyShopItem { ShopId = shop, ItemId = item }.Apply(ctx);

        [Test]
        public void DailyLimitsResetEveryDayWeeklyOnesEveryWeek()
        {
            var ctx = Player();
            Buy(ctx, "general", "summonScroll");
            Assert.That(Assert.Throws<RuleViolation>(() => Buy(ctx, "general", "summonScroll")).Code, Is.EqualTo("sold_out"));
            Buy(ctx, "general", "realmKey");
            ctx.NowMs += Quests.DayMs;
            Assert.DoesNotThrow(() => Buy(ctx, "general", "summonScroll"));
            Assert.That(Assert.Throws<RuleViolation>(() => Buy(ctx, "general", "realmKey")).Code, Is.EqualTo("sold_out"), "weekly");
            ctx.NowMs += Quests.DayMs * 7;
            Assert.DoesNotThrow(() => Buy(ctx, "general", "realmKey"));
        }

        [Test]
        public void ShopsOpenWithTheirBuildingOrStage()
        {
            var ctx = Player(stage: 10);
            Assert.That(Assert.Throws<RuleViolation>(() => Buy(ctx, "fragment", "relicMaterials")).Code, Is.EqualTo("shop_locked"));
            Assert.That(Assert.Throws<RuleViolation>(() => Buy(ctx, "arena", "summonScroll")).Code, Is.EqualTo("shop_locked"));
            Assert.DoesNotThrow(() => Buy(ctx, "village", "wood"), "the Survivor Camp stands from the start");
            ctx.State.Campaign.HighestCleared = 30;
            new UpgradeBuilding { BuildingId = "fragmentShrine" }.Apply(ctx);
            Assert.DoesNotThrow(() => Buy(ctx, "fragment", "relicMaterials"));
            Assert.DoesNotThrow(() => Buy(ctx, "arena", "summonScroll"));
        }

        [Test]
        public void ItemsGiveWhatTheyPromise()
        {
            var ctx = Player();
            new UpgradeBuilding { BuildingId = "fragmentShrine" }.Apply(ctx);
            var gold = ctx.State.Get(Ids.Gold);
            var wood = ctx.State.Get(Ids.Wood);
            Buy(ctx, "village", "wood");
            Assert.That(ctx.State.Get(Ids.Wood), Is.EqualTo(wood + 800));
            Assert.That(ctx.State.Get(Ids.Gold), Is.EqualTo(gold - 4000));

            var fragment = Buy(ctx, "fragment", "echoEpic").Fragment;
            Assert.That(fragment.Slot, Is.EqualTo(FragmentSlot.Echo));
            Assert.That(fragment.Grade, Is.EqualTo(Grade.Epic));
            Assert.That(fragment.Effect, Is.Not.Null);

            Assert.That(Buy(ctx, "arena", "gearEpic").Gear.Grade, Is.EqualTo(Grade.Epic));
            var shards = Buy(ctx, "arena", "shardsLegendary");
            Assert.That(ctx.Content.Hero(shards.ShardHero).Grade, Is.EqualTo(Grade.Legendary));
            Assert.That(ctx.State.Get(Ids.Shards(shards.ShardHero)), Is.EqualTo(10));
        }

        [Test]
        public void ShardOffersRotateDaily()
        {
            var content = ContentFixture.Content();
            var item = Shops.Item(Shops.Get(content, "arena"), "shardsEpic");
            var days = Enumerable.Range(0, 5).Select(d => Shops.ShardHero(content, item, 1_799_056_800_000 + d * Quests.DayMs)).Distinct().Count();
            Assert.That(days, Is.GreaterThan(1));
        }
    }
}
