using System.Collections.Generic;
using System.IO;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Development check only (added by Editor/PerfCheck.cs, never saved into the scene). At each location it puts the car
    // on the road, drives it along the street for a fixed time through the real ArcadeVehicle.Simulate step (so the chase
    // camera and terrain LOD behave as in play) and records the frame times of the rendered game.
    public sealed class PerfCheckRunner : MonoBehaviour
    {
        [System.Serializable] public sealed class Location
        {
            public string label;
            public Vector3 position;
            public float yaw;
            public List<Vector3> path = new List<Vector3>();
            public string counts;
        }

        public List<Location> locations = new List<Location>();
        public string outputDir;
        public float warmup = 3f, measure = 12f, speed = 11f;

        ArcadeVehicle car;
        Camera cam;
        RenderTexture rt;
        Texture2D probe;
        int index = -1, next;
        float phaseStart;
        bool measuring;
        readonly List<float> frames = new List<float>();
        double sumBatches, sumPasses, sumTris;
        int statSamples;
        readonly StringBuilder report = new StringBuilder();

        void Start()
        {
            car = FindAnyObjectByType<ArcadeVehicle>();
            car.enabled = false;
            // Batch mode never draws the game view, so render the main camera ourselves each frame into a 1080p target and
            // read one pixel back to wait for the GPU. Frame time then includes real rendering, shadows and terrain LOD.
            cam = Camera.main;
            rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;
            probe = new Texture2D(1, 1);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            report.AppendLine($"GPU {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsMemorySize} MB; CPU {SystemInfo.processorType} x{SystemInfo.processorCount}; " +
                              $"screen {Screen.width}x{Screen.height}; renderers in scene {FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length}; " +
                              $"terrain tiles {Terrain.activeTerrains.Length}");
            NextLocation();
        }

        void NextLocation()
        {
            index++;
            if (index >= locations.Count) { Finish(); return; }
            var loc = locations[index];
            var body = car.Body;
            body.position = loc.position;
            body.rotation = Quaternion.Euler(0f, loc.yaw, 0f);
            car.transform.SetPositionAndRotation(body.position, body.rotation);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
            next = 0;
            frames.Clear();
            sumBatches = sumPasses = sumTris = 0;
            statSamples = 0;
            measuring = false;
            phaseStart = Time.realtimeSinceStartup;
        }

        void FixedUpdate()
        {
            if (index < 0 || index >= locations.Count) return;
            var loc = locations[index];
            if (loc.path.Count == 0) return;
            var pos = car.transform.position;
            while (next < loc.path.Count - 1 && Flat(loc.path[next] - pos).magnitude < 9f) next++;
            Vector3 to = Flat(loc.path[Mathf.Min(next, loc.path.Count - 1)] - pos);
            float angle = Vector3.SignedAngle(Flat(car.transform.forward), to, Vector3.up);
            float steer = Mathf.Clamp(angle / 25f, -1f, 1f);
            float want = Mathf.Abs(angle) > 35f ? speed * 0.5f : speed;
            car.Simulate(car.ForwardSpeed < want ? 1f : 0f, car.ForwardSpeed > want + 3f ? 0.6f : 0f, steer, Time.fixedDeltaTime);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            cam.Render();
            RenderTexture.active = rt;
            probe.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
            RenderTexture.active = null;
        }

        void Update()
        {
            if (index < 0 || index >= locations.Count) return;
            float t = Time.realtimeSinceStartup - phaseStart;
            if (!measuring)
            {
                if (t >= warmup) { measuring = true; phaseStart = Time.realtimeSinceStartup; }
                return;
            }
            frames.Add(Time.unscaledDeltaTime);
#if UNITY_EDITOR
            sumBatches += UnityEditor.UnityStats.drawCalls;
            sumPasses += UnityEditor.UnityStats.setPassCalls;
            sumTris += UnityEditor.UnityStats.triangles;
            statSamples++;
#endif
            if (t >= measure) { Summarise(locations[index]); NextLocation(); }
        }

        void Summarise(Location loc)
        {
            var sorted = new List<float>(frames);
            sorted.Sort();
            float total = 0f;
            foreach (var f in frames) total += f;
            float avg = frames.Count / Mathf.Max(total, 1e-6f);
            float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
            float median = sorted[sorted.Count / 2];
            string stats = statSamples > 0
                ? $"; per frame: {sumBatches / statSamples:F0} draw calls, {sumPasses / statSamples:F0} SetPass calls, {sumTris / statSamples / 1e6:F2} M triangles"
                : "";
            report.AppendLine($"{loc.label}: {frames.Count} frames in {total:F1} s = {avg:F0} fps average; median frame {median * 1000f:F1} ms; " +
                              $"99th percentile {p99 * 1000f:F1} ms ({1f / p99:F0} fps); worst {sorted[sorted.Count - 1] * 1000f:F1} ms{stats}; {loc.counts}");
        }

        void Finish()
        {
            File.WriteAllText(Path.Combine(outputDir, "perf_report.txt"), report.ToString());
            Debug.Log("[PerfCheck]\n" + report);
            enabled = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
