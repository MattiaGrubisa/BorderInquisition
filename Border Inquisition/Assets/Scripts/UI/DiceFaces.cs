using System;
using System.Linq;
using UnityEngine;

namespace UI
{
    // The nine faces of the d9, drawn white with black pips so the UI can tint them per player.
    [Serializable]
    public class DiceFaces
    {
        private const int FaceCount = 9;

        // Face n at index n - 1.
        [SerializeField] private Sprite[] _faces = new Sprite[FaceCount];

        public bool IsComplete => _faces != null && _faces.Length == FaceCount && _faces.All(face => face != null);

        public Sprite Face(int value) =>
            _faces != null && value >= 1 && value <= _faces.Length ? _faces[value - 1] : null;
    }
}
