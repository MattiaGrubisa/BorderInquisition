using System;
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
    // difficulty per seat) is built in code to the right. A match needs at least one human, so Start
    // waits for one. The seats are remembered in PlayerPrefs (LastLobby, one JSON value) whenever the
    // lobby is left, by Start or Back alike, and come back next time; without them the lobby starts
    // with seat 1 human and three AIs.
    public class LobbyView : MonoBehaviour
    {
        private const float RowHeight = 48f;
        private const string LastLobbyKey = "LastLobby";
        private const int DefaultSeats = 4;

        [Serializable]
        private class SavedSeat
        {
            [SerializeField] private bool _isAI;
            [SerializeField] private Difficulty _difficulty;

            public SavedSeat(Seat seat)
            {
                _isAI = seat.IsAI;
                _difficulty = seat.Difficulty;
            }

            // An unknown difficulty (an older or edited value) falls back to Normal.
            public Seat ToSeat() =>
                new Seat(_isAI, Enum.IsDefined(typeof(Difficulty), _difficulty) ? _difficulty : Difficulty.Normal);
        }

        [Serializable]
        private class SavedLobby
        {
            [SerializeField] private List<SavedSeat> _seats = new List<SavedSeat>();

            public List<SavedSeat> Seats => _seats;
        }

        [SerializeField] private Button _removePlayerButton;
        [SerializeField] private Button _addPlayerButton;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _playerCountLabel;

        private readonly List<Seat> _seats = new List<Seat>();
        private RectTransform _seatRows;

        private void Awake()
        {
            ThemedText.ThemeAll(transform);
            if (!LoadSeats())
            {
                _seats.Add(new Seat(false));
                while (_seats.Count < DefaultSeats)
                    _seats.Add(new Seat(true));
            }

            var panel = UiFactory.Panel(transform, "Seats", new Vector2(1f, 0.5f));
            panel.anchoredPosition = new Vector2(-60f, 0f);
            _seatRows = UiFactory.Column(panel, "SeatRows");

            UiFactory.ClickSound(_removePlayerButton);
            UiFactory.ClickSound(_addPlayerButton);
            UiFactory.ClickSound(_startButton);
            UiFactory.ClickSound(_backButton);
            _removePlayerButton.onClick.AddListener(() => ChangePlayerCount(-1));
            _addPlayerButton.onClick.AddListener(() => ChangePlayerCount(1));
            _startButton.onClick.AddListener(() => GameStateMachine.Instance.StartMatch(new MatchSettings(_seats)));
            _backButton.onClick.AddListener(() => GameStateMachine.Instance.LeaveLobby());
            Refresh();
        }

        // The lobby scene unloads whichever way it is left.
        private void OnDestroy() => SaveSeats();

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

        // False when nothing usable is stored; a stored seat count outside the limits is cut to them.
        private bool LoadSeats()
        {
            var json = PlayerPrefs.GetString(LastLobbyKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return false;

            SavedLobby saved;
            try
            {
                saved = JsonUtility.FromJson<SavedLobby>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (saved?.Seats == null || saved.Seats.Count < MatchSettings.MinPlayers)
                return false;

            _seats.AddRange(saved.Seats.Take(MatchSettings.MaxPlayers).Select(seat => seat.ToSeat()));
            return true;
        }

        private void SaveSeats()
        {
            var saved = new SavedLobby();
            saved.Seats.AddRange(_seats.Select(seat => new SavedSeat(seat)));
            PlayerPrefs.SetString(LastLobbyKey, JsonUtility.ToJson(saved));
            PlayerPrefs.Save();
        }

        private static Difficulty NextDifficulty(Difficulty difficulty) =>
            difficulty == Difficulty.Hard ? Difficulty.Easy : difficulty + 1;
    }
}
