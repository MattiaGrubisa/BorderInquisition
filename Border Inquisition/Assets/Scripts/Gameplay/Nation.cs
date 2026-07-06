using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class Nation : ScriptableObject
    {
        private Town _town;
        private GameResources _resources;
        private List<Nation> _neighbours;
    }
    
}
