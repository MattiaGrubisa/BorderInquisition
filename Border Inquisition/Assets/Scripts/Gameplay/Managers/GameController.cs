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
        private int _currentPlayerIndex;

        public event Action<Player> MatchWon;

        public IReadOnlyList<Country> Countries => _countries;
        public IReadOnlyList<Player> Players => _players;
        public Player CurrentPlayer => _players[_currentPlayerIndex];

        private void HandleAttack(Player playerAttacker, Player playerDefender, Country from, Country to)
        {
            var result = _combat.AttemptAttack(from, to);
            foreach (var attackerWin in result.AttackerWins)
            {
                if (attackerWin)
                    to.RemoveRandomUnit();
                else
                    from.RemoveRandomUnit();
            }

            if (!to.IsArmyEmpty)
                return;

            to.SetOwner(playerAttacker);
            if (_countries.All(c => c.Owner == playerAttacker))
                MatchWon?.Invoke(playerAttacker);
        }

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
    }
}
