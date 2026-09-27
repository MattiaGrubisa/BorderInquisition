using System;
using System.Collections;
using System.Linq;
using Audio;
using Diplomacy;
using Gameplay;
using Gameplay.Managers;
using UI;
using UnityEngine;

namespace View
{
    // Turns what happens in the match into sounds, heard from the viewer's side and through their fog
    // of war (GameController.Viewer):
    // - a battle they can see: the dice (UI.DieRoll), then once they land the conquest, or whether the
    //   viewer's side came out ahead when it fought;
    // - moves and razes they can see, treaties that involve them, and every betrayal (traitors are
    //   visible to everyone);
    // - a new turn: its start on a human turn, then, as the income die lands, the income it paid the
    //   viewer, what their queues delivered and offers waiting for them.
    // Silent while a simulation runs. MapView adds it, like the picker.
    public class SoundCues : MonoBehaviour
    {
        private const float DeliveryDelay = 0.3f;

        private InGameHud _hud;

        private static GameController Game => GameController.Instance;
        private static bool Muted => Game == null || Game.Simulating;

        private void Start()
        {
            Game.AttackResolved += OnAttackResolved;
            Game.ArmyMoved += OnArmyMoved;
            Game.BuildingRazed += OnBuildingRazed;
            Game.Diplomacy.TreatySigned += OnTreatySigned;
            Game.Diplomacy.TreatyBroken += OnTreatyBroken;

            _hud = FindFirstObjectByType<InGameHud>();
            if (_hud != null)
                _hud.TurnBegan += OnTurnBegan;
        }

        private void OnDestroy()
        {
            if (_hud != null)
                _hud.TurnBegan -= OnTurnBegan;
            if (Game == null)
                return;

            Game.AttackResolved -= OnAttackResolved;
            Game.ArmyMoved -= OnArmyMoved;
            Game.BuildingRazed -= OnBuildingRazed;
            Game.Diplomacy.TreatySigned -= OnTreatySigned;
            Game.Diplomacy.TreatyBroken -= OnTreatyBroken;
        }

        private void OnAttackResolved(Country from, Country to, Player attacker, Player defender,
            Combat.CombatResult result)
        {
            var viewer = Game.Viewer;
            if (Muted || !Game.Fog.SeesBattle(viewer, from, to, defender))
                return;

            var sound = Outcome(viewer, to, attacker, defender, result);
            if (sound != null)
                After(DieRoll.Duration, () => AudioManager.Play(sound.Value));
        }

        // Nothing beyond the dice for a battle the viewer's side had no part in, or an even round.
        private static Sound? Outcome(Player viewer, Country to, Player attacker, Player defender,
            Combat.CombatResult result)
        {
            if (to.Owner == attacker)
                return Sound.Conquest;

            var defenderLosses = result.AttackerWins.Count(win => win);
            var attackerLosses = result.AttackerWins.Length - defenderLosses;
            var attackerAhead = defenderLosses > attackerLosses;
            var diplomacy = Game.Diplomacy;

            if (defenderLosses == attackerLosses)
                return null;
            if (diplomacy.AreAllied(viewer, attacker))
                return attackerAhead ? Sound.BattleWon : Sound.BattleLost;
            if (diplomacy.AreAllied(viewer, defender))
                return attackerAhead ? Sound.BattleLost : Sound.BattleWon;
            return null;
        }

        private void OnArmyMoved(Country from, Country to, Army units)
        {
            if (!Muted && (Game.Fog.IsVisible(Game.Viewer, from) || Game.Fog.IsVisible(Game.Viewer, to)))
                AudioManager.Play(Sound.ArmyMove);
        }

        private void OnBuildingRazed(Country country, Building building)
        {
            if (!Muted && Game.Fog.IsVisible(Game.Viewer, country))
                AudioManager.Play(Sound.Raze);
        }

        private void OnTreatySigned(Treaty treaty)
        {
            if (!Muted && treaty.Involves(Game.Viewer))
                AudioManager.Play(Sound.Treaty);
        }

        private void OnTreatyBroken(Treaty treaty)
        {
            if (!Muted)
                AudioManager.Play(Sound.Betrayal);
        }

        // The income die lands after DieRoll.Duration; the rest follows it, like the income popups.
        private void OnTurnBegan()
        {
            var viewer = Game.Viewer;
            if (Muted || viewer == null)
                return;

            var humanTurn = !Game.CurrentPlayer.IsAI;
            if (humanTurn)
                AudioManager.Play(Sound.TurnStart);

            var report = viewer.Report;
            var paid = report.PayoutsOf(report.Rolls.Count - 1).Any(p => !p.Amount.IsEmpty)
                       || humanTurn && report.RegionBonuses.Count > 0;
            if (paid)
                After(DieRoll.Duration, () => AudioManager.Play(Sound.Income));

            if (!humanTurn)
                return;

            After(DieRoll.Duration + DeliveryDelay, () =>
            {
                if (report.Deliveries.Any(d => d.Building != null))
                    AudioManager.Play(Sound.Built);
                if (report.Deliveries.Any(d => d.Building == null))
                    AudioManager.Play(Sound.Trained);
                if (Game.Diplomacy.OffersTo(viewer).Any())
                    AudioManager.Play(Sound.Offer);
            });
        }

        private void After(float seconds, Action play) => StartCoroutine(Delayed(seconds, play));

        private static IEnumerator Delayed(float seconds, Action play)
        {
            yield return new WaitForSecondsRealtime(seconds);
            play();
        }
    }
}
