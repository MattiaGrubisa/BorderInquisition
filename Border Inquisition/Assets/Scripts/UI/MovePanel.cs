using System;
using Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Picks how many of each unit type leave a country. It only collects the numbers; whoever opened it
    // performs the move in the confirm callback, so the panel knows nothing about the rules.
    public class MovePanel : MonoBehaviour
    {
        private const float RowHeight = 44f;

        private static readonly SoldierType[] Types = { SoldierType.Knight, SoldierType.Horseman, SoldierType.Archer };

        private TMP_Text _title;
        private readonly TMP_Text[] _counts = new TMP_Text[Types.Length];
        private readonly int[] _chosen = new int[Types.Length];
        private readonly int[] _available = new int[Types.Length];
        private int _mustStay;

        private Action<Army> _onConfirm;
        private Action _onCancel;

        public bool IsOpen => gameObject.activeSelf;

        public static MovePanel Create(Transform canvas)
        {
            var root = UiFactory.Panel(canvas, "MovePanel", new Vector2(0.5f, 0.5f));
            var panel = root.gameObject.AddComponent<MovePanel>();
            panel.Build(root);
            root.gameObject.SetActive(false);
            return panel;
        }

        private void Build(Transform root)
        {
            _title = UiFactory.Label(root, "-", 28f, 520f, 40f, TextAlignmentOptions.Center);

            for (var i = 0; i < Types.Length; i++)
            {
                var index = i;
                var row = UiFactory.Row(root, Types[i].ToString());
                UiFactory.Label(row, Types[i].ToString(), 26f, 200f, RowHeight);
                UiFactory.Button(row, "-", RowHeight, RowHeight, () => Change(index, -1));
                _counts[i] = UiFactory.Label(row, "0 / 0", 26f, 140f, RowHeight, TextAlignmentOptions.Center);
                UiFactory.Button(row, "+", RowHeight, RowHeight, () => Change(index, 1));
                UiFactory.Button(row, "All", 80f, RowHeight, () => Change(index, int.MaxValue));
            }

            var buttons = UiFactory.Row(root, "Buttons", 20f);
            UiFactory.Button(buttons, "Confirm", 250f, 56f, Confirm);
            UiFactory.Button(buttons, "Cancel", 250f, 56f, Cancel);
        }

        // Cancel means no units move; onCancel still runs, so the caller can carry on either way.
        // mustStay units of any type are held back from the choice.
        public void Open(string title, Army source, int mustStay, Action<Army> onConfirm, Action onCancel)
        {
            _title.text = title;
            _available[0] = source.Knights;
            _available[1] = source.Horsemen;
            _available[2] = source.Archers;
            _mustStay = mustStay;
            Array.Clear(_chosen, 0, _chosen.Length);

            _onConfirm = onConfirm;
            _onCancel = onCancel;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        public void Close()
        {
            _onConfirm = null;
            _onCancel = null;
            gameObject.SetActive(false);
        }

        private void Change(int index, int delta)
        {
            var next = delta == int.MaxValue ? _available[index] : _chosen[index] + delta;
            var room = Sum(_available) - _mustStay - (Sum(_chosen) - _chosen[index]);
            _chosen[index] = Mathf.Clamp(next, 0, Mathf.Max(0, Mathf.Min(_available[index], room)));
            Refresh();
        }

        private static int Sum(int[] counts) => counts[0] + counts[1] + counts[2];

        private void Refresh()
        {
            for (var i = 0; i < Types.Length; i++)
                _counts[i].text = $"{_chosen[i]} / {_available[i]}";
        }

        private void Confirm()
        {
            var onConfirm = _onConfirm;
            var chosen = new Army(_chosen[0], _chosen[1], _chosen[2]);
            Close();
            onConfirm?.Invoke(chosen);
        }

        private void Cancel()
        {
            var onCancel = _onCancel;
            Close();
            onCancel?.Invoke();
        }
    }
}
