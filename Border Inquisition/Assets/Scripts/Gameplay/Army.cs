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
                
        public bool IsArmyEmpty() => (_knight == 0 && _horseman == 0 && _archer == 0);
        public double ArmyPower => _knight + _horseman * 1.3 + _archer * 1.2;

        public int Knights => _knight;
        public int Horsemen => _horseman;
        public int Archers => _archer;

        public bool Contains(int knight, int horseman, int archer) =>
            _knight >= knight && _horseman >= horseman && _archer >= archer;

        public void AddUnit(int knight, int horseman, int archer)
        {
            _knight += knight;
            _horseman += horseman;
            _archer += archer;
        }

        public void RemoveUnit(int knight, int horseman, int archer)
        {
            _knight = Mathf.Max(0, _knight - knight);
            _horseman = Mathf.Max(0, _horseman - horseman);
            _archer = Mathf.Max(0, _archer - archer);
        }

        public void RemoveRandomUnit()
        {
            int total = _knight + _horseman + _archer;
            if (total <= 0) return;

            int roll = UnityEngine.Random.Range(0, total);

            if (roll < _knight)
                _knight--;
            else if (roll < _knight + _horseman)
                _horseman--;
            else
                _archer--;
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