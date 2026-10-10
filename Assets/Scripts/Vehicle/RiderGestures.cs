using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Racer
{
    // 0.78 Part B: the parametric rider's arms (cosmetic only). Tools/Blender/rider.py exports each arm as an upper arm,
    // forearm and hand whose origins are the shoulder, elbow and wrist; VehicleVisual.Rider hands them to Rig, which hangs
    // them on a three-joint chain per side. With no gesture the joints stay at rest, so the riding poses are unchanged.
    // Positions here are in the rider holder's space (the FBX space, turned half a turn from the vehicle's): Out() converts
    // a vehicle-space offset (x right, y up, z forward) into it.
    public sealed class RiderArms : MonoBehaviour
    {
        public string pose;
        readonly Transform[] shoulder = new Transform[2], elbow = new Transform[2], wrist = new Transform[2];
        readonly Vector3[] restElbow = new Vector3[2], restWrist = new Vector3[2];
        Renderer eyes;
        public bool Ready => shoulder[0] && shoulder[1] && elbow[0] && elbow[1] && wrist[0] && wrist[1];
        public static Vector3 Out(Vector3 v) => new(-v.x, v.y, -v.z);

        public static void Rig(Transform holder, string pose, List<(Transform part, string joint)> parts)
        {
            if (parts.Count == 0) return;
            var arms = holder.gameObject.AddComponent<RiderArms>(); arms.pose = pose;
            for (int side = 0; side < 2; side++)
            {
                char s = side == 0 ? 'L' : 'R';
                Transform Joint(char seg, Transform parent, string name)
                {
                    var mine = parts.Where(p => p.joint.Length == 2 && p.joint[0] == seg && p.joint[1] == s).ToList();
                    var j = new GameObject(name).transform; j.SetParent(parent, false); j.gameObject.layer = holder.gameObject.layer;
                    if (mine.Count > 0) j.position = mine[0].part.position;
                    j.rotation = holder.rotation;
                    foreach (var p in mine) p.part.SetParent(j, true);
                    return j;
                }
                arms.shoulder[side] = Joint('U', holder, "Shoulder " + s);
                arms.elbow[side] = Joint('L', arms.shoulder[side], "Elbow " + s);
                arms.wrist[side] = Joint('H', arms.elbow[side], "Wrist " + s);
                arms.restElbow[side] = holder.InverseTransformPoint(arms.elbow[side].position);
                arms.restWrist[side] = holder.InverseTransformPoint(arms.wrist[side].position);
            }
            arms.eyes = holder.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.Contains("_Base_") && r.name.EndsWith("__eyes"));
        }

        public Vector3 Shoulder(int side) => shoulder[side].localPosition;
        // The camera is at this rider's eyes (first person, set by RiderGestures): gestures are aimed into that view.
        public bool SeenFromEyes { get; set; }
        public Vector3 RestWrist(int side) => restWrist[side];
        public Vector3 RestElbow(int side) => restElbow[side];
        public float Reach(int side) => (restElbow[side] - Shoulder(side)).magnitude + (restWrist[side] - restElbow[side]).magnitude;

        public void Rest(int side) { shoulder[side].localRotation = elbow[side].localRotation = wrist[side].localRotation = Quaternion.identity; }

        // Two-bone IK: the wrist goes to target (holder space), the elbow bends toward pole; twist turns the fist about the
        // forearm (degrees). weight blends the joint rotations from rest (0) to the solved pose (1), so the arm swings
        // there along a natural arc and never snaps.
        public void Reach(int side, Vector3 target, Vector3 pole, float twist, float weight)
        {
            Vector3 S = Shoulder(side), E0 = restElbow[side], W0 = restWrist[side];
            float a = (E0 - S).magnitude, b = (W0 - E0).magnitude;
            Vector3 to = target - S; if (to.sqrMagnitude < 1e-6f) to = W0 - S;
            float d = Mathf.Clamp(to.magnitude, Mathf.Abs(a - b) + .002f, a + b - .002f); Vector3 dir = to.normalized;
            float cos = Mathf.Clamp((a * a + d * d - b * b) / (2 * a * d), -1, 1), sin = Mathf.Sqrt(1 - cos * cos);
            Vector3 n = Vector3.ProjectOnPlane(pole, dir); n = n.sqrMagnitude < 1e-6f ? Vector3.Cross(dir, Vector3.right).normalized : n.normalized;
            Vector3 E = S + dir * (a * cos) + n * (a * sin);
            var r1 = Quaternion.FromToRotation(E0 - S, E - S);
            Vector3 W1 = E + r1 * (W0 - E0);
            var r2 = Quaternion.FromToRotation(W1 - E, S + dir * d - E);
            var arm = r2 * r1; Vector3 forearm = (S + dir * d - E).normalized;
            shoulder[side].localRotation = Quaternion.Slerp(Quaternion.identity, r1, weight);
            elbow[side].localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Inverse(r1) * r2 * r1, weight);
            wrist[side].localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Inverse(arm) * Quaternion.AngleAxis(twist, forearm) * arm, weight);
        }
    }

    // 0.78 Part B: fist wave (player button F / LB since 0.80; an AI rider at whoever ran into it) and the winner's victory celebration.
    // Purely visual: it reads the vehicle and never touches input, physics, AI driving or records. Needs the New models
    // (RiderArms); with Classic nothing happens. Added to every vehicle by VehicleConfiguration.Apply, so AI clones have it.
    public sealed class RiderGestures : MonoBehaviour
    {
        public const float WaveSeconds = 1.7f, CelebrateSeconds = 3.6f, PlayerCooldown = 2.2f;
        public enum Kind { None, Wave, Celebrate }
        public Kind Current { get; private set; }
        public int Waves { get; private set; }
        public int Celebrations { get; private set; }
        public float Weight { get; private set; }
        public float Age => Time.time - started;
        public bool HasArms => Arms();
        public static int AiWaves;
        ArcadeVehicle car; VehicleConfiguration config; RiderArms arms; Transform armsVisual;   // Chastain Rush: no RaceDirector / RoadDriver
        float started, cooldownUntil, pendingAt = -1, aimYaw;
        Transform offender, lastOffender; float lastHit = -100; bool wasWiped;
        System.Random random;

        void Awake() { car = GetComponent<ArcadeVehicle>(); config = GetComponent<VehicleConfiguration>(); random = new System.Random(name.GetHashCode() ^ (int)(transform.position.x * 1000)); }
        bool Ai => false;   // Chastain Rush: no AI drivers yet (Round 14)
        RiderArms Arms()
        {
            var visual = transform.Find("Vehicle visual");
            if (visual != armsVisual || (visual && !arms)) { armsVisual = visual; arms = visual ? visual.GetComponentInChildren<RiderArms>(true) : null; Current = Kind.None; Weight = 0; }
            return arms && arms.Ready ? arms : null;
        }
        bool Rough => (config && config.WipedOut) || transform.up.y < .5f;

        // Player button: one wave, then a short cooldown. Returns whether it started.
        public bool Wave(Transform toward = null)
        {
            if (Current != Kind.None || Time.time < cooldownUntil || Rough || !Arms()) return false;
            Begin(Kind.Wave); Aim(toward); Waves++; cooldownUntil = Time.time + WaveSeconds + PlayerCooldown; return true;
        }
        public bool Celebrate()
        {
            if (!Arms()) return false;
            Begin(Kind.Celebrate); Celebrations++; return true;
        }
        void Begin(Kind kind) { Current = kind; started = Time.time; aimYaw = 0; }
        void Aim(Transform toward)
        {
            if (!toward) return;
            var local = transform.InverseTransformPoint(toward.position); local.y = 0;
            // The left arm swings toward the offender: mostly to the left side and ahead, a little to the right.
            if (local.sqrMagnitude > .01f) aimYaw = Mathf.Clamp(Mathf.Atan2(local.x, Mathf.Max(local.z, -2)) * Mathf.Rad2Deg, -60, 25);
        }

        // AI: hit hard enough to knock its line by another vehicle that was doing the hitting, or wiped out by one.
        void OnCollisionEnter(Collision c)
        {
            if (!Ai || !c.rigidbody || c.rigidbody.transform == transform) return;
            var other = c.rigidbody.GetComponent<ArcadeVehicle>(); if (!other) return;
            Vector3 toMe = transform.position - c.rigidbody.position; toMe.y = 0; if (toMe.sqrMagnitude < 1e-4f) return; toMe.Normalize();
            var mine = GetComponent<Rigidbody>();
            float theirs = Vector3.Dot(c.rigidbody.linearVelocity, toMe), ours = mine ? Vector3.Dot(mine.linearVelocity, -toMe) : 0;
            float knock = mine && mine.mass > 0 ? c.impulse.magnitude / mine.mass : 0;
            if (theirs <= ours || (knock < 1.2f && c.relativeVelocity.magnitude < 4)) return;
            lastOffender = other.transform; lastHit = Time.time;
            if (Current == Kind.None && pendingAt < 0 && Time.time >= cooldownUntil && random.NextDouble() < .75)
            { offender = other.transform; pendingAt = Time.time + .35f + (float)random.NextDouble() * .5f; }
        }

        void Update()
        {
            if (!Ai) return;
            bool wiped = config && config.WipedOut;
            // Recovered from a wipe-out someone caused: shake a fist at them once upright again.
            if (wasWiped && !wiped && Time.time - lastHit < 10 && lastOffender && pendingAt < 0) { offender = lastOffender; pendingAt = Time.time + .6f; }
            wasWiped = wiped;
            if (pendingAt >= 0 && Time.time >= pendingAt)
            {
                pendingAt = -1;
                if (Current == Kind.None && !Rough && Arms()) { Begin(Kind.Wave); Aim(offender); Waves++; AiWaves++; cooldownUntil = Time.time + WaveSeconds + 5 + (float)random.NextDouble() * 4; }
            }
        }

        void LateUpdate()
        {
            var a = Arms(); if (!a) return;
            // first person on this vehicle (the player's view or the Trailer Mode first-person shot)
            // 0.94 Part B: the view of this vehicle (in split-screen each player's own)
            // Chastain Rush: chase camera only (no CameraViews), so never seen from the rider's eyes
            a.SeenFromEyes = false;
            bool holds = Steering(a);
            if (Current == Kind.None) { if (holds) { Weight = 0; Hold(a, 0); Hold(a, 1); } else if (Weight > 0) { Weight = 0; a.Rest(0); a.Rest(1); } return; }
            float t = Time.time - started, length = Current == Kind.Wave ? WaveSeconds : CelebrateSeconds;
            // Wipe-outs drop the gesture at once (the rider grabs the bars).
            if (t >= length || (Current == Kind.Wave && Rough)) { Current = Kind.None; Weight = 0; if (holds) { Hold(a, 0); Hold(a, 1); } else { a.Rest(0); a.Rest(1); } return; }
            float rise = Current == Kind.Wave ? .3f : .35f, fall = .35f;
            float w = Mathf.SmoothStep(0, 1, Mathf.Min(t / rise, (length - t) / fall)); Weight = w;
            float beat = Mathf.Clamp01((t - rise * .8f) / (length - rise - fall));
            bool carPose = a.pose == "Car";
            if (Current == Kind.Wave)
            {
                // Left arm (the right hand stays on the throttle / wheel): a raised fist beside the head, shaken three times; in
                // the cars out of the driver's window. Seen from the rider's own eyes (first person) the fist comes up ahead of
                // the face instead, so the player sees their own arm.
                float shake = Mathf.Sin(beat * Mathf.PI * 2 * 3);
                Vector3 offset = a.SeenFromEyes ? new Vector3(carPose ? .10f : .06f, (carPose ? .22f : .28f) + .04f * shake, .44f + .03f * shake)
                    : carPose ? new Vector3(-.40f, .24f + .045f * shake, .10f) : new Vector3(-.20f, .30f + .05f * shake, .36f + .03f * shake);
                if (!carPose && !a.SeenFromEyes) offset = Quaternion.Euler(0, aimYaw, 0) * offset;
                Pose(a, 0, offset, carPose ? new Vector3(-1, -1, -.2f) : new Vector3(-1, -.7f, -.3f), w, 25 * shake);
                Free(a, 1);
            }
            else
            {
                // Both fists up, pumped three times (no-handed on the bikes); in the cars a fist pumped out of the window.
                float pump = Mathf.Abs(Mathf.Sin(beat * Mathf.PI * 3));
                if (carPose) { Pose(a, 0, new Vector3(-.38f, .22f + .09f * pump, .10f), new Vector3(-1, -1, -.2f), w, 0); Free(a, 1); }
                else for (int side = 0; side < 2; side++)
                    {
                        float s = side == 0 ? -1 : 1;
                        Pose(a, side, new Vector3(s * .17f, .40f + .12f * pump, .10f), new Vector3(s, -.4f, -.4f), w, 0);
                    }
            }
        }

        // Swings the arm from its place on the wheel / bars (from rest with no steering part) toward wrist = shoulder + offset
        // (vehicle-space metres), kept within the arm's reach.
        void Pose(RiderArms a, int side, Vector3 offset, Vector3 pole, float w, float twist)
        {
            Vector3 S = a.Shoulder(side), goal = S + RiderArms.Out(offset);
            if ((goal - S).magnitude > a.Reach(side) * .96f) goal = S + (goal - S).normalized * a.Reach(side) * .96f;
            if (!steerTurn) { a.Reach(side, goal, RiderArms.Out(pole), twist, w); return; }
            HoldTarget(a, side, out var hold, out var holdPole, out float holdTwist);
            a.Reach(side, Vector3.Lerp(hold, goal, w), Vector3.Lerp(holdPole, RiderArms.Out(pole), w), Mathf.Lerp(holdTwist, twist, w), 1);
        }
        // the hand a gesture does not use: on the wheel / bars, or at rest
        void Free(RiderArms a, int side) { if (steerTurn) Hold(a, side); else a.Rest(side); }

        // 0.85 Part B: the hands hold the steering. The steering wheel (cars) or the bars (bikes, ATV) turn about their
        // axis with the visual steering (MotorcycleFrontEnd), and the rider's pose turns a little less (VehiclePose); each
        // wrist goes where its rest place on the rim / grip has turned to (two-bone IK, elbow bent the way it is at rest),
        // and the hand rolls with the wheel. At zero steering this is exactly the rest pose. Purely visual.
        MotorcycleFrontEnd steerTurn; VehiclePose steerPose; RiderArms steerArms;
        bool Steering(RiderArms a)
        {
            if (steerArms != a)
            {
                steerArms = a; steerTurn = null; steerPose = null;
                var pose = a.transform.parent; var root = pose ? pose.parent : null;
                if (root && pose.TryGetComponent(out steerPose))
                    foreach (Transform child in root) if (child.TryGetComponent<MotorcycleFrontEnd>(out var turn)) { steerTurn = turn; break; }
                if (!steerPose) steerTurn = null;
            }
            return steerTurn;
        }
        void HoldTarget(RiderArms a, int side, out Vector3 target, out Vector3 pole, out float twist)
        {
            // vehicle-visual (root) space: the rider holder sits under the steering pose
            Transform holder = a.transform, pose = holder.parent; float steer = car ? car.VisualSteering : 0;
            Vector3 rest = pose.localPosition + holder.localPosition + holder.localRotation * a.RestWrist(side);
            Quaternion turn = steerTurn.Turn(steer); Vector3 pivot = steerTurn.transform.localPosition;
            Vector3 onWheel = pivot + turn * (rest - pivot);
            Quaternion toHolder = Quaternion.Inverse(holder.localRotation) * Quaternion.Inverse(VehiclePose.Turn(steer, steerPose.bike));
            target = toHolder * (onWheel - pose.localPosition) - Quaternion.Inverse(holder.localRotation) * holder.localPosition;
            pole = a.RestElbow(side) - a.Shoulder(side);
            Vector3 forearm = (a.RestWrist(side) - a.RestElbow(side)).normalized, axis = toHolder * steerTurn.Axis;
            twist = steerTurn.Angle(steer) * Vector3.Dot(axis, forearm);
        }
        void Hold(RiderArms a, int side) { HoldTarget(a, side, out var target, out var pole, out float twist); a.Reach(side, target, pole, twist, 1); }
    }
}
