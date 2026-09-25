using System.Linq;
using Diplomacy;

namespace Gameplay
{
    // Always on, hotseat or online: a player sees the countries of their side (themselves and their
    // allies) and every neighbour of those (depth 1 on the map graph). Of any other country only its
    // dice number is known - not the owner, not the army. The rule lives here so a future server can
    // decide what to send each player.
    public class FogOfWar
    {
        private readonly MapGraph _map;
        private readonly DiplomacySystem _diplomacy;

        public FogOfWar(MapGraph map, DiplomacySystem diplomacy)
        {
            _map = map;
            _diplomacy = diplomacy;
        }

        public bool IsVisible(Player viewer, Country country) =>
            viewer != null && country != null
            && (OnSide(viewer, country) || _map.Neighbours(country).Any(neighbour => OnSide(viewer, neighbour)));

        private bool OnSide(Player viewer, Country country) => _diplomacy.AreAllied(viewer, country.Owner);
    }
}
