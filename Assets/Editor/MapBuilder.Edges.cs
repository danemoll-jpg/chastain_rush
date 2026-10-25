using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChastainRush
{
    // Round 66 (3, Dan: "CARS STOP NATURALLY AT THE EDGE: roads that leave the map end in 'Road Closed' barricades / a detour (road-crew style),
    // highways (I-75, I-575, US 41 and the like) in a jersey-barrier closure where traffic turns round (traffic and police nets end there cleanly)"):
    // every drivable road whose end lies on the map's edge is closed ClosureBack (a highway HighwayBack) short of the edge, measured along the road
    // (through the short OSM pieces it is split into):
    //   a highway (motorway, trunk, primary and their links): a row of concrete jersey barriers across its carriageway with a ROAD CLOSED board
    //   behind them and cones before; every other road: type III barricades across it, ROAD CLOSED on the middle one, a DETOUR sign at the right
    //   before it. All solid (the last stop before the edge; MapEdge's push-back and wall cover the ground off the roads).
    // The traffic network and the police's roads end short of the closure (EdgeTrim): a two-way road's end there is a turn-round node (Traffic
    // turns round there, whatever the road's size), and a divided highway's two carriageways are joined by a turnaround through the median ahead
    // of the cut (EdgeTurnLines), so its traffic turns round too. World (the closures, the network's shape); the cars on it are the game layer.
    public static partial class MapBuilder
    {
        const float ClosureBack = 48f, HighwayBack = 70f, TrafficCut = 8f, TurnCut = 26f;
        static readonly string[] EdgeClasses = { "motorway", "motorway_link", "trunk", "trunk_link", "primary", "primary_link", "secondary", "secondary_link", "tertiary", "tertiary_link", "unclassified", "residential", "living_street", "service" };
        static readonly HashSet<string> EdgeHighway = new HashSet<string> { "motorway", "motorway_link", "trunk", "trunk_link", "primary" };

        sealed class EdgeClosure
        {
            public Vector2 at, dir;            // the closure's middle on the road, the road's way out (toward the edge)
            public Vector2 cut;                // where the traffic network ends (inside the closure)
            public float half; public string cls, name; public long osm; public bool oneway, highway, inbound, paired;
        }
        static MapData edgePlanFor; static List<EdgeClosure> edgeClosures; static Dictionary<int, List<Vector2>> edgeTrim;
        static List<(List<Vector2> pts, string cls, float half, string name)> edgeTurnLines;

        static bool OnMapEdge(Rect m, Vector2 p) => Mathf.Min(Mathf.Min(p.x - m.xMin, m.xMax - p.x), Mathf.Min(p.y - m.yMin, m.yMax - p.y)) < 3f;

        // the plan, made once per map
        static void PlanEdges(MapData map)
        {
            if (edgePlanFor == map && edgeClosures != null) return;
            edgePlanFor = map; edgeClosures = new List<EdgeClosure>(); edgeTrim = new Dictionary<int, List<Vector2>>(); edgeTurnLines = new List<(List<Vector2>, string, float, string)>();
            var ext = MapExtent(map);
            var roads = map.roads; var pts = new List<Vector2>[roads.Length];
            for (int i = 0; i < roads.Length; i++)
                if (roads[i].points != null && roads[i].points.Length >= 4 && !roads[i].tunnel && System.Array.IndexOf(EdgeClasses, roads[i].@class) >= 0) pts[i] = ToPoints(roads[i].points);
            Vector2Int Key(Vector2 p) => new Vector2Int(Mathf.RoundToInt(p.x * 2f), Mathf.RoundToInt(p.y * 2f));
            var ends = new Dictionary<Vector2Int, List<int>>();
            for (int i = 0; i < roads.Length; i++) if (pts[i] != null) foreach (var p in new[] { pts[i][0], pts[i][pts[i].Count - 1] }) { var k = Key(p); if (!ends.TryGetValue(k, out var l)) ends[k] = l = new List<int>(); l.Add(i); }
            var cut = new Dictionary<int, (float fromStart, float fromEnd)>();   // per road: metres trimmed off its start / end
            var dropped = new HashSet<int>();
            for (int r0 = 0; r0 < roads.Length; r0++)
            {
                if (pts[r0] == null) continue;
                for (int side = 0; side < 2; side++)
                {
                    var p0 = pts[r0]; var edgePt = side == 0 ? p0[0] : p0[p0.Count - 1];
                    if (!OnMapEdge(ext, edgePt)) continue;
                    bool hw = EdgeHighway.Contains(roads[r0].@class);
                    // the chain inward from the edge: (road, walked from its start?) pieces
                    var chain = new List<(int road, bool fromStart)> { (r0, side == 0) }; var line = new List<Vector2>(); var seen = new HashSet<int> { r0 };
                    float need = (hw ? HighwayBack : ClosureBack) + (roads[r0].oneway ? TurnCut : TrafficCut) + 4f;
                    void Append(int road, bool fromStart) { var q = pts[road]; if (fromStart) for (int i = 0; i < q.Count; i++) { if (line.Count == 0 || i > 0) line.Add(q[i]); } else for (int i = q.Count - 1; i >= 0; i--) { if (line.Count == 0 || i < q.Count - 1) line.Add(q[i]); } }
                    Append(r0, side == 0);
                    while (Length(line) < need)
                    {
                        var tail = line[line.Count - 1]; if (!ends.TryGetValue(Key(tail), out var nb)) break;
                        int next = nb.Where(n => !seen.Contains(n) && (roads[n].name == roads[r0].name || roads[n].@class == roads[r0].@class)).DefaultIfEmpty(-1).OrderBy(n => n < 0 ? 0 : roads[n].name == roads[r0].name ? 0 : 1).First();
                        if (next < 0 || nb.Count > 2) break;   // a junction: the closure stays on this side of it
                        bool fs = (pts[next][0] - tail).sqrMagnitude < 1f; chain.Add((next, fs)); seen.Add(next); Append(next, fs);
                    }
                    float total = Length(line), back = Mathf.Min(hw ? HighwayBack : ClosureBack, total - 2f); if (back < 6f) continue;
                    float cutAt = Mathf.Min(back + (roads[r0].oneway ? TurnCut : TrafficCut), total);
                    var c = new EdgeClosure
                    {
                        at = PointAt(line, back), dir = -DirAt(line, back), cut = PointAt(line, cutAt), half = roads[r0].width * .5f, cls = roads[r0].@class, name = roads[r0].name ?? "",
                        osm = roads[r0].osm_id, oneway = roads[r0].oneway, highway = hw, inbound = side == 1,
                    };
                    edgeClosures.Add(c);
                    // trim the chain at the cut: every piece wholly beyond it dropped, the one it falls in shortened
                    float s = 0f;
                    foreach (var (road, fs) in chain)
                    {
                        float len = Length(pts[road]);
                        if (s + len <= cutAt + .01f) { dropped.Add(road); s += len; continue; }
                        float off = cutAt - s; cut.TryGetValue(road, out var cc);
                        if (fs) cc.fromStart = Mathf.Max(cc.fromStart, off); else cc.fromEnd = Mathf.Max(cc.fromEnd, off);
                        cut[road] = cc; break;
                    }
                }
            }
            foreach (var kv in cut)
            {
                if (dropped.Contains(kv.Key)) continue; var q = pts[kv.Key]; float len = Length(q);
                if (kv.Value.fromStart + kv.Value.fromEnd > len - 2f) { dropped.Add(kv.Key); continue; }
                edgeTrim[kv.Key] = Sub(q, kv.Value.fromStart, len - kv.Value.fromEnd);
            }
            foreach (int d in dropped) edgeTrim[d] = null;
            // a divided highway's carriageways: the one going out joined to the one coming back by a turnaround ahead of the cut
            var ins = edgeClosures.Where(c => c.oneway && c.inbound).ToList(); var outs = edgeClosures.Where(c => c.oneway && !c.inbound).ToList();
            foreach (var a in ins)
            {
                var b = outs.Where(o => !o.paired && Vector2.Dot(o.dir, a.dir) > .7f && (o.cut - a.cut).magnitude < 90f && (o.cls == a.cls || o.name == a.name)).OrderBy(o => (o.cut - a.cut).magnitude).FirstOrDefault();
                if (b == null) continue; a.paired = b.paired = true;
                var P0 = a.cut; var P3 = b.cut; var P1 = P0 + a.dir * 14f; var P2 = P3 + b.dir * 14f; var u = new List<Vector2>();
                for (int i = 0; i <= 12; i++) { float t = i / 12f, m = 1 - t; u.Add(m * m * m * P0 + 3 * m * m * t * P1 + 3 * m * t * t * P2 + t * t * t * P3); }
                edgeTurnLines.Add((u, a.cls, Mathf.Min(a.half, 4f), a.name));
            }
            Debug.Log($"[Round66] road closures at the map's edge: {edgeClosures.Count} ({edgeClosures.Count(c => c.highway)} highway carriageways with jersey barriers), {edgeTurnLines.Count} divided-highway turnarounds; " +
                      $"{edgeTrim.Count(kv => kv.Value == null)} short road pieces past a closure left out of the networks, {edgeTrim.Count(kv => kv.Value != null)} shortened");
        }
        // a road's points as the traffic and police networks take them (trimmed at a closure; null: past one)
        static List<Vector2> EdgeTrimmed(MapData map, int road, List<Vector2> pts)
        {
            PlanEdges(map);
            return edgeTrim.TryGetValue(road, out var t) ? (t == null ? null : new List<Vector2>(t)) : pts;
        }

        static float Length(List<Vector2> l) { float s = 0f; for (int i = 1; i < l.Count; i++) s += (l[i] - l[i - 1]).magnitude; return s; }
        static Vector2 PointAt(List<Vector2> l, float s)
        {
            for (int i = 1; i < l.Count; i++) { float d = (l[i] - l[i - 1]).magnitude; if (s <= d) return Vector2.Lerp(l[i - 1], l[i], d < 1e-4f ? 0f : s / d); s -= d; }
            return l[l.Count - 1];
        }
        static Vector2 DirAt(List<Vector2> l, float s)
        {
            for (int i = 1; i < l.Count; i++) { float d = (l[i] - l[i - 1]).magnitude; if (s <= d || i == l.Count - 1) return (l[i] - l[i - 1]).normalized; s -= d; }
            return Vector2.up;
        }
        static List<Vector2> Sub(List<Vector2> l, float a, float b)
        {
            var o = new List<Vector2> { PointAt(l, a) }; float s = 0f;
            for (int i = 1; i < l.Count; i++) { s += (l[i] - l[i - 1]).magnitude; if (s > a + .05f && s < b - .05f) o.Add(l[i]); }
            o.Add(PointAt(l, b)); return o;
        }

        // ------------------------------------------------------------ the closures in the world

        static readonly Color JerseyGrey = new Color(.74f, .73f, .70f), SignBack = new Color(.62f, .63f, .64f), ConeOrange = new Color(.95f, .40f, .08f);
        static void BuildEdges(MapData map, TerrainGrid terrain, Materials mats, Transform world)
        {
            PlanEdges(map);
            var root = new GameObject("Road closures (map edge)"); root.transform.SetParent(world, false);
            var data = root.AddComponent<RoadClosures>();
            var closed = RoadSignMaterial("RoadClosed"); var detour = RoadSignMaterial("Detour");
            var mb = new MeshBuilder(); var colliders = new GameObject("Road closure colliders").transform; colliders.SetParent(root.transform, false);
            var signs = new GameObject("Road closure signs").transform; signs.SetParent(root.transform, false);
            int n = 0, hugging = 0; var ext = MapExtent(map);
            foreach (var c in edgeClosures)
            {
                // a road that runs along the edge (its closure would stand on the edge line): none; the edge's push-back stops a car there (MapEdge)
                if (Mathf.Min(Mathf.Min(c.at.x - ext.xMin, ext.xMax - c.at.x), Mathf.Min(c.at.y - ext.yMin, ext.yMax - c.at.y)) < 12f) { hugging++; continue; }
                var d3 = new Vector3(c.dir.x, 0f, c.dir.y); var across = new Vector3(c.dir.y, 0f, -c.dir.x);   // across: to the right looking out
                float half = c.half + (c.highway ? 4.5f : 3.5f); var mid = new Vector3(c.at.x, Ground(terrain, c.at.x, c.at.y), c.at.y);   // past the shoulders: no way round the ends
                if (c.highway)
                {
                    // jersey barriers, 3 m each, end to end across the carriageway and its shoulders
                    int segs = Mathf.CeilToInt(2f * half / 3f); float w = segs * 3f;
                    for (int i = 0; i < segs; i++)
                    {
                        var a2 = mid + across * (-w * .5f + i * 3f + .05f); var b2 = mid + across * (-w * .5f + (i + 1) * 3f - .05f);
                        a2.y = Ground(terrain, a2.x, a2.z); b2.y = Ground(terrain, b2.x, b2.z);
                        Beam(mb, a2 + Vector3.up * .16f, b2 + Vector3.up * .16f, .6f, .36f, JerseyGrey);   // the flared foot
                        Beam(mb, a2 + Vector3.up * .55f, b2 + Vector3.up * .55f, .26f, .52f, JerseyGrey);   // the upright
                        Solid(colliders, (a2 + b2) * .5f + Vector3.up * 1.2f, b2 - a2, 3f, 2.6f, .62f);
                    }
                    // cones before them, a row across
                    for (float t = -half + .8f; t <= half - .8f; t += 2.2f) Cone(mb, terrain, mid - d3 * 6f + across * t);
                    Board(mb, signs, terrain, mid + d3 * 1.6f, d3, 1.5f, .94f, 1.2f, closed);
                }
                else
                {
                    // type III barricades, 2.4 m each, across the road
                    int segs = Mathf.CeilToInt(2f * half / 2.5f); float w = segs * 2.5f;
                    for (int i = 0; i < segs; i++)
                    {
                        var cpos = mid + across * (-w * .5f + (i + .5f) * 2.5f); cpos.y = Ground(terrain, cpos.x, cpos.z);
                        foreach (float h in new[] { .62f, 1.0f })
                        {
                            var b0 = cpos - across * 1.2f + Vector3.up * h;
                            for (int k = 0; k < 6; k++) Beam(mb, b0 + across * (.4f * k), b0 + across * (.4f * (k + 1)), .04f, .22f, k % 2 == 0 ? BarrierOrange : BarrierWhite);
                        }
                        foreach (float s in new[] { -.9f, .9f })
                        {
                            var top = cpos + across * s + Vector3.up * 1.12f;
                            Beam(mb, cpos + across * s - d3 * .35f, top, .05f, .05f, BarrierWhite, across); Beam(mb, cpos + across * s + d3 * .35f, top, .05f, .05f, BarrierWhite, across);
                        }
                        Solid(colliders, cpos + Vector3.up * 1.2f, across, 2.45f, 2.6f, .8f);
                    }
                    Board(mb, signs, terrain, mid + d3 * .2f, d3, 1.2f, .75f, 1.25f, closed);   // on the middle barricade
                    // DETOUR on a post at the right, 12 m before
                    var dp = mid - d3 * 12f + across * (half + .6f);
                    Board(mb, signs, terrain, dp, d3, 1.2f, .6f, 1.7f, detour);
                }
                data.Add(c.at, c.dir, c.half, c.highway, c.name, c.cls, c.cut);
                n++;
            }
            mb.ToObject("Road closures", mats.House, root.transform, false);
            Debug.Log($"[Round66] built {n} road closures at the map's edge; {hugging} roads that run along the edge left to its push-back");
        }

        // a closure's collider: 2.6 m tall whatever the barrier's own height (a lower one let a fast car's wheels ride up it and launch it over)
        static void Solid(Transform parent, Vector3 centre, Vector3 along, float len, float h, float depth)
        {
            var go = new GameObject("Closure"); go.transform.SetParent(parent, false); go.isStatic = true;
            along.y = 0f; go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(Vector3.Cross(Vector3.up, along.normalized), Vector3.up));
            var b = go.AddComponent<BoxCollider>(); b.size = new Vector3(len, h, depth);
        }
        static void Cone(MeshBuilder mb, TerrainGrid terrain, Vector3 at)
        {
            at.y = Ground(terrain, at.x, at.z);
            Post(mb, at, .05f, .42f, Color.black); Post(mb, at + Vector3.up * .05f, .28f, .26f, ConeOrange); Post(mb, at + Vector3.up * .33f, .12f, .18f, BarrierWhite); Post(mb, at + Vector3.up * .45f, .26f, .12f, ConeOrange);
        }
        // a sign board on two posts (or one, under 1.3 m wide), its face toward a driver coming out (against 'outward'), its grey back away
        static void Board(MeshBuilder mb, Transform parent, TerrainGrid terrain, Vector3 at, Vector3 outward, float w, float h, float bottom, Material face)
        {
            at.y = Ground(terrain, at.x, at.z); var across = new Vector3(outward.z, 0f, -outward.x);
            var centre = at + Vector3.up * (bottom + h * .5f);
            if (w >= 1.3f) foreach (float s in new[] { -w * .35f, w * .35f }) Post(mb, at + across * s + outward * .06f - Vector3.up * .2f, bottom + h + .1f, .08f, SignBack);
            else Post(mb, at + outward * .06f - Vector3.up * .2f, bottom + h + .1f, .08f, SignBack);
            mb.Tint = SignBack;
            var c0 = centre + outward * .025f; var hw = across * (w * .5f); var hh = Vector3.up * (h * .5f);
            Face(mb, c0 - hw - hh, c0 + hw - hh, c0 + hw + hh, c0 - hw + hh, outward);
            if (face == null) return;
            var go = new GameObject("Road closure sign"); go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(outward, Vector3.up));
            var mesh = new Mesh { name = "ClosureSign" };
            float x = w * .5f, y = h * .5f;
            mesh.vertices = new[] { new Vector3(-x, -y, 0), new Vector3(x, -y, 0), new Vector3(x, y, 0), new Vector3(-x, y, 0) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/ClosureSign_{face.name}_{Mathf.Abs(centre.x.GetHashCode() ^ (centre.z * 3.1f).GetHashCode())}.asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = face; r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
