using UnityEngine;

namespace Gameplay
{
    [System.Serializable]
    public struct GameResources
    {   
        [SerializeField] private int _food;
        [SerializeField] private int _wood;
        [SerializeField] private int _gold;
        [SerializeField] private int _stone;
        
        public GameResources(int food, int wood, int gold, int stone)
        {
            _food = food;
            _wood = wood;
            _stone = stone;
            _gold = gold;
        }

        public static bool operator >=(GameResources r1, GameResources r2) =>
            r1._food >= r2._food 
            && r1._gold >= r2._gold 
            && r1._stone >= r2._stone
            && r1._wood >= r2._wood;

        public static bool operator <=(GameResources r1, GameResources r2) =>
            r1._food <= r2._food 
            && r1._gold <= r2._gold 
            && r1._stone <= r2._stone
            && r1._wood <= r2._wood;

        public static GameResources operator +(GameResources r1, GameResources r2)
        {
            r1._food += r2._food;
            r1._wood += r2._wood;
            r1._gold += r2._gold;
            r1._stone += r2._stone;
            return r1;
        }

        public static GameResources operator -(GameResources r1, GameResources r2)
        {
            r1._food -= r2._food;
            r1._wood -= r2._wood;
            r1._gold -= r2._gold;
            r1._stone -= r2._stone;
            return r1;
        }
    }
    
}
