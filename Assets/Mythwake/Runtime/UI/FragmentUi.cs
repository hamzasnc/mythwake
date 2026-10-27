using System;
using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Fragment widgets shared by hero detail, the Fragment Shrine and battle results (docs/systems/FRAGMENTS.md).</summary>
    public static class FragmentUi
    {
        public static string SlotName(FragmentSlot slot) => Loc.T("fragment.slot." + UiKit.Lower(slot));

        public static string Name(FragmentItem item) => $"{item.Aspect} {SlotName(item.Slot)}";

        public static Color AspectColor(Aspect aspect) => aspect switch
        {
            Aspect.Ravak => new Color(1f, 0.42f, 0.24f),
            Aspect.Gorath => new Color(0.44f, 0.81f, 0.36f),
            Aspect.Somnara => new Color(0.48f, 0.55f, 1f),
            Aspect.Veloria => new Color(1f, 0.48f, 0.85f),
            _ => new Color(0.25f, 0.88f, 0.77f)
        };

        /// <summary>The effect line with its current number: "+12 % ATK und HP", "25 % Chance …".</summary>
        public static string EffectText(GameContent content, FragmentItem item, Aspect? heroAspect = null)
        {
            if (item.Slot == FragmentSlot.Core)
            {
                // without a hero the base value is shown (any other Aspect than the Fragment's own)
                var aspect = heroAspect ?? (Aspect)(((int)item.Aspect + 1) % 5);
                var match = aspect == item.Aspect;
                var atk = Fragments.CoreBonus(content, item, aspect).attack;
                return Loc.T(match ? "fragment.coreMatch" : "fragment.core", (atk / 10f).ToString("0.#"));
            }
            var effect = Fragments.Effect(content, item.Effect);
            if (effect == null)
                return "";
            var value = Fragments.MainValue(content, item);
            // energy and milliseconds are shown as they are, everything else is permille → percent
            var number = effect.Components.Count > 0 && effect.Components[0].Scale == FragmentScale.Multiplier
                ? value.ToString()
                : (value / 10f).ToString("0.#");
            return (effect.Description ?? "").Replace("{0}", number);
        }

        public static string EffectName(GameContent content, FragmentItem item) =>
            item.Slot == FragmentSlot.Core ? SlotName(FragmentSlot.Core) : Fragments.Effect(content, item.Effect)?.Name ?? "";

        public static VisualElement Icon(UiContext ui, FragmentItem item, string size = "fragment__icon")
        {
            var icon = UiKit.Box(size);
            var texture = ui.Catalog.Icon(UiKit.Lower(item.Slot) + "_fragment");
            if (texture != null)
            {
                icon.style.backgroundImage = new StyleBackground(texture);
                icon.style.unityBackgroundImageTintColor = AspectColor(item.Aspect);
            }
            else
            {
                icon.AddToClassList("fragment__icon--placeholder");
                icon.style.backgroundColor = AspectColor(item.Aspect);
            }
            return icon;
        }

        /// <summary>Compact tile: icon in the Aspect colour, grade frame, level, effect name.</summary>
        public static VisualElement Tile(UiContext ui, FragmentItem item)
        {
            var tile = UiKit.Box("fragment", "hero-card--" + UiKit.GradeClass(item.Grade));
            var head = UiKit.Box("row").Into(tile);
            UiKit.Text(item.Aspect.ToString(), "fragment__aspect").Into(head).style.color = AspectColor(item.Aspect);
            UiKit.Box("spacer").Into(head);
            if (item.Level > 0)
                UiKit.Text("+" + item.Level, "gear__level").Into(head);
            tile.Add(Icon(ui, item));
            UiKit.Text(EffectName(ui.Content, item), "fragment__name", "grade--" + UiKit.GradeClass(item.Grade)).Into(tile);
            return tile;
        }

        public static VisualElement EmptyTile(FragmentSlot slot, string text)
        {
            var tile = UiKit.Box("fragment", "gear--empty");
            UiKit.Text(SlotName(slot), "gear__slot").Into(tile);
            UiKit.Text(text, text == "+" ? "slot__plus" : "label--small").Into(tile);
            return tile;
        }

        /// <summary>Full description block of one Fragment (name, grade, level, effect, drawback, wearer).</summary>
        public static VisualElement Details(UiContext ui, FragmentItem item, Aspect? heroAspect = null)
        {
            var box = UiKit.Box("fragment-details");
            var row = UiKit.Box("row").Into(box);
            row.Add(Icon(ui, item, "fragment__icon--big"));
            var texts = UiKit.Box("grow").Into(row);
            texts.style.flexBasis = 0;
            UiKit.Text(Name(item), "building__name").Into(texts);
            UiKit.Text($"{Loc.T("grade." + UiKit.Lower(item.Grade))} · " +
                       Loc.T("fragment.level", item.Level, Fragments.GradeStats(ui.Content, item.Grade).MaxLevel),
                "label--small", "grade--" + UiKit.GradeClass(item.Grade)).Into(texts);
            if (item.Slot != FragmentSlot.Core)
                UiKit.Text(EffectName(ui.Content, item), "subtitle").Into(texts);
            UiKit.Text(EffectText(ui.Content, item, heroAspect), "skill__text").Into(box);
            if (item.EquippedBy != null)
                UiKit.Text(Loc.T("fragment.wornBy", ui.Content.Hero(item.EquippedBy).Name), "gear__owner").Into(box);
            return box;
        }

        /// <summary>Upgrade button with its cost; null at max level.</summary>
        public static VisualElement UpgradeRow(UiContext ui, FragmentItem item, Action upgraded)
        {
            var row = UiKit.Box("row", "center");
            if (item.Level >= Fragments.GradeStats(ui.Content, item.Grade).MaxLevel)
            {
                UiKit.Text(Loc.T("gear.maxLevel"), "label--small").Into(row);
                return row;
            }
            var cost = Fragments.UpgradeCost(ui.Content, item);
            row.Add(GearUi.CostRow(ui.State, cost));
            var button = UiKit.Btn(Loc.T("fragment.upgrade"), () =>
            {
                if (ui.Try(new UpgradeFragment { Uid = item.Uid }, out int _))
                    upgraded();
            }).Into(row);
            button.SetEnabled(Fragments.ShrineLevel(ui.State, ui.Content) > 0 && cost.All(c => ui.State.Get(c.Id) >= c.Value));
            return row;
        }

        /// <summary>Popup for one hero slot: upgrade or remove the worn Fragment, or pick one from the bag.</summary>
        public static void OpenSlot(UiContext ui, VisualElement host, string heroId, FragmentSlot slot, Action changed)
        {
            var overlay = UiKit.Box("overlay").Into(host);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            UiKit.Text(SlotName(slot), "title").Into(popup);
            var heroAspect = ui.Content.Hero(heroId).Aspect;

            void Close()
            {
                overlay.RemoveFromHierarchy();
                changed?.Invoke();
            }

            void Reopen()
            {
                overlay.RemoveFromHierarchy();
                OpenSlot(ui, host, heroId, slot, changed);
            }

            var worn = Fragments.Equipped(ui.State, heroId, slot);
            if (worn != null)
            {
                popup.Add(Details(ui, worn, heroAspect));
                popup.Add(UpgradeRow(ui, worn, Reopen));
                UiKit.Btn(Loc.T("fragment.unequip"), () =>
                {
                    if (ui.Try(new UnequipFragment { Uid = worn.Uid }, out bool _))
                        Close();
                }, "btn--secondary").Into(popup);
            }

            UiKit.Text(Loc.T("fragment.bag"), "subtitle").Into(popup);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(popup);
            scroll.style.maxHeight = 900;
            var grid = UiKit.Box("hero-grid").Into(scroll);
            var options = ui.State.Fragments.Where(f => f.Slot == slot && f.EquippedBy != heroId)
                .OrderByDescending(f => f.Grade).ThenByDescending(f => f.Aspect == heroAspect).ThenByDescending(f => f.Level).ToList();
            foreach (var item in options)
            {
                var tile = Tile(ui, item).Into(grid);
                if (item.EquippedBy != null)
                    UiKit.Text(ui.Content.Hero(item.EquippedBy).Name, "gear__owner").Into(tile);
                var uid = item.Uid;
                tile.RegisterCallback<ClickEvent>(_ =>
                {
                    if (ui.Try(new EquipFragment { HeroId = heroId, Uid = uid }, out bool _))
                        Close();
                });
            }
            if (options.Count == 0)
                UiKit.Text(Loc.T("fragment.none"), "label--small").Into(grid).style.whiteSpace = WhiteSpace.Normal;
            UiKit.Btn(Loc.T("village.close"), Close, "btn--secondary").Into(popup);
        }

        /// <summary>Hero detail section: the three slots, locked ones show the Shrine level they need.</summary>
        public static void Slots(UiContext ui, VisualElement row, VisualElement host, string heroId, Action changed)
        {
            row.Clear();
            foreach (FragmentSlot slot in Enum.GetValues(typeof(FragmentSlot)))
            {
                var s = slot;
                VisualElement tile;
                if (!Fragments.SlotOpen(ui.State, ui.Content, slot))
                {
                    tile = EmptyTile(slot, Loc.T("fragment.slotLocked", Fragments.SlotShrineLevel(ui.Content, slot)));
                    tile.AddToClassList("slot--locked");
                }
                else
                {
                    var worn = Fragments.Equipped(ui.State, heroId, slot);
                    tile = worn != null ? Tile(ui, worn) : EmptyTile(slot, "+");
                    tile.RegisterCallback<ClickEvent>(_ => OpenSlot(ui, host, heroId, s, changed));
                }
                row.Add(tile);
            }
        }

        public static string Chances(int[] weights)
        {
            if (weights == null)
                return "";
            var total = (float)weights.Sum();
            return Loc.T("fragment.chances", string.Join("  ·  ", weights.Select((w, g) => (w, g)).Where(t => t.w > 0)
                .Select(t => $"{Loc.T("grade." + UiKit.Lower((Grade)t.g))} {100f * t.w / total:0.#}%")));
        }
    }

    /// <summary>
    /// Fragment Shrine bag: all Fragments by slot, details with upgrade and dissolve, and fusing three equal Fragments
    /// into the next grade.
    /// </summary>
    public sealed class FragmentScreen : UiScreen
    {
        private CurrencyBar _currencies;
        private VisualElement _tabs;
        private VisualElement _grid;
        private Label _hint;
        private Button _fuse;
        private FragmentSlot? _filter;
        private bool _fusing;
        private readonly List<int> _selected = new List<int>();

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            _currencies = new CurrencyBar(Ids.Gold, Ids.FragmentDust).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.IconBtn("back", () => Ui.Router.Back(() => new VillageScreen()), "back-btn").Into(root);
            UiKit.Text(Loc.T("fragment.title"), "title", "screen-title").Into(head);
            UiKit.Box("spacer").Into(head);
            UiKit.Btn(Loc.T("fragment.fuse"), ToggleFuse, "btn--secondary", "btn--small").Into(head);
            _tabs = UiKit.Box("tabs").Into(safe);
            _hint = UiKit.Text("", "label--small").Into(safe);
            _hint.style.whiteSpace = WhiteSpace.Normal;
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _grid = UiKit.Box("hero-grid").Into(scroll);
            _fuse = UiKit.Btn(Loc.T("fragment.fuse"), Fuse).Into(safe);
        }

        public override void Refresh()
        {
            _currencies.Refresh(Ui.State);
            _tabs.Clear();
            Tab(null, Loc.T("fragment.all"));
            foreach (FragmentSlot slot in System.Enum.GetValues(typeof(FragmentSlot)))
                Tab(slot, FragmentUi.SlotName(slot));

            _selected.RemoveAll(uid => Fragments.Find(Ui.State, uid) == null);
            _hint.text = _fusing ? Loc.T("fragment.fuseHint", Ui.Content.Fragments.FuseCount) : "";
            _hint.style.display = _fusing ? DisplayStyle.Flex : DisplayStyle.None;
            _fuse.style.display = _fusing ? DisplayStyle.Flex : DisplayStyle.None;
            _fuse.SetEnabled(_selected.Count == Ui.Content.Fragments.FuseCount);

            _grid.Clear();
            var main = _selected.Count > 0 ? Fragments.Find(Ui.State, _selected[0]) : null;
            var items = Ui.State.Fragments.Where(f => _filter == null || f.Slot == _filter)
                .OrderBy(f => f.Slot).ThenByDescending(f => f.Grade).ThenByDescending(f => f.Level).ToList();
            foreach (var item in items)
            {
                var tile = FragmentUi.Tile(Ui, item).Into(_grid);
                if (item.EquippedBy != null)
                    UiKit.Text(Ui.Content.Hero(item.EquippedBy).Name, "gear__owner").Into(tile);
                var index = _selected.IndexOf(item.Uid);
                if (index >= 0)
                {
                    tile.AddToClassList("hero-card--selected");
                    UiKit.Text((index + 1).ToString(), "fragment__pick").Into(tile);
                }
                // in fuse mode only Fragments that fit the first pick can be chosen
                var usable = !_fusing || (item.EquippedBy == null && (main == null || (item.Slot == main.Slot && item.Grade == main.Grade)));
                tile.SetEnabled(usable || index >= 0);
                var uid = item.Uid;
                tile.RegisterCallback<ClickEvent>(_ => Tap(uid));
            }
            if (items.Count == 0)
                UiKit.Text(Loc.T("fragment.none"), "label--small").Into(_grid).style.whiteSpace = WhiteSpace.Normal;
        }

        private void Tab(FragmentSlot? slot, string text)
        {
            var b = UiKit.Btn(text, () =>
            {
                _filter = slot;
                Refresh();
            }, "tab").Into(_tabs);
            b.RemoveFromClassList("btn");
            if (_filter == slot)
                b.AddToClassList("tab--active");
        }

        private void ToggleFuse()
        {
            _fusing = !_fusing;
            _selected.Clear();
            Refresh();
        }

        private void Tap(int uid)
        {
            if (_fusing)
            {
                if (!_selected.Remove(uid) && _selected.Count < Ui.Content.Fragments.FuseCount)
                    _selected.Add(uid);
                Refresh();
                return;
            }
            var item = Fragments.Find(Ui.State, uid);
            if (item != null)
                Open(item);
        }

        private void Open(FragmentItem item)
        {
            var overlay = UiKit.Box("overlay").Into(Root);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            popup.Add(FragmentUi.Details(Ui, item));
            popup.Add(FragmentUi.UpgradeRow(Ui, item, () =>
            {
                overlay.RemoveFromHierarchy();
                Refresh();
                Open(item);
            }));
            var buttons = UiKit.Box("row", "center").Into(popup);
            if (item.EquippedBy == null)
                UiKit.Btn(Loc.T("fragment.dissolve", UiKit.Short(Fragments.DissolveValue(Ui.Content, item))), () =>
                {
                    if (Ui.Try(new DissolveFragment { Uid = item.Uid }, out long _))
                    {
                        overlay.RemoveFromHierarchy();
                        Refresh();
                    }
                }, "btn--secondary").Into(buttons);
            UiKit.Btn(Loc.T("village.close"), () => overlay.RemoveFromHierarchy(), "btn--secondary").Into(buttons);
        }

        private void Fuse()
        {
            if (!Ui.Try(new FuseFragments { Uids = new List<int>(_selected) }, out FragmentItem result))
                return;
            _selected.Clear();
            _fusing = false;
            Refresh();
            var overlay = UiKit.Box("overlay").Into(Root);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            UiKit.Text(Loc.T("fragment.fused"), "title").Into(popup);
            popup.Add(FragmentUi.Details(Ui, result));
            UiKit.Btn(Loc.T("battle.continue"), () => overlay.RemoveFromHierarchy()).Into(popup);
        }
    }
}
