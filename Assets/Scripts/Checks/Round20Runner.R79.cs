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
    // Round 79 in the exe (-exeRound79 <dir> [-r79Only dan,air,arrow,replay,rail] [-r79ArrowOld] [-r78Lines <file>] -logFile <dir>\Player.log):
    // CHECK IT AS DAN PLAYS IT (TODO.md item 0).
    //   dan     BUG-001 from Dan's exact place: the Air Tour, ring 21 next, his position, heading and speed (the edge warning must not show)
    //   air     each air course (Quarry Air Race, Highway Air Race, Air Tour) flown by the human-like pilot by the arrow with three AI pilots (Hard):
    //           the farthest any plane came toward the flight boundary, any edge warning; a picture where the tour passes Dan's spot
    //   arrow   each air course flown 70 s at normal speed by the human-like pilot: the 3D arrow's wobble frame to frame (its turn as seen in the view
    //           and its place on the screen, a step against the step before), pictures in the chase and the cockpit views; -r79ArrowOld: Round 74's way
    //   replay  the Marietta Square race (1 lap, 3 AI), the human-like driver beating the train at Mill Street; then the replay: the train cars and the
    //           crossing's gates in the replay against where they were in the race, pictures (TV and chase) as the player crosses
    //   rail    D: the Canton Road Connector over the railroad both ways, Cobb Parkway under the rail bridge both ways, the other listed decks
    public sealed partial class Round20Runner
    {
        static readonly string[] R79Courses = { "quarry_air_race.json", "highway_air_race.json", "air_tour.json" };

        // the human-like pilot (Round 78's): toward the ring the race points at (the arrow's), along the race's line within 700 m of it, a little noise;
        // after three crashes before one ring the AI pilot's code takes that ring (said in the report)
        IEnumerator R79Fly(RaceRoute r, float limit, float scale, Func<bool> stop, Action each, StringBuilder note)
        {
            var pl = car.GetComponent<PlaneFlight>(); var p = race.Player; float t = 0f, bankCmd = 0f, pitchCmd = 0f, seed = (float)r73rng.NextDouble() * 50f;
            int ringCrashes = 0, lastRing = -1, crashes0 = pl.Crashes, handed = 0; PlanePilot helper = null; bool wasCrashed = false;
            Time.timeScale = scale; Time.maximumDeltaTime = .5f;
            while (t < limit && !p.Finished && !(stop != null && stop()))
            {
                yield return new WaitForFixedUpdate(); float dt = Time.fixedDeltaTime; t += dt; each?.Invoke();
                var tgt = race.Target; if (tgt == null) continue;
                if (tgt.Id != lastRing) { lastRing = tgt.Id; ringCrashes = 0; if (helper) { Destroy(helper); helper = null; pl.Scripted = false; pl.sThrottle = -1f; R73Pad(); } }
                if (pl.Crashed && !wasCrashed) ringCrashes++; wasCrashed = pl.Crashed;
                if (ringCrashes >= 3 && !helper) { helper = car.gameObject.AddComponent<PlanePilot>(); helper.Initialize(race, car, p, 0, 0f); handed++; note.Append($" the AI pilot's code took ring {tgt.Id} after 3 crashes;"); }
                if (helper) continue;
                Vector3 aimAt = tgt.Position; float dist = (tgt.Position - car.Body.position).magnitude;
                var to = aimAt - car.Body.position; var fw = car.transform.forward;
                float yawErr = Vector3.SignedAngle(Flat(fw), Flat(to), Vector3.up) + (Mathf.PerlinNoise(seed, t * .3f) - .5f) * 4f;
                if (Flat(to).magnitude > 600f)
                {
                    float hiGround = float.MinValue; var dirF = Flat(to).normalized;
                    for (float k = 0f; k <= 600f; k += 50f) { var w = car.Body.position + dirF * k; float gh = GroundQuery.Height(w.x, w.z); if (!float.IsNaN(gh)) hiGround = Mathf.Max(hiGround, gh); }
                    float floorY = hiGround + 55f; if (aimAt.y < floorY) { aimAt.y = floorY; to = aimAt - car.Body.position; }
                }
                if (dist < 700f) { float L = r.LapLength, sR = r.LapStationOf(car.Body.position), sT = Mathf.Repeat(tgt.S - r.GateS, L); float ahead_ = Mathf.Repeat(sT - sR, L); if (ahead_ < 900f) { aimAt = r.PointAt(r.GateS + sR + Mathf.Min(ahead_, Mathf.Clamp(car.Body.linearVelocity.magnitude * 1.6f, 50f, 140f)), out _); to = aimAt - car.Body.position; } }
                float climb = Mathf.Atan2(to.y, Flat(to).magnitude) * Mathf.Rad2Deg;
                float bankWant = Mathf.Clamp(yawErr * 2.0f, -65f, 65f);
                bankCmd += (Mathf.Clamp((bankWant - pl.RollDeg) / 18f, -1f, 1f) - bankCmd) * Mathf.Clamp01(dt / .2f);
                pitchCmd += (Mathf.Clamp((Mathf.Clamp(climb, -28f, 28f) - pl.PitchDeg) / 10f, -1f, 1f) - pitchCmd) * Mathf.Clamp01(dt / .2f);
                if (!pl.Airborne) { bankCmd = Mathf.Clamp(yawErr / 30f, -1f, 1f) * .4f; pitchCmd = pl.Airspeed > 32f ? .7f : 0f; }
                R73Set(bankCmd, 1f, 0f, -pitchCmd);
            }
            if (helper) { Destroy(helper); pl.Scripted = false; pl.sThrottle = -1f; }
            Time.timeScale = 1f; R73Set(0, 0, 0);
            note.Insert(0, $" crashes {pl.Crashes - crashes0}, the AI's code took {handed} ring(s);");
        }

        void R79Start(string file, int ai)
        {
            RoadDriver.Difficulty = 2;
            RaceController.RouteOverride = file; RaceController.DirectionOverride = "forward"; RaceController.LapsOverride = 1; RaceController.AiCount = ai;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            race.Reload(); race.StartRace(); R73Pad();
        }

        // ---------------------------------------------------------------- dan
        IEnumerator R79Dan()
        {
            report.AppendLine(); report.AppendLine("=== Dan's BUG-001 (session 2026-10-28_11-32-04-219_308d8c): the Air Tour, ring 21 next, from his place, heading and speed");
            yield return Clock(13f);
            R79Start("air_tour.json", 0); var r = race.Route; if (r == null) { Check("dan: the route", false); yield break; }
            while (race.State == RaceController.Stage.Countdown) { R73Set(0, 0, 0); yield return null; }
            var pl = car.GetComponent<PlaneFlight>(); race.Player.NextCheckpoint = 20; pl.MaxOutside = -1e9f;
            pl.AirStart(new Vector3(1069.84f, 540.74f, -8181.96f), 204.04f, 0f, 124.3f);
            float t = 0f; bool warned = false; while (t < 1.5f) { R73Set(0, .9f, 0); yield return null; t += Time.deltaTime; warned |= pl.Leaving; }
            yield return Capture("79_BUG-001_after");
            report.AppendLine($"  at ({car.Body.position.x:0}, {car.Body.position.y:0}, {car.Body.position.z:0}) heading {pl.YawDeg:0}: {(pl.Leaving ? "LEAVING THE SURVEY AREA shown" : "no warning")}; {(-pl.Outside):0} m inside the flight boundary; " +
                              $"{MapEdge.Land?.Distance(car.Body.position.x, car.Body.position.z) ?? 0f:0} m from the land's edge (negative: over the scenery past it); next ring {race.Target?.Id} ({race.Target?.Label}) {(race.Target != null ? (race.Target.Position - car.Body.position).magnitude : 0f):0} m; picture 79_BUG-001_after.png");
            Check("dan: no edge warning at Dan's place on the way to ring 21", !warned, warned ? "warned" : "");
            // and on toward ring 21 for 20 s by the human-like pilot (it is reachable)
            var note = new StringBuilder(); int next0 = race.Player.NextCheckpoint;
            yield return R79Fly(r, 40f, 1f, () => race.Player.NextCheckpoint != next0, () => warned |= pl.Leaving, note);
            report.AppendLine($"  then on: ring {(race.Player.NextCheckpoint != next0 ? "21 passed" : "21 NOT reached")} in the flight;{note} warning on the way {warned}");
            Check("dan: ring 21 reached from there with no warning", race.Player.NextCheckpoint != next0 && !warned);
            race.EndRace();
        }

        // ---------------------------------------------------------------- air
        IEnumerator R79Air()
        {
            report.AppendLine(); report.AppendLine($"=== B. Each air course flown by the human-like pilot with 3 AI pilots (Hard): the flight boundary (the land's outer rectangle and {MapEdge.FlightRoom:0} m); warnings");
            yield return Clock(13f);
            foreach (var file in R79Courses)
            {
                R79Start(file, 3); var r = race.Route; if (r == null) { Check($"air {file}: the route", false); continue; }
                while (race.State == RaceController.Stage.Countdown) { R73Set(0, 0, 0); yield return null; }
                var planes = race.Entrants.Where(e => e.Car).Select(e => (e, f: e.Car.GetComponent<PlaneFlight>())).Where(x => x.f).ToList();
                foreach (var x in planes) x.f.MaxOutside = -1e9f;
                float warnSecs = 0f, nearDan = float.MaxValue; var dan = new Vector3(1069.84f, 0f, -8181.96f);
                var note = new StringBuilder(); var me = car.GetComponent<PlaneFlight>();
                yield return R79Fly(r, 1300f, 2f, null, () =>
                {
                    foreach (var x in planes) if (x.f.Leaving) warnSecs += Time.fixedDeltaTime / Mathf.Max(1, planes.Count);
                    if (file == "air_tour.json") { float d = Flat(car.Body.position - dan).magnitude; if (d < nearDan) nearDan = d; }
                }, note);
                float w = 0f; while (race.Entrants.Any(e => !e.Finished) && w < 200f) { yield return new WaitForSecondsRealtime(.5f); w += .5f; }
                report.AppendLine($"  {r.Name}: {(race.Player.Finished ? "finished " + RaceController.Fmt(race.Player.FinalTotal) : "NOT finished")}, missed {race.Player.MissedTotal};{note} " +
                                  string.Join("; ", planes.Select(x => $"{x.e.Name}: nearest the flight boundary {-x.f.MaxOutside:0} m inside{(x.e.Finished ? "" : " (not finished)")}")) +
                                  (file == "air_tour.json" ? $"; the player passed {nearDan:0} m from Dan's BUG-001 spot" : ""));
                Check($"air {r.Name}: no edge warning for any plane on the course", planes.All(x => x.f.MaxOutside < -HelicopterFlight.WarnFrom), string.Join(", ", planes.Select(x => $"{-x.f.MaxOutside:0}")));
                Check($"air {r.Name}: the human-like pilot finished", race.Player.Finished);
                race.EndRace(); yield return new WaitForSecondsRealtime(1f);
            }
            // the tour where it passes Dan's spot: the chase camera there, ring 21 next, on the course's own line
            R79Start("air_tour.json", 0); var rt = race.Route;
            while (race.State == RaceController.Stage.Countdown) { R73Set(0, 0, 0); yield return null; }
            float best = float.MaxValue, sBest = 0f; for (float s = 0f; s < rt.LapLength; s += 10f) { var q = rt.PointAt(rt.GateS + s, out _); float d = Flat(q - new Vector3(1069.84f, 0f, -8181.96f)).magnitude; if (d < best) { best = d; sBest = s; } }
            var at = rt.PointAt(rt.GateS + sBest - 300f, out _); var nx = rt.PointAt(rt.GateS + sBest - 290f, out _); var f0 = Flat(nx - at).normalized;
            race.Player.NextCheckpoint = 20; var pf = car.GetComponent<PlaneFlight>(); pf.AirStart(at, Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg, 0f, 110f);
            var nn = new StringBuilder(); yield return R79Fly(rt, 3.2f, 1f, null, null, nn);
            yield return Capture("79_air_tour_past_dan_spot");
            report.AppendLine($"  the tour's line passes {best:0} m from Dan's spot; a picture flying it there (ring 21 next): 79_air_tour_past_dan_spot.png ({(pf.Leaving ? "WARNING shown" : "no warning")})");
            race.EndRace();
        }

        // ---------------------------------------------------------------- arrow
        IEnumerator R79Arrow()
        {
            bool old = AirGuideArrow.Old;
            report.AppendLine(); report.AppendLine($"=== C. The air arrow ({(old ? "BEFORE: Round 74's way, -r79ArrowOld" : "Round 79's")}): 70 s of each course at normal speed by the human-like pilot; its wobble a frame against the frame before");
            yield return Clock(13f);
            var views = FindAnyObjectByType<CameraViews>(); string tag = old ? "old" : "new";
            foreach (var file in R79Courses)
            {
                R79Start(file, 0); var r = race.Route; if (r == null) continue;
                while (race.State == RaceController.Stage.Countdown) { R73Set(0, 0, 0); yield return null; }
                float angSum = 0f, pixSum = 0f, angMax = 0f; int n = 0; Vector3 f1 = Vector3.zero, f2 = Vector3.zero, s1 = Vector3.zero, s2 = Vector3.zero; int have = 0, lastId = -1; float switchedAt = -9f;
                bool sampling = true;
                IEnumerator Sample()
                {
                    while (sampling)
                    {
                        yield return new WaitForEndOfFrame();
                        if (!AirGuideArrow.Shown || race.Target == null) { have = 0; continue; }
                        if (race.Target.Id != lastId) { lastId = race.Target.Id; switchedAt = Time.unscaledTime; have = 0; }
                        var pl = car.GetComponent<PlaneFlight>(); if (pl.Crashed || !pl.Airborne || Time.unscaledTime - switchedAt < 1.2f) { have = 0; continue; }
                        var f = AirGuideArrow.ShownOnScreen * Vector3.forward; var sp = AirGuideArrow.ShownAt;
                        if (have >= 2)
                        {
                            float a = Vector3.Angle(f - f1, f1 - f2) > 0f ? ((f - f1) - (f1 - f2)).magnitude * Mathf.Rad2Deg : 0f;
                            float px = ((sp - s1) - (s1 - s2)).magnitude;
                            angSum += a; pixSum += px; angMax = Mathf.Max(angMax, a); n++;
                        }
                        f2 = f1; f1 = f; s2 = s1; s1 = sp; have++;
                    }
                }
                var co = StartCoroutine(Sample()); var note = new StringBuilder();
                // 70 s: 30 s in the chase view, a picture, 20 s in the cockpit, a picture, 20 s back in the chase view
                float t0 = Time.time; bool pic1 = false, pic2 = false;
                yield return R79Fly(r, 70f, 1f, () =>
                {
                    float e = Time.time - t0;
                    if (!pic1 && e > 28f) { pic1 = true; StartCoroutine(Capture($"79_arrow_{tag}_{Path.GetFileNameWithoutExtension(file)}_chase")); if (views) views.SetView(CameraViews.View.FirstPerson); }
                    if (!pic2 && e > 48f) { pic2 = true; StartCoroutine(Capture($"79_arrow_{tag}_{Path.GetFileNameWithoutExtension(file)}_cockpit")); if (views) views.SetView(CameraViews.View.Chase); }
                    return false;
                }, null, note);
                sampling = false; StopCoroutine(co); if (views) views.SetView(CameraViews.View.Chase);
                report.AppendLine($"  {r.Name}: {n} frames; the arrow's turn in the view {angSum / Mathf.Max(1, n):0.000} deg a frame against the frame before on average (worst {angMax:0.00}), its place on the screen {pixSum / Mathf.Max(1, n):0.00} px;{note}");
                Check($"arrow {r.Name}: measured over 1000 frames or more", n >= 1000, n.ToString());
                race.EndRace(); yield return new WaitForSecondsRealtime(1f);
            }
        }

        // ---------------------------------------------------------------- replay
        IEnumerator R79Replay()
        {
            report.AppendLine(); report.AppendLine("=== A. The train in the replay: the Marietta Square race (1 lap, 3 AI, Normal), the human-like driver beats the train at Mill Street; then its replay");
            yield return Clock(13f);
            UseRoute("marietta_square.json", "forward"); var r = race.Route; if (r == null) { Check("replay: the route", false); yield break; }
            yield return Vehicle("original");
            RaceController.LapsOverride = 1; RaceController.AiCount = 3; RoadDriver.Difficulty = 1; race.StartRace(); R73Pad();
            while (race.State == RaceController.Stage.Countdown) yield return null;
            var line = new List<Vector3>(r.LeadIn ?? new Vector3[0]); for (float s = 0f; s <= r.LapLength; s += 3f) line.Add(r.PointAt(r.GateS + s, out _));
            var rt = RailTrains.Instance; var cross = FindObjectsByType<RailCrossing>(FindObjectsSortMode.None).OrderBy(c => Flat(c.center - R78Mill).magnitude).First();
            bool aligned = false; float crossedAt = -1f;
            var samples = new List<(float t, List<Vector3> cars, float arm, bool closed, Vector3 player)>();
            Func<float, float> speedAt = s =>
            {
                var p = car.Body.position; float d = Flat(p - cross.center).magnitude;
                if (!aligned && d < 260f && p.x > cross.center.x) { aligned = true; float eta = d / Mathf.Max(8f, car.Body.linearVelocity.magnitude); rt.Align(cross.center, eta + 12f, true); }
                if (crossedAt < 0f && d < 6f && cross.Closed) crossedAt = race.RaceClock;
                return 99f;
            };
            float nextSample = 0f;
            var watch = StartCoroutine(R73Watch(() =>
            {
                if (race.RaceClock < nextSample) return; nextSample = race.RaceClock + .5f;
                var cars = new List<Vector3>(); RailTrains.ForEachCar((a, b) => cars.Add((a + b) * .5f));
                samples.Add((race.RaceClock, cars, cross.ArmAngle, cross.Closed, car.Body.position));
            }, () => true));
            var run = new HumanRun(); Time.timeScale = 1f;
            yield return HumanDrive(line, 30f, 300f, run, () => race.Player.Finished, speedAt, new List<(string, float)>(), "79_A_in_the_air");
            StopCoroutine(watch);
            float w = 0f; while ((ReplayRecorder.Instance && ReplayRecorder.Instance.Recording) && w < 150f) { yield return new WaitForSecondsRealtime(.5f); w += .5f; }
            report.AppendLine($"  the race: {(race.Player.Finished ? "finished " + RaceController.Fmt(race.Player.FinalTotal) : "NOT finished")}; resets {run.resets}; crossed Mill Street while closed at {crossedAt:0.0} s (race clock); {ReplayRecorder.Note}");
            Check("replay: the player beat the train across the closed crossing", crossedAt > 0f && race.Player.Finished, $"at {crossedAt:0.0} s");
            var data = ReplayRecorder.Instance ? ReplayRecorder.Instance.Last : null; if (data == null) { Check("replay: the replay", false); yield break; }
            int clocks = data.events.Count(e => e.kind == ReplayData.Kind.Clock);
            report.AppendLine($"  the replay: {data.Duration:0} s, {data.events.Count} events of which {clocks} clocks");
            var v = ReplayViewer.Open(data); if (!v) { Check("replay: the viewer opened", false); yield break; }
            yield return new WaitForSecondsRealtime(1f); v.TogglePause();   // paused: each moment sought and compared
            // the train in the replay against the race, at every sample with cars near the player
            float errSum = 0f, errMax = 0f; int nCars = 0, nTimes = 0, armBad = 0, carsMissing = 0;
            foreach (var smp in samples.Where(x => x.cars.Count > 0 && x.cars.Any(c => Flat(c - x.player).magnitude < 500f)))
            {
                v.Seek(smp.t); yield return null; yield return null;
                var rep = new List<Vector3>(); RailTrains.ForEachCar((a, b) => rep.Add((a + b) * .5f));
                foreach (var c in smp.cars.Where(c => Flat(c - smp.player).magnitude < 500f))
                {
                    if (rep.Count == 0) { carsMissing++; continue; }
                    float d = rep.Min(q => (q - c).magnitude); errSum += d; errMax = Mathf.Max(errMax, d); nCars++;
                }
                if (Mathf.Abs(cross.ArmAngle - smp.arm) > 4f || cross.Closed != smp.closed) armBad++;
                nTimes++;
            }
            report.AppendLine($"  {nTimes} moments compared: the train's cars in the replay {errSum / Mathf.Max(1, nCars):0.00} m from where they were in the race on average (worst {errMax:0.00} m, {nCars} cars, {carsMissing} missing); the Mill Street gates off by more than 4 degrees or lamps different at {armBad} moments");
            Check("replay: the train in the replay where it was in the race (under 1.5 m on average, none missing)", nTimes > 5 && nCars > 20 && errSum / Mathf.Max(1, nCars) < 1.5f && carsMissing == 0, $"{errSum / Mathf.Max(1, nCars):0.00} m");
            Check("replay: the crossing's gates and lamps as they were", nTimes > 5 && armBad == 0, $"{armBad} of {nTimes}");
            // pictures as the player crosses: TV and chase, paused there, then played on 3 s
            float pt = crossedAt > 0f ? crossedAt - 1.5f : data.t0 + 30f;
            v.Seek(pt); v.SetView(ReplayViewer.Cam.TV); yield return new WaitForSecondsRealtime(.8f); yield return Capture("79_A_replay_tv_crossing");
            v.SetView(ReplayViewer.Cam.Chase); yield return new WaitForSecondsRealtime(.8f); yield return Capture("79_A_replay_chase_crossing");
            v.TogglePause(); yield return new WaitForSecondsRealtime(3f); yield return Capture("79_A_replay_chase_crossing_plus3s");
            v.SetView(ReplayViewer.Cam.TV); yield return new WaitForSecondsRealtime(2f); yield return Capture("79_A_replay_tv_after");
            v.Close(); yield return new WaitForSecondsRealtime(1f);
            race.EndRace();
        }

        // ---------------------------------------------------------------- rail
        IEnumerator R79Rail()
        {
            report.AppendLine(); report.AppendLine("=== D. The Canton Road Connector over the railroad, Cobb Parkway under its rail bridge, the other listed decks: driven through");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original"); yield return Clock(13f);
            // the connector both ways on its own lanes, Round 78's stuck spot (southbound) first
            var runs = new (string name, Vector3 from, float yaw, float secs)[]
            {
                ("connector southbound", new Vector3(1655f, 0f, -5980f), 193f, 9f),
                ("connector northbound", new Vector3(1612f, 0f, -6170f), 13f, 9f),
            };
            foreach (var x in runs)
            {
                yield return R74Place(x.from, x.yaw); yield return Capture("79_D_" + x.name.Replace(' ', '_') + "_start");
                string res = ""; yield return R78DriveThrough(x.from, x.yaw, x.secs, .8f, s => res = s);
                report.AppendLine($"  {x.name}: {res}");
                Check($"rail: the {x.name} drives through (no stop, no air over 0.4 s)", !lastDrive.stopped && lastDrive.air < .4f && lastDrive.dist > 120f, res);
            }
            yield return R74Place(new Vector3(1628f, 0f, -6075f), 193f); yield return Capture("79_D_conn_sb_end_after");
            var L = R78L();
            if (L?.rail != null)
                foreach (var p in L.rail.Where(q => q.name.Contains("Connector") || q.name.Contains("Express") || q.name.StartsWith("Cobb_Parkway")))
                {
                    var at = new Vector3(p.x, 0f, p.z); yield return R74Place(at, p.heading); yield return Capture("79_D_" + p.name);
                    foreach (bool back in new[] { false, true })
                    {
                        string res = "";
                        if (p.line != null && p.line.Length >= 8) yield return R78Human(p.line, back, s => res = s);
                        else { if (back) continue; yield return R78DriveThrough(at, p.heading, Mathf.Clamp(p.len / 14f, 6f, 11f), .65f, s => res = s); }
                        bool ok = !lastDrive.stopped && lastDrive.air < .4f;
                        report.AppendLine($"  {p.name} ({p.kind}){(back ? " the other way" : "")}: {res}{(ok ? "" : "  <- LOOK")}");
                        Check($"rail: {p.name.Replace('_', ' ')}{(back ? " the other way" : "")} drives through", ok, res);
                    }
                }
            else Check("rail: Round 78's lines file (local/round78/r78_lines.json)", false);
        }
    }
}
