using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 30 (A1, A2): one traffic car. Racer's RoadDriver drives its traffic (racing = false) round a single road loop; here the
    // same driving runs over the map's road graph (TrafficNetwork) instead: Racer's steering (pure pursuit to a point ahead on
    // the lane, Atan(2 wheelbase sin(angle) / look)), its traffic speed rules (the corner grip 7.5, the judgment 8 m/s2, speed
    // from the bends ahead), its obstacle query (a sphere cast ahead; a vehicle ahead sets the speed), its stall reverse and the
    // offscreen recycle (Traffic.cs). New here, for a road network: the lane on the right of each road (one-way roads: right of
    // middle), the next road picked at each junction, a stop or a yield at a junction onto a bigger road, a turn round at a dead
    // end, the road type's speed (TrafficNetwork.Speed), and the arcade reactions: the horn and a moment knocked after the
    // player hits it, a swerve to the curb from a vehicle coming at it in its lane, the horn at a vehicle in its way.
    public sealed class TrafficDriver : MonoBehaviour
    {
        public ArcadeVehicle Car { get; private set; }
        public float LastThrottle { get; private set; }   // OtherEngines
        public int Edge => route.Count > 0 ? route[0].edge : -1;
        public bool WantsRecycle { get; private set; }     // stuck, off the road or at the map's edge: Traffic moves it when nobody sees
        public float Pace { get; private set; } = 1;
        public string State { get; private set; } = "";   // checks
        public float Jammed { get; private set; }          // Round 32 (B2): how long it has stood blocked by a car across its way

        Traffic owner; TrafficNetwork net;
        readonly List<(int edge, int dir)> route = new();
        readonly List<Vector2> path = new(); readonly List<float> cum = new(); readonly List<float> nodeS = new();
        readonly List<Vector2> scratch = new();
        int projIndex; float stalled, knockedUntil, swerveUntil, nextHorn, blockedFor, stopHeld, upsideDown, goUntil;
        int stopNode = -1, passedNode = -1;
        AudioSource horn;
        readonly RaycastHit[] hits = new RaycastHit[16];
        const float CornerGrip = 7.5f, Judgment = 8f;   // Racer's traffic values

        public void Initialize(Traffic t, ArcadeVehicle car)
        {
            owner = t; net = TrafficNetwork.Instance; Car = car;
            horn = gameObject.AddComponent<AudioSource>(); horn.playOnAwake = false; horn.spatialBlend = 1; horn.minDistance = 10; horn.maxDistance = 160;
            horn.rolloffMode = AudioRolloffMode.Logarithmic; horn.dopplerLevel = 0; horn.priority = 120; horn.clip = Traffic.HornClip;
        }

        // put on edge e at distance s from its start (dir +1: from -> to), facing the way it drives
        public void Place(int e, int dir, float s, Vector3 position, float speed)
        {
            route.Clear(); route.Add((e, dir)); stopNode = passedNode = -1; WantsRecycle = false; stalled = blockedFor = stopHeld = upsideDown = 0; knockedUntil = swerveUntil = 0;
            Pace = Random.Range(.9f, 1.06f);
            Extend(); Rebuild();
            projIndex = 0; float at = Project(new Vector2(position.x, position.z));
            var ahead = PathAt(at + 6) - PathAt(at); var fwd = new Vector3(ahead.x, 0, ahead.y).normalized; if (fwd.sqrMagnitude < .5f) fwd = Vector3.forward;
            var rot = Quaternion.LookRotation(fwd, Vector3.up);
            Car.Body.position = position; Car.Body.rotation = rot; transform.SetPositionAndRotation(position, rot);
            Car.Body.linearVelocity = fwd * speed; Car.Body.angularVelocity = Vector3.zero; Car.ClearSteering();
        }

        void Extend()
        {
            while (route.Count < 3)
            {
                var (e, dir) = route[route.Count - 1];
                int node = dir > 0 ? net.To(e) : net.From(e);
                int next = owner.PickNext(e, node, out int ndir, out bool exit);
                if (exit || next < 0) break;
                route.Add((next, ndir));
            }
        }

        void Rebuild()
        {
            path.Clear(); cum.Clear(); nodeS.Clear();
            for (int k = 0; k < route.Count; k++)
            {
                var (e, dir) = route[k];
                scratch.Clear(); net.Points(e, dir, scratch);
                float off = Lane(e);
                for (int i = 0; i < scratch.Count; i++)
                {
                    var a = scratch[Mathf.Max(0, i - 1)]; var b = scratch[Mathf.Min(scratch.Count - 1, i + 1)];
                    var t = (b - a).normalized; var right = new Vector2(t.y, -t.x);
                    var p = scratch[i] + right * off;
                    if (k > 0 && i == 0) { path[path.Count - 1] = (path[path.Count - 1] + p) * .5f; continue; }   // the junction point: between the two lanes
                    path.Add(p);
                }
                nodeS.Add(0);
            }
            cum.Add(0); for (int i = 1; i < path.Count; i++) cum.Add(cum[i - 1] + (path[i] - path[i - 1]).magnitude);
            // the distance along the path at the end of each route edge
            float acc = 0; int idx = 0;
            for (int k = 0; k < route.Count; k++)
            {
                int n = net.PointCount(route[k].edge) - (k > 0 ? 1 : 0); idx += n; acc = cum[Mathf.Clamp(idx - 1, 0, cum.Count - 1)]; nodeS[k] = acc;
            }
            projIndex = 0;
        }
        float Lane(int e)
        {
            float half = net.Half(e);
            if (net.Oneway(e)) return half >= 4.5f ? (Pace > .99f ? half * .2f : half * .45f) : 0f;   // a wide one-way road: the right lane, a few in the left
            return Mathf.Clamp(half * .5f, 1.3f, 2.6f);
        }

        float Project(Vector2 p)
        {
            float best = float.MaxValue, s = 0; int bi = projIndex;
            int from = Mathf.Max(1, projIndex - 3), to = Mathf.Min(path.Count - 1, projIndex + 12);
            if (projIndex == 0) { from = 1; to = path.Count - 1; }
            for (int i = from; i <= to; i++)
            {
                var a = path[i - 1]; var ab = path[i] - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                float d = (a + ab * t - p).sqrMagnitude;
                if (d < best) { best = d; s = cum[i - 1] + t * Mathf.Sqrt(ab.sqrMagnitude); bi = i; }
            }
            projIndex = bi; Lateral = Mathf.Sqrt(best); return s;
        }
        float Lateral;
        Vector2 PathAt(float s)
        {
            if (path.Count == 1) return path[0];
            s = Mathf.Clamp(s, 0, cum[cum.Count - 1]);
            int i = 1; while (i < cum.Count - 1 && cum[i] < s) i++;
            float L = cum[i] - cum[i - 1]; return Vector2.Lerp(path[i - 1], path[i], L > 1e-4f ? (s - cum[i - 1]) / L : 0);
        }

        void FixedUpdate()
        {
            if (!owner || route.Count == 0 || Time.timeScale == 0) return;
            var body = Car.Body; var p2 = new Vector2(body.position.x, body.position.z);
            float s = Project(p2);
            // past the end of the first edge: on to the next
            if (route.Count > 1 && s > nodeS[0] + 1)
            {
                int node = route[0].dir > 0 ? net.To(route[0].edge) : net.From(route[0].edge); passedNode = node; stopNode = -1; stopHeld = 0;
                route.RemoveAt(0); Extend(); Rebuild(); s = Project(p2);
            }
            if (owner.Forbidden(route[0].edge)) WantsRecycle = true;
            float speed = Car.ForwardSpeed, end = cum[cum.Count - 1];

            float look = Mathf.Clamp(speed * .55f, 6, 18);
            var target2 = PathAt(s + look);
            if (Time.time < swerveUntil) { var d2 = PathAt(s + look + 1) - target2; target2 += new Vector2(d2.y, -d2.x).normalized * 1.6f; }
            var local = transform.InverseTransformPoint(new Vector3(target2.x, body.position.y, target2.y));
            float angle = Mathf.Atan2(local.x, local.z);
            float maxAngle = Mathf.Lerp(Car.slowSteerAngle, Car.fastSteerAngle, Mathf.Clamp01(speed / Car.topSpeed)) * Mathf.Deg2Rad;
            float steering = Mathf.Clamp(Mathf.Atan(2 * Car.wheelbase * Mathf.Sin(angle) / look) / maxAngle, -1, 1);

            // the road's speed, the bends ahead (Racer's traffic: corner grip 7.5, judgment 8), the next road's speed
            float target = net.Speed(route[0].edge) * Pace;
            for (float d = 0; d <= 80 && s + d + 6 < end; d += 6)
            {
                var a = PathAt(s + d); var b = PathAt(s + d + 6); var c = PathAt(s + d + 12);
                float curvature = Vector2.Angle(b - a, c - b) * Mathf.Deg2Rad / 6;
                float curve = Mathf.Sqrt(CornerGrip / Mathf.Max(.0015f, curvature));
                target = Mathf.Min(target, Mathf.Sqrt(curve * curve + 2 * Judgment * Mathf.Max(0, d - 6)));
            }
            if (route.Count > 1) { float toNode = nodeS[0] - s; float v = net.Speed(route[1].edge) * Pace; target = Mathf.Min(target, Mathf.Sqrt(v * v + 2 * Judgment * Mathf.Max(0, toNode))); }
            // the end of what is planned: a dead end (turn round slowly), the map's edge or a closed road (stop there)
            if (route.Count == 1) target = Mathf.Min(target, Mathf.Sqrt(2 * Judgment * Mathf.Max(0, end - s - 4)));
            if (route.Count == 1 && end - s < 6) WantsRecycle = true;
            State = "drive";

            // a junction onto a bigger road: stop, then go when it is clear; onto an equal one: give way to a car already at it
            if (route.Count > 1)
            {
                int node = route[0].dir > 0 ? net.To(route[0].edge) : net.From(route[0].edge);
                float toNode = nodeS[0] - s;
                if (node != passedNode && toNode < 40 && net.Degree(node) >= 3)
                {
                    bool mustStop = owner.MinorAt(route[0].edge, node);
                    bool clear = owner.JunctionClear(node, this);
                    if (mustStop && stopNode != node)
                    {
                        target = Mathf.Min(target, Mathf.Sqrt(2 * Judgment * Mathf.Max(0, toNode - 7)));
                        if (toNode < 10 && speed < 1f) { stopHeld += Time.fixedDeltaTime; State = "stop"; if (stopHeld > 1f && (clear || stopHeld > 9f)) stopNode = node; }
                    }
                    else if (!clear && toNode > 5 && toNode < 22 && stopNode != node) { target = Mathf.Min(target, Mathf.Sqrt(2 * Judgment * Mathf.Max(0, toNode - 7))); State = "yield"; stopHeld += Time.fixedDeltaTime; if (stopHeld > 6f) stopNode = node; }
                }
            }
            if (Mathf.Abs(angle) > .7f) target = Mathf.Min(target, 5f);

            // Racer's obstacle query: what is ahead sets the speed. Vehicles only (the roads are clear of the static world, and its
            // fences, signs and mailboxes break); a vehicle coming at it in its lane: swerve to the curb and sound the horn.
            float range = 6 + Mathf.Max(0, speed) * 1.5f;
            int count = Physics.SphereCastNonAlloc(body.position + Vector3.up * .35f, .8f, transform.forward, hits, range, ~0, QueryTriggerInteraction.Ignore);
            // Round 32 (B2, BUG-010: four cars stood in a knot in a junction, "Is this a car accident?"): cars crossing each other's
            // way in a junction each saw the other in its sphere cast and waited for it, for ever (the stall reverse only runs when
            // nothing ahead holds the car back). A car held 3 s by one standing across its way takes its turn when it is the
            // first of the knot (Traffic.TakesTurn): for 4 s it drives on past cars across its way (never the player, never the
            // car in front of it in its lane); a knot that still does not clear in 25 s is moved off when nobody sees it.
            bool blocked = false, across = false, going = Time.time < goUntil;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (!hit.rigidbody || hit.rigidbody == body || hit.collider is TerrainCollider) continue;
                if (!hit.rigidbody.GetComponent<ArcadeVehicle>()) continue;
                bool crossing = hit.rigidbody != owner.PlayerBody && Vector3.Angle(Flat(hit.rigidbody.transform.forward), Flat(transform.forward)) > 35f;
                if (going && crossing) continue;
                float aheadSpeed = Mathf.Max(0, Vector3.Dot(hit.rigidbody.linearVelocity, transform.forward));
                float allowed = Mathf.Sqrt(Mathf.Max(0, aheadSpeed * aheadSpeed + 2 * Judgment * (hit.distance - 4.5f)));
                target = Mathf.Min(target, allowed); blocked |= allowed < 2; across |= crossing && allowed < 2;
                bool player = hit.rigidbody == owner.PlayerBody;
                float closing = -Vector3.Dot(hit.rigidbody.linearVelocity, transform.forward);
                if (player && closing > 6 && hit.distance < 30) { swerveUntil = Time.time + 1.5f; Horn(); }
            }
            blockedFor = blocked && speed < 1 ? blockedFor + Time.fixedDeltaTime : 0;
            Jammed = across && speed < 1 ? Jammed + Time.fixedDeltaTime : going ? Jammed : 0;
            if (!going && Jammed > 3f && owner.TakesTurn(this)) { goUntil = Time.time + 4f; Traffic.KnotsUntied++; }
            if (Jammed > 25f) WantsRecycle = true;
            if (blockedFor > 3f) { Horn(); blockedFor = -4f; }

            float throttle = speed < target ? Mathf.Clamp01((target - speed) * .5f) : 0;
            float brake = speed > target + .5f ? Mathf.Clamp01((speed - target) * .25f) : 0;
            if (target < 1 && speed < .7f) { throttle = brake = 0; body.linearVelocity *= .85f; }
            if (Time.time < knockedUntil) { throttle = 0; brake = .3f; steering = 0; State = "knocked"; }

            // Racer's stall: stuck with somewhere to go, back off with the wheel the other way; long stuck, upside down, under
            // the ground or far off the road: moved when nobody sees it
            stalled = speed < 1.5f && target > 3 ? stalled + Time.fixedDeltaTime : 0;
            if (stalled > 5 && stalled < 8) { throttle = 0; brake = .45f; steering = -steering; State = "reverse"; }
            upsideDown = transform.up.y < .3f ? upsideDown + Time.fixedDeltaTime : 0;
            if (stalled > 14 || upsideDown > 4 || Lateral > 25) WantsRecycle = true;

            LastThrottle = throttle;
            Car.Simulate(throttle, brake, steering, Time.fixedDeltaTime);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void OnCollisionEnter(Collision c)
        {
            if (c.rigidbody && c.rigidbody == owner.PlayerBody && c.relativeVelocity.magnitude > 3f) { knockedUntil = Time.time + 1.2f; Horn(); }
        }
        public void Horn()
        {
            if (Time.time < nextHorn || !horn || !horn.clip) return;
            nextHorn = Time.time + 3f + Random.value * 2f;
            horn.volume = .8f * GameSettings.Settings.vehicle; horn.pitch = Random.Range(.92f, 1.1f); horn.Play();
            Traffic.Horns++;
        }
    }
}
