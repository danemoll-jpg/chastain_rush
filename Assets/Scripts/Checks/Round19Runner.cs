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
    // Round 19 items 1 to 6, checked in the exe (ChastainRush.exe -exeRound19 <dir> [-r19Only trail|jumps|bar|pipe|penalty|r3|r1|ai|race]
    // [-r19Vehicles id,id] -logFile <dir>\Player.log), muted like every check (CheckRun), its files in <dir>. One targeted check per piece:
    //   trail    no solid trunk within 3.5 m of the Noonday Creek line; a vehicle into a breakable tree snaps it and drives on
    //   jumps    each trail vehicle over each big jump from 130 m before it, flat out (lifting for bends): lip speed, air, distance,
    //            height, on its wheels after; one slow run per jump lands short on drivable ground; a picture of each in the air
    //   bar      P1's way in across the creek on the rock bar: no wading, the speed held over it
    //   pipe     every vehicle through P1; every vehicle riding the wall (and round, fast enough), back on its wheels at the bottom,
    //            out of the pipe; the camera stays in; a reset in the pipe; the stream there; pictures
    //   penalty  P1 never charges a penalty: through it, a reset in the creek at its mouth, a reset in the pipe
    //   r3       the party hint: pictures from 60 m and 30 m in the reverse direction, the sign breaks
    //   r1       each of the eleven over R1 (reverse loop) and on to the lap; one slow; the picture with the flight path
    //   ai       the AI's trials: each trail vehicle once through P1, each of the eleven once over R1
    //   race     Noonday Creek, 3 laps, three AI drivers: everyone finishes, recoveries, passes, penalties
    //   pass     the AI passing: a slowed driver ahead, a normal one behind; the one behind gets past
    public sealed partial class Round19Runner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        int fails, checks;
        ArcadeVehicle car; VehicleSwitch sw; VehicleRespawn respawn; RaceController race;
        static readonly string[] Small = { "atv", "moto", "scrambler", "drifter", "mower" };

        void Update() { var hud = DeveloperHud.Instance; if (hud && hud.MenuOpen) { hud.CloseMenu(); report.AppendLine("  (the debug menu was opened from outside; closed)"); } }

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); Flush(); }
        void Flush() { try { File.WriteAllText(Path.Combine(outputDir, "round19_check.txt"), report.ToString()); } catch (Exception) { } }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"Round 19 check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}, muted.");
            yield return new WaitForSecondsRealtime(4f);
            car = FindAnyObjectByType<ArcadeVehicle>(); sw = car.GetComponent<VehicleSwitch>(); respawn = car.GetComponent<VehicleRespawn>(); race = RaceController.Instance;
            var hud = DeveloperHud.Instance; if (hud) hud.Visible = false;
            string only = null; string[] ids = VehicleProfile.All.Select(p => p.Id).ToArray();
            var argv = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < argv.Length; i++) { if (argv[i] == "-r19Only") only = argv[i + 1]; if (argv[i] == "-r19Vehicles") ids = argv[i + 1].Split(','); }
            var parts = only == null ? null : only.Split(',');
            bool Do(string p) => parts == null || parts.Contains(p);
            if (Do("trail")) yield return Guard(Trail());
            if (Do("jumps")) yield return Guard(Jumps(ids));
            if (Do("bar")) yield return Guard(Bar());
            if (Do("pipe")) yield return Guard(Pipe(ids));
            if (Do("stream")) yield return Guard(StreamLook());
            if (Do("penalty")) yield return Guard(Penalty());
            if (Do("r3")) yield return Guard(R3Hint());
            if (Do("r1")) yield return Guard(R1(ids));
            if (Do("ai")) yield return Guard(Ai(ids));
            if (Do("race")) yield return Guard(Race());
            if (Do("pass")) yield return Guard(Pass());
            report.AppendLine(); report.AppendLine($"{checks - fails} of {checks} checks passed.");
            Flush();
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        IEnumerator Guard(IEnumerator part)
        {
            while (true)
            {
                object cur;
                try { if (!part.MoveNext()) yield break; cur = part.Current; }
                catch (Exception e) { Check("no exception", false, e.ToString()); yield break; }
                yield return cur;
            }
        }

        // ---------------------------------------------------------------- shared (as Round18Runner)

        void UseRoute(string file, string direction)
        {
            RaceController.RouteOverride = file; RaceController.DirectionOverride = direction; RaceController.LapsOverride = 0;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload();
        }

        IEnumerator Vehicle(string id) { if (sw) sw.Select(id); car.enabled = true; yield return new WaitForSecondsRealtime(0.4f); }
        static string Nm(string id) => VehicleProfile.Find(id).Name;

        void Place(Vector3 p, Vector3 fwd, float speed)
        {
            fwd.y = 0f; fwd.Normalize();
            respawn.PlaceAt(p + Vector3.up * 0.3f, Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg);
            car.Body.linearVelocity = fwd * speed;
            race.NotifyTeleported();
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }

        // a still picture from a camera placed by hand (the chase camera set aside for it)
        IEnumerator Still(string name, Vector3 at, Vector3 lookAt, float fov = 60f)
        {
            var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false;
            float f0 = cam.fieldOfView; cam.fieldOfView = fov;
            cam.transform.position = at; cam.transform.LookAt(lookAt);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture(name);
            cam.fieldOfView = f0; if (chase) { chase.enabled = was; chase.Snap(); }
        }

        public sealed class Run
        {
            public bool reached; public float time, maxDev, maxAir, minSpeed = 99f, speedAt = -1f; public int minWheels = 99;
            public Vector3 firstLanding = new Vector3(float.NaN, 0f, 0f); public string end = ""; public float upAtLanding = -1f;
            public List<Vector3> trail = new List<Vector3>();
        }

        IEnumerator DriveLine(List<Vector3> line, float cap, float timeout, Run run, Vector3? markAt = null, Func<Run, bool> stop = null, float aimAhead = 5f, bool giveUp = true, float[] profile = null)
        {
            car.enabled = false;
            var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
            float total = cum[cum.Length - 1], t = 0f, air = 0f, stopped = 0f;
            float markS = markAt.HasValue ? Project(line, cum, markAt.Value, out _) : -1f; bool marked = false;
            while (t < timeout)
            {
                yield return new WaitForFixedUpdate();
                float dt = Time.fixedDeltaTime; t += dt;
                var p = car.Body.position; float s = Project(line, cum, p, out float dev);
                if (t > 0.6f) { run.maxDev = Mathf.Max(run.maxDev, dev); run.minWheels = Mathf.Min(run.minWheels, car.GroundedWheels); }
                if (!marked && markS >= 0f && s >= markS) { marked = true; run.speedAt = Flat(car.Body.linearVelocity).magnitude; }
                if (s >= total - 1.5f) { run.reached = true; break; }
                if (stop != null && stop(run)) break;
                var here = At(line, cum, s); var fl = Flat(At(line, cum, s + 0.5f) - here).normalized;
                var aim = At(line, cum, s + aimAhead + 0.12f * Mathf.Max(0f, car.ForwardSpeed));
                float cte = Vector3.Dot(Flat(p - here), Vector3.Cross(Vector3.up, fl));
                float head = Vector3.SignedAngle(Flat(car.transform.forward).normalized, Flat(aim - here).normalized, Vector3.up);
                float corr = -Mathf.Atan2(1.6f * cte, Mathf.Max(car.ForwardSpeed, 1f) + 2f) * Mathf.Rad2Deg;
                float steer = Mathf.Clamp((head + corr) / 18f, -1f, 1f);
                float want = cap;
                for (float d = 4f; d <= 40f; d += 4f)
                {
                    Vector3 a = At(line, cum, s + d - 4f), b = At(line, cum, s + d), c = At(line, cum, s + d + 4f);
                    float area = Mathf.Abs(Vector3.Cross(Flat(b - a), Flat(c - a)).y); if (area < 1e-3f) continue;
                    float r = Flat(b - a).magnitude * Flat(c - b).magnitude * Flat(c - a).magnitude / (2f * area);
                    want = Mathf.Min(want, Mathf.Sqrt(Mathf.Pow(Mathf.Sqrt(0.8f * car.maxGripAcceleration * r), 2f) + 2f * 0.8f * car.braking * Mathf.Max(0f, d - 6f)));
                }
                if (profile != null)
                    for (int i = 0; i + 1 < profile.Length; i += 2)
                    {
                        if (s <= profile[i]) want = Mathf.Min(want, Mathf.Sqrt(profile[i + 1] * profile[i + 1] + 2f * 0.8f * car.braking * (profile[i] - s)));
                        else if (i + 3 < profile.Length && s < profile[i + 2]) want = Mathf.Min(want, Mathf.Lerp(profile[i + 1], profile[i + 3], Mathf.InverseLerp(profile[i], profile[i + 2], s)));
                    }
                float v = car.ForwardSpeed;
                if (car.GroundedWheels == 0) steer = 0f;
                car.Simulate(v < want ? 1f : 0f, v > want + 1.5f ? 1f : 0f, steer, dt);
                if (car.GroundedWheels == 0) air += dt; else { if (air > 0.3f && float.IsNaN(run.firstLanding.x)) { run.firstLanding = p; run.upAtLanding = car.transform.up.y; } air = 0f; }
                run.maxAir = Mathf.Max(run.maxAir, air);
                if (t > 0.5f) run.minSpeed = Mathf.Min(run.minSpeed, Flat(car.Body.linearVelocity).magnitude);
                stopped = car.Body.linearVelocity.magnitude < 1f ? stopped + dt : 0f;
                if (stopped > 4f && giveUp) { run.end = "stopped"; break; }
                if (((int)(t / 0.1f)) != ((int)((t - dt) / 0.1f))) run.trail.Add(p);
            }
            run.time = t;
            car.enabled = true;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static float Project(List<Vector3> line, float[] cum, Vector3 p, out float dev)
        {
            float best = float.MaxValue, bs = 0f;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                Vector3 a = Flat(line[i]), ab = Flat(line[i + 1] - line[i]), q = Flat(p);
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(q - a, ab) / ab.sqrMagnitude);
                float d = (a + ab * t - q).sqrMagnitude;
                if (d < best) { best = d; bs = cum[i] + t * (cum[i + 1] - cum[i]); }
            }
            dev = Mathf.Sqrt(best); return bs;
        }
        static Vector3 At(List<Vector3> line, float[] cum, float s)
        {
            s = Mathf.Clamp(s, 0f, cum[cum.Length - 1]);
            int i = 0; while (i + 2 < cum.Length && cum[i + 1] < s) i++;
            return Vector3.Lerp(line[i], line[i + 1], Mathf.InverseLerp(cum[i], cum[i + 1], s));
        }
        // the lap from absolute station s0 to s1 (the lap's own stations, from its start; RaceRoute.PointAt)
        List<Vector3> LapSlice(float s0, float s1, float step = 2f) { var l = new List<Vector3>(); for (float s = s0; s <= s1; s += step) l.Add(race.Route.PointAt(s, out _)); return l; }
        float AbsStation(Vector3 p) => race.Route.LapStationOf(p) + race.Route.GateS;
        static List<Vector3> Cat(params IEnumerable<Vector3>[] parts) { var l = new List<Vector3>(); foreach (var p in parts) l.AddRange(p); return l; }
    }
}
