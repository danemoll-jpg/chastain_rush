using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChastainRush
{
    // Round 30 (A1): the road graph traffic drives on (Traffic/TrafficNetwork.cs), from the map's roads and bridges: the
    // classes cars use (motorway, primary, secondary, tertiary, their links, unclassified, residential; not service roads,
    // parking aisles or drives), split at every junction. OSM ways share their junction nodes, so a vertex two ways have in
    // common is a junction; a way's end within 2.5 m of another way's vertex is joined to it there (bridges were cut out of
    // their roads by mapgen, so their ends do not always land on a vertex). Plain data in the scene, like RoadLookup.
    public static partial class MapBuilder
    {
        static readonly string[] TrafficClasses = { "motorway", "motorway_link", "trunk", "trunk_link", "primary", "primary_link", "secondary", "secondary_link", "tertiary", "tertiary_link", "unclassified", "residential" };

        static void BuildTrafficNetwork(MapData map)
        {
            var lines = new List<(List<Vector2> pts, string cls, bool oneway, float half, bool bridge, string name)>();
            foreach (var r in map.roads) if (System.Array.IndexOf(TrafficClasses, r.@class) >= 0 && r.points != null && r.points.Length >= 4) lines.Add((ToPoints(r.points), r.@class, r.oneway, r.width * .5f, r.bridge, r.name ?? ""));
            foreach (var b in map.bridges) if (System.Array.IndexOf(TrafficClasses, b.@class) >= 0 && b.points != null && b.points.Length >= 4) lines.Add((ToPoints(b.points), b.@class, b.oneway, b.width * .5f, true, b.name ?? ""));

            Vector2Int Key(Vector2 p) => new(Mathf.RoundToInt(p.x * 10), Mathf.RoundToInt(p.y * 10));
            // how many lines pass through each vertex
            var uses = new Dictionary<Vector2Int, int>();
            foreach (var l in lines) foreach (var k in l.pts.Select(Key).Distinct()) uses[k] = uses.TryGetValue(k, out int n) ? n + 1 : 1;
            // ends that meet another line near (not on) one of its vertices: moved onto that vertex
            const float Cell = 8f; var hash = new Dictionary<Vector2Int, List<(int line, int index)>>();
            for (int li = 0; li < lines.Count; li++) for (int i = 0; i < lines[li].pts.Count; i++)
                { var c = new Vector2Int(Mathf.FloorToInt(lines[li].pts[i].x / Cell), Mathf.FloorToInt(lines[li].pts[i].y / Cell)); if (!hash.TryGetValue(c, out var list)) hash[c] = list = new(); list.Add((li, i)); }
            int joined = 0;
            for (int li = 0; li < lines.Count; li++)
                foreach (int end in new[] { 0, lines[li].pts.Count - 1 })
                {
                    var p = lines[li].pts[end]; if (uses[Key(p)] > 1) continue;
                    var c = new Vector2Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell)); float best = 2.5f; Vector2 to = p; bool found = false;
                    for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                        if (hash.TryGetValue(c + new Vector2Int(dx, dz), out var list))
                            foreach (var (ol, oi) in list) { if (ol == li) continue; float d = (lines[ol].pts[oi] - p).magnitude; if (d < best) { best = d; to = lines[ol].pts[oi]; found = true; } }
                    if (!found) continue;
                    uses[Key(p)]--; lines[li].pts[end] = to; uses[Key(to)] = uses[Key(to)] + 1; joined++;
                }

            var nodes = new List<Vector2>(); var nodeOf = new Dictionary<Vector2Int, int>();
            int Node(Vector2 p) { var k = Key(p); if (!nodeOf.TryGetValue(k, out int n)) { n = nodes.Count; nodes.Add(p); nodeOf[k] = n; } return n; }
            var net = new GameObject("TrafficNetwork").AddComponent<TrafficNetwork>();
            var from = new List<int>(); var toNode = new List<int>(); var cls = new List<int>(); var oneway = new List<bool>(); var half = new List<float>(); var bridge = new List<bool>();
            var start = new List<int>(); var count = new List<int>(); var points = new List<Vector2>(); var names = new List<string>();
            foreach (var l in lines)
            {
                int a = 0;
                for (int i = 1; i < l.pts.Count; i++)
                {
                    if (i < l.pts.Count - 1 && uses[Key(l.pts[i])] < 2) continue;
                    if (i - a < 1) continue;
                    from.Add(Node(l.pts[a])); toNode.Add(Node(l.pts[i])); cls.Add(System.Array.IndexOf(TrafficClasses, l.cls)); oneway.Add(l.oneway); half.Add(l.half); bridge.Add(l.bridge); names.Add(l.name);
                    start.Add(points.Count); count.Add(i - a + 1); for (int j = a; j <= i; j++) points.Add(l.pts[j]);
                    a = i;
                }
            }
            net.Fill(nodes.ToArray(), from.ToArray(), toNode.ToArray(), cls.ToArray(), oneway.ToArray(), half.ToArray(), bridge.ToArray(), start.ToArray(), count.ToArray(), points.ToArray(), names.ToArray(), TrafficClasses);
            int deadEnds = 0; var degree = new int[nodes.Count]; for (int e = 0; e < from.Count; e++) { degree[from[e]]++; degree[toNode[e]]++; } foreach (int d in degree) if (d == 1) deadEnds++;
            Debug.Log($"[Round30] traffic network: {lines.Count} roads and bridges of the traffic classes, {from.Count} edges, {nodes.Count} nodes ({deadEnds} dead ends), {joined} ends joined to a nearby road, {points.Count} points");
        }
    }
}
