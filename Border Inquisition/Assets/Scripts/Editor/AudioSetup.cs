#if UNITY_EDITOR
using Audio;
using GameStates;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    // Border Inquisition > Set Up Audio: makes the AudioLibrary asset (one empty slot per sound and
    // track) if the project has none, and puts an AudioManager holding it next to GameStateMachine in
    // Bootstrap. Safe to run again - it only adds what is missing.
    public static class AudioSetup
    {
        private const string BootstrapPath = "Assets/Scenes/Bootstrap.unity";
        private const string LibraryFolder = "Assets/Audio";
        private const string LibraryPath = LibraryFolder + "/AudioLibrary.asset";

        [MenuItem("Border Inquisition/Set Up Audio")]
        public static void SetUpAudio()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Leave Play mode to set up audio.");
                return;
            }

            var library = FindOrCreateLibrary();

            var scene = SceneManager.GetSceneByPath(BootstrapPath);
            var opened = !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Additive);

            var machine = FindIn<GameStateMachine>(scene);
            if (machine == null)
            {
                Debug.LogError($"{BootstrapPath} has no GameStateMachine - run Create Flow Scenes first.");
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            if (!machine.TryGetComponent<AudioManager>(out var manager))
                manager = Undo.AddComponent<AudioManager>(machine.gameObject);

            var serialized = new SerializedObject(manager);
            var slot = serialized.FindProperty("_library");
            if (slot.objectReferenceValue == null)
            {
                slot.objectReferenceValue = library;
                serialized.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (opened)
                EditorSceneManager.CloseScene(scene, true);

            Selection.activeObject = library;
            Debug.Log($"Audio is set up: fill the clips in {AssetDatabase.GetAssetPath(library)}.", library);
        }

        private static AudioLibrary FindOrCreateLibrary()
        {
            var found = AssetDatabase.FindAssets($"t:{nameof(AudioLibrary)}");
            if (found.Length > 0)
                return AssetDatabase.LoadAssetAtPath<AudioLibrary>(AssetDatabase.GUIDToAssetPath(found[0]));

            if (!AssetDatabase.IsValidFolder(LibraryFolder))
                AssetDatabase.CreateFolder("Assets", "Audio");
            var library = ScriptableObject.CreateInstance<AudioLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
            library.FillSlots();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return library;
        }

        private static T FindIn<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
#endif
