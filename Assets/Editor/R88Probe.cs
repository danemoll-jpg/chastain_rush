using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 88 (C): every surface under a line in the built scene. Editor batch, no Play mode:
    //   ChastainRush.R88Probe.Run -probeLines <lines.json>[;<b.json>] -probeOut <file.tsv> [-probeOnly <name part>]
    // The lines are r82_lines.py's format (name, line). Every 0.5 m along each line, one ray down from 80 m up: every collider hit on the way down
    // (height, the hit's n.y, the collider's name), so Tools/race/probe_ride.py can ride the real surfaces (decks, the ground, a bridge's structure).
    public static class R88Probe
    {
        [System.Serializable] class PL { public string name; public float[] line; }
        [System.Serializable] class PF { public PL[] lines; }

        public static void Run()
        {
            string outPath = Arg("-probeOut") ?? "probe.tsv", only = Arg("-probeOnly");
            var lines = new List<PL>();
            foreach (var f in (Arg("-probeLines") ?? "").Split(';').Where(s => s.Length > 0)) lines.AddRange(JsonUtility.FromJson<PF>(File.ReadAllText(f)).lines);
            EditorSceneManager.OpenScene("Assets/Generated/ChastainPrototype.unity");
            Physics.SyncTransforms();
            var sb = new StringBuilder("line\tk\tx\tz\thits\n"); var ci = CultureInfo.InvariantCulture;
            for (int li = 0; li < lines.Count; li++)
            {
                if (!string.IsNullOrEmpty(only) && !lines[li].name.Contains(only)) continue;
                var q = new List<Vector2>(); var a = lines[li].line; for (int i = 0; i + 1 < a.Length; i += 2) q.Add(new Vector2(a[i], a[i + 1]));
                var cum = new float[q.Count]; for (int i = 1; i < q.Count; i++) cum[i] = cum[i - 1] + (q[i] - q[i - 1]).magnitude;
                int k = 0;
                for (float s = 0f; s <= cum[q.Count - 1]; s += .5f, k++)
                {
                    int i = 1; while (i < q.Count - 1 && cum[i] < s) i++;
                    var p = Vector2.Lerp(q[i - 1], q[i], Mathf.InverseLerp(cum[i - 1], cum[i], s));
                    var top = new Vector3(p.x, 900f, p.y);
                    var hits = Physics.RaycastAll(top, Vector3.down, 1000f, ~0, QueryTriggerInteraction.Ignore).OrderByDescending(h => h.point.y)
                        .Select(h => string.Format(ci, "{0:0.000}/{1:0.00}/{2}", h.point.y, h.normal.y, h.collider.name.Replace("\t", " ").Replace("|", " ")));
                    sb.Append(li).Append('\t').Append(k).Append('\t').Append(p.x.ToString("0.00", ci)).Append('\t').Append(p.y.ToString("0.00", ci)).Append('\t').Append(string.Join("|", hits)).Append('\n');
                }
            }
            File.WriteAllText(outPath, sb.ToString());
            EditorApplication.Exit(0);
        }
        static string Arg(string name) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
