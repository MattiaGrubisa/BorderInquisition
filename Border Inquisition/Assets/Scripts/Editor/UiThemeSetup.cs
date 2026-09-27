#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    // Border Inquisition > Set Up UI Theme: makes Resources/UiTheme.asset if there is none (Daydream
    // font, ribbon 13_20 for large buttons, 13_10 for small ones; slots already set are kept), then
    // puts the theme's font into the TMP default and on every text in the build scenes. Safe to run
    // again - change the font in the theme and run it to switch everything over.
    public static class UiThemeSetup
    {
        private const string ThemeFolder = "Assets/Resources";
        private const string ThemePath = ThemeFolder + "/" + UiTheme.ResourcePath + ".asset";
        private const string FontPath = "Assets/Assets/Fonts/Daydream DEMO SDF.asset";
        private const string RibbonsPath = "Assets/Assets/GUI/Retro Pixel Ribbons, Banners and Frames 2/13.png";
        private const string LargeButtonSprite = "13_20";
        private const string SmallButtonSprite = "13_10";

        [MenuItem("Border Inquisition/Set Up UI Theme")]
        public static void SetUpUiTheme()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Leave Play mode to set up the UI theme.");
                return;
            }

            var theme = FindOrCreateTheme();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(RibbonsPath).OfType<Sprite>().ToList();
            theme.Fill(
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath),
                sprites.FirstOrDefault(sprite => sprite.name == LargeButtonSprite),
                sprites.FirstOrDefault(sprite => sprite.name == SmallButtonSprite));
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            if (theme.Font == null)
            {
                Debug.LogError($"The UI theme has no font - put one in {ThemePath} and run this again.", theme);
                return;
            }

            SetDefaultFont(theme.Font);
            var changed = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Sum(scene => ApplyFont(scene.path, theme.Font));

            Selection.activeObject = theme;
            Debug.Log($"UI theme applied: {changed} scene text(s) switched to {theme.Font.name}.", theme);
        }

        private static UiTheme FindOrCreateTheme()
        {
            var existing = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder(ThemeFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");
            var theme = ScriptableObject.CreateInstance<UiTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
            return theme;
        }

        // Every TMP text made without a font of its own starts with this one.
        private static void SetDefaultFont(TMP_FontAsset font)
        {
            var settings = TMP_Settings.instance;
            if (settings == null)
            {
                Debug.LogWarning("No TMP Settings asset found - the TMP default font is unchanged.");
                return;
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        // Inactive objects included; a scene that was not open is opened, saved and closed again.
        private static int ApplyFont(string scenePath, TMP_FontAsset font)
        {
            var scene = EditorSceneManager.GetSceneByPath(scenePath);
            var opened = !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            var changed = 0;
            foreach (var text in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)))
            {
                if (text.font == font)
                    continue;

                Undo.RecordObject(text, "Apply UI Theme Font");
                text.font = font;
                EditorUtility.SetDirty(text);
                changed++;
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
            return changed;
        }
    }
}
#endif
