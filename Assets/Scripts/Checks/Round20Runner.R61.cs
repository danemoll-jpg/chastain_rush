using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 61 (C, after the stage 2 land): -exeRound59 <dir> -r59Only fps61x: what the frame costs at the old neighborhood (Round 59's spot), measured
    // with the candidates switched off one after another (each on top of the ones before): the renderers more than 1 km away, the water areas more
    // than 1 km away, the trees, the terrain tiles more than 1.5 km away, the breakables' triggers more than 1 km away. Check only; quits after.
    public sealed partial class Round20Runner
    {
        // the main thread's, the render thread's and the GPU's time per frame (FrameTimingManager), averaged
        sealed class R61Timing
        {
            readonly FrameTiming[] one = new FrameTiming[1]; double main, render, gpu; int n;
            public void Add() { FrameTimingManager.CaptureFrameTimings(); if (FrameTimingManager.GetLatestTimings(1, one) > 0) { main += one[0].cpuMainThreadFrameTime; render += one[0].cpuRenderThreadFrameTime; gpu += one[0].gpuFrameTime; n++; } }
            public override string ToString() => n == 0 ? "" : $" (main thread {main / n:0.0} ms, render thread {render / n:0.0} ms, GPU {gpu / n:0.0} ms)";
        }

        IEnumerator R61Experiments()
        {
            report.AppendLine(); report.AppendLine($"=== What the frame costs at the old neighborhood, 6 s each, {Screen.width} x {Screen.height}; each step adds to the ones before");
            if (race.State != RaceController.Stage.Free) race.EndRace();
            int vs = QualitySettings.vSyncCount, cap = Application.targetFrameRate; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            var at = R59Spots[0].at; respawn.PlaceAt(RaceRoute.Surface(at.x, at.y) + Vector3.up * 0.4f, R59Spots[0].yaw); race.NotifyTeleported();
            yield return new WaitForSecondsRealtime(4f);
            var here = car.Body.position;
            IEnumerator M(string label)
            {
                var frames = new List<float>(); float t0 = Time.realtimeSinceStartup; var ft = new R61Timing();
                while (Time.realtimeSinceStartup - t0 < 6f) { yield return null; frames.Add(Time.unscaledDeltaTime); ft.Add(); }
                var sorted = frames.OrderBy(x => x).ToList(); float p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99f))];
                report.AppendLine($"INFO  {label}: {frames.Count / frames.Sum():0} fps average, slowest 1 % {1f / p99:0} fps{ft}");
            }
            float Flat(Vector3 p) => new Vector2(p.x - here.x, p.z - here.z).magnitude;
            yield return M("as built");
            var rs = FindObjectsByType<Renderer>(FindObjectsSortMode.None); int nr = 0;
            foreach (var r in rs) if (r.enabled && Flat(r.bounds.center) > 1000f && r.bounds.size.magnitude < 600f) { r.enabled = false; nr++; }
            yield return M($"{nr} of {rs.Length} renderers more than 1 km away off");
            var ws = FindObjectsByType<ShallowWater>(FindObjectsSortMode.None); int nw = 0;
            foreach (var w in ws) if (w.enabled && Flat(w.transform.position) > 1000f && (w.outline == null || w.outline.Length < 3 || Flat(new Vector3(w.outline[0].x, 0f, w.outline[0].y)) > 1000f)) { w.enabled = false; nw++; }
            yield return M($"{nw} of {ws.Length} water areas more than 1 km away off");
            foreach (var t in FindObjectsByType<TreeField>(FindObjectsSortMode.None)) t.enabled = false;
            yield return M("the trees off");
            int nt = 0; foreach (var t in FindObjectsByType<Terrain>(FindObjectsSortMode.None)) { var o = t.transform.position; var s = t.terrainData.size; var c = new Vector3(o.x + s.x / 2, 0, o.z + s.z / 2); if (Flat(c) > 1500f) { t.gameObject.SetActive(false); nt++; } }
            yield return M($"{nt} terrain tiles more than 1.5 km away off");
            var ps = FindObjectsByType<FencePanel>(FindObjectsSortMode.None); int np = 0;
            foreach (var p in ps) if (Flat(p.transform.position) > 1000f) { p.gameObject.SetActive(false); np++; }
            yield return M($"{np} of {ps.Length} breakables' triggers more than 1 km away off");
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = cap;
            report.AppendLine("INFO  (the world is left changed: nothing after this in the run)");
        }
    }
}
