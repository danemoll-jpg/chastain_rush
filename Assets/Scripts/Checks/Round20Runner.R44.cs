using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 44 in the exe (-exeRound44 <dir> [-r44Only times,wjump,drain,race,pics] [-traffic Normal] -logFile <dir>\Player.log): the Chastain Lakes race
    //   times   the Street Classic and the Drifter Twin by each shortcut and by the lap's own way over the same ground (the lake trail and Bells Ferry
    //           Road against the lake drain; the west loop against the lake jump), flat out, times compared
    //   wjump   the lake jump on every vehicle the route offers, the AI's speed held at the pile (28 m/s): its speed off the lip, air, height, where it
    //           comes down, upright, on to Lakeside Drive
    //   drain   off the gazebo into the drain on a straight line at speed (Street Classic, Drifter Twin), through B28 and out of MH14 (the cover
    //           pops); a miss: off the lawn into the lake 12 m east of the drain (not taken, slowed in the water), then a reset (onto the race's line)
    //   lakesrace the full race (2 laps), 3 AI, traffic as given: everyone finishes; their shortcuts
    //   pics    the trail from photo 3's and photo 4's spots, the footbridge, the gazebo, the drain, the lake jump's pile and landing; chase pictures
    //           of the drop, the manhole popping and the lake jump in the air
    public sealed partial class Round20Runner
    {
        RaceShortcut LakesSc(string id) => race.Route.Shortcuts.FirstOrDefault(s => s.Id == id);
        static LakeDrain Drain() => LakeDrain.All.FirstOrDefault();

        IEnumerator StartLakesRace(int ai = 0)
        {
            RaceController.RouteOverride = "chastain_lakes.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 2; RaceController.AiCount = ai;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
        }

        // from 'lead' m before a shortcut's turn-off to 30 m past its end: on the lap, or through the shortcut's line (its heights); 'speedAt' holds
        // a speed at a point of the shortcut's line (the pile's lip); 'watch' sees each step
        IEnumerator TimedLakes(RaceShortcut sc, bool via, Run run, System.Action<float> done, float lead = 60f, Vector3? holdAt = null, float holdSpeed = 0f, System.Action watch = null)
        {
            var a = sc.Path[0]; var b = sc.Path[sc.Path.Length - 1]; float sa = AbsStation(a), sb = AbsStation(b); if (sb < sa) sb += race.Route.LapLength;
            var line = LapSlice(sa - lead, sa).ToList(); var aEnd = a; int iHold = -1;
            if (via) { for (int i = 0; i < sc.Path.Length; i++) { if (holdAt.HasValue && iHold < 0 && Flat(sc.Path[i] - holdAt.Value).magnitude < 1.2f) iHold = line.Count; line.Add(sc.Path[i]); } line.AddRange(LapSlice(sb + 2f, sb + 30f)); }
            else line.AddRange(LapSlice(sa + 2f, sb + 30f));
            Place(line[0], line[2] - line[0], 15f); yield return new WaitForFixedUpdate();
            var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
            float[] prof = iHold >= 0 ? new[] { cum[iHold], holdSpeed, cum[iHold] + 1f, holdSpeed } : null;
            if (via && sc.Id == "w_lake_jump")   // the check's own driver braked for the T onto Lakeside Drive (it came at it flat out and ran on across the road)
            {
                int iEnd = line.Count - LapSlice(sb + 2f, sb + 30f).Count - 1; float sj = cum[Mathf.Clamp(iEnd, 0, cum.Length - 1)] - 8f;
                prof = (prof ?? new float[0]).Concat(new[] { sj, 12f, sj + 14f, 12f }).ToArray();
            }
            float t0 = -1f, t1 = -1f, clock = 0f, lastS = 0f;
            var drive = DriveLine(line, 99f, 120f, run, null, r =>
            {
                var p = car.Body.position; clock += Time.fixedDeltaTime;
                if (t0 < 0f && Vector3.Distance(Flat(p), Flat(aEnd)) < 5f) t0 = clock;
                float s = ProjectNear(line, cum, p, lastS, out _); lastS = s;
                if (t0 >= 0f && t1 < 0f && s > cum[cum.Length - 1] - 31f) t1 = clock;
                watch?.Invoke();
                return false;
            }, 5f, true, prof);
            yield return drive;
            done(t0 >= 0f && t1 > t0 ? t1 - t0 : -1f);
        }

        IEnumerator R44Times()
        {
            report.AppendLine(); report.AppendLine("=== THE CHASTAIN LAKES RACE: each shortcut against the lap's own way over the same ground, flat out (Street Classic, Drifter Twin)");
            yield return StartLakesRace();
            report.AppendLine($"  {race.Route.Name}: lap {race.Route.LapLength:0} m, {race.Route.Checkpoints.Count} checkpoints, shortcuts {string.Join(", ", race.Route.Shortcuts.Select(s => s.Name))}, {race.Route.Closures.Count} barriers");
            var notSaving = new List<string>();
            foreach (var scId in new[] { "w_lake_jump", "e_lake_drain" })
            {
                var sc = LakesSc(scId); if (sc == null) { Check($"the {scId} shortcut is in the race", false); continue; }
                foreach (var id in new[] { "original", "drifter" })
                {
                    yield return Vehicle(id);
                    float road = -1f, via = -1f; var r0 = new Run(); var r1 = new Run();
                    yield return TimedLakes(sc, false, r0, t => road = t);
                    race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                    yield return TimedLakes(sc, true, r1, t => via = t, 60f, scId == "w_lake_jump" ? WLip() : (Vector3?)null, 28f);
                    float saved = road > 0f && via > 0f ? road - via : -99f;
                    if (saved < 1f) notSaving.Add($"{sc.Name} on the {Nm(id)}");
                    report.AppendLine($"  {sc.Name,-11} {Nm(id),-16} the lap's way {road:0.0} s{(r0.reached ? "" : " (STOPPED " + r0.end + ")")}; the shortcut {via:0.0} s{(r1.reached ? "" : $" (STOPPED {r1.end} at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}))")}: saves {saved:0.0} s");
                    if (!r1.reached) File.WriteAllLines(Path.Combine(outputDir, $"times_{scId}_{id}.csv"), r1.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
                    if (!r0.reached) { File.WriteAllLines(Path.Combine(outputDir, $"times_{scId}_{id}_lapway.csv"), r0.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); var e0 = r0.trail.Count > 0 ? r0.trail[r0.trail.Count - 1] : Vector3.zero; report.AppendLine($"      the lap's way stopped at ({e0.x:0.0}, {e0.y:0.0}, {e0.z:0.0})"); }
                    race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                }
            }
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("each shortcut taken clean and flat out saves time against the lap's own way, on both vehicles", notSaving.Count == 0, notSaving.Count == 0 ? "" : "not: " + string.Join(", ", notSaving));
        }

        static Vector3 WLip() => new Vector3(505.5f, 0f, 1399.5f);

        IEnumerator R44WJump(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== W, THE LAKE JUMP: every vehicle the route offers, held to 28 m/s at the pile (the AI's speed), a real jump");
            yield return StartLakesRace();
            var sc = LakesSc("w_lake_jump"); if (sc == null) { Check("the lake jump is in the race", false); race.EndRace(); yield break; }
            var lip = WLip(); var land0 = new Vector2(500f, 1356f);   // the water's far edge on the line
            var bad = new List<string>();
            foreach (var id in ids.Where(i => race.RouteData == null || race.RouteData.AllowsVehicle(i)))
            {
                yield return Vehicle(id);
                float lipV = -1f, lipY = float.NaN, top = float.MinValue, air = 0f, cur = 0f; bool past = false, landed = false; Vector3 land = new Vector3(float.NaN, 0f, 0f); float up = -1f; bool wet = false;
                var run = new Run();
                yield return TimedLakes(sc, true, run, t => { }, 40f, lip, 28f, () =>
                {
                    var p = car.Body.position;
                    if (!past && Vector3.Dot(Flat(p - lip), new Vector3(Mathf.Sin(186f * Mathf.Deg2Rad), 0f, Mathf.Cos(186f * Mathf.Deg2Rad))) > 0f && Flat(p - lip).magnitude < 4f) { past = true; lipV = car.Body.linearVelocity.magnitude; lipY = p.y; }
                    if (past && !landed)
                    {
                        top = Mathf.Max(top, p.y);
                        if (car.GroundedWheels == 0) cur += Time.fixedDeltaTime;
                        else if (cur > .3f) { landed = true; air = cur; land = p; up = car.transform.up.y; wet = ShallowWater.InWater(p + Vector3.up * .1f); }
                    }
                });
                bool ok = landed && !wet && up > .7f && run.reached;
                if (!ok) bad.Add(Nm(id));
                report.AppendLine($"  {Nm(id),-18} off the pile at {lipV:0.0} m/s; {air:0.00} s in the air, {top - lipY:0.0} m over the lip, down at ({land.x:0}, {land.z:0}) {(new Vector2(land.x, land.z) - new Vector2(lip.x, lip.z)).magnitude:0} m out{(wet ? " IN THE LAKE" : "")}, upright {up:0.00}; " +
                                  $"{(run.reached ? "on along Lakeside Drive" : $"STOPPED {run.end} at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})")}");
                if (!ok) File.WriteAllLines(Path.Combine(outputDir, $"wjump_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("W: every offered vehicle at the AI's speed clears the lake, lands on its wheels and drives on to Lakeside Drive", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
        }

        IEnumerator R44Drain()
        {
            report.AppendLine(); report.AppendLine("=== E, THE LAKE DRAIN: off the gazebo on a straight line at speed, down the drain, through B28, out of MH14; and a miss");
            yield return StartLakesRace();
            var sc = LakesSc("e_lake_drain"); var dr = Drain();
            if (sc == null || dr == null) { Check("the lake drain and its shortcut are in the build", false); race.EndRace(); yield break; }
            var bad = new List<string>();
            foreach (var id in new[] { "original", "drifter" })
            {
                yield return Vehicle(id);
                int sw0 = LakeDrain.Swallowed, mh0 = ManholeLid.Bursts; float offEnd = -1f; bool shot = false, popShot = false; var run = new Run();
                var gEnd = new Vector3(1283.8f, 0f, 1110.6f);
                yield return TimedLakes(sc, true, run, t => { }, 40f, null, 0f, () =>
                {
                    var p = car.Body.position;
                    if (offEnd < 0f && Flat(p - gEnd).magnitude < 2f) offEnd = Flat(car.Body.linearVelocity).magnitude;
                    if (!shot && id == "original" && LakeDrain.Swallowed > sw0) { shot = true; StartCoroutine(Capture("44_drain_drop_chase")); }
                    if (!popShot && id == "original" && ManholeLid.Bursts > mh0) { popShot = true; StartCoroutine(Capture("44_manhole_pop_chase")); }
                });
                bool took = LakeDrain.Swallowed > sw0, popped = ManholeLid.Bursts > mh0;
                if (!took || !popped || !run.reached) bad.Add(Nm(id));
                report.AppendLine($"  {Nm(id),-16} off the gazebo's end at {offEnd:0.0} m/s; {(took ? "down the drain" : "MISSED THE DRAIN")}; {(popped ? "out of MH14 (the cover popped)" : "NO POP at MH14")}; " +
                                  $"{(run.reached ? "on along North Lakeside Drive" : $"STOPPED {run.end} at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})")}, in {run.time:0.0} s");
                if (!run.reached) File.WriteAllLines(Path.Combine(outputDir, $"drain_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            Check("E: off the gazebo on its line at speed, down the drain and out of the manhole onto North Lakeside Drive (both vehicles)", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
            // a miss: from the lawn by the swing into the lake, 12 m east of the drain
            yield return Vehicle("original");
            var m0 = dr.mouth + new Vector3(12f, 0f, -8f); var from = new Vector3(1318f, 0f, 1098f);
            var missLine = new List<Vector3>(); for (int k = 0; k <= 20; k++) missLine.Add(RaceRoute.Surface(Mathf.Lerp(from.x, m0.x + 4f, k / 20f), Mathf.Lerp(from.z, m0.z + 30f, k / 20f)));
            int s0 = LakeDrain.Swallowed; var mr = new Run(); float vWater = -1f;
            Place(missLine[0], missLine[2] - missLine[0], 14f); yield return new WaitForFixedUpdate();
            yield return DriveLine(missLine, 16f, 8f, mr, null, r => { if (vWater < 0f && ShallowWater.InWater(car.Body.position + Vector3.up * .1f)) vWater = 0f; return false; }, 5f, false);
            float vAfter = Flat(car.Body.linearVelocity).magnitude; var wetAt = car.Body.position;
            bool inWater = ShallowWater.InWater(wetAt + Vector3.down * .4f) || ShallowWater.InWater(wetAt);
            bool reset = ResetNow(out var to, out _);
            float toLap = reset ? Flat(to - race.Route.PointAt(AbsStation(to), out _)).magnitude : -1f;
            report.AppendLine($"  a miss (Street Classic, the lawn into the lake 12 m east of the drain at 16 m/s): {(LakeDrain.Swallowed > s0 ? "TAKEN by the drain" : "not taken")}; {(inWater ? "in the water" : "not in the water")} at ({wetAt.x:0}, {wetAt.z:0}), {vAfter:0.0} m/s after 8 s; " +
                              $"a reset: {(reset ? $"to ({to.x:0.0}, {to.z:0.0}), {toLap:0.0} m from the race's line" : "NO SPOT")}");
            Check("E: a miss is in the lake (slowed, not taken) and a reset puts it back on the race's line", LakeDrain.Swallowed == s0 && inWater && reset && toLap < 4f);
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R44Race()
        {
            report.AppendLine(); report.AppendLine($"=== THE CHASTAIN LAKES RACE, 2 laps, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, the player parked off the course");
            yield return StartLakesRace(3);
            respawn.PlaceAt(RaceRoute.Surface(760f, 1530f) + Vector3.up * .4f, 0f); race.NotifyTeleported();
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            float end = Time.realtimeSinceStartup + 9f * 60f; int sw0 = LakeDrain.Swallowed;
            while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished)) yield return new WaitForSecondsRealtime(.25f);
            Time.timeScale = 1f;
            foreach (var e in ai)
                Check($"{e.Name} ({e.VehicleName}) finishes the 2 laps", e.Finished,
                      $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, missed {e.MissedTotal}, shortcuts {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}");
            report.AppendLine($"INFO  taken by the lake drain during the race: {LakeDrain.Swallowed - sw0}");
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R44Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures");
            yield return StartLakesRace();
            car.Body.position = RaceRoute.Surface(760f, 1530f) + Vector3.up; car.Body.linearVelocity = Vector3.zero;
            Vector3 G(float x, float z, float up) => RaceRoute.Surface(x, z) + Vector3.up * up;
            yield return Still("44_trail_photo3_spot", G(1368.5f, 1118f, 1.7f), G(1356f, 1139f, 0.8f), 70f);
            yield return Still("44_trail_photo4_spot", G(1370f, 1222f, 1.7f), G(1360f, 1175f, 0.5f), 72f);
            yield return Still("44_footbridge", G(1340f, 1134f, 2.2f), G(1355f, 1141f, 0.5f), 65f);
            yield return Still("44_footbridge_on", G(1359.5f, 1124f, 1.6f), G(1353f, 1150f, 0.6f), 65f);
            yield return Still("44_gazebo_from_the_swing", G(1316f, 1104f, 1.6f), G(1286f, 1111f, 1.5f), 60f);
            var dr = Drain(); if (dr) yield return Still("44_drain", dr.mouth + new Vector3(-9f, 6f, 14f), dr.mouth + new Vector3(4f, 0f, -4f), 60f);   // from the lake's side, the gazebo beyond
            if (dr) yield return Still("44_drain_down_the_shaft", dr.mouth + new Vector3(0.5f, 3f, 0.5f), dr.mouth + new Vector3(0f, -12f, 0f), 75f);
            yield return Still("44_manhole_MH14", G(1196f, 1318f, 3f), G(1180f, 1326f, 0f), 60f);
            yield return Still("44_lakejump_turnoff", G(520f, 1504f, 2f), G(496f, 1478f, 0.5f), 65f);
            yield return Still("44_lakejump_pile", G(520f, 1418f, 4f), G(505.5f, 1399.5f, 1.5f), 60f);
            yield return Still("44_lakejump_landing", G(532f, 1330f, 6f), G(500f, 1348f, 0f), 62f);
            race.EndRace();
            // the lake jump in the air (Street Classic, chase camera)
            yield return StartLakesRace(); yield return Vehicle("original");
            var sc = LakesSc("w_lake_jump"); bool shot = false; var run = new Run();
            if (sc != null) yield return TimedLakes(sc, true, run, t => { }, 40f, WLip(), 28f, () => { if (!shot && car.GroundedWheels == 0 && Flat(car.Body.position - WLip()).magnitude > 18f) { shot = true; StartCoroutine(Capture("44_lakejump_chase")); } });
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("pictures written", true, "44_*.png");
        }
    }
}
