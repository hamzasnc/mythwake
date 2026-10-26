using System.Linq;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    public enum Tab { Town, Adventure, Campaign, Heroes, Summon }

    /// <summary>Bottom navigation like AFK Arena / 7DS Idle: town and world map left, campaign in the centre (docs/DECISIONS.md).</summary>
    public static class MainNav
    {
        public static VisualElement Build(UiContext ui, Tab active)
        {
            var nav = UiKit.Box("nav");
            Item(nav, ui, Tab.Town, "nav.village", active, () => new VillageScreen(), TownBadge(ui));
            Item(nav, ui, Tab.Adventure, "nav.adventure", active, () => new AdventureScreen(), false);
            Item(nav, ui, Tab.Campaign, "nav.campaign", active, () => new HomeScreen(), false).AddToClassList("nav__item--main");
            Item(nav, ui, Tab.Heroes, "nav.heroes", active, () => new HeroesScreen(), false);
            Item(nav, ui, Tab.Summon, "nav.summon", active, () => new SummonScreen(), false);
            return nav;
        }

        private static Button Item(VisualElement nav, UiContext ui, Tab tab, string key, Tab active, System.Func<UiScreen> make, bool badge)
        {
            var b = new Button(() =>
            {
                if (tab != active)
                    ui.Router.Reset(make());
            }) { text = Loc.T(key) };
            b.AddToClassList("nav__item");
            if (tab == active)
                b.AddToClassList("nav__item--active");
            if (badge)
                UiKit.Box("badge").Into(b);
            nav.Add(b);
            return b;
        }

        /// <summary>Red dot on Dawnhollow when a building can be rebuilt or upgraded right now.</summary>
        private static bool TownBadge(UiContext ui) => ui.Content.Village.Buildings.Any(def => BuildingPanel.CanUpgrade(ui.State, def));
    }
}
