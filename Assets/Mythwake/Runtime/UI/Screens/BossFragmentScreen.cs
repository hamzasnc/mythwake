using System.Linq;
using Mythwake.Core.Meta;
using Mythwake.Game;
using Mythwake.Presentation;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Boss Fragments (docs/systems/BOSS_FRAGMENTS.md): refight dungeon bosses for chests, upgrade, equip one.</summary>
    public sealed class BossFragmentScreen : UiScreen
    {
        private CurrencyBar _currencies;
        private Label _entries;
        private VisualElement _body;

        protected override void Build(VisualElement root)
        {
            root.AddToClassList("screen--solid");
            root.AddToClassList("screen--dungeon");
            _currencies = new CurrencyBar(Ids.FragmentDust, Ids.Gold).Into(root);
            var safe = UiKit.Box("safe").Into(root);
            var head = UiKit.Box("row").Into(safe);
            UiKit.IconBtn("back", () => Ui.Router.Back(() => new DungeonScreen())).Into(head);
            UiKit.Text(Loc.T("bossfrag.title"), "title").Into(head);
            _entries = UiKit.Text("", "quest__points").Into(safe);
            UiKit.Text(Loc.T("bossfrag.hint"), "label--small").Into(safe).style.whiteSpace = WhiteSpace.Normal;
            var scroll = new ScrollView(ScrollViewMode.Vertical).Into(safe);
            scroll.style.flexGrow = 1;
            _body = UiKit.Box().Into(scroll);
        }

        public override void Refresh()
        {
            var content = Ui.Content;
            _currencies.Refresh(Ui.State);
            var left = BossFragments.EntriesLeft(Ui.State, content, Ui.Authority.NowMs);
            _entries.text = Loc.T("bossfrag.entries", left, content.BossFragments.EntriesPerDay);
            _body.Clear();
            foreach (var f in content.BossFragments.Fragments)
                _body.Add(Card(f, left > 0));
        }

        private VisualElement Card(BossFragmentDefinition f, bool entries)
        {
            var content = Ui.Content;
            var state = Ui.State;
            var dungeon = Dungeons.Get(content, f.Dungeon);
            var progress = BossFragments.Progress(state, f.Id);
            var level = progress?.Level ?? 0;
            var equipped = state.BossFragments.Equipped == f.Id;
            var card = UiKit.Box("panel", "boss-fragment");
            card.EnableInClassList("boss-fragment--equipped", equipped);
            card.EnableInClassList("relic-card--missing", level == 0);

            var row = UiKit.Box("row").Into(card);
            var art = UiKit.Box("fragment__icon--big").Into(row);
            var assets = Ui.Catalog.Hero(dungeon.Boss);
            var image = assets?.portrait != null ? assets.portrait : assets?.full;
            if (image != null)
                art.style.backgroundImage = new StyleBackground(image);
            var texts = UiKit.Box("grow").Into(row);
            texts.style.flexBasis = 0;
            UiKit.Text(f.Name, "building__name").Into(texts).style.whiteSpace = WhiteSpace.Normal;
            var value = BossFragments.MainValue(content, f, System.Math.Max(1, level));
            UiKit.Text((f.Description ?? "").Replace("{0}", (value / 10f).ToString("0.#")), "skill__text").Into(texts);
            if (level > 0)
            {
                UiKit.Text(Loc.T("bossfrag.level", level, content.BossFragments.MaxLevel) + "   " +
                           Loc.T("bossfrag.copies", progress.Copies, level), "gear__level").Into(texts);
            }
            else
                UiKit.Text(Loc.T("bossfrag.locked"), "label--small").Into(texts).style.whiteSpace = WhiteSpace.Normal;

            var buttons = UiKit.Box("row", "center").Into(card);
            buttons.style.flexWrap = Wrap.Wrap;
            var open = BossFragments.RefightOpen(state, content, f);
            if (!open)
                UiKit.Text(Loc.T("bossfrag.refightLocked", dungeon.Name), "label--small").Into(buttons);
            else
            {
                var refight = UiKit.Btn(Loc.T("bossfrag.refight", Dungeons.Highest(state, f.Dungeon)), () => Refight(f), "btn--small").Into(buttons);
                refight.SetEnabled(entries);
            }
            UiKit.Box("spacer").Into(buttons);
            if (level > 0 && level < content.BossFragments.MaxLevel)
            {
                var cost = BossFragments.UpgradeCost(content, level);
                buttons.Add(GearUi.CostRow(state, cost));
                var up = UiKit.Btn(Loc.T("bossfrag.upgrade"), () =>
                {
                    if (Ui.Try(new UpgradeBossFragment { FragmentId = f.Id }, out int _))
                        Refresh();
                }, "btn--secondary", "btn--small").Into(buttons);
                up.SetEnabled(progress.Copies >= level && cost.All(c => state.Get(c.Id) >= c.Value));
            }
            if (level > 0)
            {
                var equip = UiKit.Btn(equipped ? Loc.T("bossfrag.equipped") : Loc.T("bossfrag.equip"), () =>
                {
                    if (Ui.Try(new EquipBossFragment { FragmentId = equipped ? null : f.Id }, out bool _))
                        Refresh();
                }, equipped ? "btn--small" : "btn--secondary", "btn--small").Into(buttons);
            }
            return card;
        }

        private void Refight(BossFragmentDefinition f)
        {
            if (Ui.Try(new BeginBossRefight { FragmentId = f.Id }, out var setup))
                Ui.Router.Push(new BattleScreen(setup, new BossRefightBattle(f.Name)));
        }
    }

    public sealed class BossRefightBattle : IBattleMode
    {
        private readonly string _name;

        public BossRefightBattle(string name) => _name = name;

        public bool Replay => false;

        public string Background(UiContext ui) => "dungeon";

        public string Title(UiContext ui) => _name;

        public BattleReport Finish(UiContext ui, BattleView battle)
        {
            var finish = new FinishBossRefight { AutoUltimates = battle.AutoAtStart };
            finish.AutoToggleTimesMs.AddRange(battle.AutoToggles);
            finish.UltimateRequests.AddRange(battle.Requests);
            if (!ui.Try(finish, out var chest))
                return null;
            string note = null;
            if (chest.Outcome == Core.Combat.BattleOutcome.Victory)
            {
                var name = BossFragments.Get(ui.Content, chest.FragmentId).Name;
                note = chest.Unlocked ? Loc.T("bossfrag.unlocked", name) : chest.Copy ? Loc.T("bossfrag.copy", name) : Loc.T("bossfrag.noCopy");
            }
            return new BattleReport { Outcome = chest.Outcome, Rewards = chest.Rewards, Note = note };
        }

        public UiScreen After() => new BossFragmentScreen();
    }
}
