using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace ChastainRush
{
    // Round 65 (build tools): a measurement only. What writing generated meshes costs: one asset per mesh (CreateAsset, about 30 ms each whatever its size or
    // folder: probe 1 and 2) against many meshes as sub-assets of one asset file (AddObjectToAsset), in batch: -executeMethod ChastainRush.BuildSpeedProbe.Run
    // (writes Assets/BuildSpeedProbeTmp/, deleted after; the result in the log)
    public static class BuildSpeedProbe
    {
        static Mesh Small() { var m = new Mesh(); m.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }; m.triangles = new[] { 0, 1, 2 }; return m; }
        public static void Run()
        {
            const string dir = "Assets/BuildSpeedProbeTmp"; AssetDatabase.DeleteAsset(dir); AssetDatabase.CreateFolder("Assets", "BuildSpeedProbeTmp");
            try
            {
                foreach (int per in new[] { 1, 50, 500 })
                {
                    AssetDatabase.StartAssetEditing(); var sw = Stopwatch.StartNew(); Object main = null; int files = 0;
                    for (int k = 0; k < 1000; k++)
                    {
                        var m = Small(); m.name = $"m{k}";
                        if (k % per == 0) { AssetDatabase.CreateAsset(m, $"{dir}/p{per}_{k}.asset"); main = m; files++; }
                        else AssetDatabase.AddObjectToAsset(m, main);
                    }
                    var made = sw.Elapsed.TotalSeconds; AssetDatabase.StopAssetEditing(); var all = sw.Elapsed.TotalSeconds;
                    UnityEngine.Debug.Log($"[BuildSpeedProbe] 1000 meshes, {per} per file ({files} files): written {made:0.00} s, with the import {all:0.00} s");
                }
            }
            finally { AssetDatabase.DeleteAsset(dir); }
            EditorApplication.Exit(0);
        }
    }
}
