using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    public sealed partial class Round18Runner
    {
        // ---------------------------------------------------------------- J4 over New Chastain Road

        IEnumerator J4(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== J4, the road jump: each vehicle down the trail from J3's landing (160 m before J4's lip) flat out, lifting only for bends");
            UseRoute("noonday_creek.json", "forward");
            var x = Extra(); var lip = RaceRoute.Ground(x.j4.lip[0], x.j4.lip[1]); var dir = new Vector3(x.j4.dir[0], 0f, x.j4.dir[1]);
            var line = LapSlice(x.j4.s_lip - 160f, x.j4.s_lip + 95f);
            var cleared = new List<string>(); var shortOnes = new List<string>(); var stuck = new List<string>();
            bool shot = false;
            foreach (var id in ids)
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 12f);
                yield return new WaitForFixedUpdate();
                var run = new Run();
                var drive = StartCoroutine(DriveLine(line, 99f, 40f, run, lip, null, 4f));
                while (!run.reached && run.end == "" && run.time < 39.9f)
                {
                    if (!shot && car.GroundedWheels == 0 && Vector3.Dot(Flat(car.Body.position - lip), dir) > 12f) { shot = true; yield return Capture("J4_over_the_road_" + id); }
                    yield return null;
                }
                yield return drive;
                float land = float.IsNaN(run.firstLanding.x) ? -1f : Vector3.Dot(Flat(run.firstLanding - lip), dir);
                bool over = land > x.j4.road_far_edge_m;
                (over ? cleared : shortOnes).Add(VehicleProfile.Find(id).Name);
                if (!run.reached) stuck.Add(VehicleProfile.Find(id).Name);
                report.AppendLine($"  {VehicleProfile.Find(id).Name,-18} at the lip {run.speedAt,5:0.0} m/s, first touchdown {land,5:0.0} m past it ({(over ? "past the road" : land >= 0f ? "on the road" : "?")}), longest in the air {run.maxAir:0.00} s, {(run.reached ? "drove on down the south loop" : "DID NOT GET THROUGH " + run.end)} ({run.time:0.0} s)");
            }
            Check("every vehicle gets over or onto the road and on down to the south loop", stuck.Count == 0, stuck.Count > 0 ? "not: " + string.Join(", ", stuck) : $"cleared the road: {string.Join(", ", cleared)}; landed on it: {(shortOnes.Count > 0 ? string.Join(", ", shortOnes) : "none")}");
            // short of the speed: 14 m/s at the lip, onto the road, then on down the landing slope
            yield return Vehicle("atv");
            var slow = LapSlice(x.j4.s_lip - 50f, x.j4.s_lip + 95f);
            Place(slow[0], slow[2] - slow[0], 10f);
            var r2 = new Run();
            yield return DriveLine(slow, 14f, 30f, r2, lip, null, 4f);
            float l2 = float.IsNaN(r2.firstLanding.x) ? -1f : Vector3.Dot(Flat(r2.firstLanding - lip), dir);
            Check("short of the speed (Trail Four held at 14 m/s): lands on the road, drives on down the slope, no wall, no reset", r2.reached && l2 > 0f && l2 < x.j4.road_far_edge_m, $"{r2.speedAt:0.0} m/s at the lip, touchdown {l2:0.0} m past it, {(r2.reached ? "on down to the south loop" : "STOPPED " + r2.end)}");
            yield return Capture("J4_after_the_slow_run");
        }

        // ---------------------------------------------------------------- P1, the pipe

        IEnumerator Pipe(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== P1, the drain pipe: each vehicle along its line from the trail (16 m/s), through the inlet cut, the pipe and the outlet");
            UseRoute("noonday_creek.json", "forward");
            var pipe = FindAnyObjectByType<DrainPipe>();
            Check("the pipe is in the scene", pipe != null && pipe.floor != null && pipe.floor.Length > 10);
            if (pipe == null) yield break;
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "p1_pipe");
            Check("P1 is an official shortcut of Noonday Creek", sc != null);
            if (sc == null) yield break;
            var line = sc.Path.ToList();
            // the holes: from above the cuts' middles a ray meets the pipe's own floor, not the terrain; over the pipe, the terrain
            var f = pipe.floor; int n = f.Length;
            foreach (var (name, k) in new[] { ("inlet cut", n / 8), ("outlet cut", n - n / 8) })
            {
                var q = f[k]; bool hit = Physics.Raycast(q + Vector3.up * 30f, Vector3.down, out var h, 60f, ~0, QueryTriggerInteraction.Ignore);
                Check($"the terrain is open over the {name}", hit && !(h.collider is TerrainCollider) && Mathf.Abs(h.point.y - q.y) < 0.4f, hit ? $"{h.collider.name} at {h.point.y:0.00} (floor {q.y:0.00})" : "nothing");
            }
            { var q = f[n / 2]; bool hit = Physics.Raycast(q + Vector3.up * 30f, Vector3.down, out var h, 60f, ~0, QueryTriggerInteraction.Ignore);
              Check("over the middle of the pipe the ground is whole", hit && h.collider is TerrainCollider && h.point.y > q.y + 4.4f, hit ? $"{h.collider.name} at {h.point.y:0.00}, the floor {q.y:0.00} below" : "nothing"); }
            var stuck = new List<string>(); float camOut = 0f; string camWorst = "";
            bool shots = false;
            foreach (var id in ids)
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 12f);
                yield return new WaitForFixedUpdate();
                var run = new Run(); float inPipeMin = 99f;
                var drive = StartCoroutine(DriveLine(line, 16f, 40f, run, null, null, 3f));
                int stage = 0;
                while (!run.reached && run.end == "" && run.time < 39.9f)
                {
                    var p = car.Body.position;
                    if (DrainPipe.Inside(p, out float st) != null && st > n * 0.35f && st < n * 0.65f)
                    {
                        inPipeMin = Mathf.Min(inPipeMin, Flat(car.Body.linearVelocity).magnitude);
                        var cam = Camera.main ? Camera.main.transform.position : p;
                        bool inside = DrainPipe.Inside(cam, out _) != null;
                        if (!inside) { camOut += Time.deltaTime; camWorst = $"{VehicleProfile.Find(id).Name}: camera at ({cam.x:0.0}, {cam.y:0.0}, {cam.z:0.0})"; }
                        if (!shots && stage == 1) { stage = 2; yield return Capture("P1_inside_the_pipe"); }
                    }
                    if (!shots && stage == 0 && DrainPipe.Inside(p, out float s0) != null && s0 < n * 0.2f) { stage = 1; yield return Capture("P1_down_the_inlet"); }
                    if (!shots && stage == 2 && DrainPipe.Inside(p, out float s2) != null && s2 > n * 0.85f) { stage = 3; yield return Capture("P1_up_the_outlet"); }
                    yield return null;
                }
                yield return drive;
                if (stage >= 3) shots = true;
                if (!run.reached) { stuck.Add(VehicleProfile.Find(id).Name); System.IO.File.WriteAllLines(System.IO.Path.Combine(outputDir, "p1_" + id + ".csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                report.AppendLine($"  {VehicleProfile.Find(id).Name,-18} {(run.reached ? "through" : "DID NOT GET THROUGH " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})")} in {run.time:0.0} s, slowest in the pipe {inPipeMin:0.0} m/s, farthest off the line {run.maxDev:0.0} m");
            }
            Check("every vehicle drives through P1, inlet to outlet, without stopping", stuck.Count == 0, stuck.Count > 0 ? "not: " + string.Join(", ", stuck) : $"{ids.Length} of {ids.Length}");
            Check("the camera stays in the pipe (middle third)", camOut < 0.05f, camOut > 0f ? $"{camOut:0.00} s outside; {camWorst}" : "always inside");
            // a reset inside: the vehicle stopped in the pipe, facing back the way it came
            yield return Vehicle("original");
            var mid = f[n / 2]; var back = (f[n / 2 - 2] - f[n / 2 + 2]);
            Place(mid, back, 0f);
            yield return new WaitForSecondsRealtime(1.5f);
            bool found = CarRecovery.Find(respawn, out var rp, out var ry);
            var along = Flat(f[n / 2 + 2] - f[n / 2 - 2]).normalized; var face = Quaternion.Euler(0f, ry, 0f) * Vector3.forward;
            float ang = Vector3.Angle(along, face); ang = Mathf.Min(ang, 180f - ang);
            Check("a reset in the pipe puts the vehicle on the pipe's floor, along it (the way it was facing)", found && DrainPipe.Inside(rp, out _) != null && Mathf.Abs(rp.y - mid.y) < 0.6f && ang < 10f && Vector3.Dot(face, back) > 0f,
                  found ? $"at ({rp.x:0.0}, {rp.y:0.00}, {rp.z:0.0}) (floor {mid.y:0.00}), facing {ang:0} deg off the pipe's axis, {(Vector3.Dot(face, back) > 0f ? "the way it was facing" : "turned round")}" : "no spot found");
            if (found) { respawn.PlaceAt(rp, ry); yield return new WaitForSecondsRealtime(1.5f); yield return Capture("P1_after_a_reset"); }
        }

        // ---------------------------------------------------------------- R3, the slide and the trampoline

        static bool landed0(int l0) => Trampoline.Landings > l0;

        IEnumerator R3(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== R3, the trampoline hop (the Chastain Glen loop in reverse): each vehicle along its line at up to 18 m/s (11 through the bend before the slide, the line's profile), up the slide, onto the trampoline, over 1752");
            UseRoute("chastain_glen_loop.json", "reverse");
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "r3_trampoline");
            var tr = FindAnyObjectByType<Trampoline>();
            Check("R3 is an official shortcut of the reverse loop, forgiving the reverse lap's CP4; the trampoline is there", sc != null && tr != null && race.Route.Checkpoints[sc.FirstForgiven].DataId == 2, sc != null ? $"forgives reverse CP{sc.FirstForgiven + 1}" : "missing");
            if (sc == null || tr == null) yield break;
            var line = sc.Path.ToList();
            var bad = new List<string>(); bool shots = false;
            foreach (var id in ids)
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 12f);
                yield return new WaitForFixedUpdate();
                int b0 = Trampoline.Bounces, l0 = Trampoline.Landings;
                var run = new Run(); var drive = StartCoroutine(DriveLine(line, 18f, 30f, run, null, null, 3f, true, sc.AiProfile));
                Vector3 landAt = default; float landYawErr = -1f, landUp = 0f, maxY = -999f; int stage = 0; float after = -1f; int wheelsAfter = 0; float speedAfter = 0f;
                while (!run.reached && run.end == "" && run.time < 29.9f)
                {
                    maxY = Mathf.Max(maxY, car.Body.position.y);
                    if (!shots && stage == 0 && Vector3.Distance(Flat(car.Body.position), new Vector3(tr.transform.position.x, 0f, tr.transform.position.z)) < 22f) { stage = 1; yield return Capture("R3_up_the_slide"); }
                    if (!shots && stage == 1 && Trampoline.Bounces > b0 && car.Body.position.y > tr.matY + 12f) { stage = 2; yield return Capture("R3_over_the_house"); }
                    if (Trampoline.Landings > l0 && landYawErr < 0f)
                    {
                        landAt = car.Body.position; landYawErr = Mathf.Abs(Mathf.DeltaAngle(car.transform.eulerAngles.y, tr.landYaw)); landUp = car.transform.up.y; after = run.time;
                        if (!shots && stage == 2) { stage = 3; yield return Capture("R3_down_on_the_lane"); }
                    }
                    if (after > 0f && run.time > after + 1f && wheelsAfter == 0) { wheelsAfter = car.GroundedWheels; speedAfter = Flat(car.Body.linearVelocity).magnitude; }
                    yield return null;
                }
                yield return drive;
                if (landed0(l0) && wheelsAfter == 0)   // the line ended within the second after the landing: on down the lane for the rest of it
                {
                    car.enabled = false;
                    for (float t = run.time; t < after + 1f; t += Time.fixedDeltaTime) { yield return new WaitForFixedUpdate(); car.Simulate(1f, 0f, 0f, Time.fixedDeltaTime); }
                    wheelsAfter = car.GroundedWheels; speedAfter = Flat(car.Body.linearVelocity).magnitude; car.enabled = true;
                }
                if (stage >= 3) shots = true;
                bool bounced = Trampoline.Bounces > b0, landed = Trampoline.Landings > l0;
                float miss = landed ? Vector3.Distance(Flat(landAt), Flat(tr.target)) : -1f;
                bool ok = bounced && landed && miss < 5f && landYawErr < 20f && landUp > 0.9f && run.reached && wheelsAfter >= 2 && speedAfter > 5f;
                if (!ok) bad.Add(VehicleProfile.Find(id).Name);
                if (!ok) System.IO.File.WriteAllLines(System.IO.Path.Combine(outputDir, "r3_" + id + ".csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}"));
                if (!ok && !run.reached) report.AppendLine($"    stopped at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}); its track: r3_{id}.csv");
                report.AppendLine($"  {VehicleProfile.Find(id).Name,-18} {(bounced ? "bounced" : "NO BOUNCE")}, highest {maxY - tr.matY:0.0} m over the mat, {(landed ? $"landed {miss:0.0} m from the aim, facing {landYawErr:0} deg off down the lane, upright {landUp:0.00}" : "NO LANDING")}; 1 s later {wheelsAfter} wheels down at {speedAfter:0.0} m/s; {(run.reached ? "on down the lane" : "DID NOT GET ON " + run.end)}");
            }
            Check("every vehicle bounces over 1752 and comes down on its wheels on the lane, pointing down it, and drives on", bad.Count == 0, bad.Count > 0 ? "not: " + string.Join(", ", bad) : "all");
            // slow: up the slide at 5 m/s, off the deck short of the mat; no throw; not stuck (it drives away)
            yield return Vehicle("original");
            Place(line[0], line[2] - line[0], 5f);
            int bb = Trampoline.Bounces;
            var slow = new Run(); var lineToTramp = line.TakeWhile(p => Vector3.Distance(Flat(p), Flat(tr.transform.position)) > 3f).ToList();
            yield return DriveLine(lineToTramp, 5f, 40f, slow, null, null, 3f);
            var p0 = car.Body.position;
            car.enabled = false;
            for (float t = 0f; t < 6f; t += Time.fixedDeltaTime) { yield return new WaitForFixedUpdate(); car.Simulate(t < 3f ? 0f : 1f, t < 3f ? 1f : 0f, 0.6f, Time.fixedDeltaTime); }
            car.enabled = true;
            float moved = Vector3.Distance(Flat(car.Body.position), Flat(p0));
            Check("slow (5 m/s): no throw over the house, and the vehicle can drive away", Trampoline.Bounces == bb && moved > 4f, $"ended at ({p0.x:0.0}, {p0.z:0.0}), then moved {moved:0.0} m");
            yield return Capture("R3_after_the_slow_run");
        }
    }
}
