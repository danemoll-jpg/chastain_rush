using UnityEngine;
using UnityEngine.SceneManagement;

namespace Racer
{
    [RequireComponent(typeof(Rigidbody), typeof(VehicleInput))]
    public sealed class ArcadeVehicle : MonoBehaviour
    {
        [Header("Drive (metres, seconds)")]
        [Min(1)] public float topSpeed = 38;
        [Min(1)] public float reverseSpeed = 11;
        [Min(0)] public float acceleration = 12;
        [Min(0)] public float reverseAcceleration = 7;
        [Min(0)] public float braking = 24;
        [Min(0)] public float coastingDrag = 0.45f;
        [Header("Steering / grip")]
        [Range(1, 50)] public float slowSteerAngle = 32;
        [Range(1, 30)] public float fastSteerAngle = 10;
        [Min(0.1f)] public float wheelbase = 2.6f;
        [Min(0)] public float steeringResponse = 7;
        [Min(0)] public float yawResponse = 8;
        [Min(0)] public float lateralGrip = 7;
        [Min(0)] public float maxGripAcceleration = 22;
        [Header("Suspension / stability")]
        public LayerMask groundMask = 1;
        public Vector3[] suspensionPoints = { new(-0.85f, 0, 1.3f), new(0.85f, 0, 1.3f), new(-0.85f, 0, -1.3f), new(0.85f, 0, -1.3f) };
        [Min(0.1f)] public float suspensionLength = 0.8f;
        [Min(0)] public float springStrength = 65;
        [Min(0)] public float suspensionDamping = 8;
        [Min(0)] public float maxSuspensionAcceleration = 45;
        [Min(0)] public float uprightStrength = 12;
        [Min(0)] public float uprightDamping = 4;
        [Range(0, 1)] public float airStability = 0.18f;
        public Vector3 centreOfMass = new(0, -0.35f, 0);
        public int GroundedWheels { get; private set; }
        public float SuspensionLift { get; private set; }
        public float AlignmentTorque { get; private set; }
        public float ForwardSpeed => Vector3.Dot(Body.linearVelocity, transform.forward);
        public Rigidbody Body { get; private set; }
        VehicleInput input;
        float steer;
        public float VisualSteering => steer;
        public float WaterImmersion { get; private set; }
        public float WaterSurface { get; private set; }

        void Awake()
        {
            Body = GetComponent<Rigidbody>(); input = GetComponent<VehicleInput>();
            Body.centerOfMass = centreOfMass;
            Body.maxAngularVelocity = 5;
            // Chastain Rush, Round 10: Racer registers its car with VehicleSurfaceContacts in VehicleConfiguration (not
            // imported); here the car registers itself, so a body contact on a shortcut ramp uses the ramp's real face normal
            var box = GetComponentInChildren<BoxCollider>();
            if (box) VehicleSurfaceContacts.Register(box);
        }
        void OnDestroy() { var box = GetComponentInChildren<BoxCollider>(); if (box) VehicleSurfaceContacts.Unregister(box); }   // Chastain Rush, Round 10

        // 0.84: only the player's own step (input path) holds at rest; AI and traffic call Simulate directly and are unchanged.
        bool playerStep;
        /// <summary>Checks only: apply the player's hold-at-rest to an external Simulate call.</summary>
        [System.NonSerialized] public bool ForceHoldAtRest;
        void FixedUpdate() { playerStep = true; Simulate(input.Throttle, input.BrakeReverse, input.Steering, Time.fixedDeltaTime); playerStep = false; }
        // 0.84 (no idle creep): with no throttle, brake or reverse, below HoldSpeed the ground holds the vehicle like an
        // automatic with its foot off: the slope's pull is cancelled and a stopping force brings it fully to rest, both
        // fading in from nothing at HoldSpeed, so coasting at speed and everything on the throttle are as before.
        const float HoldSpeed = 5f, StopDeceleration = 3f, MaxHold = 4.5f;

