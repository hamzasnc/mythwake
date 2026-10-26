using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Dawnhollow: rebuild buildings, collect production, place heroes in building slots.</summary>
    public sealed class VillageScreen : UiScreen
    {
        private CurrencyBar _currencies;
        private VisualElement _stored;
        private Label _rates;
        private Button _collect;
        private VisualElement _list;
        private IVisualElementScheduledItem _ticker;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            _currencies = new CurrencyBar(Ids.Gold, Ids.Wood, Ids.Stone).Into(root);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(root);
            scroll.style.flexGrow = 1;
            var safe = UiKit.Box("safe").Into(scroll);
            UiKit.Text("Dawnhollow", "title").Into(safe);

            var storage = UiKit.Box("panel").Into(safe);
            UiKit.Text(Loc.T("village.storage"), "subtitle").Into(storage);
            _stored = UiKit.Box("afk-rewards").Into(storage);
            _rates = UiKit.Text("", "label--small").Into(storage);
            _collect = UiKit.Btn(Loc.T("village.collect"), Collect).Into(storage);

            _list = UiKit.Box().Into(safe);
            root.Add(MainNav.Build(Ui, Tab.Village));
        }

        public override void OnShow()
        {
            base.OnShow();
            _ticker = Root.schedule.Execute(RefreshStorage).Every(1000);
        }

        public override void OnHide() => _ticker?.Pause();

        public override void Refresh()
        {
            _currencies.Refresh(Ui.State);
            RefreshStorage();
            _list.Clear();
            foreach (var def in Ui.Content.Village.Buildings)
                _list.Add(BuildingCard(def));
        }

        private void RefreshStorage()
        {
            var pending = Village.Pending(Ui.State, Ui.Content, Ui.Authority.NowMs).Where(kv => kv.Value > 0).ToList();
            _stored.Clear();
            foreach (var kv in pending.OrderBy(kv => kv.Key))
                _stored.Add(UiKit.Amount(kv.Key, kv.Value));
            if (pending.Count == 0)
                _stored.Add(UiKit.Text(Loc.T("afk.empty"), "label--small"));
            var rates = Village.ProductionPerHour(Ui.State, Ui.Content);
            _rates.text = Loc.T("village.rate", string.Join("  ·  ", rates.OrderBy(kv => kv.Key).Select(kv => $"{UiKit.Short(kv.Value)} {UiKit.CurrencyName(kv.Key)}")));
            _collect.SetEnabled(pending.Count > 0);
        }

        private VisualElement BuildingCard(BuildingDefinition def)
        {
            var state = Ui.State;
            var level = Village.Level(state, def);
            var card = UiKit.Box("panel", "building").With(level > 0 ? "building--built" : "building--ruin");

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

            if (level > 0 && def.SlotLevels.Length > 0)
                card.Add(Slots(def, level));
            if (level > 0 && def.Effect == BuildingEffect.Forge)
                card.Add(Forge());
            if (level > 0 && def.Effect == BuildingEffect.Hunting)
                UiKit.Btn("Hunt Board", () => Ui.Router.Push(new HuntScreen())).Into(card);
            if (level > 0 && def.Effect == BuildingEffect.Portal)
                UiKit.Btn(Loc.T("dungeon.title"), () => Ui.Router.Push(new DungeonScreen())).Into(card);

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

        private string EffectText(BuildingDefinition def, int level)
        {
            if (level <= 0)
                return null;
            switch (def.Effect)
            {
                case BuildingEffect.Production:
                    return Loc.T("village.effect.production", string.Join("  ·  ",
                        Village.ProductionOf(Ui.State, Ui.Content, def).Select(kv => $"{UiKit.Short(kv.Value)} {UiKit.CurrencyName(kv.Key)}")));
                case BuildingEffect.Healing:
                    return Loc.T("village.effect.healing", (Village.TotalBonus(Ui.State, Ui.Content, def) / 10f).ToString("0.#"));
                case BuildingEffect.UnlockSummon:
                    return Loc.T("village.effect.summon");
                case BuildingEffect.Portal:
                    return Loc.T("village.effect.portal", Ui.Content.Dungeons.Dungeons.Count(d => d.PortalLevel <= level));
                case BuildingEffect.Hunting:
                    return Loc.T("village.effect.hunting", Hunts.DailyCount(Ui.Content, level), Hunts.WeeklyCount(Ui.Content, level));
                case BuildingEffect.Forge:
                    var w = Gear.CraftWeights(Ui.State, Ui.Content);
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
            var heroes = Village.Heroes(Ui.State, def);
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
                    var owned = Ui.State.FindHero(id);
                    view.Add(UiKit.HeroCard(Ui.Catalog, Ui.Content.Hero(id), owned));
                    UiKit.Text("+" + (Village.HeroBonus(Ui.Content, def, owned) / 10f).ToString("0.#") + "%", "slot__bonus").Into(view);
                    view.RegisterCallback<ClickEvent>(_ => PickHero(def, slot));
                }
                row.Add(view);
            }
            return row;
        }

        private void PickHero(BuildingDefinition def, int slot)
        {
            var overlay = UiKit.Box("overlay").Into(Root);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            UiKit.Text(def.Name, "title").Into(popup);
            UiKit.Text(Loc.T("village.pickHint"), "label--small").Into(popup);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(popup);
            scroll.style.maxHeight = 1100;
            var grid = UiKit.Box("hero-grid").Into(scroll);
            var current = Village.Heroes(Ui.State, def);
            foreach (var owned in Ui.State.Heroes.OrderByDescending(h => Village.HeroBonus(Ui.Content, def, h)).ThenBy(h => h.HeroId))
            {
                var heroId = owned.HeroId;
                var card = UiKit.HeroCard(Ui.Catalog, Ui.Content.Hero(heroId), owned).Into(grid);
                UiKit.Text("+" + (Village.HeroBonus(Ui.Content, def, owned) / 10f).ToString("0.#") + "%", "slot__bonus").Into(card);
                var work = Village.WorkplaceOf(Ui.State, heroId);
                if (work != null)
                    card.AddToClassList("hero-card--selected");
                card.RegisterCallback<ClickEvent>(_ =>
                {
                    if (Ui.Try(new AssignVillageHero { BuildingId = def.Id, Slot = slot, HeroId = heroId }, out bool _))
                        overlay.RemoveFromHierarchy();
                });
            }
            var buttons = UiKit.Box("row", "center").Into(popup);
            if (slot < current.Length && !string.IsNullOrEmpty(current[slot]))
                UiKit.Btn(Loc.T("village.remove"), () =>
                {
                    if (Ui.Try(new AssignVillageHero { BuildingId = def.Id, Slot = slot, HeroId = null }, out bool _))
                        overlay.RemoveFromHierarchy();
                }, "btn--secondary").Into(buttons);
            UiKit.Btn(Loc.T("village.close"), () => overlay.RemoveFromHierarchy(), "btn--secondary").Into(buttons);
        }

        private VisualElement Forge()
        {
            var box = UiKit.Box("forge");
            UiKit.Text(Loc.T("gear.craft"), "subtitle").Into(box);
            box.Add(GearUi.CostRow(Ui.State, Ui.Content.Gear.CraftCost));
            var row = UiKit.Box("row").Into(box);
            row.style.flexWrap = Wrap.Wrap;
            var affordable = Ui.Content.Gear.CraftCost.All(c => Ui.State.Get(c.Id) >= c.Value);
            foreach (GearSlot slot in System.Enum.GetValues(typeof(GearSlot)))
            {
                var s = slot;
                var b = UiKit.Btn(GearUi.SlotName(slot), () => Craft(s), "btn--secondary", "btn--small").Into(row);
                b.SetEnabled(affordable);
            }
            return box;
        }

        private void Craft(GearSlot slot)
        {
            if (!Ui.Try(new CraftGear { Slot = slot }, out GearItem item))
                return;
            Refresh();
            var overlay = UiKit.Box("overlay").Into(Root);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            UiKit.Text(Loc.T("gear.crafted"), "title").Into(popup);
            popup.Add(GearUi.Tile(Ui.Content, item));
            UiKit.Btn(Loc.T("battle.continue"), () => overlay.RemoveFromHierarchy()).Into(popup);
        }

        private void Upgrade(string id)
        {
            if (Ui.Try(new UpgradeBuilding { BuildingId = id }, out _))
                Refresh();
        }

        private void Collect()
        {
            if (Ui.Try(new CollectVillage(), out _))
                Refresh();
        }
    }
}
