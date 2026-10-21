using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 54 (D2, Dan BUG-007: the PARTY sign drawn solid magenta): what in the built scene would draw magenta: a renderer with an empty material
    // slot (its material asset made again over itself in the same build), or a material whose shader is missing or does not compile.
    // Editor batch, no Play mode: ChastainRush.MaterialScan.Run -scanOut <file>
    public static class MaterialScan
    {
        public static void Run()
        {
            string outPath = Arg("-scanOut") ?? "material_scan.txt";
            EditorSceneManager.OpenScene("Assets/Generated/ChastainPrototype.unity");
            var sb = new StringBuilder(); int renderers = 0, bad = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                renderers++;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; string why = null;
                    if (m == null) why = "no material";
                    else if (m.shader == null) why = $"{m.name}: no shader";
                    else if (m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported) why = $"{m.name}: shader {m.shader.name} not supported";
                    if (why == null) continue;
                    bad++; var p = r.transform.position;
                    sb.AppendLine($"{Path(r.transform)} slot {i}: {why} at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0})");
                }
            }
            File.WriteAllText(outPath, $"{renderers} renderers, {bad} empty or broken material slots\n" + sb);
            EditorApplication.Exit(0);
        }
        static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
        static string Arg(string name) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
