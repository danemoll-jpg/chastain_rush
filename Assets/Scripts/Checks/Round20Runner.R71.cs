using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 71 in the exe (-exeRound71 <dir> [-r71Only aprobe,aline,amiss,acrash,aai,atrial,apics] [-routeDir <dir>] -logFile <dir>\Player.log): the
    // Quarry Air Race, the player's plane flown by the AI pilot's own code (PlanePilot on the player, Hard's pace) unless said otherwise:
    //   aprobe  the rings' line against the built world, no flying: every 2.5 m the plane's strike points (wing tips, tail, nose) and its body on the
    //           line, turned along it: what they would meet (colliders, trees' crowns); each ring's ground under it and anything over it
    //   aline   a lap flown cleanly: every ring passed, no crash; the lap time; a trace (71_line_trace.tsv)
    //   amiss   a lap with ring 3 missed on purpose: the 5 s penalty, the lap counted
    //   acrash  a crash after ring 2: back in the air before ring 2, flying, the lap finished
    //   aai     2 AI pilots on Hard, 2 laps: every one finishes (not estimated)
    //   atrial  the time trial: solo, 2 laps, the ghost of the best lap saved, then shown on the next run
    //   apics   the start, the quarry dive, under I-75, a high ring, the circuit on the map
    public sealed partial class Round20Runner
    {
        const string R71File = "quarry_air_race.json";
        static readonly Vector3[] R71Strikes = { new(5.5f, 1.2f, .15f), new(-5.5f, 1.2f, .15f), new(2.8f, 1.15f, .15f), new(-2.8f, 1.15f, .15f), new(0, 1.7f, -4.3f), new(1.7f, .33f, -3.9f), new(-1.7f, .33f, -3.9f), new(0, .1f, 3.7f), new(0, -1.25f, 0f) };

        IEnumerator R71Probe()
        {
            report.AppendLine(); report.AppendLine("=== The rings' line against the built world (no flying): the plane's strike points every 2.5 m");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            UseRoute(R71File, "forward");
            var r = race.Route; if (r == null || !r.Air) { Check("aprobe: the air route loads", false, r?.Name ?? "none"); yield break; }
            report.AppendLine($"  {r.Name}: lap {r.LapLength:0} m, {r.Checkpoints.Count} rings + the line, {r.Laps} laps");
            Physics.SyncTransforms(); yield return null;
            var mine = new HashSet<Collider>(car.GetComponentsInChildren<Collider>());
            var hits = new Collider[16]; var stretches = new List<(float s0, float s1, string what)>();
            float minAgl = 999f, minAglS = 0f;
            for (float s = 0f; s < r.LapLength; s += 2.5f)
            {
                var p = r.PointAt(r.GateS + s, out var f); var a = r.PointAt(r.GateS + s - 5f, out _); var b = r.PointAt(r.GateS + s + 5f, out _);
                var dir = (b - a).normalized; var q = Quaternion.LookRotation(dir, Vector3.up);
                string what = null;
                foreach (var local in R71Strikes)
                {
                    var w = p + q * local;
                    int n = Physics.OverlapSphereNonAlloc(w, .4f, hits, ~0, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < n && what == null; k++) if (!mine.Contains(hits[k]) && !hits[k].attachedRigidbody) what = $"{(local.y < -1f ? "the wheels" : local.x > 1f ? "the right wing" : local.x < -1f ? "the left wing" : local.z > 0f ? "the nose" : "the tail")} meets {hits[k].name}";
                    if (what != null) break;
                }
                if (what == null && TreeCrowns.Hit(p + q * new Vector3(0, 1.1f, .15f), 5.2f, p, out string tree)) what = tree.Replace("rotor", "a wing");
                float gy = GroundQuery.Height(p.x, p.z); if (!float.IsNaN(gy) && p.y - gy < minAgl) { minAgl = p.y - gy; minAglS = s; }
                if (what == null) continue;
                if (stretches.Count > 0 && s - stretches[stretches.Count - 1].s1 <= 5f && stretches[stretches.Count - 1].what == what) stretches[stretches.Count - 1] = (stretches[stretches.Count - 1].s0, s, what);
                else stretches.Add((s, s, what));
            }
            foreach (var (s0, s1, what) in stretches)
            {
                var p = r.PointAt(r.GateS + s0, out _);
                report.AppendLine($"  HIT  {s0:0} to {s1:0} m from the line ({p.x:0}, {p.y:0}, {p.z:0}): {what}");
            }
            report.AppendLine($"  lowest over the ground: {minAgl:0.0} m at {minAglS:0} m from the line");
            // under the decks: the room over and under the line, across the wings (rays up and down at the line and 5.5 m either side)
            var win = new StringBuilder(); var dump = new StringBuilder("x\tz\ty\tbelow_top\tabove_bottom\n");
            for (float s = 0f; s < r.LapLength; s += 2.5f)
            {
                var p = r.PointAt(r.GateS + s, out var f); var rt = Vector3.Cross(Vector3.up, f);
                float up = 999f, down = 999f; string what = "";
                foreach (float o in new[] { -5.5f, 0f, 5.5f })
                {
                    var q = p + rt * o;
                    if (Physics.Raycast(q, Vector3.up, out var hu, 40f, ~0, QueryTriggerInteraction.Ignore) && !mine.Contains(hu.collider) && hu.distance < up) { up = hu.distance; what = hu.collider.name; }
                    if (Physics.Raycast(q, Vector3.down, out var hd, 40f, ~0, QueryTriggerInteraction.Ignore) && !mine.Contains(hd.collider)) down = Mathf.Min(down, hd.distance);
                }
                dump.AppendLine($"{p.x:0.00}\t{p.z:0.00}\t{p.y:0.00}\t{(down < 40f ? (p.y - down).ToString("0.00") : "nan")}\t{(up < 40f ? (p.y + up).ToString("0.00") : "nan")}");
                if (up < 40f && Mathf.Repeat(s, 5f) < .1f) win.AppendLine($"    {s,6:0} m ({p.x:0}, {p.y:0.0}, {p.z:0}): {up:0.0} m up to {what}, {down:0.0} m down (body 1.25 m under the middle, tail 1.7 m over it)");
            }
            if (win.Length > 0) { report.AppendLine("  under the decks:"); report.Append(win); }
            File.WriteAllText(Path.Combine(outputDir, "71_line_room.tsv"), dump.ToString());   // the built world over and under the line (make_air_route.py --room reads it)
            foreach (var g in new[] { r.Gate }.Concat(r.Checkpoints))
            {
                var c = g.Position; float gy = GroundQuery.Height(c.x, c.z);
                float up = Physics.Raycast(c, Vector3.up, out var hu, 60f, ~0, QueryTriggerInteraction.Ignore) ? hu.distance : -1f;
                float down = Physics.Raycast(c, Vector3.down, out var hd, 400f, ~0, QueryTriggerInteraction.Ignore) ? hd.distance : -1f;
                report.AppendLine($"  {(g.Id == 0 ? "S/F" : "ring " + g.Id),-7} {g.Label,-30} ({c.x:0}, {c.y:0}, {c.z:0}) {(g.RingW > 0f ? $"frame {2 * g.RingW:0} x {2 * g.RingH:0.0} m" : $"ring {2 * g.Ring:0} m across")}; ground {c.y - gy:0.0} m under it (ray {down:0.0} m{(down >= 0 ? " to " + hd.collider.name : "")}){(up >= 0f ? $", {hu.collider.name} {up:0.0} m over it" : "")}");
            }
            Check("aprobe: nothing solid and no tree's crown on the rings' line", stretches.Count == 0, $"{stretches.Count} stretch(es) listed above");
        }

        // a race on the air route with the player's plane flown by the AI pilot's code
        PlanePilot R71Fly(int laps, int ai, int difficulty)
        {
            RoadDriver.Difficulty = difficulty;
            RaceController.RouteOverride = R71File; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = laps; RaceController.AiCount = ai;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace();
            var pl = car.GetComponent<PlaneFlight>(); var old = car.GetComponent<PlanePilot>(); if (old) DestroyImmediate(old);
            var pilot = car.gameObject.AddComponent<PlanePilot>(); pilot.Initialize(race, car, race.Player, 0, 0f);
            return pilot;
        }
        void R71Done(PlanePilot pilot) { var pl = car.GetComponent<PlaneFlight>(); if (pilot) Destroy(pilot); if (pl) { pl.Scripted = false; pl.sThrottle = -1f; } Time.timeScale = 1f; PlanePilot.ForceMiss = -1; }

        // run until the player's race is over (or 'limit' seconds of game time), logging the race's events and a trace
        IEnumerator R71Run(string tag, float limit, List<string> events, StringBuilder trace = null, Func<bool> stop = null, Action each = null)
        {
            void OnLog(string l) { events.Add($"{race.RaceClock,7:0.0}  {l}"); }
            race.Logged += OnLog;
            var pl = car.GetComponent<PlaneFlight>();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            Time.timeScale = 2f; Time.maximumDeltaTime = .5f; float t = 0f, nextTrace = 0f;
            while (t < limit && !(race.Player.Finished) && !(stop != null && stop()))
            {
                yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; each?.Invoke();
                if (trace != null && t >= nextTrace)
                {
                    nextTrace = t + .25f; var p = car.Body.position; float gy = GroundQuery.Height(p.x, p.z);
                    trace.AppendLine($"{t:0.00}\t{race.Route.LapStationOf(p):0}\t{p.x:0.0}\t{p.y:0.0}\t{p.z:0.0}\t{pl.Airspeed:0.0}\t{p.y - gy:0.0}\t{pl.PitchDeg:0}\t{pl.RollDeg:0}\t{pl.Throttle:0.00}\t{(pl.Airborne ? 1 : 0)}\t{(pl.Crashed ? pl.CrashReason : "")}");
                }
            }
            Time.timeScale = 1f; race.Logged -= OnLog;
        }

        IEnumerator R71Line()
        {
            report.AppendLine(); report.AppendLine("=== A lap flown cleanly by script (the AI pilot's code on the player's plane, Hard's pace)");
            var pilot = R71Fly(1, 0, 2); var pl = car.GetComponent<PlaneFlight>(); int crashes0 = pl.Crashes;
            var events = new List<string>(); var trace = new StringBuilder("t\tlap_m\tx\ty\tz\tspeed\tagl\tpitch\troll\tthrottle\tair\tcrash\n");
            yield return R71Run("aline", 420f, events, trace);
            File.WriteAllText(Path.Combine(outputDir, "71_line_trace.tsv"), trace.ToString());
            int crashes = pl.Crashes - crashes0; var p = race.Player;
            foreach (var e in events.Where(x => x.Contains("CHECKPOINT") || x.Contains("PENALTY") || x.Contains("CRASH") || x.Contains("LAP") || x.Contains("START LINE") || x.Contains("FINISH"))) report.AppendLine("  " + e);
            Check("aline: the lap finished", p.Finished, p.Finished ? $"lap {RaceController.Fmt(p.LapTimes.FirstOrDefault())}" : $"laps {p.CompletedLaps}, next ring {p.NextCheckpoint + 1}");
            Check("aline: every ring passed (none missed)", p.Finished && p.MissedTotal == 0, $"{p.MissedTotal} missed");
            Check("aline: no crash", crashes == 0, crashes > 0 ? $"{crashes}: {pl.CrashReason}" : "");
            R71Done(pilot); race.EndRace();
        }

        IEnumerator R71Miss()
        {
            report.AppendLine(); report.AppendLine("=== A lap with ring 3 missed on purpose");
            var pilot = R71Fly(1, 0, 2); PlanePilot.ForceMiss = 3;
            var events = new List<string>(); yield return R71Run("amiss", 420f, events);
            var p = race.Player;
            foreach (var e in events.Where(x => x.Contains("PENALTY") || x.Contains("LAP") || x.Contains("FINISH"))) report.AppendLine("  " + e);
            Check("amiss: ring 3 missed, 5 s penalty, the lap counted", p.Finished && p.CompletedLaps == 1 && p.Missed.Count + p.MissedTotal >= 1 && p.PenaltyTotal >= 5f && events.Any(x => x.Contains("missed CP3")),
                  $"laps {p.CompletedLaps}, missed {p.MissedTotal}, penalty {p.PenaltyTotal:0} s, lap {RaceController.Fmt(p.LapTimes.FirstOrDefault())}");
            R71Done(pilot); race.EndRace();
        }

        IEnumerator R71Crash()
        {
            report.AppendLine(); report.AppendLine("=== A crash after ring 2: the reset");
            var pilot = R71Fly(1, 0, 2); var pl = car.GetComponent<PlaneFlight>();
            var events = new List<string>(); bool crashed = false; float crashT = 0f; string after = "";
            yield return R71Run("acrash", 420f, events, null, null, () =>
            {
                var p = race.Player;
                if (!crashed && p.NextCheckpoint == 2) { var q = race.Route.Checkpoints[1].Position; if (Flat(car.Body.position - q).magnitude > 300f) { crashed = true; crashT = Time.time; pl.Crash("the check: a crash on purpose"); } }
                if (crashed && after == "" && Time.time - crashT > PlaneFlight.CrashFor + .5f)
                {
                    var c = car.Body.position; var ring = race.Route.Checkpoints[1].Position; float gy = GroundQuery.Height(c.x, c.z);
                    after = $"{(pl.Airborne ? "in the air" : "NOT in the air")}, {Flat(ring - c).magnitude:0} m before ring 2, {c.y - gy:0} m over the ground, {pl.Airspeed:0} m/s";
                }
            });
            var e2 = race.Player;
            foreach (var e in events.Where(x => x.Contains("CRASH") || x.Contains("LAP") || x.Contains("FINISH"))) report.AppendLine("  " + e);
            report.AppendLine("  after the reset: " + after);
            Check("acrash: put back in the air before ring 2, flying on", crashed && after.StartsWith("in the air"), after);
            Check("acrash: the lap finished after it", e2.Finished, $"laps {e2.CompletedLaps}, missed {e2.MissedTotal}");
            R71Done(pilot); race.EndRace();
        }

        IEnumerator R71Ai()
        {
            report.AppendLine(); report.AppendLine("=== 2 AI pilots on Hard, 2 laps (the player flown by script at Normal's pace)");
            float wait = RaceController.PlayerFinishWait; RaceController.PlayerFinishWait = 600f;
            var pilot = R71Fly(2, 2, 2); var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            var events = new List<string>();
            yield return R71Run("aai", 900f, events, null, () => race.Player.Finished && ai.All(e => e.Finished));
            if (!ai.All(e => e.Finished)) { Time.timeScale = 2f; float t = 0f; while (t < 400f && !ai.All(e => e.Finished)) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; } Time.timeScale = 1f; }
            foreach (var e in events.Where(x => x.Contains("CRASH") || x.Contains("PENALTY") || x.Contains("FINISH"))) report.AppendLine("  " + e);
            foreach (var e in race.Entrants)
                report.AppendLine($"  {e.Name,-5} {(e.Finished ? RaceController.Fmt(e.FinalTotal) + (e.Estimated ? " (estimated)" : "") : "NOT FINISHED")}  laps {string.Join(" / ", e.LapTimes.Select(x => RaceController.Fmt(x)))}, missed {e.MissedTotal}");
            Check("aai: both AI pilots finish (flown, not estimated)", ai.Count == 2 && ai.All(e => e.Finished && !e.Estimated), string.Join(", ", ai.Select(e => $"{e.Name} {(e.Finished ? RaceController.Fmt(e.FinalTotal) : "no")}")));
            RaceController.PlayerFinishWait = wait; R71Done(pilot); race.EndRace();
        }

        IEnumerator R71Trial()
        {
            report.AppendLine(); report.AppendLine("=== The time trial: solo, 2 laps; the ghost");
            var pilot = R71Fly(2, 0, 2); var ghost = race.Ghost; if (ghost && !ghost.Enabled) ghost.Toggle();
            var events = new List<string>(); yield return R71Run("atrial", 900f, events);
            var p = race.Player;
            report.AppendLine($"  laps {string.Join(" / ", p.LapTimes.Select(x => RaceController.Fmt(x)))}, total {RaceController.Fmt(p.FinalTotal)}, missed {p.MissedTotal}; best lap {RaceController.Fmt(race.BestLap)}, Top 10 race #{race.RaceRank}");
            Check("atrial: the time trial finished with a best lap and a Top 10 place", p.Finished && race.BestLap > 0f && race.RaceRank > 0, $"rank {race.RaceRank}");
            R71Done(pilot); race.EndRace();
            ghost?.Refresh(); var gfile = ghost ? Directory.GetFiles(ghost.Root, "Quarry Air Race*.json") : new string[0];
            report.AppendLine($"  ghost: {ghost?.Status}; saved: {string.Join(", ", gfile.Select(Path.GetFileName))}");
            Check("atrial: the best lap's ghost saved", gfile.Length > 0);
            // the next run: the ghost flies with it
            pilot = R71Fly(1, 0, 2); bool seen = false;
            yield return R71Run("atrial2", 40f, events, null, null, () => { if (ghost && ghost.Showing) seen = true; });
            Check("atrial: the best lap's ghost flies on the next run", seen, ghost ? ghost.Status : "no ghost");
            R71Done(pilot); race.EndRace();
        }

        IEnumerator R71Pics()
        {
            report.AppendLine(); report.AppendLine("=== Pictures");
            yield return Clock(13f);
            var pilot = R71Fly(1, 3, 1); var r = race.Route;
            // the start: the grid on the runway during the countdown
            yield return new WaitForSecondsRealtime(1.2f);
            { var p = car.Body.position; var f = Flat(car.transform.forward).normalized; var rt = Vector3.Cross(Vector3.up, f); yield return Still("71_start", p - f * 30f + rt * 14f + Vector3.up * 9f, p + f * 70f, 55f); }
            var events = new List<string>();
            int shot = 0; string[] names = { "71_quarry_dive", "71_under_i75", "71_high_ring", "71_summit_ring" };
            Vector3[] at = { r.Checkpoints[0].Position, r.Checkpoints[3].Position, r.Checkpoints[5].Position, r.Checkpoints[8].Position };
            while (race.State == RaceController.Stage.Countdown) yield return null;
            while (shot < names.Length && !race.Player.Finished)
            {
                // fly on until 110 m short of the next picture's ring, then a frozen picture behind the plane
                Time.timeScale = 2f; float t = 0f;
                while (t < 300f && Flat(car.Body.position - at[shot]).magnitude > 110f) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; }
                Time.timeScale = 1f;
                yield return Frozen(names[shot], -26f, 6f, 5f); shot++;
            }
            R71Done(pilot);
            // the circuit on the map
            var m = WorldMap.Instance; if (m) { m.Open(); yield return new WaitForSecondsRealtime(1f); yield return Capture("71_map"); m.Close(); }
            race.EndRace();
        }
    }
}
