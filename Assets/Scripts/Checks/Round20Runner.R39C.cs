using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 39 Part C in the exe (-exeRound39 <dir> -r39Only climb,fwdleap,climbreset,ridgerev,revpics -traffic Normal): the reverse Chastain Ridge
    //   climb       every vehicle the route offers flat out along the reverse lap into THE RIDGE CLIMB: onto the lip, air, height, where it comes down
    //               (meters down the forward pile's ramp face from its lip), upright, and on along Chastain Ridge Drive
    //   fwdleap     the forward Ridge leap flown once more (the Street Classic): it still lands on New Chastain Road
    //   climbreset  a reverse race reset in the hollow short of the climb: onto Chastain Ridge Drive past the landing, facing on
    //   ridgerev    3 laps reverse, 3 AI, traffic Normal, the player parked off the course: every AI finishes
    //   revpics     the climb's take-off from New Chastain Road, the flight, the landing, the Knoll drop's turn-off
    public sealed partial class Round20Runner
    {
        // the climb's dirt pile ("CLIMB"; "RC" in a scene built before the rename, which Noonday Creek's RC also uses): the one at its lip
        static BigJump Climb() => BigJump.All.FirstOrDefault(b => (b.id == "CLIMB" || b.id == "RC") && Vector2.Distance(new Vector2(b.lip.x, b.lip.z), new Vector2(3221f, 632f)) < 6f);
        IEnumerator R39Climb(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== THE RIDGE CLIMB (the reverse Chastain Ridge): each vehicle flat out along the reverse lap, off the road-work pile up onto the dead end");
            UseRoute("chastain_ridge.json", "reverse");
            var j = Climb(); var fwd = BigJump.All.FirstOrDefault(b => b.id == "RL");
            if (j == null || fwd == null || race.Route == null || !race.Route.OwnReverse) { Check("the reverse Chastain Ridge lap and its climb are there", false, race.Route == null ? "no route" : $"own reverse {race.Route.OwnReverse}, the climb {(j != null)}"); yield break; }
            float sLip = AbsStation(j.lip);
            var line = LapSlice(sLip - 70f, sLip + 150f);
            float launch = -1f; bool launched = false;
            System.Action<BigJump, ArcadeVehicle, float> hook = (b, c, v) => { if (b == j && c == car) { launch = v; launched = true; } };
            BigJump.Launched += hook;
            var bad = new List<string>(); var offered = ids.Where(id => race.RouteData == null || race.RouteData.AllowsVehicle(id)).ToArray();
            foreach (var id in offered)
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                launch = -1f; launched = false;
                var run = new Run(); float top = -999f, air = 0f, maxAir = 0f; Vector3 land = new Vector3(float.NaN, 0, 0); float upLand = -1f; bool shot = false, shotLand = false;
                var drive = StartCoroutine(DriveLine(line, 99f, 40f, run, null, null, 5f));
                while (!run.reached && run.end == "" && run.time < 39.9f)
                {
                    yield return new WaitForFixedUpdate();
                    if (launched) top = Mathf.Max(top, car.Body.position.y - j.lip.y);
                    if (car.GroundedWheels == 0) air += Time.fixedDeltaTime;
                    else { if (launched && air > 1f && float.IsNaN(land.x)) { land = car.Body.position; upLand = car.transform.up.y; } maxAir = Mathf.Max(maxAir, air); air = 0f; }
                    if (id == offered[0] && launched && !shot && air > 1.5f) { shot = true; yield return Capture("39_climb_flight_chase"); }
                    if (id == offered[0] && !float.IsNaN(land.x) && !shotLand) { shotLand = true; yield return new WaitForSecondsRealtime(.3f); yield return Capture("39_climb_landing_chase"); }
                }
                yield return drive;
                // where it came down: meters past the forward pile's lip along the climb's way (down its ramp face: positive), and from the line
                float down = float.IsNaN(land.x) ? -99f : Vector3.Dot(Flat(land - fwd.lip), Flat(-fwd.dir).normalized);
                float off = float.IsNaN(land.x) ? 99f : OffLine(land);
                bool ok = launched && !float.IsNaN(land.x) && upLand > 0.7f && down > -1f && down < 30f && off < 6f && run.reached;
                if (!ok) { bad.Add(Nm(id)); File.WriteAllLines(Path.Combine(outputDir, $"climb_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                report.AppendLine($"  {Nm(id),-18} {(launched ? $"onto the lip at {launch,4:0.0} m/s (off it at {j.vmax:0.0}), {maxAir:0.00} s in the air, {top:0.0} m over the lip at the top, down {down:0.0} m past the forward pile's lip, {off:0.0} m from the line, upright {upLand:0.00}" : "NEVER LAUNCHED")}; {(run.reached ? "drove on" : "STOPPED " + run.end)}");
            }
            BigJump.Launched -= hook;
            Check("THE RIDGE CLIMB: every vehicle the route offers, flat out, lands on the dead end (on or past the forward pile's ramp), on its wheels, and drives on", bad.Count == 0, bad.Count == 0 ? $"{offered.Length} vehicles" : "not: " + string.Join(", ", bad));
        }

        IEnumerator R39ClimbReset()
        {
            report.AppendLine(); report.AppendLine("=== Short of the climb (reverse race): a reset in the hollow");
            UseRoute("chastain_ridge.json", "reverse");
            yield return Vehicle("original");
            var j = Climb(); var fwd = BigJump.All.First(b => b.id == "RL");
            RaceController.AiCount = 0; race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            var g = RaceRoute.Surface(3214f, 628f);
            respawn.PlaceAt(g + Vector3.up * 0.5f, 262f); race.NotifyTeleported();
            yield return new WaitForSecondsRealtime(1.5f);
            bool found = CarRecovery.Find(respawn, out var pos, out var yaw);
            if (found) { respawn.PlaceAt(pos, yaw); race.NotifyTeleported(); }
            yield return new WaitForSecondsRealtime(1f);
            var p = car.Body.position; float past = Vector3.Dot(Flat(p - fwd.lip), Flat(-fwd.dir).normalized);
            race.Route.PointAt(race.Route.GateS + race.Route.LapStationOf(p), out var lapF); float facing = Vector3.Angle(Flat(car.transform.forward), Flat(lapF));
            Check("a reset in the hollow short of the climb (reverse race): onto Chastain Ridge Drive past the landing, facing on", found && OffLine(p) < 4f && past > 14f && facing < 20f,
                  $"from ({g.x:0}, {g.z:0}) to ({p.x:0.0}, {p.z:0.0}), {past:0} m past the forward pile's lip, {OffLine(p):0.0} m from the line, {facing:0} degrees off the race's way");
            yield return Capture("39_climb_reset");
            race.EndRace(); RaceController.AiCount = 3;
            yield return new WaitForSecondsRealtime(0.5f);
        }

        IEnumerator R39RevRace()
        {
            report.AppendLine(); report.AppendLine($"=== The reverse race: 3 laps, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, the player parked off the course");
            RaceController.RouteOverride = "chastain_ridge.json"; RaceController.DirectionOverride = "reverse"; RaceController.LapsOverride = 3;
            RaceController.AiCount = 3; RaceController.AiVehiclesOverride = null;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            report.AppendLine($"  route {race.Route.Name} {race.Route.Direction} (its own reverse lap: {race.Route.OwnReverse}), {race.Route.LapLength:0} m, {race.Route.Checkpoints.Count} checkpoints, {race.Route.Shortcuts.Count} shortcuts ({string.Join(", ", race.Route.Shortcuts.Select(s => s.Name))}), {race.Route.Closures.Count} barriers");
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Capture("39_revrace_grid");
            respawn.PlaceAt(RaceRoute.Surface(3420f, 985f) + Vector3.up * 0.4f, 0f); race.NotifyTeleported();
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            Time.timeScale = 2f; Time.maximumDeltaTime = 0.5f;
            float t0 = Time.realtimeSinceStartup, end = t0 + 9f * 60f;
            while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished)) yield return new WaitForSecondsRealtime(0.5f);
            Time.timeScale = 1f;
            foreach (var e in ai)
                Check($"{e.Name} ({e.VehicleName}) finishes the 3 reverse laps", e.Finished,
                      $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, missed {e.MissedTotal}, shortcuts {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R39RevPics()
        {
            UseRoute("chastain_ridge.json", "reverse");
            var j = Climb(); var fwd = BigJump.All.First(b => b.id == "RL");
            var side = Vector3.Cross(Vector3.up, j.dir).normalized;
            car.Body.position = j.lip + side * 40f + Vector3.up * 3f; car.Body.linearVelocity = Vector3.zero;
            var from = race.Route.PointAt(race.Route.GateS + race.Route.LapStationOf(j.lip) - 60f, out _);
            yield return Still("39_climb_takeoff_from_new_chastain", from + Vector3.up * 3.5f, j.lip + Vector3.up * 2f, 58f);
            yield return Still("39_climb_flight_from_the_side", j.lip + j.dir * 40f + side * 60f + Vector3.up * 18f, j.lip + j.dir * 40f + Vector3.up * 12f, 70f);
            yield return Still("39_climb_landing_on_the_pile", fwd.lip + Flat(-fwd.dir).normalized * 25f + side * 8f + Vector3.up * 5f, fwd.lip + Vector3.up * 1f, 62f);
            yield return Still("39_climb_from_the_dead_end", fwd.lip + Flat(-fwd.dir).normalized * 12f + Vector3.up * 6f, j.lip + Vector3.up * 4f, 62f);
            foreach (var sc in race.Route.Shortcuts)
            {
                float s0 = race.Route.LapStationOf(sc.Path[0]);
                var at = race.Route.PointAt(race.Route.GateS + s0 - 45f, out _);
                yield return Still($"39_{sc.Id}_turn_off", at + Vector3.up * 1.6f, sc.Path[Mathf.Min(10, sc.Path.Length - 1)] + Vector3.up * 1f, 62f);
            }
        }
    }
}
