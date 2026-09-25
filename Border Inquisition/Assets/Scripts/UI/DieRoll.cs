using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Tumbles a die image through random faces, then settles on the rolled one with a small pop.
    // Presentation only: the roll has already been applied, and nothing waits for this to finish.
    public class DieRoll : MonoBehaviour
    {
        public const float Duration = 0.6f;
        private const float FaceInterval = 0.06f;
        private const float PopDuration = 0.15f;
        private const float PopScale = 1.2f;

        private Image _image;
        private DiceFaces _faces;
        private int _value;
        private Color _settledColor;
        private float _elapsed;
        private float _nextFace;
        private bool _settled;

        public static void Roll(Image image, DiceFaces faces, int value, Color settledColor)
        {
            var roll = image.GetComponent<DieRoll>();
            if (roll == null)
                roll = image.gameObject.AddComponent<DieRoll>();

            roll._image = image;
            roll._faces = faces;
            roll._value = value;
            roll._settledColor = settledColor;
            roll._elapsed = 0f;
            roll._nextFace = 0f;
            roll._settled = false;
            roll.enabled = true;

            // Tumbles at full strength; a dimmed settled colour only shows once the result is in.
            image.enabled = true;
            image.color = new Color(settledColor.r, settledColor.g, settledColor.b, 1f);
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;

            if (_elapsed < Duration)
            {
                if (_elapsed >= _nextFace)
                {
                    _image.sprite = _faces.Face(Random.Range(1, 10));
                    _nextFace += FaceInterval;
                }
                return;
            }

            if (!_settled)
            {
                _settled = true;
                _image.sprite = _faces.Face(_value);
                _image.color = _settledColor;
            }

            var pop = Mathf.Clamp01((_elapsed - Duration) / PopDuration);
            transform.localScale = Vector3.one * Mathf.Lerp(PopScale, 1f, pop);
            if (pop >= 1f)
                enabled = false;
        }

        private void OnDisable()
        {
            if (_image == null)
                return;

            // Cut short (panel hidden, scene unloaded): land on the result straight away.
            _image.sprite = _faces.Face(_value);
            _image.color = _settledColor;
            transform.localScale = Vector3.one;
        }
    }
}
