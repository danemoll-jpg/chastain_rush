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
    // Development check only (added by Editor/ShortcutCheck.cs, never saved into the scene). Round 8, item 4g. For each
    // official shortcut and each approach speed, the real car (ArcadeVehicle.Simulate, the same step the player's input
    // drives) starts on the lap 80 m before the turn-off at that speed and is steered along the shortcut's line, holding the
    // speed where it is on the ground; the same start is also driven along the road. Both are timed to a line 20 m past
    // the rejoin point. Recorded: the time against the road, every flight (air time, distance, height), the car's tilt
    // and pitch on landing, any knock against a house, and how the run ended. Then two one-lap races through the real
    // RaceController: one on the road, one taking both shortcuts (no penalty, the forgiven checkpoints named).
    public sealed class ShortcutCheckRunner : MonoBehaviour
    {
        [Serializable] public sealed class Target
        {
            public string id, name; public float leaveLapS, rejoinLapS, raceSpeed, startBack = 35f;
            public List<Vector2> path = new List<Vector2>(); public List<Vector2> jumps = new List<Vector2>();
        }
        public List<Target> targets = new List<Target>();
        public string outputDir;
        public float[] speeds = { 12f, 14f, 16f, 18f, 20f, 22f, 24f, 26f, 28f, 30f, 33f, 36f };
        public float roadSpeed = 18f;
        float LatAcc = 14f; const float BrakeAcc = 14f;   // Round 10: -shortcutLatAcc <m/s2> for a driver who turns in faster


        ArcadeVehicle car; VehicleRespawn respawn; RaceController race; RaceRoute route;
        readonly StringBuilder report = new StringBuilder();
        readonly StringBuilder csv = new StringBuilder("shortcut,mode,speed,result,time_s,road_time_s,air_s,flights,longest_flight_s,longest_flight_m,peak_m,worst_landing_tilt_deg,touchdown_pitch_deg,house_hits\n");
        int fails, checks;

        // driving state
        List<Vector3> drive; int next; float want, look;
        bool active;
        readonly List<(string name, float speed)> contacts = new List<(string, float)>();

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); }

        IEnumerator Start()
        {
            Time.timeScale = 8f; Time.maximumDeltaTime = 1f;
            car = FindAnyObjectByType<ArcadeVehicle>(); respawn = car.GetComponent<VehicleRespawn>(); race = RaceController.Instance;
            car.gameObject.AddComponent<ContactProbe>().runner = this;
            yield return null; yield return null;
            route = race.Route;
            if (targets.Count == 0)   // Round 10, in the exe: the official shortcuts of the route the race uses
                foreach (var rs in route.Shortcuts)
                {
                    var t = new Target { id = rs.Id, name = rs.Name, leaveLapS = Mathf.Round(route.LapStationOf(rs.Start)), rejoinLapS = Mathf.Round(route.LapStationOf(rs.End)), startBack = rs.Id.Contains("over_house") ? 14f : 35f };
                    foreach (var p in rs.Path) t.path.Add(new Vector2(p.x, p.z));
                    targets.Add(t);
                }
            {
                var argv0 = Environment.GetCommandLineArgs();
                for (int i = 0; i + 1 < argv0.Length; i++) { if (argv0[i] == "-shortcutSpeeds") speeds = argv0[i + 1].Split(',').Select(float.Parse).ToArray(); if (argv0[i] == "-shortcutLatAcc") LatAcc = float.Parse(argv0[i + 1]); if (argv0[i] == "-shortcutLines") lines = argv0[i + 1].Split(','); }
            }
            car.enabled = false;   // this runner steps the car itself
            report.AppendLine($"Shortcut check, {DateTime.Now:yyyy-MM-dd HH:mm}. Route {route.Name}, lap {route.LapLength:0} m, {route.Shortcuts.Count} official shortcuts in the route file.");
            report.AppendLine("Each run starts on the lap before the turn-off at the given speed (35 m before for #2; 14 m for #6, just past the sharp CP4 corner, so the speed is the speed the car arrives with); times are to a line 20 m past the rejoin point. The road run uses the same start and speed.");
            report.AppendLine("Air: wheels all off the ground (flights shorter than 0.25 s are not counted as flights). Landing tilt: worst tilt from upright in the 1 s after a flight. Pitch at touchdown: negative = nose down.");
            report.AppendLine();
            var argv = Environment.GetCommandLineArgs();
            string only = null; bool noRace = false;
            for (int i = 0; i < argv.Length; i++) { if (argv[i] == "-shortcutOnly" && i + 1 < argv.Length) only = argv[i + 1]; if (argv[i] == "-shortcutNoRace") noRace = true; }
            foreach (var t in targets)
            {
                if (only != null && t.id != only) continue;
                report.AppendLine($"=== {t.name} ({t.id}): leaves the lap {t.leaveLapS:0} m after the gate, rejoins {t.rejoinLapS:0} m; runs start {t.startBack:0} m before the turn-off");
                var rows = new List<Trial>();
                foreach (float v in speeds)
                {
                    var road = new Trial(); yield return RunTrial(t, v, false, road);
                    foreach (var ln in lines)
                    {
                    line = ln;
                    var sc = new Trial(); yield return RunTrial(t, v, true, sc);
                    sc.roadTime = road.time; rows.Add(sc);
                    report.AppendLine($"  {v,4:0} m/s{(lines.Length > 1 ? $" {LineName(ln),-10}" : "")}  road {Fmt(road)}   shortcut {Fmt(sc)}");
                    if (sc.snags.Count > 0) report.AppendLine($"           snags: {string.Join("; ", sc.snags)}");
                    snagCsv.AppendLine($"{t.id},{VehicleName()},{v},{ln},{sc.result},{sc.snags.Count},\"{string.Join(" | ", sc.snags)}\"");
                    csv.AppendLine($"{t.id},shortcut,{v},{sc.result},{sc.time:0.00},{road.time:0.00},{sc.air:0.00},{sc.flights.Count},{(sc.flights.Count > 0 ? sc.flights.Max(f => f.dur) : 0):0.00},{(sc.flights.Count > 0 ? sc.flights.Max(f => f.dist) : 0):0.0},{(sc.flights.Count > 0 ? sc.flights.Max(f => f.peak) : 0):0.0},{sc.worstTilt:0},{sc.touchPitch:0},{string.Join(" ", sc.houseHits)}");
                    }
                    line = "0";
                }
                var clean = rows.Where(r => r.result == "clean").ToList();
                int bad = rows.Count(r => r.result != "clean");
                report.AppendLine($"  -> clean at {clean.Count} of {rows.Count} speeds ({string.Join(", ", clean.Select(r => r.speed.ToString("0")))} m/s); bad landings / crashes at {bad} ({string.Join(", ", rows.Where(r => r.result != "clean").Select(r => $"{r.speed:0}: {r.result}"))})");
                if (clean.Count > 0)
                {
                    var best = clean.OrderBy(r => r.time - r.roadTime).First();
                    report.AppendLine($"  -> best: {best.speed:0} m/s, {best.time:0.00} s against {best.roadTime:0.00} s on the road ({best.roadTime - best.time:+0.00;-0.00} s), air {best.air:0.00} s");
                    t.raceSpeed = best.speed;
                }
                Check($"{t.name}: taken well it beats the road", clean.Any(r => r.time < r.roadTime - 0.5f), clean.Count > 0 ? $"best gain {clean.Max(r => r.roadTime - r.time):0.00} s" : "no clean run");
                if (t.id.Contains("over_house"))
                {
                    var cleared = rows.Where(r => r.result == "clean" && !r.houseHits.Any(h => h.Contains("1641"))).Select(r => r.speed).ToList();
                    float lowest = cleared.Count > 0 ? cleared.Min() : -1f;
                    report.AppendLine($"  -> roof of house 1641 cleared at: {string.Join(", ", cleared.Select(x => x.ToString("0")))} m/s; lowest approach speed that clears it: {(lowest > 0 ? lowest.ToString("0") + " m/s" : "none")}");
                    foreach (var r in rows.Where(r => r.houseHits.Any(h => h.Contains("1641")) || r.result != "clean")) report.AppendLine($"     {r.speed:0} m/s: {Fmt(r)}");
                }
                report.AppendLine();
            }
            if (!noRace) yield return RaceLaps();
            report.AppendLine();
            report.AppendLine(fails == 0 ? $"ALL {checks} CHECKS PASSED" : $"{fails} OF {checks} CHECKS FAILED");
            Directory.CreateDirectory(outputDir);
            File.WriteAllText(Path.Combine(outputDir, "shortcut_check.txt"), report.ToString());
            File.WriteAllText(Path.Combine(outputDir, "shortcut_runs.csv"), csv.ToString());
            File.WriteAllText(Path.Combine(outputDir, "shortcut_snags.csv"), snagCsv.ToString());   // Round 16 (E)
            Debug.Log("[ShortcutCheck]\n" + report);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(fails == 0 ? 0 : 1);
#else
            Application.Quit(fails == 0 ? 0 : 1);   // Round 10: also runs in the built exe (-exeShortcut <dir>, GameShell)
#endif
        }

        // ---------------------------------------------------------------- one run

        sealed class Flight { public float dur, dist, peak; public Vector3 from, to; }
        sealed class Trial
        {
            public float speed, time, roadTime, air, worstTilt, touchPitch, roofGap = float.MaxValue; public string result = "timeout", endNote = "";
            public List<Flight> flights = new List<Flight>(); public List<string> houseHits = new List<string>();
            public List<string> ramps = new List<string>();   // Round 10: speed onto, on and off each ramp / deck / floor (item 2j)
            public List<string> snags = new List<string>();   // Round 16 (E): body contacts that took speed off, and sudden losses
        }

        static string Fmt(Trial t)
        {
            var sb = new StringBuilder();
            sb.Append(t.time > 0f ? $"{t.time,6:0.00} s {t.result}" : t.result);
            if (t.flights.Count > 0) sb.Append($" (air {t.air:0.0} s; longest flight {t.flights.Max(f => f.dur):0.00} s, {t.flights.Max(f => f.dist):0} m, {t.flights.Max(f => f.peak):0.0} m up; landing tilt {t.worstTilt:0} deg, pitch {t.touchPitch:0})");
            if (t.roofGap < 100f) sb.Append($" [roof clearance {t.roofGap:0.0} m]");
            if (t.houseHits.Count > 0) sb.Append($" [touched {string.Join(", ", t.houseHits)}]");
            if (t.endNote.Length > 0) sb.Append(" " + t.endNote);
            return sb.ToString();
        }

        List<Vector3> Lap(float s0, float s1) => route.SamplePath(route.GateS + s0, route.GateS + s1, 3f);
        static Vector3 G(Vector2 p) => RaceRoute.Ground(p.x, p.y);

        IEnumerator RunTrial(Target t, float v, bool shortcut, Trial tr)
        {
            tr.speed = v;
            FenceChunk.RestoreAll();
            var path = Lap(t.leaveLapS - t.startBack, t.leaveLapS);
            if (shortcut) { foreach (var p in t.path) path.Add(G(p)); path.AddRange(Lap(t.rejoinLapS, t.rejoinLapS + 40f)); path = OffsetLine(path); }
            else path.AddRange(Lap(t.leaveLapS, t.rejoinLapS + 40f));
            var start = route.PointAt(route.GateS + t.leaveLapS - t.startBack, out var f0);
            var finish = route.PointAt(route.GateS + t.rejoinLapS + 20f, out var f1);
            respawn.PlaceAt(start + Vector3.up * 0.3f, Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg);
            yield return new WaitForFixedUpdate();
            car.Body.linearVelocity = f0 * v;
            drive = path; next = 0; want = v; contacts.Clear(); active = true;
            curTrial = shortcut ? tr : null; float prevSpeed = v; braking = false;
            roofGap = float.MaxValue;
            trace = shortcut ? new StringBuilder("t,x,z,y,above_ground,speed,vy,wheels,pitch,roll,cte\n") : null; traceStep = 0; traceT = 0f;
            float time = 0f, slow = 0f, airRun = 0f, tiltWatch = 0f, lastFlightEnd = -1f; bool wasAir = false;
            Flight cur = null; float takeoffY = 0f;
            float prevSide = Vector3.Dot(car.transform.position - finish, f1);
            while (true)
            {
                yield return new WaitForFixedUpdate();
                float dt = Time.fixedDeltaTime; time += dt;
                var pos = car.transform.position;
                bool air = car.GroundedWheels == 0;
                if (air) { tr.air += dt; airRun += dt; if (cur == null) { cur = new Flight { from = pos }; takeoffY = pos.y; } cur.peak = Mathf.Max(cur.peak, pos.y - takeoffY); }
                else if (wasAir)
                {
                    if (airRun >= 0.25f && cur != null)
                    {
                        cur.dur = airRun; cur.to = pos; cur.dist = Vector2.Distance(new Vector2(cur.from.x, cur.from.z), new Vector2(pos.x, pos.z));
                        tr.flights.Add(cur); tiltWatch = 1f;
                        var fw = car.transform.forward; tr.touchPitch = Mathf.Asin(Mathf.Clamp(fw.y, -1f, 1f)) * Mathf.Rad2Deg;
                        lastFlightEnd = time;
                    }
                    airRun = 0f; cur = null;
                }
                wasAir = air;
                float tilt = Vector3.Angle(car.transform.up, Vector3.up);
                if (tiltWatch > 0f) { tr.worstTilt = Mathf.Max(tr.worstTilt, tilt); tiltWatch -= dt; }
                if (tilt > 75f) { tr.result = "rolled"; tr.endNote = $"at ({pos.x:0},{pos.z:0})"; break; }
                // Round 16 (E): a sudden loss of speed (more than 1.5 m/s in one step, about 7 g) while not braking
                float spNow = car.Body.linearVelocity.magnitude;
                if (shortcut && !braking && prevSpeed - spNow > 1.5f && tr.snags.Count < 12) tr.snags.Add($"LOSS {prevSpeed:0.0}->{spNow:0.0} m/s at ({pos.x:0.0},{pos.z:0.0}) {pos.y - RaceRoute.Ground(pos.x, pos.z).y:0.0} m up, {car.GroundedWheels} wheels down, over {Under(pos)}");
                prevSpeed = spNow;
                slow = car.Body.linearVelocity.magnitude < 1.5f ? slow + dt : 0f;
                if (slow > 2.5f) { tr.result = "stuck"; tr.endNote = $"stopped at ({pos.x:0},{pos.z:0}), {pos.y - RaceRoute.Ground(pos.x, pos.z).y:0.0} m above the ground"; break; }
                if (DistToPath(pos) > 25f) { tr.result = "lost"; tr.endNote = $"{DistToPath(pos):0} m off the line at ({pos.x:0},{pos.z:0})"; break; }
                if (time > 60f) { tr.result = "timeout"; break; }
                float side = Vector3.Dot(pos - finish, f1);
                if (prevSide < 0f && side >= 0f && Vector3.Distance(Flat(pos), Flat(finish)) < 20f) { tr.time = time; tr.result = "finished"; break; }
                prevSide = side;
            }
            active = false; curTrial = null;
            tr.roofGap = roofGap;
            if (trace != null) { foreach (var c in contacts) if (c.speed > 4f && !c.name.StartsWith("rode on")) trace.AppendLine($"# contact {c.name} {c.speed:0.0}");
                Directory.CreateDirectory(Path.Combine(outputDir, "traces")); File.WriteAllText(Path.Combine(outputDir, "traces", $"{t.id}_{v:0}.csv"), trace.ToString()); trace = null; }
            foreach (var c in contacts) if (IsHouse(c.name)) tr.houseHits.Add(c.name.StartsWith("rode on ") ? Short(c.name) : $"{Short(c.name)} at {c.speed:0} m/s");
            tr.houseHits = tr.houseHits.Distinct().ToList();
            if (rampName != null) CloseRamp(tr);
            tr.ramps.AddRange(rampLog); rampLog.Clear();
            if (shortcut && tr.ramps.Count > 0) report.AppendLine($"           ramps at {v:0} m/s: {string.Join("; ", tr.ramps)}");
            if (tr.result == "finished")
            {
                bool badLanding = tr.worstTilt > 40f;
                bool hardHit = contacts.Any(c => IsHouse(c.name) && (c.speed > 5f || c.name.StartsWith("rode on ")));
                tr.result = !shortcut ? "finished" : badLanding ? "bad landing" : hardHit ? "hit a house" : "clean";
                if (!shortcut) tr.result = "clean";
            }
            yield return null;
        }

        // a house's own collider (not the shortcut's ramps, whose names mention the house they serve)
        static bool IsHouse(string n) => n.Contains("HouseColliders/") || n.Contains("(open: shortcut passage)") || n.Contains("Jumped house");
        static string Short(string n) => n.Replace("HouseColliders/", "").Replace(" (open: shortcut passage)/box", " (passage walls)").Replace("Neighborhood/Jumped house 1641 roof collider", "House 1641 roof");

        float DistToPath(Vector3 p)
        {
            float best = float.MaxValue;
            int a = Mathf.Max(0, next - 12), b = Mathf.Min(drive.Count - 1, next + 12);
            for (int i = a; i < b; i++)
            {
                Vector3 s = Flat(drive[i]), e = Flat(drive[i + 1]), q = Flat(p); var ab = e - s;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(q - s, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, (s + ab * t - q).magnitude);
            }
            return best;
        }

        // Steering: aim along the line a few metres ahead and correct for the sideways error from it (a cross-track term,
        // so a car coming out of a bend settles on the line before a passage rather than cutting toward it).
        void FixedUpdate()
        {
            if (!active || drive == null) return;
            var pos = car.transform.position; float v = Mathf.Max(car.ForwardSpeed, 1f);
            // nearest segment ahead of the last one
            int best = next; float bd = float.MaxValue;
            for (int i = next; i < Mathf.Min(drive.Count - 1, next + 15); i++)
            {
                Vector3 a = Flat(drive[i]), b = Flat(drive[i + 1]), q = Flat(pos), ab = b - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(q - a, ab) / ab.sqrMagnitude);
                float d = (a + ab * t - q).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            next = best;
            Vector3 sa = Flat(drive[next]), sb = Flat(drive[Mathf.Min(next + 1, drive.Count - 1)]);
            Vector3 segDir = (sb - sa).sqrMagnitude > 1e-6f ? (sb - sa).normalized : Flat(car.transform.forward).normalized;
            float cte = Vector3.Dot(Flat(pos) - sa, Vector3.Cross(Vector3.up, segDir));   // + = right of the line
            float la = (look > 0f ? look : 4f) + 0.12f * v;
            Vector3 aim = Flat(drive[next]); float acc = 0f; int k = next;
            while (k < drive.Count - 1 && acc < la) { acc += (Flat(drive[k + 1]) - Flat(drive[k])).magnitude; k++; }
            aim = Flat(drive[k]);
            Vector3 aimDir = (aim - Flat(pos)).normalized;
            Vector3 lineDir = (aim - sa).sqrMagnitude > 1e-4f ? (aim - sa).normalized : segDir;
            float headErr = Vector3.SignedAngle(Flat(car.transform.forward), lineDir, Vector3.up);
            float corr = -Mathf.Atan2(1.6f * cte, v + 2f) * Mathf.Rad2Deg;
            float angle = Vector3.SignedAngle(Flat(car.transform.forward), aimDir, Vector3.up);
            float steer = Mathf.Clamp((headErr + corr) / 18f, -1f, 1f), speed = car.ForwardSpeed;
            // speed: the target, but no faster than the bends ahead allow (a driver who brakes for a bend in time):
            // v <= sqrt(latAcc * radius) at each bend, reachable from here with brakeAcc of braking
            float w = want;
            {
                // the bend radius at each point ahead, from the points 6 m before and after it
                var ahead = new List<Vector3>(); float acc2 = 0f; ahead.Add(Flat(pos));
                for (int i = next + 1; i < drive.Count && acc2 < 70f; i++) { acc2 += (Flat(drive[i]) - ahead[ahead.Count - 1]).magnitude; ahead.Add(Flat(drive[i])); }
                var cum = new float[ahead.Count]; for (int i = 1; i < ahead.Count; i++) cum[i] = cum[i - 1] + (ahead[i] - ahead[i - 1]).magnitude;
                Vector3 At(float d) { int i = 1; while (i < cum.Length - 1 && cum[i] < d) i++; return Vector3.Lerp(ahead[i - 1], ahead[i], Mathf.InverseLerp(cum[i - 1], cum[i], d)); }
                for (float d = 6f; d < cum[cum.Length - 1] - 6f; d += 2f)
                {
                    Vector3 a = At(d - 6f), b = At(d), c = At(d + 6f);
                    float area2 = Mathf.Abs(Vector3.Cross(b - a, c - a).y);
                    if (area2 < 1e-3f) continue;
                    float r = (b - a).magnitude * (c - b).magnitude * (c - a).magnitude / (2f * area2);
                    float vb = Mathf.Sqrt(LatAcc * r);
                    w = Mathf.Min(w, Mathf.Sqrt(vb * vb + 2f * BrakeAcc * Mathf.Max(0f, d - 6f)));
                }
            }
            braking = speed > w + 1.5f;
            car.Simulate(speed < w ? 1f : 0f, speed > w + 1.5f ? Mathf.Clamp01((speed - w) / 4f) : 0f, steer, Time.fixedDeltaTime);
            // clearance over the jumped house's roof: the gap from the car's underside to the roof below it
            foreach (var hh in Physics.RaycastAll(pos, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore))
                if (hh.collider.name.Contains("Jumped house")) roofGap = Mathf.Min(roofGap, hh.distance - 0.3f);
            // Round 10 (item 2j): which shortcut ramp / deck the wheels are on (rays down from the four wheel points)
            string onRamp = null;
            foreach (var wp in car.suspensionPoints)
            {
                var o = car.transform.TransformPoint(wp);
                if (Physics.Raycast(o, -car.transform.up, out var wh, car.suspensionLength + 0.3f, ~0, QueryTriggerInteraction.Ignore) && wh.collider.name.StartsWith("sc")) { onRamp = wh.collider.name; break; }
            }
            float sp = car.Body.linearVelocity.magnitude;
            if (onRamp != null && onRamp != rampName) { if (rampName != null) CloseRamp(null); rampName = onRamp; rampIn = sp; rampMin = sp; rampTop = pos.y; rampOff = sp; rampOffY = pos.y; }
            else if (onRamp != null) { rampMin = Mathf.Min(rampMin, sp); rampOff = sp; rampOffY = pos.y; rampTop = Mathf.Max(rampTop, pos.y); }
            else if (rampName != null && car.GroundedWheels == 0) { rampAir += Time.fixedDeltaTime; if (rampAir > 0.15f) CloseRamp(null); }
            else if (rampName != null) CloseRamp(null);
            if (onRamp != null) rampAir = 0f;
            // riding on a house (the wheels on a roof, not the body hitting it)
            if (car.GroundedWheels > 0 && Physics.Raycast(pos - car.transform.up * 0.4f, Vector3.down, out var rh, 2.0f, ~0, QueryTriggerInteraction.Ignore))   // from under the car's own box
            {
                var rn = rh.collider.transform.parent ? rh.collider.transform.parent.name + "/" + rh.collider.name : rh.collider.name;
                if (IsHouse(rn)) contacts.Add(("rode on " + rn, 0f));
            }
            if (trace != null && (traceStep++ % 3) == 0)
            {
                var gy = RaceRoute.Ground(pos.x, pos.z).y; var e = car.transform.eulerAngles;
                trace.AppendLine($"{traceT:0.00},{pos.x:0.0},{pos.z:0.0},{pos.y:0.00},{pos.y - gy:0.00},{car.Body.linearVelocity.magnitude:0.0},{car.Body.linearVelocity.y:0.0},{car.GroundedWheels},{Mathf.DeltaAngle(0, e.x):0},{Mathf.DeltaAngle(0, e.z):0},{cte:0.0}");
            }
            traceT += Time.fixedDeltaTime;
        }
        StringBuilder trace; int traceStep; float traceT; float roofGap = float.MaxValue;
        string rampName; float rampIn, rampMin, rampOff, rampTop, rampOffY, rampAir; readonly List<string> rampLog = new List<string>();
        void CloseRamp(Trial tr)
        {
            string shortName = rampName.Replace(" collider", "");
            rampLog.Add($"{shortName}: on {rampIn:0.0}, lowest {rampMin:0.0}, off {rampOff:0.0} m/s ({rampOff / Mathf.Max(rampIn, 0.1f) * 100f:0}% of the speed it arrived with)");
            rampName = null; rampAir = 0f;
        }

        public void OnContact(Collision c)
        {
            if (!active) return;
            var n = c.collider.name; var t = c.collider.transform;
            string full = t.parent ? t.parent.name + "/" + n : n;
            Snag(c, "hit");
            contacts.Add((full + $" @({car.transform.position.x:0.0},{car.transform.position.z:0.0})", c.relativeVelocity.magnitude));
            if (trace != null && c.contactCount > 0) { var cp = c.GetContact(0); trace.AppendLine($"# enter impulse {c.impulse.magnitude / car.Body.mass:0.0} m/s"); trace.AppendLine($"# body contact {full} at {traceT:0.00}s, rel {c.relativeVelocity.magnitude:0.0} m/s, normal ({cp.normal.x:0.00},{cp.normal.y:0.00},{cp.normal.z:0.00}), local point ({car.transform.InverseTransformPoint(cp.point).x:0.00},{car.transform.InverseTransformPoint(cp.point).y:0.00},{car.transform.InverseTransformPoint(cp.point).z:0.00}), speed {car.Body.linearVelocity.magnitude:0.0}"); }
        }

        // Round 10 (item 2j): any contact that takes more than 2 m/s off the car in one step, with its collider and normal
        public void OnStay(Collision c)
        {
            if (active) Snag(c, "stay");
            if (!active || trace == null) return;
            float dv = c.impulse.magnitude / car.Body.mass;
            if (dv < 2f || c.contactCount == 0) return;
            var cp = c.GetContact(0); var t = c.collider.transform;
            trace.AppendLine($"# stay {(t.parent ? t.parent.name + "/" : "")}{c.collider.name} at {traceT:0.00}s, impulse {dv:0.0} m/s, normal ({cp.normal.x:0.00},{cp.normal.y:0.00},{cp.normal.z:0.00}), local point ({car.transform.InverseTransformPoint(cp.point).x:0.00},{car.transform.InverseTransformPoint(cp.point).y:0.00},{car.transform.InverseTransformPoint(cp.point).z:0.00}), {c.contactCount} points, speed {car.Body.linearVelocity.magnitude:0.0}");
        }

        // Round 16 (E): -shortcutLines drives the shortcut off its line: "1.5" = 1.5 m left of it all the way (negative: right);
        // "a3" = 3 m left at the run's start, onto the line by the kicker's foot (into it at an angle out of the corner before).
        // "0" = on the line (the default). Snags: body contacts that push against the motion with more than 1 m/s, and losses.
        string[] lines = { "0" }; string line = "0"; bool braking; Trial curTrial;
        readonly StringBuilder snagCsv = new StringBuilder("shortcut,vehicle,speed,line,result,snags,details\n");
        static string LineName(string ln) => ln == "0" ? "on line" : ln.StartsWith("a") ? $"angle {ln.Substring(1)}" : $"offset {ln}";
        string VehicleName() { var cfg = car.GetComponent<VehicleConfiguration>(); return cfg && cfg.Profile != null ? cfg.Profile.Id : car.name; }
        List<Vector3> OffsetLine(List<Vector3> path)
        {
            if (line == "0") return path;
            bool angle = line.StartsWith("a"); float off = float.Parse(angle ? line.Substring(1) : line);
            var c = new float[path.Count]; for (int i = 1; i < path.Count; i++) c[i] = c[i - 1] + Vector3.Distance(Flat(path[i]), Flat(path[i - 1]));
            // the kicker's foot: the path point nearest the kicker's low end
            float footS = c[c.Length - 1] * 0.25f;
            var kick = FindObjectsByType<MeshCollider>().FirstOrDefault(m => m.name.StartsWith("sc") && m.name.Contains("kicker") && !m.name.Contains("heap"));
            if (kick)
            {
                var b = kick.bounds;
                int foot = 0; float bd = float.MaxValue;
                for (int i = 0; i < path.Count; i++) { float d = (Flat(path[i]) - Flat(b.center)).sqrMagnitude; if (d < bd) { bd = d; foot = i; } }
                footS = Mathf.Max(1f, c[foot] - b.extents.magnitude * 0.6f);
            }
            var o = new List<Vector3>();
            for (int i = 0; i < path.Count; i++)
            {
                var d = Flat(path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized; var left = Vector3.Cross(d, Vector3.up);
                float k = angle ? Mathf.Clamp01(1f - c[i] / footS) : 1f;
                var q = path[i] + left * off * k; o.Add(G(new Vector2(q.x, q.z)));
            }
            return o;
        }
        string Under(Vector3 pos)
        {
            var hits = Physics.RaycastAll(pos + Vector3.up * 0.5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore).Where(h => h.collider.attachedRigidbody == null).OrderBy(h => h.distance).ToArray();
            return hits.Length > 0 ? (hits[0].collider.transform.parent ? hits[0].collider.transform.parent.name + "/" : "") + hits[0].collider.name : "nothing";
        }
        void Snag(Collision c, string kind)
        {
            if (curTrial == null || curTrial.snags.Count >= 12 || c.contactCount == 0) return;
            float dv = c.impulse.magnitude / car.Body.mass;
            if (dv < 1.0f) return;
            var cp = c.GetContact(0); var vel = car.Body.linearVelocity;
            float against = vel.sqrMagnitude > 1f ? -Vector3.Dot(cp.normal, vel.normalized) : 0f;   // how much the push opposes the motion
            if (against < 0.25f) return;   // a push up (the ground carrying the body) is not a snag
            var t = c.collider.transform; var lp = car.transform.InverseTransformPoint(cp.point);
            curTrial.snags.Add($"{kind.ToUpper()} {(t.parent ? t.parent.name + "/" : "")}{c.collider.name} {dv:0.0} m/s at ({car.transform.position.x:0.0},{car.transform.position.z:0.0}) normal ({cp.normal.x:0.00},{cp.normal.y:0.00},{cp.normal.z:0.00}) body point ({lp.x:0.00},{lp.y:0.00},{lp.z:0.00}) speed {vel.magnitude:0.0}");
        }

        sealed class ContactProbe : MonoBehaviour
        {
            public ShortcutCheckRunner runner;
            void OnCollisionEnter(Collision c) { if (runner) runner.OnContact(c); }
            void OnCollisionStay(Collision c) { if (runner) runner.OnStay(c); }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        // ---------------------------------------------------------------- races

        // Round 13, item 3: -shortcutRaceOffset id:metres,... drives the race lap that far LEFT of a shortcut's line (negative:
        // right) at its entry plane, easing on over the 20 m before and off over the 25 m after (Dan's line round house 1763)
        List<Vector2> Offset(Target t, List<Vector2> line)
        {
            float off = 0f; var argv = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < argv.Length; i++)
                if (argv[i] == "-shortcutRaceOffset")
                    foreach (var kv in argv[i + 1].Split(',')) { var p = kv.Split(':'); if (p.Length == 2 && p[0] == t.id) off = float.Parse(p[1]); }
            if (off == 0f) return line;
            var rs = route.Shortcuts.First(x => x.Id == t.id);
            var e = new Vector2(rs.Entry.Position.x, rs.Entry.Position.z);
            var c = new float[line.Count]; for (int i = 1; i < line.Count; i++) c[i] = c[i - 1] + Vector2.Distance(line[i], line[i - 1]);
            int ie = 0; for (int i = 1; i < line.Count; i++) if (Vector2.Distance(line[i], e) < Vector2.Distance(line[ie], e)) ie = i;
            var o = new List<Vector2>();
            for (int i = 0; i < line.Count; i++)
            {
                var d = (line[Mathf.Min(i + 1, line.Count - 1)] - line[Mathf.Max(i - 1, 0)]).normalized; var left = new Vector2(-d.y, d.x);
                float ds = c[i] - c[ie];
                float k = ds < 0f ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-20f, 0f, ds)) : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(25f, 0f, ds));
                o.Add(line[i] + left * off * k);
            }
            report.AppendLine($"  race lap: {t.id} driven {Mathf.Abs(off):0.0} m {(off > 0f ? "left" : "right")} of its line at the entry plane");
            return o;
        }

        IEnumerator RaceLaps()
        {
            report.AppendLine("=== Race laps through the RaceController (1 lap each, same road speed)");
            RaceController.RecordsPathOverride = Path.Combine(outputDir, "race_records_shortcut_check.json");
            RaceController.LapsOverride = 1;
            RaceController.EntryPlanesOff = Environment.GetCommandLineArgs().Contains("-shortcutNoEntryPlanes");   // Round 13
            if (RaceController.EntryPlanesOff) report.AppendLine("  (-shortcutNoEntryPlanes: the entry planes are off, a shortcut counts only by its corridor)");
            float plain = 0f;
            for (int mode = 0; mode < 2; mode++)
            {
                var events = new List<string>();
                Action<string> log = e => events.Add(e);
                race.Logged += log;
                race.StartRace();
                yield return null;
                var r = race.Route;
                var path = new List<Vector3>(); var looks = new List<float>(); var speedsAt = new List<float>();
                void Add(IEnumerable<Vector3> pts, float sp, float lk) { foreach (var p in pts) { path.Add(p); speedsAt.Add(sp); looks.Add(lk); } }
                var li = race.RouteData.lead_in;
                var lead = new List<Vector3>(); for (int i = 0; i + 1 < li.Length; i += 2) lead.Add(RaceRoute.Ground(li[i], li[i + 1]));
                Add(lead, roadSpeed, 9f);
                if (mode == 0) Add(r.SamplePath(0f, r.GateS + r.LapLength + 40f, 4f), roadSpeed, 9f);
                else
                {
                    float s = 0f;
                    foreach (var t in targets.OrderBy(x => x.leaveLapS))
                    {
                        Add(r.SamplePath(s, r.GateS + t.leaveLapS, 4f), roadSpeed, 9f);
                        Add(Offset(t, t.path).Select(G), t.raceSpeed > 0 ? t.raceSpeed : roadSpeed, 7f);
                        s = r.GateS + t.rejoinLapS;
                    }
                    Add(r.SamplePath(s, r.GateS + r.LapLength + 40f, 4f), roadSpeed, 9f);
                }
                while (race.State == RaceController.Stage.Countdown) yield return new WaitForFixedUpdate();
                drive = path; next = 0; active = true;
                float t0 = Time.time;
                while (race.State == RaceController.Stage.Racing && Time.time - t0 < 300f)
                {
                    int k = Mathf.Min(next, speedsAt.Count - 1); want = speedsAt[k]; look = looks[k];
                    if (next >= path.Count - 1) break;
                    yield return new WaitForFixedUpdate();
                }
                active = false; look = 0f;
                race.Logged -= log;
                bool finished = race.State == RaceController.Stage.Finished;
                float lap = race.LapTimes.Count > 0 ? race.LapTimes[0] : 0f;
                report.AppendLine($"  {(mode == 0 ? "road lap" : "lap by both shortcuts")}: {(finished ? RaceController.Fmt(lap) : "not finished, state " + race.State)}; penalties {race.PenaltyTotal:0} s");
                foreach (var e in events) report.AppendLine("      " + e);
                if (mode == 0) { plain = lap; Check("road lap finishes with no penalty", finished && race.PenaltyTotal == 0f, RaceController.Fmt(lap)); }
                else
                {
                    foreach (var t in targets) Check($"race: {t.name} entered and its checkpoint forgiven", events.Any(e => e.Contains("SHORTCUT " + t.id + " entered") && e.Contains("forgiven")), events.FirstOrDefault(e => e.Contains("SHORTCUT " + t.id)) ?? "no event");
                    Check("race: no penalty on the shortcut lap", finished && race.PenaltyTotal == 0f, $"penalty {race.PenaltyTotal:0} s");
                    Check("race: the shortcut lap is faster than the road lap", finished && lap < plain, $"{RaceController.Fmt(lap)} against {RaceController.Fmt(plain)}");
                }
                race.EndRace();
                yield return new WaitForFixedUpdate();
                car.enabled = false;
            }
            RaceController.LapsOverride = 0;
        }
    }
}
