using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Which road is this? Holds every road and bridge centreline of the map (filled in by MapBuilder, so it travels with
    // the scene and also works in a player build) and answers "nearest road to this point" for the debug HUD, the bug
    // report and the bump log.
    public sealed class RoadLookup : MonoBehaviour
    {
        public static RoadLookup Instance { get; private set; }

        [SerializeField] string[] names = new string[0];
        [SerializeField] float[] halfWidths = new float[0];
        [SerializeField] int[] segmentRoad = new int[0];
        [SerializeField] Vector2[] segmentA = new Vector2[0], segmentB = new Vector2[0];

        const float Cell = 32f;
        readonly Dictionary<Vector2Int, List<int>> grid = new Dictionary<Vector2Int, List<int>>();

        public void Fill(List<string> roadNames, List<float> roadHalfWidths, List<int> road, List<Vector2> a, List<Vector2> b)
        {
            names = roadNames.ToArray(); halfWidths = roadHalfWidths.ToArray(); segmentRoad = road.ToArray();
            segmentA = a.ToArray(); segmentB = b.ToArray();
        }

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < segmentA.Length; i++)
            {
                var a = segmentA[i]; var b = segmentB[i];
                int x0 = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - 40f) / Cell), x1 = Mathf.FloorToInt((Mathf.Max(a.x, b.x) + 40f) / Cell);
                int z0 = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - 40f) / Cell), z1 = Mathf.FloorToInt((Mathf.Max(a.y, b.y) + 40f) / Cell);
                for (int x = x0; x <= x1; x++)
                    for (int z = z0; z <= z1; z++)
                    {
                        var key = new Vector2Int(x, z);
                        if (!grid.TryGetValue(key, out var list)) grid[key] = list = new List<int>();
                        list.Add(i);
                    }
            }
        }

        // Nearest road within 40 m: its name (or "unnamed <width>m road"), the distance from its centreline, and its half width.
        public bool Nearest(Vector3 p, out string name, out float distance, out float halfWidth)
        {
            name = null; distance = float.MaxValue; halfWidth = 0f;
            if (!grid.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.z / Cell)), out var list)) return false;
            int best = -1;
            var q = new Vector2(p.x, p.z);
            foreach (int i in list)
            {
                var ab = segmentB[i] - segmentA[i];
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - segmentA[i], ab) / ab.sqrMagnitude);
                float d = (segmentA[i] + ab * t - q).magnitude;
                if (d < distance) { distance = d; best = i; }
            }
            if (best < 0 || distance > 40f) return false;
            int r = segmentRoad[best];
            halfWidth = halfWidths[r];
            name = string.IsNullOrEmpty(names[r]) ? $"unnamed {halfWidths[r] * 2f:0}m road" : names[r];
            return true;
        }

        // Round 75 (B): the name of the street a point is on (within its half width and 3 m more), short ("Chastain Rd NW"); "" off a named road
        public string StreetAt(Vector3 p)
        {
            if (!Nearest(p, out var name, out float d, out float half) || d > half + 3f || name.StartsWith("unnamed ")) return "";
            return Short(name);
        }
        // Round 75 (B): a street's short name for the maps (the map screen's labels, the minimap): the type and direction words after the first shortened
        static readonly System.Collections.Generic.Dictionary<string, string> Abbrev = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Northwest", "NW" }, { "Northeast", "NE" }, { "Southwest", "SW" }, { "Southeast", "SE" }, { "Road", "Rd" }, { "Drive", "Dr" }, { "Street", "St" },
            { "Lane", "Ln" }, { "Court", "Ct" }, { "Circle", "Cir" }, { "Parkway", "Pkwy" }, { "Boulevard", "Blvd" }, { "Avenue", "Ave" }, { "Place", "Pl" },
            { "Trail", "Trl" }, { "Highway", "Hwy" }, { "Terrace", "Ter" }, { "Connector", "Conn" }, { "Memorial", "Mem" },
        };
        public static string Short(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var w = name.Split(' ');
            for (int i = 1; i < w.Length; i++) if (Abbrev.TryGetValue(w[i], out var a)) w[i] = a;   // (the first word kept: "North Marietta Pkwy", "Court Ln")
            return string.Join(" ", w);
        }

        // Round 10 (CarRecovery): the nearest point on a road's centreline within 40 m and the road's direction there
        public bool NearestPoint(Vector3 p, out Vector2 point, out Vector2 tangent)
        {
            point = default; tangent = Vector2.up;
            if (!grid.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.z / Cell)), out var list)) return false;
            var q = new Vector2(p.x, p.z); float best = float.MaxValue;
            foreach (int i in list)
            {
                var ab = segmentB[i] - segmentA[i];
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - segmentA[i], ab) / ab.sqrMagnitude);
                var c = segmentA[i] + ab * t; float d = (c - q).magnitude;
                if (d < best && ab.sqrMagnitude > 1e-6f) { best = d; point = c; tangent = ab.normalized; }
            }
            return best <= 40f;
        }

        // Round 73 (C3, Dan's BUG-006: "stuck here. reset button does nothing"): the nearest road point within maxDist (every segment searched:
        // a reset far from the roads, the quarry's pit, the mountain's flanks), its direction there
        public bool NearestPointWithin(Vector3 p, float maxDist, out Vector2 point, out Vector2 tangent)
        {
            point = default; tangent = Vector2.up; var q = new Vector2(p.x, p.z); float best = maxDist;
            for (int i = 0; i < segmentA.Length; i++)
            {
                var a = segmentA[i]; var ab = segmentB[i] - a; if (ab.sqrMagnitude < 1e-6f) continue;
                if (Mathf.Abs(a.x - q.x) > maxDist + 200f || Mathf.Abs(a.y - q.y) > maxDist + 200f) continue;
                float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude); var c = a + ab * t; float d = (c - q).magnitude;
                if (d < best) { best = d; point = c; tangent = ab.normalized; }
            }
            return best < maxDist;
        }

        // True when p is on a road's pavement (within its half width plus 1.5 m).
        public bool OnRoad(Vector3 p, out string name, out float distance)
        {
            bool found = Nearest(p, out name, out distance, out float half);
            return found && distance <= half + 1.5f;
        }
    }
}
