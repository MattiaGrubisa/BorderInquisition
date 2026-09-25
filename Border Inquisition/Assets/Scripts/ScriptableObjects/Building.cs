using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(menuName = "Create Building", fileName = "Building", order = 0)]
    public class Building : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private GameResources _buildingCost;
        [SerializeField] private GameResources _productionBoost;

        // 0 for most buildings. Above 0 (the Market: 3) the owner trades with the bank at this ratio
        // instead of the default 4:1, as long as they hold the country it stands in.
        [SerializeField] private int _tradeRatio;

        public string DisplayName => string.IsNullOrEmpty(_name) ? name : _name;
        public  GameResources BuildingCost => _buildingCost;
        public  GameResources ProductionBoost => _productionBoost;
        public int TradeRatio => _tradeRatio;
    }
}