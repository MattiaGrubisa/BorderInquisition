using System.Collections.Generic;
using System.Linq;
using Gameplay;
using GameStates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // The scene holds the player count, +/-, Start and Back; the seat list (Human/AI and the AI's
    // difficulty per seat) is built in code to the right. Seat 1 starts human, the rest AI; a match
    // needs at least one human, so Start waits for one.
    public class LobbyView : MonoBehaviour
    {
        private const float RowHeight = 48f;

        [SerializeField] private Button _removePlayerButton;
        [SerializeField] private Button _addPlayerButton;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _playerCountLabel;

        private readonly List<Seat> _seats = new List<Seat>();
        private RectTransform _seatRows;

        private void Awake()
        {
            _seats.Add(new Seat(false));
            while (_seats.Count < MatchSettings.MinPlayers)
                _seats.Add(new Seat(true));

            var panel = UiFactory.Panel(transform, "Seats", new Vector2(1f, 0.5f));
            panel.anchoredPosition = new Vector2(-60f, 0f);
            _seatRows = UiFactory.Column(panel, "SeatRows");

            _removePlayerButton.onClick.AddListener(() => ChangePlayerCount(-1));
            _addPlayerButton.onClick.AddListener(() => ChangePlayerCount(1));
            _startButton.onClick.AddListener(() => GameStateMachine.Instance.StartMatch(new MatchSettings(_seats)));
            _backButton.onClick.AddListener(() => GameStateMachine.Instance.LeaveLobby());
            Refresh();
        }

        private void ChangePlayerCount(int delta)
        {
            if (delta > 0 && _seats.Count < MatchSettings.MaxPlayers)
                _seats.Add(new Seat(true));
            else if (delta < 0 && _seats.Count > MatchSettings.MinPlayers)
                _seats.RemoveAt(_seats.Count - 1);
            Refresh();
        }

        private void Refresh()
        {
            _playerCountLabel.text = $"Players: {_seats.Count}";
            _removePlayerButton.interactable = _seats.Count > MatchSettings.MinPlayers;
            _addPlayerButton.interactable = _seats.Count < MatchSettings.MaxPlayers;
            _startButton.interactable = _seats.Any(seat => !seat.IsAI);

            UiFactory.Clear(_seatRows);
            for (var i = 0; i < _seats.Count; i++)
                SeatRow(i);
        }

        private void SeatRow(int index)
        {
            var seat = _seats[index];
            var row = UiFactory.Row(_seatRows, $"Seat{index + 1}");
            UiFactory.Label(row, $"Player {index + 1}", 26f, 160f, RowHeight);
            UiFactory.Button(row, seat.IsAI ? "AI" : "Human", 140f, RowHeight, () =>
            {
                _seats[index] = new Seat(!seat.IsAI, seat.Difficulty);
                Refresh();
            });

            if (seat.IsAI)
                UiFactory.Button(row, seat.Difficulty.ToString(), 140f, RowHeight, () =>
                {
                    _seats[index] = new Seat(true, NextDifficulty(seat.Difficulty));
                    Refresh();
                });
            else
                UiFactory.Spacer(row, 140f, RowHeight);
        }

        private static Difficulty NextDifficulty(Difficulty difficulty) =>
            difficulty == Difficulty.Hard ? Difficulty.Easy : difficulty + 1;
    }
}
