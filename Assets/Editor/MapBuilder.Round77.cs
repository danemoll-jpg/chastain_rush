using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Round 77 (A, Dan's BUG-001 x 2292.44 z -5006.93 "fix", BUG-002 x 1694.78 z -5783.19 "fix this too", BUG-003 x 1618.42 z -6110.74 "need to fix here
// as well"; session 2026-10-27_21-09-31-163_2b90af): the ground round the Canton Road Connector and its ramps as proper graded embankments.
// What was wrong: at BUG-001 the Northwest Corridor Express Lanes' long deck runs at the ground under the connector, and JoinChainedDecks cut a trench
// for it through the connector's embankment: sheer, jagged walls 5 m high (the black spikes); at BUG-002 / BUG-003 the lidar keeps a mound in the
// median between the connector's two carriageways and steep raw banks beside them (and the road dips where it came off them).
// Here (in the planning, after the bridge ends are graded): each of the connector's and its ramps' carriageways gets a smooth profile along it (its
// ground smoothed over about R77Smooth m, held at every deck end, junction and chain end), and the ground within R77Reach m is rebuilt on a 0.5 m grid:
//   the carriageways (half width + 1 m) at their profile; every other road, drive, building (+2 m) and water bed held where it is; a deck's footprint
//   held where the deck stands clear of the ground, a clean floor under it where it runs at or below the ground;
//   the rest relaxed as a membrane between those and the ground at the reach (a first attempt with 1-in-2 cones left faceted stripes and kept the
//   jagged trench edges): embankments and cuttings run smoothly off the road's edge, a median spans flat, no spike, wall or mound can stay.
// The face count (cells steeper than 45 degrees within the reach) before / after, and Dan's three spots: build log and Assets/Generated/round77_embankments.txt.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const float R77Reach = 45f, R77Step = 1f, R77Smooth = 15f, R77Win = 384f;
        static readonly string[] R77Corridors = { "Canton Road Connector" };
        // Round 77 (A, "scan the whole of stage 3's new roads ... and fix what it finds"): the stage 3 piece's roads the face scan found faces beside
        // (python Tools/mapgen/road_faces.py, over 3 m in 2 m: local/round77/scan/stage3_faces.txt), their pieces inside the piece only; the main roads
        // with the connector's reach, the residential streets with R77StreetReach (a house lot stays its own shape past it)
        static readonly string[] R77PieceMains = { "South Marietta Parkway Southwest", "South Marietta Parkway Southeast", "Atlanta Street Southeast", "Powder Springs Street Southwest", "West Atlanta Street Southeast" };
        static readonly string[] R77PieceStreets = { "Griggs Street Southwest", "Marietta Walk Court Southwest", "Shy Lane Northeast", "Frasier Street Southeast", "Old Clay Street", "South Avenue Southeast",
            "Lawrence Street Northeast", "Pomeroy Street", "Henry Drive Southwest", "Manget Street Southeast", "Lakewood Drive Southeast" };
        const float R77StreetReach = 20f;
        static readonly (string name, Vector2 at)[] R77Spots = { ("BUG-001", new Vector2(2292.44f, -5006.93f)), ("BUG-002", new Vector2(1694.78f, -5783.19f)), ("BUG-003", new Vector2(1618.42f, -6110.74f)) };

        static void R77Embankments(MapData map, TerrainGrid terrain)
        {
            var log = new List<string> { "Round 77 (A): the ground round the Canton Road Connector and its ramps graded as embankments (MapBuilder.Round77.cs)" };
            // ---- the corridor: the connector's pieces on the ground, and the ramps (links) that run off them, three links deep
            var corr = new HashSet<int>(); var reachOf = new Dictionary<int, float>();
            bool InPiece(float[] q) { var pc = map.pieces; if (pc == null || pc.Length < 4) return false; for (int k = 0; k + 1 < q.Length; k += 2) if (q[k] >= pc[0] && q[k] <= pc[2] && q[k + 1] >= pc[1] && q[k + 1] <= pc[3]) return true; return false; }
            for (int i = 0; i < map.roads.Length; i++)
            {
                var r = map.roads[i]; if (r.bridge || r.tunnel || r.points == null || r.points.Length < 4) continue;
                if (R77Corridors.Contains(r.name) || (R77PieceMains.Contains(r.name) && InPiece(r.points))) { corr.Add(i); reachOf[i] = R77Reach; }
                else if (R77PieceStreets.Contains(r.name) && InPiece(r.points)) { corr.Add(i); reachOf[i] = R77StreetReach; }
            }
            var ends = new List<Vector2>();
            void EndsOf(float[] p) { int n = p.Length / 2; ends.Add(new Vector2(p[0], p[1])); ends.Add(new Vector2(p[2 * n - 2], p[2 * n - 1])); }
            foreach (int i in corr) EndsOf(map.roads[i].points);
            foreach (var b in map.bridges) if (R77Corridors.Contains(b.name) && b.points != null && b.points.Length >= 4) EndsOf(b.points);
            for (int hop = 0; hop < 3; hop++)
            {
                var add = new List<int>();
                for (int i = 0; i < map.roads.Length; i++)
                {
                    var r = map.roads[i]; if (corr.Contains(i) || r.bridge || r.tunnel || r.points == null || r.points.Length < 4 || r.@class == null || !r.@class.EndsWith("_link")) continue;
                    int n = r.points.Length / 2; var a = new Vector2(r.points[0], r.points[1]); var z = new Vector2(r.points[2 * n - 2], r.points[2 * n - 1]);
                    if (ends.Any(e => (e - a).sqrMagnitude < 16f || (e - z).sqrMagnitude < 16f)) add.Add(i);
                }
                foreach (int i in add) { corr.Add(i); reachOf[i] = R77Reach; EndsOf(map.roads[i].points); }
                foreach (var b in map.bridges) if (b.@class != null && b.@class.EndsWith("_link") && b.points != null && b.points.Length >= 4)
                    { int n = b.points.Length / 2; var a = new Vector2(b.points[0], b.points[1]); var z = new Vector2(b.points[2 * n - 2], b.points[2 * n - 1]); if (ends.Any(e => (e - a).sqrMagnitude < 16f || (e - z).sqrMagnitude < 16f)) EndsOf(b.points); }
            }
            if (corr.Count == 0) { Debug.LogWarning("[Round77] A: no Canton Road Connector on this map"); return; }
            // ---- chains: corridor pieces joined where exactly two of them meet
            Vector2Int Key(Vector2 p) => new Vector2Int(Mathf.RoundToInt(p.x * 2f), Mathf.RoundToInt(p.y * 2f));
            var at = new Dictionary<Vector2Int, List<(int piece, bool start)>>();
            foreach (int i in corr) { var q = ToPoints(map.roads[i].points); foreach (var (p, s) in new[] { (q[0], true), (q[q.Count - 1], false) }) { var k = Key(p); if (!at.TryGetValue(k, out var l)) at[k] = l = new List<(int, bool)>(); l.Add((i, s)); } }
            var used = new HashSet<int>(); var chains = new List<(List<Vector2> pts, float half, float reach)>();
            foreach (int i0 in corr.OrderBy(i => i))
            {
                if (used.Contains(i0)) continue; used.Add(i0);
                var line = ToPoints(map.roads[i0].points); float half = map.roads[i0].width * .5f; float reach = reachOf[i0];
                for (int dir = 0; dir < 2; dir++)
                {
                    for (int guard = 0; guard < 500; guard++)
                    {
                        if (!at.TryGetValue(Key(line[line.Count - 1]), out var l) || l.Count != 2) break;
                        int k = l.FindIndex(e => !used.Contains(e.piece)); if (k < 0) break;
                        var o = l[k]; used.Add(o.piece); var q = ToPoints(map.roads[o.piece].points); if (!o.start) q.Reverse();
                        line.AddRange(q.Skip(1)); half = Mathf.Max(half, map.roads[o.piece].width * .5f); reach = Mathf.Max(reach, reachOf[o.piece]);
                    }
                    line.Reverse();
                }
                chains.Add((line, half, reach));
            }
            // ---- each chain's profile: its ground smoothed along (a first-difference smoother of length R77Smooth), held at its ends, at every deck end
            // and where another road or another chain meets it
            var deckEnds = new List<Vector2>(); foreach (var b in map.bridges) { var q = ToPoints(b.points); if (q.Count > 1) { deckEnds.Add(q[0]); deckEnds.Add(q[q.Count - 1]); } }
            var roadEnds = new List<Vector2>(); foreach (var r in map.roads) { if (r.points == null || r.points.Length < 4) continue; int n = r.points.Length / 2; roadEnds.Add(new Vector2(r.points[0], r.points[1])); roadEnds.Add(new Vector2(r.points[2 * n - 2], r.points[2 * n - 1])); }
            var pinIdx = new CellIndex(16f); var pins = deckEnds.Concat(roadEnds).ToList(); foreach (var p in pins) pinIdx.Add(p);
            var segs = new List<(Vector2 a, Vector2 b, float ya, float yb, float half, int chain, float reach)>();
            float worstDip = 0f; int nPins = 0;
            var pinNear = new List<int>();
            for (int c = 0; c < chains.Count; c++)
            {
                var p = Resample(chains[c].pts, 1f); int n = p.Count; if (n < 2) continue;
                var raw = new float[n]; for (int i = 0; i < n; i++) raw[i] = terrain.Height(p[i].x, p[i].y);
                var w = new float[n];
                for (int i = 0; i < n; i++)
                {
                    bool pin = i == 0 || i == n - 1;
                    if (!pin) foreach (int k in pinIdx.Near(p[i], 2.5f, pinNear)) if ((pins[k] - p[i]).sqrMagnitude < 6.25f) { pin = true; break; }
                    w[i] = pin ? 1e6f : 1f; if (pin) nPins++;
                }
                var t = R77Smooth1D(raw, w, R77Smooth * R77Smooth);
                for (int i = 0; i < n; i++) worstDip = Mathf.Max(worstDip, Mathf.Abs(t[i] - raw[i]));
                for (int i = 0; i + 1 < n; i++) segs.Add((p[i], p[i + 1], t[i], t[i + 1], chains[c].half, c, chains[c].reach));
            }
            var segIdx = new CellIndex(16f); foreach (var s in segs) segIdx.Add(s.a, s.b, s.half + R77Reach + 1f);
            // ---- what is held: other roads and drives on the ground, buildings (+2 m), water; a deck's footprint held where the deck stands clear of the
            // ground (something passes under it), cut to a clean floor 0.1 m under it where it runs at or below the ground (JoinChainedDecks' trench)
            var held = new List<(Vector2 a, Vector2 b, float half)>();
            for (int i = 0; i < map.roads.Length; i++) { if (corr.Contains(i)) continue; var r = map.roads[i]; if (r.tunnel || r.points == null) continue; var q = ToPoints(r.points); for (int k = 0; k + 1 < q.Count; k++) held.Add((q[k], q[k + 1], r.width * .5f + .5f)); }
            if (map.driveways != null) foreach (var d in map.driveways) { if (d.points == null) continue; var q = ToPoints(d.points); for (int k = 0; k + 1 < q.Count; k++) held.Add((q[k], q[k + 1], Mathf.Max(d.width, 3f) * .5f + .3f)); }
            var heldIdx = new CellIndex(16f); foreach (var h in held) heldIdx.Add(h.a, h.b, h.half);
            var decks = new List<(Vector2 a, Vector2 b, float ya, float yb, float half)>();
            foreach (var b in map.bridges) { var q = ToPoints(b.points); if (b.deck_y == null || b.deck_y.Length != q.Count) continue; for (int k = 0; k + 1 < q.Count; k++) decks.Add((q[k], q[k + 1], b.deck_y[k], b.deck_y[k + 1], b.width * .5f + .5f)); }
            var deckIdx = new CellIndex(16f); foreach (var d in decks) deckIdx.Add(d.a, d.b, d.half);
            var rings = new List<List<Vector2>>(); var ringIdx = new CellIndex(32f);
            foreach (var bd in map.buildings) { if (bd.footprint == null || bd.footprint.Length < 6) continue; var q = ToPoints(bd.footprint); rings.Add(q); ringIdx.Add(q, 2f); }
            // ---- windows of R77Win m with 120 m of context, over the corridor's box, on a 1 m grid: the free ground within the reach relaxed as a membrane
            // (each cell the mean of its four neighbours, over-relaxed) between what is held and the ground beyond the reach: no spike, wall or mound
            // can stay (a membrane has no peak of its own), embankments and cuttings run smoothly from the road's edge to the ground, a median spans flat
            var all = segs.SelectMany(s => new[] { s.a, s.b }).ToList(); var box = BoundsOf(all, R77Reach + 2f);
            int faceB = 0, faceA = 0, nWin = 0, changed = 0, iters = 0; float maxCut = 0f, maxFill = 0f, resid = 0f;
            var spotBefore = R77Spots.Select(s => R77Steepest(terrain, s.at, 30f)).ToArray();
            var near = new List<int>(); var near2 = new List<int>();
            for (float wx = box.xMin; wx < box.xMax; wx += R77Win)
                for (float wz = box.yMin; wz < box.yMax; wz += R77Win)
                {
                    var inner = Rect.MinMaxRect(wx, wz, Mathf.Min(wx + R77Win, box.xMax), Mathf.Min(wz + R77Win, box.yMax));
                    if (segIdx.Near(inner.xMin - R77Reach, inner.yMin - R77Reach, inner.xMax + R77Reach, inner.yMax + R77Reach, near).Count == 0) continue;
                    var outer = Rect.MinMaxRect(inner.xMin - 120f, inner.yMin - 120f, inner.xMax + 120f, inner.yMax + 120f);
                    int nx = Mathf.CeilToInt(outer.width / R77Step) + 1, nz = Mathf.CeilToInt(outer.height / R77Step) + 1, N = nx * nz;
                    var H0 = new float[N]; var Y = new float[N]; var fix = new bool[N]; var dc = new float[N];
                    bool any = false;
                    for (int j = 0; j < nz; j++)
                        for (int i = 0; i < nx; i++)
                        {
                            int id = j * nx + i; float x = outer.xMin + i * R77Step, z = outer.yMin + j * R77Step; var p = new Vector2(x, z);
                            float h0 = H0[id] = Y[id] = terrain.Has(x, z) ? terrain.Height(x, z) : float.NaN; dc[id] = float.MaxValue; fix[id] = true;
                            if (float.IsNaN(h0) || i == 0 || j == 0 || i == nx - 1 || j == nz - 1) continue;
                            float d1 = float.MaxValue, t1 = 0f, h1 = 0f, rc = R77Reach, scaled = float.MaxValue; bool past = false;
                            foreach (int k in segIdx.Near(p, 0f, near))
                            {
                                var s = segs[k]; var ab = s.b - s.a; float tr = Vector2.Dot(p - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f), tt = Mathf.Clamp01(tr); float d = (s.a + ab * tt - p).magnitude;
                                if (d < d1) { d1 = d; t1 = Mathf.Lerp(s.ya, s.yb, tt); h1 = s.half; rc = s.reach; past = tr < -.01f || tr > 1.01f; }
                                scaled = Mathf.Min(scaled, d * R77Reach / s.reach);   // (a street's reach scaled to the connector's)
                            }
                            dc[id] = scaled; if (scaled > R77Reach) continue; any = true;
                            // a deck over it: held where it stands clear, a clean floor under it where it does not
                            float dd = float.MaxValue, dy = 0f;
                            foreach (int k in deckIdx.Near(p, 0f, near2)) { var d = decks[k]; var ab = d.b - d.a; float tt = Mathf.Clamp01(Vector2.Dot(p - d.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float e = (d.a + ab * tt - p).magnitude; if (e <= d.half && e < dd) { dd = e; dy = Mathf.Lerp(d.ya, d.yb, tt); } }
                            // the carriageway at its profile; Round 79 (D): not under a deck past the road's end (the road's end height carried flat under a
                            // deck climbing 13 % off it stood 1.1 m through the Canton Road Connector's southbound deck: a wall)
                            if (d1 <= h1 + 1f && !(past && dd < float.MaxValue)) { Y[id] = t1; continue; }
                            if (dd < float.MaxValue) { if (dy - .1f < h0 + 1.5f) Y[id] = Mathf.Min(h0, dy - .1f); continue; }
                            bool hold = false;
                            foreach (int k in heldIdx.Near(p, 0f, near2)) { var h = held[k]; var ab = h.b - h.a; float tt = Mathf.Clamp01(Vector2.Dot(p - h.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); if ((h.a + ab * tt - p).sqrMagnitude <= h.half * h.half) { hold = true; break; } }
                            if (!hold) foreach (int k in ringIdx.Near(p, 0f, near2)) if (InPolygon(p, rings[k]) || DistToEdges(p, rings[k]) < 2f) { hold = true; break; }
                            if (!hold && Beds != null && Beds.Wet(x, z)) hold = true;
                            if (!hold) fix[id] = false;
                        }
                    if (!any) continue; nWin++;
                    // over-relaxed Gauss-Seidel, red and black, to a tenth of a millimetre or 1500 sweeps
                    var free = new List<int>();
                    for (int k = 0; k < N; k++) if (!fix[k]) { if (float.IsNaN(Y[k - 1]) || float.IsNaN(Y[k + 1]) || float.IsNaN(Y[k - nx]) || float.IsNaN(Y[k + nx])) fix[k] = true; else free.Add(k); }
                    var red = free.Where(k => ((k % nx) + (k / nx)) % 2 == 0).ToArray(); var black = free.Where(k => ((k % nx) + (k / nx)) % 2 == 1).ToArray();
                    const float Omega = 1.9f; int it = 0; float maxStep = 0f;
                    for (; it < 1500; it++)
                    {
                        maxStep = 0f;
                        foreach (var set in new[] { red, black })
                            foreach (int k in set)
                            {
                                float g = .25f * (Y[k - 1] + Y[k + 1] + Y[k - nx] + Y[k + nx]), stp = Omega * (g - Y[k]); Y[k] += stp;
                                if (Mathf.Abs(stp) > maxStep) maxStep = Mathf.Abs(stp);
                            }
                        if (maxStep < 1e-4f) break;
                    }
                    iters = Mathf.Max(iters, it); resid = Mathf.Max(resid, maxStep);
                    // faces before / after over the inner window, within the reach
                    for (int j = 1; j + 1 < nz; j++)
                        for (int i = 1; i + 1 < nx; i++)
                        {
                            int k = j * nx + i; float x = outer.xMin + i * R77Step, z = outer.yMin + j * R77Step; if (!inner.Contains(new Vector2(x, z)) || dc[k] > R77Reach || float.IsNaN(H0[k])) continue;
                            if (float.IsNaN(H0[k + 1]) || float.IsNaN(H0[k - 1]) || float.IsNaN(H0[k + nx]) || float.IsNaN(H0[k - nx])) continue;
                            float gb = Mathf.Max(Mathf.Abs(H0[k + 1] - H0[k - 1]), Mathf.Abs(H0[k + nx] - H0[k - nx])) / (2f * R77Step);
                            float ga = Mathf.Max(Mathf.Abs(Y[k + 1] - Y[k - 1]), Mathf.Abs(Y[k + nx] - Y[k - nx])) / (2f * R77Step);
                            if (gb > 1f) faceB++; if (ga > 1f) faceA++;
                            float dlt = Y[k] - H0[k]; maxCut = Mathf.Max(maxCut, -dlt); maxFill = Mathf.Max(maxFill, dlt);
                        }
                    // onto the terrain over the inner window: within the reach the grid's surface itself (bilinear), so a spike between two of its points
                    // goes too (build 1 added the grid's change to each terrain sample: a sample's own spike, finer than the 1 m grid, stayed); easing to
                    // the change alone over the reach's last 3 m, nothing past it
                    float Bi(float[] a, float x, float z)
                    {
                        float fx = (x - outer.xMin) / R77Step, fz = (z - outer.yMin) / R77Step; int i = Mathf.Clamp((int)fx, 0, nx - 2), j = Mathf.Clamp((int)fz, 0, nz - 2); float u = fx - i, v = fz - j;
                        float S(int ii, int jj) { float q = a[jj * nx + ii]; return float.IsNaN(q) || q > 1e8f ? float.NaN : q; }
                        float s00 = S(i, j), s10 = S(i + 1, j), s01 = S(i, j + 1), s11 = S(i + 1, j + 1); if (float.IsNaN(s00 + s10 + s01 + s11)) return float.NaN;
                        return Mathf.Lerp(Mathf.Lerp(s00, s10, u), Mathf.Lerp(s01, s11, u), v);
                    }
                    var Dlt = new float[N]; for (int k = 0; k < N; k++) Dlt[k] = float.IsNaN(Y[k]) || float.IsNaN(H0[k]) ? 0f : Y[k] - H0[k];
                    EditTerrain(terrain, inner, (x, z, y) =>
                    {
                        if (x < inner.xMin || x >= inner.xMax || z < inner.yMin || z >= inner.yMax) return y;
                        float d = Bi(dc, x, z); if (float.IsNaN(d) || d > R77Reach) return y;
                        float delta = Bi(Dlt, x, z), abs = Bi(Y, x, z); if (float.IsNaN(delta)) return y;
                        float ny = float.IsNaN(abs) ? y + delta : Mathf.Lerp(y + delta, abs, Smooth01((R77Reach - d) / 3f));
                        if (Mathf.Abs(ny - y) < 1e-3f) return y; changed++; return ny;
                    });
                }
            var spotAfter = R77Spots.Select(s => R77Steepest(terrain, s.at, 30f)).ToArray();
            log.Add($"{corr.Count} road pieces ({chains.Count} chains, {segs.Count} m), {nPins} held profile points; the profile moved at most {worstDip:0.00} m from the ground; {nWin} windows; {changed} terrain samples changed (cut up to {maxCut:0.0} m, filled up to {maxFill:0.0} m); the membrane: up to {iters} sweeps, last change {resid * 1000f:0.0} mm");
            log.Add($"cells steeper than 45 degrees within {R77Reach:0} m of a carriageway: {faceB} -> {faceA}");
            for (int i = 0; i < R77Spots.Length; i++) log.Add($"{R77Spots[i].name} ({R77Spots[i].at.x:0.00}, {R77Spots[i].at.y:0.00}): the steepest ground within 30 m {spotBefore[i]:0} -> {spotAfter[i]:0} degrees");
            Debug.Log("[Round77] A: " + string.Join("; ", log.Skip(1)));
            try { File.WriteAllLines(Path.Combine(ProjectRoot, GeneratedDir, "round77_embankments.txt"), log); } catch (System.Exception e) { Debug.LogWarning("[Round77] " + e.Message); }
        }

        // a check's shortcut (TopDownShots -r77Probe): the pass run on the built scene's own ground, in memory (never saved), so the pictures show it
        public static void R77ProbeOnScene()
        {
            var meta = LoadTerrainMeta(); var map = JsonUtility.FromJson<MapData>(File.ReadAllText(Path.Combine(ProjectRoot, "Data/map.json")));
            vScale = map.vertical_scale > 0f ? map.vertical_scale : 1f;
            var grid = new TerrainGrid { Tiles = new Terrain[meta.tiles_x, meta.tiles_z], TileW = meta.tile_w_m, TileD = meta.tile_d_m, BaseY = meta.min_height, OriginX = meta.origin_x_m, OriginZ = meta.origin_z_m };
            foreach (var tr in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None)) { int i = Mathf.RoundToInt((tr.transform.position.x - meta.origin_x_m) / meta.tile_w_m), j = Mathf.RoundToInt((tr.transform.position.z - meta.origin_z_m) / meta.tile_d_m); grid.Tiles[i, j] = tr; }
            R77Embankments(map, grid);
            // -r77Dump <file>: the ground after it round Dan's spots (x, z, height; 1 m, 50 m each way)
            var a = System.Environment.GetCommandLineArgs(); int ai = System.Array.IndexOf(a, "-r77Dump");
            if (ai >= 0 && ai + 1 < a.Length)
            {
                var sb = new System.Text.StringBuilder("spot,x,z,y\n");
                foreach (var (nm, at) in R77Spots) for (float dz = -50f; dz <= 50f; dz += 1f) for (float dx = -50f; dx <= 50f; dx += 1f)
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1:0.0},{2:0.0},{3:0.000}\n", nm, at.x + dx, at.y + dz, grid.Height(at.x + dx, at.y + dz)));
                File.WriteAllText(a[ai + 1], sb.ToString());
            }
        }

        // the steepest ground (degrees) within r m of a point, from 1 m differences of the terrain
        static float R77Steepest(TerrainGrid terrain, Vector2 at, float r)
        {
            float worst = 0f;
            for (float dx = -r; dx <= r; dx += 1f) for (float dz = -r; dz <= r; dz += 1f)
            {
                float x = at.x + dx, z = at.y + dz; if (dx * dx + dz * dz > r * r) continue;
                float g = Mathf.Max(Mathf.Abs(terrain.Height(x + .5f, z) - terrain.Height(x - .5f, z)), Mathf.Abs(terrain.Height(x, z + .5f) - terrain.Height(x, z - .5f)));
                worst = Mathf.Max(worst, Mathf.Atan(g) * Mathf.Rad2Deg);
            }
            return worst;
        }

        // smoothing along a line: minimise sum w (t - raw)^2 + mu sum (t[i+1] - t[i])^2 (a tridiagonal system, Thomas' method)
        static float[] R77Smooth1D(float[] raw, float[] w, float mu)
        {
            int n = raw.Length; var a = new double[n]; var b = new double[n]; var c = new double[n]; var d = new double[n];
            for (int i = 0; i < n; i++)
            {
                double left = i > 0 ? mu : 0, right = i + 1 < n ? mu : 0;
                a[i] = -left; c[i] = -right; b[i] = w[i] + left + right; d[i] = w[i] * raw[i];
            }
            for (int i = 1; i < n; i++) { double m = a[i] / b[i - 1]; b[i] -= m * c[i - 1]; d[i] -= m * d[i - 1]; }
            var t = new float[n]; t[n - 1] = (float)(d[n - 1] / b[n - 1]);
            for (int i = n - 2; i >= 0; i--) t[i] = (float)((d[i] - c[i] * t[i + 1]) / b[i]);
            return t;
        }
    }
}
