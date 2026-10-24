using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 60 (Part A): Town Center's inside (MapBuilder.Mall.cs has the plan and the outside). From Dan's photos, generically decorated:
//   the floors: cream tile with small dark diamonds and red-brown bands across the concourse, the center court's green-and-white checkerboard,
//     the shops' own floors; two slabs (the lower floor, the upper floor with its holes), cream fascias and ceilings, the skylights over the holes;
//   the holes in the upper floor: the two escalator banks (drivable ramps, 14 degrees, glass balustrades), the center court (open to both levels:
//     its two ends' glass railings break, a drop of 7.5 m onto the court; long sides railed), the main entrance's two-storey hall;
//   wood-capped glass railings, cream columns with mahogany capitals (ball tops upstairs);
//   the shops: along both sides of the concourse and the two wings, on both levels, about 12 m wide, glass fronts that break, a lit name board,
//     breakable drywall between neighbors, their things inside (clothes racks, mannequins, shelves, tables, counters, arcade cabinets...);
//   the anchors' fronts at the ends (closed grilles, their names), the east anchor's upper floor open: a department store to drive through and out;
//   kiosks, benches, planters, trash cans, directory boards on the concourses; the food court upstairs (counters, tables and chairs) under the
//     pyramid; the play area under it;
//   every smashable thing is a breakable that costs no speed (FenceChunk.free) and comes back (TownCenterMall), the shoppers (AmbientLife).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        sealed class MallShop { public Rect r; public char front; public int level, sign, k, n; public float floor, ceil; }
        sealed class MallBreak { public Breakables glass = new(), wood = new(), iron = new(), sign = new(), foam = new(), doors = new(); }   // Round 64: doors, the doors' and fronts' panes (see-through glass)

        static void BuildMallInside(MapData map, TerrainGrid t, Materials mats, Transform root, MallPlan m, Material signs, MeshBuilder shellIn)
        {
            var floorMb = new MeshBuilder(); var pattern = new MeshBuilder(); var deco = new MeshBuilder(); var lights = new MeshBuilder();
            var br = new MallBreak();
            MallFloors(m, root, floorMb, pattern, deco);
            MallRamps(m, root, deco);
            MallRailings(m, root, deco, br);
            MallColumns(m, root, deco);
            var shops = MallShops(m);
            foreach (var s in shops) MallShopBuild(m, root, deco, lights, br, s, signs);
            MallAnchorFronts(m, root, deco, br, signs);
            MallEastStore(m, root, deco, lights, br, signs);
            MallAnchorStores(m, root, deco, lights, br, signs);   // Round 64: Halverson Home and Carroway, driven through (MapBuilder.MallShortcut.cs)
            MallDoorsGlass(m, br);
            MallConcourse(m, root, deco, br, signs);
            BuildMallFoodCourt(m, root, deco, br, signs);
            MallPlayArea(m, root, deco, br, signs);
            MallRaceFeatures(m, root, deco, br);   // Round 62: the raffle stage at the east escalator's top, the mattress mountain in Halverson's aisle (MapBuilder.MallRace.cs)
            MallCeilingLights(m, lights);
            MallShoppers(m);
            MallReverb(m, root);
            floorMb.ToObject("MallFloors", mats.House, root, false, false);
            pattern.ToObject("MallFloorPattern", mats.Sidewalk, root, false, false);   // drawn over the floor (the sidewalk material's depth offset)
            deco.ToObject("MallInside", mats.House, root, false, false);
            var lmat = AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir + "/MallLight.mat");
            if (lmat == null || QuickStep != null) { var nm = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "MallLight" }; nm.SetColor("_BaseColor", new Color(1f, .97f, .9f)); lmat = GenAsset(nm, GeneratedDir + "/MallLight.mat"); }
            lights.ToObject("MallLights", lmat, root, false, false);
            foreach (var (b, name, drop, sound) in new[] { (br.glass, "MallGlass", .3f, BreakSounds.Kind.Glass), (br.wood, "MallWood", .35f, BreakSounds.Kind.Wood), (br.iron, "MallMetal", .35f, BreakSounds.Kind.Iron),
                (br.sign, "MallSigns", .4f, BreakSounds.Kind.Sign), (br.foam, "MallSoft", .3f, BreakSounds.Kind.Wood), (br.doors, "MallDoorGlass", .3f, BreakSounds.Kind.Glass) })
            {
                if (b.Count == 0) continue;
                var tr = b.ToObjects(name, b == br.doors ? MallSeeThrough("MallDoorGlass", new Color(.62f, .75f, .80f, .28f), .92f) : mats.House, root, drop, sound);   // Round 64 (Dan, BUG-002: "these should be glass doors")
                foreach (var c in tr.GetComponentsInChildren<FenceChunk>()) { c.free = true; m.Breakables.Add(c); }   // no speed lost (Dan: "breaking them must not stop a car")
            }
            m.Log.Add($"{shops.Count} shops ({shops.Count(s => s.level == 0)} lower, {shops.Count(s => s.level == 1)} upper); breakables: glass {br.glass.Count}, wood {br.wood.Count}, metal {br.iron.Count}, signs {br.sign.Count}, soft {br.foam.Count}");
        }

        // ------------------------------------------------------------ small helpers
        static void MQuad(MeshBuilder mb, MallPlan m, float u0, float v0, float u1, float v1, float y, Color c) { mb.Tint = c; Face(mb, m.P(u0, v0, y), m.P(u1, v0, y), m.P(u1, v1, y), m.P(u0, v1, y), Vector3.up); }
        // a breakable thing: its pieces drawn by 'draw' (one piece each), its trigger box w (across 'fwd'), h, d (along 'fwd') from 'at' up
        static void MItem(Breakables b, Vector3 at, Vector3 fwd, float w, float h, float d, params Action<MeshBuilder>[] draw)
        {
            var mb = b.Begin(new Vector2(at.x, at.z));
            foreach (var dr in draw) { var x = dr; b.Piece(() => x(mb), at.y + .05f); }
            b.End(at + Vector3.up * Mathf.Max(h, .8f) * .5f, fwd.sqrMagnitude > 1e-6f ? fwd.normalized : Vector3.forward, Mathf.Max(w, .6f), Mathf.Max(h, .8f), Mathf.Max(d, .6f));
        }
        static List<Rect> MinusRects(Rect r, IEnumerable<Rect> holes)
        {
            var list = new List<Rect> { r };
            foreach (var h in holes)
            {
                var next = new List<Rect>();
                foreach (var a in list)
                {
                    if (!a.Overlaps(h) || a.xMax <= h.xMin || a.xMin >= h.xMax || a.yMax <= h.yMin || a.yMin >= h.yMax) { next.Add(a); continue; }
                    if (a.xMin < h.xMin) next.Add(Rect.MinMaxRect(a.xMin, a.yMin, h.xMin, a.yMax));
                    if (a.xMax > h.xMax) next.Add(Rect.MinMaxRect(h.xMax, a.yMin, a.xMax, a.yMax));
                    float x0 = Mathf.Max(a.xMin, h.xMin), x1 = Mathf.Min(a.xMax, h.xMax);
                    if (a.yMin < h.yMin) next.Add(Rect.MinMaxRect(x0, a.yMin, x1, h.yMin));
                    if (a.yMax > h.yMax) next.Add(Rect.MinMaxRect(x0, h.yMax, x1, a.yMax));
                }
                list = next;
            }
            return list.Where(q => q.width > .01f && q.height > .01f).ToList();
        }
        static Rect R(float u0, float v0, float u1, float v1) => Rect.MinMaxRect(u0, v0, u1, v1);
        // the concourse areas (where the tile pattern is) per level
        static List<Rect> MallWalkAreas(int level)
        {
            var l = new List<Rect> { R(MallUW, MallCS, MallUE, MallCN), MallCourtArea, MallNorthWing, MallNWWing };
            if (level == 0) { l.Add(R(-141f, -1292f, -68f, -1270f)); l.Add(R(-57f, -1292f, -45f, -1267.5f)); l.Add(R(-201f, -1340.2f, -189f, -1315f)); }
            else { l.Add(R(4f, -1339.1f, 16f, -1315f)); l.Add(R(-223.6f, -1248f, -203f, -1236f)); l.Add(R(-57f, -1292f, -45f, -1286f)); }
            return l;
        }

        // ------------------------------------------------------------ the floors
        static void MallFloors(MallPlan m, Transform root, MeshBuilder floorMb, MeshBuilder pattern, MeshBuilder deco)
        {
            var lower = MallRaster(m, m.SpineRing, .5f, (u, v) => true);
            foreach (var r in lower) MallSlab(m, floorMb, floorMb, root, r, m.L0, MallPlan.Slab, MTile, MTile, "Mall floor", true);
            var upper = MallRaster(m, m.SpineRing, .5f, (u, v) => !m.InVoid(u, v));
            foreach (var r in upper) MallSlab(m, floorMb, m.Ceil, root, r, m.L1, MallPlan.Slab, MTile, MCream, "Mall floor", false);
            var east = MallRasterRing(m, m.Rings[m.East], .5f, (u, v) => true);
            foreach (var r in east) MallSlab(m, floorMb, floorMb, root, r, m.L1, MallPlan.Slab, SC(.84f, .82f, .78f), MCream, "Mall floor", true);
            // the fascias round the holes (cream, 1.3 m deep under the slab's edge), not where a ramp's top meets the floor
            foreach (var v in m.Voids)
            {
                var rampTop = m.Ramps.FirstOrDefault(rp => Mathf.Abs(rp.U0 - v.xMin) < .01f && Mathf.Abs(rp.U1 - v.xMax) < .01f);
                var edges = new List<(float u0, float v0, float u1, float v1, Vector3 n)> { (v.xMin, v.yMin, v.xMax, v.yMin, m.D3(0, 1)), (v.xMin, v.yMax, v.xMax, v.yMax, m.D3(0, -1)), (v.xMin, v.yMin, v.xMin, v.yMax, m.D3(1, 0)), (v.xMax, v.yMin, v.xMax, v.yMax, m.D3(-1, 0)) };
                foreach (var e in edges)
                {
                    if (rampTop != null && Mathf.Abs(e.u0 - e.u1) < .01f && Mathf.Abs(e.u0 - rampTop.uTop) < .01f) continue;
                    if (v == m.Tower && Mathf.Abs(e.v0 - e.v1) < .01f && Mathf.Abs(e.v0 - v.yMax) < .01f) continue;   // the outer wall
                    deco.Tint = MCream; Face(deco, m.P(e.u0, e.v0, m.L1 - 1.3f), m.P(e.u1, e.v1, m.L1 - 1.3f), m.P(e.u1, e.v1, m.L1 + .01f), m.P(e.u0, e.v0, m.L1 + .01f), -e.n);
                    deco.Tint = MMahogany; var o = -e.n * .03f; Face(deco, m.P(e.u0, e.v0, m.L1 - 1.32f) + o, m.P(e.u1, e.v1, m.L1 - 1.32f) + o, m.P(e.u1, e.v1, m.L1 - 1.18f) + o, m.P(e.u0, e.v0, m.L1 - 1.18f) + o, -e.n);
                }
            }
            // the tile pattern on the concourses: red-brown bands across every 14 m, small dark diamonds on a 2.4 m grid
            for (int level = 0; level < 2; level++)
            {
                float y = m.Level(level) + .012f; var holes = level == 1 ? m.Voids : new List<Rect>();
                foreach (var a in MallWalkAreas(level))
                {
                    bool alongU = a.width >= a.height;
                    float s0 = alongU ? a.xMin : a.yMin, s1 = alongU ? a.xMax : a.yMax;
                    for (float s = Mathf.Ceil(s0 / 14f) * 14f; s < s1 - 2.4f; s += 14f)
                    {
                        var band = alongU ? R(s, a.yMin + 1.2f, s + 2.4f, a.yMax - 1.2f) : R(a.xMin + 1.2f, s, a.xMax - 1.2f, s + 2.4f);
                        if (level == 0 && MallCourtArea.Overlaps(band) && a != MallCourtArea) continue;
                        foreach (var q in MinusRects(band, holes)) MQuad(pattern, m, q.xMin, q.yMin, q.xMax, q.yMax, y, MTileRed);
                    }
                    for (float u = Mathf.Ceil(a.xMin / 2.4f) * 2.4f; u < a.xMax - .5f; u += 2.4f)
                        for (float v = Mathf.Ceil(a.yMin / 2.4f) * 2.4f; v < a.yMax - .5f; v += 2.4f)
                        {
                            if (level == 1 && m.InVoid(u, v, .4f)) continue; if (level == 0 && m.Court.Contains(new Vector2(u, v))) continue;
                            pattern.Tint = MDiamond; var c = m.P(u, v, y + .002f);
                            Face(pattern, c + m.D3(1, 0) * .22f, c + m.D3(0, 1) * .22f, c - m.D3(1, 0) * .22f, c - m.D3(0, 1) * .22f, Vector3.up);
                        }
                }
            }
            // the center court's checkerboard (lower), a black border round it
            {
                var c = m.Court; float y = m.L0 + .014f;
                MQuad(pattern, m, c.xMin + .6f, c.yMin + .4f, c.xMax - .6f, c.yMax - .4f, y - .001f, SC(.06f, .07f, .07f));
                for (float u = c.xMin + 1.2f; u < c.xMax - 1.19f; u += 1.2f)
                    for (float v = c.yMin + 1.0f; v < c.yMax - .99f; v += 1.2f)
                    {
                        bool dark = (Mathf.RoundToInt((u - c.xMin) / 1.2f) + Mathf.RoundToInt((v - c.yMin) / 1.2f)) % 2 == 0;
                        MQuad(pattern, m, u, v, Mathf.Min(u + 1.2f, c.xMax - 1.2f), Mathf.Min(v + 1.2f, c.yMax - 1.0f), y, dark ? SVary(MCheckDark, u, v, .08f) : SVary(MCheckLight, u, v, .03f));
                    }
            }
        }

        // ------------------------------------------------------------ the escalator banks: drivable ramps (14 degrees) with glass balustrades
        static void MallRamps(MallPlan m, Transform root, MeshBuilder deco)
        {
            foreach (var rp in m.Ramps)
            {
                var col = new MeshBuilder(); float run = rp.run, w = rp.v1 - rp.v0;
                float Y(float s) => m.L0 + Interp(rp.prof, Mathf.Clamp(s, 0f, run));
                float Uat(float s) => rp.uFoot + rp.dir * s;
                var outward = new Func<float, Vector3>(s => m.D3(rp.dir, 0));
                const float step = .45f; int n = Mathf.CeilToInt(run / step);
                for (int i = 0; i < n; i++)
                {
                    float sa = run * i / n, sb = run * (i + 1) / n; float ua = Uat(sa), ub = Uat(sb), ya = Y(sa), yb = Y(sb);
                    Vector3 l0 = m.P(ua, rp.v0, ya), r0 = m.P(ua, rp.v1, ya), l1 = m.P(ub, rp.v0, yb), r1 = m.P(ub, rp.v1, yb);
                    var up = Vector3.Cross(r0 - l0, l1 - l0); if (up.y < 0) up = -up;
                    bool comb = sa < .9f || sb > run - .9f;
                    var c = comb ? (i % 2 == 0 ? SC(.85f, .70f, .12f) : SC(.55f, .56f, .58f)) : (i % 2 == 0 ? SC(.20f, .21f, .22f) : SC(.15f, .16f, .17f));
                    deco.Tint = c; Face(deco, l0, r0, r1, l1, up); col.Tint = c; Face(col, l0, r0, r1, l1, up);
                    // yellow edge lines along both sides of the steps
                    foreach (float v in new[] { rp.v0 + .12f, rp.v1 - .32f }) { deco.Tint = SC(.92f, .76f, .10f); Face(deco, m.P(ua, v, ya + .01f), m.P(ua, v + .2f, ya + .01f), m.P(ub, v + .2f, yb + .01f), m.P(ub, v, yb + .01f), up); }
                    // the sides: brushed steel down to the lower floor
                    foreach (var (v, sgn) in new[] { (rp.v0, -1f), (rp.v1, 1f) })
                    {
                        var a = m.P(ua, v, ya); var b = m.P(ub, v, yb); var a0 = m.P(ua, v, m.L0 - .02f); var b0 = m.P(ub, v, m.L0 - .02f);
                        deco.Tint = MSteel; Face(deco, a0, b0, b, a, m.D3(0, sgn)); col.Tint = MSteel; Face(col, a0, b0, b, a, m.D3(0, sgn));
                    }
                }
                // the foot's end face is open (it meets the floor), the top end meets the upper floor
                var mc = ColliderMeshMall(col, $"Mall ramp {rp.name}", root);
                // the balustrades: glass on a steel skirt, the black handrail, both sides; colliders along the slope (2 m pieces)
                foreach (var (v, sgn) in new[] { (rp.v0 + .1f, -1f), (rp.v1 - .1f, 1f) })
                {
                    for (float s = 0f; s < run - .01f; s += 2f)
                    {
                        float s2 = Mathf.Min(run, s + 2f); var a = m.P(Uat(s), v, Y(s)); var b = m.P(Uat(s2), v, Y(s2));
                        deco.Tint = SC(.62f, .70f, .72f); Face(deco, a + Vector3.up * .15f, b + Vector3.up * .15f, b + Vector3.up * 1.0f, a + Vector3.up * 1.0f, m.D3(0, sgn)); Face(deco, a + Vector3.up * .15f, b + Vector3.up * .15f, b + Vector3.up * 1.0f, a + Vector3.up * 1.0f, m.D3(0, -sgn));
                        deco.Tint = MSteel; Face(deco, a, b, b + Vector3.up * .15f, a + Vector3.up * .15f, m.D3(0, -sgn));
                        SRail(deco, a + Vector3.up * 1.05f, b + Vector3.up * 1.05f, .07f, SC(.06f, .06f, .07f));
                        var mid = (a + b) * .5f + Vector3.up * .55f; var g = new GameObject("Mall ramp rail"); g.transform.SetParent(root, false);
                        g.transform.SetPositionAndRotation(mid, Quaternion.LookRotation(b - a, Vector3.up)); g.AddComponent<BoxCollider>().size = new Vector3(.18f, 1.1f, (b - a).magnitude + .05f);
                    }
                    // the newel ends at the foot and the top (the escalator's rounded ends)
                    foreach (float s in new[] { 0f, run }) SBox(deco, m.P(Uat(s), v, Y(s)), m.D3(rp.dir, 0), new Vector3(.25f, 1.1f, 1.2f), SC(.08f, .08f, .09f));
                }
                m.Log.Add($"escalator bank {rp.name}: {run:0.0} m from u {rp.uFoot:0.0} ({m.L0:0.00} m) to u {rp.uTop:0.0} ({m.L1:0.00} m), {w:0.0} m wide");
            }
        }
        static Collider ColliderMeshMall(MeshBuilder col, string name, Transform parent)
        {
            if (col.Count == 0) return null;
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            var mesh = col.ToMesh(name + " collider"); mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/Mall_{name.Replace(' ', '_')}_{parent.childCount}.asset");
            var mc = g.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; return mc;
        }

        // ------------------------------------------------------------ the railings round the holes (upper floor)
        static void MallRailing(MallPlan m, Transform root, MeshBuilder deco, MallBreak br, float u0, float v0, float u1, float v1, Vector3 intoVoid, bool breakable)
        {
            var a = m.P(u0, v0, m.L1); var b = m.P(u1, v1, m.L1); float len = Vector3.Distance(a, b); if (len < .1f) return;
            if (!breakable)
            {
                int posts = Mathf.Max(1, Mathf.RoundToInt(len / 1.6f));
                for (int k = 0; k <= posts; k++) { var p = Vector3.Lerp(a, b, k / (float)posts); SBox(deco, p, b - a, new Vector3(.07f, 1.05f, .07f), MBronze); }
                deco.Tint = SC(.66f, .74f, .76f); Face(deco, a + Vector3.up * .12f, b + Vector3.up * .12f, b + Vector3.up * .95f, a + Vector3.up * .95f, intoVoid); Face(deco, a + Vector3.up * .12f, b + Vector3.up * .12f, b + Vector3.up * .95f, a + Vector3.up * .95f, -intoVoid);
                SRail(deco, a + Vector3.up * 1.02f, b + Vector3.up * 1.02f, .08f, MMahogany); SRail(deco, a + Vector3.up * .08f, b + Vector3.up * .08f, .05f, MBronze);
                var mid = (a + b) * .5f + Vector3.up * .6f; var g = new GameObject("Mall railing"); g.transform.SetParent(root, false);
                g.transform.SetPositionAndRotation(mid, Quaternion.LookRotation(b - a)); g.AddComponent<BoxCollider>().size = new Vector3(.2f, 1.2f, len);
                return;
            }
            // the court's ends: glass panels that break (the way down into the court)
            int panes = Mathf.Max(1, Mathf.RoundToInt(len / 1.5f));
            for (int k = 0; k < panes; k++)
            {
                var p0 = Vector3.Lerp(a, b, k / (float)panes); var p1 = Vector3.Lerp(a, b, (k + 1) / (float)panes); var mid = (p0 + p1) * .5f;
                MItem(br.glass, mid, intoVoid, (p1 - p0).magnitude, 1.1f, 1.4f,
                    mb => { mb.Tint = SC(.66f, .74f, .76f); Face(mb, p0 + Vector3.up * .12f, p1 + Vector3.up * .12f, p1 + Vector3.up * .95f, p0 + Vector3.up * .95f, intoVoid); Face(mb, p0 + Vector3.up * .12f, p1 + Vector3.up * .12f, p1 + Vector3.up * .95f, p0 + Vector3.up * .95f, -intoVoid); },
                    mb => { SRail(mb, p0 + Vector3.up * 1.02f, p1 + Vector3.up * 1.02f, .08f, MMahogany); SBox(mb, p0, p1 - p0, new Vector3(.07f, 1.05f, .07f), MBronze); });
            }
        }
        static void MallRailings(MallPlan m, Transform root, MeshBuilder deco, MallBreak br)
        {
            foreach (var rp in m.Ramps)
            {
                MallRailing(m, root, deco, br, rp.U0, rp.v0 - .1f, rp.U1, rp.v0 - .1f, m.D3(0, 1), false);
                MallRailing(m, root, deco, br, rp.U0, rp.v1 + .1f, rp.U1, rp.v1 + .1f, m.D3(0, -1), false);
                float fu = rp.uFoot - rp.dir * .1f; MallRailing(m, root, deco, br, fu, rp.v0, fu, rp.v1, m.D3(rp.dir, 0), rp.name == "east");   // across the foot's end (Round 62: the east one breaks, the raffle stage's long landings cross it)
            }
            var c = m.Court;
            MallRailing(m, root, deco, br, c.xMin, c.yMin - .1f, c.xMax, c.yMin - .1f, m.D3(0, 1), false);
            MallRailing(m, root, deco, br, c.xMin, c.yMax + .1f, c.xMax, c.yMax + .1f, m.D3(0, -1), false);
            MallRailing(m, root, deco, br, c.xMin - .1f, c.yMin, c.xMin - .1f, c.yMax, m.D3(1, 0), true);
            MallRailing(m, root, deco, br, c.xMax + .1f, c.yMin, c.xMax + .1f, c.yMax, m.D3(-1, 0), true);
            var tw = m.Tower; MallRailing(m, root, deco, br, tw.xMin, tw.yMin - .1f, tw.xMax, tw.yMin - .1f, m.D3(0, 1), true);   // Round 62: the balcony's glass breaks (the drop into the entrance hall)
        }

        // ------------------------------------------------------------ the columns: at the holes' corners and along their long sides, floor to roof
        static void MallColumn(MallPlan m, Transform root, MeshBuilder deco, float u, float v, bool upperOnly = false)
        {
            var b = m.P(u, v, (upperOnly ? m.L1 : m.L0) - .02f); float top = m.Roof - MallPlan.Slab; var f = m.D3(1, 0);
            SBox(deco, b, f, new Vector3(.9f, top - b.y, .9f), MCream);
            foreach (float y in upperOnly ? new[] { m.L1 } : new[] { m.L0, m.L1 }) SBox(deco, m.P(u, v, y), f, new Vector3(.98f, 1.0f, .98f), y < m.L1 - 1f ? SC(.36f, .40f, .37f) : MCreamDark);   // the bases (green stone below)
            foreach (float y in upperOnly ? new[] { top - 1.9f } : new[] { m.L1 - 2.4f, top - 1.9f }) { SBox(deco, m.P(u, v, y), f, new Vector3(1.15f, .7f, 1.15f), MMahogany); SBox(deco, m.P(u, v, y + .7f), f, new Vector3(1.3f, .18f, 1.3f), MMahogany * .85f); }
            SBox(deco, m.P(u, v, top - 1.0f), f, new Vector3(.75f, .75f, .75f), MCream);   // the ball (a squared one)
            var g = new GameObject("Mall column"); g.transform.SetParent(root, false);
            g.transform.SetPositionAndRotation((b + m.P(u, v, top)) * .5f, Quaternion.LookRotation(f)); g.AddComponent<BoxCollider>().size = new Vector3(.9f, top - b.y, .9f);
        }
        static void MallColumns(MallPlan m, Transform root, MeshBuilder deco)
        {
            // floor to roof at the escalator holes' corners only; along their long sides upstairs only (at the railing, as the photos'), so the lower floor's
            // lanes beside the holes have nothing a car stops dead on (the first check: cars stopped on the court's side columns, the bikes squeezed by)
            var at = new List<(Vector2 p, bool upper)>();
            foreach (var v in m.Voids)
            {
                if (v == m.Tower) continue;
                foreach (float vv in new[] { v.yMin - .45f, v.yMax + .45f })
                {
                    int k = Mathf.Max(1, Mathf.RoundToInt(v.width / 13f));
                    for (int i = 0; i <= k; i++) { float u = Mathf.Lerp(v.xMin + .45f, v.xMax - .45f, i / (float)k); at.Add((new Vector2(u, vv), (i > 0 && i < k) || v == m.Court)); }   // the court's corners too: they stood in the way from the main doors
                }
            }
            foreach (var (p, upper) in at) MallColumn(m, root, deco, p.x, p.y, upper);
            m.Log.Add($"{at.Count} columns ({at.Count(a => !a.upper)} floor to roof at the holes' corners)");
        }

        // ------------------------------------------------------------ the shops
        static List<MallShop> MallShops(MallPlan m)
        {
            var bands = new List<(Rect r, char front, int level)>();
            void Both(Rect r, char f) { bands.Add((r, f, 0)); bands.Add((r, f, 1)); }
            // south of the concourse (front north)
            bands.Add((R(MallUW, -1340.2f, -201f, MallCS), 'N', 0)); bands.Add((R(-189f, -1340.2f, -141f, MallCS), 'N', 0)); bands.Add((R(-70f, -1339.1f, MallUE, MallCS), 'N', 0));
            bands.Add((R(MallUW, -1340.2f, -141f, MallCS), 'N', 1)); bands.Add((R(-70f, -1339.1f, 4f, MallCS), 'N', 1)); bands.Add((R(16f, -1339.1f, MallUE, MallCS), 'N', 1));
            // north of it (front south)
            Both(R(MallUW, MallCN, -224f, -1268.0f), 'S'); Both(R(-165.5f, MallCN, -141f, -1267.7f), 'S'); Both(R(-68f, MallCN, -57f, -1267.5f), 'S'); Both(R(-45f, MallCN, -34.8f, -1267.5f), 'S');
            Both(R(40.7f, MallCN, MallUE, -1268.3f), 'S');
            // the lower bump's shops (front south, onto the plaza under the food court)
            bands.Add((R(-141f, -1270f, -68f, -1249.6f), 'S', 0));
            // the north-west wing (fronts on its corridor)
            bands.Add((R(-223.6f, MallCN, -203f, -1217.8f), 'E', 0)); bands.Add((R(-223.6f, MallCN, -203f, -1248f), 'E', 1)); bands.Add((R(-223.6f, -1236f, -203f, -1217.8f), 'E', 1));
            Both(R(-187f, MallCN, -165.9f, -1217.8f), 'W');
            // the north wing
            Both(R(-34.4f, MallCN, -7f, -1203f), 'E'); Both(R(13f, MallCN, 40.3f, -1203f), 'W');
            var shops = new List<MallShop>(); int sign = 0;
            foreach (var (r, front, level) in bands)
            {
                bool alongU = front == 'N' || front == 'S'; float len = alongU ? r.width : r.height; int n = Mathf.Max(1, Mathf.RoundToInt(len / 12f));
                for (int k = 0; k < n; k++)
                {
                    float a0 = (alongU ? r.xMin : r.yMin) + len * k / n, a1 = (alongU ? r.xMin : r.yMin) + len * (k + 1) / n;
                    var sr = alongU ? R(a0, r.yMin, a1, r.yMax) : R(r.xMin, a0, r.xMax, a1);
                    // the lower bump's shops reach back to the footprint's line (it steps)
                    if (front == 'S' && level == 0 && r.yMin == -1270f)
                    {
                        float mid = (a0 + a1) * .5f, back = -1270f; while (back < -1230f && m.InSpine(mid, back + .25f) && m.InSpine(a0 + .3f, back + .25f) && m.InSpine(a1 - .3f, back + .25f)) back += .25f;
                        sr = R(a0, -1270f, a1, back - WallTM);
                    }
                    shops.Add(new MallShop { r = sr, front = front, level = level, k = k, n = n, sign = (sign++ * 7 + level * 11) % SignShopCount, floor = m.Level(level), ceil = m.Ceiling(level) });
                }
            }
            return shops;
        }

        // the shop's frame: x along its front (0..w), d into it (0..D) from the front's line
        static (Func<float, float, float, Vector3> P, Vector3 along, Vector3 inward, float w, float D) ShopFrame(MallPlan m, MallShop s)
        {
            var r = s.r; float y0 = s.floor;
            switch (s.front)
            {
                case 'N': return ((x, d, y) => m.P(r.xMin + x, r.yMax - d, y0 + y), m.D3(1, 0), m.D3(0, -1), r.width, r.height);
                case 'S': return ((x, d, y) => m.P(r.xMax - x, r.yMin + d, y0 + y), m.D3(-1, 0), m.D3(0, 1), r.width, r.height);
                case 'E': return ((x, d, y) => m.P(r.xMax - d, r.yMax - x, y0 + y), m.D3(0, -1), m.D3(-1, 0), r.height, r.width);
                default: return ((x, d, y) => m.P(r.xMin + d, r.yMin + x, y0 + y), m.D3(0, 1), m.D3(1, 0), r.height, r.width);
            }
        }

        static void MallShopBuild(MallPlan m, Transform root, MeshBuilder deco, MeshBuilder lights, MallBreak br, MallShop s, Material signs)
        {
            var (P, along, inward, w, D) = ShopFrame(m, s); float H = s.ceil - s.floor; var outN = -inward;
            int cat = ShopCategory(s.sign); var rnd = new System.Random(s.sign * 31 + (int)(s.r.xMin * 7) + (int)(s.r.yMin * 3) + s.level);
            // the floor (its own), the ceiling's lights
            var fc = cat switch { 0 => SC(.62f, .50f, .38f), 1 => SC(.50f, .50f, .52f), 2 => SC(.90f, .88f, .86f), 3 => SC(.55f, .42f, .30f), 5 => SC(.70f, .30f, .22f), 6 => SC(.25f, .35f, .55f), 7 => SC(.12f, .12f, .16f), _ => SC(.78f, .76f, .72f) };
            deco.Tint = SVary(fc, s.r.xMin, s.r.yMin, .05f); Face(deco, P(.15f, .3f, .011f), P(w - .15f, .3f, .011f), P(w - .15f, D - .1f, .011f), P(.15f, D - .1f, .011f), Vector3.up);
            foreach (float fx in new[] { .3f, .7f }) { var lp = P(w * fx, D * .55f, H - .12f); SBox(lights, lp, inward, new Vector3(2.4f, .1f, .6f), Color.white); m.Lamps.Add(lp); }
            // the front: glass that breaks (panes about 3 m), bronze mullions on them, the name board, the bulkhead over it
            float gH = 4.0f; int panes = Mathf.Max(1, Mathf.RoundToInt((w - .6f) / 3f));
            for (int k = 0; k < panes; k++)
            {
                float x0 = .3f + (w - .6f) * k / panes, x1 = .3f + (w - .6f) * (k + 1) / panes;
                var a = P(x0, 0f, 0f); var b = P(x1, 0f, 0f); var mid = (a + b) * .5f;
                MItem(br.glass, mid, outN, x1 - x0, gH, 1.2f,
                    mb => { for (int sy = 0; sy < 2; sy++) { mb.Tint = SVary(MGlass, a.x + sy, a.z, .05f) * 1.12f; var q0 = a + Vector3.up * (gH * sy / 2f); var q1 = b + Vector3.up * (gH * sy / 2f); var h = Vector3.up * gH / 2f; Face(mb, q0, q1, q1 + h, q0 + h, outN); Face(mb, q0, q1, q1 + h, q0 + h, inward); } },
                    mb => { SBox(mb, a, along, new Vector3(.08f, gH, .14f), MBronze); SBox(mb, a + Vector3.up * (gH - .1f), along, new Vector3(.1f, .1f, (b - a).magnitude), MBronze); });
            }
            // the bulkhead from the glass's top to the ceiling (both faces), the board and the name on it
            deco.Tint = MCream; Face(deco, P(0f, 0f, gH), P(w, 0f, gH), P(w, 0f, H), P(0f, 0f, H), outN); deco.Tint = MCreamDark; Face(deco, P(0f, .15f, gH), P(w, .15f, gH), P(w, .15f, H), P(0f, .15f, H), inward);
            deco.Tint = MCream; Face(deco, P(0f, 0f, gH), P(w, 0f, gH), P(w, .15f, gH), P(0f, .15f, gH), Vector3.down);
            float bw = Mathf.Min(8.5f, w - 2f), bh = bw * .078f * 1.15f, by = gH + .65f;
            SBox(deco, P(w * .5f, -.05f, by - bh * .5f - .12f), outN, new Vector3(bw + .3f, bh + .24f, .1f), SC(.10f, .10f, .11f));
            if (signs != null) MallPlate(root, P(w * .5f, -.11f, by) , inward, bw, bw * .078f, SignShopFirst + s.sign, signs);
            // the sides: drywall that breaks toward the next shop of its row (solid above 3.6 m), nothing toward the one before (that one's drywall is
            // there), a solid wall at a row's end inside the building. x = 0 is the next shop's side for fronts facing south or east (their x runs back
            // along the row)
            bool x0IsNext = s.front == 'S' || s.front == 'E';
            foreach (var (x, sideDir) in new[] { (0f, -1f), (w, 1f) })
            {
                bool next = (x == 0f) == x0IsNext; bool has = next ? s.k < s.n - 1 : s.k > 0;
                if (has && !next) continue;
                var beyond = P(x + sideDir * .6f, D * .5f, 1f); var bu = m.UV(new Vector2(beyond.x, beyond.z));
                if (!has && !m.InSpine(bu.x, bu.y)) continue;   // the outer wall is there
                var a = P(x - sideDir * .08f, .2f, 0f); var b = P(x - sideDir * .08f, D, 0f);
                if (has)
                {
                    int pieces = Mathf.Max(1, Mathf.RoundToInt((D - .2f) / 4f));
                    for (int k = 0; k < pieces; k++)
                    {
                        var p0 = Vector3.Lerp(a, b, k / (float)pieces); var p1 = Vector3.Lerp(a, b, (k + 1) / (float)pieces); var mid = (p0 + p1) * .5f;
                        MItem(br.wood, mid, along, (p1 - p0).magnitude, 3.6f, .5f, mb => { mb.Tint = SVary(SC(.90f, .88f, .84f), p0.x, p0.z, .04f); SBox(mb, (p0 + p1) * .5f, p1 - p0, new Vector3(.14f, 1.8f, (p1 - p0).magnitude), mb.Tint); },
                            mb => { mb.Tint = SVary(SC(.90f, .88f, .84f), p1.x, p1.z, .04f); SBox(mb, (p0 + p1) * .5f + Vector3.up * 1.8f, p1 - p0, new Vector3(.14f, 1.8f, (p1 - p0).magnitude), mb.Tint); });
                    }
                    SBox(deco, (a + b) * .5f + Vector3.up * 3.6f, b - a, new Vector3(.14f, H - 3.6f, (b - a).magnitude), MCreamDark);
                }
                else MallWall(deco, deco, root, new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(along.x, along.z) * sideDir, s.floor - .02f, s.ceil, MCreamDark, false, MCreamDark, "Mall shop wall");
            }
            // things inside, by kind
            MallShopThings(m, s, P, along, inward, w, D, cat, rnd, br, deco);
        }
        // the sign rows' kinds (make_mall_signs.py SHOPS, in order): 0 clothes, 1 shoes, 2 tables, 3 shelves, 4 food, 5 counters, 6 toys, 7 arcade
        static int ShopCategory(int sign) => new[] { 0, 1, 2, 3, 3, 4, 3, 5, 0, 0, 5, 5, 6, 1, 2, 2, 0, 0, 2, 3, 3, 5, 0, 0, 0, 1, 2, 5, 3, 7 }[sign % 30];

        static void MallShopThings(MallPlan m, MallShop s, Func<float, float, float, Vector3> P, Vector3 along, Vector3 inward, float w, float D, int cat, System.Random rnd, MallBreak br, MeshBuilder deco)
        {
            Color Rand(params Color[] c) => c[rnd.Next(c.Length)];
            var cloth = new[] { SC(.80f, .20f, .25f), SC(.20f, .35f, .70f), SC(.95f, .85f, .30f), SC(.15f, .15f, .17f), SC(.90f, .90f, .88f), SC(.35f, .60f, .35f), SC(.85f, .55f, .70f) };
            void Rack(float x, float d)
            {
                var at = P(x, d, 0f); float rw = Mathf.Min(3f, w * .3f);
                MItem(br.iron, at, inward, rw, 1.7f, .7f, mb => { foreach (float o in new[] { -rw * .5f + .05f, rw * .5f - .05f }) SBox(mb, at + along * o, along, new Vector3(.05f, 1.65f, .05f), MSteel); SBox(mb, at + Vector3.up * 1.6f, along, new Vector3(.04f, .04f, rw), MSteel); },
                    mb => { for (float o = -rw * .5f + .25f; o < rw * .5f - .2f; o += .3f) SBox(mb, at + along * o + Vector3.up * .55f, along, new Vector3(.55f, 1.0f, .08f), Rand(cloth)); });
            }
            void Mannequin(float x, float d)
            {
                var at = P(x, d, 0f); var c = Rand(cloth); var skin = SC(.92f, .90f, .86f);
                MItem(br.wood, at, inward, .7f, 1.9f, .7f, mb => { SCyl(mb, at, at + Vector3.up * .04f, .3f, SC(.2f, .2f, .2f), 10); SBox(mb, at, along, new Vector3(.06f, .5f, .06f), MSteel); SBox(mb, at + Vector3.up * .5f, along, new Vector3(.32f, .5f, .22f), c * .8f); },
                    mb => { SBox(mb, at + Vector3.up * 1.0f, along, new Vector3(.42f, .6f, .26f), c); SBox(mb, at + Vector3.up * 1.6f, along, new Vector3(.12f, .08f, .12f), skin); SBox(mb, at + Vector3.up * 1.68f, along, new Vector3(.22f, .26f, .24f), skin); });
            }
            void Table(float x, float d, Color top, Color goods)
            {
                var at = P(x, d, 0f);
                MItem(br.wood, at, inward, 1.8f, 1.1f, 1.1f, mb => { SBox(mb, at, along, new Vector3(1.0f, .8f, 1.6f), SC(.40f, .28f, .20f)); SBox(mb, at + Vector3.up * .8f, along, new Vector3(1.1f, .06f, 1.8f), top); },
                    mb => { for (int i = 0; i < 4; i++) SBox(mb, at + Vector3.up * .86f + along * (-.6f + i * .4f), along, new Vector3(.3f, .18f + (i % 2) * .1f, .3f), goods * (.85f + .05f * i)); });
            }
            void Shelf(float x, float d, float sw, float sh, Color[] goods)
            {
                var at = P(x, d, 0f);
                MItem(br.wood, at, inward, sw, sh, .6f, mb => SBox(mb, at, inward, new Vector3(sw, sh, .5f), SC(.82f, .80f, .76f)),
                    mb => { for (int b = 0; b < 3; b++) SBox(mb, at + Vector3.up * (.3f + b * sh / 3.4f) - inward * .03f, inward, new Vector3(sw - .1f, .28f, .48f), goods[(b + rnd.Next(3)) % goods.Length]); });
            }
            void Counter(float x, float d, float cw, Color body, bool glassTop)
            {
                var at = P(x, d, 0f);
                MItem(glassTop ? br.glass : br.wood, at, inward, cw, 1.1f, .8f, mb => { SBox(mb, at, along, new Vector3(.75f, 1.0f, cw), body); SBox(mb, at + Vector3.up * 1.0f, along, new Vector3(.8f, .05f, cw + .05f), glassTop ? SC(.70f, .80f, .84f) : SC(.88f, .86f, .82f)); },
                    mb => { SBox(mb, at + Vector3.up * 1.05f + along * (cw * .3f), along, new Vector3(.35f, .3f, .4f), SC(.12f, .12f, .14f)); });
            }
            void Bench(float x, float d)
            {
                var at = P(x, d, 0f);
                MItem(br.wood, at, inward, 1.8f, .5f, .6f, mb => { SBox(mb, at, along, new Vector3(.5f, .42f, 1.8f), SC(.25f, .22f, .22f)); SBox(mb, at + Vector3.up * .42f, along, new Vector3(.55f, .08f, 1.85f), SC(.55f, .40f, .30f)); });
            }
            void Pile(float x, float d, Color[] cols)
            {
                var at = P(x, d, 0f);
                MItem(br.foam, at, inward, 1.4f, 1.5f, 1.2f, mb => { float y = 0f; for (int i = 0; i < 3; i++) { float sz = .9f - i * .15f; SBox(mb, at + Vector3.up * y + along * ((float)rnd.NextDouble() * .3f - .15f), along, new Vector3(sz, .45f, sz), Rand(cols)); y += .45f; } });
            }
            void Cabinet(float x, float d)
            {
                var at = P(x, d, 0f); var c = Rand(SC(.15f, .15f, .45f), SC(.45f, .10f, .10f), SC(.10f, .35f, .15f));
                MItem(br.iron, at, inward, .8f, 1.9f, .9f, mb => { SBox(mb, at, inward, new Vector3(.75f, 1.85f, .8f), c); SBox(mb, at + Vector3.up * 1.1f - inward * .41f, inward, new Vector3(.6f, .5f, .02f), SC(.30f, .80f, .90f)); }, mb => SBox(mb, at + Vector3.up * .9f - inward * .45f, inward, new Vector3(.7f, .08f, .25f), SC(.1f, .1f, .1f)));
            }
            float back = D - 1.4f;
            switch (cat)
            {
                case 0: Mannequin(w * .22f, 1.8f); Mannequin(w * .78f, 1.8f); Rack(w * .3f, D * .45f); Rack(w * .7f, D * .5f); if (D > 14f) Rack(w * .5f, D * .7f); Counter(w * .5f, back, 3f, SC(.30f, .25f, .22f), false); break;
                case 1: Shelf(w * .5f, back + .5f, Mathf.Min(6f, w - 2f), 2.2f, new[] { SC(.9f, .9f, .9f), SC(.2f, .2f, .2f), SC(.8f, .3f, .2f) }); Bench(w * .3f, D * .5f); Bench(w * .7f, D * .5f); Table(w * .5f, 2.2f, SC(.85f, .85f, .82f), SC(.9f, .4f, .2f)); break;
                case 2: Table(w * .3f, D * .35f, SC(.90f, .88f, .84f), Rand(cloth)); Table(w * .7f, D * .55f, SC(.90f, .88f, .84f), Rand(cloth)); Counter(w * .5f, back, 3f, SC(.55f, .45f, .40f), false); break;
                case 3: { var g = new[] { Rand(cloth), Rand(cloth), Rand(cloth) }; for (float d = 3f; d < back - 1f; d += 4f) Shelf(w * .5f, d, Mathf.Min(6f, w - 3f), 1.8f, g); Counter(w * .2f, back, 2f, SC(.30f, .30f, .32f), false); break; }
                case 4: Counter(w * .5f, D * .45f, Mathf.Min(6f, w - 2f), SC(.75f, .45f, .18f), false); Table(w * .25f, 2.0f, SC(.9f, .9f, .9f), SC(.80f, .55f, .25f)); break;
                case 5: Counter(w * .3f, D * .35f, 2.6f, SC(.12f, .12f, .14f), true); Counter(w * .7f, D * .35f, 2.6f, SC(.12f, .12f, .14f), true); Counter(w * .5f, back, 3.2f, SC(.12f, .12f, .14f), true); break;
                case 6: { var toys = new[] { SC(.95f, .25f, .20f), SC(.20f, .55f, .95f), SC(.98f, .85f, .15f), SC(.30f, .80f, .35f) }; Pile(w * .25f, D * .35f, toys); Pile(w * .7f, D * .45f, toys); Shelf(w * .5f, back + .3f, Mathf.Min(6f, w - 2f), 2.0f, toys); break; }
                default: for (int i = 0; i < 3; i++) Cabinet(.9f, 2.5f + i * 1.2f); for (int i = 0; i < 3; i++) Cabinet(w - .9f, 2.5f + i * 1.2f); Cabinet(w * .5f, back); break;
            }
        }

        // ------------------------------------------------------------ the anchors' fronts inside (closed: their portal, a grille; the east one's upper floor open)
        static void MallAnchorFronts(MallPlan m, Transform root, MeshBuilder deco, MallBreak br, Material signs)
        {
            // (centre on the wall line, the way into the mall, half width, name, open on the upper floor)
            var fronts = new List<(float u, float v, Vector3 into, float half, int row, bool upperOpen, bool boarded)>
            {
                (MallUW + WallTM + .05f, (MallCS + MallCN) * .5f, m.D3(1, 0), 11.5f, SignBelmont, false, false),
                ((-120f - 91f) * .5f, -1325.2f + WallTM + .05f, m.D3(0, 1), 14.5f, SignCarroway, false, false),
                (3f, -1202.6f - WallTM - .05f, m.D3(0, -1), 10f, SignHalversonHome, false, false),
                (-195f, -1217.4f - WallTM - .05f, m.D3(0, -1), 8f, SignSterling, false, true),
                (MallUE - WallTM - .05f, (MallCS + MallCN) * .5f, m.D3(-1, 0), 11.5f, SignHalverson, true, false),
            };
            foreach (var (u, v, into, half, row, upperOpen, boarded) in fronts)
                for (int level = 0; level < 2; level++)
                {
                    float y0 = m.Level(level), H = m.Ceiling(level) - y0; var c = m.P(u, v, y0); var f = MakeFrame(c, new Vector2(into.x, into.z), 1f);
                    bool open = upperOpen && level == 1 || level == 0 && (row == SignHalversonHome || row == SignCarroway);   // Round 64: their lower fronts open (glass)
                    // the portal: cream piers and head, mahogany band, the name over it
                    BoxF(deco, f, -half - 1.2f, -half, 0f, H, 0f, .5f, MCream); BoxF(deco, f, half, half + 1.2f, 0f, H, 0f, .5f, MCream);
                    BoxF(deco, f, -half - 1.2f, half + 1.2f, 4.6f, H, 0f, .45f, MCream); BoxF(deco, f, -half - 1.3f, half + 1.3f, 4.5f, 4.7f, 0f, .55f, MMahogany);
                    if (signs != null) MallPlate(root, f.P(0f, 5.6f, .47f), -f.F, Mathf.Min(12f, half * 1.5f), Mathf.Min(12f, half * 1.5f) * .078f, row, signs);
                    if (open) continue;   // MallDoorsGlass puts the breakable glass across it
                    if (boarded) { BoxF(deco, f, -half, half, 0f, 4.5f, .02f, .12f, SPlyDark); for (float x = -half + 1.2f; x < half; x += 2.44f) BoxF(deco, f, x - .03f, x + .03f, 0f, 4.5f, .12f, .14f, SPly * .8f); continue; }
                    // a closed grille: dark glass behind, the grille's slats
                    BoxF(deco, f, -half, half, 0f, 4.5f, .02f, .06f, SC(.10f, .11f, .12f));
                    for (float y = .3f; y < 4.5f; y += .3f) BoxF(deco, f, -half, half, y, y + .05f, .06f, .1f, MSteel);
                    for (float x = -half; x <= half + .01f; x += 1.5f) BoxF(deco, f, x - .04f, x + .04f, 0f, 4.5f, .06f, .12f, MSteel);
                }
        }

        // ------------------------------------------------------------ the glass that breaks: the doors and the east anchor's upper front
        // Round 64 (Dan, BUG-002 / BUG-006: "these should be glass doors"): the panes see-through (br.doors, MallSeeThrough), each door's bronze frame, its
        // middle rail and push bar a breakable of its own beside it (br.iron); every pane and frame costs no speed and is back for the next lap
        static void MallPanes(MallBreak br, Vector3 a, Vector3 b, float y0, float h, Vector3 outN, float paneW = 3f)
        {
            float len = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z)); int n = Mathf.Max(1, Mathf.RoundToInt(len / paneW));
            for (int k = 0; k < n; k++)
            {
                var p0 = Vector3.Lerp(a, b, k / (float)n); p0.y = y0; var p1 = Vector3.Lerp(a, b, (k + 1) / (float)n); p1.y = y0; var mid = (p0 + p1) * .5f;
                var al = (p1 - p0).normalized;
                MItem(br.doors, mid, outN, (p1 - p0).magnitude, h, 1.4f,
                    mb => { var q0 = p0 + al * .06f; var q1 = p1 - al * .06f; var hh = Vector3.up * h; Face(mb, q0, q1, q1 + hh, q0 + hh, outN); Face(mb, q0, q1, q1 + hh, q0 + hh, -outN); });
                MItem(br.iron, mid, outN, (p1 - p0).magnitude, h, 1.4f,
                    mb => { SBox(mb, p0, p1 - p0, new Vector3(.08f, h, .12f), MBronze); SBox(mb, p0 + Vector3.up * (h - .08f), p1 - p0, new Vector3((p1 - p0).magnitude, .08f, .12f), MBronze); SBox(mb, p0, p1 - p0, new Vector3((p1 - p0).magnitude, .12f, .12f), MBronze); },
                    mb => { SBox(mb, mid + Vector3.up * 1.0f + outN * .07f, p1 - p0, new Vector3((p1 - p0).magnitude * .6f, .05f, .05f), MSteel); SBox(mb, mid + Vector3.up * 1.0f - outN * .07f, p1 - p0, new Vector3((p1 - p0).magnitude * .6f, .05f, .05f), MSteel); });
            }
        }
        // a see-through material (URP Lit, transparent), made once per build
        static Material MallSeeThrough(string name, Color c, float smooth)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetColor("_BaseColor", c); mat.SetFloat("_Smoothness", smooth); mat.SetFloat("_Metallic", 0f); MakeTransparent(mat); mat.SetFloat("_Cull", (float)CullMode.Off);
            return GenAsset(mat, $"{GeneratedDir}/{name}.mat");
        }
        static void MallDoorsGlass(MallPlan m, MallBreak br)
        {
            foreach (var d in m.Doors)
            {
                var off = -d.nOut * (WallTM * .5f); var a = new Vector3(d.a.x + off.x, 0, d.a.y + off.y); var b = new Vector3(d.b.x + off.x, 0, d.b.y + off.y);
                MallPanes(br, a, b, d.floor, d.height - .05f, new Vector3(d.nOut.x, 0, d.nOut.y));
            }
            var e0 = m.P(MallUE - WallTM * .5f, MallCS + .2f, 0f); var e1 = m.P(MallUE - WallTM * .5f, MallCN - .2f, 0f);
            MallPanes(br, e0, e1, m.L1, MallStoreFrontH - .1f, m.D3(1, 0));   // Round 62: 6.1 m (was 4.2): the escalator's launch flies through it
            // Round 64: the fronts of the anchors driven through (Halverson Home on the north wing, Carroway on the center court), on the lower floor
            foreach (var (u, v, half) in MallHollowFronts) { var f0 = m.P(u - half, v, 0f); var f1 = m.P(u + half, v, 0f); MallPanes(br, f0, f1, m.L0, MallFrontH - .1f, m.D3(0, v > -1260f ? -1 : 1), 3.3f); }
            // Round 62: the main entrance's glass tower over its doors breaks too, up to its opening (the balcony drop flies out through it): rows of panes
            var md = m.Doors.First(d => d.main); var moff = -md.nOut * (WallTM * .5f);
            var ma = new Vector3(md.a.x + moff.x, 0, md.a.y + moff.y); var mb2 = new Vector3(md.b.x + moff.x, 0, md.b.y + moff.y);
            for (float y = md.floor + md.height; y < m.L1 + 4.5f; y += 2.6f) MallPanes(br, ma, mb2, y, Mathf.Min(2.55f, m.L1 + 4.55f - y), new Vector3(md.nOut.x, 0, md.nOut.y), 2.4f);
        }

        // ------------------------------------------------------------ the east anchor's upper floor: a department store (Halverson's) to drive through
        static void MallEastStore(MallPlan m, Transform root, MeshBuilder deco, MeshBuilder lights, MallBreak br, Material signs)
        {
            var ring = m.Rings[m.East]; var uv = ring.Select(m.UV).ToList(); float u0 = uv.Min(q => q.x) + .8f, u1 = uv.Max(q => q.x) - .8f, v0 = uv.Min(q => q.y) + .8f, v1 = uv.Max(q => q.y) - .8f;
            float y = m.L1, ceil = m.L1 + 7.6f - MallPlan.Slab; const float aisle0 = -1308.5f, aisle1 = -1298.5f;
            // the aisle's floor (light tile), the departments' carpet
            MQuad(deco, m, u0, aisle0, u1, aisle1, y + .012f, SC(.92f, .90f, .86f));
            foreach (var (a, b) in new[] { (v0, aisle0), (aisle1, v1) }) MQuad(deco, m, u0, a, u1, b, y + .011f, SC(.58f, .52f, .50f));
            var rnd = new System.Random(2391); var cloth = new[] { SC(.80f, .20f, .25f), SC(.20f, .35f, .70f), SC(.95f, .85f, .30f), SC(.15f, .15f, .17f), SC(.90f, .90f, .88f), SC(.35f, .60f, .35f), SC(.85f, .55f, .70f) };
            int racks = 0, counters = 0;
            for (float u = u0 + 6f; u < u1 - 4f; u += 8f)
                foreach (var (a, b) in new[] { (v0 + 3f, aisle0 - 3f), (aisle1 + 3f, v1 - 3f) })
                    for (float v = a; v <= b; v += 6f)
                    {
                        var at = m.P(u, v, y); float rw = 3f; var along = m.D3(1, 0);
                        if (u < u0 + 16f)   // by the mall's entrance: cosmetics counters (glass tops)
                        { MItem(br.glass, at, m.D3(0, 1), 2.6f, 1.1f, .9f, mb => { SBox(mb, at, along, new Vector3(.8f, 1.0f, 2.6f), SC(.92f, .92f, .94f)); SBox(mb, at + Vector3.up * 1.0f, along, new Vector3(.85f, .05f, 2.65f), SC(.70f, .80f, .84f)); }); counters++; continue; }
                        var c1 = cloth[rnd.Next(cloth.Length)];
                        MItem(br.iron, at, m.D3(0, 1), rw, 1.7f, .8f, mb => { foreach (float o in new[] { -rw * .5f + .05f, rw * .5f - .05f }) SBox(mb, at + along * o, along, new Vector3(.05f, 1.6f, .05f), MSteel); SBox(mb, at + Vector3.up * 1.55f, along, new Vector3(.04f, .04f, rw), MSteel); },
                            mb => { for (float o = -rw * .5f + .25f; o < rw * .5f - .2f; o += .3f) SBox(mb, at + along * o + Vector3.up * .5f, along, new Vector3(.55f, 1.0f, .08f), c1 * (.85f + .1f * Mathf.Sin(o * 9f))); });
                        racks++;
                    }
            // display tables down the aisle (to smash), mannequins at its sides
            for (float u = u0 + 24f; u < u1 - 10f; u += 22f)
            {
                var at = m.P(u, (aisle0 + aisle1) * .5f, y);
                MItem(br.wood, at, m.D3(1, 0), 2.2f, 1.1f, 2.2f, mb => { SBox(mb, at, m.D3(1, 0), new Vector3(2f, .8f, 2f), SC(.95f, .95f, .93f)); }, mb => { for (int i = 0; i < 5; i++) SBox(mb, at + Vector3.up * (.8f + .12f * i), m.D3(1, 0), new Vector3(.9f - i * .1f, .12f, .9f - i * .1f), cloth[(i + (int)u) % cloth.Length]); });
                foreach (float vv in new[] { aisle0 - .8f, aisle1 + .8f })
                {
                    var mp = m.P(u + 4f, vv, y); var c = cloth[rnd.Next(cloth.Length)];
                    MItem(br.wood, mp, m.D3(0, 1), .7f, 1.9f, .7f, mb => { SCyl(mb, mp, mp + Vector3.up * .04f, .3f, SC(.2f, .2f, .2f), 10); SBox(mb, mp, m.D3(1, 0), new Vector3(.06f, .5f, .06f), MSteel); SBox(mb, mp + Vector3.up * .5f, m.D3(1, 0), new Vector3(.32f, .5f, .22f), c * .8f); },
                        mb => { SBox(mb, mp + Vector3.up * 1.0f, m.D3(1, 0), new Vector3(.42f, .6f, .26f), c); SBox(mb, mp + Vector3.up * 1.68f, m.D3(1, 0), new Vector3(.22f, .26f, .24f), SC(.92f, .90f, .86f)); });
                }
            }
            // department boards hanging over the aisle (made-up names: the store's own), the lights
            for (float u = u0 + 3f; u < u1 - 2f; u += 10f) for (float v = v0 + 4f; v < v1 - 2f; v += 10f) { var lp = m.P(u, v, ceil - .12f); SBox(lights, lp, m.D3(1, 0), new Vector3(.6f, .1f, 3f), Color.white); m.Lamps.Add(lp); }
            if (signs != null) MallPlate(root, m.P(u0 + 1f, (aisle0 + aisle1) * .5f, y + 5.2f), m.D3(1, 0), 10f, .78f, SignHalverson, signs);
            m.Log.Add($"the east anchor's store: {racks} racks, {counters} counters, the aisle from u {u0:0} to {u1:0}");
        }

        // ------------------------------------------------------------ the concourses' things: kiosks, benches, planters, trash cans, boards, banners
        static void MallConcourse(MallPlan m, Transform root, MeshBuilder deco, MallBreak br, Material signs)
        {
            float vc = (MallCS + MallCN) * .5f;
            void Kiosk(float u, float v, int level, bool alongU)
            {
                var at = m.P(u, v, m.Level(level)); var f = alongU ? m.D3(1, 0) : m.D3(0, 1); var body = new[] { SC(.45f, .22f, .14f), SC(.20f, .22f, .26f), SC(.85f, .82f, .76f) }[Mathf.Abs((int)u) % 3];
                MItem(br.wood, at, f, 2.6f, 2.6f, 4.2f, mb => { SBox(mb, at, f, new Vector3(2.2f, 1.05f, 3.8f), body); SBox(mb, at + Vector3.up * 1.05f, f, new Vector3(2.4f, .06f, 4.0f), SC(.88f, .86f, .82f)); },
                    mb => { foreach (float o in new[] { -1.7f, 1.7f }) SBox(mb, at + f * o + Vector3.up * 1.1f, f, new Vector3(.08f, 1.3f, .08f), MBronze); SBox(mb, at + Vector3.up * 2.4f, f, new Vector3(2.4f, .2f, 4.0f), body * 1.2f); },
                    mb => { for (int i = 0; i < 6; i++) SBox(mb, at + Vector3.up * 1.12f + f * (-1.5f + i * .6f), f, new Vector3(.4f, .25f + (i % 3) * .1f, .4f), new[] { SC(.9f, .3f, .3f), SC(.3f, .5f, .9f), SC(.95f, .85f, .3f) }[i % 3]); });
            }
            void Bench(Vector3 at, Vector3 f)
            {
                MItem(br.wood, at, f, 2.0f, .9f, .8f, mb => { SBox(mb, at, Vector3.Cross(Vector3.up, f), new Vector3(.6f, .42f, 1.9f), SC(.28f, .20f, .16f)); SBox(mb, at + Vector3.up * .42f, Vector3.Cross(Vector3.up, f), new Vector3(.62f, .1f, 2.0f), SC(.55f, .38f, .26f)); },
                    mb => SBox(mb, at - f * .28f + Vector3.up * .5f, Vector3.Cross(Vector3.up, f), new Vector3(.08f, .45f, 2.0f), SC(.55f, .38f, .26f)));
            }
            void Planter(Vector3 at)
            {
                MItem(br.iron, at, Vector3.forward, 1.1f, 2.2f, 1.1f, mb => { SCyl(mb, at, at + Vector3.up * 1.0f, .5f, SC(.48f, .40f, .26f), 10, SC(.25f, .18f, .12f)); },
                    mb => { for (int i = 0; i < 5; i++) { float a = i * 1.25f; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .25f; SBox(mb, at + Vector3.up * 1.0f + o, o.sqrMagnitude > 0 ? o : Vector3.forward, new Vector3(.25f, 1.1f, .5f), SVary(MPlant, a, at.x, .15f)); } });
            }
            void Trash(Vector3 at) => MItem(br.iron, at, Vector3.forward, .7f, 1f, .7f, mb => SCyl(mb, at, at + Vector3.up * .95f, .3f, SC(.30f, .26f, .22f), 8, SC(.08f, .08f, .08f)));
            void Board(Vector3 at, Vector3 f, int row)
            {
                MItem(br.sign, at, f, 1.4f, 2.3f, .6f, mb => { SBox(mb, at, Vector3.Cross(Vector3.up, f), new Vector3(.3f, .3f, 1.2f), SC(.20f, .20f, .22f)); SBox(mb, at + Vector3.up * .3f, Vector3.Cross(Vector3.up, f), new Vector3(.18f, 2.0f, 1.3f), SC(.22f, .24f, .26f)); },
                    mb => { mb.Tint = SC(.85f, .82f, .70f); var r = Vector3.Cross(Vector3.up, f); var c = at + Vector3.up * 1.4f + f * .1f; Face(mb, c - r * .55f - Vector3.up * .8f, c + r * .55f - Vector3.up * .8f, c + r * .55f + Vector3.up * .8f, c - r * .55f + Vector3.up * .8f, f); });
            }
            int kiosks = 0, benches = 0, planters = 0;
            // kiosks down the lower concourse's middle where nothing needs the room (the ramps' run-ins, the court's landing, the doors' lines), in the wings
            foreach (float u in new[] { -229f, -66f, -31f }) { Kiosk(u, vc, 0, true); kiosks++; }
            foreach (float v in new[] { -1272f, -1246f, -1222f }) { Kiosk(3f, v, 0, false); Kiosk(3f, v - 6f, 1, false); kiosks += 2; }
            foreach (float v in new[] { -1270f, -1242f }) { Kiosk(-195f, v, 0, false); kiosks++; }
            // benches and planters along the lanes, 1.6 m off the shop fronts, every 24 m (staggered)
            for (int level = 0; level < 2; level++)
            {
                float y = m.Level(level);
                foreach (var (vv, face) in new[] { (MallCS + 1.6f, 1f), (MallCN - 1.6f, -1f) })
                    for (float u = MallUW + 12f + (face > 0 ? 0f : 12f); u < MallUE - 6f; u += 24f)
                    {
                        if (MallCourtArea.Contains(new Vector2(u, vv)) || Mathf.Abs(u + 51f) < 9f || Mathf.Abs(u + 195f) < 9f || Mathf.Abs(u - 10f) < 9f || (u > -7f && u < 13f && face < 0)) continue;
                        Bench(m.P(u, vv, y), m.D3(0, face)); benches++;
                        Planter(m.P(u + 3f, vv - face * .2f, y)); Trash(m.P(u - 2.2f, vv, y)); planters++;
                    }
                // upper: planters at the railings
                if (level == 1)
                    foreach (var v in m.Voids)
                    {
                        if (v == m.Tower) continue;
                        for (float u = v.xMin + 5f; u < v.xMax - 4f; u += 12f) foreach (float vv in new[] { v.yMin - 1.2f, v.yMax + 1.2f }) { Planter(m.P(u, vv, y)); planters++; }
                    }
            }
            // directory boards by the doors and at the wings
            Board(m.P(-51f, MallCN - 3f, m.L0), m.D3(0, -1), 0); Board(m.P(-195f, MallCS + 3f, m.L0), m.D3(0, 1), 0); Board(m.P(10f, MallCS + 3f, m.L1), m.D3(0, 1), 0); Board(m.P(-206f, -1242f, m.L1), m.D3(1, 0), 0);
            Board(m.P(-3f, MallCN - 2f, m.L0), m.D3(0, -1), 0); Board(m.P(-191f, MallCN - 2f, m.L1), m.D3(0, -1), 0);
            // banners hanging from the upper ceiling over the holes (decor, out of reach), the way-finding band
            if (signs != null)
            {
                MallPlate(root, m.P(-105f, -1303.5f, m.Roof - 2.4f), m.D3(1, 0), 12.8f, 1f, SignBanner1, signs); MallPlate(root, m.P(-105f, -1303.5f, m.Roof - 2.4f), m.D3(-1, 0), 12.8f, 1f, SignBanner2, signs);
                MallPlate(root, m.P(-200f, -1303.5f, m.Roof - 2.0f), m.D3(1, 0), 12.8f, 1f, SignBanner2, signs); MallPlate(root, m.P(25f, -1303.5f, m.Roof - 2.0f), m.D3(-1, 0), 12.8f, 1f, SignBanner1, signs);
                MallPlate(root, m.P(-51f, MallCN - .3f, m.L1 - 1.0f), m.D3(0, 1), 8f, .62f, SignDirections, signs);
            }
            m.Log.Add($"{kiosks} kiosks, {benches} benches, {planters} planters and trash cans");
        }

        // ------------------------------------------------------------ the food court (upper, round the court's hole)
        static void BuildMallFoodCourt(MallPlan m, Transform root, MeshBuilder deco, MallBreak br, Material signs)
        {
            float y = m.L1; var fc = MallFoodCourt;
            // the counters along the recess at the back (north): eight stalls of 6 m, each a counter that breaks, its back wall, its board and name
            float s0 = -128.8f, s1 = -80.5f, back = -1242.4f - WallTM, front = -1249f; int n = 8; float sw = (s1 - s0) / n;
            for (int k = 0; k < n; k++)
            {
                float u = s0 + sw * (k + .5f); var at = m.P(u, front, y); var f = m.D3(0, -1); var colr = new[] { SC(.70f, .15f, .12f), SC(.95f, .70f, .15f), SC(.15f, .45f, .25f), SC(.20f, .30f, .60f) }[k % 4];
                MItem(br.wood, at, f, sw - .4f, 1.1f, 1f, mb => { SBox(mb, at, m.D3(1, 0), new Vector3(sw - .6f, 1.05f, .9f), colr); SBox(mb, at + Vector3.up * 1.05f, m.D3(1, 0), new Vector3(sw - .5f, .06f, 1f), SC(.88f, .86f, .82f)); },
                    mb => { SBox(mb, at + Vector3.up * 1.1f + m.D3(1, 0) * 1.2f, m.D3(1, 0), new Vector3(.5f, .35f, .4f), SC(.12f, .12f, .14f)); SBox(mb, at + Vector3.up * 1.1f - m.D3(1, 0) * 1.0f, m.D3(1, 0), new Vector3(.9f, .3f, .5f), SC(.75f, .75f, .78f)); });
                deco.Tint = colr * .8f; Face(deco, m.P(u - sw * .5f, back - .02f, y), m.P(u + sw * .5f, back - .02f, y), m.P(u + sw * .5f, back - .02f, y + 4.2f), m.P(u - sw * .5f, back - .02f, y + 4.2f), m.D3(0, -1));
                SBox(deco, m.P(u, back - .1f, y + 2.3f), m.D3(0, -1), new Vector3(sw - 1.4f, .9f, .08f), SC(.10f, .10f, .10f));   // the menu board
                if (signs != null) MallPlate(root, m.P(u, front - .6f, y + 4.6f), m.D3(0, 1), sw - .8f, (sw - .8f) * .078f * 1.2f, SignFoodFirst + k % SignFoodCount, signs);
                SBox(deco, m.P(u, front - .5f, y + 4.0f), m.D3(0, -1), new Vector3(sw - .2f, 1.3f, .2f), MCream);
            }
            if (signs != null) MallPlate(root, m.P(-104.5f, -1293f, m.Roof - 1.6f), m.D3(0, 1), 9f, .7f, SignFoodCourt, signs);
            // tables and chairs (six-metre grid), clear of the court's railing and a lane along the counters
            int tables = 0;
            for (float u = fc.xMin + 4f; u < fc.xMax - 3f; u += 6f)
                for (float v = MallCN + 3.5f; v < front - 3f; v += 6f)
                {
                    var at = m.P(u, v, y);
                    MItem(br.wood, at, m.D3(1, 0), 1.2f, .8f, 1.2f, mb => { SCyl(mb, at, at + Vector3.up * .72f, .08f, SC(.15f, .15f, .15f), 6); SCyl(mb, at + Vector3.up * .72f, at + Vector3.up * .77f, .55f, SC(.80f, .76f, .70f), 12); });
                    for (int c = 0; c < 4; c++)
                    {
                        float a = c * Mathf.PI * .5f + .3f; var o = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .9f; var cp = at + o; var cf = -o.normalized;
                        MItem(br.wood, cp, cf, .55f, .95f, .55f, mb => { SBox(mb, cp, cf, new Vector3(.45f, .46f, .45f), SC(.22f, .26f, .20f)); SBox(mb, cp - cf * .2f + Vector3.up * .46f, cf, new Vector3(.45f, .48f, .06f), SC(.45f, .26f, .16f)); });
                    }
                    tables++;
                }
            m.Log.Add($"the food court: {n} counters, {tables} tables with four chairs each");
        }

        // ------------------------------------------------------------ the play area (lower, under the food court): soft walls and shapes
        static void MallPlayArea(MallPlan m, Transform root, MeshBuilder deco, MallBreak br, Material signs)
        {
            float u0 = -132f, u1 = -110f, v0 = -1288f, v1 = -1274f, y = m.L0;
            MQuad(deco, m, u0, v0, u1, v1, y + .013f, SC(.25f, .55f, .70f));
            var bright = new[] { SC(.95f, .30f, .25f), SC(.25f, .60f, .95f), SC(.98f, .80f, .20f), SC(.35f, .75f, .35f), SC(.70f, .40f, .85f) };
            // the padded wall round it (2 m pieces), gaps on two sides to walk in
            void Wall(float ua, float va, float ub, float vb)
            {
                var a = m.P(ua, va, y); var b = m.P(ub, vb, y); float len = Vector3.Distance(a, b); int k = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
                for (int i = 0; i < k; i++) { var p0 = Vector3.Lerp(a, b, i / (float)k); var p1 = Vector3.Lerp(a, b, (i + 1) / (float)k); var mid = (p0 + p1) * .5f; var c = bright[i % bright.Length];
                    MItem(br.foam, mid, Vector3.Cross(Vector3.up, p1 - p0), (p1 - p0).magnitude, .9f, .6f, mb => { SBox(mb, mid, p1 - p0, new Vector3(.45f, .8f, (p1 - p0).magnitude), SC(.40f, .25f, .18f)); SBox(mb, mid + Vector3.up * .8f, p1 - p0, new Vector3(.5f, .12f, (p1 - p0).magnitude), c); }); }
            }
            Wall(u0, v0, u1, v0); Wall(u0, v1, u1 - 6f, v1); Wall(u0, v0, u0, v1);
            // the shapes inside: a little house, a log, balls, a ladybug
            foreach (var (u, v, k) in new[] { (-127f, -1284f, 0), (-121f, -1279f, 1), (-116f, -1284f, 2), (-125f, -1277f, 3), (-114f, -1278f, 4) })
            {
                var at = m.P(u, v, y); var c = bright[k];
                MItem(br.foam, at, m.D3(1, 0), 1.8f, 1.6f, 1.8f, mb => { if (k == 0) { SBox(mb, at, m.D3(1, 0), new Vector3(1.6f, 1.1f, 1.6f), c); SBox(mb, at + Vector3.up * 1.1f, m.D3(1, 0), new Vector3(1.8f, .5f, 1.0f), bright[(k + 2) % 5]); } else if (k == 1) SCyl(mb, at + m.D3(0, 1) * -1f + Vector3.up * .4f, at + m.D3(0, 1) * 1f + Vector3.up * .4f, .4f, c, 10); else SCyl(mb, at, at + Vector3.up * .9f, .55f, c, 10, bright[(k + 1) % 5]); });
            }
            if (signs != null) MallPlate(root, m.P((u0 + u1) * .5f, v0 - .05f, y + 2.6f), m.D3(0, 1), 5f, .39f, SignPlayArea, signs);
            SBox(deco, m.P((u0 + u1) * .5f, v0 + .1f, y), m.D3(0, 1), new Vector3(5.4f, .15f, .1f), SC(.2f, .2f, .2f));
        }

        // ------------------------------------------------------------ the ceiling lights of the concourses (unlit panels; the night's lights go to the nearest)
        static void MallCeilingLights(MallPlan m, MeshBuilder lights)
        {
            for (int level = 0; level < 2; level++)
            {
                float y = m.Ceiling(level) - .1f;
                foreach (var a in MallWalkAreas(level))
                    for (float u = a.xMin + 3f; u < a.xMax - 2f; u += 8f)
                        for (float v = a.yMin + 2.5f; v < a.yMax - 2f; v += 8f)
                        {
                            if (m.InVoid(u, v, 1f) || (level == 1 && MallSkylightArea(u, v))) continue;
                            var p = m.P(u, v, y); SBox(lights, p, m.D3(1, 0), new Vector3(.7f, .08f, .7f), Color.white); m.Lamps.Add(p);
                        }
            }
        }

        // ------------------------------------------------------------ the shoppers (AmbientLife's walkers and talkers; added to the life's groups by BuildLife)
        static void MallShoppers(MallPlan m)
        {
            void Add(float u, float v, int level, int action, int count, float walkU, float walkV)
            {
                var at = m.P(u, v, m.Level(level) + .02f); var walk = m.D3(walkU, walkV) * Mathf.Sqrt(walkU * walkU + walkV * walkV);
                m.Shoppers.Add(new AmbientLife.Group { at = at, yaw = Mathf.Atan2(walk.x + 1e-4f, walk.z) * Mathf.Rad2Deg, kind = AmbientLife.Kind.Shop, count = (byte)count, action = (byte)action, walk = walk });
            }
            for (int level = 0; level < 2; level++)
                for (float u = MallUW + 20f; u < MallUE - 10f; u += 34f)
                {
                    if (MallCourtArea.Contains(new Vector2(u, -1300f)) && level == 0) continue;
                    Add(u, MallCS + 3.4f, level, AmbientLife.Walkers, 2, 6f, 0f);
                    Add(u + 17f, MallCN - 3.4f, level, (int)(u / 34f) % 3 == 0 ? AmbientLife.Talking : AmbientLife.Walkers, 2, 6f, 0f);
                }
            Add(3f, -1260f, 0, AmbientLife.Walkers, 2, 0f, 6f); Add(3f, -1235f, 1, AmbientLife.Walkers, 2, 0f, 6f); Add(-195f, -1255f, 0, AmbientLife.Walkers, 2, 0f, 5f);
            Add(-98f, -1262f, 1, AmbientLife.Talking, 2, 0f, 0f); Add(-120f, -1270f, 1, AmbientLife.Talking, 2, 0f, 0f);
            Add(-121f, -1281f, 0, AmbientLife.Kids, 3, 0f, 0f);
        }

        // ------------------------------------------------------------ the echo: reverb zones down the concourse and in the wings
        static void MallReverb(MallPlan m, Transform root)
        {
            var at = new List<Vector3>();
            for (float u = MallUW + 25f; u < MallUE; u += 50f) at.Add(m.P(u, (MallCS + MallCN) * .5f, m.L1));
            at.Add(m.P(3f, -1245f, m.L1)); at.Add(m.P(-195f, -1255f, m.L1)); at.Add(m.P(110f, -1303f, m.L1 + 3f));
            foreach (var p in at)
            {
                var g = new GameObject("Mall echo"); g.transform.SetParent(root, false); g.transform.position = p;
                var z = g.AddComponent<AudioReverbZone>(); z.reverbPreset = AudioReverbPreset.Hallway; z.minDistance = 26f; z.maxDistance = 40f;
            }
        }
    }
}
