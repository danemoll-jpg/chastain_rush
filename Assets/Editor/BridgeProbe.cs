using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 27 (A2): what a wheel stands on across a bridge's end. Editor batch, no Play mode:
    // ChastainRush.BridgeProbe.Run -probeOut <file> [-probeAt x,z,heading]. Along the line through each listed point (default: the New
    // Chastain Road bridges' four ends) from 12 m before to 25 m past, every 0.25 m, at the line and 1.5 m either side: a ray down onto
    // the real colliders (deck or terrain), its height and what it hit; the largest step and the sharpest crest (as a speed held).
    public static class BridgeProbe
    {
        public static void Run()
        {
            string outPath = Arg("-probeOut") ?? "bridge_probe.txt";
            EditorSceneManager.OpenScene("Assets/Generated/ChastainPrototype.unity");
            Physics.SyncTransforms();
            var ends = new List<(string name, Vector2 at, Vector2 dir)>
            {
                ("B5 west end, westbound", new Vector2(2413.75f, 765.72f), Vector2.left),
                ("B5 east end, eastbound", new Vector2(2446.38f, 766.11f), Vector2.right),
                ("B6 west end, westbound", new Vector2(2414.14f, 752.33f), Vector2.left),
                ("B6 east end, eastbound", new Vector2(2447.65f, 752.61f), Vector2.right),
            };
            var lines = new List<string>();
            foreach (var e in ends)
                foreach (float lat in new[] { 0f, -1.5f, 1.5f })
                {
                    var side = new Vector2(-e.dir.y, e.dir.x);
                    var ys = new List<(float s, float y, string c)>();
                    for (float s = -12f; s <= 25f; s += 0.25f)
                    {
                        var p = e.at + e.dir * s + side * lat;
                        if (Physics.Raycast(new Vector3(p.x, 400f, p.y), Vector3.down, out var hit, 400f, ~0, QueryTriggerInteraction.Ignore))
                            ys.Add((s, hit.point.y, hit.collider.name.Split(' ')[0]));
                    }
                    float step = 0f, stepAt = 0f, crest = 0f, crestAt = 0f;
                    for (int i = 1; i < ys.Count; i++) if (Mathf.Abs(ys[i].y - ys[i - 1].y) > step) { step = Mathf.Abs(ys[i].y - ys[i - 1].y); stepAt = ys[i].s; }
                    for (int i = 4; i + 4 < ys.Count; i++) { float k = -(ys[i - 4].y - 2f * ys[i].y + ys[i + 4].y); if (k > crest) { crest = k; crestAt = ys[i].s; } }   // over 1 m either side
                    lines.Add($"{e.name}, {lat:+0.0;-0.0;0} m: largest step {step:0.000} m at s {stepAt:0.00}; sharpest crest held to {(crest > 1e-6f ? Mathf.Sqrt(9.81f / crest) : 999f):0} m/s at s {crestAt:0.00}");
                    if (lat == 0f)
                    {
                        var sb = new System.Text.StringBuilder("   ");
                        foreach (var r in ys) if (r.s >= -4f && r.s <= 8f) sb.Append($"{r.s:0.00}:{r.c}:{r.y:0.000} ");
                        lines.Add(sb.ToString());
                        // the terrain under the deck there, against the deck (what a body contact could find under the wheels)
                        var tb = new System.Text.StringBuilder("   terrain - surface: ");
                        foreach (var r in ys) if (r.s >= -4f && r.s <= 1f)
                        {
                            var p = e.at + e.dir * r.s; var w = new Vector3(p.x, 0f, p.y);
                            foreach (var t in Terrain.activeTerrains) { var tp = t.transform.position; var sz = t.terrainData.size; if (w.x >= tp.x && w.x <= tp.x + sz.x && w.z >= tp.z && w.z <= tp.z + sz.z) { tb.Append($"{r.s:0.00}:{t.SampleHeight(w) + tp.y - r.y:+0.000;-0.000} "); break; } }
                        }
                        lines.Add(tb.ToString());
                    }
                }
            // trees standing under or beside the New Chastain decks, and whether their trunk reaches the deck
            var tf = Object.FindAnyObjectByType<TreeField>();
            if (tf)
                for (int i = 0; i < tf.count; i++)
                {
                    var t = TreeField.Unpack(tf.data, i);
                    if (t.x < 2405f || t.x > 2460f || t.z < 740f || t.z > 778f) continue;
                    bool hit = Physics.Raycast(new Vector3(t.x, 400f, t.z), Vector3.down, out var h, 400f, ~0, QueryTriggerInteraction.Ignore);
                    lines.Add($"tree {i} at ({t.x:0.0}, {t.y:0.00}, {t.z:0.0}) trunk {t.trunkH / 10f:0.0} m (top {t.y + t.trunkH / 10f:0.00}); surface above: {(hit ? h.collider.name + " " + h.point.y.ToString("0.00") : "none")}");
                }
            File.WriteAllLines(outPath, lines);
            Debug.Log("[BridgeProbe] written " + outPath);
            EditorApplication.Exit(0);
        }

        static string Arg(string name)
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < a.Length; i++) if (a[i] == name) return a[i + 1];
            return null;
        }
    }
}
