using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>
    /// The intro a new player sees once: the design doc's lore told in five pictures (docs/systems/TUTORIAL.md).
    /// Tap moves on, "Überspringen" ends it; either way it is marked done and the campaign hub opens.
    /// </summary>
    public sealed class StoryScreen : UiScreen
    {
        public const int Pages = 5;

        private int _page;
        private VisualElement _picture;
        private Label _text;
        private VisualElement _dots;

        /// <summary>A new player who has not seen the intro yet (players from before the intro skip it).</summary>
        public static bool Due(PlayerState state) => !Tutorial.Done(state, Tutorial.Intro) && state.Campaign.HighestCleared == 0;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("story");
            _picture = UiKit.Box("story__picture").Into(root);
            var panel = UiKit.Box("story__panel").Into(root);
            _text = UiKit.Text("", "story__text").Into(panel);
            _dots = UiKit.Box("story__dots").Into(panel);
            UiKit.Text(Loc.T("story.tap"), "label--small", "story__hint").Into(panel);
            UiKit.Btn(Loc.T("story.skip"), Finish, "btn--secondary", "btn--small", "story__skip").Into(root);
            root.RegisterCallback<ClickEvent>(e =>
            {
                if (!(e.target is Button))
                    Next();
            });
        }

        /// <summary>Next page; after the last one the intro ends.</summary>
        public void Next()
        {
            if (++_page >= Pages)
                Finish();
            else
                Show();
        }

        public override void OnShow()
        {
            Ui.Battle.Clear();
            Ui.Battle.SetBackground(null);
            Show();
        }

        private void Show()
        {
            var art = Ui.Catalog.Story("story_" + (_page + 1));
            _picture.style.backgroundImage = art != null ? new StyleBackground(art) : StyleKeyword.None;
            _text.text = Loc.T("story." + (_page + 1));
            // replay the fade-in on every page
            _text.RemoveFromClassList("story__text--in");
            _text.schedule.Execute(() => _text.AddToClassList("story__text--in")).StartingIn(30);
            _dots.Clear();
            for (var i = 0; i < Pages; i++)
                UiKit.Box("story__dot").Into(_dots).EnableInClassList("story__dot--on", i == _page);
        }

        private void Finish()
        {
            Ui.Try(new CompleteTutorial { Id = Tutorial.Intro }, out _);
            Ui.Router.Reset(new HomeScreen());
        }
    }
}
