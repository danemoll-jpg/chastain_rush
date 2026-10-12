using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    public sealed partial class Round20Runner
    {
        // ---------------------------------------------------------------- 1. BUG-002, the outfall ramp over the pond

        IEnumerator Ramp(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== 1. BUG-002: out of P1 up the outfall apron and over the pond, each vehicle flat out along the shortcut's line");
            UseRoute("noonday_creek.json", "forward");
            var j = BigJump.All.FirstOrDefault(b => b.id == "P1J");
            if (j == null) { Check("the outfall ramp is there", false); yield break; }
            var sc = race.Route.Shortcuts.First(s => s.Id == "p1_pipe");
            var line = sc.Path.ToList();
            float launch = -1f; BigJump at = null; ArcadeVehicle who = null;
            System.Action<BigJump, ArcadeVehicle, float> hook = (b, c, v) => { if (b == j) { launch = v; at = b; who = c; } };
            BigJump.Launched += hook;
            var bad = new List<string>(); bool shot = false; var airs = new List<float>();
            foreach (var id in ids)
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                launch = -1f; at = null;
                var f = Flight(); var run = new Run();
                var drive = StartCoroutine(DriveLine(line, 99f, 50f, run, null, null, 3f));
                while (!run.reached && run.end == "" && run.time < 49.9f)
                {
                    f.Step(this, j, at == j && who == car, Time.deltaTime);
                    if (!shot && at == j && car.GroundedWheels == 0 && Vector3.Dot(Flat(car.Body.position - j.lip), j.dir) > 14f) { shot = true; yield return Capture("1_P1J_over_the_pond"); }
                    yield return null;
                }
                yield return drive;
                bool ok = run.reached && at == j && f.air >= 1.5f && !f.wet && f.up > 0.7f;
                if (!ok) { bad.Add(Nm(id)); File.WriteAllLines(Path.Combine(outputDir, $"ramp_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                if (at == j) airs.Add(f.air);
                report.AppendLine($"  {Nm(id),-18} {(at == j ? $"onto the lip at {launch,4:0.0} m/s (off it at {Mathf.Clamp(launch, j.vmin, j.vmax):0.0}), {f.air:0.00} s in the air, {f.top:0.0} m over the lip at the top, down {f.dist,4:0.0} m past it {(f.wet ? "IN THE WATER" : "on dry ground")}, upright {f.up:0.00}" : "NEVER LEFT THE LIP")}, {(run.reached ? "on to the trail" : "DID NOT GET BACK " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}");
            }
            Check("BUG-002: every vehicle out of P1 clears the pond on dry ground, on its wheels, and drives on to the trail", bad.Count == 0, (bad.Count > 0 ? "not: " + string.Join(", ", bad) + "; " : "") + (airs.Count > 0 ? $"air {airs.Min():0.00} to {airs.Max():0.00} s" : ""));
            // slow: the Trail Four held at 8 m/s rolls off into the shallow edge and drives out to the trail
            {
                yield return Vehicle("atv");
                var from = line.FindIndex(q => Vector3.Dot(Flat(q - j.lip), j.dir) > -40f);
                var slow = line.Skip(Mathf.Max(0, from)).ToList();
                Place(slow[0], slow[2] - slow[0], 6f);
                var run = new Run(); launch = -1f; at = null;
                yield return DriveLine(slow, 8f, 70f, run, null, null, 3f);
                Check("BUG-002 slow (the Trail Four held at 8 m/s): rolls off the lip into the shallow edge and drives out on to the trail, no reset", run.reached, $"{(at == j ? $"crossed the lip at {launch:0.0} m/s (not lifted)" : "no launch")}, {(run.reached ? $"out and on in {run.time:0} s" : "STOPPED " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})")}");
            }
            // a picture from the pipe's mouth
            {
                var back = j.lip - j.dir * 14f; back.y = j.lip.y + 1.2f;
                car.Body.position = j.lip + Vector3.Cross(Vector3.up, j.dir) * 30f + Vector3.up * 2f; car.Body.linearVelocity = Vector3.zero;
                yield return Still("1_P1J_from_the_pipe_mouth", back, j.lip + j.dir * 40f + Vector3.down * 2f);
            }
            // the AI takes it: each trail vehicle once through P1 as an AI driver made to take it
            Time.timeScale = 4f; Time.maximumDeltaTime = 1f;
            int prevCount = RaceController.AiCount; var aiBad = new List<string>();
            foreach (var id in Small.Where(ids.Contains))
            {
                RaceController.AiCount = 1; RaceController.AiVehiclesOverride = new[] { id };
                race.StartRace();
                while (race.State != RaceController.Stage.Racing) yield return null;
                var e = race.Entrants.First(x => !x.IsPlayer); var d = e.Driver;
                var ais = race.AiShortcuts.First(x => x.Id == "p1_pipe"); var rs = race.Route.Shortcuts.First(x => x.Id == "p1_pipe");
                var p = race.AiRoad.At(ais.entryRoad - 120f, out var fw); fw = Vector3.ProjectOnPlane(fw, Vector3.up).normalized;
                d.RunIn = null; d.ForcedShortcut = ais;
                var rot = Quaternion.LookRotation(fw);
                e.Car.Body.isKinematic = false; e.Car.Body.position = p + Vector3.up * 0.6f; e.Car.Body.rotation = rot; e.Car.transform.SetPositionAndRotation(e.Car.Body.position, rot);
                e.Car.Body.linearVelocity = fw * 18f; e.Car.Body.angularVelocity = Vector3.zero; e.Car.ClearSteering(); Physics.SyncTransforms();
                e.ClockStarted = true; e.StartAt = e.LapStart = race.RaceClock; e.NextCheckpoint = rs.FirstForgiven; e.Finished = false; e.ActiveShortcut = null;
                e.NotifyTeleported();
                int rec0 = d.RecoveryCount, taken0 = e.ShortcutsTaken.Count; float pen0 = e.PenaltyTotal;
                launch = -1f; at = null; who = null;
                var fl = Flight(); float t = 0f; bool back = false;
                while (t < 60f)
                {
                    yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                    fl.Step(this, j, at == j && who == e.Car, Time.fixedDeltaTime, e.Car);
                    float s = race.AiRoad.Project(e.Car.Body.position, out float lat);
                    float past = Mathf.Repeat(s - ais.exitRoad, race.AiRoad.Length);
                    if (e.ShortcutsTaken.Count > taken0 && d.OnShortcut == null && Mathf.Abs(lat) < 6f && past > 20f && past < 200f) { back = true; break; }
                }
                bool given = e.ShortcutsTaken.Count > taken0 && e.PenaltyTotal == pen0;
                bool ok = back && given && d.RecoveryCount == rec0 && at == j && !fl.wet;
                if (!ok) aiBad.Add(Nm(id));
                report.AppendLine($"  AI {Nm(id),-15} {(ok ? "OK  " : "FAIL")} {(at == j ? $"onto the lip at {launch:0.0} m/s, {fl.air:0.00} s in the air, down {fl.dist:0.0} m past it {(fl.wet ? "IN THE WATER" : "on dry ground")}" : "never left the lip")}, shortcut given {(given ? "yes" : "no")}, recoveries {d.RecoveryCount - rec0}{(back ? ", back on the lap" : $", ended at ({e.Car.Body.position.x:0},{e.Car.Body.position.z:0})")}");
                d.ForcedShortcut = null; race.EndRace(); yield return null;
            }
            RaceController.AiCount = prevCount; RaceController.AiVehiclesOverride = null;
            Time.timeScale = 1f; Time.maximumDeltaTime = 0.3333333f;
            BigJump.Launched -= hook;
            Check("BUG-002: the AI takes it (each trail vehicle once, as an AI driver): over the pond, the shortcut given, no recovery, back on the lap", aiBad.Count == 0, aiBad.Count > 0 ? "not: " + string.Join(", ", aiBad) : "");
        }

        // one flight off a jump, from its lip to two wheels down: air, top over the lip, distance past it, landed in water, upright
        sealed class FlightRec
        {
            public float air, top = -99f, dist = -1f, up = -1f; public bool wet, done;
            public void Step(Round20Runner r, BigJump j, bool launched, float dt, ArcadeVehicle c = null)
            {
                c = c ? c : r.car;
                if (!launched || done) return;
                top = Mathf.Max(top, c.Body.position.y - j.lip.y); air += dt;
                if (c.GroundedWheels >= 2 && air > 0.3f)
                {
                    done = true; dist = Vector3.Dot(Flat(c.Body.position - j.lip), j.dir); up = c.transform.up.y;
                    wet = ShallowWater.Sample(c, out _) > 0f;
                }
            }
        }
        static FlightRec Flight() => new FlightRec();
    }
}
