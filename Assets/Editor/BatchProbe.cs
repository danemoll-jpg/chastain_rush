using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 92 (item 0): what the build's static batches are made of: every batching-static renderer of the world scene, its mesh's vertices
    // (each instance is copied into a batch), by mesh size and by the object's top-level parent. Editor batch:
    // -executeMethod ChastainRush.BatchProbe.Run -probeOut <file>
    public static class BatchProbe
    {
        public static void Run()
        {
            string outPath = Arg("-probeOut") ?? "batch_probe.txt";
            try
            {
                var scene = EditorSceneManager.OpenScene(MapBuilder.ScenePath, OpenSceneMode.Single);
                var rows = new List<(string top, int verts, long bytes, string mesh)>();
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (!GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.BatchingStatic)) continue;
                        var mf = r.GetComponent<MeshFilter>(); var m = mf ? mf.sharedMesh : null; if (!m) continue;
                        long stride = 0; for (int s = 0; s < m.vertexBufferCount; s++) stride += m.GetVertexBufferStride(s);
                        long idx = 0; for (int s = 0; s < m.subMeshCount; s++) idx += (long)m.GetIndexCount(s);
                        rows.Add((root.name + "/" + (r.transform.parent ? r.transform.parent.name : ""), m.vertexCount, m.vertexCount * stride + idx * 4, m.name));
                    }
                using var w = new StreamWriter(outPath);
                w.WriteLine($"{rows.Count} batching-static renderers, {rows.Sum(x => x.bytes) / 1048576.0:0} MB of vertices and indices if all batched");
                foreach (int lim in new[] { 1000, 2000, 4000, 8000, 16000, 32000, 65000 })
                {
                    var keep = rows.Where(x => x.verts <= lim).ToList();
                    w.WriteLine($"  meshes of at most {lim,6} vertices: {keep.Count,6} renderers, {keep.Sum(x => x.bytes) / 1048576.0,7:0} MB; the rest {rows.Count - keep.Count,6} renderers, {rows.Where(x => x.verts > lim).Sum(x => x.bytes) / 1048576.0,7:0} MB");
                }
                w.WriteLine("by parent (top 40):");
                foreach (var g in rows.GroupBy(x => x.top).OrderByDescending(g => g.Sum(x => x.bytes)).Take(40))
                    w.WriteLine($"  {g.Sum(x => x.bytes) / 1048576.0,8:0} MB {g.Count(),7} renderers, largest {g.Max(x => x.verts),7} verts  {g.Key}");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText(outPath, e.ToString()); EditorApplication.Exit(1); }
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }
    }
}
