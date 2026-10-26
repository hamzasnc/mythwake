namespace Mythwake.Core.Meta
{
    /// <summary>
    /// First-time guidance: the intro story and the coach steps (docs/systems/TUTORIAL.md). The client decides when a
    /// step shows; the state only remembers which ones the player finished, so a new device does not replay them.
    /// </summary>
    public static class Tutorial
    {
        public const string Intro = "intro";

        /// <summary>Every step id the client may complete, in the order a new player meets them.</summary>
        public static readonly string[] Steps =
        {
            Intro, "first_fight", "formation_fight", "ultimate", "afk_claim", "rebuild_gate", "gate_plot", "summon", "level_up",
        };

        public static bool Done(PlayerState state, string id) => state.TutorialDone.Contains(id);
    }

    public sealed class CompleteTutorial : ICommand<bool>
    {
        public string Id;

        public bool Apply(CommandContext ctx)
        {
            ctx.Require(System.Array.IndexOf(Tutorial.Steps, Id) >= 0, "unknown_tutorial", Id);
            if (ctx.State.TutorialDone.Contains(Id))
                return false;
            ctx.State.TutorialDone.Add(Id);
            return true;
        }
    }
}
