using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Round 83: a planning-only dry run (Editor batch; nothing is built or saved, so it is not a Build Map):
//   Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod ChastainRush.MapBuilder.DryFromCommandLine
//             -dryLines <a.json>[;<b.json>...] -dryOut <report.txt> [-dryProfile <name part>]
// The build's planning runs in memory on the saved scene (as a quick build's does); then each line of the files (r82_lines.py's format: name, line,
// cap) is ridden both ways on the planned surface (the ground, or a deck where the line is on one: the surface nearest where the car is heading), a
// point mass at the line's speed cap: time in the air, the hardest landing, a step up, a deck's slab within a car's height over the surface.
// A forecast for choosing what to fix before a full Build Map, never proof: the exe check is.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [System.Serializable] class DryLine { public string name; public float[] line; public float cap; }
        [System.Serializable] class DryFile { public DryLine[] lines; }

        public static void DryFromCommandLine()
        {
            try { Dry(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Dry()
        {
            string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < a.Length; i++) if (a[i] == n) return a[i + 1]; return null; }
            var lines = new List<DryLine>();
            foreach (var f in (Arg("-dryLines") ?? "").Split(';').Where(s => s.Length > 0)) lines.AddRange(JsonUtility.FromJson<DryFile>(File.ReadAllText(f)).lines);
            string outPath = Arg("-dryOut"), prof = Arg("-dryProfile");
            BuildClock.Start("Dry run");
            // -dryWatch "x,z;x,z": the ground at those points after each pass of the planning (which pass moved it)
            var watch = (Arg("-dryWatch") ?? "").Split(';').Where(s => s.Contains(',')).Select(s => { var v = s.Split(',').Select(t => float.Parse(t, System.Globalization.CultureInfo.InvariantCulture)).ToArray(); return new Vector2(v[0], v[1]); }).ToList();
            var watchLog = new List<string>(); var last = new float[watch.Count]; for (int i = 0; i < last.Length; i++) last[i] = float.NaN;
            BuildClock.OnLap = lap =>
            {
                if (Terrain0 == null || watch.Count == 0) return;
                var moved = new List<string>();
                for (int i = 0; i < watch.Count; i++) { float h = Terrain0.Height(watch[i].x, watch[i].y); if (float.IsNaN(last[i]) || Mathf.Abs(h - last[i]) > .02f) moved.Add($"({watch[i].x:0.0}, {watch[i].y:0.0}) {last[i]:0.00} -> {h:0.00}"); last[i] = h; }
                if (moved.Count > 0) watchLog.Add($"after {lap}: " + string.Join("; ", moved));
            };
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var c = new Ctx();
            try { Plan(c, true); } finally { FlushAssets(); BuildClock.OnLap = null; }
            if (watch.Count > 0) File.WriteAllLines(Path.ChangeExtension(Arg("-dryOut"), ".watch.txt"), watchLog);
            BuildClock.Lap("the planning");
            var map = c.map; var terrain = c.terrain;
            var idx = new CellIndex(16f); var segs = new List<(int b, int i)>();
            for (int bi = 0; bi < map.bridges.Length; bi++)
            {
                var b = map.bridges[bi]; if (b.deck_y == null) continue; var q = ToPoints(b.points);
                for (int i = 0; i + 1 < q.Count && i + 1 < b.deck_y.Length; i++) { segs.Add((bi, i)); idx.Add(q[i], q[i + 1], b.width * .5f + .5f); }
            }
            var near = new List<int>();
            // every deck top over a point (its top, its slab's bottom)
            List<(float top, float bottom)> Decks(Vector2 p)
            {
                var o = new List<(float, float)>();
                foreach (int k in idx.Near(p, 0f, near))
                {
                    var (bi, i) = segs[k]; var b = map.bridges[bi];
                    var a = new Vector2(b.points[2 * i], b.points[2 * i + 1]); var e = new Vector2(b.points[2 * i + 2], b.points[2 * i + 3]); var ab = e - a;
                    float t = Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f); if (t < -.001f || t > 1.001f) continue; t = Mathf.Clamp01(t);
                    if ((a + ab * t - p).magnitude > b.width * .5f) continue;
                    float top = Mathf.Lerp(b.deck_y[i], b.deck_y[i + 1], t) + R82Lift(b.@class); o.Add((top, top - R82Lift(b.@class) - R82Slab));
                }
                return o;
            }
            var rep = new List<string> { $"Round 83 dry run {System.DateTime.Now:yyyy-MM-dd HH:mm}: the planning in memory (no build), each line ridden both ways at its cap by a point mass. " +
                                         "Faults: in the air over 0.3 s, a landing over 6 m/s, a step up over 0.25 m, a slab under 1.8 m over the surface." };
            var tsv = new StringBuilder("line\tdir\ts\tx\tz\tsurface\tcar\tground\n");
            // every line's surfaces every 0.5 m (the ground; each deck's top / slab bottom), for Tools/race/dry_ride.py
            var cs = new StringBuilder("line\tk\tx\tz\tground\tdecks\n");
            for (int li = 0; li < lines.Count; li++)
            {
                var q = new List<Vector2>(); for (int i = 0; i + 1 < lines[li].line.Length; i += 2) q.Add(new Vector2(lines[li].line[i], lines[li].line[i + 1]));
                var P = Resample(q, .5f);
                for (int k = 0; k < P.Count; k++) cs.AppendLine($"{li}\t{k}\t{P[k].x:0.00}\t{P[k].y:0.00}\t{terrain.Height(P[k].x, P[k].y):0.000}\t{string.Join("|", Decks(P[k]).Select(d => $"{d.top:0.000}/{d.bottom:0.000}"))}");
            }
            File.WriteAllText(Path.ChangeExtension(outPath, ".surf.tsv"), cs.ToString());
            int bad = 0;
            foreach (var L in lines)
            {
                var q = new List<Vector2>(); for (int i = 0; i + 1 < L.line.Length; i += 2) q.Add(new Vector2(L.line[i], L.line[i + 1]));
                var P = Resample(q, .5f); if (P.Count < 4) continue;
                float v = L.cap > 1f ? L.cap : 33f;
                for (int dir = 0; dir < 2; dir++)
                {
                    var R = dir == 0 ? P : Enumerable.Reverse(P).ToList(); int n = R.Count;
                    var S = new float[n]; var G = new float[n]; var slabs = new List<string>();
                    float prev = float.NaN, slope = 0f;
                    for (int k = 0; k < n; k++)
                    {
                        var p = R[k]; float g = terrain.Height(p.x, p.y); G[k] = g;
                        var ds = Decks(p); var cand = new List<float> { g }; cand.AddRange(ds.Select(d => d.top));
                        float pick;
                        if (float.IsNaN(prev)) pick = cand.Max();   // a line starting on a deck starts on it
                        else { float pred = prev + slope * .5f; pick = cand.OrderBy(y => Mathf.Abs(y - pred)).First(); }
                        if (!float.IsNaN(prev)) slope = Mathf.Lerp(slope, (pick - prev) / .5f, .5f);
                        S[k] = pick; prev = pick;
                        foreach (var d in ds) if (d.bottom > pick + .05f && d.bottom < pick + 1.8f) { slabs.Add($"({p.x:0}, {p.y:0}) {d.bottom - pick:0.0} m"); break; }
                    }
                    // the ride: a point mass at v, kept on the surface unless the surface falls away faster than gravity
                    float dt = .5f / v, gr = 9.81f, y = S[0], vy = 0f, air = 0f, maxAir = 0f, hard = 0f; bool up = false; Vector2 airAt = R[0], hardAt = R[0];
                    var steps = new List<string>(); var airs = new List<string>();
                    float[] carY = new float[n]; carY[0] = y;
                    for (int k = 0; k + 1 < n; k++)
                    {
                        float yb = y + vy * dt - .5f * gr * dt * dt, vb = vy - gr * dt, sNext = S[k + 1], sv = (S[k + 1] - S[k]) / dt;
                        if (!up)
                        {
                            if (S[k + 1] - S[k] > .25f) steps.Add($"({R[k].x:0}, {R[k].y:0}) {S[k + 1] - S[k]:0.00} m");
                            if (yb > sNext + .02f) { up = true; air = dt; y = yb; vy = vb; }
                            else { y = sNext; vy = sv; }
                        }
                        else
                        {
                            if (yb <= sNext) { float imp = Mathf.Max(0f, sv - vb); if (imp > hard) { hard = imp; hardAt = R[k + 1]; } if (air > .3f) airs.Add($"{air:0.00} s to ({R[k + 1].x:0}, {R[k + 1].y:0})"); if (air > maxAir) { maxAir = air; airAt = R[k + 1]; } up = false; y = sNext; vy = sv; }
                            else { y = yb; vy = vb; air += dt; }
                        }
                        carY[k + 1] = y;
                    }
                    bool fault = maxAir > .3f || hard > 6f || steps.Count > 0 || slabs.Count > 0; if (fault) bad++;
                    rep.Add($"{(fault ? "LOOK" : "ok  ")} {L.name}{(dir == 1 ? " (the other way)" : "")} at {v:0} m/s: longest in the air {maxAir:0.00} s (to ({airAt.x:0}, {airAt.y:0})), hardest landing {hard:0.0} m/s (at ({hardAt.x:0}, {hardAt.y:0})), " +
                            $"{steps.Count} step(s), {slabs.Count} slab(s) low");
                    foreach (var a in airs.Take(6)) rep.Add("    air " + a);
                    foreach (var s in steps.Take(6)) rep.Add("    step " + s);
                    foreach (var s in slabs.Distinct().Take(6)) rep.Add("    low slab " + s);
                    if (!string.IsNullOrEmpty(prof) && L.name.Contains(prof))
                        for (int k = 0; k < n; k += 2) tsv.AppendLine($"{L.name}\t{dir}\t{k * .5f:0.0}\t{R[k].x:0.0}\t{R[k].y:0.0}\t{S[k]:0.00}\t{carY[k]:0.00}\t{G[k]:0.00}");
                }
            }
            // -dryGrid "name:x0,z0,x1,z1,step;...": the planned ground on a grid (csv: x, z, height), for a shaded picture
            foreach (var g in (Arg("-dryGrid") ?? "").Split(';').Where(s => s.Contains(':')))
            {
                var nm = g.Substring(0, g.IndexOf(':')); var v = g.Substring(g.IndexOf(':') + 1).Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                var sb = new StringBuilder("x,z,y\n");
                for (float z = v[1]; z <= v[3]; z += v[4]) for (float x = v[0]; x <= v[2]; x += v[4]) sb.AppendLine($"{x:0.0},{z:0.0},{terrain.Height(x, z):0.000}");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(outPath), $"grid_{nm}.csv"), sb.ToString());
            }
            rep.Insert(1, $"{bad} of {lines.Count * 2} runs to look at");
            File.WriteAllLines(outPath, rep);
            if (!string.IsNullOrEmpty(prof)) File.WriteAllText(Path.ChangeExtension(outPath, ".tsv"), tsv.ToString());
            BuildClock.Lap("the rides");
            Debug.Log($"[Dry] {bad} runs to look at; written {outPath}");
        }
    }
}
