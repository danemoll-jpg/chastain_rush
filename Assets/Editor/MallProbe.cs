using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 60: what of Town Center's inside lies outside its walls (Editor batch, the built scene): every mesh under "Mall inside" with vertices
    // more than 0.3 m outside the six footprints (the concourse building and its anchors), between the lower floor and the roof; the object, how many
    // vertices, a sample. -probeOut <file>
    public static class MallProbe
    {
        [System.Serializable] class B { public string type; public float[] footprint; }
        [System.Serializable] class MD { public B[] buildings; }
        public static void Run()
        {
            string outPath = Arg("-probeOut") ?? "mall_probe.txt";
            EditorSceneManager.OpenScene("Assets/Generated/ChastainPrototype.unity");
            var map = JsonUtility.FromJson<MD>(File.ReadAllText("Data/map.json"));
            var rings = map.buildings.Where(b => b.type == "mall").Select(b => { var l = new List<Vector2>(); for (int i = 0; i + 1 < b.footprint.Length; i += 2) l.Add(new Vector2(b.footprint[i], b.footprint[i + 1])); return l; }).ToList();
            var root = GameObject.Find("Mall inside"); var lines = new List<string>();
            if (!root) { File.WriteAllText(outPath, "no Mall inside"); return; }
            var mall = Object.FindAnyObjectByType<TownCenterMall>();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.sharedMesh) continue; var vs = mf.sharedMesh.vertices; int n = 0; Vector3 sample = Vector3.zero; float worst = 0f;
                foreach (var v0 in vs)
                {
                    var v = mf.transform.TransformPoint(v0); if (v.y < mall.lowerFloor - .1f || v.y > mall.roof) continue;
                    var p = new Vector2(v.x, v.z); if (rings.Any(r => In(p, r))) continue;
                    float d = rings.Min(r => Dist(p, r)); if (d < .3f) continue;
                    n++; if (d > worst) { worst = d; sample = v; }
                }
                if (n > 0) lines.Add($"{mf.name}: {n} vertices outside, the farthest {worst:0.00} m at ({sample.x:0.0}, {sample.y:0.0}, {sample.z:0.0}) uv ({Vector2.Dot(new Vector2(sample.x, sample.z), mall.frameU):0.0}, {Vector2.Dot(new Vector2(sample.x, sample.z), mall.frameV):0.0})");
            }
            File.WriteAllText(outPath, lines.Count == 0 ? "nothing outside" : string.Join("\n", lines));
        }
        static bool In(Vector2 p, List<Vector2> poly) { bool c = false; for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++) if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) c = !c; return c; }
        static float Dist(Vector2 p, List<Vector2> r) { float d = float.MaxValue; for (int i = 0; i < r.Count; i++) { var a = r[i]; var b = r[(i + 1) % r.Count]; var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); d = Mathf.Min(d, (a + ab * t - p).magnitude); } return d; }
        static string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
