using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 41 Parts B and C in the exe (-exeRound41 <dir> -r41Only crest,crestpics,pipes -logFile <dir>\Player.log):
    //   crest      the crest cut taken right (from the gap down onto house 1092's roof, up the roofer's ramp, the jump past CP4) and missed (straight
    //              on over the roof's ridge, no ramp), flat out on the Drifter Twin, the Street Classic and the Needle 600, against the lap: the time,
    //              what was touched (the roof, the ramp), the jump's air, where it came down, upright
    //   crestpics  the ramp from the terrace, from the side, the side yards' boulders, the landing, the jump
    //   pipes      Part C: each of BF, X_W20, X_W11, X_BRIDGE, X_STREAM from outside, BUG-001's spot, and each driven in and out
    public sealed partial class Round20Runner
    {
        RaceShortcut Crest() => race.Route.Shortcuts.FirstOrDefault(s => s.Id == "rc_crest_cut");
        static readonly Vector2 RoofRampFoot = new Vector2(3094.6f, 675.9f);

        // the crest cut's line (its own heights), or (miss) the same from the gap but straight on north over the roof's ridge and down onto the drive
        IEnumerator TimedCrest(int how, Run run, System.Action<float, string, float, Vector3, float> done)   // how: 0 the lap, 1 the cut, 2 the cut missing the ramp
        {
            var sc = Crest(); var a = sc.Path[0]; var b = sc.Path[sc.Path.Length - 1]; float sa = AbsStation(a), sb = AbsStation(b); if (sb < sa) sb += race.Route.LapLength;
            var line = LapSlice(sa - 50f, sa).ToList(); int iFoot = NearestOn(sc.Path, RoofRampFoot);
            if (how == 0) line.AddRange(LapSlice(sa + 2f, sb + 30f));
            else
            {
                for (int i = 0; i < sc.Path.Length && (how == 1 || i <= iFoot - 4); i++) line.Add(sc.Path[i]);
                if (how == 2)   // on north from 8 m short of the ramp's foot: over the roof's ridge (x 3090), onto the drive, along it to the cut's end
                {
                    var from = line[line.Count - 1];
                    foreach (var q in new[] { new Vector2(from.x - .6f, 676f), new Vector2(from.x - 1.2f, 684f), new Vector2(from.x - 1.2f, 692f), new Vector2(from.x - .5f, 702f), new Vector2(3104f, 706f), new Vector2(3118f, 710f) })
                        line.Add(RaceRoute.Ground(q.x, q.y));
                    line.AddRange(LapSlice(AbsStation(RaceRoute.Ground(3125f, 716f)), sb + 30f));
                }
                else line.AddRange(LapSlice(sb + 2f, sb + 30f));
            }
            Place(line[0], line[2] - line[0], 15f); yield return new WaitForFixedUpdate();
            float t0 = -1f, t1 = -1f, clock = 0f, air = 0f, jump = 0f, up = -1f; Vector3 land = new Vector3(float.NaN, 0f, 0f); bool onRamp = false, offRamp = false, landed = false; var touched = new HashSet<string>();
            var drive = DriveLine(line, 99f, 50f, run, null, r =>
            {
                var p = car.Body.position; clock += Time.fixedDeltaTime;
                if (t0 < 0f && Vector3.Distance(Flat(p), Flat(a)) < 5f) t0 = clock;
                if (t0 >= 0f && t1 < 0f && Vector3.Distance(Flat(p), Flat(b)) < 6f) t1 = clock;
                if (Physics.Raycast(p + Vector3.up * .5f, Vector3.down, out var h, 4.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    var n = h.collider.name;
                    if (n.StartsWith("sc roof") || n.StartsWith("sc roofer") || n.StartsWith("House ")) touched.Add(n.StartsWith("House ") ? n + " (its box)" : n);
                    if (n.StartsWith("sc roofer")) onRamp = true;
                }
                if (onRamp && !landed)
                {
                    if (car.GroundedWheels == 0) { offRamp = true; air += Time.fixedDeltaTime; }
                    else if (offRamp && air > .3f) { landed = true; jump = air; land = p; up = car.transform.up.y; }
                }
                return false;
            }, 5f, true, null);
            yield return drive;
            done(t0 >= 0f && t1 > t0 ? t1 - t0 : -1f, touched.Count > 0 ? string.Join(", ", touched) : "nothing", landed ? jump : -1f, land, up);
        }

        IEnumerator R41Crest()
        {
            report.AppendLine(); report.AppendLine("=== B: THE CREST CUT'S ROOF RAMP (reverse race): taken right and missed, flat out, against the lap");
            yield return StartRevRace();
            if (Crest() == null) { Check("the crest cut is in the reverse race", false); race.EndRace(); yield break; }
            var slowRight = new List<string>(); var fastMiss = new List<string>(); var badLand = new List<string>();
            foreach (var id in new[] { "drifter", "original", "moto" })
            {
                yield return Vehicle(id);
                float lap = -1f, right = -1f, miss = -1f, air = -1f, up = -1f; string what = "", whatMiss = ""; Vector3 land = default;
                var r0 = new Run(); yield return TimedCrest(0, r0, (t, w, a, l, u) => lap = t);
                var r1 = new Run(); yield return TimedCrest(1, r1, (t, w, a, l, u) => { right = t; what = w; air = a; land = l; up = u; });
                File.WriteAllLines(Path.Combine(outputDir, $"crest_{id}_right.csv"), r1.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
                var r2 = new Run(); yield return TimedCrest(2, r2, (t, w, a, l, u) => { miss = t; whatMiss = w; });
                File.WriteAllLines(Path.Combine(outputDir, $"crest_{id}_miss.csv"), r2.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
                if (!(right > 0f && lap > 0f && right < lap - .3f)) slowRight.Add(Nm(id));
                if (miss > 0f && lap > 0f && miss < lap) fastMiss.Add(Nm(id));
                if (air < 0f || up < .7f || !r1.reached) badLand.Add(Nm(id));
                report.AppendLine($"  {Nm(id),-18} the lap {lap:0.0} s; taken right {right:0.0} s ({(right > 0 && lap > 0 ? $"saves {lap - right:0.0} s" : "-")}), touched {what}, off the ramp {air:0.00} s in the air, down at ({land.x:0}, {land.z:0}) upright {up:0.00}, " +
                                  $"{(r1.reached ? "on" : "STOPPED " + r1.end)}; missed {(miss > 0 ? miss.ToString("0.0") + " s" : "never through")} ({(miss > 0 && lap > 0 ? $"{miss - lap:+0.0;-0.0} s against the lap" : r2.end)}), touched {whatMiss}");
            }
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("B: the roof ramp taken right saves time against the lap (Drifter Twin, Street Classic, Needle 600)", slowRight.Count == 0, slowRight.Count == 0 ? "" : "not: " + string.Join(", ", slowRight));
            Check("B: taken right, every one comes down on its wheels past CP4 and drives on", badLand.Count == 0, badLand.Count == 0 ? "" : "not: " + string.Join(", ", badLand));
            Check("B: missed, it is slower than the lap", fastMiss.Count == 0, fastMiss.Count == 0 ? "" : "faster than the lap: " + string.Join(", ", fastMiss));
        }

        IEnumerator R41CrestPics()
        {
            report.AppendLine(); report.AppendLine("=== B: pictures of the crest cut's roof ramp");
            yield return StartRevRace();
            car.Body.position = RaceRoute.Surface(3040f, 690f) + Vector3.up; car.Body.linearVelocity = Vector3.zero;
            var foot = RaceRoute.Ground(RoofRampFoot.x, RoofRampFoot.y); var u = new Vector3(Mathf.Sin(27f * Mathf.Deg2Rad), 0f, Mathf.Cos(27f * Mathf.Deg2Rad)); var side = Vector3.Cross(Vector3.up, u);
            yield return Still("41_crest_from_the_gap", RaceRoute.Ground(3091.5f, 645f) + Vector3.up * 2.2f, foot + u * 6f + Vector3.up * 2f, 62f);
            yield return Still("41_crest_ramp_from_terrace", foot - u * 7f + Vector3.up * 2.4f, foot + u * 12f + Vector3.up * 2.5f, 62f);
            yield return Still("41_crest_ramp_side", foot + u * 6f + side * 16f + Vector3.up * 4f, foot + u * 6f + Vector3.up * 2f, 60f);
            yield return Still("41_crest_side_yard_boulders", RaceRoute.Ground(3092f, 668f) + Vector3.up * 6f, RaceRoute.Ground(3078f, 683f), 70f);
            yield return Still("41_crest_landing", foot + u * 75f + side * 8f + Vector3.up * 6f, foot + u * 40f, 62f);
            race.EndRace();
            yield return StartRevRace(); yield return Vehicle("original");
            var sc = Crest(); bool shot = false; var line = LapSlice(AbsStation(sc.Path[0]) - 50f, AbsStation(sc.Path[0])).ToList(); line.AddRange(sc.Path); line.AddRange(LapSlice(AbsStation(sc.Path[sc.Path.Length - 1]) + 2f, AbsStation(sc.Path[sc.Path.Length - 1]) + 30f));
            Place(line[0], line[2] - line[0], 15f); yield return new WaitForFixedUpdate();
            var run = new Run(); var lip = foot + u * 12f;
            var dr = StartCoroutine(DriveLine(line, 99f, 40f, run, null, r => { float d = Vector3.Distance(Flat(car.Body.position), Flat(lip)); if (!shot && car.GroundedWheels == 0 && d > 10f && d < 28f && car.Body.position.z > lip.z) { shot = true; StartCoroutine(Capture("41_crest_jump_chase")); } return false; }, 5f, true, null));
            while (!run.reached && run.end == "" && run.time < 39.9f) yield return new WaitForFixedUpdate();
            yield return dr;
            race.EndRace(); RaceController.DirectionOverride = null;
            Check("B: pictures written", true, "41_crest_*.png");
        }

        // ------------------------------------------------------------ Part C: the five pipe exits
        static readonly (string id, Vector2 a, Vector2 b)[] R41Mouths =
        {
            ("BF", new Vector2(1409.97f, 1176.84f), new Vector2(1425.72f, 1194.95f)), ("X_W20", new Vector2(1373.5f, 800.0f), new Vector2(1457.36f, 839.98f)),
            ("X_W11", new Vector2(2686.0f, 429.5f), new Vector2(2689.84f, 465.29f)), ("X_BRIDGE", new Vector2(2422.5f, 780.0f), new Vector2(2394.0f, 779.33f)),
            ("X_STREAM", new Vector2(470.0f, 1425.0f), new Vector2(507.48f, 1443.74f)),
        };
        IEnumerator R41Pipes()
        {
            report.AppendLine(); report.AppendLine("=== C: the five pipe exits (BF, X_W20, X_W11, X_BRIDGE, X_STREAM): from outside, BUG-001's spot, driven in and out");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            var bug = new Vector3(2676.13f, 293.61f, 448.57f);
            car.Body.position = RaceRoute.Ground(2650f, 470f) + Vector3.up; car.Body.linearVelocity = Vector3.zero;
            yield return Still("41_pipe_bug001_spot", bug, bug + Quaternion.Euler(13.5f, 39.48f, 0f) * Vector3.forward * 30f, 60f);
            var bad = new List<string>();
            foreach (var (id, a, b) in R41Mouths)
            {
                var d = (b - a).normalized; var d3 = new Vector3(d.x, 0f, d.y); var n3 = Vector3.Cross(Vector3.up, d3);
                var mouth = RaceRoute.Ground(a.x, a.y); float fy = DrainPipe.LineHeight(a.x + d.x * 3f, a.y + d.y * 3f, mouth.y); var m = new Vector3(a.x, fy, a.y);
                car.Body.position = RaceRoute.Ground(a.x - d.x * 60f, a.y - d.y * 60f) + Vector3.up; car.Body.linearVelocity = Vector3.zero;
                yield return Still($"41_pipe_{id}_outside", m - d3 * 18f + n3 * 5f + Vector3.up * 5f, m + Vector3.up * 1.5f, 60f);
                yield return Still($"41_pipe_{id}_side", m - d3 * 6f + n3 * 14f + Vector3.up * 6f, m + Vector3.up * .5f, 62f);
                // in: from 12 m out in front of the cut's outer end along its own floor (the cut may bend) to 24 m along it; out: back the same way
                var dp = FindObjectsByType<DrainPipe>(FindObjectsSortMode.None).Where(q => q.floor != null && q.floor.Length > 10).OrderBy(q => Vector2.Distance(new Vector2(q.floor[0].x, q.floor[0].z), a)).FirstOrDefault();
                if (dp == null || Vector2.Distance(new Vector2(dp.floor[0].x, dp.floor[0].z), a) > 6f) { bad.Add(id + " (no pipe found)"); continue; }
                var f0 = dp.floor[0]; var fo = dp.floor[0] - dp.floor[Mathf.Min(6, dp.floor.Length - 1)]; fo.y = 0f; fo.Normalize();
                var lin = new List<Vector3>(); for (float o = 12f; o > .5f; o -= 2f) lin.Add(RaceRoute.Ground(f0.x + fo.x * o, f0.z + fo.z * o));
                float acc = 0f; for (int i = 0; i < dp.floor.Length && acc < 24f; i++) { if (i > 0) acc += Vector3.Distance(dp.floor[i - 1], dp.floor[i]); if (i % 2 == 0) lin.Add(dp.floor[i]); }
                Place(lin[0], lin[2] - lin[0], 6f); yield return new WaitForFixedUpdate();
                var rin = new Run(); yield return DriveLine(lin, 14f, 25f, rin, null, null, 5f, true, null);
                var lout = Enumerable.Reverse(lin).ToList(); Place(lout[0], lout[2] - lout[0], 4f); yield return new WaitForFixedUpdate();
                var rout = new Run(); yield return DriveLine(lout, 14f, 25f, rout, null, null, 5f, true, null);
                if (!rin.reached || !rout.reached) { bad.Add(id); File.WriteAllLines(Path.Combine(outputDir, $"pipe_{id}_in.csv"), rin.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); File.WriteAllLines(Path.Combine(outputDir, $"pipe_{id}_out.csv"), rout.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                report.AppendLine($"  {id,-9} in {(rin.reached ? "through" : "STOPPED " + rin.end)} (slowest {rin.minSpeed:0.0} m/s), out {(rout.reached ? "through" : "STOPPED " + rout.end)} (slowest {rout.minSpeed:0.0} m/s)");
            }
            Check("C: each of the five exits driven in and out", bad.Count == 0, bad.Count == 0 ? "" : "not: " + string.Join(", ", bad));
        }
    }
}
