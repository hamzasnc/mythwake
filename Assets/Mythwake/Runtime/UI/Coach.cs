using System;
using Mythwake.Core.Meta;
using Mythwake.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>
    /// Coach marks for new players (docs/systems/TUTORIAL.md): a pulsing ring around one button and a short line of
    /// text. Steps are driven by the player's progress, so players who are past a step never see it; tapping the
    /// marked button (or "OK" for hints) completes it. The marked button keeps working as usual.
    /// </summary>
    public static class Coach
    {
        private sealed class Step
        {
            public string Id;
            /// <summary>Element name the ring goes around (UI elements are named "coach-…").</summary>
            public string Target;
            public Func<UiScreen, bool> OnScreen;
            public Func<UiContext, bool> When;
            /// <summary>A hint only: an "OK" closes it, the target is just pointed at.</summary>
            public bool Hint;
        }

        public const string GateId = "summoningGate";

        private static readonly Step[] Steps =
        {
            new Step
            {
                Id = "first_fight", Target = "coach-fight", OnScreen = s => s is HomeScreen,
                When = ui => ui.State.Campaign.HighestCleared == 0,
            },
            new Step
            {
                Id = "formation_fight", Target = "coach-formation-fight", OnScreen = s => s is FormationScreen f && f.IsCampaign,
                When = ui => ui.State.Campaign.HighestCleared == 0,
            },
            new Step
            {
                Id = "ultimate", Target = "coach-ults", OnScreen = s => s is BattleScreen b && b.Mode is CampaignBattle, Hint = true,
                When = ui => ui.State.Campaign.HighestCleared == 0,
            },
            new Step
            {
                Id = "afk_claim", Target = "coach-afk", OnScreen = s => s is HomeScreen,
                When = ui => ui.State.Campaign.HighestCleared >= 1 && Afk.Pending(ui.State, ui.Content, ui.Authority.NowMs).Count > 0,
            },
            new Step
            {
                Id = "rebuild_gate", Target = "coach-nav-town", OnScreen = s => s is HomeScreen,
                When = GateBuildable,
            },
            new Step
            {
                Id = "gate_plot", Target = "coach-building-" + GateId, OnScreen = s => s is VillageScreen,
                When = GateBuildable,
            },
            new Step
            {
                Id = "summon", Target = "coach-nav-summon", OnScreen = s => s is HomeScreen || s is VillageScreen,
                When = ui => Village.Level(ui.State, Village.Building(ui.Content, GateId)) > 0 && Count(ui, Stat.Summons) == 0,
            },
            new Step
            {
                Id = "level_up", Target = "coach-nav-heroes", OnScreen = s => s is HomeScreen,
                When = ui => ui.State.Campaign.HighestCleared >= 3 && Count(ui, Stat.HeroLevelUps) == 0,
            },
        };

        private static VisualElement _overlay;
        private static Step _shown;
        private static VisualElement _target;
        private static IVisualElementScheduledItem _follow;

        private static long Count(UiContext ui, string stat) => ui.State.Stats.TryGetValue(stat, out var v) ? v : 0;

        private static bool GateBuildable(UiContext ui)
        {
            var def = Village.Building(ui.Content, GateId);
            return def != null && Village.Level(ui.State, def) == 0 && BuildingPanel.CanUpgrade(ui.State, def);
        }

        /// <summary>Shows the first due step for this screen, or removes the mark. Called on every screen change and state change.</summary>
        public static void Update(UiContext ui, UiScreen screen)
        {
            if (screen == null || ui.Authority == null)
            {
                Hide();
                return;
            }
            Step due = null;
            foreach (var step in Steps)
                if (!Tutorial.Done(ui.State, step.Id) && step.OnScreen(screen) && step.When(ui))
                {
                    due = step;
                    break;
                }
            if (due == null)
            {
                Hide();
                return;
            }
            var target = screen.Root.Q(due.Target) ?? ui.Root.Q(due.Target);
            if (due == _shown && target == _target && _overlay?.parent != null)
                return;
            Hide();
            if (target == null)
                return;
            Show(ui, due, target);
        }

        public static void Hide()
        {
            _follow?.Pause();
            _follow = null;
            _target?.UnregisterCallback<ClickEvent>(OnTargetClicked, TrickleDown.TrickleDown);
            _overlay?.RemoveFromHierarchy();
            _overlay = null;
            _shown = null;
            _target = null;
        }

        private static UiContext _ui;

        private static void Show(UiContext ui, Step step, VisualElement target)
        {
            _ui = ui;
            _shown = step;
            _target = target;
            _overlay = UiKit.Box("coach").Into(ui.Root);
            _overlay.pickingMode = PickingMode.Ignore;
            var ring = UiKit.Box("coach__ring").Into(_overlay);
            ring.pickingMode = PickingMode.Ignore;
            var bubble = UiKit.Box("coach__bubble").Into(_overlay);
            bubble.pickingMode = step.Hint ? PickingMode.Position : PickingMode.Ignore;
            UiKit.Text(Loc.T("tutorial." + step.Id), "coach__text").Into(bubble).pickingMode = PickingMode.Ignore;
            if (step.Hint)
                UiKit.Btn(Loc.T("tutorial.ok"), () => Complete(step), "btn--small").Into(bubble);
            else
                target.RegisterCallback<ClickEvent>(OnTargetClicked, TrickleDown.TrickleDown);
            // follow the target: layout settles after the first frame and lists may scroll
            _follow = _overlay.schedule.Execute(() => Place(ring, bubble)).Every(100);
            _overlay.schedule.Execute(() => ring.ToggleInClassList("coach__ring--big")).Every(600);
        }

        private static void Place(VisualElement ring, VisualElement bubble)
        {
            if (_target == null || _overlay == null)
                return;
            var r = _target.worldBound;
            var root = _overlay.parent;
            var visible = _target.panel != null && r.width > 1f && r.height > 1f && _target.resolvedStyle.display != DisplayStyle.None;
            _overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible || root == null)
                return;
            var local = root.WorldToLocal(r);
            const float pad = 10f;
            ring.style.left = local.x - pad;
            ring.style.top = local.y - pad;
            ring.style.width = local.width + 2 * pad;
            ring.style.height = local.height + 2 * pad;
            // bubble above the target when it sits in the lower half, else below; kept inside the screen
            var screen = root.layout;
            var width = Mathf.Min(560f, screen.width - 32f);
            bubble.style.width = width;
            bubble.style.left = Mathf.Clamp(local.center.x - width / 2f, 16f, screen.width - width - 16f);
            var height = bubble.resolvedStyle.height > 0 ? bubble.resolvedStyle.height : 120f;
            bubble.style.top = local.center.y > screen.height / 2f ? local.y - pad - 24f - height : local.yMax + pad + 24f;
        }

        private static void OnTargetClicked(ClickEvent e)
        {
            if (_shown != null)
                Complete(_shown);
        }

        private static void Complete(Step step)
        {
            var ui = _ui;
            Hide();
            ui?.Try(new CompleteTutorial { Id = step.Id }, out _);
        }
    }
}
