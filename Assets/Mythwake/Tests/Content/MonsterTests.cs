using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Core.Progression;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Monsters and bosses in the campaign (Tools/ContentPipeline/monsters/roster.json).</summary>
    public sealed class MonsterTests
    {
        private static GameContent Content => ContentFixture.Content();

        [Test]
        public void EveryDocBossExists()
        {
            foreach (var id in new[] { "hollowtusk_alpha", "riftborn_treant", "crimson_wraith_adept", "hollow_spire_warden",
                         "obsidian_vault_sentinel", "fragment_devourer", "corrupted_guardian_of_the_deep", "dragon_echo_of_the_sky",
                         "false_guardian_construct" })
            {
                Assert.That(Content.Monsters.ContainsKey(id), Is.True, id);
                Assert.That(Content.Monsters[id].Boss, Is.True, id);
            }
        }

        [Test]
        public void MonstersAreNeverSummonedOrOwned()
        {
            Assert.That(Content.Heroes.Keys.Intersect(Content.Monsters.Keys), Is.Empty);
            Assert.That(Content.Monsters.Values.All(m => m.Group != EnemyGroup.None), Is.True);
        }

        [Test]
        public void ChapterEndsWithItsBossAndEscort()
        {
            var chapter = Content.Campaign.Chapters[0];
            var team = Campaign.Enemies(Content, chapter.Stages);
            Assert.That(team.Count(u => u.Hero.Id == chapter.Boss), Is.EqualTo(1));
            Assert.That(team.Where(u => u.Hero.Id != chapter.Boss).All(u => chapter.Monsters.Contains(u.Hero.Id)), Is.True,
                "the boss is escorted by the chapter's monsters");
        }

        [Test]
        public void StagesMixHeroesAndMonsters()
        {
            var units = Enumerable.Range(1, 19).SelectMany(s => Campaign.Enemies(Content, s)).ToList();
            Assert.That(units.Any(u => Content.Monsters.ContainsKey(u.Hero.Id)), Is.True, "monsters appear");
            Assert.That(units.Any(u => Content.Heroes.ContainsKey(u.Hero.Id)), Is.True, "corrupted heroes still appear");
            Assert.That(units.Any(u => u.Hero.Boss), Is.False, "named bosses only at the end of a chapter");
        }

        [Test]
        public void EveryMonsterUsesItsKit()
        {
            foreach (var monster in Content.Monsters.Values)
            {
                var me = new UnitSetup { Hero = monster, Slot = monster.Row == Row.Front ? 0 : 2, Stats = TeamBuilder.StatsAt(monster, 30, 1) };
                var dummies = Content.Heroes.Values.Where(h => h.Role != Role.HeroLeader).OrderBy(h => h.Id).Take(5)
                    .Select((h, i) => new UnitSetup { Hero = h, Slot = i, Stats = TeamBuilder.StatsAt(h, 10, 1) }).ToList();
                foreach (var d in dummies)
                    d.Stats.Attack = 1;
                var setup = new BattleSetup { Seed = 5, Rules = { MaxDurationMs = 60_000 } };
                setup.Allies.Add(me);
                setup.Enemies.AddRange(dummies);
                var cast = BattleSimulator.Run(setup).Events.Where(e => e.Type == BattleEventType.ActionStart && e.Source == 0).Select(e => e.Skill).ToHashSet();
                Assert.That(cast, Does.Contain(SkillSlot.Normal), monster.Id);
                Assert.That(cast, Does.Contain(SkillSlot.Special), monster.Id);
                if (monster.Ultimate != null)
                    Assert.That(cast, Does.Contain(SkillSlot.Ultimate), monster.Id);
            }
        }

        [Test]
        public void ArenaSnapshotsCanContainMonsters()
        {
            var snapshot = TeamSnapshot.Of(Campaign.Enemies(Content, 20));
            Assert.DoesNotThrow(() => snapshot.ToSetups(Content));
        }
    }
}
