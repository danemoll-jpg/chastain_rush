using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 24 Part A2, item A2 (report only): every pipe mouth, exit and manhole in the built scene against the drivable
    // surfaces: OSM roads (with their sidewalk band beside residential and service streets), driveways and the Noonday Creek
    // trail. A vertex of a cut's walls, coping, headwall, railing, outfall lip, or a manhole's lid / collar that stands more
    // than 5 cm above the ground over such a surface is something a vehicle catches; a vertex of a cut more than 0.3 m under
    // the ground there is a cut into it. Writes the list per object.
    //   Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod ChastainRush.MouthScan.Run -mouthOut <file>
    public static class MouthScan
    {
        public static void Run()
        {
            var a = Environment.GetCommandLineArgs(); int ai = Array.IndexOf(a, "-mouthOut");
            string outFile = ai >= 0 ? a[ai + 1] : "mouth_scan.txt";
            string root = Path.GetDirectoryName(Application.dataPath);
            DriveBands.Load(root);

            EditorSceneManager.OpenScene(MapBuilder.ScenePath, OpenSceneMode.Single);
            var terrains = Terrain.activeTerrains;
            float Ground(Vector3 w)
            {
                foreach (var t in terrains)
                {
                    var o = t.transform.position; var s = t.terrainData.size;
                    if (w.x >= o.x && w.x <= o.x + s.x && w.z >= o.z && w.z <= o.z + s.z) return t.SampleHeight(w) + o.y;
                }
                return float.NaN;
            }
            string Band(Vector2 p) => DriveBands.At(p);
            var sb = new StringBuilder();
            sb.AppendLine($"Mouth scan, {DateTime.Now:yyyy-MM-dd HH:mm}: parts more than 5 cm over (or cuts more than 0.3 m into) a road, sidewalk band, driveway or the creek trail");
            int hitsTotal = 0;
            foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string n = mf.gameObject.name;
                bool cut = n.EndsWith("cuts and headwalls") || n.Contains("outfall lip");
                bool part = cut || n.EndsWith(" railings") || n.Contains("grates") || n.Contains("rock bar") || (n.StartsWith("Manhole ") && (n.EndsWith(" lid") || n.EndsWith(" collar")));
                if (!part || mf.sharedMesh == null) continue;
                var verts = mf.sharedMesh.vertices; var tr = mf.transform;
                var found = new Dictionary<string, (int n, Vector3 lo, Vector3 hi, float most)>();
                foreach (var v in verts)
                {
                    var w = tr.TransformPoint(v); float g = Ground(w); if (float.IsNaN(g)) continue;
                    float over = w.y - g;
                    bool proud = over > 0.05f, sunk = cut && over < -0.3f;
                    if (!proud && !sunk) continue;
                    string b = Band(new Vector2(w.x, w.z)); if (b == null) continue;
                    string key = (proud ? "stands up over " : "cut into ") + b;
                    if (!found.TryGetValue(key, out var f)) f = (0, w, w, 0f);
                    f.n++; f.lo = Vector3.Min(f.lo, w); f.hi = Vector3.Max(f.hi, w); f.most = Mathf.Max(f.most, Mathf.Abs(over));
                    found[key] = f;
                }
                foreach (var kv in found)
                {
                    hitsTotal++;
                    sb.AppendLine($"{n}: {kv.Key}: {kv.Value.n} vertices, x {kv.Value.lo.x:0.0}..{kv.Value.hi.x:0.0}, z {kv.Value.lo.z:0.0}..{kv.Value.hi.z:0.0}, up to {kv.Value.most:0.00} m");
                }
            }
            sb.AppendLine($"{hitsTotal} findings");
            File.WriteAllText(outFile, sb.ToString());
            Debug.Log("[MouthScan] " + sb);
        }
    }
}
