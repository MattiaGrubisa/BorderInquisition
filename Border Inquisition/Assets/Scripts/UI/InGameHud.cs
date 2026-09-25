using Gameplay.Managers;
using GameStates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using View;

namespace UI
{
    // Debug HUD for driving the turn loop by hand until the real game UI exists. The move, country and
    // combat panels and the income die are built in code on this canvas; the map picker opens the panels.
    public class InGameHud : MonoBehaviour
    {
        [SerializeField] private Button _endPhaseButton;
        [SerializeField] private TMP_Text _turnLabel;
        [SerializeField] private DiceFaces _diceFaces = new DiceFaces();

        private Image _incomeDie;

        public MovePanel MovePanel { get; private set; }
        public CountryPanel CountryPanel { get; private set; }
        public CombatPanel CombatPanel { get; private set; }

        private void Awake()
        {
            _endPhaseButton.onClick.AddListener(() => GameStateMachine.Instance.EndPhase());
            if (!_diceFaces.IsComplete)
                Debug.LogWarning("InGameHud is missing dice faces - assign all nine, 1 to 9 in order.", this);

            _incomeDie = CreateDie();
            MovePanel = MovePanel.Create(transform);
            CountryPanel = CountryPanel.Create(transform);
            CombatPanel = CombatPanel.Create(transform, _diceFaces);
        }

        private void OnEnable() => GameStateMachine.Instance.PhaseChanged += Refresh;

        private void OnDisable()
        {
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.PhaseChanged -= Refresh;
        }

        // Refreshed on every phase change; income is only paid at the start of a turn, so the
        // resources shown cannot go stale in between.
        private void Refresh(TurnPhase phase)
        {
            var game = GameController.Instance;
            var player = game.CurrentPlayer;
            var resources = player.Resources;

            _turnLabel.text =
                $"<color=#{PlayerPalette.HexOf(player)}>{player.Name}</color> - {phase}\n" +
                $"Food {resources.Food}   Wood {resources.Wood}   Gold {resources.Gold}   Stone {resources.Stone}\n" +
                Hint(phase);

            _incomeDie.sprite = _diceFaces.Face(game.LastIncomeRoll);
            _incomeDie.enabled = _incomeDie.sprite != null;
            UiFactory.SetLabel(_endPhaseButton, phase == TurnPhase.Move ? "End Turn" : $"End {phase}");
        }

        private static string Hint(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.Attack:
                    return "Click your country, then a red target to attack";
                case TurnPhase.Build:
                    return "Click your country to queue buildings and soldiers";
                case TurnPhase.Move:
                    return "One move: click your country, then a green neighbour";
            }
            return string.Empty;
        }

        // Top right, clear of the turn label in the top left and the combat panel in the middle.
        private Image CreateDie()
        {
            var panel = UiFactory.Panel(transform, "IncomeDie", new Vector2(1f, 1f));
            panel.anchoredPosition = new Vector2(-20f, -20f);
            UiFactory.Label(panel, "Income roll", 22f, 140f, 30f, TextAlignmentOptions.Center);
            return UiFactory.Icon(panel, null, 140f, 128f);
        }
    }
}
