using System.Collections.Generic;
using Mythwake.Core.Meta;
using Mythwake.Game;
using Mythwake.Presentation;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Shared services every screen gets.</summary>
    public sealed class UiContext
    {
        public IGameAuthority Authority;
        /// <summary>Set when playing online; server-only features (Arena) need it.</summary>
        public ServerAuthority Server;
        /// <summary>Runs coroutines for screens (server calls).</summary>
        public UnityEngine.MonoBehaviour Runner;
        public ContentCatalog Catalog;
        public BattleView Battle;
        public Router Router;
        public VisualElement Root;

        public GameContent Content => Authority.Content;
        public PlayerState State => Authority.State;

        /// <summary>Runs a command; shows the localized error as a toast when a rule says no.</summary>
        public bool Try<T>(ICommand<T> command, out T result)
        {
            var outcome = Authority.Execute(command);
            result = outcome.Value;
            if (!outcome.Ok)
                Toast(Loc.Has("error." + outcome.Error) ? Loc.T("error." + outcome.Error) : Loc.T("error.generic"));
            return outcome.Ok;
        }

        public void Toast(string message)
        {
            var toast = UiKit.Text(message, "toast");
            Root.Add(toast);
            toast.schedule.Execute(() => toast.style.opacity = 0f).StartingIn(1600);
            toast.schedule.Execute(() => toast.RemoveFromHierarchy()).StartingIn(2000);
        }
    }

    public abstract class UiScreen
    {
        protected UiContext Ui { get; private set; }
        public VisualElement Root { get; private set; }

        public void Attach(UiContext ui)
        {
            Ui = ui;
            Root = UiKit.Box("screen");
            Build(Root);
        }

        protected abstract void Build(VisualElement root);

        /// <summary>Called when the screen becomes visible and whenever player state changes while it is visible.</summary>
        public virtual void Refresh() { }

        public virtual void OnShow() => Refresh();
        public virtual void OnHide() { }

        /// <summary>Per-frame update for screens that animate (battle HUD).</summary>
        public virtual void Tick(float dt) { }
    }

    /// <summary>Stack navigation between screens; the top screen is the only one in the visual tree.</summary>
    public sealed class Router
    {
        private readonly UiContext _ui;
        private readonly VisualElement _layer;
        private readonly Stack<UiScreen> _stack = new Stack<UiScreen>();

        public Router(UiContext ui, VisualElement layer)
        {
            _ui = ui;
            _layer = layer;
        }

        public UiScreen Current => _stack.Count > 0 ? _stack.Peek() : null;

        public void Push(UiScreen screen)
        {
            Hide(Current);
            if (screen.Root == null)
                screen.Attach(_ui);
            _stack.Push(screen);
            Show(screen);
        }

        /// <summary>Replaces the whole stack (bottom navigation).</summary>
        public void Reset(UiScreen screen)
        {
            Hide(Current);
            _stack.Clear();
            Push(screen);
        }

        public void Pop()
        {
            if (_stack.Count <= 1)
                return;
            Hide(_stack.Pop());
            Show(Current);
        }

        private void Show(UiScreen s)
        {
            if (s == null)
                return;
            _layer.Add(s.Root);
            s.OnShow();
        }

        private void Hide(UiScreen s)
        {
            if (s == null)
                return;
            s.OnHide();
            s.Root.RemoveFromHierarchy();
        }
    }
}
