#if UNITY_EDITOR
using Gameplay;
using Gameplay.Managers;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    // Play mode tools for the running match (MatchSimulator): play it out to its end, or run a batch
    // of all-AI matches and log statistics for balancing. The batch settings last for the session.
    [CustomEditor(typeof(GameController))]
    public class GameControllerEditor : UnityEditor.Editor
    {
        private static readonly MatchSimulator.BatchSettings Batch = new MatchSimulator.BatchSettings();
        private static bool _batchOpen;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var game = (GameController)target;
            var running = Application.isPlaying && game.Players.Count > 0 && !game.IsOver;

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!running))
            {
                if (GUILayout.Button("Play Out Match"))
                    MatchSimulator.PlayOut(game, Batch.RoundLimit);
            }

            _batchOpen = EditorGUILayout.Foldout(_batchOpen, "AI Simulation", true);
            if (_batchOpen)
                BatchGUI(game, running);

            if (!running)
                EditorGUILayout.HelpBox("Enter Play mode and start a match to use these.", MessageType.None);
        }

        private static void BatchGUI(GameController game, bool running)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                Batch.Matches = Mathf.Max(1, EditorGUILayout.IntField("Matches", Batch.Matches));
                Batch.Players = EditorGUILayout.IntSlider("Players", Batch.Players, MatchSettings.MinPlayers,
                    MatchSettings.MaxPlayers);
                Batch.Mixed = EditorGUILayout.Toggle("Mixed Difficulties", Batch.Mixed);
                using (new EditorGUI.DisabledScope(Batch.Mixed))
                    Batch.Difficulty = (Difficulty)EditorGUILayout.EnumPopup("Difficulty", Batch.Difficulty);
                Batch.Samples = Mathf.Max(1, EditorGUILayout.IntField("Battle Samples", Batch.Samples));
                Batch.RoundLimit = Mathf.Max(1, EditorGUILayout.IntField("Round Limit", Batch.RoundLimit));

                EditorGUILayout.HelpBox("Plays every match in one go - the editor waits, the progress bar can " +
                                        "cancel. Statistics go to the console, then the game returns to the main " +
                                        "menu. Play Out Match uses the round limit too.", MessageType.None);

                using (new EditorGUI.DisabledScope(!running))
                {
                    if (GUILayout.Button("Run AI Simulation"))
                        MatchSimulator.RunBatch(game, Batch);
                }
            }
        }
    }
}
#endif
