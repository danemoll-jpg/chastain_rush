using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 56 (B, Dan BUG-003 at the Elaine turn-off: "Maybe actually block the road off and make tiremarks right after it"): the barricade
    // across Elaine Circle's mouth is whole again for the next car. Once broken, its chunks come back when no vehicle has been within 'clear'
    // metres for 'after' seconds (so every lap shows it standing), as well as at race start (FenceChunk.RestoreAll).
    public sealed class RestoreWhenClear : MonoBehaviour
    {
        public float clear = 25f, after = 4f;
        FenceChunk[] chunks;
        ArcadeVehicle[] vehicles;
        float quietSince, nextScan;

        void Start() { chunks = GetComponentsInChildren<FenceChunk>(true); quietSince = Time.time; }

        void Update()
        {
            if (vehicles == null || Time.time >= nextScan) { vehicles = FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None); nextScan = Time.time + 1f; }
            float c2 = clear * clear; bool near = false;
            foreach (var v in vehicles)
                if (v && v.isActiveAndEnabled && (v.transform.position - transform.position).sqrMagnitude < c2) { near = true; break; }
            if (near) { quietSince = Time.time; return; }
            if (Time.time - quietSince < after) return;
            foreach (var ch in chunks) if (ch) ch.RestoreOwn();
            quietSince = Time.time;
        }
    }
}
