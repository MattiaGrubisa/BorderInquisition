using UnityEngine;

namespace Scripts
{
    public class Town : MonoBehaviour
    {
        private GameResources _resources;

        public enum Housing
        {
            House,
            Tavern,
            Church,
            Barracks,
            Storage
        };
    }
}