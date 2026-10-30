using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 84 (A): Racer 1.2's HighAssist, ported. Forgiving driving on a HIGH MODE player's own vehicle (a race in HIGH MODE, or Free Roam with
    // HIGH MODE on; split screen: each player's own choice): a vehicle on its roof or side flips itself upright after about a second (no wreck:
    // VehicleConfiguration skips its wipeout), a flight levels itself more so landings come down straighter (ArcadeVehicle.Forgiving), and a hit
    // (a wall, a bumper, traffic) gives back half the speed it took (never into what was hit). Not the planes or the helicopter (they have
    // their own crashes: PlaneFlight's in HIGH MODE is shorter, an air race's puts the plane back flying at once).
    // Racer's quicker reset (0.8 s -> 0.2 s) has no counterpart: the reset here is at once already; a crash's wait is shorter instead
    // (a train: VehicleRespawn.ResetSoon; a plane or the helicopter: their CrashFor).
    public sealed class HighAssist : MonoBehaviour
    {
        ArcadeVehicle car; VehicleRespawn respawn; VehicleConfiguration config;
        Vector3 lastVelocity, hitNormal; bool hit; float overturned;
        public static int Righted { get; private set; }   // checks
        public int Player = 1;   // split screen: whose vehicle (each player's own choice)
        public bool On => car && !car.Body.isKinematic && !car.Puppet && !(config && config.Profile != null && config.Profile.Flies) && HighMode.ForPlayer(Player);
        // the players' vehicles get one (the player's at once; split screen's player 2 when its session makes it)
        public static void Ensure(ArcadeVehicle c, int player)
        {
            if (!c) return; var a = c.GetComponent<HighAssist>(); if (!a) a = c.gameObject.AddComponent<HighAssist>(); a.Player = player;
            WallGlance.Ensure(c, player);   // Round 85: glancing off guide walls, in every mode
        }
        public static bool For(ArcadeVehicle c) => c && c.GetComponent<HighAssist>() is HighAssist a && a.On;
        void Awake() { car = GetComponent<ArcadeVehicle>(); respawn = GetComponent<VehicleRespawn>(); config = GetComponent<VehicleConfiguration>(); }
        void FixedUpdate()
        {
            bool on = On; car.Forgiving = on;
            if (!on) { overturned = 0; hit = false; lastVelocity = car.Body.linearVelocity; return; }
            if (hit)
            {
                hit = false;
                var lost = Vector3.ProjectOnPlane(lastVelocity - car.Body.linearVelocity, Vector3.up);
                var n = Vector3.ProjectOnPlane(hitNormal, Vector3.up).normalized; if (Vector3.Dot(n, lastVelocity) > 0) n = -n;
                float into = Vector3.Dot(lost, -n); if (into > 0) lost += n * into;
                if (lost.magnitude > 1.5f) car.Body.linearVelocity += lost * .5f;
            }
            lastVelocity = car.Body.linearVelocity;
            bool over = transform.up.y < .45f && car.Body.linearVelocity.magnitude < 8 && car.GroundedWheels < 2;
            overturned = over ? overturned + Time.fixedDeltaTime : 0;
            if (overturned >= 1f) Right();
        }
        void Right()
        {
            overturned = 0; var f = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (f.sqrMagnitude < .02f) f = Vector3.ProjectOnPlane(transform.up, Vector3.up);
            if (f.sqrMagnitude < .02f) f = Vector3.forward;
            var rot = Quaternion.LookRotation(f.normalized, Vector3.up); var pos = car.Body.position + Vector3.up * 1.1f;
            car.Body.position = pos; car.Body.rotation = rot; transform.SetPositionAndRotation(pos, rot);
            car.Body.linearVelocity = Vector3.ProjectOnPlane(car.Body.linearVelocity, Vector3.up) * .5f; car.Body.angularVelocity = Vector3.zero;
            if (config) config.Recover(); Physics.SyncTransforms(); Righted++;
            Debug.Log($"[High] righted at ({pos.x:0.0}, {pos.y:0.0}, {pos.z:0.0})");
        }
        void OnCollisionEnter(Collision c)
        {
            if (!On || c.contactCount == 0) return;
            var n = c.GetContact(0).normal; if (Mathf.Abs(n.y) > .6f) return;   // the ground, a landing, a ramp: not a hit
            if (!WallGlance.Off && GuideWalls.Is(c.collider)) return;   // Round 85: a bumper or a barrier: WallGlance turns it along the wall instead
            hit = true; hitNormal = n;
        }
        void OnDisable() { if (car) car.Forgiving = false; }
    }
}
