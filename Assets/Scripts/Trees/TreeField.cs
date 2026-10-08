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
    // Trees have no colliders (visual only).
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
                c.crown[s].Add(crownM); c.trunk[s].Add(trunkM); c.farCrown[fc].Add(crownM); c.farTrunk[fc].Add(trunkM); c.Grow(b);
                var fk = new Vector2Int(Mathf.FloorToInt(t.x / FarCell), Mathf.FloorToInt(t.z / FarCell));
                if (!farKeys.TryGetValue(fk, out var g)) { farKeys[fk] = g = new FarGroup(); groups.Add(g); }
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

        void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (!ready || (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView)) return;
            int calls = 0, trees = 0; long tris = 0;
            var eye = cam.transform.position;
            bool all = cam.orthographic; // overhead views draw every tree, simplified
            float draw = all ? float.MaxValue : Mathf.Min(drawDistance, cam.farClipPlane);
            GeometryUtility.CalculateFrustumPlanes(cam, planes);
            var near = new RenderParams(material) { camera = cam, shadowCastingMode = ShadowCastingMode.On, receiveShadows = true, layer = 0 };
            var far = new RenderParams(material) { camera = cam, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true, layer = 0 };
            foreach (var g in groups)
            {
                float d = Mathf.Sqrt(g.bounds.SqrDistance(eye));
                if (d > draw || (!all && !GeometryUtility.TestPlanesAABB(planes, g.bounds))) continue;
                if (all || d >= nearDistance)
                {
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
