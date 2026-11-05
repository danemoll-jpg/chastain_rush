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
    //   f92car / f92plane / f92ai / f92pics  F: Wings & Wheels: the human-like driver (a lap by the roads, a lap through the tube), the human-like
    //            pilot (2 laps of rings), the AI fields (all cars, all planes, mixed) on each difficulty (-r92Diffs 0,1,2 -r92Laps 3), pictures
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

        // ---------------------------------------------------------------- F: Wings & Wheels
        const string R92Car = "wings_and_wheels.json", R92Air = "wings_and_wheels_air.json";
        string R92Arg(string name, string def) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : def; }

        // the human-like driver in a car, solo: one lap by the roads, one lap through the tube (the turn-off, the pipes, the express, out of the manhole)
        IEnumerator R92CarLaps()
        {
            report.AppendLine(); report.AppendLine("=== F. Wings & Wheels as a car: the human-like driver (the Street Classic), solo, 2 laps: lap 1 by the roads, lap 2 through THE TUBE");
            yield return Clock(13f); yield return Vehicle(R92Arg("-r92Car", "original"));
            yield return StartRoute(R92Car, "forward", 0, 2);
            var r = race.Route; if (r == null || r.Air) { Check("F car: the car's lap loads", false); yield break; }
            var sc = r.Shortcuts.FirstOrDefault(); if (sc == null) { Check("F car: THE TUBE is an official shortcut", false); yield break; }
            float sOff = r.LapStationOf(sc.Start), sOn = r.LapStationOf(sc.End);
            var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]);
            for (float s = 0f; s < r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));   // lap 1: the roads
            for (float s = 0f; s < sOff; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));          // lap 2: to the turn-off
            line.AddRange(sc.Path);                                                                  //        the tube
            for (float s = sOn + 3f; s < r.LapLength + 30f; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
            var run = new HumanRun(); int rides0 = PipeExpress.Rides; float lap1 = -1f;
            yield return HumanDrive(line, 45f, 900f, run, () => { if (lap1 < 0f && race.Player.CompletedLaps >= 1) lap1 = race.Player.LapTimes[0]; return race.Player.Finished; });
            var p = race.Player;
            report.AppendLine($"  laps {string.Join(" / ", p.LapTimes.Select(v => RaceController.Fmt(v)))}; shortcuts {string.Join(", ", p.ShortcutsTaken)}; missed {p.MissedTotal}; express rides {PipeExpress.Rides - rides0}; resets {run.resets}, stuck {run.stuck}, top {run.topSpeed:0} m/s");
            report.AppendLine($"  the car at the end ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}), in a pipe {DrainPipe.InsideAny(car.Body.position)}{run.note}"); report.AppendLine($"  trace:{run.trace}");
            r92CarRoad = p.LapTimes.Count > 0 ? p.LapTimes[0] : -1f; r92CarTube = p.LapTimes.Count > 1 ? p.LapTimes[1] : -1f;
            Check("F car: the human-like driver finishes both laps", p.Finished, $"laps {p.CompletedLaps}");
            Check("F car: lap 2 takes THE TUBE (the express carries it) with no checkpoint missed", p.ShortcutsTaken.Count >= 1 && PipeExpress.Rides > rides0 && p.MissedTotal == 0, $"shortcuts {p.ShortcutsTaken.Count}, rides {PipeExpress.Rides - rides0}, missed {p.MissedTotal}");
            race.EndRace(); Flush();
        }
        float r92CarRoad = -1f, r92CarTube = -1f, r92Plane = -1f;

        // the human-like pilot in a plane, solo: 2 laps of the rings (the air start, the low frames, the slow rings)
        IEnumerator R92PlaneLaps()
        {
            report.AppendLine(); report.AppendLine("=== F. Wings & Wheels as a plane: the human-like pilot (the Sky Hopper), solo, 2 laps");
            yield return Clock(13f); yield return Vehicle(R92Arg("-r92Plane", "plane"));
            RaceController.RouteOverride = R92Air; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 2; RaceController.AiCount = 0;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); var r = race.Route; if (r == null || !r.Air) { Check("F plane: the plane's lap loads", false); yield break; }
            int slow0 = RaceController.SlowRingTooFast;
            race.StartRace(); R73Pad();
            while (race.State == RaceController.Stage.Countdown) { R73Set(0, 0, 0); yield return null; }
            var note = new System.Text.StringBuilder();
            yield return R80HumanFly(600f, 1f, note);
            var p = race.Player; var lows = r.Checkpoints.Where(g => g.RingW > 0f).Select(g => g.Id).ToList(); var slows = r.Checkpoints.Where(g => g.MaxSpeed > 0f).Select(g => g.Id).ToList();
            report.AppendLine($"  laps {string.Join(" / ", p.LapTimes.Select(v => RaceController.Fmt(v)))}; missed {p.MissedTotal} ({string.Join(", ", p.Missed.Select(k => k + 1))}); low frames {string.Join(", ", lows)}, slow rings {string.Join(", ", slows)}; too fast at a slow ring {RaceController.SlowRingTooFast - slow0} times;{note}");
            r92Plane = p.LapTimes.Count > 0 ? p.LapTimes.Min() : -1f;
            Check("F plane: the human-like pilot finishes both laps", p.Finished, $"laps {p.CompletedLaps}");
            Check("F plane: every low frame and slow ring taken (none missed)", p.Finished && p.MissedTotal == 0, $"missed {p.MissedTotal}");
            race.EndRace(); Flush();
        }

        // the AI fields: all cars, all planes, mixed; the player parked off the course (solo the AI race), 3 laps each, on each difficulty asked
        IEnumerator R92AiRaces()
        {
            var diffs = R92Arg("-r92Diffs", "0,1,2").Split(',').Select(int.Parse).ToArray(); int laps = int.Parse(R92Arg("-r92Laps", "3"));
            report.AppendLine(); report.AppendLine($"=== F. Wings & Wheels, the AI fields ({laps} laps, 3 AI; the player parked off the course): all cars, all planes, mixed");
            yield return Clock(13f); int prev = RoadDriver.Difficulty;
            var fields = new (string name, string[] ids)[] { ("all cars", new[] { "original", "drifter", "skyfin" }), ("all planes", new[] { "plane", "dart", "plane" }), ("mixed", new[] { "original", "plane", "drifter" }) };
            var table = new System.Text.StringBuilder();
            foreach (int diff in diffs)
                foreach (var f in fields)
                {
                    RoadDriver.Difficulty = diff; RaceController.AiVehiclesOverride = f.ids;
                    yield return Vehicle("original");
                    yield return StartRoute(R92Car, "forward", 3, laps);
                    respawn.PlaceAt(RaceRoute.Surface(1560f, -7300f) + Vector3.up * .4f, 0f); race.NotifyTeleported();   // off the course, between Church and Cherokee
                    var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
                    Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                    float end = Time.realtimeSinceStartup + 8f * 60f;
                    while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished) && !RaceStuck) yield return new WaitForSecondsRealtime(.5f);
                    Time.timeScale = 1f;
                    string dn = diff == 2 ? "Hard" : diff == 1 ? "Normal" : "Easy";
                    report.AppendLine($"  {dn}, {f.name}: " + string.Join("; ", ai.Select(e => $"{e.Name} {e.VehicleName} ({(e.Flies ? "plane" : "car")}) {(e.Finished && !e.Estimated ? RaceController.Fmt(e.FinalTotal) : "NOT finished")}, laps {string.Join(" / ", e.LapTimes.Select(v => RaceController.Fmt(v)))}, missed {e.MissedTotal}, shortcuts {e.ShortcutsTaken.Count}")));
                    foreach (var e in ai.Where(x => x.Finished && !x.Estimated && x.LapTimes.Count > 0)) table.AppendLine($"    {dn}\t{(e.Flies ? "plane" : "car")}\t{e.VehicleName}\tbest lap {RaceController.Fmt(e.LapTimes.Min())}\taverage {RaceController.Fmt(e.LapTimes.Average())}{(e.ShortcutsTaken.Count > 0 ? "\t(tube " + e.ShortcutsTaken.Count + ")" : "")}");
                    Check($"F AI {dn} {f.name}: every AI finishes", ai.All(e => e.Finished && !e.Estimated), string.Join(", ", ai.Where(e => !e.Finished || e.Estimated).Select(e => e.Name)));
                    race.EndRace(); Flush(); yield return new WaitForSecondsRealtime(.5f);
                }
            RaceController.AiVehiclesOverride = null; RoadDriver.Difficulty = prev;
            report.AppendLine("  lap times by kind:"); report.Append(table);
            if (r92CarTube > 0f && r92Plane > 0f) report.AppendLine($"  the human-like player: car by the roads {RaceController.Fmt(r92CarRoad)}, car through the tube {RaceController.Fmt(r92CarTube)}, plane {RaceController.Fmt(r92Plane)}: the plane {100f * (r92Plane / r92CarTube - 1f):+0;-0} % against the car's tube lap");
        }

        // a picture from the player's seat at the turn-off, inside the tube, at the pop-out, at each low frame and each slow ring
        IEnumerator R92Pics()
        {
            report.AppendLine(); report.AppendLine("=== F. Pictures: the tube's turn-off, inside it, the pop-out; the low frames and slow rings from the plane");
            yield return Clock(13f); yield return Vehicle("original");
            yield return StartRoute(R92Car, "forward", 0, 1);
            var r = race.Route; var sc = r.Shortcuts.FirstOrDefault(); if (sc == null) yield break;
            var cc = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null;
            foreach (var (name, k) in new[] { ("92_tube_turnoff", 0), ("92_tube_mouth", sc.Path.Length / 9), ("92_tube_inside", sc.Path.Length / 2), ("92_tube_popout", sc.Path.Length - 8) })
            {
                var a = sc.Path[Mathf.Clamp(k, 0, sc.Path.Length - 2)]; var b = sc.Path[Mathf.Clamp(k + 2, 1, sc.Path.Length - 1)];
                Place(a + Vector3.up * .5f, b - a, 0f); if (cc) cc.Snap(); yield return new WaitForSecondsRealtime(.8f); yield return Capture(name);
            }
            foreach (var (nm, at2) in new[] { ("92_tube_stall_b41", new Vector2(1349f, -6470f)), ("92_tube_stall_j25", new Vector2(1484f, -6588f)) })
            {
                int bi = 0; float bd = 1e9f; for (int i = 0; i < sc.Path.Length; i++) { float dd = (new Vector2(sc.Path[i].x, sc.Path[i].z) - at2).sqrMagnitude; if (dd < bd) { bd = dd; bi = i; } }
                int i0 = Mathf.Max(0, bi - 6); var a0 = sc.Path[i0]; var b0 = sc.Path[Mathf.Min(sc.Path.Length - 1, i0 + 2)];
                Place(a0 + Vector3.up * .5f, b0 - a0, 0f); if (cc) cc.Snap(); yield return new WaitForSecondsRealtime(.8f); yield return Capture(nm);
                report.AppendLine($"  {nm}: path point {bi} ({sc.Path[bi].x:0.0}, {sc.Path[bi].y:0.0}, {sc.Path[bi].z:0.0}); the car placed at ({a0.x:0.0}, {a0.y:0.0}, {a0.z:0.0}), in a pipe {DrainPipe.InsideAny(car.Body.position)}");
            }
            Place(RaceRoute.Surface(1440f, -6526f) + Vector3.up * .5f, Vector3.right, 0f); if (cc) cc.Snap(); yield return new WaitForSecondsRealtime(.8f); yield return Capture("92_tower_road_east");
            race.EndRace();
            yield return Vehicle("plane");
            RaceController.RouteOverride = R92Air; RaceController.LapsOverride = 1; RaceController.AiCount = 0; race.Reload(); var ra = race.Route;
            foreach (var g in ra.Checkpoints.Where(g => g.RingW > 0f || g.MaxSpeed > 0f))
            {
                var at = g.Position - g.Forward * 120f; var pl = car.GetComponent<PlaneFlight>(); if (pl) pl.AirStart(at, Mathf.Atan2(g.Forward.x, g.Forward.z) * Mathf.Rad2Deg, 0f, 40f);
                if (cc) cc.Snap(); yield return new WaitForSecondsRealtime(.3f); yield return Capture($"92_ring_{g.Id}_{(g.RingW > 0f ? "low" : "slow")}");
            }
            yield return Vehicle("original");
        }
    }
}
