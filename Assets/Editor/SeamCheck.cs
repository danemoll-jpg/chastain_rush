using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Development check: are the terrain tiles of the built scene seamless? For every pair of neighbouring tiles it
    // compares the shared edge samples (must be identical) and the height step across the seam against the steepest
    // step between neighbouring samples elsewhere in the same tile (a seam step is a problem only if it stands out).
    // Batch: Unity.exe -batchmode -nographics -projectPath . -executeMethod ChastainRush.SeamCheck.Run -seamOut <file>
    public static class SeamCheck
    {
        // tiles are named Terrain_<column>_<row>; the saved scene does not keep the neighbour links, so find them by name
        static Terrain Find(Terrain t, int dx, int dz)
        {
            var p = t.name.Split('_');
            var go = GameObject.Find($"Terrain_{int.Parse(p[1]) + dx}_{int.Parse(p[2]) + dz}");
            return go ? go.GetComponent<Terrain>() : null;
        }

        public static void Run()
        {
            string outFile = Path.GetFullPath("Temp/seam_check.txt");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-seamOut") outFile = args[i + 1];
            Directory.CreateDirectory(Path.GetDirectoryName(outFile));
            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            var sb = new StringBuilder();
            var terrains = Terrain.activeTerrains;
            int pairs = 0;
            float worstEdge = 0f, worstStep = 0f, worstInterior = 0f;
            foreach (var a in terrains)
            {
                int res = a.terrainData.heightmapResolution;
                var ha = a.terrainData.GetHeights(0, 0, res, res);
                float scale = a.terrainData.size.y;
                foreach (var b in new[] { Find(a, 1, 0), Find(a, 0, 1) })
                {
                    if (b == null) continue;
                    bool right = Find(a, 1, 0) == b;
                    var hb = b.terrainData.GetHeights(0, 0, res, res);
                    pairs++;
                    for (int k = 0; k < res; k++)
                    {
                        float ea = right ? ha[k, res - 1] : ha[res - 1, k];
                        float eb = right ? hb[k, 0] : hb[0, k];
                        worstEdge = Mathf.Max(worstEdge, Mathf.Abs(ea - eb) * scale);
                        float next = right ? hb[k, 1] : hb[1, k];
                        worstStep = Mathf.Max(worstStep, Mathf.Abs(next - eb) * scale);
                    }
                }
                for (int z = 0; z < res - 1; z += 3)
                    for (int x = 0; x < res - 1; x += 3)
                        worstInterior = Mathf.Max(worstInterior, Mathf.Abs(ha[z, x + 1] - ha[z, x]) * scale, Mathf.Abs(ha[z + 1, x] - ha[z, x]) * scale);
            }
            sb.AppendLine($"{terrains.Length} terrain tiles, {pairs} neighbouring pairs checked");
            sb.AppendLine($"largest height difference between a shared edge sample on the two tiles: {worstEdge:F5} m (0 = identical)");
            sb.AppendLine($"largest height step from a seam sample to the next sample inside the neighbour: {worstStep:F3} m");
            sb.AppendLine($"largest height step between neighbouring samples inside the tiles (sampled): {worstInterior:F3} m");
            File.WriteAllText(outFile, sb.ToString());
            Debug.Log("[SeamCheck]\n" + sb);
            EditorApplication.Exit(0);
        }
    }
}
