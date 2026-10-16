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
    // Round 20, checked in the exe (ChastainRush.exe -exeRound20 <dir> [-r20Only bj1,ramp,map,lap,fences,tramp,n5,n2,r1] [-r20Vehicles id,id]
    // -logFile <dir>\Player.log), muted like every check (CheckRun), its files in <dir>. One targeted check per piece:
    //   bj1   BUG-001: the Drifter Twin over the footbridge's near end at race speed, on Dan's line and 1 m either side; the
    //         slowest it goes there and what it touched
    //   lap   the whole Noonday Creek lap once on each trail vehicle: every dead stop (or near stop) and where
    //   ramp  BUG-002: every vehicle that comes through P1 off the outlet ramp over the pond: air, height, distance, on and on
    //   map   the map with the controller (a virtual gamepad), in free roam and in a race: open, zoom, pan, close
    //   fences, tramp, n5, n2, r1: the follow-ups (Round20Runner.Follow.cs)
    public sealed partial class Round20Runner : MonoBehaviour
    {
        public string outputDir;
        public string tag = "round20";   // Round 21: -exeRound21 runs this runner's Round 21 parts (Round20Runner.R21.cs), its report round21_check.txt
        readonly StringBuilder report = new StringBuilder();
        int fails, checks;
        ArcadeVehicle car; VehicleSwitch sw; VehicleRespawn respawn; RaceController race;
        static readonly string[] Small = { "atv", "moto", "scrambler", "drifter", "mower" };

        void Update() { var hud = DeveloperHud.Instance; if (hud && hud.MenuOpen) { hud.CloseMenu(); report.AppendLine("  (the debug menu was opened from outside; closed)"); } }

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); Flush(); }
        void Flush() { try { File.WriteAllText(Path.Combine(outputDir, tag + "_check.txt"), report.ToString()); } catch (Exception) { } }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"{(tag == "round35" ? "Round 35" : tag == "round34" ? "Round 34" : tag == "round33" ? "Round 33" : tag == "round26" ? "Round 26" : tag == "round21" ? "Round 21" : "Round 20")} check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}, muted.");
            yield return new WaitForSecondsRealtime(4f);
            car = FindAnyObjectByType<ArcadeVehicle>();
            foreach (var v in FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)) { var vi = v.GetComponent<VehicleInput>(); if (vi && vi.enabled) car = v; }   // Round 33: the player, not a traffic car (-traffic)
            sw = car.GetComponent<VehicleSwitch>(); respawn = car.GetComponent<VehicleRespawn>(); race = RaceController.Instance;
            var hud = DeveloperHud.Instance; if (hud) hud.Visible = false;
            string only = null; string[] ids = VehicleProfile.All.Select(p => p.Id).ToArray();
            var argv = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < argv.Length; i++) { if (argv[i] == "-r20Only" || argv[i] == "-r21Only" || argv[i] == "-r26Only" || argv[i] == "-r33Only" || argv[i] == "-r34Only" || argv[i] == "-r35Only") only = argv[i + 1]; if (argv[i] == "-r20Vehicles") ids = argv[i + 1].Split(','); }
            float minutes = 25f; for (int i = 0; i + 1 < argv.Length; i++) if (argv[i] == "-r20Minutes") float.TryParse(argv[i + 1], out minutes);
            StartCoroutine(Watchdog(minutes));   // a check run always ends (a stuck part must not leave the exe running)
            var parts = only == null ? null : only.Split(',');
            if (tag == "round21" && parts == null) parts = new[] { "r1", "six", "races", "revlap", "fwdlap", "revslow", "revrace" };   // Round 21's default parts
            if (tag == "round26" && parts == null) parts = new[] { "ramps", "express", "walls" };   // Round 26 (Round20Runner.R26.cs)
            if (tag == "round33" && parts == null) parts = new[] { "jump", "shortcuts", "hollow", "pics", "race" };   // Round 33 (Round20Runner.R33.cs)
            if (tag == "round34" && parts == null) parts = new[] { "lap", "bumps", "radio", "records" };   // Round 34 (Round20Runner.R34*.cs)
            bool Do(string p) => parts == null || parts.Contains(p);
            if (tag == "round35" && parts == null) parts = new[] { "line" };   // Round 35 (Round20Runner.R35*.cs)
            if (Do("bj1")) yield return Guard(Bj1());
            if (Do("ramp")) yield return Guard(Ramp(ids));
            if (Do("map")) yield return Guard(MapCheck());
            if (Do("lap") && tag != "round34") yield return Guard(Lap(ids));
            // the follow-ups (Round20Runner.Follow.cs)
            if (Do("fences")) yield return Guard(Fences());
            if (Do("tramp")) yield return Guard(Tramp());
            if (Do("n5")) yield return Guard(N5(ids));
            if (Do("n2")) yield return Guard(N2(ids));
            if (Do("r1")) yield return Guard(R1(ids));
            // Round 21 (Round20Runner.R21.cs)
            if (Do("six")) yield return Guard(Six(ids));
            if (Do("races")) yield return Guard(Races());
            // Round 21 Part B (Round20Runner.R21B.cs)
            if (Do("revlap")) yield return Guard(LapJumps("reverse", ids));
            if (Do("fwdlap")) yield return Guard(LapJumps("forward", ids));
            if (Do("revslow")) yield return Guard(RevSlow());
            if (Do("revrace")) yield return Guard(RevRace());
            if (Do("bj1diag")) yield return Guard(Bj1Diag());
            // the Round 21 follow-ups (Round20Runner.R21F.cs)
            if (Do("tube")) yield return Guard(Tube(ids));
            if (Do("ra")) yield return Guard(Ra(ids));
            if (Do("rc")) yield return Guard(Rc(ids));
            // Round 26 (Round20Runner.R26.cs)
            if (Do("ramps")) yield return Guard(Ramps());
            if (Do("express")) yield return Guard(Express());
            if (Do("walls")) yield return Guard(Walls());
            // Round 33 (Round20Runner.R33.cs): the Chastain Ridge race
            if (Do("jump")) yield return Guard(R33Jump(ids));
            if (Do("shortcuts")) yield return Guard(R33Shortcuts());
            if (Do("hollow")) yield return Guard(R33Hollow());
            if (Do("pics")) yield return Guard(R33Pics());
            if (Do("race")) yield return Guard(R33Race());
            // Round 34 (Round20Runner.R34*.cs)
            if (tag == "round34")
            {
                if (Do("lap")) yield return Guard(R34Lap());
                if (Do("bumps")) yield return Guard(R34Bumps());
                if (Do("radio")) yield return Guard(R34Radio());   // Part B (Round20Runner.R34B.cs)
                if (Do("records")) yield return Guard(R34Records());   // Part C (Round20Runner.R34C.cs)
            }
            // Round 35 (Round20Runner.R35*.cs)
            if (tag == "round35")
            {
                if (Do("line")) yield return Guard(R35Line());
            }
            report.AppendLine(); report.AppendLine($"{checks - fails} of {checks} checks passed.");
            Flush();
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        IEnumerator Watchdog(float minutes)
        {
            yield return new WaitForSecondsRealtime(minutes * 60f);
            Check($"the check finished within {minutes:0} minutes", false, "stopped by the watchdog");
            Time.timeScale = 1f; AudioListener.pause = false; Flush();
            Application.Quit(2);
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

        // ---------------------------------------------------------------- 1. BUG-001, the footbridge's near end

        // what the vehicle's body touched (a tap on the car's own object)
        sealed class Touches : MonoBehaviour
        {
            public readonly List<string> seen = new List<string>();
            public bool on;
            void OnCollisionEnter(Collision c) => Note(c, "enter");
            void OnCollisionStay(Collision c) => Note(c, "stay");
            void Note(Collision c, string how)
            {
                if (!on || c.contactCount == 0) return;
                var ct = c.GetContact(0); var p = ct.point;
                if (c.impulse.magnitude < 50f && ct.normal.y > 0.6f) return;   // resting on the ground
                seen.Add($"{how} {c.collider.name} at ({p.x:0.00}, {p.y:0.00}, {p.z:0.00}) normal ({ct.normal.x:0.00}, {ct.normal.y:0.00}, {ct.normal.z:0.00}) impulse {c.impulse.magnitude:0}");
            }
        }

        IEnumerator Bj1()
        {
            report.AppendLine(); report.AppendLine("=== 1. BUG-001: the footbridge's near end (BJ1), the Drifter Twin at race speed");
            UseRoute("noonday_creek.json", "forward");
            var j = BigJump.All.First(b => b.id == "BJ1");
            var nearEnd = j.lip - j.dir * 16.5f;
            float sNear = AbsStation(nearEnd);
            var tap = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>();
            var vids = new[] { "drifter", "moto", "scrambler", "atv", "mower" };
            var bad = new List<string>();
            foreach (var id in vids)
                foreach (float off in new[] { 0f, 1f, -1f, 2f, -2f, 2.5f, -2.5f })
                {
                    yield return Vehicle(id);
                    var line = LapSlice(sNear - 150f, sNear + 12f).Select(p => p + Vector3.Cross(Vector3.up, j.dir).normalized * off * Mathf.Clamp01((AbsStation(p) - (sNear - 40f)) / 20f)).ToList();
                    Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                    var run = new Run(); float vIn = -1f, vMin = 99f; tap.seen.Clear(); tap.on = false;
                    var drive = StartCoroutine(DriveLine(line, 99f, 30f, run, null, null, 4f));
                    while (!run.reached && run.end == "" && run.time < 29.9f)
                    {
                        float a = Vector3.Dot(Flat(car.Body.position - nearEnd), j.dir);
                        tap.on = a > -6f && a < 4f;
                        if (a > -8f && vIn < 0f) vIn = Flat(car.Body.linearVelocity).magnitude;
                        if (a > -6f && a < 4f) vMin = Mathf.Min(vMin, Flat(car.Body.linearVelocity).magnitude);
                        yield return new WaitForFixedUpdate();
                    }
                    yield return drive; tap.on = false;
                    bool ok = run.reached && vMin > vIn * 0.8f;
                    if (!ok) bad.Add($"{Nm(id)} {off:+0.0;-0.0;0} m");
                    report.AppendLine($"  {Nm(id),-16} {off,4:+0.0;-0.0;0} m off the line: {vIn:0.0} m/s 8 m before the deck, slowest {vMin:0.0} m/s from 6 m before to 4 m on it, {(run.reached ? "on" : "STOPPED " + run.end)}{(tap.seen.Count > 0 ? "; touched: " + string.Join("; ", tap.seen.Distinct().Take(4)) : "")}");
                }
            Check("BUG-001: every trail vehicle onto the footbridge at race speed, on the line and up to 2.5 m either side (the tread is 4.5 m wide), without a catch (keeps 80% of its speed)", bad.Count == 0, bad.Count == 0 ? "" : "caught: " + string.Join(", ", bad));
        }

        // ---------------------------------------------------------------- the whole lap, looking for dead stops

        IEnumerator Lap(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== The Noonday Creek lap on each trail vehicle, flat out: every dead stop or sudden loss of speed on the ground");
            UseRoute("noonday_creek.json", "forward");
            float L = race.Route.LapLength;
            var small = Small.Where(ids.Contains).ToArray(); if (small.Length == 0) small = Small;
            int stops = 0;
            foreach (var id in small)
            {
                yield return Vehicle(id);
                var line = LapSlice(race.Route.GateS, race.Route.GateS + L + 20f);
                Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                var run = new Run(); var hist = new Queue<(float t, float v, Vector3 p)>(); var found = new List<string>(); float lastAt = -99f; float t = 0f;
                var tap = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); tap.seen.Clear(); tap.on = true;
                var drive = StartCoroutine(DriveLine(line, 99f, 240f, run, null, null, 4f, false));
                while (!run.reached && run.end == "" && run.time < 239.9f)
                {
                    yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                    float v = Flat(car.Body.linearVelocity).magnitude; var p = car.Body.position;
                    if (tap.seen.Count > 40) tap.seen.RemoveRange(0, tap.seen.Count - 40);
                    hist.Enqueue((t, v, p)); while (hist.Count > 0 && hist.Peek().t < t - 0.5f) hist.Dequeue();
                    float vMax = hist.Max(h => h.v);
                    bool inPipe = DrainPipe.Inside(p, out _) != null;
                    if (car.GroundedWheels > 0 && vMax > 10f && v < vMax * 0.45f && t - lastAt > 3f && !inPipe)
                    {
                        lastAt = t; var at = hist.First(h => h.v == vMax).p;
                        var hits = tap.seen.Where(x => !x.Contains("Terrain") || !x.Contains("normal (0.0")).Reverse().Take(3).ToList();
                        found.Add($"{vMax:0.0} to {v:0.0} m/s in 0.5 s at ({p.x:0.0}, {p.z:0.0}), lap station {race.Route.LapStationOf(p):0} m (from ({at.x:0.0}, {at.z:0.0})); last contacts: {(hits.Count > 0 ? string.Join("; ", hits) : "none")}");
                    }
                }
                yield return drive; tap.on = false;
                stops += found.Count;
                report.AppendLine($"  {Nm(id),-16} {(run.reached ? $"the lap in {run.time:0.0} s" : "DID NOT FINISH " + run.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})")}, {found.Count} sudden stop(s){(found.Count > 0 ? ":" : "")}");
                foreach (var f in found) report.AppendLine("      " + f);
                if (!run.reached) stops++;
            }
            Check("the lap on each trail vehicle: no dead stop on the ground (none from over 10 m/s to under 45% of it within 0.5 s, pipes aside)", stops == 0, $"{stops} found (listed above)");
        }

        // ---------------------------------------------------------------- shared (as Round19Runner)

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
            float lastS = 0f;
            while (t < timeout)
            {
                yield return new WaitForFixedUpdate();
                float dt = Time.fixedDeltaTime; t += dt;
                var p = car.Body.position; float s = ProjectNear(line, cum, p, lastS, out float dev); lastS = s;
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
                if (stopped > 8f) { run.end = "stopped"; break; }
                if (((int)(t / 0.1f)) != ((int)((t - dt) / 0.1f))) run.trail.Add(p);
            }
            run.time = t;
            car.enabled = true;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static float Project(List<Vector3> line, float[] cum, Vector3 p, out float dev) => ProjectNear(line, cum, p, -1f, out dev);
        // nearest point on the line; with 'near' >= 0 only within 60 m of it along the line (a whole lap passes close to itself)
        static float ProjectNear(List<Vector3> line, float[] cum, Vector3 p, float near, out float dev)
        {
            float best = float.MaxValue, bs = 0f;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                if (near >= 0f && (cum[i + 1] < near - 60f || cum[i] > near + 60f)) continue;
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
        float AbsStation(Vector3 p) => race.Route.LapStationOf(p) + race.Route.GateS;
    }
}
