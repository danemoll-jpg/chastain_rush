using System;
using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 80 (E, Dan: "they are always moving in just one direction. Please use these directions"): the Northwest Corridor Express Lanes run one way at a
    // time, on the game's clock: southbound (toward Atlanta) 1:00 AM to 11:00 AM, reversal 11:00 AM to 1:30 PM, northbound 1:30 PM to 11:00 PM, reversal
    // 11:00 PM to 1:00 AM. The hour: Free Roam's clock (WorldLook.Hour); in a race its time of day (Dawn 6:30, Day 3:00 PM, Dusk 7:30 PM, Night 2:00 AM);
    // in a replay the hour it was (ReplayRecorder's Clock events). World piece (built by MapBuilder.Round80.cs), as the trains are.
    //   The traffic graph (TrafficNetwork.SetDirection): the lanes' own edges one way, the open way; in a reversal they keep the way they had and no one
    //   gets on (the ramps to and from them closed), so the cars already in finish their trip. The police paths (RoadNet.Blocked) the same.
    //   The gates (ExpressGate): one on every ramp on or off the lanes, its swing arm across the ramp when the ramp is not in use, swung back along its side
    //   when it is. The arms break when hit (Dan, 18:02: "i want them to break, but you would be driving against traffic"); a broken arm comes back once
    //   no vehicle has been near it for a while. Breaks go into replays (ReplayData.Kind.ExpressGate).
    public sealed class ExpressLanes : MonoBehaviour
    {
        public const string Name = "Northwest Corridor Express Lanes";
        public static readonly Vector2 South = new Vector2(.45f, -.89f).normalized;   // the lanes' southbound heading, roughly (I-75 south-east, I-575 south)
        public enum State { Southbound, ReversalToNorth, Northbound, ReversalToSouth }
        public static ExpressLanes Instance { get; private set; }
        public static float ForceHour = float.NaN;    // checks
        public static float ReplayHour = float.NaN;   // ReplayViewer
        public static event Action<int> GateBroken;

        public ExpressGate[] gates = new ExpressGate[0];
        public State Now { get; private set; } = (State)(-1);
        float next;
        readonly List<int> mainEdges = new(), linkEdges = new();

        public static float Hour
        {
            get
            {
                if (!float.IsNaN(ForceHour)) return ForceHour;
                if (WorldClock.Replaying && !float.IsNaN(ReplayHour)) return ReplayHour;
                var look = WorldLook.Current; if (!look) return 8f;
                if (look.Mode == "Free Roam") return look.Hour;
                return look.RaceTime == TimeOfDay.Dawn ? 6.5f : look.RaceTime == TimeOfDay.Dusk ? 19.5f : look.RaceTime == TimeOfDay.Night ? 2f : 15f;
            }
        }
        public static State StateAt(float h)
        {
            h = Mathf.Repeat(h, 24f);
            return h >= 1f && h < 11f ? State.Southbound : h >= 11f && h < 13.5f ? State.ReversalToNorth : h >= 13.5f && h < 23f ? State.Northbound : State.ReversalToSouth;
        }
        public static bool Open(State s) => s == State.Southbound || s == State.Northbound;
        // the way the lanes run (in a reversal: the way they ran)
        public static bool SouthNow(State s) => s == State.Southbound || s == State.ReversalToNorth;
        public static string Describe(State s) => s switch { State.Southbound => "southbound (toward Atlanta)", State.Northbound => "northbound", State.ReversalToNorth => "reversal (closed; was southbound)", _ => "reversal (closed; was northbound)" };

        void Awake() { Instance = this; for (int i = 0; i < gates.Length; i++) if (gates[i]) gates[i].index = i; }
        void OnDestroy() { if (Instance == this) Instance = null; if (RoadNet.Blocked == PoliceBlocked) RoadNet.Blocked = null; }

        void Start()
        {
            var net = TrafficNetwork.Instance;
            if (net) try
            {
                var main = new HashSet<int>();
                for (int e = 0; e < net.EdgeCount; e++) if (net.SafeName(e) == Name) { main.Add(e); mainEdges.Add(e); }
                // the ramps: unnamed links that touch the lanes (their first edge)
                for (int e = 0; e < net.EdgeCount; e++)
                {
                    if (main.Contains(e) || net.SafeName(e).Length > 0 || !net.ClassName(e).EndsWith("_link")) continue;
                    bool touches = false;
                    foreach (int n in new[] { net.From(e), net.To(e) }) foreach (int o in net.Adjacent(n)) if (main.Contains(o)) touches = true;
                    if (touches) linkEdges.Add(e);
                }
            }
            catch (Exception ex) { Debug.LogWarning($"[Round80] Express Lanes: the traffic graph ({net.EdgeCount} edges, {net.NodeCount} nodes): {ex.GetType().Name} {ex.Message} {ex.StackTrace}"); }
            RoadNet.Blocked = PoliceBlocked;
            Debug.Log($"[Round80] Express Lanes: {mainEdges.Count} lane edges, {linkEdges.Count} ramp edges, {gates.Length} gates");
        }

        void Update()
        {
            if (Time.unscaledTime < next) return; next = Time.unscaledTime + .5f;
            var s = StateAt(Hour);
            if (s != Now) { Now = s; Apply(); }
            foreach (var g in gates) if (g) g.Want(GateOpen(g));
        }
        public bool GateOpen(ExpressGate g) => Open(Now) && (SouthNow(Now) ? g.usableSouth : g.usableNorth);

        // the traffic graph: the lanes one way (the open way, or the way they had), the ramps open one way or closed
        void Apply()
        {
            var net = TrafficNetwork.Instance; if (!net) return;
            bool south = SouthNow(Now), open = Open(Now);
            foreach (int e in mainEdges) net.SetDirection(e, (sbyte)(Southward(net, e) == south ? 1 : -1), open);
            foreach (int e in linkEdges)
            {
                sbyte d = (sbyte)(Southward(net, e) == south ? 1 : -1);
                if (!open || net.BaseOneway(e) && d == -1) d = 2;
                net.SetDirection(e, d, open);
            }
            Debug.Log($"[Round80] Express Lanes at {Hour:0.00} h: {Describe(Now)}");
        }
        // an edge's from -> to runs southward
        static bool Southward(TrafficNetwork net, int e) { var d = net.NodePos(net.To(e)) - net.NodePos(net.From(e)); return Vector2.Dot(d, South) > 0f; }

        // the police paths: on the lanes only the way they run; onto them only while they are open
        bool PoliceBlocked(RoadNet rn, int a, int b)
        {
            bool onA = rn.RoadNames[rn.Road[a]] == Name, onB = rn.RoadNames[rn.Road[b]] == Name;
            if (!onB) return false;
            if (!onA && !Open(Now)) return true;
            var d = rn.P[b] - rn.P[a]; bool southward = Vector2.Dot(new Vector2(d.x, d.z), South) > 0f;
            return southward != SouthNow(Now);
        }

        // ---------------------------------------------------------------- the gates
        public static void Broke(ExpressGate g) => GateBroken?.Invoke(g.index);
        public static void ReplayBreak(int index, bool show) { var l = Instance; if (l && index >= 0 && index < l.gates.Length && l.gates[index]) l.gates[index].Break(Vector3.zero, show, false); }
        public static void RestoreAll() { var l = Instance; if (l) foreach (var g in l.gates) if (g) g.Restore(); }
        public static void ApplyNow() { var l = Instance; if (!l) return; l.next = 0f; l.Update(); foreach (var g in l.gates) if (g) g.Snap(); }
    }
}
