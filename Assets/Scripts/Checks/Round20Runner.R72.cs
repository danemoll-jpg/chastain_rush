using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 72 in the exe (-exeRound72 <dir> [-r72Only qjump,qshort,qreplay,qpics] -logFile <dir>\Player.log): the Viking Quarry jumps, the real vehicle
    // driven by script from the gantry (the event's own start: stopped in the box, the countdown) down the run-up to the lip at a held speed:
    //   qjump    each jump on the Street Classic, the Drifter Twin, the Needle 600 and the Stunt Bike at the middle of its speed window (or flat out
    //            where the vehicle cannot reach it), and the Drifter Twin flat out: the event's own result (landing, distance, air, height), the Top 10
    //   qshort   short (6 m/s under the window): into the lake (SPLASH), back at the gantry
    //   qreplay  the last jump's replay: the viewer opens on the jump's TV cameras
    //   qpics    the three ramps from their run-ups, mid-air over the lake, the crowd, a landing
    public sealed partial class Round20Runner
    {
        static readonly string[] R72Vehicles = { "original", "drifter", "moto", "stuntbike" };

        // a jump driven by script: placed at the gantry, held for the countdown, then the run-up at 'cap' m/s; the event's result
        IEnumerator R72Jump(QuarryJumps q, QuarryJumps.Jump j, string vid, float cap, string label, List<string> rows, Func<IEnumerator> midAir = null)
        {
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle(vid); FenceChunk.RestoreAll(); yield return new WaitForSecondsRealtime(.3f);
            q.PutAtStart(j); yield return new WaitForSecondsRealtime(.6f);
            float wait = 0f; while (q.State != QuarryJumps.Phase.Run && wait < 8f) { yield return new WaitForFixedUpdate(); wait += Time.fixedDeltaTime; }
            if (q.State != QuarryJumps.Phase.Run) { rows.Add($"  {label}: NO START (the event: {q.State})"); yield break; }
            var line = new List<Vector3>(); for (float s = 3f; s <= j.runout_to; s += 3f) { var p = QuarryJumps.At(j, s); p.y = GroundQuery.Height(p.x, p.z); line.Add(p); }
            var run = new Run(); bool pic = false; var trace = new List<string>(); float nextS = 0f;
            var tch = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); tch.seen.Clear(); tch.on = true;
            var drive = StartCoroutine(DriveLine(line, cap, 60f, run, null, r => q.State == QuarryJumps.Phase.Result, 6f, false));
            while (q.State == QuarryJumps.Phase.Run || q.State == QuarryJumps.Phase.Air)
            {
                yield return new WaitForFixedUpdate();
                { var pp = car.Body.position; float ss = Vector3.Dot(new Vector3(pp.x - j.origin[0], 0f, pp.z - j.origin[1]), QuarryJumps.Forward(j));
                  if (ss >= nextS && q.State == QuarryJumps.Phase.Run) { nextS = ss + 40f; trace.Add($"{ss:0}:{car.Body.linearVelocity.magnitude:0.0}{(car.WaterImmersion > .01f ? "w" : "")}{(car.GroundedWheels == 0 ? "a" : "")}"); } }
                if (midAir != null && !pic && q.State == QuarryJumps.Phase.Air && Time.time > 0f && car.Body.linearVelocity.y < 0f) { pic = true; yield return midAir(); }
                if (run.time > 59f) break;
            }
            yield return drive;
            tch.on = false; rows.Add($"      run-up (station:speed): {string.Join(" ", trace)}; last touched {(tch.seen.Count > 0 ? string.Join("; ", tch.seen.Skip(Math.Max(0, tch.seen.Count - 3))) : "nothing")}");
            yield return new WaitForSecondsRealtime(1.5f);   // the landing settles (a rough one may still wipe out)
            rows.Add($"  {label,-32} lip {q.LastLipSpeed:0.0} m/s ({DisplayUnits.Mph(q.LastLipSpeed):0} mph; window {DisplayUnits.Mph(j.window.lo):0}-{DisplayUnits.Mph(j.window.hi):0}): {q.LastLanding,-6} {q.LastDistance:0} m, {q.LastAir:0.0} s, +{q.LastHeight:0.0} m over the lip{(q.LastRank > 0 ? $", Top 10 #{q.LastRank}" : "")}");
        }

        IEnumerator R72Jumps()
        {
            report.AppendLine(); report.AppendLine("=== The Viking Quarry jumps: each flown from its gantry (the countdown), the run-up held at the speed window's middle (flat out where the vehicle cannot reach it)");
            var q = QuarryJumps.Instance; if (!q || q.Jumps.Length == 0) { Check("qjump: the quarry jumps are in the build", false); yield break; }
            yield return Clock(13f);
            foreach (var j in q.Jumps)
            {
                var rows = new List<string>(); int clean = 0, tried = 0;
                foreach (var vid in R72Vehicles)
                {
                    float mid = (j.window.lo + j.window.hi) / 2f;
                    yield return R72Jump(q, j, vid, mid, $"{j.id} {Nm(vid)} at {mid:0.0} m/s", rows);
                    if (q.LastLipSpeed >= j.window.lo - .3f) { tried++; if (q.LastLanding == "CLEAN") clean++; }
                }
                yield return R72Jump(q, j, "drifter", 99f, $"{j.id} {Nm("drifter")} flat out", rows);
                report.AppendLine($"  {j.id} {j.name}: lip {j.lip_y:0} m, the landing deck {j.land_s[0] - j.lip_s:0} to {j.land_s[j.land_s.Length - 1] - j.lip_s:0} m out");
                foreach (var r in rows) report.AppendLine(r);
                Check($"qjump {j.id} {j.name}: every vehicle that reaches the window lands clean", tried > 0 && clean == tried, $"{clean} of {tried}");
            }
            var top = q.Jumps.Select(j => $"{j.id}: {q.Top(j.id).Count}").ToArray();
            Check("qjump: the jumps go on their Top 10s", q.Jumps.All(j => q.Top(j.id).Count > 0), string.Join(", ", top));
        }

        IEnumerator R72Short()
        {
            report.AppendLine(); report.AppendLine("=== Short: the Street Classic 6 m/s under The Corner's window: into the lake, back at the gantry");
            var q = QuarryJumps.Instance; if (!q) yield break; var j = q.Jumps[0]; var rows = new List<string>();
            yield return R72Jump(q, j, "original", j.window.lo - 6f, $"{j.id} short", rows);
            foreach (var r in rows) report.AppendLine(r);
            yield return new WaitForSecondsRealtime(3.5f);
            float back = Vector3.Distance(car.Body.position, QuarryJumps.StartPoint(j));
            Check("qshort: short into the lake, put back at the gantry", (q.LastLanding == "SPLASH" || q.LastLanding == "SHORT") && back < 12f, $"{q.LastLanding}, {back:0} m from the gantry afterwards");
        }

        IEnumerator R72Replay()
        {
            report.AppendLine(); report.AppendLine("=== The jump's replay");
            var q = QuarryJumps.Instance; if (!q) yield break; var j = q.Jumps[1]; var rows = new List<string>();
            yield return R72Jump(q, j, "drifter", (j.window.lo + j.window.hi) / 2f, $"{j.id} for the replay", rows);
            foreach (var r in rows) report.AppendLine(r);
            bool could = q.CanWatch; q.Watch(); yield return new WaitForSecondsRealtime(3f);
            var v = ReplayViewer.Current; bool open = v != null; int tvs = v ? v.TvSpots.Count : 0; int at = v ? v.TvIndex : -1;
            if (open) { yield return Capture("72_replay"); v.Close(); }
            yield return new WaitForSecondsRealtime(.5f);
            Check("qreplay: the jump's replay opens on its TV cameras", could && open && tvs > 0, $"could {could}, open {open}, {tvs} TV cameras, on #{at}");
        }

        IEnumerator R72Pics()
        {
            report.AppendLine(); report.AppendLine("=== Pictures");
            var q = QuarryJumps.Instance; if (!q) yield break;
            yield return Clock(13f);
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter");
            foreach (var j in q.Jumps)
            {
                var a = QuarryJumps.At(j, j.lip_s - 140f, -9f); a.y = GroundQuery.Height(a.x, a.z) + 3.5f; var b = QuarryJumps.At(j, j.lip_s); b.y = j.lip_y;
                var park = QuarryJumps.At(j, j.lip_s - 300f, 30f); park.y = GroundQuery.Height(park.x, park.z) + .5f; respawn.PlaceAt(park, 0f); race.NotifyTeleported();
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Still($"72_{j.id}_from_runup", a, b + Vector3.up * 2f, 55f);
                // the landing deck and the crowd beside it
                var l0 = QuarryJumps.At(j, j.land_s[0]); l0.y = j.land_y[0];
                var st = j.stands[0]; var sp = new Vector3(st.at[0], 0f, st.at[1]); sp.y = GroundQuery.Height(sp.x, sp.z);
                yield return Still($"72_{j.id}_crowd_and_landing", sp + (sp - l0).normalized * 18f + Vector3.up * 9f, l0, 60f);
            }
            // mid-air over the lake, and the landing
            var rows = new List<string>(); var J = q.Jumps[2];
            yield return R72Jump(q, J, "drifter", (J.window.lo + J.window.hi) / 2f, "pictures", rows, () => R72Frozen("72_midair_over_the_lake", -30f, 14f, 6f));
        }
        IEnumerator R72Frozen(string name, float back, float side, float up)
        {
            Time.timeScale = 0f; var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false;
            var p = car.Body.position; var f = Flat(car.transform.forward).normalized; var r = Vector3.Cross(Vector3.up, f);
            cam.transform.position = p + f * back + r * side + Vector3.up * up; cam.transform.LookAt(p + f * 30f - Vector3.up * 8f);
            yield return null; yield return Capture(name); if (chase) { chase.enabled = was; chase.Snap(); } Time.timeScale = 1f;
            report.AppendLine($"  {name}: at ({p.x:0}, {p.y:0}, {p.z:0})");
        }
    }
}
