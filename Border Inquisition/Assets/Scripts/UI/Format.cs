using System.Collections.Generic;
using Gameplay;
using View;

namespace UI
{
    // Rich-text snippets the panels share, so a player or an amount reads the same everywhere.
    public static class Format
    {
        public static string Name(Player player) =>
            player == null ? "-" : $"<color=#{PlayerPalette.HexOf(player)}>{player.Name}</color>";

        // "Food 2, Gold 1": zero amounts are left out, and nothing at all reads as "nothing".
        public static string Resources(GameResources resources)
        {
            var parts = new List<string>();
            Add(parts, "Food", resources.Food);
            Add(parts, "Wood", resources.Wood);
            Add(parts, "Gold", resources.Gold);
            Add(parts, "Stone", resources.Stone);
            return parts.Count > 0 ? string.Join(", ", parts) : "nothing";
        }

        public static string Army(Army army) =>
            $"{Count(army.Knights, "knight")}, {Count(army.Horsemen, "horseman", "horsemen")}, " +
            Count(army.Archers, "archer");

        // "income Food 1, soldiers -20%": what a building does for whoever holds its country.
        public static string Effects(Building building)
        {
            var parts = new List<string>();
            if (!building.ProductionBoost.IsEmpty)
                parts.Add("income " + Resources(building.ProductionBoost));
            if (building.SoldierDiscount > 0)
                parts.Add($"soldiers -{building.SoldierDiscount}%");
            if (building.BuildingDiscount > 0)
                parts.Add($"buildings -{building.BuildingDiscount}%");
            return parts.Count > 0 ? string.Join(", ", parts) : "no effect";
        }

        public static string Count(int count, string one, string many = null) =>
            $"{count} {(count == 1 ? one : many ?? one + "s")}";

        private static void Add(List<string> parts, string resource, int amount)
        {
            if (amount != 0)
                parts.Add($"{resource} {amount}");
        }
    }
}
