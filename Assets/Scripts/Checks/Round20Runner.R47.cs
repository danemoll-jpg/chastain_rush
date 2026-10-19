using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChastainRush
{
    // Round 47 in the exe (-exeRound47 <dir> [-r47Only lkrevlap,lkfwdlap,lkrevshut,lkrevrace,lkrevpics,drives] [-traffic Normal] -logFile <dir>\Player.log):
    //   lkrevlap    a reverse Chastain Lakes lap from the grid on the Street Classic and the Drifter Twin (no AI): the lap counts, 0 missed
    //   lkfwdlap    the forward race unchanged: one lap on the Street Classic, 0 missed
    //   lkrevshut   in a reverse race: no shortcuts, the boat jump's way and the pier barred, the PARTY sign and the tracks hidden, the drain's grate
    //             on and the drain shut; in free roam: the sign shown, no grate
    //   lkrevrace   the reverse race, all its laps, 3 AI, traffic as given, the player parked off the course: everyone finishes, 0 missed
    //   lkrevpics   the start, the footbridge from the north, the boat jump's way and the pier barred, the grate
    //   drives    B: Dan's spot on Chastain Road (BUG-001), free roam
    public sealed partial class Round20Runner
    {
        IEnumerator StartLakes(string dir, int ai)
        {
            RaceController.RouteOverride = "chastain_lakes.json"; RaceController.DirectionOverride = dir; RaceController.LapsOverride = 0; RaceController.AiCount = ai;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
        }

        // one whole lap from the grid along the race's own line, with the route's held speeds as a profile
        IEnumerator R47Lap(string dir, string id)
        {
            yield return Vehicle(id);
            yield return StartLakes(dir, 0);
            var r = race.Route; var p = race.Entrants.First(e => e.IsPlayer);
            float s0 = r.GateS - r.RunInLength, L = r.LapLength;
            var line = LapSlice(s0 + 4f, r.GateS + L + 25f);
            var zones = dir == "reverse" ? race.RouteData.ai_zones_reverse : race.RouteData.ai_zones;
            var prof = new List<float>();
            if (zones != null)
                foreach (var z in zones.OrderBy(z => Mathf.Repeat(z.s0 - s0, L)))
                {
                    float a = Mathf.Repeat(z.s0 - s0 - 4f, L), b = Mathf.Repeat(z.s1 - s0 - 4f, L);
                    prof.AddRange(new[] { a, z.speed, b, z.speed });
                }
            // the check's own driver (not the AI: it finishes the race) ran wide off the footbridge into the S-bend by the courts and stuck on their
            // fence (the trail's edge drops 2 m toward them on the downhill bend): held to 8 m/s from the bridge to the lot
            var zb = zones?.FirstOrDefault(z => z.id == "footbridge"); var zl = zones?.FirstOrDefault(z => z.id == "lot");
            if (dir == "reverse" && zb != null && zl != null) { var extra = new List<(float, float)>(); for (int i = 0; i + 1 < prof.Count; i += 2) extra.Add((prof[i], prof[i + 1])); extra.Add((Mathf.Repeat(zb.s1 - s0 - 4f, L), 8f)); extra.Add((Mathf.Repeat(zl.s0 - s0 - 4f, L), 8f)); prof = extra.OrderBy(e => e.Item1).SelectMany(e => new[] { e.Item1, e.Item2 }).ToList(); }
            var run = new Run();
            float t0 = Time.time;
            yield return DriveLine(line, 32f, 260f, run, null, x => p.CompletedLaps >= 1 && Flat(car.Body.position - r.Gate.Position).magnitude > 15f, 5f, true, prof.Count > 0 ? prof.ToArray() : null);
            if (p.CompletedLaps < 1)
            {
                yield return Capture($"47_{dir}_{id}_stopped");
                System.IO.File.WriteAllLines(System.IO.Path.Combine(outputDir, $"lap_{dir}_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
                var hit = Physics.OverlapSphere(car.Body.position, 4f, ~0, QueryTriggerInteraction.Ignore).Where(c => c.attachedRigidbody != car.Body).Select(c => c.name).Distinct();
                report.AppendLine($"  stopped by: {string.Join(", ", hit)}");
            }
            string lap = p.LapTimes.Count > 0 ? $"{p.LapTimes[0]:0.0} s" : "-";
            Check($"{dir} lap on the {Nm(id)}: counted, 0 missed", p.CompletedLaps >= 1 && p.MissedTotal == 0,
                  $"laps {p.CompletedLaps}, lap {lap}, missed {p.MissedTotal}, penalty {p.PenaltyTotal:0} s, {(run.reached || p.CompletedLaps >= 1 ? "" : "STOPPED " + run.end + $" at ({car.Body.position.x:0}, {car.Body.position.z:0}); ")}slowest {run.minSpeed:0.0} m/s, most off the line {run.maxDev:0.0} m");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R47RevLaps()
        {
            report.AppendLine(); report.AppendLine("=== A. REVERSE CHASTAIN LAKES: a lap from the grid (the check's own driver, no AI)");
            yield return StartLakes("reverse", 0);
            var r = race.Route;
            report.AppendLine($"  {r.Name} {r.Direction}: own reverse lap {r.OwnReverse}, {r.LapLength:0} m, {r.Laps} laps, {r.Checkpoints.Count} checkpoints ({string.Join(", ", r.Checkpoints.Select(c => c.Label))}), " +
                              $"{r.Shortcuts.Count} shortcuts, {r.Closures.Count} barriers");
            Check("A: the reverse is a lap of its own, with no shortcuts", r.OwnReverse && r.Shortcuts.Count == 0);
            race.EndRace();
            foreach (var id in new[] { "original", "drifter" }) yield return R47Lap("reverse", id);
        }

        IEnumerator R47FwdLap()
        {
            report.AppendLine(); report.AppendLine("=== A. the forward race unchanged");
            yield return StartLakes("forward", 0);
            var r = race.Route;
            report.AppendLine($"  {r.Name} {r.Direction}: {r.LapLength:0} m, {r.Checkpoints.Count} checkpoints, shortcuts {string.Join(", ", r.Shortcuts.Select(s => s.Name))}, {r.Closures.Count} barriers");
            Check("A: forward still has both shortcuts and its 16 barriers", r.Shortcuts.Count == 2 && r.Closures.Count == 16);
            race.EndRace();
            yield return R47Lap("forward", "original");
        }

        IEnumerator R47Shut()
        {
            report.AppendLine(); report.AppendLine("=== A. the forward shortcuts closed in a reverse race");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return new WaitForSecondsRealtime(0.5f);
            var only = FindObjectsByType<RaceOnly>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            RaceOnly Named(string n) => only.FirstOrDefault(o => o.name.StartsWith(n));
            var sign = Named("WB clues"); var tracks = Named("Lake drain clues"); var grate = Named("Lake drain grate"); var drain = LakeDrain.All.FirstOrDefault();
            bool freeOk = sign && sign.Shown && tracks && tracks.Shown && grate && !grate.Shown;
            Check("A: in free roam the PARTY sign and the tracks are there and the drain is open", freeOk, $"sign {(sign ? sign.Shown.ToString() : "MISSING")}, tracks {(tracks ? tracks.Shown.ToString() : "MISSING")}, grate {(grate ? grate.Shown.ToString() : "MISSING")}");
            yield return StartLakes("reverse", 0);
            yield return new WaitForSecondsRealtime(0.5f);
            var bars = FindAnyObjectByType<RaceBarriers>();
            var names = bars ? Enumerable.Range(0, bars.transform.childCount).Select(i => bars.transform.GetChild(i).name).ToList() : new List<string>();
            var boatBar = race.Route.Closures.FirstOrDefault(c => c.label != null && c.label.StartsWith("the boat jump's way")); var pierBar = race.Route.Closures.FirstOrDefault(c => c.label != null && c.label.StartsWith("the pier"));
            bool barred = boatBar != null && pierBar != null && names.Contains("Barrier " + boatBar.id) && names.Contains("Barrier " + pierBar.id);
            Check("A: reverse: barriers across the boat jump's way and the pier", barred, $"{names.Count} barriers built; open: {(bars && bars.Open.Count > 0 ? string.Join(", ", bars.Open) : "none")}");
            bool shut = sign && !sign.Shown && tracks && !tracks.Shown && grate && grate.Shown && RaceOnly.ReverseRaceOf(drain ? drain.shutReverseOf : "-");
            Check("A: reverse: the PARTY sign and the drain's tracks hidden, the grate over the drain, the drain shut", shut);
            // drop onto the grate from above: it holds
            yield return Vehicle("original");
            if (drain)
            {
                int sw0 = LakeDrain.Swallowed;
                Place(drain.mouth + Vector3.up * 3f, Vector3.forward, 0f);
                yield return new WaitForSecondsRealtime(3f);
                var p = car.Body.position;
                Check("A: reverse: a vehicle dropped on the drain sits on its grate, not down the shaft", LakeDrain.Swallowed == sw0 && p.y > drain.mouth.y - 0.5f, $"at {p.y - drain.mouth.y:+0.0;-0.0} m from the water, swallowed {LakeDrain.Swallowed - sw0}");
                yield return Still("47_rev_drain_grate", drain.mouth + new Vector3(10f, 5f, -8f), drain.mouth, 60f);
            }
            race.EndRace(); RaceController.DirectionOverride = null;
            yield return new WaitForSecondsRealtime(0.5f);
            Check("A: back in free roam: the sign is back, the grate gone", sign && sign.Shown && grate && !grate.Shown);
        }

        IEnumerator R47RevRace()
        {
            report.AppendLine(); report.AppendLine($"=== A. the reverse race: all laps, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, the player parked off the course");
            yield return StartLakes("reverse", 3);
            report.AppendLine($"  {race.Route.Name} {race.Route.Direction}, {race.Route.Laps} laps of {race.Route.LapLength:0} m");
            respawn.PlaceAt(RaceRoute.Surface(760f, 1530f) + Vector3.up * .4f, 0f); race.NotifyTeleported();
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            float end = Time.realtimeSinceStartup + 9f * 60f;
            while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished)) yield return new WaitForSecondsRealtime(.5f);
            Time.timeScale = 1f;
            foreach (var e in ai)
                Check($"{e.Name} ({e.VehicleName}) finishes the reverse laps, 0 missed", e.Finished && e.MissedTotal == 0,
                      $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, missed {e.MissedTotal}");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R47Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures (reverse)");
            yield return StartLakes("reverse", 0);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture("47_rev_start_grid");
            var r = race.Route; var g = r.Gate.Position;
            car.Body.position = RaceRoute.Surface(760f, 1530f) + Vector3.up; car.Body.linearVelocity = Vector3.zero;
            Vector3 G(float x, float z, float up) => RaceRoute.Surface(x, z) + Vector3.up * up;
            var back = r.PointAt(r.GateS - 30f, out _);
            yield return Still("47_rev_start_line", back + Vector3.up * 2.5f, g + Vector3.up * 1.5f, 60f);
            yield return Still("47_rev_footbridge_from_the_north", G(1351f, 1168f, 2.4f), G(1362f, 1130f, 0.5f), 60f);
            var boatBar = r.Closures.FirstOrDefault(c => c.label != null && c.label.StartsWith("the boat jump's way")); var pierBar = r.Closures.FirstOrDefault(c => c.label != null && c.label.StartsWith("the pier"));
            if (boatBar != null) yield return Still("47_rev_boat_jump_barred", G(boatBar.x + 4f, boatBar.z + 40f, 3f), G(boatBar.x, boatBar.z, 0.5f), 60f);
            yield return Still("47_rev_boat_turnoff_no_sign", G(606f, 1500f, 1.8f), G(578f, 1480f, 1f), 62f);
            if (pierBar != null) yield return Still("47_rev_pier_barred", G(pierBar.x + 16f, pierBar.z - 18f, 3f), G(pierBar.x - 8f, pierBar.z + 6f, 0.5f), 62f);
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("pictures written", true, "47_*.png");
        }

        IEnumerator R47Drives()
        {
            report.AppendLine(); report.AppendLine("=== B. the drives on Chastain Road at Dan's spot (BUG-001: x 1304.53 z 581.52, heading 304)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter");
            Place(RaceRoute.Surface(1298.7f, 585.4f), new Vector3(Mathf.Sin(303.6f * Mathf.Deg2Rad), 0f, Mathf.Cos(303.6f * Mathf.Deg2Rad)), 0f);
            yield return new WaitForSecondsRealtime(2f);
            yield return Capture("47_drives_dans_spot_chase");
            var at = RaceRoute.Surface(1304.5f, 581.5f) + Vector3.up * 3f; var fwd = new Vector3(Mathf.Sin(303.6f * Mathf.Deg2Rad), 0f, Mathf.Cos(303.6f * Mathf.Deg2Rad));
            yield return Still("47_drives_dans_spot", at, at + fwd * 40f + Vector3.right * 4f - Vector3.up * 2.5f, 62f);
            yield return Still("47_drives_row_from_above", RaceRoute.Surface(1300f, 560f) + Vector3.up * 45f, RaceRoute.Surface(1305f, 615f), 60f);
            Check("B: pictures from Dan's spot written (the drives' new ends are in Build Map's log)", true, "47_drives_*.png");
        }
    }
}
