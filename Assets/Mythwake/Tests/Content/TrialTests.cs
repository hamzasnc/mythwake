using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Trial of Fragments (docs/systems/TRIALS.md).</summary>
    public sealed class TrialTests
    {
        // Monday 2027-01-04 10:00 UTC
        private const long Monday = 1_799_056_800_000;

        /// <summary>A player with the Shrine at <paramref name="shrineLevel"/> who owns every hero of every Aspect.</summary>
        private static CommandContext Player(int shrineLevel = 4, int heroLevel = 60, int stars = 5)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = Monday };
            new CreatePlayer { Seed = 17 }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = 40;
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone })
                ctx.State.Add(id, 50_000_000);
            for (var i = 0; i < shrineLevel; i++)
                new UpgradeBuilding { BuildingId = "fragmentShrine" }.Apply(ctx);
            foreach (var h in ctx.Content.Heroes.Values)
                if (ctx.State.FindHero(h.Id) == null)
                    ctx.State.Heroes.Add(new OwnedHero { HeroId = h.Id });
            foreach (var h in ctx.State.Heroes)
            {
                h.Level = heroLevel;
                h.Stars = stars;
            }
            return ctx;
        }

        private static string[] Team(CommandContext ctx, Aspect aspect) => Trials.SuggestedTeam(ctx.State, ctx.Content, aspect);

        private static TrialResult Climb(CommandContext ctx, Aspect aspect)
        {
            new BeginTrial { Aspect = aspect, Slots = Team(ctx, aspect) }.Apply(ctx);
            return new FinishTrial().Apply(ctx);
        }

        [Test]
        public void TrialsOpenWithFragmentShrineLevelFour()
        {
            var ctx = Player(shrineLevel: 3);
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginTrial { Aspect = Aspect.Ravak, Slots = Team(ctx, Aspect.Ravak) }.Apply(ctx)).Code,
                Is.EqualTo("trial_locked"));
            new UpgradeBuilding { BuildingId = "fragmentShrine" }.Apply(ctx);
            Assert.DoesNotThrow(() => new BeginTrial { Aspect = Aspect.Ravak, Slots = Team(ctx, Aspect.Ravak) }.Apply(ctx));
        }

        [Test]
        public void OnlyHeroesOfTheTowersAspectMayEnter()
        {
            var ctx = Player();
            var team = Team(ctx, Aspect.Ravak);
            Assert.That(team.Where(id => id != null).All(id => ctx.Content.Hero(id).Aspect == Aspect.Ravak), Is.True);
            var stranger = ctx.Content.Heroes.Values.First(h => h.Aspect == Aspect.Gorath && h.Role != Role.HeroLeader).Id;
            team[System.Array.IndexOf(team, team.First(id => id != null))] = stranger;
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginTrial { Aspect = Aspect.Ravak, Slots = team }.Apply(ctx)).Code,
                Is.EqualTo("trial_wrong_aspect"));
        }

        [Test]
        public void EveryTowerCanBeClimbedByAMidGameTeamOfItsAspect()
        {
            foreach (Aspect aspect in System.Enum.GetValues(typeof(Aspect)))
            {
                var ctx = Player();
                var r = Climb(ctx, aspect);
                Assert.That(r.Outcome, Is.EqualTo(BattleOutcome.Victory), aspect.ToString());
                Assert.That(Trials.Highest(ctx.State, aspect), Is.EqualTo(1));
                Assert.That(r.Rewards.Any(a => a.Id == Ids.FragmentDust), Is.True);
                Assert.That(ctx.State.Trials.Teams[aspect], Is.Not.Null, "team remembered");
            }
        }

        [Test]
        public void ALostFloorKeepsProgressAndPaysNothing()
        {
            var ctx = Player(heroLevel: 1, stars: 1);
            ctx.State.Trials.Highest[Aspect.Gorath] = 40;
            var dust = ctx.State.Get(Ids.FragmentDust);
            var r = Climb(ctx, Aspect.Gorath);
            Assert.That(r.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(r.Floor, Is.EqualTo(41));
            Assert.That(Trials.Highest(ctx.State, Aspect.Gorath), Is.EqualTo(40));
            Assert.That(ctx.State.Get(Ids.FragmentDust), Is.EqualTo(dust));
        }

        [Test]
        public void EveryTenthFloorGivesAnAspectFragmentOfTheTower()
        {
            var ctx = Player(heroLevel: 200, stars: 9);
            ctx.State.Trials.Highest[Aspect.Veloria] = 9;
            var r = Climb(ctx, Aspect.Veloria);
            Assert.That(r.Outcome, Is.EqualTo(BattleOutcome.Victory));
            var f = r.Fragments.Single();
            Assert.That(f.Slot, Is.EqualTo(FragmentSlot.Aspect));
            Assert.That(f.Aspect, Is.EqualTo(Aspect.Veloria));
            Assert.That(f.Grade, Is.EqualTo(Grade.Rare));
            Assert.That(Fragments.Effect(ctx.Content, f.Effect).Aspect, Is.EqualTo(Aspect.Veloria));
            Assert.That(Trials.MilestoneGrade(ctx.Content, 30), Is.EqualTo(Grade.Epic));
            Assert.That(Trials.MilestoneGrade(ctx.Content, 60), Is.EqualTo(Grade.Legendary));
        }

        [Test]
        public void EnemiesShareTheTowersAspectAndGrowWithTheFloor()
        {
            var content = ContentFixture.Content();
            foreach (Aspect aspect in System.Enum.GetValues(typeof(Aspect)))
            {
                Assert.That(Trials.Enemies(content, aspect, 1).All(u => u.Hero.Aspect == aspect), Is.True, aspect.ToString());
                Assert.That(Trials.Enemies(content, aspect, 1), Has.Count.EqualTo(3));
                Assert.That(Trials.Enemies(content, aspect, 12), Has.Count.EqualTo(5));
                Assert.That(Trials.Enemies(content, aspect, 10).All(u => u.Variant == "boss"), Is.True);
                Assert.That(Trials.Enemies(content, aspect, 50).Max(u => u.Stats.Attack),
                    Is.GreaterThan(Trials.Enemies(content, aspect, 5).Max(u => u.Stats.Attack)));
            }
        }

        [Test]
        public void EachTowerGivesItsEnemiesTheDocMechanic()
        {
            var ctx = Player();
            // a floor where the fight lasts long enough for many enemy hits
            ctx.State.Trials.Highest[Aspect.Somnara] = 25;
            new BeginTrial { Aspect = Aspect.Somnara, Slots = Team(ctx, Aspect.Somnara) }.Apply(ctx);
            var setup = Trials.BuildBattle(ctx.State, ctx.Content, ctx.State.Battle);
            Assert.That(setup.Rules.EnemyPassives.Any(p => p.Effect?.Status?.Kind == StatusKind.Slow), Is.True);
            var result = BattleSimulator.Run(setup);
            var allies = setup.Allies.Count;
            Assert.That(result.Events.Any(e => e.Type == BattleEventType.StatusApplied && e.Status == StatusKind.Slow && e.Target < allies), Is.True,
                "enemies slow the team");

            var gorath = Player();
            new BeginTrial { Aspect = Aspect.Gorath, Slots = Team(gorath, Aspect.Gorath) }.Apply(gorath);
            var g = Trials.BuildBattle(gorath.State, gorath.Content, gorath.State.Battle);
            var gr = BattleSimulator.Run(g);
            Assert.That(gr.Events.Any(e => e.Type == BattleEventType.Heal && e.Target >= g.Allies.Count), Is.True, "enemies regenerate");
        }

        [Test]
        public void TrialBattlesUseNoLeaderAndDoNotMixWithOtherBattles()
        {
            var ctx = Player();
            var leader = ctx.Content.Heroes.Values.FirstOrDefault(h => h.Role == Role.HeroLeader);
            if (leader != null)
                ctx.State.Leader = leader.Id;
            var setup = new BeginTrial { Aspect = Aspect.Envyra, Slots = Team(ctx, Aspect.Envyra) }.Apply(ctx);
            var plain = Campaign.Allies(ctx.State, ctx.Content, ctx.State.Battle.Formation, withLeader: false);
            Assert.That(setup.Allies.Select(u => u.Stats.Attack), Is.EqualTo(plain.Select(u => u.Stats.Attack)));
            Assert.That(Assert.Throws<RuleViolation>(() => new FinishStageBattle().Apply(ctx)).Code, Is.EqualTo("no_battle"));
            Assert.That(Assert.Throws<RuleViolation>(() => new FinishDungeon().Apply(ctx)).Code, Is.EqualTo("no_battle"));
            Assert.DoesNotThrow(() => new FinishTrial().Apply(ctx));
        }
    }
}
