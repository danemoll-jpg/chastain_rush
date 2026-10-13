using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 19, item 2: one of the Noonday Creek trail's big jumps (MapBuilder.BigJumps.cs; R1's kids' ramp uses it too). The
    // take-off is built for two seconds of air at 24 m/s. Crossing the lip, a vehicle is sent off at the lip's own angle at its
    // speed (no faster than 'vmax'), with half of its sideways drift taken out: an aimed launch, as the trampoline's (arcade:
    // the trail vehicles arrive at up to 39 m/s, and on a short curved take-off at that speed their suspension cannot follow the
    // curve, so they left low and short of the two seconds). Faster than vmax it would overshoot the cleared landing into the
    // trees. Slower vehicles leave at the same angle at their own speed and land short on ground that is drivable. Launched: for
    // the checks. Round 20 (P1J, the outfall over the pond): 'vmin' > 0 sends a vehicle that crosses the lip at 10 m/s or more
    // off it at vmin at least, so everyone clears the water; a slower one just rolls off into the shallow edge.
    public sealed class BigJump : MonoBehaviour
    {
        public string id, look;
        public Vector3 lip, dir;          // the lip's middle (world), the jump's direction (flat, unit)
        public float halfWidth = 3.5f, vmax = 27f, angle = 20f, vmin = 0f;   // angle: the lip's, degrees
        public static int Governed { get; private set; }
        public static event System.Action<BigJump, ArcadeVehicle, float> Launched;   // the jump, the vehicle, its speed off the lip (before any cap)
        static readonly List<BigJump> all = new List<BigJump>();
        public static IReadOnlyList<BigJump> All => all;
        readonly Dictionary<Rigidbody, float> recent = new Dictionary<Rigidbody, float>();
        readonly Dictionary<Rigidbody, float> before = new Dictionary<Rigidbody, float>();   // where each vehicle was along the jump last step

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        void FixedUpdate()
        {
            var cars = DrainPipe.Vehicles;   // Round 24 A2: the shared once-a-second search (each jump searching on its own was a 12 to 19 ms hitch)
            var left = Vector3.Cross(Vector3.up, dir);
            foreach (var car in cars)
            {
                if (!car || car.Body == null || car.Body.isKinematic) continue;
                var d = car.Body.position - lip;
                float along = d.x * dir.x + d.z * dir.z, lat = Mathf.Abs(Vector3.Dot(d, left));
                float was = before.TryGetValue(car.Body, out float w) ? w : -99f; before[car.Body] = along;
                if (was >= 0f || along < 0f || along > 3f || lat > halfWidth + 1f || d.y < -2.5f || d.y > 6f) continue;   // crossing the lip's plane, this step (or flying just over it: off BJ2's crest)
                if (recent.TryGetValue(car.Body, out float at) && Time.time - at < 2f) continue;
                var v = car.Body.linearVelocity;
                if (Vector3.Dot(v, dir) < 2f) continue;   // going the other way, or barely moving
                recent[car.Body] = Time.time;
                float speed = v.magnitude;
                Launched?.Invoke(this, car, speed);
                if (speed > vmax) Governed++;
                float s = Mathf.Min(speed, vmax), a = angle * Mathf.Deg2Rad, side = Vector3.Dot(v, left) * 0.5f;
                if (vmin > 0f && speed >= 10f) s = Mathf.Max(s, vmin);
                car.Body.linearVelocity = dir * (s * Mathf.Cos(a)) + Vector3.up * (s * Mathf.Sin(a)) + left * side;
            }
        }
    }
}
