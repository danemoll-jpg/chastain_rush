using System.Linq;
using UnityEngine;

// Round 34 (Dan, 2026-10-16, reports of session 2026-10-16_01-35-35-872_a132c0). A1 / A2 (BUG-001 to 004) are in mapgen
// (road_flatten.wider_wins_rects); A4 (stall lines only where a lot is near level) in MapBuilder.Businesses.cs (StallLevel);
// A5 (a road's paint stops on the road it joins) in MapBuilder.cs (RoadIndex.CoversPaint). Here:
// A3 (BUG-010, x 3325.65 z 1024.61: "assume this is a driveway to the house. Please take the driveway to the house"): the
//   unnamed 5 m road 564629063 off Chastain Corner stopped 18 m short of building 2158. It runs straight on, at its own width
//   and in the lots' asphalt, to 0.5 m short of the building's wall.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const long Bug010Road = 564629063; const int Bug010Building = 2158;

        static void Round34Drives(Businesses biz, MapData map)
        {
            var r = map.roads.FirstOrDefault(q => q.osm_id == Bug010Road);
            if (r == null || Bug010Building >= map.buildings.Length) { biz.Log.Append("[Round34] BUG-010 road or building not found; "); return; }
            var p = ToPoints(r.points); var ring = ToPoints(map.buildings[Bug010Building].footprint);
            Vector2 end = p[p.Count - 1], dir = (end - p[p.Count - 2]).normalized;
            float s = 0f; while (s < 40f && !InPolygon(end + dir * (s + 0.5f), ring)) s += 0.25f;
            if (s >= 40f) { biz.Log.Append("[Round34] BUG-010: the road's line misses building 2158; "); return; }
            biz.Drives.Add((end - dir * 1f, end + dir * s, r.width * 0.5f));
            biz.Log.Append($"[Round34] BUG-010: road {Bug010Road} runs on {s:0.0} m to building {Bug010Building}; ");
        }
    }
}
