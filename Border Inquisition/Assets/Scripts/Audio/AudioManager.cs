using System.Collections;
using System.Collections.Generic;
using Gameplay.Helpers;
using GameStates;
using UnityEngine;

namespace Audio
{
    // Plays every sound and the music. Sits next to GameStateMachine in Bootstrap, so it lives for the
    // whole session (Border Inquisition > Set Up Audio puts it there). Music follows the screen and
    // crossfades between two sources; effects round-robin over a few voices so they can overlap, and
    // UI sounds have a source of their own. Volumes come from SoundSettings. Gameplay code never calls
    // it: View.SoundCues turns match events into sounds, the UI plays its own clicks.
    public class AudioManager : Singleton<AudioManager>
    {
        private const int EffectVoices = 6;
        private const float MusicFade = 1f;

        [SerializeField] private AudioLibrary _library;

        private readonly AudioSource[] _music = new AudioSource[2];
        private readonly AudioSource[] _effects = new AudioSource[EffectVoices];
        private readonly Dictionary<Sound, float> _lastPlayed = new Dictionary<Sound, float>();
        private AudioSource _ui;
        private int _activeMusic;
        private int _nextVoice;
        private AudioLibrary.MusicEntry _currentMusic;
        private Coroutine _fade;

        // Safe to call from anywhere: without a manager (a scene played on its own) nothing plays.
        public static void Play(Sound sound)
        {
            if (Instance != null)
                Instance.PlayOn(Instance.NextVoice(), sound);
        }

        public static void PlayUi(Sound sound)
        {
            if (Instance != null)
                Instance.PlayOn(Instance._ui, sound);
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
                return;

            if (_library == null)
                Debug.LogWarning("AudioManager has no AudioLibrary - the game stays silent. " +
                                 "Run Border Inquisition > Set Up Audio.", this);

            for (var i = 0; i < _music.Length; i++)
                _music[i] = CreateSource();
            for (var i = 0; i < _effects.Length; i++)
                _effects[i] = CreateSource();
            _ui = CreateSource();
        }

        // GameStateMachine enters the main menu in its Awake, before this could listen.
        private void Start()
        {
            if (GameStateMachine.Instance == null)
                return;

            GameStateMachine.Instance.StageChanged += OnStageChanged;
            OnStageChanged(GameStateMachine.Instance.Stage);
        }

        private void OnEnable() => SoundSettings.Changed += ApplyMusicVolume;

        private void OnDisable()
        {
            SoundSettings.Changed -= ApplyMusicVolume;
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.StageChanged -= OnStageChanged;
        }

        // The game over screen plays Victory when a human won, Defeat otherwise.
        private void OnStageChanged(FlowStage stage)
        {
            switch (stage)
            {
                case FlowStage.Lobby:
                    PlayMusic(Music.Lobby);
                    break;
                case FlowStage.InGame:
                    PlayMusic(Music.InGame);
                    break;
                case FlowStage.GameOver:
                    var winner = GameStateMachine.Instance.Winner;
                    PlayMusic(winner != null && !winner.IsAI ? Music.Victory : Music.Defeat);
                    break;
                default:
                    PlayMusic(Music.MainMenu);
                    break;
            }
        }

        #region Music

        // The playing track fades out while the new one fades in; a track without a clip is silence.
        private void PlayMusic(Music music)
        {
            var entry = _library != null ? _library.Find(music) : null;
            if (entry != null && entry == _currentMusic && entry.Clip != null && _music[_activeMusic].isPlaying)
                return;

            _currentMusic = entry;
            var outgoing = _music[_activeMusic];
            _activeMusic = 1 - _activeMusic;
            var incoming = _music[_activeMusic];

            incoming.Stop();
            incoming.clip = entry?.Clip;
            incoming.loop = entry?.Loop ?? true;
            incoming.volume = 0f;
            if (incoming.clip != null)
                incoming.Play();

            if (_fade != null)
                StopCoroutine(_fade);
            _fade = StartCoroutine(Crossfade(outgoing, incoming));
        }

        private IEnumerator Crossfade(AudioSource outgoing, AudioSource incoming)
        {
            var from = outgoing.volume;
            for (var t = 0f; t < MusicFade; t += Time.unscaledDeltaTime)
            {
                var k = t / MusicFade;
                outgoing.volume = Mathf.Lerp(from, 0f, k);
                incoming.volume = Mathf.Lerp(0f, MusicVolume, k);
                yield return null;
            }

            outgoing.Stop();
            incoming.volume = MusicVolume;
            _fade = null;
        }

        private void ApplyMusicVolume()
        {
            if (_fade == null)
                _music[_activeMusic].volume = MusicVolume;
        }

        private float MusicVolume => SoundSettings.Master * SoundSettings.Music * (_currentMusic?.Volume ?? 1f);

        #endregion

        #region Effects

        private void PlayOn(AudioSource source, Sound sound)
        {
            var entry = _library != null ? _library.Find(sound) : null;
            var clip = entry?.PickClip();
            if (clip == null)
                return;

            var now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(sound, out var last) && now - last < entry.Cooldown)
                return;
            _lastPlayed[sound] = now;

            source.pitch = entry.Pitch;
            source.PlayOneShot(clip, SoundSettings.Master * SoundSettings.Effects * entry.Volume);
        }

        private AudioSource NextVoice()
        {
            var voice = _effects[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _effects.Length;
            return voice;
        }

        #endregion

        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
