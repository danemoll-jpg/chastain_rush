using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChastainRush
{
    // Draws the map's trees: tens of thousands of instances of the Racer tree kit (see Editor/TreeKit.cs), by GPU
    // instancing, nothing else. Same scheme as Racer's SceneryTrees: near trees use the detailed kit and cast shadows,
    // far trees the simplified kit with no shadows (and no trunks, which the crowns hide), and nothing is drawn past
    // DrawDistance. Differences: the positions come from map data (Data/trees.bin) and are packed into this component
    // when the scene is built, and instead of rebuilding lists every frame the trees are sorted once into 48 m cells
    // that sit inside 192 m far cells, so a frame costs a few dozen frustum tests and one instanced draw per mesh per cell.
    // Trees have no colliders (visual only; TreeTrunks gives the nearest ones trunk colliders).
    // Round 19 (item 1): the trees listed in 'breakable' (beside the Noonday Creek trail) can be hidden one by one when they snap
    // (TreeTrunks) and put back when a race starts: their instance matrices are swapped for a zero-size one in every list they are in.
    [DisallowMultipleComponent]
    public sealed class TreeField : MonoBehaviour
    {
        public const int Species = 6; // Round, Oak, Maple, Pine, Poplar, Young
        public static readonly string[] SpeciesNames = { "Round", "Oak", "Maple", "Pine", "Poplar", "Young" };

        // Far away the kit is reduced to three shapes (broadleaf, pine, poplar), so a far cell costs 3 instanced draws, not 12
        // (the draw calls, not the triangles, were what the first version spent its frame time on).
        public const int FarClasses = 3;
        static readonly int[] FarClassOf = { 0, 0, 0, 1, 2, 0 };   // species -> far class
        static readonly int[] FarMeshOf = { 0, 3, 4 };              // far class -> species whose far mesh is used

        public const float NearCell = 48f, FarCell = 256f;
        public float nearDistance = 55f, farTrunkDistance = 160f, drawDistance = 700f;
        // Round 63 (the view from the air, World/FarView.cs): when FarDraw is set (the opened view's distance), trees past drawDistance are drawn out to
        // it as one eight-sided shape each (Far: the far crown's bounds, its average colour), three draws per 256 m group
        public static float FarDraw;
        Mesh[] far8;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Packed
        {
            public float x, y, z;     // base of the trunk, world metres
            public ushort yaw;        // 0..65535 = 0..360 degrees
            public ushort crownH;     // crown height, centimetres
            public byte species;
            public byte radius;       // crown radius in 5 cm units
            public byte trunkH;       // trunk height in 10 cm units (0 to 25.5 m)
            public byte trunkW;       // trunk diameter in 1 cm units
        }
        public const int PackedSize = 20;

        [SerializeField] public Mesh[] crownNear, crownFar, trunkNear, trunkFar;
        [SerializeField] public Material material;
        [SerializeField] public byte[] data;
        [SerializeField] public int count;
        [SerializeField] public int[] breakable;   // Round 19: tree indices that snap when hit (MapBuilder)

        sealed class Cell
        {
            public Bounds bounds; public bool any;
            public readonly List<Matrix4x4>[] crown = new List<Matrix4x4>[Species], trunk = new List<Matrix4x4>[Species];
            public Matrix4x4[][] crownA, trunkA;
            public readonly List<Matrix4x4>[] farCrown = new List<Matrix4x4>[FarClasses], farTrunk = new List<Matrix4x4>[FarClasses];
            public Matrix4x4[][] farCrownA, farTrunkA;
            public Cell()
            {
                for (int i = 0; i < Species; i++) { crown[i] = new List<Matrix4x4>(); trunk[i] = new List<Matrix4x4>(); }
                for (int i = 0; i < FarClasses; i++) { farCrown[i] = new List<Matrix4x4>(); farTrunk[i] = new List<Matrix4x4>(); }
            }
            public void Grow(Bounds b) { if (any) bounds.Encapsulate(b); else { bounds = b; any = true; } }
            public void Freeze()
            {
                crownA = new Matrix4x4[Species][]; trunkA = new Matrix4x4[Species][]; farCrownA = new Matrix4x4[FarClasses][]; farTrunkA = new Matrix4x4[FarClasses][];
                for (int i = 0; i < Species; i++) { crownA[i] = crown[i].ToArray(); trunkA[i] = trunk[i].ToArray(); }
                for (int i = 0; i < FarClasses; i++) { farCrownA[i] = farCrown[i].ToArray(); farTrunkA[i] = farTrunk[i].ToArray(); }
            }
        }
        sealed class FarGroup
        {
            public Bounds bounds; public bool any;
            public readonly List<Cell> sub = new List<Cell>();
            public readonly List<Matrix4x4>[] crown = new List<Matrix4x4>[FarClasses], trunk = new List<Matrix4x4>[FarClasses];
            public Matrix4x4[][] crownA, trunkA;
            public FarGroup() { for (int i = 0; i < FarClasses; i++) { crown[i] = new List<Matrix4x4>(); trunk[i] = new List<Matrix4x4>(); } }
        }

        readonly Dictionary<Vector2Int, Cell> cells = new Dictionary<Vector2Int, Cell>();
        // Round 19: where each breakable tree sits in the draw lists (cell, species and far class, its index in each list)
        sealed class Slot { public Cell c; public FarGroup g; public int s, fc, iNear, iCellFar, iGroup; public Matrix4x4 crown, trunk; public bool hidden; }
        readonly Dictionary<int, Slot> slots = new Dictionary<int, Slot>();
        readonly List<FarGroup> groups = new List<FarGroup>();
        readonly Plane[] planes = new Plane[6];
        bool ready;

        // what the last camera that was drawn saw (for the checks)
        public int LastDrawCalls { get; private set; }
        public int LastTreesDrawn { get; private set; }
        public long LastTriangles { get; private set; }
        public int TreeCount => count;
        public int[] PerSpecies { get; } = new int[Species];

        void OnEnable()
        {
            BuildCells();
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        }

        void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCamera;

        public static Packed Unpack(byte[] data, int i)
        {
            var span = new System.ReadOnlySpan<byte>(data, i * PackedSize, PackedSize);
            return MemoryMarshal.Read<Packed>(span);
        }

        void BuildCells()
        {
            cells.Clear(); groups.Clear(); ready = false;
            if (data == null || count == 0 || crownNear == null || material == null) return;
            System.Array.Clear(PerSpecies, 0, Species);
            var farKeys = new Dictionary<Vector2Int, FarGroup>();
            slots.Clear(); var isBreakable = new HashSet<int>(breakable ?? new int[0]);
            for (int i = 0; i < count; i++)
            {
                var t = Unpack(data, i);
                int s = Mathf.Min(t.species, Species - 1);
                PerSpecies[s]++;
                float radius = t.radius * 0.05f, crownH = t.crownH * 0.01f, trunkH = t.trunkH * 0.1f, trunkW = t.trunkW * 0.01f;
                var pos = new Vector3(t.x, t.y, t.z);
                var rot = Quaternion.Euler(0f, t.yaw * (360f / 65536f), 0f);
                float crownBase = trunkH * 0.5f; // the crown starts half way up the trunk (the kit's branches reach down into it)
                var crownM = Matrix4x4.TRS(pos + Vector3.up * crownBase, rot, new Vector3(radius, crownH, radius));
                var trunkM = Matrix4x4.TRS(pos, rot, new Vector3(trunkW, trunkH, trunkW));
                var b = new Bounds(pos + Vector3.up * (crownBase + crownH * 0.5f), new Vector3(radius * 2.4f, crownH + crownBase, radius * 2.4f));
                var key = new Vector2Int(Mathf.FloorToInt(t.x / NearCell), Mathf.FloorToInt(t.z / NearCell));
                if (!cells.TryGetValue(key, out var c)) cells[key] = c = new Cell();
                int fc = FarClassOf[s];
                var fk = new Vector2Int(Mathf.FloorToInt(t.x / FarCell), Mathf.FloorToInt(t.z / FarCell));
                if (!farKeys.TryGetValue(fk, out var g)) { farKeys[fk] = g = new FarGroup(); groups.Add(g); }
                if (isBreakable.Contains(i)) slots[i] = new Slot { c = c, g = g, s = s, fc = fc, iNear = c.crown[s].Count, iCellFar = c.farCrown[fc].Count, iGroup = g.crown[fc].Count, crown = crownM, trunk = trunkM };
                c.crown[s].Add(crownM); c.trunk[s].Add(trunkM); c.farCrown[fc].Add(crownM); c.farTrunk[fc].Add(trunkM); c.Grow(b);
                g.crown[fc].Add(crownM); g.trunk[fc].Add(trunkM);
                if (g.any) g.bounds.Encapsulate(b); else { g.bounds = b; g.any = true; }
            }
            foreach (var kv in cells)
            {
                kv.Value.Freeze();
                var fk = new Vector2Int(Mathf.FloorToInt(kv.Key.x * NearCell / FarCell), Mathf.FloorToInt(kv.Key.y * NearCell / FarCell));
                farKeys[fk].sub.Add(kv.Value);
            }
            foreach (var g in groups)
            {
                g.crownA = new Matrix4x4[FarClasses][]; g.trunkA = new Matrix4x4[FarClasses][];
                for (int i = 0; i < FarClasses; i++) { g.crownA[i] = g.crown[i].ToArray(); g.trunkA[i] = g.trunk[i].ToArray(); g.crown[i] = null; g.trunk[i] = null; }
            }
            ready = true;
        }

        // Round 19: hide or show one breakable tree (no-op for any other)
        public bool IsBreakable(int i) => slots.ContainsKey(i);
        public bool SetHidden(int i, bool hidden)
        {
            if (!slots.TryGetValue(i, out var sl) || sl.hidden == hidden) return false;
            sl.hidden = hidden;
            var cr = hidden ? Matrix4x4.zero : sl.crown; var tr = hidden ? Matrix4x4.zero : sl.trunk;
            sl.c.crownA[sl.s][sl.iNear] = cr; sl.c.trunkA[sl.s][sl.iNear] = tr;
            sl.c.farCrownA[sl.fc][sl.iCellFar] = cr; sl.c.farTrunkA[sl.fc][sl.iCellFar] = tr;
            sl.g.crownA[sl.fc][sl.iGroup] = cr; sl.g.trunkA[sl.fc][sl.iGroup] = tr;
            return true;
        }
        public void ShowAll() { foreach (var k in slots.Keys) SetHidden(k, false); }
        public int BreakableCount => slots.Count;

        void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (!ready || (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView)) return;
            int calls = 0, trees = 0; long tris = 0;
            var eye = cam.transform.position;
            bool all = cam.orthographic; // overhead views draw every tree, simplified
            float draw = all ? float.MaxValue : Mathf.Min(Mathf.Max(drawDistance, FarDraw), cam.farClipPlane);
            GeometryUtility.CalculateFrustumPlanes(cam, planes);
            var near = new RenderParams(material) { camera = cam, shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, layer = 0 };
            var far = new RenderParams(material) { camera = cam, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true, layer = 0 };
            foreach (var g in groups)
            {
                float d = Mathf.Sqrt(g.bounds.SqrDistance(eye));
                if (d > draw || (!all && !GeometryUtility.TestPlanesAABB(planes, g.bounds))) continue;
                if (all || d >= nearDistance)
                {
                    if (!all && d > drawDistance && FarDraw > 0f) { DrawFar8(far, g.crownA, ref calls, ref trees, ref tris); continue; }   // Round 63
                    DrawFar(far, g.crownA, g.trunkA, !all && d < farTrunkDistance, ref calls, ref trees, ref tris);
                    continue;
                }
                foreach (var c in g.sub)
                {
                    float sd = Mathf.Sqrt(c.bounds.SqrDistance(eye));
                    if (sd > draw || !GeometryUtility.TestPlanesAABB(planes, c.bounds)) continue;
                    if (sd < nearDistance) DrawNear(near, c, ref calls, ref trees, ref tris);
                    else DrawFar(far, c.farCrownA, c.farTrunkA, sd < farTrunkDistance, ref calls, ref trees, ref tris);
                }
            }
            LastDrawCalls = calls; LastTreesDrawn = trees; LastTriangles = tris;
        }

        void DrawNear(RenderParams rp, Cell c, ref int calls, ref int trees, ref long tris)
        {
            for (int s = 0; s < Species; s++)
            {
                var cr = c.crownA[s];
                if (cr.Length == 0) continue;
                calls += Draw(rp, crownNear[s], cr); calls += Draw(rp, trunkNear[s], c.trunkA[s]);
                trees += cr.Length; tris += (long)cr.Length * (Tris(crownNear[s]) + Tris(trunkNear[s]));
            }
        }

        void DrawFar(RenderParams rp, Matrix4x4[][] crowns, Matrix4x4[][] trunks, bool withTrunks, ref int calls, ref int trees, ref long tris)
        {
            for (int k = 0; k < FarClasses; k++)
            {
                var cr = crowns[k];
                if (cr.Length == 0) continue;
                int s = FarMeshOf[k];
                calls += Draw(rp, crownFar[s], cr);
                tris += (long)cr.Length * Tris(crownFar[s]);
                if (withTrunks) { calls += Draw(rp, trunkFar[s], trunks[k]); tris += (long)cr.Length * Tris(trunkFar[s]); }
                trees += cr.Length;
            }
        }

        // Round 63: past drawDistance in the opened view: one eight-sided shape per tree
        void DrawFar8(RenderParams rp, Matrix4x4[][] crowns, ref int calls, ref int trees, ref long tris)
        {
            if (far8 == null) { far8 = new Mesh[FarClasses]; for (int k = 0; k < FarClasses; k++) far8[k] = Far(crownFar[FarMeshOf[k]], k); }
            for (int k = 0; k < FarClasses; k++)
            {
                var cr = crowns[k]; if (cr.Length == 0 || !far8[k]) continue;
                calls += Draw(rp, far8[k], cr); tris += (long)cr.Length * 8; trees += cr.Length;
            }
        }
        static Mesh Far(Mesh crown, int farClass)
        {
            if (!crown) return null;
            var b = crown.bounds; var c = new Color(.28f, .40f, .19f, 0f);
            try { if (crown.isReadable) { var cols = crown.colors; if (cols.Length > 0) { Color sum = Color.clear; foreach (var x in cols) sum += x; c = sum / cols.Length; c.a = 0f; } } } catch (System.Exception) { }
            c = new Color(c.r * .9f, c.g * .9f, c.b * .9f, 0f);   // a little darker: the far crowns' own shading is gone
            var mid = b.center; float wy = farClass == 1 ? .25f : .45f;   // a pine's widest point low, the others' near the middle
            var top = new Vector3(mid.x, b.max.y, mid.z); var bottom = new Vector3(mid.x, b.min.y, mid.z); float ry = Mathf.Lerp(b.min.y, b.max.y, wy);
            var ring = new[] { new Vector3(b.max.x, ry, mid.z), new Vector3(mid.x, ry, b.max.z), new Vector3(b.min.x, ry, mid.z), new Vector3(mid.x, ry, b.min.z) };
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                var a = ring[i]; var n = ring[(i + 1) % 4];
                v.Add(top); v.Add(n); v.Add(a); v.Add(bottom); v.Add(a); v.Add(n);
                int s0 = v.Count - 6; t.Add(s0); t.Add(s0 + 1); t.Add(s0 + 2); t.Add(s0 + 3); t.Add(s0 + 4); t.Add(s0 + 5);
            }
            var m = new Mesh { name = "Far tree " + farClass }; m.SetVertices(v); m.SetTriangles(t, 0);
            var colors = new Color[v.Count]; for (int i = 0; i < colors.Length; i++) colors[i] = c; m.colors = colors;
            m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        static readonly Dictionary<Mesh, int> triCache = new Dictionary<Mesh, int>();
        static int Tris(Mesh m) { if (!m) return 0; if (!triCache.TryGetValue(m, out int n)) triCache[m] = n = (int)(m.GetIndexCount(0) / 3); return n; }

        // instanced draws in batches of at most 1000
        static int Draw(RenderParams rp, Mesh mesh, Matrix4x4[] list)
        {
            if (!mesh) return 0;
            int n = 0;
            for (int start = 0; start < list.Length; start += 1000)
            {
                Graphics.RenderMeshInstanced(rp, mesh, 0, list, Mathf.Min(1000, list.Length - start), start);
                n++;
            }
            return n;
        }
    }
}
