using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mythwake.Content;
using Mythwake.Core.Combat;
using Mythwake.Core.Meta;
using Mythwake.Game;
using Mythwake.Presentation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Arena (doc: "Arena System"): rank, defense team, three opponents, ranking. Fights run on the server.</summary>
    public sealed class ArenaScreen : UiScreen
    {
        private bool _ranking;
        private bool _busy;
        private CurrencyBar _currencies;
        private VisualElement _tabs;
        private VisualElement _body;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            root.AddToClassList("screen--arena");
            _currencies = new CurrencyBar(Ids.ArenaCoins, Ids.Gold, Ids.Gems).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.IconBtn("back", () => Ui.Router.Back(() => new AdventureScreen())).Into(head);
            UiKit.Text("Arena", "title").Into(head);
            _tabs = UiKit.Box("tabs").Into(safe);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _body = UiKit.Box().Into(scroll);
        }

        public override void OnShow()
        {
            base.OnShow();
            Load();
        }

        public override void Refresh() => _currencies.Refresh(Ui.State);

        private void Load()
        {
            _tabs.Clear();
            Tab(Loc.T("arena.tab.fight"), false);
            Tab(Loc.T("arena.tab.ranking"), true);
            _body.Clear();
            if (Ui.Server == null)
            {
                UiKit.Text(Loc.T("arena.offline"), "subtitle").Into(UiKit.Box("panel").Into(_body)).style.whiteSpace = WhiteSpace.Normal;
                return;
            }
            UiKit.Text(Loc.T("server.loading"), "label--small").Into(_body);
            Ui.Runner.StartCoroutine(_ranking ? LoadRanking() : LoadArena());
        }

        private void Tab(string text, bool ranking)
        {
            var b = UiKit.Btn(text, () =>
            {
                _ranking = ranking;
                Load();
            }, "tab").Into(_tabs);
            b.EnableInClassList("tab--active", _ranking == ranking);
        }

        private IEnumerator LoadArena()
        {
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("GET", "/v1/arena", null, x => r = x);
            _body.Clear();
            if (!r.Ok)
            {
                UiKit.Text(Loc.T("server.error", r.Error), "label--bad").Into(_body);
                yield break;
            }
            var a = r.Body;
            // season rewards were paid on this visit: take the server's state
            if (Ui.Server.Pending == 0 && a.Value<string>("stateHash") != SaveJson.Hash(Ui.State))
                yield return Ui.Server.Resync("arena");

            if (!a.Value<bool>("unlocked"))
            {
                UiKit.Text(Loc.T("arena.locked", a.Value<int>("unlockStage")), "subtitle").Into(UiKit.Box("panel").Into(_body));
                yield break;
            }

            var me = UiKit.Box("panel").Into(_body);
            var row = UiKit.Box("row").Into(me);
            var texts = UiKit.Box("grow").Into(row);
            UiKit.Text(a.Value<string>("rank"), "arena__rank").Into(texts);
            UiKit.Text(Loc.T("arena.rating", a.Value<int>("rating"), a.Value<int>("wins"), a.Value<int>("losses")), "label--small").Into(texts);
            var left = System.TimeSpan.FromMilliseconds(a.Value<long>("seasonEndsMs") - Ui.Authority.NowMs);
            UiKit.Text(Loc.T("arena.season", a.Value<int>("season"), $"{left.Days}d {left.Hours}h"), "label--small").Into(texts);
            UiKit.Text(Loc.T("arena.fights", a.Value<int>("fightsLeft")), "quest__points").Into(row);

            var defense = UiKit.Box("panel").Into(_body);
            UiKit.Text(Loc.T("arena.defense"), "subtitle").Into(defense);
            UiKit.Text(a.Value<bool>("hasDefense") ? Loc.T("arena.defensePower", UiKit.Short(a.Value<long>("defensePower"))) : Loc.T("arena.noDefense"),
                "label--small").Into(defense);
            UiKit.Btn(Loc.T("arena.setDefense"), () => Ui.Runner.StartCoroutine(SetDefense()), "btn--secondary").Into(defense);

            var opponents = a["opponents"] as JArray ?? new JArray();
            if (opponents.Count > 0)
                UiKit.Text(Loc.T("arena.opponents"), "subtitle").Into(_body);
            var fightsLeft = a.Value<int>("fightsLeft");
            foreach (JObject o in opponents)
                _body.Add(Opponent(o, fightsLeft > 0));
        }

        private VisualElement Opponent(JObject o, bool canFight)
        {
            var card = UiKit.Box("panel", "hunt").With(o.Value<bool>("isBot") ? "hunt--daily" : "hunt--elite");
            var head = UiKit.Box("row").Into(card);
            UiKit.Text(o.Value<string>("name"), "quest__title").Into(head);
            UiKit.Box("spacer").Into(head);
            UiKit.Text($"{o.Value<string>("rank")} · {o.Value<int>("rating")}", "hunt__stars").Into(head);
            UiKit.Text(Loc.T("arena.power", UiKit.Short(o.Value<long>("power"))), "label--small").Into(card);
            var heroes = UiKit.Box("row", "arena__team").Into(card);
            foreach (var id in o["heroes"]?.Values<string>() ?? Enumerable.Empty<string>())
            {
                var hero = Ui.Content.Heroes.TryGetValue(id, out var h) ? h : null;
                if (hero == null)
                    continue;
                var mini = UiKit.HeroCard(Ui.Catalog, hero, null).With("hero-card--mini").Into(heroes);
                mini.Q(className: "hero-card__footer")?.RemoveFromHierarchy();
            }
            var opponentId = o.Value<string>("id");
            var b = UiKit.Btn(Loc.T("arena.fight"), () => Ui.Runner.StartCoroutine(Fight(opponentId)), "btn--small").Into(card);
            b.SetEnabled(canFight);
            return card;
        }

        private IEnumerator SetDefense()
        {
            if (_busy)
                yield break;
            _busy = true;
            yield return Ui.Server.Flush();
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("POST", "/v1/arena/defense", new JObject(), x => r = x);
            _busy = false;
            if (!r.Ok)
            {
                Ui.Toast(Loc.T("server.error", r.Error));
                yield break;
            }
            Ui.Toast(Loc.T("arena.defenseSet", UiKit.Short(r.Body.Value<long>("power"))));
            Load();
        }

        private IEnumerator Fight(string opponentId)
        {
            if (_busy)
                yield break;
            _busy = true;
            // the server fights with the confirmed state: send everything still queued first
            yield return Ui.Server.Flush();
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("POST", "/v1/arena/fight", new JObject { ["opponentId"] = opponentId }, x => r = x);
            _busy = false;
            if (!r.Ok)
            {
                Ui.Toast(Loc.T("server.error", r.Error));
                Load();
                yield break;
            }
            // the reward changed the server state
            yield return Ui.Server.Resync("arena");
            var attacker = JsonConvert.DeserializeObject<TeamSnapshot>(r.Body["attacker"].ToString(Formatting.None), SaveJson.Settings);
            var defender = JsonConvert.DeserializeObject<TeamSnapshot>(r.Body["defender"].ToString(Formatting.None), SaveJson.Settings);
            var seed = ulong.Parse(r.Body.Value<string>("seed"));
            var setup = Arena.Setup(Ui.Content, attacker, defender, seed);
            Ui.Router.Push(new BattleScreen(setup, new ArenaBattle(r.Body)));
        }

        private IEnumerator LoadRanking()
        {
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("GET", "/v1/arena/ranking", null, x => r = x);
            _body.Clear();
            if (!r.Ok)
            {
                UiKit.Text(Loc.T("server.error", r.Error), "label--bad").Into(_body);
                yield break;
            }
            var place = 0;
            foreach (JObject p in r.Body["players"] as JArray ?? new JArray())
            {
                place++;
                var row = UiKit.Box("panel", "row", "ranking__row").Into(_body);
                UiKit.Text(place + ".", "ranking__place").Into(row);
                var texts = UiKit.Box("grow").Into(row);
                UiKit.Text(p.Value<string>("name"), "quest__title").Into(texts);
                UiKit.Text($"{p.Value<string>("rank")} · {Loc.T("arena.power", UiKit.Short(p.Value<long>("power")))}", "label--small").Into(texts);
                UiKit.Text(p.Value<int>("rating").ToString(), "quest__points").Into(row);
            }
        }
    }

    /// <summary>Replays an arena fight the server already decided.</summary>
    public sealed class ArenaBattle : IBattleMode
    {
        private readonly JObject _fight;

        public ArenaBattle(JObject fight) => _fight = fight;

        public bool Replay => true;

        public string Background(UiContext ui) => "arena";

        public string Title(UiContext ui) => "Arena · " + _fight.Value<string>("defenderName");

        public BattleReport Finish(UiContext ui, BattleView battle)
        {
            var change = _fight.Value<int>("ratingChange");
            var report = new BattleReport
            {
                Outcome = _fight.Value<bool>("victory") ? BattleOutcome.Victory : BattleOutcome.Defeat,
                Note = Loc.T("arena.result", (change >= 0 ? "+" : "") + change, _fight.Value<int>("rating"), _fight.Value<string>("rank"))
            };
            foreach (JObject a in _fight["rewards"] as JArray ?? new JArray())
                report.Rewards.Add(new Amount(a.Value<string>("id"), a.Value<long>("value")));
            return report;
        }

        public UiScreen After() => new ArenaScreen();
    }
}
