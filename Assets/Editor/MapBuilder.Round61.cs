using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 61 (Part D, Dan: BUG-001 x 1359.21 z -716.10 Chastain Meadows Parkway "fix this bump", BUG-002 x 649.95 z -1663.21 and BUG-003 x 583.95
// z -1673.97 Barrett Parkway "smooth out riht here" / "also right here"; "the generated roads in the new land likely have more of these: also run
// Round 54's lap-levelling idea along stage 1 and 2's main roads"): in the planning, right after the terrain is made (so everything graded and
// built later, the mall's lots, the bridge ends, the houses, lies on it as before), each of the main roads in the map extension (map.json's
// roads from extension.first.roads on: the core's are never touched) is levelled across its width to its own centerline profile, blurred along
// the road (a Gaussian of Sigma m: steady grades stay, humps and steps shorter than about 15 m go), easing back to the ground over Blend m beside
// it. The pieces of one road are joined end to end into chains first. Never within 15 m of a bridge's end (fading in over the next 25 m: the
// decks are fixed and SmoothBridgeEnds meets them), never in the core or within 5 m of it (fading in over 30 m). Before / after: the sharpest
// step (second difference over 1 m) along each road, in the build log.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly string[] R61Mains = { "Barrett Parkway", "Chastain Road", "Chastain Meadows Parkway", "Kennesaw Mountain Drive", "Cobb Parkway" };
        const float R61Sigma = 5f, R61Shoulder = 1f, R61Blend = 6f;

        static void R61LevelMains(MapData map, TerrainGrid terrain)
        {
            int first = map.extension != null && map.extension.first != null ? map.extension.first.roads : map.roads.Length;
            var core = new Rect(0f, 0f, map.size_x_m, map.size_z_m);
            bool Main(string n) => !string.IsNullOrEmpty(n) && R61Mains.Any(m => n.StartsWith(m));
            var pick = new List<int>();
            for (int i = first; i < map.roads.Length; i++) { var r = map.roads[i]; if (!r.bridge && !r.tunnel && Main(r.name) && r.points != null && r.points.Length >= 4) pick.Add(i); }
            // the bridges' ends (decks: map.bridges; and road pieces flagged bridge)
            var deckEnds = new List<Vector2>();
            foreach (var b in map.bridges) { var q = ToPoints(b.points); if (q.Count > 1) { deckEnds.Add(q[0]); deckEnds.Add(q[q.Count - 1]); } }
            foreach (var r in map.roads) if (r.bridge) { var q = ToPoints(r.points); if (q.Count > 1) { deckEnds.Add(q[0]); deckEnds.Add(q[q.Count - 1]); } }

            // chains: pieces of the same road joined where exactly two of them meet
            Vector2Int Key(Vector2 p) => new Vector2Int(Mathf.RoundToInt(p.x * 2f), Mathf.RoundToInt(p.y * 2f));
            var ends = new Dictionary<Vector2Int, List<(int piece, bool start)>>();
            foreach (int i in pick)
            {
                var q = ToPoints(map.roads[i].points);
                foreach (var (p, s) in new[] { (q[0], true), (q[q.Count - 1], false) }) { var k = Key(p); if (!ends.TryGetValue(k, out var l)) ends[k] = l = new List<(int, bool)>(); l.Add((i, s)); }
            }
            var used = new HashSet<int>(); var chains = new List<(string name, List<Vector2> pts, float half)>();
            foreach (int i in pick)
            {
                if (used.Contains(i)) continue; used.Add(i);
                var line = ToPoints(map.roads[i].points); string name = map.roads[i].name; float half = map.roads[i].width * .5f;
                for (int dir = 0; dir < 2; dir++)
                {
                    for (int guard = 0; guard < 400; guard++)
                    {
                        var tail = line[line.Count - 1];
                        if (!ends.TryGetValue(Key(tail), out var at) || at.Count != 2) break;
                        int k = at.FindIndex(e => !used.Contains(e.piece)); if (k < 0) break;
                        var o = at[k]; if (map.roads[o.piece].name != name) break;
                        used.Add(o.piece); var q = ToPoints(map.roads[o.piece].points); if (!o.start) q.Reverse();
                        line.AddRange(q.Skip(1)); half = Mathf.Max(half, map.roads[o.piece].width * .5f);
                    }
                    line.Reverse();
                }
                chains.Add((name, line, half));
            }

            var report = new Dictionary<string, (float before, float after, int steps10b, int steps10a, float len)>();
            foreach (var (name, line0, half) in chains)
            {
                var p = Resample(line0, 1f); int n = p.Count; if (n < 20) continue;
                var raw = new float[n]; for (int i = 0; i < n; i++) raw[i] = terrain.Height(p[i].x, p[i].y);
                var w = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float dDeck = float.MaxValue; foreach (var e in deckEnds) dDeck = Mathf.Min(dDeck, (e - p[i]).sqrMagnitude); dDeck = Mathf.Sqrt(dDeck);
                    float dCore = core.Contains(p[i]) ? -1f : Mathf.Max(Mathf.Max(core.xMin - p[i].x, p[i].x - core.xMax), Mathf.Max(core.yMin - p[i].y, p[i].y - core.yMax));
                    w[i] = Mathf.Min(Smooth01((dDeck - 15f) / 25f), Smooth01((dCore - 5f) / 30f));
                }
                if (w.All(v => v <= 0f)) continue;
                float before = SharpestStep(terrain, p, float.MaxValue); int b10 = Steps(terrain, p, .1f);
                LevelAlong(terrain, p, raw, w, half, R61Sigma, R61Shoulder, R61Blend);
                float after = SharpestStep(terrain, p, float.MaxValue); int a10 = Steps(terrain, p, .1f);
                report.TryGetValue(name, out var o0);
                float len = (n - 1);
                report[name] = (Mathf.Max(o0.before, before), Mathf.Max(o0.after, after), o0.steps10b + b10, o0.steps10a + a10, o0.len + len);
            }
            foreach (var kv in report.OrderBy(k => k.Key))
                Debug.Log($"[Round61] D: {kv.Key}: {kv.Value.len:0} m levelled across its width; sharpest 1 m step {kv.Value.before:0.000} -> {kv.Value.after:0.000} m; steps over 0.10 m {kv.Value.steps10b} -> {kv.Value.steps10a}");
            foreach (var (label, at) in new[] { ("BUG-001", new Vector2(1359.21f, -716.10f)), ("BUG-002", new Vector2(649.95f, -1663.21f)), ("BUG-003", new Vector2(583.95f, -1673.97f)) })
            {
                float worst = 0f;
                for (float dx = -25f; dx <= 25f; dx += 1f) for (float dz = -25f; dz <= 25f; dz += 1f)
                {
                    float x = at.x + dx, z = at.y + dz, h = terrain.Height(x, z);
                    worst = Mathf.Max(worst, Mathf.Abs(terrain.Height(x - 1f, z) - 2f * h + terrain.Height(x + 1f, z)), Mathf.Abs(terrain.Height(x, z - 1f) - 2f * h + terrain.Height(x, z + 1f)));
                }
                Debug.Log($"[Round61] D: {label} ({at.x:0.0}, {at.y:0.0}): the sharpest 1 m step within 25 m now {worst:0.000} m");
            }
        }

        // Round 61: the ground across a line's width ('half' + 'shoulder') set to its centerline's profile 'raw' blurred along it (a Gaussian
        // of 'sigma' m; the line resampled at 1 m), weighted by w (0: as it is, 1: the blurred level), eased back to the ground over 'blend'
        static void LevelAlong(TerrainGrid terrain, List<Vector2> p, float[] raw, float[] w, float half, float sigma, float shoulder, float blend, bool[] crestCap = null, float crestK = 0f)
        {
            int n = p.Count;
            int K = Mathf.CeilToInt(sigma * 3f); var target = new float[n];
            for (int i = 0; i < n; i++)
            {
                float sw = 0f, sy = 0f;
                for (int j = Mathf.Max(0, i - K); j <= Mathf.Min(n - 1, i + K); j++) { float k = Mathf.Exp(-0.5f * (j - i) * (j - i) / (sigma * sigma)); sw += k; sy += raw[j] * k; }
                target[i] = raw[i] + w[i] * (sy / sw - raw[i]);
            }
            // Round 62 (C): where asked, no crest sharper than crestK (a second difference per meter squared: 1 / (v^2 / g) keeps wheels down at v), its top lowered
            if (crestCap != null && crestK > 0f)
                for (int it = 0; it < 400; it++)
                {
                    bool moved = false;
                    for (int i = 1; i + 1 < n; i++)
                    {
                        if (!crestCap[i] || w[i] <= 0f) continue;
                        float c = target[i - 1] + target[i + 1] - 2f * target[i];
                        if (c < -crestK - 1e-4f) { target[i] = (target[i - 1] + target[i + 1] + crestK) * .5f; moved = true; }
                    }
                    if (!moved) break;
                }
            const float Cell = 8f; float reach = half + shoulder + blend, inner = half + shoulder;
            var cells = new Dictionary<Vector2Int, List<int>>();
            for (int i = 0; i + 1 < n; i++)
            {
                var lo = Vector2.Min(p[i], p[i + 1]) - Vector2.one * reach; var hi = Vector2.Max(p[i], p[i + 1]) + Vector2.one * reach;
                for (int cx = Mathf.FloorToInt(lo.x / Cell); cx <= Mathf.FloorToInt(hi.x / Cell); cx++)
                    for (int cz = Mathf.FloorToInt(lo.y / Cell); cz <= Mathf.FloorToInt(hi.y / Cell); cz++)
                    { var key = new Vector2Int(cx, cz); if (!cells.TryGetValue(key, out var l)) cells[key] = l = new List<int>(); l.Add(i); }
            }
            var area = Rect.MinMaxRect(p.Min(q => q.x) - reach, p.Min(q => q.y) - reach, p.Max(q => q.x) + reach, p.Max(q => q.y) + reach);
            EditTerrain(terrain, area, (x, z, y) =>
            {
                var q = new Vector2(x, z);
                if (!cells.TryGetValue(new Vector2Int(Mathf.FloorToInt(x / Cell), Mathf.FloorToInt(z / Cell)), out var segs)) return y;
                float best = float.MaxValue, tgt = y, wt = 0f;
                foreach (int i in segs)
                {
                    var ab = p[i + 1] - p[i]; float t = Mathf.Clamp01(Vector2.Dot(q - p[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float dd = (p[i] + ab * t - q).magnitude;
                    if (dd < best) { best = dd; tgt = Mathf.Lerp(target[i], target[i + 1], t); wt = Mathf.Lerp(w[i], w[i + 1], t); }
                }
                if (best >= reach || wt <= 0f) return y;
                // the line's own level carried across its width (its centerline's), then eased back to the ground beside it
                float c = best <= inner ? 1f : Smooth01(1f - (best - inner) / blend);
                return y + c * wt * (tgt - y);
            });
        }

        static int Steps(TerrainGrid terrain, List<Vector2> p, float over)
        {
            int c = 0;
            for (int i = 1; i + 1 < p.Count; i++) if (Mathf.Abs(terrain.Height(p[i - 1].x, p[i - 1].y) - 2f * terrain.Height(p[i].x, p[i].y) + terrain.Height(p[i + 1].x, p[i + 1].y)) > over) c++;
            return c;
        }

        static List<Vector2> Resample(List<Vector2> line, float step)
        {
            var o = new List<Vector2> { line[0] }; float carry = 0f;
            for (int i = 1; i < line.Count; i++)
            {
                var a = line[i - 1]; var b = line[i]; float L = (b - a).magnitude; float s = step - carry;
                while (s <= L) { o.Add(Vector2.Lerp(a, b, s / Mathf.Max(L, 1e-6f))); s += step; }
                carry = L - (s - step);
            }
            if ((o[o.Count - 1] - line[line.Count - 1]).magnitude > step * .3f) o.Add(line[line.Count - 1]);
            return o;
        }
    }
}
