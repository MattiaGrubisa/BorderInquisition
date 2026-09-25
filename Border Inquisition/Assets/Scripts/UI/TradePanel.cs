using System;
using Gameplay;
using Gameplay.Managers;
using TMPro;
using UnityEngine;

namespace UI
{
    // Build-phase bank trade for the current player: pick what to give, what to get and how much.
    // The ratio and the trade itself come from GameController.Market; the HUD only opens this panel
    // in the build phase.
    public class TradePanel : MonoBehaviour
    {
        private const float RowHeight = 42f;
        private const float Width = 560f;

        private static readonly ResourceType[] Types =
            { ResourceType.Food, ResourceType.Wood, ResourceType.Gold, ResourceType.Stone };

        private TMP_Text _title;
        private TMP_Text _resources;
        private Transform _giveRow;
        private Transform _getRow;
        private TMP_Text _amount;
        private TMP_Text _summary;

        private ResourceType _give = ResourceType.Food;
        private ResourceType _get = ResourceType.Wood;
        private int _count = 1;
        private Action _onTraded;

        public bool IsOpen => gameObject.activeSelf;

        private static GameController Game => GameController.Instance;

        public static TradePanel Create(Transform canvas, Action onTraded)
        {
            var root = UiFactory.Panel(canvas, "TradePanel", new Vector2(0f, 0.5f));
            root.anchoredPosition = new Vector2(20f, 0f);

            var panel = root.gameObject.AddComponent<TradePanel>();
            panel._onTraded = onTraded;
            panel.Build(root);
            root.gameObject.SetActive(false);
            return panel;
        }

        private void Build(Transform root)
        {
            _title = UiFactory.Label(root, "-", 30f, Width, 40f);
            _resources = UiFactory.Label(root, "-", 22f, Width, 30f);

            UiFactory.Label(root, "Give", 24f, Width, 32f);
            _giveRow = UiFactory.Row(root, "Give");
            UiFactory.Label(root, "Get", 24f, Width, 32f);
            _getRow = UiFactory.Row(root, "Get");

            var amountRow = UiFactory.Row(root, "Amount");
            UiFactory.Label(amountRow, "Amount", 24f, 200f, RowHeight);
            UiFactory.Button(amountRow, "-", RowHeight, RowHeight, () => ChangeCount(-1));
            _amount = UiFactory.Label(amountRow, "1", 24f, 80f, RowHeight, TextAlignmentOptions.Center);
            UiFactory.Button(amountRow, "+", RowHeight, RowHeight, () => ChangeCount(1));

            _summary = UiFactory.Label(root, "-", 22f, Width, 30f);

            var buttons = UiFactory.Row(root, "Buttons", 20f);
            UiFactory.Button(buttons, "Trade", 270f, 52f, Trade);
            UiFactory.Button(buttons, "Close", 270f, 52f, Close);
        }

        public void Open()
        {
            _count = 1;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        private void ChangeCount(int delta)
        {
            _count = Mathf.Max(1, _count + delta);
            Refresh();
        }

        private void Trade()
        {
            if (Game.Market.TryTrade(Game.CurrentPlayer, _give, _get, _count))
                _onTraded?.Invoke();
            Refresh();
        }

        private void Refresh()
        {
            var player = Game.CurrentPlayer;
            var ratio = Game.Market.RatioFor(player);
            var cost = ratio * _count;

            _title.text = $"Market - {ratio}:1";
            _resources.text = $"You have {player.Resources}";
            FillChoices(_giveRow, _give, type => _give = type);
            FillChoices(_getRow, _get, type => _get = type);
            _amount.text = _count.ToString();

            _summary.text = _give == _get
                ? "Pick two different resources"
                : $"Pay {cost} {_give} for {_count} {_get}" +
                  (player.Resources.Get(_give) < cost ? " - not enough" : string.Empty);
        }

        private void FillChoices(Transform row, ResourceType selected, Action<ResourceType> select)
        {
            UiFactory.Clear(row);
            foreach (var type in Types)
            {
                var choice = type;
                var label = choice == selected ? $"[{choice}]" : choice.ToString();
                UiFactory.Button(row, label, 130f, RowHeight, () =>
                {
                    select(choice);
                    Refresh();
                });
            }
        }
    }
}
