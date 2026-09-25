using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    // The rules of the game that do not change from match to match, kept out of the scene so they
    // survive a component reset and a future server can load them without WorldMap. GameController
    // holds the one asset and hands it to the systems that need it.
    [CreateAssetMenu(menuName = "Create Game Rules", fileName = "GameRules", order = 0)]
    public class GameRules : ScriptableObject
    {
        [Header("Buildings")]
        // Every building a country can put in its build queue.
        [SerializeField] private List<Building> _buildings = new List<Building>();

        [Header("Match setup")]
        // Stand-in until starting values are designed: every country starts with a random army and a
        // random income, both inclusive ranges.
        [SerializeField] private Vector2Int _randomUnitsPerType = new Vector2Int(0, 4);
        [SerializeField] private Vector2Int _randomResourceGain = new Vector2Int(0, 3);

        // Every face of the income die pays out on at least min and at most max countries.
        [SerializeField] private int _minCountriesPerDiceNumber = 2;
        [SerializeField] private int _maxCountriesPerDiceNumber = 5;

        [Header("Armies")]
        // Units a move must leave in the country it starts from.
        [SerializeField, Min(0)] private int _minimumGarrison = 1;

        [Header("Market")]
        // Bank trade ratio without a trade building: give this many for 1.
        [SerializeField, Min(1)] private int _defaultTradeRatio = 4;

        [Header("Diplomacy")]
        // How many of the proposer's turns a pact covers.
        [SerializeField, Min(1)] private int _pactTurns = 3;

        public IReadOnlyList<Building> Buildings => _buildings;
        public Vector2Int RandomUnitsPerType => _randomUnitsPerType;
        public Vector2Int RandomResourceGain => _randomResourceGain;
        public int MinCountriesPerDiceNumber => _minCountriesPerDiceNumber;
        public int MaxCountriesPerDiceNumber => _maxCountriesPerDiceNumber;
        public int MinimumGarrison => _minimumGarrison;
        public int DefaultTradeRatio => _defaultTradeRatio;
        public int PactTurns => _pactTurns;
    }
}
