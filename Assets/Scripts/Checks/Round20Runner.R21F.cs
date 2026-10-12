using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // The Round 21 follow-ups, checked in the exe (-exeRound21 <dir> -r21Only tube,ra,rc [plus the earlier parts: ramp, n2, fwdlap, revlap]
    // -logFile <dir>\Player.log), one targeted check per piece:
    //   tube   P1's clear stretch: each trail vehicle flat out through P1 forward (through the glass at speed, never slowed or stopped,
    //          out over the pond); the fish are there; a reset in the clear stretch puts the vehicle on the floor facing along it; the
    //          frame rate with the camera in the clear stretch (1280 x 720: not a 3840 x 2160 number); two pictures from inside it
    //   ra     RA "Pipe dive" (reverse): each trail vehicle flat out along its line (onto the heap, the launch, where it comes down on
    //          the apron, through the pipe, back on the trail); slow (rolls off short into the pond: does it get out); the AI once per
    //          trail vehicle, made to take it (CP forgiven, no recovery)
    //   rc     RC "Creek leap back" (reverse): the same; and N2 forward still lands (the twin heap stands in its landing)
    public sealed partial class Round20Runner
    {
        IEnumerator Tube(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== Follow-ups 1: P1's clear stretch (glass under water, fish), forward through P1 on each trail vehicle");
            UseRoute("noonday_creek.json", "forward"); HookJumps();
            var aq = PipeAquarium.Instance; var pipe = FindObjectsByType<DrainPipe>(FindObjectsSortMode.None).FirstOrDefault(p => p.id == "P1");
            var rs = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "p1_pipe");
            if (aq == null || pipe == null || rs == null) { Check("the clear stretch, P1 and its shortcut are built", false); yield break; }
            Vector3 mid = (aq.a0 + aq.a1) * 0.5f, along = (aq.a1 - aq.a0).normalized;
            report.AppendLine($"  clear stretch: {Vector3.Distance(aq.a0, aq.a1):0.0} m of glass, axis at {aq.a0.y:0.00} m, the surface {aq.surfaceY:0.00} m, {aq.FishCount} fish");
            var line = rs.Path.ToList(); var bad = new List<string>(); bool shot = false;
            foreach (var id in Small.Where(ids.Contains))
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                ClearHook(); var run = new Run(); float vIn = -1f, vMin = 99f; bool through = false;
                var drive = StartCoroutine(DriveLine(line, 99f, 60f, run, null, null, 3f));
                while (!run.reached && run.end == "" && run.time < 59.9f)
                {
                    var p = car.Body.position; float s = Vector3.Dot(p - aq.a0, along); var radial = p - (aq.a0 + along * s);
                    bool inGlass = s > 0f && s < Vector3.Distance(aq.a0, aq.a1) && radial.magnitude < aq.radius + 0.5f;
                    float v = Flat(car.Body.linearVelocity).magnitude;
                    if (inGlass) { through = true; if (vIn < 0f) vIn = v; vMin = Mathf.Min(vMin, v); }
                    if (inGlass && !shot && s > 12f) { shot = true; yield return Capture("tube_1_driving_through_the_glass"); }
                    yield return new WaitForFixedUpdate();
                }
                yield return drive;
                bool ok = run.reached && through && vMin > vIn * 0.8f && hookAt != null && hookAt.id == "P1J";
                if (!ok) bad.Add(Nm(id));
                report.AppendLine($"  {Nm(id),-16} {(through ? $"into the glass at {vIn:0.0} m/s, slowest in it {vMin:0.0}" : "NEVER IN THE GLASS")}, {(hookAt != null && hookAt.id == "P1J" ? $"off the outfall at {hookLaunch:0.0}" : "no outfall launch")}, {(run.reached ? $"back on the trail ({run.time:0.0} s)" : "DID NOT GET BACK " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})")}");
            }
            Check("P1 forward through the clear stretch: every trail vehicle through the glass at speed (keeps 80%), out over the pond, back on the trail", bad.Count == 0, bad.Count > 0 ? "not: " + string.Join(", ", bad) : "");
            Check("the fish are swimming round the clear stretch", aq.FishCount >= 40, $"{aq.FishCount} fish");
            // a reset in the middle of the glass
            {
                bool rec = DrainPipe.Recover(mid + Vector3.up * 0.5f, along, Vector3.zero, car.Body, out var pos, out float yaw);
                float floorY = mid.y - aq.radius;
                Check("a reset in the clear stretch: on the pipe's floor, facing along it", rec && Mathf.Abs(pos.y - floorY) < 0.6f && Mathf.Abs(Mathf.DeltaAngle(yaw, Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg)) < 10f,
                      rec ? $"put at y {pos.y:0.00} (the floor {floorY:0.00}), yaw {yaw:0}" : "no recovery spot");
            }
            // the frame rate with the camera in the glass (the vehicle parked in it), and the pictures
            {
                yield return Vehicle("atv");
                Place(mid - along * 6f - Vector3.up * (aq.radius - 0.3f), along, 0f); car.Body.linearVelocity = Vector3.zero; yield return new WaitForSecondsRealtime(1.5f);
                var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); if (chase) chase.enabled = false;
                cam.transform.position = mid - along * 9f; cam.transform.LookAt(mid + along * 6f + Vector3.up * 0.3f);
                var dts = new List<float>(); for (int i = 0; i < 240; i++) { yield return null; dts.Add(Time.unscaledDeltaTime); }
                dts.Sort(); float avg = 1f / dts.Average(), p99 = 1f / dts[(int)(dts.Count * 0.99f)];
                report.AppendLine($"  frame rate in the glass, 1280 x 720 (not measured at 3840 x 2160): {avg:0} fps average, {p99:0} fps at the 99th percentile");
                if (chase) { chase.enabled = true; chase.Snap(); }
                yield return Still("tube_2_looking_out_at_the_fish", mid + along * 3f - Vector3.up * 0.6f + Vector3.Cross(Vector3.up, along) * 1.2f, mid + along * 9f + Vector3.Cross(Vector3.up, along) * 5f + Vector3.up * 1.8f, 70f);
                yield return Still("tube_3_from_the_concrete_into_the_glass", aq.a0 - along * 4f + Vector3.up * 0.2f, aq.a0 + along * 10f + Vector3.up * 0.4f, 70f);
                yield return Still("tube_4_the_pool_from_above", mid + Vector3.Cross(Vector3.up, along) * 22f + Vector3.up * 12f, mid + Vector3.up * 3f, 60f);
            }
        }

        // a shortcut flown on each trail vehicle: its line flat out, the launch, the landing, back on the trail
        IEnumerator ShortcutFlat(string label, string scId, string jumpId, string landsOn, string[] ids, Func<Vector3, bool> landedRight)
        {
            UseRoute("noonday_creek.json", "reverse"); HookJumps();
            var j = BigJump.All.FirstOrDefault(b => b.id == jumpId); var rs = race.Route.Shortcuts.FirstOrDefault(s => s.Id == scId);
            if (j == null || rs == null) { Check($"{label} is built and in the reverse route", false); yield break; }
            var line = rs.Path.ToList(); var bad = new List<string>(); bool shot = false; var airs = new List<float>();
            foreach (var id in Small.Where(ids.Contains))
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                ClearHook(); var f = Flight(); var run = new Run(); Vector3 landAt = Vector3.zero; bool landed = false;
                var drive = StartCoroutine(DriveLine(line, 99f, 60f, run, null, null, 3f));
                while (!run.reached && run.end == "" && run.time < 59.9f)
                {
                    bool was = f.done;
                    f.Step(this, j, hookAt == j && hookWho == car, Time.deltaTime);
                    if (!was && f.done) { landAt = car.Body.position; landed = true; }
                    if (!shot && hookAt == j && car.GroundedWheels == 0 && Vector3.Dot(Flat(car.Body.position - j.lip), j.dir) > 10f) { shot = true; yield return Capture(jumpId.ToLower() + "_in_the_air"); }
                    yield return null;
                }
                yield return drive;
                bool right = landed && landedRight(landAt);
                bool ok = run.reached && hookAt == j && right && f.up > 0.7f;
                if (!ok) { bad.Add(Nm(id)); File.WriteAllLines(Path.Combine(outputDir, $"{jumpId.ToLower()}_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                if (hookAt == j) airs.Add(f.air);
                report.AppendLine($"  {Nm(id),-16} {(hookAt == j ? $"onto the lip at {hookLaunch,4:0.0} m/s (off at {Mathf.Clamp(hookLaunch, j.vmin, j.vmax):0.0}), {f.air:0.00} s in the air, down {f.dist,4:0.0} m past it at ({landAt.x:0.0}, {landAt.y:0.00}, {landAt.z:0.0}) {(right ? "on " + landsOn : "NOT ON " + landsOn.ToUpper())}{(f.wet ? ", in the water" : "")}, upright {f.up:0.00}" : "NEVER LEFT THE LIP")}, {(run.reached ? $"back on the trail ({run.time:0.0} s)" : "DID NOT GET BACK " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}");
            }
            Check($"{label}: every trail vehicle off the heap onto {landsOn}, on its wheels, back on the trail", bad.Count == 0, (bad.Count > 0 ? "not: " + string.Join(", ", bad) + "; " : "") + (airs.Count > 0 ? $"air {airs.Min():0.00} to {airs.Max():0.00} s" : ""));
            // slow: the Trail Four at 9 m/s (no lift under 10): off the lip short; never a trap
            {
                yield return Vehicle("atv");
                int from = line.FindIndex(q => Vector3.Dot(Flat(q - j.lip), j.dir) > -30f);
                var slow = line.Skip(Mathf.Max(0, from)).ToList();
                Place(slow[0], slow[2] - slow[0], 8f);
                var run = new Run(); ClearHook();
                yield return DriveLine(slow, 9f, 120f, run, null, null, 3f, false);
                Check($"{label} slow (the Trail Four held at 9 m/s): comes off the lip short and gets out and on, no reset", run.reached,
                      $"{(hookAt == j ? $"crossed the lip at {hookLaunch:0.0} m/s" : "no launch")}, {(run.reached ? $"out and on in {run.time:0} s" : "STOPPED " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})")}");
            }
            // pictures: the turn-off from the trail, and from behind the lip
            {
                var fwd = Flat(line[3] - line[0]).normalized;
                car.Body.position = j.lip + Vector3.Cross(Vector3.up, j.dir) * 40f + Vector3.up * 3f; car.Body.linearVelocity = Vector3.zero;
                yield return Still(jumpId.ToLower() + "_turn_off", line[0] - fwd * 6f + Vector3.up * 2.2f, j.lip + Vector3.up * 0.5f);
                yield return Still(jumpId.ToLower() + "_from_the_lip", j.lip - j.dir * 10f + Vector3.up * 2.5f, j.lip + j.dir * 30f);
            }
            // the AI: each trail vehicle once, made to take it
            Time.timeScale = 4f; Time.maximumDeltaTime = 1f; int prev = RaceController.AiCount; var aiBad = new List<string>();
            foreach (var id in Small.Where(ids.Contains))
            {
                var r = new AiResult(); yield return AiThrough(scId, jumpId, id, null, r);
                bool ok = r.back && r.given && r.recoveries == 0 && r.launch > 0f;
                if (!ok) aiBad.Add(Nm(id));
                report.AppendLine($"  AI {Nm(id),-15} {(ok ? "OK  " : "FAIL")} {(r.launch > 0f ? $"onto the lip at {r.launch:0.0} m/s, {r.f.air:0.00} s in the air, down {r.f.dist:0.0} m past it{(r.f.wet ? " (in the water)" : "")}" : "never left the lip")}, CP forgiven {(r.given ? "yes" : "no")}, recoveries {r.recoveries}{(r.back ? ", back on the trail" : ", ended at " + r.where)}");
            }
            RaceController.AiCount = prev; RaceController.AiVehiclesOverride = null; Time.timeScale = 1f; Time.maximumDeltaTime = 0.3333333f;
            Check($"{label}: the AI takes it (each trail vehicle once): launched, the checkpoint forgiven, no recovery, back on the trail", aiBad.Count == 0, aiBad.Count > 0 ? "not: " + string.Join(", ", aiBad) : "");
        }

        IEnumerator Ra(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== Follow-ups 1: RA \"Pipe dive\" (reverse, official, forgives reverse CP4): over the small pond onto P1's apron and through the pipe");
            var pipe = FindObjectsByType<DrainPipe>(FindObjectsSortMode.None).FirstOrDefault(p => p.id == "P1");
            Vector3 mouth = pipe != null ? pipe.floor[pipe.floor.Length - 1] : Vector3.zero;
            // on the apron: within 9 m of the pipe's outlet mouth, along its axis (the apron is 7 m from the mouth)
            yield return ShortcutFlat("RA", "ra_pipe_dive", "RA", "P1's apron", ids, p => pipe != null && Flat(p - mouth).magnitude < 9.5f);
        }

        IEnumerator Rc(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== Follow-ups 2: RC \"Creek leap back\" (reverse, official, forgives reverse CP2): over the gorge onto N2's heap");
            var n2 = BigJump.All.FirstOrDefault(b => b.id == "N2");
            // on N2's heap or just past it (its kicker runs 8 m back from its lip)
            yield return ShortcutFlat("RC", "rc_creek_leap_back", "RC", "N2's heap", ids, p => n2 != null && Vector3.Dot(Flat(p - n2.lip), -n2.dir) > -0.5f && Vector3.Dot(Flat(p - n2.lip), -n2.dir) < 14f);
        }
    }
}
