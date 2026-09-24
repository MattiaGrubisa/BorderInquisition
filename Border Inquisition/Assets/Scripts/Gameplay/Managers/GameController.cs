using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Helpers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Managers
{
    public class GameController : Singleton<GameController>
    {
        [SerializeField] private Combat _combat;
        [SerializeField] private Dice _dice;
        [SerializeField] private List<Country> _countries;

        // Temporary match setup: every country starts with a random army and a random income, both inclusive ranges.
        [SerializeField] private Vector2Int _randomUnitsPerType = new Vector2Int(0, 4);
        [SerializeField] private Vector2Int _randomResourceGain = new Vector2Int(0, 3);

        // Every face of the income die pays out on at least min and at most max countries.
        [SerializeField] private int _minCountriesPerDiceNumber = 2;
        [SerializeField] private int _maxCountriesPerDiceNumber = 5;
        private const int DiceFaces = 9;

        private readonly List<Player> _players = new List<Player>();
        private readonly MapGraph _map = new MapGraph();
        private int _currentPlayerIndex;

        public event Action<Player> MatchWon;

        public IReadOnlyList<Country> Countries => _countries;
        public IReadOnlyList<Player> Players => _players;
        public Player CurrentPlayer => _players[_currentPlayerIndex];
        public Player Winner { get; private set; }
        public MapGraph Map => _map;

        protected override void Awake()
        {
            base.Awake();
            _map.Bake(_countries);
        }

        #region Attack legality

        // An attack is legal between neighbours the current player owns one side of, and only while
        // the attacking country still has an army to send.
        public bool CanAttack(Country from, Country to) =>
            from != null && to != null
            && _players.Count > 0 && from.Owner == CurrentPlayer
            && to.Owner != from.Owner
            && !from.IsArmyEmpty
            && _map.AreNeighbours(from, to);

        public IEnumerable<Country> AttackTargets(Country from) =>
            _map.Neighbours(from).Where(to => CanAttack(from, to));

        // Reinforcements travel between neighbouring countries the current player already holds.
        public bool CanMoveArmy(Country from, Country to) =>
            from != null && to != null
            && _players.Count > 0 && from.Owner == CurrentPlayer
            && to.Owner == CurrentPlayer
            && _map.AreNeighbours(from, to);

        #endregion

        // The caller gets the dice back for display; the outcome is already applied.
        public bool TryAttack(Country from, Country to, out Combat.CombatResult result)
        {
            result = default;
            if (!CanAttack(from, to))
                return false;

            var attacker = from.Owner;
            result = _combat.AttemptAttack(from, to);
            foreach (var attackerWin in result.AttackerWins)
            {
                if (attackerWin)
                    to.RemoveRandomUnit();
                else
                    from.RemoveRandomUnit();
            }

            if (!to.IsArmyEmpty)
                return true;

            to.SetOwner(attacker);
            from.SendWholeArmy(to);

            if (_countries.All(c => c.Owner == attacker))
            {
                Winner = attacker;
                MatchWon?.Invoke(attacker);
            }
            return true;
        }

        public bool TryMoveArmy(Country from, Country to, int knights, int horsemen, int archers) =>
            CanMoveArmy(from, to) && from.SendUnits(to, knights, horsemen, archers);

        public void StartNewMatch(MatchSettings settings)
        {
            _players.Clear();
            Winner = null;
            foreach (var playerName in settings.PlayerNames)
                _players.Add(new Player(playerName));

            DistributeCountries();
            RandomizeCountries();
            AssignDiceNumbers();
            _currentPlayerIndex = DetermineStartingPlayer();
        }

        // Each number 1-9 is put in the pool _minCountriesPerDiceNumber times, the rest of the pool is
        // drawn at random from the numbers still under _maxCountriesPerDiceNumber, and the pool is
        // shuffled over the countries.
        private void AssignDiceNumbers()
        {
            if (_countries.Count < DiceFaces * _minCountriesPerDiceNumber
                || _countries.Count > DiceFaces * _maxCountriesPerDiceNumber)
            {
                Debug.LogWarning($"{_countries.Count} countries cannot carry every dice number between " +
                                 $"{_minCountriesPerDiceNumber} and {_maxCountriesPerDiceNumber} times.", this);
            }

            var numbers = new List<int>();
            var counts = new int[DiceFaces + 1];
            for (var copy = 0; copy < _minCountriesPerDiceNumber; copy++)
            {
                for (var number = 1; number <= DiceFaces; number++)
                {
                    numbers.Add(number);
                    counts[number]++;
                }
            }

            while (numbers.Count < _countries.Count)
            {
                var open = Enumerable.Range(1, DiceFaces).Where(n => counts[n] < _maxCountriesPerDiceNumber).ToList();
                var number = open.Count > 0 ? open[Random.Range(0, open.Count)] : Random.Range(1, DiceFaces + 1);
                numbers.Add(number);
                counts[number]++;
            }

            var shuffled = numbers.OrderBy(_ => Random.value).ToList();
            for (var i = 0; i < _countries.Count; i++)
                _countries[i].SetDiceNumber(shuffled[i]);
        }

        // Stand-in until starting armies and incomes are designed; an empty army is topped up with a
        // knight so no country starts out defenceless.
        private void RandomizeCountries()
        {
            foreach (var country in _countries)
            {
                var army = new Army(RandomUnits(), RandomUnits(), RandomUnits());
                if (army.IsArmyEmpty())
                    army.AddUnit(1, 0, 0);

                country.SetArmy(army);
                country.SetBaseResourceGain(new GameResources(
                    RandomGain(), RandomGain(), RandomGain(), RandomGain()));
            }
        }

        private int RandomUnits() => Random.Range(_randomUnitsPerType.x, _randomUnitsPerType.y + 1);
        private int RandomGain() => Random.Range(_randomResourceGain.x, _randomResourceGain.y + 1);

        // Shuffled and dealt round-robin, so every player starts with an equal share (±1).
        private void DistributeCountries()
        {
            var shuffled = _countries.OrderBy(_ => Random.value).ToList();
            for (int i = 0; i < shuffled.Count; i++)
                shuffled[i].SetOwner(_players[i % _players.Count]);
        }

        public int DetermineStartingPlayer()
        {
            var candidates = new List<int>();
            for (int i = 0; i < _players.Count; i++)
                candidates.Add(i);

            while (candidates.Count > 1)
            {
                int highest = 0;
                var winners = new List<int>();

                foreach (var index in candidates)
                {
                    int roll = _dice.RollDice(0);

                    if (roll > highest)
                    {
                        highest = roll;
                        winners.Clear();
                        winners.Add(index);
                    }
                    else if (roll == highest)
                    {
                        winners.Add(index);
                    }
                }

                candidates = winners;
            }

            return candidates[0];
        }

        public void PhaseOne()
        {
            CurrentPlayer.PhaseOne();
            var dice = _dice.RollDice(0);
            foreach (var p in _players)
            {
                p.GainResources(dice);
            }
        }

        // Skips players who lost all their countries; bounded so it can never spin forever.
        public void NextPlayer()
        {
            for (int i = 0; i < _players.Count; i++)
            {
                _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
                if (!IsEliminated(CurrentPlayer))
                    return;
            }
        }

        // An empty test map eliminates nobody, so turns still rotate while the map is being built.
        private bool IsEliminated(Player player) => _countries.Count > 0 && !player.OwnedCountries.Any();

        #region Simulation

        // Plays the match out without input: each turn the current player attacks until no legal attack
        // is left, then idle armies march one step towards the nearest enemy. Ends on a winner, or with
        // a warning when every army is gone or the turn limit runs out.
        public void PlayOutMatch(int maxTurns = 1000)
        {
            if (_players.Count == 0)
            {
                Debug.LogWarning("No match is running.", this);
                return;
            }

            var turns = 0;
            var attacks = 0;
            while (Winner == null && turns < maxTurns && _countries.Any(c => !c.IsArmyEmpty))
            {
                attacks += PlayOutTurn();
                turns++;
                if (Winner == null)
                    NextPlayer();
            }

            if (Winner != null)
                Debug.Log($"{Winner.Name} wins after {turns} turns and {attacks} attacks.", this);
            else
                Debug.LogWarning($"No winner after {turns} turns and {attacks} attacks - the match is stuck.", this);
        }

        private int PlayOutTurn()
        {
            const int maxAttacks = 10000;

            var attacks = 0;
            while (Winner == null && attacks < maxAttacks && TryBestAttack())
                attacks++;

            if (Winner == null)
                AdvanceIdleArmies();
            return attacks;
        }

        // Picks the legal attack with the biggest power advantage.
        private bool TryBestAttack()
        {
            var best = CurrentPlayer.OwnedCountries
                .SelectMany(from => AttackTargets(from).Select(to => (from, to)))
                .OrderByDescending(pair => pair.from.GetArmyPower - pair.to.GetArmyPower)
                .FirstOrDefault();

            return best.from != null && TryAttack(best.from, best.to, out _);
        }

        private void AdvanceIdleArmies()
        {
            var idle = CurrentPlayer.OwnedCountries
                .Where(c => !c.IsArmyEmpty && !AttackTargets(c).Any())
                .ToList();

            foreach (var from in idle)
            {
                var step = NextStepTowardsEnemy(from);
                if (step != null)
                    TryMoveArmy(from, step, from.Army.Knights, from.Army.Horsemen, from.Army.Archers);
            }
        }

        // Breadth-first search through the owner's own countries; returns the first step of the shortest
        // path to a country someone else holds, or null when there is none.
        private Country NextStepTowardsEnemy(Country from)
        {
            var firstStep = new Dictionary<int, Country>();
            var visited = new HashSet<int> { from.Id };
            var pending = new Queue<Country>();
            pending.Enqueue(from);

            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var neighbour in _map.Neighbours(current))
                {
                    if (!visited.Add(neighbour.Id))
                        continue;

                    var step = current == from ? neighbour : firstStep[current.Id];
                    if (neighbour.Owner != from.Owner)
                        return step;

                    firstStep[neighbour.Id] = step;
                    pending.Enqueue(neighbour);
                }
            }

            return null;
        }

        #endregion

