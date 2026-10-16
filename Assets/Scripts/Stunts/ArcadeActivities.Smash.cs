using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;
namespace Racer
{
    // 0.111 Part B: a smash site's area is its own props list. While a smash attempt runs every prop of the area still standing carries a small
    // orange marker over it, so what counts can be seen and anything beyond is clearly not part of it. The HUD line says the count, what the area
    // holds and the next medal. Chastain Rush, Round 36: the props are breakable panels (ActivitySite.propChunks / propPanels).
    public sealed partial class ArcadeActivities
    {
        // the props in the selected smash area
        public int SmashArea => Selected && Selected.kind == ActivitySite.Kind.Smash ? Selected.PropCount : 0;
        // the moment of the last counted smash (the "+1" pop)
        public float SmashPop { get; private set; } = -10;
        // Free Roam's attempt line: "7 of 14 smashed  ·  3 more for gold"
        string SmashLine { get { if (!Selected || !car) return SmashCount + " smashed"; Selected.Targets(configuration.profileId, out float b, out float s, out float g); return $"{SmashCount} of {SmashArea} smashed  ·  {NextMedal(SmashCount, b, s, g)}"; } }
        public static string NextMedal(int count, float bronze, float silver, float gold)
        {
            if (count >= gold) return "GOLD: keep going";
            var (target, name) = count < bronze ? (bronze, "bronze") : count < silver ? (silver, "silver") : (gold, "gold");
            int more = Mathf.Max(1, Mathf.CeilToInt(target) - count); return $"{more} more for {name}";
        }
        readonly Dictionary<int, Transform> markers = new(); static Material markerMaterial; static Mesh markerMesh; ActivitySite markersFor;
        void LateUpdate()
        {
            bool on = AttemptActive && Selected && Selected.kind == ActivitySite.Kind.Smash && FreeRoam;
            if (!on || markersFor != Selected) { if (markers.Count > 0) ClearMarkers(); markersFor = on ? Selected : null; if (!on) return; }
            for (int i = 0; i < Selected.PropCount; i++)
            {
                bool standing = !Selected.PropDown(i);
                markers.TryGetValue(i, out var m);
                if (!standing) { if (m) m.gameObject.SetActive(false); continue; }
                if (!m) markers[i] = m = Marker(Selected.PropAt(i));
                m.gameObject.SetActive(true);
                m.localRotation = Quaternion.Euler(0, Time.time * 90 % 360, 45); m.localPosition = Vector3.up * Mathf.Sin(Time.time * 3 + i) * .08f;
            }
        }
        Transform Marker(Vector3 at)
        {
            if (!markerMaterial) { markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Smash target marker" }; markerMaterial.SetColor("_BaseColor", new Color(1f, .45f, .05f)); }
            if (!markerMesh) { var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); markerMesh = cube.GetComponent<MeshFilter>().sharedMesh; Destroy(cube); }
            var anchor = new GameObject("Smash marker").transform; anchor.position = at + Vector3.up * 1.6f;
            var shape = new GameObject("Diamond"); shape.transform.SetParent(anchor, false); shape.transform.localScale = Vector3.one * .3f;
            shape.AddComponent<MeshFilter>().sharedMesh = markerMesh; var mr = shape.AddComponent<MeshRenderer>(); mr.sharedMaterial = markerMaterial; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return shape.transform;
        }
        void ClearMarkers() { foreach (var m in markers.Values) if (m) Destroy(m.parent ? m.parent.gameObject : m.gameObject); markers.Clear(); }
        void OnDisable() => ClearMarkers();
    }
}
