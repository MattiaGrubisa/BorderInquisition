using System.Collections.Generic;
using System.Linq;
using Diplomacy;
using Gameplay;
using Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using View;

namespace UI
{
    // Follows the cursor over the map and describes the country under it, as the current player sees
    // it: name and continent always, owner, army, income and buildings only where fog of war allows,
    // and the queues only on the player's own countries. Hidden over any UI (so also behind the pause
    // menu), and never catches a click itself.
    public class CountryTooltip : MonoBehaviour
    {
        private const float Width = 420f;
        private static readonly Vector2 CursorOffset = new Vector2(24f, 24f);

        private RectTransform _root;
        private Canvas _canvas;
        private TMP_Text _text;
        private Country _shown;

        private static GameController Game => GameController.Instance;

        public static CountryTooltip Create(Transform canvas)
        {
            var root = UiFactory.Panel(canvas, "CountryTooltip", Vector2.zero);
            root.GetComponent<Image>().raycastTarget = false;
            root.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(14, 14, 10, 10);

            // The component lives on the canvas so it keeps polling while the panel is hidden.
            var tooltip = canvas.gameObject.AddComponent<CountryTooltip>();
            tooltip._root = root;
            tooltip._canvas = canvas.GetComponentInParent<Canvas>().rootCanvas;
            tooltip._text = UiFactory.Paragraph(root, "-", 20f, Width);
            root.gameObject.SetActive(false);
            return tooltip;
        }

        private void LateUpdate()
        {
            var country = CountryUnderCursor(out var screen);
            _root.gameObject.SetActive(country != null);
            if (country == null)
                return;

            var text = Describe(country);
            if (_text.text != text)
                _text.text = text;

            // Opens towards the middle of the screen, so it never runs off an edge.
            var right = screen.x > Screen.width * 0.6f;
            var below = screen.y > Screen.height * 0.4f;
            _root.pivot = new Vector2(right ? 1f : 0f, below ? 1f : 0f);
            var offset = new Vector2(right ? -CursorOffset.x : CursorOffset.x, below ? -CursorOffset.y : CursorOffset.y);
            _root.anchoredPosition = screen / _canvas.scaleFactor + offset;
            if (_root.GetSiblingIndex() != _root.parent.childCount - 1)
                _root.SetAsLastSibling();
        }

        private static Country CountryUnderCursor(out Vector2 screen)
        {
            screen = default;
            var mouse = Mouse.current;
            if (mouse == null || Game == null || Game.Players.Count == 0)
                return null;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return null;

            screen = mouse.position.ReadValue();
            return MapView.CountryAt(screen);
        }

        private static string Describe(Country country)
        {
            var viewer = Game.CurrentPlayer;
            var region = country.Region != null ? country.Region.name : "no region";
            var lines = new List<string> { $"<b>{country.name}</b>  <color=#9A9A9A>{region}</color>" };

            if (!Game.Fog.IsVisible(viewer, country))
            {
                lines.Add("<color=#9A9A9A>Hidden by fog of war</color>");
                lines.Add($"Pays on a {country.DiceNumber}");
                return string.Join("\n", lines);
            }

            lines.Add($"{Format.Name(country.Owner)}  <color=#9A9A9A>{Relation(viewer, country.Owner)}</color>");
            lines.Add(Format.Army(country.Army));
            lines.Add($"Pays on a {country.DiceNumber}: {Format.Resources(country.Income)}");

            var built = country.BuiltBuildings.Select(b => b.DisplayName).ToList();
            lines.Add("Buildings: " + (built.Count > 0 ? string.Join(", ", built) : "none"));

            if (country.Owner == viewer)
            {
                var queued = country.BuildingQueue.Select(b => b.DisplayName)
                    .Concat(country.TrainingQueue.GroupBy(s => s).Select(g => Format.Count(g.Count(), g.Key.ToString())))
                    .ToList();
                if (queued.Count > 0)
                    lines.Add("Queued: " + string.Join(", ", queued));
            }

            return string.Join("\n", lines);
        }

        private static string Relation(Player viewer, Player owner)
        {
            if (owner == viewer)
                return "you";

            var traitor = Game.Diplomacy.IsTraitor(owner) ? ", <color=#FF5040>traitor</color>" : string.Empty;
            var treaty = Game.Diplomacy.Between(viewer, owner);
            if (treaty == null)
                return "at war" + traitor;

            var kind = treaty.Kind == TreatyKind.Alliance ? "ally" : $"pact, {treaty.TurnsLeft} turn(s) left";
            return (treaty.BrokenBy != null ? kind + ", broken" : kind) + traitor;
        }
    }
}
