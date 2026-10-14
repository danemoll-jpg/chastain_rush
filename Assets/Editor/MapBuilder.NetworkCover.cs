using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 25 (A2, BUG-002 / BUG-003, Dan at the Little Noonday exit: "can we clean up the mouths? Make them look better and also I am
// stuck in this location" and "this green thing is blocking me from reentering here"). X_LNC is an exit on low ground ("an open cut
// all the way, its headwall at b"): past its headwall the network's pipe (B22) went on with less ground over it than the pipe is
// tall, so the bank's grass ran through the pipe (a green slab inside the opening, a slab of the bank over the mouth). Every network
// pipe must lie under the ground. This pass, after the mouths are graded (MapBuilder.Pipes.cs) and before anything is built on the
// ground, raises the ground (never lowers it) wherever a built pipe's crown has less than 0.6 m over it: up to 0.6 m over the crown
// along the pipe (from the headwall's top where the pipe leaves an exit's headwall), falling at 1 in 3 to the natural ground either
// side. Not under a glass stretch (its pond), a manhole's riser (its street) or in water; never on a road, drive, sidewalk or the creek
// trail, or within 2 m of a building (MapBuilder.Pipes.cs NearBuilding); never in a mouth's cut or channel. Each raised point goes to
// Assets/Generated/regraded_mouths.txt (the building sweep: Tools/mapgen/network_sweep.py --regraded).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static void CoverNetworkTubes(TerrainGrid terrain, Network n, Pipes pipes)
        {
            if (n == null) return;
            const float Over = 0.6f, Side = 1f / 3f;
            int runs = 0, raised = 0, kept = 0; float most = 0f; var where = new List<string>();
            foreach (var t in n.F.tubes)
            {
                if (!n.Built(t)) continue;
                var g = n.Geo[t.id]; int N = g.P.Length; float L = g.S[N - 1];
                bool fromExit = t.a != null && t.a.kind == "x";
                bool Skip(int i)
                {
                    float s = g.S[i];
                    if (t.glass != null && t.glass.Any(gl => s > gl.s0 - 6f && s < gl.s1 + 6f)) return true;
                    if (g.RiserFrom >= 0 && i >= g.RiserFrom - 6) return true;
                    if (t.b != null && t.b.kind == "x" && L - s < 1f) return true;
                    return false;
                }
                float Need(int i)
                {
                    float crown = g.Axis(i).y + g.R + PipeWall;
                    return fromExit ? crown + 0.12f + Mathf.Min(Over - 0.12f, g.S[i] * 0.5f) : crown + Over;
                }
                var needy = new bool[N];
                for (int i = 0; i < N; i++)
                {
                    if (Skip(i)) continue;
                    float need = Need(i), lo = float.MaxValue; var sd = g.Side(i);
                    foreach (float lat in new[] { -(g.R + PipeWall), 0f, g.R + PipeWall }) { var q = g.P[i] + sd * lat; lo = Mathf.Min(lo, Ground(terrain, q.x, q.z)); }
                    needy[i] = lo < need - 0.05f;
                }
                // each run of pipe that needs ground (with 3 m either side, so the raise eases in along it)
                for (int i = 0; i < N; i++)
                {
                    if (!needy[i]) continue;
                    int a = i; while (i + 1 < N && (needy[i + 1] || (i + 6 < N && needy.Skip(i + 1).Take(6).Any(x => x)))) i++;
                    int b = i; runs++;
                    var idx = Enumerable.Range(a, b - a + 1).Where(k => !Skip(k)).ToList(); if (idx.Count == 0) continue;
                    var pts = idx.Select(k => new Vector2(g.P[k].x, g.P[k].z)).ToList(); var needs = idx.Select(Need).ToList();
                    float reach = g.R + 1f + (needs.Max() - pts.Min(q => Ground(terrain, q.x, q.y)) + 1f) / Side;
                    float before = needs.Zip(pts, (nd, q) => nd - Ground(terrain, q.x, q.y)).Max();
                    EditTerrain(terrain, BoundsOf(pts, reach), (x, z, y) =>
                    {
                        var q = new Vector2(x, z); float best = float.MaxValue; int bk = 0;
                        for (int k = 0; k < pts.Count; k++) { float d = (pts[k] - q).sqrMagnitude; if (d < best) { best = d; bk = k; } }
                        float dl = Mathf.Sqrt(best), target = needs[bk] - Side * Mathf.Max(0f, dl - (g.R + 1f));
                        if (target <= y + 0.01f) return y;
                        if (Beds != null && Beds.Wet(x, z)) { kept++; return y; }
                        if (pipes != null && pipes.Plans.Any(p => p.InCut(q, 0.5f, out _, out _) || p.InChannel(q))) return y;
                        if (DriveBands.At(q, 0.3f) != null) { kept++; return y; }
                        float w = NearBuilding(q); if (w < 1f) { if (w <= 0f) { kept++; return y; } target = Mathf.Lerp(y, target, w); }
                        raised++; most = Mathf.Max(most, target - y); if (target - y > 0.15f) Regraded.Add($"{x:0.00} {z:0.00} {target - y:0.00} {t.id}");
                        return target;
                    });
                    where.Add($"{t.id} {g.S[a]:0}-{g.S[b]:0} m ({before:0.00} m short)");
                }
            }
            var path = System.IO.Path.Combine(ProjectRoot, GeneratedDir, "regraded_mouths.txt");
            System.IO.File.WriteAllLines(path, Regraded);
            Debug.Log($"[Round25] network pipes under the ground: {runs} stretches had less than {Over} m over the crown, the ground raised over them ({raised} samples, up to {most:0.00} m; {kept} in water, on a road or by a building left as they were): {string.Join("; ", where)}");
        }
    }
}
