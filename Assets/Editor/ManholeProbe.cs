using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 25 (A1): what a vehicle driving along the street over each manhole touches. Editor batch, no Play mode:
    // ChastainRush.ManholeProbe.Run -probeOut <file>. Along each manhole's street (its riser's way) from 10 m before to 10 m past it, every
    // 0.25 m: a ray down (what the wheels stand on) and a car-sized box 0.15 to 0.75 m over the street (what the body would touch).
    public static class ManholeProbe
    {
        public static void Run()
        {
            string outPath = Arg("-probeOut") ?? "manhole_probe.txt";
            EditorSceneManager.OpenScene("Assets/Generated/ChastainPrototype.unity");
            Physics.SyncTransforms();
            var lines = new List<string>();
            foreach (var ml in Object.FindObjectsByType<ManholeLid>(FindObjectsSortMode.None))
            {
                var home = ml.lid.position; var dir = ml.outward.normalized; var side = Vector3.Cross(Vector3.up, dir);
                var hits = new Dictionary<string, (int n, float lo, float hi)>(); var rays = new List<string>(); float prevY = float.NaN, worst = 0f; float worstAt = 0f;
                for (float s = -10f; s <= 10f; s += 0.25f)
                {
                    var p = home + dir * s;
                    if (Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (!float.IsNaN(prevY) && Mathf.Abs(hit.point.y - prevY) > worst) { worst = Mathf.Abs(hit.point.y - prevY); worstAt = s; }
                        prevY = hit.point.y;
                        if (Mathf.Abs(s) <= 4f) rays.Add($"{s:0.00}:{hit.collider.name.Split(' ')[0]}:{hit.point.y - home.y:+0.000;-0.000}");
                        var c = hit.point + Vector3.up * 0.45f;
                        foreach (var col in Physics.OverlapBox(c, new Vector3(0.9f, 0.3f, 1.9f), Quaternion.LookRotation(dir), ~0, QueryTriggerInteraction.Ignore))
                        {
                            if (col is TerrainCollider) continue;
                            string k = col.name; hits.TryGetValue(k, out var v);
                            hits[k] = (v.n + 1, v.n == 0 ? s : Mathf.Min(v.lo, s), v.n == 0 ? s : Mathf.Max(v.hi, s));
                        }
                    }
                    else rays.Add($"{s:0.00}:MISS");
                }
                // how far the floor's and the riser's vertices stand over the terrain near the hole
                string Above(Collider col)
                {
                    var mc = col as MeshCollider; if (!mc || !mc.sharedMesh) return "";
                    float most = float.MinValue; Vector3 at = default; var tr = col.transform;
                    foreach (var v in mc.sharedMesh.vertices)
                    {
                        var w = tr.TransformPoint(v); if ((new Vector2(w.x - home.x, w.z - home.z)).magnitude > 8f) continue;
                        var t = Terrain.activeTerrains.FirstOrDefault(tt => { var tp = tt.transform.position; var sz = tt.terrainData.size; return w.x >= tp.x && w.x <= tp.x + sz.x && w.z >= tp.z && w.z <= tp.z + sz.z; });
                        if (!t) continue; float d = w.y - (t.SampleHeight(w) + t.transform.position.y);
                        if (d > most) { most = d; at = w - home; }
                    }
                    return $" [{col.name.Split('(')[0].Trim()}: highest {most:+0.000;-0.000} m over the terrain at ({at.x:0.0}, {at.z:0.0})]";
                }
                var near = Physics.OverlapSphere(home, 6f, ~0, QueryTriggerInteraction.Ignore).Where(c => !(c is TerrainCollider)).ToList();
                lines.Add($"{ml.name}: largest step between rays {worst:0.000} m at {worstAt:0.00}; body touches: " + (hits.Count == 0 ? "nothing" : string.Join("; ", hits.Select(h => $"{h.Key} x{h.Value.n} ({h.Value.lo:0.0} to {h.Value.hi:0.0} m)"))) + string.Concat(near.Select(Above)));
                lines.Add("   " + string.Join(" ", rays));
            }
            File.WriteAllLines(outPath, lines);
            Debug.Log("[Round25] manhole probe: " + string.Join(" | ", lines.Where(l => !l.StartsWith("   "))));
            EditorApplication.Exit(0);
        }
        static string Arg(string name) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
