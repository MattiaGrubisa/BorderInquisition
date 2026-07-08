using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay
{
    public class Dice : MonoBehaviour
    {
        public int RolledDice { get; set; }

        public int RollDice(int minNumber)
        {
            RolledDice = Random.Range(1 + Mathf.Clamp(minNumber,0, 8), 10);
            DiceRollAnimation(RolledDice);
            return RolledDice;
        }

        // TODO
        private void DiceRollAnimation(int diceNumber) {}
    }
}