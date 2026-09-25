using System.Collections;
using System.Linq;
using Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using View;

namespace UI
{
    // Shows the last attack: both sides' dice sorted and paired highest-to-highest, one column per pair,
    // each die in its owner's seat colour. The dice tumble first; then the loser of every pair dims and
    // the outcome line appears. It never blocks map clicks, so the player can keep attacking while it
    // is up; it hides on phase change.
    public class CombatPanel : MonoBehaviour
    {
        private const float DieSize = 64f;
        private const float NameWidth = 220f;
        private const float LostAlpha = 0.25f;

        private DiceFaces _faces;
        private TMP_Text _title;
        private Transform _attackerRow;
        private Transform _defenderRow;
        private TMP_Text _outcome;

        public static CombatPanel Create(Transform canvas, DiceFaces faces)
        {
            var root = UiFactory.Panel(canvas, "CombatPanel", new Vector2(0.5f, 1f));
            root.anchoredPosition = new Vector2(0f, -20f);
            root.GetComponent<Image>().raycastTarget = false;

            var panel = root.gameObject.AddComponent<CombatPanel>();
            panel._faces = faces;
            panel.Build(root);
            root.gameObject.SetActive(false);
            return panel;
        }

        private void Build(Transform root)
        {
            _title = UiFactory.Label(root, "-", 26f, NameWidth + 3 * (DieSize + 8f), 36f, TextAlignmentOptions.Center);
            _attackerRow = UiFactory.Row(root, "Attacker");
            _defenderRow = UiFactory.Row(root, "Defender");
            _outcome = UiFactory.Label(root, "-", 22f, NameWidth + 3 * (DieSize + 8f), 30f, TextAlignmentOptions.Center);
        }

        // The defender is passed in rather than read from the country, which has changed hands on a conquest.
        public void Show(Country from, Country to, Player attacker, Player defender, Combat.CombatResult result)
        {
            var attackerDice = result.AttackerDice.OrderByDescending(die => die).ToArray();
            var defenderDice = result.DefenderDice.OrderByDescending(die => die).ToArray();
            var attackerWins = result.AttackerWins;

            _title.text = $"{from.name} attacks {to.name}";
            FillRow(_attackerRow, attacker, attackerDice, attackerWins, true);
            FillRow(_defenderRow, defender, defenderDice, attackerWins, false);

            var defenderLosses = attackerWins.Count(win => win);
            var attackerLosses = attackerWins.Length - defenderLosses;
            var outcome = to.Owner == attacker
                ? $"{to.name} falls to {attacker.Name}"
                : $"Attacker loses {attackerLosses}, defender loses {defenderLosses}";

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            StopAllCoroutines();
            StartCoroutine(RevealOutcome(outcome));
        }

        private IEnumerator RevealOutcome(string outcome)
        {
            _outcome.text = "...";
            yield return new WaitForSecondsRealtime(DieRoll.Duration);
            _outcome.text = outcome;
        }

        public void Hide() => gameObject.SetActive(false);

        // Every row gets a slot per pair, empty where that side rolled fewer dice, so the columns line up.
        private void FillRow(Transform row, Player player, int[] dice, bool[] attackerWins, bool isAttacker)
        {
            UiFactory.Clear(row);

            var tint = PlayerPalette.ColorOf(player);
            var label = UiFactory.Label(row, player != null ? player.Name : "-", 24f, NameWidth, DieSize);
            label.color = tint;

            for (var i = 0; i < attackerWins.Length; i++)
            {
                if (i >= dice.Length)
                {
                    UiFactory.Spacer(row, DieSize, DieSize);
                    continue;
                }

                var won = attackerWins[i] == isAttacker;
                var die = UiFactory.Icon(row, null, DieSize, DieSize);
                DieRoll.Roll(die, _faces, dice[i], won ? tint : new Color(tint.r, tint.g, tint.b, LostAlpha));
            }
        }
    }
}
