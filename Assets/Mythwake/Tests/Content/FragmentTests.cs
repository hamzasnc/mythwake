using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Fragment system and Fragment Shrine (docs/systems/FRAGMENTS.md).</summary>
    public sealed class FragmentTests
    {
        // Monday 2027-01-04 10:00 UTC
        private const long Monday = 1_799_056_800_000;

        private static CommandContext Player(int shrineLevel = 5, int stage = 30)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = Monday };
            new CreatePlayer { Seed = 77 }.Apply(ctx);
            ctx.State.Campaign.HighestCleared = stage;
            foreach (var id in new[] { Ids.Gold, Ids.Wood, Ids.Stone, Ids.FragmentDust })
                ctx.State.Add(id, 50_000_000);
            for (var i = 0; i < shrineLevel; i++)
                new UpgradeBuilding { BuildingId = "fragmentShrine" }.Apply(ctx);
            return ctx;
        }

        private static string FirstHero(CommandContext ctx) => ctx.State.Formation.First(h => h != null);

        private static UnitSetup Unit(CommandContext ctx, string heroId) =>
            Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation).First(u => u.Hero.Id == heroId);

        [Test]
        public void ShrineLevelOpensTheSlots()
        {
            var ctx = Player(shrineLevel: 1);
            var hero = FirstHero(ctx);
            var core = Fragments.Create(ctx.State, FragmentSlot.Core, Aspect.Ravak, Grade.Common, null);
            var aspect = Fragments.Create(ctx.State, FragmentSlot.Aspect, Aspect.Ravak, Grade.Common, "ravak_last_stand");
            Assert.DoesNotThrow(() => new EquipFragment { HeroId = hero, Uid = core.Uid }.Apply(ctx));
            Assert.That(Assert.Throws<RuleViolation>(() => new EquipFragment { HeroId = hero, Uid = aspect.Uid }.Apply(ctx)).Code,
                Is.EqualTo("fragment_slot_locked"));
            new UpgradeBuilding { BuildingId = "fragmentShrine" }.Apply(ctx);
            new UpgradeBuilding { BuildingId = "fragmentShrine" }.Apply(ctx);
            Assert.DoesNotThrow(() => new EquipFragment { HeroId = hero, Uid = aspect.Uid }.Apply(ctx));
        }

        [Test]
        public void CoreFragmentRaisesStatsMoreWithTheHerosOwnAspect()
        {
            var ctx = Player();
            var hero = FirstHero(ctx);
            var own = ctx.Content.Hero(hero).Aspect;
            var other = (Aspect)(((int)own + 1) % 5);
            var before = Unit(ctx, hero).Stats.Attack;

            var foreign = Fragments.Create(ctx.State, FragmentSlot.Core, other, Grade.Epic, null);
            new EquipFragment { HeroId = hero, Uid = foreign.Uid }.Apply(ctx);
            var withForeign = Unit(ctx, hero).Stats.Attack;
            var matching = Fragments.Create(ctx.State, FragmentSlot.Core, own, Grade.Epic, null);
            new EquipFragment { HeroId = hero, Uid = matching.Uid }.Apply(ctx);
            var withMatching = Unit(ctx, hero).Stats.Attack;

            Assert.That(withForeign, Is.GreaterThan(before));
            Assert.That(withMatching, Is.GreaterThan(withForeign));
            Assert.That(Fragments.Equipped(ctx.State, hero, FragmentSlot.Core), Is.SameAs(matching), "one Fragment per slot");
            Assert.That(foreign.EquippedBy, Is.Null);
        }

        [Test]
        public void EffectsScaleWithGradeAndLevelButDrawbacksDoNot()
        {
            var content = ContentFixture.Content();
            var common = new FragmentItem { Slot = FragmentSlot.Echo, Grade = Grade.Common, Effect = "echo_bulwark" };
            var legendary = new FragmentItem { Slot = FragmentSlot.Echo, Grade = Grade.Legendary, Level = 25, Effect = "echo_bulwark" };
            var low = Fragments.Passives(content, common);
            var high = Fragments.Passives(content, legendary);
            Assert.That(high[0].Value, Is.EqualTo(low[0].Value * 8), "Legendary ×3.2, level 25 ×2.5");
            Assert.That(high[1].Value, Is.EqualTo(low[1].Value), "drawback stays");
            Assert.That(high[1].Value, Is.LessThan(0));
        }

        [Test]
        public void EveryEffectStaysSaneAtLegendaryMaxLevel()
        {
            var content = ContentFixture.Content();
            foreach (var e in content.Fragments.Effects)
            {
                var item = new FragmentItem { Slot = e.Slot, Grade = Grade.Legendary, Level = 25, Effect = e.Id };
                foreach (var p in Fragments.Passives(content, item))
                {
                    Assert.That(p.Chance, Is.InRange(1, 1000), e.Id);
                    Assert.That(p.Value, Is.LessThan(1000), e.Id);
                }
                Assert.That(Fragments.MainValue(content, item), Is.GreaterThan(0), e.Id);
            }
        }

        [Test]
        public void AspectFragmentGoesIntoTheBattle()
        {
            var ctx = Player();
            var hero = FirstHero(ctx);
            var f = Fragments.Create(ctx.State, FragmentSlot.Aspect, Aspect.Ravak, Grade.Rare, "ravak_last_stand");
            new EquipFragment { HeroId = hero, Uid = f.Uid }.Apply(ctx);
            var unit = Unit(ctx, hero);
            Assert.That(unit.Passives.Count(p => p.Kind == PassiveKind.DamageBonus && p.BelowHp == 500), Is.EqualTo(1));
            new UnequipFragment { Uid = f.Uid }.Apply(ctx);
            Assert.That(Unit(ctx, hero).Passives, Is.Empty);
        }

        [Test]
        public void EchoBonusAgainstAGroupRaisesDamageInBattle()
        {
            var content = ContentFixture.Content();
            var hollowed = content.Monsters.Values.First(m => m.Group == EnemyGroup.Hollowed && !m.Boss);
            BattleResult Run(bool echo)
            {
                var ctx = Player();
                var hero = FirstHero(ctx);
                if (echo)
                {
                    var f = Fragments.Create(ctx.State, FragmentSlot.Echo, Aspect.Ravak, Grade.Legendary, "echo_vs_hollowed");
                    f.Level = 25;
                    new EquipFragment { HeroId = hero, Uid = f.Uid }.Apply(ctx);
                }
                var setup = new BattleSetup { Seed = 5, Allies = Campaign.Allies(ctx.State, content, ctx.State.Formation) };
                setup.Enemies.Add(new UnitSetup { Hero = hollowed, Slot = 0, Stats = hollowed.BaseStats.Clone() });
                setup.Enemies[0].Stats.Hp *= 50;
                return BattleSimulator.Run(setup);
            }
            var without = Run(false).Units.Where(u => u.Side == Side.Allies).Sum(u => u.DamageDealt);
            var with = Run(true).Units.Where(u => u.Side == Side.Allies).Sum(u => u.DamageDealt);
            Assert.That(with, Is.GreaterThan(without));
        }

        [Test]
        public void UpgradeCostsDustAndStopsAtMaxLevel()
        {
            var ctx = Player();
            var f = Fragments.Create(ctx.State, FragmentSlot.Core, Aspect.Gorath, Grade.Common, null);
            var dust = ctx.State.Get(Ids.FragmentDust);
            new UpgradeFragment { Uid = f.Uid }.Apply(ctx);
            Assert.That(ctx.State.Get(Ids.FragmentDust), Is.LessThan(dust));
            var max = Fragments.GradeStats(ctx.Content, Grade.Common).MaxLevel;
            while (f.Level < max)
                new UpgradeFragment { Uid = f.Uid }.Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new UpgradeFragment { Uid = f.Uid }.Apply(ctx)).Code, Is.EqualTo("fragment_max_level"));
        }

        [Test]
        public void FusingThreeEqualFragmentsMakesTheNextGradeAndKeepsTheFirstEffect()
        {
            var ctx = Player();
            var a = Fragments.Create(ctx.State, FragmentSlot.Aspect, Aspect.Veloria, Grade.Rare, "veloria_charm");
            var b = Fragments.Create(ctx.State, FragmentSlot.Aspect, Aspect.Ravak, Grade.Rare, "ravak_burn");
            var c = Fragments.Create(ctx.State, FragmentSlot.Aspect, Aspect.Gorath, Grade.Rare, "gorath_regen");
            b.Level = 3;
            var dust = ctx.State.Get(Ids.FragmentDust);
            var fused = new FuseFragments { Uids = new List<int> { a.Uid, b.Uid, c.Uid } }.Apply(ctx);
            Assert.That(fused.Grade, Is.EqualTo(Grade.Epic));
            Assert.That(fused.Aspect, Is.EqualTo(Aspect.Veloria));
            Assert.That(fused.Effect, Is.EqualTo("veloria_charm"));
            Assert.That(ctx.State.Fragments, Has.Count.EqualTo(1));
            Assert.That(ctx.State.Get(Ids.FragmentDust), Is.GreaterThan(dust), "half of the level dust comes back");
        }

        [Test]
        public void FusingNeedsMatchingUnequippedFragments()
        {
            var ctx = Player();
            var hero = FirstHero(ctx);
            var a = Fragments.Create(ctx.State, FragmentSlot.Core, Aspect.Ravak, Grade.Common, null);
            var b = Fragments.Create(ctx.State, FragmentSlot.Core, Aspect.Ravak, Grade.Common, null);
            var rare = Fragments.Create(ctx.State, FragmentSlot.Core, Aspect.Ravak, Grade.Rare, null);
            var echo = Fragments.Create(ctx.State, FragmentSlot.Echo, Aspect.Ravak, Grade.Common, "echo_energy");
            Assert.That(Assert.Throws<RuleViolation>(() => new FuseFragments { Uids = new List<int> { a.Uid, b.Uid, rare.Uid } }.Apply(ctx)).Code, Is.EqualTo("fuse_mismatch"));
            Assert.That(Assert.Throws<RuleViolation>(() => new FuseFragments { Uids = new List<int> { a.Uid, b.Uid, echo.Uid } }.Apply(ctx)).Code, Is.EqualTo("fuse_mismatch"));
            Assert.That(Assert.Throws<RuleViolation>(() => new FuseFragments { Uids = new List<int> { a.Uid, b.Uid, b.Uid } }.Apply(ctx)).Code, Is.EqualTo("fuse_duplicate"));
            Assert.That(Assert.Throws<RuleViolation>(() => new FuseFragments { Uids = new List<int> { a.Uid, b.Uid } }.Apply(ctx)).Code, Is.EqualTo("fuse_count"));
            var c = Fragments.Create(ctx.State, FragmentSlot.Core, Aspect.Ravak, Grade.Common, null);
            new EquipFragment { HeroId = hero, Uid = c.Uid }.Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new FuseFragments { Uids = new List<int> { a.Uid, b.Uid, c.Uid } }.Apply(ctx)).Code, Is.EqualTo("fragment_equipped"));
            Assert.That(ctx.State.Fragments, Has.Count.EqualTo(5), "nothing consumed by failed fuses");
        }

        [Test]
        public void DissolveTurnsAFragmentIntoDust()
        {
            var ctx = Player();
            var f = Fragments.Create(ctx.State, FragmentSlot.Echo, Aspect.Envyra, Grade.Epic, "echo_ultimate");
            var dust = ctx.State.Get(Ids.FragmentDust);
            var got = new DissolveFragment { Uid = f.Uid }.Apply(ctx);
            Assert.That(got, Is.EqualTo(Fragments.GradeStats(ctx.Content, Grade.Epic).Dissolve));
            Assert.That(ctx.State.Get(Ids.FragmentDust), Is.EqualTo(dust + got));
            Assert.That(ctx.State.Fragments, Is.Empty);
        }

        [Test]
        public void StabilizeNeedsTheShrineAndAnOpenSlot()
        {
            var none = Player(shrineLevel: 0);
            Assert.That(Assert.Throws<RuleViolation>(() => new StabilizeFragment { Slot = FragmentSlot.Core }.Apply(none)).Code, Is.EqualTo("shrine_locked"));
            var one = Player(shrineLevel: 1);
            Assert.That(Assert.Throws<RuleViolation>(() => new StabilizeFragment { Slot = FragmentSlot.Echo }.Apply(one)).Code, Is.EqualTo("fragment_slot_locked"));
            var f = new StabilizeFragment { Slot = FragmentSlot.Core }.Apply(one);
            Assert.That(f.Slot, Is.EqualTo(FragmentSlot.Core));
            Assert.That(f.Grade, Is.LessThanOrEqualTo(Grade.Rare), "Shrine level 1 stabilizes Common or Rare");
            Assert.That(f.Effect, Is.Null);

            var five = Player(shrineLevel: 5);
            for (var i = 0; i < 30; i++)
            {
                var e = new StabilizeFragment { Slot = FragmentSlot.Aspect }.Apply(five);
                var def = Fragments.Effect(five.Content, e.Effect);
                Assert.That(def.Slot, Is.EqualTo(FragmentSlot.Aspect));
                Assert.That(def.Aspect, Is.EqualTo(e.Aspect), "Aspect effects match the Fragment's Aspect");
            }
        }

        [Test]
        public void BossStagesDropFragmentsOnceTheShrineStands()
        {
            StageResult Boss(int shrine)
            {
                var ctx = Player(shrineLevel: shrine, stage: 19);
                foreach (var h in ctx.State.Heroes)
                {
                    h.Level = 120;
                    h.Stars = 6;
                }
                new BeginStageBattle().Apply(ctx);
                return new FinishStageBattle().Apply(ctx);
            }
            var without = Boss(0);
            Assert.That(without.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(without.Fragments, Is.Empty);
            var with = Boss(1);
            Assert.That(with.Fragments, Has.Count.EqualTo(1));
            Assert.That(with.Rewards.Any(r => r.Id == Ids.FragmentDust), Is.True);
        }

        [Test]
        public void FragmentDungeonOpensAtPortalLevelThreeAndDropsFragments()
        {
            var ctx = Player();
            foreach (var h in ctx.State.Heroes)
            {
                h.Level = 150;
                h.Stars = 6;
            }
            for (var i = 0; i < 2; i++)
                new UpgradeBuilding { BuildingId = "portalChamber" }.Apply(ctx);
            Assert.That(Assert.Throws<RuleViolation>(() => new BeginDungeon { DungeonId = "fragment", Level = 1 }.Apply(ctx)).Code, Is.EqualTo("dungeon_locked"));
            new UpgradeBuilding { BuildingId = "portalChamber" }.Apply(ctx);
            var every = Dungeons.Get(ctx.Content, "fragment").FragmentEveryLevels;
            for (var level = 1; level <= every; level++)
            {
                ctx.State.Dungeons.Used.Clear();
                new BeginDungeon { DungeonId = "fragment", Level = level }.Apply(ctx);
                var r = new FinishDungeon().Apply(ctx);
                Assert.That(r.Outcome, Is.EqualTo(BattleOutcome.Victory), $"level {level}");
                Assert.That(r.Rewards.Any(a => a.Id == Ids.FragmentDust), Is.True);
                Assert.That(r.Fragments.Count, Is.EqualTo(level == every ? 1 : 0), $"level {level}");
            }
        }

        [Test]
        public void IdleRewardsGiveDustOnlyFromStageFifteen()
        {
            var early = Player(shrineLevel: 0, stage: 14);
            early.State.Afk.LastClaimMs = early.NowMs - Afk.HourMs * 10;
            Assert.That(Afk.Pending(early.State, early.Content, early.NowMs).Any(a => a.Id == Ids.FragmentDust), Is.False);
            var later = Player(shrineLevel: 0, stage: 40);
            later.State.Afk.LastClaimMs = later.NowMs - Afk.HourMs * 10;
            Assert.That(Afk.Pending(later.State, later.Content, later.NowMs).Any(a => a.Id == Ids.FragmentDust), Is.True);
        }

        [Test]
        public void ArenaSnapshotsKeepFragmentEffects()
        {
            var ctx = Player();
            var hero = FirstHero(ctx);
            var f = Fragments.Create(ctx.State, FragmentSlot.Echo, Aspect.Ravak, Grade.Rare, "echo_ultimate");
            new EquipFragment { HeroId = hero, Uid = f.Uid }.Apply(ctx);
            var snapshot = TeamSnapshot.Of(Campaign.Allies(ctx.State, ctx.Content, ctx.State.Formation));
            // later changes to the player's Fragments do not change the stored defense
            new UnequipFragment { Uid = f.Uid }.Apply(ctx);
            var units = snapshot.ToSetups(ctx.Content);
            Assert.That(units.First(u => u.Hero.Id == hero).Passives.Any(p => p.Kind == PassiveKind.UltimateDamage), Is.True);
        }
    }
}
