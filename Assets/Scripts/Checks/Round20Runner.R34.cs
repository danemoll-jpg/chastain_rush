using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 34 in the exe: ChastainRush.exe -exeRound34 <dir> [-r34Only lap,bumps,radio,records] -logFile <dir>\Player.log. Muted
    // (CheckRun), its files in <dir>, report round34_check.txt. One check per item, at Dan's spot:
    //   lap      A6: the Chastain Ridge race from its grid: the clock starts at the first crossing of the line (lap 1, no penalty);
    //            back over the line and across it again (Dan's case): still lap 1, no penalty
    //   bumps    A1: the Drifter Twin flat out up Canton Road Northeast's northbound lane from z 630 to z 860: no bump-log event
    //            from z 650 to z 840
    //   radio, records: Parts B and C (Round20Runner.R34B.cs, Round20Runner.R34C.cs)
    public sealed partial class Round20Runner
    {
        IEnumerator R34Lap()
        {
            report.AppendLine(); report.AppendLine("=== A6: the first crossing of the line (Chastain Ridge, from the grid)");
            UseRoute("chastain_ridge.json", "forward");
            yield return Vehicle("drifter");
            int ai = RaceController.AiCount; RaceController.AiCount = 0;
            race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            var p = race.Entrants.First(e => e.IsPlayer);
            var grid = car.Body.position;
            var gate = race.Route.Gate.Position;
            // drive from the grid to 6 m short of the line: the clock must not have started
            var line = new List<Vector3>(); for (float z = grid.z; z <= 760f; z += 2f) line.Add(RaceRoute.Surface(3576.7f, z));
            var run = new Run();
            yield return DriveLine(line, 14f, 30f, run, null, r => car.Body.position.z > gate.z - 6f);
            bool before = !p.ClockStarted;
            yield return DriveLine(line, 14f, 30f, run, null, r => car.Body.position.z > gate.z + 25f);
            Check("A6: from the grid, the clock starts at the first crossing of the line: lap 1, no penalty",
                  before && p.ClockStarted && p.CompletedLaps == 0 && p.PenaltyTotal == 0f && p.NextCheckpoint == 0,
                  $"grid z {grid.z:0}, line z {gate.z:0}; clock {(before ? "not started" : "STARTED")} 6 m short of the line; after it: started {p.ClockStarted}, " +
                  $"HUD lap {p.CompletedLaps + 1}, penalty {p.PenaltyTotal:0} s, clock {race.Clock:0.0} s");
            yield return Capture("34_lap_after_first_crossing");
            // Dan's case: back over the line (placed 30 m short of it, as after backing up) and across it again
            Place(RaceRoute.Surface(3576.7f, gate.z - 30f), Vector3.forward, 0f);
            yield return new WaitForSecondsRealtime(0.5f);
            line.Clear(); for (float z = gate.z - 30f; z <= 780f; z += 2f) line.Add(RaceRoute.Surface(3576.7f, z));
            run = new Run();
            yield return DriveLine(line, 14f, 30f, run, null, r => car.Body.position.z > gate.z + 20f);
            Check("A6: back over the line and across it again: still lap 1, no penalty", p.CompletedLaps == 0 && p.PenaltyTotal == 0f,
                  $"HUD lap {p.CompletedLaps + 1}, laps counted {p.CompletedLaps}, penalty {p.PenaltyTotal:0} s");
            yield return Capture("34_lap_after_recrossing");
            race.EndRace(); RaceController.AiCount = ai;
            yield return new WaitForSecondsRealtime(0.5f);
        }

        IEnumerator R34Bumps()
        {
            report.AppendLine(); report.AppendLine("=== A1: Canton Road Northeast's northbound lane, the Drifter Twin flat out");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter");
            // the northbound lane: 3.75 m right of Canton Road Northeast's centerline (OSM 1416681832)
            var c = new[] { new Vector2(3573.2f, 592f), new Vector2(3573.4f, 628f), new Vector2(3573.4f, 680f), new Vector2(3573.4f, 710f), new Vector2(3571.4f, 746f),
                            new Vector2(3570.3f, 754f), new Vector2(3568.9f, 768f), new Vector2(3565.9f, 787f), new Vector2(3562.4f, 822f), new Vector2(3560.2f, 838f), new Vector2(3543f, 912f) };
            var line = new List<Vector3>();
            for (int i = 0; i + 1 < c.Length; i++)
            {
                var d = (c[i + 1] - c[i]).normalized; var n = new Vector2(d.y, -d.x) * 3.75f;
                for (float t = 0f; t < 1f; t += 0.1f) { var q = Vector2.Lerp(c[i], c[i + 1], t) + n; if (q.y >= 600f) line.Add(RaceRoute.Surface(q.x, q.y)); }
            }
            var bl = FindAnyObjectByType<BumpLogger>();
            int ev0 = bl ? bl.Events : 0; var seen = new List<string>(); string last = bl ? bl.LastLine : "";
            Place(line[0], line[1] - line[0], 20f);
            yield return new WaitForFixedUpdate();
            var run = new Run(); float vMax = 0f;
            var drive = DriveLine(line, 40f, 30f, run, null, r =>
            {
                vMax = Mathf.Max(vMax, car.ForwardSpeed);
                if (bl && bl.LastLine != last) { last = bl.LastLine; var z = car.Body.position.z; if (z > 650f && z < 840f) seen.Add($"z {z:0}: {last}"); }
                return car.Body.position.z > 860f;
            });
            yield return drive;
            Check("A1: no bump-log event from z 650 to z 840 at speed", bl != null && seen.Count == 0,
                  $"{(bl ? "" : "NO BUMP LOGGER; ")}top speed {vMax:0.0} m/s, least wheels down {run.minWheels}, longest air {run.maxAir:0.00} s, events in the stretch {seen.Count}" + (seen.Count > 0 ? ": " + string.Join(" | ", seen) : ""));
        }
    }
}
