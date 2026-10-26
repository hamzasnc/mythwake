using UnityEditor;
using UnityEditor.SceneManagement;

namespace Mythwake.Editor
{
    /// <summary>
    /// Opens the game scene with the Game view in Device Simulator mode, so Play shows the game on a phone screen
    /// (pick the device in the Simulator's dropdown). Runs once per editor session and from Mythwake ▸ Phone Simulator.
    /// </summary>
    [InitializeOnLoad]
    public static class PhonePreview
    {
        private const string ScenePath = "Assets/Mythwake/Scenes/Main.unity";
        private const string DoneKey = "Mythwake.PhonePreview.Done";

        static PhonePreview()
        {
            if (UnityEngine.Application.isBatchMode || SessionState.GetBool(DoneKey, false))
                return;
            SessionState.SetBool(DoneKey, true);
            EditorApplication.delayCall += Open;
        }

        [MenuItem("Mythwake/Phone Simulator")]
        public static void Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (EditorSceneManager.GetActiveScene().path != ScenePath && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
            PlayModeWindow.SetViewType(PlayModeWindow.PlayModeViewTypes.SimulatorView);
        }
    }
}
