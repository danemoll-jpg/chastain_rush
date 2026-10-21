using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 53 (Dan, 2026-10-20 12:38 and 2026-10-21 01:07; reports BUG-001 to BUG-005 of session 2026-10-20_01-26-12-613_8b4dc1): Addison Heights'
// two forward shortcuts, a house and a bump. World pieces (they stand in Free Roam too), from Data/tracks/addison_heights.json
// (Tools/mapgen/make_addison_route.py); the race's side (the shortcuts, the barriers) is Data/routes/addison_heights.json.
//   A  the Lyndon Drive road crew (BUG-002 / BUG-003): the roller's tilt-deck trailer, its deck tipped down to the road and heavy planks leaned from
//      its deck up against its front rack (a real jump off the planks' top), its flatbed truck and the roller parked on the lawn beside it, the
//      concrete mixer up Lyndon past Embry Lane, cones, the flagger (Neighborhood/Flagger.cs) on the trailer's left, and a slab of fresh concrete
//      across the whole road beyond it (Neighborhood/WetConcrete.cs: slowed as in water, ruts); shrub beds in the yards on both sides (ShrubBeds);
//      the landing zone LY on the firm road past the slab. Lyndon between Joel Drive and Embry Lane is out of the traffic's and the police's road graph.
//   B  the Elaine Circle jump (BUG-005): not built (built and driven in this round, then taken out: with the start / finish on Kelly Drive its way back
//      was as long as the lap it left and slower; TODO.md)
//   C  house 1089 (BUG-004, "fix this house"): its carport stood open over the falling ground (no floor, posts in the air, the inside of the roof
//      a grey slab from Dan's spot): a concrete slab under it down to the ground, a ceiling, a side beam (BuildH)
//   D  the Lyndon Drive / Embry Lane junction (BUG-001, "EVEN THIS SPOT OUT"): Lyndon's level end met Embry's climb in a 0.4 to 0.8 m crease:
//      across Embry the junction follows Embry's own smoothed grade, and Lyndon's mouths twist into it over 12 m (R53EvenJunction)
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class R53Closed { public long osm_id; public float[] from, to; }
        [Serializable] class R53Crew { public float[] foot, dir, concrete, flagger; public float deck, deck_angle_deg, plank, plank_angle_deg, width; public R53Closed closed_road; }
        [Serializable] class R53File { public R53Crew crew; public LakesShrubBed[] shrub_beds; }
        static R53File AddisonExtra;
        const long R53Lyndon = 9148109, R53Embry = 9134327;
        static readonly Vector2 R53Junction = new Vector2(2811.1f, 518.0f);   // Lyndon Drive's node on Embry Lane (Dan's BUG-001 at 2809.71, 512.68)

        static R53File LoadAddisonExtra()
        {
            string path = Path.Combine(ProjectRoot, AddisonTrackFile);
            return File.Exists(path) ? JsonUtility.FromJson<R53File>(File.ReadAllText(path)) : null;
        }

        // planning, after the houses and businesses (their floors are set), before the roads are built: D the junction evened
        // Round 54 (A1): and the Elaine jump's way graded along its line (AddisonNow's grades; taken out in Round 55)
        static void Round53Plan(MapData map, TerrainGrid terrain) { R54LevelLap(terrain); R53EvenJunction(map, terrain); GradeRidge(terrain, AddisonNow); R54SmoothJoins(terrain); R55SmoothSpots(terrain); }   // Round 54 (A5): the lap levelled first, the Lyndon junction evened on it; Round 55 (D): Dan's two bumps

        // ------------------------------------------------------------ D: the Lyndon / Embry junction
        static void R53EvenJunction(MapData map, TerrainGrid terrain)
        {
            var embry = map.roads.FirstOrDefault(r => r.osm_id == R53Embry); var lyndon = map.roads.FirstOrDefault(r => r.osm_id == R53Lyndon);
            if (embry == null || lyndon == null) { Debug.Log("[Round53] D: Embry Lane or Lyndon Drive not found"); return; }
            var E = ToPoints(embry.points); var Ly = ToPoints(lyndon.points);
            // Embry's centerline height every metre from x 2770 to 2860 (its line runs east there), smoothed over 4 m (a Gaussian): its grade, no step
            const float X0 = 2770f, X1 = 2860f; int n = Mathf.RoundToInt(X1 - X0) + 1;
            float EmbryZ(float x) { for (int i = 0; i + 1 < E.Count; i++) if ((E[i].x - x) * (E[i + 1].x - x) <= 0f && Mathf.Abs(E[i + 1].x - E[i].x) > 1e-3f) return Mathf.Lerp(E[i].y, E[i + 1].y, (x - E[i].x) / (E[i + 1].x - E[i].x)); return R53Junction.y; }
            var raw = new float[n]; for (int i = 0; i < n; i++) { float x = X0 + i; raw[i] = terrain.Height(x, EmbryZ(x)); }
            var ey = new float[n];
            for (int i = 0; i < n; i++) { float s = 0f, w = 0f; for (int j = 0; j < n; j++) { float k = Mathf.Exp(-0.5f * (i - j) * (i - j) / 16f); s += raw[j] * k; w += k; } ey[i] = s / w; }
            float Ey(float x) { float f = Mathf.Clamp(x - X0, 0f, n - 1.001f); int i = (int)f; return Mathf.Lerp(ey[i], ey[i + 1], f - i); }
            // Lyndon's centerline height along z, as it is (12 to 22 m out from Embry, where the mouths end)
            float LyndonX(float z) { for (int i = 0; i + 1 < Ly.Count; i++) if ((Ly[i].y - z) * (Ly[i + 1].y - z) <= 0f && Mathf.Abs(Ly[i + 1].y - Ly[i].y) > 1e-3f) return Mathf.Lerp(Ly[i].x, Ly[i + 1].x, (z - Ly[i].y) / (Ly[i + 1].y - Ly[i].y)); return R53Junction.x; }
            var lyRaw = new Dictionary<int, float>(); for (int z = 480; z <= 560; z++) lyRaw[z] = terrain.Height(LyndonX(z), z);
            float LyY(float z) { int a = Mathf.Clamp(Mathf.FloorToInt(z), 480, 559); return Mathf.Lerp(lyRaw[a], lyRaw[a + 1], Mathf.Clamp01(z - a)); }
            const float Band = 5.0f, Mouth = 12f, LHalf = 4.0f, LFade = 3.0f, XIn = 11f, XFade = 7f;
            float most = 0f; int moved = 0;
            EditTerrain(terrain, Rect.MinMaxRect(R53Junction.x - XIn - XFade, R53Junction.y - Band - Mouth - 1f, R53Junction.x + XIn + XFade, R53Junction.y + Band + Mouth + 1f), (x, z, y) =>
            {
                float e = z - EmbryZ(x), ae = Mathf.Abs(e), l = Mathf.Abs(x - LyndonX(z));
                float target, w;
                if (ae <= Band)
                {
                    // across Embry (both lanes and its edges): its smoothed grade, level across; full over Lyndon's mouth, easing out along Embry
                    target = Ey(x); w = 1f - Smooth01((Mathf.Abs(x - R53Junction.x) - XIn) / XFade);
                }
                else
                {
                    // Lyndon's mouths: from Embry's surface (a cross-slope over Lyndon's width) twisting back to Lyndon's own level-across road over 12 m
                    float m = ae - Band; if (m >= Mouth) return y;
                    float t = Smooth01(m / Mouth);
                    target = Mathf.Lerp(Ey(x), LyY(z), t);
                    w = 1f - Smooth01((l - LHalf) / LFade);
                }
                if (w <= 1e-3f) return y;
                float ny = y + w * (target - y); most = Mathf.Max(most, Mathf.Abs(ny - y)); moved++; return ny;
            });
            Debug.Log($"[Round53] D: the Lyndon Drive / Embry Lane junction evened: Embry's grade across it (smoothed over 4 m), Lyndon's mouths twisting into it over {Mouth:0} m; {moved} samples moved, by up to {most:0.00} m");
        }

        // ------------------------------------------------------------ the traffic's and the police's road graph: Lyndon between Joel and Embry closed
        static List<List<Vector2>> R53Pieces(long osm, List<Vector2> pts)
        {
            var cr = AddisonExtra?.crew?.closed_road;
            if (cr == null || cr.osm_id != osm || cr.from == null || cr.to == null) return new List<List<Vector2>> { pts };
            var a = new Vector2(cr.from[0], cr.from[1]); var b = new Vector2(cr.to[0], cr.to[1]);
            for (int i = 0; i + 1 < pts.Count; i++)
                if (((pts[i] - a).magnitude < 0.5f && (pts[i + 1] - b).magnitude < 0.5f) || ((pts[i] - b).magnitude < 0.5f && (pts[i + 1] - a).magnitude < 0.5f))
                    return new List<List<Vector2>> { pts.GetRange(0, i + 1), pts.GetRange(i + 1, pts.Count - i - 1) }.Where(p => p.Count >= 2).ToList();
            return new List<List<Vector2>> { pts };
        }

        // ------------------------------------------------------------ C: house 1089's carport
        static readonly HashSet<int> R53Carports = new HashSet<int> { 1089, 615 };   // Round 54 (A4, BUG-001 "finish this house"): 615 at Embry Lane's east corner, the same open carport
        static void R53Carport(MeshBuilder mb, Frame f, HouseSpec h, float s, float hE, float yb)
        {
            float W = h.W, D = h.D, v0 = -D / 2 + 2.2f;
            var slab = new Color(0.58f, 0.58f, 0.56f);
            BoxF(mb, f, -W / 2, s, yb - 0.3f, -0.06f, v0, D / 2, slab);               // the carport's floor, the drive's concrete carried in, down to the ground
            mb.Tint = Shade(h.trimC, 0.92f);                                            // its ceiling under the roof (the roof's inside no longer shows)
            Face(mb, f.P(-W / 2, hE - 0.26f, -D / 2), f.P(s, hE - 0.26f, -D / 2), f.P(s, hE - 0.26f, D / 2), f.P(-W / 2, hE - 0.26f, D / 2), Vector3.down);
            BoxF(mb, f, -W / 2, -W / 2 + 0.25f, hE - 0.25f, hE, -D / 2, D / 2, h.trimC);   // a beam along its open side
            Debug.Log($"[Round53] C: house {h.index}'s carport: a floor {-yb:0.00} m deep to the ground, a ceiling, a side beam");
        }

        // ------------------------------------------------------------ A and B: the world pieces (the build step "Addison")
        static void BuildAddison(MapData map, TerrainGrid terrain, Materials mats, Transform world)
        {
            if (AddisonNow == null || AddisonExtra == null) { Debug.Log("[Round53] no " + AddisonTrackFile + ": no Addison Heights pieces"); return; }
            var root = new GameObject("Addison Heights").transform; root.SetParent(world, false);
            if (AddisonNow.Data.landing_zones != null)
                foreach (var z in AddisonNow.Data.landing_zones)
                {
                    var go = new GameObject("Landing zone " + z.id); go.transform.SetParent(root, false);
                    go.AddComponent<LandingZone>().outline = ToPoints(z.poly).ToArray();
                }
            var parked = BuildRoadCrew(terrain, mats, root, AddisonExtra.crew);
            BuildShrubBeds(terrain, mats, root, AddisonExtra.shrub_beds, map, parked.ToArray());   // no shrub under the parked truck and roller
            BuildAddison54(terrain, mats, root);   // Round 54: the terrace, the bounce house, the curve signs (MapBuilder.Round54.cs)
            Debug.Log($"[Round53] Addison Heights: the road crew on Lyndon Drive, {AddisonExtra.shrub_beds?.Length ?? 0} shrub beds");
        }

        static readonly Color CrewOrange = new Color(0.96f, 0.45f, 0.06f), CrewYellow = new Color(0.95f, 0.72f, 0.08f), CrewWhite = new Color(0.90f, 0.90f, 0.88f), Tire = new Color(0.10f, 0.10f, 0.11f);

        // returns where the parked vehicles stand
        static List<Vector2> BuildRoadCrew(TerrainGrid terrain, Materials mats, Transform root, R53Crew c)
        {
            var parked = new List<Vector2>();
            if (c == null || c.foot == null) return parked;
            var go = new GameObject("Lyndon Drive road crew").transform; go.SetParent(root, false);
            var d = new Vector2(c.dir[0], c.dir[1]).normalized; var nl = new Vector2(-d.y, d.x); var foot = new Vector2(c.foot[0], c.foot[1]);
            float hw = c.width * 0.5f, len = c.deck + c.plank, fy = Ground(terrain, foot.x, foot.y);
            float kd = Mathf.Tan(c.deck_angle_deg * Mathf.Deg2Rad), kp = Mathf.Tan(c.plank_angle_deg * Mathf.Deg2Rad);
            float Rise(float s) { s = Mathf.Clamp(s, 0f, len); return s <= c.deck ? kd * s : kd * c.deck + kp * (s - c.deck); }
            Vector3 W(float s, float lat, float up) { var q = foot + d * s + nl * lat; return new Vector3(q.x, fy + Rise(s) + up, q.y); }
            Vector3 G(Vector2 q, float up) => new Vector3(q.x, Ground(terrain, q.x, q.y) + up, q.y);

            // the launch: the trailer's tipped deck and the planks on it, one solid prism (its top the collider; no face at its foot: it meets the road)
            var prof = new List<float>(); for (float s = 0f; s <= len + 1e-3f; s += 0.25f) { prof.Add(s); prof.Add(fy + Rise(s)); }
            var frame = new Color(0.20f, 0.21f, 0.23f);
            var pr = new ScPrism
            {
                name = "sc LY trailer", kind = "ramp", no_face = new[] { 0 }, origin = new[] { foot.x, foot.y }, axis = new[] { d.x, d.y }, profile = prof.ToArray(),
                colour = new[] { frame.r, frame.g, frame.b },
                poly = new[] { foot + nl * hw, foot - nl * hw, foot + d * len - nl * hw, foot + d * len + nl * hw }.SelectMany(q => new[] { q.x, q.y }).ToArray()
            };
            BuildPrism(pr, terrain, mats, go);
            var look = new MeshBuilder();
            // the deck: hardwood boards along it, the steel side rails, the beavertail's yellow and black edge at its tail
            var oak = new Color(0.47f, 0.33f, 0.20f);
            for (float lat = -hw + 0.15f; lat < hw - 0.05f; lat += 0.3f)
                for (float s = 0.3f; s < c.deck - 1e-3f; s += 0.5f) { look.Tint = Shade(oak, 0.86f + 0.2f * Hash01(Mathf.RoundToInt(lat * 10f) * 31 + Mathf.RoundToInt(s * 2f), 5301)); Face(look, W(s, lat - 0.14f, 0.012f), W(s, lat + 0.14f, 0.012f), W(Mathf.Min(c.deck, s + 0.5f), lat + 0.14f, 0.012f), W(Mathf.Min(c.deck, s + 0.5f), lat - 0.14f, 0.012f), Vector3.up); }
            for (float s = 0f; s < 0.3f; s += 0.3f)
                for (float lat = -hw; lat < hw - 0.01f; lat += 0.325f) { look.Tint = Mathf.RoundToInt((lat + hw) / 0.325f) % 2 == 0 ? CrewYellow : Tire; Face(look, W(0.02f, lat, 0.012f), W(0.02f, lat + 0.325f, 0.012f), W(0.3f, lat + 0.325f, 0.012f), W(0.3f, lat, 0.012f), Vector3.up); }
            foreach (float side in new[] { -1f, 1f })
                for (float s = 0f; s < len - 1e-3f; s += 0.5f) Beam(look, W(s, side * (hw - 0.05f), 0.04f), W(Mathf.Min(len, s + 0.5f), side * (hw - 0.05f), 0.04f), 0.1f, 0.08f, Shade(frame, 1.3f));
            // the planks: eight 2x12s, side by side, leaned from the deck up onto the front rack (their own grain and gaps), the rack's posts and top bar
            var plank = new Color(0.72f, 0.58f, 0.38f);
            for (int k = 0; k < 8; k++)
            {
                float lat = -hw + 0.08f + k * (c.width - 0.16f) / 8f, wdt = (c.width - 0.16f) / 8f - 0.03f;
                look.Tint = Shade(plank, 0.88f + 0.2f * Hash01(k, 5303));
                Face(look, W(c.deck - 0.15f, lat, 0.03f), W(c.deck - 0.15f, lat + wdt, 0.03f), W(len, lat + wdt, 0.03f), W(len, lat, 0.03f), Vector3.up);
            }
            foreach (float side in new[] { -1f, 1f }) Beam(look, W(len - 0.05f, side * (hw - 0.06f), -Rise(len) + kd * len), W(len - 0.05f, side * (hw - 0.06f), 0.02f), 0.09f, 0.09f, CrewOrange);
            Beam(look, W(len - 0.05f, -hw, -0.05f), W(len - 0.05f, hw, -0.05f), 0.1f, 0.1f, CrewOrange);
            // the wheels (tandem, under its middle) and fenders; its tongue to the jack stand in front
            foreach (float side in new[] { -1f, 1f })
                foreach (float s in new[] { 4.1f, 5.1f })
                {
                    var hub = foot + d * s + nl * side * (hw + 0.14f); float g = Ground(terrain, hub.x, hub.y);
                    LookCyl(look, new Vector3(hub.x, g + 0.37f, hub.y) - new Vector3(nl.x, 0f, nl.y) * side * 0.12f, new Vector3(hub.x, g + 0.37f, hub.y) + new Vector3(nl.x, 0f, nl.y) * side * 0.12f, 0.37f, Tire, 12);
                }
            foreach (float side in new[] { -1f, 1f }) Beam(look, W(3.6f, side * (hw + 0.15f), -0.05f), W(5.6f, side * (hw + 0.15f), -0.05f), 0.32f, 0.04f, Shade(frame, 1.2f));
            var tongue0 = foot + d * (len - 0.4f); var tongue1 = foot + d * (len + 1.8f); float ty = Ground(terrain, tongue1.x, tongue1.y);
            Beam(look, new Vector3(tongue0.x, fy + Rise(len) - 0.9f, tongue0.y), new Vector3(tongue1.x, ty + 0.55f, tongue1.y), 0.12f, 0.12f, frame);
            Beam(look, new Vector3(tongue1.x, ty, tongue1.y), new Vector3(tongue1.x, ty + 0.6f, tongue1.y), 0.08f, 0.08f, Shade(frame, 1.4f));
            var lg = look.ToObject("LY trailer look", mats.House, go, false); lg.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;

            // the truck that hauls it (a flatbed, its crew's orange stripe) and the roller, parked on the lawn east of the road before the trailer;
            // solid (a box each), as any parked vehicle
            var right = -nl; var lawn = foot + right * 6.0f;
            Truck(terrain, mats, go, lawn - d * 9.5f, d);
            Roller(terrain, mats, go, lawn - d * 1.6f, d);
            parked.AddRange(new[] { lawn - d * 11.5f, lawn - d * 6.5f, lawn - d * 1.6f });
            // the concrete mixer, its drum turning slowly look only, on the lawn west of Lyndon past Embry Lane, its chute toward the slab
            Mixer(terrain, mats, go, foot + d * 86f + nl * 7.2f, d);   // its back (the drum's mouth and chute) toward the slab

            // the fresh slab across the whole road: draped on the road 0.12 m over its ground (the wheels sink in), the forms along its edges, float marks
            var o = ToPoints(c.concrete);
            var slab = new MeshBuilder(); var wet = new Color(0.47f, 0.48f, 0.50f);
            Vector2 A = o[0], B = o[1], Cc = o[2], Dd = o[3];   // A, B: its west edge south to north; D, C: its east edge
            int nu = Mathf.CeilToInt((B - A).magnitude / 0.5f), nv = Mathf.CeilToInt((Dd - A).magnitude / 0.5f);
            for (int i = 0; i < nu; i++)
                for (int j = 0; j < nv; j++)
                {
                    Vector2 P(int a, int b) => Vector2.Lerp(Vector2.Lerp(A, B, a / (float)nu), Vector2.Lerp(Dd, Cc, a / (float)nu), b / (float)nv);
                    slab.Tint = Shade(wet, 0.93f + 0.1f * Mathf.PerlinNoise(i * 0.21f, j * 0.37f) + (i % 7 == 3 ? 0.06f : 0f));   // float marks across it
                    Face(slab, G(P(i, j), 0.12f), G(P(i + 1, j), 0.12f), G(P(i + 1, j + 1), 0.12f), G(P(i, j + 1), 0.12f), Vector3.up);
                }
            var sg = slab.ToObject("LY fresh concrete", mats.Paint, go, false, false);   // the road paint's material: drawn over the asphalt (its depth offset)
            var forms = new MeshBuilder(); var form = new Color(0.62f, 0.48f, 0.30f);
            foreach (var (a, b) in new[] { (A, B), (Dd, Cc), (A, Dd), (B, Cc) })
            {
                var seg = Densify(new List<Vector2> { a, b }, 1.0f);
                for (int k = 0; k + 1 < seg.Count; k++) Beam(forms, G(seg[k], 0.15f), G(seg[k + 1], 0.15f), 0.05f, 0.2f, form);
            }
            forms.ToObject("LY concrete forms", mats.House, go, false);
            var wc = sg.AddComponent<WetConcrete>(); wc.outline = o.ToArray(); wc.rutMaterial = mats.Paint;

            // cones: a taper from each curb to the trailer's sides (the work zone's lane into the launch), and down both edges of the slab; they break
            // and fly (FenceChunk: a trigger, a little speed lost, never solid; whole again at race start)
            var br = new Breakables(); int ncone = 0;
            void Cone(Vector2 q)
            {
                var mbc = br.Begin(q); float g = Ground(terrain, q.x, q.y) + 0.09f;
                br.Piece(() =>
                {
                    Cyl(mbc, new Vector3(q.x, g, q.y), 0.04f, 0.2f, 0.2f, Tire, 6);
                    Cyl(mbc, new Vector3(q.x, g + 0.04f, q.y), 0.66f, 0.15f, 0.03f, CrewOrange, 8);
                    Cyl(mbc, new Vector3(q.x, g + 0.32f, q.y), 0.14f, 0.095f, 0.077f, CrewWhite, 8);
                }, g);
                br.End(new Vector3(q.x, g + 0.35f, q.y), new Vector3(d.x, 0f, d.y), 0.4f, 0.7f, 0.4f); ncone++;
            }
            for (int k = 0; k <= 4; k++)
            {
                float t = k / 4f, s = -12f + 10.5f * t;   // the trailer stands 0.9 m east of the road's middle: the west curb is 4.9 m left of it, the east 3.1 m right
                Cone(foot + d * s + nl * Mathf.Lerp(4.5f, hw + 0.35f, t));
                Cone(foot + d * s - nl * Mathf.Lerp(2.8f, hw + 0.35f, t));
            }
            for (float t = 0f; t <= 1.001f; t += 1f / 8f) { Cone(Vector2.Lerp(A, B, t) - nl * 0.3f); Cone(Vector2.Lerp(Dd, Cc, t) + nl * 0.3f); }
            br.ToObjects("LY cones", mats.House, go, 1f, BreakSounds.Kind.Wood);

            // the flagger, before the ramp on its left, by the west curb
            var fl = new GameObject("Flagger"); fl.transform.SetParent(go, false);
            var fp = new Vector2(c.flagger[0], c.flagger[1]); fl.transform.position = G(fp, 0f);
            var f = fl.AddComponent<Flagger>(); f.facing = new Vector3(-d.x, 0f, -d.y); f.stepAway = new Vector3(nl.x, 0f, nl.y);
            f.stopFace = PaddleMaterial("StopPaddle"); f.slowFace = PaddleMaterial("SlowPaddle");
            Debug.Log($"[Round53] A: the trailer at ({foot.x:0.0}, {foot.y:0.0}) {fy:0.00} m, its lip {W(len, 0f, 0f).y:0.00} m ({Rise(len):0.00} m up, {c.plank_angle_deg:0} degrees), {c.width:0.0} m wide; " +
                      $"the slab {(B - A).magnitude:0} m x {(Dd - A).magnitude:0.0} m; {ncone} cones; the flagger at ({fp.x:0.0}, {fp.y:0.0})");
            return parked;
        }

        static Material PaddleMaterial(string name)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HandMade/RoadCrew/" + name + ".png");
            if (tex == null) { Debug.LogWarning("[Round53] Assets/HandMade/RoadCrew/" + name + ".png missing (Tools/handmade/make_stop_paddle.py)"); return null; }
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.3f); m.SetFloat("_Cull", (float)CullMode.Back);
            return GenAsset(m, GeneratedDir + "/" + name + ".mat");
        }

        // a box between local u (across), v (along), y (up) about a ground point, turned to 'along'
        static void CrewBox(MeshBuilder mb, Vector3 o, Vector2 along, float u0, float u1, float v0, float v1, float y0, float y1, Color col)
        {
            var f = MakeFrame(o, along, 1f); BoxF(mb, f, u0, u1, y0, y1, v0, v1, col);
        }
        // its look, and a box collider (x across, y up, z along) round 'centre' (u, y, v in its frame)
        static void CrewSolid(Materials mats, Transform parent, string name, MeshBuilder mb, Vector3 o, Vector2 along, Vector3 size, Vector3 centre)
        {
            var g = mb.ToObject(name, mats.House, parent, false); g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            var f = MakeFrame(o, along, 1f);
            var col = new GameObject(name + " collider"); col.transform.SetParent(parent, false);
            col.transform.SetPositionAndRotation(f.P(centre.x, centre.y, centre.z), Quaternion.LookRotation(new Vector3(along.x, 0f, along.y)));
            col.AddComponent<BoxCollider>().size = size;
        }
        static void Wheel(MeshBuilder mb, Vector3 o, Vector2 along, float u, float v, float r, float w)
        {
            var f = MakeFrame(o, along, 1f); var c = f.P(u, r, v); var ax = f.R * (w * 0.5f);
            LookCyl(mb, c - ax, c + ax, r, Tire, 12);
        }

        static void Truck(TerrainGrid terrain, Materials mats, Transform parent, Vector2 at, Vector2 along)
        {
            var o = new Vector3(at.x, Ground(terrain, at.x, at.y), at.y); var mb = new MeshBuilder();
            CrewBox(mb, o, along, -1.2f, 1.2f, -3.6f, 1.6f, 1.0f, 1.18f, Shade(CrewWhite, 0.8f));            // the flatbed
            CrewBox(mb, o, along, -1.2f, 1.2f, 1.6f, 2.0f, 1.0f, 2.1f, Shade(CrewWhite, 0.7f));              // the headache rack
            CrewBox(mb, o, along, -1.15f, 1.15f, 2.0f, 4.3f, 0.75f, 2.65f, CrewWhite);                       // the cab
            CrewBox(mb, o, along, -1.16f, 1.16f, 2.0f, 4.3f, 1.35f, 1.55f, CrewOrange);                      // its crew stripe
            CrewBox(mb, o, along, -1.05f, 1.05f, 3.2f, 4.25f, 1.75f, 2.5f, new Color(0.15f, 0.2f, 0.25f));   // the windshield and side glass
            CrewBox(mb, o, along, -1.1f, 1.1f, 4.3f, 5.6f, 0.75f, 1.75f, CrewWhite);                         // the hood
            CrewBox(mb, o, along, -1.0f, 1.0f, -3.6f, 5.6f, 0.45f, 0.8f, Shade(Tire, 2f));                    // the frame
            CrewBox(mb, o, along, -0.9f, 0.9f, 2.4f, 2.6f, 2.66f, 2.78f, CrewYellow);                         // the light bar
            foreach (float u in new[] { -1.05f, 1.05f }) foreach (float v in new[] { -2.4f, -1.2f, 4.6f }) Wheel(mb, o, along, u, v, 0.48f, 0.32f);
            CrewSolid(mats, parent, "LY crew truck", mb, o, along, new Vector3(2.4f, 2.4f, 9.2f), new Vector3(0f, 1.45f, 1.0f));
        }

        static void Roller(TerrainGrid terrain, Materials mats, Transform parent, Vector2 at, Vector2 along)
        {
            var o = new Vector3(at.x, Ground(terrain, at.x, at.y), at.y); var mb = new MeshBuilder(); var f = MakeFrame(o, along, 1f);
            foreach (float v in new[] { -1.15f, 1.15f }) { var c = f.P(0f, 0.5f, v); LookCyl(mb, c - f.R * 0.62f, c + f.R * 0.62f, 0.5f, Shade(CrewWhite, 0.62f), 16); }   // the two steel drums
            CrewBox(mb, o, along, -0.7f, 0.7f, -1.5f, 1.5f, 0.75f, 1.45f, CrewYellow);                       // the body over them
            CrewBox(mb, o, along, -0.45f, 0.45f, -0.3f, 0.4f, 1.45f, 1.85f, Shade(Tire, 2.5f));              // the seat and its console
            foreach (float u in new[] { -0.65f, 0.65f }) foreach (float v in new[] { -0.55f, 0.55f }) CrewBox(mb, o, along, u - 0.05f, u + 0.05f, v - 0.05f, v + 0.05f, 1.45f, 2.75f, Tire);   // the ROPS
            CrewBox(mb, o, along, -0.75f, 0.75f, -0.65f, 0.65f, 2.75f, 2.85f, CrewYellow);                    // its canopy
            CrewSolid(mats, parent, "LY roller", mb, o, along, new Vector3(1.5f, 2.8f, 3.3f), new Vector3(0f, 1.4f, 0f));
        }

        static void Mixer(TerrainGrid terrain, Materials mats, Transform parent, Vector2 at, Vector2 along)
        {
            var o = new Vector3(at.x, Ground(terrain, at.x, at.y), at.y); var mb = new MeshBuilder(); var f = MakeFrame(o, along, 1f);
            CrewBox(mb, o, along, -1.0f, 1.0f, -4.2f, 4.6f, 0.5f, 1.0f, Shade(Tire, 2f));                    // the frame
            CrewBox(mb, o, along, -1.2f, 1.2f, 2.6f, 4.8f, 0.8f, 2.9f, CrewWhite);                           // the cab
            CrewBox(mb, o, along, -1.1f, 1.1f, 3.6f, 4.75f, 1.9f, 2.7f, new Color(0.15f, 0.2f, 0.25f));
            // the drum, tilted up toward the back, its spiral stripes
            var c0 = f.P(0f, 1.5f, 2.2f); var c1 = f.P(0f, 2.6f, -3.6f);
            for (int k = 0; k < 6; k++)
            {
                float t0 = k / 6f, t1 = (k + 1) / 6f, r0 = Mathf.Lerp(0.9f, 1.25f, Mathf.Sin(t0 * Mathf.PI)), r1 = Mathf.Lerp(0.9f, 1.25f, Mathf.Sin(t1 * Mathf.PI));
                LookCyl(mb, Vector3.Lerp(c0, c1, t0), Vector3.Lerp(c0, c1, t1), (r0 + r1) / 2f, k % 2 == 0 ? CrewWhite : CrewOrange, 14);
            }
            Beam(mb, f.P(0f, 1.6f, -4.2f), f.P(0f, 0.9f, -6.0f), 0.45f, 0.15f, Shade(CrewWhite, 0.6f));   // the chute
            foreach (float u in new[] { -1.1f, 1.1f }) foreach (float v in new[] { -3.3f, -2.1f, 3.6f }) Wheel(mb, o, along, u, v, 0.52f, 0.34f);
            CrewSolid(mats, parent, "LY concrete mixer", mb, o, along, new Vector3(2.5f, 3.6f, 9.0f), new Vector3(0f, 1.9f, 0.3f));
        }
    }
}
