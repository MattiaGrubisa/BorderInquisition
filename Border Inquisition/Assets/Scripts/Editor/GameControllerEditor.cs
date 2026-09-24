#if UNITY_EDITOR
using Gameplay.Managers;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    // One button that plays the running match to its end, so the path to GameOver can be tested
    // without any map input.
    [CustomEditor(typeof(GameController))]
    public class GameControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Play Out Match"))
                    ((GameController)target).PlayOutMatch();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play mode and start a match to play it out.", MessageType.None);
        }
    }
}
#endif
