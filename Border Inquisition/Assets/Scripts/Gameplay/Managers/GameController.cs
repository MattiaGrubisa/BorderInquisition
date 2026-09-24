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

        private readonly List<Player> _players = new List<Player>();
        private readonly MapGraph _map = new MapGraph();
        private int _currentPlayerIndex;

        public event Action<Player> MatchWon;

        public IReadOnlyList<Country> Countries => _countries;
        public IReadOnlyList<Player> Players => _players;
        public Player CurrentPlayer => _players[_currentPlayerIndex];
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
                MatchWon?.Invoke(attacker);
            return true;
        }

        public bool TryMoveArmy(Country from, Country to, int knights, int horsemen, int archers) =>
            CanMoveArmy(from, to) && from.SendUnits(to, knights, horsemen, archers);

        public void StartNewMatch(MatchSettings settings)
        {
            _players.Clear();
            foreach (var playerName in settings.PlayerNames)
                _players.Add(new Player(playerName));

            DistributeCountries();
            _currentPlayerIndex = DetermineStartingPlayer();
        }

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

        // Stands in for the map input that does not exist yet, so the attack path and the win
        // condition can be exercised from the inspector during Play.
        [ContextMenu("Debug: Attack A Random Target")]
        private void DebugAttack()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode first - there is no match outside it.");
                return;
            }

            foreach (var from in CurrentPlayer.OwnedCountries.ToList())
            {
                var target = AttackTargets(from).FirstOrDefault();
                if (target == null || !TryAttack(from, target, out var result))
                    continue;

                Debug.Log($"{from.name} attacks {target.name}: " +
                          $"[{string.Join(" ", result.AttackerDice)}] vs [{string.Join(" ", result.DefenderDice)}] - " +
                          $"{target.name} is held by {target.Owner?.Name ?? "nobody"}.");
                return;
            }

            Debug.Log($"{CurrentPlayer.Name} has no legal attack.");
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
