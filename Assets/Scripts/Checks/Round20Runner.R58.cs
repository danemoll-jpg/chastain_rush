using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 58 in the exe (-exeRound58 <dir> [-r58Only aitable,airecover,hubpit,plants,ridgebump,cpics58] [-r58Races glen,creek,...] [-r58AgencyOff]
    // -traffic Normal -logFile <dir>\Player.log):
    //   aitable    A6: one race on every course and direction (Hard, 3 AI, fixed vehicles per course), the player riding along 40 m over one AI
    //              after another (traffic is placed round the player); from the race's replay (ReplayRecorder), per AI: time down, stuck,
    //              the wrong way, reversing, behind a car; its best flying lap against Dan's best (top-ten-v1.json, read only). With
    //              -r58AgencyOff: the same as before Round 58 (the "before" column). Writes r58_ai_<before|after>.csv
    //   airecover  A1 to A5 by hand in a race: an AI laid on its side, a block put in front of one, one turned round, one rolling backwards:
    //              how long until it is going again
    //   hubpit     B: the Underground race's hub: flat out at every edge of the floor near the pit, on the race's line and off it (four vehicles)
    //   plants     C: M5c's glass: every aquarium pipe's plants outside the tube (counts from the build log) and a picture at Dan's spot
    //   ridgebump  D: Chastain Ridge Drive at Dan's spot, flat out both ways: the biggest step in vertical speed
    //   cpics58    pictures at Dan's spots
    public sealed partial class Round20Runner
    {
        static readonly (string file, string dir, string key, string[] cars)[] R58Races =
        {
            ("chastain_glen_loop.json", "forward", "Chastain Glen loop|forward", new[] { "drifter", "original", "moto" }),
            ("chastain_glen_loop.json", "reverse", "Chastain Glen loop|reverse", new[] { "drifter", "original", "moto" }),
            ("noonday_creek.json", "forward", "Noonday Creek|forward", new[] { "drifter", "moto", "atv" }),
            ("noonday_creek.json", "reverse", "Noonday Creek|reverse", new[] { "drifter", "moto", "atv" }),
            ("underground.json", "forward", "Underground|forward", new[] { "drifter", "original", "atv" }),
            ("chastain_ridge.json", "forward", "Chastain Ridge|forward", new[] { "drifter", "original", "moto" }),
            ("chastain_ridge.json", "reverse", "Chastain Ridge|reverse", new[] { "drifter", "original", "moto" }),
            ("chastain_lakes.json", "forward", "Chastain Lakes|forward", new[] { "drifter", "original", "moto" }),
            ("chastain_lakes.json", "reverse", "Chastain Lakes|reverse", new[] { "drifter", "original", "moto" }),
            ("addison_heights.json", "forward", "Addison Heights (Embry Lane line)|forward", new[] { "drifter", "original", "moto" }),
            ("addison_heights.json", "reverse", "Addison Heights (Embry Lane line)|reverse", new[] { "drifter", "original", "moto" }),
        };

        // Dan's best lap per course key (any vehicle), from his Top 10 file (read only)
        static Dictionary<string, (float sec, string vehicle)> DanBest()
        {
            var best = new Dictionary<string, (float, string)>();
            try
            {
                string path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "Chastain Rush", "Chastain Rush", "top-ten-v1.json");
                if (!File.Exists(path)) return best;
                var j = JsonUtility.FromJson<TopTenFile>(File.ReadAllText(path));
                foreach (var e in j.entries) if (!e.race && e.name == "Dan" && (!best.TryGetValue(e.category, out var b) || e.seconds < b.Item1)) best[e.category] = (e.seconds, e.vehicle);
            }
            catch (System.Exception ex) { Debug.LogWarning("[R58] Dan's records: " + ex.Message); }
            return best;
        }
        [System.Serializable] sealed class TopTenEntry { public string category = "", vehicle = "", name = ""; public bool race; public float seconds; }
        [System.Serializable] sealed class TopTenFile { public TopTenEntry[] entries = new TopTenEntry[0]; }

        public sealed class R58Stats
        {
            public string course, ai, vehicle; public bool finished; public float total, bestLap = float.NaN, down, stuck, against, reversing, behind, longestStuck, longestAgainst; public int behindSpells, recoveries, downResets, blockResets, wrongResets, backUps, passes;
        }

        // the time a test is true, counting only spells at least 'min' long; and the longest spell
        static void Spells(List<(float t, bool on)> s, float min, out float total, out float longest, out int count)
        {
            total = 0f; longest = 0f; count = 0; float start = -1f;
            for (int i = 0; i < s.Count; i++)
            {
                bool on = s[i].on && i < s.Count - 1;
                if (on && start < 0f) start = s[i].t;
                if (!on && start >= 0f) { float d = s[i].t - start; if (d >= min) { total += d; count++; } longest = Mathf.Max(longest, d); start = -1f; }
            }
        }

        List<R58Stats> R58Analyse(ReplayData d, string course, Dictionary<string, RaceEntrant> byName)
        {
            var road = race.AiRoad; var list = new List<R58Stats>();
            foreach (var r in d.racers)
            {
                if (r.player) continue;
                var f = r.track.frames; float tEnd = r.finishedAt > 0f ? Mathf.Min(r.finishedAt, d.t1) : d.t1;
                var down = new List<(float, bool)>(); var stuck = new List<(float, bool)>(); var against = new List<(float, bool)>(); var rev = new List<(float, bool)>(); var behind = new List<(float, bool)>();
                float tGo = r.laps.Count > 0 ? r.laps[0].start : 3f;
                foreach (var fr in f)
                {
                    if (fr.t < 0f || fr.t > tEnd) continue;
                    var up = fr.q * Vector3.up; var fw = fr.q * Vector3.forward; fw.y = 0f; fw.Normalize();
                    bool isDown = up.y < .5f && Mathf.Abs(fr.speed) < 8f && fr.wheels < 2;
                    down.Add((fr.t, isDown));
                    stuck.Add((fr.t, !isDown && fr.t > tGo + 2f && Mathf.Abs(fr.speed) < 1.5f));
                    float s = road.Project(fr.p, out float lat); road.At(s, out var tg); tg.y = 0f; tg.Normalize();
                    bool nearLap = Mathf.Abs(lat) < road.HalfWidth(s) + 12f;
                    against.Add((fr.t, nearLap && Vector3.Dot(fw, tg) < -.17f && fr.speed > .5f));
                    rev.Add((fr.t, fr.speed < -1.5f));
                    // behind a car: another racer or a traffic car 3 to 14 m ahead in its path, it going slow and no faster than that car
                    bool b = false;
                    if (fr.speed < 12f && fr.speed > -1f)
                    {
                        var right = Vector3.Cross(Vector3.up, fw);
                        bool Ahead(Vector3 p, float v) { var dl = p - fr.p; float al = Vector3.Dot(dl, fw), sd = Mathf.Abs(Vector3.Dot(dl, right)); return al > 3f && al < 14f && sd < 2.5f && Mathf.Abs(dl.y) < 4f && fr.speed < v + 1.5f; }
                        foreach (var o in d.racers) if (o != r) { var of = ReplayData.At(o.track, fr.t); if (Ahead(of.p, of.speed)) { b = true; break; } }
                        if (!b) foreach (var tr in d.traffic) { if (tr.frames.Count == 0 || fr.t < tr.frames[0].t || fr.t > tr.frames[tr.frames.Count - 1].t) continue; var of = ReplayData.At(tr, fr.t); if (Ahead(of.p, of.speed)) { b = true; break; } }
                    }
                    behind.Add((fr.t, b));
                }
                var st = new R58Stats { course = course, ai = r.name, vehicle = VehicleProfile.Find(r.vehicle).Name, finished = r.finishedAt > 0f, total = r.total };
                Spells(down, .3f, out st.down, out _, out _);
                Spells(stuck, 1f, out st.stuck, out st.longestStuck, out _);
                Spells(against, .5f, out st.against, out st.longestAgainst, out _);
                Spells(rev, 1.5f, out st.reversing, out _, out _);
                Spells(behind, 2f, out st.behind, out _, out st.behindSpells);
                var flying = r.laps.Where(l => l.lap > 1).Select(l => l.end - l.start).ToList();
                if (flying.Count > 0) st.bestLap = flying.Min();
                if (byName.TryGetValue(r.name, out var e) && e.Driver)
                {
                    var dr = e.Driver; st.recoveries = dr.RecoveryCount; st.downResets = dr.DownResets; st.blockResets = dr.BlockResets; st.wrongResets = dr.WrongWayResets; st.backUps = dr.BackUps; st.passes = dr.Passes;
                }
                list.Add(st);
            }
            return list;
        }

        // the player rides along 40 m over one AI after another (traffic is placed round the player), out of the race's way
        IEnumerator RideAlong(List<RaceEntrant> ai, float limit)
        {
            car.enabled = false; car.Body.isKinematic = true; var cols = car.GetComponentsInChildren<Collider>(); foreach (var c in cols) c.enabled = false;
            float end = Time.realtimeSinceStartup + limit; int k = 0; float switchAt = 0f; float game0 = Time.time;
            while (Time.realtimeSinceStartup < end && ai.Any(e => !e.Finished))
            {
                if (Time.time > switchAt) { k++; switchAt = Time.time + 15f; }
                var e = ai.Where(x => !x.Finished).ElementAt(k % ai.Count(x => !x.Finished));
                var p = e.Car.Body.position + Vector3.up * 40f;
                car.Body.position = p; car.transform.position = p; race.NotifyTeleported();
                yield return new WaitForSecondsRealtime(.2f);
            }
            foreach (var c in cols) c.enabled = true; car.Body.isKinematic = false; car.enabled = true;
        }

        IEnumerator R58Table()
        {
            string mode = RoadDriver.Agency ? "after" : "before";
            report.AppendLine(); report.AppendLine($"=== A6. every course on Hard, 3 AI, traffic {(Traffic.Instance ? Traffic.LevelNames[Traffic.Instance.Level] : "none")}: measured from each race's replay ({mode} Round 58)");
            var dan = DanBest(); var all = new List<R58Stats>();
            string sel = null; var argv = System.Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < argv.Length; i++) if (argv[i] == "-r58Races") sel = argv[i + 1];
            int prev = RoadDriver.Difficulty; RoadDriver.Difficulty = 2;
            var csv = new StringBuilder("course,ai,vehicle,finished,total,best_lap,dan_best,down_s,stuck_s,longest_stuck_s,against_s,longest_against_s,reversing_s,behind_s,behind_spells,recoveries,down_resets,block_resets,wrongway_resets,backups,passes\n");
            foreach (var (file, dir, key, cars) in R58Races)
            {
                if (sel != null && !sel.Split(',').Any(x => file.StartsWith(x.Split('|')[0]) && (!x.Contains("|") || x.Split('|')[1] == dir))) continue;   // e.g. glen, creek|reverse
                RaceController.AiVehiclesOverride = cars;
                yield return StartRoute(file, dir, 3);
                var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
                Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                yield return RideAlong(ai, 6.5f * 60f);
                Time.timeScale = 1f;
                var rec = ReplayRecorder.Instance; if (rec && rec.Recording) rec.Stop("the check");
                var d = rec ? rec.Last : null;
                if (d == null) { Check($"A6 {key}: a replay was recorded", false); race.EndRace(); continue; }
                var stats = R58Analyse(d, key, ai.ToDictionary(e => e.Name, e => e));
                dan.TryGetValue(key, out var db);
                foreach (var st in stats)
                {
                    all.Add(st);
                    report.AppendLine($"  {key,-44} {st.ai,-5} {st.vehicle,-16} {(st.finished ? RaceController.Fmt(st.total) : "not finished"),-10} best lap {(float.IsNaN(st.bestLap) ? "-" : st.bestLap.ToString("0.0"))} (Dan {(db.Item1 > 0 ? db.Item1.ToString("0.0") : "-")})  down {st.down:0.0} s, stuck {st.stuck:0.0} s (longest {st.longestStuck:0.0}), wrong way {st.against:0.0} s (longest {st.longestAgainst:0.0}), reversing {st.reversing:0.0} s, behind a car {st.behind:0.0} s ({st.behindSpells} spells over 2 s); resets {st.recoveries} (down {st.downResets}, blocked {st.blockResets}, wrong way {st.wrongResets}), back-ups {st.backUps}, passes {st.passes}");
                    csv.AppendLine($"{key},{st.ai},{st.vehicle},{st.finished},{st.total:0.00},{st.bestLap:0.00},{db.Item1:0.00},{st.down:0.00},{st.stuck:0.00},{st.longestStuck:0.00},{st.against:0.00},{st.longestAgainst:0.00},{st.reversing:0.00},{st.behind:0.00},{st.behindSpells},{st.recoveries},{st.downResets},{st.blockResets},{st.wrongResets},{st.backUps},{st.passes}");
                }
                File.WriteAllText(Path.Combine(outputDir, $"r58_ai_{mode}.csv"), csv.ToString());
                Check($"A6 {key}: every AI finishes", stats.All(s => s.finished), string.Join(", ", stats.Select(s => $"{s.ai} {(s.finished ? RaceController.Fmt(s.total) : "no")}")));
                race.EndRace(); RaceController.DirectionOverride = null;
            }
            RaceController.AiVehiclesOverride = null; RoadDriver.Difficulty = prev;
            if (all.Count > 0)
                report.AppendLine($"  ALL ({mode}): {all.Count} AI races; down {all.Sum(s => s.down):0.0} s, stuck {all.Sum(s => s.stuck):0.0} s (longest {all.Max(s => s.longestStuck):0.0}), wrong way {all.Sum(s => s.against):0.0} s (longest {all.Max(s => s.longestAgainst):0.0}), reversing {all.Sum(s => s.reversing):0.0} s, behind a car {all.Sum(s => s.behind):0.0} s in {all.Sum(s => s.behindSpells)} spells, resets {all.Sum(s => s.recoveries)}, finished {all.Count(s => s.finished)} of {all.Count}");
        }

        // ------------------------------------------------------------ A1 to A5 by hand
        IEnumerator R58Recover()
        {
            report.AppendLine(); report.AppendLine($"=== A1-A5 by hand ({(RoadDriver.Agency ? "after" : "before")} Round 58): the Chastain Glen loop on Hard, 3 AI; each case put on one AI 25 s in");
            int prev = RoadDriver.Difficulty; RoadDriver.Difficulty = 2; RaceController.AiVehiclesOverride = new[] { "moto", "original", "drifter" };
            foreach (var kase in new[] { "down", "blocked", "turned", "rolling" })
            {
                yield return StartRoute("chastain_glen_loop.json", "forward", 3, 1);
                var ai = race.Entrants.Where(e => !e.IsPlayer).ToList();
                var park = race.Route.PointAt(race.Route.GateS - 40f, out var pf); Place(park + Vector3.Cross(Vector3.up, pf).normalized * 9f, pf, 0f); car.enabled = false;
                yield return new WaitForSeconds(25f);
                var e = ai.OrderBy(x => race.Score(x)).ElementAt(1); var c = e.Car; var dr = e.Driver; var road = race.AiRoad;
                float s0 = road.Project(c.Body.position, out _); var at = road.At(s0, out var tg); tg.y = 0f; tg.Normalize();
                GameObject block = null; int rc0 = dr.RecoveryCount;
                var p = c.Body.position;
                if (kase == "down") { c.Body.rotation = Quaternion.LookRotation(tg, Vector3.up) * Quaternion.Euler(0f, 0f, 85f); c.Body.position = p + Vector3.up * .6f; c.Body.linearVelocity = Vector3.zero; c.Body.angularVelocity = Vector3.zero; }
                else if (kase == "blocked")
                {
                    // a trunk-sized block 2.5 m ahead of it, where it is, and the car stopped against it
                    c.Body.linearVelocity = Vector3.zero; c.Body.angularVelocity = Vector3.zero; c.Body.rotation = Quaternion.LookRotation(tg, Vector3.up);
                    block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.name = "R58 block"; block.transform.localScale = new Vector3(1.4f, 3f, 1.4f);
                    block.transform.position = RaceRoute.Surface(p.x + tg.x * 3.2f, p.z + tg.z * 3.2f, p.y) + Vector3.up * 1.4f; block.transform.rotation = Quaternion.LookRotation(tg);
                }
                else if (kase == "turned") { c.Body.rotation = Quaternion.LookRotation(-tg, Vector3.up); c.Body.linearVelocity = -tg * 10f; c.Body.angularVelocity = Vector3.zero; }
                else { c.Body.rotation = Quaternion.LookRotation(tg, Vector3.up); c.Body.linearVelocity = -tg * 8f; c.Body.angularVelocity = Vector3.zero; }
                Physics.SyncTransforms(); e.NotifyTeleported();
                float t0 = Time.time, going = -1f, wrongT = 0f, revT = 0f; bool reset = false;
                while (Time.time - t0 < 20f)
                {
                    yield return new WaitForFixedUpdate();
                    if (dr.RecoveryCount != rc0) reset = true;
                    float s = road.Project(c.Body.position, out _); road.At(s, out var t2); t2.y = 0f; t2.Normalize();
                    var fw = c.transform.forward; fw.y = 0f; fw.Normalize();
                    if (Vector3.Dot(fw, t2) < -.17f && Mathf.Abs(c.ForwardSpeed) > .5f) wrongT += Time.fixedDeltaTime;
                    if (c.ForwardSpeed < -1.5f) revT += Time.fixedDeltaTime;
                    float moved = Mathf.Repeat(s - s0 + road.Length * .5f, road.Length) - road.Length * .5f;
                    if (Vector3.Dot(fw, t2) > .8f && c.ForwardSpeed > 8f && c.transform.up.y > .8f && (moved > 6f || reset)) { going = Time.time - t0; break; }
                }
                if (block) Destroy(block);
                report.AppendLine($"  {kase,-8} {e.Name} ({e.VehicleName}): {(going >= 0f ? $"going again at over 8 m/s along the lap after {going:0.0} s" : "not going again within 20 s")}; {(reset ? "put back on the line" : "drove out itself")} (back-ups {dr.BackUps}); the wrong way {wrongT:0.0} s, reversing {revT:0.0} s");
                if (RoadDriver.Agency)
                {
                    float limit = kase == "down" ? 1.5f : kase == "blocked" ? 6f : 5f;
                    Check($"A {kase}: going again within {limit:0.0} s", going >= 0f && going <= limit, $"{going:0.0} s");
                }
                race.EndRace(); car.enabled = true;
            }
            RaceController.AiVehiclesOverride = null; RoadDriver.Difficulty = prev;
        }

        // ------------------------------------------------------------ B: the hub's pit, every edge the race passes
        IEnumerator R58HubPit()
        {
            report.AppendLine(); report.AppendLine("=== B. the hub's pit (Dan's BUG-001 at x 1854.77 z 1132.96): flat out (34 m/s) into every railed edge the race passes, at 90, 45 and 20 degrees");
            yield return StartRoute("underground.json", "forward", 0);
            const float Y = 262f;
            float SpanY(float z) { return Physics.Raycast(new Vector3(1920.4f, 276.5f, z), Vector3.down, out var h, 20f, ~0, QueryTriggerInteraction.Ignore) ? h.point.y : Y; }
            // (name, the point aimed at, the outward normal: toward the pit)
            var targets = new List<(string, Vector3, Vector3)>
            {
                ("R1 west edge", new Vector3(1861f, Y, 1150f), Vector3.right), ("SG1 closed", new Vector3(1861f, Y, 1132.8f), Vector3.right),
                ("R2 south edge (west)", new Vector3(1874f, Y, 1126f), Vector3.forward), ("R3 south edge (east)", new Vector3(1912f, Y, 1126f), Vector3.forward),
                ("R3 south-east corner", new Vector3(1938.5f, Y, 1126.5f), new Vector3(-1f, 0f, 1f).normalized), ("R3 east edge", new Vector3(1939f, Y, 1140f), Vector3.left),
                ("R5 north edge (east of the span)", new Vector3(1932f, Y, 1164f), Vector3.back), ("R4 north edge (west of the span)", new Vector3(1890f, Y, 1164f), Vector3.back),
                ("R6 the span's west side", new Vector3(1915.92f, SpanY(1145f), 1145f), Vector3.left), ("R7 the span's east side", new Vector3(1924.92f, SpanY(1145f), 1145f), Vector3.right),
            };
            int held = 0, n = 0;
            foreach (var vid in new[] { "drifter", "original", "atv", "moto" })
            {
                yield return Vehicle(vid);
                foreach (var (name, at, nrm) in targets)
                    foreach (float ang in new[] { 90f, 45f, 20f })
                    {
                        var along = Vector3.Cross(Vector3.up, nrm);
                        var dir = (nrm * Mathf.Sin(ang * Mathf.Deg2Rad) + along * Mathf.Cos(ang * Mathf.Deg2Rad)).normalized;
                        var start = at - dir * 7f; start.y = at.y + 0.2f;
                        if (name.Contains("span")) start = new Vector3(start.x, SpanY(start.z) + 0.2f, start.z);
                        Place(start, dir, 34f); car.enabled = false;
                        bool through = false, fell = false; float t = 0f;
                        while (t < 2.5f)
                        {
                            yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                            car.Simulate(1f, 0f, 0f, Time.fixedDeltaTime);
                            var p = car.Body.position;
                            if (Vector3.Dot(Flat(p - at), nrm) > 0.8f) through = true;
                            if (p.y < at.y - 3f) fell = true;
                        }
                        car.enabled = true; n++; if (!through && !fell) held++;
                        if (through || fell) report.AppendLine($"  {Nm(vid),-16} into {name} at {ang:0} deg: {(through ? "THROUGH" : "")}{(fell ? " FELL" : "")}, ended at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})");
                    }
            }
            report.AppendLine($"  held {held} of {n} runs (4 vehicles x {targets.Count} places x 3 angles)");
            Check("B: no car through or over a rail into the pit, flat out", held == n, $"{held} of {n}");
            yield return Still("58_B_hub_south_east", new Vector3(1905f, Y + 4f, 1119f), new Vector3(1939f, Y, 1140f));
            yield return Still("58_B_hub_north_span", new Vector3(1945f, Y + 4f, 1168f), new Vector3(1912f, Y + 2f, 1150f));
            yield return Still("58_B_hub_from_dan", new Vector3(1854.8f, Y + 2.2f, 1133f), new Vector3(1880f, Y, 1133f));
            race.EndRace();
        }

        // ------------------------------------------------------------ C: plants and rocks out of every tube
        IEnumerator R58Plants()
        {
            report.AppendLine(); report.AppendLine("=== C. every aquarium's weeds, rocks and logs against every pipe (Dan's BUG-002 at x 1640.80 z 943.86 in M5c)");
            int meshes = 0, verts = 0, inside = 0, unreadable = 0; var where = new Dictionary<string, int>();
            foreach (var mf in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var nm = mf.gameObject.name; if (!(nm.Contains("clear stretch - weeds") || nm.Contains("clear stretch - rocks") || nm.Contains("clear stretch - sunken logs"))) continue;
                var m = mf.sharedMesh; if (!m) continue; meshes++;
                if (!m.isReadable) { unreadable++; continue; }
                var v = m.vertices; var tr = mf.transform;
                foreach (var q in v) { verts++; if (DrainPipe.InsideAny(tr.TransformPoint(q))) { inside++; where[nm] = (where.TryGetValue(nm, out var k) ? k : 0) + 1; } }
                yield return null;
            }
            report.AppendLine($"  {meshes} meshes ({unreadable} not readable), {verts} points; inside a pipe: {inside}" + (where.Count > 0 ? " - " + string.Join("; ", where.Select(w => $"{w.Key}: {w.Value}")) : ""));
            Check("C: no weed, rock or log point inside any pipe", meshes > 0 && unreadable < meshes && inside == 0, $"{inside} of {verts}");
            yield return StartRoute("underground.json", "forward", 0);
            yield return Vehicle("drifter");
            var spot = new Vector3(1640.8f, 0f, 943.86f); float s0 = AbsStation(spot);
            foreach (var (nm, s, back) in new[] { ("58_C_m5c_race_way", s0 - 10f, false), ("58_C_m5c_back", s0 + 10f, true) })
            {
                var p = race.Route.PointAt(s, out var f); if (back) f = -f;
                Place(p, f, 0f); yield return new WaitForSecondsRealtime(1.5f);
                var cam = Camera.main; var ch = cam ? cam.GetComponent<ChaseCamera>() : null; if (ch) ch.Snap();
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Capture(nm);
            }
            race.EndRace();
        }

        // ------------------------------------------------------------ D: the Ridge bump
        IEnumerator R58Bump()
        {
            report.AppendLine(); report.AppendLine("=== D. Chastain Ridge Drive (Dan's BUG-003 at x 3082.26 z 708.71, heading 270, 3.4 m right of the line): flat out both ways, on the line and 3.4 m either side");
            var spot = new Vector3(3082.3f, 0f, 708.7f);
            foreach (var dir in new[] { "forward", "reverse" })
            {
                yield return StartRoute("chastain_ridge.json", dir, 0);
                float sc = AbsStation(spot); var line = LapSlice(sc - 130f, sc + 70f); race.EndRace();
                foreach (float off in new[] { 0f, 3.4f, -3.4f })
                    foreach (var vid in new[] { "drifter", "original" })
                    {
                        var l = new List<Vector3>();
                        for (int i = 0; i < line.Count; i++) { var f = Flat(line[Mathf.Min(i + 1, line.Count - 1)] - line[Mathf.Max(i - 1, 0)]).normalized; var r = Vector3.Cross(Vector3.up, f); var q = line[i] + r * off; l.Add(RaceRoute.Surface(q.x, q.z, q.y)); }
                        yield return Vehicle(vid);
                        Place(l[0], l[2] - l[0], 20f); yield return new WaitForFixedUpdate();
                        var run = new Run(); float lastVy = car.Body.linearVelocity.y, jump = 0f, pitch = 0f; Vector3 jAt = Vector3.zero; int minW = 4; var lastRot = car.Body.rotation; float vAt = 0f;
                        yield return DriveLine(l, 40f, 18f, run, null, rr =>
                        {
                            var p = car.Body.position; float vy = car.Body.linearVelocity.y, dvy = Mathf.Abs(vy - lastVy); lastVy = vy;
                            var d = Quaternion.Inverse(lastRot) * car.Body.rotation; lastRot = car.Body.rotation; float px = Mathf.Abs(Mathf.DeltaAngle(0f, d.eulerAngles.x)) / Time.fixedDeltaTime;
                            if (Flat(p - spot).magnitude < 60f) { if (dvy > jump) { jump = dvy; jAt = p; vAt = Flat(car.Body.linearVelocity).magnitude; } pitch = Mathf.Max(pitch, px); minW = Mathf.Min(minW, car.GroundedWheels); }
                            return false;
                        });
                        report.AppendLine($"  {dir,-8} offset {off,4:0.0} m {Nm(vid),-16} biggest step in vertical speed {jump:0.00} m/s at ({jAt.x:0.0}, {jAt.z:0.0}) at {vAt:0.0} m/s; fastest pitch {pitch:0} deg/s; fewest wheels down {minW}; {(run.reached ? "through" : run.end)}");
                        Check($"D {dir} offset {off:0.0} {Nm(vid)}: rides through (wheels down, no jolt over 1 m/s in a step)", run.reached && minW >= 2 && jump < 1f, $"step {jump:0.00} m/s, wheels {minW}");
                    }
            }
            yield return Still("58_D_ridge_from_east", RaceRoute.Surface(3120f, 711f) + Vector3.up * 2.2f, RaceRoute.Surface(3082f, 707f) + Vector3.up * 0.6f);
        }

        IEnumerator R58Pics() { yield break; }   // the parts take their own pictures
    }
}
