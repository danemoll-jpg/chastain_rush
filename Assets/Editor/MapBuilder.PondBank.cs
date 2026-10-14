using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 25 (Dan, BUG-002 at the Enclave pond, x 1654, z 950: "Water cant act like this. Need to fix this so that he water flush with the
// ground"). The pond's water is drawn level at its surface across its outline; on its east side the ground just outside the outline lies
// up to 1.5 m lower than that surface, so the water's edge stood in the air over the bank. Here the ground round the pond (Dan's pond only,
// W4, as the rule says: other ponds with the same are listed in the build log) is raised (never lowered) to 5 cm over the water's surface
// at its outline, easing back to the natural ground over 6 m; never on a road, drive, sidewalk or the trail, eased in beside a building.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly int[] PondBankPonds = { 4 };

        static void RaisePondBanks(TerrainGrid terrain)
        {
            if (Beds == null) return;
            var others = new List<string>();
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
                        worst = Mathf.Max(worst, surf - Ground(terrain, o.x, o.y));
                    }
                }
                if (!PondBankPonds.Contains(wi)) { if (worst > 0.3f) others.Add($"W{wi} ({worst:0.0} m)"); continue; }
                int raised = 0, kept = 0; float most = 0f;
                EditTerrain(terrain, BoundsOf(ring, 7f), (x, z, y) =>
                {
                    var q = new Vector2(x, z); if (InPolygon(q, ring)) return y;
                    float d = DistToEdges(q, ring); if (d > 6f) return y;
                    float target = surf + 0.05f - Mathf.Max(0f, d - 1f) * 0.25f;   // flush at the edge, falling 1 in 4 from 1 m out
                    if (target <= y + 0.01f) return y;
                    target = Mathf.Lerp(target, y, Smooth01((d - 4f) / 2f));      // eased back to the natural ground by 6 m
                    if (target <= y + 0.01f) return y;
                    if (DriveBands.At(q, 0.3f) != null) { kept++; return y; }
                    float w = NearBuilding(q); if (w < 1f) { if (w <= 0f) { kept++; return y; } target = Mathf.Lerp(y, target, w); }
                    raised++; most = Mathf.Max(most, target - y); if (target - y > 0.15f) Regraded.Add($"{x:0.00} {z:0.00} {target - y:0.00} W{wi}");
                    return target;
                });
                Debug.Log($"[Round25] pond W{wi} (BUG-002): the ground round it raised to meet its water ({surf:0.00} m): {raised} samples, up to {most:0.00} m (it lay up to {worst:0.00} m under the surface just outside)" + (kept > 0 ? $"; {kept} on a road, drive, sidewalk or the trail or by a building left as they were" : ""));
            }
            Debug.Log("[Round25] other ponds whose water's edge stands over the ground beside it (more than 0.3 m), not changed (for Dan): " + (others.Count == 0 ? "none" : string.Join(", ", others)));
        }
    }
}
