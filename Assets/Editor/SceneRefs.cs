using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ChastainRush
{
    // Round 42 (Part A): the references a scene's components hold to other objects of the scene, by where those sit in the hierarchy. A quick
    // build (MapBuilder.Quick.cs) remakes some objects: the references other objects held to them are set again to the new ones in the same
    // places; SceneDump writes them into the scene's fingerprint, so a reference a quick build lost shows as a difference.
    public static class SceneRefs
    {
        // a scene object's place: the names and sibling indices from the scene's root, e.g. "/World#3/Businesses#12/x#0"
        public static string Place(Transform t) => (t.parent ? Place(t.parent) : "") + "/" + t.name + "#" + t.GetSiblingIndex();
        public static string Path(Transform t) => (t.parent ? Path(t.parent) : "") + "/" + t.name;

        // every object reference of a component (arrays of plain values are not walked: they hold none)
        public static IEnumerable<(string prop, Object target)> Of(Component c)
        {
            var so = new SerializedObject(c); var p = so.GetIterator(); bool enter = true;
            while (p.Next(enter))
            {
                enter = true;
                if (p.propertyType == SerializedPropertyType.String) { enter = false; continue; }
                if (p.isArray && p.propertyType == SerializedPropertyType.Generic && p.arraySize > 0)
                {
                    var e0 = p.GetArrayElementAtIndex(0);
                    if (e0.propertyType != SerializedPropertyType.ObjectReference && (e0.propertyType != SerializedPropertyType.Generic || !HasRef(e0))) { enter = false; continue; }
                }
                if (p.propertyType == SerializedPropertyType.ObjectReference && p.name != "m_Script" && p.name != "m_GameObject") yield return (p.propertyPath, p.objectReferenceValue);
            }
        }

        static bool HasRef(SerializedProperty e)
        {
            var it = e.Copy(); var end = e.GetEndProperty(); bool enter = true;
            while (it.Next(enter) && !SerializedProperty.EqualContents(it, end)) { enter = !it.isArray; if (it.propertyType == SerializedPropertyType.ObjectReference) return true; }
            return false;
        }

        // what a reference points at: a scene object by its path (and component type), an asset by its file, or null
        public static string Describe(Object o)
        {
            if (o == null) return "null";
            var go = o as GameObject ?? (o as Component)?.gameObject;
            if (go != null && go.scene.IsValid()) return Path(go.transform) + ":" + o.GetType().Name;
            if (o is Mesh m) return "mesh:" + SceneDump.MeshHash(m);   // by its contents (a quick build writes its meshes to another folder)
            var ap = AssetDatabase.GetAssetPath(o); return ap.Length > 0 ? ap + ":" + o.name : "mem:" + o.GetType().Name + ":" + o.name;
        }

        public sealed class Held { public Component holder; public string prop, place, type; public int componentIndex; }

        // the references from components outside 'gone' to objects inside it
        public static List<Held> Into(IEnumerable<GameObject> sceneRoots, HashSet<Transform> gone)
        {
            bool In(Transform t) { for (; t; t = t.parent) if (gone.Contains(t)) return true; return false; }
            var list = new List<Held>();
            foreach (var r in sceneRoots)
                foreach (var c in r.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (c == null || In(c.transform)) continue;
                    foreach (var (prop, target) in Of(c))
                    {
                        var go = target as GameObject ?? (target as Component)?.gameObject;
                        if (go == null || !go.scene.IsValid() || !In(go.transform)) continue;
                        var comps = go.GetComponents(target.GetType());
                        list.Add(new Held { holder = c, prop = prop, place = Place(go.transform), type = target is GameObject ? "GameObject" : target.GetType().AssemblyQualifiedName, componentIndex = target is Component tc ? System.Array.IndexOf(comps, tc) : 0 });
                    }
                }
            return list;
        }

        // set them again to what now sits in the same places; returns how many could not be
        public static int Relink(UnityEngine.SceneManagement.Scene scene, List<Held> held)
        {
            var byPlace = new Dictionary<string, Transform>();
            foreach (var r in scene.GetRootGameObjects()) foreach (var t in r.GetComponentsInChildren<Transform>(true)) byPlace[Place(t)] = t;
            int lost = 0;
            foreach (var grp in held.GroupBy(h => h.holder))
            {
                var so = new SerializedObject(grp.Key);
                foreach (var h in grp)
                {
                    Object target = null;
                    if (byPlace.TryGetValue(h.place, out var t))
                    {
                        if (h.type == "GameObject") target = t.gameObject;
                        else { var ty = System.Type.GetType(h.type); var cs = ty != null ? t.GetComponents(ty) : new Component[0]; target = h.componentIndex < cs.Length ? cs[h.componentIndex] : null; }
                    }
                    if (target == null) { lost++; continue; }
                    so.FindProperty(h.prop).objectReferenceValue = target;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return lost;
        }
    }
}