#if UNITY_EDITOR

        #region Map authoring

        [ContextMenu("Assign Country Ids")]
        private void AssignCountryIds()
        {
            var used = new HashSet<int>();
            foreach (var country in _countries)
                if (country != null && country.Id >= 0)
                    used.Add(country.Id);

            var next = 0;
            var assigned = 0;
            foreach (var country in _countries)
            {
                if (country == null || country.Id >= 0)
                    continue;

                while (!used.Add(next))
                    next++;

                country.SetId(next);
                UnityEditor.EditorUtility.SetDirty(country);
                assigned++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"Assigned {assigned} new country id(s); {_countries.Count} countries on the map.");
        }

        // Every country's parent object is its continent: it gets a Region, and the country points at it.
        [ContextMenu("Assign Regions")]
        private void AssignRegions()
        {
            var assigned = 0;
            foreach (var country in _countries)
            {
                if (country == null)
                    continue;

                var parent = country.transform.parent;
                if (parent == null)
                {
                    Debug.LogWarning($"{country.name} has no continent parent.", country);
                    continue;
                }

                var region = parent.GetComponent<Region>();
                if (region == null)
                    region = UnityEditor.Undo.AddComponent<Region>(parent.gameObject);

                UnityEditor.Undo.RecordObject(country, "Assign Region");
                country.SetRegion(region);
                UnityEditor.EditorUtility.SetDirty(country);
                assigned++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"Assigned a region to {assigned} countries.", this);
        }

        // Bakes the graph and reports what would break a match: unreachable groups, stray borders.
        [ContextMenu("Validate Map")]
        private void ValidateMap()
        {
            _map.Bake(_countries);

            var groups = _map.ConnectedGroups();
            var sizes = string.Join(", ", groups.Select(group => group.Count));
            var borders = _countries.Where(c => c != null).Sum(c => _map.NeighbourIds(c.Id).Count) / 2;

            if (groups.Count > 1)
                Debug.LogWarning($"The map falls apart into {groups.Count} groups ({sizes}); " +
                                 "no player can conquer every country until they are linked.", this);
            else
                Debug.Log($"The map is one connected group of {sizes} countries.", this);

            Debug.Log($"{_countries.Count} countries, {borders} borders.", this);
        }

        #endregion

#endif
    }
}
