using TMPro;
using UnityEngine;

namespace View
{
    // A line of text that rises from a country and fades out, then removes itself - the income a
    // country just paid. Presentation only: the resources are already in the player's pool.
    public class IncomePopup : MonoBehaviour
    {
        private const float Duration = 1.8f;
        private const float Rise = 0.35f;
        private const float StartHeight = 0.2f;
        private const int SortingOrder = 20;

        private static readonly Color Colour = new Color(1f, 0.86f, 0.3f);

        private TextMeshPro _label;
        private Vector3 _start;
        private float _elapsed;

        // Created at the root of the active scene (WorldMap), so no parent scale reaches the text and
        // it unloads with the map.
        public static void Spawn(Vector3 position, string text, float delay)
        {
            var go = new GameObject("IncomePopup", typeof(RectTransform));

            var popup = go.AddComponent<IncomePopup>();
            popup._start = position + Vector3.up * StartHeight;
            popup._elapsed = -delay;
            popup.transform.position = popup._start;

            var label = go.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = 1.5f;
            label.color = Colour;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.outlineWidth = 0.25f;
            label.outlineColor = Color.black;
            label.rectTransform.sizeDelta = new Vector2(2f, 0.3f);
            label.sortingOrder = SortingOrder;
            label.enabled = delay <= 0f;
            popup._label = label;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < 0f)
                return;

            _label.enabled = true;
            var t = _elapsed / Duration;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = _start + Vector3.up * (Rise * t);
            var colour = Colour;
            colour.a = 1f - t * t;
            _label.color = colour;
        }
    }
}
