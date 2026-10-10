using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 8, item 4: the official shortcuts' physical side, built from Data/shortcuts/chastain_glen_loop.json
// (Tools/mapgen/make_shortcuts.py). The race side (entry plane, forgiven checkpoints) is the route file's official_shortcuts.
//   prisms        ramps, kickers, landing decks, floors and stairs: solid, with mesh colliders (the car drives on them).
//                 Round 10: a "held" prism keeps the car on its top (ShortcutSurface + RampHold: a rounded ramp top the car
//                 would otherwise fly off at speed); under a "cut" prism the terrain is lowered to its top first
//   open houses   a house the line goes THROUGH. Round 10: the house is built whole (Dan: "it simply has to restore the
//                 houses"); only the two wall panels where the line crosses its walls are taken out of its mesh, exactly as
//                 they were (wall, windows, trim), and become breakable panels like the fences (pieces fly, a little speed,
//                 whole again at race start). Behind them a plain room along the line; the house's box collider is replaced
//                 by boxes that leave the line open
//   jump houses   a house the line goes OVER: it keeps its box and also gets a collider from its own mesh, so a car that comes
//                 up short lands on the sloped roof and slides off it instead of sitting on a flat box top
//   trail         Round 11: none (Dan: "I don't want the dirt path"; shortcuts are found, not signposted). The yard is plain grass.
//   corridor      trees, lamps, hydrants, mailboxes and front-yard shrubs are kept off the line; fences stay (they break)
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class ScPrism { public string name, kind; public float[] poly, origin, axis, profile, colour, bank; public int[] skirt_edges; public bool held, cut; }   // bank: (station, rise per metre to the left) pairs
        // Round 10: a and b are where the line crosses the house's walls (in, out); wall_a / wall_b: a point on that wall and its
        // outward normal (x, z, nx, nz); y0 / y1: the height range of the panel that breaks in each wall
        // Round 11: window_a / window_b > 0: the walls have a picture window there (that half width along the wall, glass y0 to
        // y1) and only its glass and frame break; front_u: the picture window's range along the front (house frame u)
        [Serializable] class ScOpenHouse { public int building; public float[] a, b, wall_a, wall_b, front_u; public float half_width, floor_y, y0_a, y1_a, y0_b, y1_b, window_a, window_b; public bool no_entry_gable, sliding_door_a; }
        [Serializable] class ScJumpHouse { public int building; }
        [Serializable] class ScZone { public float[] poly; }   // Round 11: a landing zone on the ground (RampHold keeps the car's speed there)
        [Serializable] class ScShortcut { public string id, name; public int[] forgives; public float[] path, trail; public float half_width, clear_half_width, entry_s; public ScPrism[] prisms; public ScOpenHouse[] open_houses; public ScJumpHouse[] jump_houses; public ScZone[] landing_zones; }
        [Serializable] class ScFile { public ScShortcut[] shortcuts; }

        const string ShortcutFile = "Data/shortcuts/chastain_glen_loop.json";
        static readonly Color PassageWall = new Color(0.86f, 0.82f, 0.72f), PassageCeiling = new Color(0.93f, 0.92f, 0.89f), DirtTrail = new Color(0.38f, 0.255f, 0.135f);

        sealed partial class Neighborhood
        {
            public ScFile ShortcutData;
            public readonly Dictionary<int, ScOpenHouse> OpenHouses = new Dictionary<int, ScOpenHouse>();
            public readonly HashSet<int> JumpHouses = new HashSet<int>();
            public int TreesCleared;
            readonly List<(Vector2 a, Vector2 b, float half)> corridor = new List<(Vector2, Vector2, float)>();

            void LoadShortcuts()
            {
                string path = Path.Combine(ProjectRoot, ShortcutFile);
                if (!File.Exists(path)) { Debug.Log("[Shortcuts] no " + ShortcutFile + " (run Tools/mapgen/make_shortcuts.py): no shortcut structures"); return; }
                ShortcutData = JsonUtility.FromJson<ScFile>(File.ReadAllText(path));
                foreach (var sc in ShortcutData.shortcuts)
                {
                    var p = ToPoints(sc.path);
                    for (int i = 0; i + 1 < p.Count; i++)
                        if (RoadClearance((p[i] + p[i + 1]) * 0.5f) > 0.5f) corridor.Add((p[i], p[i + 1], sc.clear_half_width));   // off the street only
                    if (sc.open_houses != null) foreach (var oh in sc.open_houses) OpenHouses[oh.building] = oh;
                    if (sc.jump_houses != null) foreach (var jh in sc.jump_houses) JumpHouses.Add(jh.building);
                }
            }

            // Round 11: p is under one of the shortcuts' ramps, decks, floors or stairs (a drive is not drawn there)
            public bool UnderShortcutPrism(Vector2 p)
            {
                if (ShortcutData == null) return false;
                foreach (var sc in ShortcutData.shortcuts) foreach (var pr in sc.prisms) if (InPolygon(p, ToPoints(pr.poly))) return true;
                return false;
            }

            public bool InCorridor(Vector2 p, float extra = 0f)
            {
                foreach (var s in corridor)
                {
                    float r = s.half + extra;
                    if (p.x < Mathf.Min(s.a.x, s.b.x) - r || p.x > Mathf.Max(s.a.x, s.b.x) + r || p.y < Mathf.Min(s.a.y, s.b.y) - r || p.y > Mathf.Max(s.a.y, s.b.y) + r) continue;
                    var ab = s.b - s.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                    if ((s.a + ab * t - p).sqrMagnitude < r * r) return true;
                }
                return false;
            }
        }

        // ------------------------------------------------------------ houses on a shortcut

        static void BuildHouseWithShortcuts(HoodCtx c, MeshBuilder mb, MeshBuilder db, HouseSpec h)
        {
            if (c.hood.OpenHouses.TryGetValue(h.index, out var oh))
            {
                if (oh.no_entry_gable) h.entryGable = false;   // a flat upstairs front where the line goes in (one house's small porch gable)
                var tm = new MeshBuilder(); var td = new MeshBuilder();
                h.detail = td;
                if (oh.sliding_door_a) h.backReserve = BackRange(h, oh, 1.6f);   // no back window where the sliding door goes
                bool picture = oh.window_a > 0f;   // Round 11 (BUG-013): picture windows; the door moves out of the way
                if (picture) { h.frontReserve = new Vector2(oh.front_u[0], oh.front_u[1]); h.sideLowFree = true; }
                BuildHouse(tm, h);
                if (oh.sliding_door_a) SlidingDoor(td, h, oh);
                if (picture) { PictureWindow(td, h, oh.wall_a, oh.window_a, oh.y0_a, oh.y1_a); PictureWindow(td, h, oh.wall_b, oh.window_b, oh.y0_b, oh.y1_b); }
                h.detail = db; h.backReserve = null; h.frontReserve = null; h.sideLowFree = false;
                Passage(oh, out var A, out var u, out var n);
                var inA = new List<List<(Vector3 p, Color c)>>(); var inB = new List<List<(Vector3 p, Color c)>>();
                var volA = picture ? WindowVolume(oh.wall_a, oh.window_a, oh.y0_a, oh.y1_a) : WallVolume(oh.wall_a, A, n, oh.half_width, oh.y0_a, oh.y1_a);
                var volB = picture ? WindowVolume(oh.wall_b, oh.window_b, oh.y0_b, oh.y1_b) : WallVolume(oh.wall_b, A, n, oh.half_width, oh.y0_b, oh.y1_b);
                // a picture window: the wall behind it is left out for good (the frame covers the hole) and only the glass and
                // frame break; otherwise the wall itself breaks
                var hole = new List<List<(Vector3 p, Color c)>>();
                ClipSplit(tm, mb, volA, volB, picture ? hole : inA, picture ? hole : inB);
                ClipSplit(td, db, volA, volB, inA, inB);
                WallPanel(c, h, oh, oh.wall_a, oh.y0_a, oh.y1_a, A, n, inA, "in", picture ? oh.window_a : 0f);
                WallPanel(c, h, oh, oh.wall_b, oh.y0_b, oh.y1_b, A, n, inB, "out", picture ? oh.window_b : 0f);
                PassageRoom(mb, db, h, oh, A, u, n);
                c.Log.Append(picture
                    ? $" house {h.index} (style {h.style}, floor {h.floorY:0.00}): whole, with a picture window in ({inA.Count} pieces of glass and frame) and a window out ({inB.Count}), door moved beside the garage;"
                    : $" house {h.index} (style {h.style}, floor {h.floorY:0.00}): whole, with breakable wall panels where the shortcut goes in ({inA.Count} pieces of wall) and out ({inB.Count});");
                return;
            }
            if (c.hood.JumpHouses.Contains(h.index))
            {
                var tm = new MeshBuilder();
                BuildHouse(tm, h);
                Append(tm, mb);
                var go = tm.ToObject("Jumped house " + h.index + " roof collider", c.mats.House, c.root, true);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshRenderer>());   // collider only: the visible house is in the chunk mesh
                c.Log.Append($" jumped house {h.index} (style {h.style}): roof collider;");
                return;
            }
            BuildHouse(mb, h);
        }

        static void Passage(ScOpenHouse oh, out Vector2 A, out Vector2 u, out Vector2 n)
        {
            A = new Vector2(oh.a[0], oh.a[1]); var B = new Vector2(oh.b[0], oh.b[1]);
            u = (B - A).normalized; n = new Vector2(-u.y, u.x);
        }

        static void Append(MeshBuilder src, MeshBuilder dst)
        {
            int off = dst.Count;
            dst.Verts.AddRange(src.Verts); dst.Colours.AddRange(src.Colours);
            foreach (int t in src.Tris) dst.Tris.Add(t + off);
        }

        // The volume of one breakable wall panel: within 0.35 m of the wall's plane (the wall, its windows, sills and trim),
        // within the passage's half width of the line, between y0 and y1. As planes that are >= 0 outside it.
        static Func<Vector3, float>[] WallVolume(float[] wall, Vector2 A, Vector2 n, float half, float y0, float y1)
        {
            var P = new Vector2(wall[0], wall[1]); var N = new Vector2(wall[2], wall[3]);
            return new Func<Vector3, float>[]
            {
                p => Vector2.Dot(new Vector2(p.x, p.z) - A, n) - half,
                p => -half - Vector2.Dot(new Vector2(p.x, p.z) - A, n),
                p => Vector2.Dot(new Vector2(p.x, p.z) - P, N) - 0.35f,
                p => -0.35f - Vector2.Dot(new Vector2(p.x, p.z) - P, N),
                p => y0 - p.y,
                p => p.y - y1,
            };
        }

        // Round 11: the volume of a picture window and its frame and sill (PictureWindow): within 0.35 m of the wall's plane,
        // 'half' + the frame along the wall from the point on it, from under the sill to the top of the frame
        static Func<Vector3, float>[] WindowVolume(float[] wall, float half, float y0, float y1)
        {
            var P = new Vector2(wall[0], wall[1]); var N = new Vector2(wall[2], wall[3]); var along = new Vector2(-N.y, N.x);
            float hw = half + WinFrame;
            return new Func<Vector3, float>[]
            {
                p => Vector2.Dot(new Vector2(p.x, p.z) - P, along) - hw,
                p => -hw - Vector2.Dot(new Vector2(p.x, p.z) - P, along),
                p => Vector2.Dot(new Vector2(p.x, p.z) - P, N) - 0.35f,
                p => -0.35f - Vector2.Dot(new Vector2(p.x, p.z) - P, N),
                p => (y0 - WinFrame - WinSill) - p.y,
                p => p.y - (y1 + WinFrame),
            };
        }

        const float WinFrame = 0.12f, WinSill = 0.1f;

        // Round 11 (BUG-013, Dan: "a jump through a large window"): a picture window on the wall through wall[] (a point on it
        // and its outward normal), centred on that point: a white frame, the glass in three panes (the middle one widest), a
        // sill under it. Everything inside WindowVolume.
        static void PictureWindow(MeshBuilder d, HouseSpec h, float[] wall, float half, float y0, float y1)
        {
            var P = new Vector3(wall[0], h.floorY, wall[1]);
            var wf = MakeFrame(P, new Vector2(wall[2], wall[3]), 1f);
            float a = y0 - h.floorY, b = y1 - h.floorY, m = half * 0.32f;
            FrontQuad(d, wf, -half - WinFrame, half + WinFrame, a - WinFrame, b + WinFrame, 0.02f, h.trimC);
            FrontQuad(d, wf, -half, half, a, b, 0.04f, h.glassC);
            FrontQuad(d, wf, -m, m, a + 0.05f, b - 0.05f, 0.045f, Shade(h.glassC, 1.18f));   // the middle pane catches the light a little differently
            foreach (float x in new[] { -m, m }) FrontQuad(d, wf, x - 0.05f, x + 0.05f, a, b, 0.06f, h.trimC);
            FrontQuad(d, wf, -half - WinFrame, half + WinFrame, a - WinFrame - WinSill, a - WinFrame, 0.07f, h.trimC);
        }

        // Copies src into dst without what lies inside volume a or b; those parts go to inA / inB instead (Sutherland-Hodgman,
        // each triangle cut by the planes of a, then what is left of it by the planes of b).
        static void ClipSplit(MeshBuilder src, MeshBuilder dst, Func<Vector3, float>[] a, Func<Vector3, float>[] b, List<List<(Vector3 p, Color c)>> inA, List<List<(Vector3 p, Color c)>> inB)
        {
            var V = src.Verts; var C = src.Colours; var T = src.Tris;
            for (int i = 0; i + 2 < T.Count; i += 3)
            {
                var tri = new List<(Vector3 p, Color c)> { (V[T[i]], C[T[i]]), (V[T[i + 1]], C[T[i + 1]]), (V[T[i + 2]], C[T[i + 2]]) };
                foreach (var outsideA in Split(tri, a, inA))
                    foreach (var outsideBoth in Split(outsideA, b, inB)) Emit(dst, outsideBoth);
            }
        }

        // the parts of poly outside the volume (returned) and the part inside it (added to inside)
        static List<List<(Vector3 p, Color c)>> Split(List<(Vector3 p, Color c)> poly, Func<Vector3, float>[] planes, List<List<(Vector3 p, Color c)>> inside)
        {
            var outs = new List<List<(Vector3 p, Color c)>>();
            foreach (var f in planes) if (poly.All(q => f(q.p) >= 0f)) { outs.Add(poly); return outs; }
            foreach (var f in planes)
            {
                var o = Cut(poly, f, true); var i = Cut(poly, f, false);
                if (o.Count >= 3) outs.Add(o);
                poly = i;
                if (poly.Count < 3) return outs;
            }
            inside.Add(poly);
            return outs;
        }

        static List<(Vector3 p, Color c)> Cut(List<(Vector3 p, Color c)> poly, Func<Vector3, float> f, bool keepOutside)
        {
            var o = new List<(Vector3, Color)>();
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % poly.Count];
                float fa = f(a.p), fb = f(b.p);
                bool ina = keepOutside ? fa >= 0f : fa < 0f, inb = keepOutside ? fb >= 0f : fb < 0f;
                if (ina) o.Add(a);
                if (ina != inb)
                {
                    float t = fa / (fa - fb);
                    o.Add((Vector3.Lerp(a.p, b.p, t), Color.Lerp(a.c, b.c, t)));
                }
            }
            return o;
        }

        static void Emit(MeshBuilder dst, List<(Vector3 p, Color c)> poly)
        {
            int start = dst.Count;
            foreach (var q in poly) { dst.Tint = q.c; dst.Vertex(q.p); }
            for (int k = 1; k + 1 < poly.Count; k++) dst.Tri(start, start + k, start + k + 1);
        }

        // One breakable panel: the clipped-out wall, in pieces of about 0.9 x 0.8 m (along the wall, up) that fly separately.
        // Its trigger box straddles the wall across the passage; the debris comes to rest on the ground below the wall.
        static void WallPanel(HoodCtx c, HouseSpec h, ScOpenHouse oh, float[] wall, float y0, float y1, Vector2 A, Vector2 n, List<List<(Vector3 p, Color c)>> polys, string tag, float windowHalf = 0f)
        {
            if (polys.Count == 0) return;
            var P = new Vector2(wall[0], wall[1]); var N = new Vector2(wall[2], wall[3]); var along = new Vector2(-N.y, N.x);
            float gy = c.Y(P.x + N.x * 1.5f, P.y + N.y * 1.5f);
            var br = new Breakables();
            var mbp = br.Begin(P);
            var cells = new SortedDictionary<(int, int), List<List<(Vector3 p, Color c)>>>();
            foreach (var poly in polys)
            {
                Vector3 m = Vector3.zero; foreach (var q in poly) m += q.p; m /= poly.Count;
                var key = (Mathf.FloorToInt(Vector2.Dot(new Vector2(m.x, m.z) - P, along) / 0.9f), Mathf.FloorToInt((m.y - y0) / 0.8f));
                if (!cells.TryGetValue(key, out var list)) cells[key] = list = new List<List<(Vector3 p, Color c)>>();
                list.Add(poly);
            }
            foreach (var kv in cells) br.Piece(() => { foreach (var poly in kv.Value) Emit(mbp, poly); });
            float ym = (y0 + y1) / 2f;
            br.End(new Vector3(P.x, ym, P.y), new Vector3(-N.x, 0f, -N.y), windowHalf > 0f ? (windowHalf + WinFrame) * 2f + 0.4f : oh.half_width * 2f + 1.2f, y1 - y0 + 0.4f, 1.6f);
            br.ToObjects($"House {h.index} shortcut wall ({tag})", c.mats.House, c.root, Mathf.Max(0.3f, ym - gy - 0.2f));
        }

        // the house's back wall range (in the turned-round back frame, u to the right as seen from behind) around where the
        // line crosses it, so the generic back windows leave room for the sliding door
        static Vector2 BackRange(HouseSpec h, ScOpenHouse oh, float half)
        {
            var b = new Frame { O = h.frame.O, R = -h.frame.R, F = -h.frame.F, M = h.frame.M };
            var A3 = new Vector3(oh.a[0], h.frame.O.y, oh.a[1]);
            float ub = Vector3.Dot(A3 - b.O, b.R) * b.M;
            return new Vector2(ub - half, ub + half);
        }

        // Round 10 (Dan: "drive through a sliding glass door"): a two-panel sliding glass door in the back wall where the line
        // goes in, drawn with the house so it becomes part of the breakable panel
        static void SlidingDoor(MeshBuilder d, HouseSpec h, ScOpenHouse oh)
        {
            var b = new Frame { O = h.frame.O, R = -h.frame.R, F = -h.frame.F, M = h.frame.M };
            var r = BackRange(h, oh, 0f); float uc = r.x, back = h.D / 2;
            FrontQuad(d, b, uc - 1.25f, uc + 1.25f, 0f, 2.3f, back + 0.02f, h.trimC);
            FrontQuad(d, b, uc - 1.15f, uc - 0.04f, 0.05f, 2.2f, back + 0.04f, h.glassC);
            FrontQuad(d, b, uc + 0.04f, uc + 1.15f, 0.05f, 2.2f, back + 0.04f, Shade(h.glassC, 1.15f));
            FrontQuad(d, b, uc - 0.04f, uc + 0.04f, 0.05f, 2.2f, back + 0.06f, h.trimC);
            FrontQuad(d, b, uc - 0.12f, uc - 0.07f, 0.95f, 1.15f, back + 0.06f, new Color(0.75f, 0.75f, 0.72f));   // handle
            BoxF(d, b, uc - 1.4f, uc + 1.4f, -0.14f, 0f, back, back + 0.6f, new Color(0.62f, 0.60f, 0.57f));        // step
        }

        // The plain room along the line: walls either side of the passage inside the house, a ceiling, a light. Only seen
        // once a panel is broken. (The floors and the stairs are prisms.)
        static void PassageRoom(MeshBuilder mb, MeshBuilder db, HouseSpec h, ScOpenHouse oh, Vector2 A, Vector2 u, Vector2 n)
        {
            // the house's rectangle, but only behind both walls the line crosses (the rectangle also takes in the garage's
            // projection in front of the entry wall, where a room would stick out under the eave)
            var Pa = new Vector2(oh.wall_a[0], oh.wall_a[1]); var Na = new Vector2(oh.wall_a[2], oh.wall_a[3]);
            var Pb = new Vector2(oh.wall_b[0], oh.wall_b[1]); var Nb = new Vector2(oh.wall_b[2], oh.wall_b[3]);
            var rect = ClipPoly2(ClipPoly2(HouseRect(h), p => -Vector2.Dot(p - Pa, Na)), p => -Vector2.Dot(p - Pb, Nb));
            float y0 = Mathf.Min(oh.y0_a, oh.y0_b) - 0.3f, y1 = oh.window_a > 0f ? oh.floor_y + 2.8f : Mathf.Max(oh.y1_a, oh.y1_b), hw = oh.half_width;   // Round 11: a picture window's room has a room's ceiling
            Vector3 W(Vector2 p, float y) => new Vector3(p.x, y, p.y);
            foreach (float side in new[] { -1f, 1f })
            {
                var piece = ClipPoly2(ClipPoly2(rect, p => Vector2.Dot(p - A, n) * side - hw + 0.02f), p => hw + 0.02f - Vector2.Dot(p - A, n) * side);
                if (piece.Count < 2) continue;
                float t0 = piece.Min(p => Vector2.Dot(p - A, u)) + 0.1f, t1 = piece.Max(p => Vector2.Dot(p - A, u)) - 0.1f;
                if (t1 <= t0) continue;
                Vector2 a = A + n * side * hw + u * t0, b = A + n * side * hw + u * t1;
                var inward = new Vector3(-n.x * side, 0f, -n.y * side);
                mb.Tint = PassageWall;
                Face(mb, W(a, y0), W(b, y0), W(b, y1), W(a, y1), inward);
            }
            var ceil = ClipPoly2(ClipPoly2(rect, p => hw - Vector2.Dot(p - A, n)), p => hw + Vector2.Dot(p - A, n));
            if (ceil.Count >= 3)
            {
                mb.Tint = PassageCeiling;
                for (int k = 1; k + 1 < ceil.Count; k++) TriFace(mb, W(ceil[0], y1), W(ceil[k], y1), W(ceil[k + 1], y1), Vector3.down);
                Vector2 cc = Vector2.zero; foreach (var p in ceil) cc += p; cc /= ceil.Count;
                db.Tint = new Color(1f, 0.97f, 0.85f);
                Face(db, W(cc + u * 0.4f + n * 0.4f, y1 - 0.02f), W(cc - u * 0.4f + n * 0.4f, y1 - 0.02f), W(cc - u * 0.4f - n * 0.4f, y1 - 0.02f), W(cc + u * 0.4f - n * 0.4f, y1 - 0.02f), Vector3.down);
            }
        }

        // the house's footprint rectangle (as the styled house is built: W along R, D along F) as a polygon
        static List<Vector2> HouseRect(HouseSpec h)
        {
            var r = new Vector2(h.frame.R.x, h.frame.R.z); var f = new Vector2(h.frame.F.x, h.frame.F.z); var c = h.footprintCentre;
            return new List<Vector2> { c + r * h.W / 2 + f * h.D / 2, c - r * h.W / 2 + f * h.D / 2, c - r * h.W / 2 - f * h.D / 2, c + r * h.W / 2 - f * h.D / 2 };
        }

        static List<Vector2> ClipPoly2(List<Vector2> poly, Func<Vector2, float> f)   // keeps f >= 0
        {
            var o = new List<Vector2>();
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % poly.Count]; float fa = f(a), fb = f(b);
                if (fa >= 0f) o.Add(a);
                if ((fa >= 0f) != (fb >= 0f)) o.Add(Vector2.Lerp(a, b, fa / (fa - fb)));
            }
            return o;
        }

        // The house's collider without the passage: boxes in the passage's frame, 0.5 m slices of the rectangle on either side
        // of the passage (full height), and over the passage a block from the top of the panels to the top of the old box.
        // Below the panels the passage is left open: the ramp, the floors and the stairs are its floor.
        static void OpenHouseColliders(HoodCtx c, HouseSpec h, Transform parent)
        {
            var oh = c.hood.OpenHouses[h.index];
            Passage(oh, out var A, out var u, out var n);
            var rect = HouseRect(h);
            float hh = h.style == HouseStyle.C ? 3.0f : 5.9f;
            float yb = h.baseY, yt = h.floorY + hh;
            var root = new GameObject("House " + h.index + " (open: shortcut passage)").transform; root.SetParent(parent, false);
            var rot = Quaternion.LookRotation(new Vector3(u.x, 0f, u.y), Vector3.up);
            void Box(float l0, float l1, float y0, float y1)
            {
                var piece = ClipPoly2(ClipPoly2(rect, p => Vector2.Dot(p - A, n) - l0), p => l1 - Vector2.Dot(p - A, n));
                if (piece.Count < 3 || y1 - y0 < 0.05f) return;
                float t0 = piece.Min(p => Vector2.Dot(p - A, u)), t1 = piece.Max(p => Vector2.Dot(p - A, u));
                if (t1 - t0 < 0.05f) return;
                var cxz = A + n * ((l0 + l1) / 2f) + u * ((t0 + t1) / 2f);
                var go = new GameObject("box"); go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(new Vector3(cxz.x, (y0 + y1) / 2f, cxz.y), rot);
                go.AddComponent<BoxCollider>().size = new Vector3(l1 - l0, y1 - y0, t1 - t0);
            }
            float lmin = rect.Min(p => Vector2.Dot(p - A, n)), lmax = rect.Max(p => Vector2.Dot(p - A, n));
            for (float l = oh.half_width; l < lmax; l += 0.5f) Box(l, Mathf.Min(l + 0.5f, lmax), yb, yt);
            for (float l = -oh.half_width; l > lmin; l -= 0.5f) Box(Mathf.Max(l - 0.5f, lmin), l, yb, yt);
            float top = Mathf.Max(oh.y1_a, oh.y1_b);
            Box(-oh.half_width, oh.half_width, top, Mathf.Max(yt, top + 1.5f));
        }

        // ------------------------------------------------------------ ramps and decks

        static void BuildShortcuts(Neighborhood hood, TerrainGrid terrain, Materials mats, Transform world)
        {
            if (hood == null || hood.ShortcutData == null) return;
            var root = new GameObject("Shortcuts").transform; root.SetParent(world, false);
            rampMaterial = null;
            int prisms = 0;
            foreach (var sc in hood.ShortcutData.shortcuts)
            {
                var sroot = new GameObject("Shortcut " + sc.id + " (" + sc.name + ")").transform; sroot.SetParent(root, false);
                foreach (var pr in sc.prisms) { BuildPrism(pr, terrain, mats, sroot); prisms++; }
                if (sc.landing_zones != null)
                    foreach (var z in sc.landing_zones)   // Round 11: ground the car lands on, with the landing carry (LandingZone, RampHold)
                    {
                        var go = new GameObject("Landing zone"); go.transform.SetParent(sroot, false);
                        go.AddComponent<LandingZone>().outline = ToPoints(z.poly).ToArray();
                    }
            }
            Debug.Log($"[Shortcuts] {hood.ShortcutData.shortcuts.Length} shortcuts, {prisms} ramps / decks / floors, {hood.OpenHouses.Count} houses opened, {hood.JumpHouses.Count} jumped; {hood.TreesCleared} trees kept off the lines");
        }

        // Ramps and decks are frictionless to the car's BODY. The car's grip comes from its own suspension rays
        // (ArcadeVehicle), not from collider friction, so this changes nothing while it drives; but on a kicker steep enough
        // to clear a house, the soft suspension lets the body touch the ramp, and with friction that contact stopped the car.
        static PhysicsMaterial rampMaterial;
        static PhysicsMaterial RampMaterial()
        {
            if (rampMaterial) return rampMaterial;
            rampMaterial = new PhysicsMaterial("ShortcutRamp") { dynamicFriction = 0f, staticFriction = 0f, bounciness = 0f, frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum };
            AssetDatabase.CreateAsset(rampMaterial, GeneratedDir + "/ShortcutRamp.physicMaterial");
            return rampMaterial;
        }

        static float Profile(float[] pr, float s)
        {
            int n = pr.Length / 2;
            if (s <= pr[0]) return pr[1];
            for (int i = 0; i + 1 < n; i++)
                if (s <= pr[2 * i + 2]) return Mathf.Lerp(pr[2 * i + 1], pr[2 * i + 3], Mathf.InverseLerp(pr[2 * i], pr[2 * i + 2], s));
            return pr[2 * n - 1];
        }

        // A solid: the convex outline in plan, its top at Profile(station along the axis), its sides down into the ground.
        static void BuildPrism(ScPrism pr, TerrainGrid terrain, Materials mats, Transform parent)
        {
            var poly = ToPoints(pr.poly);
            var O = new Vector2(pr.origin[0], pr.origin[1]); var U = new Vector2(pr.axis[0], pr.axis[1]).normalized;
            float S(Vector2 p) => Vector2.Dot(p - O, U);
            float s0 = poly.Min(S), s1 = poly.Max(S);
            var cuts = new List<float>();
            bool stairs = pr.kind == "stairs";   // Round 10: the stairs in house 1627, a smooth ramp drawn as treads and risers
            float step = stairs ? 0.28f : 0.5f;
            for (float s = s0; s < s1; s += step) cuts.Add(s);
            for (int i = 0; i < pr.profile.Length; i += 2) if (pr.profile[i] > s0 && pr.profile[i] < s1) cuts.Add(pr.profile[i]);
            cuts.Add(s1); cuts.Sort();
            var col = pr.colour != null && pr.colour.Length >= 3 ? new Color(pr.colour[0], pr.colour[1], pr.colour[2]) : new Color(0.5f, 0.36f, 0.22f);
            var mb = new MeshBuilder();
            var Lv = new Vector2(-U.y, U.x);
            bool banked = pr.bank != null && pr.bank.Length >= 2;
            Vector3 Top(Vector2 p) => new Vector3(p.x, Profile(pr.profile, S(p)) + (banked ? Profile(pr.bank, S(p)) * Vector2.Dot(p - O, Lv) : 0f), p.y);
            // top, strip by strip so the profile's bends are kept; boards: every other strip a shade darker
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                if (cuts[i + 1] - cuts[i] < 1e-3f) continue;
                float a = cuts[i], b = cuts[i + 1];
                var piece = ClipPoly2(ClipPoly2(poly, p => S(p) - a), p => b - S(p));
                if (piece.Count < 3) continue;
                mb.Tint = stairs ? Shade(col, (Mathf.FloorToInt((a - s0) / step + 0.01f) & 1) == 0 ? 1.08f : 0.62f) : Shade(col, (Mathf.FloorToInt(a / 0.5f) & 1) == 0 ? 1f : 0.93f);
                int st = mb.Count;
                foreach (var p in piece) mb.Vertex(Top(p));
                for (int k = 1; k + 1 < piece.Count; k++)
                {
                    // wind so the face points up
                    Vector3 p0 = mb.Verts[st], p1 = mb.Verts[st + k], p2 = mb.Verts[st + k + 1];
                    if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), Vector3.up) >= 0f) mb.Tri(st, st + k, st + k + 1); else mb.Tri(st, st + k + 1, st + k);
                }
            }
            // sides: each outline edge in 0.5 m pieces, from the top down to 0.5 m below the ground
            Vector2 centre = Vector2.zero; foreach (var p in poly) centre += p; centre /= poly.Count;
            var skirt = new HashSet<int>(pr.skirt_edges ?? new int[0]);
            mb.Tint = Shade(col, 0.72f);
            for (int e = 0; e < poly.Count; e++)
            {
                var a = poly[e]; var b = poly[(e + 1) % poly.Count];
                var mid = (a + b) * 0.5f; var dir = (b - a).normalized; var outN = new Vector2(dir.y, -dir.x);
                if (Vector2.Dot(outN, mid - centre) < 0f) outN = -outN;
                int k = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.5f));
                for (int i = 0; i < k; i++)
                {
                    var p = Vector2.Lerp(a, b, i / (float)k); var q = Vector2.Lerp(a, b, (i + 1) / (float)k);
                    float gp = Ground(terrain, p.x, p.y) - 0.5f, gq = Ground(terrain, q.x, q.y) - 0.5f;
                    var tp = Top(p); var tq = Top(q);
                    if (tp.y <= gp + 0.5f && tq.y <= gq + 0.5f) continue;   // buried
                    if (skirt.Contains(e))
                    {
                        // a sloped skirt (about 40 degrees) from the top edge out and down to the ground, instead of a wall
                        float hp = Mathf.Max(0f, tp.y - gp - 0.5f) * 1.2f, hq = Mathf.Max(0f, tq.y - gq - 0.5f) * 1.2f;
                        var po = p + outN * hp; var qo = q + outN * hq;
                        Face(mb, new Vector3(po.x, Ground(terrain, po.x, po.y) - 0.05f, po.y), new Vector3(qo.x, Ground(terrain, qo.x, qo.y) - 0.05f, qo.y), tq, tp, new Vector3(outN.x, 1f, outN.y));
                        continue;
                    }
                    Face(mb, new Vector3(p.x, gp, p.y), new Vector3(q.x, gq, q.y), tq, tp, new Vector3(outN.x, 0f, outN.y));
                }
            }
            var go = mb.ToObject(pr.name, mats.House, parent, false);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            var surf = go.AddComponent<ShortcutSurface>(); surf.held = pr.held; surf.landing = pr.kind == "landing";   // Round 10: RampHold
            // the collider: the same surfaces with the vertices along the strip seams welded (1 mm), so the car's body does
            // not catch on seams between the strips ("internal edges")
            var weld = new Dictionary<Vector3Int, int>(); var wv = new List<Vector3>(); var wt = new List<int>();
            foreach (int t in mb.Tris)
            {
                var v = mb.Verts[t]; var key = new Vector3Int(Mathf.RoundToInt(v.x * 1000f), Mathf.RoundToInt(v.y * 1000f), Mathf.RoundToInt(v.z * 1000f));
                if (!weld.TryGetValue(key, out int id)) { id = wv.Count; wv.Add(v); weld[key] = id; }
                wt.Add(id);
            }
            var cm = new Mesh { name = pr.name + " collider", indexFormat = IndexFormat.UInt32 };
            cm.SetVertices(wv); cm.SetTriangles(wt, 0); cm.RecalculateNormals(); cm.RecalculateBounds();
            AssetDatabase.CreateAsset(cm, $"{GeneratedDir}/Meshes/{pr.name} collider.asset");
            var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = cm; mc.sharedMaterial = RampMaterial();
        }
    }
}
