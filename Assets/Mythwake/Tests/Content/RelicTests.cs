using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Relics (docs/systems/RELICS.md).</summary>
    public sealed class RelicTests
    {
        // Monday 2027-01-04 10:00 UTC
        private const long Monday = 1_799_056_800_000;

        private static CommandContext Player(int shrineLevel = 2, int stage = 30, int heroLevel = 0)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = Monday };
            new CreatePlayer { Seed = 91 }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = stage;
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone, Ids.FragmentDust, Ids.RelicMaterials })
                ctx.State.Add(id, 50_000_000);
            for (var i = 0; i < shrineLevel; i++)
                new UpgradeBuilding { BuildingId = "fragmentShrine" }.Apply(ctx);
            if (heroLevel > 0)
                foreach (var h in ctx.State.Heroes)
                {
                    h.Level = heroLevel;
                    h.Stars = 6;
                }
            return ctx;
        }

        private static string FirstHero(CommandContext ctx) => ctx.State.Formation.First(h => h != null);

        private static UnitSetup Unit(CommandContext ctx, string heroId) =>
            Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation).First(u => u.Hero.Id == heroId);

        [Test]
        public void EveryChapterBossHasARelic()
        {
            var content = ContentFixture.Content();
            foreach (var c in content.Campaign.Chapters.Where(c => c.Boss != null))
                Assert.That(Relics.OfBoss(content, c.Boss), Is.Not.Null, c.Id);
        }

        [Test]
        public void ChapterBossDropsItsRelicOnItsDefeat()
        {
            var ctx = Player(shrineLevel: 0, stage: 19, heroLevel: 120);
            new BeginStageBattle().Apply(ctx);
            var r = new FinishStageBattle().Apply(ctx);
            Assert.That(r.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(r.Relics.Select(x => x.RelicId), Is.EqualTo(new[] { "relic_hollowtusk_tusk" }));
            Assert.That(ctx.State.Relics, Has.Count.EqualTo(1), "won before the slot is open, kept for later");

            var ctx2 = Player(shrineLevel: 0, stage: 9, heroLevel: 120);
            new BeginStageBattle().Apply(ctx2);
            Assert.That(new FinishStageBattle().Apply(ctx2).Relics, Is.Empty, "mid-chapter bosses give no Relic");
        }

        [Test]
        public void RelicSlotOpensWithShrineLevelTwoAndAddsTheEffect()
        {
            var ctx = Player(shrineLevel: 1);
            var hero = FirstHero(ctx);
            var relic = Relics.Create(ctx.State, "relic_dragon_scale");
            Assert.That(Assert.Throws<RuleViolation>(() => new EquipRelic { HeroId = hero, Uid = relic.Uid }.Apply(ctx)).Code, Is.EqualTo("relic_slot_locked"));
            new UpgradeBuilding { BuildingId = "fragmentShrine" }.Apply(ctx);
            new EquipRelic { HeroId = hero, Uid = relic.Uid }.Apply(ctx);
            Assert.That(Unit(ctx, hero).Passives.Any(p => p.Kind == PassiveKind.UltimateDamage), Is.True);
        }

        [Test]
        public void OneRelicPerHeroAndMovingTakesItAlong()
        {
            var ctx = Player();
            var heroes = ctx.State.Formation.Where(h => h != null).Take(2).ToArray();
            var a = Relics.Create(ctx.State, "relic_dragon_scale");
            var b = Relics.Create(ctx.State, "relic_warden_lantern");
            new EquipRelic { HeroId = heroes[0], Uid = a.Uid }.Apply(ctx);
            new EquipRelic { HeroId = heroes[0], Uid = b.Uid }.Apply(ctx);
            Assert.That(a.EquippedBy, Is.Null);
            Assert.That(Relics.Equipped(ctx.State, heroes[0]), Is.SameAs(b));
            new EquipRelic { HeroId = heroes[1], Uid = b.Uid }.Apply(ctx);
            Assert.That(Relics.Equipped(ctx.State, heroes[0]), Is.Null);
            Assert.That(b.EquippedBy, Is.EqualTo(heroes[1]));
        }

        [Test]
        public void UpgradingRaisesTheEffectUpToTheRarityCap()
        {
            var ctx = Player();
            var relic = Relics.Create(ctx.State, "relic_warden_lantern");
            var before = Relics.MainValue(ctx.Content, relic);
            new UpgradeRelic { Uid = relic.Uid }.Apply(ctx);
            Assert.That(Relics.MainValue(ctx.Content, relic), Is.GreaterThan(before));
            while (relic.Level < Relics.MaxLevel(ctx.Content, relic))
                new UpgradeRelic { Uid = relic.Uid }.Apply(ctx);
            Assert.That(relic.Level, Is.EqualTo(15), "Epic");
            Assert.That(Assert.Throws<RuleViolation>(() => new UpgradeRelic { Uid = relic.Uid }.Apply(ctx)).Code, Is.EqualTo("relic_max_level"));
        }

        [Test]
        public void SpareCopiesDissolveIntoRelicMaterials()
        {
            var ctx = Player();
            var hero = FirstHero(ctx);
            var spare = Relics.Create(ctx.State, "relic_treant_root");
            var worn = Relics.Create(ctx.State, "relic_treant_root");
            new EquipRelic { HeroId = hero, Uid = worn.Uid }.Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new DissolveRelic { Uid = worn.Uid }.Apply(ctx)).Code, Is.EqualTo("relic_equipped"));
            var before = ctx.State.Get(Ids.RelicMaterials);
            var got = new DissolveRelic { Uid = spare.Uid }.Apply(ctx);
            Assert.That(got, Is.GreaterThan(0));
            Assert.That(ctx.State.Get(Ids.RelicMaterials), Is.EqualTo(before + got));
        }

        [Test]
        public void TreantRootHealsTheTeamAfterTheUltimate()
        {
            int Heals(bool relic)
            {
                var ctx = Player(heroLevel: 60);
                var hero = FirstHero(ctx);
                if (relic)
                    new EquipRelic { HeroId = hero, Uid = Relics.Create(ctx.State, "relic_treant_root").Uid }.Apply(ctx);
                var setup = new BattleSetup { Seed = 3, Allies = Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation) };
                var boss = ctx.Content.Hero("obsidian_vault_sentinel");
                setup.Enemies.Add(new UnitSetup { Hero = boss, Slot = 0, Stats = boss.BaseStats.Clone() });
                setup.Enemies[0].Stats.Hp *= 200;
                var result = BattleSimulator.Run(setup);
                var index = setup.Allies.FindIndex(u => u.Hero.Id == hero);
                return result.Events.Count(e => e.Type == BattleEventType.Heal && e.Source == index && e.Skill == SkillSlot.Passive);
            }
            Assert.That(Heals(true), Is.GreaterThan(Heals(false)));
        }

        [Test]
        public void DeepShellMakesShieldsStronger()
        {
            var content = ContentFixture.Content();
            var shielder = content.Heroes.Values.First(h => h.Skills().Any(s => s.Effects.Any(e => e.Kind == EffectKind.Shield)));
            long Shields(bool relic)
            {
                var ctx = Player(heroLevel: 60);
                ctx.State.Heroes.Add(new OwnedHero { HeroId = shielder.Id, Level = 60, Stars = 6 });
                ctx.State.Formation = new[] { shielder.Id, null, null, null, null };
                if (relic)
                    new EquipRelic { HeroId = shielder.Id, Uid = Relics.Create(ctx.State, "relic_deep_shell").Uid }.Apply(ctx);
                var setup = new BattleSetup { Seed = 4, Allies = Campaign.Allies(ctx.State, content, ctx.State.Formation) };
                var boss = content.Hero("obsidian_vault_sentinel");
                setup.Enemies.Add(new UnitSetup { Hero = boss, Slot = 0, Stats = boss.BaseStats.Clone() });
                setup.Enemies[0].Stats.Hp *= 200;
                return BattleSimulator.Run(setup).Events.Where(e => e.Type == BattleEventType.ShieldGained && e.Source == 0).Select(e => e.Value).DefaultIfEmpty(0).Max();
            }
            Assert.That(Shields(false), Is.GreaterThan(0), shielder.Id);
            Assert.That(Shields(true), Is.GreaterThan(Shields(false)));
        }

        [Test]
        public void DevourerCoreBoostsTheWearersFragments()
        {
            var ctx = Player(shrineLevel: 3);
            var hero = FirstHero(ctx);
            var core = Fragments.Create(ctx.State, FragmentSlot.Core, ctx.Content.Hero(hero).Aspect, Grade.Legendary, null);
            new EquipFragment { HeroId = hero, Uid = core.Uid }.Apply(ctx);
            var without = Unit(ctx, hero).Stats.Attack;
            new EquipRelic { HeroId = hero, Uid = Relics.Create(ctx.State, "relic_devourer_core").Uid }.Apply(ctx);
            Assert.That(Unit(ctx, hero).Stats.Attack, Is.GreaterThan(without));
        }

        [Test]
        public void VaultKeyRaisesMaterialDungeonRewardsOnlyThere()
        {
            long Gold(bool key, string dungeon)
            {
                var ctx = Player();
                for (var i = 0; i < 4; i++)
                    new UpgradeBuilding { BuildingId = "portalChamber" }.Apply(ctx);
                ctx.State.Dungeons.Highest[dungeon] = 10;
                if (key)
                    new EquipRelic { HeroId = FirstHero(ctx), Uid = Relics.Create(ctx.State, "relic_vault_key").Uid }.Apply(ctx);
                return new SweepDungeon { DungeonId = dungeon }.Apply(ctx).Where(a => a.Id == Ids.Gold).Sum(a => a.Value);
            }
            Assert.That(Gold(true, "material"), Is.GreaterThan(Gold(false, "material")));
            Assert.That(Gold(true, "fragment"), Is.EqualTo(Gold(false, "fragment")));
        }

        [Test]
        public void RelicDungeonOpensAtPortalLevelFourAndDropsRelics()
        {
            var ctx = Player(heroLevel: 200);
            for (var i = 0; i < 3; i++)
                new UpgradeBuilding { BuildingId = "portalChamber" }.Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginDungeon { DungeonId = "relic", Level = 1 }.Apply(ctx)).Code, Is.EqualTo("dungeon_locked"));
            new UpgradeBuilding { BuildingId = "portalChamber" }.Apply(ctx);
            var every = Dungeons.Get(ctx.Content, "relic").RelicEveryLevels;
            for (var level = 1; level <= every; level++)
            {
                ctx.State.Dungeons.Used.Clear();
                new BeginDungeon { DungeonId = "relic", Level = level }.Apply(ctx);
                var r = new FinishDungeon().Apply(ctx);
                Assert.That(r.Outcome, Is.EqualTo(BattleOutcome.Victory), $"level {level}");
                Assert.That(r.Rewards.Any(a => a.Id == Ids.RelicMaterials), Is.True);
                Assert.That(r.Relics.Count, Is.EqualTo(level == every ? 1 : 0), $"level {level}");
            }
        }

        [Test]
        public void IdleRewardsGiveRelicMaterialsFromStageTwentyFive()
        {
            var early = Player(shrineLevel: 0, stage: 24);
            early.State.Afk.LastClaimMs = early.NowMs - Afk.HourMs * 10;
            Assert.That(Afk.Pending(early.State, early.Content, early.NowMs).Any(a => a.Id == Ids.RelicMaterials), Is.False);
            var later = Player(shrineLevel: 0, stage: 25);
            later.State.Afk.LastClaimMs = later.NowMs - Afk.HourMs * 10;
            Assert.That(Afk.Pending(later.State, later.Content, later.NowMs).Any(a => a.Id == Ids.RelicMaterials), Is.True);
        }
    }
}
