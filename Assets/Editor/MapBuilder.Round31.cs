using System.Collections.Generic;
using UnityEngine;

// Round 31, BUG-003 (Dan, on New Chastain Road at x 1524.4, z 561.8, heading 48: "need to remove grass from streets"). The
// westbound carriageway (OSM 290281675) and its right-turn lane onto Bells Ferry Road (OSM 564621144) run side by side for
// about 65 m, 6.3 to 8.4 m apart between their centerlines; their two ribbons barely overlap, and the lawn showed through the
// seam as a ragged strip along the middle. The ground between the two centerlines is paved over that stretch (wherever they
// are less than 9.5 m apart), following the ground like the Glen's street grids, so the asphalt is whole. Nothing else moves.
// BUG-002 (the buried garage row at x 3405 to 3536, z 488 to 500) is fixed in MapBuilder.Businesses.cs (stepped bodies).
// BUG-004 (the I-575 / Chastain Road ramps, added by mapgen): where the northbound exit ramp converges on Chastain Road at about
// 46 degrees, and where the northbound entrance ramp leaves it, the two roads' levelled grounds met in a step up to 0.75 m high
// (the exit drive flat out rolled the car there). That junction only is smoothed as Round 17's seams were (RampSeams): the
// pavement's own heights blurred, the grades kept.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly (Vector2 c, float r, float sigma, float fade, string what)[] RampSeams =
        {
            (new Vector2(508f, 912f), 32f, 6f, 12f, "BUG-004 the northbound ramps where they meet Chastain Road"),
        };
        static void SmoothRampSeams(MapData map, TerrainGrid terrain) { foreach (var spot in RampSeams) SmoothSpot(map, terrain, spot); }

        const long SeamRoadA = 290281675, SeamRoadB = 564621144;
        const float SeamMaxApart = 9.5f;

        static void FillRoadSeam(MapData map, TerrainGrid t, Chunks main)
        {
            Road ra = null, rb = null;
            foreach (var r in map.roads) { if (r.osm_id == SeamRoadA && ra == null) ra = r; else if (r.osm_id == SeamRoadB && rb == null) rb = r; }
            if (ra == null || rb == null) { Debug.LogWarning("[Round31] BUG-003: New Chastain Road or its turn lane not found; seam not paved"); return; }
            var pa = Densify(ToPoints(ra.points), 1f); var pb = Densify(ToPoints(rb.points), 0.5f);
            float lift = (RoadLift.TryGetValue(ra.@class, out var l) ? l : 0.06f) * vScale - 0.004f;
            const int across = 8;
            var rows = new List<(Vector2 a, Vector2 b)>();
            float paved = 0f, minApart = float.MaxValue, maxApart = 0f;
            void Flush()
            {
                if (rows.Count >= 2)
                {
                    var mb = main.At((rows[0].a + rows[rows.Count - 1].a) * 0.5f);
                    int start = mb.Count;
                    for (int i = 0; i < rows.Count; i++)
                    {
                        for (int k = 0; k <= across; k++)
                        {
                            var p = Vector2.Lerp(rows[i].a, rows[i].b, k / (float)across);
                            mb.Vertex(new Vector3(p.x, GroundMax(t, p.x, p.y, 0.45f) + lift, p.y));
                        }
                        if (i == 0) continue;
                        for (int k = 0; k < across; k++)
                        {
                            int a = start + (i - 1) * (across + 1) + k, b = a + across + 1;
                            // the winding that faces up (clockwise from above), whichever side of the lane the carriageway is on
                            var v0 = mb.Verts[a]; var v1 = mb.Verts[a + 1]; var v2 = mb.Verts[b];
                            bool up = Vector3.Cross(v1 - v0, v2 - v0).y > 0f;
                            if (up) { mb.Tri(a, a + 1, b); mb.Tri(a + 1, b + 1, b); }
                            else { mb.Tri(a, b, a + 1); mb.Tri(a + 1, b, b + 1); }
                        }
                        paved += Vector2.Distance(rows[i].a, rows[i - 1].a);
                    }
                }
                rows.Clear();
            }
            foreach (var p in pa)
            {
                Vector2 q = p; float best = float.MaxValue;
                for (int i = 0; i + 1 < pb.Count; i++)
                {
                    var ab = pb[i + 1] - pb[i]; float tt = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - pb[i], ab) / ab.sqrMagnitude);
                    var c = pb[i] + ab * tt; float d = (c - p).sqrMagnitude; if (d < best) { best = d; q = c; }
                }
                float apart = Mathf.Sqrt(best);
                if (apart < SeamMaxApart) { rows.Add((p, q)); minApart = Mathf.Min(minApart, apart); maxApart = Mathf.Max(maxApart, apart); }
                else Flush();
            }
            Flush();
            Debug.Log($"[Round31] BUG-003: New Chastain Road and its turn lane onto Bells Ferry Road: the seam between them paved over {paved:0} m (centerlines {minApart:0.0} to {maxApart:0.0} m apart)");
        }
    }
}
