using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Normal Summon banner with 1x / 10x pulls (Summon Scrolls or Gems) and pity counter.</summary>
    public sealed class SummonScreen : UiScreen
    {
        private const string BannerId = "normal";
        private CurrencyBar _currencies;
        private Label _pity;
        private Button _scroll1;
        private Button _scroll10;
        private Button _gems1;
        private Button _gems10;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            var banner = Ui.Content.Banner(BannerId);
            _currencies = new CurrencyBar(Ids.Gems, Ids.SummonScroll).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            UiKit.Text(Loc.T("summon.title"), "title").Into(safe);

            var card = UiKit.Box("banner").Into(safe);
            var art = UiKit.Box("banner__art").Into(card);
            // feature the banner's strongest heroes as banner art
            var featured = Ui.Content.Heroes.Values.Where(h => h.Grade >= Grade.Legendary).OrderBy(h => h.Id).FirstOrDefault();
            var assets = featured == null ? null : Ui.Catalog.Hero(featured.Id);
            var image = assets?.cutIn != null ? assets.cutIn : assets?.full;
            if (image != null)
                art.style.backgroundImage = new StyleBackground(image);
            var info = UiKit.Box("banner__info").Into(card);
            UiKit.Text(banner.Name, "title").Into(info);
            var r = banner.GradeRates;
            UiKit.Text(Loc.T("summon.rates", Pct(r[0]), Pct(r[1]), Pct(r[2]), Pct(r[3])), "label--small").Into(info);
            _pity = UiKit.Text("", "subtitle").Into(info);

            var scrolls = UiKit.Box("row", "center").Into(safe);
            _scroll1 = UiKit.Btn("", () => Pull(1, false), "btn--secondary").Into(scrolls);
            _scroll10 = UiKit.Btn("", () => Pull(10, false), "btn--big").Into(scrolls);
            var gems = UiKit.Box("row", "center").Into(safe);
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
            state.Summon.PullsSinceLegendary.TryGetValue(BannerId, out var since);
            _pity.text = Loc.T("summon.pity", banner.LegendaryPity - since);
            Label(_scroll1, banner.Cost, 1);
            Label(_scroll10, banner.Cost, 10);
            Label(_gems1, banner.AltCost, 1);
            Label(_gems10, banner.AltCost, 10);
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

        private void Pull(int count, bool gems)
        {
            if (!Ui.Try(new SummonHeroes { BannerId = BannerId, Count = count, UseAltCost = gems }, out var results))
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
