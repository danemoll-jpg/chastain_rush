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
    // Round 21 (Dan: "yes put it in case you are short"): the near step. A vehicle that was on the step (or climbed it from the lawn) in the
    // last 1.5 s is caught over the pool up to 3 m over the rim, not only below it, so it drops in instead of flying off the step's edge
    // and over the water (the exe check: from 15 to 19 m/s off the lip some cleared the pool off the step). A clean jump never touches it.
    public sealed class PoolHold : MonoBehaviour
    {
        public Vector2 centre; public float radius, rimY, maxSpeed = 1f, stall = 2.5f;
        public Vector2 axis; public float stepOut, stepHalf;   // Round 21: the near step, stepOut m out from the wall toward -axis, stepHalf either side
        readonly Dictionary<ArcadeVehicle, float> onStep = new Dictionary<ArcadeVehicle, float>();
        static readonly Dictionary<ArcadeVehicle, float> inPool = new Dictionary<ArcadeVehicle, float>();
        // Round 21: for the AI (RoadDriver): this vehicle is in a pool now (or was a moment ago), wading out: not stuck
        public static bool Wading(ArcadeVehicle c) => c != null && inPool.TryGetValue(c, out float t) && Time.time - t < 1.5f;
        public static int Held { get; private set; }
        public static int LiftedOut { get; private set; }   // Round 21: vehicles lifted onto the exit deck (stuck in the water)
        readonly Dictionary<ArcadeVehicle, (Vector3 p, float t)> lastMove = new Dictionary<ArcadeVehicle, (Vector3, float)>();   // for the checks: physics steps a vehicle was held
        readonly List<ArcadeVehicle> cars = new List<ArcadeVehicle>(); float nextScan;
        readonly Dictionary<ArcadeVehicle, float> since = new Dictionary<ArcadeVehicle, float>();   // when it came down in the water

        void FixedUpdate()
        {
            if (Time.time >= nextScan) { cars.Clear(); cars.AddRange(FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)); nextScan = Time.time + 1f; }
            foreach (var car in cars)
            {
                if (!car || car.Body == null || car.Body.isKinematic) continue;
                var p = car.Body.position; var q = new Vector2(p.x, p.z) - centre;
                if (stepOut > 0f && axis.sqrMagnitude > 0.5f)
                {
                    float along = -Vector2.Dot(q, axis), side = Mathf.Abs(Vector2.Dot(q, new Vector2(-axis.y, axis.x)));
                    if (along > radius - 1.5f && along < radius + stepOut + 0.5f && side < stepHalf + 0.3f && p.y < rimY + 1.3f) onStep[car] = Time.time;
                }
                bool fromStep = onStep.TryGetValue(car, out float ts) && Time.time - ts < 1.5f;
                if (q.magnitude > radius || p.y > rimY + (fromStep ? 3f : 0.3f)) { since.Remove(car); continue; }
                inPool[car] = Time.time;
                if (!since.TryGetValue(car, out float t0)) { since[car] = t0 = Time.time; lastMove[car] = (p, Time.time); }
                // Round 21: the widest car (Skyfin Cruiser, nearly as long as the pool) could come to rest across it and never climb out: still
                // for 4 s after the stall, it is lifted out onto the exit deck facing the way out (arcade: never a trap, no reset needed)
                if (!lastMove.TryGetValue(car, out var lm) || (p - lm.p).magnitude > 1f) lastMove[car] = lm = (p, Time.time);
                if (axis.sqrMagnitude > 0.5f && Time.time - t0 > stall + 4f && Time.time - lm.t > 4f)
                {
                    var a3 = new Vector3(axis.x, 0f, axis.y).normalized; var at = new Vector3(centre.x, rimY + 0.7f, centre.y) + a3 * (radius + 0.9f);
                    var rot = Quaternion.LookRotation(a3, Vector3.up);
                    car.Body.position = at; car.Body.rotation = rot; car.transform.SetPositionAndRotation(at, rot);
                    car.Body.linearVelocity = a3 * 3f; car.Body.angularVelocity = Vector3.zero; car.ClearSteering(); Physics.SyncTransforms();
                    since.Remove(car); lastMove.Remove(car); LiftedOut++; continue;
                }
                // Round 21: after the stall the cap grows (0.6 m/s more each second): the widest car (Skyfin Cruiser) could not climb out at 1 m/s
                float cap = Time.time - t0 < stall ? 0.15f : maxSpeed + 0.6f * (Time.time - t0 - stall);
                var v = car.Body.linearVelocity; var h = new Vector3(v.x, 0f, v.z);
                if (h.magnitude <= cap) continue;
                h = h.normalized * cap; car.Body.linearVelocity = new Vector3(h.x, v.y, h.z); Held++;
            }
        }
    }
}
