using Gameplay;
using UnityEngine;

namespace AI
{
    // How one difficulty plays. AiDriver holds one per difficulty; an empty slot falls back to Preset,
    // so the numbers here are the defaults to tune from.
    [CreateAssetMenu(menuName = "Create AI Profile", fileName = "AiProfile", order = 1)]
    public class AiProfile : ScriptableObject
    {
        [Header("Knowledge")]
        // Off: the AI only reads what its fog of war shows, like a human.
        [SerializeField] private bool _seesThroughFog;

        [Header("Attack")]
        // An attack is made only when a simulated battle is won at least this often.
        [SerializeField, Range(0f, 1f)] private float _minWinChance = 0.6f;
        // Chance per attack of a sloppy choice: a random target at half the usual win chance.
        [SerializeField, Range(0f, 1f)] private float _mistakeChance = 0.1f;
        // Dice rounds per turn, each one TryAttack.
        [SerializeField, Min(0)] private int _maxAttacksPerTurn = 8;
        // Battles simulated per option to estimate the win chance.
        [SerializeField, Min(1)] private int _simulations = 100;

        [Header("Economy")]
        [SerializeField] private bool _trades = true;
        // Chance to queue a building before soldiers even while a front is outnumbered.
        [SerializeField, Range(0f, 1f)] private float _buildFirstChance = 0.4f;

        [Header("Diplomacy")]
        // A treaty is accepted when the proposer's army power is at least this many times the AI's own.
        [SerializeField, Min(0f)] private float _pactAcceptRatio = 0.8f;
        [SerializeField, Min(0f)] private float _allianceAcceptRatio = 1.5f;
        // Chance per turn to send an offer of its own.
        [SerializeField, Range(0f, 1f)] private float _offerChance = 0.1f;

        [Header("Pacing")]
        // Seconds after each dice round, so the dice and the outcome can be read.
        [SerializeField, Min(0f)] private float _actionDelay = 1.6f;
        // Seconds between the camera reaching a new battle (both sides highlighted) and the first roll.
        [SerializeField, Min(0f)] private float _aimDelay = 0.7f;

        public bool SeesThroughFog => _seesThroughFog;
        public float MinWinChance => _minWinChance;
        public float MistakeChance => _mistakeChance;
        public int MaxAttacksPerTurn => _maxAttacksPerTurn;
        public int Simulations => Mathf.Max(1, _simulations);
        public bool Trades => _trades;
        public float BuildFirstChance => _buildFirstChance;
        public float PactAcceptRatio => _pactAcceptRatio;
        public float AllianceAcceptRatio => _allianceAcceptRatio;
        public float OfferChance => _offerChance;
        public float ActionDelay => _actionDelay;
        public float AimDelay => _aimDelay;

        // Easy and Normal play fair through the fog, Hard sees everything (user's decision).
        public static AiProfile Preset(Difficulty difficulty)
        {
            var profile = CreateInstance<AiProfile>();
            profile.name = $"{difficulty} AI (preset)";
            switch (difficulty)
            {
                case Difficulty.Easy:
                    profile._minWinChance = 0.5f;
                    profile._mistakeChance = 0.2f;
                    profile._maxAttacksPerTurn = 4;
                    profile._trades = false;
                    profile._buildFirstChance = 0.3f;
                    profile._pactAcceptRatio = 0.6f;
                    profile._allianceAcceptRatio = 1.2f;
                    profile._offerChance = 0.05f;
                    break;
                case Difficulty.Hard:
                    profile._seesThroughFog = true;
                    profile._minWinChance = 0.65f;
                    profile._mistakeChance = 0f;
                    profile._maxAttacksPerTurn = 12;
                    profile._simulations = 150;
                    profile._buildFirstChance = 0.45f;
                    profile._pactAcceptRatio = 1f;
                    profile._allianceAcceptRatio = 2f;
                    break;
            }
            return profile;
        }
    }
}
