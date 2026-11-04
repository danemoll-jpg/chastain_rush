using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ChastainRush
{
    // Round 92 (item 0): the terrain tiles moved back from a trial under Assets/Generated/Resources/Terrain to Assets/Generated (where the map build writes them).
    // Moving keeps each asset's GUID, so the saved scene still points at them. Editor batch: -executeMethod ChastainRush.Round92Tools.MoveTerrain
    public static class Round92Tools
    {
        public static void MoveTerrain()
        {
            try
            {
                const string to = "Assets/Generated";
                
                int n = 0;
                foreach (var path in AssetDatabase.FindAssets("TerrainData t:TerrainData", new[] { "Assets/Generated/Resources/Terrain" }).Select(AssetDatabase.GUIDToAssetPath).ToList())
                {
                    
                    string err = AssetDatabase.MoveAsset(path, to + "/" + Path.GetFileName(path));
                    if (!string.IsNullOrEmpty(err)) throw new Exception(path + ": " + err);
                    n++;
                }
                AssetDatabase.SaveAssets(); AssetDatabase.DeleteAsset("Assets/Generated/Resources/Terrain");
                Debug.Log($"[Round92] moved {n} terrain tiles to {to}");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
