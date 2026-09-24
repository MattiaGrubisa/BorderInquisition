using Gameplay;
using Gameplay.Managers;
using UnityEngine;

namespace View
{
    // Seat colours are presentation, so they live here and not on Player; a player's seat is their
    // index in the match's player list.
    public static class PlayerPalette
    {
        private static readonly Color Unowned = new Color(0.62f, 0.62f, 0.62f);

        private static readonly Color[] Seats =
        {
            new Color(0.86f, 0.24f, 0.22f),
            new Color(0.22f, 0.46f, 0.88f),
            new Color(0.25f, 0.70f, 0.32f),
            new Color(0.95f, 0.80f, 0.20f),
            new Color(0.62f, 0.34f, 0.80f),
            new Color(0.96f, 0.56f, 0.18f)
        };

        public static Color ColorOf(Player player)
        {
            if (player == null || GameController.Instance == null)
                return Unowned;

            var players = GameController.Instance.Players;
            for (var seat = 0; seat < players.Count; seat++)
                if (players[seat] == player)
                    return Seats[seat % Seats.Length];

            return Unowned;
        }

        public static string HexOf(Player player) => ColorUtility.ToHtmlStringRGB(ColorOf(player));
    }
}
