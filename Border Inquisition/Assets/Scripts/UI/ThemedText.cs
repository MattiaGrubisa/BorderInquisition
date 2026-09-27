using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Keeps one text at its base size times UiTheme's font scale: applied when enabled, and again on
    // every open text when the theme changes (so Font Scale can be tuned in Play mode). A text built
    // by UiFactory.Label also grows its row height with a scale above 1 (rows never shrink, so
    // buttons stay lined up); scene texts only change size.
    [DisallowMultipleComponent]
    public class ThemedText : MonoBehaviour
    {
        private static readonly HashSet<ThemedText> Enabled = new HashSet<ThemedText>();

        private TMP_Text _text;
        private LayoutElement _layout;
        private float _size;
        private float _minSize;
        private float _maxSize;
        private float _height;

        // Base sizes as written in code or the inspector; the scale is applied on top.
        public static ThemedText Set(TMP_Text text, float size, float minSize = 0f, float maxSize = 0f, float height = 0f)
        {
            var themed = text.GetComponent<ThemedText>();
            if (themed == null)
                themed = text.gameObject.AddComponent<ThemedText>();

            themed._text = text;
            themed._size = size;
            themed._minSize = minSize > 0f ? minSize : text.fontSizeMin;
            themed._maxSize = maxSize > 0f ? maxSize : text.fontSizeMax;
            themed._height = height;
            themed._layout = height > 0f ? text.GetComponent<LayoutElement>() : null;
            themed.Apply();
            return themed;
        }

        // Every text under the root that has no ThemedText yet (the ones placed in the scene) gets
        // one at its inspector size, and the theme's font.
        public static void ThemeAll(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponent<ThemedText>() != null)
                    continue;

                UiFactory.ApplyFont(text);
                Set(text, text.fontSize, text.fontSizeMin, text.fontSizeMax);
            }
        }

        public static void ApplyAll()
        {
            foreach (var themed in Enabled)
                themed.Apply();
        }

        private void OnEnable()
        {
            Enabled.Add(this);
            Apply();
        }

        private void OnDisable() => Enabled.Remove(this);

        private void Apply()
        {
            if (_text == null)
                return;

            var scale = UiTheme.Current != null ? UiTheme.Current.FontScale : 1f;
            _text.fontSize = _size * scale;
            _text.fontSizeMin = _minSize * scale;
            _text.fontSizeMax = _maxSize * scale;
            if (_layout != null)
                _layout.preferredHeight = Mathf.Max(_height, _height * scale);
        }
    }
}
