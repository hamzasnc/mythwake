using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Shops (docs/systems/SHOPS.md): one tab per shop, offers with price and daily / weekly limits.</summary>
    public sealed class ShopScreen : UiScreen
    {
        private string _shopId = "general";
        private CurrencyBar _currencies;
        private VisualElement _tabs;
        private Label _description;
        private VisualElement _grid;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            _currencies = new CurrencyBar(Ids.Gold, Ids.Gems, Ids.FragmentDust, Ids.ArenaCoins).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.Btn("‹", () => Ui.Router.Back(() => new VillageScreen()), "btn--secondary", "btn--small").Into(head);
            UiKit.Text(Loc.T("shop.title"), "title").Into(head);
            _tabs = UiKit.Box("tabs").Into(safe);
            _description = UiKit.Text("", "label--small").Into(safe);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _grid = UiKit.Box("shop-grid").Into(scroll);
        }

        public override void Refresh()
        {
            var state = Ui.State;
            var content = Ui.Content;
            _currencies.Refresh(state);
            _tabs.Clear();
            foreach (var s in content.Shops.Shops)
            {
                var id = s.Id;
                var tab = UiKit.Btn(s.Name, () =>
                {
                    _shopId = id;
                    Refresh();
                }, "tab").Into(_tabs);
                tab.RemoveFromClassList("btn");
                tab.EnableInClassList("tab--active", id == _shopId);
            }

            var shop = Shops.Get(content, _shopId);
            _description.text = shop.Description;
            _grid.Clear();
            if (!Shops.Open(state, content, shop))
            {
                var building = content.Village.Buildings.FirstOrDefault(b => b.Id == shop.Building);
                var text = building != null ? Loc.T("shop.lockedBuilding", building.Name) : Loc.T("shop.lockedStage", shop.UnlockStage);
                UiKit.Text(text, "subtitle").Into(UiKit.Box("panel").Into(_grid));
                return;
            }
            var now = Ui.Authority.NowMs;
            foreach (var item in shop.Items)
                _grid.Add(Offer(shop, item, now));
        }

        private VisualElement Offer(ShopDefinition shop, ShopItem item, long now)
        {
            var state = Ui.State;
            var content = Ui.Content;
            var card = UiKit.Box("panel", "shop-item");
            var bought = Shops.Bought(state, content, shop, item, now);
            var left = item.Limit > 0 ? item.Limit - bought : int.MaxValue;

            var icon = UiKit.Box("shop-item__icon").Into(card);
            var hero = Shops.ShardHero(content, item, now);
            if (hero != null)
            {
                var portrait = Ui.Catalog.Hero(hero)?.portrait;
                if (portrait != null)
                    icon.style.backgroundImage = new StyleBackground(portrait);
            }
            else if (item.FragmentSlot != null)
            {
                var texture = Ui.Catalog.Icon(UiKit.Lower(item.FragmentSlot.Value) + "_fragment");
                if (texture != null)
                    icon.style.backgroundImage = new StyleBackground(texture);
            }
            else if (item.Gives.Count > 0)
                UiKit.Box("currency__icon", "icon--" + item.Gives[0].Id, "shop-item__currency").Into(icon);

            UiKit.Text(hero != null ? $"{item.Name}: {content.Hero(hero).Name}" : item.Name, "shop-item__name").Into(card);
            foreach (var g in item.Gives)
                UiKit.Text($"+{UiKit.Short(g.Value)} {UiKit.CurrencyName(g.Id)}", "label--small").Into(card);
            if (item.ShardAmount > 0)
                UiKit.Text($"+{item.ShardAmount} Hero Shards", "label--small").Into(card);
            if (item.Limit > 0)
                UiKit.Text(left > 0 ? Loc.T(item.Weekly ? "shop.leftWeek" : "shop.left", left) : Loc.T("shop.soldOut"),
                    left > 0 ? "label--small" : "label--bad").Into(card);
            card.Add(GearUi.CostRow(state, item.Price));
            var buy = UiKit.Btn(Loc.T("shop.buy"), () => Buy(shop, item), "btn--small").Into(card);
            buy.SetEnabled(left > 0 && item.Price.All(p => state.Get(p.Id) >= p.Value));
            return card;
        }

        private void Buy(ShopDefinition shop, ShopItem item)
        {
            if (!Ui.Try(new BuyShopItem { ShopId = shop.Id, ItemId = item.Id }, out var purchase))
                return;
            Refresh();
            if (purchase.Fragment != null)
            {
                var overlay = UiKit.Box("overlay").Into(Root);
                var popup = UiKit.Box("panel", "popup").Into(overlay);
                popup.Add(FragmentUi.Details(Ui, purchase.Fragment));
                UiKit.Btn(Loc.T("battle.continue"), () => overlay.RemoveFromHierarchy()).Into(popup);
                return;
            }
            if (purchase.Gear != null)
            {
                var overlay = UiKit.Box("overlay").Into(Root);
                var popup = UiKit.Box("panel", "popup").Into(overlay);
                popup.Add(GearUi.Tile(Ui, purchase.Gear));
                UiKit.Btn(Loc.T("battle.continue"), () => overlay.RemoveFromHierarchy()).Into(popup);
                return;
            }
            Ui.Toast(Loc.T("shop.bought", item.Name));
        }

        internal void SelectForAutoPilot(string shopId)
        {
            _shopId = shopId;
            Refresh();
        }
    }
}
