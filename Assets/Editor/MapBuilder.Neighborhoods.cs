using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Round 30 (Part B): the neighborhoods still built as plain boxes, each dressed with one look of its own
// (Data/neighborhood/neighborhoods.json, Tools/mapgen/neighborhoods_rest.py: the streets, houses and the era picked from the
// footprints). Their houses are built with Chastain Glen's kit (MapBuilder.Houses.cs and the four era styles in
// MapBuilder.HouseEras.cs) on their real OSM footprints, facing their own street, with the floor at the foot of the garage,
// a block foundation where the floor stands over the ground and a walk-out wall at a high back (Round 27's), shrubs, one box
// collider each, a breakable mailbox at the street and, behind some, a breakable privacy fence (the Glen's FenceChunk). Their
// drives are the map's own (OSM). Their streets are left as they are: no curbs, sidewalks or lamps (not asked for).
// Each neighborhood: 3 to 5 house variants of its era and a palette of its own (picked by its key), so it is varied but one place.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class NeighborhoodsJson { public NeighborhoodJson[] neighborhoods; }
        [Serializable] class NeighborhoodJson { public string key, name, era, why; public int[] roads, houses; public string[] streets; public float x, z; public int median_area, median_spacing; }

        sealed class RestNeighborhoods
        {
            public NeighborhoodsJson Data;
            public readonly List<HouseSpec> Houses = new List<HouseSpec>();
            public readonly HashSet<int> Owned = new HashSet<int>();
            public readonly Dictionary<int, int> Of = new Dictionary<int, int>();          // house index -> neighborhood
            public readonly Dictionary<int, List<RoadSeg>> Segs = new Dictionary<int, List<RoadSeg>>();
            public readonly HashSet<int> Foundation = new HashSet<int>();
            public readonly Dictionary<int, float> BackExposure = new Dictionary<int, float>();
            public readonly List<FenceRun> Fences = new List<FenceRun>();
            public bool Owns(int i) => Owned.Contains(i);
            // no tree inside a house's rectangle (grown 1.5 m, 3.5 m at the front for porches and carports)
            Dictionary<Vector2Int, List<HouseSpec>> grid;
            public bool RejectTree(float x, float z)
            {
                if (grid == null) { grid = new Dictionary<Vector2Int, List<HouseSpec>>(); foreach (var h in Houses) { var k = new Vector2Int(Mathf.FloorToInt(h.footprintCentre.x / 40f), Mathf.FloorToInt(h.footprintCentre.y / 40f)); if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<HouseSpec>(); l.Add(h); } }
                var p = new Vector2(x, z); var c = new Vector2Int(Mathf.FloorToInt(x / 40f), Mathf.FloorToInt(z / 40f));
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                    if (grid.TryGetValue(c + new Vector2Int(dx, dz), out var l))
                        foreach (var h in l)
                        {
                            var d = p - h.footprintCentre; float v = Vector2.Dot(d, new Vector2(h.frame.F.x, h.frame.F.z));
                            if (Mathf.Abs(Vector2.Dot(d, new Vector2(h.frame.R.x, h.frame.R.z))) < h.W / 2 + 1.5f && v > -h.D / 2 - 1.5f && v < h.D / 2 + 3.5f) return true;
                        }
                return false;
            }
            public int Buried, Extra;
        }

        // the eras' palettes (East Cobb: painted brick and siding on the ranches, brick fronts on the traditionals, earthy siding
        // and stacked stone on the craftsman houses, brick and siding mixed along a townhome row)
        static readonly Color[] RanchSiding = { new Color(0.93f, 0.92f, 0.88f), new Color(0.88f, 0.84f, 0.66f), new Color(0.66f, 0.74f, 0.66f), new Color(0.66f, 0.75f, 0.82f), new Color(0.76f, 0.68f, 0.56f), new Color(0.80f, 0.79f, 0.76f), new Color(0.62f, 0.66f, 0.60f) };
        static readonly Color[] RanchBrick = { new Color(0.55f, 0.24f, 0.17f), new Color(0.62f, 0.40f, 0.28f), new Color(0.84f, 0.82f, 0.78f), new Color(0.48f, 0.30f, 0.24f) };   // red, tan, painted white, brown
        static readonly Color[] CraftsSiding = { new Color(0.47f, 0.53f, 0.45f), new Color(0.36f, 0.42f, 0.48f), new Color(0.66f, 0.62f, 0.50f), new Color(0.58f, 0.57f, 0.54f), new Color(0.42f, 0.45f, 0.36f), new Color(0.82f, 0.78f, 0.67f), new Color(0.55f, 0.47f, 0.40f) };
        static readonly Color[] StoneTones = { new Color(0.56f, 0.52f, 0.46f), new Color(0.48f, 0.46f, 0.44f), new Color(0.62f, 0.56f, 0.47f) };
        static readonly Color[] TradSiding = { new Color(0.92f, 0.92f, 0.89f), new Color(0.86f, 0.80f, 0.64f), new Color(0.55f, 0.58f, 0.63f), new Color(0.78f, 0.74f, 0.66f), new Color(0.84f, 0.82f, 0.74f), new Color(0.70f, 0.72f, 0.70f) };
        static readonly Color[] TownSiding = { new Color(0.86f, 0.82f, 0.72f), new Color(0.70f, 0.72f, 0.70f), new Color(0.62f, 0.66f, 0.72f), new Color(0.80f, 0.74f, 0.62f) };
        static readonly Color[] BrownRoofs = { new Color(0.30f, 0.24f, 0.19f), new Color(0.26f, 0.22f, 0.19f), new Color(0.34f, 0.35f, 0.37f), new Color(0.19f, 0.19f, 0.21f) };

        static RestNeighborhoods LoadRest(MapData map, TerrainGrid terrain, Neighborhood hood, ShoppingCenter shop)
        {
            var rest = new RestNeighborhoods();
            string path = Path.Combine(ProjectRoot, "Data/neighborhood/neighborhoods.json");
            if (!File.Exists(path)) { Debug.LogWarning("[Round30] no Data/neighborhood/neighborhoods.json (run Tools/mapgen/neighborhoods_rest.py): the other neighborhoods stay plain"); return rest; }
            rest.Data = JsonUtility.FromJson<NeighborhoodsJson>(File.ReadAllText(path));
            // the map's own drives (OSM): a house a drive runs up to faces down its drive, its garage at the drive's end
            var driveEnds = new List<(Vector2 near, Vector2 far)>();
            foreach (var dw in map.driveways) { var dp = ToPoints(dw.points); if (dp.Count < 2) continue; driveEnds.Add((dp[0], dp[dp.Count - 1])); driveEnds.Add((dp[dp.Count - 1], dp[0])); }
            int byDrive = 0;
            for (int n = 0; n < rest.Data.neighborhoods.Length; n++)
            {
                var nb = rest.Data.neighborhoods[n];
                var segs = rest.Segs[n] = new List<RoadSeg>();
                foreach (int ri in nb.roads) { var p = ToPoints(map.roads[ri].points); for (int i = 0; i + 1 < p.Count; i++) segs.Add(new RoadSeg { a = p[i], b = p[i + 1], half = map.roads[ri].width * .5f, road = ri }); }
                foreach (int idx in nb.houses) Dress(idx, n, segs);
            }
            // Round 31 (4): the houses outside every neighborhood (along the bigger roads, or too few on a street group: OSM
            // detached houses and the rectangular terrace units) get the same kit, in the look of the nearest neighborhood, facing
            // their own drive or else the nearest road within 60 m
            int extra0 = rest.Houses.Count;
            var homes = new List<(Vector2 c, int n)>();
            foreach (var hs in rest.Houses) homes.Add((hs.footprintCentre, rest.Of[hs.index]));
            for (int idx = 0; idx < map.buildings.Length; idx++)
            {
                var b = map.buildings[idx];
                if (b.type != "detached" && b.type != "house" && b.type != "terrace") continue;
                if (hood.OwnsBuilding(idx) || shop.Owns(idx) || rest.Owned.Contains(idx) || Round32NotHouses.Contains(idx)) continue;   // Round 32 (A4): the ice cream shop
                var ring = ToPoints(b.footprint); if (ring.Count < 3) continue;
                if (b.type == "terrace" && (!BestRectangle(ring, out _, out _, out _, out float thl, out float thw) || Mathf.Abs(PolyArea(ring)) / (4f * thl * thw) < 0.8f)) continue;   // a cross-shaped block: an apartment building (MapBuilder.Businesses.cs)
                Vector2 c = Vector2.zero; foreach (var q in ring) c += q; c /= ring.Count;
                int n = -1; float best = float.MaxValue;
                foreach (var hm in homes) { float d = (hm.c - c).sqrMagnitude; if (d < best) { best = d; n = hm.n; } }
                if (n < 0) continue;
                var near = new List<RoadSeg>();
                for (int ri = 0; ri < map.roads.Length; ri++)
                {
                    var r = map.roads[ri]; if (r.@class == "motorway" || r.@class == "motorway_link") continue;
                    var p = ToPoints(r.points);
                    for (int i = 0; i + 1 < p.Count; i++) if (SegDist(c, p[i], p[i + 1]) < 60f) near.Add(new RoadSeg { a = p[i], b = p[i + 1], half = r.width * .5f, road = ri });
                }
                if (near.Count == 0) continue;
                Dress(idx, n, near);
            }
            rest.Extra = rest.Houses.Count - extra0;

            void Dress(int idx, int n, List<RoadSeg> segs)
            {
                var nb = rest.Data.neighborhoods[n]; int nseed = nb.key.GetHashCode() & 0x7fffffff;
                // the neighborhood's own few colors and its mix of variants
                var siding = nb.era == "ranch" ? RanchSiding : nb.era == "craftsman" ? CraftsSiding : nb.era == "townhomes" ? TownSiding : TradSiding;
                var mySiding = Enumerable.Range(0, 4).Select(k => PickOf(siding, nseed, 500 + k)).Distinct().ToArray();
                var myBrick = nb.era == "ranch" ? new[] { PickOf(RanchBrick, nseed, 510), PickOf(RanchBrick, nseed, 511) } : new[] { PickOf(BrickPalette, nseed, 512), PickOf(BrickPalette, nseed, 513) };
                var myRoof = new[] { PickOf(nb.era == "ranch" || nb.era == "craftsman" ? BrownRoofs : ShingleRoofs, nseed, 520), PickOf(ShingleRoofs, nseed, 521) };
                var myStone = PickOf(StoneTones, nseed, 530);
                float brickShare = nb.era == "ranch" ? 0.55f : nb.era == "traditional" ? 0.6f : 0.25f;
                {
                    if (hood.OwnsBuilding(idx) || shop.Owns(idx) || rest.Owned.Contains(idx)) return;
                    var b = map.buildings[idx]; var ring = ToPoints(b.footprint);
                    if (ring.Count < 3 || !BestRectangle(ring, out Vector2 c, out Vector2 U, out Vector2 V, out float hl, out float hw)) return;
                    float area = Mathf.Abs(PolyArea(ring));
                    // the nearest of its own streets, by the centreline (as the Glen's houses)
                    Vector2 q = c, tan = Vector2.right; float roadHalf = 4f, best = float.MaxValue;
                    foreach (var s in segs) { var ab = s.b - s.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(c - s.a, ab) / ab.sqrMagnitude); var p = s.a + ab * t; float dd = (p - c).magnitude; if (dd < best) { best = dd; q = p; roadHalf = s.half; tan = ab.normalized; } }
                    Vector2 toRoad = (q - c).normalized, garageAt = q;
                    float dBest = 3.5f; (Vector2 near, Vector2 far)? drive = null;
                    foreach (var de in driveEnds) { if ((de.near - c).sqrMagnitude > 900f) continue; float dd = InPolygon(de.near, ring) ? 0f : DistToEdgesStatic(de.near, ring); if (dd < dBest && (de.far - c).magnitude > (de.near - c).magnitude) { dBest = dd; drive = de; } }
                    if (drive.HasValue) { toRoad = (drive.Value.far - c).normalized; garageAt = drive.Value.near; byDrive++; }
                    Vector2[] cand = { U, -U, V, -V }; Vector2 F = cand[0]; float bd = -2f;
                    foreach (var cd in cand) { float dd = Vector2.Dot(cd, toRoad); if (dd > bd) { bd = dd; F = cd; } }
                    bool facadeAlongU = Mathf.Abs(Vector2.Dot(F, U)) < 0.7f;
                    float W = facadeAlongU ? hl * 2f : hw * 2f, D = facadeAlongU ? hw * 2f : hl * 2f;
                    if (W < 6.5f || D < 6f) return;   // too small for any of the styles: left as the map's own box
                    var h = new HouseSpec { index = idx, seed = b.seed, ring = ring, footprintCentre = c, W = W, D = D, nearestRoadPoint = q, roadHalf = roadHalf, community = -1 - n };
                    h.style = EraStyle(nb.era, area, Mathf.Max(W, D), Mathf.Min(W, D), b.seed);
                    if (h.style == HouseStyle.G && W < 12f) h.style = HouseStyle.D;   // a single narrow unit: a plain two-story house
                    // colors: the Glen's rules, then the neighborhood's own palette
                    ChooseColours(h);
                    h.sidingC = PickOf(mySiding, b.seed, 540); h.brickC = Shade(PickOf(myBrick, b.seed, 541), 0.95f + 0.1f * Hash01(b.seed, 47)); h.roofC = PickOf(myRoof, b.seed, 542);
                    h.stoneC = Shade(myStone, 0.95f + 0.1f * Hash01(b.seed, 543));
                    h.brick = h.style == HouseStyle.B || (h.style != HouseStyle.D && h.style != HouseStyle.F && Hash01(b.seed, 544) < brickShare);
                    if (h.style == HouseStyle.F) { h.garageC = Hash01(b.seed, 545) < 0.6f ? new Color(0.36f, 0.24f, 0.15f) : GarageWhite; h.doorC = new Color(0.33f, 0.21f, 0.12f); h.trimC = Hash01(nseed, 546) < 0.5f ? TrimWhite : new Color(0.88f, 0.85f, 0.76f); }
                    if (h.style == HouseStyle.G) h.garageBays = Hash01(nseed, 547) < 0.65f ? 1 : 0;
                    if (h.brick && h.style == HouseStyle.A && h.sidingC.grayscale < 0.5f) h.sidingC = mySiding.OrderByDescending(x => x.grayscale).First();
                    // the garage on the side nearest the road point (as the Glen's), the floor at the foot of its doors
                    var Rv = Vector3.Cross(Vector3.up, -new Vector3(F.x, 0f, F.y));
                    float uR = Vector2.Dot(garageAt - c, new Vector2(Rv.x, Rv.z));
                    h.frame = MakeFrame(new Vector3(c.x, 0f, c.y), F, uR > 0f ? -1f : 1f);
                    EraGarageSpan(h);
                    Vector3 g = h.frame.P((h.garageU0 + h.garageU1) / 2f, 0f, h.D / 2f + 0.4f);
                    float lo = float.MaxValue, hi = float.MinValue;
                    foreach (var p in ring) { float gy = terrain.Height(p.x, p.y); lo = Mathf.Min(lo, gy); hi = Mathf.Max(hi, gy); }
                    h.floorY = terrain.Height(g.x, g.z) + 0.16f;
                    h.baseY = Mathf.Min(lo, h.floorY) - 0.45f;
                    h.frame.O = new Vector3(c.x, h.floorY, c.y);
                    h.garageCentre = new Vector3(g.x, h.floorY, g.z);
                    // Round 27's settling, without cutting the ground: a block foundation under a floor that stands over the ground,
                    // a walk-out wall at a high back; a house whose ground stands over its floor is counted (left as the box was)
                    if (h.floorY - lo > 0.6f) rest.Foundation.Add(idx);
                    float backLo = float.MaxValue; foreach (float uu in new[] { -W / 2 + 0.5f, 0f, W / 2 - 0.5f }) { var bp = h.frame.P(uu, 0f, -D / 2 - 0.3f); backLo = Mathf.Min(backLo, terrain.Height(bp.x, bp.z)); }
                    if (h.floorY - backLo > 1.5f && h.style != HouseStyle.G) rest.BackExposure[idx] = h.floorY - backLo;
                    if (hi - h.floorY > 0.6f) rest.Buried++;
                    rest.Houses.Add(h); rest.Owned.Add(idx); rest.Of[idx] = n;
                }
            }
            Debug.Log($"[Round30] other neighborhoods: {rest.Data.neighborhoods.Length}, {rest.Houses.Count} houses dressed ({rest.Extra} of them outside every neighborhood, Round 31) (styles " +
                      string.Join(", ", Enum.GetValues(typeof(HouseStyle)).Cast<HouseStyle>().Select(s => $"{s} {rest.Houses.Count(h => h.style == s)}")) +
                      $"), {byDrive} facing down their own drive, {rest.Foundation.Count} block foundations, {rest.BackExposure.Count} walk-out walls, {rest.Buried} with ground over 0.6 m above the floor somewhere (not cut)");
            return rest;
        }

        static HouseStyle EraStyle(string era, float area, float longSide, float shortSide, int seed)
        {
            float r = Hash01(seed, 551), aspect = longSide / Mathf.Max(shortSide, 1f);
            switch (era)
            {
                case "ranch": return area >= 230f ? (r < 0.6f ? HouseStyle.E : HouseStyle.C) : aspect >= 1.35f ? (r < 0.45f ? HouseStyle.H : r < 0.75f ? HouseStyle.C : HouseStyle.E) : (r < 0.5f ? HouseStyle.E : HouseStyle.H);
                case "craftsman": return r < 0.72f ? HouseStyle.F : HouseStyle.A;
                case "townhomes": return HouseStyle.G;
                default: return area >= 190f && r < 0.3f ? HouseStyle.B : r < 0.62f ? HouseStyle.A : r < 0.86f ? HouseStyle.D : HouseStyle.C;
            }
        }
        static void EraGarageSpan(HouseSpec h)
        {
            float W = h.W;
            switch (h.style)
            {
                case HouseStyle.E: h.garageU0 = -W / 2; h.garageU1 = -W / 2 + Mathf.Min(SplitBlockWidth(h) - 0.6f, 5.8f); break;
                case HouseStyle.F: h.garageU0 = -W / 2; h.garageU1 = -W / 2 + Mathf.Clamp(W * 0.42f, 5.8f, 6.6f); break;
                case HouseStyle.G: h.garageU0 = -W / 2 + 0.4f; h.garageU1 = -W / 2 + Mathf.Min(W / TownUnits(h) - 0.6f, 3.3f); break;
                case HouseStyle.H: h.garageU0 = -W / 2; h.garageU1 = -W / 2 + CarportWidth(h); break;
                default: GarageSpan(h); break;
            }
        }

        // ------------------------------------------------------------ fences: behind about a third of the houses, where there is room

        static void PlanRestFences(MapData map, RestNeighborhoods rest, Neighborhood hood, ShoppingCenter shop, CreekTrack creek, Pipes pipes, Network net)
        {
            var roads = new RoadIndex(map.roads);
            // every building's footprint, grown 1 m, in a grid
            var grid = new Dictionary<Vector2Int, List<List<Vector2>>>();
            foreach (var b in map.buildings)
            {
                var ring = ToPoints(b.footprint); if (ring.Count < 3) continue;
                var c = ring.Aggregate(Vector2.zero, (s, p) => s + p) / ring.Count; var k = new Vector2Int(Mathf.FloorToInt(c.x / 40f), Mathf.FloorToInt(c.y / 40f));
                if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<List<Vector2>>(); l.Add(ring);
            }
            var drives = new List<(Vector2 a, Vector2 b)>();
            foreach (var dw in map.driveways) { var p = ToPoints(dw.points); for (int i = 0; i + 1 < p.Count; i++) drives.Add((p[i], p[i + 1])); }
            bool Free(Vector2 p)
            {
                if (roads.Covers(p, NoRoad, 1.5f)) return false;
                if (hood.InCorridor(p, 2f) || Beds.Wet(p.x, p.y) || shop.RejectTree(p.x, p.y)) return false;
                if ((creek != null && creek.RejectTree(p.x, p.y)) || pipes.RejectTree(p.x, p.y) || (net != null && net.RejectTree(p.x, p.y))) return false;   // off the trail and the pipe and network cuts
                if (RidgeNow != null && RidgeNow.Cleared(p.x, p.y)) return false;   // Round 33: off the Chastain Ridge race's run-ups, landings and the way out of the hollow
                var k = new Vector2Int(Mathf.FloorToInt(p.x / 40f), Mathf.FloorToInt(p.y / 40f));
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                    if (grid.TryGetValue(k + new Vector2Int(dx, dz), out var l)) foreach (var ring in l) if (InPolygon(p, ring) || DistToEdgesStatic(p, ring) < 1.0f) return false;
                foreach (var (a, b) in drives) if ((a - p).sqrMagnitude < 900f && SegDistStatic(p, a, b) < 3f) return false;
                return true;
            }
            foreach (var h in rest.Houses)
            {
                if (h.style == HouseStyle.G || Hash01(h.seed, 560) > 0.33f) continue;
                // a U behind the house: from its back corners straight back, then across
                float depth = 8f + 4f * Hash01(h.seed, 561), side = 1.2f;
                var f = h.frame; Vector2 P(float u, float v) { var w = f.P(u, 0f, v); return new Vector2(w.x, w.z); }
                var pts = new List<Vector2> { P(-h.W / 2 - side, -h.D / 2 + 0.6f), P(-h.W / 2 - side, -h.D / 2 - depth), P(h.W / 2 + side, -h.D / 2 - depth), P(h.W / 2 + side, -h.D / 2 + 0.6f) };
                bool ok = true;
                for (int i = 1; i < pts.Count && ok; i++) { float L = (pts[i] - pts[i - 1]).magnitude; for (float t = 0; t <= L && ok; t += 1f) ok = Free(Vector2.Lerp(pts[i - 1], pts[i], t / Mathf.Max(L, .01f))); }
                if (!ok) continue;
                rest.Fences.Add(new FenceRun { kind = FenceKind.Privacy, pts = pts, seed = h.seed });
            }
        }
        static float SegDistStatic(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return (a + ab * t - p).magnitude; }
        static float DistToEdgesStatic(Vector2 p, List<Vector2> ring) { float best = float.MaxValue; for (int i = 0; i < ring.Count; i++) best = Mathf.Min(best, SegDistStatic(p, ring[i], ring[(i + 1) % ring.Count])); return best; }

        // ------------------------------------------------------------ building them

        static void BuildRest(MapData map, TerrainGrid terrain, Materials mats, Transform world, RestNeighborhoods rest)
        {
            if (rest.Houses.Count == 0) return;
            var root = new GameObject("Neighborhoods").transform; root.SetParent(world, false);
            var chunks = new Dictionary<Vector2Int, MeshBuilder>(); var detail = new Dictionary<Vector2Int, MeshBuilder>();
            var colRoot = new GameObject("HouseColliders").transform; colRoot.SetParent(root, false);
            var hroot = new GameObject("Houses").transform; hroot.SetParent(root, false);
            var mailboxes = new Breakables(); var roads = new RoadIndex(map.roads);
            var drives = new List<(Vector2 a, Vector2 b)>();
            foreach (var dw in map.driveways) { var p = ToPoints(dw.points); for (int i = 0; i + 1 < p.Count; i++) drives.Add((p[i], p[i + 1])); }
            int mail = 0; var views = new StringBuilder();
            var shown = new HashSet<int>();
            foreach (var h in rest.Houses)
            {
                var key = new Vector2Int(Mathf.FloorToInt(h.footprintCentre.x / 128f), Mathf.FloorToInt(h.footprintCentre.y / 128f));
                if (!chunks.TryGetValue(key, out var mb)) chunks[key] = mb = new MeshBuilder();
                if (!detail.TryGetValue(key, out var db)) detail[key] = db = new MeshBuilder();
                h.detail = db;
                if (rest.Foundation.Contains(h.index)) FoundationTint = FoundationBlock;
                BuildHouse(mb, h);
                FoundationTint = null;
                if (rest.BackExposure.TryGetValue(h.index, out float bx)) Walkout(db, h, bx, (x, z) => terrain.Height(x, z));
                int shrubs = 2 + (int)(Hash01(h.seed, 81) * 3f);
                for (int i = 0; i < shrubs && h.style != HouseStyle.G; i++)
                {
                    float u = Mathf.Lerp(h.garageU1 + 0.6f, h.W / 2 - 0.5f, (i + 0.5f) / shrubs);
                    Vector3 p = h.frame.P(u, 0f, h.D / 2 + 0.9f + Hash01(h.seed, 90 + i) * 0.3f);
                    float gy = terrain.Height(p.x, p.z), r = 0.5f + 0.35f * Hash01(h.seed, 100 + i);
                    var col = PickOf(ShrubGreens, h.seed, 110 + i);
                    Blob(mb, new Vector3(p.x, gy + 0.05f, p.z), new Vector3(r, r * 0.85f, r), col * 1.15f, col * 0.75f, 8, 4, true, h.seed + i);
                }
                // one box over the footprint
                var go = new GameObject("House " + h.index); go.transform.SetParent(colRoot, false);
                float hh = h.style == HouseStyle.C || h.style == HouseStyle.H ? 3.0f : h.style == HouseStyle.E ? 5.0f : 5.9f;
                float yMid = (h.baseY + h.floorY + hh) / 2f;
                go.transform.SetPositionAndRotation(h.frame.P(0f, 0f, 0f) + Vector3.up * (yMid - h.floorY), Quaternion.LookRotation(h.frame.F, Vector3.up));
                go.AddComponent<BoxCollider>().size = new Vector3(h.W, hh - (h.baseY - h.floorY), h.D);
                // the mailbox at the street, beside the drive (the map's own drives are kept clear)
                Vector3 s3 = h.frame.P((h.garageU0 + h.garageU1) / 2f, 0f, h.D / 2); var S = new Vector2(s3.x, s3.z);
                var q = h.nearestRoadPoint; Vector2 toHouse = (S - q).normalized; if (toHouse.sqrMagnitude < .5f) toHouse = new Vector2(h.frame.F.x, h.frame.F.z) * -1f;
                Vector2 tan = new Vector2(-toHouse.y, toHouse.x) * (Hash01(h.seed, 130) < 0.5f ? 1f : -1f);
                Vector2 mp = q + toHouse * (h.roadHalf + 1.2f) + tan * 3.6f;
                bool onDrive = drives.Any(dd => (dd.a - mp).sqrMagnitude < 900f && SegDistStatic(mp, dd.a, dd.b) < 2.2f);
                if (!onDrive && !roads.Covers(mp, NoRoad, 0.8f))
                {
                    float gy = terrain.Height(mp.x, mp.y);
                    var mmb = mailboxes.Begin(mp);
                    Color post = Hash01(h.seed, 131) < 0.6f ? new Color(0.92f, 0.92f, 0.9f) : new Color(0.3f, 0.22f, 0.15f);
                    mailboxes.Piece(() => Post(mmb, new Vector3(mp.x, gy - 0.05f, mp.y), 1.05f, 0.1f, post));
                    var face = -toHouse; var a = new Vector3(mp.x - face.x * 0.24f, gy + 1.17f, mp.y - face.y * 0.24f); var bb = new Vector3(mp.x + face.x * 0.24f, gy + 1.17f, mp.y + face.y * 0.24f);
                    mailboxes.Piece(() => Beam(mmb, a, bb, 0.2f, 0.22f, Hash01(h.seed, 132) < 0.5f ? new Color(0.12f, 0.12f, 0.13f) : new Color(0.55f, 0.57f, 0.6f)));
                    mailboxes.End(new Vector3(mp.x, gy + 0.7f, mp.y), new Vector3(face.x, 0, face.y), 0.6f, 1.4f);
                    mail++;
                }
                // one street view per neighborhood for the pictures (the first house of each)
                int n = rest.Of[h.index];
                if (shown.Add(n))
                {
                    var eye = q + (q - h.footprintCentre).normalized * 2f + tan * 6f; var look = h.footprintCentre - eye;
                    float yaw = Mathf.Atan2(look.x, look.y) * Mathf.Rad2Deg; var nb = rest.Data.neighborhoods[n];
                    views.AppendLine($"{nb.key}\t{nb.name}\t{nb.era}\t{eye.x:0.0}\t{eye.y:0.0}\t{yaw:0}\t{nb.x:0}\t{nb.z:0}");
                }
            }
            foreach (var kv in chunks) kv.Value.ToObject($"Houses_{kv.Key.x}_{kv.Key.y}", mats.House, hroot, false);
            foreach (var kv in detail) kv.Value.ToObject($"HouseDetail_{kv.Key.x}_{kv.Key.y}", mats.House, hroot, false, false);
            mailboxes.ToObjects("Mailboxes", mats.House, root, 1.25f, BreakSounds.Kind.Mailbox);
            File.WriteAllText(Path.Combine(ProjectRoot, GeneratedDir, "neighborhood_views.tsv"), "key\tname\tera\tx\tz\tyaw\tcx\tcz\n" + views);
            Debug.Log($"[Round30] built {rest.Houses.Count} houses in {chunks.Count} chunks, {mail} mailboxes, {rest.Fences.Count} back-yard fences; street views in {GeneratedDir}/neighborhood_views.tsv");
        }
    }
}
