using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Audio
{
    // Every sound the game plays. Battle outcomes are heard from the viewer's side.
    public enum Sound
    {
        UiClick,
        PanelOpen,
        PanelClose,
        Error,
        DiceRoll,
        DiceLand,
        BattleWon,
        BattleLost,
        Conquest,
        Income,
        Built,
        Trained,
        Raze,
        Trade,
        TurnStart,
        ArmyMove,
        Offer,
        Treaty,
        Betrayal
    }

    // One track per screen; Victory and Defeat play on the game over screen.
    public enum Music
    {
        MainMenu,
        Lobby,
        InGame,
        Victory,
        Defeat
    }

    // The clips behind every Sound and Music, with how each plays. Every enum value gets a slot of its
    // own (filled in when the asset is created or edited); a slot without clips stays silent.
    [CreateAssetMenu(menuName = "Create Audio Library", fileName = "AudioLibrary", order = 2)]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public class SoundEntry
        {
            [SerializeField] private Sound _sound;
            // One is picked at random each time, so a repeated sound does not grate.
            [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
            [SerializeField, Range(0f, 1f)] private float _volume = 1f;
            // Pitch is 1 ± this, at random.
            [SerializeField, Range(0f, 0.5f)] private float _pitchVariance = 0.05f;
            // Seconds before the sound may play again; many requests at once (six dice) play it once.
            [SerializeField, Min(0f)] private float _cooldown = 0.05f;

            public SoundEntry(Sound sound) => _sound = sound;

            public Sound Sound => _sound;
            public float Volume => _volume;
            public float Cooldown => _cooldown;
            public float Pitch => 1f + Random.Range(-_pitchVariance, _pitchVariance);

            public AudioClip PickClip()
            {
                var clips = _clips.Where(clip => clip != null).ToList();
                return clips.Count == 0 ? null : clips[Random.Range(0, clips.Count)];
            }
        }

        [Serializable]
        public class MusicEntry
        {
            [SerializeField] private Music _music;
            [SerializeField] private AudioClip _clip;
            [SerializeField, Range(0f, 1f)] private float _volume = 1f;
            [SerializeField] private bool _loop = true;

            public MusicEntry(Music music) => _music = music;

            public Music Music => _music;
            public AudioClip Clip => _clip;
            public float Volume => _volume;
            public bool Loop => _loop;
        }

        [SerializeField] private List<SoundEntry> _sounds = new List<SoundEntry>();
        [SerializeField] private List<MusicEntry> _music = new List<MusicEntry>();

        public SoundEntry Find(Sound sound) => _sounds.FirstOrDefault(entry => entry.Sound == sound);
        public MusicEntry Find(Music music) => _music.FirstOrDefault(entry => entry.Music == music);

        private void Reset() => FillSlots();
        private void OnValidate() => FillSlots();

        // A slot for every enum value that has none, in enum order after the existing ones. Public for the
        // editor setup, which makes the asset in code, where Reset does not run.
        public void FillSlots()
        {
            foreach (Sound sound in Enum.GetValues(typeof(Sound)))
                if (Find(sound) == null)
                    _sounds.Add(new SoundEntry(sound));

            foreach (Music music in Enum.GetValues(typeof(Music)))
                if (Find(music) == null)
                    _music.Add(new MusicEntry(music));
        }
    }
}
