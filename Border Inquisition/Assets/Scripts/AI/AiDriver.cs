using System.Collections;
using System.Collections.Generic;
using Gameplay;
using Gameplay.Managers;
using GameStates;
using UI;
using UnityEngine;
using View;

namespace AI
{
    // Plays the turns of the AI seats. On each phase of an AI turn it lets an AiBrain decide and
    // performs the steps one at a time, pausing between them so the attacks and dice can be followed;
    // the pause menu holds it. Before a new battle both sides light up and the camera glides to it.
    // A phase ends through GameStateMachine.EndPhase, like the button.
    // GameController adds it when WorldMap has none; profiles left empty fall back to AiProfile.Preset.
    public class AiDriver : MonoBehaviour
    {
        [SerializeField] private AiProfile _easy;
        [SerializeField] private AiProfile _normal;
        [SerializeField] private AiProfile _hard;

        private readonly Dictionary<Difficulty, AiProfile> _presets = new Dictionary<Difficulty, AiProfile>();
        private const float MaxFocusWait = 2f;

        private InGameHud _hud;
        private MapCamera _mapCamera;
        private MapView _mapView;
        private Coroutine _turn;
        private TurnPhase? _pendingPhase;

        private static GameController Game => GameController.Instance;

        private void Start()
        {
            _hud = FindFirstObjectByType<InGameHud>();
            _mapCamera = FindFirstObjectByType<MapCamera>();
            _mapView = FindFirstObjectByType<MapView>();
        }

        private void OnEnable()
        {
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.PhaseChanged -= OnPhaseChanged;
        }

        private void OnDestroy()
        {
            foreach (var preset in _presets.Values)
                Destroy(preset);
        }

        // Phases change from inside EndPhase, which a running step calls, so the next phase is picked
        // up in Update rather than started from inside the old one.
        private void OnPhaseChanged(TurnPhase phase) => _pendingPhase = phase;

        private void Update()
        {
            if (_pendingPhase == null)
                return;

            var phase = _pendingPhase.Value;
            _pendingPhase = null;
            if (_turn != null)
            {
                StopCoroutine(_turn);
                _turn = null;
            }

            if (Game == null || Game.IsOver || Game.Players.Count == 0 || !Game.CurrentPlayer.IsAI)
                return;

            var profile = ProfileFor(Game.CurrentPlayer.Difficulty);
            var brain = new AiBrain(Game.CurrentPlayer, profile);
            if (phase == TurnPhase.Attack)
                _turn = StartCoroutine(PlayAttack(brain, profile));
            else if (phase == TurnPhase.BuildAndMove)
                _turn = StartCoroutine(PlayBuildAndMove(brain, profile));
        }

        // The first pause lets the income die land. The camera only moves when the battle changes.
        private IEnumerator PlayAttack(AiBrain brain, AiProfile profile)
        {
            yield return Pause(profile.ActionDelay);
            brain.AnswerOffers();

            Country lastFrom = null, lastTo = null;
            for (var i = 0; i < profile.MaxAttacksPerTurn && !Game.IsOver; i++)
            {
                if (!brain.TryChooseAttack(out var from, out var to))
                    break;

                if (from != lastFrom || to != lastTo)
                {
                    yield return Aim(from, to, profile);
                    lastFrom = from;
                    lastTo = to;
                }

                if (!Game.TryAttack(from, to, out _))
                    break;

                if (!Game.IsOver && to.Owner == from.Owner)
                {
                    var back = brain.SendBack(from, to);
                    if (!back.IsArmyEmpty())
                        Game.TryMoveArmy(to, from, back.Knights, back.Horsemen, back.Archers);
                }

                yield return Pause(profile.ActionDelay);
            }

            EndPhase();
        }

        private IEnumerator PlayBuildAndMove(AiBrain brain, AiProfile profile)
        {
            brain.PlayBuildAndMove();
            yield return Pause(profile.ActionDelay);
            EndPhase();
        }

        // Highlights both sides, glides the camera between them, and gives the player a moment.
        private IEnumerator Aim(Country from, Country to, AiProfile profile)
        {
            if (_mapView != null)
            {
                _mapView.ClearHighlights();
                _mapView.SetHighlight(from, CountryMarker.Highlight.Selected);
                _mapView.SetHighlight(to, CountryMarker.Highlight.Target);
            }

            if (_mapCamera != null)
            {
                _mapCamera.Focus(from.transform.position, to.transform.position);
                for (var waited = 0f; _mapCamera.IsFocusing && waited < MaxFocusWait; waited += Time.deltaTime)
                    yield return null;
            }

            yield return Pause(profile.AimDelay);
        }

        private IEnumerator Pause(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            while (_hud != null && _hud.PauseMenu.IsOpen)
                yield return null;
        }

        private void EndPhase()
        {
            _turn = null;
            if (!Game.IsOver)
                GameStateMachine.Instance.EndPhase();
        }

        private AiProfile ProfileFor(Difficulty difficulty)
        {
            var assigned = difficulty == Difficulty.Easy ? _easy : difficulty == Difficulty.Hard ? _hard : _normal;
            if (assigned != null)
                return assigned;

            if (!_presets.TryGetValue(difficulty, out var preset))
                _presets[difficulty] = preset = AiProfile.Preset(difficulty);
            return preset;
        }
    }
}
