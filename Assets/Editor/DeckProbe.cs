using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 62 (check: vehicles fell through bridge decks): every "BridgeDecks" object in the built scene: its collider, its mesh (vertices, bounds,
    // vertices that are not finite), and a ray down at given points. Editor batch: ChastainRush.DeckProbe.Run -probeOut <file> [-probeAt "x,z;x,z" | -probeAtFile <file>]
    public static class DeckProbe
    {
        public static void Run()
        {
            string outPath = Arg("-probeOut") ?? "deck_probe.txt"; var lines = new List<string>();
            EditorSceneManager.OpenScene("Assets/Generated/ChastainPrototype.unity"); Physics.SyncTransforms();
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name == "BridgeDecks").Select(t => t.gameObject))
            {
                var mf = go.GetComponent<MeshFilter>(); var mc = go.GetComponent<MeshCollider>(); var m = mf ? mf.sharedMesh : null;
                int bad = m ? m.vertices.Count(v => !float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z)) : -1;
                lines.Add($"{GetPath(go.transform)}: active {go.activeInHierarchy}; mesh {(m ? $"{m.name}, {m.vertexCount} vertices, {m.triangles.Length / 3} triangles, bounds {m.bounds}, not finite {bad}, index format {m.indexFormat}" : "none")}; " +
                          $"collider {(mc ? $"{mc.enabled}, mesh {(mc.sharedMesh ? mc.sharedMesh.name + " " + mc.sharedMesh.vertexCount : "none")}, convex {mc.convex}" : "none")}");
            }
            foreach (var item in (Arg("-probeAtFile") is string pf ? File.ReadAllText(pf) : Arg("-probeAt") ?? "").Split(';'))   // Round 81: -probeAtFile, the points from a file (a long list)
            {
                if (string.IsNullOrWhiteSpace(item)) continue; var v = item.Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                var hits = Physics.RaycastAll(new Vector3(v[0], 400f, v[1]), Vector3.down, 400f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance);
                lines.Add($"at ({v[0]}, {v[1]}): " + string.Join("; ", hits.Select(h => $"{h.point.y:0.00} {h.collider.name}")));
            }
            File.WriteAllLines(outPath, lines); EditorApplication.Exit(0);
        }
        static string GetPath(Transform t) => t.parent ? GetPath(t.parent) + "/" + t.name : t.name;
        static string Arg(string name) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
