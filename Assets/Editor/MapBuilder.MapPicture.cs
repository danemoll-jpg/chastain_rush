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
        }
        [System.Serializable] class WoodsFile { public Area[] woods; }
    }
}
