using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 18, item 4: a drive-through drainage pipe (MapBuilder.Pipes.cs builds it from Data/pipes/pipes.json). Pipes are their
    // own layer under the terrain: an inlet cut down from the ground, a round concrete pipe under it, an outlet cut back up; the
    // terrain has holes only over the cuts. This component knows the pipe's floor, for:
    //   resets   a reset (R / Y) inside a pipe puts the vehicle back on the pipe's floor, facing along it (the way the vehicle
    //            was heading; in a race, the way the race goes through it), on the first spot clear of other vehicles
    //   lines    a race line through a pipe (a shortcut's path, the AI's line) takes its heights from the pipe's floor, not from
    //            the ground above it (RaceRoute)
    //   walls    Round 19 (BUG-002, Dan: "can we have the ability so that you could drive around the walls?so if you had enough
    //            momentum you could even circle them or ride the walls"): in the round part (the barrel) a vehicle going faster
    //            than StickFrom is held to the pipe's surface: its gravity turns to point at the wall under it (out from the
    //            axis), fully from StickFull up, so steering up the side carries it up the wall and, fast enough, right over the
    //            top and round. Slower, the real gravity comes back and it slides or drops to the bottom, righted in the air so it
    //            lands on its wheels. The hold fades out over the last 5 m to each headwall (the cuts are open, flat sided). Arcade
    //            by design (fun first): no speed is needed round the top beyond StickFull. The AI just drives the bottom.
    //   camera   Round 19: inside the barrel the chase camera rides the pipe's axis behind the vehicle, world up (ChaseCamera.PipeView)
    //   lights   Round 19: a vehicle in a pipe has its headlight on (VehicleLights)
    public sealed class DrainPipe : MonoBehaviour
    {
        public string id;
        public Vector3[] floor;          // the floor's centerline, a to b, 0.5 m apart (cuts and pipe)
        public float halfWidth = 2.4f;   // inside the cut walls; the pipe's own floor is narrower, its sides curve up
        public float clearHeight = 4.4f; // floor to the pipe's crown
        public float radius = 2.5f;      // Round 19: the barrel's inside radius; its invert is the floor line
        public float barrelFrom, barrelTo;   // the barrel's ends, as stations (indices into floor)
        // Round 23 follow-ups: a network pipe is built in stretches of about 60 m, each with its own DrainPipe; an open end carries on
        // into the next stretch: no fading of the hold to the wall there, and a vehicle past it belongs to the next stretch
        public bool openStart, openEnd;
        public Color mapColor;   // Round 23 follow-ups: the line's color on the map (a network main's stripe; none: gray)
        Bounds box; bool boxed;
        bool Near(Vector3 p, float pad)   // a quick test against the floor's bounds before the search along it
        {
            if (!boxed && floor != null && floor.Length > 0)
            {
                box = new Bounds(floor[0], Vector3.zero); foreach (var f in floor) box.Encapsulate(f);
                box.Expand(new Vector3(2f * (halfWidth + radius) + 2f, 2f * (clearHeight + radius) + 4f, 2f * (halfWidth + radius) + 2f)); boxed = true;
            }
            return boxed && box.SqrDistance(p) <= pad * pad;
        }
        // Round 23 follow-ups: the axis is the floor's point plus the radius square to the floor's slope (on a manhole's 50 degree riser
        // the axis is not straight up from the floor)
        Vector3 AxisFrom(Vector3 f, Vector3 along) { var n = Vector3.up - along * Vector3.Dot(Vector3.up, along); return f + (n.sqrMagnitude > 1e-6f ? n.normalized : Vector3.up) * radius; }
        public const float StickFrom = 7f, StickFull = 11f, EndFade = 5f;
        public static int WallSteps { get; private set; }   // checks: physics steps some vehicle was held to the wall more than 45 degrees up it
        public static float MostRound { get; private set; }  // checks: the furthest round from the invert any vehicle went, degrees
        public static int Rescues { get; private set; }
        static readonly bool NoSlide = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-noPipeSlide") >= 0;   // checks only      // checks: vehicles put back in the bottom after lying overturned and slow
        static readonly List<DrainPipe> all = new List<DrainPipe>();
        static readonly Collider[] hits = new Collider[16];

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install() { ChaseCamera.PipeView = View; }

        // ---------------------------------------------------------------- Round 19: riding the walls
        sealed class Ride { public float lastGround = -10f, stick, stuck; }
        readonly Dictionary<ArcadeVehicle, Ride> rides = new Dictionary<ArcadeVehicle, Ride>();
        // Round 23 follow-ups: one search for the vehicles a second for every pipe (the network has a hundred pieces: each searching on
        // its own, all in the same frame, was a hitch every second)
        static ArcadeVehicle[] cars = new ArcadeVehicle[0]; static float nextFind;
        public static ArcadeVehicle[] Vehicles { get { if (Time.time >= nextFind || Time.time < nextFind - 2f) { cars = FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None); nextFind = Time.time + 1f; } return cars; } }

        // where p is in the barrel: its axis point, the axis direction, how far into the barrel from its nearer end (m)
        bool Barrel(Vector3 p, float pad, out Vector3 axis, out Vector3 along, out float fromEnd, out float station)
        {
            axis = default; along = Vector3.forward; fromEnd = 0f; station = 0f;
            if (floor == null || floor.Length < 2 || barrelTo <= barrelFrom) return false;
            if (!Near(p, 1f)) return false;
            float best = float.MaxValue; var q = new Vector2(p.x, p.z);
            int i0 = Mathf.Max(0, (int)barrelFrom - 6), i1 = Mathf.Min(floor.Length - 2, (int)barrelTo + 6);
            for (int i = i0; i <= i1; i++)
            {
                var a = new Vector2(floor[i].x, floor[i].z); var ab = new Vector2(floor[i + 1].x, floor[i + 1].z) - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                float d = (a + ab * t - q).sqrMagnitude;
                if (d < best) { best = d; station = i + t; }
            }
            if (station < barrelFrom - pad * 2f || station > barrelTo + pad * 2f) return false;
            int k = Mathf.Clamp((int)station, 0, floor.Length - 2);
            var f = Vector3.Lerp(floor[k], floor[k + 1], station - k);
            along = (floor[k + 1] - floor[k]).normalized; axis = AxisFrom(f, along);
            // Round 23 follow-ups: past an open end the vehicle is in the next stretch
            if (openEnd && station >= floor.Length - 1.001f && Vector3.Dot(p - floor[floor.Length - 1], floor[floor.Length - 1] - floor[floor.Length - 2]) > 0f) return false;
            if (openStart && station <= 0.001f && Vector3.Dot(p - floor[0], floor[1] - floor[0]) < 0f) return false;
            var r = p - axis; r -= along * Vector3.Dot(r, along);
            if (r.magnitude > radius + 0.6f) return false;
            fromEnd = Mathf.Min(openStart ? 999f : station - barrelFrom, openEnd ? 999f : barrelTo - station) * 0.5f;
            return true;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            foreach (var car in Vehicles)
            {
                if (!car || car.Body == null || car.Body.isKinematic) continue;
                // Round 24 A2 (A1, the frame rate): a vehicle nowhere near this piece costs one box test. Before, every piece asked
                // Inside() (a search of every piece) for every vehicle on every physics step: with the network's ~260 pieces and a
                // race's four vehicles that was ~320 ms a frame, and the slow frames ran more physics steps, down to 3 fps.
                rides.TryGetValue(car, out var ride);
                if (!Near(car.Body.position, 2f))
                {
                    if (ride != null && ride.stick > 0f) { ride.stick = 0f; car.SurfaceUp = Vector3.up; }
                    continue;
                }
                if (ride == null) rides[car] = ride = new Ride();
                bool inBarrel = Barrel(car.Body.position, 0f, out var axis, out var along, out float fromEnd, out _);
                // never a trap: overturned (or on its side) and slow anywhere in the pipe or its cuts for 1.5 s, it is put back in the
                // bottom facing along the pipe, as a reset does (Round 24 A2: this piece's own test first; the search of all only then)
                if (Holds(car.Body.position) && Inside(car.Body.position, out _) == this)
                {
                    var refUp = Vector3.up;
                    if (inBarrel) { var rr = car.Body.position - axis; rr -= along * Vector3.Dot(rr, along); if (rr.magnitude > 0.3f) refUp = -rr.normalized; }
                    bool onWall = Vector3.Angle(refUp, Vector3.up) > 35f && car.Body.linearVelocity.magnitude < 1.5f;   // parked up the side
                    bool over = Vector3.Dot(car.transform.up, refUp) < 0.5f || car.GroundedWheels < 2 || onWall;
                    ride.stuck = over && car.Body.linearVelocity.magnitude < 2.5f ? ride.stuck + dt : 0f;
                    if (ride.stuck > 1.5f && Recover(car.Body.position, car.transform.forward, car.Body.linearVelocity, car.Body, out var rp, out float ry))
                    {
                        ride.stuck = 0f; ride.stick = 0f; car.SurfaceUp = Vector3.up; Rescues++;
                        var rot = Quaternion.Euler(0f, ry, 0f);
                        car.Body.position = rp + Vector3.up * 0.5f; car.Body.rotation = rot; car.transform.SetPositionAndRotation(car.Body.position, rot);
                        car.Body.linearVelocity = Vector3.zero; car.Body.angularVelocity = Vector3.zero; car.ClearSteering();
                        continue;
                    }
                    if (!inBarrel && car.GroundedWheels < 2)   // in a cut, on its side: roll it back onto its wheels
                    {
                        var tilt = Vector3.Cross(car.transform.up, Vector3.up);
                        car.Body.AddTorque(tilt * 22f - Vector3.ProjectOnPlane(car.Body.angularVelocity, Vector3.up) * 5f, ForceMode.Acceleration);
                    }
                }
                if (!inBarrel)
                {
                    if (ride.stick > 0f) { ride.stick = 0f; car.SurfaceUp = Vector3.up; }
                    continue;
                }
                var r = car.Body.position - axis; r -= along * Vector3.Dot(r, along);
                var n = r.magnitude > 0.3f ? r.normalized : Vector3.down;   // out from the axis: toward the wall under the vehicle
                float speed = car.Body.linearVelocity.magnitude;
                if (car.GroundedWheels >= 2) ride.lastGround = Time.time;
                float target = Time.time - ride.lastGround < 0.3f ? Smooth01((speed - StickFrom) / (StickFull - StickFrom)) * Smooth01((fromEnd - 0.5f) / EndFade) : 0f;
                ride.stick = Mathf.MoveTowards(ride.stick, target, dt * 4f);
                // slow on a steep part of the wall: slide down it (the tires' grip would otherwise hold a vehicle parked sideways there)
                if (!NoSlide && speed < StickFrom && car.GroundedWheels >= 1 && Vector3.Angle(-n, Vector3.up) > 30f)
                    car.Body.AddForce(Vector3.ProjectOnPlane(Physics.gravity, -n) * (1.6f * (1f - speed / StickFrom)), ForceMode.Acceleration);
                if (ride.stick > 0.001f) car.Body.AddForce((n * 9.81f - Physics.gravity) * ride.stick, ForceMode.Acceleration);
                car.SurfaceUp = ride.stick > 0.001f ? Vector3.Slerp(Vector3.up, -n, ride.stick).normalized : Vector3.up;
                float round = Vector3.Angle(n, Vector3.down);
                if (round > 45f && car.GroundedWheels >= 2) WallSteps++;
                MostRound = Mathf.Max(MostRound, car.GroundedWheels >= 2 ? round : 0f);
                // in the air and not held (off the wall or the roof): turn it upright so it comes down on its wheels; lying on its side
                // or its roof against the pipe (fewer than two wheels down): roll it back onto its wheels, toward the axis
                if (car.GroundedWheels == 0 && ride.stick < 0.05f && r.magnitude < radius - 1.2f)
                {
                    var tilt = Vector3.Cross(car.transform.up, Vector3.up);
                    car.Body.AddTorque(tilt * 14f - Vector3.ProjectOnPlane(car.Body.angularVelocity, Vector3.up) * 4f, ForceMode.Acceleration);
                }
                else if (car.GroundedWheels < 2)
                {
                    var want = -n; var tilt = Vector3.Cross(car.transform.up, want);
                    car.Body.AddTorque(tilt * 22f - Vector3.ProjectOnPlane(car.Body.angularVelocity, want) * 5f, ForceMode.Acceleration);
                    if (speed < 4f && Vector3.Dot(car.transform.up, want) < 0.3f) car.Body.AddForce(-n * 6f, ForceMode.Acceleration);   // lift it off the wall a little so it can roll
                }
            }
        }
        static float Smooth01(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        // the chase camera inside the barrel: on the axis 6 m behind the vehicle (the way it is pointing along the pipe), looking
        // ahead of it, world up; it never leaves the pipe and does not roll or swing when the vehicle is on the wall or the roof
        // Round 24: steadier. The camera follows a SMOOTHED station along the axis (so it always lies on the axis, never cuts a bend
        // through the wall), the direction only flips after the vehicle has gone the other way for half a second, the pipe is kept
        // while the vehicle is still in it, and it looks along the axis, not toward the vehicle (so wall riding does not swing it)
        static DrainPipe viewPipe; static float viewSt, viewStVel, viewSign = 1f, flipTime, viewIdle;
        static bool View(Transform target, out Vector3 desired, out Vector3 lookAt)
        {
            desired = lookAt = default;
            DrainPipe d = null; Vector3 axis = default, along = default; float st = 0f;
            if (viewPipe && viewPipe.Barrel(target.position, 1f, out axis, out along, out _, out st)) d = viewPipe;
            else foreach (var q in all) if (q.Barrel(target.position, 1f, out axis, out along, out _, out st)) { d = q; break; }
            if (d == null) { viewPipe = null; return false; }
            var rb = target.GetComponent<Rigidbody>();
            float want = viewSign;
            if (rb && rb.linearVelocity.magnitude > 3f) want = Vector3.Dot(rb.linearVelocity, along) >= 0f ? 1f : -1f;
            else if (!viewPipe) want = Vector3.Dot(target.forward, along) >= 0f ? 1f : -1f;
            if (d != viewPipe) { viewPipe = d; viewSt = st; viewStVel = 0f; viewSign = want; flipTime = 0f; }
            if (want != viewSign) { flipTime += Time.deltaTime; if (flipTime > 0.5f) { viewSign = want; flipTime = 0f; } } else flipTime = 0f;
            viewSt = Mathf.SmoothDamp(viewSt, st, ref viewStVel, 0.12f);
            var pipe = d;
            Vector3 AxisAt(float s) { s = Mathf.Clamp(s, 0f, pipe.floor.Length - 1.001f); int k = (int)s; return pipe.AxisFrom(Vector3.Lerp(pipe.floor[k], pipe.floor[k + 1], s - k), (pipe.floor[k + 1] - pipe.floor[k]).normalized); }
            desired = AxisAt(viewSt - viewSign * 12f) + Vector3.up * 0.4f;   // 12 stations = 6 m
            lookAt = AxisAt(viewSt + viewSign * 14f);
            return true;
        }

        // the vehicle is in any pipe or its cuts (VehicleLights: its headlight on)
        public static bool InsideAny(Vector3 p) => Inside(p, out _) != null;

        // the station (index into floor, fractional) and lateral offset of p, if p is inside this pipe's space
        bool Locate(Vector3 p, float lateralPad, out float station, out Vector3 at, out Vector3 along)
        {
            station = 0f; at = default; along = Vector3.forward;
            if (floor == null || floor.Length < 2) return false;
            if (!Near(p, 1f + lateralPad)) return false;
            float best = float.MaxValue; var q = new Vector2(p.x, p.z);
            for (int i = 0; i + 1 < floor.Length; i++)
            {
                var a = new Vector2(floor[i].x, floor[i].z); var ab = new Vector2(floor[i + 1].x, floor[i + 1].z) - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                float d = (a + ab * t - q).sqrMagnitude;
                if (d < best) { best = d; station = i + t; }
            }
            if (Mathf.Sqrt(best) > halfWidth + lateralPad) return false;
            int k = Mathf.Clamp((int)station, 0, floor.Length - 2);
            at = Vector3.Lerp(floor[k], floor[k + 1], station - k);
            along = floor[k + 1] - floor[k]; along.y = 0f; along.Normalize();
            return station > 0.01f && station < floor.Length - 1.01f;
        }

        bool Holds(Vector3 p) => Locate(p, 0.6f, out _, out var at, out _) && p.y > at.y - 1.5f && p.y < at.y + clearHeight + 0.5f;

        public static DrainPipe Inside(Vector3 p, out float station)
        {
            station = 0f;
            foreach (var d in all)
                if (d.Locate(p, 0.6f, out station, out var at, out _) && p.y > at.y - 1.5f && p.y < at.y + d.clearHeight + 0.5f) return d;
            return null;
        }

        // a race line's height at (x, z) given the ground's: the floor's where the point is in a pipe or its cuts, and the
        // ground there stands above the floor (the pipe runs under it)
        public static float LineHeight(float x, float z, float groundY)
        {
            foreach (var d in all)
                if (d.Locate(new Vector3(x, groundY, z), 0.5f, out _, out var at, out _) && groundY > at.y + 0.3f) return at.y;   // Round 23 follow-ups: at the ground's height (Near tests in 3D)
            return groundY;
        }

        public static bool Recover(Vector3 p, Vector3 heading, Vector3 raceForward, Rigidbody own, out Vector3 pos, out float yaw)
        {
            pos = default; yaw = 0f;
            var pipe = Inside(p, out float s);
            if (pipe == null) return false;
            pipe.Locate(p, 1f, out s, out _, out var along);
            var fwd = raceForward.sqrMagnitude > 0.01f ? raceForward : heading;
            float sign = Vector3.Dot(along, fwd) >= 0f ? 1f : -1f;
            foreach (float back in new[] { 0f, -4f, 4f, -8f, 8f, -12f })
            {
                float st = Mathf.Clamp(s - sign * back * 2f, 2f, pipe.floor.Length - 3f);   // floor points are 0.5 m apart
                int k = (int)st; var at = Vector3.Lerp(pipe.floor[k], pipe.floor[k + 1], st - k);
                var dir = (pipe.floor[k + 1] - pipe.floor[k]) * sign; dir.y = 0f; dir.Normalize();
                var rot = Quaternion.LookRotation(dir, Vector3.up);
                int n = Physics.OverlapBoxNonAlloc(at + Vector3.up * 1.0f, new Vector3(1.1f, 0.6f, 2.4f), hits, rot, ~0, QueryTriggerInteraction.Ignore);
                bool clear = true;
                for (int i = 0; i < n; i++) if (hits[i].attachedRigidbody != null && hits[i].attachedRigidbody != own) { clear = false; break; }   // the pipe itself is fine to stand in
                if (!clear) continue;
                pos = at; yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                return true;
            }
            return false;
        }
    }
}
