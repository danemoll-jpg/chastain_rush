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
    // Development check only (added by Editor/RaceCheck.cs with -raceCheck round6, never saved into the scene). Round 6, item 8.
    // The car is moved by hand from one side of a gate to the other (RaceController.NotifyTeleported says it was moved, as a
    // reset does), so the REAL RaceController.FixedUpdate does the crossing test, the rules and the bookkeeping. Then one
    // physical test with the real car: a barrier across a closed road must stop it, and must be gone in free driving.
    //   A  gates      every gate crossed at the left edge, the right edge, and at 45 degrees from each edge; just outside does not count
    //   B  penalty    skipped checkpoints cost 5 s each, the laps still count, the total carries the penalties
    //   C  shortcut   a TEST shortcut (Chasefield Way, defined here, not in the route file) forgives its checkpoints; backwards does not
    //   D  barriers   listed, clear of the course, skipped at the test shortcut's ends, and the car is stopped by one, then not in free driving
    public sealed class Round6CheckRunner : MonoBehaviour
    {
        public string outputDir, recordsPath;
        public RouteShortcutJson testShortcut;

        RaceController race;
        ArcadeVehicle car;
        VehicleRespawn respawn;
        readonly List<string> events = new List<string>();
        readonly StringBuilder report = new StringBuilder();
        readonly List<string> failures = new List<string>();
        readonly Queue<string> shots = new Queue<string>();
        int checks;
        bool capturing, finished;

        void Awake() { RaceController.RecordsPathOverride = recordsPath; }

        void Start()
        {
            car = FindAnyObjectByType<ArcadeVehicle>();
            race = RaceController.Instance;
            respawn = car.GetComponent<VehicleRespawn>();
            car.enabled = false; // this runner calls Simulate itself where it needs the car to drive
            Time.timeScale = 4f;
            race.Logged += s => events.Add($"{Time.time,7:0.0}  {s}");
            StartCoroutine(Main());
        }

        void Check(string name, bool ok, string detail = "")
        {
            checks++;
            if (!ok) failures.Add(name);
            report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}");
        }
        void Note(string s) => report.AppendLine(s);

        // ---------------------------------------------------------------- moving the car by hand

        void Put(Vector3 p, Vector3 forward, bool notify)
        {
            var rot = Quaternion.LookRotation(new Vector3(forward.x, 0f, forward.z), Vector3.up);
            var b = car.Body;
            b.useGravity = false; b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero;
            b.position = p; b.rotation = rot;
            car.transform.SetPositionAndRotation(p, rot);
            Physics.SyncTransforms();
            if (notify) race.NotifyTeleported();
        }

        // One crossing of a gate's plane at a lateral offset and a heading (degrees away from the gate's own direction).
        IEnumerator Cross(RaceGate g, float lateral, float headingDeg, bool backwards = false)
        {
            Vector3 d = Quaternion.AngleAxis(headingDeg, Vector3.up) * g.Forward;
            if (backwards) d = -d;
            Vector3 hit = g.Position + g.Right * lateral + Vector3.up * 0.5f;
            Put(hit - d * 3f, d, true);
            yield return new WaitForFixedUpdate();
            Put(hit + d * 3f, d, false);
            yield return new WaitForFixedUpdate();
        }

        IEnumerator WaitRacing()
        {
            float t0 = Time.unscaledTime;
            while (race.State == RaceController.Stage.Countdown && Time.unscaledTime - t0 < 20f) yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        IEnumerator Idle(int steps) { for (int i = 0; i < steps; i++) yield return new WaitForFixedUpdate(); }

        // ---------------------------------------------------------------- the checks

        IEnumerator Main()
        {
            yield return null; yield return null;
            Note($"Route {race.Route.Name}, direction {race.Route.Direction}, lap {race.Route.LapLength:0.0} m");
            yield return RunGates();
            yield return RunPenalty();
            yield return RunShortcut();
            yield return RunBarriers();
            Finish();
        }

        IEnumerator RunGates()
        {
            Note(""); Note("A. Gates crossed at the edges and at 45 degrees (4 laps; every crossing must count, no penalty)");
            RaceController.LapsOverride = 4;
            race.StartRace();
            yield return WaitRacing();
            var r = race.Route;
            Note($"   finish half width {r.Gate.HalfWidth:0.0} m, checkpoints " + string.Join(", ", r.Checkpoints.Select(c => $"CP{c.Id} {c.HalfWidth:0.0} m")) + $"; window {-r.Gate.Below:0} to +{r.Gate.Above:0} m of the road surface");
            var gates = new List<RaceGate>(); gates.AddRange(r.Checkpoints); gates.Add(r.Gate);
            // lap 1: left edge, straight; lap 2: right edge, straight; lap 3: left edge at 45 deg; lap 4: right edge at 45 deg
            float[] side = { -1, 1, -1, 1 }; float[] head = { 0, 0, 45, -45 };
            string[] name = { "left edge, straight", "right edge, straight", "left edge, 45 deg", "right edge, 45 deg" };
            // the start line first, at the edge of lap 1
            yield return Cross(r.Gate, side[0] * r.Gate.HalfWidth, head[0]);
            Check("start line at the left edge starts the clock", race.ClockStarted, $"lateral {side[0] * r.Gate.HalfWidth:0.0} m");
            for (int lap = 0; lap < 4; lap++)
            {
                int ev0 = events.Count; int lapsBefore = race.CompletedLaps;
                for (int k = 0; k < gates.Count; k++)
                {
                    var g = gates[k];
                    bool lastLap = lap == 3;
                    int before = events.Count;
                    // the finish line of lap n is also where lap n+1 starts: the next lap's offsets apply to the other gates
                    float lat = side[lap] * g.HalfWidth;
                    yield return Cross(g, lat, head[lap]);
                    bool isFinish = g.Id == 0;
                    bool registered = isFinish ? race.CompletedLaps == lapsBefore + 1 : events.Skip(before).Any(e => e.Contains($"CHECKPOINT {g.Id} at"));
                    Check($"lap {lap + 1} ({name[lap]}): {(isFinish ? "finish line" : "CP" + g.Id)} counts at {lat:+0.0;-0.0} m", registered, $"gate half width {g.HalfWidth:0.0} m");
                    if (isFinish && lastLap) break;
                }
                var evs = events.Skip(ev0).ToList();
                Check($"lap {lap + 1} ({name[lap]}): counted with no penalty", race.CompletedLaps == lapsBefore + 1 && race.PenaltyTotal == 0f && !evs.Any(e => e.Contains("PENALTY")), $"completed {race.CompletedLaps}, penalty {race.PenaltyTotal:0}");
            }
            Check("race finished after 4 laps, total carries no penalty", race.State == RaceController.Stage.Finished && race.PenaltyTotal == 0f, race.State.ToString());
            yield return Idle(2);
            shots.Enqueue("a_gates_finished");

            // just outside the posts, past the car's own width, must NOT count
            Note("   negative control: a crossing outside the posts");
            RaceController.LapsOverride = 1;
            race.StartRace(); yield return WaitRacing();
            r = race.Route;
            float outside = r.Gate.HalfWidth + 3.5f;   // centre 3.5 m outside the post: the body (half width about 1) is clear of it
            yield return Cross(r.Gate, outside, 0);
            Check("start line crossed 3.5 m outside the post does NOT start the clock", !race.ClockStarted, $"lateral {outside:0.0} m");
            yield return Cross(r.Gate, -outside, 45);
            Check("... nor at 45 degrees on the other side", !race.ClockStarted);
            yield return Cross(r.Gate, 0, 0, backwards: true);
            Check("crossing the start line backwards does NOT start the clock", !race.ClockStarted);
            yield return Cross(r.Gate, 0, 0);
            Check("... but crossing it properly does", race.ClockStarted);
            // touching the post counts: the centre is outside the post line but the body overlaps it
            RaceController.LapsOverride = 1;
            race.EndRace(); yield return Idle(2);
        }

        IEnumerator RunPenalty()
        {
            Note(""); Note("B. Missed checkpoints: 5 s each, the lap still counts, penalties on the total (3 laps)");
            RaceController.LapsOverride = 3;
            race.StartRace(); yield return WaitRacing();
            var r = race.Route; var cps = r.Checkpoints;
            yield return Cross(r.Gate, 0, 0);
            // lap 1: skip CP2 only
            yield return Cross(cps[0], 0, 0);
            yield return Cross(cps[2], 0, 0);
            Check("CP2 skipped: +5 s given when CP3 is crossed", race.PenaltyTotal == 5f && race.MissedTotal == 1, $"penalty {race.PenaltyTotal:0} s");
            Check("... and the HUD message says so", race.Message.Contains("Missed checkpoint 2") && race.Message.Contains("+5 s"), race.Message);
            yield return Cross(cps[3], 0, 0); yield return Cross(cps[4], 0, 0);
            shots.Enqueue("b_penalty_shown"); yield return Idle(3);
            float clockAtFinish1 = race.Clock;
            yield return Cross(r.Gate, 0, 0);
            Check("lap 1 counts despite the missed checkpoint", race.CompletedLaps == 1 && race.LapTimes.Count == 1);
            float lap1 = race.LapTimes[0];
            Check("lap 1 time = clock + 5 s penalty", Mathf.Abs(lap1 - (clockAtFinish1 + 0.04f + 5f)) < 0.25f, $"lap {lap1:0.000}, clock at the line about {clockAtFinish1:0.000}");
            Check("no 'not counted' anywhere", !events.Any(e => e.Contains("NOT COUNTED")));
            // lap 2: skip CP2 and CP3 (cross CP1 then CP4): +10
            yield return Cross(cps[0], 0, 0);
            yield return Cross(cps[3], 0, 0);
            Check("CP2 and CP3 skipped: +10 s more", race.PenaltyTotal == 15f && race.LapPenalty == 10f, $"race penalty {race.PenaltyTotal:0} s, this lap {race.LapPenalty:0} s");
            yield return Cross(cps[4], 0, 0); yield return Cross(r.Gate, 0, 0);
            Check("lap 2 counts, 2 laps done", race.CompletedLaps == 2);
            Check("lap 2 time includes its +10 s", race.LapTimes[1] >= 10f && race.LapTimes[1] < 10.6f, $"{race.LapTimes[1]:0.000}");
            // lap 3: cross nothing but the line: all five missed (+25), and the finish ends the race
            float clockAtFinish3 = race.Clock;
            yield return Cross(r.Gate, 0, 0);
            Check("lap 3: all five checkpoints skipped = +25 s, lap counts", race.CompletedLaps == 3 && race.MissedTotal == 8 && race.PenaltyTotal == 40f, $"{race.MissedTotal} missed, penalty {race.PenaltyTotal:0} s");
            Check("race finished", race.State == RaceController.Stage.Finished);
            Check("total = clock + 40 s of penalties", Mathf.Abs(race.FinalTotal - (clockAtFinish3 + 0.04f + 40f)) < 0.3f, $"total {RaceController.Fmt(race.FinalTotal)}, clock {clockAtFinish3:0.000} + 40");
            Check("the penalty log has every miss", race.PenaltyLog.Count == 8, race.PenaltyLog.Count + " entries");
            Check("the finish panel lap lines name the penalties", race.LapLog.Count == 3 && race.LapLog[0].Contains("+5 s") && race.LapLog[1].Contains("+10 s") && race.LapLog[2].Contains("+25 s"), string.Join(" | ", race.LapLog));
            Check("best lap includes its penalty", Mathf.Abs(race.BestLap - Mathf.Min(race.LapTimes[0], Mathf.Min(race.LapTimes[1], race.LapTimes[2]))) < 0.001f, RaceController.Fmt(race.BestLap));
            yield return Idle(3);
            shots.Enqueue("b_results_with_penalties");
            yield return Idle(30);
            race.EndRace(); yield return Idle(2);
        }

        IEnumerator RunShortcut()
        {
            Note(""); Note("C. Official shortcut (a TEST shortcut on Chasefield Way, added by this check only)");
            RaceController.ExtraShortcuts.Clear(); RaceController.ExtraShortcuts.Add(testShortcut);
            RaceController.LapsOverride = 2;
            race.StartRace(); yield return WaitRacing();
            var r = race.Route; var cps = r.Checkpoints;
            Check("the route has the test shortcut", r.Shortcuts.Count == 1, r.Shortcuts.Count + " shortcut(s)");
            var sc = r.Shortcuts[0];
            Note($"   shortcut '{sc.Name}': entry plane at ({sc.Entry.Position.x:0},{sc.Entry.Position.z:0}) half width {sc.Entry.HalfWidth:0} m, exit ({sc.Exit.Position.x:0},{sc.Exit.Position.z:0}), forgives CP{sc.FirstForgiven + 1} to CP{sc.LastForgiven + 1}, path {sc.Path.Length} points");
            yield return Cross(r.Gate, 0, 0);
            // lap 1: take the shortcut: enter it (nothing else crossed), then CP4, CP5, finish
            yield return Cross(sc.Entry, 0, 0);
            Check("entering the shortcut forgives CP1-3 with no penalty", race.NextCheckpoint == 3 && race.PenaltyTotal == 0f && race.ForgivenThisLap.Count == 3, $"next index {race.NextCheckpoint}, penalty {race.PenaltyTotal:0}, forgiven {string.Join(",", race.ForgivenThisLap.Select(x => x + 1))}");
            Check("... the HUD says shortcut, no penalty", race.Message.Contains("Shortcut") && race.Message.Contains("no penalty"), race.Message);
            yield return Cross(sc.Exit, 0, 0);
            Check("leaving the shortcut ends the 'on the shortcut' state", race.ActiveShortcut == null);
            yield return Cross(cps[3], 0, 0); yield return Cross(cps[4], 0, 0);
            yield return Cross(r.Gate, 0, 0);
            Check("lap 1 by shortcut counts, no penalty", race.CompletedLaps == 1 && race.PenaltyTotal == 0f, $"penalty {race.PenaltyTotal:0} s, lap {RaceController.Fmt(race.LapTimes.Count > 0 ? race.LapTimes[0] : 0)}");
            Check("lap line says which checkpoints were forgiven", race.LapLog.Count == 1 && race.LapLog[0].Contains("forgiven"), race.LapLog.Count > 0 ? race.LapLog[0] : "");
            // lap 2 (control): the entry plane crossed the wrong way does nothing, so skipping CP1-3 costs 15 s
            yield return Cross(sc.Entry, 0, 0, backwards: true);
            Check("entry plane crossed backwards forgives nothing", race.NextCheckpoint == 0 && race.ForgivenThisLap.Count == 0);
            yield return Cross(sc.Entry, sc.Entry.HalfWidth + 6f, 0);
            Check("entry plane crossed far outside its width forgives nothing", race.NextCheckpoint == 0);
            yield return Cross(cps[3], 0, 0);
            Check("control: without the shortcut CP1-3 cost 15 s", race.PenaltyTotal == 15f && race.MissedTotal == 3, $"penalty {race.PenaltyTotal:0} s");
            yield return Cross(cps[4], 0, 0); yield return Cross(r.Gate, 0, 0);
            Check("race finished; total carries the 15 s", race.State == RaceController.Stage.Finished && Mathf.Abs((race.FinalTotal - race.PenaltyTotal) - race.Clock) < 0.3f, $"total {RaceController.Fmt(race.FinalTotal)}");
            race.EndRace(); yield return Idle(2);
            RaceController.ExtraShortcuts.Clear();
        }

        IEnumerator RunBarriers()
        {
            Note(""); Note("D. Closed roads (barriers)");
            RaceController.LapsOverride = 3;
            // D1: with the test shortcut, the barriers at its two ends are left out
            RaceController.ExtraShortcuts.Add(testShortcut);
            race.StartRace(); yield return WaitRacing();
            int withShortcut = race.Barriers.Count; var open = new List<string>(race.Barriers.Open);
            Check("barriers at the test shortcut's ends are left open", open.Count == 2 && open.All(o => o.Contains("Chasefield")), $"{withShortcut} barriers, open: {string.Join("; ", open)}");
            race.EndRace(); RaceController.ExtraShortcuts.Clear(); yield return Idle(2);
            // D2: without it, every closure has a barrier
            Check("before the race starts there are no barriers", race.Barriers.Count == 0);
            race.StartRace(); yield return WaitRacing();
            var br = race.Barriers; var route = race.Route;
            Check("every closure in the route data has a barrier", br.Count == route.Closures.Count && br.Count > 0, $"{br.Count} barriers for {route.Closures.Count} closures");
            Check("two more than with the shortcut (the Chasefield Way ends)", br.Count == withShortcut + 2, $"{br.Count} vs {withShortcut}");
            Note("   barriers, in lap order:");
            foreach (var b in br.Built) Note($"     {b.Data.id,-4} {b.Data.label}  ({b.Centre.x:0},{b.Centre.z:0})  {b.HalfLength * 2f:0.0} m wide  {b.Data.width_m:0} m road");
            // barriers stay clear of the course: the grid, the lap and the lead-in
            float minToCourse = 1e9f; string worst = "";
            foreach (var b in br.Built)
            {
                float d = DistanceToCourse(b.Centre, route);
                if (d < minToCourse) { minToCourse = d; worst = b.Data.id; }
            }
            Check("every barrier is at least 10 m from the course centreline", minToCourse >= 10f, $"closest {worst} at {minToCourse:0.0} m");
            float toGrid = br.Built.Min(b => Vector3.Distance(Flat(b.Centre), Flat(route.GridPosition)));
            Check("the grid is not boxed in (nearest barrier 10 m+ away)", toGrid >= 10f, $"{toGrid:0.0} m");
            // pick the Courtfield Trace barrier (a plain side street) for the physical test
            var target = br.Built.FirstOrDefault(b => b.Data.label.Contains("Courtfield")) ?? br.Built[0];
            Note($"   physical test on {target.Data.id}: {target.Data.label}");
            yield return DriveInto(target, 28f, "d_barrier_in_race", true);
            race.EndRace(); yield return Idle(5);
            Check("free driving again: all barrier objects gone", race.Barriers.Count == 0 && GameObject.Find("RaceBarriers").transform.childCount == 0);
            yield return DriveInto(target, 28f, "d_barrier_gone_in_free", false);
        }

        // Drives the real car at a barrier along its road, from 'from' metres back, for up to 8 s of game time.
        IEnumerator DriveInto(RaceBarriers.Barrier b, float from, string shot, bool expectStopped)
        {
            Vector3 start = b.Centre - b.Along * from;
            start.y = RaceRoute.Ground(start.x, start.z).y;
            car.Body.useGravity = true;
            float yaw = Mathf.Atan2(b.Along.x, b.Along.z) * Mathf.Rad2Deg;
            respawn.PlaceAt(start + Vector3.up * 0.3f, yaw);
            yield return Idle(25);
            float maxS = -1e9f; float stopT = -1f; int shotted = 0;
            for (int i = 0; i < 400; i++)
            {
                float speed = car.ForwardSpeed;
                Vector3 aim = b.Centre + b.Along * 10f - car.transform.position; aim.y = 0f;
                float steer = Mathf.Clamp(Vector3.SignedAngle(Flat(car.transform.forward), aim, Vector3.up) / 25f, -1f, 1f);
                car.Simulate(speed < 14f ? 1f : 0f, 0f, steer, Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
                Vector3 rel = car.transform.position - b.Centre;
                float s = Vector3.Dot(rel, b.Along);
                maxS = Mathf.Max(maxS, s);
                if (shotted == 0 && s > -9f) { shotted = 1; shots.Enqueue(shot + "_approach"); }
                if (expectStopped && s > -3f && car.Body.linearVelocity.magnitude < 0.4f && stopT < 0f) stopT = Time.time;
                if (!expectStopped && s > 4f) break;
                if (expectStopped && stopT > 0f && Time.time - stopT > 1.0f) break;
            }
            float sEnd = Vector3.Dot(car.transform.position - b.Centre, b.Along);
            float lateralOff = Mathf.Abs(Vector3.Dot(car.transform.position - b.Centre, Vector3.Cross(Vector3.up, b.Along)));
            if (expectStopped)
            {
                shots.Enqueue(shot + "_stopped"); yield return Idle(4);
                Check("the car is stopped by the barrier (does not pass it)", maxS < 0f && maxS > -4f && car.Body.linearVelocity.magnitude < 1.5f, $"closest to the barrier line {maxS:0.00} m short of it (negative = before it), speed {car.Body.linearVelocity.magnitude:0.0} m/s, lateral {lateralOff:0.0} m from the centre");
            }
            else Check("the same drive in free driving passes the barrier line", maxS > 3f, $"went {maxS:0.0} m past the line");
        }

        static float DistanceToCourse(Vector3 p, RaceRoute r)
        {
            float best = 1e9f;
            for (int i = 0; i + 1 < r.Path.Length; i++) best = Mathf.Min(best, SegDist(p, r.Path[i], r.Path[i + 1]));
            return best;
        }
        static float SegDist(Vector3 p, Vector3 a, Vector3 b)
        {
            a.y = b.y = p.y = 0f; Vector3 ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return Vector3.Distance(p, a + ab * t);
        }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        // ---------------------------------------------------------------- screenshots and the end

        void Update()
        {
            if (shots.Count > 0 && !capturing) StartCoroutine(Shot(shots.Dequeue()));
        }

        IEnumerator Shot(string name)
        {
            capturing = true;
            yield return new WaitForEndOfFrame();
            try
            {
                var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
                report.AppendLine($"(screenshot {name} saved, {Screen.width}x{Screen.height})");
                Destroy(tex);
            }
            catch (Exception e) { report.AppendLine("screenshot failed: " + e.Message); }
            capturing = false;
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            StartCoroutine(Conclude());
        }

        IEnumerator Conclude()
        {
            float t0 = Time.unscaledTime;
            while ((shots.Count > 0 || capturing) && Time.unscaledTime - t0 < 20f) yield return null;
            report.AppendLine();
            report.AppendLine("Race events:");
            foreach (var e in events) report.AppendLine("  " + e);
            report.AppendLine();
            report.AppendLine(failures.Count == 0 ? $"ALL {checks} CHECKS PASSED" : $"{failures.Count} OF {checks} CHECKS FAILED: " + string.Join("; ", failures));
            Directory.CreateDirectory(outputDir);
            File.WriteAllText(Path.Combine(outputDir, "round6_check.txt"), report.ToString());
            Debug.Log("[Round6Check]\n" + report);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
#endif
        }
    }
}
