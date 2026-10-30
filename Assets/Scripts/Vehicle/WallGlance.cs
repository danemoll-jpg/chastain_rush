using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 85 (Dan, 2026-10-30: "in high mode ... you bounce of barriers and they keep you straight on course as opposed to having you got hung up on the
    // wall. Actually in general I want this for bumpers"). A player's vehicle that hits a guide wall (GuideWalls: HIGH MODE's gutter bumpers, a race's
    // closure barriers and the Underground's rails, the Express Lanes' walls, the bridges' parapets, the map edge's closures) at a shallow angle is turned
    // along the wall and keeps most of its speed, eased off it so it does not ride or grind along it:
    // - the speed kept falls with the angle (1 - (1 - cos a) * Loss): 10 degrees keeps about 99 %, 30 degrees about 89 % (normal mode);
    // - normal mode: up to 50 degrees; past that it is a crash as before (head on stops you);
    // - HIGH MODE (the player's own choice): up to 82 degrees, less lost, and a vehicle pinned slow against a guide wall for 0.6 s is turned along the
    //   course (the race's line, else the wall) and set going at 8 m/s: never stopped against a bumper;
    // - the push is sideways only (no climbing, no launch): the vertical speed is never raised.
    // The players' vehicles only (HighAssist.Ensure adds it); the AI, the planes and the helicopter are not changed. Breakable things (fences, signs,
    // gates, mailboxes, railings) and solid scenery (trees, houses) are not guide walls.
    public sealed class WallGlance : MonoBehaviour
    {
        public const float NormalMax = 50f, HighMax = 82f, NormalLoss = .8f, HighLoss = .6f, Settle = .18f, PinTime = .6f, PinSpeed = 8f;
        ArcadeVehicle car; VehicleConfiguration config;
        public int Player = 1;
        Vector3 lastVelocity, hitNormal, target; int hitCount; Collider hitCollider; float settleFor, pinnedFor; bool touching;
        // checks: every glance (angle in, speed before, speed after, heading error after the settle)
        public static readonly List<string> Log = new List<string>(); public static int Glances, Pins, Crashes;
        public static float LastAngle, LastBefore, LastAfter;

        public static void Ensure(ArcadeVehicle c, int player)
        {
            if (!c) return; var g = c.GetComponent<WallGlance>(); if (!g) g = c.gameObject.AddComponent<WallGlance>(); g.Player = player;
        }
        public static bool Off = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-r85GlanceOff") >= 0;   // checks only: Round 84's way, for the before
        bool On => !Off && car && !car.Body.isKinematic && !car.Puppet && !car.Flown && !(config && config.Profile != null && config.Profile.Flies);
        bool High => HighMode.ForPlayer(Player);
        void Awake() { car = GetComponent<ArcadeVehicle>(); config = GetComponent<VehicleConfiguration>(); }

        void FixedUpdate()
        {
            if (!On) { hitCount = 0; settleFor = 0; pinnedFor = 0; touching = false; if (car) lastVelocity = car.Body.linearVelocity; return; }
            var body = car.Body; float dt = Time.fixedDeltaTime;
            if (hitCount > 0) Glance(body);
            // pinned (HIGH MODE): slow against a guide wall: turned along the course and on its way
            if (touching && High && Flat(body.linearVelocity).magnitude < 2.5f) pinnedFor += dt; else pinnedFor = 0;
            if (pinnedFor >= PinTime) Unpin(body);
            // the settle: the heading and the travel held on the wall's line for a moment (the tires would steer it back into the wall otherwise)
            if (settleFor > 0)
            {
                settleFor -= dt;
                var v = body.linearVelocity; var flat = Flat(v); float sp = flat.magnitude;
                if (sp > 1f) { var dir = Vector3.Slerp(flat / sp, target, .5f).normalized; body.linearVelocity = dir * sp + Vector3.up * v.y; }
                YawToward(body, Vector3.Dot(Flat(transform.forward), target) >= 0 ? target : -target, .1f);
            }
            hitCount = 0; touching = false; lastVelocity = body.linearVelocity;
        }

        void Glance(Rigidbody body)
        {
            var n = Flat(hitNormal); if (n.sqrMagnitude < 1e-4f) return; n.Normalize();
            var before = Flat(lastVelocity); float speed = before.magnitude; if (speed < 3f) return;
            float into = -Vector3.Dot(before, n); if (into <= .2f) return;   // already leaving the wall
            var t = Vector3.Cross(Vector3.up, n); float along = Vector3.Dot(before, t);
            if (Mathf.Abs(along) < .5f) along = CourseSign(t, n);
            if (along < 0) t = -t;
            float angle = Vector3.Angle(before, t); bool high = High;
            if (angle > (high ? HighMax : NormalMax)) { Crashes++; LastAngle = angle; return; }   // head on: the wall stops it, as before
            float keep = 1f - (1f - Mathf.Cos(angle * Mathf.Deg2Rad)) * (high ? HighLoss : NormalLoss);
            float after = speed * keep, push = Mathf.Clamp(.8f + .12f * into, .8f, 3f);
            var v = body.linearVelocity; float vy = Mathf.Min(v.y, Mathf.Max(lastVelocity.y, 0f));   // never up off the wall
            body.linearVelocity = t * after + n * push + Vector3.up * vy;
            var w = body.angularVelocity; body.angularVelocity = new Vector3(w.x * .5f, 0f, w.z * .5f);
            target = (t * after + n * push).normalized; settleFor = Settle;
            YawToward(body, Vector3.Dot(Flat(transform.forward), t) >= 0 ? target : -target, .06f);
            Glances++; LastAngle = angle; LastBefore = speed; LastAfter = after;
            if (Log.Count < 400) Log.Add($"{Time.time:0.00} {(high ? "HIGH" : "normal")} {Name(hitCollider)} at ({body.position.x:0.0}, {body.position.z:0.0}): {angle:0} deg, {speed:0.0} -> {after:0.0} m/s");
        }
        // pinned against a guide wall in HIGH MODE: pointed along the course and set going
        void Unpin(Rigidbody body)
        {
            pinnedFor = 0; var n = Flat(hitNormal).normalized; var t = Vector3.Cross(Vector3.up, n);
            if (CourseSign(t, n) < 0) t = -t;
            var rot = Quaternion.LookRotation(t, Vector3.up); var pos = body.position + n * .4f;
            body.position = pos; body.rotation = rot; transform.SetPositionAndRotation(pos, rot);
            body.linearVelocity = t * PinSpeed + n * 1f; body.angularVelocity = Vector3.zero; target = (t * PinSpeed + n).normalized; settleFor = Settle;
            Physics.SyncTransforms(); Pins++;
            if (Log.Count < 400) Log.Add($"{Time.time:0.00} HIGH pinned on {Name(hitCollider)} at ({pos.x:0.0}, {pos.z:0.0}): turned along the course");
        }
        // which way along the wall: the race's line where a race runs (its forward), else the vehicle's own heading
        float CourseSign(Vector3 t, Vector3 n)
        {
            var race = RaceController.Instance;
            if (race && race.State != RaceController.Stage.Free && race.AiRoad)
            {
                float s = race.AiRoad.Project(transform.position, out _); race.AiRoad.At(s, out var f);
                return Vector3.Dot(Flat(f), t) >= 0 ? 1f : -1f;
            }
            return Vector3.Dot(Flat(transform.forward), t) >= 0 ? 1f : -1f;
        }
        void YawToward(Rigidbody body, Vector3 heading, float seconds)
        {
            float err = Vector3.SignedAngle(Flat(transform.forward), heading, Vector3.up) * Mathf.Deg2Rad;
            var w = body.angularVelocity; w.y = Mathf.Clamp(err / seconds, -9f, 9f); body.angularVelocity = w;
        }
        void OnCollisionEnter(Collision c) => Contact(c);
        void OnCollisionStay(Collision c) => Contact(c);
        void Contact(Collision c)
        {
            if (!On || c.contactCount == 0 || !GuideWalls.Is(c.collider)) return;
            Vector3 sum = Vector3.zero; int k = 0; var com = car.Body.worldCenterOfMass;
            for (int i = 0; i < c.contactCount && i < 6; i++)
            {
                var cp = c.GetContact(i); var nn = cp.normal; if (Mathf.Abs(nn.y) > .5f) continue;   // its top, a seam under the wheels: not the wall's face
                // the wall's own face there (a ray from the vehicle's middle at the contact's height): PhysX gives an edge's normal where two pieces meet,
                // which can point back against the travel and read as a head-on hit
                var from = new Vector3(com.x, cp.point.y, com.z); var dir = Flat(cp.point - from);
                if (dir.sqrMagnitude > 1e-4f && c.collider.Raycast(new Ray(from, dir.normalized), out var hit, dir.magnitude + 1.5f) && Mathf.Abs(hit.normal.y) < .5f) nn = hit.normal;
                if (Vector3.Dot(nn, com - cp.point) < 0) nn = -nn;          // pointing from the wall to the vehicle
                sum += nn; k++;
            }
            if (k == 0) return;
            touching = true; hitNormal = hitCount == 0 ? sum : hitNormal + sum; hitCount += k; hitCollider = c.collider;
            GuideWalls.Smooth(c.collider);
        }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        static string Name(Collider c) => c ? c.name : "?";
    }

    // Round 85: which colliders are guide walls (see WallGlance), and their surface made frictionless the first time one is met
    public static class GuideWalls
    {
        static readonly Dictionary<EntityId, bool> known = new Dictionary<EntityId, bool>();
        static PhysicsMaterial slick;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { known.Clear(); }
        public static bool Is(Collider c)
        {
            if (!c || c.isTrigger || c.attachedRigidbody) return false;
            var id = c.GetEntityId(); if (known.TryGetValue(id, out bool yes)) return yes;
            string n = c.name;
            yes = HighBumpers.IsBumper(c)
                || n.StartsWith("Express Lanes wall", System.StringComparison.Ordinal)
                || n == "BridgeStructure" || n == "Closure"
                || c.GetComponentInParent<RaceBarriers>() != null;
            if (known.Count > 20000) known.Clear();
            known[id] = yes; return yes;
        }
        // no friction against the vehicle's side (bumpers have their own low-friction material)
        public static void Smooth(Collider c)
        {
            if (HighBumpers.IsBumper(c)) return;
            if (!slick) slick = new PhysicsMaterial("Guide wall") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0f, bounceCombine = PhysicsMaterialCombine.Minimum };
            if (c.sharedMaterial != slick) c.sharedMaterial = slick;
        }
    }
}
