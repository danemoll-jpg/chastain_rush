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
    // Round 12: one targeted check in the built exe. ChastainRush.exe -exeRound12 <dir> (added by GameShell):
    //   #2 (items 1 to 5)  house 1763 from Dan's BUG-003 camera; the turn-off and its sign from the lane at driving height;
    //                      1627's back (sliding door) and front (upstairs window) with the glass whole and after the jump; a
    //                      side profile of the flight out of 1627 (three frames); the line driven by the real car
    //                      (ArcadeVehicle.Simulate) from the lane at racing speed (braking for the turn-off as the curvature
    //                      needs, like a player) and slow: the speed off 1627's kicker, the flight (height over the yard,
    //                      length), what broke, the time against the road; a car dropped on each roof house's roof, then
    //                      driven straight on: does it get off?
    //   items 6 to 8       Dan's BUG-002, BUG-001 and BUG-006 cameras (sidewalks at drives, the clubhouse, no start gate in
    //                      free roam)
    //   item 9             #6's kicker from the lane, and the line driven once at 27 m/s
    // Dan's house and lot are hidden for every shot. Then the exe quits.
    public sealed class Round12Runner : MonoBehaviour
    {
        public string outputDir;
        ArcadeVehicle car; VehicleRespawn respawn; RaceController race;
        readonly StringBuilder report = new StringBuilder();
        readonly List<(Vector3 at, float speed, string chunk)> breaks = new List<(Vector3, float, string)>();

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"Round 12 exe check, {DateTime.Now:yyyy-MM-dd HH:mm:ss}, screen {Screen.width}x{Screen.height}, {(Application.isEditor ? "Editor" : "built exe")}");
            yield return new WaitForSecondsRealtime(4f);
            car = FindAnyObjectByType<ArcadeVehicle>(); respawn = car.GetComponent<VehicleRespawn>(); race = RaceController.Instance;
            FenceChunk.PanelBroken += OnBroken;
            SetDan(false);
            yield return DanViews();
            yield return Shortcut2();
            yield return Roofs();
            yield return Shortcut6();
            SetDan(true);
            report.AppendLine($"RampHold: {RampHold.Landings} landings carried, {RampHold.HeldSteps} held steps over the whole check");
            File.WriteAllText(Path.Combine(outputDir, "round12_check.txt"), report.ToString());
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

        Vector3 PanelAt(string name)
        {
            foreach (var c in FindObjectsByType<FenceChunk>(FindObjectsSortMode.None))
                if (c.transform.parent && c.transform.parent.name == name) return c.transform.TransformPoint(c.panels[0].centre);
            return Vector3.zero;
        }

        static GameObject Named(string name) => FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == name)?.gameObject;

        void Park() { car.transform.position = new Vector3(3200f, 400f, 900f); car.Body.linearVelocity = Vector3.zero; car.enabled = false; car.Body.isKinematic = true; }
        void Unpark() { car.Body.isKinematic = false; car.enabled = true; }

        static Vector3 Cam(float x, float y, float z, float pitch, float yaw, out Vector3 look)
        {
            var eye = new Vector3(x, y, z); look = eye + Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * 10f; return eye;
        }

        // ---------------------------------------------------------------- Dan's report cameras (free roam: no race running)

        IEnumerator DanViews()
        {
            Park(); yield return null;
            report.AppendLine($"race state during the free-roam shots: {race.State}; start / finish gate showing: {(Named("Start-finish gate") is GameObject g && g.activeInHierarchy)}");
            var e = Cam(3084.57f, 316.61f, 1029.95f, 16.25f, 332.08f, out var l); yield return Shoot("r12_1763_from_dan_bug003", e, l, 60f);
            e = Cam(3085.27f, 315.88f, 1012.37f, 15.44f, 31.66f, out l); yield return Shoot("r12_sidewalks_from_dan_bug002", e, l, 60f);
            e = Cam(2825.05f, 298.28f, 802.69f, 15.44f, 206.72f, out l); yield return Shoot("r12_pool_from_dan_bug001", e, l, 60f);
            e = Cam(2941.00f, 297.41f, 867.86f, 15.44f, 19.02f, out l); yield return Shoot("r12_no_start_gate_from_dan_bug006", e, l, 60f);
            Unpark();
        }

        // ---------------------------------------------------------------- #2

        Vector3 doorAt, windowAt, h2;

        IEnumerator Shortcut2()
        {
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "sc2_two_houses");
            if (sc == null) { report.AppendLine("shortcut #2: not in the route"); yield break; }
            doorAt = PanelAt("House 1627 shortcut wall (in)"); windowAt = PanelAt("House 1627 shortcut wall (out)");
            h2 = Flat(windowAt - doorAt).normalized;
            report.AppendLine($"shortcut #2: 1627's sliding door {Fmt(doorAt)}, upstairs window {Fmt(windowAt)}; house 1763 open for the shortcut: {(PanelAt("House 1763 shortcut wall (in)") != Vector3.zero)}");
            var sign = Named("Shortcut sign face sc2_two_houses");
            report.AppendLine($"shortcut #2 sign: {(sign ? Fmt(sign.transform.position) : "MISSING")}");
            FenceChunk.RestoreAll(); Park(); yield return null;
            var shots = Views(sc, sign);
            foreach (var s in shots) yield return Shoot(s.Item1 + "_before", s.Item2, s.Item3, s.Item4);
            Unpark();
            float leave = race.Route.LapStationOf(sc.Start), rejoin = race.Route.LapStationOf(sc.End);
            foreach (var (label, v) in new[] { ("racing speed", 30f), ("slow", 12f) })
            {
                FenceChunk.RestoreAll();
                var road = new Run(); yield return Drive(sc, leave, rejoin, v, false, road, "sc2 stairs");
                FenceChunk.RestoreAll(); breaks.Clear();
                var run = new Run { profile = label == "racing speed" };
                yield return Drive(sc, leave, rejoin, v, true, run, "sc2 stairs");
                report.AppendLine($"shortcut #2, {label} (target {v:0} m/s): {run.result}, {run.time:0.00} s against {road.time:0.00} s by the road ({road.time - run.time:+0.00;-0.00} s); speed on to 1627's stairs {run.footSpeed:0.0} m/s, off its kicker {run.lipSpeed:0.0} m/s");
                foreach (var b in breaks) report.AppendLine($"    broken: {b.chunk} at {b.speed:0.0} m/s");
                foreach (var f in run.flights) report.AppendLine($"    flight: {f}");
                report.AppendLine($"    speeds: turn-off {run.speedAt[0]:0.0}, side yard {run.speedAt[1]:0.0}, the crest {run.speedAt[2]:0.0}, the hollow {run.speedAt[3]:0.0} m/s; most tilt {run.maxTilt:0} deg; things broken {breaks.Count}; touched: {string.Join(", ", run.touched.Distinct())}");
                if (label == "racing speed")
                {
                    Park(); yield return null;
                    foreach (var s in shots) yield return Shoot(s.Item1 + "_after", s.Item2, s.Item3, s.Item4);
                    Unpark();
                }
            }
            FenceChunk.RestoreAll();
        }

        List<(string, Vector3, Vector3, float)> Views(RaceShortcut sc, GameObject sign)
        {
            var l = new List<(string, Vector3, Vector3, float)>();
            if (sign)
            {
                // the turn-off from the lane at driving height, 25 m and 12 m before the sign (looking along the lane, and at it)
                var face = -sign.transform.forward; face.y = 0f; face.Normalize();   // the plate's front faces the driver
                var road = sign.transform.position + face * 22f; road = RaceRoute.Ground(road.x, road.z) + Vector3.up * 1.3f;
                l.Add(("r12_turnoff_sign_from_lane_22m", road, sign.transform.position, 55f));
                var near = sign.transform.position + face * 11f; near = RaceRoute.Ground(near.x, near.z) + Vector3.up * 1.3f;
                l.Add(("r12_turnoff_sign_from_lane_11m", near, sign.transform.position + Vector3.up * 0.2f, 60f));
                var mouth = PathAt(sc, 62f);
                l.Add(("r12_turnoff_side_yard_from_lane", road, mouth + Vector3.up * 1.5f, 60f));
            }
            var back = doorAt - h2 * 16f; back = RaceRoute.Ground(back.x, back.z) + Vector3.up * 2.2f;
            l.Add(("r12_1627_back_sliding_door", back, doorAt + Vector3.up * 0.3f, 55f));
            var front = windowAt + h2 * 22f + Vector3.Cross(Vector3.up, h2) * 6f; front = RaceRoute.Ground(front.x, front.z) + Vector3.up * 1.7f;
            l.Add(("r12_1627_front_upstairs_window", front, windowAt, 55f));
            return l;
        }

        // ---------------------------------------------------------------- roofs a car may land on

        IEnumerator Roofs()
        {
            foreach (var t in FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(m => m.name.StartsWith("Roof house ")).OrderBy(m => m.name).ToList())
            {
                var b = t.bounds;
                foreach (float yaw in new[] { 0f, 135f })
                {
                    var drop = new Vector3(b.center.x, b.max.y + 2f, b.center.z);
                    respawn.PlaceAt(drop, yaw);
                    yield return new WaitForFixedUpdate();
                    car.enabled = false; car.Body.linearVelocity = Vector3.zero;
                    float t0 = 0f, offAt = -1f;
                    while (t0 < 7f && offAt < 0f)
                    {
                        yield return new WaitForFixedUpdate();
                        t0 += Time.fixedDeltaTime;
                        car.Simulate(t0 > 1.0f ? 1f : 0f, 0f, 0f, Time.fixedDeltaTime);
                        var q = car.transform.position;
                        bool outside = q.x < b.min.x - 0.5f || q.x > b.max.x + 0.5f || q.z < b.min.z - 0.5f || q.z > b.max.z + 0.5f;
                        if (outside && car.GroundedWheels >= 2 && Physics.Raycast(q, Vector3.down, out var gh, 2f, ~0, QueryTriggerInteraction.Ignore) && gh.collider is TerrainCollider) offAt = t0;
                    }
                    var p = car.transform.position;
                    report.AppendLine($"{t.name}: dropped on its roof (heading {yaw:0}), then on the throttle: {(offAt >= 0f ? $"OFF the roof and on the ground after {offAt:0.0} s" : "STILL ON IT after 7 s")} at ({p.x:0.0}, {p.z:0.0}), speed {car.Body.linearVelocity.magnitude:0.0} m/s, tilt {Vector3.Angle(car.transform.up, Vector3.up):0} deg");
                    car.enabled = true;
                }
            }
        }

        // ---------------------------------------------------------------- #6

        IEnumerator Shortcut6()
        {
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "sc6_over_house");
            if (sc == null) { report.AppendLine("shortcut #6: not in the route"); yield break; }
            var kicker = Named("sc6 kicker over house 1641");
            if (kicker)
            {
                Park(); yield return null;
                var kb = kicker.GetComponent<Renderer>().bounds;
                float s0 = race.Route.LapStationOf(sc.Start);
                var lane = race.Route.PointAt(race.Route.GateS + s0 - 5f, out var fw) + Vector3.up * 1.3f;
                yield return Shoot("r12_sc6_kicker_from_lane", lane, kb.center, 60f);
                var side = kb.center + Vector3.Cross(Vector3.up, (kb.center - lane).normalized * 1f).normalized * 16f; side.y = RaceRoute.Ground(side.x, side.z).y + 2.5f;
                yield return Shoot("r12_sc6_kicker_side", side, kb.center, 60f);
                Unpark();
                var mc = kicker.GetComponent<MeshCollider>();
                report.AppendLine($"shortcut #6 kicker: drawn mesh {kicker.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3} triangles (board and bales), collider '{(mc && mc.sharedMesh ? mc.sharedMesh.name : "none")}' {(mc && mc.sharedMesh ? mc.sharedMesh.triangles.Length / 3 : 0)} triangles, bounds {kb.size.ToString("0.0")}");
            }
            float leave = race.Route.LapStationOf(sc.Start), rejoin = race.Route.LapStationOf(sc.End);
            var road = new Run(); yield return Drive(sc, leave, rejoin, 27f, false, road, "sc6 kicker");
            FenceChunk.RestoreAll(); breaks.Clear(); int landings0 = RampHold.Landings;
            var run = new Run(); yield return Drive(sc, leave, rejoin, 27f, true, run, "sc6 kicker");
            report.AppendLine($"shortcut #6 at 27 m/s: {run.result}, {run.time:0.00} s against {road.time:0.00} s by the road; speed at the kicker's foot {run.footSpeed:0.0} m/s; landings carried {RampHold.Landings - landings0}");
            foreach (var f in run.flights) report.AppendLine($"    flight: {f}");
            report.AppendLine($"    most tilt {run.maxTilt:0} deg; touched: {string.Join(", ", run.touched.Distinct())}");
            FenceChunk.RestoreAll();
        }

        // ---------------------------------------------------------------- shots and drives (as Round11Runner)

        IEnumerator Shoot(string name, Vector3 eye, Vector3 look, float fov)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            ShootNow(name, eye, look, fov);
        }

        void ShootNow(string name, Vector3 eye, Vector3 look, float fov)
        {
            var go = new GameObject("Round12 camera"); var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.1f; cam.farClipPlane = 2000f;
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

        sealed class Run
        {
            public string result = "timeout"; public float time, footSpeed, lipSpeed, maxTilt; public bool profile;
            public float[] speedAt = new float[4];
            public List<string> flights = new List<string>(); public List<string> touched = new List<string>();
        }

        sealed class ContactNote : MonoBehaviour
        {
            public List<string> hits = new List<string>();
            void OnCollisionEnter(Collision c)
            {
                if (c.collider is TerrainCollider) return;
                hits.Add((c.collider.transform.parent ? c.collider.transform.parent.name + "/" : "") + c.collider.name);
            }
        }

        IEnumerator Drive(RaceShortcut sc, float leave, float rejoin, float v, bool shortcut, Run run, string footPrefix)
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
            var probe = car.gameObject.AddComponent<ContactNote>();
            int next = 0; float t = 0f, airT = 0f; bool onFoot = false, onStairs = false; Vector3 takeoff = Vector3.zero; float slow = 0f, airSpeed = 0f, apex = 0f;
            int shots = 0; float prevSide = Vector3.Dot(car.transform.position - finish, f1);
            // where the speeds are noted along #2: the turn-off (where the line leaves the street), the side yard, the crest, the hollow
            var marks = shortcut && sc.Id == "sc2_two_houses" ? new[] { PathAt(sc, 44f), PathAt(sc, 60f), PathAt(sc, 72f), doorAt - h2 * 8f } : new Vector3[0];
            var marked = new bool[marks.Length];
            while (t < 60f)
            {
                yield return new WaitForFixedUpdate();
                float dt = Time.fixedDeltaTime; t += dt;
                var pos = car.transform.position; float speed = car.ForwardSpeed;
                int best = next; float bd = float.MaxValue;
                for (int i = next; i < Mathf.Min(path.Count, next + 15); i++) { float d = (Flat(path[i]) - Flat(pos)).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
                next = best;
                float la = 4f + 0.12f * Mathf.Max(speed, 1f), acc = 0f; int k = next;
                while (k < path.Count - 1 && acc < la) { acc += (Flat(path[k + 1]) - Flat(path[k])).magnitude; k++; }
                float steer = Mathf.Clamp(Vector3.SignedAngle(Flat(car.transform.forward), Flat(path[k]) - Flat(pos), Vector3.up) / 18f, -1f, 1f);
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
                for (int m = 0; m < marks.Length; m++) if (!marked[m] && (Flat(pos) - Flat(marks[m])).magnitude < 2.5f) { marked[m] = true; run.speedAt[m] = car.Body.linearVelocity.magnitude; }
                bool air = car.GroundedWheels == 0;
                if (air)
                {
                    if (airT == 0f) { takeoff = pos; airSpeed = car.Body.linearVelocity.magnitude; apex = 0f; if (onStairs) run.lipSpeed = airSpeed; }
                    airT += dt;
                    apex = Mathf.Max(apex, pos.y - RaceRoute.Ground(pos.x, pos.z).y);
                    // the side profile of the flight out of 1627: three frames from 30 m to the side
                    if (run.profile && onStairs && shots < 3 && airT > 0.15f + shots * 0.55f)
                    {
                        var mid = windowAt + h2 * 16f; var eye = mid + Vector3.Cross(h2, Vector3.up) * 34f; eye.y = RaceRoute.Ground(eye.x, eye.z).y + 4f;
                        ShootNow($"r12_sc2_flight_side_profile_{shots + 1}", eye, mid + Vector3.up * 2f, 62f); shots++;
                    }
                }
                else if (airT > 0f)
                {
                    if (airT > 0.25f)
                    {
                        string on = Physics.Raycast(pos + Vector3.up, Vector3.down, out var gh, 3f, ~0, QueryTriggerInteraction.Ignore) ? (gh.collider is TerrainCollider ? "the ground" : gh.collider.name) : "?";
                        string past = shortcut && sc.Id == "sc2_two_houses" && windowAt != Vector3.zero ? $", {Vector3.Dot(Flat(pos - windowAt), h2):0} m past 1627's front wall" : "";
                        run.flights.Add($"{airT:0.00} s, {Vector2.Distance(new Vector2(takeoff.x, takeoff.z), new Vector2(pos.x, pos.z)):0} m from ({takeoff.x:0},{takeoff.z:0}) at {airSpeed:0.0} m/s, up to {apex:0.0} m over the ground, down on {on} at {car.Body.linearVelocity.magnitude:0.0} m/s{past}");
                    }
                    airT = 0f;
                }
                if (!air && Physics.Raycast(pos, Vector3.down, out var hh, 2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    bool st = hh.collider.name.StartsWith(footPrefix);
                    if (shortcut && st && !onFoot) { onFoot = true; run.footSpeed = car.Body.linearVelocity.magnitude; }
                    onStairs = st;
                }
                run.maxTilt = Mathf.Max(run.maxTilt, Vector3.Angle(car.transform.up, Vector3.up));
                slow = car.Body.linearVelocity.magnitude < 1.5f ? slow + dt : 0f;
                if (slow > 2.5f) { run.result = $"STUCK at ({pos.x:0},{pos.z:0})"; break; }
                if (Vector3.Angle(car.transform.up, Vector3.up) > 75f) { run.result = $"ROLLED at ({pos.x:0},{pos.z:0})"; break; }
                float sd = Vector3.Dot(pos - finish, f1);
                if (prevSide < 0f && sd >= 0f && Vector3.Distance(Flat(pos), Flat(finish)) < 20f) { run.time = t; run.result = "finished"; break; }
                prevSide = sd;
            }
            run.touched.AddRange(probe.hits);
            Destroy(probe);
            car.enabled = true;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        static Vector3 PathAt(RaceShortcut sc, float s)   // the point s metres along the shortcut's line (in plan)
        {
            for (int i = 1; i < sc.Path.Length; i++) { float d = (Flat(sc.Path[i]) - Flat(sc.Path[i - 1])).magnitude; if (s <= d) return Vector3.Lerp(sc.Path[i - 1], sc.Path[i], s / d); s -= d; }
            return sc.Path[sc.Path.Length - 1];
        }
        static string Fmt(Vector3 v) => $"({v.x:0.0}, {v.y:0.0}, {v.z:0.0})";
    }
}
