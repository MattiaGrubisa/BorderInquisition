using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(menuName = "Create Building", fileName = "Building", order = 0)]
    public class Building : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private GameResources _buildingCost;
        [SerializeField] private GameResources _productionBoost;
        
        public  GameResources BuildingCost => _buildingCost;
        public  GameResources ProductionBoost => _productionBoost;
    }
}