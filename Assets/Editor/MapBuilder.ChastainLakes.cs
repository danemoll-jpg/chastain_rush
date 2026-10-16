using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 32 (C2): the Chastain Lakes amenity area on East Chastain Lake, between the lake, Bells Ferry Road and the end of
// Lakeside Drive (Dan, BUG-025 / BUG-026: "there should be a parking lot in front of a pool, pool house and tennis courts", "on
// the right there should be a swinging bench in front of the lake, a gazebo in a deck on the water and there are always many
// geese here"). World only, no game layer. Placed from OSM (buildings 266 / 267, the lake, the stream, Lakeside Drive and its
// OSM drive into the lot), NAIP (the pool, deck, courts, pier and the shore path, read off raw/naip_4band.tif) and the terrain;
// Dan's two reference pictures (Google imagery) were looked at only for what is there, never traced or measured.
//   - the lap pool (9 x 21 m, four lanes, lane lines and ropes) in a concrete deck, water as the Glen's pool (BuildSunkPool:
//     ShallowWater, drive out up the shallow end), five loungers a side with a table and umbrella at each end, an iron fence
//     round the deck (breakable, built with the Glen's fences) closed by the two buildings' walls, a gateway toward the lot
//   - building 266 the pool house (one story, hipped roof, between the pool and the courts), 267 the small bathhouse on the
//     pool's lake side (one story, hipped roof, two restroom doors); both take the place of Round 31's office blocks
//   - four tennis courts in two fenced pairs (the north pair down by the stream, the south pair beside the lot), breakable
//     chain-link panels with two open gates a pair, light poles
//   - a paved lot off Lakeside Drive's end (OSM drive 93 widened to a 6 m apron), stalls along both long sides, a sidewalk to
//     the pool gate, light poles
//   - a walk from the lot down to a pier and a gazebo on a deck out on the water; a porch swing on an A-frame on the lawn,
//     facing the water; a flock of geese on the lawn and the shore (the Glen pond's GooseFlock)
//   - the paved path on the dam's lawn along Bells Ferry Road, from the courts over a wooden footbridge across the lake's
//     outflow stream and north along the shore. The bridge only spans the stream: nothing stands in the channel, its underside
//     clears the stream's line (logged), and its deck and rails are solid so a vehicle can drive over it. BF (Round 23) and
//     the stream are not touched (no terrain edit near either).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        sealed class ChastainLakes
        {
            public const int PoolHouse = 266, Bathhouse = 267;
            // pictures: Dan's two spots and one from the lot toward the pool (x, z, yaw; heading 0 = +z)
            public static readonly (string name, float x, float z, float yaw)[] Views =
            {
                ("r32_lakes_bug025", 1312.55f, 1080.73f, 54f), ("r32_lakes_bug026", 1314.67f, 1085.28f, 331f), ("r32_lakes_lot_to_pool", 1344.0f, 1080.0f, 0f),
            };

            // the pool deck (axis-aligned) and the pool on it (NAIP: water x 1338.5..1347.5, z 1100.5..1121.5)
            public readonly Vector2 DeckC = new Vector2(1344.1f, 1110f); public const float DeckHX = 11.5f, DeckHZ = 14f; public float DeckY;
            public readonly List<Vector2> Pool = new List<Vector2> { new Vector2(1338.5f, 1100.5f), new Vector2(1347.5f, 1100.5f), new Vector2(1347.5f, 1121.5f), new Vector2(1338.5f, 1121.5f) };
            public const float GateX = 1344f, GateHalf = 1.8f;
            // the lot off the end of Lakeside Drive (the OSM drive 93 runs into its west end), axis-aligned
            public readonly Vector2 LotC = new Vector2(1339f, 1084f); public const float LotHX = 23f, LotHZ = 8f; public float LotY;
            public List<Vector2> Lot => Box(LotC, LotHX, LotHZ);
            public readonly List<Vector2> Sidewalk = new List<Vector2> { new Vector2(1329.5f, 1091.6f), new Vector2(1358.5f, 1091.6f), new Vector2(1358.5f, 1096.1f), new Vector2(1329.5f, 1096.1f) };
            // the courts: two fenced pairs, the courts' length north-south (NAIP); gates by side (0 south, 1 east, 2 north, 3 west)
            public sealed class LakesCourt { public Vector2 C; public float HX, HZ, Y; public int[] Gates; public string Name; }
            public readonly List<LakesCourt> Courts = new List<LakesCourt>
            {
                new LakesCourt { Name = "north pair", C = new Vector2(1393f, 1111.5f), HX = 16f, HZ = 17.5f, Gates = new[] { 3, 0 } },
                new LakesCourt { Name = "south pair", C = new Vector2(1381.5f, 1062f), HX = 15.5f, HZ = 17f, Gates = new[] { 3, 2 } },
            };
            // walks: the lot to the pier, the lot past the north courts to the footbridge, and the dam path north of it
            public readonly Vector2 PierRoot = new Vector2(1301.5f, 1098.5f), PierDir = new Vector2(-0.825f, 0.565f).normalized;
            public const float PierLen = 15f, GazeboHalf = 3.2f;
            public readonly List<Vector2> PierWalk = new List<Vector2> { new Vector2(1317f, 1087f), new Vector2(1310f, 1091f), new Vector2(1304f, 1096f), new Vector2(1301.5f, 1098.5f) };
            public readonly Vector2 BridgeS = new Vector2(1351.3f, 1132.5f), BridgeN = new Vector2(1353.3f, 1151.0f);
            public readonly List<Vector2> CourtWalk = new List<Vector2> { new Vector2(1361.5f, 1090f), new Vector2(1369f, 1094f), new Vector2(1374.5f, 1099f), new Vector2(1374.5f, 1124f), new Vector2(1368f, 1128.5f), new Vector2(1360f, 1130.5f), new Vector2(1351.3f, 1132.5f) };
            public readonly List<Vector2> ShorePath = new List<Vector2> { new Vector2(1353.3f, 1151.0f), new Vector2(1353.5f, 1160f), new Vector2(1357f, 1175f), new Vector2(1360f, 1185f), new Vector2(1363f, 1195f), new Vector2(1365f, 1205f), new Vector2(1368f, 1215f), new Vector2(1372f, 1225f), new Vector2(1375f, 1230f), new Vector2(1378f, 1235f) };
            public const float WalkW = 1.8f, PathW = 2.2f;
            public readonly Vector2 Swing = new Vector2(1315f, 1107f);

            public int Lake = -1; public float LakeY; public List<Vector2> LakeRing;
            readonly HashSet<int> owned = new HashSet<int>();
            public readonly List<List<Vector2>> OwnedRings = new List<List<Vector2>>(), OtherRings = new List<List<Vector2>>();
            public readonly List<(Vector2 a, Vector2 b, float half)> Roads = new List<(Vector2, Vector2, float)>();
            public readonly List<string> Log = new List<string>();

            public bool Owns(int i) => owned.Contains(i);
            public void Own(int i, List<Vector2> ring) { owned.Add(i); OwnedRings.Add(ring); }

            public static List<Vector2> Box(Vector2 c, float hx, float hz) => new List<Vector2> { c + new Vector2(-hx, -hz), c + new Vector2(hx, -hz), c + new Vector2(hx, hz), c + new Vector2(-hx, hz) };
            static bool InBox(Vector2 p, Vector2 c, float hx, float hz, float m) => Mathf.Abs(p.x - c.x) < hx + m && Mathf.Abs(p.y - c.y) < hz + m;
            static float LineDist(Vector2 p, List<Vector2> l) { float d = float.MaxValue; for (int i = 0; i + 1 < l.Count; i++) d = Mathf.Min(d, SegDist(p, l[i], l[i + 1])); return d; }

            // everything built here, grown by m (trees, other fences)
            public bool Blocks(Vector2 p, float m)
            {
                if (p.x < 1270f || p.x > 1425f || p.y < 1035f || p.y > 1245f) return false;
                if (InBox(p, DeckC, DeckHX + 0.8f, DeckHZ + 0.8f, m) || InBox(p, LotC, LotHX, LotHZ, m) || InPolygon(p, Sidewalk)) return true;
                foreach (var r in OwnedRings) if (InPolygon(p, r) || RingDist(p, r) < m + 0.5f) return true;
                foreach (var c in Courts) if (InBox(p, c.C, c.HX, c.HZ, m)) return true;
                if (LineDist(p, PierWalk) < WalkW / 2f + m || LineDist(p, CourtWalk) < PathW / 2f + m || LineDist(p, ShorePath) < PathW / 2f + m) return true;
                if (SegDist(p, BridgeS, BridgeN) < 1.5f + m || (p - Swing).magnitude < 2.5f + m) return true;
                var pe = PierRoot + PierDir * (PierLen + 2f * GazeboHalf);
                return SegDist(p, PierRoot, pe) < GazeboHalf + m;
            }
            public bool RejectTree(float x, float z) => Blocks(new Vector2(x, z), 1.5f);

            // Fences: drop any other fence run (the Round 30 back-yard fences) that would cross the area, then add the pool's iron
            // fence round the deck (0.6 m out), open where a building's wall closes it and at the gateway toward the lot
            public void PlanFences(List<FenceRun> fences)
            {
                int dropped = fences.RemoveAll(r => r.pts != null && r.pts.Count >= 2 && Densify(r.pts, 1f).Any(p => Blocks(p, 0.5f)));
                float x0 = DeckC.x - DeckHX - 0.6f, x1 = DeckC.x + DeckHX + 0.6f, z0 = DeckC.y - DeckHZ - 0.6f, z1 = DeckC.y + DeckHZ + 0.6f;
                var loop = Densify(new List<Vector2> { new Vector2(GateX + GateHalf, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0, z1), new Vector2(x0, z0), new Vector2(GateX - GateHalf, z0) }, 0.5f);
                var run = new List<Vector2>(); int runs = 0;
                void Flush() { if (run.Count >= 2) { var fr = new FenceRun { kind = FenceKind.Iron, seed = 32, closed = false }; fr.pts.AddRange(run); fences.Add(fr); runs++; } run = new List<Vector2>(); }
                foreach (var p in loop)
                {
                    bool wall = OwnedRings.Any(r => InPolygon(p, r) || RingDist(p, r) < 0.45f);
                    if (wall) Flush(); else run.Add(p);
                }
                Flush();
                Log.Add($"pool fence in {runs} iron runs (open at the buildings' walls and a {GateHalf * 2f:0.0} m gateway toward the lot); {dropped} other fence runs kept off the area");
            }
        }

        // ------------------------------------------------------------ planning and grading (before anything samples the ground)

        static ChastainLakes PlanChastainLakes(MapData map, TerrainGrid t)
        {
            var k = new ChastainLakes();
            for (int wi = 0; wi < map.water.Length; wi++) if (map.water[wi].name == "East Chastain Lake") { k.Lake = wi; break; }
            if (k.Lake >= 0 && Beds != null && k.Lake < Beds.Rings.Count)
            {
                k.LakeRing = Beds.Rings[k.Lake];
                k.LakeY = (Beds.Level.TryGetValue(k.Lake, out float lv) ? lv : WaterLevel(k.LakeRing, t)) + PondLift;
            }
            // the pool house and the bathhouse, if the map still has them where Round 32 found them
            foreach (int i in new[] { ChastainLakes.PoolHouse, ChastainLakes.Bathhouse })
            {
                if (i >= map.buildings.Length) continue;
                var r = ToPoints(map.buildings[i].footprint); if (r.Count < 3) continue;
                if (r.Count > 1 && (r[0] - r[r.Count - 1]).sqrMagnitude < 1e-4f) r.RemoveAt(r.Count - 1);
                if ((RingCentroid(r) - k.DeckC).magnitude < 30f) k.Own(i, r);
                else k.Log.Add($"building {i} is not by the pool (map changed?): left to the business dressing");
            }
            LakesOwns = k.Owns;   // MapBuilder.Round32.cs: the business dressing skips them
            var area = Rect.MinMaxRect(1260f, 1020f, 1440f, 1250f);
            for (int i = 0; i < map.buildings.Length; i++)
            {
                if (k.Owns(i)) continue; var r = ToPoints(map.buildings[i].footprint); if (r.Count < 3) continue;
                if (r.Any(p => area.Contains(p))) k.OtherRings.Add(r);
            }
            foreach (var rd in map.roads) { var p = ToPoints(rd.points); for (int i = 0; i + 1 < p.Count; i++) if (area.Contains(p[i]) || area.Contains(p[i + 1])) k.Roads.Add((p[i], p[i + 1], rd.width * 0.5f)); }
            // grading: the deck, the lot and each court pair set level, banks of 1 in 2 back to the ground within reach; never at a
            // road, another building, the lake, or within reach of the stream (BF's stream is left exactly as it is)
            k.DeckY = LakesMedian(t, k.DeckC, ChastainLakes.DeckHX, ChastainLakes.DeckHZ);
            LakesLevel(t, k, k.DeckC, ChastainLakes.DeckHX, ChastainLakes.DeckHZ, k.DeckY - 0.04f, 6f, "pool deck");
            k.LotY = LakesMedian(t, k.LotC, ChastainLakes.LotHX, ChastainLakes.LotHZ);
            LakesLevel(t, k, k.LotC, ChastainLakes.LotHX, ChastainLakes.LotHZ, k.LotY, 8f, "parking lot", 0.25f);   // 1 in 4: BUG-026's spot is on the lot's bank
            foreach (var c in k.Courts) { c.Y = LakesMedian(t, c.C, c.HX, c.HZ); LakesLevel(t, k, c.C, c.HX, c.HZ, c.Y, 8f, "courts, " + c.Name); }
            return k;
        }

        static float LakesMedian(TerrainGrid t, Vector2 c, float hx, float hz)
        {
            var s = new List<float>();
            for (float x = -hx; x <= hx; x += 1.5f) for (float z = -hz; z <= hz; z += 1.5f) s.Add(Ground(t, c.x + x, c.y + z));
            s.Sort(); return s[s.Count / 2];
        }

        static void LakesLevel(TerrainGrid t, ChastainLakes k, Vector2 c, float hx, float hz, float y, float reach, string what, float Bank = 0.5f)
        {
            int changed = 0; float most = 0f;
            EditTerrain(t, Rect.MinMaxRect(c.x - hx - reach, c.y - hz - reach, c.x + hx + reach, c.y + hz + reach), (x, z, h) =>
            {
                var p = new Vector2(x, z);
                float dx = Mathf.Max(0f, Mathf.Abs(x - c.x) - hx), dz = Mathf.Max(0f, Mathf.Abs(z - c.y) - hz), d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d > reach || Clearance(k.Roads, p) < 1f) return h;
                foreach (var r in k.OtherRings) if (InPolygon(p, r) || RingDist(p, r) < 1.5f) return h;
                if (Beds != null && (Beds.Wet(x, z) || (Beds.NearStream(p, out float sd, out _) && sd < StreamHalf + 2f))) return h;
                float nh = d <= 0f ? y : Mathf.Clamp(h, y - d * Bank, y + d * Bank);
                if (Mathf.Abs(nh - h) > 1e-3f) { changed++; most = Mathf.Max(most, Mathf.Abs(nh - h)); }
                return nh;
            });
            k.Log.Add($"{what} level at {y:0.00} m ({changed} terrain samples moved, by up to {most:0.00} m)");
        }

        // ------------------------------------------------------------ the build

        static void BuildChastainLakes(MapData map, TerrainGrid t, Materials mats, Transform parent, ChastainLakes k)
        {
            if (k == null) return;
            var root = new GameObject("ChastainLakes").transform; root.SetParent(parent, false);
            var mb = new MeshBuilder();      // deck, pool, buildings: lit, shadows, no mesh collider (boxes below)
            var wood = new MeshBuilder();    // footbridge, pier, gazebo
            var flat = new MeshBuilder();    // court surfaces and lines (depth-offset material)
            var link = new MeshBuilder();    // nets
            var lotMb = new Chunks { Tint = LotAsphalt }; var paint = new Chunks(); var walks = new Chunks { Tint = ConcreteLight };
            var furniture = new Breakables(); var fence = new Breakables(); var lamps = new Breakables(); var swing = new Breakables();
            Color deckC = new Color(0.80f, 0.77f, 0.70f), boards = new Color(0.56f, 0.42f, 0.29f), boardsDark = new Color(0.42f, 0.31f, 0.21f);

            // --- the deck and the pool (water as the Glen's pool: ShallowWater, out up the shallow end)
            var f = new Frame { O = new Vector3(k.DeckC.x, k.DeckY, k.DeckC.y), R = Vector3.right, F = Vector3.forward, M = 1f };
            float lo = float.MaxValue;
            for (float u = -ChastainLakes.DeckHX; u <= ChastainLakes.DeckHX; u += 1.5f) for (float v = -ChastainLakes.DeckHZ; v <= ChastainLakes.DeckHZ; v += 1.5f) lo = Mathf.Min(lo, Ground(t, k.DeckC.x + u, k.DeckC.y + v));
            BoxNoBottom(mb, f, -ChastainLakes.DeckHX, ChastainLakes.DeckHX, Mathf.Min(lo - 0.3f - k.DeckY, -0.3f), 0f, -ChastainLakes.DeckHZ, ChastainLakes.DeckHZ, deckC, deckC, deckC);
            var hc = new HoodCtx { map = map, terrain = t, mats = mats, root = root };
            float surface = BuildSunkPool(hc, mb, f, k.Pool, k.DeckY, ChastainLakes.DeckHX, ChastainLakes.DeckHZ, deckC, root);
            var pr = k.Pool;
            mb.Tint = new Color(0.16f, 0.72f, 0.86f);
            int pv0 = mb.Count;
            foreach (var p in pr) mb.Vertex(new Vector3(p.x, surface, p.y));
            foreach (var tri in EarClip(pr)) mb.Tri(pv0 + tri.x, pv0 + tri.z, pv0 + tri.y);
            for (int i = 0; i < pr.Count; i++)
            {
                var p0 = pr[i]; var p1 = pr[(i + 1) % pr.Count]; var d = (p1 - p0).normalized; var outN = new Vector2(d.y, -d.x);
                mb.Tint = new Color(0.93f, 0.92f, 0.88f);   // coping
                Face(mb, new Vector3(p0.x, k.DeckY + 0.035f, p0.y), new Vector3(p1.x, k.DeckY + 0.035f, p1.y), new Vector3(p1.x + outN.x * 0.35f, k.DeckY + 0.035f, p1.y + outN.y * 0.35f), new Vector3(p0.x + outN.x * 0.35f, k.DeckY + 0.035f, p0.y + outN.y * 0.35f), Vector3.up);
                mb.Tint = new Color(0.10f, 0.18f, 0.40f);   // the tile line at the waterline
                var inn = -outN * 0.015f;
                Face(mb, new Vector3(p0.x + inn.x, surface - 0.14f, p0.y + inn.y), new Vector3(p1.x + inn.x, surface - 0.14f, p1.y + inn.y), new Vector3(p1.x + inn.x, k.DeckY - 0.01f, p1.y + inn.y), new Vector3(p0.x + inn.x, k.DeckY - 0.01f, p0.y + inn.y), new Vector3(-outN.x, 0f, -outN.y));
            }
            // four lanes along the pool's length: a dark line down each lane with its T at the ends, lane ropes between them
            float px0 = pr[0].x, px1 = pr[1].x, pz0 = pr[0].y, pz1 = pr[2].y, lane = (px1 - px0) / 4f;
            void WaterQuad(float x0, float x1, float z0, float z1, float lift) => Face(mb, new Vector3(x0, surface + lift, z0), new Vector3(x1, surface + lift, z0), new Vector3(x1, surface + lift, z1), new Vector3(x0, surface + lift, z1), Vector3.up);
            mb.Tint = new Color(0.07f, 0.16f, 0.40f);
            for (int l = 0; l < 4; l++)
            {
                float cx = px0 + lane * (l + 0.5f);
                WaterQuad(cx - 0.12f, cx + 0.12f, pz0 + 2f, pz1 - 2f, 0.004f);
                foreach (float zz in new[] { pz0 + 2f, pz1 - 2f }) WaterQuad(cx - 0.5f, cx + 0.5f, zz - 0.12f, zz + 0.12f, 0.004f);
            }
            for (int l = 1; l < 4; l++)
            {
                float rx = px0 + lane * l;
                for (float z = pz0 + 0.3f; z < pz1 - 0.3f; z += 0.5f)
                    Beam(mb, new Vector3(rx, surface + 0.03f, z), new Vector3(rx, surface + 0.03f, Mathf.Min(z + 0.5f, pz1 - 0.3f)), 0.09f, 0.09f, ((int)((z - pz0) / 0.5f) % 4 < 2) ? new Color(0.80f, 0.12f, 0.12f) : LineWhite);
            }
            // loungers (striped) along both long sides facing the pool, five a side, a table and umbrella at each end of a row
            int chairs = 0, tables = 0;
            Color[] stripe = { new Color(0.15f, 0.40f, 0.62f), new Color(0.88f, 0.86f, 0.80f) };
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side < 0f ? px0 - 2.3f : px1 + 2.3f; var face = new Vector2(-side, 0f);
                for (int n = 0; n < 7; n++)
                {
                    float z = k.DeckC.y + (n - 3) * 1.4f; var p = new Vector2(x, z);
                    var m = furniture.Begin(p);
                    if (n == 0 || n == 6)
                    {
                        var g = new Vector3(p.x, k.DeckY, p.y);
                        furniture.Piece(() => { Cyl(m, g + Vector3.up * 0.7f, 0.04f, 0.55f, 0.55f, LineWhite, 10); Cyl(m, g, 0.7f, 0.04f, 0.04f, LineWhite, 5); }, k.DeckY);
                        furniture.Piece(() => { Cyl(m, g, 2.3f, 0.025f, 0.025f, new Color(0.8f, 0.8f, 0.8f), 5); Cyl(m, g + Vector3.up * 2.05f, 0.5f, 1.3f, 0.0f, new Color(0.12f, 0.30f, 0.55f), 8); }, k.DeckY);
                        furniture.End(new Vector3(p.x, k.DeckY + 0.9f, p.y), Vector3.forward, 1.3f, 1.8f, 1.3f);
                        tables++; continue;
                    }
                    var cf = MakeFrame(new Vector3(p.x, k.DeckY, p.y), face, 1f); var col = stripe[chairs % 2];
                    furniture.Piece(() =>
                    {
                        for (int s = 0; s < 6; s++) BoxF(m, cf, -0.3f, 0.3f, 0.30f, 0.36f, -0.95f + s * 0.27f, -0.95f + s * 0.27f + 0.25f, s % 2 == 0 ? col : LineWhite);
                        Beam(m, cf.P(-0.3f, 0.36f, -0.95f), cf.P(-0.3f, 0.85f, -1.25f), 0.04f, 0.04f, LineWhite); Beam(m, cf.P(0.3f, 0.36f, -0.95f), cf.P(0.3f, 0.85f, -1.25f), 0.04f, 0.04f, LineWhite);
                        BoxF(m, cf, -0.3f, 0.3f, 0.36f, 0.85f, -1.27f, -1.2f, col);
                        foreach (float uu in new[] { -0.28f, 0.28f }) foreach (float vv in new[] { -0.85f, 0.75f }) BoxF(m, cf, uu - 0.025f, uu + 0.025f, 0f, 0.3f, vv - 0.025f, vv + 0.025f, LineWhite);
                    }, k.DeckY);
                    furniture.End(new Vector3(p.x, k.DeckY + 0.45f, p.y), new Vector3(face.x, 0f, face.y), 0.7f, 0.9f, 2.2f);
                    chairs++;
                }
            }

            // --- the pool house (266) and the bathhouse (267): one story, hipped roofs, fronts toward the pool
            foreach (int bi in new[] { ChastainLakes.PoolHouse, ChastainLakes.Bathhouse })
            {
                if (!k.Owns(bi)) continue;
                var ring = ToPoints(map.buildings[bi].footprint);
                bool poolHouse = bi == ChastainLakes.PoolHouse;
                LakesHouse(mb, root, t, ring, k.DeckC, k.DeckY, poolHouse ? 3.2f : 2.8f, poolHouse ? 2.1f : 1.5f,
                           poolHouse ? new Color(0.56f, 0.30f, 0.23f) : new Color(0.86f, 0.83f, 0.74f), poolHouse ? new Color(0.24f, 0.25f, 0.27f) : new Color(0.34f, 0.44f, 0.40f), poolHouse, poolHouse ? "Pool house" : "Bathhouse");
            }

            // --- the lot: paved over the levelled ground, a 6 m apron along OSM's drive from the end of Lakeside Drive, stalls
            // along both long sides (5 m deep, 2.7 m wide, a 6 m aisle down the middle), light poles; a sidewalk to the pool gate
            var lot = k.Lot;
            DrapePoly(lotMb.At(k.LotC), lot, 1f, 0.085f, t, 8);
            var drive = map.driveways != null ? map.driveways.Select(dw => ToPoints(dw.points)).Where(q => q.Count >= 2).OrderBy(q => Mathf.Min((q[0] - new Vector2(1319f, 1085f)).magnitude, (q[q.Count - 1] - new Vector2(1319f, 1085f)).magnitude)).FirstOrDefault() : null;
            if (drive != null && Mathf.Min((drive[0] - new Vector2(1319f, 1085f)).magnitude, (drive[drive.Count - 1] - new Vector2(1319f, 1085f)).magnitude) < 4f)
            {
                if (InPolygon(drive[0], lot)) drive.Reverse();   // from the street end into the lot
                var dd = (drive[1] - drive[0]).normalized; var apron = Densify(new List<Vector2> { drive[0] + dd * 3.6f, drive[drive.Count - 1] + dd * 1.5f }, 0.75f);
                RibbonGridMax(lotMb.At(apron[0]), apron, Enumerable.Repeat(6f, apron.Count).ToArray(), 0.085f, t, 4);
                k.Log.Add($"lot apron along OSM drive from ({drive[0].x:0}, {drive[0].y:0})");
            }
            int stalls = 0;
            foreach (float side in new[] { -1f, 1f })
            {
                float zEdge = k.LotC.y + side * ChastainLakes.LotHZ, zIn = zEdge - side * 5f;
                for (float x = k.LotC.x - ChastainLakes.LotHX + 4.5f; x <= k.LotC.x + ChastainLakes.LotHX - 1f; x += 2.7f)
                {
                    PaintLine(paint.At(new Vector2(x, zEdge)), new Vector2(x, zEdge - side * 0.3f), new Vector2(x, zIn), 0.12f, 0.10f, t);
                    stalls++;
                }
            }
            foreach (var lp in new[] { new Vector2(1326f, 1092.3f), new Vector2(1352f, 1092.3f), new Vector2(1339f, 1075.7f) })
            {
                var p = lp; float gy = Ground(t, p.x, p.y); var n = new Vector2(0f, p.y > k.LotC.y ? -1f : 1f); var dark = new Color(0.20f, 0.20f, 0.22f);
                var lb = lamps.Begin(p);
                lamps.Piece(() => Post(lb, new Vector3(p.x, gy - 0.1f, p.y), 4.6f, 0.18f, dark), gy + 0.1f);
                lamps.Piece(() => Post(lb, new Vector3(p.x, gy + 4.5f, p.y), 3.4f, 0.15f, dark), gy + 0.1f);
                lamps.Piece(() => { var head = new Vector3(p.x, gy + 7.8f, p.y) + new Vector3(n.x, 0f, n.y) * 0.9f; Beam(lb, new Vector3(p.x, gy + 7.75f, p.y), head, 0.09f, 0.09f, dark); BoxF(lb, MakeFrame(head, n, 1f), -0.5f, 0.5f, -0.18f, 0.1f, -0.3f, 0.3f, dark); }, gy + 0.1f);
                lamps.End(new Vector3(p.x, gy + 1.2f, p.y), Vector3.right, 0.6f, 2.4f);
            }
            DrapePoly(walks.At(k.Sidewalk[0]), k.Sidewalk, 0.5f, 0.095f, t, 8);

            // --- the walks and the dam path (pale concrete, on the ground), and the footbridge over the outflow stream
            var bridge = LakesFootbridge(wood, root, t, k, boards, boardsDark);
            var courtWalk = new List<Vector2>(k.CourtWalk); var shore = new List<Vector2>(k.ShorePath);
            if (bridge.ok) { courtWalk[courtWalk.Count - 1] = bridge.s; shore[0] = bridge.n; }
            foreach (var (line, w) in new[] { (k.PierWalk, ChastainLakes.WalkW), (courtWalk, ChastainLakes.PathW), (shore, ChastainLakes.PathW) })
            {
                var pts = Densify(line, 1f);
                RibbonGridMax(walks.At(pts[pts.Count / 2]), pts, Enumerable.Repeat(w, pts.Count).ToArray(), 0.07f, t, 2);
            }

            // --- the pier and the gazebo on its deck out on the water
            LakesPier(wood, root, t, k, boards, boardsDark);

            // --- the porch swing on its A-frame on the lawn, facing the water (breaks like the patio furniture)
            {
                var face = k.LakeRing != null ? (Closest(k.LakeRing, k.Swing) - k.Swing).normalized : new Vector2(-0.8f, 0.6f);
                var sf = MakeFrame(new Vector3(k.Swing.x, Ground(t, k.Swing.x, k.Swing.y), k.Swing.y), face, 1f);
                float top = 2.3f;
                var m = swing.Begin(k.Swing);
                Vector3 Foot(float u, float v) { var q = sf.P(u, 0f, v); return new Vector3(q.x, Ground(t, q.x, q.z) - 0.05f, q.z); }
                foreach (float u in new[] { -1.35f, 1.35f })
                    swing.Piece(() => { Beam(m, Foot(u, -0.95f), sf.P(u, top, 0f), 0.1f, 0.1f, boards); Beam(m, Foot(u, 0.95f), sf.P(u, top, 0f), 0.1f, 0.1f, boards); Beam(m, sf.P(u, 0.8f, -0.62f), sf.P(u, 0.8f, 0.62f), 0.07f, 0.07f, boardsDark); }, sf.O.y);
                swing.Piece(() => Beam(m, sf.P(-1.5f, top + 0.05f, 0f), sf.P(1.5f, top + 0.05f, 0f), 0.14f, 0.14f, boards), sf.O.y);
                swing.Piece(() =>
                {
                    foreach (float u in new[] { -0.78f, 0.78f }) Beam(m, sf.P(u, top - 0.02f, 0f), sf.P(u, 0.55f, 0f), 0.025f, 0.025f, new Color(0.30f, 0.30f, 0.32f));
                    BoxF(m, sf, -0.8f, 0.8f, 0.45f, 0.52f, -0.28f, 0.28f, new Color(0.92f, 0.90f, 0.84f));             // the seat
                    BoxF(m, sf, -0.8f, 0.8f, 0.52f, 1.0f, -0.3f, -0.24f, new Color(0.92f, 0.90f, 0.84f));             // the back
                    foreach (float u in new[] { -0.8f, 0.8f }) BoxF(m, sf, u - 0.04f, u + 0.04f, 0.52f, 0.72f, -0.28f, 0.25f, new Color(0.92f, 0.90f, 0.84f));
                }, sf.O.y);
                swing.End(sf.P(0f, 1.2f, 0f), sf.R, 2.0f, 2.4f, 3.1f);
            }

            // --- the tennis courts
            int courts = 0, panels = 0, poles = 0;
            foreach (var c in k.Courts) LakesCourts(flat, link, fence, lamps, root, t, c, ref courts, ref panels, ref poles);

            // --- the geese (the Glen pond's flock): on the lawn by the swing, on the shore by the pier, on the dam's lawn, and on
            // the water off the lawn and off the dam
            if (k.LakeRing != null)
            {
                var go = new GameObject("Geese"); go.transform.SetParent(root, false);
                var gf = go.AddComponent<GooseFlock>();
                gf.material = mats.House; gf.pondOutline = k.LakeRing.ToArray(); gf.waterY = k.LakeY; gf.honkClips = LoadHonkClips(); gf.seed = 32;
                Vector3 At(Vector2 p) => new Vector3(p.x, Ground(t, p.x, p.y), p.y);
                Vector3 OnWater(Vector2 p) => new Vector3(p.x, k.LakeY, p.y);
                gf.zones = new[]
                {
                    new GooseFlock.Zone { kind = GooseFlock.ZoneKind.Bank, centre = At(new Vector2(1310f, 1101f)), radius = 4.5f },
                    new GooseFlock.Zone { kind = GooseFlock.ZoneKind.Bank, centre = At(new Vector2(1297f, 1093f)), radius = 3f },
                    new GooseFlock.Zone { kind = GooseFlock.ZoneKind.Bank, centre = At(new Vector2(1357f, 1172f)), radius = 5f },
                    new GooseFlock.Zone { kind = GooseFlock.ZoneKind.Water, centre = OnWater(new Vector2(1305f, 1118f)), radius = 6f },
                    new GooseFlock.Zone { kind = GooseFlock.ZoneKind.Water, centre = OnWater(new Vector2(1335f, 1152f)), radius = 4.5f },
                };
                gf.gooseZones = new[] { 0, 0, 0, 0, 0, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4 };
                var land = new Vector2(1354f, 1158f); var q = Closest(k.LakeRing, land);
                gf.heronPost = At(q + (land - q).normalized * 0.4f); gf.heron = true;
                k.Log.Add($"{gf.gooseZones.Length} geese and a heron");
            }

            mb.ToObject("LakesPoolAndBuildings", mats.House, root, false, true);
            wood.ToObject("LakesBridgePierGazebo", mats.House, root, false, true);
            flat.ToObject("LakesCourts", mats.Driveway, root, false, false);
            link.ToObject("LakesNets", mats.House, root, false, false);
            lotMb.ToObjects("LakesLot", mats.Driveway, root, false);
            paint.ToObjects("LakesLotPaint", mats.Paint, root, false);
            walks.ToObjects("LakesWalks", mats.Sidewalk, root, false);
            furniture.ToObjects("LakesPoolFurniture", mats.House, root, 0.2f, BreakSounds.Kind.Sign);
            swing.ToObjects("LakesSwing", mats.House, root, 0.3f, BreakSounds.Kind.Wood);
            fence.ToObjects("LakesCourtFence", mats.House, root, 1.2f, BreakSounds.Kind.Iron);
            lamps.ToObjects("LakesLamps", mats.House, root, 1f, BreakSounds.Kind.Lamp);
            k.Log.Add($"pool {px1 - px0:0.0} x {pz1 - pz0:0.0} m (surface {surface:0.00}, deck {k.DeckY:0.00}), {chairs} loungers, {tables} tables; lot {ChastainLakes.LotHX * 2f:0} x {ChastainLakes.LotHZ * 2f:0} m with {stalls} stalls; {courts} courts, {panels} fence panels, {poles} light poles");
            k.Log.Add(hc.Log.ToString().TrimEnd(' ', ';'));
            Debug.Log("[Round32] Chastain Lakes: " + string.Join("; ", k.Log));
        }

        // one story on its footprint's rectangle, the floor at the deck (a block foundation down to the ground), a hipped roof;
        // the front toward the pool: the pool house a glass double door and windows, the bathhouse two restroom doors
        static void LakesHouse(MeshBuilder mb, Transform root, TerrainGrid t, List<Vector2> ring, Vector2 toward, float floor, float wallH, float rise, Color siding, Color roof, bool poolHouse, string name)
        {
            if (!BestRectangle(ring, out Vector2 c, out Vector2 U, out Vector2 V, out float hl, out float hw)) return;
            var front = new[] { U, -U, V, -V }.OrderByDescending(d => Vector2.Dot(d, (toward - c).normalized)).First();
            bool frontAlongU = Mathf.Abs(Vector2.Dot(front, U)) > 0.7f;
            float w = 2f * (frontAlongU ? hw : hl), dp = 2f * (frontAlongU ? hl : hw);
            var f = MakeFrame(new Vector3(c.x, floor, c.y), front, 1f);
            float lo = float.MaxValue; foreach (var p in ring) lo = Mathf.Min(lo, Ground(t, p.x, p.y));
            BoxNoBottom(mb, f, -w / 2f, w / 2f, Mathf.Min(lo - floor - 0.3f, -0.3f), 0f, -dp / 2f, dp / 2f, FoundationBlock, FoundationBlock, FoundationBlock);
            BoxNoBottom(mb, f, -w / 2f, w / 2f, 0f, wallH, -dp / 2f, dp / 2f, siding, siding, siding);
            HipRoof(mb, f, -w / 2f, w / 2f, -dp / 2f, dp / 2f, wallH, rise, 0.5f, roof, Shade(TrimWhite, 0.95f));
            float v = dp / 2f;
            if (poolHouse)
            {
                FrontQuad(mb, f, -1.0f, 1.0f, 0f, 2.3f, v + 0.02f, TrimWhite); FrontQuad(mb, f, -0.9f, 0.9f, 0f, 2.2f, v + 0.04f, GlassDark); FrontQuad(mb, f, -0.03f, 0.03f, 0f, 2.2f, v + 0.05f, TrimWhite);
                foreach (float u in new[] { -w / 2f + 1.4f, w / 2f - 1.4f }) { if (Mathf.Abs(u) < 2f) continue; FrontQuad(mb, f, u - 0.6f, u + 0.6f, 0.9f, 2.2f, v + 0.02f, TrimWhite); FrontQuad(mb, f, u - 0.5f, u + 0.5f, 1.0f, 2.1f, v + 0.04f, GlassDark); }
                BoxF(mb, f, -1.8f, 1.8f, 2.55f, 2.7f, v, v + 1.4f, roof);   // a small canopy over the doors
            }
            else
                foreach (float u in new[] { -1.4f, 1.4f })
                {
                    FrontQuad(mb, f, u - 0.55f, u + 0.55f, 0f, 2.2f, v + 0.02f, TrimWhite);
                    FrontQuad(mb, f, u - 0.46f, u + 0.46f, 0f, 2.12f, v + 0.04f, new Color(0.30f, 0.40f, 0.36f));
                    FrontQuad(mb, f, u - 0.14f, u + 0.14f, 1.45f, 1.75f, v + 0.06f, LineWhite);   // the restroom plate (no lettering)
                }
            // windows on the sides (u = +-w/2): high, small
            foreach (float s in new[] { -1f, 1f }) for (float vv = -dp / 2f + 1.5f; vv <= dp / 2f - 1.5f; vv += 3f) SideQuad(mb, f, s * (w / 2f + 0.02f), vv - 0.5f, vv + 0.5f, 1.5f, 2.2f, s, GlassDark);
            var col = new GameObject(name + " collider"); col.transform.SetParent(root, false);
            col.transform.SetPositionAndRotation(f.P(0f, wallH / 2f, 0f), Quaternion.LookRotation(f.F, Vector3.up));
            col.AddComponent<BoxCollider>().size = new Vector3(w, wallH, dp);
        }

        // the footbridge: from where the bank on the south reaches the deck's level to where the north bank does, nothing in the
        // channel. Returns its two ends (the walks join them).
        static (bool ok, Vector2 s, Vector2 n) LakesFootbridge(MeshBuilder wood, Transform root, TerrainGrid t, ChastainLakes k, Color boards, Color dark)
        {
            Vector2 BS = k.BridgeS, BN = k.BridgeN; float L = (BN - BS).magnitude; var D = (BN - BS) / L;
            float G(float s) { var q = BS + D * s; return Ground(t, q.x, q.y); }
            float deckY = Mathf.Min(G(0f), G(L)) + 0.03f;
            float sLow = 0f, gLow = float.MaxValue; for (float s = 0f; s <= L; s += 0.25f) { float g = G(s); if (g < gLow) { gLow = g; sLow = s; } }
            float s0 = 0f, s1 = L;
            for (float s = sLow; s >= 0f; s -= 0.25f) if (G(s) >= deckY - 0.08f) { s0 = s; break; }
            for (float s = sLow; s <= L; s += 0.25f) if (G(s) >= deckY - 0.08f) { s1 = s; break; }
            s0 = Mathf.Max(0f, s0 - 0.4f); s1 = Mathf.Min(L, s1 + 0.4f);
            float len = s1 - s0;
            if (len < 3f || deckY - gLow < 1f) { k.Log.Add($"footbridge NOT built: the banks give a {len:0.0} m span {deckY - gLow:0.0} m over the stream"); return (false, BS, BN); }
            Vector2 A = BS + D * s0, B = BS + D * s1, mid = (A + B) * 0.5f;
            var bf = MakeFrame(new Vector3(mid.x, deckY, mid.y), D, 1f);   // v along the bridge, u across
            const float HW = 1.3f;
            BoxF(wood, bf, -HW, HW, -0.12f, 0f, -len / 2f, len / 2f, boards);
            wood.Tint = dark;
            for (float v = -len / 2f + 0.3f; v < len / 2f; v += 0.3f) Face(wood, bf.P(-HW, 0.004f, v - 0.012f), bf.P(HW, 0.004f, v - 0.012f), bf.P(HW, 0.004f, v + 0.012f), bf.P(-HW, 0.004f, v + 0.012f), Vector3.up);
            foreach (float s in new[] { -1f, 1f })
            {
                BoxF(wood, bf, s * HW - 0.12f, s * HW + 0.12f, -0.45f, -0.12f, -len / 2f, len / 2f, dark);                                   // the stringer
                for (float v = -len / 2f + 0.1f; v <= len / 2f; v += Mathf.Max(1.4f, (len - 0.2f) / Mathf.Max(1, Mathf.RoundToInt((len - 0.2f) / 1.6f)))) BoxF(wood, bf, s * (HW - 0.06f) - 0.06f, s * (HW - 0.06f) + 0.06f, 0f, 1.05f, v - 0.06f, v + 0.06f, boards);
                BoxF(wood, bf, s * (HW - 0.06f) - 0.05f, s * (HW - 0.06f) + 0.05f, 0.98f, 1.08f, -len / 2f, len / 2f, boards);              // top rail
                BoxF(wood, bf, s * (HW - 0.06f) - 0.03f, s * (HW - 0.06f) + 0.03f, 0.50f, 0.56f, -len / 2f, len / 2f, boards);              // middle rail
                var rc = new GameObject("Footbridge rail"); rc.transform.SetParent(root, false);
                rc.transform.SetPositionAndRotation(bf.P(s * (HW - 0.06f), 0.55f, 0f), Quaternion.LookRotation(bf.F, Vector3.up));
                rc.AddComponent<BoxCollider>().size = new Vector3(0.12f, 1.1f, len);
            }
            var dc = new GameObject("Footbridge deck"); dc.transform.SetParent(root, false);
            dc.transform.SetPositionAndRotation(bf.P(0f, -0.15f, 0f), Quaternion.LookRotation(bf.F, Vector3.up));
            dc.AddComponent<BoxCollider>().size = new Vector3(HW * 2f, 0.3f, len);
            float clear = float.NaN;
            if (Beds != null && Beds.NearStream(BS + D * sLow, out float sd, out float surf) && sd < 6f) clear = deckY - 0.45f - surf;
            k.Log.Add($"footbridge {len:0.0} x {HW * 2f:0.0} m at {deckY:0.00} m from ({A.x:0.0}, {A.y:0.0}) to ({B.x:0.0}, {B.y:0.0}), underside {(float.IsNaN(clear) ? "?" : clear.ToString("0.0"))} m over the stream, {deckY - gLow:0.0} m over the channel's floor");
            return (true, A, B);
        }

        // the pier from the shore at the end of the walk, and the gazebo on a deck at its end; deck, gazebo deck, rails and posts solid
        static void LakesPier(MeshBuilder wood, Transform root, TerrainGrid t, ChastainLakes k, Color boards, Color dark)
        {
            var R = k.PierRoot;
            float y = Mathf.Max(Ground(t, R.x, R.y) + 0.03f, k.LakeY + 0.45f);
            var pf = MakeFrame(new Vector3(R.x, y, R.y), k.PierDir, 1f);   // v out along the pier
            const float PL = ChastainLakes.PierLen, GH = ChastainLakes.GazeboHalf, HW = 1.2f;
            float gc = PL + GH;
            BoxF(wood, pf, -HW, HW, -0.15f, 0f, -0.6f, PL, boards);
            BoxF(wood, pf, -GH, GH, -0.15f, 0f, PL, PL + 2f * GH, boards);
            wood.Tint = dark;
            for (float v = -0.3f; v < PL + 2f * GH; v += 0.3f) { float hw = v < PL ? HW : GH; Face(wood, pf.P(-hw, 0.004f, v - 0.012f), pf.P(hw, 0.004f, v - 0.012f), pf.P(hw, 0.004f, v + 0.012f), pf.P(-hw, 0.004f, v + 0.012f), Vector3.up); }
            // pilings down into the lake bed where the deck stands over lower ground
            var piles = new List<Vector2>();
            for (float v = 1.5f; v < PL; v += 3f) { piles.Add(new Vector2(-HW + 0.15f, v)); piles.Add(new Vector2(HW - 0.15f, v)); }
            foreach (float u in new[] { -GH + 0.2f, GH - 0.2f }) foreach (float v in new[] { PL + 0.2f, gc, PL + 2f * GH - 0.2f }) piles.Add(new Vector2(u, v));
            foreach (var pp in piles) { var q = pf.P(pp.x, 0f, pp.y); float g = Ground(t, q.x, q.z); if (g < y - 0.3f) BoxF(wood, pf, pp.x - 0.12f, pp.x + 0.12f, g - y - 0.3f, -0.15f, pp.y - 0.12f, pp.y + 0.12f, dark); }
            // the pier's rails
            foreach (float s in new[] { -1f, 1f })
            {
                for (float v = 0.5f; v <= PL + 0.01f; v += 1.45f) BoxF(wood, pf, s * (HW - 0.06f) - 0.06f, s * (HW - 0.06f) + 0.06f, 0f, 1.05f, v - 0.06f, v + 0.06f, boards);
                BoxF(wood, pf, s * (HW - 0.06f) - 0.05f, s * (HW - 0.06f) + 0.05f, 0.98f, 1.08f, 0.5f, PL, boards);
                var rc = new GameObject("Pier rail"); rc.transform.SetParent(root, false);
                rc.transform.SetPositionAndRotation(pf.P(s * (HW - 0.06f), 0.55f, (0.5f + PL) / 2f), Quaternion.LookRotation(pf.F, Vector3.up));
                rc.AddComponent<BoxCollider>().size = new Vector3(0.12f, 1.1f, PL - 0.5f);
            }
            // the gazebo: eight white posts, a rail between them (open toward the pier), an eight-sided roof
            const float GR = 2.75f, EH = 2.6f; var white = new Color(0.94f, 0.94f, 0.92f); var roofC = new Color(0.30f, 0.36f, 0.34f);
            var postAt = new List<Vector2>();
            for (int i = 0; i < 8; i++) { float a = (22.5f + 45f * i) * Mathf.Deg2Rad; postAt.Add(new Vector2(Mathf.Cos(a) * GR, gc + Mathf.Sin(a) * GR)); }
            for (int i = 0; i < 8; i++)
            {
                var p = postAt[i]; BoxF(wood, pf, p.x - 0.09f, p.x + 0.09f, 0f, EH, p.y - 0.09f, p.y + 0.09f, white);
                var pc = new GameObject("Gazebo post"); pc.transform.SetParent(root, false);
                pc.transform.SetPositionAndRotation(pf.P(p.x, EH / 2f, p.y), Quaternion.LookRotation(pf.F, Vector3.up));
                pc.AddComponent<BoxCollider>().size = new Vector3(0.2f, EH, 0.2f);
                var q = postAt[(i + 1) % 8]; var m = (p + q) * 0.5f;
                if (m.y < gc - GR * 0.8f) continue;   // the side toward the pier stays open
                Beam(wood, pf.P(p.x, 0.95f, p.y), pf.P(q.x, 0.95f, q.y), 0.07f, 0.07f, white);
                Beam(wood, pf.P(p.x, EH - 0.12f, p.y), pf.P(q.x, EH - 0.12f, q.y), 0.12f, 0.16f, white);
                for (float s = 0.15f; s < 1f; s += 0.15f) { var b = Vector2.Lerp(p, q, s); Beam(wood, pf.P(b.x, 0.05f, b.y), pf.P(b.x, 0.93f, b.y), 0.035f, 0.035f, white); }
            }
            var top = pf.P(0f, EH, gc);
            Cyl(wood, top, 1.5f, GR + 0.6f, 0f, roofC, 8);
            wood.Tint = Shade(white, 0.9f);
            for (int i = 0; i < 8; i++)
            {
                float a0 = 45f * i * Mathf.Deg2Rad, a1 = 45f * (i + 1) * Mathf.Deg2Rad, r = GR + 0.6f;
                TriFace(wood, top, top + new Vector3(Mathf.Cos(a0) * r, 0f, Mathf.Sin(a0) * r), top + new Vector3(Mathf.Cos(a1) * r, 0f, Mathf.Sin(a1) * r), Vector3.down);
            }
            Cyl(wood, top + Vector3.up * 1.45f, 0.4f, 0.08f, 0.02f, white, 6);   // the finial
            foreach (var (c, hx, lz) in new[] { (PL / 2f - 0.3f, HW, PL + 0.6f), (gc, GH, 2f * GH) })
            {
                var dc = new GameObject(c > PL ? "Gazebo deck" : "Pier deck"); dc.transform.SetParent(root, false);
                dc.transform.SetPositionAndRotation(pf.P(0f, -0.15f, c), Quaternion.LookRotation(pf.F, Vector3.up));
                dc.AddComponent<BoxCollider>().size = new Vector3(hx * 2f, 0.3f, lz);
            }
            var gpos = pf.P(0f, 0f, gc);
            bool onWater = k.LakeRing != null && InPolygon(new Vector2(gpos.x, gpos.z), k.LakeRing);
            k.Log.Add($"pier {PL:0} m and a {GH * 2f:0.0} m gazebo deck at {y:0.00} m ({y - k.LakeY:0.00} m over the water), gazebo at ({gpos.x:0.0}, {gpos.z:0.0}){(onWater ? " on the water" : " NOT over the lake")}");
        }

        // a fenced pair of courts: green surround, blue courts with their lines and nets, 3 m chain-link in breakable ~3 m panels
        // (as the Enclave's), two open 4 m gates, four light poles outside the east and west sides
        static void LakesCourts(MeshBuilder flat, MeshBuilder link, Breakables fence, Breakables lamps, Transform root, TerrainGrid t, ChastainLakes.LakesCourt k, ref int courts, ref int panels, ref int poles)
        {
            float y = k.Y;
            Vector3 P(Vector2 q, float lift) => new Vector3(q.x, y + lift, q.y);
            void Quad(MeshBuilder m, float x0, float x1, float z0, float z1, float lift) => Face(m, new Vector3(x0, y + lift, z0), new Vector3(x1, y + lift, z0), new Vector3(x1, y + lift, z1), new Vector3(x0, y + lift, z1), Vector3.up);
            flat.Tint = CourtGreen; Quad(flat, k.C.x - k.HX, k.C.x + k.HX, k.C.y - k.HZ, k.C.y + k.HZ, 0.03f);
            const float HL = 11.885f, HWd = 5.485f, HWs = 4.115f, SL = 6.40f, LW = 0.05f;
            foreach (float off in new[] { -7.3f, 7.3f })
            {
                float cx = k.C.x + off, cz = k.C.y;
                flat.Tint = CourtBlue; Quad(flat, cx - HWd, cx + HWd, cz - HL, cz + HL, 0.045f);
                flat.Tint = LineWhite;
                foreach (float z in new[] { -HL, HL }) Quad(flat, cx - HWd, cx + HWd, cz + z - LW, cz + z + LW, 0.06f);                   // baselines
                foreach (float x in new[] { -HWd, -HWs, HWs, HWd }) Quad(flat, cx + x - LW, cx + x + LW, cz - HL, cz + HL, 0.06f);       // side lines
                foreach (float z in new[] { -SL, SL }) Quad(flat, cx - HWs, cx + HWs, cz + z - LW, cz + z + LW, 0.06f);                  // service lines
                Quad(flat, cx - LW, cx + LW, cz - SL, cz + SL, 0.06f);                                                                    // center service line
                foreach (float z in new[] { -HL + 0.2f, HL - 0.2f }) Quad(flat, cx - LW, cx + LW, cz + z - LW, cz + z + LW, 0.06f);    // center marks
                var post = new[] { new Vector2(cx - HWd - 0.914f, cz), new Vector2(cx + HWd + 0.914f, cz) };
                foreach (var pp in post) Post(link, P(pp, 0f), 1.07f, 0.08f, new Color(0.12f, 0.24f, 0.16f));
                var mid = new Vector2(cx, cz);
                for (int s = 0; s < 2; s++)
                {
                    var a0 = s == 0 ? post[0] : mid; var a1 = s == 0 ? mid : post[1];
                    float h0 = s == 0 ? 1.07f : 0.914f, h1 = s == 0 ? 0.914f : 1.07f;
                    link.Tint = new Color(0.08f, 0.08f, 0.09f);
                    Face(link, P(a0, 0.08f), P(a1, 0.08f), P(a1, h1 - 0.06f), P(a0, h0 - 0.06f), Vector3.forward); Face(link, P(a0, 0.08f), P(a1, 0.08f), P(a1, h1 - 0.06f), P(a0, h0 - 0.06f), Vector3.back);
                    link.Tint = LineWhite;
                    Face(link, P(a0, h0 - 0.06f), P(a1, h1 - 0.06f), P(a1, h1), P(a0, h0), Vector3.forward); Face(link, P(a0, h0 - 0.06f), P(a1, h1 - 0.06f), P(a1, h1), P(a0, h0), Vector3.back);
                }
                courts++;
            }
            // the fence (corners: SW, SE, NE, NW; side e runs from corner e to e + 1: 0 south, 1 east, 2 north, 3 west)
            const float FH = 3.0f;
            var corners = new[] { new Vector2(k.C.x - k.HX, k.C.y - k.HZ), new Vector2(k.C.x + k.HX, k.C.y - k.HZ), new Vector2(k.C.x + k.HX, k.C.y + k.HZ), new Vector2(k.C.x - k.HX, k.C.y + k.HZ) };
            for (int e = 0; e < 4; e++)
            {
                var p0 = corners[e]; var p1 = corners[(e + 1) % 4]; float L = (p1 - p0).magnitude; var dir = (p1 - p0) / L;
                var o2 = (((p0 + p1) * 0.5f) - k.C).normalized; var outN = new Vector3(o2.x, 0f, o2.y);
                var stretches = new List<(float s0, float s1)>();
                if (k.Gates.Contains(e)) { stretches.Add((0f, L / 2f - 2f)); stretches.Add((L / 2f + 2f, L)); } else stretches.Add((0f, L));
                foreach (var (s0, s1) in stretches)
                {
                    var q0 = p0 + dir * s0; var q1 = p0 + dir * s1;
                    int n = Mathf.Max(1, Mathf.CeilToInt((s1 - s0) / 3f));
                    for (int j = 0; j < n; j++)
                    {
                        var qa = Vector2.Lerp(q0, q1, j / (float)n); var qb = Vector2.Lerp(q0, q1, (j + 1) / (float)n); var qm = (qa + qb) * 0.5f;
                        var m = fence.Begin(qm);
                        fence.Piece(() => Post(m, P(qa, -0.1f), FH + 0.1f, 0.07f, ChainLink));
                        if (j == n - 1) fence.Piece(() => Post(m, P(qb, -0.1f), FH + 0.1f, 0.07f, ChainLink));
                        fence.Piece(() =>
                        {
                            Beam(m, P(qa, FH - 0.03f), P(qb, FH - 0.03f), 0.05f, 0.05f, ChainLink);
                            m.Tint = ChainLink;
                            float la = Vector2.Dot(qa - p0, dir), lb = Vector2.Dot(qb - p0, dir);
                            for (float sw = la + 0.12f; sw < lb; sw += 0.16f)
                            {
                                var w0 = p0 + dir * sw; var w1 = w0 + dir * 0.012f;
                                Face(m, P(w0, 0f), P(w1, 0f), P(w1, FH), P(w0, FH), outN); Face(m, P(w0, 0f), P(w1, 0f), P(w1, FH), P(w0, FH), -outN);
                            }
                            for (float h = 0.08f; h < FH; h += 0.16f) { Face(m, P(qa, h), P(qb, h), P(qb, h + 0.012f), P(qa, h + 0.012f), outN); Face(m, P(qa, h), P(qb, h), P(qb, h + 0.012f), P(qa, h + 0.012f), -outN); }
                        });
                        if (e == 3)   // the green windscreen on the west side (toward the pool and the path)
                            fence.Piece(() =>
                            {
                                m.Tint = new Color(0.11f, 0.29f, 0.18f);
                                Face(m, P(qa + o2 * 0.03f, 0.3f), P(qb + o2 * 0.03f, 0.3f), P(qb + o2 * 0.03f, 2.1f), P(qa + o2 * 0.03f, 2.1f), outN);
                                Face(m, P(qa - o2 * 0.03f, 0.3f), P(qb - o2 * 0.03f, 0.3f), P(qb - o2 * 0.03f, 2.1f), P(qa - o2 * 0.03f, 2.1f), -outN);
                            });
                        fence.End(P(qm, FH / 2f), new Vector3(dir.x, 0f, dir.y), 0.9f, FH, (qb - qa).magnitude);
                        panels++;
                    }
                }
            }
            // light poles: two outside each of the east and west sides (break like the street lamps)
            foreach (int e in new[] { 1, 3 })
            {
                var p0 = corners[e]; var p1 = corners[(e + 1) % 4]; var outV = (((p0 + p1) * 0.5f) - k.C).normalized;
                foreach (float s in new[] { 0.3f, 0.7f })
                {
                    var p = Vector2.Lerp(p0, p1, s) + outV * 0.6f; float gy = Ground(t, p.x, p.y);
                    var lb = lamps.Begin(p);
                    lamps.Piece(() => Post(lb, new Vector3(p.x, gy - 0.1f, p.y), 4.2f, 0.16f, ChainLink), gy + 0.1f);
                    lamps.Piece(() => Post(lb, new Vector3(p.x, gy + 4.1f, p.y), 3.9f, 0.13f, ChainLink), gy + 0.1f);
                    lamps.Piece(() =>
                    {
                        var head = new Vector3(p.x, gy + 8.0f, p.y) - new Vector3(outV.x, 0f, outV.y) * 0.5f;
                        Beam(lb, new Vector3(p.x, gy + 7.9f, p.y), head, 0.08f, 0.08f, ChainLink);
                        BoxF(lb, MakeFrame(head, -outV, 1f), -0.45f, 0.45f, -0.15f, 0.12f, -0.3f, 0.3f, new Color(0.2f, 0.2f, 0.22f));
                    }, gy + 0.1f);
                    lamps.End(new Vector3(p.x, gy + 1.2f, p.y), new Vector3(outV.x, 0f, outV.y), 0.6f, 2.4f);
                    poles++;
                }
            }
        }
    }
}
