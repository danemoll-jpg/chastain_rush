using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 25 (B1, Dan: "is there a way I can warp to specific places in the pipe area so it is easier for me to determine issues and
    // check out things"): the places the debug menu's "Pipe warp" row steps through (GameMenus; free roam only), written by the map
    // build (MapBuilder.Network.cs): the hub, every junction (J*, K*, and the shafts and turning rooms), both pipes of BF and of GP,
    // every exit's inside end and outside mouth, every manhole's foot and the middle of each main, named as on the map and in
    // Docs/PIPE-NETWORK.md. Go puts the vehicle on the floor there, stopped, facing along the pipe toward the hub where there is one.
    // A bug report's position line names the nearest of them when it is taken underground (DeveloperHud, DebugReportSession).
    public sealed class PipeWarp : MonoBehaviour
    {
        public string[] names;
        public Vector3[] at;      // on the floor
        public float[] yaw;       // facing (degrees)
        public static PipeWarp Instance { get; private set; }
        public int Count => names != null ? names.Length : 0;

        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; }

        public bool Go(int i)
        {
            if (i < 0 || i >= Count) return false;
            var car = FindAnyObjectByType<ArcadeVehicle>();
            var respawn = car ? car.GetComponent<VehicleRespawn>() : null;
            if (!respawn) return false;
            respawn.PlaceAt(at[i] + Vector3.up * 0.3f, yaw[i]);
            var cam = FindAnyObjectByType<ChaseCamera>(); if (cam) cam.Snap();
            return true;
        }

        // the nearest place, if p is underground (in a pipe or chamber, or well under the ground)
        public string NearestUnderground(Vector3 p)
        {
            if (Count == 0) return null;
            bool under = DrainPipe.InsideAny(p) || p.y < RaceRoute.Ground(p.x, p.z).y - 2.5f;
            if (!under) return null;
            int best = -1; float bd = float.MaxValue;
            for (int i = 0; i < Count; i++) { float d = (at[i] - p).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            return best < 0 ? null : $"{names[best]}, {Mathf.Sqrt(bd):0} m";
        }
    }
}
