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
        public float ForwardSpeed => Puppet ? PuppetSpeed : Vector3.Dot(Body.linearVelocity, transform.forward);
        // Chastain Rush, Round 57 (F): a race replay's puppet (ReplayViewer): kinematic, its speed and steering from the recording, so the wheels,
        // the front end, the lean, the rider's arms and its engine follow them
        [System.NonSerialized] public bool Puppet; [System.NonSerialized] public float PuppetSpeed, PuppetSteer;
        public Rigidbody Body { get; private set; }
        VehicleInput input;
        float steer;
        public float VisualSteering => Puppet ? PuppetSteer : steer;
        public float WaterImmersion { get; private set; }
        public float GripScale { get; private set; } = 1; // 0.97 Part B: the weather's multiplier on the tyres' grip under this vehicle (1 = dry). Chastain Rush, Round 35 (Part C)
        public float WaterSurface { get; private set; }
        // Chastain Rush, Round 19: the "up" a wheel's ground must face and the vehicle settles to in the air (world up; a drainage
        // pipe's DrainPipe turns it toward the pipe's axis while it holds a fast vehicle to the wall)
        [System.NonSerialized] public Vector3 SurfaceUp = Vector3.up;
        // 0.106 Part C: air steering, stunt vehicles only (VehicleConfiguration sets it from the profile; 0 = none, every other vehicle as before).
        // While all wheels are off the ground the player's input turns the vehicle about its own axes (VehicleInput.AirPitch / Steering /
        // AirSpin) toward these rates; the flight's self-levelling is off while the stick is held, so a flip carries on, and on when it is let go.
        // Chastain Rush, Round 36 (Racer's rivals' AiAir stays behind: no Stunt Races here); Round 38 (Part D): Racer's AiAir ported for the
        // stunt rivals (StuntRival): an AI vehicle (driven through Simulate, not the player's step) turns toward AiAirInput while AiAir is on
        [System.NonSerialized] public float airControl;
        [System.NonSerialized] public bool AiAir; [System.NonSerialized] public Vector3 AiAirInput;   // x tip, y spin, z roll, each -1..1
        public const float AirTipRate = 5.5f, AirRollRate = 5.5f, AirSpinRate = 5f, AirResponse = 16f;
        public bool AirSteering { get; private set; }
        float rideEnergy = float.NegativeInfinity, lastSpeed;   // Round 37: per unit mass, on the park's ramps and loop
        static readonly bool CarryOff = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-r37CarryOff") >= 0;   // Round 37, checks only
        static readonly bool SpringUp = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-r37SpringUp") >= 0;   // Round 37, checks only

        void Awake()
        {
            Body = GetComponent<Rigidbody>(); input = GetComponent<VehicleInput>();
            Body.centerOfMass = centreOfMass;
            Body.maxAngularVelocity = 5;
            // Chastain Rush, Round 10: Racer registers its car with VehicleSurfaceContacts in VehicleConfiguration (not
            // imported); here the car registers itself, so a body contact on a shortcut ramp uses the ramp's real face normal
            var box = GetComponentInChildren<BoxCollider>();
            if (box) VehicleSurfaceContacts.Register(box);
            if(!GetComponent<WaterFeedback>())gameObject.AddComponent<WaterFeedback>();   // Round 15: Racer's, as in Racer
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
            // vehicles' wipeout (WaterFeedback came in Round 15, the weather grip in Round 35)
            var configuration = GetComponent<VehicleConfiguration>();
            if(configuration) configuration.PrepareContacts();
            if (configuration && configuration.WipedOut && transform.up.y<.35f) { throttle = 0; steering *= .25f; }
            GroundedWheels = 0;
            SuspensionLift = 0;
            Vector3 normal = Vector3.zero; bool onLoop = false; int parkWheels = 0;   // Round 36: the stunt park's loop (Racer 0.106); Round 37: wheels on its ramps
            var weather = WeatherGrip.InEffect; float gripSum = 0; int gripCount = 0; // 0.97 Part B (Chastain Rush, Round 35)
            foreach (Vector3 local in suspensionPoints)
            {
                Vector3 origin = transform.TransformPoint(local);
                if (!gameObject.scene.GetPhysicsScene().Raycast(origin, -transform.up, out RaycastHit hit, suspensionLength, groundMask, QueryTriggerInteraction.Ignore)) continue;
                bool loop = StuntPark.Loop(hit.collider); if (loop) onLoop = true;   // Round 36: the stunt park's loop holds the wheels all the way round (Racer 0.106)
                if (Vector3.Dot(hit.normal, SurfaceUp) < 0.35f && !loop) continue;   // Chastain Rush, Round 19: SurfaceUp (Vector3.up in Racer)
                GroundedWheels++;
                if (loop || hit.collider is MeshCollider && StuntPark.Surface(hit.collider)) parkWheels++;   // Round 37: on the park's ramps
                normal += hit.normal;
                if (weather != Weather.Clear) { gripSum += WeatherGrip.ForHit(weather, hit.collider, hit.point); gripCount++; }
                float compression = suspensionLength - hit.distance;
                float speed = Vector3.Dot(Body.GetPointVelocity(origin), transform.up);
                float lift = Mathf.Clamp(compression * springStrength - speed * suspensionDamping, 0, maxSuspensionAcceleration);
                SuspensionLift += lift / suspensionPoints.Length;
                // Chastain Rush, Round 37 (BUG-002, "Loop slows on both sides"): on the stunt park's surfaces a wheel pushes square to the surface it is
                // on; along the vehicle's own up, which lags the loop's and the half-pipe's curve, part of the spring pushed back against the travel
                // (the Skywhip 250 lost 5 m/s entering the loop). Everywhere else as Racer (transform.up). -r37SpringUp: as before (checks only)
                var push = (loop || StuntPark.Surface(hit.collider)) && !SpringUp ? hit.normal : transform.up;
                Body.AddForceAtPosition(push * (lift / suspensionPoints.Length), origin, ForceMode.Acceleration);
            }
            bool grounded = GroundedWheels >= 2;
            // 0.97 Part B: the grip of the surface under the wheels, eased in so crossing a verge or a patch of ice is not a snap
            if (weather == Weather.Clear) GripScale = 1; else if (gripCount > 0) GripScale = Mathf.MoveTowards(GripScale, gripSum / gripCount, 2.5f * dt);
            float gs = GripScale;
            float immersed=ShallowWater.Sample(this,out float surface);
            WaterSurface=surface;
            WaterImmersion=immersed<=0?0:Mathf.MoveTowards(WaterImmersion,immersed,dt*2.5f);
            Vector3 up = grounded ? normal.normalized : SurfaceUp;   // Chastain Rush, Round 19: SurfaceUp (Vector3.up in Racer)
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            Vector3 right = Vector3.Cross(up, forward);
            float speedForward = Vector3.Dot(Body.linearVelocity, forward);
            steer = Mathf.MoveTowards(steer, Mathf.Clamp(steering, -1, 1), steeringResponse * dt);
            if (grounded)
            {
                // Opposite pedal brakes first. Holding it through a stop engages the other direction.
                float drive;
                if (brakeReverse > 0.05f && speedForward > 0.6f) drive = -braking * gs * brakeReverse;
                else if (throttle > 0.05f && speedForward < -0.6f) drive = braking * gs * throttle;
                else drive = throttle * acceleration * Mathf.Lerp(1, gs, .5f) * Mathf.Clamp01(1 - Mathf.Max(0, speedForward) / topSpeed)
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
                // wet or snowy ground: a lower ceiling on the sideways grip; in snow also a slower pull back (more slide, slower recovery)
                float recover = gs >= 1 ? 1 : weather == Weather.Snow ? gs : Mathf.Sqrt(gs);
                Body.AddForce(-right * Mathf.Clamp(sideways * lateralGrip * recover, -maxGripAcceleration * gs, maxGripAcceleration * gs), ForceMode.Acceleration);
                float angle = Mathf.Lerp(slowSteerAngle, fastSteerAngle, Mathf.Clamp01(Mathf.Abs(speedForward) / topSpeed));
                float yaw = Mathf.Tan(steer * angle * Mathf.Deg2Rad) * speedForward / wheelbase;
                yaw = Mathf.Clamp(yaw, -1.6f, 1.6f);
                // 0.106 Part A: on the stunt park's loop the vehicle follows the ribbon's line (as a car follows a track's curve), whatever the steering
                if (onLoop)
                {
                    var along = StuntPark.LoopTangent(Body.position, out float off); if (Vector3.Dot(along, transform.forward) < 0) along = -along;
                    float error = Vector3.SignedAngle(forward, Vector3.ProjectOnPlane(along, up).normalized, up) * Mathf.Deg2Rad;
                    off = Mathf.Clamp(off, -2.5f, 2.5f);
                    // along the line, aiming back toward its centre the further off it is; and held on it (a track holds a car to its curve)
                    yaw = Mathf.Clamp(error * 6 - off * .7f * Mathf.Sign(Vector3.Dot(Vector3.Cross(up, forward), StuntPark.LoopSide)) * Mathf.Sign(speedForward), -2.5f, 2.5f);
                    Body.AddForce(-Vector3.ProjectOnPlane(StuntPark.LoopSide, up).normalized * (off * 5), ForceMode.Acceleration);
                }
                Body.AddTorque(up * ((yaw - Vector3.Dot(Body.angularVelocity, up)) * yawResponse), ForceMode.Acceleration);
            }
            // Chastain Rush, Round 37 (BUG-001 to BUG-003, "ramp slows", "Loop slows on both sides"; standing rule: ramps never slow you): on the
            // stunt park's ramps and round its loop the vehicle keeps its speed but for what the height takes (its speed and height when it came
            // on, and what it gains on the throttle, kept as energy per unit mass). The 5 m loop and the 6 m half-pipe and quarter-pipes at 30 m/s
            // turn a body faster than its suspension follows, and the solver took 3 m/s more than the climb half way up a quarter-pipe and 7 more
            // over the loop's top. Like RampHold's landing carry, arcade by design, the park only. On the ramps only while the throttle is held
            // (coasting and braking slow as anywhere), and a knock (over 3 m/s in one step: the bus, a wall) is never given back.
            // -r37CarryOff: as before (checks only)
            bool carry = !CarryOff && grounded && brakeReverse < .05f && (onLoop || parkWheels >= 2 && throttle > .05f);
            var vNow = Body.linearVelocity; float g9 = -Physics.gravity.y, yNow = Body.position.y, eNow = .5f * vNow.sqrMagnitude + g9 * yNow;
            if (carry)
            {
                if (rideEnergy == float.NegativeInfinity || eNow > rideEnergy || !onLoop && lastSpeed - vNow.magnitude > 3f) rideEnergy = eNow;
                else { float want = 2f * (rideEnergy - g9 * yNow); if (want > vNow.sqrMagnitude && vNow.sqrMagnitude > 1f) { vNow *= Mathf.Sqrt(want / vNow.sqrMagnitude); Body.linearVelocity = vNow; } }
            }
            else rideEnergy = float.NegativeInfinity;
            lastSpeed = vNow.magnitude;
            // Damped alignment resists roll, follows ramp slope, and gently settles attitude in flight.
            Vector3 tilt = Vector3.Cross(transform.up, up);
            Vector3 tiltVelocity = Vector3.ProjectOnPlane(Body.angularVelocity, up);
            // 0.106 Part C: a stunt vehicle in the air with the stick held: no self-levelling, the stick sets the turn rates instead (Round 36)
            bool air = playerStep && airControl > 0 && GroundedWheels == 0 && !(configuration && configuration.WipedOut);
            bool aiAir = !playerStep && AiAir && airControl > 0 && GroundedWheels == 0 && !(configuration && configuration.WipedOut);   // Round 38 (Racer)
            if (playerStep) input.AirActive = airControl > 0 && GroundedWheels == 0;
            float tip = air ? input.AirPitch : aiAir ? AiAirInput.x : 0, rollInput = air ? steering : aiAir ? AiAirInput.z : 0, spin = air ? input.AirSpin : aiAir ? AiAirInput.y : 0;
            AirSteering = aiAir || air && (Mathf.Abs(tip) > .1f || Mathf.Abs(rollInput) > .1f || Mathf.Abs(spin) > .1f);
            float level = grounded ? 1 : AirSteering ? 0 : airStability;
            AlignmentTorque = ((tilt * uprightStrength - tiltVelocity * uprightDamping) * level).magnitude;
            Body.AddTorque((tilt * uprightStrength - tiltVelocity * uprightDamping) * level, ForceMode.Acceleration);
            if (AirSteering)
            {
                var w = transform.InverseTransformDirection(Body.angularVelocity); float k = AirResponse * airControl * dt;
                if (aiAir || Mathf.Abs(tip) > .1f) w.x = Mathf.MoveTowards(w.x, tip * AirTipRate, k);            // stick forward: nose down
                if (aiAir || Mathf.Abs(rollInput) > .1f) w.z = Mathf.MoveTowards(w.z, -rollInput * AirRollRate, k); // stick right: roll right
                if (aiAir || Mathf.Abs(spin) > .1f) w.y = Mathf.MoveTowards(w.y, spin * AirSpinRate, k);            // right: spin clockwise from above
                Body.angularVelocity = transform.TransformDirection(w);
            }
        }

        public void ClearSteering() { steer = 0; WaterImmersion=0; WaterSurface=0; GetComponent<WaterFeedback>()?.Clear(); GetComponent<VehicleConfiguration>()?.Recover(); }   // Round 13: Racer 0.97 (Round 15: with its WaterFeedback)
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (var p in suspensionPoints) { var o = transform.TransformPoint(p); Gizmos.DrawLine(o, o - transform.up * suspensionLength); }
        }
    }
}
