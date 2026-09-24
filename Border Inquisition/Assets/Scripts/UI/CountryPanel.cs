using Gameplay;
using Gameplay.Managers;
using TMPro;
using UnityEngine;

namespace UI
{
    // Build-phase panel for one country: queue or unqueue buildings and soldiers. Nothing is paid here -
    // both queues are processed at the start of the owner's next turn, and whatever is unaffordable then
    // stays queued. Costs are shown so the player can plan against their current resources.
    public class CountryPanel : MonoBehaviour
    {
        private const float RowHeight = 42f;

        private static readonly SoldierType[] Types = { SoldierType.Knight, SoldierType.Horseman, SoldierType.Archer };

        private TMP_Text _title;
        private TMP_Text _resources;
        private Transform _buildingRows;
        private Transform _trainingRows;
        private Country _country;

        public bool IsOpen => gameObject.activeSelf;
        public Country Country => _country;

        public static CountryPanel Create(Transform canvas)
        {
            var root = UiFactory.Panel(canvas, "CountryPanel", new Vector2(1f, 0.5f));
            root.anchoredPosition = new Vector2(-20f, 0f);

            var panel = root.gameObject.AddComponent<CountryPanel>();
            panel.Build(root);
            root.gameObject.SetActive(false);
            return panel;
        }

        private void Build(Transform root)
        {
            _title = UiFactory.Label(root, "-", 30f, 560f, 40f);
            _resources = UiFactory.Label(root, "-", 22f, 560f, 30f);

            UiFactory.Label(root, "Buildings", 26f, 560f, 36f);
            _buildingRows = UiFactory.Column(root, "BuildingRows");

            UiFactory.Label(root, "Training", 26f, 560f, 36f);
            _trainingRows = UiFactory.Column(root, "TrainingRows");

            UiFactory.Button(root, "Close", 560f, 52f, Close);
        }

        public void Open(Country country)
        {
            _country = country;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        public void Close()
        {
            _country = null;
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (_country == null)
                return;

            var region = _country.Region != null ? _country.Region.name : "no region";
            _title.text = $"{_country.name} ({region})";
            _resources.text = $"You have {_country.Owner?.Resources}";

            UiFactory.Clear(_buildingRows);
            var buildings = GameController.Instance.Buildings;
            if (buildings.Count == 0)
                UiFactory.Label(_buildingRows, "No buildings set on the GameController.", 22f, 560f, RowHeight);

            foreach (var building in buildings)
                if (building != null)
                    BuildingRow(building);

            UiFactory.Clear(_trainingRows);
            foreach (var soldier in Types)
                TrainingRow(soldier);
        }

        private void BuildingRow(Building building)
        {
            var row = UiFactory.Row(_buildingRows, building.DisplayName);
            UiFactory.Label(row, $"{building.DisplayName}  {building.BuildingCost}", 22f, 360f, RowHeight);

            if (_country.IsBuilt(building))
            {
                UiFactory.Label(row, "Built", 22f, 180f, RowHeight, TextAlignmentOptions.Center);
                return;
            }

            var queued = _country.IsQueued(building);
            UiFactory.Button(row, queued ? "Unqueue" : "Queue", 180f, RowHeight, () =>
            {
                if (queued)
                    _country.RemoveBuildingFromQueue(building);
                else
                    _country.AddBuildingToQueue(building);
                Refresh();
            });
        }

        private void TrainingRow(SoldierType soldier)
        {
            var row = UiFactory.Row(_trainingRows, soldier.ToString());
            UiFactory.Label(row, $"{soldier}  {_country.GetSoldierCost(soldier)}", 22f, 300f, RowHeight);
            UiFactory.Button(row, "-", RowHeight, RowHeight, () =>
            {
                _country.RemoveSoldierFromQueue(soldier);
                Refresh();
            });
            UiFactory.Label(row, $"{_country.QueuedSoldiers(soldier)} queued", 22f, 120f, RowHeight, TextAlignmentOptions.Center);
            UiFactory.Button(row, "+", RowHeight, RowHeight, () =>
            {
                _country.AddSoldierToQueue(soldier);
                Refresh();
            });
        }
    }
}
