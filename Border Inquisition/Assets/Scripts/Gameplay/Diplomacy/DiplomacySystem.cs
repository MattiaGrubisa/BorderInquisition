using System.Collections.Generic;
using System.Linq;
using Gameplay;

namespace Diplomacy
{
    public enum TreatyKind
    {
        Pact,
        Alliance
    }

    // A treaty between two players. Any treaty forbids attacks both ways; an alliance also shares
    // vision through the fog of war.
    public class Treaty
    {
        public Treaty(TreatyKind kind, Player proposer, Player partner, int turnsLeft)
        {
            Kind = kind;
            Proposer = proposer;
            Partner = partner;
            TurnsLeft = turnsLeft;
        }

        public TreatyKind Kind { get; }
        public Player Proposer { get; }
        public Player Partner { get; }

        // Pacts only: how many more of the proposer's turns the pact covers.
        public int TurnsLeft { get; set; }

        // Set when a side breaks the treaty. It holds until that player's next turn starts, and until
        // then everyone sees them marked as a traitor.
        public Player BrokenBy { get; set; }

        public bool Involves(Player player) => player == Proposer || player == Partner;
        public Player Other(Player player) => player == Proposer ? Partner : Proposer;
    }

    public class Offer
    {
        public Offer(TreatyKind kind, Player from, Player to)
        {
            Kind = kind;
            From = from;
            To = to;
        }

        public TreatyKind Kind { get; }
        public Player From { get; }
        public Player To { get; }
    }

    // Pacts, alliances and the offers that lead to them, for one match. Plain C# owned by the
    // GameController; it knows nothing about phases. An offer waits for its target's turn and lapses
    // unanswered when that turn ends, which works the same hotseat or online.
    public class DiplomacySystem
    {
        private readonly GameRules _rules;
        private readonly List<Treaty> _treaties = new List<Treaty>();
        private readonly List<Offer> _offers = new List<Offer>();

        public DiplomacySystem(GameRules rules) => _rules = rules;

        public int PactTurns => _rules.PactTurns;

        public void Clear()
        {
            _treaties.Clear();
            _offers.Clear();
        }

        #region Queries

        public Treaty Between(Player a, Player b) =>
            a == null || b == null || a == b ? null : _treaties.FirstOrDefault(t => t.Involves(a) && t.Involves(b));

        // Any treaty, even one being broken, rules out attacks both ways.
        public bool AtPeace(Player a, Player b) => Between(a, b) != null;

        public bool AreAllied(Player a, Player b) =>
            a != null && (a == b || Between(a, b)?.Kind == TreatyKind.Alliance);

        public bool IsTraitor(Player player) => _treaties.Any(t => t.BrokenBy == player && player != null);

        public IEnumerable<Player> Traitors => _treaties.Where(t => t.BrokenBy != null).Select(t => t.BrokenBy).Distinct();

        public IEnumerable<Offer> OffersTo(Player player) => _offers.Where(o => o.To == player);

        public bool HasOffered(Player from, Player to, TreatyKind kind) =>
            _offers.Any(o => o.From == from && o.To == to && o.Kind == kind);

        // A pact needs no treaty yet; an alliance may also grow out of a pact. Nothing is offered
        // across a treaty that is being broken.
        public bool CanPropose(Player from, Player to, TreatyKind kind)
        {
            if (from == null || to == null || from == to || HasOffered(from, to, kind))
                return false;

            var treaty = Between(from, to);
            if (treaty == null)
                return true;

            return kind == TreatyKind.Alliance && treaty.Kind == TreatyKind.Pact && treaty.BrokenBy == null;
        }

        #endregion

        #region Commands

        public bool Propose(Player from, Player to, TreatyKind kind)
        {
            if (!CanPropose(from, to, kind))
                return false;

            _offers.Add(new Offer(kind, from, to));
            return true;
        }

        // Only the target can answer, and only while the offer still makes sense.
        public bool Accept(Offer offer, Player answering)
        {
            if (offer == null || offer.To != answering || !_offers.Remove(offer))
                return false;

            var existing = Between(offer.From, offer.To);
            if (existing != null)
            {
                if (offer.Kind != TreatyKind.Alliance || existing.Kind != TreatyKind.Pact || existing.BrokenBy != null)
                    return false;
                _treaties.Remove(existing);
            }

            _treaties.Add(new Treaty(offer.Kind, offer.From, offer.To, PactTurns));
            return true;
        }

        public bool Decline(Offer offer, Player answering) => offer != null && offer.To == answering && _offers.Remove(offer);

        public bool Break(Player breaker, Player other)
        {
            var treaty = Between(breaker, other);
            if (treaty == null || treaty.BrokenBy != null)
                return false;

            treaty.BrokenBy = breaker;
            _offers.RemoveAll(o => o.From == other && o.To == breaker || o.From == breaker && o.To == other);
            return true;
        }

        #endregion

        #region Turn hooks

        // At the start of a player's turn: treaties they broke end (and their traitor mark with them),
        // and pacts they proposed count down one turn.
        public void OnTurnStarted(Player player)
        {
            _treaties.RemoveAll(t => t.BrokenBy == player);

            foreach (var pact in _treaties.Where(t => t.Kind == TreatyKind.Pact && t.Proposer == player).ToList())
            {
                if (pact.TurnsLeft <= 0)
                    _treaties.Remove(pact);
                else
                    pact.TurnsLeft--;
            }
        }

        // Offers the player did not answer during their turn lapse.
        public void OnTurnEnded(Player player) => _offers.RemoveAll(o => o.To == player);

        #endregion
    }
}
