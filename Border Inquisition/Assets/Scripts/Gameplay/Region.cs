using UnityEngine;

namespace Gameplay
{
    // Sits on a continent's parent object. The region's profile sets its prices: whatever it is rich in
    // is cheaper there and whatever it is poor in dearer (GameRules.RichPricePercent / PoorPricePercent),
    // for soldiers and buildings alike. Holding every country of the region pays its bonus each turn.
    public class Region : MonoBehaviour
    {
        [SerializeField] private ResourceType _richIn;
        [SerializeField] private ResourceType _poorIn;

        // Paid to a player at the start of each of their turns while they own the whole region.
        [SerializeField] private GameResources _completionBonus;

        public ResourceType RichIn => _richIn;
        public ResourceType PoorIn => _poorIn;
        public GameResources CompletionBonus => _completionBonus;

        public GameResources Price(GameResources baseCost, GameRules rules) =>
            baseCost
                .Percent(_richIn, rules.RichPricePercent)
                .Percent(_poorIn, rules.PoorPricePercent);
    }
}
