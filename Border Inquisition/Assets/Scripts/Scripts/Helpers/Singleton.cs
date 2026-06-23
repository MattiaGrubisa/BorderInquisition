using UnityEngine;

namespace Scripts.Helpers
{
    public class Singleton<T>, Monobehaviour where T : Singleton<T>, new()
    {
        private static T _instance;

        protected Singleton() { }

        public static T GetInstance()
        {
            if ( _instance == null )
                _instance = new T();
            return _instance;
        }
    }
}
