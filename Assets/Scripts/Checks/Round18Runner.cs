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
    // Round 18 items 3 to 9, checked in the exe (ChastainRush.exe -exeRound18 <dir> [-r18Only j4|pipe|r3|ai|race|pics]
    // [-r18Vehicles id,id]), muted like every check (CheckRun), its files in <dir>. One targeted check per piece:
    //   j4    each of the eleven vehicles driven down the trail from J3's landing to J4 flat out (lifting only for bends, as a
    //         driver would): its speed at the lip, where it comes down, whether it clears New Chastain Road; then one slow
    //         (14 m/s): it lands on the road and drives on down the landing slope to the south loop
    //   pipe  each of the eleven along P1's line from the trail: through the inlet cut, the pipe and the outlet; the camera stays
    //         in the pipe; a reset inside it puts the vehicle on its floor facing along it; the terrain holes are open
    //   r3    each of the eleven along R3's line: up the slide, onto the trampoline, over 1752, on its wheels on the lane
    //         pointing down it; one slow (8 m/s): no big bounce, it can drive away
    //   ai    the AI drivers: each small vehicle once through P1 (Noonday Creek); each of the eleven once over R3 (the Chastain
    //         Glen loop in reverse). R1 was not built (it does not fit as marked: TODO.md)
    //   pics  (only when asked for) the two R3 pictures for Dan
    //   race  Noonday Creek, 3 laps, three AI drivers and the player driven by script: everyone finishes, nobody stuck, passes,
    //         how long any AI drove right behind another, the speed held in the creek bed under the bridges, the frame rate
    public sealed partial class Round18Runner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        int fails, checks;
        ArcadeVehicle car; VehicleSwitch sw; VehicleRespawn respawn; RaceController race;

        // the debug menu (F6 / \ / a pad's Start) holds the game; a check never wants it open (a key from outside reaches it)
        void Update() { var hud = DeveloperHud.Instance; if (hud && hud.MenuOpen) { hud.CloseMenu(); report.AppendLine("  (the debug menu was opened from outside; closed)"); } }

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); Flush(); }
        void Flush() { try { File.WriteAllText(Path.Combine(outputDir, "round18_check.txt"), report.ToString()); } catch (Exception) { } }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"Round 18 check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}, muted.");
            yield return new WaitForSecondsRealtime(4f);
            car = FindAnyObjectByType<ArcadeVehicle>(); sw = car.GetComponent<VehicleSwitch>(); respawn = car.GetComponent<VehicleRespawn>(); race = RaceController.Instance;
            var hud = DeveloperHud.Instance; if (hud) hud.Visible = false;
            string only = null; string[] ids = VehicleProfile.All.Select(p => p.Id).ToArray();
            var argv = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < argv.Length; i++) { if (argv[i] == "-r18Only") only = argv[i + 1]; if (argv[i] == "-r18Vehicles") ids = argv[i + 1].Split(','); }
            report.AppendLine($"Routes in the scene: {string.Join(", ", race.Routes().Select(r => $"{r.name} ({r.file})"))}");
            if (only == "pics") yield return Guard(Pics());   // the pictures only when asked for
            if (only == null || only == "j4") yield return Guard(J4(ids));
            if (only == null || only == "pipe") yield return Guard(Pipe(ids));
            if (only == null || only == "r3") yield return Guard(R3(ids));
            if (only == null || only == "ai") yield return Guard(Ai(ids));
            if (only == null || only == "race") yield return Guard(Race());
            report.AppendLine(); report.AppendLine($"{checks - fails} of {checks} checks passed.");
            Flush();
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        // a part that throws is reported, not fatal to the others
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

        // ---------------------------------------------------------------- shared

        void UseRoute(string file, string direction)
        {
            RaceController.RouteOverride = file; RaceController.DirectionOverride = direction; RaceController.LapsOverride = 0;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload();
        }

        IEnumerator Vehicle(string id) { if (sw) sw.Select(id); car.enabled = true; yield return new WaitForSecondsRealtime(0.4f); }

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

        public sealed class Run
        {
            public bool reached; public float time, maxDev, maxAir, minSpeed = 99f, speedAt = -1f; public int minWheels = 99;
            public Vector3 firstLanding = new Vector3(float.NaN, 0f, 0f); public string end = "";
            public List<Vector3> trail = new List<Vector3>();
        }

        // drives the player's vehicle along a line: steering at a point a few meters ahead, the throttle to 'cap' but no faster
        // than the bends ahead allow (sqrt(0.8 x grip x radius)), as a driver would; 'markAt' records the speed when it passes
        // that point; a drive gives up after 'timeout' s or 4 s stopped
        IEnumerator DriveLine(List<Vector3> line, float cap, float timeout, Run run, Vector3? markAt = null, Func<Run, bool> stop = null, float aimAhead = 5f, bool giveUp = true, float[] profile = null)
        {
            car.enabled = false;
            var cum = new float[line.Count]; for (int i = 1; i < line.Count; i++) cum[i] = cum[i - 1] + Flat(line[i] - line[i - 1]).magnitude;
            float total = cum[cum.Length - 1], t = 0f, air = 0f, stopped = 0f, lastAirborne = 0f;
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
                // AiShortcut.Steer's law: the heading to a point a few meters ahead on the line plus a cross-track term
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
                if (profile != null)   // the line's own speeds (R3: the AI's profile), as AiShortcut.SpeedAt
                    for (int i = 0; i + 1 < profile.Length; i += 2)
                    {
                        if (s <= profile[i]) want = Mathf.Min(want, Mathf.Sqrt(profile[i + 1] * profile[i + 1] + 2f * 0.8f * car.braking * (profile[i] - s)));
                        else if (i + 3 < profile.Length && s < profile[i + 2]) want = Mathf.Min(want, Mathf.Lerp(profile[i + 1], profile[i + 3], Mathf.InverseLerp(profile[i], profile[i + 2], s)));
                    }
                float v = car.ForwardSpeed;
                car.Simulate(v < want ? 1f : 0f, v > want + 1.5f ? 1f : 0f, steer, dt);
                if (car.GroundedWheels == 0) { air += dt; lastAirborne = t; } else { if (air > 0.3f && float.IsNaN(run.firstLanding.x)) run.firstLanding = p; air = 0f; }
                run.maxAir = Mathf.Max(run.maxAir, air);
                if (t > 0.5f) run.minSpeed = Mathf.Min(run.minSpeed, Flat(car.Body.linearVelocity).magnitude);
                stopped = car.Body.linearVelocity.magnitude < 1f ? stopped + dt : 0f;
                if (stopped > 4f && giveUp) { run.end = "stopped"; break; }   // the race's player never gives up: it resets and drives on
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
        List<Vector3> LapSlice(float s0, float s1, float step = 2f) { var l = new List<Vector3>(); for (float s = s0; s <= s1; s += step) l.Add(race.Route.PointAt(s, out _)); return l; }

        // the route file's check fields (make_creek_track.py): J4's lip and direction, the creek-bed run's stations
        [Serializable] class J4J { public float[] lip, dir; public float s_lip, road_far_edge_m; }
        [Serializable] class CreekExtra { public J4J j4; public float[] creek_bed_s; }
        CreekExtra Extra() { int k = Array.IndexOf(race.routeFileNames, "noonday_creek.json"); return JsonUtility.FromJson<CreekExtra>(race.embeddedRouteJsons[k]); }
    }
}
