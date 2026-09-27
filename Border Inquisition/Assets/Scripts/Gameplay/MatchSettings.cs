using System.Collections.Generic;
using System.Linq;

namespace Gameplay
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    // One place at the table: a human, or an AI of some difficulty.
    public readonly struct Seat
    {
        public Seat(bool isAI, Difficulty difficulty = Difficulty.Normal)
        {
            IsAI = isAI;
            Difficulty = difficulty;
        }

        public bool IsAI { get; }
        public Difficulty Difficulty { get; }
    }

    // What the lobby hands to a new match: 2-6 seats, at least one of them human (a match with no
    // human in it ends at once). Players are named by seat for now.
    public class MatchSettings
    {
        public const int MinPlayers = 2;
        public const int MaxPlayers = 6;

        public IReadOnlyList<Seat> Seats { get; }

        public MatchSettings(IEnumerable<Seat> seats)
        {
            var list = seats.Take(MaxPlayers).ToList();
            while (list.Count < MinPlayers)
                list.Add(new Seat(true));
            if (list.All(seat => seat.IsAI))
                list[0] = new Seat(false);
            Seats = list;
        }

        private MatchSettings(List<Seat> seats) => Seats = seats;

        // Every seat an AI, for the editor's simulated matches only; the lobby always seats a human.
        public static MatchSettings AllAI(IEnumerable<Difficulty> difficulties) =>
            new MatchSettings(difficulties.Take(MaxPlayers).Select(difficulty => new Seat(true, difficulty)).ToList());

        public static string PlayerName(int seatIndex, Seat seat) =>
            seat.IsAI ? $"Player {seatIndex + 1} (AI)" : $"Player {seatIndex + 1}";
    }
}
