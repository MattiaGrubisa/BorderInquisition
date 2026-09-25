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
    // - Build: click your country to open its build/train panel.
    // - Move: one move per turn - click your country, then a green neighbour you own.
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

            var clicked = CountryUnder(mouse.position.ReadValue());
            switch (GameStateMachine.Instance.CurrentPhase)
            {
                case TurnPhase.Attack:
                    ClickInAttack(clicked);
                    break;
                case TurnPhase.Build:
                    ClickInBuild(clicked);
                    break;
                case TurnPhase.Move:
                    ClickInMove(clicked);
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
                GameController.MinimumGarrison,
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

        #region Build

        private void ClickInBuild(Country clicked)
        {
            if (!IsOwn(clicked))
            {
                Deselect();
                _hud.CountryPanel.Close();
                return;
            }

            Highlight(clicked, null, CountryMarker.Highlight.None);
            _hud.CountryPanel.Open(clicked);
        }

        #endregion

        #region Move

        private void ClickInMove(Country clicked)
        {
            if (_moveUsed)
            {
                Debug.Log("The move for this turn is used - end the turn.");
                return;
            }

            if (_selected != null && clicked != null && Game.CanMoveArmy(_selected, clicked) && clicked != _selected)
            {
                OpenMove(_selected, clicked);
                return;
            }

            if (IsOwn(clicked) && CanSendUnits(clicked))
                Highlight(clicked, Destinations(clicked), CountryMarker.Highlight.Destination);
            else
                Deselect();
        }

        private void OpenMove(Country from, Country to)
        {
            Highlight(from, new[] { to }, CountryMarker.Highlight.Destination);
            _hud.MovePanel.Open($"Move units from {from.name} to {to.name}", from.Army,
                GameController.MinimumGarrison,
                units =>
                {
                    if (Move(from, to, units))
                        _moveUsed = true;
                    Deselect();
                },
                () => Highlight(from, Destinations(from), CountryMarker.Highlight.Destination));
        }

        private IEnumerable<Country> Destinations(Country from) =>
            Game.Map.Neighbours(from).Where(to => Game.CanMoveArmy(from, to));

        #endregion

        private static bool Move(Country from, Country to, Army units) =>
            !units.IsArmyEmpty() && Game.TryMoveArmy(from, to, units.Knights, units.Horsemen, units.Archers);

        private static bool IsOwn(Country country) => country != null && country.Owner == Game.CurrentPlayer;

        // Whether anything is left to move once the garrison stays behind.
        private static bool CanSendUnits(Country country) => country.Army.Count > GameController.MinimumGarrison;

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

        private static Country CountryUnder(Vector2 screen)
        {
            var camera = Camera.main;
            if (camera == null)
                return null;

            var hit = Physics2D.OverlapPoint(camera.ScreenToWorldPoint(screen));
            return hit != null ? hit.GetComponentInParent<Country>() : null;
        }
    }
}
