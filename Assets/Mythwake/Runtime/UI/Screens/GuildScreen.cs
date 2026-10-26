using System.Collections;
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
    /// <summary>
    /// Guild (docs/systems/GUILDS.md): find or found a guild; members, chat, donations, Guardian Raid and Guild Shop.
    /// Everything lives on the server; this screen only shows it and sends requests.
    /// </summary>
    public sealed class GuildScreen : UiScreen
    {
        public enum Page { Members, Chat, Raid, Donate }

        private Page _page;
        private bool _busy;
        private JObject _guild;
        private CurrencyBar _currencies;
        private VisualElement _body;
        private IVisualElementScheduledItem _chatPoll;
        private long _lastMessage;
        private VisualElement _chatList;

        public GuildScreen(Page page = Page.Members) => _page = page;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            root.AddToClassList("screen--guild");
            _currencies = new CurrencyBar(Ids.GuildCoins, Ids.Gold, Ids.Gems).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.IconBtn("back", () => Ui.Router.Back(() => new VillageScreen())).Into(head);
            UiKit.Text(Loc.T("guild.title"), "title").Into(head);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _body = UiKit.Box().Into(scroll);
        }

        public override void OnShow()
        {
            base.OnShow();
            Load();
        }

        public override void OnHide() => _chatPoll?.Pause();

        public override void Refresh() => _currencies.Refresh(Ui.State);

        private void Load()
        {
            _body.Clear();
            if (Ui.Server == null)
            {
                UiKit.Text(Loc.T("guild.offline"), "subtitle").Into(UiKit.Box("panel").Into(_body)).style.whiteSpace = WhiteSpace.Normal;
                return;
            }
            UiKit.Text(Loc.T("server.loading"), "label--small").Into(_body);
            Ui.Runner.StartCoroutine(LoadGuild());
        }

        private IEnumerator LoadGuild()
        {
            yield return Ui.Server.Flush();
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("GET", "/v1/guild", null, x => r = x);
            if (!r.Ok)
            {
                _body.Clear();
                UiKit.Text(Loc.T("server.error", r.Error), "label--bad").Into(_body);
                yield break;
            }
            _guild = r.Body;
            if (r.Body["paid"] is JArray paid && paid.Count > 0)
            {
                yield return Ui.Server.Resync("guild");
                Ui.Toast(Loc.T("guild.killPaid"));
            }
            if (_guild.Value<bool>("inGuild"))
                ShowGuild();
            else
                yield return ShowSearch("");
        }

        // ------------------------------------------------------------------ no guild: search and found

        private IEnumerator ShowSearch(string query)
        {
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("GET", "/v1/guilds?q=" + System.Uri.EscapeDataString(query), null, x => r = x);
            _body.Clear();
            var found = UiKit.Box("panel").Into(_body);
            UiKit.Text(Loc.T("guild.found"), "subtitle").Into(found);
            var nameField = new TextField { maxLength = 20 };
            nameField.AddToClassList("guild-input");
            found.Add(nameField);
            var row = UiKit.Box("row").Into(found);
            var cost = _guild["createCost"]?.ToObject<Amount[]>(JsonSerializer.Create(SaveJson.Settings)) ?? new Amount[0];
            row.Add(GearUi.CostRow(Ui.State, cost));
            UiKit.Box("spacer").Into(row);
            UiKit.Btn(Loc.T("guild.create"), () => Ui.Runner.StartCoroutine(Create(nameField.value)), "btn--small").Into(row);

            var search = UiKit.Box("panel").Into(_body);
            UiKit.Text(Loc.T("guild.search"), "subtitle").Into(search);
            var queryField = new TextField { value = query, maxLength = 20 };
            queryField.AddToClassList("guild-input");
            search.Add(queryField);
            UiKit.Btn(Loc.T("guild.searchButton"), () => Ui.Runner.StartCoroutine(ShowSearch(queryField.value)), "btn--secondary", "btn--small").Into(search);
            if (!r.Ok)
            {
                UiKit.Text(Loc.T("server.error", r.Error), "label--bad").Into(_body);
                yield break;
            }
            var list = r.Body["guilds"] as JArray ?? new JArray();
            if (list.Count == 0)
                UiKit.Text(Loc.T("guild.none"), "label--small").Into(_body);
            foreach (JObject g in list)
            {
                var card = UiKit.Box("panel", "row", "guild-row").Into(_body);
                var texts = UiKit.Box("grow").Into(card);
                UiKit.Text(g.Value<string>("name"), "quest__title").Into(texts);
                UiKit.Text(Loc.T("guild.summary", g.Value<int>("level"), g.Value<int>("members"), g.Value<int>("maxMembers"), g.Value<int>("raidLevel")),
                    "label--small").Into(texts);
                var id = g.Value<string>("id");
                var join = UiKit.Btn(Loc.T("guild.join"), () => Ui.Runner.StartCoroutine(Join(id)), "btn--small").Into(card);
                join.SetEnabled(g.Value<int>("members") < g.Value<int>("maxMembers"));
            }
        }

        private IEnumerator Create(string name)
        {
            if (_busy)
                yield break;
            _busy = true;
            yield return Ui.Server.Flush();
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("POST", "/v1/guilds", new JObject { ["name"] = name }, x => r = x);
            _busy = false;
            if (!r.Ok)
            {
                Ui.Toast(Loc.Has("error." + r.Error) ? Loc.T("error." + r.Error) : Loc.T("server.error", r.Error));
                yield break;
            }
            yield return Ui.Server.Resync("guild");
            Load();
        }

        private IEnumerator Join(string id)
        {
            if (_busy)
                yield break;
            _busy = true;
            yield return Ui.Server.Flush();
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("POST", $"/v1/guilds/{id}/join", new JObject(), x => r = x);
            _busy = false;
            if (!r.Ok)
            {
                Ui.Toast(Loc.Has("error." + r.Error) ? Loc.T("error." + r.Error) : Loc.T("server.error", r.Error));
                yield break;
            }
            yield return Ui.Server.Resync("guild");
            Load();
        }

        // ------------------------------------------------------------------ in a guild

        private void ShowGuild()
        {
            _chatPoll?.Pause();
            _body.Clear();
            var g = (JObject)_guild["guild"];
            var head = UiKit.Box("panel").Into(_body);
            var top = UiKit.Box("row").Into(head);
            UiKit.Text(g.Value<string>("name"), "title").Into(top);
            UiKit.Box("spacer").Into(top);
            UiKit.Btn(Loc.T("guild.shop"), () =>
            {
                Ui.Router.Push(new ShopScreen());
                ((ShopScreen)Ui.Router.Current).SelectForAutoPilot("guild");
            }, "btn--secondary", "btn--small").Into(top);
            UiKit.Text(Loc.T("guild.summary", g.Value<int>("level"), g.Value<int>("members"), g.Value<int>("maxMembers"), g.Value<int>("raidLevel")),
                "label--small").Into(head);
            var next = _guild.Value<long>("nextLevelExp");
            if (next > 0)
            {
                var bar = UiKit.Box("afk-progress").Into(head);
                UiKit.Box("afk-progress__fill").Into(bar).style.width = Length.Percent(100f * _guild.Value<long>("exp") / next);
            }

            var tabs = UiKit.Box("tabs").Into(_body);
            foreach (Page p in System.Enum.GetValues(typeof(Page)))
            {
                var page = p;
                var tab = UiKit.Btn(Loc.T("guild.tab." + UiKit.Lower(p)), () =>
                {
                    _page = page;
                    ShowGuild();
                }, "tab").Into(tabs);
                tab.RemoveFromClassList("btn");
                tab.EnableInClassList("tab--active", p == _page);
            }
            var page2 = UiKit.Box().Into(_body);
            switch (_page)
            {
                case Page.Members:
                    Members(page2);
                    break;
                case Page.Chat:
                    Chat(page2);
                    break;
                case Page.Raid:
                    RaidPage(page2);
                    break;
                default:
                    Donations(page2);
                    break;
            }
        }

        private void Members(VisualElement page)
        {
            var place = 0;
            foreach (JObject m in _guild["members"] as JArray ?? new JArray())
            {
                place++;
                var row = UiKit.Box("panel", "row", "ranking__row").Into(page);
                UiKit.Text(place + ".", "ranking__place").Into(row);
                var texts = UiKit.Box("grow").Into(row);
                UiKit.Text(m.Value<string>("name") + (m.Value<string>("role") == "leader" ? "  ★" : ""), "quest__title").Into(texts);
                UiKit.Text(Loc.T("guild.memberLine", UiKit.Short(m.Value<long>("raidDamage")), m.Value<bool>("donated") ? "✓" : "–"), "label--small").Into(texts);
            }
            UiKit.Btn(Loc.T("guild.leave"), () => Ui.Runner.StartCoroutine(Leave()), "btn--secondary").Into(page);
        }

        private IEnumerator Leave()
        {
            if (_busy)
                yield break;
            _busy = true;
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("POST", "/v1/guild/leave", new JObject(), x => r = x);
            _busy = false;
            if (!r.Ok)
            {
                Ui.Toast(Loc.T("server.error", r.Error));
                yield break;
            }
            yield return Ui.Server.Resync("guild");
            Load();
        }

        private void Chat(VisualElement page)
        {
            _chatList = UiKit.Box("guild-chat").Into(page);
            _lastMessage = 0;
            var row = UiKit.Box("row").Into(page);
            var field = new TextField { maxLength = 200 };
            field.AddToClassList("guild-input");
            field.style.flexGrow = 1;
            row.Add(field);
            UiKit.Btn(Loc.T("guild.send"), () => Ui.Runner.StartCoroutine(Send(field)), "btn--small").Into(row);
            Ui.Runner.StartCoroutine(PollChat());
            _chatPoll = Root.schedule.Execute(() => Ui.Runner.StartCoroutine(PollChat())).Every(5000);
        }

        private IEnumerator PollChat()
        {
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("GET", "/v1/guild/chat?after=" + _lastMessage, null, x => r = x);
            if (r == null || !r.Ok || _chatList == null)
                yield break;
            foreach (JObject m in r.Body["messages"] as JArray ?? new JArray())
            {
                _lastMessage = System.Math.Max(_lastMessage, m.Value<long>("id"));
                var line = UiKit.Box("guild-message").Into(_chatList);
                UiKit.Text(m.Value<string>("name"), "guild-message__name").Into(line);
                UiKit.Text(m.Value<string>("text"), "skill__text").Into(line);
            }
        }

        private IEnumerator Send(TextField field)
        {
            var text = field.value?.Trim();
            if (string.IsNullOrEmpty(text))
                yield break;
            field.value = "";
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("POST", "/v1/guild/chat", new JObject { ["text"] = text }, x => r = x);
            if (!r.Ok)
                Ui.Toast(Loc.T("server.error", r.Error));
            yield return PollChat();
        }

        private void Donations(VisualElement page)
        {
            var donated = _guild.Value<bool>("donated");
            if (donated)
                UiKit.Text(Loc.T("guild.donated"), "label--good").Into(page);
            foreach (JObject d in _guild["donations"] as JArray ?? new JArray())
            {
                var card = UiKit.Box("panel").Into(page);
                UiKit.Text(d.Value<string>("name"), "quest__title").Into(card);
                UiKit.Text(Loc.T("guild.donationGives", d.Value<long>("guildExp")), "label--small").Into(card);
                var gives = UiKit.Box("row").Into(card);
                foreach (JObject a in d["gives"] as JArray ?? new JArray())
                    gives.Add(UiKit.Amount(a.Value<string>("id"), a.Value<long>("value")));
                var row = UiKit.Box("row").Into(card);
                var cost = d["cost"]?.ToObject<Amount[]>(JsonSerializer.Create(SaveJson.Settings)) ?? new Amount[0];
                row.Add(GearUi.CostRow(Ui.State, cost));
                UiKit.Box("spacer").Into(row);
                var id = d.Value<string>("id");
                var button = UiKit.Btn(Loc.T("guild.donate"), () => Ui.Runner.StartCoroutine(Donate(id)), "btn--small").Into(row);
                button.SetEnabled(!donated && cost.All(c => Ui.State.Get(c.Id) >= c.Value));
            }
        }

        private IEnumerator Donate(string id)
        {
            if (_busy)
                yield break;
            _busy = true;
            yield return Ui.Server.Flush();
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("POST", "/v1/guild/donate", new JObject { ["donationId"] = id }, x => r = x);
            _busy = false;
            if (!r.Ok)
            {
                Ui.Toast(Loc.Has("error." + r.Error) ? Loc.T("error." + r.Error) : Loc.T("server.error", r.Error));
                yield break;
            }
            yield return Ui.Server.Resync("guild");
            Load();
        }

        private void RaidPage(VisualElement page)
        {
            var bossId = _guild.Value<string>("raidBoss");
            var boss = Ui.Content.Hero(bossId);
            var card = UiKit.Box("panel", "dungeon").Into(page);
            var art = UiKit.Box("dungeon__art").Into(card);
            var assets = Ui.Catalog.Hero(bossId);
            var image = assets?.full != null ? assets.full : assets?.portrait;
            if (image != null)
                art.style.backgroundImage = new StyleBackground(image);
            UiKit.Text(boss.Name, "dungeon__name").Into(card);
            UiKit.Text(Loc.T("guild.raidLevel", _guild.Value<int>("raidLevel")), "subtitle").Into(card);
            var hp = _guild.Value<long>("raidHp");
            var max = System.Math.Max(1, _guild.Value<long>("raidHpMax"));
            var bar = UiKit.Box("afk-progress", "raid-hp").Into(card);
            UiKit.Box("afk-progress__fill", "raid-hp__fill").Into(bar).style.width = Length.Percent(100f * hp / max);
            UiKit.Text($"{UiKit.Short(hp)} / {UiKit.Short(max)} HP", "label--small").Into(card);
            UiKit.Text(Loc.T("guild.raidHint"), "skill__text").Into(card);
            var left = _guild.Value<int>("raidLeft");
            var row = UiKit.Box("row", "center").Into(card);
            UiKit.Text(Loc.T("guild.raidLeft", left), "quest__points").Into(row);
            UiKit.Box("spacer").Into(row);
            UiKit.Btn(Loc.T("home.fight"), () => Ui.Runner.StartCoroutine(Fight()), "btn--small").Into(row).SetEnabled(left > 0);
        }

        private IEnumerator Fight()
        {
            if (_busy)
                yield break;
            _busy = true;
            yield return Ui.Server.Flush();
            ApiResponse r = null;
            yield return Ui.Server.Api.Send("POST", "/v1/guild/raid", new JObject(), x => r = x);
            _busy = false;
            if (!r.Ok)
            {
                Ui.Toast(Loc.Has("error." + r.Error) ? Loc.T("error." + r.Error) : Loc.T("server.error", r.Error));
                yield break;
            }
            yield return Ui.Server.Resync("guild");
            var team = JsonConvert.DeserializeObject<TeamSnapshot>(r.Body["team"].ToString(Formatting.None), SaveJson.Settings);
            var setup = Guilds.RaidSetup(Ui.Content, team, r.Body.Value<int>("raidLevel"), r.Body.Value<long>("hpLeft"), ulong.Parse(r.Body.Value<string>("seed")));
            Ui.Router.Push(new BattleScreen(setup, new RaidBattle(r.Body)));
        }
    }

    /// <summary>Replays a Guardian Raid attempt the server already decided.</summary>
    public sealed class RaidBattle : IBattleMode
    {
        private readonly JObject _fight;

        public RaidBattle(JObject fight) => _fight = fight;

        public bool Replay => true;

        public string Background(UiContext ui) => "raid";

        public string Title(UiContext ui) => $"Guardian Raid · {ui.Content.Hero(_fight.Value<string>("boss")).Name}";

        public BattleReport Finish(UiContext ui, BattleView battle)
        {
            var killed = _fight.Value<bool>("killed");
            var report = new BattleReport
            {
                // a raid attempt is a success whenever it dealt damage
                Outcome = BattleOutcome.Victory,
                Note = Loc.T(killed ? "guild.raidKilled" : "guild.raidDamage", UiKit.Short(_fight.Value<long>("damage")))
            };
            foreach (JObject a in _fight["rewards"] as JArray ?? new JArray())
                report.Rewards.Add(new Amount(a.Value<string>("id"), a.Value<long>("value")));
            return report;
        }

        public UiScreen After() => new GuildScreen(GuildScreen.Page.Raid);
    }
}
