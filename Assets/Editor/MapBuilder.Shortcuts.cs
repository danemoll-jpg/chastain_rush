using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 8, item 4: the official shortcuts' physical side, built from Data/shortcuts/chastain_glen_loop.json
// (Tools/mapgen/make_shortcuts.py). The race side (entry plane, forgiven checkpoints) is the route file's official_shortcuts.
//   prisms        ramps, kickers, landing decks and passage floors: solid, with mesh colliders (the car drives on them)
//   open houses   a house the line goes THROUGH: its mesh is clipped by the passage (both walls it crosses, and anything in
//                 between, up to the passage height), the passage gets walls, a ceiling and a little decoration, and the
//                 house's single box collider is replaced by boxes around the passage
//   jump houses   a house the line goes OVER: it keeps its box and also gets a collider from its own mesh, so a car that comes
//                 up short lands on the sloped roof and slides off it instead of sitting on a flat box top
//   trail         a dirt trail painted on the ground along the line (Racer marks its backyard shortcuts with a dirt colour
//                 painted into the ground, BackyardForwardAuthoring: the same colour is used here)
//   corridor      trees, lamps, hydrants, mailboxes and front-yard shrubs are kept off the line; fences stay (they break)
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class ScPrism { public string name, kind; public float[] poly, origin, axis, profile, colour, bank; public int[] skirt_edges; }   // bank: (station, rise per metre to the left) pairs
        [Serializable] class ScOpenHouse { public int building; public float[] a, b; public float half_width, floor_y, height; }
        [Serializable] class ScJumpHouse { public int building; }
        [Serializable] class ScShortcut { public string id, name; public int[] forgives; public float[] path, trail; public float half_width, clear_half_width, entry_s; public ScPrism[] prisms; public ScOpenHouse[] open_houses; public ScJumpHouse[] jump_houses; }
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
                var tm = new MeshBuilder(); var td = new MeshBuilder();
                h.detail = td; BuildHouse(tm, h); h.detail = db;
                Passage(oh, out var A, out var u, out var n);
                float top = oh.floor_y + oh.height;
                ClipAppend(tm, mb, A, n, oh.half_width, top);
                ClipAppend(td, db, A, n, oh.half_width, top);
                PassageInterior(mb, db, h, oh, A, u, n);
                c.Log.Append($" open house {h.index} (style {h.style}, floor {h.floorY:0.00}, passage floor {oh.floor_y:0.00}, {oh.half_width * 2f:0.0} x {oh.height:0.0} m);");
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

        // Copies src into dst without what lies inside the passage: |lateral| < half and y < top. Each triangle is cut by the
        // three planes in turn (Sutherland-Hodgman); the parts outside any of them are kept, the part inside all three dropped.
        static void ClipAppend(MeshBuilder src, MeshBuilder dst, Vector2 A, Vector2 n, float half, float top)
        {
            var V = src.Verts; var C = src.Colours; var T = src.Tris;
            Func<Vector3, float>[] planes =
            {
                p => Vector2.Dot(new Vector2(p.x, p.z) - A, n) - half,
                p => -half - Vector2.Dot(new Vector2(p.x, p.z) - A, n),
                p => p.y - top,
            };
            for (int i = 0; i + 2 < T.Count; i += 3)
            {
                var poly = new List<(Vector3 p, Color c)> { (V[T[i]], C[T[i]]), (V[T[i + 1]], C[T[i + 1]]), (V[T[i + 2]], C[T[i + 2]]) };
                bool whole = false;
                foreach (var f in planes) if (poly.All(q => f(q.p) >= 0f)) { whole = true; break; }
                if (whole) { Emit(dst, poly); continue; }
                foreach (var f in planes)
                {
                    var outside = Cut(poly, f, true); var inside = Cut(poly, f, false);
                    if (outside.Count >= 3) Emit(dst, outside);
                    poly = inside;
                    if (poly.Count < 3) break;
                }
            }
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

        static void PassageInterior(MeshBuilder mb, MeshBuilder db, HouseSpec h, ScOpenHouse oh, Vector2 A, Vector2 u, Vector2 n)
        {
            var rect = HouseRect(h);
            float y0 = oh.floor_y, y1 = oh.floor_y + oh.height, hw = oh.half_width;
            Vector3 W(Vector2 p, float y) => new Vector3(p.x, y, p.y);
            // the passage's side walls: the lines lateral = +-hw inside the rectangle
            foreach (float side in new[] { -1f, 1f })
            {
                var piece = ClipPoly2(ClipPoly2(rect, p => Vector2.Dot(p - A, n) * side - hw + 0.02f), p => hw + 0.02f - Vector2.Dot(p - A, n) * side);
                if (piece.Count < 2) continue;
                float t0 = piece.Min(p => Vector2.Dot(p - A, u)), t1 = piece.Max(p => Vector2.Dot(p - A, u));
                Vector2 a = A + n * side * hw + u * t0, b = A + n * side * hw + u * t1;
                var inward = new Vector3(-n.x * side, 0f, -n.y * side);
                mb.Tint = PassageWall;
                Face(mb, W(a, y0 - 0.3f), W(b, y0 - 0.3f), W(b, y1), W(a, y1), inward);
                // a baseboard, a picture and a door frame painted on the wall (flat quads, no shadows)
                db.Tint = TrimWhite;
                Vector2 m0 = a + (b - a) * 0.5f;
                var inw2 = new Vector2(inward.x, inward.z) * 0.03f;
                Face(db, W(a + inw2, y0), W(b + inw2, y0), W(b + inw2, y0 + 0.12f), W(a + inw2, y0 + 0.12f), inward);
                float pw = Mathf.Min(1.2f, (t1 - t0) * 0.2f);
                db.Tint = new Color(0.25f, 0.18f, 0.12f);
                Face(db, W(m0 - u * pw + inw2, y0 + 1.3f), W(m0 + u * pw + inw2, y0 + 1.3f), W(m0 + u * pw + inw2, y0 + 2.2f), W(m0 - u * pw + inw2, y0 + 2.2f), inward);
                db.Tint = side > 0 ? new Color(0.35f, 0.55f, 0.75f) : new Color(0.62f, 0.48f, 0.30f);
                Face(db, W(m0 - u * (pw - 0.1f) + inw2 * 2f, y0 + 1.4f), W(m0 + u * (pw - 0.1f) + inw2 * 2f, y0 + 1.4f), W(m0 + u * (pw - 0.1f) + inw2 * 2f, y0 + 2.1f), W(m0 - u * (pw - 0.1f) + inw2 * 2f, y0 + 2.1f), inward);
            }
            // the ceiling: the passage strip inside the rectangle, facing down, with a light
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

        // The house's collider without the passage: boxes in the passage's frame, 0.5 m slices of the rectangle on either side
        // of the passage, and one block over it (from the passage ceiling to the top of the old box).
        static void OpenHouseColliders(HoodCtx c, HouseSpec h, Transform parent)
        {
            var oh = c.hood.OpenHouses[h.index];
            Passage(oh, out var A, out var u, out var n);
            var rect = HouseRect(h);
            float hh = h.style == HouseStyle.C ? 3.0f : 5.9f;
            float yb = h.baseY, yt = h.floorY + hh;
            var root = new GameObject("House " + h.index + " (open: shortcut passage)").transform; root.SetParent(parent, false);
            var rot = Quaternion.LookRotation(new Vector3(u.x, 0f, u.y), Vector3.up);
            int boxes = 0;
            void Box(float l0, float l1, float y0, float y1)
            {
                var piece = ClipPoly2(ClipPoly2(rect, p => Vector2.Dot(p - A, n) - l0), p => l1 - Vector2.Dot(p - A, n));
                if (piece.Count < 3 || y1 - y0 < 0.05f) return;
                float t0 = piece.Min(p => Vector2.Dot(p - A, u)), t1 = piece.Max(p => Vector2.Dot(p - A, u));
                if (t1 - t0 < 0.05f) return;
                var cxz = A + n * ((l0 + l1) / 2f) + u * ((t0 + t1) / 2f);
                var go = new GameObject("box"); go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(new Vector3(cxz.x, (y0 + y1) / 2f, cxz.y), rot);
                var bc = go.AddComponent<BoxCollider>(); bc.size = new Vector3(l1 - l0, y1 - y0, t1 - t0);
                boxes++;
            }
            float lmin = rect.Min(p => Vector2.Dot(p - A, n)), lmax = rect.Max(p => Vector2.Dot(p - A, n));
            for (float l = oh.half_width; l < lmax; l += 0.5f) Box(l, Mathf.Min(l + 0.5f, lmax), yb, yt);
            for (float l = -oh.half_width; l > lmin; l -= 0.5f) Box(Mathf.Max(l - 0.5f, lmin), l, yb, yt);
            float top = oh.floor_y + oh.height;
            if (yt > top + 0.1f) Box(-oh.half_width, oh.half_width, top, yt);
            else Box(-oh.half_width, oh.half_width, top, top + 1.5f);
        }

        // ------------------------------------------------------------ ramps, decks, the trail

        static void BuildShortcuts(Neighborhood hood, TerrainGrid terrain, Materials mats, Transform world)
        {
            if (hood == null || hood.ShortcutData == null) return;
            var root = new GameObject("Shortcuts").transform; root.SetParent(world, false);
            rampMaterial = null;
            var trail = new Chunks { Tint = DirtTrail };
            int prisms = 0;
            foreach (var sc in hood.ShortcutData.shortcuts)
            {
                var sroot = new GameObject("Shortcut " + sc.id + " (" + sc.name + ")").transform; sroot.SetParent(root, false);
                foreach (var pr in sc.prisms) { BuildPrism(pr, terrain, mats, sroot); prisms++; }
                // the dirt trail: on the ground only (not on roads, not under decks or through houses)
                var path = ToPoints(sc.path);
                var arc = new float[path.Count];
                for (int i = 1; i < path.Count; i++) arc[i] = arc[i - 1] + Vector2.Distance(path[i - 1], path[i]);
                for (int k = 0; k + 1 < sc.trail.Length; k += 2)
                {
                    var run = new List<Vector2>();
                    void Flush() { if (run.Count >= 2) foreach (var piece in Runs(run, 96f)) AddRibbon(trail.At(piece[piece.Count / 2]), piece, sc.half_width * 0.9f, 0.05f, terrain); run.Clear(); }
                    for (int i = 0; i < path.Count; i++)
                    {
                        if (arc[i] < sc.trail[k] || arc[i] > sc.trail[k + 1]) continue;
                        bool onRoad = hood.Present && hood.RoadClearance(path[i]) < 1.0f;
                        if (onRoad) Flush(); else run.Add(path[i]);
                    }
                    Flush();
                }
            }
            trail.ToObjects("ShortcutTrail", mats.Driveway, root, false);
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
            for (float s = s0; s < s1; s += 0.5f) cuts.Add(s);
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
                mb.Tint = Shade(col, (Mathf.FloorToInt(a / 0.5f) & 1) == 0 ? 1f : 0.93f);
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
