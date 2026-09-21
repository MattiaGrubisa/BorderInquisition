using System.Collections.Generic;
using System.Linq;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private GameResources _playerResources;

        public IEnumerable<Country> OwnedCountries => GameController.Instance.Countries.Where(c => c.Owner == this);

        public void GainResources(int dice)
        {
            foreach (var country in OwnedCountries)
                country.GainResources(ref _playerResources, dice);
        }

        public void PhaseOne()
        {
            foreach (var country in OwnedCountries)
                country.StartPhaseOne(ref _playerResources);
        }
    }
}