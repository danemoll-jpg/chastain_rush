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
    // Round 24 Part A3, item B0 (report only): every house, garage, fence, deck, pool and other built thing in the built scene
    // against the terrain holes the pipes and the network cut (mouths, exits, manholes, glass stretches, the hub's grates). A
    // vertex of something that is not part of the pipes, standing within 1.5 m of the ground, over a hole cell (or within 1 m
    // of one) means that thing stands over a cut: a floating house or fence, or one with a notch beside it. Roads and drives
    // over a manhole's hole are left out (the lid sits in them on purpose). Also writes every hole cell for the plot
    // (Tools/mapgen/network_sweep.py).
    //   Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod ChastainRush.NetworkSweep.Run -sweepOut <dir>
    public static class NetworkSweep
    {
        static readonly string[] Skip = { "PipeNetwork", "Pipes", "Terrain", "Race", "Proposal", "WaterVolumes", "Stream water", "Sun", "MapCredits", "Geese" };
        static readonly string[] Paved = { "road", "Road", "drive", "Drive", "street", "Street", "paint", "Paint", "curb", "Curb", "sidewalk", "Sidewalk" };

        public static void Run()
        {
            var a = Environment.GetCommandLineArgs(); int ai = Array.IndexOf(a, "-sweepOut");
            string outDir = ai >= 0 ? a[ai + 1] : "network_sweep";
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene(MapBuilder.ScenePath, OpenSceneMode.Single);
            var terrains = Terrain.activeTerrains;

            // every hole cell, by a 1 m key, for the lookup
            var hole = new HashSet<long>(); var cells = new StringBuilder();
            long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
            foreach (var t in terrains)
            {
                var d = t.terrainData; int hr = d.holesResolution; var s = d.size; var o = t.transform.position;
                var h = d.GetHoles(0, 0, hr, hr); float cx = s.x / hr, cz = s.z / hr;
                for (int j = 0; j < hr; j++) for (int i = 0; i < hr; i++)
                    {
                        if (h[j, i]) continue;
                        float x0 = o.x + i * cx, z0 = o.z + j * cz;
                        cells.AppendLine($"{x0 + cx * 0.5f:0.00} {z0 + cz * 0.5f:0.00}");
                        for (int zz = Mathf.FloorToInt(z0); zz <= Mathf.FloorToInt(z0 + cz); zz++)
                            for (int xx = Mathf.FloorToInt(x0); xx <= Mathf.FloorToInt(x0 + cx); xx++) hole.Add(Key(xx, zz));
                    }
            }
            File.WriteAllText(Path.Combine(outDir, "holes.txt"), cells.ToString());
            bool NearHole(float x, float z, float r)
            {
                for (int zz = Mathf.FloorToInt(z - r); zz <= Mathf.FloorToInt(z + r); zz++)
                    for (int xx = Mathf.FloorToInt(x - r); xx <= Mathf.FloorToInt(x + r); xx++) if (hole.Contains(Key(xx, zz))) return true;
                return false;
            }
            float Ground(Vector3 w)
            {
                foreach (var t in terrains)
                {
                    var o = t.transform.position; var s = t.terrainData.size;
                    if (w.x >= o.x && w.x <= o.x + s.x && w.z >= o.z && w.z <= o.z + s.z) return t.SampleHeight(w) + o.y;
                }
                return float.NaN;
            }
            string PathOf(Transform tr) { var p = tr.name; while (tr.parent != null) { tr = tr.parent; p = tr.name + "/" + p; } return p; }

            int pi = Array.IndexOf(a, "-sweepProfile");   // x0,z0,x1,z1: the ground every 0.5 m along a line, and whether it is holed
            if (pi >= 0)
            {
                var v = a[pi + 1].Split(',').Select(float.Parse).ToArray(); var pr = new StringBuilder();
                var p0 = new Vector2(v[0], v[1]); var p1 = new Vector2(v[2], v[3]); float len = (p1 - p0).magnitude;
                for (float s = 0f; s <= len; s += 0.5f) { var q = Vector2.Lerp(p0, p1, s / len); pr.AppendLine($"{s:0.0}	{q.x:0.0}	{q.y:0.0}	{Ground(new Vector3(q.x, 0f, q.y)):0.00}	{(NearHole(q.x, q.y, 0f) ? "hole" : "")}"); }
                File.WriteAllText(Path.Combine(outDir, "profile.txt"), pr.ToString());
            }
            var sb = new StringBuilder();
            sb.AppendLine($"Network sweep, {DateTime.Now:yyyy-MM-dd HH:mm}: built things standing over (or within 1 m of) a terrain hole cut by the pipes and the network; {hole.Count} 1 m cells holed");
            int found = 0;
            foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mf.sharedMesh == null) continue;
                string path = PathOf(mf.transform);
                if (Skip.Any(k => path.Contains(k))) continue;
                bool paved = Paved.Any(k => path.Contains(k));
                var verts = mf.sharedMesh.vertices; var tr = mf.transform;
                int n = 0; Vector3 lo = Vector3.positiveInfinity, hi = Vector3.negativeInfinity;
                foreach (var v in verts)
                {
                    var w = tr.TransformPoint(v); float g = Ground(w); if (float.IsNaN(g)) continue;
                    if (w.y - g > 1.5f) continue;
                    if (!NearHole(w.x, w.z, paved ? 0f : 1f)) continue;
                    n++; lo = Vector3.Min(lo, w); hi = Vector3.Max(hi, w);
                }
                if (n == 0) continue;
                found++;
                sb.AppendLine($"{path}\t{(paved ? "paved" : "thing")}\t{n} vertices\tx {lo.x:0.0}..{hi.x:0.0}\tz {lo.z:0.0}..{hi.z:0.0}");
            }
            sb.AppendLine($"{found} objects");
            File.WriteAllText(Path.Combine(outDir, "sweep.txt"), sb.ToString());
            Debug.Log("[NetworkSweep] " + sb);
        }
    }
}
