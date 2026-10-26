using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Core.Progression;
using Mythwake.Game;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Hunt Board (daily and weekly hunts) and Hunt Shop (docs/systems/HUNT_BOARD.md).</summary>
    public sealed class HuntScreen : UiScreen
    {
        private bool _shop;
        private CurrencyBar _currencies;
        private VisualElement _tabs;
        private VisualElement _body;
        private Label _reset;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            _currencies = new CurrencyBar(Ids.HuntTokens, Ids.Gold, Ids.GearMaterials).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.IconBtn("back", () => Ui.Router.Back(() => new AdventureScreen())).Into(head);
            UiKit.Text("Hunt Board", "title").Into(head);
            UiKit.Box("spacer").Into(head);
            _reset = UiKit.Text("", "label--small").Into(head);
            _tabs = UiKit.Box("tabs").Into(safe);
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _body = UiKit.Box().Into(scroll);
        }

        public override void Refresh()
        {
            _currencies.Refresh(Ui.State);
            var now = Ui.Authority.NowMs;
            var left = System.TimeSpan.FromMilliseconds(Quests.NextDailyResetMs(Ui.Content, now) - now);
            _reset.text = Loc.T("quests.reset", $"{left.Hours}h {left.Minutes:00}m");
            _tabs.Clear();
            Tab(Loc.T("hunt.tab.board"), false);
            Tab("Hunt Shop", true);
            _body.Clear();
            if (_shop)
                BuildShop();
            else
                BuildBoard();
        }

        private void Tab(string text, bool shop)
        {
            var b = UiKit.Btn(text, () =>
            {
                _shop = shop;
                Refresh();
            }, "tab").Into(_tabs);
            b.EnableInClassList("tab--active", _shop == shop);
        }

        private void BuildBoard()
        {
            var now = Ui.Authority.NowMs;
            var board = Hunts.Board(Ui.State, Ui.Content, now);
            if (board.Count == 0)
            {
                UiKit.Text(Loc.T("hunt.locked"), "subtitle").Into(UiKit.Box("panel").Into(_body));
                return;
            }
            var team = Campaign.Allies(Ui.State, Ui.Content, Ui.State.Formation).Sum(u => TeamBuilder.Power(u.Stats));
            UiKit.Text(Loc.T("hunt.daily"), "subtitle").Into(_body);
            foreach (var offer in board.Where(o => !o.Weekly))
                _body.Add(Card(offer, team, now));
            UiKit.Text(Loc.T("hunt.weekly"), "subtitle").Into(_body);
            foreach (var offer in board.Where(o => o.Weekly))
                _body.Add(Card(offer, team, now));
        }

        private VisualElement Card(HuntOffer offer, long teamPower, long now)
        {
            var done = Hunts.IsDone(Ui.State, Ui.Content, offer, now);
            var card = UiKit.Box("panel", "hunt").With(offer.Weekly ? "hunt--elite" : "hunt--daily");
            card.EnableInClassList("quest--claimed", done);
            var head = UiKit.Box("row").Into(card);
            UiKit.Text(offer.Mission.Name, "quest__title").Into(head);
            UiKit.Box("spacer").Into(head);
            UiKit.Text(new string('★', offer.Tier), "hunt__stars").Into(head);

            var enemy = Hunts.Enemies(Ui.State, Ui.Content, offer).Sum(u => TeamBuilder.Power(u.Stats));
            var power = UiKit.Text(Loc.T("hunt.power", UiKit.Short(enemy), UiKit.Short(teamPower)), "label--small").Into(card);
            power.EnableInClassList("label--bad", enemy > teamPower);

            var rewards = UiKit.Box("row").Into(card);
            rewards.style.flexWrap = Wrap.Wrap;
            foreach (var r in offer.Rewards)
                rewards.Add(UiKit.Amount(r.Id, r.Value));

            if (done)
            {
                UiKit.Text(Loc.T("hunt.done"), "label--good").Into(card);
            }
            else
            {
                var id = offer.Id;
                var title = offer.Mission.Name;
                UiKit.Btn(Loc.T("hunt.start"), () =>
                {
                    if (Ui.Try(new BeginHunt { HuntId = id }, out var setup))
                        Ui.Router.Push(new BattleScreen(setup, new HuntBattle(title)));
                }).Into(card);
            }
            return card;
        }

        private void BuildShop()
        {
            var now = Ui.Authority.NowMs;
            if (Hunts.HutLevel(Ui.State, Ui.Content) <= 0)
            {
                UiKit.Text(Loc.T("hunt.locked"), "subtitle").Into(UiKit.Box("panel").Into(_body));
                return;
            }
            foreach (var item in Ui.Content.Hunts.Shop)
            {
                var it = item;
                var bought = Hunts.Bought(Ui.State, Ui.Content, item, now);
                var card = UiKit.Box("panel").Into(_body);
                var row = UiKit.Box("row").Into(card);
                var info = UiKit.Box("grow").Into(row);
                if (item.GearGrade != null)
                    UiKit.Text(Loc.T("hunt.shop.gear", Loc.T("grade." + UiKit.Lower(item.GearGrade.Value))), "quest__title").Into(info);
                foreach (var g in item.Gives)
                    info.Add(UiKit.Amount(g.Id, g.Value));
                UiKit.Text(Loc.T(item.Weekly ? "hunt.shop.weekly" : "hunt.shop.daily", item.Limit - bought, item.Limit), "label--small").Into(info);
                var buy = UiKit.Btn("", () =>
                {
                    if (Ui.Try(new BuyHuntItem { ItemId = it.Id }, out GearItem piece))
                    {
                        if (piece != null)
                            Ui.Toast(Loc.T("hunt.shop.got", GearUi.SlotName(piece.Slot)));
                        Refresh();
                    }
                }, "btn--small").Into(row);
                buy.Add(GearUi.CostRow(Ui.State, item.Price));
                buy.SetEnabled(bought < item.Limit && item.Price.All(p => Ui.State.Get(p.Id) >= p.Value));
            }
        }
    }
}
