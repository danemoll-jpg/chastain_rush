using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 73 in the exe (-exeRound73 <dir> [-r73Only dan,bars,kdrive,leap,qroad,qjump,qcrowd,qreset,qtrack,airprobe,airpilot,allraces] -logFile <dir>\Player.log):
    // CHECK IT AS DAN PLAYS IT (TODO.md Round 73, item 0). Everything is driven through the game's own input: a virtual controller (an Input System
    // device the check adds; the player's VehicleInput and PlaneFlight read it like Dan's pad) worked by a human-like driver or pilot: it sees where
    // it wants to go a moment late (reaction lag), holds an imperfect line (a slow wander either side), brakes late, lifts now and then; when stuck it
    // presses Y. Pictures are the player's own camera (the chase view, the HUD on), taken along the way.
    //   dan       Dan's six reports (session 2026-10-26_18-37-35-665_130b58) from his exact position and heading: BUG-001 / 002 in the Kennesaw Mountain race,
    //             BUG-003 to 006 in Free Roam (in daylight, and BUG-004 / 005 at night as he saw them)
    //   bars      every car course, each direction it is raced: the race started (its barriers up), its line and lead-in swept for anything standing
    //             across them (a picture at each barrier found on a line)
    //   kdrive    Kennesaw Mountain Drive in Free Roam, up past BUG-002 to the summit lot (BUG-003) and back down, at a normal speed
    //   leap      the summit leap from its fast-travel spot: flat out (the Drifter Twin, the Street Classic, the Needle 600), a short one (half
    //             throttle), a reset after the landing
    //   qroad     from the QUARRY JUMPS sign on the airport's service road along the quarry jumps drive to each start box
    //   qjump     each jump from its fast-travel spot: into the box, the countdown, the run-up held by eye at the window's middle (the HUD's speed),
    //             the jump, the event's result; The Corner flat out; the Street Classic short
    //   qcrowd    the bleachers (BUG-004 / 005's spots and each stand)
    //   qreset    the reset at BUG-006's spot and wedged against a kicker's skirt
    //   qtrack    the Quarry Track: a race with 3 AI riders, the player driven by the human-like driver round the lap (pictures at each checkpoint)
    //   airprobe  the Quarry Air Race's line against the built world (Round 71's aprobe)
    //   airpilot  the Quarry Air Race flown by the human-like pilot by the arrow alone: take-off, ring 3 missed on purpose and gone back for, the rest
    //   allraces  one AI race per car course (3 AI, 1 lap, the player driven by the human-like driver): every racer finishes
    public sealed partial class Round20Runner
    {
        Gamepad r73pad;
        [Serializable] class R73Lines { public float[] drive_up, leap, leap_lip, leap_dir, leap_start, quarry_drive; public float leap_yaw; }
        R73Lines r73lines;
        System.Random r73rng = new System.Random(73);

        void R73Pad()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;   // a check runs minimized: its pad counts without focus
            if (r73pad == null || !r73pad.added) r73pad = InputSystem.AddDevice<Gamepad>("Round73 pad");
            var vi = car.GetComponent<VehicleInput>(); if (vi) vi.Scripted = false;
            var pl = car.GetComponent<PlaneFlight>(); if (pl) pl.Scripted = false;
            car.enabled = true;
        }
        void R73Set(float steer, float throttle, float brake, float pitch = 0f, bool y = false)
        {
            var st = new GamepadState { leftStick = new Vector2(Mathf.Clamp(steer, -1f, 1f), Mathf.Clamp(pitch, -1f, 1f)), rightTrigger = Mathf.Clamp01(throttle), leftTrigger = Mathf.Clamp01(brake) };
            if (y) st = st.WithButton(GamepadButton.North);
            InputSystem.QueueStateEvent(r73pad, st);
        }
        IEnumerator R73Reset() { R73Set(0, 0, 0, 0, true); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null; R73Set(0, 0, 0); yield return new WaitForSecondsRealtime(.8f); }
        R73Lines Lines() { if (r73lines == null) { var a = Environment.GetCommandLineArgs(); int ai = Array.IndexOf(a, "-r73Lines"); var p = Path.Combine(Environment.CurrentDirectory, ai >= 0 && ai + 1 < a.Length ? a[ai + 1] : "local/round73/r73_lines.json"); if (File.Exists(p)) r73lines = JsonUtility.FromJson<R73Lines>(File.ReadAllText(p)); } return r73lines; }   // Round 74: -r73Lines <file>
        static List<Vector3> R73Line(float[] flat) { var l = new List<Vector3>(); if (flat == null) return l; for (int i = 0; i + 1 < flat.Length; i += 2) l.Add(RaceRoute.Ground(flat[i], flat[i + 1])); return l; }
        static float[] Cum(List<Vector3> l) { var c = new float[l.Count]; for (int i = 1; i < l.Count; i++) c[i] = c[i - 1] + Flat(l[i] - l[i - 1]).magnitude; return c; }

        // ---------------------------------------------------------------- the human-like driver
        public sealed class HumanRun { public float time, maxAir, airNow, topSpeed; public int resets, stuck; public bool reachedEnd; public string note = ""; public List<string> pics = new List<string>(); public float sEnd; public StringBuilder trace = new StringBuilder(); }
        // drives 'line' from where the vehicle is to its end (or until 'stop'), at up to 'cap' m/s (a human: a little late on the stick, a wandering
        // line, late on the brakes); 'speedAt' (optional) the speed it holds at a station (by eye: the HUD's speed, a lag of 0.35 s); 'pics' stations
        // where it takes a picture (name, station); 'airPic' a picture 1.5 s into each spell in the air
        IEnumerator HumanDrive(List<Vector3> line, float cap, float timeout, HumanRun run, Func<bool> stop = null, Func<float, float> speedAt = null,
                               List<(string name, float s)> pics = null, string airPic = null, bool pressYWhenStuck = true)
        {
            R73Pad(); var cum = Cum(line); float s = 0f, steerCmd = 0f, t = 0f, slowFor = 0f, liftUntil = -1f, airFor = 0f, nextTrace = 0f; int pic = 0; bool airShot = false;
            float wobbleSeed = (float)r73rng.NextDouble() * 100f; var lagged = new Queue<(float t, float steer)>();
            float near = 0f; { var p0 = car.Body.position; ProjectNear(line, cum, p0, 0f, out _); near = ProjectNear(line, cum, p0, 0f, out _); s = near; }
            while (t < timeout)
            {
                yield return new WaitForFixedUpdate(); float dt = Time.fixedDeltaTime; t += dt;
                var p = car.Body.position; s = ProjectNear(line, cum, p, s, out float dev);
                float v = car.Body.linearVelocity.magnitude; run.topSpeed = Mathf.Max(run.topSpeed, v);
                if (s >= nextTrace) { nextTrace = s + 25f; run.trace.Append($" {s:0}:{v:0.0}{(car.GroundedWheels == 0 ? "a" : "")}{(Mathf.Abs(dev) > 3f ? $"d{dev:0}" : "")}"); }
                if (s >= cum[cum.Length - 1] - 3f) { run.reachedEnd = true; break; }
                if (stop != null && stop()) break;
                // in the air: hands steady, throttle held
                bool air = car.GroundedWheels == 0;
                if (air) { airFor += dt; run.maxAir = Mathf.Max(run.maxAir, airFor); if (airPic != null && !airShot && airFor > 1.5f) { airShot = true; yield return Capture(airPic); run.pics.Add(airPic); } R73Set(steerCmd * .3f, 1f, 0f); continue; }
                airFor = 0f;
                // where it looks: ahead along the line, more ahead the faster, wandering a little either side (a person's line)
                float wander = (Mathf.PerlinNoise(wobbleSeed, t * .25f) - .5f) * 3.0f;
                var a = At(line, cum, s + 7f + .55f * v); var a2 = At(line, cum, s + 8f + .55f * v);
                var side = Vector3.Cross(Vector3.up, Flat(a2 - a).normalized); var aim = a + side * wander;
                float ang = Vector3.SignedAngle(Flat(car.transform.forward), Flat(aim - p), Vector3.up);
                // the reaction lag: what it steers now is what it wanted 0.15 s ago
                lagged.Enqueue((t, Mathf.Clamp(ang / 24f, -1f, 1f)));
                float want = steerCmd; while (lagged.Count > 0 && lagged.Peek().t <= t - .15f) want = lagged.Dequeue().steer;
                steerCmd += (want - steerCmd) * Mathf.Clamp01(dt / .08f);
                // the speed: the cap, less into the bends it sees coming (late), or the held speed by eye
                var f0 = At(line, cum, s + 5f) - At(line, cum, s); var f1 = At(line, cum, s + 45f) - At(line, cum, s + 40f);
                float bend = Vector3.Angle(Flat(f0), Flat(f1)); float target = Mathf.Lerp(cap, 10f, Mathf.Clamp01(bend / 75f));
                if (speedAt != null) target = Mathf.Min(target, speedAt(s));
                if (t > liftUntil && r73rng.NextDouble() < dt * .04) liftUntil = t + .3f;   // a lift now and then
                float thr = v < target - .7f ? 1f : v < target + .3f ? .45f : 0f, brk = v > target + 4f ? .8f : 0f;
                if (t < liftUntil) thr = 0f;
                R73Set(steerCmd, thr, brk);
                // stuck: barely moving with the throttle on for 4 s: Y (the reset), as a player would
                if (v < 1.2f && thr > .5f) slowFor += dt; else slowFor = 0f;
                if (slowFor > 4f && pressYWhenStuck) { run.stuck++; run.note += $" stuck at ({p.x:0}, {p.z:0}) s {s:0};"; yield return R73Reset(); run.resets++; slowFor = 0f; }
                while (pics != null && pic < pics.Count && s >= pics[pic].s) { yield return Capture(pics[pic].name); run.pics.Add(pics[pic].name); pic++; }
            }
            R73Set(0, 0, 0); run.time = t; run.sEnd = s;
        }

        // ---------------------------------------------------------------- dan: Dan's six reports from his own spots
        IEnumerator R73Dan()
        {
            report.AppendLine(); report.AppendLine("=== Dan's six reports, from his exact position and heading (the chase camera, as he saw it)");
            var spots = new (string id, Vector3 at, float yaw, bool race, string what)[]
            {
                ("BUG-001", new Vector3(-866.04f, 0f, -5208.17f), 56.2f, true, "Kennesaw Mountain race, before the start line: the barrier across the road"),
                ("BUG-002", new Vector3(-1084.27f, 0f, -5837.29f), 48.6f, true, "Kennesaw Mountain race, the drive with CP5 next: the barrier across the road"),
                ("BUG-003", new Vector3(-933.27f, 0f, -5721.73f), 140.7f, false, "Kennesaw Mountain Drive by the summit lot: the road cut by J1's kicker"),
                ("BUG-004", new Vector3(-2024.24f, 0f, -1350.35f), 216.3f, false, "the quarry bleachers: blocks for people"),
                ("BUG-005", new Vector3(-2423.25f, 0f, -1323.20f), 115.1f, false, "the far bleachers: people buried"),
                ("BUG-006", new Vector3(-2398.76f, 0f, -1359.36f), 307.7f, false, "down in the pit under a ramp's trestle: stuck, reset did nothing"),
            };
            yield return Vehicle("drifter"); yield return Clock(13f);
            foreach (var sp in spots)
            {
                if (sp.race) { UseRoute("kennesaw_mountain.json", "forward"); RaceController.AiCount = 0; race.StartRace(); yield return new WaitForSecondsRealtime(1f); }
                else if (race.State != RaceController.Stage.Free) race.EndRace();
                var p = RaceRoute.Surface(sp.at.x, sp.at.z); respawn.PlaceAt(p + Vector3.up * .3f, sp.yaw); car.Body.linearVelocity = Vector3.zero; race.NotifyTeleported();
                var cam = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cam) cam.Snap();
                yield return new WaitForSecondsRealtime(2.5f);
                yield return Capture($"73_{sp.id}_after");
                var hits = Physics.OverlapBox(p + Quaternion.Euler(0, sp.yaw, 0) * Vector3.forward * 30f + Vector3.up * 1.2f, new Vector3(5f, .8f, 30f), Quaternion.Euler(0, sp.yaw, 0), ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => !(c is TerrainCollider) && c.attachedRigidbody != car.Body).Select(c => c.name).Distinct().ToList();
                report.AppendLine($"  {sp.id} ({sp.what}): at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}) heading {sp.yaw:0}; standing in the 60 m ahead: {(hits.Count == 0 ? "nothing" : string.Join(", ", hits.Take(8)))}; picture 73_{sp.id}_after.png");
                if (sp.race) race.EndRace();
            }
            yield return Clock(1f);
            foreach (var sp in spots.Where(x => x.id == "BUG-004" || x.id == "BUG-005"))
            {
                var p = RaceRoute.Surface(sp.at.x, sp.at.z); respawn.PlaceAt(p + Vector3.up * .3f, sp.yaw); race.NotifyTeleported();
                var cam = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cam) cam.Snap();
                yield return new WaitForSecondsRealtime(2.5f); yield return Capture($"73_{sp.id}_after_night");
            }
            yield return Clock(13f);
        }

        // ---------------------------------------------------------------- bars: nothing standing across any course's line
        static readonly (string file, string[] dirs)[] R73CarCourses =
        {
            ("chastain_glen_loop.json", new[] { "forward", "reverse" }), ("noonday_creek.json", new[] { "forward", "reverse" }), ("underground.json", new[] { "forward" }),
            ("chastain_ridge.json", new[] { "forward", "reverse" }), ("chastain_lakes.json", new[] { "forward", "reverse" }), ("addison_heights.json", new[] { "forward", "reverse" }),
            ("noonday_creek_trail.json", new[] { "forward", "reverse" }), ("kennesaw_mountain.json", new[] { "forward" }),
        };
        IEnumerator R73Bars()
        {
            report.AppendLine(); report.AppendLine("=== Every car course, each direction: the race started (its barriers up), its line and lead-in swept 0.4 to 2 m over the line, 2.5 m either side");
            yield return Vehicle("drifter"); int total = 0;
            foreach (var (file, dirs) in R73CarCourses)
                foreach (var dir in dirs)
                {
                    UseRoute(file, dir); var r = race.Route; if (r == null) { report.AppendLine($"  {file} {dir}: NOT LOADED"); continue; }
                    if (r.Data != null && !r.Data.Allows(dir)) continue;
                    RaceController.AiCount = 0; race.StartRace(); yield return new WaitForSecondsRealtime(1.2f); Physics.SyncTransforms();
                    var barriers = FindAnyObjectByType<RaceBarriers>();
                    var found = new Dictionary<string, (Vector3 at, Vector3 f, float s)>(); var other = new Dictionary<string, int>();
                    void Sweep(Vector3[] path, string which)
                    {
                        if (path == null) return; float acc = 0f;
                        for (int i = 0; i + 1 < path.Length; i++)
                        {
                            var a = path[i]; var b = path[i + 1]; var ab = b - a; float L = Flat(ab).magnitude; if (L < .01f) continue; var f = Flat(ab).normalized;
                            for (float u = 0f; u < L; u += 2f)
                            {
                                var q = a + ab * (u / L); var g = RaceRoute.Surface(q.x, q.z); if (Mathf.Abs(g.y - q.y) < 6f) q.y = g.y;
                                foreach (var c in Physics.OverlapBox(q + Vector3.up * 1.2f, new Vector3(2.5f, .8f, 1f), Quaternion.LookRotation(f), ~0, QueryTriggerInteraction.Ignore))
                                {
                                    if (c is TerrainCollider || c.attachedRigidbody == car.Body || c.attachedRigidbody != null) continue;
                                    string n = c.transform.parent ? c.transform.parent.name + "/" + c.name : c.name;
                                    bool barrier = n.IndexOf("arrier", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("losure", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Seal", StringComparison.Ordinal) >= 0 || n.IndexOf("divider", StringComparison.OrdinalIgnoreCase) >= 0;
                                    if (barrier) { if (!found.ContainsKey(n)) found[n] = (q, f, acc + u); }
                                    else other[n] = other.TryGetValue(n, out int k) ? k + 1 : 1;
                                }
                            }
                            acc += L;
                        }
                    }
                    Sweep(r.Path, "lap"); Sweep(r.LeadIn, "lead-in");
                    string bInfo = barriers ? $"{barriers.Count} barriers built, {barriers.OnCourse.Count} left out as on the line{(barriers.OnCourse.Count > 0 ? " (" + string.Join("; ", barriers.OnCourse) + ")" : "")}, {barriers.Trimmed} blocks trimmed off the line" : "no barriers";
                    report.AppendLine($"  {r.Name} {dir}: {bInfo}; on the line: {(found.Count == 0 ? "NO BARRIER" : string.Join(", ", found.Keys))}" + (other.Count > 0 ? $"; other things the sweep met (for review): {string.Join(", ", other.OrderByDescending(x => x.Value).Take(6).Select(x => $"{x.Key} x{x.Value}"))}" : ""));
                    int k2 = 0;
                    foreach (var kv in found)
                    {
                        var at = kv.Value.at - kv.Value.f * 25f + Vector3.up * 3f; yield return Still($"73_bar_{Path.GetFileNameWithoutExtension(file)}_{dir}_{k2++}", at, kv.Value.at + Vector3.up, 60f);
                    }
                    total += found.Count;
                    Check($"bars {r.Name} {dir}: no barrier on the race's line", found.Count == 0, found.Count == 0 ? "" : string.Join(", ", found.Keys));
                    race.EndRace(); yield return new WaitForSecondsRealtime(.3f);
                }
            report.AppendLine($"  barriers on any line: {total}");
        }

        // ---------------------------------------------------------------- kdrive: the drive at the summit in Free Roam, both ways
        IEnumerator R73KDrive()
        {
            report.AppendLine(); report.AppendLine("=== Kennesaw Mountain Drive in Free Roam: up past BUG-002 to the summit lot (BUG-003), then back down (the Street Classic, then the Drifter Twin)");
            var L = Lines(); if (L == null) { Check("kdrive: the lines file", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var up = R73Line(L.drive_up); var down = new List<Vector3>(up); down.Reverse();
            foreach (var vid in new[] { "original", "drifter" })
            {
                yield return Vehicle(vid);
                foreach (var (name, line) in new[] { ("up", up), ("down", down) })
                {
                    var f = Flat(line[2] - line[0]).normalized; respawn.PlaceAt(line[0] + Vector3.up * .3f, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1f);
                    var run = new HumanRun(); var cum = Cum(line);
                    float maxRoll = 0f, maxPitchStep = 0f, prevPitch = 0f; var rollAt = Vector3.zero;
                    var pics = vid == "drifter" ? new List<(string, float)> { ($"73_kdrive_{name}_1", cum[cum.Length - 1] * .3f), ($"73_kdrive_{name}_2", cum[cum.Length - 1] * .6f), ($"73_kdrive_{name}_3", cum[cum.Length - 1] * .9f) } : null;
                    var watch = StartCoroutine(R73Watch(() => { float roll = Mathf.Abs(Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(car.transform.up, car.transform.forward), car.transform.forward)); if (car.GroundedWheels >= 3 && roll > maxRoll) { maxRoll = roll; rollAt = car.Body.position; } float pt = car.transform.eulerAngles.x; maxPitchStep = Mathf.Max(maxPitchStep, Mathf.Abs(Mathf.DeltaAngle(pt, prevPitch))); prevPitch = pt; }, () => run.time > 0f));
                    yield return HumanDrive(line, 22f, 90f, run, null, null, pics);
                    StopCoroutine(watch);
                    report.AppendLine($"  {Nm(vid),-15} {name}: {(run.reachedEnd ? "reached the end" : "DID NOT reach the end")} in {run.time:0.0} s, top {run.topSpeed:0} m/s, longest in the air {run.maxAir:0.00} s, most roll on the ground {maxRoll:0.0} deg at ({rollAt.x:0}, {rollAt.z:0}), resets {run.resets}{run.note}");
                    Check($"kdrive {Nm(vid)} {name}: a normal road (to the end, no air over 0.3 s, roll under 8 degrees, no reset)", run.reachedEnd && run.maxAir < .3f && maxRoll < 8f && run.resets == 0, $"air {run.maxAir:0.00} s, roll {maxRoll:0.0}, resets {run.resets}");
                }
            }
        }
        IEnumerator R73Watch(Action each, Func<bool> started) { while (true) { yield return new WaitForFixedUpdate(); each(); } }

        // ---------------------------------------------------------------- leap: the summit leap from its fast-travel spot
        IEnumerator R73Leap()
        {
            report.AppendLine(); report.AppendLine("=== The summit leap: fast travel to its run-up, flat out (by the human-like driver), the flight, the landing; a short one; a reset after the landing");
            var L = Lines(); if (L == null) { Check("leap: the lines file", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var line = R73Line(L.leap); var lip = new Vector3(L.leap_lip[0], 0f, L.leap_lip[1]); var dir = new Vector3(L.leap_dir[0], 0f, L.leap_dir[1]).normalized;
            var cum = Cum(line); float sLip = 0f; for (int i = 0; i < line.Count; i++) if (Vector3.Dot(Flat(line[i] - lip), dir) <= 0f) sLip = cum[i];
            foreach (var (vid, thr, tag) in new[] { ("drifter", 1f, "flat out"), ("original", 1f, "flat out"), ("moto", 1f, "flat out"), ("drifter", .5f, "short (half throttle)") })
            {
                yield return Vehicle(vid); yield return R73Travel("kennesaw-summit-ramp");
                var run = new HumanRun(); bool first = vid == "drifter" && thr > .9f;
                var pics = first ? new List<(string, float)> { ("73_leap_start", 2f), ("73_leap_runup", sLip * .5f), ("73_leap_lip", sLip - 15f) } : null;
                float lipV = 0f, airT = 0f, landS = 0f; bool crossed = false; Vector3 landAt = Vector3.zero; float upAtLand = 1f; bool landed = false; float airStart = 0f;
                var mon = StartCoroutine(R73Watch(() =>
                {
                    var p = car.Body.position; float along = Vector3.Dot(Flat(p - lip), dir);
                    if (!crossed && along >= 0f) { crossed = true; lipV = car.Body.linearVelocity.magnitude; airStart = Time.time; }
                    if (crossed && !landed && car.GroundedWheels > 0 && Time.time - airStart > .5f) { landed = true; airT = Time.time - airStart; landS = along; landAt = p; upAtLand = car.transform.up.y; }
                }, () => true));
                Func<float, float> speed = thr < .9f ? (s => 22f) : (Func<float, float>)null;
                yield return HumanDrive(line, 99f, 70f, run, () => landed && car.Body.linearVelocity.magnitude < 3f, speed, pics, first ? "73_leap_midair" : null);
                yield return new WaitForSecondsRealtime(1.5f);
                StopCoroutine(mon);
                if (first) { yield return Capture("73_leap_after_landing"); }
                var end = car.Body.position;
                report.AppendLine($"      run-up (station:speed):{run.trace}");
                report.AppendLine($"  {Nm(vid),-15} {tag}: off the lip at {lipV:0.0} m/s, {airT:0.0} s in the air, came down {landS:0} m past the lip at ({landAt.x:0}, {landAt.z:0}) upright {upAtLand:0.00}; stopped at ({end.x:0}, {end.z:0}) upright {car.transform.up.y:0.00}; resets {run.resets}{run.note}");
                if (thr > .9f) Check($"leap {Nm(vid)} flat out: a long flight (7 s or more), lands upright", crossed && airT >= 7f && upAtLand > .6f && car.transform.up.y > .6f, $"{airT:0.0} s, {landS:0} m");
                // a reset after the landing (Y): goes somewhere clear, upright
                var before = car.Body.position; yield return R73Reset(); var after = car.Body.position;
                report.AppendLine($"    reset after it: from ({before.x:0}, {before.z:0}) to ({after.x:0}, {after.z:0}), {Flat(after - before).magnitude:0} m, upright {car.transform.up.y:0.00}");
                if (first) yield return Capture("73_leap_after_reset");
            }
        }
        IEnumerator R73Travel(string id)
        {
            var ex = Exploration.Instance; var d = ex ? ex.destinations.FirstOrDefault(x => x.id == id) : null;
            if (d == null) { report.AppendLine($"  (no fast-travel place {id})"); yield break; }
            ex.Find(id); bool ok = ex.Travel(d); yield return new WaitForSecondsRealtime(1.2f);
            report.AppendLine($"  fast travel to {d.title}: {(ok ? "arrived" : "FAILED: " + ex.Message)} at ({car.Body.position.x:0}, {car.Body.position.z:0})");
        }

        // ---------------------------------------------------------------- qroad: from the QUARRY JUMPS sign along the drive to each box
        IEnumerator R73QRoad()
        {
            report.AppendLine(); report.AppendLine("=== From the QUARRY JUMPS sign on the airport's service road (by The Big One's start) along the quarry jumps drive past each start box");
            var L = Lines(); var q = QuarryJumps.Instance; if (L == null || !q) { Check("qroad: the lines and the event", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            var line = R73Line(L.quarry_drive); var cum = Cum(line);
            var f = Flat(line[3] - line[0]).normalized; respawn.PlaceAt(line[0] - f * 25f + Vector3.up * .3f, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg); race.NotifyTeleported();
            yield return new WaitForSecondsRealtime(1.5f); yield return Capture("73_qroad_the_sign");
            var pics = new List<(string, float)>();
            foreach (var j in q.Jumps) { var sp = QuarryJumps.StartPoint(j); float best = 1e9f, bs = 0f; for (int i = 0; i < line.Count; i++) { float d = Flat(line[i] - sp).magnitude; if (d < best) { best = d; bs = cum[i]; } } pics.Add(($"73_qroad_passing_{j.id}", bs - 15f)); }
            pics.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            var run = new HumanRun(); yield return HumanDrive(line, 16f, 120f, run, null, null, pics);
            report.AppendLine($"  the drive: {(run.reachedEnd ? "driven end to end" : "NOT to its end")} in {run.time:0} s, resets {run.resets}{run.note}");
            Check("qroad: the quarry jumps drive drivable end to end", run.reachedEnd && run.resets == 0);
            // each start box from the box: the whole line in view
            foreach (var j in q.Jumps)
            {
                q.PutAtStart(j); yield return new WaitForSecondsRealtime(2f); var cam = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cam) cam.Snap(); yield return new WaitForSecondsRealtime(.6f);
                yield return Capture($"73_q{j.id}_from_the_box");
                var a = QuarryJumps.At(j, 4f); a.y = GroundQuery.Height(a.x, a.z) + 4f; var b = QuarryJumps.At(j, j.land_s[0]); b.y = j.land_y[0];
                yield return Still($"73_q{j.id}_line_from_the_box", a, b, 40f);
            }
        }

        // ---------------------------------------------------------------- qjump: each jump by the human-like driver
        IEnumerator R73QJump()
        {
            report.AppendLine(); report.AppendLine("=== The quarry jumps from fast travel: into the box, the countdown, the run-up held by eye at the window's middle, the jump");
            var q = QuarryJumps.Instance; if (!q) { Check("qjump: the event", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(13f);
            var tries = new List<(QuarryJumps.Jump j, string vid, string how)>();
            foreach (var j in q.Jumps) { tries.Add((j, "original", "window")); tries.Add((j, "drifter", "window")); }
            tries.Add((q.Jumps[1], "moto", "window"));
            tries.Add((q.Jumps[0], "original", "flat"));
            foreach (var (j, vid, how) in tries)
            {
                yield return Vehicle(vid); yield return R73Travel("quarry-jump-" + j.id.ToLower());
                // drive into the box (the fast-travel spot is the box): stop, face the ramp; the event's countdown
                float wait = 0f; while (q.State != QuarryJumps.Phase.Run && wait < 10f) { R73Set(0, 0, .3f); yield return new WaitForFixedUpdate(); wait += Time.fixedDeltaTime; }
                if (q.State != QuarryJumps.Phase.Run) { report.AppendLine($"  {j.id} {Nm(vid)}: the countdown did not start ({q.State}) at ({car.Body.position.x:0}, {car.Body.position.z:0})"); Check($"qjump {j.id} {Nm(vid)}: the event starts from the box", false); continue; }
                var line = new List<Vector3>(); for (float s = 6f; s <= j.runout_to; s += 3f) { var p = QuarryJumps.At(j, s); p.y = GroundQuery.Height(p.x, p.z); line.Add(p); }
                float mid = (j.window.lo + j.window.hi) / 2f; float lipS = j.lip_s - 6f;
                Func<float, float> hold = how == "window" ? (s => s < lipS - 5f ? mid + .8f : 99f) : (Func<float, float>)null;
                var run = new HumanRun(); bool pic = j.id == "J2" && vid == "drifter" && how == "window";
                var pics = pic ? new List<(string, float)> { ("73_qJ2_runup_at_speed", lipS * .6f), ("73_qJ2_at_the_lip", lipS - 12f) } : null;
                yield return HumanDrive(line, 99f, 60f, run, () => q.State == QuarryJumps.Phase.Result, hold, pics, pic ? "73_qJ2_midair" : null);
                yield return new WaitForSecondsRealtime(2f); if (pic) yield return Capture("73_qJ2_landing");
                report.AppendLine($"      run-up (station:speed, a = in the air, d = off the line):{run.trace}");
                report.AppendLine($"  {j.id} {j.name}, {Nm(vid)} {(how == "window" ? $"held by eye at {DisplayUnits.Mph(mid):0} mph" : "flat out")}: lip {q.LastLipSpeed:0.0} m/s ({DisplayUnits.Mph(q.LastLipSpeed):0} mph, window {DisplayUnits.Mph(j.window.lo):0}-{DisplayUnits.Mph(j.window.hi):0}): {q.LastLanding} {q.LastDistance:0} m, {q.LastAir:0.0} s{run.note}");
                if (how == "window") Check($"qjump {j.id} {Nm(vid)} at the window: lands clean", q.LastLanding == "CLEAN", q.LastLanding);
                yield return new WaitForSecondsRealtime(4f);
            }
        }

        // ---------------------------------------------------------------- qcrowd: the bleachers
        IEnumerator R73QCrowd()
        {
            report.AppendLine(); report.AppendLine("=== The bleachers: each stand from the front, close");
            var q = QuarryJumps.Instance; if (!q) yield break; yield return Clock(13f);
            foreach (var j in q.Jumps)
                for (int k = 0; k < (j.stands?.Length ?? 0); k++)
                {
                    var st = j.stands[k]; var c = new Vector3(st.at[0], 0f, st.at[1]); c.y = GroundQuery.Height(c.x, c.z);
                    var face = new Vector3(Mathf.Sin(st.yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(st.yaw * Mathf.Deg2Rad));
                    // (the stand faces the jump's line: look at it from the line's side)
                    var toLine = Flat(QuarryJumps.At(j, Vector3.Dot(Flat(c - new Vector3(j.origin[0], 0, j.origin[1])), QuarryJumps.Forward(j))) - c).normalized;
                    yield return Still($"73_crowd_{j.id}_{k}", c + toLine * 14f + Vector3.up * 3f, c + Vector3.up * 1.6f, 55f);
                    float gy = GroundQuery.Height(c.x, c.z); var span = new[] { -10f, 0f, 10f }.Select(u => { var w = c + Vector3.Cross(Vector3.up, toLine) * u; return GroundQuery.Height(w.x, w.z) - gy; }).ToArray();
                    report.AppendLine($"  {j.id} stand {k}: at ({c.x:0}, {c.z:0}); the ground across it {string.Join(" / ", span.Select(x => x.ToString("+0.0;-0.0")))} m (level pad)");
                }
        }

        // ---------------------------------------------------------------- qreset: the reset at BUG-006's spot and against a kicker's skirt
        IEnumerator R73QReset()
        {
            report.AppendLine(); report.AppendLine("=== The reset (Y) where Dan was stuck, and wedged against a kicker's side");
            var q = QuarryJumps.Instance; if (race.State != RaceController.Stage.Free) race.EndRace(); yield return Vehicle("drifter");
            var spots = new List<(string, Vector3, float)> { ("BUG-006's spot", new Vector3(-2398.76f, 0f, -1359.36f), 307.7f) };
            if (q) foreach (var j in q.Jumps) { var p = QuarryJumps.At(j, j.lip_s - 25f, j.half + 1.6f); spots.Add(($"against {j.name}'s kicker", p, Mathf.Atan2(-j.dir[1], j.dir[0]) * Mathf.Rad2Deg + 180f)); }
            foreach (var (name, at, yaw) in spots)
            {
                var p = RaceRoute.Surface(at.x, at.z); respawn.PlaceAt(p + Vector3.up * .3f, yaw); car.Body.linearVelocity = Vector3.zero; race.NotifyTeleported();
                yield return new WaitForSecondsRealtime(2.5f);   // it settles: a stable pose there (the old reset went back to it)
                var before = car.Body.position; yield return R73Reset(); yield return new WaitForSecondsRealtime(.5f); var after = car.Body.position;
                float moved = Flat(after - before).magnitude;
                report.AppendLine($"  {name}: from ({before.x:0}, {before.y:0}, {before.z:0}) to ({after.x:0}, {after.y:0}, {after.z:0}), {moved:0} m, upright {car.transform.up.y:0.00}");
                Check($"qreset {name}: the reset moves the vehicle somewhere else, upright", moved >= 8f && car.transform.up.y > .8f, $"{moved:0} m");
                if (name.StartsWith("BUG")) yield return Capture("73_BUG-006_after_reset");
            }
        }

        // ---------------------------------------------------------------- qtrack: the Quarry Track raced
        IEnumerator R73QTrack()
        {
            report.AppendLine(); report.AppendLine("=== The Quarry Track: 3 AI riders, 1 lap, the player on the Drifter Twin driven by the human-like driver; pictures at each checkpoint");
            yield return Clock(13f); yield return Vehicle("drifter");
            UseRoute("quarry_track.json", "forward"); var r = race.Route; if (r == null) { Check("qtrack: the route loads", false); yield break; }
            RaceController.LapsOverride = 1; RaceController.AiCount = 3; RoadDriver.Difficulty = 1; race.StartRace();
            var events = new List<string>(); void OnLog(string l) { events.Add($"{race.RaceClock,7:0.0}  {l}"); } race.Logged += OnLog;
            while (race.State == RaceController.Stage.Countdown) yield return null;
            var divider = FindObjectsByType<RaceOnly>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == "Quarry Track divider");
            report.AppendLine($"  the divider: {(divider ? (divider.Shown ? "standing" : "NOT standing") : "NOT FOUND")}");
            var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]); for (float s = 0f; s <= r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
            var pics = r.Checkpoints.Select(g => ($"73_qtrack_CP{g.Id}", (r.LeadIn != null ? r.RunInLength - r.GateS : 0f) + g.S - 30f)).ToList();
            var run = new HumanRun();
            // the run-ups held at the window's middle by eye (a player who knows the jumps), flat out elsewhere
            var q = QuarryJumps.Instance;
            Func<float, float> hold = s =>
            {
                var p = car.Body.position; if (!q) return 99f;
                foreach (var j in q.Jumps) { float al = Vector3.Dot(Flat(p - new Vector3(j.origin[0], 0, j.origin[1])), QuarryJumps.Forward(j)); float ac = Mathf.Abs(Vector3.Dot(Flat(p - new Vector3(j.origin[0], 0, j.origin[1])), new Vector3(-j.dir[1], 0, j.dir[0]))); if (ac < 8f && al > 20f && al < j.lip_s - 5f) return (j.window.lo + j.window.hi) / 2f + .8f; }
                return 99f;
            };
            Time.timeScale = 1.5f;
            var startTrace = new StringBuilder(); float t0q = Time.time;
            var tr = StartCoroutine(R73Watch(() => { if (Time.time - t0q < 40f && Mathf.Repeat(Time.time - t0q, 2f) < Time.fixedDeltaTime * 1.5f) { var pp = car.Body.position; startTrace.Append($" ({pp.x:0},{pp.z:0} {car.Body.linearVelocity.magnitude:0}{(race.Player.ClockStarted ? " clock" : "")})"); } }, () => true));
            yield return HumanDrive(line, 40f, 600f, run, () => race.Player.Finished, hold, pics, "73_qtrack_midair");
            StopCoroutine(tr); report.AppendLine($"  the player's first 40 s (x,z speed): {startTrace}; the grid at ({r.GridPosition.x:0}, {r.GridPosition.z:0}), the line at ({r.Gate.Position.x:0}, {r.Gate.Position.z:0}) facing ({r.Gate.Forward.x:0.00}, {r.Gate.Forward.z:0.00})");
            Time.timeScale = 1f;
            float wait = 0f; while (race.Entrants.Any(e => !e.Finished) && wait < 240f) { yield return new WaitForSecondsRealtime(1f); wait += 1f; }
            race.Logged -= OnLog;
            foreach (var e in events.Where(x => x.Contains("CHECKPOINT") || x.Contains("PENALTY") || x.Contains("FINISH") || x.Contains("LAP"))) report.AppendLine("  " + e);
            foreach (var e in race.Entrants) report.AppendLine($"  {e.Name,-6} {e.VehicleName,-18} {(e.Finished ? RaceController.Fmt(e.FinalTotal) + (e.Estimated ? " (estimated)" : "") : "NOT finished")}, missed {e.MissedTotal}");
            report.AppendLine($"  the player's drive: resets {run.resets}{run.note}");
            Check("qtrack: the player finishes the lap", race.Player.Finished, $"next CP {race.Player.NextCheckpoint + 1}");
            Check("qtrack: every AI rider finishes (not estimated)", race.Entrants.All(e => e.Finished && !e.Estimated));
            race.EndRace();
        }

        // ---------------------------------------------------------------- airpilot: the air race flown by the arrow
        IEnumerator R73AirPilot()
        {
            report.AppendLine(); report.AppendLine("=== The Quarry Air Race flown by the human-like pilot, by the arrow alone (race.Target, what the arrow points at): ring 3 missed on purpose, then gone back for");
            yield return Clock(13f);
            RaceController.RouteOverride = R71File; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 1; RaceController.AiCount = 0;   // Round 74: -r71Route (the Highway Air Race)
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace(); R73Pad();
            var pl = car.GetComponent<PlaneFlight>(); if (!pl) { Check("airpilot: the plane", false); yield break; }
            var events = new List<string>(); void OnLog(string l) { events.Add($"{race.RaceClock,7:0.0}  {l}"); } race.Logged += OnLog;
            while (race.State == RaceController.Stage.Countdown) { R73Set(0, 0, 0); yield return null; }
            var p = race.Player; float t = 0f; int phase = 0; float bankCmd = 0f, pitchCmd = 0f; var seen = new HashSet<int>(); bool backFor = false, missSaid = false, wentBack = false; int crashes0 = pl.Crashes;
            float seed = (float)r73rng.NextDouble() * 50f;
            Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            while (t < 600f && !p.Finished)
            {
                yield return new WaitForFixedUpdate(); float dt = Time.fixedDeltaTime; t += dt;
                var tgt = race.Target; if (tgt == null) continue;
                Vector3 aimAt = tgt.Position;
                // ring 3 (index 2): aim 90 m to its side on the way in; once it is flown past (RING MISSED), turn back for it
                if (p.ClockStarted && p.NextCheckpoint == 2 && !backFor) aimAt = race.Route.Checkpoints[2].Position + race.Route.Checkpoints[2].Right * 90f;
                if (p.RingSkipped == 2 && !backFor) { backFor = true; missSaid = true; yield return Capture("73_air_ring3_missed"); }
                if (backFor && p.NextCheckpoint == 2) aimAt = race.Route.Checkpoints[2].Position;
                if (backFor && p.NextCheckpoint > 2 && !wentBack) { wentBack = true; yield return Capture("73_air_ring3_gone_back_for"); }
                // a picture as each ring comes up (400 m out): the arrow and the ring
                int idx = tgt.Id; if (!seen.Contains(idx) && (tgt.Position - car.Body.position).magnitude < 450f) { seen.Add(idx); yield return Capture($"73_air_ring_{(idx == 0 ? "line" : idx.ToString())}_ahead"); }
                // the pilot: banks toward the aim (late, a little wobble), pitches toward its height, throttle full
                var to = aimAt - car.Body.position; var fw = car.transform.forward;
                float yawErr = Vector3.SignedAngle(Flat(fw), Flat(to), Vector3.up) + (Mathf.PerlinNoise(seed, t * .3f) - .5f) * 6f;
                // a person flies over the woods to a ring far off, and comes down to it once near: the height it holds is at least 55 m over the
                // highest ground in the next 600 m toward the aim (the trees stand up to 45 m), until 600 m from the ring
                if (Flat(to).magnitude > 600f)
                {
                    float hiGround = float.MinValue; var dirF = Flat(to).normalized;
                    for (float k = 0f; k <= 600f; k += 50f) { var w = car.Body.position + dirF * k; float gh = GroundQuery.Height(w.x, w.z); if (!float.IsNaN(gh)) hiGround = Mathf.Max(hiGround, gh); }
                    float floorY = hiGround + 55f; if (aimAt.y < floorY) { aimAt.y = floorY; to = aimAt - car.Body.position; }
                }
                float climb = Mathf.Atan2(to.y, Flat(to).magnitude) * Mathf.Rad2Deg, pitchNow = pl.PitchDeg;   // (+ nose up)
                float bankWant = Mathf.Clamp(yawErr * 2.0f, -65f, 65f), roll = pl.RollDeg;                        // (+ banked right)
                bankCmd += (Mathf.Clamp((bankWant - roll) / 18f, -1f, 1f) - bankCmd) * Mathf.Clamp01(dt / .2f);
                float pitchWant = Mathf.Clamp(climb, -28f, 28f); pitchCmd += (Mathf.Clamp((pitchWant - pitchNow) / 10f, -1f, 1f) - pitchCmd) * Mathf.Clamp01(dt / .2f);
                if (!pl.Airborne) { bankCmd = Mathf.Clamp(yawErr / 30f, -1f, 1f) * .4f; pitchCmd = pl.Airspeed > 32f ? .7f : 0f; }
                R73Set(bankCmd, 1f, 0f, -pitchCmd);   // (the stick: x banks right, pulled back (-y) noses up, as PlaneFlight reads a pad)
            }
            Time.timeScale = 1f; race.Logged -= OnLog; R73Set(0, 0, 0);
            foreach (var e in events.Where(x => x.Contains("CHECKPOINT") || x.Contains("PENALTY") || x.Contains("RING") || x.Contains("CRASH") || x.Contains("LAP") || x.Contains("FINISH"))) report.AppendLine("  " + e);
            report.AppendLine($"  {(p.Finished ? "finished the lap" : "DID NOT finish")} in {t:0} s of game time; missed {p.MissedTotal}, penalty {p.PenaltyTotal:0} s; crashes {pl.Crashes - crashes0}; ring 3 missed shown: {missSaid}, gone back for: {wentBack}");
            Check("airpilot: the lap flown by the arrow alone, finished", p.Finished, $"next ring {p.NextCheckpoint + 1}");
            Check("airpilot: ring 3 flown past outside it: RING MISSED, then gone back for and taken (no penalty)", missSaid && wentBack && p.PenaltyTotal < 1f, $"penalty {p.PenaltyTotal:0} s");
            race.EndRace();
        }

        // ---------------------------------------------------------------- races: one AI race per car course
        IEnumerator R73Races()
        {
            report.AppendLine(); report.AppendLine("=== One race per car course: 3 AI, 1 lap, Normal; the player driven by the human-like driver; every racer finishes");
            yield return Clock(13f);
            var only73 = R70Arg("-r73Courses", "");   // Round 74: -r73Courses kennesaw_mountain.json,... (some courses only)
            foreach (var (file, dirs) in R73CarCourses)
            {
                if (only73 != "" && !only73.Split(',').Contains(file)) continue;
                string dir = dirs[0];
                UseRoute(file, dir); var r = race.Route; if (r == null) { report.AppendLine($"  {file}: NOT LOADED"); continue; }
                string vid = r.Data != null && r.Data.vehicles != null && r.Data.vehicles.Length > 0 ? r.Data.vehicles[0] : "drifter";
                yield return Vehicle(vid);
                RaceController.LapsOverride = 1; RaceController.AiCount = 3; RoadDriver.Difficulty = 1; race.StartRace();
                while (race.State == RaceController.Stage.Countdown) yield return null;
                var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]); for (float s = 0f; s <= r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                var run = new HumanRun(); Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                var q = QuarryJumps.Instance;
                Func<float, float> hold = file == "quarry_track.json" && q ? (s =>
                {
                    var p = car.Body.position;
                    foreach (var j in q.Jumps) { float al = Vector3.Dot(Flat(p - new Vector3(j.origin[0], 0, j.origin[1])), QuarryJumps.Forward(j)); float ac = Mathf.Abs(Vector3.Dot(Flat(p - new Vector3(j.origin[0], 0, j.origin[1])), new Vector3(-j.dir[1], 0, j.dir[0]))); if (ac < 8f && al > 20f && al < j.lip_s - 5f) return (j.window.lo + j.window.hi) / 2f + .8f; }
                    return 99f;
                }) : (Func<float, float>)null;
                yield return HumanDrive(line, 40f, 420f, run, () => race.Player.Finished, hold);
                float wait = 0f; while (race.Entrants.Any(e => !e.Finished) && wait < 200f) { yield return new WaitForSecondsRealtime(.5f); wait += 1f; }
                Time.timeScale = 1f;
                report.AppendLine($"  {r.Name} {dir} ({Nm(vid)}): " + string.Join("; ", race.Entrants.Select(e => $"{e.Name} {(e.Finished ? RaceController.Fmt(e.FinalTotal) + (e.Estimated ? " est." : "") : "NOT finished")}")) + $"; the player's resets {run.resets}{run.note}");
                Check($"races {r.Name}: every racer finishes", race.Entrants.All(e => e.Finished && !e.Estimated), string.Join(", ", race.Entrants.Where(e => !e.Finished || e.Estimated).Select(e => e.Name)));
                race.EndRace(); yield return new WaitForSecondsRealtime(.5f);
            }
        }
    }
}
