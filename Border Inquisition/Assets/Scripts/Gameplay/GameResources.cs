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

        public int Food => _food;
        public int Wood => _wood;
        public int Gold => _gold;
        public int Stone => _stone;

        public int Get(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Food:
                    return _food;
                case ResourceType.Wood:
                    return _wood;
                case ResourceType.Gold:
                    return _gold;
                case ResourceType.Stone:
                    return _stone;
            }
            return 0;
        }

        // An amount of a single resource, for trades.
        public static GameResources Of(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Food:
                    return new GameResources(amount, 0, 0, 0);
                case ResourceType.Wood:
                    return new GameResources(0, amount, 0, 0);
                case ResourceType.Gold:
                    return new GameResources(0, 0, amount, 0);
                case ResourceType.Stone:
                    return new GameResources(0, 0, 0, amount);
            }
            return default;
        }

        public override string ToString() => $"F{_food} W{_wood} G{_gold} S{_stone}";

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
