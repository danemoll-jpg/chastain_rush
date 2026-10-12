using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Round 7: the Chastain Glen area, planned from data. Nothing here changes the generated map data; the area itself is
// listed in Data/neighborhood/chastain_glen_area.json (Tools/mapgen/neighborhood_area.py) and its small features (pool,
// parking, playground, fountain) in Data/neighborhood/extras.json (Tools/mapgen/neighborhood_extras.py). Houses, drives,
// fences and keep-out zones for the trees are worked out once, here, so the builder and the tree placement agree.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class AreaJson { public int[] roads; public int[] buildings; public int[] houses; public int[] other_buildings; public int owner_index; public PondRef[] ponds; }
        [Serializable] class PondRef { public int index; public string name; public float[] centre; public float area_m2; }
        [Serializable] class PolyItem { public long osm_id; public float[] outline; }
        [Serializable] class PointItem { public long osm_id; public float x; public float z; }
        [Serializable] class ExtrasJson { public PolyItem pool; public PolyItem parking; public PolyItem playground; public PointItem fountain; public PointItem gate; }

        enum FenceKind { Privacy, Picket, Rail, Iron }
        sealed class FenceRun { public FenceKind kind; public List<Vector2> pts = new List<Vector2>(); public int seed; public bool closed; }

        struct RoadSeg { public Vector2 a, b; public float half; public int road; }

        sealed partial class Neighborhood
        {
            public AreaJson Area;
            public ExtrasJson Extras;
            public bool Present;
            public readonly HashSet<int> HouseSet = new HashSet<int>();
            public readonly List<HouseSpec> Houses = new List<HouseSpec>();
            public HouseSpec Dan;
            public Vector2? DanMailbox;   // Round 11 (BUG-006): where his mailbox went (the hydrant keeps clear of it)
            public readonly HashSet<int> DriveSet = new HashSet<int>();
            public readonly List<RoadSeg> Segs = new List<RoadSeg>();
            public readonly List<FenceRun> Fences = new List<FenceRun>();
            public int PondIndex = -1;
            public List<Vector2> PondRing = new List<Vector2>();
            public Vector2 FountainAt;
            public List<Vector2> PoolRing, ParkingRing, PlaygroundRing;
            public Vector2 PoolCentre; public float PoolDeckHalfU, PoolDeckHalfV; public Vector2 PoolU, PoolV;
            public AreaBounds Bounds;   // Round 22: Chastain Glen's rectangle and one per community (was one Rect)
            public Neighborhood Self => this;
            readonly List<(Vector2 a, Vector2 b)> fenceSegs = new List<(Vector2, Vector2)>();
            readonly List<(Vector2 c, float r)> discs = new List<(Vector2, float)>();   // extra keep-outs (docks, walls, lamps, signs)
            MapData map; TerrainGrid terrain;

            readonly HashSet<int> roadSet = new HashSet<int>();
            public bool IsAreaRoad(int i) => Present && roadSet.Contains(i);
            public bool OwnsBuilding(int i) => Present && (HouseSet.Contains(i) || Replaced.Contains(i));   // Round 9: and the pavilion, now a gazebo
            public bool OwnsDriveway(int i) => Present && (DriveSet.Contains(i) || i == PoolRoadIndex);   // Round 11: and the road into the pool's lot
            public bool OwnsPond(int waterIndex) => Present && waterIndex == PondIndex;
            public void KeepOut(Vector2 c, float r) => discs.Add((c, r));

            // ---------------------------------------------------------------- load

            public static Neighborhood Load(MapData map, TerrainGrid terrain)
            {
                var n = new Neighborhood { map = map, terrain = terrain };
                string areaPath = Path.Combine(ProjectRoot, "Data/neighborhood/chastain_glen_area.json");
                string extraPath = Path.Combine(ProjectRoot, "Data/neighborhood/extras.json");
                if (!File.Exists(areaPath)) { Debug.LogWarning("[Neighborhood] no area file (run Tools/mapgen/neighborhood_area.py); the map is built as before"); return n; }
                n.Area = JsonUtility.FromJson<AreaJson>(File.ReadAllText(areaPath));
                n.Extras = File.Exists(extraPath) ? JsonUtility.FromJson<ExtrasJson>(File.ReadAllText(extraPath)) : new ExtrasJson();
                n.Present = true;
                n.LoadCommunities();   // Round 22: Grasmere Court, Chastain Commons and Chastain Enclave join the area (MapBuilder.Communities.cs)
                n.Plan();
                n.LoadShortcuts();   // after Plan: the corridor leaves out the stretches of the line that run on a street
                return n;
            }

            // ---------------------------------------------------------------- roads

            public bool Nearest(Vector2 p, out Vector2 q, out float half, out Vector2 tangent, out float dist, bool byEdge = true)
            {
                q = p; half = 4f; tangent = Vector2.right; dist = float.MaxValue;
                float best = float.MaxValue;
                foreach (var s in Segs)
                {
                    var ab = s.b - s.a;
                    float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                    var c = s.a + ab * t;
                    float d = (c - p).magnitude;
                    // Round 11: the nearest pavement's EDGE (a cul-de-sac's bulb is nearer than the end of its road's centreline)
                    float key = byEdge ? d - s.half : d;   // (the houses face their streets as before: by the centreline)
                    if (key >= best) continue;
                    best = key;
                    // Round 11: a cul-de-sac bulb is a point with a radius: along its edge is round it
                    dist = d; q = c; half = s.half; tangent = ab.sqrMagnitude < 1e-6f ? (d > 1e-4f ? new Vector2(-(p - c).y, (p - c).x) / d : Vector2.right) : ab.normalized;
                }
                return dist < float.MaxValue;
            }

            public bool FenceOk(Vector2 p) => Clear(p, null);
            public bool OnPoolOrLot(Vector2 p)
            {
                if (InRing(p, ParkingRing) || InRing(p, PlaygroundRing)) return true;
                if (PoolDeckHalfU <= 0f) return false;
                var d = p - PoolCentre;
                return Mathf.Abs(Vector2.Dot(d, PoolU)) < PoolDeckHalfU + 3f && Mathf.Abs(Vector2.Dot(d, PoolV)) < PoolDeckHalfV + 3f;
            }
            public float RoadClearance(Vector2 p) { Nearest(p, out _, out float half, out _, out float d); return d - half; }

            // ---------------------------------------------------------------- planning

            void Plan()
            {
                foreach (int ri in Area.roads)
                {
                    roadSet.Add(ri);
                    var r = map.roads[ri]; var pts = ToPoints(r.points);
                    for (int i = 0; i + 1 < pts.Count; i++) Segs.Add(new RoadSeg { a = pts[i], b = pts[i + 1], half = r.width * 0.5f, road = ri });
                }
                if (Area.ponds != null && Area.ponds.Length > 0) { PondIndex = Area.ponds[0].index; PondRing = ToPoints(map.water[PondIndex].outline); }
                if (Extras.fountain != null && Extras.fountain.x != 0f) FountainAt = new Vector2(Extras.fountain.x, Extras.fountain.z);
                else if (PondRing.Count > 0) { Vector2 c = Vector2.zero; foreach (var p in PondRing) c += p; FountainAt = c / PondRing.Count; }
                if (Extras.pool != null && Extras.pool.outline != null) PoolRing = ToPoints(Extras.pool.outline);
                if (Extras.parking != null && Extras.parking.outline != null) ParkingRing = ToPoints(Extras.parking.outline);
                if (Extras.playground != null && Extras.playground.outline != null) PlaygroundRing = ToPoints(Extras.playground.outline);
                float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                foreach (var s in Segs) { if (communityRoadSet.Contains(s.road)) continue; x0 = Mathf.Min(x0, s.a.x, s.b.x); x1 = Mathf.Max(x1, s.a.x, s.b.x); z0 = Mathf.Min(z0, s.a.y, s.b.y); z1 = Mathf.Max(z1, s.a.y, s.b.y); }
                foreach (var p in PondRing) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.y); z1 = Mathf.Max(z1, p.y); }
                if (PoolRing != null) foreach (var p in PoolRing) { x0 = Mathf.Min(x0, p.x); z0 = Mathf.Min(z0, p.y); }
                Bounds = new AreaBounds(Rect.MinMaxRect(x0 - 40f, z0 - 40f, x1 + 40f, z1 + 40f));
                AddCommunityBounds();   // Round 22: each community's own rectangle (the streets between them are not part of it)
                PlanPool();
                PlanPoolRoad();    // Round 11 (BUG-010): the way from Grasmere Court into the pool's parking lot is a road
                PlanHouses();
                PlanStreets();     // Round 11: junction corners and cul-de-sac bulbs (after the houses face their streets, before the drives)
                PlanDrives();      // Round 9: before the trees, so none is left standing on a drive
                ResolveDriveOverlaps();   // Round 11: no two drives overlap
                PlanGazebo();
                PlanCommunities();   // Round 22: the Enclave's pool and courts, the communities' monuments (MapBuilder.Communities.cs)
                PlanDriveOwnership();
                PlanFences();
            }

            void PlanHouses()
            {
                var owner = LoadOwner();
                foreach (int idx in Area.houses)
                {
                    var b = map.buildings[idx];
                    var ring = ToPoints(b.footprint);
                    if (ring.Count < 3 || !BestRectangle(ring, out Vector2 c, out Vector2 U, out Vector2 V, out float hl, out float hw)) continue;
                    float area = Mathf.Abs(PolyArea(ring));
                    int community = CommunityOf(idx);
                    if (community == EnclaveCommunity) GrowEnclaveHouse(idx, ref ring, c, U, V, ref hl, ref hw);   // Round 22: larger houses where the lot allows
                    Nearest(c, out Vector2 q, out float roadHalf, out Vector2 tan, out _, false);
                    Vector2 toRoad = (q - c).normalized;
                    Vector2[] cand = { U, -U, V, -V };
                    Vector2 F = cand[0]; float best = -2f;
                    foreach (var cd in cand) { float d = Vector2.Dot(cd, toRoad); if (d > best) { best = d; F = cd; } }
                    bool facadeAlongU = Mathf.Abs(Vector2.Dot(F, U)) < 0.7f;
                    float W = facadeAlongU ? hl * 2f : hw * 2f, D = facadeAlongU ? hw * 2f : hl * 2f;
                    var h = new HouseSpec { index = idx, seed = b.seed, ring = ring, footprintCentre = c, W = W, D = D, nearestRoadPoint = q, roadHalf = roadHalf };
                    h.community = community;
                    h.style = community == 0 ? ChooseStyle(area, Mathf.Max(W, D), Mathf.Min(W, D), b.seed) : CommunityStyle(community, area, Mathf.Max(W, D), Mathf.Min(W, D), b.seed);   // Round 22
                    ChooseColours(h);
                    if (community != 0) DressForCommunity(h);   // Round 22: brick fronts and three-car garages in the Enclave, Commons' own colors
                    h.dan = owner != null && owner.index == idx;
                    if (h.dan) ApplyDan(h);
                    // mirror so the garage is on the side nearest the road point
                    var Rv = Vector3.Cross(Vector3.up, -new Vector3(F.x, 0f, F.y));
                    float uR = Vector2.Dot(q - c, new Vector2(Rv.x, Rv.z));
                    float M = h.dan ? 1f : uR > 0f ? -1f : 1f;   // Dan's garage is on the observer's left in the photos
                    h.frame = MakeFrame(new Vector3(c.x, 0f, c.y), F, M);
                    GarageSpan(h);
                    // floor level: the ground at the foot of the garage doors, a little above; walls go down to the lowest corner
                    Vector3 g = h.frame.P((h.garageU0 + h.garageU1) / 2f, 0f, h.D / 2f + 0.4f);
                    float lo = float.MaxValue;
                    foreach (var p in ring) lo = Mathf.Min(lo, terrain.Height(p.x, p.y));
                    foreach (float uu in new[] { -W / 2, W / 2 }) { var pc = h.frame.P(uu, 0f, D / 2 + 0.3f); lo = Mathf.Min(lo, terrain.Height(pc.x, pc.z)); }
                    h.floorY = terrain.Height(g.x, g.z) + 0.16f;
                    h.baseY = Mathf.Min(lo, h.floorY) - 0.45f;
                    h.frame.O = new Vector3(c.x, h.floorY, c.y);
                    h.garageCentre = new Vector3(g.x, h.floorY, g.z);
                    Houses.Add(h);
                    HouseSet.Add(idx);
                    if (h.dan) Dan = h;
                }
            }

            // oriented footprint test: inside house rectangles grown by 'grow'
            bool InsideHouse(Vector2 p, float grow, HouseSpec except)
            {
                foreach (var h in Houses)
                {
                    if (h == except) continue;
                    var d = p - h.footprintCentre;
                    if (d.sqrMagnitude > 400f) continue;
                    var f = new Vector2(h.frame.F.x, h.frame.F.z);
                    var r = new Vector2(h.frame.R.x, h.frame.R.z);
                    if (Mathf.Abs(Vector2.Dot(d, r)) < h.W / 2 + grow && Mathf.Abs(Vector2.Dot(d, f)) < h.D / 2 + grow) return true;
                }
                return false;
            }

            public static bool InRing(Vector2 p, List<Vector2> ring) => ring != null && ring.Count > 2 && InPolygon(p, ring);

            float DistToRing(Vector2 p, List<Vector2> ring)
            {
                float best = float.MaxValue;
                for (int i = 0; i < ring.Count; i++)
                {
                    var a = ring[i]; var b = ring[(i + 1) % ring.Count]; var ab = b - a;
                    float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    best = Mathf.Min(best, (a + ab * t - p).magnitude);
                }
                return best;
            }
            public float DistToPond(Vector2 p) => PondRing.Count > 2 ? (InPolygon(p, PondRing) ? 0f : DistToRing(p, PondRing)) : float.MaxValue;

            bool Clear(Vector2 p, HouseSpec except)
            {
                if (InsideHouse(p, 1.2f, except)) return false;
                if (RoadClearance(p) < 1.6f) return false;
                if (PondRing.Count > 2 && (InPolygon(p, PondRing) || DistToRing(p, PondRing) < 1.2f)) return false;
                if (InRing(p, PoolRing) || InRing(p, ParkingRing) || InRing(p, PlaygroundRing)) return false;
                if (AmenityBlocks(p, 1.5f)) return false;   // Round 22: the Enclave's pool, courts, pavilion and lot
                if (PoolRing != null && PoolDeckHalfU > 0f)
                {
                    var d = p - PoolCentre;
                    if (Mathf.Abs(Vector2.Dot(d, PoolU)) < PoolDeckHalfU + 2f && Mathf.Abs(Vector2.Dot(d, PoolV)) < PoolDeckHalfV + 2f) return false;
                }
                return true;
            }

            void PlanPool()
            {
                if (PoolRing == null || PoolRing.Count < 3) return;
                if (!BestRectangle(PoolRing, out Vector2 c, out Vector2 U, out Vector2 V, out float hl, out float hw)) return;
                PoolCentre = c; PoolU = U; PoolV = V; PoolDeckHalfU = hl + 3.2f; PoolDeckHalfV = hw + 3.2f;
                // Round 12 (BUG-001, Dan: "the big building shoulding exist and the small building should be in its place"): the
                // large generic building beside the pool (the nearest non-house building to his report position, not the
                // gazebo's pavilion) is not built; the small clubhouse stands on its footprint, its porch toward the pool
                float best = 20f;
                for (int i = 0; i < map.buildings.Length; i++)
                {
                    var b = map.buildings[i];
                    if (b.type == "pavilion" || HouseSet.Contains(i)) continue;
                    var ring = ToPoints(b.footprint); if (ring.Count < 3) continue;
                    Vector2 cc = Vector2.zero; foreach (var p in ring) cc += p; cc /= ring.Count;
                    float d = (cc - Bug001Pool).magnitude;
                    if (d < best) { best = d; ClubhouseBuilding = i; }
                }
                if (ClubhouseBuilding < 0) return;
                Replaced.Add(ClubhouseBuilding);
                BestRectangle(ToPoints(map.buildings[ClubhouseBuilding].footprint), out var bc, out var bu, out var bv, out _, out _);
                ClubhouseAt = bc;
                var toPool = (PoolCentre - bc).normalized;
                ClubhouseFront = new[] { bu, -bu, bv, -bv }.OrderByDescending(a => Vector2.Dot(a, toPool)).First();
            }
            public int ClubhouseBuilding = -1;
            public Vector2? ClubhouseAt; public Vector2 ClubhouseFront;

            void PlanDriveOwnership()
            {
                // OSM / generated driveways that start or end at one of the area's houses are replaced by the house's own
                for (int i = 0; i < map.driveways.Length; i++)
                {
                    var pts = map.driveways[i].points;
                    if (pts.Length < 4) continue;
                    var s = new Vector2(pts[0], pts[1]); var e = new Vector2(pts[pts.Length - 2], pts[pts.Length - 1]);
                    foreach (var h in Houses)
                    {
                        float reach = Mathf.Max(h.W, h.D) * 0.5f + 3.5f;
                        if ((s - h.footprintCentre).magnitude < reach || (e - h.footprintCentre).magnitude < reach) { DriveSet.Add(i); break; }
                    }
                }
            }

            // ---------------------------------------------------------------- fences
            // No data says where real fences are, so they are placed plausibly along rear and side lot lines with a stable hash:
            // about 60% of the lots get a wooden privacy fence round the back (sides too, about half of them), the lots whose
            // back is on the pond get a low picket fence instead, and a white three-rail fence follows the road on the pond
            // side. The pool gets a black iron fence. A segment is dropped wherever it would cross a house, road, drive or water.

            // Round 8: fixed exceptions to the hashed placement, from Dan (2026-10-09): no fence on his own lot, a fence for
            // the neighbour on his right (seen from the street) and for the lot behind him at the top of the hill. They are
            // stored with the owner data in local/owner_fences.json (git-ignored: it locates his house), by house position:
            //   {"no_fence": [{"x": .., "z": ..}], "fence": [{"x": .., "z": ..}, ...]}   (each matched to the nearest area house within 8 m)
            [Serializable] class FencePoint { public float x, z; public string note; }
            [Serializable] class FenceExceptions { public FencePoint[] no_fence; public FencePoint[] fence; }
            public readonly HashSet<int> NoFence = new HashSet<int>(), ForceFence = new HashSet<int>();

            void LoadFenceExceptions()
            {
                if (Dan != null) NoFence.Add(Dan.index);   // Dan has no fence even without the file
                string path = Path.Combine(ProjectRoot, "local/owner_fences.json");
                if (!File.Exists(path)) { Debug.Log("[Neighborhood] no local/owner_fences.json: only Dan's own lot is an exception to the fence placement"); return; }
                var ex = JsonUtility.FromJson<FenceExceptions>(File.ReadAllText(path));
                HouseSpec Match(FencePoint fp)
                {
                    HouseSpec best = null; float bd = 8f;
                    foreach (var h in Houses) { float d = (h.footprintCentre - new Vector2(fp.x, fp.z)).magnitude; if (d < bd) { bd = d; best = h; } }
                    if (best == null) Debug.LogWarning($"[Neighborhood] owner_fences.json: no area house within 8 m of ({fp.x:0.0}, {fp.z:0.0})");
                    return best;
                }
                if (ex.no_fence != null) foreach (var fp in ex.no_fence) { var h = Match(fp); if (h != null) NoFence.Add(h.index); }
                if (ex.fence != null) foreach (var fp in ex.fence) { var h = Match(fp); if (h != null) { ForceFence.Add(h.index); NoFence.Remove(h.index); } }
                Debug.Log($"[Neighborhood] fence exceptions from local/owner_fences.json: {NoFence.Count} lot(s) without a fence, {ForceFence.Count} with one");
            }

            void PlanFences()
            {
                LoadFenceExceptions();
                foreach (var h in Houses)
                {
                    bool forced = ForceFence.Contains(h.index);
                    if (NoFence.Contains(h.index)) continue;
                    if (Hash01(h.seed, 71) > 0.62f && !forced) continue;
                    var f = new Vector2(h.frame.F.x, h.frame.F.z); var r = new Vector2(h.frame.R.x, h.frame.R.z);
                    Vector2 P(float u, float v) => h.footprintCentre + r * u + f * v;
                    // side lines midway to the neighbours (or a default), measured along the facade
                    float sideL = h.W / 2 + 3.4f, sideR = h.W / 2 + 3.4f;
                    foreach (var o in Houses)
                    {
                        if (o == h) continue;
                        var d = o.footprintCentre - h.footprintCentre;
                        if (d.sqrMagnitude > 900f) continue;
                        float along = Vector2.Dot(d, f), across = Vector2.Dot(d, r);
                        if (Mathf.Abs(along) > h.D) continue;
                        float half = Mathf.Abs(across) / 2f;
                        if (across > 0f) sideR = Mathf.Min(sideR, Mathf.Max(half, h.W / 2 + 1.2f)); else sideL = Mathf.Min(sideL, Mathf.Max(half, h.W / 2 + 1.2f));
                    }
                    float rear = 0f;
                    for (float depth = 14f; depth >= 5f; depth -= 1f)
                    {
                        bool ok = true;
                        for (int k = 0; k <= 8 && ok; k++)
                            ok = Clear(P(Mathf.Lerp(-sideL, sideR, k / 8f), -h.D / 2 - depth), h);
                        if (ok) { rear = depth; break; }
                    }
                    if (rear <= 0f) continue;
                    var rearC = P(0f, -h.D / 2 - rear);
                    bool pond = DistToPond(rearC) < 22f;
                    FenceKind kind = pond ? FenceKind.Picket : FenceKind.Privacy;
                    if (pond && Hash01(h.seed, 72) > 0.55f && !forced) continue;                  // a few lots by the pond, not all
                    var run = new FenceRun { kind = kind, seed = h.seed };
                    bool sideLeft = Hash01(h.seed, 73) < 0.5f, sideRight = Hash01(h.seed, 74) < 0.5f;
                    if (pond || forced) { sideLeft = sideRight = true; }   // a fixed exception gets the rear and both sides
                    float vSide = h.D / 2 - 1.5f;
                    if (sideLeft) run.pts.Add(P(-sideL, vSide));
                    if (sideLeft) run.pts.Add(P(-sideL, -h.D / 2 - rear)); else run.pts.Add(P(-sideL, -h.D / 2 - rear));
                    run.pts.Add(P(sideR, -h.D / 2 - rear));
                    if (sideRight) run.pts.Add(P(sideR, vSide));
                    Fences.Add(run);
                }
                foreach (var h in Houses)
                    if (ForceFence.Contains(h.index) || NoFence.Contains(h.index))
                    {
                        var run = Fences.Find(f => f.seed == h.seed && (f.kind == FenceKind.Privacy || f.kind == FenceKind.Picket));
                        Debug.Log($"[Neighborhood] fence exception, house {h.index}: " + (run != null ? $"{run.kind} fence, {run.pts.Count - 1} side(s)" : "no fence") + (ForceFence.Contains(h.index) && run == null ? " (WANTED one, but no clear rear line was found)" : ""));
                    }
                PlanPoolStreet();   // Round 9: the sidewalk, benches, pool fence and monuments on New Chastain Road
                AddRoadSideRail();
                AddPoolFence();
                foreach (var run in Fences) for (int i = 0; i + 1 < run.pts.Count; i++) fenceSegs.Add((run.pts[i], run.pts[i + 1]));
            }

            // white three-rail fence along the road on the pond side (the entrance spur and the lane past the pond)
            void AddRoadSideRail()
            {
                if (PondRing.Count < 3) return;
                foreach (int ri in Area.roads)
                {
                    var r = map.roads[ri]; var pts = Densify(ToPoints(r.points), 2f);
                    if (pts.Count < 3) continue;
                    foreach (float side in new[] { 1f, -1f })
                    {
                        var run = new FenceRun { kind = FenceKind.Rail, seed = ri };
                        for (int i = 0; i < pts.Count; i++)
                        {
                            Vector2 dirIn = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                            Vector2 dirOut = i < pts.Count - 1 ? (pts[i + 1] - pts[i]).normalized : dirIn;
                            Vector2 t = (dirIn + dirOut).normalized; if (t.sqrMagnitude < 1e-6f) t = dirOut;
                            Vector2 nrm = new Vector2(-t.y, t.x) * side;
                            Vector2 p = pts[i] + nrm * (r.width * 0.5f + 4.2f);
                            bool keep = (DistToPond(p) < 15f || RailExtension(r, p)) && !InPolygon(p, PondRing) && RoadClearanceExcept(p, ri) > 3.2f && !InsideHouse(p, 2f, null) && !InRing(p, ParkingRing) && !(PoolRing != null && DistToRing(p, PoolRing) < 6f)
                                        && (PoolWalk == null || DistToLine(p, PoolWalk) > 1.7f);   // Round 11: a gap where the lake walk comes through
                            if (keep) run.pts.Add(p);
                            else { if (run.pts.Count >= 3) Fences.Add(run); run = new FenceRun { kind = FenceKind.Rail, seed = ri }; }
                        }
                        if (run.pts.Count >= 3) Fences.Add(run);
                    }
                }
            }

            float RoadClearanceExcept(Vector2 p, int except)
            {
                float best = float.MaxValue;
                foreach (var s in Segs)
                {
                    if (s.road == except) continue;
                    var ab = s.b - s.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                    best = Mathf.Min(best, (s.a + ab * t - p).magnitude - s.half);
                }
                return best;
            }

            void AddPoolFence()
            {
                if (PoolRing == null || PoolDeckHalfU <= 0f) return;
                float hu = PoolDeckHalfU + 0.8f, hv = PoolDeckHalfV + 0.8f;
                var run = new FenceRun { kind = FenceKind.Iron, seed = 5, closed = true };
                run.pts.Add(PoolCentre + PoolU * hu + PoolV * hv); run.pts.Add(PoolCentre - PoolU * hu + PoolV * hv);
                run.pts.Add(PoolCentre - PoolU * hu - PoolV * hv); run.pts.Add(PoolCentre + PoolU * hu - PoolV * hv);
                run.pts.Add(run.pts[0]);
                Fences.Add(run);
            }

            // ---------------------------------------------------------------- tree keep-outs

            public bool RejectTree(float x, float z)
            {
                if (!Present) return false;
                var p = new Vector2(x, z);
                if (!Bounds.Contains(p)) return false;
                if (InCorridor(p)) { TreesCleared++; return true; }   // Round 8: nothing grows on a shortcut's line
                return TreeBlocked(p);
            }

            bool TreeBlocked(Vector2 p)
            {
                // not through the curb, grass strip and sidewalk of the area's streets
                foreach (var s in Segs)
                {
                    var ab = s.b - s.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                    if ((s.a + ab * t - p).magnitude < s.half + 3.6f) return true;
                }
                foreach (var h in Houses) { if ((p - h.footprintCentre).sqrMagnitude < 400f && InsideHouse(p, 0.8f, null)) return true; }
                foreach (var seg in fenceSegs)
                {
                    var ab = seg.b - seg.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - seg.a, ab) / ab.sqrMagnitude);
                    if ((seg.a + ab * t - p).magnitude < 1.0f) return true;
                }
                if (PoolDeckHalfU > 0f)
                {
                    var d = p - PoolCentre;
                    if (Mathf.Abs(Vector2.Dot(d, PoolU)) < PoolDeckHalfU + 3.5f && Mathf.Abs(Vector2.Dot(d, PoolV)) < PoolDeckHalfV + 3.5f) return true;
                }
                if (InRing(p, ParkingRing) || InRing(p, PlaygroundRing)) return true;
                if (AmenityBlocks(p, 2.5f)) return true;   // Round 22
                if (PondRing.Count > 2 && DistToPond(p) < 2.5f) return true;
                if (InCorridor(p)) return true;
                if (OnDrivePlan(p, 0.4f)) return true;                                              // Round 9: never on a drive
                if (PoolWalk != null && Neighborhood.DistToLine(p, PoolWalk) < 1.6f) return true;   // Round 9: nor on the new sidewalk
                if (PoolRoad != null && Neighborhood.DistToLine(p, PoolRoad) < PoolRoadWidth * 0.5f + 1.2f) return true;   // Round 11: nor on the pool's road
                foreach (var dsc in discs) if ((p - dsc.c).sqrMagnitude < dsc.r * dsc.r) return true;
                return false;
            }
        }
    }
}
