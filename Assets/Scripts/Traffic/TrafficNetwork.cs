using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 30 (A1): the road graph the traffic drives on, filled in by the map build (MapBuilder.Traffic.cs) so it travels
    // with the scene. World data (which roads join where); the traffic itself (Traffic.cs) is a game layer on top of it.
    // An edge runs from one junction (or dead end) to the next along its road's points; one-way edges only from -> to.
    public sealed class TrafficNetwork : MonoBehaviour
    {
        public static TrafficNetwork Instance { get; private set; }

        [SerializeField] Vector2[] nodes = new Vector2[0];
        [SerializeField] int[] edgeFrom = new int[0], edgeTo = new int[0], edgeClass = new int[0], edgeStart = new int[0], edgeCount = new int[0];
        [SerializeField] bool[] edgeOneway = new bool[0], edgeBridge = new bool[0];
        [SerializeField] float[] edgeHalf = new float[0];
        [SerializeField] Vector2[] points = new Vector2[0];
        [SerializeField] string[] edgeName = new string[0], classNames = new string[0];
        // Round 66: the closures at the map's edge (MapBuilder.Edges.cs): two-way roads' ends there (traffic turns round) and the divided highways' turnarounds
        [SerializeField] int[] turnNodes = new int[0], turnEdges = new int[0];
        HashSet<int> turnNodeSet, turnEdgeSet;

        public int EdgeCount => edgeFrom.Length;
        public int NodeCount => nodes.Length;
        public float[] Length { get; private set; }
        Vector2[] boxLo, boxHi;   // Round 80 (F): each edge's bounds
        // Round 80 (F): how far p is from an edge's bounds (0 inside)
        public float BoxDistance(int e, Vector2 p) { var c = Vector2.Max(boxLo[e], Vector2.Min(p, boxHi[e])); return (p - c).magnitude; }
        List<int>[] adjacent;   // node -> edges touching it

        public void Fill(Vector2[] n, int[] from, int[] to, int[] cls, bool[] oneway, float[] half, bool[] bridge, int[] start, int[] count, Vector2[] pts, string[] names, string[] classes)
        {
            nodes = n; edgeFrom = from; edgeTo = to; edgeClass = cls; edgeOneway = oneway; edgeHalf = half; edgeBridge = bridge;
            edgeStart = start; edgeCount = count; points = pts; edgeName = names; classNames = classes;
        }

        public void SetEdgeEnds(int[] nodesTurn, int[] edgesTurn) { turnNodes = nodesTurn; turnEdges = edgesTurn; }
        public bool TurnRound(int node) => turnNodeSet != null && turnNodeSet.Contains(node);
        public bool IsTurnaround(int e) => turnEdgeSet != null && turnEdgeSet.Contains(e);
        public int TurnNodeCount => turnNodes.Length;
        public int TurnEdgeCount => turnEdges.Length;

        void Awake()
        {
            Instance = this;
            turnNodeSet = new HashSet<int>(turnNodes ?? new int[0]); turnEdgeSet = new HashSet<int>(turnEdges ?? new int[0]);
            adjacent = new List<int>[nodes.Length]; for (int i = 0; i < nodes.Length; i++) adjacent[i] = new List<int>();
            Length = new float[edgeFrom.Length]; boxLo = new Vector2[edgeFrom.Length]; boxHi = new Vector2[edgeFrom.Length];
            for (int e = 0; e < edgeFrom.Length; e++)
            {
                adjacent[edgeFrom[e]].Add(e); if (edgeTo[e] != edgeFrom[e]) adjacent[edgeTo[e]].Add(e);
                float L = 0; for (int i = 1; i < edgeCount[e]; i++) L += (points[edgeStart[e] + i] - points[edgeStart[e] + i - 1]).magnitude; Length[e] = L;
                var lo = points[edgeStart[e]]; var hi = lo; for (int i = 1; i < edgeCount[e]; i++) { lo = Vector2.Min(lo, points[edgeStart[e] + i]); hi = Vector2.Max(hi, points[edgeStart[e] + i]); }
                boxLo[e] = lo; boxHi[e] = hi;
            }
        }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public Vector2 NodePos(int n) => nodes[n];
        public int From(int e) => edgeFrom[e];
        public int To(int e) => edgeTo[e];
        public bool Oneway(int e) => edgeOneway[e] || dirSet != null && dirSet[e] != 0;
        public bool BaseOneway(int e) => edgeOneway[e];
        // Round 80 (E): a way set at run time (the Express Lanes' schedule, ExpressLanes): 0 the road's own, 1 from -> to only, -1 to -> from only,
        // 2 closed; and whether new cars may be put on it
        sbyte[] dirSet; bool[] spawnOff;
        public void SetDirection(int e, sbyte d, bool spawn)
        {
            if (dirSet == null) { dirSet = new sbyte[edgeFrom.Length]; spawnOff = new bool[edgeFrom.Length]; }
            dirSet[e] = d; spawnOff[e] = !spawn || d == 2;
        }
        public sbyte Direction(int e) => dirSet == null ? (sbyte)0 : dirSet[e];
        public bool SpawnOff(int e) => spawnOff != null && spawnOff[e];
        public bool Bridge(int e) => edgeBridge[e];
        public float Half(int e) => edgeHalf[e];
        public string Name(int e) => edgeName[e];
        public string ClassName(int e) => edgeClass[e] >= 0 && edgeClass[e] < classNames.Length ? classNames[edgeClass[e]] : "";   // Round 80: an edge without a class (none in the list) gives ""
        public string SafeName(int e) => e >= 0 && e < edgeName.Length ? edgeName[e] : "";   // Round 80
        public List<int> Adjacent(int node) => adjacent[node];
        public int Degree(int node) => adjacent[node].Count;
        // the node at the far end when entering edge e at node `at` (dir +1: from -> to)
        public int Other(int e, int at) => edgeFrom[e] == at ? edgeTo[e] : edgeFrom[e];
        public bool CanEnter(int e, int at)
        {
            int d = dirSet == null ? 0 : dirSet[e];
            return d == 2 ? false : d == 1 ? edgeFrom[e] == at : d == -1 ? edgeTo[e] == at : !edgeOneway[e] || edgeFrom[e] == at;
        }

        // A road's rank: the bigger road has the right of way at a junction (motorway 6 ... residential 1).
        public int Rank(int e)
        {
            switch (classNames[edgeClass[e]])
            {
                case "motorway": case "trunk": return 6;
                case "primary": return 5;
                case "secondary": return 4;
                case "tertiary": return 3;
                case "motorway_link": case "trunk_link": case "primary_link": case "secondary_link": case "tertiary_link": return 2;
                default: return 1;
            }
        }
        // Speed by road type (m/s): neighborhood streets slow, Chastain Road / Bells Ferry / Canton Road faster, I-575 fastest
        public float Speed(int e)
        {
            switch (classNames[edgeClass[e]])
            {
                case "motorway": case "trunk": return 29f;
                case "primary": return 20f;
                case "secondary": return 18f;
                case "tertiary": return 16f;
                case "motorway_link": case "trunk_link": return 18f;
                case "primary_link": case "secondary_link": case "tertiary_link": return 13f;
                default: return 11f;
            }
        }

        // the edge's points in driving order (dir +1 from -> to, -1 to -> from)
        public void Points(int e, int dir, List<Vector2> into)
        {
            int s = edgeStart[e], n = edgeCount[e];
            if (dir > 0) for (int i = 0; i < n; i++) into.Add(points[s + i]);
            else for (int i = n - 1; i >= 0; i--) into.Add(points[s + i]);
        }
        public Vector2 Point(int e, int i) => points[edgeStart[e] + i];
        public int PointCount(int e) => edgeCount[e];

        // nearest point on edge e to p (plan), its distance
        public float Distance(int e, Vector2 p)
        {
            float best = float.MaxValue; int s = edgeStart[e];
            for (int i = 1; i < edgeCount[e]; i++)
            {
                var a = points[s + i - 1]; var ab = points[s + i] - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, (a + ab * t - p).magnitude);
            }
            return best;
        }
    }
}
