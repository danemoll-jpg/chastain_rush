using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 60 (Part A): the run-time half of Town Center (built by MapBuilder.Mall.cs / MallInside.cs):
    //   the inside (shops, the food court, the smashables, the lights) drawn only while the camera is within NearDraw of the building (the frame rate);
    //   its breakables put back when the player is more than RestoreAway from it or resets in it (as the stunt park's);
    //   at night the nearest ceiling lights carry real point lights (a pool of NearLights, as NightLamps), while the camera is inside.
    public sealed class TownCenterMall : MonoBehaviour
    {
        public const float NearDraw = 140f, RestoreAway = 220f; const int NearLights = 8;
        public Vector2[] outline;            // the building's outline (the concourse and its anchors' bounding box), x / z
        public Renderer[] inside;
        public FenceChunk[] breakables;
        public Vector3[] lamps;
        public float lowerFloor, upperFloor, roof;
        public Vector2 frameU = Vector2.right, frameV = Vector2.up;   // the building's frame (u along the concourse, v across it; checks place things by it)
        public Vector3 P(float u, float v, float y) { var w = frameU * u + frameV * v; return new Vector3(w.x, y, w.y); }
        public static TownCenterMall Current { get; private set; }

        ArcadeVehicle player; VehicleRespawn respawn; bool shown = true, away = true; float nextLook; Light[] pool; bool[] wasOn;
        public bool Shown => shown;   // checks

        void Awake() { Current = this; }
        void OnDestroy() { if (Current == this) Current = null; }
        void Start()
        {
            var sw = FindAnyObjectByType<VehicleSwitch>(); player = sw ? sw.GetComponent<ArcadeVehicle>() : FindAnyObjectByType<ArcadeVehicle>();
            respawn = player ? player.GetComponent<VehicleRespawn>() : null; if (respawn) respawn.Respawned += OnReset;
            pool = new Light[NearLights];
            for (int i = 0; i < NearLights; i++)
            {
                var l = new GameObject("Mall light").AddComponent<Light>(); l.transform.SetParent(transform, false);
                l.type = LightType.Point; l.range = 18f; l.color = new Color(1f, .93f, .82f); l.shadows = LightShadows.None; l.enabled = false; pool[i] = l;
            }
        }
        void OnReset() { if (player && Distance(player.Body.position) < 30f) RestoreBreakables(); }

        // how far a point is from the building (0 inside its box)
        public float Distance(Vector3 p)
        {
            if (outline == null || outline.Length < 4) return 1e6f;
            float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (var q in outline) { x0 = Mathf.Min(x0, q.x); x1 = Mathf.Max(x1, q.x); z0 = Mathf.Min(z0, q.y); z1 = Mathf.Max(z1, q.y); }
            float dx = Mathf.Max(0f, Mathf.Max(x0 - p.x, p.x - x1)), dz = Mathf.Max(0f, Mathf.Max(z0 - p.z, p.z - z1));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
        public bool Inside(Vector3 p)
        {
            if (outline == null || outline.Length < 3) return false; var q = new Vector2(p.x, p.z); bool inside = false;
            for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
            { Vector2 a = outline[i], b = outline[j]; if ((a.y > q.y) != (b.y > q.y) && q.x < (b.x - a.x) * (q.y - a.y) / (b.y - a.y) + a.x) inside = !inside; }
            return inside && p.y > lowerFloor - 3f && p.y < roof + 1f;
        }

        void Update()
        {
            var cam = Camera.main;
            if (Time.time >= nextLook)
            {
                nextLook = Time.time + .25f;
                bool near = false;
                if (cam) near |= Distance(cam.transform.position) < NearDraw;
                if (player) near |= Distance(player.Body.position) < NearDraw;
                if (near != shown) Show(near);
                if (player)
                {
                    bool far = Distance(player.Body.position) > RestoreAway;
                    if (far && !away) RestoreBreakables();
                    away = far;
                }
            }
            NightLights(cam);
        }
        void Show(bool on)
        {
            shown = on; if (inside == null) return;
            if (wasOn == null || wasOn.Length != inside.Length) { wasOn = new bool[inside.Length]; for (int i = 0; i < inside.Length; i++) wasOn[i] = true; }
            for (int i = 0; i < inside.Length; i++)
            {
                var r = inside[i]; if (!r) continue;
                if (!on) { wasOn[i] = r.enabled; r.enabled = false; } else r.enabled = wasOn[i];
            }
        }
        void NightLights(Camera cam)
        {
            float level = VehicleLights.Level;
            bool use = cam && level > .02f && lamps != null && lamps.Length > 0 && Distance(cam.transform.position) < 6f;
            if (!use) { if (pool != null) foreach (var l in pool) if (l) l.enabled = false; return; }
            var c = cam.transform.position; var best = new List<(float d, Vector3 p)>();
            foreach (var p in lamps) { float d = (p - c).sqrMagnitude; if (best.Count < NearLights) best.Add((d, p)); else { int w = 0; for (int i = 1; i < best.Count; i++) if (best[i].d > best[w].d) w = i; if (d < best[w].d) best[w] = (d, p); } }
            for (int i = 0; i < pool.Length; i++)
            {
                if (i < best.Count) { pool[i].transform.position = best[i].p + Vector3.down * .4f; pool[i].intensity = 14f * level; pool[i].enabled = true; }
                else pool[i].enabled = false;
            }
        }
        public int RestoreBreakables() { if (breakables == null) return 0; int n = 0; foreach (var b in breakables) if (b && b.RestoreOwn()) n++; if (n > 0) Debug.Log($"[TownCenterMall] {n} breakable sets put back"); return n; }
    }
}
