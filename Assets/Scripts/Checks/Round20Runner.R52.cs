using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChastainRush
{
    // Round 52 in the exe (-exeRound52 <dir> [-r52Only ahlap,ahshut,ahrace,ahpics,ahhouses] [-r20Vehicles ids] [-traffic Normal] [-r20Minutes 90]
    // -logFile <dir>\Player.log):
    //   ahlap     a lap of Addison Heights from the grid, forward and reverse, on every vehicle (the check's own driver, no AI): counted, 0 missed, no stop
    //   ahshut    each direction: every barrier built, and the vehicle driven at 18 m/s from the lap down each closed road: it does not get past
    //   ahrace    each direction twice: 3 AI, traffic as given, the player parked off the course: everyone finishes, 0 missed
    //   ahpics    the start / finish, each barrier
    //   ahhouses  Dan's three spots (BUG-001 to BUG-003) as he stood, and from above
    public sealed partial class Round20Runner
    {
        IEnumerator StartAddison(string dir, int ai)
        {
            RaceController.RouteOverride = "addison_heights.json"; RaceController.DirectionOverride = dir; RaceController.LapsOverride = 0; RaceController.AiCount = ai;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
        }

        IEnumerator R52Lap(string dir, string id)
        {
            yield return Vehicle(id);
            yield return StartAddison(dir, 0);
            var r = race.Route; var p = race.Entrants.First(e => e.IsPlayer);
            // the grid to the gate: RunInLength counts the lead-in as ending at the loop's start (the Glen loop's), this route's ends at the gate
            float s0 = r.GateS - (dir == "reverse" ? r.RunInLength : race.RouteData.lead_in_length_m), L = r.LapLength;
            var line = LapSlice(s0 + 4f, r.GateS + L + 25f);
            var run = new Run();
            Time.timeScale = 2f;
            yield return DriveLine(line, 32f, 300f, run, null, x => p.CompletedLaps >= 1 && Flat(car.Body.position - r.Gate.Position).magnitude > 15f, 5f, true);
            Time.timeScale = 1f;
            if (p.CompletedLaps < 1)
            {
                yield return Capture($"52_{dir}_{id}_stopped");
                var hit = Physics.OverlapSphere(car.Body.position, 4f, ~0, QueryTriggerInteraction.Ignore).Where(c => c.attachedRigidbody != car.Body).Select(c => c.name).Distinct();
                report.AppendLine($"  stopped by: {string.Join(", ", hit)}");
            }
            string lap = p.LapTimes.Count > 0 ? $"{p.LapTimes[0]:0.0} s" : "-";
            Check($"{dir} lap on the {Nm(id)}: counted, 0 missed, no stop", p.CompletedLaps >= 1 && p.MissedTotal == 0 && run.end != "stopped",
                  $"laps {p.CompletedLaps}, lap {lap}, missed {p.MissedTotal}, {(run.reached || p.CompletedLaps >= 1 ? "" : "STOPPED " + run.end + $" at ({car.Body.position.x:0}, {car.Body.position.z:0}); ")}slowest {run.minSpeed:0.0} m/s, most off the line {run.maxDev:0.0} m, longest air {run.maxAir:0.0} s");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R52Laps(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== A. ADDISON HEIGHTS: a lap from the grid each way on every vehicle (the check's own driver, no AI)");
            foreach (var dir in new[] { "forward", "reverse" })
            {
                yield return StartAddison(dir, 0);
                var r = race.Route;
                report.AppendLine($"  {r.Name} {r.Direction}: own reverse lap {r.OwnReverse}, {r.LapLength:0} m, {r.Laps} laps, traffic {race.RouteData.traffic}, {r.Checkpoints.Count} checkpoints ({string.Join(", ", r.Checkpoints.Select(c => c.Label))}), " +
                                  $"{r.Shortcuts.Count} shortcuts, {r.Closures.Count} barriers; the line at ({r.Gate.Position.x:0}, {r.Gate.Position.z:0})");
                Check($"A: {dir}: 3 laps, no shortcuts, open roads", r.Laps == 3 && r.Shortcuts.Count == 0 && race.RouteData.traffic);
                race.EndRace();
                foreach (var id in ids) yield return R52Lap(dir, id);
            }
        }

        IEnumerator R52Shut()
        {
            yield return Vehicle("original");
            foreach (var dir in new[] { "forward", "reverse" })
            {
                report.AppendLine(); report.AppendLine($"=== A. {dir}: every road off the lap barred (driven at from the lap)");
                yield return StartAddison(dir, 0);
                yield return new WaitForSecondsRealtime(0.5f);
                var bars = FindAnyObjectByType<RaceBarriers>();
                var names = bars ? Enumerable.Range(0, bars.transform.childCount).Select(i => bars.transform.GetChild(i).name).ToList() : new List<string>();
                var closures = race.Route.Closures.ToList();
                Check($"A: {dir}: all {closures.Count} barriers built", closures.All(c => names.Contains("Barrier " + c.id)), $"open: {(bars && bars.Open.Count > 0 ? string.Join(", ", bars.Open) : "none")}");
                foreach (var c in closures)
                {
                    var along = new Vector3(c.dx, 0f, c.dz).normalized; var bar = RaceRoute.Surface(c.x, c.z);
                    var from = RaceRoute.Surface(c.contact[0], c.contact[1]);
                    var dirTo = Flat(bar - from).normalized; if (dirTo.sqrMagnitude < 0.5f) dirTo = along;
                    var line = new List<Vector3> { from, bar, bar + along * 30f };
                    Place(from, dirTo, 8f);
                    yield return new WaitForSecondsRealtime(0.3f);
                    var run = new Run(); float past = -99f;
                    yield return DriveLine(line, 18f, 7f, run, null, x => { past = Mathf.Max(past, Vector3.Dot(Flat(car.Body.position - bar), along)); return past > 4f; }, 5f, false);
                    past = Mathf.Max(past, Vector3.Dot(Flat(car.Body.position - bar), along));
                    Check($"A: {dir}: {c.label} shut", past < 2f, $"from the lap at ({c.contact[0]:0}, {c.contact[1]:0}) to the barrier at ({c.x:0}, {c.z:0}): got {past:+0.0;-0.0} m past it");
                }
                race.EndRace(); RaceController.DirectionOverride = null;
            }
        }

        IEnumerator R52Race(string dir, int n)
        {
            report.AppendLine(); report.AppendLine($"=== A. the {dir} race #{n}: 3 laps, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, the player parked off the course");
            yield return StartAddison(dir, 3);
            respawn.PlaceAt(RaceRoute.Surface(2880f, 313f) + Vector3.up * .4f, 90f); race.NotifyTeleported();   // Crawford Circle's west leg, past its barrier
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            float end = Time.realtimeSinceStartup + 9f * 60f;
            while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished)) yield return new WaitForSecondsRealtime(.5f);
            Time.timeScale = 1f;
            foreach (var e in ai)
                Check($"{dir} #{n}: {e.Name} ({e.VehicleName}) finishes, 0 missed", e.Finished && e.MissedTotal == 0,
                      $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, missed {e.MissedTotal}");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R52Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures: the start / finish, the barriers");
            foreach (var dir in new[] { "forward", "reverse" })
            {
                yield return StartAddison(dir, 0);
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Capture($"52_{dir}_grid");
                var r = race.Route; var g = r.Gate.Position;
                var back = r.PointAt(r.GateS - 30f, out _);
                yield return Still($"52_{dir}_start_line", back + Vector3.up * 2.5f, g + Vector3.up * 1.5f, 60f);
                foreach (var c in r.Closures)
                {
                    var bar = RaceRoute.Surface(c.x, c.z); var from = RaceRoute.Surface(c.contact[0], c.contact[1]);
                    var d = Flat(bar - from).normalized; if (d.sqrMagnitude < 0.5f) d = new Vector3(c.dx, 0f, c.dz);
                    var cam = bar - d * 22f; cam = RaceRoute.Surface(cam.x, cam.z) + Vector3.up * 3f;   // over the ground there (these streets climb)
                    yield return Still($"52_{dir}_barrier_{c.id}", cam, bar + Vector3.up * 0.6f, 55f);
                }
                race.EndRace(); RaceController.DirectionOverride = null;
            }
            Check("pictures written", true, "52_*.png");
        }

        IEnumerator R52Houses()
        {
            report.AppendLine(); report.AppendLine("=== B. Dan's three spots (free roam)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter");
            // 'top' the house (houses) looked at, 'eye' a second view from higher up, clear of the trees
            foreach (var (nm, x, z, yaw, top, eye) in new[] { ("bug001", 2764.81f, 565.33f, 179.3f, new Vector2(2765f, 545f), new Vector3(2766f, 9f, 578f)), ("bug002", 3231.02f, 503.06f, 239.8f, new Vector2(3216f, 491f), new Vector3(3246f, 7f, 512f)), ("bug003", 2943.80f, 419.43f, 2.5f, new Vector2(2940f, 446f), new Vector3(2944f, 7f, 404f)) })
            {
                var fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
                Place(RaceRoute.Surface(x, z), fwd, 0f);
                yield return new WaitForSecondsRealtime(2f);
                yield return Capture($"52_{nm}_chase");
                var at = RaceRoute.Surface(x, z) - fwd * 6f + Vector3.up * 3.2f;
                yield return Still($"52_{nm}_dans_view", at, at + fwd * 30f - Vector3.up * 2.5f, 62f);
                var c = RaceRoute.Surface(top.x, top.y);
                yield return Still($"52_{nm}_above", RaceRoute.Surface(eye.x, eye.z) + Vector3.up * eye.y, c, 60f);
            }
            Check("B: pictures at Dan's three spots written", true, "52_bug00*.png");
        }
    }
}
