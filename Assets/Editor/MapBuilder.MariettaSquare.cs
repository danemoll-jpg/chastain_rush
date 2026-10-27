using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Racer;

// Round 76 (B, Dan, 2026-10-27: "MARIETTA SQUARE MADE TO LOOK LIKE ITSELF (as Town Center, Round 60)"): downtown Marietta in the map's new piece
// (Tools/mapgen/mapgen_piece.py), from Data/tracks/marietta_square.json (Tools/mapgen/make_marietta.py, OSM). World, not game layer.
//   Glover Park   brick walks round its edge and corner to corner, the fountain in the middle (FountainSpray), the bandstand on OSM's gazebo,
//                 benches and lamp posts along the walks (both break, as the neighborhoods' do), planters at the corners, a few people
//   the square    the old storefronts round it are MapBuilder.Businesses.cs' shops with a brick front, two or three stories, tall upper windows
//                 and a cornice (MariettaHistoric); brick sidewalks in front of them, café tables (they break); crosswalks at the park's corners
//                 and parallel parking marks along the curbs round it
//   landmarks     the theater (its marquee and name board), the food hall (Station Street Market), the museum (a columned portico, a pediment,
//                 steps), the old depot by the tracks (a hip roof on brackets, a platform); every name made up (make_business_signs.py rows 54 to 63)
//   the railroad  rails on ties on a gravel bed along OSM's lines near the square (no collider: the car drives on the ground under them)
// Quick-buildable alone: step "Marietta".
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class MariettaMarks { public int theater = -1, market = -1, museum = -1, depot = -1; }
        [Serializable] class MariettaRail { public float[] points; }
        [Serializable] class MariettaPlace { public string name; public float x, z, heading; }
        [Serializable] class MariettaData { public float[] park, park_center; public int gazebo = -1; public int[] historic, churches, park_drop; public MariettaMarks landmarks; public MariettaRail[] rail; public MariettaPlace place; }

        static MariettaData MariettaNow;
        static readonly HashSet<int> mariettaOwned = new HashSet<int>(), mariettaHistoric = new HashSet<int>(), mariettaChurch = new HashSet<int>();
        static readonly List<AmbientLife.Group> MariettaPeople = new List<AmbientLife.Group>();
        static bool MariettaOwns(int i) => mariettaOwned.Contains(i);
        static bool MariettaHistoric(int i) => mariettaHistoric.Contains(i);
        static bool MariettaChurch(int i) => mariettaChurch.Contains(i);
        static Vector2? mariettaCenter;
        static bool MariettaNear(Vector2 p) => mariettaCenter.HasValue && (p - mariettaCenter.Value).sqrMagnitude < 700f * 700f;   // downtown (its civic blocks are offices)
        static readonly Color[] MariettaBrick = { new Color(0.55f, 0.25f, 0.18f), new Color(0.62f, 0.31f, 0.22f), new Color(0.48f, 0.23f, 0.17f), new Color(0.66f, 0.40f, 0.28f), new Color(0.58f, 0.36f, 0.30f) };
        static readonly Color MariettaTrim = new Color(0.88f, 0.86f, 0.80f), MariettaStone = new Color(0.86f, 0.84f, 0.78f), BrickWalk = new Color(0.60f, 0.33f, 0.25f);
        const int SignTheater = 54, SignFoodHall = 55, SignMuseum = 56, SignDepot = 57, SignSquareFirst = 58, SignSquareCount = 6;
        static int MariettaShopRow(int seed, int k) => SignSquareFirst + (int)((Mix(seed, 77) + (uint)k) % SignSquareCount);

        static void PlanMarietta(MapData map, TerrainGrid t)
        {
            MariettaNow = null; mariettaCenter = null; mariettaOwned.Clear(); mariettaHistoric.Clear(); mariettaChurch.Clear(); MariettaPeople.Clear();
            var path = Path.Combine(ProjectRoot, "Data/tracks/marietta_square.json"); if (!File.Exists(path)) return;
            var d = JsonUtility.FromJson<MariettaData>(File.ReadAllText(path)); if (d?.park == null || d.park.Length < 6) return;
            bool Ok(int i) => i >= 0 && i < map.buildings.Length;
            MariettaNow = d;
            if (Ok(d.gazebo)) mariettaOwned.Add(d.gazebo);
            if (d.park_drop != null) foreach (var i in d.park_drop) if (Ok(i)) mariettaOwned.Add(i);   // the park's other buildings: not built (an open lawn)
            mariettaCenter = new Vector2(d.park_center[0], d.park_center[1]);
            foreach (var i in new[] { d.landmarks.theater, d.landmarks.market, d.landmarks.museum, d.landmarks.depot }) if (Ok(i)) mariettaOwned.Add(i);
            if (d.historic != null) foreach (var i in d.historic) if (Ok(i) && !mariettaOwned.Contains(i)) mariettaHistoric.Add(i);
            if (d.churches != null) foreach (var i in d.churches) if (Ok(i) && !mariettaOwned.Contains(i)) mariettaChurch.Add(i);
            // the people (AmbientLife, added by BuildLife): walkers along the park's walks, two talking by the fountain and on the corners
            var park = ToPoints(d.park); var c = new Vector2(d.park_center[0], d.park_center[1]);
            void Add(Vector2 p, Vector2 walk, byte action, int count)
            {
                var at = new Vector3(p.x, Ground(t, p.x, p.y) + .02f, p.y); var w3 = new Vector3(walk.x, 0f, walk.y);
                MariettaPeople.Add(new AmbientLife.Group { at = at, yaw = Mathf.Atan2(w3.x + 1e-4f, w3.z) * Mathf.Rad2Deg, kind = AmbientLife.Kind.Shop, count = (byte)count, action = action, walk = w3 });
            }
            for (int i = 0; i < park.Count; i++)
            {
                Vector2 a = park[i], b = park[(i + 1) % park.Count], m = (a + b) * .5f, along = (b - a).normalized, inward = (c - m).normalized;
                Add(m + inward * 3.5f, along * Mathf.Min(14f, (b - a).magnitude * .35f), AmbientLife.Walkers, 2);
                Add(Vector2.Lerp(a, c, .25f), inward * .5f, AmbientLife.Talking, 2);
            }
            Add(c + new Vector2(6.5f, 0f), Vector2.left, AmbientLife.Talking, 2); Add(c + new Vector2(-6.5f, 2f), Vector2.right, AmbientLife.Kids, 3);
            Debug.Log($"[Round76] Marietta Square planned: {mariettaOwned.Count} landmarks built by its step, {mariettaHistoric.Count} old storefronts, {mariettaChurch.Count} churches, {MariettaPeople.Count} groups of people");
        }

        static void BuildMarietta(MapData map, TerrainGrid t, Materials mats, Transform world)
        {
            var d = MariettaNow; if (d == null) return;
            var root = new GameObject("Marietta Square").transform; root.SetParent(world, false);
            var props = new MeshBuilder(); var walks = new MeshBuilder(); var paint = new MeshBuilder(); var bodies = new MeshBuilder(); var rail = new MeshBuilder();
            // (the businesses' own sign material, never made again: a second one written to the same asset left their signs magenta)
            var signs = new SignBatch(); var signMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir + "/BusinessSigns.mat") ?? BusinessSignMaterial();
            var benches = new Breakables(); var lamps = new Breakables(); var tables = new Breakables();
            var park = ToPoints(d.park); var c = new Vector2(d.park_center[0], d.park_center[1]);
            var log = new System.Text.StringBuilder();
            // road segments (the paint, and keeping things off the carriageways)
            var segs = new List<(Vector2 a, Vector2 b, float half, string name)>();
            foreach (var r in map.roads)
            {
                if (r.tunnel || r.points == null) continue; var p = ToPoints(r.points);
                for (int i = 0; i + 1 < p.Count; i++) if (SegDist(c, p[i], p[i + 1]) < 700f) segs.Add((p[i], p[i + 1], r.width * .5f, r.name ?? ""));
            }
            float RoadGap(Vector2 q) { float g = float.MaxValue; foreach (var s in segs) g = Mathf.Min(g, SegDist(q, s.a, s.b) - s.half); return g; }

            // ---------------- Glover Park: brick walks round the edge and corner to corner
            walks.Tint = BrickWalk;
            var ringWalk = new List<Vector2>(); foreach (var p in park) ringWalk.Add(p + (c - p).normalized * 3.0f); ringWalk.Add(ringWalk[0]);
            AddRibbon(walks, Resample(ringWalk, 2f), 3.0f, 0.05f, t);
            foreach (var p in park) AddRibbon(walks, Resample(new List<Vector2> { p + (c - p).normalized * 3.0f, c + (p - c).normalized * 7.5f }, 2f), 2.6f, 0.05f, t);
            // the plaza round the fountain
            var plaza = new List<Vector2>(); for (int k = 0; k < 24; k++) { float a = k * Mathf.PI * 2f / 24f; plaza.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 8.5f); }
            DrapePoly(walks, plaza, 1.5f, 0.06f, t);

            // ---------------- the fountain (no collider: it is driven through, the spray over the car)
            float gy = Ground(t, c.x, c.y); var stone = MariettaStone;
            for (int k = 0; k < 16; k++)
            {
                float a0 = k * Mathf.PI * 2f / 16f, a1 = (k + 1) * Mathf.PI * 2f / 16f;
                Vector3 P(float a, float r, float y) => new Vector3(c.x + Mathf.Cos(a) * r, gy + y, c.y + Mathf.Sin(a) * r);
                props.Tint = stone; Face(props, P(a0, 4.3f, 0f), P(a1, 4.3f, 0f), P(a1, 4.3f, 0.6f), P(a0, 4.3f, 0.6f), new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)));
                Face(props, P(a0, 4.3f, 0.6f), P(a1, 4.3f, 0.6f), P(a1, 3.8f, 0.6f), P(a0, 3.8f, 0.6f), Vector3.up);
                props.Tint = new Color(0.25f, 0.45f, 0.62f); TriFace(props, P(0f, 0f, 0.45f), P(a1, 3.8f, 0.45f), P(a0, 3.8f, 0.45f), Vector3.up);
            }
            Cyl(props, new Vector3(c.x, gy + 0.4f, c.y), 1.3f, 0.45f, 0.35f, stone, 10);
            Cyl(props, new Vector3(c.x, gy + 1.7f, c.y), 0.25f, 1.4f, 1.6f, stone, 12);
            Cyl(props, new Vector3(c.x, gy + 1.95f, c.y), 0.9f, 0.18f, 0.12f, stone, 8);
            var fgo = new GameObject("Marietta fountain"); fgo.transform.SetParent(root, false); fgo.transform.position = new Vector3(c.x, gy + 2.6f, c.y);
            var spray = fgo.AddComponent<FountainSpray>(); spray.material = SpraySoftMaterial != null ? SpraySoftMaterial : MakeSprayMaterial(); spray.waterY = gy + 2.6f; spray.height = 2.4f; spray.spread = 2.2f;

            // ---------------- the bandstand (OSM's gazebo): an octagon on a brick base, white posts and rail, a roof to a finial
            if (d.gazebo >= 0 && d.gazebo < map.buildings.Length)
            {
                var g = ToPoints(map.buildings[d.gazebo].footprint); var gc = Vector2.zero; foreach (var p in g) gc += p; gc /= g.Count;
                float r = Mathf.Clamp(g.Max(p => (p - gc).magnitude), 3f, 7f), lo = g.Min(p => Ground(t, p.x, p.y)), hi = g.Max(p => Ground(t, p.x, p.y)), floor = hi + 0.8f;
                Vector3 O(float a, float rr, float y) => new Vector3(gc.x + Mathf.Cos(a) * rr, y, gc.y + Mathf.Sin(a) * rr);
                for (int k = 0; k < 8; k++)
                {
                    float a0 = k * Mathf.PI / 4f, a1 = (k + 1) * Mathf.PI / 4f; var n = new Vector3(Mathf.Cos((a0 + a1) / 2f), 0f, Mathf.Sin((a0 + a1) / 2f));
                    props.Tint = new Color(0.52f, 0.25f, 0.19f); Face(props, O(a0, r, lo - 0.3f), O(a1, r, lo - 0.3f), O(a1, r, floor), O(a0, r, floor), n);
                    props.Tint = new Color(0.62f, 0.55f, 0.45f); TriFace(props, new Vector3(gc.x, floor, gc.y), O(a1, r, floor), O(a0, r, floor), Vector3.up);
                    Post(props, O(a0, r - 0.25f, floor), 3.1f, 0.22f, MariettaTrim);
                    if (k != 2) { Beam(props, O(a0, r - 0.25f, floor + 0.95f), O(a1, r - 0.25f, floor + 0.95f), 0.08f, 0.08f, MariettaTrim); Beam(props, O(a0, r - 0.25f, floor + 0.15f), O(a1, r - 0.25f, floor + 0.15f), 0.06f, 0.06f, MariettaTrim); }
                    props.Tint = new Color(0.30f, 0.32f, 0.34f); TriFace(props, new Vector3(gc.x, floor + 5.1f, gc.y), O(a0, r + 0.6f, floor + 3.0f), O(a1, r + 0.6f, floor + 3.0f), n + Vector3.up);
                    props.Tint = MariettaTrim; TriFace(props, new Vector3(gc.x, floor + 5.0f, gc.y), O(a1, r + 0.6f, floor + 2.95f), O(a0, r + 0.6f, floor + 2.95f), Vector3.down);
                    var col = new GameObject("Bandstand post"); col.transform.SetParent(root, false); col.transform.position = O(a0, r - 0.25f, floor + 1.5f);
                    col.AddComponent<BoxCollider>().size = new Vector3(0.25f, 3f, 0.25f);
                }
                // the steps up on the side toward the park's middle
                var sd = (c - gc).normalized; var sf = MakeFrame(new Vector3(gc.x + sd.x * r, lo, gc.y + sd.y * r), sd, 1f);
                for (int s = 0; s < 4; s++) BoxF(props, sf, -1.2f, 1.2f, lo - lo - 0.3f, (floor - lo) * (4 - s) / 4f, s * 0.35f - 0.1f, s * 0.35f + 0.35f, MariettaStone);
                // its floor: two flat boxes crossed at 45 degrees (an octagon); a capsule as wide as it is tall is a ball that threw cars off
                for (int q = 0; q < 2; q++)
                {
                    var floorCol = new GameObject("Bandstand floor"); floorCol.transform.SetParent(root, false);
                    floorCol.transform.SetPositionAndRotation(new Vector3(gc.x, (lo - 0.3f + floor) / 2f, gc.y), Quaternion.Euler(0f, q * 45f + 22.5f, 0f));
                    floorCol.AddComponent<BoxCollider>().size = new Vector3(r * 1.84f * Mathf.Cos(Mathf.PI / 8f), floor - lo + 0.3f, r * 1.84f * Mathf.Cos(Mathf.PI / 8f));
                }
                log.Append($"bandstand {r * 2f:0.0} m across; ");
            }

            // ---------------- benches along the edge walk (facing the walk, on the park side) and lamp posts on its outer side; planters at the corners
            int nb = 0, nl = 0, np = 0;
            var iron = new Color(0.08f, 0.09f, 0.08f); var glass = new Color(0.98f, 0.92f, 0.70f);
            for (int i = 0; i < park.Count; i++)
            {
                Vector2 a = ringWalk[i], b = ringWalk[i + 1]; float len = (b - a).magnitude; var along = (b - a) / len; var inward = (c - (a + b) * .5f).normalized;
                if (Vector2.Dot(inward, new Vector2(-along.y, along.x)) < 0f) { } // (inward is toward the middle either way)
                for (float s = 9f; s < len - 8f; s += 13f)
                {
                    var p = a + along * s + inward * 2.4f; float y = Ground(t, p.x, p.y);
                    var f = MakeFrame(new Vector3(p.x, y, p.y), -inward, 1f);
                    var mb = benches.Begin(p); benches.Piece(() => Bench(mb, f), y + 0.1f);
                    benches.End(new Vector3(p.x, y + 0.5f, p.y), new Vector3(along.x, 0f, along.y), 1.9f, 1.0f, 0.6f); nb++;
                }
                for (float s = 4f; s < len - 3f; s += 18f)
                {
                    var p = a + along * s - inward * 2.0f; float y = Ground(t, p.x, p.y);
                    var mb = lamps.Begin(p);
                    lamps.Piece(() => { Post(mb, new Vector3(p.x, y - 0.05f, p.y), 0.5f, 0.32f, iron); Post(mb, new Vector3(p.x, y + 0.4f, p.y), 3.2f, 0.12f, iron); }, y + 0.1f);
                    lamps.Piece(() => { Cyl(mb, new Vector3(p.x, y + 3.55f, p.y), 0.55f, 0.22f, 0.26f, glass, 8); Cyl(mb, new Vector3(p.x, y + 4.1f, p.y), 0.18f, 0.3f, 0.05f, iron, 8); }, y + 0.15f);
                    lamps.End(new Vector3(p.x, y + 1.0f, p.y), new Vector3(along.x, 0f, along.y), 0.6f, 2.2f); nl++;
                }
                // a planter at each corner of the walk: a low brick box with a shrub (breaks like a bench)
                {
                    var p = park[i] + (c - park[i]).normalized * 6.5f; float y = Ground(t, p.x, p.y); var f = MakeFrame(new Vector3(p.x, y, p.y), (c - p).normalized, 1f);
                    var mb = benches.Begin(p);
                    benches.Piece(() => BoxF(mb, f, -0.9f, 0.9f, -0.1f, 0.6f, -0.9f, 0.9f, new Color(0.52f, 0.26f, 0.20f)), y + 0.1f);
                    benches.Piece(() => { mb.Tint = new Color(0.20f, 0.38f, 0.16f); Cyl(mb, new Vector3(p.x, y + 0.6f, p.y), 0.8f, 0.75f, 0.35f, new Color(0.20f, 0.38f, 0.16f), 7); }, y + 0.1f);
                    benches.End(new Vector3(p.x, y + 0.4f, p.y), new Vector3(1f, 0f, 0f), 1.8f, 0.8f, 1.8f); np++;
                }
            }

            // ---------------- the old storefronts' brick sidewalks and café tables (on the face toward the park)
            int nt = 0, nw = 0;
            foreach (int bi in mariettaHistoric.Concat(new[] { d.landmarks.theater, d.landmarks.museum, d.landmarks.market }.Where(i => i >= 0)))
            {
                var ring = ToPoints(map.buildings[bi].footprint); if (ring.Count < 3) continue;
                if (PolyArea(ring) < 0f) ring.Reverse();
                // the face that looks most toward the nearest road (its front onto the street)
                int best = -1; float bestScore = float.MinValue; Vector2 bn = Vector2.zero;
                for (int e = 0; e < ring.Count; e++)
                {
                    Vector2 a = ring[e], b = ring[(e + 1) % ring.Count]; float len = (b - a).magnitude; if (len < 4f) continue;
                    var dd = (b - a) / len; var n = new Vector2(dd.y, -dd.x); var m = (a + b) * .5f; if (InPolygon(m + n * 0.3f, ring)) n = -n;
                    float gap = RoadGap(m + n * 1.0f); float score = -gap + len * 0.05f;
                    if (score > bestScore) { bestScore = score; best = e; bn = n; }
                }
                if (best < 0) continue;
                Vector2 fa = ring[best], fb = ring[(best + 1) % ring.Count]; float flen = (fb - fa).magnitude; var fd = (fb - fa) / flen;
                float room = RoadGap((fa + fb) * .5f + bn * 0.2f); if (room < 1.6f) continue;
                float w = Mathf.Min(room - 0.3f, 4.5f);
                walks.Tint = BrickWalk;
                var strip = new List<Vector2> { fa + bn * 0.05f, fb + bn * 0.05f, fb + bn * w, fa + bn * w };
                DrapePoly(walks, strip, 1.5f, 0.07f, t); nw++;
                if (w < 2.6f || !mariettaHistoric.Contains(bi) || Hash01(map.buildings[bi].seed, 762) > 0.55f) continue;
                for (float s = 2.5f; s < flen - 2f; s += 4.5f)
                {
                    var p = fa + fd * s + bn * Mathf.Min(1.7f, w * 0.5f); float y = Ground(t, p.x, p.y);
                    var mb = tables.Begin(p); var f = MakeFrame(new Vector3(p.x, y + 0.05f, p.y), bn, 1f); var top = PickOf(AwningColors, map.buildings[bi].seed, 40);
                    tables.Piece(() => { Cyl(mb, new Vector3(p.x, y, p.y), 0.72f, 0.05f, 0.05f, iron, 6); Cyl(mb, new Vector3(p.x, y + 0.72f, p.y), 0.04f, 0.38f, 0.38f, MariettaTrim, 10); }, y + 0.05f);
                    tables.Piece(() => { BoxF(mb, f, -0.75f, -0.35f, 0f, 0.45f, -0.2f, 0.2f, iron); BoxF(mb, f, -0.75f, -0.72f, 0.45f, 0.9f, -0.2f, 0.2f, iron); }, y + 0.05f);
                    tables.Piece(() => { BoxF(mb, f, 0.35f, 0.75f, 0f, 0.45f, -0.2f, 0.2f, iron); BoxF(mb, f, 0.72f, 0.75f, 0.45f, 0.9f, -0.2f, 0.2f, iron); }, y + 0.05f);
                    tables.Piece(() => { Cyl(mb, new Vector3(p.x, y + 0.75f, p.y), 1.5f, 0.03f, 0.03f, iron, 4); Cyl(mb, new Vector3(p.x, y + 2.2f, p.y), 0.35f, 1.2f, 0.05f, top, 8); }, y + 0.05f);
                    tables.End(new Vector3(p.x, y + 0.5f, p.y), new Vector3(fd.x, 0f, fd.y), 1.6f, 1.2f, 1.0f); nt++;
                }
            }

            // ---------------- the streets round the square: crosswalks at the park's corners, parallel parking marks along the curbs
            int ncw = 0, npk = 0; var white = new Color(0.94f, 0.94f, 0.90f);
            void Mark(Vector2 a, Vector2 b, float w)   // a painted bar a..b, w wide, on the road
            {
                var dir = (b - a).normalized; var n = new Vector2(-dir.y, dir.x) * (w * .5f);
                var q = new List<Vector2> { a - n, b - n, b + n, a + n }; paint.Tint = white; DrapePoly(paint, q, 1.0f, 0.14f, t);
            }
            var square = segs.Where(s => s.name.Contains("Park Square") || RingDistSeg(park, s.a, s.b) < 22f).ToList();
            for (int i = 0; i < park.Count; i++)
            {
                var corner = park[i]; var out_ = (corner - c).normalized;
                foreach (var s in square)
                {
                    var cp = ClosestOnSeg(corner + out_ * 6f, s.a, s.b); if ((cp - corner).magnitude > 24f) continue;
                    var sd = (s.b - s.a).normalized; var across = new Vector2(-sd.y, sd.x);
                    // the crosswalk 3 m wide across the street, 6 m along it from the corner: stripes along the street, 0.5 m wide, 0.6 m apart
                    var mid = cp + sd * Mathf.Sign(Vector2.Dot(c - corner, sd)) * 0f;
                    for (float k = -s.half + 0.5f; k <= s.half - 0.3f; k += 1.1f) Mark(mid + across * k - sd * 1.6f, mid + across * k + sd * 1.6f, 0.5f);
                    ncw++;
                }
            }
            foreach (var s in square)
            {
                float len = (s.b - s.a).magnitude; if (len < 14f) continue; var sd = (s.b - s.a) / len; var across = new Vector2(-sd.y, sd.x);
                foreach (float side in new[] { -1f, 1f })
                {
                    var edge = across * side * (s.half - 2.3f);
                    for (float u = 10f; u < len - 10f; u += 6.5f)
                    {
                        var p = s.a + sd * u + edge; if (InPolygon(p, park)) continue;
                        Mark(p - across * side * 1.1f, p + across * side * 1.1f, 0.12f); npk++;
                    }
                }
            }

            // ---------------- the railroad: a gravel bed, ties, two rails (off the roads' carriageways: a level crossing shows the rails only)
            int nties = 0; float railLen = 0f; var ballast = new Color(0.42f, 0.40f, 0.37f); var tie = new Color(0.30f, 0.22f, 0.16f); var steel = new Color(0.36f, 0.33f, 0.31f);
            if (d.rail != null)
                foreach (var rl in d.rail)
                {
                    var pts = Resample(ToPoints(rl.points), 1.2f); if (pts.Count < 2) continue;
                    var bed = new List<Vector2>();
                    for (int i = 0; i < pts.Count; i++)
                    {
                        var p = pts[i]; var dir = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized; var n = new Vector2(-dir.y, dir.x);
                        bool onRoad = RoadGap(p) < 0.5f; float y = Ground(t, p.x, p.y);
                        if (i > 0) railLen += (pts[i] - pts[i - 1]).magnitude;
                        if (!onRoad)
                        {
                            bed.Add(p);
                            Beam(rail, new Vector3(p.x - n.x * 1.3f, y + 0.12f, p.y - n.y * 1.3f), new Vector3(p.x + n.x * 1.3f, y + 0.12f, p.y + n.y * 1.3f), 0.24f, 0.14f, tie); nties++;
                        }
                        else if (bed.Count > 1) { rail.Tint = ballast; AddRibbon(rail, bed, 3.4f, 0.04f, t); bed.Clear(); }
                        else bed.Clear();
                        if (i > 0)
                        {
                            var q = pts[i - 1]; float yq = Ground(t, q.x, q.y);
                            foreach (float g in new[] { -0.72f, 0.72f })
                                Beam(rail, new Vector3(q.x + n.x * g, yq + 0.24f, q.y + n.y * g), new Vector3(p.x + n.x * g, y + 0.24f, p.y + n.y * g), 0.08f, 0.14f, steel);
                        }
                    }
                    if (bed.Count > 1) { rail.Tint = ballast; AddRibbon(rail, bed, 3.4f, 0.04f, t); }
                }

            // ---------------- the landmarks
            void Body(int bi, float height, Color wall, Color roof, out List<Vector2> ring, out float floor)
            {
                ring = ToPoints(map.buildings[bi].footprint); if (PolyArea(ring) < 0f) ring.Reverse();
                float lo = ring.Min(p => Ground(t, p.x, p.y)), hi = ring.Max(p => Ground(t, p.x, p.y));
                floor = Mathf.Min(hi, lo + 1.2f) + 0.1f;
                if (floor - lo > 0.05f) Extrude(bodies, ring, lo - 0.4f, floor, Shade(wall, 0.8f), Shade(wall, 0.8f), false);
                Extrude(bodies, ring, floor, floor + height, wall, roof, true);
                for (int e = 0; e < ring.Count; e++) Parapet(bodies, ring[e], ring[(e + 1) % ring.Count], ring, floor + height, Shade(wall, 0.85f));
            }
            (Vector2 a, Vector2 b, Vector2 n) FaceToward(List<Vector2> ring, Vector2 target)
            {
                (Vector2, Vector2, Vector2) best = (ring[0], ring[1], Vector2.up); float bs = float.MinValue;
                for (int e = 0; e < ring.Count; e++)
                {
                    Vector2 a = ring[e], b = ring[(e + 1) % ring.Count]; float len = (b - a).magnitude; if (len < 4f) continue;
                    var dd = (b - a) / len; var n = new Vector2(dd.y, -dd.x); var m = (a + b) * .5f; if (InPolygon(m + n * 0.3f, ring)) n = -n;
                    float s = Vector2.Dot(n, (target - m).normalized) * Mathf.Sqrt(len); if (s > bs) { bs = s; best = (a, b, n); }
                }
                return best;
            }
            void SignOn(Frame f, float u, float y, float w, int row, float proud = 0.2f)
            {
                if (signMat == null) return; float h = w / 8f;
                BoxF(bodies, f, u - w / 2f - 0.15f, u + w / 2f + 0.15f, y - h / 2f - 0.15f, y + h / 2f + 0.15f, Mathf.Max(0.02f, proud - 0.18f), proud, new Color(0.12f, 0.12f, 0.13f));
                signs.Add(f.P(u, y, proud + 0.012f), new Vector2(f.F.x, f.F.z), w, h, row);
            }
            void UpperWindows(Frame f, float half, float y0, float y1, Color wall)
            {
                for (float u = -half + 1.6f; u <= half - 1.4f; u += 2.6f)
                    for (float y = y0; y + 2.0f <= y1; y += 3.7f)
                    { FrontQuad(bodies, f, u - 0.65f, u + 0.65f, y - 0.15f, y + 2.05f, 0.03f, MariettaTrim); FrontQuad(bodies, f, u - 0.5f, u + 0.5f, y, y + 1.9f, 0.05f, BizGlass); }
            }
            // the theater: four stories of brick, the marquee over the doors (its name on the front, a lit edge), a name board up the front
            if (d.landmarks.theater >= 0)
            {
                var wall = new Color(0.56f, 0.27f, 0.20f); float H = Mathf.Max(map.buildings[d.landmarks.theater].height, 14f);
                Body(d.landmarks.theater, H, wall, new Color(0.30f, 0.30f, 0.31f), out var ring, out float floor);
                var (a, b, n) = FaceToward(ring, c); var m = (a + b) * .5f; float half = (b - a).magnitude / 2f;
                var f = MakeFrame(new Vector3(m.x, floor, m.y), n, 1f);
                FrontQuad(bodies, f, -half * 0.55f, half * 0.55f, 0f, 3.2f, 0.05f, BizGlass);
                for (float u = -half * 0.55f + 1.4f; u < half * 0.55f; u += 1.4f) FrontQuad(bodies, f, u - 0.04f, u + 0.04f, 0f, 3.2f, 0.07f, BizFrame);
                float mw = Mathf.Min(half * 1.6f, 13f);
                BoxF(bodies, f, -mw / 2f, mw / 2f, 3.9f, 5.3f, 0.0f, 3.2f, new Color(0.18f, 0.16f, 0.15f));
                BoxF(bodies, f, -mw / 2f - 0.05f, mw / 2f + 0.05f, 5.3f, 5.42f, 0.0f, 3.25f, new Color(1f, 0.85f, 0.45f));
                BoxF(bodies, f, -mw / 2f - 0.05f, mw / 2f + 0.05f, 3.78f, 3.9f, 0.0f, 3.25f, new Color(1f, 0.85f, 0.45f));
                var mf = MakeFrame(f.P(0f, 0f, 3.2f), n, 1f); mf.O = new Vector3(mf.O.x, floor, mf.O.z);
                SignOn(mf, 0f, 4.6f, Mathf.Min(mw - 0.6f, 9.6f), SignTheater, 0.02f);
                SignOn(f, 0f, Mathf.Min(H - 2f, 10f), Mathf.Min(half * 1.5f, 10f), SignTheater);
                UpperWindows(f, half, 6.4f, H - 1.0f, wall);
                log.Append($"theater {half * 2f:0} m front, {H:0} m tall; ");
            }
            // the food hall: one tall story of brick, big windows, awnings, its name board
            if (d.landmarks.market >= 0)
            {
                var wall = new Color(0.60f, 0.30f, 0.22f); float H = Mathf.Max(map.buildings[d.landmarks.market].height, 6.5f);
                Body(d.landmarks.market, H, wall, new Color(0.32f, 0.32f, 0.33f), out var ring, out float floor);
                // its front: the long face nearest a road (its door onto the street)
                (Vector2 a, Vector2 b, Vector2 n) = (ring[0], ring[1], Vector2.up); float bestGap = float.MaxValue;
                for (int e = 0; e < ring.Count; e++)
                {
                    Vector2 ea = ring[e], eb = ring[(e + 1) % ring.Count]; float len = (eb - ea).magnitude; if (len < 8f) continue;
                    var dd = (eb - ea) / len; var en = new Vector2(dd.y, -dd.x); if (InPolygon((ea + eb) * .5f + en * 0.3f, ring)) en = -en;
                    float gap = RoadGap((ea + eb) * .5f + en); if (gap < bestGap) { bestGap = gap; (a, b, n) = (ea, eb, en); }
                }
                var m = (a + b) * .5f; float half = (b - a).magnitude / 2f; var f = MakeFrame(new Vector3(m.x, floor, m.y), n, 1f);
                for (float u = -half + 1.2f; u < half - 1.0f; u += 3.6f)
                {
                    FrontQuad(bodies, f, u - 0.1f, u + 3.0f, -0.2f, 3.4f, 0.03f, BizFrame); FrontQuad(bodies, f, u, u + 2.9f, 0.3f, 3.3f, 0.05f, BizGlass);
                    BoxF(bodies, f, u - 0.1f, u + 3.0f, 3.5f, 3.62f, 0.02f, 1.5f, new Color(0.18f, 0.32f, 0.24f));
                }
                SignOn(f, 0f, Mathf.Min(H - 0.9f, 5.2f), Mathf.Min(half * 1.4f, 9f), SignFoodHall);
                log.Append($"market {half * 2f:0} m front; ");
            }
            // the museum: pale stone, a columned portico with a pediment and steps up to it, its name on the frieze
            if (d.landmarks.museum >= 0)
            {
                float H = Mathf.Max(map.buildings[d.landmarks.museum].height, 9f);
                Body(d.landmarks.museum, H, MariettaStone, new Color(0.40f, 0.40f, 0.42f), out var ring, out float floor);
                var (a, b, n) = FaceToward(ring, c); var m = (a + b) * .5f; float half = Mathf.Min((b - a).magnitude / 2f, 12f);
                var f = MakeFrame(new Vector3(m.x, floor, m.y), n, 1f); float depth = 3.6f, colH = H - 2.2f;
                BoxF(bodies, f, -half, half, -0.4f, 0.6f, 0f, depth + 0.6f, Shade(MariettaStone, 0.95f));
                for (int s = 1; s <= 3; s++) BoxF(bodies, f, -half + 0.6f, half - 0.6f, -0.4f, 0.6f - s * 0.25f, depth + 0.6f, depth + 0.6f + s * 0.45f, Shade(MariettaStone, 0.92f));
                int cols = Mathf.Clamp(Mathf.RoundToInt(half * 2f / 3.2f) + 1, 4, 8);
                for (int k = 0; k < cols; k++) { float u = Mathf.Lerp(-half + 0.8f, half - 0.8f, k / (cols - 1f)); Cyl(bodies, f.P(u, 0.6f, depth), colH - 0.6f, 0.45f, 0.38f, MariettaTrim, 12); }
                BoxF(bodies, f, -half, half, colH, colH + 1.3f, -0.1f, depth + 0.4f, MariettaStone);
                var pa = f.P(-half, colH + 1.3f, depth + 0.4f); var pb = f.P(half, colH + 1.3f, depth + 0.4f); var pt = f.P(0f, colH + 1.3f + half * 0.28f, depth + 0.4f);
                bodies.Tint = MariettaTrim; TriFace(bodies, pa, pb, pt, f.F);
                var qa = f.P(-half, colH + 1.3f, -0.1f); var qb = f.P(half, colH + 1.3f, -0.1f); var qt = f.P(0f, colH + 1.3f + half * 0.28f, -0.1f);
                bodies.Tint = new Color(0.40f, 0.40f, 0.42f); Face(bodies, pa, qa, qt, pt, Vector3.Cross(pt - pa, qa - pa).normalized); Face(bodies, pb, pt, qt, qb, Vector3.Cross(qb - pb, pt - pb).normalized);
                FrontQuad(bodies, f, -1.3f, 1.3f, 0.6f, 3.6f, 0.05f, new Color(0.25f, 0.18f, 0.12f));
                log.Append($"museum {cols} columns; ");
            }
            // the depot: one story of brick under a wide hip roof on brackets, its name on the trackside, a platform along the tracks
            if (d.landmarks.depot >= 0)
            {
                var wall = new Color(0.58f, 0.30f, 0.22f); float H = 4.6f;
                var ring = ToPoints(map.buildings[d.landmarks.depot].footprint); if (PolyArea(ring) < 0f) ring.Reverse();
                if (BestRectangle(ring, out var dc, out var U, out var V, out float hl, out float hw))
                {
                    float lo = ring.Min(p => Ground(t, p.x, p.y)), floor = ring.Max(p => Ground(t, p.x, p.y)) + 0.3f;
                    // the trackside: the long side nearer the nearest rail
                    Vector2 near = dc + V * 50f; float bestD = float.MaxValue;
                    if (d.rail != null) foreach (var rl in d.rail) foreach (var q in ToPoints(rl.points)) { float dd = (q - dc).magnitude; if (dd < bestD) { bestD = dd; near = q; } }
                    if (Vector2.Dot(near - dc, V) < 0f) V = -V;
                    var f = new Frame { O = new Vector3(dc.x, floor, dc.y), R = new Vector3(U.x, 0f, U.y), F = new Vector3(V.x, 0f, V.y), M = 1f };
                    BoxF(bodies, f, -hl, hl, lo - floor - 0.4f, 0f, -hw, hw, Shade(wall, 0.8f));
                    BoxF(bodies, f, -hl, hl, 0f, H, -hw, hw, wall);
                    float oh = 1.8f, rise = Mathf.Min(hw, hl) * 0.6f;
                    Vector3 A = f.P(-hl - oh, H, -hw - oh), B = f.P(hl + oh, H, -hw - oh), C = f.P(hl + oh, H, hw + oh), D = f.P(-hl - oh, H, hw + oh);
                    float ru = Mathf.Max(0.3f, hl - hw); Vector3 R0 = f.P(-ru, H + rise, 0f), R1 = f.P(ru, H + rise, 0f);
                    bodies.Tint = new Color(0.28f, 0.24f, 0.22f); Face(bodies, A, B, R1, R0, Vector3.up); Face(bodies, C, D, R0, R1, Vector3.up); TriFace(bodies, B, C, R1, Vector3.up); TriFace(bodies, D, A, R0, Vector3.up);
                    bodies.Tint = MariettaTrim; Face(bodies, A, R0, R1, B, Vector3.down); Face(bodies, C, R1, R0, D, Vector3.down); TriFace(bodies, B, R1, C, Vector3.down); TriFace(bodies, D, R0, A, Vector3.down);
                    for (float u = -hl + 1.5f; u <= hl - 1.4f; u += 3f) { FrontQuad(bodies, f, u - 0.55f, u + 0.55f, 0.9f, 2.9f, hw + 0.02f, BizGlass); var bk = f; Beam(bodies, f.P(u, H - 1.0f, hw), f.P(u, H - 0.05f, hw + oh * 0.8f), 0.12f, 0.12f, MariettaTrim); }
                    BoxF(bodies, f, -hl - 4f, hl + 4f, lo - floor - 0.3f, 0.05f, hw, hw + Mathf.Clamp(bestD - hw - 2.2f, 2f, 5f), new Color(0.62f, 0.60f, 0.56f));
                    var sf = new Frame { O = f.O, R = f.R, F = f.F, M = 1f };
                    SignOn(sf, 0f, H - 0.6f, Mathf.Min(hl * 1.2f, 6.5f), SignDepot, hw + 0.25f);
                    log.Append($"depot {hl * 2f:0} x {hw * 2f:0} m, its platform toward the tracks; ");
                }
            }

            // ---------------- out to the scene
            walks.ToObject("Marietta walks", mats.House, root, false, false);
            paint.ToObject("Marietta paint", mats.House, root, false, false);
            props.ToObject("Marietta park", mats.House, root, false, true);
            rail.ToObject("Marietta railroad", mats.House, root, false, true);
            bodies.ToObject("Marietta landmarks", mats.House, root, true, true);
            benches.ToObjects("Marietta benches", mats.House, root, 0.4f, BreakSounds.Kind.Wood);
            lamps.ToObjects("Marietta lamps", mats.House, root, 1f, BreakSounds.Kind.Lamp);
            tables.ToObjects("Marietta cafe tables", mats.House, root, 0.4f, BreakSounds.Kind.Iron);
            if (signMat != null) signs.ToObjects(root, signMat, "MariettaSigns");
            Debug.Log($"[Round76] Marietta Square: {log}{nb} benches, {np} planters, {nl} lamp posts, {nw} brick walks, {nt} café tables, {ncw} crosswalks, {npk} parking marks, " +
                      $"railroad {railLen:0} m ({nties} ties), {signs.Count} signs");
        }

        // Round 76 (B): an old storefront's upper stories (MapBuilder.Businesses.cs, MariettaHistoric): tall windows with pale surrounds every 2.6 m on the
        // front (3.4 m on a side face over 8 m long), a pale band over the awnings and a cornice at the top
        static void HistoricUpper(MeshBuilder d, Frame f, float half, float eave, Color wall, bool front)
        {
            if (half < 2.5f) return;
            if (front || half > 4f)
                for (float y = 4.2f; y + 2.0f <= eave - 0.8f; y += 3.7f)
                    for (float u = -half + 1.5f; u <= half - 1.3f; u += front ? 2.6f : 3.4f)
                    { FrontQuad(d, f, u - 0.66f, u + 0.66f, y - 0.18f, y + 2.1f, 0.03f, MariettaTrim); FrontQuad(d, f, u - 0.5f, u + 0.5f, y, y + 1.9f, 0.05f, BizGlass); }
            if (front) BoxF(d, f, -half, half, 3.62f, 3.82f, 0f, 0.12f, MariettaTrim);
            BoxF(d, f, -half - 0.05f, half + 0.05f, eave - 0.55f, eave - 0.2f, 0f, 0.38f, Shade(wall, 0.72f));
        }

        static float RingDistSeg(List<Vector2> ring, Vector2 a, Vector2 b)
        {
            float d = float.MaxValue;
            for (int i = 0; i < ring.Count; i++) { d = Mathf.Min(d, SegDist(ring[i], a, b)); d = Mathf.Min(d, SegDist((a + b) * .5f, ring[i], ring[(i + 1) % ring.Count])); }
            return d;
        }
    }
}
