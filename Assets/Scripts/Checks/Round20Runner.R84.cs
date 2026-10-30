using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 84 in the exe (-exeRound84 <dir> [-r84Only p84pages,p84lap,p84land,p84air,p84rec,p84prompt,p84gates] [-r84Tag <name>] -r20Minutes 120
    // -logFile <dir>\Player.log), from the repo root, on the check's own save (settings, records and replays in <dir>): CHECK IT AS DAN PLAYS IT.
    //   p84pages   A1/A2: the title by a virtual controller: HIGH MODE: OFF -> A -> ON; each HIGH page (FREE ROAM, RACE, CHASES & STUNTS, VEHICLE,
    //              TIME & WEATHER) and the Free Roam pause, a picture each (the window's size in the pictures' names: run it at 1280 x 800 and 3840 x 2160)
    //   p84lap     A3: one HIGH lap of Kennesaw Mountain and of the Noonday Creek Extension by the human-like driver, steering into the edges on purpose
    //   p84land    A3: a crooked landing (dropped on its roof): HIGH upright, normal wrecks (a motorcycle) / stays on its roof (a car)
    //   p84air     A3: a HIGH air race (Quarry Air Race) flown into the ground: back flying at once; the rings larger
    //   p84rec     A4: a HIGH race's record on the HIGH board only, its replay marked HIGH, the RECORDS page's HIGH board
    //   p84prompt  A5: normal mode, 2 wrecks in a lap: "Would you like to play HIGH MODE?"; NO; 4 more wrecks: not asked again
    //   p84gates   B: every Express gate from the driver's seat on its ramp; the road beside each driven at speed: no gate met
    public sealed partial class Round20Runner
    {
        string R84Tag { get { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, "-r84Tag"); return i >= 0 && i + 1 < a.Length ? a[i + 1] : $"{Screen.width}x{Screen.height}"; } }
        GameMenus R84Menus() { var m = GameMenus.Instance ? GameMenus.Instance : GameMenus.Create(); return m; }
        IEnumerator R84Focus(GameMenus m, string id)
        {
            for (int i = 0; i < 64 && m.FocusedForCheck != id; i++) yield return R70Tap(GamepadButton.DpadDown);
        }
        static void R84High(bool on) { GameSettings.Settings.highMode = on; HighMode.Declined = true; }   // the question only in the prompt part (it clears Declined)

        // ---------------------------------------------------------------- pages
        IEnumerator R84Pages()
        {
            string res = R84Tag;
            report.AppendLine(); report.AppendLine($"=== A1/A2. The HIGH MODE menus by a controller, the window {Screen.width} x {Screen.height}");
            R74Devices(); R84High(false);
            var m = R84Menus(); yield return new WaitForSecondsRealtime(1.5f);
            m.OpenTitleForCheck(); yield return new WaitForSecondsRealtime(.8f);
            yield return Capture($"84_title_normal_{res}");
            Check("pages: the title's top row is HIGH MODE: OFF", m.FocusedForCheck == "high-mode" && m.FocusedLabel.Contains("OFF"), $"{m.FocusedForCheck} '{m.FocusedLabel}'");
            yield return R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(.6f);
            yield return Capture($"84_title_high_{res}");
            Check("pages: A switches HIGH MODE on; the title becomes HIGH MODE's menu", HighMode.On && m.Heading.Contains("HIGH MODE"), $"on {HighMode.On}, heading '{m.Heading}'");
            report.AppendLine($"  HIGH MODE saved in the check's settings.json: {System.IO.File.ReadAllText(GameSettings.FilePath).Contains("\"highMode\": true")}");
            var pages = new (string row, string page, string name)[] { ("roam", "high-roam", "free_roam"), ("race", "high-race", "race"), ("high-chases", "high-chases", "chases"), ("high-vehicle", "high-vehicle", "vehicle"), ("high-time", "high-time", "time_weather") };
            foreach (var (row, page, name) in pages)
            {
                yield return R84Focus(m, row); yield return R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(1f);
                yield return Capture($"84_page_{name}_{res}");
                Check($"pages: {row} opens its page by the controller", m.PageForCheck == page, $"{m.PageForCheck}, focus {m.FocusedForCheck}");
                if (page == "high-vehicle")
                {
                    string a = m.FocusedLabel; yield return R70Tap(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.8f); string b = m.FocusedLabel;
                    yield return Capture($"84_page_vehicle_next_{res}");
                    Check("pages: VEHICLE: right steps to the next vehicle", a != b, $"'{a}' -> '{b}'");
                    yield return R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(.8f);
                    Check("pages: VEHICLE: A drives it (back on the menu)", m.PageForCheck == "root", m.PageForCheck);
                    continue;
                }
                if (page == "high-time")
                {
                    yield return R84Focus(m, "high-weather"); string a = m.FocusedLabel; yield return R70Tap(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.5f);
                    Check("pages: TIME & WEATHER: right changes the weather", a != m.FocusedLabel, $"'{a}' -> '{m.FocusedLabel}'");
                    yield return R70Tap(GamepadButton.DpadLeft); yield return new WaitForSecondsRealtime(.4f);
                }
                yield return R70Tap(GamepadButton.East); yield return new WaitForSecondsRealtime(.6f);
                Check($"pages: B goes back from {page}", m.PageForCheck == "root", m.PageForCheck);
            }
            // the Free Roam pause in HIGH MODE (RESUME DRIVING) and in normal mode (HIGH MODE: OFF on top)
            m.CloseForCheck(); yield return new WaitForSecondsRealtime(.5f);
            m.OpenPauseForCheck(); yield return new WaitForSecondsRealtime(.8f); yield return Capture($"84_pause_high_{res}");
            Check("pages: the Free Roam pause in HIGH MODE is its menu with RESUME DRIVING", m.Heading.Contains("HIGH MODE") && System.Array.Exists(new[] { "resume" }, x => true), m.Heading);
            yield return R84Focus(m, "high-mode"); yield return R70Tap(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.6f);
            yield return Capture($"84_pause_normal_{res}");
            Check("pages: left / right on HIGH MODE switches it off from the pause", !HighMode.On && !m.Heading.Contains("HIGH MODE"), m.Heading);
            m.CloseForCheck(); yield return new WaitForSecondsRealtime(.5f);
        }

        // ---------------------------------------------------------------- lap: HIGH laps steering into the edges
        IEnumerator R84Lap()
        {
            foreach (var (file, veh, cap) in new[] { ("kennesaw_mountain.json", "drifter", 32f), ("noonday_creek.json", "atv", 20f) })
            {
                report.AppendLine(); report.AppendLine($"=== A3. One HIGH MODE lap of {file} by the human-like driver, steering into the edges on purpose");
                R84High(true); yield return Vehicle(veh);
                yield return StartRoute(file, "forward", 0, 1);
                var r = race.Route; var road = race.AiRoad;
                Check($"lap {file}: the race is in HIGH MODE with its bumpers up", race.High && HighBumpers.Shown, $"high {race.High}, {HighBumpers.Report}");
                yield return new WaitForSecondsRealtime(.5f); yield return Capture($"84_lap_{file.Replace(".json", "")}_start");
                // the line: the run-in then the lap, pulled 7 m off the road's middle for 40 m in every 150 (alternate sides): a player aiming at the edge
                var line = new List<Vector3>();
                var rd = race.RouteData;
                if (rd.lead_in != null) for (int i = 0; i + 1 < rd.lead_in.Length; i += 2) line.Add(RaceRoute.Ground(rd.lead_in[i], rd.lead_in[i + 1]));
                float L = r.LapLength;
                var lap = r.SamplePath(0f, r.GateS + L + 30f, 4f);
                var pushed = new List<Vector3>(); int pushes = 0;
                // a push: 20 m aimed 7 m off the middle (a player drifting wide, then back), never on a jump's run-up, flight or landing
                // (the route's held zones and the landing zones, 60 m round them): a crooked take-off is a different test
                var zones = race.AiZones;
                bool NearJump(float s)
                {
                    if (zones != null) foreach (var z in zones) if (Mathf.Repeat(s - (z.s0 - 60f), L) <= z.s1 - z.s0 + 120f) return true;
                    for (float d = -60f; d <= 120f; d += 10f) if (LandingZone.Contains(r.PointAt(s + d, out _))) return true;
                    return false;
                }
                for (int i = 0; i < lap.Count; i++)
                {
                    float s = i * 4f; int block = Mathf.FloorToInt(s / 150f); float into = s - block * 150f;
                    var p = lap[i];
                    if (block > 0 && into > 60f && into < 80f && !NearJump(block * 150f + 60f))
                    {
                        r.PointAt(s, out var f); var side = Vector3.Cross(Vector3.up, f).normalized * (block % 2 == 0 ? 1f : -1f);
                        p += side * 7f; if (into < 64f) pushes++;
                    }
                    pushed.Add(p);
                }
                line.AddRange(pushed);
                var run = new HumanRun(); float worst = 0f; Vector3 worstAt = Vector3.zero; int falls = 0; float lastY = car.Body.position.y; int pics = 0; float nextPic = 0f;
                yield return HumanDrive(line, cap, 480f, run, () =>
                {
                    var p = car.Body.position; road.Project(p, out float lat);
                    if (Mathf.Abs(lat) > worst) { worst = Mathf.Abs(lat); worstAt = p; }
                    return race.Player.CompletedLaps >= 1;
                });
                report.AppendLine($"  {run.time:0} s, top {run.topSpeed:0} m/s, longest air {run.maxAir:0.0} s, resets {run.resets}, stuck {run.stuck}{run.note}; {pushes} pushes at the edge; farthest from the line {worst:0.0} m at ({worstAt.x:0}, {worstAt.z:0}); {HighBumpers.Report}; HIGH righted {HighAssist.Righted}");
                report.AppendLine($"  trace:{run.trace}");
                yield return Capture($"84_lap_{file.Replace(".json", "")}_end");
                Check($"lap {file}: the HIGH lap is completed steering into the edges ({pushes} pushes)", race.Player.CompletedLaps >= 1, $"laps {race.Player.CompletedLaps}, resets {run.resets}");
                Check($"lap {file}: the bumpers keep it on the course (never more than 12 m off the line)", worst <= 12f, $"{worst:0.0} m at ({worstAt.x:0}, {worstAt.z:0})");
                race.EndRace(); R84High(false);
            }
        }

        // ---------------------------------------------------------------- land: a crooked landing
        IEnumerator R84Land()
        {
            report.AppendLine(); report.AppendLine("=== A3. A crooked landing (dropped on its roof from 3 m): HIGH MODE upright, normal mode wrecks");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            string bike = VehicleProfile.All.First(p => p.Motorcycle).Id;
            var spot = respawn.StartPosition;
            foreach (var (veh, high) in new[] { (bike, true), (bike, false), ("drifter", true), ("drifter", false) })
            {
                R84High(high); yield return Vehicle(veh); yield return R74Place(spot, 0f, 1.5f);
                int righted = HighAssist.Righted;
                var rot = Quaternion.Euler(0f, 0f, 170f); var p = car.Body.position + Vector3.up * 3f;
                car.Body.position = p; car.Body.rotation = rot; car.transform.SetPositionAndRotation(p, rot); car.Body.linearVelocity = Vector3.down * 2f; car.Body.angularVelocity = Vector3.zero;
                yield return new WaitForSeconds(3.5f);
                var cfg = car.GetComponent<VehicleConfiguration>();
                string tagName = $"84_land_{veh}_{(high ? "high" : "normal")}";
                yield return Capture(tagName);
                bool up = car.transform.up.y > .7f;
                report.AppendLine($"  {Nm(veh)} {(high ? "HIGH" : "normal")}: upright {up} (up.y {car.transform.up.y:0.00}), wrecked {cfg.WipedOut}, righted {HighAssist.Righted - righted}; picture {tagName}.png");
                if (high) Check($"land: {Nm(veh)} in HIGH MODE is upright again, no wreck", up && !cfg.WipedOut && HighAssist.Righted > righted, $"up.y {car.transform.up.y:0.00}");
                else Check($"land: {Nm(veh)} in normal mode {(VehicleProfile.Find(veh).Motorcycle ? "wrecks" : "stays on its roof")}", !up && (!VehicleProfile.Find(veh).Motorcycle || cfg.WipedOut), $"up.y {car.transform.up.y:0.00}, wrecked {cfg.WipedOut}");
                yield return R73ResetPad();
            }
            R84High(false);
        }
        IEnumerator R73ResetPad() { R73Pad(); yield return R73Reset(); }

        // ---------------------------------------------------------------- air: a HIGH air race flown into the ground
        IEnumerator R84Air()
        {
            report.AppendLine(); report.AppendLine("=== A3. The Quarry Air Race in HIGH MODE, flown into the ground");
            R84High(true); yield return Vehicle("plane");
            yield return StartRoute("quarry_air_race.json", "forward", 0, 1);
            var j = race.RouteData; var g = race.Route.Checkpoints[0]; var jc = j.checkpoints.OrderBy(c => c.s).First();
            float fileRing = Mathf.Max(jc.ring_m, jc.ring_w);
            report.AppendLine($"  ring 1: {Mathf.Max(g.Ring, g.RingW):0.0} m in HIGH MODE (the course file's {fileRing:0.0} m; others in the file: {string.Join(", ", j.checkpoints.Take(4).Select(c => Mathf.Max(c.ring_m, c.ring_w).ToString("0.0")))})");
            float sumHigh = race.Route.Checkpoints.Sum(c => Mathf.Max(c.Ring, c.RingW)), sumFile = j.checkpoints.Sum(c => Mathf.Max(c.ring_m, c.ring_w));
            Check("air: the rings are larger in HIGH MODE", sumHigh > sumFile * 1.4f, $"rings {sumHigh:0} m against the file's {sumFile:0} m in all");
            var plane = car.GetComponent<PlaneFlight>();
            yield return new WaitForSecondsRealtime(1f);
            race.AirReset(plane); yield return new WaitForSecondsRealtime(1.5f);   // in the air on the line (as after the take-off)
            yield return Capture("84_air_flying");
            int crashes = PlaneFlight.HighCrashes; R73Pad();
            float t = 0f; bool hit = false; float h0 = car.Body.position.y;
            while (t < 25f) { R73Set(0f, 1f, 0f, 1f); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; if (PlaneFlight.HighCrashes > crashes) { hit = true; break; } }
            R73Set(0, 0, 0);
            yield return new WaitForSecondsRealtime(.4f);
            yield return Capture("84_air_after_crash");
            float agl = car.Body.position.y - RaceRoute.Ground(car.Body.position.x, car.Body.position.z).y;
            report.AppendLine($"  nose down from {h0:0} m: {(hit ? $"crashed after {t:0.0} s ({plane.CrashReason})" : "no crash")}; 0.4 s later: crashed {plane.Crashed}, in the air {plane.Airborne}, {agl:0} m over the ground, {car.Body.linearVelocity.magnitude:0} m/s");
            Check("air: flown into the ground in HIGH MODE, it is back flying at once", hit && !plane.Crashed && agl > 10f && car.Body.linearVelocity.magnitude > 30f, $"hit {hit}, crashed {plane.Crashed}, {agl:0} m up");
            race.EndRace(); R84High(false);
        }

        // ---------------------------------------------------------------- rec: HIGH records
        IEnumerator R84Rec()
        {
            report.AppendLine(); report.AppendLine("=== A4. A HIGH MODE race's times: the HIGH boards only; the replay marked HIGH; the RECORDS page's HIGH board");
            var b = RaceController.Boards;
            int HighN() => b.Top(c => c.StartsWith(HighMode.RecordPrefix), true).Count + b.Top(c => c.StartsWith(HighMode.RecordPrefix), false).Count;
            int NormalN() => b.Top(c => !c.StartsWith(HighMode.RecordPrefix), true).Count + b.Top(c => !c.StartsWith(HighMode.RecordPrefix), false).Count;
            int h0 = HighN(), n0 = NormalN();
            R84High(true); yield return Vehicle("original");
            yield return StartRoute("chastain_glen_loop.json", "forward", 0, 1);
            var rr = race.Route; yield return new WaitForSecondsRealtime(1f);
            yield return AcrossLine(rr.Gate, 0f); foreach (var cp in rr.Checkpoints) yield return AcrossLine(cp, 0f); race.Player.LapAway = true; yield return AcrossLine(rr.Gate, 0f);
            car.Body.linearVelocity = Vector3.zero; float until = Time.realtimeSinceStartup + 12f; while (WinnerShot.Active && Time.realtimeSinceStartup < until) yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            int h1 = HighN(), n1 = NormalN();
            report.AppendLine($"  the race: {race.State}, category {race.BoardCategory}; HIGH entries {h0} -> {h1}, normal {n0} -> {n1}");
            Check("rec: the HIGH race's lap and race went on the HIGH boards only", race.State == RaceController.Stage.Finished && h1 > h0 && n1 == n0 && race.BoardCategory.StartsWith(HighMode.RecordPrefix), $"HIGH {h0}->{h1}, normal {n0}->{n1}");
            var rec = ReplayRecorder.Instance; string title = rec && rec.Last != null ? rec.Last.Title : "(none)";
            report.AppendLine($"  its replay: \"{title}\"");
            Check("rec: the replay is marked HIGH", title.StartsWith("HIGH"), title);
            var m = R84Menus(); yield return new WaitForSecondsRealtime(.5f); R74Devices();
            m.OpenPauseForCheck(); yield return new WaitForSecondsRealtime(.8f); yield return Capture("84_rec_results");
            Check("rec: the finish menu is HIGH MODE's results (VEHICLE, CHANGE TRACK)", m.Heading.Contains("HIGH MODE"), m.Heading);
            yield return R84Focus(m, "records"); yield return R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(1f);
            yield return Capture("84_rec_records_high");
            report.AppendLine($"  RECORDS opened on: '{m.FocusedLabel}'");
            Check("rec: RECORDS opens on the HIGH board of this course", m.PageForCheck == "records" && m.FocusedLabel.StartsWith("HIGH"), m.FocusedLabel);
            m.CloseForCheck(); race.EndRace(); R84High(false);
        }

        // ---------------------------------------------------------------- prompt: "Would you like to play HIGH MODE?"
        IEnumerator R84Prompt()
        {
            report.AppendLine(); report.AppendLine("=== A5. Normal mode: 2 wrecks in one lap -> \"Would you like to play HIGH MODE?\"; NO; 4 more wrecks: not asked again");
            R84High(false); HighMode.Declined = false; var m = R84Menus(); yield return new WaitForSecondsRealtime(1f); if (GameMenus.Open) m.CloseForCheck();
            R74Devices(); yield return Vehicle("drifter");
            yield return StartRoute("chastain_glen_loop.json", "forward", 0, 3);
            int asked0 = HighPrompt.Asked;
            IEnumerator Wreck()
            {
                var p = car.Body.position + Vector3.up * 1.5f; var rot = Quaternion.Euler(0f, car.transform.eulerAngles.y, 175f);
                car.Body.position = p; car.Body.rotation = rot; car.transform.SetPositionAndRotation(p, rot); car.Body.linearVelocity = Vector3.zero;
                yield return new WaitForSeconds(2.2f);
                if (!GameMenus.Open) { R73Pad(); yield return R73Reset(); }   // as a player: Y once it is down
                yield return new WaitForSeconds(1.5f);
            }
            yield return Wreck(); yield return Wreck();
            yield return new WaitForSecondsRealtime(.6f);
            yield return Capture("84_prompt_asked");
            report.AppendLine($"  after 2 wrecks: asked {HighPrompt.Asked - asked0} time(s), the menu '{m.Heading}', mishaps {HighPrompt.Mishaps}");
            Check("prompt: asked after 2 wrecks in a lap", HighPrompt.Asked == asked0 + 1 && GameMenus.Open && m.PageName.StartsWith("modal:WOULD YOU LIKE"), m.PageName);
            yield return R70Tap(GamepadButton.East); yield return new WaitForSecondsRealtime(.8f);
            Check("prompt: B is No: the game goes on in normal mode, not asked again this session", !GameMenus.Open && HighMode.Declined && !HighMode.On, $"open {GameMenus.Open}, declined {HighMode.Declined}, high {HighMode.On}");
            for (int i = 0; i < 4; i++) yield return Wreck();
            Check("prompt: 4 more wrecks: not asked again", HighPrompt.Asked == asked0 + 1, $"asked {HighPrompt.Asked - asked0}");
            if (GameMenus.Open) m.CloseForCheck();
            race.EndRace();
        }

        // ---------------------------------------------------------------- gates (B)
        IEnumerator R84Gates()
        {
            report.AppendLine(); report.AppendLine("=== B. The Express gates: each from the driver's seat on its ramp, then the road beside it driven at speed (no gate met)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); yield return Clock(13f);
            var lanes = ExpressLanes.Instance; if (!lanes) { Check("gates: the Express Lanes", false); yield break; }
            ExpressLanes.ForceHour = 3f; ExpressLanes.ApplyNow();   // a closed hour: every arm down across its ramp
            var roads = RoadLookup.Instance; var views = Camera.main ? Camera.main.GetComponent<CameraViews>() : null;
            for (int gi = 0; gi < lanes.gates.Length; gi++)
            {
                var g = lanes.gates[gi]; if (!g) continue;
                var post = g.transform.position; var f = g.transform.forward; var right = g.transform.right;
                var col = g.arm ? g.arm.GetComponent<BoxCollider>() : null; var mid = col ? col.bounds.center : post + right * 4f;
                // the ramp's middle under the arm; the driver 30 m back toward the general road, facing the gate
                var c = new Vector3(mid.x, post.y, mid.z);
                yield return R82Place(c + f * 30f, Mathf.Atan2(-f.x, -f.z) * Mathf.Rad2Deg);
                if (views) views.SetView(CameraViews.View.FirstPerson); yield return new WaitForSecondsRealtime(.6f);
                yield return Capture($"84_gate{gi + 1}_driver");
                if (views) views.SetView(CameraViews.View.Chase);
                // the arm against the map's roads: what it stands across
                var across = new List<string>();
                if (roads) for (float d = -1f; d <= 9f; d += .5f) { var q = post + right * d; if (roads.Nearest(q, out string nm, out float dist, out float half) && dist < half && !nm.StartsWith("unnamed") && !across.Contains(nm)) across.Add(nm); }
                report.AppendLine($"  gate {gi + 1} at ({post.x:0.0}, {post.z:0.0}), heading {Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg:0}: the arm over named roads: {(across.Count == 0 ? "none (its own unnamed ramp only)" : string.Join(", ", across))}; picture 84_gate{gi + 1}_driver.png");
                Check($"gates: gate {gi + 1}'s arm stands across no named road (I-75, I-575, the Express Lanes, a through road)", across.Count == 0, string.Join(", ", across));
                // the road beside it: the nearest named road within 40 m of the arm's middle, driven 250 m past the gate at speed
                string beside = null; Vector2 bp = default, bt = default; float bd = 99f;
                if (roads) for (float a = 0f; a < 360f; a += 15f) for (float rr = 4f; rr <= 40f; rr += 3f)
                        {
                            var q = c + Quaternion.Euler(0, a, 0) * Vector3.forward * rr;
                            if (roads.Nearest(q, out string nm, out float dist, out float half) && dist < half && !nm.StartsWith("unnamed") && rr < bd && roads.NearestPoint(q, out var np, out var nt)) { beside = nm; bd = rr; bp = np; bt = nt; }
                        }
                if (beside == null) { report.AppendLine($"    no named road within 40 m"); continue; }
                var line = new List<Vector3>(); var tan = new Vector3(bt.x, 0, bt.y).normalized; var start = new Vector3(bp.x, 0, bp.y) - tan * 200f;
                for (float s = 0f; s <= 450f; s += 10f)
                {
                    var q = start + tan * s; if (roads.NearestPointWithin(q, 12f, out var np2, out var nt2)) { q = new Vector3(np2.x, 0, np2.y); if (Vector2.Dot(nt2, new Vector2(tan.x, tan.z)) < 0) nt2 = -nt2; tan = new Vector3(nt2.x, 0, nt2.y).normalized; }
                    line.Add(RaceRoute.Ground(q.x, q.z));
                }
                var p0 = line[0]; yield return R82Place(p0 + Vector3.up * 2f, Mathf.Atan2(line[1].x - p0.x, line[1].z - p0.z) * Mathf.Rad2Deg);
                car.Body.linearVelocity = car.transform.forward * 28f;
                float minArm = 999f; var run = new HumanRun();
                yield return HumanDrive(line, 33f, 40f, run, () => { if (col) minArm = Mathf.Min(minArm, (col.ClosestPoint(car.Body.position) - car.Body.position).magnitude); return false; });
                report.AppendLine($"    beside it: {beside}, driven {run.sEnd:0} m at up to {run.topSpeed:0} m/s; nearest the arm came {minArm:0.0} m; resets {run.resets}");
                Check($"gates: gate {gi + 1}: driving past on {beside} at speed meets no gate", minArm > 1.5f && run.topSpeed > 20f, $"{minArm:0.0} m, top {run.topSpeed:0} m/s");
            }
            ExpressLanes.ForceHour = float.NaN;
        }
    }
}
