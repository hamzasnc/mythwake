using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Core.Progression;
using Mythwake.Game;
using Mythwake.Presentation;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Portal Chamber dungeons (docs/systems/DUNGEONS.md): boss, special rule, difficulty, rewards, fight or sweep.</summary>
    public sealed class DungeonScreen : UiScreen
    {
        private readonly Dictionary<string, int> _selected = new Dictionary<string, int>();
        private CurrencyBar _currencies;
        private VisualElement _body;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            _currencies = new CurrencyBar(Ids.Gold, Ids.HeroExp, Ids.GearMaterials).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.Btn("‹", () => Ui.Router.Back(() => new AdventureScreen()), "btn--secondary", "btn--small").Into(head);
            UiKit.Text(Loc.T("dungeon.title"), "title").Into(head);
            UiKit.Box("spacer").Into(head);
            UiKit.Btn(Loc.T("bossfrag.title"), () => Ui.Router.Push(new BossFragmentScreen()), "btn--secondary", "btn--small").Into(head);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _body = UiKit.Box().Into(scroll);
        }

        public override void Refresh()
        {
            _currencies.Refresh(Ui.State);
            _body.Clear();
            if (Dungeons.PortalLevel(Ui.State, Ui.Content) <= 0)
            {
                UiKit.Text(Loc.T("dungeon.locked"), "subtitle").Into(UiKit.Box("panel").Into(_body)).style.whiteSpace = WhiteSpace.Normal;
                return;
            }
            var team = Campaign.Allies(Ui.State, Ui.Content, Ui.State.Formation).Sum(u => TeamBuilder.Power(u.Stats));
            foreach (var d in Ui.Content.Dungeons.Dungeons)
                _body.Add(Card(d, team));
        }

        private int Selected(DungeonDefinition d)
        {
            var highest = Dungeons.Highest(Ui.State, d.Id);
            if (!_selected.TryGetValue(d.Id, out var level))
                level = System.Math.Min(d.MaxLevel, highest + 1);
            return System.Math.Max(1, System.Math.Min(level, System.Math.Min(d.MaxLevel, highest + 1)));
        }

        private VisualElement Card(DungeonDefinition d, long teamPower)
        {
            var open = Dungeons.Open(Ui.State, Ui.Content, d);
            var card = UiKit.Box("panel", "dungeon");
            card.EnableInClassList("building--planned", !open);
            var boss = Ui.Content.Hero(d.Boss);
            var art = UiKit.Box("dungeon__art").Into(card);
            var assets = Ui.Catalog.Hero(d.Boss);
            var image = assets?.full != null ? assets.full : assets?.portrait;
            if (image != null)
                art.style.backgroundImage = new StyleBackground(image);
            UiKit.Text(d.Name, "dungeon__name").Into(card);
            UiKit.Text(boss.Name, "subtitle").Into(card);
            UiKit.Text(d.Description, "skill__text").Into(card);
            if (!open)
            {
                UiKit.Text(Loc.T("dungeon.portalLevel", d.PortalLevel), "label--small").Into(card);
                return card;
            }

            var level = Selected(d);
            var highest = Dungeons.Highest(Ui.State, d.Id);
            var selector = UiKit.Box("row", "center", "dungeon__selector").Into(card);
            var prev = UiKit.Btn("◀", () => Select(d, level - 1), "btn--secondary", "btn--small").Into(selector);
            prev.SetEnabled(level > 1);
            UiKit.Text(Loc.T("dungeon.level", level), "dungeon__level").Into(selector);
            var next = UiKit.Btn("▶", () => Select(d, level + 1), "btn--secondary", "btn--small").Into(selector);
            next.SetEnabled(level < System.Math.Min(d.MaxLevel, highest + 1));

            var enemy = Dungeons.Enemies(Ui.Content, d, level).Sum(u => TeamBuilder.Power(u.Stats));
            var power = UiKit.Text(Loc.T("hunt.power", UiKit.Short(enemy), UiKit.Short(teamPower)), "label--small").Into(card);
            power.EnableInClassList("label--bad", enemy > teamPower);

            var rewards = UiKit.Box("row", "dungeon__rewards").Into(card);
            var clear = UiKit.Box("grow").Into(rewards);
            UiKit.Text(Loc.T("dungeon.clear"), "label--small").Into(clear);
            foreach (var r in Dungeons.Rewards(d.ClearRewards, level))
                clear.Add(UiKit.Amount(r.Id, r.Value));
            if (level > highest)
            {
                var first = UiKit.Box("grow").Into(rewards);
                UiKit.Text(Loc.T("dungeon.first"), "label--small").Into(first);
                foreach (var r in Dungeons.Rewards(d.FirstClearRewards, level))
                    first.Add(UiKit.Amount(r.Id, r.Value));
            }

            var attempts = Dungeons.AttemptsLeft(Ui.State, Ui.Content, d, Ui.Authority.NowMs);
            var row = UiKit.Box("row", "center").Into(card);
            UiKit.Text(Loc.T("dungeon.attempts", attempts, d.AttemptsPerDay), "quest__points").Into(row);
            UiKit.Box("spacer").Into(row);
            var sweep = UiKit.Btn(Loc.T("dungeon.sweep", highest), () => Sweep(d), "btn--secondary", "btn--small").Into(row);
            sweep.SetEnabled(attempts > 0 && highest > 0);
            var fight = UiKit.Btn(Loc.T("home.fight"), () => Fight(d, level), "btn--small").Into(row);
            fight.SetEnabled(attempts > 0);
            return card;
        }

        private void Select(DungeonDefinition d, int level)
        {
            _selected[d.Id] = level;
            Refresh();
        }

        private void Fight(DungeonDefinition d, int level)
        {
            if (Ui.Try(new BeginDungeon { DungeonId = d.Id, Level = level }, out var setup))
                Ui.Router.Push(new BattleScreen(setup, new DungeonBattle(d.Name, level)));
        }

        private void Sweep(DungeonDefinition d)
        {
            if (!Ui.Try(new SweepDungeon { DungeonId = d.Id }, out List<Amount> rewards))
                return;
            Ui.Toast(string.Join("  ·  ", rewards.Select(r => $"+{UiKit.Short(r.Value)} {UiKit.CurrencyName(r.Id)}")));
            Refresh();
        }
    }

    public sealed class DungeonBattle : IBattleMode
    {
        private readonly string _name;
        private readonly int _level;

        public DungeonBattle(string name, int level)
        {
            _name = name;
            _level = level;
        }

        public bool Replay => false;

        public string Background(UiContext ui) => "dungeon";

        public string Title(UiContext ui) => $"{_name} · {Loc.T("dungeon.level", _level)}";

        public BattleReport Finish(UiContext ui, BattleView battle)
        {
            var finish = new FinishDungeon { AutoUltimates = battle.AutoAtStart };
            finish.AutoToggleTimesMs.AddRange(battle.AutoToggles);
            finish.UltimateRequests.AddRange(battle.Requests);
            if (!ui.Try(finish, out var result))
                return null;
            return new BattleReport
            {
                Outcome = result.Outcome, Rewards = result.Rewards, Gear = result.Gear, Fragments = result.Fragments, Relics = result.Relics,
                Note = result.FirstClear ? Loc.T("dungeon.firstDone") : null
            };
        }

        public UiScreen After() => new DungeonScreen();
    }
}
