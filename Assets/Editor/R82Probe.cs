using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Round 82: the built scene's surface along lines (Editor batch, report only):
//   Unity.exe -batchmode -quit -projectPath . -executeMethod ChastainRush.R82Probe.Run -probeLines local/round82/r82_lines_a.json -probeOut <file.tsv>
// every metre along each line: the top surface's height and what it is (a ray down from 60 m over the ground), the ground's height.
namespace ChastainRush
{
    public static class R82Probe
    {
        [System.Serializable] class L { public string name; public float[] line; }
        [System.Serializable] class F { public L[] lines; }

        public static void Run()
        {
            string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < a.Length; i++) if (a[i] == n) return a[i + 1]; return null; }
            var f = JsonUtility.FromJson<F>(File.ReadAllText(Arg("-probeLines")));
            EditorSceneManager.OpenScene(MapBuilder.ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var sb = new StringBuilder("line\ts\tx\tz\ttop\twhat\tground\n");
            foreach (var l in f.lines)
            {
                var pts = Enumerable.Range(0, l.line.Length / 2).Select(i => new Vector2(l.line[2 * i], l.line[2 * i + 1])).ToList();
                float s = 0f;
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    float d = (pts[i + 1] - pts[i]).magnitude;
                    for (float u = 0f; u < d; u += 1f)
                    {
                        var p = Vector2.Lerp(pts[i], pts[i + 1], u / d);
                        float g = Terrain.activeTerrains.Select(t => { var tp = t.transform.position; var sz = t.terrainData.size; return p.x >= tp.x && p.x <= tp.x + sz.x && p.y >= tp.z && p.y <= tp.z + sz.z ? t.SampleHeight(new Vector3(p.x, 0, p.y)) + tp.y : float.NaN; }).FirstOrDefault(v => !float.IsNaN(v));
                        var hits = Physics.RaycastAll(new Vector3(p.x, g + 60f, p.y), Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore).OrderByDescending(h => h.point.y).ToList();
                        string what = hits.Count > 0 ? hits[0].collider.name : "none"; float top = hits.Count > 0 ? hits[0].point.y : float.NaN;
                        sb.AppendLine($"{l.name}\t{s + u:0}\t{p.x:0.0}\t{p.y:0.0}\t{top:0.00}\t{what}\t{g:0.00}");
                    }
                    s += d;
                }
            }
            File.WriteAllText(Arg("-probeOut"), sb.ToString());
            Debug.Log("[R82Probe] written " + Arg("-probeOut"));
        }
    }
}
