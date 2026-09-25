using System.Collections.Generic;
using System.Linq;
using Gameplay.Managers;

namespace Gameplay
{
    // Plain C# object created per match from the lobby settings; presentation (colour, banner)
    // is looked up by the view layer, not stored here.
    public class Player
    {
        private GameResources _playerResources;

        public Player(string name) => Name = name;

        public string Name { get; }
        public GameResources Resources => _playerResources;
        public IEnumerable<Country> OwnedCountries => GameController.Instance.Countries.Where(c => c.Owner == this);

        public void GainResources(int dice)
        {
            foreach (var country in OwnedCountries)
                country.GainResources(ref _playerResources, dice);
        }

        // Trades go straight to the pool; income and queues go through the countries.
        public bool TrySpend(GameResources cost)
        {
            if (!(_playerResources >= cost))
                return false;

            _playerResources -= cost;
            return true;
        }

        public void Receive(GameResources gain) => _playerResources += gain;

        public void PhaseOne()
        {
            foreach (var country in OwnedCountries)
                country.StartPhaseOne(ref _playerResources);
        }
    }
}
