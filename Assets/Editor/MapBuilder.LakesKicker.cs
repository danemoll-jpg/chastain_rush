using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 49 (B, Dan 2026-10-19 08:37: the reverse Chastain Lakes race, "around CP 4, you go between houses and find a ramp that jumps past 5. It
// should be a fairly large jump, I would prefer something other than a dirt ramp"): the neighborhood kids' big wooden BMX kicker in the back yard
// between houses 239 and 237, world (it stands in Free Roam too), from the 'kicker' entry of Data/tracks/chastain_lakes.json
// (Tools/mapgen/make_lakes_route.py). Why a kids' kicker: it is something that is really in back yards and really this shape (a curved plywood
// ramp on a 2x6 frame); built big by teenagers on a slope it stands 3 m over its foot and 4 m over the falling ground at its lip.
//   the kicker  4.6 m wide, 9.6 m long, curving up to 32 degrees at its lip: solid (its plywood top and sides are the collider), the frame,
//               braces, painted stripes and the kids' graffiti look only. A real jump off its lip (physics alone: no launch control, the
//               standing rule of 2026-10-17), west over the creek hollow below East Chastain Lake's south tip; the landing (zone RK) past it
//   the hint    worn tracks across the yards, hidden in a forward race (the forward race's barrier across the gap is the route's). Round 50 (C):
//               no JUMP! board (Dan: "the tire tracks are enough to know it is a shortcut")
// Round 50 (A, Dan 2026-10-19 13:37: "you could just drive through this area and skip the ramp and nothing will slow you down"):
//   the carport     the second jump, at the end of Northlake Point: a tall two-car metal carport with closed sides, its east legs folded under by
//                   a storm, its roof sheet down on the ground at its east edge and bent up into the part its two west legs still hold (the
//                   'carport' entry). Solid (roof and side panels are the collider); a real jump off its west eave; the landing (zone RC) past
//                   the shrub bed between 231 and 230
//   the shrub beds  across every free way through these yards (the 'shrub_beds' entry): brush under the trees in the valley, cattails where a
//                   bed runs into the lake, new shrub beds (boxwood, holly, azaleas) in the yards. Look only: ShrubBeds (Racer's
//                   ShortcutUndergrowth) slows a vehicle driving through them
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class LakesKickerData
        {
            public float[] lip, dir, foot, tracks;
            public float foot_y, lip_y, len, width, angle_deg;
            public string clues_hidden_forward_of;
        }
        [Serializable] class LakesCarportData { public float[] foot, dir; public float len, bend, angle_deg, width; }
        [Serializable] class LakesShrubBed { public string id; public float[] points; public float half; }

        static readonly Color Plywood = new Color(0.74f, 0.60f, 0.42f), PlywoodDark = new Color(0.55f, 0.43f, 0.29f), Lumber = new Color(0.66f, 0.55f, 0.40f);

        static void BuildLakeKicker(TerrainGrid terrain, Materials mats, Transform root, LakesKickerData k)
        {
            if (k == null || k.lip == null || k.lip.Length < 2) return;
            var go = new GameObject("Kicker jump").transform; go.SetParent(root, false);
            var d = new Vector2(k.dir[0], k.dir[1]).normalized; var nl = new Vector2(-d.y, d.x); var foot = new Vector2(k.foot[0], k.foot[1]); var lip = new Vector2(k.lip[0], k.lip[1]);
            float tk = Mathf.Tan(k.angle_deg * Mathf.Deg2Rad), hw = k.width * 0.5f;
            float Y(float s) => k.foot_y + tk * s * s / (2f * k.len);
            // the surface: a curve from the ground at its foot to its lip, every 0.25 m (no face at the foot: it meets the ground)
            var prof = new List<float>(); for (float s = 0f; s <= k.len + 1e-3f; s += 0.25f) { prof.Add(s); prof.Add(Y(Mathf.Min(s, k.len))); }
            var ramp = new ScPrism
            {
                name = "sc RK kicker", kind = "ramp", no_face = new[] { 0 }, origin = new[] { foot.x, foot.y }, axis = new[] { d.x, d.y }, profile = prof.ToArray(),
                colour = new[] { Plywood.r, Plywood.g, Plywood.b },
                poly = new[] { foot + nl * hw, foot - nl * hw, lip - nl * hw, lip + nl * hw }.SelectMany(q => new[] { q.x, q.y }).ToArray()
            };
            BuildPrism(ramp, terrain, mats, go);

            // the look: plywood sheet seams across the top, a painted lip stripe, the 2x6 frame showing at the sides (studs every 0.6 m, a diagonal
            // brace), the coping pipe along the lip, the kids' spray-painted arrows
            var look = new MeshBuilder();
            Vector3 W(float s, float lat, float up) { var q = foot + d * s + nl * lat; return new Vector3(q.x, Y(Mathf.Clamp(s, 0f, k.len)) + up, q.y); }
            for (float s = 1.22f; s < k.len; s += 1.22f) Beam(look, W(s, -hw + 0.05f, 0.012f), W(s, hw - 0.05f, 0.012f), 0.02f, 0.006f, PlywoodDark);   // sheet seams
            look.Tint = new Color(0.95f, 0.78f, 0.12f);   // the lip's yellow stripe
            Face(look, W(k.len - 0.45f, -hw + 0.02f, 0.014f), W(k.len - 0.45f, hw - 0.02f, 0.014f), W(k.len - 0.05f, hw - 0.02f, 0.014f), W(k.len - 0.05f, -hw + 0.02f, 0.014f), Vector3.up);
            LookCyl(look, W(k.len - 0.02f, -hw, 0.03f), W(k.len - 0.02f, hw, 0.03f), 0.045f, new Color(0.62f, 0.64f, 0.66f), 8);   // coping
            foreach (float side in new[] { -1f, 1f })
            {
                float lat = side * (hw + 0.04f);
                for (float s = 0.6f; s <= k.len; s += 0.6f)
                {
                    var top = W(s, lat, -0.03f); float g = Ground(terrain, top.x, top.z);
                    if (top.y - g > 0.25f) Beam(look, new Vector3(top.x, g - 0.1f, top.z), top, 0.045f, 0.07f, Lumber);   // studs
                }
                var b0 = W(k.len * 0.35f, lat + side * 0.02f, -0.1f); var b1 = W(k.len, lat + side * 0.02f, -0.1f); float g1 = Ground(terrain, b1.x, b1.z);
                Beam(look, new Vector3(b0.x, Ground(terrain, b0.x, b0.z) + 0.1f, b0.z), new Vector3(b1.x, b1.y - 0.2f, b1.z), 0.045f, 0.07f, Lumber);   // a diagonal brace
                Beam(look, new Vector3(b1.x, g1 + 0.1f, b1.z), new Vector3(b0.x, b0.y - 0.1f, b0.z), 0.045f, 0.07f, Lumber);
            }
            look.Tint = new Color(0.12f, 0.55f, 0.85f);   // two spray-painted chevrons up the face
            foreach (float s in new[] { 3.0f, 5.5f })
                foreach (float side in new[] { -1f, 1f })
                    Face(look, W(s, 0f, 0.016f), W(s - 1.0f, side * 1.3f, 0.016f), W(s - 1.25f, side * 1.3f, 0.016f), W(s - 0.25f, 0f, 0.016f), Vector3.up);
            var lg = look.ToObject("RK kicker look", mats.House, go, false); lg.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;

            // the hint: the worn tracks (hidden in a forward race)
            var clues = go;
            if (!string.IsNullOrEmpty(k.clues_hidden_forward_of))
            {
                var h = new GameObject("RK clues (hidden in a forward race)"); h.transform.SetParent(go, false);
                h.AddComponent<RaceOnly>().hideInForwardOf = k.clues_hidden_forward_of; clues = h.transform;
            }
            if (k.tracks != null && k.tracks.Length >= 6) WornRuts(terrain, mats, clues, ToPoints(k.tracks), "RK worn tracks");
            Debug.Log($"[Round49] the kicker: foot ({foot.x:0.0}, {foot.y:0.0}) {k.foot_y:0.00} m, lip ({lip.x:0.0}, {lip.y:0.0}) {Y(k.len):0.00} m ({Y(k.len) - Ground(terrain, lip.x, lip.y):0.0} m over the ground there), " +
                      $"{k.len:0.0} m long, {k.width:0.0} m wide, {k.angle_deg:0} degrees at the lip");
        }

        // Round 50 (A): the collapsed carport. Its roof: flat on the ground at its east edge, bent up over 'bend' m (the folded side) to the straight
        // part at angle_deg up to the west eave; its heights from the ground built here (the foot's ground). The prism is the roof and the closed
        // side and end panels down to the ground (all solid, one collider); the look: roof ribs, trim, the west legs and the folded east legs
        static void BuildLakeCarport(TerrainGrid terrain, Materials mats, Transform root, LakesCarportData c)
        {
            if (c == null || c.foot == null || c.foot.Length < 2) return;
            var go = new GameObject("Carport jump").transform; go.SetParent(root, false);
            var d = new Vector2(c.dir[0], c.dir[1]).normalized; var nl = new Vector2(-d.y, d.x); var foot = new Vector2(c.foot[0], c.foot[1]); var lip = foot + d * c.len;
            float kc = Mathf.Tan(c.angle_deg * Mathf.Deg2Rad), hw = c.width * 0.5f, fy = Ground(terrain, foot.x, foot.y);
            float Rise(float s) { s = Mathf.Clamp(s, 0f, c.len); return s <= c.bend ? kc * s * s / (2f * c.bend) : kc * c.bend / 2f + kc * (s - c.bend); }
            var prof = new List<float>(); for (float s = 0f; s <= c.len + 1e-3f; s += 0.25f) { prof.Add(s); prof.Add(fy + Rise(s)); }
            var roof = new Color(0.58f, 0.24f, 0.20f);   // a barn-red painted steel roof and sides
            var pr = new ScPrism
            {
                name = "sc RC carport", kind = "ramp", no_face = new[] { 0 }, origin = new[] { foot.x, foot.y }, axis = new[] { d.x, d.y }, profile = prof.ToArray(),
                colour = new[] { roof.r, roof.g, roof.b },
                poly = new[] { foot + nl * hw, foot - nl * hw, lip - nl * hw, lip + nl * hw }.SelectMany(q => new[] { q.x, q.y }).ToArray()
            };
            BuildPrism(pr, terrain, mats, go);
            // the look: the roof's ribs running down the slope (every 0.76 m, as a 3 ft panel's), white trim along the eave and the sides, the two
            // standing west legs (boxed steel) at the eave's corners, the folded east legs lying flat by the foot, a crumpled sheet's edge at the foot
            var look = new MeshBuilder(); var trim = new Color(0.92f, 0.92f, 0.90f); var steel = new Color(0.55f, 0.56f, 0.57f);
            Vector3 W(float s, float lat, float up) { var q = foot + d * s + nl * lat; return new Vector3(q.x, fy + Rise(s) + up, q.y); }
            for (float lat = -hw + 0.38f; lat < hw; lat += 0.76f)
                for (float s = 0f; s < c.len - 1e-3f; s += 0.5f) Beam(look, W(s, lat, 0.02f), W(Mathf.Min(c.len, s + 0.5f), lat, 0.02f), 0.05f, 0.03f, Shade(roof, 0.82f));
            for (float s = 0f; s < c.len - 1e-3f; s += 0.5f)
                foreach (float side in new[] { -1f, 1f }) Beam(look, W(s, side * (hw - 0.03f), 0.03f), W(Mathf.Min(c.len, s + 0.5f), side * (hw - 0.03f), 0.03f), 0.08f, 0.06f, trim);
            Beam(look, W(c.len - 0.03f, -hw, 0.03f), W(c.len - 0.03f, hw, 0.03f), 0.06f, 0.08f, trim);   // the eave
            foreach (float side in new[] { -1f, 1f })
            {
                var top = W(c.len - 0.08f, side * (hw + 0.05f), -0.06f); float g = Ground(terrain, top.x, top.z);
                Beam(look, new Vector3(top.x, g - 0.1f, top.z), top, 0.07f, 0.07f, steel);   // a standing west leg
                var f0 = W(0.4f, side * (hw + 0.1f), 0f); var f1 = foot + d * 3.6f + nl * side * (hw + 0.45f);
                Beam(look, new Vector3(f0.x, Ground(terrain, f0.x, f0.z) + 0.06f, f0.z), new Vector3(f1.x, Ground(terrain, f1.x, f1.y) + 0.06f, f1.y), 0.07f, 0.07f, steel);   // a folded east leg on the grass
            }
            var lg = look.ToObject("RC carport look", mats.House, go, false); lg.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            Debug.Log($"[Round50] the carport: foot ({foot.x:0.0}, {foot.y:0.0}) {fy:0.00} m, eave ({lip.x:0.0}, {lip.y:0.0}) {fy + Rise(c.len):0.00} m ({fy + Rise(c.len) - Ground(terrain, lip.x, lip.y):0.0} m over the ground there), " +
                      $"{c.len:0.0} m long, {c.width:0.0} m wide, {c.angle_deg:0} degrees at the eave");
        }

        // Round 50 (A): the shrub beds' look and their ShrubBeds. Shrubs on a 1.5 m grid (jittered) inside each bed, none on a road, drive, sidewalk or
        // trail (DriveBands), in a building or on the shortcuts' carport; in the lake, cattails; the valley's bed is brush (bigger, darker, looser),
        // the yards' beds are planted shrubs (boxwood and holly greens, a pink azalea here and there, as in the photos)
        static readonly Color[] BrushGreens = { new Color(0.16f, 0.27f, 0.12f), new Color(0.20f, 0.30f, 0.13f), new Color(0.24f, 0.29f, 0.14f), new Color(0.30f, 0.27f, 0.15f) };
        static void BuildShrubBeds(TerrainGrid terrain, Materials mats, Transform root, LakesShrubBed[] beds, MapData map, Vector2[] keepOff)
        {
            if (beds == null || beds.Length == 0) return;
            var go = new GameObject("Shrub beds"); go.transform.SetParent(root, false);
            var shrubs = new Chunks(); var reeds = new Chunks();
            var segA = new List<Vector2>(); var segB = new List<Vector2>(); var segH = new List<float>();
            var rings = map.buildings.Select(bd => ToPoints(bd.footprint)).Where(r => r.Count >= 3).ToList();
            int nShrub = 0, nReed = 0, nSkip = 0;
            for (int bi = 0; bi < beds.Length; bi++)
            {
                var bed = beds[bi]; var p = ToPoints(bed.points); if (p.Count < 2) continue;
                for (int i = 0; i + 1 < p.Count; i++) { segA.Add(p[i]); segB.Add(p[i + 1]); segH.Add(bed.half); }
                bool brush = bed.half > 8f;   // the valley's
                float Dist(Vector2 q) { float best = float.MaxValue; for (int i = 0; i + 1 < p.Count; i++) best = Mathf.Min(best, SegDist(q, p[i], p[i + 1])); return best; }
                float x0 = p.Min(q => q.x) - bed.half, x1 = p.Max(q => q.x) + bed.half, z0 = p.Min(q => q.y) - bed.half, z1 = p.Max(q => q.y) + bed.half;
                var near = rings.Where(r => r.Any(v => v.x > x0 - 30f && v.x < x1 + 30f && v.y > z0 - 30f && v.y < z1 + 30f)).ToList();
                float step = brush ? 1.7f : 1.4f; int n = 0;
                for (float x = x0; x <= x1; x += step)
                    for (float z = z0; z <= z1; z += step)
                    {
                        n++; int seed = bi * 100003 + n;
                        var q = new Vector2(x + (Hash01(seed, 1) - 0.5f) * step * 0.8f, z + (Hash01(seed, 2) - 0.5f) * step * 0.8f);
                        if (Dist(q) > bed.half - 0.3f - 0.6f * Hash01(seed, 3)) continue;
                        if (DriveBands.At(q, 0.6f) != null || keepOff.Any(k => (k - q).sqrMagnitude < 25f) || near.Any(r => InPolygon(q, r) || RingDist(q, r) < 0.6f)) { nSkip++; continue; }
                        float wy = Beds != null ? WaterSurfaceAt(q) : float.NaN;
                        if (!float.IsNaN(wy))
                        {
                            // cattails: a clump of thin blades out of the water, a brown head on a few
                            var mb = reeds.At(q); float bed_ = Ground(terrain, q.x, q.y);
                            for (int k = 0; k < 7; k++)
                            {
                                var o = new Vector2(Hash01(seed, 10 + k) - 0.5f, Hash01(seed, 20 + k) - 0.5f) * 0.9f; float h = 1.3f + 0.8f * Hash01(seed, 30 + k);
                                var lean = new Vector3(Hash01(seed, 40 + k) - 0.5f, 0f, Hash01(seed, 50 + k) - 0.5f) * 0.35f;
                                var s0 = new Vector3(q.x + o.x, bed_, q.y + o.y); var s1 = new Vector3(q.x + o.x, wy + h, q.y + o.y) + lean;
                                Beam(mb, s0, s1, 0.035f, 0.035f, Color.Lerp(new Color(0.30f, 0.42f, 0.17f), new Color(0.45f, 0.48f, 0.22f), Hash01(seed, 60 + k)));
                                if (k < 3) Beam(mb, s1 - (s1 - s0).normalized * 0.45f, s1 - (s1 - s0).normalized * 0.12f, 0.07f, 0.07f, new Color(0.36f, 0.22f, 0.12f));
                            }
                            nReed++; continue;
                        }
                        float gy = Ground(terrain, q.x, q.y);
                        if (brush)
                        {
                            float r = 0.8f + 0.8f * Hash01(seed, 4); var col = PickOf(BrushGreens, seed, 5);
                            Blob(shrubs.At(q), new Vector3(q.x, gy - 0.1f, q.y), new Vector3(r, r * (0.9f + 0.5f * Hash01(seed, 6)), r * (0.8f + 0.3f * Hash01(seed, 7))), col * 1.15f, col * 0.7f, 7, 4, true, seed);
                        }
                        else
                        {
                            float r = 0.6f + 0.45f * Hash01(seed, 4);
                            var col = Hash01(seed, 8) < 0.12f ? new Color(0.72f, 0.30f, 0.52f) : PickOf(ShrubGreens, seed, 5);   // an azalea in bloom now and then
                            Blob(shrubs.At(q), new Vector3(q.x, gy - 0.05f, q.y), new Vector3(r, r * (1.0f + 0.4f * Hash01(seed, 6)), r), col * 1.15f, col * 0.75f, 8, 4, true, seed);
                        }
                        nShrub++;
                    }
            }
            shrubs.ToObjects("Shrub beds look", mats.House, go.transform, false);
            reeds.ToObjects("Shrub beds cattails", mats.House, go.transform, false);
            var sb = go.AddComponent<ShrubBeds>(); sb.a = segA.ToArray(); sb.b = segB.ToArray(); sb.half = segH.ToArray();
            Debug.Log($"[Round50] shrub beds: {beds.Length} beds ({string.Join(", ", beds.Select(b => b.id))}), {nShrub} shrubs, {nReed} cattail clumps, {nSkip} spots left bare (roads, drives, walks, buildings, the carport)");
        }

    }
}
