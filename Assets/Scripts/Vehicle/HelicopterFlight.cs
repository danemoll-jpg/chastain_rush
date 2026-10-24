using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 63 (Dan: "a helicopter as a start ... a good way for me to survey the map more quickly"): the player's helicopter. Arcade, not a
    // simulator. It is the player's own vehicle object with the "heli" profile (VehicleProfile.Helicopter), so the camera, the map, the minimap,
    // exploration and resets keep working; VehicleConfiguration.Apply adds this and starts it, and ArcadeVehicle does not drive while it flies
    // (ArcadeVehicle.Flown). Free Roam only (VehicleProfile.Flies keeps it out of races, rosters, the Stunt event, split screen and police modes).
    //   Pad:      left stick up / down climb / descend, left / right turn; right stick flies (forward / back / sideways); RT / LT fine climb /
    //             descend; RB (or the left stick's click) boost; X view (chase, far chase, first person, straight down); Y reset.
    //   Keyboard: W / S fly forward / back, A / D turn, Q / E sideways, Space / C (or Ctrl) climb / descend, Shift boost, V view, R reset.
    // Let go of the sticks and it holds a steady hover at its height. It never comes down faster than it can stop (a soft landing); it lands on anything flat
    // enough. A crash (the body into something at speed, the rotor into a tree crown, a building, a pole or the ground, the tail rotor, or
    // set down in water) spins the rotor down, then after a moment puts it on the nearest open ground (HelicopterFlight.Crash.cs).
    public sealed partial class HelicopterFlight : MonoBehaviour
    {
        public const float TopSpeed = 80f, BoostSpeed = 140f, Accel = 14f, BoostAccel = 22f, Climb = 18f, Descend = 20f, YawRate = 95f, Ceiling = 1700f;
        public const float SkidDrop = 1.10f;   // the skids' bottom below the body's middle (Tools/Blender/helicopter.py GROUND)
        ArcadeVehicle car; VehicleInput vInput; VehicleConfiguration config;
        Transform visual, mainRotor, tailRotor, disc; Renderer discRenderer;
        InputAction lift, fineUp, fineDown, yaw, cyclic, cyclicKeys, sideKeys, boost;
        float holdY, yawDeg, rotorAngle, tailAngle;
        public float Spool { get; private set; }            // rotor speed 0..1
        public bool Landed { get; private set; }
        public bool Boosting { get; private set; }
        public float HeightAboveGround { get; private set; } // the skids above whatever is under them (m)
        public bool Crashed => crashedAt >= 0;
        public static HelicopterFlight PlayerHeli { get; private set; }
        // checks only: scripted sticks (lift, yaw, forward, side, boost), read in place of the devices
        [System.NonSerialized] public bool Scripted; [System.NonSerialized] public float sLift, sYaw, sForward, sSide; [System.NonSerialized] public bool sBoost;

        // Exploration: the fog clears wider under a helicopter the higher it flies (48 m on the ground, up to 300 m from 420 m up)
        public static float RevealBonus(ArcadeVehicle v) => v && v.TryGetComponent<HelicopterFlight>(out var h) && h.enabled && !h.Landed ? Mathf.Clamp(h.HeightAboveGround * .6f, 0f, 252f) : 0f;
        public static bool Flying(ArcadeVehicle v) => v && v.TryGetComponent<HelicopterFlight>(out var h) && h.enabled;
        public static bool Flying(Component c) => c && c.TryGetComponent<HelicopterFlight>(out var h) && h.enabled;

        // VehicleConfiguration.Apply: the helicopter profile is on (visual = the vehicle's new visual)
        public void Begin(Transform newVisual)
        {
            car = GetComponent<ArcadeVehicle>(); vInput = GetComponent<VehicleInput>(); config = GetComponent<VehicleConfiguration>();
            visual = newVisual;
            mainRotor = visual ? visual.Find("Main rotor") : null; tailRotor = visual ? visual.Find("Tail rotor") : null; disc = visual ? visual.Find("Rotor disc") : null;
            discRenderer = disc ? disc.GetComponentInChildren<Renderer>() : null;
            bool wasOn = enabled && car.Flown;
            car.Flown = true; car.Body.useGravity = true; car.Body.maxAngularVelocity = 4;
            if (!wasOn)
            {
                Spool = 0; Landed = true; crashedAt = -1;
                // swapped in among trees (the garage, anywhere): it starts on the nearest clear ground instead of striking them as it spools up
                var at = car.Body.position;
                if (!TreeCrowns.Clear(at) && ResetSpot(at, out var spot, out float y))
                { car.Body.position = spot; car.Body.rotation = Quaternion.Euler(0, y, 0); transform.SetPositionAndRotation(spot, car.Body.rotation); car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero; }
            }
            holdY = car.Body.position.y; yawDeg = transform.eulerAngles.y;
            if (vInput && vInput.Device == null && GetComponent<VehicleSwitch>() is VehicleSwitch sw && sw.player == 1) PlayerHeli = this;
            enabled = true;
            MakeInput(); StartEffects();
        }
        // VehicleConfiguration.Apply: another profile is on
        public void End()
        {
            if (!enabled) return;
            enabled = false;
            if (car) { car.Flown = false; car.Body.useGravity = true; car.Body.maxAngularVelocity = 5; }
            if (vInput) vInput.AirActive = false;
            StopEffects(); DisposeInput();
            if (PlayerHeli == this) PlayerHeli = null;
        }
        void OnDestroy() { DisposeInput(); if (PlayerHeli == this) PlayerHeli = null; }

        void MakeInput()
        {
            if (lift != null) return;
            InputAction A(string name, params (string path, string proc)[] b) { var a = new InputAction(name, InputActionType.Value); foreach (var (p, pr) in b) { var x = a.AddBinding(p); if (pr != null) x.WithProcessor(pr); } a.Enable(); return a; }
            lift = A("Heli lift", ("<Gamepad>/leftStick/y", "axisDeadzone(min=0.15,max=0.95)"));
            lift.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/c").With("Positive", "<Keyboard>/space");
            lift.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftCtrl").With("Positive", "<Keyboard>/space");
            fineUp = A("Heli fine up", ("<Gamepad>/rightTrigger", "axisDeadzone(min=0.1,max=1)"));
            fineDown = A("Heli fine down", ("<Gamepad>/leftTrigger", "axisDeadzone(min=0.1,max=1)"));
            yaw = A("Heli yaw", ("<Gamepad>/leftStick/x", "axisDeadzone(min=0.15,max=0.95)"));
            yaw.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            cyclic = new InputAction("Heli cyclic", InputActionType.Value, expectedControlType: "Vector2"); cyclic.AddBinding("<Gamepad>/rightStick").WithProcessor("stickDeadzone(min=0.15,max=0.95)"); cyclic.Enable();
            cyclicKeys = new InputAction("Heli cyclic keys", InputActionType.Value);
            cyclicKeys.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/s").With("Positive", "<Keyboard>/w");
            cyclicKeys.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/downArrow").With("Positive", "<Keyboard>/upArrow"); cyclicKeys.Enable();
            sideKeys = new InputAction("Heli side keys", InputActionType.Value);
            sideKeys.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            sideKeys.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow"); sideKeys.Enable();
            boost = new InputAction("Heli boost", InputActionType.Button); boost.AddBinding("<Gamepad>/rightShoulder"); boost.AddBinding("<Gamepad>/leftStickPress"); boost.AddBinding("<Keyboard>/leftShift"); boost.Enable();
        }
        void DisposeInput()
        {
            foreach (var a in new[] { lift, fineUp, fineDown, yaw, cyclic, cyclicKeys, sideKeys, boost }) a?.Dispose();
            lift = fineUp = fineDown = yaw = cyclic = cyclicKeys = sideKeys = boost = null;
        }

        static bool InputFree => !MenuInput.Blocked && !GameMenus.Open && !WorldMap.OwnsInput && Time.timeScale > 0;
        void Read(out float l, out float y, out float f, out float s, out bool b)
        {
            if (Scripted) { l = sLift; y = sYaw; f = sForward; s = sSide; b = sBoost; return; }
            l = y = f = s = 0; b = false;
            if (lift == null || !InputFree || (vInput && !vInput.enabled)) return;
            l = Mathf.Clamp(lift.ReadValue<float>() + .45f * (fineUp.ReadValue<float>() - fineDown.ReadValue<float>()), -1, 1);
            y = yaw.ReadValue<float>();
            var c = cyclic.ReadValue<Vector2>(); f = Mathf.Clamp(c.y + cyclicKeys.ReadValue<float>(), -1, 1); s = Mathf.Clamp(c.x + sideKeys.ReadValue<float>(), -1, 1);
            b = boost.IsPressed();
        }

        void Update()
        {
            if (vInput) vInput.AirActive = true;   // the right stick flies (VehicleInput: no look-around on it), the bumpers do not spin
            Effects(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (!car || car.Body.isKinematic) return;
            var body = car.Body; float dt = Time.fixedDeltaTime;
            Read(out float l, out float yIn, out float f, out float s, out bool b);
            Probe(body);
            if (Crashed) { CrashStep(dt); return; }
            Spool = Mathf.MoveTowards(Spool, 1f, dt / 2.2f);
            CheckStrikes(body);
            if (Crashed) return;
            var v = body.linearVelocity; var pos = body.position;
            // on the ground: it sits there (gravity, the ground holds it) until lifted with the rotor up to speed
            if (Landed)
            {
                Boosting = false;
                if (l > .15f && Spool > .85f) { Landed = false; holdY = pos.y + 2f; }
                else { Settle(body, dt); return; }
            }
            Boosting = b && Spool > .95f;
            float maxH = Boosting ? BoostSpeed : TopSpeed, accel = Boosting ? BoostAccel : Accel;
            yawDeg += yIn * YawRate * Mathf.Lerp(1f, .7f, new Vector2(v.x, v.z).magnitude / BoostSpeed) * dt;
            var heading = Quaternion.Euler(0, yawDeg, 0); Vector3 fwd = heading * Vector3.forward, right = heading * Vector3.right;
            var want = fwd * f + right * s; if (want.sqrMagnitude > 1) want.Normalize(); want *= maxH;
            var vH = new Vector3(v.x, 0, v.z);
            bool sticks = Mathf.Abs(f) > .02f || Mathf.Abs(s) > .02f;
            var aH = Vector3.ClampMagnitude((want - vH) * (sticks ? 0.9f : 1.4f), accel);
            // vertical: the stick climbs or descends; let go and it holds the height it was at
            float wantVy;
            if (Mathf.Abs(l) > .08f) { wantVy = l > 0 ? l * Climb : l * Descend; holdY = pos.y + v.y * Mathf.Abs(v.y) / (2f * 14f); }   // let go: it holds where it coasts to
            else wantVy = Mathf.Clamp((holdY - pos.y) * 1.2f, -6f, 6f);
            if (pos.y > Ceiling && wantVy > 0) { wantVy = 0; holdY = Mathf.Min(holdY, Ceiling); }
            // the soft landing: never coming down faster than it can stop before the ground (about 20 m/s from 25 m, 1.2 m/s at the last)
            if (wantVy < 0) wantVy = Mathf.Max(wantVy, -Mathf.Max(1.2f, Mathf.Sqrt(16f * Mathf.Max(0f, HeightAboveGround - .3f))));
            float aY = Mathf.Clamp((wantVy - v.y) * 3f, -16f, 16f);
            body.AddForce((new Vector3(aH.x, aY, aH.z)) * Spool - Physics.gravity * Spool, ForceMode.Acceleration);
            // touch down: slow, low, not climbing
            if (HeightAboveGround < .25f && v.y < 1.5f && l <= .08f && vH.magnitude < 6f) { Landed = true; holdY = pos.y; }
            // attitude: nose down to fly forward, banked into a turn and a sideways slide (arcade: the tilt follows the flight)
            var local = Quaternion.Inverse(heading) * (vH + aH * .9f);
            float pitch = Mathf.Clamp(local.z / TopSpeed * 16f, -22f, 22f) + Mathf.Clamp(Vector3.Dot(aH, fwd) * .9f, -8f, 8f);
            float roll = Mathf.Clamp(-local.x / TopSpeed * 14f - yIn * Mathf.Clamp01(vH.magnitude / 25f) * 14f - Vector3.Dot(aH, right) * .7f, -28f, 28f);
            Steer(body, Quaternion.Euler(pitch, yawDeg, roll), 5f);
        }

        // on the ground: level with the ground under the skids, no lift
        void Settle(Rigidbody body, float dt)
        {
            yawDeg = transform.eulerAngles.y;
            var up = groundNormal.y > .8f ? groundNormal : Vector3.up;
            var fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            if (fwd.sqrMagnitude > .5f) Steer(body, Quaternion.LookRotation(fwd, up), 3f);
            var vH = Vector3.ProjectOnPlane(body.linearVelocity, up); if (vH.magnitude < 4f) body.AddForce(-vH * 3f, ForceMode.Acceleration);
            holdY = body.position.y;
            if (HeightAboveGround > 1.2f) Landed = false;   // pushed off an edge, a roof: it flies on (and holds there)
        }

        void Steer(Rigidbody body, Quaternion target, float gain)
        {
            var delta = target * Quaternion.Inverse(body.rotation); delta.ToAngleAxis(out float ang, out var axis);
            if (ang > 180f) ang -= 360f;
            if (float.IsNaN(axis.x) || Mathf.Abs(ang) < .01f) { body.angularVelocity = Vector3.Lerp(body.angularVelocity, Vector3.zero, .3f); return; }
            body.angularVelocity = Vector3.ClampMagnitude(axis * (ang * Mathf.Deg2Rad * gain), 3.5f);
        }

        Vector3 groundNormal = Vector3.up; Vector3 groundPoint; Collider groundCollider;
        void Probe(Rigidbody body)
        {
            var from = body.position + Vector3.up * .2f;
            if (Physics.Raycast(from, Vector3.down, out var hit, 4000f, ~0, QueryTriggerInteraction.Ignore))
            { HeightAboveGround = hit.distance - .2f - SkidDrop; groundNormal = hit.normal; groundPoint = hit.point; groundCollider = hit.collider; }
            else { HeightAboveGround = 999f; groundNormal = Vector3.up; groundPoint = body.position - Vector3.up * 999f; groundCollider = null; }
            // water under the skids reads as the ground for the height (and a touch down in it is a ditching: CheckStrikes)
            var skids = body.position + Vector3.down * SkidDrop;
            if (ShallowWater.SurfaceBelow(skids + Vector3.up * .5f, out float surface) && skids.y - surface < HeightAboveGround + .3f)
            { HeightAboveGround = Mathf.Max(0f, skids.y - surface); OverWater = true; }
            else OverWater = false;
        }
        public bool OverWater { get; private set; }
    }
}
