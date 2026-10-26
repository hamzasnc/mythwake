using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Hunter's Hut, Hunt Board and Hunt Shop (docs/systems/HUNT_BOARD.md).</summary>
    public sealed class HuntTests
    {
        // Monday 2027-01-04 10:00 UTC
        private const long Monday = 1_799_056_800_000;

        private static CommandContext HunterPlayer(int hutLevel = 1, ulong seed = 41)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = Monday };
            new CreatePlayer { Seed = seed }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = 12;
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone })
                ctx.State.Add(id, 10_000_000);
            for (var i = 0; i < hutLevel; i++)
                new UpgradeBuilding { BuildingId = "huntersHut" }.Apply(ctx);
            return ctx;
        }

        private static HuntResult Play(CommandContext ctx, HuntOffer offer)
        {
            new BeginHunt { HuntId = offer.Id }.Apply(ctx);
            return new FinishHunt().Apply(ctx);
        }

        [Test]
        public void TheBoardNeedsTheHut()
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = Monday };
            new CreatePlayer { Seed = 1 }.Apply(ctx);
            Assert.That(Hunts.Board(ctx.State, ctx.Content, ctx.NowMs), Is.Empty);
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginHunt { HuntId = "d0-0" }.Apply(ctx)).Code, Is.EqualTo("hunts_locked"));
        }

        [Test]
        public void HutLevelsAddHunts()
        {
            var content = ContentFixture.Content();
            Assert.That(Hunts.DailyCount(content, 1), Is.EqualTo(3));
            Assert.That(Hunts.DailyCount(content, 4), Is.EqualTo(4));
            Assert.That(Hunts.DailyCount(content, 10), Is.EqualTo(6));
            Assert.That(Hunts.WeeklyCount(content, 5), Is.EqualTo(1));
            Assert.That(Hunts.WeeklyCount(content, 6), Is.EqualTo(2));
            var ctx = HunterPlayer();
            var board = Hunts.Board(ctx.State, ctx.Content, ctx.NowMs);
            Assert.That(board.Count(o => !o.Weekly), Is.EqualTo(3));
            Assert.That(board.Count(o => o.Weekly), Is.EqualTo(1));
        }

        [Test]
        public void TheBoardIsStableForADayAndChangesTheNext()
        {
            var ctx = HunterPlayer();
            string Key(long now) => string.Join(",", Hunts.Board(ctx.State, ctx.Content, now).Where(o => !o.Weekly).Select(o => o.Mission.Id + o.Tier + o.Seed));
            Assert.That(Key(Monday + Afk.HourMs), Is.EqualTo(Key(Monday)));
            Assert.That(Key(Monday + Quests.DayMs), Is.Not.EqualTo(Key(Monday)));
        }

        [Test]
        public void WinningAHuntPaysOnceAndCountsForQuests()
        {
            var ctx = HunterPlayer();
            foreach (var h in ctx.State.Heroes)
                h.Level = 40;
            var offer = Hunts.Board(ctx.State, ctx.Content, ctx.NowMs).First(o => !o.Weekly && o.Tier == Hunts.Board(ctx.State, ctx.Content, ctx.NowMs).Where(x => !x.Weekly).Min(x => x.Tier));
            var tokens = ctx.State.Get(Ids.HuntTokens);
            var result = Play(ctx, offer);
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(ctx.State.Get(Ids.HuntTokens), Is.GreaterThan(tokens));
            Assert.That(Hunts.IsDone(ctx.State, ctx.Content, offer, ctx.NowMs), Is.True);
            Assert.That(Quests.StatValue(ctx.State, Stat.HuntCompleted), Is.EqualTo(1));
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginHunt { HuntId = offer.Id }.Apply(ctx)).Code, Is.EqualTo("hunt_done"));
        }

        [Test]
        public void LosingCostsNothingAndCanBeRetried()
        {
            var ctx = HunterPlayer();
            var offer = Hunts.Board(ctx.State, ctx.Content, ctx.NowMs).First(o => o.Weekly);
            var gold = ctx.State.Get(Ids.Gold);
            var result = Play(ctx, offer);
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Defeat), "level-1 starters cannot beat a weekly elite at stage 12");
            Assert.That(ctx.State.Get(Ids.Gold), Is.EqualTo(gold));
            Assert.That(Hunts.IsDone(ctx.State, ctx.Content, offer, ctx.NowMs), Is.False);
            Assert.DoesNotThrow(() => new BeginHunt { HuntId = offer.Id }.Apply(ctx));
        }

        [Test]
        public void HigherStarsAndHutLevelsPayMoreAndFightHarder()
        {
            var low = HunterPlayer(1);
            var high = HunterPlayer(10);
            var a = Hunts.Board(low.State, low.Content, Monday).First(o => !o.Weekly);
            var b = Hunts.Board(high.State, high.Content, Monday).First(o => !o.Weekly && o.Tier == a.Tier);
            long Tokens(HuntOffer o) => o.Rewards.First(r => r.Id == Ids.HuntTokens).Value;
            Assert.That(Tokens(b), Is.GreaterThan(Tokens(a)));
            Assert.That(b.EnemyLevel, Is.GreaterThan(a.EnemyLevel));
        }

        [Test]
        public void WeeklyHuntsAreEliteAndUseTheirFaction()
        {
            var ctx = HunterPlayer();
            var weekly = Hunts.Board(ctx.State, ctx.Content, ctx.NowMs).First(o => o.Weekly);
            var enemies = Hunts.Enemies(ctx.State, ctx.Content, weekly);
            Assert.That(enemies.All(u => u.Variant == "boss"), Is.True);
            Assert.That(enemies.All(u => u.Hero.Faction == Faction.TheHollowed), Is.True);
        }

        [Test]
        public void ShopLimitsResetWithTheirPeriod()
        {
            var ctx = HunterPlayer();
            ctx.State.Add(Ids.HuntTokens, 10_000);
            var item = Hunts.ShopItem(ctx.Content, "summonScroll");
            for (var i = 0; i < item.Limit; i++)
                new BuyHuntItem { ItemId = item.Id }.Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new BuyHuntItem { ItemId = item.Id }.Apply(ctx)).Code, Is.EqualTo("sold_out"));
            ctx.NowMs += Quests.DayMs;
            Assert.That(Assert.Throws<RuleViolation>(() => new BuyHuntItem { ItemId = item.Id }.Apply(ctx)).Code, Is.EqualTo("sold_out"), "weekly item stays sold out tomorrow");
            ctx.NowMs += 7 * Quests.DayMs;
            Assert.DoesNotThrow(() => new BuyHuntItem { ItemId = item.Id }.Apply(ctx));
        }

        [Test]
        public void ShopGearItemGivesAPiece()
        {
            var ctx = HunterPlayer();
            ctx.State.Add(Ids.HuntTokens, 1_000);
            var piece = new BuyHuntItem { ItemId = "epicGear" }.Apply(ctx);
            Assert.That(piece.Grade, Is.EqualTo(Grade.Epic));
            Assert.That(ctx.State.Gear, Does.Contain(piece));
        }

        [Test]
        public void CampaignAndHuntBattlesDoNotMix()
        {
            var ctx = HunterPlayer();
            var offer = Hunts.Board(ctx.State, ctx.Content, ctx.NowMs).First();
            new BeginHunt { HuntId = offer.Id }.Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new FinishStageBattle().Apply(ctx)).Code, Is.EqualTo("no_battle"));
        }
    }
}
