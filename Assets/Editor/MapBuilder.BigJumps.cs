using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 19, item 2 (Dan: "I feel like we should also add some big jumps too"): four big jumps on the Noonday Creek trail, from
// Data/tracks/noonday_creek.json 'big_jumps' (Tools/mapgen/make_creek_track.py: where, how high, the lip's angle; two seconds of air
// or more at 24 m/s). Each a different idea, each something that could be in those woods, at its real size:
//   footbridge   BJ1, the side gully: a washed-out wooden footbridge. The near span climbs from the bank (5 degrees) and its broken
//                end is heaved up (23 degrees at the lip, splintered planks past it); the middle span is gone; the far span has
//                collapsed and lies down the far bank, so a vehicle that comes up short lands in the gully and drives up it
//   stepdown     BJ2, off the top of the bank: a dirt lip on the bank's edge, landing down the bank on the creek flats
//   fallen_tree  BJ3: an earth ramp heaped against a huge fallen tree lying across the trail (1.6 m through, its root plate up on
//                one side, its top on the other); the lip is the log; behind it the earth falls away, so nothing to drop into
//   outcrop      BJ4: a granite slab tilted up out of the hump on the south loop
//   outfall      P1J (Round 20, BUG-002): P1's outfall apron at the pipe's mouth, turned up, over the small pond (not on the trail)
// The driven surfaces are prisms (BuildPrism: mesh colliders, frictionless, "sc" names so VehicleSurfaceContacts treats them as
// ramps); railings are solid, everything else (piers, planks, the log, the root plate) is look only. Each jump gets a BigJump
// (runtime: the launch speed cap) and its landing zone comes from the file (RampHold's landing carry).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly Color OldWood = new Color(0.46f, 0.38f, 0.28f), WetWood = new Color(0.34f, 0.28f, 0.21f), Granite = new Color(0.56f, 0.55f, 0.52f), RootEarth = new Color(0.33f, 0.25f, 0.17f);

        static void BuildBigJumps(TerrainGrid terrain, Materials mats, Transform parent, CreekTrack ct)
        {
            if (ct.Data.big_jumps == null || ct.Data.big_jumps.Length == 0) return;
            var root = new GameObject("Big jumps").transform; root.SetParent(parent, false);
            foreach (var b in ct.Data.big_jumps)
            {
                var L = new Vector2(b.lip[0], b.lip[1]); var d = new Vector2(b.dir[0], b.dir[1]).normalized; var nl = new Vector2(-d.y, d.x);
                var O = L - d * b.ramp_m; float y0 = Ground(terrain, O.x, O.y) - 0.02f, y1 = b.lip_y, k = Mathf.Tan(b.angle_deg * Mathf.Deg2Rad), hw = b.width * 0.5f;
                var jr = new GameObject(b.id + " " + b.look).transform; jr.SetParent(root, false);
                var look = new MeshBuilder();
                float[] KickerProfile()
                {
                    // J4's curve: from y0 at O, steepening to the lip's slope k at the lip; flat first if the rise is small for its length
                    float R = y1 - y0, Lk = Mathf.Min(b.ramp_m, 2f * R / k), flat = b.ramp_m - Lk;
                    var pr = new List<float>();
                    for (float s = 0f; s <= b.ramp_m + 1e-3f; s += 0.5f) { float u = Mathf.Max(0f, s - flat); pr.Add(s); pr.Add(y0 + k * u * u / (2f * Lk)); }
                    return pr.ToArray();
                }
                ScPrism Prism(string kind, float[] profile, float sEnd, float halfW, Color c, int[] skirts = null) => new ScPrism
                {
                    name = $"sc {b.id} {b.look} {kind}", kind = kind, origin = new[] { O.x, O.y }, axis = new[] { d.x, d.y }, profile = profile, colour = new[] { c.r, c.g, c.b }, skirt_edges = skirts,
                    poly = new[] { O + nl * halfW, O - nl * halfW, O + d * sEnd - nl * halfW, O + d * sEnd + nl * halfW }.SelectMany(q => new[] { q.x, q.y }).ToArray()
                };
                Vector3 W(float s, float lat, float y) { var q = O + d * s + nl * lat; return new Vector3(q.x, y, q.y); }

                if (b.look == "footbridge")
                {
                    // the near span: an open deck, 5 degrees, the last 4.5 m heaved up to the lip's angle. Round 20 (BUG-001): its end
                    // starts at the lowest ground across its width (it stood a little proud where the ground falls away to one side)
                    for (float lat = -hw; lat <= hw + 1e-3f; lat += 0.5f) { var q = O + nl * lat; y0 = Mathf.Min(y0, Ground(terrain, q.x, q.y) - 0.03f); }
                    const float Lk = 4.5f; float R = y1 - y0, s1 = (R - k * Lk * 0.5f) / (b.ramp_m - Lk * 0.5f);
                    var pr = new List<float>();
                    for (float s = 0f; s <= b.ramp_m + 1e-3f; s += 0.5f)
                    {
                        float u = Mathf.Max(0f, s - (b.ramp_m - Lk));
                        pr.Add(s); pr.Add(y0 + s1 * s + (k - s1) * u * u / (2f * Lk));
                    }
                    BuildPrism(Prism("deck", pr.ToArray(), b.ramp_m, hw, OldWood), terrain, mats, jr);
                    float DeckY(float s) => Profile(pr.ToArray(), s);
                    // railings: posts every 2 m, a top rail and a mid rail. Round 20 (BUG-001, Dan: "there is a catch right here that
                    // brings the bike to a stop"): the first post faced the approach and stopped a vehicle 2 m off the line dead. The
                    // near ends now flare out (wing rails, 0.8 m wider at the deck's end, in over the first 3.5 m) and only a smooth
                    // panel along each railing is solid (no post faces): a vehicle that meets one glances off it
                    const float Flare = 0.8f, FlareLen = 3.5f;
                    float RailLat(float s) => hw - 0.12f + Flare * (1f - Smooth01(Mathf.Clamp01(s / FlareLen)));
                    float RailFoot(float s, float lat) { var q = O + d * s + nl * lat; return Mathf.Abs(lat) > hw ? Mathf.Min(DeckY(s), Ground(terrain, q.x, q.y)) : DeckY(s); }
                    var rail = new MeshBuilder(); var railWall = new MeshBuilder();
                    foreach (float side in new[] { 1f, -1f })
                    {
                        Vector3? pt = null, pm = null;
                        for (float s = 0f; s <= b.ramp_m - 0.6f; s += 2f)
                        {
                            float lat = side * RailLat(s);
                            var bt = W(s, lat, RailFoot(s, lat)); Post(rail, bt, 1.05f + DeckY(s) - bt.y, 0.12f, Shade(OldWood, 0.85f));
                            var top = W(s, lat, DeckY(s) + 1.0f); var mid = W(s, lat, DeckY(s) + 0.5f);
                            if (pt.HasValue) { Beam(rail, pt.Value, top, 0.09f, 0.12f, OldWood); Beam(rail, pm.Value, mid, 0.07f, 0.1f, Shade(OldWood, 0.9f)); }
                            pt = top; pm = mid;
                        }
                        // the solid panel: both sides, from just under the deck to the top rail
                        float sEnd = Mathf.Floor((b.ramp_m - 0.6f) / 2f) * 2f;
                        for (float s = 0f; s < sEnd - 1e-3f; s += 0.5f)
                        {
                            float s2 = Mathf.Min(s + 0.5f, sEnd), l1 = side * RailLat(s), l2 = side * RailLat(s2);
                            Vector3 a0 = W(s, l1, RailFoot(s, l1) - 0.2f), a1 = W(s, l1, DeckY(s) + 1.05f), b0 = W(s2, l2, RailFoot(s2, l2) - 0.2f), b1 = W(s2, l2, DeckY(s2) + 1.05f);
                            var o = new Vector3(nl.x, 0f, nl.y) * side;
                            Face(railWall, a0, b0, b1, a1, o); Face(railWall, a0, b0, b1, a1, -o);
                        }
                    }
                    var rg = rail.ToObject(b.id + " railings", mats.House, jr, false); AddCollider(rg, railWall, b.id + " railings collider");
                    // under it: two stringers and posts down to the ground every 4 m (look only)
                    foreach (float side in new[] { 1f, -1f })
                    {
                        Beam(look, W(0f, side * (hw - 0.4f), DeckY(0f) - 0.4f), W(b.ramp_m, side * (hw - 0.4f), DeckY(b.ramp_m) - 0.4f), 0.22f, 0.3f, WetWood);
                        for (float s = 3f; s <= b.ramp_m + 0.01f; s += 4f) { var top = W(s, side * (hw - 0.4f), DeckY(s) - 0.5f); float g = Ground(terrain, top.x, top.z) - 0.3f; if (top.y - g > 0.3f) Post(look, new Vector3(top.x, g, top.z), top.y - g, 0.28f, WetWood); }
                    }
                    // the broken end: splintered planks past the lip, bent up
                    for (int i = 0; i < 7; i++)
                    {
                        float lat = -hw + 0.45f + i * (b.width - 0.9f) / 6f, len = 0.25f + 0.7f * Hash01(i, 911), lift = 0.15f + 0.35f * Hash01(i, 913);
                        var a = W(b.ramp_m - 0.05f, lat, y1 + 0.1f); var e = a + new Vector3(d.x, 0f, d.y) * len + Vector3.up * len * lift;
                        Beam(look, a, e, 0.22f, 0.05f, Shade(OldWood, 0.8f + 0.25f * Hash01(i, 917)));
                    }
                    // the far span, collapsed down the far bank: from the gully's floor to the bank's top (a landing deck)
                    float sLo = b.ramp_m, lo = float.MaxValue, sHi = b.ramp_m + 20f;
                    for (float s = b.ramp_m + 1f; s <= b.ramp_m + 16f; s += 0.5f) { var q = O + d * s; float g = Ground(terrain, q.x, q.y); if (g < lo) { lo = g; sLo = s; } }
                    for (float s = sLo; s <= b.ramp_m + 30f; s += 0.5f) { var q = O + d * s; if (Ground(terrain, q.x, q.y) > y1 - 2.4f) { sHi = s; break; } }
                    var far = new List<float>(); float s0 = sLo - 1f, sEndFar = sHi + 1.5f;
                    for (float s = s0; s <= sEndFar + 1e-3f; s += 0.5f)
                    {
                        var q = O + d * s; float g = Ground(terrain, q.x, q.y);
                        far.Add(s - s0); far.Add(g + Mathf.Lerp(-0.18f, 0.1f, Smooth01((s - s0) / 1.5f)));   // its low end in the gully's floor: no step up onto it
                    }
                    var fp = new ScPrism
                    {
                        name = $"sc {b.id} {b.look} fallen span", kind = "deckland", origin = new[] { (O + d * s0).x, (O + d * s0).y }, axis = new[] { d.x, d.y }, profile = far.ToArray(), colour = new[] { WetWood.r, WetWood.g, WetWood.b },
                        poly = new[] { O + d * s0 + nl * hw, O + d * s0 - nl * hw, O + d * sEndFar - nl * hw, O + d * sEndFar + nl * hw }.SelectMany(q => new[] { q.x, q.y }).ToArray()
                    };
                    BuildPrism(fp, terrain, mats, jr);
                    // its one railing, lying askew on it; two planks from the middle span down in the gully (look only)
                    for (float s = s0 + 1f; s < sEndFar - 1f; s += 2f) { var a = W(s, hw - 0.3f, Profile(far.ToArray(), s - s0) + 0.1f); Beam(look, a, a + new Vector3(d.x, 0f, d.y) * 1.9f + new Vector3(nl.x, 0.15f, nl.y) * 0.25f, 0.09f, 0.12f, Shade(WetWood, 0.9f)); }
                    for (int i = 0; i < 2; i++)
                    {
                        var c = O + d * (b.ramp_m + 2.5f + i * 2f) + nl * (i == 0 ? -1.6f : 1.9f); float g = Ground(terrain, c.x, c.y) + 0.05f;
                        var dd = (d + nl * (i == 0 ? 0.6f : -0.4f)).normalized;
                        Beam(look, new Vector3(c.x - dd.x * 1.6f, g, c.y - dd.y * 1.6f), new Vector3(c.x + dd.x * 1.6f, g + 0.15f, c.y + dd.y * 1.6f), 0.3f, 0.06f, WetWood);
                    }
                    Debug.Log($"[Round19] {b.id} footbridge: near span {b.ramp_m:0.0} m from {y0:0.00} m to the lip at {y1:0.00} m ({Mathf.Atan(s1) * Mathf.Rad2Deg:0} then {b.angle_deg:0} degrees), {b.width:0.0} m wide; the fallen far span {sEndFar - s0:0.0} m from the gully's floor ({lo:0.00} m, {sLo - b.ramp_m:0.0} m past the lip) up the far bank");
                }
                else if (b.look == "stepdown")
                {
                    BuildPrism(Prism("kicker", KickerProfile(), b.ramp_m, hw, new Color(0.40f, 0.28f, 0.18f)), terrain, mats, jr);   // a dirt lip (DirtPileLook)
                    Debug.Log($"[Round19] {b.id} step-down: a dirt lip {b.ramp_m:0.0} m long from {y0:0.00} m to {y1:0.00} m ({b.angle_deg:0} degrees at the lip) on the bank's edge, {b.width:0.0} m wide");
                }
                else if (b.look == "fallen_tree")
                {
                    // the earth ramp up to the log, over it, and down behind (nothing to drop into)
                    var pr = KickerProfile().ToList();
                    var back = L + d * 0.8f; float gb = Ground(terrain, back.x, back.y);
                    pr.Add(b.ramp_m + 0.6f); pr.Add(y1 - 0.04f); pr.Add(b.ramp_m + 1.2f); pr.Add(y1 - 0.25f);
                    float sEnd = b.ramp_m + 1.2f + Mathf.Max(0.5f, (y1 - 0.25f - gb) / 0.75f);
                    pr.Add(sEnd); pr.Add(gb - 0.05f);
                    BuildPrism(Prism("ramp", pr.ToArray(), sEnd, hw, new Color(0.40f, 0.30f, 0.20f), new[] { 1, 3 }), terrain, mats, jr);
                    // the log: 1.6 m through, its crown just over the lip; root plate up on the left, its top tapering away on the right
                    const float Rlog = 0.8f; var c0 = L + d * 0.65f; float cy = y1 + 0.05f - Rlog;
                    var a = c0 + nl * 9f; var e = c0 - nl * 7.5f;
                    LogCyl(look, new Vector3(a.x, cy, a.y), new Vector3(e.x, cy - 0.1f, e.y), Rlog, 12);
                    var e2 = c0 - nl * 13f; LogCyl(look, new Vector3(e.x, cy - 0.1f, e.y), new Vector3(e2.x, Ground(terrain, e2.x, e2.y) + 0.45f, e2.y), 0.5f, 10);
                    foreach (var (at, up) in new[] { (-8.5f, 0.7f), (-10.5f, -0.4f), (-11.8f, 0.5f) })
                    {
                        var p = c0 + nl * at; var bp = new Vector3(p.x, cy + 0.2f, p.y);
                        Beam(look, bp, bp + new Vector3(d.x * up, 1.2f, d.y * up) * 1.6f, 0.18f, 0.18f, LogBark);
                    }
                    // the root plate: a ragged disc of earth and roots standing up at the log's foot, facing along the log
                    var rc = c0 + nl * 9.3f; var rcy = Ground(terrain, rc.x, rc.y) + 1.5f; var ax = new Vector3(nl.x, 0f, nl.y); var u1 = new Vector3(d.x, 0f, d.y);
                    for (int i = 0; i < 14; i++)
                    {
                        float t0 = Mathf.PI * 2f * i / 14f, t1 = Mathf.PI * 2f * (i + 1) / 14f;
                        float r0 = 2.2f + 0.5f * Hash01(i, 931), r1 = 2.2f + 0.5f * Hash01((i + 1) % 14, 931);
                        var cc = new Vector3(rc.x, rcy, rc.y);
                        Vector3 P0 = cc + (u1 * Mathf.Cos(t0) + Vector3.up * Mathf.Sin(t0)) * r0, P1 = cc + (u1 * Mathf.Cos(t1) + Vector3.up * Mathf.Sin(t1)) * r1;
                        P0.y = Mathf.Max(P0.y, Ground(terrain, P0.x, P0.z) - 0.2f); P1.y = Mathf.Max(P1.y, Ground(terrain, P1.x, P1.z) - 0.2f);
                        look.Tint = Shade(RootEarth, 0.85f + 0.25f * Hash01(i, 933));
                        TriFace(look, cc - ax * 0.25f, P0 - ax * 0.25f, P1 - ax * 0.25f, -ax); TriFace(look, cc + ax * 0.3f, P0 + ax * 0.3f, P1 + ax * 0.3f, ax);
                        Face(look, P0 - ax * 0.25f, P1 - ax * 0.25f, P1 + ax * 0.3f, P0 + ax * 0.3f, (P0 + P1) * 0.5f - cc);
                        if (Hash01(i, 935) > 0.4f) Beam(look, P0, P0 + (P0 - cc).normalized * 0.9f + ax * 0.3f, 0.08f, 0.08f, Shade(RootEarth, 0.7f));   // a root sticking out
                    }
                    Debug.Log($"[Round19] {b.id} fallen tree: an earth ramp {b.ramp_m:0.0} m from {y0:0.00} m to the lip at {y1:0.00} m ({b.angle_deg:0} degrees), over a 1.6 m log 22 m long and down behind it, {b.width:0.0} m wide");
                }
                else if (b.look == "outcrop")
                {
                    // a granite slab: wider in the middle, its sides sloped rock shoulders, its back a short drop
                    var pr = KickerProfile();
                    var poly = new[] { O + nl * (hw - 0.3f), O - nl * (hw - 0.3f), O + d * (b.ramp_m * 0.55f) - nl * (hw + 0.5f), L - nl * (hw - 0.1f), L + nl * (hw - 0.1f), O + d * (b.ramp_m * 0.55f) + nl * (hw + 0.5f) };
                    var p = new ScPrism { name = $"sc {b.id} outcrop slab", kind = "rock", origin = new[] { O.x, O.y }, axis = new[] { d.x, d.y }, profile = pr, colour = new[] { Granite.r, Granite.g, Granite.b }, skirt_edges = new[] { 1, 2, 4, 5 },
                                          poly = poly.SelectMany(q => new[] { q.x, q.y }).ToArray() };
                    BuildPrism(p, terrain, mats, jr);
                    // a few lower rocks of the same outcrop showing through the ground either side (look only, 3.5 m and more off the line)
                    for (int i = 0; i < 5; i++)
                    {
                        float side = i % 2 == 0 ? 1f : -1f, s = 1f + i * 2.4f, r = 0.5f + 0.5f * Hash01(i, 941);
                        var q = O + d * s + nl * side * (hw + 1.4f + 1.2f * Hash01(i, 943)); float g = Ground(terrain, q.x, q.y);
                        look.Tint = Shade(Granite, 0.8f + 0.2f * Hash01(i, 945));
                        var cc = new Vector3(q.x, g - 0.1f, q.y);
                        Vector3[] dirs = { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };
                        var apex = cc + Vector3.up * r * 0.8f;
                        for (int j = 0; j < 4; j++) TriFace(look, cc + dirs[j] * r * 1.3f, cc + dirs[(j + 1) % 4] * r * 1.3f, apex, dirs[j] + dirs[(j + 1) % 4] + Vector3.up);
                    }
                    Debug.Log($"[Round19] {b.id} outcrop: a granite slab {b.ramp_m:0.0} m from {y0:0.00} m to the lip at {y1:0.00} m ({b.angle_deg:0} degrees), {b.width + 1f:0.0} m at its widest");
                }
                else if (b.look == "outfall")
                {
                    // Round 20 (BUG-002): P1's outfall apron, a concrete slab from the pipe cut's mouth out into the pond's edge, its
                    // end turned up (a spillway lip); sloped concrete sides and back into the water (a vehicle in the pond rides up
                    // them, never into a wall); creek rock heaped either side of it in the shallows (look only)
                    var p = Prism("apron", KickerProfile(), b.ramp_m, hw, new Color(0.58f, 0.57f, 0.54f), new[] { 1, 2, 3 });
                    BuildPrism(p, terrain, mats, jr);
                    for (int i = 0; i < 8; i++)
                    {
                        float side = i % 2 == 0 ? 1f : -1f, s = 0.5f + (i / 2) * 2f, r = 0.45f + 0.4f * Hash01(i, 961);
                        var q = O + d * s + nl * side * (hw + 0.6f + 0.8f * Hash01(i, 963)); float g = Ground(terrain, q.x, q.y);
                        look.Tint = Shade(Granite, 0.75f + 0.2f * Hash01(i, 965));
                        var cc = new Vector3(q.x, g - 0.1f, q.y);
                        Vector3[] dirs = { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };
                        var apex = cc + Vector3.up * (r + Mathf.Max(0f, DeckTop(s) - g) * 0.6f);
                        for (int k2 = 0; k2 < 4; k2++) TriFace(look, cc + dirs[k2] * r * 1.4f, cc + dirs[(k2 + 1) % 4] * r * 1.4f, apex, dirs[k2] + dirs[(k2 + 1) % 4] + Vector3.up);
                    }
                    Debug.Log($"[Round20] {b.id} outfall: a concrete apron {b.ramp_m:0.0} m from {y0:0.00} m to the lip at {y1:0.00} m ({b.angle_deg:0} degrees), {b.width:0.0} m wide; off the lip at {b.vmin:0.0} to {b.vmax:0} m/s");
                    float DeckTop(float s) => Profile(KickerProfile(), s);
                }
                if (look.Count > 0) { var lg = look.ToObject(b.id + " look", mats.House, jr, false); lg.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On; }
                // the runtime side: the launch speed cap
                var bj = jr.gameObject.AddComponent<BigJump>(); bj.id = b.id; bj.look = b.look; bj.lip = new Vector3(L.x, y1, L.y); bj.dir = new Vector3(d.x, 0f, d.y); bj.halfWidth = hw; bj.vmax = b.vmax; bj.angle = b.angle_deg; bj.vmin = b.vmin;
            }
        }
    }
}
