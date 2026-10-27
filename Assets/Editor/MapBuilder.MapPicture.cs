using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Round 20, item 2: the picture under the in-game map (Assets/Scripts/Map/WorldMap.cs). Woodstock Rush photographs its built
// scene from above (Racer's Capture-WorldMaps, a WorldMapVisual asset); here the woods would hide the Noonday Creek trail and
// the narrow roads, so the picture is drawn from the same data the world is built from, at the end of every Build Map: the
// ground's relief (hill shading), woods and trees, water and streams, roads (by width), bridges, drives, the shopping center's
// lot, buildings, the Noonday Creek trail. North up, the whole map, about 1 m a pixel. The route, racers, shortcuts and pipes are
// drawn over it at run time. Assets/Generated/Resources/WorldMapPicture.png (git-ignored, rebuilt with the scene).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const string MapPicturePath = GeneratedDir + "/Resources/WorldMapPicture.png";

        sealed class Raster
        {
            public readonly int W, H; public readonly float Sx, Sz, X0, Z0; public readonly Color32[] Px;   // Round 59: X0, Z0 the map's south-west corner
            public Raster(int w, int h, Rect world) { W = w; H = h; Sx = w / world.width; Sz = h / world.height; X0 = world.xMin; Z0 = world.yMin; Px = new Color32[w * h]; }
            Vector2 P(Vector2 q) => new Vector2((q.x - X0) * Sx, (q.y - Z0) * Sz);
            void Span(int y, float x0, float x1, Color32 c, float a)
            {
                if (y < 0 || y >= H) return;
                int i0 = Mathf.Max(0, Mathf.CeilToInt(x0 - 0.5f)), i1 = Mathf.Min(W - 1, Mathf.FloorToInt(x1 - 0.5f));
                for (int x = i0; x <= i1; x++) Blend(y * W + x, c, a);
            }
            public void Blend(int i, Color32 c, float a)
            {
                if (a >= 0.999f) { Px[i] = c; return; }
                var o = Px[i];
                Px[i] = new Color32((byte)(o.r + (c.r - o.r) * a), (byte)(o.g + (c.g - o.g) * a), (byte)(o.b + (c.b - o.b) * a), 255);
            }
            // even-odd scanline fill of a polygon (map x, z)
            public void Fill(IList<Vector2> poly, Color32 c, float a = 1f)
            {
                if (poly.Count < 3) return;
                var p = poly.Select(P).ToArray();
                int y0 = Mathf.Max(0, Mathf.FloorToInt(p.Min(q => q.y))), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(p.Max(q => q.y)));
                var xs = new List<float>();
                for (int y = y0; y <= y1; y++)
                {
                    float sy = y + 0.5f; xs.Clear();
                    for (int k = 0; k < p.Length; k++)
                    {
                        var u = p[k]; var v = p[(k + 1) % p.Length];
                        if ((u.y <= sy && v.y > sy) || (v.y <= sy && u.y > sy)) xs.Add(u.x + (sy - u.y) / (v.y - u.y) * (v.x - u.x));
                    }
                    xs.Sort();
                    for (int k = 0; k + 1 < xs.Count; k += 2) Span(y, xs[k], xs[k + 1], c, a);
                }
            }
            // a line 'width' meters wide (at least one pixel), with round joins
            public void Line(IList<Vector2> pts, float width, Color32 c, float a = 1f)
            {
                float hw = Mathf.Max(width * 0.5f, 0.6f / Sx);
                for (int k = 0; k + 1 < pts.Count; k++)
                {
                    var u = pts[k]; var v = pts[k + 1]; var d = v - u; if (d.sqrMagnitude < 1e-6f) continue;
                    var n = new Vector2(-d.y, d.x).normalized * hw;
                    Fill(new[] { u + n, v + n, v - n, u - n }, c, a);
                }
                if (hw * Sx >= 1.5f) foreach (var q in pts) Disc(q, hw, c, a);
            }
            public void Disc(Vector2 q, float r, Color32 c, float a = 1f)
            {
                var o = P(q); float rp = r * Sx;
                for (int y = Mathf.FloorToInt(o.y - rp); y <= Mathf.CeilToInt(o.y + rp); y++)
                {
                    float dy = y + 0.5f - o.y; if (Mathf.Abs(dy) > rp) continue; float dx = Mathf.Sqrt(rp * rp - dy * dy);
                    Span(y, o.x - dx, o.x + dx, c, a);
                }
            }
        }

        static void BakeMapPicture(MapData map, TerrainGrid terrain, ShoppingCenter shop, CreekTrack ct, TreeField trees)
        {
            // Round 59: over the whole map (the extension's too); 8192 wide once the map is over 4.5 km (about 0.8 m a pixel; 4096 kept the core's 1 m)
            var ext = MapExtent(map);
            int W = ext.width > 4500f ? 8192 : 4096; int H = Mathf.RoundToInt(W * ext.height / ext.width);
            var r = new Raster(W, H, ext);
            // the ground: a grass green, lit from the north-west by the ground's slope (sampled every other pixel)
            int hw = W / 2, hh = H / 2; var hgt = new float[hw * hh]; float cx = ext.width / hw, cz = ext.height / hh;
            for (int j = 0; j < hh; j++) for (int i = 0; i < hw; i++) hgt[j * hw + i] = terrain.Height(ext.xMin + Mathf.Min((i + 0.5f) * cx, ext.width - 0.5f), ext.yMin + Mathf.Min((j + 0.5f) * cz, ext.height - 0.5f));
            float lo = hgt.Min(), hi = hgt.Max();
            var light = new Vector3(-1f, 1.4f, 1f).normalized;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = Mathf.Clamp(x / 2, 1, hw - 2), j = Mathf.Clamp(y / 2, 1, hh - 2);
                    float gx = (hgt[j * hw + i + 1] - hgt[j * hw + i - 1]) / (2f * cx), gz = (hgt[(j + 1) * hw + i] - hgt[(j - 1) * hw + i]) / (2f * cz);
                    var n = new Vector3(-gx * 2.5f, 1f, -gz * 2.5f).normalized;
                    float shade = Mathf.Clamp(0.55f + 0.6f * (Vector3.Dot(n, light) - 0.7f) * 2f, 0.35f, 1.15f);
                    float e = Mathf.InverseLerp(lo, hi, hgt[j * hw + i]);
                    var g = Color.Lerp(new Color(0.42f, 0.55f, 0.30f), new Color(0.62f, 0.62f, 0.42f), e * 0.8f) * shade;
                    r.Px[y * W + x] = (Color32)new Color(g.r, g.g, g.b, 1f);
                }
            // woods (tinted darker) and every tree as a small dot of canopy
            Color32 Wood = new Color(0.20f, 0.33f, 0.17f), Canopy = new Color(0.16f, 0.30f, 0.13f);
            var woods = JsonUtility.FromJson<WoodsFile>(File.ReadAllText(Path.Combine(ProjectRoot, "Data/map.json")));
            if (woods.woods != null) foreach (var w in woods.woods) r.Fill(ToPoints(w.outline), Wood, 0.45f);
            if (trees != null) for (int i = 0; i < trees.count; i++) { var t = TreeField.Unpack(trees.data, i); r.Disc(new Vector2(t.x, t.z), 1.6f, Canopy, 0.55f); }
            // Round 75 (B): what the build placed, from the scene itself (lots, pools, courts, plazas, paths, boardwalks, the shortcuts' built ground)
            DrawBuilt(r, hgt, hw, hh, light);
            // water: ponds and lakes, streams
            Color32 Water = new Color(0.27f, 0.52f, 0.78f), Shore = new Color(0.18f, 0.36f, 0.58f);
            foreach (var w in map.water) { var o = ToPoints(w.outline); r.Fill(o, Water); var closed = o.ToList(); closed.Add(o[0]); r.Line(closed, 1.2f, Shore); }
            if (Beds != null) for (int i = map.water.Length; i < Beds.Rings.Count; i++) { var o = Beds.Rings[i]; r.Fill(o, Water); var closed = o.ToList(); closed.Add(o[0]); r.Line(closed, 1.2f, Shore); }   // Round 21 follow-ups: P1's pool
            foreach (var s in map.streams) r.Line(ToPoints(s.points), s.kind == "river" ? 8f : 3f, Water);
            // the shopping center's lot, drives, roads (by width), bridges over them with a dark edge
            Color32 Asphalt = new Color(0.30f, 0.30f, 0.31f), Edge = new Color(0.12f, 0.12f, 0.13f), Drive = new Color(0.62f, 0.61f, 0.58f), Deck = new Color(0.55f, 0.53f, 0.50f);
            if (shop?.Lot != null && shop.Lot.Count >= 3) r.Fill(shop.Lot, Asphalt);
            // Round 74 (J, Dan: "show the airport with concrete runways on the map instead of grass"): the parking lots as asphalt, and the airport's
            // aprons, taxiways and runways as concrete (a lighter gray, a dark edge), the runways' center lines dashed white and their ends marked
            Color32 Concrete = new Color(0.66f, 0.66f, 0.63f), ConcreteEdge = new Color(0.40f, 0.40f, 0.39f), Paint = new Color(0.97f, 0.97f, 0.95f);
            if (map.parking != null) foreach (var pk in map.parking) { var o = ToPoints(pk.outline); if (o.Count >= 3) r.Fill(o, Asphalt); }
            if (map.aeroways != null)
            {
                foreach (var a in map.aeroways) if (a.kind == "apron" && a.outline != null) { var o = ToPoints(a.outline); if (o.Count < 3) continue; r.Fill(o, Concrete); var c = o.ToList(); c.Add(o[0]); r.Line(c, 1.2f, ConcreteEdge); }
                foreach (var a in map.aeroways) if ((a.kind == "taxiway" || a.kind == "runway") && a.points != null) r.Line(ToPoints(a.points), Mathf.Max(a.width, 8f) + 2f, ConcreteEdge);
                foreach (var a in map.aeroways) if ((a.kind == "taxiway" || a.kind == "runway") && a.points != null) r.Line(ToPoints(a.points), Mathf.Max(a.width, 8f), Concrete);
                foreach (var a in map.aeroways)
                {
                    if (a.kind != "runway" || a.points == null) continue;
                    var p = Resample(ToPoints(a.points), 1f); if (p.Count < 40) continue;
                    for (int i = 30; i + 30 < p.Count; i += 60) r.Line(p.GetRange(i, Mathf.Min(30, p.Count - 30 - i)), 1.2f, Paint);   // the center line: 30 m dashes
                    foreach (int e in new[] { 0, p.Count - 1 })
                    {
                        var at = p[e]; var dir = (p[e == 0 ? 20 : p.Count - 21] - at).normalized; var side = new Vector2(-dir.y, dir.x); float half = a.width * .5f - 3f;
                        for (float l = -half; l <= half + .01f; l += 3f) r.Line(new List<Vector2> { at + dir * 6f + side * l, at + dir * 36f + side * l }, 1.5f, Paint);   // the threshold stripes
                    }
                }
            }
            if (map.driveways != null) foreach (var d in map.driveways) r.Line(ToPoints(d.points), Mathf.Max(d.width, 3f), Drive);
            foreach (var rd in map.roads) r.Line(ToPoints(rd.points), Mathf.Max(rd.width, 4f) + 1.5f, Edge);
            foreach (var rd in map.roads) r.Line(ToPoints(rd.points), Mathf.Max(rd.width, 4f), Asphalt);
            foreach (var b in map.bridges) { r.Line(ToPoints(b.points), Mathf.Max(b.width, 4f) + 3f, Edge); r.Line(ToPoints(b.points), Mathf.Max(b.width, 4f) + 1f, Deck); }
            // the Noonday Creek trail
            if (ct != null && ct.P.Count > 1) { var l = ct.P.ToList(); l.Add(ct.P[0]); r.Line(l, ct.Data.tread_half_m * 2f, new Color(0.62f, 0.44f, 0.24f)); }
            // buildings: roofs
            Color32 Roof = new Color(0.66f, 0.55f, 0.45f), RoofEdge = new Color(0.30f, 0.24f, 0.20f);
            foreach (var b in map.buildings) { var f = ToPoints(b.footprint); r.Fill(f, Roof); var closed = f.ToList(); closed.Add(f[0]); r.Line(closed, 0.8f, RoofEdge); }

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false); tex.SetPixels32(r.Px); tex.Apply();
            var full = Path.Combine(ProjectRoot, MapPicturePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            FlushAssets(); AssetDatabase.ImportAsset(MapPicturePath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(MapPicturePath) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Default; ti.maxTextureSize = W; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Clamp;
                ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.npotScale = TextureImporterNPOTScale.None; ti.SaveAndReimport();
            }
            Debug.Log($"[Round20] the map's picture: {W} x {H} ({ext.width / W:0.00} m a pixel), {MapPicturePath}");
            BakeMapStreets(map);   // Round 75 (B): the street names for the map screen
        }
        [System.Serializable] class WoodsFile { public Area[] woods; }

        // Round 75 (B, Dan: "why didn't it fix the mall parking lot?" and "I would like that stuff fixed in general ... community swimming pools should
        // show up as well"): the picture drew only map.json's lists, so what a step builds by itself (Town Center's lots, the stunt park, the quarry's
        // pads and lanes, the helipads, every pool and court, plazas, the trails' boardwalks, a shortcut's ramps) never showed. Now every visible mesh
        // in the built scene (MapPicture runs after every world step) is drawn from above: its upward-facing triangles that stand on or over the
        // ground (none underground: the pipes, the crypt), the highest one at each pixel, in its own colors (vertex color x material color), lit as
        // the ground is, water surfaces in the map's water blue. Glass and other see-through materials are left out. Nothing needs adding by hand:
        // a new built thing is on the map at the next build. The map's own drawing (water, roads, lots, buildings) goes on top as before.
        static void DrawBuilt(Raster r, float[] hgt, int hw, int hh, Vector3 light)
        {
            var top = new float[r.W * r.H]; for (int i = 0; i < top.Length; i++) top[i] = float.NegativeInfinity;
            Color WaterC = new Color(0.27f, 0.52f, 0.78f);
            int rends = 0; long tris = 0, pix = 0;
            float Ground(int x, int y) => hgt[Mathf.Clamp(y / 2, 0, hh - 1) * hw + Mathf.Clamp(x / 2, 0, hw - 1)];
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!mr.enabled || mr.gameObject.layer == 8) continue;   // (8: the Horizon layer)
                var mf = mr.GetComponent<MeshFilter>(); var mesh = mf ? mf.sharedMesh : null; if (!mesh) continue;
                var bb = mr.bounds; if (bb.max.x < r.X0 || bb.max.z < r.Z0 || bb.min.x > r.X0 + r.W / r.Sx || bb.min.z > r.Z0 + r.H / r.Sz) continue;
                var mats = mr.sharedMaterials; if (mats == null || mats.Length == 0) continue;
                Vector3[] v; Color32[] vc; try { v = mesh.vertices; vc = mesh.colors32; } catch { continue; }
                if (v.Length == 0) continue;
                var m = mr.localToWorldMatrix; var w = new Vector3[v.Length]; for (int i = 0; i < v.Length; i++) w[i] = m.MultiplyPoint3x4(v[i]);
                bool flip = m.determinant < 0f, any = false, hasCol = vc != null && vc.Length == v.Length;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    if (mesh.GetTopology(sub) != MeshTopology.Triangles) continue;
                    var mat = mats[Mathf.Min(sub, mats.Length - 1)]; if (!mat || !mat.shader) continue;
                    string sn = mat.shader.name;
                    if (mat.renderQueue >= 2450 || sn.Contains("Sky") || sn.Contains("Cloud") || sn.Contains("Glass") || sn.Contains("Horizon") || mat.name.Contains("Glass")) continue;   // see-through, the sky
                    bool water = mat.name.Contains("Water");
                    Color mc = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                    var idx = mesh.GetTriangles(sub);
                    for (int k = 0; k + 2 < idx.Length; k += 3)
                    {
                        int ia = idx[k], ib = flip ? idx[k + 2] : idx[k + 1], ic = flip ? idx[k + 1] : idx[k + 2];
                        Vector3 a = w[ia], b = w[ib], c = w[ic]; var n = Vector3.Cross(b - a, c - a); float nl = n.magnitude;
                        if (nl < 1e-8f) continue; n /= nl; if (n.y < 0.35f) continue;
                        tris++; any = true;
                        Color col;
                        if (water) col = WaterC;
                        else
                        {
                            Color ca = hasCol ? (Color)vc[ia] : Color.white, cb = hasCol ? (Color)vc[ib] : Color.white, cc = hasCol ? (Color)vc[ic] : Color.white;
                            col = (ca + cb + cc) / 3f * mc; col *= Mathf.Clamp(0.78f + 0.4f * Vector3.Dot(n, light), 0.6f, 1.1f);
                        }
                        var c32 = (Color32)new Color(col.r, col.g, col.b, 1f);
                        // the triangle in pixels; each pixel center inside it, at the triangle's height there
                        Vector2 pa = new Vector2((a.x - r.X0) * r.Sx, (a.z - r.Z0) * r.Sz), pb = new Vector2((b.x - r.X0) * r.Sx, (b.z - r.Z0) * r.Sz), pc = new Vector2((c.x - r.X0) * r.Sx, (c.z - r.Z0) * r.Sz);
                        float area = (pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x);
                        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.x, Mathf.Min(pb.x, pc.x)))), x1 = Mathf.Min(r.W - 1, Mathf.CeilToInt(Mathf.Max(pa.x, Mathf.Max(pb.x, pc.x))));
                        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.y, Mathf.Min(pb.y, pc.y)))), y1 = Mathf.Min(r.H - 1, Mathf.CeilToInt(Mathf.Max(pa.y, Mathf.Max(pb.y, pc.y))));
                        if (x0 > x1 || y0 > y1) continue;
                        bool hit = false;
                        if (Mathf.Abs(area) > 1e-6f)
                            for (int y = y0; y <= y1; y++)
                                for (int x = x0; x <= x1; x++)
                                {
                                    float qx = x + .5f, qy = y + .5f;
                                    float l0 = ((pb.x - qx) * (pc.y - qy) - (pb.y - qy) * (pc.x - qx)) / area, l1 = ((pc.x - qx) * (pa.y - qy) - (pc.y - qy) * (pa.x - qx)) / area, l2 = 1f - l0 - l1;
                                    if (l0 < -1e-4f || l1 < -1e-4f || l2 < -1e-4f) continue;
                                    hit = true; Put(x, y, l0 * a.y + l1 * b.y + l2 * c.y, c32);
                                }
                        if (!hit) { var ct = (pa + pb + pc) / 3f; int x = (int)ct.x, y = (int)ct.y; if (x >= 0 && y >= 0 && x < r.W && y < r.H) Put(x, y, (a.y + b.y + c.y) / 3f, c32); }   // smaller than a pixel
                    }
                }
                if (any) rends++;
            }
            void Put(int x, int y, float h, Color32 c32)
            {
                int i = y * r.W + x; if (h <= top[i]) return;
                float g = Ground(x, y); if (h < g - 0.4f || h > g + 60f) return;   // on or over the ground, not high in the air (the overpasses' decks are under 60 m)
                top[i] = h; r.Px[i] = c32; pix++;
            }
            Debug.Log($"[Round75] the map's picture: the built scene drawn from above: {rends} renderers, {tris} upward triangles, {pix} pixels written");
        }

        // Round 75 (B, Dan: "when zoomed in, it would be great to get street names as well"): the streets' names for the map screen's labels, written beside
        // the picture (Assets/Generated/Resources/MapStreets.json, read by Map/WorldMap.Streets.cs): one entry per named road of map.json (service roads and
        // links left out), its line simplified, its rank (0 interstates, 1 main roads, 2 tertiary, 3 side streets), its short name ("Chastain Rd NW") and
        // its route shield ("I-75", "US 41") from Data/road_refs.json (Tools/mapgen/road_refs.py)
        [System.Serializable] class StreetOut { public string n, s; public int k; public float[] p; }
        [System.Serializable] class StreetsOut { public StreetOut[] streets; }
        [System.Serializable] class RefsFile { public RefPair[] refs; } [System.Serializable] class RefPair { public long id; public string r; }
        const string MapStreetsPath = GeneratedDir + "/Resources/MapStreets.json";
        static void BakeMapStreets(MapData map)
        {
            var refs = new Dictionary<long, string>();
            var refFile = Path.Combine(ProjectRoot, "Data/road_refs.json");
            if (File.Exists(refFile)) foreach (var q in JsonUtility.FromJson<RefsFile>(File.ReadAllText(refFile)).refs ?? new RefPair[0]) refs[q.id] = q.r;
            var outList = new List<StreetOut>();
            foreach (var rd in map.roads)
            {
                string cls = rd.@class ?? ""; if (cls == "service" || cls.EndsWith("_link")) continue;
                int k = cls == "motorway" ? 0 : cls == "trunk" || cls == "primary" || cls == "secondary" ? 1 : cls == "tertiary" ? 2 : 3;
                refs.TryGetValue(rd.osm_id, out string rf); string shield = Shield(rf);
                string name = k == 0 ? "" : RoadLookup.Short(rd.name);
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(shield)) continue;
                var pts = Simplify(ToPoints(rd.points), 3f); if (pts.Count < 2) continue;
                outList.Add(new StreetOut { n = name, s = shield, k = k, p = pts.SelectMany(q => new[] { Mathf.Round(q.x * 10f) / 10f, Mathf.Round(q.y * 10f) / 10f }).ToArray() });
            }
            var full = Path.Combine(ProjectRoot, MapStreetsPath); Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, JsonUtility.ToJson(new StreetsOut { streets = outList.ToArray() }));
            AssetDatabase.ImportAsset(MapStreetsPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[Round75] the map's street names: {outList.Count} named roads, {outList.Count(q => !string.IsNullOrEmpty(q.s))} with a route shield, {MapStreetsPath}");
        }
        // "I 75" -> "I-75", "I 575;GA 5" -> "I-575", "US 41;GA 3" -> "US 41" (Georgia's own numbers left out: Dan named the interstates and US 41)
        static string Shield(string rf)
        {
            if (string.IsNullOrEmpty(rf)) return "";
            foreach (var part in rf.Split(';'))
            {
                var t = part.Trim();
                if (t.StartsWith("I ")) return "I-" + t.Substring(2).Split(' ')[0];
                if (t.StartsWith("US ")) return "US " + t.Substring(3).Split(' ')[0];
            }
            return "";
        }
        // Douglas-Peucker: a line's points within 'tol' meters of it dropped
        static List<Vector2> Simplify(List<Vector2> p, float tol)
        {
            if (p.Count < 3) return p;
            var keep = new bool[p.Count]; keep[0] = keep[p.Count - 1] = true; var st = new Stack<(int, int)>(); st.Push((0, p.Count - 1));
            while (st.Count > 0)
            {
                var (a, b) = st.Pop(); float best = -1f; int at = -1; var ab = p[b] - p[a]; float L = ab.magnitude;
                for (int i = a + 1; i < b; i++)
                {
                    float d = L < 1e-4f ? (p[i] - p[a]).magnitude : Mathf.Abs(ab.x * (p[i].y - p[a].y) - ab.y * (p[i].x - p[a].x)) / L;
                    if (d > best) { best = d; at = i; }
                }
                if (at >= 0 && best > tol) { keep[at] = true; st.Push((a, at)); st.Push((at, b)); }
            }
            var o = new List<Vector2>(); for (int i = 0; i < p.Count; i++) if (keep[i]) o.Add(p[i]); return o;
        }
    }
}
