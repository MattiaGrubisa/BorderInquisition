using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private GameResources _playerResources;
        [SerializeField] private List<Country> _ownedCountries;
        
        public List<Country> OwnedCountries => _ownedCountries;
        
        public void AddCountry(Country country) => _ownedCountries.Add(country);
        public void RemoveCountry(Country country) => _ownedCountries.Remove(country);
        
        private void Awake()
        {
            _ownedCountries ??= new List<Country>();
        }

        public void GainResources(int dice)
        {
            foreach (var country in _ownedCountries)
                country.GainResources(ref _playerResources, dice);
        }

        public void PhaseOne()
        {
            foreach (var country in _ownedCountries)
                country.StartPhaseOne(ref _playerResources);
        }
    }
}
