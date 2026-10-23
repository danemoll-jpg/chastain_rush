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
    // Round 62 in the exe (-exeRound62 <dir> [-r62Only tloop,tai,tbar,tpics] [-r20Vehicles ids] [-traffic Normal] -logFile <dir>\Player.log):
    //   tloop   the Noonday Creek Trail's lap both ways on every race vehicle, driven by script along the race's line (its AI zones' speeds as caps):
    //           the lap's time, every dead stop on the ground (from over 10 m/s to under 45 % within 0.5 s) and what it met, stuck (wedged) or not,
    //           every spell in the air over 0.3 s and where; in the mall: forward, whether the raffle stage's leap cleared the escalator's well
    //           (the car still on the upper floor 10 m past it) and the balcony's drop; reverse, the escalator's launch through Halverson's front
    //   tai     the race twice each way on Hard, 3 AI, traffic as given (-traffic Normal), the player riding along over the AI; from each race's
    //           replay (Round 58's analysis): finished, down, stuck, wrong way, resets; every AI must finish
    //   tbar    every race barrier holds: in a race (no AI), a car driven at each barrier from 25 m on the course's side at 16 m/s, along the closed
    //           road: it never gets past
    //   tpics   pictures: the start on Bells Ferry Road, Bells Ferry and Barrett Parkway, the lot and Halverson's doors, the mattress mountain, the
    //           raffle stage and the escalator's well, the balcony and the entrance hall, the main doors, the north lot, the red bridge and the
    //           I-575 underpass from about where Dan's photos were taken, the trail in the woods, the trailhead; Dan's two bump spots
    public sealed partial class Round20Runner
    {
        const string R62File = "noonday_creek_trail.json";

        // the race's AI zones as DriveLine's speed profile (station, speed pairs; free between them)
        float[] R62Profile()
        {
            var zones = race.AiZones; if (zones == null) return null;
            var p = new List<float>();
            foreach (var z in zones.OrderBy(z => z.s0)) { p.Add(z.s0 - .5f); p.Add(99f); p.Add(z.s0); p.Add(z.speed); p.Add(z.s1); p.Add(z.speed); p.Add(z.s1 + .5f); p.Add(99f); }
            return p.ToArray();
        }
        float MallU(Vector3 p) { var m = MallNow; return m ? Vector2.Dot(new Vector2(p.x, p.z), m.frameU) : 0f; }
        float MallV(Vector3 p) { var m = MallNow; return m ? Vector2.Dot(new Vector2(p.x, p.z), m.frameV) : 0f; }
        bool InMall(Vector3 p) { var m = MallNow; if (!m) return false; float u = MallU(p), v = MallV(p); return u > -80f && u < 175f && v > -1320f && v < -1240f; }

        IEnumerator R62Loop()
        {
            report.AppendLine(); report.AppendLine("=== The Noonday Creek Trail, both ways, every race vehicle, driven by script along the race's line (the AI zones' speeds as caps, flat out elsewhere)");
            var m = MallNow; if (!m) { Check("Town Center is built", false); yield break; }
            int stopsAll = 0, notThrough = 0;
            foreach (var dir in new[] { "forward", "reverse" })
            {
                UseRoute(R62File, dir);
                if (race.Route == null || race.Route.Name != "Noonday Creek Trail") { Check($"tloop {dir}: the route loads", false, race.Route?.Name ?? "none"); continue; }
                var ids = VehicleIds().Where(id => race.RouteData.AllowsVehicle(id)).ToArray();
                var line = race.Route.Path.ToList(); var prof = R62Profile();
                report.AppendLine($"  {dir}: lap {race.Route.LapLength:0} m, {race.Route.Checkpoints.Count} checkpoints, {race.Route.Closures.Count} barriers; floors {m.lowerFloor:0.00} / {m.upperFloor:0.00} m");
                foreach (var id in ids)
                {
                    yield return Vehicle(id);
                    m.RestoreBreakables(); yield return new WaitForSecondsRealtime(.2f);
                    Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
                    var run = new Run(); var hist = new Queue<(float t, float v)>(); var stops = new List<string>(); var airs = new List<string>(); float lastAt = -99f, t = 0f;
                    var touch = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); touch.seen.Clear(); touch.on = true;
                    float air = 0f; Vector3 airFrom = Vector3.zero; float airTop = 0f; string well = "-", balcony = "-", front = "-";
                    Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                    var drive = StartCoroutine(DriveLine(line, 45f, 900f, run, null, null, 5f, false, prof));
                    while (!run.reached && run.end == "" && run.time < 899f)
                    {
                        yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                        var p = car.Body.position; float v = Flat(car.Body.linearVelocity).magnitude;
                        if (car.GroundedWheels == 0) { if (air == 0f) { airFrom = p; airTop = p.y; } air += Time.fixedDeltaTime; airTop = Mathf.Max(airTop, p.y); }
                        else if (air > 0f) { if (air > .3f) airs.Add($"{air:0.0} s {Flat(p - airFrom).magnitude:0} m {airTop - airFrom.y:+0.0;-0.0} m up from ({airFrom.x:0}, {airFrom.z:0}){(InMall(airFrom) ? $" (the mall, u {MallU(airFrom):0})" : "")}"); air = 0f; }
                        if (InMall(p))
                        {
                            float u = MallU(p), vv = MallV(p);
                            if (dir == "forward" && well == "-" && u < -2f && u > -20f && vv < -1296f) well = p.y > (m.lowerFloor + m.upperFloor) * .5f ? "cleared (upper floor)" : "landed on the escalator (lower floor)";
                            if (dir == "forward" && balcony == "-" && vv > -1272f && vv < -1262f && Mathf.Abs(u + 51f) < 9f) balcony = $"through the hall at {p.y - m.lowerFloor:0.0} m over its floor, {v:0} m/s";
                            if (dir == "reverse" && front == "-" && u > 53.5f && u < 58f) front = $"through Halverson's front {p.y - m.upperFloor:0.0} m over the floor, {v:0} m/s";
                        }
                        hist.Enqueue((t, v)); while (hist.Count > 0 && hist.Peek().t < t - .5f) hist.Dequeue();
                        float vMax = hist.Max(h => h.v);
                        if (touch.seen.Count > 40) touch.seen.RemoveRange(0, touch.seen.Count - 40);
                        if (car.GroundedWheels > 0 && vMax > 10f && v < vMax * .45f && t - lastAt > 3f)
                        {
                            lastAt = t; var last = touch.seen.Skip(Math.Max(0, touch.seen.Count - 6)).ToList();
                            stops.Add($"{vMax:0.0} to {v:0.0} m/s at ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}), lap {race.Route.LapStationOf(p):0} m{(InMall(p) ? $" (the mall, u {MallU(p):0} v {MallV(p):0})" : "")}: {(last.Count > 0 ? "hit " + last[last.Count - 1] : car.WaterImmersion > .05f ? "water" : "nothing touched")}");
                        }
                    }
                    yield return drive; touch.on = false; Time.timeScale = 1f;
                    stopsAll += stops.Count; if (!run.reached) notThrough++;
                    report.AppendLine($"  {dir,-8} {Nm(id),-16} {(run.reached ? $"the lap in {run.time:0.0} s" : $"NOT THROUGH ({run.end}) at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}), lap {race.Route.LapStationOf(car.Body.position):0} m")}; slowest {run.minSpeed:0.0} m/s; {stops.Count} dead stop(s)" +
                                      (dir == "forward" ? $"; the well: {well}; the balcony: {balcony}" : $"; the escalator's launch: {front}"));
                    report.AppendLine($"      air: {(airs.Count > 0 ? string.Join("; ", airs) : "none")}");
                    foreach (var s in stops) report.AppendLine("      stop: " + s);
                    Check($"tloop {dir} {Nm(id)}: round the lap, never stuck", run.reached, run.reached ? $"{run.time:0.0} s" : run.end);
                }
            }
            Check("tloop: no dead stop on the ground anywhere (listed above)", stopsAll == 0, $"{stopsAll} found");
            m.RestoreBreakables();
        }

        IEnumerator R62Ai()
        {
            report.AppendLine(); report.AppendLine($"=== The Noonday Creek Trail raced: twice each way on Hard, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")} (each race's replay, Round 58's analysis)");
            int prev = RoadDriver.Difficulty; RoadDriver.Difficulty = 2; var all = new List<R58Stats>();
            var sets = new[] { new[] { "drifter", "original", "moto" }, new[] { "atv", "scrambler", "mower" } };
            foreach (var dir in new[] { "forward", "reverse" })
                for (int k = 0; k < 2; k++)
                {
                    RaceController.AiVehiclesOverride = sets[k].Where(id => VehicleProfile.IndexOf(id) >= 0).ToArray();
                    yield return StartRoute(R62File, dir, 3);
                    var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
                    Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                    yield return RideAlong(ai, 11f * 60f);
                    Time.timeScale = 1f;
                    var rec = ReplayRecorder.Instance; if (rec && rec.Recording) rec.Stop("the check");
                    var d = rec ? rec.Last : null; string key = $"Noonday Creek Trail|{dir} #{k + 1}";
                    if (d == null) { Check($"tai {key}: a replay was recorded", false); race.EndRace(); continue; }
                    var stats = R58Analyse(d, key, ai.ToDictionary(e => e.Name, e => e));
                    foreach (var st in stats)
                    {
                        all.Add(st); var e = ai.FirstOrDefault(x => x.Name == st.ai);
                        report.AppendLine($"  {key,-30} {st.ai,-5} {st.vehicle,-16} {(st.finished ? RaceController.Fmt(st.total) : "NOT FINISHED")} laps {(e != null ? string.Join(" / ", e.LapTimes.Select(x => x.ToString("0.0"))) : "-")}, missed {(e != null ? e.MissedTotal : 0)}; down {st.down:0.0} s, stuck {st.stuck:0.0} s (longest {st.longestStuck:0.0}), wrong way {st.against:0.0} s, reversing {st.reversing:0.0} s; resets {st.recoveries} (down {st.downResets}, blocked {st.blockResets}, wrong way {st.wrongResets}), passes {st.passes}");
                    }
                    Check($"tai {key}: every AI finishes", stats.Count == 3 && stats.All(s => s.finished), string.Join(", ", stats.Select(s => $"{s.ai} {(s.finished ? RaceController.Fmt(s.total) : "no")}")));
                    race.EndRace(); RaceController.DirectionOverride = null;
                }
            RaceController.AiVehiclesOverride = null; RoadDriver.Difficulty = prev;
            if (all.Count > 0) report.AppendLine($"  ALL: {all.Count} AI races; finished {all.Count(s => s.finished)}; down {all.Sum(s => s.down):0.0} s, stuck {all.Sum(s => s.stuck):0.0} s (longest {all.Max(s => s.longestStuck):0.0}), wrong way {all.Sum(s => s.against):0.0} s, resets {all.Sum(s => s.recoveries)}");
        }

        IEnumerator R62Barriers()
        {
            report.AppendLine(); report.AppendLine("=== Every race barrier holds: a car driven at each from 25 m on the course's side at 16 m/s along the closed road (Street Classic)");
            yield return Vehicle("original");
            foreach (var dir in new[] { "forward", "reverse" })
            {
                yield return StartRoute(R62File, dir, 0);
                var bars = race.Barriers.Built.ToList(); int held = 0; var through = new List<string>();
                foreach (var b in bars)
                {
                    var along = Flat(b.Along).normalized; var c = b.Centre; var start = RaceRoute.Surface(c.x - along.x * 25f, c.z - along.z * 25f);
                    Place(start, along, 16f); yield return new WaitForFixedUpdate();
                    var run = new Run(); var lineB = new List<Vector3> { start, RaceRoute.Surface(c.x + along.x * 30f, c.z + along.z * 30f) };
                    yield return DriveLine(lineB, 16f, 4.5f, run, null, null, 5f, false);
                    float past = Vector3.Dot(Flat(car.Body.position - c), along);
                    if (past < 1.5f) held++; else through.Add($"{b.Data.label} ({c.x:0}, {c.z:0}): {past:0.0} m past");
                }
                Check($"tbar {dir}: every barrier holds ({bars.Count})", through.Count == 0 && bars.Count > 0, through.Count == 0 ? $"{held} of {bars.Count}" : string.Join("; ", through));
                race.EndRace();
            }
        }

        IEnumerator R62Pics()
        {
            report.AppendLine(); report.AppendLine("=== Pictures");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var m = MallNow;
            Vector3 G(float x, float z, float h) => RaceRoute.Surface(x, z) + Vector3.up * h;
            IEnumerator Park(float x, float z, float yaw) { respawn.PlaceAt(G(x, z, .4f), yaw); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2.5f); }
            UseRoute(R62File, "forward"); race.StartRace(); yield return new WaitForSecondsRealtime(1.5f);
            yield return Still("62_start_bells_ferry", G(1745f, -700f, 7f), G(1733f, -800f, 2f), 60f);
            race.EndRace();
            yield return Park(1712f, -1220f, 180f); yield return Still("62_bells_ferry_road", G(1716f, -1150f, 4f), G(1712f, -1330f, 1f), 60f);
            yield return Park(1300f, -1676f, 270f); yield return Still("62_barrett_parkway", G(1380f, -1670f, 4f), G(1200f, -1680f, 1f), 60f);
            if (m)
            {
                yield return Park(m.P(200f, -1303.5f, 0f).x, m.P(200f, -1303.5f, 0f).z, 255f);
                yield return Still("62_halversons_east_doors", G(m.P(215f, -1306f, 0f).x, m.P(215f, -1306f, 0f).z, 4f), m.P(163.5f, -1303.5f, m.upperFloor + 2f), 60f);
                yield return Still("62_mattress_mountain", m.P(132f, -1306f, m.upperFloor + 2.2f), m.P(110.8f, -1303.5f, m.upperFloor + .8f), 60f);
                yield return Still("62_raffle_stage_forward", m.P(60f, -1305f, m.upperFloor + 2.4f), m.P(40f, -1303.5f, m.upperFloor + 1f), 65f);
                yield return Still("62_escalator_well", m.P(52f, -1311f, m.upperFloor + 4f), m.P(10f, -1303.5f, m.lowerFloor), 70f);
                yield return Still("62_escalator_reverse", m.P(-6f, -1305f, m.lowerFloor + 2.2f), m.P(46f, -1303.5f, m.upperFloor + 1.5f), 65f);
                yield return Still("62_balcony", m.P(-40f, -1300f, m.upperFloor + 2.2f), m.P(-51f, -1270f, m.lowerFloor + 2f), 70f);
                yield return Still("62_entrance_hall", m.P(-51f, -1292f, m.lowerFloor + 2f), m.P(-51f, -1267f, m.lowerFloor + 4f), 75f);
                yield return Park(m.P(-55f, -1225f, 0f).x, m.P(-55f, -1225f, 0f).z, 0f);
                yield return Still("62_main_doors_outside", G(m.P(-48f, -1225f, 0f).x, m.P(-48f, -1225f, 0f).z, 3f), m.P(-51f, -1267f, m.lowerFloor + 5f), 70f);
            }
            yield return Park(200f, -960f, 330f); yield return Still("62_north_lot_to_the_trail", G(222f, -1040f, 5f), G(160f, -890f, 1f), 60f);
            yield return Park(150f, -870f, 345f); yield return Still("62_red_bridge", G(162f, -885f, 2.4f), G(138f, -805f, 2f), 60f);
            yield return Park(580f, -478f, 70f); yield return Still("62_i575_underpass", G(585f, -479f, 2.0f), G(650f, -462f, 2.5f), 70f);
            yield return Park(1000f, -560f, 90f); yield return Still("62_trail_woods", G(960f, -555f, 2.0f), G(1040f, -575f, 1.5f), 60f);
            yield return Park(1612f, -660f, 80f); yield return Still("62_trailhead", G(1580f, -680f, 4f), G(1690f, -600f, 2f), 60f);
            yield return Park(640f, 9f, 272f); yield return Still("62_bug001_big_shanty", G(640f, 10f, 2.4f), G(520f, -12f, 2f), 60f);
            yield return Park(512f, 30f, 165f); yield return Still("62_bug002_i575", G(511f, 32f, 2.4f), G(518f, -20f, 2f), 60f);
        }
    }
}
