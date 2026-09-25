using System.Linq;

namespace Gameplay
{
    // Bank trade, Catan style: give Ratio of one resource for 1 of another. The ratio is
    // GameRules.DefaultTradeRatio (4:1) unless the player holds a country with a built trade building
    // (Building.TradeRatio), which lowers it.
    // Player-to-player trade offers are planned for online play.
    public class Market
    {
        private readonly GameRules _rules;

        public Market(GameRules rules) => _rules = rules;

        public int RatioFor(Player player) =>
            player == null
                ? _rules.DefaultTradeRatio
                : player.OwnedCountries
                    .SelectMany(country => country.BuiltBuildings)
                    .Where(building => building.TradeRatio > 0)
                    .Select(building => building.TradeRatio)
                    .Append(_rules.DefaultTradeRatio)
                    .Min();

        // Receives `amount` of `get`, paying ratio * amount of `give`.
        public bool TryTrade(Player player, ResourceType give, ResourceType get, int amount)
        {
            if (player == null || give == get || amount <= 0)
                return false;

            if (!player.TrySpend(GameResources.Of(give, RatioFor(player) * amount)))
                return false;

            player.Receive(GameResources.Of(get, amount));
            return true;
        }
    }
}
