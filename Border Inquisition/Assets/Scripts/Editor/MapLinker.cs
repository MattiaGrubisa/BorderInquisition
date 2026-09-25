#if UNITY_EDITOR
using Gameplay;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    // Draws every country and its borders as scene gizmos, to check the map graph at a glance. Borders
    // themselves are authored in the Country inspector.
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
