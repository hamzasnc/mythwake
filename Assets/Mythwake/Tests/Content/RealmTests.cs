using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Shattered Realms (docs/systems/SHATTERED_REALMS.md).</summary>
    public sealed class RealmTests
    {
        // Monday 2027-01-04 10:00 UTC
        private const long Monday = 1_799_056_800_000;

        private static CommandContext Player(int portalLevel = 5, int heroLevel = 150, int stars = 7)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = Monday };
            new CreatePlayer { Seed = 23 }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = 80;
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone })
                ctx.State.Add(id, 500_000_000);
            for (var i = 0; i < portalLevel; i++)
                new UpgradeBuilding { BuildingId = "portalChamber" }.Apply(ctx);
            foreach (var h in ctx.State.Heroes)
            {
                h.Level = heroLevel;
                h.Stars = stars;
            }
            return ctx;
        }

        private static RealmResult Fight(CommandContext ctx, string realm, int level)
        {
            new BeginRealm { RealmId = realm, Level = level }.Apply(ctx);
            return new FinishRealm().Apply(ctx);
        }

        [Test]
        public void RealmsOpenWithPortalChamberLevelFive()
        {
            var ctx = Player(portalLevel: 4);
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginRealm { RealmId = "ash", Level = 1 }.Apply(ctx)).Code, Is.EqualTo("realm_locked"));
            new UpgradeBuilding { BuildingId = "portalChamber" }.Apply(ctx);
            Assert.DoesNotThrow(() => new BeginRealm { RealmId = "ash", Level = 1 }.Apply(ctx));
        }

        [Test]
        public void TwoFreeEntriesADayForAllRealmsThenKeys()
        {
            var ctx = Player();
            Assert.That(Fight(ctx, "ash", 1).UsedKey, Is.False);
            Assert.That(Fight(ctx, "hunger", 1).UsedKey, Is.False, "the free entries are shared by all realms");
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginRealm { RealmId = "ash", Level = 2 }.Apply(ctx)).Code, Is.EqualTo("realm_no_entries"));
            ctx.State.Add(Ids.RealmKey, 1);
            var keyed = Fight(ctx, "ash", 2);
            Assert.That(keyed.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(keyed.UsedKey, Is.True);
            Assert.That(ctx.State.Get(Ids.RealmKey), Is.Zero);
            ctx.NowMs += Quests.DayMs;
            Assert.That(Realms.FreeLeft(ctx.State, ctx.Content, ctx.NowMs), Is.EqualTo(2), "entries reset daily and do not stack");
        }

        [Test]
        public void ALostFightUsesNoEntry()
        {
            var ctx = Player(heroLevel: 1, stars: 1);
            Assert.That(Fight(ctx, "stillness", 1).Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(Realms.FreeLeft(ctx.State, ctx.Content, ctx.NowMs), Is.EqualTo(2));
        }

        [Test]
        public void FirstClearsPayGuardianSigilsAndGems()
        {
            var ctx = Player();
            var first = Fight(ctx, "reflection", 1);
            var again = Fight(ctx, "reflection", 1);
            Assert.That(first.FirstClear, Is.True);
            Assert.That(again.FirstClear, Is.False);
            long Of(RealmResult r, string id) => r.Rewards.Where(a => a.Id == id).Sum(a => a.Value);
            Assert.That(Of(first, Ids.GuardianSigil), Is.GreaterThan(Of(again, Ids.GuardianSigil)));
            Assert.That(Of(first, Ids.Gems), Is.GreaterThan(0));
            Assert.That(Of(again, Ids.GuardianSigil), Is.GreaterThan(0), "every clear pays rare materials");
        }

        [Test]
        public void EachRealmBringsItsRule()
        {
            var ctx = Player();
            var ash = new BeginRealm { RealmId = "ash", Level = 1 }.Apply(ctx);
            Assert.That(ash.Rules.HealingModifier, Is.LessThan(0));
            Assert.That(ash.Rules.EnemyDamageModifier, Is.GreaterThan(0));
            new FinishRealm().Apply(ctx);

            var reflection = new BeginRealm { RealmId = "reflection", Level = 1 }.Apply(ctx);
            var result = BattleSimulator.Run(reflection);
            var allies = reflection.Allies.Count;
            Assert.That(result.Events.Any(e => e.Type == BattleEventType.Hit && e.Source >= allies && e.Target < allies && e.Skill == SkillSlot.Passive),
                Is.True, "enemies hit back when struck");
            new FinishRealm().Apply(ctx);

            var hunger = Realms.Get(ctx.Content, "hunger");
            Assert.That(hunger.Rules.EnemyRegenPermille, Is.GreaterThan(0));
            Assert.That(Realms.Get(ctx.Content, "stillness").Rules.EnergyGainModifier, Is.LessThan(0));
        }

        [Test]
        public void SweepUsesAnEntryAndNeedsAClearedLevel()
        {
            var ctx = Player();
            Assert.That(Assert.Throws<RuleViolation>(() => new SweepRealm { RealmId = "ash" }.Apply(ctx)).Code, Is.EqualTo("realm_nothing_to_sweep"));
            Fight(ctx, "ash", 1);
            var rewards = new SweepRealm { RealmId = "ash" }.Apply(ctx);
            Assert.That(rewards.Any(a => a.Id == Ids.GuardianSigil), Is.True);
            Assert.That(Realms.FreeLeft(ctx.State, ctx.Content, ctx.NowMs), Is.Zero);
        }

        [Test]
        public void KeysComeFromTheDailyChestAndTheHuntShop()
        {
            var content = ContentFixture.Content();
            Assert.That(content.Quests.DailyChests.Last().Rewards.Any(r => r.Id == Ids.RealmKey), Is.True);
            Assert.That(content.Hunts.Shop.Any(i => i.Gives.Any(g => g.Id == Ids.RealmKey)), Is.True);
        }

        [Test]
        public void RealmBattlesDoNotMixWithDungeons()
        {
            var ctx = Player();
            new BeginRealm { RealmId = "ash", Level = 1 }.Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new FinishDungeon().Apply(ctx)).Code, Is.EqualTo("no_battle"));
            Assert.That(Assert.Throws<RuleViolation>(() => new FinishStageBattle().Apply(ctx)).Code, Is.EqualTo("no_battle"));
            Assert.DoesNotThrow(() => new FinishRealm().Apply(ctx));
        }
    }
}
