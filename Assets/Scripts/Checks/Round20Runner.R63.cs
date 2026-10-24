using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 63 in the exe (-exeRound63 <dir> [-r63Only hover,hfly,hpads,hcrash,hfps,hpics] -logFile <dir>\Player.log): the helicopter.
    //   hover   Free Roam's pick (the garage's Helicopter) at Chastain Glen: it spools up, lifts off to 40 m, then 30 s hands off: the drift
    //   hfly    from there to the summit, Town Center and Chastain Glen by autopilot (the player's sticks, scripted), landing on each: time, landed
    //   hpads   each helipad: driven onto in the Street Classic, the swap (as the button), a lift off to 25 m, back down on the pad, swapped back
    //   hcrash  into a tree's crown in the mountain's woods and into a building at Town Center: it crashes, is put on open ground, lifts off again
    //   hfps    hovering hands off at 50, 200 and 500 m over Chastain Glen, Town Center and the mountain: frame rate (chase camera), how far it sees
    //   hpics   from 500 m over the map, straight down over Town Center, each helipad
    public sealed partial class Round20Runner
    {
        HelicopterFlight Heli => car ? car.GetComponent<HelicopterFlight>() : null;
        static readonly (string name, Vector2 at)[] R63Places =
        {
            ("Chastain Glen", new Vector2(2938.68f, 799.3f)), ("Town Center", new Vector2(265.11f, -1207.29f)), ("Kennesaw Mountain summit", new Vector2(-1122.98f, -5870.01f)),
        };

        IEnumerator R63Heli(Vector2 at, float yaw)
        {
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            respawn.PlaceAt(RaceRoute.Surface(at.x, at.y) + Vector3.up * .4f, yaw); race.NotifyTeleported();
            yield return Vehicle("heli");
            var h = Heli; if (h) { h.Scripted = true; h.sLift = h.sYaw = h.sForward = h.sSide = 0; h.sBoost = false; }
        }
        void Sticks(float lift = 0, float yaw = 0, float fwd = 0, float side = 0, bool boost = false) { var h = Heli; if (!h) return; h.Scripted = true; h.sLift = lift; h.sYaw = yaw; h.sForward = fwd; h.sSide = side; h.sBoost = boost; }

        // climb (or descend) to `height` over the ground with the sticks; true when there
        IEnumerator R63ToHeight(float height, float timeout = 60f)
        {
            var h = Heli; float t = 0;
            while (t < timeout && h && !h.Crashed)
            {
                float err = height - h.HeightAboveGround; if (Mathf.Abs(err) < 1.5f && Mathf.Abs(car.Body.linearVelocity.y) < 1f) break;
                Sticks(lift: Mathf.Clamp(err / 15f, -1f, 1f)); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
            }
            Sticks();
        }

        // the autopilot: to a place at a cruise height over the highest ground ahead, then down to land there
        IEnumerator R63FlyTo(Vector2 to, float cruise, bool land, float timeout, System.Action<float, bool, string> done)
        {
            var h = Heli; float t = 0; bool landing = false;
            while (t < timeout && h)
            {
                yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                if (h.Crashed) { Sticks(); done(t, false, "crashed: " + h.CrashReason); yield break; }
                var p = car.Body.position; var d = to - new Vector2(p.x, p.z); float dist = d.magnitude;
                float want = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, err = Mathf.DeltaAngle(car.transform.eulerAngles.y, want);
                float ground = 0f; var dir = dist > 1f ? d / dist : Vector2.zero;
                for (float a = 0; a <= 900f; a += 60f) { var q = new Vector2(p.x, p.z) + dir * Mathf.Min(a, dist); ground = Mathf.Max(ground, R59Terrain(new Vector3(q.x, 0, q.y))); }
                float speed = new Vector2(car.Body.linearVelocity.x, car.Body.linearVelocity.z).magnitude;
                if (!landing && dist < 25f && speed < 6f) landing = true;
                if (landing)
                {
                    if (!land) { Sticks(); done(t, true, "there"); yield break; }
                    // hold over the spot (the right stick toward it), descend
                    var local = Quaternion.Inverse(Quaternion.Euler(0, car.transform.eulerAngles.y, 0)) * new Vector3(d.x, 0, d.y);
                    Sticks(lift: -.7f, fwd: Mathf.Clamp(local.z / 20f, -.3f, .3f), side: Mathf.Clamp(local.x / 20f, -.3f, .3f));
                    if (h.Landed && t > 1f) { Sticks(); done(t, true, "landed"); yield break; }
                    continue;
                }
                float wantY = ground + cruise, lift = Mathf.Clamp((wantY - p.y) / 25f, -1f, 1f);
                float fwd = Mathf.Clamp(dist / 250f, 0f, 1f) * (Mathf.Abs(err) < 35f ? 1f : .15f);
                if (dist < 300f) fwd = Mathf.Min(fwd, Mathf.Clamp(dist / 300f, .05f, 1f)) * (speed > dist / 4f + 4f ? 0f : 1f);   // slow for the spot
                Sticks(lift: lift, yaw: Mathf.Clamp(err / 25f, -1f, 1f), fwd: fwd, boost: dist > 1500f && Mathf.Abs(err) < 10f && p.y > ground + cruise * .6f);
            }
            Sticks(); done(t, false, "timed out");
        }

        IEnumerator R63Hover()
        {
            report.AppendLine(); report.AppendLine("=== The helicopter from Free Roam's pick at Chastain Glen: spool up, lift off, 30 s hands off at 40 m");
            yield return R63Heli(R63Places[0].at, 352f);
            var h = Heli; Check("hover: the helicopter is flying the player's vehicle", h && h.enabled && car.Flown, h ? "" : "no HelicopterFlight");
            if (!h) yield break;
            float t0 = Time.time; while (h.Spool < .95f && Time.time - t0 < 6f) yield return null;
            report.AppendLine($"INFO  spooled up in {Time.time - t0:0.0} s; landed {h.Landed}");
            Sticks(lift: 1f); t0 = Time.time; while (h.HeightAboveGround < 3f && Time.time - t0 < 6f) yield return new WaitForFixedUpdate();
            Check("hover: lifts off", h.HeightAboveGround >= 3f, $"{h.HeightAboveGround:0.0} m after {Time.time - t0:0.0} s");
            yield return R63ToHeight(40f);
            yield return new WaitForSecondsRealtime(2f);
            var start = car.Body.position; float maxH = 0, maxV = 0;
            t0 = Time.time; while (Time.time - t0 < 30f) { yield return new WaitForFixedUpdate(); var p = car.Body.position; maxH = Mathf.Max(maxH, Flat(p - start).magnitude); maxV = Mathf.Max(maxV, Mathf.Abs(p.y - start.y)); }
            Check("hover: 30 s hands off drifts under 3 m", maxH < 3f && maxV < 3f, $"sideways {maxH:0.00} m, up / down {maxV:0.00} m at {h.HeightAboveGround:0} m over the ground");
            yield return Capture("63_hover");
        }

        IEnumerator R63Fly()
        {
            report.AppendLine(); report.AppendLine("=== Flights by autopilot (the sticks, scripted; boost on the long legs), landing at each place, 120 m over the highest ground ahead");
            yield return R63Heli(R63Places[0].at, 352f);
            var h = Heli; if (!h) yield break; float t0 = Time.time; while (h.Spool < .95f && Time.time - t0 < 6f) yield return null;
            foreach (var (name, at) in new[] { R63Places[2], R63Places[1], R63Places[0] })
            {
                var from = car.Body.position; float time = 0; bool ok = false; string how = "";
                yield return R63FlyTo(at, 120f, true, 400f, (tt, o, w) => { time = tt; ok = o; how = w; });
                var p = car.Body.position; float km = Flat(p - from).magnitude / 1000f;
                Check($"hfly: to {name}, landed", ok && h.Landed, $"{how}, {km:0.0} km in {time:0} s ({km * 1000f / Mathf.Max(1, time):0} m/s average), {Flat(p - new Vector3(at.x, 0, at.y)).magnitude:0} m from the spot");
                yield return new WaitForSecondsRealtime(1f);
                yield return Capture("63_landed_" + new string(name.Where(char.IsLetterOrDigit).Take(16).ToArray()));
                Sticks(lift: 1f); yield return new WaitForSecondsRealtime(1.5f); Sticks();
            }
        }

        IEnumerator R63Pads()
        {
            report.AppendLine(); report.AppendLine("=== The helipads: onto each in the Street Classic (the roof pad by fast travel), the swap, a lift off to 25 m, back down on the pad, swapped back");
            var stands = HelipadStands.Instance; Check("hpads: the helipads are built", stands && stands.pads.Length == 2, stands ? $"{stands.pads.Length} pads" : "none");
            if (!stands) yield break;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            foreach (var p in stands.pads)
            {
                yield return Vehicle("original");
                var f = Quaternion.Euler(0, p.yaw, 0) * Vector3.forward;
                if (p.id.Contains("kennestone"))
                {
                    var ex = Exploration.Instance; var d = ex ? ex.destinations.FirstOrDefault(x => x.id == p.id) : null;
                    if (ex && d != null) { ex.Find(p.id); Check($"hpads: {p.title}: fast travel there", ex.Travel(d), ex.Message); }
                    else Check($"hpads: {p.title}: a place on the map", false, "no destination");
                }
                else { Place(p.centre - f * 30f, f, 0f); yield return new WaitForSecondsRealtime(.5f); var run = new Run(); yield return DriveLine(new List<Vector3> { p.centre - f * 30f, p.centre }, 8f, 20f, run); }
                yield return null; yield return null;   // the pads' own frame after a move
                float t0 = Time.time; while (!stands.CanTake && Time.time - t0 < 6f) { car.Body.linearVelocity *= .9f; yield return new WaitForFixedUpdate(); }
                Check($"hpads: {p.title}: on the pad, the swap is offered", stands.CanTake, $"{Flat(car.Body.position - p.centre).magnitude:0.0} m from its middle, {car.Body.position.y - p.centre.y:+0.0;-0.0} m");
                yield return Capture("63_pad_" + p.id.Replace("helipad-", "") + "_car");
                stands.Swap(car, sw); yield return new WaitForSecondsRealtime(.6f); var h = Heli;
                Check($"hpads: {p.title}: swapped to the helicopter", h && h.enabled, "");
                if (!h) continue;
                t0 = Time.time; while (h.Spool < .95f && Time.time - t0 < 6f) yield return null;
                yield return R63ToHeight(25f, 30f); float up = h.HeightAboveGround;
                yield return Capture("63_pad_" + p.id.Replace("helipad-", "") + "_up");
                bool landed = false; yield return R63FlyTo(new Vector2(p.centre.x, p.centre.z), 25f, true, 60f, (tt, ok, w) => landed = ok);
                yield return new WaitForSecondsRealtime(.8f);
                Check($"hpads: {p.title}: up to {up:0} m and back down on the pad", landed && stands.CanLeave, $"{Flat(car.Body.position - p.centre).magnitude:0.0} m from its middle, landed {h.Landed}");
                stands.Swap(car, sw); yield return new WaitForSecondsRealtime(.6f);
                Check($"hpads: {p.title}: swapped back to the Street Classic", !HelicopterFlight.Flying(car) && car.GetComponent<VehicleConfiguration>().profileId == "original", car.GetComponent<VehicleConfiguration>().profileId);
                yield return Capture("63_pad_" + p.id.Replace("helipad-", "") + "_back");
            }
        }

        IEnumerator R63Crash()
        {
            report.AppendLine(); report.AppendLine("=== Crashes: into a tree's crown, into a building; then it is put on open ground and lifts off again");
            // the densest woods near the mountain's road: the tree nearest (-1000, -5600) with the tallest crown within 150 m
            var tf = FindAnyObjectByType<TreeField>(); TreeField.Packed best = default; float top = 0;
            for (int i = 0; tf && i < tf.count; i++) { var t = TreeField.Unpack(tf.data, i); if (Mathf.Abs(t.x + 1000f) > 150f || Mathf.Abs(t.z + 5600f) > 150f) continue; float ch = t.trunkH * .1f * .5f + t.crownH * .01f; if (ch > top) { top = ch; best = t; } }
            var targets = new List<(string what, Vector2 from, float yaw, Vector3 aim)>();
            if (top > 0) targets.Add(("a tree", new Vector2(best.x, best.z + 120f), 180f, new Vector3(best.x, best.y + best.trunkH * .05f + best.crownH * .01f * .55f, best.z)));
            // a building: Town Center's front, from its main doors' fast-travel spot in the lot (32 m out, facing the doors), 8 m up, 80 m on
            var c = new Vector2(265.11f, -1207.29f); var dir = new Vector2(Mathf.Sin(165.25f * Mathf.Deg2Rad), Mathf.Cos(165.25f * Mathf.Deg2Rad)); var b = c + dir * 80f;
            targets.Add(("a building (Town Center's front)", c, 165.25f, new Vector3(b.x, RaceRoute.Surface(c.x, c.y).y + 8f, b.y)));
            foreach (var (what, from, yaw0, aim) in targets)
            {
                yield return R63Heli(from, yaw0); var h = Heli; if (!h) yield break;
                float t0 = Time.time; while (h.Spool < .95f && Time.time - t0 < 6f) yield return null;
                yield return R63ToHeight(aim.y - R59Terrain(car.Body.position) + 0f);
                int before = h.Crashes; t0 = Time.time;
                while (Time.time - t0 < 25f && h.Crashes == before)
                {
                    yield return new WaitForFixedUpdate(); var p = car.Body.position; var d = new Vector2(aim.x - p.x, aim.z - p.z);
                    float err = Mathf.DeltaAngle(car.transform.eulerAngles.y, Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg);
                    Sticks(lift: Mathf.Clamp((aim.y - p.y) / 6f, -1f, 1f), yaw: Mathf.Clamp(err / 20f, -1f, 1f), fwd: .45f);
                }
                Check($"hcrash: into {what}, it crashes", h.Crashes > before, $"{h.CrashReason}");
                Sticks(); yield return new WaitForSecondsRealtime(.6f); yield return Capture("63_crash_" + (what.Contains("tree") ? "tree" : "building"));
                t0 = Time.time; while (h.Crashed && Time.time - t0 < 10f) yield return null;
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();   // its height measured again where it was put
                var p2 = car.Body.position;
                Check($"hcrash: {what}: put on open ground, upright", !h.Crashed && car.transform.up.y > .95f, $"{Flat(p2 - aim).magnitude:0} m from the impact, {h.HeightAboveGround:0.0} m over the ground, up {car.transform.up.y:0.00}");
                t0 = Time.time; while (h.Spool < .95f && Time.time - t0 < 6f) yield return null;
                yield return R63ToHeight(12f, 20f);
                Check($"hcrash: {what}: lifts off again (not stuck)", h.HeightAboveGround > 8f && !h.Crashed, $"{h.HeightAboveGround:0.0} m");
            }
        }

        IEnumerator R63Fps()
        {
            report.AppendLine(); report.AppendLine($"=== Frame rate hovering hands off, chase camera, uncapped, {Screen.width} x {Screen.height} (not measured at 3840 x 2160), 8 s each");
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            foreach (var (name, at) in R63Places)
            {
                yield return R63Heli(at, 0f); var h = Heli; if (!h) yield break;
                float t0 = Time.time; while (h.Spool < .95f && Time.time - t0 < 6f) yield return null;
                foreach (float height in new[] { 50f, 200f, 500f })
                {
                    yield return R63ToHeight(height, 60f); yield return new WaitForSecondsRealtime(2f);
                    var frames = new List<float>(); var ft = new R61Timing(); t0 = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - t0 < 8f) { yield return null; frames.Add(Time.unscaledDeltaTime); ft.Add(); }
                    var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * .99f))];
                    report.AppendLine($"INFO  {name}, {height:0} m up: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps; fog to {FarView.View:0} m, far clip {Camera.main.farClipPlane:0} m, terrain tiles drawn {FarTerrain.Drawn}, trees drawn {(FindAnyObjectByType<TreeField>() is TreeField tfd ? tfd.LastTreesDrawn : -1)}{ft}");
                    yield return Capture($"63_fps_{new string(name.Where(char.IsLetterOrDigit).Take(12).ToArray())}_{height:0}");
                }
            }
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }

        // what the opened view costs: at the summit and Town Center, 500 m up, as built, then without the far trees, then with the far clip at 3000 and 1500 m
        IEnumerator R63FpsX()
        {
            report.AppendLine(); report.AppendLine($"=== What the view from 500 m costs, {Screen.width} x {Screen.height}, 6 s each");
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            foreach (var (name, at) in new[] { R63Places[2], R63Places[1] })
            {
                yield return R63Heli(at, 0f); var h = Heli; if (!h) yield break;
                float t0 = Time.time; while (h.Spool < .95f && Time.time - t0 < 6f) yield return null;
                yield return R63ToHeight(500f, 60f); yield return new WaitForSecondsRealtime(1.5f);
                foreach (var (label, clip, trees, thin) in new[] { ("as built (thinned trees)", 0f, 0f, true), ("trees not thinned", 0f, 0f, false), ("far trees off (700 m)", 0f, 700f, true), ("far clip 3000 m", 3000f, 0f, true), ("far clip 1500 m", 1500f, 0f, true) })
                {
                    FarView.ExpClip = clip; FarView.ExpTrees = trees; TreeField.Thin = thin; yield return new WaitForSecondsRealtime(1f);
                    var frames = new List<float>(); t0 = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - t0 < 6f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                    var tf = FindAnyObjectByType<TreeField>();
                    report.AppendLine($"INFO  {name}, {label}: {frames.Count / frames.Sum():0} fps; trees {(tf ? tf.LastTreesDrawn : -1)}, draws {(tf ? tf.LastDrawCalls : -1)}, terrain tiles {FarTerrain.Drawn}, details hidden {FarView.DetailHidden} of {FarView.DetailCount}");
                }
                FarView.ExpClip = FarView.ExpTrees = 0f; TreeField.Thin = true;
            }
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }

        IEnumerator R63Pics()
        {
            report.AppendLine(); report.AppendLine("=== Pictures");
            var views = Camera.main.GetComponent<CameraViews>();
            // from 500 m over the middle of the map, chase camera, looking west over the whole map
            yield return R63Heli(new Vector2(1500f, -800f), 250f); var h = Heli; if (!h) yield break;
            float t0 = Time.time; while (h.Spool < .95f && Time.time - t0 < 6f) yield return null;
            yield return R63ToHeight(500f, 90f); yield return new WaitForSecondsRealtime(2f);
            yield return Capture("63_500m_over_the_map");
            if (views) views.SetView(CameraViews.View.FarChase); yield return new WaitForSecondsRealtime(1.5f); yield return Capture("63_500m_far_chase"); if (views) views.SetView(CameraViews.View.Chase);
            // straight down over Town Center from 150 m
            yield return R63Heli(R63Places[1].at, 165f); h = Heli; t0 = Time.time; while (h.Spool < .95f && Time.time - t0 < 6f) yield return null;
            yield return R63ToHeight(150f, 60f);
            if (views) views.SetView(CameraViews.View.Down); yield return new WaitForSecondsRealtime(2f); yield return Capture("63_straight_down_town_center");
            if (views) views.SetView(CameraViews.View.Chase);
            Sticks(); yield return Vehicle("original");
        }
    }
}
