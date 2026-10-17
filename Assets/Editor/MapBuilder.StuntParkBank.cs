using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEditor;
using UnityEngine;

// Round 38 (Part A, BUG-001 of session 2026-10-16_21-19-50-140_b75298: "Can we make this bank part of the course as well because otherwise this ramp
// is kind of useless the way it is positioned"): the abandoned bank is the stunt park's. Built here instead of MapBuilder.ShoppingCenter's solid
// block when Data/tracks/stunt_park.json has the bank (Tools/mapgen/make_stunt_park.py, which also lays out K1, the BANK HOP, and sizes the BANK DROP):
//   - its walls 0.4 m thick round its footprint, up to a parapet over its roof (0.6 m on the west, where the hop comes in over it, 1.1 m elsewhere;
//     none where the BANK DROP leaves), boarded windows on them
//     as before, the BANK sign (letters missing) over its front doors, the FOR LEASE banner over its drive-up bay;
//   - its front doors (west) and the drive-up bay (east) boarded with plywood that breaks (Kind.Wood; back with the park's other breakables), so a
//     line runs straight through it: the bus gap's run-out, in at the doors, out of the bay, on round to the half-pipe;
//   - inside, one tall hall (the pad is graded to the park's floor; the floor drawn in old terrazzo tiles), a few bare work lamps; Part C dresses it;
//   - its roof a slab you can ride (the K1 hop lands on it), the BANK DROP's kicker on its north edge (a park ramp on base "bank").
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly Color BankOut = SC(.60f, .57f, .52f), BankIn = SC(.70f, .67f, .62f), BankFloorA = SC(.62f, .58f, .52f), BankFloorB = SC(.54f, .50f, .45f);

        static void BuildParkBank(MapData map, TerrainGrid t, Materials mats, Transform parent, StuntParkPlan p, Material parkSigns)
        {
            var d = p.D; var root = new GameObject("Stunt park bank").transform; root.SetParent(parent, false);
            var ring = Pairs(d.bank_ring).Select(q => p.W(q.x, q.y)).ToList();
            var shell = new MeshBuilder(); var deco = new MeshBuilder();
            float roof = d.bank_roof, top = roof + d.bank_parapet, lowest = ring.Min(q => p.FloorAt(q)), baseY = lowest - .8f;
            float FloorAtW(Vector2 w) => p.FloorAt(w);
            // ---------------- the openings (the doors) and the parapet's gap (the BANK DROP's lip)
            var openings = new List<(Vector2 a, Vector2 b, float h)>();
            for (int i = 0; i + 4 < (d.bank_doors?.Length ?? 0); i += 5)
            {
                float u = d.bank_doors[i + 1], v = d.bank_doors[i + 2], w = d.bank_doors[i + 3], h = d.bank_doors[i + 4];
                openings.Add((p.W(u, v - w / 2f), p.W(u, v + w / 2f), h));
            }
            var gaps = new List<(Vector2 a, Vector2 b)>();
            foreach (var f in d.features.Where(f => f.kind == "bigjump" && f.@base == "bank"))
            {
                var dir = new Vector2(f.du, f.dv); var right = new Vector2(dir.y, -dir.x); float hw = f.width / 2f + .5f;
                gaps.Add((p.W(f.lip_u + right.x * hw, f.lip_v + right.y * hw), p.W(f.lip_u - right.x * hw, f.lip_v - right.y * hw)));
            }
            // ---------------- the walls: full height to the parapet's top; under an opening nothing, over it the lintel; under the gap only to the roof
            int pieces = 0;
            var faces = new List<(Vector2 a, Vector2 b, Vector2 n, List<(float s0, float s1)> holes)>();
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count]; float len = Vector2.Distance(a, b); if (len < .05f) continue;
                var dd = (b - a) / len; var n = new Vector2(dd.y, -dd.x); if (InPolygon((a + b) * .5f + n * .2f, ring)) n = -n;
                float S(Vector2 q) => Vector2.Dot(q - a, dd);
                bool OnEdge(Vector2 q) => Mathf.Abs(Vector2.Dot(q - a, n)) < .8f && S(q) > -.5f && S(q) < len + .5f;
                var cuts = new List<(float s0, float s1, int kind, float h)>();
                foreach (var o in openings) if (OnEdge(o.a) && OnEdge(o.b)) cuts.Add((Mathf.Min(S(o.a), S(o.b)), Mathf.Max(S(o.a), S(o.b)), 0, o.h));
                foreach (var g in gaps) if (OnEdge(g.a) && OnEdge(g.b)) cuts.Add((Mathf.Clamp(Mathf.Min(S(g.a), S(g.b)), 0, len), Mathf.Clamp(Mathf.Max(S(g.a), S(g.b)), 0, len), 1, 0));
                var marks = new SortedSet<float> { 0f, len }; foreach (var c in cuts) { marks.Add(Mathf.Clamp(c.s0, 0, len)); marks.Add(Mathf.Clamp(c.s1, 0, len)); }
                var m = marks.ToList(); var holes = new List<(float, float)>();
                for (int j = 0; j + 1 < m.Count; j++)
                {
                    float s0 = m[j], s1 = m[j + 1]; if (s1 - s0 < .02f) continue; float sm = (s0 + s1) * .5f;
                    var open = cuts.FirstOrDefault(c => c.kind == 0 && sm > c.s0 && sm < c.s1); bool isOpen = open.s1 > open.s0;
                    bool gap = cuts.Any(c => c.kind == 1 && sm > c.s0 && sm < c.s1);
                    float pieceTop = gap ? roof : n.x < -.5f ? top : roof + 1.1f, floorHere = FloorAtW(a + dd * sm);   // the parapet: 1.1 m but on the west (the hop's way in, 0.6 m)
                    if (isOpen) holes.Add((s0, s1));
                    var spans = isOpen ? new[] { (baseY, floorHere - .02f), (floorHere + open.h, pieceTop) } : new[] { (baseY, pieceTop) };
                    foreach (var (y0, y1) in spans) { BankWall(shell, root, a + dd * s0, a + dd * s1, n, y0, y1, pieceTop); pieces++; }
                }
                faces.Add((a, b, n, holes));
            }
            // ---------------- the roof: a slab you can ride ("Stunt park roof", as the stores': VehicleSurfaceContacts knows its seams)
            var rects = StoreRaster(p, ring, .5f, (u, v) => true);
            foreach (var r in rects) SlabRect(p, shell, root, r, roof, SlabT, SC(.40f, .40f, .41f), SC(.62f, .60f, .56f), "Stunt park roof", false);
            // ---------------- the floor inside: the graded ground, drawn in old tiles (no collider: the terrain is the floor)
            var uvRing = ring.Select(p.UVof).ToList(); float u0 = uvRing.Min(q => q.x), u1 = uvRing.Max(q => q.x), v0 = uvRing.Min(q => q.y), v1 = uvRing.Max(q => q.y);
            for (float u = u0; u < u1 - .01f; u += 2f)
                for (float v = v0; v < v1 - .01f; v += 2f)
                {
                    float ua = u, ub = Mathf.Min(u1, u + 2f), va = v, vb = Mathf.Min(v1, v + 2f);
                    var mid = p.W((ua + ub) * .5f, (va + vb) * .5f); if (!InPolygon(mid, ring) || RingDist(mid, ring) < .3f) continue;
                    Vector3 P(float uu, float vv) { var w = p.W(uu, vv); return new Vector3(w.x, p.FloorUVy(uu, vv) + .03f, w.y); }
                    deco.Tint = (Mathf.FloorToInt(u / 2f) + Mathf.FloorToInt(v / 2f)) % 2 == 0 ? BankFloorA : BankFloorB;
                    Face(deco, P(ua, va), P(ub, va), P(ub, vb), P(ua, vb), Vector3.up);
                }
            // ---------------- boarded windows on the outside, two floors, as the old bank (none over the doors)
            var ply = SC(.60f, .48f, .32f); var plyDark = SC(.47f, .37f, .24f); var grime = SC(.40f, .38f, .35f);
            int boards = 0;
            foreach (var (a, b, n, holes) in faces)
            {
                float len = Vector2.Distance(a, b); if (len < 4f) continue; var dd = (b - a) / len;
                int count = Mathf.FloorToInt((len - 1f) / 3.6f);
                for (int k = 0; k < count; k++)
                {
                    float s = (k + .5f) * len / count; var mpt = a + dd * s;
                    var f = MakeFrame(new Vector3(mpt.x, FloorAtW(mpt + n), mpt.y), n, 1f);
                    foreach (float y in new[] { 1.0f, 5.2f })
                    {
                        if (y < 4f && holes.Any(h => s > h.Item1 - 1.2f && s < h.Item2 + 1.2f)) continue;
                        FrontQuad(deco, f, -.85f, .85f, y - .1f, y + 1.9f, .02f, grime);
                        FrontQuad(deco, f, -.75f, .75f, y, y + 1.8f, .04f, (k + boards) % 3 == 0 ? plyDark : ply);
                        FrontQuad(deco, f, -.78f, .78f, y + .55f, y + .68f, .05f, plyDark);
                        boards++;
                    }
                }
            }
            // ---------------- the boards over the doors: plywood sheets that break (2 sheets a door, each in 3 boards), studs behind
            var bw = new Breakables(); int sheets = 0;
            foreach (var o in openings)
            {
                var face = faces.FirstOrDefault(fc => Mathf.Abs(Vector2.Dot(o.a - fc.a, fc.n)) < .8f && Mathf.Abs(Vector2.Dot(o.b - fc.a, fc.n)) < .8f);
                var n = face.n; float len = Vector2.Distance(o.a, o.b); var dd = (o.b - o.a) / len;
                for (int k = 0; k < 2; k++)
                {
                    var q0 = o.a + dd * (len * k / 2f); var q1 = o.a + dd * (len * (k + 1) / 2f); var mid = (q0 + q1) * .5f - n * (WallT * .5f);
                    float fy = FloorAtW(mid); var mb = bw.Begin(mid);
                    for (int sx = 0; sx < 3; sx++)
                    {
                        var a0 = Vector2.Lerp(q0, q1, sx / 3f) + n * .02f; var a1 = Vector2.Lerp(q0, q1, (sx + 1) / 3f) + n * .02f; int col = sx + k;
                        bw.Piece(() => { mb.Tint = col % 3 == 0 ? plyDark : ply; BoxWorld(mb, a0, a1, new Vector3(n.x, 0, n.y), .05f, fy, fy + o.h - .02f); }, fy + .05f);
                    }
                    var ma = Vector2.Lerp(q0, q1, .02f) + n * .06f; var mb2 = Vector2.Lerp(q0, q1, .98f) + n * .06f;
                    bw.Piece(() => { mb.Tint = plyDark; BoxWorld(mb, ma, mb2, new Vector3(n.x, 0, n.y), .06f, fy + 1.0f, fy + 1.15f); }, fy + .05f);   // a batten across
                    bw.End(new Vector3(mid.x, fy + o.h * .5f, mid.y), new Vector3(n.x, 0, n.y), len / 2f, o.h, 1.2f); sheets++;
                }
            }
            var br = bw.ToObjects("StuntParkBankBoards", mats.House, root, .3f, BreakSounds.Kind.Wood);
            p.Breakables.AddRange(br.GetComponentsInChildren<FenceChunk>());
            // ---------------- the signs: BANK over the front doors (the store-sign atlas, letters missing), FOR LEASE over the bay, BANK HOP on the west wall
            var storeSigns = AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir + "/StoreSigns.mat");
            foreach (var o in openings)
            {
                var face = faces.FirstOrDefault(fc => Mathf.Abs(Vector2.Dot(o.a - fc.a, fc.n)) < .8f && Mathf.Abs(Vector2.Dot(o.b - fc.a, fc.n)) < .8f);
                var mid = (o.a + o.b) * .5f; var f = MakeFrame(new Vector3(mid.x, FloorAtW(mid + face.n), mid.y), face.n, 1f);
                bool front = face.n.x < 0f;   // the west face
                if (storeSigns == null) continue;
                if (front) { float y = 7.6f; BoxF(deco, f, -5.3f, 5.3f, y - .55f, y + .55f, .02f, .15f, grime); AtlasPlate(root, f.P(0f, y, .16f), -f.F, 10f, .78f, SignBank, storeSigns); }
                else AtlasPlate(root, f.P(0f, o.h + 1.0f, .09f), -f.F, 7f, .55f, SignLease, storeSigns);
            }
            if (parkSigns != null)
            {
                var hop = d.features.FirstOrDefault(f => f.kind == "bigjump" && f.id == "K1");
                var west = faces.Where(fc => fc.n.x < -.5f).OrderByDescending(fc => Vector2.Distance(fc.a, fc.b)).FirstOrDefault();
                if (hop != null && west.b != west.a)
                {
                    var at = p.W(p.UVof(west.a).x, hop.lip_v); var f = MakeFrame(new Vector3(at.x, FloorAtW(at + west.n), at.y), west.n, 1f);
                    BoxF(deco, f, -2.8f, 2.8f, 4.6f - .38f, 4.6f + .38f, .02f, .1f, SBlack);
                    AtlasPlate(root, f.P(0f, 4.6f, .11f), -f.F, 5.4f, .42f, SignBankHop, parkSigns);
                }
            }
            // (Round 38: no berm along the east edge: the check found it a kicker that threw a fast vehicle over the parapet; the parapet is 1.1 m there)
            if (false)
            {
                var east = faces.Where(fc => fc.n.x > .5f).Select(fc => (p.UVof(fc.a), p.UVof(fc.b))).ToList();
                float eu = east.Count > 0 ? east.Min(e => Mathf.Min(e.Item1.x, e.Item2.x)) : u1;
                var berm = new SProf(); berm.Arc(3f, 50f).Flat(.4f); var bp = berm.pts.ToList(); bp.Add(new Vector2(berm.s, 0f));
                float startU = eu - WallT - berm.s - .05f, vlo = v0 + WallT + .1f, vhi = v1 - WallT - .1f;
                foreach (var f in d.features.Where(f => f.kind == "ramp" && f.@base == "bank")) vhi = Mathf.Min(vhi, f.v - .6f);   // it stops short of the BANK DROP's kicker
                ParkRamp(p, shell, bp, startU, (vlo + vhi) * .5f, 1, 0, vhi - vlo, "ply", "bank", root, "bank roof berm");
            }
            // ---------------- bare work lamps under the roof (the ceiling lights' material: always lit)
            var lampMat = AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir + "/StuntParkCeilingLight.mat");
            if (lampMat != null)
            {
                var lamps = new MeshBuilder();
                foreach (var (lu, lv) in new[] { (3600f, 366f), (3601f, 378f), (3600.5f, 390f) })
                {
                    var w = p.W(lu, lv); var hang = new Vector3(w.x, roof - SlabT, w.y);
                    SBox(deco, hang - Vector3.up * 1.6f, p.Dir3(1, 0), new Vector3(.03f, 1.6f, .03f), SBlack);
                    SBox(lamps, hang - Vector3.up * 1.85f, p.Dir3(1, 0), new Vector3(.22f, .25f, .22f), Color.white);
                }
                lamps.ToObject("StuntParkBankLamps", lampMat, root, false, false);
            }
            BuildBankInside(p, root, mats, deco);   // Part C: the lobby's teller counter and the vault (MapBuilder.StuntParkShops.cs)
            shell.ToObject("StuntParkBankShell", mats.House, root, false, true);
            deco.ToObject("StuntParkBankDeco", mats.House, root, false, false);
            p.Log.Add($"the bank: {pieces} wall pieces, roof {roof:0.00} m ({roof - lowest:0.0} m up), {rects.Count} roof slabs, {sheets} breakable sheets over {openings.Count} doors, {boards} boarded windows");
        }

        // a wall piece of the bank (MapBuilder.StuntParkStores.cs WallPiece in the bank's colours; a box collider, "Stunt park bank wall")
        static void BankWall(MeshBuilder mb, Transform root, Vector2 a, Vector2 b, Vector2 n, float y0, float y1, float top)
        {
            if (y1 - y0 < .02f) return;
            var nn = new Vector3(n.x, 0, n.y); Vector3 A(Vector2 q, float y, float inward) => new Vector3(q.x - n.x * inward, y, q.y - n.y * inward);
            var along = new Vector3(b.x - a.x, 0, b.y - a.y).normalized;
            mb.Tint = SVary(BankOut, a.x, a.y, .03f); Face(mb, A(a, y0, 0), A(b, y0, 0), A(b, y1, 0), A(a, y1, 0), nn);
            mb.Tint = BankIn; Face(mb, A(a, y0, WallT), A(b, y0, WallT), A(b, y1, WallT), A(a, y1, WallT), -nn);
            mb.Tint = Mathf.Abs(y1 - top) < .01f ? SC(.52f, .50f, .47f) : BankOut; Face(mb, A(a, y1, 0), A(b, y1, 0), A(b, y1, WallT), A(a, y1, WallT), Vector3.up);
            mb.Tint = BankOut * .92f; Face(mb, A(a, y0, 0), A(b, y0, 0), A(b, y0, WallT), A(a, y0, WallT), Vector3.down);
            mb.Tint = BankOut * .95f; Face(mb, A(a, y0, 0), A(a, y0, WallT), A(a, y1, WallT), A(a, y1, 0), -along); Face(mb, A(b, y0, 0), A(b, y0, WallT), A(b, y1, WallT), A(b, y1, 0), along);
            var mid = (a + b) * .5f - n * (WallT * .5f);
            var g = new GameObject("Stunt park bank wall"); g.transform.SetParent(root, false);
            g.transform.SetPositionAndRotation(new Vector3(mid.x, (y0 + y1) * .5f, mid.y), Quaternion.LookRotation(along));
            g.AddComponent<BoxCollider>().size = new Vector3(WallT, y1 - y0, Vector2.Distance(a, b));
        }
    }
}
