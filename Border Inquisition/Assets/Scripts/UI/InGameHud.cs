using Gameplay.Managers;
using GameStates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Debug HUD for driving the turn loop by hand until the real game UI exists.
    public class InGameHud : MonoBehaviour
    {
        [SerializeField] private Button _endPhaseButton;
        [SerializeField] private TMP_Text _turnLabel;

        private void Awake()
        {
            _endPhaseButton.onClick.AddListener(() => GameStateMachine.Instance.EndPhase());
        }

        private void OnEnable() => GameStateMachine.Instance.PhaseChanged += Refresh;

        private void OnDisable()
        {
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.PhaseChanged -= Refresh;
        }

        private void Refresh(TurnPhase phase) =>
            _turnLabel.text = $"{GameController.Instance.CurrentPlayer.Name} - {phase}";
    }
}
