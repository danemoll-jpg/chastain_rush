using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 31 (4) (Dan: "I want to have the buildings that aren't part of a neighborhood also dressed up. Much of Canton Road
// would be businesses or schools"; 2026-10-10: "Store names can be faked"). Every building outside the dressed
// neighborhoods, the Glen's area, the communities and the shopping center gets a look from what OSM says it is:
//   Shop        retail, commercial, and untagged buildings within 90 m of Canton Road (small businesses): storefront glass,
//               doors, an awning and a sign per unit (a strip of units along a long front), a parapet, rooftop units.
//   Office      office, and other untagged buildings under 1500 m2: window bands per floor, a glass entrance under a canopy,
//               the name on a board over the parapet.
//   Industrial  industrial, warehouse, and untagged buildings of 1500 m2 or more: ribbed metal walls, roll-up doors, an
//               office door with the sign, a strip of high windows.
//   Church      tall windows, double doors, the sign over them, a steeple at the front.
//   School      brick, rows of windows, a canopy over the doors, the sign, a flagpole.
//   Civic       the fire station: bay doors and its sign.
//   Apartments  apartments and the cross-shaped "terrace" blocks (and the few houses the house kit could not take): windows
//               per floor, a door every 7 m along the front.
//   Garage / Shed / Canopy   garages get their doors, sheds a door, roofs / carports / pavilions stand on posts.
// The front is the face that looks most toward the nearest parking lot or road. Bodies are settled on the ground as the
// houses are (Round 27): the floor at the front's ground, raised over the ground where it falls away (a block foundation),
// never more than 1.2 m under it at the back; a long rectangular building on a slope (BUG-002's garage row) is stepped into
// segments, each on its own ground. Every surface parking lot OSM maps outside the dressed areas is paved, with curbs, stall
// lines and light poles (breakable, as the street lamps); a few trees round the buildings and lots. Signs: every name made up
// (Tools/handmade/make_business_signs.py draws Assets/HandMade/Businesses/BusinessSigns.png), one-sided, lit and depth-tested,
// each fitted to its board. Batching as the neighborhoods: bodies in 128 m chunks with colliders, the details in shadowless
// chunks without.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        enum BizKind { Shop, Office, Industrial, Church, School, Civic, Apartments, Garage, Shed, Canopy }

        // rows of BusinessSigns.png per kind (make_business_signs.py's KINDS, in order)
        static readonly Dictionary<BizKind, (int first, int count)> BizSignRows = new Dictionary<BizKind, (int, int)>
        {
            { BizKind.Shop, (0, 20) }, { BizKind.Office, (20, 10) }, { BizKind.Industrial, (30, 8) }, { BizKind.Church, (38, 4) },
            { BizKind.School, (42, 2) }, { BizKind.Civic, (45, 1) }, { BizKind.Apartments, (46, 3) },
        };
        const int SignPreschool = 44;

        sealed class Biz
        {
            public int index, seed; public BizKind kind; public string type;
            public List<Vector2> ring; public Vector2 c; public float area, height, pitch; public bool pitched;
            public int front = -1; public Vector2 frontN;
            public bool canton;                                     // Round 32 (A1): faces Canton Road (MapBuilder.Round32.cs)
            public float floorY, baseY;
            public List<(List<Vector2> ring, float floor)> steps;   // a stepped body (BUG-002), else null
        }

        sealed class Businesses
        {
            public readonly List<Biz> Items = new List<Biz>();
            public readonly HashSet<int> Owned = new HashSet<int>();
            public readonly List<List<Vector2>> Lots = new List<List<Vector2>>();
            public readonly List<Vector2> Trees = new List<Vector2>();
            public readonly List<List<Vector2>> NewLots = new List<List<Vector2>>(), Walks = new List<List<Vector2>>();   // Round 32 (A1)
            public readonly List<(Vector2 a, Vector2 b, float half)> Drives = new List<(Vector2, Vector2, float)>();
            public (Vector2 a, Vector2 b, Vector2 n)? PizzaLane;
            public bool Owns(int i) => Owned.Contains(i);
            public bool InNewLot(Vector2 p)
            {
                if (newIdx == null || newIdxN != NewLots.Count) { newIdx = new CellIndex(64f); newIdxN = NewLots.Count; foreach (var lot in NewLots) newIdx.Add(lot, 0.05f); }
                foreach (int i in newIdx.Near(p, 0f, nearNew)) if (InPolygon(p, NewLots[i])) return true; return false;
            }
            // build speed (after Round 61): the lots, walks and drives by cell (filed again whenever one is added: the lists only grow), each
            // padded by its distance, so only those that can answer are asked; called for every tree on the map
            CellIndex rejIdx, newIdx; int rejL = -1, rejW = -1, rejD = -1, newIdxN = -1; readonly List<int> nearRej = new List<int>(), nearNew = new List<int>();
            public bool RejectTree(float x, float z)
            {
                var p = new Vector2(x, z);
                if (rejIdx == null || rejL != Lots.Count || rejW != Walks.Count || rejD != Drives.Count)
                {
                    rejIdx = new CellIndex(32f); rejL = Lots.Count; rejW = Walks.Count; rejD = Drives.Count;
                    foreach (var lot in Lots) rejIdx.Add(lot, 1.05f);
                    foreach (var w in Walks) rejIdx.Add(w, 1.05f);
                    foreach (var d in Drives) rejIdx.Add(d.a, d.b, d.half + 1.55f);
                }
                foreach (int i in rejIdx.Near(p, 0f, nearRej))
                {
                    if (i < rejL) { var lot = Lots[i]; if (InPolygon(p, lot) || RingDist(p, lot) < 1.0f) return true; }
                    else if (i < rejL + rejW) { var w = Walks[i - rejL]; if (InPolygon(p, w) || RingDist(p, w) < 1.0f) return true; }
                    else { var d = Drives[i - rejL - rejW]; if (SegDist(p, d.a, d.b) < d.half + 1.5f) return true; }
                }
                return false;
            }
            public readonly StringBuilder Log = new StringBuilder();
        }

        static Businesses PlanBusinesses(MapData map, TerrainGrid t, Neighborhood hood, ShoppingCenter shop, RestNeighborhoods rest)
        {
            var biz = new Businesses();
            // roads (no motorways) and lots, for the fronts
            var segs = new List<(Vector2 a, Vector2 b, float half)>();
            var canton = new List<(Vector2 a, Vector2 b)>();
            var cantonHalf = new List<(Vector2 a, Vector2 b, float half)>(); var cross = new List<(Vector2 a, Vector2 b)>();   // Round 32 (A1)
            foreach (var r in map.roads)
            {
                if (r.@class == "motorway" || r.@class == "motorway_link") continue;
                var p = ToPoints(r.points); bool isCanton = (r.name ?? "").StartsWith("Canton Road");
                for (int i = 0; i + 1 < p.Count; i++)
                {
                    segs.Add((p[i], p[i + 1], r.width * 0.5f));
                    if (isCanton) { canton.Add((p[i], p[i + 1])); cantonHalf.Add((p[i], p[i + 1], r.width * 0.5f)); }
                    else if (r.@class != "service") cross.Add((p[i], p[i + 1]));
                }
            }
            // the lots: OSM's surface parking outside the dressed areas (the Glen's pool lot, the Enclave's) and off the shopping center's
            if (map.parking != null)
                foreach (var a in map.parking)
                {
                    var ring = ToPoints(a.outline); if (ring.Count < 3) continue;
                    Vector2 c = Vector2.zero; foreach (var p in ring) c += p; c /= ring.Count;
                    if ((hood.Bounds != null && hood.Bounds.Contains(c)) || InPolygon(c, shop.Lot) || hood.RejectTree(c.x, c.y) || CoursonLotAt(c)) { biz.Log.Append($"lot at ({c.x:0}, {c.y:0}) left to its dressed area; "); continue; }
                    bool nearOwned = false;
                    for (int i = 0; i < map.buildings.Length && !nearOwned; i++)
                    {
                        if (!hood.OwnsBuilding(i)) continue;
                        var f = map.buildings[i].footprint; if ((new Vector2(f[0], f[1]) - c).sqrMagnitude < 40f * 40f) nearOwned = true;
                    }
                    if (nearOwned) { biz.Log.Append($"lot at ({c.x:0}, {c.y:0}) is a community's, left as it is; "); continue; }
                    biz.Lots.Add(ring);
                }
            var kinds = new Dictionary<BizKind, int>();
            var owner = LoadOwner();
            // build speed: the lots (as they are while the fronts are chosen) and the road segments by cell, in their order
            var lotIdx = new CellIndex(64f); foreach (var lot in biz.Lots) lotIdx.Add(lot, 0.05f);
            var segIdx = new CellIndex(64f); foreach (var s in segs) segIdx.Add(s.a, s.b, s.half);
            var cand = new List<int>();
            for (int i = 0; i < map.buildings.Length; i++)
            {
                if (owner != null && owner.index == i) continue;   // Dan's house is never touched
                if (hood.OwnsBuilding(i) || shop.Owns(i) || i == shop.Center || rest.Owns(i)) continue;
                if (LakesOwns(i)) continue;   // Round 32 (C2): the Chastain Lakes pool house and bathhouse
                if (CoursonOwns(i)) continue;   // Round 39 (A4): the Courson Street pool's clubhouse, pool house and canopies
                if (Mall != null && Mall.Owns(i)) continue;   // Round 60: Town Center and its anchors (MapBuilder.Mall.cs)
                if (MariettaOwns(i) || RailOwns(i) || KennestoneOwns(i) || R78Gone(i)) continue;   // Round 76 (B): Marietta Square's bandstand, theater, market, museum and depot (MapBuilder.MariettaSquare.cs); Round 77 (C): a building the railroad runs through gives way
                var b = map.buildings[i]; var ring = ToPoints(b.footprint); if (ring.Count < 3) continue;
                if (PolyArea(ring) < 0f) ring.Reverse();
                Vector2 c = Vector2.zero; foreach (var p in ring) c += p; c /= ring.Count;
                if (hood.Bounds != null && hood.Bounds.Contains(c)) continue;   // the Glen's and the communities' own
                float area = Mathf.Abs(PolyArea(ring));
                bool byCanton = canton.Any(s => SegDist(c, s.a, s.b) < 90f);
                BizKind k;
                switch (b.type)
                {
                    case "retail": case "commercial": case "supermarket": case "kiosk": k = BizKind.Shop; break;
                    case "office": k = BizKind.Office; break;
                    case "industrial": case "warehouse": case "manufacture": k = BizKind.Industrial; break;
                    case "church": case "chapel": case "religious": k = BizKind.Church; break;
                    case "school": case "kindergarten": case "college": k = BizKind.School; break;
                    case "fire_station": case "public": case "civic": case "government": k = BizKind.Civic; break;
                    case "apartments": case "terrace": case "residential": case "detached": case "house": case "semidetached_house": case "dormitory": k = BizKind.Apartments; break;
                    case "garage": case "garages": k = BizKind.Garage; break;
                    case "roof": case "carport": case "pavilion": k = BizKind.Canopy; break;
                    case "shed": case "stable": case "barn": case "service": case "hut": k = BizKind.Shed; break;
                    default: k = area < 60f ? BizKind.Shed : byCanton ? BizKind.Shop : area >= 1500f ? BizKind.Industrial : BizKind.Office; break;
                }
                if ((k == BizKind.Apartments || k == BizKind.Garage) && area < 40f) k = BizKind.Shed;
                if (i == IceCreamShop) k = BizKind.Shop;   // Round 32 (A4): Dan, "I am pretty sure this is an ice cream shop"
                if (MariettaHistoric(i)) k = BizKind.Shop; else if (MariettaChurch(i)) k = BizKind.Church;
                else if (k == BizKind.Civic && MariettaNear(c)) k = BizKind.Office;   // Round 76: downtown's courthouses and county buildings are offices, not fire stations   // Round 76 (B): the square's old storefronts, its churches
                var z = new Biz { index = i, seed = b.seed, kind = k, type = b.type, ring = ring, c = c, area = area, height = b.height, pitched = b.roof == "gable", pitch = b.pitch_deg };
                if (MariettaHistoric(i)) { z.pitched = false; z.height = Mathf.Max(b.height, (Hash01(b.seed, 761) < 0.45f ? 3 : 2) * 3.7f + 0.9f); }   // two or three stories
                // the front: the face that looks most toward the nearest lot or road (Round 32: on Canton Road, toward Canton Road)
                float best = float.MinValue;
                if (!PickCantonFront(z, cantonHalf, cross))
                for (int e = 0; e < ring.Count; e++)
                {
                    Vector2 a = ring[e], bb = ring[(e + 1) % ring.Count]; float len = Vector2.Distance(a, bb); if (len < 3f) continue;
                    Vector2 m = (a + bb) * 0.5f, d = (bb - a) / len, n = new Vector2(d.y, -d.x);
                    if (InPolygon(m + n * 0.3f, ring)) n = -n;
                    Vector2 target = m + n * 200f; float dist = 200f;
                    // (a lot 200 m or more away, or a road dist - 8 m or more past its edge, can never be taken: only the nearer ones are asked)
                    foreach (int li in lotIdx.Near(m, 200f, cand)) { var lot = biz.Lots[li]; float dd = RingDist(m, lot); if (InPolygon(m, lot)) dd = 0f; if (dd < dist) { dist = dd; target = Closest(lot, m); } }
                    foreach (int si in segIdx.Near(m, Mathf.Max(dist - 8f, 0f) + 1f, cand)) { var s = segs[si]; float dd = SegDist(m, s.a, s.b) - s.half; if (dd < dist - 8f) { dist = dd + 8f; target = ClosestOnSeg(m, s.a, s.b); } }   // a lot counts 8 m nearer than a road
                    var to = target - m; float facing = to.sqrMagnitude > 1e-4f ? Vector2.Dot(n, to.normalized) : 0f;
                    float score = facing * Mathf.Sqrt(Mathf.Min(len, 40f)) / (1f + Mathf.Max(0f, dist) / 60f);
                    if (score > best) { best = score; z.front = e; z.frontN = n; }
                }
                if (z.front < 0) continue;
                SettleBody(z, t);
                biz.Items.Add(z); biz.Owned.Add(i);
                kinds[k] = kinds.TryGetValue(k, out int nk) ? nk + 1 : 1;
            }
            PlanCantonLots(biz, map, cantonHalf, cross, new HashSet<int>());   // Round 32 (A1)
            // a few trees: two off the front corners of each larger building, one off each lot corner, never on a road, a drive,
            // a lot, in or within 3 m of any building
            var all = new List<(Vector2 lo, Vector2 hi, List<Vector2> ring)>();
            foreach (var b in map.buildings) { var r = ToPoints(b.footprint); if (r.Count < 3) continue; all.Add((new Vector2(r.Min(p => p.x), r.Min(p => p.y)), new Vector2(r.Max(p => p.x), r.Max(p => p.y)), r)); }
            var allIdx = new CellIndex(32f); foreach (var a in all) allIdx.Add(a.lo.x - 3f, a.lo.y - 3f, a.hi.x + 3f, a.hi.y + 3f);   // build speed: by cell
            var treeIdx = new CellIndex(16f);
            bool Clear(Vector2 p)
            {
                if (biz.RejectTree(p.x, p.y) || hood.RejectTree(p.x, p.y) || rest.RejectTree(p.x, p.y) || shop.RejectTree(p.x, p.y)) return false;
                foreach (int si in segIdx.Near(p, 2.55f, cand)) { var s = segs[si]; if (SegDist(p, s.a, s.b) < s.half + 2.5f) return false; }
                foreach (int ai in allIdx.Near(p, 0f, cand)) { var a = all[ai]; if (p.x < a.lo.x - 3f || p.y < a.lo.y - 3f || p.x > a.hi.x + 3f || p.y > a.hi.y + 3f) continue; if (InPolygon(p, a.ring) || RingDist(p, a.ring) < 3f) return false; }
                while (treeIdx.Count < biz.Trees.Count) treeIdx.Add(biz.Trees[treeIdx.Count]);
                foreach (int ti in treeIdx.Near(p, 6f, cand)) if ((biz.Trees[ti] - p).sqrMagnitude < 36f) return false;
                return true;
            }
            foreach (var z in biz.Items)
            {
                if (z.kind == BizKind.Shed || z.kind == BizKind.Canopy || z.kind == BizKind.Garage || z.area < 150f) continue;
                Vector2 a = z.ring[z.front], bb = z.ring[(z.front + 1) % z.ring.Count], d = (bb - a).normalized;
                foreach (var p in new[] { a - d * 3f + z.frontN * 4f, bb + d * 3f + z.frontN * 4f }) if (Clear(p)) biz.Trees.Add(p);
            }
            foreach (var lot in biz.Lots)
            {
                Vector2 c = Vector2.zero; foreach (var p in lot) c += p; c /= lot.Count;
                foreach (var p in lot) { var q = p + (p - c).normalized * 3f; if (Clear(q)) biz.Trees.Add(q); }
            }
            biz.Log.Insert(0, $"[Round31] businesses: {biz.Items.Count} buildings dressed (" + string.Join(", ", kinds.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key} {kv.Value}")) +
                              $"), {biz.Items.Count(z => z.steps != null)} stepped, {biz.Lots.Count} parking lots, {biz.Trees.Count} trees; ");
            return biz;
        }

        static Vector2 ClosestOnSeg(Vector2 q, Vector2 a, Vector2 b) { var ab = b - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude); return a + ab * t; }
        // Round 34 (A4, BUG-005 / 006 / 008 / 009: "can we not put parking spaces on hills? ... just remove the lines"): a stall's
        // lines are painted only where the lot is near level: no more than a 5 % grade between any two of the stall's corners
        // and its middle within 8 cm of their mean (no hump or hollow). Steeper parts of a lot stay paved with no lines.
        const float StallMaxGrade = 0.05f, StallMaxHump = 0.08f;
        static bool StallLevel(Vector2 a, Vector2 b, Vector2 c, Vector2 d, TerrainGrid t, ref int sloped)
        {
            var q = new[] { a, b, c, d }; var h = q.Select(p => Ground(t, p.x, p.y)).ToArray();
            bool ok = true;
            for (int i = 0; i < 4 && ok; i++)
                for (int j = i + 1; j < 4; j++)
                    if (Mathf.Abs(h[i] - h[j]) > StallMaxGrade * Mathf.Max(1f, Vector2.Distance(q[i], q[j]))) { ok = false; break; }
            var m = (a + b + c + d) * 0.25f;
            if (ok && Mathf.Abs(Ground(t, m.x, m.y) - h.Average()) > StallMaxHump) ok = false;
            if (!ok) sloped++;
            return ok;
        }

        static Vector2 Closest(List<Vector2> ring, Vector2 q) { Vector2 best = ring[0]; float bd = float.MaxValue; for (int i = 0; i < ring.Count; i++) { var c = ClosestOnSeg(q, ring[i], ring[(i + 1) % ring.Count]); float d = (c - q).sqrMagnitude; if (d < bd) { bd = d; best = c; } } return best; }

        // the floor: the ground at the front, raised to within 1.2 m of the highest ground under the walls; a long rectangle whose
        // ground climbs more than 1.5 m along it is stepped into segments of 8 m or more, each with its own floor (BUG-002)
        static void SettleBody(Biz z, TerrainGrid t)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var p in Sample(z.ring)) { float g = Ground(t, p.x, p.y); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            Vector2 fa = z.ring[z.front], fb = z.ring[(z.front + 1) % z.ring.Count], fm = (fa + fb) * 0.5f + z.frontN * 0.8f;
            float frontY = Ground(t, fm.x, fm.y);
            z.baseY = lo - 0.5f * vScale;
            z.floorY = Mathf.Max(frontY, hi - 1.2f) + 0.1f;
            if (RaisedBuildings.Contains(z.index)) { z.floorY = hi + 0.1f; return; }   // Round 40 (C): Dan's buried building, set on the ground (its floor over the highest ground, no steps)
            if (z.kind == BizKind.Canopy) return;
            if (!BestRectangle(z.ring, out var c, out var U, out var V, out float hl, out float hw) || z.area / (4f * hl * hw) < 0.85f || hl * 2f < 30f) return;
            float g0 = Ground(t, (c - U * hl).x, (c - U * hl).y), g1 = Ground(t, (c + U * hl).x, (c + U * hl).y);
            if (hi - lo < 1.5f || Mathf.Abs(g1 - g0) < 1.5f) return;
            int n = Mathf.Clamp(Mathf.CeilToInt((hi - lo) / 0.9f), 2, Mathf.Max(2, Mathf.FloorToInt(hl * 2f / 8f)));
            z.steps = new List<(List<Vector2>, float)>();
            for (int k = 0; k < n; k++)
            {
                float u0 = -hl + 2f * hl * k / n, u1 = -hl + 2f * hl * (k + 1) / n;
                var seg = new List<Vector2> { c + U * u0 - V * hw, c + U * u1 - V * hw, c + U * u1 + V * hw, c + U * u0 + V * hw };
                if (PolyArea(seg) < 0f) seg.Reverse();
                float shi = float.MinValue, sfront = float.MinValue;
                foreach (var p in Sample(seg)) shi = Mathf.Max(shi, Ground(t, p.x, p.y));
                var sm = c + U * ((u0 + u1) * 0.5f) + z.frontN * (hw + 0.8f); sfront = Ground(t, sm.x, sm.y);
                z.steps.Add((seg, Mathf.Max(sfront, shi - 0.6f) + 0.1f));
            }
        }

        // the ring's corners and a point every 2 m along its edges
        static IEnumerable<Vector2> Sample(List<Vector2> ring)
        {
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count]; int k = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 2f));
                for (int j = 0; j < k; j++) yield return Vector2.Lerp(a, b, j / (float)k);
            }
        }

        // ------------------------------------------------------------ the build

        static readonly Color BizConcrete = new Color(0.62f, 0.61f, 0.58f), BizGlass = new Color(0.15f, 0.21f, 0.27f), BizFrame = new Color(0.22f, 0.22f, 0.23f),
                              BizDoor = new Color(0.10f, 0.12f, 0.14f), BizTrim = new Color(0.88f, 0.88f, 0.86f), BizDark = new Color(0.16f, 0.16f, 0.17f);
        static readonly Color[] ShopWalls = { new Color(0.80f, 0.74f, 0.62f), new Color(0.72f, 0.70f, 0.66f), new Color(0.60f, 0.34f, 0.26f), new Color(0.84f, 0.82f, 0.76f), new Color(0.66f, 0.58f, 0.48f), new Color(0.56f, 0.60f, 0.62f) };
        static readonly Color[] OfficeWalls = { new Color(0.78f, 0.76f, 0.72f), new Color(0.56f, 0.32f, 0.24f), new Color(0.66f, 0.66f, 0.66f), new Color(0.74f, 0.68f, 0.58f) };
        static readonly Color[] MetalWalls = { new Color(0.70f, 0.72f, 0.73f), new Color(0.78f, 0.76f, 0.70f), new Color(0.52f, 0.58f, 0.64f), new Color(0.62f, 0.64f, 0.60f) };
        static readonly Color[] AwningColors = { new Color(0.20f, 0.30f, 0.22f), new Color(0.48f, 0.12f, 0.12f), new Color(0.18f, 0.24f, 0.42f), new Color(0.30f, 0.30f, 0.32f), new Color(0.62f, 0.42f, 0.12f) };
        static readonly Color[] AptDoors = { new Color(0.30f, 0.18f, 0.12f), new Color(0.15f, 0.22f, 0.32f), new Color(0.88f, 0.88f, 0.86f), new Color(0.42f, 0.10f, 0.10f) };
        static readonly Color SchoolBrick = new Color(0.58f, 0.30f, 0.22f), ChurchWhite = new Color(0.92f, 0.91f, 0.87f), ChurchBrick = new Color(0.55f, 0.27f, 0.20f);

        sealed class SignBatch
        {
            sealed class Cell { public List<Vector3> v = new List<Vector3>(); public List<Vector2> uv = new List<Vector2>(); public List<int> tri = new List<int>(); }
            readonly Dictionary<Vector2Int, Cell> cells = new Dictionary<Vector2Int, Cell>();
            public int Count;
            // a sign face w x h centered at c, facing out along n, showing atlas row 'row'
            public void Add(Vector3 c, Vector2 n, float w, float h, int row)
            {
                var key = new Vector2Int(Mathf.FloorToInt(c.x / 128f), Mathf.FloorToInt(c.z / 128f));
                if (!cells.TryGetValue(key, out var cell)) cells[key] = cell = new Cell();
                var F = new Vector3(n.x, 0f, n.y).normalized; var R = Vector3.Cross(Vector3.up, -F);
                Vector3 bl = c - R * (w / 2) - Vector3.up * (h / 2), br = c + R * (w / 2) - Vector3.up * (h / 2);
                int i0 = cell.v.Count;
                cell.v.Add(bl); cell.v.Add(br); cell.v.Add(br + Vector3.up * h); cell.v.Add(bl + Vector3.up * h);
                int col = row / 32, r = row % 32;
                float u0 = col * 0.25f + 0.001f, u1 = col * 0.25f + 0.249f,   // Round 77: four columns (BusinessSigns.png 4096 wide)
                     v1 = 1f - r / 32f - 0.0008f, v0 = 1f - (r + 1) / 32f + 0.0008f;
                cell.uv.Add(new Vector2(u0, v0)); cell.uv.Add(new Vector2(u1, v0)); cell.uv.Add(new Vector2(u1, v1)); cell.uv.Add(new Vector2(u0, v1));
                cell.tri.AddRange(new[] { i0, i0 + 3, i0 + 2, i0, i0 + 2, i0 + 1 });
                Count++;
            }
            public void ToObjects(Transform parent, Material mat, string name = "BusinessSigns")   // Round 76: a name of its own per batch (its meshes are saved by name)
            {
                foreach (var kv in cells)
                {
                    var go = new GameObject($"{name}_{kv.Key.x}_{kv.Key.y}"); go.transform.SetParent(parent, false);
                    GameObjectUtility.SetStaticEditorFlags(go, (StaticEditorFlags)int.MaxValue & ~StaticEditorFlags.ContributeGI);
                    var mesh = new Mesh { name = go.name };
                    mesh.SetVertices(kv.Value.v); mesh.SetUVs(0, kv.Value.uv); mesh.SetTriangles(kv.Value.tri, 0);
                    mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/{go.name}.asset");
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        static Material BusinessSignMaterial()
        {
            const string path = "Assets/HandMade/Businesses/BusinessSigns.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("[Round31] " + path + " missing (Tools/handmade/make_business_signs.py): blank sign boards"); return null; }
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "BusinessSigns" };   // lit, depth-tested, one-sided
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.2f); m.SetFloat("_Cull", (float)CullMode.Back);
            m = GenAsset(m, GeneratedDir + "/BusinessSigns.mat");
            return m;
        }

        static void BuildBusinesses(MapData map, TerrainGrid t, Materials mats, Transform parent, Businesses biz)
        {
            var root = new GameObject("Businesses").transform; root.SetParent(parent, false);
            var bodies = new Dictionary<Vector2Int, MeshBuilder>(); var details = new Dictionary<Vector2Int, MeshBuilder>();
            MeshBuilder At(Dictionary<Vector2Int, MeshBuilder> d, Vector2 p) { var k = new Vector2Int(Mathf.FloorToInt(p.x / 128f), Mathf.FloorToInt(p.y / 128f)); if (!d.TryGetValue(k, out var mb)) d[k] = mb = new MeshBuilder(); return mb; }
            var signs = new SignBatch(); var signMat = BusinessSignMaterial();
            var used = new Dictionary<BizKind, int>();
            int Next(BizKind k, int seed)
            {
                if (!BizSignRows.TryGetValue(k, out var rr)) return -1;
                used.TryGetValue(k, out int u); used[k] = u + 1;
                return rr.first + (u + (int)(Mix(seed, 901) % 3u)) % rr.count;   // cycled in map order (a strip's units take consecutive names: neighbors differ)
            }
            int signsShown = 0;
            void Sign(MeshBuilder d, Frame f, float u, float y, float w, int row, float proud = 0.18f)
            {
                if (row < 0 || signMat == null) return;
                float h = w / 8f;
                BoxF(d, f, u - w / 2f - 0.12f, u + w / 2f + 0.12f, y - h / 2f - 0.12f, y + h / 2f + 0.12f, 0.02f, proud, BizDark);
                var c = f.P(u, y, proud + 0.012f);
                signs.Add(c, new Vector2(f.F.x, f.F.z), w, h, row); signsShown++;
            }

            bool pizzaDone = false, pizzaWindow = false;
            foreach (var z in biz.Items.OrderBy(q => q.c.y).ThenBy(q => q.c.x))
            {
                var body = At(bodies, z.c); var det = At(details, z.c);
                float wallH = Mathf.Max(3.0f, z.height);
                if (RaisedBuildings.Contains(z.index)) wallH += 2.0f;   // Round 40 (C, BUG-011): the garage row's 27 m wide roof took most of its 4.8 m: its walls stood 2.6 m, sunk-looking; now 3.7 m under the roof
                Color wall, roofC = PickColour(FlatRoofPalette, z.seed, 2);
                switch (z.kind)
                {
                    case BizKind.Shop: wall = MariettaHistoric(z.index) ? PickOf(MariettaBrick, z.seed, 11) : PickOf(ShopWalls, z.seed, 11); break;
                    case BizKind.Office: wall = PickOf(OfficeWalls, z.seed, 12); break;
                    case BizKind.Industrial: wall = PickOf(MetalWalls, z.seed, 13); break;
                    case BizKind.Church: wall = Hash01(z.seed, 14) < 0.5f ? ChurchWhite : ChurchBrick; roofC = new Color(0.22f, 0.22f, 0.24f); break;
                    case BizKind.School: wall = SchoolBrick; break;
                    case BizKind.Civic: wall = new Color(0.60f, 0.32f, 0.24f); break;
                    case BizKind.Apartments: wall = PickOf(TownSiding, z.seed, 15); roofC = PickOf(ShingleRoofs, z.seed, 16); break;
                    default: wall = PickColour(WallPalette, z.seed, 1); roofC = z.pitched ? PickColour(RoofPalette, z.seed, 2) : roofC; break;
                }
                // --- the body (a foundation band under the floor where it stands over the ground)
                if (z.kind == BizKind.Canopy) { CanopyOnPosts(body, z, t, wallH); continue; }
                var parts = z.steps ?? new List<(List<Vector2>, float)> { (z.ring, z.floorY) };
                if (HasWings(z.index)) { BuildChurchWings(body, z, wall, roofC, wallH); parts = new List<(List<Vector2>, float)>(); }   // Round 32 (A5)
                foreach (var (ring, floor) in parts)
                {
                    float top = floor + wallH;
                    if (floor - z.baseY > 0.05f) Extrude(body, ring, z.baseY, floor, BizConcrete, BizConcrete, false);
                    int shape = z.pitched ? AddPitched(body, ring, floor, top, wallH, z.pitch, wall, roofC) : -1;
                    if (shape < 0)
                    {
                        Extrude(body, ring, floor, top, wall, roofC, true);
                        if (z.kind != BizKind.Shed && z.kind != BizKind.Garage && z.kind != BizKind.Apartments)
                            for (int e = 0; e < ring.Count; e++) Parapet(body, ring[e], ring[(e + 1) % ring.Count], ring, top, Shade(wall, 0.85f));
                    }
                }
                float eave = HasWings(z.index) ? 9f : z.pitched ? WallTopUnderRoof(z, wallH) : wallH;
                // --- the front and the other faces (a stepped body: each step's own outer faces, at its own floor)
                var faces = new List<(Vector2 a, Vector2 b, float floor, bool front)>();
                if (z.steps == null) for (int e = 0; e < z.ring.Count; e++) faces.Add((z.ring[e], z.ring[(e + 1) % z.ring.Count], z.floorY, e == z.front));
                else
                    foreach (var (sr, sf) in z.steps)
                        for (int e = 0; e < sr.Count; e++)
                        {
                            Vector2 sa = sr[e], sb = sr[(e + 1) % sr.Count]; if (RingDist((sa + sb) * 0.5f, z.ring) > 0.3f) continue;   // a wall between two steps
                            Vector2 sd = (sb - sa).normalized, sn = new Vector2(sd.y, -sd.x); if (InPolygon((sa + sb) * 0.5f + sn * 0.3f, sr)) sn = -sn;
                            faces.Add((sa, sb, sf, Vector2.Dot(sn, z.frontN) > 0.9f));
                        }
                foreach (var (a, b, floorHere, front0) in faces)
                {
                    float len = Vector2.Distance(a, b); if (len < 2.5f) continue;
                    Vector2 d = (b - a) / len, n = new Vector2(d.y, -d.x); if (InPolygon((a + b) * 0.5f + n * 0.3f, z.ring)) n = -n;
                    Vector2 m = (a + b) * 0.5f;
                    // Round 32 (A1 / A3: "make these all store fronts"): a Canton Road shop has storefronts on every face onto its lot
                    bool front = front0 || (z.canton && z.kind == BizKind.Shop && len >= 5f && (Vector2.Dot(n, z.frontN) > 0.7f || biz.InNewLot(m + n * (LotWalk + 2f))));
                    // A3: at 76's corner by Dan's spot the face toward the lot is the pizza place's front, the other its drive-up side
                    bool atCorner = z.index == PizzaStrip && (Vector2.Distance(a, PizzaCorner) < 2f || Vector2.Distance(b, PizzaCorner) < 2f);
                    bool pizzaFront = atCorner && n.y < -0.7f, pizzaSide = atCorner && n.x < -0.7f;
                    if (pizzaFront) front = true; if (pizzaSide) front = false;
                    var f = MakeFrame(new Vector3(m.x, floorHere, m.y), n, 1f);
                    // the frame's u runs to the right seen from outside; that is along +d or -d
                    float half = len / 2f;
                    switch (z.kind)
                    {
                        case BizKind.Shop:
                            if (front)
                            {
                                System.Func<int, int> row = null;
                                if (pizzaFront)
                                {
                                    // the units in order from the corner: the pizza place, the comic book store, the hardware store
                                    var cornerU = Vector2.Dot(PizzaCorner - m, f.R.x * Vector2.right + f.R.z * Vector2.up);
                                    row = k => PizzaRow(k);
                                    ShopFronts(det, f, half, eave, z, Sign, Next, row, cornerU > 0f);
                                }
                                else if (z.index == IceCreamShop) { ShopFronts(det, f, half * 0.55f, eave, z, Sign, Next, k => SignIceCream, false); WalkUpWindow(det, f, half * 0.72f); }
                                else if (MariettaHistoric(z.index)) { ShopFronts(det, f, half, eave, z, Sign, Next, k => MariettaShopRow(z.seed, k), false); }   // Round 76 (B): the square's made-up names
                                else ShopFronts(det, f, half, eave, z, Sign, Next, null, false);
                                pizzaDone |= z.index == PizzaStrip && row != null;
                            }
                            else if (pizzaSide)
                            {
                                var cu = Vector2.Dot(PizzaCorner - m, f.R.x * Vector2.right + f.R.z * Vector2.up);
                                DriveUpWindow(det, f, cu + (cu < 0f ? 4.5f : -4.5f), Sign); pizzaWindow = true;
                                biz.PizzaLane = (a, b, n);   // its lane, painted with the lot
                            }
                            else FrontQuad(det, f, -half + 0.05f, half - 0.05f, -0.3f, 0.7f, 0.03f, Shade(wall, 0.78f));
                            if (!front && len > 12f && Vector2.Dot(n, z.frontN) < -0.9f) ServiceDoor(det, f, half * 0.4f);
                            if (MariettaHistoric(z.index)) HistoricUpper(det, f, half, eave, wall, front);   // Round 76 (B): tall upper windows and a cornice
                            break;
                        case BizKind.Office:
                            WindowBands(det, f, half, eave, front ? 3.4f : 0f);
                            if (front) { Entrance(det, f, 0f, 3.2f, BizGlass); RoofSign(det, f, half, eave, Next(BizKind.Office, z.seed), Sign); }
                            break;
                        case BizKind.Industrial:
                            Ribs(det, f, half, eave, Shade(wall, 0.9f));
                            if (front) Docks(det, f, half, eave, z, Sign, Next);
                            else if (len > 10f) FrontQuad(det, f, -half + 1f, half - 1f, eave - 1.6f, eave - 0.9f, 0.04f, BizGlass);
                            break;
                        case BizKind.Church:
                            bool hollowEast = z.index == 74 && ChurchHollows.Count > 0 && Mathf.Abs(m.x - ChurchWings[2].x1) < 1.5f && m.y > ChurchWings[2].z0 && m.y < ChurchWings[2].z1;
                            if (!hollowEast) TallWindows(det, f, half, eave, front);   // Round 42 (B1): the north wing's east wall (the doors') has the crypt's windows (they break)
                            if (front && z.index == 74) ChurchEastDoor(body, det, z, wallH, Sign, Next);   // Round 32 (A5): the door, sign and steeple toward Canton Road
                            else if (front) ChurchFront(body, det, f, half, eave, wall, z, Sign, Next, z.index != 75);
                            break;
                        case BizKind.School:
                            Windows(det, f, half, eave, 3.2f, 1.6f, 1.6f, front ? 4.5f : 0f, BizTrim);
                            if (front) SchoolFront(det, f, half, eave, z, Sign);
                            break;
                        case BizKind.Civic:
                            if (front) BayDoors(det, f, half, eave, z, Sign, Next);
                            else Windows(det, f, half, eave, 3.2f, 1.4f, 1.4f, 0f, BizTrim);
                            break;
                        case BizKind.Apartments:
                            Windows(det, f, half, eave, 3.2f, 1.2f, 1.4f, 0f, BizTrim);
                            if (front) for (float u = -half + 3.5f; u <= half - 2f; u += 7f) { FrontQuad(det, f, u - 0.55f, u + 0.55f, 0f, 2.15f, 0.06f, PickOf(AptDoors, z.seed, 17)); FrontQuad(det, f, u - 0.7f, u + 0.7f, -0.1f, 2.3f, 0.04f, BizTrim); }
                            break;
                        case BizKind.Garage:
                            if (front) for (float u = -half + 1.9f; u <= half - 1.5f; u += 3.4f) { FrontQuad(det, f, u - 1.35f, u + 1.35f, 0f, Mathf.Min(2.3f, eave - 0.4f), 0.05f, new Color(0.86f, 0.86f, 0.84f)); for (float y = 0.55f; y < 2.2f; y += 0.55f) FrontQuad(det, f, u - 1.35f, u + 1.35f, y, y + 0.04f, 0.06f, new Color(0.72f, 0.72f, 0.70f)); }
                            break;
                        case BizKind.Shed:
                            if (front && len >= 2.6f) { FrontQuad(det, f, -0.5f, 0.5f, 0f, Mathf.Min(2.0f, eave - 0.2f), 0.05f, Shade(wall, 0.6f)); }
                            break;
                    }
                }
                if (!z.pitched && z.area > 150f && z.kind != BizKind.Shed && z.kind != BizKind.Garage) RoofUnits(det, z, wallH);
                if (z.kind == BizKind.School) Flagpole(det, z, t);
            }
            BuildClock.Lap("Businesses: the buildings");   // Round 65: the step's parts timed (build_times.txt)

            // --- the lots
            var lotMb = new Chunks { Tint = LotAsphalt }; var lotPaint = new Chunks(); var lamps = new Breakables();
            int stalls = 0, lampsN = 0, stallsSloped = 0;
            var roadSegs = new List<(Vector2 a, Vector2 b, float half)>();
            foreach (var r in map.roads) { var p = ToPoints(r.points); for (int i = 0; i + 1 < p.Count; i++) roadSegs.Add((p[i], p[i + 1], r.width * 0.5f)); }
            roadSegs.AddRange(biz.Drives);   // Round 32 (A1): the drives cut the curbs and keep stalls and poles off them
            var bizRings = biz.Items.Select(q => q.ring).ToList();
            // build speed (after Round 61): the buildings and the lots by cell (padded by the furthest any test below reaches), in their order
            var ringIdx = new CellIndex(32f); foreach (var r in bizRings) ringIdx.Add(r, 2.05f);
            var otherIdx = new CellIndex(64f); foreach (var l in biz.Lots) otherIdx.Add(l, 0.05f);
            var nearR = new List<int>(); var nearO = new List<int>();
            bool AnyRing(Vector2 q0, Vector2 q1, Vector2 q2, Vector2 q3, System.Func<List<Vector2>, bool> hit)
            {
                foreach (int i in ringIdx.Near(Mathf.Min(Mathf.Min(q0.x, q1.x), Mathf.Min(q2.x, q3.x)), Mathf.Min(Mathf.Min(q0.y, q1.y), Mathf.Min(q2.y, q3.y)),
                                               Mathf.Max(Mathf.Max(q0.x, q1.x), Mathf.Max(q2.x, q3.x)), Mathf.Max(Mathf.Max(q0.y, q1.y), Mathf.Max(q2.y, q3.y)), nearR))
                    if (hit(bizRings[i])) return true;
                return false;
            }
            var roadGrid = new SegGrid(roadSegs, 3f);   // Round 42 (A): every use below compares with less than 3 m
            // Round 32 (A1): the walks in front of the stores (concrete, a little above the lot) and the drives (the lot's asphalt)
            foreach (var w in biz.Walks) { var mb = lotMb.At(Centroid(w)); mb.Tint = new Color(0.70f, 0.69f, 0.66f); DrapePoly(mb, w, 1.5f, 0.075f * vScale, t, 10); }
            foreach (var dr in biz.Drives)
            {
                var mb = lotMb.At(dr.a); mb.Tint = LotAsphalt; Vector2 dd = (dr.b - dr.a).normalized, nn = new Vector2(-dd.y, dd.x);
                Strip(mb, dr.a, dr.b, nn, -dr.half, dr.half, 0.045f * vScale, t);
            }
            if (biz.PizzaLane.HasValue)
            {
                var (pla, plb, pln) = biz.PizzaLane.Value;
                var pm = lotPaint.At(pla); Vector2 pld = (plb - pla).normalized;
                PaintLine(pm, pla + pln * 4.2f - pld * 6f, plb + pln * 4.2f + pld * 2f, 0.15f, 0.065f * vScale, t);   // the drive-up lane round the side
            }
            BuildClock.Lap("Businesses: walks and drives");
            int lotNo = 0;
            foreach (var lot in biz.Lots)
            {
                int self = lotNo++;
                bool InOther(Vector2 q) { foreach (int j in otherIdx.Near(q, 0f, nearO)) if (j != self && InPolygon(q, biz.Lots[j])) return true; return false; }   // Round 32: lots that overlap
                Vector2 c = Vector2.zero; foreach (var p in lot) c += p; c /= lot.Count;
                var mb = lotMb.At(c); mb.Tint = LotAsphalt;
                DrapePoly(mb, lot, 1.5f, 0.05f * vScale, t, 12, ChurchHollows.Count > 0 ? ChurchHollows.Values.Select(h => Rect.MinMaxRect(h.x0, h.z0, h.x1, h.z1)).ToList() : null);   // Round 42 (B2, BUG-002): never through the hollow church (it stood over its aisle and stair)
                var walks = lotMb.At(c);
                for (int i = 0; i < lot.Count; i++)
                {
                    Vector2 a = lot[i], b = lot[(i + 1) % lot.Count]; float len = Vector2.Distance(a, b); if (len < 1f) continue;
                    Vector2 d = (b - a) / len, n = new Vector2(-d.y, d.x); if (!InPolygon((a + b) * 0.5f + n * 0.5f, lot)) n = -n;   // n points into the lot
                    // the curb, except where a road runs into the lot
                    for (float s = 0f; s < len; s += 1f)
                    {
                        Vector2 p0 = a + d * s, p1 = a + d * Mathf.Min(len, s + 1f);
                        if (roadGrid.Clearance((p0 + p1) * 0.5f) < 0.6f || InOther((p0 + p1) * 0.5f)) continue;
                        walks.Tint = new Color(0.70f, 0.69f, 0.66f); Strip(walks, p0, p1, n, 0f, 0.35f, 0.08f * vScale, t);
                    }
                    // stalls along the edges long enough for them: 2.8 m apart, 5.2 m deep
                    if (len >= 12f)
                        for (float s = 1.5f; s + 2.8f <= len - 1.5f; s += 2.8f)
                        {
                            Vector2 p0 = a + d * s, p1 = p0 + n * 5.2f, p2 = p0 + d * 2.8f, p3 = p2 + n * 5.2f;
                            bool ok = InPolygon(p1, lot) && InPolygon(p3, lot) && roadGrid.Clearance((p0 + p3) * 0.5f) > 1f && !AnyRing(p0, p1, p2, p3, r => InPolygon(p1, r) || InPolygon(p3, r) || InPolygon(p0, r) || InPolygon(p2, r)) && !InOther(p0) && !InOther(p3) && StallLevel(p0, p1, p2, p3, t, ref stallsSloped);   // Round 34 (A4)
                            if (!ok) continue;
                            var pm = lotPaint.At(p0); PaintLine(pm, p0 + n * 0.3f, p1, 0.12f, 0.065f * vScale, t); stalls++;
                            if (s + 5.6f > len - 1.5f || !InPolygon(p3 + d * 2.8f, lot)) PaintLine(pm, p2 + n * 0.3f, p3, 0.12f, 0.065f * vScale, t);
                        }
                    // light poles every 32 m along the edge, just inside it
                    for (float s = 8f; s < len - 4f; s += 32f)
                    {
                        var p = a + d * s + n * 0.7f; float gy = Ground(t, p.x, p.y);
                        if (roadGrid.Clearance(p) < 1.5f || InOther(p) || AnyRing(p, p, p, p, r => InPolygon(p, r) || RingDist(p, r) < 1.5f)) continue;
                        var lb = lamps.Begin(p); var dark = new Color(0.20f, 0.20f, 0.22f);
                        lamps.Piece(() => Post(lb, new Vector3(p.x, gy - 0.1f, p.y), 4.6f, 0.18f, dark), gy + 0.1f);
                        lamps.Piece(() => Post(lb, new Vector3(p.x, gy + 4.5f, p.y), 4.4f, 0.15f, dark), gy + 0.1f);
                        lamps.Piece(() =>
                        {
                            var head = new Vector3(p.x, gy + 8.8f, p.y) + new Vector3(n.x, 0f, n.y) * 0.9f;
                            Beam(lb, new Vector3(p.x, gy + 8.75f, p.y), head, 0.09f, 0.09f, dark);
                            BoxF(lb, MakeFrame(head, n, 1f), -0.5f, 0.5f, -0.18f, 0.1f, -0.3f, 0.3f, dark);
                        }, gy + 0.1f);
                        lamps.End(new Vector3(p.x, gy + 1.2f, p.y), new Vector3(d.x, 0f, d.y), 0.6f, 2.4f);
                        lampsN++;
                    }
                }
                // Round 32 (A1): a new lot's middle in double rows of stalls along its longest edge, 18 m apart (5.2 m stalls each
                // side of the row's line, a 7.6 m aisle between rows)
                if (!biz.NewLots.Contains(lot)) continue;
                int le = 0; float ll = 0f; for (int i = 0; i < lot.Count; i++) { float l = Vector2.Distance(lot[i], lot[(i + 1) % lot.Count]); if (l > ll) { ll = l; le = i; } }
                Vector2 A = lot[le], U = (lot[(le + 1) % lot.Count] - A).normalized, N = new Vector2(-U.y, U.x); if (!InPolygon((A + lot[(le + 1) % lot.Count]) * 0.5f + N * 0.5f, lot)) N = -N;
                float span = lot.Max(q => Vector2.Dot(q - A, N)), u0 = lot.Min(q => Vector2.Dot(q - A, U)), u1 = lot.Max(q => Vector2.Dot(q - A, U));
                for (float v = 5.2f + 7.6f + 5.2f; v + 5.2f + 6f < span; v += 18f)
                    for (float u = u0 + 1.5f; u + 2.8f < u1; u += 2.8f)
                    {
                        var p0 = A + U * u + N * v; var p2 = p0 + U * 2.8f;
                        foreach (float side in new[] { -1f, 1f })
                        {
                            var p1 = p0 + N * (5.2f * side); var p3 = p2 + N * (5.2f * side);
                            bool ok = InPolygon(p0, lot) && InPolygon(p1, lot) && InPolygon(p2, lot) && InPolygon(p3, lot) && roadGrid.Clearance((p0 + p3) * 0.5f) > 1f
                                      && !AnyRing(p0, p1, p2, p3, r => InPolygon(p0, r) || InPolygon(p1, r) || InPolygon(p2, r) || InPolygon(p3, r) || RingDist(p1, r) < 2f || RingDist(p3, r) < 2f) && !InOther(p1) && !InOther(p3) && StallLevel(p0, p1, p2, p3, t, ref stallsSloped);   // Round 34 (A4)
                            if (!ok) continue;
                            PaintLine(lotPaint.At(p0), p0 + N * (0.1f * side), p1, 0.12f, 0.065f * vScale, t); stalls++;
                        }
                    }
            }

            BuildClock.Lap("Businesses: the lots");
            foreach (var kv in bodies) kv.Value.ToObject($"BusinessBodies_{kv.Key.x}_{kv.Key.y}", mats.House, root, true);
            BuildClock.Lap("Businesses: the bodies' meshes and colliders");
            foreach (var kv in details) kv.Value.ToObject($"BusinessDetail_{kv.Key.x}_{kv.Key.y}", mats.House, root, false, false);
            BuildClock.Lap("Businesses: the details' meshes");
            lotMb.ToObjects("BusinessLots", mats.Driveway, root, false);
            lotPaint.ToObjects("BusinessLotPaint", mats.Paint, root, false);
            BuildClock.Lap("Businesses: the lots' meshes");
            if (lampsN > 0) lamps.ToObjects("BusinessLotLamps", mats.House, root, 1f, BreakSounds.Kind.Lamp);
            if (signMat != null) signs.ToObjects(root, signMat);
            BuildClock.Lap("Businesses: lamps and signs");
            biz.Log.Append($"{signsShown} signs, {biz.Lots.Count} lots with {stalls} stalls ({stallsSloped} left unpainted on a slope, Round 34) and {lampsN} light poles");
            Debug.Log(biz.Log.ToString());
            // views for the pictures: one per kind (the largest of each), from 40 m in front of its front
            var views = new StringBuilder("kind\tindex\tx\tz\tyaw\tcx\tcz\n");
            foreach (var g in biz.Items.GroupBy(q => q.kind).OrderBy(g => g.Key))
            {
                var z = g.OrderByDescending(q => q.kind == BizKind.Shop ? (q.ring.Count > 0 ? Vector2.Distance(q.ring[q.front], q.ring[(q.front + 1) % q.ring.Count]) : 0f) : q.area).First();
                Vector2 fm = (z.ring[z.front] + z.ring[(z.front + 1) % z.ring.Count]) * 0.5f, eye = fm + z.frontN * (z.kind == BizKind.Shed || z.kind == BizKind.Canopy || z.kind == BizKind.Garage ? 14f : 32f);
                float yaw = Mathf.Atan2(-z.frontN.x, -z.frontN.y) * Mathf.Rad2Deg;
                views.AppendLine($"{g.Key}\t{z.index}\t{eye.x:0.0}\t{eye.y:0.0}\t{yaw:0}\t{z.c.x:0}\t{z.c.y:0}");
            }
            File.WriteAllText(Path.Combine(ProjectRoot, GeneratedDir, "business_views.tsv"), views.ToString());
        }

        static float FloorAt(Biz z, Vector2 p)
        {
            if (z.steps == null) return z.floorY;
            foreach (var (ring, floor) in z.steps) if (InPolygon(p, ring) || RingDist(p, ring) < 0.5f) return floor;
            return z.steps.OrderBy(s => (Centroid(s.ring) - p).sqrMagnitude).First().floor;
        }
        static Vector2 Centroid(List<Vector2> r) { Vector2 c = Vector2.zero; foreach (var p in r) c += p; return c / r.Count; }

        // the wall height under a pitched roof (as AddPitched: the rise is clamped to 45% of the height)
        static float WallTopUnderRoof(Biz z, float wallH)
        {
            if (!BestRectangle(z.ring, out _, out _, out _, out float hl, out float hw)) return wallH * 0.6f;
            float tan = Mathf.Tan(Mathf.Clamp(z.pitch, 10f, 45f) * Mathf.Deg2Rad);
            float rect = z.area / (4f * hl * hw);
            if (hw >= 1.8f && rect >= 0.72f) return wallH - Mathf.Clamp(tan * hw * vScale, 0.5f, 0.45f * wallH);
            return wallH * 0.6f;
        }

        static void Parapet(MeshBuilder mb, Vector2 a, Vector2 b, List<Vector2> ring, float top, Color c)
        {
            float len = Vector2.Distance(a, b); if (len < 0.5f) return;
            Vector2 d = (b - a) / len, n = new Vector2(d.y, -d.x); if (InPolygon((a + b) * 0.5f + n * 0.3f, ring)) n = -n;
            var f = MakeFrame(new Vector3((a.x + b.x) / 2f, top, (a.y + b.y) / 2f), n, 1f);
            BoxF(mb, f, -len / 2f, len / 2f, -0.15f, 0.55f, -0.3f, 0.06f, c);
        }

        static void ShopFronts(MeshBuilder d, Frame f, float half, float eave, Biz z, System.Action<MeshBuilder, Frame, float, float, float, int, float> sign, System.Func<BizKind, int, int> next,
                               System.Func<int, int> special = null, bool fromRight = false)   // Round 32: a row per unit, counted from the left end (or the right)
        {
            float len = half * 2f;
            bool big = len > 70f && z.area > 2500f && !z.canton;   // Round 32 (A3): on Canton Road a long front is a row of stores
            int units = big ? 1 : Mathf.Max(1, Mathf.RoundToInt(len / 9f));
            float w = len / units;
            for (int k = 0; k < units; k++)
            {
                float uc = -half + w * (k + 0.5f);
                float gw = big ? Mathf.Min(24f, len * 0.4f) : w - 1.2f;
                FrontQuad(d, f, uc - gw / 2f - 0.15f, uc + gw / 2f + 0.15f, -0.3f, 2.95f, 0.03f, BizFrame);
                FrontQuad(d, f, uc - gw / 2f, uc + gw / 2f, 0.25f, 2.8f, 0.05f, BizGlass);
                float du = (k % 2 == 0 ? -1f : 1f) * gw * 0.25f;
                FrontQuad(d, f, uc + du - 0.9f, uc + du + 0.9f, 0.0f, 2.4f, 0.07f, BizDoor);
                FrontQuad(d, f, uc + du - 0.03f, uc + du + 0.03f, 0.0f, 2.4f, 0.08f, BizFrame);
                BoxF(d, f, uc - (big ? gw / 2f + 1f : w / 2f - 0.2f), uc + (big ? gw / 2f + 1f : w / 2f - 0.2f), 3.05f, 3.2f, 0.02f, 1.6f, PickOf(AwningColors, z.seed, 30 + k));
                float sw = big ? Mathf.Min(18f, len * 0.3f) : Mathf.Clamp(w - 2f, 3.5f, 7.5f), sh = sw / 8f;
                float sy = Mathf.Min(3.45f + sh / 2f + 0.3f, eave - sh / 2f - 0.25f);
                int sr = special != null ? special(fromRight ? units - 1 - k : k) : -1;
                if (sy - sh / 2f > 3.25f) sign(d, f, uc, sy, sw, sr >= 0 ? sr : next(BizKind.Shop, z.seed), 0.18f);
            }
        }

        static void ServiceDoor(MeshBuilder d, Frame f, float u) { FrontQuad(d, f, u - 0.5f, u + 0.5f, 0f, 2.1f, 0.05f, new Color(0.45f, 0.46f, 0.47f)); }

        static void WindowBands(MeshBuilder d, Frame f, float half, float eave, float gap)
        {
            if (half < 2f) return;
            int floors = Mathf.Max(1, Mathf.FloorToInt((eave - 0.8f) / 3.6f));
            for (int k = 0; k < floors; k++)
            {
                float y0 = 1.0f + 3.6f * k, y1 = y0 + 1.7f; if (y1 > eave - 0.4f) break;
                foreach (var (u0, u1) in Spans(-half + 0.8f, half - 0.8f, k == 0 ? gap : 0f))
                {
                    if (u1 - u0 < 1f) continue;
                    FrontQuad(d, f, u0, u1, y0, y1, 0.04f, BizGlass);
                    for (float u = u0 + 1.8f; u < u1 - 0.3f; u += 1.8f) FrontQuad(d, f, u - 0.05f, u + 0.05f, y0, y1, 0.06f, BizFrame);
                }
            }
        }

        // the stretch u0..u1 with the middle 2 * gap left out (an entrance)
        static IEnumerable<(float, float)> Spans(float u0, float u1, float gap)
        {
            if (gap <= 0f) { yield return (u0, u1); yield break; }
            yield return (u0, -gap); yield return (gap, u1);
        }

        static void Entrance(MeshBuilder d, Frame f, float u, float w, Color glass)
        {
            FrontQuad(d, f, u - w / 2f - 0.2f, u + w / 2f + 0.2f, -0.2f, 2.9f, 0.04f, BizFrame);
            FrontQuad(d, f, u - w / 2f, u + w / 2f, 0f, 2.7f, 0.06f, glass);
            FrontQuad(d, f, u - 0.03f, u + 0.03f, 0f, 2.7f, 0.07f, BizFrame);
            BoxF(d, f, u - w / 2f - 1.2f, u + w / 2f + 1.2f, 3.0f, 3.25f, 0.02f, 2.4f, new Color(0.80f, 0.80f, 0.78f));
        }

        static void RoofSign(MeshBuilder d, Frame f, float half, float eave, int row, System.Action<MeshBuilder, Frame, float, float, float, int, float> sign)
        {
            float w = Mathf.Clamp(half * 0.9f, 4f, 11f), h = w / 8f;
            float y = eave + 0.55f + 0.25f + h / 2f;
            foreach (float u in new[] { -w / 3f, w / 3f }) BoxF(d, f, u - 0.06f, u + 0.06f, eave + 0.4f, y, -0.05f, 0.05f, BizDark);
            sign(d, f, 0f, y, w, row, 0.12f);
        }

        static void Ribs(MeshBuilder d, Frame f, float half, float eave, Color c)
        {
            if (half < 3f) return;
            for (float u = -half + 1f; u < half - 0.5f; u += 2.0f) FrontQuad(d, f, u - 0.07f, u + 0.07f, 0.1f, eave - 0.1f, 0.03f, c);
        }

        static void Docks(MeshBuilder d, Frame f, float half, float eave, Biz z, System.Action<MeshBuilder, Frame, float, float, float, int, float> sign, System.Func<BizKind, int, int> next)
        {
            float len = half * 2f, dh = Mathf.Min(4.2f, eave - 0.8f);
            int doors = Mathf.Clamp(Mathf.FloorToInt((len - 10f) / 7f), 1, 8);
            // the office door and the sign near one end, the roll-up doors along the rest
            float uo = -half + 3.5f;
            FrontQuad(d, f, uo - 0.5f, uo + 0.5f, 0f, 2.15f, 0.07f, BizDoor);
            FrontQuad(d, f, uo + 1.0f, uo + 3.0f, 1.0f, 2.2f, 0.06f, BizGlass);
            sign(d, f, uo + 1.0f, Mathf.Min(3.2f, eave - 0.8f), Mathf.Min(6f, len * 0.3f), next(BizKind.Industrial, z.seed), 0.15f);
            float span = half - 2f - (uo + 5f);
            for (int k = 0; k < doors; k++)
            {
                float u = uo + 5f + span * (k + 0.5f) / doors;
                FrontQuad(d, f, u - 1.9f, u + 1.9f, 0f, dh + 0.2f, 0.04f, BizDark);
                FrontQuad(d, f, u - 1.75f, u + 1.75f, 0f, dh, 0.06f, new Color(0.72f, 0.73f, 0.72f));
                for (float y = 0.5f; y < dh; y += 0.5f) FrontQuad(d, f, u - 1.75f, u + 1.75f, y, y + 0.04f, 0.07f, new Color(0.58f, 0.59f, 0.58f));
                BoxF(d, f, u - 1.9f, u + 1.9f, -0.1f, 0.25f, 0.04f, 0.5f, BizDark);   // the dock bumper
            }
        }

        static void TallWindows(MeshBuilder d, Frame f, float half, float eave, bool front)
        {
            if (half < 3f) return;
            for (float u = -half + 2.5f; u <= half - 2.4f; u += 4f)
            {
                if (front && Mathf.Abs(u) < 3.5f) continue;
                float y1 = Mathf.Min(eave - 0.6f, 4.6f); if (y1 < 2.5f) return;
                FrontQuad(d, f, u - 0.6f, u + 0.6f, 1.0f, y1 + 0.1f, 0.03f, BizTrim);
                FrontQuad(d, f, u - 0.5f, u + 0.5f, 1.1f, y1, 0.05f, new Color(0.28f, 0.30f, 0.46f));
            }
        }

        static void ChurchFront(MeshBuilder body, MeshBuilder d, Frame f, float half, float eave, Color wall, Biz z, System.Action<MeshBuilder, Frame, float, float, float, int, float> sign, System.Func<BizKind, int, int> next, bool steeple = true)
        {
            FrontQuad(d, f, -1.4f, 1.4f, 0f, 3.0f, 0.05f, BizTrim);
            FrontQuad(d, f, -1.25f, 1.25f, 0f, 2.85f, 0.07f, new Color(0.36f, 0.22f, 0.13f));
            FrontQuad(d, f, -0.03f, 0.03f, 0f, 2.85f, 0.08f, new Color(0.25f, 0.15f, 0.09f));
            if (eave > 4.6f) sign(d, f, 0f, 3.75f, Mathf.Min(6.5f, half * 1.2f), next(BizKind.Church, z.seed), 0.15f);
            if (!steeple) return;   // Round 32 (A5): 75 is the church's hall; the one steeple is 74's
            // the steeple: a square tower over the door, above the ridge, a spire on it (white)
            float ridge = z.height, tw = 1.6f;
            float t0 = ridge + 3.0f;
            BoxF(body, f, -tw, tw, eave - 0.2f, t0, -2f * tw - 0.4f, -0.4f, ChurchWhite);
            var apex = f.P(0f, t0 + 7f, -tw - 0.4f);
            body.Tint = new Color(0.30f, 0.30f, 0.32f);
            Vector3 c0 = f.P(-tw, t0, -0.4f), c1 = f.P(tw, t0, -0.4f), c2 = f.P(tw, t0, -2f * tw - 0.4f), c3 = f.P(-tw, t0, -2f * tw - 0.4f);
            TriFace(body, c0, c1, apex, f.F); TriFace(body, c1, c2, apex, f.R); TriFace(body, c2, c3, apex, -f.F); TriFace(body, c3, c0, apex, -f.R);
            body.Tint = Color.white;
        }

        static void Windows(MeshBuilder d, Frame f, float half, float eave, float every, float w, float h, float gap, Color trim)
        {
            if (half < 1.8f) return;
            int floors = Mathf.Max(1, Mathf.FloorToInt((eave - 0.4f) / 3.0f));
            for (int k = 0; k < floors; k++)
            {
                float y0 = 1.0f + 3.0f * k; if (y0 + h > eave - 0.3f) break;
                for (float u = -half + every / 2f; u <= half - every / 2f + 0.01f; u += every)
                {
                    if (k == 0 && gap > 0f && Mathf.Abs(u) < gap) continue;
                    FrontQuad(d, f, u - w / 2f - 0.08f, u + w / 2f + 0.08f, y0 - 0.08f, y0 + h + 0.08f, 0.03f, trim);
                    FrontQuad(d, f, u - w / 2f, u + w / 2f, y0, y0 + h, 0.05f, BizGlass);
                }
            }
        }

        static void SchoolFront(MeshBuilder d, Frame f, float half, float eave, Biz z, System.Action<MeshBuilder, Frame, float, float, float, int, float> sign)
        {
            Entrance(d, f, 0f, 4f, BizGlass);
            int row = z.type == "kindergarten" ? SignPreschool : z.index == ElementarySchool ? SignElementary : SignMiddle;   // Round 32 (A6): one of each
            float w = Mathf.Min(9f, half * 1.4f);
            if (eave > 3.6f + w / 16f + 0.4f) sign(d, f, 0f, Mathf.Min(eave - w / 16f - 0.3f, 3.6f + w / 16f + 0.15f), w, row, 0.15f);
        }

        static void BayDoors(MeshBuilder d, Frame f, float half, float eave, Biz z, System.Action<MeshBuilder, Frame, float, float, float, int, float> sign, System.Func<BizKind, int, int> next)
        {
            int bays = Mathf.Clamp(Mathf.FloorToInt((half * 2f - 2f) / 5f), 1, 3);
            float dh = Mathf.Min(4.2f, eave - 1.2f);
            for (int k = 0; k < bays; k++)
            {
                float u = -half + 1f + (half * 2f - 2f) * (k + 0.5f) / bays;
                FrontQuad(d, f, u - 2.1f, u + 2.1f, 0f, dh + 0.25f, 0.04f, BizTrim);
                FrontQuad(d, f, u - 1.9f, u + 1.9f, 0f, dh, 0.06f, new Color(0.70f, 0.12f, 0.12f));
                for (float y = 0.8f; y < dh - 0.4f; y += 0.8f) FrontQuad(d, f, u - 1.6f, u + 1.6f, y, y + 0.35f, 0.07f, BizGlass);
            }
            if (eave > dh + 1.0f) sign(d, f, 0f, Mathf.Min(eave - 0.45f, dh + 0.75f), Mathf.Min(6f, half * 1.4f), next(BizKind.Civic, z.seed), 0.15f);
        }

        static void RoofUnits(MeshBuilder d, Biz z, float wallH)
        {
            int n = z.area > 2000f ? 3 : z.area > 600f ? 2 : 1;
            for (int k = 0; k < n; k++)
            {
                var p = z.c + new Vector2(Hash01(z.seed, 60 + k) - 0.5f, Hash01(z.seed, 70 + k) - 0.5f) * Mathf.Sqrt(z.area) * 0.5f;
                if (!InPolygon(p, z.ring) || RingDist(p, z.ring) < 2.5f) continue;
                float y = FloorAt(z, p) + wallH;
                var f = MakeFrame(new Vector3(p.x, y, p.y), Vector2.right, 1f);
                BoxF(d, f, -1.1f, 1.1f, 0f, 1.2f, -0.8f, 0.8f, new Color(0.74f, 0.74f, 0.72f));
                BoxF(d, f, -0.5f, 0.5f, 1.2f, 1.3f, -0.5f, 0.5f, BizDark);
            }
        }

        static void Flagpole(MeshBuilder d, Biz z, TerrainGrid t)
        {
            Vector2 a = z.ring[z.front], b = z.ring[(z.front + 1) % z.ring.Count], m = (a + b) * 0.5f + (b - a).normalized * 7f + z.frontN * 6f;
            if (InPolygon(m, z.ring)) return;
            float gy = Ground(t, m.x, m.y);
            Post(d, new Vector3(m.x, gy - 0.1f, m.y), 9.5f, 0.12f, new Color(0.85f, 0.85f, 0.85f));
            var f = MakeFrame(new Vector3(m.x, gy, m.y), z.frontN, 1f);
            FrontQuad(d, f, 0.06f, 2.0f, 8.0f, 9.2f, 0f, new Color(0.70f, 0.12f, 0.14f));
            FrontQuad(d, MakeFrame(new Vector3(m.x, gy, m.y), -z.frontN, 1f), -2.0f, -0.06f, 8.0f, 9.2f, 0f, new Color(0.70f, 0.12f, 0.14f));
            FrontQuad(d, f, 0.06f, 0.9f, 8.6f, 9.2f, 0.005f, new Color(0.15f, 0.20f, 0.45f));
        }

        // roofs, carports and pavilions: the roof slab on posts (open underneath)
        static void CanopyOnPosts(MeshBuilder mb, Biz z, TerrainGrid t, float wallH)
        {
            float lo = float.MaxValue, sum = 0f; foreach (var p in z.ring) { float g = Ground(t, p.x, p.y); lo = Mathf.Min(lo, g); sum += g; }
            float top = sum / z.ring.Count + Mathf.Max(3.4f, wallH * 0.75f), under = top - 0.45f;
            var col = new Color(0.82f, 0.82f, 0.80f);
            Extrude(mb, z.ring, under, top, col, PickColour(FlatRoofPalette, z.seed, 2), true);
            mb.Tint = Shade(col, 0.8f);
            int s0 = mb.Count; foreach (var p in z.ring) mb.Vertex(new Vector3(p.x, under, p.y));
            foreach (var tri in EarClip(z.ring)) mb.Tri(s0 + tri.x, s0 + tri.y, s0 + tri.z);   // the underside, seen from below
            foreach (var p in z.ring)
            {
                Vector2 c = z.c; var q = p + (c - p).normalized * 0.3f;
                Post(mb, new Vector3(q.x, Ground(t, q.x, q.y) - 0.1f, q.y), under - Ground(t, q.x, q.y) + 0.1f, 0.25f, col);
            }
            mb.Tint = Color.white;
        }
    }
}
