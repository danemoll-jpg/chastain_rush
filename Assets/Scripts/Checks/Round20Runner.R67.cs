using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 67 in the exe (-exeRound67 <dir> [-r67Only mloop,mjumps,mwalk,mpics,tcreek,tpark,tmound,tpics | mai] [-r20Vehicles ids] -logFile <dir>\Player.log):
    //   mloop   the Kennesaw Mountain lap on every vehicle, driven by script along the race's line (its AI zones' speeds as caps, flat out between): the time,
    //           dead stops and what they met, every spell in the air (which jump, how long, how far)
    //   mjumps  each jump on the Drifter Twin and the Street Classic at its AI speed, 26 and 32 m/s: air time, distance, the landing (upright, speed kept)
    //   mai     the race twice on Hard, 3 AI each (Round 58's replay analysis): every AI finishes
    //   mwalk   park visitors on the mountain's trails in Free Roam, two at most in a race
    //   mpics   the start, the three biggest jumps mid-air, the summit leg, the overlook
    //   tcreek  B1 (forward Noonday Creek Trail): the creek leap clean, missed (slow off the riprap) and round it (down the bank, through the creek) against the lap
    //   tpark   B2 (reverse): the parkway leap the same way (round it: up the embankment beside the kicker, across the road, through the far hedge)
    //   tmound  B3: over the mound by Town Center Drive both ways at 13 and 22 m/s (Round 64's xmound): the speed kept
    //   tpics   B1, B2, B3 pictures
    public sealed partial class Round20Runner
    {
        const string R67File = "kennesaw_mountain.json";
        // the jumps' lips and headings (Data/tracks/kennesaw_mountain.json, Tools/mapgen/make_mountain_route.py)
        static readonly (string id, string name, float x, float z, float dx, float dz)[] R67Jumps =
        {
            ("J1", "the summit overlook", -851.53f, -5684.32f, 0.5185f, 0.8551f), ("J2", "the ridge drop", -807.4f, -5606.05f, 0.8659f, 0.5001f),
            ("J3", "the earthworks berm", -588.45f, -5510.23f, 0.9951f, 0.0985f), ("J4", "the switchback cut", -652.23f, -5379.72f, -0.3625f, 0.932f),
            ("J5", "the washout", -719.3f, -5289.51f, -0.9704f, 0.2414f), ("J6", "the last ledge", -817.99f, -5281.06f, -0.9863f, 0.1647f),
        };
        string R67JumpAt(Vector3 p) { foreach (var j in R67Jumps) if (Flat(p - new Vector3(j.x, 0f, j.z)).magnitude < 14f) return j.id; return null; }

        IEnumerator R67Loop()
        {
            report.AppendLine(); report.AppendLine("=== A. The Kennesaw Mountain lap on every vehicle, driven by script along the race's line (the AI zones' speeds as caps, up to 45 m/s between)");
            UseRoute(R67File, "forward");
            if (race.Route == null || race.Route.Name != "Kennesaw Mountain") { Check("mloop: the route loads", false, race.Route?.Name ?? "none"); yield break; }
            var line = race.Route.Path.ToList(); var prof = R62Profile(); int stopsAll = 0;
            report.AppendLine($"  lap {race.Route.LapLength:0} m, {race.Route.Checkpoints.Count} checkpoints ({string.Join(", ", race.Route.Checkpoints.Select(c => c.Label))}), {race.Route.Closures.Count} barrier(s), {race.RouteData.laps} laps");
            foreach (var id in VehicleIds().Where(i => race.RouteData.AllowsVehicle(i)))
            {
                yield return Vehicle(id); FenceChunk.RestoreAll(); yield return new WaitForSecondsRealtime(.2f);
                Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
                var run = new Run(); var hist = new Queue<(float t, float v)>(); var stops = new List<string>(); var airs = new List<string>(); float lastAt = -99f, t = 0f;
                var touch = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); touch.seen.Clear(); touch.on = true;
                float air = 0f; Vector3 airFrom = Vector3.zero; float airTop = 0f; var jumped = new HashSet<string>();
                Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                var drive = StartCoroutine(DriveLine(line, 45f, 900f, run, null, null, 5f, false, prof));
                while (!run.reached && run.end == "" && run.time < 899f)
                {
                    yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                    var p = car.Body.position; float v = Flat(car.Body.linearVelocity).magnitude;
                    if (car.GroundedWheels == 0) { if (air == 0f) { airFrom = p; airTop = p.y; } air += Time.fixedDeltaTime; airTop = Mathf.Max(airTop, p.y); }
                    else if (air > 0f)
                    {
                        string jid = R67JumpAt(airFrom); if (jid != null) jumped.Add(jid);
                        if (air > .3f) airs.Add($"{(jid ?? "-")} {air:0.0} s {Flat(p - airFrom).magnitude:0} m (+{airTop - airFrom.y:0.0} m), landed at {v:0} m/s, up {car.transform.up.y:0.00}");
                        air = 0f;
                    }
                    hist.Enqueue((t, v)); while (hist.Count > 0 && hist.Peek().t < t - .5f) hist.Dequeue();
                    float vMax = hist.Max(h => h.v);
                    if (touch.seen.Count > 40) touch.seen.RemoveRange(0, touch.seen.Count - 40);
                    if (car.GroundedWheels > 0 && vMax > 10f && v < vMax * .45f && t - lastAt > 3f)
                    {
                        lastAt = t; var last = touch.seen.Skip(Math.Max(0, touch.seen.Count - 6)).ToList();
                        stops.Add($"{vMax:0.0} to {v:0.0} m/s at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}), lap {race.Route.LapStationOf(p):0} m: {(last.Count > 0 ? "hit " + last[last.Count - 1] : car.WaterImmersion > .05f ? "water" : "nothing touched")}");
                    }
                }
                yield return drive; touch.on = false; Time.timeScale = 1f;
                stopsAll += stops.Count;
                report.AppendLine($"  {Nm(id),-16} {(run.reached ? $"the lap in {run.time:0.0} s" : $"NOT ROUND ({run.end}) at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}), lap {race.Route.LapStationOf(car.Body.position):0} m")}; slowest {run.minSpeed:0.0} m/s; " +
                                  $"{stops.Count} dead stop(s); jumps flown {string.Join(" ", R67Jumps.Select(j => jumped.Contains(j.id) ? j.id : "-"))}");
                report.AppendLine($"      air: {(airs.Count > 0 ? string.Join("; ", airs) : "none")}");
                foreach (var s in stops) report.AppendLine("      stop: " + s);
                Check($"mloop {Nm(id)}: round the lap, never stuck", run.reached, run.reached ? $"{run.time:0.0} s" : run.end);
            }
            Check("mloop: no dead stop on the ground anywhere (listed above)", stopsAll == 0, $"{stopsAll} found");
        }

        IEnumerator R67JumpRuns()
        {
            report.AppendLine(); report.AppendLine("=== A2. Each jump on the Drifter Twin and the Street Classic: run at the lip at the AI's speed, 26 and 32 m/s along the race's line");
            UseRoute(R67File, "forward"); var zones = race.AiZones;
            foreach (var vid in new[] { "drifter", "original" })
            {
                yield return Vehicle(vid);
                foreach (var j in R67Jumps)
                {
                    var lip = RaceRoute.Surface(j.x, j.z); float sL = race.Route.LapStationOf(lip);
                    float aiV = zones?.FirstOrDefault(z => z.s0 <= sL && sL <= z.s1 + 2f)?.speed ?? 20f;
                    foreach (float v in new[] { aiV, 26f, 32f })
                    {
                        var line = LapPart(sL - 70f, sL + 160f); Place(line[0], line[2] - line[0], v * .8f); yield return new WaitForFixedUpdate();
                        var run = new Run(); float air = 0f, best = 0f, bestD = 0f, top = 0f, landV = 0f, upL = 1f, vLip = 0f; Vector3 from = Vector3.zero; bool left = false, done = false;
                        var touch = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); touch.seen.Clear(); touch.on = true; Vector3 landAt = Vector3.zero;
                        Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                        var drive = StartCoroutine(DriveLine(line, v, 30f, run, null, null, 5f, false, new[] { 0f, v, 400f, v }));
                        while (!run.reached && run.end == "" && run.time < 29f && !done)
                        {
                            yield return new WaitForFixedUpdate();
                            var p = car.Body.position;
                            if (car.GroundedWheels == 0) { if (air == 0f) { from = p; top = p.y; vLip = Flat(car.Body.linearVelocity).magnitude; } air += Time.fixedDeltaTime; top = Mathf.Max(top, p.y); }
                            else if (air > 0f)
                            {
                                if (air > best && Flat(from - lip).magnitude < 16f) { best = air; bestD = Flat(p - from).magnitude; landV = Flat(car.Body.linearVelocity).magnitude; upL = car.transform.up.y; left = true; landAt = p; }
                                air = 0f; if (left) done = true;
                            }
                        }
                        // a second's run-out after the landing: kept going?
                        float after = 99f; for (float w = 0f; w < 1f && left; w += Time.fixedDeltaTime) { yield return new WaitForFixedUpdate(); after = Mathf.Min(after, Flat(car.Body.linearVelocity).magnitude); }
                        StopCoroutine(drive); car.enabled = true; Time.timeScale = 1f; touch.on = false;
                        bool clean = left && upL > .7f && after > Mathf.Min(landV, v) * .5f;
                        string hit = touch.seen.Count > 0 ? touch.seen[touch.seen.Count - 1] : "nothing";
                        report.AppendLine($"  {Nm(vid),-14} {j.id} {j.name,-20} at {v:0} m/s (off the lip at {vLip:0.0}): {(left ? $"{best:0.00} s in the air, {bestD:0} m, {top - lip.y:+0.0;-0.0} m over the lip, landed at {landV:0.0} m/s upright {upL:0.00}, slowest in the next second {after:0.0} m/s at ({landAt.x:0}, {landAt.z:0}), lap {race.Route.LapStationOf(landAt) - sL:+0;-0} m from the lip{(clean ? "" : "; last touched " + hit)}" : "NO FLIGHT off its lip (airborne before it)")}");
                        Check($"mjumps {Nm(vid)} {j.id} at {v:0} m/s: flies and lands clean", clean, left ? $"{best:0.0} s, {bestD:0} m" : "no flight");
                    }
                }
            }
        }

        IEnumerator R67Ai()
        {
            report.AppendLine(); report.AppendLine($"=== A3. The Kennesaw Mountain race twice on Hard, 3 AI (each race's replay, Round 58's analysis)");
            int prev = RoadDriver.Difficulty; RoadDriver.Difficulty = 2; var all = new List<R58Stats>();
            var sets = new[] { new[] { "drifter", "original", "moto" }, new[] { "atv", "scrambler", "mower" } };
            for (int k = 0; k < 2; k++)
            {
                RaceController.AiVehiclesOverride = sets[k].Where(id => VehicleProfile.IndexOf(id) >= 0).ToArray();
                yield return StartRoute(R67File, "forward", 3);
                var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
                Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                yield return RideAlong(ai, 9f * 60f);
                Time.timeScale = 1f;
                var rec = ReplayRecorder.Instance; if (rec && rec.Recording) rec.Stop("the check");
                var d = rec ? rec.Last : null; string key = $"Kennesaw Mountain #{k + 1}";
                if (d == null) { Check($"mai {key}: a replay was recorded", false); race.EndRace(); continue; }
                var stats = R58Analyse(d, key, ai.ToDictionary(e => e.Name, e => e));
                foreach (var st in stats)
                {
                    all.Add(st); var e = ai.FirstOrDefault(x => x.Name == st.ai);
                    report.AppendLine($"  {key,-22} {st.ai,-5} {st.vehicle,-16} {(st.finished ? RaceController.Fmt(st.total) : "NOT FINISHED")} laps {(e != null ? string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0"))) : "-")}, missed {(e != null ? e.MissedTotal : 0)}; down {st.down:0.0} s, stuck {st.stuck:0.0} s (longest {st.longestStuck:0.0}), wrong way {st.against:0.0} s; resets {st.recoveries} (down {st.downResets}, blocked {st.blockResets}, wrong way {st.wrongResets}), passes {st.passes}");
                }
                Check($"mai {key}: every AI finishes", stats.Count == 3 && stats.All(s => s.finished), string.Join(", ", stats.Select(s => $"{s.ai} {(s.finished ? RaceController.Fmt(s.total) : "no")}")));
                race.EndRace(); RaceController.DirectionOverride = null;
            }
            RaceController.AiVehiclesOverride = null; RoadDriver.Difficulty = prev;
            if (all.Count > 0) report.AppendLine($"  ALL: {all.Count} AI races; finished {all.Count(s => s.finished)}; down {all.Sum(s => s.down):0.0} s, stuck {all.Sum(s => s.stuck):0.0} s (longest {all.Max(s => s.longestStuck):0.0}), resets {all.Sum(s => s.recoveries)}");
        }

        IEnumerator R67Walkers()
        {
            report.AppendLine(); report.AppendLine("=== A5. Park visitors on Kennesaw Mountain's trails");
            var life = AmbientLife.Current; if (!life) { Check("the life is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            int most = 0;
            foreach (var (x, z, yaw) in new[] { (-700f, -5553f, 90f), (-760f, -5281f, 270f), (-1110f, -5300f, 250f) })
                foreach (float hour in new[] { 9f, 17f })
                {
                    yield return Clock(hour); life.ClearWalkers(); respawn.PlaceAt(RaceRoute.Surface(x, z) + Vector3.up * .4f, yaw); race.NotifyTeleported();
                    yield return new WaitForSecondsRealtime(2f); yield return LookRound(RaceRoute.Surface(x, z)); yield return new WaitForSecondsRealtime(5f);
                    most = Mathf.Max(most, life.TrailWalkers);
                    report.AppendLine($"  {hour:00}:00 at ({x:0}, {z:0}): {life.WalkersInfo}");
                }
            Check("mwalk: visitors out on the mountain's trails", most > 0, $"most at once {most}");
            yield return Clock(9f); yield return StartRoute(R67File, "forward", 0);
            respawn.PlaceAt(RaceRoute.Surface(-760f, -5281f) + Vector3.up * .4f, 270f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f);
            yield return LookRound(RaceRoute.Surface(-760f, -5281f)); yield return new WaitForSecondsRealtime(5f);
            report.AppendLine($"  in a race: {life.WalkersInfo}");
            Check("mwalk: in a race, two groups at most on the trails", life.TrailWalkers <= 2, $"{life.TrailWalkers}");
            race.EndRace();
            life.ClearWalkers(); var at = RaceRoute.Surface(-700f, -5553f); respawn.PlaceAt(at + Vector3.up * .4f + Vector3.right * 30f, 270f); race.NotifyTeleported();
            yield return new WaitForSecondsRealtime(1.5f); int id = life.ForceWalker(AmbientLife.WalkKind.Jogger, at); yield return new WaitForSecondsRealtime(1.5f);
            var wp = life.WalkerPosition(id); if (id >= 0) yield return Still("67_mountain_visitor", wp + new Vector3(4f, 2.2f, 5f), wp + Vector3.up, 55f);
        }

        // a picture of a jump mid-air: the car run at the lip at 'v', frozen half way through its flight, the camera beside the flight
        IEnumerator R67MidAir(string name, (string id, string name, float x, float z, float dx, float dz) j, float v, string vid)
        {
            UseRoute(R67File, "forward"); yield return Vehicle(vid);
            var lip = RaceRoute.Surface(j.x, j.z); float sL = race.Route.LapStationOf(lip);
            var line = LapPart(sL - 70f, sL + 160f); Place(line[0], line[2] - line[0], v * .8f); yield return new WaitForFixedUpdate();
            var run = new Run(); float air = 0f; bool shot = false;
            var drive = StartCoroutine(DriveLine(line, v, 30f, run, null, null, 5f, false, new[] { 0f, v, 400f, v }));
            while (!run.reached && run.end == "" && run.time < 29f && !shot)
            {
                yield return new WaitForFixedUpdate();
                if (car.GroundedWheels == 0) air += Time.fixedDeltaTime; else air = 0f;
                if (air > .7f && Flat(car.Body.position - lip).magnitude > 12f)
                {
                    Time.timeScale = 0f; var p = car.Body.position; var d = new Vector3(j.dx, 0f, j.dz); var side = Vector3.Cross(Vector3.up, d).normalized;
                    var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false;
                    float f0 = cam.fieldOfView; cam.fieldOfView = 60f; cam.transform.position = p - d * 16f + side * 4f + Vector3.up * 6f; cam.transform.LookAt(p + d * 6f - Vector3.up * 2f);
                    yield return null; yield return Capture(name); cam.fieldOfView = f0; if (chase) { chase.enabled = was; chase.Snap(); }
                    Time.timeScale = 1f; shot = true;
                }
            }
            StopCoroutine(drive); car.enabled = true; Time.timeScale = 1f;
            report.AppendLine($"  {name}: {(shot ? "taken" : "NOT TAKEN (no flight)")}");
        }

        IEnumerator R67Pics()
        {
            report.AppendLine(); report.AppendLine("=== A pictures");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(15f);
            Vector3 G(float x, float z, float h) => RaceRoute.Surface(x, z) + Vector3.up * h;
            UseRoute(R67File, "forward"); race.StartRace(); yield return new WaitForSecondsRealtime(1.5f);
            var gp = race.Route.GridPosition; yield return Still("67_start", gp + new Vector3(-25f, 9f, -18f), gp + new Vector3(30f, 0f, 5f), 60f);
            race.EndRace();
            foreach (var (jid, v) in new[] { ("J2", 30f), ("J5", 30f), ("J1", 28f), ("J3", 28f) })
                yield return R67MidAir($"67_midair_{jid}", R67Jumps.First(q => q.id == jid), v, "drifter");
            yield return Vehicle("original");
            respawn.PlaceAt(G(-875f, -5715f, .4f), 60f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2.5f);
            yield return Still("67_summit_lot_overlook", G(-905f, -5730f, 4f), G(-850f, -5680f, 0f), 60f);
            yield return Still("67_summit_leg_down", G(-862f, -5703f, 3f), G(-830f, -5625f, -6f), 65f);
            yield return Still("67_ridge_drop_from_below", G(-770f, -5585f, 3f), G(-807f, -5606f, 1f), 60f);
            yield return Still("67_switchback_cut", G(-640f, -5410f, 4f), G(-662f, -5335f, -4f), 60f);
            yield return Still("67_washout", G(-700f, -5294f, 3f), G(-735f, -5286f, -1f), 60f);
            yield return Still("67_summit_road", G(-1150f, -6060f, 4f), G(-1243f, -5993f, 1f), 60f);
        }

        // ------------------------------------------------------------ B1, B2: the creek leap and the parkway leap against the lap
        IEnumerator R67Shortcut(string dir, string scId, string label, float missSpeed, Vector2[] round, Vector2 lipAt)
        {
            report.AppendLine(); report.AppendLine($"=== {label}: clean, missed ({missSpeed:0} m/s off the lip), round the stunt, against the lap between the same two points (Drifter Twin)");
            yield return Vehicle("drifter"); UseRoute(R62File, dir);
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == scId); if (sc == null) { Check($"{scId}: the shortcut is in the route", false); yield break; }
            var path = sc.Path.ToList(); float sIn = race.Route.LapStationOf(path[0]), sOut = race.Route.LapStationOf(path[path.Count - 1]);
            var before = LapPart(sIn - 60f, sIn); var after = LapPart(sOut, sOut + 60f); var lap = LapPart(sIn - 60f, sOut + 60f);
            float off = LineLen(before);
            float[] Prof(float[] p) { if (p == null) return null; var o = new float[p.Length]; for (int i = 0; i < p.Length; i += 2) { o[i] = p[i] + off; o[i + 1] = p[i + 1]; } return o; }
            var clean = before.Concat(path).Concat(after).ToList();
            // the lip: the path's point nearest the jump's lip
            int kLip = 0; float bestD = 1e9f; for (int i = 0; i < path.Count; i++) { float dd = (new Vector2(path[i].x, path[i].z) - lipAt).sqrMagnitude; if (dd < bestD) { bestD = dd; kLip = i; } }
            float lipS = LineLen(before.Concat(path.Take(kLip + 1)).ToList());
            var slow = new List<float>(Prof(sc.AiProfile) ?? new float[0]); slow.AddRange(new[] { lipS - 30f, missSpeed, lipS + 4f, missSpeed });
            var rl = round.Select(q => RaceRoute.Surface(q.x, q.y)).ToList();
            var roundLine = before.Concat(rl).Concat(after).ToList();
            float tLap = -1f, tClean = -1f, tMiss = -1f, tRound = -1f; Run rc = null, rm = null, rr = null;
            bedsOn = true; var beds = StartCoroutine(R67BedDrag());   // (the scripted car is a disabled ArcadeVehicle: the brush, which acts on ArcadeVehicle.Live, never sees it)
            yield return TimedDrive(lap, R62Profile() is float[] zp ? ZonesOn(zp, sIn - 60f) : null, 200f, r => { tLap = r.reached ? r.time : -1f; });
            yield return TimedDrive(clean, Prof(sc.AiProfile), 200f, r => { rc = r; tClean = r.reached ? r.time : -1f; });
            yield return TimedDrive(clean, slow.ToArray(), 200f, r => { rm = r; tMiss = r.reached ? r.time : -1f; });
            yield return TimedDrive(roundLine, new[] { 0f, 22f, 999f, 22f }, 200f, r => { rr = r; tRound = r.reached ? r.time : -1f; });
            bedsOn = false; StopCoroutine(beds);
            report.AppendLine($"  the lap {LineLen(lap):0} m in {Tm(tLap)}; the shortcut clean {LineLen(clean):0} m in {Tm(tClean)} (most air {rc.maxAir:0.0} s, landed {(float.IsNaN(rc.firstLanding.x) ? "-" : $"at ({rc.firstLanding.x:0}, {rc.firstLanding.z:0}) upright {rc.upAtLanding:0.00}")}); " +
                              $"missed {Tm(tMiss)} (most air {rm.maxAir:0.0} s, landed {(float.IsNaN(rm.firstLanding.x) ? "-" : $"at ({rm.firstLanding.x:0}, {rm.firstLanding.z:0})")}); round the stunt {LineLen(roundLine):0} m in {Tm(tRound)}");
            foreach (var (nm, r2) in new[] { ("clean", rc), ("missed", rm), ("round", rr) })
                if (!r2.reached && r2.trail.Count > 0) { var e = r2.trail[r2.trail.Count - 1]; report.AppendLine($"      {nm} stopped at ({e.x:0.0}, {e.y:0.0}, {e.z:0.0}) ({r2.end})"); }
            Check($"{scId}: clean saves time", tClean > 0f && tLap > 0f && tClean < tLap, $"{Tm(tClean)} against {Tm(tLap)}");
            Check($"{scId}: missed costs more than the lap", tMiss < 0f || tMiss > tLap, $"{Tm(tMiss)} against {Tm(tLap)}");
            Check($"{scId}: round the stunt is slower than the lap", tRound < 0f || tRound > tLap, $"{Tm(tRound)} against {Tm(tLap)}");
            Check($"{scId}: never stuck (missed and round get out)", rm.reached && rr.reached, $"missed {(rm.reached ? "out" : rm.end)}, round {(rr.reached ? "out" : rr.end)}");
        }

        // B3 flat out: the speed kept over the mound (forward: from the red bridge over Town Center Drive and up the face; reverse the other way)
        IEnumerator R67Mound()
        {
            report.AppendLine(); report.AppendLine("=== B3 (BUG-001: \"it wasn't flush and caused me to lose momentum\"): over the mound at 26 and 30 m/s, the speed kept (Drifter Twin, Street Classic)");
            foreach (var vid in new[] { "drifter", "original" })
            {
                yield return Vehicle(vid);
                foreach (var dir in new[] { "forward", "reverse" })
                {
                    UseRoute(R62File, dir); float s = race.Route.LapStationOf(RaceRoute.Surface(172.5f, -910.5f));
                    foreach (float v in new[] { 26f, 30f })
                    {
                        var line = LapPart(s - 60f, s + 50f); Place(line[0], line[2] - line[0], v); yield return new WaitForFixedUpdate();
                        var run = new Run(); float vIn = -1f, vMin = 99f, air = 0f, most = 0f; float t = 0f;
                        Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                        var drive = StartCoroutine(DriveLine(line, v, 14f, run, null, null, 5f, false, new[] { 0f, v, 400f, v }));
                        while (!run.reached && run.end == "" && run.time < 13.5f)
                        {
                            yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; var p = car.Body.position; float sp = Flat(car.Body.linearVelocity).magnitude;
                            float d = Flat(p - new Vector3(167f, 0f, -899.6f)).magnitude;   // Dan's spot, the face's foot
                            if (vIn < 0f && d < 30f) vIn = sp;
                            if (d < 22f) vMin = Mathf.Min(vMin, sp);
                            if (car.GroundedWheels == 0) { air += Time.fixedDeltaTime; most = Mathf.Max(most, air); } else air = 0f;
                        }
                        yield return drive; Time.timeScale = 1f;
                        report.AppendLine($"  {Nm(vid),-14} {dir,-8} at {v:0} m/s: {vIn:0.0} m/s 30 m out, slowest within 22 m of the face's foot {vMin:0.0} m/s ({(vIn > 0f ? (1f - vMin / vIn) * 100f : 0f):0} % lost), most air {most:0.00} s, {(run.reached ? "through" : "NOT THROUGH " + run.end)}");
                        Check($"tmound {Nm(vid)} {dir} {v:0} m/s: no more than 15 % of the speed lost at the mound", run.reached && vIn > 0f && vMin > vIn * .85f, $"{vIn:0.0} -> {vMin:0.0} m/s");
                    }
                }
            }
        }

        // ShrubBeds' drag (ShrubBeds.FixedUpdate) on the scripted car while it drives with its ArcadeVehicle disabled
        bool bedsOn;
        IEnumerator R67BedDrag()
        {
            var all = FindObjectsByType<ShrubBeds>(FindObjectsSortMode.None);
            while (bedsOn)
            {
                yield return new WaitForFixedUpdate();
                if (car.enabled || car.GroundedWheels < 2 || car.Body.isKinematic) continue;
                float amount = 0f; foreach (var b in all) amount = Mathf.Max(amount, b.Coverage(car.Body.position));
                if (amount <= 0f || !Physics.Raycast(car.Body.position, Vector3.down, out var hit, car.suspensionLength + 0.5f, 1, QueryTriggerInteraction.Ignore) || !(hit.collider is TerrainCollider)) continue;
                var horizontal = Vector3.ProjectOnPlane(car.Body.linearVelocity, Vector3.up);
                car.Body.AddForce(-horizontal * Mathf.Min(ShrubBeds.Drag * amount, 0.8f / Time.fixedDeltaTime), ForceMode.Acceleration);
            }
        }

        IEnumerator R67Creek() => R67Shortcut("forward", "t_creek", "B1 the creek leap (forward)", 14f,
            new[] { new Vector2(1739f, -695f), new Vector2(1724f, -686f), new Vector2(1710f, -681f), new Vector2(1694f, -673f), new Vector2(1678f, -667f), new Vector2(1660f, -660f), new Vector2(1640f, -653f), new Vector2(1626f, -657.5f), new Vector2(1613f, -664f) }, new Vector2(1717f, -671.5f));
        IEnumerator R67Parkway() => R67Shortcut("reverse", "t_parkway_rev", "B2 the parkway leap (reverse)", 15f,
            new[] { new Vector2(1300f, -746.5f), new Vector2(1318f, -748f), new Vector2(1334f, -751f), new Vector2(1346f, -754f), new Vector2(1362f, -759f), new Vector2(1374f, -766f), new Vector2(1388f, -775f), new Vector2(1402f, -779f) }, new Vector2(1341f, -759.5f));

        IEnumerator R67Pics2()
        {
            report.AppendLine(); report.AppendLine("=== B pictures");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Clock(15f); yield return Vehicle("original");
            Vector3 G(float x, float z, float h) => RaceRoute.Surface(x, z) + Vector3.up * h;
            respawn.PlaceAt(G(1745f, -720f, .4f), 0f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2.5f);
            yield return Still("67_bug002_creek_leap_from_road", G(1736.6f, -671f, 2.2f), G(1690f, -668f, 0f), 60f);
            yield return Still("67_creek_leap_riprap", G(1735f, -690f, 4f), G(1717f, -671.5f, 0f), 60f);
            yield return Still("67_creek_leap_landing", G(1690f, -640f, 6f), G(1650f, -658f, 0f), 60f);
            respawn.PlaceAt(G(1270f, -750f, .4f), 90f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2.5f);
            yield return Still("67_bug003_parkway_leap", G(1318.1f, -749.8f, 2.2f), G(1341f, -759.5f, 1f), 60f);
            yield return Still("67_parkway_leap_far_side", G(1405f, -785f, 4f), G(1350f, -762f, 2f), 60f);
            respawn.PlaceAt(G(150f, -880f, .4f), 143f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2.5f);
            yield return Still("67_bug001_mound_face", G(160f, -889f, 2.2f), G(176f, -914f, 1.5f), 60f);
            yield return Still("67_mound_side", G(190f, -895f, 3f), G(170f, -906f, 0f), 60f);
            yield return R67Flight("67_midair_creek_leap", "forward", "t_creek", "drifter");
            yield return R67Flight("67_midair_parkway_leap", "reverse", "t_parkway_rev", "drifter");
        }
        // a shortcut's jump mid-air (its AI profile)
        IEnumerator R67Flight(string name, string dir, string scId, string vid)
        {
            yield return Vehicle(vid); UseRoute(R62File, dir);
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == scId); if (sc == null) yield break;
            float sIn = race.Route.LapStationOf(sc.Path[0]); var before = LapPart(sIn - 60f, sIn); float off = LineLen(before);
            var line = before.Concat(sc.Path).ToList(); var prof = sc.AiProfile?.Select((v, i) => i % 2 == 0 ? v + off : v).ToArray();
            Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
            var run = new Run(); float air = 0f; bool shot = false;
            var drive = StartCoroutine(DriveLine(line, 45f, 40f, run, null, null, 5f, false, prof));
            while (!run.reached && run.end == "" && run.time < 39f && !shot)
            {
                yield return new WaitForFixedUpdate();
                if (car.GroundedWheels == 0) air += Time.fixedDeltaTime; else air = 0f;
                if (air > .8f)
                {
                    Time.timeScale = 0f; var p = car.Body.position; var d = Flat(car.Body.linearVelocity).normalized; var side = Vector3.Cross(Vector3.up, d).normalized;
                    var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); bool was = chase && chase.enabled; if (chase) chase.enabled = false;
                    float f0 = cam.fieldOfView; cam.fieldOfView = 60f; cam.transform.position = p - d * 12f + side * 14f + Vector3.up * 2f; cam.transform.LookAt(p);
                    yield return null; yield return Capture(name); cam.fieldOfView = f0; if (chase) { chase.enabled = was; chase.Snap(); }
                    Time.timeScale = 1f; shot = true;
                }
            }
            StopCoroutine(drive); car.enabled = true; Time.timeScale = 1f;
            report.AppendLine($"  {name}: {(shot ? "taken" : "NOT TAKEN (no flight)")}");
        }
    }
}
