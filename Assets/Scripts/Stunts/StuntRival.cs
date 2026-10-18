using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 38 (Part D; Dan: "is there the ability to play ai on the stunt course?"): a stunt rival in the stunt park. Racer's rivals
    // are its Stunt Races' RoadDriver plus RivalTricks; Racer has no AI in its stunt park, so the driving here is new, the tricks are Racer's:
    //   Driving: the park's authored lines (StuntPark.aiLines, from Tools/mapgen/make_stunt_park.py AI_LINES): pure pursuit along the line with a
    //     cross-track term (AiShortcut.Steer's rule), the speed from the line's own speeds (braked for in time) and its bends; at each line's end the
    //     next is picked by its weights. Round the loop and in the air the line lets go (the loop holds the wheels; the air is RivalTricks').
    //   Tricks: Racer's RivalTricks (StuntRace.cs 0.119 / 0.120) as it is: on a flight long enough it picks a trick its airtime allows by the
    //     difficulty and its luck, turns it with the player's air steering (ArcadeVehicle.AiAir, the same rates), stops it square, levels for the
    //     landing; now and then it bails (more on Easy).
    //   Scoring: the player's rule (ArcadeActivities.Score: flips, rolls, spins, the barrel roll off its ramp, the loop, the jump's distance and air,
    //     smashing (Part B: a link on the ground, SMASH in the air), combos within ComboGap on the ground, bails end them); each banked combo goes to
    //     the Stunt event (Banked).
    //   Not perfect (CLAUDE.md): on some lines a mistake: a slow run-up (short on a jump), a wide line (off the kicker's middle, a clipped frame),
    //     late braking (runs wide); never left stuck: no progress for 6 s, lying on its side or roof, or outside the park puts it back on its line.
    // The split-screen AI player 2 in the stunt park and the Stunt event drives with this too, its tricks scored by its own ArcadeActivities
    // (Scoring off). Game layer: the park's lines are data next to the world's park; this rides on them.
    public sealed class StuntRival : MonoBehaviour
    {
        public static readonly List<StuntRival> All = new();
        public ArcadeVehicle car; public string Name = "AI"; public int Difficulty = 1; public bool Scoring = true;
        public System.Action<StuntRival, int> Banked;   // a combo banked: its points
        public int Points { get; private set; }
        public int Best { get; private set; }
        public int Tricks { get; private set; }
        public int Bails { get; private set; }
        public int Flights { get; private set; }
        public int Recoveries { get; private set; }
        public int Smashes { get; private set; }
        public int Combos { get; private set; }
        public float Outside { get; private set; }   // checks: seconds spent outside the park
        public string LineId => line != null ? line.id : "";
        public string LastLine { get; private set; } = ""; public float LineUntil { get; private set; } public bool LastBail { get; private set; }
        public string Log { get; private set; } = "";
        public readonly List<string> LinesTaken = new();   // checks
        public int Links => links;
        public bool Busy => flying || inLoop;
        System.Random rng; float pace = 1f;

        void OnEnable() { All.Add(this); FenceChunk.PanelBrokenBy += OnPanel; TreeTrunks.SnappedBy += OnTree; }
        void OnDisable() { All.Remove(this); FenceChunk.PanelBrokenBy -= OnPanel; TreeTrunks.SnappedBy -= OnTree; if (car) car.AiAir = false; }
        public void Initialize(ArcadeVehicle vehicle, string name, int difficulty, int seed, bool scoring)
        {
            car = vehicle; Name = name; Difficulty = Mathf.Clamp(difficulty, 0, 2); Scoring = scoring; rng = new System.Random(seed);
            pace = .93f + (float)rng.NextDouble() * .07f; if (Difficulty == 0) pace -= .06f;
            car.enabled = false; Pick(First()); Log = "";
        }

        // ---------------------------------------------------------------- the lines
        StuntPark.AiLine line; float[] cum; float total, lastS; Mistake mistake;
        sealed class Mistake { public int kind; public float s0, s1, amount; }   // 1 slow run-up, 2 wide line, 3 late braking
        StuntPark.AiLine Find(string id) => StuntPark.Current && StuntPark.Current.aiLines != null ? StuntPark.Current.aiLines.FirstOrDefault(l => l.id == id) : null;
        StuntPark.AiLine First()
        {
            // the line whose start is nearest ahead of the vehicle (the hub after the start line)
            var lines = StuntPark.Current && StuntPark.Current.aiLines != null ? StuntPark.Current.aiLines : new StuntPark.AiLine[0];
            var p = car.Body.position; StuntPark.AiLine best = null; float bd = float.MaxValue;
            foreach (var l in lines) { if (l.points.Length < 2) continue; float d = Flat(l.points[0] - p).magnitude; if (Mathf.Abs(l.points[0].y - p.y) > 6f) d += 50f; if (d < bd) { bd = d; best = l; } }
            return best;
        }
        void Pick(StuntPark.AiLine l)
        {
            line = l; lastS = 0f; if (l == null) return;
            cum = new float[l.points.Length]; for (int i = 1; i < l.points.Length; i++) cum[i] = cum[i - 1] + Flat(l.points[i] - l.points[i - 1]).magnitude; total = cum[cum.Length - 1];
            LinesTaken.Add(l.id);
            // the AI is not perfect: now and then a mistake on this line (more on Easy)
            mistake = null; float chance = Difficulty == 0 ? .3f : Difficulty == 2 ? .08f : .16f;
            if (rng.NextDouble() < chance)
            {
                int kind = 1 + rng.Next(3); float at = (float)rng.NextDouble() * Mathf.Max(1f, total - 30f);
                mistake = new Mistake { kind = kind, s0 = at, s1 = at + (kind == 2 ? 22f : 35f), amount = kind == 1 ? .75f + (float)rng.NextDouble() * .1f : kind == 2 ? (rng.Next(2) == 0 ? -1f : 1f) * (1.0f + (float)rng.NextDouble() * .8f) : .55f };
                Log += $"{l.id}: mistake {(kind == 1 ? "slow run-up" : kind == 2 ? "wide line" : "late braking")} from {mistake.s0:0} m\n";
            }
        }
        void Next()
        {
            if (line == null || line.next == null || line.next.Length == 0) { Pick(First()); return; }
            float sum = 0f; for (int i = 0; i < line.next.Length; i++) sum += i < line.weights.Length ? line.weights[i] : 1f;
            float r = (float)rng.NextDouble() * sum; string pick = line.next[0];
            for (int i = 0; i < line.next.Length; i++) { r -= i < line.weights.Length ? line.weights[i] : 1f; if (r <= 0f) { pick = line.next[i]; break; } }
            Pick(Find(pick) ?? First());
        }
        Vector3 At(float s) { s = Mathf.Clamp(s, 0f, total); int i = 1; while (i < cum.Length - 1 && cum[i] < s) i++; float t = Mathf.InverseLerp(cum[i - 1], cum[i], s); return Vector3.Lerp(line.points[i - 1], line.points[i], t); }
        float SpeedAt(int i) => line.speeds != null && i < line.speeds.Length ? line.speeds[i] : 0f;
        // nearest point on the line within 40 m of the last (xz; a line's roof part never lies over its floor part)
        float Project(Vector3 p, out float lateral)
        {
            float best = float.MaxValue, bs = lastS; lateral = 0f; var q = Flat(p);
            for (int i = 0; i + 1 < line.points.Length; i++)
            {
                if (cum[i + 1] < lastS - 15f || cum[i] > lastS + 40f) continue;
                Vector3 a = Flat(line.points[i]), b = Flat(line.points[i + 1]), ab = b - a; float L = ab.magnitude; if (L < 1e-3f) continue;
                float t = Mathf.Clamp01(Vector3.Dot(q - a, ab) / (L * L)); var c = a + ab * t; float d = (q - c).sqrMagnitude;
                if (d < best) { best = d; bs = cum[i] + t * L; lateral = Vector3.Dot(q - c, Vector3.Cross(Vector3.up, ab / L)); }
            }
            return bs;
        }

        // ---------------------------------------------------------------- driving
        float stuck, lying, outside, nextRecover, progressBest, progressAt; bool inLoop; float loopAcc, loopPrev = float.NaN, loopOut = 1f;
        public bool Held => StuntEvent.Current && StuntEvent.Current.Holds(car);
        public bool Stopped;   // the event is over for it: brake and stand
        void FixedUpdate()
        {
            if (!car || !car.Body || !StuntPark.Current) return;
            car.enabled = false;
            if (Held) { car.AiAir = false; return; }
            float dt = Time.fixedDeltaTime;
            if (Stopped) { car.AiAir = false; car.Simulate(0f, 1f, 0f, dt); return; }
            if (line == null) { Pick(First()); if (line == null) return; }
            var pos = car.Body.position; float speed = car.ForwardSpeed;
            float s = Project(pos, out float lateral); lastS = Mathf.Max(lastS, s - 2f); if (s > lastS) lastS = s;
            if (s >= total - 2.5f) { Next(); s = Project(pos, out lateral); }
            LoopStep(pos, dt);
            bool grounded = car.GroundedWheels > 0;
            // steering: toward a point ahead on the line (a wide-line mistake shifts it), with a cross-track term
            float look = Mathf.Clamp(4f + speed * .25f, 5f, 12f);
            float offset = mistake != null && mistake.kind == 2 && s > mistake.s0 && s < mistake.s1 ? mistake.amount : 0f;
            var here = At(s); var fwd = Flat(At(s + .8f) - here); if (fwd.sqrMagnitude < 1e-4f) fwd = Flat(car.transform.forward); fwd.Normalize();
            var aim = At(s + look) + Vector3.Cross(Vector3.up, fwd) * offset;
            float cte = lateral - offset;
            float head = Vector3.SignedAngle(Flat(car.transform.forward).normalized, Flat(aim - pos).normalized, Vector3.up);
            float corr = -Mathf.Atan2(1.2f * cte, Mathf.Max(speed, 1f) + 2f) * Mathf.Rad2Deg;
            float steer = Mathf.Clamp((head + corr) / 20f, -1f, 1f);
            if (!grounded || inLoop) steer = 0f;
            // speed: the vehicle's pace, the line's own speeds ahead (braked for in time), its bends
            float grip = car.maxGripAcceleration * (Difficulty == 0 ? .62f : Difficulty == 2 ? .82f : .74f);
            float judgment = car.braking * (Difficulty == 0 ? .6f : .85f) * (mistake != null && mistake.kind == 3 && s > mistake.s0 && s < mistake.s1 ? mistake.amount : 1f);
            float want = car.topSpeed * pace;
            for (int i = 0; i < line.points.Length; i++)
            {
                float sp = SpeedAt(i); if (sp <= 0f) continue; float d = cum[i] - s;
                if (d < -1f || d > 80f) continue;
                if (mistake != null && mistake.kind == 1 && cum[i] > mistake.s0 && cum[i] < mistake.s1) sp *= mistake.amount;
                want = Mathf.Min(want, d <= 0f ? sp : Mathf.Sqrt(sp * sp + 2f * judgment * Mathf.Max(0f, d - 3f)));
            }
            for (float d = 4f; d <= 36f; d += 4f)
            {
                Vector3 a = Flat(At(s + d - 4f)), b = Flat(At(s + d)), c = Flat(At(s + d + 4f));
                float area = Mathf.Abs(Vector3.Cross(b - a, c - a).y); if (area < 1e-3f) continue;
                float r = (b - a).magnitude * (c - b).magnitude * (c - a).magnitude / (2f * area);
                want = Mathf.Min(want, Mathf.Sqrt(grip * r + 2f * judgment * Mathf.Max(0f, d - 6f)));
            }
            if (inLoop) want = car.topSpeed;
            float throttle = speed < want ? Mathf.Clamp01((want - speed) * .8f) : 0f, brake = speed > want + .7f ? Mathf.Clamp01((speed - want) * .3f) : 0f;
            if (!grounded) { throttle = speed < want ? 1f : 0f; brake = 0f; }
            // not stuck: progress along the line; lying on its side or roof; outside the park
            if (s > progressBest + 1.5f || line != null && s < progressBest - 30f) { progressBest = s; progressAt = Time.time; }
            if (progressAt == 0f) progressAt = Time.time;
            lying = car.transform.up.y < .35f && grounded ? lying + dt : 0f;
            outside = StuntPark.Inside(pos, 2f) ? 0f : outside + dt; if (outside > 0f) Outside += dt;
            bool fell = pos.y < StuntPark.Current.FloorY(pos.x, pos.z) - 10f;
            if (Time.time > nextRecover && (Time.time - progressAt > 6f && !flying || lying > 2.5f || outside > 2f || fell)) { Recover(s, Time.time - progressAt > 6f ? "no progress" : lying > 2.5f ? "on its side" : outside > 2f ? "outside the park" : "fell"); return; }
            // backing off a wall it is pushed against (as RoadDriver's stall back-off)
            float stall = Time.time - progressAt; if (stall > 3f && stall < 4.5f && grounded) { throttle = 0f; brake = .6f; steer = -steer; }
            car.Simulate(throttle, brake, steer, dt);
            TrickStep(dt);
        }
        float lastRecoverAt = -100f; Vector3 lastRecoverPos; int repeats;
        void Recover(float s, string why)
        {
            nextRecover = Time.time + 2f; Recoveries++;
            // stuck again where it was put back last time: past the trouble this time (forward along the line), and the line's mistake is off
            bool again = Time.time - lastRecoverAt < 25f && (car.Body.position - lastRecoverPos).sqrMagnitude < 12f * 12f; lastRecoverAt = Time.time; lastRecoverPos = car.Body.position; repeats = again ? repeats + 1 : 0;
            Log += $"recover ({why}) on {line?.id} at {s:0} m, ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})\n";
            if (links > 0) Bank();
            if (repeats > 2) { Next(); repeats = 0; s = 6f; }   // (three times there: on to the next line)
            foreach (float back in repeats > 0 ? new[] { -14f * repeats, -20f * repeats, -26f * repeats, 6f } : new[] { 6f, 12f, 2f, 18f, 26f })
            {
                float ss = Mathf.Clamp(s - back, 0f, total); var p = At(ss); var f = Flat(At(ss + 1.5f) - p).normalized; if (f.sqrMagnitude < .5f) f = Flat(car.transform.forward).normalized;
                if (!Ground(p, out var g) || !Clear(g, f)) continue;
                var rot = Quaternion.LookRotation(f, Vector3.up);
                car.Body.position = g + Vector3.up * .6f; car.Body.rotation = rot; transform.SetPositionAndRotation(car.Body.position, rot);
                car.Body.linearVelocity = f * 4f; car.Body.angularVelocity = Vector3.zero; car.ClearSteering(); car.AiAir = false; Physics.SyncTransforms();
                lastS = ss; progressBest = ss; progressAt = Time.time; lying = outside = 0f; flying = false; ground = 0f; mistake = null; return;
            }
            // nowhere clear on this line: the park's own reset spots (as the player's)
            foreach (var (at, facing) in StuntPark.ResetsNear(car.Body.position))
            {
                if (!Clear(at, facing * Vector3.forward)) continue;
                car.Body.position = at; car.Body.rotation = facing; transform.SetPositionAndRotation(at, facing); car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero; car.ClearSteering(); Physics.SyncTransforms();
                Pick(First()); progressAt = Time.time; lying = outside = 0f; flying = false; return;
            }
        }
        bool Ground(Vector3 p, out Vector3 g)
        {
            g = p; var hits = Physics.RaycastAll(p + Vector3.up * 4f, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; bool any = false;
            foreach (var h in hits) { if (h.rigidbody == car.Body || h.normal.y < .6f) continue; float d = Mathf.Abs(h.point.y - p.y); if (d < best) { best = d; g = h.point; any = true; } }
            return any;
        }
        static readonly Collider[] overlap = new Collider[16];
        bool Clear(Vector3 q, Vector3 f)
        {
            if (f.sqrMagnitude < 1e-4f) f = Vector3.forward;
            var rot = Quaternion.LookRotation(Flat(f).normalized, Vector3.up);
            var box = car.GetComponent<BoxCollider>(); var half = box ? box.size * .5f + new Vector3(.3f, 0, .4f) : new Vector3(1.3f, .7f, 2.4f);
            int n = Physics.OverlapBoxNonAlloc(q + Vector3.up * (half.y + .35f), half, overlap, rot, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) { var h = overlap[i]; if (h is TerrainCollider || h.attachedRigidbody == car.Body) continue; return false; }
            return true;
        }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        // the loop (ArcadeActivities.LoopStep's rule): once round on the wheels is the LOOP trick
        void LoopStep(Vector3 p, float dt)
        {
            var c = StuntPark.Current; if (!c || !c.loopCollider) { inLoop = false; return; }
            var rel = p - StuntPark.LoopOrigin; float r = StuntPark.LoopRadius;
            float fwd = Vector3.Dot(rel, StuntPark.LoopForward), up = rel.y, lat = Vector3.Dot(rel, StuntPark.LoopSide);
            float dist = new Vector2(fwd, up - r).magnitude;
            bool inside = lat > -4 && lat < StuntPark.LoopShift + 4 && dist > r - 3.2f && dist < r + 1.5f && Mathf.Abs(fwd) < r + 2.5f;
            if (!inside) { loopOut += dt; if (loopOut > .4f) { loopAcc = 0; loopPrev = float.NaN; inLoop = false; } return; }
            loopOut = 0; float phi = Mathf.Atan2(fwd, r - up) * Mathf.Rad2Deg;
            if (!float.IsNaN(loopPrev)) { float d = Mathf.DeltaAngle(loopPrev, phi); if (d > 0) loopAcc += d; else if (d < -15) loopAcc = 0; }
            loopPrev = phi; inLoop = loopAcc > 60f;
            if (loopAcc >= 330 && car.GroundedWheels >= 2)
            {
                loopAcc = float.NegativeInfinity; flying = false; ground = 1f; Tricks++;
                Link(ArcadeActivities.LoopPoints, "LOOP"); Say("LOOP", false);
            }
        }

        // ---------------------------------------------------------------- Racer's RivalTricks (StuntRace.cs), with the player's scoring rule
        bool flying, planned, bail, abort, barrel; float air, ground, stable, touch = -1, since, landAt; Vector3 takeoff, landing, turned;
        readonly List<(int axis, float degrees)> plan = new(); int step; float stepBase; string planName = "";
        int links, comboPoints, smashPoints, smashes; float smashLinkAt = -10f; int smashBurst; readonly List<string> names = new();
        static readonly float[] TryChance = { .6f, .82f, .95f }, BailChance = { .22f, .1f, .04f };
        sealed class Trick { public string name; public (int axis, float degrees)[] steps; public float seconds; }
        static readonly Trick[] Menu =
        {
            new() { name = "BACKFLIP", steps = new[] { (0, -360f) }, seconds = 1.5f },
            new() { name = "ROLL", steps = new[] { (2, 360f) }, seconds = 1.5f },
            new() { name = "360 SPIN", steps = new[] { (1, 360f) }, seconds = 1.6f },
            new() { name = "FRONTFLIP", steps = new[] { (0, 360f) }, seconds = 1.5f },
            new() { name = "DOUBLE BACKFLIP", steps = new[] { (0, -720f) }, seconds = 2.7f },
            new() { name = "BACKFLIP + 360 SPIN", steps = new[] { (0, -360f), (1, 360f) }, seconds = 3.2f },
            new() { name = "ROLL + 360 SPIN", steps = new[] { (2, 360f), (1, 360f) }, seconds = 3.2f },
            new() { name = "DOUBLE BACKFLIP + 360 SPIN", steps = new[] { (0, -720f), (1, 360f) }, seconds = 4.4f },
            new() { name = "BACKFLIP + ROLL", steps = new[] { (0, -360f), (2, 360f) }, seconds = 3.1f },
            new() { name = "FRONTFLIP + 360 SPIN", steps = new[] { (0, 360f), (1, 360f) }, seconds = 3.2f },
            new() { name = "BACKFLIP + ROLL + 360 SPIN", steps = new[] { (0, -360f), (2, 360f), (1, 360f) }, seconds = 4.8f },
        };
        public void Close() { if (links > 0) Bank(); if (car) car.AiAir = false; }
        void Say(string text, bool bailed) { LastLine = text; LineUntil = Time.time + 2.2f; LastBail = bailed; }
        void TrickStep(float dt)
        {
            bool any = car.GroundedWheels > 0, grounded = car.GroundedWheels >= 2; var cfg = car.GetComponent<VehicleConfiguration>();
            if (inLoop) { flying = false; car.AiAir = false; return; }
            if (!flying)
            {
                if (any) { ground += dt; if (links > 0 && (since += dt) >= ArcadeActivities.ComboGap) Bank(); }
                else if (ground >= .2f && car.Body.linearVelocity.magnitude > 3 && !(cfg && cfg.WipedOut))
                {
                    flying = true; air = stable = 0; touch = -1; turned = Vector3.zero; takeoff = car.Body.position; planned = bail = abort = false; plan.Clear(); step = 0; planName = ""; smashPoints = smashes = 0;
                    if (Time.time - groundSmashAt < ArcadeActivities.TakeoffGrace && groundSmashPoints > 0)   // a smash just before the takeoff is the flight's (as the player's)
                    { comboPoints = Mathf.Max(0, comboPoints - groundSmashPoints); if (groundSmashNewLink && links > 0) { links--; if (names.Count > 0) names.RemoveAt(names.Count - 1); } smashPoints = groundSmashPoints; smashes = 1; groundSmashPoints = 0; }
                    barrel = Vector3.Distance(takeoff, StuntPark.BarrelLip) < 7 && Vector3.Dot(car.Body.linearVelocity, StuntPark.BarrelForward) > 3;
                }
                else ground = 0;
                return;
            }
            turned += car.transform.InverseTransformDirection(car.Body.angularVelocity) * (dt * Mathf.Rad2Deg);
            if (cfg && cfg.WipedOut) { Bailed("wiped out"); return; }
            if (any && touch < 0) { touch = 0; car.AiAir = false; }
            if (touch >= 0) touch += dt;
            if (!grounded || car.transform.up.y < .5f)
            {
                if (!grounded && touch < 0) { air += dt; if (!planned && air >= .12f) Plan(); Steer(); }
                stable = 0;
                if (touch >= ArcadeActivities.RecoverWindow) { if (car.transform.up.y < .5f) { Bailed("landed on its " + (car.transform.up.y < ArcadeActivities.RoofUp ? "roof" : "side")); return; } touch = -1; }
                return;
            }
            car.AiAir = false;
            if (stable == 0) landing = car.Body.position;
            stable += dt; if (stable < .3f) return;
            flying = false; ground = stable; Landed();
        }
        float TimeToLand()
        {
            var p = car.Body.position - car.transform.up * .55f; var v = car.Body.linearVelocity; var g = Physics.gravity; float t = 0, dt = .05f;
            while (t < 9)
            {
                var next = p + v * dt + .5f * g * dt * dt; var seg = next - p;
                foreach (var hit in Physics.RaycastAll(p, seg.normalized, seg.magnitude, car.groundMask, QueryTriggerInteraction.Ignore))
                    if (hit.rigidbody != car.Body && !hit.collider.transform.IsChildOf(car.transform)) return t + dt * hit.distance / Mathf.Max(1e-3f, seg.magnitude);
                p = next; v += g * dt; t += dt;
            }
            return 9;
        }
        void Plan()
        {
            planned = true; Flights++;
            float toLand = TimeToLand(), totalAir = air + toLand, left = toLand - .5f; int d = Difficulty; landAt = Time.time + toLand;
            var fit = Menu.Where(m => m.seconds <= left).ToList();
            if (fit.Count == 0 || car.airControl <= 0) { Log += $"flight {Flights}: {totalAir:0.0} s, no trick\n"; return; }
            if (rng.NextDouble() > TryChance[d]) { Log += $"flight {Flights}: {totalAir:0.0} s, chose none\n"; return; }
            var byTime = fit.OrderBy(m => m.seconds).ThenBy(_ => rng.Next()).ToList();
            var pick = d == 0 ? byTime[rng.Next(Mathf.Min(2, byTime.Count))] : d == 2 ? byTime[byTime.Count - 1 - rng.Next(Mathf.Min(2, byTime.Count))] : byTime[rng.Next(byTime.Count)];
            plan.AddRange(pick.steps); planName = pick.name; step = 0; stepBase = turned[plan[0].axis];
            bail = rng.NextDouble() < BailChance[d];
            Log += $"flight {Flights}: {totalAir:0.0} s, {pick.name}{(bail ? " (will bail)" : "")}\n";
        }
        void Steer()
        {
            if (step >= plan.Count || abort) { car.AiAir = false; return; }
            if (landAt - Time.time < .35f) { abort = true; car.AiAir = false; return; }
            var (axis, degrees) = plan[step]; float done = turned[axis] - stepBase, remaining = degrees - done;
            float rate = axis == 1 ? ArcadeVehicle.AirSpinRate : axis == 0 ? ArcadeVehicle.AirTipRate : ArcadeVehicle.AirRollRate;
            float want = Mathf.Clamp(remaining * Mathf.Deg2Rad * 4f, -rate, rate);
            float w = car.transform.InverseTransformDirection(car.Body.angularVelocity)[axis];
            if (Mathf.Abs(remaining) < 6 && Mathf.Abs(w) < .5f) { step++; if (step < plan.Count) stepBase = turned[plan[step].axis]; else car.AiAir = false; return; }
            var input = Vector3.zero; input[axis] = axis == 2 ? -want / rate : want / rate;
            car.AiAir = true; car.AiAirInput = input;
        }
        void Landed()
        {
            float distance = Vector3.ProjectOnPlane(landing - takeoff, Vector3.up).magnitude;
            if (air < ArcadeActivities.MinStuntAir || distance < ArcadeActivities.MinStuntDistance && Mathf.Abs(turned.y) < 300)
            {
                if (smashes > 0) { int pts = smashPoints; smashPoints = smashes = 0; GroundSmash("SMASH", pts); }   // a hop: what it broke counts on the ground
                return;
            }
            if (bail && planName != "") { Bailed("bailed the " + planName.ToLowerInvariant()); car.Body.linearVelocity *= .6f; car.Body.angularVelocity += car.transform.forward * 1.2f; return; }
            var f = ArcadeActivities.Score(turned, barrel, distance, air, false, smashPoints, smashes); smashPoints = smashes = 0;
            Tricks += f.names.Count;
            Link(f.points, f.names.Count > 0 ? f.Names : "JUMP");
            if (f.names.Count > 0) Say(f.Names + (f.types > 1 ? "  " + f.Multiplier : ""), false);
            Log += $"  landed: {(f.names.Count > 0 ? f.Names + (f.types > 1 ? " " + f.Multiplier : "") : "no trick")} {f.points} pts ({distance:0} m, {air:0.0} s)\n";
        }
        void Link(int points, string what) { if (links < ArcadeActivities.MaxLinks) links++; comboPoints += points; since = 0; names.Add(what); }
        void Bailed(string why)
        {
            flying = false; ground = 0; car.AiAir = false; Bails++; smashPoints = smashes = 0; Say("BAILED", true); Log += $"  bail: {why}\n";
            if (links > 0) Bank();
        }
        void Bank()
        {
            int total = comboPoints * Mathf.Max(1, links);
            if (total > 0 && Scoring) { Points += total; Combos++; Best = Mathf.Max(Best, total); Banked?.Invoke(this, total); Log += $"  banked {total} (x{links}: {string.Join(" · ", names)})\n"; }
            links = comboPoints = 0; names.Clear(); since = 0;
        }
        // Part B: smashing counts (the player's rule)
        void OnPanel(FenceChunk chunk, int panel, ArcadeVehicle by) { if (by != car || !car) return; var v = ArcadeActivities.Value(chunk); Smash(v.name, v.points); }
        void OnTree(ArcadeVehicle by, Vector3 at) { if (by != car || !car) return; Smash("TREE", ArcadeActivities.TreePoints); }
        void Smash(string what, int points)
        {
            if (Stopped || Held) return; Smashes++;
            if (flying) { smashPoints += points; smashes++; Say("SMASH: " + what, false); return; }
            GroundSmash(what, points);
        }
        float groundSmashAt = -10f; int groundSmashPoints; bool groundSmashNewLink;
        void GroundSmash(string what, int points)
        {
            groundSmashAt = Time.time; groundSmashPoints = points; groundSmashNewLink = !(links > 0 && Time.time - smashLinkAt < ArcadeActivities.SmashBurst && names.Count > 0 && names[names.Count - 1].StartsWith("SMASH"));
            if (links > 0 && Time.time - smashLinkAt < ArcadeActivities.SmashBurst && names.Count > 0 && names[names.Count - 1].StartsWith("SMASH")) { comboPoints += points; since = 0; smashBurst++; smashLinkAt = Time.time; Say($"SMASH x{smashBurst}: {what}", false); return; }
            smashLinkAt = Time.time; smashBurst = 1; Link(points, "SMASH: " + what); Say("SMASH: " + what, false);
        }

        // its trick over it for a moment, near a camera (Racer's StuntRaceScore labels): gold, red for a bail
        static GUIStyle label;
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || !car || GameMenus.Open || WorldMap.IsOpen || TrailerMode.HudHidden) return;
            bool line = Time.time < LineUntil && LastLine != "";
            foreach (var cam in Camera.allCameras)
            {
                if (!cam || !cam.isActiveAndEnabled || cam.targetTexture) continue;
                var at = car.Body.position + Vector3.up * 2.4f; var sp = cam.WorldToScreenPoint(at);
                float d = Vector3.Distance(cam.transform.position, at); if (sp.z <= 0 || d > 70f || !cam.pixelRect.Contains(new Vector2(sp.x, sp.y))) continue;
                label ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, richText = true };
                label.fontSize = Mathf.Clamp(Mathf.RoundToInt(cam.pixelHeight / 40f * (1f - d / 140f)), 11, 26);
                string text = $"<color=#FFFFFF>{Name}</color>" + (line ? $"\n<color={(LastBail ? "#FF7A66" : "#FFD24A")}>{LastLine}</color>" : "");
                var r = new Rect(sp.x - 160, Screen.height - sp.y - 30, 320, 60);
                var c = label.normal.textColor; label.normal.textColor = new Color(0, 0, 0, .8f); GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), System.Text.RegularExpressions.Regex.Replace(text, "<color=[^>]+>|</color>", ""), label); label.normal.textColor = c;
                GUI.Label(r, text, label);
            }
        }
    }
}
