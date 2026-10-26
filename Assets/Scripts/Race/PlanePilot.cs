using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 71 (TODO.md Round 71 D: "the AI flies the ring line with skill by difficulty, Hard close to a good player, no cheating through walls, recovers
    // from crashes as the cars' AI"): an AI pilot in the air race. It flies its plane with the plane's own controls (PlaneFlight.Scripted: the stick's
    // pitch and bank, the rudder, the throttle), never moving the body itself, so it flies exactly what a player can.
    //   On the runway: full throttle in its lane, past the take-off speed it pulls back.
    //   In the air: it chases a point on the rings' line (RaceRoute.Path) a little ahead (about 1.3 s; less where the line is low: under the bridges,
    //   in the quarry), banks toward it (a coordinated turn, the rudder helping in a tight one) and pitches toward it. Its throttle is its pace
    //   (Hard full, Normal 94 %, Easy 88 %, each pilot a little apart), eased off only where the line ahead turns tighter than it can follow at speed.
    //   Mistakes (TODO.md's standing rule "The AI is not perfect"): now and then it lines up a ring a few meters off and misses it (5 s), more often on
    //   Easy; never at a low ring (under a bridge, over the lake) where a miss would put it into the ground.
    //   A crash (a wing into a tree, the ground): the plane's own crash, then the race puts it back in the air before its last ring (RaceController.AirReset).
    [DefaultExecutionOrder(40)]
    public sealed class PlanePilot : MonoBehaviour
    {
        RaceController race; ArcadeVehicle car; RaceEntrant entrant; PlaneFlight plane;
        int index; float lane; Vector3[] path; float[] cum; float L; int near = -1;
        System.Random rng; int decidedFor = -1; float missSide;
        public float Pace { get; private set; }
        public float LastThrottle => plane ? plane.Throttle : 0f;
        public string Why { get; private set; } = "";
        public static readonly float[] Paces = { .88f, .94f, 1f };
        public static readonly float[] MissChance = { .10f, .06f, .035f };
        public static int ForceMiss = -1;   // checks: the player's pilot misses this ring (its Id) on purpose

        public void Initialize(RaceController r, ArcadeVehicle c, RaceEntrant e, int n, float laneOffset)
        {
            race = r; car = c; entrant = e; index = n; lane = laneOffset; plane = c.GetComponent<PlaneFlight>();
            int skill = Mathf.Clamp(RoadDriver.Difficulty, 0, 2);
            Pace = Mathf.Min(1f, Paces[skill] - .015f * n + (skill == 2 ? .015f * 2 : 0f));   // Hard: the leader at full, the others a touch under
            path = r.Route.Path; cum = new float[path.Length];
            for (int i = 1; i < path.Length; i++) { var d = path[i] - path[i - 1]; d.y = 0f; cum[i] = cum[i - 1] + d.magnitude; }
            L = cum[cum.Length - 1];
            rng = new System.Random(unchecked(DriverVariation.Seed + n * 7919));
            if (plane) { plane.Scripted = true; plane.sThrottle = 0f; plane.sPitch = plane.sBank = plane.sRudder = plane.sSteer = plane.sBrake = 0f; }
        }

        void FixedUpdate()
        {
            if (!race || !plane || !plane.enabled || race.Route == null) return;
            if (race.State == RaceController.Stage.Countdown || race.State == RaceController.Stage.Free) { plane.sThrottle = 0f; return; }
            if (car.Body.isKinematic) { car.Body.isKinematic = false; car.Body.linearVelocity = Vector3.zero; }   // GO
            if (plane.Crashed) return;
            var pos = car.Body.position;
            if (!plane.Airborne) { Roll(); return; }
            Fly(pos);
        }

        // the take-off run: in its lane along the runway, full throttle, past the take-off speed pull back
        void Roll()
        {
            var fwd = Quaternion.Euler(0, race.Route.GridYaw, 0) * Vector3.forward; var right = Vector3.Cross(Vector3.up, fwd);
            float off = Vector3.Dot(car.Body.position - race.Route.GridPosition, right) - lane;
            float err = Mathf.DeltaAngle(car.transform.eulerAngles.y, race.Route.GridYaw);
            plane.sSteer = Mathf.Clamp(err * .08f - off * .03f, -1f, 1f); plane.sBank = plane.sRudder = 0f;
            plane.sThrottle = 1f; plane.sBrake = 0f;
            plane.sPitch = plane.Airspeed > PlaneFlight.TakeOffSpeed + 3f ? .9f : 0f;
            Why = "take-off run";
        }

        void Fly(Vector3 pos)
        {
            plane.sSteer = 0f;
            float v = Mathf.Max(plane.Airspeed, 40f);
            int i = Nearest(pos); float s = cum[i] + Along(i, pos);
            // how low the line is ahead: under a bridge, over the lake, a shorter look ahead (it holds the line closer)
            float lowAhead = 999f;
            for (float d = 0f; d <= 250f; d += 25f) { var q = At(s + d); lowAhead = Mathf.Min(lowAhead, q.y - Ground(q)); }
            float look = lowAhead < 18f ? Mathf.Clamp(v * .75f, 45f, 85f) : Mathf.Clamp(v * 1.3f, 70f, 160f);
            var target = At(s + look);
            // a mistake: lining up the next ring a few meters off (decided once a ring, 400 m out)
            var ring = NextRing();
            if (ring != null)
            {
                float toRing = Flat(ring.Position - pos).magnitude;
                int id = ring.Id + 1000 * entrant.CompletedLaps;
                if (toRing < 400f && decidedFor != id)
                {
                    decidedFor = id; missSide = 0f;
                    bool safe = ring.Ring > 0f && ring.Position.y - Ground(ring.Position) > 40f;
                    if (safe && rng.NextDouble() < MissChance[Mathf.Clamp(RoadDriver.Difficulty, 0, 2)] && !entrant.Finished && !entrant.IsPlayer) missSide = (rng.NextDouble() < .5 ? -1f : 1f) * (ring.Ring + 7f);
                    if (entrant.IsPlayer && ForceMiss == ring.Id) missSide = Mathf.Max(ring.Ring, ring.RingW) + 9f;
                }
                if (missSide != 0f && toRing < 400f && decidedFor == id) target += ring.Right * missSide;
            }
            var d3 = target - pos; var flat = Flat(d3);
            float wantYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg, yawErr = Mathf.DeltaAngle(plane.YawDeg, wantYaw);
            float bankWant = Mathf.Clamp(yawErr * 2.4f, -68f, 68f);
            plane.sBank = Mathf.Clamp((bankWant - plane.RollDeg) / 14f, -1f, 1f);
            plane.sRudder = Mathf.Clamp(yawErr / 9f, -1f, 1f);
            // the pitch: the line's own slope just ahead, and back onto its height (the far point's height would cut over a rim into a dive)
            // toward the line's point half a second ahead (0.35 s low) (the line's crests and dips are rounded for the plane's pitch rate: make_air_route.py); low (under a bridge, over the lake) the point half a second ahead (it comes down to the opening in time)
            float A(float t) { var q = At(s + v * t); return Mathf.Atan2(q.y - pos.y, v * t) * Mathf.Rad2Deg; }
            float pitchWant = Mathf.Clamp(lowAhead < 18f ? A(.35f) : A(.5f), -34f, 33f);
            if (plane.HeightAboveGround < 3f && lowAhead > 10f) pitchWant = Mathf.Max(pitchWant, 12f);   // too near the ground off the low places: up
            plane.sPitch = Mathf.Clamp((pitchWant - plane.PitchDeg) / 3f, -1f, 1f);
            // the throttle: its pace, eased where the line ahead turns tighter than it can follow at this speed
            float vmax = 999f;
            for (float d = 40f; d <= 420f; d += 40f)
            {
                var a = At(s + d - 30f); var b = At(s + d); var c = At(s + d + 30f);
                float h1 = Mathf.Atan2(b.x - a.x, b.z - a.z), h2 = Mathf.Atan2(c.x - b.x, c.z - b.z);
                float k = Mathf.Abs(Mathf.DeltaAngle(h1 * Mathf.Rad2Deg, h2 * Mathf.Rad2Deg)) * Mathf.Deg2Rad / 30f;   // curvature, 1/m
                if (k < 1e-4f) continue;
                // the turn rate it has at speed u: g tan(62) / u from the bank plus the rudder's 16 deg/s; k u <= that
                float rud = PlaneFlight.RudderRate * Mathf.Deg2Rad * .8f, g = 9.81f * Mathf.Tan(62f * Mathf.Deg2Rad);
                float u = (rud + Mathf.Sqrt(rud * rud + 4f * k * g)) / (2f * k);
                vmax = Mathf.Min(vmax, u + d * .12f);   // farther off, it has time to slow
            }
            float paceV = Mathf.Lerp(PlaneFlight.StallSpeed * .9f, PlaneFlight.TopSpeed, Pace);
            float wantV = Mathf.Min(paceV, vmax);
            plane.sThrottle = entrant.Finished ? .7f : Mathf.Clamp01(Mathf.InverseLerp(PlaneFlight.StallSpeed * .9f, PlaneFlight.TopSpeed, wantV) - (plane.Airspeed > wantV + 6f ? .3f : 0f));
            Why = vmax < paceV ? $"slowing for the turn ({vmax:0} m/s)" : missSide != 0f ? "lining up wide" : "pace";
        }

        RaceGate NextRing()
        {
            var r = race.Route; if (r == null) return null;
            return !entrant.ClockStarted ? r.Gate : entrant.NextCheckpoint < r.Checkpoints.Count ? r.Checkpoints[entrant.NextCheckpoint] : r.Gate;
        }

        // the nearest point of the line (height counting), searched near the last one, or all of it after a jump (a reset)
        int Nearest(Vector3 p)
        {
            int n = path.Length - 1; int best = 0; float bd = float.MaxValue;
            bool local = near >= 0 && (Flat(path[near] - p)).sqrMagnitude < 150f * 150f;
            int from = local ? near - 40 : 0, to = local ? near + 80 : n;
            for (int k = from; k <= to; k++)
            {
                int i = ((k % n) + n) % n; var d = path[i] - p; float dd = d.x * d.x + d.z * d.z + 4f * d.y * d.y;
                if (dd < bd) { bd = dd; best = i; }
            }
            near = best; return best;
        }
        float Along(int i, Vector3 p) { int j = (i + 1) % (path.Length - 1); var ab = Flat(path[j] - path[i]); float len = ab.magnitude; return len < 1e-3f ? 0f : Mathf.Clamp(Vector3.Dot(Flat(p - path[i]), ab) / len, 0f, len); }
        Vector3 At(float s)
        {
            s = Mathf.Repeat(s, L);
            int lo = 0, hi = cum.Length - 1;
            while (lo + 1 < hi) { int mid = (lo + hi) / 2; if (cum[mid] <= s) lo = mid; else hi = mid; }
            return Vector3.Lerp(path[lo], path[lo + 1], Mathf.InverseLerp(cum[lo], cum[lo + 1], s));
        }
        static float Ground(Vector3 p) { float y = GroundQuery.Height(p.x, p.z); return float.IsNaN(y) ? p.y - 100f : y; }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
