using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 66 (3): the road closures at the map's edge, as the map build made them (MapBuilder.Edges.cs): where each stands, the road's way out,
    // its half width, a highway's jersey barriers or the barricades, the road's name and class, where the traffic network ends before it.
    // World data (the closures themselves are meshes and colliders beside this); read by the checks.
    public sealed class RoadClosures : MonoBehaviour
    {
        public static RoadClosures Instance { get; private set; }
        [SerializeField] public List<Vector2> at = new List<Vector2>(), dir = new List<Vector2>(), cut = new List<Vector2>();
        [SerializeField] public List<float> half = new List<float>();
        [SerializeField] public List<bool> highway = new List<bool>();
        [SerializeField] public List<string> names = new List<string>(), classes = new List<string>();
        public int Count => at.Count;
        public void Add(Vector2 a, Vector2 d, float h, bool hw, string name, string cls, Vector2 c) { at.Add(a); dir.Add(d); half.Add(h); highway.Add(hw); names.Add(name); classes.Add(cls); cut.Add(c); }
        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
