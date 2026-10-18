using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 40 (A7, the church leap): a take-off's lip that sends a vehicle crossing it on an aimed flight (GuidedFlight) to a landing: the
    // church's window under the steeple, curving round onto New Chastain Road heading west. As BigJump's lip (the plane across 'dir' at
    // 'lip', within 'halfWidth'), from any speed of 2 m/s or more over it: the flight's speed is the one that lands it.
    public sealed class GuidedLaunch : MonoBehaviour
    {
        public string id;
        public Vector3 lip, dir = Vector3.forward, target, targetDir = Vector3.forward;
        public float halfWidth = 2.5f, angle = 38f, gravity = 1f;
        public static int Launches { get; private set; }
        readonly Dictionary<Rigidbody, float> before = new Dictionary<Rigidbody, float>(), recent = new Dictionary<Rigidbody, float>();

        void FixedUpdate()
        {
            var left = Vector3.Cross(Vector3.up, dir);
            foreach (var car in DrainPipe.Vehicles)
            {
                if (!car || car.Body == null || car.Body.isKinematic) continue;
                var d = car.Body.position - lip;
                float along = d.x * dir.x + d.z * dir.z, lat = Mathf.Abs(Vector3.Dot(d, left));
                float was = before.TryGetValue(car.Body, out float w) ? w : -99f; before[car.Body] = along;
                if (was >= 0f || along < 0f || along > 3f || lat > halfWidth + .8f || d.y < -1.5f || d.y > 3.5f) continue;
                if (Vector3.Dot(car.Body.linearVelocity, dir) < 2f) continue;
                if (recent.TryGetValue(car.Body, out float at) && Time.time - at < 2f) continue;
                recent[car.Body] = Time.time; Launches++;
                GuidedFlight.Begin(car, "launch " + id, lip.y, target, dir, targetDir, angle, gravity);
            }
        }
    }
}
