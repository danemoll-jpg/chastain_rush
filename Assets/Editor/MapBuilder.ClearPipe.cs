using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 21 follow-ups, item 1 (Dan: "can we have the pipe be clear and you can see fish swimming around?"; Claude's reading, confirmed
// by Dan: part of the pipe becomes a CLEAR tube running through water, like an aquarium tunnel, with fish swimming round it). A pipe in
// Data/pipes/pipes.json with 'clear' gets it (P1):
//   the pool    P1 ran under dry ground between Noonday Creek and the small pond. The small pond itself is only about 38 m across on
//               P1's line, and its east shore is where P1J's flight (forward) and RA's take-off (reverse) are, so the pipe cannot pass
//               under it without moving the outlet and losing both. Instead the small pond's water is brought west over P1's middle:
//               a pool at the small pond's own level (the same surface), along the low swale that already runs from there toward the
//               pond, dug into the ground (only lowered, banks at 1 in 3), its shores straight across the pipe where the clear stretch
//               ends. Registered as water like every pond (ShallowWater: a vehicle wades its shallow shelf; no trees in it)
//   the floor   in the clear stretch the pipe sits deeper, its crown 'water_over_m' under the surface (the cut slopes and both mouths
//               are as before); the clear stretch is where the crown is 0.6 m or more under it (27 m on P1)
//   the trench  round the tube the pond is deep: a trench in the pool's bed (terrain holes over it), its bed of silt with rocks and
//               weeds 1.6 m under the tube, walls sloping up to the pool's bed 8 m either side; an invisible lid at the bed's level
//               over it, so a vehicle wading the pool never drops in (and only what is in the tube sees the deep water)
//   the tube    the barrel there is glass (ChastainRush/PipeGlass: clear looking straight through, tinted and glinting at grazing
//               angles), steel ribs round it every 3 m and thick end rings where it meets the concrete; the same round collider as
//               the rest of the barrel (wall riding, the camera, resets: DrainPipe is unchanged), no lamps (daylight), the stream
//               runs on along its bottom
//   the water   seen from in the tube: the surface overhead (ChastainRush/UnderwaterScroll: rippling, moving), light shafts down
//               from it, two faint layers of water haze inside the trench, the bed's colors bluer and darker with depth
//   the life    fish (PipeAquarium, run time: schools of small fish, sunfish, three big carp and bass, drifting and darting, drawn
//               instanced with ChastainRush/Fish: tails wag, no physics), weeds that sway (the same shader), a sound change inside
//               (the world goes muffled, a low underwater hum with bubbles)
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class PipeClearJson { public float from, to, water_over_m = 1f, pool_half_m = 12f, trench_half_m = 8f; }
        const float ClearCrownUnder = 0.6f, TrenchBelow = 1.6f, TrenchBottomHalf = 3.8f, GlassOut = 0.05f, PoolBank = 0.33f;

        static bool HasClear(PipeJson j) => j.clear != null && j.clear.to > j.clear.from;

        // the small pond's surface: the pond whose outline holds the point a little past the pipe's outlet
        static float PondSurfaceNear(Vector2 q, out int ring)
        {
            ring = -1;
            for (int i = 0; i < Beds.Rings.Count; i++) if (Beds.Level.ContainsKey(i) && Beds.RingBounds[i].Contains(q) && InPolygon(q, Beds.Rings[i])) { ring = i; return Beds.Level[i] + PondLift; }
            return float.NaN;
        }

        // where the crown is ClearCrownUnder or more under the surface, inside the json's from..to
        static void ClearStretch(PipePlan p, float[] floor)
        {
            float crown = p.J.diameter_m + GlassOut; p.C0 = -1f; p.C1 = -1f;
            for (float s = p.J.clear.from; s <= p.J.clear.to + 1e-3f; s += PipeStep)
            {
                float f = Mathf.Clamp(s / PipeStep, 0f, floor.Length - 1.001f); int i = (int)f; float y = Mathf.Lerp(floor[i], floor[i + 1], f - i);
                if (y + crown > p.Surface - ClearCrownUnder) continue;
                if (p.C0 < 0f) p.C0 = s; p.C1 = s;
            }
        }

        // the pool: a rounded rectangle over the clear stretch (its ends straight across the pipe at the stretch's ends, wide enough for
        // the trench; its sides 'pool_half_m' out, wavering a little), dug into the ground at the pond's level
        static void DigPool(TerrainGrid terrain, PipePlan p, float[] floor)
        {
            var cj = p.J.clear; float W = cj.pool_half_m, T = cj.trench_half_m + 0.5f, Rc = W - T;
            var ring = new List<Vector2>();
            void Add(float s, float lat) => ring.Add(p.P(s, lat));
            // round from the C0 end's right corner, along the left side, the C1 end, back along the right side
            for (float lat = -T; lat <= T + 1e-3f; lat += 2f) Add(p.C0, lat);
            for (int k = 1; k <= 6; k++) { float a = Mathf.PI * 0.5f * k / 6f; Add(p.C0 + Rc * (1f - Mathf.Cos(a)), T + Rc * Mathf.Sin(a)); }
            for (float s = p.C0 + Rc + 2f; s < p.C1 - Rc - 1f; s += 2f) Add(s, W + 0.8f * Mathf.Sin(s * 0.41f) * Mathf.Sin(s * 0.17f + 1f));
            for (int k = 0; k <= 6; k++) { float a = Mathf.PI * 0.5f * k / 6f; Add(p.C1 - Rc * (1f - Mathf.Sin(a)) , T + Rc * Mathf.Cos(a)); }
            for (float lat = T - 2f; lat >= -T - 1e-3f; lat -= 2f) Add(p.C1, lat);
            for (int k = 1; k <= 6; k++) { float a = Mathf.PI * 0.5f * k / 6f; Add(p.C1 - Rc * (1f - Mathf.Cos(a)), -T - Rc * Mathf.Sin(a)); }
            for (float s = p.C1 - Rc - 2f; s > p.C0 + Rc + 1f; s -= 2f) Add(s, -W - 0.8f * Mathf.Sin(s * 0.37f + 2f) * Mathf.Sin(s * 0.19f));
            for (int k = 0; k < 6; k++) { float a = Mathf.PI * 0.5f * k / 6f; Add(p.C0 + Rc * (1f - Mathf.Sin(a)), -T - Rc * Mathf.Cos(a)); }
            p.Pool = ring;
            float level = p.Surface - PondLift, most = 0f; int lowered = 0;
            float Crown(float s) { float f = Mathf.Clamp(s / PipeStep, 0f, floor.Length - 1.001f); int i = (int)f; return Mathf.Lerp(floor[i], floor[i + 1], f - i) + p.J.diameter_m + PipeWall; }
            EditTerrain(terrain, BoundsOf(ring, 14f), (x, z, y) =>
            {
                var q = new Vector2(x, z); float d = DistToEdges(q, ring); bool inside = InPolygon(q, ring);
                if (!inside && d > 14f) return y;
                float target = inside ? level - Mathf.Min(BedDepth - PondLift, d * PondSlope) : level + d * PoolBank;
                // never off the pipe's crown beyond the clear stretch (0.5 m of ground kept over it), never into the cuts
                float s = Vector2.Dot(q - p.A, p.U), lat = Vector2.Dot(q - p.A, p.Nl);
                if (Mathf.Abs(lat) < p.R + PipeWall + 2f && (s < p.C0 || s > p.C1)) target = Mathf.Max(target, Crown(Mathf.Clamp(s, 0f, p.L)) + 0.5f);
                if (s < 15f || s > p.L - 18f) return y;
                if (target >= y) return y;
                lowered++; most = Mathf.Max(most, y - target); return target;
            });
            // a pond like the others (trees kept out of it, wading, the map)
            Beds.Rings.Add(ring); Beds.RingBounds.Add(BoundsOf(ring, 0f)); Beds.Level[Beds.Rings.Count - 1] = level;
            p.PoolRing = Beds.Rings.Count - 1;
            Debug.Log($"[Round21+] pipe {p.J.id}: a pool over its clear stretch at the small pond's level ({p.Surface:0.00} m), {p.C1 - p.C0:0.0} m along the pipe and {2f * W:0} m across, dug up to {most:0.00} m ({lowered} terrain samples lowered; banks 1 in {1f / PoolBank:0})");
        }

        // terrain holes over the trench (the band round the tube over the clear stretch). Round 25 (A5 / A7): only where the bowl of deep
        // water (ClearFrame.BowlY) is below the ground, and only under water (every corner of the cell under the surface): never in a bank
        static void CutTrenchHoles(TerrainGrid g, PipePlan p)
        {
            var f = P1Frame(p);
            Debug.Log($"[Round21+] pipe {p.J.id}: {CutBowlHoles(g, f)} terrain hole cells over the deep water round its clear stretch (Round 25: under water only)");
        }

        static int CutBowlHoles(TerrainGrid g, ClearFrame f)
        {
            var corners = new List<Vector2>(); for (float s = f.C0; s <= f.C1 + 1e-3f; s += 2f) foreach (float side in new[] { -1f, 1f }) { var w = f.W(s, side * (f.Rim(s) + 1.5f), 0f); corners.Add(new Vector2(w.x, w.z)); }
            var area = BoundsOf(corners, 2f); float hcx = 0.33f, hcz = 0.32f;
            return SetHoles(g, area, q =>
            {
                var sl = f.ToSL(q); float s = sl.x, lat = sl.y;
                if (s < f.C0 || s > f.C1 || Mathf.Abs(lat) > f.Rim(s) + 1f) return false;
                float hi = float.MinValue; foreach (var (dx, dz) in new[] { (-hcx, -hcz), (hcx, -hcz), (hcx, hcz), (-hcx, hcz) }) hi = Mathf.Max(hi, Ground(g, q.x + dx, q.y + dz));
                if (hi > f.Surface - 0.03f) return false;   // a bank: never holed (no flaps over the water, no gap under them)
                if (f.Water != null) foreach (var (dx, dz) in new[] { (-hcx, -hcz), (hcx, -hcz), (hcx, hcz), (-hcx, hcz) }) if (!InPolygon(new Vector2(q.x + dx, q.y + dz), f.Water)) return false;
                return f.BowlY(s, lat) < Ground(g, q.x, q.y) - 0.05f;
            });
        }

        // P1's frame: straight, level; its deep water as wide as its pool (1 m in from the pool's shore)
        static ClearFrame P1Frame(PipePlan p)
        {
            float axisY = p.Y((p.C0 + p.C1) * 0.5f) + p.R;
            var pool = p.Pool;
            return new ClearFrame { Id = p.J.id, R = p.R, C0 = p.C0, C1 = p.C1, Surface = p.Surface, T = p.J.clear.trench_half_m, AxisY = axisY, AY = s => axisY,
                                    W = (s, lat, y) => { var q = p.P(s, lat); return new Vector3(q.x, y, q.y); }, U3 = s => new Vector3(p.U.x, 0f, p.U.y), N3 = s => new Vector3(p.Nl.x, 0f, p.Nl.y),
                                    ToSL = q => new Vector2(Vector2.Dot(q - p.A, p.U), Vector2.Dot(q - p.A, p.Nl)), Water = pool,
                                    TS = pool == null ? (Func<float, float>)null : s => Mathf.Clamp(DistToEdges(p.P(s, 0f), pool) - 1f, p.R + 1.2f, 45f) };
        }

        static Material clearGlass, clearHaze, clearSurface, clearShaft, clearWeed;
        static Material[] fishMats;

        static void ClearMaterials()
        {
            if (clearGlass) return;
            Material M(string shader, string name, Action<Material> set) { var m = new Material(Shader.Find(shader)) { name = name }; set(m); m = GenAsset(m, $"{GeneratedDir}/{name}.mat"); return m; }
            clearGlass = M("ChastainRush/PipeGlass", "PipeGlass", m => { m.SetColor("_BaseColor", new Color(0.55f, 0.85f, 0.90f, 0.10f)); m.SetColor("_EdgeColor", new Color(0.70f, 0.92f, 1.0f, 0.55f)); m.SetFloat("_Power", 3f); });
            clearHaze = M("ChastainRush/PipeGlass", "PipeWaterHaze", m => { m.SetColor("_BaseColor", new Color(0.10f, 0.42f, 0.46f, 0.16f)); m.SetColor("_EdgeColor", new Color(0.12f, 0.45f, 0.50f, 0.30f)); m.SetFloat("_Power", 1.5f); m.renderQueue = 2990; });
            var rip = RippleTexture(); rip = GenAsset(rip, GeneratedDir + "/UnderwaterRipple.asset");
            clearSurface = M("ChastainRush/UnderwaterScroll", "UnderwaterSurface", m => { m.SetTexture("_BaseMap", rip); m.SetTextureScale("_BaseMap", new Vector2(0.12f, 0.12f)); m.SetColor("_BaseColor", new Color(0.62f, 0.90f, 0.92f)); m.SetFloat("_Cull", (float)CullMode.Back); });
            clearShaft = M("ChastainRush/UnderwaterScroll", "UnderwaterShaft", m => { m.SetColor("_BaseColor", new Color(0.30f, 0.42f, 0.36f)); m.SetFloat("_Src", (float)BlendMode.One); m.SetFloat("_Dst", (float)BlendMode.One); m.SetFloat("_ZWrite", 0f); m.SetFloat("_Shaft", 1f); m.renderQueue = 3000; });
            clearWeed = M("ChastainRush/Fish", "PondWeed", m => { m.SetFloat("_Wag", 0.25f); m.SetFloat("_WagSize", 0.22f); m.SetFloat("_Mode", 1f); });
            fishMats = new[]
            {
                M("ChastainRush/Fish", "FishSmall", m => { m.SetFloat("_Wag", 3.2f); m.SetFloat("_WagSize", 0.16f); m.enableInstancing = true; }),
                M("ChastainRush/Fish", "FishSunfish", m => { m.SetFloat("_Wag", 2.2f); m.SetFloat("_WagSize", 0.12f); m.enableInstancing = true; }),
                M("ChastainRush/Fish", "FishBig", m => { m.SetFloat("_Wag", 0.9f); m.SetFloat("_WagSize", 0.10f); m.enableInstancing = true; }),
                // Round 25 (A8): turtles (a slow paddle), water snakes (a long winding wave), ducks and geese (still)
                M("ChastainRush/Fish", "Turtle", m => { m.SetFloat("_Wag", 0.6f); m.SetFloat("_WagSize", 0.03f); m.enableInstancing = true; }),
                M("ChastainRush/Fish", "WaterSnake", m => { m.SetFloat("_Wag", 0.9f); m.SetFloat("_WagSize", 0.32f); m.enableInstancing = true; }),
                M("ChastainRush/Fish", "WaterBird", m => { m.SetFloat("_Wag", 0f); m.SetFloat("_WagSize", 0f); m.enableInstancing = true; }),
            };
        }

        // the surface from below: soft bright cells (a caustic-like web from two Perlin layers), tiling
        static Texture2D RippleTexture()
        {
            const int N = 128; var t = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "UnderwaterRipple", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            var px = new Color[N * N];
            float P(float x, float y, float f) => Mathf.PerlinNoise((x % N) / N * f + 11f, (y % N) / N * f + 7f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    // tile by blending four shifted samples
                    float u = x / (float)N, v = y / (float)N;
                    float a = Mathf.Lerp(Mathf.Lerp(P(x, y, 8), P(x + N, y, 8), u), Mathf.Lerp(P(x, y + N, 8), P(x + N, y + N, 8), u), v);
                    float web = 1f - Mathf.Abs(a - 0.5f) * 2f; web = Mathf.Pow(web, 6f);
                    float b = 0.55f + 0.45f * web; px[y * N + x] = new Color(b, b, b, 1f);
                }
            t.SetPixels(px); t.Apply(true); return t;
        }

        // ---------------------------------------------------------------- the build (from BuildPipe)
        // Round 23 follow-ups: the stretch is described by a frame along its line (a straight pipe's, or a network pipe's curve:
        // MapBuilder.Network.cs), so the network's glass stretches are built from the same parts as P1's
        sealed class ClearFrame
        {
            public string Id; public float R, C0, C1, Surface, T, AxisY; public Func<float, float> AY;   // Round 23 follow-ups: AY, the axis's height along the stretch (a network pipe's glass may slope); AxisY its lowest
            public Func<float, float, float, Vector3> W;   // (s, lateral, y) to the world
            public Func<float, Vector3> U3, N3;            // along and left at s (level)
            // Round 24 Part B (item 5, Dan: "if I am in the middle of a lake I would want it to look like I am in the middle of a lake then in a
            // channel"): a network glass stretch's deep water is as wide as its lake at each point (TS, the half width at the surface), its
            // bed falling away on both sides from the shore, the bottom half as wide (open water all round the tube, no walls hugging it);
            // null: P1's trench, T wide all along
            public Func<float, float> TS;
            public float TAt(float s) => TS != null ? TS(s) : T;
            public float BottomAt(float s) => TS != null ? Mathf.Max(TrenchBottomHalf, 0.55f * TS(s)) : TrenchBottomHalf;
            // Round 25 (A7, BUG-007, Dan in M1's glass under the pond: "this still appears to be a trench"; A5, BUG-004 by M5's glass: "this
            // should be cleaned up"). The deep water was a cut trench: sloping straight walls from its bed up to the surface, closed at each
            // end by a wall across it, and its terrain holes reached into the banks (ragged grass flaps over the water, a gap under them).
            // Now it is a BOWL in the pond's bed: from the shore's shelf (0.2 m under the surface, 1 m in from the shore: TS) the bed falls
            // in a smooth curve (steepening toward the shore) to a bed 1.6 m under the tube, with a little unevenness; toward the stretch's
            // ends (over its last 10 m) the bowl closes in round the tube and rises to just under it, where the tube goes into the bed.
            // The terrain is holed only where the bowl is below it and under the water (CutBowlHoles)
            public Func<Vector2, Vector2> ToSL;   // world (x, z) to (s, lateral)
            // Round 25 (BUG-002 at the Enclave pond, Dan: "Water cant act like this. Need to fix this so that he water flush with the ground"):
            // the pond's water outline; a terrain hole only where its whole cell is inside it, so the water's surface always covers it (a low
            // bank just outside the outline was holed, and the deep water showed from the bank past the water's edge)
            public List<Vector2> Water;
            float Lend => Mathf.Min(10f, (C1 - C0) * 0.3f);
            public float E(float s) => Smooth01(Mathf.Min(s - C0, C1 - s) / Mathf.Max(0.5f, Lend));
            public float AYs(float s) => AY != null ? AY(s) : AxisY;
            public float Rim(float s) => Mathf.Lerp(R + 1.2f, Mathf.Max(R + 1.2f, TAt(s)), E(s));
            public float Bottom(float s) => AYs(s) - R - GlassOut - Mathf.Lerp(0.3f, TrenchBelow, E(s));
            public float Top => Surface - 0.2f;
            public float BowlY(float s, float lat)
            {
                float a = Mathf.Abs(lat), rim = Rim(s), b0 = Mathf.Min(R * 0.8f, rim - 0.6f), bot = Bottom(s), y;
                if (a <= b0) y = bot;
                else if (a <= rim) { float u = (a - b0) / (rim - b0); y = bot + (Top - bot) * Mathf.Pow(u, 1.7f) + 0.14f * (Mathf.PerlinNoise(s * 0.35f + 5f, lat * 0.35f + 3f) - 0.5f) * Mathf.Clamp01(u * 4f); }
                else y = Top + (a - rim) * 0.3f;
                float rt = R + GlassOut + 0.15f;   // never into the tube
                if (a < rt) y = Mathf.Min(y, AYs(s) - Mathf.Sqrt(rt * rt - a * a) - 0.05f);
                return y;
            }
        }
        static void BuildClearStretch(TerrainGrid terrain, Materials mats, Transform root, PipePlan p) => BuildClearStretch(terrain, mats, root, P1Frame(p));
        static void BuildClearStretch(TerrainGrid terrain, Materials mats, Transform root, ClearFrame p)
        {
            ClearMaterials();
            float T = p.T, R = p.R;
            Vector3 W(float s, float lat, float y) => p.W(s, lat, y);
            var up3 = Vector3.up;
            float axisY = p.AxisY;   // the floor is level across the clear stretch (P1); a network pipe's: its lowest
            float AY(float s) => p.AYs(s);
            float surf = p.Surface, bot = axisY - R - TrenchBelow, top = surf - 0.03f, bedY = surf - BedDepth;
            float Gnd(float s, float lat) { var w = W(s, lat, 0f); return Ground(terrain, w.x, w.z); }
            // Round 25 (A5 / A7): the deep water is the bowl under the surface and the ground (ClearFrame.BowlY)
            bool Inside(float s, float lat, float y) => y >= p.BowlY(s, lat) && y <= Mathf.Min(top, Gnd(s, lat));
            float Depth01(float y) => Mathf.InverseLerp(top, bot, y);
            Color Under(Color c, float y) => Color.Lerp(c, new Color(0.10f, 0.26f, 0.28f), 0.25f + 0.55f * Depth01(y));
            var silt = new Color(0.42f, 0.42f, 0.32f); var stone = new Color(0.46f, 0.47f, 0.44f);

            // ---- the bed: a sheet across the stretch every meter, out past the bowl's rim (there it lies just under the terrain, which
            // covers it), its height the bowl's or the ground's (8 cm under it), whichever is lower
            var bed = new MeshBuilder();
            float len = p.C1 - p.C0; int ns = Mathf.CeilToInt(len / 1f);
            const int across = 24;
            float BedAt(float s, float lat) => Mathf.Min(p.BowlY(s, lat), Gnd(s, lat) - 0.08f);
            Vector3 Pt(float s, int k) { float half = p.Rim(s) + 2f, lat = Mathf.Lerp(half, -half, k / (float)(2 * across)); return W(s, lat, BedAt(s, lat)); }
            for (int i = 0; i < ns; i++)
            {
                float sa = p.C0 + len * i / ns, sb = p.C0 + len * (i + 1) / ns;
                for (int k = 0; k < 2 * across; k++)
                {
                    Vector3 a0 = Pt(sa, k), a1 = Pt(sa, k + 1), b0 = Pt(sb, k), b1 = Pt(sb, k + 1);
                    float y = (a0.y + a1.y) * 0.5f; float n = Mathf.PerlinNoise(sa * 0.3f + k * 0.21f, 3f);
                    bed.Tint = Under(Shade(Color.Lerp(silt, new Color(0.30f, 0.36f, 0.22f), n * 0.6f), 0.85f + 0.25f * n), y);
                    Face(bed, a0, b0, b1, a1, up3);
                }
            }
            // the ends: where the tube goes into the bed, a face across the bowl's (small) cross-section round the tube, up to the ground
            foreach (var (s, sgn) in new[] { (p.C0, 1f), (p.C1, -1f) })
            {
                var u3 = p.U3(s); var n3 = p.N3(s);
                var c = W(s, 0f, AY(s)); float axS = AY(s); const int seg = 48; float ro = R;   // the opening is the tube's own inside: no gap round it to see past
                for (int i = 0; i < seg; i++)
                {
                    float t0 = Mathf.PI * 2f * i / seg, t1 = Mathf.PI * 2f * (i + 1) / seg;
                    Vector3 C(float th) => c + n3 * (Mathf.Sin(th) * ro) - up3 * (Mathf.Cos(th) * ro);
                    Vector3 Edge(float th)   // out from the center along th to the bowl's outline (its bed, the surface, the ground)
                    {
                        var d = new Vector2(Mathf.Sin(th), -Mathf.Cos(th)); float lo = ro, hi = 30f;
                        for (int it = 0; it < 24; it++) { float mid = (lo + hi) * 0.5f; if (Inside(s, d.x * mid, axS + d.y * mid)) lo = mid; else hi = mid; }
                        return c + n3 * (d.x * lo) + up3 * (d.y * lo);
                    }
                    var e0 = Edge(t0); bed.Tint = Under(Shade(silt, 0.8f), e0.y);
                    Face(bed, C(t0), C(t1), Edge(t1), e0, u3 * sgn); Face(bed, C(t0), C(t1), Edge(t1), e0, -u3 * sgn);   // both faces: nothing past it shows from any angle
                }
            }
            var bedGo = bed.ToObject("Pipe " + p.Id + " clear stretch - the deep water bed", mats.House, root, true, false);   // Round 23 follow-ups: solid (nothing falls out of the world through it)
            // rocks on the bowl's bed and slopes (look only), never on the tube's line
            var rocks = new MeshBuilder(); int nRocks = 0;
            int rockN = Mathf.Max(46, Mathf.RoundToInt(len * 1.5f));   // Round 24 Part B: as many per meter on a long stretch
            for (int i = 0; i < rockN; i++)
            {
                float s = p.C0 + 0.8f + (len - 1.6f) * Hash01(i, 1211), side = Hash01(i, 1213) < 0.5f ? -1f : 1f;
                float lat = side * Mathf.Lerp(R + 0.9f, p.Rim(s) - 0.4f, Mathf.Pow(Hash01(i, 1215), 1.4f));
                if (Mathf.Abs(lat) < R + 0.9f) continue;
                float y = BedAt(s, lat); if (y > Gnd(s, lat) - 0.2f) continue;
                var cc = W(s, lat, y - 0.1f); float r = 0.25f + 0.5f * Hash01(i, 1221);
                rocks.Tint = Under(Shade(stone, 0.7f + 0.35f * Hash01(i, 1223)), y);
                var apex = cc + up3 * r * (0.7f + 0.5f * Hash01(i, 1225));
                Vector3[] dirs = { Vector3.right, (Vector3.right + Vector3.forward).normalized, Vector3.forward, (Vector3.forward + Vector3.left).normalized, Vector3.left, (Vector3.left + Vector3.back).normalized, Vector3.back, (Vector3.back + Vector3.right).normalized };
                for (int k = 0; k < 8; k++) TriFace(rocks, cc + dirs[k] * r * (1.0f + 0.35f * Hash01(i * 8 + k, 1227)), cc + dirs[(k + 1) % 8] * r * (1.0f + 0.35f * Hash01(i * 8 + (k + 1) % 8, 1227)), apex, dirs[k] + dirs[(k + 1) % 8] + Vector3.up);
                nRocks++;
            }
            rocks.ToObject("Pipe " + p.Id + " clear stretch - rocks", mats.House, root, false, false);
            // weeds: clumps of ribbons rising from the bed and the slopes; they sway (ChastainRush/Fish, _Mode 1: vertex alpha is how far up
            // the blade)
            var weeds = new MeshBuilder(); int nClumps = 0;
            int weedN = Mathf.Max(38, Mathf.RoundToInt(len * 1.3f));
            for (int i = 0; i < weedN; i++)
            {
                float s = p.C0 + 1f + (len - 2f) * Hash01(i, 1231), side = Hash01(i, 1233) < 0.5f ? -1f : 1f;
                float lat = side * Mathf.Lerp(R + 0.9f, p.Rim(s) - 0.3f, Hash01(i, 1237));
                if (Mathf.Abs(lat) < R + 0.9f) continue;
                float y = BedAt(s, lat); if (y > Gnd(s, lat) - 0.2f || y > top - 0.4f) continue;
                var at = W(s, lat, y);
                int blades = 4 + (int)(5 * Hash01(i, 1239));
                for (int b = 0; b < blades; b++)
                {
                    int h0 = i * 16 + b;
                    float h = Mathf.Min(0.8f + 1.9f * Hash01(h0, 1241), top - y - 0.15f), wdt = 0.07f + 0.06f * Hash01(h0, 1243), ang = 6.283f * Hash01(h0, 1245);
                    if (h < 0.3f) continue;
                    var off = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 0.25f * Hash01(h0, 1247);
                    var side3 = new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * wdt;
                    var lean = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 0.25f;
                    var g = Color.Lerp(new Color(0.20f, 0.42f, 0.16f), new Color(0.38f, 0.52f, 0.20f), Hash01(h0, 1251));
                    const int segs = 5; int first = weeds.Count;
                    for (int k = 0; k <= segs; k++)
                    {
                        float t = k / (float)segs; var c = at + off + up3 * (h * t) + lean * (t * t);
                        weeds.Tint = new Color(g.r * (0.7f + 0.4f * t), g.g * (0.7f + 0.4f * t), g.b, t);
                        weeds.Vertex(c + side3 * (1f - 0.7f * t)); weeds.Vertex(c - side3 * (1f - 0.7f * t));
                    }
                    for (int k = 0; k < segs; k++) { int a0 = first + k * 2; weeds.Tri(a0, a0 + 2, a0 + 1); weeds.Tri(a0 + 1, a0 + 2, a0 + 3); weeds.Tri(a0, a0 + 1, a0 + 2); weeds.Tri(a0 + 1, a0 + 3, a0 + 2); }
                }
                nClumps++;
            }
            weeds.ToObject("Pipe " + p.Id + " clear stretch - weeds", clearWeed, root, false, false);
            // ---- the surface seen from below, over the bowl and out to the shore (down-facing; the pond's own surface is drawn from above)
            var sv = new List<Vector3>(); var suv = new List<Vector2>(); var st = new List<int>();
            for (int i = 0; i <= ns; i++)
                for (int k = 0; k <= 8; k++)
                {
                    float s = p.C0 + len * i / ns, lat = Mathf.Lerp(-p.Rim(s) - 1.5f, p.Rim(s) + 1.5f, k / 8f); var q = W(s, lat, surf - 0.02f);
                    sv.Add(q); suv.Add(new Vector2(q.x, q.z));
                    if (i > 0 && k > 0)
                    {
                        int a = i * 9 + k; var mid = (sv[a - 10] + sv[a]) * 0.5f;
                        if (p.Water != null && !InPolygon(new Vector2(mid.x, mid.z), p.Water)) continue;   // Round 25 (BUG-002): only under the water itself
                        st.Add(a - 10); st.Add(a - 1); st.Add(a - 9); st.Add(a - 9); st.Add(a - 1); st.Add(a);
                    }
                }
            if (st.Count == 0) { st.Add(0); st.Add(0); st.Add(0); }   // Round 25: none under the water (a degenerate triangle keeps the mesh valid)
            {   // facing down (seen only from below: from above the pool's own surface is drawn)
                var n0 = Vector3.Cross(sv[st[1]] - sv[st[0]], sv[st[2]] - sv[st[0]]);
                if (n0.y > 0f) for (int i = 0; i < st.Count; i += 3) { int x = st[i + 1]; st[i + 1] = st[i + 2]; st[i + 2] = x; }
            }
            var smesh = new Mesh { name = "Pipe " + p.Id + " surface from below" }; smesh.SetVertices(sv); smesh.SetUVs(0, suv); smesh.SetColors(Enumerable.Repeat(Color.white, sv.Count).ToList()); smesh.SetTriangles(st, 0); smesh.RecalculateNormals(); smesh.RecalculateBounds();
            smesh = GenAsset(smesh, $"{GeneratedDir}/Meshes/Pipe {p.Id} surface from below.asset");
            var sgo = new GameObject("Pipe " + p.Id + " clear stretch - the surface from below"); sgo.transform.SetParent(root, false);
            sgo.AddComponent<MeshFilter>().sharedMesh = smesh; var srr = sgo.AddComponent<MeshRenderer>(); srr.sharedMaterial = clearSurface; srr.shadowCastingMode = ShadowCastingMode.Off;
            // ---- light shafts: crossed quads slanting down from the surface (uv.y 0 at the top), beside and over the tube
            var sun = (new Vector3(0.28f, -1f, 0.18f)).normalized;
            var shv = new List<Vector3>(); var shuv = new List<Vector2>(); var shc = new List<Color>(); var sht = new List<int>(); int nShafts = 0;
            int shafts = Mathf.Max(14, Mathf.RoundToInt(len / 4f));   // Round 24 Part B: as thick along a long stretch
            for (int i = 0; i < shafts; i++)
            {
                float s = p.C0 + 1.5f + (len - 3f) * (i + Hash01(i, 1261) * 0.8f) / shafts, lat = (Hash01(i, 1263) - 0.5f) * 2f * Mathf.Max(0.5f, Mathf.Min(p.Rim(s), 14f) - 2f);
                var a = W(s, lat, surf - 0.05f); float depth = surf - p.Bottom(s), wdt = 0.7f + 1.1f * Hash01(i, 1265);
                if (p.Water != null && (!InPolygon(new Vector2(a.x, a.z), p.Water) || DistToEdges(new Vector2(a.x, a.z), p.Water) < wdt * 1.5f + 1f)) continue;   // Round 25 (BUG-002): inside the water
                var b = a + sun * (depth / -sun.y);
                foreach (var side in new[] { p.N3(s), p.U3(s) })
                {
                    int v0 = shv.Count;
                    shv.Add(a - side * wdt); shv.Add(a + side * wdt); shv.Add(b + side * wdt * 1.4f); shv.Add(b - side * wdt * 1.4f);
                    shuv.Add(new Vector2(0, 0)); shuv.Add(new Vector2(1, 0)); shuv.Add(new Vector2(1, 1)); shuv.Add(new Vector2(0, 1));
                    var col = new Color(Hash01(i, 1267), 0f, 0f, 0.6f + 0.4f * Hash01(i, 1269)); for (int k = 0; k < 4; k++) shc.Add(col);
                    sht.Add(v0); sht.Add(v0 + 1); sht.Add(v0 + 2); sht.Add(v0); sht.Add(v0 + 2); sht.Add(v0 + 3);
                }
                nShafts++;
            }
            var shm = new Mesh { name = "Pipe " + p.Id + " light shafts" }; shm.SetVertices(shv); shm.SetUVs(0, shuv); shm.SetColors(shc); shm.SetTriangles(sht, 0); shm.RecalculateBounds();
            shm = GenAsset(shm, $"{GeneratedDir}/Meshes/Pipe {p.Id} light shafts.asset");
            var shgo = new GameObject("Pipe " + p.Id + " clear stretch - light shafts"); shgo.transform.SetParent(root, false);
            shgo.AddComponent<MeshFilter>().sharedMesh = shm; var shr = shgo.AddComponent<MeshRenderer>(); shr.sharedMaterial = clearShaft; shr.shadowCastingMode = ShadowCastingMode.Off;
            // ---- water haze: two faint layers in the deep water, inset copies of its cross-section, along the stretch
            var hz = new MeshBuilder();
            foreach (float k in new[] { 0.62f, 0.86f })
            {
                Vector3 H(float s, int i)   // the bowl's outline (12 points round) pulled toward the axis by k, kept clear of the glass
                {
                    float th = Mathf.PI * 2f * i / 12f; var d = new Vector2(Mathf.Sin(th), -Mathf.Cos(th)); float lo = R + 0.3f, hi = 30f;
                    for (int it = 0; it < 20; it++) { float mid = (lo + hi) * 0.5f; if (Inside(s, d.x * mid, AY(s) + d.y * mid)) lo = mid; else hi = mid; }
                    float rr = Mathf.Max(R + 0.35f, lo * k);
                    return W(s, d.x * rr, AY(s) + d.y * rr);
                }
                int nj = Mathf.Max(6, Mathf.CeilToInt(len / 4f));   // Round 24 Part B: short pieces (the width changes along a lake)
                for (int j = 0; j < nj; j++)
                {
                    float sa = p.C0 + 0.3f + (len - 0.6f) * j / nj, sb = p.C0 + 0.3f + (len - 0.6f) * (j + 1) / nj;
                    for (int i = 0; i < 12; i++) Face(hz, H(sa, i), H(sb, i), H(sb, i + 1), H(sa, i + 1), W(sa, 0f, AY(sa)) - H(sa, i));
                }
            }
            { var hgo = hz.ToObject("Pipe " + p.Id + " clear stretch - water haze", clearHaze, root, false, false); GameObjectUtility.SetStaticEditorFlags(hgo, 0); }
            // ---- steel ribs round the glass (just outside it: seen through it), thick rings where it meets the concrete
            var ribs = new MeshBuilder(); var steel = new Color(0.30f, 0.33f, 0.35f);
            var ribAt = new List<(float s, float w, float t)> { (p.C0, 0.45f, 0.16f), (p.C1, 0.45f, 0.16f) };
            for (float s = p.C0 + 3f; s < p.C1 - 1.5f; s += 3f) ribAt.Add((s, 0.14f, 0.07f));
            foreach (var (s, w, th) in ribAt)
            {
                const int seg = 40; ribs.Tint = steel;
                for (int i = 0; i < seg; i++)
                {
                    float t0 = Mathf.PI * 2f * i / seg, t1 = Mathf.PI * 2f * (i + 1) / seg;
                    Vector3 Rg(float sx, float th2, float r) => W(sx, Mathf.Sin(th2) * r, AY(sx) - Mathf.Cos(th2) * r);
                    float r0 = R + GlassOut + 0.005f, r1 = r0 + th;
                    var dir0 = (Rg(s, t0, 1f) - W(s, 0f, AY(s))).normalized; var u3 = p.U3(s);
                    Face(ribs, Rg(s - w / 2, t0, r0), Rg(s + w / 2, t0, r0), Rg(s + w / 2, t1, r0), Rg(s - w / 2, t1, r0), -dir0);   // inner face (seen from the tube)
                    Face(ribs, Rg(s - w / 2, t0, r1), Rg(s + w / 2, t0, r1), Rg(s + w / 2, t1, r1), Rg(s - w / 2, t1, r1), dir0);
                    Face(ribs, Rg(s - w / 2, t0, r0), Rg(s - w / 2, t1, r0), Rg(s - w / 2, t1, r1), Rg(s - w / 2, t0, r1), -u3);
                    Face(ribs, Rg(s + w / 2, t0, r0), Rg(s + w / 2, t1, r0), Rg(s + w / 2, t1, r1), Rg(s + w / 2, t0, r1), u3);
                }
            }
            ribs.ToObject("Pipe " + p.Id + " clear stretch - steel ribs", mats.House, root, false, false);
            // ---- the lid: an invisible floor at the pool's bed over the trench (a wading vehicle never drops in)
            for (float s0 = p.C0; s0 < p.C1 - 0.01f; s0 += 4f)   // Round 23 follow-ups: in 4 m pieces (a network pipe's line curves)
            {
                float s1 = Mathf.Min(s0 + 4f, p.C1), sm = (s0 + s1) * 0.5f;
                var lid = new GameObject("Pipe " + p.Id + " clear stretch - lid over the deep water (collider only)"); lid.transform.SetParent(root, false);
                lid.transform.SetPositionAndRotation(W(sm, 0f, bedY - 0.1f), Quaternion.LookRotation(p.U3(sm)));
                var bc = lid.AddComponent<BoxCollider>(); bc.size = new Vector3(2f * p.Rim(sm) + 3f, 0.4f, s1 - s0 + 1.5f);   // Round 23 follow-ups: wider and overlapping (a car launched over the big lake's trench fell past it)
            }
            // ---- the run-time side: fish and the sound change
            var aq = root.gameObject.AddComponent<PipeAquarium>();
            aq.a0 = W(p.C0, 0f, AY(p.C0)); aq.a1 = W(p.C1, 0f, AY(p.C1)); aq.radius = R; aq.surfaceY = surf; aq.bottomY = bot; aq.topHalf = Enumerable.Range(0, 9).Min(k => p.Rim(Mathf.Lerp(p.C0, p.C1, k / 8f))); aq.bottomHalf = R;
            aq.fishMaterials = fishMats;
            // Round 25 (A8): the tube's line every meter, the deep water's half width and bed along it; turtles' resting spots on the bowl's
            // slopes beside the tube, every other one on a sunken log (look only)
            {
                var path = new List<Vector3>(); var rimL = new List<float>(); var botL = new List<float>();
                for (float s = p.C0; s <= p.C1 + 1e-3f; s += 1f) { path.Add(W(s, 0f, AY(s))); rimL.Add(p.Rim(s)); botL.Add(p.Bottom(s)); }
                aq.path = path.ToArray(); aq.rim = rimL.ToArray(); aq.bottom = botL.ToArray();
                var rests = new List<Vector3>(); var yaws = new List<float>(); var logs = new MeshBuilder(); var bark = new Color(0.26f, 0.21f, 0.15f);
                int want = Mathf.Clamp(Mathf.RoundToInt(len / 40f), 2, 8);
                for (int i = 0; i < want * 4 && rests.Count < want; i++)
                {
                    float s = p.C0 + 3f + (len - 6f) * Hash01(i, 1301), side = Hash01(i, 1303) < 0.5f ? -1f : 1f, lat = side * (R + 1.6f + 2.5f * Hash01(i, 1305));
                    if (Mathf.Abs(lat) > p.Rim(s) - 0.8f) continue;
                    float y = BedAt(s, lat); if (y > Gnd(s, lat) - 0.3f || y > top - 1.0f || y < AY(s) - R - 0.2f && Mathf.Abs(lat) < R + 1.8f) continue;
                    float yaw = 360f * Hash01(i, 1307); bool log = (rests.Count & 1) == 1;
                    if (log)   // a log 3 m long lying on the bed, the turtle on it
                    {
                        var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward; var c0 = W(s, lat, y + 0.18f); var a = c0 - dir * 1.5f; var b = c0 + dir * 1.5f;
                        a.y = Mathf.Max(a.y, BedAt(s, lat) + 0.15f); b.y = Mathf.Max(b.y, BedAt(s, lat) + 0.15f);
                        logs.Tint = Under(bark, y); Beam(logs, a, b, 0.36f, 0.36f, Under(bark, y), Vector3.up);
                        rests.Add(c0 + Vector3.up * 0.2f);
                    }
                    else rests.Add(W(s, lat, y + 0.02f));
                    yaws.Add(yaw);
                }
                if (logs.Count > 0) logs.ToObject("Pipe " + p.Id + " clear stretch - sunken logs", mats.House, root, false, false);
                aq.rests = rests.ToArray(); aq.restYaw = yaws.ToArray();
            }
            Debug.Log($"[Round21+] pipe {p.Id}: clear stretch {p.C0:0.0} to {p.C1:0.0} m ({len:0.0} m of glass), its axis at {axisY:0.00} m, the crown {surf - (axisY + R + GlassOut):0.00} m under the surface ({surf:0.00} m); the deep water round it (Round 25: a bowl in the bed) {2f * aq.topHalf:0} m wide (narrowest) to {2f * Enumerable.Range(0, 9).Max(k => p.Rim(Mathf.Lerp(p.C0, p.C1, k / 8f))):0} m, its bed {bot:0.00} m ({surf - bot:0.0} m deep), {nRocks} rocks, {nClumps} weed clumps, {nShafts} light shafts, {ribAt.Count} steel ribs; {aq.rests.Length} turtles' resting spots");
        }

        // the pool's own surface, from above, at the small pond's level (no shadow: the trench under it is lit)
        static void BuildPoolSurface(Materials mats, Transform root, PipePlan p)
        {
            var mb = new MeshBuilder(); int v = mb.Count;
            foreach (var q in p.Pool) mb.Vertex(new Vector3(q.x, p.Surface, q.y));
            foreach (var tri in EarClip(p.Pool))
            {
                var a = mb.Verts[v + tri.x]; var b = mb.Verts[v + tri.z]; var c = mb.Verts[v + tri.y];
                if (Vector3.Cross(b - a, c - a).y > 0f) mb.Tri(v + tri.x, v + tri.z, v + tri.y); else mb.Tri(v + tri.x, v + tri.y, v + tri.z);   // facing up whatever the ring's winding
            }
            mb.ToObject("Pipe " + p.J.id + " pool (water)", mats.Water, root, false, false);
        }
    }
}
