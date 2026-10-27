using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    // 0.73 stylized clouds (sky only; Dan asked how complicated clouds are - the stylized kind is a small addition, true
    // volumetric clouds are not worth their 4K cost and would not match the low-poly world). Faceted low-poly cloud
    // clusters, flat-bottomed, in a layer 430-560 m above sea level (well above the playable world and the Mountain
    // summit), drifting slowly with one wind. The field is world-fixed and wraps around the camera; clusters shrink away
    // near the wrap edge so nothing pops. Driven by the current WorldLook preset every frame:
    //   Clear = scattered fair-weather clouds; Rain = heavy dark overcast (lightning lights it from inside);
    //   Snow = pale even overcast. Lit by the main light (sun by day, warm at dusk, the moon at night) and the sky ambient.
    // No cloud shadows (not cheap enough to be worth it). Free Roam blends with the day-night cycle automatically.
    // Chastain Rush, Round 35 (Part C): Racer's SkyClouds, ported. This map's ground is about 200 m higher than Racer's, so the
    // layer is lifted by Lift; the camera sees 750 m (Racer's about 1.8 km), so the field fades out by 600 m across and the
    // overcast deck and its skirt are 660 m round (inside the far clip). No split-screen.
    public sealed class SkyClouds : MonoBehaviour
    {
        public const float Lift = 200;
        public static float Flash;// 0..1, set by the lightning in WeatherEffects
        const int Count = 64; const float Tile = 3000, Base = 430 + Lift, Top = 560 + Lift, Fade0 = 500, Fade1 = 600;   // Chastain Rush: Lift; fade 1250-1480 in Racer
        static readonly Vector3 Wind = new Vector3(.86f, 0, .5f) * 4.5f;// m/s, from the west-south-west
        readonly Transform[] clusters = new Transform[Count]; readonly Vector3[] home = new Vector3[Count]; readonly float[] size = new float[Count];
        readonly List<Mesh> meshes = new(); Material material;
        public int Visible { get; private set; }
        // Chastain Rush, Round 75 (Dan, 2026-10-27 11:24: "I thought it was cool that you could potentially jump over clouds"): Round 74's rise over
        // high ground is undone, the layer (630-760 m) is at Kennesaw's summit (689 m) again; a cloud near the camera breaks up instead (the shader's
        // near fade, StylizedClouds: dithered away within about 260 m), so the summit leap flies over and through soft cloud, not into a white wall.
        // Overcast only: one flat cloud deck above the clusters, out to the haze, so no band of open sky shows near the horizon.
        // A ring in the haze colour from below the horizon up to the deck edge closes the last strip of open sky (the camera
        // sees 1.8 km at most, so the deck cannot reach the horizon itself); distant terrain is fogged to the same colour.
        Transform deck, skirt; Material skirtMaterial; const float DeckHeight = 485 + Lift, DeckRadius = 660;   // Chastain Rush: Lift; 1650 m round in Racer

        void Awake()
        {
            var asset = Resources.Load<Material>("SkyClouds");
            if (!asset) { enabled = false; return; }
            material = new Material(asset) { name = "Sky clouds" };
            var rng = new System.Random(7303);
            for (int i = 0; i < Count; i++)
            {
                // Stratified so the scattered (first) clusters are spread evenly, not clumped.
                int gx = i % 8, gz = (i / 8) % 8; float jx = (float)rng.NextDouble(), jz = (float)rng.NextDouble();
                home[i] = new Vector3((gx + jx) / 8 * Tile - Tile / 2, Mathf.Lerp(Base, Top - 60, (float)rng.NextDouble()), (gz + jz) / 8 * Tile - Tile / 2);
                size[i] = Mathf.Lerp(70, 150, (float)rng.NextDouble());
                var go = new GameObject("Cloud"); go.transform.SetParent(transform, false);
                var mesh = Cluster(rng); meshes.Add(mesh);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = material; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                clusters[i] = go.transform; go.SetActive(false);
            }
            {
                var v = new List<Vector3> { Vector3.zero }; var col = new List<Color> { new Color(.55f, 0, 0) }; var tri = new List<int>();
                for (int i = 0; i < 64; i++) { float a = i * Mathf.PI * 2 / 64; /* counter-clockwise from above = faces down */ v.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))); col.Add(new Color(.55f, 0, 0)); tri.AddRange(new[] { 0, 1 + i, 1 + (i + 1) % 64 }); }
                var m = new Mesh { name = "Cloud deck" }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tri, 0); m.normals = v.ConvertAll(_ => Vector3.down).ToArray(); m.RecalculateBounds(); meshes.Add(m);
                var go = new GameObject("Overcast deck"); go.transform.SetParent(transform, false); go.AddComponent<MeshFilter>().sharedMesh = m;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = material; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                deck = go.transform; go.SetActive(false);
                // skirt: double-sided ring, y 0..1 (scaled to camera-250 .. deck height)
                var sv = new List<Vector3>(); var sc = new List<Color>(); var st = new List<int>();
                for (int i = 0; i <= 64; i++) { float a = i * Mathf.PI * 2 / 64; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); sv.Add(d); sv.Add(d + Vector3.up); sc.Add(new Color(.3f, 0, 0)); sc.Add(new Color(.6f, 0, 0)); }
                for (int i = 0; i < 64; i++) { int a = i * 2; st.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3, a, a + 2, a + 1, a + 2, a + 3, a + 1 }); }
                var sm = new Mesh { name = "Cloud skirt" }; sm.SetVertices(sv); sm.SetColors(sc); sm.SetTriangles(st, 0); sm.normals = sv.ConvertAll(q => -new Vector3(q.x, 0, q.z)).ToArray(); sm.bounds = new Bounds(Vector3.up * .5f, new Vector3(2, 1, 2)); meshes.Add(sm);
                skirtMaterial = new Material(material) { name = "Sky cloud skirt" }; skirtMaterial.SetFloat("_HazeAmount", 1); skirtMaterial.SetFloat("_HazeDistance", 360);   // Chastain Rush: 900 in Racer
                var sg = new GameObject("Overcast horizon"); sg.transform.SetParent(transform, false); sg.AddComponent<MeshFilter>().sharedMesh = sm;
                var sr = sg.AddComponent<MeshRenderer>(); sr.sharedMaterial = skirtMaterial; sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; sr.receiveShadows = false;
                sr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; sr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                skirt = sg.transform; sg.SetActive(false);
            }
            // Shuffle which slots fill in first so the scattered set covers the whole sky.
            for (int i = Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (home[i], home[j]) = (home[j], home[i]); (size[i], size[j]) = (size[j], size[i]); }
        }
        // One cluster: 4-7 flattened low-poly puffs (subdivided icosahedra), flat-shaded, flat underside. Vertex colour red
        // carries the height in the cluster (shader darkens the base).
        static Mesh Cluster(System.Random rng)
        {
            var verts = new List<Vector3>(); var cols = new List<Color>(); var tris = new List<int>();
            int puffs = 4 + rng.Next(4);
            var (bv, bt) = Ico();
            for (int k = 0; k < puffs; k++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2, r = k == 0 ? 0 : Mathf.Lerp(.25f, .55f, (float)rng.NextDouble());
                var c = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r * .7f); float s = k == 0 ? .5f : Mathf.Lerp(.28f, .42f, (float)rng.NextDouble());
                var squash = new Vector3(s, s * Mathf.Lerp(.55f, .75f, (float)rng.NextDouble()), s * .9f);
                for (int t = 0; t < bt.Count; t += 3)
                {
                    for (int e = 0; e < 3; e++)
                    {
                        var p = c + Vector3.Scale(bv[bt[t + e]], squash); p.y = Mathf.Max(p.y, -.12f); verts.Add(p);
                        cols.Add(new Color(Mathf.InverseLerp(-.12f, .5f, p.y), 0, 0, 1)); tris.Add(verts.Count - 1);
                    }
                }
            }
            var m = new Mesh { name = "Cloud cluster" }; m.SetVertices(verts); m.SetColors(cols); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
        static (List<Vector3>, List<int>) Ico()
        {
            float t = (1 + Mathf.Sqrt(5)) / 2;
            var v = new List<Vector3> { new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0), new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t), new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1) };
            for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized;
            var f = new List<int> { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
            // One subdivision (80 faces) with a little jitter for a hand-made, faceted look.
            var cache = new Dictionary<long, int>(); var o = new List<int>(); var rng = new System.Random(11);
            int Mid(int a, int b) { long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a; if (cache.TryGetValue(key, out int i)) return i; v.Add(((v[a] + v[b]) * .5f).normalized * (1 + ((float)rng.NextDouble() - .5f) * .12f)); return cache[key] = v.Count - 1; }
            for (int i = 0; i < f.Count; i += 3) { int a = f[i], b = f[i + 1], c = f[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a); o.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca }); }
            return (v, o);
        }
        void LateUpdate()
        {
            var look = WorldLook.Current; var cam = Camera.main; if (!material || !look || look.Preset == null || !cam) return;
            var p = look.Preset;
            float overcast = Mathf.Clamp01(Mathf.Max(p.rain, p.snowfall));
            // Colour: white fair-weather, dark grey rain, pale even snow overcast.
            var colour = Color.Lerp(Color.white, new Color(.42f, .44f, .48f), Mathf.Clamp01(p.rain)); colour = Color.Lerp(colour, new Color(.88f, .9f, .94f), Mathf.Clamp01(p.snowfall));
            material.SetColor("_Color", colour); material.SetFloat("_Shade", Mathf.Lerp(.32f, p.rain > .5f ? .45f : .2f, overcast));
            material.SetColor("_Haze", RenderSettings.fogColor); material.SetFloat("_HazeAmount", Mathf.Lerp(.42f, .62f, overcast));
            material.SetFloat("_Flash", Flash);
            int visible = Mathf.RoundToInt(Mathf.Lerp(22, Count, overcast)); float grow = Mathf.Lerp(1, 3.6f, overcast); Visible = 0;
            var c = cam.transform.position; var drift = Wind * Time.time;
            bool deckOn = overcast > .3f; if (deck.gameObject.activeSelf != deckOn) deck.gameObject.SetActive(deckOn);
            if (deckOn) { deck.position = new Vector3(c.x, DeckHeight, c.z); deck.localScale = new Vector3(DeckRadius, 1, DeckRadius); }
            if (skirt.gameObject.activeSelf != deckOn) skirt.gameObject.SetActive(deckOn);
            if (deckOn)
            {
                float bottom = c.y - 250; skirt.position = new Vector3(c.x, bottom, c.z); skirt.localScale = new Vector3(DeckRadius, DeckHeight - bottom, DeckRadius);
                skirtMaterial.SetColor("_Haze", RenderSettings.fogColor); skirtMaterial.SetFloat("_Flash", Flash);
                material.SetFloat("_HazeDistance", 660); material.SetFloat("_HazeAmount", Mathf.Lerp(.42f, .8f, overcast));   // Chastain Rush: 660 (1650 in Racer)
            }
            else material.SetFloat("_HazeDistance", 690);   // Chastain Rush: 1700 in Racer
            for (int i = 0; i < Count; i++)
            {
                var t = clusters[i]; bool on = i < visible;
                if (!on) { if (t.gameObject.activeSelf) t.gameObject.SetActive(false); continue; }
                float x = Mathf.Repeat(home[i].x + drift.x - c.x + Tile / 2, Tile) - Tile / 2, z = Mathf.Repeat(home[i].z + drift.z - c.z + Tile / 2, Tile) - Tile / 2;
                float d = Mathf.Sqrt(x * x + z * z), fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(Fade0, Fade1, d));
                if (fade <= .01f) { if (t.gameObject.activeSelf) t.gameObject.SetActive(false); continue; }
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                // Overcast lowers and flattens the layer and swells the clusters (about 3.5x) so they close up into an even ceiling.
                float y = Mathf.Lerp(home[i].y, Base + 10 + (home[i].y - Base) * .3f, overcast);
                t.position = new Vector3(c.x + x, y, c.z + z);
                float s = size[i] * grow * fade; t.localScale = new Vector3(s, s * Mathf.Lerp(1, .45f, overcast), s);
                t.rotation = Quaternion.Euler(0, i * 47.3f, 0);
                Visible++;
            }
        }
        void OnDestroy() { foreach (var m in meshes) if (m) Destroy(m); if (material) Destroy(material); if (skirtMaterial) Destroy(skirtMaterial); }
    }
}
