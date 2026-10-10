using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 10, item 2: on a "held" shortcut surface (ShortcutSurface.held) the car keeps to the surface: the part of its
    // velocity that would carry it off the surface (along the surface's normal, away from it) is removed each physics step.
    // Its speed along the surface is untouched, so nothing is lost; on a rounded ramp top (radius 6 to 8 m) this is what
    // lets a car at racing speed level out on to the upstairs floor instead of flying into the ceiling. Arcade, by design,
    // and only on the two held ramps of shortcut #2; Racer has no equivalent (its ramps are kickers). Added to the car by
    // GameShell.
    // Landings (Dan: "touching down on a landing ramp must not scrub speed or stop the car"): when the car comes down on a
    // shortcut's landing deck or slope after at least a quarter of a second in the air, its speed is kept and turned along
    // the surface (less 3%), instead of the part of it into the surface being lost in the impact. On #6, a car that reaches
    // the kicker at 21 m/s or more flies 57 m and comes down at about 40 degrees on to a 7 degree deck: it lost 40 to 47%.
    public sealed class RampHold : MonoBehaviour
    {
        public static int HeldSteps, Landings;   // checks: how many physics steps it held, how many landings it carried
        const float LandingKeep = 0.97f;
        float airTime;
        ArcadeVehicle car;
        readonly Dictionary<Collider, ShortcutSurface> cache = new Dictionary<Collider, ShortcutSurface>();

        void Awake() { car = GetComponent<ArcadeVehicle>(); }

        float leftHeldAt = -10f;
        const float LeaveSeconds = 0.25f;

        void StopPitching()
        {
            var right = transform.right; var av = car.Body.angularVelocity;
            car.Body.angularVelocity = av - right * Vector3.Dot(av, right);
        }

        void FixedUpdate()
        {
            if (car == null) return;
            var up = transform.up;
            ShortcutSurface surface = null; RaycastHit hit = default;
            if (car.GroundedWheels >= 1 && Physics.Raycast(transform.position + up * 0.2f, -up, out hit, 1.6f, ~0, QueryTriggerInteraction.Ignore)
                && !cache.TryGetValue(hit.collider, out surface)) cache[hit.collider] = surface = hit.collider.GetComponent<ShortcutSurface>();
            if (car.GroundedWheels == 0) airTime += Time.fixedDeltaTime;
            else
            {
                if (airTime > 0.25f && ((surface != null && surface.landing) || (hit.collider is TerrainCollider && LandingZone.Contains(hit.point))))   // Round 11: or the ground of a landing zone
                {
                    var v0 = car.Body.linearVelocity; float into = Vector3.Dot(v0, hit.normal);
                    if (into < 0f)
                    {
                        var along = v0 - hit.normal * into;
                        if (along.sqrMagnitude > 1f) { car.Body.linearVelocity = along.normalized * v0.magnitude * LandingKeep; Landings++; }
                    }
                }
                airTime = 0f;
            }
            bool held = surface != null && surface.held && car.GroundedWheels >= 2;
            // just off the edge of a held floor: the rear wheels, still on it, would push the tail up and pitch the car
            // nose-down for the whole flight; for a quarter of a second its pitching is stopped, then it flies as it left
            if (!held)
            {
                if (Time.fixedTime - leftHeldAt < LeaveSeconds && car.GroundedWheels < 4) StopPitching();
                return;
            }
            leftHeldAt = Time.fixedTime;
            // on the level part (the upstairs floor, the top of the stairs) the car's pitching is stopped too, so it leaves the
            // edge level: coming over a rounded top it pitches nose-down at speed / radius (about 165 deg/s at 23 m/s over an
            // 8 m top), and in the air that kept turning it nose-first into the ground
            if (hit.normal.y > 0.995f) StopPitching();
            else
            {
                leftHeldAt = -10f;   // only after a level held part (not off a held ramp's slope)
                // climbing a held ramp the car keeps its speed (item 2j: "ramps must not slow the car"; a car met the 1627
                // stairs at 22 to 27 m/s and left them 5 to 8% slower): the pull of gravity along the slope is cancelled
                var downhill = Vector3.ProjectOnPlane(Physics.gravity, hit.normal);
                if (Vector3.Dot(car.Body.linearVelocity, downhill) < 0f) car.Body.AddForce(-downhill, ForceMode.Acceleration);
            }
            var v = car.Body.linearVelocity; float away = Vector3.Dot(v, hit.normal);
            if (away <= 0f) return;
            car.Body.linearVelocity = v - hit.normal * away;
            HeldSteps++;
        }
    }
}
