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
    // Round 13, item 9 (ChastainRush.exe -exeRound13 <dir>, GameShell). Every one of Racer's eleven vehicles in turn, swapped
    // where the car stands by the temporary switch (VehicleSwitch.Select, the D-pad's own call): its model is built, it is
    // driven by script (ArcadeVehicle.Simulate, full throttle, steering along the lap) up the steepest 150 m of Chastain Glen
    // Lane from a rolling start at 8 m/s, and its sound is checked while it drives (the engine voices of VehicleAudio playing
    // and audible). A picture of each. Frame rate parked beside the pond (8 s) with one vehicle of each kind: the Street Classic
    // (car), the Needle 600 (motorcycle), the Trail Four (ATV) and the Turf Rocket (mower). The shortcuts are driven per vehicle
    // by -exeShortcut <dir> -vehicle <id>. Never touches the player's saved vehicle or records.
    public sealed class Round13Runner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        int fails, checks;
        ArcadeVehicle car; RaceController race; VehicleSwitch sw;
        static readonly string[] Kinds = { "original", "moto", "atv", "mower" };

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"Round 13 check (vehicles), {DateTime.Now:yyyy-MM-dd HH:mm}. Screen {Screen.width}x{Screen.height}, GPU {SystemInfo.graphicsDeviceName}.");
            yield return new WaitForSecondsRealtime(4f);
            car = FindAnyObjectByType<ArcadeVehicle>(); race = RaceController.Instance; sw = car.GetComponent<VehicleSwitch>();
            Check("the player car has the vehicle switch, Racer's VehicleConfiguration and VehicleAudio", sw && car.GetComponent<VehicleConfiguration>() && car.GetComponent<VehicleAudio>());
            var cfg = car.GetComponent<VehicleConfiguration>();
            Check("the default vehicle is the Street Classic", cfg && cfg.profileId == "original", cfg ? cfg.Profile.Name : "none");
            Check("eleven player vehicles, no police", VehicleProfile.All.Length == 11 && !VehicleProfile.All.Any(p => p.IsPolice), string.Join(", ", VehicleProfile.All.Select(p => p.Name)));

            // the steepest 150 m of the lap (Chastain Glen Lane), climbing in the race's direction
            var r = race.Route; float bestRise = 0f, bestS = 0f;
            for (float s = 0f; s < r.LapLength; s += 5f)
            {
                float rise = r.PointAt(s + 150f, out _).y - r.PointAt(s, out _).y;
                if (rise > bestRise) { bestRise = rise; bestS = s; }
            }
            var climb = new List<Vector3>(); for (float s = bestS; s <= bestS + 190f; s += 3f) climb.Add(r.PointAt(s, out _));
            var bottom = r.PointAt(bestS, out var f0); var top = r.PointAt(bestS + 150f, out var f1);
            report.AppendLine($"Climb: the steepest 150 m of the lap, {bestS:0} to {bestS + 150f:0} m along it, rising {bestRise:0.0} m ({bestRise / 150f * 100f:0.0}% average); from ({bottom.x:0},{bottom.z:0}) to ({top.x:0},{top.z:0}).");
            report.AppendLine();

            var fountain = FindAnyObjectByType<FountainSpray>();
            Vector3 pond = Vector3.zero; float pondYaw = 0f;
            if (fountain)
            {
                float best = float.MaxValue, bs = 0f;
                for (float s = 0; s < r.LapLength; s += 2f) { float d = Flat(r.PointAt(s, out _) - fountain.transform.position).magnitude; if (d < best) { best = d; bs = s; } }
                pond = r.PointAt(bs - 25f, out var pf) + Vector3.up * 0.3f; pondYaw = Mathf.Atan2(pf.x, pf.z) * Mathf.Rad2Deg;
            }

            foreach (var p in VehicleProfile.All)
            {
                report.AppendLine($"=== {p.Name} ({p.Id}, {p.Class})");
                var respawn = car.GetComponent<VehicleRespawn>();
                respawn.PlaceAt(bottom + Vector3.up * 0.3f, Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg);
                yield return new WaitForFixedUpdate();
                sw.Select(p.Id);
                yield return new WaitForSecondsRealtime(1f);
                var visual = car.transform.Find("Vehicle visual");
                int renderers = visual ? visual.GetComponentsInChildren<Renderer>().Length : 0;
                Check($"{p.Name}: loads (swapped in place, model built)", cfg.profileId == p.Id && visual && renderers > 0, $"{renderers} renderers, body {car.GetComponent<BoxCollider>().size.ToString("0.00")}, mass {car.Body.mass:0} kg, top speed {car.topSpeed:0} m/s");
                bool camOk = false; var cam = FindAnyObjectByType<ChaseCamera>(); if (cam) camOk = cam.offset == p.Camera;
                Check($"{p.Name}: chase camera uses Racer's offset for it", camOk, cam ? cam.offset.ToString("0.0") : "no camera");
                if (Kinds.Contains(p.Id) && fountain)
                {
                    respawn.PlaceAt(pond, pondYaw);
                    yield return new WaitForSecondsRealtime(2f);
                    var ft = new List<float>(); float t0 = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - t0 < 8f) { yield return null; ft.Add(Time.unscaledDeltaTime); }
                    report.AppendLine("  " + Stats("parked beside the pond", ft));
                    yield return Shot($"r13_{p.Id}_pond");
                    respawn.PlaceAt(bottom + Vector3.up * 0.3f, Mathf.Atan2(f0.x, f0.z) * Mathf.Rad2Deg);
                    yield return new WaitForFixedUpdate();
                }
                // the climb, full throttle from 8 m/s
                car.enabled = false;
                car.Body.linearVelocity = f0 * 8f;
                float time = 0f, minSpeed = float.MaxValue, maxSound = 0f; int soundFrames = 0, frames = 0; bool reached = false; float stuck = 0f; string end = "";
                float prevSide = Vector3.Dot(car.transform.position - top, f1);
                var audio = car.GetComponents<AudioSource>();
                while (time < 40f)
                {
                    yield return new WaitForFixedUpdate();
                    float dt = Time.fixedDeltaTime; time += dt;
                    var pos = car.transform.position;
                    car.Simulate(1f, 0f, Steer(climb, pos), dt);
                    float sp = car.Body.linearVelocity.magnitude;
                    if (time > 1f) minSpeed = Mathf.Min(minSpeed, sp);
                    frames++;
                    float loud = audio.Where(a => a && a.isPlaying).Select(a => a.volume).DefaultIfEmpty(0f).Max();
                    maxSound = Mathf.Max(maxSound, loud); if (loud > 0.02f) soundFrames++;
                    stuck = sp < 1f ? stuck + dt : 0f;
                    if (stuck > 3f) { end = $"stopped at ({pos.x:0},{pos.z:0})"; break; }
                    if (Vector3.Angle(car.transform.up, Vector3.up) > 75f) { end = $"rolled at ({pos.x:0},{pos.z:0})"; break; }
                    float side = Vector3.Dot(pos - top, f1);
                    if (prevSide < 0f && side >= 0f && Flat(pos - top).magnitude < 15f) { reached = true; break; }
                    prevSide = side;
                    if (Mathf.Abs(time - 4f) < dt * 0.5f) yield return Shot($"r13_{p.Id}_driving");
                }
                car.enabled = true;
                Check($"{p.Name}: drives up the steep part of the lane", reached, reached ? $"{time:0.00} s, slowest {minSpeed:0.0} m/s, top {car.Body.linearVelocity.magnitude:0.0} m/s at the top" : end.Length > 0 ? end : "did not reach the top in 40 s");
                Check($"{p.Name}: sounds while driving (VehicleAudio)", soundFrames > frames / 2, $"{audio.Count(a => a)} voices, loudest {maxSound:0.000}, audible in {soundFrames} of {frames} steps");
                report.AppendLine();
            }
            sw.Select("original");
            report.AppendLine(fails == 0 ? $"ALL {checks} CHECKS PASSED" : $"{fails} OF {checks} CHECKS FAILED");
            File.WriteAllText(Path.Combine(outputDir, "round13_check.txt"), report.ToString());
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        float Steer(List<Vector3> line, Vector3 pos)
        {
            float v = Mathf.Max(car.ForwardSpeed, 1f);
            int near = 0; float bd = float.MaxValue;
            for (int i = 0; i < line.Count; i++) { float d = Flat(line[i] - pos).sqrMagnitude; if (d < bd) { bd = d; near = i; } }
            float look = 6f + 0.35f * v; int k = near; float acc = 0f;
            while (k < line.Count - 1 && acc < look) { acc += Flat(line[k + 1] - line[k]).magnitude; k++; }
            float ang = Vector3.SignedAngle(Flat(car.transform.forward), Flat(line[k] - pos), Vector3.up);
            return Mathf.Clamp(ang / 20f, -1f, 1f);
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            try
            {
                var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
                Destroy(tex);
            }
            catch (Exception e) { report.AppendLine("screenshot failed: " + e.Message); }
        }

        static string Stats(string label, List<float> ft)
        {
            if (ft.Count < 2) return label + ": no frames";
            var a = ft.ToArray(); Array.Sort(a);
            float total = a.Sum(), med = a[a.Length / 2], p99 = a[Mathf.Min(a.Length - 1, Mathf.FloorToInt(a.Length * 0.99f))];
            return $"{label}: {a.Length / total:0} fps average; median {1f / med:0} fps; 99th percentile {p99 * 1000f:0.0} ms ({1f / p99:0} fps)";
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
