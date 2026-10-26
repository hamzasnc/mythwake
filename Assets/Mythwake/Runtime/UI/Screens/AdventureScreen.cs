using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>
    /// World map with every mode outside the campaign as a pin (AFK Arena's Dark Forest, the 7DS Idle map):
    /// Hunt Board, Dungeons, Arena; Shattered Realms and Guardian Raids stay locked until those systems exist.
    /// </summary>
    public sealed class AdventureScreen : UiScreen
    {
        private MapView _map;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            _map = new MapView(Ui.Catalog.town?.worldMap, 2f / 3f, "map__canvas--world").Into(root);
            new CurrencyBar(Ids.Gold, Ids.GearMaterials, Ids.HuntTokens).Into(root).Refresh(Ui.State);
            UiKit.Text(Loc.T("adventure.title"), "map-title").Into(root);
            UiKit.Box("spacer").Into(root).pickingMode = PickingMode.Ignore;
            root.Add(MainNav.Build(Ui, Tab.Adventure));
        }

        public override void Refresh()
        {
            _map.ClearPlaced();
            var state = Ui.State;
            var content = Ui.Content;
            Pin("campaign", Loc.T("nav.campaign"), true, null, () => Ui.Router.Reset(new HomeScreen()), Art("riftborn_treant"), 0.25f, 0.43f);
            Pin("town", "Dawnhollow", true, null, () => Ui.Router.Reset(new VillageScreen()), Ui.Catalog.town?.Building("survivorCamp"), 0.4f, 0.7f);
            Pin("hunts", "Hunt Board", Hunts.HutLevel(state, content) > 0, Loc.T("adventure.needs", "Hunter's Hut"),
                () => Ui.Router.Push(new HuntScreen()), Art("hollowtusk_alpha"), 0.72f, 0.55f);
            Pin("dungeons", Loc.T("dungeon.title"), Dungeons.PortalLevel(state, content) > 0, Loc.T("adventure.needs", "Portal Chamber"),
                () => Ui.Router.Push(new DungeonScreen()), Art("obsidian_vault_sentinel"), 0.26f, 0.3f);
            Pin("arena", "Arena", state.Campaign.HighestCleared >= content.Arena.UnlockStage, Loc.T("arena.locked", content.Arena.UnlockStage),
                () => Ui.Router.Push(new ArenaScreen()), Art("dragon_echo_of_the_sky"), 0.74f, 0.27f);
            Pin("trials", Loc.T("trial.title"), Trials.Open(state, content), Loc.T("trial.locked", content.Trials.ShrineLevel),
                () => Ui.Router.Push(new TrialScreen()), Ui.Catalog.Icon("aspect_fragment"), 0.55f, 0.36f);
            Pin("realms", "Shattered Realms", Realms.Open(state, content), Loc.T("realm.locked", content.Realms.PortalLevel),
                () => Ui.Router.Push(new RealmScreen()), Art("fragment_devourer"), 0.5f, 0.12f);
            Pin("raids", "Guardian Raids", true, null, () => Ui.Router.Push(new GuildScreen(GuildScreen.Page.Raid)), Art("celestial_turtle"), 0.78f, 0.78f);
        }

        private Texture2D Art(string monsterId)
        {
            var assets = Ui.Catalog.Hero(monsterId);
            return assets?.portrait != null ? assets.portrait : assets?.full;
        }

        private void Pin(string id, string name, bool open, string lockedText, System.Action go, Texture2D image, float x, float y)
        {
            var spot = Ui.Catalog.Layout().Pin(id) ?? new Spot { id = id, x = x, y = y, size = 0.16f };
            var pin = UiKit.Box("map-pin");
            pin.EnableInClassList("map-pin--locked", !open);
            var frame = UiKit.Box("map-pin__frame").Into(pin);
            if (image != null)
                UiKit.Box("map-pin__art").Into(frame).style.backgroundImage = new StyleBackground(image);
            var plate = UiKit.Box("map-pin__plate").Into(UiKit.Box("map-pin__under").Into(pin));
            UiKit.Text(name, "map-pin__name").Into(plate);
            if (!open)
                UiKit.Text(lockedText, "map-pin__hint").Into(plate);
            pin.RegisterCallback<ClickEvent>(_ =>
            {
                if (_map.Dragged)
                    return;
                if (open && go != null)
                    go();
                else
                    Ui.Toast(lockedText);
            });
            _map.Place(pin, spot, 1f, false);
        }
    }
}
