using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    public sealed partial class Round19Runner
    {
        // ---------------------------------------------------------------- 5. R3's party hint

        IEnumerator R3Hint()
        {
            report.AppendLine(); report.AppendLine("=== 5. R3's turn-off: the party hint, seen in the reverse direction");
            UseRoute("chastain_glen_loop.json", "reverse");
            var sign = FindObjectsByType<FenceChunk>(FindObjectsSortMode.None).FirstOrDefault(c => c.transform.parent && c.transform.parent.name.StartsWith("Party sign"));
            var face = GameObject.Find("Party sign face r3_trampoline");
            Check("the PARTY sign (breakable, with its face and balloons) is at R3's turn-off", sign != null && face != null, sign ? sign.transform.parent.name : "missing");
            if (sign == null || face == null) yield break;
            var at = face.transform.position;
            float sSign = 0f, bestD = float.MaxValue;   // the lap point nearest the sign (scanned: the reverse lap's stations)
            for (float s = 0f; s < race.Route.LapLength; s += 1f) { float dd = Flat(race.Route.PointAt(s, out _) - at).magnitude; if (dd < bestD) { bestD = dd; sSign = s; } }
            car.Body.isKinematic = true; car.Body.position += Vector3.down * 50f;
            foreach (float back in new[] { 60f, 30f })
            {
                var p0 = race.Route.PointAt(sSign, out _); var road = Flat(p0 - race.Route.PointAt(sSign - 20f, out _)).normalized;   // the road's direction coming up to the sign
                var eye = p0 - road * back; eye = RaceRoute.Ground(eye.x, eye.z) + Vector3.up * 1.5f;
                yield return Still($"5_R3_party_hint_from_{back:0}m", eye, Vector3.Lerp(at, race.Route.PointAt(sSign + 5f, out _) + Vector3.up * 1.6f, 0.5f), 55f);
            }
            var sc = race.Route.Shortcuts.First(s => s.Id == "r3_trampoline");
            var gapView = sc.Path[Mathf.Min(sc.Path.Length - 1, 18)];
            yield return Still("5_R3_tracks_through_the_gap", at + Vector3.up * 2.5f - Flat(gapView - at).normalized * 4f, gapView + Vector3.up * 1f, 65f);
            car.Body.isKinematic = false;
            // breakable: drive into it
            yield return Vehicle("atv");
            int b0 = FenceChunk.BrokenPanels;
            var from = at + Flat(race.Route.PointAt(sSign - 14f, out _) - at).normalized * 14f; from = RaceRoute.Ground(from.x, from.z);
            Place(from, at - from, 12f); yield return new WaitForFixedUpdate();
            car.enabled = false;
            for (float t = 0f; t < 2.5f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                float aim = Vector3.SignedAngle(Flat(car.transform.forward), Flat(at - car.Body.position), Vector3.up);
                car.Simulate(car.ForwardSpeed < 12f ? 1f : 0f, 0f, FenceChunk.BrokenPanels > b0 ? 0f : Mathf.Clamp(aim / 15f, -1f, 1f), Time.fixedDeltaTime);
            }
            car.enabled = true;
            Check("the sign and its balloons break when hit and the vehicle drives on", FenceChunk.BrokenPanels > b0 && Flat(car.Body.linearVelocity).magnitude > 6f, $"panels broken {FenceChunk.BrokenPanels - b0}, {Flat(car.Body.linearVelocity).magnitude:0.0} m/s after");
            FenceChunk.RestoreAll();
        }

        // ---------------------------------------------------------------- 6. R1, the kids' ramp

        IEnumerator R1(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== 6. R1 (a trial): each vehicle along its line (the reverse loop), up to 24 m/s, the line's own speeds through the bend");
            UseRoute("chastain_glen_loop.json", "reverse");
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "r1_kids_ramp"); var bj = BigJump.All.FirstOrDefault(j => j.look == "kids_ramp");
            Check("R1 is an official shortcut of the reverse loop, forgiving the reverse lap's CP1; the ramp is there", sc != null && bj != null && race.Route.Checkpoints[sc.FirstForgiven].DataId == 5, sc != null ? $"forgives reverse CP{sc.FirstForgiven + 1} (file CP{race.Route.Checkpoints[sc.FirstForgiven].DataId})" : "missing");
            if (sc == null || bj == null) yield break;
            var line = sc.Path.ToList(); var bad = new List<string>(); bool shot = false;
            float launch = -1f; System.Action<BigJump, ArcadeVehicle, float> hook = (j, c, v) => { if (c == car && j == bj) launch = v; };
            BigJump.Launched += hook;
            foreach (var id in ids)
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
                launch = -1f; float top = -99f, flight = 0f, jumpAir = 0f; Vector3 landAt = new Vector3(float.NaN, 0f, 0f); float upAt = -1f;
                var run = new Run(); var drive = StartCoroutine(DriveLine(line, 24f, 40f, run, bj.lip, null, 3.5f, true, sc.AiProfile));
                while (!run.reached && run.end == "" && run.time < 39.9f)
                {
                    if (launch > 0f && float.IsNaN(landAt.x)) { top = Mathf.Max(top, car.Body.position.y - bj.lip.y); flight += Time.deltaTime; if (car.GroundedWheels >= 2 && flight > 0.3f) { landAt = car.Body.position; upAt = car.transform.up.y; jumpAir = flight; } }
                    if (!shot && launch > 0f && car.GroundedWheels == 0 && Vector3.Dot(Flat(car.Body.position - bj.lip), bj.dir) > 10f) { shot = true; yield return Capture("6_R1_in_the_air"); }
                    yield return null;
                }
                yield return drive;
                float dist = float.IsNaN(landAt.x) ? -1f : Vector3.Dot(Flat(landAt - bj.lip), bj.dir);
                run.maxAir = jumpAir; run.upAtLanding = upAt;
                bool ok = run.reached && launch > 0f && upAt > 0.7f;
                if (!ok) { bad.Add(Nm(id)); File.WriteAllLines(Path.Combine(outputDir, $"r1_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                report.AppendLine($"  {Nm(id),-18} off the lip at {launch,5:0.0} m/s, {run.maxAir:0.00} s in the air, down {dist,5:0.0} m past the lip, top {top,4:0.0} m over it, upright {run.upAtLanding:0.00}, {(run.reached ? "on onto the lap" : "DID NOT GET ON " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}");
            }
            BigJump.Launched -= hook;
            Check("every vehicle over the kids' ramp, down on its wheels, and on onto the lap", bad.Count == 0, bad.Count > 0 ? "not: " + string.Join(", ", bad) : "all");
            // slow: 8 m/s off the lip, into the yard, drives out onto the lap
            yield return Vehicle("original");
            Place(line[0], line[2] - line[0], 8f);
            var slow = new Run(); yield return DriveLine(line, 8f, 50f, slow, bj.lip, null, 3.5f);
            Check("slow (8 m/s): off the ramp into the yard and out onto the lap", slow.reached, $"{slow.speedAt:0.0} m/s at the lip, {(slow.reached ? "on the lap" : "STOPPED " + slow.end)}");
            // the picture: from the approach, the flight drawn (24 m/s into the ramp, held to 20 at the lip; and 14 m/s)
            car.Body.isKinematic = true; car.Body.position += Vector3.down * 50f;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.SetColor("_BaseColor", new Color(1f, 0.2f, 0.85f));
            var lrs = new List<GameObject>();
            float th = 25f * Mathf.Deg2Rad;
            foreach (float v in new[] { 20f, 14f })
            {
                var lr = new GameObject("R1 flight " + v).AddComponent<LineRenderer>(); lr.sharedMaterial = mat; lr.widthMultiplier = v > 15f ? 0.35f : 0.2f; lrs.Add(lr.gameObject);
                var pts = new List<Vector3>(); var p0 = bj.lip + Vector3.up * 0.6f;
                for (float t = 0f; t < 4f; t += 0.04f) { var q = p0 + bj.dir * v * Mathf.Cos(th) * t + Vector3.up * (v * Mathf.Sin(th) * t - 4.905f * t * t); pts.Add(q); if (t > 0.2f && q.y < RaceRoute.Ground(q.x, q.z).y + 0.6f) break; }
                lr.positionCount = pts.Count; lr.SetPositions(pts.ToArray());
            }
            var app = bj.lip - bj.dir * 26f + Vector3.Cross(Vector3.up, bj.dir) * 7f; app = RaceRoute.Ground(app.x, app.z) + Vector3.up * 3.5f;
            yield return Still("6_R1_from_the_approach_with_the_flight", app, bj.lip + bj.dir * 16f, 66f);
            var side = bj.lip + bj.dir * 16f + Vector3.Cross(Vector3.up, bj.dir) * 30f + Vector3.up * 16f;
            yield return Still("6_R1_from_the_side_with_the_flight", side, bj.lip + bj.dir * 18f + Vector3.up * 2f, 60f);
            foreach (var g in lrs) Destroy(g);
            car.Body.isKinematic = false;
        }

        // ---------------------------------------------------------------- the AI's trials: P1 (the new round pipe), R1

        IEnumerator Ai(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== The AI drivers' trials: each trail vehicle once through P1, each of the eleven once over R1, as an AI driver made to take it");
            Time.timeScale = 4f; Time.maximumDeltaTime = 1f;
            int prevCount = RaceController.AiCount;
            var bad = new List<string>(); int runs = 0;
            foreach (var (file, direction, scId, vehicles) in new[] { ("noonday_creek.json", "forward", "p1_pipe", Small.Where(ids.Contains).ToArray()), ("chastain_glen_loop.json", "reverse", "r1_kids_ramp", ids) })
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
                    while (t < 45f)
                    {
                        yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                        if (((int)(t / 0.1f)) != ((int)((t - Time.fixedDeltaTime) / 0.1f))) { var q = e.Car.Body.position; track.Add($"{t:0.0},{q.x:0.00},{q.y:0.00},{q.z:0.00},{e.Car.Body.linearVelocity.magnitude:0.0},{e.Car.GroundedWheels},{d.TargetSpeed:0.0},{(d.OnShortcut != null ? 1 : 0)}"); }
                        float s = race.AiRoad.Project(e.Car.Body.position, out float lat);
                        if (e.Car.GroundedWheels > 0 && DrainPipe.Inside(e.Car.Body.position, out _) == null) worstTilt = Mathf.Max(worstTilt, Vector3.Angle(e.Car.transform.up, Vector3.up));
                        float past = Mathf.Repeat(s - sc.exitRoad, race.AiRoad.Length);
                        if (e.ShortcutsTaken.Count > taken0 && d.OnShortcut == null && Mathf.Abs(lat) < 6f && past > 20f && past < 200f) { back = true; break; }
                    }
                    bool given = e.ShortcutsTaken.Count > taken0 && e.PenaltyTotal == pen0;
                    bool ok = back && given && d.RecoveryCount == rec0 && worstTilt < 75f; runs++;
                    if (!ok) { bad.Add($"{scId} {id}"); File.WriteAllLines(Path.Combine(outputDir, $"ai_{scId}_{id}.csv"), track); }
                    report.AppendLine($"  {Nm(id),-18} {rs.Name,-16} {(ok ? "OK  " : "FAIL")}  {t,5:0.0} s, shortcut given {(given ? "yes" : "no")}, recoveries {d.RecoveryCount - rec0}, worst tilt on the ground {worstTilt:0} deg{(back ? "" : $", ended at ({e.Car.Body.position.x:0},{e.Car.Body.position.z:0})")}");
                    d.ForcedShortcut = null;
                    race.EndRace();
                    yield return null;
                }
            }
            RaceController.AiCount = prevCount; RaceController.AiVehiclesOverride = null;
            Time.timeScale = 1f; Time.maximumDeltaTime = 0.3333333f;
            Check("every AI trial takes the shortcut cleanly and is back on the lap", bad.Count == 0, bad.Count > 0 ? $"{runs - bad.Count} of {runs}; not: {string.Join(", ", bad)}" : $"{runs} of {runs}");
        }

        // ---------------------------------------------------------------- the race on Noonday Creek (the AI retuned to the wider trail)

        IEnumerator Race()
        {
            report.AppendLine(); report.AppendLine("=== Noonday Creek: 3 laps, three AI drivers (random trail vehicles); the player parked off the trail");
            UseRoute("noonday_creek.json", "forward");
            RaceController.AiCount = 3; RaceController.AiVehiclesOverride = null;
            yield return Vehicle("atv");
            race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            float L = race.Route.LapLength;
            var follow = new Dictionary<RaceEntrant, float>(); var longestFollow = new Dictionary<RaceEntrant, float>();
            int passes = 0; List<RaceEntrant> lastOrder = null; float sample = 0f; float frames = 0f; int frameN = 0; int gov0 = BigJump.Governed, snap0 = TreeTrunks.Snapped;
            var park = race.Route.PointAt(race.Route.GateS + 300f, out var pf); park += Vector3.Cross(Vector3.up, pf).normalized * 25f;
            respawn.PlaceAt(RaceRoute.Ground(park.x, park.z) + Vector3.up * 0.5f, 0f); car.Body.isKinematic = true; race.NotifyTeleported();
            float time = 0f; int shotsTaken = 0;
            while (time < 480f && !race.Entrants.Where(e => !e.IsPlayer).All(e => e.Finished))
            {
                yield return null; time += Time.deltaTime; frames += Time.unscaledDeltaTime; frameN++;
                foreach (var e in race.Entrants)
                {
                    if (!e.Car || e.Finished || e.IsPlayer) continue;
                    bool behind = race.Entrants.Any(o => o != e && o.Car && !o.Finished && !o.IsPlayer && IsBehind(e, o));
                    follow[e] = behind ? (follow.TryGetValue(e, out var fv) ? fv : 0f) + Time.deltaTime : 0f;
                    longestFollow[e] = Mathf.Max(longestFollow.TryGetValue(e, out var lf) ? lf : 0f, follow[e]);
                }
                sample += Time.deltaTime;
                if (sample > 0.5f && race.State == RaceController.Stage.Racing)
                {
                    sample = 0f; var order = race.Ordered().Where(x => !x.IsPlayer).ToList();
                    if (lastOrder != null) for (int i = 0; i < order.Count; i++) if (lastOrder.IndexOf(order[i]) > i) passes++;
                    lastOrder = order;
                }
                if (shotsTaken == 0 && time > 40f) { shotsTaken++; yield return Capture("NC_racing_1"); }
                if (shotsTaken == 1 && time > 120f) { shotsTaken++; yield return Capture("NC_racing_2"); }
            }
            car.Body.isKinematic = false;
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture("NC_finish_panel");
            var final = race.Ordered().Where(x => !x.IsPlayer).ToList();
            report.AppendLine("  Finishing order:");
            for (int i = 0; i < final.Count; i++)
            {
                var e = final[i];
                report.AppendLine($"   {i + 1}. {e.Name,-5} {e.VehicleName,-16} total {RaceController.Fmt(e.FinalTotal)}{(e.Estimated ? " (estimated)" : "")}  laps {string.Join(" / ", e.LapTimes.Select(RaceController.Fmt))}  penalties +{e.PenaltyTotal:0} s  shortcuts: {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}"
                                  + (e.Driver ? $"  recoveries {e.Driver.RecoveryCount}, longest without progress {e.Driver.LongestStall:0.0} s, longest right behind another {(longestFollow.TryGetValue(e, out var lf2) ? lf2 : 0f):0.0} s" : ""));
            }
            report.AppendLine($"  places changed {passes} times; launches held to a jump's top speed {BigJump.Governed - gov0}; trees snapped {TreeTrunks.Snapped - snap0}; frame rate {frameN / Mathf.Max(frames, 0.01f):0} fps average ({Screen.width} x {Screen.height}, not measured at 3840 x 2160)");
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            Check("all three AI drivers finish the 3 laps (none estimated)", ai.All(e => e.Finished && !e.Estimated), string.Join(", ", ai.Select(e => $"{e.Name} {(e.Finished ? (e.Estimated ? "estimated" : "finished") : "not finished")}")));
            Check("no AI driver stuck for more than 8 s", ai.All(e => e.Driver.LongestStall <= 8f), string.Join(", ", ai.Select(e => $"{e.Name} {e.Driver.LongestStall:0.0} s, {e.Driver.RecoveryCount} recoveries")));
            Check("far fewer recoveries than Round 18 (1 to 9 each): at most 2 each", ai.All(e => e.Driver.RecoveryCount <= 2), string.Join(", ", ai.Select(e => $"{e.Name} {e.Driver.RecoveryCount}")));
            Check("no AI penalties", ai.All(e => e.PenaltyTotal == 0f), string.Join(", ", ai.Select(e => $"{e.Name} +{e.PenaltyTotal:0} s")));
            float lapTime = ai.Where(e => e.LapTimes.Count > 0).Select(e => e.LapTimes.Average()).DefaultIfEmpty(120f).Average();
            Check("real passing: places change, and nobody sits behind another for a whole lap", passes > 0 && longestFollow.Values.DefaultIfEmpty(0f).Max() < lapTime, $"{passes} place changes; longest right behind another {longestFollow.Values.DefaultIfEmpty(0f).Max():0.0} s against a lap of about {lapTime:0} s");
            race.EndRace();
        }

        bool IsBehind(RaceEntrant e, RaceEntrant o)
        {
            var f = e.Car.transform.forward; f.y = 0f; f.Normalize();
            var d = o.Car.transform.position - e.Car.transform.position; d.y = 0f;
            float along = Vector3.Dot(d, f), across = Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Vector3.up, f)));
            return along > 2f && along < 8f && across < 1.5f;
        }
    
        // ---------------------------------------------------------------- the AI passing on the wider trail
        // two AI drivers on Noonday Creek: the one ahead held to 40 % of its top speed (about 15 m/s: on the winding trail the bends,
        // not top speed, limit both, so a smaller cut is no slower there), the other 15 m behind it on the start
        // straight; it must get past (by the passing lane or a squeeze), neither stuck nor recovered
        IEnumerator Pass()
        {
            report.AppendLine(); report.AppendLine("=== The AI passing: a driver held to 40 % of its top speed ahead, a normal one 15 m behind (Noonday Creek, from the start line)");
            UseRoute("noonday_creek.json", "forward");
            var results = new List<string>(); bool all = true;
            foreach (var (lead, chase) in new[] { ("atv", "moto"), ("mower", "scrambler"), ("drifter", "atv") })
            {
                RaceController.AiCount = 2; RaceController.AiVehiclesOverride = new[] { lead, chase };
                race.StartRace();
                while (race.State != RaceController.Stage.Racing) yield return null;
                car.Body.isKinematic = true; car.Body.position += Vector3.down * 50f;
                var ai = race.Entrants.Where(x => !x.IsPlayer).ToList();
                var a = ai.First(x => x.Car.GetComponent<VehicleConfiguration>().profileId == lead); var b = ai.First(x => x != a);
                float g = race.Route.GateS; float top0 = a.Car.topSpeed; a.Car.topSpeed = top0 * 0.4f;
                foreach (var (e, ds) in new[] { (a, 25f), (b, 10f) })
                {
                    var p = race.Route.PointAt(g + ds, out var f); var rot = Quaternion.LookRotation(f);
                    e.Car.Body.position = p + Vector3.up * 0.6f; e.Car.Body.rotation = rot; e.Car.transform.SetPositionAndRotation(e.Car.Body.position, rot);
                    e.Car.Body.linearVelocity = f * 12f; e.Car.Body.angularVelocity = Vector3.zero; e.Car.ClearSteering(); e.Driver.RunIn = null; e.NotifyTeleported();
                }
                Physics.SyncTransforms();
                int ra = a.Driver.RecoveryCount, rb = b.Driver.RecoveryCount; float t = 0f; bool passed = false;
                while (t < 45f && !passed)
                {
                    yield return null; t += Time.deltaTime;
                    float sa = race.Route.LapStationOf(a.Car.transform.position), sb = race.Route.LapStationOf(b.Car.transform.position);
                    if (sb > sa + 4f && sb - sa < 500f) passed = true;
                }
                a.Car.topSpeed = top0;
                bool ok = passed && a.Driver.RecoveryCount == ra && b.Driver.RecoveryCount == rb;
                if (!ok) all = false;
                results.Add($"{Nm(chase)} behind a slowed {Nm(lead)}: {(passed ? $"past in {t:0.0} s" : "NOT PAST in 45 s")}, recoveries {a.Driver.RecoveryCount - ra} / {b.Driver.RecoveryCount - rb}");
                race.EndRace(); car.Body.isKinematic = false; yield return null;
            }
            RaceController.AiCount = 3; RaceController.AiVehiclesOverride = null;
            Check("a faster AI driver gets past a slower one on the trail, nobody recovered", all, string.Join("; ", results));
        }
    }
}
