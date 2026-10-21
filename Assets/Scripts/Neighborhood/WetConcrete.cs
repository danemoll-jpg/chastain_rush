using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 53 (A, Dan 2026-10-20: "A whole part of the road is all freshly poured concrete if you try to drive through it you are slowed like you
    // would be if you were driving in water"): the road crew's fresh concrete across Lyndon Drive. A vehicle with two wheels down inside its outline
    // is slowed as in deep water (ArcadeVehicle wading: half the engine and a drag of 0.95 per second, settling at about 5 m/s; here one drag of 4 per
    // second on the horizontal speed, settling at about 3 m/s: Round 53 check, tuning try 1, at 2 per second a car driven round the trailer and
    // through the slab lost only 3 s, a third of what the shortcut saves), and its wheels leave ruts in the slab. The vehicle drives on the
    // road's ground under the slab (the slab has no collider, as the roads), so it sinks into it a little. A jump that carries over it never
    // touches it. World, not race layer: the slab is there in Free Roam too.
    [DefaultExecutionOrder(100)]
    public sealed class WetConcrete : MonoBehaviour
    {
        public Vector2[] outline;            // x, z
        public Material rutMaterial;         // vertex coloured (the map's House material)
        public const float Drag = 4.0f;      // per second of the horizontal speed
        const int MaxRuts = 1200;            // the oldest are reused
        const float RutStep = 0.35f, RutW = 0.32f;
        public int RutsMade { get; private set; }   // checks

        Rect bounds;
        Mesh rutMesh; Vector3[] rv; Color[] rc; int next;
        readonly System.Collections.Generic.Dictionary<ArcadeVehicle, Vector3[]> last = new();
        readonly System.Collections.Generic.Dictionary<ArcadeVehicle, bool> bikes = new();

        void Awake()
        {
            Vector2 lo = outline[0], hi = outline[0]; foreach (var q in outline) { lo = Vector2.Min(lo, q); hi = Vector2.Max(hi, q); }
            bounds = Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
            rv = new Vector3[MaxRuts * 4]; rc = new Color[MaxRuts * 4]; var tris = new int[MaxRuts * 6];
            for (int i = 0; i < MaxRuts; i++) { int v = i * 4, t = i * 6; tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1; tris[t + 3] = v; tris[t + 4] = v + 3; tris[t + 5] = v + 2; }
            for (int i = 0; i < rv.Length; i++) rv[i] = new Vector3(bounds.center.x, -1000f, bounds.center.y);
            rutMesh = new Mesh { name = "Concrete ruts" }; rutMesh.MarkDynamic(); rutMesh.vertices = rv; rutMesh.colors = rc; rutMesh.triangles = tris;
            rutMesh.bounds = new Bounds(new Vector3(bounds.center.x, 0f, bounds.center.y), new Vector3(bounds.width + 4f, 4000f, bounds.height + 4f));
            var go = new GameObject("Concrete ruts"); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = rutMesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = rutMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        void OnDestroy() { if (rutMesh) Destroy(rutMesh); }

        public bool Contains(Vector3 p)
        {
            var q = new Vector2(p.x, p.z); if (!bounds.Contains(q)) return false;
            bool inside = false;
            for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
                if ((outline[i].y > q.y) != (outline[j].y > q.y) && q.x < (outline[j].x - outline[i].x) * (q.y - outline[i].y) / (outline[j].y - outline[i].y) + outline[i].x) inside = !inside;
            return inside;
        }

        void FixedUpdate()
        {
            bool changed = false;
            foreach (var car in DrainPipe.Vehicles)
            {
                if (!car || !car.gameObject.activeInHierarchy || !car.Body || car.Body.isKinematic) continue;
                var p = car.Body.position;
                if (car.GroundedWheels < 2 || !Contains(p) || !Physics.Raycast(p, Vector3.down, out var hit, car.suspensionLength + 0.6f, 1, QueryTriggerInteraction.Ignore) || !(hit.collider is TerrainCollider))
                { last.Remove(car); continue; }
                var horizontal = Vector3.ProjectOnPlane(car.Body.linearVelocity, Vector3.up);
                car.Body.AddForce(-horizontal * Mathf.Min(Drag, 0.8f / Time.fixedDeltaTime), ForceMode.Acceleration);
                // the ruts: under each side of the vehicle, a mark every RutStep of travel
                var right = Vector3.ProjectOnPlane(car.transform.right, Vector3.up).normalized;
                if (!bikes.TryGetValue(car, out bool bike)) { var cfg = car.GetComponent<VehicleConfiguration>(); bikes[car] = bike = cfg && cfg.Profile != null && cfg.Profile.Motorcycle; }
                float half = bike ? 0f : 0.78f;   // a motorcycle leaves one rut
                var wheels = half > 0f ? new[] { hit.point + right * half, hit.point - right * half } : new[] { hit.point };
                if (!last.TryGetValue(car, out var prev) || prev.Length != wheels.Length) { last[car] = wheels; continue; }
                for (int k = 0; k < wheels.Length; k++)
                {
                    var d = wheels[k] - prev[k]; d.y = 0f; if (d.magnitude < RutStep) continue;
                    if (d.magnitude < 3f) { AddRut(prev[k], wheels[k], right); changed = true; }
                    prev[k] = wheels[k];
                }
            }
            if (changed) { rutMesh.vertices = rv; rutMesh.colors = rc; }
        }

        void AddRut(Vector3 a, Vector3 b, Vector3 right)
        {
            int v = next * 4; next = (next + 1) % MaxRuts; RutsMade++;
            var s = right * (RutW * 0.5f); var up = Vector3.up * 0.135f;   // on the slab (0.12 over the road's ground), the rut a shade lower in look only
            rv[v] = a - s + up; rv[v + 1] = a + s + up; rv[v + 2] = b + s + up; rv[v + 3] = b - s + up;
            var c = new Color(0.25f, 0.26f, 0.27f); for (int i = 0; i < 4; i++) rc[v + i] = c;
        }

        // the strongest slab at a point (checks)
        public static bool At(Vector3 p) { foreach (var w in FindObjectsByType<WetConcrete>(FindObjectsSortMode.None)) if (w.Contains(p)) return true; return false; }
    }
}
