#if UNITY_EDITOR
using Gameplay;
using Gameplay.Managers;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    // Play mode tools for the running match (MatchSimulator): play it out to its end, or run a batch
    // of all-AI matches and log statistics for balancing. The batch settings and the foldout are kept
    // in EditorPrefs (this machine's editor, not the project), so they survive a restart.
    [CustomEditor(typeof(GameController))]
    public class GameControllerEditor : UnityEditor.Editor
    {
        private const string Prefix = "BorderInquisition.Simulation.";

        private static MatchSimulator.BatchSettings _batch;
        private static bool _batchOpen;

        private static MatchSimulator.BatchSettings Batch => _batch ??= LoadBatch();

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

            EditorGUI.BeginChangeCheck();
            _batchOpen = EditorGUILayout.Foldout(EditorPrefs.GetBool(Prefix + "Open"), "AI Simulation", true);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(Prefix + "Open", _batchOpen);
            if (_batchOpen)
                BatchGUI(game, running);

            if (!running)
                EditorGUILayout.HelpBox("Enter Play mode and start a match to use these.", MessageType.None);
        }

        private static void BatchGUI(GameController game, bool running)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();
                Batch.Matches = Mathf.Max(1, EditorGUILayout.IntField("Matches", Batch.Matches));
                Batch.Players = EditorGUILayout.IntSlider("Players", Batch.Players, MatchSettings.MinPlayers,
                    MatchSettings.MaxPlayers);
                Batch.Mixed = EditorGUILayout.Toggle("Mixed Difficulties", Batch.Mixed);
                using (new EditorGUI.DisabledScope(Batch.Mixed))
                    Batch.Difficulty = (Difficulty)EditorGUILayout.EnumPopup("Difficulty", Batch.Difficulty);
                Batch.Samples = Mathf.Max(1, EditorGUILayout.IntField("Battle Samples", Batch.Samples));
                Batch.RoundLimit = Mathf.Max(1, EditorGUILayout.IntField("Round Limit", Batch.RoundLimit));
                if (EditorGUI.EndChangeCheck())
                    SaveBatch();

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

        // Missing keys fall back to the BatchSettings defaults.
        private static MatchSimulator.BatchSettings LoadBatch()
        {
            var batch = new MatchSimulator.BatchSettings();
            batch.Matches = EditorPrefs.GetInt(Prefix + "Matches", batch.Matches);
            batch.Players = EditorPrefs.GetInt(Prefix + "Players", batch.Players);
            batch.Difficulty = (Difficulty)EditorPrefs.GetInt(Prefix + "Difficulty", (int)batch.Difficulty);
            batch.Mixed = EditorPrefs.GetBool(Prefix + "Mixed", batch.Mixed);
            batch.Samples = EditorPrefs.GetInt(Prefix + "Samples", batch.Samples);
            batch.RoundLimit = EditorPrefs.GetInt(Prefix + "RoundLimit", batch.RoundLimit);
            return batch;
        }

        private static void SaveBatch()
        {
            EditorPrefs.SetInt(Prefix + "Matches", Batch.Matches);
            EditorPrefs.SetInt(Prefix + "Players", Batch.Players);
            EditorPrefs.SetInt(Prefix + "Difficulty", (int)Batch.Difficulty);
            EditorPrefs.SetBool(Prefix + "Mixed", Batch.Mixed);
            EditorPrefs.SetInt(Prefix + "Samples", Batch.Samples);
            EditorPrefs.SetInt(Prefix + "RoundLimit", Batch.RoundLimit);
        }
    }
}
#endif
