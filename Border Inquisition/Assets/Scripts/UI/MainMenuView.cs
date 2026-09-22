using GameStates;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _quitButton;

        private void Awake()
        {
            _playButton.onClick.AddListener(() => GameStateMachine.Instance.Play());
            _quitButton.onClick.AddListener(() => GameStateMachine.Instance.Quit());
        }
    }
}
