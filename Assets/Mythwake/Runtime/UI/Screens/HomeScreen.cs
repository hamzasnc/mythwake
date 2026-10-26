using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Core.Progression;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    public enum Tab { Campaign, Village, Heroes, Summon }

    /// <summary>Bottom navigation shared by the main screens.</summary>
    public static class MainNav
    {
        public static VisualElement Build(UiContext ui, Tab active)
        {
            var nav = UiKit.Box("nav");
            Item(nav, ui, Tab.Campaign, "nav.campaign", active, () => new HomeScreen());
            Item(nav, ui, Tab.Village, "nav.village", active, () => new VillageScreen());
            Item(nav, ui, Tab.Heroes, "nav.heroes", active, () => new HeroesScreen());
            Item(nav, ui, Tab.Summon, "nav.summon", active, () => new SummonScreen());
            return nav;
        }

        private static void Item(VisualElement nav, UiContext ui, Tab tab, string key, Tab active, System.Func<UiScreen> make)
        {
            var b = new Button(() =>
            {
                if (tab != active)
                    ui.Router.Reset(make());
            }) { text = Loc.T(key) };
            b.AddToClassList("nav__item");
            if (tab == active)
                b.AddToClassList("nav__item--active");
            nav.Add(b);
        }
    }

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
            // no solid background: the team idles on the battlefield behind the home screen
            _currencies = new CurrencyBar(Ids.Gold, Ids.HeroExp, Ids.Gems, Ids.SummonScroll).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var quest = UiKit.Box("panel", "quest-strip").Into(safe);
            quest.RegisterCallback<ClickEvent>(_ => Ui.Router.Push(new QuestScreen()));
            var questText = UiKit.Box("grow").Into(quest);
            UiKit.Text(Loc.T("quests.current"), "label--small").Into(questText);
            _mainQuest = UiKit.Text("", "quest__title").Into(questText);
            // the whole strip is the button; the pill only labels it
            var questButton = UiKit.Text(Loc.T("quests.title") + "  ›", "pill").Into(quest);
            questButton.style.width = 210;
            questButton.style.height = 56;
            questButton.style.flexShrink = 0;
            questButton.style.flexShrink = 0;
            questButton.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            questText.style.flexBasis = 0;
            questText.style.flexShrink = 1;
            _questBadge = UiKit.Box("badge").Into(questButton);
            var arena = UiKit.Box("panel", "quest-strip").Into(safe);
            arena.RegisterCallback<ClickEvent>(_ => Ui.Router.Reset(new AdventureScreen()));
            var arenaText = UiKit.Box("grow").Into(arena);
            arenaText.style.flexBasis = 0;
            UiKit.Text(Loc.T("adventure.title"), "quest__title").Into(arenaText);
            UiKit.Text(Loc.T("adventure.teaser"), "label--small").Into(arenaText);
            var arenaPill = UiKit.Text(Loc.T("arena.open") + "  ›", "pill").Into(arena);
            arenaPill.style.width = 210;
            arenaPill.style.height = 56;
            arenaPill.style.flexShrink = 0;
            arenaPill.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            UiKit.Box("spacer").Into(safe);

            var card = UiKit.Box("panel", "stage-card").Into(safe);
            _chapter = UiKit.Text("", "subtitle").Into(card);
            _stage = UiKit.Text("", "stage-card__number").Into(card);
            _boss = UiKit.Text(Loc.T("home.boss"), "boss-tag").Into(card);
            var powers = UiKit.Box("row").Into(card);
            _teamPower = UiKit.Text("", "label--small").Into(powers);
            UiKit.Text("   ·   ", "label--small").Into(powers);
            _enemyPower = UiKit.Text("", "label--small").Into(powers);
            _fight = UiKit.Btn(Loc.T("home.fight"), () => Ui.Router.Push(new FormationScreen()), "btn--big").Into(card);
            _fight.style.marginTop = 20;

            var afk = UiKit.Box("panel").Into(safe);
            var head = UiKit.Box("row").Into(afk);
            UiKit.Text(Loc.T("afk.title"), "title").Into(head);
            UiKit.Box("spacer").Into(head);
            _afkTime = UiKit.Text("", "subtitle").Into(head);
            var progress = UiKit.Box("afk-progress").Into(afk);
            _afkFill = UiKit.Box("afk-progress__fill").Into(progress);
            _afkRewards = UiKit.Box("afk-rewards").Into(afk);
            _claim = UiKit.Btn(Loc.T("afk.claim"), Claim).Into(afk);

            root.Add(MainNav.Build(Ui, Tab.Campaign));
        }

        public override void OnShow()
        {
            base.OnShow();
            _ticker = Root.schedule.Execute(RefreshAfk).Every(1000);
            var state = Ui.State;
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
                _stage.text = Loc.T("home.stage", number, index);
                _boss.style.display = Campaign.IsBoss(content, next) ? DisplayStyle.Flex : DisplayStyle.None;
                var enemy = Campaign.Enemies(content, next).Sum(u => TeamBuilder.Power(u.Stats));
                var team = TeamBuilder.Build(Formations.ToProgress(state, content, state.Formation)).Sum(u => TeamBuilder.Power(u.Stats));
                _enemyPower.text = Loc.T("home.enemyPower", UiKit.Short(enemy));
                _teamPower.text = Loc.T("home.teamPower", UiKit.Short(team));
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
                _afkRewards.Add(UiKit.Amount(a.Id, a.Value));
            if (pending.Count == 0)
                _afkRewards.Add(UiKit.Text(Loc.T("afk.empty"), "label--small"));
            _claim.SetEnabled(pending.Count > 0);
        }

        private void Claim()
        {
            if (Ui.Try(new ClaimAfkRewards(), out _))
                Refresh();
        }
    }
}
