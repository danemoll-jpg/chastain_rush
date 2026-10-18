using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 41: what is drawn (not what is solid) over a rectangle of the built scene. Editor batch, no Play mode:
    // ChastainRush.MeshProbe.Run -meshRects "name:x0,z0,x1,z1,step;..." -probeOut <file>. Every drawn mesh whose bounds reach the rectangle gets a
    // temporary mesh collider on its own layer; then a ray down every 'step' m: the top drawn surface and its object, and the top collider
    // (what a vehicle stands on) for comparison. Report only. Round 42: -probeTop <y> starts the rays there (inside a building).
    public static class MeshProbe
    {
        const int Layer = 31;
        public static void Run()
        {
            string outPath = Arg("-probeOut") ?? "mesh_probe.txt", spec = Arg("-meshRects") ?? "";
            float top = float.Parse(Arg("-probeTop") ?? "600", CultureInfo.InvariantCulture);   // Round 42: rays from under a roof
            EditorSceneManager.OpenScene("Assets/Generated/ChastainPrototype.unity");
            var ci = CultureInfo.InvariantCulture; var sb = new StringBuilder();
            var rends = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var item in spec.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                var parts = item.Split(':'); var v = System.Array.ConvertAll(parts[1].Split(','), q => float.Parse(q, ci));
                float x0 = v[0], z0 = v[1], x1 = v[2], z1 = v[3], step = v[4];
                var temp = new List<GameObject>();
                foreach (var r in rends)
                {
                    var b = r.bounds; if (b.max.x < x0 || b.min.x > x1 || b.max.z < z0 || b.min.z > z1) continue;
                    var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                    var g = new GameObject("probe " + r.name); g.layer = Layer;
                    g.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation); g.transform.localScale = r.transform.lossyScale;
                    g.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh; temp.Add(g);
                }
                Physics.SyncTransforms();
                sb.AppendLine($"{parts[0]}: x {x0:0.0}..{x1:0.0}, z {z0:0.0}..{z1:0.0}, every {step} m ({temp.Count} meshes)");
                sb.AppendLine("  x, z: drawn top y (object) | solid top y (collider)");
                for (float z = z1; z >= z0 - 1e-3f; z -= step)
                    for (float x = x0; x <= x1 + 1e-3f; x += step)
                    {
                        var o = new Vector3(x, top, z);
                        string drawn = Physics.Raycast(o, Vector3.down, out var h, 1000f, 1 << Layer, QueryTriggerInteraction.Ignore) ? $"{h.point.y:0.00} ({h.collider.name.Substring(6)})" : "none";
                        string solid = Physics.Raycast(o, Vector3.down, out var s, 1000f, ~(1 << Layer), QueryTriggerInteraction.Ignore) ? $"{s.point.y:0.00} ({s.collider.name})" : "none";
                        sb.AppendLine($"  {x:0.0}, {z:0.0}: {drawn} | {solid}");
                    }
                foreach (var g in temp) Object.DestroyImmediate(g);
            }
            File.WriteAllText(outPath, sb.ToString());
            EditorApplication.Exit(0);
        }
        static string Arg(string name) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
