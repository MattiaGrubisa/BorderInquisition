#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AI;
using Gameplay;
using Gameplay.Managers;
using GameStates;
using UnityEditor;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Editor
{
    // Plays matches without the view, every seat through an AiBrain, turns back to back in one call.
    // PlayOut finishes the running match, humans included (its end goes through MatchWon/MatchLost as
    // usual, so GameOver follows). RunBatch plays many all-AI matches with GameController.Simulating
    // set, and logs statistics to balance the numbers by. Both take the running match over, so the AI
    // driver is halted first; a batch, and a play-out that hits the round limit, return to the main
    // menu afterwards, since the phase machine no longer matches the match.
    public static class MatchSimulator
    {
        public const int DefaultRoundLimit = 200;
        private const int ProgressEveryTurns = 10;

        public class BatchSettings
        {
            public int Matches = 20;
            public int Players = 5;
            public Difficulty Difficulty = Difficulty.Normal;
            // Seats cycle Easy, Normal, Hard instead of all playing Difficulty.
            public bool Mixed;
            // Battles simulated per attack option; fewer than the profile's makes batches faster.
            public int Samples = 30;
            public int RoundLimit = DefaultRoundLimit;
        }

        // Counted over a whole batch.
        private class Stats
        {
            public int Matches;
            public int Stuck;
            public readonly List<int> Rounds = new List<int>();
            public readonly Dictionary<int, int> WinsBySeatOrder = new Dictionary<int, int>();
            public readonly Dictionary<Difficulty, int> WinsByDifficulty = new Dictionary<Difficulty, int>();
            public readonly Dictionary<Difficulty, int> SeatsByDifficulty = new Dictionary<Difficulty, int>();
            public int Attacks;
            public int Conquests;
            public int Razed;
            public int PlayerTurns;
            public GameResources Income;
            public readonly Dictionary<string, int> Built = new Dictionary<string, int>();
            public readonly Dictionary<SoldierType, int> Trained = new Dictionary<SoldierType, int>();
            public readonly Dictionary<string, int> RegionBonuses = new Dictionary<string, int>();
        }

        public static void PlayOut(GameController game, int roundLimit)
        {
            if (!CanTakeOver(game))
                return;

            Halt(game);
            var limit = roundLimit * game.Players.Count;
            var turns = 0;
            while (!game.IsOver && turns < limit)
            {
                // The running turn has had its income already.
                PlayTurn(game, Driver(game).ProfileFor(PlayedAs(game.CurrentPlayer)), turns > 0, null);
                turns++;
                if (!game.IsOver)
                    game.NextPlayer();
            }

            if (game.Winner != null)
                Debug.Log($"{game.Winner.Name} wins after {turns} turns.", game);
            else if (!game.IsOver)
            {
                Debug.LogWarning($"No winner after {turns} turns - the match is stuck. Back to the main menu.", game);
                GameStateMachine.Instance.LeaveMatch();
            }
        }

        public static void RunBatch(GameController game, BatchSettings settings)
        {
            if (!CanTakeOver(game))
                return;

            Halt(game);
            var profiles = new Dictionary<Difficulty, AiProfile>();
            var stats = new Stats();
            var clock = Stopwatch.StartNew();
            var cancelled = false;
            game.Simulating = true;
            try
            {
                for (var match = 0; match < settings.Matches && !cancelled; match++)
                    cancelled = !PlayMatch(game, settings, match, profiles, stats);
            }
            finally
            {
                game.Simulating = false;
                EditorUtility.ClearProgressBar();
                foreach (var profile in profiles.Values)
                    Object.Destroy(profile);
            }

            Debug.Log(Report(settings, stats, clock.Elapsed, cancelled), game);
            GameStateMachine.Instance.LeaveMatch();
        }

        // False when cancelled from the progress bar.
        private static bool PlayMatch(GameController game, BatchSettings settings, int match,
            Dictionary<Difficulty, AiProfile> profiles, Stats stats)
        {
            var seats = Enumerable.Range(0, Math.Clamp(settings.Players, MatchSettings.MinPlayers, MatchSettings.MaxPlayers))
                .Select(seat => settings.Mixed ? (Difficulty)(seat % 3) : settings.Difficulty)
                .ToList();
            game.StartNewMatch(MatchSettings.AllAI(seats));
            foreach (var player in game.Players)
                Add(stats.SeatsByDifficulty, player.Difficulty);

            var start = game.Players.ToList().IndexOf(game.CurrentPlayer);
            var limit = settings.RoundLimit * game.Players.Count;
            var turns = 0;
            while (!game.IsOver && turns < limit)
            {
                if (turns % ProgressEveryTurns == 0 && EditorUtility.DisplayCancelableProgressBar("AI simulation",
                        $"Match {match + 1} of {settings.Matches}, round {turns / game.Players.Count + 1}",
                        (match + (float)turns / limit) / settings.Matches))
                    return false;

                var player = game.CurrentPlayer;
                PlayTurn(game, Profile(game, player.Difficulty, settings.Samples, profiles), false, stats);
                Collect(player.Report, stats);
                turns++;
                if (!game.IsOver)
                    game.NextPlayer();
            }

            stats.Matches++;
            if (game.Winner == null)
            {
                stats.Stuck++;
                return true;
            }

            var players = game.Players.ToList();
            stats.Rounds.Add((turns + players.Count - 1) / players.Count);
            Add(stats.WinsBySeatOrder, (players.IndexOf(game.Winner) - start + players.Count) % players.Count);
            Add(stats.WinsByDifficulty, game.Winner.Difficulty);
            return true;
        }

        // One whole turn of the current player, as AiDriver plays it but without pauses.
        private static void PlayTurn(GameController game, AiProfile profile, bool skipIncome, Stats stats)
        {
            if (!skipIncome)
                game.PhaseOne();

            var brain = new AiBrain(game.CurrentPlayer, profile);
            brain.AnswerOffers();
            for (var i = 0; i < profile.MaxAttacksPerTurn && !game.IsOver; i++)
            {
                if (!brain.TryChooseAttack(out var from, out var to) || !game.TryAttack(from, to, out _))
                    break;

                if (stats != null)
                    stats.Attacks++;
                if (game.IsOver || to.Owner != from.Owner)
                    continue;

                var razed = brain.Occupy(from, to);
                if (stats == null)
                    continue;

                stats.Conquests++;
                stats.Razed += razed;
            }

            if (game.IsOver)
                return;

            if (brain.TryChooseMove(out var moveFrom, out var moveTo, out var units))
                game.TryMoveArmy(moveFrom, moveTo, units.Knights, units.Horsemen, units.Archers);
            brain.PlayEconomy();
        }

        // Read before NextPlayer clears the report: this turn's deliveries and region bonuses, and the
        // income of every roll since the player's last turn.
        private static void Collect(TurnReport report, Stats stats)
        {
            stats.PlayerTurns++;
            stats.Income += report.TotalIncome;
            foreach (var delivery in report.Deliveries)
            {
                if (delivery.Building != null)
                    Add(stats.Built, delivery.Building.DisplayName);
                else
                    Add(stats.Trained, delivery.Soldier, delivery.Count);
            }

            foreach (var bonus in report.RegionBonuses)
                Add(stats.RegionBonuses, bonus.Region.name);
        }

        private static string Report(BatchSettings settings, Stats stats, TimeSpan elapsed, bool cancelled)
        {
            var text = new StringBuilder();
            var seats = settings.Mixed ? "mixed Easy/Normal/Hard" : settings.Difficulty.ToString();
            text.AppendLine($"AI simulation: {stats.Matches} matches{(cancelled ? " (cancelled)" : string.Empty)}, " +
                            $"{settings.Players} players ({seats}), {settings.Samples} battle samples, " +
                            $"round limit {settings.RoundLimit}");
            if (stats.Matches == 0)
                return text.ToString();

            var won = stats.Matches - stats.Stuck;
            text.AppendLine($"Won {won}, stuck {stats.Stuck} (hit the round limit)");
            if (stats.Rounds.Count > 0)
                text.AppendLine($"Rounds to a win: avg {stats.Rounds.Average():0.#}, min {stats.Rounds.Min()}, " +
                                $"max {stats.Rounds.Max()}");

            if (won > 0)
            {
                text.AppendLine("Wins by turn order: " + string.Join(", ", stats.WinsBySeatOrder.OrderBy(p => p.Key)
                    .Select(p => $"{Ordinal(p.Key + 1)} {p.Value} ({100f * p.Value / won:0}%)")));
                if (settings.Mixed)
                    text.AppendLine("Wins per seat by difficulty: " + string.Join(", ", stats.SeatsByDifficulty
                        .OrderBy(p => p.Key)
                        .Select(p => $"{p.Key} {Get(stats.WinsByDifficulty, p.Key)}/{p.Value} seats")));
            }

            var matches = (float)stats.Matches;
            text.AppendLine($"Per match: {stats.Attacks / matches:0} dice rounds, {stats.Conquests / matches:0.#} conquests, " +
                            $"{stats.Razed / matches:0.#} buildings razed");

            var turns = Math.Max(1, stats.PlayerTurns);
            text.AppendLine($"Income per player turn: Food {stats.Income.Food / (float)turns:0.##}, " +
                            $"Wood {stats.Income.Wood / (float)turns:0.##}, Gold {stats.Income.Gold / (float)turns:0.##}, " +
                            $"Stone {stats.Income.Stone / (float)turns:0.##}");

            var buildings = GameController.Instance.Buildings.Where(b => b != null).Select(b => b.DisplayName);
            text.AppendLine("Built per match: " + PerMatch(buildings, stats.Built, matches));
            text.AppendLine("Trained per match: " + PerMatch((SoldierType[])Enum.GetValues(typeof(SoldierType)),
                stats.Trained, matches));

            var regions = GameController.Instance.Countries.Where(c => c.Region != null)
                .Select(c => c.Region.name).Distinct().OrderBy(name => name);
            text.AppendLine("Region bonuses paid per match: " + PerMatch(regions, stats.RegionBonuses, matches));
            text.Append($"Took {elapsed.TotalSeconds:0} s ({elapsed.TotalSeconds / stats.Matches:0.0} s per match)");
            return text.ToString();
        }

        private static string PerMatch<T>(IEnumerable<T> keys, Dictionary<T, int> counts, float matches) =>
            string.Join(", ", keys.Select(key => $"{key} {Get(counts, key) / matches:0.#}"));

        private static bool CanTakeOver(GameController game)
        {
            if (game != null && game.Players.Count > 0 && !game.IsOver)
                return true;

            Debug.LogWarning("Start a match first - the simulation takes the running one over.", game);
            return false;
        }

        private static void Halt(GameController game)
        {
            var driver = Driver(game);
            if (driver != null)
                driver.Halt();
        }

        private static AiDriver Driver(GameController game) => game.GetComponent<AiDriver>();

        // PlayOut plays humans as Normal AIs.
        private static Difficulty PlayedAs(Player player) => player.IsAI ? player.Difficulty : Difficulty.Normal;

        private static AiProfile Profile(GameController game, Difficulty difficulty, int samples,
            Dictionary<Difficulty, AiProfile> profiles)
        {
            if (!profiles.TryGetValue(difficulty, out var profile))
                profiles[difficulty] = profile = Driver(game).ProfileFor(difficulty).WithSimulations(samples);
            return profile;
        }

        private static void Add<T>(Dictionary<T, int> counts, T key, int amount = 1) =>
            counts[key] = Get(counts, key) + amount;

        private static int Get<T>(Dictionary<T, int> counts, T key) => counts.TryGetValue(key, out var count) ? count : 0;

        private static string Ordinal(int n) => n == 1 ? "1st" : n == 2 ? "2nd" : n == 3 ? "3rd" : $"{n}th";
    }
}
#endif
