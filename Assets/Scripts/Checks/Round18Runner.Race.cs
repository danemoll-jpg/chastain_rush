using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    public sealed partial class Round18Runner
    {
        // ---------------------------------------------------------------- the AI through P1 and R3 (Round15Runner's trial)

        IEnumerator Ai(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== The AI drivers' trials: each vehicle once through P1 and R3 as an AI driver (made to take it), 120 m before the turn-off at 18 m/s");
            Time.timeScale = 4f; Time.maximumDeltaTime = 1f;
            int prevCount = RaceController.AiCount;
            var small = new[] { "atv", "moto", "scrambler", "drifter", "mower" };
            var bad = new List<string>(); int runs = 0;
            foreach (var (file, direction, scId, vehicles) in new[] { ("noonday_creek.json", "forward", "p1_pipe", small.Where(ids.Contains).ToArray()), ("chastain_glen_loop.json", "reverse", "r3_trampoline", ids) })
            {
                UseRoute(file, direction);
                foreach (var id in vehicles)
                {
                    RaceController.AiCount = 1; RaceController.AiVehiclesOverride = new[] { id };
                    race.StartRace();
                    while (race.State != RaceController.Stage.Racing) yield return null;
                    var e = race.Entrants.First(x => !x.IsPlayer); var d = e.Driver;
                    var sc = race.AiShortcuts.First(x => x.Id == scId); var rs = race.Route.Shortcuts.First(x => x.Id == scId);
                    var p = race.AiRoad.At(sc.entryRoad - 120f, out var f); f = Vector3.ProjectOnPlane(f, Vector3.up).normalized;
                    d.RunIn = null; d.ForcedShortcut = sc;
                    var rot = Quaternion.LookRotation(f);
                    e.Car.Body.isKinematic = false; e.Car.Body.position = p + Vector3.up * 0.6f; e.Car.Body.rotation = rot; e.Car.transform.SetPositionAndRotation(e.Car.Body.position, rot);
                    e.Car.Body.linearVelocity = f * 18f; e.Car.Body.angularVelocity = Vector3.zero; e.Car.ClearSteering(); Physics.SyncTransforms();
                    e.ClockStarted = true; e.StartAt = e.LapStart = race.RaceClock; e.NextCheckpoint = rs.FirstForgiven; e.Finished = false; e.ActiveShortcut = null;
                    e.NotifyTeleported();
                    int rec0 = d.RecoveryCount, taken0 = e.ShortcutsTaken.Count; float pen0 = e.PenaltyTotal;
                    float t = 0f, worstTilt = 0f; bool back = false; var track = new List<string>();
                    while (t < 40f)
                    {
                        yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                        if (((int)(t / 0.1f)) != ((int)((t - Time.fixedDeltaTime) / 0.1f))) { var q = e.Car.Body.position; track.Add($"{t:0.0},{q.x:0.00},{q.y:0.00},{q.z:0.00},{e.Car.Body.linearVelocity.magnitude:0.0},{e.Car.GroundedWheels},{d.TargetSpeed:0.0},{(d.OnShortcut != null ? 1 : 0)}"); }
                        float s = race.AiRoad.Project(e.Car.Body.position, out float lat);
                        if (e.Car.GroundedWheels > 0) worstTilt = Mathf.Max(worstTilt, Vector3.Angle(e.Car.transform.up, Vector3.up));
                        float past = Mathf.Repeat(s - sc.exitRoad, race.AiRoad.Length);
                        if (e.ShortcutsTaken.Count > taken0 && d.OnShortcut == null && Mathf.Abs(lat) < 6f && past > 20f && past < 200f) { back = true; break; }
                    }
                    bool given = e.ShortcutsTaken.Count > taken0 && e.PenaltyTotal == pen0;
                    bool ok = back && given && d.RecoveryCount == rec0 && worstTilt < 75f; runs++;
                    if (!ok) { bad.Add($"{scId} {id}"); File.WriteAllLines(Path.Combine(outputDir, $"ai_{scId}_{id}.csv"), track); }
                    report.AppendLine($"  {VehicleProfile.Find(id).Name,-18} {rs.Name,-16} {(ok ? "OK  " : "FAIL")}  {t,5:0.0} s, shortcut given {(given ? "yes" : "no")}, recoveries {d.RecoveryCount - rec0}, worst tilt on the ground {worstTilt:0} deg{(back ? "" : $", ended at ({e.Car.Body.position.x:0},{e.Car.Body.position.z:0})")}");
                    d.ForcedShortcut = null;
                    race.EndRace();
                    yield return null;
                }
            }
            RaceController.AiCount = prevCount; RaceController.AiVehiclesOverride = null;
            Time.timeScale = 1f; Time.maximumDeltaTime = 0.3333333f;
            File.WriteAllText(Path.Combine(outputDir, "ai_trials_failed.txt"), string.Join("\n", bad));
            Check("every AI trial takes the shortcut cleanly and is back on the lap", bad.Count == 0, bad.Count > 0 ? $"{runs - bad.Count} of {runs}; not: {string.Join(", ", bad)}" : $"{runs} of {runs}");
        }

        // ---------------------------------------------------------------- the race on Noonday Creek

        IEnumerator Race()
        {
            report.AppendLine(); report.AppendLine("=== Noonday Creek: 3 laps, three AI drivers (random small vehicles); the player sits it out, parked off the trail");
            UseRoute("noonday_creek.json", "forward");
            RaceController.AiCount = 3; RaceController.AiVehiclesOverride = null;
            yield return Vehicle("original");   // a car: the race must put the player in an allowed vehicle
            race.StartRace();
            var pcfg = car.GetComponent<VehicleConfiguration>();
            Check("starting Noonday Creek in a car puts the player in a trail vehicle", race.RouteData.AllowsVehicle(pcfg.profileId), pcfg.profileId);
            while (race.State == RaceController.Stage.Countdown) yield return null;
            Check("the AI drivers are in trail vehicles", race.Entrants.Where(e => !e.IsPlayer).All(e => race.RouteData.AllowsVehicle(e.Car.GetComponent<VehicleConfiguration>().profileId)), string.Join(", ", race.Entrants.Where(e => !e.IsPlayer).Select(e => $"{e.Name} {e.VehicleName}")));
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture("NC_grid_go");
            // the player: the lead-in, then the lap three and a half times
            var path = new List<Vector3>();
            for (int i = 0; i + 1 < race.RouteData.lead_in.Length; i += 2) path.Add(RaceRoute.Ground(race.RouteData.lead_in[i], race.RouteData.lead_in[i + 1]));
            float L = race.Route.LapLength;
            for (float s = 0f; s < L * 3.4f; s += 2f) path.Add(race.Route.PointAt(s, out _));
            var x = Extra(); float c0 = x.creek_bed_s[0], c1 = x.creek_bed_s[1];
            var bedSpeed = new Dictionary<RaceEntrant, (float sum, int n, float min)>();
            var follow = new Dictionary<RaceEntrant, float>(); var longestFollow = new Dictionary<RaceEntrant, float>();
            int passes = 0; List<RaceEntrant> lastOrder = null; float sample = 0f; float frames = 0f; int frameN = 0;
            // the player sits this race out, parked off the trail (a scripted driver flat out between the trunks was the race's
            // problem, not the AI); the race is the three AI drivers'
            var park = race.Route.PointAt(race.Route.GateS + 300f, out var pf); park += Vector3.Cross(Vector3.up, pf).normalized * 25f;
            respawn.PlaceAt(RaceRoute.Ground(park.x, park.z) + Vector3.up * 0.5f, 0f); car.Body.isKinematic = true; race.NotifyTeleported();
            var run = new Run(); var drive = StartCoroutine(Idle());
            float time = 0f; int shotsTaken = 0;
            while (time < 480f && !race.Entrants.Where(e => !e.IsPlayer).All(e => e.Finished))
            {
                yield return null; time += Time.deltaTime; frames += Time.unscaledDeltaTime; frameN++;
                // (not used while the player sits out) a stuck player presses reset after 3 s, as Dan would (R / Y: CarRecovery)
                if (car.Body.isKinematic) playerSlow = 0f; else playerSlow = car.Body.linearVelocity.magnitude < 1f && race.State == RaceController.Stage.Racing ? playerSlow + Time.deltaTime : 0f;
                if (playerSlow > 3f)
                {
                    playerSlow = 0f; playerResets18++;
                    if (playerResets18 <= 12)   // what it was stuck on: the colliders within 2 m of its box
                    {
                        var at = car.Body.position; var near = Physics.OverlapBox(at + Vector3.up * 0.6f, new Vector3(1.5f, 1.0f, 2.5f), car.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
                        report.AppendLine($"    player stuck at ({at.x:0.0}, {at.y:0.0}, {at.z:0.0}) lap {race.Route.LapStationOf(at):0} m, up {car.transform.up.y:0.00}, wheels {car.GroundedWheels}, touching: {string.Join(", ", near.Where(c => c.attachedRigidbody != car.Body).Select(c => c.name).Distinct())}");
                    }
                    if (CarRecovery.Find(respawn, out var rp, out var ry)) respawn.PlaceAt(rp, ry); race.NotifyTeleported();
                }
                foreach (var e in race.Entrants)
                {
                    if (!e.Car || e.Finished) continue;
                    float st = race.Route.LapStationOf(e.Car.transform.position);   // from the gate
                    float abs = Mathf.Repeat(st + race.Route.GateS, L);
                    if (abs >= c0 && abs <= c1) { var v = Flat(e.Car.Body.linearVelocity).magnitude; bedSpeed.TryGetValue(e, out var b); bedSpeed[e] = (b.sum + v, b.n + 1, b.n == 0 ? v : Mathf.Min(b.min, v)); }
                    if (e.IsPlayer) continue;
                    // following: right behind another vehicle (2 to 8 m ahead along the trail, within 1.5 m across)
                    bool behind = race.Entrants.Any(o => o != e && o.Car && !o.Finished && IsBehind(e, o));
                    follow[e] = behind ? (follow.TryGetValue(e, out var fv) ? fv : 0f) + Time.deltaTime : 0f;
                    longestFollow[e] = Mathf.Max(longestFollow.TryGetValue(e, out var lf) ? lf : 0f, follow[e]);
                }
                sample += Time.deltaTime;
                if (sample > 0.5f && race.State == RaceController.Stage.Racing)
                {
                    sample = 0f; var order = race.Ordered();
                    if (lastOrder != null) for (int i = 0; i < order.Count; i++) if (lastOrder.IndexOf(order[i]) > i) passes++;
                    lastOrder = order;
                }
                if (shotsTaken == 0 && time > 40f) { shotsTaken++; yield return Capture("NC_racing_1"); }
                if (shotsTaken == 1 && time > 120f) { shotsTaken++; yield return Capture("NC_racing_2"); }
            }
            car.enabled = true; car.Body.isKinematic = false;
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture("NC_finish_panel");
            var final = race.Ordered();
            report.AppendLine("  Finishing order:");
            for (int i = 0; i < final.Count; i++)
            {
                var e = final[i];
                report.AppendLine($"   {i + 1}. {e.Name,-5} {e.VehicleName,-16} total {RaceController.Fmt(e.FinalTotal)}{(e.Estimated ? " (estimated)" : "")}  laps {string.Join(" / ", e.LapTimes.Select(RaceController.Fmt))}  penalties +{e.PenaltyTotal:0} s  shortcuts: {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}"
                                  + (e.Driver ? $"  recoveries {e.Driver.RecoveryCount}, longest without progress {e.Driver.LongestStall:0.0} s, longest right behind another {(longestFollow.TryGetValue(e, out var lf2) ? lf2 : 0f):0.0} s" : ""));
            }
            foreach (var kv in bedSpeed) report.AppendLine($"  creek bed under the bridges ({c1 - c0:0} m): {kv.Key.Name} {kv.Key.VehicleName} averaged {kv.Value.sum / Mathf.Max(1, kv.Value.n):0.0} m/s, slowest {kv.Value.min:0.0} m/s");
            report.AppendLine($"  the scripted player pressed reset {playerResets18} time(s) (stuck 3 s)");
            report.AppendLine($"  places changed {passes} times during the race; frame rate {frameN / Mathf.Max(frames, 0.01f):0} fps average ({Screen.width} x {Screen.height}, not measured at 3840 x 2160)");
            Check("all three AI drivers finish the 3 laps (none estimated)", race.Entrants.Where(e => !e.IsPlayer).All(e => e.Finished && !e.Estimated), string.Join(", ", race.Entrants.Where(e => !e.IsPlayer).Select(e => $"{e.Name} {(e.Finished ? (e.Estimated ? "estimated" : "finished") : "not finished")}")));
            Check("no AI driver stuck for more than 8 s", race.Entrants.Where(e => e.Driver).All(e => e.Driver.LongestStall <= 8f), string.Join(", ", race.Entrants.Where(e => e.Driver).Select(e => $"{e.Name} {e.Driver.LongestStall:0.0} s, {e.Driver.RecoveryCount} recoveries")));
            float lapTime = race.Entrants.Where(e => e.LapTimes.Count > 0).Select(e => e.LapTimes.Average()).DefaultIfEmpty(120f).Average();
            Check("no AI driver sits behind another for a whole lap", longestFollow.Values.DefaultIfEmpty(0f).Max() < lapTime, $"longest {longestFollow.Values.DefaultIfEmpty(0f).Max():0.0} s against a lap of about {lapTime:0} s");
            race.EndRace();
        }

        float playerSlow; int playerResets18;
        IEnumerator Idle() { yield break; }
        bool IsBehind(RaceEntrant e, RaceEntrant o)
        {
            var f = e.Car.transform.forward; f.y = 0f; f.Normalize();
            var d = o.Car.transform.position - e.Car.transform.position; d.y = 0f;
            float along = Vector3.Dot(d, f), across = Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Vector3.up, f)));
            return along > 2f && along < 8f && across < 1.5f;
        }

        // ---------------------------------------------------------------- item 7: the two pictures for Dan (R3)

        // From the approach (behind a vehicle at the slide's foot) and from the side with the flight path drawn: the deck to the
        // mat at 18 m/s, and the bounce from the middle of the mat over 1752 to the lane (Trampoline's own aim). The chase
        // camera is set aside for the pictures. Taken in the exe (the trees are drawn at run time only).
        IEnumerator Pics()
        {
            report.AppendLine(); report.AppendLine("=== R3 pictures for Dan: r3_approach.png, r3_side_flight.png");
            UseRoute("chastain_glen_loop.json", "reverse");
            var tr = FindAnyObjectByType<Trampoline>(); var sc = race.Route.Shortcuts.FirstOrDefault(x => x.Id == "r3_trampoline");
            if (tr == null || sc == null) { Check("R3 is there for the pictures", false); yield break; }
            yield return Vehicle("original");
            var T = tr.transform.position; var target = tr.target;
            var u = Flat(target - T).normalized;   // roughly east-south-east
            // the slide's foot: the line's point 33 m before the trampoline
            var line = sc.Path.ToList(); var cum = new float[line.Count]; for (int k = 1; k < line.Count; k++) cum[k] = cum[k - 1] + Flat(line[k] - line[k - 1]).magnitude;
            float sT = Project(line, cum, T, out _);
            var foot = At(line, cum, sT - 31f); var ahead = At(line, cum, sT - 27f);
            Place(foot, ahead - foot, 0f);
            yield return new WaitForSecondsRealtime(2f);
            car.Body.isKinematic = true;
            var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); if (chase) chase.enabled = false;
            // the flight path: off the deck at 18 m/s (level, 1.8 m over the mat, about 11 m out), then from the middle of the mat
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.SetColor("_BaseColor", new Color(1f, 0.2f, 0.85f));
            var lr = new GameObject("R3 flight path").AddComponent<LineRenderer>(); lr.sharedMaterial = mat; lr.widthMultiplier = 0.35f;
            var pts = new List<Vector3>();
            var deckEdge = At(line, cum, sT - 13f); deckEdge.y = tr.matY + 1.8f + 0.7f;
            var dir = Flat(T - deckEdge).normalized;
            for (float t = 0f; t <= 0.62f; t += 0.03f) pts.Add(deckEdge + dir * 18f * t + Vector3.down * 4.905f * t * t);
            var p0 = T + Vector3.up * 0.7f; var g = Physics.gravity; var v0 = (target - p0 - 0.5f * g * tr.flight * tr.flight) / tr.flight;
            for (float t = 0f; t <= tr.flight + 1e-3f; t += 0.05f) pts.Add(p0 + v0 * t + 0.5f * g * t * t);
            lr.positionCount = pts.Count; lr.SetPositions(pts.ToArray());
            float apex = pts.Max(p => p.y) - tr.matY;
            // 1: the approach, behind and above the vehicle at the slide's foot
            var back = Flat(foot - ahead).normalized;
            cam.transform.position = foot + back * 11f + Vector3.up * 4.5f; cam.transform.LookAt(T + Vector3.up * 3f);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture("r3_approach");
            // 2: from the side (south-west of the line, up high), the whole hop in view
            var side = Vector3.Cross(Vector3.up, u).normalized;   // left of the flight
            var mid = Vector3.Lerp(T, target, 0.45f); mid.y = tr.matY + 8f;
            cam.transform.position = mid - side * 70f - u * 18f + Vector3.up * 14f; cam.transform.LookAt(mid);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture("r3_side_flight");
            report.AppendLine($"  the flight drawn: off the deck at 18 m/s onto the mat, then from the mat's middle to ({target.x:0.0}, {target.z:0.0}) in {tr.flight:0.0} s, top {apex:0.0} m over the mat");
            Destroy(lr.gameObject); if (chase) chase.enabled = true; car.Body.isKinematic = false;
        }
    }
}
