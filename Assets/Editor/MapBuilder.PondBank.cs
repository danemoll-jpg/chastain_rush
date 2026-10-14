using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 25 (Dan, BUG-002 at the Enclave pond, x 1654, z 950: "Water cant act like this. Need to fix this so that he water flush with the
// ground"). The pond's water is drawn level at its surface across its outline; on its east side the ground just outside the outline lies
// up to 1.5 m lower than that surface, so the water's edge stood in the air over the bank. Here the ground round the pond (Dan's pond only,
// W4, as the rule says: other ponds with the same are listed in the build log) is raised (never lowered) to 5 cm over the water's surface
// at its outline, easing back to the natural ground over 6 m; never on a road, drive, sidewalk or the trail, eased in beside a building.
// Round 26 (item 2): every pond and lake whose water's edge stood more than 0.3 m over the ground beside it gets the same, not only W4.
// Never in a stream's bed where it leaves or enters the water (the bed stays open), in another pond, or in a pipe's cut or a bank's
// open channel (the mouths stay as they are); those places are left out of the measure of how low the ground lies too.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const float PondBankFrom = 0.3f;   // Round 26: every pond whose edge stands this much over the ground beside it

        static void RaisePondBanks(TerrainGrid terrain, Pipes pipes)
        {
            if (Beds == null) return;
            var changed = new List<string>();
            // Round 26: places the bank is never raised (a stream's bed at the water, another pond, a pipe's cut or open channel)
            bool Keep(Vector2 q, int wi)
            {
                if (Beds.NearStream(q, out float sd, out _) && sd < StreamHalf + 1.5f) return true;
                for (int j = 0; j < Beds.Rings.Count; j++) if (j != wi && Beds.RingBounds[j].Contains(q) && InPolygon(q, Beds.Rings[j])) return true;
                if (pipes != null) foreach (var pp in pipes.Plans) if (pp.InCut(q, 2f, out _, out _) || pp.InChannel(q)) return true;
                return false;
            }
            for (int wi = 0; wi < Beds.Rings.Count; wi++)
            {
                if (!Beds.Level.TryGetValue(wi, out float lv)) continue;
                var ring = Beds.Rings[wi]; float surf = lv + PondLift;
                // how low the ground gets just outside the outline (1 m out, every 2 m round it)
                float worst = 0f;
                for (int i = 0; i < ring.Count; i++)
                {
                    var a = ring[i]; var b = ring[(i + 1) % ring.Count]; float len = Vector2.Distance(a, b);
                    for (float t = 0f; t < len; t += 2f)
                    {
                        var q = Vector2.Lerp(a, b, t / Mathf.Max(len, 1e-3f)); var e = (b - a).normalized; var n = new Vector2(e.y, -e.x);
                        var o = InPolygon(q + n * 1f, ring) ? q - n * 1f : q + n * 1f;
                        if (Keep(o, wi)) continue;
                        worst = Mathf.Max(worst, surf - Ground(terrain, o.x, o.y));
                    }
                }
                if (worst <= PondBankFrom) continue;
                int raised = 0, kept = 0; float most = 0f;
                EditTerrain(terrain, BoundsOf(ring, 7f), (x, z, y) =>
                {
                    var q = new Vector2(x, z); if (InPolygon(q, ring)) return y;
                    float d = DistToEdges(q, ring); if (d > 6f) return y;
                    float target = surf + 0.05f - Mathf.Max(0f, d - 1f) * 0.25f;   // flush at the edge, falling 1 in 4 from 1 m out
                    if (target <= y + 0.01f) return y;
                    target = Mathf.Lerp(target, y, Smooth01((d - 4f) / 2f));      // eased back to the natural ground by 6 m
                    if (target <= y + 0.01f) return y;
                    if (DriveBands.At(q, 0.3f) != null || Keep(q, wi)) { kept++; return y; }
                    float w = NearBuilding(q); if (w < 1f) { if (w <= 0f) { kept++; return y; } target = Mathf.Lerp(y, target, w); }
                    raised++; most = Mathf.Max(most, target - y); if (target - y > 0.15f) Regraded.Add($"{x:0.00} {z:0.00} {target - y:0.00} W{wi}");
                    return target;
                });
                changed.Add($"W{wi} ({worst:0.0} m, raised up to {most:0.00} m)");
                Debug.Log($"[Round26] pond W{wi}: the ground round it raised to meet its water ({surf:0.00} m): {raised} samples, up to {most:0.00} m (it lay up to {worst:0.00} m under the surface just outside)" + (kept > 0 ? $"; {kept} on a road, drive, sidewalk, the trail, a stream's bed, a pipe's cut or by a building left as they were" : ""));
            }
            Debug.Log($"[Round26] pond banks raised to their water at {changed.Count} ponds and lakes: " + (changed.Count == 0 ? "none" : string.Join(", ", changed)));
        }
    }
}
