using System;
using UnityEngine;

namespace Audio
{
    // Master, music and effects volume, 0-1: the player's preference, kept between sessions (pause menu
    // sliders). A slider fires many changes, so they are written to disk by Save, when the menu closes.
    public static class SoundSettings
    {
        private const string MasterKey = "VolumeMaster";
        private const string MusicKey = "VolumeMusic";
        private const string EffectsKey = "VolumeEffects";

        private static float? _master;
        private static float? _music;
        private static float? _effects;

        public static event Action Changed;

        public static float Master
        {
            get => _master ??= Load(MasterKey);
            set => Set(ref _master, MasterKey, value);
        }

        public static float Music
        {
            get => _music ??= Load(MusicKey);
            set => Set(ref _music, MusicKey, value);
        }

        public static float Effects
        {
            get => _effects ??= Load(EffectsKey);
            set => Set(ref _effects, EffectsKey, value);
        }

        public static void Save() => PlayerPrefs.Save();

        private static float Load(string key) => Mathf.Clamp01(PlayerPrefs.GetFloat(key, 1f));

        private static void Set(ref float? field, string key, float value)
        {
            value = Mathf.Clamp01(value);
            field = value;
            PlayerPrefs.SetFloat(key, value);
            Changed?.Invoke();
        }
    }
}
