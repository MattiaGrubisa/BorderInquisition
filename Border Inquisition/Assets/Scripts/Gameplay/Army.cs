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
        
        public int Knight => _knight;
        public int Horseman => _horseman;
        public int Archer => _archer;
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

        public int UniqueUnits()
        {
            int unique = 0;
            if(_knight >= 1)
                unique += 1;
            if(_horseman >= 1)
                unique += 1;
            if(_archer >= 1)
                unique += 1;
            return unique;
        }
    }
}