using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEditor;
using UnityEngine;

// Round 38 (Part C, Dan picked "More inside the stores"): the shop row's ten shops get rooms that fit their made-up names, and the park gets two more
// inside lines; the bank (Part A) gets its lobby and vault. Layout data: Data/tracks/stunt_park.json (Tools/mapgen/make_stunt_park.py: shop_ramp,
// the PIZZA launch, bank_vault, bank_teller).
//   The rooms: simple and readable, each shop its own colors and things, every piece of furniture breakable (as the stores' windows: they come back
//     with the park's breakables). The hall's front 8 m stays the shop-to-shop run through the partitions, with the light furniture in it to smash.
//   The shop ramp: a steel ramp along the back of the laundry, the florist and the taqueria, eastbound, up 9 m through a hatch in the roof onto the
//     shop row's roof in the roof gap's lane (its partitions open over the ramp's width); its run-up from the supermarket's north end.
//   The pizza window: Corner Slice Pizza's aimed kicker out through its front window (a park launch: MapBuilder.StuntPark.cs builds it and its landing).
//   The bank: the teller counter across the lobby (breakable, glass screens on it), the vault against the east wall behind the drive-up bay, its round
//     door on the line through (breakable: the biggest smash), cash carts inside, deposit boxes on its walls.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // ---------------------------------------------------------------- the shop ramp: its profile and where it is (the roof's hatch over it)
        sealed class ShopRampPlan { public List<Vector2> prof; public float footU, topU, v, width, holeU0; }
        static ShopRampPlan ShopRamp(StuntParkPlan p)
        {
            var d = p.D; if (d.shop_ramp == null || d.shop_ramp.Length < 5) return null;
            float v = d.shop_ramp[0], w = d.shop_ramp[1], topU = d.shop_ramp[2], ang = d.shop_ramp[3], r = d.shop_ramp[4], rise = d.roof_y - d.front_y;
            var prof = new SProf(); float arcRise = 2f * r * (1f - Mathf.Cos(ang * Mathf.Deg2Rad));
            prof.Arc(r, ang).Line(Mathf.Max(0f, (rise - arcRise) / Mathf.Sin(ang * Mathf.Deg2Rad))).Arc(r, -ang);
            var pts = prof.pts.ToList(); pts[pts.Count - 1] = new Vector2(pts[pts.Count - 1].x, rise);
            float len = pts[pts.Count - 1].x, foot = topU - len;
            float want = rise - 3.0f, holeS = len;   // the hatch: where the ramp is within 3 m of the roof (a vehicle on it clears the roof's edge)
            for (int i = 1; i < pts.Count; i++) if (pts[i].y > want) { holeS = Mathf.Lerp(pts[i - 1].x, pts[i].x, Mathf.InverseLerp(pts[i - 1].y, pts[i].y, want)); break; }
            return new ShopRampPlan { prof = pts, footU = foot, topU = topU, v = v, width = w, holeU0 = foot + Mathf.Max(0f, holeS - .5f) };
        }
        static bool InShopRampBand(ShopRampPlan sr, float u, float v, float pad = .3f) => sr != null && u > sr.footU - pad && u < sr.topU + pad && Mathf.Abs(v - sr.v) < sr.width * .5f + pad;

        static void BuildShopRamp(StuntParkPlan p, MeshBuilder vis, MeshBuilder props, Transform root)
        {
            var sr = ShopRamp(p); if (sr == null) return;
            ParkRamp(p, vis, sr.prof, sr.footU, sr.v, 1, 0, sr.width, "steel", "front", root, "shop ramp");
            // rails along both sides where it is over 1.5 m up (as the roof ramp's)
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 prev = default; bool has = false;
                for (float s = 0; s <= sr.prof[sr.prof.Count - 1].x + .01f; s += 2f)
                {
                    float y = Interp(sr.prof, s); if (y < 1.5f) { has = false; continue; }
                    var w = p.W(sr.footU + s, sr.v + side * (sr.width * .5f - .15f)); var at = new Vector3(w.x, p.D.front_y + y, w.y);
                    SBox(props, at, p.Dir3(1, 0), new Vector3(.12f, 1.1f, .12f), SYellow);
                    if (has) { foreach (float h in new[] { .55f, 1.05f }) SRail(props, prev + Vector3.up * h, at + Vector3.up * h, .06f, SYellow); var mid = (prev + at) * .5f; SolidBox(root, mid, at - prev, new Vector3(.2f, 1.2f, (at - prev).magnitude + .05f), "Stunt park shop ramp rail"); }
                    prev = at; has = true;
                }
            }
            p.Log.Add($"the shop ramp: {sr.prof[sr.prof.Count - 1].x:0} m from u {sr.footU:0.0} up to the roof at u {sr.topU:0.0}, the hatch from u {sr.holeU0:0.0}");
        }

        // ---------------------------------------------------------------- the rooms
        sealed class Room
        {
            public StuntParkPlan p; public float u0, u1, v0, v1, cu; public MeshBuilder deco;
            public Breakables furniture = new(), counters = new(), displays = new(), shelves = new(), appliances = new();
        }
        // a breakable item: boxes in the store's frame (u across, v deep), from the floor; one panel
        static void Item(Room r, Breakables b, float u, float v, float wu, float dv, float h, Action<MeshBuilder, Func<float, float, float, Vector3>> draw)
        {
            var p = r.p; float fy = p.D.front_y; var c = p.W(u, v); var mb = b.Begin(c);
            Vector3 P(float du, float dvv, float y) { var w = p.W(u + du, v + dvv); return new Vector3(w.x, fy + y, w.y); }
            b.Piece(() => draw(mb, P), fy + .05f);
            b.End(new Vector3(c.x, fy + h * .5f, c.y), p.Dir3(0, 1), Mathf.Max(wu, .6f), Mathf.Max(h, .8f), Mathf.Max(dv, .6f));
        }
        static void BoxAt(MeshBuilder mb, Vector3 bottom, StuntParkPlan p, float wu, float h, float dv, Color c, Color? top = null) => SBox(mb, bottom, p.Dir3(0, 1), new Vector3(wu, h, dv), c, top);
        static void Table(Room r, float u, float v, Color top, Color chair, int chairs = 2, float size = 1.0f)
        {
            var p = r.p;
            Item(r, r.furniture, u, v, size + .1f, size + .1f, .78f, (mb, P) => { BoxAt(mb, P(0, 0, 0), p, .12f, .72f, .12f, SC(.2f, .2f, .22f)); BoxAt(mb, P(0, 0, .72f), p, size, .06f, size, top); });
            for (int i = 0; i < chairs; i++) { float dv = i == 0 ? -(size * .5f + .45f) : size * .5f + .45f; Chair(r, u, v + dv, chair); }
        }
        static void Chair(Room r, float u, float v, Color c)
        {
            var p = r.p;
            Item(r, r.furniture, u, v, .5f, .5f, .9f, (mb, P) => { foreach (var (a, b) in new[] { (-.2f, -.2f), (.2f, -.2f), (-.2f, .2f), (.2f, .2f) }) BoxAt(mb, P(a, b, 0), p, .05f, .45f, .05f, c * .7f); BoxAt(mb, P(0, 0, .45f), p, .46f, .05f, .46f, c); BoxAt(mb, P(0, .21f, .5f), p, .46f, .45f, .05f, c); });
        }
        static void Counter(Room r, float u, float v, float wu, Color body, Color top, Action<MeshBuilder, Func<float, float, float, Vector3>> extra = null)
        {
            var p = r.p; int n = Mathf.Max(1, Mathf.RoundToInt(wu / 2f)); float seg = wu / n;
            for (int i = 0; i < n; i++)
            {
                float cu = u - wu * .5f + seg * (i + .5f); int k = i;
                Item(r, r.counters, cu, v, seg, .8f, 1.05f, (mb, P) => { BoxAt(mb, P(0, 0, 0), p, seg - .02f, 1.0f, .75f, body); BoxAt(mb, P(0, 0, 1.0f), p, seg, .05f, .8f, top); if (k == 0 && extra != null) extra(mb, P); });
            }
        }
        static void Rack(Room r, float u, float v, float wu, Color[] cloth)
        {
            var p = r.p;
            Item(r, r.displays, u, v, wu, .7f, 1.7f, (mb, P) =>
            {
                foreach (float a in new[] { -wu * .5f + .05f, wu * .5f - .05f }) BoxAt(mb, P(a, 0, 0), p, .05f, 1.65f, .05f, SC(.6f, .6f, .62f));
                BoxAt(mb, P(0, 0, 1.6f), p, wu, .04f, .04f, SC(.6f, .6f, .62f));
                for (float a = -wu * .5f + .25f; a < wu * .5f - .2f; a += .32f) BoxAt(mb, P(a, 0, .55f), p, .08f, 1.0f, .55f, SVary(cloth[Mathf.Abs(Mathf.RoundToInt(a * 7)) % cloth.Length], a, v, .06f));
            });
        }
        static void Pile(Room r, float u, float v, int seed)
        {
            var p = r.p; var cards = new[] { SC(.66f, .50f, .32f), SC(.60f, .45f, .28f), SC(.72f, .56f, .36f) };
            Item(r, r.displays, u, v, 1.4f, 1.2f, 1.6f, (mb, P) =>
            {
                var rnd = new System.Random(seed);
                float y = 0f; for (int i = 0; i < 3; i++) { float s = .9f - i * .15f; BoxAt(mb, P((float)rnd.NextDouble() * .3f - .15f, (float)rnd.NextDouble() * .3f - .15f, y), p, s, .5f, s * .9f, cards[(seed + i) % 3]); y += .5f; }
            });
        }
        static void Shelf(Room r, float u, float v, float wu, Color[] goods)
        {
            var p = r.p;
            Item(r, r.shelves, u, v, wu, .6f, 1.9f, (mb, P) =>
            {
                BoxAt(mb, P(0, 0, 0), p, wu, 1.9f, .5f, SC(.82f, .82f, .80f));
                for (int b = 0; b < 3; b++) BoxAt(mb, P(0, -.03f, .3f + b * .55f), p, wu - .1f, .3f, .5f, goods[b % goods.Length]);
            });
        }
        static void Washer(Room r, float u, float v, bool dryer)
        {
            var p = r.p;
            Item(r, r.appliances, u, v, .75f, .75f, 1.0f, (mb, P) => { BoxAt(mb, P(0, 0, 0), p, .7f, .95f, .7f, SC(.92f, .93f, .94f)); BoxAt(mb, P(0, -.36f, dryer ? .45f : .35f), p, .45f, .45f, .02f, SC(.25f, .30f, .36f)); });
        }
        static void Flowers(Room r, float u, float v, int seed)
        {
            var p = r.p; var cols = new[] { SC(.90f, .20f, .30f), SC(.98f, .80f, .15f), SC(.75f, .35f, .85f), SC(.95f, .50f, .70f), SC(1f, 1f, .95f) };
            Item(r, r.displays, u, v, 1.6f, .8f, 1.3f, (mb, P) =>
            {
                BoxAt(mb, P(0, 0, 0), p, 1.6f, .75f, .8f, SC(.45f, .32f, .22f));
                for (int i = 0; i < 4; i++) { float a = -.6f + i * .4f; BoxAt(mb, P(a, 0, .75f), p, .28f, .3f, .28f, SC(.25f, .40f, .55f)); BoxAt(mb, P(a, 0, 1.05f), p, .36f, .25f, .36f, cols[(seed + i) % cols.Length]); BoxAt(mb, P(a, 0, .98f), p, .3f, .08f, .3f, SC(.2f, .55f, .25f)); }
            });
        }
        static void SalonChair(Room r, float u, float v)
        {
            var p = r.p;
            Item(r, r.furniture, u, v, .8f, .8f, 1.3f, (mb, P) => { BoxAt(mb, P(0, 0, 0), p, .5f, .45f, .5f, SC(.6f, .6f, .62f)); BoxAt(mb, P(0, 0, .45f), p, .7f, .2f, .7f, SC(.12f, .12f, .13f)); BoxAt(mb, P(0, .3f, .65f), p, .7f, .65f, .12f, SC(.12f, .12f, .13f)); });
        }
        // a panel on the back wall's inside (paint, a menu board, a mirror): u, from the floor y0..y1, the shop's back at v
        static void BackPanel(Room r, float u, float wu, float y0, float y1, Color c)
        {
            var p = r.p; float fy = p.D.front_y, v = r.v1 - .02f;
            Vector3 P(float a, float y) { var w = p.W(u + a, v); return new Vector3(w.x, fy + y, w.y); }
            r.deco.Tint = c; Face(r.deco, P(-wu * .5f, y0), P(wu * .5f, y0), P(wu * .5f, y1), P(-wu * .5f, y1), p.Dir3(0, -1));
        }

        static void BuildShopRooms(StuntParkPlan p, Transform root, Materials mats, MeshBuilder deco, Vector2 rowE, Vector2 rowD, float shopW)
        {
            var d = p.D; var sr = ShopRamp(p); var ring = p.Ring; var inward = (p.W(0, 1) - p.W(0, 0)).normalized;
            var rooms = new List<Room>();
            var furniture = new Breakables(); var counters = new Breakables(); var displays = new Breakables(); var shelves = new Breakables(); var appliances = new Breakables();
            for (int k = 0; k < d.shop_fronts; k++)
            {
                var c = rowE + rowD * (shopW * (k + .5f)); var cuv = p.UVof(c);
                float depth = 0f; while (depth < 40f && InPolygon(c + inward * (depth + .25f), ring)) depth += .25f;
                var r = new Room { p = p, cu = cuv.x, u0 = cuv.x - shopW * .5f + .3f, u1 = cuv.x + shopW * .5f - .3f, v0 = cuv.y + WallT + .1f, v1 = cuv.y + depth - WallT, deco = deco,
                    furniture = furniture, counters = counters, displays = displays, shelves = shelves, appliances = appliances };
                rooms.Add(r);
                float u = r.cu, back = r.v1 - 1.2f;
                bool rampHere = sr != null && r.u1 > sr.footU && r.u0 < sr.topU;   // the ramp along its back (k7 to k9): things in the front 8 m only
                Color[] warm = { SC(.75f, .30f, .20f), SC(.95f, .75f, .25f), SC(.30f, .55f, .35f) };
                switch (k)
                {
                    case 0:   // BEAN THERE COFFEE: the counter and its machine, little tables
                        BackPanel(r, u, shopW - 1f, 0f, 3.4f, SC(.42f, .30f, .22f)); BackPanel(r, u, 5f, 2.1f, 3.0f, SC(.15f, .13f, .12f));
                        Counter(r, u - 1f, back, 6f, SC(.36f, .24f, .16f), SC(.80f, .78f, .74f), (mb, P) => { BoxAt(mb, P(.4f, 0, 1.05f), p, .6f, .45f, .45f, SC(.75f, .75f, .78f)); });
                        Table(r, u - 4f, 473.5f, SC(.55f, .40f, .28f), SC(.25f, .18f, .12f), 2, .8f); Table(r, u, 475.5f, SC(.55f, .40f, .28f), SC(.25f, .18f, .12f), 2, .8f); Table(r, u + 4f, 473.5f, SC(.55f, .40f, .28f), SC(.25f, .18f, .12f), 2, .8f);
                        break;
                    case 1:   // PACK & SHIP CENTER: the counter, piles of boxes, a shelf of supplies
                        BackPanel(r, u, shopW - 1f, 0f, 3.4f, SC(.80f, .84f, .92f));
                        Counter(r, u + 2f, back, 6f, SC(.18f, .28f, .50f), SC(.85f, .85f, .82f));
                        Pile(r, u - 4.5f, 473.5f, 1); Pile(r, u - 1.5f, 475.5f, 2); Pile(r, u + 2.5f, 473f, 3); Pile(r, u + 5f, 476f, 4);
                        Shelf(r, u - 4f, back + .4f, 3f, new[] { SC(.66f, .50f, .32f), SC(.85f, .85f, .82f), SC(.2f, .3f, .6f) });
                        break;
                    case 2:   // LUCKY WOK: red and gold, tables, the counter
                        BackPanel(r, u, shopW - 1f, 0f, 3.4f, SC(.62f, .12f, .10f)); BackPanel(r, u, 5f, 2.2f, 3.1f, SC(.95f, .78f, .25f));
                        Counter(r, u, back, 6f, SC(.45f, .10f, .08f), SC(.90f, .72f, .25f));
                        Table(r, u - 4.5f, 474f, SC(.60f, .15f, .12f), SC(.25f, .10f, .08f)); Table(r, u, 475.5f, SC(.60f, .15f, .12f), SC(.25f, .10f, .08f)); Table(r, u + 4.5f, 474f, SC(.60f, .15f, .12f), SC(.25f, .10f, .08f));
                        foreach (float a in new[] { -4f, 0f, 4f }) { var w = p.W(u + a, 476f); SBox(deco, new Vector3(w.x, d.front_y + 3.2f, w.y), p.Dir3(0, 1), new Vector3(.5f, .55f, .5f), SC(.85f, .12f, .10f)); }   // lanterns
                        break;
                    case 3:   // MANE STREET SALON: chairs at the mirrors, the reception desk
                        BackPanel(r, u, shopW - 1f, 0f, 3.4f, SC(.88f, .74f, .80f));
                        foreach (float a in new[] { -4f, -1f, 2f }) { BackPanel(r, u + a, 1.4f, 1.0f, 2.4f, SC(.70f, .80f, .86f)); SalonChair(r, u + a, back + .2f); }
                        Counter(r, u + 4.5f, 474f, 2f, SC(.10f, .10f, .11f), SC(.85f, .70f, .78f));
                        Chair(r, u - 4f, 473.5f, SC(.80f, .45f, .60f)); Chair(r, u - 3f, 473.5f, SC(.80f, .45f, .60f));
                        break;
                    case 4:   // CORNER SLICE PIZZA: the oven and counter to one side, tables; the kicker out of the window in the middle (kept clear)
                        BackPanel(r, u, shopW - 1f, 0f, 3.4f, SC(.92f, .88f, .80f)); BackPanel(r, u - 4f, 3f, .9f, 2.4f, SC(.45f, .22f, .15f));
                        Counter(r, u - 4f, back, 4f, SC(.75f, .20f, .15f), SC(.90f, .88f, .84f));
                        Table(r, u + 4.5f, 476f, SC(.92f, .92f, .90f), SC(.75f, .20f, .15f)); Table(r, u + 4.5f, back - .5f, SC(.92f, .92f, .90f), SC(.75f, .20f, .15f));
                        break;
                    case 5:   // BRIGHT SMILE DENTAL (under the roof gap's atrium): reception, waiting chairs, the patient's chair
                        BackPanel(r, u, shopW - 1f, 0f, 3.4f, SC(.80f, .90f, .94f));
                        Counter(r, u - 4f, back, 4f, SC(.90f, .92f, .94f), SC(.25f, .55f, .70f));
                        foreach (float a in new[] { -2f, -1.2f, -.4f, .4f }) Chair(r, u + a, 474f, SC(.25f, .55f, .70f));
                        Item(r, furniture, u + 3.5f, back - .3f, 1.0f, 2.0f, 1.2f, (mb, P) => { BoxAt(mb, P(0, 0, 0), p, .5f, .5f, .5f, SC(.85f, .86f, .88f)); BoxAt(mb, P(0, 0, .5f), p, .8f, .2f, 1.9f, SC(.20f, .55f, .70f)); BoxAt(mb, P(0, .85f, .7f), p, .8f, .5f, .2f, SC(.20f, .55f, .70f)); BoxAt(mb, P(.6f, -.2f, 1.2f), p, .5f, .08f, .3f, SC(.9f, .9f, .9f)); });
                        break;
                    case 6:   // CHASTAIN CLEANERS: the counter, racks of clothes in bags
                        BackPanel(r, u, shopW - 1f, 0f, 3.4f, SC(.70f, .76f, .86f));
                        Counter(r, u, back, 6f, SC(.20f, .26f, .42f), SC(.85f, .85f, .85f));
                        var bags = new[] { SC(.85f, .87f, .90f), SC(.20f, .22f, .30f), SC(.55f, .15f, .15f), SC(.70f, .65f, .55f) };
                        Rack(r, u - 2f, 473.5f, 7f, bags); Rack(r, u + 1f, 477f, 7f, bags);
                        break;
                    case 7:   // TAQUERIA SOL: bright tables
                        BackPanel(r, u, shopW - 1f, 0f, 3.4f, SC(.98f, .70f, .20f));
                        Counter(r, u + 3f, 477f, 4f, SC(.15f, .45f, .40f), SC(.95f, .55f, .15f));
                        Table(r, u - 4.5f, 474f, SC(.95f, .40f, .20f), SC(.15f, .45f, .40f)); Table(r, u - .5f, 474.5f, SC(.20f, .60f, .85f), SC(.95f, .70f, .20f));
                        break;
                    case 8:   // BLOOM FLORIST: tables of flowers, buckets
                        BackPanel(r, u, shopW - 1f, 0f, 3.4f, SC(.92f, .94f, .88f));
                        Flowers(r, u - 4f, 474f, 0); Flowers(r, u, 475.5f, 2); Flowers(r, u + 4f, 474f, 4);
                        Counter(r, u + 4.5f, 477.2f, 2f, SC(.40f, .55f, .35f), SC(.90f, .90f, .86f));
                        break;
                    case 9:   // SPIN CITY LAUNDRY: washers and dryers in a row, the folding table
                        BackPanel(r, u, shopW - 1f, 0f, 3.4f, SC(.55f, .78f, .90f));
                        for (int i = 0; i < 6; i++) Washer(r, u - 5f + i * .85f, 477f, i % 2 == 1);
                        Table(r, u + 3.5f, 474f, SC(.85f, .85f, .82f), SC(.30f, .50f, .70f), 0, 1.4f);
                        break;
                }
            }
            int Count(Breakables b) => b.Count;
            int total = Count(furniture) + Count(counters) + Count(displays) + Count(shelves) + Count(appliances);
            foreach (var (b, name, drop, sound) in new[] { (furniture, "StuntParkFurniture", .3f, BreakSounds.Kind.Wood), (counters, "StuntParkShopCounters", .4f, BreakSounds.Kind.Wood),
                (displays, "StuntParkDisplays", .3f, BreakSounds.Kind.Wood), (shelves, "StuntParkShelves", .5f, BreakSounds.Kind.Iron), (appliances, "StuntParkAppliances", .4f, BreakSounds.Kind.Iron) })
            {
                if (b.Count == 0) continue;
                var tr = b.ToObjects(name, mats.House, root, drop, sound);
                p.Breakables.AddRange(tr.GetComponentsInChildren<FenceChunk>());
            }
            p.Log.Add($"the shops' rooms: {total} breakable things in {rooms.Count} shops");
        }

        // ---------------------------------------------------------------- the bank's lobby and vault
        static void BuildBankInside(StuntParkPlan p, Transform root, Materials mats, MeshBuilder deco)
        {
            var d = p.D; if (d.bank_vault == null || d.bank_vault.Length < 6) return;
            float vu0 = d.bank_vault[0], vu1 = d.bank_vault[1], vv0 = d.bank_vault[2], vv1 = d.bank_vault[3], dw = d.bank_vault[4], dh = d.bank_vault[5];
            float vh = 4.2f, midV = (vv0 + vv1) * .5f;
            Vector3 P(float u, float v, float y) { var w = p.W(u, v); return new Vector3(w.x, p.FloorUVy(u, v) + y, w.y); }
            var steel = SC(.46f, .48f, .50f); var concrete = SC(.52f, .51f, .49f);
            // the vault's walls (solid): south and north full, west with the door's opening; a ceiling slab
            void WallUV(float u0, float v0, float u1, float v1, float y0, float y1)
            {
                var a = p.W(u0, v0); var b = p.W(u1, v1); var mid = (a + b) * .5f; var along = new Vector3(b.x - a.x, 0, b.y - a.y); float len = along.magnitude; if (len < .05f) return; along /= len;
                var bottom = P((u0 + u1) * .5f, (v0 + v1) * .5f, y0);
                SBox(deco, bottom, along, new Vector3(.4f, y1 - y0, len), concrete, concrete * .9f);
                var g = new GameObject("Stunt park bank vault wall"); g.transform.SetParent(root, false);
                g.transform.SetPositionAndRotation(bottom + Vector3.up * (y1 - y0) * .5f, Quaternion.LookRotation(along)); g.AddComponent<BoxCollider>().size = new Vector3(.4f, y1 - y0, len);
            }
            WallUV(vu0, vv0, vu1, vv0, -.3f, vh); WallUV(vu0, vv1, vu1, vv1, -.3f, vh);
            WallUV(vu0, vv0, vu0, midV - dw * .5f, -.3f, vh); WallUV(vu0, midV + dw * .5f, vu0, vv1, -.3f, vh); WallUV(vu0, midV - dw * .5f, vu0, midV + dw * .5f, dh, vh);
            { var c = P((vu0 + vu1) * .5f, midV, vh); SBox(deco, c, p.Dir3(0, 1), new Vector3(vu1 - vu0 + .4f, .3f, vv1 - vv0 + .4f), concrete, concrete * .9f); }
            // deposit boxes on its inner walls (north and south), a grid of little doors
            foreach (float v in new[] { vv0 + .22f, vv1 - .22f })
                for (float u = vu0 + .5f; u < vu1 - .4f; u += .5f) for (float y = .5f; y < 3.6f; y += .4f)
                { var w = p.W(u, v); deco.Tint = ((int)(u * 2 + y * 2.5f)) % 3 == 0 ? SC(.62f, .55f, .30f) : SC(.55f, .50f, .28f); var c = new Vector3(w.x, p.FloorUVy(u, v) + y, w.y); SBox(deco, c, p.Dir3(0, v < midV ? 1 : -1), new Vector3(.42f, .32f, .03f), deco.Tint); }
            // the vault door: a thick round steel door in the opening (breakable: it bursts open), bolts round its rim, a wheel
            var vault = new Breakables();
            {
                var c = p.W(vu0, midV); var mb = vault.Begin(c); float fy = p.FloorUVy(vu0, midV), rr = Mathf.Min(dw, dh) * .5f - .05f;
                var ax = p.Dir3(1, 0); var mid = new Vector3(c.x, fy + dh * .5f, c.y);
                vault.Piece(() => SCyl(mb, mid - ax * .35f, mid + ax * .15f, rr, steel, 16, SC(.58f, .60f, .62f)), fy + .05f);
                vault.Piece(() => { for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6f; var o = (p.Dir3(0, 1) * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * (rr - .18f); SCyl(mb, mid + o - ax * .42f, mid + o - ax * .35f, .07f, SC(.75f, .72f, .60f), 6); } }, fy + .05f);
                vault.Piece(() => { foreach (var o in new[] { p.Dir3(0, 1), Vector3.up }) SBox(mb, mid - ax * .55f - o * .5f - Vector3.up * .04f, o, new Vector3(.08f, .08f, 1.0f), SC(.70f, .68f, .55f)); SCyl(mb, mid - ax * .5f, mid - ax * .36f, .1f, SC(.70f, .68f, .55f), 8); }, fy + .05f);
                vault.Piece(() => BoxAt(mb, new Vector3(c.x, fy, c.y) - ax * .05f + p.Dir3(0, 1) * (rr + .05f), p, .3f, dh - .2f, .3f, steel * .8f), fy + .05f);   // the hinge post
                vault.End(mid, ax, dw, dh, 1.2f);
            }
            var vr = vault.ToObjects("StuntParkVault", mats.House, root, .45f, BreakSounds.Kind.Iron);
            p.Breakables.AddRange(vr.GetComponentsInChildren<FenceChunk>());
            // cash carts inside, on the line through (breakable: bills fly)
            var cash = new Breakables();
            foreach (float u in new[] { vu0 + 1.8f, vu0 + 4.0f })
            {
                var c = p.W(u, midV + (u < vu0 + 3f ? -.9f : .9f)); var mb = cash.Begin(c); float fy = p.FloorUVy(u, midV);
                var at = new Vector3(c.x, fy, c.y);
                cash.Piece(() => { SBox(mb, at, p.Dir3(0, 1), new Vector3(1.0f, .9f, .6f), SC(.55f, .56f, .58f)); }, fy + .05f);
                cash.Piece(() => { for (int i = 0; i < 6; i++) SBox(mb, at + Vector3.up * (.9f + (i / 3) * .12f) + p.Dir3(1, 0) * (-.3f + (i % 3) * .3f), p.Dir3(0, 1), new Vector3(.26f, .12f, .5f), SC(.35f, .55f, .30f)); }, fy + .05f);
                cash.End(at + Vector3.up * .6f, p.Dir3(0, 1), 1.1f, 1.3f, .8f);
            }
            var cr = cash.ToObjects("StuntParkVaultCash", mats.House, root, .4f, BreakSounds.Kind.Iron);
            p.Breakables.AddRange(cr.GetComponentsInChildren<FenceChunk>());
            // the teller counter across the lobby (breakable, 2 m sections with their glass screens)
            if (d.bank_teller != null && d.bank_teller.Length >= 3)
            {
                var tell = new Breakables(); float tu = d.bank_teller[0], t0 = d.bank_teller[1], t1 = d.bank_teller[2]; int n = Mathf.Max(1, Mathf.RoundToInt((t1 - t0) / 2f)); float seg = (t1 - t0) / n;
                for (int i = 0; i < n; i++)
                {
                    float v = t0 + seg * (i + .5f); var c = p.W(tu, v); var mb = tell.Begin(c); float fy = p.FloorUVy(tu, v); var at = new Vector3(c.x, fy, c.y); var along = p.Dir3(0, 1);
                    tell.Piece(() => { SBox(mb, at, p.Dir3(1, 0), new Vector3(seg - .02f, 1.05f, .7f), SC(.40f, .28f, .20f)); SBox(mb, at + Vector3.up * 1.05f, p.Dir3(1, 0), new Vector3(seg, .06f, .8f), SC(.60f, .58f, .54f)); }, fy + .05f);
                    tell.Piece(() => { mb.Tint = SC(.55f, .66f, .70f); SBox(mb, at + Vector3.up * 1.11f, p.Dir3(1, 0), new Vector3(seg - .1f, .95f, .03f), SC(.55f, .66f, .70f)); }, fy + .05f);
                    tell.End(at + Vector3.up * .9f, p.Dir3(1, 0), seg, 2.0f, .9f);
                }
                var tr = tell.ToObjects("StuntParkTellers", mats.House, root, .4f, BreakSounds.Kind.Glass);
                p.Breakables.AddRange(tr.GetComponentsInChildren<FenceChunk>());
            }
            p.Log.Add($"the bank's inside: the vault ({vu1 - vu0:0.0} x {vv1 - vv0:0.0} m, its round door {dw:0.0} m) and the teller counter");
        }
    }
}
