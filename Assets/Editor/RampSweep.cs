using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 26 (BUG-001, a log ramp's foot was a wall): a sweep of every ramp, kicker and log jump for a lip at the foot or a catch at
    // the sides, on the real colliders of the built scene (no Play mode). Report only.
    //   big jumps   every BigJump (the creek trail's big jumps, N2, N5, RA, RC, RD, RE, J4 / RJ4, R1's, and each reverse take-off off a
    //               back): lines toward the lip from 30 m out, across the width (the middle, half way out, 0.4 m in from each side) and
    //               coming in at 6 and 12 degrees either side, so the side edges are crossed too
    //   shortcuts   every official shortcut's line in Data/shortcuts/chastain_glen_loop.json (#2, #6, R1, R3), on it and 1.5 m either side
    // Every 5 cm a ray straight down; a STEP is a rise of more than 5 cm from one sample to the next (steeper than 45 degrees: no ramp
    // here is steeper than 27), where the higher sample is a ramp's surface ("sc ...") or the lower one was not on the same collider.
    // Batch: Unity.exe -batchmode -nographics -projectPath . -executeMethod ChastainRush.RampSweep.Run -rampOut <file>
    public static class RampSweep
    {
        [Serializable] class ScLine { public string id, name; public float[] path; }
        [Serializable] class ScFile { public ScLine[] shortcuts; }
        const float Dx = 0.05f, StepLimit = 0.05f;

        public static void Run()
        {
            string root = Path.GetDirectoryName(Application.dataPath), outFile = Path.Combine(root, "Assets/Generated/ramp_sweep.txt");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-rampOut") outFile = Path.GetFullPath(args[i + 1]);
            try { Sweep(root, outFile); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        struct Hit { public float y; public string what; public bool ok; }

        static Hit Down(Vector3 p, float top)
        {
            if (!Physics.Raycast(new Vector3(p.x, top, p.z), Vector3.down, out var h, 80f, ~0, QueryTriggerInteraction.Ignore)) return new Hit();
            return new Hit { y = h.point.y, what = h.collider.name, ok = true };
        }

        static float TerrainY(Vector3 p)
        {
            foreach (var h in Physics.RaycastAll(new Vector3(p.x, 1000f, p.z), Vector3.down, 2000f, ~0, QueryTriggerInteraction.Ignore))
                if (h.collider is TerrainCollider) return h.point.y;
            return 0f;
        }

        static void Sweep(string root, string outFile)
        {
            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>()) if (c.attachedRigidbody != null) c.enabled = false;   // the player's vehicle
            Physics.SyncTransforms();
            var sb = new StringBuilder();
            sb.AppendLine($"Ramp sweep (Round 26), {DateTime.Now:yyyy-MM-dd HH:mm}: a STEP is a rise of more than {StepLimit * 100f:0} cm in {Dx * 100f:0} cm onto a ramp or from one collider onto another.");
            int bad = 0, lines = 0;
            // one line, sampled toward its end: the worst rise, where, onto what
            string Line(IList<Vector3> pts, float top, bool rampOnly)
            {
                lines++;
                Hit prev = new Hit(); float worst = 0f; string at = "";
                for (int k = 0; k + 1 < pts.Count; k++)
                {
                    var a = pts[k]; var b = pts[k + 1]; float len = Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z));
                    for (float t = 0f; t < len; t += Dx)
                    {
                        var p = Vector3.Lerp(a, b, t / Mathf.Max(len, 1e-4f));
                        // a fixed height over the jump; along a shortcut, 1.5 m over the last surface (what a vehicle there meets: not a roof
                        // over it on #2's way through the house)
                        var h = Down(p, top > 0f ? top : prev.ok ? prev.y + 1.5f : TerrainY(p) + 1.5f);
                        if (h.ok && prev.ok)
                        {
                            float rise = h.y - prev.y; bool ramp = h.what.StartsWith("sc ");
                            bool counts = rampOnly ? ramp && (rise > StepLimit) : rise > StepLimit && (ramp || h.what != prev.what);
                            if (counts && rise > worst) { worst = rise; at = $"x {p.x:0.00} z {p.z:0.00}: {prev.what} at {prev.y:0.00} onto {h.what} at {h.y:0.00}"; }
                        }
                        prev = h;
                    }
                }
                if (worst > 0f) bad++;
                return worst > 0f ? $"STEP {worst * 100f:0} cm, {at}" : "smooth";
            }
            sb.AppendLine(); sb.AppendLine("== big jumps (toward the lip from 30 m out; o: across, from the middle; a: the angle off the jump's line)");
            foreach (var j in UnityEngine.Object.FindObjectsByType<BigJump>().OrderBy(j => j.id, StringComparer.Ordinal))
            {
                var d = new Vector3(j.dir.x, 0f, j.dir.z).normalized; var nl = Vector3.Cross(Vector3.up, d) * -1f; float hw = j.halfWidth;
                sb.AppendLine($"{j.id} ({j.look}) lip x {j.lip.x:0.0} z {j.lip.z:0.0}, {2f * hw:0.0} m wide");
                foreach (float o in new[] { 0f, -hw * 0.5f, hw * 0.5f, -(hw - 0.4f), hw - 0.4f })
                    foreach (float ang in new[] { 0f, -6f, 6f, -12f, 12f })
                    {
                        if (Mathf.Abs(o) > hw * 0.6f && ang != 0f && Mathf.Sign(ang) != Mathf.Sign(o)) continue;   // at a side, only the angles that cross that side
                        var da = Quaternion.AngleAxis(ang, Vector3.up) * d; var end = j.lip + nl * o - d * 0.3f;
                        string r = Line(new[] { end - da * 30f, end }, j.lip.y + 8f, false);
                        if (r != "smooth" || (o == 0f && ang == 0f)) sb.AppendLine($"  o {o,5:+0.0;-0.0} a {ang,4:+0;-0;0}: {r}");
                    }
            }
            sb.AppendLine(); sb.AppendLine("== shortcuts (along the line, on it and 1.5 m either side; steps onto a ramp's surface only)");
            var file = Path.Combine(root, "Data/shortcuts/chastain_glen_loop.json");
            var sc = File.Exists(file) ? JsonUtility.FromJson<ScFile>(File.ReadAllText(file)) : null;
            foreach (var s in sc?.shortcuts ?? new ScLine[0])
            {
                if (s.path == null || s.path.Length < 4) continue;
                var c = new List<Vector2>(); for (int i = 0; i + 1 < s.path.Length; i += 2) c.Add(new Vector2(s.path[i], s.path[i + 1]));
                sb.AppendLine($"{s.id} {s.name}");
                foreach (float o in new[] { 0f, -1.5f, 1.5f })
                {
                    var pts = new List<Vector3>();
                    for (int i = 0; i < c.Count; i++)
                    {
                        var t = (c[Mathf.Min(i + 1, c.Count - 1)] - c[Mathf.Max(i - 1, 0)]).normalized; var n = new Vector2(-t.y, t.x);
                        var q = c[i] + n * o; pts.Add(new Vector3(q.x, 0f, q.y));
                    }
                    sb.AppendLine($"  o {o,5:+0.0;-0.0}: {Line(pts, 0f, true)}");
                }
            }
            sb.AppendLine(); sb.AppendLine($"{lines} lines, {bad} with a step.");
            Directory.CreateDirectory(Path.GetDirectoryName(outFile));
            File.WriteAllText(outFile, sb.ToString());
            Debug.Log($"[Round26] ramp sweep: {lines} lines, {bad} with a step; {outFile}");
        }
    }
}
