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
    // Round 8, item 1: the check that runs INSIDE the built exe (ChastainRush.exe -exeCheck <dir>; added by GameShell only
    // when that argument is given). It measures the rendered frame rate of the real player at the three places that matter
    // and then runs a whole race by script, and writes exe_check.txt plus screenshots to <dir>, then quits.
    //   1. Parked views (10 s each, after 2 s to settle): the entrance (the race grid), beside the pond, outside Dan's house.
    //   2. A 3-lap race through the real RaceController (the T key's StartRace), the car driven along the route by
    //      ArcadeVehicle.Simulate: lap 1 at real time (frame times recorded with the car's position, so the three places are
    //      also measured while driving), laps 2 and 3 at 4x time to save time. Records go to <dir>, not to the player's file.
    public sealed class ExeCheckRunner : MonoBehaviour
    {
        public string outputDir;
        public float driveSpeed = 15f;

        ArcadeVehicle car;
        RaceController race;
        readonly StringBuilder report = new StringBuilder();
        readonly List<string> events = new List<string>();
        readonly List<(Vector3 p, float dt)> lapFrames = new List<(Vector3, float)>();
        readonly List<Vector3> path = new List<Vector3>();
        int next, fails, checks;
        bool driving, recordLap;

        void Check(string name, bool ok, string detail = "")
        {
            checks++; if (!ok) fails++;
            report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}");
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;   // the player pauses when it loses focus; the check must not
            report.AppendLine($"Chastain Rush exe check, {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Player: {(Application.isEditor ? "Editor" : "built exe")}, Unity {Application.unityVersion}, screen {Screen.width}x{Screen.height} {Screen.fullScreenMode}, " +
                              $"GPU {SystemInfo.graphicsDeviceName}, CPU {SystemInfo.processorType}, vSync {QualitySettings.vSyncCount}, target {Application.targetFrameRate}");
            report.AppendLine($"persistentDataPath: {Application.persistentDataPath}");
            yield return new WaitForSecondsRealtime(4f);   // first frames compile shaders and stream the terrain
            car = FindAnyObjectByType<ArcadeVehicle>();
            race = RaceController.Instance;
            Check("scene loaded: car, race controller, debug HUD, road lookup", car && race && DeveloperHud.Instance && RoadLookup.Instance);
            Check("Dan's house is in the scene", GameObject.Find("DanHouse") != null);
            Check("geese are in the scene", FindObjectsByType<GooseFlock>(FindObjectsSortMode.None).Length > 0, FindObjectsByType<GooseFlock>(FindObjectsSortMode.None).Length + " flock(s)");
            Check("fences are in the scene", FindObjectsByType<FenceChunk>(FindObjectsSortMode.None).Length > 0, FindObjectsByType<FenceChunk>(FindObjectsSortMode.None).Length + " fence chunks");
            Check("route loaded from the scene (embedded copy)", race && race.Route != null, race && race.Route != null ? $"{race.Route.Name}, {race.Route.Shortcuts.Count} official shortcuts" : "none");

            // ---- the three places
            var spots = Spots();
            report.AppendLine();
            report.AppendLine("Frame rate, parked (10 s, car still, chase camera behind it; frame time = wall clock between rendered frames):");
            foreach (var s in spots)
            {
                var respawn = car.GetComponent<VehicleRespawn>();
                respawn.PlaceAt(s.pos, s.yaw);
                yield return new WaitForSecondsRealtime(2f);
                var ft = new List<float>();
                float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; ft.Add(Time.unscaledDeltaTime); }
                report.AppendLine("  " + Stats(s.label, ft));
                yield return Shot("view_" + s.key);
            }

            // ---- the race
            RaceController.RecordsPathOverride = Path.Combine(outputDir, "race_records_exe_check.json");
            if (File.Exists(RaceController.RecordsPathOverride)) File.Delete(RaceController.RecordsPathOverride);
            race.Logged += e => events.Add($"{Time.time,7:0.0}  {e}");
            race.StartRace();
            Check("race started (T): countdown", race.State == RaceController.Stage.Countdown);
            BuildPath();
            car.enabled = false;
            while (race.State == RaceController.Stage.Countdown) yield return null;
            Check("countdown ended: racing", race.State == RaceController.Stage.Racing);
            driving = true; recordLap = true;
            float start = Time.realtimeSinceStartup;
            bool shotRace = false;
            while (race.State == RaceController.Stage.Racing && Time.realtimeSinceStartup - start < 600f)
            {
                if (recordLap && race.CompletedLaps >= 1) { recordLap = false; Time.timeScale = 4f; }
                if (!shotRace && race.ClockStarted && race.NextCheckpoint >= 2) { shotRace = true; yield return Shot("race_hud"); }
                yield return null;
            }
            driving = false;
            Time.timeScale = 1f;
            car.enabled = true;
            yield return new WaitForSecondsRealtime(1f);
            Check("race finished (3 counted laps)", race.State == RaceController.Stage.Finished && race.CompletedLaps == race.LapsInRace, $"{race.CompletedLaps} laps, state {race.State}, total {RaceController.Fmt(race.FinalTotal)}, laps {string.Join(" ", race.LapTimes.Select(RaceController.Fmt))}, penalties {race.PenaltyTotal:0} s");
            Check("records written (to the check's own file)", File.Exists(RaceController.RecordsPathOverride), RaceController.RecordsPathOverride);
            yield return Shot("race_results");
            race.EndRace();

            report.AppendLine();
            report.AppendLine($"Frame rate while driving lap 1 at {driveSpeed:0} m/s (real time):");
            report.AppendLine("  " + Stats("whole lap 1", lapFrames.Select(f => f.dt).ToList()));
            foreach (var s in spots)
            {
                var near = lapFrames.Where(f => Flat(f.p - s.pos).magnitude < 60f).Select(f => f.dt).ToList();
                report.AppendLine("  " + (near.Count > 10 ? Stats(s.label + ", within 60 m", near) : s.label + ": the lap does not pass within 60 m"));
            }

            // ---- files the player writes outside the project
            report.AppendLine();
            report.AppendLine("Run-time files:");
            report.AppendLine($"  bump log: {Path.GetFullPath(BumpLogger.LogPath)} exists {File.Exists(BumpLogger.LogPath)}");
            report.AppendLine($"  debug reports folder: {DeveloperHud.ReportRoot()}");
            report.AppendLine($"  race records (the player's own, untouched by this check): {Path.Combine(Application.persistentDataPath, "race_records.json")}");
            report.AppendLine();
            report.AppendLine("Race events:");
            foreach (var e in events) report.AppendLine("  " + e);
            report.AppendLine();
            report.AppendLine(fails == 0 ? $"ALL {checks} CHECKS PASSED" : $"{fails} OF {checks} CHECKS FAILED");
            File.WriteAllText(Path.Combine(outputDir, "exe_check.txt"), report.ToString());
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        struct Spot { public string key, label; public Vector3 pos; public float yaw; }

        // The entrance is the race grid; the pond and Dan's house are taken as the nearest point of the race route to the
        // fountain and to the house, with the car facing along the route there.
        List<Spot> Spots()
        {
            var l = new List<Spot>();
            var r = race.Route;
            l.Add(new Spot { key = "entrance", label = "Entrance (race grid on the entrance lane)", pos = r.GridPosition + Vector3.up * 0.3f, yaw = r.GridYaw });
            var fountain = FindAnyObjectByType<FountainSpray>();
            if (fountain) l.Add(Near("pond", "Beside the pond (route point nearest the fountain)", fountain.transform.position));
            var dan = GameObject.Find("DanHouse");
            if (dan) l.Add(Near("dan_house", "Outside Dan's house (route point nearest the house)", dan.transform.position));
            return l;
        }

        Spot Near(string key, string label, Vector3 target)
        {
            var r = race.Route; float best = float.MaxValue, bs = 0f;
            for (float s = 0; s < r.LapLength; s += 2f) { var p = r.PointAt(s, out _); float d = Flat(p - target).magnitude; if (d < best) { best = d; bs = s; } }
            var q = r.PointAt(bs - 25f, out var f);   // 25 m before it, so the place is in front of the camera
            return new Spot { key = key, label = label + $" ({best:0} m away)", pos = q + Vector3.up * 0.3f, yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg };
        }

        static string Stats(string label, List<float> ft)
        {
            if (ft.Count < 2) return label + ": no frames";
            var a = ft.ToArray(); Array.Sort(a);
            float total = a.Sum(), med = a[a.Length / 2], p99 = a[Mathf.Min(a.Length - 1, Mathf.FloorToInt(a.Length * 0.99f))];
            return $"{label}: {a.Length} frames in {total:0.0} s = {a.Length / total:0} fps average; median {med * 1000f:0.0} ms ({1f / med:0} fps); 99th percentile {p99 * 1000f:0.0} ms ({1f / p99:0} fps); worst {a[a.Length - 1] * 1000f:0.0} ms";
        }

        void BuildPath()
        {
            var r = race.Route; var li = race.RouteData.lead_in;
            for (int i = 0; i + 1 < li.Length; i += 2) path.Add(RaceRoute.Ground(li[i], li[i + 1]));
            path.AddRange(r.SamplePath(0f, r.GateS + 3 * r.LapLength + 60f, 5f));
        }

        void FixedUpdate()
        {
            if (!driving || !car) return;
            var pos = car.transform.position;
            while (next < path.Count - 1 && Flat(path[next] - pos).magnitude < 9f) next++;
            Vector3 to = Flat(path[next] - pos);
            float angle = Vector3.SignedAngle(Flat(car.transform.forward), to, Vector3.up);
            float steer = Mathf.Clamp(angle / 25f, -1f, 1f), speed = car.ForwardSpeed;
            float want = Mathf.Abs(angle) > 35f ? driveSpeed * 0.5f : driveSpeed;
            car.Simulate(speed < want ? 1f : 0f, speed > want + 3f ? 0.6f : 0f, steer, Time.fixedDeltaTime);
        }

        void Update()
        {
            if (driving && recordLap && race.ClockStarted) lapFrames.Add((car.transform.position, Time.unscaledDeltaTime));
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            try
            {
                var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
                Destroy(tex);
            }
            catch (Exception e) { report.AppendLine("screenshot failed: " + e.Message); }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
