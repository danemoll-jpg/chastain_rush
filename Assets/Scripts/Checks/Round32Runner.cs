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
    // Round 32 check in the exe (B2): ChastainRush.exe -exeRound32 <dir> -traffic Heavy [-r32Minutes 5] -logFile <dir>\Player.log.
    // Muted (CheckRun), its files in <dir>. Heavy traffic round the Canton Road junction of BUG-010 (Dan: four cars in a knot,
    // "Is this a car accident?") for 5 minutes, the player parked beside it: every traffic car within 30 m of the junction, how
    // long it stands still (under 0.5 m/s) at a stretch; pass when none stands there longer than 10 s.
    public sealed class Round32Runner : MonoBehaviour
    {
        public string outputDir;
        static readonly Vector2 Junction = new Vector2(3383.7f, 1649.7f);   // Canton Road and Rockelberry Court (BUG-010 at x 3377.17, z 1656.77)

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            float minutes = 5f; var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-r32Minutes") float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out minutes);
            var report = new StringBuilder($"Round 32 check (B2), {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}, muted.\n");
            yield return new WaitForSecondsRealtime(4f);
            ArcadeVehicle car = null; foreach (var v in FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)) { var vi = v.GetComponent<VehicleInput>(); if (vi && vi.enabled) car = v; }
            if (DeveloperHud.Instance) DeveloperHud.Instance.Visible = false;
            var traffic = Traffic.Instance;
            if (!traffic || !car) { File.WriteAllText(Path.Combine(outputDir, "round32_junction.txt"), report + "FAIL no traffic or no player\n"); Application.Quit(1); yield break; }
            Traffic.ForcedLevel = 3;
            void Park(float x, float z, float yaw)
            {
                float y = 400f; if (Physics.Raycast(new Vector3(x, 900, z), Vector3.down, out var hit, 1200f, ~0, QueryTriggerInteraction.Ignore)) y = hit.point.y;
                var rot = Quaternion.Euler(0, yaw, 0); var pos = new Vector3(x, y + .8f, z);
                car.Body.position = pos; car.Body.rotation = rot; car.transform.SetPositionAndRotation(pos, rot);
                car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero; car.ClearSteering();
            }
            Park(3404f, 1676f, 225f);
            yield return new WaitForSecondsRealtime(20f);   // the cars come in round the player
            var still = new Dictionary<TrafficDriver, float>(); var worst = new Dictionary<TrafficDriver, float>();
            int samples = 0, knots0 = Traffic.KnotsUntied, recycled0 = traffic.Recycled; float maxStill = 0; string maxWho = "";
            var passers = new HashSet<TrafficDriver>();
            for (float t = 0; t < minutes * 60f; t += .25f)
            {
                foreach (var d in traffic.Cars)
                {
                    if (!d || !d.gameObject.activeSelf) { if (d) still[d] = 0; continue; }
                    var p = d.Car.Body.position;
                    if ((new Vector2(p.x, p.z) - Junction).magnitude > 30f) { still[d] = 0; continue; }
                    samples++; passers.Add(d);
                    float s = d.Car.Body.linearVelocity.magnitude < .5f ? (still.TryGetValue(d, out float o) ? o : 0) + .25f : 0;
                    still[d] = s; if (s > maxStill) { maxStill = s; maxWho = $"{d.name} ({d.State}) at x {p.x:0} z {p.z:0}"; }
                }
                if (Mathf.Abs(t - 150f) < .01f) yield return Shot("junction_mid");
                yield return new WaitForSecondsRealtime(.25f);
            }
            report.AppendLine($"Heavy traffic, {minutes:0.#} min, junction at x {Junction.x} z {Junction.y}: {passers.Count} cars came within 30 m ({samples} samples), {traffic.Cars.Count} cars in all");
            report.AppendLine($"longest stand within 30 m of the junction: {maxStill:0.0} s ({maxWho}); knots untied (a car taking its turn): {Traffic.KnotsUntied - knots0}; recycled: {traffic.Recycled - recycled0}");
            bool ok = maxStill <= 10f;
            report.AppendLine(ok ? "PASS no car stood at the junction longer than 10 s" : "FAIL a car stood at the junction longer than 10 s");
            Park(3377.17f, 1656.77f, 157.5f);   // Dan's BUG-010 spot
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("bug010_dan_spot");
            File.WriteAllText(Path.Combine(outputDir, "round32_junction.txt"), report.ToString());
            Debug.Log("[Round32]\n" + report);
            Application.Quit(ok ? 0 : 1);
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
