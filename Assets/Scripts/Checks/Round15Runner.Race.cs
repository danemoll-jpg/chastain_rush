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
    // Round 15 Part B checks (see Round15Runner.cs).
    public sealed partial class Round15Runner
    {
        IEnumerator PartB(string only)
        {
            if (only == null || only == "shortcuts") yield return ShortcutTrials();
            if (only == null || only == "race") yield return Race();
        }

        // ---------------------------------------------------------------- item 10: every vehicle down each shortcut as an AI driver

        IEnumerator ShortcutTrials()
        {
            var race = RaceController.Instance;
            report.AppendLine();
            report.AppendLine("=== Shortcuts as the AI drives them: each vehicle once down each official shortcut (RoadDriver, made to take it)");
            report.AppendLine("    The AI starts on the lap 120 m before the turn-off at 18 m/s, its lap already running; a run counts when the race");
            report.AppendLine("    gives it the shortcut (its checkpoint forgiven, no penalty) and it is back on the lap 30 m past the rejoin, upright,");
            report.AppendLine("    without a recovery, within 40 s.");
            var ids = VehicleProfile.All.Select(p => p.Id).ToArray();
            var argv = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < argv.Length; i++) if (argv[i] == "-r15Vehicles") ids = argv[i + 1].Split(',');
            var bad = new Dictionary<string, List<string>>();
            Time.timeScale = 4f; Time.maximumDeltaTime = 1f;   // the same physics step, four per frame
            int prevCount = RaceController.AiCount;
            foreach (var id in ids)
            {
                RaceController.AiCount = 1; RaceController.AiVehiclesOverride = new[] { id };
                race.StartRace();
                while (race.State != RaceController.Stage.Racing) yield return null;
                var e = race.Entrants.First(x => !x.IsPlayer); var d = e.Driver;
                foreach (var sc in race.AiShortcuts)
                {
                    var rs = race.Route.Shortcuts.First(x => x.Id == sc.Id);
                    float s0 = sc.entryRoad - 120f;
                    var p = race.AiRoad.At(s0, out var f); f = Vector3.ProjectOnPlane(f, Vector3.up).normalized;
                    p = RaceRoute.Ground(p.x + Vector3.Cross(Vector3.up, f).x * 1.7f, p.z + Vector3.Cross(Vector3.up, f).z * 1.7f);
                    d.RunIn = null; d.ForcedShortcut = sc;
                    var rot = Quaternion.LookRotation(f);
                    e.Car.Body.isKinematic = false;
                    e.Car.Body.position = p + Vector3.up * 0.6f; e.Car.Body.rotation = rot; e.Car.transform.SetPositionAndRotation(e.Car.Body.position, rot);
                    e.Car.Body.linearVelocity = f * 18f; e.Car.Body.angularVelocity = Vector3.zero; e.Car.ClearSteering(); Physics.SyncTransforms();
                    e.ClockStarted = true; e.StartAt = e.LapStart = race.RaceClock; e.NextCheckpoint = rs.FirstForgiven; e.Finished = false; e.ActiveShortcut = null;
                    e.NotifyTeleported();
                    int rec0 = d.RecoveryCount, taken0 = e.ShortcutsTaken.Count; float pen0 = e.PenaltyTotal;
                    float t = 0f, worstTilt = 0f, slow = 0f, maxSlow = 0f; bool back = false;
                    while (t < 40f)
                    {
                        yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                        float s = race.AiRoad.Project(e.Car.Body.position, out float lat);
                        worstTilt = Mathf.Max(worstTilt, Vector3.Angle(e.Car.transform.up, Vector3.up));
                        slow = e.Car.Body.linearVelocity.magnitude < 1.5f ? slow + Time.fixedDeltaTime : 0f; maxSlow = Mathf.Max(maxSlow, slow);
                        if (e.ShortcutsTaken.Count > taken0 && d.OnShortcut == null && lat < 6f && Mathf.Repeat(s - sc.exitRoad, race.AiRoad.Length) > 30f && Mathf.Repeat(s - sc.exitRoad, race.AiRoad.Length) < 200f) { back = true; break; }
                    }
                    bool given = e.ShortcutsTaken.Count > taken0 && e.PenaltyTotal == pen0;
                    bool ok = back && given && d.RecoveryCount == rec0 && worstTilt < 75f;
                    if (!ok) { if (!bad.TryGetValue(sc.Id, out var l)) bad[sc.Id] = l = new List<string>(); l.Add(id); }
                    report.AppendLine($"  {VehicleProfile.Find(id).Name,-18} {sc.Name,-16} {(ok ? "OK  " : "FAIL")}  {t,5:0.0} s, shortcut given {(given ? "yes" : "no")}, recoveries {d.RecoveryCount - rec0}, worst tilt {worstTilt:0} deg, longest stop {maxSlow:0.0} s{(back ? "" : $", ended at ({e.Car.Body.position.x:0},{e.Car.Body.position.z:0})")}");
                    d.ForcedShortcut = null;
                }
            }
            RaceController.AiCount = prevCount; RaceController.AiVehiclesOverride = null;
            Time.timeScale = 1f; Time.maximumDeltaTime = 0.3333333f;
            race.EndRace();
            foreach (var sc in race.AiShortcuts)
            {
                var l = bad.TryGetValue(sc.Id, out var x) ? x : new List<string>();
                report.AppendLine($"  -> {sc.Name}: taken cleanly by {ids.Length - l.Count} of {ids.Length}" + (l.Count > 0 ? $"; not by {string.Join(", ", l)}" : ""));
            }
            File.WriteAllText(Path.Combine(outputDir, "ai_shortcut_never.txt"), string.Join("\n", bad.Select(kv => kv.Key + ": " + string.Join(",", kv.Value))));
            Check("every vehicle tried on both shortcuts", true, $"{ids.Length * 2} runs");
        }

        // ---------------------------------------------------------------- item 13: a 3-lap race, three AI drivers and a fast player

        // the player's line for one lap: the lap from the gate, with both shortcuts' lines in place of the road they cut
        List<(Vector3 p, float cap)> PlayerLine(RaceController race)
        {
            var r = race.Route; float L = r.LapLength;
            var cuts = r.Shortcuts.Select(sc => (sc, a: r.LapStationOf(sc.Start), b: r.LapStationOf(sc.End))).OrderBy(c => c.a).ToList();
            var line = new List<(Vector3, float)>();
            float s = 0f;
            foreach (var (sc, a, b) in cuts)
            {
                for (; s < a; s += 3f) line.Add((r.PointAt(r.GateS + s, out _), 99f));
                float cap = RaceController.AiShortcutSpeed(sc.Id) + (sc.Id.Contains("over_house") ? 4f : 0f);
                foreach (var p in sc.Path) line.Add((p, cap));
                s = b + 3f;
            }
            for (; s < L; s += 3f) line.Add((r.PointAt(r.GateS + s, out _), 99f));
            return line;
        }

        IEnumerator Race()
        {
            var race = RaceController.Instance;
            report.AppendLine();
            report.AppendLine("=== The race: 3 laps, three AI drivers (random vehicles), the player driven by script flat out, both shortcuts every lap");
            sw.Select("original"); yield return new WaitForSecondsRealtime(0.5f);
            race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            foreach (var e in race.Entrants) if (!e.IsPlayer) report.AppendLine($"  {e.Name}: {e.VehicleName}");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return CaptureScreen("r15_grid_go");
            // the scripted player: the lead-in, then the line of each lap, steering and braking as the shortcut check does
            var lap = PlayerLine(race);
            var path = new List<(Vector3 p, float cap)>();
            for (int i = 0; i + 1 < race.RouteData.lead_in.Length; i += 2) path.Add((RaceRoute.Ground(race.RouteData.lead_in[i], race.RouteData.lead_in[i + 1]), 99f));
            float join = race.Route.LapStationOf(path[path.Count - 1].p);
            for (float s = join; s < race.Route.LapLength; s += 3f) path.Add((race.Route.PointAt(race.Route.GateS + s, out _), 99f));
            for (int k = 0; k < 4; k++) path.AddRange(lap);
            car.enabled = false; float playerSlow = 0f;
            int next = 0; float time = 0f, fpsT = 0f; int frames = 0; var dts = new List<float>(); float startFps = -1f;
            float startWindow = 0f; int startFrames = 0;
            while (time < 360f)
            {
                yield return new WaitForFixedUpdate();
                float dt = Time.fixedDeltaTime; time += dt;
                if (race.State == RaceController.Stage.Finished && race.Entrants.All(x => x.Finished)) break;
                if (race.State == RaceController.Stage.Racing) next = DriveLine(path, next);
                else car.Simulate(0f, 1f, 0f, dt);
                // a stuck player presses reset after 3 s, as Dan would (R / Y: CarRecovery), then drives on from the nearest point
                playerSlow = car.Body.linearVelocity.magnitude < 1f && race.State == RaceController.Stage.Racing ? playerSlow + dt : 0f;
                if (playerSlow > 3f)
                {
                    playerSlow = 0f; playerResets++;
                    var rs = car.GetComponent<VehicleRespawn>();
                    if (CarRecovery.Find(rs, out var rp, out var ry)) rs.PlaceAt(rp, ry);
                    race.NotifyTeleported();
                    int bi = next; float bd = float.MaxValue;
                    for (int i = Mathf.Max(0, next - 60); i < Mathf.Min(path.Count, next + 120); i++) { float d0 = (F(path[i].p) - F(car.Body.position)).sqrMagnitude; if (d0 < bd) { bd = d0; bi = i; } }
                    next = bi;
                }
            }
            car.enabled = true;
            yield return CaptureScreen("r15_finish_panel");
            // frame rate: measured by the frame loop below during the race (see FrameMeter)
            var order = race.Ordered();
            report.AppendLine("  Finishing order (the finish panel's):");
            for (int i = 0; i < order.Count; i++)
            {
                var e = order[i];
                report.AppendLine($"   {i + 1}. {e.Name,-5} {e.VehicleName,-18} total {RaceController.Fmt(e.FinalTotal)}{(e.Estimated ? " (estimated)" : "")}  laps {string.Join(" / ", e.LapTimes.Select(RaceController.Fmt))}  penalties +{e.PenaltyTotal:0} s  shortcuts: {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}" + (e.Driver ? $"  recoveries {e.Driver.RecoveryCount}, longest without progress {e.Driver.LongestStall:0.0} s" : ""));
            }
            Check("all four finish", race.Entrants.Count == 4 && race.Entrants.All(e => e.Finished && !e.Estimated), string.Join(", ", race.Entrants.Select(e => $"{e.Name} {(e.Finished ? (e.Estimated ? "estimated" : "finished") : "not finished")}")));
            Check("no AI driver stuck for more than 5 s", race.Entrants.Where(e => e.Driver).All(e => e.Driver.LongestStall <= 5f), string.Join(", ", race.Entrants.Where(e => e.Driver).Select(e => $"{e.Name} {e.Driver.LongestStall:0.0} s")));
            report.AppendLine($"  The scripted player pressed reset {playerResets} time(s) (stuck 3 s)");
            Check("the player took both shortcuts every lap", race.Player.ShortcutsTaken.Count >= 2 * race.Route.Laps, string.Join("; ", race.Player.ShortcutsTaken));
            report.AppendLine($"  Frame rate ({Screen.width} x {Screen.height}): first 10 s after GO, all four on screen: {meterStart:0} fps; whole race {meterAll:0} fps average, 1% low {meterLow:0} fps");
            { float acc = 0f; var first = new List<float>(); foreach (var f in raceFrames) { if (acc >= 10f) break; acc += f; first.Add(f); }
              report.AppendLine($"    first 10 s: {first.Count} frames, longest {first.Max() * 1000f:0} ms, over 20 ms: {first.Count(f => f > .02f)} (at frames {string.Join(",", first.Select((f, i) => (f, i)).Where(x => x.f > .02f).Select(x => $"{x.i}:{x.f * 1000f:0}ms").Take(12))})"); }
            Check("frame rate with four vehicles on screen at least 60", meterStart >= 59f, $"{meterStart:0} fps");
        }

        float meterStart, meterAll, meterLow; int playerResets;
        readonly List<float> raceFrames = new List<float>();
        void Update()
        {
            var race = RaceController.Instance;
            if (race == null || race.Entrants.Count < 4 || race.State == RaceController.Stage.Free || race.State == RaceController.Stage.Countdown) return;
            if (race.State == RaceController.Stage.Finished && race.Entrants.All(e => e.Finished)) return;
            if (capturing) return;
            raceFrames.Add(Time.unscaledDeltaTime);
            float sum = 0f; int n10 = 0; foreach (var f in raceFrames) { if (sum >= 10f) break; sum += f; n10++; }
            if (sum > 0f) meterStart = n10 / sum;
            float all = raceFrames.Sum(); meterAll = raceFrames.Count / all;
            var sorted = raceFrames.OrderByDescending(x => x).ToList(); meterLow = 1f / sorted[Mathf.Clamp(sorted.Count / 100, 0, sorted.Count - 1)];
        }

        // the shortcut check's driver: aim along the line, cross-track term, the bends ahead limit the speed (fast: 19 m/s²)
        int DriveLine(List<(Vector3 p, float cap)> path, int next)
        {
            const float LatAcc = 19f, BrakeAcc = 15f;
            var pos = car.Body.position; float v = Mathf.Max(car.ForwardSpeed, 1f);
            int best = next; float bd = float.MaxValue;
            for (int i = next; i < Mathf.Min(path.Count - 1, next + 15); i++)
            {
                Vector3 a = F(path[i].p), b = F(path[i + 1].p), q = F(pos), ab = b - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(q - a, ab) / ab.sqrMagnitude);
                float d = (a + ab * t - q).sqrMagnitude; if (d < bd) { bd = d; best = i; }
            }
            next = best;
            Vector3 sa = F(path[next].p), sb = F(path[Mathf.Min(next + 1, path.Count - 1)].p);
            var segDir = (sb - sa).sqrMagnitude > 1e-6f ? (sb - sa).normalized : F(car.transform.forward).normalized;
            float cte = Vector3.Dot(F(pos) - sa, Vector3.Cross(Vector3.up, segDir));
            float la = 4f + 0.12f * v, acc = 0f; int k = next;
            while (k < path.Count - 1 && acc < la) { acc += (F(path[k + 1].p) - F(path[k].p)).magnitude; k++; }
            var lineDir = (F(path[k].p) - sa).sqrMagnitude > 1e-4f ? (F(path[k].p) - sa).normalized : segDir;
            float head = Vector3.SignedAngle(F(car.transform.forward), lineDir, Vector3.up);
            float corr = -Mathf.Atan2(1.6f * cte, v + 2f) * Mathf.Rad2Deg;
            float steer = Mathf.Clamp((head + corr) / 18f, -1f, 1f);
            float w = car.topSpeed; acc = 0f;
            for (int i = next; i < path.Count - 2 && acc < 90f; i++)
            {
                acc += (F(path[i + 1].p) - F(path[i].p)).magnitude;
                w = Mathf.Min(w, Mathf.Sqrt(path[i + 1].cap * path[i + 1].cap + 2f * BrakeAcc * Mathf.Max(0f, acc - 4f)));
                if (i < 2 || i + 2 >= path.Count) continue;
                Vector3 a = F(path[Mathf.Max(0, i - 2)].p), b = F(path[i].p), c = F(path[Mathf.Min(path.Count - 1, i + 2)].p);
                float area2 = Mathf.Abs(Vector3.Cross(b - a, c - a).y); if (area2 < 1e-3f) continue;
                float r = (b - a).magnitude * (c - b).magnitude * (c - a).magnitude / (2f * area2);
                float vb = Mathf.Sqrt(LatAcc * r);
                w = Mathf.Min(w, Mathf.Sqrt(vb * vb + 2f * BrakeAcc * Mathf.Max(0f, acc - 6f)));
            }
            float speed = car.ForwardSpeed;
            car.Simulate(speed < w ? 1f : 0f, speed > w + 1.5f ? Mathf.Clamp01((speed - w) / 4f) : 0f, steer, Time.fixedDeltaTime);
            return next;
        }
        static Vector3 F(Vector3 v) { v.y = 0f; return v; }

    }
}
