using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Game;
using Mythwake.Presentation;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Watchtower village defense (docs/systems/WATCHTOWER.md): waves with HP carried over.</summary>
    public sealed class DefenseScreen : UiScreen
    {
        private CurrencyBar _currencies;
        private VisualElement _body;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            _currencies = new CurrencyBar(Ids.Gold, Ids.Wood, Ids.Stone, Ids.Gems).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.Btn("‹", () => Ui.Router.Back(() => new VillageScreen()), "btn--secondary", "btn--small").Into(head);
            UiKit.Text(Loc.T("defense.title"), "title").Into(head);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _body = UiKit.Box().Into(scroll);
        }

        public override void Refresh()
        {
            var state = Ui.State;
            var content = Ui.Content;
            _currencies.Refresh(state);
            _body.Clear();
            if (Defense.TowerLevel(state, content) <= 0)
            {
                UiKit.Text(Loc.T("error.defense_locked"), "subtitle").Into(UiKit.Box("panel").Into(_body));
                return;
            }
            var info = UiKit.Box("panel").Into(_body);
            UiKit.Text(Loc.T("defense.best", state.Defense.BestWave, content.Defense.MaxWaves), "quest__title").Into(info);
            UiKit.Text(Loc.T("defense.runs", Defense.RunsLeft(state, content, Ui.Authority.NowMs), content.Defense.RunsPerDay), "label--small").Into(info);
            UiKit.Text(Loc.T("defense.hint"), "skill__text").Into(info);

            var run = state.Defense.Run;
            if (run == null)
            {
                var start = UiKit.Btn(Loc.T("defense.start"), () =>
                {
                    if (Ui.Try(new StartDefense(), out bool _))
                        Refresh();
                }, "btn--big").Into(info);
                start.SetEnabled(Defense.RunsLeft(state, content, Ui.Authority.NowMs) > 0);
                return;
            }

            var wave = UiKit.Box("panel").Into(_body);
            var title = UiKit.Box("row").Into(wave);
            UiKit.Text(Loc.T("defense.wave", run.Wave), "title").Into(title);
            UiKit.Box("spacer").Into(title);
            if (Defense.IsBossWave(content, run.Wave))
                UiKit.Text(Loc.T("defense.boss"), "boss-tag").Into(title);

            var team = UiKit.Box("hero-grid").Into(wave);
            var allies = Campaign.Allies(state, content, run.Formation);
            for (var i = 0; i < 5; i++)
            {
                var id = run.Formation[i];
                if (string.IsNullOrEmpty(id))
                    continue;
                var card = UiKit.HeroCard(Ui.Catalog, content.Hero(id), state.FindHero(id)).Into(team);
                var max = allies.FirstOrDefault(u => u.Slot == i)?.Stats.Hp ?? 1;
                var hp = run.Hp[i] < 0 ? max : run.Hp[i];
                var bar = UiKit.Box("afk-progress", "defense-hp").Into(card);
                UiKit.Box("afk-progress__fill").Into(bar).style.width = Length.Percent(100f * hp / System.Math.Max(1, max));
                if (run.Hp[i] == 0)
                {
                    card.AddToClassList("defense--fallen");
                    UiKit.Text(Loc.T("defense.fallen"), "new-badge").Into(card);
                }
            }

            var rewards = UiKit.Box("row", "dungeon__rewards").Into(wave);
            rewards.style.flexWrap = Wrap.Wrap;
            foreach (var r in Defense.WaveRewards(state, content, run.Wave))
                rewards.Add(UiKit.Amount(r.Id, r.Value));

            var buttons = UiKit.Box("row", "center").Into(wave);
            UiKit.Btn(Loc.T("defense.retreat"), () =>
            {
                if (Ui.Try(new EndDefense(), out bool _))
                    Refresh();
            }, "btn--secondary").Into(buttons);
            UiKit.Btn(Loc.T("defense.next", run.Wave), Fight).Into(buttons);
        }

        private void Fight()
        {
            if (Ui.Try(new BeginDefenseWave(), out var setup))
                Ui.Router.Push(new BattleScreen(setup, new DefenseBattle(Ui.State.Battle.Floor)));
        }
    }

    public sealed class DefenseBattle : IBattleMode
    {
        private readonly int _wave;

        public DefenseBattle(int wave) => _wave = wave;

        public bool Replay => false;

        public string Background(UiContext ui) => "watchtower";

        public string Title(UiContext ui) => $"Dawnhollow · {Loc.T("defense.wave", _wave)}";

        public BattleReport Finish(UiContext ui, BattleView battle)
        {
            var finish = new FinishDefenseWave { AutoUltimates = battle.AutoAtStart };
            finish.AutoToggleTimesMs.AddRange(battle.AutoToggles);
            finish.UltimateRequests.AddRange(battle.Requests);
            if (!ui.Try(finish, out var result))
                return null;
            var note = result.RunOver ? Loc.T("defense.over", ui.State.Defense.BestWave) : null;
            return new BattleReport { Outcome = result.Outcome, Rewards = result.Rewards, Note = note };
        }

        public UiScreen After() => new DefenseScreen();
    }
}
