using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Intro and coach steps: only known steps, each remembered once, saved with the player.</summary>
    public sealed class TutorialTests
    {
        private static CommandContext NewPlayer()
        {
            var ctx = new CommandContext { Content = ContentFixture.Content(), NowMs = 1_800_000_000_000 };
            new CreatePlayer { Seed = 3 }.Apply(ctx);
            return ctx;
        }

        [Test]
        public void NewPlayerHasNoStepDone()
        {
            var ctx = NewPlayer();
            foreach (var id in Tutorial.Steps)
                Assert.That(Tutorial.Done(ctx.State, id), Is.False, id);
        }

        [Test]
        public void CompletingAStepIsRememberedOnce()
        {
            var ctx = NewPlayer();
            Assert.That(new CompleteTutorial { Id = Tutorial.Intro }.Apply(ctx), Is.True);
            Assert.That(new CompleteTutorial { Id = Tutorial.Intro }.Apply(ctx), Is.False, "second time changes nothing");
            Assert.That(ctx.State.TutorialDone, Is.EqualTo(new[] { Tutorial.Intro }));
        }

        [Test]
        public void UnknownStepsAreRejected()
        {
            var ctx = NewPlayer();
            var error = Assert.Throws<RuleViolation>(() => new CompleteTutorial { Id = "free_gems" }.Apply(ctx));
            Assert.That(error.Code, Is.EqualTo("unknown_tutorial"));
            Assert.That(ctx.State.TutorialDone, Is.Empty);
        }
    }
}
