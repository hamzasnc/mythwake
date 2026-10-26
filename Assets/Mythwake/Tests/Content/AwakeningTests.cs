using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Guardian Awakening (docs/systems/AWAKENING.md).</summary>
    public sealed class AwakeningTests
    {
        private static CommandContext Player(string heroId = "elara", int stars = 6, int stage = 60, bool shrine = true)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = 1_799_056_800_000 };
            new CreatePlayer { Seed = 41 }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = stage;
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone, Ids.GuardianSigil, Ids.FragmentDust, Ids.RelicMaterials })
                ctx.State.Add(id, 10_000_000);
            if (shrine)
            {
                ctx.State.Campaign.HighestCleared = System.Math.Max(stage, 60);
                new UpgradeBuilding { BuildingId = "guardianShrine" }.Apply(ctx);
                ctx.State.Campaign.HighestCleared = stage;
            }
            ctx.State.Heroes.Add(new OwnedHero { HeroId = heroId, Level = 100, Stars = stars });
            ctx.State.Formation = new[] { null, null, heroId, null, null };
            return ctx;
        }

        private static string Code(CommandContext ctx, string hero) =>
            Assert.Throws<RuleViolation>(() => new AwakenHero { HeroId = hero }.Apply(ctx)).Code;

        [Test]
        public void TheDocsFourExamplesCanAwaken()
        {
            var content = ContentFixture.Content();
            Assert.That(content.Awakening.Heroes.Select(a => a.HeroId), Is.EquivalentTo(new[] { "elara", "kaelen", "veyl", "arion" }));
        }

        [Test]
        public void AwakeningNeedsShrineStageStarsAndCost()
        {
            Assert.That(Code(Player(shrine: false), "elara"), Is.EqualTo("awakening_locked"));
            Assert.That(Code(Player(stage: 59), "elara"), Is.EqualTo("awakening_stage"));
            Assert.That(Code(Player(stars: 5), "elara"), Is.EqualTo("awakening_stars"));
            var poor = Player();
            poor.State.Currencies[Ids.GuardianSigil] = 10;
            Assert.That(Code(poor, "elara"), Is.EqualTo("not_enough"));
            var other = Player("thalindra");
            Assert.That(Code(other, "thalindra"), Is.EqualTo("no_awakening"));

            var ctx = Player();
            var sigils = ctx.State.Get(Ids.GuardianSigil);
            new AwakenHero { HeroId = "elara" }.Apply(ctx);
            Assert.That(ctx.State.FindHero("elara").Awakened, Is.True);
            Assert.That(ctx.State.Get(Ids.GuardianSigil), Is.EqualTo(sigils - 120));
            Assert.That(Code(ctx, "elara"), Is.EqualTo("already_awakened"));
        }

        [Test]
        public void AnAwakenedHeroIsTranscendentStrongerAndCarriesItsTrait()
        {
            var ctx = Player("kaelen");
            var before = Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation).Single();
            new AwakenHero { HeroId = "kaelen" }.Apply(ctx);
            var after = Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation).Single();
            Assert.That(after.Stats.Attack, Is.GreaterThan(before.Stats.Attack));
            Assert.That(after.Passives.Any(p => p.Kind == PassiveKind.Lifesteal), Is.True);
            Assert.That(after.Passives.Any(p => p.Team), Is.True, "Trait");
            Assert.That(Progression.MaxStars(ctx.Content, ctx.State.FindHero("kaelen")), Is.EqualTo(7));
            ctx.State.Add(Ids.Shards("kaelen"), 1000);
            Assert.That(new StarUpHero { HeroId = "kaelen" }.Apply(ctx), Is.EqualTo(7));
        }

        [Test]
        public void ArionsAwakeningGivesTheTeamStartingEnergy()
        {
            var ctx = Player("arion");
            new AwakenHero { HeroId = "arion" }.Apply(ctx);
            var setup = new BattleSetup { Seed = 1, Allies = Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation) };
            var boss = ctx.Content.Hero("obsidian_vault_sentinel");
            setup.Enemies.Add(new UnitSetup { Hero = boss, Slot = 0, Stats = boss.BaseStats.Clone() });
            var result = BattleSimulator.Run(setup);
            Assert.That(result.Events.Any(e => e.Type == BattleEventType.EnergyChanged && e.TimeMs == 0 && e.Source == 0 && e.Value >= 150), Is.True);
        }
    }
}
