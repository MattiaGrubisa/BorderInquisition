using System.Linq;
using Gameplay.Managers;
using GameStates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using View;

namespace UI
{
    // Debug HUD for driving the turn loop by hand until the real game UI exists. The move, country,
    // combat, trade and diplomacy panels, the income die and the action bar are built in code on this
    // canvas; the map picker opens the move/country/combat panels, the action bar the other two.
    public class InGameHud : MonoBehaviour
    {
        [SerializeField] private Button _endPhaseButton;
        [SerializeField] private TMP_Text _turnLabel;
        [SerializeField] private DiceFaces _diceFaces = new DiceFaces();

        private Image _incomeDie;
        private RectTransform _actionBar;
        private Button _marketButton;
        private TurnPhase _phase;

        public MovePanel MovePanel { get; private set; }
        public CountryPanel CountryPanel { get; private set; }
        public CombatPanel CombatPanel { get; private set; }
        public TradePanel TradePanel { get; private set; }
        public DiplomacyPanel DiplomacyPanel { get; private set; }

        private void Awake()
        {
            _endPhaseButton.onClick.AddListener(() => GameStateMachine.Instance.EndPhase());
            if (!_diceFaces.IsComplete)
                Debug.LogWarning("InGameHud is missing dice faces - assign all nine, 1 to 9 in order.", this);

            _incomeDie = CreateDie();
            MovePanel = MovePanel.Create(transform);
            CountryPanel = CountryPanel.Create(transform);
            CombatPanel = CombatPanel.Create(transform, _diceFaces);
            TradePanel = TradePanel.Create(transform, UpdateStatus);
            DiplomacyPanel = DiplomacyPanel.Create(transform, UpdateStatus);
            CreateActionBar();
        }

        private void OnEnable() => GameStateMachine.Instance.PhaseChanged += Refresh;

        private void OnDisable()
        {
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.PhaseChanged -= Refresh;
        }

        // On every phase change. Diplomacy opens by itself at the start of a turn with offers waiting,
        // and the market only exists in the build phase.
        private void Refresh(TurnPhase phase)
        {
            _phase = phase;
            var game = GameController.Instance;

            TradePanel.Close();
            DiplomacyPanel.Close();
            _actionBar.gameObject.SetActive(phase != TurnPhase.Income);
            _marketButton.gameObject.SetActive(phase == TurnPhase.Build);

            // Income is rolled on entering the income phase, which ends straight away - so the die
            // tumbles from here and is left alone on the phases that follow.
            if (phase == TurnPhase.Income && game.LastIncomeRoll > 0)
                DieRoll.Roll(_incomeDie, _diceFaces, game.LastIncomeRoll, Color.white);

            if (phase == TurnPhase.Attack && game.Diplomacy.OffersTo(game.CurrentPlayer).Any())
                DiplomacyPanel.Open();

            UiFactory.SetLabel(_endPhaseButton, phase == TurnPhase.Move ? "End Turn" : $"End {phase}");
            UpdateStatus();
        }

        // Also called by the trade and diplomacy panels, since resources and traitor marks change
        // mid-phase.
        private void UpdateStatus()
        {
            var game = GameController.Instance;
            var player = game.CurrentPlayer;
            var resources = player.Resources;

            var traitors = game.Diplomacy.Traitors.ToList();
            var traitorLine = traitors.Count == 0
                ? string.Empty
                : "\n<color=#FF5040>Traitors: </color>" +
                  string.Join(", ", traitors.Select(t => $"<color=#{PlayerPalette.HexOf(t)}>{t.Name}</color>"));

            _turnLabel.text =
                $"<color=#{PlayerPalette.HexOf(player)}>{player.Name}</color> - {_phase}\n" +
                $"Food {resources.Food}   Wood {resources.Wood}   Gold {resources.Gold}   Stone {resources.Stone}\n" +
                Hint(_phase) + traitorLine;
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

        // Bottom left; the two side panels open above it on the left edge.
        private void CreateActionBar()
        {
            _actionBar = UiFactory.Panel(transform, "ActionBar", new Vector2(0f, 0f));
            _actionBar.anchoredPosition = new Vector2(20f, 20f);

            var row = UiFactory.Row(_actionBar, "Buttons", 12f);
            UiFactory.Button(row, "Diplomacy", 200f, 56f, () => Toggle(DiplomacyPanel.IsOpen, DiplomacyPanel.Open));
            _marketButton = UiFactory.Button(row, "Market", 200f, 56f, () => Toggle(TradePanel.IsOpen, TradePanel.Open));
        }

        // The two side panels share the left edge, so only one is open at a time.
        private void Toggle(bool wasOpen, System.Action open)
        {
            TradePanel.Close();
            DiplomacyPanel.Close();
            if (!wasOpen)
                open();
        }

        // Top right, clear of the turn label in the top left and the combat panel in the middle.
        private Image CreateDie()
        {
            var panel = UiFactory.Panel(transform, "IncomeDie", new Vector2(1f, 1f));
            panel.anchoredPosition = new Vector2(-20f, -20f);
            UiFactory.Label(panel, "Income roll", 22f, 140f, 30f, TextAlignmentOptions.Center);
            var die = UiFactory.Icon(panel, null, 140f, 128f);
            die.enabled = false;
            return die;
        }
    }
}
