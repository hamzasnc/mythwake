using System.Linq;
using Mythwake.Core.Content;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Who runs, who shoots, who hits from where it stands (presentation of melee and ranged units).</summary>
    public sealed class AttackRangeTests
    {
        [Test]
        public void FrontRowFightsInMeleeAndBackRowShootsUnlessTheDataSaysOtherwise()
        {
            var content = ContentFixture.Content();
            Assert.That(content.Hero("dorn").AttackRangeKind, Is.EqualTo(AttackRange.Melee), "front-row swordsman");
            Assert.That(content.Hero("pyra").AttackRangeKind, Is.EqualTo(AttackRange.Ranged), "back-row fire mage");
            Assert.That(content.Hero("kiro").AttackRangeKind, Is.EqualTo(AttackRange.Melee), "back-row assassin runs in");
            Assert.That(content.Hero("corrupted_crow").AttackRangeKind, Is.EqualTo(AttackRange.Melee), "the crow swoops in");
        }

        [Test]
        public void HugeBossesHitFromWhereTheyStand()
        {
            var content = ContentFixture.Content();
            var huge = content.Monsters.Values.Where(m => m.Body == "huge").ToList();
            Assert.That(huge, Is.Not.Empty);
            foreach (var m in huge)
                Assert.That(m.AttackRangeKind, Is.EqualTo(AttackRange.Reach), m.Id);
        }
    }
}
