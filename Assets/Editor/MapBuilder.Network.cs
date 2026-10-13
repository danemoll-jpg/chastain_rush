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
        [Serializable] class NetTube { public string id, name; public float d; public bool main; public int stage; public float[] stripe; public NetEnd a, b; public float[] pts; public NetGlass[] glass; public NetFlood flood; }
        [Serializable] class NetOpening { public int wall; public float t, d, inset; public string tube, lvl; public bool high; }
        [Serializable] class NetChamber { public string id, name, kind, sign; public float x, z, yaw, length, width, height, floor, drop, mid, top, inset_mid, inset_floor, ceiling; public int stage; public NetOpening[] openings; }
        [Serializable] class NetExit { public string id, name, tube; public float[] a, b; public float b_floor, d, ground_a; public int outfall, stage; }
        [Serializable] class NetManhole { public string id, name, tube, road; public float x, z, y, d, height; public float[] dir; public int stage, riser_points; }
        [Serializable] class NetFile { public int build_stage; public NetTube[] tubes; public NetChamber[] chambers; public NetExit[] exits; public NetManhole[] manholes; }

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
                AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/Meshes/{name}.asset");
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
            public readonly List<(NetTubeGeo g, NetGlass gl, float T)> Glass = new List<(NetTubeGeo, NetGlass, float)>();
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
                    float T = 8f;   // the trench's half width at the top: never out past the pond's shore (1 m inside it)
                    for (float s = gl.s0; s <= gl.s1; s += 1f) { int i = g.At(s); T = Mathf.Min(T, DistToEdges(new Vector2(g.P[i].x, g.P[i].z), ring) - 1f); }
                    T = Mathf.Max(T, g.R + 2.2f);
                    int holed = SetHoles(terrain, BoundsOf(Enumerable.Range(g.At(gl.s0), g.At(gl.s1) - g.At(gl.s0) + 1).Select(i => new Vector2(g.P[i].x, g.P[i].z)), T + 1f), q =>
                    {
                        float best = float.MaxValue, at = 0f;
                        for (int i = g.At(gl.s0); i < g.At(gl.s1); i++) { var a = new Vector2(g.P[i].x, g.P[i].z); var b = new Vector2(g.P[i + 1].x, g.P[i + 1].z); var ab = b - a; float tt = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); float d = (a + ab * tt - q).magnitude; if (d < best) { best = d; at = g.S[i] + tt * ab.magnitude; } }
                        return best <= T && at > gl.s0 && at < gl.s1;
                    });
                    n.Glass.Add((g, gl, T));
                    Debug.Log($"[Round23+] pipe {t.id}: glass under pond W{gl.pond} from {gl.s0:0} to {gl.s1:0} m ({gl.s1 - gl.s0:0} m), the trench {2f * T:0.0} m wide, {holed} terrain hole cells");
                }
            }
        }

        // holes in the terrain where 'hole' is true for a cell's center inside 'area'; the count
        static int SetHoles(TerrainGrid g, Rect area, Func<Vector2, bool> hole)
        {
            int cells = 0;
            foreach (var tile in g.Tiles)
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

        // Chunks.ToObjects: no street, paint or sidewalk over a manhole's hole
        static void CutNetworkHoles(MeshBuilder mb)
        {
            if (Net == null || Net.Holes.Count == 0 || mb.Count == 0) return;
            var v = mb.Verts; var t = mb.Tris; var keep = new List<int>(t.Count);
            for (int i = 0; i + 2 < t.Count; i += 3)
            {
                var c = (v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f; var q = new Vector2(c.x, c.z); bool cut = false;
                foreach (var (hc, hr) in Net.Holes) if ((q - hc).sqrMagnitude < hr * hr) { cut = true; break; }
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
            AssetDatabase.CreateAsset(m, path); return m;
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
            foreach (var (m, c, r, y) in n.Lids) BuildManholeLid(terrain, mats, root, m, c, r, y, n.LidRoad.TryGetValue(m.id, out var lr) ? lr : (3.5f, 0.06f), cells);
            foreach (var (g, gl, T) in n.Glass) BuildNetworkGlass(terrain, mats, root, g, gl, T, cells);
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
                        if (riserTop(i) && v.y > Gnd(v) - 0.02f) v.y = Mathf.Min(v.y, Gnd(v) - 0.02f);   // nothing of the riser above the street
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
                            int above = 0; foreach (int q in new[] { a0, a1, b0, b1 }) if (bv[q].y >= Gnd(bv[q]) - 0.05f) above++;
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
                AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/Meshes/Net {t.id} {cellStart}.asset");
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
                        AssetDatabase.CreateAsset(smesh, $"{GeneratedDir}/Meshes/Net {t.id} {cellStart} stream.asset");
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
            AssetDatabase.CreateAsset(floodWater, GeneratedDir + "/PipeFloodWater.mat");
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
            var rp = rh.AddComponent<DrainPipe>(); rp.id = c.id; rp.halfWidth = Mathf.Max(hw, hl) - 0.6f; rp.clearHeight = Mathf.Min(h, c.drop > 0f ? c.drop - 0.5f : h); rp.radius = 0.1f; rp.barrelFrom = 0f; rp.barrelTo = 0f;
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
        static void BuildManholeLid(TerrainGrid terrain, Materials mats, Transform parent, NetManhole m, Vector2 c, float r, float y, (float half, float lift) road, NetCells cells)
        {
            var root = new GameObject("Manhole " + m.id + " (" + m.road + ")").transform; root.SetParent(parent, false);
            // the street's surface round the lid: the street's own triangles over the hole were cut away (CutNetworkHoles) in whole
            // pieces, so a patch of asphalt in the street's width fills round the collar (draped on the ground, just over the street)
            {
                var patch = new MeshBuilder(); var d = new Vector2(m.dir[0], m.dir[1]).normalized; var sd = new Vector2(-d.y, d.x);
                float hl = r + 3.2f, hw = Mathf.Max(1.5f, road.half - 0.25f);
                for (float a = -hl; a < hl - 1e-3f; a += 0.5f)
                    for (float b = -hw; b < hw - 1e-3f; b += 0.5f)
                    {
                        var q0 = c + d * a + sd * b; var q1 = c + d * (a + 0.5f) + sd * b; var q2 = c + d * (a + 0.5f) + sd * (b + 0.5f); var q3 = c + d * a + sd * (b + 0.5f);
                        if (((q0 + q2) * 0.5f - c).magnitude < r) continue;
                        Vector3 V(Vector2 q) => new Vector3(q.x, Ground(terrain, q.x, q.y) + road.lift + 0.008f, q.y);
                        Face(patch, V(q0), V(q1), V(q2), V(q3), Vector3.up);
                    }
                patch.ToObject("Manhole " + m.id + " street patch", mats.Asphalt, root, false, false);
            }
            // the collar round the hole (concrete, flush with the street): hides the hole's edge when the lid is off
            var collar = new MeshBuilder(); const int seg = 32;
            for (int k = 0; k < seg; k++)
            {
                float a0 = Mathf.PI * 2f * k / seg, a1 = Mathf.PI * 2f * (k + 1) / seg;
                Vector3 Q(float a, float rr) => new Vector3(c.x + Mathf.Cos(a) * rr, y + 0.02f, c.y + Mathf.Sin(a) * rr);
                collar.Tint = new Color(0.50f, 0.49f, 0.46f); Face(collar, Q(a0, r - 0.15f), Q(a1, r - 0.15f), Q(a1, r + 0.4f), Q(a0, r + 0.4f), Vector3.up);
            }
            collar.ToObject("Manhole " + m.id + " collar", mats.House, root, false, false);
            // the lid: a big round iron plate, cast pattern on top; solid (a convex collider), flush with the street
            var lid = new MeshBuilder(); var iron = new Color(0.20f, 0.20f, 0.21f);
            float th = 0.12f;
            for (int k = 0; k < seg; k++)
            {
                float a0 = Mathf.PI * 2f * k / seg, a1 = Mathf.PI * 2f * (k + 1) / seg;
                Vector3 Q(float a, float rr, float yy) => new Vector3(Mathf.Cos(a) * rr, yy, Mathf.Sin(a) * rr);
                lid.Tint = iron; TriFace(lid, Vector3.up * 0.03f, Q(a1, r, 0.03f), Q(a0, r, 0.03f), Vector3.up);
                lid.Tint = Shade(iron, 0.8f); Face(lid, Q(a0, r, 0.03f), Q(a1, r, 0.03f), Q(a1, r, -th), Q(a0, r, -th), Q((a0 + a1) * 0.5f, 1f, 0f));
                TriFace(lid, Vector3.up * -th, Q(a0, r, -th), Q(a1, r, -th), Vector3.down);
            }
            lid.Tint = Shade(iron, 1.35f);   // the cast rings and a cross on top
            foreach (float rr in new[] { r * 0.35f, r * 0.7f })
                for (int k = 0; k < seg; k++)
                {
                    float a0 = Mathf.PI * 2f * k / seg, a1 = Mathf.PI * 2f * (k + 1) / seg;
                    Face(lid, new Vector3(Mathf.Cos(a0) * rr, 0.035f, Mathf.Sin(a0) * rr), new Vector3(Mathf.Cos(a1) * rr, 0.035f, Mathf.Sin(a1) * rr), new Vector3(Mathf.Cos(a1) * (rr + 0.08f), 0.035f, Mathf.Sin(a1) * (rr + 0.08f)), new Vector3(Mathf.Cos(a0) * (rr + 0.08f), 0.035f, Mathf.Sin(a0) * (rr + 0.08f)), Vector3.up);
                }
            BoxAt(lid, Vector3.up * 0.04f, Vector3.forward, 2f * r * 0.9f, 0.01f, 0.1f); BoxAt(lid, Vector3.up * 0.04f, Vector3.right, 2f * r * 0.9f, 0.01f, 0.1f);
            var lidGo = lid.ToObject("Manhole " + m.id + " lid", mats.House, root, false, true);
            GameObjectUtility.SetStaticEditorFlags(lidGo, 0);
            lidGo.transform.position = new Vector3(c.x, y, c.y);
            var disc = new Mesh { name = "Manhole lid collider " + m.id }; var dv = new List<Vector3>(); var dt = new List<int>();
            for (int k = 0; k < 16; k++) { float a = Mathf.PI * 2f * k / 16; dv.Add(new Vector3(Mathf.Cos(a) * r, 0.03f, Mathf.Sin(a) * r)); dv.Add(new Vector3(Mathf.Cos(a) * r, -0.25f, Mathf.Sin(a) * r)); }
            for (int k = 0; k < 16; k++) { int a = 2 * k, b = 2 * ((k + 1) % 16); dt.AddRange(new[] { a, b, b + 1, a, b + 1, a + 1 }); }
            disc.SetVertices(dv); disc.SetTriangles(dt, 0); disc.RecalculateBounds(); AssetDatabase.CreateAsset(disc, $"{GeneratedDir}/Meshes/Manhole lid collider {m.id}.asset");
            var mc = lidGo.AddComponent<MeshCollider>(); mc.sharedMesh = disc; mc.convex = true;
            // the trigger in the top of the riser: a vehicle coming up hits it before the lid
            var trig = new GameObject("Manhole " + m.id + " burst trigger"); trig.transform.SetParent(root, false);
            var dir = new Vector3(m.dir[0], 0f, m.dir[1]);
            trig.transform.SetPositionAndRotation(new Vector3(c.x, y - 1.6f, c.y), Quaternion.LookRotation(dir));
            var bc = trig.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(2f * r, 2.4f, 2f * r);
            var ml = root.gameObject.AddComponent<ManholeLid>(); ml.lid = lidGo.transform; ml.lidCollider = mc; ml.outward = dir; ml.radius = r;
            trig.AddComponent<ManholeTrigger>().owner = ml;   // not a cell: the lid is seen from anywhere up the street
        }

        // ---------------------------------------------------------------- glass under a pond (P1's parts: MapBuilder.ClearPipe.cs)
        static void BuildNetworkGlass(TerrainGrid terrain, Materials mats, Transform parent, NetTubeGeo g, NetGlass gl, float T, NetCells cells)
        {
            var root = cells.Make(parent, "Glass " + g.J.id + " under pond W" + gl.pond);
            float surface = Beds.Level.TryGetValue(gl.pond, out float lv) ? lv + PondLift : gl.surface;
            Vector3 At(float s) { int i = Mathf.Clamp(g.At(s), 1, g.P.Length - 1); float t = Mathf.InverseLerp(g.S[i - 1], g.S[i], s); return Vector3.Lerp(g.P[i - 1], g.P[i], t); }
            Vector3 Along(float s) { var d = At(s + 0.5f) - At(s - 0.5f); d.y = 0f; return d.normalized; }
            Vector3 Left(float s) { var u = Along(s); return new Vector3(-u.z, 0f, u.x); }
            float axis = 0f; int k = 0; for (float s = gl.s0; s <= gl.s1; s += 1f) { axis += At(s).y + g.R; k++; } axis /= Mathf.Max(1, k);
            if (axis + g.R + GlassOut > surface - ClearCrownUnder) { Debug.LogError($"[Round23+] pipe {g.J.id}: the glass under W{gl.pond} is not under the water (crown {axis + g.R:0.00} m, surface {surface:0.00} m): not built"); return; }
            var f = new ClearFrame { Id = g.J.id + " W" + gl.pond, R = g.R, C0 = gl.s0, C1 = gl.s1, Surface = surface, T = T, AxisY = axis,
                                     W = (s, lat, y) => { var p = At(s) + Left(s) * lat; return new Vector3(p.x, y, p.z); }, U3 = Along, N3 = Left };
            BuildClearStretch(terrain, mats, root, f);
            var a = At(gl.s0); var b = At(gl.s1);
            cells.Cells.Add((root, new Bounds((a + b) * 0.5f, new Vector3(Mathf.Abs(a.x - b.x) + 2f * T + 10f, 20f, Mathf.Abs(a.z - b.z) + 2f * T + 10f))));
        }
    }
}
