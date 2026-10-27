using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Main quest, daily and weekly tasks with activity chests (docs/systems/QUESTS.md).</summary>
    public sealed class QuestScreen : UiScreen
    {
        private enum Page { Main, Daily, Weekly }

        private Page _page = Page.Main;
        private CurrencyBar _currencies;
        private VisualElement _tabs;
        private VisualElement _body;
        private Label _reset;
        private IVisualElementScheduledItem _ticker;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            root.AddToClassList("screen--quests");
            _currencies = new CurrencyBar(Ids.Gold, Ids.Gems, Ids.SummonScroll).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.IconBtn("back", () => Ui.Router.Pop(), "back-btn").Into(root);
            UiKit.Text(Loc.T("quests.title"), "title", "screen-title").Into(head);
            UiKit.Box("spacer").Into(head);
            _reset = UiKit.Text("", "label--small").Into(head);
            _tabs = UiKit.Box("tabs").Into(safe);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _body = UiKit.Box().Into(scroll);
        }

        public override void OnShow()
        {
            base.OnShow();
            _ticker = Root.schedule.Execute(UpdateReset).Every(1000);
        }

        public override void OnHide() => _ticker?.Pause();

        public override void Refresh()
        {
            _currencies.Refresh(Ui.State);
            _tabs.Clear();
            foreach (Page p in System.Enum.GetValues(typeof(Page)))
            {
                var page = p;
                var b = UiKit.Btn(Loc.T("quests.tab." + UiKit.Lower(p)), () =>
                {
                    _page = page;
                    Refresh();
                }, "tab").Into(_tabs);
                b.EnableInClassList("tab--active", p == _page);
            }
            _body.Clear();
            if (_page == Page.Main)
                BuildMain();
            else
                BuildPeriod(_page == Page.Daily ? QuestKind.Daily : QuestKind.Weekly);
            UpdateReset();
        }

        private void UpdateReset()
        {
            var now = Ui.Authority.NowMs;
            if (_page == Page.Main)
            {
                _reset.text = "";
                return;
            }
            var next = _page == Page.Daily ? Quests.NextDailyResetMs(Ui.Content, now) : Quests.NextWeeklyResetMs(Ui.Content, now);
            var left = System.TimeSpan.FromMilliseconds(next - now);
            _reset.text = Loc.T("quests.reset", left.Days > 0 ? $"{left.Days}d {left.Hours}h" : $"{left.Hours}h {left.Minutes:00}m");
        }

        private void BuildMain()
        {
            var quest = Quests.CurrentMain(Ui.State, Ui.Content);
            var panel = UiKit.Box("panel").Into(_body);
            if (quest == null)
            {
                UiKit.Text(Loc.T("quests.mainDone"), "subtitle").Into(panel);
                return;
            }
            UiKit.Text(Loc.T("quests.main", Ui.State.Quests.Main + 1), "label--small").Into(panel);
            UiKit.Text(quest.Text, "quest__title").Into(panel);
            var (have, need) = Quests.MainProgress(Ui.State, Ui.Content, quest);
            Progress(panel, have, need);
            Rewards(panel, quest.Rewards);
            var claim = UiKit.Btn(Loc.T("quests.claim"), () =>
            {
                if (Ui.Try(new ClaimMainQuest(), out List<Amount> _))
                    Refresh();
            }).Into(panel);
            claim.SetEnabled(have >= need);
        }

        private void BuildPeriod(QuestKind kind)
        {
            var now = Ui.Authority.NowMs;
            var points = Quests.Points(Ui.State, Ui.Content, kind, now);
            var chests = Quests.Chests(Ui.Content, kind);
            var max = chests.Count > 0 ? chests.Max(c => c.Points) : 100;

            var activity = UiKit.Box("panel").Into(_body);
            var head = UiKit.Box("row").Into(activity);
            UiKit.Text(Loc.T("quests.activity"), "subtitle").Into(head);
            UiKit.Box("spacer").Into(head);
            UiKit.Text($"{points} / {max}", "quest__points").Into(head);
            Progress(activity, points, max);
            var row = UiKit.Box("row", "chests").Into(activity);
            foreach (var c in chests)
            {
                var chest = c;
                var claimed = Quests.ChestClaimed(Ui.State, Ui.Content, kind, c.Points, now);
                var ready = points >= c.Points && !claimed;
                var box = UiKit.Box("chest").Into(row);
                box.EnableInClassList("chest--ready", ready);
                box.EnableInClassList("chest--claimed", claimed);
                UiKit.Text(c.Points.ToString(), "chest__points").Into(box);
                foreach (var r in c.Rewards)
                {
                    var line = UiKit.Box("row").Into(box);
                    UiKit.Box("currency__icon", "icon--" + r.Id).Into(line);
                    UiKit.Text(UiKit.Short(r.Value), "chest__value").Into(line);
                }
                if (ready)
                    box.RegisterCallback<ClickEvent>(_ =>
                    {
                        if (Ui.Try(new ClaimQuestChest { Kind = kind, Points = chest.Points }, out List<Amount> _))
                            Refresh();
                    });
            }

            foreach (var t in Quests.Tasks(Ui.Content, kind))
            {
                var task = t;
                var progress = Quests.Progress(Ui.State, Ui.Content, kind, t, now);
                var claimed = Quests.IsClaimed(Ui.State, Ui.Content, kind, t.Id, now);
                var panel = UiKit.Box("panel", "quest").Into(_body);
                panel.EnableInClassList("quest--claimed", claimed);
                var line = UiKit.Box("row").Into(panel);
                var texts = UiKit.Box("grow").Into(line);
                UiKit.Text(t.Text, "quest__title").Into(texts);
                UiKit.Text($"+{t.Points} {Loc.T("quests.points")}", "label--small").Into(texts);
                if (claimed)
                {
                    UiKit.Icon("check", "quest__done").Into(line);
                }
                else
                {
                    var b = UiKit.Btn(Loc.T("quests.claim"), () =>
                    {
                        if (Ui.Try(new ClaimQuest { Kind = kind, QuestId = task.Id }, out int _))
                            Refresh();
                    }, "btn--small").Into(line);
                    b.SetEnabled(progress >= t.Target);
                }
                Progress(panel, progress, t.Target);
            }
        }

        private static void Progress(VisualElement parent, long have, long need)
        {
            var bar = UiKit.Box("afk-progress").Into(parent);
            UiKit.Box("afk-progress__fill").Into(bar).style.width = Length.Percent(need <= 0 ? 100 : 100f * System.Math.Min(have, need) / need);
            UiKit.Text($"{have} / {need}", "label--small").Into(parent);
        }

        private static void Rewards(VisualElement parent, IEnumerable<Amount> rewards)
        {
            var row = UiKit.Box("row").Into(parent);
            row.style.flexWrap = Wrap.Wrap;
            foreach (var r in rewards)
                row.Add(UiKit.Amount(r.Id, r.Value));
        }
    }
}
