using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 39 (Part D): the police's data in the scene, game layer: the drivable roads with their names, classes, widths and speed limits (OSM's
    // maxspeed where tagged, else DefaultLimit by class; RoadNet builds the cops' network from them at run time) and the radio's script
    // (Tools/Audio/police-radio-lines.csv, its IDs unchanged)
    public static partial class MapBuilder
    {
        static readonly string[] PoliceClasses = { "motorway", "motorway_link", "trunk", "trunk_link", "primary", "primary_link", "secondary", "secondary_link", "tertiary", "tertiary_link", "residential", "unclassified", "living_street", "service" };

        static void BuildPolice(MapData map)
        {
            var root = new GameObject("Police (game layer)");
            var roads = root.AddComponent<PoliceRoads>();
            var lines = map.roads.Where(r => r.points != null && r.points.Length >= 4 && !r.tunnel && System.Array.IndexOf(PoliceClasses, r.@class) >= 0)
                .SelectMany(r => R53Pieces(r.osm_id, ToPoints(r.points)).Select(p => (pts: p, cls: r.@class, half: r.width * .5f, name: r.name ?? "", limit: r.maxspeed_mph > 0 ? r.maxspeed_mph : PoliceRoads.DefaultLimit(r.@class))))   // Round 53: Lyndon Drive's crew site left out
                .Concat(map.bridges.Where(b => b.points != null && b.points.Length >= 4 && System.Array.IndexOf(PoliceClasses, b.@class) >= 0)
                .Select(b => (pts: ToPoints(b.points), cls: b.@class, half: b.width * .5f, name: b.name ?? "", limit: PoliceRoads.DefaultLimit(b.@class)))).ToList();
            var first = new int[lines.Count + 1]; var pts = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < lines.Count; i++) { first[i] = pts.Count; pts.AddRange(lines[i].pts); }
            first[lines.Count] = pts.Count;
            roads.names = lines.Select(l => l.name).ToArray(); roads.classes = lines.Select(l => l.cls).ToArray(); roads.halfWidths = lines.Select(l => l.half).ToArray();
            roads.limits = lines.Select(l => l.limit).ToArray(); roads.first = first; roads.points = pts.ToArray();
            int tagged = map.roads.Count(r => r.maxspeed_mph > 0 && System.Array.IndexOf(PoliceClasses, r.@class) >= 0);
            var csv = Path.Combine(ProjectRoot, "Tools/Audio/police-radio-lines.csv");
            root.AddComponent<PoliceRadioScript>().csv = File.Exists(csv) ? File.ReadAllText(csv) : "";
            Debug.Log($"[Round39] police: {lines.Count} roads ({pts.Count} points) for the cops' network, {tagged} with OSM's speed limit (the rest by class); the radio's script {(File.Exists(csv) ? "from " + csv : "MISSING")}; " +
                      string.Join(", ", lines.GroupBy(l => l.limit).OrderBy(g => g.Key).Select(g => $"{g.Key} mph {g.Count()}")));
        }
    }
}
