using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 40 Part A in the exe (-exeRound40 <dir> -r40Only plume,plumewhen,plumepics,corner,clues,crest,ridgerace -traffic Normal):
    //   plume       every vehicle the reverse race offers, flat out along New Chastain Road into the manhole plume from the left lane, the right lane
    //               and the shoulder: thrown, the air, where it comes down (meters down the forward pile's ramp face), upright, on along the drive
    //   plumewhen   the first racer's arrival with the cover still shut (where it blew), a forward race (it never blows), Free Roam (it blows at
    //               speed, throws, shuts again)
    //   plumepics   from Dan's BUG-001 spot before and after it blows, the flight, the landing
    //   corner      A3: the reverse lap through the Canton Road / New Chastain Road junction on the Drifter Twin flat out: no bump-log event
    //   clues       A4 / A5: S1's and S2's clues hidden in a reverse race (shown in a forward race and Free Roam), no reverse S1, a picture from BUG-004
    //   crest       A6: the crest cut driven clean and crashed, against the lap
    //   (church, churchpics: Round 40's church leap, removed in Round 41 for the crypt: Round20Runner.R41.cs)
    //   ridgerace   3 laps reverse, 3 AI, traffic Normal: everyone finishes; the AI's shortcuts (then a race where they take every one: sloppy tries)
    public sealed partial class Round20Runner
    {
        static ManholePlume Plume() => ManholePlume.All.FirstOrDefault(p => p.id == "RP");
        static readonly Vector3 Bug001Cam = new Vector3(3265.30f, 314.19f, 627.41f);   // Dan's BUG-001 camera (heading 302, pitch 13.5)
        float lastFlightAir; Vector3 lastFlightLand; string lastFlightSrc; float lastFlightSpeed;

        IEnumerator StartRevRace(int ai = 0)
        {
            RaceController.RouteOverride = "chastain_ridge.json"; RaceController.DirectionOverride = "reverse"; RaceController.LapsOverride = 3; RaceController.AiCount = ai;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
        }

        IEnumerator R40Plume(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== A1 / A2: THE MANHOLE PLUME (reverse race): each vehicle flat out along New Chastain Road into it, from three lines");
            var pl = Plume(); var fwd = BigJump.All.FirstOrDefault(b => b.id == "RL");
            if (pl == null || fwd == null) { Check("the manhole plume and the forward pile are in the build", false); yield break; }
            yield return Vehicle("original");
            yield return StartRevRace();
            float sP = AbsStation(pl.transform.position);
            var offered = ids.Where(id => race.RouteData == null || race.RouteData.AllowsVehicle(id)).ToArray();
            var bad = new List<string>(); int flights = 0;
            string src = null; float speed = 0f; System.Action<ArcadeVehicle, string, float, float> hook = (c, s, u, T) => { if (c == car) { src = s; speed = u; } };
            GuidedFlight.Began += hook;
            var side = Vector3.Cross(Vector3.up, pl.along).normalized;
            foreach (var id in offered)
                foreach (var (lane, off) in new[] { ("left lane", -2.0f), ("right lane", 2.0f), ("shoulder", 4.6f) })
                {
                    yield return Vehicle(id);
                    // the lap's line up New Chastain Road, moved across onto the lane, then on from the landing along the drive
                    var line = LapSlice(sP - 160f, sP + 175f).Select(p => AbsStation(p) < sP + 2f ? RaceRoute.Surface(p.x + side.x * off, p.z + side.z * off) : p).ToList();
                    Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
                    src = null; var run = new Run(); float top = -999f, air = 0f, maxAir = 0f; Vector3 land = new Vector3(float.NaN, 0, 0); float upLand = -1f; bool shot = false, shotLand = false;
                    var drive = StartCoroutine(DriveLine(line, 99f, 40f, run, null, null, 5f));
                    while (!run.reached && run.end == "" && run.time < 39.9f)
                    {
                        yield return new WaitForFixedUpdate();
                        if (src != null) top = Mathf.Max(top, car.Body.position.y - pl.transform.position.y);
                        if (car.GroundedWheels == 0) air += Time.fixedDeltaTime;
                        else { if (src != null && air > 1f && float.IsNaN(land.x)) { land = car.Body.position; upLand = car.transform.up.y; } maxAir = Mathf.Max(maxAir, air); air = 0f; }
                        if (flights == 0 && src != null && !shot && air > 1.6f) { shot = true; yield return Capture("40_plume_flight_chase"); }
                        if (flights == 0 && !float.IsNaN(land.x) && !shotLand) { shotLand = true; yield return new WaitForSecondsRealtime(.3f); yield return Capture("40_plume_landing_chase"); }
                    }
                    yield return drive;
                    float down = float.IsNaN(land.x) ? -99f : Vector3.Dot(Flat(land - fwd.lip), Flat(-fwd.dir).normalized);
                    bool ok = src != null && src.StartsWith("plume") && !float.IsNaN(land.x) && upLand > .7f && down > -1f && down < 30f && run.reached;
                    if (!ok) { bad.Add($"{Nm(id)} {lane}"); File.WriteAllLines(Path.Combine(outputDir, $"plume_{id}_{off:0.0}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                    report.AppendLine($"  {Nm(id),-18} {lane,-10} {(src != null ? $"thrown at {speed:0.0} m/s across, {maxAir:0.00} s in the air, {top:0.0} m up, down {down:0.0} m on the forward pile's ramp face, upright {upLand:0.00}" : "NOT THROWN")}; {(run.reached ? "drove on" : "STOPPED " + run.end)}");
                    flights++;
                }
            GuidedFlight.Began -= hook;
            race.EndRace(); RaceController.DirectionOverride = null; RaceController.AiCount = 3;
            Check("the manhole plume: every vehicle the reverse race offers, flat out from the left lane, the right lane and the shoulder, thrown onto the dead end, upright, and on", bad.Count == 0,
                  bad.Count == 0 ? $"{offered.Length} vehicles x 3 lines" : "not: " + string.Join(", ", bad));
        }

        IEnumerator R40PlumeWhen()
        {
            report.AppendLine(); report.AppendLine("=== A1: when the plume blows");
            var pl = Plume(); if (pl == null) { Check("the plume is there", false); yield break; }
            yield return Vehicle("drifter");
            // the first racer comes at it with the cover shut
            yield return StartRevRace();
            float sP = AbsStation(pl.transform.position);
            bool shutAtStart = !pl.Erupting;
            var line = LapSlice(sP - 200f, sP + 120f);
            Place(line[0], line[2] - line[0], 20f); yield return new WaitForFixedUpdate();
            float blewAt = -1f; var run = new Run();
            var drive = StartCoroutine(DriveLine(line, 99f, 30f, run, null, r => { if (blewAt < 0f && pl.Erupting) blewAt = Vector3.Distance(Flat(car.Body.position), Flat(pl.transform.position)); return false; }, 5f));
            yield return drive;
            Check("reverse race: the cover is shut until the first racer comes, then it blows ahead of them", shutAtStart && blewAt > 20f, $"shut at the start {shutAtStart}; it blew with the racer {blewAt:0} m away");
            yield return new WaitForSecondsRealtime(3f);
            Check("reverse race: it keeps gushing", pl.Erupting, $"erupting {pl.Erupting} {(Time.time - pl.EruptedAt):0} s after it blew");
            race.EndRace(); yield return new WaitForSecondsRealtime(.5f);
            Check("after the race it is shut", !pl.Erupting);
            // a forward race: never blows, never throws (driven at it along the northwest-bound carriageway at speed)
            RaceController.RouteOverride = "chastain_ridge.json"; RaceController.DirectionOverride = "forward"; RaceController.AiCount = 0; race.Reload(); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            int th0 = ManholePlume.Throws;
            var nw = Straight(RaceRoute.Surface(pl.transform.position.x - pl.along.x * 120f, pl.transform.position.z - pl.along.z * 120f), RaceRoute.Surface(pl.transform.position.x + pl.along.x * 40f, pl.transform.position.z + pl.along.z * 40f));
            Place(nw[0], nw[2] - nw[0], 20f); yield return new WaitForFixedUpdate();
            bool everOn = false; run = new Run(); drive = StartCoroutine(DriveLine(nw, 30f, 20f, run, null, r => { everOn |= pl.Erupting; return false; }, 5f)); yield return drive;
            Check("forward race: the plume never blows (driven through its place at 30 m/s)", !everOn && ManholePlume.Throws == th0, $"erupted {everOn}, thrown {ManholePlume.Throws - th0}");
            race.EndRace(); RaceController.DirectionOverride = null; RaceController.AiCount = 3; yield return new WaitForSecondsRealtime(.5f);
            // Round 42 (BUG-003): Free Roam no longer blows it (Round42Runner checks that)
        }

        IEnumerator R40PlumePics()
        {
            report.AppendLine(); report.AppendLine("=== A1: pictures of the plume");
            var pl = Plume(); var fwd = BigJump.All.FirstOrDefault(b => b.id == "RL"); if (pl == null || fwd == null) yield break;
            yield return Vehicle("drifter");
            yield return StartRevRace();
            // the player parked 30 m back so the camera shows the road ahead
            car.Body.position = RaceRoute.Surface(3420f, 985f) + Vector3.up; car.Body.linearVelocity = Vector3.zero; race.NotifyTeleported(); pl.Shut();
            var look = Bug001Cam + Quaternion.Euler(13.5f, 301.96f, 0f) * Vector3.forward * 40f;
            yield return Still("40_plume_bug001_before", Bug001Cam, look, 60f);
            pl.Erupt(1e9f); yield return new WaitForSecondsRealtime(2.5f);
            yield return Still("40_plume_bug001_after", Bug001Cam, look, 60f);
            var side = Vector3.Cross(Vector3.up, (pl.target - pl.transform.position).normalized);
            var mid = Vector3.Lerp(pl.transform.position, pl.target, .45f);
            yield return Still("40_plume_side", mid + side * 75f + Vector3.up * 25f, mid + Vector3.up * 14f, 62f);
            yield return Still("40_plume_landing", fwd.lip + Flat(-fwd.dir).normalized * 22f + Vector3.up * 5f, fwd.lip + Vector3.up * 1f, 62f);
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R40Corner()
        {
            report.AppendLine(); report.AppendLine("=== A3: the reverse lap's turn from Canton Road onto New Chastain Road (BUG-003), the Drifter Twin flat out");
            UseRoute("chastain_ridge.json", "reverse");
            yield return Vehicle("drifter");
            var j = new Vector3(3563f, 0f, 402f); float sJ = AbsStation(j);
            var line = LapSlice(sJ - 150f, sJ + 160f);
            var bl = FindAnyObjectByType<BumpLogger>(); string last = bl ? bl.LastLine : ""; var seen = new List<string>();
            Place(line[0], line[2] - line[0], 25f); yield return new WaitForFixedUpdate();
            var run = new Run(); float vMin = 99f;
            var drive = DriveLine(line, 99f, 30f, run, null, r =>
            {
                var p = car.Body.position; bool near = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(3480f, 405f)) < 90f || Vector2.Distance(new Vector2(p.x, p.z), new Vector2(3563f, 420f)) < 40f;
                if (near) vMin = Mathf.Min(vMin, car.ForwardSpeed);
                if (bl && bl.LastLine != last) { last = bl.LastLine; if (near) seen.Add(last); }
                return false;
            });
            yield return drive;
            Check("A3: no bump-log event through the junction and on to Dan's spot at speed", bl != null && seen.Count == 0 && run.reached,
                  $"{(bl ? "" : "NO BUMP LOGGER; ")}slowest {vMin:0.0} m/s through the turn, least wheels down {run.minWheels}, longest air {run.maxAir:0.00} s; events {seen.Count}{(seen.Count > 0 ? ": " + string.Join(" | ", seen) : "")}");
        }

        IEnumerator R40Clues()
        {
            report.AppendLine(); report.AppendLine("=== A4 / A5: S1's and S2's clues, no reverse S1");
            var holders = FindObjectsByType<RaceOnly>(FindObjectsSortMode.None).Where(r => !string.IsNullOrEmpty(r.hideInReverseOf)).ToList();
            yield return Vehicle("drifter");
            if (race.State != RaceController.Stage.Free) race.EndRace(); yield return new WaitForSecondsRealtime(.5f);
            bool free = holders.Count >= 2 && holders.All(h => h.Shown);
            RaceController.RouteOverride = "chastain_ridge.json"; RaceController.DirectionOverride = "forward"; RaceController.AiCount = 0; race.Reload(); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null; yield return new WaitForSecondsRealtime(.5f);
            bool fw = holders.All(h => h.Shown); race.EndRace();
            yield return StartRevRace(); yield return new WaitForSecondsRealtime(.5f);
            bool rv = holders.All(h => !h.Shown);
            var names = race.Route.Shortcuts.Select(s => s.Name).ToList();
            car.Body.position = RaceRoute.Surface(3300f, 520f) + Vector3.up; car.Body.linearVelocity = Vector3.zero;
            yield return Still("40_bug004_reverse_no_clues", new Vector3(3352.09f, 319.40f, 461.53f), new Vector3(3352.09f, 319.40f, 461.53f) + Quaternion.Euler(13.5f, 313.35f, 0f) * Vector3.forward * 40f, 60f);
            race.EndRace(); RaceController.DirectionOverride = null; RaceController.AiCount = 3;
            Check("A4: S1's and S2's turn-off clues hidden in a reverse race, shown in Free Roam and a forward race", free && fw && rv, $"{holders.Count} clue holders; Free Roam shown {free}, forward shown {fw}, reverse hidden {rv}");
            Check("A5: no reverse S1 (the reverse race's shortcuts)", !names.Any(n => n.Contains("Knoll")) && !BigJump.All.Any(b => b.id == "S1R"), string.Join(", ", names));
        }

        // drives a shortcut's line (or the lap from its turn-off to where it rejoins), timed from a line across the lap at the turn-off to the rejoin
        IEnumerator TimedShortcut(string scId, bool viaShortcut, float off, float[] profile, Run run, System.Action<float> done)
        {
            var sc = race.Route.Shortcuts.First(s => s.Id == scId);
            var a = sc.Path[0]; var b = sc.Path[sc.Path.Length - 1]; float sa = AbsStation(a), sb = AbsStation(b); if (sb < sa) sb += race.Route.LapLength;
            var line = LapSlice(sa - 60f, sa).ToList();
            if (viaShortcut)
            {
                var path = sc.Path.Select(p => RaceRoute.Surface(p.x, p.z)).ToList();
                for (int i = 0; i < path.Count; i++)
                {
                    var t = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]); t.y = 0f; var n = Vector3.Cross(Vector3.up, t.normalized);
                    float k = Mathf.Clamp01(i / 6f) * Mathf.Clamp01((path.Count - 1 - i) / 6f);
                    line.Add(path[i] + n * off * k);
                }
                line.AddRange(LapSlice(sb + 2f, sb + 30f));
            }
            else line.AddRange(LapSlice(sa + 2f, sb + 30f));
            Place(line[0], line[2] - line[0], 15f); yield return new WaitForFixedUpdate();
            float t0 = -1f, t1 = -1f, clock = 0f; Vector3 bEnd = RaceRoute.Surface(b.x, b.z);
            var lp = profile == null ? null : profile.Select((v, i) => i % 2 == 0 ? v + 60f : v).ToArray();   // the profile's stations are from the shortcut's start
            var drive = DriveLine(line, 99f, 60f, run, null, r =>
            {
                var p = car.Body.position; clock += Time.fixedDeltaTime;   // (r.time is only set when the drive ends)
                if (t0 < 0f && Vector3.Distance(Flat(p), Flat(RaceRoute.Surface(a.x, a.z))) < 5f) t0 = clock;
                if (t0 >= 0f && t1 < 0f && Vector3.Distance(Flat(p), Flat(bEnd)) < 6f) t1 = clock;
                return false;
            }, 5f, true, viaShortcut ? lp : null);
            yield return drive;
            done(t0 >= 0f && t1 > t0 ? t1 - t0 : -1f);
        }

        IEnumerator R40Crest()
        {
            report.AppendLine(); report.AppendLine("=== A6: THE CREST CUT (reverse race): driven clean and crashed, against the lap");
            yield return StartRevRace();
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "rc_crest_cut"); var sj = race.RouteData.official_shortcuts.FirstOrDefault(s => s.id == "rc_crest_cut");
            if (sc == null || sj == null) { Check("the crest cut is in the reverse race", false); race.EndRace(); yield break; }
            foreach (var id in new[] { "drifter", "original", "moto" })
            {
                yield return Vehicle(id);
                float road = -1f, clean = -1f, crash = -1f;
                float careful = -1f;
                var r0 = new Run(); yield return TimedShortcut(sc.Id, false, 0f, null, r0, v => road = v);
                var r3 = new Run(); yield return TimedShortcut(sc.Id, true, 0f, sj.ai_profile, r3, v => careful = v);
                var r1 = new Run(); yield return TimedShortcut(sc.Id, true, 0f, sj.ai_slip_profile, r1, v => clean = v);   // flat out on the line
                var r2 = new Run(); yield return TimedShortcut(sc.Id, true, 1.6f, sj.ai_slip_profile, r2, v => crash = v);   // flat out, 1.6 m off it
                report.AppendLine($"  {Nm(id),-18} at the AI's careful speeds {careful:0.0} s ({(r3.reached ? "through" : "STOPPED " + r3.end)})");
                if (id == "drifter") yield return Capture("40_crest_after_crash");
                foreach (var (nm, rr) in new[] { ("clean", r1), ("crash", r2) })
                    if (!rr.reached) { File.WriteAllLines(Path.Combine(outputDir, $"crest_{id}_{nm}.csv"), rr.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); report.AppendLine($"    {nm} run stopped at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0}) [trail crest_{id}_{nm}.csv]"); }
                report.AppendLine($"  {Nm(id),-18} the lap {road:0.0} s; the crest cut flat out on the line {clean:0.0} s ({(clean > 0 && road > 0 ? $"saves {road - clean:0.0} s" : "-")}, {(r1.reached ? "through" : "STOPPED " + r1.end)}); " +
                                  $"flat out 1.6 m off the line {(crash > 0 ? crash.ToString("0.0") + " s" : "crashed")} ({(crash > 0 && road > 0 ? $"{crash - road:+0.0;-0.0} s against the lap" : r2.reached ? "through, slowed" : "STOPPED " + r2.end)})");
                if (id == "drifter") Check("A6: the crest cut driven clean saves time (the Drifter Twin)", clean > 0f && road > 0f && clean < road - .3f, $"lap {road:0.0} s, cut {clean:0.0} s");
            }
            race.EndRace(); RaceController.DirectionOverride = null;
            // pictures: Dan's spot, the gap, the boulder wall
            yield return Still("40_crest_bug005_spot", new Vector3(3091.70f, 337.55f, 616.18f), new Vector3(3091.70f, 337.55f, 616.18f) + Quaternion.Euler(13.5f, 357.6f, 0f) * Vector3.forward * 40f, 60f);
            var gap = RaceRoute.Surface(3091.75f, 644f); yield return Still("40_crest_gap", gap + new Vector3(0f, 3f, -14f), gap + Vector3.up * 1f, 60f);
            var w = RaceRoute.Surface(3084f, 662f); yield return Still("40_crest_boulder_wall", w + new Vector3(-6f, 4f, -12f), w + Vector3.up * .5f, 62f);
        }

        IEnumerator R40RevRace()
        {
            foreach (bool all in new[] { false, true })
            {
                report.AppendLine(); report.AppendLine($"=== Part A: the reverse race, 3 laps, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}{(all ? " (every AI driver takes every shortcut: sloppy tries as they come)" : "")}, the player parked off the course");
                RoadDriver.CheckTakeAll = all;
                yield return StartRevRace(3);
                report.AppendLine($"  {race.Route.Name} {race.Route.Direction}: {race.Route.LapLength:0} m, {race.Route.Checkpoints.Count} checkpoints, shortcuts {string.Join(", ", race.Route.Shortcuts.Select(s => s.Name))}, {race.Route.Closures.Count} barriers");
                respawn.PlaceAt(RaceRoute.Surface(3420f, 985f) + Vector3.up * .4f, 0f); race.NotifyTeleported();
                var ai = race.Entrants.Where(e => !e.IsPlayer).ToList(); int th0 = ManholePlume.Throws;
                Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                float end = Time.realtimeSinceStartup + 9f * 60f;
                var sloppy = new HashSet<string>();
                while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished))
                {
                    yield return new WaitForSecondsRealtime(.25f);
                    foreach (var e in ai) if (e.Driver && e.Driver.Sloppy && e.Driver.OnShortcut) sloppy.Add($"{e.Name} {e.Driver.OnShortcut.Id} lap {e.CompletedLaps + 1}");
                }
                Time.timeScale = 1f;
                foreach (var e in ai)
                    Check($"{e.Name} ({e.VehicleName}) finishes the 3 reverse laps{(all ? " taking every shortcut" : "")}", e.Finished,
                          $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, missed {e.MissedTotal}, shortcuts {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}");
                report.AppendLine($"INFO  thrown by the plume {ManholePlume.Throws - th0}, AI sloppy tries on the shortcuts {sloppy.Count}{(sloppy.Count > 0 ? ": " + string.Join(", ", sloppy) : "")}");
                race.EndRace();
            }
            RoadDriver.CheckTakeAll = false; RaceController.DirectionOverride = null;
        }
    }
}
