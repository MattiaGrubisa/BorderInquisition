using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay
{
    // What the lobby hands to a new match. Local hotseat for now, so players are only named by seat.
    public class MatchSettings
    {
        public const int MinPlayers = 4;
        public const int MaxPlayers = 6;

        public IReadOnlyList<string> PlayerNames { get; }

        public MatchSettings(int playerCount)
        {
            playerCount = Mathf.Clamp(playerCount, MinPlayers, MaxPlayers);
            PlayerNames = Enumerable.Range(1, playerCount).Select(i => $"Player {i}").ToList();
        }
    }
}
