using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay
{
    public class Combat : MonoBehaviour
    {
        [SerializeField] private float _armyFactor;
        [SerializeField] private Dice _dice;

        public struct CombatResult
        {
            private int[] _attackerDice;
            private int[] _defenderDice;
            private bool[] _attackerWins;
            
            public int[] AttackerDice => _attackerDice;
            public int[] DefenderDice => _defenderDice;
            public bool[] AttackerWins => _attackerWins;
            
            public CombatResult(int[] attackerDice, int[] defenderDice, bool[] attackerWins)
            {
                _attackerDice = attackerDice;
                _defenderDice = defenderDice;
                _attackerWins = attackerWins;
            }
        }
        
        public CombatResult AttemptAttack(Player player, Country attacker, Country defender)
        {
            var attackerArmyPower = CalculateBonusArmyPower(attacker.Army);
            var defenderArmyPower = CalculateBonusArmyPower(defender.Army);
            
            int[] attackerDice = new int[attacker.Army.UniqueUnits()];
            int[] defenderDice = new int[defender.Army.UniqueUnits()];
            
            for(int i = 0; i < attackerDice.Length; i++)
                attackerDice[i] = _dice.RollDice(attackerArmyPower);
            for(int i = 0; i < defenderDice.Length; i++)
                defenderDice[i] = _dice.RollDice(defenderArmyPower);

            int[] sortedAttackerDice = attackerDice.OrderByDescending(a => a).ToArray();
            int[] sortedDefenderDice = defenderDice.OrderByDescending(a => a).ToArray();

            var pairs = Math.Min(attackerDice.Length, defenderDice.Length);
            bool[] attackerWins = new bool[Math.Max(attackerDice.Length, defenderDice.Length)];

            for (int i = 0; i < pairs; i++)
            {
                attackerWins[i] = sortedAttackerDice[i] > sortedDefenderDice[i];
            }
            
            if (attackerDice.Length > defenderDice.Length)
            {
                for (int i = defenderDice.Length; i < attackerDice.Length; i++)
                    attackerWins[i] = true;
            }
            return new CombatResult(attackerDice, defenderDice, attackerWins);
        }

        private int CalculateBonusArmyPower(Army army)
        {
            int snaga = army.Knight + (army.Archer * 2) + (army.Horseman * 3);
            float logSnaga = snaga <= 0 ? 0 : (float)Math.Log(snaga);
            return Mathf.FloorToInt(logSnaga * _armyFactor);
        }
    }
}