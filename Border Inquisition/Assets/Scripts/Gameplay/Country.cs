using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay
{
    public class Country : MonoBehaviour
    {
        // Stable id the gameplay and the future network layer address this country by.
        [SerializeField] private int _id = -1;

        // Author a border on one side only; MapGraph mirrors it.
        [SerializeField] private List<Country> _borders = new List<Country>();

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

        private Player _owner;
        private HashSet<Building> _builtBuildings;
        private List<Building> _buildingQueue;

        public Player Owner => _owner;
        public void SetOwner(Player player) => _owner = player;

        public int Id => _id;
        public IReadOnlyList<Country> Borders => _borders;

#if UNITY_EDITOR
        public void SetId(int id) => _id = id;

        // Refuses a duplicate in either direction, so an edge is only ever authored once.
        public bool AddBorder(Country other)
        {
            if (other == null || other == this || _borders.Contains(other) || other._borders.Contains(this))
                return false;

            _borders.Add(other);
            return true;
        }

        public bool RemoveBorder(Country other)
        {
            if (other == null)
                return false;

            var removed = _borders.Remove(other);
            return other._borders.Remove(this) || removed;
        }
#endif


        private void Awake()
        {
            if (!_isDiceNumberSet)
            {
                _nationDiceNumber = Random.Range(1, 10);
                _isDiceNumberSet = true;
            }

            _builtBuildings ??= new HashSet<Building>();
            _buildingQueue ??= new List<Building>();
            _trainingQueue ??= new List<SoldierType>();
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
            for (int i = 0; i < _trainingQueue.Count; )
            {
                var soldier = _trainingQueue[i];
                if (CanAffordTraining(soldier, resources))
                {
                    CreateSoldier(soldier);
                    resources -= GetSoldierCost(soldier);
                    _trainingQueue.RemoveAt(i);
                }
                else
                {
                    i++;
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
        public bool IsArmyEmpty => _army.IsArmyEmpty();
        public Army Army => _army;

        // Whether the move is legal at all is the GameController's call, not the country's.
        public bool SendUnits(Country target, int knights, int horsemen, int archers)
        {
            if (!_army.Contains(knights, horsemen, archers))
                return false;

            _army.RemoveUnit(knights, horsemen, archers);
            target._army.AddUnit(knights, horsemen, archers);
            return true;
        }

        // The force that won a country occupies it; nothing is left behind.
        public void SendWholeArmy(Country target) =>
            SendUnits(target, _army.Knights, _army.Horsemen, _army.Archers);

        #endregion
        
        public void StartPhaseOne(ref GameResources currentResources)
        {
            currentResources = ProcessBuildingQueue(currentResources);
            currentResources = StartTraining(currentResources);
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