using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 33 in the exe: ChastainRush.exe -exeRound33 <dir> [-r33Only jump,shortcuts,hollow,race,pics] [-r20Vehicles ids] -traffic Normal
    // -logFile <dir>\Player.log. Muted (CheckRun), its files in <dir>, report round33_check.txt. The Chastain Ridge race:
    //   jump       every vehicle flat out from the ridge's crest off the dirt pile: its speed onto the lip, air, height, where it lands
    //              (meters from the lip, from the carriageway's centerline), upright, and on along New Chastain Road
    //   shortcuts  S1 and S2 driven flat out on their lines (the Street Classic): over the pile, landed, back on the lap
    //   hollow     short of the road: in the hollow, a race reset puts the vehicle on New Chastain Road past the landing, facing on;
    //              and driven, the way out up the graded ramp onto the road
    //   race       3 laps, 3 AI, traffic Normal, the player parked off the course: every AI finishes; shortcuts taken, resets, missed
    //   pics       the take-off from the crest, the flight, the landing, each shortcut's turn-off
    public sealed partial class Round20Runner
    {
        static readonly Vector2 RidgeHollow = new Vector2(3222f, 616f);

        IEnumerator R33Jump(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== The Ridge leap: each vehicle flat out from the crest (on the lap's line), off the dirt pile onto New Chastain Road");
            UseRoute("chastain_ridge.json", "forward");
            var j = BigJump.All.FirstOrDefault(b => b.id == "RL");
            if (j == null || race.Route == null || race.Route.Name != "Chastain Ridge") { Check("the Chastain Ridge route and its dirt pile are there", false); yield break; }
            float sLip = AbsStation(j.lip);
            var road = race.Route.Path;   // New Chastain Road is the lap after the landing
            var line = LapSlice(sLip - 72f, sLip + 175f);
            float launch = -1f; bool launched = false;
            System.Action<BigJump, ArcadeVehicle, float> hook = (b, c, v) => { if (b == j && c == car) { launch = v; launched = true; } };
            BigJump.Launched += hook;
            var bad = new List<string>();
            foreach (var id in ids)
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
                launch = -1f; launched = false;
                var run = new Run(); bool shotLanding = false; float top = -999f, air = 0f, maxAir = 0f; Vector3 land = new Vector3(float.NaN, 0, 0); float upLand = -1f;
                var drive = StartCoroutine(DriveLine(line, 99f, 40f, run, null, null, 5f));
                while (!run.reached && run.end == "" && run.time < 39.9f)
                {
                    yield return new WaitForFixedUpdate();
                    if (launched) top = Mathf.Max(top, car.Body.position.y - j.lip.y);
                    if (car.GroundedWheels == 0) air += Time.fixedDeltaTime;
                    else { if (launched && air > 1f && float.IsNaN(land.x)) { land = car.Body.position; upLand = car.transform.up.y; } maxAir = Mathf.Max(maxAir, air); air = 0f; }
                    if (id == ids[0] && launched && air > 1.6f && air < 1.6f + Time.fixedDeltaTime * 1.5f) yield return Capture("33_flight_chase");   // over the hollow, the chase camera
                    if (id == ids[0] && !float.IsNaN(land.x) && !shotLanding && Vector3.Dot(Flat(car.Body.position - land), j.dir) > 6f) { shotLanding = true; yield return Capture("33_landing_chase"); }
                }
                yield return drive;
                float dist = float.IsNaN(land.x) ? -1f : Vector3.Dot(Flat(land - j.lip), j.dir);
                float off = float.IsNaN(land.x) ? 99f : OffLine(land);
                bool ok = launched && run.reached && !float.IsNaN(land.x) && off < 6f && upLand > 0.7f;
                if (!ok) { bad.Add(Nm(id)); File.WriteAllLines(Path.Combine(outputDir, $"ridge_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                report.AppendLine($"  {Nm(id),-18} {(launched ? $"onto the lip at {launch,4:0.0} m/s (off it at {j.vmax:0.0}), {maxAir:0.00} s in the air, {top:0.0} m over the lip at the top, down {dist:0} m out, {off:0.0} m from the carriageway's centerline (the lap's line), upright {upLand:0.00}" : "NEVER LEFT THE LIP")}, {(run.reached ? "on down New Chastain Road" : "DID NOT GET ON " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}");
            }
            BigJump.Launched -= hook;
            Check("the Ridge leap: every vehicle flat out from the crest lands on New Chastain Road (within 6 m of the carriageway's centerline), on its wheels, and drives on", bad.Count == 0, bad.Count == 0 ? $"{ids.Length} vehicles" : "not: " + string.Join(", ", bad));
        }

        // meters from the race's line (the carriageway's centerline after the landing)
        float OffLine(Vector3 p) { var q = race.Route.PointAt(race.Route.GateS + race.Route.LapStationOf(p), out _); return Flat(q - p).magnitude; }

        IEnumerator R33Shortcuts()
        {
            report.AppendLine(); report.AppendLine("=== S1 and S2 on their lines, flat out (the Street Classic): over the dirt pile, landed, back on the lap");
            UseRoute("chastain_ridge.json", "forward");
            yield return Vehicle("original");
            foreach (var sc in race.Route.Shortcuts)
            {
                var j = BigJump.All.FirstOrDefault(b => sc.Id.StartsWith(b.id.ToLower() + "_"));
                float launch = -1f; System.Action<BigJump, ArcadeVehicle, float> hook = (b, c, v) => { if (b == j && c == car) launch = v; };
                BigJump.Launched += hook;
                var line = sc.Path.ToList();
                var before = LapSlice(AbsStation(line[0]) - 40f, AbsStation(line[0]) - 2f);
                line.InsertRange(0, before);
                var prof = sc.AiProfile?.Select((v, i) => i % 2 == 0 ? v + 40f : v + 4f).ToArray();   // the AI's held speeds a little faster, on this line (40 m of lap first)
                Place(line[0], line[2] - line[0], 16f); yield return new WaitForFixedUpdate();
                var run = new Run();
                yield return DriveLine(line, 99f, 40f, run, null, null, 5f, true, prof);
                BigJump.Launched -= hook;
                Check($"{sc.Name}: over its dirt pile, landed and back on the lap", launch > 0f && run.reached,
                      $"{(launch > 0f ? $"onto the lip at {launch:0.0} m/s (off it at {j.vmax:0.0}), {run.maxAir:0.00} s in the air, landed at ({run.firstLanding.x:0.0}, {run.firstLanding.z:0.0}) upright {run.upAtLanding:0.00}" : "NEVER LEFT THE PILE")}; {(run.reached ? $"back on the lap in {run.time:0.0} s" : "DID NOT GET BACK " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}");
            }
        }

        IEnumerator R33Hollow()
        {
            report.AppendLine(); report.AppendLine("=== Short of the road: in the hollow");
            UseRoute("chastain_ridge.json", "forward");
            yield return Vehicle("original");
            var j = BigJump.All.First(b => b.id == "RL");
            // a race reset there (the rule CarRecovery applies to R / Y in a race)
            RaceController.AiCount = 0; race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            var g = RaceRoute.Surface(RidgeHollow.x, RidgeHollow.y);
            respawn.PlaceAt(g + Vector3.up * 0.5f, 98f); race.NotifyTeleported();
            yield return new WaitForSecondsRealtime(1.5f);
            bool found = CarRecovery.Find(respawn, out var pos, out var yaw);
            if (found) { respawn.PlaceAt(pos, yaw); race.NotifyTeleported(); }
            yield return new WaitForSecondsRealtime(1f);
            var p = car.Body.position; float along = Vector3.Dot(Flat(p - j.lip), j.dir); var fwd = race.Route.PointAt(race.Route.GateS + race.Route.LapStationOf(p), out var lapF);
            float facing = Vector3.Angle(Flat(car.transform.forward), Flat(lapF));
            Check("a reset in the hollow (race): onto New Chastain Road past the landing, facing on", found && OffLine(p) < 4f && along > 105f && facing < 15f,
                  $"from ({g.x:0}, {g.z:0}) to ({p.x:0.0}, {p.z:0.0}), {along:0} m out from the lip, {OffLine(p):0.0} m from the line, {facing:0} degrees off the race's way");
            yield return Capture("hollow_after_reset");
            race.EndRace(); RaceController.AiCount = 3;
            yield return new WaitForSecondsRealtime(0.5f);
            // driven out: from the hollow's floor up the graded ramp along the embankment onto the carriageway
            var path = new List<Vector3>();
            foreach (var (a, b) in new[] { (new Vector2(3214f, 626f), new Vector2(3224f, 618f)), (new Vector2(3224f, 618f), new Vector2(3262f, 579f)), (new Vector2(3262f, 579f), new Vector2(3272f, 562f)) })
                for (float t = 0f; t < 1f; t += 0.05f) { var q = Vector2.Lerp(a, b, t); path.Add(RaceRoute.Surface(q.x, q.y)); }
            Place(path[0], path[2] - path[0], 4f); yield return new WaitForFixedUpdate();
            var run = new Run();
            yield return DriveLine(path, 12f, 30f, run, null, null, 4f);
            Check("the way out of the hollow: driven up the graded ramp onto New Chastain Road", run.reached, run.reached ? $"out in {run.time:0.0} s, slowest {run.minSpeed:0.0} m/s" : $"STOPPED {run.end} at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})");
            yield return Capture("hollow_way_out");
        }

        IEnumerator R33Race()
        {
            report.AppendLine(); report.AppendLine($"=== A race: 3 laps, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, the player parked off the course");
            RaceController.RouteOverride = "chastain_ridge.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 3;
            RaceController.AiCount = 3; RaceController.AiVehiclesOverride = null;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload();
            race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            Check("traffic stays on the course (open roads): only the barred side roads closed to it", !Traffic.Instance || Traffic.Instance.Level == 0 || Traffic.Instance.ClosedRoads > 0, Traffic.Instance ? $"{Traffic.Instance.ClosedRoads} road pieces closed, {race.Route.Closures.Count} barriers" : "no traffic");
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Capture("race_grid");
            respawn.PlaceAt(RaceRoute.Surface(3420f, 985f) + Vector3.up * 0.4f, 0f); race.NotifyTeleported();   // on the knoll, off the course
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            Time.timeScale = 2f; Time.maximumDeltaTime = 0.5f;
            float t0 = Time.realtimeSinceStartup, end = t0 + 9f * 60f; int nearTraffic = 0;
            while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished))
            {
                yield return new WaitForSecondsRealtime(0.5f);
                if (Traffic.Instance)
                    foreach (var e in ai) if (!e.Finished && Traffic.Instance.Cars.Any(c => c && c.gameObject.activeSelf && (c.Car.Body.position - e.Car.Body.position).sqrMagnitude < 15f * 15f)) nearTraffic++;
            }
            Time.timeScale = 1f;
            foreach (var e in ai)
            {
                var drv = e.Driver;
                Check($"{e.Name} ({e.VehicleName}) finishes the 3 laps", e.Finished,
                      $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, missed {e.MissedTotal}, shortcuts {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}, AI resets {(drv ? drv.RecoveryCount : -1)}");
            }
            report.AppendLine($"      AI within 15 m of a traffic car: {nearTraffic} half-second samples");
            race.EndRace();
        }

        IEnumerator R33Pics()
        {
            UseRoute("chastain_ridge.json", "forward");
            var j = BigJump.All.First(b => b.id == "RL");
            var side = Vector3.Cross(Vector3.up, j.dir).normalized;
            car.Body.position = j.lip + side * 40f + Vector3.up * 3f; car.Body.linearVelocity = Vector3.zero;   // out of the pictures
            // the take-off seen from the crest (the run-up's start)
            var crest = race.Route.PointAt(race.Route.GateS + race.Route.LapStationOf(j.lip) - 70f, out _);
            yield return Still("33_takeoff_from_the_crest", crest + Vector3.up * 3.5f, j.lip + Vector3.up * 1f, 55f);
            yield return Still("33_flight_from_the_side", j.lip + j.dir * 50f + side * -55f + Vector3.up * 14f, j.lip + j.dir * 50f, 70f);
            var land = j.lip + j.dir * 107f; land.y = RaceRoute.Surface(land.x, land.z).y;
            yield return Still("33_landing_from_new_chastain", land + new Vector3(0.47f, 0f, -0.88f) * 45f + Vector3.up * 3f, land, 62f);
            yield return Still("33_hollow_and_way_out", land - j.dir * 50f + side * 25f + Vector3.up * 6f, land - j.dir * 22f + side * -8f, 66f);
            // each shortcut's turn-off, from the lap 50 m before it, and its pile from the turn-off
            foreach (var sc in race.Route.Shortcuts)
            {
                var b = BigJump.All.FirstOrDefault(x => sc.Id.StartsWith(x.id.ToLower() + "_"));
                float s0 = race.Route.LapStationOf(sc.Path[0]);
                var at = race.Route.PointAt(race.Route.GateS + s0 - 45f, out var f);
                yield return Still($"33_{b.id}_turn_off", at + Vector3.up * 1.6f, sc.Path[Mathf.Min(10, sc.Path.Length - 1)] + Vector3.up * 1f, 62f);
                if (b) yield return Still($"33_{b.id}_pile", b.lip - b.dir * 30f + Vector3.up * 2f, b.lip + b.dir * 25f + Vector3.up * 3f, 62f);
            }
        }
    }
}
