using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Core.Progression;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Campaign hub: next stage, idle reward chest, currencies.</summary>
    public sealed class HomeScreen : UiScreen
    {
        private CurrencyBar _currencies;
        private Label _chapter;
        private Label _stage;
        private Label _boss;
        private Label _enemyPower;
        private Label _teamPower;
        private Button _fight;
        private VisualElement _afkRewards;
        private Label _afkTime;
        private VisualElement _afkFill;
        private Button _claim;
        private Label _mainQuest;
        private VisualElement _questBadge;
        private IVisualElementScheduledItem _ticker;

        protected override void Build(VisualElement root)
        {
            // AFK Arena / 7DS style (docs/UI_STYLE.md): the team idles full-screen, UI only on the edges
            _currencies = new CurrencyBar(Ids.Gold, Ids.HeroExp, Ids.Gems, Ids.SummonScroll).Into(root);
            var hub = UiKit.Box("hub").Into(root);

            var head = UiKit.Box("hub__head").Into(hub);
            _chapter = UiKit.Text("", "hub__chapter").Into(head);
            _stage = UiKit.Text("", "hub__stage").Into(head);
            _boss = UiKit.Text(Loc.T("home.boss"), "boss-tag").Into(head);

            var left = UiKit.Box("hub__side", "hub__side--left").Into(hub);
            var quest = UiKit.SideButton(left, "reliefQuests", Loc.T("quests.title"), () => Ui.Router.Push(new QuestScreen()));
            _questBadge = UiKit.Box("badge").Into(quest);
            var right = UiKit.Box("hub__side", "hub__side--right").Into(hub);
            UiKit.SideButton(right, "reliefMap", Loc.T("nav.adventure"), () => Ui.Router.Reset(new AdventureScreen()));

            // current main quest as a slim strip under the stage name
            var strip = UiKit.Box("hub__quest").Into(hub);
            strip.RegisterCallback<ClickEvent>(_ => Ui.Router.Push(new QuestScreen()));
            UiKit.Icon("questMain", "hub__quest-icon").Into(strip);
            _mainQuest = UiKit.Text("", "hub__quest-text").Into(strip);

            UiKit.Box("spacer").Into(hub);

            // idle loot pile in front of the team: tap to collect
            var afk = UiKit.Box("hub__afk").Into(hub);
            var pile = UiKit.Box("hub__pile").Into(afk);
            _afkRewards = UiKit.Box("hub__loot").Into(afk);
            var bar = UiKit.Box("hub__afk-bar").Into(afk);
            var progress = UiKit.Box("afk-progress").Into(bar);
            _afkFill = UiKit.Box("afk-progress__fill").Into(progress);
            _afkTime = UiKit.Text("", "hub__afk-time").Into(bar);
            _claim = UiKit.Btn(Loc.T("afk.claim"), Claim, "btn--secondary", "btn--small", "hub__claim").Into(afk);
            _claim.name = "coach-afk";
            pile.RegisterCallback<ClickEvent>(_ => Claim());

            var powers = UiKit.Box("hub__powers").Into(hub);
            UiKit.Icon("power", "hub__power-icon").Into(powers);
            _teamPower = UiKit.Text("", "hub__power").Into(powers);
            UiKit.Text("vs", "hub__vs").Into(powers);
            _enemyPower = UiKit.Text("", "hub__power", "hub__power--enemy").Into(powers);
            _fight = UiKit.Btn(Loc.T("home.fight"), () => Ui.Router.Push(new FormationScreen()), "btn--big", "hub__fight").Into(hub);
            _fight.name = "coach-fight";

            root.Add(MainNav.Build(Ui, Tab.Campaign));
        }

        public override void OnShow()
        {
            base.OnShow();
            _ticker = Root.schedule.Execute(RefreshAfk).Every(1000);
            var state = Ui.State;
            var next = System.Math.Min(state.Campaign.HighestCleared + 1, Ui.Content.TotalStages);
            Ui.Battle.SetBackground(Ui.Catalog.Background(Campaign.Locate(Ui.Content, next).chapter?.Id));
            Ui.Battle.ShowIdle(Ui.Catalog, TeamBuilder.Build(Formations.ToProgress(state, Ui.Content, state.Formation)));
        }

        public override void OnHide()
        {
            _ticker?.Pause();
            Ui.Battle.Clear();
        }

        public override void Refresh()
        {
            var state = Ui.State;
            var content = Ui.Content;
            _currencies.Refresh(state);

            var next = state.Campaign.HighestCleared + 1;
            if (next > content.TotalStages)
            {
                _chapter.text = "";
                _stage.text = Loc.T("home.campaignDone");
                _boss.style.display = DisplayStyle.None;
                _fight.SetEnabled(false);
            }
            else
            {
                var (chapter, index, number) = Campaign.Locate(content, next);
                _chapter.text = string.IsNullOrEmpty(chapter?.Name) ? Loc.T("home.chapter", number) : $"{Loc.T("home.chapter", number)} · {chapter.Name}";
                _enemyPower.EnableInClassList("hub__power--danger", Campaign.Enemies(content, next).Sum(u => TeamBuilder.Power(u.Stats)) > TeamBuilder.Build(Formations.ToProgress(state, content, state.Formation)).Sum(u => TeamBuilder.Power(u.Stats)));
                _stage.text = Loc.T("home.stage", number, index);
                _boss.style.display = Campaign.IsBoss(content, next) ? DisplayStyle.Flex : DisplayStyle.None;
                var enemy = Campaign.Enemies(content, next).Sum(u => TeamBuilder.Power(u.Stats));
                var team = TeamBuilder.Build(Formations.ToProgress(state, content, state.Formation)).Sum(u => TeamBuilder.Power(u.Stats));
                _enemyPower.text = UiKit.Short(enemy);
                _teamPower.text = UiKit.Short(team);
                _fight.SetEnabled(true);
            }
            RefreshAfk();
            var main = Quests.CurrentMain(state, content);
            _mainQuest.text = main == null ? Loc.T("quests.mainDone") : main.Text;
            _questBadge.style.display = Quests.AnyClaimable(state, content, Ui.Authority.NowMs) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshAfk()
        {
            var state = Ui.State;
            var content = Ui.Content;
            var now = Ui.Authority.NowMs;
            var elapsed = Afk.ElapsedMs(state, content, now);
            var hours = elapsed / (float)Afk.HourMs;
            _afkTime.text = Loc.T("afk.time", hours.ToString("0.0"), content.Afk.MaxHours);
            _afkFill.style.width = Length.Percent(100f * elapsed / Afk.MaxMs(content));
            _afkRewards.Clear();
            var pending = Afk.Pending(state, content, now);
            foreach (var a in pending)
                _afkRewards.Add(UiKit.Slot(a.Id, a.Value));
            if (pending.Count == 0)
                _afkRewards.Add(UiKit.Text(Loc.T("afk.empty"), "hub__afk-empty"));
            _claim.SetEnabled(pending.Count > 0);
        }

        private void Claim()
        {
            if (Ui.Try(new ClaimAfkRewards(), out _))
                Refresh();
        }
    }
}
