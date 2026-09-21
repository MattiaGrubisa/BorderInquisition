using System;
using System.Collections.Generic;
using Gameplay.Helpers;
using UnityEngine;

namespace Gameplay.Managers
{
    public class GameController : Singleton<GameController>
    {
        [SerializeField] private List<Player> _players;
        [SerializeField] private Combat _combat;
        [SerializeField] private Dice _dice;
        [SerializeField] private List<Country> _countries;
        
        private int _currentPlayerIndex;
        private bool _newGame = true;
        
        public IReadOnlyList<Country> Countries => _countries;
 
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

            if (to.IsArmyEmpty)
                to.SetOwner(playerAttacker);
        }

        private void Start()
        {
            if (_newGame)
                _currentPlayerIndex = DetermineStartingPlayer();
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
            _players[_currentPlayerIndex].PhaseOne();
            var dice = _dice.RollDice(0);
            foreach (var p in _players)
            {
                p.GainResources(dice);
            }
        }
        
        public void NextPlayer() => _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
    }
}
