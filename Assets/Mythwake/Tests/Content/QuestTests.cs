using System.Linq;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Daily, weekly and main quests (docs/systems/QUESTS.md).</summary>
    public sealed class QuestTests
    {
        // Monday 2027-01-04 10:00 UTC
        private const long Monday = 1_799_056_800_000;

        private static CommandContext NewPlayer(long now = Monday)
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = now };
            new CreatePlayer { Seed = 31 }.Apply(ctx);
            return ctx;
        }

        private static TaskDefinition Daily(CommandContext ctx, string id) => ctx.Content.Quests.Daily.First(t => t.Id == id);

        [Test]
        public void ResetIsAtTheConfiguredHourAndWeeksStartOnMonday()
        {
            var content = ContentFixture.Content();
            var hour = content.Quests.ResetHourUtc * Afk.HourMs;
            var dayStart = Quests.NextDailyResetMs(content, Monday) - Quests.DayMs;
            Assert.That((dayStart - hour) % Quests.DayMs, Is.EqualTo(0));
            Assert.That(Quests.DayIndex(content, dayStart), Is.EqualTo(Quests.DayIndex(content, dayStart - 1) + 1));
            // Monday 10:00 and Sunday 10:00 are in the same week, the next Monday is not
            Assert.That(Quests.WeekIndex(content, Monday + 6 * Quests.DayMs), Is.EqualTo(Quests.WeekIndex(content, Monday)));
            Assert.That(Quests.WeekIndex(content, Monday + 7 * Quests.DayMs), Is.EqualTo(Quests.WeekIndex(content, Monday) + 1));
        }

        [Test]
        public void ActionsAdvanceDailyQuestsAndClaimingGivesPoints()
        {
            var ctx = NewPlayer();
            var task = Daily(ctx, "levels");
            new LevelUpHero { HeroId = "dorn", Levels = 2 }.Apply(ctx);
            Assert.That(Quests.Progress(ctx.State, ctx.Content, QuestKind.Daily, task, ctx.NowMs), Is.EqualTo(2));
            Assert.That(Assert.Throws<RuleViolation>(() => new ClaimQuest { Kind = QuestKind.Daily, QuestId = "levels" }.Apply(ctx)).Code, Is.EqualTo("quest_not_done"));

            new LevelUpHero { HeroId = "dorn" }.Apply(ctx);
            Assert.That(new ClaimQuest { Kind = QuestKind.Daily, QuestId = "levels" }.Apply(ctx), Is.EqualTo(task.Points));
            Assert.That(Assert.Throws<RuleViolation>(() => new ClaimQuest { Kind = QuestKind.Daily, QuestId = "levels" }.Apply(ctx)).Code, Is.EqualTo("quest_claimed"));
        }

        [Test]
        public void ProgressBeforeTodayDoesNotCount()
        {
            var ctx = NewPlayer();
            new LevelUpHero { HeroId = "dorn", Levels = 3 }.Apply(ctx);
            ctx.NowMs += Quests.DayMs;
            Assert.That(Quests.Progress(ctx.State, ctx.Content, QuestKind.Daily, Daily(ctx, "levels"), ctx.NowMs), Is.EqualTo(0));
            new LevelUpHero { HeroId = "dorn" }.Apply(ctx);
            Assert.That(Quests.Progress(ctx.State, ctx.Content, QuestKind.Daily, Daily(ctx, "levels"), ctx.NowMs), Is.EqualTo(1));
        }

        [Test]
        public void ChestsNeedPointsAndResetWithTheDay()
        {
            var ctx = NewPlayer();
            var first = ctx.Content.Quests.DailyChests[0];
            Assert.That(Assert.Throws<RuleViolation>(() => new ClaimQuestChest { Kind = QuestKind.Daily, Points = first.Points }.Apply(ctx)).Code, Is.EqualTo("chest_locked"));
            new LevelUpHero { HeroId = "dorn", Levels = 3 }.Apply(ctx);
            new ClaimQuest { Kind = QuestKind.Daily, QuestId = "levels" }.Apply(ctx);
            var gold = ctx.State.Get(Ids.Gold);
            new ClaimQuestChest { Kind = QuestKind.Daily, Points = first.Points }.Apply(ctx);
            Assert.That(ctx.State.Get(Ids.Gold), Is.EqualTo(gold + first.Rewards.First(r => r.Id == Ids.Gold).Value));
            Assert.That(Assert.Throws<RuleViolation>(() => new ClaimQuestChest { Kind = QuestKind.Daily, Points = first.Points }.Apply(ctx)).Code, Is.EqualTo("chest_claimed"));

            ctx.NowMs += Quests.DayMs;
            Assert.That(Quests.Points(ctx.State, ctx.Content, QuestKind.Daily, ctx.NowMs), Is.EqualTo(0));
            Assert.That(Quests.ChestClaimed(ctx.State, ctx.Content, QuestKind.Daily, first.Points, ctx.NowMs), Is.False);
        }

        [Test]
        public void AFullDailyBoardCountsTowardsTheWeek()
        {
            var ctx = NewPlayer();
            ctx.State.Add(Ids.Gold, 1_000_000);
            ctx.State.Add(Ids.HeroExp, 1_000_000);
            ctx.State.Add(Ids.GearMaterials, 1_000);
            ctx.State.Village.Buildings["summoningGate"] = new BuildingState { Level = 1 };
            var item = Gear.Create(ctx.State, GearSlot.Weapon, Core.Content.Grade.Rare, Core.Content.Faction.OrderOfTheVeil);

            ctx.NowMs += Afk.HourMs;
            new ClaimAfkRewards().Apply(ctx);
            ctx.NowMs += Afk.HourMs;
            new ClaimAfkRewards().Apply(ctx);
            for (var i = 0; i < 3; i++)
            {
                new BeginStageBattle().Apply(ctx);
                new FinishStageBattle().Apply(ctx);
            }
            new LevelUpHero { HeroId = "dorn", Levels = 3 }.Apply(ctx);
            new CollectVillage().Apply(ctx);
            new EnhanceGear { Uid = item.Uid }.Apply(ctx);
            new SummonHeroes { BannerId = "normal", Count = 1 }.Apply(ctx);

            foreach (var t in ctx.Content.Quests.Daily)
                new ClaimQuest { Kind = QuestKind.Daily, QuestId = t.Id }.Apply(ctx);
            Assert.That(Quests.StatValue(ctx.State, Stat.DailyCompleted), Is.EqualTo(1));
            var weekly = ctx.Content.Quests.Weekly.First(t => t.Stat == Stat.DailyCompleted);
            Assert.That(Quests.Progress(ctx.State, ctx.Content, QuestKind.Weekly, weekly, ctx.NowMs), Is.EqualTo(1));
        }

        [Test]
        public void MainQuestsRunInOrderAndCheckTheState()
        {
            var ctx = NewPlayer();
            var first = Quests.CurrentMain(ctx.State, ctx.Content);
            Assert.That(first.Id, Is.EqualTo(ctx.Content.Quests.Main[0].Id));
            Assert.That(Assert.Throws<RuleViolation>(() => new ClaimMainQuest().Apply(ctx)).Code, Is.EqualTo("quest_not_done"));

            new BeginStageBattle().Apply(ctx);
            new FinishStageBattle().Apply(ctx);
            var gold = ctx.State.Get(Ids.Gold);
            new ClaimMainQuest().Apply(ctx);
            Assert.That(ctx.State.Get(Ids.Gold), Is.EqualTo(gold + first.Rewards[0].Value));
            Assert.That(Quests.CurrentMain(ctx.State, ctx.Content).Id, Is.EqualTo(ctx.Content.Quests.Main[1].Id));
        }

        [Test]
        public void GoalsReachedEarlierCompleteAtOnce()
        {
            var ctx = NewPlayer();
            ctx.State.Campaign.HighestCleared = 3;
            new ClaimMainQuest().Apply(ctx);
            new ClaimMainQuest().Apply(ctx);
            Assert.That(ctx.State.Quests.Main, Is.EqualTo(2));
        }

        [Test]
        public void TheMainChainWalksTheSystemsInUnlockOrder()
        {
            // every building quest must be reachable: its building unlocks no later than the stage quests before it
            var content = ContentFixture.Content();
            var stage = 0L;
            foreach (var q in content.Quests.Main)
            {
                if (q.Condition == MainCondition.StageCleared)
                    stage = System.Math.Max(stage, q.Value);
                if (q.Condition == MainCondition.BuildingLevel)
                    Assert.That(Village.Building(content, q.Target).UnlockStage, Is.LessThanOrEqualTo(stage + 5), q.Id);
            }
        }

        [Test]
        public void ClaimableBadge()
        {
            var ctx = NewPlayer();
            Assert.That(Quests.AnyClaimable(ctx.State, ctx.Content, ctx.NowMs), Is.False);
            new BeginStageBattle().Apply(ctx);
            new FinishStageBattle().Apply(ctx);
            Assert.That(Quests.AnyClaimable(ctx.State, ctx.Content, ctx.NowMs), Is.True);
        }
    }
}
