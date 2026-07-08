using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay
{
    public class Country : MonoBehaviour
    {
        [SerializeField] private int _nationDiceNumber;
        [SerializeField] private GameResources _baseResources;
        [SerializeField] private Army _army;
        [SerializeField] private List<SoldierType> _trainingQueue;

        //Army cost per nation (Country environment conditions...)
        [SerializeField] private GameResources _knightCost;
        [SerializeField] private GameResources _horsemanCost;
        [SerializeField] private GameResources _archerCost;

        //For saving later
        [SerializeField] private bool _isDiceNumberSet;

        private HashSet<Building> _buildings;
        public Army Army => _army;

        private void Awake()
        {
            if (!_isDiceNumberSet)
                _nationDiceNumber = Random.Range(1, 9);

            _buildings = new HashSet<Building>();
            _trainingQueue = new List<SoldierType>();
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

        public GameResources StartPhaseOne(GameResources currentResources)
        {
            var resourceGained = _baseResources;
            foreach (var building in _buildings)
                resourceGained += building.ProductionBoost;
            currentResources += resourceGained;
            StartTraining(currentResources);
            return currentResources;
        }

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
                    _army.AddSoldier(1, 0, 0);
                    break;
                case SoldierType.Horseman:
                    _army.AddSoldier(0, 1, 0);
                    break;
                case SoldierType.Archer:
                    _army.AddSoldier(0, 0, 1);
                    break;
            }
        }

        private GameResources StartTraining(GameResources resources)
        {
            for (int i = 0; i < _trainingQueue.Count; i++)
            {
                var soldier = _trainingQueue[i];
                if (CanAffordTraining(soldier, resources))
                {
                    CreateSoldier(soldier);
                    resources -= GetSoldierCost(soldier, resources);
                    _trainingQueue.RemoveAt(i);
                }
            }
            return resources;
        }

        private GameResources GetSoldierCost(SoldierType soldier, GameResources resources)
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
    }
}