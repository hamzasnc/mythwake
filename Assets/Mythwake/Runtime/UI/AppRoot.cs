using System.Collections;
using Mythwake.Game;
using Mythwake.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>
    /// Composition root of the client: loads content, connects to the server (or falls back to the local, offline
    /// authority), opens the first screen.
    /// </summary>
    public sealed class AppRoot : MonoBehaviour
    {
        [SerializeField] private ContentCatalog catalog;
        [SerializeField] private UIDocument document;
        [SerializeField] private BattleView battle;
        [SerializeField] private StyleSheet style;
        [Tooltip("Mythwake API. Empty = offline (local save). Command line: -server <url> or -offline.")]
        [SerializeField] private string serverUrl = "http://127.0.0.1:8080";

        private UiContext _ui;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            UnityEngine.Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Loc.Load(catalog.strings.Length > 0 ? catalog.strings[0] : null);

            var root = document.rootVisualElement;
            root.styleSheets.Add(style);
            root.AddToClassList("app");
            var layer = new VisualElement { pickingMode = PickingMode.Ignore };
            layer.style.flexGrow = 1;
            root.Add(layer);
            _ui = new UiContext { Catalog = catalog, Battle = battle, Root = root, Runner = this };
            _ui.Router = new Router(_ui, layer);
            StartCoroutine(Boot(layer));
        }

        private IEnumerator Boot(VisualElement layer)
        {
            var content = catalog.LoadContent();
            var autoPilot = AutoPilot.OutputDir();
            var url = Argument("-server") ?? (autoPilot != null || HasArgument("-offline") ? null : serverUrl);

            IGameAuthority authority = null;
            if (!string.IsNullOrEmpty(url))
            {
                var connecting = UiKit.Text(Loc.T("server.connecting"), "subtitle", "boot").Into(layer);
                ServerAuthority server = null;
                string error = null;
                yield return ServerAuthority.Connect(url, content, catalog.ContentVersion(), (s, e) =>
                {
                    server = s;
                    error = e;
                });
                connecting.RemoveFromHierarchy();
                if (server != null)
                {
                    authority = server;
                    _ui.Server = server;
                    server.Resynced += reason =>
                    {
                        if (reason != "arena")
                            _ui.Toast(Loc.T("server.resynced"));
                    };
                    StartCoroutine(server.Pump());
                }
                else
                {
                    Debug.LogWarning($"[Server] {url} not usable ({error}), playing offline");
                    _ui.Toast(Loc.T(error != null && error.StartsWith("content_mismatch") ? "server.outdated" : "server.offline"));
                }
            }
            if (authority == null)
            {
                // the smoke run plays on a throwaway save so it never touches a real player's progress
                var saveDir = autoPilot == null ? Application.persistentDataPath : System.IO.Path.Combine(autoPilot, "save");
                if (autoPilot != null && System.IO.Directory.Exists(saveDir))
                    System.IO.Directory.Delete(saveDir, true);
                authority = new LocalAuthority(content, new SystemClock(), new SaveStore(saveDir));
            }

            _ui.Authority = authority;
            authority.StateChanged += () =>
            {
                _ui.Router.Current?.Refresh();
                Coach.Update(_ui, _ui.Router.Current);
            };
            _ui.Router.Reset(autoPilot == null && StoryScreen.Due(authority.State) ? new StoryScreen() : new HomeScreen());
            if (autoPilot != null)
                StartCoroutine(AutoPilot.Run(_ui, autoPilot));
        }

        private static string Argument(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == name)
                    return args[i + 1];
            return null;
        }

        private static bool HasArgument(string name) => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), name) >= 0;

        private void Update()
        {
            if (_ui?.Authority != null)
                _ui.Router.Current?.Tick(Time.deltaTime);
        }
    }
}
