using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 69 in the exe (-exeRound69 <dir> [-r69Only ptake,pfly,pcrash,plevel,pedge,pstand,pfps,ppics] -logFile <dir>\Player.log), the plane flown by a
    // scripted pilot (PlaneFlight.Scripted: the stick's pitch and bank, the throttle, the rudder, the brake):
    //   ptake   take-off from runway 09 and from 27: the roll's length and speed, lift-off, a climb to 150 m
    //   pfly    from 09 to Kennesaw Mountain, Town Center and Chastain Glen at 450 m and back, the approach and the landing on 09, the roll-out
    //   pcrash  into a tree (low over the woods), into a building (Town Center's wall), into the ground (a dive): crashed, then reset on open ground,
    //           not stuck (on its wheels, still, a take-off from there)
    //   plevel  hands off at 300 m for 30 s: height and heading held, wings level
    //   pedge   west at 400 m past the map's edge, sticks straight: the warning, the headwind, the turn back (how far out at most)
    //   pstand  the apron: take a parked plane (A / H), leave it for the car
    //   pfps    frame rate crossing the map at 300 m and at 600 m (Round 63: 164 / 99 / 88 fps at 1080p 500 m over Chastain Glen / Town Center / the
    //           mountain; Round 66: 100 / 95 / 96 at the ceiling)
    //   ppics   take-off, over Viking Quarry Lake, over Town Center, Atlanta on the horizon from 600 m, the apron
    public sealed partial class Round20Runner
    {
        PlaneFlight Plane => car ? car.GetComponent<PlaneFlight>() : null;

        IEnumerator BoardPlane(bool west = true)
        {
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("plane"); var pl = Plane;
            if (!pl || !pl.enabled) { Check("the plane is on", false); yield break; }
            pl.Scripted = true; pl.sPitch = pl.sBank = pl.sRudder = pl.sBrake = pl.sSteer = 0f; pl.sThrottle = 0f;
            pl.Recovered(); pl.LineUp(west); yield return new WaitForSecondsRealtime(1.5f);
        }
        void Hands(PlaneFlight pl) { pl.sPitch = pl.sBank = pl.sRudder = pl.sSteer = 0f; }

        // the take-off roll: full throttle, kept on the centreline, past the take-off speed pull back; then a climb to 'climbTo' m over the ground
        IEnumerator TakeOff(string label, float climbTo, Action<float, float> done = null)
        {
            var pl = Plane; var start = car.Body.position; float yaw0 = car.transform.eulerAngles.y, t = 0f, liftAt = -1f, liftV = 0f;
            pl.sThrottle = 1f; Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            while (t < 120f)
            {
                yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                if (pl.Crashed) break;
                if (!pl.Airborne)
                {
                    float err = Mathf.DeltaAngle(car.transform.eulerAngles.y, yaw0); pl.sSteer = Mathf.Clamp(err * .08f, -1f, 1f);
                    pl.sPitch = pl.Airspeed > PlaneFlight.TakeOffSpeed + 2f ? 1f : 0f;
                }
                else
                {
                    if (liftAt < 0f) { liftAt = Flat(car.Body.position - start).magnitude; liftV = pl.Airspeed; }
                    pl.sSteer = 0f; Steer(pl, yaw0, car.Body.position.y + 0f, 0f);
                    pl.sPitch = Mathf.Clamp((12f - pl.PitchDeg) / 8f, -1f, 1f);
                    if (pl.HeightAboveGround > climbTo) break;
                }
            }
            Time.timeScale = 1f;
            report.AppendLine($"  {label}: {(liftAt > 0f ? $"lifted off after {liftAt:0} m of runway at {liftV:0.0} m/s, {climbTo:0} m up in {t:0} s" : pl.Crashed ? "CRASHED: " + pl.CrashReason : "NO LIFT-OFF")}");
            done?.Invoke(liftAt, liftV);
        }

        // the scripted pilot: the bank toward a heading (a coordinated turn), the pitch toward a climb rate that brings it to a height
        void Steer(PlaneFlight pl, float heading, float altitude, float bankMax = 40f, float climbMax = 10f)
        {
            float err = Mathf.DeltaAngle(pl.YawDeg, heading);
            float bankWant = Mathf.Clamp(err * 1.4f, -bankMax, bankMax);
            pl.sBank = Mathf.Clamp((bankWant - pl.RollDeg) / 20f, -1f, 1f);
            float climbWant = Mathf.Clamp((altitude - car.Body.position.y) * .1f, -climbMax, climbMax);
            float pitchWant = Mathf.Asin(Mathf.Clamp(climbWant / Mathf.Max(pl.Airspeed, 20f), -.6f, .6f)) * Mathf.Rad2Deg;
            pl.sPitch = Mathf.Clamp((pitchWant - pl.PitchDeg) / 6f, -1f, 1f);
        }
        static float Bearing(Vector3 from, Vector3 to) => Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg;

        IEnumerator FlyTo(string label, Vector2 to, float height, float throttle, float timeout, List<string> log, List<float> fps = null)
        {
            var pl = Plane; float t = 0f; pl.sThrottle = throttle; Time.timeScale = fps != null ? 1f : 2f; Time.maximumDeltaTime = .5f; float top = 0f, minClear = 999f;
            while (t < timeout)
            {
                if (fps != null)
                {
                    yield return null; t += Time.deltaTime;
                    if (Time.unscaledDeltaTime > 0f && Flat(car.Body.position - new Vector3(to.x, 0, to.y)).magnitude < 1500f) { fps.Add(1f / Time.unscaledDeltaTime); R69Timing(label, true); }
                }
                else { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; }
                if (pl.Crashed) break;
                var p = car.Body.position; float alt = (GroundQuery.TryHeight(p.x, p.z, out float gy) ? gy : 0f) + height;
                // over high ground ahead, the height kept over the highest of the next 1.5 km
                for (float d = 200f; d <= 1500f; d += 200f) { var q = p + (new Vector3(to.x, 0, to.y) - Flat(p)).normalized * d; if (GroundQuery.TryHeight(q.x, q.z, out float qy)) alt = Mathf.Max(alt, qy + height * .6f); }
                Steer(pl, Bearing(p, new Vector3(to.x, 0, to.y)), alt);
                top = Mathf.Max(top, p.y); minClear = Mathf.Min(minClear, pl.HeightAboveGround);
                if (Flat(p - new Vector3(to.x, 0, to.y)).magnitude < 150f) break;
            }
            Time.timeScale = 1f; if (fps != null) R69Timing(label, false);
            log.Add($"{label}: {(pl.Crashed ? "CRASHED: " + pl.CrashReason : t >= timeout ? "NOT REACHED" : $"reached in {t:0} s")} (as high as {top:0} m, as low as {minClear:0} m over the ground, {pl.Airspeed:0} m/s)");
        }

        // the approach and landing on a runway (west: land on 09, toward the east): a long final from 4 km out down a 4 degree slope, the flare, the brakes
        IEnumerator Land(bool on09, List<string> log)
        {
            var pl = Plane; var a = on09 ? PlaneFlight.RunwayWest : PlaneFlight.RunwayEast; var b = on09 ? PlaneFlight.RunwayEast : PlaneFlight.RunwayWest;
            var dir = (b - a).normalized; var touch = a + dir * 250f; float rwyY = GroundQuery.TryHeight(touch.x, touch.y, out float ry) ? ry : 300f;
            var final0 = touch - dir * 4000f;
            yield return FlyTo("to the final approach fix (4 km out, 280 m up)", final0, 280f, .7f, 200f, log);
            // hold the fix's height: fly the slope
            float t = 0f; bool down = false; float tdAt = -1f, tdV = 0f, sinkAtTd = 0f; Time.timeScale = 2f; Time.maximumDeltaTime = .5f; Vector3 tdPos = Vector3.zero;
            while (t < 240f)
            {
                yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                if (pl.Crashed) break;
                var p = car.Body.position; var q = new Vector2(p.x, p.z);
                float along = Vector2.Dot(q - touch, dir), cross = dir.x * (q.y - touch.y) - dir.y * (q.x - touch.x);
                if (pl.Airborne)
                {
                    // the line to fly: a point ahead on the centreline; the height on the glide slope over the touch-down point
                    var aim = touch + dir * (along + 400f); float hdg = Mathf.Atan2(aim.x - q.x, aim.y - q.y) * Mathf.Rad2Deg;
                    float glide = rwyY + PlaneFlight.WheelDrop + Mathf.Max(0f, -along) * Mathf.Tan(4f * Mathf.Deg2Rad);
                    float agl = pl.HeightAboveGround;
                    pl.sThrottle = agl < 8f ? 0f : -along > 1500f ? .45f : .32f;
                    Steer(pl, hdg, p.y, 25f, 6f);   // (the bank toward the line; the pitch below)
                    // down the slope by the plane's own height over the runway (the terrain lookup is off out here): a descent rate that follows the 4
                    // degree slope and closes on it; at 6 m the flare: 1.5 m/s down, the throttle closed
                    float slopeAgl = Mathf.Max(0f, -along) * Mathf.Tan(4f * Mathf.Deg2Rad);
                    float vyWant = agl < 6f ? -1.5f : Mathf.Clamp(-(along < 0f ? pl.Airspeed * Mathf.Tan(4f * Mathf.Deg2Rad) : 2.5f) + (slopeAgl - agl) * .3f, -9f, 2f);
                    float pitchWant = Mathf.Asin(Mathf.Clamp((vyWant + PlaneFlight.Sink(pl.Airspeed)) / Mathf.Max(pl.Airspeed, 20f), -.5f, .5f)) * Mathf.Rad2Deg;
                    pl.sPitch = Mathf.Clamp((pitchWant - pl.PitchDeg) / 5f, -1f, 1f);
                    glide = rwyY + PlaneFlight.WheelDrop + slopeAgl;
                    sinkAtTd = -car.Body.linearVelocity.y;
                    if (Mathf.Repeat(t, 1f) < Time.fixedDeltaTime) log.Add($"    final: {along:0} m from the touch-down point, {cross:+0.0;-0.0} m off the line, {agl:0.0} m up (the slope {glide - rwyY - PlaneFlight.WheelDrop:0.0}), {pl.Airspeed:0} m/s, pitch {pl.PitchDeg:0.0}, coming down {sinkAtTd:0.0} m/s, throttle {pl.Throttle:0.00}");
                }
                else
                {
                    if (!down) { down = true; tdAt = along; tdV = pl.Airspeed; tdPos = p; }
                    Hands(pl); pl.sThrottle = 0f; pl.sBrake = 1f; float err = Mathf.DeltaAngle(car.transform.eulerAngles.y, Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg); pl.sSteer = Mathf.Clamp(err * .05f, -1f, 1f);
                    if (car.Body.linearVelocity.magnitude < .5f) break;
                }
            }
            pl.sBrake = 0f; Time.timeScale = 1f;
            float stop = Flat(car.Body.position - tdPos).magnitude;
            log.Add(down ? $"landed on {(on09 ? "09" : "27")} {tdAt:+0;-0} m from the touch-down point, {tdV:0.0} m/s, coming down {sinkAtTd:0.0} m/s; rolled {stop:0} m to a stop, {Mathf.Abs(Vector2.Dot(new Vector2(car.Body.position.x, car.Body.position.z) - touch, new Vector2(-dir.y, dir.x))):0.0} m off the centreline"
                          : pl.Crashed ? "CRASHED on the approach: " + pl.CrashReason : "NO LANDING");
            Check($"pfly: landed on {(on09 ? "09" : "27")} and stopped", down && !pl.Crashed && car.Body.linearVelocity.magnitude < 1f, down ? $"{tdV:0} m/s at touch-down, {sinkAtTd:0.0} m/s down" : pl.CrashReason);
        }

        IEnumerator R69Take()
        {
            report.AppendLine(); report.AppendLine("=== Take-off from runway 09 (east) and 27 (west)");
            foreach (bool west in new[] { true, false })
            {
                yield return BoardPlane(west); float lift = -1f;
                yield return TakeOff(west ? "09 (from the west end)" : "27 (from the east end)", 150f, (d, v) => lift = d);
                Check($"ptake {(west ? "09" : "27")}: off the runway and up to 150 m", lift > 0f && lift < 1700f && !Plane.Crashed, $"{lift:0} m of runway");
            }
        }

        IEnumerator R69Fly()
        {
            report.AppendLine(); report.AppendLine("=== From 09 to Kennesaw Mountain, Town Center and Chastain Glen at 450 m and back, then the landing on 09 (the scripted pilot, cruise throttle 75 %)");
            yield return BoardPlane(true); yield return TakeOff("09", 120f);
            var log = new List<string>(); float t0 = Time.time; int crashes = Plane.Crashes;
            yield return FlyTo("Kennesaw Mountain's summit", new Vector2(-1123f, -5870f), 450f, .75f, 300f, log);
            yield return FlyTo("Town Center at Cobb", new Vector2(265f, -1207f), 450f, .75f, 300f, log);
            yield return FlyTo("Chastain Glen", new Vector2(2938f, 799f), 450f, .75f, 300f, log);
            yield return FlyTo("back to the airport", new Vector2(-3000f, -1400f), 450f, .75f, 400f, log);
            yield return Land(true, log);
            foreach (var l in log) report.AppendLine("  " + l);
            Check("pfly: the whole flight without a crash", Plane.Crashes == crashes, $"{Plane.Crashes - crashes} crash(es)");
        }

        IEnumerator R69Crash()
        {
            report.AppendLine(); report.AppendLine("=== Crashes: into a tree, into a building, into the ground; then reset onto open ground (not stuck: on its wheels, still, and a take-off from there)");
            var cases = new (string label, Vector2 from, Vector2 to, float height)[]
            {
                ("a tree (low over the woods west of the mountain)", new Vector2(-2000f, -5200f), new Vector2(-1700f, -5600f), 12f),
                ("a building (Town Center's front, 9 m up: Round 63's target)", new Vector2(265.11f, -1207.29f) - new Vector2(Mathf.Sin(165.25f * Mathf.Deg2Rad), Mathf.Cos(165.25f * Mathf.Deg2Rad)) * 420f, new Vector2(265.11f, -1207.29f) + new Vector2(Mathf.Sin(165.25f * Mathf.Deg2Rad), Mathf.Cos(165.25f * Mathf.Deg2Rad)) * 80f, 9f),
                ("the ground (a dive)", new Vector2(-3000f, -3000f), new Vector2(-2600f, -3000f), -1f),
            };
            foreach (var (label, from, to, height) in cases)
            {
                yield return BoardPlane(true); var pl = Plane;
                float gy = GroundQuery.TryHeight(from.x, from.y, out float g) ? g : 400f; float yaw = Bearing(new Vector3(from.x, 0, from.y), new Vector3(to.x, 0, to.y));
                // put in the air there, flying
                respawn.PlaceAt(new Vector3(from.x, gy + (height > 0f ? height + 25f : 200f) + PlaneFlight.WheelDrop, from.y), yaw); race.NotifyTeleported();   // (high enough to start clear of the trees, then down)
                yield return new WaitForFixedUpdate(); pl.LiftOffAt(60f);
                float t = 0f; pl.sThrottle = .7f; Time.timeScale = 2f; Time.maximumDeltaTime = .5f; int c0 = pl.Crashes;
                while (t < 40f && pl.Crashes == c0)
                {
                    yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                    var p = car.Body.position; float h = (GroundQuery.TryHeight(p.x, p.z, out float hy) ? hy : 0f) + (height > 0f ? height : -50f);
                    Steer(pl, yaw, h, 10f, height > 0f ? 30f : 60f);
                    if (height < 0f) pl.sPitch = -1f;
                }
                string why = pl.CrashReason; Hands(pl); pl.sThrottle = 0f;
                yield return new WaitForSecondsRealtime(PlaneFlight.CrashFor / 2f + 2.5f); Time.timeScale = 1f; yield return new WaitForSecondsRealtime(1.5f);
                bool reset = !pl.Crashed && !pl.Airborne && car.transform.up.y > .9f && car.Body.linearVelocity.magnitude < 2f;
                report.AppendLine($"  {label}: {(pl.Crashes > c0 ? $"crashed ({why}), reset at ({car.Body.position.x:0}, {car.Body.position.z:0}), up {car.transform.up.y:0.00}" : "NO CRASH in 40 s")}");
                Check($"pcrash {label}: crashed and reset, not stuck", pl.Crashes > c0 && reset, why);
                // and it can go again from there (a short run and a lift-off, or at least it rolls)
                var at = car.Body.position; pl.sThrottle = 1f; Time.timeScale = 2f; float t2 = 0f;
                while (t2 < 20f && Flat(car.Body.position - at).magnitude < 30f) { yield return new WaitForFixedUpdate(); t2 += Time.fixedDeltaTime; }
                Time.timeScale = 1f; pl.sThrottle = 0f;
                Check($"pcrash {label}: rolls away from the reset spot", Flat(car.Body.position - at).magnitude >= 30f, $"{Flat(car.Body.position - at).magnitude:0} m in {t2:0} s");
            }
        }

        IEnumerator R69Level()
        {
            report.AppendLine(); report.AppendLine("=== Hands off for 30 s at 300 m, throttle 75 %");
            yield return BoardPlane(true); yield return TakeOff("09", 120f);
            var log = new List<string>(); yield return FlyTo("up to 300 m north of the airport", new Vector2(-3000f, 0f), 300f, .75f, 120f, log);
            var pl = Plane; Hands(pl); pl.sThrottle = .75f; float y0 = car.Body.position.y, h0 = pl.YawDeg, minY = y0, maxY = y0, maxRoll = 0f, t = 0f;
            while (t < 30f) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; minY = Mathf.Min(minY, car.Body.position.y); maxY = Mathf.Max(maxY, car.Body.position.y); maxRoll = Mathf.Max(maxRoll, Mathf.Abs(pl.RollDeg)); }
            float dh = Mathf.Abs(Mathf.DeltaAngle(h0, pl.YawDeg));
            report.AppendLine($"  {string.Join("; ", log)}");
            report.AppendLine($"  30 s hands off: height {minY - y0:+0.0;-0.0} to {maxY - y0:+0.0;-0.0} m, heading changed {dh:0.0} deg, the most bank {maxRoll:0.0} deg, {pl.Airspeed:0} m/s");
            Check("plevel: holds its height (within 15 m) and heading (within 5 deg) hands off", maxY - minY < 15f && dh < 5f && !pl.Crashed, $"{maxY - minY:0.0} m, {dh:0.0} deg");
        }

        IEnumerator R69Edge()
        {
            report.AppendLine(); report.AppendLine("=== West at 400 m past the map's edge, the sticks held straight on (heading 270)");
            yield return BoardPlane(true); yield return TakeOff("09", 120f);
            var pl = Plane; var log = new List<string>(); yield return FlyTo("out west", new Vector2(-7800f, -1900f), 400f, 1f, 200f, log);
            pl.MaxOutside = -1e9f; float t = 0f; bool warned = false, back = false; float backAt = 0f; Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            while (t < 120f)
            {
                yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                if (pl.Crashed) break;
                var p = car.Body.position; float alt = (GroundQuery.TryHeight(p.x, p.z, out float gy) ? gy : p.y - 400f) + 400f;
                if (pl.Outside <= 0f && !back) Steer(pl, 270f, alt); else { pl.sBank = 0f; float climb = Mathf.Clamp((alt - p.y) * .1f, -8, 8); pl.sPitch = Mathf.Clamp((Mathf.Asin(Mathf.Clamp(climb / pl.Airspeed, -.5f, .5f)) * Mathf.Rad2Deg - pl.PitchDeg) / 6f, -1, 1); }
                warned |= pl.Leaving;
                if (pl.Outside > 0f && Mathf.Abs(Mathf.DeltaAngle(pl.YawDeg, 90f)) < 60f && !back) { back = true; backAt = t; }
                if (back && pl.Outside < -200f) break;
            }
            Time.timeScale = 1f;
            report.AppendLine($"  {string.Join("; ", log)}");
            report.AppendLine($"  the warning {(warned ? "shown" : "NOT shown")}; at most {pl.MaxOutside:0} m past the edge; turned back toward the map {(back ? $"after {backAt:0} s" : "NEVER")}; {(pl.Outside < -200f ? "back over the map" : $"still {pl.Outside:0} m out")}");
            Check("pedge: warned, turned back, never far out", warned && back && pl.MaxOutside < 900f && !pl.Crashed, $"{pl.MaxOutside:0} m out at most");
        }

        IEnumerator R69Stand()
        {
            report.AppendLine(); report.AppendLine("=== The apron: take a parked plane, leave it for the car");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var st = PlaneStands.Instance; if (!st || st.Spots.Count == 0) { Check("pstand: the parked planes are there", false); yield break; }
            report.AppendLine($"  {st.Spots.Count} planes parked");
            yield return Vehicle("original"); var s = st.Spots[1];
            respawn.PlaceAt(s.at + Quaternion.Euler(0, s.yaw, 0) * Vector3.right * 6f + Vector3.up * .5f, s.yaw); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(1.5f);
            bool took = st.Swap(car, sw); yield return new WaitForSecondsRealtime(1.5f);
            bool on = PlaneFlight.On(car), placed = on && Flat(car.Body.position - s.at).magnitude < 3f;
            Check("pstand: took the parked plane where it stood", took && on && placed, $"{Flat(car.Body.position - s.at).magnitude:0.0} m from the stand");
            yield return new WaitForSecondsRealtime(1f);
            bool left = st.Swap(car, sw); yield return new WaitForSecondsRealtime(1.5f);
            Check("pstand: left it for the Street Classic", left && !PlaneFlight.On(car) && car.GetComponent<VehicleConfiguration>().profileId == "original");
        }

        // the frame-rate legs' CPU / GPU split (FrameTimingManager) and, with -r69Raw, a profiler capture of each leg's measured stretch
        // (<dir>/r69_<leg>.raw, a development build's markers; read it with ChastainRush.ProfileDump.Run -profileRaw <file>)
        double r69Main, r69Render, r69Gpu; int r69Ft; bool r69Rec; readonly FrameTiming[] r69T = new FrameTiming[1];
        public string R69Split => r69Ft > 0 ? $"main {r69Main / r69Ft:0.0} ms, render thread {r69Render / r69Ft:0.0} ms, GPU {r69Gpu / r69Ft:0.0} ms" : "no frame timings";
        void R69Timing(string label, bool on)
        {
            bool raw = Environment.GetCommandLineArgs().Contains("-r69Raw");
            if (on && !r69Rec)
            {
                r69Rec = true; r69Main = r69Render = r69Gpu = 0; r69Ft = 0;
                if (Environment.GetCommandLineArgs().Contains("-r69Markers")) R69Markers(true);
                if (raw) { UnityEngine.Profiling.Profiler.logFile = System.IO.Path.Combine(outputDir, "r69_" + new string(label.Where(char.IsLetterOrDigit).ToArray()) + "_" + Mathf.RoundToInt(car.Body.position.y) + ".raw"); UnityEngine.Profiling.Profiler.enableBinaryLog = true; UnityEngine.Profiling.Profiler.enabled = true; }
            }
            if (on && r69Recs != null) foreach (var (_, r, st) in r69Recs) { long v = r.LastValue; st[0] += v; if (v > st[1]) st[1] = v; if (v > 8000000) st[2]++; }
            if (on) { FrameTimingManager.CaptureFrameTimings(); if (FrameTimingManager.GetLatestTimings(1, r69T) > 0) { r69Main += r69T[0].cpuMainThreadFrameTime; r69Render += r69T[0].cpuRenderThreadFrameTime; r69Gpu += r69T[0].gpuFrameTime; r69Ft++; } }
            if (!on && r69Rec) { r69Rec = false; R69Markers(false); if (raw) { UnityEngine.Profiling.Profiler.enabled = false; UnityEngine.Profiling.Profiler.enableBinaryLog = false; UnityEngine.Profiling.Profiler.logFile = ""; } }
        }

        // -r69Markers (a development build): every profiler marker's time per frame over the leg: the average, the worst frame, and in how many frames it took over 8 ms
        List<(string name, Unity.Profiling.ProfilerRecorder r, long[] st)> r69Recs; public readonly List<string> R69MarkerLines = new List<string>();
        static readonly HashSet<string> R69Noise = new HashSet<string> { "BeginJob", "EndJob", "ScheduleJob", "WaitForCompleted", "ScheduleAllocJob", "KickJobs", "Component.SetGameObject", "Object.SetName", "Idle", "Main Thread", "Semaphore.WaitForSignal", "EngineJob", "PlayerLoop", "Gfx.WaitForGfxCommandsFromMainThread" };
        static readonly (Unity.Profiling.ProfilerCategory cat, string name)[] R69Counters =
        {
            (Unity.Profiling.ProfilerCategory.Render, "Batches Count"), (Unity.Profiling.ProfilerCategory.Render, "SetPass Calls Count"), (Unity.Profiling.ProfilerCategory.Render, "Draw Calls Count"),
            (Unity.Profiling.ProfilerCategory.Render, "Triangles Count"), (Unity.Profiling.ProfilerCategory.Render, "Vertices Count"), (Unity.Profiling.ProfilerCategory.Render, "Shadow Casters Count"),
            (Unity.Profiling.ProfilerCategory.Memory, "Gfx Used Memory"), (Unity.Profiling.ProfilerCategory.Memory, "Texture Memory"), (Unity.Profiling.ProfilerCategory.Memory, "Mesh Memory"), (Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame"),
        };
        void R69Markers(bool on)
        {
            if (on)
            {
                var hs = new List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>(); Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(hs);
                r69Recs = new List<(string, Unity.Profiling.ProfilerRecorder, long[])>();
                foreach (var h in hs)
                {
                    var d = Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription(h);
                    if (d.UnitType != Unity.Profiling.ProfilerMarkerDataUnit.TimeNanoseconds || (d.Flags & Unity.Profiling.LowLevel.MarkerFlags.Counter) != 0) continue;
                    if (R69Noise.Contains(d.Name)) continue;
                    r69Recs.Add((d.Name, Unity.Profiling.ProfilerRecorder.StartNew(d.Category, d.Name, 1), new long[3]));
                }
                foreach (var c in R69Counters) r69Recs.Add(("#" + c.name, Unity.Profiling.ProfilerRecorder.StartNew(c.cat, c.name, 1), new long[3]));
                return;
            }
            if (r69Recs == null) return;
            int n = Mathf.Max(1, r69Ft);
            R69MarkerLines.Add("        counters (average / most): " + string.Join("; ", r69Recs.Where(x => x.name[0] == '#').Select(x => $"{x.name.Substring(1)} {x.st[0] / n:N0} / {x.st[1]:N0}")));
            r69Recs.RemoveAll(x => { if (x.name[0] != '#') return false; x.r.Dispose(); return true; });
            var rows = r69Recs.Where(x => x.st[1] < 10_000_000_000L).Select(x => (x.name, avg: x.st[0] / (double)n / 1e6, max: x.st[1] / 1e6, over: x.st[2])).ToList();
            foreach (var x in r69Recs) x.r.Dispose(); r69Recs = null;
            R69MarkerLines.Add("        physics: " + string.Join("; ", rows.Where(x => x.name == "FixedUpdate.PhysicsFixedUpdate" || x.name == "Physics.Simulate" || x.name == "Physics.SyncTransforms" || x.name == "FixedBehaviourUpdate" || x.name == "Physics.SwapPhysXBuffersJob").Select(x => $"{x.name} {x.avg:0.00} ms avg, worst {x.max:0} ms, {x.over} frames over 8 ms")));
            R69MarkerLines.Add("        by average: " + string.Join("; ", rows.OrderByDescending(x => x.avg).Take(30).Select(x => $"{x.name} {x.avg:0.00}")));
            R69MarkerLines.Add("        by worst frame: " + string.Join("; ", rows.OrderByDescending(x => x.max).Take(30).Select(x => $"{x.name} {x.max:0} ms ({x.over} over 8 ms)")));
        }

        // -r69Meshes: what the loaded meshes cost (Profiler.GetRuntimeMemorySizeLong: the CPU copy of a readable mesh and its GPU buffers), grouped by
        // name (digits dropped), with how many are readable and whether a renderer draws them or only a collider uses them
        static string R69Group(string n) { var t = new string((n ?? "").Where(c => !char.IsDigit(c)).ToArray()).Trim(' ', '_', '-', '(', ')', '.'); return t.Length == 0 ? "(no name)" : t; }
        void R69MeshCensus()
        {
            var drawn = new HashSet<Mesh>(); var collided = new HashSet<Mesh>();
            foreach (var f in FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (f.sharedMesh && f.GetComponent<MeshRenderer>()) drawn.Add(f.sharedMesh);
            foreach (var c in FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (c.sharedMesh) collided.Add(c.sharedMesh);
            var all = Resources.FindObjectsOfTypeAll<Mesh>();
            long total = 0, readable = 0, colliderOnly = 0, unused = 0;
            var groups = new Dictionary<string, (long bytes, int n, int readable, int drawnN, int colOnly, long verts)>();
            foreach (var m in all)
            {
                long b = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(m); total += b; if (m.isReadable) readable += b;
                bool d = drawn.Contains(m), c = collided.Contains(m); if (c && !d) colliderOnly += b; if (!c && !d) unused += b;
                var k = R69Group(m.name); groups.TryGetValue(k, out var g);
                groups[k] = (g.bytes + b, g.n + 1, g.readable + (m.isReadable ? 1 : 0), g.drawnN + (d ? 1 : 0), g.colOnly + (c && !d ? 1 : 0), g.verts + m.vertexCount);
            }
            report.AppendLine($"  meshes loaded: {all.Length}, {total / 1048576} MB (readable ones {readable / 1048576} MB; used only by a collider {colliderOnly / 1048576} MB; neither drawn nor a collider {unused / 1048576} MB)");
            report.AppendLine($"  textures: {Resources.FindObjectsOfTypeAll<Texture>().Sum(t => UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)) / 1048576} MB; the graphics driver's estimate {UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576} MB; {SystemInfo.graphicsDeviceName} {SystemInfo.graphicsMemorySize} MB");
            foreach (var kv in groups.OrderByDescending(x => x.Value.bytes).Take(45))
                report.AppendLine($"    {kv.Value.bytes / 1048576,6} MB  {kv.Value.n,6} meshes ({kv.Value.readable} readable, {kv.Value.drawnN} drawn, {kv.Value.colOnly} collider only), {kv.Value.verts / 1000} k vertices  {kv.Key}");
            Flush();
        }
        // -r69Meshes: at each leg's end, the triangles of the renderers in view (isVisible) by name group, and the trees' own draw
        void R69VisibleCensus(string label)
        {
            var groups = new Dictionary<string, (long tris, int n)>();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!r.isVisible || !r.enabled || r.forceRenderingOff) continue; var f = r.GetComponent<MeshFilter>(); if (!f || !f.sharedMesh) continue;
                var m = f.sharedMesh; long t = 0; for (int i = 0; i < m.subMeshCount; i++) t += m.GetIndexCount(i) / 3;
                var k = R69Group(r.gameObject.name); groups.TryGetValue(k, out var g); groups[k] = (g.tris + t, g.n + 1);
            }
            foreach (var tr in FindObjectsByType<Terrain>(FindObjectsSortMode.None)) if (tr.drawHeightmap) { groups.TryGetValue("(terrain tiles drawn)", out var g); groups["(terrain tiles drawn)"] = (g.tris, g.n + 1); }
            var mall = TownCenterMall.Current; if (mall) report.AppendLine($"      {label}: Town Center's inside {(mall.Shown ? "DRAWN" : "not drawn")} ({mall.Distance(car.Body.position):0} m from the building across, {car.Body.position.y - mall.roof:0} m over its roof)");
            report.AppendLine($"      {label}, in view at {car.Body.position.y:0} m: " + string.Join("; ", groups.OrderByDescending(x => x.Value.tris).Take(25).Select(x => $"{x.Key} {x.Value.tris / 1000} k tris ({x.Value.n})")));
            Flush();
        }

        IEnumerator R69Fps()
        {
            report.AppendLine(); report.AppendLine("=== Frame rate crossing the map (1280 x 720, not measured at 3840 x 2160), at 300 m and 600 m over the ground (real time, the frames over the last 1.5 km to each place)");
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;   // (a check run's settings may cap it at 60, PerfCheckRunner's way)
            if (Environment.GetCommandLineArgs().Contains("-r69MallAsBefore")) { TownCenterMall.HeightCounts = false; report.AppendLine("  (BEFORE: -r69MallAsBefore, Town Center's inside drawn from any height over it)"); }
            if (Environment.GetCommandLineArgs().Contains("-r69TrunksAsBefore")) { TreeTrunks.HighSkip = false; report.AppendLine("  (BEFORE: -r69TrunksAsBefore, the trunk colliders follow the plane at any height)"); }
            bool census = Environment.GetCommandLineArgs().Contains("-r69Meshes"); if (census) R69MeshCensus();
            yield return BoardPlane(true); yield return TakeOff("09", 120f);
            var legs = new (string name, Vector2 at)[] { ("Kennesaw Mountain", new Vector2(-1123f, -5870f)), ("Town Center", new Vector2(265f, -1207f)), ("Chastain Glen", new Vector2(2938f, 799f)) };
            foreach (float h in new[] { 300f, 600f })
            {
                var log = new List<string>(); var line = new List<string>();
                foreach (var (name, at) in (h > 400f ? legs.Reverse() : legs))
                {
                    var fr = new List<float>(); yield return FlyTo(name, at, h, .8f, 300f, log, fr); if (census) R69VisibleCensus(name);
                    foreach (var ml in R69MarkerLines) log.Add(name + ml); R69MarkerLines.Clear();
                    fr.Sort(); line.Add(fr.Count > 0 ? $"{name} {fr.Average():0} fps (1 % low {fr[Mathf.Max(0, fr.Count / 100)]:0}; {fr.Count(f => f < 20f)} frames over 50 ms; {R69Split})" : $"{name} -");
                }
                report.AppendLine($"  at {h:0} m: {string.Join("; ", line)}");
                foreach (var l in log) report.AppendLine("      " + l);
            }
        }

        IEnumerator R69Pics()
        {
            report.AppendLine(); report.AppendLine("=== Pictures");
            yield return Clock(14f);
            // the apron
            var st = PlaneStands.Instance;
            if (st && st.Spots.Count > 0) { var s = st.Spots[1].at; yield return Vehicle("original"); respawn.PlaceAt(s + new Vector3(60f, .5f, -60f), 0f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f); yield return Still("69_apron", s + new Vector3(-28f, 14f, -38f), s + new Vector3(0, 1f, 0), 60f); }
            // the take-off: from beside the runway as it lifts off
            yield return BoardPlane(true); var pl = Plane; pl.sThrottle = 1f; float t = 0f;
            while (t < 60f && !pl.Airborne) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; pl.sPitch = pl.Airspeed > PlaneFlight.TakeOffSpeed + 2f ? 1f : 0f; }
            for (float w = 0f; w < 1.2f; w += Time.fixedDeltaTime) { yield return new WaitForFixedUpdate(); pl.sPitch = Mathf.Clamp((10f - pl.PitchDeg) / 8f, -1, 1); }
            yield return Frozen("69_takeoff", -18f, 22f, 4f);
            // over the quarry, over Town Center, Atlanta from 600 m
            var log = new List<string>();
            yield return FlyTo("the quarry", new Vector2(-2500f, -1500f), 90f, .8f, 120f, log); yield return Frozen("69_over_quarry", -30f, 12f, 14f);
            yield return FlyTo("Town Center", new Vector2(150f, -1150f), 220f, .8f, 200f, log); yield return Frozen("69_over_town_center", -40f, 0f, 22f);
            yield return FlyTo("600 m up south-east", new Vector2(1500f, -3500f), 600f, .8f, 200f, log);
            // looking south-east (Atlanta, about 30 km off at 125 degrees) over the plane
            Time.timeScale = 0f; var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false;
            var p = car.Body.position; var dirSE = Quaternion.Euler(0, 128f, 0) * Vector3.forward;
            cam.transform.position = p - dirSE * 25f + Vector3.up * 6f; cam.transform.LookAt(p + dirSE * 400f - Vector3.up * 40f);
            yield return null; yield return Capture("69_atlanta_from_600m"); if (chase) { chase.enabled = was; chase.Snap(); } Time.timeScale = 1f;
            foreach (var l in log) report.AppendLine("  " + l);
            Hands(pl); pl.Scripted = false;
        }
        // a picture of the plane where it is, frozen, the camera 'back' behind it, 'side' to its right, 'up' over it
        IEnumerator Frozen(string name, float back, float side, float up)
        {
            Time.timeScale = 0f; var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false;
            var p = car.Body.position; var f = Flat(car.transform.forward).normalized; var r = Vector3.Cross(Vector3.up, f);
            cam.transform.position = p + f * back + r * side + Vector3.up * up; cam.transform.LookAt(p + f * 30f - Vector3.up * (up * .4f));
            yield return null; yield return Capture(name); if (chase) { chase.enabled = was; chase.Snap(); } Time.timeScale = 1f;
            report.AppendLine($"  {name}: at ({p.x:0}, {p.y:0}, {p.z:0}), {Plane.HeightAboveGround:0} m over the ground");
        }
    }
}
