using Gameplay;
using GameStates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class LobbyView : MonoBehaviour
    {
        [SerializeField] private Button _removePlayerButton;
        [SerializeField] private Button _addPlayerButton;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _playerCountLabel;

        private int _playerCount = MatchSettings.MinPlayers;

        private void Awake()
        {
            _removePlayerButton.onClick.AddListener(() => ChangePlayerCount(-1));
            _addPlayerButton.onClick.AddListener(() => ChangePlayerCount(1));
            _startButton.onClick.AddListener(() => GameStateMachine.Instance.StartMatch(_playerCount));
            _backButton.onClick.AddListener(() => GameStateMachine.Instance.LeaveLobby());
            Refresh();
        }

        private void ChangePlayerCount(int delta)
        {
            _playerCount = Mathf.Clamp(_playerCount + delta, MatchSettings.MinPlayers, MatchSettings.MaxPlayers);
            Refresh();
        }

        private void Refresh()
        {
            _playerCountLabel.text = $"Players: {_playerCount}";
            _removePlayerButton.interactable = _playerCount > MatchSettings.MinPlayers;
            _addPlayerButton.interactable = _playerCount < MatchSettings.MaxPlayers;
        }
    }
}
