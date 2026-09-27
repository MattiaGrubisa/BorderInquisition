using System;
using System.Collections.Generic;
using Gameplay;

namespace Save
{
    // A running match as plain data, written as JSON (JsonUtility) at the start of every turn, once
    // the income is paid. Countries are addressed by id, players by seat index (-1 = nobody),
    // buildings and regions by their asset / object name. What the scene and the assets already hold
    // (borders, regions, prices) and what is derived (fog, ratios, discounts) is not stored.
    [Serializable]
    public class MatchSave
    {
        // A save of another format version is not read.
        public int FormatVersion;
        public string GameVersion;
        // UTC, ISO 8601 ("o").
        public string SavedAtUtc;

        public int CurrentPlayer;
        public int LastHuman = -1;
        public int LastIncomeRoll;

        public List<PlayerSave> Players = new List<PlayerSave>();
        public List<CountrySave> Countries = new List<CountrySave>();
        public List<TreatySave> Treaties = new List<TreatySave>();
        public List<OfferSave> Offers = new List<OfferSave>();
    }

    [Serializable]
    public class PlayerSave
    {
        public string Name;
        public bool IsAI;
        public Difficulty Difficulty;
        public GameResources Resources;
        public ReportSave Report = new ReportSave();
    }

    [Serializable]
    public class CountrySave
    {
        public int Id;
        public int Owner = -1;
        public int DiceNumber;
        public GameResources BaseResourceGain;
        public Army Army;
        public List<string> Built = new List<string>();
        public List<string> BuildingQueue = new List<string>();
        public List<SoldierType> TrainingQueue = new List<SoldierType>();
    }

    [Serializable]
    public class TreatySave
    {
        public TreatyKindSave Kind;
        public int Proposer;
        public int Partner;
        public int TurnsLeft;
        public int BrokenBy = -1;
    }

    [Serializable]
    public class OfferSave
    {
        public TreatyKindSave Kind;
        public int From;
        public int To;
    }

    // Mirrors Diplomacy.TreatyKind, so the save format does not move if that enum does.
    public enum TreatyKindSave
    {
        Pact,
        Alliance
    }

    [Serializable]
    public class ReportSave
    {
        public List<int> Rolls = new List<int>();
        public List<PayoutSave> Payouts = new List<PayoutSave>();
        public List<RegionBonusSave> RegionBonuses = new List<RegionBonusSave>();
        public List<DeliverySave> Deliveries = new List<DeliverySave>();
        public List<DefenceSave> Defences = new List<DefenceSave>();
    }

    [Serializable]
    public class PayoutSave
    {
        public int RollIndex;
        public int Country;
        public GameResources Amount;
    }

    [Serializable]
    public class RegionBonusSave
    {
        public string Region;
        public GameResources Amount;
    }

    // A building when Building is set, otherwise Count soldiers of one type.
    [Serializable]
    public class DeliverySave
    {
        public int Country;
        public string Building;
        public SoldierType Soldier;
        public int Count;
    }

    [Serializable]
    public class DefenceSave
    {
        public int Attacker;
        public int Country;
        public int Attacks;
        public int UnitsLost;
        public int UnitsKilled;
        public bool Fallen;
        public List<string> Razed = new List<string>();
    }
}
