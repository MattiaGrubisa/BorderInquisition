using UnityEngine;

namespace Core
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;
        public static T Instance { get { return _instance; } }

        protected void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogError($"Duplicate {typeof(T)}, destroying", this);
                Destroy(gameObject);
                return;
            }
            else
                _instance = (T)this;
            OnAwake();
        }

        protected void OnDestroy()
        {
            if(_instance != this) return;
            OnSingletonDestroy();
            _instance = null;
        }

        protected virtual void OnAwake() {}
        protected virtual void OnSingletonDestroy() {}
    }
}
