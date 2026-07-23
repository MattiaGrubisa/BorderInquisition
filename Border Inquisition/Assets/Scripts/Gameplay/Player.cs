using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private GameResources _playerResources;
        [SerializeField] private List<Country> _ownedCountries;

        public GameResources PlayerResources
        {
            get => _playerResources;
            set => _playerResources += value;
        }
        
        public List<Country> OwnedCountries => _ownedCountries;

        private void AddResources(GameResources resources) => _playerResources += resources;
        private void SubtractResources(GameResources resources) => _playerResources -= resources;
        
        public void AddCountry(Country country) => _ownedCountries.Add(country);
        public void RemoveCountry(Country country) => _ownedCountries.Remove(country);
        
        private void Awake()
        {
            _ownedCountries = new List<Country>();
        }
    }
}
