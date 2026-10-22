using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 59 in the exe (-exeRound59 <dir> [-r59Only load,fps,drive,pics] [-r59Drives <file>] -logFile <dir>\Player.log); the races: -exeRound58 ... -r58Only aitable:
    //   load   how long from the process's start to the world being up, the memory in use, the terrain tiles, trees and buildings
    //   fps    the frame rate parked at the old neighborhood (Canton Road by the shops, Round 35's spot) and in the new land (Town Center, KSU, the
    //          I-75 / I-575 interchange, the old I-575 edge), uncapped, each over 10 s (the window's own size: run it at 1920 x 1080 for 1080p)
    //   drive  free roam from Chastain Glen to Town Center and to KSU along the roads (the lines in -r59Drives, from Tools/mapgen/ext_drives.py), at
    //          up to 25 m/s: never under the ground, never below the terrain, through
    //   pics   the mall, the campus, the interchange, the old I-575 edge joined
    public sealed partial class Round20Runner
    {
        static readonly (string name, Vector2 at, float yaw)[] R59Spots =
        {
            ("old neighborhood (Canton Road by the shops)", new Vector2(3502.8f, 1166.1f), -13f),
        };
        [System.Serializable] sealed class R59Places { public R59Place[] places = new R59Place[0]; public R59Line[] drives = new R59Line[0]; }
        [System.Serializable] sealed class R59Place { public string name; public float x, z, yaw, h; public float[] look; }
        [System.Serializable] sealed class R59Line { public string name; public float[] points; }
        R59Places r59;
        R59Places R59Data()
        {
            if (r59 != null) return r59;
            var argv = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(argv, "-r59Drives");
            r59 = i >= 0 && i + 1 < argv.Length && File.Exists(argv[i + 1]) ? JsonUtility.FromJson<R59Places>(File.ReadAllText(argv[i + 1])) : new R59Places();
            return r59;
        }

        IEnumerator R59Load()
        {
            report.AppendLine(); report.AppendLine("=== Load and size");
            var proc = System.Diagnostics.Process.GetCurrentProcess();
            float load = R20Started;
            report.AppendLine($"INFO  the world up {load:0.0} s after the process started (this check's runner began then); memory: working set {proc.WorkingSet64 / 1048576} MB, " +
                              $"Unity reserved {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576} MB, allocated {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576} MB");
            var ext = GroundQuery.Extent();
            report.AppendLine($"INFO  terrain: {Terrain.activeTerrains.Length} tiles, x {ext.xMin:0.0} to {ext.xMax:0.0}, z {ext.yMin:0.0} to {ext.yMax:0.0} ({ext.width:0} x {ext.height:0} m)");
            var tf = FindAnyObjectByType<TreeField>(); report.AppendLine($"INFO  trees: {(tf ? tf.count : -1)}");
            Check("load: the world covers the extension (west of x 0 and south of z 0)", ext.xMin < -1000f && ext.yMin < -1000f, $"{ext}");
            yield break;
        }

        IEnumerator R59Fps()
        {
            report.AppendLine(); report.AppendLine($"=== Frame rate, uncapped, parked, chase camera, {Screen.width} x {Screen.height} (not measured at 3840 x 2160)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            var spots = R59Spots.ToList(); foreach (var p in R59Data().places) spots.Add((p.name, new Vector2(p.x, p.z), p.yaw));
            foreach (var (name, at, yaw) in spots)
            {
                respawn.PlaceAt(RaceRoute.Surface(at.x, at.y) + Vector3.up * 0.4f, yaw); race.NotifyTeleported();
                yield return new WaitForSecondsRealtime(4f);
                var frames = new List<float>(); float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
                report.AppendLine($"INFO  {name}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps");
                yield return Capture("59_fps_" + new string(name.Where(char.IsLetterOrDigit).Take(24).ToArray()));
            }
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }

        IEnumerator R59Drive()
        {
            report.AppendLine(); report.AppendLine("=== Free roam drives along the roads, Street Classic, up to 25 m/s");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            foreach (var d in R59Data().drives)
            {
                var line = new List<Vector3>(); for (int i = 0; i + 1 < d.points.Length; i += 2) line.Add(RaceRoute.Surface(d.points[i], d.points[i + 1]));
                if (line.Count < 3) continue;
                Place(line[0], line[2] - line[0], 8f); yield return new WaitForFixedUpdate();
                var run = new Run(); float below = 0f, lowest = 99f; Vector3 lowAt = Vector3.zero; int resets = 0;
                float len = 0f; for (int i = 1; i < line.Count; i++) len += Flat(line[i] - line[i - 1]).magnitude;
                yield return DriveLine(line, 25f, len / 9f + 60f, run, null, r =>
                {
                    var p = car.Body.position; float g = R59Terrain(p);
                    float gap = p.y - g; if (gap < lowest) { lowest = gap; lowAt = p; }
                    if (gap < -1.5f) below += Time.fixedDeltaTime;
                    return false;
                }, 6f, false);
                report.AppendLine($"  {d.name}: {len:0} m, {(run.reached ? "through" : "NOT through: " + run.end)} in {run.time:0} s; lowest the body's middle against the ground under it {lowest:0.00} m at ({lowAt.x:0}, {lowAt.z:0}); under the ground {below:0.0} s");
                Check($"drive {d.name}: through, never under the ground", run.reached && below < 0.5f, $"{(run.reached ? "through" : run.end)}, under {below:0.0} s");
            }
        }

        // the terrain's own height under p (not a deck or a roof)
        static float R59Terrain(Vector3 p)
        {
            foreach (var t in Terrain.activeTerrains) { var o = t.transform.position; var s = t.terrainData.size; if (p.x >= o.x && p.x <= o.x + s.x && p.z >= o.z && p.z <= o.z + s.z) return t.SampleHeight(p) + o.y; }
            return float.NegativeInfinity;
        }

        static Vector3 R59Eye(R59Place p)
        {
            float a = R59Terrain(new Vector3(p.look[0], 0f, p.look[2])), b = R59Terrain(new Vector3(p.look[3], 0f, p.look[5]));
            float g = Mathf.Max(float.IsInfinity(a) ? -1e9f : a, float.IsInfinity(b) ? -1e9f : b);
            return new Vector3(p.look[0], g + (p.h > 0f ? p.h : 60f), p.look[2]);
        }

        IEnumerator R59Pics()
        {
            report.AppendLine(); report.AppendLine("=== Pictures");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            foreach (var p in R59Data().places)
            {
                if (p.look == null || p.look.Length < 6) continue;
                yield return Still("59_" + new string(p.name.Where(char.IsLetterOrDigit).Take(24).ToArray()), R59Eye(p), R59Terrain(new Vector3(p.look[3], 0f, p.look[5])) is float ty && !float.IsInfinity(ty) ? new Vector3(p.look[3], ty, p.look[5]) : RaceRoute.Surface(p.look[3], p.look[5]), 60f);   // from 'h' over the higher of the two grounds
            }
        }
    }
}
