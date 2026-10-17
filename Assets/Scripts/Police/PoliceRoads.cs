using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): every drivable road of the map for the police (filled in by the map build from Data/map.json): its name, OSM
    // class, half width, posted limit (OSM's maxspeed where tagged, else by class: PoliceRoads.DefaultLimit) and centerline. RoadNet is built from it;
    // the speed limit tests read it.
    // (in its own file: a MonoBehaviour placed in the scene by MapBuilder.Police.cs must live in a file named after it)
    public sealed class PoliceRoads : MonoBehaviour
    {
        public static PoliceRoads Instance { get; private set; }
        public string[] names = new string[0], classes = new string[0];
        public float[] halfWidths = new float[0];
        public int[] limits = new int[0];        // mph
        public int[] first = new int[0];         // road i is points first[i] .. first[i + 1] - 1
        public Vector2[] points = new Vector2[0];
        void Awake() => Instance = this;
        public int Count => Mathf.Max(0, first.Length - 1);
        // Round 39 (D2): the defaults where OSM gives no limit (Georgia's usual postings): interstate 65, its ramps 45, a trunk road 55, a primary 45,
        // a secondary 40, a tertiary 35, a neighborhood street 25, a service road or lot 15
        public static int DefaultLimit(string cls) => cls switch
        {
            "motorway" => 65, "motorway_link" => 45, "trunk" => 55, "trunk_link" => 45, "primary" => 45, "primary_link" => 35, "secondary" => 40, "secondary_link" => 35,
            "tertiary" => 35, "tertiary_link" => 30, "service" => 15, _ => 25,
        };
        // the posted limit at a point and whether the point is on that road (within its half width + 2.5 m)
        Dictionary<Vector2Int, List<int>> grid; int[] segRoad;
        public int LimitAt(Vector3 p, out bool onRoad, out int road)
        {
            onRoad = false; road = -1;
            if (grid == null)
            {
                grid = new(); segRoad = new int[points.Length];
                for (int r = 0; r < Count; r++)
                    for (int i = first[r]; i + 1 < first[r + 1]; i++)
                    {
                        segRoad[i] = r; var a = points[i]; var b = points[i + 1];
                        for (int x = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - 20f) / 32f); x <= Mathf.FloorToInt((Mathf.Max(a.x, b.x) + 20f) / 32f); x++)
                            for (int z = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - 20f) / 32f); z <= Mathf.FloorToInt((Mathf.Max(a.y, b.y) + 20f) / 32f); z++)
                            { var k = new Vector2Int(x, z); if (!grid.TryGetValue(k, out var l)) grid[k] = l = new(); l.Add(i); }
                    }
            }
            var q = new Vector2(p.x, p.z); float best = float.MaxValue;
            if (grid.TryGetValue(new Vector2Int(Mathf.FloorToInt(q.x / 32f), Mathf.FloorToInt(q.y / 32f)), out var list))
                foreach (int i in list)
                {
                    var a = points[i]; var ab = points[i + 1] - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                    float d = (a + ab * t - q).magnitude - halfWidths[segRoad[i]]; if (d < best) { best = d; road = segRoad[i]; }
                }
            if (road < 0) return 25;
            onRoad = best < 2.5f; return limits[road];
        }
    }
}
