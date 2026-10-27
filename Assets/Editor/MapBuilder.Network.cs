using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 23 follow-ups (Part B and Part C, Dan 2026-10-13 01:01: "I think go ahead and build it out (maybe wait on the race) so
// that I can get a better feel"): the underground pipe network, from Data/pipes/network.json (Tools/mapgen/pipe_network.py, which
// lays out every pipe, its floor and every chamber's openings; the layout follows Docs/PIPE-NETWORK-PROPOSAL.md). World, not race:
// no race route, gates or switches here. Built: whatever is at or under the file's build_stage.
//   tubes     round pipes swept along their floor line (5 m mains with a painted color stripe each side; 3.5 m maze branches,
//             unmarked), to the pipe standard of Round 19 (P1): round all the way (wall riding, DrainPipe), stained concrete (the
//             pipes' own material and texture), lamps along the crown, a trickle down the invert flowing on toward the hub
//             (PipeStream), resets in the bottom, headlights on. Where a main runs under a pond: a glass tube in a deep trench
//             with fish (MapBuilder.ClearPipe.cs, the same parts as P1). A flooded sag: water that slows you (ShallowWater)
//   chambers  every place pipes meet is a concrete room (MapBuilder.NetworkChamber); a drop shaft is a tall room one pipe
//             enters high in the wall; a dead end has a small room to turn round in; the hub is its own build
//             (MapBuilder.NetworkHub.cs)
//   stubs     a built chamber's opening onto a pipe not built yet: 6 m of it and a closed, lit grille (as J1's, Round 23)
//   exits     open_b pipes of MapBuilder.Pipes.cs (a cut and headwall in a bank, P1's parts); outfalls with a lip over the water
//   manholes  a riser climbs from the pipe up at 50 degrees along the street to a hole in it, covered by a big flush lid that
//             holds a car from above and flies off when a vehicle bursts up from below (ManholeLid); the street's surface,
//             paint and the terrain are cut round it
//   cells     NetworkCells (run time) switches each cell's meshes, lamps and water on only near the camera (the frame-rate plan)
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class NetEnd { public string kind, chamber, pipe, manhole, exit, lvl; public int wall, side; public float t, level, inset; public bool high; }
        [Serializable] class NetGlass { public int pond; public float s0, s1, surface; }
        [Serializable] class NetFlood { public float s0, s1, level; }
        [Serializable] class NetTube { public string id, name; public float d; public bool main; public int stage, to_hub; public float[] stripe; public NetEnd a, b; public float[] pts; public NetGlass[] glass; public NetFlood flood; }
        [Serializable] class NetOpening { public int wall; public float t, d, inset; public string tube, lvl; public bool high; }
        [Serializable] class NetChamber { public string id, name, kind, sign, hub_tube; public float x, z, yaw, length, width, height, floor, drop, mid, top, inset_mid, inset_floor, ceiling, roof_open; public int stage; public NetOpening[] openings; }   // Round 44: roof_open, LD's hole under the lake drain
        [Serializable] class NetExit { public string id, name, tube; public float[] a, b; public float b_floor, d, ground_a; public int outfall, stage; }
        [Serializable] class NetManhole { public string id, name, tube, road; public float x, z, y, d, height; public float[] dir; public int stage, riser_points; }
        [Serializable] class NetExpress { public string id, name, tube; public float s0, s1, top; }   // Round 25 (B2): the pipe express's stretches; Round 26: top, its own top speed
        [Serializable] class NetFile { public int build_stage; public NetTube[] tubes; public NetChamber[] chambers; public NetExit[] exits; public NetManhole[] manholes; public NetExpress[] express; }

        // a mesh with UVs (the pipes' stained concrete needs them), one-sided faces (from Round 23's MapBuilder.NetworkEntrance.cs)
        sealed class UvMesh
        {
            public readonly List<Vector3> V = new List<Vector3>(); public readonly List<Vector2> U = new List<Vector2>(); public readonly List<int> T = new List<int>();
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 facing)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0f) { (b, d) = (d, b); (ub, ud) = (ud, ub); }
                int i = V.Count; V.Add(a); V.Add(b); V.Add(c); V.Add(d); U.Add(ua); U.Add(ub); U.Add(uc); U.Add(ud);
                T.Add(i); T.Add(i + 1); T.Add(i + 2); T.Add(i); T.Add(i + 2); T.Add(i + 3);
            }
            public GameObject ToObject(string name, Material m, Transform parent, bool collider)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(V); mesh.SetUVs(0, U); mesh.SetTriangles(T, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
                mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/{name}.asset");
                var go = new GameObject(name); go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
                if (collider) { var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; mc.sharedMaterial = RampMaterial(); }
                return go;
            }
        }

        const string NetworkFilePath = "Data/pipes/network.json";
        const float NetLampEvery = 18f, NetCell = 60f;

        // Round 23 follow-ups (the exe check: motorcycles stalled on the lip where a round opening's rim rose off a room's flat floor): at a
        // chamber's wall an opening is D-shaped, its lower half square (the room's floor runs straight in, flat, 2R wide) and its upper half
        // round; the pipe eases from that D to round over its first NetDEase m. (lateral, height over the invert) of a ring point at angle
        // th (0 at the invert), d01 1 at the wall, 0 round
        const float NetDEase = 6f;
        static Vector2 NetRingPoint(float R, float th, float d01)
        {
            var c = new Vector2(Mathf.Sin(th) * R, R - Mathf.Cos(th) * R);
            if (d01 <= 0f || Mathf.Cos(th) <= 0f) return c;
            var d = new Vector2(Mathf.Sin(th), -Mathf.Cos(th));
            float k = Mathf.Min(Mathf.Abs(d.x) > 1e-4f ? R / Mathf.Abs(d.x) : float.MaxValue, Mathf.Abs(d.y) > 1e-4f ? R / Mathf.Abs(d.y) : float.MaxValue);
            var sq = new Vector2(d.x * k, R + d.y * k);
            return Vector2.Lerp(c, sq, d01);
        }

        sealed class NetTubeGeo
        {
            public NetTube J; public Vector3[] P; public float[] S; public float R; public int RiserFrom = -1;   // floor points, arc length; the riser's first point
            public Vector3 T(int i) { int a = Mathf.Max(0, i - 1), b = Mathf.Min(P.Length - 1, i + 1); return (P[b] - P[a]).normalized; }
            public Vector3 Side(int i) { var t = T(i); var s = Vector3.Cross(Vector3.up, t); return s.sqrMagnitude < 1e-6f ? Vector3.right : s.normalized; }
            public Vector3 Up(int i) => Vector3.Cross(T(i), Side(i)).normalized;
            public Vector3 Axis(int i) => P[i] + Up(i) * R;
            public int At(float s) { int i = Array.BinarySearch(S, s); if (i < 0) i = ~i; return Mathf.Clamp(i, 0, S.Length - 1); }
            // how far ring i is into the D at a chamber's wall (1 at the wall, 0 from NetDEase m on)
            public float D01(int i)
            {
                float L = S[S.Length - 1], m = 0f;
                if (J.a != null && J.a.kind == "ch") m = Mathf.Max(m, Smooth01(1f - S[i] / NetDEase));
                if (J.b != null && J.b.kind == "ch") m = Mathf.Max(m, Smooth01(1f - (L - S[i]) / NetDEase));
                return m;
            }
            public Vector3 RingAt(int i, float th) { var q = NetRingPoint(R, th, D01(i)); return P[i] + Side(i) * q.x + Up(i) * q.y; }
        }

        sealed class Network
        {
            public NetFile F; public int Stage;
            public readonly Dictionary<string, NetChamber> Ch = new Dictionary<string, NetChamber>();
            public readonly Dictionary<string, NetTubeGeo> Geo = new Dictionary<string, NetTubeGeo>();
            public readonly List<(Vector2 c, float r)> Holes = new List<(Vector2, float)>();     // the street cut round each manhole (roads, paint)
            public readonly List<(NetManhole m, Vector2 c, float r, float y)> Lids = new List<(NetManhole, Vector2, float, float)>();
            public readonly Dictionary<string, (float half, float lift)> LidRoad = new Dictionary<string, (float, float)>();   // the street's half width and lift at each manhole
            // Round 25 (A1): which hole is which lid's; each lid's terrain hole cells (centers, cell size); the street's own triangles
            // cut out over each lid (its hatch: MapBuilder.Network.cs BuildManholeLid)
            public readonly Dictionary<int, int> HoleLid = new Dictionary<int, int>();
            public readonly Dictionary<int, (List<Vector2> cells, float cx, float cz)> LidCells = new Dictionary<int, (List<Vector2>, float, float)>();
            public readonly Dictionary<int, List<(Material m, Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)>> LidStreet = new Dictionary<int, List<(Material, Vector3, Vector3, Vector3, Color, Color, Color)>>();
            public readonly List<(NetTubeGeo g, NetGlass gl, float T, float[] TS)> Glass = new List<(NetTubeGeo, NetGlass, float, float[])>();   // Round 24 Part B: TS, the deep water's half width every meter from s0
            public bool Built(NetTube t) => t.stage <= Stage;
            public bool Built(NetChamber c) => c.stage <= Stage;
            public int TreesRemoved;
            public bool RejectTree(float x, float z)
            {
                var q = new Vector2(x, z);
                foreach (var (c, r) in Holes) if ((q - c).sqrMagnitude < (r + 3f) * (r + 3f)) { TreesRemoved++; return true; }
                return false;
            }
        }
        static Network Net;   // the surface cut (Chunks.ToObjects) and the map picture read it

        static Network LoadNetwork()
        {
            string path = Path.Combine(ProjectRoot, NetworkFilePath);
            if (!File.Exists(path)) return null;
            var n = new Network { F = JsonUtility.FromJson<NetFile>(File.ReadAllText(path)) };
            n.Stage = n.F.build_stage;
            foreach (var c in n.F.chambers) n.Ch[c.id] = c;
            foreach (var t in n.F.tubes)
            {
                int k = t.pts.Length / 3; var g = new NetTubeGeo { J = t, P = new Vector3[k], S = new float[k], R = t.d * 0.5f };
                for (int i = 0; i < k; i++) { g.P[i] = new Vector3(t.pts[3 * i], t.pts[3 * i + 1], t.pts[3 * i + 2]); if (i > 0) g.S[i] = g.S[i - 1] + Vector3.Distance(g.P[i], g.P[i - 1]); }
                if (t.b != null && t.b.kind == "mh") { var mh = n.F.manholes.First(m => m.tube == t.id); g.RiserFrom = k - mh.riser_points - 1; }
                n.Geo[t.id] = g;
            }
            Net = n;
            Debug.Log($"[Round23+] network: build_stage {n.Stage}; {n.F.tubes.Count(n.Built)} of {n.F.tubes.Length} pipes, {n.F.chambers.Count(n.Built)} of {n.F.chambers.Length} chambers built");
            return n;
        }

        // the exits and outfalls of built pipes, as open_b pipes for MapBuilder.Pipes.cs (a cut and headwall in the bank, P1's parts)
        static IEnumerable<PipeJson> NetworkExitPipes(Network n)
        {
            if (n == null) yield break;
            foreach (var x in n.F.exits)
            {
                var t = n.F.tubes.First(tt => tt.id == x.tube);
                if (!n.Built(t)) continue;
                yield return new PipeJson { id = x.id, name = x.name, a = x.a, b = x.b, diameter_m = x.d, cover_m = 0.6f, cut_slope = 0.18f, open_b = true, b_floor = x.b_floor, outfall = x.outfall > 0 };
            }
        }

        // ---------------------------------------------------------------- planning (before anything samples the ground)
        static void PlanNetwork(TerrainGrid terrain, Network n, MapData map)
        {
            if (n == null) return;
            // manholes: terrain holes where the street's ground is inside the riser's top, the street cut round it
            foreach (var m in n.F.manholes)
            {
                var t = n.F.tubes.First(tt => tt.id == m.tube); if (!n.Built(t)) continue;
                var g = n.Geo[t.id];
                var cells = new List<Vector2>();
                bool Inside(Vector3 q)
                {
                    for (int i = Mathf.Max(1, g.RiserFrom - 2); i < g.P.Length; i++)
                    {
                        var a = g.Axis(i - 1); var b = g.Axis(i); var ab = b - a; float tt = Mathf.Clamp01(Vector3.Dot(q - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                        if (i == g.P.Length - 1 && Vector3.Dot(q - b, g.T(i)) > 0f) continue;   // past the riser's top ring
                        if ((a + ab * tt - q).magnitude < g.R - 0.05f) return true;
                    }
                    return false;
                }
                var top = new Vector2(m.x, m.z); var dir = new Vector2(m.dir[0], m.dir[1]);
                var box = BoundsOf(new[] { top + dir * 2f, top - dir * 9f, top + new Vector2(-dir.y, dir.x) * 4f, top - new Vector2(-dir.y, dir.x) * 4f }, 2f);
                int holed = SetHoles(terrain, box, q => { if (Inside(new Vector3(q.x, Ground(terrain, q.x, q.y), q.y))) { cells.Add(q); return true; } return false; });
                if (cells.Count == 0) { Debug.LogError($"[Round23+] manhole {m.id}: the riser never meets the street (no hole)"); continue; }
                var c = new Vector2(cells.Average(q => q.x), cells.Average(q => q.y)); float r = cells.Max(q => (q - c).magnitude) + 0.9f;
                n.HoleLid[n.Holes.Count] = n.Lids.Count;
                { var tile = terrain.At(c.x, c.y).terrainData; n.LidCells[n.Lids.Count] = (cells, tile.size.x / tile.holesResolution, tile.size.z / tile.holesResolution); }
                n.Holes.Add((c, r + 0.4f)); n.Lids.Add((m, c, r, Ground(terrain, c.x, c.y)));
                float half = 3.5f, lift = 0.06f * vScale; float bestD = float.MaxValue;   // the street it is in: its half width and lift (the patch round the lid)
                foreach (var road in map.roads)
                {
                    if (road.name != m.road) continue; var pts = ToPoints(road.points);
                    for (int i = 0; i + 1 < pts.Count; i++)
                    {
                        var ab = pts[i + 1] - pts[i]; float tt = Mathf.Clamp01(Vector2.Dot(c - pts[i], ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); float d = (pts[i] + ab * tt - c).magnitude;
                        if (d < bestD) { bestD = d; half = road.width * 0.5f; lift = (RoadLift.TryGetValue(road.@class, out var l) ? l : 0.06f) * vScale; }
                    }
                }
                n.LidRoad[m.id] = (half, lift);
                Debug.Log($"[Round23+] manhole {m.id} in {m.road}: riser {m.height:0.0} m high, {holed} terrain hole cells, the lid {2f * r:0.0} m across at ({c.x:0.0}, {c.y:0.0})");
            }
            // the hub's daylight grates: no trees on them (MapBuilder.NetworkHub.cs cuts their holes)
            foreach (var c in n.F.chambers)
                if (c.kind == "hub" && n.Built(c)) foreach (var g in HubGrates(c)) n.Holes.Add((g, HubGrateHalf * 1.5f));
            // glass: terrain holes over the trench round each glass stretch (in the pond; its water surface hides them from above)
            foreach (var t in n.F.tubes)
            {
                if (!n.Built(t) || t.glass == null) continue;
                var g = n.Geo[t.id];
                foreach (var gl in t.glass)
                {
                    if (gl.s1 - gl.s0 < 8f || gl.pond < 0 || gl.pond >= Beds.Rings.Count) continue;
                    var ring = Beds.Rings[gl.pond];
                    // Round 24 Part B (item 5, Dan: "if I am in the middle of a lake I would want it to look like I am in the middle of a lake
                    // then in a channel"): the deep water's half width at the top at each meter: out to where the pond's own bed is at full
                    // depth (4 m in from the shore, PondSlope), up to 40 m, smoothed along (it was 8 m at most: a channel round the tube)
                    int nT = Mathf.FloorToInt(gl.s1 - gl.s0) + 1; var raw = new float[nT]; var TS = new float[nT];
                    for (int k = 0; k < nT; k++)
                    {
                        // Round 25 (A5 / A7): the bowl's rim 1 m in from the shore (its own shelf is 0.2 m under the surface there)
                        int i = g.At(gl.s0 + k); raw[k] = Mathf.Clamp(DistToEdges(new Vector2(g.P[i].x, g.P[i].z), ring) - 1f, g.R + 1.2f, 45f);
                        // and inside the bends' radius (the bed is laid out square to the line: past the radius it would fold over itself)
                        int i0 = g.At(gl.s0 + k - 5f), i1 = g.At(gl.s0 + k + 5f);
                        Vector2 A2 = new Vector2(g.P[i0].x, g.P[i0].z), B2 = new Vector2(g.P[i].x, g.P[i].z), C2 = new Vector2(g.P[i1].x, g.P[i1].z);
                        float area2 = Mathf.Abs((B2.x - A2.x) * (C2.y - A2.y) - (B2.y - A2.y) * (C2.x - A2.x));
                        if (area2 > 1e-3f) raw[k] = Mathf.Max(g.R + 1.2f, Mathf.Min(raw[k], 0.7f * (A2 - B2).magnitude * (B2 - C2).magnitude * (C2 - A2).magnitude / (2f * area2)));
                    }
                    for (int k = 0; k < nT; k++)
                    {
                        float lo = float.MaxValue; for (int j = Mathf.Max(0, k - 6); j <= Mathf.Min(nT - 1, k + 6); j++) lo = Mathf.Min(lo, raw[j] + Mathf.Abs(j - k) * 0.6f);   // never past the shore, eased along
                        TS[k] = lo;
                    }
                    float T = TS.Min();
                    // Round 25 (A5, A7): holes only where the bowl of deep water is under the ground and under the water (CutBowlHoles)
                    float surface = Beds.Level.TryGetValue(gl.pond, out float lv0) ? lv0 + PondLift : gl.surface;
                    int holed = CutBowlHoles(terrain, NetGlassFrame(g, gl, TS, surface));
                    n.Glass.Add((g, gl, T, TS));
                    Debug.Log($"[Round23+] pipe {t.id}: glass under pond W{gl.pond} from {gl.s0:0} to {gl.s1:0} m ({gl.s1 - gl.s0:0} m), the deep water's rim {2f * T:0.0} to {2f * TS.Max():0.0} m across (Round 25: a bowl in the bed, 1 m in from the shore), {holed} terrain hole cells (under water only)");
                }
            }
        }

        // holes in the terrain where 'hole' is true for a cell's center inside 'area'; the count
        static int SetHoles(TerrainGrid g, Rect area, Func<Vector2, bool> hole)
        {
            int cells = 0;
            foreach (var tile in g.All())
            {
                var data = tile.terrainData; int hr = data.holesResolution; var size = data.size; var pos = tile.transform.position;
                if (area.xMax < pos.x || area.xMin > pos.x + size.x || area.yMax < pos.z || area.yMin > pos.z + size.z) continue;
                float cx = size.x / hr, cz = size.z / hr;
                int x0 = Mathf.Clamp(Mathf.FloorToInt((area.xMin - pos.x) / cx), 0, hr - 1), x1 = Mathf.Clamp(Mathf.CeilToInt((area.xMax - pos.x) / cx), 0, hr - 1);
                int z0 = Mathf.Clamp(Mathf.FloorToInt((area.yMin - pos.z) / cz), 0, hr - 1), z1 = Mathf.Clamp(Mathf.CeilToInt((area.yMax - pos.z) / cz), 0, hr - 1);
                if (x1 <= x0 || z1 <= z0) continue;
                var holes = data.GetHoles(x0, z0, x1 - x0 + 1, z1 - z0 + 1); bool any = false;
                for (int j = 0; j <= z1 - z0; j++)
                    for (int i = 0; i <= x1 - x0; i++)
                    {
                        var q = new Vector2(pos.x + (x0 + i + 0.5f) * cx, pos.z + (z0 + j + 0.5f) * cz);
                        if (!holes[j, i] || !hole(q)) continue;
                        holes[j, i] = false; any = true; cells++;
                    }
                if (any) data.SetHoles(x0, z0, holes);
            }
            return cells;
        }

        // Chunks.ToObjects: no street, paint or sidewalk over a manhole's hole. Round 25 (A1): the triangles cut out over a manhole are
        // kept (with their material and colors) as that manhole's hatch: the street itself, flush, until a vehicle bursts up through it
        static void CutNetworkHoles(MeshBuilder mb, Material mat = null)
        {
            if (Net == null || Net.Holes.Count == 0 || mb.Count == 0) return;
            var v = mb.Verts; var t = mb.Tris; var col = mb.Colours;
            // Round 65 (build speed): only the holes that reach this mesh's bounds (every road and lot mesh tested every triangle against every manhole
            // on the map); in their order, so the cut is the same
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (var p in v) { if (p.x < x0) x0 = p.x; if (p.x > x1) x1 = p.x; if (p.z < z0) z0 = p.z; if (p.z > z1) z1 = p.z; }
            var near = new List<int>();
            for (int h = 0; h < Net.Holes.Count; h++) { var (hc, hr) = Net.Holes[h]; if (hc.x + hr > x0 && hc.x - hr < x1 && hc.y + hr > z0 && hc.y - hr < z1) near.Add(h); }
            if (near.Count == 0) return;
            var keep = new List<int>(t.Count);
            for (int i = 0; i + 2 < t.Count; i += 3)
            {
                var c = (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f; var q = new Vector2(c.x, c.z); bool cut = false;
                foreach (int h in near)
                {
                    var (hc, hr) = Net.Holes[h]; if ((q - hc).sqrMagnitude >= hr * hr) continue;
                    cut = true;
                    if (mat != null && Net.HoleLid.TryGetValue(h, out int lid))
                    {
                        if (!Net.LidStreet.TryGetValue(lid, out var l)) Net.LidStreet[lid] = l = new List<(Material, Vector3, Vector3, Vector3, Color, Color, Color)>();
                        l.Add((mat, v[t[i]], v[t[i + 1]], v[t[i + 2]], col[t[i]], col[t[i + 1]], col[t[i + 2]]));
                    }
                    break;
                }
                if (!cut) { keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]); }
            }
            if (keep.Count != t.Count) { t.Clear(); t.AddRange(keep); }
        }

        // ---------------------------------------------------------------- the build
        static Material netStripe(Color c)
        {
            string name = $"PipeStripe_{Mathf.RoundToInt(c.r * 255):X2}{Mathf.RoundToInt(c.g * 255):X2}{Mathf.RoundToInt(c.b * 255):X2}";
            var path = $"{GeneratedDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (m) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.25f); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 0.18f);
            m = GenAsset(m, path); return m;
        }
        static Material netGrille;

        sealed class NetCells
        {
            public readonly List<(Transform root, Bounds b)> Cells = new List<(Transform, Bounds)>();
            public Transform Make(Transform parent, string name) { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
        }

        static void BuildNetwork(TerrainGrid terrain, Materials mats, Transform world, Network n, Pipes pipes)
        {
            if (n == null) return;
            var root = new GameObject("PipeNetwork").transform; root.SetParent(world, false);
            if (!pipeConcrete) { Debug.LogError("[Round23+] network: the pipe materials are not made (no pipes built)"); return; }
            netGrille = mats.House;
            var cells = new NetCells();
            int tubes = 0, stubs = 0, lamps = 0; float metres = 0f;
            foreach (var t in n.F.tubes)
            {
                var g = n.Geo[t.id];
                if (n.Built(t)) { lamps += BuildTube(terrain, mats, root, g, cells, n); tubes++; metres += g.S[g.S.Length - 1]; continue; }
                // a stub with a closed grille at each built chamber end of a pipe not built yet
                foreach (var (e, atStart) in new[] { (t.a, true), (t.b, false) })
                    if (e != null && e.kind == "ch" && n.Ch.TryGetValue(e.chamber, out var c) && n.Built(c)) { BuildStub(root, g, atStart, cells); stubs++; }
            }
            int rooms = 0;
            foreach (var c in n.F.chambers)
            {
                if (!n.Built(c)) continue;
                if (c.kind == "hub") BuildHub(terrain, mats, root, c, n, cells);
                else BuildChamberRoom(terrain, mats, root, c, n, cells);
                rooms++;
            }
            for (int li = 0; li < n.Lids.Count; li++) { var (m, c, r, y) = n.Lids[li]; BuildManholeLid(terrain, mats, root, li, m, c, r, y, n.LidRoad.TryGetValue(m.id, out var lr) ? lr : (3.5f, 0.06f), cells); }
            foreach (var (g, gl, T, TS) in n.Glass) BuildNetworkGlass(terrain, mats, root, g, gl, T, TS, cells);
            BuildPipeWarp(root, n, pipes);        // Round 25 (B1): the debug menu's pipe warp places (MapBuilder.NetworkWarp.cs)
            BuildPipeExpress(root, mats, n);      // Round 25 (B2): the pipe express
            // the run time: cells switched on near the camera
            var nc = root.gameObject.AddComponent<NetworkCells>();
            nc.roots = cells.Cells.Select(c => c.root).ToArray(); nc.bounds = cells.Cells.Select(c => c.b).ToArray();
            var labels = new List<(string, Vector3)>();
            foreach (var c in n.F.chambers) if (n.Built(c) && !c.id.StartsWith("K") && !c.id.StartsWith("D") && !c.id.StartsWith("S")) labels.Add((c.kind == "hub" ? "THE HUB" : c.name, new Vector3(c.x, c.floor, c.z)));
            foreach (var m in n.F.manholes) if (n.Built(n.F.tubes.First(t => t.id == m.tube))) labels.Add(($"{m.id} manhole ({m.road})", new Vector3(m.x, m.y, m.z)));
            foreach (var x in n.F.exits) if (n.Built(n.F.tubes.First(t => t.id == x.tube))) labels.Add((x.name.Substring(0, 1) + x.name.Substring(1).ToLowerInvariant(), new Vector3(x.a[0], 0f, x.a[1])));
            nc.labels = labels.Select(l => l.Item1).ToArray(); nc.labelAt = labels.Select(l => l.Item2).ToArray();
            Debug.Log($"[Round23+] network built (stage {n.Stage}): {tubes} pipes, {metres:0} m; {stubs} stubs at closed grilles; {rooms} chambers; {n.Lids.Count} manholes; {n.Glass.Count} glass stretches; {lamps} pipe lamps; {cells.Cells.Count} cells");
        }

        // ---------------------------------------------------------------- a tube
        static int BuildTube(TerrainGrid terrain, Materials mats, Transform parent, NetTubeGeo g, NetCells cells, Network n)
        {
            var t = g.J; float R = g.R; int arc = t.d >= 5f ? 40 : 32;
            var root = new GameObject("Pipe " + t.id + " (" + t.name + ")").transform; root.SetParent(parent, false);
            int N = g.P.Length, lamps = 0;
            var glassAt = new List<(float, float)>(); if (t.glass != null) foreach (var gl in t.glass) if (n.Glass.Any(x => x.gl == gl)) glassAt.Add((gl.s0, gl.s1));
            bool Glass(float s) => glassAt.Any(x => s >= x.Item1 && s <= x.Item2);
            // ring vertices; a manhole riser's rings are cut off at the street
            Vector3 Ring(int i, float th) => g.RingAt(i, th);
            float Gnd(Vector3 v) => Ground(terrain, v.x, v.z);
            bool riserTop(int i) => g.RiserFrom >= 0 && i >= g.RiserFrom;
            // cells along it (60 m)
            int cellStart = 0;
            while (cellStart < N - 1)
            {
                int cellEnd = cellStart; while (cellEnd < N - 1 && g.S[cellEnd] - g.S[cellStart] < NetCell) cellEnd++;
                var cell = cells.Make(root, $"cell {g.S[cellStart]:0}-{g.S[cellEnd]:0} m");
                var bv = new List<Vector3>(); var buv = new List<Vector2>(); var bt = new List<int>(); var gt = new List<int>();
                for (int i = cellStart; i <= cellEnd; i++)
                    for (int k = 0; k <= arc; k++)
                    {
                        float th = Mathf.PI * 2f * k / arc; var v = Ring(i, th);
                        if (riserTop(i) && v.y > Gnd(v) - 0.3f) v.y = Mathf.Min(v.y, Gnd(v) - 0.3f);   // nothing of the riser above the street; Round 25 (A1): 0.3 m under it (2 cm under, a small vehicle's body riding the street caught its steep edges: a JOLT; the floor over the hole carries the street)
                        bv.Add(v); buv.Add(new Vector2(g.S[i] / 2.4f, k / (float)arc));
                    }
                for (int i = cellStart; i < cellEnd; i++)
                {
                    bool glass = Glass((g.S[i] + g.S[i + 1]) * 0.5f);
                    for (int k = 0; k < arc; k++)
                    {
                        int a0 = (i - cellStart) * (arc + 1) + k, a1 = a0 + 1, b0 = a0 + arc + 1, b1 = b0 + 1;
                        if (riserTop(i + 1))   // a riser quad wholly at the street: the opening
                        {
                            int above = 0; foreach (int q in new[] { a0, a1, b0, b1 }) if (bv[q].y >= Gnd(bv[q]) - 0.33f) above++;
                            if (above >= 3) continue;
                        }
                        var l = glass ? gt : bt;
                        // inward: the face looks at the axis
                        var nrm = Vector3.Cross(bv[b0] - bv[a0], bv[a1] - bv[a0]); var ax = g.Axis(i) - bv[a0];
                        if (Vector3.Dot(nrm, ax) >= 0f) { l.Add(a0); l.Add(b0); l.Add(a1); l.Add(a1); l.Add(b0); l.Add(b1); }
                        else { l.Add(a0); l.Add(a1); l.Add(b0); l.Add(a1); l.Add(b1); l.Add(b0); }
                    }
                }
                var mesh = new Mesh { name = $"Net {t.id} {cellStart}", indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(bv); mesh.SetUVs(0, buv); bool hasGlass = gt.Count > 0;
                mesh.subMeshCount = hasGlass ? 2 : 1; mesh.SetTriangles(bt, 0); if (hasGlass) mesh.SetTriangles(gt, 1);
                mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
                mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/Net {t.id} {cellStart}.asset");
                var go = new GameObject("sc Net " + t.id + " barrel"); go.transform.SetParent(cell, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.shadowCastingMode = ShadowCastingMode.Off;
                if (hasGlass) { ClearMaterials(); mr.sharedMaterials = new[] { pipeConcrete, clearGlass }; } else mr.sharedMaterial = pipeConcrete;
                var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; mc.sharedMaterial = RampMaterial();
                // the mains' color stripes: a painted band each side, a little under the axis
                if (t.stripe != null && t.stripe.Length >= 3)
                {
                    var sm = new MeshBuilder();
                    foreach (float side in new[] { -1f, 1f })
                        for (int i = cellStart; i < cellEnd; i++)
                        {
                            if (Glass(g.S[i]) || riserTop(i)) continue;
                            Vector3 Pt(int j, float th) => g.Axis(j) + (g.Side(j) * Mathf.Sin(th) - g.Up(j) * Mathf.Cos(th)) * (R - 0.012f);
                            float th0 = side * 1.30f, th1 = side * 1.43f;
                            Face(sm, Pt(i, th0), Pt(i + 1, th0), Pt(i + 1, th1), Pt(i, th1), g.Axis(i) - Pt(i, (th0 + th1) * 0.5f));
                        }
                    // Round 24 Part B (item 6, Dan: "It is very easy to get turned around in the tunnels. Can we [make] the lines always point
                    // towards the hub"): chevrons on both stripes every 8 m, pointing the way to the hub along the mains (to_hub,
                    // Tools/mapgen/pipe_network.py): 0.8 m tall across the 0.33 m stripe, in its color, just off the wall
                    if (t.to_hub != 0)
                    {
                        float dir = t.to_hub, Lc = 0.8f, Wc = 0.3f, hh = 0.42f / R;
                        Vector3 Ps(float s, float th)
                        {
                            int j = Mathf.Clamp(g.At(s), 1, N - 1); float u = Mathf.InverseLerp(g.S[j - 1], g.S[j], s);
                            var ax = Vector3.Lerp(g.Axis(j - 1), g.Axis(j), u); var sd = Vector3.Lerp(g.Side(j - 1), g.Side(j), u).normalized; var upv = Vector3.Lerp(g.Up(j - 1), g.Up(j), u).normalized;
                            return ax + (sd * Mathf.Sin(th) - upv * Mathf.Cos(th)) * (R - 0.02f);
                        }
                        float first8 = Mathf.Ceil((g.S[cellStart] + 0.01f) / 8f) * 8f;
                        for (float sc = first8; sc < g.S[cellEnd]; sc += 8f)
                        {
                            if (sc < 4f || sc > g.S[N - 1] - 4f || Glass(sc - 1.5f) || Glass(sc + 1.5f) || riserTop(g.At(sc)) || (g.RiserFrom >= 0 && g.At(sc) > g.RiserFrom - 3)) continue;
                            foreach (float side in new[] { -1f, 1f })
                            {
                                float thc = side * 1.365f, sb = sc - dir * Lc * 0.5f, st = sc + dir * Lc * 0.5f;
                                var axm = g.Axis(g.At(sc));
                                // the two arms of the ">" (back corner, back corner moved on by the arm's thickness, the tip moved on, the tip)
                                Face(sm, Ps(sb, thc + hh), Ps(sb + dir * Wc, thc + hh), Ps(st + dir * Wc, thc), Ps(st, thc), axm - Ps(sc, thc));
                                Face(sm, Ps(st, thc), Ps(st + dir * Wc, thc), Ps(sb + dir * Wc, thc - hh), Ps(sb, thc - hh), axm - Ps(sc, thc));
                            }
                        }
                    }
                    if (sm.Count > 0) { var so = sm.ToObject($"Net {t.id} {cellStart} stripes", netStripe(new Color(t.stripe[0], t.stripe[1], t.stripe[2])), cell, false, false); GameObjectUtility.SetStaticEditorFlags(so, 0); }
                }
                // lamps along the crown (none over glass, none on a riser)
                var lampMb = new MeshBuilder();
                float first = Mathf.Ceil((g.S[cellStart] + 6f) / NetLampEvery) * NetLampEvery - 6f;
                for (float s = Mathf.Max(first, 6f); s < g.S[cellEnd] && s < g.S[N - 1] - 5f; s += NetLampEvery)
                {
                    int i = g.At(s); if (Glass(s) || riserTop(i) || (g.RiserFrom >= 0 && i > g.RiserFrom - 6)) continue;
                    var top = Ring(i, Mathf.PI); var c = top - g.Up(i) * 0.12f;
                    lampMb.Tint = Color.white; BoxAt(lampMb, c, g.T(i), 0.7f, 0.14f, 0.22f);
                    var lg = new GameObject("Pipe lamp"); lg.transform.SetParent(cell, false); lg.transform.position = c - g.Up(i) * 0.4f;
                    var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = R < 2f ? 7f : 9f; l.intensity = 1.1f; l.color = new Color(1f, 0.82f, 0.58f); l.shadows = LightShadows.None;
                    lamps++;
                }
                if (lampMb.Count > 0) { var lgo = lampMb.ToObject($"Net {t.id} {cellStart} lamps", pipeLamp, cell, false); lgo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; }
                // the trickle down the invert (flowing a to b: on toward the hub), not up a riser
                {
                    var sv = new List<Vector3>(); var suv = new List<Vector2>(); var st = new List<int>(); var line = new List<Vector3>(); int sn = 0;
                    for (int i = cellStart; i <= cellEnd; i++)
                    {
                        if (riserTop(i) || (g.RiserFrom >= 0 && i > g.RiserFrom)) break;
                        float s = g.S[i], half = (R < 2f ? 0.32f : 0.45f) + 0.07f * Mathf.Sin(s * 0.7f), wob = 0.06f * Mathf.Sin(s * 0.45f);
                        foreach (float tt in new[] { -1f, -0.5f, 0f, 0.5f, 1f })
                        {
                            float lat = wob + tt * half, th = Mathf.Asin(Mathf.Clamp(lat / R, -1f, 1f));
                            var v = g.Axis(i) + (g.Side(i) * Mathf.Sin(th) - g.Up(i) * Mathf.Cos(th)) * (R - 0.035f - 0.01f * (1f - Mathf.Abs(tt)));
                            sv.Add(v); suv.Add(new Vector2(tt * 0.5f + 0.5f, -s / 3f));
                        }
                        line.Add(g.P[i] + g.Up(i) * 0.05f);
                        if (sn > 0) for (int k = 0; k < 4; k++) { int a0 = (sn - 1) * 5 + k, b0 = sn * 5 + k; st.Add(a0); st.Add(b0); st.Add(a0 + 1); st.Add(a0 + 1); st.Add(b0); st.Add(b0 + 1); }
                        sn++;
                    }
                    if (st.Count > 0)
                    {
                        var n0 = Vector3.Cross(sv[st[1]] - sv[st[0]], sv[st[2]] - sv[st[0]]); if (n0.y < 0f) for (int i = 0; i < st.Count; i += 3) { int x = st[i + 1]; st[i + 1] = st[i + 2]; st[i + 2] = x; }
                        var smesh = new Mesh { name = $"Net {t.id} {cellStart} stream", indexFormat = IndexFormat.UInt32 };
                        smesh.SetVertices(sv); smesh.SetUVs(0, suv); smesh.SetTriangles(st, 0); smesh.RecalculateNormals(); smesh.RecalculateBounds();
                        smesh = GenAsset(smesh, $"{GeneratedDir}/Meshes/Net {t.id} {cellStart} stream.asset");
                        var sgo = new GameObject($"Net {t.id} {cellStart} stream"); sgo.transform.SetParent(cell, false);
                        sgo.AddComponent<MeshFilter>().sharedMesh = smesh; var smr = sgo.AddComponent<MeshRenderer>(); smr.sharedMaterial = pipeWater; smr.shadowCastingMode = ShadowCastingMode.Off;
                        var ps = sgo.AddComponent<PipeStream>(); ps.line = line.ToArray(); ps.water = pipeWater; ps.splash = pipeSplash;
                    }
                }
                // the run time: this stretch's floor, for resets, wall riding, the camera and headlights
                {
                    var host = new GameObject($"Net {t.id} {cellStart} runtime"); host.transform.SetParent(cell, false);
                    var dp = host.AddComponent<DrainPipe>(); dp.id = t.id + "@" + Mathf.RoundToInt(g.S[cellStart]); dp.halfWidth = R * 0.85f; dp.clearHeight = t.d; dp.radius = R;
                    // its own stretch only (two would both hold a vehicle to the wall where they overlapped)
                    int a = cellStart, b = cellEnd;
                    var fl = new List<Vector3>(); for (int i = a; i <= b; i++) fl.Add(g.P[i]);
                    dp.floor = fl.ToArray(); dp.barrelFrom = 0f; dp.barrelTo = fl.Count - 1;
                    // the hold to the wall fades out only at the pipe's real ends (DrainPipe.EndFade): its neighbors carry on past these ends
                    dp.openStart = cellStart != 0; dp.openEnd = cellEnd != N - 1;
                    if (t.stripe != null && t.stripe.Length >= 3) dp.mapColor = new Color(t.stripe[0], t.stripe[1], t.stripe[2]);
                }
                var bb = mesh.bounds; bb.Expand(2f);
                cells.Cells.Add((cell, bb));
                cellStart = cellEnd;
            }
            // a flooded sag: its water across the pipe (look), and ShallowWater over it (it slows you; it is never deep)
            if (t.flood != null && t.flood.s1 > t.flood.s0)
            {
                var wm = new MeshBuilder(); var ol = new List<Vector2>(); var or2 = new List<Vector2>();
                for (float s = t.flood.s0; s <= t.flood.s1; s += 1f)
                {
                    int i = g.At(s); float h = t.flood.level - g.P[i].y; if (h <= 0.02f) continue;
                    float w = Mathf.Sqrt(Mathf.Max(0f, R * R - (R - h) * (R - h)));
                    var c = new Vector3(g.P[i].x, t.flood.level, g.P[i].z); var sd = g.Side(i);
                    ol.Add(new Vector2((c + sd * w).x, (c + sd * w).z)); or2.Add(new Vector2((c - sd * w).x, (c - sd * w).z));
                }
                if (ol.Count >= 2)
                {
                    wm.Tint = Color.white;
                    for (int k = 0; k + 1 < ol.Count; k++)
                        Face(wm, new Vector3(ol[k].x, t.flood.level, ol[k].y), new Vector3(ol[k + 1].x, t.flood.level, ol[k + 1].y), new Vector3(or2[k + 1].x, t.flood.level, or2[k + 1].y), new Vector3(or2[k].x, t.flood.level, or2[k].y), Vector3.up);
                    var wgo = wm.ToObject("Net " + t.id + " flooded water", FloodWater(), root, false, false);
                    var ring = new List<Vector2>(ol); for (int k = or2.Count - 1; k >= 0; k--) ring.Add(or2[k]);
                    var fgo = new GameObject("Net " + t.id + " flooded (water volume)"); fgo.transform.SetParent(root, false);
                    var sw = fgo.AddComponent<ShallowWater>(); sw.outline = ring.ToArray(); sw.surfaceY = t.flood.level;
                    Debug.Log($"[Round23+] pipe {t.id}: a flooded sag {t.flood.s1 - t.flood.s0:0} m long, its water up to {t.flood.level - g.P[g.At((t.flood.s0 + t.flood.s1) * 0.5f)].y:0.00} m deep");
                }
            }
            return lamps;
        }

        static Material floodWater;
        static Material FloodWater()
        {
            if (floodWater) return floodWater;
            floodWater = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "PipeFloodWater" };
            floodWater.SetColor("_BaseColor", new Color(0.22f, 0.32f, 0.30f)); floodWater.SetFloat("_Smoothness", 0.95f);
            floodWater = GenAsset(floodWater, GeneratedDir + "/PipeFloodWater.mat");
            return floodWater;
        }

        // ---------------------------------------------------------------- a stub at a closed grille (a pipe not built yet)
        static void BuildStub(Transform parent, NetTubeGeo g, bool atStart, NetCells cells)
        {
            var t = g.J; float R = g.R; const float Len = 6f; int arc = t.d >= 5f ? 40 : 32;
            var root = cells.Make(parent, "Stub " + t.id + " (closed grille: not built yet)");
            // its first 6 m from the chamber's wall (the straight lead), as a short barrel
            var idx = new List<int>();
            if (atStart) { for (int i = 0; i < g.P.Length && g.S[i] <= Len + 0.01f; i++) idx.Add(i); }
            else { float L = g.S[g.S.Length - 1]; for (int i = g.P.Length - 1; i >= 0 && L - g.S[i] <= Len + 0.01f; i--) idx.Add(i); }
            var barrel = new UvMesh();
            Vector3 Ring(int i, float th) => g.RingAt(i, th);
            for (int q = 0; q + 1 < idx.Count; q++)
                for (int k = 0; k < arc; k++)
                {
                    float th0 = Mathf.PI * 2f * k / arc, th1 = Mathf.PI * 2f * (k + 1) / arc; int i = idx[q], j = idx[q + 1];
                    barrel.Quad(Ring(i, th0), Ring(j, th0), Ring(j, th1), Ring(i, th1), new Vector2(q / 2.4f, k / (float)arc), new Vector2((q + 1) / 2.4f, k / (float)arc), new Vector2((q + 1) / 2.4f, (k + 1) / (float)arc), new Vector2(q / 2.4f, (k + 1) / (float)arc), g.Axis(i) - Ring(i, (th0 + th1) * 0.5f));
                }
            int e = idx[idx.Count - 1]; var dirOut = (g.P[e] - g.P[idx[0]]).normalized; var ctr = g.Axis(e);
            for (int k = 0; k < arc; k++)   // the dark end behind the grille
            {
                float th0 = Mathf.PI * 2f * k / arc, th1 = Mathf.PI * 2f * (k + 1) / arc;
                barrel.Quad(ctr, Ring(e, th0), Ring(e, th1), ctr, new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), -dirOut);
            }
            barrel.ToObject("sc Stub " + t.id, pipeConcrete, root, true);
            // the grille 1.5 m before the end: upright bars, two crossbars, a hazard-striped bar; an amber lamp over it
            var steel = new Color(0.30f, 0.32f, 0.34f); var red = new Color(0.78f, 0.1f, 0.08f); var white = new Color(0.92f, 0.92f, 0.9f);
            var mb = new MeshBuilder(); var up = Vector3.up; var side = Vector3.Cross(up, new Vector3(dirOut.x, 0f, dirOut.z)).normalized;
            int gi = idx[Mathf.Max(0, idx.Count - 3)]; var gc = g.Axis(gi);
            for (float x = -R + 0.25f; x <= R - 0.25f + 1e-3f; x += 0.32f)
            {
                float half = Mathf.Sqrt(Mathf.Max(0f, R * R - x * x)) - 0.05f;
                Beam(mb, gc + side * x - up * half, gc + side * x + up * half, 0.07f, 0.07f, steel, dirOut);
            }
            foreach (float yy in new[] { -R * 0.36f, R * 0.56f }) { float half = Mathf.Sqrt(R * R - yy * yy) - 0.05f; Beam(mb, gc + up * yy - side * half, gc + up * yy + side * half, 0.12f, 0.12f, steel, up); }
            { float half = R - 0.1f; int segs = Mathf.CeilToInt(2f * half / 0.6f); for (int k = 0; k < segs; k++) { float x0 = -half + 2f * half * k / segs, x1 = -half + 2f * half * (k + 1) / segs; Beam(mb, gc - dirOut * 0.12f + up * 0.25f + side * x0, gc - dirOut * 0.12f + up * 0.25f + side * x1, 0.22f, 0.32f, (k & 1) == 0 ? red : white, up); } }
            var go = mb.ToObject("Grille " + t.id + " (closed)", netGrille, root, false); go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var col = new GameObject("Grille " + t.id + " collider"); col.transform.SetParent(root, false); col.transform.SetPositionAndRotation(gc, Quaternion.LookRotation(dirOut, up));
            col.AddComponent<BoxCollider>().size = new Vector3(2f * R, 2f * R, 0.3f);
            var lamp = new MeshBuilder(); var lc = gc - dirOut * 0.6f + up * (R - 0.25f); lamp.Tint = Color.white; BoxAt(lamp, lc, side, 0.6f, 0.18f, 0.3f);
            lamp.ToObject("Grille lamp " + t.id, pipeLamp, root, false, false);
            var lg = new GameObject("Grille light " + t.id); lg.transform.SetParent(root, false); lg.transform.position = lc - up * 0.4f - dirOut * 0.3f;
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 9f; l.intensity = 1.8f; l.color = new Color(1f, 0.55f, 0.12f); l.shadows = LightShadows.None;
            var host = new GameObject("Stub " + t.id + " runtime"); host.transform.SetParent(root, false);
            var dp = host.AddComponent<DrainPipe>(); dp.id = t.id + "-stub"; dp.halfWidth = R * 0.8f; dp.clearHeight = 2f * R; dp.radius = R;
            dp.floor = idx.Take(Mathf.Max(2, idx.Count - 2)).Select(i => g.P[i]).ToArray(); dp.barrelFrom = 0f; dp.barrelTo = dp.floor.Length - 1;
            var b = new Bounds(gc, Vector3.one * (2f * R + 8f)); cells.Cells.Add((root, b));
        }

        // ---------------------------------------------------------------- a chamber (a junction, a drop shaft, a turning room)
        static void BuildChamberRoom(TerrainGrid terrain, Materials mats, Transform parent, NetChamber c, Network n, NetCells cells)
        {
            var root = cells.Make(parent, "Chamber " + c.id + " (" + c.name + ")");
            var u3 = new Vector3(Mathf.Sin(c.yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(c.yaw * Mathf.Deg2Rad)); var v3 = new Vector3(u3.z, 0f, -u3.x); var up = Vector3.up;
            float hl = c.length * 0.5f, hw = c.width * 0.5f, h = c.height, y0 = c.floor;
            var C = new Vector3(c.x, y0, c.z);
            var room = new UvMesh();
            // floor and ceiling in 2 m tiles
            for (float a = -hl; a < hl - 1e-3f; a += 2f)
                for (float b = -hw; b < hw - 1e-3f; b += 2f)
                {
                    float a1 = Mathf.Min(a + 2f, hl), b1 = Mathf.Min(b + 2f, hw);
                    Vector3 P(float aa, float bb, float yy) => C + u3 * aa + v3 * bb + up * yy;
                    room.Quad(P(a, b, 0f), P(a1, b, 0f), P(a1, b1, 0f), P(a, b1, 0f), new Vector2(a / 2.4f, 0.02f), new Vector2(a1 / 2.4f, 0.02f), new Vector2(a1 / 2.4f, 0.04f), new Vector2(a / 2.4f, 0.04f), up);
                    // Round 44: no ceiling tile under the lake drain's shaft (LD: a tile any part of which is inside the hole; the shaft's own collar closes the rest)
                    if (c.roof_open > 0f && new Vector2(Mathf.Max(Mathf.Abs(a), Mathf.Abs(a1)) - 1f, Mathf.Max(Mathf.Abs(b), Mathf.Abs(b1)) - 1f).magnitude < c.roof_open) continue;
                    room.Quad(P(a, b, h), P(a1, b, h), P(a1, b1, h), P(a, b1, h), new Vector2(a / 2.4f, 0.5f), new Vector2(a1 / 2.4f, 0.5f), new Vector2(a1 / 2.4f, 0.52f), new Vector2(a / 2.4f, 0.52f), -up);
                }
            // the four walls with the round openings of their pipes (built or stubs: every opening is open)
            var holes = new List<float>[4]; var hy = new List<float>[4]; var hr = new List<float>[4];
            for (int w = 0; w < 4; w++) { holes[w] = new List<float>(); hy[w] = new List<float>(); hr[w] = new List<float>(); }
            foreach (var o in c.openings) { holes[o.wall].Add(o.t); hy[o.wall].Add(o.high ? c.drop : 0f); hr[o.wall].Add(o.d * 0.5f); }
            // wall 0 +u (from -v to +v), 1 +v (from -u to +u), 2 -u, 3 -v
            ChamberWall(room, C + u3 * hl - v3 * hw, v3, 2f * hw, -u3, h, holes[0], hy[0], hr[0], hw);
            ChamberWall(room, C + v3 * hw - u3 * hl, u3, 2f * hl, -v3, h, holes[1], hy[1], hr[1], hl);
            ChamberWall(room, C - u3 * hl - v3 * hw, v3, 2f * hw, u3, h, holes[2], hy[2], hr[2], hw);
            ChamberWall(room, C - v3 * hw - u3 * hl, u3, 2f * hl, v3, h, holes[3], hy[3], hr[3], hl);
            room.ToObject("sc Chamber " + c.id + " room", pipeConcrete, root, true);
            // lamps under the ceiling
            var lampMb = new MeshBuilder(); int nl = c.length * c.width > 200f ? 4 : 2;
            var at = nl == 4 ? new[] { (0.5f, 0.5f), (-0.5f, 0.5f), (0.5f, -0.5f), (-0.5f, -0.5f) } : new[] { (0f, 0.5f), (0f, -0.5f) };
            foreach (var (fa, fb) in at)
            {
                var lc = C + u3 * (hl * fa) + v3 * (hw * fb) + up * (h - 0.15f);
                lampMb.Tint = Color.white; BoxAt(lampMb, lc, u3, 1.2f, 0.2f, 0.35f);
                var lg = new GameObject("Chamber lamp"); lg.transform.SetParent(root, false); lg.transform.position = lc - up * 0.5f;
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = Mathf.Max(12f, h * 2.2f); l.intensity = 1.5f; l.color = new Color(1f, 0.82f, 0.58f); l.shadows = LightShadows.None;
            }
            var lgo = lampMb.ToObject("Chamber " + c.id + " lamps", pipeLamp, root, false); lgo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            // painted signs over the mains' openings (their name, in their stripe's color); the maze stays unmarked
            if (!string.IsNullOrEmpty(c.sign)) BuildChamberSigns(root, c, n, C, u3, v3, hl, hw);
            // a sump grate in the floor's middle
            var sump = new MeshBuilder(); sump.Tint = new Color(0.08f, 0.08f, 0.09f); var sc = C + up * 0.02f;
            Face(sump, sc + (u3 + v3) * 1.2f, sc + (u3 - v3) * 1.2f, sc + (-u3 - v3) * 1.2f, sc + (-u3 + v3) * 1.2f, up);
            for (float x = -1.05f; x <= 1.06f; x += 0.3f) { sump.Tint = new Color(0.30f, 0.32f, 0.34f); BoxAt(sump, sc + v3 * x + up * 0.03f, u3, 2.3f, 0.06f, 0.08f); }
            sump.ToObject("Chamber " + c.id + " sump", mats.House, root, false, false);
            // run time: resets on its floor (a line down its middle, the room's half width), headlights on
            var rh = new GameObject("Chamber " + c.id + " runtime"); rh.transform.SetParent(root, false);
            var rp = rh.AddComponent<DrainPipe>(); rp.id = c.id; rp.halfWidth = Mathf.Max(hw, hl) - 0.6f; rp.clearHeight = Mathf.Min(h, c.kind == "shaft" && c.drop > 0f ? c.drop - 0.5f : h); rp.radius = 0.1f; rp.barrelFrom = 0f; rp.barrelTo = 0f;   // Round 28: a junction with a high opening (J2) keeps its full height
            var along = hl >= hw ? u3 : v3; float half = Mathf.Max(hl, hw) - 0.5f;
            var rf = new List<Vector3>(); for (float s = -half; s <= half + 1e-3f; s += 0.5f) rf.Add(C + along * s); rp.floor = rf.ToArray();
            // the ground over it (report)
            float least = float.MaxValue;
            for (float a = -hl; a <= hl; a += 2f) for (float b = -hw; b <= hw; b += 2f) { var q = C + u3 * a + v3 * b; least = Mathf.Min(least, Ground(terrain, q.x, q.z) - (y0 + h + 0.3f)); }
            if (least < 0.4f) Debug.LogWarning($"[Round23+] chamber {c.id}: only {least:0.00} m of ground over its ceiling");
            cells.Cells.Add((root, new Bounds(C + up * (h * 0.5f), new Vector3(2f * Mathf.Max(hl, hw) + 4f, h + 4f, 2f * Mathf.Max(hl, hw) + 4f))));
        }

        // a chamber wall from 'o' along 'along' for 'width' m, facing 'inward', 'h' tall (from 0.6 m under the floor), with round
        // openings: centers 'ts' (along the wall from its middle), their bottoms 'ys' over the floor, radii 'rs'
        static void ChamberWall(UvMesh room, Vector3 o, Vector3 along, float width, Vector3 inward, float h, List<float> ts, List<float> ys, List<float> rs, float halfW)
        {
            var up = Vector3.up; float vScale = 1f / (2f * Mathf.PI * 2.5f);
            Vector3 P(float t, float v) => o + along * t + up * v;
            Vector2 Uv(float t, float v) => new Vector2(t / 2.4f, Mathf.Clamp(v, 0f, 2.5f) * vScale + (v > 2.5f ? 0.25f : 0f));
            void Rect(float t0, float t1, float v0, float v1) { if (t1 - t0 < 1e-3f || v1 - v0 < 1e-3f) return; room.Quad(P(t0, v0), P(t1, v0), P(t1, v1), P(t0, v1), Uv(t0, v0), Uv(t1, v0), Uv(t1, v1), Uv(t0, v1), inward); }
            var order = Enumerable.Range(0, ts.Count).OrderBy(i => ts[i]).ToList();
            float done = 0f;
            foreach (int i in order)
            {
                float hc = ts[i] + halfW, R = rs[i], yb = ys[i], a = R + 0.4f, cy = yb + R;
                Rect(done, hc - a, -0.6f, h);
                Rect(hc - a, hc + a, cy + a, h);
                Rect(hc - a, hc + a, -0.6f, cy - a);
                const int seg = 40;
                for (int k = 0; k < seg; k++)
                {
                    float th0 = Mathf.PI * 2f * k / seg, th1 = Mathf.PI * 2f * (k + 1) / seg;
                    Vector2 Cq(float th) { var q = NetRingPoint(R, th, 1f); return new Vector2(hc + q.x, yb + q.y); }   // the D (MapBuilder.Network.cs)
                    Vector2 Sq(float th) { var d = new Vector2(Mathf.Sin(th), -Mathf.Cos(th)); float kx = Mathf.Abs(d.x) > 1e-4f ? a / Mathf.Abs(d.x) : float.MaxValue, ky = Mathf.Abs(d.y) > 1e-4f ? a / Mathf.Abs(d.y) : float.MaxValue; float kk = Mathf.Min(kx, ky); return new Vector2(hc + d.x * kk, cy + d.y * kk); }
                    Vector2 c0 = Cq(th0), c1 = Cq(th1), q0 = Sq(th0), q1 = Sq(th1);
                    room.Quad(P(c0.x, c0.y), P(c1.x, c1.y), P(q1.x, q1.y), P(q0.x, q0.y), Uv(c0.x, c0.y), Uv(c1.x, c1.y), Uv(q1.x, q1.y), Uv(q0.x, q0.y), inward);
                }
                done = hc + a;
            }
            Rect(done, width, -0.6f, h);
        }

        // ---------------------------------------------------------------- manhole lids
        // Round 25 (A1, BUG-001, Dan in Chasefield Way over MH11: "can we make sure that manhole covers dont interfere with vehicles
        // causing them to bounce. Also is there anyway to make them look normal size on the street without changing anything from the
        // exits?"). The riser's opening in the street is about 5 m across (a 3.5 m pipe at 50 degrees); before, a 5 to 6 m iron lid
        // was drawn over it with a concrete collar and an asphalt patch round it (jagged edges and grass between them), and its collider
        // was a flat convex disc that stood off the sloping street. Now, from the street:
        //   the hatch   the street's own triangles cut out over the hole (asphalt and paint, their colors: CutNetworkHoles), so the
        //               opening is hidden flush in the road with no seam, and a normal manhole cover (0.9 m) drawn in its middle
        //   the floor   a collider that is the terrain's own surface over the hole's cells (and one cell round them), at the terrain's
        //               own heights: driving over it is driving over the ground the road lies on (roads have no colliders)
        // The way up is as before: the riser, the trigger just under the street, the hatch (all of it) flies off and comes back
        static void BuildManholeLid(TerrainGrid terrain, Materials mats, Transform parent, int lidIndex, NetManhole m, Vector2 c, float r, float y, (float half, float lift) road, NetCells cells)
        {
            var root = new GameObject("Manhole " + m.id + " (" + m.road + ")").transform; root.SetParent(parent, false);
            var home = new Vector3(c.x, y, c.y);
            // the hatch: the cut-out street triangles (one submesh per material), then the cover on top (the paint's material: drawn over
            // the road without fighting it)
            var byMat = new Dictionary<Material, (List<Vector3> v, List<Color> col, List<int> t)>();
            (List<Vector3> v, List<Color> col, List<int> t) Sub(Material mm) { if (!byMat.TryGetValue(mm, out var x)) byMat[mm] = x = (new List<Vector3>(), new List<Color>(), new List<int>()); return x; }
            if (Net.LidStreet.TryGetValue(lidIndex, out var street))
                foreach (var (mm, a, b, cc, ca, cb, ccol) in street)
                {
                    var x = Sub(mm); int k = x.v.Count;
                    x.v.Add(a - home); x.v.Add(b - home); x.v.Add(cc - home); x.col.Add(ca); x.col.Add(cb); x.col.Add(ccol);
                    x.t.Add(k); x.t.Add(k + 1); x.t.Add(k + 2);
                }
            else Debug.LogWarning($"[Round25] manhole {m.id}: no street triangles over its hole (no hatch drawn)");
            {
                var cover = Sub(mats.Paint); const int seg = 20; const float Rc = 0.45f;
                // Round 28 (A, BUG-001 on Westminister Way: "there is no manhole cover here"): the cover sat at the terrain + the road's
                // lift, but the street's own triangles over the hole stand 0 to 0.1 m higher on the communities' streets and on slopes, so
                // the street was drawn over it (MH4, MH7, MH10, MH11) or fought it (MH3, MH5, MH9). Now 12 mm over the highest street
                // triangle under each of its points (the terrain + lift only where there is no street)
                float Street(float qx, float qz)
                {
                    float best = float.NegativeInfinity;
                    if (street != null)
                        foreach (var (_, a, b, cc, _, _, _) in street)
                        {
                            float v0x = b.x - a.x, v0z = b.z - a.z, v1x = cc.x - a.x, v1z = cc.z - a.z, v2x = qx - a.x, v2z = qz - a.z;
                            float den = v0x * v1z - v1x * v0z; if (Mathf.Abs(den) < 1e-9f) continue;
                            float u = (v2x * v1z - v1x * v2z) / den, w = (v0x * v2z - v2x * v0z) / den;
                            if (u < -1e-4f || w < -1e-4f || u + w > 1f + 1e-4f) continue;
                            best = Mathf.Max(best, a.y + u * (b.y - a.y) + w * (cc.y - a.y));
                        }
                    return float.IsNegativeInfinity(best) ? Ground(terrain, qx, qz) + road.lift : best;
                }
                float Lift(float qx, float qz) => Street(qx, qz) + 0.012f - y;
                Vector3 Q(float a, float rr) { float qx = c.x + Mathf.Cos(a) * rr, qz = c.y + Mathf.Sin(a) * rr; return new Vector3(qx - c.x, Lift(qx, qz), qz - c.y); }
                void Ring(float r0, float r1, Color colr)
                {
                    for (int k = 0; k < seg; k++)
                    {
                        float a0 = Mathf.PI * 2f * k / seg, a1 = Mathf.PI * 2f * (k + 1) / seg; int b = cover.v.Count;
                        if (r0 <= 0f) { cover.v.Add(new Vector3(0f, Lift(c.x, c.y), 0f)); cover.v.Add(Q(a1, r1)); cover.v.Add(Q(a0, r1)); for (int j = 0; j < 3; j++) cover.col.Add(colr); cover.t.Add(b); cover.t.Add(b + 1); cover.t.Add(b + 2); continue; }
                        cover.v.Add(Q(a0, r0)); cover.v.Add(Q(a1, r0)); cover.v.Add(Q(a1, r1)); cover.v.Add(Q(a0, r1)); for (int j = 0; j < 4; j++) cover.col.Add(colr);
                        cover.t.Add(b); cover.t.Add(b + 2); cover.t.Add(b + 1); cover.t.Add(b); cover.t.Add(b + 3); cover.t.Add(b + 2);
                    }
                }
                // Round 25 (Dan, after driving it: "I really want the manhole covers visible above ground at a normal size not completely
                // gone"): the first cover was near-black on the dark asphalt and read as nothing. Now worn cast iron, clearly lighter than
                // the road, a bright polished rim, a dark ring of grout round it, cast rings and a cross-hatch, 0.9 m across
                var grout = new Color(0.10f, 0.10f, 0.10f); var iron = new Color(0.40f, 0.39f, 0.37f); var cast = new Color(0.55f, 0.54f, 0.51f); var rim = new Color(0.68f, 0.67f, 0.64f);
                Ring(Rc, Rc + 0.05f, grout);
                Ring(0f, Rc, iron); Ring(Rc - 0.06f, Rc, rim);                                 // the plate, its worn rim
                Ring(Rc * 0.30f, Rc * 0.40f, cast); Ring(Rc * 0.62f, Rc * 0.72f, cast);       // cast rings
                for (int k = -2; k <= 2; k++)   // the cross-hatch, inside the outer cast ring
                    foreach (bool across in new[] { false, true })
                    {
                        float o = k * Rc * 0.22f, half = Mathf.Sqrt(Mathf.Max(0f, (Rc * 0.6f) * (Rc * 0.6f) - o * o)), w = 0.018f; int b = cover.v.Count;
                        Vector3 P2(float u, float v) { float qx = c.x + (across ? v : u), qz = c.y + (across ? u : v); return new Vector3(qx - c.x, Lift(qx, qz) + 0.001f, qz - c.y); }
                        cover.v.Add(P2(-half, o - w)); cover.v.Add(P2(half, o - w)); cover.v.Add(P2(half, o + w)); cover.v.Add(P2(-half, o + w)); for (int j = 0; j < 4; j++) cover.col.Add(cast);
                        cover.t.Add(b); cover.t.Add(b + 2); cover.t.Add(b + 1); cover.t.Add(b); cover.t.Add(b + 3); cover.t.Add(b + 2);
                    }
                for (int i = 0; i + 2 < cover.t.Count; i += 3)   // every face up
                {
                    var n0 = Vector3.Cross(cover.v[cover.t[i + 1]] - cover.v[cover.t[i]], cover.v[cover.t[i + 2]] - cover.v[cover.t[i]]);
                    if (n0.y < 0f) { int sw = cover.t[i + 1]; cover.t[i + 1] = cover.t[i + 2]; cover.t[i + 2] = sw; }
                }
            }
            var mesh = new Mesh { name = "Manhole hatch " + m.id, indexFormat = IndexFormat.UInt32 };
            var allV = new List<Vector3>(); var allC = new List<Color>(); var mlist = new List<Material>(); var tris = new List<List<int>>();
            foreach (var kv in byMat) { int off = allV.Count; allV.AddRange(kv.Value.v); allC.AddRange(kv.Value.col); tris.Add(kv.Value.t.Select(q => q + off).ToList()); mlist.Add(kv.Key); }
            mesh.SetVertices(allV); mesh.SetColors(allC); mesh.subMeshCount = tris.Count; for (int i = 0; i < tris.Count; i++) mesh.SetTriangles(tris[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/Manhole hatch {m.id}.asset");
            var lidGo = new GameObject("Manhole " + m.id + " hatch (the street over it, a cover)"); lidGo.transform.SetParent(root, false); lidGo.transform.position = home;
            lidGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var lr = lidGo.AddComponent<MeshRenderer>(); lr.sharedMaterials = mlist.ToArray(); lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveGI = ReceiveGI.LightProbes;
            // the floor over the hole: the terrain's surface over its cells and a ring of cells round them, at the terrain's heights
            MeshCollider mc = null;
            if (Net.LidCells.TryGetValue(lidIndex, out var lc))
            {
                var keys = new HashSet<Vector2Int>();
                var tpos = terrain.At(c.x, c.y).transform.position; var org = new Vector2(tpos.x, tpos.z);
                foreach (var q in lc.cells)
                {
                    int ix = Mathf.FloorToInt((q.x - org.x) / lc.cx), iz = Mathf.FloorToInt((q.y - org.y) / lc.cz);
                    for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++) keys.Add(new Vector2Int(ix + dx, iz + dz));
                }
                var cv = new List<Vector3>(); var ct = new List<int>(); var vid = new Dictionary<Vector2Int, int>();
                int V(int ix, int iz)
                {
                    var key = new Vector2Int(ix, iz); if (vid.TryGetValue(key, out int id)) return id;
                    float x = org.x + ix * lc.cx, z = org.y + iz * lc.cz; cv.Add(new Vector3(x, Ground(terrain, x, z), z)); vid[key] = cv.Count - 1; return cv.Count - 1;
                }
                foreach (var k in keys)
                {
                    int a = V(k.x, k.y), b = V(k.x + 1, k.y), d = V(k.x + 1, k.y + 1), e = V(k.x, k.y + 1);
                    ct.AddRange(new[] { a, e, d, a, d, b });   // up-facing
                }
                var cm = new Mesh { name = "Manhole floor collider " + m.id, indexFormat = IndexFormat.UInt32 }; cm.SetVertices(cv); cm.SetTriangles(ct, 0); cm.RecalculateNormals(); cm.RecalculateBounds();
                cm = GenAsset(cm, $"{GeneratedDir}/Meshes/Manhole floor collider {m.id}.asset");
                var fgo = new GameObject("Manhole " + m.id + " floor over the hole (collider: the terrain's surface)"); fgo.transform.SetParent(root, false);
                mc = fgo.AddComponent<MeshCollider>(); mc.sharedMesh = cm;
                Debug.Log($"[Round25] manhole {m.id}: the hatch is the street's own {(street != null ? street.Count : 0)} triangles and a 0.9 m cover; the floor over its {lc.cells.Count} hole cells is the terrain's surface ({keys.Count} cells)");
            }
            else Debug.LogError($"[Round25] manhole {m.id}: no hole cells (no floor over it)");
            // the trigger in the top of the riser: a vehicle coming up hits it before the lid
            var trig = new GameObject("Manhole " + m.id + " burst trigger"); trig.transform.SetParent(root, false);
            var dir = new Vector3(m.dir[0], 0f, m.dir[1]);
            trig.transform.SetPositionAndRotation(new Vector3(c.x, y - 1.6f, c.y), Quaternion.LookRotation(dir));
            var bc = trig.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(2f * r, 2.4f, 2f * r);
            var ml = root.gameObject.AddComponent<ManholeLid>(); ml.lid = lidGo.transform; ml.lidCollider = mc; ml.outward = dir; ml.radius = r;
            ml.slope = new Vector2((Ground(terrain, c.x + 1.5f, c.y) - Ground(terrain, c.x - 1.5f, c.y)) / 3f, (Ground(terrain, c.x, c.y + 1.5f) - Ground(terrain, c.x, c.y - 1.5f)) / 3f);   // Round 25 (A1)
            trig.AddComponent<ManholeTrigger>().owner = ml;   // not a cell: the lid is seen from anywhere up the street
        }

        // Round 54 (E1, Dan BUG-009 on North Lakeside Drive: "something right here slows me down"): a manhole's floor over the hole is the
        // terrain's surface as it was when the network was built; a later step that grades the ground round it (the Chastain Lakes race's, for
        // MH15) left the floor's edge standing out of the road, and the vehicle's body met that edge head on (21 m/s to 0 in two steps). After
        // every step that changes the ground, each floor is put back on the ground as it now is.
        static void RefitManholeFloors(TerrainGrid terrain, Transform world)
        {
            int floors = 0; float worst = 0f; string worstName = "";
            foreach (var mc in world.GetComponentsInChildren<MeshCollider>(true))
            {
                if (!mc.name.StartsWith("Manhole ") || !mc.name.Contains(" floor over the hole") || !mc.sharedMesh) continue;
                var mesh = mc.sharedMesh; var v = mesh.vertices; var t = mc.transform; float moved = 0f;
                for (int i = 0; i < v.Length; i++)
                {
                    var w = t.TransformPoint(v[i]); float g = Ground(terrain, w.x, w.z);
                    moved = Mathf.Max(moved, Mathf.Abs(g - w.y)); w.y = g; v[i] = t.InverseTransformPoint(w);
                }
                mesh.vertices = v; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
                mc.sharedMesh = null; mc.sharedMesh = mesh; floors++;
                if (moved > worst) { worst = moved; worstName = mc.name; }
            }
            Debug.Log($"[Round54] {floors} manhole floors refitted to the finished ground; the most moved {worst:0.000} m ({worstName})");
        }

        // ---------------------------------------------------------------- glass under a pond (P1's parts: MapBuilder.ClearPipe.cs)
        // the stretch's frame (Round 25: also for its terrain holes, ClearFrame.ToSL: the nearest point of the pipe's line)
        static readonly HashSet<string> NetGlassAvoid = new HashSet<string> { "S2", "B28" };   // Round 57 (C): the glass stretches kept out of the other tubes (ClearFrame.Others); B28 (M2a) at Dan's yes
        static ClearFrame NetGlassFrame(NetTubeGeo g, NetGlass gl, float[] TS, float surface)
        {
            Vector3 At(float s) { int i = Mathf.Clamp(g.At(s), 1, g.P.Length - 1); float t = Mathf.InverseLerp(g.S[i - 1], g.S[i], s); return Vector3.Lerp(g.P[i - 1], g.P[i], t); }
            Vector3 Along(float s) { var d = At(s + 0.5f) - At(s - 0.5f); d.y = 0f; return d.normalized; }
            Vector3 Left(float s) { var u = Along(s); return new Vector3(-u.z, 0f, u.x); }
            float axis = float.MaxValue; for (float s = gl.s0; s <= gl.s1; s += 1f) axis = Mathf.Min(axis, At(s).y + g.R);
            int i0 = Mathf.Max(0, g.At(gl.s0) - 2), i1 = Mathf.Min(g.P.Length - 1, g.At(gl.s1) + 2);
            Vector2 ToSL(Vector2 q)
            {
                float best = float.MaxValue, at = 0f, lat = 0f;
                for (int i = i0; i < i1; i++)
                {
                    var a = new Vector2(g.P[i].x, g.P[i].z); var b = new Vector2(g.P[i + 1].x, g.P[i + 1].z); var ab = b - a;
                    float tt = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); var c = a + ab * tt; float d = (c - q).magnitude;
                    if (d < best) { best = d; at = g.S[i] + tt * ab.magnitude; var u = ab.normalized; lat = Vector2.Dot(q - c, new Vector2(-u.y, u.x)); }
                }
                return new Vector2(at, lat);
            }
            // Round 57 (C): the other tubes' axes within reach of the stretch (NetGlassAvoid's stretches only); Round 58 (C): every stretch (its weeds,
            // rocks and fish keep out of them; the bed only for NetGlassAvoid's: AvoidBed)
            List<(Vector3, Vector3, float)> others = null;
            if (Net != null)
            {
                others = new List<(Vector3, Vector3, float)>(); float reach = TS.Max() + 6f;
                var mid = new List<Vector2>(); for (float s = gl.s0; s <= gl.s1; s += 2f) { var q = At(s); mid.Add(new Vector2(q.x, q.z)); }
                foreach (var o in Net.Geo.Values)
                {
                    if (o == g || !Net.Built(o.J)) continue;
                    for (int i = 0; i + 1 < o.P.Length; i++)
                    {
                        var m2 = new Vector2(o.P[i].x, o.P[i].z);
                        if (mid.Any(c => (c - m2).sqrMagnitude < reach * reach)) others.Add((o.Axis(i), o.Axis(i + 1), o.R));
                    }
                }
            }
            return new ClearFrame { Id = g.J.id + " W" + gl.pond, R = g.R, C0 = gl.s0, C1 = gl.s1, Surface = surface, T = TS.Min(), AxisY = axis,
                                    W = (s, lat, y) => { var p = At(s) + Left(s) * lat; return new Vector3(p.x, y, p.z); }, U3 = Along, N3 = Left, AY = s => At(s).y + g.R,
                                    TS = s => TS[Mathf.Clamp(Mathf.RoundToInt(s - gl.s0), 0, TS.Length - 1)], ToSL = ToSL,
                                    Water = gl.pond >= 0 && gl.pond < Beds.Rings.Count ? Beds.Rings[gl.pond] : null, Others = others,
                                    AvoidBed = NetGlassAvoid.Contains(g.J.id) };
        }
        static void BuildNetworkGlass(TerrainGrid terrain, Materials mats, Transform parent, NetTubeGeo g, NetGlass gl, float T, float[] TS, NetCells cells)
        {
            var root = cells.Make(parent, "Glass " + g.J.id + " under pond W" + gl.pond);
            float surface = Beds.Level.TryGetValue(gl.pond, out float lv) ? lv + PondLift : gl.surface;
            Vector3 At(float s) { int i = Mathf.Clamp(g.At(s), 1, g.P.Length - 1); float t = Mathf.InverseLerp(g.S[i - 1], g.S[i], s); return Vector3.Lerp(g.P[i - 1], g.P[i], t); }
            Vector3 Along(float s) { var d = At(s + 0.5f) - At(s - 0.5f); d.y = 0f; return d.normalized; }
            Vector3 Left(float s) { var u = Along(s); return new Vector3(-u.z, 0f, u.x); }
            float axis = float.MaxValue, axisTop = float.MinValue; for (float s = gl.s0; s <= gl.s1; s += 1f) { axis = Mathf.Min(axis, At(s).y + g.R); axisTop = Mathf.Max(axisTop, At(s).y + g.R); }   // its lowest (the trench's depth) and highest (the crown under the water)
            if (axisTop + g.R + GlassOut > surface - ClearCrownUnder) { Debug.LogError($"[Round23+] pipe {g.J.id}: the glass under W{gl.pond} is not under the water (crown {axis + g.R:0.00} m, surface {surface:0.00} m): not built"); return; }
            var f = NetGlassFrame(g, gl, TS, surface);
            BuildClearStretch(terrain, mats, root, f);
            var a = At(gl.s0); var b = At(gl.s1);
            cells.Cells.Add((root, new Bounds((a + b) * 0.5f, new Vector3(Mathf.Abs(a.x - b.x) + 2f * TS.Max() + 10f, 20f, Mathf.Abs(a.z - b.z) + 2f * TS.Max() + 10f))));
        }
    }
}
