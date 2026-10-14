using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Racer;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

namespace ChastainRush
{
    // Round 24 Part A2, item A1: where the frame time goes, in the built exe. ChastainRush.exe -exeProfile <dir> visits each
    // place (Dan's BUG-003 and BUG-004, underground in the hub and J4, and the grid of all four races with their AI driving),
    // lets it settle, then for a few seconds records the frame time, FrameTimingManager's CPU / GPU split, the render counters
    // and (in a development build) every profiler marker, and writes the top ones to <dir>/profile.txt. With -exeProfileRaw a
    // binary profiler capture (<name>.raw, open it in the Editor's Profiler) is written for each place as well. Then it quits.
    // In a release build the marker list is empty (Unity records them in development builds only); the frame times still count.
    public sealed class ProfileRunner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        ArcadeVehicle car; VehicleRespawn respawn; RaceController race;

        struct Place { public string name; public float x, z, yaw, y; public string route, dir; }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;   // measure the real cost, not the monitor's pace
            bool raw = Environment.GetCommandLineArgs().Contains("-exeProfileRaw");
            shots = Environment.GetCommandLineArgs().Contains("-profileShots");   // a picture at each place
            string only = Arg("-profileOnly");
            report.AppendLine($"Chastain Rush profile, {DateTime.Now:yyyy-MM-dd HH:mm:ss}, screen {Screen.width}x{Screen.height}, {(Debug.isDebugBuild ? "development build" : "release build")}, {SystemInfo.graphicsDeviceName}, {SystemInfo.processorType}");
            report.AppendLine("vSync off, no frame cap (cost per frame; 16.7 ms = 60 fps)");
            yield return new WaitForSecondsRealtime(5f);
            car = FindAnyObjectByType<ArcadeVehicle>(); respawn = car.GetComponent<VehicleRespawn>(); race = FindAnyObjectByType<RaceController>();
            var places = new List<Place>
            {
                new Place { name = "free_bug003_creek_trail", x = 2340.43f, z = 1148.03f, yaw = 9.5f },
                new Place { name = "free_bug004_creek_bridges", x = 2434.99f, z = 796.73f, yaw = 153f },
                new Place { name = "free_glen_entrance", x = 2860f, z = 760f, yaw = 0f },
                new Place { name = "under_hub", x = 1900f, z = 1145f, yaw = 90f, y = 253f },
                new Place { name = "under_j4", x = 2280f, z = 1215f, yaw = 75f, y = 269.2f },
                // Round 25 (A8): in the four glass stretches (M1 under the pond, M5 along the Enclave pond, M2 along East Chastain Lake, P1)
                new Place { name = "glass_m1", x = 1563f, z = 1139f, yaw = 134f, y = 274.8f },
                new Place { name = "glass_m5", x = 1645.5f, z = 969.6f, yaw = 184f, y = 274.6f },
                new Place { name = "glass_m2", x = 1338.8f, z = 1162.2f, yaw = 257f, y = 283.5f },
                new Place { name = "glass_p1", x = 2355f, z = 1304.5f, yaw = 77f, y = 270.3f },
                new Place { name = "race_glen_forward", route = "chastain_glen_loop.json", dir = "forward" },
                new Place { name = "race_glen_reverse", route = "chastain_glen_loop.json", dir = "reverse" },
                new Place { name = "race_creek_forward", route = "noonday_creek.json", dir = "forward" },
                new Place { name = "race_creek_reverse", route = "noonday_creek.json", dir = "reverse" },
                // the creek-trail exit during a race (closed), seen from the trail 12 m short of it
                new Place { name = "race_creek_xtrail", route = "noonday_creek.json", dir = "forward", x = 2343f, z = 1099.5f, yaw = 335f },
            };
            if (!string.IsNullOrEmpty(only)) places = places.Where(p => only.Split(',').Any(o => p.name.Contains(o))).ToList();
            foreach (var p in places)
            {
                if (p.route != null)
                {
                    RaceController.RouteOverride = p.route; RaceController.DirectionOverride = p.dir; RaceController.LapsOverride = 0;
                    RaceController.AiCount = 3; RaceController.AiVehiclesOverride = null;
                    if (race.State != RaceController.Stage.Free) race.EndRace();
                    race.Reload();
                    race.StartRace();
                    while (race.State == RaceController.Stage.Countdown) yield return null;
                    if (p.x != 0f) { respawn.PlaceAt(RaceRoute.Ground(p.x, p.z) + Vector3.up * 0.3f, p.yaw); race.NotifyTeleported(); }
                    yield return new WaitForSecondsRealtime(3f);   // the AI pull away down the course; the player waits on the grid
                }
                else
                {
                    if (race.State != RaceController.Stage.Free) race.EndRace();
                    var at = p.y > 0f ? new Vector3(p.x, p.y, p.z) : RaceRoute.Ground(p.x, p.z);
                    respawn.PlaceAt(at + Vector3.up * 0.3f, p.yaw);
                    yield return new WaitForSecondsRealtime(4f);
                }
                yield return Measure(p.name, raw);
                if (shots) yield return Capture(p.name);
            }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            File.WriteAllText(Path.Combine(outputDir, "profile.txt"), report.ToString());
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(0);
        }

        bool shots;
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            if (race.Barriers != null) report.AppendLine($"   race mouths closed: {string.Join(", ", race.Barriers.MouthsClosed)}");
        }

        static string Arg(string name) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

        IEnumerator Measure(string name, bool raw)
        {
            const int frames = 240;
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var recs = new List<(string name, string cat, ProfilerRecorder r)>();
            foreach (var h in handles)
            {
                var d = ProfilerRecorderHandle.GetDescription(h);
                if (d.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds && d.UnitType != ProfilerMarkerDataUnit.Count) continue;
                bool counter = (d.Flags & MarkerFlags.Counter) != 0;
                if (!counter && d.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds) continue;
                recs.Add((d.Name, d.Category.Name + (counter ? " (counter)" : ""), ProfilerRecorder.StartNew(d.Category, d.Name, frames + 8)));
            }
            if (raw) { Profiler.logFile = Path.Combine(outputDir, name + ".raw"); Profiler.enableBinaryLog = true; Profiler.enabled = true; }
            var dts = new List<float>(); double main = 0, render = 0, gpu = 0; int ft = 0;
            var timing = new FrameTiming[1];
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                dts.Add(Time.unscaledDeltaTime * 1000f);
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, timing) > 0) { main += timing[0].cpuMainThreadFrameTime; render += timing[0].cpuRenderThreadFrameTime; gpu += timing[0].gpuFrameTime; ft++; }
            }
            if (raw) { Profiler.enabled = false; Profiler.enableBinaryLog = false; Profiler.logFile = ""; }
            dts.Sort();
            float avg = dts.Average(), p99 = dts[(int)(dts.Count * 0.99f)];
            var pos = car.transform.position;
            report.AppendLine();
            report.AppendLine($"== {name}  (camera at {Camera.main.transform.position.x:0}, {Camera.main.transform.position.y:0}, {Camera.main.transform.position.z:0}; vehicle {pos.x:0}, {pos.y:0}, {pos.z:0}; race {race.State})");
            report.AppendLine($"   frame {avg:0.0} ms avg = {1000f / avg:0} fps; 99% {p99:0.0} ms; FrameTiming main {main / Mathf.Max(1, ft):0.0} ms, render thread {render / Mathf.Max(1, ft):0.0} ms, GPU {gpu / Mathf.Max(1, ft):0.0} ms; network cells on {NetworkCells.CellsOn}");
            var rows = new List<(string name, string cat, double v)>();
            foreach (var (n, c, r) in recs)
            {
                if (r.Count == 0) { r.Dispose(); continue; }
                double sum = 0; for (int i = 0; i < r.Count; i++) sum += r.GetSample(i).Value;
                double v = sum / r.Count; r.Dispose();
                rows.Add((n, c, v));
            }
            foreach (var cnt in new[] { "Batches Count", "SetPass Calls Count", "Triangles Count", "Vertices Count", "Shadow Casters Count", "Visible Lights Count", "Draw Calls Count", "GC Allocated In Frame", "Particle System Count", "Active Dynamic Bodies", "Active Kinematic Bodies", "Total Used Memory" })
                foreach (var row in rows.Where(x => x.name == cnt)) report.AppendLine($"   {cnt}: {row.v:0}");
            foreach (var row in rows.Where(x => !x.cat.EndsWith("(counter)")).OrderByDescending(x => x.v).Take(70))
                report.AppendLine($"   {row.v / 1e6,8:0.00} ms  {row.name}  [{row.cat}]");
            File.WriteAllText(Path.Combine(outputDir, "profile.txt"), report.ToString());
        }
    }
}
