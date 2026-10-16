using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 17, Part B: the shopping center behind Chastain Glen (Dan: "the parking lot is pretty much all grass and should be
// paved instead. There is a small gas station close to the road but separate from building ... and an abandoned bank. I want
// the gas pumps to be breakable"; "I would like explosions but not really do any damage to anything else"; "Store names can
// be faked"). Placed on Dan's approved overhead (TODO.md, Round 17): the L-shaped retail footprint is the center (its big
// south-west block the supermarket, the long arm along the north the row of small shops), the "roof" footprint in the lot's
// north-east the gas station's canopy, the "commercial" footprint by Canton Road the abandoned bank.
//   - The lot (LotRing, by hand from the map data): east of the supermarket and south of the shop row, and (BUG-003, Dan:
//     "this is a parking lot that meets with the main parking lot") the ground south of the supermarket too, one paved area.
//     The ground is graded first (a 6 m smoothing of its own heights, fading out over 6 m past its edge and never onto a
//     road outside it), then drawn as asphalt with a curb band at the edge, painted stalls in double rows with grass islands
//     at the row ends, and a sidewalk along each shop front. The map's service roads run through it as its drive aisles.
//   - Shop fronts (glass, doors, an awning and a sign) on the shop row, the supermarket's front and (BUG-003: "there are
//     shops on this side") the supermarket's south face. Every name is made up (Tools/handmade/make_store_signs.py).
//   - The gas station: the canopy on four columns over two pump islands, four pumps that break like hydrants (a trigger,
//     a little speed lost, pieces flying) and then explode for show (PumpBlast: fire, a bang, smoke; no force, no damage).
//   - The bank: drab, its windows and door boarded up, letters missing from its sign, a FOR LEASE banner.
// Nothing here is a ramp or a jump; the islands and sidewalks are flat paint-thin (3.5 cm, like Chastain Glen's curbs).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // Round 18 (A): the four corners along road b (the service road west of the supermarket) moved onto its north-east edge
        // (2.3 m from its centerline, just under its asphalt) and one more in the corner where b joins road a; the lot used to
        // stop 1.8 to 3.4 m short of b with a ragged grass strip between
        static readonly Vector2[] LotRing =
        {
            new Vector2(3220.5f, 1061.1f), new Vector2(3223.8f, 1048.4f), new Vector2(3238.7f, 1034.5f), new Vector2(3255.7f, 1019.5f), new Vector2(3260.8f, 1015.3f), new Vector2(3271f, 1019f),
            new Vector2(3289f, 1015f), new Vector2(3312f, 1040f), new Vector2(3340f, 1056f), new Vector2(3398f, 1072f), new Vector2(3430f, 1080f),
            new Vector2(3447f, 1084f), new Vector2(3487f, 1093f), new Vector2(3485f, 1112f), new Vector2(3480f, 1135f), new Vector2(3475f, 1158f),
            new Vector2(3467f, 1196f), new Vector2(3446f, 1212f), new Vector2(3416f, 1207f), new Vector2(3284.9f, 1179.1f), new Vector2(3277.2f, 1167.8f),
            new Vector2(3280.5f, 1152.2f), new Vector2(3286.5f, 1153.6f), new Vector2(3304.1f, 1075.0f), new Vector2(3297.3f, 1073.4f),
            new Vector2(3300.9f, 1058.4f), new Vector2(3241.1f, 1045.8f), new Vector2(3237.8f, 1061.4f), new Vector2(3231.1f, 1060.1f),
        };
        static readonly Vector2 CenterAt = new Vector2(3270f, 1150f), CanopyAt = new Vector2(3418f, 1164f), BankAt = new Vector2(3445f, 1113f);
        const float LotSigma = 6f, LotFadeOut = 6f, LotStep = 1f;

        // rows of Assets/HandMade/ShoppingCenter/StoreSigns.png (same order as make_store_signs.py)
        const int SignMarket = 0, SignPharmacy = 1, SignRowFirst = 2, SignRowCount = 10, SignSouthFirst = 12, SignSouthCount = 4,
                  SignFuel = 16, SignBank = 17, SignLease = 18, SignRows = 24;

        sealed class ShoppingCenter
        {
            public int Center = -1, Canopy = -1, Bank = -1;
            public List<Vector2> Lot;
            public bool Owns(int i) => i == Canopy || i == Bank || (Park != null && i == Center);   // built here; the center itself is an ordinary building plus fronts (Round 36: in the stunt park it is built there, MapBuilder.StuntParkStores.cs)
            public string Log = "";

            public static ShoppingCenter Find(MapData map)
            {
                var s = new ShoppingCenter { Lot = LotRing.ToList() };
                for (int i = 0; i < map.buildings.Length; i++)
                {
                    var b = map.buildings[i]; var ring = ToPoints(b.footprint); if (ring.Count < 3) continue;
                    Vector2 c = Vector2.zero; foreach (var p in ring) c += p; c /= ring.Count;
                    if (b.type == "retail" && InPolygon(CenterAt, ring)) s.Center = i;
                    else if (b.type == "roof" && (c - CanopyAt).sqrMagnitude < 36f) s.Canopy = i;
                    else if (b.type == "commercial" && (c - BankAt).sqrMagnitude < 36f) s.Bank = i;
                }
                if (s.Center < 0 || s.Canopy < 0 || s.Bank < 0) Debug.LogWarning($"[Round17] shopping center: footprints found center {s.Center}, canopy {s.Canopy}, bank {s.Bank}");
                return s;
            }

            // no tree on the lot (1.5 m round it) or under the canopy
            public bool RejectTree(float x, float z)
            {
                var p = new Vector2(x, z);
                if (InPolygon(p, Lot)) return true;
                for (int i = 0; i < Lot.Count; i++) if (SegDist(p, Lot[i], Lot[(i + 1) % Lot.Count]) < 1.5f) return true;
                return false;
            }
        }

        // ------------------------------------------------------------ grading the lot (before anything samples the ground)

        static void GradeLot(MapData map, TerrainGrid terrain, ShoppingCenter sc)
        {
            var lot = sc.Lot;
            float x0 = lot.Min(p => p.x), x1 = lot.Max(p => p.x), z0 = lot.Min(p => p.y), z1 = lot.Max(p => p.y);
            float pad = LotFadeOut + 3f * LotSigma + 2f;
            var g = new HeightGrid(new Vector2(x0 - pad, z0 - pad), Mathf.CeilToInt((x1 - x0 + 2f * pad) / LotStep) + 1, Mathf.CeilToInt((z1 - z0 + 2f * pad) / LotStep) + 1, LotStep);
            // roads that run through the lot (a point inside it) are graded with it; every other road is kept as it is
            var centre = new Vector2((x0 + x1) / 2f, (z0 + z1) / 2f);
            float reach = Mathf.Max(x1 - x0, z1 - z0);
            var outside = RoadSegsNear(map, centre, reach, ri => !ToPoints(map.roads[ri].points).Any(q => InPolygon(q, lot)));
            var h = g.Map((x, z) => terrain.Height(x, z));
            var inside = g.Map((x, z) => InPolygon(new Vector2(x, z), lot) ? 1f : 0f);
            var blur = g.NormalizedBlur(h, inside, LotSigma);
            var weight = g.Map((x, z) =>
            {
                var p = new Vector2(x, z);
                if (inside[g.I(x), g.J(z)] > 0f) return 1f;
                float d = float.MaxValue; for (int i = 0; i < lot.Count; i++) d = Mathf.Min(d, SegDist(p, lot[i], lot[(i + 1) % lot.Count]));
                if (d >= LotFadeOut) return 0f;
                return Smooth01(1f - d / LotFadeOut) * Smooth01((Clearance(outside, p) - 1f) / 3f);
            });
            float most = 0f;
            EditTerrain(terrain, Rect.MinMaxRect(x0 - LotFadeOut, z0 - LotFadeOut, x1 + LotFadeOut, z1 + LotFadeOut), (x, z, y) =>
            {
                float w = g.Sample(weight, x, z);
                if (w <= 1e-3f) return y;
                float ny = y + w * (g.Sample(blur, x, z) - y);
                most = Mathf.Max(most, Mathf.Abs(ny - y));
                return ny;
            });
            // the gas station stands on a level pad: the ground under the canopy and 1 m round it at its mean height, blending
            // back to the graded lot over 12 m (a gentle slope, so the service roads beside it stay smooth)
            if (sc.Canopy >= 0 && BestRectangle(ToPoints(map.buildings[sc.Canopy].footprint), out var cc, out var cu, out var cv, out var hl, out var hw))
            {
                float sum = 0f; int n = 0;
                for (float a = -hl - 1f; a <= hl + 1f; a += 1f) for (float b = -hw - 1f; b <= hw + 1f; b += 1f) { var q = cc + cu * a + cv * b; sum += terrain.Height(q.x, q.y); n++; }
                float padY = sum / n, r = hl + hw + 15f;
                EditTerrain(terrain, Rect.MinMaxRect(cc.x - r, cc.y - r, cc.x + r, cc.y + r), (x, z, y) =>
                {
                    var q = new Vector2(x, z) - cc;
                    float ou = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(q, cu)) - hl - 1f), ov = Mathf.Max(0f, Mathf.Abs(Vector2.Dot(q, cv)) - hw - 1f);
                    float w = Smooth01(1f - Mathf.Sqrt(ou * ou + ov * ov) / 12f);
                    return y + w * (padY - y);
                });
            }
            sc.Log += $"lot {Mathf.Abs(PolyArea(lot)):0} m2 graded (sigma {LotSigma:0} m, largest change {most:0.00} m); ";
        }

        // ------------------------------------------------------------ Round 18 (A): the service roads joined to the lot

        // Dan's session 2026-10-10_20-18-10-199_3f4ac2, BUG-001 ("this drive way should enter the parking lot. the othe driveway
        // should continue all the way around the building until it matches the other side of the parking lot"). The map data
        // leaves gaps there; these pieces are added to the map's own service roads in the build (generated data untouched):
        //   a: the drive up from Chastain Corner (OSM 564629091) stopped 10 m short of the lot: it now runs into the lot.
        //   b: the service road west of the supermarket (OSM 564629093) now goes on round the back of the building (west, then
        //      the north-west corner) to the road behind the shop row (OSM 564629095), on the paved ground the elevation data
        //      already shows there (flat, under 0.2 m across 5 m), and at its south end it meets a.
        //   c: the stub at the shop row's east end (OSM 564629086) now runs down into the main lot.
        // The pieces lie on the ground as it is (the grass gaps were the ground's own slope, no step). The lot's edge along b is
        // levelled to b's own height so the two meet flush, the corner where a, b and the lot meet is smoothed, and a's climb
        // into the lot is one smooth curve (GradeRoadA). Edge paint on these roads stops on the lot and where they meet.
        static readonly (long osm, bool atStart, Vector2[] add)[] ServiceRoadJoins =
        {
            (564629091, true,  new[] { new Vector2(3262.5f, 1025f), new Vector2(3262.8f, 1018f), new Vector2(3263.3f, 1013f) }),
            (564629093, true,  new[] { new Vector2(3278f, 1203f), new Vector2(3270f, 1199.5f), new Vector2(3262f, 1192f), new Vector2(3256f, 1182f),
                                       new Vector2(3252f, 1170f), new Vector2(3252.5f, 1157f), new Vector2(3248f, 1152f), new Vector2(3216f, 1144.5f),
                                       new Vector2(3209f, 1141f), new Vector2(3204f, 1134f) }),
            (564629093, false, new[] { new Vector2(3260f, 1013f), new Vector2(3263.3f, 1011.5f) }),
            (564629086, true,  new[] { new Vector2(3426.5f, 1205f), new Vector2(3426f, 1218f) }),
            // Round 18 (2a): Dan's session 2026-10-10_23-08-26-878_0e77b1, BUG-002 ("connect this as a roads"): the drive in from
            // Canton Road north of the gas station (OSM 564629097) ended in a rounded stub 17 m short of the lot's north-east
            // corner; it now runs on into the lot (the ground falls about 9 % along it, no step). The only center road that stopped
            // short: every other one already reaches the lot, and the drives south of Chastain Corner and east of Canton Road
            // belong to other sites.
            (564629097, false, new[] { new Vector2(3441.5f, 1223f), new Vector2(3439.5f, 1215f), new Vector2(3438.5f, 1206f) }),
        };
        const long RoadB = 564629093;
        static readonly long[] JoinedRoads = { 564629091, 564629093, 564629086, 564629095, 564629097 };   // a, b, c, the road behind the shop row, BUG-002's drive

        // Round 18 (2a), BUG-002 ("The drive ways and darking lots shoutl be the same color"): the center's own service roads (the
        // joined ones and every one that runs onto the lot) are drawn in the lot's asphalt, not the lighter service shade
        static readonly Color LotAsphalt = new Color(0.235f, 0.235f, 0.245f);
        static bool IsCenterRoad(Road r) => r.@class == "service" &&
            (JoinedRoads.Contains(r.osm_id) || ToPoints(r.points).Any(q => InPolygon(q, LotRing.ToList())));
        static List<(long osm, Vector2 a, Vector2 b, float half)> joinedSegs = new List<(long, Vector2, Vector2, float)>();

        static void JoinServiceRoads(MapData map, TerrainGrid terrain, ShoppingCenter sc)
        {
            var log = new List<string>();
            // road b's centerline as it was, for levelling the lot's edge beside it
            var bOld = map.roads.Where(r => r.osm_id == RoadB).Select(r => Densify(ToPoints(r.points), 1f)).FirstOrDefault();
            foreach (var j in ServiceRoadJoins)
            {
                var r = map.roads.FirstOrDefault(x => x.osm_id == j.osm);
                if (r == null) { Debug.LogWarning($"[Round18] service road {j.osm} not found; not joined"); continue; }
                var pts = ToPoints(r.points);
                Vector2 end = j.atStart ? pts[0] : pts[pts.Count - 1];
                if (j.atStart) pts.InsertRange(0, j.add); else pts.AddRange(j.add);
                r.points = pts.SelectMany(p => new[] { p.x, p.y }).ToArray();
                float len = 0f; var piece = new List<Vector2> { end }; piece.AddRange(j.atStart ? Enumerable.Reverse(j.add) : j.add);
                for (int i = 1; i < piece.Count; i++) len += Vector2.Distance(piece[i - 1], piece[i]);
                log.Add($"{j.osm} +{len:0} m");
            }
            joinedSegs.Clear();
            foreach (var r in map.roads.Where(x => JoinedRoads.Contains(x.osm_id)))
            {
                var p = ToPoints(r.points);
                for (int i = 1; i < p.Count; i++) joinedSegs.Add((r.osm_id, p[i - 1], p[i], r.width * 0.5f));
            }

            // the lot's edge beside road b: within 6 m of b's centerline and on the lot, the ground goes to b's own height
            // (all of it up to b's edge, fading out 3.5 m into the lot)
            float edge = 0f;
            if (bOld != null)
            {
                var bh = bOld.Select(p => terrain.Height(p.x, p.y)).ToArray();
                Vector2 lo = new Vector2(sc.Lot.Min(p => p.x), sc.Lot.Min(p => p.y)), hi = new Vector2(sc.Lot.Max(p => p.x), 1070f);
                EditTerrain(terrain, Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y), (x, z, y) =>
                {
                    var p = new Vector2(x, z);
                    if (!InPolygon(p, sc.Lot) && RingDist(p, sc.Lot) > 0.7f) return y;
                    float best = float.MaxValue, by = y;
                    for (int i = 1; i < bOld.Count; i++)
                    {
                        Vector2 a = bOld[i - 1], b = bOld[i], ab = b - a; float tt = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                        float d = Vector2.Distance(p, a + ab * tt); if (d < best) { best = d; by = Mathf.Lerp(bh[i - 1], bh[i], tt); }
                    }
                    float w = Smooth01((6f - best) / 3.5f);
                    if (w <= 0f) return y;
                    float ny = y + w * (by - y);
                    edge = Mathf.Max(edge, Mathf.Abs(ny - y));
                    return ny;
                });
            }
            Debug.Log($"[Round18] BUG-001 service roads joined: {string.Join(", ", log)}; the lot's edge beside road b levelled to it (largest change {edge:0.00} m)");
            // the corner where a and b meet the lot: the pavement (the roads and the lot) smoothed as at Round 17's junctions, so
            // the lot and the road surfaces follow the ground without it showing through
            var lotList = LotRing.ToList();
            SmoothSpot(map, terrain, (new Vector2(3259f, 1018f), 14f, 4f, 6f, "Round 18 (A) BUG-001 the lot's south-west corner, where roads a and b meet it"), q => InPolygon(q, lotList));
            GradeRoadA(terrain);
            GradeDriveIn(terrain, Bug002Profile, "BUG-002 the drive from Canton Road into the lot's north-east corner");
        }

        // Road a climbed most of its 3 m in its last 15 m (14 %) and met the flat lot in a crest that threw a car at 16 m/s.
        // From Chastain Corner's edge to 7 m into the lot it now follows one smooth curve (cubic: Chastain Corner's own grade at
        // the bottom, level at the top; about 8 % in the middle, its crest radius over 80 m, so it holds a car at 28 m/s), across
        // the road and 0.5 m past its edges (the ground moved up or down with the line), fading out over 9 m. The lot and the
        // road beyond are untouched.
        static readonly Vector2[] RoadAProfile = { new Vector2(3275.5f, 997.5f), new Vector2(3264f, 1009f), new Vector2(3263.3f, 1013f), new Vector2(3262.8f, 1018f), new Vector2(3262.5f, 1025f), new Vector2(3262.3f, 1032f) };

        // Round 18 (2a): BUG-002's drive (OSM 564629097) the same way, from 20 m up it to 7 m into the lot (the lot's own grading
        // fades out across its new end, which lifted a car's wheels for 0.14 s at 12 m/s)
        static readonly Vector2[] Bug002Profile = { new Vector2(3464f, 1231f), new Vector2(3450f, 1229.4f), new Vector2(3444.3f, 1228.7f), new Vector2(3441.5f, 1223f), new Vector2(3439.5f, 1215f), new Vector2(3438.5f, 1206f), new Vector2(3438f, 1204f) };

        static void GradeRoadA(TerrainGrid terrain) => GradeDriveIn(terrain, RoadAProfile, "BUG-001 road a");

        static void GradeDriveIn(TerrainGrid terrain, Vector2[] profile, string what)
        {
            var line = Densify(profile.ToList(), 0.5f);
            var s = new float[line.Count]; for (int i = 1; i < line.Count; i++) s[i] = s[i - 1] + Vector2.Distance(line[i - 1], line[i]);
            float L = s[line.Count - 1], h0 = terrain.Height(line[0].x, line[0].y), h1 = terrain.Height(line[line.Count - 1].x, line[line.Count - 1].y);
            var yc = line.Select(p => terrain.Height(p.x, p.y)).ToArray();   // the ground along the line as it was
            var q = PathAt(line, s, 3f);
            float m0 = (terrain.Height(q.x, q.y) - h0) / 3f;   // the grade coming off Chastain Corner
            float H(float u)
            {
                float t = Mathf.Clamp01(u / L), t2 = t * t, t3 = t2 * t;
                return (2f * t3 - 3f * t2 + 1f) * h0 + (t3 - 2f * t2 + t) * L * m0 + (3f * t2 - 2f * t3) * h1;
            }
            const float Inner = 3f, Outer = 12f;   // a wide fade: the stall island and road b beside it stay on gentle ground
            Vector2 lo = new Vector2(line.Min(p => p.x), line.Min(p => p.y)) - Vector2.one * Outer, hi = new Vector2(line.Max(p => p.x), line.Max(p => p.y)) + Vector2.one * Outer;
            float most = 0f;
            EditTerrain(terrain, Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y), (x, z, y) =>
            {
                var p = new Vector2(x, z); float best = float.MaxValue, at = 0f, was = y;
                for (int i = 1; i < line.Count; i++)
                {
                    Vector2 a = line[i - 1], b = line[i], ab = b - a; float tt = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    float d = Vector2.Distance(p, a + ab * tt); if (d < best) { best = d; at = s[i - 1] + tt * ab.magnitude; was = Mathf.Lerp(yc[i - 1], yc[i], tt); }
                }
                if (at <= 0.01f || at >= L - 0.01f) return y;   // past either end: Chastain Corner and the lot as they are
                float w = Smooth01((Outer - best) / (Outer - Inner));
                if (w <= 0f) return y;
                // the line is lifted or lowered onto the curve and the ground beside it with it (its cross slope kept), so the change
                // is nothing at both ends and nothing shows where it stops
                float ny = y + w * (H(at) - was);
                most = Mathf.Max(most, Mathf.Abs(ny - y));
                return ny;
            });
            Debug.Log($"[Round18] {what}: one curve from {h0:0.00} m (grade {m0 * 100f:0} %) to {h1:0.00} m over {L:0} m, steepest {MaxGrade(H, L) * 100f:0} %, largest change {most:0.00} m");
        }

        static Vector2 PathAt(List<Vector2> line, float[] s, float u)
        {
            for (int i = 1; i < line.Count; i++) if (s[i] >= u) return Vector2.Lerp(line[i - 1], line[i], (u - s[i - 1]) / Mathf.Max(s[i] - s[i - 1], 1e-4f));
            return line[line.Count - 1];
        }

        static float MaxGrade(System.Func<float, float> h, float L)
        {
            float g = 0f; for (float u = 0f; u + 0.5f <= L; u += 0.5f) g = Mathf.Max(g, Mathf.Abs(h(u + 0.5f) - h(u)) / 0.5f);
            return g;
        }

        // BuildRoads' paint rule for the joined roads: no edge line on the lot, nor inside another joined road where they meet
        // (they are all unnamed service roads, so the usual rule takes them for one street and keeps the lines)
        // (p is a point on an edge line; it stops within the two half widths of the other road's centerline)
        static bool JoinPaintStops(long osm, Vector2 p, float half)
        {
            if (!JoinedRoads.Contains(osm)) return false;
            if (InPolygon(p, LotRing.ToList()) || RingDist(p, LotRing.ToList()) < 0.5f) return true;
            foreach (var s in joinedSegs) if (s.osm != osm && SegDist(p, s.a, s.b) < s.half + half + 0.1f) return true;
            return false;
        }

        // ------------------------------------------------------------ building it (after the ordinary buildings)

        static void BuildShoppingCenter(MapData map, TerrainGrid t, Materials mats, Transform parent, ShoppingCenter sc)
        {
            var root = new GameObject("ShoppingCenter").transform; root.SetParent(parent, false);
            var ground = new MeshBuilder(); var walks = new MeshBuilder(); var paint = new MeshBuilder(); var solid = new MeshBuilder(); var props = new MeshBuilder();
            var signMat = StoreSignMaterial();
            var retail = sc.Center >= 0 ? ToPoints(map.buildings[sc.Center].footprint) : null;
            float retailTop = 0f;
            if (retail != null) { float sum = 0f; foreach (var p in retail) sum += Ground(t, p.x, p.y); retailTop = sum / retail.Count + map.buildings[sc.Center].height; }

            // --- the lot's asphalt and its edge (a pale curb band where it meets grass, not along the shop walls)
            const float LotLift = 0.045f, WalkLift = 0.08f, PaintLift = 0.06f;
            ground.Tint = LotAsphalt;
            DrapePoly(ground, sc.Lot, 2f, LotLift, t, 24);
            walks.Tint = new Color(0.70f, 0.69f, 0.66f);
            int edges = 0;
            var edgeRoads = RoadSegsNear(map, (LotRing[0] + LotRing[12]) / 2f, 260f, _ => true);
            for (int i = 0; i < sc.Lot.Count; i++)
            {
                Vector2 a = sc.Lot[i], b = sc.Lot[(i + 1) % sc.Lot.Count], mid = (a + b) / 2f;
                if (retail != null && RingDist(mid, retail) < 1.5f) continue;
                // Round 18 (A): no curb where a road meets the lot or runs into it (asphalt to asphalt, flush)
                int k = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
                for (int j = 0; j < k; )
                {
                    if (Clearance(edgeRoads, Vector2.Lerp(a, b, (j + 0.5f) / k)) < 0.5f) { j++; continue; }
                    int e = j; while (e + 1 < k && Clearance(edgeRoads, Vector2.Lerp(a, b, (e + 1.5f) / k)) >= 0.5f) e++;
                    Band(walks, Vector2.Lerp(a, b, j / (float)k), Vector2.Lerp(a, b, (e + 1) / (float)k), 0.35f, WalkLift, t, sc.Lot);
                    j = e + 1;
                }
                edges++;
            }

            // --- shop fronts: (face start, face end, sign rows, kind)
            var fronts = new List<(Vector2 a, Vector2 b, int first, int count, bool market)>();
            if (retail != null)
            {
                fronts.Add((new Vector2(3416f, 1207f), new Vector2(3284.9f, 1179.1f), SignRowFirst, SignRowCount, false));   // the shop row, east to west
                fronts.Add((new Vector2(3304.1f, 1075.0f), new Vector2(3286.5f, 1153.6f), SignMarket, 1, true));              // the supermarket's front
                fronts.Add((new Vector2(3241.1f, 1045.8f), new Vector2(3300.9f, 1058.4f), SignSouthFirst, SignSouthCount, false)); // BUG-003: its south face
            }
            var walkStrips = new List<(Vector2 a, Vector2 b, Vector2 n)>();
            int shops = 0;
            foreach (var f in fronts)
            {
                Vector2 d = (f.b - f.a).normalized, n = new Vector2(d.y, -d.x);
                if (InPolygon((f.a + f.b) / 2f + n * 0.5f, retail)) n = -n;   // n points out of the building, onto the lot
                float len = Vector2.Distance(f.a, f.b);
                walkStrips.Add((f.a, f.b, n));
                SidewalkStrip(walks, f.a, f.b, n, 3.5f, WalkLift, t);
                if (Park != null && f.first != SignSouthFirst) continue;   // Round 36: the stunt park builds these fronts (smash-through windows)
                if (f.market) { MarketFront(solid, root, signMat, f.a, d, n, len, t, retailTop); shops++; continue; }
                float w = len / f.count;
                for (int k = 0; k < f.count; k++) { ShopFront(solid, root, signMat, f.a + d * (w * (k + 0.5f)), d, n, w, f.first + k, t, retailTop, k); shops++; }
            }

            // --- stalls: double rows along the shop row (u), running out from the supermarket's front
            var keepOff = new List<(Vector2 c, float r)>();
            Rect canopyR = default; Vector2 cc = default, cu = default, cv = default; float chl = 0f, chw = 0f;
            if (sc.Canopy >= 0 && BestRectangle(ToPoints(map.buildings[sc.Canopy].footprint), out cc, out cu, out cv, out chl, out chw)) { }
            var bankRing = sc.Bank >= 0 ? ToPoints(map.buildings[sc.Bank].footprint) : null;
            var roads = RoadSegsNear(map, (LotRing[0] + LotRing[12]) / 2f, 260f, _ => true);
            Vector2 U = (new Vector2(3416f, 1207f) - new Vector2(3284.9f, 1179.1f)).normalized, V = new Vector2(-U.y, U.x);   // V points north
            float vTop = Vector2.Dot(new Vector2(3284.9f, 1179.1f), V);
            bool StallOk(Vector2 p)
            {
                if (!InPolygon(p, sc.Lot)) return false;
                for (int i = 0; i < sc.Lot.Count; i++) if (SegDist(p, sc.Lot[i], sc.Lot[(i + 1) % sc.Lot.Count]) < 1.5f) return false;
                if (Clearance(roads, p) < 1.2f) return false;
                foreach (var s in walkStrips) if (SegDist(p, s.a, s.b) < 3.5f + 6.5f) return false;   // the sidewalk and a fire lane in front of it
                if (sc.Canopy >= 0) { var q = p - cc; if (Mathf.Abs(Vector2.Dot(q, cu)) < chl + 7f && Mathf.Abs(Vector2.Dot(q, cv)) < chw + 7f) return false; }
                if (bankRing != null && (InPolygon(p, bankRing) || RingDist(p, bankRing) < 5f)) return false;
                return true;
            }
            const float Bay = 5.4f, Aisle = 7.0f, Pitch = 2.8f, Island = 2.0f;
            int stalls = 0, islands = 0;
            float uMin = sc.Lot.Min(p => Vector2.Dot(p, U)), uMax = sc.Lot.Max(p => Vector2.Dot(p, U)), vMin = sc.Lot.Min(p => Vector2.Dot(p, V));
            for (float vRow = vTop - 3.5f - Aisle - 2f * Bay; vRow > vMin && Park == null; vRow -= 2f * Bay + Aisle)   // Round 36: no stalls in the stunt park
            {
                // one double row: stalls from vRow to vRow + 2 Bay, a line between the halves
                var ok = new List<bool>(); var us = new List<float>();
                for (float u = Mathf.Ceil(uMin / Pitch) * Pitch; u <= uMax; u += Pitch)
                {
                    bool good = true;
                    for (float vv = vRow; vv <= vRow + 2f * Bay + 0.01f; vv += Bay) if (!StallOk(U * u + V * vv)) { good = false; break; }
                    ok.Add(good); us.Add(u);
                }
                for (int i = 0; i < ok.Count; )
                {
                    if (!ok[i]) { i++; continue; }
                    int j = i; while (j + 1 < ok.Count && ok[j + 1]) j++;
                    if (j - i >= 5)
                    {
                        // islands take the end positions of the run; lines between
                        float ua = us[i] + Island, ub = us[j] - Island;
                        for (float u = ua; u <= ub + 0.01f; u += Pitch)
                        {
                            PaintLine(paint, U * u + V * vRow, U * u + V * (vRow + 2f * Bay), 0.12f, PaintLift, t);
                            stalls += 2;
                        }
                        PaintLine(paint, U * ua + V * (vRow + Bay), U * ub + V * (vRow + Bay), 0.12f, PaintLift, t);
                        foreach (float ue in new[] { us[i], us[j] }) { IslandAt(walks, ground, U * ue + V * (vRow + Bay), U, V, Island * 0.8f, 2f * Bay - 0.6f, WalkLift, t); islands++; }
                        stalls -= 2;
                    }
                    i = j + 1;
                }
            }

            // --- the gas station: canopy, columns, islands, pumps that break and explode for show
            int pumps = 0;
            if (sc.Canopy >= 0)
            {
                float floor = Ground(t, cc.x, cc.y);   // the pad is level (GradeLot)
                var f = new Frame { O = new Vector3(cc.x, floor, cc.y), R = new Vector3(cu.x, 0f, cu.y), F = new Vector3(cv.x, 0f, cv.y), M = 1f };
                var white = new Color(0.93f, 0.93f, 0.92f); var red = new Color(0.78f, 0.12f, 0.14f); var conc = new Color(0.66f, 0.65f, 0.62f);
                const float roof0 = 5.0f, roof1 = 5.9f;
                BoxF(solid, f, -chl, chl, roof0, roof1, -chw, chw, white);
                BoxF(solid, f, -chl - 0.05f, chl + 0.05f, roof0 + 0.15f, roof1 - 0.05f, -chw - 0.05f, chw + 0.05f, red);   // the fascia band round it
                var roofCol = new GameObject("Canopy roof collider"); roofCol.transform.SetParent(root, false);
                roofCol.transform.SetPositionAndRotation(f.P(0f, (roof0 + roof1) / 2f, 0f), Quaternion.LookRotation(f.F, Vector3.up));
                roofCol.AddComponent<BoxCollider>().size = new Vector3(2f * chl, roof1 - roof0, 2f * chw);
                var pumpSet = new Breakables();
                foreach (float isl in new[] { -chw * 0.45f, chw * 0.45f })
                {
                    float y0 = Lowest(t, f, -chl * 0.45f, chl * 0.45f, isl) - floor;
                    BoxF(solid, f, -chl * 0.45f - 0.6f, chl * 0.45f + 0.6f, y0 - 0.2f, 0.035f, isl - 0.55f, isl + 0.55f, conc);
                    foreach (float uc in new[] { -chl * 0.45f, chl * 0.45f })
                    {
                        BoxF(solid, f, uc - 0.25f, uc + 0.25f, y0 - 0.2f, roof0, isl - 0.25f, isl + 0.25f, white);
                        var col = new GameObject("Canopy column"); col.transform.SetParent(root, false);
                        col.transform.SetPositionAndRotation(f.P(uc, roof0 / 2f, isl), Quaternion.LookRotation(f.F, Vector3.up));
                        col.AddComponent<BoxCollider>().size = new Vector3(0.5f, roof0 + 0.4f, 0.5f);
                    }
                    foreach (float up in new[] { -chl * 0.18f, chl * 0.18f })
                    {
                        var at = f.P(up, 0.035f, isl); var p2 = new Vector2(at.x, at.z);
                        var mb = pumpSet.Begin(p2);
                        var pf = f; pf.O = at;
                        pumpSet.Piece(() => BoxF(mb, pf, -0.42f, 0.42f, 0f, 0.95f, -0.26f, 0.26f, new Color(0.86f, 0.86f, 0.84f)), at.y + 0.1f);
                        pumpSet.Piece(() =>
                        {
                            BoxF(mb, pf, -0.42f, 0.42f, 0.95f, 1.75f, -0.24f, 0.24f, red);
                            BoxF(mb, pf, -0.28f, 0.28f, 1.12f, 1.45f, -0.27f, 0.27f, new Color(0.07f, 0.09f, 0.11f));   // the screens, both sides
                            BoxF(mb, pf, -0.46f, 0.46f, 1.75f, 1.9f, -0.28f, 0.28f, white);
                        }, at.y + 0.1f);
                        pumpSet.Piece(() =>
                        {
                            BoxF(mb, pf, 0.42f, 0.52f, 0.7f, 1.0f, -0.08f, 0.08f, new Color(0.08f, 0.08f, 0.08f));    // the nozzles in their holsters
                            BoxF(mb, pf, -0.52f, -0.42f, 0.7f, 1.0f, -0.08f, 0.08f, new Color(0.08f, 0.08f, 0.08f));
                        }, at.y + 0.1f);
                        pumpSet.End(at + Vector3.up * 0.95f, f.R, 1.0f, 1.9f, 0.6f);
                        pumps++;
                    }
                }
                var pumpRoot = pumpSet.ToObjects("GasPumps", mats.House, root, 0.5f, BreakSounds.Kind.Sign);
                foreach (var fc in pumpRoot.GetComponentsInChildren<FenceChunk>()) fc.gameObject.AddComponent<PumpChunk>();
                var blast = new GameObject("PumpBlast").AddComponent<PumpBlast>(); blast.transform.SetParent(root, false);
                blast.particleMaterial = SpraySoftMaterial != null ? SpraySoftMaterial : MakeSprayMaterial();
                // the brand on both long sides of the fascia
                if (signMat != null)
                    foreach (float s in new[] { -1f, 1f })
                    {
                        var n3 = f.F * s;
                        AtlasPlate(root, f.P(0f, (roof0 + roof1) / 2f + 0.02f, s * (chw + 0.06f)), -n3, 8f, 0.62f, SignFuel, signMat);
                    }
            }

            // --- the abandoned bank
            if (bankRing != null) BuildBank(map, t, mats, root, solid, signMat, bankRing, map.buildings[sc.Bank]);

            ground.ToObject("ShoppingLot", mats.Driveway, root, false, false);
            walks.ToObject("ShoppingSidewalks", mats.Sidewalk, root, false, false);
            paint.ToObject("ShoppingStalls", mats.Paint, root, false, false);
            solid.ToObject("ShoppingFronts", mats.House, root, false, true);
            sc.Log += $"{shops} shop fronts (the supermarket's included), {stalls} stalls, {islands} islands, {edges} curb edges, {pumps} pumps under the canopy, the bank boarded up";
            Debug.Log("[Round17] shopping center: " + sc.Log);
        }

        static float Lowest(TerrainGrid t, Frame f, float u0, float u1, float v)
        {
            float lo = float.MaxValue;
            for (float u = u0; u <= u1 + 0.01f; u += (u1 - u0) / 4f) { var q = f.P(u, 0f, v); lo = Mathf.Min(lo, Ground(t, q.x, q.z)); }
            return lo;
        }

        static float RingDist(Vector2 p, List<Vector2> ring)
        {
            float d = float.MaxValue; for (int i = 0; i < ring.Count; i++) d = Mathf.Min(d, SegDist(p, ring[i], ring[(i + 1) % ring.Count]));
            return d;
        }

        // a strip of width w from a to b, draped (one quad per metre), on the lot's side of the line
        static void Band(MeshBuilder mb, Vector2 a, Vector2 b, float w, float lift, TerrainGrid t, List<Vector2> lot)
        {
            Vector2 d = (b - a).normalized, n = new Vector2(-d.y, d.x);
            if (!InPolygon((a + b) / 2f + n * 0.5f, lot)) n = -n;
            Strip(mb, a, b, n, 0f, w, lift, t);
        }

        static void Strip(MeshBuilder mb, Vector2 a, Vector2 b, Vector2 n, float v0, float v1, float lift, TerrainGrid t)
        {
            float len = Vector2.Distance(a, b); int k = Mathf.Max(1, Mathf.CeilToInt(len)); Vector2 d = (b - a) / k;
            for (int i = 0; i < k; i++)
            {
                Vector2 p0 = a + d * i, p1 = a + d * (i + 1);
                Vector3 W(Vector2 q) => new Vector3(q.x, Ground(t, q.x, q.y) + lift, q.y);
                Face(mb, W(p0 + n * v0), W(p1 + n * v0), W(p1 + n * v1), W(p0 + n * v1), Vector3.up);
            }
        }

        // the sidewalk in front of a face: concrete, 3.5 cm above the lot, with its curb face
        static void SidewalkStrip(MeshBuilder mb, Vector2 a, Vector2 b, Vector2 n, float w, float lift, TerrainGrid t)
        {
            Vector2 d = (b - a).normalized;
            a -= d * 0.5f; b += d * 0.5f;
            mb.Tint = new Color(0.72f, 0.71f, 0.68f);
            Strip(mb, a, b, n, -0.3f, w, lift, t);
            mb.Tint = new Color(0.60f, 0.59f, 0.56f);
            float len = Vector2.Distance(a, b); int k = Mathf.Max(1, Mathf.CeilToInt(len)); Vector2 s = (b - a) / k;
            for (int i = 0; i < k; i++)
            {
                Vector2 p0 = a + s * i + n * w, p1 = a + s * (i + 1) + n * w;
                float g0 = Ground(t, p0.x, p0.y), g1 = Ground(t, p1.x, p1.y);
                Face(mb, new Vector3(p0.x, g0 - 0.05f, p0.y), new Vector3(p1.x, g1 - 0.05f, p1.y), new Vector3(p1.x, g1 + lift, p1.y), new Vector3(p0.x, g0 + lift, p0.y), new Vector3(n.x, 0f, n.y));
            }
        }

        static void PaintLine(MeshBuilder mb, Vector2 a, Vector2 b, float w, float lift, TerrainGrid t)
        {
            mb.Tint = new Color(0.92f, 0.92f, 0.90f);
            Vector2 d = (b - a).normalized, n = new Vector2(-d.y, d.x);
            Strip(mb, a, b, n, -w / 2f, w / 2f, lift, t);
        }

        // a grass island with a curb round it (flat, 3.5 cm up like the sidewalks: nothing to snag a wheel)
        static void IslandAt(MeshBuilder walks, MeshBuilder ground, Vector2 c, Vector2 U, Vector2 V, float wu, float wv, float lift, TerrainGrid t)
        {
            walks.Tint = new Color(0.70f, 0.69f, 0.66f);
            Strip(walks, c - U * (wu / 2f) - V * (wv / 2f), c - U * (wu / 2f) + V * (wv / 2f), U, 0f, wu, lift, t);
            ground.Tint = new Color(0.33f, 0.45f, 0.24f);
            Strip(ground, c - U * (wu / 2f - 0.2f) - V * (wv / 2f - 0.2f), c - U * (wu / 2f - 0.2f) + V * (wv / 2f - 0.2f), U, 0f, wu - 0.4f, lift + 0.012f, t);
            ground.Tint = new Color(0.235f, 0.235f, 0.245f);
        }

        // one shop: a glass front with a door in it, a band of wall over it, a flat awning and the sign on the band
        static void ShopFront(MeshBuilder mb, Transform root, Material signMat, Vector2 c, Vector2 d, Vector2 n, float w, int row, TerrainGrid t, float top, int k)
        {
            float g = Ground(t, c.x + n.x, c.y + n.y);
            var f = MakeFrame(new Vector3(c.x, g, c.y), n, 1f);
            float room = Mathf.Max(3.8f, top - g);
            float signY = Mathf.Min(4.0f, room - 0.6f);
            var glass = new Color(0.16f, 0.22f, 0.28f); var frame = new Color(0.22f, 0.22f, 0.23f); var awning = ShopAwnings[k % ShopAwnings.Length];
            float half = w / 2f - 0.6f;
            FrontQuad(mb, f, -half - 0.15f, half + 0.15f, -0.3f, 2.95f, 0.03f, frame);
            FrontQuad(mb, f, -half, half, 0.25f, 2.8f, 0.05f, glass);
            float du = (k % 2 == 0 ? -1f : 1f) * half * 0.55f;
            FrontQuad(mb, f, du - 0.9f, du + 0.9f, 0.0f, 2.4f, 0.07f, new Color(0.10f, 0.12f, 0.14f));   // the double door
            FrontQuad(mb, f, du - 0.03f, du + 0.03f, 0.0f, 2.4f, 0.08f, frame);
            BoxF(mb, f, -w / 2f + 0.2f, w / 2f - 0.2f, 3.05f, 3.2f, 0.02f, 1.6f, awning);
            float bw = Mathf.Min(9f, w - 1.6f), bh = bw * 0.078f;
            BoxF(mb, f, -bw / 2f - 0.1f, bw / 2f + 0.1f, signY - bh / 2f - 0.1f, signY + bh / 2f + 0.1f, 0.02f, 0.18f, new Color(0.16f, 0.16f, 0.17f));
            if (signMat != null) AtlasPlate(root, f.P(0f, signY, 0.19f), -f.F, bw, bh, row, signMat);
        }
        static readonly Color[] ShopAwnings = { new Color(0.20f, 0.30f, 0.22f), new Color(0.45f, 0.12f, 0.12f), new Color(0.18f, 0.24f, 0.40f), new Color(0.30f, 0.30f, 0.32f) };

        // the supermarket's front: a long glass entrance in the middle under a deep canopy, the big sign above it, PHARMACY to one side
        static void MarketFront(MeshBuilder mb, Transform root, Material signMat, Vector2 a, Vector2 d, Vector2 n, float len, TerrainGrid t, float top)
        {
            Vector2 c = a + d * (len / 2f);
            float g = Ground(t, c.x + n.x, c.y + n.y);
            var f = MakeFrame(new Vector3(c.x, g, c.y), n, 1f);
            var glass = new Color(0.16f, 0.22f, 0.28f); var frame = new Color(0.22f, 0.22f, 0.23f); var green = new Color(0.16f, 0.36f, 0.20f);
            FrontQuad(mb, f, -15.2f, 15.2f, -0.3f, 3.6f, 0.03f, frame);
            FrontQuad(mb, f, -15f, 15f, 0.25f, 3.4f, 0.05f, glass);
            foreach (float du in new[] { -6f, 6f })
            {
                FrontQuad(mb, f, du - 1.6f, du + 1.6f, 0.0f, 2.6f, 0.07f, new Color(0.10f, 0.12f, 0.14f));   // the sliding doors in and out
                FrontQuad(mb, f, du - 0.03f, du + 0.03f, 0.0f, 2.6f, 0.08f, frame);
            }
            BoxF(mb, f, -16f, 16f, 3.8f, 4.2f, 0.02f, 3.0f, green);
            float room = Mathf.Max(6f, top - g);
            float signY = Mathf.Min(6.0f, room - 1.3f);
            const float bw = 26f, bh = bw * 0.078f;
            BoxF(mb, f, -bw / 2f - 0.2f, bw / 2f + 0.2f, signY - bh / 2f - 0.2f, signY + bh / 2f + 0.2f, 0.02f, 0.25f, new Color(0.88f, 0.87f, 0.83f));
            if (signMat != null)
            {
                AtlasPlate(root, f.P(0f, signY, 0.26f), -f.F, bw, bh, SignMarket, signMat);
                // PHARMACY over a second, smaller door toward the south end
                BoxF(mb, f, 22f, 31f, 0.25f, 2.8f, 0.03f, 0.05f, glass);
                FrontQuad(mb, f, 25.6f, 27.4f, 0f, 2.4f, 0.07f, new Color(0.10f, 0.12f, 0.14f));
                BoxF(mb, f, 22f, 31f, 3.05f, 3.2f, 0.02f, 1.6f, green);
                BoxF(mb, f, 22.4f, 30.6f, 3.6f - 0.45f, 3.6f + 0.45f, 0.02f, 0.18f, new Color(0.16f, 0.16f, 0.17f));
                AtlasPlate(root, f.P(26.5f, 3.6f, 0.19f), -f.F, 8f, 0.62f, SignPharmacy, signMat);
            }
        }

        static void BuildBank(MapData map, TerrainGrid t, Materials mats, Transform root, MeshBuilder solid, Material signMat, List<Vector2> ring, Building b)
        {
            float lo = float.MaxValue, sum = 0f;
            foreach (var p in ring) { float g = Ground(t, p.x, p.y); lo = Mathf.Min(lo, g); sum += g; }
            float baseY = lo - 0.5f * vScale, topY = sum / ring.Count + b.height;
            var body = new MeshBuilder();
            Extrude(body, ring, baseY, topY, new Color(0.60f, 0.57f, 0.52f), new Color(0.30f, 0.29f, 0.28f), true);
            body.ToObject("AbandonedBank", mats.House, root, true);
            // boarded windows round every long enough wall, two floors; the door boarded too
            var ply = new Color(0.60f, 0.48f, 0.32f); var plyDark = new Color(0.47f, 0.37f, 0.24f); var grime = new Color(0.40f, 0.38f, 0.35f);
            Vector2 cen = Vector2.zero; foreach (var p in ring) cen += p; cen /= ring.Count;
            int boards = 0; float bestW = 0f, bestE = 0f; int west = -1, east = -1;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], c2 = ring[(i + 1) % ring.Count]; float len = Vector2.Distance(a, c2);
                if (len < 4f) continue;
                Vector2 d = (c2 - a) / len, n = new Vector2(d.y, -d.x);
                if (InPolygon((a + c2) / 2f + n * 0.5f, ring)) n = -n;
                if (len > bestW && n.x < -0.5f) { bestW = len; west = i; }
                if (len > bestE && n.x > 0.5f) { bestE = len; east = i; }
                int count = Mathf.FloorToInt((len - 1f) / 3.6f);
                for (int k = 0; k < count; k++)
                {
                    Vector2 m = a + d * ((k + 0.5f) * len / count);
                    var f = MakeFrame(new Vector3(m.x, Ground(t, m.x + n.x, m.y + n.y), m.y), n, 1f);
                    foreach (float y in new[] { 1.0f, 5.2f })
                    {
                        if (y + 2.0f > topY - f.O.y - 0.6f) continue;
                        FrontQuad(solid, f, -0.85f, 0.85f, y - 0.1f, y + 1.9f, 0.02f, grime);
                        FrontQuad(solid, f, -0.75f, 0.75f, y, y + 1.8f, 0.04f, (k + i) % 3 == 0 ? plyDark : ply);
                        FrontQuad(solid, f, -0.78f, 0.78f, y + 0.55f, y + 0.68f, 0.05f, plyDark);   // a cross batten
                        boards++;
                    }
                }
            }
            if (west >= 0 && signMat != null)
            {
                Vector2 a = ring[west], c2 = ring[(west + 1) % ring.Count], m = (a + c2) / 2f;
                Vector2 d = (c2 - a).normalized, nn = new Vector2(d.y, -d.x); if (InPolygon(m + nn * 0.5f, ring)) nn = -nn;
                var f = MakeFrame(new Vector3(m.x, Ground(t, m.x + nn.x, m.y + nn.y), m.y), nn, 1f);
                float y = Mathf.Min(8.0f, topY - f.O.y - 1.0f);
                FrontQuad(solid, f, -2.0f, 2.0f, 0f, 2.6f, 0.06f, plyDark);   // the boarded door under the sign
                BoxF(solid, f, -5.3f, 5.3f, y - 0.55f, y + 0.55f, 0.02f, 0.15f, grime);
                AtlasPlate(root, f.P(0f, y, 0.16f), -f.F, 10f, 0.78f, SignBank, signMat);
            }
            if (east >= 0 && signMat != null)
            {
                Vector2 a = ring[east], c2 = ring[(east + 1) % ring.Count], m = (a + c2) / 2f;
                Vector2 d = (c2 - a).normalized, nn = new Vector2(d.y, -d.x); if (InPolygon(m + nn * 0.5f, ring)) nn = -nn;
                var f = MakeFrame(new Vector3(m.x, Ground(t, m.x + nn.x, m.y + nn.y), m.y), nn, 1f);
                AtlasPlate(root, f.P(0f, 3.6f, 0.09f), -f.F, 7f, 0.55f, SignLease, signMat);
            }
        }

        // a sign face: one row of the store-sign atlas on a quad of w x h metres; the quad's front faces -facing (as SignPlate)
        static GameObject AtlasPlate(Transform parent, Vector3 centre, Vector3 facing, float w, float h, int row, Material mat)
        {
            var go = new GameObject("StoreSign"); go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(facing, Vector3.up));
            float v1 = 1f - row * 160f / 4096f, v0 = 1f - (row + 1) * 160f / 4096f;
            float hw = w / 2f, hh = h / 2f;
            var mesh = new Mesh { name = "StoreSign" };
            mesh.vertices = new[] { new Vector3(-hw, -hh, 0), new Vector3(hw, -hh, 0), new Vector3(hw, hh, 0), new Vector3(-hw, hh, 0) };
            mesh.uv = new[] { new Vector2(0.004f, v0 + 0.002f), new Vector2(0.996f, v0 + 0.002f), new Vector2(0.996f, v1 - 0.002f), new Vector2(0.004f, v1 - 0.002f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/Meshes/StoreSign_{parent.childCount}_{row}_{Mathf.Abs(centre.x.GetHashCode() ^ centre.z.GetHashCode())}.asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static Material StoreSignMaterial()
        {
            const string path = "Assets/HandMade/ShoppingCenter/StoreSigns.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("[Round17] " + path + " missing (Tools/handmade/make_store_signs.py): blank sign boards"); return null; }
            // lit and depth-tested (hidden by anything in front), one-sided
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "StoreSigns" };
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.2f);
            m.SetFloat("_Cull", (float)CullMode.Back);
            AssetDatabase.CreateAsset(m, GeneratedDir + "/StoreSigns.mat");
            return m;
        }
    }
}
