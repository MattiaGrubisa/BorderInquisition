using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay;
using TMPro;
using UnityEngine;

namespace UI
{
    // Opens after a conquest of a country with buildings: keep or raze each one. A kept building works
    // for the conqueror from now on; a razed one is gone and pays its loot at once. Everything starts
    // as kept. Like MovePanel it only collects the choice - whoever opened it razes in the callback.
    public class RazePanel : MonoBehaviour
    {
        private const float RowHeight = 42f;
        private const float Width = 760f;
        private const float ChoiceWidth = 150f;

        private TMP_Text _title;
        private Transform _rows;
        private TMP_Text _summary;

        private readonly List<Building> _buildings = new List<Building>();
        private readonly HashSet<Building> _razed = new HashSet<Building>();
        private Func<Building, GameResources> _loot;
        private Action<IReadOnlyList<Building>> _onConfirm;

        public bool IsOpen => gameObject.activeSelf;

        public static RazePanel Create(Transform canvas)
        {
            var root = UiFactory.Panel(canvas, "RazePanel", new Vector2(0.5f, 0.5f));
            var panel = root.gameObject.AddComponent<RazePanel>();
            panel.Build(root);
            root.gameObject.SetActive(false);
            return panel;
        }

        private void Build(Transform root)
        {
            _title = UiFactory.Label(root, "-", 28f, Width, 40f, TextAlignmentOptions.Center);
            UiFactory.Paragraph(root, "Keep a building and it works for you; raze it and take the loot now.",
                22f, Width);
            _rows = UiFactory.Column(root, "Buildings");
            _summary = UiFactory.Label(root, "-", 22f, Width, 30f);

            var buttons = UiFactory.Row(root, "Buttons", 20f);
            UiFactory.Button(buttons, "Keep all", 240f, 52f, () => SetAll(false));
            UiFactory.Button(buttons, "Raze all", 240f, 52f, () => SetAll(true));
            UiFactory.Button(buttons, "Done", 240f, 52f, Confirm);
        }

        public void Open(Country country, IEnumerable<Building> buildings, Func<Building, GameResources> loot,
            Action<IReadOnlyList<Building>> onConfirm)
        {
            _title.text = $"{country.name} taken - its buildings";
            _buildings.Clear();
            _buildings.AddRange(buildings);
            _razed.Clear();
            _loot = loot;
            _onConfirm = onConfirm;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        public void Close()
        {
            _onConfirm = null;
            gameObject.SetActive(false);
        }

        private void SetAll(bool raze)
        {
            _razed.Clear();
            if (raze)
                _razed.UnionWith(_buildings);
            Refresh();
        }

        private void Toggle(Building building)
        {
            if (!_razed.Remove(building))
                _razed.Add(building);
            Refresh();
        }

        private void Refresh()
        {
            UiFactory.Clear(_rows);
            foreach (var building in _buildings)
            {
                var row = UiFactory.Row(_rows, building.DisplayName);
                UiFactory.Label(row, $"<b>{building.DisplayName}</b>  <color=#9A9A9A>{Format.Effects(building)}</color>",
                    22f, Width - 2 * ChoiceWidth - 16f, RowHeight);
                UiFactory.Label(row, "loot " + Format.Resources(_loot(building)), 20f, ChoiceWidth, RowHeight);
                var target = building;
                UiFactory.Button(row, _razed.Contains(building) ? "Raze" : "Keep", ChoiceWidth, RowHeight,
                    () => Toggle(target));
            }

            var total = _razed.Aggregate(default(GameResources), (sum, building) => sum + _loot(building));
            _summary.text = _razed.Count == 0
                ? "Keeping every building"
                : $"Razing {_razed.Count} - you take {Format.Resources(total)}";
        }

        private void Confirm()
        {
            var onConfirm = _onConfirm;
            var razed = _buildings.Where(_razed.Contains).ToList();
            Close();
            onConfirm?.Invoke(razed);
        }
    }
}
