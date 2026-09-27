using System.Collections.Generic;
using System.Linq;
using Diplomacy;
using Gameplay;
using Gameplay.Managers;
using UnityEngine;

namespace Save
{
    // Turns the running match into a MatchSave and back. Only called between phases (after income),
    // so nothing of a turn in progress - the move used, countries conquered this turn - is stored.
    public static class MatchSnapshot
    {
        #region Capture

        public static MatchSave Capture(GameController game)
        {
            var players = game.Players;
            var save = new MatchSave
            {
                CurrentPlayer = SeatOf(players, game.CurrentPlayer),
                LastHuman = SeatOf(players, game.LastHuman),
                LastIncomeRoll = game.LastIncomeRoll
            };

            foreach (var player in players)
            {
                save.Players.Add(new PlayerSave
                {
                    Name = player.Name,
                    IsAI = player.IsAI,
                    Difficulty = player.Difficulty,
                    Resources = player.Resources,
                    Report = CaptureReport(player.Report, players)
                });
            }

            foreach (var country in game.Countries)
            {
                save.Countries.Add(new CountrySave
                {
                    Id = country.Id,
                    Owner = SeatOf(players, country.Owner),
                    DiceNumber = country.DiceNumber,
                    BaseResourceGain = country.BaseResourceGain,
                    Army = country.Army,
                    Built = country.BuiltBuildings.Select(building => building.name).ToList(),
                    BuildingQueue = country.BuildingQueue.Select(building => building.name).ToList(),
                    TrainingQueue = country.TrainingQueue.ToList()
                });
            }

            foreach (var treaty in game.Diplomacy.Treaties)
            {
                save.Treaties.Add(new TreatySave
                {
                    Kind = (TreatyKindSave)treaty.Kind,
                    Proposer = SeatOf(players, treaty.Proposer),
                    Partner = SeatOf(players, treaty.Partner),
                    TurnsLeft = treaty.TurnsLeft,
                    BrokenBy = SeatOf(players, treaty.BrokenBy)
                });
            }

            foreach (var offer in game.Diplomacy.Offers)
            {
                save.Offers.Add(new OfferSave
                {
                    Kind = (TreatyKindSave)offer.Kind,
                    From = SeatOf(players, offer.From),
                    To = SeatOf(players, offer.To)
                });
            }

            return save;
        }

        private static ReportSave CaptureReport(TurnReport report, IReadOnlyList<Player> players)
        {
            var save = new ReportSave { Rolls = report.Rolls.ToList() };

            foreach (var payout in report.Payouts)
                save.Payouts.Add(new PayoutSave { RollIndex = payout.RollIndex, Country = payout.Country.Id, Amount = payout.Amount });

            foreach (var (region, amount) in report.RegionBonuses)
                save.RegionBonuses.Add(new RegionBonusSave { Region = region.name, Amount = amount });

            foreach (var delivery in report.Deliveries)
            {
                save.Deliveries.Add(new DeliverySave
                {
                    Country = delivery.Country.Id,
                    Building = delivery.Building != null ? delivery.Building.name : string.Empty,
                    Soldier = delivery.Soldier,
                    Count = delivery.Count
                });
            }

            foreach (var defence in report.Defences)
            {
                save.Defences.Add(new DefenceSave
                {
                    Attacker = SeatOf(players, defence.Attacker),
                    Country = defence.Country.Id,
                    Attacks = defence.Attacks,
                    UnitsLost = defence.UnitsLost,
                    UnitsKilled = defence.UnitsKilled,
                    Fallen = defence.Fallen,
                    Razed = defence.Razed.Select(building => building.name).ToList()
                });
            }

            return save;
        }

        private static int SeatOf(IReadOnlyList<Player> players, Player player)
        {
            for (var i = 0; i < players.Count; i++)
                if (players[i] == player)
                    return i;
            return -1;
        }

        #endregion

        #region Restore

