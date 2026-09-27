using UnityEngine;

namespace AI
{
    public enum AiPace
    {
        Normal,
        Fast,
        Instant
    }

    // How quickly AI turns play out: the viewer's preference, kept between sessions (pause menu).
    // Instant skips the camera and every pause, so a round of AI turns passes in a few frames.
    public static class AiSettings
    {
        private const string PaceKey = "AiPace";
        private const float FastFactor = 0.35f;

        private static AiPace? _pace;

        public static AiPace Pace
        {
            get => _pace ??= (AiPace)Mathf.Clamp(PlayerPrefs.GetInt(PaceKey, 0), 0, (int)AiPace.Instant);
            set
            {
                _pace = value;
                PlayerPrefs.SetInt(PaceKey, (int)value);
                // Written at once, so a crash does not lose it; it changes rarely.
                PlayerPrefs.Save();
            }
        }

        // What every AI pause is multiplied by.
        public static float PaceFactor => Pace == AiPace.Normal ? 1f : Pace == AiPace.Fast ? FastFactor : 0f;

        public static AiPace NextPace() => Pace = Pace == AiPace.Instant ? AiPace.Normal : Pace + 1;
    }
}
