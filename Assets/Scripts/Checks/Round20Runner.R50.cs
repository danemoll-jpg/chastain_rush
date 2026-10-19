using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 50 in the exe (-exeRound50 <dir> [-r50Only kclean,kcheat,kshort,krace,ktrail,kpics] [-traffic Normal] -logFile <dir>\Player.log):
    //   kclean   A: the kicker line clean, both jumps (Street Classic, Drifter Twin): speeds at the lips, air, where each comes down; time against
    //            the lap's own way
    //   kcheat   A: the same ground WITHOUT the jumps (Dan's standing rule of 2026-10-19): beside both ramps; the kicker then beside the carport;
    //            beside the kicker then the carport; across the lake round the valley's brush; the kicker then down Northlake Point round its
    //            barrier. Each must be slower than the lap's own way
    //   kshort   A: short off the kicker (16 m/s, into the valley's brush) and short off the carport (14 m/s, into the bed past it): each slower
    //            than the lap's own way
    //   krace    the reverse race, 2 laps, 3 AI, twice (with -traffic Normal): everyone finishes; who takes the kicker line and who fails
    //   ktrail   B: the lake trail both ways, every offered vehicle at the race's held speeds, 2 m to the side of the path (partly on the grass)
    //   kpics    Dan's BUG-001 to BUG-004 spots, the carport, its flight, the trail from photo 2's and photo 3's spots
    public sealed partial class Round20Runner
    {
        static readonly Vector3 CarportFoot = new Vector3(1143.5f, 0f, 979.5f), CarportLip = new Vector3(1136.54f, 0f, 978.77f);   // make_lakes_route.py C_FOOT, C_LIP
        static Vector3 CarportDir => new Vector3(Mathf.Sin(264f * Mathf.Deg2Rad), 0f, Mathf.Cos(264f * Mathf.Deg2Rad));

        // from 'lead' m before the shortcut's turn-off, through 'mid', then on along the lap to 30 m past the shortcut's rejoin; timed as TimedLakes
        IEnumerator TimedVia(RaceShortcut sc, List<Vector3> mid, Run run, System.Action<float> done, System.Action watch = null, float[] prof = null, float lead = 60f)
        {
            var a = sc.Path[0]; var b = sc.Path[sc.Path.Length - 1]; float sa = AbsStation(a), sb = AbsStation(b); if (sb < sa) sb += race.Route.LapLength;
            var line = LapSlice(sa - lead, sa).ToList(); line.AddRange(mid); line.AddRange(LapSlice(sb + 2f, sb + 30f));
            race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            Place(line[0], line[2] - line[0], 15f); yield return new WaitForFixedUpdate();
            var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
            float t0 = -1f, t1 = -1f, clock = 0f, lastS = 0f;
            yield return DriveLine(line, 99f, 150f, run, null, r =>
            {
                var p = car.Body.position; clock += Time.fixedDeltaTime;
                if (t0 < 0f && Vector3.Distance(Flat(p), Flat(a)) < 5f) t0 = clock;
                float s = ProjectNear(line, cum, p, lastS, out _); lastS = s;
                if (t0 >= 0f && t1 < 0f && s > cum[cum.Length - 1] - 31f) t1 = clock;
                watch?.Invoke();
                return false;
            }, 5f, true, prof);
            done(t0 >= 0f && t1 > t0 ? t1 - t0 : -1f);
        }
        // the shortcut's line with its points near a ramp moved 'side' m to its left (eased in and out over 12 m): round the ramp, not up it
        static List<Vector3> Beside(Vector3[] path, Vector3 foot, Vector3 lip, Vector3 dir, float side)
        {
            var left = Vector3.Cross(dir, Vector3.up).normalized;   // the open side: the kicker's south (237's yard), the carport's south (the cul-de-sac)
            var outp = new List<Vector3>();
            foreach (var q in path)
            {
                float s = Vector3.Dot(Flat(q - foot), dir), len = Vector3.Dot(Flat(lip - foot), dir);
                float w = s < -14f ? 0f : s < -2f ? Mathf.SmoothStep(0f, 1f, (s + 14f) / 12f) : s <= len + 3f ? 1f : s < len + 15f ? Mathf.SmoothStep(1f, 0f, (s - len - 3f) / 12f) : 0f;
                if (Flat(q - foot).magnitude > 60f) w = 0f;
                var p = q + left * side * w; outp.Add(RaceRoute.Surface(p.x, p.z));
            }
            return outp;
        }
        static List<Vector3> Pts(params float[] xz) { var l = new List<Vector3>(); for (int i = 0; i + 1 < xz.Length; i += 2) l.Add(RaceRoute.Surface(xz[i], xz[i + 1])); return Dense(l, 2f); }
        static List<Vector3> Dense(List<Vector3> l, float step)
        {
            var o = new List<Vector3>();
            for (int i = 0; i + 1 < l.Count; i++) { int n = Mathf.Max(1, Mathf.CeilToInt(Flat(l[i + 1] - l[i]).magnitude / step)); for (int k = 0; k < n; k++) { var p = Vector3.Lerp(l[i], l[i + 1], k / (float)n); o.Add(RaceRoute.Surface(p.x, p.z)); } }
            o.Add(l[l.Count - 1]); return o;
        }
        // a driver's line round the tree trunks (the check's driver steers straight at a point; a player weaves): each point within 2.6 m of a trunk
        // pushed out sideways, a few passes
        static List<Vector3> Dodge(List<Vector3> line)
        {
            var trunks = new List<Vector2>(); float x0 = line.Min(q => q.x) - 5f, x1 = line.Max(q => q.x) + 5f, z0 = line.Min(q => q.z) - 5f, z1 = line.Max(q => q.z) + 5f;
            foreach (var f in FindObjectsByType<TreeField>(FindObjectsSortMode.None))
                for (int i = 0; i < f.count; i++)
                {
                    float x = System.BitConverter.ToSingle(f.data, i * TreeField.PackedSize), z = System.BitConverter.ToSingle(f.data, i * TreeField.PackedSize + 8);
                    if (x > x0 && x < x1 && z > z0 && z < z1) trunks.Add(new Vector2(x, z));
                }
            var p = line.Select(q => new Vector2(q.x, q.z)).ToList();
            for (int pass = 0; pass < 4; pass++)
                for (int i = 1; i + 1 < p.Count; i++)
                {
                    var t = (p[i + 1] - p[i - 1]).normalized; var n = new Vector2(-t.y, t.x);
                    foreach (var tr in trunks) { var d = p[i] - tr; if (d.magnitude < 2.6f) p[i] += n * (Vector2.Dot(d, n) >= 0f ? 1f : -1f) * (2.6f - Mathf.Abs(Vector2.Dot(d, n))); }
                }
            return p.Select(q => RaceRoute.Surface(q.x, q.y)).ToList();
        }
        sealed class Jump2 { public KickRes k1 = new KickRes(), k2 = new KickRes(); public float bedMax; public Vector3 bedAt; }
        void Jump2Watch(Jump2 j, bool shots)
        {
            KickWatch(j.k1, shots);
            var k = j.k2; var p = car.Body.position;
            if (!k.past && Vector3.Dot(Flat(p - CarportLip), CarportDir) > 0f && Flat(p - CarportLip).magnitude < 6f) { k.past = true; k.lipV = car.Body.linearVelocity.magnitude; k.lipY = p.y; }
            if (car.GroundedWheels >= 2) { float c = ShrubBeds.CoverageAt(p); if (c > j.bedMax) { j.bedMax = c; j.bedAt = p; } }
            if (!k.past || k.landed) return;
            k.top = Mathf.Max(k.top, p.y);
            if (shots && !k.shot && car.GroundedWheels == 0 && Vector3.Dot(Flat(p - CarportLip), CarportDir) > 18f) { k.shot = true; StartCoroutine(Capture("50_A_carport_flight_chase")); }
            if (car.GroundedWheels == 0) k.cur += Time.fixedDeltaTime;
            else if (k.cur > .3f) { k.landed = true; k.air = k.cur; k.land = p; k.up = car.transform.up.y; k.hollow = ShrubBeds.CoverageAt(p) > 0.3f; }
        }
        static string CarportLine(KickRes k) =>
            $"off the eave at {k.lipV:0.0} m/s, {k.air:0.00} s in the air, {k.top - k.lipY:0.0} m over it, down at ({k.land.x:0}, {k.land.y:0.0}, {k.land.z:0}) {(float.IsNaN(k.land.x) ? -1f : Flat(k.land - CarportLip).magnitude):0} m out" +
            $"{(k.hollow ? " IN THE SHRUB BED" : "")}, upright {k.up:0.00}";

        IEnumerator R50Clean()
        {
            report.AppendLine(); report.AppendLine("=== A. THE KICKER LINE, both jumps, flat out (reverse race), timed against the lap's own way");
            yield return StartLakes("reverse", 0);
            var sc = LakesSc("r_kicker_jump"); if (sc == null) { Check("A: the kicker line is in the reverse race", false); race.EndRace(); yield break; }
            report.AppendLine($"  {sc.Name}: forgives {string.Join(", ", race.Route.Checkpoints.Where((c, i) => i >= sc.FirstForgiven && i <= sc.LastForgiven).Select(c => c.Label))}");
            var bad = new List<string>();
            foreach (var id in new[] { "original", "drifter" })
            {
                yield return Vehicle(id);
                float road = -1f, via = -1f; var r0 = new Run(); var r1 = new Run();
                yield return TimedLakes(sc, false, r0, t => road = t);
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                var j = new Jump2(); yield return TimedLakes(sc, true, r1, t => via = t, 60f, null, 0f, () => Jump2Watch(j, id == "original"));
                float saved = road > 0f && via > 0f ? road - via : -99f;
                bool clean = j.k1.landed && !j.k1.hollow && j.k1.up > .6f && j.k2.landed && !j.k2.hollow && j.k2.up > .6f && j.bedMax < 0.3f;
                if (!(clean && r1.reached && saved > 1.1f)) bad.Add(Nm(id));
                report.AppendLine($"  {Nm(id),-16} the lap's way {road:0.0} s; the kicker line {via:0.0} s, saves {saved:0.0} s{(r1.reached ? "" : "; STOPPED " + r1.end)}");
                report.AppendLine($"      the kicker: {KickLine(j.k1)}");
                report.AppendLine($"      the carport: {CarportLine(j.k2)}; deepest in a shrub bed {j.bedMax:0.00}");
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
            }
            Check("A: the kicker line flat out (Street Classic, Drifter Twin): both jumps clean (over the brush and the bed), on to the lap, saving more than Round 49's 1.1 s", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R50Cheat()
        {
            report.AppendLine(); report.AppendLine("=== A. THE SAME GROUND WITHOUT THE JUMPS (Street Classic, flat out): each must be slower than the lap's own way (times without the 5 s penalty for CP5)");
            yield return StartLakes("reverse", 0);
            var sc = LakesSc("r_kicker_jump"); if (sc == null) { Check("A: the kicker line is in the reverse race", false); race.EndRace(); yield break; }
            yield return Vehicle("original");
            float road = -1f; { var r0 = new Run(); yield return TimedLakes(sc, false, r0, t => road = t); }
            report.AppendLine($"  the lap's own way {road:0.0} s");
            var kFoot = KickerLip - KickerDir * 10f; var path = sc.Path;
            var bothBeside = Beside(Beside(path, kFoot, KickerLip, KickerDir, 7f).ToArray(), CarportFoot, CarportLip, CarportDir, 7.5f);
            var kickThenBeside = Beside(path, CarportFoot, CarportLip, CarportDir, 7.5f);
            var besideThenCarport = Beside(path, kFoot, KickerLip, KickerDir, 7f);
            // across the lake: from the turn-off up 239's back yard to the lake's arm, west across it north of the valley's brush (its cattails), ashore in the pocket,
            // on to the carport's line
            int iC = System.Array.FindIndex(path, q => Flat(q - CarportFoot).magnitude < 14f);
            var lake = path.TakeWhile(q => q.x > 1241f).ToList();   // the turn-off and the gap between 239 and 237, then north up 239's back yard
            lake.AddRange(Pts(1232f, 1012f, 1228f, 1030f, 1222f, 1046f, 1205f, 1054f, 1186f, 1053f, 1179f, 1041f, 1186f, 1030f, 1186f, 1005f, 1172f, 989f));
            lake.AddRange(path.Skip(Mathf.Max(0, iC)));
            // the kicker, then down Northlake Point (the cul-de-sac, the road) and round its barrier's west end onto the lap at CP5
            int iL = System.Array.FindIndex(path, q => Vector3.Dot(Flat(q - KickerLip), KickerDir) > 52f);
            var road2 = path.Take(Mathf.Max(1, iL)).ToList();
            road2.AddRange(Pts(1165f, 982f, 1148f, 974f, 1141f, 955f, 1133f, 940f, 1121f, 940f, 1112f, 934f, 1103f, 935f));
            float sCp = AbsStation(RaceRoute.Surface(1103f, 935f)), sEnd = AbsStation(path[path.Length - 1]); if (sEnd < sCp) sEnd += race.Route.LapLength;
            road2.AddRange(LapSlice(sCp + 2f, sEnd));
            bothBeside = Dodge(bothBeside); kickThenBeside = Dodge(kickThenBeside); besideThenCarport = Dodge(besideThenCarport); lake = Dodge(lake); road2 = Dodge(road2);
            var runs = new (string name, List<Vector3> line)[]
            {
                ("beside both ramps (the valley's brush, the bed between 231 and 230)", bothBeside),
                ("the kicker, then beside the carport (the bed between 231 and 230)", kickThenBeside),
                ("beside the kicker (the valley's brush), then the carport", besideThenCarport),
                ("across the lake round the valley's brush, then the carport", lake),
                ("the kicker, then down Northlake Point and round its barrier (the hedge)", road2),
            };
            var faster = new List<string>(); int n = 0;
            foreach (var (name, line) in runs)
            {
                var r = new Run(); float t = -1f; var j = new Jump2();
                yield return TimedVia(sc, line, r, x => t = x, () => Jump2Watch(j, false));
                yield return Capture($"50_A_cheat_{++n}_end");
                bool slower = !r.reached || t < 0f || t > road;
                if (!slower) faster.Add(name);
                report.AppendLine($"  {n}. {name}: {(r.reached && t > 0f ? $"{t:0.0} s ({t - road:+0.0;-0.0} s against the lap's way)" : "did not get through (" + r.end + $", at ({car.Body.position.x:0}, {car.Body.position.z:0}))")}; " +
                                  $"deepest in a bed {j.bedMax:0.00} at ({j.bedAt.x:0}, {j.bedAt.z:0}), slowest {r.minSpeed:0.0} m/s");
            }
            Check("A: every way over the shortcut's ground without its jumps is slower than the plain lap", faster.Count == 0, faster.Count == 0 ? "" : "faster: " + string.Join("; ", faster));
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R50Short()
        {
            report.AppendLine(); report.AppendLine("=== A. SHORT JUMPS (Street Classic): each must lose more than the line saves (slower than the lap's own way)");
            yield return StartLakes("reverse", 0);
            var sc = LakesSc("r_kicker_jump"); if (sc == null) { Check("A: the kicker line is in the reverse race", false); race.EndRace(); yield break; }
            yield return Vehicle("original");
            float road = -1f; { var r0 = new Run(); yield return TimedLakes(sc, false, r0, t => road = t); }
            var res = new List<string>(); bool allSlower = true;
            foreach (var (name, at, v) in new[] { ("off the kicker at 16 m/s", KickerLip + Vector3.up * 300f, 16f), ("off the carport at 14 m/s", CarportLip + Vector3.up * 305f, 14f) })
            {
                race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                var r = new Run(); float t = -1f; var j = new Jump2();
                yield return TimedLakes(sc, true, r, x => t = x, 60f, at, v, () => Jump2Watch(j, false));
                yield return Capture($"50_A_short_{(v < 15f ? "carport" : "kicker")}");
                bool slower = !r.reached || t < 0f || t > road; allSlower &= slower;
                res.Add($"{name}: {(r.reached && t > 0f ? $"{t:0.0} s ({t - road:+0.0;-0.0} s against the lap's way {road:0.0} s)" : "not through: " + r.end)}; deepest in a bed {j.bedMax:0.00} at ({j.bedAt.x:0}, {j.bedAt.z:0}); " +
                        $"kicker {(j.k1.landed ? KickLine(j.k1) : "-")}; carport {(j.k2.landed ? CarportLine(j.k2) : "-")}");
            }
            foreach (var l in res) report.AppendLine("  " + l);
            Check("A: a short jump (either) costs more than the line saves", allSlower);
            race.EndRace(); RaceController.DirectionOverride = null;
        }

        IEnumerator R50Race()
        {
            report.AppendLine(); report.AppendLine($"=== the reverse race, 2 laps, 3 AI, twice, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}, the player parked off the course");
            var lines = new List<string>(); int kTries = 0, kShort = 0, cTries = 0, cShort = 0;
            for (int raceNo = 0; raceNo < 2; raceNo++)
            {
                yield return StartLakes("reverse", 3);
                respawn.PlaceAt(RaceRoute.Surface(760f, 1530f) + Vector3.up * .4f, 0f); race.NotifyTeleported();
                var ai = race.Entrants.Where(e => !e.IsPlayer).ToList(); var atK = new Dictionary<RaceEntrant, int>(); var lowK = new HashSet<RaceEntrant>(); var atC = new Dictionary<RaceEntrant, int>(); var lowC = new HashSet<RaceEntrant>();
                Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                float end = Time.realtimeSinceStartup + 9f * 60f;
                while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished))
                {
                    foreach (var e in ai)
                    {
                        if (!e.Car) continue; var p = e.Car.Body.position;
                        if (Flat(p - KickerLip).magnitude < 4f && (!atK.TryGetValue(e, out int l) || l != e.CompletedLaps)) { atK[e] = e.CompletedLaps; kTries++; lowK.Remove(e); }
                        if (atK.ContainsKey(e) && !lowK.Contains(e) && p.y < 297.5f && p.x > 1185f && p.x < 1228f && p.z > 975f && p.z < 1020f) { lowK.Add(e); kShort++; lines.Add($"{e.Name} ({e.VehicleName}) short off the kicker, lap {e.CompletedLaps + 1}"); }
                        if (Flat(p - CarportLip).magnitude < 4f && (!atC.TryGetValue(e, out int l2) || l2 != e.CompletedLaps)) { atC[e] = e.CompletedLaps; cTries++; lowC.Remove(e); }
                        if (atC.ContainsKey(e) && !lowC.Contains(e) && e.Car.GroundedWheels >= 2 && p.x < 1130f && p.x > 1095f && ShrubBeds.CoverageAt(p) > 0.5f) { lowC.Add(e); cShort++; lines.Add($"{e.Name} ({e.VehicleName}) short off the carport into the bed, lap {e.CompletedLaps + 1}"); }
                    }
                    yield return new WaitForSecondsRealtime(.1f);
                }
                Time.timeScale = 1f;
                foreach (var e in ai)
                    Check($"race {raceNo + 1}: {e.Name} ({e.VehicleName}) finishes the 2 reverse laps", e.Finished,
                          $"{(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"not finished, laps {e.CompletedLaps}")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, missed {e.MissedTotal}, shortcuts {(e.ShortcutsTaken.Count > 0 ? string.Join(", ", e.ShortcutsTaken) : "none")}");
                race.EndRace(); RaceController.DirectionOverride = null;
            }
            report.AppendLine($"INFO  the AI in two reverse races: off the kicker {kTries} times (short {kShort}), off the carport {cTries} times (short into the bed {cShort}){(lines.Count > 0 ? " (" + string.Join("; ", lines) + ")" : "")}");
        }

        IEnumerator R50Trail(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== B. THE LAKE TRAIL: both directions, every offered vehicle at the race's held speeds, 2 m to the side of the path (partly on the grass)");
            var bad = new List<string>(); int n = 0;
            foreach (var dir in new[] { "forward", "reverse" })
            {
                yield return StartLakes(dir, 0);
                var zones = dir == "reverse" ? race.RouteData.ai_zones_reverse : race.RouteData.ai_zones;
                var lot = zones.First(z => z.id == "lot"); var bridge = zones.First(z => z.id == "footbridge"); var dam = zones.First(z => z.id == "the dam");
                float a = dir == "forward" ? lot.s0 - 30f : dam.s0, b = dir == "forward" ? dam.s1 : lot.s1 + 30f;
                var center = LapSlice(a, b);
                var cum = new float[center.Count]; for (int i = 1; i < center.Count; i++) cum[i] = cum[i - 1] + Flat(center[i] - center[i - 1]).magnitude;
                var prof = new List<float>();
                foreach (var z in zones.Where(z => z.s1 > a && z.s0 < b).OrderBy(z => z.s0)) prof.AddRange(new[] { Mathf.Max(0f, z.s0 - a), z.speed, Mathf.Min(b - a, z.s1 - a), z.speed });
                foreach (var id in ids.Where(i => race.RouteData == null || race.RouteData.AllowsVehicle(i)))
                {
                    float side = (n++ & 1) == 0 ? 2f : -2f;
                    // off to one side along the trail, on the line through the lot, over the footbridge and onto the road
                    var line = new List<Vector3>();
                    for (int i = 0; i < center.Count; i++)
                    {
                        var p = center[i]; var t = Flat(center[Mathf.Min(i + 1, center.Count - 1)] - center[Mathf.Max(i - 1, 0)]).normalized; var l = Vector3.Cross(t, Vector3.up) * -1f;
                        bool onTrail = p.x < 1404f && !(p.x > 1315f && p.x < 1363f && p.z > 1075f && p.z < 1093f) && Flat(p - new Vector3(1346.5f, 0f, 1145f)).magnitude > 14f;
                        var q = onTrail ? p + l * side : p; line.Add(RaceRoute.Surface(q.x, q.z));
                    }
                    yield return Vehicle(id);
                    race.StartRace(); while (race.State == RaceController.Stage.Countdown) yield return null;
                    Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                    var run = new Run();
                    yield return DriveLine(line, 30f, 70f, run, null, null, 5f, true, prof.ToArray());
                    bool ok = run.reached && run.minSpeed > 3f;
                    if (!ok) { bad.Add($"{dir} {Nm(id)}"); yield return Capture($"50_trail_{dir}_{id}_slowed"); }
                    report.AppendLine($"  {dir,-8} {Nm(id),-18} {side:+0;-0} m {(run.reached ? "through" : "STOPPED " + run.end + $" at ({car.Body.position.x:0}, {car.Body.position.z:0})")} in {run.time:0.0} s, slowest {run.minSpeed:0.0} m/s, most off its line {run.maxDev:0.0} m");
                }
                race.EndRace();
            }
            RaceController.DirectionOverride = null;
            Check("B: the lake trail both ways, every offered vehicle at the race's speeds, 2 m off the path (on the grass beside it): no stop", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
        }

        IEnumerator R50Pics()
        {
            report.AppendLine(); report.AppendLine("=== pictures");
            yield return StartLakes("reverse", 0);
            car.Body.position = RaceRoute.Surface(760f, 1530f) + Vector3.up; car.Body.linearVelocity = Vector3.zero;
            Vector3 G(float x, float z, float up) => RaceRoute.Surface(x, z) + Vector3.up * up;
            // Dan's spots (his report's camera: position, heading, 13.5 degrees down)
            IEnumerator Dan(string name, float x, float y, float z, float heading)
            {
                var at = new Vector3(x, y, z); var f = new Vector3(Mathf.Sin(heading * Mathf.Deg2Rad), -Mathf.Tan(13.5f * Mathf.Deg2Rad), Mathf.Cos(heading * Mathf.Deg2Rad));
                yield return Still(name, at, at + f * 20f, 60f);
            }
            yield return Dan("50_BUG001_trail_behind_the_pool", 1307.56f, 299.35f, 1105.97f, 87.2f);
            yield return Dan("50_BUG002_turnoff_no_sign", 1268.79f, 304.20f, 1040.35f, 187.0f);
            yield return Dan("50_BUG003_valley_brush", 1207.19f, 296.76f, 1004.53f, 214.2f);
            yield return Dan("50_BUG004_northlake_point_carport", 1161.78f, 306.19f, 982.28f, 226.9f);
            var cl = RaceRoute.Surface(CarportLip.x, CarportLip.z); var cf = RaceRoute.Surface(CarportFoot.x, CarportFoot.z);
            yield return Still("50_A_carport_from_the_landing", cf - CarportDir * 30f + Vector3.up * 2f, cf + Vector3.up * 1.5f, 55f);
            yield return Still("50_A_carport_side", cl + Vector3.Cross(Vector3.up, CarportDir) * 14f + CarportDir * -3f + Vector3.up * 2f, cl, 60f);
            yield return Still("50_A_from_the_eave_over_the_bed", cl + Vector3.up * 4.5f, cl + CarportDir * 45f, 62f);
            yield return Still("50_A_bed_between_231_and_230", G(1100f, 960f, 2.5f), G(1114f, 980f, 0.8f), 60f);
            yield return Still("50_A_hedges_by_the_barrier", G(1128f, 948f, 3f), G(1128f, 932f, 0.5f), 70f);
            var kl = new Vector3(KickerLip.x, 300.5f, KickerLip.z);
            yield return Still("50_A_kicker_over_the_brush", kl + Vector3.up * 2f, kl + KickerDir * 45f, 62f);
            yield return Still("50_A_lake_tip_cattails", G(1215f, 1030f, 3f), G(1192f, 1040f, 0.5f), 60f);
            yield return Still("50_A_from_above", G(1170f, 940f, 70f), G(1170f, 985f, 0f), 60f);
            yield return Still("50_B_trail_photo2_spot", G(1318f, 1121f, 1.7f), G(1322f, 1098f, 1.2f), 62f);
            yield return Still("50_B_trail_photo3_spot", G(1321f, 1119f, 1.7f), G(1343f, 1139f, 1.0f), 62f);
            yield return Still("50_B_dam_path_photo4_spot", G(1375f, 1232f, 1.7f), G(1355f, 1170f, 1.0f), 62f);
            yield return Still("50_B_trail_from_above", G(1300f, 1100f, 30f), G(1325f, 1110f, 0f), 60f);
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("pictures written", true, "50_*.png");
        }
    }
}
