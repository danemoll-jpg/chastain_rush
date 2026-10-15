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
    // Round 28 check in the exe: ChastainRush.exe -exeRound28 <dir> [-r28Only race,shortcuts,resets,bikes,camera] [-r28Laps n] [-vehicle id]
    // -logFile <dir>\Player.log. Muted like every check (CheckRun), its files in <dir>.
    //   race       (B1) the Underground race, 3 AI drivers that take every open shortcut and every switch's detour (RoadDriver.CheckTakeAll):
    //              who opens each switch, the shortcuts each AI took, finishes, missed checkpoints, AI resets; the player on the grid
    //   shortcuts  (B1) the player's vehicle driven by script along S1, S2 and S3: first S1 with its gate shut (it must stop it), then
    //              each switch's plate (it must open the gate), then each shortcut to its end
    //   resets     (B4) 20 places evenly round the lap (pipes, glass, junctions, ledges, the span): the vehicle put there facing backward,
    //              then a reset as R / Y does it: it must face the way the race goes (within 20 degrees), stand on its wheels, on the route
    //   bikes      (B5) the vehicle (-vehicle drifter) driven round the whole lap by script at race pace, its heading against the lap's
    //              logged every physics step (bikes_heading.tsv); every spell turned more than 30 degrees across, with what it was doing
    //   camera     (B6) first person chosen; through the bike's lap, a reset, the finish and an express ride in free roam: the view must
    //              stay first person and the camera at the rider's eyes (never the chase camera's place)
    public sealed class Round28Runner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        int fails, checks;
        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); }
        static string Arg(string name) { var a = Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < a.Length; i++) if (a[i] == name) return a[i + 1]; return null; }
        static bool Only(string part) { var o = Arg("-r28Only"); return o == null || Array.IndexOf(o.Split(','), part) >= 0; }
        RaceController race; ArcadeVehicle car; VehicleRespawn respawn; CameraViews views; Camera cam;
        readonly List<float> frames = new List<float>(); bool measuring;
        void Update() { if (measuring) frames.Add(Time.unscaledDeltaTime); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"Round 28 check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}, muted (AudioListener.volume {AudioListener.volume:0}).");
            yield return new WaitForSecondsRealtime(3f);
            race = RaceController.Instance; car = FindAnyObjectByType<ArcadeVehicle>(); respawn = car ? car.GetComponent<VehicleRespawn>() : null;
            cam = Camera.main; views = cam ? cam.GetComponent<CameraViews>() : null;
            if (!race || !car || !respawn) { Check("a race controller and the player's vehicle", false); }
            else
            {
                report.AppendLine($"vehicle {car.GetComponent<VehicleConfiguration>()?.profileId}");
                if (Only("race")) yield return RaceAi();
                if (Only("shortcuts")) yield return Shortcuts();
                if (Only("resets")) yield return Resets();
                if (Only("bikes") || Only("camera")) yield return BikeLap();
                if (Only("bikewall")) yield return BikeWall();
                if (Only("camera")) yield return Express();
            }
            report.AppendLine();
            report.AppendLine(fails == 0 ? $"ALL {checks} CHECKS PASSED" : $"{fails} OF {checks} CHECKS FAILED");
            File.WriteAllText(Path.Combine(outputDir, "round28.txt"), report.ToString());
            Debug.Log("[Round28]\n" + report);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        IEnumerator StartUnderground(int ai, int laps)
        {
            RaceController.RouteOverride = "underground.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = laps;
            RaceController.AiCount = ai; RaceController.AiVehiclesOverride = null;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload();
            race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG()); Destroy(tex);
        }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        // ---------------------------------------------------------------- B1: the AI race
        IEnumerator RaceAi()
        {
            report.AppendLine("== B1 the Underground race, 3 AI taking every open shortcut and every switch's detour");
            int laps = int.TryParse(Arg("-r28Laps"), out var l) ? l : 3;
            RoadDriver.CheckTakeAll = true;
            yield return StartUnderground(3, laps);
            var gates = race.Barriers ? race.Barriers.Gates : new List<RaceBarriers.SwitchGate>();
            Check("the race layer: 10 seals, 2 railings, 3 switch gates S1 to S3", race.Barriers && race.Barriers.Seals == 10 && race.Barriers.Rails == 2 && gates.Count == 3,
                  race.Barriers ? $"{race.Barriers.Seals} seals, {race.Barriers.Rails} rails, gates {string.Join(", ", gates.Select(g => g.Data.id))}" : "none");
            Check("the route: 3 shortcuts and 3 switch detours", race.Route.Shortcuts.Count == 6, string.Join(", ", race.Route.Shortcuts.Select(s => s.Id)));
            var gp = race.Route.GridPosition;
            Check("the grid is off the lap (the player there is in nobody's way)", DistToLap(gp) > 6f, $"grid ({gp.x:0.0}, {gp.y:0.0}, {gp.z:0.0}), {DistToLap(gp):0.0} m from the lap");
            yield return Shot("b1_grid");
            var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
            frames.Clear(); measuring = true;
            float t0 = Time.time, end = t0 + 12f * 60f; int shots = 0; float nextShot = t0 + 20f;
            var shotGate = new HashSet<string>();
            while (Time.time < end && ai.Any(e => !e.Finished))
            {
                yield return new WaitForSecondsRealtime(0.25f);
                foreach (var g in gates) if (g.Open && !shotGate.Contains(g.Data.id) && Time.time > g.OpenedAt + 1.2f) { shotGate.Add(g.Data.id); }
                if (Time.time >= nextShot && shots < 4) { nextShot = Time.time + 45f; shots++; yield return Shot($"b1_race_{shots}"); }
            }
            measuring = false;
            foreach (var g in gates) Check($"{g.Data.label}: a racer over its plate opened it", g.Open, g.Open ? $"by {g.OpenedBy} at {g.OpenedAt - t0:0} s" : "never opened");
            var took = ai.SelectMany(e => e.ShortcutsTaken.Select(x => x.Split(' ').Last())).ToList();
            foreach (var sid in new[] { "S1", "S2", "S3" }) Check($"the AI took {sid} once it was open", took.Contains(sid), $"{took.Count(x => x == sid)} times");
            foreach (var e in ai)
            {
                var drv = e.Driver;
                Check($"{e.Name} ({e.VehicleName}) finishes the {laps} laps with no missed checkpoint", e.Finished && e.MissedTotal == 0,
                      $"laps {e.CompletedLaps}, {(e.Finished ? $"total {e.FinalTotal:0.0} s" : "not finished")}, missed {e.MissedTotal}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, shortcuts [{string.Join(", ", e.ShortcutsTaken)}], AI resets {(drv ? drv.RecoveryCount : -1)}");
            }
            if (frames.Count > 30)
            {
                float sum = frames.Sum(); var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
                report.AppendLine($"INFO  frame rate during the race (the camera on the grid in the hub): {frames.Count / sum:0} fps average, slowest 1 % {1f / p99:0} fps ({Screen.width} x {Screen.height}, vSync {QualitySettings.vSyncCount}; not measured at 3840 x 2160)");
            }
            RoadDriver.CheckTakeAll = false;
            race.EndRace();
            yield return new WaitForSecondsRealtime(1f);
        }

        float DistToLap(Vector3 p)
        {
            var path = race.Route.Path; float best = float.MaxValue;
            for (int i = 0; i + 1 < path.Length; i++)
            {
                Vector3 a = path[i], b = path[i + 1], ab = Flat(b - a); float L2 = ab.sqrMagnitude;
                float t = L2 < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(Flat(p - a), ab) / L2); var q = Vector3.Lerp(a, b, t);
                best = Mathf.Min(best, Flat(q - p).sqrMagnitude + 4f * (q.y - p.y) * (q.y - p.y));
            }
            return Mathf.Sqrt(best);
        }

        // ---------------------------------------------------------------- B1: the shortcuts by the player
        IEnumerator Shortcuts()
        {
            report.AppendLine("== B1 the shortcuts, the player's vehicle driven along each by script");
            yield return StartUnderground(0, 9);
            var sc = race.Route.Shortcuts.ToDictionary(s => s.Id);
            var gates = race.Barriers.Gates.ToDictionary(g => g.Data.shortcut);
            // S1 with its gate shut: the gate (a grille in the ledge's railing) must stop it before the edge
            var shut = DrivePath(sc["S1"].Path, 0, 16f, 8f, "s1_shut");
            yield return shut.run;
            Check("S1's gate shut: the vehicle does not get over the ledge's edge", !shut.reached && car.Body.position.y > 260f, shut.detail);
            foreach (var id in new[] { "S1", "S2", "S3" })
            {
                var g = gates[id]; var plate = new Vector3(g.Data.sx, g.Data.sy, g.Data.sz); var n = new Vector3(g.Data.nx, g.Data.ny, g.Data.nz).normalized;
                if (id == "S1")
                {
                    // the wall plate: the vehicle held against the wall there, as riding the wall does
                    var at = plate + n * 0.55f;
                    car.Body.isKinematic = true; car.Body.position = at;
                    yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                    car.Body.isKinematic = false;
                }
                else { respawn.PlaceAt(plate, 0f); race.NotifyTeleported(); }
                yield return new WaitForSecondsRealtime(0.6f);
                Check($"{g.Data.label}: over its plate opens its gate", g.Open, g.Open ? $"at ({plate.x:0.0}, {plate.y:0.0}, {plate.z:0.0}) by {g.OpenedBy}" : $"plate at ({plate.x:0.0}, {plate.y:0.0}, {plate.z:0.0}) not opened");
                yield return new WaitForSecondsRealtime(1.4f);
                var d = DrivePath(sc[id].Path, 0, id == "S2" ? 9f : id == "S1" ? 12f : 7f, 40f, id.ToLower());   // the AI's speeds there
                yield return d.run;
                Check($"{id} {sc[id].Name}: driven through to its end, on its wheels, never stuck", d.reached && !d.stuck, d.detail);
            }
            race.EndRace();
            yield return new WaitForSecondsRealtime(1f);
        }

        sealed class Drive { public IEnumerator run; public bool reached, stuck; public string detail = ""; }
        Drive DrivePath(Vector3[] path, int from, float speed, float seconds, string shot)
        {
            var d = new Drive(); d.run = DriveRun(d, path, from, speed, seconds, shot); return d;
        }
        IEnumerator DriveRun(Drive d, Vector3[] path, int from, float speed, float seconds, string shot)
        {
            var p0 = path[Mathf.Min(from, path.Length - 2)]; var f0 = Flat(path[Mathf.Min(from + 2, path.Length - 1)] - p0).normalized;
            respawn.PlaceAt(p0, Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg); race.NotifyTeleported();
            yield return new WaitForSecondsRealtime(0.8f);
            car.enabled = false; car.Body.linearVelocity = f0 * speed * 0.6f;
            var cum = new float[path.Length]; for (int i = 1; i < path.Length; i++) cum[i] = cum[i - 1] + Flat(path[i] - path[i - 1]).magnitude;
            float total = cum[cum.Length - 1], t = 0f, slow = 0f, maxDev = 0f; int minWheels = 99; bool shotDone = false;
            while (t < seconds)
            {
                yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                var at = car.Body.position; float along = Project(path, cum, at, out float dev);
                if (t > 0.8f) { maxDev = Mathf.Max(maxDev, dev); minWheels = Mathf.Min(minWheels, car.GroundedWheels); }
                if (along >= total - 2f) { d.reached = true; break; }
                var aim = PointAlong(path, cum, along + 6f); var fwd = Flat(aim - at).normalized;
                float steer = Mathf.Clamp(Vector3.SignedAngle(Flat(car.transform.forward), fwd, Vector3.up) / 20f, -1f, 1f);
                car.Simulate(car.ForwardSpeed < speed ? 1f : 0f, car.ForwardSpeed > speed + 2f ? 1f : 0f, steer, Time.fixedDeltaTime);
                slow = car.Body.linearVelocity.magnitude < 1f && t > 1f ? slow + Time.fixedDeltaTime : 0f;
                if (slow > 2f) { d.stuck = true; break; }
                if (!shotDone && along > total * 0.5f) { shotDone = true; yield return Shot("b1_" + shot); }
            }
            car.enabled = true;
            var e = car.Body.position;
            d.detail = $"{(d.reached ? "reached its end" : d.stuck ? "STUCK" : "did not reach its end")} in {t:0.0} s, ended at ({e.x:0.0}, {e.y:0.0}, {e.z:0.0}), farthest from the line {maxDev:0.0} m, fewest wheels down {minWheels}";
        }
        static float Project(Vector3[] path, float[] cum, Vector3 p, out float dev)
        {
            float best = float.MaxValue, s = 0f;
            for (int i = 0; i + 1 < path.Length; i++)
            {
                Vector3 a = path[i], ab = Flat(path[i + 1] - a); float L2 = ab.sqrMagnitude;
                float t = L2 < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(Flat(p - a), ab) / L2); var q = Vector3.Lerp(path[i], path[i + 1], t);
                float dd = Flat(q - p).sqrMagnitude + 4f * (q.y - p.y) * (q.y - p.y);
                if (dd < best) { best = dd; s = cum[i] + t * (cum[i + 1] - cum[i]); }
            }
            dev = Mathf.Sqrt(best); return s;
        }
        static Vector3 PointAlong(Vector3[] path, float[] cum, float s)
        {
            if (s >= cum[cum.Length - 1]) return path[path.Length - 1];
            int i = 1; while (i < cum.Length - 1 && cum[i] < s) i++;
            return Vector3.Lerp(path[i - 1], path[i], Mathf.InverseLerp(cum[i - 1], cum[i], s));
        }

        // ---------------------------------------------------------------- B4: resets round the lap
        IEnumerator Resets()
        {
            report.AppendLine("== B4 resets at 20 places round the lap (put there facing backward, then a reset as R / Y does)");
            yield return StartUnderground(0, 9);
            var route = race.Route; float L = route.LapLength; int good = 0; var lines = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                float s = L * (i + 0.5f) / 20f;
                var p = route.PointAt(route.GateS + s, out var f);
                float back = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg + 160f;
                respawn.PlaceAt(p + Vector3.Cross(Vector3.up, f) * 0.6f, back); race.NotifyTeleported();
                yield return new WaitForSecondsRealtime(1.0f);
                var where = car.Body.position;
                if (!(VehicleRespawn.Recover != null && VehicleRespawn.Recover(respawn, out var rp, out var ry))) { lines.Add($"{i + 1,2} s {s,5:0}: NO SPOT FOUND"); continue; }
                respawn.PlaceAt(rp, ry); race.NotifyTeleported();
                yield return new WaitForSecondsRealtime(1.5f);
                var now = car.Body.position; float sNow = route.LapStationOf(now); route.PointAt(route.GateS + sNow, out var fNow);
                // on a shortcut's line the race's way is along it
                var near = route.NearestOnRoute(now, out var fRoute, out var onSc, out _);
                float off = Vector3.Angle(Flat(car.transform.forward), fRoute);
                float dist = Mathf.Sqrt(Flat(near - now).sqrMagnitude), dy = now.y - near.y;
                bool ok = off < 20f && car.GroundedWheels >= 2 && car.transform.up.y > 0.8f && dist < 2.0f && Mathf.Abs(dy) < 1.6f;
                if (ok) good++;
                string place = DrainPipe.Inside(now, out _) is DrainPipe dp ? dp.id : "outside";
                lines.Add($"{i + 1,2} s {s,5:0} m ({place}{(onSc != null ? ", on " + onSc.Id : "")}): from ({where.x:0.0}, {where.y:0.0}, {where.z:0.0}) facing back -> ({now.x:0.0}, {now.y:0.0}, {now.z:0.0}), {off:0} deg off the race's way, {car.GroundedWheels} wheels down, up {car.transform.up.y:0.00}, {dist:0.0} m from the line, {dy:+0.0;-0.0} m over it {(ok ? "OK" : "<< BAD")}");
                if (i == 4 || i == 12) yield return Shot($"b4_reset_{i + 1}");
            }
            foreach (var x in lines) report.AppendLine("      " + x);
            Check("every reset faces the way the race goes, on its wheels, on the route", good == 20, $"{good} of 20");
            race.EndRace();
            yield return new WaitForSecondsRealtime(1f);
        }

        // ---------------------------------------------------------------- B5 and B6: the bike's lap, its heading and the camera
        IEnumerator BikeLap()
        {
            report.AppendLine("== B5 / B6 the lap by script (heading logged; first person chosen)");
            if (views) views.SetView(CameraViews.View.FirstPerson);
            yield return StartUnderground(0, 1);
            var route = race.Route; var path = route.Path; var cum = new float[path.Length];
            for (int i = 1; i < path.Length; i++) cum[i] = cum[i - 1] + Flat(path[i] - path[i - 1]).magnitude;
            float total = cum[cum.Length - 1];
            var tsv = new StringBuilder("t\ts\tx\ty\tz\tspeed\tsteer\theading_off\twheels\tsurface_deg\tin_pipe\tview\tcam_from_car\n");
            float t = 0f; int camFar = 0, camFrames = 0, viewChanged = 0; bool resetDone = false, finished = false;
            var spells = new List<string>(); float spell = 0f, spellSteer = 0f; string spellStart = ""; int unsteered = 0;
            car.enabled = false;
            float lastAlong = 0f;
            while (t < 260f)
            {
                yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                var at = car.Body.position;
                float along = Project(path, cum, at, out _);
                if (along < lastAlong - total * 0.5f) along += total;   // past the line
                lastAlong = along;
                if (race.Player != null && race.Player.Finished) { finished = true; break; }
                var aim = PointAlong(path, cum, Mathf.Repeat(along + 7f, total)); var fwd = Flat(aim - at).normalized;
                var dirAt = Flat(PointAlong(path, cum, Mathf.Repeat(along + 1.5f, total)) - PointAlong(path, cum, Mathf.Repeat(along - 1.5f, total))).normalized;
                float steer = Mathf.Clamp(Vector3.SignedAngle(Flat(car.transform.forward), fwd, Vector3.up) / 20f, -1f, 1f);
                // race pace: lift for the turn still to make 14 m ahead
                var d0 = Flat(PointAlong(path, cum, Mathf.Repeat(along + 1f, total)) - PointAlong(path, cum, along)); var d1 = Flat(PointAlong(path, cum, Mathf.Repeat(along + 15f, total)) - PointAlong(path, cum, Mathf.Repeat(along + 14f, total)));
                float want = 26f * Mathf.Clamp(1f - Vector3.Angle(d0, d1) / 110f, 0.35f, 1f);
                car.Simulate(car.ForwardSpeed < want ? 1f : 0f, car.ForwardSpeed > want + 2f ? 1f : 0f, steer, Time.fixedDeltaTime);
                // heading across the lap: the vehicle's nose on the surface it rides (its SurfaceUp) against the lap's way, in plan
                var nose = Vector3.ProjectOnPlane(car.transform.forward, car.SurfaceUp); float off = Vector3.Angle(Flat(nose), dirAt);
                if (Flat(nose).sqrMagnitude < 0.05f) off = 0f;
                float surf = Vector3.Angle(car.SurfaceUp, Vector3.up); bool inPipe = DrainPipe.InsideAny(at);
                float camD = cam ? Vector3.Distance(cam.transform.position, car.transform.position) : 0f;
                if (views) { camFrames++; if (views.Current != CameraViews.View.FirstPerson) viewChanged++; if (camD > 3f) camFar++; }
                tsv.AppendLine($"{t:0.00}\t{along:0.0}\t{at.x:0.00}\t{at.y:0.00}\t{at.z:0.00}\t{car.ForwardSpeed:0.0}\t{steer:0.00}\t{off:0.0}\t{car.GroundedWheels}\t{surf:0}\t{(inPipe ? 1 : 0)}\t{(views ? views.CurrentName : "-")}\t{camD:0.0}");
                if (off > 30f) { if (spell == 0f) { spellSteer = steer; spellStart = $"at s {along:0} m ({at.x:0.0}, {at.y:0.0}, {at.z:0.0}) {(inPipe ? "in a pipe" : "outside the pipes")}, {car.ForwardSpeed:0.0} m/s, steering {steer:+0.00;-0.00}, {car.GroundedWheels} wheels down, on a surface {surf:0} deg from level"; } spell += Time.fixedDeltaTime; }
                else { if (spell > 0.25f) { spells.Add($"{spell:0.00} s turned across, from {spellStart}"); if (Mathf.Abs(spellSteer) < 0.5f) unsteered++; } spell = 0f; }
                if (!resetDone && along > total * 0.55f)
                {
                    resetDone = true; car.enabled = true;
                    if (VehicleRespawn.Recover != null && VehicleRespawn.Recover(respawn, out var rp, out var ry)) { respawn.PlaceAt(rp, ry); race.NotifyTeleported(); }
                    yield return new WaitForSecondsRealtime(0.4f);
                    if (views) Check("B6: first person kept through a reset", views.Current == CameraViews.View.FirstPerson && cam && Vector3.Distance(cam.transform.position, car.transform.position) < 3f, $"view {views.CurrentName}, camera {Vector3.Distance(cam.transform.position, car.transform.position):0.0} m from the vehicle");
                    yield return Shot("b6_after_reset");
                    car.enabled = false;
                }
            }
            car.enabled = true;
            File.WriteAllText(Path.Combine(outputDir, "bikes_heading.tsv"), tsv.ToString());
            if (Only("bikes"))
            {
                report.AppendLine($"INFO  the lap by script in {t:0} s, {(finished ? "finished" : "NOT finished")}; spells of 0.25 s or more turned 30 degrees or more across the lap: {spells.Count}");
                foreach (var x in spells) report.AppendLine("      " + x);
                Check("B5: the bike never turned across the lap by itself (steering under half) for a quarter second or more", unsteered == 0, $"{spells.Count} spells, {unsteered} with the steering under half");
            }
            if (views && Only("camera"))
            {
                yield return Shot("b6_after_finish");
                Check("B6: first person kept round the whole lap (pipes, glass, junctions, M1's mouth, the hub, the span) and at the finish", viewChanged == 0 && camFar == 0, $"{camFrames} frames: view changed in {viewChanged}, camera more than 3 m from the vehicle in {camFar}");
            }
            race.EndRace();
            yield return new WaitForSecondsRealtime(1f);
        }

        // B5: riding the walls as a player does, in M1b (free roam): at 12, 20 and 28 m/s, steered hard up one side for 0.6 s, then let go
        // for 3 s; and slow up a wall (the throttle off up there). How far the nose turns across the pipe, and how long it stays across
        // once the steering is let go
        IEnumerator BikeWall()
        {
            report.AppendLine("== B5 riding the walls (M1b, free roam, the express off): steered up a wall, then let go");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var expressWas = PipeExpress.Setting; PipeExpress.Setting = PipeExpress.Mode.Off;
            yield return new WaitForSecondsRealtime(0.5f);
            var pipe = FindObjectsByType<DrainPipe>(FindObjectsSortMode.None).Where(d => d.id != null && d.id.StartsWith("M1b") && d.floor != null && d.floor.Length > 50 && d.radius > 2f).OrderByDescending(d => d.floor.Length).FirstOrDefault();
            if (!pipe) { Check("a stretch of M1b", false); yield break; }
            var tsv = new StringBuilder("case\tt\tphase\tspeed\tsteer\toff\tsurface_deg\twheels\n");
            int bad = 0; var lines = new List<string>();
            foreach (float speed in new[] { 12f, 20f, 28f })
                foreach (float side in new[] { 1f, -1f })
                    foreach (bool slow in new[] { false, true })
                    {
                        string name = $"{speed:0} m/s {(side > 0 ? "right" : "left")} wall{(slow ? ", throttle off up there" : "")}";
                        var a = pipe.floor[4]; var along = (pipe.floor[8] - pipe.floor[4]).normalized;
                        respawn.PlaceAt(a, Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg);
                        yield return new WaitForSecondsRealtime(0.6f);
                        car.enabled = false; car.Body.linearVelocity = along * speed;
                        float t = 0f, maxOff = 0f, acrossAfter = 0f, maxSurf = 0f, maxSteered = 0f; bool released = false;
                        while (t < 4.6f)
                        {
                            yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                            string phase = t < 0.8f ? "straight" : t < 1.4f ? "steer" : "let go";
                            float steer = phase == "steer" ? side : 0f;
                            bool gas = !(slow && t > 1.4f);
                            car.Simulate(gas && car.ForwardSpeed < speed ? 1f : 0f, 0f, steer, Time.fixedDeltaTime);
                            // the pipe's way here (its floor near the vehicle)
                            int k = 0; float best = float.MaxValue; for (int i = 0; i < pipe.floor.Length; i++) { float d = (pipe.floor[i] - car.Body.position).sqrMagnitude; if (d < best) { best = d; k = i; } }
                            var dir = (pipe.floor[Mathf.Min(k + 1, pipe.floor.Length - 1)] - pipe.floor[Mathf.Max(k - 1, 0)]).normalized;
                            var nose = Vector3.ProjectOnPlane(car.transform.forward, car.SurfaceUp); var ax = Vector3.ProjectOnPlane(dir, car.SurfaceUp);
                            float off = nose.sqrMagnitude > 0.01f && ax.sqrMagnitude > 0.01f ? Vector3.Angle(nose, ax) : 0f; if (off > 90f) off = 180f - off;
                            // the nose against the pipe's line, seen from the axis: also the vehicle's own up against the surface it is on
                            float surf = Vector3.Angle(car.SurfaceUp, Vector3.up); maxSurf = Mathf.Max(maxSurf, surf);
                            if (phase == "steer") maxSteered = Mathf.Max(maxSteered, off);
                            if (phase == "let go") { if (!released) released = true; maxOff = Mathf.Max(maxOff, off); if (off > 30f && t > 2.0f) acrossAfter += Time.fixedDeltaTime; }
                            tsv.AppendLine($"{name}\t{t:0.00}\t{phase}\t{car.ForwardSpeed:0.0}\t{steer:0.0}\t{off:0.0}\t{surf:0}\t{car.GroundedWheels}");
                        }
                        car.enabled = true;
                        bool ok = acrossAfter < 0.3f;
                        if (!ok) bad++;
                        lines.Add($"{name}: up the wall to {maxSurf:0} deg, steered at most {maxSteered:0} deg across; after letting go the nose at most {maxOff:0} deg across, more than 30 deg across from 0.6 s after letting go for {acrossAfter:0.00} s {(ok ? "OK" : "<< ACROSS")}");
                    }
            foreach (var x in lines) report.AppendLine("      " + x);
            File.WriteAllText(Path.Combine(outputDir, "bikes_walls.tsv"), tsv.ToString());
            Check("B5: let go up a wall, the bike comes back along the pipe (never left across)", bad == 0, $"{lines.Count - bad} of {lines.Count}");
            PipeExpress.Setting = expressWas;
        }

        // B6: an express ride in free roam (Boost), first person kept: the vehicle rolled into the shortest stretch at 14 m/s, the express
        // carries it (no input)
        IEnumerator Express()
        {
            if (!views) yield break;
            report.AppendLine("== B6 an express ride in free roam (Boost), first person");
            PipeExpress.Setting = PipeExpress.Mode.Boost;
            var x = PipeExpress.Instance;
            var st = x && x.stretches != null ? x.stretches.Where(q => q.floor != null && q.floor.Length > 40).OrderBy(q => q.floor.Length).FirstOrDefault() : null;
            if (st == null) { Check("an express stretch to ride", false, "none found"); yield break; }
            var a = st.floor[0]; var dirIn = Flat(st.floor[4] - st.floor[0]).normalized;
            respawn.PlaceAt(a - dirIn * 6f, Mathf.Atan2(dirIn.x, dirIn.z) * Mathf.Rad2Deg);
            yield return new WaitForSecondsRealtime(0.6f);
            int rides0 = PipeExpress.Rides; car.Body.linearVelocity = dirIn * 14f;
            int far = 0, n = 0, changed = 0; float t = 0f; string farInfo = "";
            while (t < 25f && !(PipeExpress.Rides > rides0 && t > 3f && car.Body.linearVelocity.magnitude < 22f))
            {
                yield return new WaitForEndOfFrame(); t += Time.unscaledDeltaTime; n++;   // after the camera has been placed this frame
                if (views.Current != CameraViews.View.FirstPerson) changed++;
                float cd = Vector3.Distance(cam.transform.position, car.transform.position);
                if (cd > 3f) { far++; farInfo += $" [frame {n}, t {t:0.00} s, {cd:0.0} m, speed {car.Body.linearVelocity.magnitude:0}, kinematic {car.Body.isKinematic}, view {views.CurrentName}]"; }
                if (n == 90) yield return Shot("b6_express");
            }
            Check("B6: first person kept on an express ride", PipeExpress.Rides > rides0 && changed == 0 && far == 0, $"{st.id} {st.name}: rides {PipeExpress.Rides - rides0} ({PipeExpress.LastRide}); {n} frames: view changed {changed}, camera more than 3 m away {far}{farInfo}");
        }
    }
}
