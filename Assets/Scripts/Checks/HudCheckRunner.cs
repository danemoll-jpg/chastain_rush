using System.IO;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Development check only (added by Editor/HudCheck.cs, never saved into the scene). Runs in a windowed Editor Play
    // session: drives the car at a test ramp laid across the spawn street so the bump log has something to record, then
    // uses the debug HUD's own capture and save calls, and writes what it found to a report.
    public sealed class HudCheckRunner : MonoBehaviour
    {
        public string outputDir;
        public float rampAhead = 40f;

        ArcadeVehicle car;
        DeveloperHud hud;
        float t, rampZoneEnd;
        int phase;
        float phaseAt;
        readonly StringBuilder log = new StringBuilder();
        Vector3 start;
        Vector3 heading;

        void Start()
        {
            car = FindAnyObjectByType<ArcadeVehicle>();
            hud = DeveloperHud.Instance;
            car.enabled = false;
            start = car.transform.position;
            heading = car.transform.forward;
            // a wedge across the road: rises 0.45 m over 2 m, then drops off, at 'rampAhead' metres
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "TestRamp";
            ramp.transform.position = start + heading * rampAhead + Vector3.down * 0.35f;
            ramp.transform.rotation = Quaternion.LookRotation(heading) * Quaternion.Euler(-12f, 0f, 0f);
            ramp.transform.localScale = new Vector3(6f, 0.5f, 3.2f);
            Physics.SyncTransforms();
            log.AppendLine("HUD present: " + (hud != null) + "; BumpLogger present: " + (FindAnyObjectByType<BumpLogger>() != null) + "; RoadLookup present: " + (RoadLookup.Instance != null));
            if (RoadLookup.Instance != null)
            {
                bool found = RoadLookup.Instance.Nearest(car.transform.position, out string name, out float d, out float half);
                log.AppendLine($"Road under the car at spawn: {found} '{name}' {d:0.0} m from centre, half width {half:0.0}");
            }
        }

        void FixedUpdate()
        {
            t += Time.fixedDeltaTime;
            if (phase == 0)
            {
                // straight at the ramp at about 14 m/s (the road bends little over 60 m)
                float speed = car.ForwardSpeed;
                car.Simulate(speed < 14f ? 1f : 0f, 0f, 0f, Time.fixedDeltaTime);
                if (Vector3.Dot(car.transform.position - start, heading) > rampAhead + 25f || t > 12f)
                {
                    log.AppendLine($"Drove to {Vector3.Dot(car.transform.position - start, heading):0} m after {t:0.0} s; speed {car.ForwardSpeed:0.0} m/s");
                    car.Body.linearVelocity = Vector3.zero;
                    phase = 1; phaseAt = Time.unscaledTime;
                    if (hud != null) hud.CaptureBug();
                }
            }
        }

        void Update()
        {
            if (phase == 1 && hud != null)
            {
                if (hud.CommentOpen)
                {
                    hud.CommentText = "Automated check: comment typed by HudCheckRunner";
                    hud.SaveComment();
                    phase = 2; phaseAt = Time.unscaledTime;
                }
                else if (Time.unscaledTime - phaseAt > 5f) { log.AppendLine("Capture never reached the comment box: " + hud.Status); phase = 2; }
            }
            if (phase == 2 && Time.unscaledTime - phaseAt > 0.5f) Finish();
        }

        void Finish()
        {
            phase = 3;
            var session = DeveloperHud.Session;
            log.AppendLine("Status line: " + (hud != null ? hud.Status : "(no HUD)"));
            if (session != null)
            {
                log.AppendLine("Session folder: " + session.DirectoryPath + "; reports " + session.Count);
                foreach (var f in new[] { "bugs.json", "BUG_REPORT.md", "Screenshots/BUG-001.png" })
                {
                    var p = Path.Combine(session.DirectoryPath, f);
                    log.AppendLine($"  {f}: {(File.Exists(p) ? new FileInfo(p).Length + " bytes" : "MISSING")}");
                }
            }
            else log.AppendLine("No session was created.");
            string bump = Path.GetFullPath(BumpLogger.LogPath);
            log.AppendLine("Bump log " + bump + ":");
            log.AppendLine(File.Exists(bump) ? File.ReadAllText(bump) : "(file not written)");
            Directory.CreateDirectory(outputDir);
            File.WriteAllText(Path.Combine(outputDir, "hud_check.txt"), log.ToString());
            Debug.Log("[HudCheck]\n" + log);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
