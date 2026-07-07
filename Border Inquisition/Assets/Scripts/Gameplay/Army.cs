using System;
using UnityEngine;

namespace Gameplay
{
    [Serializable]
    public struct Army
    {
        [SerializeField] private int _knight;
        [SerializeField] private int _horseman;
        [SerializeField] private int _archer;

        public Army(int knight, int horseman, int archer)
        {
            _knight = knight;
            _horseman = horseman;
            _archer = archer;
        }

        public void AddSoldier(int knight, int horseman, int archer)
        {
            _knight += knight;
            _horseman += horseman;
            _archer += archer;
        }
    }
}