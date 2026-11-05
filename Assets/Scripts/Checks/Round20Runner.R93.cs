using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 93 in the exe (-exeRound93 <dir> [-r93Only a93dan,a93hops,b93plane,c93uturn] [-r93Courses viking_quarry.json,wings_and_wheels.json]
    // [-r93Vehicles moto,original] [-r93Modes normal,high] [-r93Laps 3] [-r93Planes plane,dart] -r20Minutes 240 -logFile <dir>\Player.log), from the repo
    // root, on the check's own save. CHECK IT AS DAN PLAYS IT: the human-like driver / pilot on the game's input.
    //   a93dan    A: Dan's spot on the Viking Quarry race (lap 1820 to 1840 m, after The Big One's run-out), the chase camera and the driver's seat
    //   a93hops   A, C1: the bump log of each course: the human-like driver, solo, 3 laps (Wings & Wheels: lap 2 through THE TUBE) on the bike and a car,
    //             normal and HIGH: every spell with no wheel down over 0.3 s that is not a designed jump (the quarry's three), and every hard jolt on the
    //             ground; then a picture from 30 m before each spot
    //   b93plane  B: each plane flown by the virtual controller: full stick right / left with no shoulder button (the bank held at its limit, no roll
    //             past it, the turn rate), then with LB / RB held a barrel roll each way and an inside loop, and level again after letting go
    //   c93uturn  C2: Wings & Wheels' U-turn: pictures from 120, 60 and 25 m before its checkpoint, then the human-like driver's lap: the U-turn's
    //             checkpoint not missed
    public sealed partial class Round20Runner
    {
        string R93Arg(string name, string def) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : def; }
        GamepadButton? r93Shoulder;   // held with R73Set while set

        // ---------------------------------------------------------------- A: Dan's spot
        IEnumerator R93Dan()
        {
            report.AppendLine(); report.AppendLine("=== A: Dan's spot on the Viking Quarry race (after The Big One's run-out; his photo: CHECKPOINT 5 340 m ahead)");
            yield return Clock(13f); yield return Vehicle("moto");
            yield return StartRoute("viking_quarry.json", "forward", 0, 1);
            var r = race.Route; if (r == null) { Check("A: the Viking Quarry race loads", false); yield break; }
            var views = FindAnyObjectByType<CameraViews>();
            foreach (float s in new[] { 1800f, 1820f, 1840f, 1880f })
            {
                var p = r.PointAt(r.GateS + s, out var f); Place(RaceRoute.Surface(p.x, p.z), f, 0f); yield return new WaitForSecondsRealtime(.4f);
                if (views) views.SetView(CameraViews.View.Chase); var cc = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cc) cc.Snap();
                yield return new WaitForSecondsRealtime(.4f); yield return Capture($"93_dan_{s:0000}_chase");
                if (views) views.SetView(CameraViews.View.FirstPerson); yield return new WaitForSecondsRealtime(.3f); yield return Capture($"93_dan_{s:0000}_seat");
                if (views) views.SetView(CameraViews.View.Chase);
                report.AppendLine($"  lap {s:0} m at ({p.x:0.0}, {p.z:0.0}), heading {Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg:0}: pictures 93_dan_{s:0000}_chase / _seat");
            }
            race.EndRace();
        }

        // ---------------------------------------------------------------- A, C1: hops and jolts on each course
        sealed class R93Hop { public string kind; public float s, dur, v, jolt; public Vector3 at; public int lap; public bool designed; public string why = ""; }
        IEnumerator R93Hops()
        {
            var courses = R93Arg("-r93Courses", "viking_quarry.json,wings_and_wheels.json").Split(',');
            var vehicles = R93Arg("-r93Vehicles", "moto,original").Split(','); var modes = R93Arg("-r93Modes", "normal,high").Split(','); int laps = int.Parse(R93Arg("-r93Laps", "3"));
            report.AppendLine(); report.AppendLine($"=== A / C1: the bump log, the human-like driver solo, {laps} laps, {string.Join(", ", vehicles.Select(R90Nm))}, {string.Join(" and ", modes)}: hops over 0.3 s (designed jumps apart) and hard jolts");
            yield return Clock(13f);
            foreach (var course in courses)
                foreach (var mode in modes)
                    foreach (var vid in vehicles)
                    {
                        bool high = mode == "high"; R84High(high); yield return Vehicle(vid);
                        RoadDriver.Difficulty = 1; yield return StartRoute(course, "forward", 0, laps);
                        var r = race.Route; if (r == null) { Check($"hops: {course} loads", false); continue; }
                        bool quarry = course.StartsWith("viking"); var jumps = quarry ? R90Jumps() : new List<(QuarryJumps.Jump j, float lipS)>();
                        var sc = r.Shortcuts.FirstOrDefault();
                        var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]);
                        for (int k = 0; k < laps; k++)
                        {
                            if (!quarry && k == 1 && sc != null)
                            {
                                float sOff = r.LapStationOf(sc.Start), sOn = r.LapStationOf(sc.End);
                                for (float s = 0f; s < sOff; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                                line.AddRange(sc.Path);
                                for (float s = sOn + 3f; s < r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                            }
                            else for (float s = 0f; s < r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
                        }
                        line.Add(r.PointAt(r.GateS, out _)); line.Add(r.PointAt(r.GateS + 30f, out _));
                        var hops = new List<R93Hop>(); float air = 0f, nextJolt = 0f; Vector3 off = default; float offS = 0f, offV = 0f;
                        var vys = new Queue<float>(); int lr0 = RaceController.LakeResets;
                        var run = new HumanRun();
                        bool Designed(Vector3 p, out string why)
                        {
                            why = "";
                            foreach (var (j, _) in jumps) { float al = R90Along(j, p, out float lat); if (Mathf.Abs(lat) < 16f && al > j.lip_s - 12f && al < j.land_s[j.land_s.Length - 1] + 40f) { why = j.id + " " + j.name; return true; } }
                            if (DrainPipe.InsideAny(p)) { why = "in the tube"; return true; }
                            foreach (var lid in FindObjectsByType<ManholeLid>(FindObjectsSortMode.None)) if (Flat(lid.transform.position - p).magnitude < 14f) { why = "a manhole's pop-out"; return true; }
                            return false;
                        }
                        yield return HumanDrive(line, 60f, 2400f, run, () =>
                        {
                            var p = car.Body.position; float dt = Time.fixedDeltaTime;
                            if (car.GroundedWheels == 0) { if (air == 0f) { off = p; offS = r.LapStationOf(p); offV = car.Body.linearVelocity.magnitude; } air += dt; }
                            else
                            {
                                if (air > .3f) { var h = new R93Hop { kind = "hop", s = offS, dur = air, v = offV, at = off, lap = race.Player.CompletedLaps + 1 }; h.designed = Designed(off, out h.why); hops.Add(h); }
                                air = 0f;
                            }
                            // a hard jolt on the ground: the vertical speed changing by 3 m/s or more within 0.1 s at speed (the bump log's own measure is 2 m/s)
                            vys.Enqueue(car.Body.linearVelocity.y); while (vys.Count > 5) vys.Dequeue();
                            if (car.GroundedWheels >= 2 && vys.Count == 5 && Time.time > nextJolt && car.Body.linearVelocity.magnitude > 10f)
                            {
                                float dv = Mathf.Abs(vys.Last() - vys.First());
                                if (dv >= 3f) { var h = new R93Hop { kind = "jolt", s = r.LapStationOf(p), jolt = dv, v = car.Body.linearVelocity.magnitude, at = p, lap = race.Player.CompletedLaps + 1 }; h.designed = Designed(p, out h.why); hops.Add(h); nextJolt = Time.time + .8f; }
                            }
                            if (run.resets > 10) return true;
                            return race.Player.Finished;
                        }, quarry ? (Func<float, float>)(s => R90Hold(car.Body.position)) : null);
                        string tag = $"{course.Replace(".json", "")} {(high ? "HIGH" : "normal")} {R90Nm(vid)}";
                        var bad = hops.Where(h => !h.designed).ToList();
                        report.AppendLine($"  {tag}: {(race.Player.Finished ? "finished" : "NOT finished")}, laps {string.Join(" / ", race.Player.LapTimes.Select(v => RaceController.Fmt(v)))}, resets {run.resets}, lake {RaceController.LakeResets - lr0}, stuck {run.stuck}{run.note}");
                        foreach (var h in hops) report.AppendLine($"    lap {h.lap} {h.kind} at lap {h.s:0} m ({h.at.x:0.0}, {h.at.y:0.0}, {h.at.z:0.0}) {(h.kind == "hop" ? $"{h.dur:0.00} s off the ground" : $"{h.jolt:0.0} m/s within 0.1 s")} at {h.v:0} m/s{(h.designed ? " (designed: " + h.why + ")" : "")}");
                        Check($"A/C1 {tag}: no unwanted hop over 0.3 s, no hard jolt", bad.Count == 0 && race.Player.Finished, $"{bad.Count(h => h.kind == "hop")} hops, {bad.Count(h => h.kind == "jolt")} jolts");
                        r93Spots.AddRange(bad.Select(h => (course, h.s, h.at)));
                        Flush(); race.EndRace(); R84High(false); yield return new WaitForSecondsRealtime(.5f);
                    }
            // a picture from 30 m before each spot (one per 20 m of lap)
            var done = new List<(string, float)>();
            foreach (var g in r93Spots.GroupBy(x => x.course))
            {
                yield return Vehicle("original"); yield return StartRoute(g.Key, "forward", 0, 1); var r = race.Route; if (r == null) continue;
                foreach (var sp in g.OrderBy(x => x.s))
                {
                    if (done.Any(d => d.Item1 == g.Key && Mathf.Abs(d.Item2 - sp.s) < 20f) || done.Count > 40) continue; done.Add((g.Key, sp.s));
                    var p = r.PointAt(r.GateS + sp.s - 30f, out var f); Place(RaceRoute.Surface(p.x, p.z), f, 0f); yield return new WaitForSecondsRealtime(.3f);
                    var cc = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cc) cc.Snap(); yield return new WaitForSecondsRealtime(.3f);
                    yield return Capture($"93_spot_{g.Key.Substring(0, 4)}_{sp.s:0000}");
                }
                race.EndRace();
            }
            report.AppendLine($"  spots pictured: {string.Join(", ", done.Select(d => d.Item1.Substring(0, 4) + " " + d.Item2.ToString("0")))}");
        }
        readonly List<(string course, float s, Vector3 at)> r93Spots = new List<(string, float, Vector3)>();

        // ---------------------------------------------------------------- B: the planes
        IEnumerator R93Plane()
        {
            report.AppendLine(); report.AppendLine("=== B: the planes on the virtual controller: the stick alone turns (bank held at its limit), LB / RB held rolls and loops");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(13f);
            foreach (var id in R93Arg("-r93Planes", "plane,dart").Split(','))
            {
                yield return Vehicle(id); yield return new WaitForSecondsRealtime(.6f);
                var pl = car.GetComponent<PlaneFlight>(); if (!pl || !pl.enabled) { Check($"B {id}: the plane flies", false, "no PlaneFlight"); continue; }
                R73Pad();
                var start = new Vector3(-1200f, 0f, -3200f); start.y = GroundQuery.Height(start.x, start.z); if (float.IsNaN(start.y)) start.y = 300f; start.y += 450f;
                pl.AirStart(start, 90f, 0f, 85f); yield return new WaitForSecondsRealtime(1.5f);
                int c0 = pl.Crashes, k = 0;
                IEnumerator Fly(float steer, float pitch, GamepadButton? sh, float sec, Func<bool> done, string pic, Action<float> each = null)
                {
                    r93Shoulder = sh; float t = 0f, next = 0f;
                    while (t < sec && (done == null || !done())) { R73Set(steer, 1f, 0f, pitch); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; each?.Invoke(t); if (pic != null && t >= next) { next += 1f; yield return Capture($"93_{id}_{pic}_{k++:00}"); } }
                    r93Shoulder = null;
                }
                // 1-2: the stick alone, full right then full left, 6 s each; then let go 3 s
                bool turnsOk = true; string turnTxt = "";
                foreach (float dir in new[] { 1f, -1f })
                {
                    float most = 0f; float y0 = pl.YawDeg; bool free = false; int r0 = pl.Rolls;
                    yield return Fly(dir, 0f, null, 6f, null, dir > 0 ? "turn_right" : "turn_left", t => { most = Mathf.Max(most, Mathf.Abs(pl.RollDeg)); free |= pl.FreeFlight; });
                    float yawRate = Mathf.Abs(Mathf.DeltaAngle(y0, pl.YawDeg)) / 6f;   // (under 180 degrees in 6 s: see the rate below)
                    yield return Fly(0f, 0f, null, 3f, null, null); float after = Mathf.Abs(pl.RollDeg);
                    bool ok = most <= pl.Bank + .5f && !free && pl.Rolls == r0 && after < 5f; turnsOk &= ok;
                    turnTxt += $" {(dir > 0 ? "right" : "left")}: bank at most {most:0} deg (limit {pl.Bank:0}), about {yawRate:0} deg/s, {(free ? "FREE FLIGHT came on" : "no roll past it")}, wings {after:0} deg 3 s after letting go;";
                }
                // 3-4: LB held: a barrel roll right; RB held: one left; let go 5 s
                int rr0 = pl.Rolls; yield return Fly(1f, 0f, GamepadButton.LeftShoulder, 8f, () => pl.Rolls > rr0, "roll_right"); bool rr = pl.Rolls > rr0;
                yield return Fly(0f, 0f, null, 5f, null, null); bool lvl1 = !pl.FreeFlight && Mathf.Abs(pl.RollDeg) < 12f;
                int rl0 = pl.Rolls; yield return Fly(-1f, 0f, GamepadButton.RightShoulder, 8f, () => pl.Rolls > rl0, "roll_left"); bool rl = pl.Rolls > rl0;
                yield return Fly(0f, 0f, null, 5f, null, null); bool lvl2 = !pl.FreeFlight && Mathf.Abs(pl.RollDeg) < 12f;
                // 5: RB held: an inside loop (the stick back); let go 5 s
                int l0 = pl.Loops; float yl = car.Body.position.y, hl = pl.YawDeg;
                yield return Fly(0f, -1f, GamepadButton.RightShoulder, 22f, () => pl.Loops > l0, "loop");
                bool lp = pl.Loops > l0; float dy = car.Body.position.y - yl, dh = Mathf.Abs(Mathf.DeltaAngle(hl, pl.YawDeg));
                yield return Fly(0f, 0f, null, 5f, null, null); bool lvl3 = !pl.FreeFlight && Mathf.Abs(pl.RollDeg) < 12f && Mathf.Abs(pl.PitchDeg) < 20f;
                bool flying = pl.Airborne && !pl.Crashed && pl.Crashes == c0;
                yield return Capture($"93_{id}_after");
                report.AppendLine($"  {id}: no shoulder:{turnTxt}");
                report.AppendLine($"  {id}: LB + stick right: barrel roll {(rr ? "yes" : "NO")}, level 5 s after letting go {(lvl1 ? "yes" : "NO")}; RB + stick left: {(rl ? "yes" : "NO")}, level {(lvl2 ? "yes" : "NO")}; RB + stick back: loop {(lp ? "yes" : "NO")} ({dy:+0;-0} m, heading {dh:0} deg off), level after {(lvl3 ? "yes" : "NO")}; {(flying ? "flying" : "NOT flying (" + pl.CrashReason + ")")}");
                Check($"B {id}: without a shoulder button the stick turns it with the bank held at its limit (no roll past it), level after letting go", turnsOk, turnTxt);
                Check($"B {id}: with LB / RB a barrel roll each way and an inside loop, level again after letting go, still flying", rr && rl && lp && lvl1 && lvl2 && lvl3 && flying, $"rolls {rr}/{rl}, loop {lp}, level {lvl1}/{lvl2}/{lvl3}, crashes {pl.Crashes - c0}");
                R73Set(0, 0, 0);
            }
            yield return Vehicle("drifter");
        }

        // ---------------------------------------------------------------- C2: the U-turn
        IEnumerator R93UTurn()
        {
            report.AppendLine(); report.AppendLine("=== C2: Wings & Wheels' U-turn (Tower Road's west end round by North Avenue)");
            yield return Clock(13f); yield return Vehicle("original");
            yield return StartRoute("wings_and_wheels.json", "forward", 0, 1);
            var r = race.Route; if (r == null) { Check("C2: the car's lap loads", false); yield break; }
            int cu = r.Checkpoints.FindIndex(c => c.Label != null && c.Label.Contains("U-turn"));
            if (cu < 0) { Check("C2: a U-turn checkpoint", false); yield break; }
            float su = r.Checkpoints[cu].S;
            var cc = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null;
            foreach (float back in new[] { 120f, 60f, 25f })
            {
                var p = r.PointAt(r.GateS + su - back, out var f); Place(RaceRoute.Surface(p.x, p.z), f, 0f); yield return new WaitForSecondsRealtime(.4f); if (cc) cc.Snap(); yield return new WaitForSecondsRealtime(.4f);
                yield return Capture($"93_uturn_{back:000}m");
            }
            race.EndRace();
            // the human-like driver's lap from the grid
            yield return StartRoute("wings_and_wheels.json", "forward", 0, 1);
            r = race.Route; var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]);
            for (float s = 0f; s < r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
            line.Add(r.PointAt(r.GateS + 30f, out _));
            var run = new HumanRun();
            yield return HumanDrive(line, 45f, 600f, run, () => race.Player.Finished);
            var pe = race.Player; bool hit = pe.MissedTotal == 0;
            report.AppendLine($"  CP{cu + 1} '{r.Checkpoints[cu].Label}' at lap {su:0} m; the driver's lap {(pe.Finished ? RaceController.Fmt(pe.LapTimes[0]) : "NOT finished")}, missed {pe.MissedTotal}, resets {run.resets}, stuck {run.stuck}{run.note}; pictures 93_uturn_*");
            Check("C2: the human-like driver takes the U-turn without missing its checkpoint", pe.Finished && hit, $"missed {pe.MissedTotal}");
            race.EndRace();
        }
    }
}
