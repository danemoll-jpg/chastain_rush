using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 60 in the exe (-exeRound60 <dir> [-r60Only mallin,smash,mallfps,overpass,pics60] [-r20Vehicles ids] -logFile <dir>\Player.log):
    //   mallin    Town Center driven through on every vehicle, three ways: in at the main (north) doors, along the lower concourse, up the east escalator
    //             bank, through the east anchor's store and out its east doors; in at the south doors (upper), along the upper concourse, off the court's
    //             edge into the center court (the drop), out the south-west doors; in at the main doors, up the west bank, along the north-west wing
    //             and out its west doors. Through or not, stuck (wedged) or not, the slowest, the longest in the air
    //   smash     a sample of every kind of breakable (glass, wood, metal, signs, soft), each driven into at 12 m/s: broken, the speed kept
    //   mallfps   the frame rate parked inside (lower concourse, upper concourse, the court, the food court) and in the lot, the window's size
    //   overpass  Chastain Road over I-75 (BUG-001), both carriageways, both ways, every vehicle, flat out: the sharpest vertical jolt (change of the
    //             body's vertical speed in one step), the longest with no wheel down, from 60 m before each deck end to 60 m past it
    //   pics60    the outside from the main entrance and the east anchor, each level, the court, the food court, the store, the overpass
    public sealed partial class Round20Runner
    {
        TownCenterMall MallNow => TownCenterMall.Current;
        List<Vector3> MallLine(params (float u, float v, int level)[] pts) { var m = MallNow; return pts.Select(p => m.P(p.u, p.v, p.level == 0 ? m.lowerFloor : p.level == 1 ? m.upperFloor : RaceRoute.Surface(m.P(p.u, p.v, 0).x, m.P(p.u, p.v, 0).z).y)).ToList(); }

        IEnumerator R60MallIn()
        {
            report.AppendLine(); report.AppendLine("=== Town Center driven through (every vehicle, three ways, up to 22 m/s inside)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var m = MallNow; if (!m) { Check("Town Center is built", false); yield break; }
            var ways = new (string name, List<Vector3> line)[]
            {
                ("main doors -> lower concourse -> east escalators -> the east anchor's store -> its east doors", MallLine((-51f, -1232f, 2), (-51f, -1262f, 0), (-51f, -1296f, 0), (-38f, -1311.5f, 0), (-18f, -1311.5f, 0), (-6f, -1304.5f, 0), (6f, -1303.5f, 0), (44f, -1303.5f, 1), (60f, -1303.5f, 1), (155f, -1303.5f, 1), (163.5f, -1303f, 1), (192f, -1303f, 2))),
                ("south doors -> upper concourse -> off the court's edge -> south-west doors", MallLine((10f, -1362f, 2), (10f, -1330f, 1), (6f, -1314f, 1), (-6f, -1304.5f, 1), (-80f, -1303.5f, 1), (-104f, -1303.5f, 0), (-122f, -1305f, 0), (-140f, -1313.2f, 0), (-178f, -1313.2f, 0), (-195f, -1313.6f, 0), (-195f, -1322f, 0), (-195f, -1342f, 0), (-195f, -1368f, 2))),
                ("main doors -> lower concourse west -> west escalators -> north-west wing -> its west doors", MallLine((-51f, -1232f, 2), (-51f, -1262f, 0), (-58f, -1293.0f, 0), (-80f, -1293.0f, 0), (-130f, -1293.0f, 0), (-140f, -1303.5f, 0), (-150f, -1303.5f, 0), (-185.5f, -1303.5f, 1), (-192f, -1299f, 1), (-195f, -1288f, 1), (-195f, -1250f, 1), (-205f, -1242f, 1), (-224f, -1242f, 1), (-250f, -1242f, 2))),
            };
            var ids = VehicleIds();
            foreach (var vid in ids)
            {
                yield return Vehicle(vid);
                foreach (var (name, line) in ways)
                {
                    m.RestoreBreakables(); yield return new WaitForSecondsRealtime(.2f);
                    Place(line[0], line[1] - line[0], 6f); yield return new WaitForFixedUpdate();
                    var run = new Run(); float len = 0f; for (int i = 1; i < line.Count; i++) len += Flat(line[i] - line[i - 1]).magnitude;
                    int broke0 = FenceChunk.BrokenPanels; float lowest = 99f; var ct = car.GetComponent<ContactTracer>() ?? car.gameObject.AddComponent<ContactTracer>(); ct.Take(); string lastHit = ""; Vector3 hitAt = Vector3.zero; float lastV = 0f;
                    yield return DriveLine(line, 22f, len / 6f + 25f, run, null, r =>
                    {
                        lowest = Mathf.Min(lowest, car.Body.position.y - m.lowerFloor); float v = Flat(car.Body.linearVelocity).magnitude; string touch = ct.Take();
                        if (lastV - v > 3f && touch.Length > 0) { lastHit = touch.Split(' ')[0]; hitAt = car.Body.position; }   // a hard stop: what it met
                        lastV = v; return false;
                    }, 5f, false);
                    int broke = FenceChunk.BrokenPanels - broke0; var end = car.Body.position; var eu = new Vector2(Vector2.Dot(new Vector2(end.x, end.z), m.frameU), Vector2.Dot(new Vector2(end.x, end.z), m.frameV));
                    report.AppendLine($"  {Nm(vid),-16} {name}: {(run.reached ? "through" : "NOT through: " + run.end + $" at u {eu.x:0.0} v {eu.y:0.0} y {end.y:0.0}")} in {run.time:0.0} s; slowest {run.minSpeed:0.0} m/s; longest in the air {run.maxAir:0.0} s; {broke} things broken; lowest {lowest:0.0} m over the lower floor{(lastHit.Length > 0 ? $"; the last hard stop: {lastHit} at ({hitAt.x:0.0}, {hitAt.y:0.0}, {hitAt.z:0.0})" : "")}");
                    Check($"mallin {Nm(vid)}: {name.Split(' ')[0]} {name.Split(' ')[1]} ... through, never stuck", run.reached && run.end != "stopped" && lowest > -1.5f, run.reached ? $"{run.time:0.0} s" : run.end);
                }
            }
        }
        string[] VehicleIds()
        {
            var argv = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(argv, "-r20Vehicles");
            return i >= 0 && i + 1 < argv.Length ? argv[i + 1].Split(',') : VehicleProfile.All.Select(p => p.Id).ToArray();
        }

        IEnumerator R60Smash()
        {
            report.AppendLine(); report.AppendLine("=== Town Center's breakables: a sample of each kind driven into at 12 m/s (Street Classic): broken, the speed kept");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var m = MallNow; if (!m) yield break;
            yield return Vehicle("original");
            foreach (var kind in new[] { "MallGlass", "MallWood", "MallMetal", "MallSigns", "MallSoft" })
            {
                var chunks = m.breakables.Where(c => c && c.transform.parent && c.transform.parent.name == kind).ToList();
                var panels = chunks.SelectMany(c => c.GetComponentsInChildren<FencePanel>().Select(p => (c, p))).ToList();
                int tried = 0;
                foreach (int pick in new[] { 0, panels.Count / 3, 2 * panels.Count / 3, panels.Count - 1 }.Distinct())
                {
                    if (pick < 0 || pick >= panels.Count || tried >= 3) continue;
                    var (chunk, panel) = panels[pick]; var box = panel.GetComponent<BoxCollider>(); var c = panel.transform.TransformPoint(box.center);
                    float floor = c.y - box.size.y * .5f;
                    // a clear way in: from 7 m out along one of its four sides
                    Vector3 dir = Vector3.zero;
                    foreach (var d in new[] { panel.transform.forward, -panel.transform.forward, panel.transform.right, -panel.transform.right })
                    {
                        var from = new Vector3(c.x, floor + .9f, c.z); var dd = Flat(d).normalized; var side = Vector3.Cross(Vector3.up, dd) * 1.1f;
                        bool clear = true;   // room for the car's width both ways: 8 m to come in, 8 m past it
                        foreach (var o in new[] { Vector3.zero, side, -side }) if (Physics.Raycast(from + o, dd, 8f, ~0, QueryTriggerInteraction.Ignore) || Physics.Raycast(from + o, -dd, 8f, ~0, QueryTriggerInteraction.Ignore)) clear = false;
                        if (clear) { dir = dd; break; }
                    }
                    if (dir == Vector3.zero) continue;
                    tried++; m.RestoreBreakables(); yield return new WaitForSecondsRealtime(.3f);
                    var start = new Vector3(c.x, floor, c.z) + dir * 7f;
                    Place(start, -dir, 12f); yield return new WaitForFixedUpdate(); (car.GetComponent<ContactTracer>() ?? car.gameObject.AddComponent<ContactTracer>()).Take();
                    car.enabled = false; float t = 0f, before = -1f, after = -1f; bool broken = false; float brokeAt = -1f;
                    while (t < 2.2f)
                    {
                        yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                        car.Simulate(car.ForwardSpeed < 12f ? 1f : 0f, 0f, 0f, Time.fixedDeltaTime);
                        float dist = Flat(car.Body.position - c).magnitude; float v = Flat(car.Body.linearVelocity).magnitude;
                        if (before < 0f && dist < 3.2f) before = v;
                        if (!broken && chunk.broken != null && chunk.broken.Length > panel.index && chunk.broken[panel.index]) { broken = true; brokeAt = t; }
                        if (broken && t > brokeAt + .35f && after < 0f) after = v;
                    }
                    car.enabled = true;
                    var cte = car.GetComponent<ContactTracer>(); string met = cte ? string.Join(" ", cte.Take().Split(' ').Where(x => x.StartsWith("enter")).Select(x => x.Split(':')[1]).Distinct()) : "";
                    report.AppendLine($"  {kind} #{pick} at ({c.x:0.0}, {c.y:0.0}, {c.z:0.0}) from {(Vector3.Dot(dir, m.P(1, 0, 0) - m.P(0, 0, 0)) > .7f ? "+u" : Vector3.Dot(dir, m.P(1, 0, 0) - m.P(0, 0, 0)) < -.7f ? "-u" : Vector3.Dot(dir, m.P(0, 1, 0) - m.P(0, 0, 0)) > 0 ? "+v" : "-v")}: {(broken ? "broken" : "NOT broken")}; {before:0.0} m/s before, {after:0.0} m/s 0.35 s after{(met.Length > 0 ? "; met " + met : "")}");
                    Check($"smash {kind} #{pick}: broken, the speed kept (95 %)", broken && after >= before * .95f - .2f, $"{before:0.0} -> {after:0.0} m/s");
                }
                if (tried == 0) Check($"smash {kind}: a sample with a clear way in", false, $"{panels.Count} panels");
            }
            m.RestoreBreakables();
        }

        IEnumerator R60Fps()
        {
            report.AppendLine(); report.AppendLine($"=== Frame rate in and by Town Center, uncapped, parked, chase camera, {Screen.width} x {Screen.height} (Round 59 at Town Center: 171 fps average, 51 slowest 1 %, 1920 x 1080 window; not measured at 3840 x 2160)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var m = MallNow; if (!m) yield break;
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            yield return Vehicle("original");
            foreach (var (name, at, look) in new[] { ("lower concourse", m.P(-30f, -1311.5f, m.lowerFloor), m.P(-120f, -1303.5f, m.lowerFloor)), ("upper concourse", m.P(-60f, -1311.5f, m.upperFloor), m.P(-200f, -1303.5f, m.upperFloor)),
                ("the center court", m.P(-75f, -1304f, m.lowerFloor), m.P(-130f, -1303.5f, m.lowerFloor)), ("the food court", m.P(-70f, -1290f, m.upperFloor), m.P(-120f, -1270f, m.upperFloor)),
                ("the lot by the main doors", m.P(-51f, -1225f, 0f), m.P(-51f, -1300f, 0f)) })
            {
                var p = name.StartsWith("the lot") ? RaceRoute.Surface(at.x, at.z) : at;
                respawn.PlaceAt(p + Vector3.up * .4f, Mathf.Atan2(look.x - at.x, look.z - at.z) * Mathf.Rad2Deg); race.NotifyTeleported();
                yield return new WaitForSecondsRealtime(4f);
                var frames = new List<float>(); float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 10f) { yield return null; frames.Add(Time.unscaledDeltaTime); }
                var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * .99f))];
                report.AppendLine($"INFO  {name}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps (the inside {(m.Shown ? "drawn" : "hidden")})");
                yield return Capture("60_fps_" + new string(name.Where(char.IsLetterOrDigit).Take(24).ToArray()));
            }
            // the inside hidden from far off
            respawn.PlaceAt(RaceRoute.Surface(m.P(-51f, -880f, 0f).x, m.P(-51f, -880f, 0f).z) + Vector3.up * .4f, 0f); race.NotifyTeleported();
            yield return new WaitForSecondsRealtime(2f);
            Check("mallfps: the inside not drawn from 250 m away", !m.Shown, m.Shown ? "drawn" : "hidden");
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
        }

        IEnumerator R60Overpass()
        {
            report.AppendLine(); report.AppendLine("=== Chastain Road over I-75 (BUG-001, Dan at x -1051.61 z 420.74): both carriageways, both ways, flat out (up to 45 m/s)");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            // each carriageway's line (map.json's road pieces and decks), west to east
            var eb = Pts(-1120f, 391f, -1078f, 402f, -1056f, 408f, -1029.9f, 414.5f, -988.9f, 425.2f, -936.6f, 438.5f, -907f, 446f, -870f, 456f);
            var wb = Pts(-1141f, 406f, -1103f, 416f, -1081f, 422f, -1035.5f, 433.8f, -1008.4f, 440.7f, -941.8f, 458.1f, -907f, 467f, -872f, 476f);
            for (int i = 0; i < eb.Count; i++) eb[i] = RaceRoute.Surface(eb[i].x, eb[i].z); for (int i = 0; i < wb.Count; i++) wb[i] = RaceRoute.Surface(wb[i].x, wb[i].z);
            var ways = new (string, List<Vector3>)[] { ("eastbound, eastward", eb), ("eastbound, westward", Enumerable.Reverse(eb).ToList()), ("westbound, westward", Enumerable.Reverse(wb).ToList()), ("westbound, eastward", wb) };
            float worstAll = 0f; string worstWhere = "";
            foreach (var vid in VehicleIds())
            {
                yield return Vehicle(vid);
                foreach (var (name, line) in ways)
                {
                    Place(line[0], line[1] - line[0], 30f); yield return new WaitForFixedUpdate();
                    var run = new Run(); float lastVy = car.Body.linearVelocity.y, worst = 0f, air = 0f, airMost = 0f; Vector3 worstAt = Vector3.zero; float t = 0f;
                    yield return DriveLine(line, 45f, 25f, run, null, r =>
                    {
                        t += Time.fixedDeltaTime; var p = car.Body.position; float vy = car.Body.linearVelocity.y, jolt = Mathf.Abs(vy - lastVy); lastVy = vy;
                        bool zone = p.x > -1110f && p.x < -880f;
                        if (zone && t > .8f) { if (jolt > worst) { worst = jolt; worstAt = p; } air = car.GroundedWheels == 0 ? air + Time.fixedDeltaTime : 0f; airMost = Mathf.Max(airMost, air); }
                        return false;
                    }, 6f, false);
                    report.AppendLine($"  {Nm(vid),-16} {name}: the sharpest jolt {worst:0.00} m/s in one step at ({worstAt.x:0.0}, {worstAt.z:0.0}); longest with no wheel down {airMost:0.00} s; {(run.reached ? "through" : run.end)}");
                    if (worst > worstAll) { worstAll = worst; worstWhere = $"{Nm(vid)} {name} at ({worstAt.x:0}, {worstAt.z:0})"; }
                    Check($"overpass {Nm(vid)} {name}: no wheels-off over 0.15 s, no jolt over 1.5 m/s", airMost <= .15f && worst <= 1.5f, $"{worst:0.00} m/s, {airMost:0.00} s");
                }
            }
            report.AppendLine($"INFO  the sharpest jolt of all: {worstAll:0.00} m/s ({worstWhere})");
        }

        IEnumerator R60Pics()
        {
            report.AppendLine(); report.AppendLine("=== Pictures");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            var m = MallNow; if (!m) yield break;
            Vector3 G(float u, float v, float h) { var p = m.P(u, v, 0f); return RaceRoute.Surface(p.x, p.z) + Vector3.up * h; }
            // park the car out of the way, near the main doors (the inside is drawn near it)
            respawn.PlaceAt(G(-51f, -1215f, .4f), 180f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f);
            yield return Still("60_outside_main_entrance", G(-55f, -1222f, 5f), m.P(-51f, -1267f, m.lowerFloor + 8f), 70f);
            yield return Still("60_outside_from_the_north_west", G(-150f, -1120f, 14f), m.P(-90f, -1250f, m.lowerFloor + 6f), 70f);
            yield return Still("60_outside_east_anchor", G(240f, -1330f, 10f), m.P(163f, -1303f, m.upperFloor + 5f), 60f);
            yield return Still("60_outside_south_doors", G(-5f, -1395f, 8f), m.P(10f, -1339f, m.upperFloor + 4f), 60f);
            yield return Still("60_outside_from_the_south_west", G(-260f, -1470f, 25f), m.P(-120f, -1320f, m.lowerFloor + 8f), 65f);
            yield return Still("60_lower_concourse", m.P(-20f, -1312f, m.lowerFloor + 2.2f), m.P(-120f, -1300f, m.lowerFloor + 2f), 70f);
            yield return Still("60_upper_concourse", m.P(-40f, -1312.5f, m.upperFloor + 2.2f), m.P(-190f, -1300f, m.upperFloor + 1f), 70f);
            yield return Still("60_center_court", m.P(-85f, -1313f, m.upperFloor + 3.5f), m.P(-110f, -1300f, m.lowerFloor), 75f);
            yield return Still("60_food_court", m.P(-72f, -1293f, m.upperFloor + 2.5f), m.P(-115f, -1255f, m.upperFloor + 1f), 75f);
            yield return Still("60_east_escalators", m.P(-10f, -1310f, m.lowerFloor + 2f), m.P(40f, -1303.5f, m.upperFloor), 70f);
            yield return Still("60_east_anchor_store", m.P(58f, -1306f, m.upperFloor + 2.2f), m.P(150f, -1303f, m.upperFloor + 1f), 70f);
            yield return Still("60_play_area", m.P(-100f, -1293f, m.lowerFloor + 2.5f), m.P(-122f, -1281f, m.lowerFloor), 70f);
            yield return Still("60_north_wing", m.P(3f, -1288f, m.lowerFloor + 2f), m.P(3f, -1210f, m.lowerFloor + 2f), 70f);
            respawn.PlaceAt(RaceRoute.Surface(-1060f, 412f) + Vector3.up * .4f, 75f); race.NotifyTeleported(); yield return new WaitForSecondsRealtime(2f);
            yield return Still("60_overpass_west_end", RaceRoute.Surface(-1075f, 405f) + Vector3.up * 2.2f, RaceRoute.Surface(-1010f, 421f) + Vector3.up, 60f);
        }
    }
}
