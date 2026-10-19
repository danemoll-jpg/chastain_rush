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
//   the hint    a hand-painted JUMP! board (R1's, Tools/handmade/make_party_sign.py) on the lap's verge at the turn-off, its arrow to the
//               yard; worn tracks across the yards; hidden in a forward race (the forward race's barrier across the gap is the route's)
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class LakesKickerData
        {
            public float[] lip, dir, foot, sign, tracks;
            public float foot_y, lip_y, len, width, angle_deg;
            public string clues_hidden_forward_of;
        }

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

            // the hint
            var clues = go;
            if (!string.IsNullOrEmpty(k.clues_hidden_forward_of))
            {
                var h = new GameObject("RK clues (hidden in a forward race)"); h.transform.SetParent(go, false);
                h.AddComponent<RaceOnly>().hideInForwardOf = k.clues_hidden_forward_of; clues = h.transform;
            }
            if (k.sign != null && k.sign.Length >= 3) KickerJumpSign(terrain, mats, clues, new Vector2(k.sign[0], k.sign[1]), k.sign[2]);
            if (k.tracks != null && k.tracks.Length >= 6) WornRuts(terrain, mats, clues, ToPoints(k.tracks), "RK worn tracks");
            Debug.Log($"[Round49] the kicker: foot ({foot.x:0.0}, {foot.y:0.0}) {k.foot_y:0.00} m, lip ({lip.x:0.0}, {lip.y:0.0}) {Y(k.len):0.00} m ({Y(k.len) - Ground(terrain, lip.x, lip.y):0.0} m over the ground there), " +
                      $"{k.len:0.0} m long, {k.width:0.0} m wide, {k.angle_deg:0} degrees at the lip");
        }

        // R1's hand-painted JUMP! board on two posts at the turn-off, its arrow right (toward the gap); breaks like a mailbox
        static void KickerJumpSign(TerrainGrid terrain, Materials mats, Transform parent, Vector2 at, float yawDeg)
        {
            var facing = new Vector2(Mathf.Sin(yawDeg * Mathf.Deg2Rad), Mathf.Cos(yawDeg * Mathf.Deg2Rad));
            var f = MakeFrame(new Vector3(at.x, 0f, at.y), facing, 1f);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (float u in new[] { -1.5f, 1.5f }) { var q = f.P(u, 0f, -0.05f); float g = Ground(terrain, q.x, q.z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            f.O.y = hi;
            var br = new Breakables(); var mb = br.Begin(at);
            var post = new Color(0.40f, 0.31f, 0.22f); var edge = new Color(0.93f, 0.91f, 0.86f);
            foreach (float u in new[] { -1.45f, 1.45f }) br.Piece(() => BoxF(mb, f, u - 0.06f, u + 0.06f, lo - hi - 0.3f, 2.6f, -0.1f, -0.01f, post), lo + 0.05f);
            foreach (float u in new[] { -0.8f, 0.8f }) br.Piece(() => BoxF(mb, f, u - 0.8f, u + 0.8f, 0.9f, 2.5f, -0.01f, 0.03f, edge), lo + 0.05f);
            var mat = PartySignMaterial(false, "JumpSign");
            if (mat != null)
            {
                var plate = SignPlate(parent, f.P(0f, 1.7f, 0.035f), -f.F, 1f, mat);
                plate.name = "Jump sign face RK"; plate.transform.localScale = new Vector3(3.2f, 1.6f, 1f);
                br.Face(plate);
            }
            br.End(f.P(0f, 1.7f, -0.03f), f.F, 3.6f, 3.0f, 0.9f);
            br.ToObjects("Jump sign RK", mats.House, parent, 1f, BreakSounds.Kind.Wood);
            Debug.Log($"[Round49] the kicker's JUMP! sign at ({at.x:0.0}, {at.y:0.0}), facing {yawDeg:0}, its arrow right");
        }
    }
}
