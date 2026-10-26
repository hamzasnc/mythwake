using System;
using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Relic widgets shared by hero detail, the Relic collection and battle results (docs/systems/RELICS.md).</summary>
    public static class RelicUi
    {
        public static string EffectText(GameContent content, RelicItem item)
        {
            var def = Relics.Get(content, item.RelicId);
            if (def == null)
                return "";
            var value = Relics.MainValue(content, item);
            var number = def.Raw ? value.ToString() : (value / 10f).ToString("0.#");
            return (def.Description ?? "").Replace("{0}", number);
        }

        public static VisualElement Icon(UiContext ui, string relicId, string size = "fragment__icon")
        {
            var icon = UiKit.Box(size);
            var texture = ui.Catalog.Icon(relicId);
            if (texture != null)
                icon.style.backgroundImage = new StyleBackground(texture);
            else
                icon.AddToClassList("relic__icon--placeholder");
            return icon;
        }

        public static VisualElement Tile(UiContext ui, RelicItem item)
        {
            var def = Relics.Get(ui.Content, item.RelicId);
            var tile = UiKit.Box("fragment", "hero-card--" + UiKit.GradeClass(def.Grade));
            var head = UiKit.Box("row").Into(tile);
            UiKit.Text(Loc.T("relic.slot"), "fragment__aspect", "grade--" + UiKit.GradeClass(def.Grade)).Into(head);
            UiKit.Box("spacer").Into(head);
            if (item.Level > 0)
                UiKit.Text("+" + item.Level, "gear__level").Into(head);
            tile.Add(Icon(ui, item.RelicId));
            UiKit.Text(def.Name, "fragment__name", "grade--" + UiKit.GradeClass(def.Grade)).Into(tile);
            return tile;
        }

        public static VisualElement Details(UiContext ui, RelicItem item)
        {
            var def = Relics.Get(ui.Content, item.RelicId);
            var box = UiKit.Box("fragment-details");
            var row = UiKit.Box("row").Into(box);
            row.Add(Icon(ui, item.RelicId, "fragment__icon--big"));
            var texts = UiKit.Box("grow").Into(row);
            texts.style.flexBasis = 0;
            UiKit.Text(def.Name, "building__name").Into(texts).style.whiteSpace = WhiteSpace.Normal;
            UiKit.Text($"{Loc.T("grade." + UiKit.Lower(def.Grade))} · " + Loc.T("relic.level", item.Level, Relics.MaxLevel(ui.Content, item)),
                "label--small", "grade--" + UiKit.GradeClass(def.Grade)).Into(texts);
            UiKit.Text(Loc.T("relic.source", ui.Content.Hero(def.Source).Name), "label--small").Into(texts);
            UiKit.Text(EffectText(ui.Content, item), "skill__text").Into(box);
            if (item.EquippedBy != null)
                UiKit.Text(Loc.T("fragment.wornBy", ui.Content.Hero(item.EquippedBy).Name), "gear__owner").Into(box);
            return box;
        }

        public static VisualElement UpgradeRow(UiContext ui, RelicItem item, Action upgraded)
        {
            var row = UiKit.Box("row", "center");
            if (item.Level >= Relics.MaxLevel(ui.Content, item))
            {
                UiKit.Text(Loc.T("gear.maxLevel"), "label--small").Into(row);
                return row;
            }
            var cost = Relics.UpgradeCost(ui.Content, item);
            row.Add(GearUi.CostRow(ui.State, cost));
            var button = UiKit.Btn(Loc.T("relic.upgrade"), () =>
            {
                if (ui.Try(new UpgradeRelic { Uid = item.Uid }, out int _))
                    upgraded();
            }).Into(row);
            button.SetEnabled(Relics.SlotOpen(ui.State, ui.Content) && cost.All(c => ui.State.Get(c.Id) >= c.Value));
            return row;
        }

        /// <summary>Hero detail tile: the worn Relic, an empty slot or the Shrine level that opens it.</summary>
        public static VisualElement Slot(UiContext ui, VisualElement host, string heroId, Action changed)
        {
            if (!Relics.SlotOpen(ui.State, ui.Content))
            {
                var locked = FragmentUi.EmptyTile(FragmentSlot.Core, Loc.T("relic.slotLocked", ui.Content.Relics.ShrineLevel));
                locked.Q<Label>().text = Loc.T("relic.slot");
                locked.AddToClassList("slot--locked");
                return locked;
            }
            var worn = Relics.Equipped(ui.State, heroId);
            VisualElement tile;
            if (worn != null)
                tile = Tile(ui, worn);
            else
            {
                tile = FragmentUi.EmptyTile(FragmentSlot.Core, "+");
                tile.Q<Label>().text = Loc.T("relic.slot");
            }
            tile.RegisterCallback<ClickEvent>(_ => OpenSlot(ui, host, heroId, changed));
            return tile;
        }

        public static void OpenSlot(UiContext ui, VisualElement host, string heroId, Action changed)
        {
            var overlay = UiKit.Box("overlay").Into(host);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            UiKit.Text(Loc.T("relic.slot"), "title").Into(popup);

            void Close()
            {
                overlay.RemoveFromHierarchy();
                changed?.Invoke();
            }

            var worn = Relics.Equipped(ui.State, heroId);
            if (worn != null)
            {
                popup.Add(Details(ui, worn));
                popup.Add(UpgradeRow(ui, worn, () =>
                {
                    overlay.RemoveFromHierarchy();
                    OpenSlot(ui, host, heroId, changed);
                }));
                UiKit.Btn(Loc.T("relic.unequip"), () =>
                {
                    if (ui.Try(new UnequipRelic { Uid = worn.Uid }, out bool _))
                        Close();
                }, "btn--secondary").Into(popup);
            }

            UiKit.Text(Loc.T("relic.title"), "subtitle").Into(popup);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(popup);
            scroll.style.maxHeight = 900;
            var grid = UiKit.Box("hero-grid").Into(scroll);
            var options = ui.State.Relics.Where(r => r.EquippedBy != heroId)
                .OrderByDescending(r => Relics.Get(ui.Content, r.RelicId).Grade).ThenByDescending(r => r.Level).ToList();
            foreach (var item in options)
            {
                var tile = Tile(ui, item).Into(grid);
                if (item.EquippedBy != null)
                    UiKit.Text(ui.Content.Hero(item.EquippedBy).Name, "gear__owner").Into(tile);
                var uid = item.Uid;
                tile.RegisterCallback<ClickEvent>(_ =>
                {
                    if (ui.Try(new EquipRelic { HeroId = heroId, Uid = uid }, out bool _))
                        Close();
                });
            }
            if (options.Count == 0)
                UiKit.Text(Loc.T("relic.none"), "label--small").Into(grid).style.whiteSpace = WhiteSpace.Normal;
            UiKit.Btn(Loc.T("village.close"), Close, "btn--secondary").Into(popup);
        }
    }

    /// <summary>
    /// The Relic collection: every Relic of the doc's bosses, owned copies with their levels, missing ones greyed with
    /// the boss that drops them.
    /// </summary>
    public sealed class RelicScreen : UiScreen
    {
        private CurrencyBar _currencies;
        private VisualElement _list;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            _currencies = new CurrencyBar(Ids.Gold, Ids.RelicMaterials).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.Btn("‹", () => Ui.Router.Back(() => new VillageScreen()), "btn--secondary", "btn--small").Into(head);
            UiKit.Text(Loc.T("relic.title"), "title").Into(head);
            UiKit.Text(Loc.T("relic.hint"), "label--small").Into(safe);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _list = UiKit.Box().Into(scroll);
        }

        public override void Refresh()
        {
            _currencies.Refresh(Ui.State);
            _list.Clear();
            foreach (var def in Ui.Content.Relics.Relics.OrderBy(r => r.Grade))
            {
                var copies = Ui.State.Relics.Where(r => r.RelicId == def.Id).OrderByDescending(r => r.Level).ToList();
                var card = UiKit.Box("panel", "relic-card").Into(_list);
                card.EnableInClassList("relic-card--missing", copies.Count == 0);
                var row = UiKit.Box("row").Into(card);
                row.Add(RelicUi.Icon(Ui, def.Id, "fragment__icon--big"));
                var texts = UiKit.Box("grow").Into(row);
                texts.style.flexBasis = 0;
                UiKit.Text(def.Name, "building__name", "grade--" + UiKit.GradeClass(def.Grade)).Into(texts).style.whiteSpace = WhiteSpace.Normal;
                UiKit.Text(Loc.T("grade." + UiKit.Lower(def.Grade)) + " · " + Loc.T("relic.source", Ui.Content.Hero(def.Source).Name), "label--small").Into(texts);
                var shown = copies.Count > 0 ? copies[0] : new RelicItem { RelicId = def.Id };
                UiKit.Text(RelicUi.EffectText(Ui.Content, shown), "skill__text").Into(texts);
                UiKit.Text(copies.Count > 0 ? Loc.T("relic.copies", copies.Count) : Loc.T("relic.missing"), "label--small").Into(texts);
                foreach (var copy in copies)
                {
                    var item = copy;
                    var line = UiKit.Box("row", "relic-copy").Into(card);
                    UiKit.Text(Loc.T("relic.level", item.Level, Relics.MaxLevel(Ui.Content, item)), "gear__level").Into(line);
                    if (item.EquippedBy != null)
                        UiKit.Text("  " + Ui.Content.Hero(item.EquippedBy).Name, "gear__owner").Into(line);
                    UiKit.Box("spacer").Into(line);
                    UiKit.Btn("›", () => Open(item), "btn--secondary", "btn--small").Into(line);
                }
            }
        }

        private void Open(RelicItem item)
        {
            var overlay = UiKit.Box("overlay").Into(Root);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            popup.Add(RelicUi.Details(Ui, item));
            popup.Add(RelicUi.UpgradeRow(Ui, item, () =>
            {
                overlay.RemoveFromHierarchy();
                Refresh();
                Open(item);
            }));
            var buttons = UiKit.Box("row", "center").Into(popup);
            if (item.EquippedBy == null)
                UiKit.Btn(Loc.T("relic.dissolve", UiKit.Short(Relics.DissolveValue(Ui.Content, item))), () =>
                {
                    if (Ui.Try(new DissolveRelic { Uid = item.Uid }, out long _))
                    {
                        overlay.RemoveFromHierarchy();
                        Refresh();
                    }
                }, "btn--secondary").Into(buttons);
            UiKit.Btn(Loc.T("village.close"), () => overlay.RemoveFromHierarchy(), "btn--secondary").Into(buttons);
        }
    }
}
