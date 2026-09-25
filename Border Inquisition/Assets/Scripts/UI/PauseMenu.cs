using GameStates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Esc or the Menu button: resume, or abandon the match for the main menu (asked twice - nothing
    // of the match is kept). It dims and blocks the whole screen, above every panel; the game itself
    // has nothing running on a clock, so nothing needs stopping.
    public class PauseMenu : MonoBehaviour
    {
        public const int SortingOrder = 200;
        private const float Width = 420f;

        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);

        private RectTransform _root;
        private Button _leaveButton;
        private bool _confirmingLeave;

        public bool IsOpen => gameObject.activeSelf;

        public static PauseMenu Create(Transform canvas)
        {
            var root = UiFactory.Overlay(canvas, "PauseMenu", SortingOrder, Dim);
            var menu = root.gameObject.AddComponent<PauseMenu>();
            menu._root = root;
            menu.Build(root);
            root.gameObject.SetActive(false);
            return menu;
        }

        private void Build(Transform root)
        {
            var panel = UiFactory.Panel(root, "Panel", new Vector2(0.5f, 0.5f));
            UiFactory.Label(panel, "Paused", 36f, Width, 50f, TextAlignmentOptions.Center);
            UiFactory.Button(panel, "Resume", Width, 56f, Close);
            _leaveButton = UiFactory.Button(panel, "Leave match", Width, 56f, Leave);
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            _confirmingLeave = false;
            UiFactory.SetLabel(_leaveButton, "Leave match");
            gameObject.SetActive(true);
            UiFactory.Raise(_root, SortingOrder);
        }

        public void Close() => gameObject.SetActive(false);

        private void Leave()
        {
            if (!_confirmingLeave)
            {
                _confirmingLeave = true;
                UiFactory.SetLabel(_leaveButton, "Sure? The match is lost");
                return;
            }

            GameStateMachine.Instance.LeaveMatch();
        }
    }
}
