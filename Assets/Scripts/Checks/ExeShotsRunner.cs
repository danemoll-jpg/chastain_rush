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
    // most it tilted, whether it stopped and (Round 17) the longest spell with no wheel on the ground. Then the exe quits. Added by GameShell only when that argument is given.
    public sealed class ExeShotsRunner : MonoBehaviour
    {
        // Round 18: path (x, z, x, z, ...) makes a drive follow those points instead of holding straight on; it ends at the last
        // point (or after drive_seconds) and also reports the farthest the car got from the path and the fewest wheels down
        [Serializable] public class Shot { public string name; public float x, z, yaw, drive_speed, drive_seconds, shot_at = -1f; public float[] path; public float reset_at = -1f, fps_from = -1f; public float[] shot_near, fps_near; }   // fps_near (x, z): the frame rate from when the vehicle first comes within 5 m of it   // Round 23 follow-ups: shot_near (x, z): the shot when the vehicle first comes within 3 m of it (in plan)   // Round 23 follow-ups: reset_at (a reset, R, during the drive), fps_from (the frame rate from then on)
        [Serializable] public class ShotList { public Shot[] shots; }

        public string listPath, outputDir;
        ArcadeVehicle car;
        readonly System.Collections.Generic.List<float> frames = new System.Collections.Generic.List<float>();   // Round 22: frame times during a drive
        bool measuring;
        void Update() { if (measuring) frames.Add(Time.unscaledDeltaTime); }
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
                int broken0 = FenceChunk.TotalBroken(), bursts0 = ManholeLid.Bursts; string resetInfo = ""; bool didReset = false, fpsNearHit = false;
                car.enabled = false;
                var fwd = Quaternion.Euler(0f, s.yaw, 0f) * Vector3.forward;
                car.Body.linearVelocity = fwd * s.drive_speed;
                float t = 0f, maxTilt = 0f, minSpeed = s.drive_speed, air = 0f, maxAir = 0f; bool shot = false;   // Round 17: air, the longest spell with no wheel down
                bool follow = s.path != null && s.path.Length >= 4, reached = false; float maxDev = 0f; int minWheels = 99; Vector2 devAt = default, wheelAt = default;
                frames.Clear(); measuring = true;
                while (t < s.drive_seconds)
                {
                    yield return new WaitForFixedUpdate();
                    if (follow)
                    {
                        var at = new Vector2(car.transform.position.x, car.transform.position.z);
                        float dev = PathDistance(s.path, at, out float along, out float total);
                        if (t > 0.6f) { if (dev > maxDev) { maxDev = dev; devAt = at; } if (car.GroundedWheels < minWheels) { minWheels = car.GroundedWheels; wheelAt = at; } }
                        if (along >= total - 1.5f) { reached = true; break; }
                        var aim = PathPoint(s.path, along + 6f);
                        fwd = new Vector3(aim.x - at.x, 0f, aim.y - at.y).normalized;
                    }
                    float steer = Mathf.Clamp(Vector3.SignedAngle(Flat(car.transform.forward), fwd, Vector3.up) / 20f, -1f, 1f);
                    float want = s.drive_speed;
                    if (follow)   // a driver lifts for a sharp corner: the turn still to make 12 m ahead sets the speed (down to 40%)
                    {
                        var at2 = new Vector2(car.transform.position.x, car.transform.position.z); PathDistance(s.path, at2, out float al, out _);
                        Vector2 d0 = PathPoint(s.path, al + 1f) - PathPoint(s.path, al), d1 = PathPoint(s.path, al + 13f) - PathPoint(s.path, al + 12f);
                        want *= Mathf.Clamp(1f - Vector2.Angle(d0, d1) / 120f, 0.4f, 1f);
                    }
                    car.Simulate(car.ForwardSpeed < want ? 1f : 0f, car.ForwardSpeed > want + 2f ? 1f : 0f, steer, Time.fixedDeltaTime);
                    t += Time.fixedDeltaTime;
                    maxTilt = Mathf.Max(maxTilt, Vector3.Angle(car.transform.up, Vector3.up));
                    air = car.GroundedWheels == 0 ? air + Time.fixedDeltaTime : 0f; if (t > 0.6f) maxAir = Mathf.Max(maxAir, air);   // not the drop from the placing height
                    if (t > 0.3f) minSpeed = Mathf.Min(minSpeed, car.ForwardSpeed);
                    if (!shot && s.shot_at >= 0f && t >= s.shot_at) { shot = true; yield return Capture(s.name); }
                    if (!shot && s.shot_near != null && s.shot_near.Length >= 2 && new Vector2(car.Body.position.x - s.shot_near[0], car.Body.position.z - s.shot_near[1]).magnitude < 3f) { shot = true; yield return Capture(s.name); }
                    if (s.fps_from >= 0f && t < s.fps_from) frames.Clear();
                    if (s.fps_near != null && s.fps_near.Length >= 2 && !fpsNearHit) { if (new Vector2(car.Body.position.x - s.fps_near[0], car.Body.position.z - s.fps_near[1]).magnitude < 5f) fpsNearHit = true; else frames.Clear(); }
                    if (!didReset && s.reset_at >= 0f && t >= s.reset_at)   // Round 23 follow-ups: a reset as R does it
                    {
                        didReset = true; var was = car.Body.position;
                        if (VehicleRespawn.Recover != null && VehicleRespawn.Recover(respawn, out var rp, out var ry)) { respawn.PlaceAt(rp, ry); var dp = DrainPipe.Inside(rp + Vector3.up * 0.3f, out _); resetInfo = $"; reset at {t:0.0} s from ({was.x:0.0}, {was.y:0.0}, {was.z:0.0}) to ({rp.x:0.0}, {rp.y:0.0}, {rp.z:0.0}) heading {ry:0}, {(dp ? "in pipe " + dp.id : "NOT in a pipe")}"; }
                        else resetInfo = $"; reset at {t:0.0} s: no recovery spot found (the last safe spot used)";
                    }
                }
                car.enabled = true;
                measuring = false;
                string fps = "";
                if (frames.Count > 10)
                {
                    float sum = 0f; foreach (var ft in frames) sum += ft;
                    frames.Sort(); float p99 = frames[Mathf.Min(frames.Count - 1, (int)(frames.Count * 0.99f))];
                    fps = $"; frame rate {frames.Count / sum:0} fps average, slowest 1% of frames {1f / p99:0} fps ({Screen.width} x {Screen.height})";
                }
                if (!shot) yield return Capture(s.name);
                report.AppendLine($"{s.name}: drive at {s.drive_speed:0.0} m/s for {s.drive_seconds:0.0} s from ({s.x:0.0}, {s.z:0.0}) heading {s.yaw:0}: broke {FenceChunk.TotalBroken() - broken0} panel(s); " +
                                  $"lowest speed after the start {minSpeed:0.0} m/s ({(1f - minSpeed / s.drive_speed) * 100f:0}% below), most tilt {maxTilt:0} deg, {(minSpeed < 1f ? "STOPPED" : "kept going")}, " +
                                  $"longest with no wheel down {maxAir:0.00} s" +
                                  fps + resetInfo + (ManholeLid.Bursts > bursts0 ? $"; manhole lids burst: {ManholeLid.Bursts - bursts0}" : "") + $"; ended at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}){(DrainPipe.InsideAny(car.Body.position) ? " underground" : " above ground")}" + (follow ? $"; path {(reached ? "followed to its end" : "NOT finished")} in {t:0.0} s, farthest from it {maxDev:0.00} m at ({devAt.x:0.0}, {devAt.y:0.0}), fewest wheels down {minWheels} first at ({wheelAt.x:0.0}, {wheelAt.y:0.0})" : ""));
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

        // distance from p to the polyline, and how far along it the nearest point is
        static float PathDistance(float[] path, Vector2 p, out float along, out float total)
        {
            float best = float.MaxValue, s = 0f; along = 0f;
            for (int i = 2; i + 1 < path.Length; i += 2)
            {
                Vector2 a = new Vector2(path[i - 2], path[i - 1]), b = new Vector2(path[i], path[i + 1]), ab = b - a;
                float tt = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)), d = Vector2.Distance(p, a + ab * tt);
                if (d < best) { best = d; along = s + tt * ab.magnitude; }
                s += ab.magnitude;
            }
            total = s;
            return best;
        }

        static Vector2 PathPoint(float[] path, float along)
        {
            for (int i = 2; i + 1 < path.Length; i += 2)
            {
                Vector2 a = new Vector2(path[i - 2], path[i - 1]), b = new Vector2(path[i], path[i + 1]); float l = Vector2.Distance(a, b);
                if (along <= l || i + 3 >= path.Length) return Vector2.Lerp(a, b, Mathf.Clamp01(along / Mathf.Max(l, 1e-4f)));
                along -= l;
            }
            return new Vector2(path[path.Length - 2], path[path.Length - 1]);
        }
    }
}
