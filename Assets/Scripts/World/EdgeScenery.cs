using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChastainRush
{
    // Round 66 (Dan, 2026-10-24: "Speaking of edges, how can I handle this on my map especially now I have helicopters"; "And I like the idea of
    // Atlanta in the distance"): the world past the map's edge, to look at only (no colliders, roads or traffic). Made at run time from
    // Resources/Scenery/edge_scenery.bytes (Tools/mapgen/make_scenery.py: USGS 3DEP heights, NLCD land cover):
    //   the ring  - land 9 km wide past every edge: ground coloured by its cover, building blocks where it is built up, a carpet of trees drawn
    //               with the map's own far tree shapes (TreeField's kit: its far crowns near, its eight-sided shapes far), all in the scene's fog;
    //   the horizon (only while the view is open: FarView) - the land out to 70 km and the towers of Atlanta, Buckhead, Cumberland and Perimeter
    //               Center (generic, no names), on layer Horizon, drawn past the opened fog's end where everything else has gone to haze: the
    //               plain is the fog's own colour, the ridges and the towers show as silhouettes, faint or gone in rain, snow and mist, the
    //               towers' windows lit at night (ChastainRush/Horizon). The camera's far clip opens to 90 km for that layer only (FarView sets
    //               every other layer's cull distance to the world's own far clip).
    // Also the land's low point round any place (RegionalLow): FarView opens the view by the camera's height over it as well as over the ground,
    // so the view opens on the summit. World, not game layer. Check runs: -sceneryOff leaves it out (the frame rate before).
    [DefaultExecutionOrder(1010)]
    public sealed class EdgeScenery : MonoBehaviour
    {
        public const int HorizonLayer = 8; public const float HorizonFar = 90000f, ClearVis = 42000f;
        public static EdgeScenery Instance { get; private set; }
        public static bool HorizonOn { get; private set; }   // drawn this frame
        public static Rect Map { get; private set; }
        public static Rect[] Land { get; private set; }   // Round 76: the land's rectangles (Map is the rectangle round them)
        public int Trees { get; private set; }
        public int Buildings { get; private set; }
        public int Towers { get; private set; }
        public int TreesDrawn { get; private set; }
        float vs = 1.5f;
        // the horizon grid (heights, the land's low point within 3 km)
        float[] fxs, fzs; short[] flow; int fnx, fnz;
        readonly List<Renderer> horizon = new List<Renderer>();
        Material ringMat, horizonMat;
        TreeField kit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<EdgeScenery>() || Terrain.activeTerrains.Length == 0) return;
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-sceneryOff") >= 0) return;
            if (Resources.Load<TextAsset>("Scenery/edge_scenery") == null) return;
            new GameObject("Edge scenery").AddComponent<EdgeScenery>();
        }

        void Awake()
        {
            Instance = this;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var asset = Resources.Load<TextAsset>("Scenery/edge_scenery");
            ringMat = Resources.Load<Material>("Scenery/SceneryRing"); horizonMat = Resources.Load<Material>("Scenery/SceneryHorizon");
            if (!asset || !ringMat || !horizonMat) { Debug.LogWarning("[Round66] edge scenery: data or materials missing"); enabled = false; return; }
            kit = FindAnyObjectByType<TreeField>();
            using (var r = new BinaryReader(new MemoryStream(asset.bytes)))
            {
                int ver = new string(r.ReadChars(4)) == "CRSC" ? r.ReadInt32() : 0;
                if (ver != 1 && ver != 2) { Debug.LogWarning("[Round66] edge scenery: unknown data"); enabled = false; return; }
                float x0 = r.ReadSingle(), z0 = r.ReadSingle(), x1 = r.ReadSingle(), z1 = r.ReadSingle(); vs = r.ReadSingle();
                Map = Rect.MinMaxRect(x0, z0, x1, z1);
                // Round 76 (A): version 2 lists the land's rectangles (the map and Marietta Square's piece); the ring is left out only under them
                var land = new List<Rect>();
                if (ver >= 2) { int n = r.ReadInt32(); for (int i = 0; i < n; i++) { float a = r.ReadSingle(), b = r.ReadSingle(), c = r.ReadSingle(), d = r.ReadSingle(); land.Add(Rect.MinMaxRect(a, b, c, d)); } }
                else land.Add(Map);
                Land = land.ToArray();
                ReadRing(r); ReadHorizon(r); ReadTrees(r); ReadBuildings(r); ReadTowers(r);
            }
            Shader.SetGlobalFloat("_HorizonNear", 1e9f);
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            Debug.Log($"[Round66] edge scenery: ring {ringCells} cells, {Trees} trees in {blocks.Count} blocks, {Buildings} buildings; horizon {fnx} x {fnz}, {Towers} towers; made in {watch.ElapsedMilliseconds} ms");
        }
        void OnDestroy() { RenderPipelineManager.beginCameraRendering -= OnBeginCamera; if (Instance == this) { Instance = null; HorizonOn = false; } Shader.SetGlobalFloat("_HorizonNear", 1e9f); }

        // ------------------------------------------------------------ the ring's ground

        static readonly Dictionary<int, Color> Cover = new Dictionary<int, Color>
        {
            { 11, new Color(.20f, .33f, .45f, 0f) }, { 12, new Color(.85f, .88f, .92f, 1f) }, { 21, new Color(.35f, .49f, .23f, 1f) }, { 22, new Color(.37f, .46f, .26f, 1f) },
            { 23, new Color(.44f, .45f, .39f, 1f) }, { 24, new Color(.44f, .44f, .43f, 1f) }, { 31, new Color(.48f, .44f, .37f, 1f) }, { 41, new Color(.19f, .30f, .14f, 1f) },
            { 42, new Color(.16f, .27f, .13f, 1f) }, { 43, new Color(.19f, .29f, .14f, 1f) }, { 52, new Color(.33f, .40f, .21f, 1f) }, { 71, new Color(.40f, .50f, .24f, 1f) },
            { 81, new Color(.44f, .53f, .24f, 1f) }, { 82, new Color(.48f, .47f, .27f, 1f) }, { 90, new Color(.22f, .32f, .18f, 1f) }, { 95, new Color(.30f, .41f, .25f, 1f) },
        };
        int ringCells;
        void ReadRing(BinaryReader r)
        {
            int nx = r.ReadInt32(), nz = r.ReadInt32();
            var xs = Floats(r, nx); var zs = Floats(r, nz);
            var h = Shorts(r, nx * nz); var cov = r.ReadBytes(nx * nz);
            bool On(float x, float z) { foreach (var m in Land) if (x >= m.xMin && x <= m.xMax && z >= m.yMin && z <= m.yMax) return true; return false; }
            bool Inside(float x, float z) => On(x + .5f, z) && On(x - .5f, z) && On(x, z + .5f) && On(x, z - .5f);   // (a point where two rectangles meet is inside)
            const int C = 64;
            for (int ck = 0; ck < nz - 1; ck += C)
                for (int ci = 0; ci < nx - 1; ci += C)
                {
                    int cw = Mathf.Min(C, nx - 1 - ci), ch = Mathf.Min(C, nz - 1 - ck);
                    var tris = new List<int>();
                    for (int k = 0; k < ch; k++)
                        for (int i = 0; i < cw; i++)
                        {
                            int gi = ci + i, gk = ck + k;
                            if (Inside(xs[gi], zs[gk]) && Inside(xs[gi + 1], zs[gk]) && Inside(xs[gi], zs[gk + 1]) && Inside(xs[gi + 1], zs[gk + 1])) continue;
                            int a = k * (cw + 1) + i, b = a + 1, c = a + cw + 1, d = c + 1;
                            tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d); ringCells++;
                        }
                    if (tris.Count == 0) continue;
                    var v = new Vector3[(cw + 1) * (ch + 1)]; var n = new Vector3[v.Length]; var col = new Color[v.Length];
                    for (int k = 0; k <= ch; k++)
                        for (int i = 0; i <= cw; i++)
                        {
                            int gi = ci + i, gk = ck + k, at = gk * nx + gi, o = k * (cw + 1) + i;
                            v[o] = new Vector3(xs[gi], h[at] * .1f, zs[gk]);
                            int il = Mathf.Max(0, gi - 1), ir = Mathf.Min(nx - 1, gi + 1), kd = Mathf.Max(0, gk - 1), ku = Mathf.Min(nz - 1, gk + 1);
                            float dx = (h[gk * nx + ir] - h[gk * nx + il]) * .1f / Mathf.Max(1f, xs[ir] - xs[il]), dz = (h[ku * nx + gi] - h[kd * nx + gi]) * .1f / Mathf.Max(1f, zs[ku] - zs[kd]);
                            n[o] = new Vector3(-dx, 1f, -dz).normalized;
                            col[o] = Cover.TryGetValue(cov[at], out var c) ? c : Cover[21];
                        }
                    Part("Edge scenery ground", v, n, col, null, null, tris, ringMat, 0);
                }
        }

        GameObject Part(string name, Vector3[] v, Vector3[] n, Color[] col, Vector2[] uv, Vector2[] uv1, List<int> tris, Material mat, int layer)
        {
            var mesh = new Mesh { name = name, indexFormat = v.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetColors(col); if (uv != null) mesh.SetUVs(0, uv); if (uv1 != null) mesh.SetUVs(1, uv1);
            mesh.SetTriangles(tris, 0); mesh.RecalculateBounds(); mesh.UploadMeshData(true);
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.layer = layer; go.isStatic = false;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            if (layer == HorizonLayer) { horizon.Add(mr); mr.enabled = false; }
            return go;
        }

        // ------------------------------------------------------------ the horizon's land

        void ReadHorizon(BinaryReader r)
        {
            fnx = r.ReadInt32(); fnz = r.ReadInt32(); fxs = Floats(r, fnx); fzs = Floats(r, fnz);
            var h = Shorts(r, fnx * fnz); var prom = r.ReadBytes(fnx * fnz); flow = Shorts(r, fnx * fnz);
            const int C = 64;
            for (int ck = 0; ck < fnz - 1; ck += C)
                for (int ci = 0; ci < fnx - 1; ci += C)
                {
                    int cw = Mathf.Min(C, fnx - 1 - ci), ch = Mathf.Min(C, fnz - 1 - ck);
                    var v = new Vector3[(cw + 1) * (ch + 1)]; var n = new Vector3[v.Length]; var col = new Color[v.Length]; var tris = new List<int>(cw * ch * 6);
                    for (int k = 0; k <= ch; k++)
                        for (int i = 0; i <= cw; i++)
                        {
                            int gi = ci + i, gk = ck + k, at = gk * fnx + gi, o = k * (cw + 1) + i;
                            v[o] = new Vector3(fxs[gi], h[at] * .1f, fzs[gk]);
                            int il = Mathf.Max(0, gi - 1), ir = Mathf.Min(fnx - 1, gi + 1), kd = Mathf.Max(0, gk - 1), ku = Mathf.Min(fnz - 1, gk + 1);
                            float dx = (h[gk * fnx + ir] - h[gk * fnx + il]) * .1f / (fxs[ir] - fxs[il]), dz = (h[ku * fnx + gi] - h[kd * fnx + gi]) * .1f / (fzs[ku] - fzs[kd]);
                            n[o] = new Vector3(-dx, 1f, -dz).normalized; col[o] = new Color(.5f, .5f, .5f, prom[at] / 255f);
                            if (k < ch && i < cw) { int a = o, b = a + 1, c = a + cw + 1, d = c + 1; tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d); }
                        }
                    Part("Horizon land", v, n, col, null, null, tris, horizonMat, HorizonLayer);
                }
        }

        // the land's lowest point within about 3 km (null outside the horizon grid)
        public static bool RegionalLow(float x, float z, out float low)
        {
            low = 0f; var s = Instance; if (!s || s.flow == null) return false;
            int i = Lower(s.fxs, x), k = Lower(s.fzs, z); if (i < 0 || k < 0) return false;
            float tx = Mathf.InverseLerp(s.fxs[i], s.fxs[i + 1], x), tz = Mathf.InverseLerp(s.fzs[k], s.fzs[k + 1], z);
            int n = s.fnx; float a = s.flow[k * n + i], b = s.flow[k * n + i + 1], c = s.flow[(k + 1) * n + i], d = s.flow[(k + 1) * n + i + 1];
            low = Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz) * .1f; return true;
        }
        static int Lower(float[] a, float x)
        {
            if (x < a[0] || x >= a[a.Length - 1]) return -1;
            int lo = 0, hi = a.Length - 1; while (hi - lo > 1) { int mid = (lo + hi) >> 1; if (a[mid] <= x) lo = mid; else hi = mid; }
            return lo;
        }

        // ------------------------------------------------------------ the towers

        void ReadTowers(BinaryReader r)
        {
            int count = r.ReadInt32(); Towers = count;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var col = new List<Color>(); var uv = new List<Vector2>(); var uv1 = new List<Vector2>(); var tris = new List<int>();
            var glass = new[] { new Color(.52f, .58f, .66f), new Color(.60f, .63f, .66f), new Color(.46f, .52f, .58f), new Color(.70f, .70f, .68f), new Color(.56f, .62f, .70f) };
            for (int t = 0; t < count; t++)
            {
                float x = r.ReadSingle(), z = r.ReadSingle(), y = r.ReadSingle() - 6f, w = r.ReadSingle(), d = r.ReadSingle(), hgt = r.ReadSingle(), yaw = r.ReadSingle(); int top = r.ReadInt32();
                var c = glass[t % glass.Length]; var rot = Quaternion.Euler(0f, yaw * 360f, 0f); var at = new Vector3(x, y, z); float H = hgt + 6f;
                void Box(float hw, float hd, float y0, float y1, int sides)
                {
                    var corners = new Vector3[sides]; float per = 0f;
                    for (int s = 0; s < sides; s++)
                    {
                        float a = (s + .5f) / sides * Mathf.PI * 2f;
                        corners[s] = sides == 4 ? new Vector3((s == 0 || s == 3 ? 1 : -1) * hw, 0f, (s < 2 ? 1 : -1) * hd) : new Vector3(Mathf.Cos(a) * hw, 0f, Mathf.Sin(a) * hd);
                    }
                    for (int s = 0; s < sides; s++)
                    {
                        var p = corners[s]; var q = corners[(s + 1) % sides]; float len = (q - p).magnitude; var nn = rot * Vector3.Cross(Vector3.up, q - p).normalized;   // outward
                        int b = v.Count;
                        foreach (var (pt, yy, u) in new[] { (p, y0, per), (q, y0, per + len), (q, y1, per + len), (p, y1, per) })
                        { v.Add(at + rot * pt + Vector3.up * yy); n.Add(nn); col.Add(c); uv.Add(new Vector2(u, yy)); uv1.Add(new Vector2(1f, H)); }
                        tris.Add(b); tris.Add(b + 2); tris.Add(b + 1); tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
                        per += len;
                    }
                    int cb = v.Count;
                    for (int s = 0; s < sides; s++) { v.Add(at + rot * corners[s] + Vector3.up * y1); n.Add(Vector3.up); col.Add(c * .8f); uv.Add(new Vector2(0, y1)); uv1.Add(new Vector2(1f, H)); }
                    for (int s = 1; s < sides - 1; s++) { tris.Add(cb); tris.Add(cb + s + 1); tris.Add(cb + s); }
                }
                void Point(float hw, float hd, float y0, float y1)
                {
                    var apex = at + Vector3.up * y1; var cs = new[] { new Vector3(hw, 0, hd), new Vector3(-hw, 0, hd), new Vector3(-hw, 0, -hd), new Vector3(hw, 0, -hd) };
                    for (int s = 0; s < 4; s++)
                    {
                        var p = at + rot * cs[s] + Vector3.up * y0; var q = at + rot * cs[(s + 1) % 4] + Vector3.up * y0; var nn = Vector3.Cross(q - p, apex - p).normalized;
                        int b = v.Count; v.Add(p); v.Add(q); v.Add(apex); for (int k = 0; k < 3; k++) { n.Add(-nn); col.Add(c * .9f); uv.Add(new Vector2(0, y1)); uv1.Add(new Vector2(1f, H)); }
                        tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                    }
                }
                float hw0 = w * .5f, hd0 = d * .5f;
                switch (top)
                {
                    case 1: Box(hw0, hd0, 0, H * .86f, 4); Point(hw0, hd0, H * .86f, H); break;
                    case 2: Box(hw0, hd0, 0, H * .84f, 4); Box(hw0 * .55f, hd0 * .55f, H * .84f, H * .9f, 4); Point(hw0 * .12f, hd0 * .12f, H * .9f, H); break;
                    case 3: Box(hw0, hd0, 0, H * .66f, 4); Box(hw0 * .72f, hd0 * .72f, H * .66f, H * .86f, 4); Box(hw0 * .45f, hd0 * .45f, H * .86f, H, 4); break;
                    case 4: Box(hw0, hd0, 0, H, 8); break;
                    default: Box(hw0, hd0, 0, H, 4); break;
                }
            }
            if (v.Count > 0) Part("Horizon towers", v.ToArray(), n.ToArray(), col.ToArray(), uv.ToArray(), uv1.ToArray(), tris, horizonMat, HorizonLayer);
        }

        // ------------------------------------------------------------ the ring's buildings

        static readonly Color[] Walls =
        {
            new Color(.85f, .82f, .75f), new Color(.78f, .72f, .62f), new Color(.70f, .62f, .55f), new Color(.88f, .86f, .82f),
            new Color(.62f, .58f, .52f), new Color(.80f, .76f, .70f), new Color(.72f, .70f, .66f), new Color(.84f, .78f, .68f),
        };
        void ReadBuildings(BinaryReader r)
        {
            int count = r.ReadInt32(); Buildings = count;
            var chunks = new Dictionary<Vector2Int, (List<Vector3> v, List<Vector3> n, List<Color> c, List<int> t)>();
            for (int b = 0; b < count; b++)
            {
                float x = r.ReadSingle(), z = r.ReadSingle(), y = r.ReadSingle() - .6f; float w = r.ReadByte() * .25f, d = r.ReadByte() * .25f, h = r.ReadByte() * .25f, yaw = r.ReadByte() / 255f * 360f; int kind = r.ReadByte(), ci = r.ReadByte();
                var key = new Vector2Int(Mathf.FloorToInt(x / 2000f), Mathf.FloorToInt(z / 2000f));
                if (!chunks.TryGetValue(key, out var ch) || ch.v.Count > 60000) { if (ch.v != null && ch.v.Count > 60000) Flush(ch); chunks[key] = ch = (new List<Vector3>(), new List<Vector3>(), new List<Color>(), new List<int>()); }
                var rot = Quaternion.Euler(0, yaw, 0); var at = new Vector3(x, y, z); var wall = Walls[ci % Walls.Length];
                var roof = kind == 0 ? (ci % 2 == 0 ? new Color(.30f, .29f, .29f, 1f) : new Color(.42f, .31f, .25f, 1f)) : new Color(.60f, .60f, .58f, 1f);
                float wallH = kind == 0 ? h * .58f : h;
                var cs = new[] { new Vector3(w * .5f, 0, d * .5f), new Vector3(-w * .5f, 0, d * .5f), new Vector3(-w * .5f, 0, -d * .5f), new Vector3(w * .5f, 0, -d * .5f) };
                for (int s = 0; s < 4; s++)
                {
                    var p = at + rot * cs[s]; var q = at + rot * cs[(s + 1) % 4]; var nn = Vector3.Cross(Vector3.up, q - p).normalized; int o = ch.v.Count;
                    ch.v.Add(p); ch.v.Add(q); ch.v.Add(q + Vector3.up * wallH); ch.v.Add(p + Vector3.up * wallH);
                    for (int k = 0; k < 4; k++) { ch.n.Add(nn); ch.c.Add(new Color(wall.r, wall.g, wall.b, 0f)); }
                    ch.t.Add(o); ch.t.Add(o + 2); ch.t.Add(o + 1); ch.t.Add(o); ch.t.Add(o + 3); ch.t.Add(o + 2);
                }
                if (kind == 0)
                {
                    // a gable roof along the longer side
                    bool alongX = w >= d; float rise = h - wallH;
                    Vector3 P(float lx, float ly, float lz) => at + rot * new Vector3(lx, ly, lz);
                    float a = (alongX ? w : d) * .5f + .4f, bHalf = (alongX ? d : w) * .5f + .4f;
                    Vector3 L(float u, float vv, float yy) => alongX ? P(u, yy, vv) : P(vv, yy, u);
                    var r0 = L(-a, 0, wallH + rise); var r1 = L(a, 0, wallH + rise);
                    foreach (int side in new[] { 1, -1 })
                    {
                        var e0 = L(-a, side * bHalf, wallH - .3f); var e1 = L(a, side * bHalf, wallH - .3f);
                        var nn = Vector3.Cross(e1 - e0, r0 - e0).normalized; if (nn.y < 0) nn = -nn; int o = ch.v.Count;
                        ch.v.Add(e0); ch.v.Add(e1); ch.v.Add(r1); ch.v.Add(r0); for (int k = 0; k < 4; k++) { ch.n.Add(nn); ch.c.Add(roof); }
                        ch.t.Add(o); ch.t.Add(o + 1); ch.t.Add(o + 2); ch.t.Add(o); ch.t.Add(o + 2); ch.t.Add(o + 3);
                        ch.t.Add(o); ch.t.Add(o + 2); ch.t.Add(o + 1); ch.t.Add(o); ch.t.Add(o + 3); ch.t.Add(o + 2);   // both ways (the slope's facing depends on the yaw)
                    }
                    foreach (int end in new[] { 1, -1 })
                    {
                        var g0 = L(end * (a - .4f), bHalf - .4f, wallH); var g1 = L(end * (a - .4f), -(bHalf - .4f), wallH); var g2 = L(end * (a - .4f), 0, wallH + rise);
                        var nn = (alongX ? rot * Vector3.right : rot * Vector3.forward) * end; int o = ch.v.Count;
                        ch.v.Add(g0); ch.v.Add(g1); ch.v.Add(g2); for (int k = 0; k < 3; k++) { ch.n.Add(nn); ch.c.Add(new Color(wall.r, wall.g, wall.b, 0f)); }
                        ch.t.Add(o); ch.t.Add(o + 1); ch.t.Add(o + 2); ch.t.Add(o); ch.t.Add(o + 2); ch.t.Add(o + 1);
                    }
                }
                else
                {
                    int o = ch.v.Count; for (int s = 0; s < 4; s++) { ch.v.Add(at + rot * cs[s] + Vector3.up * wallH); ch.n.Add(Vector3.up); ch.c.Add(roof); }
                    ch.t.Add(o); ch.t.Add(o + 2); ch.t.Add(o + 1); ch.t.Add(o); ch.t.Add(o + 3); ch.t.Add(o + 2);
                }
            }
            foreach (var ch in chunks.Values) Flush(ch);
        }
        void Flush((List<Vector3> v, List<Vector3> n, List<Color> c, List<int> t) ch) { if (ch.v.Count > 0) Part("Edge scenery buildings", ch.v.ToArray(), ch.n.ToArray(), ch.c.ToArray(), null, null, ch.t, ringMat, 0); }

        // ------------------------------------------------------------ the ring's trees (TreeField's kit, drawn as TreeField draws its far trees)

        sealed class Block { public Bounds b; public bool any; public List<Matrix4x4>[] nearL = L(), farL = L(), trunkL = L(); public Matrix4x4[][] near, thin4, thin12, far, trunk; }
        static List<Matrix4x4>[] L() { var l = new List<Matrix4x4>[TreeField.FarClasses]; for (int i = 0; i < l.Length; i++) l[i] = new List<Matrix4x4>(); return l; }
        readonly List<Block> blocks = new List<Block>();
        static readonly float[] BaseRadius = { 4.2f, 5.5f, 4.5f, 2.8f, 3.0f, 2.2f }, BaseHeight = { 15f, 18f, 16f, 23f, 22f, 8f }, TrunkFraction = { 0.62f, 0.62f, 0.62f, 0.45f, 0.60f, 0.62f };   // TreeBuilder's
        void ReadTrees(BinaryReader r)
        {
            int count = r.ReadInt32(); Trees = count; var keys = new Dictionary<Vector2Int, Block>();
            for (int t = 0; t < count; t++)
            {
                float x = r.ReadInt16(), z = r.ReadInt16(), y = r.ReadInt16() * .1f; int fc = Mathf.Min(r.ReadByte(), TreeField.FarClasses - 1); float f = r.ReadByte() * .01f; bool near = r.ReadByte() != 0; r.ReadByte();
                var key = new Vector2Int(Mathf.FloorToInt(x / 1000f), Mathf.FloorToInt(z / 1000f));
                if (!keys.TryGetValue(key, out var bl)) { keys[key] = bl = new Block(); blocks.Add(bl); }
                int s = TreeField.FarMeshOf[fc]; float hash = Mathf.Repeat(x * 0.618f + z * 0.371f, 1f);
                float height = BaseHeight[s] * f * vs, radius = BaseRadius[s] * f, trunkH = Mathf.Min(25.5f, height * TrunkFraction[s]), crownBase = trunkH * .5f, crownH = height - crownBase;
                var rot = Quaternion.Euler(0, hash * 360f, 0); var pos = new Vector3(x, y, z);
                Matrix4x4 crown;
                if (near) { crown = Matrix4x4.TRS(pos + Vector3.up * crownBase, rot, new Vector3(radius * 1.15f, crownH, radius * 1.15f)); bl.nearL[fc].Add(crown); bl.trunkL[fc].Add(Matrix4x4.TRS(pos, rot, new Vector3(.18f + .018f * BaseHeight[s] * f, trunkH, .18f + .018f * BaseHeight[s] * f))); }
                else { crown = Matrix4x4.TRS(pos + Vector3.up * crownBase * .6f, rot, new Vector3(radius * 2.4f, crownH * 1.3f, radius * 2.4f)); bl.farL[fc].Add(crown); }   // one per 30 m, drawn larger
                var bb = new Bounds(pos + Vector3.up * height * .5f, new Vector3(radius * 7f, height * 1.4f, radius * 7f));
                if (bl.any) bl.b.Encapsulate(bb); else { bl.b = bb; bl.any = true; }
            }
            var grow4 = Matrix4x4.Scale(new Vector3(1.6f, 1.15f, 1.6f)); var grow12 = Matrix4x4.Scale(new Vector3(2.1f, 1.25f, 2.1f));   // (past ThinMore every 6th, not TreeField's 12th: the ring's near band is sparser than the map's woods)
            foreach (var bl in blocks)
            {
                int K = TreeField.FarClasses; bl.near = new Matrix4x4[K][]; bl.thin4 = new Matrix4x4[K][]; bl.thin12 = new Matrix4x4[K][]; bl.far = new Matrix4x4[K][]; bl.trunk = new Matrix4x4[K][];
                for (int k = 0; k < K; k++)
                {
                    bl.near[k] = bl.nearL[k].ToArray(); bl.far[k] = bl.farL[k].ToArray(); bl.trunk[k] = bl.trunkL[k].ToArray();
                    var t4 = new List<Matrix4x4>(); var t12 = new List<Matrix4x4>();
                    for (int i = 0; i < bl.near[k].Length; i++) { if (i % 4 == 0) t4.Add(bl.near[k][i] * grow4); if (i % 6 == 0) t12.Add(bl.near[k][i] * grow12); }
                    bl.thin4[k] = t4.ToArray(); bl.thin12[k] = t12.ToArray();
                }
                bl.nearL = bl.farL = bl.trunkL = null;
            }
        }

        readonly Plane[] planes = new Plane[6];
        void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (!kit || !kit.material || kit.crownFar == null || cam.orthographic || (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView)) return;
            var eye = cam.transform.position; float world = cam == Camera.main ? FarView.WorldFar : cam.farClipPlane;
            float draw = Mathf.Min(Mathf.Max(kit.drawDistance, TreeField.FarDraw), world);
            GeometryUtility.CalculateFrustumPlanes(cam, planes);
            var rp = new RenderParams(kit.material) { camera = cam, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true, layer = 0 };
            int drawn = 0;
            foreach (var bl in blocks)
            {
                float d = Mathf.Sqrt(bl.b.SqrDistance(eye));
                if (d > draw || !GeometryUtility.TestPlanesAABB(planes, bl.b)) continue;
                var p = rp; p.worldBounds = bl.b;
                for (int k = 0; k < TreeField.FarClasses; k++)
                {
                    if (d < kit.drawDistance)
                    {
                        var crown = kit.crownFar[TreeField.FarMeshOf[k]];
                        drawn += Draw(p, crown, bl.near[k]) + Draw(p, crown, bl.far[k]);
                        if (d < kit.farTrunkDistance) Draw(p, kit.trunkFar[TreeField.FarMeshOf[k]], bl.trunk[k]);
                    }
                    else
                    {
                        var m8 = kit.Far8(k); if (!m8) continue;
                        drawn += Draw(p, m8, d < TreeField.ThinFrom ? bl.near[k] : d < TreeField.ThinMore ? bl.thin4[k] : bl.thin12[k]) + Draw(p, m8, bl.far[k]);
                    }
                }
            }
            if (cam == Camera.main) TreesDrawn = drawn;
        }
        static int Draw(RenderParams rp, Mesh mesh, Matrix4x4[] list)
        {
            if (!mesh || list == null) return 0;
            for (int start = 0; start < list.Length; start += 1000) Graphics.RenderMeshInstanced(rp, mesh, 0, list, Mathf.Min(1000, list.Length - start), start);
            return list.Length;
        }

        // ------------------------------------------------------------ every frame: the horizon on or off, the haze

        void LateUpdate()
        {
            bool on = FarView.Opened && !SplitScreen.Active;
            if (on != HorizonOn) { HorizonOn = on; foreach (var r in horizon) if (r) r.enabled = on; }
            var look = Racer.WorldLook.Current; var p = look ? look.Preset : null;
            float vis = ClearVis, night = 0f;
            if (p != null) { vis *= (1f - .85f * Mathf.Clamp01(p.rain)) * (1f - .8f * Mathf.Clamp01(p.snowfall)) * (1f - .5f * Mathf.Clamp01(p.mist)); night = Mathf.Clamp01(p.lights); }
            Shader.SetGlobalFloat("_HorizonNear", on ? RenderSettings.fogEndDistance : 1e9f);
            Shader.SetGlobalFloat("_HorizonVis", vis); Shader.SetGlobalFloat("_HorizonNight", night);
        }

        static float[] Floats(BinaryReader r, int n) { var a = new float[n]; for (int i = 0; i < n; i++) a[i] = r.ReadSingle(); return a; }
        static short[] Shorts(BinaryReader r, int n) { var a = new short[n]; for (int i = 0; i < n; i++) a[i] = r.ReadInt16(); return a; }
    }
}
