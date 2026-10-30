using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 85 in the exe (-exeRound85 <dir> [-r85Only p85lap,p85wall] [-r85GlanceOff] -r20Minutes 60 -logFile <dir>\Player.log), from the repo root,
    // on the check's own save. CHECK IT AS DAN PLAYS IT. -r85GlanceOff: WallGlance off (the before).
    //   p85lap   A: one HIGH MODE lap each of the Kennesaw Mountain trails, the Noonday Creek Extension and the Chastain Glen loop by the human-like driver
    //            steering into the edges on purpose (Round 84's pushes); stalls against a bumper (4 s barely moving with the throttle on) and glances;
    //            pictures of the first glance off a bumper (at it, 0.3 s and 0.9 s after)
    //   p85wall  B: normal mode, the Drifter Twin into the I-75 Express Lanes' wall and into a race's closure barrier at 10, 20, 30 and 70 degrees
    //            through the game's input (throttle held, hands off the wheel): speed before / after and heading against the wall 0.6 s after the hit;
    //            pictures of the 20 degree hit on each
    public sealed partial class Round20Runner
    {
        IEnumerator R85Lap()
        {
            string mode = WallGlance.Off ? "WallGlance OFF (before)" : "WallGlance on";
            int totalStalls = 0;
            foreach (var (file, veh, cap) in new[] { ("kennesaw_mountain.json", "drifter", 32f), ("noonday_creek.json", "atv", 20f), ("chastain_glen_loop.json", "drifter", 26f) })
            {
                report.AppendLine(); report.AppendLine($"=== A. One HIGH MODE lap of {file} by the human-like driver, steering into the edges on purpose ({mode})");
                R84High(true); yield return Vehicle(veh);
                yield return StartRoute(file, "forward", 0, 1);
                var r = race.Route; var road = race.AiRoad;
                Check($"lap {file}: HIGH MODE with its bumpers up", race.High && HighBumpers.Shown, $"high {race.High}, {HighBumpers.Report}");
                var line = new List<Vector3>(); var rd = race.RouteData;
                if (rd.lead_in != null) for (int i = 0; i + 1 < rd.lead_in.Length; i += 2) line.Add(RaceRoute.Ground(rd.lead_in[i], rd.lead_in[i + 1]));
                float L = r.LapLength; var lap = r.SamplePath(0f, r.GateS + L + 30f, 4f); var zones = race.AiZones; int pushes = 0;
                bool NearJump(float s)
                {
                    if (zones != null) foreach (var z in zones) if (Mathf.Repeat(s - (z.s0 - 30f), L) <= z.s1 - z.s0 + 60f) return true;
                    for (float d = -20f; d <= 40f; d += 10f) if (LandingZone.Contains(r.PointAt(s + d, out _))) return true;
                    return false;
                }
                // the same pushes as Round 84: 20 m aimed 7 m off the middle in every 150 m, alternate sides, never near a jump
                for (int i = 0; i < lap.Count; i++)
                {
                    float s = i * 4f; int block = Mathf.FloorToInt(s / 150f); float into = s - block * 150f; var p = lap[i];
                    if (block > 0 && into > 60f && into < 80f && !NearJump(block * 150f + 60f)) { r.PointAt(s, out var f); p += Vector3.Cross(Vector3.up, f).normalized * (block % 2 == 0 ? 1f : -1f) * 7f; if (into < 64f) pushes++; }
                    line.Add(p);
                }
                int g0 = WallGlance.Glances, p0 = WallGlance.Pins, c0 = WallGlance.Crashes, log0 = WallGlance.Log.Count; bool shot = false;
                var run = new HumanRun(); float worst = 0f; Vector3 worstAt = Vector3.zero; string tag = file.Replace(".json", "");
                yield return HumanDrive(line, cap, 480f, run, () =>
                {
                    var p = car.Body.position; road.Project(p, out float lat); if (Mathf.Abs(lat) > worst) { worst = Mathf.Abs(lat); worstAt = p; }
                    if (!shot && WallGlance.Glances > g0 && !WallGlance.Off) { shot = true; StartCoroutine(R85Series($"85_lap_{tag}_glance")); }
                    return race.Player.CompletedLaps >= 1;
                });
                var bumpers = FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(cc => HighBumpers.IsBumper(cc) && cc.gameObject.activeInHierarchy).ToList();
                int atBumper = 0; var stuckLines = new List<string>();
                foreach (System.Text.RegularExpressions.Match mm in System.Text.RegularExpressions.Regex.Matches(run.note, @"stuck at \((-?\d+), (-?\d+)\)"))
                {
                    var sp = new Vector3(float.Parse(mm.Groups[1].Value), 0f, float.Parse(mm.Groups[2].Value));
                    float nb = bumpers.Count == 0 ? -1f : bumpers.Min(cc => cc.sharedMesh.vertices.Min(v => Flat(cc.transform.TransformPoint(v) - sp).magnitude));
                    if (nb >= 0 && nb < 3f) atBumper++;
                    stuckLines.Add($"    stuck at ({sp.x:0}, {sp.z:0}): the nearest bumper {nb:0.0} m");
                }
                totalStalls += atBumper;
                report.AppendLine($"  {run.time:0} s, top {run.topSpeed:0} m/s, resets {run.resets}, stuck {run.stuck}; {pushes} pushes; farthest from the line {worst:0.0} m at ({worstAt.x:0}, {worstAt.z:0}); glances {WallGlance.Glances - g0}, pinned and turned {WallGlance.Pins - p0}, head on {WallGlance.Crashes - c0}");
                foreach (var l in stuckLines) report.AppendLine(l);
                foreach (var l in WallGlance.Log.Skip(log0).Take(12)) report.AppendLine("    " + l);
                report.AppendLine($"  trace:{run.trace}");
                Check($"lap {file}: the HIGH lap is completed steering into the edges", race.Player.CompletedLaps >= 1, $"laps {race.Player.CompletedLaps}");
                Check($"lap {file}: no stall against a bumper (aim 0)", atBumper == 0, $"{atBumper} stalls at a bumper, {run.stuck} in all");
                race.EndRace(); R84High(false);
            }
            report.AppendLine($"  stalls against a bumper over the three laps: {totalStalls} ({mode}); Round 84's build: Kennesaw 6, Noonday Creek Extension 9");
        }
        // three pictures from the player's camera: at the hit, 0.3 s and 0.9 s after
        IEnumerator R85Series(string name)
        {
            yield return Capture(name + "_0"); yield return new WaitForSeconds(.3f); yield return Capture(name + "_1"); yield return new WaitForSeconds(.6f); yield return Capture(name + "_2");
        }

        // ---------------------------------------------------------------- wall: the Express Lanes' wall and a closure barrier, normal mode
        IEnumerator R85Wall()
        {
            report.AppendLine(); report.AppendLine($"=== B. Normal mode: the Drifter Twin into a wall at 10, 20, 30 and 70 degrees, throttle held, hands off ({(WallGlance.Off ? "WallGlance OFF (before)" : "WallGlance on")})");
            R84High(false); yield return Vehicle("drifter"); R73Pad();
            if (race.State != RaceController.Stage.Free) race.EndRace();
            // the Express Lanes' wall: a straight stretch of one of its pieces, the Express road on one side
            Vector3 face = default, n = default, t = default; bool found = false; var roads = RoadLookup.Instance;
            // a vertex of a wall piece, a point 4 m out from it on the Express road, a ray back to the face; straight if rays 15 m either way along it
            // meet the same plane within 0.5 m
            foreach (var mc in FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c => c.name.StartsWith("Express Lanes wall collider")).OrderBy(c => c.name))
            {
                var vs = mc.sharedMesh.vertices;
                for (int vi = 0; vi < vs.Length && !found; vi += 7)
                {
                    var v = mc.transform.TransformPoint(vs[vi]);
                    for (int k = 0; k < 8 && !found; k++)
                    {
                        var dir8 = Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward; var q = v + dir8 * 4f;
                        if (!roads || !roads.Nearest(q, out string nm, out float dd, out float hh) || !nm.Contains("Express") || dd > hh - 1f) continue;
                        var g = RaceRoute.Surface(q.x, q.z); var from = new Vector3(q.x, g.y + .7f, q.z);
                        if (!mc.Raycast(new Ray(from, -dir8), out var hit, 6f) || Mathf.Abs(hit.normal.y) > .3f) continue;
                        var nn = Flat(hit.normal).normalized; if (Vector3.Dot(nn, dir8) < 0) nn = -nn; var tt = Vector3.Cross(Vector3.up, nn);
                        bool straight = true;
                        foreach (float off in new[] { -15f, 15f })
                        {
                            var q2 = hit.point + nn * 3f + tt * off; var g2 = RaceRoute.Surface(q2.x, q2.z); q2.y = g2.y + .7f;
                            if (!Physics.Raycast(q2, -nn, out var h2, 6f, ~0, QueryTriggerInteraction.Ignore) || !h2.collider.name.StartsWith("Express Lanes wall") || Mathf.Abs(Vector3.Dot(Flat(h2.point - hit.point), nn)) > .5f) straight = false;
                            if (!roads.Nearest(q2 - nn * 0f + nn * 3f, out string n2, out float d2, out float h2w) || !n2.Contains("Express")) straight = false;
                        }
                        if (!straight) continue;
                        face = hit.point; n = nn; t = tt; found = true;
                        report.AppendLine($"  the Express Lanes' wall: {mc.name}, the face at ({face.x:0.0}, {face.z:0.0}), along {Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg:0} degrees");
                    }
                }
                if (found) break;
            }
            Check("wall: a straight stretch of the Express Lanes' wall found", found, "");
            if (found) yield return R85Hits("Express Lanes wall", face, n, t, 28f, 30f);
            // a race's closure barrier (the Chastain Glen loop): its longest unbroken face, from the course's side
            yield return StartRoute("chastain_glen_loop.json", "forward", 0, 1);
            var walls = race.Barriers.GetComponentsInChildren<BoxCollider>().Where(b => b.name == "Barrier wall").ToList();
            int allRuns = walls.Count;
            // a closure across a side road (not a pipe mouth), the course's road in front of it for the run-in
            walls = walls.Where(b => b.transform.parent.name.StartsWith("Barrier ") && b.size.x >= 10f && roads && R85OnRoad(roads, b.transform.position - Flat(b.transform.forward).normalized * 8f)).OrderByDescending(b => b.size.x).ToList();
            Check("wall: the race's barriers are one collider per run (Barrier wall)", allRuns > 0, $"{allRuns} runs, {walls.Count} closures 10 m+ with the road in front");
            // the first closure whose run-ins (every angle, either way along it) start on a road
            bool picked = false;
            foreach (var bw in walls)
            {
                var along = Flat(bw.transform.forward).normalized; var bface = bw.transform.position - along * (bw.size.z * .5f); bface.y = RaceRoute.Surface(bface.x, bface.z).y + .7f;
                foreach (float sgn in new[] { 1f, -1f })
                {
                    var lat = Flat(bw.transform.right).normalized * sgn; var nn = -along; bool ok = true;
                    foreach (float ang in new[] { 10f, 20f, 30f }) { var st = R85Start(bface, nn, lat, ang, 10f); foreach (float f in new[] { 0f, .5f }) if (!R85OnRoad(roads, Vector3.Lerp(st, bface + nn * 1.3f, f))) ok = false; }
                    if (!ok) continue;
                    report.AppendLine($"  the barrier: {bw.transform.parent.name}, {bw.size.x:0.0} m long, its face at ({bface.x:0.0}, {bface.z:0.0})");
                    yield return R85Hits("closure barrier", bface, nn, lat, 18f, 10f); picked = true; break;
                }
                if (picked) break;
            }
            Check("wall: a closure barrier with every run-in on the road", picked, $"{walls.Count} candidates");
            race.EndRace();
        }
        static Vector3 R85Start(Vector3 face, Vector3 n, Vector3 t, float angle, float back)
        {
            var dir = (t * Mathf.Cos(angle * Mathf.Deg2Rad) - n * Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;   // the steep ones start closer (on the road)
            return face + n * 1.3f - dir * Mathf.Min(back, 9f / Mathf.Sin(angle * Mathf.Deg2Rad));
        }
        static bool R85OnRoad(RoadLookup roads, Vector3 p) => roads.Nearest(p, out _, out float d, out float h) && d < h;
        // one hit per angle: placed back from the face, heading into it at the angle, the speed given, throttle held; measured 0.6 s after the first touch
        IEnumerator R85Hits(string what, Vector3 face, Vector3 n, Vector3 t, float speed, float back)
        {
            foreach (float angle in new[] { 10f, 20f, 30f, 70f })
            {
                var dir = (t * Mathf.Cos(angle * Mathf.Deg2Rad) - n * Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;
                var start = R85Start(face, n, t, angle, back); start = RaceRoute.Surface(start.x, start.z);
                Place(start, dir, speed); R73Set(0f, 1f, 0f);
                var cam = Camera.main ? Camera.main.GetComponent<ChaseCamera>() : null; if (cam) cam.Snap();
                float tt = 0f, touched = -1f, before = 0f; bool pics = angle == 20f; int pic = 0; string tag = what.Replace(" ", "_").Replace("'", "") + (WallGlance.Off ? "_before" : "");
                float after = 0f, headErr = 0f, gap = 0f;
                while (tt < 4f)
                {
                    yield return new WaitForFixedUpdate(); tt += Time.fixedDeltaTime;
                    var p = car.Body.position; float dist = Vector3.Dot(Flat(p - face), n);
                    if (touched < 0f) { before = Flat(car.Body.linearVelocity).magnitude; if (dist < 1.3f) touched = tt; }
                    if (pics && touched >= 0f && pic == 0) { pic++; yield return Capture($"85_{tag}_20deg_0"); }
                    if (pics && touched >= 0f && pic == 1 && tt > touched + .3f) { pic++; yield return Capture($"85_{tag}_20deg_1"); }
                    if (touched >= 0f && tt >= touched + .6f && after == 0f)
                    {
                        after = Flat(car.Body.linearVelocity).magnitude; headErr = Vector3.Angle(Flat(car.transform.forward), t); gap = dist;
                    }
                    if (pics && after > 0f && pic == 2 && tt > touched + .9f) { pic++; yield return Capture($"85_{tag}_20deg_2"); }
                    if (after > 0f && (!pics || pic >= 3)) break;
                }
                R73Set(0f, 0f, 0f);
                bool glanced = angle <= 30f;
                report.AppendLine($"  {what} at {angle:0} degrees: {before:0.0} m/s before, {after:0.0} m/s 0.6 s after the hit ({(before > 0 ? after / before * 100f : 0):0} %), heading {headErr:0} degrees off the wall's line, {gap:0.0} m from it{(touched < 0 ? " (never touched)" : "")}");
                if (glanced) Check($"wall: {what} at {angle:0} degrees: slides along, speed kept (80 % or more), straightened (heading within 10 degrees)", touched >= 0 && after >= before * .8f && headErr <= 10f, $"{after / Mathf.Max(before, .1f) * 100f:0} %, {headErr:0} degrees");
                else Check($"wall: {what} at {angle:0} degrees (head on): it stops you, as before", touched >= 0 && after < before * .5f, $"{after:0.0} of {before:0.0} m/s");
                yield return new WaitForSeconds(.3f);
            }
        }
    }
}
