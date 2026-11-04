using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 91 (C, Dan: "one or two that shoot the user up back to the top of the quarry"): a launch pipe's mouth on Viking Quarry's floor. Drive out
    // of the pipe and a geyser under the mouth blows: a column of water, and the vehicle is thrown up out of the pit along an aimed flight
    // (GuidedFlight: a scripted path is allowed for something creative that is not a ramp) onto open, drivable ground on the rim, landing along
    // 'landDir'. World (the network's exits, MapBuilder.Round91.cs); off while a race runs (the network's mouths are shut then).
    public sealed class PipeLaunch : MonoBehaviour
    {
        public Vector3 outward = Vector3.forward;   // the way out of the mouth (flat, unit)
        public Vector3 target, landDir = Vector3.forward;
        public float angleDeg = 62f, reach = 6f;
        public string launchName = "quarry launch";
        public static int Launches { get; private set; }
        public static string Last { get; private set; } = "";
        readonly System.Collections.Generic.Dictionary<Rigidbody, float> recent = new();

        void FixedUpdate()
        {
            var race = RaceController.Instance; if (race && race.State != RaceController.Stage.Free) return;
            foreach (var car in DrainPipe.Vehicles)
            {
                if (!car || car.Body == null || car.Body.isKinematic) continue;
                var d = car.Body.position - transform.position; if (d.y < -3f || d.y > 5f) continue;
                if (new Vector2(d.x, d.z).magnitude > reach) continue;
                var v = car.Body.linearVelocity; if (Vector3.Dot(new Vector3(v.x, 0f, v.z), outward) < 4f) continue;   // only on the way out
                if (recent.TryGetValue(car.Body, out float at) && Time.time - at < 4f) continue;
                if (car.TryGetComponent<GuidedFlight>(out var gf) && gf.Flying) continue;
                recent[car.Body] = Time.time;
                Geyser.Spray(transform.position, 3.5f, 30f);
                car.Body.angularVelocity = Vector3.zero;
                // (it leaves aimed at its landing: a flight that left along the cut first and curved round met the pit's west wall)
                var aim = new Vector3(target.x - car.Body.position.x, 0f, target.z - car.Body.position.z).normalized;
                // (off the cut's floor and turned to the flight first: a wheel still dragging as it turned read as a hit and let it go)
                car.Body.position += Vector3.up * 1.5f; car.Body.rotation = Quaternion.LookRotation(aim, Vector3.up); car.transform.SetPositionAndRotation(car.Body.position, car.Body.rotation);
                var f = GuidedFlight.Begin(car, launchName, car.Body.position.y, target, aim, landDir, angleDeg);   // (from where it is: the rise to the rim in the flight)
                Launches++; Last = $"{launchName}: from ({transform.position.x:0}, {transform.position.z:0}) to ({target.x:0}, {target.y:0}, {target.z:0}), {(f ? f.Planned : 0f):0.0} s planned";
                Debug.Log("[PipeLaunch] " + Last);
            }
        }
    }
}
