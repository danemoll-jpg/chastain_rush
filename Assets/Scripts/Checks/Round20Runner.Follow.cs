using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // The Round 20 follow-ups, checked in the exe (-exeRound20 <dir> -r20Only n2,n5,r1,fences,tramp [-soundsRoot <test dir>]):
    //   n2      N2 "Low creek leap": each trail vehicle flat out along its line (launch, air, where it comes down, dry, on its wheels,
    //           back on the trail); the Trail Four slow (rolls off into the creek: does it drive out); the AI once per trail vehicle
    //   n5      N5 "Ford leap": each trail vehicle; an AI driver that did not take P1 takes it by itself, and CP2 still counts
    //   r1      R1 on Dan's line: every vehicle flat out (clean over the pool, through the gate, onto the lane), the time against the lap;
    //           the Street Classic held at several speeds into the ramp (the speed window); slow into the pool (the time it costs);
    //           the AI clean and sloppy; pictures
    //   fences  the fences that stand only in a reverse race: hidden in free roam and a forward race, standing in a reverse race
    //   tramp   a test drop-in trampoline sound (in the -soundsRoot test folder) is read and played on a bounce
    public sealed partial class Round20Runner
    {
        System.Action<BigJump, ArcadeVehicle, float> jumpHook;
        float hookLaunch = -1f; BigJump hookAt; ArcadeVehicle hookWho;
        void HookJumps() { if (jumpHook != null) return; jumpHook = (b, c, v) => { hookLaunch = v; hookAt = b; hookWho = c; }; BigJump.Launched += jumpHook; }
        void ClearHook() { hookLaunch = -1f; hookAt = null; hookWho = null; }

        sealed class AiResult { public bool ok, wet, given, back, took; public float launch = -1f; public FlightRec f; public int recoveries; public string where = ""; }

        // one AI driver through shortcut 'scId' (forced, or on its own with force = false), from 'startBack' m before its turn-off
        IEnumerator AiThrough(string scId, string jumpId, string vid, bool? sloppy, AiResult res, bool force = true, float startBack = 120f, int nextCp = -1)
        {
            RaceController.AiCount = 1; RaceController.AiVehiclesOverride = new[] { vid };
            race.StartRace();
            while (race.State != RaceController.Stage.Racing) yield return null;
            var e = race.Entrants.First(x => !x.IsPlayer); var d = e.Driver;
            var ais = race.AiShortcuts.First(x => x.Id == scId); var rs = race.Route.Shortcuts.First(x => x.Id == scId);
            var j = BigJump.All.FirstOrDefault(b => b.id == jumpId);
            var p = race.AiRoad.At(ais.entryRoad - startBack, out var fw); fw = Vector3.ProjectOnPlane(fw, Vector3.up).normalized;
            d.RunIn = null; d.ForcedShortcut = force ? ais : null; d.ForceSloppy = sloppy;
            var rot = Quaternion.LookRotation(fw);
            e.Car.Body.isKinematic = false; e.Car.Body.position = p + Vector3.up * 0.6f; e.Car.Body.rotation = rot; e.Car.transform.SetPositionAndRotation(e.Car.Body.position, rot);
            e.Car.Body.linearVelocity = fw * 16f; e.Car.Body.angularVelocity = Vector3.zero; e.Car.ClearSteering(); Physics.SyncTransforms();
            e.ClockStarted = true; e.StartAt = e.LapStart = race.RaceClock; e.NextCheckpoint = nextCp >= 0 ? nextCp : rs.FirstForgiven; e.Finished = false; e.ActiveShortcut = null;
            e.NotifyTeleported();
            int rec0 = d.RecoveryCount, taken0 = e.ShortcutsTaken.Count, miss0 = e.MissedTotal; float pen0 = e.PenaltyTotal; int held0 = PoolHold.Held;
            ClearHook(); res.f = Flight(); float t = 0f; bool on = false;
            while (t < 80f)
            {
                yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                if (j != null) res.f.Step(this, j, hookAt == j && hookWho == e.Car, Time.fixedDeltaTime, e.Car);
                if (hookAt == j && hookWho == e.Car && res.launch < 0f) res.launch = hookLaunch;
                if (e.Car.WaterImmersion > 0.3f && (j == null || Flat(e.Car.Body.position - j.lip).magnitude < 25f || InR1Pool(e.Car.Body.position))) res.wet = true;
                if (d.OnShortcut == ais) on = true;
                float s = race.AiRoad.Project(e.Car.Body.position, out float lat);
                float past = Mathf.Repeat(s - ais.exitRoad, race.AiRoad.Length);
                if (on && d.OnShortcut == null && Mathf.Abs(lat) < 6f && past > 15f && past < 200f) { res.back = true; break; }
            }
            if (PoolHold.Held > held0) res.wet = true;   // held in R1's pool (an AI car's own immersion is not always measured)
            res.took = on; res.recoveries = d.RecoveryCount - rec0;
            res.given = e.PenaltyTotal == pen0 && e.MissedTotal == miss0 && (rs.LastForgiven < 0 || e.ShortcutsTaken.Count > taken0);
            res.where = $"({e.Car.Body.position.x:0}, {e.Car.Body.position.z:0})";
            d.ForcedShortcut = null; d.ForceSloppy = null; race.EndRace(); yield return null;
        }

        // ---------------------------------------------------------------- N2

        IEnumerator N2(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== Follow-up: N2 \"Low creek leap\" (official, forgives CP4): each trail vehicle flat out along its line");
            UseRoute("noonday_creek.json", "forward"); HookJumps();
            var j = BigJump.All.FirstOrDefault(b => b.id == "N2"); var rs = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "n2_creek_leap");
            if (j == null || rs == null) { Check("N2 is built and in the route", false); yield break; }
            var line = rs.Path.ToList(); var bad = new List<string>(); var airs = new List<float>(); bool shot = false;
            foreach (var id in Small.Where(ids.Contains))
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                ClearHook(); var f = Flight(); var run = new Run();
                var drive = StartCoroutine(DriveLine(line, 99f, 40f, run, null, null, 3f));
                while (!run.reached && run.end == "" && run.time < 39.9f)
                {
                    f.Step(this, j, hookAt == j && hookWho == car, Time.deltaTime);
                    if (!shot && hookAt == j && car.GroundedWheels == 0 && Vector3.Dot(Flat(car.Body.position - j.lip), j.dir) > 12f) { shot = true; yield return Capture("n2_over_the_creek"); }
                    yield return null;
                }
                yield return drive;
                bool ok = run.reached && hookAt == j && f.air >= 1.3f && !f.wet && f.up > 0.7f;
                if (!ok) { bad.Add(Nm(id)); File.WriteAllLines(Path.Combine(outputDir, $"n2_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                if (hookAt == j) airs.Add(f.air);
                report.AppendLine($"  {Nm(id),-16} {(hookAt == j ? $"onto the lip at {hookLaunch,4:0.0} m/s (off it at {Mathf.Clamp(hookLaunch, j.vmin, j.vmax):0.0}), {f.air:0.00} s in the air, {f.top:0.0} m over the lip, down {f.dist,4:0.0} m past it {(f.wet ? "IN THE WATER" : "on dry ground")}, upright {f.up:0.00}" : "NEVER LEFT THE LIP")}, {(run.reached ? $"back on the trail ({run.time:0.0} s)" : "DID NOT GET BACK " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}");
            }
            Check("N2: every trail vehicle over the creek on dry ground, on its wheels, back on the trail", bad.Count == 0, (bad.Count > 0 ? "not: " + string.Join(", ", bad) + "; " : "") + (airs.Count > 0 ? $"air {airs.Min():0.00} to {airs.Max():0.00} s" : ""));
            // slow: the Trail Four at 9 m/s (no lift under 10): off the lip into the creek; never a trap
            {
                yield return Vehicle("atv");
                int from = line.FindIndex(q => Vector3.Dot(Flat(q - j.lip), j.dir) > -30f);
                var slow = line.Skip(Mathf.Max(0, from)).ToList();
                Place(slow[0], slow[2] - slow[0], 8f);
                var run = new Run(); ClearHook();
                yield return DriveLine(slow, 9f, 90f, run, null, null, 3f, false);
                Check("N2 slow (the Trail Four held at 9 m/s): comes off the lip short and drives out to the trail, no reset", run.reached,
                      $"{(hookAt == j ? $"crossed the lip at {hookLaunch:0.0} m/s" : "no launch")}, {(run.reached ? $"out and on in {run.time:0} s" : "STOPPED " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})")}");
            }
            // pictures: the turn-off from the trail (the barricade, the tracks, the kicker) and from beside the lip across the creek
            {
                var turn = line[0]; var look = j.lip; var fwd = Flat(line[3] - line[0]).normalized;
                car.Body.position = j.lip + Vector3.Cross(Vector3.up, j.dir) * 40f + Vector3.up * 3f; car.Body.linearVelocity = Vector3.zero;
                yield return Still("n2_turn_off", turn - fwd * 6f + Vector3.up * 2.2f, look + Vector3.up * 0.5f);
                yield return Still("n2_from_the_lip", j.lip - j.dir * 10f + Vector3.up * 2.5f, j.lip + j.dir * 30f);
            }
            // the AI: each trail vehicle once, made to take it
            Time.timeScale = 4f; Time.maximumDeltaTime = 1f; int prev = RaceController.AiCount; var aiBad = new List<string>();
            foreach (var id in Small.Where(ids.Contains))
            {
                var r = new AiResult(); yield return AiThrough("n2_creek_leap", "N2", id, null, r);
                bool ok = r.back && r.given && r.recoveries == 0 && r.launch > 0f && !r.f.wet;
                if (!ok) aiBad.Add(Nm(id));
                report.AppendLine($"  AI {Nm(id),-15} {(ok ? "OK  " : "FAIL")} {(r.launch > 0f ? $"onto the lip at {r.launch:0.0} m/s, {r.f.air:0.00} s in the air, down {r.f.dist:0.0} m past it {(r.f.wet ? "IN THE WATER" : "on dry ground")}" : "never left the lip")}, CP4 forgiven {(r.given ? "yes" : "no")}, recoveries {r.recoveries}{(r.back ? ", back on the trail" : ", ended at " + r.where)}");
            }
            RaceController.AiCount = prev; RaceController.AiVehiclesOverride = null; Time.timeScale = 1f; Time.maximumDeltaTime = 0.3333333f;
            Check("N2: the AI takes it (each trail vehicle once): over the creek, CP4 forgiven, no recovery, back on the trail", aiBad.Count == 0, aiBad.Count > 0 ? "not: " + string.Join(", ", aiBad) : "");
        }

        // ---------------------------------------------------------------- N5

        IEnumerator N5(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== Follow-up: N5 \"Ford leap\" (part of the lap, beside the hairpin's ford): each trail vehicle flat out along its line");
            UseRoute("noonday_creek.json", "forward"); HookJumps();
            var j = BigJump.All.FirstOrDefault(b => b.id == "N5"); var rs = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "n5_ford_leap");
            if (j == null || rs == null) { Check("N5 is built and in the route (a lap option)", false); yield break; }
            var line = rs.Path.ToList(); var bad = new List<string>(); var airs = new List<float>(); bool shot = false;
            foreach (var id in Small.Where(ids.Contains))
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                ClearHook(); var f = Flight(); var run = new Run();
                var drive = StartCoroutine(DriveLine(line, 99f, 30f, run, null, null, 3f));
                while (!run.reached && run.end == "" && run.time < 29.9f)
                {
                    f.Step(this, j, hookAt == j && hookWho == car, Time.deltaTime);
                    if (!shot && hookAt == j && car.GroundedWheels == 0 && Vector3.Dot(Flat(car.Body.position - j.lip), j.dir) > 10f) { shot = true; yield return Capture("n5_over_the_creek"); }
                    yield return null;
                }
                yield return drive;
                bool ok = run.reached && hookAt == j && f.air >= 1.1f && !f.wet && f.up > 0.7f;
                if (!ok) { bad.Add(Nm(id)); File.WriteAllLines(Path.Combine(outputDir, $"n5_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                if (hookAt == j) airs.Add(f.air);
                report.AppendLine($"  {Nm(id),-16} {(hookAt == j ? $"onto the lip at {hookLaunch,4:0.0} m/s (off it at {Mathf.Clamp(hookLaunch, j.vmin, j.vmax):0.0}), {f.air:0.00} s in the air, {f.top:0.0} m over the lip, down {f.dist,4:0.0} m past it {(f.wet ? "IN THE WATER" : "on dry ground")}, upright {f.up:0.00}" : "NEVER LEFT THE LIP")}, {(run.reached ? $"back on the trail ({run.time:0.0} s)" : "DID NOT GET BACK " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}");
            }
            Check("N5: every trail vehicle over the creek on dry ground, on its wheels, back on the trail before CP2", bad.Count == 0, (bad.Count > 0 ? "not: " + string.Join(", ", bad) + "; " : "") + (airs.Count > 0 ? $"air {airs.Min():0.00} to {airs.Max():0.00} s" : ""));
            {
                var fwd = Flat(line[3] - line[0]).normalized;
                car.Body.position = j.lip + Vector3.Cross(Vector3.up, j.dir) * 40f + Vector3.up * 3f; car.Body.linearVelocity = Vector3.zero;
                yield return Still("n5_from_the_trail", line[0] - fwd * 4f + Vector3.up * 2.2f, j.lip + Vector3.up * 0.5f);
            }
            // the AI, not made to take it: put down just after P1's turn-off (so it did not take P1), with CP2 next; it takes N5 by
            // itself, crosses CP2 as usual (no penalty), back on the trail
            Time.timeScale = 4f; Time.maximumDeltaTime = 1f; int prev = RaceController.AiCount; var aiBad = new List<string>();
            int cp2 = race.Route.Checkpoints.FindIndex(g => g.DataId == 2);
            foreach (var id in Small.Where(ids.Contains))
            {
                var r = new AiResult(); yield return AiThrough("n5_ford_leap", "N5", id, null, r, false, 32f, cp2);
                bool ok = r.took && r.back && r.given && r.recoveries == 0 && r.launch > 0f && !r.f.wet;
                if (!ok) aiBad.Add(Nm(id));
                report.AppendLine($"  AI {Nm(id),-15} {(ok ? "OK  " : "FAIL")} {(r.took ? "took it by itself" : "DID NOT TAKE IT")}, {(r.launch > 0f ? $"onto the lip at {r.launch:0.0} m/s, {r.f.air:0.00} s in the air, down {r.f.dist:0.0} m past it {(r.f.wet ? "IN THE WATER" : "on dry ground")}" : "never left the lip")}, CP2 counted with no penalty {(r.given ? "yes" : "no")}, recoveries {r.recoveries}{(r.back ? ", back on the trail" : ", ended at " + r.where)}");
            }
            RaceController.AiCount = prev; RaceController.AiVehiclesOverride = null; Time.timeScale = 1f; Time.maximumDeltaTime = 0.3333333f;
            Check("N5: an AI driver that did not take P1 takes it by itself (each trail vehicle once): over the creek, CP2 counted, no recovery", aiBad.Count == 0, aiBad.Count > 0 ? "not: " + string.Join(", ", aiBad) : "");
        }

        // ---------------------------------------------------------------- R1

        // in R1's above-ground pool (its water's outline, a little grown)
        bool InR1Pool(Vector3 p)
        {
            foreach (var w in ShallowWater.Active) if (w && w.name.Contains("r1_pool_jump") && w.outline != null && w.outline.Length > 2)
            {
                var c = Vector2.zero; foreach (var q in w.outline) c += q; c /= w.outline.Length; float r = (w.outline[0] - c).magnitude;
                if ((new Vector2(p.x, p.z) - c).magnitude < r + 0.5f) return true;
            }
            return false;
        }

        Vector3 GatePoint()
        {
            var posts = FindObjectsByType<CapsuleCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c => c.name == "gate post").ToList();
            return posts.Count >= 2 ? (posts[0].transform.position + posts[1].transform.position) * 0.5f : Vector3.zero;
        }

        IEnumerator R1(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== Follow-up: R1 on Dan's line, with risk (reverse; forgives CP5): ramp over the fence, the pool, the side gate, onto the lane");
            UseRoute("chastain_glen_loop.json", "reverse"); HookJumps();
            var rs = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "r1_pool_jump"); var j = BigJump.All.FirstOrDefault(b => b.id == "r1_pool_jump");
            if (rs == null || j == null) { Check("R1 is built and in the reverse route", false); yield break; }
            var line = rs.Path.ToList(); var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
            var gate = GatePoint(); float sG = Project(line, cum, gate, out float gdev);
            report.AppendLine($"  the side gate at ({gate.x:0.0}, {gate.z:0.0}), {gdev:0.0} m off the line, {sG:0} m along it; the lip at {Project(line, cum, j.lip, out _):0} m");
            var gateProfile = new[] { sG - 10f, 9f, sG + 6f, 9f, sG + 7f, 99f };   // a driver lines up for the gate at 9 m/s
            // the lap between the same two points, for the time saved
            float sa = AbsStation(line[0]), sb = AbsStation(line[line.Count - 1]); if (sb < sa) sb += race.Route.LapLength;
            var lapLine = LapSlice(sa, sb);
            var tap = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>();
            var bad = new List<string>(); var saved = new List<float>(); bool shot = false;
            foreach (var id in ids)
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
                ClearHook(); var f = Flight(); var run = new Run(); bool wet = false; tap.seen.Clear(); tap.on = true;
                var drive = StartCoroutine(DriveLine(line, 99f, 40f, run, null, null, 3f, true, gateProfile));
                while (!run.reached && run.end == "" && run.time < 39.9f)
                {
                    f.Step(this, j, hookAt == j && hookWho == car, Time.deltaTime);
                    if (car.WaterImmersion > 0.3f && InR1Pool(car.Body.position)) wet = true;
                    if (!shot && hookAt == j && car.GroundedWheels == 0 && Vector3.Dot(Flat(car.Body.position - j.lip), j.dir) > 5f) { shot = true; yield return Capture("r1_over_the_pool"); }
                    yield return null;
                }
                yield return drive; tap.on = false;
                // a real hit on a fence or a gate post (a skim with no impulse, the car's tail over the first fence's boards, costs nothing)
                float Imp(string s) { int k = s.LastIndexOf("impulse "); return k >= 0 && float.TryParse(s.Substring(k + 8), out float v) ? v : 0f; }
                var touched = tap.seen.Where(s => s.Contains("gate post") || s.Contains("panel")).Distinct().ToList();
                var hits = touched.Where(s => Imp(s) >= 100f).ToList(); int skims = touched.Count - hits.Count;
                // the lap between the same points, flat out
                Place(lapLine[0], lapLine[2] - lapLine[0], 14f); yield return new WaitForFixedUpdate();
                var lapRun = new Run(); yield return DriveLine(lapLine, 99f, 60f, lapRun, null, null, 4f);
                float gain = lapRun.reached && run.reached ? lapRun.time - run.time : float.NaN; if (!float.IsNaN(gain)) saved.Add(gain);
                bool ok = run.reached && hookAt == j && !wet && !f.wet && f.up > 0.7f && hits.Count == 0;
                if (!ok) { bad.Add(Nm(id)); File.WriteAllLines(Path.Combine(outputDir, $"r1_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                report.AppendLine($"  {Nm(id),-18} {(hookAt == j ? $"onto the lip at {hookLaunch,4:0.0} m/s (off it at {Mathf.Min(hookLaunch, j.vmax):0.0}), {f.air:0.00} s in the air, down {f.dist,4:0.0} m past the lip {(wet || f.wet ? "IN THE POOL" : "clear of the pool")}, upright {f.up:0.00}" : "NEVER LEFT THE LIP")}; " +
                                  $"{(run.reached ? $"through the gate and on the lane in {run.time:0.0} s" : "DID NOT GET THROUGH " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}{(hits.Count > 0 ? "; HIT: " + string.Join("; ", hits.Take(2)) : "")}{(skims > 0 ? $"; brushed a gate post or a fence {skims} time(s) with no impulse" : "")}; the lap between the same points {(lapRun.reached ? $"{lapRun.time:0.0} s: saves {gain:0.0} s" : "not driven to the end")}");
            }
            Check("R1 clean: every vehicle flat out at the ramp clears the pool, comes down upright, lines up through the side gate and onto the lane", bad.Count == 0, (bad.Count > 0 ? "not: " + string.Join(", ", bad) + "; " : "") + (saved.Count > 0 ? $"saves {saved.Min():0.0} to {saved.Max():0.0} s against the lap" : ""));
            // the speed window: the Street Classic held at a speed into the ramp (and on to the gate)
            report.AppendLine("  The speed window (Street Classic held at a speed up the ramp; every vehicle flies the same arc for the same lip speed; short of the pool it runs into its wall):");
            float poolV = rs.AiSlipProfile != null && rs.AiSlipProfile.Length > 1 ? rs.AiSlipProfile[1] : 20f;
            float cleanFrom = 99f;
            foreach (float cap in new[] { 15f, 18f, poolV - 0.5f, poolV, poolV + 0.5f, j.vmax - 0.5f, j.vmax, 24f })
            {
                yield return Vehicle("original");
                int from = line.FindIndex(q => Vector3.Dot(Flat(q - j.lip), j.dir) > -35f); var part = line.Skip(Mathf.Max(0, from)).ToList();
                Place(part[0], part[2] - part[0], cap); yield return new WaitForFixedUpdate();
                ClearHook(); var f = Flight(); var run = new Run(); bool wet = false;
                float sGp = sG - cum[Mathf.Max(0, from)];   // the gate, along this part: lined up for it at 9 m/s, as in the clean runs
                var drive = StartCoroutine(DriveLine(part, cap, 50f, run, null, null, 3f, false, new[] { sGp - 10f, 9f, sGp + 6f, 9f, sGp + 7f, 99f }));
                while (!run.reached && run.end == "" && run.time < 49.9f) { f.Step(this, j, hookAt == j && hookWho == car, Time.deltaTime); if (car.WaterImmersion > 0.3f && InR1Pool(car.Body.position)) wet = true; yield return null; }
                yield return drive;
                if (!wet && !f.wet && hookAt == j && run.reached) cleanFrom = Mathf.Min(cleanFrom, hookLaunch);   // clear of the pool and on through the gate
                report.AppendLine($"    {cap,4:0} m/s: {(hookAt == j ? $"off the lip at {hookLaunch:0.0}, down {f.dist:0.0} m past it" : "no launch")}, {(wet || f.wet ? "IN THE POOL" : "clear")}, {(run.reached ? $"out onto the lane ({run.time:0.0} s from 35 m before the lip)" : "STOPPED " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}), {Vector3.Dot(Flat(car.Body.position - j.lip), j.dir):0.0} m past the lip")}");
            }
            Check("R1 risk: only a fast, committed jump clears the pool (clean from close to the lip's speed cap), slower lands in it or short", cleanFrom > j.vmax - 2.5f && cleanFrom <= j.vmax + 0.01f, $"clear from about {cleanFrom:0.0} m/s off the lip, held to {j.vmax:0.0}");
            // the time a fall into the pool costs: slow into the pool (8 m/s) against flat out, from the same place, to the lane
            {
                var lost = new List<string>(); bool allOut = true;
                foreach (var id in new[] { "original", "moto", "mower" })
                {
                    yield return Vehicle(id);
                    Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
                    var clean = new Run(); yield return DriveLine(line, 99f, 40f, clean, null, null, 3f, true, gateProfile);
                    float sL = Project(line, cum, j.lip, out _);
                    var slowProfile = new[] { sL - 1f, poolV, sL + 6f, poolV, sG - 10f, 9f, sG + 6f, 9f, sG + 7f, 99f };   // up the ramp at the speed that comes down in the pool
                    Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
                    var slow = new Run(); bool wet = false;
                    var drive = StartCoroutine(DriveLine(line, 99f, 90f, slow, null, null, 3f, false, slowProfile));
                    while (!slow.reached && slow.end == "" && slow.time < 89.9f) { if (car.WaterImmersion > 0.3f) wet = true; yield return null; }
                    yield return drive;
                    if (!slow.reached) allOut = false;
                    lost.Add($"{Nm(id)} {(wet ? "in the pool" : "NOT IN THE POOL")}, {(slow.reached ? $"{slow.time - clean.time:0.0} s lost ({slow.time:0.0} against {clean.time:0.0})" : "DID NOT GET OUT " + slow.end)}");
                }
                Check("R1 risk: a fall into the pool costs real time and is never a trap (out up the steps, through the gate, onto the lane)", allOut, string.Join("; ", lost));
            }
            // pictures: from the lap's approach, from over the pool, the gate from the lawn
            {
                var fwd = j.dir; car.Body.position = j.lip + Vector3.Cross(Vector3.up, fwd) * 40f + Vector3.up * 5f; car.Body.linearVelocity = Vector3.zero;
                var foot = j.lip - fwd * 8f;
                yield return Still("r1_approach", foot - fwd * 18f + Vector3.up * 1.8f, j.lip + fwd * 12f + Vector3.up * 1f, 65f);
                yield return Still("r1_side_yard_and_fence", j.lip + fwd * 3f + Vector3.up * 14f - fwd * 6f, j.lip + fwd * 8f, 60f);
                var pw = ShallowWater.Active.FirstOrDefault(w => w && w.name.Contains("r1_pool_jump"));
                if (pw != null)
                {
                    var pc = Vector3.zero; foreach (var q in pw.outline) pc += new Vector3(q.x, 0f, q.y); pc /= pw.outline.Length; pc.y = pw.surfaceY;
                    yield return Still("r1_pool_from_the_landing_strip", pc - fwd * 16f + Vector3.up * 3f, pc + Vector3.up * 0.5f, 60f);   // as a driver sees it, coming
                    yield return Still("r1_pool_from_above", pc - fwd * 7f + Vector3.up * 12f, pc + fwd * 4f, 60f);
                }
                yield return Still("r1_gate_from_the_lawn", gate - Flat(line[Mathf.Min(line.Count - 1, Mathf.Max(0, cum.ToList().FindIndex(c => c > sG + 4f)))] - gate).normalized * 14f + Vector3.up * 2f, gate + Vector3.up * 1f, 60f);
            }
            // the AI: two clean tries and one sloppy one (into the pool, out again)
            Time.timeScale = 4f; Time.maximumDeltaTime = 1f; int prev = RaceController.AiCount; var aiBad = new List<string>();
            foreach (var (id, sloppy) in new[] { ("original", false), ("moto", false), ("skyfin", false), ("original", true) })
            {
                if (!ids.Contains(id)) continue;
                if (sloppy && rs.AiSlip <= 0f) { report.AppendLine("  AI sloppy tries are off on this shortcut (ai_slip 0): not tried"); continue; }
                var r = new AiResult(); yield return AiThrough("r1_pool_jump", "r1_pool_jump", id, sloppy, r);
                bool ok = r.back && r.given && r.launch > 0f && (sloppy ? r.wet : !r.wet);
                if (!ok) aiBad.Add(Nm(id) + (sloppy ? " (sloppy)" : ""));
                report.AppendLine($"  AI {Nm(id),-17} {(sloppy ? "sloppy" : "clean ")} {(ok ? "OK  " : "FAIL")} {(r.launch > 0f ? $"onto the lip at {r.launch:0.0} m/s, {r.f.air:0.00} s in the air" : "never left the lip")}, {(r.wet ? "in the pool" : "clear of the pool")}, CP5 forgiven {(r.given ? "yes" : "no")}, recoveries {r.recoveries}{(r.back ? ", back on the lap" : ", ended at " + r.where)}");
            }
            RaceController.AiCount = prev; RaceController.AiVehiclesOverride = null; Time.timeScale = 1f; Time.maximumDeltaTime = 0.3333333f;
            Check("R1: the AI takes it, clean (over the pool, through the gate) and, on a sloppy try, into the pool and out again; CP5 forgiven, back on the lap", aiBad.Count == 0, aiBad.Count > 0 ? "not: " + string.Join(", ", aiBad) : "");
        }

        // ---------------------------------------------------------------- the reverse-race fences

        IEnumerator Fences()
        {
            report.AppendLine(); report.AppendLine("=== Follow-up: the lobe's fences that stand only during a reverse race (#6's corridor)");
            var ro = FindAnyObjectByType<RaceOnly>();
            if (ro == null) { Check("the reverse-race fences are built", false); yield break; }
            int n = ro.transform.childCount;
            UseRoute("chastain_glen_loop.json", "forward"); yield return null; yield return null;
            bool free = ro.Shown;
            int prev = RaceController.AiCount; RaceController.AiCount = 0;
            race.StartRace(); yield return null; yield return null; bool fwd = ro.Shown; race.EndRace(); yield return null;
            UseRoute("chastain_glen_loop.json", "reverse"); race.StartRace(); yield return null; yield return null; bool rev = ro.Shown;
            var colliders = ro.GetComponentsInChildren<BoxCollider>(false).Length;
            race.EndRace(); yield return null; yield return null; bool after = ro.Shown;
            RaceController.AiCount = prev;
            Check("the 3 fences in #6's corridor: hidden in free roam and a forward race, standing (with their colliders) in a reverse race, gone again after it",
                  !free && !fwd && rev && !after && colliders > 0, $"{n} fences; free roam {(free ? "shown" : "hidden")}, forward race {(fwd ? "shown" : "hidden")}, reverse race {(rev ? "shown" : "hidden")} ({colliders} colliders), after it {(after ? "shown" : "hidden")}");
        }

        // ---------------------------------------------------------------- the trampoline sound drop-in

        IEnumerator Tramp()
        {
            report.AppendLine(); report.AppendLine("=== Follow-up: the trampoline sound drop-in (Sounds\\Trampoline\\trampoline*)");
            var sd = SoundDropIns.Instance; float t0 = Time.realtimeSinceStartup;
            while (sd != null && !sd.Loaded && Time.realtimeSinceStartup - t0 < 10f) yield return null;
            var readMe = Path.Combine(SoundDropIns.Root, "Trampoline", "READ ME.txt");
            Check("the Trampoline drop-in folder and its READ ME are made (in this check's test folder)", File.Exists(readMe), readMe);
            Check("a trampoline* file in it is read", sd != null && sd.Bounces.Count > 0, sd != null ? $"{sd.Bounces.Count} file(s): {string.Join(", ", sd.Bounces.Select(c => c.name))}" : "no SoundDropIns");
            var tr = FindAnyObjectByType<Trampoline>();
            if (tr == null) { Check("the trampoline is there", false); yield break; }
            yield return Vehicle("original");
            var c = tr.transform.position;
            respawn.PlaceAt(new Vector3(c.x, tr.matY + 1.4f, c.z), 0f); car.Body.linearVelocity = Vector3.down * 5f; race.NotifyTeleported();
            float t = 0f; while (t < 3f && Trampoline.LastBounceClip == "") { t += Time.deltaTime; yield return null; }
            Check("a bounce plays the drop-in sound instead of the made one", sd != null && sd.Bounces.Count > 0 && Trampoline.LastBounceClip == sd.Bounces[0].name, $"played: \"{Trampoline.LastBounceClip}\"");
            yield return new WaitForSecondsRealtime(5f);   // the trampoline steers a thrown vehicle for its whole flight (4 s): let it land before the next part moves it
        }
    }
}
