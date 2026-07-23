using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay
{
    public class Country : MonoBehaviour
    {
        [SerializeField] private int _nationDiceNumber;
        [SerializeField] private GameResources _baseResourceGain;
        [SerializeField] private Army _army;
        [SerializeField] private List<SoldierType> _trainingQueue;

        //Army cost per nation (Country environment conditions...)
        [SerializeField] private GameResources _knightCost;
        [SerializeField] private GameResources _horsemanCost;
        [SerializeField] private GameResources _archerCost;

        //For saving later
        [SerializeField] private bool _isDiceNumberSet;

        private HashSet<Building> _builtBuildings;
        private List<Building> _buildingQueue;

        private void Awake()
        {
            if (!_isDiceNumberSet)
                _nationDiceNumber = Random.Range(1, 9);

            _builtBuildings = new HashSet<Building>();
            _buildingQueue = new List<Building>();
            _trainingQueue = new List<SoldierType>();
        }

        #region Buildings
        
        private GameResources ProcessBuildingQueue(GameResources resources)
        {
            if (!_buildingQueue.Any())
                return resources;
            
            if (resources >= _buildingQueue[0].BuildingCost)
            {
                _builtBuildings.Add(_buildingQueue[0]);
                resources-= _buildingQueue[0].BuildingCost;
                _buildingQueue.RemoveAt(0);
            }
            else
                Debug.LogWarning($"Can't create building {_buildingQueue[0].name}");

            return resources;
        }


        public bool AddBuildingToQueue(Building building)
        {
            if (_buildingQueue.Contains(building) || _builtBuildings.Contains(building))
                return false;
            _buildingQueue.Add(building);
            return true;
        }

        // Mabye change to razing building so when country conquered, give some % of razed buildings as win?
        // Raze all or one?
        public void DestroyBuildings(Building building) => _builtBuildings.Remove(building);
        public bool RemoveBuildingFromQueue(Building building) => _buildingQueue.Remove(building);

        #endregion

        #region Units
        
        private bool CanAffordTraining(SoldierType soldierType, GameResources resources)
        {
            switch (soldierType)
            {
                case SoldierType.Knight:
                    return resources >= _knightCost;
                case SoldierType.Archer:
                    return resources >= _archerCost;
                case SoldierType.Horseman:
                    return resources >= _horsemanCost;
            }
            return false;
        }

        private void CreateSoldier(SoldierType soldierType)
        {
            switch (soldierType)
            {
                case SoldierType.Knight:
                    _army.AddUnit(1, 0, 0);
                    break;
                case SoldierType.Horseman:
                    _army.AddUnit(0, 1, 0);
                    break;
                case SoldierType.Archer:
                    _army.AddUnit(0, 0, 1);
                    break;
            }
        }

        private GameResources StartTraining(GameResources resources)
        {
            for (int i = _trainingQueue.Count - 1; i >= 0; i--)
            {
                var soldier = _trainingQueue[i];
                if (CanAffordTraining(soldier, resources))
                {
                    CreateSoldier(soldier);
                    resources -= GetSoldierCost(soldier);
                    _trainingQueue.RemoveAt(i);
                }
            }
            return resources;
        }

        private GameResources GetSoldierCost(SoldierType soldier)
        {
            switch (soldier)
            {
                case SoldierType.Knight:
                    return _knightCost;
                case SoldierType.Archer:
                    return _archerCost;
                case SoldierType.Horseman:
                    return _horsemanCost;
            }
            return default;
        }
        
        public void AddSoldierToQueue(SoldierType soldier) => _trainingQueue.Add(soldier);
        public void RemoveSoldierFromQueue(SoldierType soldier) => _trainingQueue.Remove(soldier);
        
        public void RemoveRandomUnit() => _army.RemoveRandomUnit();
        public int UniqueUnits() =>  _army.UniqueUnits();
        public double GetArmyPower => _army.ArmyPower;
        public bool AnyArmy() => _army.AnyArmy();
        
        #endregion
        
        public GameResources StartPhaseOne(ref GameResources currentResources)
        {
            currentResources = ProcessBuildingQueue(currentResources);
            currentResources = StartTraining(currentResources);
            return currentResources;
        }

        public void GainResources(ref GameResources playerResources, int dice)
        {
            if (_nationDiceNumber != dice) 
                return;
            
            var resourceGained = _baseResourceGain;
            foreach (var building in _builtBuildings)
                resourceGained += building.ProductionBoost;
            playerResources += resourceGained;
        }
    }
}