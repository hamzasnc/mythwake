using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>What a formation is picked for when it is not the campaign (e.g. a Trial of Fragments tower).</summary>
    public interface IFormationMode
    {
        string Title { get; }
        /// <summary>Rule shown under the title ("only heroes of Aspect …").</summary>
        string Hint { get; }
        bool AllowLeader { get; }
        bool Allows(HeroDefinition hero);
        string[] Initial(UiContext ui);
        string[] Auto(UiContext ui);
        void Fight(UiContext ui, string[] slots, string leader);
    }

    /// <summary>Pick up to 5 heroes (2 front, 3 back) and a Hero Leader, then start the stage (or the mode's fight).</summary>
    public sealed class FormationScreen : UiScreen
    {
        private readonly IFormationMode _mode;

        public FormationScreen() { }

        public FormationScreen(IFormationMode mode) => _mode = mode;

        /// <summary>The campaign formation (no special mode).</summary>
        public bool IsCampaign => _mode == null;

        private readonly string[] _slots = new string[5];
        private string _leader;
        private VisualElement[] _slotViews;
        private VisualElement _leaderSlot;
        private VisualElement _list;
        private VisualElement _synergies;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.Btn("‹", () => Ui.Router.Pop(), "btn--secondary", "btn--small").Into(head);
            UiKit.Text(_mode?.Title ?? Loc.T("formation.title"), "title").Into(head);
            if (_mode != null)
                UiKit.Text(_mode.Hint, "label--small").Into(safe);

            var formation = UiKit.Box("formation").Into(safe);
            // enemies stand to the right, so the back row is the left column and the front row the right column
            var back = UiKit.Box("formation__column").Into(formation);
            UiKit.Text(Loc.T("formation.back"), "label--small").Into(back);
            var front = UiKit.Box("formation__column").Into(formation);
            UiKit.Text(Loc.T("formation.front"), "label--small").Into(front);
            _slotViews = new VisualElement[5];
            for (var i = 0; i < 5; i++)
            {
                var slot = i;
                var view = UiKit.Box("slot").Into(i < 2 ? front : back);
                view.RegisterCallback<ClickEvent>(_ => RemoveAt(slot));
                _slotViews[i] = view;
            }

            var leaderRow = UiKit.Box("row", "center").Into(safe);
            leaderRow.style.display = _mode == null || _mode.AllowLeader ? DisplayStyle.Flex : DisplayStyle.None;
            UiKit.Text(Loc.T("formation.leader") + "  ", "label--small").Into(leaderRow);
            _leaderSlot = UiKit.Box("slot").Into(leaderRow);
            _leaderSlot.style.width = 130;
            _leaderSlot.style.height = 170;
            _leaderSlot.RegisterCallback<ClickEvent>(_ =>
            {
                _leader = null;
                Redraw();
            });

            _synergies = UiKit.Box("synergy").Into(safe);

            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _list = UiKit.Box("hero-grid").Into(scroll);

            var buttons = UiKit.Box("row", "center").Into(safe);
            UiKit.Btn(Loc.T("formation.auto"), AutoFill, "btn--secondary").Into(buttons);
            UiKit.Btn(Loc.T("formation.fight"), Fight, "btn--big").Into(buttons).name = "coach-formation-fight";
        }

        public override void OnShow()
        {
            System.Array.Copy(_mode != null ? _mode.Initial(Ui) : Ui.State.Formation, _slots, 5);
            _leader = _mode == null || _mode.AllowLeader ? Ui.State.Leader : null;
            base.OnShow();
        }

        public override void Refresh() => Redraw();

        private int Unlocked => Formations.UnlockedSlots(Ui.State, Ui.Content);

        private void Redraw()
        {
            var content = Ui.Content;
            var state = Ui.State;
            var unlocked = Unlocked;
            var used = 0;
            for (var i = 0; i < 5; i++)
            {
                var view = _slotViews[i];
                view.Clear();
                view.EnableInClassList("slot--locked", false);
                var id = _slots[i];
                if (!string.IsNullOrEmpty(id))
                {
                    used++;
                    view.Add(UiKit.HeroCard(Ui.Catalog, content.Hero(id), state.FindHero(id)));
                }
            }
            // locked slots: show how many are still closed and from which stage they open
            var lockedCount = 5 - System.Math.Max(unlocked, used);
            var unlocks = content.Campaign.SlotUnlocks.Where(u => u.Slots > unlocked).OrderBy(u => u.Stage).ToList();
            for (int i = 4, shown = 0; i >= 0 && shown < lockedCount; i--)
            {
                if (!string.IsNullOrEmpty(_slots[i]))
                    continue;
                _slotViews[i].EnableInClassList("slot--locked", true);
                var unlock = unlocks.Count > 0 ? unlocks[System.Math.Min(lockedCount - 1 - shown, unlocks.Count - 1)] : null;
                if (unlock != null)
                    _slotViews[i].Add(UiKit.Text(Loc.T("formation.locked", unlock.Stage), "label--small"));
                shown++;
            }

            _leaderSlot.Clear();
            if (!string.IsNullOrEmpty(_leader))
                _leaderSlot.Add(UiKit.HeroCard(Ui.Catalog, content.Hero(_leader), state.FindHero(_leader)));

            _list.Clear();
            foreach (var owned in state.Heroes.OrderByDescending(h => UiKit.Power(state, content, h)))
            {
                var hero = content.Hero(owned.HeroId);
                if (_mode != null && !_mode.Allows(hero))
                    continue;
                var card = UiKit.HeroCard(Ui.Catalog, hero, owned);
                var inTeam = _slots.Contains(owned.HeroId) || _leader == owned.HeroId;
                card.EnableInClassList("hero-card--selected", inTeam);
                card.RegisterCallback<ClickEvent>(_ => Toggle(owned.HeroId));
                _list.Add(card);
            }

            _synergies.Clear();
            var factions = _slots.Where(s => !string.IsNullOrEmpty(s)).GroupBy(s => content.Hero(s).Faction);
            foreach (var g in factions.OrderByDescending(g => g.Count()))
            {
                var bonus = Core.Progression.TeamBuilder.FactionBonus(g.Count());
                var chip = UiKit.Text($"{Loc.T("faction." + UiKit.Lower(g.Key))} {g.Count()}" + (bonus > 0 ? $"  +{bonus / 10}%" : ""), "synergy__chip").Into(_synergies);
                chip.EnableInClassList("synergy__chip--active", bonus > 0);
            }
        }

        private void Toggle(string heroId)
        {
            var hero = Ui.Content.Hero(heroId);
            if (_leader == heroId)
            {
                _leader = null;
            }
            else if (_slots.Contains(heroId))
            {
                RemoveAt(System.Array.IndexOf(_slots, heroId));
                return;
            }
            else if (hero.Role == Role.HeroLeader)
            {
                if (_mode != null && !_mode.AllowLeader)
                    return;
                _leader = heroId;
            }
            else
            {
                if (_slots.Count(s => !string.IsNullOrEmpty(s)) >= Unlocked)
                {
                    Ui.Toast(Loc.T("error.slots_locked"));
                    return;
                }
                // front-row heroes prefer the front slots, everyone else the back
                var order = hero.Row == Row.Front ? new[] { 0, 1, 2, 3, 4 } : new[] { 2, 3, 4, 0, 1 };
                var free = order.FirstOrDefault(i => string.IsNullOrEmpty(_slots[i]));
                if (!string.IsNullOrEmpty(_slots[free]))
                    return;
                _slots[free] = heroId;
            }
            Redraw();
        }

        private void RemoveAt(int slot)
        {
            if (slot < 0)
                return;
            _slots[slot] = null;
            Redraw();
        }

        private void AutoFill()
        {
            if (_mode != null)
            {
                System.Array.Copy(_mode.Auto(Ui), _slots, 5);
                Redraw();
                return;
            }
            var copy = Mythwake.Content.SaveJson.Read(Mythwake.Content.SaveJson.Write(Ui.State));
            Formations.AutoFill(copy, Ui.Content);
            System.Array.Copy(copy.Formation, _slots, 5);
            Redraw();
        }

        private void Fight()
        {
            if (_mode != null)
            {
                _mode.Fight(Ui, (string[])_slots.Clone(), _leader);
                return;
            }
            if (!Ui.Try(new SetFormation { Slots = (string[])_slots.Clone(), Leader = _leader }, out _))
                return;
            if (!Ui.Try(new BeginStageBattle(), out var setup))
                return;
            Ui.Router.Push(new BattleScreen(setup, Ui.State.Battle.Stage));
        }
    }
}
