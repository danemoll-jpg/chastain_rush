using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 75 in the exe (-exeRound75 <dir> [-r75Only dan,types,mapb] -vehicleSave <file> -logFile <dir>\Player.log): CHECK IT AS DAN PLAYS IT (TODO.md
    // item 0). The summit leap's flight and its pictures (the clouds) run as -exeRound73 -r73Only leap.
    //   dan     BUG-001 (session 2026-10-27_11-27-43-992_53ef4c) from Dan's exact position and heading; the ground round it: its steepest step
    //   types   A: race setup's RACE TYPE with the game's own input (the menus with a virtual pad, then keys): a street race finished -> RACE SETUP ->
    //           Air -> the garage -> the Sky Dart -> START RACE; an air race finished -> Street -> the Drifter Twin -> START RACE; the title -> RACE ->
    //           Air (the last plane and air course come back by themselves)
    //   mapb    B: the map screen at Town Center, the two community pools, the stunt park, the quarry (the built things); street names at three
    //           zoom levels over Chastain Glen and Town Center; the minimap with the street's name
    public sealed partial class Round20Runner
    {
        string Prof => car.GetComponent<VehicleConfiguration>().profileId;

        // ---------------------------------------------------------------- dan
        IEnumerator R75Dan()
        {
            report.AppendLine(); report.AppendLine("=== Dan's BUG-001 (a gravel track off Kennesaw Mountain, by the summit leap's way back): \"there isn't a drop off for the road but a smooth slope\"");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            var at = new Vector3(-1268.09f, 0f, -6341.39f);
            yield return R74Place(at, 100.43f);
            yield return Capture("75_BUG-001_after");
            // the ground within 40 m: the largest rise between neighbors 1 m apart (a face shows as a rise near 1 m or more)
            float worst = 0f; Vector3 wAt = at;
            for (float x = -40f; x <= 40f; x += 1f)
                for (float z = -40f; z <= 40f; z += 1f)
                {
                    float h = GroundQuery.Height(at.x + x, at.z + z), hx = GroundQuery.Height(at.x + x + 1f, at.z + z), hz = GroundQuery.Height(at.x + x, at.z + z + 1f);
                    float d = Mathf.Max(Mathf.Abs(hx - h), Mathf.Abs(hz - h)); if (d > worst) { worst = d; wAt = new Vector3(at.x + x, h, at.z + z); }
                }
            report.AppendLine($"  at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}) heading 100; picture 75_BUG-001_after.png");
            report.AppendLine($"  the ground within 40 m: steepest {Mathf.Atan(worst) * Mathf.Rad2Deg:0} degrees ({worst:0.00} m in 1 m) at ({wAt.x:0}, {wAt.z:0})");
            Check("dan BUG-001: no face within 40 m (nothing steeper than 45 degrees)", worst < 1f, $"{Mathf.Atan(worst) * Mathf.Rad2Deg:0} deg");
            // and from the track below, looking back up at where the run-out meets it
            yield return R74Place(new Vector3(-1250f, 0f, -6362f), 340f); yield return Capture("75_BUG-001_from_the_track");
        }

        // ---------------------------------------------------------------- types
        IEnumerator R75Press(bool keys, string what)
        {
            switch (what)
            {
                case "ok": yield return keys ? R70Key(Key.Space) : R70Tap(GamepadButton.South); break;
                case "back": yield return keys ? R70Key(Key.Escape) : R70Tap(GamepadButton.East); break;
                case "right": yield return keys ? R70Key(Key.RightArrow) : R70Tap(GamepadButton.DpadRight); break;
                case "left": yield return keys ? R70Key(Key.LeftArrow) : R70Tap(GamepadButton.DpadLeft); break;
            }
            yield return new WaitForSecondsRealtime(.45f);
        }
        // in race setup: RACE TYPE to 'air' (or street), then the garage to 'want', back, START RACE; the race's first seconds from the chase camera
        IEnumerator R75SetupAndRace(GameMenus m, bool keys, bool toAir, string want, string tag)
        {
            string side = keys ? "keys" : "pad";
            yield return R70Focus(m, "racetype", keys);
            Check($"types {tag}: race setup opens with RACE TYPE ({side})", m.PageForCheck == "race" && m.FocusedForCheck == "racetype", $"page {m.PageForCheck}, focus {m.FocusedForCheck}");
            bool wasAir = race.RouteData != null && race.RouteData.air;
            if (wasAir != toAir) yield return R75Press(keys, toAir ? "right" : "left");
            yield return new WaitForSecondsRealtime(.4f); yield return Capture($"75_{tag}_setup");
            var rd = race.RouteData;
            report.AppendLine($"  {tag} ({side}): RACE TYPE {(rd != null && rd.air ? "Air" : "Street")}, course {race.CurrentFile}, race {race.State}, vehicle put on {Nm(Prof)}");
            Check($"types {tag}: the type switched, its course loaded ({side})", rd != null && rd.air == toAir && race.IsAir(race.CurrentFile) == toAir && race.State == RaceController.Stage.Free, $"{race.CurrentFile}, {race.State}");
            yield return R70Focus(m, "garage", keys); yield return R75Press(keys, "ok");
            yield return R70Focus(m, "vehicle", keys);
            var seen = new List<string> { Prof };
            for (int i = 0; i < 16 && Prof != want; i++) { yield return R75Press(keys, "right"); if (!seen.Contains(Prof)) seen.Add(Prof); }
            yield return Capture($"75_{tag}_garage");
            report.AppendLine($"    the garage ({m.PageForCheck}) stepped through: {string.Join(", ", seen.Select(Nm))}");
            Check($"types {tag}: the garage holds only {(toAir ? "planes" : "ground vehicles")} and {Nm(want)} ({side})", m.PageForCheck == "garage" && Prof == want && seen.All(v => VehicleProfile.Find(v).IsPlane == toAir), string.Join(",", seen));
            yield return R75Press(keys, "back");
            yield return R70Focus(m, "start", keys); yield return R75Press(keys, "ok");
            float until = Time.realtimeSinceStartup + 15f; while (race.State == RaceController.Stage.Free && Time.realtimeSinceStartup < until) yield return null;
            Check($"types {tag}: START RACE starts {(toAir ? "an air" : "a street")} race in the {Nm(want)} ({side})", race.State != RaceController.Stage.Free && race.Route != null && race.Route.Air == toAir && Prof == want, $"{race.State}, {race.Route?.Name}, {Nm(Prof)}");
            R73Pad(); while (race.State == RaceController.Stage.Countdown) { R73Set(0, 0, 0); yield return null; }
            float t = 0f; while (t < (toAir ? 9f : 5f)) { R73Set(0f, 1f, 0f, toAir && t > 5f ? -.5f : 0f); yield return null; t += Time.deltaTime; }   // full throttle (an air race: the stick back once at speed)
            yield return Capture($"75_{tag}_racing"); R73Set(0, 0, 0);
            report.AppendLine($"    the race: {race.Route.Name} in the {Nm(Prof)}, {race.State}");
            race.EndRace(); yield return new WaitForSecondsRealtime(.5f);
        }
        IEnumerator R75Types()
        {
            report.AppendLine(); report.AppendLine("=== A. Race setup's RACE TYPE (Street / Air) and the garage that follows, through the menus (Dan: \"I did a street race and then selected the new air course, and my only garage choices were street vehicles\")");
            R74Devices(); RaceController.RouteOverride = null;
            var m = GameMenus.Instance ? GameMenus.Instance : GameMenus.Create(); yield return new WaitForSecondsRealtime(1.5f); if (m) m.CloseForCheck();
            // 1. pad: a street race finished, then race setup from the race-over menu -> Air -> the Sky Dart -> START RACE
            yield return Vehicle("original");
            yield return StartRoute("chastain_glen_loop.json", "forward", 0, 1);
            var rr = race.Route; yield return new WaitForSecondsRealtime(1f);
            yield return AcrossLine(rr.Gate, 0f); foreach (var cp in rr.Checkpoints) yield return AcrossLine(cp, 0f); race.Player.LapAway = true; yield return AcrossLine(rr.Gate, 0f);
            car.Body.linearVelocity = Vector3.zero; float until = Time.realtimeSinceStartup + 12f; while (WinnerShot.Active && Time.realtimeSinceStartup < until) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            Check("types 1: the street race is over", race.State == RaceController.Stage.Finished, race.State.ToString());
            m.OpenPauseForCheck(); yield return new WaitForSecondsRealtime(.6f);
            yield return R70Focus(m, "setup", false); yield return R75Press(false, "ok");
            yield return R75SetupAndRace(m, false, true, "dart", "street_then_air");
            // 2. keys: an air race finished (flown by the AI pilot's code on the player's Sky Dart), then Street -> the Drifter Twin -> START RACE
            var pilot = R71Fly(1, 0, 2); var ev = new List<string>(); yield return R71Run("r75air", 400f, ev); R71Done(pilot);
            yield return new WaitForSecondsRealtime(1f); until = Time.realtimeSinceStartup + 12f; while (WinnerShot.Active && Time.realtimeSinceStartup < until) yield return null;
            report.AppendLine($"  the air race flown in the {Nm(Prof)}: {(race.Player.Finished ? "finished" : "NOT finished")}");
            Check("types 2: the air race is over", race.State == RaceController.Stage.Finished, race.State.ToString());
            m.OpenPauseForCheck(); yield return new WaitForSecondsRealtime(.6f);
            yield return R70Focus(m, "setup", true); yield return R75Press(true, "ok");
            yield return R75SetupAndRace(m, true, false, "drifter", "air_then_street");
            // 3. pad: from the title, RACE -> Air: the last air course and the Sky Dart come back without choosing them again
            m.OpenTitleForCheck(); yield return new WaitForSecondsRealtime(.6f);
            yield return R70Focus(m, "race", false); yield return R75Press(false, "ok");
            yield return R70Focus(m, "racetype", false);
            string streetBack = race.CurrentFile; string streetCar = Prof;
            yield return R75Press(false, "right"); yield return new WaitForSecondsRealtime(.4f); yield return Capture("75_title_air_setup");
            report.AppendLine($"  3 (pad): the title > RACE: street course {streetBack} in the {Nm(streetCar)}; RACE TYPE Air: {race.CurrentFile} in the {Nm(Prof)}");
            Check("types 3: from the title, Air brings back the last air course and the Sky Dart", race.RouteData.air && Prof == "dart" && race.CurrentFile == "quarry_air_race.json", $"{race.CurrentFile}, {Nm(Prof)}");
            Check("types 3: the street side kept its course and the Drifter Twin", streetBack == "chastain_glen_loop.json" && streetCar == "drifter", $"{streetBack}, {Nm(streetCar)}");
            yield return R70Focus(m, "start", false); yield return R75Press(false, "ok");
            until = Time.realtimeSinceStartup + 15f; while (race.State == RaceController.Stage.Free && Time.realtimeSinceStartup < until) yield return null;
            Check("types 3: START RACE from the title's race setup starts the air race in the Sky Dart", race.State != RaceController.Stage.Free && race.Route.Air && Prof == "dart", $"{race.State}, {Nm(Prof)}");
            while (race.State == RaceController.Stage.Countdown) yield return null;
            yield return new WaitForSecondsRealtime(1f); yield return Capture("75_title_air_racing");
            race.EndRace(); if (GameMenus.Open) m.CloseForCheck();
        }

        // ---------------------------------------------------------------- map
        IEnumerator R75Map()
        {
            report.AppendLine(); report.AppendLine("=== B. The map: what the build placed (lots, pools, courts, the stunt park, the quarry), street names, the minimap's street");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("drifter"); yield return Clock(13f);
            var map = WorldMap.Instance; if (!map) { Check("map: the map", false); yield break; }
            var places = new (string name, Vector3 at, float[] zooms)[]
            {
                ("town_center", new Vector3(265f, 0f, -1207f), new[] { 7f, 14f }),
                ("lakes_pool", new Vector3(1343f, 0f, 1095f), new[] { 16f }),
                ("enclave_pool", new Vector3(1789f, 0f, 745f), new[] { 16f }),
                ("stunt_park", new Vector3(3628f, 0f, 403f), new[] { 12f }),
                ("quarry", new Vector3(-1638f, 0f, -1893f), new[] { 9f }),
                ("chastain_glen", new Vector3(2938f, 0f, 799f), new[] { 2.5f, 6f, 14f }),
                ("town_center_names", new Vector3(265f, 0f, -1207f), new[] { 2.5f, 5f, 10f }),
            };
            foreach (var pl in places)
            {
                yield return R74Place(pl.at, 0f, 1.2f);
                map.Open(); yield return new WaitForSecondsRealtime(1f);
                foreach (var z in pl.zooms)
                {
                    map.ZoomForCheck(z); yield return new WaitForSecondsRealtime(.8f);
                    yield return Capture($"75_map_{pl.name}_z{z:0.#}");
                    report.AppendLine($"  {pl.name} zoom {z:0.#}: {map.StreetLabelsShown} street names and shields on screen; picture 75_map_{pl.name}_z{z:0.#}.png");
                    if (pl.name == "chastain_glen" && z >= 14f) Check("map: street names close in over Chastain Glen", map.StreetLabelsShown >= 4, map.StreetLabelsShown.ToString());
                }
                map.Close(); yield return new WaitForSecondsRealtime(.4f);
            }
            // the minimap on a street: its name at the top
            yield return R74Place(new Vector3(2938.68f, 0f, 799.3f), 0f, 2f);
            string st = MiniMap.Instance ? MiniMap.Instance.StreetText : "";
            yield return Capture("75_minimap_street");
            report.AppendLine($"  the minimap at Chastain Glen's fast-travel spot: \"{st}\"");
            Check("map: the minimap names the street you are on", !string.IsNullOrEmpty(st), st);
        }
    }
}
