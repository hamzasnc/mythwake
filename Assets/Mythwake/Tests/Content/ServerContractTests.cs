using System.Linq;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>What client and server rely on: the command wire format and identical state hashes (docs/SERVER.md).</summary>
    public sealed class ServerContractTests
    {
        private const long Now = 1_799_056_800_000;

        private static CommandContext NewPlayer()
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = Now };
            new CreatePlayer { Seed = 77 }.Apply(ctx);
            return ctx;
        }

        [Test]
        public void EveryCommandIsRegistered()
        {
            foreach (var name in new[] { "BeginStageBattle", "FinishStageBattle", "ClaimAfkRewards", "LevelUpHero", "SummonHeroes",
                         "UpgradeBuilding", "CraftGear", "ClaimQuest", "BeginHunt", "FinishHunt", "BuyHuntItem", "GrantRewards" })
                Assert.That(CommandCodec.Names, Does.Contain(name));
        }

        [Test]
        public void CommandsSurviveTheWire()
        {
            var finish = new FinishStageBattle { AutoUltimates = false };
            finish.AutoToggleTimesMs.Add(4_000);
            finish.UltimateRequests.Add(new Core.Combat.UltimateRequest(12_350, 2));
            var decoded = (FinishStageBattle)CommandCodec.Decode(CommandCodec.NameOf(finish), CommandCodec.Encode(finish));
            Assert.That(decoded.AutoUltimates, Is.False);
            Assert.That(decoded.AutoToggleTimesMs, Is.EqualTo(new[] { 4_000 }));
            Assert.That(decoded.UltimateRequests.Single().TimeMs, Is.EqualTo(12_350));
            Assert.That(decoded.UltimateRequests.Single().Slot, Is.EqualTo(2));
        }

        [Test]
        public void ServerOnlyCommandsAreMarked()
        {
            Assert.That(typeof(GrantRewards).GetCustomAttributes(typeof(ServerOnlyAttribute), false), Is.Not.Empty);
            Assert.That(typeof(LevelUpHero).GetCustomAttributes(typeof(ServerOnlyAttribute), false), Is.Empty);
        }

        [Test]
        public void ClientPredictionAndServerReachTheSameHash()
        {
            // both sides start from the same stored JSON and run the same commands with the same clock
            var stored = SaveJson.Write(NewPlayer().State);
            var commands = new object[]
            {
                new BeginStageBattle(), new FinishStageBattle(), new LevelUpHero { HeroId = "dorn", Levels = 2 },
                new ClaimAfkRewards(), new CollectVillage()
            };

            string Run()
            {
                var ctx = new CommandContext { State = SaveJson.Read(stored), Content = ContentFixture.Content(), NowMs = Now + 3_600_000 };
                foreach (var c in commands)
                    CommandCodec.Run(CommandCodec.Decode(CommandCodec.NameOf(c), CommandCodec.Encode(c)), ctx);
                return SaveJson.Hash(ctx.State);
            }

            Assert.That(Run(), Is.EqualTo(Run()));
        }

        [Test]
        public void SaveTextIsStableAcrossReadAndWrite()
        {
            var ctx = NewPlayer();
            var json = SaveJson.Write(ctx.State);
            Assert.That(SaveJson.Write(SaveJson.Read(json)), Is.EqualTo(json));
        }

        [Test]
        public void RuleViolationsKeepTheirCodeThroughTheRunner()
        {
            var ctx = NewPlayer();
            var error = Assert.Throws<RuleViolation>(() => CommandCodec.Run(new LevelUpHero { HeroId = "nobody" }, ctx));
            Assert.That(error.Code, Is.EqualTo("hero_not_owned"));
        }
    }
}
