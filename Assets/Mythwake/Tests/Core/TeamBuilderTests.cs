using Mythwake.Core.Content;
using Mythwake.Core.Progression;
using NUnit.Framework;
using static Mythwake.Core.Tests.TestHeroes;

namespace Mythwake.Core.Tests
{
    public sealed class TeamBuilderTests
    {
        [Test]
        public void LevelAndStarsScaleStats()
        {
            var hero = Fighter("a", hp: 1000, atk: 100);
            var s = TeamBuilder.StatsAt(hero, 11, 2);
            // +80 permille per level (10 levels = +800), +150 for the second star
            Assert.That(s.Attack, Is.EqualTo(100 * 1800 / 1000 * 1150 / 1000));
        }

        [Test]
        public void FactionSynergyFollowsTheDocSteps()
        {
            Assert.That(TeamBuilder.FactionBonus(1), Is.EqualTo(0));
            Assert.That(TeamBuilder.FactionBonus(2), Is.GreaterThan(0));
            Assert.That(TeamBuilder.FactionBonus(3), Is.GreaterThan(TeamBuilder.FactionBonus(2)));
            Assert.That(TeamBuilder.FactionBonus(5), Is.GreaterThan(TeamBuilder.FactionBonus(3)));
        }

        [Test]
        public void SameFactionTeamGetsBonus()
        {
            var a = new HeroProgress { Hero = Fighter("a", atk: 1000, faction: Faction.OrderOfTheVeil, race: Race.Veyr) };
            var b = new HeroProgress { Hero = Fighter("b", atk: 1000, faction: Faction.OrderOfTheVeil, race: Race.Human) };
            var c = new HeroProgress { Hero = Fighter("c", atk: 1000, faction: Faction.CircleOfAether, race: Race.Drakari) };
            var team = TeamBuilder.Build(new[] { a, b, c });
            Assert.That(team[0].Stats.Attack, Is.EqualTo(1050));
            Assert.That(team[2].Stats.Attack, Is.EqualTo(1000));
        }

        [Test]
        public void HeroLeaderAuraBoostsTheWholeTeam()
        {
            var leaderHero = Fighter("leader");
            leaderHero.Role = Role.HeroLeader;
            leaderHero.Passive = new SkillDefinition
            {
                Id = "aura", Slot = SkillSlot.Passive, Trigger = { Kind = TriggerKind.Passive },
                Passives = { new PassiveDefinition { Kind = PassiveKind.LeaderAura, AttackBonus = 100, HpBonus = 100 } }
            };
            var a = new HeroProgress { Hero = Fighter("a", hp: 1000, atk: 1000, faction: Faction.OrderOfTheVeil) };
            var team = TeamBuilder.Build(new[] { a }, new HeroProgress { Hero = leaderHero });
            Assert.That(team[0].Stats.Attack, Is.EqualTo(1100));
            Assert.That(team[0].Stats.Hp, Is.EqualTo(1100));
        }
    }
}