        // False, with nothing changed, when the save does not fit this map (a country missing, a seat
        // out of range). A building or region no longer in the game is skipped with a warning.
        public static bool TryRestore(GameController game, MatchSave save)
        {
            var saved = save.Countries.ToDictionary(country => country.Id);
            var missing = game.Countries.Where(country => !saved.ContainsKey(country.Id)).ToList();
            if (missing.Count > 0)
            {
                Debug.LogError($"The save does not fit this map: {missing.Count} countries missing, e.g. {missing[0].name}.");
                return false;
            }

            if (save.Players.Count == 0 || save.CurrentPlayer < 0 || save.CurrentPlayer >= save.Players.Count)
            {
                Debug.LogError("The save has no valid current player.");
                return false;
            }

            var players = save.Players.Select(RestorePlayer).ToList();
            game.ResumeMatch(players, save.CurrentPlayer, Seat(players, save.LastHuman), save.LastIncomeRoll);

            foreach (var country in game.Countries)
            {
                var data = saved[country.Id];
                country.SetOwner(Seat(players, data.Owner));
                country.SetDiceNumber(data.DiceNumber);
                country.SetBaseResourceGain(data.BaseResourceGain);
                country.SetArmy(data.Army);
                country.RestoreQueues(
                    Buildings(game, data.Built),
                    Buildings(game, data.BuildingQueue),
                    data.TrainingQueue);
            }

            var treaties = save.Treaties
                .Where(t => Seat(players, t.Proposer) != null && Seat(players, t.Partner) != null)
                .Select(t => new Treaty((TreatyKind)t.Kind, players[t.Proposer], players[t.Partner], t.TurnsLeft)
                {
                    BrokenBy = Seat(players, t.BrokenBy)
                });
            var offers = save.Offers
                .Where(o => Seat(players, o.From) != null && Seat(players, o.To) != null)
                .Select(o => new Offer((TreatyKind)o.Kind, players[o.From], players[o.To]));
            game.Diplomacy.Restore(treaties, offers);

            for (var i = 0; i < players.Count; i++)
                RestoreReport(game, players[i].Report, save.Players[i].Report, players);

            return true;
        }

        private static Player RestorePlayer(PlayerSave data)
        {
            var player = new Player(data.Name, data.IsAI, data.Difficulty);
            player.Receive(data.Resources);
            return player;
        }

        private static void RestoreReport(GameController game, TurnReport report, ReportSave data, List<Player> players)
        {
            if (data == null)
                return;

            foreach (var roll in data.Rolls)
                report.AddRoll(roll);

            foreach (var payout in data.Payouts)
            {
                var country = CountryById(game, payout.Country);
                if (country != null)
                    report.AddPayout(payout.RollIndex, country, payout.Amount);
            }

            foreach (var bonus in data.RegionBonuses)
            {
                var region = RegionByName(game, bonus.Region);
                if (region != null)
                    report.AddRegionBonus(region, bonus.Amount);
            }

            foreach (var delivery in data.Deliveries)
            {
                var country = CountryById(game, delivery.Country);
                if (country == null)
                    continue;

                if (!string.IsNullOrEmpty(delivery.Building))
                {
                    var building = BuildingByName(game, delivery.Building);
                    if (building != null)
                        report.AddBuilt(country, building);
                }
                else
                {
                    for (var i = 0; i < delivery.Count; i++)
                        report.AddTrained(country, delivery.Soldier);
                }
            }

            foreach (var defence in data.Defences)
            {
                var country = CountryById(game, defence.Country);
                if (country == null)
                    continue;

                var restored = report.DefenceAgainst(Seat(players, defence.Attacker), country);
                restored.Attacks = defence.Attacks;
                restored.UnitsLost = defence.UnitsLost;
                restored.UnitsKilled = defence.UnitsKilled;
                restored.Fallen = defence.Fallen;
                restored.Razed.AddRange(Buildings(game, defence.Razed));
            }
        }

        private static Player Seat(List<Player> players, int seat) =>
            seat >= 0 && seat < players.Count ? players[seat] : null;

        private static Country CountryById(GameController game, int id) =>
            game.Countries.FirstOrDefault(country => country.Id == id);

        private static Region RegionByName(GameController game, string name) =>
            game.Countries.Select(country => country.Region).FirstOrDefault(region => region != null && region.name == name);

        private static IEnumerable<Building> Buildings(GameController game, IEnumerable<string> names) =>
            names.Select(name => BuildingByName(game, name)).Where(building => building != null).ToList();

        private static Building BuildingByName(GameController game, string name)
        {
            var building = game.Buildings.FirstOrDefault(b => b != null && b.name == name);
            if (building == null)
                Debug.LogWarning($"The save names a building \"{name}\" that is not in GameRules - skipped.");
            return building;
        }

        #endregion
    }
}
