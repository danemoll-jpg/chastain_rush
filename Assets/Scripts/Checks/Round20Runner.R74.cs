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
    // Round 74 in the exe (-exeRound74 <dir> [-r74Only dan,clouds,plane,preplay,controls,arrow,map,qpreview,forks,hfly] -logFile <dir>\Player.log):
    // CHECK IT AS DAN PLAYS IT (TODO.md item 0), the parts Round 73's runner does not already cover (the summit leap, the quarry jumps, the quarry track and
    // the AI races run as -exeRound73 -r73Only leap,qroad,qjump,qtrack,allraces -r73Lines local/round74/r74_lines.json; the highway air race's probe, line
    // and AI race as -exeRound71 -r71Route highway_air_race.json).
    //   dan       Dan's four reports (session 2026-10-27_02-23-23-098_3e251a) from his exact position and heading: BUG-002 in the Kennesaw Mountain race,
    //             BUG-003 to 005 in Free Roam; a picture each, what stands in the 60 m ahead
    //   clouds    the summit: the clouds' layer over the highest ground near the camera (SkyClouds.Raise), a picture past the lip
    //   plane     the plane stands (the Sky Hopper, the Sky Dart), each plane from the chase camera (the pilot seen through the glass), Free Roam's
    //             garage on the plane rows (two colors), race setup's Garage for the air races (the route's planes only)
    //   preplay   a short Quarry Air Race flown by the AI pilot's code, then its replay: the puppet's propeller turning (angles over a second)
    //   controls  the map, the race HUD and a loading tip with the virtual pad, then with the virtual keyboard (pictures; the texts)
    //   arrow     a race lap's HUD arrow driven by the human-like driver: how far it moves frame to frame against the raw bearing
    //   map       the map open at the airport (pictures), the minimap there
    //   qpreview  each quarry jump's countdown from the box: the preview camera's picture
    //   forks     the Kennesaw Mountain race: Dan's CP5 fork in the race (picture), a few of the new fork barriers on other courses (pictures)
    public sealed partial class Round20Runner
    {
        Gamepad r74pad; Keyboard r74keys;
        void R74Devices()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            if (r74pad == null || !r74pad.added) r74pad = InputSystem.AddDevice<Gamepad>("Round74 pad");
            if (r74keys == null || !r74keys.added) r74keys = InputSystem.AddDevice<Keyboard>("Round74 keys");
            r70pad = r74pad; r70keys = r74keys;   // (Round 70's menu helpers press these)
        }
        IEnumerator R74Place(Vector3 at, float yaw, float settle = 2.5f)
        {
            var p = RaceRoute.Surface(at.x, at.z); respawn.PlaceAt(p + Vector3.up * .3f, yaw); car.Body.linearVelocity = Vector3.zero; race.NotifyTeleported();
            var cam = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cam) cam.Snap();
            yield return new WaitForSecondsRealtime(settle);
        }
        string R74Ahead(Vector3 p, float yaw)
        {
            var hits = Physics.OverlapBox(p + Quaternion.Euler(0, yaw, 0) * Vector3.forward * 30f + Vector3.up * 1.2f, new Vector3(5f, .8f, 30f), Quaternion.Euler(0, yaw, 0), ~0, QueryTriggerInteraction.Ignore)
                .Where(c => !(c is TerrainCollider) && c.attachedRigidbody != car.Body).Select(c => c.name).Distinct().ToList();
            return hits.Count == 0 ? "nothing" : string.Join(", ", hits.Take(8));
        }

        // ---------------------------------------------------------------- dan
        IEnumerator R74Dan()
        {
            report.AppendLine(); report.AppendLine("=== Dan's four reports, from his exact position and heading (the chase camera, as he saw it)");
            var spots = new (string id, Vector3 at, float yaw, bool race, string what)[]
            {
                ("BUG-002", new Vector3(-1102.78f, 0f, -5849.19f), 68.8f, true, "Kennesaw Mountain race, the drive before CP5: the road on the right needs a barrier"),
                ("BUG-003", new Vector3(-1996.33f, 0f, -1390.14f), 226.3f, false, "The Long One's landing deck: the hill through it"),
                ("BUG-004", new Vector3(-2128.42f, 0f, -1443.31f), 190.4f, false, "The Big One's landing deck: blocked, where does it lead"),
                ("BUG-005", new Vector3(-1068.06f, 0f, -5968.78f), 209.2f, false, "the summit leap's lip: makes you turn; the white shape"),
            };
            yield return Vehicle("drifter"); yield return Clock(12f);
            foreach (var sp in spots)
            {
                if (sp.race) { UseRoute("kennesaw_mountain.json", "forward"); RaceController.AiCount = 0; race.StartRace(); yield return new WaitForSecondsRealtime(1f); }
                else if (race.State != RaceController.Stage.Free) race.EndRace();
                yield return R74Place(sp.at, sp.yaw);
                yield return Capture($"74_{sp.id}_after");
                var p = car.Body.position;
                report.AppendLine($"  {sp.id} ({sp.what}): at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}) heading {sp.yaw:0}; standing in the 60 m ahead: {R74Ahead(p, sp.yaw)}; picture 74_{sp.id}_after.png");
                if (sp.id == "BUG-003" || sp.id == "BUG-004")
                {
                    // the ground against the deck along the line ahead (the deck's top, the terrain's): nothing of the ground through it
                    var q = QuarryJumps.Instance; var j = q ? q.Jumps.FirstOrDefault(x => x.id == (sp.id == "BUG-003" ? "J2" : "J3")) : null;
                    if (j != null)
                    {
                        float worst = -99f; string at = "";
                        for (int k = 0; k < j.land_s.Length; k++)
                            for (float lat = -j.land_half + .5f; lat <= j.land_half - .5f; lat += 1f)
                            {
                                var w = QuarryJumps.At(j, j.land_s[k], lat); var t = Terrain.activeTerrains.FirstOrDefault(tt => { var tp = tt.transform.position; var sz = tt.terrainData.size; return w.x >= tp.x && w.x <= tp.x + sz.x && w.z >= tp.z && w.z <= tp.z + sz.z; });
                                if (!t) continue; float gy = t.SampleHeight(w) + t.transform.position.y; float over = gy - j.land_y[k];
                                if (over > worst) { worst = over; at = $"s {j.land_s[k]:0} lat {lat:0}"; }
                            }
                        report.AppendLine($"    {j.id} {j.name}'s landing deck: the ground at most {worst:+0.00;-0.00} m against the deck's top ({at})");
                        Check($"dan {sp.id}: no ground through {j.name}'s landing deck", worst < .05f, $"{worst:0.00} m at {at}");
                    }
                }
                if (sp.race) race.EndRace();
            }
        }

        // ---------------------------------------------------------------- clouds
        IEnumerator R74Clouds()
        {
            report.AppendLine(); report.AppendLine("=== The clouds over the summit (Dan's BUG-005: the white shape was the cloud layer at the summit's height)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter");
            var clouds = FindAnyObjectByType<SkyClouds>();
            foreach (var (name, at, yaw) in new[] { ("summit lip", new Vector3(-1068.06f, 0f, -5968.78f), 209.2f), ("Chastain Glen", new Vector3(2900f, 0f, 1000f), 0f) })
            {
                yield return R74Place(at, yaw, 3f);
                float raise = 0f; var cam = Camera.main.transform.position;   // (Round 75: the layer no longer rises)
                float lowest = float.MaxValue; if (clouds) foreach (Transform t in clouds.transform) if (t.gameObject.activeSelf && t.name == "Cloud") lowest = Mathf.Min(lowest, t.position.y - t.localScale.y * .15f);
                report.AppendLine($"  {name}: the camera at {cam.y:0} m; the layer raised {raise:0} m; the lowest cloud's base about {lowest:0} m ({lowest - cam.y:+0;-0} m from the camera)");
                if (name == "summit lip") { yield return Capture("74_clouds_summit"); Check("clouds: over the summit, the lowest cloud stays over the camera", lowest > cam.y + 60f, $"{lowest - cam.y:0} m"); }
            }
        }

        // ---------------------------------------------------------------- plane
        IEnumerator R74Plane()
        {
            report.AppendLine(); report.AppendLine("=== The planes: the stands, each plane with its pilot seen from the chase camera, the garage (Free Roam's, race setup's for the air race), colors");
            R74Devices();
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(12f);
            var st = PlaneStands.Instance;
            report.AppendLine($"  plane stands: {(st ? st.Spots.Count : 0)}; on them: " + (st ? string.Join(", ", Enumerable.Range(0, st.Spots.Count).Select(i => Nm(PlaneStands.PlaneOn(i)))) : "none"));
            Check("plane: the Sky Dart stands on the apron with the Sky Hopper", st && Enumerable.Range(0, st.Spots.Count).Any(i => PlaneStands.PlaneOn(i) == "dart"));
            foreach (var id in new[] { "plane", "dart", "heli" })
            {
                yield return Vehicle(id); var pl = car.GetComponent<PlaneFlight>(); if (pl && pl.enabled) pl.LineUp(true); else pl = null;
                yield return new WaitForSecondsRealtime(2f); var cam = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cam) cam.Snap(); yield return new WaitForSecondsRealtime(1f);
                yield return Capture($"74_plane_{id}_chase");
                var cfg = car.GetComponent<VehicleConfiguration>(); var rider = car.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Rider");
                var glass = car.GetComponentsInChildren<Renderer>(true).Count(r => r.sharedMaterial && r.sharedMaterial.name.StartsWith("VehicleGlazing"));
                report.AppendLine($"  {Nm(id)}: {(pl ? $"top speed {pl.Top:0} m/s, roll {pl.Roll:0} deg/s, pitch {pl.PitchR:0} deg/s" : "")}; the rider {(rider ? "aboard" : "MISSING")}, {glass} glass parts; picture 74_plane_{id}_chase.png");
                Check($"plane {Nm(id)}: the pilot aboard behind glass", rider && glass > 0);
                // from above and behind, closer (the pilot through the skylight / canopy)
                var b = car.Body.position; var f = car.transform.forward;
                yield return Still($"74_plane_{id}_pilot", b - f * 7f + Vector3.up * 4.5f, b + Vector3.up * .4f, 40f);
            }
            // Free Roam's garage: the plane rows, two colors
            var m = GameMenus.Instance ? GameMenus.Instance : GameMenus.Create(); yield return new WaitForSecondsRealtime(1f); if (m) m.CloseForCheck();
            yield return Vehicle("plane");
            m.OpenPauseForCheck(); yield return new WaitForSecondsRealtime(.6f);
            yield return R70Focus(m, "garage", false); yield return R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(.8f);
            report.AppendLine($"  Free Roam: pause > GARAGE: page {m.PageForCheck}, the vehicle {Nm(car.GetComponent<VehicleConfiguration>().profileId)}");
            yield return R70Focus(m, "colour", false);
            yield return R70Tap(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.6f); yield return Capture("74_garage_plane_color1");
            yield return R70Tap(GamepadButton.DpadRight); yield return R70Tap(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.6f); yield return Capture("74_garage_plane_color2");
            yield return R70Focus(m, "vehicle", false);
            var seen = new List<string>(); for (int i = 0; i < 24; i++) { var now = car.GetComponent<VehicleConfiguration>().profileId; if (!seen.Contains(now)) seen.Add(now); if (now == "dart") break; yield return R70Tap(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.25f); }
            yield return new WaitForSecondsRealtime(.6f); yield return Capture("74_garage_dart");
            report.AppendLine($"  Free Roam's garage stepped through: {string.Join(", ", seen.Select(Nm))}");
            Check("plane: Free Roam's garage has the Sky Dart and colors", seen.Contains("dart"));
            m.CloseForCheck(); yield return new WaitForSecondsRealtime(.4f);
            // race setup for the Quarry Air Race: Garage... holds the route's planes
            UseRoute("quarry_air_race.json", "forward"); yield return new WaitForSecondsRealtime(.5f);
            m.OpenPauseForCheck(); yield return new WaitForSecondsRealtime(.6f);
            yield return R70Focus(m, "race", false); yield return R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(.6f);
            yield return R70Focus(m, "garage", false); yield return R70Tap(GamepadButton.South); yield return new WaitForSecondsRealtime(.8f);
            string page = m.PageForCheck; yield return R70Focus(m, "vehicle", false);
            var inRace = new List<string>(); for (int i = 0; i < 6; i++) { var now = car.GetComponent<VehicleConfiguration>().profileId; if (!inRace.Contains(now)) inRace.Add(now); yield return R70Tap(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.25f); }
            yield return Capture("74_garage_air_race");
            report.AppendLine($"  race setup (Quarry Air Race) > Garage: page {page}; its vehicles: {string.Join(", ", inRace.Select(Nm))}");
            Check("plane: race setup's Garage for the air race holds the planes only", page == "garage" && inRace.All(v => VehicleProfile.Find(v).IsPlane) && inRace.Count >= 2, string.Join(",", inRace));
            m.CloseForCheck(); yield return new WaitForSecondsRealtime(.4f);
        }

        // ---------------------------------------------------------------- preplay: the propeller in a replay
        IEnumerator R74PropReplay()
        {
            report.AppendLine(); report.AppendLine("=== A race replay of the Quarry Air Race: the propeller turns (Dan: \"airplane - replay, the propeller doesn't move\")");
            yield return Vehicle("plane");
            var pilot = R71Fly(1, 1, 1); var ev = new List<string>();
            yield return R71Run("preplay", 70f, ev);   // a minute of the race is enough for a replay
            R71Done(pilot);
            var rec = ReplayRecorder.Instance; if (rec && rec.Recording) rec.Stop("the check"); yield return new WaitForSecondsRealtime(1f);
            var data = rec ? rec.Last : null;
            report.AppendLine($"  the replay: {(data != null ? data.Title + ", " + data.racers.Count + " racers: " + string.Join(", ", data.racers.Select(x => x.name + " " + x.vehicle)) : "none")}");
            if (data == null) { Check("preplay: a replay recorded", false); race.EndRace(); yield break; }
            var viewer = ReplayViewer.Open(data); yield return new WaitForSecondsRealtime(3f);
            var puppets = FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None).Where(c => c.Puppet).ToList();
            var angles = new List<string>(); int moving = 0;
            foreach (var pp in puppets)
            {
                var prop = pp.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Propeller"); if (!prop) { angles.Add($"{pp.name}: no propeller"); continue; }
                float a0 = prop.localEulerAngles.z; yield return new WaitForSecondsRealtime(.13f); float a1 = prop.localEulerAngles.z;
                angles.Add($"{pp.name}: {a0:0} -> {a1:0} deg in 0.13 s"); if (Mathf.Abs(Mathf.DeltaAngle(a0, a1)) > 1f) moving++;
            }
            yield return Capture("74_replay_propeller");
            report.AppendLine($"  {puppets.Count} puppets; " + string.Join("; ", angles));
            Check("preplay: every plane's propeller turns in the replay", puppets.Count > 0 && moving == puppets.Count, $"{moving} of {puppets.Count}");
            if (ReplayViewer.Current) ReplayViewer.Current.Close();
            yield return new WaitForSecondsRealtime(1f); race.EndRace();
        }

        // ---------------------------------------------------------------- controls
        IEnumerator R74Controls()
        {
            report.AppendLine(); report.AppendLine("=== Controls shown for the device in use (the map, the race HUD, the minimap, the loading tips), the pad then the keys");
            R74Devices();
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return R74Place(new Vector3(-2700f, 0f, -1950f), 90f, 1.5f);
            var map = WorldMap.Instance;
            foreach (bool pad in new[] { true, false })
            {
                if (pad) yield return R70Tap(GamepadButton.DpadUp); else { InputSystem.QueueStateEvent(r74keys, new KeyboardState(Key.LeftShift)); yield return null; yield return null; InputSystem.QueueStateEvent(r74keys, new KeyboardState()); yield return new WaitForSecondsRealtime(.3f); }
                string dev = pad ? "pad" : "keys";
                report.AppendLine($"  {dev}: the game sees {(KeyHints.Pad ? "the pad" : "the keyboard")}");
                Check($"controls ({dev}): the device in use follows the last input", KeyHints.Pad == pad);
                yield return Capture($"74_controls_{dev}_roam");   // the minimap's key, the debug line
                if (map) { map.Open(); yield return new WaitForSecondsRealtime(1.2f); yield return Capture($"74_controls_{dev}_map"); map.Close(); yield return new WaitForSecondsRealtime(.5f); }
                UseRoute("chastain_glen_loop.json", "forward"); yield return new WaitForSecondsRealtime(.6f); yield return Capture($"74_controls_{dev}_race_hint");
                report.AppendLine($"    race hint: \"{race.Hint}\"; minimap: \"{(MiniMap.Instance ? MiniMap.Instance.KeyText : "")}\"");
                Check($"controls ({dev}): the race hint names only {(pad ? "the pad's button" : "the key")}", pad ? race.Hint.Contains("Start") && !race.Hint.Contains("Esc") : race.Hint.Contains("Esc") && !race.Hint.Contains("Start"), race.Hint);
            }
        }

        // ---------------------------------------------------------------- arrow
        IEnumerator R74Arrow()
        {
            report.AppendLine(); report.AppendLine("=== The race's HUD arrow, steadied: a lap of the Chastain Glen loop by the human-like driver; the arrow's turn frame to frame against the raw bearing's");
            UseRoute("chastain_glen_loop.json", "forward"); yield return Vehicle("drifter");
            RaceController.LapsOverride = 1; RaceController.AiCount = 0; race.StartRace();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            var r = race.Route; var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]); for (float s = 0f; s <= r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
            float rawSum = 0f, shownSum = 0f; int n = 0; float lastRaw = float.NaN, lastShown = float.NaN, lastDr = 0f, lastDs = 0f;
            IEnumerator EveryFrame(Action each) { while (true) { yield return null; each(); } }   // (per frame: the arrow is drawn once a frame)
            var mon = StartCoroutine(EveryFrame(() =>
            {
                var t = race.Target; if (t == null) return;
                var to = t.Position - car.transform.position; to.y = 0f; var fw = car.transform.forward; fw.y = 0f;
                float raw = Vector3.SignedAngle(fw, to, Vector3.up), shown = race.GuideAngleForCheck;
                // the wobble: how much each step's turn differs from the step before's (a steady turn through a bend is no wobble)
                if (!float.IsNaN(lastRaw)) { float dr = Mathf.DeltaAngle(lastRaw, raw), ds = Mathf.DeltaAngle(lastShown, shown); rawSum += Mathf.Abs(dr - lastDr); shownSum += Mathf.Abs(ds - lastDs); lastDr = dr; lastDs = ds; n++; }
                lastRaw = raw; lastShown = shown;
            }));
            var run = new HumanRun();
            var pics = new List<(string, float)> { ("74_arrow_lap", r.LapLength * .4f) };
            yield return HumanDrive(line, 30f, 150f, run, () => race.Player.Finished, null, pics);
            StopCoroutine(mon);
            report.AppendLine($"  {n} frames: the raw bearing's wobble {rawSum / Mathf.Max(1, n):0.000} deg a step on average, the arrow's {shownSum / Mathf.Max(1, n):0.000} (a step's turn against the step before's)");
            Check("arrow: the HUD arrow wobbles far less than the raw bearing (steadied)", n > 100 && shownSum < rawSum * .4f, $"{shownSum:0} against {rawSum:0}");
            race.EndRace();
        }

        // ---------------------------------------------------------------- map
        IEnumerator R74Map()
        {
            report.AppendLine(); report.AppendLine("=== The map at the airport: the runways, taxiways and aprons as pavement");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return R74Place(new Vector3(-2800f, 0f, -1960f), 90f, 1.5f);
            yield return Capture("74_minimap_airport");
            var map = WorldMap.Instance; if (!map) { Check("map: the map", false); yield break; }
            map.Open(); yield return new WaitForSecondsRealtime(1.2f);
            map.ZoomForCheck(6f); yield return new WaitForSecondsRealtime(.8f); yield return Capture("74_map_airport");
            map.ZoomForCheck(2.5f); yield return new WaitForSecondsRealtime(.8f); yield return Capture("74_map_airport_wide");
            map.Close();
            // the picture's own pixels on the runway's middle and on the grass beside it
            var pic = Resources.Load<Texture2D>("WorldMapPicture");
            report.AppendLine($"  the map's picture {(pic ? pic.width + " x " + pic.height : "missing")}");
        }

        // ---------------------------------------------------------------- qpreview
        IEnumerator R74QPreview()
        {
            report.AppendLine(); report.AppendLine("=== The quarry jumps' countdown from the box: the preview camera over the line (the lake and the landing in view)");
            var q = QuarryJumps.Instance; if (!q) { Check("qpreview: the event", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f); R73Pad();
            foreach (var j in q.Jumps)
            {
                yield return R73Travel("quarry-jump-" + j.id.ToLower());
                float wait = 0f; bool shotHold = false, shotCount = false;
                while (wait < 8f && q.State != QuarryJumps.Phase.Run)
                {
                    R73Set(0, 0, .3f); yield return new WaitForFixedUpdate(); wait += Time.fixedDeltaTime;
                    if (!shotHold && q.State == QuarryJumps.Phase.Hold) { shotHold = true; yield return Capture($"74_q{j.id}_preview_hold"); }
                    if (!shotCount && q.State == QuarryJumps.Phase.Countdown && q.CountdownForCheck > 1.6f) { shotCount = true; yield return Capture($"74_q{j.id}_preview_countdown"); }
                }
                yield return new WaitForSecondsRealtime(.4f); yield return Capture($"74_q{j.id}_go");
                report.AppendLine($"  {j.id} {j.name}: {(shotHold ? "held" : "(the hold passed during the travel)")}, {(shotCount ? "the countdown pictured" : "no countdown")}, now {q.State}");
                Check($"qpreview {j.id}: the countdown ran from the box", shotCount && q.State == QuarryJumps.Phase.Run);
                R73Set(0, 0, 1f); yield return new WaitForSecondsRealtime(1.5f); R73Set(0, 0, 0);
            }
        }

        // ---------------------------------------------------------------- forks
        IEnumerator R74Forks()
        {
            report.AppendLine(); report.AppendLine("=== Fork barriers: Kennesaw's CP5 fork in the race, and some of the new barriers on other courses (from the line, looking at them)");
            yield return Vehicle("drifter"); yield return Clock(12f);
            foreach (var (file, dir, ids) in new[] { ("kennesaw_mountain.json", "forward", new[] { "B2" }), ("noonday_creek_trail.json", "forward", new[] { "F1", "F16", "F19" }), ("chastain_ridge.json", "forward", new[] { "F1", "F2" }), ("noonday_creek.json", "forward", new[] { "F1" }) })
            {
                UseRoute(file, dir); RaceController.AiCount = 0; race.StartRace(); yield return new WaitForSecondsRealtime(1.2f);
                var r = race.Route;
                foreach (var id in ids)
                {
                    var c = r.Closures.FirstOrDefault(x => x.id == id); if (c == null) { report.AppendLine($"  {r.Name}: no closure {id}"); continue; }
                    var at = new Vector3(c.contact[0], 0f, c.contact[1]); float s = r.LapStationOf(at); var back = r.PointAt(s - 45f, out var f);
                    yield return R74Place(back, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, 2f);
                    bool built = !race.Barriers || !race.Barriers.OnCourse.Any(x => x.StartsWith(id + " ")) && !race.Barriers.Open.Any(x => x.StartsWith(id + " "));
                    yield return Capture($"74_fork_{Path.GetFileNameWithoutExtension(file)}_{id}");
                    report.AppendLine($"  {r.Name} {id} ({c.label}): at ({c.x:0}, {c.z:0}); {(built ? "standing" : "NOT built")}; picture from 45 m back on the line");
                }
                race.EndRace(); yield return new WaitForSecondsRealtime(.5f);
            }
        }
    }
}
