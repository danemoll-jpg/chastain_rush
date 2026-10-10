using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 7: builds the Chastain Glen area on top of the generated map: styled houses with concrete drives, curbs, sidewalks,
// mailboxes, street lamps, hydrants, fences, the entrance island and its signs. The pond, pool and geese are in
// MapBuilder.Pond.cs; Dan's house and lot in MapBuilder.DanHouse.cs. Nothing here has a collider except the houses (a box
// each) and the fences' trigger panels: curbs, sidewalks and street furniture are visual only so they cannot snag the car.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        sealed class HoodCtx
        {
            public MapData map; public TerrainGrid terrain; public Materials mats; public Transform root; public Neighborhood hood;
            public TreeBuilder.Result trees; public RoadIndex roadIndex;
            public Chunks Ground = new Chunks();                 // ribbons lying on the terrain (drives, sidewalks, mulch, straw): depth-offset material
            public Chunks Props = new Chunks();                  // small 3D things: curbs, lamps, mailboxes, shrubs
            public Chunks Walks = new Chunks();                  // Round 11: sidewalks (their own material, drawn over the drives they cross)
            public List<(Vector2 a, Vector2 b, float half)> DriveSegs = new List<(Vector2, Vector2, float)>();   // Round 9: every drive, planned before the trees (Neighborhood.PlanDrives)
            public readonly Breakables Mailboxes3D = new Breakables();   // Round 9: mailboxes break like the fences
            public readonly Breakables Lamps3D = new Breakables(), Hydrants3D = new Breakables();   // Round 10: street lamps and fire hydrants too
            public readonly StringBuilder Log = new StringBuilder();
            public readonly List<ViewPt> Views = new List<ViewPt>();
            public int GrassyDrives, FlagsUp;
            public int Houses, Drives, Mailboxes, Lamps, Hydrants, FencePanels, FenceRuns, CurbMetres, WalkMetres, ShortcutCleared;
            public float Lift => 0.06f;
            public float Y(float x, float z) => terrain.Height(x, z);
            public bool OnDrive(Vector2 p, float margin)
            {
                foreach (var d in DriveSegs)
                {
                    var ab = d.b - d.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - d.a, ab) / ab.sqrMagnitude);
                    if ((d.a + ab * t - p).magnitude < d.half + margin) return true;
                }
                return false;
            }
        }

        // viewpoints for the Round 7 screenshots and the performance check, written to Assets/Generated/hood_views.json
        [Serializable] class ViewPt { public string label; public float x, z, fx, fz, qx, qz; public int index; }
        [Serializable] class ViewsJson { public List<ViewPt> points = new List<ViewPt>(); }

        static void AddView(HoodCtx c, string label, Vector2 at, Vector2 front, Vector2 roadPoint, int index = -1)
            => c.Views.Add(new ViewPt { label = label, x = at.x, z = at.y, fx = front.x, fz = front.y, qx = roadPoint.x, qz = roadPoint.y, index = index });

        static void ExportViews(HoodCtx c)
        {
            var json = new ViewsJson { points = c.Views };
            File.WriteAllText(Path.Combine(ProjectRoot, GeneratedDir, "hood_views.json"), JsonUtility.ToJson(json, true));
        }

        static readonly Road NoRoad = new Road { name = "", @class = "", points = new float[0] };
        static readonly Color ConcreteLight = new Color(0.60f, 0.59f, 0.56f), CurbConcrete = new Color(0.66f, 0.64f, 0.60f), CurbFace = new Color(0.52f, 0.50f, 0.47f);

        static void BuildNeighborhood(MapData map, TerrainGrid terrain, Materials mats, Transform world, Neighborhood hood, TreeBuilder.Result trees)
        {
            if (!hood.Present) return;
            var root = new GameObject("Neighborhood").transform;
            root.SetParent(world, false);
            var ctx = new HoodCtx { map = map, terrain = terrain, mats = mats, root = root, hood = hood, trees = trees, roadIndex = new RoadIndex(map.roads), DriveSegs = hood.DriveSegs };
            BuildHouses(ctx);
            BuildStreets(ctx);
            BuildFences(ctx);
            BuildEntrance(ctx);
            BuildPond(ctx);
            BuildPoolSite(ctx);
            BuildDanLot(ctx);
            BuildRound9(ctx);   // Round 9: the gazebo, the playground, the sidewalk with benches, the pool fence, the entrance monuments
            ctx.Mailboxes3D.ToObjects("Mailboxes", mats.House, root, 1.25f);
            ctx.Lamps3D.ToObjects("StreetLamps", mats.House, root, 1f);
            var hyd = ctx.Hydrants3D.ToObjects("Hydrants", mats.House, root, 0.3f);
            foreach (var fc in hyd.GetComponentsInChildren<FenceChunk>()) fc.gameObject.AddComponent<HydrantChunk>();
            var water = new GameObject("HydrantWater").AddComponent<HydrantWater>();   // Round 10, item 9
            water.transform.SetParent(root, false);
            water.sprayMaterial = SpraySoftMaterial != null ? SpraySoftMaterial : MakeSprayMaterial(); water.groundMaterial = mats.Driveway;
            ExportViews(ctx);
            ctx.Ground.ToObjects("HoodGround", mats.Driveway, root, false);
            ctx.Walks.ToObjects("HoodSidewalks", mats.Sidewalk, root, false);
            ctx.Props.ToObjects("HoodProps", mats.House, root, false);   // curbs, mailboxes, lamps, shrubs: small, no shadows (performance)
            Debug.Log($"[Neighborhood] {ctx.Houses} houses (styles A {hood.Houses.Count(h => h.style == HouseStyle.A)}, B {hood.Houses.Count(h => h.style == HouseStyle.B)}, " +
                      $"C {hood.Houses.Count(h => h.style == HouseStyle.C)}, D {hood.Houses.Count(h => h.style == HouseStyle.D)}), {ctx.Drives} drives, {ctx.Mailboxes} mailboxes, " +
                      $"{ctx.Lamps} lamps, {ctx.Hydrants} hydrants, {ctx.CurbMetres} m of curb, {ctx.WalkMetres} m of sidewalk, {ctx.FenceRuns} fence runs / {ctx.FencePanels} panels; " +
                      $"Round 9: drives rebuilt as a fine grid, {ctx.GrassyDrives} of {ctx.Drives} had ground showing through the old surface; {ctx.Mailboxes} mailboxes breakable, {ctx.FlagsUp} flags up; " +
                      $"trees: {hood.TreesMovedOffDrives} moved off drives into the yard, {hood.TreesDroppedOffDrives} on a drive with no room beside it removed; {ctx.Log}");
        }

        // ------------------------------------------------------------ houses, drives, mailboxes

        static readonly Color[] ShrubGreens =
        {
            new Color(0.13f, 0.30f, 0.14f), new Color(0.17f, 0.34f, 0.15f), new Color(0.12f, 0.26f, 0.15f), new Color(0.30f, 0.16f, 0.22f), // the last: a red-leaf shrub
        };

        static void BuildHouses(HoodCtx c)
        {
            var chunks = new Dictionary<Vector2Int, MeshBuilder>();
            var detailChunks = new Dictionary<Vector2Int, MeshBuilder>();
            var colRoot = new GameObject("HouseColliders").transform; colRoot.SetParent(c.root, false);
            var hroot = new GameObject("HoodHouses").transform; hroot.SetParent(c.root, false);
            foreach (var h in c.hood.Houses)
            {
                var key = new Vector2Int(Mathf.FloorToInt(h.footprintCentre.x / 128f), Mathf.FloorToInt(h.footprintCentre.y / 128f));
                if (!chunks.TryGetValue(key, out var mb)) chunks[key] = mb = new MeshBuilder();
                if (!detailChunks.TryGetValue(key, out var db)) detailChunks[key] = db = new MeshBuilder();
                h.detail = db;
                if (!h.dan) BuildHouseWithShortcuts(c, mb, db, h);   // Round 8: a house on a shortcut is opened up or given a roof collider
                // the front yard: round shrubs either side of the entry, as in the photos
                int shrubs = h.dan ? 0 : 3 + (int)(Hash01(h.seed, 81) * 3f);
                for (int i = 0; i < shrubs; i++)
                {
                    float u = Mathf.Lerp(h.garageU1 + 0.6f, h.W / 2 - 0.5f, (i + 0.5f) / shrubs) + (Hash01(h.seed, 82 + i) - 0.5f) * 0.5f;
                    Vector3 p = h.frame.P(u, 0f, h.D / 2 + 0.85f + Hash01(h.seed, 90 + i) * 0.35f);
                    if (c.hood.InCorridor(new Vector2(p.x, p.z), 1f)) continue;   // Round 8: not on a shortcut's line
                    float gy = c.Y(p.x, p.z), r = 0.55f + 0.35f * Hash01(h.seed, 100 + i);
                    var col = PickOf(ShrubGreens, h.seed, 110 + i);
                    Blob(mb, new Vector3(p.x, gy + 0.05f, p.z), new Vector3(r, r * 0.85f, r), col * 1.15f, col * 0.75f, 8, 4, true, h.seed + i);
                }
                // the collider: one box over the footprint (the car never needs to know about eaves and trim)
                if (c.hood.OpenHouses.ContainsKey(h.index)) { OpenHouseColliders(c, h, colRoot); c.Houses++; BuildDrive(c, h); BuildMailbox(c, h); continue; }
                if (c.hood.JumpHouses.Contains(h.index)) { c.Houses++; BuildDrive(c, h); BuildMailbox(c, h); continue; }   // Round 8: its own mesh is its collider (a box would stand above the eaves)
                var go = new GameObject("House " + h.index); go.transform.SetParent(colRoot, false);
                float hh = h.style == HouseStyle.C ? 3.0f : 5.9f;
                float yMid = (h.baseY + h.floorY + hh) / 2f;
                go.transform.SetPositionAndRotation(h.frame.P(0f, 0f, 0f) + Vector3.up * (yMid - h.floorY), Quaternion.LookRotation(h.frame.F, Vector3.up));
                var box = go.AddComponent<BoxCollider>(); box.size = new Vector3(h.W, hh - (h.baseY - h.floorY), h.D);
                c.Houses++;
                var fr = new Vector2(h.frame.F.x, h.frame.F.z);
                string vk = h.dan ? "dan" : h.style == HouseStyle.A ? (h.brick ? "style_A_brick" : "style_A_siding") : "style_" + h.style;
                if (!c.Views.Exists(v => v.label == vk))
                    AddView(c, vk, h.footprintCentre, fr, h.nearestRoadPoint, h.index);
                BuildDrive(c, h);
                BuildMailbox(c, h);
            }
            foreach (var kv in chunks) kv.Value.ToObject($"HoodHouses_{kv.Key.x}_{kv.Key.y}", c.mats.House, hroot, false);
            foreach (var kv in detailChunks) kv.Value.ToObject($"HoodHouseDetail_{kv.Key.x}_{kv.Key.y}", c.mats.House, hroot, false, false);   // no shadows: doors, windows and trim are flat quads on walls
        }

        static void BuildDrive(HoodCtx c, HouseSpec h)
        {
            // Round 9: the path is planned once (Neighborhood.PlanDrives, before the trees, so no tree stands on a drive), and
            // the surface is a grid (5 vertices across, a row every 0.75 m): the old two-edge ribbon let the lawn show through
            // wherever the ground between its edges bulged above the straight line between them (BUG-008)
            if (!c.hood.DrivePlans.TryGetValue(h.index, out var dp)) return;
            var pts = dp.pts; var widths = dp.widths; bool asphalt = dp.asphalt;
            if (GroundShowedThroughOldRibbon(c, pts, widths)) c.GrassyDrives++;
            Color col = asphalt ? Shade(new Color(0.22f, 0.22f, 0.23f), 0.9f + 0.2f * Hash01(h.seed, 121)) : Shade(new Color(0.58f, 0.57f, 0.54f), 0.92f + 0.14f * Hash01(h.seed, 122));
            c.Ground.Tint = col;
            // Round 11 (BUG-013: lawn showing on 1763's drive): the inner vertices stand on the highest ground near them, and
            // no part of a drive is drawn under a shortcut's ramp
            RibbonGridMax(c.Ground.At(pts[pts.Count / 2]), pts, widths, 0.075f, c.terrain, 4, false, p => c.hood.UnderShortcutPrism(p));
            // joints of a concrete drive: a seam every 3 m
            if (!asphalt)
            {
                c.Ground.Tint = Shade(col, 0.82f);
                for (int i = 4; i + 1 < pts.Count; i += 4)
                {
                    var d = (pts[i + 1] - pts[i - 1]).normalized; var nrm = new Vector2(-d.y, d.x);
                    var mb = c.Ground.At(pts[i]);
                    Vector3 V(Vector2 p, float dd) => new Vector3(p.x + d.x * dd, c.Y(p.x, p.y) + 0.082f, p.y + d.y * dd);
                    for (int k = 0; k < 4; k++)
                    {
                        var a = pts[i] + nrm * widths[i] * (0.5f - k / 4f); var b = pts[i] + nrm * widths[i] * (0.5f - (k + 1) / 4f);
                        Face(mb, V(a, -0.02f), V(b, -0.02f), V(b, 0.02f), V(a, 0.02f), Vector3.up);
                    }
                }
            }
            c.Drives++;
        }

        // Round 9 (BUG-008 a): would the Round 7 surface (a vertex at each edge, every ~1.5 m, 7.5 cm up) have let the lawn
        // show? It did wherever the ground in the middle of the drive stood above the straight line between the two edges.
        static bool GroundShowedThroughOldRibbon(HoodCtx c, List<Vector2> pts, float[] widths)
        {
            for (int i = 0; i + 1 < pts.Count; i += 2)
            {
                var d = (pts[i + 1] - pts[i]).normalized; var nrm = new Vector2(-d.y, d.x);
                var l = pts[i] + nrm * widths[i] * 0.5f; var r = pts[i] - nrm * widths[i] * 0.5f;
                foreach (float f in new[] { 0f, 0.25f, -0.25f })
                {
                    var m = pts[i] + nrm * widths[i] * f;
                    float surface = Mathf.Lerp(c.Y(l.x, l.y), c.Y(r.x, r.y), 0.5f - f) + 0.075f;
                    if (c.Y(m.x, m.y) > surface + 0.01f) return true;
                }
            }
            return false;
        }

        // a ribbon like RibbonVar, but with 'across' cells across its width, so it follows the ground between the edges
        static void RibbonGrid(MeshBuilder mb, List<Vector2> pts, float[] width, float lift, TerrainGrid t, int across)
        {
            int start = mb.Count, cols = across + 1;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 dirIn = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                Vector2 dirOut = i < pts.Count - 1 ? (pts[i + 1] - pts[i]).normalized : dirIn;
                Vector2 tg = (dirIn + dirOut).normalized; if (tg.sqrMagnitude < 1e-6f) tg = dirOut;
                Vector2 nrm = new Vector2(-tg.y, tg.x);
                for (int k = 0; k < cols; k++)
                {
                    var p = pts[i] + nrm * width[i] * (0.5f - k / (float)across);
                    mb.Vertex(new Vector3(p.x, Ground(t, p.x, p.y) + lift, p.y));
                }
                if (i > 0)
                    for (int k = 0; k < across; k++)
                    {
                        int a = start + (i - 1) * cols + k, b = a + cols;
                        mb.Tri(a, b, a + 1); mb.Tri(a + 1, b, b + 1);
                    }
            }
        }

        // a ribbon with its own width at each point, lying on the terrain
        static void RibbonVar(MeshBuilder mb, List<Vector2> pts, float[] width, float lift, TerrainGrid t)
        {
            int start = mb.Count;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 dirIn = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                Vector2 dirOut = i < pts.Count - 1 ? (pts[i + 1] - pts[i]).normalized : dirIn;
                Vector2 tg = (dirIn + dirOut).normalized; if (tg.sqrMagnitude < 1e-6f) tg = dirOut;
                Vector2 nrm = new Vector2(-tg.y, tg.x);
                Vector2 l = pts[i] + nrm * width[i] * 0.5f, r = pts[i] - nrm * width[i] * 0.5f;
                mb.Vertex(new Vector3(l.x, Ground(t, l.x, l.y) + lift, l.y));
                mb.Vertex(new Vector3(r.x, Ground(t, r.x, r.y) + lift, r.y));
                if (i > 0) { int a = start + (i - 1) * 2; mb.Tri(a, a + 2, a + 1); mb.Tri(a + 1, a + 2, a + 3); }
            }
        }

        static void BuildMailbox(HoodCtx c, HouseSpec h)
        {
            float u = (h.garageU0 + h.garageU1) / 2f;
            Vector3 s3 = h.frame.P(u, 0f, h.D / 2);
            var S = new Vector2(s3.x, s3.z);
            c.hood.Nearest(S, out Vector2 q, out float half, out Vector2 tan, out float dist);
            Vector2 toHouse = (S - q).normalized;
            float side = Hash01(h.seed, 130) < 0.5f ? 1f : -1f;
            float alongM = 3.4f + (h.style == HouseStyle.D ? 0f : 0.5f);
            if (h.dan && c.hood.DrivePlans.TryGetValue(h.index, out var dp))
            {
                // Round 11 (BUG-006, Dan: "my house doesn't have a mailbox. should be to the right of the driveway"): just past
                // the right-hand edge of his drive as seen from the street (the house frame's +u)
                var right = new Vector2(h.frame.R.x, h.frame.R.z) * h.frame.M;
                side = Vector2.Dot(tan, right) >= 0f ? 1f : -1f;
                alongM = dp.widths[dp.widths.Length - 1] * 0.5f + 0.95f;
            }
            Vector2 along = tan * side;
            Vector2 p = q + toHouse * (half + 0.45f + 0.18f + 0.55f) + along * alongM;
            if (h.dan) { int k = 0; while (c.OnDrive(p, 0.8f) && k++ < 12) p += along * 0.25f; }   // Round 11: Dan's stays on the right of his drive
            else if (c.OnDrive(p, 0.8f)) p = q + toHouse * (half + 1.2f) - along * 4.2f;
            if (c.OnDrive(p, 0.8f) || c.hood.RoadClearance(p) < 0.4f) { if (h.dan) c.Log.Append("BUG-006 Dan's mailbox: NO ROOM beside his drive; "); return; }
            if (c.hood.InCorridor(p, 1f)) return;   // Round 8: no mailbox on a shortcut's line
            if (c.roadIndex.Covers(p, NoRoad, 0.8f)) return;
            float gy = c.Y(p.x, p.y);
            // Round 9 (BUG-008 c, d): breakable (FenceChunk: a trigger, a little speed lost, whole again at race start), and
            // the flag is down on most of them, up on about one in five (a stable hash)
            var mb = c.Mailboxes3D.Begin(p);
            Color post = Hash01(h.seed, 131) < 0.6f ? new Color(0.92f, 0.92f, 0.9f) : new Color(0.3f, 0.22f, 0.15f);
            c.Mailboxes3D.Piece(() => Post(mb, new Vector3(p.x, gy - 0.05f, p.y), 1.05f, 0.1f, post));
            var face = -toHouse; var a = new Vector3(p.x - face.x * 0.24f, gy + 1.17f, p.y - face.y * 0.24f); var b = new Vector3(p.x + face.x * 0.24f, gy + 1.17f, p.y + face.y * 0.24f);
            var flagSide = new Vector3(-face.y, 0, face.x) * 0.11f; var fwd = new Vector3(face.x, 0, face.y);
            bool flagUp = Hash01(h.seed, 133) < 0.2f;
            c.Mailboxes3D.Piece(() =>
            {
                Beam(mb, a, b, 0.2f, 0.22f, Hash01(h.seed, 132) < 0.5f ? new Color(0.12f, 0.12f, 0.13f) : new Color(0.55f, 0.57f, 0.6f));
                var red = new Color(0.8f, 0.1f, 0.08f);
                if (flagUp) Beam(mb, a + Vector3.up * 0.12f + flagSide, a + Vector3.up * 0.28f + flagSide, 0.03f, 0.03f, red);
                else Beam(mb, a + Vector3.up * 0.03f + flagSide + fwd * 0.04f, a + Vector3.up * 0.03f + flagSide + fwd * 0.2f, 0.03f, 0.03f, red, Vector3.up);
            });
            c.Mailboxes3D.End(new Vector3(p.x, gy + 0.7f, p.y), fwd, 0.6f, 1.4f);
            if (flagUp) c.FlagsUp++;
            if (h.dan) { c.Log.Append("BUG-006 Dan's mailbox: right of his drive; "); c.hood.DanMailbox = p; }
            AddView(c, "mailbox", p, toHouse, q, h.index);   // Round 9: for the smash check in the exe (ExeShotsRunner)
            c.hood.KeepOut(p, 0.9f);
            c.Mailboxes++;
        }
    }
}
