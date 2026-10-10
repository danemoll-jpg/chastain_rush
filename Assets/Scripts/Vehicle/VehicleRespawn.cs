using System;
using UnityEngine;

namespace Racer
{
    // Chastain Rush: reduced from Racer's VehicleRespawn, which recovers onto race routes through RaceDirector,
    // RaceRoad and the course layouts. Here there are no routes yet, so reset (R / Y) puts the car back on its
    // last stable grounded pose, and falling below fallResetHeight returns it to the spawn point.
    [RequireComponent(typeof(ArcadeVehicle), typeof(VehicleInput))]
    public sealed class VehicleRespawn : MonoBehaviour
    {
        public Transform spawnPoint;
        public float fallResetHeight = -15;
        public event Action Respawned;
        // Chastain Rush, Round 10
        public delegate bool RecoverFn(VehicleRespawn respawn, out Vector3 position, out float yawDegrees);
        public static RecoverFn Recover;
        public Vector3 SafePosition => safePosition;
        public float SafeYaw => safeRotation.eulerAngles.y;

        const float SampleInterval = 0.5f, StableFor = 1f;
        ArcadeVehicle vehicle;
        VehicleInput input;
        Vector3 startPosition, safePosition;
        Quaternion startRotation, safeRotation;
        float stableSince = -1, nextSample;

        void Awake()
        {
            vehicle = GetComponent<ArcadeVehicle>();
            input = GetComponent<VehicleInput>();
            startPosition = safePosition = spawnPoint ? spawnPoint.position : transform.position;
            startRotation = safeRotation = spawnPoint ? spawnPoint.rotation : transform.rotation;
        }

        void FixedUpdate()
        {
            if (vehicle.Body.position.y < fallResetHeight) { Place(startPosition, startRotation); return; }
            // Chastain Rush, Round 10: a reset asks Recover first (CarRecovery: back on the race route facing the right way, or
            // off a spot a tree now blocks), as Racer's VehicleRespawn.RecoverNearest does; the last stable spot otherwise
            if (input.ConsumeReset())
            {
                if (Recover != null && Recover(this, out var rp, out var ry)) Place(rp, Quaternion.Euler(0, ry, 0));
                else Place(safePosition, safeRotation);
                return;
            }
            RecordSafePose();
        }

        // A pose counts as safe once all four wheels have been on the ground, upright, for a second.
        void RecordSafePose()
        {
            bool stable = vehicle.GroundedWheels == 4 && transform.up.y > 0.9f;
            if (vehicle.WaterImmersion > 0f || ShallowWater.InWater(vehicle.Body.position)) stable = false;   // Chastain Rush, Round 15: never a spot in the water
            if (!stable) { stableSince = -1; return; }
            if (stableSince < 0) stableSince = Time.time;
            if (Time.time - stableSince < StableFor || Time.time < nextSample) return;
            nextSample = Time.time + SampleInterval;
            safePosition = vehicle.Body.position;
            safeRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }

        // Chastain Rush, Round 5: the race puts the car on its grid with this. It also makes that pose the one R returns to,
        // so a reset right after a restart cannot send the car back to wherever it last stood before the race.
        public void PlaceAt(Vector3 position, float yawDegrees)
        {
            var rotation = Quaternion.Euler(0, yawDegrees, 0);
            safePosition = position; safeRotation = rotation;
            Place(position, rotation);
        }

        void Place(Vector3 position, Quaternion rotation)
        {
            var body = vehicle.Body;
            body.position = position + Vector3.up * 0.5f;
            body.rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
            transform.SetPositionAndRotation(body.position, body.rotation);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            vehicle.ClearSteering();
            stableSince = -1;
            Physics.SyncTransforms();
            Respawned?.Invoke();
        }
    }
}
