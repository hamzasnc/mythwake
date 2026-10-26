using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Core.Progression;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Gear: equipping, enhancing, set bonus, Broken Forge crafting, boss drops.</summary>
    public sealed class GearTests
    {
        private const long Start = 1_800_000_000_000;

        private static CommandContext NewPlayer()
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = Start };
            new CreatePlayer { Seed = 21 }.Apply(ctx);
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone, Ids.GearMaterials })
                ctx.State.Add(id, 10_000_000);
            return ctx;
        }

        private static GearItem Give(CommandContext ctx, GearSlot slot, Grade grade, Faction set) =>
            Gear.Create(ctx.State, slot, grade, set);

        private static Stats Battle(CommandContext ctx, string heroId) =>
            Campaign.BuildBattle(ctx.State, ctx.Content, new PendingBattle { Stage = 1, Seed = 1, Formation = ctx.State.Formation })
                .Allies.First(u => u.Hero.Id == heroId).Stats;

        [Test]
        public void EquippedGearRaisesBattleStats()
        {
            var ctx = NewPlayer();
            var before = Battle(ctx, "dorn").Attack;
            var weapon = Give(ctx, GearSlot.Weapon, Grade.Rare, Faction.GuildsOfTheHunt);
            new EquipGear { HeroId = "dorn", Uid = weapon.Uid }.Apply(ctx);
            Assert.That(Battle(ctx, "dorn").Attack, Is.GreaterThan(before));
        }

        [Test]
        public void OnePiecePerSlotAndPiecesMoveBetweenHeroes()
        {
            var ctx = NewPlayer();
            var a = Give(ctx, GearSlot.Armor, Grade.Common, Faction.OrderOfTheVeil);
            var b = Give(ctx, GearSlot.Armor, Grade.Rare, Faction.OrderOfTheVeil);
            new EquipGear { HeroId = "dorn", Uid = a.Uid }.Apply(ctx);
            new EquipGear { HeroId = "dorn", Uid = b.Uid }.Apply(ctx);
            Assert.That(a.EquippedBy, Is.Null);
            Assert.That(Gear.Equipped(ctx.State, "dorn", GearSlot.Armor), Is.SameAs(b));
            new EquipGear { HeroId = "maren", Uid = b.Uid }.Apply(ctx);
            Assert.That(Gear.Equipped(ctx.State, "dorn", GearSlot.Armor), Is.Null);
        }

        [Test]
        public void FullSetGrantsTheSetBonus()
        {
            var ctx = NewPlayer();
            foreach (var slot in new[] { GearSlot.Weapon, GearSlot.Armor, GearSlot.Boots })
                new EquipGear { HeroId = "dorn", Uid = Give(ctx, slot, Grade.Epic, Faction.TribeOfTheCrimsonFang).Uid }.Apply(ctx);
            var three = Gear.StatsFor(ctx.State, ctx.Content, "dorn");
            Assert.That(three.SetBonusActive, Is.False);

            new EquipGear { HeroId = "dorn", Uid = Give(ctx, GearSlot.Accessory, Grade.Rare, Faction.TribeOfTheCrimsonFang).Uid }.Apply(ctx);
            var full = Gear.StatsFor(ctx.State, ctx.Content, "dorn");
            Assert.That(full.SetBonusActive, Is.True);
            // the lowest grade in the set (Rare) decides the bonus
            Assert.That(full.Attack, Is.EqualTo(three.Attack * (1000 + Gear.GradeStats(ctx.Content, Grade.Rare).SetBonus) / 1000));
        }

        [Test]
        public void MixedSetsGiveNoSetBonus()
        {
            var ctx = NewPlayer();
            var sets = new[] { Faction.OrderOfTheVeil, Faction.OrderOfTheVeil, Faction.OrderOfTheVeil, Faction.CircleOfAether };
            for (var i = 0; i < 4; i++)
                new EquipGear { HeroId = "dorn", Uid = Give(ctx, (GearSlot)i, Grade.Rare, sets[i]).Uid }.Apply(ctx);
            Assert.That(Gear.StatsFor(ctx.State, ctx.Content, "dorn").SetBonusActive, Is.False);
        }

        [Test]
        public void EnhancingCostsMoreEachLevelAndStopsAtTheGradeCap()
        {
            var ctx = NewPlayer();
            var item = Give(ctx, GearSlot.Weapon, Grade.Common, Faction.OrderOfTheVeil);
            var first = Gear.EnhanceCost(ctx.Content, item).First(c => c.Id == Ids.Gold).Value;
            new EnhanceGear { Uid = item.Uid }.Apply(ctx);
            Assert.That(Gear.EnhanceCost(ctx.Content, item).First(c => c.Id == Ids.Gold).Value, Is.GreaterThan(first));
            var cap = Gear.GradeStats(ctx.Content, Grade.Common).MaxLevel;
            while (item.Level < cap)
                new EnhanceGear { Uid = item.Uid }.Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new EnhanceGear { Uid = item.Uid }.Apply(ctx)).Code, Is.EqualTo("gear_max_level"));
        }

        [Test]
        public void SalvageReturnsMaterialsButNotForWornGear()
        {
            var ctx = NewPlayer();
            var item = Give(ctx, GearSlot.Boots, Grade.Epic, Faction.TheHollowed);
            new EquipGear { HeroId = "dorn", Uid = item.Uid }.Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new SalvageGear { Uid = item.Uid }.Apply(ctx)).Code, Is.EqualTo("gear_equipped"));
            new UnequipGear { Uid = item.Uid }.Apply(ctx);
            var before = ctx.State.Get(Ids.GearMaterials);
            new SalvageGear { Uid = item.Uid }.Apply(ctx);
            Assert.That(ctx.State.Get(Ids.GearMaterials), Is.GreaterThan(before));
            Assert.That(ctx.State.Gear, Is.Empty);
        }

        [Test]
        public void CraftingNeedsTheForgeAndHigherLevelsCraftBetterGear()
        {
            var ctx = NewPlayer();
            Assert.That(Assert.Throws<RuleViolation>(() => new CraftGear { Slot = GearSlot.Weapon }.Apply(ctx)).Code, Is.EqualTo("forge_locked"));

            ctx.State.Campaign.HighestCleared = 999;
            new UpgradeBuilding { BuildingId = "brokenForge" }.Apply(ctx);
            var low = Enumerable.Range(0, 200).Select(_ => new CraftGear { Slot = GearSlot.Weapon }.Apply(ctx)).ToList();
            Assert.That(low.All(g => g.Slot == GearSlot.Weapon), Is.True);
            Assert.That(low.Max(g => g.Grade), Is.LessThanOrEqualTo(Grade.Rare), "forge level 1 crafts Common and Rare only");

            while (Village.Level(ctx.State, Village.Building(ctx.Content, "brokenForge")) < 10)
                new UpgradeBuilding { BuildingId = "brokenForge" }.Apply(ctx);
            var high = Enumerable.Range(0, 200).Select(_ => new CraftGear { Slot = GearSlot.Armor }.Apply(ctx)).ToList();
            Assert.That(high.Any(g => g.Grade == Grade.Legendary), Is.True);
            Assert.That(high.Average(g => (int)g.Grade), Is.GreaterThan(low.Average(g => (int)g.Grade)));
        }

        [Test]
        public void ForgeHeroesImproveTheOdds()
        {
            var ctx = NewPlayer();
            ctx.State.Campaign.HighestCleared = 999;
            for (var i = 0; i < 6; i++)
                new UpgradeBuilding { BuildingId = "brokenForge" }.Apply(ctx);
            var plain = Gear.CraftWeights(ctx.State, ctx.Content);
            new AssignVillageHero { BuildingId = "brokenForge", Slot = 0, HeroId = "dorn" }.Apply(ctx);
            var helped = Gear.CraftWeights(ctx.State, ctx.Content);
            Assert.That(helped[3], Is.GreaterThan(plain[3]));
            Assert.That(helped[0], Is.EqualTo(plain[0]));
        }

        [Test]
        public void BossStagesDropGearAndEveryClearGivesMaterials()
        {
            var ctx = NewPlayer();
            var boss = ctx.Content.Campaign.BossEvery;
            // a strong formation so the boss is cleared
            foreach (var h in ctx.State.Heroes)
                h.Level = 30;
            for (var stage = 1; stage <= boss; stage++)
            {
                new BeginStageBattle().Apply(ctx);
                var result = new FinishStageBattle().Apply(ctx);
                Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Victory), $"stage {stage}");
                Assert.That(result.Rewards.Any(r => r.Id == Ids.GearMaterials), Is.True);
                Assert.That(result.Gear.Count, Is.EqualTo(stage == boss ? 1 : 0), $"stage {stage}");
            }
            Assert.That(ctx.State.Gear.Count, Is.EqualTo(1));
        }

        [Test]
        public void GearStatsShowInPower()
        {
            var ctx = NewPlayer();
            var owned = ctx.State.FindHero("dorn");
            var before = TeamBuilder.Power(TeamBuilder.StatsOf(Formations.Progress(ctx.State, ctx.Content, owned)));
            new EquipGear { HeroId = "dorn", Uid = Give(ctx, GearSlot.Armor, Grade.Legendary, Faction.OrderOfTheVeil).Uid }.Apply(ctx);
            var after = TeamBuilder.Power(TeamBuilder.StatsOf(Formations.Progress(ctx.State, ctx.Content, owned)));
            Assert.That(after, Is.GreaterThan(before));
        }
    }
}
