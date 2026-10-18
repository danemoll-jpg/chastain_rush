using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Round 41 (B, Dan 2026-10-17 21:17: "I asked for an obstacle to make it so that you it hit right and there was no obstacle, you even clip
// through the roof of the house that you land on. Could easily put a second ramp on the house you pass through the roof of at a slightly
// different angle to add challenge"): the reverse Chastain Ridge's crest cut comes down off the slope onto house 1092's roof. Its roof was
// drawn but not solid (a box to its eaves: a vehicle sank 2.4 m into it); now the house's own mesh is its collider (the box stays for its
// walls), and on it stands a roofer's ramp of boards on shingle bundles, set at an angle to the line down the slope (its numbers in
// Data/tracks/chastain_ridge.json "roof_ramp", Tools/mapgen/make_ridge_route.py): a real jump off it (physics only) past CP4.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class RidgeRoofRamp { public int house; public float[] foot, dir, profile; public float length, width; }
        static MeshBuilder RoofRampHouse;   // that house's own mesh (BuildRest), for the ramp's supports (the roof's height under them)
        static int RoofRampHouseIndex => RidgeNow?.Data?.roof_ramp != null && RidgeNow.Data.roof_ramp.length > 0f ? RidgeNow.Data.roof_ramp.house : -1;

        // in BuildRest: the house built into a mesh of its own (appended to its chunk as any other) and that mesh its collider ("sc": a ramp surface)
        static void SolidRoofHouse(MeshBuilder chunk, HouseSpec h, Transform root)
        {
            var tm = new MeshBuilder(); BuildHouse(tm, h); Append(tm, chunk); RoofRampHouse = tm;
            var go = new GameObject("sc roof house " + h.index); go.transform.SetParent(root, false); AddCollider(go, tm, "sc roof house " + h.index + " collider");
            float top = float.MinValue; foreach (var v in tm.Verts) top = Mathf.Max(top, v.y);
            Debug.Log($"[Round41] B: house {h.index}'s roof solid (its own mesh as its collider, {tm.Tris.Count / 3} triangles, up to {top:0.00} m; its box to the eaves kept)");
        }

        // the highest up-facing surface of a mesh over a point (none: NaN)
        static float RoofAt(MeshBuilder m, Vector2 q)
        {
            float best = float.NaN; var v = m.Verts; var t = m.Tris;
            for (int i = 0; i + 2 < t.Count; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                if (Mathf.Abs(Vector3.Cross(b - a, c - a).normalized.y) < .2f) continue;
                float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z); if (Mathf.Abs(d) < 1e-6f) continue;
                float l1 = ((b.z - c.z) * (q.x - c.x) + (c.x - b.x) * (q.y - c.z)) / d, l2 = ((c.z - a.z) * (q.x - c.x) + (a.x - c.x) * (q.y - c.z)) / d, l3 = 1f - l1 - l2;
                if (l1 < -1e-4f || l2 < -1e-4f || l3 < -1e-4f) continue;
                float y = l1 * a.y + l2 * b.y + l3 * c.y; if (float.IsNaN(best) || y > best) best = y;
            }
            return best;
        }

        static void BuildRoofRamp(TerrainGrid terrain, Materials mats, Transform root, Ridge ridge)
        {
            var rr = ridge.Data.roof_ramp; if (rr == null || rr.length <= 0f) return;
            if (RoofRampHouse == null) { Debug.LogWarning($"[Round41] B: house {rr.house} was not built with its solid roof: no roofer's ramp"); return; }
            var O = new Vector2(rr.foot[0], rr.foot[1]); var U = new Vector2(rr.dir[0], rr.dir[1]).normalized; var N = new Vector2(-U.y, U.x); float hw = rr.width * .5f;
            var poly = new[] { O + N * hw, O - N * hw, O + U * rr.length - N * hw, O + U * rr.length + N * hw };
            BuildPrism(new ScPrism { name = "sc roofer's ramp", kind = "deck", origin = new[] { O.x, O.y }, axis = new[] { U.x, U.y }, profile = rr.profile, colour = new[] { .64f, .52f, .36f },
                                     poly = new[] { poly[0].x, poly[0].y, poly[1].x, poly[1].y, poly[2].x, poly[2].y, poly[3].x, poly[3].y } }, terrain, mats, root);
            // the supports: shingle bundles stacked from the roof (or the terrace) up under the boards, 2x4 posts at the lip; a few loose bundles
            var mb = new MeshBuilder(); var u3 = new Vector3(U.x, 0f, U.y); int bundles = 0, stacks = 0;
            Color[] wrap = { new Color(.30f, .29f, .28f), new Color(.36f, .27f, .20f), new Color(.72f, .64f, .48f) };
            float Base(Vector2 q) { float r = RoofAt(RoofRampHouse, q), g = Ground(terrain, q.x, q.y); return float.IsNaN(r) ? g : Mathf.Max(r, g); }
            foreach (float s in new[] { 1.6f, 4.0f, 6.6f, 9.0f, 11.3f })
                foreach (float side in new[] { -1f, 1f })
                {
                    var q = O + U * s + N * side * (hw - .5f); float top = Profile(rr.profile, s) - .25f, y = Base(q);
                    if (top - y < .1f) continue;
                    int k = 0; float y0 = y;
                    while (y < top - .02f) { float h = Mathf.Min(.13f, top - y); mb.Tint = wrap[(k + (side > 0 ? 1 : 0)) % 3]; SBox(mb, new Vector3(q.x, y, q.y), (k & 1) == 0 ? u3 : new Vector3(-U.y, 0f, U.x), new Vector3(.34f, h, .92f), mb.Tint); y += h; k++; bundles++; }
                    var c = new GameObject("roofer's ramp support"); c.transform.SetParent(root, false); c.transform.SetPositionAndRotation(new Vector3(q.x, (y0 + top) * .5f, q.y), Quaternion.LookRotation(u3));
                    c.AddComponent<BoxCollider>().size = new Vector3(.92f, top - y0, .92f); stacks++;
                }
            foreach (float side in new[] { -1f, 1f })
            {
                var q = O + U * (rr.length - .15f) + N * side * (hw - .1f); float top = Profile(rr.profile, rr.length) - .25f, y = Base(q);
                mb.Tint = new Color(.70f, .58f, .40f); SBox(mb, new Vector3(q.x, y, q.y), u3, new Vector3(.09f, top - y, .09f), mb.Tint);
            }
            foreach (var (s, l) in new[] { (2.5f, 2.6f), (5.2f, -2.4f), (3.6f, -3.0f) })
            {
                var q = O + U * s + N * l; float y = Base(q); mb.Tint = wrap[bundles % 3]; SBox(mb, new Vector3(q.x, y, q.y), u3, new Vector3(.34f, .13f, .92f), mb.Tint); bundles++;
            }
            mb.ToObject("Roofer's ramp supports", mats.House, root, false);
            Debug.Log($"[Round41] B: the roofer's ramp on house {rr.house}: {rr.length:0} m of boards, {rr.width:0.0} m wide, from {Profile(rr.profile, 0f):0.00} m on the terrace to its lip at {Profile(rr.profile, rr.length):0.00} m " +
                      $"(the roof {RoofAt(RoofRampHouse, O + U * rr.length):0.00} m under it), heading {Mathf.Atan2(U.x, U.y) * Mathf.Rad2Deg:0} deg; {stacks} stacks of {bundles} shingle bundles");
        }
    }
}
