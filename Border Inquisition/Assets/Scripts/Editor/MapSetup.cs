#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Gameplay;
using Gameplay.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor
{
    // Spawns the hand-authored territory list into WorldMap, one parent per continent, and hands the
    // countries to the GameController. Positions are rough clusters only - the real layout is placed
    // by hand over the map art, and borders are authored afterwards on each Country.
    public static class MapSetup
    {
        private const float Spacing = 1.1f;
        private const int Columns = 3;

        private class Continent
        {
            public readonly string Name;
            public readonly Vector2 Anchor;
            public readonly string[] Territories;

            public Continent(string name, Vector2 anchor, params string[] territories)
            {
                Name = name;
                Anchor = anchor;
                Territories = territories;
            }
        }

        // Each continent draws its names from a different real-world tradition.
        private static readonly Continent[] Continents =
        {
            new Continent("Vargmark", new Vector2(-6.0f, 2.6f),
                "Vargheim", "Skarnfjord", "Eldvik", "Norrhald", "Myrkdal", "Stravenn"),

            new Continent("Zlatokraj", new Vector2(2.4f, 3.4f),
                "Volenska", "Radomir", "Zlatobor", "Vyshgorod", "Tarnovka"),

            new Continent("Higanshu", new Vector2(6.6f, 1.0f),
                "Kazamori", "Shirakane", "Ryuudani", "Takanohara", "Yomigase"),

            new Continent("Aureliana", new Vector2(-0.4f, 0.4f),
                "Aurelia", "Castramar", "Veridium", "Solventia", "Tarquinia", "Lucanor"),

            new Continent("Kanembara", new Vector2(-4.6f, -2.8f),
                "Kanemba", "Sokotai", "Ifewara", "Nandobe", "Tembakuru", "Jalibari", "Zumara"),

            new Continent("Ashqaran", new Vector2(3.8f, -2.8f),
                "Asharaq", "Zafirin", "Mihrabad", "Qastalin", "Neydara", "Shirvanah", "Rakhshan")
        };

        [MenuItem("Border Inquisition/Create Countries")]
        public static void CreateCountries()
        {
            var controller = Object.FindFirstObjectByType<GameController>();
            if (controller == null)
            {
                Debug.LogError("No GameController in the open scene - open WorldMap, or run Create Flow Scenes first.");
                return;
            }

            if (Object.FindObjectsByType<Country>(FindObjectsSortMode.None).Length > 0)
            {
                Debug.LogWarning("The scene already has countries; delete them first if you want them regenerated.");
                return;
            }

            var countries = new List<Country>();
            foreach (var continent in Continents)
                countries.AddRange(CreateContinent(continent, countries.Count));

            Fill(controller, countries);

            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Debug.Log($"Created {countries.Count} countries across {Continents.Length} continents. " +
                      "Place them over the map, then author each country's Borders and run Validate Map.");
        }

        // Adds what the map needs to be seen and clicked: a MapView on the map sprite, and a
        // PolygonCollider2D on every country that has none yet. The collider starts as Unity's default
        // shape and is traced over the region art by hand; existing colliders are never touched.
        [MenuItem("Border Inquisition/Set Up Map View")]
        public static void SetUpMapView()
        {
            var countries = Object.FindObjectsByType<Country>(FindObjectsSortMode.None);
            if (countries.Length == 0)
            {
                Debug.LogError("No countries in the open scene - open WorldMap first.");
                return;
            }

            var colliders = 0;
            foreach (var country in countries)
            {
                if (country.GetComponent<Collider2D>() != null)
                    continue;

                Undo.AddComponent<CircleCollider2D>(country.gameObject);
                colliders++;
            }

            var addedView = false;
            if (Object.FindFirstObjectByType<View.MapView>() == null)
            {
                var map = Object.FindFirstObjectByType<View.MapCamera>();
                var host = map != null ? map.gameObject : ObjectFactory.CreateGameObject("MapView");
                Undo.AddComponent<View.MapView>(host);
                addedView = true;
            }

            EditorSceneManager.MarkSceneDirty(countries[0].gameObject.scene);
            Debug.Log($"Added {colliders} country collider(s)" + (addedView ? " and the MapView." : "; MapView already there."));
        }

        // Swaps every country's CircleCollider2D for Unity's default PolygonCollider2D, ready to be traced
        // over the region art. Countries without a circle are skipped.
        [MenuItem("Border Inquisition/Convert Country Colliders To Polygon")]
        public static void ConvertCollidersToPolygon()
        {
            var countries = Object.FindObjectsByType<Country>(FindObjectsSortMode.None);
            if (countries.Length == 0)
            {
                Debug.LogError("No countries in the open scene - open WorldMap first.");
                return;
            }

            Undo.SetCurrentGroupName("Convert Country Colliders To Polygon");
            var converted = 0;
            foreach (var country in countries)
            {
                var circle = country.GetComponent<CircleCollider2D>();
                if (circle == null)
                    continue;

                Undo.DestroyObjectImmediate(circle);
                Undo.AddComponent<PolygonCollider2D>(country.gameObject);
                converted++;
            }

            if (converted > 0)
                EditorSceneManager.MarkSceneDirty(countries[0].gameObject.scene);
            Debug.Log($"Converted {converted} circle collider(s) to polygons.");
        }

        private static IEnumerable<Country> CreateContinent(Continent continent, int firstId)
        {
            var parent = ObjectFactory.CreateGameObject(continent.Name).transform;
            parent.position = continent.Anchor;

            var countries = new List<Country>();
            for (var i = 0; i < continent.Territories.Length; i++)
            {
                var go = ObjectFactory.CreateGameObject(continent.Territories[i], typeof(Country));
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Offset(i);

                var country = go.GetComponent<Country>();
                country.SetId(firstId + i);
                countries.Add(country);
            }

            return countries;
        }

        // Loose grid around the continent anchor, only so the objects do not pile up on one spot.
        private static Vector3 Offset(int index) =>
            new Vector3(index % Columns * Spacing, -(index / Columns) * Spacing, 0f);

        private static void Fill(GameController controller, IReadOnlyList<Country> countries)
        {
            var serialized = new SerializedObject(controller);
            var list = serialized.FindProperty("_countries");
            if (list == null)
            {
                Debug.LogError("GameController has no serialized field '_countries'.", controller);
                return;
            }

            list.arraySize = countries.Count;
            for (var i = 0; i < countries.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = countries[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
