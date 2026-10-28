using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 80 (D1, Round 79's found-not-changed item: "Express Lanes 13's 780 m deck flies 2.06 s and lands hard (10.7 m/s) driven against its direction:
// with E the lanes run both ways, so smooth it both ways (and every Express Lanes deck)"): every Northwest Corridor Express Lanes deck (map.json bridges),
// its OSM pieces that meet end to end joined into one profile, smoothed (Round 77's membrane, the chain's two outer ends held) until no crest is sharper
// than a vertical curve a car at 40 m/s stays down on (radius ExpressCrestR: a second difference of h^2 / R at the deck's spacing h) and no sag sharper
// than twice that; the reach widened until it is. Runs in the planning after RailDecks and before the bridge ends are graded (anything that changes a
// deck's heights must: Round 79). Report Assets/Generated/round80_express_decks.txt.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const string ExpressName = "Northwest Corridor Express Lanes";
        const float ExpressCrestR = 160f;

        // the deck's points with something crossing under them: a road, another road's deck, a rail line (its segment crossing the deck's line), the points
        // within the crossing's half width and 3 m of it along the deck
        static List<int> HeldUnder(MapData map, List<Bridge> own, List<Vector2> pts, float[] cum)
        {
            var held = new HashSet<int>();
            float x0 = pts.Min(q => q.x) - 20f, x1 = pts.Max(q => q.x) + 20f, z0 = pts.Min(q => q.y) - 20f, z1 = pts.Max(q => q.y) + 20f;
            var lines = new List<(float[] p, float half)>();
            if (map.roads != null) foreach (var r in map.roads) if (r.points != null && r.name != ExpressName) lines.Add((r.points, r.width * .5f));
            if (map.bridges != null) foreach (var b in map.bridges) if (b.points != null && !own.Contains(b) && b.name != ExpressName) lines.Add((b.points, b.width * .5f));
            var rp = System.IO.Path.Combine(ProjectRoot, "Data/tracks/railroad.json");
            if (System.IO.File.Exists(rp)) { var rj = JsonUtility.FromJson<RailJ>(System.IO.File.ReadAllText(rp)); if (rj?.lines != null) foreach (var l in rj.lines) if (l.points != null) lines.Add((l.points, 3f)); }
            foreach (var (lp, half) in lines)
                for (int k = 0; k + 3 < lp.Length; k += 2)
                {
                    var a = new Vector2(lp[k], lp[k + 1]); var b = new Vector2(lp[k + 2], lp[k + 3]);
                    if (Mathf.Max(a.x, b.x) < x0 || Mathf.Min(a.x, b.x) > x1 || Mathf.Max(a.y, b.y) < z0 || Mathf.Min(a.y, b.y) > z1) continue;
                    for (int i = 0; i + 1 < pts.Count; i++)
                    {
                        if (!SegCross(a, b, pts[i], pts[i + 1], out float t)) continue;
                        float sAt = Mathf.Lerp(cum[i], cum[i + 1], t), reach = half + 3f;
                        for (int j = 0; j < pts.Count; j++) if (Mathf.Abs(cum[j] - sAt) <= reach) held.Add(j);
                    }
                }
            return held.OrderBy(i => i).ToList();
        }
        static bool SegCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out float tOnCd)
        {
            tOnCd = 0f; var r = b - a; var q = d - c; float den = r.x * q.y - r.y * q.x; if (Mathf.Abs(den) < 1e-6f) return false;
            var ac = c - a; float ta = (ac.x * q.y - ac.y * q.x) / den, tc = (ac.x * r.y - ac.y * r.x) / den;
            tOnCd = tc; return ta >= 0f && ta <= 1f && tc >= 0f && tc <= 1f;
        }

        static void ExpressDecks(MapData map)
        {
            if (map.bridges == null) return;
            var log = new List<string>();
            var pieces = map.bridges.Where(b => b.name == ExpressName && b.points != null && b.deck_y != null && b.deck_y.Length >= 2 && b.points.Length / 2 == b.deck_y.Length).ToList();
            // chains: pieces whose ends meet (within 1.5 m), in order
            var left = new List<Bridge>(pieces); var chains = new List<List<(Bridge b, bool rev)>>();
            Vector2 First(Bridge b, bool rev) { var p = ToPoints(b.points); return rev ? p[p.Count - 1] : p[0]; }
            Vector2 Last(Bridge b, bool rev) { var p = ToPoints(b.points); return rev ? p[0] : p[p.Count - 1]; }
            while (left.Count > 0)
            {
                var chain = new List<(Bridge b, bool rev)> { (left[0], false) }; left.RemoveAt(0);
                for (bool grew = true; grew;)
                {
                    grew = false;
                    for (int i = 0; i < left.Count; i++)
                    {
                        var b = left[i]; var end = Last(chain[chain.Count - 1].b, chain[chain.Count - 1].rev); var start = First(chain[0].b, chain[0].rev);
                        if ((First(b, false) - end).magnitude < 1.5f) { chain.Add((b, false)); }
                        else if ((Last(b, false) - end).magnitude < 1.5f) { chain.Add((b, true)); }
                        else if ((Last(b, false) - start).magnitude < 1.5f) { chain.Insert(0, (b, false)); }
                        else if ((First(b, false) - start).magnitude < 1.5f) { chain.Insert(0, (b, true)); }
                        else continue;
                        left.RemoveAt(i); grew = true; break;
                    }
                }
                chains.Add(chain);
            }
            foreach (var chain in chains)
            {
                // the chain's profile (a shared end point once)
                var pts = new List<Vector2>(); var ys = new List<float>(); var owner = new List<(Bridge b, int i)>();
                foreach (var (b, rev) in chain)
                {
                    var p = ToPoints(b.points); int n = p.Count;
                    for (int k = 0; k < n; k++)
                    {
                        int i = rev ? n - 1 - k : k;
                        if (k == 0 && pts.Count > 0) { owner.Add((b, i)); continue; }   // the joint: the earlier piece's point stands for both
                        pts.Add(p[i]); ys.Add(b.deck_y[i]); owner.Add((b, i));
                    }
                }
                int m = ys.Count; if (m < 3) continue;
                // a chain over the old core's edge is Round 62's (R62SeamOverpasses lays it with its roads as one curve, after this)
                if (pts.Any(q => Mathf.Abs(q.y) < .3f && q.x >= 0f && q.x <= map.size_x_m || Mathf.Abs(q.x) < .3f && q.y >= 0f && q.y <= map.size_z_m)) { log.Add($"{ExpressName} at ({pts[0].x:0}, {pts[0].y:0}): over the old core's edge, Round 62's (left)"); continue; }
                float len = 0f; for (int i = 1; i < m; i++) len += (pts[i] - pts[i - 1]).magnitude;
                float h = len / (m - 1), c = h * h / ExpressCrestR;
                float Crest(float[] a) { float w = 0f; for (int i = 1; i + 1 < a.Length; i++) w = Mathf.Max(w, -(a[i + 1] - 2f * a[i] + a[i - 1])); return w; }
                float Sag(float[] a) { float w = 0f; for (int i = 1; i + 1 < a.Length; i++) w = Mathf.Max(w, a[i + 1] - 2f * a[i] + a[i - 1]); return w; }
                var raw = ys.ToArray(); float c0 = Crest(raw), s0 = Sag(raw);
                string name = $"{ExpressName} ({pts[0].x:0}, {pts[0].y:0} to {pts[m - 1].x:0}, {pts[m - 1].y:0}, {len:0} m, {chain.Count} piece(s))";
                if (c0 <= c && s0 <= 2f * c) { log.Add($"{name}: smooth already (crest {c0:0.000} m, sag {s0:0.000} m a step; limit {c:0.000})"); continue; }
                var wt = new float[m]; for (int i = 0; i < m; i++) wt[i] = i == 0 || i == m - 1 ? 1e6f : 1f;
                float reach = 6f; float[] sm = raw;
                for (int tries = 0; tries < 9; tries++) { sm = R77Smooth1D(raw, wt, (reach / h) * (reach / h)); if (Crest(sm) <= c && Sag(sm) <= 2f * c) break; reach *= 1.6f; }
                // where a road, another deck or the railroad crosses under it the deck is never lowered: each such point holds its height and a cap of
                // the same curve (ExpressCrestR) falls away from it; the deck is the higher of the smoothed profile and the caps (where they meet, a sag)
                var cum = new float[m]; for (int i = 1; i < m; i++) cum[i] = cum[i - 1] + (pts[i] - pts[i - 1]).magnitude;
                var held = HeldUnder(map, chain.Select(x => x.b).ToList(), pts, cum);
                if (held.Count > 0)
                    for (int i = 0; i < m; i++)
                        foreach (int j in held) { float dd = cum[i] - cum[j]; sm[i] = Mathf.Max(sm[i], raw[j] - dd * dd / (2f * ExpressCrestR)); }
                for (int i = 0; i < m; i++) if (i == 0 || i == m - 1) sm[i] = raw[i];
                float up = 0f, down = 0f; for (int i = 0; i < m; i++) { up = Mathf.Max(up, sm[i] - raw[i]); down = Mathf.Max(down, raw[i] - sm[i]); }
                float downHeld = 0f; foreach (int j in held) downHeld = Mathf.Max(downHeld, raw[j] - sm[j]);
                // back into the pieces
                int at = 0;
                foreach (var (b, rev) in chain)
                {
                    int n = b.deck_y.Length;
                    for (int k = 0; k < n; k++)
                    {
                        int i = rev ? n - 1 - k : k;
                        int idx = k == 0 && at > 0 ? at - 1 : at++;
                        b.deck_y[i] = sm[idx];
                    }
                }
                log.Add($"{name}: crest {c0:0.000} -> {Crest(sm):0.000} m a step, sag {s0:0.000} -> {Sag(sm):0.000} m (limit {c:0.000} / {2f * c:0.000} at {h:0.0} m spacing, a {ExpressCrestR:0} m curve) " +
                        $"smoothed over {reach:0} m; {held.Count} point(s) over something held (lowered there at most {downHeld:0.00} m); raised at most {up:0.00} m, lowered at most {down:0.00} m; the outer ends kept");
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(ProjectRoot, GeneratedDir, "round80_express_decks.txt"), string.Join("\n", log) + "\n");
            Debug.Log($"[Round80] D1: {chains.Count} Express Lanes deck chain(s) of {pieces.Count} piece(s): " + string.Join("; ", log));
        }
    }
}
