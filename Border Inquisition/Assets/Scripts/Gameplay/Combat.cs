using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay
{
    public class Combat : MonoBehaviour
    {
        [SerializeField] private float _armyFactor;
        [SerializeField] private Dice _dice;

        // TODO: Separate function for caluclating difference in dice roll for losses per dice
        public void AttemptAttack(Player player, Country attacker, Country defender)
        {
            var attackerArmyPower = CalculateBonusArmyPower(attacker.Army);
            var defenderArmyPower = CalculateBonusArmyPower(defender.Army);
            
            var attackerDiceThrows = attacker.Army.UniqueUnits();
            var defenderDiceThrows = defender.Army.UniqueUnits();

            int[] attackerDice = new int[attackerDiceThrows];
            int[] defenderDice = new int[defenderDiceThrows];
            
            for(int i = 0; i < attackerDiceThrows; i++)
                attackerDice[i] = _dice.RollDice(attackerArmyPower);
            for(int i = 0; i < defenderDiceThrows; i++)
                defenderDice[i] = _dice.RollDice(defenderArmyPower);

            Array.Sort(attackerDice);
            Array.Sort(defenderDice);

            var diceComparison = Math.Min(attackerDice.Length, defenderDice.Length);
            
            // Return dice numbers and calculated losses
            for (int i = 0; i < diceComparison; i++)
            {
                if (attackerDice[i] > defenderDice[i])
                {
                    
                }
            }
        }

        private int CalculateBonusArmyPower(Army army)
        {
            int snaga = army.Knight + (army.Archer * 2) + (army.Horseman * 3);
            float logSnaga = snaga <= 0 ? 0 : (float)Math.Log(snaga);
            return Mathf.FloorToInt(logSnaga * _armyFactor);
        }
    }
}