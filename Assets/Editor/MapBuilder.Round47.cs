using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 47 (B, BUG-001 of session 2026-10-19_01-29-19-650_77c31b at x 1304.53 z 581.52, Dan: "These driveways shouldn't be here. The driveways would
// be from inside the neighborhood not on Chastain"): the row of townhouses on the north side of Chastain Road there had drives that mapgen ran straight
// to the nearest road, Chastain Road. Their drives belong to the complex's own street (the service road behind them): each one is re-run from the house
// to that street if a straight drive clears the other buildings, and dropped otherwise (a lawn and the sidewalk on Chastain Road). This row only.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly Rect R47Row = new Rect(1255f, 580f, 95f, 50f);   // the row's houses and their drives' ends on Chastain Road (x, z)
        const long R47Street = 392525239;                                 // the complex's own street

        static void Round47Drives(MapData map)
        {
            if (map.driveways == null) return;
            var chastain = map.roads.Where(r => r.name == "Chastain Road Northwest").Select(r => (p: ToPoints(r.points), half: r.width * 0.5f)).ToList();
            var street = map.roads.FirstOrDefault(r => r.osm_id == R47Street);
            var sp = street != null ? ToPoints(street.points) : new List<Vector2>();
            var rings = map.buildings.Select(b => ToPoints(b.footprint)).ToList();
            float Dist(Vector2 q, List<Vector2> p, out Vector2 at)
            {
                float best = float.MaxValue; at = q;
                for (int i = 0; i + 1 < p.Count; i++) { var c = ClosestOnSeg(q, p[i], p[i + 1]); float d = (c - q).magnitude; if (d < best) { best = d; at = c; } }
                return best;
            }
            bool Crosses(Vector2 a, Vector2 b, int self)
            {
                for (int j = 0; j < rings.Count; j++)
                {
                    if (j == self) continue; var r = rings[j]; if (r.Count < 3) continue;
                    for (int k = 0; k <= 20; k++)
                    {
                        var q = Vector2.Lerp(a, b, k / 20f);
                        if (InPolygon(q, r)) return true;
                        for (int e = 0; e < r.Count; e++) if ((ClosestOnSeg(q, r[e], r[(e + 1) % r.Count]) - q).magnitude < 1.2f) return true;
                    }
                }
                return false;
            }
            var log = new List<string>();
            for (int di = 0; di < map.driveways.Length; di++)
            {
                var d = map.driveways[di]; var p = ToPoints(d.points);
                if (d.source != "generated" || p.Count < 2 || !R47Row.Contains(p[0]) || !R47Row.Contains(p[p.Count - 1])) continue;
                var end = p[p.Count - 1];
                if (!chastain.Any(c => Dist(end, c.p, out _) < c.half + 2f)) continue;
                // the house it leaves: the footprint its start is in (or nearest)
                int house = -1; float hd = 3f;
                for (int j = 0; j < rings.Count; j++)
                {
                    if (rings[j].Count < 3) continue;
                    float dd = InPolygon(p[0], rings[j]) ? 0f : Enumerable.Range(0, rings[j].Count).Min(e => (ClosestOnSeg(p[0], rings[j][e], rings[j][(e + 1) % rings[j].Count]) - p[0]).magnitude);
                    if (dd < hd) { hd = dd; house = j; }
                }
                string was = $"drive {di} (house {house}) onto Chastain Road at ({end.x:0.0}, {end.y:0.0})";
                // re-run: from the footprint's edge nearest the complex's street, straight to its edge, if it clears every other building
                bool done = false;
                if (house >= 0 && sp.Count > 1)
                {
                    var ring = rings[house]; Vector2 bestA = default, bestB = default; float bestL = float.MaxValue;
                    for (int e = 0; e < ring.Count; e++)
                        for (int k = 0; k <= 4; k++)
                        {
                            var a = Vector2.Lerp(ring[e], ring[(e + 1) % ring.Count], k / 4f);
                            float L = Dist(a, sp, out var b);
                            if (L < bestL) { bestL = L; bestA = a; bestB = b; }
                        }
                    float half = street.width * 0.5f;
                    if (bestL < 40f && bestL > half + 2f)
                    {
                        var u = (bestB - bestA).normalized; var a2 = bestA + u * 0.4f; var b2 = bestB - u * half;
                        if (!Crosses(a2, b2, house))
                        {
                            d.points = new[] { a2.x, a2.y, b2.x, b2.y }; done = true;
                            log.Add(was + $": now to its own street at ({b2.x:0.0}, {b2.y:0.0}), {(b2 - a2).magnitude:0.0} m");
                        }
                    }
                }
                if (!done) { d.points = new float[0]; log.Add(was + ": dropped (no straight way to its own street past the other houses)"); }
            }
            Debug.Log("[Round47] the drives on Chastain Road at Dan's spot: " + (log.Count == 0 ? "none found" : string.Join("; ", log)));
        }
    }
}
