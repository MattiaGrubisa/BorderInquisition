using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay
{
    // Rolls only; showing a roll is the UI's job (UI.DieRoll).
    public class Dice : MonoBehaviour
    {
        public int RollDice(int minNumber) => Random.Range(Mathf.Clamp(minNumber, 1, 8), 10);
    }
}
