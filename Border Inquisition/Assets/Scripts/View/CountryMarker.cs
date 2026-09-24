using Gameplay;
using TMPro;
using UnityEngine;

namespace View
{
    // Crudest possible country marker: a disc in the owner's colour carrying the income dice number,
    // with the army (knights/horsemen/archers) underneath. It lives in the world, not on a canvas, so
    // it pans and zooms with the map for free. It reads the country every frame - there are only a
    // few dozen of them.
    public class CountryMarker : MonoBehaviour
    {
        public enum Highlight
        {
            None,
            Selected,
            Target,
            Destination
        }

        private const float Diameter = 0.32f;
        private const float HaloScale = 1.45f;
        private const int SortingOrder = 10;
        private const int DiscPixels = 64;
        private const int RimPixels = 3;

        private static readonly Color SelectedColor = Color.white;
        private static readonly Color TargetColor = new Color(1f, 0.15f, 0.1f);
        private static readonly Color DestinationColor = new Color(0.2f, 0.95f, 0.3f);

        private static Sprite _discSprite;

        private Country _country;
        private SpriteRenderer _disc;
        private SpriteRenderer _halo;
        private TextMeshPro _diceLabel;
        private TextMeshPro _armyLabel;

        public Country Country => _country;

        public static CountryMarker Create(Country country)
        {
            var go = new GameObject("Marker");
            go.transform.SetParent(country.transform, false);

            var marker = go.AddComponent<CountryMarker>();
            marker.Build(country);
            return marker;
        }

        private void Build(Country country)
        {
            _country = country;

            _disc = gameObject.AddComponent<SpriteRenderer>();
            _disc.sprite = DiscSprite;
            _disc.sortingOrder = SortingOrder;
            transform.localScale = Vector3.one * Diameter;

            var halo = new GameObject("Halo");
            halo.transform.SetParent(transform, false);
            halo.transform.localScale = Vector3.one * HaloScale;
            _halo = halo.AddComponent<SpriteRenderer>();
            _halo.sprite = DiscSprite;
            _halo.sortingOrder = SortingOrder - 1;
            _halo.enabled = false;

            _diceLabel = CreateLabel("DiceNumber", Vector3.zero, 2.2f, Color.black);
            _armyLabel = CreateLabel("Army", new Vector3(0f, -0.95f, 0f), 1.4f, Color.white);
            _armyLabel.outlineWidth = 0.25f;
            _armyLabel.outlineColor = Color.black;
        }

        private void LateUpdate()
        {
            if (_country == null)
                return;

            var army = _country.Army;
            _disc.color = PlayerPalette.ColorOf(_country.Owner);
            _diceLabel.text = _country.DiceNumber.ToString();
            _armyLabel.text = $"{army.Knights}/{army.Horsemen}/{army.Archers}";
        }

        public void SetHighlight(Highlight highlight)
        {
            _halo.enabled = highlight != Highlight.None;
            _halo.color = HaloColor(highlight);
        }

        private static Color HaloColor(Highlight highlight)
        {
            switch (highlight)
            {
                case Highlight.Target:
                    return TargetColor;
                case Highlight.Destination:
                    return DestinationColor;
            }
            return SelectedColor;
        }

        // Labels sit under the scaled marker, so their size is divided back out to stay in world units.
        private TextMeshPro CreateLabel(string labelName, Vector3 localPosition, float fontSize, Color colour)
        {
            var go = new GameObject(labelName, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one / Diameter;

            var label = go.AddComponent<TextMeshPro>();
            label.fontSize = fontSize;
            label.color = colour;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(1f, 0.3f);
            label.sortingOrder = SortingOrder + 1;
            return label;
        }

        private static Sprite DiscSprite => _discSprite != null ? _discSprite : _discSprite = BuildDiscSprite();

        // White disc with a dark rim; the SpriteRenderer colour tints the white part only.
        private static Sprite BuildDiscSprite()
        {
            var texture = new Texture2D(DiscPixels, DiscPixels, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var radius = DiscPixels / 2f;
            var pixels = new Color32[DiscPixels * DiscPixels];
            for (var y = 0; y < DiscPixels; y++)
            {
                for (var x = 0; x < DiscPixels; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    var alpha = Mathf.Clamp01(radius - distance);
                    var shade = distance > radius - RimPixels ? 0.15f : 1f;
                    pixels[y * DiscPixels + x] = new Color(shade, shade, shade, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, DiscPixels, DiscPixels), new Vector2(0.5f, 0.5f), DiscPixels);
        }
    }
}
