using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Development check only (added by Editor/RaceCheck.cs, never saved into the scene). Round 5, item 8: drives the route by
    // script through the real ArcadeVehicle.Simulate step and the real RaceController, and writes what registered.
    //   mode "lap"   grid -> start line -> one full lap: every checkpoint in order, the lap counted, its time recorded.
    //   mode "full"  the same, then a lap that cuts through Chasefield Way (must NOT count and must name the missed
    //                checkpoint), then laps until the race finishes; also bug capture during the race (the clock must stop)
    //                and the records file.
    // Windowed Editor run (not -batchmode), because the screenshots include the race HUD and the debug panel.
    public sealed class RaceCheckRunner : MonoBehaviour
    {
        public string outputDir, mode = "full", recordsPath;
        public List<Vector3> shortcut = new List<Vector3>();   // Chasefield Way, from the first Chasefield junction to the second (forward direction only)
        public float targetSpeed = 15f, timeScale = 6f;

        RaceController race;
        ArcadeVehicle car;
        DeveloperHud hud;
        readonly List<Vector3> path = new List<Vector3>();
        readonly List<string> events = new List<string>();
        readonly StringBuilder report = new StringBuilder();
        readonly List<string> failures = new List<string>();
        readonly Queue<string> shots = new Queue<string>();
        int next, checks;
        float t, maxSim = 1400f;
        bool started, driving, done, shortcutLap, pauseTested, shotCountdown, shotCheckpoint, shotMissed, shotFinish, shotNear;
        float goAt = -1f, startRequestedAt;
        Vector3 gridPos; float gridYawErr, gridDist;
        int shortcutStart = -1, shortcutEnd = -1;
        float pauseClockBefore;
        bool pauseWaiting;
        float pauseUntilReal;
        string lastMissedMessage = "";

        void Awake() { RaceController.RecordsPathOverride = recordsPath; } // before the controller loads the records

        void Start()
        {
            car = FindAnyObjectByType<ArcadeVehicle>();
            race = RaceController.Instance;
            hud = DeveloperHud.Instance;
            car.enabled = false; // this runner calls Simulate itself
            Time.timeScale = timeScale;
            race.Logged += s => events.Add($"{t,7:0.0}  {s}");
        }

        void Check(string name, bool ok, string detail = "")
        {
            checks++;
            if (!ok) failures.Add(name);
            report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}");
        }

        void BuildPath()
        {
            var r = race.Route; float L = r.LapLength, gateS = r.GateS;
            if (!r.Reverse) foreach (var q in LeadIn()) path.Add(q);
            else path.AddRange(r.SamplePath(gateS - race.RouteData.reverse_grid_back_m, gateS, 5f));
            // to the gate, then one full lap, then a little past the gate
            if (mode == "lap") { path.AddRange(r.SamplePath(r.Reverse ? gateS : 0f, gateS + L + 40f, 5f)); return; }
            // full: lap 1 normal; lap 2 by the shortcut (forward only); then normal laps until the race ends
            path.AddRange(r.SamplePath(r.Reverse ? gateS : 0f, gateS + L, 5f));
            if (!r.Reverse && shortcut.Count > 1)
            {
                path.AddRange(r.SamplePath(gateS + L, gateS + L + r.Checkpoints[0].S, 5f)); shortcutStart = path.Count - 1;
                path.AddRange(shortcut); shortcutEnd = path.Count - 1;
                path.AddRange(r.SamplePath(gateS + L + r.Checkpoints[2].S, gateS + 2 * L, 5f));
                path.AddRange(r.SamplePath(gateS + 2 * L, gateS + 3 * L, 5f));
                path.AddRange(r.SamplePath(gateS + 3 * L, gateS + 4 * L + 40f, 5f));
            }
            else path.AddRange(r.SamplePath(gateS + L, gateS + 4 * L + 40f, 5f));
        }

        IEnumerable<Vector3> LeadIn()
        {
            var li = race.RouteData.lead_in;
            for (int i = 0; i + 1 < li.Length; i += 2) yield return new Vector3(li[i], GroundQuery.Height(li[i], li[i + 1]), li[i + 1]);
        }

        void FixedUpdate()
        {
            if (done) return;
            t += Time.fixedDeltaTime;
            if (!started)
            {
                started = true; startRequestedAt = t;
                race.StartRace();                // same call the T key makes
                BuildPath();
                gridPos = race.Route.GridPosition;
                gridDist = Vector3.Distance(Flat(car.transform.position), Flat(gridPos));
                gridYawErr = Mathf.Abs(Mathf.DeltaAngle(car.transform.eulerAngles.y, race.Route.GridYaw));
                Check("start: state is Countdown", race.State == RaceController.Stage.Countdown, race.State.ToString());
                Check("start: car on the grid", gridDist < 1.0f, $"{gridDist:0.00} m from the grid point");
                Check("start: car faces the route direction", gridYawErr < 3f, $"{gridYawErr:0.0} deg off the grid heading {race.Route.GridYaw:0.0}");
                return;
            }
            if (race.State == RaceController.Stage.Countdown)
            {
                if (!shotCountdown && race.CountdownRemaining < 2.2f) { shotCountdown = true; shots.Enqueue("01_countdown"); }
                return;
            }
            if (goAt < 0f) { goAt = t; Check("countdown lasts 3 s", Mathf.Abs((goAt - startRequestedAt) - 3f) < 0.15f, $"{goAt - startRequestedAt:0.00} s"); driving = true; }
            if (race.State == RaceController.Stage.Finished) { Finish(); return; }
            if (t > maxSim) { report.AppendLine("TIMEOUT"); Check("race finished within the time limit", false); Finish(); return; }
            Drive();
            Watch();
        }

        void Drive()
        {
            var pos = car.transform.position;
            while (next < path.Count - 1 && Flat(path[next] - pos).magnitude < 9f) next++;
            Vector3 aim = path[Mathf.Min(next, path.Count - 1)];
            Vector3 to = Flat(aim - pos);
            float angle = Vector3.SignedAngle(Flat(car.transform.forward), to, Vector3.up);
            float steer = Mathf.Clamp(angle / 25f, -1f, 1f);
            float speed = car.ForwardSpeed;
            float want = Mathf.Abs(angle) > 35f ? targetSpeed * 0.5f : targetSpeed;
            car.Simulate(speed < want ? 1f : 0f, speed > want + 3f ? 0.6f : 0f, steer, Time.fixedDeltaTime);
            if (next >= path.Count - 1 && Flat(path[path.Count - 1] - pos).magnitude < 12f && race.State == RaceController.Stage.Racing)
            { if (mode != "lap") report.AppendLine("Path ended before the race did."); Finish(); }
        }

        // screenshots, the bug-capture-during-race test, and "which shortcut lap is this"
        void Watch()
        {
            if (!shotCheckpoint && events.Exists(e => e.Contains("CHECKPOINT 1 at"))) { shotCheckpoint = true; shots.Enqueue("03_checkpoint_1_passed_next_lit"); }
            var tg = race.Target;
            if (!shotNear && tg != null && tg.Id == 2 && Vector3.Distance(Flat(tg.Position), Flat(car.transform.position)) < 40f) { shotNear = true; shots.Enqueue("02_next_checkpoint_close"); }
            if (!shotMissed && events.Exists(e => e.Contains("MISSED"))) { shotMissed = true; shots.Enqueue("04_missed_checkpoint"); }
            if (!pauseTested && mode == "full" && race.CompletedLaps >= 2 && race.NextCheckpoint >= 1 && hud != null)
            {
                pauseTested = true; pauseClockBefore = race.Clock;
                hud.CaptureBug(); pauseWaiting = true; pauseUntilReal = Time.unscaledTime + 4f;
            }
        }

        void Update()
        {
            if (shots.Count > 0 && !capturing) StartCoroutine(Shot(shots.Dequeue()));
            if (pauseWaiting && hud != null)
            {
                if (hud.CommentOpen)
                {
                    // the game is paused here (Time.timeScale 0): wait real time, then check the race clock did not move
                    if (pauseUntilReal > Time.unscaledTime + 3.2f) pauseUntilReal = Time.unscaledTime + 0.8f;
                    float clockNow = race.Clock;
                    if (Time.unscaledTime >= pauseUntilReal)
                    {
                        Check("bug capture during a race: clock stopped while paused", Mathf.Abs(clockNow - pauseClockBefore) < 0.1f && Time.timeScale == 0f, $"clock {pauseClockBefore:0.00} -> {clockNow:0.00}, timeScale {Time.timeScale}");
                        hud.CommentText = "Automated check: bug captured during a race";
                        hud.SaveComment();
                        pauseWaiting = false;
                        Check("bug capture during a race: saved, game running again", Time.timeScale > 0f && !hud.CommentOpen, hud.Status);
                    }
                }
                else if (Time.unscaledTime > pauseUntilReal + 2f) { Check("bug capture during a race: comment box opened", false, hud.Status); pauseWaiting = false; }
            }
        }

        bool capturing;
        IEnumerator Shot(string name)
        {
            capturing = true;
            report.AppendLine($"(screenshot {name} requested at {t:0.0} s, screen {Screen.width}x{Screen.height})");
            yield return new WaitForEndOfFrame();
            try
            {
                var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
                report.AppendLine($"(screenshot {name} saved)");
                Destroy(tex);
            }
            catch (Exception e) { report.AppendLine("screenshot failed: " + e.Message); }
            capturing = false;
        }

        void Finish()
        {
            if (done) return;
            done = true;
            StartCoroutine(Conclude());
        }

        IEnumerator Conclude()
        {
            if (race.State == RaceController.Stage.Finished) { shots.Enqueue("05_results"); }
            yield return null; yield return null; yield return new WaitForSecondsRealtime(0.6f);
            var laps = race.LapTimes;
            report.AppendLine();
            report.AppendLine($"Route {race.Route.Name}, direction {race.Route.Direction}, lap {race.Route.LapLength:0.0} m, {race.Route.Checkpoints.Count} checkpoints, run-in {race.Route.RunInLength:0} m, mode {mode}");
            report.AppendLine("Checkpoints in travel order:");
            foreach (var c in race.Route.Checkpoints) report.AppendLine($"  CP{c.Id} {c.S:0} m after the gate  ({c.Position.x:0},{c.Position.z:0})  {c.Label}");
            report.AppendLine("Junctions (m after the gate): " + string.Join("; ", race.Route.Junctions));

            // checkpoint order within each lap, from the event log
            var seq = new List<int>(); var lapChecks = new List<List<int>>(); List<int> cur = null;
            foreach (var e in events)
            {
                if (e.Contains("START LINE crossed")) { cur = new List<int>(); lapChecks.Add(cur); }
                else if (e.Contains("LAP ") && cur != null) { cur = new List<int>(); lapChecks.Add(cur); }
                else if (e.Contains("CHECKPOINT ") && cur != null) { int id = int.Parse(e.Substring(e.IndexOf("CHECKPOINT ") + 11).Split(' ')[0]); cur.Add(id); }
            }
            int n = race.Route.Checkpoints.Count;
            var all = new List<int>(); for (int i = 1; i <= n; i++) all.Add(i);
            if (lapChecks.Count > 0)
            {
                var first = lapChecks[0];
                Check("lap 1: every checkpoint registered, in order", first.Count == n && first.SequenceEqual(all), "passed " + string.Join(",", first));
            }
            Check("lap 1: counted, time recorded", laps.Count >= 1 && laps[0] > 20f, laps.Count >= 1 ? RaceController.Fmt(laps[0]) : "no lap");
            if (laps.Count >= 1)
            {
                Check("lap 1: time is plausible for the length", laps[0] > race.Route.LapLength / 30f && laps[0] < race.Route.LapLength / 4f, $"{laps[0]:0.0} s for {race.Route.LapLength:0} m = {race.Route.LapLength / laps[0]:0.0} m/s average");
            }
            Check("the start line starts the clock", events.Exists(e => e.Contains("START LINE crossed")));
            if (mode == "full")
            {
                bool shortcutRan = shortcutEnd > 0;
                if (shortcutRan)
                {
                    Check("shortcut lap (Chasefield Way): not counted", events.Exists(e => e.Contains("NOT COUNTED")));
                    var miss = events.Find(e => e.Contains("NOT COUNTED"));
                    Check("shortcut lap: the missed checkpoint is named (CP2)", miss != null && miss.Contains("CP2"), miss ?? "no event");
                    Check("shortcut lap: CP1 was not reported missed", miss != null && !miss.Contains("CP1"));
                    Check("an on-the-spot 'missed' event came before the end of the lap", events.Exists(e => e.Contains("MISSED")));
                }
                else report.AppendLine("(no shortcut path was supplied; shortcut lap not run)");
                Check("race finishes after 3 counted laps", race.State == RaceController.Stage.Finished && race.CompletedLaps == race.Route.Laps, $"{race.CompletedLaps} laps, state {race.State}");
                if (race.State == RaceController.Stage.Finished)
                {
                    float sum = 0; foreach (var l in laps) sum += l;
                    Check("finish: total time recorded", race.FinalTotal > sum - 0.01f, $"total {RaceController.Fmt(race.FinalTotal)} vs sum of counted laps {RaceController.Fmt(sum)}" + (shortcutRan ? " (the not-counted lap is in the total)" : ""));
                    string rp = RaceController.RecordsPath();
                    bool has = File.Exists(rp);
                    Check("records file written", has, rp);
                    if (has) { var txt = File.ReadAllText(rp); Check("records file holds best lap and best total", txt.Contains("best_lap") && txt.Contains("best_total") && race.BestLap > 0 && race.BestTotal > 0, txt.Replace("\n", " ")); }
                    Check("best lap = fastest counted lap", Mathf.Abs(race.BestLap - Min(laps)) < 0.001f, $"{race.BestLap:0.000} vs {Min(laps):0.000}");
                }
                // leave the race: free driving
                race.EndRace();
                yield return new WaitForFixedUpdate();
                var inp = car.GetComponent<VehicleInput>();
                Check("leaving the race returns to free driving", race.State == RaceController.Stage.Free && inp.enabled);
                Check("debug HUD present during a race", hud != null);
                Check("bump logger present", FindAnyObjectByType<BumpLogger>() != null);
            }
            report.AppendLine();
            report.AppendLine("Race events:");
            foreach (var e in events) report.AppendLine("  " + e);
            report.AppendLine();
            report.AppendLine(failures.Count == 0 ? $"ALL {checks} CHECKS PASSED" : $"{failures.Count} OF {checks} CHECKS FAILED: " + string.Join("; ", failures));
            Directory.CreateDirectory(outputDir);
            File.WriteAllText(Path.Combine(outputDir, "race_check_" + mode + "_" + race.Route.Direction + ".txt"), report.ToString());
            Debug.Log("[RaceCheck]\n" + report);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
#endif
        }

        static float Min(IReadOnlyList<float> l) { float m = float.MaxValue; foreach (var x in l) m = Mathf.Min(m, x); return m; }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }

    static class ListExt
    {
        public static bool SequenceEqual(this List<int> a, List<int> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
