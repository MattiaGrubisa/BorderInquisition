using System.Collections.Generic;
using Gameplay;
using Gameplay.Managers;
using UnityEngine;

namespace View
{
    // Puts a marker over every country once WorldMap is up; the markers keep themselves current. The
    // picker is added here too, so the scene needs nothing but this component.
    public class MapView : MonoBehaviour
    {
        private readonly Dictionary<Country, CountryMarker> _markers = new Dictionary<Country, CountryMarker>();

        private void Start()
        {
            foreach (var country in GameController.Instance.Countries)
                if (country != null)
                    _markers[country] = CountryMarker.Create(country);

            if (!TryGetComponent<CountryPicker>(out _))
                gameObject.AddComponent<CountryPicker>();
        }

        public void ClearHighlights()
        {
            foreach (var marker in _markers.Values)
                marker.SetHighlight(CountryMarker.Highlight.None);
        }

        public void SetHighlight(Country country, CountryMarker.Highlight highlight)
        {
            if (country != null && _markers.TryGetValue(country, out var marker))
                marker.SetHighlight(highlight);
        }
    }
}
