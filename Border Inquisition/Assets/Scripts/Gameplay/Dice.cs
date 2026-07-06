using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Scripts
{
    public class Dice : MonoBehaviour
    {
        private int _rolledDice;

        public int RolledDice
        {
            get { return _rolledDice; }
            set
            {
                if (value is < 1 or > 6)
                {
                    _rolledDice = value;
                   //call anime or smth 
                } 
                else
                    Debug.Log("Dice value out of range: " +  value);
            }
        }
        
        private int randomNumber => Random.Range(1, 6);
        public void SetDice() => RolledDice = randomNumber;
    }
}