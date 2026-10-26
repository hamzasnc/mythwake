using System.Collections.Generic;
using System.Linq;
using Mythwake.Core.Combat;
using Mythwake.Core.Content;
using Mythwake.Core.Meta;
using Mythwake.Game;
using Mythwake.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>What a finished battle gave, for the result popup.</summary>
    public sealed class BattleReport
    {
        public BattleOutcome Outcome;
        public List<Amount> Rewards = new List<Amount>();
        public List<GearItem> Gear = new List<GearItem>();
        public List<FragmentItem> Fragments = new List<FragmentItem>();
        public List<RelicItem> Relics = new List<RelicItem>();
        public string Note;
    }

    /// <summary>Where a battle comes from (campaign stage, hunt, …): its title, how it is settled, where to go after.</summary>
    public interface IBattleMode
    {
        string Title(UiContext ui);
        /// <summary>The result is already decided (server fight): always Auto, no ultimate taps.</summary>
        bool Replay { get; }
        /// <summary>Sends the finished battle to the authority. Null when the rules rejected it.</summary>
        BattleReport Finish(UiContext ui, BattleView battle);
        UiScreen After();
    }

    public sealed class CampaignBattle : IBattleMode
    {
        private readonly int _stage;

        public CampaignBattle(int stage) => _stage = stage;

        public bool Replay => false;

        public string Title(UiContext ui)
        {
            var (_, index, number) = Campaign.Locate(ui.Content, _stage);
            return Loc.T("home.stage", number, index);
        }

        public BattleReport Finish(UiContext ui, BattleView battle)
        {
            var finish = new FinishStageBattle { AutoUltimates = battle.AutoAtStart };
            finish.AutoToggleTimesMs.AddRange(battle.AutoToggles);
            finish.UltimateRequests.AddRange(battle.Requests);
            var slotsBefore = Formations.UnlockedSlots(ui.State, ui.Content);
            if (!ui.Try(finish, out var stage))
                return null;
            var report = new BattleReport { Outcome = stage.Outcome, Rewards = stage.Rewards, Gear = stage.Gear, Fragments = stage.Fragments, Relics = stage.Relics };
            if (stage.UnlockedSlots > slotsBefore)
                report.Note = Loc.T("battle.slotUnlocked");
            return report;
        }

        public UiScreen After() => new HomeScreen();
    }

    public sealed class HuntBattle : IBattleMode
    {
        private readonly string _title;

        public HuntBattle(string title) => _title = title;

        public bool Replay => false;

        public string Title(UiContext ui) => _title;

        public BattleReport Finish(UiContext ui, BattleView battle)
        {
            var finish = new FinishHunt { AutoUltimates = battle.AutoAtStart };
            finish.AutoToggleTimesMs.AddRange(battle.AutoToggles);
            finish.UltimateRequests.AddRange(battle.Requests);
            if (!ui.Try(finish, out var hunt))
                return null;
            return new BattleReport { Outcome = hunt.Outcome, Rewards = hunt.Rewards };
        }

        public UiScreen After() => new HuntScreen();
    }

    /// <summary>Battle HUD over the <see cref="BattleView"/>: ultimate portraits, Auto and speed toggles, floating numbers, cut-ins, result.</summary>
    public sealed class BattleScreen : UiScreen
    {
        private const string AutoPref = "battle.auto";
        private const string SpeedPref = "battle.speed";

        private readonly BattleSetup _setup;
        private readonly IBattleMode _mode;
        private readonly List<UltButton> _ults = new List<UltButton>();
        private readonly List<Floating> _floating = new List<Floating>();
        private VisualElement _floatLayer;
        private VisualElement _cutIn;
        private Button _auto;
        private Button _speed;
        private bool _done;

        private sealed class UltButton
        {
            public int Slot;
            public VisualElement Root;
            public VisualElement Energy;
            public VisualElement Hp;
        }

        private sealed class Floating
        {
            public Label Label;
            public Vector3 World;
            public float Age;
            public float Life;
            public float Drift;
        }

        public BattleScreen(BattleSetup setup, int stage) : this(setup, new CampaignBattle(stage)) { }

        public BattleScreen(BattleSetup setup, IBattleMode mode)
        {
            _setup = setup;
            _mode = mode;
        }

        private BattleView Battle => Ui.Battle;

        protected override void Build(VisualElement root)
        {
            root.pickingMode = PickingMode.Ignore;
            _floatLayer = UiKit.Box("screen").Into(root);
            _floatLayer.pickingMode = PickingMode.Ignore;

            var top = UiKit.Box("hud-top").Into(root);
            UiKit.Text(_mode.Title(Ui), "title").Into(top);
            var toggles = UiKit.Box("row").Into(top);
            _speed = UiKit.Btn("", ToggleSpeed, "btn--secondary", "btn--small").Into(toggles);
            _auto = UiKit.Btn("", ToggleAuto, "btn--secondary", "btn--small").Into(toggles);

            UiKit.Box("hud-bottom").Into(root);
        }

        public override void OnShow()
        {
            var auto = _mode.Replay || PlayerPrefs.GetInt(AutoPref, 1) == 1;
            _auto.style.display = _mode.Replay ? DisplayStyle.None : DisplayStyle.Flex;
            Battle.Speed = PlayerPrefs.GetFloat(SpeedPref, 1f);
            Battle.FloatingText += OnFloatingText;
            Battle.UltimateCutIn += OnCutIn;
            Battle.LeaderAbility += OnLeaderAbility;
            Battle.BossFragmentAbility += OnBossFragment;
            Battle.Finished += OnFinished;
            Battle.Begin(Ui.Catalog, _setup, auto);
            BuildUltButtons();
            UpdateToggles();
        }

        public override void OnHide()
        {
            Battle.FloatingText -= OnFloatingText;
            Battle.UltimateCutIn -= OnCutIn;
            Battle.LeaderAbility -= OnLeaderAbility;
            Battle.BossFragmentAbility -= OnBossFragment;
            Battle.Finished -= OnFinished;
            Battle.Clear();
        }

        private void BuildUltButtons()
        {
            var bottom = Root.Q(className: "hud-bottom");
            bottom.Clear();
            _ults.Clear();
            foreach (var unit in _setup.Allies.OrderBy(u => u.Slot))
            {
                var b = new UltButton { Slot = unit.Slot };
                b.Root = UiKit.HeroCard(Ui.Catalog, unit.Hero, Ui.State.FindHero(unit.Hero.Id)).With("ult-button");
                b.Root.RemoveFromClassList("hero-card");
                var hpBar = UiKit.Box("ult-button__hp").Into(b.Root);
                b.Hp = UiKit.Box("ult-button__hp-fill").Into(hpBar);
                var energyBar = UiKit.Box("ult-button__energy").Into(b.Root);
                b.Energy = UiKit.Box("ult-button__energy-fill").Into(energyBar);
                var slot = unit.Slot;
                if (!_mode.Replay)
                    b.Root.RegisterCallback<ClickEvent>(_ => Battle.RequestUltimate(slot));
                bottom.Add(b.Root);
                _ults.Add(b);
            }
        }

        private void UpdateToggles()
        {
            _auto.text = Battle.AutoUltimates ? Loc.T("battle.auto") : Loc.T("battle.manual");
            _auto.EnableInClassList("btn--secondary", !Battle.AutoUltimates);
            _speed.text = Loc.T("battle.speed", Battle.Speed.ToString("0"));
        }

        private void ToggleAuto()
        {
            Battle.SetAutoUltimates(!Battle.AutoUltimates);
            PlayerPrefs.SetInt(AutoPref, Battle.AutoUltimates ? 1 : 0);
            UpdateToggles();
        }

        private void ToggleSpeed()
        {
            Battle.Speed = Battle.Speed >= 2f ? 1f : 2f;
            PlayerPrefs.SetFloat(SpeedPref, Battle.Speed);
            UpdateToggles();
        }

        public override void Tick(float dt)
        {
            foreach (var b in _ults)
            {
                var unit = Battle.Units.FirstOrDefault(u => u != null && u.Side == Side.Allies && u.Slot == b.Slot);
                if (unit == null)
                    continue;
                b.Energy.style.width = Length.Percent(100f * unit.Energy / BattleSimulator.MaxEnergy);
                b.Hp.style.width = Length.Percent(100f * unit.Hp / unit.MaxHp);
                b.Root.EnableInClassList("ult-button--ready", !unit.Dead && unit.Energy >= BattleSimulator.MaxEnergy);
                b.Root.EnableInClassList("ult-button--dead", unit.Dead);
            }

            var cam = Camera.main;
            for (var i = _floating.Count - 1; i >= 0; i--)
            {
                var f = _floating[i];
                f.Age += dt;
                var t = f.Age / f.Life;
                if (t >= 1f || cam == null || Root.panel == null)
                {
                    f.Label.RemoveFromHierarchy();
                    _floating.RemoveAt(i);
                    continue;
                }
                var world = f.World + new Vector3(f.Drift * t, 0.9f * t, 0f);
                var p = RuntimePanelUtils.CameraTransformWorldToPanel(Root.panel, world, cam);
                f.Label.style.left = p.x;
                f.Label.style.top = p.y;
                f.Label.style.opacity = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                var pop = t < 0.12f ? 1f + (0.12f - t) * 3f : 1f;
                f.Label.style.scale = new Scale(new Vector3(pop, pop, 1f));
            }
        }

        private void OnFloatingText(UnitView unit, long value, FloatingTextKind kind)
        {
            var text = kind switch
            {
                FloatingTextKind.Miss => Loc.T("battle.miss"),
                FloatingTextKind.Heal => "+" + UiKit.Short(value),
                FloatingTextKind.Shield => "◈" + UiKit.Short(value),
                _ => UiKit.Short(value)
            };
            var label = UiKit.Text(text, "floating", "floating--" + UiKit.Lower(kind)).Into(_floatLayer);
            label.pickingMode = PickingMode.Ignore;
            _floating.Add(new Floating
            {
                Label = label,
                World = unit.HeadPosition + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.1f, 0.2f), 0f),
                Life = kind == FloatingTextKind.Crit ? 1.1f : 0.85f,
                Drift = Random.Range(-0.3f, 0.3f)
            });
        }

        /// <summary>The off-field Hero Leader used a leader ability: a short banner with its portrait and the skill name.</summary>
        private void OnLeaderAbility(SkillSlot slot)
        {
            var leader = _setup.Leader?.Hero;
            if (leader == null)
                return;
            var skill = slot == SkillSlot.Special ? leader.Special : slot == SkillSlot.Special2 ? leader.Special2 : null;
            var banner = UiKit.Box("leader-banner").Into(Root);
            var assets = Ui.Catalog.Hero(leader.Id);
            if (assets?.portrait != null)
                UiKit.Box("leader-banner__portrait").Into(banner).style.backgroundImage = new StyleBackground(assets.portrait);
            UiKit.Text(Loc.T("battle.leader", skill?.Name ?? leader.Name), "leader-banner__text").Into(banner);
            banner.schedule.Execute(() => banner.style.opacity = 0f).StartingIn(1400);
            banner.schedule.Execute(() => banner.RemoveFromHierarchy()).StartingIn(1800);
        }

        private void OnBossFragment()
        {
            var f = _setup.BossFragment?.Hero;
            if (f == null)
                return;
            var banner = UiKit.Box("leader-banner", "leader-banner--boss").Into(Root);
            var boss = Ui.Content.Dungeons.Dungeons.FirstOrDefault(d => d.Id == BossFragments.Get(Ui.Content, f.Id).Dungeon)?.Boss;
            var assets = boss == null ? null : Ui.Catalog.Hero(boss);
            if (assets?.portrait != null)
                UiKit.Box("leader-banner__portrait").Into(banner).style.backgroundImage = new StyleBackground(assets.portrait);
            UiKit.Text(f.Special?.Name ?? f.Name, "leader-banner__text").Into(banner);
            banner.schedule.Execute(() => banner.style.opacity = 0f).StartingIn(1400);
            banner.schedule.Execute(() => banner.RemoveFromHierarchy()).StartingIn(1800);
        }

        private void OnCutIn(UnitView unit, string heroId)
        {
            _cutIn?.RemoveFromHierarchy();
            var hero = Ui.Content.Hero(heroId);
            _cutIn = UiKit.Box("cutin").Into(Root);
            var assets = Ui.Catalog.Hero(heroId);
            var art = assets?.cutIn != null ? assets.cutIn : assets?.portrait;
            if (art != null)
                UiKit.Box("cutin__image").Into(_cutIn).style.backgroundImage = new StyleBackground(art);
            var text = UiKit.Box("cutin__text").Into(_cutIn);
            UiKit.Text(hero.Name, "cutin__name").Into(text);
            UiKit.Text(hero.Ultimate?.Name ?? "", "cutin__skill").Into(text);
            _cutIn.style.translate = new Translate(Length.Percent(-100), 0);
            var cut = _cutIn;
            cut.schedule.Execute(() => cut.style.translate = new Translate(0, 0)).StartingIn(10);
            cut.schedule.Execute(() =>
            {
                cut.style.translate = new Translate(Length.Percent(100), 0);
                cut.style.opacity = 0f;
            }).StartingIn(780);
            cut.schedule.Execute(() => cut.RemoveFromHierarchy()).StartingIn(1000);
        }

        private void OnFinished(BattleResult result)
        {
            if (_done)
                return;
            _done = true;
            var report = _mode.Finish(Ui, Battle);
            if (report == null)
            {
                Ui.Router.Reset(_mode.After());
                return;
            }
            ShowResult(report);
        }

        private void ShowResult(BattleReport report)
        {
            var victory = report.Outcome == BattleOutcome.Victory;
            var overlay = UiKit.Box("overlay").Into(Root);
            var popup = UiKit.Box("panel", "popup").Into(overlay);
            UiKit.Text(Loc.T(victory ? "battle.victory" : "battle.defeat"), "result-title", victory ? "result-title--victory" : "result-title--defeat").Into(popup);
            if (victory || report.Rewards.Count > 0)
            {
                UiKit.Text(Loc.T("battle.rewards"), "subtitle").Into(popup);
                var rewards = UiKit.Box("row").Into(popup);
                rewards.style.flexWrap = Wrap.Wrap;
                rewards.style.justifyContent = Justify.Center;
                foreach (var r in report.Rewards)
                    rewards.Add(UiKit.Amount(r.Id, r.Value));
                if (report.Gear.Count > 0)
                {
                    UiKit.Text(Loc.T("gear.drop"), "subtitle").Into(popup);
                    var drops = UiKit.Box("row").Into(popup);
                    foreach (var item in report.Gear)
                        drops.Add(GearUi.Tile(Ui.Content, item));
                }
                if (report.Fragments.Count > 0)
                {
                    UiKit.Text(Loc.T("fragment.drop"), "subtitle").Into(popup);
                    var drops = UiKit.Box("row").Into(popup);
                    foreach (var item in report.Fragments)
                        drops.Add(FragmentUi.Tile(Ui, item));
                }
                if (report.Relics.Count > 0)
                {
                    UiKit.Text(Loc.T("relic.drop"), "subtitle").Into(popup);
                    var drops = UiKit.Box("row").Into(popup);
                    foreach (var item in report.Relics)
                        drops.Add(RelicUi.Tile(Ui, item));
                }
            }
            if (!string.IsNullOrEmpty(report.Note))
                UiKit.Text(report.Note, victory ? "label--good" : "label--bad").Into(popup);
            if (!victory && !_mode.Replay)
            {
                UiKit.Text(Loc.T("battle.tip.defeat"), "subtitle").Into(popup).style.whiteSpace = WhiteSpace.Normal;
            }
            var buttons = UiKit.Box("row").Into(popup);
            if (!victory)
                UiKit.Btn(Loc.T("nav.heroes"), () => Ui.Router.Reset(new HeroesScreen()), "btn--secondary").Into(buttons);
            UiKit.Btn(Loc.T("battle.continue"), () => Ui.Router.Reset(_mode.After())).Into(buttons);
        }
    }
}
