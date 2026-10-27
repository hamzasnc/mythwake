using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Core.Progression;
using Mythwake.Game;
using Mythwake.Presentation;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Shattered Realms (docs/systems/SHATTERED_REALMS.md): four realms with their own rules, shared daily entries and keys.</summary>
    public sealed class RealmScreen : UiScreen
    {
        private readonly Dictionary<string, int> _selected = new Dictionary<string, int>();
        private CurrencyBar _currencies;
        private Label _entries;
        private VisualElement _body;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            _currencies = new CurrencyBar(Ids.RealmKey, Ids.GuardianSigil, Ids.FragmentDust, Ids.RelicMaterials).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.IconBtn("back", () => Ui.Router.Back(() => new AdventureScreen()), "back-btn").Into(root);
            UiKit.Text(Loc.T("realm.title"), "title", "screen-title").Into(head);
            _entries = UiKit.Text("", "quest__points").Into(safe);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _body = UiKit.Box().Into(scroll);
        }

        public override void Refresh()
        {
            _currencies.Refresh(Ui.State);
            _body.Clear();
            var content = Ui.Content;
            if (!Realms.Open(Ui.State, content))
            {
                _entries.text = "";
                UiKit.Text(Loc.T("error.realm_locked"), "subtitle").Into(UiKit.Box("panel").Into(_body)).style.whiteSpace = WhiteSpace.Normal;
                return;
            }
            var now = Ui.Authority.NowMs;
            _entries.text = Loc.T("realm.entries", Realms.FreeLeft(Ui.State, content, now), content.Realms.FreeEntriesPerDay, Ui.State.Get(Ids.RealmKey));
            var team = Campaign.Allies(Ui.State, content, Ui.State.Formation).Sum(u => TeamBuilder.Power(u.Stats));
            foreach (var r in content.Realms.Realms)
                _body.Add(Card(r, team, Realms.EntriesLeft(Ui.State, content, now) > 0));
        }

        private int Selected(DungeonDefinition r)
        {
            var highest = Realms.Highest(Ui.State, r.Id);
            var cap = System.Math.Min(r.MaxLevel, highest + 1);
            if (!_selected.TryGetValue(r.Id, out var level))
                level = cap;
            return System.Math.Max(1, System.Math.Min(level, cap));
        }

        private VisualElement Card(DungeonDefinition r, long teamPower, bool canEnter)
        {
            var card = UiKit.Box("panel", "dungeon", "realm");
            var art = UiKit.Box("dungeon__art").Into(card);
            var assets = Ui.Catalog.Hero(r.Boss);
            var image = assets?.full != null ? assets.full : assets?.portrait;
            if (image != null)
                art.style.backgroundImage = new StyleBackground(image);
            UiKit.Text(r.Name, "dungeon__name").Into(card);
            UiKit.Text(Ui.Content.Hero(r.Boss).Name, "subtitle").Into(card);
            UiKit.Text(r.Description, "skill__text").Into(card);

            var level = Selected(r);
            var highest = Realms.Highest(Ui.State, r.Id);
            var selector = UiKit.Box("row", "center", "dungeon__selector").Into(card);
            UiKit.Btn("◀", () => Select(r, level - 1), "btn--secondary", "btn--small").Into(selector).SetEnabled(level > 1);
            UiKit.Text(Loc.T("realm.level", level), "dungeon__level").Into(selector);
            UiKit.Btn("▶", () => Select(r, level + 1), "btn--secondary", "btn--small").Into(selector)
                .SetEnabled(level < System.Math.Min(r.MaxLevel, highest + 1));

            var enemy = Dungeons.Enemies(Ui.Content, r, level).Sum(u => TeamBuilder.Power(u.Stats));
            var power = UiKit.Text(Loc.T("hunt.power", UiKit.Short(enemy), UiKit.Short(teamPower)), "label--small").Into(card);
            power.EnableInClassList("label--bad", enemy > teamPower);

            var rewards = UiKit.Box("row", "dungeon__rewards").Into(card);
            var clear = UiKit.Box("grow").Into(rewards);
            UiKit.Text(Loc.T("dungeon.clear"), "label--small").Into(clear);
            foreach (var a in Dungeons.Rewards(r.ClearRewards, level))
                clear.Add(UiKit.Amount(a.Id, a.Value));
            if (level > highest)
            {
                var first = UiKit.Box("grow").Into(rewards);
                UiKit.Text(Loc.T("dungeon.first"), "label--small").Into(first);
                foreach (var a in Dungeons.Rewards(r.FirstClearRewards, level))
                    first.Add(UiKit.Amount(a.Id, a.Value));
            }

            var row = UiKit.Box("row", "center").Into(card);
            UiKit.Box("spacer").Into(row);
            UiKit.Btn(Loc.T("realm.sweep", highest), () => Sweep(r), "btn--secondary", "btn--small").Into(row).SetEnabled(canEnter && highest > 0);
            UiKit.Btn(Loc.T("home.fight"), () => Fight(r, level), "btn--small").Into(row).SetEnabled(canEnter);
            return card;
        }

        private void Select(DungeonDefinition r, int level)
        {
            _selected[r.Id] = level;
            Refresh();
        }

        private void Fight(DungeonDefinition r, int level)
        {
            if (Ui.Try(new BeginRealm { RealmId = r.Id, Level = level }, out var setup))
                Ui.Router.Push(new BattleScreen(setup, new RealmBattle(r.Id, r.Name, level)));
        }

        private void Sweep(DungeonDefinition r)
        {
            if (!Ui.Try(new SweepRealm { RealmId = r.Id }, out List<Amount> rewards))
                return;
            Ui.Toast(string.Join("  ·  ", rewards.Select(a => $"+{UiKit.Short(a.Value)} {UiKit.CurrencyName(a.Id)}")));
            Refresh();
        }
    }

    public sealed class RealmBattle : IBattleMode
    {
        private readonly string _id;
        private readonly string _name;
        private readonly int _level;

        public RealmBattle(string id, string name, int level)
        {
            _id = id;
            _name = name;
            _level = level;
        }

        public bool Replay => false;

        public string Background(UiContext ui) => "realm_of_" + _id;

        public string Title(UiContext ui) => $"{_name} · {Loc.T("realm.level", _level)}";

        public BattleReport Finish(UiContext ui, BattleView battle)
        {
            var finish = new FinishRealm { AutoUltimates = battle.AutoAtStart };
            finish.AutoToggleTimesMs.AddRange(battle.AutoToggles);
            finish.UltimateRequests.AddRange(battle.Requests);
            if (!ui.Try(finish, out var result))
                return null;
            var note = result.FirstClear ? Loc.T("realm.firstDone") : null;
            if (result.UsedKey)
                note = (note == null ? "" : note + "  ") + Loc.T("realm.keyUsed");
            return new BattleReport { Outcome = result.Outcome, Rewards = result.Rewards, Note = note };
        }

        public UiScreen After() => new RealmScreen();
    }
}
