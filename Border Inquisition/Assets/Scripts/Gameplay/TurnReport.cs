using System.Collections.Generic;
using System.Linq;

namespace Gameplay
{
    // What happened to one player since their last turn ended, so a hotseat player who was away from
    // the screen can catch up: every income roll and what it paid them, the bonus of every region they hold whole, what their queues delivered
    // at the start of this turn, and every attack on their countries. Gameplay fills it, the HUD shows
    // it when the turn begins, and it is cleared when the player's turn ends.
    public class TurnReport
    {
        public readonly struct Payout
        {
            public Payout(int rollIndex, Country country, GameResources amount)
            {
                RollIndex = rollIndex;
                Country = country;
                Amount = amount;
            }

            // Index into Rolls, since the same number can come up twice.
            public int RollIndex { get; }
            public Country Country { get; }
            public GameResources Amount { get; }
        }

        // A building, or Count soldiers of one type when Building is null.
        public class Delivery
        {
            public Delivery(Country country, Building building, SoldierType soldier)
            {
                Country = country;
                Building = building;
                Soldier = soldier;
            }

            public Country Country { get; }
            public Building Building { get; }
            public SoldierType Soldier { get; }
            public int Count { get; set; } = 1;
        }

        // Every attack one player made on one of this player's countries, added up.
        public class Defence
        {
            public Defence(Player attacker, Country country)
            {
                Attacker = attacker;
                Country = country;
            }

            public Player Attacker { get; }
            public Country Country { get; }
            public int Attacks { get; set; }
            public int UnitsLost { get; set; }
            public int UnitsKilled { get; set; }
            public bool Fallen { get; set; }
        }

        private readonly List<int> _rolls = new List<int>();
        private readonly List<Payout> _payouts = new List<Payout>();
        private readonly List<Delivery> _deliveries = new List<Delivery>();
        private readonly List<Defence> _defences = new List<Defence>();
        private readonly List<(Region Region, GameResources Amount)> _regionBonuses =
            new List<(Region Region, GameResources Amount)>();

        public IReadOnlyList<int> Rolls => _rolls;
        public IReadOnlyList<Payout> Payouts => _payouts;
        public IReadOnlyList<Delivery> Deliveries => _deliveries;
        public IReadOnlyList<Defence> Defences => _defences;
        public IReadOnlyList<(Region Region, GameResources Amount)> RegionBonuses => _regionBonuses;

        public GameResources TotalIncome => _payouts.Aggregate(default(GameResources), (sum, p) => sum + p.Amount);

        public IEnumerable<Payout> PayoutsOf(int rollIndex) => _payouts.Where(p => p.RollIndex == rollIndex);

        public void AddRoll(int roll) => _rolls.Add(roll);

        // Belongs to the roll added last.
        public void AddPayout(Country country, GameResources amount) =>
            _payouts.Add(new Payout(_rolls.Count - 1, country, amount));

        public void AddRegionBonus(Region region, GameResources amount) => _regionBonuses.Add((region, amount));

        public void AddBuilt(Country country, Building building) =>
            _deliveries.Add(new Delivery(country, building, default));

        public void AddTrained(Country country, SoldierType soldier)
        {
            var delivery = _deliveries.FirstOrDefault(d =>
                d.Building == null && d.Country == country && d.Soldier == soldier);

            if (delivery != null)
                delivery.Count++;
            else
                _deliveries.Add(new Delivery(country, null, soldier));
        }

        public void AddDefence(Player attacker, Country country, int unitsLost, int unitsKilled, bool fallen)
        {
            var defence = _defences.FirstOrDefault(d => d.Attacker == attacker && d.Country == country);
            if (defence == null)
            {
                defence = new Defence(attacker, country);
                _defences.Add(defence);
            }

            defence.Attacks++;
            defence.UnitsLost += unitsLost;
            defence.UnitsKilled += unitsKilled;
            defence.Fallen |= fallen;
        }

        public void Clear()
        {
            _rolls.Clear();
            _payouts.Clear();
            _deliveries.Clear();
            _defences.Clear();
            _regionBonuses.Clear();
        }
    }
}
