using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(menuName = "Create Building", fileName = "Building", order = 0)]
    public class Building : ScriptableObject
    {
        [SerializeField] private string _name;
        // The base price; the country's region raises or lowers it (Region.Price).
        [SerializeField] private GameResources _buildingCost;
        [SerializeField] private GameResources _productionBoost;

        // Percent off every soldier (Barracks) or every building (Tavern) the owner pays for, in any of
        // their countries, while they hold the country it stands in. Discounts do not stack; the best
        // one counts.
        [SerializeField, Range(0, 100)] private int _soldierDiscount;
        [SerializeField, Range(0, 100)] private int _buildingDiscount;

        public string DisplayName => string.IsNullOrEmpty(_name) ? name : _name;
        public  GameResources BuildingCost => _buildingCost;
        public  GameResources ProductionBoost => _productionBoost;
        public int SoldierDiscount => _soldierDiscount;
        public int BuildingDiscount => _buildingDiscount;
    }
}
