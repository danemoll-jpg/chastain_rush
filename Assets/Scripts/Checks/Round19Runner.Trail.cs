using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    public sealed partial class Round19Runner
    {
        // ---------------------------------------------------------------- 1. the wider trail, breakable trees

        IEnumerator Trail()
        {
            report.AppendLine(); report.AppendLine("=== 1. Noonday Creek trail: the trunks beside it, a breakable tree");
            UseRoute("noonday_creek.json", "forward");
            var field = FindAnyObjectByType<TreeField>();
            float L = race.Route.LapLength;
            var pts = new List<Vector3>(); for (float s = 0f; s < L; s += 1f) pts.Add(race.Route.PointAt(s, out _));
            var grid = new Dictionary<Vector2Int, List<Vector3>>();
            foreach (var p in pts) { var k = new Vector2Int(Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f)); if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<Vector3>(); l.Add(p); }
            int solid35 = 0, breakable6 = 0, solid6 = 0; float nearestSolid = 99f; Vector3 nearestAt = default; int snapIndex = -1; float snapD = 99f;
            for (int i = 0; i < field.count; i++)
            {
                var t = TreeField.Unpack(field.data, i); var q = new Vector3(t.x, 0f, t.z); float best = 99f;
                int cx = Mathf.FloorToInt(t.x / 8f), cz = Mathf.FloorToInt(t.z / 8f);
                for (int x = cx - 1; x <= cx + 1; x++) for (int z = cz - 1; z <= cz + 1; z++) if (grid.TryGetValue(new Vector2Int(x, z), out var l)) foreach (var p in l) best = Mathf.Min(best, Flat(p - q).magnitude);
                if (best > 6.5f) continue;
                bool br = field.IsBreakable(i);
                if (br)
                {
                    breakable6++;
                    bool clearOfJumps = BigJump.All.All(j => Flat(j.lip - q).magnitude > 60f) && Flat(new Vector3(2494.5f, 0f, 774.5f) - q).magnitude > 90f;   // and J4
                    if (best > 3.6f && best < 4.5f && best < snapD && clearOfJumps) { snapD = best; snapIndex = i; }
                }
                else { if (best < 3.5f) solid35++; if (best < 6f) solid6++; if (best < nearestSolid) { nearestSolid = best; nearestAt = q; } }
            }
            Check("no solid trunk within 3.5 m of the trail's line", solid35 == 0, $"{solid35} solid within 3.5 m; nearest solid {nearestSolid:0.0} m (at {nearestAt.x:0}, {nearestAt.z:0}); {breakable6} breakable trees within 6 m, {solid6} solid ones 3.5 to 6 m (none should be: all left within 6 m are breakable)");
            if (snapIndex < 0) { Check("a breakable tree to drive into", false); yield break; }
            yield return Vehicle("atv");
            var tp = TreeField.Unpack(field.data, snapIndex); var target = new Vector3(tp.x, tp.y, tp.z);
            var from = race.Route.PointAt(AbsStation(target) - 16f, out _);   // on the line 16 m before it, then straight at it
            Place(from, target - from, 15f); yield return new WaitForFixedUpdate();
            int snapped0 = TreeTrunks.Snapped; float v0 = 0f, vAfter = 0f; bool shot = false;
            car.enabled = false;
            for (float t = 0f; t < 3f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                var aim = Vector3.SignedAngle(Flat(car.transform.forward), Flat(target - car.Body.position), Vector3.up);
                car.Simulate(car.ForwardSpeed < 15f ? 1f : 0f, 0f, TreeTrunks.Snapped > snapped0 ? 0f : Mathf.Clamp(aim / 15f, -1f, 1f), Time.fixedDeltaTime);
                if (TreeTrunks.Snapped == snapped0) v0 = Flat(car.Body.linearVelocity).magnitude;
                else if (!shot)
                {
                    shot = true;
                    for (int k = 0; k < 15; k++) { yield return new WaitForFixedUpdate(); car.Simulate(1f, 0f, 0f, Time.fixedDeltaTime); }   // 0.3 s on
                    vAfter = Flat(car.Body.linearVelocity).magnitude;
                    car.enabled = true; yield return new WaitForSecondsRealtime(0.4f); yield return Capture("1_tree_snaps"); car.enabled = false;
                    break;
                }
            }
            car.enabled = true;
            var around = Physics.OverlapSphere(target + Vector3.up * 1f, 1.5f, ~0, QueryTriggerInteraction.Collide);
            report.AppendLine($"    the tree at ({target.x:0.0}, {target.z:0.0}); the vehicle ended at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0}), {Flat(car.Body.position - target).magnitude:0.0} m from it; colliders there: {string.Join(", ", around.Select(c => $"{c.name} trigger {c.isTrigger} enabled {c.enabled}"))}");
            Check("a vehicle into a breakable tree snaps it and drives on (a little speed lost)", TreeTrunks.Snapped > snapped0 && vAfter > 8f, $"snapped {TreeTrunks.Snapped - snapped0}, {v0:0.0} m/s before, {vAfter:0.0} m/s 0.3 s after (the ATV held at 15)");
            race.StartRace(); race.EndRace();   // a race start puts the snapped trees back (FenceChunk.AllRestored)
        }

        // ---------------------------------------------------------------- 2. the big jumps

        IEnumerator Jumps(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== 2. The big jumps: each trail vehicle from 130 m before the lip, flat out (lifting only for bends); then one slow");
            UseRoute("noonday_creek.json", "forward");
            var jumps = BigJump.All.Where(j => j.look != "kids_ramp").OrderBy(j => j.id).ToList();
            Check("four big jumps on the trail", jumps.Count == 4, string.Join(", ", jumps.Select(j => j.id + " " + j.look)));
            var small = Small.Where(ids.Contains).ToArray(); if (small.Length == 0) small = Small;
            var bad = new List<string>();
            float launch = -1f; BigJump launchedAt = null;
            System.Action<BigJump, ArcadeVehicle, float> hook = (j, c, v) => { if (c == car) { launch = v; launchedAt = j; } };
            BigJump.Launched += hook;
            foreach (var j in jumps)
            {
                float sLip = AbsStation(j.lip);
                var line = LapSlice(sLip - 130f, sLip + 95f);
                bool shot = false; var airs = new List<float>();
                foreach (var id in small)
                {
                    yield return Vehicle(id);
                    Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                    launch = -1f; launchedAt = null; float top = -99f, flight = 0f, jumpAir = 0f; Vector3 landAt = new Vector3(float.NaN, 0f, 0f); float upAt = -1f;
                    var run = new Run(); var drive = StartCoroutine(DriveLine(line, 99f, 45f, run, j.lip, null, 4f));
                    while (!run.reached && run.end == "" && run.time < 44.9f)
                    {
                        if (launchedAt == j && float.IsNaN(landAt.x))   // this jump's own flight: from the lip to the first two wheels down
                        {
                            top = Mathf.Max(top, car.Body.position.y - j.lip.y); flight += Time.deltaTime;
                            if (car.GroundedWheels >= 2 && flight > 0.3f) { landAt = car.Body.position; upAt = car.transform.up.y; jumpAir = flight; }
                        }
                        if (!shot && launchedAt == j && car.GroundedWheels == 0 && Vector3.Dot(Flat(car.Body.position - j.lip), j.dir) > 8f) { shot = true; yield return Capture($"2_{j.id}_{j.look}_in_the_air"); }
                        yield return null;
                    }
                    yield return drive;
                    float dist = float.IsNaN(landAt.x) ? -1f : Vector3.Dot(Flat(landAt - j.lip), j.dir);
                    run.maxAir = jumpAir; run.upAtLanding = upAt;
                    bool ok = run.reached && launchedAt == j && jumpAir >= 1.0f && upAt > 0.7f;
                    if (!ok) { bad.Add($"{j.id} {Nm(id)}"); File.WriteAllLines(Path.Combine(outputDir, $"jump_{j.id}_{id}.csv"), run.trail.Select(q => $"{q.x:0.00},{q.y:0.00},{q.z:0.00}")); }
                    airs.Add(run.maxAir);
                    report.AppendLine($"  {j.id} {j.look,-11} {Nm(id),-16} off the lip at {launch,5:0.0} m/s{(launch > j.vmax ? $" (held to {j.vmax:0})" : "")}, {run.maxAir:0.00} s in the air, down {dist,5:0.0} m past the lip, top {top,4:0.0} m over it, upright {run.upAtLanding:0.00}, {(run.reached ? "drove on" : "DID NOT GET ON " + run.end)}");
                }
                Check($"{j.id} ({j.look}): every trail vehicle over it, two seconds of air or more at race speed, on its wheels, and on", bad.Count(b => b.StartsWith(j.id)) == 0 && airs.Min() >= 1.9f, $"air {airs.Min():0.00} to {airs.Max():0.00} s");
                // slow: 12 m/s off the lip, lands short on drivable ground and drives on
                yield return Vehicle("atv");
                var slow = LapSlice(sLip - 40f, sLip + 95f);
                Place(slow[0], slow[2] - slow[0], 10f);
                var r2 = new Run(); launch = -1f; launchedAt = null;
                yield return DriveLine(slow, 12f, 40f, r2, j.lip, null, 4f);
                float d2 = float.IsNaN(r2.firstLanding.x) ? -1f : Vector3.Dot(Flat(r2.firstLanding - j.lip), j.dir);
                Check($"{j.id} slow (the Trail Four held at 12 m/s): lands short and drives on, no wall, no reset", r2.reached, $"off the lip at {launch:0.0} m/s, down {d2:0.0} m past it, {(r2.reached ? "on along the trail" : "STOPPED " + r2.end + $" at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0})")}");
                // a picture from the approach (the chase camera set aside), the vehicle parked out of the way
                car.Body.isKinematic = true; car.Body.position += Vector3.down * 50f;
                var app = j.lip - j.dir * 34f; app = RaceRoute.Ground(app.x, app.z) + Vector3.up * 2.2f;
                yield return Still($"2_{j.id}_{j.look}_from_the_approach", app, j.lip + j.dir * 18f + Vector3.up * 1.0f, 62f);
                car.Body.isKinematic = false;
            }
            BigJump.Launched -= hook;
        }

        // ---------------------------------------------------------------- 3. the rock bar to P1

        IEnumerator Bar()
        {
            report.AppendLine(); report.AppendLine("=== 3. BUG-001: the rock bar across Noonday Creek into P1");
            UseRoute("noonday_creek.json", "forward");
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "p1_pipe"); var pipe = FindAnyObjectByType<DrainPipe>();
            var dry = FindAnyObjectByType<DryPatch>();
            Check("the rock bar is there (a dry patch over the creek in front of the inlet)", dry != null && sc != null && pipe != null);
            if (dry == null || sc == null || pipe == null) yield break;
            var mouth = pipe.floor[0];
            var line = sc.Path.TakeWhile(p => Flat(p - mouth).magnitude > 1.5f || p == sc.Path[0]).ToList(); line.Add(mouth); line.Add(pipe.floor[8]);
            var results = new List<string>(); bool allDry = true; bool shot = false;
            foreach (var id in Small)
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 14f); yield return new WaitForFixedUpdate();
                var run = new Run(); float minOnBar = 99f, wade = 0f; bool onBar = false; Vector3 slowAt = default; string touching = "";
                var drive = StartCoroutine(DriveLine(line, 16f, 25f, run, null, null, 3f));
                while (!run.reached && run.end == "" && run.time < 24.9f)
                {
                    if (DryPatch.Contains(car.Body.position) && Flat(car.Body.linearVelocity).magnitude < minOnBar) { slowAt = car.Body.position; touching = string.Join("/", Physics.OverlapBox(car.Body.position, new Vector3(1.2f, 1f, 1.8f), car.transform.rotation, ~0, QueryTriggerInteraction.Ignore).Where(c => c.attachedRigidbody != car.Body).Select(c => c.name).Distinct()); }
                    if (DryPatch.Contains(car.Body.position)) { onBar = true; minOnBar = Mathf.Min(minOnBar, Flat(car.Body.linearVelocity).magnitude); wade = Mathf.Max(wade, car.WaterImmersion); if (!shot) { shot = true; yield return Capture("3_rock_bar_into_P1"); } }
                    yield return null;
                }
                yield return drive;
                if (!onBar || wade > 0.01f || !run.reached) allDry = false;
                if (onBar && minOnBar < 8f) allDry = false;
                results.Add($"{Nm(id)} {(onBar ? $"slowest on the bar {minOnBar:0.0} m/s (at {slowAt.x:0.0}, {slowAt.y:0.00}, {slowAt.z:0.0}, touching {touching}), wading {wade:0.00}" : "NOT ON THE BAR")}, {(run.reached ? "into the pipe" : "STOPPED")}");
            }
            Check("every trail vehicle crosses on the bar dry (no water drag) at speed into the inlet", allDry, string.Join("; ", results));
            car.Body.isKinematic = true; car.Body.position += Vector3.down * 50f;
            var at = (line[0] + line[1]) * 0.5f; var stand = at + Flat(at - mouth).normalized * 6f + Vector3.up * 2.5f;
            yield return Still("3_rock_bar_from_the_trail", stand, mouth + Vector3.up * 1.5f, 60f);
            car.Body.isKinematic = false;
        }

        // ---------------------------------------------------------------- 4. the pipe: through, ride the walls, the camera, a reset

        IEnumerator Pipe(string[] ids)
        {
            report.AppendLine(); report.AppendLine("=== 4. BUG-002: P1 inside (used and dark, a stream), riding its walls");
            UseRoute("noonday_creek.json", "forward");
            var pipe = FindAnyObjectByType<DrainPipe>(); var stream = FindAnyObjectByType<PipeStream>();
            Check("the pipe is round with a stream down it", pipe != null && pipe.barrelTo - pipe.barrelFrom > 40f && stream != null, pipe != null ? $"barrel {(pipe.barrelTo - pipe.barrelFrom) * 0.5f:0.0} m, radius {pipe.radius:0.0} m, stream {(stream ? "yes" : "no")}" : "no pipe");
            if (pipe == null) yield break;
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "p1_pipe");
            var f = pipe.floor; int n = f.Length;
            { var q = f[n / 2]; bool hit = Physics.Raycast(q + Vector3.up * 30f, Vector3.down, out var h, 60f, ~0, QueryTriggerInteraction.Ignore);
              Check("over the middle of the pipe the ground is whole", hit && h.collider is TerrainCollider && h.point.y > q.y + 5f, hit ? $"{h.collider.name} at {h.point.y:0.00}, the floor {q.y:0.00} below" : "nothing"); }
            // through, along the line, each vehicle
            var line = sc.Path.ToList(); var stuck = new List<string>(); float slowest = 99f;
            foreach (var id in ids)
            {
                yield return Vehicle(id);
                Place(line[0], line[2] - line[0], 12f); yield return new WaitForFixedUpdate();
                var run = new Run(); float inPipeMin = 99f;
                var drive = StartCoroutine(DriveLine(line, 16f, 40f, run, null, null, 3f));
                while (!run.reached && run.end == "" && run.time < 39.9f)
                {
                    if (DrainPipe.Inside(car.Body.position, out float st) != null && st > pipe.barrelFrom + 4 && st < pipe.barrelTo - 4) inPipeMin = Mathf.Min(inPipeMin, Flat(car.Body.linearVelocity).magnitude);
                    yield return null;
                }
                yield return drive;
                if (!run.reached) stuck.Add(Nm(id));
                slowest = Mathf.Min(slowest, inPipeMin);
                report.AppendLine($"  through: {Nm(id),-18} {(run.reached ? "through" : "DID NOT GET THROUGH " + run.end)} in {run.time:0.0} s, slowest in the pipe {inPipeMin:0.0} m/s (held to 16: the stream slows nobody)");
            }
            Check("every vehicle drives through P1 on its line, inlet to outlet", stuck.Count == 0, stuck.Count > 0 ? "not: " + string.Join(", ", stuck) : $"{ids.Length} of {ids.Length}, slowest {slowest:0.0} m/s in the pipe");
            // ride the walls: into the barrel at 14 m/s along it, steering up the wall to 60 degrees off the axis and holding that (a
            // helix round the pipe); then straight on out. The furthest round, the turns, back on its wheels at the bottom, out
            var bad = new List<string>(); float camOut = 0f; string camWorst = ""; bool shotWall = false, shotIn = false; int circles = 0;
            var fcum = new float[n]; for (int i = 1; i < n; i++) fcum[i] = fcum[i - 1] + Flat(f[i] - f[i - 1]).magnitude;
            foreach (var id in ids)
            {
                yield return Vehicle(id);
                int k0 = Mathf.CeilToInt(pipe.barrelFrom) + 2; var start = f[k0]; var along = (f[k0 + 2] - f[k0]); along.y = 0f; along.Normalize();
                bool longOne = car.GetComponentInChildren<BoxCollider>().size.z > 3f;
                Place(start, along, 14f); yield return new WaitForFixedUpdate();   // 14 m/s: fully held (11 and up), and every vehicle steers more sharply than at 20
                car.enabled = false;
                float most = 0f, turned = 0f, prevAng = 0f, t = 0f, landedUp = -1f; bool started = false, outOfPipe = false; float leftBarrelAt = -1f;
                while (t < 14f)
                {
                    yield return new WaitForFixedUpdate(); float dt = Time.fixedDeltaTime; t += dt;
                    var p = car.Body.position;
                    float st = Project(f.ToList(), fcum, p, out float stDev) / 0.5f;   // stations along the floor (0.5 m), wherever the vehicle is
                    bool inBarrel = st > pipe.barrelFrom && st < pipe.barrelTo && stDev < pipe.radius + 0.6f;
                    int k = Mathf.Clamp((int)st, 0, n - 2); var axis = Vector3.Lerp(f[k], f[k + 1], st - k) + Vector3.up * pipe.radius; var ax = (f[k + 1] - f[k]).normalized;
                    var r = p - axis; r -= ax * Vector3.Dot(r, ax); var nOut = r.magnitude > 0.2f ? r.normalized : Vector3.down;
                    float ang = Vector3.SignedAngle(Vector3.down, nOut, ax);
                    if (started) { turned += Mathf.DeltaAngle(prevAng, ang); } started = true; prevAng = ang;
                    if (inBarrel && car.GroundedWheels >= 2) most = Mathf.Max(most, Mathf.Abs(ang) > 179f ? 180f : Vector3.Angle(Vector3.down, nOut));
                    // steering: 60 degrees off the axis round the pipe while in the full hold, then back along it
                    var circ = Vector3.Cross(ax, nOut).normalized;   // round the pipe (the way it turns: + steering left)
                    var fwd = car.transform.forward; float a = Vector3.SignedAngle(ax, Vector3.ProjectOnPlane(fwd, -nOut), -nOut);
                    bool hold = inBarrel && st < pipe.barrelTo - 12f && t < 8f;   // then (or after being put back in the bottom) straight on along it
                    float wantA = hold ? (longOne ? -35f : -60f) : 0f;   // a car rides a shallower spiral: turned 60 degrees across a 5 m pipe its nose and tail dig into the rising wall
                    float steer = Mathf.Clamp((wantA - a) / 25f, -1f, 1f);   // + steers right, and a grows to the right of the axis
                    car.Simulate(car.ForwardSpeed < 15f ? 1f : 0f, 0f, car.GroundedWheels == 0 ? 0f : steer, dt);
                    var cam = Camera.main ? Camera.main.transform.position : p;
                    if (inBarrel && DrainPipe.Inside(cam, out _) == null) { camOut += dt; camWorst = $"{Nm(id)}: camera at ({cam.x:0.0}, {cam.y:0.0}, {cam.z:0.0})"; }
                    if (!shotWall && inBarrel && Vector3.Angle(Vector3.down, nOut) > 100f && car.GroundedWheels >= 2) { shotWall = true; car.enabled = true; yield return Capture("4_P1_riding_the_wall"); car.enabled = false; }
                    if (st >= pipe.barrelTo && leftBarrelAt < 0f) leftBarrelAt = t;
                    if (leftBarrelAt > 0f && t > leftBarrelAt + 1.5f && landedUp < 0f && car.GroundedWheels >= 2) landedUp = car.transform.up.y;
                    if (leftBarrelAt > 0f && t > leftBarrelAt + 2.5f) { outOfPipe = st > pipe.barrelTo + 6f; break; }
                }
                car.enabled = true;
                float speed = Flat(car.Body.linearVelocity).magnitude;
                if (Mathf.Abs(turned) >= 340f) circles++;
                bool ok = most > 90f && landedUp > 0.8f && outOfPipe && speed > 3f;
                if (!ok) bad.Add(Nm(id));
                report.AppendLine($"  walls:   {Nm(id),-18} furthest round {most:0} deg from the bottom, round the pipe {Mathf.Abs(turned):0} deg in all, then {(landedUp > 0 ? $"on its wheels (upright {landedUp:0.00})" : "NOT BACK ON ITS WHEELS")}, {(outOfPipe ? "out of the pipe" : "STILL IN IT")} at {speed:0.0} m/s");
                if (!shotIn) { shotIn = true; }
            }
            Check("every vehicle rides up the wall (past the side, 90 degrees) and comes back down on its wheels and out", bad.Count == 0, (bad.Count > 0 ? "not: " + string.Join(", ", bad) + "; " : $"all {ids.Length}; ") + $"{circles} went right round (360 degrees or more); put back in the bottom after lying overturned {DrainPipe.Rescues} time(s)");
            Check("the camera stays in the pipe while riding the walls", camOut < 0.05f, camOut > 0f ? $"{camOut:0.00} s outside; {camWorst}" : "always inside");
            // never a trap: left parked up the wall at 60 degrees, no input, every vehicle is back in the bottom on its wheels within 4 s
            var trapped = new List<string>();
            foreach (var id in ids)
            {
                yield return Vehicle(id);
                int km = n / 2; var ax = (f[km + 1] - f[km]).normalized; var axisP = f[km] + Vector3.up * pipe.radius;
                var nOut = Quaternion.AngleAxis(60f, ax) * Vector3.down;
                var rot = Quaternion.LookRotation(ax, -nOut);
                respawn.PlaceAt(f[km], Mathf.Atan2(ax.x, ax.z) * Mathf.Rad2Deg);
                yield return new WaitForFixedUpdate();
                car.Body.position = axisP + nOut * (pipe.radius - 0.75f); car.Body.rotation = rot; car.transform.SetPositionAndRotation(car.Body.position, rot);
                car.Body.linearVelocity = Vector3.zero; car.Body.angularVelocity = Vector3.zero;
                car.enabled = false;
                for (float t = 0f; t < 4f; t += Time.fixedDeltaTime) { yield return new WaitForFixedUpdate(); car.Simulate(0f, 0f, 0f, Time.fixedDeltaTime); }
                car.enabled = true;
                var r = car.Body.position - axisP; r -= ax * Vector3.Dot(r, ax);
                float round = Vector3.Angle(Vector3.down, r.normalized);
                bool ok = car.transform.up.y > 0.8f && car.GroundedWheels >= 2 && round < 25f;
                if (!ok) trapped.Add($"{Nm(id)} (round {round:0} deg, up {car.transform.up.y:0.00}, wheels {car.GroundedWheels})");
            }
            Check("never a trap: parked up the wall at 60 degrees with no input, every vehicle is back in the bottom on its wheels within 4 s", trapped.Count == 0, trapped.Count > 0 ? "not: " + string.Join(", ", trapped) : $"all {ids.Length}");
            // a reset inside: stopped on the wall side? stopped in the pipe facing back the way it came
            yield return Vehicle("original");
            var mid = f[n / 2]; var back = (f[n / 2 - 2] - f[n / 2 + 2]);
            Place(mid, back, 0f);
            yield return new WaitForSecondsRealtime(1.5f);
            bool found = CarRecovery.Find(respawn, out var rp, out var ry);
            var al = Flat(f[n / 2 + 2] - f[n / 2 - 2]).normalized; var face = Quaternion.Euler(0f, ry, 0f) * Vector3.forward;
            float angl = Vector3.Angle(al, face); angl = Mathf.Min(angl, 180f - angl);
            Check("a reset in the pipe puts the vehicle in its bottom, along it", found && DrainPipe.Inside(rp, out _) != null && Mathf.Abs(rp.y - mid.y) < 0.6f && angl < 10f, found ? $"at ({rp.x:0.0}, {rp.y:0.00}, {rp.z:0.0}) (floor {mid.y:0.00}), {angl:0} deg off the axis" : "no spot found");
            if (found) { respawn.PlaceAt(rp, ry); yield return new WaitForSecondsRealtime(1.2f); }
            // the look: from the chase camera in the middle of the pipe (the headlight on, the lamps dim)
            yield return Vehicle("moto");
            Place(f[Mathf.CeilToInt(pipe.barrelFrom) + 10], al, 0f);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture("4_P1_inside_dark_with_the_stream");
            var inside = f[n / 2] + Vector3.up * 1.6f;
            car.Body.isKinematic = true; car.Body.position += Vector3.down * 50f;
            yield return Still("4_P1_inside_no_vehicle", inside - al * 6f, inside + al * 10f - Vector3.up * 0.8f, 70f);
            car.Body.isKinematic = false;
        }

        // ---------------------------------------------------------------- 4 (also): P1 never charges a penalty

        IEnumerator Penalty()
        {
            report.AppendLine(); report.AppendLine("=== 4 (also): the penalty Dan saw going into P1 on lap 3: P1 three ways, the penalty after each");
            UseRoute("noonday_creek.json", "forward");
            var sc = race.Route.Shortcuts.First(s => s.Id == "p1_pipe"); var pipe = FindAnyObjectByType<DrainPipe>();
            int cp2 = sc.FirstForgiven, cp3 = sc.LastForgiven + 1; var gate3 = race.Route.Checkpoints[cp3];
            int prevAi = RaceController.AiCount; RaceController.AiCount = 0; RaceController.AiVehiclesOverride = null;
            var lines = new List<string>(); bool allClear = true;
            foreach (var how in new[] { "through", "reset in the creek at the mouth", "reset in the pipe" })
            {
                yield return Vehicle("drifter");   // Dan's vehicle in that session
                race.StartRace();
                while (race.State != RaceController.Stage.Racing) yield return null;
                var e = race.Entrants.First(x => x.IsPlayer);
                var path = sc.Path.ToList();
                var lead = LapSlice(AbsStation(path[0]) - 60f, AbsStation(path[0]), 2f);
                Place(lead[0], lead[1] - lead[0], 12f);
                e.ClockStarted = true; e.StartAt = e.LapStart = race.RaceClock; e.NextCheckpoint = cp2; e.Missed.Clear(); e.Forgiven.Clear(); e.PenaltyTotal = e.LapPenalty = 0f; e.MissedTotal = 0; e.ActiveShortcut = null; e.Finished = false;
                e.NotifyTeleported();
                var run = new Run();
                if (how == "through")
                    yield return DriveLine(Cat(lead, path), 16f, 60f, run, null, null, 3f);
                else
                {
                    var stopAt = how.Contains("creek") ? pipe.floor[0] - (pipe.floor[4] - pipe.floor[0]).normalized * 2.5f : pipe.floor[pipe.floor.Length / 2];
                    yield return DriveLine(Cat(lead, path), 16f, 60f, run, null, r => Flat(car.Body.position - stopAt).magnitude < 2.5f, 3f);
                    car.enabled = false; for (int i = 0; i < 60; i++) { yield return new WaitForFixedUpdate(); car.Simulate(0f, 1f, 0f, Time.fixedDeltaTime); } car.enabled = true;
                    yield return new WaitForSecondsRealtime(0.5f);
                    if (CarRecovery.Find(respawn, out var rp, out var ry)) { respawn.PlaceAt(rp, ry); race.NotifyTeleported(); }
                    yield return new WaitForSecondsRealtime(0.5f);
                    var rest = DrainPipe.Inside(car.Body.position, out _) != null ? path.SkipWhile(q => Flat(q - car.Body.position).magnitude > 4f).ToList() : new List<Vector3>();
                    if (rest.Count > 2) yield return DriveLine(rest, 16f, 40f, new Run(), null, null, 3f);
                }
                // then on along the lap past CP3
                float s0 = AbsStation(car.Body.position), s3 = AbsStation(gate3.Position) + 15f; if (s3 < s0) s3 += race.Route.LapLength;
                yield return DriveLine(LapSlice(s0, s3), 18f, 120f, new Run(), null, null, 4f, false);
                bool ok = e.PenaltyTotal == 0f && e.NextCheckpoint > cp3 && e.ShortcutsTaken.Any(x => x.Contains("p1_pipe"));
                report.AppendLine($"    {how}: ended at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0}), lap station {race.Route.LapStationOf(car.Body.position):0} from the gate (CP{cp3 + 1} at {gate3.S - race.Route.GateS:0}), shortcuts taken [{string.Join(",", e.ShortcutsTaken)}], race {race.State}");
                if (!ok) allClear = false;
                lines.Add($"{how}: penalty +{e.PenaltyTotal:0} s, {(e.ShortcutsTaken.Any(x => x.Contains("p1_pipe")) ? "P1 taken" : "P1 not counted")}, CP{cp2 + 1} {(e.Forgiven.Contains(cp2) ? "forgiven" : "passed")}, next gate {(e.NextCheckpoint > cp3 ? "past CP" + (cp3 + 1) : "CP" + (e.NextCheckpoint + 1))}" + (e.PenaltyLog.Count > 0 ? " (" + string.Join("; ", e.PenaltyLog) + ")" : ""));
                race.EndRace(); yield return null;
            }
            RaceController.AiCount = prevAi;
            Check("P1, its creek crossing and a reset at its mouth or inside it never charge a penalty", allClear, string.Join(" | ", lines));
        }
    
        // diagnostics: is the pipe's stream drawn, and where is it against the floor
        IEnumerator StreamLook()
        {
            report.AppendLine(); report.AppendLine("=== the pipe's stream (diagnostics)");
            UseRoute("noonday_creek.json", "forward");
            var ps = FindAnyObjectByType<PipeStream>(); var pipe = FindAnyObjectByType<DrainPipe>();
            if (ps == null || pipe == null) { Check("stream there", false); yield break; }
            var mr = ps.GetComponent<MeshRenderer>(); var mf = ps.GetComponent<MeshFilter>();
            var mesh = mf ? mf.sharedMesh : null;
            yield return Vehicle("moto");
            var f = pipe.floor; int n = f.Length; var al = Flat(f[n / 2 + 2] - f[n / 2 - 2]).normalized;
            Place(f[n / 2 - 20], al, 0f);
            yield return new WaitForSecondsRealtime(1.5f);
            var v = mesh != null ? mesh.vertices : new Vector3[0];
            float bestD = 99f; Vector3 near = default; foreach (var q in v) { float d = Flat(q - f[n / 2]).magnitude; if (d < bestD) { bestD = d; near = q; } }
            report.AppendLine($"  renderer enabled {mr && mr.enabled}, visible {mr && mr.isVisible}, layer {ps.gameObject.layer}, active {ps.gameObject.activeInHierarchy}, bounds {(mr ? mr.bounds.ToString() : "-")}");
            report.AppendLine($"  material {(mr && mr.sharedMaterial ? mr.sharedMaterial.name + " / " + mr.sharedMaterial.shader.name + " queue " + mr.sharedMaterial.renderQueue : "none")}, mesh {(mesh ? mesh.name + " " + mesh.vertexCount + " verts, " + mesh.triangles.Length / 3 + " tris, normal[0] " + (mesh.normals.Length > 0 ? mesh.normals[0].ToString() : "-") : "none")}");
            report.AppendLine($"  the ribbon's vertex nearest the pipe's middle at y {near.y:0.000}, the floor line there {f[n / 2].y:0.000}; camera at {(Camera.main ? Camera.main.transform.position.ToString() : "-")}");
            yield return Capture("4_stream_diagnostic");
            Check("stream drawn", mr && mr.enabled && mesh != null && mesh.vertexCount > 0);
        }
    }
}
