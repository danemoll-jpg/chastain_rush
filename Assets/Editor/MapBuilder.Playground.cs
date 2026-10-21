using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 18, item 7: reverse shortcut R3, the play set and the trampoline in house 1752's back yard (Data/shortcuts, made by
// Tools/mapgen/make_shortcuts.py, build_r3). The vehicle drives up the slide, across the tower's deck and off its far edge
// onto the trampoline, which throws it over 1752 onto the lane (Trampoline.cs). Giant-sized, because a vehicle has to fit:
//   play set   a wooden tower (four posts, a deck 4.0 x 3.5 m, rails along its sides, a striped canopy roof 3.4 m over the deck,
//              a ladder on its north side) and a bright yellow slide 3.6 m wide with raised sides, on legs. The slide and the
//              deck are the shortcut's prisms (held, "open": boards on legs), built by BuildPrism; this adds the rest
//   trampoline 13 m across the mat on a 1 m frame: a black mat (drawn apart: it dips and springs back), a blue padded rim,
//              a steel frame on legs and a fabric skirt down to the grass all round, so nothing can get under it (solid: the
//              skirt and mat are one convex collider)
// Look and size: every part is in proportion to that size (posts 0.3 m, rails 1 m high, the pad 0.75 m wide).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly Color PlayWood = new Color(0.55f, 0.40f, 0.25f), PlayWoodDark = new Color(0.42f, 0.30f, 0.19f), SlideYellow = new Color(0.95f, 0.76f, 0.14f),
            CanopyGreen = new Color(0.16f, 0.55f, 0.28f), CanopyYellow = new Color(0.96f, 0.85f, 0.30f), MatBlack = new Color(0.07f, 0.07f, 0.08f),
            PadBlue = new Color(0.12f, 0.38f, 0.86f), SkirtGreen = new Color(0.06f, 0.22f, 0.12f), FrameSteel = new Color(0.55f, 0.57f, 0.60f);

        static void BuildPlayground(ScShortcut sc, TerrainGrid terrain, Materials mats, Transform parent)
        {
            var pg = sc.playground; var tw = pg.tower; var tr = pg.trampoline;
            var root = new GameObject("Play set and trampoline (R3)").transform; root.SetParent(parent, false);
            var u2 = new Vector2(tw.u[0], tw.u[1]).normalized; var n2 = new Vector2(-u2.y, u2.x);
            Vector3 W(Vector2 q, float y) => new Vector3(q.x, y, q.y);
            float G(Vector2 q) => Ground(terrain, q.x, q.y);
            var look = new MeshBuilder(); var solid = new MeshBuilder(); var rails = new MeshBuilder();

            // ---- the tower: posts outside the run, rails along the sides, the canopy, a ladder
            var D0 = new Vector2(tw.o[0], tw.o[1]);
            float hw = tw.half + 0.25f;
            var corners = new[] { D0 + n2 * hw, D0 - n2 * hw, D0 + u2 * tw.len + n2 * hw, D0 + u2 * tw.len - n2 * hw };
            foreach (var cq in corners) { var b = W(cq, G(cq) - 0.2f); Post(solid, b, tw.roof_y - b.y + 0.05f, 0.32f, PlayWood); }
            foreach (float side in new[] { 1f, -1f })
            {
                var a = D0 + n2 * side * (tw.half + 0.05f); var b = a + u2 * tw.len;
                Beam(solid, W(a, tw.deck_y + 0.95f), W(b, tw.deck_y + 0.95f), 0.12f, 0.12f, PlayWoodDark);
                Beam(solid, W(a, tw.deck_y + 0.45f), W(b, tw.deck_y + 0.45f), 0.08f, 0.08f, PlayWoodDark);
                for (float s = 0.5f; s < tw.len; s += 0.5f) { var q = a + u2 * s; Beam(look, W(q, tw.deck_y), W(q, tw.deck_y + 0.95f), 0.05f, 0.05f, PlayWood); }
                // the deck's support beams under its sides
                Beam(solid, W(a, tw.deck_y - 0.4f), W(b, tw.deck_y - 0.4f), 0.2f, 0.3f, PlayWoodDark);
            }
            // canopy: two slopes, striped, ridge along u, 0.4 m overhang
            {
                float eave = tw.roof_y, ridge = tw.roof_y + 1.1f, oh = 0.45f;
                int strips = 8;
                for (int k = 0; k < strips; k++)
                {
                    float s0 = -oh + (tw.len + 2f * oh) * k / strips, s1 = -oh + (tw.len + 2f * oh) * (k + 1) / strips;
                    look.Tint = (k & 1) == 0 ? CanopyGreen : CanopyYellow;
                    foreach (float side in new[] { 1f, -1f })
                    {
                        var e0 = D0 + u2 * s0 + n2 * side * (hw + oh); var e1 = D0 + u2 * s1 + n2 * side * (hw + oh);
                        var r0 = D0 + u2 * s0; var r1 = D0 + u2 * s1;
                        Face(look, W(e0, eave), W(e1, eave), W(r1, ridge), W(r0, ridge), Vector3.up);
                        Face(look, W(e0, eave - 0.02f), W(e1, eave - 0.02f), W(r1, ridge - 0.02f), W(r0, ridge - 0.02f), Vector3.down);
                    }
                }
            }
            // ladder on the north (left) side: two rails leaning out to the ground, rungs every 0.35 m
            {
                var mid = D0 + u2 * (tw.len * 0.5f) + n2 * (hw + 0.05f); var foot = mid + n2 * 1.4f;
                foreach (float o in new[] { -0.45f, 0.45f })
                {
                    var a = foot + u2 * o; var b = mid + u2 * o;
                    Beam(solid, W(a, G(a) - 0.1f), W(b, tw.deck_y + 1.0f), 0.1f, 0.1f, PlayWoodDark);
                }
                float h0 = G(foot), h1 = tw.deck_y;
                for (float y = h0 + 0.35f; y < h1; y += 0.35f)
                {
                    float f = (y - h0) / (h1 - h0); var q = Vector2.Lerp(foot, mid, f);
                    Beam(look, W(q - u2 * 0.45f, y), W(q + u2 * 0.45f, y), 0.06f, 0.06f, PlayWood);
                }
            }

            // ---- the slide: raised sides along its edges and legs under it, from its prism's profile
            var slidePr = sc.prisms.FirstOrDefault(p => p.kind == "slide");
            if (slidePr != null)
            {
                var O = new Vector2(slidePr.origin[0], slidePr.origin[1]); var U = new Vector2(slidePr.axis[0], slidePr.axis[1]).normalized; var Nn = new Vector2(-U.y, U.x);
                float half = pg.slide.half;
                // the flared foot: the side's distance from the axis, and the sides' height (low over the flare: they lead a wheel in)
                float HalfAt(float s) => pg.slide.flare > 0f && s < pg.slide.flare ? Mathf.Lerp(pg.slide.foot_half, half, s / pg.slide.flare) : half;
                float SideH(float s) => 0.55f * Mathf.Clamp01(s / Mathf.Max(3f, pg.slide.flare));
                for (float s = 0f; s < pg.slide.len - 1e-3f; s += 0.5f)
                {
                    float s1 = Mathf.Min(pg.slide.len, s + 0.5f);
                    float y0 = Profile(slidePr.profile, s), y1 = Profile(slidePr.profile, s1);
                    float h0 = SideH(s), h1 = SideH(s1);   // the sides rise from nothing at the foot (nothing to catch a wheel)
                    // Round 54 (D3, Dan BUG-008 "i kept losing momentum on this slide"): over the flared foot the sides close in at 24 degrees to the
                    // travel, and a vehicle coming in a little off the middle ran up onto one and hit its edge (the Drifter Twin 19 to 8.6 m/s in one
                    // step). There they are look only; from the end of the flare on they are solid, a collider of their own named as the shortcut
                    // surfaces (VehicleSurfaceContacts: a contact from above gets the true face normal) with the ramp material
                    var rb = s < pg.slide.flare ? look : rails;
                    foreach (float side in new[] { 1f, -1f })
                    {
                        var a = O + U * s + Nn * side * HalfAt(s); var b = O + U * s1 + Nn * side * HalfAt(s1); var ao = a + Nn * side * 0.14f; var bo = b + Nn * side * 0.14f;
                        rb.Tint = Shade(SlideYellow, 0.88f);
                        Face(rb, W(a, y0 - 0.25f), W(b, y1 - 0.25f), W(b, y1 + h1), W(a, y0 + h0), new Vector3(-Nn.x * side, 0f, -Nn.y * side));
                        Face(rb, W(ao, y0 - 0.25f), W(bo, y1 - 0.25f), W(bo, y1 + h1), W(ao, y0 + h0), new Vector3(Nn.x * side, 0f, Nn.y * side));
                        rb.Tint = Shade(SlideYellow, 1.05f);
                        Face(rb, W(a, y0 + h0), W(b, y1 + h1), W(bo, y1 + h1), W(ao, y0 + h0), Vector3.up);
                    }
                }
                for (float s = 3f; s < pg.slide.len - 1f; s += 3.5f)
                    foreach (float side in new[] { 1f, -1f })
                    {
                        var q = O + U * s + Nn * side * (half - 0.3f); float top = Profile(slidePr.profile, s) - 0.25f, g = G(q);
                        if (top - g > 0.4f) Post(look, W(q, g - 0.2f), top - g + 0.2f, 0.18f, FrameSteel);
                    }
                // Round 18 (the check): closed underneath. Vehicles that came at the slide from the side drove under it between its
                // legs, and a car could wedge under the chute: lattice panels from the chute's edges down to the grass, solid
                for (float s = 0f; s < pg.slide.len - 1e-3f; s += 0.5f)
                {
                    float s1 = Mathf.Min(pg.slide.len, s + 0.5f);
                    foreach (float side in new[] { 1f, -1f })
                    {
                        var a = O + U * s + Nn * side * (HalfAt(s) + 0.14f); var b = O + U * s1 + Nn * side * (HalfAt(s1) + 0.14f);
                        float ta = Profile(slidePr.profile, s) - 0.25f, tb = Profile(slidePr.profile, s1) - 0.25f;
                        if (ta - G(a) < 0.05f && tb - G(b) < 0.05f) continue;
                        solid.Tint = ((int)(s / 0.5f) & 1) == 0 ? PlayWood : PlayWoodDark;
                        Face(solid, W(a, G(a) - 0.2f), W(b, G(b) - 0.2f), W(b, tb), W(a, ta), new Vector3(Nn.x * side, 0f, Nn.y * side));
                    }
                }
            }
            // the tower's base: lattice on all four sides, from under the deck down to the grass
            {
                Vector2 c0 = D0 - n2 * tw.half, c1 = D0 + n2 * tw.half, c2 = D0 + u2 * tw.len + n2 * tw.half, c3 = D0 + u2 * tw.len - n2 * tw.half;
                foreach (var (a, b, outN) in new[] { (c0, c3, -n2), (c1, c2, n2), (c3, c2, u2), (c0, c1, -u2) })
                {
                    int k = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.5f));
                    for (int i = 0; i < k; i++)
                    {
                        var p = Vector2.Lerp(a, b, i / (float)k); var q = Vector2.Lerp(a, b, (i + 1) / (float)k);
                        solid.Tint = (i & 1) == 0 ? PlayWood : PlayWoodDark;
                        Face(solid, W(p, G(p) - 0.2f), W(q, G(q) - 0.2f), W(q, tw.deck_y - 0.25f), W(p, tw.deck_y - 0.25f), new Vector3(outN.x, 0f, outN.y));
                    }
                }
            }

            // ---- the trampoline
            var c = new Vector2(tr.x, tr.z); float R = tr.r, pad = 0.75f, rimY = tr.mat_y + 0.12f;
            const int seg = 40;
            float low = float.MaxValue; for (int i = 0; i < seg; i++) { float a = Mathf.PI * 2f * i / seg; low = Mathf.Min(low, G(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (R + pad))); }
            Vector2 Rim(float a, float r) => c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            for (int i = 0; i < seg; i++)
            {
                float a0 = Mathf.PI * 2f * i / seg, a1 = Mathf.PI * 2f * (i + 1) / seg;
                Vector3 Out(float a) => new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                look.Tint = (i & 1) == 0 ? PadBlue : Shade(PadBlue, 0.88f);
                Face(look, W(Rim(a0, R - 0.05f), rimY), W(Rim(a1, R - 0.05f), rimY), W(Rim(a1, R + pad), rimY), W(Rim(a0, R + pad), rimY), Vector3.up);
                look.Tint = Shade(PadBlue, 0.75f);
                Face(look, W(Rim(a0, R + pad), rimY - 0.28f), W(Rim(a1, R + pad), rimY - 0.28f), W(Rim(a1, R + pad), rimY), W(Rim(a0, R + pad), rimY), Out((a0 + a1) * 0.5f));
                look.Tint = SkirtGreen;
                Face(look, W(Rim(a0, R + pad - 0.05f), G(Rim(a0, R + pad)) - 0.1f), W(Rim(a1, R + pad - 0.05f), G(Rim(a1, R + pad)) - 0.1f), W(Rim(a1, R + pad - 0.05f), rimY - 0.28f), W(Rim(a0, R + pad - 0.05f), rimY - 0.28f), Out((a0 + a1) * 0.5f));
                if (i % 4 == 0)   // the frame's legs, standing out from the skirt a little
                {
                    var q = Rim(a0, R + pad + 0.06f);
                    Post(look, W(q, G(q) - 0.1f), rimY - 0.2f - G(q) + 0.1f, 0.1f, FrameSteel);
                }
            }
            // the mat (look only, moved by Trampoline): a disc, with the spring line round its edge
            var matMb = new MeshBuilder { Tint = MatBlack };
            int ctr = matMb.Vertex(new Vector3(0f, 0.02f, 0f)), first = matMb.Count;
            for (int i = 0; i < seg; i++) { float a = Mathf.PI * 2f * i / seg; matMb.Vertex(new Vector3(Mathf.Cos(a) * (R - 0.05f), 0.02f, Mathf.Sin(a) * (R - 0.05f))); }
            for (int i = 0; i < seg; i++) { int a = first + i, b = first + (i + 1) % seg; if (Vector3.Dot(Vector3.Cross(matMb.Verts[a] - matMb.Verts[ctr], matMb.Verts[b] - matMb.Verts[ctr]), Vector3.up) >= 0f) matMb.Tri(ctr, a, b); else matMb.Tri(ctr, b, a); }
            matMb.Tint = FrameSteel;
            for (int i = 0; i < seg * 2; i++)
            {
                float a = Mathf.PI * 2f * i / (seg * 2);
                var p0 = new Vector3(Mathf.Cos(a) * (R - 0.35f), 0.04f, Mathf.Sin(a) * (R - 0.35f)); var p1 = new Vector3(Mathf.Cos(a) * (R - 0.04f), 0.04f, Mathf.Sin(a) * (R - 0.04f));
                Beam(matMb, p0, p1, 0.035f, 0.02f, FrameSteel);
            }
            var tramp = new GameObject("Trampoline"); tramp.transform.SetParent(root, false); tramp.transform.position = new Vector3(c.x, tr.mat_y, c.y);
            var matGo = matMb.ToObject("Trampoline mat", mats.House, tramp.transform, false);
            matGo.transform.localPosition = Vector3.zero; GameObjectUtility.SetStaticEditorFlags(matGo, 0);   // it moves
            // the collider: one convex solid, the skirt's ring from under the grass to the mat
            var col = new MeshBuilder();
            for (int i = 0; i < 24; i++)
            {
                float a = Mathf.PI * 2f * i / 24f; var q = Rim(a, R + pad);
                col.Vertex(W(q, low - 0.4f)); col.Vertex(W(q, tr.mat_y));
            }
            for (int i = 0; i < 24; i++) { int a = i * 2, b = ((i + 1) % 24) * 2; col.Tri(a, b, a + 1); col.Tri(b, b + 1, a + 1); }
            var cm = new Mesh { name = "Trampoline collider" }; cm.SetVertices(col.Verts); cm.SetTriangles(col.Tris, 0); cm.RecalculateBounds();
            cm = GenAsset(cm, $"{GeneratedDir}/Meshes/Trampoline collider.asset");
            var cgo = new GameObject("Trampoline collider"); cgo.transform.SetParent(root, false);
            var mc = cgo.AddComponent<MeshCollider>(); mc.sharedMesh = cm; mc.convex = true; mc.sharedMaterial = RampMaterial();
            var t = tramp.AddComponent<Trampoline>();
            t.radius = R; t.matY = tr.mat_y; t.target = new Vector3(tr.target[0], tr.target[1], tr.target[2]); t.flight = tr.flight_s; t.landYaw = tr.land_yaw; t.mat = matGo.transform;

            var lookGo = look.ToObject("Play set and trampoline", mats.House, root, false);
            var solidGo = solid.ToObject("Play set (solid parts)", mats.House, root, false);
            AddCollider(solidGo, solid, "Play set collider");
            if (rails.Verts.Count > 0) { var railGo = rails.ToObject("sc R3 play set slide rails", mats.House, root, false); AddCollider(railGo, rails, "sc R3 play set slide rails"); }   // Round 54 (D3)
            Debug.Log($"[Round18] R3 play set: tower deck {tw.len:0.0} x {2f * tw.half:0.0} m at {tw.deck_y:0.00} m (canopy {tw.roof_y - tw.deck_y:0.0} m over it), slide {2f * pg.slide.half:0.0} m wide and {pg.slide.len:0} m long; trampoline {2f * R:0} m across the mat ({2f * (R + pad):0.0} m with the pad) at {tr.mat_y:0.00} m, aimed at ({tr.target[0]:0.0}, {tr.target[2]:0.0}) in {tr.flight_s:0.0} s");
        }
    }
}
