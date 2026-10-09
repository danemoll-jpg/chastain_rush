using System;
using System.Collections;
using System.IO;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 9: "look at the result where Dan stood", in the built exe. ChastainRush.exe -exeShots <shots.json> <dir> puts the
    // car at each listed place and heading (Dan's debug-report positions, so the chase camera sees what he saw), waits for
    // the camera to settle and writes <dir>/<name>.png. A shot with drive_speed > 0 is a drive instead: the car starts there
    // at that speed, is held straight on for drive_seconds by ArcadeVehicle.Simulate (the step the player's input drives),
    // and the line in shots.txt says how many breakable panels (fences, mailboxes) it broke, the speed before and after, the
    // most it tilted and whether it stopped. Then the exe quits. Added by GameShell only when that argument is given.
    public sealed class ExeShotsRunner : MonoBehaviour
    {
        [Serializable] public class Shot { public string name; public float x, z, yaw, drive_speed, drive_seconds, shot_at = -1f; }
        [Serializable] public class ShotList { public Shot[] shots; }

        public string listPath, outputDir;
        ArcadeVehicle car;
        readonly StringBuilder report = new StringBuilder();

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            var list = JsonUtility.FromJson<ShotList>(File.ReadAllText(listPath));
            report.AppendLine($"Chastain Rush exe shots, {DateTime.Now:yyyy-MM-dd HH:mm:ss}, screen {Screen.width}x{Screen.height}, {(Application.isEditor ? "Editor" : "built exe")}");
            yield return new WaitForSecondsRealtime(4f);
            car = FindAnyObjectByType<ArcadeVehicle>();
            var respawn = car.GetComponent<VehicleRespawn>();
            var hud = DeveloperHud.Instance;
            foreach (var s in list.shots)
            {
                var p = RaceRoute.Ground(s.x, s.z);
                respawn.PlaceAt(p + Vector3.up * 0.3f, s.yaw);
                yield return new WaitForSecondsRealtime(2.5f);
                if (s.drive_speed <= 0f) { yield return Capture(s.name); report.AppendLine($"{s.name}: car at ({s.x:0.0}, {p.y:0.0}, {s.z:0.0}) heading {s.yaw:0}"); continue; }
                int broken0 = FenceChunk.TotalBroken();
                car.enabled = false;
                var fwd = Quaternion.Euler(0f, s.yaw, 0f) * Vector3.forward;
                car.Body.linearVelocity = fwd * s.drive_speed;
                float t = 0f, maxTilt = 0f, minSpeed = s.drive_speed; bool shot = false;
                while (t < s.drive_seconds)
                {
                    yield return new WaitForFixedUpdate();
                    float steer = Mathf.Clamp(Vector3.SignedAngle(Flat(car.transform.forward), fwd, Vector3.up) / 20f, -1f, 1f);
                    car.Simulate(car.ForwardSpeed < s.drive_speed ? 1f : 0f, 0f, steer, Time.fixedDeltaTime);
                    t += Time.fixedDeltaTime;
                    maxTilt = Mathf.Max(maxTilt, Vector3.Angle(car.transform.up, Vector3.up));
                    if (t > 0.3f) minSpeed = Mathf.Min(minSpeed, car.ForwardSpeed);
                    if (!shot && s.shot_at >= 0f && t >= s.shot_at) { shot = true; yield return Capture(s.name); }
                }
                car.enabled = true;
                if (!shot) yield return Capture(s.name);
                report.AppendLine($"{s.name}: drive at {s.drive_speed:0.0} m/s for {s.drive_seconds:0.0} s from ({s.x:0.0}, {s.z:0.0}) heading {s.yaw:0}: broke {FenceChunk.TotalBroken() - broken0} panel(s); " +
                                  $"lowest speed after the start {minSpeed:0.0} m/s ({(1f - minSpeed / s.drive_speed) * 100f:0}% below), most tilt {maxTilt:0} deg, {(minSpeed < 1f ? "STOPPED" : "kept going")}");
            }
            File.WriteAllText(Path.Combine(outputDir, "shots.txt"), report.ToString());
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(0);
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
