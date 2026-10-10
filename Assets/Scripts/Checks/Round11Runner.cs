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
    // Round 11: one targeted check in the built exe. ChastainRush.exe -exeRound11 <dir> (added by GameShell):
    //   #2 (item 1)   house 1763 from Dan's BUG-013 spot and from its north side, before and after the glass breaks; the line
    //                 driven by the real car (ArcadeVehicle.Simulate) from the lane at racing speed and slow: where each
    //                 window broke and at what speed, the flights, the time against the road
    //   #6 (item 3)   the line driven at 27, 21 and 14 m/s: the flight over 1641, the touchdown on the ground behind it (speed
    //                 kept?), finished or not
    //   monuments     (item 10) from a car turning into the entrance from New Chastain Road
    // Dan's house and lot are hidden for every shot. Then the exe quits.
    public sealed class Round11Runner : MonoBehaviour
    {
        public string outputDir;
        ArcadeVehicle car; VehicleRespawn respawn; RaceController race;
        readonly StringBuilder report = new StringBuilder();
        readonly List<(Vector3 at, float speed, string chunk)> breaks = new List<(Vector3, float, string)>();

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"Round 11 exe check, {DateTime.Now:yyyy-MM-dd HH:mm:ss}, screen {Screen.width}x{Screen.height}, {(Application.isEditor ? "Editor" : "built exe")}");
            yield return new WaitForSecondsRealtime(4f);
            car = FindAnyObjectByType<ArcadeVehicle>(); respawn = car.GetComponent<VehicleRespawn>(); race = RaceController.Instance;
            FenceChunk.PanelBroken += OnBroken;
            SetDan(false);
            yield return Shortcut2();
            yield return Shortcut6();
            yield return Monuments();
            SetDan(true);
            report.AppendLine($"RampHold: {RampHold.Landings} landings carried, {RampHold.HeldSteps} held steps over the whole check");
            File.WriteAllText(Path.Combine(outputDir, "round11_check.txt"), report.ToString());
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

        void Park() { car.transform.position = new Vector3(3200f, 400f, 900f); car.Body.linearVelocity = Vector3.zero; car.enabled = false; car.Body.isKinematic = true; }
        void Unpark() { car.Body.isKinematic = false; car.enabled = true; }

        // ---------------------------------------------------------------- #2

        IEnumerator Shortcut2()
        {
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "sc2_two_houses");
            if (sc == null) { report.AppendLine("shortcut #2: not in the route"); yield break; }
            Vector3 aIn = PanelAt("House 1763 shortcut wall (in)"), aOut = PanelAt("House 1763 shortcut wall (out)");
            report.AppendLine($"shortcut #2 windows of 1763: in {Fmt(aIn)}, out {Fmt(aOut)}");
            FenceChunk.RestoreAll(); Park();
            yield return null;
            // Dan's BUG-013 view (his camera: position and rotation from the report) and the north side
            var danEye = new Vector3(3085.30f, 318.17f, 1039.04f); var danLook = danEye + Quaternion.Euler(15.44f, 312.55f, 0f) * Vector3.forward * 10f;
            var north = aOut + (aOut - aIn).normalized * 14f; north.y = RaceRoute.Ground(north.x, north.z).y + 1.7f;
            var shots = new List<(string, Vector3, Vector3, float)> { ("r11_1763_from_dan_bug013", danEye, danLook, 60f), ("r11_1763_side_window_from_behind", north, aOut, 55f) };
            foreach (var s in shots) yield return Shoot(s.Item1 + "_before", s.Item2, s.Item3, s.Item4);
            Unpark();
            float leave = race.Route.LapStationOf(sc.Start), rejoin = race.Route.LapStationOf(sc.End);
            foreach (var (label, v) in new[] { ("racing speed", 30f), ("slow", 13f) })
            {
                FenceChunk.RestoreAll();
                var road = new Run(); yield return Drive(sc, leave, rejoin, v, false, road, "sc2 ramp");
                FenceChunk.RestoreAll(); breaks.Clear();
                var run = new Run(); yield return Drive(sc, leave, rejoin, v, true, run, "sc2 ramp");
                report.AppendLine($"shortcut #2, {label} (target {v:0} m/s): {run.result}, {run.time:0.00} s against {road.time:0.00} s by the road ({road.time - run.time:+0.00;-0.00} s); speed at the ramp's foot {run.footSpeed:0.0} m/s");
                foreach (var b in breaks) report.AppendLine($"    broken: {b.chunk} at {b.speed:0.0} m/s");
                foreach (var f in run.flights) report.AppendLine($"    flight: {f}");
                report.AppendLine($"    lowest speed after the ramp's foot {run.minAfterFoot:0.0} m/s; most tilt {run.maxTilt:0} deg; things broken {breaks.Count}; touched: {string.Join(", ", run.touched.Distinct())}");
                if (label == "racing speed")
                {
                    Park(); yield return null;
                    foreach (var s in shots) yield return Shoot(s.Item1 + "_after", s.Item2, s.Item3, s.Item4);
                    Unpark();
                }
            }
            FenceChunk.RestoreAll();
        }

        // ---------------------------------------------------------------- #6

        IEnumerator Shortcut6()
        {
            var sc = race.Route.Shortcuts.FirstOrDefault(s => s.Id == "sc6_over_house");
            if (sc == null) { report.AppendLine("shortcut #6: not in the route"); yield break; }
            float leave = race.Route.LapStationOf(sc.Start), rejoin = race.Route.LapStationOf(sc.End);
            var road = new Run(); yield return Drive(sc, leave, rejoin, 27f, false, road, "sc6 kicker");
            foreach (float v in new[] { 27f, 21f, 14f })
            {
                FenceChunk.RestoreAll(); breaks.Clear(); int landings0 = RampHold.Landings;
                var run = new Run(); yield return Drive(sc, leave, rejoin, v, true, run, "sc6 kicker");
                report.AppendLine($"shortcut #6 at {v:0} m/s: {run.result}, {run.time:0.00} s against {road.time:0.00} s by the road at 27 m/s; speed at the kicker's foot {run.footSpeed:0.0} m/s; landings carried {RampHold.Landings - landings0}");
                foreach (var f in run.flights) report.AppendLine($"    flight: {f}");
                report.AppendLine($"    lowest speed after the kicker's foot {run.minAfterFoot:0.0} m/s; most tilt {run.maxTilt:0} deg; things broken {breaks.Count}; touched: {string.Join(", ", run.touched.Distinct())}");
            }
            FenceChunk.RestoreAll();
        }

        // ---------------------------------------------------------------- the monuments

        IEnumerator Monuments()
        {
            Park(); yield return null;
            // a car on New Chastain Road's north carriageway about to turn in, and one in the mouth of the entrance (Dan's BUG-012 view)
            var turnEye = RaceRoute.Ground(2924f, 757f) + Vector3.up * 1.6f;
            yield return Shoot("r11_monuments_turning_in", turnEye, RaceRoute.Ground(2940f, 785f) + Vector3.up * 1.5f, 60f);
            var danEye = new Vector3(2945.91f, 298.18f, 750.88f);
            yield return Shoot("r11_monuments_from_dan_bug012", danEye, danEye + Quaternion.Euler(15.44f, 343.22f, 0f) * Vector3.forward * 10f, 60f);
            Unpark();
            report.AppendLine("monuments: shots from a car turning in and from Dan's BUG-012 view");
        }

        // ---------------------------------------------------------------- shots and drives (as Round10Runner)

        IEnumerator Shoot(string name, Vector3 eye, Vector3 look, float fov)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            var go = new GameObject("Round11 camera"); var cam = go.AddComponent<Camera>();
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

        sealed class Run { public string result = "timeout"; public float time, footSpeed, minAfterFoot = 99f, maxTilt; public List<string> flights = new List<string>(); public List<string> touched = new List<string>(); }

        sealed class ContactNote : MonoBehaviour
        {
            public List<string> hits = new List<string>();
            void OnCollisionEnter(Collision c)
            {
                if (c.collider is TerrainCollider) return;
                string n = (c.collider.transform.parent ? c.collider.transform.parent.name + "/" : "") + c.collider.name;
                if (n.Contains("open: shortcut passage") && c.contactCount > 0)
                {
                    var cp = c.GetContact(0); var bc = c.collider as BoxCollider;
                    var local = c.collider.transform.InverseTransformPoint(cp.point);
                    n += $" (contact at y {cp.point.y:0.00}, car at y {transform.position.y:0.00}, box centre y {c.collider.bounds.center.y:0.00} size {(bc ? bc.size.ToString("0.00") : "?")}, local {local.ToString("0.00")}, normal {cp.normal.ToString("0.00")})";
                }
                hits.Add(n);
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
            int next = 0; float t = 0f, airT = 0f; bool onFoot = false; Vector3 takeoff = Vector3.zero; float slow = 0f, airSpeed = 0f;
            float prevSide = Vector3.Dot(car.transform.position - finish, f1);
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
                bool air = car.GroundedWheels == 0;
                if (air) { if (airT == 0f) { takeoff = pos; airSpeed = car.Body.linearVelocity.magnitude; } airT += dt; }
                else if (airT > 0f)
                {
                    if (airT > 0.25f)
                    {
                        string on = Physics.Raycast(pos + Vector3.up, Vector3.down, out var gh, 3f, ~0, QueryTriggerInteraction.Ignore) ? (gh.collider is TerrainCollider ? "the ground" : gh.collider.name) : "?";
                        run.flights.Add($"{airT:0.00} s, {Vector2.Distance(new Vector2(takeoff.x, takeoff.z), new Vector2(pos.x, pos.z)):0} m from ({takeoff.x:0},{takeoff.z:0}) at {airSpeed:0.0} m/s, down on {on} at {car.Body.linearVelocity.magnitude:0.0} m/s");
                    }
                    airT = 0f;
                }
                if (shortcut && !onFoot && Physics.Raycast(pos, Vector3.down, out var hh, 2f, ~0, QueryTriggerInteraction.Ignore) && hh.collider.name.StartsWith(footPrefix)) { onFoot = true; run.footSpeed = car.Body.linearVelocity.magnitude; }
                if (onFoot) run.minAfterFoot = Mathf.Min(run.minAfterFoot, car.Body.linearVelocity.magnitude);
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
        static string Fmt(Vector3 v) => $"({v.x:0.0}, {v.y:0.0}, {v.z:0.0})";
    }
}
