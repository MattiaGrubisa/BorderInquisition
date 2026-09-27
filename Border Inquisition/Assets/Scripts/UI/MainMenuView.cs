using GameStates;
using Save;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _quitButton;
        // Optional: without one, a copy of the play button is placed above it, one button's step up.
        [SerializeField] private Button _continueButton;

        private void Awake()
        {
            if (_continueButton == null)
                _continueButton = CreateContinueButton();

            UiFactory.ClickSound(_playButton);
            UiFactory.ClickSound(_continueButton);
            UiFactory.ClickSound(_quitButton);
            _playButton.onClick.AddListener(() => GameStateMachine.Instance.Play());
            _continueButton.onClick.AddListener(() => GameStateMachine.Instance.Continue());
            _quitButton.onClick.AddListener(() => GameStateMachine.Instance.Quit());

            _continueButton.gameObject.SetActive(SaveSystem.HasSave);
        }

        private Button CreateContinueButton()
        {
            var button = Instantiate(_playButton, _playButton.transform.parent);
            button.name = "ContinueButton";
            UiFactory.SetLabel(button, "Continue");

            var play = (RectTransform)_playButton.transform;
            var quit = (RectTransform)_quitButton.transform;
            ((RectTransform)button.transform).anchoredPosition =
                play.anchoredPosition + (play.anchoredPosition - quit.anchoredPosition);
            return button;
        }
    }
}
