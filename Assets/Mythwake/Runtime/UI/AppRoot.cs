using Mythwake.Game;
using Mythwake.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mythwake.UI
{
    /// <summary>Composition root of the client: loads content and the save, creates the authority, opens the first screen.</summary>
    public sealed class AppRoot : MonoBehaviour
    {
        [SerializeField] private ContentCatalog catalog;
        [SerializeField] private UIDocument document;
        [SerializeField] private BattleView battle;
        [SerializeField] private StyleSheet style;

        private UiContext _ui;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            UnityEngine.Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Loc.Load(catalog.strings.Length > 0 ? catalog.strings[0] : null);

            var content = catalog.LoadContent();
            var autoPilot = AutoPilot.OutputDir();
            // the smoke run plays on a throwaway save so it never touches a real player's progress
            var saveDir = autoPilot == null ? Application.persistentDataPath : System.IO.Path.Combine(autoPilot, "save");
            if (autoPilot != null && System.IO.Directory.Exists(saveDir))
                System.IO.Directory.Delete(saveDir, true);
            var authority = new LocalAuthority(content, new SystemClock(), new SaveStore(saveDir));

            var root = document.rootVisualElement;
            root.styleSheets.Add(style);
            root.AddToClassList("app");
            var layer = new VisualElement { pickingMode = PickingMode.Ignore };
            layer.style.flexGrow = 1;
            root.Add(layer);

            _ui = new UiContext { Authority = authority, Catalog = catalog, Battle = battle, Root = root };
            _ui.Router = new Router(_ui, layer);
            authority.StateChanged += () => _ui.Router.Current?.Refresh();
            _ui.Router.Reset(new HomeScreen());
            if (autoPilot != null)
                StartCoroutine(AutoPilot.Run(_ui, autoPilot));
        }

        private void Update()
        {
            _ui?.Router.Current?.Tick(Time.deltaTime);
        }
    }
}
