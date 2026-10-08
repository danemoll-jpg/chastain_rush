using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ChastainRush
{
    // Lists the meshes in the tree kit (Assets/Trees/SceneryTrees.fbx) with triangle counts.
    // Batch: Unity.exe -batchmode -nographics -projectPath . -executeMethod ChastainRush.TreeKitInfo.Run -quit
    public static class TreeKitInfo
    {
        public static void Run()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Trees/SceneryTrees.fbx");
            var sb = new StringBuilder("[TreeKitInfo]\n");
            foreach (var f in asset.GetComponentsInChildren<MeshFilter>(true).OrderBy(f => f.name))
            {
                var m = f.sharedMesh;
                sb.AppendLine($"{f.name}: {m.triangles.Length / 3} triangles, {m.vertexCount} vertices, bounds {m.bounds.min} .. {m.bounds.max}");
            }
            Debug.Log(sb.ToString());
        }
    }
}
