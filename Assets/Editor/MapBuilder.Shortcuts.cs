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
        [Serializable] class ScPrism { public string name, kind; public float[] poly, origin, axis, profile, colour, bank; public int[] skirt_edges, no_face; public bool held, cut, landing; }   // Round 21: no_face, outline edges with no side face (a back against its take-off)   // Round 21: landing (RampHold's carry) on any kind   // bank: (station, rise per metre to the left) pairs
        // Round 10: a and b are where the line crosses the house's walls (in, out); wall_a / wall_b: a point on that wall and its
        // outward normal (x, z, nx, nz); y0 / y1: the height range of the panel that breaks in each wall
        // Round 11: window_a / window_b > 0: the walls have a picture window there (that half width along the wall, glass y0 to
        // y1) and only its glass and frame break; front_u: the picture window's range along the front (house frame u)
        // Round 12: door_half > 0: the sliding door in wall a is that half width of glass along the wall and only its glass and
        // frame break (as a window); upper_u: the upstairs front (house frame u) kept free of the house's own windows
        [Serializable] class ScOpenHouse { public int building; public float[] a, b, wall_a, wall_b, front_u, upper_u; public float half_width, floor_y, y0_a, y1_a, y0_b, y1_b, window_a, window_b, door_half; public bool no_entry_gable, sliding_door_a; }
        [Serializable] class ScJumpHouse { public int building; }
        [Serializable] class ScZone { public float[] poly; public bool hold; }   // Round 11: a landing zone on the ground (RampHold keeps the car's speed there); Round 12: hold, the car is kept on the ground there
        // Round 12: ground along a line set to a profile (station along the line, height), cut and fill, half_width either side
        // of the line, banks outside: the shortcut's surface is the grass itself
        [Serializable] class ScGrade { public string label; public float[] path, profile; public float half_width; }
        [Serializable] class ScSign { public float x, z; }
        [Serializable] class ScShortcut { public string id, name, direction; public int[] forgives; public float[] path, trail; public float half_width, clear_half_width, entry_s; public ScPrism[] prisms; public ScOpenHouse[] open_houses; public ScJumpHouse[] jump_houses, roof_houses; public ScZone[] landing_zones; public ScGrade[] grades; public ScSign sign; public string sign_face, look; public ScBigJump big_jump; public ScPlayground playground;
                                        public ScPool pool; public ScFence[] fences; public ScProp[] props; public float[] tracks; }   // Round 19: sign_face "party" (R3) / "jump" (R1); look "kids_ramp"; big_jump (R1's lip speed cap). Round 20 follow-up: look "pool_jump" (R1 on Dan's line: MapBuilder.R1Pool.cs)
        [Serializable] class ScBigJump { public float[] lip, dir; public float vmax, half, angle; }
        // Round 18 (R3): the play set and trampoline (MapBuilder.Playground.cs)
        [Serializable] class ScTower { public float[] o, u; public float len, half, deck_y, roof_y; }
        [Serializable] class ScSlide { public float[] o; public float len, half, foot_half, flare; }   // Round 18: the foot flares from foot_half to half over 'flare' m
        [Serializable] class ScTrampoline { public float x, z, r, mat_y, flight_s, land_yaw; public float[] target; }
        [Serializable] class ScPlayground { public ScTower tower; public ScSlide slide; public ScTrampoline trampoline; }
        [Serializable] class ScFile { public ScShortcut[] shortcuts; }

        const string ShortcutFile = "Data/shortcuts/chastain_glen_loop.json";
        static readonly Color PassageWall = new Color(0.86f, 0.82f, 0.72f), PassageCeiling = new Color(0.93f, 0.92f, 0.89f), DirtTrail = new Color(0.38f, 0.255f, 0.135f);

        sealed partial class Neighborhood
        {
            public ScFile ShortcutData;
            public readonly Dictionary<int, ScOpenHouse> OpenHouses = new Dictionary<int, ScOpenHouse>();
            public readonly HashSet<int> JumpHouses = new HashSet<int>();
            public readonly HashSet<int> RoofHouses = new HashSet<int>();   // Round 12: houses a car may land on (a convex roof collider)
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
                        if (RoadClearance((p[i] + p[i + 1]) * 0.5f) > 0.5f)   // off the street only
                        {
                            corridor.Add((p[i], p[i + 1], sc.clear_half_width));
                            treeBand.Add((p[i], p[i + 1], Mathf.Max(sc.clear_half_width, TreeClearHalf)));
                        }
                    if (sc.open_houses != null) foreach (var oh in sc.open_houses) OpenHouses[oh.building] = oh;
                    if (sc.jump_houses != null) foreach (var jh in sc.jump_houses) JumpHouses.Add(jh.building);
                    if (sc.roof_houses != null) foreach (var rh in sc.roof_houses) RoofHouses.Add(rh.building);
                    if (sc.sign != null && sc.sign.x != 0f && SignSpot(sc, out var spot, out var facing, out float turn)) { SignSpots[sc.id] = (spot, facing, turn); KeepOut(spot, sc.sign_face == "party" ? 2.2f : 1.5f); }
                }
            }

            // Round 12, item 3 (Dan: "we would need some sort of clue that there is a shortcut that way (a sign if need be)"):
            // the sign at a shortcut's turn-off stands in the grass strip (1.5 m past the road's edge) on the side the line turns
            // to, just past where the line's centre leaves the street (at the corner a driver turns round; failing that, up to
            // 30 m before it), off every drive and 3.5 m or more from the line. It faces the oncoming driver, turned 25 degrees
            // toward the road.
            public readonly Dictionary<string, (Vector2 at, Vector2 facing, float turn)> SignSpots = new Dictionary<string, (Vector2, Vector2, float)>();   // Round 19: turn, +1 the line turns left, -1 right
            bool SignSpot(ScShortcut sc, out Vector2 spot, out Vector2 facing, out float turn)
            {
                spot = facing = default; turn = 0f;
                var p = ToPoints(sc.path);
                int i0 = p.FindIndex(q => RoadClearance(q) > 0f);
                if (i0 < 16) return false;
                var d = (p[i0 - 6] - p[i0 - 16]).normalized; var L = new Vector2(-d.y, d.x);
                float side = Vector2.Dot(p[Mathf.Min(i0 + 8, p.Count - 1)] - p[i0 - 6], L) > 0f ? 1f : -1f; turn = side;
                var tries = new List<float>();
                for (float back = -2f; back >= -16f; back -= 0.5f) tries.Add(back);
                for (float back = 6f; back <= 30f; back += 0.5f) tries.Add(back);
                foreach (float back in tries)
                {
                    Nearest(p[i0] - d * back, out Vector2 q, out float half, out Vector2 tan, out _);
                    var n = new Vector2(-tan.y, tan.x); if (Vector2.Dot(n, L * side) < 0f) n = -n;   // toward the side the line turns to
                    var s = q + n * (half + 1.5f);
                    bool onDrive = false;
                    foreach (var dr in DriveSegs)
                    {
                        var ab = dr.b - dr.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(s - dr.a, ab) / ab.sqrMagnitude);
                        if ((dr.a + ab * t - s).magnitude < dr.half + 1.2f) { onDrive = true; break; }
                    }
                    if (onDrive || InsideHouse(s, 1.0f, null) || p.Any(pp => (pp - s).sqrMagnitude < 3.5f * 3.5f)) continue;
                    spot = s; facing = (-d * Mathf.Cos(25f * Mathf.Deg2Rad) - n * Mathf.Sin(25f * Mathf.Deg2Rad)).normalized;
                    return true;
                }
                Debug.LogWarning($"[Round12] {sc.id}: no room for the sign at its turn-off");
                return false;
            }

            // Round 11: p is under one of the shortcuts' ramps, decks, floors or stairs (a drive is not drawn there)
            public bool UnderShortcutPrism(Vector2 p)
            {
                if (ShortcutData == null) return false;
                foreach (var sc in ShortcutData.shortcuts) foreach (var pr in sc.prisms) if (InPolygon(p, ToPoints(pr.poly))) return true;
                return false;
            }

            // Round 13, item 1 (Dan: "there is a tree in the first shortcut path"): no tree trunk within 6.5 m of a shortcut's
            // line. A trunk in the corridor is still left out (as before); one between the corridor's edge and 6.5 m is moved
            // straight away from the line into its yard, to 7 m off it (or up to 4 m further where that spot is taken)
            public const float TreeClearHalf = 6.5f;
            readonly List<(Vector2 a, Vector2 b, float half)> treeBand = new List<(Vector2, Vector2, float)>();
            public readonly List<(Vector2 from, Vector2 to)> TreesMovedOffLines = new List<(Vector2, Vector2)>();
            public readonly List<Vector2> TreesLeftOnLines = new List<Vector2>();

            public Vector2 MoveTreeOffLine(Vector2 p)
            {
                if (TreeBlocked(p) || !InBand(treeBand, p, 0f, out Vector2 foot)) return p;   // TreeBlocked: left out anyway (in the corridor, a house, ...)
                var away = (p - foot).sqrMagnitude > 1e-6f ? (p - foot).normalized : Vector2.right;
                for (float d = TreeClearHalf + 0.5f; d <= TreeClearHalf + 4.5f; d += 0.5f)
                {
                    var np = foot + away * d;
                    if (!InBand(treeBand, np, 0.3f, out _) && !TreeBlocked(np)) { TreesMovedOffLines.Add((p, np)); return np; }
                }
                TreesLeftOnLines.Add(p);
                return p;
            }

            static bool InBand(List<(Vector2 a, Vector2 b, float half)> band, Vector2 p, float extra, out Vector2 foot)
            {
                foot = p; float best = float.MaxValue; bool inside = false;
                foreach (var s in band)
                {
                    float r = s.half + extra;
                    if (p.x < Mathf.Min(s.a.x, s.b.x) - r || p.x > Mathf.Max(s.a.x, s.b.x) + r || p.y < Mathf.Min(s.a.y, s.b.y) - r || p.y > Mathf.Max(s.a.y, s.b.y) + r) continue;
                    var ab = s.b - s.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                    var c = s.a + ab * t; float d2 = (c - p).sqrMagnitude;
                    if (d2 < r * r) { inside = true; if (d2 < best) { best = d2; foot = c; } }
                }
                return inside;
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
                float doorHalf = oh.door_half > 0f ? oh.door_half : 1.15f;
                if (oh.sliding_door_a) h.backReserve = BackRange(h, oh, doorHalf + 0.45f);   // no back window where the sliding door goes
                bool picture = oh.window_a > 0f;   // Round 11 (BUG-013): picture windows; the door moves out of the way
                if (picture) { h.frontReserve = new Vector2(oh.front_u[0], oh.front_u[1]); h.sideLowFree = true; }
                if (oh.upper_u != null && oh.upper_u.Length == 2) h.upperReserve = new Vector2(oh.upper_u[0], oh.upper_u[1]);   // Round 12
                BuildHouse(tm, h);
                if (oh.sliding_door_a) SlidingDoor(td, h, oh, doorHalf);
                if (picture) PictureWindow(td, h, oh.wall_a, oh.window_a, oh.y0_a, oh.y1_a);
                if (oh.window_b > 0f) PictureWindow(td, h, oh.wall_b, oh.window_b, oh.y0_b, oh.y1_b);
                h.detail = db; h.backReserve = null; h.frontReserve = null; h.sideLowFree = false; h.upperReserve = null;
                Passage(oh, out var A, out var u, out var n);
                var inA = new List<List<(Vector3 p, Color c)>>(); var inB = new List<List<(Vector3 p, Color c)>>();
                // Round 12 (Dan: broken, it shows broken glass with the wall round it intact): a wide sliding door breaks like a
                // window, only its glass and frame
                bool glassA = picture || oh.door_half > 0f, glassB = oh.window_b > 0f;
                float halfA = picture ? oh.window_a : oh.door_half;
                var volA = glassA ? WindowVolume(oh.wall_a, halfA, oh.y0_a, oh.y1_a) : WallVolume(oh.wall_a, A, n, oh.half_width, oh.y0_a, oh.y1_a);
                var volB = glassB ? WindowVolume(oh.wall_b, oh.window_b, oh.y0_b, oh.y1_b) : WallVolume(oh.wall_b, A, n, oh.half_width, oh.y0_b, oh.y1_b);
                // a window: the wall behind it is left out for good (the frame covers the hole) and only the glass and frame
                // break; otherwise the wall itself breaks
                var hole = new List<List<(Vector3 p, Color c)>>();
                ClipSplit(tm, mb, volA, volB, glassA ? hole : inA, glassB ? hole : inB);
                ClipSplit(td, db, volA, volB, inA, inB);
                WallPanel(c, h, oh, oh.wall_a, oh.y0_a, oh.y1_a, A, n, inA, "in", glassA ? halfA : 0f);
                WallPanel(c, h, oh, oh.wall_b, oh.y0_b, oh.y1_b, A, n, inB, "out", glassB ? oh.window_b : 0f);
                PassageRoom(mb, db, h, oh, A, u, n);
                c.Log.Append($" house {h.index} (style {h.style}, floor {h.floorY:0.00}): whole, the shortcut in through {(glassA ? "glass" : "a wall panel")} ({inA.Count} pieces) and out through {(glassB ? "glass" : "a wall panel")} ({inB.Count});");
                return;
            }
            if (c.hood.RoofHouses.Contains(h.index))
            {
                // Round 12 (Dan: "If someone ends up on a roof of a house, it is not a tragedy, they can just drive off of it"): the
                // collider is the convex hull of the house, so a car that comes down on it slides or drives off: no valley between
                // roofs, no pocket behind a chimney, no lip at the eaves (MapBuilder.NeighborhoodBuild gives it no box)
                var tm = new MeshBuilder();
                BuildHouse(tm, h);
                Append(tm, mb);
                var go = tm.ToObject("Roof house " + h.index + " collider", c.mats.House, c.root, false);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
                go.AddComponent<MeshCollider>().convex = true;
                go.GetComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                c.Log.Append($" roof house {h.index} (style {h.style}): convex collider;");
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
            br.ToObjects($"House {h.index} shortcut wall ({tag})", c.mats.House, c.root, Mathf.Max(0.3f, ym - gy - 0.2f), windowHalf > 0f ? BreakSounds.Kind.Glass : BreakSounds.Kind.Wood);   // Round 15: glass for a window or the sliding door
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

        // Round 10 (Dan: "drive through a sliding glass door"): a sliding glass door in the back wall where the line goes in,
        // drawn with the house so it becomes part of the breakable panel. Round 12: 'half' of glass either side (four panels
        // when wide), its frame inside WindowVolume (only glass and frame break); no step (the ground climbs to the sill)
        static void SlidingDoor(MeshBuilder d, HouseSpec h, ScOpenHouse oh, float half)
        {
            var b = new Frame { O = h.frame.O, R = -h.frame.R, F = -h.frame.F, M = h.frame.M };
            var r = BackRange(h, oh, 0f); float uc = r.x, back = h.D / 2;
            float y0 = oh.y0_a - h.floorY, y1 = oh.y1_a - h.floorY;
            FrontQuad(d, b, uc - half - WinFrame, uc + half + WinFrame, y0 - WinFrame, y1 + WinFrame, back + 0.02f, h.trimC);
            int panels = half > 1.6f ? 4 : 2; float pw = 2f * half / panels;
            for (int k = 0; k < panels; k++)
            {
                float a = uc - half + k * pw;
                FrontQuad(d, b, a + 0.04f, a + pw - 0.04f, y0, y1, back + 0.04f, (k & 1) == 0 ? h.glassC : Shade(h.glassC, 1.15f));
                if (k > 0) FrontQuad(d, b, a - 0.04f, a + 0.04f, y0, y1, back + 0.06f, h.trimC);
            }
            foreach (float hu in new[] { uc - 0.12f, uc + 0.07f })
                FrontQuad(d, b, hu, hu + 0.05f, 0.95f, 1.15f, back + 0.06f, new Color(0.75f, 0.75f, 0.72f));   // handles
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

        // Round 13, item 1: every trunk (the forest's and the hand-placed kit trees') within 6.5 m of a shortcut's line where it is
        // off the street, from the turn-off to the rejoin (flight and landing included). Written to Data/screenshots/round13/.
        static void ReportTreesOnLines(Neighborhood hood, TreeBuilder.Result trees)
        {
            var trunks = new List<Vector2>();
            for (int i = 0; i < trees.Field.count; i++) { var t = TreeField.Unpack(trees.Field.data, i); trunks.Add(new Vector2(t.x, t.z)); }
            foreach (var cc in UnityEngine.Object.FindObjectsByType<CapsuleCollider>(FindObjectsInactive.Include))
                if (cc.gameObject.name.StartsWith("Tree ")) trunks.Add(new Vector2(cc.transform.position.x, cc.transform.position.z));
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Round 13 item 1: tree trunks within {Neighborhood.TreeClearHalf} m of a shortcut line (off the street), after the build");
            sb.AppendLine($"moved into their yards: {hood.TreesMovedOffLines.Count}");
            foreach (var (from, to) in hood.TreesMovedOffLines) sb.AppendLine($"  ({from.x:0.0}, {from.y:0.0}) -> ({to.x:0.0}, {to.y:0.0}), {Vector2.Distance(from, to):0.0} m");
            sb.AppendLine($"no room to move (left standing): {hood.TreesLeftOnLines.Count}");
            foreach (var p in hood.TreesLeftOnLines) sb.AppendLine($"  ({p.x:0.0}, {p.y:0.0})");
            foreach (var sc in hood.ShortcutData.shortcuts)
            {
                var pts = ToPoints(sc.path); int n = 0;
                // Round 42 (A): which pieces of the line are off the street, once (asked per trunk and piece it took 16 minutes of the build)
                var off = new bool[Mathf.Max(pts.Count - 1, 0)]; for (int k = 0; k + 1 < pts.Count; k++) off[k] = hood.RoadClearance((pts[k] + pts[k + 1]) * 0.5f) > 0.5f;
                foreach (var q in trunks)
                    for (int k = 0; k + 1 < pts.Count; k++)
                    {
                        if (!off[k]) continue;
                        var ab = pts[k + 1] - pts[k]; float u = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - pts[k], ab) / ab.sqrMagnitude);
                        float d = (pts[k] + ab * u - q).magnitude;
                        if (d < Neighborhood.TreeClearHalf) { n++; sb.AppendLine($"  {sc.id}: trunk at ({q.x:0.0}, {q.y:0.0}) {d:0.00} m from the line"); break; }
                    }
                sb.AppendLine($"{sc.id}: {n} trunks within {Neighborhood.TreeClearHalf} m of the line");
            }
            string dir = Path.Combine(ProjectRoot, "Data/screenshots/round13"); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "trees_on_lines.txt"), sb.ToString());
            Debug.Log("[Round13] " + sb.ToString().Replace("\n", " | "));
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
                foreach (var pr in sc.prisms) { if (pr.kind == "slide") { FitFootToGround(pr, terrain); FitGroundToSlideFoot(pr, terrain, sc.playground?.slide); } BuildPrism(pr, terrain, mats, sroot); prisms++; }
                BuildShortcutSign(hood, terrain, mats, sroot, sc);   // Round 12, item 3
                SlidingDoorTracks(hood, terrain, mats, sroot, sc);   // Round 24 A3, item B2
                if (sc.playground != null && sc.playground.trampoline != null && sc.playground.trampoline.r > 0f) { BuildPlayground(sc, terrain, mats, sroot); if (sc.sign_face == "party") BuildPartyDressing(sc, terrain, mats, sroot); }   // Round 18 (R3); Round 19: the party
                if (sc.look == "kids_ramp" || sc.look == "pool_jump") BuildKidsRamp(sc, terrain, mats, sroot);   // Round 19 (6): R1's homemade ramp (MapBuilder.PartyHint.cs)
                if (sc.look == "pool_jump") BuildPoolJump(sc, terrain, mats, sroot);   // Round 20 follow-up: R1's pool, fences, gate, patio (MapBuilder.R1Pool.cs)
                if (sc.big_jump != null && sc.big_jump.lip != null && sc.big_jump.lip.Length == 3)
                {
                    var bj = sroot.gameObject.AddComponent<BigJump>(); bj.id = sc.id; bj.look = sc.look;
                    bj.lip = new Vector3(sc.big_jump.lip[0], sc.big_jump.lip[1], sc.big_jump.lip[2]); bj.dir = new Vector3(sc.big_jump.dir[0], 0f, sc.big_jump.dir[1]).normalized; bj.vmax = sc.big_jump.vmax; bj.halfWidth = sc.big_jump.half; bj.angle = sc.big_jump.angle;
                }
                if (sc.landing_zones != null)
                    foreach (var z in sc.landing_zones)   // Round 11: ground the car lands on, with the landing carry (LandingZone, RampHold)
                    {
                        var go = new GameObject("Landing zone"); go.transform.SetParent(sroot, false);
                        var lz = go.AddComponent<LandingZone>(); lz.outline = ToPoints(z.poly).ToArray(); lz.hold = z.hold;
                    }
            }
            Debug.Log($"[Shortcuts] {hood.ShortcutData.shortcuts.Length} shortcuts, {prisms} ramps / decks / floors, {hood.OpenHouses.Count} houses opened, {hood.JumpHouses.Count} jumped; {hood.TreesCleared} trees kept off the lines");
        }

        // Round 24 A3, item B2 (Dan's session 2026-10-13_16-28, BUG-001 on #2's run-up: "we need the tire marks here. Because it isnt
        // really obvious that you are supposed to go through the sliding glass door"): worn tire tracks in the grass along the line
        // from where it leaves the street to 1.5 m short of the open house's sliding glass door (WornRuts, as R3's and R1's)
        static void SlidingDoorTracks(Neighborhood hood, TerrainGrid terrain, Materials mats, Transform parent, ScShortcut sc)
        {
            var oh = sc.open_houses?.FirstOrDefault(o => o.sliding_door_a && o.a != null && o.a.Length >= 2);
            if (oh == null || sc.path == null) return;
            var p = ToPoints(sc.path); var door = new Vector2(oh.a[0], oh.a[1]);
            int i0 = p.FindIndex(q => hood.RoadClearance(q) > 0.3f);
            int i1 = Enumerable.Range(0, p.Count).OrderBy(k => (p[k] - door).sqrMagnitude).First();
            while (i1 > 0 && (p[i1] - door).magnitude < 1.5f) i1--;
            if (i0 < 0 || i1 <= i0 + 2) { Debug.LogWarning($"[Round24] {sc.id}: no room for worn tracks to the sliding door"); return; }
            var line = p.GetRange(i0, i1 - i0 + 1);
            WornRuts(terrain, mats, parent, line, sc.id + " worn tracks to the sliding door");
            float len = 0f; for (int k = 1; k < line.Count; k++) len += Vector2.Distance(line[k - 1], line[k]);
            Debug.Log($"[Round24] {sc.id}: worn tire tracks {len:0} m from the street at ({line[0].x:0.0}, {line[0].y:0.0}) to the sliding glass door of house {oh.building}");
        }

        // Round 12, item 3: a home-made sign at the turn-off: a plywood board 1.6 x 0.8 m on two posts, its painted face
        // (Tools/handmade/make_shortcut_sign.py) on the front only, lit and depth-tested (hidden by terrain like the board). It
        // stands 3 m or more off the line; breakable since Round 16.
        static void BuildShortcutSign(Neighborhood hood, TerrainGrid terrain, Materials mats, Transform parent, ScShortcut sc)
        {
            if (!hood.SignSpots.TryGetValue(sc.id, out var s)) return;
            if (sc.sign_face == "party" || sc.sign_face == "jump") { BuildPartySign(hood, terrain, mats, parent, sc, s); return; }   // Round 19 (5, 6): R3's party hint, R1's JUMP sign (MapBuilder.PartyHint.cs)
            var f = MakeFrame(new Vector3(s.at.x, 0f, s.at.y), s.facing, 1f);
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (float u in new[] { -0.6f, 0.6f }) { var q = f.P(u, 0f, -0.05f); float g = Ground(terrain, q.x, q.z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            f.O.y = hi;
            // Round 16 (D): breakable like a mailbox (FenceChunk: a trigger, a little speed lost, a wood sound, whole again at race
            // start); posts and board fly in pieces, the painted face with them
            var br = new Breakables();
            var mb = br.Begin(s.at);
            var post = new Color(0.40f, 0.31f, 0.22f); var edge = new Color(0.62f, 0.52f, 0.38f);
            foreach (float u in new[] { -0.6f, 0.6f }) br.Piece(() => BoxF(mb, f, u - 0.045f, u + 0.045f, lo - hi - 0.3f, 1.85f, -0.1f, -0.01f, post), lo + 0.05f);
            foreach (float u in new[] { -0.4f, 0.4f }) br.Piece(() => BoxF(mb, f, u - 0.4f, u + 0.4f, 1.0f, 1.8f, -0.01f, 0.03f, edge), lo + 0.05f);
            var mat = ShortcutSignMaterial();
            if (mat != null)
            {
                var plate = SignPlate(parent, f.P(0f, 1.4f, 0.035f), -f.F, 1f, mat);   // the quad's front is -z locally
                plate.name = "Shortcut sign face " + sc.id;
                plate.transform.localScale = new Vector3(1.6f, 0.8f, 1f);
                br.Face(plate);
            }
            br.End(f.P(0f, 1.0f, -0.03f), f.F, 1.9f, 2.2f, 0.8f);
            br.ToObjects("Shortcut sign " + sc.id, mats.House, parent, 1f, BreakSounds.Kind.Wood);
            Debug.Log($"[Round12] {sc.id}: sign at ({s.at.x:0.0}, {s.at.y:0.0}), facing ({s.facing.x:0.00}, {s.facing.y:0.00})");
        }

        static Material ShortcutSignMaterial()
        {
            const string path = "Assets/HandMade/Shortcut/ShortcutSign.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("[Round12] " + path + " missing (Tools/handmade/make_shortcut_sign.py): a blank sign"); return null; }
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "ShortcutSign" };
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.1f);
            m.SetFloat("_Cull", (float)CullMode.Back);
            m = GenAsset(m, GeneratedDir + "/ShortcutSign.mat");
            return m;
        }

        // Ramps and decks are frictionless to the car's BODY. The car's grip comes from its own suspension rays
        // (ArcadeVehicle), not from collider friction, so this changes nothing while it drives; but on a kicker steep enough
        // to clear a house, the soft suspension lets the body touch the ramp, and with friction that contact stopped the car.
        static PhysicsMaterial rampMaterial;
        static PhysicsMaterial RampMaterial()
        {
            // Round 42: only ever the asset as stored (the object made here stopped being it partway through a full build: the crypt's
            // colliders, made after the houses' assets were saved, were saved with no material; a quick build gave them this one)
            // and never made again over itself (that leaves what was given the old one with nothing)
            if (rampMaterial && AssetDatabase.Contains(rampMaterial)) return rampMaterial;
            string path = GeneratedDir + "/ShortcutRamp.physicMaterial";
            var fresh = new PhysicsMaterial("ShortcutRamp") { dynamicFriction = 0f, staticFriction = 0f, bounciness = 0f, frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum };
            FlushAssets(); var stored = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (stored) { EditorUtility.CopySerialized(fresh, stored); stored.name = "ShortcutRamp"; EditorUtility.SetDirty(stored); rampMaterial = stored; }
            else { AssetDatabase.CreateAsset(fresh, path); rampMaterial = fresh; }
            return rampMaterial;
        }

        // Round 18 (R3): the slide's foot on the ground as built (its profile was made from the heights before the map build; the
        // small vehicles went under a foot that stood above the grass): the difference eased out over its first 6 m, the top kept
        static void FitFootToGround(ScPrism pr, TerrainGrid terrain)
        {
            float delta = Ground(terrain, pr.origin[0], pr.origin[1]) - 0.03f - pr.profile[1];
            for (int i = 0; i + 1 < pr.profile.Length; i += 2) pr.profile[i + 1] += delta * (1f - Smooth01(pr.profile[i] / 6f));
            Debug.Log($"[Round18] {pr.name}: its foot moved {delta:+0.00;-0.00} m onto the ground");
        }

        // Round 54 (D3, Dan BUG-008 "i kept losing momentum on this slide"): the slide's foot flares to 9 m wide over ground that is not level across it, so
        // off its middle the foot's edge stood out of the grass and a vehicle met it square (the Drifter Twin 17 to 5 m/s in one step). The ground under
        // the foot's first 2 m and for 4 m before it is set to the slide's own surface there, across its whole width and a metre beyond: it runs onto
        // the board with no edge
        static void FitGroundToSlideFoot(ScPrism pr, TerrainGrid terrain, ScSlide sl)
        {
            var O = new Vector2(pr.origin[0], pr.origin[1]); var U = new Vector2(pr.axis[0], pr.axis[1]).normalized; var N = new Vector2(-U.y, U.x);
            float half = (sl != null && sl.foot_half > 0f ? sl.foot_half : 4.5f) + 1f, most = 0f;
            var area = Rect.MinMaxRect(O.x - 8f, O.y - 8f, O.x + 8f, O.y + 8f);
            EditTerrain(terrain, area, (x, z, y) =>
            {
                var q = new Vector2(x, z) - O; float s = Vector2.Dot(q, U), lat = Mathf.Abs(Vector2.Dot(q, N));
                if (s < -4f || s > 2f || lat > half + 1.5f) return y;
                float target = Profile(pr.profile, Mathf.Max(0f, s)) - 0.03f;
                float w = (s < 0f ? Smooth01((s + 4f) / 4f) : 1f) * (lat <= half ? 1f : Smooth01(1f - (lat - half) / 1.5f));
                float ny = y + w * (target - y); most = Mathf.Max(most, Mathf.Abs(ny - y)); return ny;
            });
            Debug.Log($"[Round54] D3: the ground under {pr.name}'s foot set to its surface, moved by up to {most:0.00} m");
        }

        static float Profile(float[] pr, float s)
        {
            int n = pr.Length / 2;
            if (s <= pr[0]) return pr[1];
            for (int i = 0; i + 1 < n; i++)
                if (s <= pr[2 * i + 2]) return Mathf.Lerp(pr[2 * i + 1], pr[2 * i + 3], Mathf.InverseLerp(pr[2 * i], pr[2 * i + 2], s));
            return pr[2 * n - 1];
        }

        // Round 14, item 7 (Dan chose a dirt pile: "4 was actually closer to what I was thinking initally"): the kicker drawn
        // as one big mound of packed brown earth, as if dumped for landscaping or building work. The Round 13 flatbed (deck,
        // rails, steel ramps, beams, wheels, cribbing) is gone. LOOK ONLY: the face the car drives up is drawn on the kicker's
        // own surface (Top), the same curve, pitch, height and lip, and the collider (made from the prism, BuildPrism) is
        // unchanged. Round the whole outline the earth falls away to the grass: from the top's edge over a rounded shoulder
        // to a toe on the ground, about 40 degrees, wider where the top stands higher and longest behind the lip, the
        // outline wavering by up to a fifth so it reads as a heap and not a ramp. The skirt runs from the top's edge down to
        // just under the ground, so nothing floats and nothing is open underneath.
        // Round 16 (B, BUG-002): the skirt is solid too ('heap', the same faces, a collider of its own made by BuildPrism):
        // before, it had none and a vehicle drove into it up to the prism's upright side under it. Its top edge is the driven
        // face's own edge, so the face, the lip and the jump are unchanged.
        static MeshBuilder DirtPileLook(List<Vector2> poly, Vector2 O, Vector2 U, Vector2 Lv, List<float> cuts, Func<Vector2, Vector3> Top, TerrainGrid terrain, out MeshBuilder heap)
        {
            var mb = new MeshBuilder();
            heap = new MeshBuilder();
            float S(Vector2 p) => Vector2.Dot(p - O, U);
            var packed = new Color(0.40f, 0.28f, 0.18f);   // the driven face: packed, darker
            var loose = new Color(0.47f, 0.34f, 0.22f);    // the heap's sides: drier, looser
            // the driven face, cell by cell (0.5 m strips, about 1.2 m across), each cell a slightly different shade of earth
            var Lmin = poly.Min(p => Vector2.Dot(p - O, Lv)); var Lmax = poly.Max(p => Vector2.Dot(p - O, Lv));
            int across = Mathf.Max(1, Mathf.RoundToInt((Lmax - Lmin) / 1.2f));
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                if (cuts[i + 1] - cuts[i] < 1e-3f) continue;
                var strip = ClipPoly2(ClipPoly2(poly, p => S(p) - cuts[i]), p => cuts[i + 1] - S(p));
                for (int k = 0; k < across; k++)
                {
                    float la = Mathf.Lerp(Lmin, Lmax, k / (float)across), lb = Mathf.Lerp(Lmin, Lmax, (k + 1) / (float)across);
                    var cell = ClipPoly2(ClipPoly2(strip, p => Vector2.Dot(p - O, Lv) - la), p => lb - Vector2.Dot(p - O, Lv));
                    if (cell.Count < 3) continue;
                    mb.Tint = Shade(packed, 0.96f + 0.07f * Hash01(i * 37 + k, 311));   // barely varied: packed earth, not boards
                    int st = mb.Count; foreach (var p in cell) mb.Vertex(Top(p));
                    for (int t = 1; t + 1 < cell.Count; t++)
                    {
                        Vector3 p0 = mb.Verts[st], p1 = mb.Verts[st + t], p2 = mb.Verts[st + t + 1];
                        if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), Vector3.up) >= 0f) mb.Tri(st, st + t, st + t + 1); else mb.Tri(st, st + t + 1, st + t);
                    }
                }
            }
            // the outline walked round every 0.5 m, with extra stations at each corner so the skirt turns the corner round
            Vector2 centre = Vector2.zero; foreach (var p in poly) centre += p; centre /= poly.Count;
            Vector2 OutN(Vector2 a, Vector2 b) { var d = (b - a).normalized; var n = new Vector2(d.y, -d.x); return Vector2.Dot(n, (a + b) * 0.5f - centre) < 0f ? -n : n; }
            var ring = new List<(Vector2 p, Vector2 n, float along)>(); float run = 0f;
            for (int e = 0; e < poly.Count; e++)
            {
                var a = poly[e]; var b = poly[(e + 1) % poly.Count]; var nPrev = OutN(poly[(e + poly.Count - 1) % poly.Count], a); var n = OutN(a, b);
                float turn = Vector2.SignedAngle(nPrev, n); int fan = Mathf.CeilToInt(Mathf.Abs(turn) / 15f);
                for (int f = 1; f < fan; f++) ring.Add((a, (Vector2)(Quaternion.Euler(0, 0, turn * f / fan) * nPrev), run));
                int m = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.5f));
                for (int i = 0; i < m; i++) { ring.Add((Vector2.Lerp(a, b, i / (float)m), n, run)); run += Vector2.Distance(a, b) / m; }
            }
            // per station: the top edge, a shoulder a third of the way out (a quarter of the way down: rounded), the toe on the grass
            var top = new Vector3[ring.Count]; var shoulder = new Vector3[ring.Count]; var toe = new Vector3[ring.Count];
            for (int i = 0; i < ring.Count; i++)
            {
                var (p, n, along) = ring[i];
                var t = Top(p); float g = Ground(terrain, p.x, p.y), h = Mathf.Max(0f, t.y - g);
                // a slow, uneven waver round the outline (two long waves and a short one), up to about a fifth of the width
                float waver = 1f + 0.12f * Mathf.Sin(along * 0.37f + 1.3f) + 0.07f * Mathf.Sin(along * 0.91f + 4.1f) + 0.04f * Mathf.Sin(along * 2.3f);
                // about 40 degrees on the sides (dry earth's angle of repose), a longer fall behind the lip
                float w = (0.3f + h * Mathf.Lerp(1.2f, 1.6f, Mathf.Clamp01(Vector2.Dot(n, U)))) * waver;
                var pt = p + n * w; var ps = p + n * (w * 0.34f);
                top[i] = t;
                toe[i] = new Vector3(pt.x, Ground(terrain, pt.x, pt.y) - 0.06f, pt.y);
                float gs = Ground(terrain, ps.x, ps.y);
                shoulder[i] = new Vector3(ps.x, Mathf.Max(gs - 0.02f, Mathf.Lerp(t.y, toe[i].y, 0.24f) + 0.05f * Mathf.Sin(along * 1.7f)), ps.y);
            }
            for (int i = 0; i < ring.Count; i++)
            {
                int j = (i + 1) % ring.Count;
                var outward = new Vector3(ring[i].n.x + ring[j].n.x, 0.8f, ring[i].n.y + ring[j].n.y);
                mb.Tint = Shade(loose, 0.82f + 0.2f * Hash01(i, 313));
                if ((top[i] - top[j]).sqrMagnitude > 1e-6f) { Face(mb, top[i], top[j], shoulder[j], shoulder[i], outward); Face(heap, top[i], top[j], shoulder[j], shoulder[i], outward); }
                else { TriFace(mb, top[i], shoulder[j], shoulder[i], outward); TriFace(heap, top[i], shoulder[j], shoulder[i], outward); }   // a corner fan: the top edge is one point
                mb.Tint = Shade(loose, 0.9f + 0.2f * Hash01(i, 317));
                Face(mb, shoulder[i], shoulder[j], toe[j], toe[i], outward); Face(heap, shoulder[i], shoulder[j], toe[j], toe[i], outward);
            }
            // a few clods of earth rolled down to the toe
            int clods = 0;
            for (int i = 0; i < ring.Count; i += 7)
            {
                if (Hash01(i, 319) < 0.45f) continue;
                var c = Vector3.Lerp(shoulder[i], toe[i], 0.85f + 0.2f * Hash01(i, 321)); var cg = new Vector2(c.x, c.z);
                c.y = Ground(terrain, cg.x, cg.y); float r = 0.18f + 0.22f * Hash01(i, 323);
                mb.Tint = Shade(loose, 0.75f + 0.2f * Hash01(i, 325));
                Vector3[] d = { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };
                var apex = c + Vector3.up * r * 0.9f; var foot = c - Vector3.up * r * 0.3f;
                for (int k = 0; k < 4; k++)
                {
                    var a = c + d[k] * r; var b = c + d[(k + 1) % 4] * r;
                    TriFace(mb, a, b, apex, (d[k] + d[(k + 1) % 4]) + Vector3.up);
                    TriFace(mb, a, b, foot, (d[k] + d[(k + 1) % 4]) - Vector3.up);
                }
                clods++;
            }
            Debug.Log($"[Round14] #6 dirt pile: {ring.Count} stations round the outline ({run:0} m), widest skirt {ring.Select((r, i) => Vector2.Distance(r.p, new Vector2(toe[i].x, toe[i].z))).Max():0.0} m, {clods} clods");
            return mb;
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
                mb.Tint = stairs ? Shade(col, (Mathf.FloorToInt((a - s0) / step + 0.01f) & 1) == 0 ? 1.08f : 0.62f)
                        : pr.kind == "rock" ? (Hash01(i, 951) > 0.86f ? Shade(new Color(0.52f, 0.56f, 0.46f), 0.9f + 0.15f * Hash01(i, 953)) : Shade(col, 0.84f + 0.2f * Hash01(i, 955)))   // Round 19: granite, mottled, a band of lichen here and there
                        : Shade(col, (Mathf.FloorToInt(a / 0.5f) & 1) == 0 ? 1f : 0.93f);
                int st = mb.Count;
                foreach (var p in piece) mb.Vertex(Top(p));
                for (int k = 1; k + 1 < piece.Count; k++)
                {
                    // wind so the face points up
                    Vector3 p0 = mb.Verts[st], p1 = mb.Verts[st + k], p2 = mb.Verts[st + k + 1];
                    if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), Vector3.up) >= 0f) mb.Tri(st, st + k, st + k + 1); else mb.Tri(st, st + k + 1, st + k);
                }
            }
            // Round 18 (R3): the play set's slide and deck are "open": a 0.25 m thick board on legs (MapBuilder.Playground.cs),
            // not a solid down to the ground: the underside is the top 0.25 m lower, the edges a 0.25 m band
            bool open = pr.kind == "slide" || pr.kind == "deck" || pr.kind == "deckland";   // Round 19: deckland, an open board that is a landing (BJ1's fallen span)
            if (open)
            {
                int tops = mb.Tris.Count;
                for (int i = 0; i + 1 < cuts.Count; i++)
                {
                    if (cuts[i + 1] - cuts[i] < 1e-3f) continue;
                    var piece = ClipPoly2(ClipPoly2(poly, p => S(p) - cuts[i]), p => cuts[i + 1] - S(p));
                    if (piece.Count < 3) continue;
                    mb.Tint = Shade(col, 0.6f); int st = mb.Count;
                    foreach (var p in piece) mb.Vertex(Top(p) - Vector3.up * 0.25f);
                    for (int k = 1; k + 1 < piece.Count; k++)
                    {
                        Vector3 p0 = mb.Verts[st], p1 = mb.Verts[st + k], p2 = mb.Verts[st + k + 1];
                        if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), Vector3.down) >= 0f) mb.Tri(st, st + k, st + k + 1); else mb.Tri(st, st + k + 1, st + k);
                    }
                }
            }
            // sides: each outline edge in 0.5 m pieces, from the top down to 0.5 m below the ground
            Vector2 centre = Vector2.zero; foreach (var p in poly) centre += p; centre /= poly.Count;
            var skirt = new HashSet<int>(pr.skirt_edges ?? new int[0]); var noFace = new HashSet<int>(pr.no_face ?? new int[0]);
            mb.Tint = Shade(col, 0.72f); int wedges = 0;
            for (int e = 0; e < poly.Count; e++)
            {
                if (noFace.Contains(e)) continue;   // Round 21: a back's edge against its take-off's lip (that face caught a vehicle's nose)
                var a = poly[e]; var b = poly[(e + 1) % poly.Count];
                var mid = (a + b) * 0.5f; var dir = (b - a).normalized; var outN = new Vector2(dir.y, -dir.x);
                if (Vector2.Dot(outN, mid - centre) < 0f) outN = -outN;
                int k = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.5f));
                for (int i = 0; i < k; i++)
                {
                    var p = Vector2.Lerp(a, b, i / (float)k); var q = Vector2.Lerp(a, b, (i + 1) / (float)k);
                    float gp = Ground(terrain, p.x, p.y) - 0.5f, gq = Ground(terrain, q.x, q.y) - 0.5f;
                    var tp = Top(p); var tq = Top(q);
                    // Round 26 (BUG-001, a ramp's foot was a wall): an edge standing a little proud of the ground (up to 0.3 m: a foot, a
                    // board's end, the low start of a side) meets the ground with a thin wedge (15 degrees) instead of an upright face, so a
                    // wheel rolls up it; higher edges are a ramp's sides and keep their face or skirt
                    float hpw = tp.y - gp - 0.5f, hqw = tq.y - gq - 0.5f;
                    if (!stairs && !skirt.Contains(e) && Mathf.Max(hpw, hqw) > 0.005f && Mathf.Max(hpw, hqw) <= 0.3f)
                    {
                        const float Cot15 = 3.73f;
                        var po = p + outN * (Mathf.Max(0f, hpw) * Cot15 + 0.05f); var qo = q + outN * (Mathf.Max(0f, hqw) * Cot15 + 0.05f);
                        Face(mb, new Vector3(po.x, Ground(terrain, po.x, po.y) - 0.03f, po.y), new Vector3(qo.x, Ground(terrain, qo.x, qo.y) - 0.03f, qo.y), tq, tp, new Vector3(outN.x, Cot15, outN.y));
                        wedges++;
                        if (!open) continue;
                    }
                    if (open) { Face(mb, tp - Vector3.up * 0.25f, tq - Vector3.up * 0.25f, tq, tp, new Vector3(outN.x, 0f, outN.y)); continue; }   // Round 18: the board's edge
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
            if (wedges > 0) Debug.Log($"[Round26] {pr.name}: {wedges} low edge pieces (0.5 m) meet the ground with a wedge");
            // Round 12, item 9 / Round 13, item 2 / Round 14, item 7: the kicker's LOOK only (a dirt pile since Round 14, a flatbed in Round 13, hay bales in Round
            // 12). The surface, its curve, pitch, height, lip and the collider below are exactly as before (the collider is still
            // made from mb); what is drawn sits on that same surface and is held up from the ground, so nothing floats
            MeshBuilder heap = null;
            var look = pr.kind == "kicker" ? DirtPileLook(poly, O, U, Lv, cuts, Top, terrain, out heap) : mb;
            var go = look.ToObject(pr.name, mats.House, parent, false);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            var surf = go.AddComponent<ShortcutSurface>(); surf.held = pr.held; surf.landing = pr.kind == "landing" || pr.kind == "deckland" || pr.landing;   // Round 10: RampHold
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
            cm = GenAsset(cm, $"{GeneratedDir}/Meshes/{pr.name} collider.asset");
            var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = cm; mc.sharedMaterial = RampMaterial();
            // Round 16 (B): the heap's skirt, solid, frictionless like the ramp, on a child (not a held surface; its name starts
            // "sc" so VehicleSurfaceContacts treats it as a ramp surface: its slope, not the triangle edges, pushes the body)
            if (heap != null && heap.Tris.Count > 0)
            {
                var hm = new Mesh { name = pr.name + " heap collider", indexFormat = IndexFormat.UInt32 };
                hm.SetVertices(heap.Verts); hm.SetTriangles(heap.Tris, 0); hm.RecalculateNormals(); hm.RecalculateBounds();
                hm = GenAsset(hm, $"{GeneratedDir}/Meshes/{pr.name} heap collider.asset");
                var hgo = new GameObject(pr.name + " heap"); hgo.transform.SetParent(go.transform, false);
                var hc = hgo.AddComponent<MeshCollider>(); hc.sharedMesh = hm; hc.sharedMaterial = RampMaterial();
                Debug.Log($"[Round16] B (BUG-002): {pr.name}: the dirt pile's skirt is solid ({heap.Tris.Count / 3} triangles)");
            }
        }
    }
}
