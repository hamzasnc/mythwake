using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Hero Leader abilities in battle (doc: Hero Leaders; kit: Äbtissin Seraphine).</summary>
    public sealed class LeaderAbilityTests
    {
        private static (CommandContext ctx, BattleSetup setup) Battle(bool leader)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = 1_799_056_800_000 };
            new CreatePlayer { Seed = 31 }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = 40;
            foreach (var h in ctx.State.Heroes)
            {
                h.Level = 40;
                h.Stars = 3;
            }
            if (leader)
            {
                ctx.State.Heroes.Add(new OwnedHero { HeroId = "seraphine", Level = 40, Stars = 3 });
                ctx.State.Leader = "seraphine";
            }
            new BeginStageBattle().Apply(ctx);
            return (ctx, Campaign.BuildBattle(ctx.State, ctx.Content, ctx.State.Battle));
        }

        [Test]
        public void TheLeaderStandsOffTheFieldAndUsesItsAbilities()
        {
            var (_, setup) = Battle(true);
            Assert.That(setup.Leader?.Hero.Id, Is.EqualTo("seraphine"));
            var result = BattleSimulator.Run(setup);
            var leaderIndex = setup.Allies.Count + setup.Enemies.Count;
            Assert.That(result.Units.Any(u => u.Index == leaderIndex), Is.False, "not a combatant");
            Assert.That(result.Events.Any(e => e.Target == leaderIndex && e.Type == BattleEventType.Hit), Is.False, "never hit");
            var casts = result.Events.Where(e => e.Type == BattleEventType.ActionStart && e.Source == leaderIndex).ToList();
            if (result.DurationMs > 8000)
                Assert.That(casts.Any(e => e.Skill == SkillSlot.Special2), Is.True, "Stimme der Wahrheit after 8 s");
            Assert.That(result.Events.Any(e => e.Source == leaderIndex && e.Type == BattleEventType.StatusApplied && e.Status == StatusKind.Confuse)
                        || result.DurationMs <= 8000, Is.True);
        }

        [Test]
        public void SchleiersegenShieldsTheTeamOnceWhenAnAllyFallsLow()
        {
            var (_, setup) = Battle(true);
            // make the fight hard so an ally drops below 20 %
            foreach (var e in setup.Enemies)
                e.Stats.Attack *= 4;
            var result = BattleSimulator.Run(setup);
            var leaderIndex = setup.Allies.Count + setup.Enemies.Count;
            var shieldCasts = result.Events.Count(e => e.Type == BattleEventType.ActionStart && e.Source == leaderIndex && e.Skill == SkillSlot.Special);
            Assert.That(shieldCasts, Is.EqualTo(1));
            Assert.That(result.Events.Any(e => e.Type == BattleEventType.ShieldGained && e.Source == leaderIndex), Is.True);
        }

        [Test]
        public void WithoutALeaderNothingChanges()
        {
            var (_, setup) = Battle(false);
            Assert.That(setup.Leader, Is.Null);
            Assert.DoesNotThrow(() => BattleSimulator.Run(setup));
        }

        [Test]
        public void ArenaAndTrialsKeepNoLeader()
        {
            var (ctx, _) = Battle(true);
            new FinishStageBattle().Apply(ctx);
            var team = TeamSnapshot.Of(Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation));
            var arena = Arena.Setup(ctx.Content, team, team, 7);
            Assert.That(arena.Leader, Is.Null);
        }
    }
}
