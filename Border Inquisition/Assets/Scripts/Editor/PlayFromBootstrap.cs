#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    // Nothing runs without the GameStateMachine, which only Bootstrap holds, so Play is redirected
    // there no matter which scene is open. Toggle it off from the same menu item to play as-is.
    [InitializeOnLoad]
    public static class PlayFromBootstrap
    {
        private const string MenuPath = "Border Inquisition/Always Play From Bootstrap";
        private const string PreferenceKey = "BorderInquisition.PlayFromBootstrap";
        private const string BootstrapPath = "Assets/Scenes/Bootstrap.unity";

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PreferenceKey, true);
            set => EditorPrefs.SetBool(PreferenceKey, value);
        }

        // The asset database is not ready while static constructors run.
        static PlayFromBootstrap() => EditorApplication.delayCall += Apply;

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool DrawToggle()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Apply()
        {
            if (!Enabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            var bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapPath);
            if (bootstrap == null)
            {
                Debug.LogWarning($"{BootstrapPath} is missing - run Border Inquisition > Create Flow Scenes.");
                return;
            }

            EditorSceneManager.playModeStartScene = bootstrap;
        }
    }
}
#endif
