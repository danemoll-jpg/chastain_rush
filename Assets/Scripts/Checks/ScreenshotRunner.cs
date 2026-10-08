using System.Collections.Generic;
using System.IO;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Development check only (added by Editor/ScreenshotCheck.cs, never saved into the scene). In Play mode it stands the
    // car at each viewpoint, renders the game camera's view (same FOV and clip planes as the game) to a PNG, then an
    // overhead view of the whole map with the fog off, and quits.
    public sealed class ScreenshotRunner : MonoBehaviour
    {
        [System.Serializable] public sealed class View
        {
            public string file, label;
            public Vector2 at, heading;
            public float deckY = float.NaN; // set for a viewpoint on a bridge deck
        }

        public List<View> views = new List<View>();
        public string outputDir;
        public Vector2 overheadSize;

        int frame;

        void Update()
        {
            frame++;
            if (frame < 10) return; // let the terrain and trees settle
            Shoot();
        }

        void Shoot()
        {
            enabled = false;
            var main = Camera.main;
            var car = FindAnyObjectByType<ArcadeVehicle>();
            car.enabled = false;
            var report = new StringBuilder();
            var go = new GameObject("ShotCamera");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = main.fieldOfView; cam.nearClipPlane = main.nearClipPlane; cam.farClipPlane = main.farClipPlane;
            cam.tag = "Untagged";
            main.enabled = false;
            var trees = FindAnyObjectByType<TreeField>();
            foreach (var v in views)
            {
                float y = float.IsNaN(v.deckY) ? GroundQuery.Height(v.at.x, v.at.y) : v.deckY;
                var pos = new Vector3(v.at.x, y + 0.8f, v.at.y);
                var rot = Quaternion.LookRotation(new Vector3(v.heading.x, 0f, v.heading.y), Vector3.up);
                var body = car.Body;
                body.position = pos; body.rotation = rot;
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                car.transform.SetPositionAndRotation(pos, rot);
                Physics.SyncTransforms();
                go.transform.position = pos + rot * new Vector3(0f, 3.6f, -7.5f);
                go.transform.rotation = Quaternion.LookRotation(pos + Vector3.up * 0.7f + rot * Vector3.forward * 3f - go.transform.position, Vector3.up);
                Capture(cam, v.file);
                report.AppendLine($"{v.file}: {v.label}; trees drawn {trees.LastTreesDrawn}, {trees.LastDrawCalls} tree draw calls, {trees.LastTriangles / 1000} k tree triangles");
            }
            // overhead: orthographic, straight down, the whole map; no fog
            RenderSettings.fog = false;
            cam.orthographic = true;
            cam.farClipPlane = 6000f;
            cam.transform.SetPositionAndRotation(new Vector3(overheadSize.x / 2f, 3000f, overheadSize.y / 2f), Quaternion.Euler(90f, 0f, 0f));
            cam.orthographicSize = Mathf.Max(overheadSize.y / 2f, overheadSize.x / 2f / (16f / 9f)) * 1.02f;
            Capture(cam, "06_overhead.png", 2560, 1440);
            report.AppendLine($"06_overhead.png: whole map, {overheadSize.x:F0} x {overheadSize.y:F0} m, orthographic, fog off; trees drawn {trees.LastTreesDrawn}");
            File.WriteAllText(Path.Combine(outputDir, "screenshots.txt"), report.ToString());
            Debug.Log("[ScreenshotCheck]\n" + report);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }

        void Capture(Camera cam, string file, int w = 1920, int h = 1080)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(outputDir, file), tex.EncodeToPNG());
            Destroy(tex);
            rt.Release();
        }
    }
}
