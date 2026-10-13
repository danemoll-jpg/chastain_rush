using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 24 Part A2, item A3 (BUG-001 at the creek-trail exit, Dan: "this needs to be blocked off during races"; "it routed everyone
    // to go down the pipe even though it wasn't part of the race"): every pipe mouth in the world (the network's entrances and exits,
    // P1's ends), written by the map build (MapBuilder.Pipes.cs). World data only: RaceBarriers closes, while a race runs, each mouth
    // that neither the lap nor an official shortcut of that race goes through, with the same red and white blocks as its road
    // closures; free roam leaves them all open.
    public sealed class PipeMouths : MonoBehaviour
    {
        public string[] ids;
        public Vector3[] mouth;    // the mouth's middle on its floor
        public Vector3[] inner;    // a point on the cut's floor further in (toward the headwall), on the same line
        public float[] half;       // the cut's half width at the mouth (inside its walls)
        public static PipeMouths Instance { get; private set; }

        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; }

        // the mouths a race with these lines does not use
        public List<int> Unused(IEnumerable<Vector3[]> lines)
        {
            var used = new bool[ids.Length];
            foreach (var line in lines)
                for (int i = 0; i + 1 < line.Length; i++)
                {
                    Vector2 p0 = new Vector2(line[i].x, line[i].z), p1 = new Vector2(line[i + 1].x, line[i + 1].z);
                    int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(p0, p1) / 1f));
                    for (int k = 0; k <= n; k++)
                    {
                        var q = Vector2.Lerp(p0, p1, k / (float)n);
                        for (int m = 0; m < ids.Length; m++)
                        {
                            if (used[m]) continue;
                            Vector2 a = new Vector2(mouth[m].x, mouth[m].z), b = new Vector2(inner[m].x, inner[m].z), ab = b - a;
                            float t = Vector2.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f);
                            // through it: inside the cut 3 m or more in from the mouth (a line that only passes the mouth, as the creek
                            // trail passes its exit, does not count)
                            if (t * ab.magnitude >= 3f && t <= 1f && (a + ab * t - q).magnitude < half[m] + 1f) used[m] = true;
                        }
                    }
                }
            var list = new List<int>();
            for (int m = 0; m < ids.Length; m++) if (!used[m]) list.Add(m);
            return list;
        }
    }
}
