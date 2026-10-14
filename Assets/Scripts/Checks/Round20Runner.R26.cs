using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 26, checked in the exe (ChastainRush.exe -exeRound26 <dir> [-r26Only ramps,express,walls] [-wallRideRound25] -logFile
    // <dir>\Player.log), muted, minimized, its files in <dir>; report round26_check.txt. One targeted check per item:
    //   ramps    every big jump's take-off (and each back that is a reverse take-off): the Drifter Twin onto it at its race speed on the
    //            middle, 1.6 m in from each side and 8 degrees either way, the Trail Four on the middle; the speed kept from 20 m out to
    //            1.5 m short of the lip, the sharpest change in vertical speed in 0.1 s there (the bump log's JOLT is 2 m/s) and what the
    //            body hit. Dan's spot at RE2 first, from where he stood
    //   express  rides on E1 (the longest), E2, E5, E9 (up to a glass stretch) and E6 (short): the time, the speed let go at, the
    //            camera's steadiness (its fastest turn) and that it stayed in the pipe
    //   walls    in M4a, in the Street Classic and the Drifter Twin: up the wall and let go of the steering; a loop over the ceiling; slow
    //            on the ceiling; high on the wall toward the hub (eased down before it). Run once with -wallRideRound25 for Round 25's
    //            behavior (the before)
    public sealed partial class Round20Runner
    {
        IEnumerator Ramps()
        {
            report.AppendLine(); report.AppendLine("=== 1. Ramps: onto every big jump's take-off (BUG-001: RE2's foot was a wall)");
            PipeExpress.Setting = PipeExpress.Mode.Off;
            var tap = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>();
            var jumps = BigJump.All.OrderBy(j => j.id == "RE2" ? "" : j.id, System.StringComparer.Ordinal).ToList();
            var bad = new List<string>(); int drives = 0; float worstJolt = 0f; string worstAt = "";
            // Dan's own approach first: where he stood (x 2362.22, z 1256.83, heading 275), the Drifter Twin at RE2's speed
            {
                yield return Vehicle("drifter");
                var j = jumps.First(b => b.id == "RE2");
                var from = new Vector3(2362.22f, 0f, 1256.83f); from.y = j.lip.y;
                var r = new RampRun(); yield return RampDrive(j, from, Flat(j.lip - from).normalized, 17f, r, tap);
                report.AppendLine($"  Dan's spot to RE2 (Drifter Twin, 17 m/s): kept {r.kept * 100f:0} % of its speed to the lip, sharpest vertical change {r.jolt:0.0} m/s in 0.1 s, {(r.reached ? "off the lip" : "DID NOT REACH THE LIP")}{(r.hits.Count > 0 ? "; hit " + string.Join("; ", r.hits.Take(3)) : "")}");
                yield return Capture("1_RE2_from_Dans_spot");
                if (!r.Ok) bad.Add("RE2 from Dan's spot");
            }
            foreach (var j in jumps)
            {
                float v = j.vmin > 0f ? j.vmin : Mathf.Min(j.vmax > 0f ? j.vmax : 18f, 20f);
                var d = Flat(j.dir).normalized; var nl = Vector3.Cross(Vector3.up, d) * -1f; float hw = j.halfWidth;
                var tries = new List<(string id, float o, float a)> { ("drifter", 0f, 0f), ("drifter", hw - 1.6f, 0f), ("drifter", -(hw - 1.6f), 0f), ("drifter", 0f, 8f), ("drifter", 0f, -8f), ("atv", 0f, 0f) };
                if (j.look == "outfall") tries.RemoveAll(q => q.a != 0f);   // P1J is met only coming out of P1's mouth: at an angle the line runs through the cut's walls
                var line = new List<string>();
                foreach (var (id, o, a) in tries)
                {
                    yield return Vehicle(id);
                    var da = Quaternion.AngleAxis(a, Vector3.up) * d; var aim = j.lip + nl * o;
                    var r = new RampRun(); yield return RampDrive(j, aim - da * 24f, da, v, r, tap); drives++;
                    if (r.jolt > worstJolt) { worstJolt = r.jolt; worstAt = $"{j.id} {Nm(id)} o {o:+0.0;-0.0} a {a:+0;-0;0}"; }
                    line.Add($"{(id == "atv" ? "ATV" : "")}o{o:+0.0;-0.0}a{a:+0;-0;0} {r.kept * 100f:0}%/{r.jolt:0.0}{(r.reached ? "" : " SHORT")}{(r.hits.Count > 0 ? " hit " + r.hits[0] : "")}");
                    if (!r.Ok) bad.Add($"{j.id} {Nm(id)} o {o:+0.0;-0.0} a {a:+0;-0;0} (kept {r.kept * 100f:0} %, jolt {r.jolt:0.0}{(r.reached ? "" : ", short")})");
                }
                report.AppendLine($"  {j.id,-13} {v:0} m/s: " + string.Join(" | ", line));
            }
            Check("onto every take-off, on the line, near each side and 8 degrees off: no stop, no catch (speed kept 85 % or more, no body contact against the way it goes); the sharpest change in vertical speed over the foot for comparison", bad.Count == 0,
                  (bad.Count > 0 ? "not: " + string.Join("; ", bad) + "; " : "") + $"{drives} drives; the sharpest {worstJolt:0.0} m/s ({worstAt})");
            yield return Still("1_RE2_foot", new Vector3(2361f, 278.2f, 1253.2f), new Vector3(2354f, 277.2f, 1256.3f));
            yield return Still("1_RE2_foot_side", new Vector3(2357.5f, 278.6f, 1262.5f), new Vector3(2354f, 277.2f, 1256.1f));
            PipeExpress.Setting = PipeExpress.Mode.Boost;
        }

        // jolt: the sharpest change in vertical speed in 0.1 s from 20 m out until the vehicle is 2.5 m onto the ramp (its foot: the ramp's own
        // curve beyond is not a bump); hits: body contacts facing back against it (a wall or a lip), not the ramp's own surface under it
        sealed class RampRun { public float kept = 1f, jolt; public bool reached; public List<string> hits = new List<string>(); public bool Ok => reached && kept >= 0.85f && hits.Count == 0; }

        // straight at the lip from 'from' (any height: placed on the ground) along dir, held at v; measured from 20 m out to 1.5 m short of it
        IEnumerator RampDrive(BigJump j, Vector3 from, Vector3 dir, float v, RampRun r, Touches tap)
        {
            var d = Flat(j.dir).normalized;
            float g = Physics.Raycast(from + Vector3.up * 30f, Vector3.down, out var h, 60f, ~0, QueryTriggerInteraction.Ignore) ? h.point.y : from.y;
            Place(new Vector3(from.x, g, from.z), dir, v); yield return new WaitForFixedUpdate();
            car.enabled = false; tap.seen.Clear();
            var hist = new Queue<(float t, float vy)>(); float t = 0f, vIn = -1f, vMin = 99f, footAt = float.NaN;
            while (t < 6f)
            {
                yield return new WaitForFixedUpdate(); float dt = Time.fixedDeltaTime; t += dt;
                var p = car.Body.position; float toLip = Vector3.Dot(Flat(j.lip - p), d);   // metres short of the lip along the jump
                if (toLip < 1.5f) { r.reached = true; break; }
                float fwd = Vector3.Dot(car.Body.linearVelocity, Flat(car.transform.forward).normalized);
                float steer = Mathf.Clamp(Vector3.SignedAngle(Flat(car.transform.forward).normalized, dir, Vector3.up) / 15f, -1f, 1f);
                car.Simulate(fwd < v ? 1f : 0f, fwd > v + 1.5f ? 0.5f : 0f, car.GroundedWheels == 0 ? 0f : steer, dt);
                bool watch = toLip < 20f;
                tap.on = watch;
                if (!watch) continue;
                float sp = Flat(car.Body.linearVelocity).magnitude;
                if (vIn < 0f) vIn = sp;
                vMin = Mathf.Min(vMin, sp);
                hist.Enqueue((t, car.Body.linearVelocity.y)); while (hist.Count > 0 && t - hist.Peek().t > 0.1f + 1e-3f) hist.Dequeue();
                // the foot: where the ground under the vehicle's front first is the ramp
                if (float.IsNaN(footAt) && Physics.Raycast(p + Flat(car.transform.forward).normalized * 1.2f + Vector3.up * 1.5f, Vector3.down, out var fh, 4f, ~0, QueryTriggerInteraction.Ignore) && fh.collider.name.StartsWith("sc ")) footAt = toLip;
                bool nearFoot = float.IsNaN(footAt) || toLip > footAt - 2.5f;
                if (nearFoot && car.GroundedWheels > 0 && hist.Count > 1) r.jolt = Mathf.Max(r.jolt, Mathf.Abs(car.Body.linearVelocity.y - hist.Peek().vy));
            }
            tap.on = false; car.enabled = true;
            r.kept = vIn > 0f ? vMin / vIn : 0f;
            foreach (var s in tap.seen)
            {
                if (s.Contains("Terrain_")) continue;
                var m = System.Text.RegularExpressions.Regex.Match(s, @"normal \((-?[0-9.]+), (-?[0-9.]+), (-?[0-9.]+)\)");
                if (!m.Success) continue;
                var nrm = new Vector3(float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), float.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture));
                if (nrm.y > 0.5f || Vector3.Dot(nrm, dir) > -0.3f) continue;   // the surface under it, or a glancing touch from the side
                string what = s.Split(new[] { " at " }, System.StringSplitOptions.None)[0];
                if (!r.hits.Contains(what)) r.hits.Add(what);
            }
        }

        // ---------------------------------------------------------------- 3. the pipe express everywhere

        IEnumerator Express()
        {
            report.AppendLine(); report.AppendLine("=== 3. The pipe express on every run of pipe over about 100 m");
            var px = PipeExpress.Instance;
            Check("the pipe express is on every run over 100 m (37 stretches)", px != null && px.stretches != null && px.stretches.Length >= 30, px != null ? $"{px.stretches.Length} stretches; the fastest {px.stretches.Max(s => s.top):0} m/s ({px.stretches.OrderByDescending(s => s.top).First().id})" : "none");
            if (px == null) yield break;
            PipeExpress.Setting = PipeExpress.Mode.Boost;
            yield return Vehicle("original");
            var bad = new List<string>(); bool shot = false;
            foreach (var id in new[] { "E1", "E2", "E5", "E9", "E6" })
            {
                var st = px.stretches.FirstOrDefault(s => s.id == id); if (st == null) { bad.Add(id + " missing"); continue; }
                yield return new WaitForSeconds(2f);   // the express's pause after a ride (1.5 s) is over
                var f = st.floor; var dir = Flat(f[3] - f[0]).normalized;
                Place(f[0] - dir * 8f + Vector3.up * 0.2f, dir, 14f); yield return new WaitForFixedUpdate();   // coasting in (no input)
                int rides0 = PipeExpress.Rides; float t = 0f, camTurn = 0f, camOut = 0f; Quaternion prevCam = Camera.main.transform.rotation; bool boarded = false; float exitV = 0f;
                while (t < 30f)
                {
                    yield return null; t += Time.deltaTime;
                    if (!boarded && PipeExpress.Rides > rides0) boarded = true;
                    if (boarded)
                    {
                        var cam = Camera.main.transform;
                        if (Time.deltaTime > 1e-4f) camTurn = Mathf.Max(camTurn, Quaternion.Angle(prevCam, cam.rotation) / Time.deltaTime);
                        if (car.Body.isKinematic && DrainPipe.Inside(cam.position, out _) == null) camOut += Time.deltaTime;
                        if (!shot && car.Body.isKinematic && PipeExpress.LastRideTime > 2f) { shot = true; yield return Capture("3_express_ride_" + id); }
                        if (!car.Body.isKinematic) { exitV = car.Body.linearVelocity.magnitude; break; }
                    }
                    prevCam = Camera.main.transform.rotation;
                    if (!boarded && t > 6f) { var rel = car.Body.position - f[0]; report.AppendLine($"    ({id} not boarded: the vehicle {Vector3.Dot(Flat(rel), dir):0.0} m along from the entry, {Flat(rel - dir * Vector3.Dot(Flat(rel), dir)).magnitude:0.0} m off its line, {rel.y:0.0} m over it, at {car.Body.linearVelocity.magnitude:0.0} m/s)"); break; }
                }
                report.AppendLine($"  {id} {st.name}: {(boarded ? $"ride {PipeExpress.LastRideTime:0.0} s at up to {(st.top > 0 ? st.top : PipeExpress.BoostSpeed):0} m/s over {Length(f):0} m, let go at {exitV:0.0} m/s; the camera turned at most {camTurn:0} deg/s, {(camOut > 0f ? $"OUTSIDE the pipe {camOut:0.00} s" : "always in the pipe")}" : "NOT BOARDED")}");
                if (!boarded || PipeExpress.LastRideTime > 12.5f || exitV < 15f || exitV > 25f || camOut > 0.05f) bad.Add(id);
            }
            Check("rides of 12 s or less, let go at about 20 m/s, the camera in the pipe", bad.Count == 0, bad.Count > 0 ? "not: " + string.Join(", ", bad) : "E1, E2, E5, E9, E6");
        }
        static float Length(Vector3[] f) { float L = 0f; for (int i = 1; i < f.Length; i++) L += Vector3.Distance(f[i - 1], f[i]); return L; }

        // ---------------------------------------------------------------- 4. driving on the walls and the ceiling

        IEnumerator Walls()
        {
            bool before = DrainPipe.Round25Walls;
            report.AppendLine(); report.AppendLine($"=== 4. Driving on the walls and the ceiling, in M4a ({(before ? "ROUND 25's BEHAVIOR (-wallRideRound25): the before" : "Round 26")})");
            PipeExpress.Setting = PipeExpress.Mode.Off;
            // the piece of M4a about 150 m from J6 (a long straight run), and the one at its hub end
            var pieces = FindObjectsByType<DrainPipe>(FindObjectsSortMode.None).Where(p => p.id != null && p.id.StartsWith("M4a@")).OrderBy(p => int.Parse(p.id.Substring(4))).ToList();
            if (pieces.Count < 4) { Check("M4a's pieces are there", false, $"{pieces.Count}"); yield break; }
            var mid = pieces[2];
            DrainPipe last = pieces[pieces.Count - 1]; int lastK = 0;
            { float need = 70f; for (int i = pieces.Count - 1; i >= 0; i--) { float L = Length(pieces[i].floor); if (L >= need) { last = pieces[i]; lastK = Mathf.Clamp(Mathf.RoundToInt((L - need) / Mathf.Max(0.1f, L / (pieces[i].floor.Length - 1))), 0, pieces[i].floor.Length - 3); break; } need -= L; } }
            var lines = new List<string>(); bool shotWall = false, shotRoof = false;
            foreach (var id in new[] { "original", "drifter" })
            {
                yield return Vehicle(id);
                // a. up the wall: 16 m/s along, steer left 0.5 s, then let go of the steering
                trace = new List<string>();
                var a = new WallRun(); yield return WallDrive(mid, 16f, 0.5f, -1f, 4.5f, a, false, "4a_up_the_wall_" + id, !shotWall); shotWall = true;
                var aTrace = trace; trace = null;
                lines.Add($"  {Nm(id),-14} up the wall at 16 m/s, steering let go: highest {a.most:0} deg round, then {a.endRound:0} deg round with its nose {a.endHeading:0} deg off the pipe's line after 4 s (round the pipe {a.turned:0} deg in all), on the wall {a.onWall:0.0} s, camera roll up to {a.camRoll:0} deg, camera {(a.camOut > 0f ? "behind " + a.camWhy + " " + a.camOut.ToString("0.00") + " s" : "in the pipe, nothing between it and the vehicle")}");
                lines.AddRange(aTrace);
                // b. a loop over the ceiling: 20 m/s, steering left held until right round (360 degrees), then let go
                var b = new WallRun(); yield return WallDrive(mid, 20f, 99f, -1f, 6f, b, true, "4b_over_the_ceiling_" + id, !shotRoof); shotRoof = true;
                lines.Add($"  {Nm(id),-14} a loop at 20 m/s: round the pipe {b.turned:0} deg, highest {b.most:0} deg, slowest {b.slowest:0.0} m/s, {(b.dropped ? "FELL OFF" : "stayed on")}, upright at the end {b.upEnd:0.00}, camera roll up to {b.camRoll:0} deg, camera {(b.camOut > 0f ? "behind " + b.camWhy + " " + b.camOut.ToString("0.00") + " s" : "clear")}");
                // c. slow on the ceiling: up to it at 20 m/s, then off the throttle and braking: let go, dropped, on its wheels
                var c = new WallRun(); yield return WallDrive(mid, 20f, 99f, -1f, 7f, c, false, null, false, brakeOnTop: true);
                lines.Add($"  {Nm(id),-14} slow on the ceiling: let go at {c.letGoAt:0.0} m/s ({c.letGoRound:0} deg round), {(c.upEnd > 0.8f ? $"back on its wheels at the bottom (upright {c.upEnd:0.00})" : $"NOT ON ITS WHEELS (upright {c.upEnd:0.00})")}");
                // d. high on the wall toward the hub (M4a's end): eased down before it
                trace = new List<string>();
                var e = new WallRun(); yield return WallDrive(last, 16f, 0.6f, -1f, 12f, e, false, null, false, toEnd: true, startK: lastK);
                lines.Add($"  {Nm(id),-14} high on the wall toward the hub: {e.most:0} deg round at most, {e.endRound:0} deg round and upright {e.upEnd:0.00} leaving the pipe ({(e.leftPipe ? "out of it" : "STILL IN IT")})");
                lines.AddRange(trace); trace = null;
                if (!before)
                {
                    if (!(a.most > 60f && Mathf.Abs(a.endHeading) < 15f && a.endRound > 30f)) bad26.Add($"{Nm(id)} up the wall");
                    if (!(b.turned >= 340f && !b.dropped)) bad26.Add($"{Nm(id)} loop");
                    if (!(c.upEnd > 0.8f && c.letGoAt < 13f)) bad26.Add($"{Nm(id)} slow on the ceiling");
                    if (!(e.leftPipe && e.endRound < 25f && e.upEnd > 0.8f)) bad26.Add($"{Nm(id)} out at the hub");
                    if (a.camOut > 0.05f || b.camOut > 0.05f) bad26.Add($"{Nm(id)} camera");
                }
            }
            foreach (var l in lines) report.AppendLine(l);
            if (!before) Check("on the walls: up the wall and along it, a loop over the ceiling, dropped when slow, eased to the floor before the hub, the camera in the pipe", bad26.Count == 0, bad26.Count > 0 ? "not: " + string.Join(", ", bad26) : "Street Classic and Drifter Twin");
            report.AppendLine($"  (physics steps eased off the wall before an end: {DrainPipe.Eased})");
            PipeExpress.Setting = PipeExpress.Mode.Boost;
        }
        readonly List<string> bad26 = new List<string>();

        sealed class WallRun { public float most, endRound, endHeading, turned, onWall, camRoll, camOut, slowest = 99f, upEnd, letGoAt, letGoRound; public bool dropped, leftPipe; public string camWhy = ""; }

        // in a network pipe piece: placed on its floor 10 m in, along it at v; steer (-1 left) for steerFor s (99: until 360 degrees round)
        List<string> trace;
        IEnumerator WallDrive(DrainPipe pipe, float v, float steerFor, float steerDir, float seconds, WallRun w, bool loop, string shot, bool takeShot, bool brakeOnTop = false, bool toEnd = false, int startK = 10)
        {
            var f = pipe.floor; int k0 = startK;
            var tap = car.gameObject.GetComponent<Touches>() ?? car.gameObject.AddComponent<Touches>(); tap.seen.Clear(); tap.on = true; float prevSpeed = v;
            var along0 = Flat(f[k0 + 2] - f[k0]).normalized;
            Place(f[k0], along0, v); yield return new WaitForFixedUpdate();
            car.enabled = false;
            float t = 0f, prevAng = 0f; bool started = false, letGo = false, offTop = false; float air = 0f;
            while (t < seconds)
            {
                yield return new WaitForFixedUpdate(); float dt = Time.fixedDeltaTime; t += dt;
                var p = car.Body.position;
                var dp = DrainPipe.Inside(p, out float st);
                if (dp == null || !dp.id.StartsWith(pipe.id.Split('@')[0] + "@")) { if (toEnd && t > 0.5f) { w.leftPipe = true; w.upEnd = car.transform.up.y; break; } continue; }
                // what it touched when it lost speed suddenly (a check of why a vehicle comes off the wall)
                float spNow = car.Body.linearVelocity.magnitude;
                if (trace != null && prevSpeed - spNow > 0.6f && tap.seen.Count > 0) trace.Add($"      t {t:0.00} lost {prevSpeed - spNow:0.0} m/s in a step: {tap.seen[tap.seen.Count - 1]}");
                prevSpeed = spNow; tap.seen.Clear();
                int k = Mathf.Clamp((int)st, 0, dp.floor.Length - 2); var ax = (dp.floor[k + 1] - dp.floor[k]).normalized; var axis = Vector3.Lerp(dp.floor[k], dp.floor[k + 1], st - k) + Vector3.up * dp.radius;
                var r = p - axis; r -= ax * Vector3.Dot(r, ax); var nOut = r.magnitude > 0.2f ? r.normalized : Vector3.down;
                float round = Vector3.Angle(Vector3.down, nOut), ang = Vector3.SignedAngle(Vector3.down, nOut, ax);
                if (started) w.turned += Mathf.Abs(Mathf.DeltaAngle(prevAng, ang)); started = true; prevAng = ang;
                if (car.GroundedWheels >= 2) { w.most = Mathf.Max(w.most, round); if (round > 45f) w.onWall += dt; }
                float speed = car.Body.linearVelocity.magnitude; if (round > 90f && car.GroundedWheels >= 2) w.slowest = Mathf.Min(w.slowest, speed);
                air = car.GroundedWheels == 0 ? air + dt : 0f; if (round > 100f && air > 0.3f && !brakeOnTop) w.dropped = true;
                if (brakeOnTop && round > 150f && car.GroundedWheels >= 2) offTop = true;
                if (brakeOnTop && offTop && !letGo && car.GroundedWheels == 0) { letGo = true; w.letGoAt = speed; w.letGoRound = round; }
                // steering
                float steer = 0f;
                if (loop) steer = w.turned < 350f ? steerDir * 0.6f : 0f;
                else if (brakeOnTop) steer = offTop ? 0f : steerDir;
                else steer = t < steerFor ? steerDir : 0f;
                if (car.GroundedWheels == 0) steer = 0f;
                float fwdV = car.ForwardSpeed;
                float thr = brakeOnTop && offTop ? 0f : (fwdV < v ? 1f : 0f), brk = brakeOnTop && offTop ? 1f : (fwdV > v + 1.5f ? 1f : 0f);
                car.Simulate(thr, brk, steer, dt);
                var fwd = Vector3.ProjectOnPlane(car.transform.forward, -nOut); var axp = Vector3.ProjectOnPlane(ax, -nOut);
                if (Vector3.Dot(fwd, axp) < 0f) axp = -axp;
                w.endHeading = Vector3.SignedAngle(axp, fwd, -nOut); w.endRound = round; w.upEnd = round < 30f ? car.transform.up.y : Vector3.Dot(car.transform.up, -nOut);
                var cam = Camera.main.transform; w.camRoll = Mathf.Max(w.camRoll, Vector3.Angle(cam.up, Vector3.ProjectOnPlane(Vector3.up, cam.forward)));
                if (t > 0.5f && Physics.Linecast(p + (-nOut) * 0.8f, cam.position, out var ch, ~0, QueryTriggerInteraction.Ignore) && ch.rigidbody == null) { w.camOut += dt; w.camWhy = $"{ch.collider.name}"; }
                if (trace != null && ((int)(t / 0.25f)) != ((int)((t - dt) / 0.25f)))
                    trace.Add($"      t {t:0.00}  {(dp.floor.Length - 1 - st) * Vector3.Distance(dp.floor[0], dp.floor[1]):0.0} m to the piece's end  round {round:0}  nose {w.endHeading:+0;-0}  wheels {car.GroundedWheels}  {speed:0.0} m/s  surface-up tilt {Vector3.Angle(car.SurfaceUp, Vector3.up):0}");
                if (takeShot && shot != null && ((loop && round > 160f && car.GroundedWheels >= 2) || (!loop && round > 75f && car.GroundedWheels >= 2 && t > steerFor + 0.5f)))
                { takeShot = false; car.enabled = true; yield return Capture(shot); car.enabled = false; }
            }
            tap.on = false; car.enabled = true;
        }
    }
}
