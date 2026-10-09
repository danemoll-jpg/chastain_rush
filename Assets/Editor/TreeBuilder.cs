using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChastainRush
{
    // Builds the TreeField: reads Data/trees.bin (positions from NAIP canopy, made by Tools/mapgen/mapgen.py), picks a
    // species and size for each tree, sets it on the terrain and packs it into a TreeField component. The models are
    // Racer's Blender tree kit (Assets/Trees/SceneryTrees.fbx, copied unchanged). Kit meshes follow Racer's
    // SceneryTrees.Prepare: one mesh per part with per-face colours (leaf facets vary a little in shade, bark is the
    // species' bark), vertex alpha = sway weight, uv.x = 1 on leaves. Kit space: crown y 0..1 and x,z -1..1;
    // trunk y 0..1 with a radius of 0.5 at the base (so scale x,z = trunk diameter).
    static class TreeBuilder
    {
        const string KitPath = "Assets/Trees/SceneryTrees.fbx";
        static readonly string[] Variants = { "Round", "Oak", "Maple", "Pine", "Poplar", "Young" };
        const int Round = 0, Oak = 1, Maple = 2, Pine = 3, Poplar = 4, Young = 5;
        // Racer's colours (its shader runs in linear space; this project is in gamma space, so they are brightened a little)
        static readonly Color[] Leaf =
        {
            new Color(.25f, .37f, .17f), new Color(.21f, .32f, .14f), new Color(.31f, .38f, .16f),
            new Color(.13f, .25f, .15f), new Color(.30f, .41f, .18f), new Color(.33f, .45f, .20f),
        };
        static readonly Color[] Bark =
        {
            new Color(.27f, .21f, .15f), new Color(.24f, .19f, .14f), new Color(.29f, .22f, .16f),
            new Color(.26f, .18f, .13f), new Color(.36f, .33f, .29f), new Color(.30f, .24f, .17f),
        };
        // typical size of a full-grown tree of each species (true metres; height gets vertical_scale on top)
        static readonly float[] BaseRadius = { 4.2f, 5.5f, 4.5f, 2.8f, 3.0f, 2.2f };
        static readonly float[] BaseHeight = { 15f, 18f, 16f, 23f, 22f, 8f };
        static readonly float[] TrunkFraction = { 0.62f, 0.62f, 0.62f, 0.45f, 0.60f, 0.62f };

        public sealed class Result
        {
            public int Trees;
            public int[] PerSpecies = new int[Variants.Length];
            public string KitTriangles;
            public int Woods, Yards, Rejected;
            public TreeField Field; // the kit meshes, reused by the hand-placed trees of Round 7
        }

        public static Result Build(string treesBin, Func<float, float, float> ground, float vScale, string generatedDir, Material material, Transform parent, Func<float, float, bool> reject = null, Func<float, float, Vector2> move = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(KitPath);
            if (asset == null) throw new Exception("Missing " + KitPath);
            Mesh Part(string name) => asset.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f => f.name == name)?.sharedMesh;
            int n = Variants.Length;
            var field = new GameObject("Trees").AddComponent<TreeField>();
            field.transform.SetParent(parent, false);
            field.crownNear = new Mesh[n]; field.crownFar = new Mesh[n]; field.trunkNear = new Mesh[n]; field.trunkFar = new Mesh[n];
            var tri = new StringBuilder();
            for (int v = 0; v < n; v++)
            {
                string name = Variants[v];
                field.crownNear[v] = Save(Prepare(name + " crown", v, Part(name + "_Crown__leaf"), Part(name + "_Crown__bark"), true), generatedDir);
                field.crownFar[v] = Save(Prepare(name + " crown far", v, Part(name + "_CrownFar__leaf"), Part(name + "_CrownFar__bark"), true), generatedDir);
                field.trunkNear[v] = Save(Prepare(name + " trunk", v, null, Part(name + "_Trunk__bark"), false), generatedDir);
                field.trunkFar[v] = Save(Prepare(name + " trunk far", v, null, Part(name + "_TrunkFar__bark"), false), generatedDir);
                tri.Append($"{name} near {Tris(field.crownNear[v]) + Tris(field.trunkNear[v])} (crown {Tris(field.crownNear[v])} + trunk {Tris(field.trunkNear[v])}), far {Tris(field.crownFar[v]) + Tris(field.trunkFar[v])}; ");
            }
            field.material = material;

            var bytes = File.ReadAllBytes(treesBin);
            if (bytes.Length < 8 || bytes[0] != 'C' || bytes[1] != 'R' || bytes[2] != 'T' || bytes[3] != '1') throw new Exception("Bad trees.bin");
            int count = BitConverter.ToInt32(bytes, 4);
            if (bytes.Length != 8 + count * 12) throw new Exception($"trees.bin is {bytes.Length} bytes, expected {8 + count * 12}");
            var result = new Result { Trees = count, KitTriangles = tri.ToString(), Field = field };
            var packed = new byte[count * TreeField.PackedSize];
            int kept = 0;
            for (int i = 0; i < count; i++)
            {
                int o = 8 + i * 12;
                float x = BitConverter.ToSingle(bytes, o), z = BitConverter.ToSingle(bytes, o + 4);
                if (move != null) { var m = move(x, z); x = m.x; z = m.y; }   // Round 9: a tree on a drive is moved into the yard
                if (reject != null && reject(x, z)) { result.Rejected++; continue; } // Round 7: not through a sidewalk, a pool or a fence
                int seed = BitConverter.ToUInt16(bytes, o + 8);
                bool yard = bytes[o + 10] != 0;
                float depth = bytes[o + 11] * 0.1f; // metres inside the canopy
                float h1 = Hash(seed, 1), h2 = Hash(seed, 2), h3 = Hash(seed, 3);
                int sp = Pick(x, z, h1, yard, depth);
                result.PerSpecies[sp]++;
                if (yard) result.Yards++; else result.Woods++;
                float f = (0.8f + 0.4f * h2) * (0.75f + 0.35f * Mathf.Clamp01(depth / 10f)) * (yard ? 0.9f : 1f);
                if (sp == Young) f = 0.7f + 0.5f * h2;
                float height = BaseHeight[sp] * f * vScale, radius = BaseRadius[sp] * f;
                float trunkH = Mathf.Min(25.5f, height * TrunkFraction[sp]);
                float crownBase = trunkH * 0.5f;
                float trunkW = 0.18f + 0.018f * BaseHeight[sp] * f;
                var p = new TreeField.Packed
                {
                    x = x, z = z, y = ground(x, z) - 0.1f,
                    yaw = (ushort)(h3 * 65535f),
                    crownH = (ushort)Mathf.Clamp(Mathf.RoundToInt((height - crownBase) * 100f), 1, 65535),
                    species = (byte)sp,
                    radius = (byte)Mathf.Clamp(Mathf.RoundToInt(radius * 20f), 1, 255),
                    trunkH = (byte)Mathf.Clamp(Mathf.RoundToInt(trunkH * 10f), 1, 255),
                    trunkW = (byte)Mathf.Clamp(Mathf.RoundToInt(trunkW * 100f), 5, 255),
                };
                var span = new Span<byte>(packed, kept * TreeField.PackedSize, TreeField.PackedSize);
                System.Runtime.InteropServices.MemoryMarshal.Write(span, ref p);
                kept++;
            }
            System.Array.Resize(ref packed, kept * TreeField.PackedSize);
            result.Trees = kept;
            field.data = packed;
            field.count = kept;
            return result;
        }

        // Natural-looking mix: pine in patches (about a quarter of the woods), oak and maple more in yards and open
        // woods, tulip poplar scattered, young trees at the edge of the canopy and now and then in yards.
        static int Pick(float x, float z, float h, bool yard, float depth)
        {
            float patch = Mathf.PerlinNoise((x + 311f) / 140f, (z - 97f) / 140f);
            if (depth < 0.8f && h < 0.35f) return Young;
            if (yard)
            {
                if (h < 0.10f) return Young;
                if (h < 0.17f) return Pine;
                return h < 0.45f ? Oak : h < 0.75f ? Maple : Round;
            }
            if (patch < 0.38f) return h < 0.55f ? Pine : h < 0.75f ? Round : Poplar;
            if (patch > 0.62f) return h < 0.30f ? Oak : h < 0.55f ? Maple : h < 0.75f ? Round : h < 0.88f ? Poplar : Pine;
            return h < 0.18f ? Pine : h < 0.38f ? Round : h < 0.58f ? Maple : h < 0.78f ? Oak : Poplar;
        }

        static float Hash(int seed, int salt)
        {
            uint x = (uint)seed * 2654435761u + (uint)salt * 40503u;
            x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; x *= 3266489917u; x ^= x >> 16;
            return (x & 0xFFFFFF) / (float)0x1000000;
        }

        static int Tris(Mesh m) => m ? (int)(m.GetIndexCount(0) / 3) : 0;

        static Mesh Save(Mesh m, string dir)
        {
            if (m == null) return null;
            AssetDatabase.CreateAsset(m, $"{dir}/Meshes/TreeKit_{m.name.Replace(' ', '_')}.asset");
            return m;
        }

        static Mesh Prepare(string name, int variant, Mesh leaf, Mesh bark, bool crown)
        {
            if (!leaf && !bark) return null;
            var v = new List<Vector3>(); var nrm = new List<Vector3>(); var col = new List<Color>(); var uv = new List<Vector2>(); var tri = new List<int>();
            void Add(Mesh m, bool isLeaf)
            {
                if (!m) return;
                var mv = m.vertices; var mn = m.normals; var mt = m.triangles;
                for (int i = 0; i < mt.Length; i += 3)
                {
                    Vector3 a = mv[mt[i]], b = mv[mt[i + 1]], c = mv[mt[i + 2]], centre = (a + b + c) / 3;
                    float shade = isLeaf ? .86f + .28f * Mathf.PerlinNoise(centre.x * 3.1f + variant, centre.z * 3.1f + centre.y * 2.3f)
                                         : .9f + .2f * Mathf.PerlinNoise(centre.y * 4, variant);
                    var baseColor = (isLeaf ? Leaf[variant] : Bark[variant]) * shade; baseColor.a = 1;
                    foreach (int k in new[] { mt[i], mt[i + 1], mt[i + 2] })
                    {
                        var p = mv[k];
                        float sway = crown ? (isLeaf ? Mathf.Clamp01(.35f + p.y * .65f) : Mathf.Clamp01(p.y) * .5f) : 0;
                        var c4 = baseColor; c4.a = sway;
                        tri.Add(v.Count); v.Add(p); nrm.Add(mn != null && mn.Length > k ? mn[k] : Vector3.up); col.Add(c4); uv.Add(new Vector2(isLeaf ? 1 : 0, 0));
                    }
                }
            }
            Add(leaf, true); Add(bark, false);
            var mesh = new Mesh { name = "Scenery " + name, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(v); mesh.SetNormals(nrm); mesh.SetColors(col); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
