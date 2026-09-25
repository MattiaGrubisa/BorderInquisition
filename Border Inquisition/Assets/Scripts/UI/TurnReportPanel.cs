using System.Collections.Generic;
using System.Linq;
using Gameplay;
using TMPro;
using UnityEngine;

namespace UI
{
    // Opens when a turn is revealed: everything that happened to the player since their last turn
    // ended (Player.Report) - every income roll and what it paid, what the queues delivered, what is
    // still waiting for resources, and every attack on their countries. Information only; it closes
    // on Continue or on the next phase change.
    public class TurnReportPanel : MonoBehaviour
    {
        private const float Width = 760f;
        private const float BodySize = 22f;

        private TMP_Text _title;
        private Transform _body;

        public bool IsOpen => gameObject.activeSelf;

        public static TurnReportPanel Create(Transform canvas)
        {
            var root = UiFactory.Panel(canvas, "TurnReportPanel", new Vector2(0.5f, 0.5f));
            var panel = root.gameObject.AddComponent<TurnReportPanel>();
            panel.Build(root);
            root.gameObject.SetActive(false);
            return panel;
        }

        private void Build(Transform root)
        {
            _title = UiFactory.Label(root, "-", 30f, Width, 40f);
            _body = UiFactory.Column(root, "Body");
            UiFactory.Button(root, "Continue", Width, 52f, Close);
        }

        public void Open(Player player)
        {
            _title.text = $"{Format.Name(player)} - since your last turn";
            UiFactory.Clear(_body);

            var report = player.Report;
            Income(report);
            Section("Delivered", report.Deliveries.Select(Describe));
            Section("Still queued - waiting for resources", Waiting(player));
            Section("Attacks on you", report.Defences.Select(Describe));

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close() => gameObject.SetActive(false);

        // One line per roll, oldest first; the last one is this turn's.
        private void Income(TurnReport report)
        {
            var lines = new List<string>();
            for (var i = 0; i < report.Rolls.Count; i++)
            {
                var payouts = report.PayoutsOf(i).ToList();
                var when = i == report.Rolls.Count - 1 ? " (this turn)" : string.Empty;
                var paid = payouts.Count == 0
                    ? "<color=#9A9A9A>nothing for you</color>"
                    : $"{Format.Resources(Sum(payouts))} from {string.Join(", ", payouts.Select(p => p.Country.name))}";
                lines.Add($"Roll <b>{report.Rolls[i]}</b>{when}: {paid}");
            }

            if (report.Rolls.Count > 1)
                lines.Add($"<b>Total: {Format.Resources(report.TotalIncome)}</b>");

            Section("Income", lines);
        }

        // Queues as they stand after this turn's deliveries; the head of a building queue blocks the
        // rest, soldiers are tried one by one.
        private static IEnumerable<string> Waiting(Player player)
        {
            foreach (var country in player.OwnedCountries)
            {
                foreach (var building in country.BuildingQueue)
                    yield return $"{building.DisplayName} in {country.name} - costs {Format.Resources(building.BuildingCost)}";

                foreach (var group in country.TrainingQueue.GroupBy(soldier => soldier))
                    yield return $"{Format.Count(group.Count(), group.Key.ToString())} in {country.name} - " +
                                 $"{Format.Resources(country.GetSoldierCost(group.Key))} each";
            }
        }

        private static string Describe(TurnReport.Delivery delivery) =>
            delivery.Building != null
                ? $"{delivery.Building.DisplayName} built in {delivery.Country.name}"
                : $"{Format.Count(delivery.Count, delivery.Soldier.ToString())} trained in {delivery.Country.name}";

        private static string Describe(TurnReport.Defence defence)
        {
            var attacks = defence.Attacks == 1 ? "attacked" : $"attacked {defence.Attacks} times";
            var outcome = defence.Fallen ? "<color=#FF5040>taken</color>" : "held";
            return $"{Format.Name(defence.Attacker)} {attacks} {defence.Country.name} - {outcome}. " +
                   $"You lost {Format.Count(defence.UnitsLost, "unit")}, they lost {defence.UnitsKilled}.";
        }

        private void Section(string heading, IEnumerable<string> lines)
        {
            var list = lines.ToList();
            if (list.Count == 0)
                return;

            UiFactory.Label(_body, heading, 26f, Width, 36f);
            foreach (var line in list)
                UiFactory.Paragraph(_body, line, BodySize, Width);
        }

        private static GameResources Sum(IEnumerable<TurnReport.Payout> payouts) =>
            payouts.Aggregate(default(GameResources), (sum, p) => sum + p.Amount);
    }
}
