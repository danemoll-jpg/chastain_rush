using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 36 (Part A2b, A3): the stunt park's stores, their insides and their roofs (Dan: "I could also see smashing through the store windows and
// having stunts inside as well"; "get up on top of the building area with stunts up there"; "We can start with something and build from there").
// The supermarket and the shop row are one footprint (OSM building 2, retail, 9 m); in the park it is built here instead of as a solid block:
//   - its walls 0.4 m thick round the footprint, up 1 m past the roof as a parapet (gaps where the roof ramp arrives and where the launches leave),
//     with openings behind the supermarket's front windows and every shop's window: the glass breaks when driven through (breakable panes, as the
//     other breakables, Kind.Glass; they come back when the player leaves the park or resets: StuntPark.RestoreBreakables);
//   - its floor a level slab at the stores' floor (the ground under it holed), its roof a slab you can drive on (ROOF_Y), with the skylight the
//     inside ramp comes up through and the atrium of the roof gap;
//   - inside the supermarket: checkout counters that break, aisles of shelves (solid, 2.2 m) with a kicker in every other lane, a mezzanine along
//     the back wall (4.5 m, reached by a ramp from the floor) with a drop-in back down and the ramp up through the skylight onto the roof;
//   - inside the shop row: one hall behind the ten fronts, the partitions between the shops breakable (drywall), so a run goes the length of it
//     and on into the supermarket.
// The shop fronts keep their awnings and made-up names (the store-sign atlas); the south face (BUG-003) is as it was.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly Color WallOut = SC(.80f, .76f, .68f), WallIn = SC(.88f, .87f, .84f), RoofTop = SC(.42f, .42f, .44f), Ceiling = SC(.86f, .86f, .84f), StoreFloor = SC(.66f, .66f, .64f),
            GlassC = SC(.34f, .46f, .55f), FrameC = SC(.22f, .22f, .23f), ShelfC = SC(.86f, .86f, .88f), ShelfGoods = SC(.70f, .30f, .20f);
        const float WallT = 0.4f, StoreParapet = 1.0f, SlabT = 0.5f;

        static void BuildStuntStores(MapData map, TerrainGrid t, Materials mats, Transform parent, StuntParkPlan p, ShoppingCenter sc)
        {
            var d = p.D; var root = new GameObject("Stunt park stores").transform; root.SetParent(parent, false);
            var ring = p.Ring; var shell = new MeshBuilder(); var inside = new MeshBuilder();
            var signMat = AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir + "/StoreSigns.mat"); var parkSigns = AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir + "/StuntSigns.mat");   // made by BuildShoppingCenter and BuildStuntPark
            bool ccw = PolyArea(ring) > 0;
            // ---------------- holes in the ground under the stores' floors (cells whose middle is 0.35 m or more inside the walls' outer line)
            var bb = Rect.MinMaxRect(ring.Min(q => q.x) - 1, ring.Min(q => q.y) - 1, ring.Max(q => q.x) + 1, ring.Max(q => q.y) + 1);
            int holed = SetHoles(t, bb, q => InPolygon(q, ring) && RingDist(q, ring) > .35f);
            float lowest = ring.Min(q => Ground(t, q.x, q.y)), baseY = Mathf.Min(lowest, d.front_y) - .8f, roof = d.roof_y, floorY = d.front_y;
            // ---------------- the openings and parapet gaps, along the edges (in the store's frame)
            var openings = new List<(Vector2 a, Vector2 b, float top)>();   // world segments on the wall line; top: the opening's height over the floor
            var gaps = new List<(Vector2 a, Vector2 b)>();
            var glassPanes = new List<(Vector2 a, Vector2 b, float h, bool market)>();
            Vector2 Wuv(float u, float v) => p.W(u, v);
            foreach (var g in Pairs(d.market_glass)) { openings.Add((Wuv(d.front_u, g.x), Wuv(d.front_u, g.y), 3.5f)); glassPanes.Add((Wuv(d.front_u, g.x), Wuv(d.front_u, g.y), 3.4f, true)); }
            // the shop row: ten shops from its east end (the store-sign rows' order, as BuildShoppingCenter)
            Vector2 rowE = new Vector2(3416f, 1207f), rowW = new Vector2(3284.9f, 1179.1f); var rowD = (rowW - rowE).normalized; float rowLen = Vector2.Distance(rowE, rowW), shopW = rowLen / d.shop_fronts;
            for (int k = 0; k < d.shop_fronts; k++)
            {
                var c = rowE + rowD * (shopW * (k + .5f)); float half = shopW / 2f - .6f;
                openings.Add((c - rowD * half, c + rowD * half, 2.95f)); glassPanes.Add((c - rowD * half, c + rowD * half, 2.8f, false));
            }
            foreach (var f in d.features)
            {
                if (f.kind == "climb") { gaps.Add((Wuv(f.u_top, f.v - f.width / 2 - .5f), Wuv(f.u_top, f.v + f.width / 2 + .5f))); }
                if (f.kind == "bigjump")
                {
                    var dir = new Vector2(f.du, f.dv); var right = new Vector2(dir.y, -dir.x); float hw = f.width / 2 + .5f;
                    gaps.Add((Wuv(f.lip_u + right.x * hw, f.lip_v + right.y * hw), Wuv(f.lip_u - right.x * hw, f.lip_v - right.y * hw)));
                }
            }
            // ---------------- the walls
            int wallPieces = 0;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count]; float len = Vector2.Distance(a, b); if (len < .05f) continue;
                var dd = (b - a) / len; var n = new Vector2(dd.y, -dd.x); if (InPolygon((a + b) * .5f + n * .2f, ring)) n = -n;   // n outward
                // the cuts along this edge: (s0, s1, kind 0 opening / 1 gap, top)
                var cuts = new List<(float s0, float s1, int kind, float top)>();
                float S(Vector2 q) => Vector2.Dot(q - a, dd);
                bool OnEdge(Vector2 q) => Mathf.Abs(Vector2.Dot(q - a, n)) < .8f && S(q) > -.5f && S(q) < len + .5f;
                foreach (var o in openings) if (OnEdge(o.a) && OnEdge(o.b)) cuts.Add((Mathf.Min(S(o.a), S(o.b)), Mathf.Max(S(o.a), S(o.b)), 0, o.top));
                foreach (var g in gaps) if (OnEdge(g.a) && OnEdge(g.b)) cuts.Add((Mathf.Clamp(Mathf.Min(S(g.a), S(g.b)), 0, len), Mathf.Clamp(Mathf.Max(S(g.a), S(g.b)), 0, len), 1, 0));
                var marks = new SortedSet<float> { 0f, len }; foreach (var c in cuts) { marks.Add(Mathf.Clamp(c.s0, 0, len)); marks.Add(Mathf.Clamp(c.s1, 0, len)); }
                var m = marks.ToList();
                for (int j = 0; j + 1 < m.Count; j++)
                {
                    float s0 = m[j], s1 = m[j + 1]; if (s1 - s0 < .02f) continue; float sm = (s0 + s1) * .5f;
                    bool open = cuts.Any(c => c.kind == 0 && sm > c.s0 && sm < c.s1); float otop = open ? cuts.First(c => c.kind == 0 && sm > c.s0 && sm < c.s1).top : 0;
                    bool gap = cuts.Any(c => c.kind == 1 && sm > c.s0 && sm < c.s1);
                    float top = gap ? roof : roof + StoreParapet;
                    // the solid parts: below the opening nothing (open to the floor), the lintel above it; elsewhere the full height
                    var spans = open ? new[] { (baseY, floorY - .02f), (floorY + otop, top) } : new[] { (baseY, top) };   // under an opening its footing, over it the lintel
                    foreach (var (y0, y1) in spans) { WallPiece(shell, root, a + dd * s0, a + dd * s1, n, y0, y1, top); wallPieces++; }
                }
            }
            // ---------------- the floor slab and the roof slab (rasters of 0.5 m cells in the store's frame, merged into rectangles)
            var skylight = d.skylight; var atrium = d.features.FirstOrDefault(f => f.id == "GAP")?.atrium;
            var skyRamp = SkylightRamp(p);   // where the skylight ramp is, and the roof hole over it (u from)
            bool InHole(float u, float v) => u > skyRamp.holeU0 && u < skylight[1] && v > skylight[2] && v < skylight[3] || atrium != null && atrium.Length == 4 && u > atrium[0] && u < atrium[1] && v > atrium[2] && v < atrium[3];
            var floorRects = StoreRaster(p, ring, .5f, (u, v) => true);
            var roofRects = StoreRaster(p, ring, .5f, (u, v) => !InHole(u, v));
            foreach (var r in floorRects) SlabRect(p, inside, root, r, floorY + .02f, .3f, StoreFloor, StoreFloor * .8f, "Stunt park store floor", true);
            foreach (var r in roofRects) SlabRect(p, shell, root, r, roof, SlabT, RoofTop, Ceiling, "Stunt park roof", false);
            // the holes' edges (the slab's thickness), and low curbs along the atrium's long sides
            HoleEdges(p, shell, skyRamp.holeU0, skylight[1], skylight[2], skylight[3], roof);
            if (atrium != null && atrium.Length == 4)
            {
                HoleEdges(p, shell, atrium[0], atrium[1], atrium[2], atrium[3], roof);
                foreach (float v in new[] { atrium[2] - .15f, atrium[3] + .15f }) { var w0 = p.W(atrium[0], v); var w1 = p.W(atrium[1], v); var mid = (w0 + w1) * .5f; SBox(shell, new Vector3(mid.x, roof, mid.y), p.Dir3(1, 0), new Vector3(.3f, .45f, Vector2.Distance(w0, w1)), SYellow); SolidBox(root, new Vector3(mid.x, roof, mid.y), p.Dir3(1, 0), new Vector3(.3f, .45f, Vector2.Distance(w0, w1)), "Stunt park atrium curb"); }
            }
            // ---------------- the windows: breakable panes (each 3 x 3 shards), as the other breakables
            var glass = new Breakables(); int panes = 0;
            foreach (var g in glassPanes)
            {
                float len = Vector2.Distance(g.a, g.b); int count = Mathf.Max(1, Mathf.RoundToInt(len / (g.market ? 3f : 4f))); var dd = (g.b - g.a) / len; var n = new Vector2(dd.y, -dd.x); if (InPolygon((g.a + g.b) * .5f + n * .2f, ring)) n = -n;
                for (int k = 0; k < count; k++)
                {
                    var p0 = g.a + dd * (len * k / count); var p1 = g.a + dd * (len * (k + 1) / count); var mid = (p0 + p1) * .5f - n * (WallT * .5f);
                    var mb = glass.Begin(mid);
                    for (int sx = 0; sx < 3; sx++) for (int sy = 0; sy < 3; sy++)
                    {
                        Vector2 q0 = Vector2.Lerp(p0, p1, sx / 3f) - n * (WallT * .5f), q1 = Vector2.Lerp(p0, p1, (sx + 1) / 3f) - n * (WallT * .5f); float y0 = floorY + g.h * sy / 3f, y1 = floorY + g.h * (sy + 1) / 3f;
                        glass.Piece(() => { mb.Tint = SVary(GlassC, q0.x + sy, q0.y, .06f); var nn = new Vector3(n.x, 0, n.y); Face(mb, new Vector3(q0.x, y0, q0.y) + nn * .02f, new Vector3(q1.x, y0, q1.y) + nn * .02f, new Vector3(q1.x, y1, q1.y) + nn * .02f, new Vector3(q0.x, y1, q0.y) + nn * .02f, nn); Face(mb, new Vector3(q0.x, y0, q0.y) - nn * .02f, new Vector3(q1.x, y0, q1.y) - nn * .02f, new Vector3(q1.x, y1, q1.y) - nn * .02f, new Vector3(q0.x, y1, q0.y) - nn * .02f, -nn); }, floorY + .05f);
                    }
                    glass.End(new Vector3(mid.x, floorY + g.h * .5f, mid.y), new Vector3(n.x, 0, n.y), Vector2.Distance(p0, p1), g.h, 1.2f); panes++;
                }
            }
            var glassRoot = glass.ToObjects("StuntParkWindows", mats.House, root, .3f, BreakSounds.Kind.Glass);
            p.Breakables.AddRange(glassRoot.GetComponentsInChildren<FenceChunk>());
            // the fronts round the openings: the supermarket's canopy and signs, the shops' awnings and signs (the store-sign atlas, as before)
            StoreFronts(p, shell, root, signMat, rowE, rowD, shopW, floorY, roof);
            // ---------------- inside
            int parts = ShopPartitions(p, root, mats, inside, rowE, rowD, shopW, floorY, roof);
            int shelves = MarketInside(p, root, mats, inside, floorY, roof, parkSigns);
            CeilingLights(p, root, mats, floorRects, roof);
            shell.ToObject("StuntParkStoreShell", mats.House, root, false, true);
            inside.ToObject("StuntParkStoreInside", mats.House, root, false, true);
            p.Log.Add($"the stores: {wallPieces} wall pieces, {floorRects.Count} floor and {roofRects.Count} roof slabs, {panes} window panes, {parts} partitions, {shelves} shelves, {holed} ground cells holed");
        }

        // one wall piece from a to b on the footprint's line, inward 0.4 m, y0 to y1 (its outside and inside faces, its top, its ends)
        static void WallPiece(MeshBuilder mb, Transform root, Vector2 a, Vector2 b, Vector2 n, float y0, float y1, float top)
        {
            if (y1 - y0 < .02f) return;
            var nn = new Vector3(n.x, 0, n.y); Vector3 A(Vector2 q, float y, float inward) => new Vector3(q.x - n.x * inward, y, q.y - n.y * inward);
            var along = new Vector3(b.x - a.x, 0, b.y - a.y).normalized;
            mb.Tint = WallOut; Face(mb, A(a, y0, 0), A(b, y0, 0), A(b, y1, 0), A(a, y1, 0), nn);
            mb.Tint = WallIn; Face(mb, A(a, y0, WallT), A(b, y0, WallT), A(b, y1, WallT), A(a, y1, WallT), -nn);
            mb.Tint = Mathf.Abs(y1 - top) < .01f ? SC(.62f, .62f, .60f) : WallOut; Face(mb, A(a, y1, 0), A(b, y1, 0), A(b, y1, WallT), A(a, y1, WallT), Vector3.up);
            mb.Tint = WallOut * .92f; Face(mb, A(a, y0, 0), A(b, y0, 0), A(b, y0, WallT), A(a, y0, WallT), Vector3.down);
            mb.Tint = WallOut * .95f; Face(mb, A(a, y0, 0), A(a, y0, WallT), A(a, y1, WallT), A(a, y1, 0), -along); Face(mb, A(b, y0, 0), A(b, y0, WallT), A(b, y1, WallT), A(b, y1, 0), along);
            var mid = (a + b) * .5f - n * (WallT * .5f);
            var g = new GameObject("Stunt park store wall"); g.transform.SetParent(root, false);
            g.transform.SetPositionAndRotation(new Vector3(mid.x, (y0 + y1) * .5f, mid.y), Quaternion.LookRotation(along));
            g.AddComponent<BoxCollider>().size = new Vector3(WallT, y1 - y0, Vector2.Distance(a, b));
        }

        // cells of `cell` m in the store's frame whose middle is inside the footprint and 0.15 m or more inside its line, kept by `keep`, merged into
        // rectangles (rows first, then equal rows stacked)
        static List<Rect> StoreRaster(StuntParkPlan p, List<Vector2> ring, float cell, Func<float, float, bool> keep)
        {
            var uv = ring.Select(p.UVof).ToList(); float u0 = uv.Min(q => q.x), u1 = uv.Max(q => q.x), v0 = uv.Min(q => q.y), v1 = uv.Max(q => q.y);
            int nu = Mathf.CeilToInt((u1 - u0) / cell), nv = Mathf.CeilToInt((v1 - v0) / cell);
            var on = new bool[nu, nv];
            for (int i = 0; i < nu; i++) for (int j = 0; j < nv; j++) { float u = u0 + (i + .5f) * cell, v = v0 + (j + .5f) * cell; var w = p.W(u, v); on[i, j] = InPolygon(w, ring) && RingDist(w, ring) > .15f && keep(u, v); }
            var rects = new List<Rect>(); var used = new bool[nu, nv];
            for (int j = 0; j < nv; j++)
                for (int i = 0; i < nu; i++)
                {
                    if (!on[i, j] || used[i, j]) continue;
                    int i1 = i; while (i1 + 1 < nu && on[i1 + 1, j] && !used[i1 + 1, j]) i1++;
                    int j1 = j; bool grow = true;
                    while (grow && j1 + 1 < nv) { for (int k = i; k <= i1; k++) if (!on[k, j1 + 1] || used[k, j1 + 1]) { grow = false; break; } if (grow) j1++; }
                    for (int a = i; a <= i1; a++) for (int b = j; b <= j1; b++) used[a, b] = true;
                    rects.Add(Rect.MinMaxRect(u0 + i * cell, v0 + j * cell, u0 + (i1 + 1) * cell, v0 + (j1 + 1) * cell));
                }
            return rects;
        }
        // a slab over a rectangle of the store's frame: its top (and its underside), a box collider
        static void SlabRect(StuntParkPlan p, MeshBuilder mb, Transform root, Rect r, float topY, float thick, Color top, Color under, string name, bool tiles)
        {
            Vector3 P(float u, float v, float y) { var w = p.W(u, v); return new Vector3(w.x, y, w.y); }
            if (tiles)   // the store's floor in 4 m tiles of two shades
                for (float u = r.xMin; u < r.xMax - .01f; u += 4f) for (float v = r.yMin; v < r.yMax - .01f; v += 4f)
                {
                    float ua = u, ub = Mathf.Min(r.xMax, u + 4f), va = v, vb = Mathf.Min(r.yMax, v + 4f);
                    mb.Tint = (Mathf.FloorToInt(u / 4f) + Mathf.FloorToInt(v / 4f)) % 2 == 0 ? top : top * .94f;
                    Face(mb, P(ua, va, topY), P(ub, va, topY), P(ub, vb, topY), P(ua, vb, topY), Vector3.up);
                }
            else { mb.Tint = top; Face(mb, P(r.xMin, r.yMin, topY), P(r.xMax, r.yMin, topY), P(r.xMax, r.yMax, topY), P(r.xMin, r.yMax, topY), Vector3.up); }
            if (!tiles) { mb.Tint = under; Face(mb, P(r.xMin, r.yMin, topY - thick), P(r.xMax, r.yMin, topY - thick), P(r.xMax, r.yMax, topY - thick), P(r.xMin, r.yMax, topY - thick), Vector3.down); }
            var c = p.W(r.center.x, r.center.y); var g = new GameObject(name); g.transform.SetParent(root, false);
            g.transform.SetPositionAndRotation(new Vector3(c.x, topY - thick * .5f, c.y), Quaternion.LookRotation(p.Dir3(0, 1)));
            // local x = right of the frame's v = +u (Dir3(0,1) forward: its right is +u); local z = v
            g.AddComponent<BoxCollider>().size = new Vector3(r.width, thick, r.height);
        }
        static void HoleEdges(StuntParkPlan p, MeshBuilder mb, float u0, float u1, float v0, float v1, float roof)
        {
            Vector3 P(float u, float v, float y) { var w = p.W(u, v); return new Vector3(w.x, y, w.y); }
            mb.Tint = SYellow;
            Face(mb, P(u0, v0, roof - SlabT), P(u1, v0, roof - SlabT), P(u1, v0, roof), P(u0, v0, roof), p.Dir3(0, 1));
            Face(mb, P(u0, v1, roof - SlabT), P(u1, v1, roof - SlabT), P(u1, v1, roof), P(u0, v1, roof), p.Dir3(0, -1));
            Face(mb, P(u0, v0, roof - SlabT), P(u0, v1, roof - SlabT), P(u0, v1, roof), P(u0, v0, roof), p.Dir3(1, 0));
            Face(mb, P(u1, v0, roof - SlabT), P(u1, v1, roof - SlabT), P(u1, v1, roof), P(u1, v0, roof), p.Dir3(-1, 0));
        }

        // the fronts: the supermarket's green canopy, its big sign and PHARMACY over the windows; each shop's awning and sign (the old fronts' sizes)
        static void StoreFronts(StuntParkPlan p, MeshBuilder mb, Transform root, Material signMat, Vector2 rowE, Vector2 rowD, float shopW, float floorY, float roof)
        {
            var d = p.D; var green = SC(.16f, .36f, .20f);
            var fm = MakeFrame(new Vector3(p.W(d.front_u, 404f).x, floorY, p.W(d.front_u, 404f).y), p.W(1, 0) - p.W(0, 0), 1f);   // front: +u
            // the frame round the main windows, the canopy, the sign
            BoxF(mb, fm, -15.3f, 15.3f, 3.5f, 3.75f, .0f, .25f, FrameC);
            BoxF(mb, fm, -16f, 16f, 3.8f, 4.2f, .02f, 3.0f, green);
            float room = roof - floorY, signY = Mathf.Min(6.0f, room - 1.3f); const float bw = 26f, bh = bw * 0.078f;
            BoxF(mb, fm, -bw / 2f - .2f, bw / 2f + .2f, signY - bh / 2f - .2f, signY + bh / 2f + .2f, .02f, .25f, SC(.88f, .87f, .83f));
            if (signMat != null)
            {
                AtlasPlate(root, fm.P(0f, signY, .26f), -fm.F, bw, bh, SignMarket, signMat);
                BoxF(mb, fm, 22f, 31f, 3.05f, 3.2f, .02f, 1.6f, green);
                BoxF(mb, fm, 22.4f, 30.6f, 3.6f - .45f, 3.6f + .45f, .02f, .18f, SC(.16f, .16f, .17f));
                AtlasPlate(root, fm.P(26.5f, 3.6f, .19f), -fm.F, 8f, .62f, SignPharmacy, signMat);
            }
            // the shops: awning over the window, the sign board and its name
            var nOut = new Vector2(-rowD.y, rowD.x); if (Vector2.Dot(nOut, p.W(0, 1) - p.W(0, 0)) < 0) nOut = -nOut;   // out of the shop row: -v
            nOut = -(p.W(0, 1) - p.W(0, 0)).normalized;
            for (int k = 0; k < d.shop_fronts; k++)
            {
                var c = rowE + rowD * (shopW * (k + .5f)); var f = MakeFrame(new Vector3(c.x, floorY, c.y), nOut, 1f);
                var awning = ShopAwnings[k % ShopAwnings.Length]; float half = shopW / 2f - .6f;
                BoxF(mb, f, -half - .15f, half + .15f, 2.95f, 3.05f, 0f, .2f, FrameC);
                BoxF(mb, f, -shopW / 2f + .2f, shopW / 2f - .2f, 3.05f, 3.2f, .02f, 1.6f, awning);
                float sy = Mathf.Min(4.0f, roof - floorY - .6f), bw2 = Mathf.Min(9f, shopW - 1.6f), bh2 = bw2 * .078f;
                BoxF(mb, f, -bw2 / 2f - .1f, bw2 / 2f + .1f, sy - bh2 / 2f - .1f, sy + bh2 / 2f + .1f, .02f, .18f, SC(.16f, .16f, .17f));
                if (signMat != null) AtlasPlate(root, f.P(0f, sy, .19f), -f.F, bw2, bh2, SignRowFirst + k, signMat);
            }
        }

        // the shop row's partitions between the shops: breakable drywall up to 3.6 m (drywall and studs flying), the wall above it to the roof
        static int ShopPartitions(StuntParkPlan p, Transform root, Materials mats, MeshBuilder inside, Vector2 rowE, Vector2 rowD, float shopW, float floorY, float roof)
        {
            var walls = new Breakables(); int n = 0; var ring = p.Ring; var inward = (p.W(0, 1) - p.W(0, 0)).normalized;   // from the front into the shops
            for (int k = 1; k < p.D.shop_fronts; k++)
            {
                var front = rowE + rowD * (shopW * k) + inward * (WallT + .02f);
                // to the back wall: the first point along +v that leaves the footprint (less the wall)
                float depth = 0f; while (depth < 40f && InPolygon(front + inward * (depth + .25f), ring)) depth += .25f; depth -= WallT + .05f;
                int panels = Mathf.Max(2, Mathf.RoundToInt(depth / 3.9f));
                for (int i = 0; i < panels; i++)
                {
                    var a = front + inward * (depth * i / panels); var b = front + inward * (depth * (i + 1) / panels); var mid = (a + b) * .5f;
                    var mb = walls.Begin(mid); var side = new Vector3(rowD.x, 0, rowD.y);
                    for (int sy = 0; sy < 2; sy++)
                    {
                        float y0 = floorY + 3.6f * sy / 2f, y1 = floorY + 3.6f * (sy + 1) / 2f; var aa = a; var bb2 = b;
                        walls.Piece(() => { mb.Tint = SVary(SC(.90f, .89f, .86f), aa.x + sy, aa.y, .04f); BoxWorld(mb, aa, bb2, side, .14f, y0, y1); }, floorY + .05f);
                    }
                    walls.Piece(() => { mb.Tint = SC(.62f, .52f, .36f); BoxWorld(mb, a, a + (b - a) * .06f, side, .1f, floorY, floorY + 3.6f); }, floorY + .05f);   // a stud
                    walls.End(new Vector3(mid.x, floorY + 1.8f, mid.y), side, Vector2.Distance(a, b), 3.6f, .8f); n++;
                }
                // above: the wall to the ceiling (no collider: nothing drives up there)
                var top0 = front; var top1 = front + inward * depth; inside.Tint = WallIn; BoxWorld(inside, top0, top1, new Vector3(rowD.x, 0, rowD.y), .14f, floorY + 3.6f, roof - SlabT);
            }
            var wr = walls.ToObjects("StuntParkPartitions", mats.House, root, .4f, BreakSounds.Kind.Wood);
            p.Breakables.AddRange(wr.GetComponentsInChildren<FenceChunk>());
            return n;
        }
        // a box from a to b (on the ground plan), `thick` across (along `side`), y0 to y1
        static void BoxWorld(MeshBuilder mb, Vector2 a, Vector2 b, Vector3 side, float thick, float y0, float y1)
        {
            var t = mb.Tint; var along = new Vector3(b.x - a.x, 0, b.y - a.y); float len = along.magnitude; if (len < 1e-3f) return; along /= len;
            var c = new Vector3((a.x + b.x) * .5f, y0, (a.y + b.y) * .5f);
            SBox(mb, c, along, new Vector3(thick, y1 - y0, len), t, t * 1.04f);
        }

        // the supermarket's inside: checkouts (breakable), shelves (solid), the lanes' kickers, the mezzanine with its ramp up, railing, drop-in, and the
        // ramp up through the skylight onto the roof
        static int MarketInside(StuntParkPlan p, Transform root, Materials mats, MeshBuilder m, float floorY, float roof, Material parkSigns)
        {
            var d = p.D; int shelves = 0;
            Vector3 P(float u, float v, float y) { var w = p.W(u, v); return new Vector3(w.x, y, w.y); }
            var east = p.Dir3(1, 0); var north = p.Dir3(0, 1);
            // shelves: long units along u, 1.4 m deep, 2.2 m tall, goods in colored bands; solid
            foreach (float v in d.shelves)
            {
                float u0 = d.shelf_u[0], u1 = d.shelf_u[1]; var c = P((u0 + u1) * .5f, v, floorY);
                SBox(m, c, east, new Vector3(d.shelf_d, d.shelf_h, u1 - u0), ShelfC);
                for (int b = 0; b < 3; b++) SBox(m, c + Vector3.up * (.35f + b * .65f), east, new Vector3(d.shelf_d + .04f, .35f, u1 - u0 - .2f), b == 0 ? ShelfGoods : b == 1 ? SC(.20f, .45f, .70f) : SC(.95f, .80f, .25f));
                SolidBox(root, c, east, new Vector3(d.shelf_d, d.shelf_h, u1 - u0), "Stunt park shelf"); shelves++;
            }
            // the lanes' kickers (plywood, 0.7 m, facing west: the run from the windows)
            foreach (var k in Pairs(d.aisle_kickers))
            {
                var prof = new SProf(); float r = .7f / (1f - Mathf.Cos(18f * Mathf.Deg2Rad)); prof.Arc(r, 18f); prof.pts.Add(new Vector2(prof.s, 0f));
                ParkRampFrom(p, m, prof.pts, k.x, k.y, -1, 0, 4.5f, "ply", "front", root, "aisle kicker");
            }
            // checkouts: counters along u with their registers (breakable: wood and metal flying)
            var counters = new Breakables();
            foreach (float v in d.checkouts)
            {
                var c = P(d.checkout_u, v, floorY); var mb = counters.Begin(new Vector2(c.x, c.z));
                counters.Piece(() => SBox(mb, c, east, new Vector3(1.0f, 1.0f, 4.0f), SC(.30f, .32f, .36f), SC(.20f, .20f, .22f)), floorY + .05f);
                counters.Piece(() => { SBox(mb, c + east * 1.2f + Vector3.up * 1.0f, east, new Vector3(.5f, .35f, .5f), SC(.12f, .12f, .14f)); SBox(mb, c + east * 1.2f + Vector3.up * 1.35f, east, new Vector3(.08f, .4f, .08f), SC(.1f, .1f, .1f)); }, floorY + .05f);
                counters.End(c + Vector3.up * .6f, east, 1.1f, 1.4f, 4.1f);
            }
            var cr = counters.ToObjects("StuntParkCheckouts", mats.House, root, .4f, BreakSounds.Kind.Iron);
            p.Breakables.AddRange(cr.GetComponentsInChildren<FenceChunk>());
            // the mezzanine: a deck along the back wall at 4.5 m, a ramp up to it at its south end, posts, the railing on its edge (gaps at the drop-in and
            // the skylight ramp)
            float mu0 = d.mezz[0], mu1 = d.mezz[1], mv0 = d.mezz[2], mv1 = d.mezz[3], mh = d.mezz[4];
            var ramp = new SProf(); float ang = Mathf.Atan2(mh, d.mezz_ramp[1] - d.mezz_ramp[0]) * Mathf.Rad2Deg * 1.15f;
            ramp.Arc(10f, ang); float rise = mh - 2f * 10f * (1f - Mathf.Cos(ang * Mathf.Deg2Rad)); ramp.Line(rise / Mathf.Sin(ang * Mathf.Deg2Rad)); ramp.Arc(10f, -ang);
            float rampLen = ramp.s; ParkRampFrom(p, m, ramp.pts.Concat(new[] { new Vector2(rampLen, 0f) }).ToList(), (mu0 + mu1) * .5f, mv0, 0, 1, mu1 - mu0, "steel", "front", root, "mezzanine ramp");
            float deckV0 = mv0 + rampLen;
            var deckC = P((mu0 + mu1) * .5f, (deckV0 + mv1) * .5f, floorY + mh - .35f);
            SBox(m, deckC, north, new Vector3(mu1 - mu0, .35f, mv1 - deckV0), SSteelDark, SSteel); SolidBox(root, deckC, north, new Vector3(mu1 - mu0, .35f, mv1 - deckV0), "Stunt park mezzanine");
            for (float v = deckV0 + 2f; v < mv1 - 1f; v += 6f) { var pc = P(mu1 - .3f, v, floorY); SBox(m, pc, north, new Vector3(.3f, mh - .35f, .3f), SYellow); SolidBox(root, pc, north, new Vector3(.3f, mh - .35f, .3f), "Stunt park mezzanine post"); }
            var gapsV = new List<(float a, float b)> { (d.mezz_dropin[0], d.mezz_dropin[1]), (d.skylight[2], d.skylight[3]) };
            for (float v = deckV0; v < mv1 - .01f; v += 1f)
            {
                float v2 = Mathf.Min(mv1, v + 1f), vm = (v + v2) * .5f; if (gapsV.Any(g => vm > g.a - .2f && vm < g.b + .2f)) continue;
                var rc = P(mu1 - .1f, vm, floorY + mh); SBox(m, rc, north, new Vector3(.12f, 1.05f, v2 - v), SYellow); SolidBox(root, rc, north, new Vector3(.2f, 1.1f, v2 - v), "Stunt park mezzanine railing");
            }
            // the drop-in: from the deck's edge east, down to the floor between two shelves
            var drop = new SProf(); drop.y = mh; drop.pts[0] = new Vector2(0, mh); drop.Flat(.5f); drop.Arc(6f, -15f); float dl = (drop.y - 14f * (1f - Mathf.Cos(15f * Mathf.Deg2Rad))) / Mathf.Sin(15f * Mathf.Deg2Rad); drop.Line(dl); drop.Arc(14f, 15f);
            var dpts = drop.pts.ToList(); dpts[dpts.Count - 1] = new Vector2(dpts[dpts.Count - 1].x, 0f);
            ParkRampFrom(p, m, dpts, mu1, (d.mezz_dropin[0] + d.mezz_dropin[1]) * .5f, 1, 0, d.mezz_dropin[1] - d.mezz_dropin[0], "launch", "front", root, "mezzanine drop-in");
            // the skylight ramp: from the deck up through the roof (SkylightRamp: the same profile)
            var sr = SkylightRamp(p);
            ParkRampFrom(p, m, sr.prof, d.skylight[0], (d.skylight[2] + d.skylight[3]) * .5f, 1, 0, d.skylight[3] - d.skylight[2], "steel", "front", root, "skylight ramp");
            // boards on the deck: DROP IN and UP TO THE ROOF, facing south (a rider on the deck heading north sees them)
            if (parkSigns != null)
            {
                foreach (var (v, row) in new[] { (d.mezz_dropin[1] + .6f, SignDropIn), (d.skylight[3] + .6f, SignUpToRoof) })
                {
                    var at = P((mu0 + mu1) * .5f, v, floorY + mh + 2.2f); var face = p.Dir3(0, -1);
                    SBox(m, at - Vector3.up * .5f + north * .1f, face, new Vector3(6.6f, .75f, .08f), SBlack);
                    AtlasPlate(root, at + face * .02f, -face, 6.4f, .5f, row, parkSigns);
                    SBox(m, P(mu0 + .3f, v, floorY + mh), face, new Vector3(.12f, 2.0f, .12f), SSteelDark); SBox(m, P(mu1 - .5f, v, floorY + mh), face, new Vector3(.12f, 2.0f, .12f), SSteelDark);
                }
            }
            return shelves;
        }
        // a ramp from a profile list (the inside's ramps: base "front")
        static void ParkRampFrom(StuntParkPlan p, MeshBuilder vis, List<Vector2> prof, float u, float v, float du, float dv, float width, string look, string baseKind, Transform root, string name)
            => ParkRamp(p, vis, prof, u, v, du, dv, width, look, baseKind, root, name);

        // the skylight ramp's profile (from the mezzanine's top at u0 up to the roof, rounded in and out) and where the roof's hole begins (the ramp
        // there 3 m under the roof: a vehicle on it clears the roof's edge)
        static (List<Vector2> prof, float holeU0) SkylightRamp(StuntParkPlan p)
        {
            var d = p.D; float mh = d.mezz[4], rise = d.roof_y - d.front_y - mh, ang = 14f, r = 12f;
            var prof = new SProf(); prof.y = mh; prof.pts[0] = new Vector2(0, mh); prof.Flat(.3f);
            float arcRise = 2f * r * (1f - Mathf.Cos(ang * Mathf.Deg2Rad)); prof.Arc(r, ang).Line(Mathf.Max(0f, (rise - arcRise) / Mathf.Sin(ang * Mathf.Deg2Rad))).Arc(r, -ang);
            var pts = prof.pts.ToList(); pts[pts.Count - 1] = new Vector2(pts[pts.Count - 1].x, mh + rise);
            float want = d.roof_y - d.front_y - 3.0f, holeS = pts[pts.Count - 1].x;   // where the ramp climbs past 3 m under the roof (between the profile's points)
            for (int i = 1; i < pts.Count; i++) if (pts[i].y > want) { holeS = Mathf.Lerp(pts[i - 1].x, pts[i].x, Mathf.InverseLerp(pts[i - 1].y, pts[i].y, want)); break; }
            return (pts, d.skylight[0] + Mathf.Max(0f, holeS - .5f));
        }

        // the ceiling's light panels (emissive strips, always on: a store is lit inside), every 8 m
        static void CeilingLights(StuntParkPlan p, Transform root, Materials mats, List<Rect> floorRects, float roof)
        {
            var lights = new MeshBuilder(); var ring = p.Ring;
            var uv = ring.Select(p.UVof).ToList(); float u0 = uv.Min(q => q.x), u1 = uv.Max(q => q.x), v0 = uv.Min(q => q.y), v1 = uv.Max(q => q.y);
            for (float v = v0 + 4f; v < v1 - 2f; v += 8f)
                for (float u = u0 + 3f; u < u1 - 3f; u += 6f)
                {
                    var w = p.W(u, v); if (!InPolygon(w, ring) || RingDist(w, ring) < 1.5f) continue;
                    if (u > p.D.skylight[0] - 1 && u < p.D.skylight[1] + 1 && v > p.D.skylight[2] - 1 && v < p.D.skylight[3] + 1) continue;
                    SBox(lights, new Vector3(w.x, roof - SlabT - .12f, w.y), p.Dir3(1, 0), new Vector3(.5f, .1f, 3.6f), Color.white);
                }
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "StuntParkCeilingLight" }; mat.SetColor("_BaseColor", new Color(1f, .97f, .9f));
            AssetDatabase.CreateAsset(mat, GeneratedDir + "/StuntParkCeilingLight.mat");
            lights.ToObject("StuntParkCeilingLights", mat, root, false, false);
        }
    }
}
