using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 69 (Dan: "planes"; TODO.md Round 69: "Arcade flight, not a simulator: it must be easy and fun"): the player's plane, "Sky Hopper"
    // (VehicleProfile.Plane, Tools/Blender/plane.py). It is the player's own vehicle object with the plane's profile, so the camera, the map, the
    // minimap, exploration and resets keep working; VehicleConfiguration.Apply adds this and starts it. ArcadeVehicle does not run by itself while the
    // plane is on (ArcadeVehicle.Flown): on the ground this calls its Simulate with the plane's own controls (it rolls on its wheels, steers, brakes),
    // in the air it flies the body itself. Free Roam only (VehicleProfile.Flies).
    //   Pad:      left stick: pitch (pull back: nose up) and bank (the bank turns it); RT / LT throttle up / down (on the ground LT brakes); LB / RB
    //             rudder; X view (chase, far chase, cockpit; the right stick looks round); Y reset.
    //   Keyboard: W / S throttle up / down (on the ground S brakes); up / down arrows nose down / up; left / right arrows bank; A / D rudder (and
    //             steer on the ground); V view; R reset.
    // Take-off: full throttle down the runway (or any long straight road or field) and past TakeOffSpeed pull back. In the air it holds its height
    // and its wings level hands-off at the throttle's speed; climbing costs speed and diving gains it; too slow, the nose eases down until it has
    // speed again (never a spin). Landing: come down gently (under LandSink m/s, wings near level) and it rolls out on its wheels; harder, or a
    // wing, the nose or the body into anything, is a forgiving crash (PlaneFlight.Crash.cs: a moment, then put on open ground nearby).
    public sealed partial class PlaneFlight : MonoBehaviour
    {
        public const float TopSpeed = 115f, Cruise = 85f, TakeOffSpeed = 30f, StallSpeed = 27f, Ceiling = 1700f, SpeedGain = .22f;
        public const float PitchRate = 45f, RollRate = 95f, MaxBank = 70f, MaxPitch = 35f, RudderRate = 16f, LandSink = 5f;
        public const float WheelDrop = 1.25f;   // the wheels' bottom below the body's middle (Tools/Blender/plane.py GROUND)
        ArcadeVehicle car; VehicleConfiguration config; VehicleInput vInput;
        Transform visual, prop, disc; Renderer discRenderer;
        InputAction stick, throttleUp, throttleDown, rudder, keysThrottle, keysPitch, keysBank;
        float pitchDeg, rollDeg, yawDeg, propAngle;
        public bool Airborne { get; private set; }
        public float Throttle { get; private set; }            // 0..1
        public float Airspeed { get; private set; }            // m/s along the nose (in the air)
        public float HeightAboveGround { get; private set; }   // the wheels above whatever is under them
        public float Climb => car ? car.Body.linearVelocity.y : 0f;
        public float PitchDeg => pitchDeg; public float RollDeg => rollDeg; public float YawDeg => yawDeg;
        public static PlaneFlight Player { get; private set; }
        // a stand's swap places it itself (PlaneStands); any other start (the garage, a saved choice) lines it up on the runway
        [System.NonSerialized] public static bool PlacedByStand;
        // checks only: scripted controls (pitch: + nose up, bank: + right, throttle target 0..1 or < 0 for the sticks, rudder, brake)
        [System.NonSerialized] public bool Scripted; [System.NonSerialized] public float sPitch, sBank, sThrottle = -1f, sRudder, sBrake, sSteer;

        public static bool On(Component c) => c && c.TryGetComponent<PlaneFlight>(out var p) && p.enabled;
        public static float RevealBonus(ArcadeVehicle v) => v && v.TryGetComponent<PlaneFlight>(out var p) && p.enabled && p.Airborne ? Mathf.Clamp(p.HeightAboveGround * .6f, 0f, 252f) : 0f;

        public void Begin(Transform newVisual)
        {
            car = GetComponent<ArcadeVehicle>(); config = GetComponent<VehicleConfiguration>(); vInput = GetComponent<VehicleInput>();
            visual = newVisual; prop = visual ? visual.Find("Propeller") : null; disc = visual ? visual.Find("Rotor disc") : null;
            discRenderer = disc ? disc.GetComponentInChildren<Renderer>() : null;
            bool wasOn = enabled;
            car.Flown = true; car.OwnSound = true; car.Body.useGravity = true; car.Body.maxAngularVelocity = 4;
            if (!wasOn)
            {
                Airborne = false; Throttle = 0f; crashedAt = -1f;
                if (!PlacedByStand) LineUp(true);
                PlacedByStand = false;
            }
            yawDeg = transform.eulerAngles.y;
            if (vInput && vInput.Device == null && GetComponent<VehicleSwitch>() is VehicleSwitch sw && sw.player == 1) Player = this;
            enabled = true;
            MakeInput(); StartEffects();
        }
        public void End()
        {
            if (!enabled) return;
            enabled = false;
            if (car) { car.Flown = false; car.OwnSound = false; car.Body.useGravity = true; car.Body.maxAngularVelocity = 5; }
            StopEffects(); DisposeInput();
            if (Player == this) Player = null;
        }
        void OnDestroy() { DisposeInput(); if (Player == this) Player = null; }

        // the runway's start: Cobb County International's runway 09/27 (map.json aeroways, OSM ways 30655059 and 1409772190), lined up at its west end
        // for 09 (east) or its east end for 27 (west)
        public static readonly Vector2 RunwayWest = new Vector2(-3690f, -1909f), RunwayEast = new Vector2(-1845f, -1825f);
        public void LineUp(bool west)
        {
            var a = west ? RunwayWest : RunwayEast; var b = west ? RunwayEast : RunwayWest; float yaw = Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg;
            var respawn = GetComponent<VehicleRespawn>();
            if (!HelicopterFlight.GroundUnder(new Vector3(a.x, 2500f, a.y), out var hit)) return;
            var at = hit.point + Vector3.up * (WheelDrop + .1f);
            if (respawn) respawn.PlaceAt(at, yaw); else { car.Body.position = at; car.Body.rotation = Quaternion.Euler(0, yaw, 0); }
            car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero; Airborne = false; Throttle = 0f; yawDeg = yaw;
            if (RaceController.Instance) RaceController.Instance.NotifyTeleported();
        }

        void MakeInput()
        {
            if (stick != null) return;
            stick = new InputAction("Plane stick", InputActionType.Value, expectedControlType: "Vector2"); stick.AddBinding("<Gamepad>/leftStick").WithProcessor("stickDeadzone(min=0.12,max=0.95)"); stick.Enable();
            throttleUp = new InputAction("Plane throttle up", InputActionType.Value); throttleUp.AddBinding("<Gamepad>/rightTrigger").WithProcessor("axisDeadzone(min=0.1,max=1)"); throttleUp.Enable();
            throttleDown = new InputAction("Plane throttle down", InputActionType.Value); throttleDown.AddBinding("<Gamepad>/leftTrigger").WithProcessor("axisDeadzone(min=0.1,max=1)"); throttleDown.Enable();
            rudder = new InputAction("Plane rudder", InputActionType.Value);
            rudder.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/leftShoulder").With("Positive", "<Gamepad>/rightShoulder");
            rudder.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d"); rudder.Enable();
            keysThrottle = new InputAction("Plane throttle keys", InputActionType.Value); keysThrottle.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/s").With("Positive", "<Keyboard>/w"); keysThrottle.Enable();
            keysPitch = new InputAction("Plane pitch keys", InputActionType.Value); keysPitch.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/upArrow").With("Positive", "<Keyboard>/downArrow"); keysPitch.Enable();
            keysBank = new InputAction("Plane bank keys", InputActionType.Value); keysBank.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow"); keysBank.Enable();
        }
        void DisposeInput()
        {
            foreach (var a in new[] { stick, throttleUp, throttleDown, rudder, keysThrottle, keysPitch, keysBank }) a?.Dispose();
            stick = throttleUp = throttleDown = rudder = keysThrottle = keysPitch = keysBank = null;
        }
        static bool InputFree => !MenuInput.Blocked && !GameMenus.Open && !WorldMap.OwnsInput && Time.timeScale > 0;
        // pitch + nose up, bank + right, up / down the throttle's change (or a scripted target), rudder + right
        void Read(out float pitch, out float bank, out float up, out float down, out float rud)
        {
            pitch = bank = up = down = rud = 0;
            if (Scripted) { pitch = sPitch; bank = sBank; rud = sRudder; if (sThrottle >= 0f) { up = sThrottle > Throttle + .01f ? 1f : 0f; down = sThrottle < Throttle - .01f ? 1f : 0f; } down = Mathf.Max(down, sBrake); return; }
            if (stick == null || !InputFree || (vInput && !vInput.enabled)) return;
            var s = stick.ReadValue<Vector2>(); pitch = Mathf.Clamp(-s.y + keysPitch.ReadValue<float>(), -1, 1); bank = Mathf.Clamp(s.x + keysBank.ReadValue<float>(), -1, 1);
            float k = keysThrottle.ReadValue<float>(); up = Mathf.Max(throttleUp.ReadValue<float>(), k > 0 ? k : 0); down = Mathf.Max(throttleDown.ReadValue<float>(), k < 0 ? -k : 0);
            rud = rudder.ReadValue<float>();
        }

        void Update() => Effects(Time.deltaTime);

        void FixedUpdate()
        {
            if (!car || car.Body.isKinematic) return;
            var body = car.Body; float dt = Time.fixedDeltaTime;
            Read(out float pIn, out float bIn, out float up, out float down, out float rud);
            Probe(body);
            if (Crashed) { CrashStep(dt); return; }
            if (!Airborne) { Ground(body, pIn, bIn, up, down, rud, dt); return; }
            CheckStrikes(body);
            if (Crashed) return;
            // the throttle: held up / down (scripted: straight to its target)
            Throttle = Mathf.Clamp01(Throttle + (up - down) * .6f * dt);
            if (Scripted && sThrottle >= 0f) Throttle = Mathf.MoveTowards(Throttle, sThrottle, .6f * dt);
            // the speed: toward the throttle's speed, less for a climb and more for a dive (gravity along the nose), a little drag in a steep bank
            float targetV = Mathf.Lerp(StallSpeed * .9f, TopSpeed, Throttle);
            Airspeed += ((targetV - Airspeed) * SpeedGain - 9.81f * Mathf.Sin(pitchDeg * Mathf.Deg2Rad) * .75f - Mathf.Abs(Mathf.Sin(rollDeg * Mathf.Deg2Rad)) * 1.2f) * dt;
            Airspeed = Mathf.Clamp(Airspeed, 10f, TopSpeed * 1.35f);
            // pitch: the stick sets the rate; let go and it eases back to level; slow, the nose eases down until it has speed again; never past the ceiling
            float slow = Mathf.Clamp01((StallSpeed + 6f - Airspeed) / 10f);
            if (Mathf.Abs(pIn) > .05f) pitchDeg += pIn * PitchRate * Mathf.Lerp(1f, .55f, Mathf.Clamp01(Airspeed / TopSpeed)) * dt;
            else pitchDeg = Mathf.MoveTowards(pitchDeg, 0f, 14f * dt);
            if (slow > 0f) pitchDeg = Mathf.MoveTowards(pitchDeg, -12f, 30f * slow * dt);
            if (body.position.y > Ceiling) pitchDeg = Mathf.Min(pitchDeg, Mathf.Lerp(0f, -8f, (body.position.y - Ceiling) / 60f));
            pitchDeg = Mathf.Clamp(pitchDeg, -MaxPitch - 10f, MaxPitch * (1f - slow * .8f));
            // bank: the stick rolls it; let go and the wings come level; the bank turns it (a coordinated turn), the rudder a little more
            if (Mathf.Abs(bIn) > .05f && Outside <= 0f) rollDeg += bIn * RollRate * dt;   // (past the map's edge the turn back has the bank)
            else if (Outside <= 0f) rollDeg = Mathf.MoveTowards(rollDeg, 0f, 40f * dt);
            Boundary(body.position, ref rollDeg);   // past the map's edge: a wide turn back (PlaneFlight.Effects.cs)
            rollDeg = Mathf.Clamp(rollDeg, -MaxBank, MaxBank);
            float turn = Mathf.Rad2Deg * 9.81f * Mathf.Tan(rollDeg * Mathf.Deg2Rad) / Mathf.Max(Airspeed, 15f);
            yawDeg += (turn + rud * RudderRate) * dt;
            if (Outside > 0f) Airspeed = Mathf.MoveTowards(Airspeed, Mathf.Max(StallSpeed + 10f, Airspeed * .97f), 6f * dt);   // the headwind
            // the body: along its nose at the airspeed (a lift that just holds it: the climb is the nose's), turned to the attitude
            var rot = Quaternion.Euler(-pitchDeg, yawDeg, -rollDeg);
            body.useGravity = false;
            // slow (under SinkFrom), the wings hold less than its weight: it sinks, more the slower (what brings it down onto the runway with the
            // nose up and the throttle closed; none at cruise, so it holds its height hands-off)
            body.linearVelocity = rot * Vector3.forward * Airspeed + Vector3.down * Sink(Airspeed);
            body.angularVelocity = Vector3.zero; body.MoveRotation(rot);
            // touch down: the wheels at the ground, coming down gently, wings near level, the nose not down: it rolls out on its wheels
            float sink = -body.linearVelocity.y;
            if (HeightAboveGround < .35f && sink > .2f && Time.time - liftedAt > 1.2f)
            {
                if (sink < LandSink && Mathf.Abs(rollDeg) < 18f && pitchDeg > -8f && GroundFlat) TouchDown(body);
                else Crash(sink >= LandSink ? $"a hard landing ({sink:0.0} m/s down)" : Mathf.Abs(rollDeg) >= 18f ? "a wing into the ground" : "the nose into the ground");
            }
        }

        public const float SinkFrom = 36f;
        public static float Sink(float v) => 6f * Mathf.Clamp01((SinkFrom - v) / 12f);
        // on its wheels: ArcadeVehicle's own drive with the plane's controls; past the take-off speed, pull back and it flies
        void Ground(Rigidbody body, float pIn, float bIn, float up, float down, float rud, float dt)
        {
            body.useGravity = true;
            float fwd = Vector3.Dot(body.linearVelocity, transform.forward);
            float steer = Mathf.Clamp(rud + bIn + (Scripted ? sSteer : 0f), -1f, 1f);
            float thr = Scripted && sThrottle >= 0f ? sThrottle : up, brk = Scripted ? sBrake : down;
            Throttle = Mathf.MoveTowards(Throttle, thr, 2f * dt);
            car.Simulate(thr, brk, steer, dt);
            Airspeed = fwd;
            if (fwd > TakeOffSpeed && pIn > .3f && car.GroundedWheels >= 2) LiftOff(body, fwd);
            else if (HeightAboveGround > 3f && fwd > TakeOffSpeed * .8f) LiftOff(body, fwd);   // off a ramp, a roof, a ridge at speed: it flies
            yawDeg = transform.eulerAngles.y;
        }
        void LiftOff(Rigidbody body, float fwd)
        {
            Airborne = true; liftedAt = Time.time; Airspeed = Mathf.Max(fwd, TakeOffSpeed); pitchDeg = 10f; rollDeg = 0f; yawDeg = transform.eulerAngles.y;
            Throttle = Mathf.Max(Throttle, .75f); TakeOffs++;
            Debug.Log($"[Plane] lift off at ({body.position.x:0.0}, {body.position.y:0.0}, {body.position.z:0.0}), {fwd:0.0} m/s");
        }
        // checks: put it in the air where it is, level, at 'v' m/s
        public void LiftOffAt(float v) { LiftOff(car.Body, v); pitchDeg = 0f; Airspeed = v; }
        void TouchDown(Rigidbody body)
        {
            Airborne = false; body.useGravity = true; Landings++;
            var n = groundNormal.y > .8f ? groundNormal : Vector3.up; var f = Vector3.ProjectOnPlane(transform.forward, n).normalized;
            body.MoveRotation(Quaternion.LookRotation(f, n)); body.linearVelocity = f * Airspeed * Mathf.Cos(pitchDeg * Mathf.Deg2Rad); body.angularVelocity = Vector3.zero;
            Throttle = Mathf.Min(Throttle, .2f);
            Debug.Log($"[Plane] touch down at ({body.position.x:0.0}, {body.position.y:0.0}, {body.position.z:0.0}), {Airspeed:0.0} m/s");
        }
        float liftedAt = -9f;
        public int TakeOffs { get; private set; }
        public int Landings { get; private set; }

        Vector3 groundNormal = Vector3.up; Vector3 groundPoint; Collider groundCollider;
        bool GroundFlat => groundNormal.y > .9f && !OverWater;
        void Probe(Rigidbody body)
        {
            var from = body.position + Vector3.up * .2f;
            if (HelicopterFlight.GroundUnder(from, out var hit))
            { HeightAboveGround = hit.distance - .2f - WheelDrop; groundNormal = hit.normal; groundPoint = hit.point; groundCollider = hit.collider; EdgeFloor = Mathf.Lerp(EdgeFloor, hit.point.y, EdgeFloor < -9000f ? 1f : .05f); }
            else if (EdgeFloor > -9000f) { HeightAboveGround = body.position.y - WheelDrop - EdgeFloor; groundNormal = Vector3.up; groundPoint = new Vector3(body.position.x, EdgeFloor, body.position.z); groundCollider = null; }
            else { HeightAboveGround = 999f; groundNormal = Vector3.up; groundCollider = null; }
            var wheels = body.position + Vector3.down * WheelDrop;
            if (ShallowWater.SurfaceBelow(wheels + Vector3.up * .5f, out float surface) && wheels.y - surface < HeightAboveGround + .3f) { HeightAboveGround = Mathf.Max(0f, wheels.y - surface); OverWater = true; }
            else OverWater = false;
        }
        public bool OverWater { get; private set; }
        public float EdgeFloor { get; private set; } = -1e4f;
    }
}
