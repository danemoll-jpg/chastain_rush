using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Round 25 (A2, Dan: "can we clean up the mouths?"; the brief: sweep every mouth, no terrain spikes or slabs in or across a pipe
// opening). After every hole is cut, for every mouth (both pipes of a twin, every exit, GP, P1's two ends) and the first 15 m of the
// network's pipe on from each exit: any point of the terrain's surface that is drawn (its cell not holed) and lies inside the
// opening's or the barrel's space (from 0.3 m over the floor to 0.3 m under the crown, within 0.8 of the radius of the middle) is a
// blade or a slab across the way. Written to the build log and Assets/Generated/mouth_sweep.txt, one line per mouth.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static bool Holed(TerrainGrid g, float x, float z)
        {
            var tile = g.At(x, z); var d = tile.terrainData; var pos = tile.transform.position; int hr = d.holesResolution;
            int i = Mathf.Clamp(Mathf.FloorToInt((x - pos.x) / (d.size.x / hr)), 0, hr - 1), j = Mathf.Clamp(Mathf.FloorToInt((z - pos.z) / (d.size.z / hr)), 0, hr - 1);
            return !d.GetHoles(i, j, 1, 1)[0, 0];
        }

        static void MouthSweep(TerrainGrid terrain, Pipes pipes, Network n)
        {
            if (pipes == null) return;
            var lines = new List<string>(); int bad = 0;
            int Count(System.Func<float, (Vector2 c, Vector2 side, float floor, float r)> at, float s0, float s1)
            {
                int hits = 0;
                for (float s = s0; s <= s1; s += 0.25f)
                {
                    var (c, side, floor, r) = at(s);
                    for (float l = -0.8f * r; l <= 0.8f * r + 1e-3f; l += 0.25f)
                    {
                        var q = c + side * l; float y = Ground(terrain, q.x, q.y);
                        float lo = floor + 0.3f, hi = floor + 2f * r - 0.3f;
                        if (y > lo && y < hi && !Holed(terrain, q.x, q.y)) hits++;
                    }
                }
                return hits;
            }
            foreach (var p in pipes.Plans)
            {
                foreach (float bl in p.Lats)
                {
                    string id = p.J.id + (p.J.twin ? (bl < 0f ? " right" : " left") : "");
                    // its mouth at a: the apron up to the headwall and 8 m into the barrel (or to b)
                    int ha = Count(s => (p.P(s, bl), p.Nl, p.FloorAt(Mathf.Clamp(s, 0f, p.L), bl), p.R), Mathf.Max(0f, p.WallFrom), Mathf.Min(p.L, p.S1 + 8f));
                    // its mouth at b (a pipe of pipes.json with two mouths)
                    int hb = !p.J.open_b && p.S2 < p.L ? Count(s => (p.P(s, bl), p.Nl, p.FloorAt(Mathf.Clamp(s, 0f, p.L), bl), p.R), Mathf.Max(0f, p.S2 - 8f), p.L) : 0;
                    lines.Add($"{id}: {(ha + hb == 0 ? "clear" : $"{ha + hb} terrain points inside the opening ({ha} at a, {hb} at b)")}");
                    bad += ha + hb;
                }
            }
            if (n != null)
                foreach (var x in n.F.exits)
                {
                    var t = n.F.tubes.FirstOrDefault(tt => tt.a != null && tt.a.kind == "x" && tt.a.exit == x.id) ?? n.F.tubes.FirstOrDefault(tt => tt.b != null && tt.b.kind == "x" && tt.b.exit == x.id);
                    if (t == null || !n.Built(t)) continue;
                    var g = n.Geo[t.id]; bool fromA = t.a != null && t.a.kind == "x" && t.a.exit == x.id; float L = g.S[g.S.Length - 1];
                    int h = Count(s => { int i = g.At(fromA ? s : L - s); var sd = g.Side(i); return (new Vector2(g.P[i].x, g.P[i].z), new Vector2(sd.x, sd.z), g.P[i].y, g.R); }, 0f, Mathf.Min(15f, L));
                    lines.Add($"{x.id} on into {t.id}: {(h == 0 ? "clear" : $"{h} terrain points inside the pipe")}");
                    bad += h;
                }
            File.WriteAllLines(Path.Combine(ProjectRoot, GeneratedDir, "mouth_sweep.txt"), lines);
            Debug.Log($"[Round25] mouth sweep (terrain inside a pipe's opening or its first meters): {(bad == 0 ? "all clear" : bad + " points")}; " + string.Join("; ", lines));
        }
    }
}
