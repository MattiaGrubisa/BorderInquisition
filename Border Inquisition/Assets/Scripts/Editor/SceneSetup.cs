#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gameplay;
using Gameplay.Managers;
using GameStates;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Editor
{
    // Scaffolds the scenes the game flow expects, wires the views and fills the build scene list.
    // A scene that already exists is left alone, so re-running never overwrites hand-made work.
    public static class SceneSetup
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string Bootstrap = "Bootstrap";
        private const string MainMenu = "MainMenu";
        private const string Lobby = "Lobby";
        private const string WorldMap = "WorldMap";
        private const string GameOver = "GameOver";

        // Build order; Bootstrap must stay first because it is the only scene loaded on its own.
        private static readonly string[] BuildOrder = { Bootstrap, MainMenu, Lobby, WorldMap, GameOver };

        private static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);
        private static readonly Vector2 WideButton = new Vector2(280, 64);
        private static readonly Vector2 SmallButton = new Vector2(64, 64);
        private static readonly Color LabelColor = new Color(0.16f, 0.16f, 0.18f);
        private const float DefaultArmyFactor = 1f;

        [MenuItem("Border Inquisition/Create Flow Scenes")]
        public static void CreateFlowScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (TMP_Settings.instance == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                Debug.LogWarning("TMP Essential Resources are importing. Run Border Inquisition > Create Flow Scenes again once the import finishes.");
                return;
            }

            var created = new List<string>();
            var skipped = new List<string>();

            Build(Bootstrap, BuildBootstrap, created, skipped);
            Build(MainMenu, BuildMainMenu, created, skipped);
            Build(Lobby, BuildLobby, created, skipped);
            Build(GameOver, BuildGameOver, created, skipped);
            UpdateWorldMap();
            ApplyBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Scene setup done. Created: {Join(created)}. Left untouched: {Join(skipped)}. " +
                      $"Open {Bootstrap} and press Play.");
        }

        #region Scenes

        private static void BuildBootstrap()
        {
            var camera = ObjectFactory.CreateGameObject("Main Camera",
                typeof(Camera), typeof(UniversalAdditionalCameraData), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var component = camera.GetComponent<Camera>();
            component.orthographic = true;
            component.orthographicSize = 5f;
            component.clearFlags = CameraClearFlags.SolidColor;
            component.backgroundColor = new Color(0.09f, 0.09f, 0.12f);

            ObjectFactory.CreateGameObject("GameStateMachine", typeof(GameStateMachine));
        }

        private static void BuildMainMenu()
        {
            var canvas = CreateCanvas();
            var view = ObjectFactory.AddComponent<MainMenuView>(canvas.gameObject);

            var play = CreateButton(canvas.transform, "PlayButton", "Play", new Vector2(0f, 40f), WideButton);
            var quit = CreateButton(canvas.transform, "QuitButton", "Quit", new Vector2(0f, -40f), WideButton);

            Wire(view, ("_playButton", play), ("_quitButton", quit));
        }

        private static void BuildLobby()
        {
            var canvas = CreateCanvas();
            var view = ObjectFactory.AddComponent<LobbyView>(canvas.gameObject);

            var label = CreateText(canvas.transform, "PlayerCountLabel", "Players: 4",
                new Vector2(0f, 140f), new Vector2(400f, 60f), 36f);
            var remove = CreateButton(canvas.transform, "RemovePlayerButton", "-", new Vector2(-90f, 50f), SmallButton);
            var add = CreateButton(canvas.transform, "AddPlayerButton", "+", new Vector2(90f, 50f), SmallButton);
            var start = CreateButton(canvas.transform, "StartButton", "Start", new Vector2(0f, -50f), WideButton);
            var back = CreateButton(canvas.transform, "BackButton", "Back", new Vector2(0f, -130f), WideButton);

            Wire(view,
                ("_removePlayerButton", remove),
                ("_addPlayerButton", add),
                ("_startButton", start),
                ("_backButton", back),
                ("_playerCountLabel", label));
        }

        private static void BuildGameOver()
        {
            var canvas = CreateCanvas();
            var view = ObjectFactory.AddComponent<GameOverView>(canvas.gameObject);

            var label = CreateText(canvas.transform, "WinnerLabel", "Game over",
                new Vector2(0f, 140f), new Vector2(800f, 90f), 48f);
            var rematch = CreateButton(canvas.transform, "RematchButton", "Rematch", new Vector2(0f, 0f), WideButton);
            var mainMenu = CreateButton(canvas.transform, "MainMenuButton", "Main Menu", new Vector2(0f, -80f), WideButton);

            Wire(view, ("_winnerLabel", label), ("_rematchButton", rematch), ("_mainMenuButton", mainMenu));
        }

        // WorldMap is hand-authored, so only the missing pieces are added and the camera is removed
        // (Bootstrap owns the only camera in the game).
        private static void UpdateWorldMap()
        {
            var path = ScenePath(WorldMap);
            if (!File.Exists(path))
            {
                Debug.LogError($"{path} is missing - create it before running the setup.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Object.DestroyImmediate(camera.gameObject);

            if (Object.FindFirstObjectByType<GameController>() == null)
                CreateGameController();

            if (Object.FindFirstObjectByType<InGameHud>() == null)
                CreateHud();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateGameController()
        {
            var go = ObjectFactory.CreateGameObject("GameController",
                typeof(Dice), typeof(Combat), typeof(GameController));

            var dice = go.GetComponent<Dice>();
            var combat = go.GetComponent<Combat>();

            Wire(combat, ("_dice", dice));
            SetFloat(combat, "_armyFactor", DefaultArmyFactor);
            Wire(go.GetComponent<GameController>(), ("_combat", combat), ("_dice", dice));
        }

        private static void CreateHud()
        {
            var canvas = CreateCanvas();
            var hud = ObjectFactory.AddComponent<InGameHud>(canvas.gameObject);

            var label = CreateText(canvas.transform, "TurnLabel", "-",
                new Vector2(0f, -60f), new Vector2(700f, 60f), 32f);
            Anchor(label.rectTransform, new Vector2(0.5f, 1f));

            var endPhase = CreateButton(canvas.transform, "EndPhaseButton", "End Phase",
                new Vector2(-180f, 60f), WideButton);
            Anchor(endPhase.GetComponent<RectTransform>(), new Vector2(1f, 0f));

            Wire(hud, ("_endPhaseButton", endPhase), ("_turnLabel", label));
        }

        #endregion

        #region UI factory

        private static Canvas CreateCanvas()
        {
            var go = ObjectFactory.CreateGameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            // Without an EventSystem no button ever receives a click, but a second one only warns.
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                ObjectFactory.CreateGameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return canvas;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            var go = ObjectFactory.CreateGameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = Place(go, parent, position, size);

            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            var text = CreateText(rect, "Label", label, Vector2.zero, size, 28f);
            Stretch(text.rectTransform);
            return button;
        }

        private static TMP_Text CreateText(Transform parent, string name, string content,
            Vector2 position, Vector2 size, float fontSize)
        {
            var go = ObjectFactory.CreateGameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Place(go, parent, position, size);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = LabelColor;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        private static RectTransform Place(GameObject go, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion

        #region Plumbing

        private static void Build(string sceneName, System.Action build, List<string> created, List<string> skipped)
        {
            var path = ScenePath(sceneName);
            if (File.Exists(path))
            {
                skipped.Add(sceneName);
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            build();
            EditorSceneManager.SaveScene(scene, path);
            created.Add(sceneName);
        }

        private static void ApplyBuildSettings()
        {
            EditorBuildSettings.scenes = BuildOrder
                .Select(ScenePath)
                .Where(File.Exists)
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        // Serialized fields are private, so the references are written through SerializedObject.
        private static void Wire(Component target, params (string Field, Object Value)[] links)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in links)
            {
                var property = serialized.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"{target.GetType().Name} has no serialized field '{field}'.", target);
                    continue;
                }

                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Component target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"{target.GetType().Name} has no serialized field '{field}'.", target);
                return;
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string ScenePath(string sceneName) => $"{ScenesFolder}/{sceneName}.unity";

        private static string Join(IReadOnlyCollection<string> names) =>
            names.Count == 0 ? "none" : string.Join(", ", names);

        #endregion
    }
}
#endif
