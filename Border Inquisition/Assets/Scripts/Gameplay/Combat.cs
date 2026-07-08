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
            
            var attackerUniqueUnits = attacker.Army.UniqueUnits();
            var defenderUniqueUnits = defender.Army.UniqueUnits();
            
            var attackerDice= _dice.RollDice(attackerArmyPower);
            var defendersDice = _dice.RollDice(defenderArmyPower);
            
            
        }

        private int CalculateBonusArmyPower(Army army)
        {
            int snaga = army.Knight + (army.Archer * 2) + (army.Horseman * 3);
            float logSnaga = snaga <= 0 ? 0 : (float)Math.Log(snaga);
            return Mathf.FloorToInt(logSnaga * _armyFactor);
        }
    }
}