        // Public step supports deterministic physics checks without coupling the motor to input devices.
        public void Simulate(float throttle, float brakeReverse, float steering, float dt)
        {
            // Round 13: Racer's current ArcadeVehicle (0.97) re-applied here: the vehicle profile's contact mode and the small
            // vehicles' wipeout. Its weather grip and WaterFeedback stay behind (clear weather: the grip scale is always 1)
            var configuration = GetComponent<VehicleConfiguration>();
            if(configuration) configuration.PrepareContacts();
            if (configuration && configuration.WipedOut && transform.up.y<.35f) { throttle = 0; steering *= .25f; }
            GroundedWheels = 0;
            SuspensionLift = 0;
            Vector3 normal = Vector3.zero;
            foreach (Vector3 local in suspensionPoints)
            {
                Vector3 origin = transform.TransformPoint(local);
                if (!gameObject.scene.GetPhysicsScene().Raycast(origin, -transform.up, out RaycastHit hit, suspensionLength, groundMask, QueryTriggerInteraction.Ignore)) continue;
                if (Vector3.Dot(hit.normal, Vector3.up) < 0.35f) continue;
                GroundedWheels++;
                normal += hit.normal;
                float compression = suspensionLength - hit.distance;
                float speed = Vector3.Dot(Body.GetPointVelocity(origin), transform.up);
                float lift = Mathf.Clamp(compression * springStrength - speed * suspensionDamping, 0, maxSuspensionAcceleration);
                SuspensionLift += lift / suspensionPoints.Length;
                Body.AddForceAtPosition(transform.up * (lift / suspensionPoints.Length), origin, ForceMode.Acceleration);
            }
            bool grounded = GroundedWheels >= 2;
            float immersed=ShallowWater.Sample(this,out float surface);
            WaterSurface=surface;
            WaterImmersion=immersed<=0?0:Mathf.MoveTowards(WaterImmersion,immersed,dt*2.5f);
            Vector3 up = grounded ? normal.normalized : Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            Vector3 right = Vector3.Cross(up, forward);
            float speedForward = Vector3.Dot(Body.linearVelocity, forward);
            steer = Mathf.MoveTowards(steer, Mathf.Clamp(steering, -1, 1), steeringResponse * dt);
            if (grounded)
            {
                // Opposite pedal brakes first. Holding it through a stop engages the other direction.
                float drive;
                if (brakeReverse > 0.05f && speedForward > 0.6f) drive = -braking * brakeReverse;
                else if (throttle > 0.05f && speedForward < -0.6f) drive = braking * throttle;
                else drive = throttle * acceleration * Mathf.Clamp01(1 - Mathf.Max(0, speedForward) / topSpeed)
                           - brakeReverse * reverseAcceleration * Mathf.Clamp01(1 - Mathf.Max(0, -speedForward) / reverseSpeed);
                if (throttle < 0.05f && brakeReverse < 0.05f) drive -= speedForward * coastingDrag;
                drive *= Mathf.Lerp(1,.48f,WaterImmersion);
                Body.AddForce(forward * drive, ForceMode.Acceleration);
                if ((playerStep || ForceHoldAtRest) && throttle < 0.05f && brakeReverse < 0.05f)
                {
                    Vector3 planar = Vector3.ProjectOnPlane(Body.linearVelocity, up);
                    float rolling = planar.magnitude, hold = Mathf.Clamp01(1 - rolling / HoldSpeed);
                    if (hold > 0)
                    {
                        Vector3 slope = Vector3.ProjectOnPlane(Physics.gravity, up);
                        Vector3 stop = rolling > 1e-4f ? -planar / rolling * Mathf.Min(StopDeceleration * hold, rolling / dt) : Vector3.zero;
                        Body.AddForce(Vector3.ClampMagnitude(-slope * hold + stop, MaxHold), ForceMode.Acceleration);
                    }
                }
                Body.AddForce(-Vector3.ProjectOnPlane(Body.linearVelocity,up)*(.95f*WaterImmersion),ForceMode.Acceleration);
                float sideways = Vector3.Dot(Body.linearVelocity, right);
                Body.AddForce(-right * Mathf.Clamp(sideways * lateralGrip, -maxGripAcceleration, maxGripAcceleration), ForceMode.Acceleration);
                float angle = Mathf.Lerp(slowSteerAngle, fastSteerAngle, Mathf.Clamp01(Mathf.Abs(speedForward) / topSpeed));
                float yaw = Mathf.Tan(steer * angle * Mathf.Deg2Rad) * speedForward / wheelbase;
                yaw = Mathf.Clamp(yaw, -1.6f, 1.6f);
                Body.AddTorque(up * ((yaw - Vector3.Dot(Body.angularVelocity, up)) * yawResponse), ForceMode.Acceleration);
            }
            // Damped alignment resists roll, follows ramp slope, and gently settles attitude in flight.
            Vector3 tilt = Vector3.Cross(transform.up, up);
            Vector3 tiltVelocity = Vector3.ProjectOnPlane(Body.angularVelocity, up);
            AlignmentTorque = ((tilt * uprightStrength - tiltVelocity * uprightDamping) * (grounded ? 1 : airStability)).magnitude;
            Body.AddTorque((tilt * uprightStrength - tiltVelocity * uprightDamping) * (grounded ? 1 : airStability), ForceMode.Acceleration);
        }

        public void ClearSteering() { steer = 0; WaterImmersion=0; WaterSurface=0; GetComponent<VehicleConfiguration>()?.Recover(); }   // Round 13: Racer 0.97
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (var p in suspensionPoints) { var o = transform.TransformPoint(p); Gizmos.DrawLine(o, o - transform.up * suspensionLength); }
        }
    }
}
