using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 92 in the exe (-exeRound92 <dir> [-r92Only p92fps,...] [-steamDeck 1] -r20Minutes 60 -logFile <dir>\Player.log), from the repo root, on the check's own save.
    //   e92glass E: each creek pool (network.json pools): pictures from inside its pipe at the glass (the driver's seat and the chase camera,
    //            looking along the pipe) and from the pool's bank; whether the car stands in a pipe there (DrainPipe) and under water
    //   p92fps   item 0: how long the game took to load (process start to the check's first frame) and the frame rate parked at places across the
    //            map (10 s each after 3 s to settle, the chase camera, Free Roam, noon): the same code before and after the data files were split
    public sealed partial class Round20Runner
    {
        static readonly (string name, float x, float z, float yaw)[] R92Spots =
        {
            ("Chastain Glen entrance", 2938.68f, 799.3f, 352f), ("Chastain Enclave", 1801.54f, 681.5f, 19.2f), ("Stunt park", 3628f, 403.5f, 270f),
            ("Underground hub", 1870f, 1168.5f, 90f), ("Town Center", 265.11f, -1207.29f, 165.25f), ("KSU", -1858.74f, 987.44f, 185.3f),
            ("Viking Quarry J1", -1638.73f, -1893.3f, 345f), ("Airport", -3368.85f, -2166.78f, 269.7f), ("Kennesaw summit", -1122.98f, -5870.01f, 49.4f),
            ("Kennestone", 1667.84f, -6719.83f, 170.3f), ("Marietta Square", 1765.26f, -8416.87f, 89.6f),
        };

        IEnumerator R92Fps()
        {
            report.AppendLine(); report.AppendLine($"=== Item 0: load time and frame rate (Steam Deck setting {(SteamDeck.On ? "ON" : "off")}, {Screen.width} x {Screen.height})");
            report.AppendLine($"  load: the check's first frame {R20Started:0.0} s after the process started (Windows process start to the check runner)");
            string data = Path.Combine(Application.dataPath);
            try { foreach (var f in new DirectoryInfo(data).GetFiles().OrderByDescending(f => f.Length).Take(6)) report.AppendLine($"  data file {f.Name}: {f.Length / 1048576.0:0} MB"); } catch { }
            R92Memory("at load");
            yield return Clock(12f);
            if (race && race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            var all = new List<float>();
            foreach (var s in R92Spots)
            {
                var p = RaceRoute.Surface(s.x, s.z);
                var f = Quaternion.Euler(0f, s.yaw, 0f) * Vector3.forward;
                Place(p, f, 0f);
                var cc = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cc) cc.Snap();
                yield return new WaitForSecondsRealtime(3f);
                var ft = new List<float>(); float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; ft.Add(Time.unscaledDeltaTime); }
                all.AddRange(ft);
                ft.Sort(); float med = ft[ft.Count / 2], p99 = ft[Mathf.Min(ft.Count - 1, (int)(ft.Count * .99f))];
                report.AppendLine($"  {s.name,-24} {ft.Count / ft.Sum(),5:0} fps average, median {1f / med,4:0} fps, 99th percentile {p99 * 1000f,5:0.0} ms");
                yield return Capture("92_fps_" + s.name.Replace(' ', '_'));
            }
            R92Memory("after the 11 places");
            all.Sort();
            report.AppendLine($"  ALL PLACES {all.Count / all.Sum():0} fps average, median {1f / all[all.Count / 2]:0} fps, 99th percentile {all[Mathf.Min(all.Count - 1, (int)(all.Count * .99f))] * 1000f:0.0} ms");
            Flush();
        }

        void R92Memory(string when)
        {
            report.AppendLine($"  memory {when}: Unity reserved {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1073741824.0:0.00} GB, allocated {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1073741824.0:0.00} GB, mesh memory {MeshBytes() / 1073741824.0:0.00} GB");
        }
        static long MeshBytes() { long n = 0; foreach (var m in Resources.FindObjectsOfTypeAll<Mesh>()) n += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(m); return n; }


        [Serializable] class R92GlassJ { public int pond = -1, pool = -1; public float s0, s1, surface; }
        [Serializable] class R92Tube { public string id; public float d; public float[] pts; public R92GlassJ[] glass; }
        [Serializable] class R92Pool { public string id, tube, creek; public float[] at; public float surface; public float[] ring; }
        [Serializable] class R92Net { public R92Tube[] tubes; public R92Pool[] pools; }
        IEnumerator R92Glass()
        {
            report.AppendLine(); report.AppendLine("=== E: glass under the creeks (the stage-5 mains' creek pools)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var path = Path.Combine(Environment.CurrentDirectory, "Data/pipes/network.json");
            if (!File.Exists(path)) { Check("E: Data/pipes/network.json (run from the repo root)", false); yield break; }
            var net = JsonUtility.FromJson<R92Net>(File.ReadAllText(path));
            if (net.pools == null || net.pools.Length == 0) { Check("E: creek pools in network.json", false); yield break; }
            yield return Clock(12f); yield return Vehicle("original");
            var views = FindAnyObjectByType<CameraViews>(); int good = 0;
            for (int k = 0; k < net.pools.Length; k++)
            {
                var pool = net.pools[k]; var t = net.tubes.FirstOrDefault(x => x.id == pool.tube); var gl = t?.glass?.FirstOrDefault(g => g.pool == k);
                if (t == null || gl == null) { report.AppendLine($"  {pool.id}: no glass entry"); continue; }
                // the floor's line every meter; the glass's middle
                var line = new List<Vector3>(); for (int i = 0; i + 2 < t.pts.Length; i += 3) line.Add(new Vector3(t.pts[i], t.pts[i + 1], t.pts[i + 2]));
                float s = 0f; int mid = 0; float want = (gl.s0 + gl.s1) * .5f;
                for (int i = 1; i < line.Count; i++) { s += Vector2.Distance(new Vector2(line[i].x, line[i].z), new Vector2(line[i - 1].x, line[i - 1].z)); if (s >= want) { mid = i; break; } }
                var at = line[mid]; var fwd = line[Mathf.Min(mid + 3, line.Count - 1)] - line[Mathf.Max(mid - 3, 0)];
                Place(at + Vector3.up * .6f, fwd, 0f); yield return new WaitForSecondsRealtime(.6f);
                bool inPipe = DrainPipe.InsideAny(car.Body.position);
                float crown = at.y + t.d; bool under = crown < pool.surface;
                if (views) views.SetView(CameraViews.View.FirstPerson); yield return new WaitForSecondsRealtime(.3f);
                yield return Capture($"92_glass_{pool.id}_seat");
                if (views) views.SetView(CameraViews.View.Chase); var cc = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cc) cc.Snap();
                yield return new WaitForSecondsRealtime(.4f); yield return Capture($"92_glass_{pool.id}_chase");
                // from the bank: 6 m out from the pool's edge nearest the crossing's side, looking at the crossing
                var c = new Vector2(pool.at[0], pool.at[1]); Vector2 edge = c; float best = 1e9f;
                for (int i = 0; i + 1 < pool.ring.Length; i += 2) { var q = new Vector2(pool.ring[i], pool.ring[i + 1]); float dd = (q - c).sqrMagnitude; if (dd < best) { best = dd; edge = q; } }
                var o = (edge - c).normalized; var bank = edge + o * 8f;
                var bp = RaceRoute.Surface(bank.x, bank.y); Place(bp, new Vector3(-o.x, 0f, -o.y), 0f); if (cc) cc.Snap();
                yield return new WaitForSecondsRealtime(.8f); yield return Capture($"92_glass_{pool.id}_bank");
                bool ok = inPipe && under; if (ok) good++;
                report.AppendLine($"  {pool.id} ({pool.tube} under {pool.creek} at {pool.at[0]:0}, {pool.at[1]:0}): glass {gl.s1 - gl.s0:0} m; the car {(inPipe ? "in the pipe" : "NOT in a pipe")}; the crown {pool.surface - crown:0.0} m under the creek's surface; pictures 92_glass_{pool.id}_seat / _chase / _bank");
            }
            Check("E: every creek pool's glass stretch is a pipe under water", good == net.pools.Length, $"{good} of {net.pools.Length}");
        }
    }
}
