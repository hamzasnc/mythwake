using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Guild rules the server relies on (docs/systems/GUILDS.md); the guild itself lives on the server.</summary>
    public sealed class GuildTests
    {
        private static CommandContext Player()
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = 1_799_056_800_000 };
            new CreatePlayer { Seed = 97 }.Apply(ctx);
            ctx.State.Add(Ids.Gold, 1_000_000);
            ctx.State.Add(Ids.Gems, 10_000);
            return ctx;
        }

        [Test]
        public void TheRaidBossesAreTheDocsGuardians()
        {
            var content = ContentFixture.Content();
            Assert.That(content.Guilds.Raid.Bosses, Is.EqualTo(new[]
                { "celestial_turtle", "dragon_of_the_sky", "hollowed_leviathan", "six_legged_worldbearer", "craterborn_guardian" }));
            Assert.That(content.Guilds.Raid.Bosses.All(id => content.Monsters[id].RaidBoss), Is.True);
            Assert.That(Guilds.RaidBoss(content, 6), Is.EqualTo("celestial_turtle"), "the order starts again");
            Assert.That(Guilds.RaidBossHp(content, 2), Is.GreaterThan(Guilds.RaidBossHp(content, 1)));
        }

        [Test]
        public void ARaidAttemptLastsTheBattleTimeAgainstTheHpLeft()
        {
            var ctx = Player();
            var team = TeamSnapshot.Of(Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation));
            var setup = Guilds.RaidSetup(ctx.Content, team, 1, 1_000_000, 42);
            Assert.That(setup.Enemies.Single().Stats.Hp, Is.EqualTo(1_000_000));
            Assert.That(setup.Rules.MaxDurationMs, Is.EqualTo(60_000));
            var result = BattleSimulator.Run(setup);
            var damage = result.Units.Where(u => u.Side == Side.Enemies).Sum(u => u.DamageTaken);
            Assert.That(damage, Is.GreaterThan(0), "even a starting team deals damage");
            Assert.That(Guilds.AttemptRewards(ctx.Content, damage).Any(a => a.Id == Ids.GuildCoins), Is.True);
        }

        [Test]
        public void GuildCommandsAreServerOnlyAndTheShopNeedsAGuild()
        {
            Assert.That(typeof(SetGuild).GetCustomAttributes(typeof(ServerOnlyAttribute), false), Is.Not.Empty);
            Assert.That(typeof(DonateToGuild).GetCustomAttributes(typeof(ServerOnlyAttribute), false), Is.Not.Empty);
            Assert.That(typeof(PayGuildCreation).GetCustomAttributes(typeof(ServerOnlyAttribute), false), Is.Not.Empty);

            var ctx = Player();
            ctx.State.Add(Ids.GuildCoins, 1000);
            Assert.That(Assert.Throws<RuleViolation>(() => new BuyShopItem { ShopId = "guild", ItemId = "relicMaterials" }.Apply(ctx)).Code, Is.EqualTo("shop_locked"));
            Assert.That(Assert.Throws<RuleViolation>(() => new DonateToGuild { DonationId = "gold" }.Apply(ctx)).Code, Is.EqualTo("no_guild"));
            new SetGuild { GuildId = "00000000-0000-0000-0000-000000000001" }.Apply(ctx);
            Assert.DoesNotThrow(() => new BuyShopItem { ShopId = "guild", ItemId = "relicMaterials" }.Apply(ctx));
            var coins = ctx.State.Get(Ids.GuildCoins);
            Assert.That(new DonateToGuild { DonationId = "gold" }.Apply(ctx), Is.GreaterThan(0));
            Assert.That(ctx.State.Get(Ids.GuildCoins), Is.GreaterThan(coins));
        }

        [Test]
        public void FoundingCostsGems()
        {
            var ctx = Player();
            var gems = ctx.State.Get(Ids.Gems);
            new PayGuildCreation().Apply(ctx);
            Assert.That(ctx.State.Get(Ids.Gems), Is.EqualTo(gems - 300));
        }
    }
}
