using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 91 in the exe (-exeRound91 <dir> [-r91Only q91view,s91notes,p91aero,n91pipes] [-r91Tubes M7a,...] -r20Minutes 150 -logFile <dir>\Player.log), from the
    // repo root (n91pipes reads Data/pipes/network.json), on the check's own save. CHECK IT AS DAN PLAYS IT:
    //   q91view   item 0: the Viking Quarry race from the driver's seat and the chase camera at each landing's touchdown, at Dan's spot (the lap's 1700 m)
    //             and before every bend of the lap; at each landing whether the next checkpoint's arch is in the camera's view and not hidden; HIGH too
    //   s91notes  A: split screen (side by side and top / bottom): a jump note and a speed-trap note in each Free Roam half, a race note in each race half
    //   p91aero   B: each plane in the air flown by the virtual controller: a barrel roll right, one left, an inside loop; pictures every half second
    //   n91pipes  C: every stage-5 pipe driven end to end by the human-like driver (stuck, resets, the hardest jolt); each exit and manhole driven out
    //             of; each launch pipe from inside: the throw, where it lands, upright
    public sealed partial class Round20Runner
    {
        string R91Arg(string name, string def) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : def; }

        // ---------------------------------------------------------------- item 0
        IEnumerator R91View()
        {
            report.AppendLine(); report.AppendLine("=== Item 0: the Viking Quarry race readable after its landings (the Drifter Twin; driver's seat and chase)");
            yield return Clock(13f);
            var views = FindAnyObjectByType<CameraViews>();
            foreach (bool high in new[] { false, true })
            {
                R84High(high); yield return Vehicle("drifter");
                yield return StartRoute("viking_quarry.json", "forward", 0, 1);
                var r = race.Route; if (r == null) { Check("view: the Viking Quarry race loads", false); yield break; }
                string hm = high ? "_high" : "";
                var q = QuarryJumps.Instance; int seen = 0, n = 0;
                if (q)
                    foreach (var j in q.Jumps)
                    {
                        float td = (j.land_s[0] + j.land_s[j.land_s.Length - 1]) * .5f;
                        var at = QuarryJumps.At(j, td); var p = RaceRoute.Surface(at.x, at.z);
                        Place(p, QuarryJumps.Forward(j), 22f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(.25f);
                        float st = r.LapStationOf(p); var next = R91NextCp(r, st, out int id, out float ahead);
                        if (views) views.SetView(CameraViews.View.FirstPerson); yield return new WaitForSecondsRealtime(.15f);
                        bool vis = R91InView(next); n++; if (vis) seen++;
                        yield return Capture($"91_land_{j.id}_seat{hm}");
                        if (views) views.SetView(CameraViews.View.Chase); var cc = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cc) cc.Snap();
                        yield return new WaitForSecondsRealtime(.2f); yield return Capture($"91_land_{j.id}_chase{hm}");
                        report.AppendLine($"  {j.id} {j.name}{(high ? " (HIGH)" : "")}: touchdown at lap {st:0} m; the next checkpoint CP{id} {ahead:0} m on: {(vis ? "IN VIEW from the driver's seat" : "NOT in view")}; pictures 91_land_{j.id}_seat{hm}, _chase{hm}");
                    }
                // Dan's spot (the lap's 1700 m, after The Big One's landing)
                {
                    var p = r.PointAt(r.GateS + 1700f, out var f); Place(RaceRoute.Surface(p.x, p.z), f, 18f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(.25f);
                    var next = R91NextCp(r, 1700f, out int id, out float ahead); var cc = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cc) cc.Snap();
                    yield return new WaitForSecondsRealtime(.3f); bool vis = R91InView(next);
                    yield return Capture($"91_dan_spot{hm}");
                    report.AppendLine($"  Dan's spot (lap 1700 m): the next checkpoint CP{id} {ahead:0} m on: {(vis ? "in view" : "NOT in view")}; picture 91_dan_spot{hm}");
                    if (!high) Check("0: at Dan's spot the next checkpoint's arch is in view", vis, $"CP{id}, {ahead:0} m");
                }
                if (!high)
                {
                    Check("0: from every landing's touchdown the next checkpoint's arch is in view (driver's seat)", seen == n && n == 3, $"{seen} of {n}");
                    // every bend of the lap (the route's AI bend zones), 25 m before it, from the chase camera
                    var bends = race.AiZones?.Where(z => z.id.StartsWith("the bend")).ToList();
                    int b = 0;
                    if (bends != null)
                        foreach (var z in bends)
                        {
                            var p = r.PointAt(r.GateS + z.s0 - 25f, out var f); Place(RaceRoute.Surface(p.x, p.z), f, 14f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(.2f);
                            var cc = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cc) cc.Snap(); yield return new WaitForSecondsRealtime(.3f);
                            yield return Capture($"91_bend_{(int)z.s0:0000}"); b++;
                        }
                    report.AppendLine($"  {b} bends pictured (91_bend_<lap m>)");
                }
                race.EndRace(); yield return new WaitForSecondsRealtime(.5f);
            }
            R84High(false);
        }
        static Vector3 R91NextCp(RaceRoute r, float st, out int id, out float ahead)
        {
            float L = r.LapLength; id = 0; ahead = 1e9f; Vector3 best = default;
            for (int k = 0; k < r.Checkpoints.Count; k++)
            {
                float d = Mathf.Repeat(r.Checkpoints[k].S - st, L); if (d < 4f || d >= ahead) continue;
                ahead = d; id = k + 1; var p = r.PointAt(r.GateS + r.Checkpoints[k].S, out _); best = RaceRoute.Surface(p.x, p.z);
            }
            return best;
        }
        bool R91InView(Vector3 gate)
        {
            var cam = Camera.main; if (!cam) return false;
            foreach (float up in new[] { 2f, 5f, 8f })   // (any part of the arch: its sides, its top)
            {
                var t = gate + Vector3.up * up; var vp = cam.WorldToViewportPoint(t);
                if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) continue;
                bool hidden = false;
                foreach (var h in Physics.RaycastAll(cam.transform.position, (t - cam.transform.position).normalized, Vector3.Distance(cam.transform.position, t) - 1f, ~0, QueryTriggerInteraction.Ignore))
                    if (!(h.collider.attachedRigidbody && h.collider.attachedRigidbody == car.Body)) { hidden = true; break; }
                if (!hidden) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- A
        IEnumerator R91Notes()
        {
            report.AppendLine(); report.AppendLine("=== A: split screen, each player's notes at the top of their half");
            var msg = typeof(ArcadeActivities).GetMethod("Message", BindingFlags.Instance | BindingFlags.NonPublic);
            bool keep = GameSettings.Settings.splitLeftRight; int ok = 0, n = 0;
            foreach (bool lr in new[] { true, false })
            {
                GameSettings.Settings.splitLeftRight = lr; string tagL = lr ? "side" : "stack";
                if (race.State != RaceController.Stage.Free) race.EndRace();
                car.GetComponent<VehicleRespawn>().PlaceAt(RaceRoute.Surface(3108.8f, 1065.0f) + Vector3.up * .3f, 0f); yield return new WaitForSecondsRealtime(.3f);
                var s = BeginSplit(SplitScreen.Kind.FreeRoam, false); yield return new WaitForSecondsRealtime(.8f);
                var a1 = ArcadeActivities.Instance; var a2 = s.Activities2;
                foreach (var (what, t1, t2) in new[] { ("jump", "CLEAN JUMP / 38 m / 1.42 s / 522 pts", "CLEAN JUMP / 21 m / 0.88 s / 298 pts"), ("trap", "SPEED TRAP / 71 mph", "SPEED TRAP / 64 mph") })
                {
                    if (msg != null) { if (a1) msg.Invoke(a1, new object[] { t1, 6f }); if (a2) msg.Invoke(a2, new object[] { t2, 6f }); }
                    yield return new WaitForSecondsRealtime(.4f); yield return Capture($"91_split_{tagL}_{what}");
                    var nr = SplitHud.NoteRect; n++; bool top = nr.width > 0f && nr.y < 200f; if (top) ok++;
                    report.AppendLine($"  {(lr ? "side by side" : "top / bottom")}, {what}: the note's box at y {nr.y:0} (the half's own units), {(top ? "at the top" : "NOT at the top")}; picture 91_split_{tagL}_{what}");
                }
                SplitScreen.End(); yield return new WaitForSecondsRealtime(.3f);
                // a race: each player's own note
                RaceController.RouteOverride = "chastain_glen_loop.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 1; RaceController.AiCount = 0;
                race.Reload(); var sr = BeginSplit(SplitScreen.Kind.Race, false); yield return new WaitForSecondsRealtime(.5f);
                race.StartRace(); float c0 = Time.realtimeSinceStartup; while (race.State == RaceController.Stage.Countdown && Time.realtimeSinceStartup - c0 < 10f) yield return null;
                yield return new WaitForSecondsRealtime(1.2f);
                var e1 = race.HumanEntrant(1); var e2 = race.HumanEntrant(2);
                if (e1 != null) { e1.Message = "MISSED CHECKPOINT 2  +5 s"; e1.MessageUntil = Time.time + 6f; }
                if (e2 != null) { e2.Message = "SHORTCUT!"; e2.MessageUntil = Time.time + 6f; }
                yield return new WaitForSecondsRealtime(.4f); yield return Capture($"91_split_{tagL}_race");
                race.EndRace(); SplitScreen.End(); RaceController.LapsOverride = 0; yield return new WaitForSecondsRealtime(.5f);
            }
            GameSettings.Settings.splitLeftRight = keep;
            Check("A: in split screen the jump and speed-trap notes are at the top of each half (both layouts)", ok == n && n == 4, $"{ok} of {n}; race notes: pictures 91_split_*_race");
        }

        // ---------------------------------------------------------------- B
        IEnumerator R91Aero()
        {
            report.AppendLine(); report.AppendLine("=== B: barrel rolls and loops, each plane flown by the virtual controller (full throttle; the stick held, then let go)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(13f);
            foreach (var id in R91Arg("-r91Planes", "plane,dart").Split(','))
            {
                yield return Vehicle(id); yield return new WaitForSecondsRealtime(.6f);
                var pl = car.GetComponent<PlaneFlight>(); if (!pl || !pl.enabled) { Check($"B {id}: the plane flies", false, "no PlaneFlight"); continue; }
                R73Pad();
                var start = new Vector3(-1200f, 0f, -3200f); start.y = GroundQuery.Height(start.x, start.z); if (float.IsNaN(start.y)) start.y = 300f; start.y += 450f;
                pl.AirStart(start, 90f, 0f, 85f); yield return new WaitForSecondsRealtime(1.5f);
                int c0 = pl.Crashes; float minH = 1e9f;
                IEnumerator Hold(string name, float steer, float pitch, Func<bool> done, float max)
                {
                    float t = 0f, nextPic = 0f; int k = 0;
                    while (t < max && !done()) { R73Set(steer, 1f, 0f, pitch); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; minH = Mathf.Min(minH, pl.HeightAboveGround); if (t >= nextPic) { nextPic += .5f; yield return Capture($"91_{id}_{name}_{k++:00}"); } }
                    report.AppendLine($"    {name}: {t:0.0} s held, then roll {pl.RollDeg:0}, pitch {pl.PitchDeg:0}, {pl.Airspeed:0} m/s, {pl.HeightAboveGround:0} m up");
                }
                IEnumerator LetGo(float sec) { float t = 0f; while (t < sec) { R73Set(0f, 1f, 0f, 0f); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; minH = Mathf.Min(minH, pl.HeightAboveGround); } }
                int r0 = pl.Rolls; yield return Hold("roll_right", 1f, 0f, () => pl.Rolls > r0, 8f); bool rr = pl.Rolls > r0; yield return LetGo(3f);
                int r1 = pl.Rolls; yield return Hold("roll_left", -1f, 0f, () => pl.Rolls > r1, 8f); bool rl = pl.Rolls > r1; yield return LetGo(3f);
                int l0 = pl.Loops; float y0 = car.Body.position.y, h0 = pl.YawDeg;
                yield return Hold("loop", 0f, -1f, () => pl.Loops > l0, 22f);
                float dy = car.Body.position.y - y0, dh = Mathf.Abs(Mathf.DeltaAngle(h0, pl.YawDeg));
                bool lp = pl.Loops > l0 && Mathf.Abs(dy) < 250f && dh < 45f;   // (a loop comes back round: about the height and the heading it went in at)
                report.AppendLine($"    the loop: {dy:+0;-0} m from where it started, heading {dh:0} deg off");
                yield return LetGo(5f);
                bool flying = pl.Airborne && !pl.Crashed && pl.Crashes == c0;
                yield return Capture($"91_{id}_after");
                report.AppendLine($"  {id}: barrel roll right {(rr ? "yes" : "NO")}, left {(rl ? "yes" : "NO")}, loop {(lp ? "yes" : "NO")}; 5 s after letting go: roll {pl.RollDeg:0}, pitch {pl.PitchDeg:0}, {(flying ? "flying" : "NOT flying (" + pl.CrashReason + ")")}; lowest {minH:0} m over the ground");
                Check($"B {id}: a barrel roll each way and an inside loop on the controller, still flying after", rr && rl && lp && flying, $"rolls {rr}/{rl}, loop {lp}, crashes {pl.Crashes - c0}");
                R73Set(0, 0, 0);
            }
            yield return Vehicle("drifter");
        }

        // ---------------------------------------------------------------- C
        [Serializable] class R91End { public string kind, chamber, manhole, exit; }
        [Serializable] class R91Tube { public string id, name; public int stage; public float d; public R91End a, b; public float[] pts; }
        [Serializable] class R91Exit { public string id, name, tube; public float[] a, b, launch_to; }
        [Serializable] class R91Mh { public string id, tube, road; public float x, y, z; }
        [Serializable] class R91Net { public R91Tube[] tubes; public R91Exit[] exits; public R91Mh[] manholes; }
        IEnumerator R91Pipes()
        {
            report.AppendLine(); report.AppendLine("=== C: the underground across the map: every stage-5 pipe driven end to end, every new exit and manhole driven out of, the quarry's launch pipes");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var path = Path.Combine(Environment.CurrentDirectory, "Data/pipes/network.json");
            if (!File.Exists(path)) { Check("C: Data/pipes/network.json (run from the repo root)", false); yield break; }
            var net = JsonUtility.FromJson<R91Net>(File.ReadAllText(path));
            var only = R91Arg("-r91Tubes", ""); var pick = only.Length > 0 ? only.Split(',') : null;
            yield return Vehicle("drifter"); int good = 0, n = 0, lgood = 0, ln = 0;
            foreach (var t in net.tubes.Where(t => t.stage == 5 && (pick == null || pick.Contains(t.id))))
            {
                var line = new List<Vector3>(); for (int i = 0; i + 2 < t.pts.Length; i += 9) line.Add(new Vector3(t.pts[i], t.pts[i + 1], t.pts[i + 2]));   // (every 3 m)
                if (line.Count < 3) continue;
                var x = net.exits.FirstOrDefault(e => e.tube == t.id); var mh = net.manholes.FirstOrDefault(m => m.tube == t.id);
                // out of an exit: on past its mouth; up a manhole: on past the street point
                if (x != null) { var A = new Vector3(x.a[0], 0f, x.a[1]); var B = new Vector3(x.b[0], 0f, x.b[1]); var o = (A - B).normalized; line.Add(RaceRoute.Surface(A.x, A.z)); if (x.launch_to != null) line.Add(RaceRoute.Surface(A.x + o.x * 12f, A.z + o.z * 12f)); if (x.launch_to == null) for (float d = 4f; d <= 20f; d += 4f) line.Add(RaceRoute.Surface(A.x + o.x * d, A.z + o.z * d)); }
                if (mh != null) { var e = line[line.Count - 1]; var o = Flat(e - line[line.Count - 4]).normalized; for (float d = 4f; d <= 16f; d += 4f) line.Add(RaceRoute.Surface(mh.x + o.x * d, mh.z + o.z * d)); }
                var p0 = line[3] + Vector3.up * .6f; Place(p0, line[5] - line[3], 8f); line.RemoveRange(0, 3);   // (9 m in: out of the junction, clear of the other pipes' openings) yield return new WaitForSecondsRealtime(.3f);
                var run = new HumanRun(); float jolt = 0f, lastVy = car.Body.linearVelocity.y; int l0 = PipeLaunch.Launches; bool launched = false;
                float t0 = Time.time;
                yield return HumanDrive(line, 26f, 420f, run, () =>
                {
                    float vy = car.Body.linearVelocity.y; if (!launched && Time.time - t0 > 1f) jolt = Mathf.Max(jolt, Mathf.Abs(vy - lastVy) / Time.fixedDeltaTime); lastVy = vy;
                    if (PipeLaunch.Launches > l0) launched = true;
                    return launched;
                }, pressYWhenStuck: false);
                string end;
                bool ok;
                if (x != null && x.launch_to != null)
                {
                    ln++; float w = 0f; while (w < 14f && !(car.GroundedWheels >= 2 && w > 1.5f)) { R73Set(0, 0, 0); yield return new WaitForFixedUpdate(); w += Time.fixedDeltaTime; }
                    yield return new WaitForSecondsRealtime(1.2f);
                    var p = car.Body.position; var tgt = new Vector3(x.launch_to[0], x.launch_to[1], x.launch_to[2]); float off = Flat(p - tgt).magnitude; bool up = Vector3.Dot(car.transform.up, Vector3.up) > .7f;
                    ok = launched && off < 45f && up && !DrainPipe.InsideAny(p); if (ok) lgood++;
                    end = $"launch {(launched ? "fired" : "DID NOT fire")} ({PipeLaunch.Last}; the flight: {GuidedFlight.LastEnd}); came down at ({p.x:0}, {p.y:0}, {p.z:0}), {off:0} m from its target, {(up ? "upright" : "NOT upright")}";
                    yield return Capture($"91_launch_{x.id}_landed");
                }
                else
                {
                    var p = car.Body.position; bool outside = !DrainPipe.InsideAny(p);
                    float reach = Flat(p - line[line.Count - 1]).magnitude;
                    ok = run.stuck == 0 && run.resets == 0 && reach < 25f && ((x == null && mh == null) || outside);
                    end = x != null ? $"out of {x.name}: {(outside ? "outside" : "STILL INSIDE")} at ({p.x:0}, {p.y:0}, {p.z:0})" : mh != null ? $"up {mh.id} ({mh.road}): {(outside ? "out on the street" : "STILL INSIDE")}" : $"{reach:0} m from the far end";
                    if (x != null || mh != null) yield return Capture($"91_out_{t.id}");
                }
                n++; if (ok) good++;
                report.AppendLine($"  {t.id} {t.name} ({t.pts.Length / 3} m): {run.time:0} s, stuck {run.stuck}, resets {run.resets}, top {run.topSpeed:0} m/s, hardest jolt {jolt:0} m/s2; {end} -> {(ok ? "ok" : "PROBLEM")}"); Flush();
                R73Set(0, 0, 0);
            }
            Check("C: every stage-5 pipe driven end to end with no stuck spot, every new exit and manhole driven out of", good == n && n > 0, $"{good} of {n}");
            Check("C: each quarry launch pipe throws you out of the pit onto the rim, upright", lgood == ln && ln >= 2, $"{lgood} of {ln}");
        }
    }
}
