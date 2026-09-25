using System.Collections.Generic;
using System.Linq;
using Gameplay.Managers;
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

        // Prices, soldiers and buildings alike, are set per region, not per country.
        [SerializeField] private Region _region;

        [SerializeField] private int _nationDiceNumber;
        [SerializeField] private GameResources _baseResourceGain;
        [SerializeField] private Army _army;
        [SerializeField] private List<SoldierType> _trainingQueue;

        private Player _owner;
        private HashSet<Building> _builtBuildings;
        private List<Building> _buildingQueue;

        public Player Owner => _owner;
        // A new owner does not inherit the old one's orders; built buildings stay with the country.
        public void SetOwner(Player player)
        {
            if (player != _owner)
            {
                _buildingQueue?.Clear();
                _trainingQueue?.Clear();
            }
            _owner = player;
        }

        public int Id => _id;
        public IReadOnlyList<Country> Borders => _borders;
        public Region Region => _region;

        public int DiceNumber => _nationDiceNumber;
        public void SetDiceNumber(int number) => _nationDiceNumber = number;

        public void SetArmy(Army army) => _army = army;
        public void SetBaseResourceGain(GameResources gain) => _baseResourceGain = gain;

#if UNITY_EDITOR
        public void SetId(int id) => _id = id;
        public void SetRegion(Region region) => _region = region;
#endif


        private void Awake()
        {
            _builtBuildings ??= new HashSet<Building>();
            _buildingQueue ??= new List<Building>();
            _trainingQueue ??= new List<SoldierType>();

            if (_region == null)
                Debug.LogWarning($"{name} has no region - run Assign Regions on the GameController.", this);
        }

        #region Buildings
        
        // An unaffordable head of the queue simply waits; the owner sees it in the turn report.
        private GameResources ProcessBuildingQueue(GameResources resources, TurnReport report)
        {
            if (!_buildingQueue.Any())
                return resources;

            var building = _buildingQueue[0];
            var cost = GetBuildingCost(building);
            if (!(resources >= cost))
                return resources;

            _builtBuildings.Add(building);
            resources -= cost;
            _buildingQueue.RemoveAt(0);
            report.AddBuilt(this, building);
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

        public IReadOnlyCollection<Building> BuiltBuildings => _builtBuildings;
        public IReadOnlyList<Building> BuildingQueue => _buildingQueue;
        public bool IsBuilt(Building building) => _builtBuildings.Contains(building);
        public bool IsQueued(Building building) => _buildingQueue.Contains(building);

        public GameResources GetBuildingCost(Building building) =>
            PriceHere(building.BuildingCost, _owner?.BuildingDiscount ?? 0);

        #endregion

        #region Units
        
        private bool CanAffordTraining(SoldierType soldierType, GameResources resources) =>
            _region != null && resources >= GetSoldierCost(soldierType);

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

        private GameResources StartTraining(GameResources resources, TurnReport report)
        {
            for (int i = 0; i < _trainingQueue.Count; )
            {
                var soldier = _trainingQueue[i];
                if (CanAffordTraining(soldier, resources))
                {
                    CreateSoldier(soldier);
                    resources -= GetSoldierCost(soldier);
                    _trainingQueue.RemoveAt(i);
                    report.AddTrained(this, soldier);
                }
                else
                {
                    i++;
                }
            }
            return resources;
        }

        public GameResources GetSoldierCost(SoldierType soldier) =>
            PriceHere(GameController.Instance.Rules.SoldierCost(soldier), _owner?.SoldierDiscount ?? 0);
        
        public void AddSoldierToQueue(SoldierType soldier) => _trainingQueue.Add(soldier);
        public void RemoveSoldierFromQueue(SoldierType soldier) => _trainingQueue.Remove(soldier);
        public int QueuedSoldiers(SoldierType soldier) => _trainingQueue.Count(queued => queued == soldier);
        public IReadOnlyList<SoldierType> TrainingQueue => _trainingQueue;
        
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
        
        public void StartPhaseOne(ref GameResources currentResources, TurnReport report)
        {
            currentResources = ProcessBuildingQueue(currentResources, report);
            currentResources = StartTraining(currentResources, report);
        }

        // The region's price for a base cost, less the owner's best discount in percent.
        private GameResources PriceHere(GameResources baseCost, int discount)
        {
            var price = _region != null ? _region.Price(baseCost, GameController.Instance.Rules) : baseCost;
            return price.Percent(100 - discount);
        }

        // What the country pays its owner whenever the income die shows its number.
        public GameResources Income =>
            _builtBuildings.Aggregate(_baseResourceGain, (income, building) => income + building.ProductionBoost);
    }
}