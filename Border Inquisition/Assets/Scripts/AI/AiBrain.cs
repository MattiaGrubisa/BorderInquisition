using System;
using System.Collections.Generic;
using System.Linq;
using Diplomacy;
using Gameplay;
using Gameplay.Managers;
using Random = UnityEngine.Random;

namespace AI
{
    // Decides for one AI player. Plain C#: it reads the match through GameController and acts through
    // the same commands a human's clicks end up in, so the rules hold for it as well. The turn rules
    // the view enforces for humans (one move a turn, garrison) it keeps by itself. A fog-fair profile
    // only reads countries its player can see.
    public class AiBrain
    {
        private const int MaxSoldiersPerTurn = 12;
        private const int MaxTradesPerTurn = 6;
        private const int SimulatedRoundLimit = 100;

        private static readonly SoldierType[] SoldierTypes = (SoldierType[])Enum.GetValues(typeof(SoldierType));
        private static readonly ResourceType[] ResourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));

        private readonly Player _me;
        private readonly AiProfile _profile;
        private readonly Dictionary<(int, int, int, int, int, int), float> _winChances =
            new Dictionary<(int, int, int, int, int, int), float>();

        public AiBrain(Player me, AiProfile profile)
        {
            _me = me;
            _profile = profile;
        }

        private static GameController Game => GameController.Instance;
        private static int Garrison => Game.Rules.MinimumGarrison;

        #region Attack

        // The best attack worth making, if any. A mistake lowers the bar and picks at random.
        public bool TryChooseAttack(out Country from, out Country to)
        {
            from = to = null;
            var mistake = Random.value < _profile.MistakeChance;
            var threshold = mistake ? _profile.MinWinChance * 0.5f : _profile.MinWinChance;

            var options = _me.OwnedCountries
                .SelectMany(origin => Game.AttackTargets(origin).Select(target => (origin, target)))
                .Select(option => (option.origin, option.target, chance: WinChance(option.origin.Army, option.target.Army)))
                .Where(option => option.chance >= threshold)
                .ToList();
            if (options.Count == 0)
                return false;

            var pick = mistake
                ? options[Random.Range(0, options.Count)]
                : options.OrderByDescending(option => option.chance * Value(option.target)).First();
            from = pick.origin;
            to = pick.target;
            return true;
        }

        // After a conquest the whole army stands in the conquered country and its origin is empty; units
        // go back in proportion to the threat each of the two faces.
        public Army SendBack(Country origin, Country conquered)
        {
            var army = conquered.Army;
            var spare = army.Count - Garrison;
            var threatHome = Threat(origin);
            if (spare <= 0 || threatHome <= 0f)
                return default;

            var share = threatHome / (threatHome + Threat(conquered));
            return Take(army, Math.Clamp((int)Math.Round(army.Count * share), 1, spare));
        }

        private float WinChance(Army attacker, Army defender)
        {
            var key = (attacker.Knights, attacker.Horsemen, attacker.Archers,
                defender.Knights, defender.Horsemen, defender.Archers);
            if (_winChances.TryGetValue(key, out var cached))
                return cached;

            var wins = 0;
            for (var i = 0; i < _profile.Simulations; i++)
                if (SimulateBattle(attacker, defender))
                    wins++;

            return _winChances[key] = (float)wins / _profile.Simulations;
        }

        // Rounds of the real combat rules on copies of the armies, until one side is gone.
        private static bool SimulateBattle(Army attacker, Army defender)
        {
            for (var round = 0; round < SimulatedRoundLimit && !attacker.IsArmyEmpty() && !defender.IsArmyEmpty(); round++)
            {
                foreach (var attackerWin in Game.Combat.AttemptAttack(attacker, defender).AttackerWins)
                {
                    if (attackerWin)
                        defender.RemoveRandomUnit();
                    else
                        attacker.RemoveRandomUnit();
                }
            }
            return defender.IsArmyEmpty();
        }

        // Income, completing a region of its own, breaking someone else's, and (seen through the fog)
        // finishing a player off.
        private float Value(Country target)
        {
            var value = 1f + Total(target.Income) * 0.25f;

            var region = target.Region;
            if (region != null)
            {
                var others = Game.Countries.Where(c => c.Region == region && c != target).ToList();
                if (others.All(c => c.Owner == _me))
                    value += 2f + Total(region.CompletionBonus) * 0.5f;
                else
                    value += 0.2f * others.Count(c => c.Owner == _me);

                if (target.Owner != null && others.All(c => Knows(c) && c.Owner == target.Owner))
                    value += 1.5f;
            }

            if (_profile.SeesThroughFog && target.Owner != null && target.Owner.OwnedCountries.Count() == 1)
                value += 1f;
            return value;
        }

        #endregion

        #region Build & Move

        public void PlayBuildAndMove()
        {
            MakeMove();
            if (_profile.Trades)
                Trade();

            var budget = _me.Resources - PendingCosts();
            var outnumbered = _me.OwnedCountries.Any(c => Threat(c) > c.GetArmyPower);
            if (!outnumbered || Random.value < _profile.BuildFirstChance)
            {
                QueueBuilding(ref budget);
                QueueSoldiers(ref budget);
            }
            else
            {
                QueueSoldiers(ref budget);
                QueueBuilding(ref budget);
            }

            MaybePropose();
        }

        // The turn's one move, from where units are needed least to a neighbour that needs them most.
        // On the front a country needs them more the harder it is pressed; behind it, the closer to the
        // front the better. Behind the front everything but the garrison goes, on it half the spare.
        private void MakeMove()
        {
            var own = _me.OwnedCountries.ToList();
            var distance = DistancesToFront(own);
            var urgency = own.ToDictionary(c => c, c =>
            {
                var threat = Threat(c);
                return threat > 0f ? 100f + threat - (float)c.GetArmyPower : -distance[c];
            });

            Country bestFrom = null, bestTo = null;
            var bestCount = 0;
            var bestGain = 0f;
            foreach (var from in own.Where(c => c.Army.Count > Garrison))
            {
                var spare = from.Army.Count - Garrison;
                var count = Threat(from) > 0f ? spare / 2 : spare;
                if (count <= 0)
                    continue;

                foreach (var to in Game.Map.Neighbours(from).Where(t => Game.CanMoveArmy(from, t)))
                {
                    var lift = urgency[to] - urgency[from];
                    if (lift <= 1f || lift * count <= bestGain)
                        continue;

                    bestFrom = from;
                    bestTo = to;
                    bestCount = count;
                    bestGain = lift * count;
                }
            }

            if (bestFrom == null)
                return;

            var units = Take(bestFrom.Army, bestCount);
            Game.TryMoveArmy(bestFrom, bestTo, units.Knights, units.Horsemen, units.Archers);
        }

        // Steps through own countries to the nearest one with a hostile neighbour; own.Count when cut off.
        private Dictionary<Country, int> DistancesToFront(List<Country> own)
        {
            var distance = own.ToDictionary(c => c, _ => own.Count);
            var pending = new Queue<Country>();
            foreach (var front in own.Where(c => Threat(c) > 0f))
            {
                distance[front] = 0;
                pending.Enqueue(front);
            }

            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var neighbour in Game.Map.Neighbours(current))
                {
                    if (neighbour.Owner != _me || distance[neighbour] <= distance[current] + 1)
                        continue;

                    distance[neighbour] = distance[current] + 1;
                    pending.Enqueue(neighbour);
                }
            }
            return distance;
        }

        // Bank trades that turn a pile of one resource into the one it is shortest of.
        private void Trade()
        {
            var ratio = Game.Market.RatioFor(_me);
            for (var i = 0; i < MaxTradesPerTurn; i++)
            {
                var have = _me.Resources;
                var most = ResourceTypes.OrderByDescending(type => have.Get(type)).First();
                var least = ResourceTypes.OrderBy(type => have.Get(type)).First();
                if (have.Get(most) - ratio < have.Get(least) + 2 || !Game.Market.TryTrade(_me, most, least, 1))
                    return;
            }
        }

        // Queues are paid on delivery, so what is already queued counts against the budget.
        private GameResources PendingCosts()
        {
            var pending = default(GameResources);
            foreach (var country in _me.OwnedCountries)
            {
                foreach (var building in country.BuildingQueue)
                    pending += country.GetBuildingCost(building);
                foreach (var soldier in country.TrainingQueue)
                    pending += country.GetSoldierCost(soldier);
            }
            return pending;
        }

        // Round the three most outnumbered fronts, one soldier each per pass, while the budget lasts.
        private void QueueSoldiers(ref GameResources budget)
        {
            var fronts = _me.OwnedCountries
                .Where(c => c.Region != null && Threat(c) > 0f)
                .OrderByDescending(c => Threat(c) - c.GetArmyPower)
                .Take(3)
                .ToList();

            var queued = 0;
            var progress = true;
            while (progress && queued < MaxSoldiersPerTurn)
            {
                progress = false;
                foreach (var country in fronts)
                {
                    var soldier = AffordableSoldier(country, budget);
                    if (soldier == null || queued >= MaxSoldiersPerTurn)
                        continue;

                    country.AddSoldierToQueue(soldier.Value);
                    budget -= country.GetSoldierCost(soldier.Value);
                    queued++;
                    progress = true;
                }
            }
        }

        // A type the country lacks adds a die in combat, so it comes first; then the cheapest there.
        private static SoldierType? AffordableSoldier(Country country, GameResources budget) =>
            SoldierTypes
                .OrderBy(type => UnitsOf(country.Army, type) + country.QueuedSoldiers(type) == 0 ? 0 : 1)
                .ThenBy(type => Total(country.GetSoldierCost(type)))
                .Where(type => budget >= country.GetSoldierCost(type))
                .Cast<SoldierType?>()
                .FirstOrDefault();

        // One building a turn, in one of the three safest countries with an empty building queue.
        private void QueueBuilding(ref GameResources budget)
        {
            var sites = _me.OwnedCountries
                .Where(c => c.Region != null && c.BuildingQueue.Count == 0)
                .OrderBy(Threat)
                .Take(3)
                .ToList();

            Country bestSite = null;
            Building bestBuilding = null;
            var bestScore = 0f;
            foreach (var site in sites)
            {
                foreach (var building in Game.Buildings.Where(b => !site.IsBuilt(b)))
                {
                    var cost = site.GetBuildingCost(building);
                    if (!(budget >= cost))
                        continue;

                    var score = BuildingScore(building) / (1f + Total(cost) * 0.1f);
                    if (score <= bestScore)
                        continue;

                    bestSite = site;
                    bestBuilding = building;
                    bestScore = score;
                }
            }

            if (bestSite != null && bestSite.AddBuildingToQueue(bestBuilding))
                budget -= bestSite.GetBuildingCost(bestBuilding);
        }

        // Production counts more for the resources it is short of; a discount or trade ratio only when
        // it beats what the player already has.
        private float BuildingScore(Building building)
        {
            var have = _me.Resources;
            var score = ResourceTypes.Sum(type => building.ProductionBoost.Get(type) * 4f / (1 + Math.Max(0, have.Get(type))));

            if (building.SoldierDiscount > _me.SoldierDiscount)
                score += (building.SoldierDiscount - _me.SoldierDiscount) / 10f;
            if (building.BuildingDiscount > _me.BuildingDiscount)
                score += (building.BuildingDiscount - _me.BuildingDiscount) / 15f;
            if (building.TradeRatio > 0 && building.TradeRatio < Game.Market.RatioFor(_me))
                score += 1f;
            return score;
        }

        #endregion

        #region Diplomacy

        // Offers wait for this turn and lapse at its end, so they are answered at its start.
        public void AnswerOffers()
        {
            foreach (var offer in Game.Diplomacy.OffersTo(_me).ToList())
            {
                if (WantsTreaty(offer.From, offer.Kind))
                    Game.Diplomacy.Accept(offer, _me);
                else
                    Game.Diplomacy.Decline(offer, _me);
            }
        }

        // Peace with the strong; a pact comes easier while fighting on two fronts. Never with a
        // traitor, and no alliance with the last opponent, since allies cannot win together.
        private bool WantsTreaty(Player other, TreatyKind kind)
        {
            if (Game.Diplomacy.IsTraitor(other))
                return false;
            if (kind == TreatyKind.Alliance && AlivePlayers() <= 2)
                return false;

            var needed = kind == TreatyKind.Pact ? _profile.PactAcceptRatio : _profile.AllianceAcceptRatio;
            if (kind == TreatyKind.Pact && NeighbourPlayers().Count(IsHostile) >= 2)
                needed *= 0.7f;
            return PowerOf(other) >= needed * Math.Max(1f, PowerOf(_me));
        }

        // Now and then: a pact to the strongest neighbour it would rather not fight, otherwise an
        // alliance to a pact partner.
        private void MaybePropose()
        {
            if (Random.value >= _profile.OfferChance)
                return;

            var diplomacy = Game.Diplomacy;
            var strongest = NeighbourPlayers()
                .Where(p => !diplomacy.IsTraitor(p) && diplomacy.CanPropose(_me, p, TreatyKind.Pact))
                .OrderByDescending(PowerOf)
                .FirstOrDefault();
            if (strongest != null && PowerOf(strongest) >= PowerOf(_me) * 0.8f)
            {
                diplomacy.Propose(_me, strongest, TreatyKind.Pact);
                return;
            }

            if (AlivePlayers() <= 2)
                return;

            var partner = Game.Players.FirstOrDefault(p =>
                diplomacy.Between(_me, p) != null && diplomacy.CanPropose(_me, p, TreatyKind.Alliance));
            if (partner != null)
                diplomacy.Propose(_me, partner, TreatyKind.Alliance);
        }

        #endregion

        #region Knowledge

        private bool Knows(Country country) => _profile.SeesThroughFog || Game.Fog.IsVisible(_me, country);

        private bool IsHostile(Player owner) => owner != null && owner != _me && !Game.Diplomacy.AtPeace(_me, owner);

        // The army power of hostile neighbours it can see.
        private float Threat(Country country) =>
            (float)Game.Map.Neighbours(country).Where(n => Knows(n) && IsHostile(n.Owner)).Sum(n => n.GetArmyPower);

        // Its own power in full; anyone else's only as far as it can see.
        private float PowerOf(Player player) =>
            (float)Game.Countries.Where(c => c.Owner == player && (player == _me || Knows(c))).Sum(c => c.GetArmyPower);

        private IEnumerable<Player> NeighbourPlayers() =>
            _me.OwnedCountries
                .SelectMany(c => Game.Map.Neighbours(c))
                .Where(n => n.Owner != null && n.Owner != _me && Knows(n))
                .Select(n => n.Owner)
                .Distinct();

        private static int AlivePlayers() => Game.Players.Count(p => !Game.IsEliminated(p));

        #endregion

        // Taken from whichever type has the most left, so the country keeps as many unit types - and so
        // dice - as it can.
        private static Army Take(Army army, int count)
        {
            var left = new[] { army.Knights, army.Horsemen, army.Archers };
            var taken = new int[3];
            for (var i = 0; i < count; i++)
            {
                var type = Array.IndexOf(left, left.Max());
                if (left[type] == 0)
                    break;

                left[type]--;
                taken[type]++;
            }
            return new Army(taken[0], taken[1], taken[2]);
        }

        private static int UnitsOf(Army army, SoldierType type)
        {
            switch (type)
            {
                case SoldierType.Knight:
                    return army.Knights;
                case SoldierType.Horseman:
                    return army.Horsemen;
                case SoldierType.Archer:
                    return army.Archers;
            }
            return 0;
        }

        private static int Total(GameResources resources) =>
            resources.Food + resources.Wood + resources.Gold + resources.Stone;
    }
}
