using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>
    /// Dawnhollow as a drawn town (AFK Arena's Ranhorn, the 7DS Idle village): every building stands on its plot,
    /// ruins until rebuilt. Tapping a building opens its panel; the Survivor Camp shows a bubble to collect production.
    /// </summary>
    public sealed class VillageScreen : UiScreen
    {
        private CurrencyBar _currencies;
        private MapView _map;
        private VisualElement _popupLayer;
        private string _openId;
        private IVisualElementScheduledItem _ticker;
        private Label _bubble;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            var town = Ui.Catalog.town;
            _map = new MapView(town?.background, 2f / 3f, "map__canvas--town").Into(root);
            _currencies = new CurrencyBar(Ids.Gold, Ids.Wood, Ids.Stone).Into(root);
            UiKit.Text("Dawnhollow", "map-title").Into(root);
            UiKit.Box("spacer").Into(root).pickingMode = PickingMode.Ignore;
            root.Add(MainNav.Build(Ui, Tab.Town));
            _popupLayer = UiKit.Box("popup-layer").Into(root);
            _popupLayer.pickingMode = PickingMode.Ignore;
        }

        public override void OnShow()
        {
            base.OnShow();
            _ticker = Root.schedule.Execute(RefreshBubble).Every(1000);
        }

        public override void OnHide() => _ticker?.Pause();

        public override void Refresh()
        {
            _currencies.Refresh(Ui.State);
            _map.ClearPlaced();
            _bubble = null;
            var layout = Ui.Catalog.Layout();
            var defs = Ui.Content.Village.Buildings;
            for (var i = 0; i < defs.Count; i++)
                PlaceBuilding(defs[i], layout.Building(defs[i].Id) ?? FallbackSpot(defs[i].Id, i));
            RefreshBubble();
            if (_openId != null)
                Open(_openId);
        }

        /// <summary>Grid positions until town.json places the building.</summary>
        private static Spot FallbackSpot(string id, int index) =>
            new Spot { id = id, x = 0.2f + 0.3f * (index % 3), y = 0.3f + 0.19f * (index / 3), size = 0.26f };

        private void PlaceBuilding(BuildingDefinition def, Spot spot)
        {
            var state = Ui.State;
            var level = Village.Level(state, def);
            var town = Ui.Catalog.town;
            var image = level > 0 ? town?.Building(def.Id) : town?.ruin;
            var view = UiKit.Box("town-building");
            view.EnableInClassList("town-building--ruin", level <= 0);
            var aspect = 1f;
            if (image != null)
            {
                var art = UiKit.Box("town-building__art").Into(view);
                art.style.backgroundImage = new StyleBackground(image);
                aspect = image.width / (float)image.height;
            }
            else
                UiKit.Box("town-building__placeholder").Into(view);

            var plate = UiKit.Box("town-building__plate").Into(view);
            UiKit.Text(def.Name, "town-building__name").Into(plate);
            string sub;
            if (def.Effect == BuildingEffect.None)
                sub = Loc.T("village.soon");
            else if (level > 0)
                sub = def.MaxLevel > 1 ? Loc.T("village.lv", level) : null;
            else if (!Village.Unlocked(state, def))
                sub = Loc.T("village.lockedStage", def.UnlockStage);
            else
                sub = Loc.T("village.ruin");
            if (sub != null)
                UiKit.Text(sub, "town-building__level").Into(plate);
            if (BuildingPanel.CanUpgrade(state, def))
                UiKit.Box("badge").Into(plate);

            view.RegisterCallback<ClickEvent>(_ =>
            {
                if (!_map.Dragged)
                    Open(def.Id);
            });
            // rubble is flatter than the buildings it stands in for
            var placed = level > 0 ? spot : new Spot { id = spot.id, x = spot.x, y = spot.y, size = spot.size * 0.85f };
            _map.Place(view, placed, aspect, true);

            if (def.Effect == BuildingEffect.Production && level > 0)
            {
                // collect bubble floats above the camp like the AFK Arena loot bubbles
                _bubble = UiKit.Text("", "town-bubble");
                _bubble.RegisterCallback<ClickEvent>(e =>
                {
                    e.StopPropagation();
                    if (Ui.Try(new CollectVillage(), out _))
                        Refresh();
                });
                // building height as a fraction of the picture height, bubble sits on its upper part
                var height = spot.size / aspect * _map.Aspect;
                var bubbleSpot = new Spot { id = "bubble", x = spot.x, y = Mathf.Max(0.06f, spot.y - height * 0.8f), size = 0.3f };
                _map.Place(_bubble, bubbleSpot, 2.6f, true);
            }
        }

        private void RefreshBubble()
        {
            if (_bubble == null)
                return;
            var pending = Village.Pending(Ui.State, Ui.Content, Ui.Authority.NowMs).Where(kv => kv.Value > 0).OrderBy(kv => kv.Key).ToList();
            _bubble.style.display = pending.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (pending.Count > 0)
                _bubble.text = string.Join("  ", pending.Select(kv => $"+{UiKit.Short(kv.Value)} {UiKit.CurrencyName(kv.Key)}"));
        }

        /// <summary>Opens a building's panel as if tapped (autopilot, deep links from quests).</summary>
        public void OpenBuilding(string id) => Open(id);

        private void Open(string id)
        {
            _openId = id;
            _popupLayer.Clear();
            var def = Ui.Content.Village.Buildings.FirstOrDefault(b => b.Id == id);
            if (def == null)
                return;
            var overlay = UiKit.Box("overlay").Into(_popupLayer);
            overlay.RegisterCallback<ClickEvent>(e =>
            {
                if (e.target == overlay)
                    Close();
            });
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(overlay);
            scroll.AddToClassList("building-popup");
            var card = new BuildingPanel(Ui, Root, Refresh).Build(def).Into(scroll);
            var close = UiKit.Btn("×", Close, "btn--secondary", "btn--small", "building-popup__close");
            card.Insert(0, close);
        }

        private void Close()
        {
            _openId = null;
            _popupLayer.Clear();
        }
    }

    /// <summary>One building's details: effect, upgrade, hero slots and what the building unlocks (forge, Hunt Board, dungeons).</summary>
    public sealed class BuildingPanel
    {
        private readonly UiContext _ui;
        private readonly VisualElement _root;
        private readonly System.Action _refresh;

        public BuildingPanel(UiContext ui, VisualElement root, System.Action refresh)
        {
            _ui = ui;
            _root = root;
            _refresh = refresh;
        }

        public static bool CanUpgrade(PlayerState state, BuildingDefinition def)
        {
            if (def.Effect == BuildingEffect.None || !Village.Unlocked(state, def))
                return false;
            var level = Village.Level(state, def);
            return level < def.MaxLevel && Village.UpgradeCost(def, level).All(c => state.Get(c.Id) >= c.Value);
        }

        public VisualElement Build(BuildingDefinition def)
        {
            var state = _ui.State;
            var level = Village.Level(state, def);
            var card = UiKit.Box("panel", "building").With(level > 0 ? "building--built" : "building--ruin");

            var art = _ui.Catalog.town != null ? (level > 0 ? _ui.Catalog.town.Building(def.Id) : _ui.Catalog.town.ruin) : null;
            if (art != null)
            {
                var image = UiKit.Box("building__art").Into(card);
                image.style.backgroundImage = new StyleBackground(art);
            }

            var head = UiKit.Box("row").Into(card);
            UiKit.Text(def.Name, "building__name").Into(head);
            UiKit.Box("spacer").Into(head);
            var levelText = level > 0
                ? (def.MaxLevel > 1 ? Loc.T("village.level", level, def.MaxLevel) : Loc.T("village.built"))
                : Loc.T("village.ruin");
            UiKit.Text(levelText, "building__level").Into(head);
            UiKit.Text(def.Description, "skill__text").Into(card);

            var effect = EffectText(def, level);
            if (!string.IsNullOrEmpty(effect))
                UiKit.Text(effect, "label--good").Into(card);

            if (def.Effect == BuildingEffect.None)
            {
                UiKit.Text(Loc.T("village.planned"), "label--small").Into(card);
                card.AddToClassList("building--planned");
                return card;
            }

            if (level > 0 && def.Effect == BuildingEffect.Production)
                card.Add(Storage());
            if (level > 0 && def.SlotLevels.Length > 0)
                card.Add(Slots(def, level));
            if (level > 0 && def.Effect == BuildingEffect.Forge)
                card.Add(Forge());
            if (level > 0 && def.Effect == BuildingEffect.Shrine)
                card.Add(Shrine());
            if (level > 0 && def.Effect == BuildingEffect.Awakening)
                card.Add(AwakeningList());
            if (level > 0 && def.Effect == BuildingEffect.Hunting)
                UiKit.Btn("Hunt Board", () => _ui.Router.Push(new HuntScreen())).Into(card);
            if (level > 0 && def.Effect == BuildingEffect.Portal)
                UiKit.Btn(Loc.T("dungeon.title"), () => _ui.Router.Push(new DungeonScreen())).Into(card);
            if (level > 0 && def.Effect == BuildingEffect.UnlockSummon)
                UiKit.Btn(Loc.T("nav.summon"), () => _ui.Router.Reset(new SummonScreen())).Into(card);

            if (!Village.Unlocked(state, def))
            {
                UiKit.Text(Loc.T("village.lockedStage", def.UnlockStage), "label--small").Into(card);
                return card;
            }
            if (level >= def.MaxLevel)
                return card;

            var cost = Village.UpgradeCost(def, level);
            var row = UiKit.Box("row").Into(card);
            foreach (var c in cost)
            {
                var chip = UiKit.Box("row", "cost").Into(row);
                UiKit.Box("currency__icon", "icon--" + c.Id).Into(chip);
                UiKit.Text(UiKit.Short(c.Value), state.Get(c.Id) >= c.Value ? "cost__value" : "label--bad").Into(chip);
            }
            UiKit.Box("spacer").Into(row);
            var button = UiKit.Btn(Loc.T(level == 0 ? "village.build" : "village.upgrade"), () => Upgrade(def.Id)).Into(row);
            button.SetEnabled(cost.All(c => state.Get(c.Id) >= c.Value));
            return card;
        }

        private VisualElement Storage()
        {
            var box = UiKit.Box("forge");
            UiKit.Text(Loc.T("village.storage"), "subtitle").Into(box);
            var pending = Village.Pending(_ui.State, _ui.Content, _ui.Authority.NowMs).Where(kv => kv.Value > 0).OrderBy(kv => kv.Key).ToList();
            var stored = UiKit.Box("afk-rewards").Into(box);
            foreach (var kv in pending)
                stored.Add(UiKit.Amount(kv.Key, kv.Value));
            if (pending.Count == 0)
                stored.Add(UiKit.Text(Loc.T("afk.empty"), "label--small"));
            var rates = Village.ProductionPerHour(_ui.State, _ui.Content);
            UiKit.Text(Loc.T("village.rate", string.Join("  ·  ", rates.OrderBy(kv => kv.Key)
                .Select(kv => $"{UiKit.Short(kv.Value)} {UiKit.CurrencyName(kv.Key)}"))), "label--small").Into(box);
            var collect = UiKit.Btn(Loc.T("village.collect"), () =>
            {
                if (_ui.Try(new CollectVillage(), out _))
                    _refresh();
            }).Into(box);
            collect.SetEnabled(pending.Count > 0);
            return box;
        }

        private string EffectText(BuildingDefinition def, int level)
        {
            if (level <= 0)
                return null;
            switch (def.Effect)
            {
                case BuildingEffect.Production:
                    return Loc.T("village.effect.production", string.Join("  ·  ",
                        Village.ProductionOf(_ui.State, _ui.Content, def).Select(kv => $"{UiKit.Short(kv.Value)} {UiKit.CurrencyName(kv.Key)}")));
                case BuildingEffect.Healing:
                    return Loc.T("village.effect.healing", (Village.TotalBonus(_ui.State, _ui.Content, def) / 10f).ToString("0.#"));
                case BuildingEffect.UnlockSummon:
                    return Loc.T("village.effect.summon");
                case BuildingEffect.Portal:
                    return Loc.T("village.effect.portal", _ui.Content.Dungeons.Dungeons.Count(d => d.PortalLevel <= level));
                case BuildingEffect.Awakening:
                    return Loc.T("village.effect.awakening");
                case BuildingEffect.Shrine:
                    return Loc.T("village.effect.shrine", string.Join(", ", System.Enum.GetValues(typeof(FragmentSlot)).Cast<FragmentSlot>()
                        .Where(s => Fragments.SlotOpen(_ui.State, _ui.Content, s)).Select(FragmentUi.SlotName)));
                case BuildingEffect.Hunting:
                    return Loc.T("village.effect.hunting", Hunts.DailyCount(_ui.Content, level), Hunts.WeeklyCount(_ui.Content, level));
                case BuildingEffect.Forge:
                    var w = Gear.CraftWeights(_ui.State, _ui.Content);
                    if (w == null)
                        return null;
                    var total = (float)w.Sum();
                    return Loc.T("village.effect.forge", string.Join("  ·  ", w.Select((x, g) => (x, g)).Where(t => t.x > 0)
                        .Select(t => $"{Loc.T("grade." + UiKit.Lower((Core.Content.Grade)t.g))} {100f * t.x / total:0.#}%")));
                default:
                    return null;
            }
        }

        private VisualElement Slots(BuildingDefinition def, int level)
        {
            var row = UiKit.Box("row", "building__slots");
            var heroes = Village.Heroes(_ui.State, def);
            for (var i = 0; i < def.SlotLevels.Length; i++)
            {
                var slot = i;
                var open = level >= def.SlotLevels[i];
                var id = i < heroes.Length ? heroes[i] : null;
                VisualElement view;
                if (!open)
                {
                    view = UiKit.Box("slot", "slot--small", "slot--locked");
                    UiKit.Text(Loc.T("village.slotLevel", def.SlotLevels[i]), "label--small").Into(view);
                }
                else if (string.IsNullOrEmpty(id))
                {
                    view = UiKit.Box("slot", "slot--small");
                    UiKit.Text("+", "slot__plus").Into(view);
                    view.RegisterCallback<ClickEvent>(_ => PickHero(def, slot));
                }
                else
                {
                    view = UiKit.Box("slot", "slot--small");
                    var owned = _ui.State.FindHero(id);
                    view.Add(UiKit.HeroCard(_ui.Catalog, _ui.Content.Hero(id), owned));
                    UiKit.Text("+" + (Village.HeroBonus(_ui.Content, def, owned) / 10f).ToString("0.#") + "%", "slot__bonus").Into(view);
                    view.RegisterCallback<ClickEvent>(_ => PickHero(def, slot));
                }
                row.Add(view);
            }
            return row;
        }

        private void PickHero(BuildingDefinition def, int slot)
        {
            var overlay = UiKit.Box("overlay").Into(_root);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            UiKit.Text(def.Name, "title").Into(popup);
            UiKit.Text(Loc.T("village.pickHint"), "label--small").Into(popup);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(popup);
            scroll.style.maxHeight = 1100;
            var grid = UiKit.Box("hero-grid").Into(scroll);
            var current = Village.Heroes(_ui.State, def);
            foreach (var owned in _ui.State.Heroes.OrderByDescending(h => Village.HeroBonus(_ui.Content, def, h)).ThenBy(h => h.HeroId))
            {
                var heroId = owned.HeroId;
                var card = UiKit.HeroCard(_ui.Catalog, _ui.Content.Hero(heroId), owned).Into(grid);
                UiKit.Text("+" + (Village.HeroBonus(_ui.Content, def, owned) / 10f).ToString("0.#") + "%", "slot__bonus").Into(card);
                if (Village.WorkplaceOf(_ui.State, heroId) != null)
                    card.AddToClassList("hero-card--selected");
                card.RegisterCallback<ClickEvent>(_ =>
                {
                    if (_ui.Try(new AssignVillageHero { BuildingId = def.Id, Slot = slot, HeroId = heroId }, out bool _))
                        overlay.RemoveFromHierarchy();
                });
            }
            var buttons = UiKit.Box("row", "center").Into(popup);
            if (slot < current.Length && !string.IsNullOrEmpty(current[slot]))
                UiKit.Btn(Loc.T("village.remove"), () =>
                {
                    if (_ui.Try(new AssignVillageHero { BuildingId = def.Id, Slot = slot, HeroId = null }, out bool _))
                        overlay.RemoveFromHierarchy();
                }, "btn--secondary").Into(buttons);
            UiKit.Btn(Loc.T("village.close"), () => overlay.RemoveFromHierarchy(), "btn--secondary").Into(buttons);
        }

        private VisualElement Forge()
        {
            var box = UiKit.Box("forge");
            UiKit.Text(Loc.T("gear.craft"), "subtitle").Into(box);
            box.Add(GearUi.CostRow(_ui.State, _ui.Content.Gear.CraftCost));
            var row = UiKit.Box("row").Into(box);
            row.style.flexWrap = Wrap.Wrap;
            var affordable = _ui.Content.Gear.CraftCost.All(c => _ui.State.Get(c.Id) >= c.Value);
            foreach (GearSlot slot in System.Enum.GetValues(typeof(GearSlot)))
            {
                var s = slot;
                var b = UiKit.Btn(GearUi.SlotName(slot), () => Craft(s), "btn--secondary", "btn--small").Into(row);
                b.SetEnabled(affordable);
            }
            return box;
        }

        private VisualElement Shrine()
        {
            var box = UiKit.Box("forge");
            UiKit.Text(Loc.T("fragment.stabilize"), "subtitle").Into(box);
            UiKit.Text(FragmentUi.Chances(Fragments.StabilizeWeights(_ui.State, _ui.Content)), "label--small").Into(box);
            box.Add(GearUi.CostRow(_ui.State, _ui.Content.Fragments.StabilizeCost));
            var row = UiKit.Box("row").Into(box);
            row.style.flexWrap = Wrap.Wrap;
            var affordable = _ui.Content.Fragments.StabilizeCost.All(c => _ui.State.Get(c.Id) >= c.Value);
            foreach (FragmentSlot slot in System.Enum.GetValues(typeof(FragmentSlot)))
            {
                var s = slot;
                var b = UiKit.Btn(FragmentUi.SlotName(slot), () => Stabilize(s), "btn--secondary", "btn--small").Into(row);
                b.SetEnabled(affordable && Fragments.SlotOpen(_ui.State, _ui.Content, slot));
            }
            var lists = UiKit.Box("row").Into(box);
            UiKit.Btn(Loc.T("fragment.title") + $" ({_ui.State.Fragments.Count})", () => _ui.Router.Push(new FragmentScreen())).Into(lists).style.flexGrow = 1;
            UiKit.Btn(Loc.T("relic.title") + $" ({_ui.State.Relics.Count})", () => _ui.Router.Push(new RelicScreen())).Into(lists).style.flexGrow = 1;
            return box;
        }

        private VisualElement AwakeningList()
        {
            var box = UiKit.Box("forge");
            UiKit.Text(Loc.T("awaken.heroes"), "subtitle").Into(box);
            var grid = UiKit.Box("hero-grid").Into(box);
            var any = false;
            foreach (var a in _ui.Content.Awakening.Heroes)
            {
                var owned = _ui.State.FindHero(a.HeroId);
                if (owned == null)
                    continue;
                any = true;
                var card = UiKit.HeroCard(_ui.Catalog, _ui.Content.Hero(a.HeroId), owned).Into(grid);
                if (owned.Awakened)
                    UiKit.Text(Loc.T("awaken.done"), "new-badge").Into(card);
                var id = a.HeroId;
                card.RegisterCallback<ClickEvent>(_ => _ui.Router.Push(new HeroDetailScreen(id)));
            }
            if (!any)
                UiKit.Text(Loc.T("awaken.none"), "label--small").Into(box).style.whiteSpace = WhiteSpace.Normal;
            return box;
        }

        private void Stabilize(FragmentSlot slot)
        {
            if (!_ui.Try(new StabilizeFragment { Slot = slot }, out FragmentItem item))
                return;
            _refresh();
            var overlay = UiKit.Box("overlay").Into(_root);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            UiKit.Text(Loc.T("fragment.stabilized"), "title").Into(popup);
            popup.Add(FragmentUi.Details(_ui, item));
            UiKit.Btn(Loc.T("battle.continue"), () => overlay.RemoveFromHierarchy()).Into(popup);
        }

        private void Craft(GearSlot slot)
        {
            if (!_ui.Try(new CraftGear { Slot = slot }, out GearItem item))
                return;
            _refresh();
            var overlay = UiKit.Box("overlay").Into(_root);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            UiKit.Text(Loc.T("gear.crafted"), "title").Into(popup);
            popup.Add(GearUi.Tile(_ui.Content, item));
            UiKit.Btn(Loc.T("battle.continue"), () => overlay.RemoveFromHierarchy()).Into(popup);
        }

        private void Upgrade(string id)
        {
            if (_ui.Try(new UpgradeBuilding { BuildingId = id }, out _))
                _refresh();
        }
    }
}
