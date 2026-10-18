using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 41 (A, Dan 2026-10-17 21:17: "Lets not do fake jumps off of ramps ... lets make it go downstairs instead to an underground crypt, you
// emerge out in graveyard with a jump that is made of the stairs of the exit"): THE CRYPT under church 74, a world piece (it is there in Free
// Roam too; the reverse Chastain Ridge race's "Crypt" shortcut runs through it). Replaces Round 40's church leap (MapBuilder.ChurchClimb.cs).
// Every number of its line is in Data/tracks/chastain_ridge.json "crypt" (Tools/mapgen/make_ridge_route.py lays it out under the ground):
//   - church 74's north wing (the front doors, under the steeple) and its spine (the main nave) are hollow: one church inside with pews, the
//     chancel and altar at the spine's south end, a cross; the front doors break (wood); outside it is the church it was;
//   - the aisle from the doors west, then a straight stone stair (driven as a ramp: steps drawn, a smooth collider) down through the nave in a
//     stairwell with a balustrade, under the spine's west wall into the crypt: a vaulted hall turning southwest (pillars, tombs, candles, candle
//     stands and urns that break), then the catacomb (niches in its walls, candles), under the churchyard, the storage row and its lot, a bend
//     to the right under the lot's bank;
//   - the portal in the bank and the exit stair up an open cut into the graveyard: the top of the stair and its stone surround (piers either
//     side) are the launch, a real jump (physics only, the standing rule) over the graveyard and out of its open gate onto New Chastain Road;
//   - the graveyard on the low ground between the lot's bank and New Chastain Road: an iron fence, headstones (the few beside the flight's line
//     break), old trees.
// The ground over the crypt is never lowered: the vault stays under 0.6 m of it (the route script lays the floor from the terrain), holes only
// over the stairwell inside the church and over the open cut.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // a hollow wing of church 74 as BuildChurchWings made its roof (its walls are this file's): by its index in ChurchWings' list for 74
        sealed class ChurchShell { public float x0, z0, x1, z1, floor, eave, top, baseY; public bool ridgeX; public Color wall, roof; }
        static readonly Dictionary<int, ChurchShell> ChurchHollows = new Dictionary<int, ChurchShell>();
        static bool ChurchHollowWing(int building, int wing) => building == 74 && (wing == 0 || wing == 2) && RidgeNow?.Data?.crypt?.line != null && RidgeNow.Data.crypt.line.Length > 4 && RidgeNow.Data.crypt.building == 74;
        const float ChWallT = 0.3f;
        static readonly Color ChPlaster = new Color(0.86f, 0.84f, 0.79f), ChWood = new Color(0.47f, 0.33f, 0.21f), ChWoodDark = new Color(0.33f, 0.23f, 0.15f),
                              ChCarpet = new Color(0.52f, 0.14f, 0.13f), ChFloor = new Color(0.58f, 0.45f, 0.31f), ChCloth = new Color(0.93f, 0.92f, 0.88f),
                              CrStone = new Color(0.47f, 0.45f, 0.41f), CrStoneDark = new Color(0.33f, 0.31f, 0.28f), CrFlag = new Color(0.40f, 0.38f, 0.34f),
                              CrNiche = new Color(0.10f, 0.09f, 0.08f), CrIron = new Color(0.12f, 0.12f, 0.13f), CrGrave = new Color(0.62f, 0.62f, 0.60f), CrCandle = new Color(0.93f, 0.90f, 0.80f);

        sealed class CryptLine
        {
            public List<Vector2> P; public float[] Y, S; public RidgeCrypt D;
            public int N => P.Count;
            public Vector2 T(int k) { var d = P[Mathf.Min(k + 1, N - 1)] - P[Mathf.Max(k - 1, 0)]; return d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.up; }
            public Vector2 Left(int k) { var t = T(k); return new Vector2(-t.y, t.x); }
            public float Half(int k)   // half the floor's width: the hall wider, eased over 3 m either end
            {
                float s = S[k], h = D.lane * .5f, H = D.hall_w * .5f;
                if (s < D.s_west) return h;
                float a = Mathf.InverseLerp(D.s_foot - 4f, D.s_foot - 1f, s), b = 1f - Mathf.InverseLerp(D.s_hall + 1f, D.s_hall + 4f, s);
                return Mathf.Lerp(h, H, Mathf.Clamp01(Mathf.Min(a, b))) + (s >= D.s_bend0 - 3f && s <= D.s_bend1 + 1f ? .5f : 0f);
            }
            public Vector3 At(int k, float lat, float up) { var q = P[k] + Left(k) * lat; return new Vector3(q.x, Y[k] + up, q.y); }
            public int IndexAt(float s) { int b = 0; for (int k = 0; k < N; k++) if (S[k] <= s) b = k; return b; }
        }

        static void BuildChurchCrypt(TerrainGrid terrain, Materials mats, Transform world)
        {
            var cd = RidgeNow?.Data?.crypt; if (cd?.line == null || cd.line.Length < 4) return;
            if (!ChurchHollows.TryGetValue(2, out var nw) || !ChurchHollows.TryGetValue(0, out var sp)) { Debug.LogWarning("[Round41] crypt: church 74's wings were not built hollow"); return; }
            var root = new GameObject("Crypt (church 74)").transform; root.SetParent(world, false);
            var L = new CryptLine { P = ToPoints(cd.line), Y = cd.y, D = cd }; L.S = new float[L.N]; for (int k = 1; k < L.N; k++) L.S[k] = L.S[k - 1] + Vector2.Distance(L.P[k - 1], L.P[k]);
            float F = nw.floor; if (Mathf.Abs(F - L.Y[0]) > .05f) Debug.LogWarning($"[Round41] crypt: the church's floor is {F:0.00} m, the route script laid the line from {L.Y[0]:0.00} m (make_ridge_route.py CR floor)");
            var church = new MeshBuilder(); var deco = new MeshBuilder(); var col = new MeshBuilder();
            float dz = cd.door[1], hw = cd.lane * .5f;
            int kTop = L.IndexAt(cd.s_top), kWest = L.IndexAt(cd.s_west), kPortal = L.IndexAt(cd.s_portal);
            // ---------------- the church inside: the ground under both hollow wings cut under the floor, the floor slabs round the stairwell
            foreach (var sh in new[] { nw, sp }) EditTerrain(terrain, Rect.MinMaxRect(sh.x0 + .2f, sh.z0 + .2f, sh.x1 - .2f, sh.z1 - .2f), (x, z, y) => Mathf.Min(y, F - .6f));
            float sx1 = L.P[kTop].x, sx0 = sp.x0, sz0 = dz - hw, sz1 = dz + hw;   // the stairwell: from the stair's top to the spine's west wall
            var slabs = new List<Rect> {
                Rect.MinMaxRect(nw.x0 - .1f, nw.z0, nw.x1, sz0), Rect.MinMaxRect(nw.x0 - .1f, sz1, nw.x1, nw.z1), Rect.MinMaxRect(sx1, sz0, nw.x1, sz1),
                Rect.MinMaxRect(sp.x0, sp.z0, sp.x1, sz0), Rect.MinMaxRect(sp.x0, sz1, sp.x1, sp.z1) };
            foreach (var r in slabs)
            {
                if (r.width < .05f || r.height < .05f) continue;
                var g = new GameObject("sc church floor"); g.transform.SetParent(root, false); g.transform.position = new Vector3(r.center.x, F - .25f, r.center.y);
                g.AddComponent<BoxCollider>().size = new Vector3(r.width, .5f, r.height);
                deco.Tint = ChFloor; Face(deco, new Vector3(r.xMin, F + .01f, r.yMin), new Vector3(r.xMax, F + .01f, r.yMin), new Vector3(r.xMax, F + .01f, r.yMax), new Vector3(r.xMin, F + .01f, r.yMax), Vector3.up);
            }
            // the stairwell's terrain holes (inside the church; the floor and the walls hide their edges)
            int holed = SetHoles(terrain, Rect.MinMaxRect(sx0 - .5f, sz0 - .5f, sx1 + .5f, sz1 + .5f), q => q.x > sx0 + .1f && q.x < sx1 + .3f && q.y > sz0 - .3f && q.y < sz1 + .3f);
            // ---------------- the walls with their openings (the doors in the north wing's east wall); both faces, a box collider a piece
            float dTop = F + cd.door_h, dz0 = dz - cd.door_w * .5f, dz1 = dz + cd.door_w * .5f;
            int pieces = 0; float nb = Mathf.Min(nw.baseY, F), sb = Mathf.Min(sp.baseY, F);   // the walls from the ground (no foundation band under a hollow wing)
            pieces += ChWall(church, root, new Vector2(nw.x1, nw.z0), new Vector2(nw.x1, nw.z1), Vector2.right, nb, nw.eave, nw, new[] { (dz0 - nw.z0, dz1 - nw.z0, F, dTop) });
            pieces += ChWall(church, root, new Vector2(nw.x0, nw.z1), new Vector2(nw.x1, nw.z1), Vector2.up, nb, nw.eave, nw, null);
            pieces += ChWall(church, root, new Vector2(nw.x0, nw.z0), new Vector2(nw.x1, nw.z0), Vector2.down, nb, nw.eave, nw, null);
            pieces += ChWall(church, root, new Vector2(sp.x0, sp.z0), new Vector2(sp.x0, sp.z1), Vector2.left, sb, sp.eave, sp, new[] { (sz0 - sp.z0, sz1 - sp.z0, sb - 2f, F) });   // the stairwell runs on under it into the vault
            pieces += ChWall(church, root, new Vector2(sp.x0, sp.z1), new Vector2(sp.x1, sp.z1), Vector2.up, sb, sp.eave, sp, null);
            pieces += ChWall(church, root, new Vector2(sp.x0, sp.z0), new Vector2(sp.x1, sp.z0), Vector2.down, sb, sp.eave, sp, null);
            const float SouthWingZ0 = 548.2f, SouthWingZ1 = 562.0f;   // ChurchWings[1] (solid: its own west face is the spine's wall there)
            foreach (var (za, zb) in new[] { (sp.z0, SouthWingZ0), (SouthWingZ1, nw.z0), (nw.z1, sp.z1) })
                if (zb - za > .05f) pieces += ChWall(church, root, new Vector2(sp.x1, za), new Vector2(sp.x1, zb), Vector2.right, sb, sp.eave, sp, null);
            // the roofs' undersides and the gables' insides (the roofs themselves are BuildChurchWings')
            foreach (var sh in new[] { nw, sp })
            {
                Vector2 U = sh.ridgeX ? Vector2.right : Vector2.up, V = sh.ridgeX ? Vector2.up : Vector2.right;
                var c = new Vector2((sh.x0 + sh.x1) * .5f, (sh.z0 + sh.z1) * .5f);
                float hl = (sh.ridgeX ? sh.x1 - sh.x0 : sh.z1 - sh.z0) * .5f, hv = (sh.ridgeX ? sh.z1 - sh.z0 : sh.x1 - sh.x0) * .5f - ChWallT;
                Vector3 W(float a, float b, float y) { var q = c + U * a + V * b; return new Vector3(q.x, y, q.y); }
                float yIn = sh.eave + (sh.top - sh.eave) * (ChWallT / (hv + ChWallT)) - .05f;
                church.Tint = ChPlaster * .92f;
                foreach (float side in new[] { 1f, -1f })
                    Face(church, W(-hl, side * hv, yIn), W(hl, side * hv, yIn), W(hl, 0f, sh.top - .1f), W(-hl, 0f, sh.top - .1f), new Vector3(-V.x * side * .3f, -1f, -V.y * side * .3f));
                church.Tint = ChPlaster;
                foreach (float end in sh == nw ? new[] { 1f } : new[] { 1f, -1f })
                    TriFace(church, W(end * (hl - ChWallT), hv, sh.eave), W(end * (hl - ChWallT), -hv, sh.eave), W(end * (hl - ChWallT), 0f, sh.top - .1f), new Vector3(-U.x * end, 0f, -U.y * end));
            }
            // ---------------- the chancel, the altar and the cross at the spine's south end; pews either side; the carpet up the aisle
            float chZ = sp.z0 + ChWallT + 3.2f, ax = (sp.x0 + sp.x1) * .5f;
            SBox(deco, new Vector3(ax, F, (sp.z0 + ChWallT + chZ) * .5f), Vector3.forward, new Vector3(sp.x1 - sp.x0 - 2f * ChWallT, .5f, chZ - sp.z0 - ChWallT), ChWood, ChCarpet);
            var chancel = new GameObject("church chancel"); chancel.transform.SetParent(root, false); chancel.transform.position = new Vector3(ax, F + .25f, (sp.z0 + ChWallT + chZ) * .5f);
            chancel.AddComponent<BoxCollider>().size = new Vector3(sp.x1 - sp.x0 - 2f * ChWallT, .5f, chZ - sp.z0 - ChWallT);
            var altar = new Vector3(ax, F + .5f, sp.z0 + ChWallT + 1.5f);
            SBox(deco, altar, Vector3.right, new Vector3(1.0f, 1.05f, 1.8f), ChWoodDark, ChCloth); SBox(deco, altar + Vector3.up * 1.05f, Vector3.right, new Vector3(1.1f, .03f, 1.9f), ChCloth);
            var ag = new GameObject("church altar"); ag.transform.SetParent(root, false); ag.transform.position = altar + Vector3.up * .55f; ag.AddComponent<BoxCollider>().size = new Vector3(1.8f, 1.1f, 1.0f);
            deco.Tint = ChWoodDark; float cz = sp.z0 + ChWallT + .02f;
            BoxWorld(deco, new Vector2(ax - .13f, cz), new Vector2(ax + .13f, cz), Vector3.forward, .08f, F + 4.0f, F + 7.6f);
            BoxWorld(deco, new Vector2(ax - 1.1f, cz), new Vector2(ax + 1.1f, cz), Vector3.forward, .08f, F + 6.3f, F + 6.56f);
            int pews = 0;
            void Pew(Vector3 c, Vector3 along, float len, Vector3 back)
            {
                SBox(deco, c, along, new Vector3(.45f, .45f, len), ChWood); SBox(deco, c + back * .2f + Vector3.up * .45f, along, new Vector3(.06f, .5f, len), ChWoodDark);
                var pg = new GameObject("church pew"); pg.transform.SetParent(root, false); pg.transform.SetPositionAndRotation(c + Vector3.up * .45f + back * .05f, Quaternion.LookRotation(along));
                pg.AddComponent<BoxCollider>().size = new Vector3(.6f, .9f, len); pews++;
            }
            // the spine: rows across it facing the altar, an aisle down its middle; south of the stairwell and north of it
            float half = (sp.x1 - sp.x0) * .5f - ChWallT, pl = half - .8f;
            for (float z = chZ + 1.3f; z < sz0 - 1.0f; z += 1.15f) foreach (float side in new[] { -1f, 1f }) { Pew(new Vector3(ax + side * (.7f + pl * .5f), F, z), Vector3.right, pl, Vector3.forward); }
            for (float z = sz1 + 1.4f; z < sp.z1 - ChWallT - .8f; z += 1.15f) foreach (float side in new[] { -1f, 1f }) { Pew(new Vector3(ax + side * (.7f + pl * .5f), F, z), Vector3.right, pl, Vector3.forward); }
            // the north wing: rows beside the stairwell, facing west (the crossing)
            foreach (var (za, zb) in new[] { (nw.z0 + ChWallT + .3f, sz0 - .9f), (sz1 + .9f, nw.z1 - ChWallT - .3f) })
                if (zb - za > 1.2f) for (float x = nw.x0 + 2.0f; x < sx1 - .6f; x += 1.15f) Pew(new Vector3(x, F, (za + zb) * .5f), Vector3.forward, zb - za, Vector3.right);
            deco.Tint = ChCarpet;
            Face(deco, new Vector3(sx1, F + .02f, dz - 1.1f), new Vector3(nw.x1 - .05f, F + .02f, dz - 1.1f), new Vector3(nw.x1 - .05f, F + .02f, dz + 1.1f), new Vector3(sx1, F + .02f, dz + 1.1f), Vector3.up);
            Face(deco, new Vector3(ax - .7f, F + .02f, chZ), new Vector3(ax + .7f, F + .02f, chZ), new Vector3(ax + .7f, F + .02f, sz0 - .1f), new Vector3(ax - .7f, F + .02f, sz0 - .1f), Vector3.up);
            // the balustrade round the stairwell's sides (stone, solid; the top end is open onto the aisle)
            var rail = new MeshBuilder(); rail.Tint = CrStone * 1.15f;
            foreach (float side in new[] { -1f, 1f })
            {
                float zr = dz + side * (hw + .15f);
                BoxWorld(rail, new Vector2(sx0 + ChWallT, zr), new Vector2(sx1, zr), new Vector3(0f, 0f, 1f), .3f, F, F + .95f);
            }
            var rg = rail.ToObject("Crypt stairwell balustrade", mats.House, root, false); AddCollider(rg, rail, "crypt balustrade collider");
            // ---------------- the doors (wood) that break
            var bw = new Breakables(); int leaves = 0;
            for (int k = 0; k < 2; k++)
            {
                float z0 = dz0 + cd.door_w * .5f * k, z1 = z0 + cd.door_w * .5f, x = nw.x1 - ChWallT * .5f;
                var mb = bw.Begin(new Vector2(x, (z0 + z1) * .5f));
                for (int p = 0; p < 3; p++)
                {
                    float za = Mathf.Lerp(z0, z1, p / 3f), zb = Mathf.Lerp(z0, z1, (p + 1) / 3f); int c = p + k;
                    bw.Piece(() => { mb.Tint = c % 2 == 0 ? new Color(.36f, .22f, .13f) : new Color(.30f, .18f, .11f); BoxWorld(mb, new Vector2(x, za + .01f), new Vector2(x, zb - .01f), Vector3.right, .12f, F + .02f, dTop - .02f); }, F + .05f);
                }
                bw.End(new Vector3(x, F + cd.door_h * .5f, (z0 + z1) * .5f), Vector3.right, cd.door_w * .5f, cd.door_h, 1.2f); leaves++;
            }
            bw.ToObjects("Crypt church doors", mats.House, root, .3f, BreakSounds.Kind.Wood);
            // the way in: the lawn in front of the doors up to the floor (a ramp if the floor stands over it)
            {
                float outY = Ground(terrain, nw.x1 + 3.5f, dz), rise = F - outY;
                if (rise > .05f)
                {
                    var pr = new List<float>(); for (float s = 0f; s <= 4.01f; s += .5f) { pr.Add(s); pr.Add(Mathf.Lerp(Ground(terrain, nw.x1 + 4f - s, dz) - .05f, F + .01f, Smooth01(s / 4f))); }
                    var O = new Vector2(nw.x1 + 4f, dz); var axv = Vector2.left; var nl = new Vector2(0f, 1f); float w = cd.door_w * .5f + .4f;
                    BuildPrism(new ScPrism { name = "sc church door ramp", kind = "earth", origin = new[] { O.x, O.y }, axis = new[] { axv.x, axv.y }, profile = pr.ToArray(), colour = new[] { .55f, .53f, .50f }, skirt_edges = new[] { 1, 3 },
                                              poly = new[] { O + nl * w, O - nl * w, O + axv * 4f - nl * w, O + axv * 4f + nl * w }.SelectMany(q => new[] { q.x, q.y }).ToArray() }, terrain, mats, root);
                }
            }
            church.ToObject("Crypt church walls", mats.House, root, false);
            // ---------------- the crypt: the floor (steps drawn on the two stairs, a smooth collider), the stairwell's walls, the vault
            var stones = new SortedDictionary<int, MeshBuilder>(); MeshBuilder StoneAt(int kk) { int key = kk / 24; if (!stones.TryGetValue(key, out var m)) stones[key] = m = new MeshBuilder(); return m; }   // in lengths of 24 m: each lit by its own candles (URP's lights per object)
            var floorCol = new MeshBuilder(); var wallCol = new MeshBuilder(); var flames = new MeshBuilder();
            bool Stair(float s) => (s >= cd.s_top && s <= cd.s_foot) || s >= cd.s_bend1;
            for (int k = kTop; k + 1 < L.N; k++)
            {
                float ha = L.Half(k), hb = L.Half(k + 1);
                Vector3 a0 = L.At(k, ha, 0f), a1 = L.At(k, -ha, 0f), b0 = L.At(k + 1, hb, 0f), b1 = L.At(k + 1, -hb, 0f);
                Face(floorCol, a0, b0, b1, a1, Vector3.up);
                if (!Stair(L.S[k]) || !Stair(L.S[k + 1]))
                {
                    StoneAt(k).Tint = ((k / 2) % 2 == 0 ? CrFlag : CrFlag * .9f); StoneAt(k).Tint = new Color(StoneAt(k).Tint.r, StoneAt(k).Tint.g, StoneAt(k).Tint.b, 1f);
                    Face(StoneAt(k), a0 + Vector3.up * .01f, b0 + Vector3.up * .01f, b1 + Vector3.up * .01f, a1 + Vector3.up * .01f, Vector3.up);
                }
            }
            // the steps: treads 0.4 m deep, level, over the smooth slope (the vehicle rides the slope; the eye sees steps)
            int steps = 0;
            foreach (var (s0, s1) in new[] { (cd.s_top, cd.s_foot), (cd.s_bend1, L.S[L.N - 1]) })
                for (float s = s0; s < s1 - .05f; s += .4f)
                {
                    int k = L.IndexAt(s); float u = Mathf.InverseLerp(L.S[k], L.S[Mathf.Min(k + 1, L.N - 1)], s), u2 = Mathf.InverseLerp(L.S[k], L.S[Mathf.Min(k + 1, L.N - 1)], Mathf.Min(s + .4f, s1));
                    int k2 = L.IndexAt(Mathf.Min(s + .4f, s1));
                    float y = Mathf.Max(YAt(L, s), YAt(L, Mathf.Min(s + .4f, s1))) + .02f; float h = L.Half(k) - .02f;
                    var pa = PosAt(L, s); var pb = PosAt(L, Mathf.Min(s + .4f, s1)); var n = L.Left(k);
                    StoneAt(k).Tint = steps % 2 == 0 ? CrStone : CrStone * .93f;
                    Vector3 V3(Vector2 q, float yy) => new Vector3(q.x, yy, q.y);
                    Face(StoneAt(k), V3(pa + n * h, y), V3(pb + n * h, y), V3(pb - n * h, y), V3(pa - n * h, y), Vector3.up);
                    float yLow = Mathf.Min(YAt(L, s), YAt(L, Mathf.Min(s + .4f, s1))) - .02f; var along = new Vector3(pb.x - pa.x, 0f, pb.y - pa.y);
                    var edge = YAt(L, Mathf.Min(s + .4f, s1)) > YAt(L, s) ? pa : pb;   // the riser on the low side
                    StoneAt(k).Tint = CrStoneDark; Face(StoneAt(k), V3(edge + n * h, yLow), V3(edge - n * h, yLow), V3(edge - n * h, y), V3(edge + n * h, y), YAt(L, Mathf.Min(s + .4f, s1)) > YAt(L, s) ? -along : along);
                    steps++;
                }
            // the stairwell's side walls (in the church: from the stair up to the nave's floor) and the vault (from the spine's west wall to the portal)
            const int Arch = 7;
            int lights = 0, niches = 0, candles = 0; float nextLight = cd.s_foot + 2f;
            var lroot = new GameObject("Crypt candles' light").transform; lroot.SetParent(root, false);
            for (int k = kTop; k + 1 < L.N && k < kPortal; k++)
            {
                float s = L.S[k]; bool inChurch = s < cd.s_west;
                foreach (float side in new[] { 1f, -1f })
                {
                    float ha = L.Half(k) * side, hb = L.Half(k + 1) * side;
                    float topA = inChurch ? F - L.Y[k] : cd.clear - CrRise(L.Half(k)), topB = inChurch ? F - L.Y[k + 1] : cd.clear - CrRise(L.Half(k + 1));
                    // the wall in courses of 1.5 m (the candles light it per vertex)
                    for (float y0 = -.2f; y0 < Mathf.Max(topA, topB) - .01f; y0 += 1.5f)
                    {
                        float ya0 = Mathf.Min(y0, topA), yb0 = Mathf.Min(y0, topB), ya1 = Mathf.Min(y0 + 1.5f, topA), yb1 = Mathf.Min(y0 + 1.5f, topB);
                        StoneAt(k).Tint = ((int)(y0 / 1.5f) + k / 3) % 2 == 0 ? CrStone : CrStone * .9f;
                        var n3 = new Vector3(-L.Left(k).x * side, 0f, -L.Left(k).y * side);
                        Face(StoneAt(k), L.At(k, ha, ya0), L.At(k + 1, hb, yb0), L.At(k + 1, hb, yb1), L.At(k, ha, ya1), n3);
                        Face(wallCol, L.At(k, ha, ya0), L.At(k + 1, hb, yb0), L.At(k + 1, hb, yb1), L.At(k, ha, ya1), n3);
                    }
                    if (!inChurch)
                    {
                        // the arch over it: from the spring line to the crown, both halves
                        for (int i = 0; i < Arch; i++)
                        {
                            float t0 = i / (float)Arch, t1 = (i + 1) / (float)Arch;
                            Vector3 A(int kk, float t) { float h = L.Half(kk), r = CrRise(h); float ang = t * Mathf.PI * .5f; return L.At(kk, side * h * Mathf.Cos(ang), cd.clear - r + r * Mathf.Sin(ang)); }
                            StoneAt(k).Tint = CrStone * (.86f + .04f * (i % 2));
                            var mid = (A(k, t0) + A(k + 1, t1)) * .5f; var inward = L.At(k, 0f, cd.clear * .4f) - mid;
                            Face(StoneAt(k), A(k, t0), A(k + 1, t0), A(k + 1, t1), A(k, t1), inward);
                            Face(wallCol, A(k, t0), A(k + 1, t0), A(k + 1, t1), A(k, t1), inward);
                        }
                        // niches in the catacomb's walls (loculi: dark, in rows of three), a candle in some
                        if (s > cd.s_hall + 2f && s < cd.s_portal - 3f && k % 3 == 0)
                        {
                            var n3 = new Vector3(-L.Left(k).x * side, 0f, -L.Left(k).y * side);
                            for (int row = 0; row < 3; row++)
                            {
                                float y0 = .7f + row * 1.0f;
                                StoneAt(k).Tint = CrNiche;
                                Face(StoneAt(k), L.At(k, side * (L.Half(k) - .02f), y0), L.At(k + 2, side * (L.Half(k + 2) - .02f), y0), L.At(k + 2, side * (L.Half(k + 2) - .02f), y0 + .55f), L.At(k, side * (L.Half(k) - .02f), y0 + .55f), n3);
                                niches++;
                            }
                            if ((k / 3) % 4 == (side > 0 ? 0 : 2)) { Candle(StoneAt(k), flames, L.At(k + 1, side * (L.Half(k + 1) - .25f), 1.25f)); candles++; }
                        }
                    }
                }
                if (!inChurch && s >= nextLight)
                {
                    var lg = new GameObject("crypt light"); lg.transform.SetParent(lroot, false); lg.transform.position = L.At(k, 0f, cd.clear - 1.3f);
                    var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 12f; l.intensity = 1.6f; l.color = new Color(1f, .74f, .45f); l.shadows = LightShadows.None;
                    lights++; nextLight = s + (s < cd.s_hall + 2f ? 9f : 14f);
                }
            }
            // the stairwell lit from over it (the nave's own light does not reach down it)
            foreach (float sl in new[] { cd.s_top + 6f, cd.s_top + 16f, cd.s_west - 3f })
            {
                int k = L.IndexAt(sl); var lg = new GameObject("crypt stairwell light"); lg.transform.SetParent(lroot, false); lg.transform.position = new Vector3(L.P[k].x, F + .8f, L.P[k].y);
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 11f; l.intensity = 1.5f; l.color = new Color(1f, .80f, .55f); l.shadows = LightShadows.None; lights++;
            }
            // where the stairwell meets the vault, under the spine's west wall: the face between the vault's crown and the nave's floor
            {
                int k = kWest; float h = L.Half(k), r = CrRise(h);
                StoneAt(k).Tint = CrStoneDark;
                var dirOut = new Vector3(L.T(k).x, 0f, L.T(k).y);
                for (int i = 0; i < Arch; i++)
                    foreach (float side in new[] { 1f, -1f })
                    {
                        float a0 = i / (float)Arch * Mathf.PI * .5f, a1 = (i + 1) / (float)Arch * Mathf.PI * .5f;
                        var p0 = L.At(k, side * h * Mathf.Cos(a0), cd.clear - r + r * Mathf.Sin(a0)); var p1 = L.At(k, side * h * Mathf.Cos(a1), cd.clear - r + r * Mathf.Sin(a1));
                        Face(StoneAt(k), p0, p1, new Vector3(p1.x, F, p1.z), new Vector3(p0.x, F, p0.z), -dirOut);
                    }
            }
            // ---------------- the hall: pillars along both sides, tombs between them (solid), candles on the tombs, stands and urns that break
            var props = new Breakables(); int tombs = 0, stands = 0, urns = 0;
            for (float s = cd.s_foot + 1f; s < cd.s_hall - 1f; s += 4.2f)
            {
                int k = L.IndexAt(s); float h = L.Half(k);
                foreach (float side in new[] { 1f, -1f })
                {
                    var pc = L.At(k, side * (h - .55f), 0f); SBox(StoneAt(k), pc, new Vector3(L.T(k).x, 0f, L.T(k).y), new Vector3(.8f, cd.clear - .2f, .8f), CrStone * .95f, CrStone);
                    var pg = new GameObject("crypt pillar"); pg.transform.SetParent(root, false); pg.transform.position = pc + Vector3.up * (cd.clear * .5f); pg.AddComponent<BoxCollider>().size = new Vector3(.8f, cd.clear, .8f);
                    if (s + 2.1f < cd.s_hall - 1f)
                    {
                        int k2 = L.IndexAt(s + 2.1f); var tc = L.At(k2, side * (L.Half(k2) - .6f), 0f); var tf = new Vector3(L.T(k2).x, 0f, L.T(k2).y);
                        SBox(StoneAt(k), tc, tf, new Vector3(.95f, .95f, 2.1f), CrStone * 1.05f, CrStone * 1.18f); SBox(StoneAt(k), tc + Vector3.up * .95f, tf, new Vector3(.75f, .12f, 1.9f), CrStone * 1.2f);
                        var tg = new GameObject("crypt tomb"); tg.transform.SetParent(root, false); tg.transform.SetPositionAndRotation(tc + Vector3.up * .53f, Quaternion.LookRotation(tf)); tg.AddComponent<BoxCollider>().size = new Vector3(.95f, 1.07f, 2.1f); tombs++;
                        Candle(StoneAt(k), flames, tc + Vector3.up * 1.07f + tf * .7f); Candle(StoneAt(k), flames, tc + Vector3.up * 1.07f - tf * .6f); candles += 2;
                        // an iron candle stand and an urn on the hall's floor in front of the tomb, where a wide line takes them (they break)
                        var st = L.At(k2, side * (L.Half(k2) - 1.45f), 0f); var smb = props.Begin(new Vector2(st.x, st.z));
                        props.Piece(() => { smb.Tint = CrIron; SBox(smb, st, tf, new Vector3(.08f, 1.45f, .08f), CrIron); SBox(smb, st + Vector3.up * 1.45f, tf, new Vector3(.5f, .05f, .5f), CrIron); }, st.y);
                        props.Piece(() => { Candle(smb, null, st + Vector3.up * 1.5f + tf * .12f); Candle(smb, null, st + Vector3.up * 1.5f - tf * .12f); }, st.y);
                        props.End(st + Vector3.up * .8f, tf, .6f, 1.6f, .6f); stands++;
                        var uc = L.At(L.IndexAt(s + 3.3f), side * (h - 1.45f), 0f); var umb = props.Begin(new Vector2(uc.x, uc.z));
                        props.Piece(() => { umb.Tint = new Color(.55f, .35f, .24f); Blob(umb, uc + Vector3.up * .45f, new Vector3(.32f, .45f, .32f), new Color(.6f, .38f, .25f), new Color(.42f, .26f, .17f), 8, 4); }, uc.y);
                        props.End(uc + Vector3.up * .45f, tf, .7f, .9f, .7f); urns++;
                    }
                }
            }
            props.ToObjects("Crypt stands and urns", mats.House, root, .2f, BreakSounds.Kind.Iron);
            // ---------------- the portal in the bank, the open cut to the lip (its walls up to the ground, coping), the stone surround at the lip
            var cut = new MeshBuilder();
            {
                int k = kPortal; float h = L.Half(k), r = CrRise(h); var t3 = new Vector3(L.T(k).x, 0f, L.T(k).y);
                float gTop = float.MinValue; for (float o = -h - 2f; o <= h + 2f; o += .5f) { var q = L.At(k, o, 0f); gTop = Mathf.Max(gTop, Ground(terrain, q.x, q.z)); }
                float yTop = Mathf.Max(gTop + .35f, L.Y[k] + cd.clear + .7f);
                cut.Tint = CrStone;
                for (int i = 0; i < Arch; i++)
                    foreach (float side in new[] { 1f, -1f })
                    {
                        float a0 = i / (float)Arch * Mathf.PI * .5f, a1 = (i + 1) / (float)Arch * Mathf.PI * .5f;
                        var p0 = L.At(k, side * h * Mathf.Cos(a0), cd.clear - r + r * Mathf.Sin(a0)); var p1 = L.At(k, side * h * Mathf.Cos(a1), cd.clear - r + r * Mathf.Sin(a1));
                        Face(cut, p0, p1, new Vector3(p1.x, yTop, p1.z), new Vector3(p0.x, yTop, p0.z), t3); Face(wallCol, p0, p1, new Vector3(p1.x, yTop, p1.z), new Vector3(p0.x, yTop, p0.z), t3);
                    }
                // the headwall's wings either side of the arch, and its coping
                foreach (float side in new[] { 1f, -1f })
                {
                    var a = L.At(k, side * h, -1.0f); var b = L.At(k, side * (h + 2.2f), -1.0f);
                    Face(cut, a, b, new Vector3(b.x, yTop, b.z), new Vector3(a.x, yTop, a.z), t3); Face(wallCol, a, b, new Vector3(b.x, yTop, b.z), new Vector3(a.x, yTop, a.z), t3);
                }
                BoxAtTint(cut, L.At(k, 0f, 0f) + Vector3.up * (yTop + .15f - L.Y[k]) - t3 * .25f, new Vector3(L.Left(k).x, 0f, L.Left(k).y), 2f * (h + 2.3f), .3f, .7f, CrStone * 1.12f);
            }
            int coped = 0; float wallMost = 0f;
            for (int k = kPortal; k + 1 < L.N; k++)
                foreach (float side in new[] { 1f, -1f })
                {
                    float TopAt(int kk)
                    {
                        float hi = float.MinValue, h0 = L.Half(kk);
                        for (float o = h0 + .3f; o <= h0 + 1.3f; o += .25f) { var q = L.At(kk, side * o, 0f); hi = Mathf.Max(hi, Ground(terrain, q.x, q.z)); }
                        return Mathf.Max(hi + .15f, L.Y[kk] + .55f);   // never under 0.55 m over the stair: the surround a vehicle must keep between
                    }
                    float ta = TopAt(k), tb = TopAt(k + 1); wallMost = Mathf.Max(wallMost, ta - L.Y[k]);
                    Vector3 ia0 = L.At(k, side * L.Half(k), -.3f), ib0 = L.At(k + 1, side * L.Half(k + 1), -.3f);
                    Vector3 ia1 = new Vector3(ia0.x, ta, ia0.z), ib1 = new Vector3(ib0.x, tb, ib0.z);
                    var n3 = new Vector3(-L.Left(k).x * side, 0f, -L.Left(k).y * side);
                    cut.Tint = ((k / 2) % 2 == 0 ? CrStone : CrStone * .92f);
                    Face(cut, ia0, ib0, ib1, ia1, n3); Face(wallCol, ia0, ib0, ib1, ia1, n3);
                    // the outer face where the wall stands over the ground (near the lip), down into it
                    Vector3 oa1 = L.At(k, side * (L.Half(k) + .45f), 0f), ob1 = L.At(k + 1, side * (L.Half(k + 1) + .45f), 0f);
                    float ga = Ground(terrain, oa1.x, oa1.z), gb = Ground(terrain, ob1.x, ob1.z);
                    Vector3 oa = new Vector3(oa1.x, ta, oa1.z), ob = new Vector3(ob1.x, tb, ob1.z);
                    if (ta > ga + .05f || tb > gb + .05f) { Face(cut, new Vector3(oa.x, ga - .3f, oa.z), new Vector3(ob.x, gb - .3f, ob.z), ob, oa, -n3); Face(wallCol, new Vector3(oa.x, ga - .3f, oa.z), new Vector3(ob.x, gb - .3f, ob.z), ob, oa, -n3); }
                    cut.Tint = CrStone * 1.12f; Face(cut, ia1, ib1, ob, oa, Vector3.up); Face(wallCol, ia1, ib1, ob, oa, Vector3.up); coped++;
                }
            // the lip's front (the top step's face down to the graveyard's grass) and the piers either side of it: the stone surround
            {
                int k = L.N - 1; float h = L.Half(k); var t3 = new Vector3(L.T(k).x, 0f, L.T(k).y);
                var a = L.At(k, h + .45f, 0f); var b = L.At(k, -h - .45f, 0f); float ga = Ground(terrain, a.x, a.z) - .3f, gb = Ground(terrain, b.x, b.z) - .3f;
                cut.Tint = CrStoneDark; Face(cut, new Vector3(a.x, ga, a.z), new Vector3(b.x, gb, b.z), b, a, t3); Face(wallCol, new Vector3(a.x, ga, a.z), new Vector3(b.x, gb, b.z), b, a, t3);
                foreach (float side in new[] { 1f, -1f })
                {
                    var pc = L.At(k, side * (h + .75f), 0f) - t3 * .45f; float g0 = Ground(terrain, pc.x, pc.z);
                    SBox(cut, new Vector3(pc.x, g0 - .2f, pc.z), t3, new Vector3(1.1f, L.Y[k] + 1.7f - g0, 1.1f), CrStone * 1.05f, CrStone * 1.2f);
                    SBox(cut, new Vector3(pc.x, L.Y[k] + 1.5f, pc.z), t3, new Vector3(1.3f, .25f, 1.3f), CrStone * 1.2f);
                    var pg = new GameObject("crypt surround pier"); pg.transform.SetParent(root, false); pg.transform.SetPositionAndRotation(new Vector3(pc.x, (g0 + L.Y[k] + 1.75f) * .5f, pc.z), Quaternion.LookRotation(t3));
                    pg.AddComponent<BoxCollider>().size = new Vector3(1.1f, L.Y[k] + 1.75f - g0, 1.1f);
                }
            }
            // the cut's terrain holes: between its walls, from the portal to the lip
            int cutHoles = 0;
            {
                var lo = new Vector2(float.MaxValue, float.MaxValue); var hi = new Vector2(float.MinValue, float.MinValue);
                for (int k = kPortal; k < L.N; k++) { lo = Vector2.Min(lo, L.P[k]); hi = Vector2.Max(hi, L.P[k]); }
                cutHoles = SetHoles(terrain, Rect.MinMaxRect(lo.x - 8f, lo.y - 8f, hi.x + 8f, hi.y + 8f), q =>
                {
                    for (int k = kPortal; k + 1 < L.N; k++)
                    {
                        var a = L.P[k]; var ab = L.P[k + 1] - a; float t = Vector2.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f);
                        if (t < -.05f || t > 1.05f) continue;   // only between the portal and the lip (none in the grass beyond the lip)
                        if (Vector2.Distance(a + ab * Mathf.Clamp01(t), q) < L.Half(k) + .25f) return true;
                    }
                    return false;
                });
            }
            foreach (var kv in stones) { var sg = kv.Value.ToObject("Crypt stone " + kv.Key, mats.House, root, false); sg.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; }
            cut.ToObject("Crypt cut and surround", mats.House, root, false);
            if (flames.Count > 0) { var fg = flames.ToObject("Crypt candle flames", mats.Beacon, root, false, false); }
            var fc = new GameObject("sc crypt floor"); fc.transform.SetParent(root, false); AddCollider(fc, floorCol, "crypt floor collider");
            var wc = new GameObject("crypt walls"); wc.transform.SetParent(root, false); AddCollider(wc, wallCol, "crypt walls collider");
            deco.ToObject("Crypt church inside", mats.House, root, false, false);
            // the cover over the vault, measured after every edit of the ground (the route script laid it 0.6 m under; nothing may have lowered it)
            float least = float.MaxValue; float leastS = 0f;
            for (int k = kWest; k < kPortal; k++)
                foreach (float o in new[] { -1f, 0f, 1f })
                {
                    var q = L.At(k, o * L.Half(k), 0f); float c = Ground(terrain, q.x, q.z) - (L.Y[k] + cd.clear);
                    if (c < least) { least = c; leastS = L.S[k]; }
                }
            BuildGraveyard(terrain, mats, root, L);
            Debug.Log($"[Round41] crypt: church 74's north wing and spine hollow (floor {F:0.00} m): {pieces} wall pieces, doors {cd.door_w:0.0} x {cd.door_h:0.0} m ({leaves} leaves), {pews} pews, {holed} stairwell holes; " +
                      $"the line {L.S[L.N - 1]:0} m from the doors to the lip: {steps} steps drawn, the hall's {tombs} tombs, {stands} candle stands and {urns} urns (break), {niches} niches, {candles} candles, {lights} lights; " +
                      $"the cover over the vault {least:0.00} m at least (at {leastS:0} m); the cut from the portal ({cd.s_portal:0} m) to the lip, its walls up to {wallMost:0.0} m, {cutHoles} holes; the lip at {L.Y[L.N - 1]:0.00} m ({L.P[L.N - 1].x:0.0}, {L.P[L.N - 1].y:0.0})");
        }

        static float CrRise(float half) => Mathf.Min(half * .55f, 2.2f);   // the vault's arch over the walls
        static float YAt(CryptLine L, float s) { int k = L.IndexAt(s); int k2 = Mathf.Min(k + 1, L.N - 1); float u = L.S[k2] > L.S[k] ? Mathf.Clamp01((s - L.S[k]) / (L.S[k2] - L.S[k])) : 0f; return Mathf.Lerp(L.Y[k], L.Y[k2], u); }
        static Vector2 PosAt(CryptLine L, float s) { int k = L.IndexAt(s); int k2 = Mathf.Min(k + 1, L.N - 1); float u = L.S[k2] > L.S[k] ? Mathf.Clamp01((s - L.S[k]) / (L.S[k2] - L.S[k])) : 0f; return Vector2.Lerp(L.P[k], L.P[k2], u); }
        static void BoxAtTint(MeshBuilder mb, Vector3 c, Vector3 along, float len, float h, float w, Color col) { mb.Tint = col; BoxAt(mb, c, along, len, h, w); }
        // a candle: wax (stone's mesh) and its flame (the glowing mesh, or the same when it is a breakable's)
        static void Candle(MeshBuilder wax, MeshBuilder flame, Vector3 at)
        {
            SBox(wax, at, Vector3.forward, new Vector3(.07f, .26f, .07f), CrCandle);
            var f = flame ?? wax; var c = f.Tint;
            Blob(f, at + Vector3.up * .33f, new Vector3(.035f, .07f, .035f), new Color(1f, .85f, .4f), new Color(1f, .55f, .15f), 5, 2);
            f.Tint = c;
        }

        // a wall of the hollow wing from a to b (outward n), from y0 to y1, both faces, with openings (along from a: s0..s1, from ya to yb), box colliders
        static int ChWall(MeshBuilder mb, Transform root, Vector2 a, Vector2 b, Vector2 n, float y0, float y1, ChurchShell sh, (float s0, float s1, float ya, float yb)[] holes)
        {
            float len = Vector2.Distance(a, b); var d = (b - a) / len; int k = 0;
            var marks = new SortedSet<float> { 0f, len }; if (holes != null) foreach (var h in holes) { marks.Add(Mathf.Clamp(h.s0, 0, len)); marks.Add(Mathf.Clamp(h.s1, 0, len)); }
            var m = marks.ToList();
            for (int j = 0; j + 1 < m.Count; j++)
            {
                float s0 = m[j], s1 = m[j + 1], sm = (s0 + s1) * .5f; if (s1 - s0 < .02f) continue;
                var hole = holes?.FirstOrDefault(h => sm > h.s0 && sm < h.s1) ?? default;
                var spans = hole.s1 > hole.s0 ? new[] { (y0, hole.ya), (hole.yb, y1) } : new[] { (y0, y1) };
                foreach (var (ya, yb) in spans) { if (yb - ya < .02f) continue; ChWallPiece(mb, root, a + d * s0, a + d * s1, n, ya, yb, sh); k++; }
                if (hole.s1 > hole.s0)   // the opening's sill, lintel and sides (the wall's thickness)
                {
                    var nn = new Vector3(n.x, 0f, n.y); Vector3 P(float s, float y, float inw) { var q = a + d * s - n * inw; return new Vector3(q.x, y, q.y); }
                    mb.Tint = ChPlaster;
                    Face(mb, P(s0, hole.ya, 0f), P(s1, hole.ya, 0f), P(s1, hole.ya, ChWallT), P(s0, hole.ya, ChWallT), Vector3.up);
                    Face(mb, P(s0, hole.yb, 0f), P(s1, hole.yb, 0f), P(s1, hole.yb, ChWallT), P(s0, hole.yb, ChWallT), Vector3.down);
                }
            }
            if (holes != null)
                foreach (var h in holes)
                    foreach (var (s, sign) in new[] { (h.s0, 1f), (h.s1, -1f) })
                    {
                        var q0 = a + d * s; var q1 = q0 - n * ChWallT; mb.Tint = ChPlaster;
                        Face(mb, new Vector3(q0.x, h.ya, q0.y), new Vector3(q1.x, h.ya, q1.y), new Vector3(q1.x, h.yb, q1.y), new Vector3(q0.x, h.yb, q0.y), new Vector3(d.x * sign, 0f, d.y * sign));
                    }
            return k;
        }
        static void ChWallPiece(MeshBuilder mb, Transform root, Vector2 a, Vector2 b, Vector2 n, float y0, float y1, ChurchShell sh)
        {
            var nn = new Vector3(n.x, 0, n.y); Vector3 A(Vector2 q, float y, float inward) => new Vector3(q.x - n.x * inward, y, q.y - n.y * inward);
            mb.Tint = y1 <= sh.floor + .01f ? BizConcrete : sh.wall; Face(mb, A(a, y0, 0), A(b, y0, 0), A(b, y1, 0), A(a, y1, 0), nn);
            mb.Tint = ChPlaster; Face(mb, A(a, Mathf.Max(y0, sh.floor), ChWallT), A(b, Mathf.Max(y0, sh.floor), ChWallT), A(b, y1, ChWallT), A(a, y1, ChWallT), -nn);
            mb.Tint = sh.wall; Face(mb, A(a, y1, 0), A(b, y1, 0), A(b, y1, ChWallT), A(a, y1, ChWallT), Vector3.up);
            var mid = (a + b) * .5f - n * (ChWallT * .5f);
            var g = new GameObject("church wall"); g.transform.SetParent(root, false);
            g.transform.SetPositionAndRotation(new Vector3(mid.x, (y0 + y1) * .5f, mid.y), Quaternion.LookRotation(new Vector3(b.x - a.x, 0, b.y - a.y).normalized));
            g.AddComponent<BoxCollider>().size = new Vector3(ChWallT, y1 - y0, Vector2.Distance(a, b));
        }

        // ------------------------------------------------------------ the graveyard below the bank
        static void BuildGraveyard(TerrainGrid terrain, Materials mats, Transform parent, CryptLine L)
        {
            var cd = L.D; if (cd.graveyard == null || cd.graveyard.Length < 6) return;
            var root = new GameObject("Graveyard").transform; root.SetParent(parent, false);
            var ring = ToPoints(cd.graveyard); var mb = new MeshBuilder(); var colm = new MeshBuilder();
            int gateSide = Mathf.RoundToInt(cd.gate[0]); var gate = new Vector2(cd.gate[1], cd.gate[2]); const float GateHalf = 4.5f;
            // the cut through the fence (the exit stair rises inside it): no fence within the cut's walls
            bool InCut(Vector2 q)
            {
                for (int k = L.IndexAt(cd.s_portal - 3f); k + 1 < L.N; k++)
                {
                    var a = L.P[k]; var ab = L.P[k + 1] - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    if (Vector2.Distance(a + ab * t, q) < L.Half(k) + 1.4f) return true;
                }
                return false;
            }
            int posts = 0, spans = 0;
            for (int i = 0; i < ring.Count; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % ring.Count]; float len = Vector2.Distance(a, b); int n = Mathf.Max(1, Mathf.CeilToInt(len / 2.4f));
                for (int j = 0; j < n; j++)
                {
                    var p0 = Vector2.Lerp(a, b, j / (float)n); var p1 = Vector2.Lerp(a, b, (j + 1) / (float)n); var pm = (p0 + p1) * .5f;
                    if (i == gateSide && Vector2.Distance(pm, gate) < GateHalf) continue;
                    if (InCut(pm) || InCut(p0) || InCut(p1)) continue;
                    float y0 = Ground(terrain, p0.x, p0.y), y1 = Ground(terrain, p1.x, p1.y);
                    var d3 = new Vector3(p1.x - p0.x, 0f, p1.y - p0.y).normalized; var side = Vector3.Cross(Vector3.up, d3);
                    mb.Tint = CrIron;
                    SBox(mb, new Vector3(p0.x, y0 - .1f, p0.y), d3, new Vector3(.09f, 1.55f, .09f), CrIron); posts++;
                    foreach (float h in new[] { .15f, 1.25f }) { var q0 = new Vector3(p0.x, y0 + h, p0.y); var q1 = new Vector3(p1.x, y1 + h, p1.y); BoxAt(mb, (q0 + q1) * .5f, d3, Vector3.Distance(q0, q1), .05f, .04f); }
                    int pick = Mathf.Max(2, Mathf.RoundToInt(Vector2.Distance(p0, p1) / .16f));
                    for (int q = 1; q < pick; q++) { var pp = Vector2.Lerp(p0, p1, q / (float)pick); float yy = Mathf.Lerp(y0, y1, q / (float)pick); SBox(mb, new Vector3(pp.x, yy + .1f, pp.y), d3, new Vector3(.025f, 1.32f, .025f), CrIron); }
                    var c = new GameObject("graveyard fence"); c.transform.SetParent(root, false);
                    c.transform.SetPositionAndRotation(new Vector3(pm.x, (y0 + y1) * .5f + .7f, pm.y), Quaternion.LookRotation(d3)); c.AddComponent<BoxCollider>().size = new Vector3(.12f, 1.5f, Vector2.Distance(p0, p1));
                    spans++;
                }
            }
            // the gate: two stone piers, its iron leaves standing open outward along the fence
            {
                var a = ring[gateSide]; var b = ring[(gateSide + 1) % ring.Count]; var d = (b - a).normalized; var outward = new Vector2(d.y, -d.x); if (InPolygon(gate + outward * 2f, ring)) outward = -outward;
                foreach (float side in new[] { -1f, 1f })
                {
                    var pc = gate + d * side * GateHalf; float y0 = Ground(terrain, pc.x, pc.y); var d3 = new Vector3(d.x, 0f, d.y);
                    SBox(mb, new Vector3(pc.x, y0 - .2f, pc.y), d3, new Vector3(.6f, 2.3f, .6f), CrGrave * .9f, CrGrave);
                    var pg = new GameObject("graveyard gate pier"); pg.transform.SetParent(root, false); pg.transform.position = new Vector3(pc.x, y0 + .95f, pc.y); pg.AddComponent<BoxCollider>().size = new Vector3(.6f, 2.3f, .6f);
                    var leafDir = new Vector3(outward.x, 0f, outward.y); var h0 = new Vector3(pc.x, y0, pc.y) + leafDir * .4f;
                    for (float u = 0f; u <= 3.2f; u += .16f) SBox(mb, h0 + leafDir * u, d3, new Vector3(.025f, 1.6f, .025f), CrIron);
                    foreach (float h in new[] { .2f, 1.5f }) BoxAt(mb, h0 + leafDir * 1.6f + Vector3.up * h, leafDir, 3.2f, .06f, .05f);
                }
            }
            // the headstones: solid, but the few beside the flight's line, which break
            var brk = new Breakables(); int solid = 0, breaks = 0;
            for (int i = 0; i + 4 < cd.graves.Length; i += 5)
            {
                var p = new Vector2(cd.graves[i], cd.graves[i + 1]); float yaw = cd.graves[i + 2]; int kind = Mathf.RoundToInt(cd.graves[i + 3]); bool breaks1 = cd.graves[i + 4] > .5f;
                float y = Ground(terrain, p.x, p.y); var f = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward; var at = new Vector3(p.x, y - .15f, p.y);
                var tint = CrGrave * (.82f + .06f * ((i / 5) % 4));
                var dims = kind switch { 0 => new Vector3(.75f, 1.05f, .16f), 1 => new Vector3(.18f, 1.35f, .18f), 2 => new Vector3(.45f, 1.9f, .45f), 3 => new Vector3(.9f, .8f, .2f), _ => new Vector3(.5f, .6f, .14f) };
                void Draw(MeshBuilder m)
                {
                    SBox(m, at, f, new Vector3(dims.x, dims.y, dims.z), tint, tint * 1.08f);
                    if (kind == 1) SBox(m, at + Vector3.up * (dims.y * .68f), f, new Vector3(.7f, .17f, .18f), tint, tint * 1.08f);   // the cross's arms
                    if (kind == 2) SBox(m, at + Vector3.up * dims.y, f, new Vector3(.2f, .35f, .2f), tint * 1.05f);                   // the obelisk's tip
                    if (kind != 2) SBox(m, at, f, new Vector3(dims.x + .2f, .2f, Mathf.Max(dims.z + .25f, .5f)), tint * .9f);            // its base
                }
                if (breaks1)
                {
                    var m = brk.Begin(p); brk.Piece(() => Draw(m), y); brk.End(at + Vector3.up * dims.y * .5f, Quaternion.Euler(0f, yaw + 90f, 0f) * Vector3.forward, Mathf.Max(dims.x, .4f), dims.y + .2f, Mathf.Max(dims.z, .4f)); breaks++;
                }
                else
                {
                    Draw(mb); solid++;
                    var g = new GameObject("headstone"); g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(at + Vector3.up * dims.y * .5f, Quaternion.LookRotation(f));
                    g.AddComponent<BoxCollider>().size = new Vector3(kind == 1 ? .7f : dims.x, dims.y, Mathf.Max(dims.z, .2f));
                }
            }
            if (breaks > 0) brk.ToObjects("Graveyard headstones that break", mats.House, root, .2f, BreakSounds.Kind.Wood);
            // old trees: a thick trunk and a wide crown, solid
            int trees = 0;
            for (int i = 0; i + 2 < cd.trees.Length; i += 3)
            {
                var p = new Vector2(cd.trees[i], cd.trees[i + 1]); float r = cd.trees[i + 2], y = Ground(terrain, p.x, p.y);
                var b = new Vector3(p.x, y - .2f, p.y);
                SCyl(mb, b, b + Vector3.up * 6.5f, r, new Color(.30f, .23f, .17f), 8);
                foreach (var (o, s) in new[] { (new Vector3(0f, 8.5f, 0f), 4.6f), (new Vector3(2.2f, 7.4f, 1.0f), 3.2f), (new Vector3(-2.0f, 7.8f, -1.4f), 3.4f), (new Vector3(.6f, 10.2f, -.8f), 3.0f) })
                    Blob(mb, b + o, new Vector3(s, s * .72f, s), new Color(.24f, .38f, .17f), new Color(.15f, .25f, .11f), 9, 5, false, i + s);
                var g = new GameObject("graveyard tree"); g.transform.SetParent(root, false); g.transform.position = b + Vector3.up * 3.4f;
                var cc = g.AddComponent<CapsuleCollider>(); cc.radius = r; cc.height = 7f; trees++;
            }
            mb.ToObject("Graveyard", mats.House, root, false);
            Debug.Log($"[Round41] graveyard: an iron fence of {spans} spans round {ring.Count} sides (its gate open where the jump leaves it, none across the cut), {solid} headstones and {breaks} that break, {trees} old trees");
        }
    }
}
