using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 57 in the exe (-exeRound57 <dir> [-r57Only corner,complete,m5c,m5cai,hub,cpics57] [-traffic Normal] -logFile <dir>\Player.log):
    //   corner    A: Embry Lane onto Suholden Circle (Dan's BUG-001) flat out, the lap's way both directions (Drifter Twin, Street Classic): the
    //             fastest pitch and roll rates, the most the body's vertical speed jumps in a step, wheels off the ground
    //   complete  B: the player finishes with the AI still racing; a virtual pad's A presses COMPLETE RACE (then a second race: the keyboard's Space)
    //   m5c       C: M5c from J15 to J14 (the race's way) and back, flat out, every vehicle: through, no stop
    //   m5cai     C: the Underground race, 3 AI, the player parked: every AI finishes; their recoveries in M5c
    //   hub       D: at the hub's mid ledge, into rail R1, the closed gate SG1 and rail R2 flat out (90, 45 and 20 degrees): none gets through or over
    //   cpics57   Dan's spots: the corner, M5c at his spot both ways, the hub's ledge
    public sealed partial class Round20Runner
    {
        // ------------------------------------------------------------ A: the Embry / Suholden corner
        IEnumerator R57Corner()
        {
            report.AppendLine(); report.AppendLine("=== A. Embry Lane onto Suholden Circle (Dan's BUG-001 at x 3279.17 z 308.15): flat out on the lap's line, both ways");
            yield return StartRoute("addison_heights.json", "forward", 0);
            float sc = AbsStation(new Vector3(3281f, 0f, 303f));
            var fwd = LapSlice(sc - 70f, sc + 45f);
            race.EndRace();
            foreach (var (label, line) in new[] { ("forward (down Embry, right onto Suholden)", fwd), ("reverse (Suholden, left up Embry)", Enumerable.Reverse(fwd).ToList()) })
                foreach (var vid in new[] { "drifter", "original" })
                {
                    yield return Vehicle(vid);
                    Place(line[0], line[2] - line[0], 18f); yield return new WaitForFixedUpdate();
                    var run = new Run(); var lastRot = car.Body.rotation; float lastVy = car.Body.linearVelocity.y, pitch = 0f, roll = 0f, jump = 0f, vAt = 0f; Vector3 pAt = Vector3.zero, jAt = Vector3.zero; int minW = 4;
                    yield return DriveLine(line, 40f, 15f, run, null, r =>
                    {
                        var p = car.Body.position; var q = car.Body.rotation; var d = Quaternion.Inverse(lastRot) * q; lastRot = q;
                        var e = d.eulerAngles; float px = Mathf.Abs(Mathf.DeltaAngle(0f, e.x)) / Time.fixedDeltaTime, rz = Mathf.Abs(Mathf.DeltaAngle(0f, e.z)) / Time.fixedDeltaTime;
                        float vy = car.Body.linearVelocity.y, dvy = Mathf.Abs(vy - lastVy); lastVy = vy;
                        if ((new Vector2(p.x, p.z) - new Vector2(3279f, 305f)).magnitude < 22f)
                        {
                            if (px > pitch) { pitch = px; pAt = p; vAt = Flat(car.Body.linearVelocity).magnitude; }
                            roll = Mathf.Max(roll, rz); if (dvy > jump) { jump = dvy; jAt = p; } minW = Mathf.Min(minW, car.GroundedWheels);
                        }
                        return false;
                    });
                    report.AppendLine($"  {label}, {Nm(vid),-16} fastest pitch {pitch:0} deg/s at ({pAt.x:0.0}, {pAt.z:0.0}) at {vAt:0.0} m/s; fastest roll {roll:0} deg/s; the biggest step in vertical speed {jump:0.00} m/s at ({jAt.x:0.0}, {jAt.z:0.0}); fewest wheels down {minW}; {(run.reached ? "through" : run.end)}");
                    Check($"A {label} {Nm(vid)}: rides through flat (all wheels down, no jolt over 1 m/s in a step)", run.reached && minW >= 3 && jump < 1f, $"wheels {minW}, step {jump:0.00} m/s, pitch {pitch:0} deg/s");
                }
            yield return Still("57_A_corner_from_embry", RaceRoute.Surface(3276f, 330f) + Vector3.up * 2.2f, RaceRoute.Surface(3279f, 304f) + Vector3.up * 0.5f);
            yield return Still("57_A_corner_from_suholden", RaceRoute.Surface(3252f, 303f) + Vector3.up * 2.2f, RaceRoute.Surface(3280f, 306f) + Vector3.up * 0.5f);
        }

        // ------------------------------------------------------------ B: COMPLETE RACE with the pad's A and with Space
        IEnumerator R57Complete()
        {
            report.AppendLine(); report.AppendLine("=== B. COMPLETE RACE pressed (Addison Heights, 1 lap, 3 AI): the pad's A, then the keyboard's Space");
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            if (pad == null) pad = InputSystem.AddDevice<Gamepad>("Round57 virtual pad");
            var menus = GameMenus.Create(); yield return new WaitForSecondsRealtime(1.5f); if (menus) menus.CloseForCheck();
            foreach (var how in new[] { "pad A", "keyboard Space" })
            {
                yield return StartRoute("addison_heights.json", "forward", 3, 1);
                var p = race.Player; var r = race.Route;
                yield return new WaitForSecondsRealtime(1.5f);
                yield return AcrossLine(r.Gate, 0f);
                foreach (var cp in r.Checkpoints) yield return AcrossLine(cp, 0f);
                p.LapAway = true;
                yield return AcrossLine(r.Gate, 0f);
                car.Body.linearVelocity = Vector3.zero;
                float until = Time.realtimeSinceStartup + 12f; while (WinnerShot.Active && Time.realtimeSinceStartup < until) yield return null;
                yield return new WaitForSecondsRealtime(1f);
                Check($"B ({how}): the player has finished, the AI racing, the button shows", p.Finished && race.CanSimulateRemaining && menus && menus.CompleteButtonShowing, $"finished {p.Finished}, can {race.CanSimulateRemaining}, button {(menus && menus.CompleteButtonShowing)}");
                if (how == "pad A") yield return Press(GamepadButton.South);
                else
                {
                    var kb = Keyboard.current;
                    if (kb != null) { InputSystem.QueueStateEvent(kb, new KeyboardState(Key.Space)); yield return null; yield return null; InputSystem.QueueStateEvent(kb, new KeyboardState()); }
                    yield return new WaitForSecondsRealtime(0.3f);
                }
                yield return new WaitForSecondsRealtime(0.7f);
                var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
                Check($"B ({how}): pressed, every AI is classified (estimated), the button gone", ai.All(e => e.Finished) && !race.CanSimulateRemaining && !menus.CompleteButtonShowing,
                      string.Join("; ", ai.Select(e => $"{e.Name} {(e.Finished ? RaceController.Fmt(e.FinalTotal) + (e.Estimated ? " (estimated)" : "") : "racing")}")));
                yield return Capture("57_B_after_" + (how == "pad A" ? "pad" : "space"));
                race.EndRace(); RaceController.DirectionOverride = null; RaceController.LapsOverride = 0;
            }
            if (menus) Destroy(menus.gameObject);
        }

        // ------------------------------------------------------------ C: M5c both ways, every vehicle
        static readonly Vector3 M57Spot = new Vector3(1642.07f, 0f, 950.60f);
        IEnumerator R57M5c()
        {
            report.AppendLine(); report.AppendLine("=== C. M5c (Dan's BUG-003 / BUG-004 at x 1642.07 z 950.60): the race's line from 70 m before his spot to 70 m past it, flat out, both ways, every vehicle");
            yield return StartRoute("underground.json", "forward", 0);
            float s0 = AbsStation(M57Spot);
            var line = LapSlice(s0 - 70f, s0 + 70f, 1f);
            report.AppendLine($"  the line: from ({line[0].x:0}, {line[0].y:0.0}, {line[0].z:0}) to ({line[line.Count - 1].x:0}, {line[line.Count - 1].y:0.0}, {line[line.Count - 1].z:0}), {line.Count} points");
            foreach (var vid in VehicleProfile.All.Select(v => v.Id))
                foreach (var back in new[] { false, true })
                {
                    var l = back ? Enumerable.Reverse(line).ToList() : line;
                    yield return Vehicle(vid);
                    Place(l[0], l[2] - l[0], 15f); yield return new WaitForFixedUpdate();
                    var run = new Run();
                    yield return DriveLine(l, 40f, 25f, run, M57Spot);
                    report.AppendLine($"  {Nm(vid),-16} {(back ? "J14 to J15 (against the race)" : "J15 to J14 (the race's way)")}: {(run.reached ? $"through in {run.time:0.0} s" : "NOT through (" + run.end + $") at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}, at Dan's spot {run.speedAt:0.0} m/s, slowest {run.minSpeed:0.0} m/s");
                    Check($"C M5c {(back ? "back" : "the race's way")} {Nm(vid)}: through, never stopped", run.reached && run.minSpeed > 3f, $"{run.end}, slowest {run.minSpeed:0.0} m/s");
                }
            race.EndRace();
        }

        IEnumerator R57M5cAi()
        {
            report.AppendLine(); report.AppendLine("=== C. the Underground race, 3 laps, 3 AI on Hard, the player parked off the line: every AI finishes; its recoveries");
            int prev = RoadDriver.Difficulty; RoadDriver.Difficulty = 2;
            yield return StartRoute("underground.json", "forward", 3);
            var park = race.Route.PointAt(race.Route.GateS - 30f, out var pf); Place(park + Vector3.Cross(Vector3.up, pf).normalized * 1.5f, pf, 0f); car.enabled = false;
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            var nearM5c = new Dictionary<string, int>(); var lastRec = ai.ToDictionary(e => e.Name, e => 0);
            Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            float end = Time.realtimeSinceStartup + 7f * 60f;
            while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished))
            {
                foreach (var e in ai)
                {
                    int rc = e.Driver ? e.Driver.RecoveryCount : 0;
                    if (rc > lastRec[e.Name]) { var p = e.Car.Body.position; if (Flat(p - M57Spot).magnitude < 60f) nearM5c[e.Name] = (nearM5c.TryGetValue(e.Name, out var n) ? n : 0) + rc - lastRec[e.Name]; lastRec[e.Name] = rc; }
                }
                yield return new WaitForSecondsRealtime(.25f);
            }
            Time.timeScale = 1f; RoadDriver.Difficulty = prev; car.enabled = true;
            foreach (var e in ai)
            {
                report.AppendLine($"  {e.Name} ({e.VehicleName}): {(e.Finished ? RaceController.Fmt(e.FinalTotal) + (e.Estimated ? " (estimated)" : "") : "not finished")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, recoveries {(e.Driver ? e.Driver.RecoveryCount : 0)} (within 60 m of Dan's spot {(nearM5c.TryGetValue(e.Name, out var n) ? n : 0)})");
                Check($"C Underground race: {e.Name} finishes", e.Finished && !e.Estimated);
            }
            race.EndRace();
        }

        // ------------------------------------------------------------ D: the hub's rails and gate
        IEnumerator R57Hub()
        {
            report.AppendLine(); report.AppendLine("=== D. the hub's mid ledge (Dan's BUG-005 at x 1854.67 z 1132.51): into rail R1, the closed gate SG1 and rail R2 flat out");
            yield return StartRoute("underground.json", "forward", 0);
            const float Y = 262f;
            // (name, the point aimed at on the rail, the rail's outward normal)
            var targets = new[] { ("R1 (west edge, x 1861)", new Vector3(1861f, Y, 1150f), Vector3.right), ("SG1 closed (x 1861)", new Vector3(1861f, Y, 1132.8f), Vector3.right), ("R2 (south edge, z 1126)", new Vector3(1874f, Y, 1126f), Vector3.forward) };
            foreach (var vid in new[] { "drifter", "original", "atv", "moto" })
            {
                yield return Vehicle(vid);
                foreach (var (name, at, n) in targets)
                    foreach (float ang in new[] { 90f, 45f, 20f })
                    {
                        // run up 7 m along the ledge, hitting the rail at 'ang' to it
                        var along = Vector3.Cross(Vector3.up, n);
                        var dir = (n * Mathf.Sin(ang * Mathf.Deg2Rad) + along * Mathf.Cos(ang * Mathf.Deg2Rad)).normalized;
                        var start = at - dir * 7f; start.y = Y + 0.2f;
                        Place(start, dir, 30f); car.enabled = false;
                        bool through = false, fell = false; float t = 0f;
                        while (t < 2.5f)
                        {
                            yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                            car.Simulate(1f, 0f, 0f, Time.fixedDeltaTime);
                            var p = car.Body.position;
                            if (Vector3.Dot(p - at, n) > 0.6f) through = true;
                            if (p.y < Y - 1.5f) fell = true;
                        }
                        car.enabled = true;
                        report.AppendLine($"  {Nm(vid),-16} into {name} at {ang:0} deg, 30 m/s: {(through ? "THROUGH" : "held")}{(fell ? ", fell off the ledge" : "")}, ended at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})");
                        Check($"D {Nm(vid)} into {name} at {ang:0} deg: held", !through && !fell);
                    }
            }
            yield return Still("57_D_hub_ledge", new Vector3(1846f, Y + 3f, 1132.8f), new Vector3(1866f, Y, 1130f));
            race.EndRace();
        }

        IEnumerator R57Pics()
        {
            report.AppendLine(); report.AppendLine("=== Pictures");
            yield return StartRoute("underground.json", "forward", 0);
            yield return Vehicle("drifter");
            float s0 = AbsStation(M57Spot);
            foreach (var (nm, s, back) in new[] { ("57_C_m5c_race_way", s0 - 12f, false), ("57_C_m5c_back", s0 + 12f, true) })
            {
                var p = race.Route.PointAt(s, out var f); if (back) f = -f;
                Place(p, f, 0f); yield return new WaitForSecondsRealtime(1.5f);
                var cam = Camera.main; var ch = cam ? cam.GetComponent<ChaseCamera>() : null; if (ch) ch.Snap();
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Capture(nm);
            }
            race.EndRace();
        }
    }
}
