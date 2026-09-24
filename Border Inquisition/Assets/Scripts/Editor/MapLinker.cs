#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    // Authoring borders by dragging object references is slow and easy to get wrong, so borders are
    // made from the hierarchy selection instead and drawn as gizmos to check the result at a glance.
    public static class MapLinker
    {
        private static readonly Color BorderColor = new Color(1f, 0.82f, 0.25f, 0.9f);
        private const float NodeRadius = 0.07f;

        [MenuItem("Border Inquisition/Link Selected Countries %#l")]
        private static void Link()
        {
            if (!TryReadSelection(out var hub, out var others))
                return;

            var added = others.Count(other => hub.AddBorder(other));
            Commit(hub, $"Linked {hub.name} to {added} of {others.Count} selected countr(y/ies).");
        }

        [MenuItem("Border Inquisition/Unlink Selected Countries %#u")]
        private static void Unlink()
        {
            if (!TryReadSelection(out var hub, out var others))
                return;

            var removed = others.Count(other => hub.RemoveBorder(other));
            foreach (var other in others)
                EditorUtility.SetDirty(other);

            Commit(hub, $"Removed {removed} border(s) from {hub.name}.");
        }

        // The hub is the active object - the one clicked last, drawn with a lighter outline.
        private static bool TryReadSelection(out Country hub, out IReadOnlyList<Country> others)
        {
            hub = null;
            others = null;

            var selected = Selection.gameObjects
                .Select(go => go.GetComponent<Country>())
                .Where(country => country != null)
                .ToList();

            if (selected.Count < 2)
            {
                Debug.LogWarning("Select a country and its neighbours, clicking the shared country last.");
                return false;
            }

            var active = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponent<Country>();
            var shared = active != null ? active : selected[0];

            hub = shared;
            others = selected.Where(country => country != shared).ToList();
            return true;
        }

        private static void Commit(Country hub, string message)
        {
            EditorUtility.SetDirty(hub);
            EditorSceneManager.MarkSceneDirty(hub.gameObject.scene);
            Debug.Log(message, hub);
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void DrawBorders(Country country, GizmoType type)
        {
            Gizmos.color = BorderColor;
            Gizmos.DrawSphere(country.transform.position, NodeRadius);

            // Borders are authored one way, so every edge is drawn exactly once.
            foreach (var border in country.Borders)
                if (border != null)
                    Gizmos.DrawLine(country.transform.position, border.transform.position);
        }
    }
}
#endif
