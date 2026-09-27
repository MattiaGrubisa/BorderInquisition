using TMPro;
using UnityEngine;

namespace UI
{
    // The look every view shares: the font, and the sprites UiFactory puts on the buttons it builds.
    // One asset, at Resources/UiTheme, made and applied by Border Inquisition > Set Up UI Theme, which
    // also writes the font into the TMP default and every text already placed in a scene.
    [CreateAssetMenu(menuName = "Create UI Theme", fileName = "UiTheme")]
    public class UiTheme : ScriptableObject
    {
        public const string ResourcePath = "UiTheme";

        [SerializeField] private TMP_FontAsset _font;

        // Every text size built in code is multiplied by this (scene texts keep their own sizes).
        [SerializeField, Range(0.3f, 2f)] private float _fontScale = 1f;

        // Big buttons (panel actions, the action bar) and small ones (row buttons, +/-).
        [SerializeField] private Sprite _largeButton;
        [SerializeField] private Sprite _smallButton;

        // A button at least this tall gets the large sprite.
        [SerializeField] private float _largeButtonMinHeight = 50f;

        // With borders set in the Sprite Editor the ribbons are 9-sliced; this scales their ends up
        // (pixel art at 1 would look thin).
        [SerializeField] private float _slicedPixelScale = 0.5f;

        [SerializeField] private Color _buttonTextColor = new Color(0.12f, 0.12f, 0.14f);

        // Room left and right of a button's label, so it stays off the ribbon's ends.
        [SerializeField] private float _labelPadding = 12f;

        private static UiTheme _current;
        private static bool _loaded;

        // Null when there is no theme asset; UiFactory then keeps its plain look.
        public static UiTheme Current
        {
            get
            {
                if (!_loaded)
                {
                    _current = Resources.Load<UiTheme>(ResourcePath);
                    _loaded = true;
                }
                return _current;
            }
        }

        public TMP_FontAsset Font => _font;
        public float FontScale => _fontScale;
        public Color ButtonTextColor => _buttonTextColor;
        public float LabelPadding => _labelPadding;
        public float SlicedPixelScale => _slicedPixelScale;

        public Sprite ButtonSprite(float height) => height >= _largeButtonMinHeight ? _largeButton : _smallButton;

#if UNITY_EDITOR
        public void Fill(TMP_FontAsset font, Sprite largeButton, Sprite smallButton)
        {
            if (_font == null)
                _font = font;
            if (_largeButton == null)
                _largeButton = largeButton;
            if (_smallButton == null)
                _smallButton = smallButton;
        }
#endif
    }
}
