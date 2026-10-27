using System.Linq;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Core.Progression;
using Mythwake.Game;
using Mythwake.Presentation;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Trial of Fragments (docs/systems/TRIALS.md): one tower per Fragment Aspect.</summary>
    public sealed class TrialScreen : UiScreen
    {
        private CurrencyBar _currencies;
        private VisualElement _body;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            _currencies = new CurrencyBar(Ids.Gold, Ids.FragmentDust, Ids.Gems).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.IconBtn("back", () => Ui.Router.Back(() => new AdventureScreen()), "back-btn").Into(root);
            UiKit.Text(Loc.T("trial.title"), "title", "screen-title").Into(head);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _body = UiKit.Box().Into(scroll);
        }

        public override void Refresh()
        {
            _currencies.Refresh(Ui.State);
            _body.Clear();
            if (!Trials.Open(Ui.State, Ui.Content))
            {
                UiKit.Text(Loc.T("error.trial_locked"), "subtitle").Into(UiKit.Box("panel").Into(_body)).style.whiteSpace = WhiteSpace.Normal;
                return;
            }
            foreach (var t in Ui.Content.Trials.Trials)
                _body.Add(Tower(t));
        }

        private VisualElement Tower(TrialDefinition t)
        {
            var content = Ui.Content;
            var state = Ui.State;
            var highest = Trials.Highest(state, t.Aspect);
            var max = content.Trials.MaxFloor;
            var color = FragmentUi.AspectColor(t.Aspect);
            var card = UiKit.Box("panel", "trial");
            card.style.borderLeftColor = color;
            var row = UiKit.Box("row").Into(card);
            var icon = UiKit.Box("fragment__icon--big").Into(row);
            var texture = Ui.Catalog.Icon("aspect_fragment");
            if (texture != null)
            {
                icon.style.backgroundImage = new StyleBackground(texture);
                icon.style.unityBackgroundImageTintColor = color;
            }
            var texts = UiKit.Box("grow").Into(row);
            texts.style.flexBasis = 0;
            UiKit.Text(t.Name, "building__name").Into(texts).style.color = color;
            UiKit.Text(t.Description, "skill__text").Into(texts);
            UiKit.Text(Loc.T("trial.highest", highest, max), "label--small").Into(texts);

            var bar = UiKit.Box("afk-progress").Into(card);
            UiKit.Box("afk-progress__fill").Into(bar).style.width = Length.Percent(100f * highest / max);

            if (highest >= max)
            {
                UiKit.Text(Loc.T("trial.complete"), "label--good").Into(card);
                return card;
            }
            var floor = highest + 1;
            var team = Trials.SuggestedTeam(state, content, t.Aspect);
            var teamPower = Campaign.Allies(state, content, team, withLeader: false).Sum(u => TeamBuilder.Power(u.Stats));
            var enemyPower = Trials.Enemies(content, t.Aspect, floor).Sum(u => TeamBuilder.Power(u.Stats));
            var info = UiKit.Box("row").Into(card);
            UiKit.Text(Loc.T("trial.floor", floor), "quest__title").Into(info);
            UiKit.Box("spacer").Into(info);
            var power = UiKit.Text(Loc.T("hunt.power", UiKit.Short(enemyPower), UiKit.Short(teamPower)), "label--small").Into(info);
            power.EnableInClassList("label--bad", enemyPower > teamPower);

            var rewards = UiKit.Box("row", "dungeon__rewards").Into(card);
            rewards.style.flexWrap = Wrap.Wrap;
            foreach (var r in Trials.FloorRewards(content, floor))
                rewards.Add(UiKit.Amount(r.Id, r.Value));
            var every = content.Trials.MilestoneEvery;
            if (every > 0)
            {
                var next = (floor + every - 1) / every * every;
                UiKit.Text(Loc.T("trial.milestone", next, Loc.T("grade." + UiKit.Lower(Trials.MilestoneGrade(content, next)))), "label--small").Into(card);
            }

            var owned = state.Heroes.Count(h => content.Hero(h.HeroId).Aspect == t.Aspect && content.Hero(h.HeroId).Role != Role.HeroLeader);
            var buttons = UiKit.Box("row", "center").Into(card);
            UiKit.Text(Loc.T("trial.heroes", owned), "label--small").Into(buttons);
            UiKit.Box("spacer").Into(buttons);
            var enter = UiKit.Btn(Loc.T("trial.enter"), () => Ui.Router.Push(new FormationScreen(new TrialFormation(t))), "btn--small").Into(buttons);
            enter.SetEnabled(owned > 0);
            return card;
        }
    }

    /// <summary>Formation picker for a tower: only heroes of its Aspect, no Hero Leader.</summary>
    public sealed class TrialFormation : IFormationMode
    {
        private readonly TrialDefinition _trial;

        public TrialFormation(TrialDefinition trial) => _trial = trial;

        public string Title => Loc.T("trial.team", _trial.Name);
        public string Hint => Loc.T("trial.onlyAspect", _trial.Aspect);
        public bool AllowLeader => false;

        public bool Allows(HeroDefinition hero) => hero.Aspect == _trial.Aspect && hero.Role != Role.HeroLeader;

        public string[] Initial(UiContext ui) => Trials.SuggestedTeam(ui.State, ui.Content, _trial.Aspect);

        public string[] Auto(UiContext ui) => Trials.SuggestedTeam(ui.State, ui.Content, _trial.Aspect, useSaved: false);

        public void Fight(UiContext ui, string[] slots, string leader)
        {
            if (!ui.Try(new BeginTrial { Aspect = _trial.Aspect, Slots = slots }, out var setup))
                return;
            ui.Router.Push(new BattleScreen(setup, new TrialBattle(_trial.Name, ui.State.Battle.Floor)));
        }
    }

    public sealed class TrialBattle : IBattleMode
    {
        private readonly string _name;
        private readonly int _floor;

        public TrialBattle(string name, int floor)
        {
            _name = name;
            _floor = floor;
        }

        public bool Replay => false;

        public string Background(UiContext ui) => "trial";

        public string Title(UiContext ui) => $"{_name} · {Loc.T("trial.floor", _floor)}";

        public BattleReport Finish(UiContext ui, BattleView battle)
        {
            var finish = new FinishTrial { AutoUltimates = battle.AutoAtStart };
            finish.AutoToggleTimesMs.AddRange(battle.AutoToggles);
            finish.UltimateRequests.AddRange(battle.Requests);
            if (!ui.Try(finish, out var result))
                return null;
            return new BattleReport { Outcome = result.Outcome, Rewards = result.Rewards, Fragments = result.Fragments };
        }

        public UiScreen After() => new TrialScreen();
    }
}
