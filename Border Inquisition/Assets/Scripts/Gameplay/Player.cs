using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private GameResources _playerResources;
        [SerializeField] private List<Country> _ownedNations;

        public GameResources PlayerResources
        {
            get => _playerResources;
            set => _playerResources += value;
        }

        public List<Country> OwnedNations => _ownedNations;

        private void AddResources(GameResources resources) => _playerResources += resources;
        private void SubtractResources(GameResources resources) => _playerResources -= resources;
        
        private void AddCountry(Country country) => _ownedNations.Add(country);
        private void RemoveCountry(Country country) => _ownedNations.Remove(country);
        
        private void Awake()
        {
            _ownedNations = new List<Country>();
        }
    }
}
