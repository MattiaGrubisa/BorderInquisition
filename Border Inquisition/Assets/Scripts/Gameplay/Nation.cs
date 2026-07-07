using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay
{
    public class Nation : MonoBehaviour
    {
        [SerializeField] private int _nationDiceNumber;
        [SerializeField] private GameResources _baseResources;
        [SerializeField] private Army _army;
        private Queue<SoldierType> _soldiers;
        
        //Army cost per nation (Nation environment conditions...)
        [SerializeField] private GameResources _knightCost;
        [SerializeField] private GameResources _horsemanCost;
        [SerializeField] private GameResources _archerCost;
        
        //For saving later
        [SerializeField] private bool _isDiceNumberSet;
        
        private HashSet<Building> _buildings;
        
        private void Awake()
        {
            if(!_isDiceNumberSet)
                _nationDiceNumber = Random.Range(1, 9);
            _buildings = new HashSet<Building>();
        }

        public bool TryCreateBuilding(Building building, GameResources resources)
        {
            if (resources >= building.BuildingCost)
            {
                _buildings.Add(building);
                return true;
            }
            else
            {
                Debug.LogWarning($"Can't create building {building.name}");
                return false;
            }
        }

        private void DestroyBuildings(Building building) => _buildings.Remove(building);

        public GameResources GetPhaseOneResources()
        {
            var resourceGained = _baseResources;
            foreach (var building in _buildings) 
                resourceGained += building.ProductionBoost;
            
            return resourceGained;
        }
        
        public (bool, GameResources) AddSoldiers(SoldierType soldierType,GameResources resources)
        {
            switch (soldierType)
            {
                case SoldierType.Knight:
                    if(resources <= _knightCost) return  (false, _knightCost);
                        _army.AddSoldier(1,0,0);
                        return (true, _knightCost);
                case SoldierType.Archer:
                    if(resources <= _archerCost) return(false, _archerCost);
                        _army.AddSoldier(0,0,1);
                        return (true, _archerCost);
                case SoldierType.Horseman:
                    if(resources <= _horsemanCost) return  (false, _horsemanCost);
                        _army.AddSoldier(0,1,0);
                        return (true, _horsemanCost);
            }

            return (false, default);
        }

        public void CreationQueue()
        {
            
        }
    }
}