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
