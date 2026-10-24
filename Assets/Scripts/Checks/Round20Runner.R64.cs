using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 64 in the exe (-exeRound64 <dir> [-r64Only xloop,xshort,xdoors,xturn,xmound,xwalk,xpics | xai,xbar] [-r20Vehicles ids] [-traffic Normal]
    // -logFile <dir>\Player.log): the Noonday Creek Trail put right (Dan, 2026-10-23 21:06).
    //   xloop    the lap both ways (the new forward first) on every race vehicle, driven by script along the race's line (its AI zones' speeds as caps): the
    //            time, dead stops and what they met, spells in the air; in the mall: down the escalator and into the north wing (reverse), up it (forward),
    //            through Halverson Home both ways; over the mound by Town Center Drive
    //   xshort   each way's Carroway shortcut against the lap between the same two points: clean (its AI speeds), missed (slow off the kicker into the pit, out
    //            up the plank ramp at its end), and round the stunt (to the pit's end and down onto that ramp): the shortcut must save time clean, the
    //            other two must lose it
    //   xdoors   Halverson's east doors, the main doors, Halverson Home's and Carroway's portals and fronts: driven through at 15 m/s, their glass breaks and
    //            costs no speed; put back afterwards
    //   xturn    BUG-005: in a race, a car going west along the lower concourse past the north wing is held by the grille
    //   xmound   BUG-003: over the mound both ways at 13 and 22 m/s: the air, the landing
    //   xai      the race twice each way on Hard, 3 AI, traffic as given: every AI finishes, nobody hangs at the boardwalk (Round 58's replay analysis)
    //   xbar     every barrier holds (Round 62's check)
    //   xwalk    people on the trail in Free Roam (walking, jogging, cycling, with dogs) and a few in a race
    //   xpics    Dan's spots and the new pieces
    public sealed partial class Round20Runner
    {
        const string R64File = "noonday_creek_trail.json";
        // the mall's frame numbers (Tools/mapgen/make_trail_route.py)
        const float R64Vc = -1303.5f, R64PitV0 = -1372f, R64PitV1 = -1386f, R64PitU = -105.8f;

        bool InCorridor(Vector3 p) { float u = MallU(p), v = MallV(p); return u > -7f && u < 13f && v > -1290f && v < -1205f; }
        bool InHome(Vector3 p) { float u = MallU(p), v = MallV(p); return u > -48f && u < 44f && v > -1202f && v < -1136f; }

        IEnumerator R64Loop()
        {
            report.AppendLine(); report.AppendLine("=== The lap both ways (the new forward first), every race vehicle, driven by script along the race's line (the AI zones' speeds as caps)");
            var m = MallNow; if (!m) { Check("Town Center is built", false); yield break; }
            int stopsAll = 0;
            foreach (var dir in new[] { "forward", "reverse" })
            {
                UseRoute(R64File, dir);
                if (race.Route == null || race.Route.Name != "Noonday Creek Trail") { Check($"xloop {dir}: the route loads", false, race.Route?.Name ?? "none"); continue; }
                var ids = VehicleIds().Where(id => race.RouteData.AllowsVehicle(id)).ToArray();
                var line = race.Route.Path.ToList(); var prof = R62Profile();
                report.AppendLine($"  {dir}: lap {race.Route.LapLength:0} m, {race.Route.Checkpoints.Count} checkpoints ({string.Join(", ", race.Route.Checkpoints.Select(c => c.Label))}), {race.Route.Closures.Count} barriers, " +
                                  $"{race.Route.Shortcuts.Count} shortcut(s); grid ({race.Route.GridPosition.x:0}, {race.Route.GridPosition.z:0})");
                foreach (var id in ids)
                {
                    yield return Vehicle(id);
                    m.RestoreBreakables(); yield return new WaitForSecondsRealtime(.2f);
                    Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
                    var run = new Run(); var hist = new Queue<(float t, float v)>(); var stops = new List<string>(); var airs = new List<string>(); float lastAt = -99f, t = 0f;
                    var touch = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); touch.seen.Clear(); touch.on = true;
                    float air = 0f; Vector3 airFrom = Vector3.zero; float airTop = 0f; bool corridor = false, home = false, upper = false; string esc = "-";
                    Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
                    var drive = StartCoroutine(DriveLine(line, 45f, 900f, run, null, null, 5f, false, prof));
                    while (!run.reached && run.end == "" && run.time < 899f)
                    {
                        yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                        var p = car.Body.position; float v = Flat(car.Body.linearVelocity).magnitude;
                        if (car.GroundedWheels == 0) { if (air == 0f) { airFrom = p; airTop = p.y; } air += Time.fixedDeltaTime; airTop = Mathf.Max(airTop, p.y); }
                        else if (air > 0f) { if (air > .3f) airs.Add($"{air:0.0} s {Flat(p - airFrom).magnitude:0} m {airTop - airFrom.y:+0.0;-0.0} m up from ({airFrom.x:0}, {airFrom.z:0}){(InMall(airFrom) ? $" (the mall, u {MallU(airFrom):0})" : "")}"); air = 0f; }
                        if (InCorridor(p) && p.y < m.lowerFloor + 2.5f) corridor = true;
                        if (InHome(p) && p.y < m.lowerFloor + 2.5f) home = true;
                        if (InMall(p) && MallU(p) > 45f && MallU(p) < 52f && p.y > m.upperFloor - .5f) upper = true;
                        if (dir == "reverse" && esc == "-" && InMall(p) && MallU(p) < 6f && Mathf.Abs(MallV(p) - R64Vc) < 12f) esc = $"down at u {MallU(p):0}, {p.y - m.lowerFloor:0.0} m over the lower floor, {v:0} m/s";
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
                    stopsAll += stops.Count;
                    report.AppendLine($"  {dir,-8} {Nm(id),-16} {(run.reached ? $"the lap in {run.time:0.0} s" : $"NOT THROUGH ({run.end}) at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}), lap {race.Route.LapStationOf(car.Body.position):0} m")}; slowest {run.minSpeed:0.0} m/s; {stops.Count} dead stop(s); " +
                                      $"the north wing {(corridor ? "yes" : "NO")}, Halverson Home {(home ? "yes" : "NO")}, the escalator's top {(upper ? "yes" : "NO")}{(dir == "reverse" ? $", off the escalator: {esc}" : "")}");
                    report.AppendLine($"      air: {(airs.Count > 0 ? string.Join("; ", airs) : "none")}");
                    foreach (var s in stops) report.AppendLine("      stop: " + s);
                    Check($"xloop {dir} {Nm(id)}: round the lap through the north wing and Halverson Home, never stuck", run.reached && corridor && home, run.reached ? $"{run.time:0.0} s" : run.end);
                }
            }
            Check("xloop: no dead stop on the ground anywhere (listed above)", stopsAll == 0, $"{stopsAll} found");
            m.RestoreBreakables();
        }

        // the lap between two stations (wrapping) as a line
        List<Vector3> LapPart(float s0, float s1)
        {
            float L = race.Route.LapLength; var o = new List<Vector3>(); if (s1 < s0) s1 += L;
            for (float s = s0; s < s1; s += 2f) o.Add(race.Route.PointAt(Mathf.Repeat(s, L), out _));
            o.Add(race.Route.PointAt(Mathf.Repeat(s1, L), out _)); return o;
        }
        float LineLen(List<Vector3> l) { float a = 0f; for (int i = 1; i < l.Count; i++) a += Flat(l[i] - l[i - 1]).magnitude; return a; }
        // a timed drive along 'line' from a standing start at 14 m/s; null when it did not get through
        IEnumerator TimedDrive(List<Vector3> line, float[] prof, float timeout, Action<Run> done)
        {
            var m = MallNow; if (m) m.RestoreBreakables(); FenceChunk.RestoreAll(); yield return new WaitForSecondsRealtime(.2f);
            Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
            var run = new Run(); Time.timeScale = 2f; Time.maximumDeltaTime = .5f;
            yield return DriveLine(line, 45f, timeout, run, null, null, 5f, false, prof);
            Time.timeScale = 1f; done(run);
        }

        IEnumerator R64Shortcuts()
        {
            report.AppendLine(); report.AppendLine("=== The Carroway shortcut each way: clean, missed (into the pit), and round the stunt, against the lap between the same two points (Drifter Twin)");
            var m = MallNow; if (!m) { Check("Town Center is built", false); yield break; }
            yield return Vehicle("drifter");
            foreach (var dir in new[] { "forward", "reverse" })
            {
                UseRoute(R64File, dir);
                var sc = race.Route.Shortcuts.FirstOrDefault(); if (sc == null) { Check($"xshort {dir}: the shortcut is in the route", false); continue; }
                var path = sc.Path.ToList(); float sIn = race.Route.LapStationOf(path[0]), sOut = race.Route.LapStationOf(path[path.Count - 1]);
                var before = LapPart(sIn - 60f, sIn); var after = LapPart(sOut, sOut + 60f);
                var lap = LapPart(sIn - 60f, sOut + 60f);
                bool south = dir == "forward";   // forward: north to south through Carroway
                // its own profile on the combined line (the lap parts free)
                float off = LineLen(before);
                float[] Prof(float[] p) { if (p == null) return null; var o = new float[p.Length]; for (int i = 0; i < p.Length; i += 2) { o[i] = p[i] + off; o[i + 1] = p[i + 1]; } return o; }
                var clean = before.Concat(path).Concat(after).ToList();
                // missed: slow off the kicker (8 m/s), down into the pit, along it to the plank ramp at its end (forward: its west end rising south; reverse: east, rising north)
                Vector3 F(float u, float v, bool low = false) => m.P(u, v, low ? m.lowerFloor - 2.4f : m.lowerFloor);
                float edgeIn = south ? R64PitV0 : R64PitV1, edgeOut = south ? R64PitV1 : R64PitV0, sg = south ? -1f : 1f, rampU = south ? -137.6f : -73.5f;
                int kIn = path.FindIndex(q => Mathf.Abs(MallV(q) - (edgeIn - sg * 14f)) < 1.2f && InCarroway(q)), kOut = path.FindIndex(q => Mathf.Abs(MallV(q) - (edgeOut + sg * 12f)) < 1.2f && InCarroway(q));
                if (kIn < 0 || kOut < 0) { Check($"xshort {dir}: the path crosses the pit", false); continue; }
                var miss = before.Concat(path.Take(kIn + 1)).Concat(new[] { F(R64PitU, edgeIn + sg * 3f, true), F(R64PitU + (rampU - R64PitU) * .4f, (edgeIn + edgeOut) * .5f, true), F(rampU + (R64PitU - rampU) * .15f, edgeIn + sg * 2f, true), F(rampU, edgeIn + sg * 3.5f, true),
                    F(rampU, edgeOut + sg * 1.5f), F(rampU, edgeOut + sg * 7f), F(R64PitU, edgeOut + sg * 12f) }).Concat(path.Skip(kOut + 1)).Concat(after).ToList();
                var round = before.Concat(path.Take(kIn + 1)).Concat(new[] { F(rampU, edgeIn - sg * 4f), F(rampU, edgeIn - sg * 1f), F(rampU, edgeIn + sg * 1.5f, true), F(rampU, edgeIn + sg * 4f, true),
                    F(rampU, edgeOut + sg * 1.5f), F(rampU, edgeOut + sg * 7f), F(R64PitU, edgeOut + sg * 12f) }).Concat(path.Skip(kOut + 1)).Concat(after).ToList();
                float[] slow = Prof(sc.AiProfile)?.ToArray();
                var lipS = LineLen(before.Concat(path.Take(kIn + 1)).ToList()) + 14f;   // (the kicker's lip)
                var slowProf = new List<float>(slow ?? new float[0]); slowProf.AddRange(new[] { lipS - 20f, 8f, lipS + 2f, 8f });
                float tLap = -1f, tClean = -1f, tMiss = -1f, tRound = -1f; Run rl = null, rc = null, rm = null, rr = null;
                yield return TimedDrive(lap, R62Profile() is float[] zp ? ZonesOn(zp, sIn - 60f) : null, 200f, r => { rl = r; tLap = r.reached ? r.time : -1f; });
                yield return TimedDrive(clean, Prof(sc.AiProfile), 200f, r => { rc = r; tClean = r.reached ? r.time : -1f; });
                string inPit = car.Body.position.y < m.lowerFloor - 1f ? " (ended in the pit)" : "";
                yield return TimedDrive(miss, slowProf.ToArray(), 200f, r => { rm = r; tMiss = r.reached ? r.time : -1f; });
                yield return TimedDrive(round, Prof(sc.AiProfile), 200f, r => { rr = r; tRound = r.reached ? r.time : -1f; });
                report.AppendLine($"  {dir}: the lap {LineLen(lap):0} m in {Tm(tLap)}; the shortcut clean {LineLen(clean):0} m in {Tm(tClean)} (most air {rc.maxAir:0.0} s{inPit}); missed (8 m/s off the kicker, out up the plank ramp) {Tm(tMiss)}; round the stunt (down onto the plank ramp) {Tm(tRound)}");
                foreach (var (nm, rr2) in new[] { ("clean", rc), ("missed", rm), ("round", rr) })
                    if (!rr2.reached && rr2.trail.Count > 0) { var e = rr2.trail[rr2.trail.Count - 1]; report.AppendLine($"      {nm} stopped at ({e.x:0.0}, {e.y:0.0}, {e.z:0.0}): u {MallU(e):0.0} v {MallV(e):0.0} ({rr2.end})"); }
                Check($"xshort {dir}: clean saves time", tClean > 0f && tLap > 0f && tClean < tLap, $"{Tm(tClean)} against {Tm(tLap)}");
                Check($"xshort {dir}: missed costs more than the lap", tMiss < 0f || tMiss > tLap, $"{Tm(tMiss)} against {Tm(tLap)}");
                Check($"xshort {dir}: round the stunt is slower than the lap", tRound < 0f || tRound > tLap, $"{Tm(tRound)} against {Tm(tLap)}");
                Check($"xshort {dir}: never stuck (missed and round get out)", rm.reached && rr.reached, $"missed {(rm.reached ? "out" : rm.end)}, round {(rr.reached ? "out" : rr.end)}");
            }
            m.RestoreBreakables();
        }
        static string Tm(float t) => t < 0f ? "NOT THROUGH" : $"{t:0.0} s";
        bool InCarroway(Vector3 p) { float u = MallU(p), v = MallV(p); return u > -141f && u < -70f && v > -1440f && v < -1326f; }
        // the race's zone profile (lap stations) moved onto a line that starts at lap station s0
        float[] ZonesOn(float[] zp, float s0) { var o = new float[zp.Length]; float L = race.Route.LapLength; for (int i = 0; i < zp.Length; i += 2) { o[i] = Mathf.Repeat(zp[i] - s0, L); o[i + 1] = zp[i + 1]; } var pairs = new List<(float s, float v)>(); for (int i = 0; i < o.Length; i += 2) pairs.Add((o[i], o[i + 1])); return pairs.OrderBy(q => q.s).SelectMany(q => new[] { q.s, q.v }).ToArray(); }

        IEnumerator R64Doors()
        {
            report.AppendLine(); report.AppendLine("=== The doors (Dan, BUG-002 / BUG-006: \"these should be glass doors\"): driven through at 15 m/s, Street Classic, Free Roam");
            var m = MallNow; if (!m) yield break;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            var doors = new (string name, float u0, float v0, float u1, float v1, int level)[]
            {
                ("Halverson's east doors (in)", 182f, R64Vc, 150f, R64Vc, 1), ("the main doors (out)", -51f, -1285f, -51f, -1250f, 0),
                ("Halverson Home's portal (out)", -1.5f, -1150f, -1.7f, -1120f, 0), ("Halverson Home's front (in from the north wing)", -1f, -1225f, -1f, -1190f, 0),
                ("Carroway's front (in from the court)", -105.6f, -1312f, -105.6f, -1340f, 0), ("Carroway's portal (out)", -105.8f, -1425f, -105.8f, -1455f, 0),
            };
            foreach (var d in doors)
            {
                m.RestoreBreakables(); yield return new WaitForSecondsRealtime(.3f);
                var a = m.P(d.u0, d.v0, 0f); var b = m.P(d.u1, d.v1, 0f);
                var line = new List<Vector3> { RaceRoute.Surface(a.x, a.z), RaceRoute.Surface(Vector3.Lerp(a, b, .5f).x, Vector3.Lerp(a, b, .5f).z), RaceRoute.Surface(b.x, b.z) };
                if (d.level >= 0) for (int i = 0; i < line.Count; i++) line[i] = new Vector3(line[i].x, (d.level == 0 ? m.lowerFloor : m.upperFloor) + .6f, line[i].z);
                if (d.name.StartsWith("Halverson's east")) line[0] = RaceRoute.Surface(a.x, a.z);
                int before = FenceChunk.BrokenPanels; Place(line[0], line[1] - line[0], 15f); yield return new WaitForFixedUpdate();
                var run = new Run(); yield return DriveLine(line, 15f, 6f, run, null, null, 4f, false);
                int broke = FenceChunk.BrokenPanels - before;
                report.AppendLine($"  {d.name}: {broke} panels broken, slowest {run.minSpeed:0.0} m/s, {(run.reached ? "through" : "NOT THROUGH (" + run.end + ")")}");
                Check($"xdoors {d.name}: through, the glass breaks, no speed lost", run.reached && broke > 0 && run.minSpeed > 11f, $"{broke} panels, slowest {run.minSpeed:0.0} m/s");
            }
            int back = m.RestoreBreakables(); report.AppendLine($"  put back: {back} sets");
        }

        IEnumerator R64Turn()
        {
            report.AppendLine(); report.AppendLine("=== BUG-005 (\"there should been something here forcing you to turn\"): in a race, a car going west along the lower concourse past the north wing");
            var m = MallNow; if (!m) yield break;
            yield return Vehicle("original");
            foreach (var dir in new[] { "reverse", "forward" })
            {
                yield return StartRoute(R64File, dir, 0);
                var a = m.P(4f, R64Vc, m.lowerFloor + .5f); var b = m.P(-30f, R64Vc, m.lowerFloor + .5f);
                Place(a, b - a, 16f); yield return new WaitForFixedUpdate();
                var run = new Run(); yield return DriveLine(new List<Vector3> { a, b }, 16f, 5f, run, null, null, 5f, false);
                float u = MallU(car.Body.position);
                report.AppendLine($"  {dir}: the car stopped at u {u:0.0} (the grille at u -12), {Flat(car.Body.linearVelocity).magnitude:0.0} m/s; grilles built {race.Barriers.Seals}");
                Check($"xturn {dir}: the grille west of the north wing holds", u > -13.5f, $"u {u:0.0}");
                race.EndRace();
            }
        }

        IEnumerator R64Mound()
        {
            report.AppendLine(); report.AppendLine("=== BUG-003: the mound by Town Center Drive, both ways, at 13 and 22 m/s (Drifter Twin)");
            yield return Vehicle("drifter");
            foreach (var dir in new[] { "forward", "reverse" })
            {
                UseRoute(R64File, dir);
                float s = race.Route.LapStationOf(RaceRoute.Surface(172.5f, -910.5f));
                foreach (float v in new[] { 13f, 22f })
                {
                    var line = LapPart(s - 45f, s + 45f); Place(line[0], line[2] - line[0], v); yield return new WaitForFixedUpdate();
                    var run = new Run(); yield return DriveLine(line, v, 12f, run, null, null, 5f, false, new[] { 0f, v, 200f, v });
                    report.AppendLine($"  {dir} at {v:0} m/s: most air {run.maxAir:0.00} s, landed {(float.IsNaN(run.firstLanding.x) ? "-" : $"at ({run.firstLanding.x:0}, {run.firstLanding.z:0}), upright {run.upAtLanding:0.00}")}, slowest {run.minSpeed:0.0} m/s, {(run.reached ? "through" : "NOT THROUGH " + run.end)}");
                    Check($"xmound {dir} {v:0} m/s: a jump, landed clean", run.reached && run.minSpeed > v * .5f && (float.IsNaN(run.upAtLanding) || run.upAtLanding < 0f || run.upAtLanding > .85f), $"air {run.maxAir:0.00} s, slowest {run.minSpeed:0.0}");
                }
            }
        }

        IEnumerator R64Walkers()
        {
            report.AppendLine(); report.AppendLine("=== BUG-004 (\"It would be good to have peopel walking or jogging this trail\"): people on the trail");
            var life = AmbientLife.Current; if (!life) { Check("the life is in the build", false); yield break; }
            if (race.State != RaceController.Stage.Free) race.EndRace();
            yield return Vehicle("original");
            int most = 0; var kinds = new int[5];
            foreach (var (x, z, yaw) in new[] { (360f, -627f, 60f), (1000f, -560f, 90f), (1300f, -790f, 100f) })
                foreach (float hour in new[] { 8f, 18f })
                {
                    yield return Clock(hour); life.ClearWalkers(); respawn.PlaceAt(RaceRoute.Surface(x, z) + Vector3.up * .4f, yaw); race.NotifyTeleported();
                    yield return new WaitForSecondsRealtime(2f); yield return LookRound(RaceRoute.Surface(x, z)); yield return new WaitForSecondsRealtime(5f);
                    most = Mathf.Max(most, life.TrailWalkers); for (int k = 0; k < 5; k++) kinds[k] += life.WalkersByKind[k];
                    report.AppendLine($"  {hour:00}:00 at ({x:0}, {z:0}): {life.WalkersInfo}");
                }
            Check("xwalk: people out on the trail", most > 0, $"most at once {most}; over the stops dog walkers {kinds[0]}, couples {kinds[2]}, joggers {kinds[3]}, cyclists {kinds[4]}");
            Check("xwalk: cyclists among them", kinds[4] > 0, $"{kinds[4]}");
            // a race: a few on it
            yield return Clock(8f); yield return StartRoute(R64File, "forward", 0);
            respawn.PlaceAt(RaceRoute.Surface(1000f, -560f) + Vector3.up * .4f, 90f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f);
            yield return LookRound(RaceRoute.Surface(1000f, -560f)); yield return new WaitForSecondsRealtime(5f);
            report.AppendLine($"  in a race: {life.WalkersInfo}");
            Check("xwalk: in a race, two groups at most on the trail", life.TrailWalkers <= 2, $"{life.TrailWalkers}");
            race.EndRace();
            // a cyclist's picture
            life.ClearWalkers(); yield return Clock(9f); var at = RaceRoute.Surface(1000f, -560f); respawn.PlaceAt(at + Vector3.up * .4f + Vector3.right * 30f, 270f); race.NotifyTeleported();
            yield return new WaitForSecondsRealtime(1.5f); int id = life.ForceWalker(AmbientLife.WalkKind.Cyclist, at); yield return new WaitForSecondsRealtime(1.5f);
            var wp = life.WalkerPosition(id); if (id >= 0) yield return Still("64_trail_cyclist", wp + new Vector3(4f, 2.2f, 5f), wp + Vector3.up, 55f);
            id = life.ForceWalker(AmbientLife.WalkKind.Jogger, at); yield return new WaitForSecondsRealtime(1f); wp = life.WalkerPosition(id);
            if (id >= 0) yield return Still("64_trail_jogger", wp + new Vector3(-4f, 2f, 5f), wp + Vector3.up, 55f);
        }

        IEnumerator R64Pics()
        {
            report.AppendLine(); report.AppendLine("=== Pictures");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var m = MallNow; yield return Clock(16f);
            Vector3 G(float x, float z, float h) => RaceRoute.Surface(x, z) + Vector3.up * h;
            IEnumerator Park(float x, float z, float yaw) { respawn.PlaceAt(G(x, z, .4f), yaw); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2.5f); }
            UseRoute(R64File, "forward"); race.StartRace(); yield return new WaitForSecondsRealtime(1.5f);
            yield return Still("64_start_forward", G(1745f, -700f, 7f), G(1733f, -800f, 2f), 60f);
            race.EndRace();
            if (m)
            {
                yield return Park(m.P(215f, R64Vc, 0f).x, m.P(215f, R64Vc, 0f).z, 255f);
                yield return Still("64_bug002_east_doors", G(506f, -1214.5f, 2.2f), m.P(163.5f, R64Vc, m.upperFloor + 1.5f), 65f);
                yield return Still("64_bug006_main_doors", G(266.6f, -1217f, 2.2f), m.P(-51f, -1267f, m.lowerFloor + 1.6f), 65f);
                yield return Still("64_bug005_turn_grille", m.P(20f, R64Vc - 2f, m.lowerFloor + 2f), m.P(-12f, R64Vc, m.lowerFloor + 1.2f), 70f);
                yield return Still("64_north_wing_turn", m.P(14f, R64Vc - 6f, m.lowerFloor + 2f), m.P(-1f, -1270f, m.lowerFloor + 1.2f), 70f);
                yield return Still("64_escalator_down_reverse", m.P(55f, R64Vc, m.upperFloor + 2.2f), m.P(0f, R64Vc, m.lowerFloor), 70f);
                yield return Still("64_north_wing_corridor", m.P(-1f, -1290f, m.lowerFloor + 2f), m.P(-1f, -1205f, m.lowerFloor + 1.6f), 70f);
                yield return Still("64_halverson_home_inside", m.P(-1f, -1198f, m.lowerFloor + 2f), m.P(-1.7f, -1136f, m.lowerFloor + 1.6f), 70f);
                yield return Park(m.P(-1.7f, -1110f, 0f).x, m.P(-1.7f, -1110f, 0f).z, 165f);
                yield return Still("64_halverson_home_portal", G(m.P(-1.7f, -1112f, 0f).x, m.P(-1.7f, -1112f, 0f).z, 2.4f), m.P(-1.7f, -1135.4f, m.lowerFloor + 1.6f), 65f);
                yield return Still("64_carroway_pit", m.P(-105.8f, -1352f, m.lowerFloor + 2.6f), m.P(-105.8f, -1380f, m.lowerFloor), 70f);
                yield return Still("64_carroway_pit_side", m.P(-128f, -1366f, m.lowerFloor + 3f), m.P(-105f, -1384f, m.lowerFloor - 1f), 70f);
                yield return Still("64_carroway_front_from_hall", m.P(-51f, -1288f, m.lowerFloor + 2f), m.P(-105.6f, -1325f, m.lowerFloor + 1.5f), 70f);
                yield return Park(m.P(-105.8f, -1470f, 0f).x, m.P(-105.8f, -1470f, 0f).z, 345f);
                yield return Still("64_carroway_portal", G(m.P(-100f, -1468f, 0f).x, m.P(-100f, -1468f, 0f).z, 2.6f), m.P(-105.8f, -1439.3f, m.lowerFloor + 1.6f), 65f);
            }
            yield return Park(560f, -1440f, 30f); yield return Still("64_ring_road", G(540f, -1462f, 4f), G(588f, -1330f, 1f), 60f);
            yield return Park(600f, -1290f, 300f); yield return Still("64_east_lot_to_doors", G(590f, -1320f, 4f), G(505f, -1215f, 1.5f), 60f);
            yield return Park(255f, -1050f, 120f); yield return Still("64_north_lot_reroute", G(225f, -1030f, 4f), G(287f, -1098f, 1.5f), 60f);
            yield return Park(179.8f, -919.5f, 323.5f); yield return Still("64_bug003_mound", G(186f, -930f, 2.2f), G(170f, -905f, 1.5f), 60f);
            yield return Still("64_bug003_mound_from_road", G(162f, -888f, 2.2f), G(176f, -920f, 2f), 60f);
            yield return Park(199f, -721f, 48f); yield return Still("64_bug004_trail", G(199f, -721f, 2.2f), G(230f, -690f, 1.5f), 60f);
            yield return Park(704.8f, -441.5f, 248f); yield return Still("64_bug007_boardwalk", G(704.8f, -441.5f, 2.2f), G(660f, -458f, 1.5f), 60f);
            yield return Park(133.4f, -793.4f, 171f); yield return Still("64_bug008_red_bridge_rails", G(133.4f, -793.4f, 2.2f), G(140f, -830f, 1.5f), 60f);
            yield return Park(700f, -1668f, 272f); yield return Still("64_bug001_barrett", G(700f, -1668f, 2.2f), G(620f, -1670f, 1.5f), 60f);
            yield return Park(400f, -1500f, 300f); yield return Still("64_shortcut_tracks_to_carroway", G(425f, -1520f, 4f), G(290f, -1447f, 1.5f), 60f);
            yield return Park(212f, -1012f, 170f); yield return Still("64_shortcut_tracks_to_main_doors", G(210f, -1000f, 4f), G(257f, -1200f, 1.5f), 60f);
        }
    }
}
