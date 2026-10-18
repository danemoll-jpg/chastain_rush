using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 42 (Part A): a fingerprint of the built scene, to show that a quick area or part build gives the same scene as a full Build Map.
    // Editor batch: ChastainRush.SceneDump.Run -dumpOut <file> [-dumpScene <scene path>]. One line per object (its path in the hierarchy, its
    // world bounds on the ground, a hash of its transform, components, meshes, colliders and serialized fields) and one per 32 x 32 block of
    // every terrain tile's heights and holes. Compare two with: python Tools/scene_diff.py <a> <b> [--box x0,z0,x1,z1].
    public static class SceneDump
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static readonly Regex InstanceIds = new Regex("\"(instanceID|m_FileID|fileID)\":-?\\d+", RegexOptions.Compiled);

        public static void Run()
        {
            try
            {
                string outPath = Arg("-dumpOut") ?? "scene_dump.txt";
                EditorSceneManager.OpenScene(Arg("-dumpScene") ?? MapBuilder.ScenePath);
                Write(outPath);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static string Detail; static readonly List<string> DetailLines = new List<string>();   // -dumpDetail <text>: every part of the objects whose path has it, unhashed
        public static void Write(string outPath)
        {
            Detail = Arg("-dumpDetail"); DetailLines.Clear();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var lines = new List<string>();
            var seen = new Dictionary<string, int>();
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                Walk(root.transform, "", lines, seen);
            foreach (var t in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(t => t.name))
                TerrainLines(t, lines);
            lines.Sort(StringComparer.Ordinal);
            if (Detail != null) File.WriteAllLines(outPath + ".detail.txt", DetailLines);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)));
            File.WriteAllLines(outPath, lines);
            Debug.Log($"[SceneDump] {lines.Count} lines to {outPath} in {sw.Elapsed.TotalSeconds:0.0} s");
        }

        static void Walk(Transform t, string parent, List<string> lines, Dictionary<string, int> seen)
        {
            string path = parent + "/" + t.name;
            seen.TryGetValue(path, out int n); seen[path] = n + 1;
            string key = n == 0 ? path : path + "#" + n;   // same-named siblings, in hierarchy order
            var sb = new StringBuilder();
            sb.Append(t.gameObject.activeSelf ? 'A' : 'I').Append(t.gameObject.layer).Append('|');
            V(sb, t.localPosition); V(sb, t.localEulerAngles); V(sb, t.localScale);
            var bounds = new Bounds(t.position, Vector3.zero);
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null) { sb.Append("|missing"); continue; }
                sb.Append('|').Append(c.GetType().Name).Append(':');
                switch (c)
                {
                    case Transform _: break;
                    case MeshFilter mf: sb.Append(MeshHash(mf.sharedMesh)); break;
                    case MeshRenderer mr: sb.Append(string.Join(",", mr.sharedMaterials.Select(m => m ? m.name : "-"))).Append(mr.shadowCastingMode); bounds.Encapsulate(mr.bounds); break;
                    case BoxCollider bc: V(sb, bc.center); V(sb, bc.size); sb.Append(bc.isTrigger).Append(bc.sharedMaterial ? bc.sharedMaterial.name : ""); bounds.Encapsulate(bc.bounds); break;
                    case MeshCollider mc: sb.Append(MeshHash(mc.sharedMesh)).Append(mc.convex).Append(mc.isTrigger).Append(mc.sharedMaterial ? mc.sharedMaterial.name : ""); if (mc.sharedMesh) bounds.Encapsulate(mc.bounds); break;
                    case Terrain _: case TerrainCollider _: break;   // the heights are TerrainLines'
                    case Collider col: sb.Append(Hash(InstanceIds.Replace(EditorJsonUtility.ToJson(col), ""))); bounds.Encapsulate(col.bounds); break;
                    case MonoBehaviour mb: sb.Append(Hash(InstanceIds.Replace(EditorJsonUtility.ToJson(c), ""))).Append(Hash(string.Join("|", SceneRefs.Of(mb).Select(r => r.prop + "=" + SceneRefs.Describe(r.target)).OrderBy(x => x, StringComparer.Ordinal)))); break;   // and what it refers to, by place
                    default: sb.Append(Hash(InstanceIds.Replace(EditorJsonUtility.ToJson(c), ""))); break;
                }
            }
            lines.Add($"O\t{key}\t{F(bounds.min.x)},{F(bounds.min.z)},{F(bounds.max.x)},{F(bounds.max.z)}\t{Hash(sb.ToString())}");
            if (Detail != null && key.Contains(Detail))
                DetailLines.Add(key + "\n  " + sb.ToString().Replace("|", "\n  |") + "\n  refs: " + string.Join("\n    ", t.GetComponents<MonoBehaviour>().Where(m => m).SelectMany(m => SceneRefs.Of(m).Select(r => r.prop + "=" + SceneRefs.Describe(r.target)))) +
                                "\n  json: " + string.Join("\n    ", t.GetComponents<MonoBehaviour>().Where(m => m).Select(m => InstanceIds.Replace(EditorJsonUtility.ToJson(m), "")).Select(j => j.Length > 4000 ? j.Substring(0, 4000) : j)));
            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), path, lines, seen);
        }

        // the heights (normalized, to 1e-6) and holes, a line per 32 x 32 block of samples with the block's world rectangle
        static void TerrainLines(Terrain t, List<string> lines)
        {
            var d = t.terrainData; int res = d.heightmapResolution, hr = d.holesResolution; var pos = t.transform.position; var size = d.size;
            var h = d.GetHeights(0, 0, res, res); var holes = d.GetHoles(0, 0, hr, hr);
            const int B = 32;
            for (int bz = 0; bz < res; bz += B)
                for (int bx = 0; bx < res; bx += B)
                {
                    unchecked
                    {
                        long acc = 17;
                        for (int z = bz; z < Mathf.Min(bz + B, res); z++)
                            for (int x = bx; x < Mathf.Min(bx + B, res); x++) acc = acc * 31 + Mathf.RoundToInt(h[z, x] * 1e6f);
                        for (int z = bz * hr / res; z < Mathf.Min((bz + B) * hr / res, hr); z++)
                            for (int x = bx * hr / res; x < Mathf.Min((bx + B) * hr / res, hr); x++) acc = acc * 3 + (holes[z, x] ? 1 : 2);
                        float x0 = pos.x + bx * size.x / (res - 1), z0 = pos.z + bz * size.z / (res - 1);
                        float x1 = pos.x + Mathf.Min(bx + B, res - 1) * size.x / (res - 1), z1 = pos.z + Mathf.Min(bz + B, res - 1) * size.z / (res - 1);
                        lines.Add($"T\t{t.name}/{bx}_{bz}\t{F(x0)},{F(z0)},{F(x1)},{F(z1)}\t{acc:x16}");
                    }
                }
        }

        public static string MeshHash(Mesh m)
        {
            if (!m) return "none";
            unchecked
            {
                long acc = m.vertexCount * 7919L + m.triangles.Length;
                foreach (var v in m.vertices) acc = acc * 31 + Mathf.RoundToInt(v.x * 1000f) * 73856093L + Mathf.RoundToInt(v.y * 1000f) * 19349663L + Mathf.RoundToInt(v.z * 1000f) * 83492791L;
                foreach (var i in m.triangles) acc = acc * 31 + i;
                foreach (var c in m.colors) acc = acc * 31 + Mathf.RoundToInt(c.r * 255f) * 65536 + Mathf.RoundToInt(c.g * 255f) * 256 + Mathf.RoundToInt(c.b * 255f);
                return acc.ToString("x16");
            }
        }

        static string Hash(string s)
        {
            unchecked { long acc = 1469598103934665603L; foreach (char ch in s) acc = (acc ^ ch) * 1099511628211L; return acc.ToString("x16"); }
        }
        static void V(StringBuilder sb, Vector3 v) => sb.Append(F(v.x)).Append(',').Append(F(v.y)).Append(',').Append(F(v.z)).Append(';');
        static string F(float f) => (Mathf.Round(f * 1000f) / 1000f).ToString("0.###", CI);
        static string Arg(string name) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
