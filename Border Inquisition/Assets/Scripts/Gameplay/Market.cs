using System.Collections.Generic;
using System.Linq;

namespace Gameplay
{
    // Bank trade, Catan style: give Ratio of one resource for 1 of another. The ratio is per player and
    // per resource given: GameRules.DefaultTradeRatio (4:1), or GameRules.RegionTradeRatio (2:1) for a
    // resource that a region the player holds whole is rich in - a whole region has it to spare.
    // Player-to-player trade offers are planned for online play.
    public class Market
    {
        private readonly GameRules _rules;
        private readonly IReadOnlyList<Country> _countries;

        public Market(GameRules rules, IReadOnlyList<Country> countries)
        {
            _rules = rules;
            _countries = countries;
        }

        public int RatioFor(Player player, ResourceType give) =>
            HoldsRegionRichIn(player, give) ? _rules.RegionTradeRatio : _rules.DefaultTradeRatio;

        // The regions a player holds whole that are rich in the resource; for the trade panel to name them.
        public IEnumerable<Region> RegionsRichIn(Player player, ResourceType resource) =>
            _countries
                .Where(country => country.Region != null && country.Region.RichIn == resource)
                .GroupBy(country => country.Region)
                .Where(region => player != null && region.All(country => country.Owner == player))
                .Select(region => region.Key);

        // Receives `amount` of `get`, paying ratio * amount of `give`.
        public bool TryTrade(Player player, ResourceType give, ResourceType get, int amount)
        {
            if (player == null || give == get || amount <= 0)
                return false;

            if (!player.TrySpend(GameResources.Of(give, RatioFor(player, give) * amount)))
                return false;

            player.Receive(GameResources.Of(get, amount));
            return true;
        }

        private bool HoldsRegionRichIn(Player player, ResourceType resource) => RegionsRichIn(player, resource).Any();
    }
}
