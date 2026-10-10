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
    // Round 10, items 2i and 10: one targeted check in the built exe. ChastainRush.exe -exeRound10 <dir> (added by GameShell):
    //   shortcut #2  street-side shots of houses 1763 and 1627 (and 1627's back door) before and after their panels break, a
    //                side profile of the line, and two drives of it through the real car (ArcadeVehicle.Simulate) from the
    //                lane, at racing speed and slow: speed where each panel breaks, flights, time against the road
    //   trees        the car driven into a yard tree at 20 m/s (stops?), reversed away, then reset (free roam and in a race)
    //   lamp, hydrant driven into at 20 m/s (breaks, keeps going), the hydrant's water running and shot from down the street
    // Dan's house and lot are hidden for the committed shots (they must never show where it is). Then the exe quits.
    public sealed class Round10Runner : MonoBehaviour
    {
        public string outputDir;
        ArcadeVehicle car; VehicleRespawn respawn; RaceController race;
        readonly StringBuilder report = new StringBuilder();
        readonly List<(Vector3 at, float speed, string chunk)> breaks = new List<(Vector3, float, string)>();

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"Round 10 exe check, {DateTime.Now:yyyy-MM-dd HH:mm:ss}, screen {Screen.width}x{Screen.height}, {(Application.isEditor ? "Editor" : "built exe")}");
            yield return new WaitForSecondsRealtime(4f);
            car = FindAnyObjectByType<ArcadeVehicle>(); respawn = car.GetComponent<VehicleRespawn>(); race = RaceController.Instance;
            FenceChunk.PanelBroken += OnBroken;
            SetDan(false);
            yield return Shortcut2();
            yield return Trees();
            yield return Breakable("StreetLamps", "lamp");
            yield return Breakable("Hydrants", "hydrant");
            SetDan(true);
            var tt = TreeTrunks.Instance;
            report.AppendLine(tt ? $"Trunk colliders: pool of {tt.pool} within {tt.radius:0} m of the car; {tt.Refreshes} refreshes, last one {tt.LastRefreshMs:0.00} ms, {tt.Active} active at the end" : "Trunk colliders: no TreeTrunks in the scene");
            File.WriteAllText(Path.Combine(outputDir, "round10_check.txt"), report.ToString());
            FenceChunk.PanelBroken -= OnBroken;
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(0);
        }

        void OnBroken(FenceChunk c, int panel, Vector3 at) => breaks.Add((at, car.Body.linearVelocity.magnitude, c.transform.parent ? c.transform.parent.name : c.name));

        static void SetDan(bool on)
        {
            foreach (var n in new[] { "DanHouse", "DanLot" })
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name == n) t.gameObject.SetActive(on);
        }

        // ---------------------------------------------------------------- shortcut #2

        Vector3 PanelAt(string name)
        {
            foreach (var c in FindObjectsByType<FenceChunk>(FindObjectsSortMode.None))
                if (c.transform.parent && c.transform.parent.name == name) return c.transform.TransformPoint(c.panels[0].centre);
            return Vector3.zero;
        }

        IEnumerator Shortcut2()
        {
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "sc2_two_houses");
            if (sc == null) { report.AppendLine("shortcut #2: not in the route"); yield break; }
            Vector3 aIn = PanelAt("House 1763 shortcut wall (in)"), aOut = PanelAt("House 1763 shortcut wall (out)"), bIn = PanelAt("House 1627 shortcut wall (in)"), bOut = PanelAt("House 1627 shortcut wall (out)");
            report.AppendLine($"shortcut #2 panels: 1763 in {Fmt(aIn)}, out {Fmt(aOut)}; 1627 door {Fmt(bIn)}, out {Fmt(bOut)}");
            FenceChunk.RestoreAll();
            car.transform.position = new Vector3(3200f, 400f, 900f);   // out of the shots
            car.Body.linearVelocity = Vector3.zero; car.enabled = false; car.Body.isKinematic = true;
            yield return null;
            var shots = ShotList(aIn, bIn, bOut);
            foreach (var s in shots) yield return Shoot(s.name + "_before", s.eye, s.look, s.fov);
            yield return Profile(aIn, bOut, "r10_sc2_side_profile_before");
            car.Body.isKinematic = false; car.enabled = true;
            float leave = race.Route.LapStationOf(sc.Start), rejoin = race.Route.LapStationOf(sc.End);
            foreach (var (label, v) in new[] { ("racing speed", 30f), ("slow", 13f) })
            {
                FenceChunk.RestoreAll();
                var road = new Run(); yield return Drive(sc, leave, rejoin, v, false, road);
                FenceChunk.RestoreAll(); breaks.Clear();
                var run = new Run(); yield return Drive(sc, leave, rejoin, v, true, run);
                report.AppendLine($"shortcut #2, {label} (target {v:0} m/s): {run.result}, {run.time:0.00} s against {road.time:0.00} s by the road ({road.time - run.time:+0.00;-0.00} s); speed at the ramp's foot {run.footSpeed:0.0} m/s");
                foreach (var b in breaks) report.AppendLine($"    panel broken: {b.chunk} at {b.speed:0.0} m/s");
                foreach (var f in run.flights) report.AppendLine($"    flight: {f}");
                report.AppendLine($"    lowest speed after the ramp's foot {run.minAfterFoot:0.0} m/s; most tilt {run.maxTilt:0} deg; panels broken {breaks.Count}");
                if (label == "racing speed")
                {
                    car.transform.position = new Vector3(3200f, 400f, 900f); car.Body.linearVelocity = Vector3.zero; car.enabled = false; car.Body.isKinematic = true;
                    yield return null;
                    foreach (var s in shots) yield return Shoot(s.name + "_after", s.eye, s.look, s.fov);
                    yield return Profile(aIn, bOut, "r10_sc2_side_profile_after");
                    car.Body.isKinematic = false; car.enabled = true;
                }
            }
            FenceChunk.RestoreAll();
        }

        static List<(string name, Vector3 eye, Vector3 look, float fov)> ShotList(Vector3 aIn, Vector3 bIn, Vector3 bOut)
        {
            var l = new List<(string, Vector3, Vector3, float)>();
            // 1763 from the lane in front of it; 1627 from its street; 1627's back door from the slope above it
            var lane1763 = RaceRoute.Ground(3086.0f, 1036.6f) + Vector3.up * 1.7f;
            l.Add(("r10_1763_from_the_lane", lane1763, aIn, 55f));
            var st1627 = RaceRoute.Ground(3034.0f, 1120.0f) + Vector3.up * 1.7f;
            l.Add(("r10_1627_from_its_street", st1627, bOut, 55f));
            var back = bIn + (bIn - bOut).normalized * 16f; back.y = RaceRoute.Ground(back.x, back.z).y + 1.7f;
            l.Add(("r10_1627_back_door_from_the_slope", back, bIn, 55f));
            return l;
        }

        IEnumerator Shoot(string name, Vector3 eye, Vector3 look, float fov, bool ortho = false, float size = 0f, float near = 0.1f, float far = 2000f)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            var go = new GameObject("Round10 camera"); var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = near; cam.farClipPlane = far; cam.orthographic = ortho; if (ortho) cam.orthographicSize = size;
            go.transform.position = eye; go.transform.LookAt(look);
            int w = 1920, h = 1080;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex); rt.Release(); Destroy(go);
        }

        // looking at the line from the east, square to its first leg, showing only what is within 15 m of it
        IEnumerator Profile(Vector3 from, Vector3 to, string name)
        {
            var mid = (from + to) * 0.5f; var dir = to - from; dir.y = 0f; dir.Normalize();
            var side = Vector3.Cross(Vector3.up, dir);   // to the right of the line (east)
            var eye = mid + side * 150f; eye.y = mid.y + 2f;
            float len = Vector3.Distance(new Vector3(from.x, 0, from.z), new Vector3(to.x, 0, to.z)) + 30f;
            yield return Shoot(name, eye, mid - side * 10f + Vector3.up * 2f, 30f, true, len / 2f / (16f / 9f), 135f, 165f);
        }

        // ---------------------------------------------------------------- driving a line

        sealed class Run { public string result = "timeout"; public float time, footSpeed, minAfterFoot = 99f, maxTilt; public List<string> flights = new List<string>(); }

        IEnumerator Drive(RaceShortcut sc, float leave, float rejoin, float v, bool shortcut, Run run)
        {
            var route = race.Route;
            var path = route.SamplePath(route.GateS + leave - 45f, route.GateS + leave, 3f);
            if (shortcut) { path.AddRange(sc.Path); path.AddRange(route.SamplePath(route.GateS + rejoin, route.GateS + rejoin + 40f, 3f)); }
            else path.AddRange(route.SamplePath(route.GateS + leave, route.GateS + rejoin + 40f, 3f));
            var start = route.PointAt(route.GateS + leave - 45f, out var f0);
            var finish = route.PointAt(route.GateS + rejoin + 20f, out var f1);
            respawn.PlaceAt(start + Vector3.up * 0.3f, Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg);
            yield return new WaitForFixedUpdate();
            car.enabled = false;
            car.Body.linearVelocity = f0 * v;
            int next = 0; float t = 0f, airT = 0f; bool onFoot = false; Vector3 takeoff = Vector3.zero; float slow = 0f;
            float prevSide = Vector3.Dot(car.transform.position - finish, f1);
            while (t < 60f)
            {
                yield return new WaitForFixedUpdate();
                float dt = Time.fixedDeltaTime; t += dt;
                var pos = car.transform.position; float speed = car.ForwardSpeed;
                // steering: aim a few metres ahead along the line
                int best = next; float bd = float.MaxValue;
                for (int i = next; i < Mathf.Min(path.Count, next + 15); i++) { float d = (Flat(path[i]) - Flat(pos)).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
                next = best;
                float la = 4f + 0.12f * Mathf.Max(speed, 1f), acc = 0f; int k = next;
                while (k < path.Count - 1 && acc < la) { acc += (Flat(path[k + 1]) - Flat(path[k])).magnitude; k++; }
                float steer = Mathf.Clamp(Vector3.SignedAngle(Flat(car.transform.forward), Flat(path[k]) - Flat(pos), Vector3.up) / 18f, -1f, 1f);
                // speed: the target, but slow enough for the bends ahead (18 m/s2 of grip, braking at 14 m/s2)
                float w = v; acc = 0f;
                for (int i = next + 1; i + 2 < path.Count && acc < 60f; i++)
                {
                    acc += (Flat(path[i]) - Flat(path[i - 1])).magnitude;
                    Vector3 a = Flat(path[i - 1]), b = Flat(path[i]), c = Flat(path[i + 2]);
                    float area2 = Mathf.Abs(Vector3.Cross(b - a, c - a).y); if (area2 < 1e-3f) continue;
                    float r = (b - a).magnitude * (c - b).magnitude * (c - a).magnitude / (2f * area2);
                    if (r < 4f) continue;
                    w = Mathf.Min(w, Mathf.Sqrt(18f * r + 2f * 14f * acc));
                }
                car.Simulate(speed < w ? 1f : 0f, speed > w + 1.5f ? Mathf.Clamp01((speed - w) / 4f) : 0f, steer, dt);
                bool air = car.GroundedWheels == 0;
                if (air) { if (airT == 0f) takeoff = pos; airT += dt; }
                else if (airT > 0f) { if (airT > 0.25f) run.flights.Add($"{airT:0.00} s, {Vector2.Distance(new Vector2(takeoff.x, takeoff.z), new Vector2(pos.x, pos.z)):0} m from ({takeoff.x:0},{takeoff.z:0}), landing at {car.Body.linearVelocity.magnitude:0.0} m/s"); airT = 0f; }
                if (shortcut && !onFoot && Physics.Raycast(pos, Vector3.down, out var hh, 2f, ~0, QueryTriggerInteraction.Ignore) && hh.collider.name.StartsWith("sc2 ramp")) { onFoot = true; run.footSpeed = car.Body.linearVelocity.magnitude; }
                if (onFoot) run.minAfterFoot = Mathf.Min(run.minAfterFoot, car.Body.linearVelocity.magnitude);
                run.maxTilt = Mathf.Max(run.maxTilt, Vector3.Angle(car.transform.up, Vector3.up));
                slow = car.Body.linearVelocity.magnitude < 1.5f ? slow + dt : 0f;
                if (slow > 2.5f) { run.result = $"STUCK at ({pos.x:0},{pos.z:0})"; break; }
                if (Vector3.Angle(car.transform.up, Vector3.up) > 75f) { run.result = $"ROLLED at ({pos.x:0},{pos.z:0})"; break; }
                float sd = Vector3.Dot(pos - finish, f1);
                if (prevSide < 0f && sd >= 0f && Vector3.Distance(Flat(pos), Flat(finish)) < 20f) { run.time = t; run.result = "finished"; break; }
                prevSide = sd;
            }
            car.enabled = true;
        }

        // ---------------------------------------------------------------- trees, reset

        IEnumerator Trees()
        {
            var field = FindAnyObjectByType<TreeField>();
            var tt = TreeTrunks.Instance;
            if (!field || !tt) { report.AppendLine("trees: no TreeField / TreeTrunks"); yield break; }
            // a yard tree in Chastain Glen with a clear 25 m run-in: the first thing a car-sized box swept toward it meets is its trunk
            Vector3 tree = Vector3.zero, from = Vector3.zero, dir = Vector3.zero; bool found = false;
            var refPt = new Vector3(2900f, 0f, 900f);
            var order = Enumerable.Range(0, field.count).Select(i => TreeField.Unpack(field.data, i)).Where(p => Mathf.Abs(p.x - refPt.x) < 250f && Mathf.Abs(p.z - refPt.z) < 250f && p.trunkW >= 30).OrderBy(p => (new Vector3(p.x, 0, p.z) - refPt).sqrMagnitude).Take(400).ToList();
            foreach (var p in order)
            {
                var tp = new Vector3(p.x, p.y, p.z);
                for (int a = 0; a < 8 && !found; a++)
                {
                    var d = Quaternion.Euler(0f, a * 45f, 0f) * Vector3.forward;
                    var st = RaceRoute.Ground(tp.x - d.x * 25f, tp.z - d.z * 25f);
                    if (Mathf.Abs(st.y - tp.y) > 2.5f) continue;
                    tt.Refresh(st);
                    if (Physics.BoxCast(st + Vector3.up * 1.0f, new Vector3(0.95f, 0.4f, 0.5f), d, out var hit, Quaternion.LookRotation(d), 26f, ~0, QueryTriggerInteraction.Ignore)
                        && TreeTrunks.IsTrunk(hit.collider) && Vector2.Distance(new Vector2(hit.collider.transform.position.x, hit.collider.transform.position.z), new Vector2(tp.x, tp.z)) < 0.3f && hit.distance > 18f)
                    { tree = tp; from = st; dir = d; found = true; }
                }
                if (found) break;
            }
            if (!found) { report.AppendLine("trees: no yard tree with a clear 25 m run-in found near the check point"); yield break; }
            respawn.PlaceAt(from + Vector3.up * 0.3f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
            yield return new WaitForFixedUpdate();
            car.enabled = false; car.Body.linearVelocity = dir * 20f;
            float t = 0f, before = 0f; bool hitTree = false; string what = "";
            var probe = car.gameObject.AddComponent<ContactNote>();
            while (t < 4f)
            {
                yield return new WaitForFixedUpdate();
                t += Time.fixedDeltaTime;
                if (!hitTree) before = car.Body.linearVelocity.magnitude;
                var aim = Flat(tree) - Flat(car.transform.position);
                float steer = hitTree ? 0f : Mathf.Clamp(Vector3.SignedAngle(Flat(car.transform.forward), aim, Vector3.up) / 15f, -1f, 1f);
                car.Simulate(car.ForwardSpeed < 20f && !hitTree ? 1f : 0f, 0f, steer, Time.fixedDeltaTime);
                if (probe.last != null && !hitTree) { hitTree = true; what = probe.last; }
                if (hitTree && car.Body.linearVelocity.magnitude < 0.5f) break;
            }
            var stopAt = car.transform.position;
            report.AppendLine($"tree at ({tree.x:0.0}, {tree.z:0.0}), driven into at 20 m/s: first contact '{what}' at {before:0.0} m/s, speed {car.Body.linearVelocity.magnitude:0.0} m/s after {t:0.0} s, " +
                              $"{Vector2.Distance(new Vector2(stopAt.x, stopAt.z), new Vector2(tree.x, tree.z)):0.0} m from the trunk, tilt {Vector3.Angle(car.transform.up, Vector3.up):0} deg");
            yield return Shoot("r10_tree_stopped", stopAt - dir * 7f + Vector3.up * 3f, stopAt + dir * 2f, 55f);
            var p0 = car.transform.position;
            for (float r = 0f; r < 2f; r += Time.fixedDeltaTime) { yield return new WaitForFixedUpdate(); car.Simulate(0f, 1f, 0f, Time.fixedDeltaTime); }
            report.AppendLine($"    reversed for 2 s: {Vector3.Distance(p0, car.transform.position):0.0} m back, {(probe.last == what ? "free of the tree" : "touched " + probe.last)}");
            Destroy(probe);
            car.enabled = true;
            // reset, not racing: back to the last stable spot unless that faces into the tree within 6 m, then the nearest road
            respawn.PlaceAt(tree - dir * 4f + Vector3.up * 0.3f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);   // a stable spot facing the tree, 4 m off
            yield return new WaitForFixedUpdate();
            bool ok = CarRecovery.Find(respawn, out var rp, out var ry);
            var rf = Quaternion.Euler(0f, ry, 0f) * Vector3.forward;
            bool facingTree = Physics.SphereCast(rp + Vector3.up * 0.8f, 0.9f, rf, out var fh, 6f, ~0, QueryTriggerInteraction.Ignore) && TreeTrunks.IsTrunk(fh.collider);
            RoadLookup.Instance.Nearest(rp, out var rn, out var rd, out _);
            report.AppendLine($"    reset (free roam) from a spot facing the tree 4 m off: {(ok ? $"to ({rp.x:0.0}, {rp.z:0.0}) on {rn} ({rd:0.0} m from its centre), heading {ry:0}, {(facingTree ? "FACING A TREE" : "clear ahead")}" : "no spot found, last stable spot used")}");
            // reset in a race: the nearest point of the lap, facing the race direction
            race.StartRace();
            yield return null;
            respawn.PlaceAt(tree - dir * 2f + Vector3.up * 0.3f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
            yield return new WaitForFixedUpdate();
            ok = CarRecovery.Find(respawn, out rp, out ry);
            float s = race.Route.LapStationOf(rp); var lp = race.Route.PointAt(race.Route.GateS + s, out var lf);
            float yawErr = Mathf.Abs(Mathf.DeltaAngle(ry, Mathf.Atan2(lf.x, lf.z) * Mathf.Rad2Deg));
            report.AppendLine($"    reset in a race from beside the tree: {(ok ? $"to the lap {s:0} m after the gate, {Vector2.Distance(new Vector2(rp.x, rp.z), new Vector2(lp.x, lp.z)):0.0} m from its centre, {yawErr:0} deg off the race direction" : "no spot found")}");
            race.EndRace();
            yield return null;
        }

        sealed class ContactNote : MonoBehaviour
        {
            public string last;
            void OnCollisionEnter(Collision c) { if (!(c.collider is TerrainCollider)) last = (c.collider.transform.parent ? c.collider.transform.parent.name + "/" : "") + c.collider.name; }
        }

        // ---------------------------------------------------------------- a lamp, a hydrant

        IEnumerator Breakable(string rootName, string label)
        {
            var root = GameObject.Find(rootName);
            if (!root) { report.AppendLine($"{label}: no {rootName} in the scene"); yield break; }
            var panels = root.GetComponentsInChildren<FencePanel>();
            // the one furthest from Dan's house (positions near it are never written down) with a clear 20 m run-in along its road
            var dan = GameObject.Find("DanHouse");
            Vector3 danAt = dan ? dan.transform.position : new Vector3(1e6f, 0, 1e6f);
            foreach (var fp in panels.OrderByDescending(p => (p.transform.position - danAt).sqrMagnitude))
            {
                var at = fp.transform.position;
                if (!RoadLookup.Instance.NearestPoint(at, out var c, out var tan)) continue;
                var d = new Vector3(tan.x, 0f, tan.y);
                var st = RaceRoute.Ground(at.x - d.x * 20f, at.z - d.z * 20f);
                if (TreeTrunks.Instance) TreeTrunks.Instance.Refresh(st);
                if (Physics.BoxCast(st + Vector3.up * 1.0f, new Vector3(0.95f, 0.4f, 0.5f), d, out _, Quaternion.LookRotation(d), 22f, ~0, QueryTriggerInteraction.Ignore)) continue;
                int broken0 = FenceChunk.TotalBroken();
                respawn.PlaceAt(st + Vector3.up * 0.3f, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
                yield return new WaitForFixedUpdate();
                car.enabled = false; car.Body.linearVelocity = d * 20f;
                float t = 0f, minAfter = 99f, before = 20f, maxTilt = 0f; bool past = false;
                while (t < 2.2f)
                {
                    yield return new WaitForFixedUpdate();
                    t += Time.fixedDeltaTime;
                    float along = Vector3.Dot(car.transform.position - at, d);
                    if (along < -1.5f) before = car.Body.linearVelocity.magnitude;
                    if (along > 0f && along < 6f) { past = true; minAfter = Mathf.Min(minAfter, car.Body.linearVelocity.magnitude); }
                    maxTilt = Mathf.Max(maxTilt, Vector3.Angle(car.transform.up, Vector3.up));
                    float steer = Mathf.Clamp(Vector3.SignedAngle(Flat(car.transform.forward), d, Vector3.up) / 20f, -1f, 1f);
                    car.Simulate(along < -1.5f && car.ForwardSpeed < 20f ? 1f : 0f, 0f, steer, Time.fixedDeltaTime);   // no throttle from just before it, so the cost shows
                }
                car.enabled = true;
                int broke = FenceChunk.TotalBroken() - broken0;
                report.AppendLine($"{label} at ({at.x:0.0}, {at.z:0.0}): driven into at 20 m/s along its road: {broke} broken; {before:0.0} m/s before it, lowest {minAfter:0.0} m/s past it ({(1f - minAfter / Mathf.Max(before, 0.1f)) * 100f:0}% lower), most tilt {maxTilt:0} deg, {(past && minAfter > 1f ? "kept going" : "STOPPED")}");
                if (label == "hydrant")
                {
                    yield return new WaitForSecondsRealtime(4f);
                    var water = FindAnyObjectByType<HydrantWater>();
                    int particles = water ? water.GetComponentsInChildren<ParticleSystem>().Sum(ps => ps.particleCount) : 0;
                    report.AppendLine($"    water: {HydrantWater.Running} jet(s) running, {particles} particles after 4 s");
                    var eye = RaceRoute.Ground(at.x - d.x * 40f, at.z - d.z * 40f) + Vector3.up * 1.7f;
                    car.transform.position = new Vector3(3200f, 400f, 900f); car.Body.isKinematic = true;
                    yield return Shoot("r10_hydrant_water_from_down_the_street", eye, at + Vector3.up * 3f, 50f);
                    var eye2 = at + new Vector3(-d.z, 0f, d.x) * 9f - d * 6f + Vector3.up * 2.5f;
                    yield return Shoot("r10_hydrant_water_close", eye2, at + Vector3.up * 2f, 60f);
                    car.Body.isKinematic = false;
                }
                else
                {
                    var eye = at - d * 8f + new Vector3(-d.z, 0f, d.x) * 5f + Vector3.up * 2.5f;
                    yield return Shoot("r10_lamp_knocked_over", eye, at, 60f);
                }
                FenceChunk.RestoreAll();
                yield break;
            }
            report.AppendLine($"{label}: none with a clear 20 m run-in found");
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static string Fmt(Vector3 v) => $"({v.x:0.0}, {v.y:0.0}, {v.z:0.0})";
    }
}
