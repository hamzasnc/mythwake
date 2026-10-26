using System;
using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Gear widgets shared by hero detail, the Broken Forge and battle results.</summary>
    public static class GearUi
    {
        public static string SlotName(GearSlot slot) => Loc.T("gear.slot." + UiKit.Lower(slot));

        public static string StatText(GameContent content, GearItem item)
        {
            var v = Gear.MainValue(content, item);
            return item.Slot switch
            {
                GearSlot.Weapon => $"+{v} {Loc.T("hero.attack")}",
                GearSlot.Armor => $"+{v} {Loc.T("hero.hp")}",
                GearSlot.Boots => $"+{v} {Loc.T("hero.defense")}",
                _ => $"+{v / 10f:0.#}% {Loc.T("gear.crit")}"
            };
        }

        /// <summary>Icon of a gear piece: one picture per set and slot (Content/Icons/gear_&lt;set&gt;_&lt;slot&gt;.png).</summary>
        public static string IconName(GearItem item) => $"gear_{UiKit.Lower(item.Set)}_{UiKit.Lower(item.Slot)}";

        /// <summary>Compact gear tile: grade frame, icon, slot, enhance level, main stat, set.</summary>
        public static VisualElement Tile(UiContext ui, GearItem item)
        {
            var tile = UiKit.Box("gear", "hero-card--" + UiKit.GradeClass(item.Grade));
            var head = UiKit.Box("row").Into(tile);
            UiKit.Text(SlotName(item.Slot), "gear__slot", "grade--" + UiKit.GradeClass(item.Grade)).Into(head);
            UiKit.Box("spacer").Into(head);
            if (item.Level > 0)
                UiKit.Text("+" + item.Level, "gear__level").Into(head);
            var icon = ui.Catalog.Icon(IconName(item));
            if (icon != null)
                UiKit.Box("gear__icon").Into(tile).style.backgroundImage = new StyleBackground(icon);
            UiKit.Text(StatText(ui.Content, item), "gear__stat").Into(tile);
            UiKit.Text(Loc.T("faction." + UiKit.Lower(item.Set)), "gear__set").Into(tile);
            return tile;
        }

        public static VisualElement EmptyTile(GearSlot slot)
        {
            var tile = UiKit.Box("gear", "gear--empty");
            UiKit.Text(SlotName(slot), "gear__slot").Into(tile);
            UiKit.Text("+", "slot__plus").Into(tile);
            return tile;
        }

        public static VisualElement CostRow(PlayerState state, IEnumerable<Amount> cost)
        {
            var row = UiKit.Box("row");
            foreach (var c in cost)
            {
                var chip = UiKit.Box("row", "cost").Into(row);
                UiKit.Box("currency__icon", "icon--" + c.Id).Into(chip);
                UiKit.Text(UiKit.Short(c.Value), state.Get(c.Id) >= c.Value ? "cost__value" : "label--bad").Into(chip);
            }
            return row;
        }

        /// <summary>Popup for one hero slot: enhance or remove the worn piece, or pick one from the bag.</summary>
        public static void OpenSlot(UiContext ui, VisualElement host, string heroId, GearSlot slot, Action changed)
        {
            var overlay = UiKit.Box("overlay").Into(host);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            UiKit.Text(SlotName(slot), "title").Into(popup);

            void Close()
            {
                overlay.RemoveFromHierarchy();
                changed?.Invoke();
            }

            var worn = Gear.Equipped(ui.State, heroId, slot);
            if (worn != null)
            {
                popup.Add(Tile(ui, worn));
                var maxLevel = Gear.GradeStats(ui.Content, worn.Grade).MaxLevel;
                var row = UiKit.Box("row", "center").Into(popup);
                if (worn.Level < maxLevel)
                {
                    var cost = Gear.EnhanceCost(ui.Content, worn);
                    row.Add(CostRow(ui.State, cost));
                    var enhance = UiKit.Btn(Loc.T("gear.enhance"), () =>
                    {
                        if (ui.Try(new EnhanceGear { Uid = worn.Uid }, out int _))
                        {
                            overlay.RemoveFromHierarchy();
                            OpenSlot(ui, host, heroId, slot, changed);
                        }
                    }).Into(row);
                    enhance.SetEnabled(cost.All(c => ui.State.Get(c.Id) >= c.Value));
                }
                else
                {
                    UiKit.Text(Loc.T("gear.maxLevel"), "label--small").Into(row);
                }
                UiKit.Btn(Loc.T("gear.unequip"), () =>
                {
                    if (ui.Try(new UnequipGear { Uid = worn.Uid }, out bool _))
                        Close();
                }, "btn--secondary").Into(popup);
            }

            UiKit.Text(Loc.T("gear.bag"), "subtitle").Into(popup);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(popup);
            scroll.style.maxHeight = 900;
            var grid = UiKit.Box("hero-grid").Into(scroll);
            var options = ui.State.Gear.Where(g => g.Slot == slot && g.EquippedBy != heroId)
                .OrderByDescending(g => Gear.MainValue(ui.Content, g)).ToList();
            foreach (var item in options)
            {
                var tile = Tile(ui, item).Into(grid);
                if (item.EquippedBy != null)
                    UiKit.Text(ui.Content.Hero(item.EquippedBy).Name, "gear__owner").Into(tile);
                var uid = item.Uid;
                tile.RegisterCallback<ClickEvent>(_ =>
                {
                    if (ui.Try(new EquipGear { HeroId = heroId, Uid = uid }, out bool _))
                        Close();
                });
            }
            if (options.Count == 0)
                UiKit.Text(Loc.T("gear.none"), "label--small").Into(grid);
            UiKit.Btn(Loc.T("village.close"), Close, "btn--secondary").Into(popup);
        }
    }
}
