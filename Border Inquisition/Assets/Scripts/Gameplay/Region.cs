using UnityEngine;

namespace Gameplay
{
    // Sits on a continent's parent object; every country of the continent trains soldiers at its prices.
    public class Region : MonoBehaviour
    {
        [SerializeField] private GameResources _knightCost;
        [SerializeField] private GameResources _horsemanCost;
        [SerializeField] private GameResources _archerCost;

        public GameResources SoldierCost(SoldierType soldier)
        {
            switch (soldier)
            {
                case SoldierType.Knight:
                    return _knightCost;
                case SoldierType.Horseman:
                    return _horsemanCost;
                case SoldierType.Archer:
                    return _archerCost;
            }
            return default;
        }
    }
}
