using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 70 in the exe (-exeRound70 <dir> [-r70Only aitrace] [-r70Races kennesaw,trail,glen,lakes] [-r70Diff 0|1|2] [-r70Laps n] -logFile <dir>\Player.log):
    //   aitrace  B: a race per course (3 AI, the vehicles of Dan's own race replays, the player riding along 40 m over one AI after another): each AI's
    //            station, speed, target speed and what set it (RoadDriver.Why: bend, hill, crest, zone, obstacle, angle ...), pedals, every 0.1 s
    //            (r70_trace_<course>.tsv), the race's replay (r70_<course>.crreplay, for Tools/race/replay_ai_table.py) and the laps against Dan's best
    //   ramp     A: the summit ramp from the fast-travel spot, from rest, flat out (Drifter Twin, Street Classic, Needle 600): air, distance, height,
    //            the landing, where it stopped (the arrester, Old Mountain Road); pictures mid-air, of the ramp from the summit lot and the drive
    //   garage   C: a race started straight away, finished, then race setup's garage from the race-over menu (the pad, then the keys): every vehicle
    //            stepped through; then the same after Free Roam in the helicopter and in the plane
    //   vsave1 / vsave2 / vsave3   D: three launches with -vehicleSave <file> (the check's own vehicle.json, starting as Dan's old one): the plane read,
    //            a race vehicle chosen, each restart starts in the plane and race setup puts on the Drifter Twin
    //   park     E: Kennesaw Mountain's visitors in Free Roam (the summit lot, the visitor center, the trails) and in a race; pictures
    //   hyd      F1: hydrants on Kennesaw Mountain's drive and park (x -1700 to -400, z -6600 to -4900)
    //   roll     F2: the drive's roll across its width at Dan's BUG-004 (-949.83, -5725.88)
    //   gantry   F3: every course's start / finish gantry, a picture of each from the grid
    public sealed partial class Round20Runner
    {
        static readonly (string file, string dir, string key, string label, string[] cars)[] R70Races =
        {
            ("kennesaw_mountain.json", "forward", "Kennesaw Mountain|forward", "kennesaw", new[] { "original", "tourer", "drifter" }),
            ("noonday_creek_trail.json", "forward", "Noonday Creek Trail|forward", "trail", new[] { "original", "mower", "original" }),
            ("chastain_glen_loop.json", "forward", "Chastain Glen loop|forward", "glen", new[] { "mower", "skyfin", "pebble" }),
            ("chastain_lakes.json", "forward", "Chastain Lakes|forward", "lakes", new[] { "moto", "drifter", "moto" }),
        };
        static string R70Arg(string name, string def) { var a = Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < a.Length; i++) if (a[i] == name) return a[i + 1]; return def; }

        IEnumerator R70AiTrace()
        {
            int diff = int.Parse(R70Arg("-r70Diff", "2")), laps = int.Parse(R70Arg("-r70Laps", "3")); string sel = R70Arg("-r70Races", "kennesaw,trail,glen,lakes");
            string dn = diff == 2 ? "Hard" : diff == 1 ? "Normal" : "Easy";
            report.AppendLine(); report.AppendLine($"=== B. the AI on {dn}, {laps} laps, 3 AI, no traffic: laps against Dan's best (his Top 10, read only), traces r70_trace_<course>.tsv");
            var dan = DanBest(); int prev = RoadDriver.Difficulty; RoadDriver.Difficulty = diff;
            foreach (var (file, dir, key, label, cars) in R70Races)
            {
                if (!sel.Split(',').Contains(label)) continue;
                RaceController.AiVehiclesOverride = cars;
                yield return StartRoute(file, dir, 3, laps);
                var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
                var sb = new StringBuilder("ai\tt\ts\tlat\tspeed\ttarget\twhy\tat\tof\tthrottle\tbrake\tx\tz\ty\n");
                bool tracing = true;
                IEnumerator Trace()
                {
                    var road = race.AiRoad; float next = 0f;
                    while (tracing)
                    {
                        yield return new WaitForFixedUpdate();
                        if (Time.time < next) continue; next = Time.time + .1f;
                        foreach (var e in ai)
                        {
                            if (e.Finished || !e.Driver || !e.Car) continue; var d = e.Driver; var p = e.Car.Body.position;
                            float s = road.Project(p, out float lat);
                            string of = d.WhyOf is RouteAiZoneJson z ? z.id : d.WhyOf is Collider c ? c.name : "";
                            sb.Append(e.Name).Append('\t').Append(race.RaceClock.ToString("0.00")).Append('\t').Append(s.ToString("0.0")).Append('\t').Append(lat.ToString("0.0")).Append('\t')
                              .Append(e.Car.ForwardSpeed.ToString("0.0")).Append('\t').Append(d.TargetSpeed.ToString("0.0")).Append('\t').Append(d.Why).Append('\t').Append(d.WhyAt).Append('\t').Append(of.Replace('\t', ' ')).Append('\t')
                              .Append(d.LastThrottle.ToString("0.00")).Append('\t').Append(d.LastBrake.ToString("0.00")).Append('\t').Append(p.x.ToString("0.0")).Append('\t').Append(p.z.ToString("0.0")).Append('\t').Append(p.y.ToString("0.0")).Append('\n');
                        }
                    }
                }
                StartCoroutine(Trace());
                Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                yield return RideAlong(ai, 9f * 60f);
                Time.timeScale = 1f; tracing = false;
                File.WriteAllText(Path.Combine(outputDir, $"r70_trace_{label}.tsv"), sb.ToString());
                var rec = ReplayRecorder.Instance; if (rec && rec.Recording) rec.Stop("the check");
                var data = rec ? rec.Last : null; if (data != null) data.Write(Path.Combine(outputDir, $"r70_{label}.crreplay"));
                dan.TryGetValue(key, out var db);
                var best = new List<float>();
                foreach (var e in ai)
                {
                    var fl = e.LapTimes.Skip(1).ToList(); if (fl.Count > 0) best.Add(fl.Min());
                    report.AppendLine($"  {key,-30} {e.Name,-5} {e.VehicleName,-16} {(e.Finished ? $"total {e.FinalTotal:0.0} s" : $"NOT FINISHED (laps {e.CompletedLaps})")}, laps {string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0")))}, " +
                                      $"resets {(e.Driver ? e.Driver.RecoveryCount : 0)}, missed {e.MissedTotal}");
                }
                report.AppendLine($"  {key}: {dn}'s best flying lap {(best.Count > 0 ? best.Min().ToString("0.0") : "-")} s, average {(best.Count > 0 ? best.Average().ToString("0.0") : "-")} s; Dan's best {(db.Item1 > 0 ? $"{db.Item1:0.0} s ({db.Item2})" : "-")}");
                Check($"B {key}: every AI finishes", ai.All(e => e.Finished && !e.Estimated), string.Join(", ", ai.Select(e => $"{e.Name} {(e.Finished ? e.FinalTotal.ToString("0.0") : "no")}")));
                Flush();
                race.EndRace(); RaceController.DirectionOverride = null;
            }
            RaceController.AiVehiclesOverride = null; RoadDriver.Difficulty = prev;
        }
    
        // ------------------------------------------------------------ A: the summit ramp
        [Serializable] class R70RampData { public float[] dir, origin, lip, run, arrester; public float track_to, lip_y; }
        [Serializable] class R70RampFile { public R70RampData ramp; }
        IEnumerator R70Ramp()
        {
            report.AppendLine(); report.AppendLine("=== A. The summit ramp: from the fast-travel spot, from rest, flat out along the drive and straight on past the hairpin");
            string path = Path.Combine(Environment.CurrentDirectory, "Data/tracks/kennesaw_summit_ramp.json");
            if (!File.Exists(path)) { Check("ramp: its track file", false, path); yield break; }
            var r = JsonUtility.FromJson<R70RampFile>(File.ReadAllText(path)).ramp;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var o = new Vector3(r.origin[0], 0f, r.origin[1]); var d = new Vector3(r.dir[0], 0f, r.dir[1]).normalized;
            var line = new List<Vector3>();
            for (int i = 0; i + 1 < r.run.Length; i += 2) line.Add(RaceRoute.Ground(r.run[i], r.run[i + 1]));
            for (float s = 5f; s <= r.track_to; s += 5f) { var q = o + d * s; line.Add(RaceRoute.Ground(q.x, q.z)); }
            foreach (var vid in new[] { "drifter", "original", "moto" })
            {
                if (VehicleProfile.IndexOf(vid) < 0) continue;
                yield return Vehicle(vid); yield return new WaitForSecondsRealtime(.3f);
                var flights = new List<R68Flight>(); var run = new Run();
                Func<R68Flight, float, IEnumerator> pic = vid == "drifter" ? (fl, t) => R68Pic("70_ramp_midair", fl) : (Func<R68Flight, float, IEnumerator>)null;
                bedsOn = true; var beds = StartCoroutine(R67BedDrag());   // (the arrester acts on ArcadeVehicle.Live: the scripted car is a disabled one)
                yield return FlyLine(line, 99f, null, flights, run, p => "the summit ramp", pic, 1.0f, 70f, $"70_ramp_trace_{vid}", 0f);
                bedsOn = false;
                var f = flights.OrderByDescending(q => q.air).FirstOrDefault(); var end = car.Body.position;
                float along = Vector3.Dot(Flat(end - o), d);
                report.AppendLine($"  {Nm(vid),-15} {(f != null ? FlightTxt(f) : "NO FLIGHT")}; stopped {along:0} m past the hairpin at ({end.x:0}, {end.z:0}), {car.Body.linearVelocity.magnitude:0.0} m/s, upright {car.transform.up.y:0.00}; catch-slope landings {RampHold.CatchLandings}");
                Check($"ramp {Nm(vid)}: 8 s or more in the air, several hundred meters, lands upright", f != null && f.air >= 8f && f.dist >= 250f && f.up > .7f, f != null ? $"{f.air:0.0} s, {f.dist:0} m, up {f.up:0.00}" : "no flight");
                Check($"ramp {Nm(vid)}: stopped short of the houses (by Old Mountain Road)", along < r.track_to + 30f && car.transform.up.y > .5f, $"{along:0} m");
            }
            // pictures: the ramp from the summit lot, from the drive at the run-up's start, from beside it, the landing from the foot
            var lip = new Vector3(r.lip[0], r.lip_y, r.lip[1]);
            yield return Clock(13f);
            yield return Still("70_ramp_from_summit_lot", RaceRoute.Ground(-944f, -5724f) + Vector3.up * 1.8f, lip, 50f);
            var st = new Vector3(r.run[0], 0f, r.run[1]); yield return Still("70_ramp_runup_start", RaceRoute.Ground(st.x, st.z) + Vector3.up * 2.2f - d * 6f, lip, 55f);
            var side = Vector3.Cross(Vector3.up, d); yield return Still("70_ramp_side", lip - d * 40f + side * 70f + Vector3.down * 10f, lip - d * 40f, 60f);
            var land = o + d * 470f; yield return Still("70_ramp_landing", RaceRoute.Ground(land.x, land.z) + d * 120f + Vector3.up * 6f + side * 20f, lip, 55f);
        }

        // ------------------------------------------------------------ C: the garage in race setup
        Gamepad r70pad; Keyboard r70keys;
        IEnumerator R70Tap(GamepadButton b) { InputSystem.QueueStateEvent(r70pad, new GamepadState().WithButton(b)); yield return null; yield return null; InputSystem.QueueStateEvent(r70pad, new GamepadState()); yield return new WaitForSecondsRealtime(.35f); }
        IEnumerator R70Key(Key k) { InputSystem.QueueStateEvent(r70keys, new KeyboardState(k)); yield return null; yield return null; InputSystem.QueueStateEvent(r70keys, new KeyboardState()); yield return new WaitForSecondsRealtime(.35f); }
        IEnumerator R70Focus(GameMenus m, string id, bool keys)
        {
            for (int i = 0; i < 16 && m.FocusedForCheck != id; i++) yield return keys ? R70Key(Key.DownArrow) : R70Tap(GamepadButton.DpadDown);
        }
        IEnumerator R70GarageFrom(GameMenus m, bool keys, string label)
        {
            m.OpenPauseForCheck(); yield return new WaitForSecondsRealtime(.6f);
            string into = m.FocusedForCheck == "setup" ? "setup" : "race";
            yield return R70Focus(m, "setup", keys); if (m.FocusedForCheck != "setup") yield return R70Focus(m, "race", keys);
            yield return keys ? R70Key(Key.Space) : R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(.5f);
            yield return R70Focus(m, "garage", keys); yield return keys ? R70Key(Key.Space) : R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(.5f);
            string page = m.PageForCheck; var cfg = car.GetComponent<VehicleConfiguration>(); string first = cfg.profileId;
            yield return R70Focus(m, "vehicle", keys);
            var seen = new List<string> { first }; int changed = 0;
            for (int i = 0; i < 16; i++)
            {
                yield return keys ? R70Key(Key.RightArrow) : R70Tap(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.2f);
                string now = cfg.profileId; if (now != seen[seen.Count - 1]) changed++; if (now == first) break; seen.Add(now);
            }
            if (label.Contains("finished")) yield return Capture("70_garage_" + (keys ? "keys" : "pad"));
            var all = VehicleProfile.All.Where(v => !v.Stunt && !v.Flies).Select(v => v.Id).ToList();
            bool every = all.All(seen.Contains);
            report.AppendLine($"  {label} ({(keys ? "keys" : "pad")}): page {page}, the vehicle row stepped {changed} times through {seen.Distinct().Count()} vehicles ({string.Join(", ", seen.Distinct().Select(Nm))})");
            Check($"garage {label} ({(keys ? "keys" : "pad")}): every racing vehicle chosen in race setup's garage", page == "garage" && every, $"{seen.Distinct().Count()} of {all.Count}");
            m.CloseForCheck(); yield return new WaitForSecondsRealtime(.4f);
        }
        IEnumerator R70Garage()
        {
            report.AppendLine(); report.AppendLine("=== C. Race setup's garage: a race started at once and finished, then race setup > Garage from the race-over menu; then after Free Roam in the helicopter and the plane");
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            if (r70pad == null) r70pad = InputSystem.AddDevice<Gamepad>("Round70 virtual pad");
            if (r70keys == null) r70keys = InputSystem.AddDevice<Keyboard>("Round70 virtual keys");
            var m = GameMenus.Instance ? GameMenus.Instance : GameMenus.Create(); yield return new WaitForSecondsRealtime(1.5f); if (m) m.CloseForCheck();
            foreach (bool keys in new[] { false, true })
            {
                yield return Vehicle("original");
                yield return StartRoute("chastain_glen_loop.json", "forward", 0, 1);
                var rr = race.Route; yield return new WaitForSecondsRealtime(1f);
                yield return AcrossLine(rr.Gate, 0f); foreach (var cp in rr.Checkpoints) yield return AcrossLine(cp, 0f); race.Player.LapAway = true; yield return AcrossLine(rr.Gate, 0f);
                car.Body.linearVelocity = Vector3.zero; float until = Time.realtimeSinceStartup + 12f; while (WinnerShot.Active && Time.realtimeSinceStartup < until) yield return null;
                yield return new WaitForSecondsRealtime(1f);
                Check($"garage: the race is over ({(keys ? "keys" : "pad")})", race.State == RaceController.Stage.Finished, race.State.ToString());
                yield return R70GarageFrom(m, keys, "after a finished race");
                race.EndRace();
            }
            foreach (var flyer in new[] { "heli", "plane" })
            {
                if (VehicleProfile.IndexOf(flyer) < 0) continue;
                if (race.State != RaceController.Stage.Free) race.EndRace();
                yield return Vehicle(flyer); yield return new WaitForSecondsRealtime(1f);
                yield return R70GarageFrom(m, false, $"after Free Roam in the {Nm(flyer)}");
            }
        }

        // ------------------------------------------------------------ D: the saved vehicle across restarts (-vehicleSave <file>)
        IEnumerator R70VSave(int stage)
        {
            var cfg = car.GetComponent<VehicleConfiguration>(); string file = VehicleSwitch.SavePath(); string text = File.Exists(file) ? File.ReadAllText(file) : "(none)";
            report.AppendLine(); report.AppendLine($"=== D. The saved vehicle, launch {stage}: started in {Nm(cfg.profileId)} ({cfg.profileId}); the file: {text}");
            if (stage == 1)
            {
                Check("vsave1: the old file's plane (Free Roam's last pick) is what the game starts in", cfg.profileId == "plane", cfg.profileId);
                // race setup's garage: the Drifter Twin; then Free Roam's garage: the plane (as Dan would)
                sw.Select("drifter", VehicleSwitch.Pick.Race); yield return new WaitForSecondsRealtime(.5f); sw.Select("plane", VehicleSwitch.Pick.Roam); yield return new WaitForSecondsRealtime(.5f);
                text = File.ReadAllText(file); report.AppendLine("  after a race pick (Drifter Twin) and a Free Roam pick (the plane): " + text);
                Check("vsave1: the file keeps both, in American English", text.Contains("\"roam\":\"plane\"") && text.Contains("\"race\":\"drifter\"") && text.Contains("\"color\"") && !text.Contains("colour"), text);
            }
            else
            {
                Check($"vsave{stage}: the game starts in Free Roam's last pick (the plane)", cfg.profileId == "plane", cfg.profileId);
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                var m = GameMenus.Instance ? GameMenus.Instance : GameMenus.Create(); yield return new WaitForSecondsRealtime(1.5f);
                if (r70pad == null) r70pad = InputSystem.AddDevice<Gamepad>("Round70 virtual pad");
                if (m) { m.CloseForCheck(); m.OpenPauseForCheck(); yield return new WaitForSecondsRealtime(.5f); }
                yield return R70Focus(m, "race", false); yield return R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(.6f);
                Check($"vsave{stage}: race setup puts on the last race vehicle (the Drifter Twin)", cfg.profileId == "drifter", $"{cfg.profileId} on page {(m ? m.PageForCheck : "-")}");
                if (m) m.CloseForCheck();
                text = File.ReadAllText(file); Check($"vsave{stage}: the file still says the plane for Free Roam", text.Contains("\"roam\":\"plane\""), text);
            }
        }

        // ------------------------------------------------------------ E: the park's visitors
        IEnumerator R70Park()
        {
            report.AppendLine(); report.AppendLine("=== E. Kennesaw Mountain's visitors (Free Roam, 10:00 and 17:00; then a race)");
            var life = AmbientLife.Current; if (!life) { Check("park: the life is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); int most = 0;
            var spots = new[] { ("the summit lot", -944f, -5724f, 230f), ("the visitor center", -929f, -5228f, 270f), ("the mountain trail", -1110f, -5300f, 250f), ("the west trail", -1450f, -5600f, 120f) };
            foreach (float hour in new[] { 10f, 17f })
                foreach (var (name, x, z, yaw) in spots)
                {
                    yield return Clock(hour); life.ClearWalkers(); respawn.PlaceAt(RaceRoute.Surface(x, z) + Vector3.up * .4f, yaw); race.NotifyTeleported();
                    yield return new WaitForSecondsRealtime(2f); yield return LookRound(RaceRoute.Surface(x, z)); yield return new WaitForSecondsRealtime(6f);
                    most = Mathf.Max(most, life.TrailWalkers);
                    report.AppendLine($"  {hour:00}:00 {name}: {life.WalkersInfo}");
                    if (hour == 10f) yield return Still("70_park_" + name.Replace(' ', '_'), RaceRoute.Surface(x, z) + Vector3.up * 3f, RaceRoute.Surface(x, z) + Quaternion.Euler(0, yaw, 0) * Vector3.forward * 30f, 70f);
                }
            Check("park: plenty out on the mountain in Free Roam (6 groups or more at once)", most >= 6, $"most at once {most}");
            yield return Clock(10f); yield return StartRoute("kennesaw_mountain.json", "forward", 0);
            respawn.PlaceAt(RaceRoute.Surface(-1110f, -5300f) + Vector3.up * .4f, 250f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f);
            yield return LookRound(RaceRoute.Surface(-1110f, -5300f)); yield return new WaitForSecondsRealtime(6f);
            report.AppendLine($"  in a race: {life.WalkersInfo}");
            Check("park: a few in a race (1 to 5 groups)", life.TrailWalkers >= 1 && life.TrailWalkers <= 5, $"{life.TrailWalkers}");
            race.EndRace();
        }

        // ------------------------------------------------------------ F1, F2, F3
        IEnumerator R70Hydrants()
        {
            int n = 0; var box = new Rect(-1700f, -6600f, 1300f, 1700f); var at = new List<Vector2>();
            foreach (var c in FindObjectsByType<HydrantChunk>(FindObjectsSortMode.None))
            {
                var fc = c.GetComponent<FenceChunk>(); if (!fc) continue;
                for (int i = 0; i < fc.panels.Length; i++) { var w = fc.transform.TransformPoint(fc.panels[i].centre); if (box.Contains(new Vector2(w.x, w.z))) { n++; at.Add(new Vector2(w.x, w.z)); } }
            }
            report.AppendLine(); report.AppendLine($"=== F1. Hydrants on Kennesaw Mountain (x -1700 to -400, z -6600 to -4900): {n}" + (n > 0 ? ": " + string.Join(" ", at.Take(40).Select(q => $"({q.x:0}, {q.y:0})")) : ""));
            Check("hyd: none on the mountain drive or in the park", n == 0, $"{n}");
            yield return null;
        }
        IEnumerator R70Roll()
        {
            report.AppendLine(); report.AppendLine("=== F2. The drive's roll across its width at Dan's BUG-004 (-949.83, -5725.88): rays onto the built ground every 4 m along the drive, 3.5 m either side of its middle");
            var pts = new[] { new Vector2(-1027f, -5790f), new Vector2(-996f, -5762f), new Vector2(-979f, -5747f), new Vector2(-954f, -5730f), new Vector2(-944f, -5724.6f) };
            float most = 0f; var sb = new StringBuilder();
            float Y(Vector2 q) => Physics.Raycast(new Vector3(q.x, 900f, q.y), Vector3.down, out var h, 900f, ~0, QueryTriggerInteraction.Ignore) ? h.point.y : float.NaN;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                var a = pts[i]; var b = pts[i + 1]; var dd = (b - a).normalized; var nn = new Vector2(-dd.y, dd.x); float L = (b - a).magnitude;
                for (float s = 0f; s < L; s += 4f)
                {
                    var c = a + dd * s; float yl = Y(c + nn * 3.5f), yr = Y(c - nn * 3.5f); float deg = Mathf.Atan2(yl - yr, 7f) * Mathf.Rad2Deg; most = Mathf.Max(most, Mathf.Abs(deg));
                    sb.Append($"({c.x:0}, {c.y:0}) {deg:0.0} deg; ");
                }
            }
            report.AppendLine("  " + sb); report.AppendLine($"  the most roll: {most:0.0} degrees");
            Check("roll: the drive level across its width there (under 4 degrees)", most < 4f, $"{most:0.0} degrees");
            yield return null;
        }
        IEnumerator R70Gantries()
        {
            report.AppendLine(); report.AppendLine("=== F3. Every course's start / finish gantry: a picture of each from 30 m back on the lap");
            foreach (var (file, nm) in race.Routes())
            {
                yield return StartRoute(file, "forward", 0, 1); if (race.Route == null) continue;
                var g = race.Route.Gate; var at = g.Position - g.Forward * 30f + Vector3.up * 3f;
                yield return Still("70_gantry_" + Path.GetFileNameWithoutExtension(file), at, g.Position + Vector3.up * 4f, 65f);
                var root = GameObject.Find("Start-finish gate"); string posts = "";
                if (root) { var cubes = root.GetComponentsInChildren<Renderer>(); if (cubes.Length > 0) posts = $"{cubes.Length} blocks, its top {cubes.Max(c => c.bounds.max.y) - g.Position.y:0.0} m over the line"; }
                report.AppendLine($"  {nm}: {posts}");
                race.EndRace(); RaceController.DirectionOverride = null;
            }
        }
    }
}
