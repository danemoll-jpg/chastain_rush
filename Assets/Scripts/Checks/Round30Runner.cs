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
    // Round 30 check in the exe: ChastainRush.exe -exeRound30 <dir> -traffic Normal [-r30Only fps,drive,hit,race,pics]
    // -logFile <dir>\Player.log. Muted (CheckRun), its files in <dir>. Part A (traffic): the frame rate at the busiest place
    // (Canton Road by the shops) with traffic Off / Normal / Heavy, the cars' driving for 90 s (where they are, stops at
    // junctions, stalls, recycles), the player running into one (knocked, horn), a race (its roads closed to traffic), pictures.
    public sealed class Round30Runner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        int fails, checks;
        ArcadeVehicle car; CameraViews views;
        string only = "";

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); }
        void Note(string line) { report.AppendLine("      " + line); }
        bool Want(string part) => only.Length == 0 || only.Split(',').Contains(part);

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-r30Only") only = args[i + 1];
            report.AppendLine($"Round 30 check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height} (frame rates here are not 3840 x 2160 numbers), muted.");
            yield return new WaitForSecondsRealtime(4f);
            car = null; foreach (var v in FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)) { var vi = v.GetComponent<VehicleInput>(); if (vi && vi.enabled) car = v; }   // the player (not a traffic car)
            views = FindAnyObjectByType<CameraViews>();
            if (DeveloperHud.Instance) DeveloperHud.Instance.Visible = false;
            var traffic = Traffic.Instance; var net = TrafficNetwork.Instance;
            Check("traffic network in the scene", net && net.EdgeCount > 100, net ? $"{net.EdgeCount} edges, {net.NodeCount} nodes" : "none");
            Check("traffic running", traffic, traffic ? "level " + Traffic.LevelNames[traffic.Level] : "");
            if (!traffic || !net) { Finish(); yield break; }

            if (Want("fps")) yield return Fps(traffic);
            if (Want("drive")) yield return Drive(traffic, net);
            if (Want("hit")) yield return Hit(traffic);
            if (Want("race")) yield return Race(traffic, net);
            if (Want("pics")) yield return Pics(traffic);
            if (only.Split(',').Contains("houses")) yield return Houses(traffic);   // Part B (only when asked)
            Finish();
        }

        void Finish()
        {
            report.AppendLine();
            report.AppendLine(fails == 0 ? $"ALL {checks} CHECKS PASSED" : $"{fails} OF {checks} CHECKS FAILED");
            File.WriteAllText(Path.Combine(outputDir, "round30_traffic.txt"), report.ToString());
            Debug.Log("[Round30]\n" + report);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        void PlaceCar(float x, float z, float yaw)
        {
            float y = RaceRoute.Surface(x, z).y;
            if (Physics.Raycast(new Vector3(x, 900, z), Vector3.down, out var hit, 1200f, ~0, QueryTriggerInteraction.Ignore)) y = hit.point.y;
            var rot = Quaternion.Euler(0, yaw, 0); var pos = new Vector3(x, y + .8f, z);
            car.Body.position = pos; car.Body.rotation = rot; car.transform.SetPositionAndRotation(pos, rot);
            car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero; car.ClearSteering();
        }

        // the frame rate at Canton Road by the shops (the gas station side), standing still, the chase camera along the road
        IEnumerator Fps(Traffic traffic)
        {
            report.AppendLine(); report.AppendLine("Frame rate, Canton Road at the shops (beside it at x 3503, z 1166, looking along it), standing, 1280 x 720, VSync and the frame cap off for the measurement:");
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            var results = new List<string>();
            foreach (int level in new[] { 0, 2, 3 })
            {
                Traffic.ForcedLevel = level;
                PlaceCar(3502.8f, 1166.1f, -13);
                yield return new WaitForSecondsRealtime(level == 0 ? 6f : 25f);   // the cars come in round the player
                PlaceCar(3502.8f, 1166.1f, -13);
                float t = 0; int frames = 0; float worst = 0;
                while (t < 10f) { yield return null; float dt = Time.unscaledDeltaTime; t += dt; frames++; worst = Mathf.Max(worst, dt); }
                int near = traffic.Cars.Count(c => c && c.gameObject.activeSelf && (c.Car.Body.position - car.Body.position).magnitude < 250);
                string line = $"{Traffic.LevelNames[level],-7} {frames / t,6:0.0} fps average, worst frame {worst * 1000:0} ms; cars {traffic.Cars.Count} ({near} within 250 m)";
                Note(line); results.Add(line);
                yield return Shot($"fps_{Traffic.LevelNames[level].ToLower()}");
            }
            Traffic.ForcedLevel = 2; QualitySettings.vSyncCount = 1; Application.targetFrameRate = 60;
        }

        // 90 s of free roam traffic round the spawn: where the cars are, what they do
        IEnumerator Drive(Traffic traffic, TrafficNetwork net)
        {
            report.AppendLine(); report.AppendLine("Traffic driving, 90 s, Normal, the player parked beside Bells Ferry Road:");
            Traffic.ForcedLevel = 2;
            PlaceCar(1305.6f, 1492.5f, -27);
            yield return new WaitForSecondsRealtime(20f);
            var states = new Dictionary<string, int>(); var roads = new HashSet<string>(); var edges = new Dictionary<TrafficDriver, HashSet<int>>();
            int samples = 0, offRoad = 0, recycled0 = traffic.Recycled; float maxSpeed = 0;
            for (float t = 0; t < 90f; t += .5f)
            {
                foreach (var d in traffic.Cars)
                {
                    if (!d || !d.gameObject.activeSelf) continue;
                    samples++; states[d.State] = states.TryGetValue(d.State, out int n) ? n + 1 : 1;
                    if (d.Edge >= 0) { roads.Add((net.Name(d.Edge).Length > 0 ? net.Name(d.Edge) : "unnamed") + " (" + net.ClassName(d.Edge) + ")"); if (!edges.TryGetValue(d, out var set)) edges[d] = set = new HashSet<int>(); set.Add(d.Edge); }
                    var p = d.Car.Body.position; if (d.Edge >= 0 && net.Distance(d.Edge, new Vector2(p.x, p.z)) > 9f) offRoad++;
                    maxSpeed = Mathf.Max(maxSpeed, d.Car.ForwardSpeed);
                }
                yield return new WaitForSeconds(.5f);
            }
            Note($"cars {traffic.Cars.Count}; samples {samples}; states " + string.Join(", ", states.Select(kv => $"{kv.Key} {kv.Value * 100 / Mathf.Max(1, samples)}%")));
            Note($"roads driven ({roads.Count}): " + string.Join("; ", roads.Take(30)));
            Note($"road pieces per car (junctions passed): " + string.Join(", ", edges.Values.Select(s => (s.Count - 1).ToString())));
            Note($"fastest {maxSpeed:0.0} m/s; recycled in the 90 s {traffic.Recycled - recycled0}; samples more than 9 m off their road {offRoad}");
            Check("traffic drives (most samples moving)", states.TryGetValue("drive", out int dr) && dr > samples / 2, $"drive {dr} of {samples}");
            Check("cars pass junctions (most cars on 2 or more road pieces)", edges.Values.Count(s => s.Count >= 2) >= edges.Count / 2, $"{edges.Values.Count(s => s.Count >= 2)} of {edges.Count}");
            Check("cars keep to their roads (under 5% of samples 9 m off)", offRoad < samples / 20 + 1, $"{offRoad} of {samples}");
            yield return Shot("drive_bells_ferry");
        }

        // the player into a traffic car at 15 m/s: it is knocked and sounds its horn
        IEnumerator Hit(Traffic traffic)
        {
            report.AppendLine(); report.AppendLine("The player runs into a traffic car:");
            TrafficDriver target = null;
            for (float t = 0; t < 30 && !target; t += 1f)
            {
                target = traffic.Cars.Where(c => c && c.gameObject.activeSelf && c.Car.ForwardSpeed > 4).OrderBy(c => (c.Car.Body.position - car.Body.position).sqrMagnitude).FirstOrDefault();
                if (!target) yield return new WaitForSeconds(1f);
            }
            if (!target) { Check("a traffic car to hit", false); yield break; }
            int horns = Traffic.Horns;
            // from behind, in its lane, 10 m/s faster than it
            var tp = target.Car.Body.position; var fwd = target.transform.forward;
            var start = tp - fwd * 9f; var rot = Quaternion.LookRotation(fwd, Vector3.up);
            car.Body.position = start + Vector3.up * .3f; car.Body.rotation = rot; car.transform.SetPositionAndRotation(car.Body.position, rot);
            car.Body.linearVelocity = target.Car.Body.linearVelocity + fwd * 10f; car.Body.angularVelocity = Vector3.zero;
            var before = target.Car.Body.position;
            float closest = 99;
            for (float t = 0; t < 3f; t += Time.fixedDeltaTime) { closest = Mathf.Min(closest, (car.Body.position - target.Car.Body.position).magnitude); yield return new WaitForFixedUpdate(); }
            Note($"target {target.name}, closest {closest:0.0} m, state after: {target.State}, horns {Traffic.Horns - horns}");
            Check("the traffic car reacts (horn)", Traffic.Horns > horns, $"horns {Traffic.Horns - horns}");
            yield return Shot("hit");
            yield return new WaitForSecondsRealtime(6f);
            Check("it drives on afterwards (not stuck upside down)", target.transform.up.y > .5f, $"up.y {target.transform.up.y:0.00}, state {target.State}");
        }

        // a race on the Chastain Glen loop: its roads closed to traffic; no traffic car near its line during it
        IEnumerator Race(Traffic traffic, TrafficNetwork net)
        {
            report.AppendLine(); report.AppendLine("A race (the Chastain Glen loop, 3 AI drivers), traffic Normal:");
            var race = RaceController.Instance; if (!race) { Check("race controller", false); yield break; }
            RaceController.AiCount = 3; race.StartRace();
            yield return new WaitForSeconds(1f);
            Check("roads closed to traffic in the race", traffic.ClosedRoads > 0, $"{traffic.ClosedRoads} road pieces");
            var rd = race.RouteData; float nearest = 999; int onClosed = 0, samples = 0;
            for (float t = 0; t < 40; t += 1f)
            {
                foreach (var d in traffic.Cars)
                {
                    if (!d || !d.gameObject.activeSelf) continue; samples++;
                    var p = d.Car.Body.position; var q = new Vector2(p.x, p.z);
                    for (int i = 0; i + 1 < rd.loop.Length; i += 2) nearest = Mathf.Min(nearest, (new Vector2(rd.loop[i], rd.loop[i + 1]) - q).magnitude);
                    if (d.Edge >= 0 && traffic.Forbidden(d.Edge)) onClosed++;
                }
                yield return new WaitForSeconds(1f);
            }
            Note($"cars {traffic.Cars.Count}; nearest any came to the race's line {nearest:0} m; samples on a closed road {onClosed} of {samples}");
            Check("no traffic on the race's roads", nearest > 10 && onClosed == 0, $"nearest {nearest:0} m");
            race.EndRace();
            yield return new WaitForSeconds(1f);
            Check("free roam: every road open again", traffic.ClosedRoads == 0, traffic.ClosedRoads.ToString());
        }

        // pictures: behind traffic on Canton Road, a junction, a neighborhood street; then three from a camera behind traffic cars
        IEnumerator Pics(Traffic traffic)
        {
            Traffic.ForcedLevel = 3;
            PlaceCar(3502.8f, 1166.1f, -13); yield return new WaitForSecondsRealtime(20f);
            int k = 0;
            foreach (var d in traffic.Cars.Where(c => c && c.gameObject.activeSelf && c.Car.ForwardSpeed > 5).OrderBy(c => (c.Car.Body.position - car.Body.position).sqrMagnitude).Take(3).ToList())
            {
                var cam = new GameObject("Traffic picture camera").AddComponent<Camera>(); cam.depth = 100; cam.fieldOfView = 55;
                for (float t = 0; t < 1.2f; t += Time.deltaTime)
                {
                    var p = d.Car.Body.position; var f = d.transform.forward;
                    cam.transform.SetPositionAndRotation(p - f * 10f + Vector3.up * 3.6f, Quaternion.LookRotation(p + f * 12f - (p - f * 10f + Vector3.up * 3.6f)));
                    yield return null;
                }
                yield return Shot($"pic_follow_{k++}_{(TrafficNetwork.Instance.Name(d.Edge).Length > 0 ? TrafficNetwork.Instance.Name(d.Edge).Replace(' ', '_') : "unnamed")}");
                Destroy(cam.gameObject);
            }
            Traffic.ForcedLevel = 3;
            foreach (var (name, x, z, yaw) in new[] { ("canton_road", 3502.8f, 1166.1f, -13f), ("chastain_road", 1278.2f, 602.1f, -68f), ("new_chastain", 1649.3f, 604.5f, 59f), ("bells_ferry", 1305.6f, 1492.5f, -27f) })
            {
                PlaceCar(x, z, yaw); yield return new WaitForSecondsRealtime(20f); PlaceCar(x, z, yaw);
                if (views) views.SetView(CameraViews.View.FarChase);
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Shot("pic_" + name);
                if (views) views.SetView(CameraViews.View.Chase);
            }
        }

        // Part B (B4): the frame rate at two street views in the most built-up neighborhoods (Guidhall Trail, Lakeside), traffic
        // off, VSync and the frame cap off; the chase view and the far chase view at each
        IEnumerator Houses(Traffic traffic)
        {
            Traffic.ForcedLevel = 0; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            report.AppendLine(); report.AppendLine("Frame rate over the most built-up neighborhoods, traffic Off, 1280 x 720, VSync and the frame cap off:");
            foreach (var (name, x, z, yaw) in new[] { ("guidhall_cambridge_drive", 2878.8f, 1553.0f, 107.5f), ("lakeside_chastain_lakes_drive", 917.9f, 1172.2f, 114.1f) })
                foreach (var view in new[] { CameraViews.View.Chase, CameraViews.View.FarChase })
                {
                    PlaceCar(x, z, yaw); if (views) views.SetView(view);
                    yield return new WaitForSecondsRealtime(4f);
                    float t = 0; int frames = 0; float worst = 0;
                    while (t < 8f) { yield return null; float dt = Time.unscaledDeltaTime; t += dt; frames++; worst = Mathf.Max(worst, dt); }
                    Note($"{name,-32} {view,-9} {frames / t,6:0.0} fps average, worst frame {worst * 1000:0} ms");
                    yield return Shot($"houses_{name}_{view.ToString().ToLower()}");
                }
            if (views) views.SetView(CameraViews.View.Chase);
            QualitySettings.vSyncCount = 1; Application.targetFrameRate = 60;
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(0.3f);
            yield return new WaitForEndOfFrame();
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }
    }
}
