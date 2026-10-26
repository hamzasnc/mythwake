using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Summon banners (Normal, Faction, Fragment) with 1x / 10x pulls (Summon Scrolls or Gems), focus picker and pity.</summary>
    public sealed class SummonScreen : UiScreen
    {
        private string BannerId = "normal";
        private string _focus;
        private CurrencyBar _currencies;
        private VisualElement _tabs;
        private VisualElement _focusRow;
        private Label _focusLabel;
        private Label _bannerName;
        private Label _rates;
        private Label _gateLocked;
        private Label _pity;
        private Button _scroll1;
        private Button _scroll10;
        private Button _gems1;
        private Button _gems10;
        private VisualElement _locked;
        private VisualElement _pulls;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            var banner = Ui.Content.Banner(BannerId);
            _currencies = new CurrencyBar(Ids.Gems, Ids.SummonScroll).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            UiKit.Text(Loc.T("summon.title"), "title").Into(safe);
            _tabs = UiKit.Box("tabs").Into(safe);

            var card = UiKit.Box("banner").Into(safe);
            var art = UiKit.Box("banner__art").Into(card);
            // feature the banner's strongest heroes as banner art
            var featured = Ui.Content.Heroes.Values.Where(h => h.Grade >= Grade.Legendary).OrderBy(h => h.Id).FirstOrDefault();
            var assets = featured == null ? null : Ui.Catalog.Hero(featured.Id);
            var image = assets?.cutIn != null ? assets.cutIn : assets?.full;
            if (image != null)
                art.style.backgroundImage = new StyleBackground(image);
            var info = UiKit.Box("banner__info").Into(card);
            _bannerName = UiKit.Text(banner.Name, "title").Into(info);
            _rates = UiKit.Text("", "label--small").Into(info);
            _pity = UiKit.Text("", "subtitle").Into(info);
            _focusLabel = UiKit.Text("", "label--small").Into(safe);
            _focusRow = UiKit.Box("row", "focus-row").Into(safe);
            _gateLocked = UiKit.Text("", "subtitle").Into(safe);

            _locked = UiKit.Box("panel").Into(safe);
            UiKit.Text(Loc.T("summon.locked"), "subtitle").Into(_locked).style.whiteSpace = WhiteSpace.Normal;
            UiKit.Btn("Dawnhollow", () => Ui.Router.Reset(new VillageScreen())).Into(_locked);
            _pulls = UiKit.Box().Into(safe);
            var scrolls = UiKit.Box("row", "center").Into(_pulls);
            _scroll1 = UiKit.Btn("", () => Pull(1, false), "btn--secondary").Into(scrolls);
            _scroll10 = UiKit.Btn("", () => Pull(10, false), "btn--big").Into(scrolls);
            var gems = UiKit.Box("row", "center").Into(_pulls);
            _gems1 = UiKit.Btn("", () => Pull(1, true), "btn--secondary").Into(gems);
            _gems10 = UiKit.Btn("", () => Pull(10, true), "btn--secondary").Into(gems);
            UiKit.Box("spacer").Into(safe);
            root.Add(MainNav.Build(Ui, Tab.Summon));
        }

        private static string Pct(int permille) => (permille / 10f).ToString("0.#") + "%";

        public override void Refresh()
        {
            var state = Ui.State;
            var banner = Ui.Content.Banner(BannerId);
            _currencies.Refresh(state);
            var unlocked = Village.SummonUnlocked(state, Ui.Content);
            var gateOk = Summoning.GateLevel(state, Ui.Content) >= banner.GateLevel;
            _locked.style.display = unlocked ? DisplayStyle.None : DisplayStyle.Flex;
            _pulls.style.display = unlocked && gateOk ? DisplayStyle.Flex : DisplayStyle.None;
            _gateLocked.style.display = unlocked && !gateOk ? DisplayStyle.Flex : DisplayStyle.None;
            _gateLocked.text = Loc.T("summon.locked.gate", banner.GateLevel);
            _bannerName.text = banner.Name;
            var r = banner.GradeRates;
            _rates.text = Loc.T("summon.rates", Pct(r[0]), Pct(r[1]), Pct(r[2]), Pct(r[3]));
            RefreshTabs();
            RefreshFocus(banner, unlocked && gateOk);
            state.Summon.PullsSinceLegendary.TryGetValue(BannerId, out var since);
            _pity.text = Loc.T("summon.pity", banner.LegendaryPity - since);
            Label(_scroll1, banner.Cost, 1);
            Label(_scroll10, banner.Cost, 10);
            Label(_gems1, banner.AltCost, 1);
            Label(_gems10, banner.AltCost, 10);
        }

        private void RefreshTabs()
        {
            _tabs.Clear();
            foreach (var b in Ui.Content.Banners)
            {
                var id = b.Id;
                var tab = UiKit.Btn(b.Name, () =>
                {
                    BannerId = id;
                    _focus = null;
                    Refresh();
                }, "tab").Into(_tabs);
                tab.RemoveFromClassList("btn");
                tab.EnableInClassList("tab--active", id == BannerId);
            }
        }

        private void RefreshFocus(BannerDefinition banner, bool open)
        {
            _focusRow.Clear();
            var show = open && banner.Focus != BannerFocus.None;
            _focusRow.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _focusLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show)
                return;
            var options = banner.Focus == BannerFocus.Faction
                ? System.Enum.GetValues(typeof(Faction)).Cast<object>().Select(v => (id: UiKit.Lower(v), name: Loc.T("faction." + UiKit.Lower(v))))
                : System.Enum.GetValues(typeof(Aspect)).Cast<object>().Select(v => (id: UiKit.Lower(v), name: v.ToString()));
            var list = options.ToList();
            if (_focus == null || list.All(o => o.id != _focus))
                _focus = list[0].id;
            _focusLabel.text = Loc.T("summon.focus", list.First(o => o.id == _focus).name);
            foreach (var (id, name) in list)
            {
                var chip = UiKit.Btn(name, () =>
                {
                    _focus = id;
                    Refresh();
                }, "btn--secondary", "btn--small", "focus-chip").Into(_focusRow);
                chip.EnableInClassList("focus-chip--active", id == _focus);
            }
        }

        private void Label(Button b, Amount unit, int count)
        {
            var total = unit.Value * count;
            if (unit == Ui.Content.Banner(BannerId).AltCost && count == 10)
                total = total * 9 / 10;
            b.Clear();
            b.text = "";
            var row = UiKit.Box("row").Into(b);
            UiKit.Text(Loc.T(count == 1 ? "summon.pull1" : "summon.pull10") + "   ").Into(row);
            UiKit.Box("currency__icon", "icon--" + unit.Id).Into(row);
            UiKit.Text(UiKit.Short(total)).Into(row);
            b.SetEnabled(Ui.State.Get(unit.Id) >= total);
        }

        internal void PullForAutoPilot(int count) => Pull(count, false);

        internal void SelectForAutoPilot(string bannerId)
        {
            BannerId = bannerId;
            _focus = null;
            Refresh();
        }

        private void Pull(int count, bool gems)
        {
            if (!Ui.Try(new SummonHeroes { BannerId = BannerId, Count = count, UseAltCost = gems, Focus = _focus }, out var results))
                return;
            Refresh();
            ShowResults(results);
        }

        private void ShowResults(List<SummonResult> results)
        {
            var overlay = UiKit.Box("overlay").Into(Root);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            var grid = UiKit.Box("hero-grid").Into(popup);
            var delay = 0L;
            foreach (var r in results.OrderByDescending(r => Ui.Content.Hero(r.HeroId).Grade))
            {
                var hero = Ui.Content.Hero(r.HeroId);
                var card = UiKit.HeroCard(Ui.Catalog, hero, Ui.State.FindHero(r.HeroId)).Into(grid);
                if (r.IsNew)
                    UiKit.Text(Loc.T("summon.new"), "new-badge").Into(card);
                else
                    UiKit.Text(Loc.T("summon.shards", r.Shards), "new-badge").Into(card);
                // cards flip in one after another
                card.style.opacity = 0f;
                card.style.transitionDuration = new List<TimeValue> { new TimeValue(0.2f) };
                card.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
                card.schedule.Execute(() => card.style.opacity = 1f).StartingIn(delay);
                delay += 110;
            }
            UiKit.Btn(Loc.T("battle.continue"), () => overlay.RemoveFromHierarchy()).Into(popup);
        }
    }
}
