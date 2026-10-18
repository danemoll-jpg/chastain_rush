using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Round 40 (A7, BUG-007 of session 2026-10-17_12-09-19-449_081d93, Dan: "Lets put a shortcut here....you can drive through the front doors of the
// church, wind you way up top and jump from the steeple area"): church 74's north wing (the one with the main door and the steeple toward Canton
// Road) is hollow when Data/tracks/chastain_ridge.json has the church leap (Tools/mapgen/make_ridge_route.py lays out its line inside). Outside it
// is the church it was (the same walls, roof, gables and steeple; the door is now real double doors and there is a tall window in the south wall
// under the steeple). Inside, simple and readable:
//   - the aisle from the doors west past the pews (the south side) and up a short ramp onto the chancel, the altar in the middle of it;
//   - round the altar and up a ramp along the north wall, round the corner at the east end onto the gallery under the steeple, up it (its last
//     meters steeper, a kicker) to the window: glass that breaks, and out on an aimed flight (GuidedLaunch) curving round onto New Chastain Road;
//   - the decks are solid to the floor but the gallery, which stands on posts over the doors; low solid parapets on their open sides (a vehicle
//     that comes off them drops to the floor and drives on: never a trap).
// The front doors break (wood), the window breaks (glass); both are back for each race (FenceChunk.RestoreAll).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // the wing as BuildChurchWings made its roof: set there (when the wing is hollow its walls are left to this file), built by BuildChurchClimb
        sealed class ChurchShell { public float x0, z0, x1, z1, floor, eave, top, baseY; public Color wall, roof; }
        static ChurchShell ChurchHollow;
        static bool ChurchHollowWing(int building, int wing) => building == 74 && wing == 2 && RidgeNow?.Data?.church != null && RidgeNow.Data.church.building == 74;
        const float ChWallT = 0.3f;
        static readonly Color ChPlaster = new Color(0.86f, 0.84f, 0.79f), ChWood = new Color(0.47f, 0.33f, 0.21f), ChWoodDark = new Color(0.33f, 0.23f, 0.15f),
                              ChCarpet = new Color(0.52f, 0.14f, 0.13f), ChFloor = new Color(0.58f, 0.45f, 0.31f), ChCloth = new Color(0.93f, 0.92f, 0.88f);

        static void BuildChurchClimb(TerrainGrid terrain, Materials mats, Transform world)
        {
            var sh = ChurchHollow; var cd = RidgeNow?.Data?.church; if (sh == null || cd == null) return;
            var root = new GameObject("Church leap (church 74's north wing)").transform; root.SetParent(world, false);
            var shell = new MeshBuilder(); var deco = new MeshBuilder(); var deck = new MeshBuilder(); var rail = new MeshBuilder();
            float F = sh.floor;
            // ---------------- the ground inside cut under the floor (it stood up to 1.1 m over it at the high corner), the floor a slab
            EditTerrain(terrain, Rect.MinMaxRect(sh.x0 + .2f, sh.z0 + .2f, sh.x1 - .2f, sh.z1 - .2f), (x, z, y) => Mathf.Min(y, F - .6f));
            var slab = new GameObject("sc church floor"); slab.transform.SetParent(root, false);
            slab.transform.position = new Vector3((sh.x0 + sh.x1) * .5f, F - .25f, (sh.z0 + sh.z1) * .5f);
            slab.AddComponent<BoxCollider>().size = new Vector3(sh.x1 - sh.x0 - .1f, .5f, sh.z1 - sh.z0 - .1f);
            float ix0 = sh.x0, ix1 = sh.x1 - ChWallT, iz0 = sh.z0 + ChWallT, iz1 = sh.z1 - ChWallT;   // the inside (the west wall is the spine's)
            deco.Tint = ChFloor; Face(deco, new Vector3(ix0, F + .01f, iz0), new Vector3(ix1, F + .01f, iz0), new Vector3(ix1, F + .01f, iz1), new Vector3(ix0, F + .01f, iz1), Vector3.up);
            // ---------------- the walls with their openings: the doors (east), the window (south); both faces, a box collider a piece
            float dz0 = cd.door[1] - cd.door_w * .5f, dz1 = cd.door[1] + cd.door_w * .5f, dTop = F + cd.door_h;
            float wy0 = F + cd.inside_y[cd.inside_y.Length - 1] - .3f, wy1 = Mathf.Min(sh.eave - .15f, wy0 + 3.5f);
            int pieces = 0;
            pieces += ChWall(shell, root, new Vector2(sh.x1, sh.z0), new Vector2(sh.x1, sh.z1), Vector2.right, F, sh.eave, sh, new[] { (dz0 - sh.z0, dz1 - sh.z0, F, dTop) });
            pieces += ChWall(shell, root, new Vector2(sh.x0, sh.z1), new Vector2(sh.x1, sh.z1), Vector2.up, F, sh.eave, sh, null);
            pieces += ChWall(shell, root, new Vector2(sh.x0, sh.z0), new Vector2(sh.x1, sh.z0), Vector2.down, F, sh.eave, sh, new[] { (cd.window[0] - sh.x0, cd.window[1] - sh.x0, wy0, wy1) });
            // the roof's underside and the east gable's inside (the roof itself is BuildChurchWings')
            float zc = (sh.z0 + sh.z1) * .5f;
            shell.Tint = ChPlaster * .92f;
            foreach (float zw in new[] { iz0, iz1 })
            {
                float yw = sh.eave + (sh.top - sh.eave) * (Mathf.Abs(zw - (zw < zc ? sh.z0 : sh.z1)) / (zc - sh.z0)) - .05f;
                Face(shell, new Vector3(ix0, yw, zw), new Vector3(ix1, yw, zw), new Vector3(ix1, sh.top - .1f, zc), new Vector3(ix0, sh.top - .1f, zc), new Vector3(0f, -1f, zw < zc ? 1f : -1f));
            }
            shell.Tint = ChPlaster; TriFace(shell, new Vector3(ix1, sh.eave, iz0), new Vector3(ix1, sh.eave, iz1), new Vector3(ix1, sh.top - .1f, zc), Vector3.left);
            // ---------------- the line inside: its points (every 0.5 m from the doors) and its heights over the floor
            var line = ToPoints(cd.inside); var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Vector2.Distance(line[i - 1], line[i]);
            float H(float s) { var y = cd.inside_y; if (s <= y[0]) return y[1]; for (int i = 0; i + 3 < y.Length; i += 2) if (s <= y[i + 2]) return Mathf.Lerp(y[i + 1], y[i + 3], Mathf.InverseLerp(y[i], y[i + 2], s)); return y[y.Length - 1]; }
            float sK1 = cum[NearestIndex(line, new Vector2(cd.corner[0] + cd.turn_r, cd.corner[1]))];   // the gallery's start (round the corner): from here on posts, not solid
            float hw = cd.lane * .5f;
            // the chancel: a platform 1 m up across the west end, the altar on it in the middle of the turn
            float chX = cd.altar[0] + 2.0f;
            SBox(deck, new Vector3((ix0 + chX) * .5f, F, (iz0 + iz1) * .5f), Vector3.right, new Vector3(iz1 - iz0, 1.0f, chX - ix0), ChWood, ChCarpet);
            var chancel = new GameObject("sc church chancel"); chancel.transform.SetParent(root, false); chancel.transform.position = new Vector3((ix0 + chX) * .5f, F + .5f, (iz0 + iz1) * .5f);
            chancel.AddComponent<BoxCollider>().size = new Vector3(chX - ix0, 1f, iz1 - iz0);
            var altar = new Vector3(cd.altar[0], F + 1f, cd.altar[1]);
            SBox(deco, altar, Vector3.right, new Vector3(1.6f, 1.05f, 1.0f), ChWoodDark, ChCloth); SBox(deco, altar + Vector3.up * 1.05f, Vector3.right, new Vector3(1.7f, .03f, 1.1f), ChCloth);
            var ag = new GameObject("church altar"); ag.transform.SetParent(root, false); ag.transform.position = altar + Vector3.up * .55f; ag.AddComponent<BoxCollider>().size = new Vector3(1.0f, 1.1f, 1.6f);
            // a cross on the west wall over the chancel
            deco.Tint = ChWoodDark;
            BoxWorld(deco, new Vector2(ix0 + .02f, cd.altar[1] - .12f), new Vector2(ix0 + .02f, cd.altar[1] + .12f), Vector3.right, .08f, F + 4.0f, F + 7.4f);
            BoxWorld(deco, new Vector2(ix0 + .02f, cd.altar[1] - 1.0f), new Vector2(ix0 + .02f, cd.altar[1] + 1.0f), Vector3.right, .08f, F + 6.2f, F + 6.45f);
            // the pews: rows facing the altar on the south side of the aisle (solid: a vehicle off the aisle glances off them)
            int pews = 0; float aisleZ = line[NearestIndex(line, new Vector2(chX + 3f, cd.altar[1] - cd.turn_r))].y;
            for (float x = chX + 1.2f; x < ix1 - 7.2f; x += 1.15f)
            {
                var c = new Vector3(x, F, (iz0 + .15f + aisleZ - hw - .4f) * .5f); float len = (aisleZ - hw - .4f) - (iz0 + .15f);
                if (len < 1.2f) break;
                SBox(deco, c, Vector3.forward, new Vector3(.45f, .45f, len), ChWood); SBox(deco, c + Vector3.right * .2f + Vector3.up * .45f, Vector3.forward, new Vector3(.06f, .5f, len), ChWoodDark);
                var pg = new GameObject("church pew"); pg.transform.SetParent(root, false); pg.transform.position = c + Vector3.up * .45f + Vector3.right * .05f;
                pg.AddComponent<BoxCollider>().size = new Vector3(.6f, .9f, len); pews++;
            }
            // the aisle's carpet from the doors to the chancel
            deco.Tint = ChCarpet;
            for (int i = 0; i + 1 < line.Count && line[i + 1].x > chX + .5f; i++)
            {
                var a = line[i]; var b = line[i + 1]; var n = Perp(b - a) * 1.1f;
                Face(deco, new Vector3(a.x + n.x, F + .02f, a.y + n.y), new Vector3(b.x + n.x, F + .02f, b.y + n.y), new Vector3(b.x - n.x, F + .02f, b.y - n.y), new Vector3(a.x - n.x, F + .02f, a.y - n.y), Vector3.up);
            }
            // ---------------- the climb: a deck along the line where it is off the floor (solid to the floor, but the gallery), parapets on its open sides
            var col = new MeshBuilder(); int railSegs = 0;
            bool NearWall(Vector2 q) => q.x < ix0 + .7f || q.x > ix1 - .7f || q.y < iz0 + .7f || q.y > iz1 - .7f;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                float ya = H(cum[i]), yb = H(cum[i + 1]); if (ya < .02f && yb < .02f) continue;
                Vector2 a = line[i], b = line[i + 1], na = Perp(line[Mathf.Min(i + 1, line.Count - 1)] - line[Mathf.Max(i - 1, 0)]) * hw, nb = Perp(line[Mathf.Min(i + 2, line.Count - 1)] - line[i]) * hw;
                Vector3 A(Vector2 q, Vector2 n, float side, float y) => new Vector3(q.x + n.x * side, F + y, q.y + n.y * side);
                deck.Tint = ChWood;
                Face(deck, A(a, na, 1, ya), A(b, nb, 1, yb), A(b, nb, -1, yb), A(a, na, -1, ya), Vector3.up);
                Face(col, A(a, na, 1, ya), A(b, nb, 1, yb), A(b, nb, -1, yb), A(a, na, -1, ya), Vector3.up);
                bool gallery = cum[i] >= sK1 - .25f;
                float under = gallery ? .35f : Mathf.Max(ya, yb) + .05f;   // the gallery a 0.35 m deck on posts, the rest solid down to the floor
                deck.Tint = ChWoodDark;
                foreach (float side in new[] { 1f, -1f })
                {
                    var o = new Vector3((na.x + nb.x) * side, 0f, (na.y + nb.y) * side);
                    Face(deck, A(a, na, side, ya), A(b, nb, side, yb), A(b, nb, side, yb - under), A(a, na, side, ya - under), o);
                    Face(col, A(a, na, side, ya), A(b, nb, side, yb), A(b, nb, side, yb - under), A(a, na, side, ya - under), o);
                    // a parapet where the side is open (not against a wall) and the drop is more than 0.6 m
                    if (Mathf.Min(ya, yb) > .6f && !NearWall(a + na * side) && !NearWall(b + nb * side))
                    {
                        rail.Tint = ChPlaster * .95f;
                        Vector3 ra = A(a, na, side * .97f, ya), rb = A(b, nb, side * .97f, yb);
                        Face(rail, ra, rb, rb + Vector3.up * .9f, ra + Vector3.up * .9f, o); Face(rail, ra, rb, rb + Vector3.up * .9f, ra + Vector3.up * .9f, -o);
                        railSegs++;
                    }
                }
                if (gallery) { deck.Tint = ChWoodDark; Face(deck, A(a, na, 1, ya - under), A(b, nb, 1, yb - under), A(b, nb, -1, yb - under), A(a, na, -1, ya - under), Vector3.down); }
            }
            // the gallery's posts: at its south end, either side of the window (out of the aisle and the doors' way)
            {
                var e = line[line.Count - 1]; var t = (line[line.Count - 1] - line[line.Count - 5]).normalized; var n = Perp(t) * (hw - .2f);
                foreach (float side in new[] { 1f, -1f })
                {
                    var q = e - t * 1.2f + n * side; float y = H(cum[cum.Length - 1] - 1.2f) - .35f;
                    SBox(deck, new Vector3(q.x, F, q.y), Vector3.forward, new Vector3(.3f, y, .3f), ChWoodDark);
                    var pg = new GameObject("church gallery post"); pg.transform.SetParent(root, false); pg.transform.position = new Vector3(q.x, F + y * .5f, q.y); pg.AddComponent<BoxCollider>().size = new Vector3(.3f, y, .3f);
                }
            }
            var dg = deck.ToObject("Church leap decks", mats.House, root, false); AddCollider(dg, col, "sc church climb");
            var rg = rail.ToObject("Church leap parapets", mats.House, root, false); AddCollider(rg, rail, "church parapets collider");
            // ---------------- the doors (wood) and the window (stained glass) that break
            var bw = new Breakables(); var bg = new Breakables(); int leaves = 0, panes = 0;
            for (int k = 0; k < 2; k++)
            {
                float z0 = dz0 + cd.door_w * .5f * k, z1 = z0 + cd.door_w * .5f, x = sh.x1 - ChWallT * .5f;
                var mb = bw.Begin(new Vector2(x, (z0 + z1) * .5f));
                for (int p = 0; p < 3; p++)
                {
                    float za = Mathf.Lerp(z0, z1, p / 3f), zb = Mathf.Lerp(z0, z1, (p + 1) / 3f); int c = p + k;
                    bw.Piece(() => { mb.Tint = c % 2 == 0 ? new Color(.36f, .22f, .13f) : new Color(.30f, .18f, .11f); BoxWorld(mb, new Vector2(x, za + .01f), new Vector2(x, zb - .01f), Vector3.right, .12f, F + .02f, dTop - .02f); }, F + .05f);
                }
                bw.End(new Vector3(x, F + cd.door_h * .5f, (z0 + z1) * .5f), Vector3.right, cd.door_w * .5f, cd.door_h, 1.2f); leaves++;
            }
            {
                float wz = sh.z0 + ChWallT * .5f; Color[] glass = { new Color(.30f, .38f, .62f), new Color(.62f, .24f, .22f), new Color(.82f, .68f, .30f), new Color(.30f, .52f, .36f) };
                for (int k = 0; k < 3; k++)
                {
                    float xa = Mathf.Lerp(cd.window[0], cd.window[1], k / 3f), xb = Mathf.Lerp(cd.window[0], cd.window[1], (k + 1) / 3f);
                    var mb = bg.Begin(new Vector2((xa + xb) * .5f, wz));
                    for (int r = 0; r < 4; r++)
                    {
                        float ya = Mathf.Lerp(wy0, wy1, r / 4f), yb = Mathf.Lerp(wy0, wy1, (r + 1) / 4f); var gc = glass[(k + r) % 4];
                        bg.Piece(() => { mb.Tint = gc; BoxWorld(mb, new Vector2(xa + .02f, wz), new Vector2(xb - .02f, wz), Vector3.forward, .05f, ya + .02f, yb - .02f); }, F + .05f);
                    }
                    bg.End(new Vector3((xa + xb) * .5f, (wy0 + wy1) * .5f, wz), Vector3.forward, xb - xa, wy1 - wy0, 1.2f); panes++;
                }
            }
            bw.ToObjects("Church leap doors", mats.House, root, .3f, BreakSounds.Kind.Wood);
            bg.ToObjects("Church leap window", mats.House, root, .3f, BreakSounds.Kind.Glass);
            // ---------------- the way in: the lawn in front of the doors up to the floor (a ramp if the floor stands over it)
            {
                float outY = Ground(terrain, sh.x1 + 3.5f, cd.door[1]), rise = F - outY;
                if (rise > .05f)
                {
                    var pr = new List<float>(); for (float s = 0f; s <= 4.01f; s += .5f) { pr.Add(s); pr.Add(Mathf.Lerp(Ground(terrain, sh.x1 + 4f - s, cd.door[1]) - .05f, F + .01f, Smooth01(s / 4f))); }
                    var O = new Vector2(sh.x1 + 4f, cd.door[1]); var ax = Vector2.left; var nl = new Vector2(0f, 1f); float w = cd.door_w * .5f + .4f;
                    BuildPrism(new ScPrism { name = "sc church door ramp", kind = "earth", origin = new[] { O.x, O.y }, axis = new[] { ax.x, ax.y }, profile = pr.ToArray(), colour = new[] { .55f, .53f, .50f }, skirt_edges = new[] { 1, 3 },
                                              poly = new[] { O + nl * w, O - nl * w, O + ax * 4f - nl * w, O + ax * 4f + nl * w }.SelectMany(q => new[] { q.x, q.y }).ToArray() }, terrain, mats, root);
                }
            }
            // ---------------- the launch out of the window
            var lip = new Vector3(cd.lip[0], F + cd.inside_y[cd.inside_y.Length - 1], cd.lip[1]);
            var tg = new Vector3(cd.target[0], Ground(terrain, cd.target[0], cd.target[1]), cd.target[1]);
            var gl = new GameObject("Church leap launch").AddComponent<GuidedLaunch>(); gl.transform.SetParent(root, false);
            gl.gravity = cd.gravity > 0f ? cd.gravity : 1f; gl.id = "church"; gl.lip = lip; gl.dir = new Vector3(cd.lip_dir[0], 0f, cd.lip_dir[1]).normalized; gl.halfWidth = hw; gl.angle = cd.angle_deg;
            gl.target = tg; gl.targetDir = new Vector3(cd.target_dir[0], 0f, cd.target_dir[1]).normalized;
            // the two bends held (ChurchTurn): round the altar, and round the corner onto the gallery
            foreach (var (c, a0, a1, y0, y1, nm) in new[] { (cd.altar, 180f, 360f, H(cum[NearestIndex(line, new Vector2(cd.altar[0], cd.altar[1] - cd.turn_r))]), H(cum[NearestIndex(line, new Vector2(cd.altar[0], cd.altar[1] + cd.turn_r))]), "round the altar"),
                                                           (cd.corner, 0f, 90f, H(cum[NearestIndex(line, new Vector2(cd.corner[0], cd.corner[1] + cd.turn_r))]), H(cum[NearestIndex(line, new Vector2(cd.corner[0] + cd.turn_r, cd.corner[1]))]), "onto the gallery") })
            {
                var tg2 = new GameObject("Church leap bend " + nm).AddComponent<ChurchTurn>(); tg2.transform.SetParent(root, false);
                tg2.center = new Vector3(c[0], F, c[1]); tg2.radius = cd.turn_r; tg2.halfLane = hw; tg2.a0 = a0; tg2.a1 = a1; tg2.yLow = y0; tg2.yHigh = y1;
            }
            shell.ToObject("Church leap walls", mats.House, root, false);
            deco.ToObject("Church leap inside", mats.House, root, false, false);
            Debug.Log($"[Round40] church leap: church 74's north wing hollow (floor {F:0.00} m, walls to {sh.eave - F:0.0} m, ridge {sh.top - F:0.0} m): {pieces} wall pieces, doors {cd.door_w:0.0} x {cd.door_h:0.0} m ({leaves} leaves), " +
                      $"the window {cd.window[1] - cd.window[0]:0.0} m wide from {wy0 - F:0.0} to {wy1 - F:0.0} m ({panes} panes), {pews} pews, {railSegs} parapet pieces; the lip at {lip.y - F:0.0} m ({lip.x:0.0}, {lip.z:0.0}), " +
                      $"a {cd.angle_deg:0} deg flight to ({tg.x:0.0}, {tg.y:0.0}, {tg.z:0.0})");
        }

        static Vector2 Perp(Vector2 d) { d = d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.up; return new Vector2(-d.y, d.x); }
        static int NearestIndex(List<Vector2> p, Vector2 q) { int b = 0; float bd = float.MaxValue; for (int i = 0; i < p.Count; i++) { float d = (p[i] - q).sqrMagnitude; if (d < bd) { bd = d; b = i; } } return b; }

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
    }
}
