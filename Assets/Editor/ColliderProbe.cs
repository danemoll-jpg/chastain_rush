using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 81 (F): every collider (active or not) in a rectangle of the built scene whose object's or parent's name holds a text, with whether it is a
    // trigger, enabled, active, and what race rule (RaceOnly) or breakable (FencePanel) it carries. Editor batch, report only:
    // ChastainRush.ColliderProbe.Run -probeRect x0,z0,x1,z1 [-probeName text] -probeOut <file>
    public static class ColliderProbe
    {
        public static void Run()
        {
            var r = Arg("-probeRect").Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            string name = Arg("-probeName") ?? ""; var lines = new List<string>();
            EditorSceneManager.OpenScene("Assets/Generated/ChastainPrototype.unity");
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var b = c.bounds; if (b.max.x < r[0] || b.min.x > r[2] || b.max.z < r[1] || b.min.z > r[3]) continue;
                string path = GetPath(c.transform); if (name.Length > 0 && path.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                var ro = c.GetComponentInParent<RaceOnly>(true);
                lines.Add($"{path}: {c.GetType().Name} trigger {c.isTrigger} enabled {c.enabled} active {c.gameObject.activeInHierarchy} bounds ({b.min.x:0.0}, {b.min.z:0.0})-({b.max.x:0.0}, {b.max.z:0.0}) y {b.min.y:0.0}-{b.max.y:0.0}" +
                          (ro ? $" RaceOnly(reverse {ro.reverse}, any {ro.any}, freeRoam {ro.freeRoam}, hideRev '{ro.hideInReverseOf}', showRev '{ro.showInReverseOf}', hideFwd '{ro.hideInForwardOf}', showFwd '{ro.showInForwardOf}')" : "") +
                          (c.GetComponent<FencePanel>() ? " FencePanel" : ""));
            }
            File.WriteAllLines(Arg("-probeOut") ?? "collider_probe.txt", lines.OrderBy(x => x)); EditorApplication.Exit(0);
        }
        static string GetPath(Transform t) => t.parent ? GetPath(t.parent) + "/" + t.name : t.name;
        static string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
