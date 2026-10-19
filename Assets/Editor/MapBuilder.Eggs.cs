using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Round 48: the goose egg hunt's eggs and the map's destinations, game layer: their own root in the scene ("Goose eggs"), made here from
// Data/explore/eggs.json (Tools/mapgen/make_eggs.py), never part of the world's pieces. The game makes the eggs' looks and finds a "ground"
// egg's surface at start (Explore/EggHunt.cs); this only carries the data. Built with the game layer (the GameLayer step). The eggs alone
// into the built scene: -executeMethod ChastainRush.MapBuilder.RebuildEggsFromCommandLine (new spots without a whole Build Map).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class EggJson { public string id, area, how, kind, note; public float x, y, z, radius; }
        [Serializable] class EggAreaJson { public string name, direction; public int eggs; }
        [Serializable] class EggDestJson { public string id, title; public float x, y, z, yaw; public bool @fixed; }
        [Serializable] class EggsFile { public int[] tiers; public EggAreaJson[] areas; public EggJson[] eggs; public EggDestJson[] destinations; }
        const string EggsFileName = "Data/explore/eggs.json";

        public static void RebuildEggsFromCommandLine()
        {
            try
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
                foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "Goose eggs")) UnityEngine.Object.DestroyImmediate(g);
                BuildEggs();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ScenePath);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void BuildEggs()
        {
            string file = Path.Combine(ProjectRoot, EggsFileName);
            if (!File.Exists(file)) { Debug.LogWarning("[Round48] no " + EggsFileName + ": no egg hunt (python Tools/mapgen/make_eggs.py)"); return; }
            string text = File.ReadAllText(file);
            var f = JsonUtility.FromJson<EggsFile>(text);
            var root = new GameObject("Goose eggs");
            var hunt = root.AddComponent<EggHunt>();
            hunt.sites = f.eggs.Select(e => new EggHunt.Site { id = e.id, area = e.area, how = e.how, kind = e.kind, note = e.note, position = new Vector3(e.x, e.y, e.z), radius = e.radius > 0f ? e.radius : 2.8f }).ToArray();
            hunt.areas = f.areas.Select(a => new EggHunt.AreaInfo { name = a.name, direction = a.direction }).ToArray();
            if (f.tiers != null && f.tiers.Length > 0) hunt.tiers = f.tiers;
            // the destinations ("fixed": its own height, the hub underground; otherwise the ground under it, found at start)
            var places = root.AddComponent<ExplorationPlaces>();
            var dests = (f.destinations ?? new EggDestJson[0]).Select(d => new Exploration.Destination { id = d.id, title = d.title, position = new Vector3(d.x, d.y, d.z), yaw = d.yaw, fixedY = d.@fixed }).ToArray();
            places.destinations = dests;
            Debug.Log($"[Round48] goose eggs: {hunt.sites.Length} in {hunt.areas.Length} areas ({hunt.sites.Count(s => s.how == "easy")} easy, {hunt.sites.Count(s => s.how != "easy")} hard; " +
                      $"{hunt.sites.Count(s => s.kind == "fixed")} at a fixed height), tiers {string.Join(", ", hunt.tiers)}; {dests.Length} destinations");
        }
    }
}
