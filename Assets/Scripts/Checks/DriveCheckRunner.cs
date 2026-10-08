using System.Collections.Generic;
using System.IO;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Development check only (added to the scene by Editor/DriveCheck.cs, never saved into it). Drives the player car
    // along a waypoint path through the real ArcadeVehicle.Simulate step and logs what happens, so "the car drives on
    // the generated map" can be checked without a person at the keyboard.
    public sealed class DriveCheckRunner : MonoBehaviour
    {
        public List<Vector3> path = new List<Vector3>();
        public float duration = 150f, targetSpeed = 13f;
        public string outputDir;

        ArcadeVehicle car;
        Camera cam;
        int next;
        float t, nextLog, nextShot, maxTilt, minGap = float.MaxValue, maxSpeed, distance, airTime;
        Vector3 last;
        int shots;
        readonly StringBuilder log = new StringBuilder();

        void Start()
        {
            car = FindAnyObjectByType<ArcadeVehicle>();
            car.enabled = false; // stop the input-driven step; this runner calls Simulate itself
            cam = Camera.main;
            last = car.transform.position;
            log.AppendLine("t(s)\tx\tz\tspeed(m/s)\tgrounded\ttilt(deg)\tgap(m)\twaypoint");
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            t += dt;
            var pos = car.transform.position;
            while (next < path.Count - 1 && Flat(path[next] - pos).magnitude < 9f) next++;
            Vector3 aim = path[Mathf.Min(next, path.Count - 1)];
            Vector3 to = Flat(aim - pos);
            float angle = Vector3.SignedAngle(Flat(car.transform.forward), to, Vector3.up);
            float steer = Mathf.Clamp(angle / 25f, -1f, 1f);
            float speed = car.ForwardSpeed;
            float want = Mathf.Abs(angle) > 35f ? targetSpeed * 0.5f : targetSpeed;
            float throttle = speed < want ? 1f : 0f, brake = speed > want + 3f ? 0.6f : 0f;
            car.Simulate(throttle, brake, steer, dt);

            distance += Flat(pos - last).magnitude;
            last = pos;
            float tilt = Vector3.Angle(car.transform.up, Vector3.up);
            if (t > 2f) maxTilt = Mathf.Max(maxTilt, tilt);
            maxSpeed = Mathf.Max(maxSpeed, speed);
            if (car.GroundedWheels < 2 && t > 2f) airTime += dt;
            float gap = pos.y - GroundQuery.Height(pos.x, pos.z);
            if (t > 2f) minGap = Mathf.Min(minGap, gap);

            if (t >= nextLog)
            {
                nextLog += 1f;
                log.AppendLine($"{t:F0}\t{pos.x:F1}\t{pos.z:F1}\t{speed:F1}\t{car.GroundedWheels}\t{tilt:F1}\t{gap:F2}\t{next}/{path.Count}");
            }
            if (t >= nextShot && cam != null && shots < 4)
            {
                nextShot += duration / 4f;
                Shoot($"drive_{shots++}.png", cam);
            }
            if (t >= duration || next >= path.Count - 1)
            {
                string summary = $"time {t:F1} s, distance {distance:F0} m, reached waypoint {next}/{path.Count}, " +
                                 $"max speed {maxSpeed:F1} m/s, max tilt {maxTilt:F1} deg, time with <2 wheels grounded {airTime:F2} s, " +
                                 $"min body height above terrain {minGap:F2} m";
                log.AppendLine(summary);
                File.WriteAllText(Path.Combine(outputDir, "drive_log.tsv"), log.ToString());
                enabled = false;
                Debug.Log("[DriveCheck] " + summary);
                Overview();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.Exit(0);
#endif
            }
        }

        // Top-down views of the whole map and of the marked house, for checking layout and the marker.
        void Overview()
        {
            var go = new GameObject("OverviewCamera");
            var c = go.AddComponent<Camera>();
            c.farClipPlane = 5000f;
            var ext = GroundQuery.Extent();
            go.transform.SetPositionAndRotation(new Vector3(ext.center.x, 1500f, ext.center.y), Quaternion.Euler(90f, 0f, 0f));
            c.orthographic = true;
            c.orthographicSize = Mathf.Max(ext.height / 2f, ext.width / 2f / (16f / 9f));
            Shoot("overview_map.png", c, 2560, 1440);
            var beacon = GameObject.Find("OwnerHouseBeacon");
            if (beacon)
            {
                c.orthographic = false;
                c.fieldOfView = 50f;
                var b = beacon.transform.position;
                go.transform.position = new Vector3(b.x - 60f, b.y + 10f, b.z - 90f);
                go.transform.LookAt(new Vector3(b.x, b.y - 30f, b.z));
                Shoot("overview_owner_house.png", c);
            }
        }

        public void Shoot(string file, Camera c, int w = 1280, int h = 720)
        {
            var rt = new RenderTexture(w, h, 24);
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            c.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(outputDir, file), tex.EncodeToPNG());
            Destroy(tex);
            rt.Release();
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
