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
    // Round 80 (A, Dan: "the ai planes never make a mistake so if you make a mistake you can't make up for it"): human mistakes by difficulty (Easy
    //   most, Hard fewest but some), decided once a ring: a ring lined up wide and missed, then (mostly) gone back for (run out, turn, fly back through
    //   it: rings count either way, then turn again onto the line); a late turn after a ring (it holds its heading a moment and runs wide, which can
    //   miss the next one); a low ring (under a bridge, in the quarry) taken slow and careful; and now and then a whole lap off its pace. The car AI
    //   has no catch-up (only its shortcut choice leans on being behind), so neither has this.
    //   Advise (checks): the pilot works out its controls but does not fly; the check's human-like pilot puts them through the pad with a lag.
    [DefaultExecutionOrder(40)]
    public sealed class PlanePilot : MonoBehaviour
    {
        RaceController race; ArcadeVehicle car; RaceEntrant entrant; PlaneFlight plane;
        int index; float lane; Vector3[] path; float[] cum; float L; int near = -1;
        System.Random rng; int decidedFor = -1; float missSide;
        // Round 80 (A)
        public bool Advise; public float aPitch, aBank, aRudder, aThrottle = -1f, aSteer;
        int lapSeen = -1; float lapPace; bool careful, lateTurn; float lateUntil = -1f; Vector3 lateDir;
        int backFor = -1; int backPhase; float backSince; RaceGate backRing;
        public int Wide, Late, Careful, Backs, BacksDone, OffLaps;   // what it did, for the checks
        public static readonly float[] OffLapChance = { .45f, .3f, .18f }, OffLapSize = { .07f, .05f, .035f };
        public static readonly float[] LateChance = { .12f, .08f, .05f }, CarefulChance = { .6f, .4f, .25f }, GoBackChance = { .75f, .65f, .55f };
        public float Pace { get; private set; }
        public float LastThrottle => plane ? plane.Throttle : 0f;
        public string Why { get; private set; } = "";
        public static readonly float[] Paces = { .88f, .94f, 1f };
        public static readonly float[] MissChance = { .12f, .08f, .05f };
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
            lapPace = Pace;
            if (plane && !Advise) { plane.Scripted = true; plane.sThrottle = 0f; plane.sPitch = plane.sBank = plane.sRudder = plane.sSteer = plane.sBrake = 0f; }
        }

        bool Mistakes => !Advise && !entrant.IsPlayer && !entrant.Finished;
        static int Skill => Mathf.Clamp(RoadDriver.Difficulty, 0, 2);
        // the controls: the plane's own (flying it) or the advice (a check's human-like pilot)
        void Set(float pitch, float bank, float rudder, float throttle, float steer = 0f, float brake = 0f)
        {
            if (Advise) { aPitch = pitch; aBank = bank; aRudder = rudder; aThrottle = throttle; aSteer = steer; return; }
            plane.sPitch = pitch; plane.sBank = bank; plane.sRudder = rudder; plane.sThrottle = throttle; plane.sSteer = steer; plane.sBrake = brake;
        }

        void FixedUpdate()
        {
            if (!race || !plane || !plane.enabled || race.Route == null) return;
            if (race.State == RaceController.Stage.Countdown || race.State == RaceController.Stage.Free) { if (!Advise) plane.sThrottle = 0f; else aThrottle = 0f; return; }
            if (!Advise && car.Body.isKinematic) { car.Body.isKinematic = false; car.Body.linearVelocity = Vector3.zero; }   // GO
            if (plane.Crashed) { backRing = null; lateUntil = -1f; return; }
            // Round 80 (A): a lap's pace, drawn as each lap starts (now and then one off the pace)
            if (entrant.CompletedLaps != lapSeen)
            {
                lapSeen = entrant.CompletedLaps; lapPace = Pace;
                if (Mistakes && rng.NextDouble() < OffLapChance[Skill]) { lapPace = Pace - OffLapSize[Skill] * Mathf.Lerp(.6f, 1f, (float)rng.NextDouble()); OffLaps++; }
            }
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
            Set(plane.Airspeed > PlaneFlight.TakeOffSpeed + 3f ? .9f : 0f, 0f, 0f, 1f, Mathf.Clamp(err * .08f - off * .03f, -1f, 1f));
            Why = "take-off run";
        }

        void Fly(Vector3 pos)
        {
            float v = Mathf.Max(plane.Airspeed, 40f);
            if (backRing != null && GoBack(pos)) return;
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
                    decidedFor = id; missSide = 0f; careful = lateTurn = false; backFor = -1;
                    bool safe = ring.Ring > 0f && ring.Position.y - Ground(ring.Position) > 40f;
                    bool low = ring.Position.y - Ground(ring.Position) < 25f || lowAhead < 18f;
                    if (Mistakes)
                    {
                        double r = rng.NextDouble();
                        if (safe && r < MissChance[Skill]) { missSide = (rng.NextDouble() < .5 ? -1f : 1f) * (Mathf.Max(ring.Ring, ring.RingW) + 22f); Wide++; }
                        else if (safe && r < MissChance[Skill] + LateChance[Skill] && TurnAfter(ring) > 30f) { lateTurn = true; Late++; }
                        else if (low && rng.NextDouble() < CarefulChance[Skill]) { careful = true; Careful++; }
                    }
                    if (entrant.IsPlayer && ForceMiss == ring.Id) missSide = Mathf.Max(ring.Ring, ring.RingW) + 30f;
                }
                if (missSide != 0f && toRing < 400f && decidedFor == id) target += ring.Right * missSide;
                // a ring flown past outside it (a wide line-up, a late turn): mostly gone back for, else on (5 s)
                float past = Vector3.Dot(pos - ring.Position, ring.Forward);
                if (Mistakes && ring != race.Route.Gate && past > 30f && past < 250f && toRing < 600f && backFor != id && decidedFor == id)
                {
                    backFor = id;   // decided once
                    if (rng.NextDouble() < GoBackChance[Skill] && HighAround(ring, pos)) { backPhase = 0; backSince = Time.time; backRing = ring; Backs++; missSide = 0f; return; }
                }
                // the late turn: past the ring it holds its heading a moment (it runs wide)
                if (lateTurn && decidedFor == id && past > 0f) { lateUntil = Time.time + (Skill == 0 ? 1.8f : Skill == 1 ? 1.4f : 1.0f) * Mathf.Lerp(.8f, 1.2f, (float)rng.NextDouble()); lateDir = Flat(car.Body.linearVelocity).normalized; lateTurn = false; }
            }
            if (lateUntil > 0f)
            {
                if (Time.time < lateUntil && lowAhead > 30f) target = new Vector3(pos.x, target.y, pos.z) + lateDir * look;
                else lateUntil = -1f;
            }
            var d3 = target - pos; var flat = Flat(d3);
            float wantYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg, yawErr = Mathf.DeltaAngle(plane.YawDeg, wantYaw);
            float bankWant = Mathf.Clamp(yawErr * 2.4f, -68f, 68f);
            // the pitch: the line's own slope just ahead, and back onto its height (the far point's height would cut over a rim into a dive)
            // toward the line's point half a second ahead (0.35 s low) (the line's crests and dips are rounded for the plane's pitch rate: make_air_route.py); low (under a bridge, over the lake) the point half a second ahead (it comes down to the opening in time)
            float A(float t) { var q = At(s + v * t); return Mathf.Atan2(q.y - pos.y, v * t) * Mathf.Rad2Deg; }
            float pitchWant = Mathf.Clamp(lowAhead < 18f ? A(.35f) : A(.5f), -34f, 33f);
            if (plane.HeightAboveGround < 3f && lowAhead > 10f) pitchWant = Mathf.Max(pitchWant, 12f);   // too near the ground off the low places: up
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
            // and where the line ahead, low, bends up or down more sharply than the plane can pitch at this speed (the dip under I-75): the pitch rate is
            // PlaneFlight's (45 deg/s slow, 55 % of it at top speed)
            for (float d = 30f; d <= 420f; d += 15f)
            {
                var a = At(s + d - 15f); var b = At(s + d); var c = At(s + d + 15f);
                float k = Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(b.y - a.y, 15f) * Mathf.Rad2Deg, Mathf.Atan2(c.y - b.y, 15f) * Mathf.Rad2Deg)) * Mathf.Deg2Rad / 15f;
                if (k < 1e-4f || b.y - Ground(b) > 25f) continue;   // only near the ground: higher up a late pull-up costs nothing
                for (float u = plane.Top; u > 45f; u -= 5f)
                    if (k * u <= .75f * plane.PitchR * Mathf.Lerp(1f, .55f, u / plane.Top) * Mathf.Deg2Rad) { vmax = Mathf.Min(vmax, u + d * .1f); break; }
            }
            float paceV = Mathf.Lerp(PlaneFlight.StallSpeed * .9f, plane.Top, lapPace);
            if (careful && ring != null && Flat(ring.Position - pos).magnitude < 450f) paceV = Mathf.Min(paceV, Mathf.Lerp(PlaneFlight.StallSpeed, plane.Top, .55f));   // a low ring, slow and careful
            float wantV = Mathf.Min(paceV, vmax);
            float thr = entrant.Finished ? .7f : Mathf.Clamp01(Mathf.InverseLerp(PlaneFlight.StallSpeed * .9f, plane.Top, wantV) - (plane.Airspeed > wantV + 6f ? .3f : 0f));
            Set(Mathf.Clamp((pitchWant - plane.PitchDeg) / 3f, -1f, 1f), Mathf.Clamp((bankWant - plane.RollDeg) / 14f, -1f, 1f), Mathf.Clamp(yawErr / 9f, -1f, 1f), thr);
            Why = vmax < paceV ? $"slowing for the turn ({vmax:0} m/s)" : missSide != 0f ? "lining up wide" : lateUntil > 0f ? "late turn" : careful ? "careful at a low ring" : lapPace < Pace ? "off the pace this lap" : "pace";
        }

        // how far the line turns over the 300 m after a ring (a late turn there runs wide)
        float TurnAfter(RaceGate ring)
        {
            int keep = near; int i = Nearest(ring.Position); near = keep; float s0 = cum[i] + Along(i, ring.Position);
            var a = At(s0 + 20f) - At(s0); var b = At(s0 + 300f) - At(s0 + 280f);
            return Vector3.Angle(Flat(a), Flat(b));
        }
        // room to turn round: the ground for 900 m about the ring well under the plane's way
        static bool HighAround(RaceGate ring, Vector3 pos)
        {
            float top = Mathf.Min(ring.Position.y, pos.y);
            for (float a = 0f; a < 360f; a += 30f)
                for (float d = 0f; d <= 900f; d += 150f)
                {
                    var q = ring.Position + Quaternion.Euler(0f, a, 0f) * Vector3.forward * d;
                    if (top - Ground(q) < 45f) return false;
                }
            return true;
        }
        // going back for a missed ring: run out (slowing) to 650 m from it, then turn and fly back through it (either way counts); then on (the
        // line's own law turns it round again); given up after 45 s
        bool GoBack(Vector3 pos)
        {
            var ring = backRing;
            if (entrant.Finished || Time.time - backSince > 45f || NextRing() != ring)
            {
                if (NextRing() != ring && Time.time - backSince <= 45f) BacksDone++;
                backRing = null; near = -1; return false;
            }
            Vector3 target; float thr;
            if (backPhase == 0)
            {
                target = pos + Flat(car.Body.linearVelocity).normalized * 200f; target.y = Mathf.Max(ring.Position.y, Ground(target) + 60f);
                thr = .25f; Why = "missed a ring: running out to turn back";
                if (Flat(ring.Position - pos).magnitude > 650f) backPhase = 1;
            }
            else { target = ring.Position; thr = .55f; Why = "going back for the ring"; }
            var flat = Flat(target - pos);
            float wantYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg, yawErr = Mathf.DeltaAngle(plane.YawDeg, wantYaw);
            float bankWant = Mathf.Clamp(yawErr * 2.4f, -68f, 68f);
            float pitchWant = Mathf.Clamp(Mathf.Atan2(target.y - pos.y, Mathf.Max(flat.magnitude, 60f)) * Mathf.Rad2Deg, -25f, 25f);
            if (plane.HeightAboveGround < 25f) pitchWant = Mathf.Max(pitchWant, 12f);
            if (plane.Airspeed < PlaneFlight.StallSpeed + 8f) thr = 1f;
            Set(Mathf.Clamp((pitchWant - plane.PitchDeg) / 3f, -1f, 1f), Mathf.Clamp((bankWant - plane.RollDeg) / 14f, -1f, 1f), Mathf.Clamp(yawErr / 9f, -1f, 1f), thr);
            return true;
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
