using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 60 (Part A, Dan 2026-10-22: "Can we make town center look like town center ... It is a two level mall. I would want to be able to drive
// through it and have smashable things throughout"). Town Center at Cobb, built from its six OSM footprints (Round 59 placed them; type "mall"):
// the concourse building (the largest) and its five anchors. Dan's photos (local/reference_photos/town_center/, reference only, never traced or
// used as textures) set the look; every store and brand name is made up (Tools/handmade/make_mall_signs.py).
//   The frame: u along the concourse's long walls (about 14.75 degrees north of east), v across it (north). The footprints are square to it.
//   The levels: the ground itself has two (lower 305.9 m on the north, west and south-west; upper 313.4 m on the east, south-east and north-west,
//     7.5 m apart at the 1.5 scale), so the lower entrances open on the north and south-west lots and the upper ones on the east, south and
//     north-west, as at the real mall.
//   Outside (this file): the concourse building's walls in tan split-face block, the main entrance's glass tower with its stepped parapet, the side
//     entrances' curved canopies on stone piers, the five anchors as solid blocks with their own portals and (made-up) names, the closed former
//     anchor in the north-west; the barrel-vault skylights and the food court's pyramid rising over the roof. The lots and the ring road are the
//     businesses' (Round 31), as they were.
//   Inside: MapBuilder.MallInside.cs. The run-time half (the inside drawn only near it, the breakables put back, lights at night, the echo):
//     Assets/Scripts/World/TownCenterMall.cs.
// World, not game layer (Free Roam only this round). Quick-buildable alone: step "Mall" (its grading is in the planning: PlanMall).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        sealed class MallRamp
        {
            public string name; public float uFoot, run, v0, v1; public int dir;   // dir +1: rises toward +u
            public List<Vector2> prof;
            public float uTop => uFoot + dir * run;
            public float U0 => Mathf.Min(uFoot, uTop); public float U1 => Mathf.Max(uFoot, uTop);
        }
        sealed class MallDoor
        {
            public string name; public Vector2 a, b, nOut; public float floor, height; public int level; public bool main, anchor;
            public Vector2 Mid => (a + b) * .5f; public float Width => Vector2.Distance(a, b);
        }
        sealed class MallPlan
        {
            public Vector2 U, V;
            public int Spine = -1, East = -1, West = -1, South = -1, North = -1, NorthWest = -1;
            public List<Vector2> SpineRing; public readonly Dictionary<int, List<Vector2>> Rings = new();
            public readonly HashSet<int> Owned = new();
            public float L0 = 305.9f, L1 = 313.4f, Roof = 320.6f, AnchorTop = 323.2f;   // floors, the concourse's roof top, the anchors' parapets
            public const float Slab = .5f, Parapet = 1.2f, WallT = .4f;
            public readonly List<MallRamp> Ramps = new(); public readonly List<Rect> Voids = new(); public Rect Court, Tower;
            public readonly List<MallDoor> Doors = new();
            public readonly List<FenceChunk> Breakables = new(); public readonly List<string> Log = new();
            public readonly List<Vector3> Lamps = new();                    // the ceiling lights (the night's point lights go to the nearest)
            public readonly List<AmbientLife.Group> Shoppers = new();
            public MeshBuilder Ceil;
            public const float LotCell = 4f; public bool[,] LotOn; public float LotU0, LotV0; public int LotCells;   // the parking round it (MapBuilder.MallLots.cs)
            public bool InLot(float x, float z) { if (LotOn == null) return false; var q = UV(new Vector2(x, z)); int i = Mathf.FloorToInt((q.x - LotU0) / LotCell), j = Mathf.FloorToInt((q.y - LotV0) / LotCell); return i >= 0 && j >= 0 && i < LotOn.GetLength(0) && j < LotOn.GetLength(1) && LotOn[i, j]; }   // the ceilings (drawn unlit, cream: the photos' bright ceilings)
            public Vector2 W(float u, float v) => U * u + V * v;
            public Vector2 UV(Vector2 p) => new Vector2(Vector2.Dot(p, U), Vector2.Dot(p, V));
            public Vector3 P(float u, float v, float y) { var w = W(u, v); return new Vector3(w.x, y, w.y); }
            public Vector3 D3(float du, float dv) { var d = (U * du + V * dv).normalized; return new Vector3(d.x, 0f, d.y); }
            public bool Owns(int i) => Owned.Contains(i);
            public bool InSpine(float u, float v) => InPolygon(W(u, v), SpineRing);
            public float Level(int level) => level == 0 ? L0 : L1;
            public float Ceiling(int level) => level == 0 ? L1 - Slab : Roof - Slab;
            public bool InVoid(float u, float v, float pad = 0f) { foreach (var r in Voids) if (u > r.xMin - pad && u < r.xMax + pad && v > r.yMin - pad && v < r.yMax + pad) return true; return false; }
        }
        static MallPlan Mall;

        // the layout in the frame (metres; from the footprints as Round 59 placed them, checked against them in PlanMall)
        const float MallFrameDeg = 14.75f;
        const float MallUW = -239f, MallUE = 53f, MallCS = -1315f, MallCN = -1292f;   // the concourse: the west and east anchors' walls, its two sides
        static readonly Rect MallCourtArea = Rect.MinMaxRect(-141f, -1325f, -70f, -1292f);     // the center court (to the south anchor's front)
        static readonly Rect MallFoodCourt = Rect.MinMaxRect(-141f, -1292f, -68f, -1242.4f);   // over it on the upper level: the food court
        static readonly Rect MallNorthWing = Rect.MinMaxRect(-7f, -1292f, 13f, -1202.6f);      // its corridor (to the north anchor)
        static readonly Rect MallNWWing = Rect.MinMaxRect(-203f, -1292f, -187f, -1217.4f);     // its corridor (to the closed north-west anchor)

        static float MallRampRun(List<Vector2> prof) => prof[prof.Count - 1].x;
        static List<Vector2> MallRampProfile(float rise)
        {
            const float r = 22f, ang = 14f;
            var p = new SProf(); float arcRise = 2f * r * (1f - Mathf.Cos(ang * Mathf.Deg2Rad));
            p.Arc(r, ang).Line((rise - arcRise) / Mathf.Sin(ang * Mathf.Deg2Rad)).Arc(r, -ang);
            var pts = p.pts.ToList(); pts[pts.Count - 1] = new Vector2(pts[pts.Count - 1].x, rise); return pts;
        }

        // ------------------------------------------------------------ the planning: which buildings, the frame, the doors, the ground at the doors
        static MallPlan PlanMall(MapData map, TerrainGrid t)
        {
            var ids = Enumerable.Range(0, map.buildings.Length).Where(i => map.buildings[i].type == "mall").ToList();
            if (ids.Count < 6) { Debug.LogWarning($"[Round60] Town Center: {ids.Count} mall buildings in map.json (6 expected): built as before"); return null; }
            var m = new MallPlan();
            foreach (var i in ids) { var r = ToPoints(map.buildings[i].footprint); if (PolyArea(r) < 0f) r.Reverse(); m.Rings[i] = r; }
            m.Spine = ids.OrderByDescending(i => Mathf.Abs(PolyArea(m.Rings[i]))).First(); m.SpineRing = m.Rings[m.Spine];
            // the frame: 14.75 degrees north of east, the one the layout's numbers below were measured in (the footprints' walls are square to it
            // within 0.2 m; the single longest wall is 14.67 degrees, which at 1.3 km from the origin moved the layout up to 1.7 m)
            var sr = m.SpineRing; var U = new Vector2(Mathf.Cos(MallFrameDeg * Mathf.Deg2Rad), Mathf.Sin(MallFrameDeg * Mathf.Deg2Rad)); m.U = U; m.V = new Vector2(-U.y, U.x);
            var uv = sr.Select(m.UV).ToList();
            float u0 = uv.Min(q => q.x), u1 = uv.Max(q => q.x), v0 = uv.Min(q => q.y), v1 = uv.Max(q => q.y);
            if (Mathf.Abs(u0 - MallUW) > 2f || Mathf.Abs(u1 - MallUE) > 2f || Mathf.Abs(v0 + 1340.7f) > 2f || Mathf.Abs(v1 + 1202.6f) > 2f)
            { Debug.LogWarning($"[Round60] Town Center: the concourse building's outline moved (u {u0:0.0} to {u1:0.0}, v {v0:0.0} to {v1:0.0}): built as before"); return null; }
            foreach (var i in ids)
            {
                if (i == m.Spine) continue;
                var c = m.UV(m.Rings[i].Aggregate(Vector2.zero, (s, q) => s + q) / m.Rings[i].Count);
                if (c.x > MallUE) m.East = i; else if (c.x < MallUW) m.West = i; else if (c.y < -1340f) m.South = i; else if (c.x > -60f) m.North = i; else m.NorthWest = i;
            }
            if (new[] { m.East, m.West, m.South, m.North, m.NorthWest }.Any(i => i < 0)) { Debug.LogWarning("[Round60] Town Center: an anchor was not found: built as before"); return null; }
            foreach (var i in ids) m.Owned.Add(i);
            // the levels from the ground at the entrances (the lower ones' lots, the upper ones')
            float G(float u, float v) { var w = m.W(u, v); return t.Height(w.x, w.y); }
            float lower = new[] { G(-51f, -1258f), G(-195f, -1350f) }.Average(), upper = new[] { G(10f, -1350f), G(175f, -1303f), G(-233f, -1242f) }.Average();
            m.L0 = Mathf.Round(lower * 10f) / 10f; m.L1 = m.L0 + Mathf.Clamp(Mathf.Round((upper - lower) * 10f) / 10f, 6.5f, 8.5f);
            m.Roof = m.L1 + 7.2f; m.AnchorTop = m.L0 + 17.3f;
            // the ramps (the escalator banks), each in its own hole in the upper floor: the west one rises west (to the north-west wing), the east one east
            // (to the east anchor's upper floor)
            var prof = MallRampProfile(m.L1 - m.L0); float run = MallRampRun(prof);
            m.Ramps.Add(new MallRamp { name = "west", uFoot = -150f, dir = -1, v0 = -1307.5f, v1 = -1299.5f, prof = prof, run = run });
            m.Ramps.Add(new MallRamp { name = "east", uFoot = 8f, dir = 1, v0 = -1307.5f, v1 = -1299.5f, prof = prof, run = run });
            foreach (var r in m.Ramps) m.Voids.Add(Rect.MinMaxRect(r.U0, r.v0, r.U1, r.v1));
            m.Court = Rect.MinMaxRect(-126f, -1311f, -84f, -1296f); m.Voids.Add(m.Court);      // the center court, open to both levels
            m.Tower = Rect.MinMaxRect(-57f, -1286f, -45f, -1267.1f - MallPlan.WallT); m.Voids.Add(m.Tower);   // the main entrance's two-storey hall
            // the doors: on the footprints' lines (the wall's outer line), their floors, which way is out
            MallDoor Door(string name, int level, Vector2 a, Vector2 b, Vector2 nOut, float h, bool main = false, bool anchor = false)
                => new MallDoor { name = name, level = level, a = m.W(a.x, a.y), b = m.W(b.x, b.y), nOut = (m.U * nOut.x + m.V * nOut.y).normalized, floor = m.Level(level), height = h, main = main, anchor = anchor };
            m.Doors.Add(Door("main (north)", 0, new Vector2(-57f, -1267.1f), new Vector2(-45f, -1267.1f), new Vector2(0, 1), 3.4f, main: true));
            m.Doors.Add(Door("south", 1, new Vector2(4f, -1339.5f), new Vector2(16f, -1339.5f), new Vector2(0, -1), 3.4f));
            m.Doors.Add(Door("north-west wing (west)", 1, new Vector2(-224f, -1248f), new Vector2(-224f, -1236f), new Vector2(-1, 0), 3.4f));
            m.Doors.Add(Door("south-west", 0, new Vector2(-201f, -1340.6f), new Vector2(-189f, -1340.6f), new Vector2(0, -1), 3.4f));
            m.Doors.Add(Door("east anchor (east)", 1, new Vector2(163.5f, -1309f), new Vector2(163.5f, -1297f), new Vector2(1, 0), 3.4f, anchor: true));
            // the ground at each door: its apron graded to the floor (flat for 6 m out, easing back over 10 m more, 4 m either side)
            foreach (var d in m.Doors)
            {
                var along = (d.b - d.a).normalized; float half = d.Width * .5f + 3f; int n = 0; float most = 0f;
                var c = d.Mid; var lo = Vector2.Min(Vector2.Min(c - along * (half + 4f), c + along * (half + 4f)), c + d.nOut * 17f) - Vector2.one; var hi = Vector2.Max(Vector2.Max(c - along * (half + 4f), c + along * (half + 4f)), c + d.nOut * 17f) + Vector2.one;
                EditTerrain(t, Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y), (x, z, y) =>
                {
                    var q = new Vector2(x, z) - c; float out_ = Vector2.Dot(q, d.nOut), side = Mathf.Abs(Vector2.Dot(q, along));
                    if (out_ < .2f || out_ > 16f || side > half + 4f) return y;
                    float w = Smooth01(1f - (out_ - 6f) / 10f) * Smooth01(1f - (side - half) / 4f);
                    float ny = y + w * (d.floor - .02f - y); if (Mathf.Abs(ny - y) > 1e-3f) { n++; most = Mathf.Max(most, Mathf.Abs(ny - y)); }
                    return ny;
                });
                m.Log.Add($"door {d.name}: {(d.level == 0 ? "lower" : "upper")} floor {d.floor:0.00} m, its apron graded ({n} samples, up to {most:0.00} m)");
            }
            PlanMallLots(m, map);   // the parking round it (MapBuilder.MallLots.cs)
            m.Log.Insert(0, $"the frame {Mathf.Atan2(U.y, U.x) * Mathf.Rad2Deg:0.00} deg; floors {m.L0:0.00} and {m.L1:0.00} m (the ground: lower lots {lower:0.00}, upper {upper:0.00}), roof {m.Roof:0.00}; anchors: east {m.East}, west {m.West}, south {m.South}, north {m.North}, north-west {m.NorthWest} (closed)");
            return m;
        }

        // ------------------------------------------------------------ colours (from the photos)
        static readonly Color MBlock = SC(.79f, .72f, .61f), MBlockPink = SC(.82f, .68f, .58f), MCoping = SC(.29f, .26f, .23f), MBronze = SC(.27f, .24f, .21f),
            MCream = SC(.93f, .90f, .82f), MCreamDark = SC(.85f, .81f, .72f), MMahogany = SC(.47f, .23f, .14f), MTile = SC(.88f, .83f, .72f), MTileRed = SC(.68f, .38f, .27f),
            MDiamond = SC(.16f, .18f, .19f), MCheckDark = SC(.10f, .19f, .15f), MCheckLight = SC(.92f, .92f, .89f), MGlass = SC(.50f, .60f, .65f), MGranite = SC(.56f, .56f, .57f),
            MStone = SC(.62f, .63f, .64f), MRoof = SC(.46f, .46f, .47f), MCarpet = SC(.30f, .27f, .30f), MSteel = SC(.70f, .71f, .73f), MPlant = SC(.20f, .45f, .22f);

        // ------------------------------------------------------------ the step: everything (the outside here, the inside in MapBuilder.MallInside.cs)
        static void BuildMall(MapData map, TerrainGrid t, Materials mats, Transform parent, MallPlan m)
        {
            var root = new GameObject("Town Center (Round 60)").transform; root.SetParent(parent, false);
            var outside = new GameObject("Mall outside").transform; outside.SetParent(root, false);
            var inside = new GameObject("Mall inside").transform; inside.SetParent(root, false);
            m.Breakables.Clear(); m.Lamps.Clear(); m.Shoppers.Clear();
            var signs = MallSignMaterial();
            // the ground under the concourse building and the east anchor (their floors are slabs)
            var east = m.Rings[m.East];
            var all = m.SpineRing.Concat(east).ToList();
            var bb = Rect.MinMaxRect(all.Min(q => q.x) - 1, all.Min(q => q.y) - 1, all.Max(q => q.x) + 1, all.Max(q => q.y) + 1);
            int holed = SetHoles(t, bb, q => (InPolygon(q, m.SpineRing) && RingDist(q, m.SpineRing) > .35f) || (InPolygon(q, east) && RingDist(q, east) > .35f));
            var shell = new MeshBuilder(); var shellIn = new MeshBuilder(); var deco = new MeshBuilder(); var glassOut = new MeshBuilder(); m.Ceil = new MeshBuilder();
            MallShell(m, t, outside, inside, shell, shellIn, signs, glassOut);
            MallAnchors(m, t, outside, shell, signs, mats);
            MallEntrances(m, t, outside, inside, shell, glassOut, signs);
            MallLots(m, map, t, mats, outside);
            MallSkylights(m, outside, mats);
            shell.ToObject("MallShell", mats.House, outside, false, true);
            glassOut.ToObject("MallGlassOutside", mats.House, outside, false, false);
            BuildMallInside(map, t, mats, inside, m, signs, shellIn);   // MapBuilder.MallInside.cs
            shellIn.ToObject("MallShellInside", mats.House, inside, false, false);
            var cm = AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir + "/MallCeiling.mat");
            if (cm == null || QuickStep != null) { var nm = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "MallCeiling" }; nm.SetColor("_BaseColor", new Color(.90f, .88f, .82f)); cm = GenAsset(nm, GeneratedDir + "/MallCeiling.mat"); }
            m.Ceil.ToObject("MallCeilings", cm, inside, false, false);
            MallComponent(m, root, inside);
            m.Log.Add($"{holed} ground cells holed under the floors; {m.Breakables.Count} breakable sets; {m.Lamps.Count} ceiling lights; {m.Shoppers.Count} shopper groups");
            Debug.Log("[Round60] Town Center: " + string.Join("; ", m.Log));
        }

        // the run-time half (Assets/Scripts/World/TownCenterMall.cs): the building's box, the inside's renderers, its breakables, its lights
        static void MallComponent(MallPlan m, Transform root, Transform inside)
        {
            var c = root.gameObject.AddComponent<TownCenterMall>();
            var uv = m.Rings.Values.SelectMany(r => r).Select(m.UV).ToList();
            float u0 = uv.Min(q => q.x), u1 = uv.Max(q => q.x), v0 = uv.Min(q => q.y), v1 = uv.Max(q => q.y);
            c.outline = new[] { m.W(u0, v0), m.W(u1, v0), m.W(u1, v1), m.W(u0, v1) };
            c.inside = inside.GetComponentsInChildren<Renderer>(true);
            c.breakables = m.Breakables.Where(b => b).Distinct().ToArray();
            c.lamps = m.Lamps.ToArray(); c.lowerFloor = m.L0; c.upperFloor = m.L1; c.roof = m.Roof; c.frameU = m.U; c.frameV = m.V;
        }

        // ------------------------------------------------------------ walls
        // a wall from a to b on a footprint's line, WallT inward (n: outward), y0 to y1; its outside banded as split-face block (a darker course every
        // 1.2 m, the panels' tints varied) or plain; its inside cream; a box collider
        static void MallWall(MeshBuilder outMb, MeshBuilder inMb, Transform colRoot, Vector2 a, Vector2 b, Vector2 n, float y0, float y1, Color outside, bool banded, Color inside, string name = "Mall wall")
        {
            if (y1 - y0 < .02f) return; float len = Vector2.Distance(a, b); if (len < .02f) return;
            var nn = new Vector3(n.x, 0, n.y); Vector3 A(Vector2 q, float y, float inward) => new Vector3(q.x - n.x * inward, y, q.y - n.y * inward);
            var along = new Vector3(b.x - a.x, 0, b.y - a.y) / len;
            if (banded)
            {
                // panels of about 6 m, courses of 1.2 m (each course: 1.1 m of block and a 0.1 m darker joint)
                int panels = Mathf.Max(1, Mathf.RoundToInt(len / 6f));
                for (int p = 0; p < panels; p++)
                {
                    var pa = Vector2.Lerp(a, b, p / (float)panels); var pb = Vector2.Lerp(a, b, (p + 1) / (float)panels); var tint = SVary(outside, pa.x * .37f, pa.y * .53f, .035f);
                    for (float y = Mathf.Floor(y0 / 1.2f) * 1.2f; y < y1; y += 1.2f)
                    {
                        float ya = Mathf.Max(y0, y), yb = Mathf.Min(y1, y + 1.1f), yc = Mathf.Min(y1, y + 1.2f);
                        if (yb > ya) { outMb.Tint = tint; Face(outMb, A(pa, ya, 0), A(pb, ya, 0), A(pb, yb, 0), A(pa, yb, 0), nn); }
                        if (yc > yb && yb >= ya) { outMb.Tint = tint * .86f; Face(outMb, A(pa, yb, 0), A(pb, yb, 0), A(pb, yc, 0), A(pa, yc, 0), nn); }
                    }
                }
            }
            else { outMb.Tint = outside; Face(outMb, A(a, y0, 0), A(b, y0, 0), A(b, y1, 0), A(a, y1, 0), nn); }
            inMb.Tint = inside; Face(inMb, A(a, y0, WallT), A(b, y0, WallT), A(b, y1, WallT), A(a, y1, WallT), -nn);
            outMb.Tint = MCoping; Face(outMb, A(a, y1, -.05f), A(b, y1, -.05f), A(b, y1, WallT), A(a, y1, WallT), Vector3.up);
            outMb.Tint = outside * .95f; Face(outMb, A(a, y0, 0), A(a, y0, WallT), A(a, y1, WallT), A(a, y1, 0), -along); Face(outMb, A(b, y0, 0), A(b, y0, WallT), A(b, y1, WallT), A(b, y1, 0), along);
            outMb.Tint = outside * .9f; Face(outMb, A(a, y0, 0), A(b, y0, 0), A(b, y0, WallT), A(a, y0, WallT), Vector3.down);
            var mid = (a + b) * .5f - n * (WallT * .5f);
            var g = new GameObject(name); g.transform.SetParent(colRoot, false);
            g.transform.SetPositionAndRotation(new Vector3(mid.x, (y0 + y1) * .5f, mid.y), Quaternion.LookRotation(along));
            g.AddComponent<BoxCollider>().size = new Vector3(WallT, y1 - y0, len);
        }
        const float WallTM = MallPlan.WallT;

        // a ring's walls with openings: (a, b on the ring's line, the opening's bottom and top); edges skipped where 'skip' says (shared with
        // another building); 'outside' colour per edge
        static void RingWalls(MallPlan m, List<Vector2> ring, MeshBuilder outMb, MeshBuilder inMb, Transform colRoot, float baseY, float topY,
            List<(Vector2 a, Vector2 b, float y0, float y1)> cuts, Func<Vector2, Vector2, bool> skip, Func<Vector2, Vector2, (Color c, bool banded)> look, Color inside)
        {
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count]; float len = Vector2.Distance(a, b); if (len < .05f) continue;
                if (skip != null && skip(a, b)) continue;
                var dd = (b - a) / len; var n = new Vector2(dd.y, -dd.x); if (InPolygon((a + b) * .5f + n * .2f, ring)) n = -n;
                float S(Vector2 q) => Vector2.Dot(q - a, dd);
                bool OnEdge(Vector2 q) => Mathf.Abs(Vector2.Dot(q - a, n)) < .8f && S(q) > -.5f && S(q) < len + .5f;
                var here = cuts.Where(c => OnEdge(c.a) && OnEdge(c.b)).Select(c => (s0: Mathf.Clamp(Mathf.Min(S(c.a), S(c.b)), 0, len), s1: Mathf.Clamp(Mathf.Max(S(c.a), S(c.b)), 0, len), c.y0, c.y1)).ToList();
                var marks = new SortedSet<float> { 0f, len }; foreach (var c in here) { marks.Add(c.s0); marks.Add(c.s1); }
                var mk = marks.ToList(); var (col, banded) = look(a, b);
                for (int j = 0; j + 1 < mk.Count; j++)
                {
                    float s0 = mk[j], s1 = mk[j + 1]; if (s1 - s0 < .02f) continue; float sm = (s0 + s1) * .5f;
                    var open = here.Where(c => sm > c.s0 && sm < c.s1).ToList();
                    var spans = new List<(float, float)>(); float y = baseY;
                    foreach (var o in open.OrderBy(o => o.y0)) { if (o.y0 > y) spans.Add((y, o.y0)); y = Mathf.Max(y, o.y1); }
                    if (topY > y) spans.Add((y, topY));
                    foreach (var (y0, y1) in spans) MallWall(outMb, inMb, colRoot, a + dd * s0, a + dd * s1, n, y0, y1, col, banded, inside);
                }
            }
        }

        static bool OnRing(Vector2 q, List<Vector2> ring, float tol = .6f) => RingDist(q, ring) < tol;

        // the concourse building's and the east anchor's walls (both hollow), the roof, the anchor's floor and roof
        static void MallShell(MallPlan m, TerrainGrid t, Transform outside, Transform inside, MeshBuilder shell, MeshBuilder shellIn, Material signs, MeshBuilder roofTop)
        {
            var east = m.Rings[m.East]; var ring = m.SpineRing;
            float baseY = Mathf.Min(m.L0, ring.Concat(east).Min(q => Ground(t, q.x, q.y))) - .8f;
            var cuts = new List<(Vector2 a, Vector2 b, float y0, float y1)>();
            foreach (var d in m.Doors) cuts.Add((d.a, d.b, d.floor - .02f, d.main ? m.L1 + 4.6f : d.floor + d.height));
            // between the concourse and the east anchor: the upper floor open across the concourse's width (breakable glass: MallInside)
            cuts.Add((m.W(MallUE, MallCS), m.W(MallUE, MallCN), m.L1 - .02f, m.L1 + 4.2f));
            (Color, bool) Look(Vector2 a, Vector2 b)
            {
                var mid = (a + b) * .5f;
                if (OnRing(mid, east)) return (MCream, false);                                   // inside the east anchor's store
                return (MBlock, true);
            }
            // the concourse building: its walls to the parapet; hidden faces inside the solid anchors are skipped (their walls stand there)
            RingWalls(m, ring, shell, shellIn, outside, baseY, m.Roof + MallPlan.Parapet, cuts, null, Look, MCream);
            // the east anchor (Halverson's): its walls except the one it shares with the concourse
            float eTop = m.AnchorTop;
            RingWalls(m, east, shell, shellIn, outside, baseY, eTop, cuts, (a, b) => OnRing(a, ring, 1f) && OnRing(b, ring, 1f), (a, b) => (MBlockPink, true), MCream);
            // the roofs: the concourse building's (holes for the skylights), the east anchor's; flat slabs, dark gray on top, cream underneath; colliders
            var roofRects = MallRaster(m, ring, .5f, (u, v) => !MallSkylightArea(u, v));
            foreach (var r in roofRects) MallSlab(m, roofTop, m.Ceil, outside, r, m.Roof, MallPlan.Slab, MRoof, MCream, "Mall roof", false);   // its top casts no shadow: the inside lit as a lit mall
            var eRects = MallRasterRing(m, east, 1f, (u, v) => true);
            foreach (var r in eRects) MallSlab(m, roofTop, m.Ceil, outside, r, m.L1 + 7.6f, MallPlan.Slab, MRoof, MCream, "Mall roof", false);
            // rooftop units on the concourse's roof (the photos' boxes): a few, off the skylights
            var rnd = new System.Random(60);
            for (int k = 0; k < 18; k++)
            {
                float u = Mathf.Lerp(MallUW + 8f, MallUE - 8f, (float)rnd.NextDouble()), v = (float)rnd.NextDouble() < .5f ? -1330f : -1278f;
                if (MallSkylightArea(u, v)) continue; var at = m.P(u, v, m.Roof);
                SBox(shell, at, m.D3(1, 0), new Vector3(3.2f, 1.6f, 4.5f), SC(.62f, .62f, .60f), SC(.55f, .55f, .54f));
            }
            m.Log.Add($"walls from {baseY:0.0} m; roof slabs {roofRects.Count}, the east anchor's {eRects.Count}");
        }

        // where the skylights are (the roof is open there): the concourse's barrel vault, the court and food court's pyramid, the wings' vaults
        static bool MallSkylightArea(float u, float v) =>
            (u > MallUW + 6f && u < MallUE - 6f && v > -1308.5f && v < -1298.5f) || (u > -130f && u < -80f && v > -1315f && v < -1262f) ||
            (u > -5f && u < 11f && v > -1288f && v < -1210f) || (u > -201f && u < -189f && v > -1288f && v < -1222f);

        // cells of 'cell' m in the frame inside the concourse building (0.15 m or more inside its line) and kept by 'keep', merged into rectangles
        static List<Rect> MallRaster(MallPlan m, List<Vector2> ring, float cell, Func<float, float, bool> keep) => MallRasterRing(m, ring, cell, keep);
        static List<Rect> MallRasterRing(MallPlan m, List<Vector2> ring, float cell, Func<float, float, bool> keep)
        {
            var uv = ring.Select(m.UV).ToList(); float u0 = uv.Min(q => q.x), u1 = uv.Max(q => q.x), v0 = uv.Min(q => q.y), v1 = uv.Max(q => q.y);
            int nu = Mathf.CeilToInt((u1 - u0) / cell), nv = Mathf.CeilToInt((v1 - v0) / cell);
            var on = new bool[nu, nv];
            for (int i = 0; i < nu; i++) for (int j = 0; j < nv; j++) { float u = u0 + (i + .5f) * cell, v = v0 + (j + .5f) * cell; var w = m.W(u, v); on[i, j] = InPolygon(w, ring) && RingDist(w, ring) > .15f && keep(u, v); }
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
        // a slab over a rectangle of the frame: its top, its underside (unless 'top only'), a box collider named 'name'
        static void MallSlab(MallPlan m, MeshBuilder topMb, MeshBuilder underMb, Transform root, Rect r, float topY, float thick, Color top, Color under, string name, bool topOnly)
        {
            Vector3 P(float u, float v, float y) => m.P(u, v, y);
            if (top.a > 0f) { topMb.Tint = top; Face(topMb, P(r.xMin, r.yMin, topY), P(r.xMax, r.yMin, topY), P(r.xMax, r.yMax, topY), P(r.xMin, r.yMax, topY), Vector3.up); }
            if (!topOnly) { underMb.Tint = under; Face(underMb, P(r.xMin, r.yMin, topY - thick), P(r.xMax, r.yMin, topY - thick), P(r.xMax, r.yMax, topY - thick), P(r.xMin, r.yMax, topY - thick), Vector3.down); }
            var c = m.W(r.center.x, r.center.y); var g = new GameObject(name); g.transform.SetParent(root, false);
            g.transform.SetPositionAndRotation(new Vector3(c.x, topY - thick * .5f, c.y), Quaternion.LookRotation(m.D3(0, 1)));
            g.AddComponent<BoxCollider>().size = new Vector3(r.width, thick, r.height);   // local x = +u (the frame's right of +v), z = +v
        }

        // ------------------------------------------------------------ the anchors (solid; their portals and names), the closed one
        static void MallAnchors(MallPlan m, TerrainGrid t, Transform outside, MeshBuilder shell, Material signs, Materials mats)
        {
            var bodies = new MeshBuilder();
            foreach (var (id, row, col, facing, closed) in new[] { (m.West, SignBelmont, MBlock, new Vector2(-1, 0), false), (m.South, SignCarroway, MBlock, new Vector2(0, -1), false),
                (m.North, SignHalversonHome, MBlockPink, new Vector2(0, 1), false), (m.NorthWest, SignSterling, MBlock, new Vector2(0, 1), true) })
            {
                var ring = m.Rings[id]; float baseY = ring.Min(q => Ground(t, q.x, q.y)) - .8f, top = closed ? m.AnchorTop - 1.5f : m.AnchorTop;
                Extrude(bodies, ring, baseY, top, col, MRoof, true);
                // block courses: a darker joint every 1.2 m, 2 cm proud, on the faces that are not the concourse's
                for (int i = 0; i < ring.Count; i++)
                {
                    Vector2 a = ring[i], b = ring[(i + 1) % ring.Count]; if (Vector2.Distance(a, b) < 2f || OnRing((a + b) * .5f, m.SpineRing)) continue;
                    var dd = (b - a).normalized; var n = new Vector2(dd.y, -dd.x); if (InPolygon((a + b) * .5f + n * .2f, ring)) n = -n; var nn = new Vector3(n.x, 0, n.y);
                    for (float y = Mathf.Ceil(baseY / 1.2f) * 1.2f; y < top - .2f; y += 1.2f)
                    { shell.Tint = col * .86f; var o = nn * .02f; Face(shell, new Vector3(a.x, y, a.y) + o, new Vector3(b.x, y, b.y) + o, new Vector3(b.x, y + .1f, b.y) + o, new Vector3(a.x, y + .1f, a.y) + o, nn); }
                    shell.Tint = MCoping; Face(shell, new Vector3(a.x, top, a.y) + nn * .05f, new Vector3(b.x, top, b.y) + nn * .05f, new Vector3(b.x, top - .35f, b.y) + nn * .05f, new Vector3(a.x, top - .35f, a.y) + nn * .05f, nn);
                }
                // the portal on its main face: a raised cream frame up past the parapet, an arch-like stepped head, the name, glass doors at its foot
                var uv = ring.Select(m.UV).ToList(); var fu = facing.x; var fv = facing.y;
                float cu, cv, half = 9f;
                if (fu != 0) { cu = fu > 0 ? uv.Max(q => q.x) : uv.Min(q => q.x); cv = (uv.Min(q => q.y) + uv.Max(q => q.y)) * .5f; }
                else { cv = fv > 0 ? uv.Max(q => q.y) : uv.Min(q => q.y); cu = (uv.Min(q => q.x) + uv.Max(q => q.x)) * .5f; }
                var c = m.W(cu, cv); var nOut = (m.U * fu + m.V * fv).normalized; var floor = Ground(t, (c + nOut * 3f).x, (c + nOut * 3f).y);
                var f = MakeFrame(new Vector3(c.x, floor, c.y), nOut, 1f);
                float h = top - floor;
                BoxF(shell, f, -half, half, -.5f, h + 2.2f, -.05f, .9f, closed ? MCreamDark * .92f : MCream);   // the frame
                BoxF(shell, f, -half + 1.6f, half - 1.6f, -.5f, h - 3.2f, .9f, 1.0f, closed ? SC(.58f, .54f, .48f) : SC(.74f, .66f, .56f));   // the recessed panel
                BoxF(shell, f, -half - .4f, half + .4f, h + 2.2f, h + 2.6f, -.1f, 1.1f, MCoping);
                for (int s = 0; s < 3; s++) BoxF(shell, f, -half + 1.5f + s * 1.8f, half - 1.5f - s * 1.8f, h + 2.6f + s * .7f, h + 3.3f + s * .7f, .2f, .9f, MCream);   // the stepped head
                float sw = 13f, shh = sw * .078f;
                if (signs != null) MallPlate(outside, f.P(0f, h - 5.0f, 1.02f), -f.F, sw, shh, row, signs);
                if (!closed) { BoxF(shell, f, -4.5f, 4.5f, 0f, 3.4f, 1.0f, 1.08f, MGlass); for (float x = -4.5f; x <= 4.51f; x += 1.5f) BoxF(shell, f, x - .06f, x + .06f, 0f, 3.4f, 1.0f, 1.12f, MBronze); BoxF(shell, f, -6f, 6f, 3.6f, 3.9f, .9f, 4.2f, SC(.90f, .90f, .88f)); }
                else
                {
                    BoxF(shell, f, -4.5f, 4.5f, 0f, 3.4f, 1.0f, 1.08f, SPlyDark);   // boarded
                    if (signs != null) MallPlate(outside, f.P(0f, 5.2f, 1.04f), -f.F, 9f, 9f * .078f, SignForLease, signs);
                }
            }
            bodies.ToObject("MallAnchors", mats.House, outside, true, true);
            // the east anchor's portal over its east door (its walls and floors: MallShell, its inside: MallInside)
            {
                var d = m.Doors.First(x => x.anchor); var f = MakeFrame(new Vector3(d.Mid.x, d.floor, d.Mid.y), d.nOut, 1f); float h = m.AnchorTop - d.floor;
                BoxF(shell, f, -10f, 10f, -.3f, h + 2.4f, -.05f, .9f, MCream);
                BoxF(shell, f, -10.4f, 10.4f, h + 2.4f, h + 2.8f, -.1f, 1.1f, MCoping);
                for (int s = 0; s < 3; s++) BoxF(shell, f, -8.5f + s * 1.8f, 8.5f - s * 1.8f, h + 2.8f + s * .7f, h + 3.5f + s * .7f, .2f, .9f, MCream);
                if (signs != null) MallPlate(outside, f.P(0f, h - 3.4f, 1.02f), -f.F, 15f, 15f * .078f, SignHalverson, signs);
                // the same name on the west anchor's other faces would be too much; one more on the east anchor's south face (seen from the south lot)
            }
        }

        // ------------------------------------------------------------ the entrances (outside): doors' frames, the canopies, the main entrance's tower
        static void MallEntrances(MallPlan m, TerrainGrid t, Transform outside, Transform inside, MeshBuilder shell, MeshBuilder glass, Material signs)
        {
            foreach (var d in m.Doors)
            {
                var f = MakeFrame(new Vector3(d.Mid.x, d.floor, d.Mid.y), d.nOut, 1f); float hw = d.Width * .5f;
                if (d.main)
                {
                    // the glass tower: a bronze-framed glass front from the doors up past the roof, the stepped block parapet round it, the name
                    float top = m.Roof + 4.2f - d.floor;
                    BoxF(shell, f, -hw - 3f, -hw, -.3f, top, -.05f, 1.6f, MBlock); BoxF(shell, f, hw, hw + 3f, -.3f, top, -.05f, 1.6f, MBlock);   // the piers
                    for (float y = 3.6f; y < top - .5f; y += 2.6f) { glass.Tint = SVary(MGlass, y, 3f, .05f); Face(glass, f.P(-hw, y, 1.2f), f.P(hw, y, 1.2f), f.P(hw, Mathf.Min(top - .5f, y + 2.6f), 1.2f), f.P(-hw, Mathf.Min(top - .5f, y + 2.6f), 1.2f), f.F); }
                    for (float x = -hw; x <= hw + .01f; x += hw / 2.5f) BoxF(shell, f, x - .09f, x + .09f, 3.4f, top - .5f, 1.15f, 1.35f, MBronze);
                    for (float y = 3.4f; y < top; y += 2.6f) BoxF(shell, f, -hw, hw, y, y + .18f, 1.15f, 1.35f, MBronze);
                    BoxF(shell, f, -hw - 3.4f, hw + 3.4f, top - .5f, top + .3f, -.1f, 2.4f, MBronze);   // its cap, overhanging
                    BoxF(shell, f, -hw - 6f, hw + 6f, -.3f, m.Roof + 2.2f - d.floor, -.05f, .5f, MBlock);   // the stepped parapet behind it
                    BoxF(shell, f, -hw - 9f, hw + 9f, -.3f, m.Roof + 1.6f - d.floor, -.05f, .35f, MBlock);
                    BoxF(shell, f, -hw - 1f, hw + 1f, 3.4f, 3.7f, 1.2f, 4.0f, MBronze);   // the door canopy
                    if (signs != null) MallPlate(outside, f.P(0f, m.Roof + 2.2f - d.floor - 1.4f, .52f), -f.F, 11f, 11f * .078f, SignTownCenter, signs);
                    // planters by the doors, two lamp posts
                    foreach (float x in new[] { -hw - 1.5f, hw + 1.5f }) { SCyl(shell, f.P(x, 0f, 3f), f.P(x, .9f, 3f), .7f, SC(.80f, .78f, .74f), 10); SBox(shell, f.P(x, .9f, 3f), f.F, new Vector3(1f, .5f, 1f), SC(.70f, .18f, .20f)); }
                }
                else
                {
                    // the curved canopy on two stone piers, gray granite panels either side, the name on a black board each side
                    float reach = 6f, ch = 4.4f;
                    foreach (float x in new[] { -hw - .4f, hw + .4f })
                    {
                        BoxF(shell, f, x - .55f, x + .55f, -.3f, ch, reach - .9f, reach + .2f, MStone);
                        var pier = f.P(x, ch * .5f - .15f, reach - .35f); var g = new GameObject("Mall canopy pier"); g.transform.SetParent(outside, false);
                        g.transform.SetPositionAndRotation(pier, Quaternion.LookRotation(f.F)); g.AddComponent<BoxCollider>().size = new Vector3(1.1f, ch + .3f, 1.1f);
                    }
                    BoxF(shell, f, -hw - 1f, hw + 1f, ch, ch + .5f, 0f, reach + .4f, SC(.40f, .38f, .35f));
                    for (int s = 0; s < 8; s++)   // the curved top
                    {
                        float a0 = s / 8f * Mathf.PI, a1 = (s + 1) / 8f * Mathf.PI; float r = hw + 1.4f;
                        var p0 = f.P(-Mathf.Cos(a0) * r, ch + .5f + Mathf.Sin(a0) * 1.4f, 0f); var p1 = f.P(-Mathf.Cos(a1) * r, ch + .5f + Mathf.Sin(a1) * 1.4f, 0f);
                        var cn = Vector3.up + f.R * (-Mathf.Cos((a0 + a1) * .5f)); shell.Tint = SC(.93f, .94f, .95f); Face(shell, p0, p1, p1 + f.F * (reach + .6f), p0 + f.F * (reach + .6f), cn); shell.Tint = SC(.82f, .83f, .84f); Face(shell, p0, p1, p1 + f.F * (reach + .6f), p0 + f.F * (reach + .6f), -cn);
                    }
                    foreach (float x in new[] { -hw - 6f, hw + 6f })
                    {
                        BoxF(shell, f, x - 4.5f, x + 4.5f, -.3f, 3.9f, -.05f, .25f, MGranite);
                        BoxF(shell, f, x - 2.6f, x + 2.6f, 1.5f, 2.9f, .25f, .3f, SC(.08f, .08f, .09f));
                        if (signs != null) MallPlate(outside, f.P(x, 2.2f, .31f), -f.F, 4.8f, 4.8f * .078f * 2.2f, SignTownCenterSmall, signs);
                    }
                    if (!d.anchor) { BoxF(shell, f, -hw - 6f, hw + 6f, d.height + .6f, d.height + 6.5f, -.05f, .3f, MBlock * .96f); if (signs != null) MallPlate(outside, f.P(0f, d.height + 5.3f, .32f), -f.F, 9f, 9f * .078f, SignTownCenter, signs); }
                }
                // the doors' frame (its glass is breakable: MallInside)
                BoxF(shell, f, -hw - .2f, -hw, 0f, d.height, -.1f, .35f, MBronze); BoxF(shell, f, hw, hw + .2f, 0f, d.height, -.1f, .35f, MBronze);
                BoxF(shell, f, -hw - .2f, hw + .2f, d.height, d.height + .25f, -.1f, .35f, MBronze);
            }
        }

        // ------------------------------------------------------------ the skylights: glazing (bright, unlit) on white ribs
        static void MallSkylights(MallPlan m, Transform outside, Materials mats)
        {
            var glaze = new MeshBuilder(); var ribs = new MeshBuilder(); float y0 = m.Roof - MallPlan.Slab;
            // a barrel vault along u (or v) over a rectangle: half-circle-ish of rise 'rise'
            void Vault(float a0, float a1, float b0, float b1, bool alongU, float rise)
            {
                int seg = 10; float span = b1 - b0;
                Vector3 Q(float a, float s) { float ang = s / seg * Mathf.PI; float b = b0 + span * (1f - Mathf.Cos(ang)) * .5f, y = y0 + Mathf.Sin(ang) * rise; return alongU ? m.P(a, b, y) : m.P(b, a, y); }
                for (float a = a0; a < a1 - .01f; a += 2.5f)
                {
                    float an = Mathf.Min(a1, a + 2.5f);
                    for (int s = 0; s < seg; s++)
                    {
                        glaze.Tint = Color.white; var p0 = Q(a, s); var p1 = Q(an, s); var p2 = Q(an, s + 1); var p3 = Q(a, s + 1);
                        Face(glaze, p0, p1, p2, p3, Vector3.down); Face(glaze, p0, p1, p2, p3, Vector3.up);
                    }
                    for (int s = 0; s < seg; s++) SRail(ribs, Q(a, s), Q(a, s + 1), .07f, SC(.95f, .95f, .94f));
                }
                // the ends: gables of glazing
                foreach (float a in new[] { a0, a1 })
                    for (int s = 0; s < seg; s++) { glaze.Tint = Color.white; var p = Q(a, s); var q = Q(a, s + 1); var bot = alongU ? m.P(a, b0 + span * .5f, y0) : m.P(b0 + span * .5f, a, y0); TriFace(glaze, p, q, bot, alongU ? m.D3(a == a0 ? -1 : 1, 0) : m.D3(0, a == a0 ? -1 : 1)); TriFace(glaze, p, q, bot, alongU ? m.D3(a == a0 ? 1 : -1, 0) : m.D3(0, a == a0 ? 1 : -1)); }
                // a curb round it on the roof
                foreach (float b in new[] { b0, b1 }) { var c0 = alongU ? m.P(a0, b, m.Roof) : m.P(b, a0, m.Roof); var c1 = alongU ? m.P(a1, b, m.Roof) : m.P(b, a1, m.Roof); SRail(ribs, c0 + Vector3.up * .2f, c1 + Vector3.up * .2f, .25f, SC(.88f, .88f, .86f)); }
            }
            Vault(MallUW + 6f, -130f, -1308.5f, -1298.5f, true, 3.6f);
            Vault(-80f, MallUE - 6f, -1308.5f, -1298.5f, true, 3.6f);
            Vault(-1288f, -1210f, -5f, 11f, false, 4.5f);
            Vault(-1288f, -1222f, -201f, -189f, false, 3.4f);
            // the court's and food court's pyramid, with a white lantern on top (the photos' cupola)
            {
                float u0 = -130f, u1 = -80f, v0 = -1315f, v1 = -1262f, rise = 7f; var apex = m.P((u0 + u1) * .5f, (v0 + v1) * .5f, y0 + rise);
                var c = new[] { m.P(u0, v0, y0), m.P(u1, v0, y0), m.P(u1, v1, y0), m.P(u0, v1, y0) };
                for (int i = 0; i < 4; i++)
                {
                    var a = c[i]; var b = c[(i + 1) % 4];
                    for (int k = 0; k < 6; k++)
                    {
                        float s0 = k / 6f, s1 = (k + 1) / 6f;
                        var p0 = Vector3.Lerp(a, apex, s0); var p1 = Vector3.Lerp(b, apex, s0); var p2 = Vector3.Lerp(b, apex, s1); var p3 = Vector3.Lerp(a, apex, s1);
                        var mid = (a + b) * .5f; var outw = (mid - apex); outw.y = 0; var nrm = (outw.normalized + Vector3.up * .8f).normalized;
                        glaze.Tint = Color.white; Face(glaze, p0, p1, p2, p3, nrm); Face(glaze, p0, p1, p2, p3, -nrm);
                        SRail(ribs, p0, p1, .07f, SC(.95f, .95f, .94f));
                    }
                    SRail(ribs, a, apex, .1f, SC(.95f, .95f, .94f));
                    for (int k = 1; k < 8; k++) SRail(ribs, Vector3.Lerp(a, b, k / 8f), apex, .05f, SC(.95f, .95f, .94f));
                }
                var top = apex; SBox(ribs, top - Vector3.up * .6f, m.D3(1, 0), new Vector3(3f, 2.2f, 3f), SC(.95f, .95f, .94f)); SCyl(ribs, top + Vector3.up * 1.6f, top + Vector3.up * 4.5f, .08f, SC(.9f, .9f, .9f), 6);
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir + "/MallSkylight.mat");
            if (mat == null || QuickStep != null)
            {
                var nm = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "MallSkylight" }; nm.SetColor("_BaseColor", new Color(.86f, .92f, .97f));
                mat = GenAsset(nm, GeneratedDir + "/MallSkylight.mat");
            }
            var g = glaze.ToObject("MallSkylights", mat, outside, false, false); g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            ribs.ToObject("MallSkylightRibs", mats.House, outside, false, false);
        }

        // ------------------------------------------------------------ the sign atlas (Tools/handmade/make_mall_signs.py; its row order)
        const int SignTownCenter = 0, SignHalverson = 1, SignHalversonHome = 2, SignBelmont = 3, SignCarroway = 4, SignSterling = 5, SignForLease = 6, SignFoodCourt = 7,
            SignDirections = 8, SignBanner1 = 9, SignBanner2 = 10, SignTownCenterSmall = 11, SignShopFirst = 12, SignShopCount = 30, SignFoodFirst = 42, SignFoodCount = 8, SignPlayArea = 50;
        const int MallSignRows = 51, MallSignRowPx = 160, MallSignH = 8192;
        static Material MallSignMaterial()
        {
            const string path = "Assets/HandMade/Mall/MallSigns.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("[Round60] " + path + " missing (Tools/handmade/make_mall_signs.py): blank boards"); return null; }
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "MallSigns" };   // lit, depth-tested, one-sided (the store signs' material)
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", .2f); m.SetFloat("_Cull", (float)CullMode.Back);
            m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", new Color(.35f, .35f, .35f));   // a little light of their own (lit boards)
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return GenAsset(m, GeneratedDir + "/MallSigns.mat");
        }
        // one row of the mall's sign atlas on a quad of w x h m, its front facing -facing (as AtlasPlate)
        static GameObject MallPlate(Transform parent, Vector3 centre, Vector3 facing, float w, float h, int row, Material mat)
        {
            var go = new GameObject("MallSign"); go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(facing, Vector3.up));
            float v1 = 1f - row * (float)MallSignRowPx / MallSignH, v0 = 1f - (row + 1) * (float)MallSignRowPx / MallSignH; float hw = w / 2f, hh = h / 2f;
            var mesh = new Mesh { name = "MallSign" };
            mesh.vertices = new[] { new Vector3(-hw, -hh, 0), new Vector3(hw, -hh, 0), new Vector3(hw, hh, 0), new Vector3(-hw, hh, 0) };
            mesh.uv = new[] { new Vector2(.004f, v0 + .001f), new Vector2(.996f, v0 + .001f), new Vector2(.996f, v1 - .001f), new Vector2(.004f, v1 - .001f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = GenAsset(mesh, $"{GeneratedDir}/Meshes/MallSign_{parent.childCount}_{row}_{Mathf.Abs(centre.x.GetHashCode() ^ (centre.z * 3.1f).GetHashCode() ^ centre.y.GetHashCode())}.asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
    }
}
