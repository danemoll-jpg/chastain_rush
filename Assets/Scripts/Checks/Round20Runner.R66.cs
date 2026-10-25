using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 66 in the exe (-exeRound66 <dir> [-r66Only spics,sfps,hedge,eoff,edrive,etraffic] [-sceneryOff] -logFile <dir>\Player.log): the map's edges.
    //   spics     from the summit (eye height) and from the helicopter's ceiling over the map's middle and near each edge, looking out each way:
    //             day, night, and rain toward Atlanta (pictures 66_*.png)
    //   sfps      frame rate at the summit (eye height, looking out) and at the ceiling over Chastain Glen, Town Center and the mountain (a fixed
    //             camera, 8 s each; with -sceneryOff the same without the scenery)
    //   hedge     the helicopter flown flat out (boost) straight at each edge from 1.5 km inside: the warning, how far out it gets, turned back
    //   eoff      a car driven off road straight at each edge at 30 m/s: stops before the edge, never past it
    //   edrive    every road that leaves the map: driven toward the edge from 160 m before its closure: stops at the closure
    //   etraffic  traffic at the highways' closures (-traffic Heavy): cars that turn round there against cars that stop
    public sealed partial class Round20Runner
    {
        static readonly Vector2 R66Summit = new Vector2(-1122.98f, -5870.01f);
        // the summit's highest ground within 300 m (the eye 45 m over it: over the trees' tops round the top)
        static Vector3 R66SummitEye()
        {
            var best = RaceRoute.Surface(R66Summit.x, R66Summit.y);
            for (float dx = -300f; dx <= 300f; dx += 10f) for (float dz = -300f; dz <= 300f; dz += 10f)
                if (GroundQuery.TryHeight(R66Summit.x + dx, R66Summit.y + dz, out float y) && y > best.y) best = new Vector3(R66Summit.x + dx, y, R66Summit.y + dz);
            return best + Vector3.up * 45f;   // over the summit's woods (the pines there stand 30 m and more)
        }
        static readonly (string name, float yaw)[] R66Ways = { ("north", 0f), ("east", 90f), ("south", 180f), ("west", 270f) };
        const float R66Ceiling = 1690f;

        IEnumerator R66Look(string name, Vector3 at, float yaw, float pitch, float fov = 62f)
        {
            var look = at + Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * 100f;
            yield return Still(name, at, look, fov);
        }
        static float R66AtlantaYaw(Vector3 from)
        {
            var t = new Vector3(17373f, 0f, -28229f) - from; return Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg;
        }
        string R66View() => $"lift {FarView.Lift:0} m, fog to {FarView.View:0} m, world far {FarView.WorldFar:0} m, far clip {Camera.main.farClipPlane:0} m, horizon {(EdgeScenery.HorizonOn ? "on" : "off")}";

        IEnumerator R66Pics()
        {
            report.AppendLine(); report.AppendLine("=== Pictures: the summit and the ceiling, looking out each way (day, night; rain toward Atlanta)");
            var look = WorldLook.Current; var s = GameSettings.Settings;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            Place(RaceRoute.Surface(R66Summit.x + 40f, R66Summit.y + 40f), Vector3.forward, 0f);   // the car out of the pictures
            var summit = R66SummitEye();
            var middle = new Vector3(-2300f, R66Ceiling, -3200f);
            var conds = new (string name, float hour, int weather)[] { ("day", 12f, 0), ("night", 23.5f, 0) };
            foreach (var c in conds)
            {
                if (look) { s.roamWeather = c.weather; look.SetClock(12, c.hour); }
                foreach (var (way, yaw) in R66Ways) { yield return R66Look($"66_summit_{way}_{c.name}", summit, yaw, 4f); }
                yield return R66Look($"66_summit_atlanta_{c.name}", summit, R66AtlantaYaw(summit), 2f);
                yield return R66Look($"66_summit_atlanta_zoom_{c.name}", summit, R66AtlantaYaw(summit), 1.5f, 18f);
                report.AppendLine($"INFO  summit, {c.name}: {R66View()}; {(look ? look.Conditions : "no look")}");
                foreach (var (way, yaw) in R66Ways) yield return R66Look($"66_ceiling_{way}_{c.name}", middle, yaw, 12f);
                yield return R66Look($"66_ceiling_atlanta_{c.name}", middle, R66AtlantaYaw(middle), 6f);
                report.AppendLine($"INFO  ceiling over the middle ({middle.x:0}, {middle.z:0}, y {middle.y:0}), {c.name}: {R66View()}");
            }
            if (look) { s.roamWeather = 0; look.SetClock(12, 12f); }
            // near each edge at the ceiling, looking out over it
            var m = MapEdge.Map;
            var edges = new (string name, Vector3 at, float yaw)[]
            {
                ("north", new Vector3(m.center.x, R66Ceiling, m.yMax - 400f), 0f), ("east", new Vector3(m.xMax - 400f, R66Ceiling, m.center.y), 90f),
                ("south", new Vector3(m.center.x, R66Ceiling, m.yMin + 400f), 180f), ("west", new Vector3(m.xMin + 400f, R66Ceiling, m.center.y), 270f),
            };
            foreach (var e in edges) yield return R66Look($"66_edge_{e.name}_ceiling", e.at, e.yaw, 14f);
            // low over the west edge (150 m), and the summit in rain toward Atlanta
            yield return R66Look("66_edge_west_low", new Vector3(m.xMin + 300f, RaceRoute.Surface(m.xMin + 300f, m.center.y).y + 150f, m.center.y), 270f, 10f);
            if (look) { s.roamWeather = 1; look.SetClock(12, 12f); yield return new WaitForSecondsRealtime(3f); }
            yield return R66Look("66_summit_atlanta_rain", summit, R66AtlantaYaw(summit), 2f);
            report.AppendLine($"INFO  summit in rain: {R66View()}");
            if (look) { s.roamWeather = 0; look.SetClock(12, 12f); }
            var sc = EdgeScenery.Instance;
            Check("spics: the edge scenery is loaded", sc, sc ? $"{sc.Trees} trees, {sc.Buildings} buildings, {sc.Towers} towers" : "none (-sceneryOff?)");
        }

        IEnumerator R66Fps()
        {
            report.AppendLine(); report.AppendLine($"=== Frame rate, {Screen.width} x {Screen.height} (not measured at 3840 x 2160), a fixed camera, uncapped, 8 s each; scenery {(EdgeScenery.Instance ? "on" : "OFF (-sceneryOff)")}");
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var summit = R66SummitEye();
            var spots = new List<(string name, Vector3 at, float yaw, float pitch)>
            {
                ("summit, looking south-east (Atlanta)", summit, R66AtlantaYaw(summit), 2f), ("summit, looking north (the map)", summit, 20f, 3f),
                ("ceiling over Chastain Glen, looking west", new Vector3(2938.68f, R66Ceiling, 799.3f), 270f, 12f),
                ("ceiling over Town Center, looking south-east", new Vector3(265.11f, R66Ceiling, -1207.29f), 140f, 12f),
                ("ceiling over the mountain, looking west", new Vector3(-1122.98f, R66Ceiling, -5870.01f), 270f, 12f),
                ("ceiling near the north-east corner, looking out", new Vector3(3500f, R66Ceiling, 1500f), 45f, 12f),
            };
            Place(RaceRoute.Surface(R66Summit.x + 40f, R66Summit.y + 40f), Vector3.forward, 0f);
            foreach (var (name, at, yaw, pitch) in spots)
            {
                var cam = Camera.main; var chase = cam.GetComponent<ChaseCamera>(); if (chase) chase.enabled = false;
                cam.transform.position = at; cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                yield return new WaitForSecondsRealtime(2.5f);
                var frames = new List<float>(); float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 8f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * .99f))];
                var tf = FindAnyObjectByType<TreeField>();
                report.AppendLine($"INFO  {name}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps; {R66View()}, terrain tiles {FarTerrain.Drawn}, map trees {(tf ? tf.LastTreesDrawn : -1)}, edge trees {(EdgeScenery.Instance ? EdgeScenery.Instance.TreesDrawn : 0)}");
                if (chase) { chase.enabled = true; chase.Snap(); }
            }
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }

        IEnumerator R66HeliEdges()
        {
            report.AppendLine(); report.AppendLine("=== The helicopter flown flat out (boost) straight at each edge from 1.5 km inside, 150 m over the ground, 70 s, sticks held forward");
            var m = MapEdge.Map;
            var runs = new (string name, Vector2 at, float yaw)[]
            {
                ("north", new Vector2(-1500f, m.yMax - 1500f), 0f), ("east", new Vector2(m.xMax - 1500f, -2000f), 90f),
                ("south", new Vector2(-3000f, m.yMin + 1500f), 180f), ("west", new Vector2(m.xMin + 1500f, -3000f), 270f),
            };
            foreach (var (name, at, yaw) in runs)
            {
                yield return R63Heli(at, yaw); var h = Heli; if (!h) yield break;
                float t0 = Time.time; while (h.Spool < .95f && Time.time - t0 < 6f) yield return null;
                // started in the air, 150 m over the ground (it holds there)
                var air = RaceRoute.Surface(at.x, at.y) + Vector3.up * 150f; car.Body.position = air; car.Body.rotation = Quaternion.Euler(0, yaw, 0); car.Body.linearVelocity = Vector3.zero; car.transform.SetPositionAndRotation(air, car.Body.rotation);
                yield return new WaitForSecondsRealtime(1.5f);
                h.MaxOutside = -1e9f; bool warned = false; float warnAt = float.NaN, turnedAt = float.NaN; int crashes = h.Crashes; t0 = Time.time; bool shot = false; float nextLog = 0f; var trace = new System.Text.StringBuilder();
                while (Time.time - t0 < 70f)
                {
                    Sticks(fwd: 1f, boost: true); yield return new WaitForFixedUpdate();
                    if (Time.time - t0 >= nextLog) { nextLog += 10f; var vv = car.Body.linearVelocity; trace.Append($" {Time.time - t0:0}s:{h.Outside:0}m/{Flat(vv).magnitude:0}m/s/yaw{car.transform.eulerAngles.y:0}"); }
                    if (h.Leaving && !warned) { warned = true; warnAt = h.Outside; }
                    var v = car.Body.linearVelocity; var outward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                    if (float.IsNaN(turnedAt) && h.Outside > 0f && Vector3.Dot(v, outward) < 0f) turnedAt = h.Outside;
                    if (!shot && h.Outside > 200f) { shot = true; yield return Capture($"66_heli_{name}_headwind"); }
                }
                Sticks();
                float endOut = h.Outside; var heading = car.transform.forward; var outw = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                Check($"hedge: {name}: warned, turned back past the edge, no crash", warned && h.MaxOutside < HelicopterFlight.HardStop + 150f && h.Crashes == crashes && !float.IsNaN(turnedAt),
                    $"warning from {warnAt:0} m (negative = inside), furthest out {h.MaxOutside:0} m, flying back from {turnedAt:0} m out, after 70 s {endOut:0} m out, nose {Vector3.Angle(Flat(heading), -Flat(outw)):0} deg from straight back, {h.HeightAboveGround:0} m up, crashes {h.Crashes - crashes}; trace{trace}");
            }
            Sticks(); yield return Vehicle("original");
        }

        IEnumerator R66OffRoad()
        {
            report.AppendLine(); report.AppendLine("=== A car (the Street Classic) driven off road straight at each edge at 30 m/s from 120 m inside");
            yield return Vehicle("original");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var m = MapEdge.Map;
            var sides = new (string name, Vector2 dir)[] { ("north", Vector2.up), ("east", Vector2.right), ("south", Vector2.down), ("west", Vector2.left) };
            foreach (var (name, dir) in sides)
            {
                // starts along the edge every 700 m (off any road): the first run that gets within 25 m of the edge counts (trees, houses and fences stop the others first)
                float best = float.MaxValue; string how = ""; int tries = 0, put = MapEdge.PutBack; bool shot = false;
                for (float u = .12f; u < .95f && best > 25f && tries < 8; u += .0577f)
                {
                    Vector2 at = dir.x != 0 ? new Vector2(dir.x > 0 ? m.xMax - 120f : m.xMin + 120f, Mathf.Lerp(m.yMin, m.yMax, u)) : new Vector2(Mathf.Lerp(m.xMin, m.xMax, u), dir.y > 0 ? m.yMax - 120f : m.yMin + 120f);
                    var pr = PoliceRoads.Instance; if (pr && pr.LimitAt(new Vector3(at.x, 0, at.y), out bool onRoad, out _) >= 0 && onRoad) continue;
                    tries++;
                    var line = new List<Vector3> { RaceRoute.Surface(at.x, at.y), RaceRoute.Surface(at.x + dir.x * 300f, at.y + dir.y * 300f) };
                    Place(line[0], line[1] - line[0], 25f); yield return new WaitForSecondsRealtime(.3f);
                    var run = new Run(); float closest = float.MaxValue;
                    yield return DriveLine(line, 30f, 14f, run, null, r => { closest = Mathf.Min(closest, MapEdge.Inside(car.Body.position, out _)); return false; });
                    float d = MapEdge.Inside(car.Body.position, out _);
                    how += $" [{at.x:0},{at.y:0}: closest {closest:0.0} m, ended {d:0.0} m at {Flat(car.Body.linearVelocity).magnitude:0.0} m/s]";
                    if (closest < best) best = closest;
                    if (closest < 25f && !shot) { shot = true; yield return Capture($"66_offroad_{name}"); }
                }
                Check($"eoff: {name}: a car off road stops short of the edge, never past it", best < 25f && best > -0.5f && MapEdge.PutBack == put, $"closest {best:0.0} m inside, put back {MapEdge.PutBack - put};{how}");
            }
        }

        // the road's line from ~110 m before a closure's cut up to it (the police roads end there too), then on through the closure toward the edge
        static List<Vector3> R66Approach(RoadClosures rc, int i)
        {
            var pr = PoliceRoads.Instance; var cut = rc.cut[i]; var line2 = new List<Vector2>();
            if (pr)
            {
                int best = -1; bool atEnd = false; float bd = 4f * 4f;
                for (int r = 0; r < pr.Count; r++)
                {
                    var a = pr.points[pr.first[r]]; var b = pr.points[pr.first[r + 1] - 1];
                    if ((a - cut).sqrMagnitude < bd) { bd = (a - cut).sqrMagnitude; best = r; atEnd = false; }
                    if ((b - cut).sqrMagnitude < bd) { bd = (b - cut).sqrMagnitude; best = r; atEnd = true; }
                }
                if (best >= 0)
                {
                    var pts = new List<Vector2>(); for (int k = pr.first[best]; k < pr.first[best + 1]; k++) pts.Add(pr.points[k]);
                    if (!atEnd) pts.Reverse();   // toward the cut
                    float s = 0f; line2.Add(pts[pts.Count - 1]);
                    for (int k = pts.Count - 2; k >= 0 && s < 110f; k--) { s += (pts[k + 1] - pts[k]).magnitude; line2.Add(pts[k]); }
                    line2.Reverse();
                }
            }
            if (line2.Count < 2 || (line2[line2.Count - 1] - line2[0]).magnitude < 40f) { line2.Clear(); line2.Add(cut - rc.dir[i] * 110f); line2.Add(cut); }
            line2.Add(rc.at[i] + rc.dir[i] * 30f);
            return line2.Select(q => RaceRoute.Surface(q.x, q.y)).ToList();
        }

        IEnumerator R66Closures()
        {
            var rc = RoadClosures.Instance;
            report.AppendLine(); report.AppendLine($"=== Every road that leaves the map: the Street Classic driven along it at 22 m/s (highways 30) from ~110 m before its closure, straight on into it (time x 4)");
            Check("edrive: the closures are built", rc && rc.Count > 0, rc ? $"{rc.Count} closures, {rc.highway.Count(h => h)} with jersey barriers" : "none (not built)");
            if (!rc) yield break;
            yield return Vehicle("original");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            float scale = Time.timeScale; Time.timeScale = 4f;
            int stopped = 0, past = 0, shots = 0; var bad = new List<string>(); float worstPast = -99f;
            for (int i = 0; i < rc.Count; i++)
            {
                var line = R66Approach(rc, i); var at = rc.at[i]; var d = rc.dir[i]; int put = MapEdge.PutBack;
                Place(line[0], line[1] - line[0], 12f); yield return new WaitForFixedUpdate();
                var run = new Run(); float beyond = -99f, still = 0f;
                yield return DriveLine(line, rc.highway[i] ? 30f : 22f, 22f, run, null, r =>
                {
                    var p = car.Body.position; var q = new Vector2(p.x, p.z) - at; if (q.magnitude < 60f) beyond = Mathf.Max(beyond, Vector2.Dot(q, d));   // (near the closure: a start on another road far off is not 'past' it)
                    still = Flat(car.Body.linearVelocity).magnitude < 1f && r.trail.Count > 20 ? still + Time.fixedDeltaTime : 0f;
                    return still > 1.2f;
                });
                bool ok = beyond < 2.5f && MapEdge.PutBack == put;
                if (ok) stopped++; else { past++; bad.Add($"{(rc.names[i].Length > 0 ? rc.names[i] : rc.classes[i])} at ({at.x:0}, {at.y:0}): {beyond:0.0} m past the closure{(MapEdge.PutBack != put ? ", put back from past the edge" : "")}"); }
                worstPast = Mathf.Max(worstPast, beyond);
                if ((rc.highway[i] && shots < 3) || (!ok && shots < 8) || i == 0 || i == rc.Count / 2)
                {
                    shots++; Time.timeScale = 1f;
                    var eye = RaceRoute.Surface(at.x - d.x * 22f, at.y - d.y * 22f) + Vector3.up * 2.2f;
                    yield return Still($"66_closure_{i:000}_{new string((rc.names[i].Length > 0 ? rc.names[i] : rc.classes[i]).Where(char.IsLetterOrDigit).Take(20).ToArray())}", eye, new Vector3(at.x, eye.y - 1.2f, at.y) + new Vector3(d.x, 0, d.y) * 30f, 58f);
                    Time.timeScale = 4f;
                }
            }
            Time.timeScale = scale;
            Check("edrive: every road leaving the map stops at its closure", past == 0, $"{stopped} of {rc.Count} stopped at their closure; furthest past a closure's middle {worstPast:0.0} m" + (bad.Count > 0 ? "; " + string.Join("; ", bad.Take(12)) : ""));
        }

        IEnumerator R66Traffic()
        {
            var rc = RoadClosures.Instance; var tr = Traffic.Instance; var net = TrafficNetwork.Instance;
            report.AppendLine(); report.AppendLine($"=== Traffic at the closures (traffic level {(tr ? Traffic.LevelNames[tr.Level] : "none")}): 50 s by each highway closure and four two-way roads' closures");
            if (!rc || !tr || !net) { Check("etraffic: traffic and closures present", false, $"closures {(rc ? rc.Count : 0)}, traffic {(tr ? "on" : "off")} (-traffic Heavy)"); yield break; }
            report.AppendLine($"INFO  the network: {net.TurnNodeCount} turn-round ends, {net.TurnEdgeCount} divided-highway turnarounds");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var picks = Enumerable.Range(0, rc.Count).Where(i => rc.highway[i]).ToList();
            picks.AddRange(Enumerable.Range(0, rc.Count).Where(i => !rc.highway[i] && (rc.classes[i] == "secondary" || rc.classes[i] == "tertiary")).Take(4));
            var done = new List<Vector2>(); int turns0 = Traffic.EdgeTurns, stops0 = Traffic.EdgeStops;
            foreach (int i in picks)
            {
                if (done.Any(q => (q - rc.at[i]).magnitude < 200f)) continue; done.Add(rc.at[i]);
                var park = rc.at[i] - rc.dir[i] * 140f + new Vector2(rc.dir[i].y, -rc.dir[i].x) * (rc.half[i] + 25f);
                Place(RaceRoute.Surface(park.x, park.y), new Vector3(rc.dir[i].x, 0, rc.dir[i].y), 0f);
                int t0 = Traffic.EdgeTurns, s0 = Traffic.EdgeStops; float until = Time.time + 50f; bool shot = false;
                while (Time.time < until)
                {
                    yield return new WaitForSecondsRealtime(.5f);
                    if (!shot && tr.Cars.Any(c => c && net.IsTurnaround(c.Edge) && Vector2.Distance(new Vector2(c.Car.Body.position.x, c.Car.Body.position.z), rc.at[i]) < 120f))
                    { shot = true; var eye = RaceRoute.Surface(rc.at[i].x - rc.dir[i].x * 60f, rc.at[i].y - rc.dir[i].y * 60f) + Vector3.up * 14f; yield return Still($"66_traffic_turn_{i:000}", eye, new Vector3(rc.at[i].x, eye.y - 12f, rc.at[i].y), 60f); }
                }
                report.AppendLine($"INFO  {(rc.names[i].Length > 0 ? rc.names[i] : rc.classes[i])} ({(rc.highway[i] ? "highway" : rc.classes[i])}) at ({rc.at[i].x:0}, {rc.at[i].y:0}): {Traffic.EdgeTurns - t0} turned round at the edge, {Traffic.EdgeStops - s0} came to a dead end with nowhere to turn");
            }
            Check("etraffic: traffic turns round at the closures", Traffic.EdgeTurns - turns0 > 0, $"{Traffic.EdgeTurns - turns0} turned round, {Traffic.EdgeStops - stops0} dead ends with nowhere to turn (they wait and are moved off unseen, as before)");
        }
    }
}
