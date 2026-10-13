using UnityEngine;

namespace Racer
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new(0, 3.6f, -7.5f);
        // 0.77: Far chase and the Trailer Mode chase scale the profile offset; (1, 1, 1) is the normal chase camera.
        public Vector3 offsetScale = Vector3.one;
        public float positionSmoothTime = 0.16f;
        public float headingResponse = 6;
        public float lookAhead = 3;
        public LayerMask obstructionMask = 1;
        public float collisionRadius = 0.3f;
        // 0.90 Part D: degrees the camera tips down during a long flight (set only for the wide split-screen halves).
        [System.NonSerialized] public float flightTilt;
        float airborne, tilt; ArcadeVehicle car;
        Vector3 velocity;
        Quaternion heading;
        VehicleRespawn respawn;
        // 0.77: the chase pose is kept here, not read back from the camera, so it keeps tracking while another view
        // (CameraViews) places the camera, and that view can ease back to it.
        Vector3 position;
        Quaternion rotation = Quaternion.identity;
        public Vector3 ChasePosition => position;
        public Quaternion ChaseRotation => rotation;
        Vector3 Offset => Vector3.Scale(offset, offsetScale);
        // Chastain Rush, Round 19: inside a drainage pipe (DrainPipe.View) the camera rides the pipe's axis behind the vehicle and
        // looks along it, world up, so a vehicle riding the walls or over the roof does not swing it round or out of the pipe
        public delegate bool ViewFn(Transform target, out Vector3 desired, out Vector3 lookAt);
        public static ViewFn PipeView; float pipeTime;
        void OnEnable()
        {
            if (!target) return;
            respawn = target.GetComponent<VehicleRespawn>();
            if (respawn) respawn.Respawned += Snap;
            Snap();
        }
        void OnDisable() { if (respawn) respawn.Respawned -= Snap; }
        public void Snap()
        {
            if (!target) return;
            heading = Quaternion.Euler(0, target.eulerAngles.y, 0); velocity = Vector3.zero;
            position = target.position + heading * Offset;
            Aim(); transform.SetPositionAndRotation(position, rotation);
        }
        void LateUpdate()
        {
            if (!target) return;
            if (PipeView != null && PipeView(target, out var pipeAt, out var pipeLook))   // Chastain Rush, Round 19
            {
                // Round 24: a short blend on entering, then the camera sits on the (already smoothed) axis point; the rotation eases
                pipeTime += Time.deltaTime;
                if (pipeTime < 0.5f) position = Vector3.SmoothDamp(position, pipeAt, ref velocity, 0.2f); else { position = pipeAt; velocity = Vector3.zero; }
                var lk = pipeLook - position;
                if (lk.sqrMagnitude > 1e-6f) { var want = Quaternion.LookRotation(lk, Vector3.up); rotation = pipeTime < 0.02f ? rotation : Quaternion.Slerp(rotation, want, 1 - Mathf.Exp(-7f * Time.deltaTime)); }
                var flat = Vector3.ProjectOnPlane(pipeLook - pipeAt, Vector3.up); if (flat.sqrMagnitude > 1e-4f) heading = Quaternion.LookRotation(flat);
                transform.SetPositionAndRotation(position, rotation);
                return;
            }
            pipeTime = 0f;
            heading = Quaternion.Slerp(heading, Quaternion.Euler(0, target.eulerAngles.y, 0), 1 - Mathf.Exp(-headingResponse * Time.deltaTime));
            Vector3 desired = target.position + heading * Offset;
            Vector3 pivot = target.position + Vector3.up;
            Vector3 direction = desired - pivot;
            if (Physics.SphereCast(pivot, collisionRadius, direction.normalized, out RaycastHit hit, direction.magnitude, obstructionMask, QueryTriggerInteraction.Ignore))
                desired = pivot + direction.normalized * Mathf.Max(0, hit.distance - 0.1f);
            Vector3 smoothed = Vector3.SmoothDamp(position, desired, ref velocity, positionSmoothTime);
            // Clip the smoothed path too, so smoothing cannot carry the camera through a wall.
            direction = smoothed - pivot;
            if (Physics.SphereCast(pivot, collisionRadius, direction.normalized, out hit, direction.magnitude, obstructionMask, QueryTriggerInteraction.Ignore))
                smoothed = pivot + direction.normalized * Mathf.Max(0, hit.distance - 0.1f);
            position = smoothed; Aim();
            if (flightTilt > 0 || tilt > 0)
            {
                if (!car) car = target.GetComponent<ArcadeVehicle>();
                airborne = car && car.GroundedWheels == 0 ? airborne + Time.deltaTime : 0;
                tilt = Mathf.MoveTowards(tilt, airborne > .45f ? flightTilt : 0, Time.deltaTime * 12);
                rotation = rotation * Quaternion.Euler(tilt, 0, 0);
            }
            transform.SetPositionAndRotation(position, rotation);
        }
        void Aim()
        {
            var look = target.position + Vector3.up * 0.7f + heading * Vector3.forward * lookAhead - position;
            if (look.sqrMagnitude > 1e-6f) rotation = Quaternion.LookRotation(look, Vector3.up);
        }
    }
}
