using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Skill upgrades (docs/systems/SKILLS.md).</summary>
    public sealed class SkillUpgradeTests
    {
        private static (CommandContext ctx, string hero) Player(int stars = 3)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = 1_799_056_800_000 };
            new CreatePlayer { Seed = 13 }.Apply(ctx);
            ctx.State.Add(Ids.UpgradeStones, 100_000);
            ctx.State.Add(Ids.Gold, 100_000_000);
            var hero = ctx.State.Formation.First(h => h != null);
            ctx.State.FindHero(hero).Stars = stars;
            return (ctx, hero);
        }

        [Test]
        public void SkillsLevelUpToALimitSetByStars()
        {
            var (ctx, hero) = Player(stars: 3);
            var owned = ctx.State.FindHero(hero);
            var max = Skills.MaxLevel(ctx.Content, owned);
            Assert.That(max, Is.EqualTo(6));
            for (var l = 1; l < max; l++)
                Assert.That(new UpgradeSkill { HeroId = hero, Slot = SkillSlot.Normal }.Apply(ctx), Is.EqualTo(l + 1));
            Assert.That(Assert.Throws<RuleViolation>(() => new UpgradeSkill { HeroId = hero, Slot = SkillSlot.Normal }.Apply(ctx)).Code, Is.EqualTo("skill_max_level"));
            Assert.That(Assert.Throws<RuleViolation>(() => new UpgradeSkill { HeroId = hero, Slot = SkillSlot.Passive }.Apply(ctx)).Code, Is.EqualTo("no_skill"));
        }

        [Test]
        public void UpgradesCostStonesAndGold()
        {
            var (ctx, hero) = Player();
            var stones = ctx.State.Get(Ids.UpgradeStones);
            new UpgradeSkill { HeroId = hero, Slot = SkillSlot.Ultimate }.Apply(ctx);
            Assert.That(ctx.State.Get(Ids.UpgradeStones), Is.EqualTo(stones - 5));
            ctx.State.Currencies.Remove(Ids.UpgradeStones);
            Assert.Throws<RuleViolation>(() => new UpgradeSkill { HeroId = hero, Slot = SkillSlot.Ultimate }.Apply(ctx));
        }

        [Test]
        public void HigherSkillLevelsHitHarder()
        {
            long Damage(int levels)
            {
                var (ctx, hero) = Player(stars: 7);
                for (var i = 0; i < levels; i++)
                    new UpgradeSkill { HeroId = hero, Slot = SkillSlot.Normal }.Apply(ctx);
                ctx.State.Formation = new string[5];
                ctx.State.Formation[ctx.Content.Hero(hero).Row == Row.Front ? 0 : 2] = hero;
                var setup = new BattleSetup { Seed = 2, Allies = Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation) };
                var dummy = ctx.Content.Hero("obsidian_vault_sentinel");
                setup.Enemies.Add(new UnitSetup { Hero = dummy, Slot = 0, Stats = dummy.BaseStats.Clone() });
                setup.Enemies[0].Stats.Hp *= 100;
                setup.Enemies[0].Stats.Attack = 1;
                var result = BattleSimulator.Run(setup);
                return result.Events.Where(e => e.Type == BattleEventType.Hit && e.Source == 0 && e.Skill == SkillSlot.Normal).Take(5).Sum(e => e.Value);
            }
            Assert.That(Damage(5), Is.GreaterThan(Damage(0)));
        }

        [Test]
        public void UpgradeStonesHaveSources()
        {
            var content = ContentFixture.Content();
            Assert.That(content.Trials.Rewards.Any(r => r.Id == Ids.UpgradeStones), Is.True, "doc: Trial of Fragments rewards upgrade stones");
            Assert.That(content.Campaign.BossRewards.Any(r => r.Id == Ids.UpgradeStones), Is.True);
            Assert.That(content.Shops.Shops.First(s => s.Id == "general").Items.Any(i => i.Gives.Any(g => g.Id == Ids.UpgradeStones)), Is.True);
        }
    }
}
