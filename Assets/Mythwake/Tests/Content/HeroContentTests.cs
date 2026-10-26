using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Progression;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Loads the real hero files and runs them through the rules.</summary>
    public sealed class HeroContentTests
    {
        private static List<HeroDefinition> _heroes;

        private static string HeroesDir() => ContentFixture.HeroesDir();

        private static List<HeroDefinition> Heroes() =>
            _heroes ??= ContentFixture.HeroFiles().Select(p => ContentJson.ParseHero(File.ReadAllText(p))).ToList();

        private static List<HeroDefinition> Fighters() => Heroes().Where(h => h.Role != Role.HeroLeader).ToList();

        [Test]
        public void AllHeroesLoad()
        {
            Assert.That(Heroes().Count, Is.EqualTo(40));
        }

        [Test]
        public void ContentIsValid()
        {
            var errors = ContentValidator.Validate(Heroes());
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void JsonRoundTripIsLossless()
        {
            foreach (var hero in Heroes())
            {
                var json = ContentJson.Write(hero);
                Assert.That(ContentJson.Write(ContentJson.ParseHero(json)), Is.EqualTo(json), hero.Id);
            }
        }

        [Test]
        public void SpineFilesSitNextToEveryHero()
        {
            foreach (var hero in Heroes())
            {
                var dir = Path.Combine(HeroesDir(), hero.Id);
                foreach (var file in new[] { $"{hero.SpineId}.json", $"{hero.SpineId}.atlas.txt", $"{hero.SpineId}.png" })
                    Assert.That(File.Exists(Path.Combine(dir, file)), Is.True, $"{hero.Id}: missing {file}");
            }
        }

        [Test]
        public void EveryFighterCastsEverySkillItHas()
        {
            // each hero (level 30) fights a sturdy dummy team long enough to use everything; catches dead triggers and crashes
            foreach (var hero in Fighters())
            {
                var team = TeamBuilder.Build(new[] { new HeroProgress { Hero = hero, Level = 30 } });
                team[0].Slot = hero.Row == Row.Front ? 0 : 2;
                var dummies = Fighters().Take(5).Select((h, i) => new UnitSetup
                {
                    Hero = h, Slot = i, Stats = TeamBuilder.StatsAt(h, 10, 1)
                }).ToList();
                foreach (var d in dummies)
                    d.Stats.Attack = 1; // make them harmless so the hero survives long enough
                var setup = new BattleSetup { Seed = 99, Rules = { MaxDurationMs = 60_000 } };
                setup.Allies.AddRange(team);
                setup.Enemies.AddRange(dummies);

                var result = BattleSimulator.Run(setup);
                var cast = result.Events.Where(e => e.Type == BattleEventType.ActionStart && e.Source == 0).Select(e => e.Skill).ToHashSet();
                Assert.That(cast, Does.Contain(SkillSlot.Normal), hero.Id);
                if (hero.Ultimate != null)
                    Assert.That(cast, Does.Contain(SkillSlot.Ultimate), $"{hero.Id}: ultimate never fired");
                if (hero.Special != null && IsUnconditional(hero.Special))
                    Assert.That(cast, Does.Contain(SkillSlot.Special), $"{hero.Id}: special never fired");
            }
        }

        private static bool IsUnconditional(SkillDefinition s) =>
            s.Trigger.Kind == TriggerKind.Cooldown || s.Trigger.Kind == TriggerKind.BattleStart;

        [Test]
        public void FullTeamBattlesAreDeterministicAndFinish()
        {
            var fighters = Fighters();
            for (var round = 0; round < 20; round++)
            {
                List<UnitSetup> Team(int offset) => TeamBuilder.Build(Enumerable.Range(0, 5)
                    .Select(i => new HeroProgress { Hero = fighters[(round * 7 + offset + i * 3) % fighters.Count], Level = 20 })
                    .ToList());

                BattleSetup Make() => new BattleSetup { Seed = (ulong)(1000 + round), Allies = Team(0), Enemies = Team(17) };

                var a = BattleSimulator.Run(Make());
                var b = BattleSimulator.Run(Make());
                Assert.That(b.Events.Count, Is.EqualTo(a.Events.Count), $"round {round}");
                Assert.That(b.Outcome, Is.EqualTo(a.Outcome));
                Assert.That(a.DurationMs, Is.LessThanOrEqualTo(90_000));
                Assert.That(a.Events.Last().Type, Is.EqualTo(BattleEventType.BattleEnd));
            }
        }
    }
}
