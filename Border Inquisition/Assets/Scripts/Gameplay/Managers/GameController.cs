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
        
        private int _currentPlayerIndex;
        private bool _newGame = true;
        
        protected override void Awake()
        {
            base.Awake();
        }
        
        private void HandleAttack(Player playerAttacker, Player playerDefender, Country from,  Country to)
        {
            var result = _combat.AttemptAttack(playerAttacker, from, to);
            foreach (var attackerWin in result.AttackerWins)
            {
                if(attackerWin)
                    to.RemoveRandomUnit();
                else
                    from.RemoveRandomUnit();
            }

            if (IsConquered(to))
            {
                playerAttacker.AddCountry(to);
                playerDefender.RemoveCountry(to);
            }
        }

        private bool IsConquered(Country country) => !country.AnyArmy();

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
