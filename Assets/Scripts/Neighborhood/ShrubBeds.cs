using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 50 (A, Dan 2026-10-19: "bushes that you can drive through but will slow you down, this is to prevent people from just driving
    // through here"): shrub beds, brush and reeds a vehicle drives through but that slow it hard. Ported from Racer's ShortcutUndergrowth
    // ("Bushes resist ground travel smoothly; the actual terrain remains the sole collision surface"): the same drag on the horizontal speed
    // while two wheels are down on the ground inside a bed. Chastain Rush changes: beds are lines with a half width (Racer: round patches of
    // one radius); the ground is any TerrainCollider (Racer: colliders named "Ground_"); the drag a constant per second of 2.5 (Racer: 6,
    // which all but stops a car: here a car crawls on through at 3 m/s or so and is never stuck); Racer's protected main road, cleared
    // corridor and reset past the brush are left behind (the beds stand off the roads; the map build places them, MapBuilder.LakesKicker.cs).
    // World, not race layer: the beds are there in Free Roam too.
    [DefaultExecutionOrder(100)]
    public sealed class ShrubBeds : MonoBehaviour
    {
        public Vector2[] a, b;          // each bed's line, piece by piece (x, z)
        public float[] half;            // the half width of the bed each piece belongs to
        public float[] strength;        // Round 56: the drag's multiple for each piece (none: 1; the Elaine jump's bank, 2: brush on a steep downhill slows a car too little)
        public const float Drag = 2.5f; // per second of the horizontal speed, at full strength
        public const float Fade = 1.5f; // full strength to this far inside a bed's edge

        Rect bounds;
        // Round 61 (C): the pieces by 32 m cell (the beds are spread over several races' ground: every piece was tested for every vehicle
        // on every physics step)
        const float Cell = 32f;
        readonly System.Collections.Generic.Dictionary<Vector2Int, int[]> cells = new System.Collections.Generic.Dictionary<Vector2Int, int[]>();
        static readonly int[] None = new int[0];

        void Awake()
        {
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            var lists = new System.Collections.Generic.Dictionary<Vector2Int, System.Collections.Generic.List<int>>();
            for (int i = 0; i < a.Length; i++)
            {
                float px0 = Mathf.Min(a[i].x, b[i].x) - half[i], px1 = Mathf.Max(a[i].x, b[i].x) + half[i], pz0 = Mathf.Min(a[i].y, b[i].y) - half[i], pz1 = Mathf.Max(a[i].y, b[i].y) + half[i];
                x0 = Mathf.Min(x0, px0); x1 = Mathf.Max(x1, px1); z0 = Mathf.Min(z0, pz0); z1 = Mathf.Max(z1, pz1);
                for (int cx = Mathf.FloorToInt(px0 / Cell); cx <= Mathf.FloorToInt(px1 / Cell); cx++)
                    for (int cz = Mathf.FloorToInt(pz0 / Cell); cz <= Mathf.FloorToInt(pz1 / Cell); cz++)
                    {
                        var k = new Vector2Int(cx, cz); if (!lists.TryGetValue(k, out var l)) lists[k] = l = new System.Collections.Generic.List<int>(); l.Add(i);
                    }
            }
            foreach (var kv in lists) cells[kv.Key] = kv.Value.ToArray();
            bounds = Rect.MinMaxRect(x0, z0, x1, z1);
        }

        // 0 outside every bed, 1 more than Fade inside one (times its strength)
        public float Coverage(Vector3 position)
        {
            var q = new Vector2(position.x, position.z);
            if (!bounds.Contains(q)) return 0f;
            float best = 0f;
            if (!cells.TryGetValue(new Vector2Int(Mathf.FloorToInt(q.x / Cell), Mathf.FloorToInt(q.y / Cell)), out var near)) near = None;
            foreach (int i in near)
            {
                var ab = b[i] - a[i]; float t = Mathf.Clamp01(Vector2.Dot(q - a[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                float d = (a[i] + ab * t - q).magnitude;
                if (d < half[i]) best = Mathf.Max(best, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(half[i], half[i] - Fade, d)) * (strength != null && i < strength.Length ? strength[i] : 1f));
            }
            return best;
        }

        void FixedUpdate()
        {
            foreach (var car in ArcadeVehicle.Live)   // Round 61 (C): the vehicles' own list
            {
                if (!car || !car.gameObject.activeInHierarchy || !car.Body || car.Body.isKinematic || car.GroundedWheels < 2) continue;
                float amount = Coverage(car.Body.position);
                if (amount <= 0f || !Physics.Raycast(car.Body.position, Vector3.down, out var hit, car.suspensionLength + 0.5f, 1, QueryTriggerInteraction.Ignore)
                    || !(hit.collider is TerrainCollider)) continue;
                var horizontal = Vector3.ProjectOnPlane(car.Body.linearVelocity, Vector3.up);
                car.Body.AddForce(-horizontal * Mathf.Min(Drag * amount, 0.8f / Time.fixedDeltaTime), ForceMode.Acceleration);
            }
        }

        // the strongest bed at a point (checks)
        public static float CoverageAt(Vector3 position)
        {
            float best = 0f;
            foreach (var s in FindObjectsByType<ShrubBeds>(FindObjectsSortMode.None)) best = Mathf.Max(best, s.Coverage(position));
            return best;
        }
    }
}
