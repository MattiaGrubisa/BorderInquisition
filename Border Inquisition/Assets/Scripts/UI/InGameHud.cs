using System;
using System.Linq;
using Gameplay.Managers;
using GameStates;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
    // Debug HUD for driving the turn loop by hand until the real game UI exists. The move, country,
    // combat, trade, diplomacy and turn report panels, the income die, the action bar, the country
    // tooltip and the pause menu are built in code on this canvas; the map picker opens the
    // move/country/combat panels, the action bar the trade and diplomacy ones.
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
        public TurnReportPanel TurnReportPanel { get; private set; }
        public PauseMenu PauseMenu { get; private set; }

        public event Action TurnBegan;

        private void Awake()
        {
            _endPhaseButton.onClick.AddListener(() => GameStateMachine.Instance.EndPhase());
            if (!_diceFaces.IsComplete)
                Debug.LogWarning("InGameHud is missing dice faces - assign all nine, 1 to 9 in order.", this);

            BackTurnLabel();
            _incomeDie = CreateDie();
            MovePanel = MovePanel.Create(transform);
            CountryPanel = CountryPanel.Create(transform);
            CombatPanel = CombatPanel.Create(transform, _diceFaces);
            TradePanel = TradePanel.Create(transform, UpdateStatus);
            DiplomacyPanel = DiplomacyPanel.Create(transform, UpdateStatus);
            TurnReportPanel = TurnReportPanel.Create(transform);
            CreateActionBar();
            CountryTooltip.Create(transform);
            PauseMenu = PauseMenu.Create(transform);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                PauseMenu.Toggle();
        }

        private void OnEnable() => GameStateMachine.Instance.PhaseChanged += Refresh;

        private void OnDisable()
        {
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.PhaseChanged -= Refresh;
        }

        // On every phase change. The income phase ends as soon as it is entered, so income followed by
        // attack means a new turn; its presentation starts once the panels have been reset. The market
        // only exists in the build phase.
        private void Refresh(TurnPhase phase)
        {
            var turnStarted = _phase == TurnPhase.Income && phase == TurnPhase.Attack;
            _phase = phase;

            TradePanel.Close();
            DiplomacyPanel.Close();
            TurnReportPanel.Close();
            _actionBar.gameObject.SetActive(phase != TurnPhase.Income);
            _marketButton.gameObject.SetActive(phase == TurnPhase.Build);

            UiFactory.SetLabel(_endPhaseButton, phase == TurnPhase.Move ? "End Turn" : $"End {phase}");
            UpdateStatus();

            if (turnStarted)
                BeginTurn();
        }

        // A new turn: the income die tumbles, the report opens, diplomacy opens by itself when offers
        // are waiting, and TurnBegan lets the map show the income popups.
        private void BeginTurn()
        {
            var game = GameController.Instance;
            if (game.LastIncomeRoll > 0)
                DieRoll.Roll(_incomeDie, _diceFaces, game.LastIncomeRoll, Color.white);

            TurnReportPanel.Open(game.CurrentPlayer);
            if (game.Diplomacy.OffersTo(game.CurrentPlayer).Any())
                DiplomacyPanel.Open();

            TurnBegan?.Invoke();
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
                  string.Join(", ", traitors.Select(Format.Name));

            _turnLabel.text =
                $"{Format.Name(player)} - {_phase}\n" +
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
            UiFactory.Button(row, "Menu", 140f, 56f, () => PauseMenu.Open());
        }

        // The two side panels share the left edge, so only one is open at a time.
        private void Toggle(bool wasOpen, Action open)
        {
            TradePanel.Close();
            DiplomacyPanel.Close();
            if (!wasOpen)
                open();
        }

        // The scene's turn label is moved into a panel where it stands, so it reads over the map. The
        // text wraps at a fixed width that keeps the panel clear of the combat panel in the top
        // centre, and the panel grows downwards with the lines.
        private void BackTurnLabel()
        {
            const float width = 600f;

            var label = _turnLabel.rectTransform;
            var panel = UiFactory.Panel(label.parent, "TurnLabelPanel", label.pivot);
            panel.anchorMin = label.anchorMin;
            panel.anchorMax = label.anchorMax;
            panel.anchoredPosition = label.anchoredPosition;
            panel.SetSiblingIndex(label.GetSiblingIndex());

            label.SetParent(panel, false);
            var element = label.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            _turnLabel.textWrappingMode = TextWrappingModes.Normal;
            _turnLabel.alignment = TextAlignmentOptions.TopLeft;
            _turnLabel.raycastTarget = false;
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
