using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 20 follow-up (R1, Dan: a pool that costs "real time (well more than the shortcut saves)"): the above-ground pool behind
    // 1572 / 1622 kills a vehicle's speed. Arcade, by design: a vehicle that comes down in its water (inside its wall, below the rim)
    // is stopped dead for 'stall' seconds (a belly flop), then held to 'maxSpeed' across the ground, so it wades to the exit ramp, up
    // it and off the deck, instead of skimming the water in a blink (ShallowWater's drag alone cost the bikes 1 s; 1 m/s alone, 1.5 s).
    // Its height is never touched; one flying over is not affected.
    public sealed class PoolHold : MonoBehaviour
    {
        public Vector2 centre; public float radius, rimY, maxSpeed = 1f, stall = 2.5f;
        public static int Held { get; private set; }   // for the checks: physics steps a vehicle was held
        readonly List<ArcadeVehicle> cars = new List<ArcadeVehicle>(); float nextScan;
        readonly Dictionary<ArcadeVehicle, float> since = new Dictionary<ArcadeVehicle, float>();   // when it came down in the water

        void FixedUpdate()
        {
            if (Time.time >= nextScan) { cars.Clear(); cars.AddRange(FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)); nextScan = Time.time + 1f; }
            foreach (var car in cars)
            {
                if (!car || car.Body == null || car.Body.isKinematic) continue;
                var p = car.Body.position;
                if ((new Vector2(p.x, p.z) - centre).magnitude > radius || p.y > rimY + 0.3f) { since.Remove(car); continue; }
                if (!since.TryGetValue(car, out float t0)) since[car] = t0 = Time.time;
                float cap = Time.time - t0 < stall ? 0.15f : maxSpeed;
                var v = car.Body.linearVelocity; var h = new Vector3(v.x, 0f, v.z);
                if (h.magnitude <= cap) continue;
                h = h.normalized * cap; car.Body.linearVelocity = new Vector3(h.x, v.y, h.z); Held++;
            }
        }
    }
}
