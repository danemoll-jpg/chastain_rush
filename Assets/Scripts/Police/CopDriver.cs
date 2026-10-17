using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): Racer's CopDriver as it is, on this map's RoadNet; it runs while its chase does (no RaceDirector / RaceFlow).
    // 0.96 Part E: the AI cop's driving. It supplies pedals and steering to the patrol car's own motor (the same as RoadDriver and
    // the player) along a path of RoadNet nodes: pure pursuit on the path with a speed that respects the bends and the traffic
    // ahead; near the runner on the road it aims at the runner instead, to get alongside and box it in; blocked by traffic it
    // goes round it, or pushes it aside when chasing. It never leaves the path: far off it, or flipped, or stuck, it is set back on
    // the road. With no path it stops. The director (GetawayChase) chooses the paths and the roles.
    public sealed class CopDriver : MonoBehaviour
    {
        public Vector3 PrevVel; public ArcadeVehicle Car; public RoadNet Net; public GetawayChase Boss; public PoliceLights Lights;
        public float SpeedFactor = 1, Catchup = 1, BaseTop = 0, Boost = 1; // 0.97: Car.topSpeed = BaseTop (the runner's vehicle's) x Boost (catch-up, up to 1.3)
        public enum Task { Idle, Chase, Flank, Cutoff, Search, Edge, Exit, Block }
        public Task Role = Task.Idle; public string Label => Role.ToString();
        public bool Hold;                 // stop at the end of the path and wait there
        public bool Frozen;               // held in place (before GO, a roadblock)
        public Transform Chasing; public Rigidbody ChasingBody; public float Offset; public bool Pushy;
        public float LastThrottle { get; private set; } // 0.113 Part E: for its engine sound (OtherEngines)
        public static readonly List<CopDriver> All = new();   // Chastain Rush (Round 39): the cops' engines for OtherEngines
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);
        public bool Arrived { get; private set; }
        public int Recoveries { get; private set; }
        public float TargetSpeed { get; private set; } public string Limit { get; private set; } = ""; // what holds the speed down: corner / angle / lateral / obstacle / end (debug)
        public Vector3 GoalPoint => path.Count > 0 ? path[path.Count - 1] : Car ? Car.Body.position : Vector3.zero;
        public int GoalNode { get; private set; } = -1; public int GoFails;
        public bool HasPath => path.Count > 1;
        readonly List<Vector3> path = new(); int seg; float stalled, reverseUntil, flipped, laneOffset, laneUntil, offPath, lastTeleport = -99, lastAdvance;
        readonly RaycastHit[] hits = new RaycastHit[16];

        public static int StartNode(RoadNet net, Vector3 position, Vector3 forward)
        {
            int best = -1; float bs = float.MaxValue;
            foreach (int i in net.Near(position, 45))
            {
                var d = net.P[i] - position; d.y = 0; if (Mathf.Abs(net.P[i].y - position.y) > 9) continue;
                float score = d.magnitude + (Vector3.Dot(d, forward) < 0 ? 22 : 0); if (score < bs) { bs = score; best = i; }
            }
            return best >= 0 ? best : net.Nearest(position, out _, 400);
        }
        // follow these nodes (from where the cop is); false when there is no way
        public bool Go(List<int> nodes)
        {
            if (nodes == null || nodes.Count == 0) return false;
            path.Clear(); foreach (int n in nodes) path.Add(Net.P[n]); GoalNode = nodes[nodes.Count - 1];
            if (path.Count == 1) path.Add(path[0] + Vector3.forward * .1f);
            seg = ClosestSegment(); Arrived = false; return true;
        }
        public void Stop() { path.Clear(); GoalNode = -1; Arrived = true; }
        public bool GoTo(int node)
        {
            int from = StartNode(Net, Car.Body.position, Car.transform.forward); if (from < 0 || node < 0) return false;
            if (node == GoalNode && HasPath && !Arrived) return true;
            var found = Net.Path(from, node); if (found == null) GoFails++; return Go(found);
        }
        int ClosestSegment()
        {
            int best = 0; float bd = float.MaxValue; var p = Car.Body.position;
            for (int i = 0; i + 1 < path.Count && i < 40; i++) { float d = DistToSegment(p, path[i], path[i + 1]); if (d < bd) { bd = d; best = i; } }
            return best;
        }
        static float DistToSegment(Vector3 p, Vector3 a, Vector3 b) { var ab = b - a; ab.y = 0; var ap = p - a; ap.y = 0; float t = ab.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude); return (ap - ab * t).magnitude; }
        Vector3 PointAlong(int from, float metres, out Vector3 tangent)
        {
            tangent = Car.transform.forward; if (path.Count < 2) return Car.Body.position;
            var p = Car.Body.position; int i = Mathf.Clamp(from, 0, path.Count - 2);
            // start from the projection of the car on the current segment
            var a = path[i]; var b = path[i + 1]; var ab = b - a; ab.y = 0; float t = ab.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            var cur = Vector3.Lerp(a, b, t); float left = metres;
            while (true)
            {
                var end = path[i + 1]; float segLen = Vector3.Distance(cur, end); tangent = (end - cur).sqrMagnitude > .0001f ? (end - cur).normalized : tangent;
                if (segLen >= left) return cur + (end - cur).normalized * left;
                left -= segLen; cur = end; i++; if (i + 1 >= path.Count) return cur;
            }
        }

        void FixedUpdate()
        {
            if (Car && Car.Body) PrevVel = Car.Body.linearVelocity;
            if (!Car) return;
            Car.enabled = false; if (BaseTop > 0) Car.topSpeed = BaseTop * Mathf.Clamp(Boost, 1f, 1.3f);
            if (Frozen || (Boss && Boss.State != GetawayChase.Phase.Running)) { Car.Body.isKinematic = true; return; }
            Car.Body.isKinematic = false;
            float dt = Time.fixedDeltaTime; var p = Car.Body.position; float speed = Mathf.Abs(Car.ForwardSpeed);
            // upside down or on its side for 2 s: put back on the road
            flipped = Car.transform.up.y < .3f ? flipped + dt : 0; if (flipped > 2f) { Recover(); return; }
            if (path.Count < 2 && Chasing) { path.Clear(); path.Add(p); path.Add(p + Car.transform.forward * 10); seg = 0; } // chasing with no road path left: aim at the runner
            if (path.Count < 2) { Brake(dt, speed); return; }
            // advance along the path
            while (seg + 1 < path.Count - 1)
            {
                var a = path[seg]; var b = path[seg + 1]; var ab = b - a; ab.y = 0; var ap = p - a; ap.y = 0;
                if (Vector3.Dot(ap, ab) > ab.sqrMagnitude) seg++; else break;
            }
            float lateral = DistToSegment(p, path[seg], path[seg + 1]);
            offPath = lateral > 22 ? offPath + dt : 0;
            if (offPath > 3f && Time.time - lastTeleport > 4f) { Recover(); return; }
            var end = path[path.Count - 1]; var toEnd = end - p; toEnd.y = 0; float remaining = toEnd.magnitude;
            if (!Arrived && !(Chasing && !Hold) && seg + 1 >= path.Count - 1 && remaining < (Hold ? 7f : 14f)) { Arrived = true; if (!Hold) { Stop(); Brake(dt, speed); return; } }
            if (Arrived && Hold) { Brake(dt, speed); return; }

            // where to aim
            float look = Mathf.Clamp(7 + speed * .5f, 9, 30);
            var target = PointAlong(seg, look, out var tangent); var right = Vector3.Cross(Vector3.up, tangent).normalized;
            // traffic ahead: go round it
            UpdateLane(speed, tangent, right);
            target += right * laneOffset;
            bool direct = false; Vector3 runnerVelocity = Vector3.zero; float chaseDistance = 999;
            if (Chasing)
            {
                runnerVelocity = ChasingBody ? ChasingBody.linearVelocity : Vector3.zero;
                var to = Chasing.position - p; to.y = 0; chaseDistance = to.magnitude;
                bool ahead = Vector3.Dot(Car.transform.forward, to) > -2f || chaseDistance < 12;
                if (chaseDistance < 60 && ahead && Net.OffNet(Chasing.position) < 12)
                {
                    var rrel = Vector3.Cross(Vector3.up, ChasingBody && runnerVelocity.sqrMagnitude > 4 ? runnerVelocity.normalized : Chasing.forward).normalized;
                    float off = Offset * Mathf.Clamp01((chaseDistance - 5f) / 18f);
                    target = Chasing.position + runnerVelocity * Mathf.Clamp(chaseDistance / Mathf.Max(8, speed + 4), .1f, .6f) + rrel * off; direct = true;
                }
            }
            var local = Car.transform.InverseTransformPoint(target);
            float angle = Mathf.Atan2(local.x, local.z);
            float maxAngle = Mathf.Lerp(Car.slowSteerAngle, Car.fastSteerAngle, Mathf.Clamp01(speed / Car.topSpeed)) * Mathf.Deg2Rad;
            float lookUsed = direct ? Mathf.Clamp(Vector3.Distance(p, target), 6, 30) : look;
            float steering = Mathf.Clamp(Mathf.Atan(2 * Car.wheelbase * Mathf.Sin(angle) / lookUsed) / maxAngle, -1, 1);
            if (Mathf.Abs(angle) > Mathf.PI * .5f) steering = Mathf.Sign(angle);

            // the speed: top (pursuit) speed, held down by the bends ahead and the end of the path
            float vmax = Car.topSpeed * SpeedFactor * Catchup; float cornerGrip = Mathf.Min(Car.maxGripAcceleration * .62f, 14f) * Car.GripScale, decel = Mathf.Max(6f, Car.braking * .75f) * Car.GripScale; // GripScale: the weather's grip (0.97)
            float v = vmax; string why = "top"; int v0 = Mathf.Min(seg + 1, path.Count - 1); float dist = Vector3.Distance(p, path[v0]);
            // 0.97: the bend ahead is measured over two nodes each side of a vertex, not between neighbours: where the network hops between
            // two parallel roads the path zig-zags a few metres, which neighbour-to-neighbour read as a hairpin (cops crawled at 11 m/s)
            for (int k = v0; k < path.Count - 1 && dist < 130; k++)
            {
                if (k > v0) { var step = path[k] - path[k - 1]; step.y = 0; dist += step.magnitude; }
                int i0 = Mathf.Max(0, k - 2), i1 = Mathf.Min(path.Count - 1, k + 2);
                var a = path[k] - path[i0]; var b = path[i1] - path[k]; a.y = 0; b.y = 0; if (a.sqrMagnitude < .01f || b.sqrMagnitude < .01f) continue;
                float curvature = Vector3.Angle(a, b) * Mathf.Deg2Rad / Mathf.Max(8f, (a.magnitude + b.magnitude) * .5f);
                float curveSpeed = Mathf.Sqrt(cornerGrip / Mathf.Max(.00025f, curvature));
                float cap = Mathf.Sqrt(curveSpeed * curveSpeed + 2 * decel * Mathf.Max(0, dist - 10)); if (cap < v) { v = cap; why = "corner"; }
            }
            if (Hold) { float cap = Mathf.Sqrt(2 * decel * .6f * Mathf.Max(0, remaining - 5)); if (cap < v) { v = cap; why = "end"; } }
            else if (path.Count > 1 && seg + 2 >= path.Count) { float cap = Mathf.Max(8, Mathf.Sqrt(2 * decel * .6f * Mathf.Max(0, remaining - 5))); if (cap < v) { v = cap; why = "end"; } }
            // a sharp swing to the aim point slows a cop following the path; one going straight at the runner (direct) steers and keeps its speed:
            // 0.97: it used to be capped at 11 m/s whenever the runner was off to the side, which let a fast runner leave every cop alongside it
            if (!direct && Mathf.Abs(angle) > .9f && v > 14) { v = 14; why = "angle"; }
            if (!direct && lateral > 6 && v > 16) { v = 16; why = "lateral"; }
            // the obstacle ahead (traffic, the odd solid thing): slow to its speed; chasing, push traffic aside rather than stop
            int count = Physics.SphereCastNonAlloc(p + Vector3.up * .45f, .75f, Car.transform.forward, hits, 7 + speed * 1.3f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i]; if (hit.rigidbody == Car.Body || hit.normal.y > .55f || hit.distance < .01f) continue;
                if (Chasing && hit.collider.transform.IsChildOf(Chasing.root)) continue;
                float aheadSpeed = hit.rigidbody ? Mathf.Max(0, Vector3.Dot(hit.rigidbody.linearVelocity, Car.transform.forward)) : 0;
                float allowed = Mathf.Sqrt(Mathf.Max(0, aheadSpeed * aheadSpeed + 2 * decel * (hit.distance - 4)));
                bool car = hit.rigidbody && hit.rigidbody.GetComponent<ArcadeVehicle>();
                if (Pushy && car && !Boss.IsCop(hit.rigidbody)) allowed = Mathf.Max(allowed, 9);
                if (allowed < v) { v = allowed; why = "obstacle"; }
            }
            TargetSpeed = v; Limit = why;
            float throttle = speed < v ? Mathf.Clamp01((v - speed) * .6f) : 0;
            float brake = speed > v + .5f ? Mathf.Clamp01((speed - v) * .25f) : 0;
            // stuck: nearly stopped while it should be going: back up, then recover
            if (speed < 1.2f && v > 3f) stalled += dt; else if (speed > 3f) stalled = 0;
            if (stalled > 2.5f && stalled < 4.2f) { throttle = 0; brake = .8f; steering = -steering; }
            if (stalled >= 4.2f && stalled < 4.4f) { stalled = 4.4f; }
            if (stalled > 7f && Time.time - lastTeleport > 4f) { Recover(); return; }
            LastThrottle = throttle; Car.Simulate(throttle, brake, steering, dt);
        }
        void Brake(float dt, float speed) { LastThrottle = 0; Car.Simulate(0, speed > .6f ? 1f : 0f, 0, dt); if (speed < .7f) Car.Body.linearVelocity *= .85f; }
        void UpdateLane(float speed, Vector3 tangent, Vector3 right)
        {
            if (Time.time < laneUntil) return;
            float want = Offset != 0 && Chasing ? 0 : 0;
            var p = Car.Body.position + Vector3.up * .45f; float range = 12 + speed * 1.2f;
            if (Physics.SphereCast(p, .8f, Car.transform.forward, out var hit, range, ~0, QueryTriggerInteraction.Ignore) && hit.rigidbody && hit.rigidbody != Car.Body && hit.rigidbody.GetComponent<ArcadeVehicle>() && !(Chasing && hit.collider.transform.IsChildOf(Chasing.root)))
            {
                foreach (float option in new[] { laneOffset >= 0 ? 3.2f : -3.2f, laneOffset >= 0 ? -3.2f : 3.2f })
                    if (!Physics.SphereCast(p + right * option, .8f, Car.transform.forward, out var side, range, ~0, QueryTriggerInteraction.Ignore) || !side.rigidbody) { want = option; break; }
                laneUntil = Time.time + 1.8f;
            }
            laneOffset = Mathf.MoveTowards(laneOffset, want, 5f * Time.fixedDeltaTime * 3);
            if (want == 0 && Mathf.Abs(laneOffset) > 0.05f) laneUntil = Time.time + .1f;
        }
        // back on the road, on the path, facing along it, stopped
        public void Recover()
        {
            lastTeleport = Time.time; Recoveries++; stalled = 0; flipped = 0; offPath = 0;
            int node = path.Count > 0 && seg < path.Count ? Net.Nearest(path[Mathf.Min(seg + 1, path.Count - 1)], out _, 120) : Net.Nearest(Car.Body.position, out _, 400);
            if (node < 0) return;
            var at = Net.P[node]; var look = path.Count > 1 ? path[Mathf.Min(seg + 2, path.Count - 1)] : at + Net.Tangent(node) * 10; var f = look - at; f.y = 0; if (f.sqrMagnitude < .01f) f = Net.Tangent(node);
            Place(at, f.normalized);
        }
        public void Place(Vector3 at, Vector3 forward)
        {
            var rot = Quaternion.LookRotation(forward); var pos = at + Vector3.up * Mathf.Max(.5f, Car.suspensionLength - Physics.gravity.magnitude / Car.springStrength + .3f);
            Car.Body.position = pos; Car.Body.rotation = rot; Car.transform.SetPositionAndRotation(pos, rot); Car.Body.linearVelocity = Car.Body.angularVelocity = Vector3.zero; Car.ClearSteering();
            if (path.Count > 1) seg = ClosestSegment();
        }
        void OnCollisionEnter(Collision c) { if (Boss) Boss.Contact(this, c); }
    }
}
