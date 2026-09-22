using GameStates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _winnerLabel;
        [SerializeField] private Button _rematchButton;
        [SerializeField] private Button _mainMenuButton;

        private void Awake()
        {
            _rematchButton.onClick.AddListener(() => GameStateMachine.Instance.Rematch());
            _mainMenuButton.onClick.AddListener(() => GameStateMachine.Instance.ReturnToMainMenu());

            var winner = GameStateMachine.Instance.Winner;
            _winnerLabel.text = winner != null ? $"{winner.Name} wins!" : "Game over";
        }
    }
}
