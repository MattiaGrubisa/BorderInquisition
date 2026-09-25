using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    // Runtime counterpart of the editor scene factory: in-game panels are built in code from layout
    // groups, so the scene needs no wiring and rows can be rebuilt when their content changes.
    public static class UiFactory
    {
        public static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.1f, 0.92f);
        public static readonly Color ButtonColor = new Color(0.88f, 0.88f, 0.9f);
        public static readonly Color TextColor = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color ButtonTextColor = new Color(0.12f, 0.12f, 0.14f);

        public static RectTransform Panel(Transform parent, string name, Vector2 anchor)
        {
            var rect = Column(parent, name, 10f);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = Vector2.zero;

            var image = rect.gameObject.AddComponent<Image>();
            image.color = PanelColor;

            var layout = rect.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);

            var fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        public static RectTransform Column(Transform parent, string name, float spacing = 6f)
        {
            var rect = Create(parent, name);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            Configure(layout, spacing);
            return rect;
        }

        public static RectTransform Row(Transform parent, string name, float spacing = 8f)
        {
            var rect = Create(parent, name);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            Configure(layout, spacing);
            layout.childAlignment = TextAnchor.MiddleLeft;
            return rect;
        }

        public static TMP_Text Label(Transform parent, string content, float fontSize, float width, float height,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var rect = Create(parent, "Label");
            Size(rect, width, height);

            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = TextColor;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        // A label that wraps at the given width and grows as tall as its text.
        public static TMP_Text Paragraph(Transform parent, string content, float fontSize, float width)
        {
            var text = Label(parent, content, fontSize, width, 0f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.GetComponent<LayoutElement>().preferredHeight = -1f;
            return text;
        }

        // A full-screen layer with a canvas of its own, drawn above everything on the parent canvas
        // however the panels there are reordered. It swallows clicks, so nothing under it can be used
        // while it is up. Call Raise when showing it again: a nested canvas can lose its sorting
        // override while inactive.
        public static RectTransform Overlay(Transform parent, string name, int sortingOrder, Color background)
        {
            var rect = Create(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            rect.gameObject.AddComponent<GraphicRaycaster>();

            var image = rect.gameObject.AddComponent<Image>();
            image.color = background;
            return rect;
        }

        public static void Raise(RectTransform overlay, int sortingOrder)
        {
            var canvas = overlay.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        public static Button Button(Transform parent, string label, float width, float height, UnityAction onClick)
        {
            var rect = Create(parent, label);
            Size(rect, width, height);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var text = Label(rect, label, 24f, width, height, TextAlignmentOptions.Center);
            text.color = ButtonTextColor;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return button;
        }

        public static Image Icon(Transform parent, Sprite sprite, float width, float height)
        {
            var rect = Create(parent, "Icon");
            Size(rect, width, height);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        // Empty room in a layout row, to keep columns lined up.
        public static void Spacer(Transform parent, float width, float height) =>
            Size(Create(parent, "Spacer"), width, height);

        public static void SetLabel(Button button, string label) =>
            button.GetComponentInChildren<TMP_Text>().text = label;

        // Deactivated first, so the layout ignores the children at once instead of at end of frame.
        public static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }

        private static RectTransform Create(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Size(RectTransform rect, float width, float height)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Configure(HorizontalOrVerticalLayoutGroup layout, float spacing)
        {
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
    }
}
