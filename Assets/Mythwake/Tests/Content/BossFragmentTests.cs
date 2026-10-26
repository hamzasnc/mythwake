using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Boss Fragments (docs/systems/BOSS_FRAGMENTS.md).</summary>
    public sealed class BossFragmentTests
    {
        private const string Vault = "bf_obsidian_vault_sentinel";

        private static CommandContext Player(int heroLevel = 150, int stars = 7, int materialCleared = 5)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = 1_799_056_800_000 };
            new CreatePlayer { Seed = 61 }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = 60;
            ctx.State.Add(Ids.FragmentDust, 1_000_000);
            if (materialCleared > 0)
                ctx.State.Dungeons.Highest["material"] = materialCleared;
            foreach (var h in ctx.State.Heroes)
            {
                h.Level = heroLevel;
                h.Stars = stars;
            }
            return ctx;
        }

        private static BossChest Refight(CommandContext ctx, string id = Vault)
        {
            new BeginBossRefight { FragmentId = id }.Apply(ctx);
            return new FinishBossRefight().Apply(ctx);
        }

        [Test]
        public void TheFourDungeonBossesHaveFragments()
        {
            var content = ContentFixture.Content();
            Assert.That(content.BossFragments.Fragments.Select(f => f.Dungeon), Is.EquivalentTo(content.Dungeons.Dungeons.Select(d => d.Id)));
        }

        [Test]
        public void RefightsOpenAfterTheDungeonAndShareSevenDailyEntries()
        {
            var locked = Player(materialCleared: 0);
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginBossRefight { FragmentId = Vault }.Apply(locked)).Code, Is.EqualTo("boss_refight_locked"));

            var ctx = Player();
            for (var i = 0; i < 7; i++)
                Assert.That(Refight(ctx).Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginBossRefight { FragmentId = Vault }.Apply(ctx)).Code, Is.EqualTo("boss_no_entries"));
            ctx.NowMs += Quests.DayMs;
            Assert.That(BossFragments.EntriesLeft(ctx.State, ctx.Content, ctx.NowMs), Is.EqualTo(7), "entries reset and do not stack");
        }

        [Test]
        public void ALostRefightUsesNoEntry()
        {
            var ctx = Player(heroLevel: 1, stars: 1, materialCleared: 60);
            Assert.That(Refight(ctx).Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(BossFragments.EntriesLeft(ctx.State, ctx.Content, ctx.NowMs), Is.EqualTo(7));
        }

        [Test]
        public void ChestsGiveACopyAtTheLatestEveryFifthTime()
        {
            var ctx = Player();
            var chests = Enumerable.Range(0, 5).Select(_ => Refight(ctx)).ToList();
            Assert.That(chests.Any(c => c.Copy), Is.True);
            Assert.That(chests.Count(c => c.Unlocked), Is.EqualTo(1));
            Assert.That(chests.All(c => c.Rewards.Any(r => r.Id == Ids.FragmentDust)), Is.True);
            Assert.That(BossFragments.Level(ctx.State, Vault), Is.EqualTo(1));
        }

        [Test]
        public void UpgradingNeedsCopiesAndDust()
        {
            var ctx = Player();
            Assert.That(Assert.Throws<RuleViolation>(() => new UpgradeBossFragment { FragmentId = Vault }.Apply(ctx)).Code, Is.EqualTo("boss_fragment_locked"));
            ctx.State.BossFragments.Owned[Vault] = new BossFragmentProgress { Level = 1, Copies = 0 };
            Assert.That(Assert.Throws<RuleViolation>(() => new UpgradeBossFragment { FragmentId = Vault }.Apply(ctx)).Code, Is.EqualTo("boss_fragment_copies"));
            ctx.State.BossFragments.Owned[Vault].Copies = 3;
            Assert.That(new UpgradeBossFragment { FragmentId = Vault }.Apply(ctx), Is.EqualTo(2));
            Assert.That(ctx.State.BossFragments.Owned[Vault].Copies, Is.EqualTo(2));
            Assert.That(BossFragments.MainValue(ctx.Content, BossFragments.Get(ctx.Content, Vault), 2),
                Is.GreaterThan(BossFragments.MainValue(ctx.Content, BossFragments.Get(ctx.Content, Vault), 1)));
        }

        [Test]
        public void TheEquippedFragmentFiresInCampaignBattles()
        {
            var ctx = Player();
            Assert.That(Assert.Throws<RuleViolation>(() => new EquipBossFragment { FragmentId = Vault }.Apply(ctx)).Code, Is.EqualTo("boss_fragment_locked"));
            ctx.State.BossFragments.Owned[Vault] = new BossFragmentProgress { Level = 1 };
            new EquipBossFragment { FragmentId = Vault }.Apply(ctx);
            new BeginStageBattle().Apply(ctx);
            var setup = Campaign.BuildBattle(ctx.State, ctx.Content, ctx.State.Battle);
            Assert.That(setup.BossFragment, Is.Not.Null);
            foreach (var e in setup.Enemies)
                e.Stats.Hp *= 50;
            var result = BattleSimulator.Run(setup);
            var index = setup.Allies.Count + setup.Enemies.Count + 1;
            Assert.That(result.Events.Any(e => e.Type == BattleEventType.ShieldGained && e.Source == index), Is.True);
            Assert.That(result.Units.Any(u => u.Index == index), Is.False, "off the field");
        }

        [Test]
        public void ThePassiveFragmentBuffsEveryAlly()
        {
            var ctx = Player();
            const string devourer = "bf_fragment_devourer";
            ctx.State.Dungeons.Highest["fragment"] = 1;
            ctx.State.BossFragments.Owned[devourer] = new BossFragmentProgress { Level = 3 };
            new EquipBossFragment { FragmentId = devourer }.Apply(ctx);
            new BeginStageBattle().Apply(ctx);
            var setup = Campaign.BuildBattle(ctx.State, ctx.Content, ctx.State.Battle);
            Assert.That(setup.BossFragment, Is.Null, "a passive Fragment has no ability");
            Assert.That(setup.Allies.All(u => u.Passives.Any(p => p.Kind == PassiveKind.DamageBonus && p.Value > 80)), Is.True);
        }

        [Test]
        public void TheArenaDoesNotUseBossFragments()
        {
            var ctx = Player();
            ctx.State.BossFragments.Owned[Vault] = new BossFragmentProgress { Level = 1 };
            new EquipBossFragment { FragmentId = Vault }.Apply(ctx);
            var team = TeamSnapshot.Of(Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation));
            Assert.That(Arena.Setup(ctx.Content, team, team, 3).BossFragment, Is.Null);
        }
    }
}
