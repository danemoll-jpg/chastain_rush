using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 35 Part C in the exe: ChastainRush.exe -exeRound35 <dir> -r35Only cpics,grip,fps -logFile <dir>\Player.log (muted).
    //  cpics: the same three places (Chastain Glen Lane, Canton Road at the shops, the hub) at noon, dusk and night and in rain and
    //        snow (Free Roam's clock and weather, the camera placed by hand);
    //  grip: a race started in rain: the grip under the car on the road, and the braking distance from 25 m/s on the course's
    //        first straight in Clear, in Rain, and in Rain with Settings > Gameplay > Weather affects grip Off;
    //  fps:  the frame rate parked at Canton Road by the shops, by day, at night and in rain (run it with -lookOff for "before").
    public sealed partial class Round20Runner
    {
        IEnumerator R35Pics()
        {
            report.AppendLine(); report.AppendLine("=== Part C: pictures at three places, noon / dusk / night / rain / snow");
            var look = WorldLook.Current; if (!look) { Check("Part C: the world look is running", false, "no WorldLook (-lookOff?)"); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var s = GameSettings.Settings;
            // the street (Chastain Glen Lane, its north side), Canton Road at the shops, the hub from its top ledge's north-west corner
            var places = new List<(string name, Vector3 at, Vector3 lookAt)>();
            var st = RaceRoute.Surface(3108.8f, 1065.0f); var st2 = RaceRoute.Surface(3112.0f, 1105.0f);
            places.Add(("street", st + Vector3.up * 1.8f, st2 + Vector3.up * 2.5f));
            var cs = RaceRoute.Surface(3502.8f, 1166.1f); var dir = Quaternion.Euler(0, -13f, 0) * Vector3.forward;
            places.Add(("canton", cs + Vector3.up * 2.2f - dir * 6f, cs + dir * 40f + Vector3.up * 3f));
            places.Add(("hub", new Vector3(1852f, 274.5f, 1170f), new Vector3(1915f, 258f, 1142f)));
            car.Body.position = new Vector3(3420f, RaceRoute.Surface(3420f, 985f).y + 0.5f, 985f); car.Body.linearVelocity = Vector3.zero;   // out of the pictures
            var conds = new (string name, float hour, int weather)[] { ("noon", 12f, 0), ("dusk", 19.7f, 0), ("night", 23.5f, 0), ("rain", 12f, 1), ("snow", 12f, 2) };
            foreach (var c in conds)
            {
                s.roamWeather = c.weather; look.SetClock(12, c.hour);
                foreach (var p in places)
                {
                    var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); if (chase) chase.enabled = false;
                    cam.transform.position = p.at; cam.transform.LookAt(p.lookAt);
                    yield return new WaitForSecondsRealtime(c.weather == 2 ? 7f : 3f);   // the weather and the look settle (snow takes longest to fill the air)
                    look.SetClock(12, c.hour);
                    yield return Still($"c_{p.name}_{c.name}", p.at, p.lookAt, 62f);
                }
                report.AppendLine($"      {c.name}: {look.Conditions}; sun {RenderSettings.sun.intensity:0.00}, fog {RenderSettings.fogStartDistance:0}-{RenderSettings.fogEndDistance:0} m, lamps level {VehicleLights.Level:0.00}" +
                                  $"{(FindAnyObjectByType<NightLamps>() is NightLamps nl ? $", {nl.Lamps} street lamps and light poles ({nl.LitNear} lit near)" : "")}");
            }
            s.roamWeather = 0; look.SetClock(12, 12f);
            var fx = look.GetComponent<WeatherEffects>();
            Check("Part C: pictures taken at three places in five conditions (c_*.png)", true, "street, canton, hub x noon, dusk, night, rain, snow");
        }

        IEnumerator R35Grip()
        {
            report.AppendLine(); report.AppendLine("=== Part C: a race in rain: the grip");
            var s = GameSettings.Settings; bool keepGrip = s.weatherGrip;
            var results = new List<string>(); float clear = 0, rain = 0, off = 0, gripRain = 0;
            foreach (var (label, weather, setting) in new[] { ("Clear", 0, true), ("Rain", 1, true), ("Rain, setting Off", 1, false) })
            {
                GameMenus.Current.weather = weather; s.weatherGrip = setting;
                RaceController.RouteOverride = "chastain_glen_loop.json"; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 1; RaceController.AiCount = 0;
                if (race.State != RaceController.Stage.Free) race.EndRace();
                race.Reload(); race.StartRace();
                while (race.State == RaceController.Stage.Countdown) yield return null;
                // the course's first straight: 30 m past the line, along the lap
                var at = race.Route.PointAt(race.Route.GateS + 30f, out var f);
                var vi = car.GetComponent<VehicleInput>(); if (vi) vi.enabled = false;
                respawn.PlaceAt(at + Vector3.up * 0.4f, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg); race.NotifyTeleported();
                yield return new WaitForFixedUpdate();
                car.Body.linearVelocity = f * 25f;
                for (int i = 0; i < 25; i++) { car.Simulate(1f, 0f, 0f, Time.fixedDeltaTime); yield return new WaitForFixedUpdate(); }
                float grip = car.GripScale; var start = car.Body.position; float v0 = car.ForwardSpeed;
                int steps = 0; while (car.ForwardSpeed > 0.5f && steps < 600) { car.Simulate(0f, 1f, 0f, Time.fixedDeltaTime); steps++; yield return new WaitForFixedUpdate(); }
                float dist = Vector3.Distance(start, car.Body.position);
                if (vi) vi.enabled = true;
                results.Add($"{label}: grip {grip:P0} ({WeatherGrip.InEffect}), from {v0:0.0} m/s stopped in {dist:0.0} m");
                if (weather == 0) clear = dist; else if (setting) { rain = dist; gripRain = grip; } else off = dist;
                if (label == "Rain") { yield return new WaitForSecondsRealtime(1.5f); yield return Capture("c_race_in_rain"); }
                race.EndRace(); yield return new WaitForSecondsRealtime(0.5f);
            }
            GameMenus.Current.weather = 0; s.weatherGrip = keepGrip;
            Check("Part C: a race in rain has lower grip on the road (85 % on paved, Racer's) and stops longer", gripRain < 0.9f && rain > clear * 1.08f, string.Join("; ", results));
            Check("Part C: with Weather affects grip Off, rain grips as Clear", Mathf.Abs(off - clear) < clear * 0.03f, $"Clear {clear:0.0} m, Rain with the setting Off {off:0.0} m");
        }

        IEnumerator R35Fps()
        {
            bool off = System.Environment.GetCommandLineArgs().Contains("-lookOff");
            report.AppendLine(); report.AppendLine($"=== Part C: frame rate parked at Canton Road by the shops ({(off ? "BEFORE: -lookOff, the world look not running" : "after")}; {Screen.width} x {Screen.height}, not measured at 3840 x 2160)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var look = WorldLook.Current; var s = GameSettings.Settings;
            // what costs what (check only): -r35Off clouds,effects,lamps,look disables those parts before measuring
            var argv = System.Environment.GetCommandLineArgs(); int ai = System.Array.IndexOf(argv, "-r35Off"); string offs = ai >= 0 && ai + 1 < argv.Length ? argv[ai + 1] : "";
            if (offs.Contains("clouds")) foreach (var c in FindObjectsByType<SkyClouds>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
            if (offs.Contains("effects")) foreach (var c in FindObjectsByType<WeatherEffects>(FindObjectsSortMode.None)) c.enabled = false;
            if (offs.Contains("lamps")) foreach (var c in FindObjectsByType<NightLamps>(FindObjectsSortMode.None)) c.enabled = false;
            if (offs.Contains("look") && look) look.enabled = false;
            if (offs.Contains("pin") && look) look.Pin(LookPresets.ClearDay);   // this game's day exactly (its sun, 48 / 325)
            if (offs.Contains("probes")) foreach (var rp in FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) rp.enabled = false;
            if (offs.Length > 0) report.AppendLine($"      parts off: {offs}");
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;   // uncapped, to compare
            var cs = RaceRoute.Surface(3502.8f, 1166.1f);
            respawn.PlaceAt(cs + Vector3.up * 0.4f, -13f); race.NotifyTeleported();
            foreach (var (label, hour, weather) in new[] { ("day", 12f, 0), ("night", 23.5f, 0), ("rain", 12f, 1) })
            {
                if (look && !offs.Contains("pin")) { s.roamWeather = weather; look.SetClock(12, hour); }
                yield return new WaitForSecondsRealtime(3f);
                var frames = new List<float>(); float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; frames.Add(Time.unscaledDeltaTime); if (look) look.SetClock(12, hour); }
                var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
                report.AppendLine($"INFO  {label}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps (vSync {QualitySettings.vSyncCount}, cap {Application.targetFrameRate})");
                if (off) break;   // before: one look only
            }
            if (look) { s.roamWeather = 0; look.SetClock(12, 12f); }
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }
    }
}
