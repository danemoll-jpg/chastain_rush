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
            public readonly List<(Vector2 a, Vector2 b, float half)> DriveSegs = new List<(Vector2, Vector2, float)>();
            public readonly StringBuilder Log = new StringBuilder();
            public readonly List<ViewPt> Views = new List<ViewPt>();
            public int Houses, Drives, Mailboxes, Lamps, Hydrants, FencePanels, FenceRuns, CurbMetres, WalkMetres;
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
            var ctx = new HoodCtx { map = map, terrain = terrain, mats = mats, root = root, hood = hood, trees = trees, roadIndex = new RoadIndex(map.roads) };
            BuildHouses(ctx);
            BuildStreets(ctx);
            BuildFences(ctx);
            BuildEntrance(ctx);
            BuildPond(ctx);
            BuildPoolSite(ctx);
            BuildDanLot(ctx);
            ExportViews(ctx);
            ctx.Ground.ToObjects("HoodGround", mats.Driveway, root, false);
            ctx.Props.ToObjects("HoodProps", mats.House, root, false);   // curbs, mailboxes, lamps, shrubs: small, no shadows (performance)
            Debug.Log($"[Neighborhood] {ctx.Houses} houses (styles A {hood.Houses.Count(h => h.style == HouseStyle.A)}, B {hood.Houses.Count(h => h.style == HouseStyle.B)}, " +
                      $"C {hood.Houses.Count(h => h.style == HouseStyle.C)}, D {hood.Houses.Count(h => h.style == HouseStyle.D)}), {ctx.Drives} drives, {ctx.Mailboxes} mailboxes, " +
                      $"{ctx.Lamps} lamps, {ctx.Hydrants} hydrants, {ctx.CurbMetres} m of curb, {ctx.WalkMetres} m of sidewalk, {ctx.FenceRuns} fence runs / {ctx.FencePanels} panels; {ctx.Log}");
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
                if (!h.dan) BuildHouse(mb, h);
                // the front yard: round shrubs either side of the entry, as in the photos
                int shrubs = h.dan ? 0 : 3 + (int)(Hash01(h.seed, 81) * 3f);
                for (int i = 0; i < shrubs; i++)
                {
                    float u = Mathf.Lerp(h.garageU1 + 0.6f, h.W / 2 - 0.5f, (i + 0.5f) / shrubs) + (Hash01(h.seed, 82 + i) - 0.5f) * 0.5f;
                    Vector3 p = h.frame.P(u, 0f, h.D / 2 + 0.85f + Hash01(h.seed, 90 + i) * 0.35f);
                    float gy = c.Y(p.x, p.z), r = 0.55f + 0.35f * Hash01(h.seed, 100 + i);
                    var col = PickOf(ShrubGreens, h.seed, 110 + i);
                    Blob(mb, new Vector3(p.x, gy + 0.05f, p.z), new Vector3(r, r * 0.85f, r), col * 1.15f, col * 0.75f, 8, 4, true, h.seed + i);
                }
                // the collider: one box over the footprint (the car never needs to know about eaves and trim)
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
            float u = (h.garageU0 + h.garageU1) / 2f;
            Vector3 s3 = h.frame.P(u, 0f, h.D / 2 + 0.05f);
            var S = new Vector2(s3.x, s3.z);
            c.hood.Nearest(S, out Vector2 q, out float half, out Vector2 tan, out float dist);
            Vector2 toHouse = (S - q).normalized;
            Vector2 E = q + toHouse * (half + 0.45f);              // the road edge
            var F2 = new Vector2(h.frame.F.x, h.frame.F.z);
            float len = (E - S).magnitude;
            if (len < 2f) return;
            Vector2 C1 = S + F2 * Mathf.Clamp(len * 0.55f, 2f, 9f);
            var pts = new List<Vector2>();
            int n = Mathf.Max(4, Mathf.CeilToInt(len / 1.5f));
            for (int i = 0; i <= n; i++) { float t = i / (float)n; pts.Add((1 - t) * (1 - t) * S + 2 * (1 - t) * t * C1 + t * t * E); }
            float w0 = h.style == HouseStyle.D ? 3.8f : 5.8f;
            var widths = new float[pts.Count];
            for (int i = 0; i < pts.Count; i++) { float t = i / (float)n; widths[i] = w0 + (t > 0.78f ? (t - 0.78f) / 0.22f * 1.5f : 0f); }
            bool asphalt = h.dan || Hash01(h.seed, 120) < 0.28f;
            Color col = asphalt ? Shade(new Color(0.22f, 0.22f, 0.23f), 0.9f + 0.2f * Hash01(h.seed, 121)) : Shade(new Color(0.58f, 0.57f, 0.54f), 0.92f + 0.14f * Hash01(h.seed, 122));
            c.Ground.Tint = col;
            RibbonVar(c.Ground.At(pts[pts.Count / 2]), pts, widths, 0.075f, c.terrain);
            // joints of a concrete drive: a seam every 3 m
            if (!asphalt)
            {
                c.Ground.Tint = Shade(col, 0.82f);
                for (int i = 2; i + 1 < pts.Count; i += 2)
                {
                    var d = (pts[i + 1] - pts[i - 1]).normalized; var nrm = new Vector2(-d.y, d.x);
                    var a = pts[i] + nrm * widths[i] * 0.5f; var b = pts[i] - nrm * widths[i] * 0.5f;
                    var mb = c.Ground.At(pts[i]);
                    Vector3 V(Vector2 p, float dd) => new Vector3(p.x + d.x * dd, c.Y(p.x, p.y) + 0.082f, p.y + d.y * dd);
                    Face(mb, V(a, -0.02f), V(b, -0.02f), V(b, 0.02f), V(a, 0.02f), Vector3.up);
                }
            }
            for (int i = 0; i + 1 < pts.Count; i++) c.DriveSegs.Add((pts[i], pts[i + 1], widths[i] * 0.5f));
            c.Drives++;
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
            Vector2 along = tan * side;
            Vector2 p = q + toHouse * (half + 0.45f + 0.18f + 0.55f) + along * (3.4f + (h.style == HouseStyle.D ? 0f : 0.5f));
            if (c.OnDrive(p, 0.8f)) p = q + toHouse * (half + 1.2f) - along * 4.2f;
            if (c.OnDrive(p, 0.8f) || c.hood.RoadClearance(p) < 0.4f) return;
            if (c.roadIndex.Covers(p, NoRoad, 0.8f)) return;
            float gy = c.Y(p.x, p.y);
            var mb = c.Props.At(p);
            Color post = Hash01(h.seed, 131) < 0.6f ? new Color(0.92f, 0.92f, 0.9f) : new Color(0.3f, 0.22f, 0.15f);
            Post(mb, new Vector3(p.x, gy - 0.05f, p.y), 1.05f, 0.1f, post);
            var face = -toHouse; var a = new Vector3(p.x - face.x * 0.24f, gy + 1.17f, p.y - face.y * 0.24f); var b = new Vector3(p.x + face.x * 0.24f, gy + 1.17f, p.y + face.y * 0.24f);
            Beam(mb, a, b, 0.2f, 0.22f, Hash01(h.seed, 132) < 0.5f ? new Color(0.12f, 0.12f, 0.13f) : new Color(0.55f, 0.57f, 0.6f));
            Beam(mb, a + Vector3.up * 0.12f + new Vector3(-face.y, 0, face.x) * 0.11f, a + Vector3.up * 0.28f + new Vector3(-face.y, 0, face.x) * 0.11f, 0.03f, 0.03f, new Color(0.8f, 0.1f, 0.08f));
            c.hood.KeepOut(p, 0.9f);
            c.Mailboxes++;
        }
    }
}
