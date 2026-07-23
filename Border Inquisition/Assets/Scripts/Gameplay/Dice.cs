using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay
{
    public class Dice : MonoBehaviour
    {
        public int RollDice(int minNumber)
        {
            var diceRoll = Random.Range(1 + Mathf.Clamp(minNumber,0, 8), 10);
            DiceRollAnimation(diceRoll);
            return diceRoll;
        }

        // TODO
        private void DiceRollAnimation(int diceNumber) {}
    }
}