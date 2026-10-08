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

        // True when p is on a road's pavement (within its half width plus 1.5 m).
        public bool OnRoad(Vector3 p, out string name, out float distance)
        {
            bool found = Nearest(p, out name, out distance, out float half);
            return found && distance <= half + 1.5f;
        }
    }
}
