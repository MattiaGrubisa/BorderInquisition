using System.Collections.Generic;
using System.Linq;
using Gameplay;
using Gameplay.Managers;
using UI;
using UnityEngine;

namespace View
{
    // Puts a marker over every country once WorldMap is up; the markers keep themselves current. The
    // picker is added here too, so the scene needs nothing but this component. When the HUD reveals a
    // turn, the income this turn's roll paid the current player rises from each paying country.
    public class MapView : MonoBehaviour
    {
        private readonly Dictionary<Country, CountryMarker> _markers = new Dictionary<Country, CountryMarker>();
        private InGameHud _hud;

        private void Start()
        {
            foreach (var country in GameController.Instance.Countries)
                if (country != null)
                    _markers[country] = CountryMarker.Create(country);

            if (!TryGetComponent<CountryPicker>(out _))
                gameObject.AddComponent<CountryPicker>();

            _hud = FindFirstObjectByType<InGameHud>();
            if (_hud != null)
                _hud.TurnBegan += ShowIncome;
        }

        private void OnDestroy()
        {
            if (_hud != null)
                _hud.TurnBegan -= ShowIncome;
        }

        // The popups wait for the income die to land.
        private void ShowIncome()
        {
            var report = GameController.Instance.CurrentPlayer.Report;
            foreach (var payout in report.PayoutsOf(report.Rolls.Count - 1).Where(p => p.Country != null && !p.Amount.IsEmpty))
                IncomePopup.Spawn(payout.Country.transform.position, "+ " + Format.Resources(payout.Amount),
                    DieRoll.Duration);
        }

        // The country whose collider is under a screen point, or null.
        public static Country CountryAt(Vector2 screen)
        {
            var camera = Camera.main;
            if (camera == null)
                return null;

            var hit = Physics2D.OverlapPoint(camera.ScreenToWorldPoint(screen));
            return hit != null ? hit.GetComponentInParent<Country>() : null;
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
