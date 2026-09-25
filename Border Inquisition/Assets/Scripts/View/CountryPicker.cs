using System.Collections.Generic;
using System.Linq;
using Gameplay;
using Gameplay.Managers;
using GameStates;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace View
{
    // Map input for the current player, and the only place the turn rules that GameController leaves
    // to the view are enforced (GameController itself is phase-blind):
    // - Attack: click your country, then a red target. After a conquest the move panel offers to send
    //   units back between exactly those two countries; the selection then follows the army.
    // - Build & Move: click your country to open its build/train panel; while the turn's one move is
    //   unused, its own neighbours light up green and a click on one opens the move panel.
    // Left click only; right and middle drag stay with MapCamera.
    [RequireComponent(typeof(MapView))]
    public class CountryPicker : MonoBehaviour
    {
        private MapView _view;
        private InGameHud _hud;
        private Country _selected;
        private bool _moveUsed;

        private static GameController Game => GameController.Instance;

        private void Awake() => _view = GetComponent<MapView>();

        private void Start() => _hud = FindFirstObjectByType<InGameHud>();

        private void OnEnable() => GameStateMachine.Instance.PhaseChanged += OnPhaseChanged;

        private void OnDisable()
        {
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.PhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(TurnPhase phase)
        {
            _moveUsed = false;
            if (_hud != null)
            {
                _hud.MovePanel.Close();
                _hud.CountryPanel.Close();
                _hud.CombatPanel.Hide();
            }
            Deselect();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || _hud == null || _hud.MovePanel.IsOpen)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var clicked = MapView.CountryAt(mouse.position.ReadValue());
            switch (GameStateMachine.Instance.CurrentPhase)
            {
                case TurnPhase.Attack:
                    ClickInAttack(clicked);
                    break;
                case TurnPhase.BuildAndMove:
                    ClickInBuildAndMove(clicked);
                    break;
            }
        }

        #region Attack

        private void ClickInAttack(Country clicked)
        {
            if (_selected != null && clicked != null && Game.CanAttack(_selected, clicked))
            {
                Attack(_selected, clicked);
                return;
            }

            SelectForAttack(IsOwn(clicked) ? clicked : null);
        }

        private void Attack(Country from, Country to)
        {
            var defender = to.Owner;
            if (!Game.TryAttack(from, to, out var result))
                return;

            var conquered = to.Owner == from.Owner;
            _hud.CombatPanel.Show(from, to, from.Owner, defender, result);

            if (Game.Winner != null)
                return;

            if (!conquered)
            {
                SelectForAttack(from);
                return;
            }

            if (!CanSendUnits(to))
            {
                AfterConquest(from, to);
                return;
            }

            Highlight(to, new[] { from }, CountryMarker.Highlight.Destination);
            _hud.MovePanel.Open($"{to.name} taken - send units back to {from.name}?", to.Army,
                Game.Rules.MinimumGarrison,
                units =>
                {
                    Move(to, from, units);
                    AfterConquest(from, to);
                },
                () => AfterConquest(from, to));
        }

        private void AfterConquest(Country from, Country to) => SelectForAttack(to.IsArmyEmpty ? from : to);

        private void SelectForAttack(Country country) =>
            Highlight(country, country != null ? Game.AttackTargets(country) : null, CountryMarker.Highlight.Target);

        #endregion

        #region Build & Move

        private void ClickInBuildAndMove(Country clicked)
        {
            if (clicked != null && clicked != _selected && Destinations(_selected).Contains(clicked))
            {
                OpenMove(_selected, clicked);
                return;
            }

            if (!IsOwn(clicked))
            {
                Deselect();
                _hud.CountryPanel.Close();
                return;
            }

            SelectForBuildAndMove(clicked);
        }

        private void SelectForBuildAndMove(Country country)
        {
            Highlight(country, Destinations(country), CountryMarker.Highlight.Destination);
            _hud.CountryPanel.Open(country);
        }

        // Either way the selection returns to the country the units left from, with its panel.
        private void OpenMove(Country from, Country to)
        {
            _hud.CountryPanel.Close();
            Highlight(from, new[] { to }, CountryMarker.Highlight.Destination);
            _hud.MovePanel.Open($"Move units from {from.name} to {to.name}", from.Army,
                Game.Rules.MinimumGarrison,
                units =>
                {
                    if (Move(from, to, units))
                        _moveUsed = true;
                    SelectForBuildAndMove(from);
                },
                () => SelectForBuildAndMove(from));
        }

        // None once the turn's move is used, or when the garrison is all the country has.
        private IEnumerable<Country> Destinations(Country from) =>
            from == null || _moveUsed || !CanSendUnits(from)
                ? Enumerable.Empty<Country>()
                : Game.Map.Neighbours(from).Where(to => Game.CanMoveArmy(from, to));

        #endregion

        private static bool Move(Country from, Country to, Army units) =>
            !units.IsArmyEmpty() && Game.TryMoveArmy(from, to, units.Knights, units.Horsemen, units.Archers);

        private static bool IsOwn(Country country) => country != null && country.Owner == Game.CurrentPlayer;

        // Whether anything is left to move once the garrison stays behind.
        private static bool CanSendUnits(Country country) => country.Army.Count > Game.Rules.MinimumGarrison;

        private void Deselect() => Highlight(null, null, CountryMarker.Highlight.None);

        private void Highlight(Country selected, IEnumerable<Country> others, CountryMarker.Highlight kind)
        {
            _selected = selected;
            _view.ClearHighlights();

            if (_selected == null)
                return;

            _view.SetHighlight(_selected, CountryMarker.Highlight.Selected);
            if (others == null)
                return;

            foreach (var other in others.ToList())
                _view.SetHighlight(other, kind);
        }
    }
}
