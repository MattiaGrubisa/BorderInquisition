using UnityEngine;

namespace Gameplay
{
    public class Alliance : MonoBehaviour
    {
        private float _friendship;
        public float Friendship
        {
            get { return _friendship; } 
            set { _friendship += value; }
        }
    }
}