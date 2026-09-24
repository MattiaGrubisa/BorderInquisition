using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    // Borders are authored one way per Country; the graph mirrors every edge and keys the map by
    // Country.Id, so gameplay and the future network layer pass ids instead of object references.
    public class MapGraph
    {
        private static readonly IReadOnlyCollection<int> NoNeighbours = new int[0];

        private readonly Dictionary<int, Country> _countriesById = new Dictionary<int, Country>();
        private readonly Dictionary<int, HashSet<int>> _adjacency = new Dictionary<int, HashSet<int>>();

        public void Bake(IReadOnlyList<Country> countries)
        {
            _countriesById.Clear();
            _adjacency.Clear();

            foreach (var country in countries)
            {
                if (country == null)
                {
                    Debug.LogWarning("The country list has an empty slot.");
                    continue;
                }

                if (country.Id < 0)
                {
                    Debug.LogWarning($"{country.name} has no id - run Assign Country Ids.", country);
                    continue;
                }

                if (_countriesById.TryGetValue(country.Id, out var taken))
                {
                    Debug.LogWarning($"{country.name} reuses id {country.Id} of {taken.name}.", country);
                    continue;
                }

                _countriesById.Add(country.Id, country);
                _adjacency.Add(country.Id, new HashSet<int>());
            }

            foreach (var country in _countriesById.Values)
                foreach (var border in country.Borders)
                    Connect(country, border);
        }

        private void Connect(Country country, Country border)
        {
            if (border == null)
            {
                Debug.LogWarning($"{country.name} has an empty border slot.", country);
                return;
            }

            if (border == country)
            {
                Debug.LogWarning($"{country.name} borders itself.", country);
                return;
            }

            if (!_countriesById.TryGetValue(border.Id, out var mapped) || mapped != border)
            {
                Debug.LogWarning($"{country.name} borders {border.name}, which is not on the map.", country);
                return;
            }

            _adjacency[country.Id].Add(border.Id);
            _adjacency[border.Id].Add(country.Id);
        }

        public Country Find(int id) => _countriesById.TryGetValue(id, out var country) ? country : null;

        public bool AreNeighbours(int id, int otherId) =>
            _adjacency.TryGetValue(id, out var neighbours) && neighbours.Contains(otherId);

        public bool AreNeighbours(Country country, Country other) =>
            country != null && other != null && AreNeighbours(country.Id, other.Id);

        public IReadOnlyCollection<int> NeighbourIds(int id) =>
            _adjacency.TryGetValue(id, out var neighbours) ? neighbours : NoNeighbours;

        public IEnumerable<Country> Neighbours(Country country)
        {
            if (country == null)
                yield break;

            foreach (var id in NeighbourIds(country.Id))
                yield return _countriesById[id];
        }

        // A map that falls apart into several groups cannot be won: no attack ever crosses the gap.
        public IReadOnlyList<IReadOnlyList<int>> ConnectedGroups()
        {
            var groups = new List<IReadOnlyList<int>>();
            var visited = new HashSet<int>();

            foreach (var id in _countriesById.Keys)
            {
                if (!visited.Add(id))
                    continue;

                var group = new List<int>();
                var pending = new Queue<int>();
                pending.Enqueue(id);

                while (pending.Count > 0)
                {
                    var current = pending.Dequeue();
                    group.Add(current);

                    foreach (var neighbour in NeighbourIds(current))
                        if (visited.Add(neighbour))
                            pending.Enqueue(neighbour);
                }

                groups.Add(group);
            }

            return groups;
        }
    }
}
