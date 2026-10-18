using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 42 (B1): breakables that are whole again when the player resets (R / Y) within 'reach' of them, as the stunt park's windows
    // (StuntPark.RestoreBreakables); a race's start restores them too (FenceChunk.RestoreAll). World piece: the church's front windows.
    public sealed class RestoreOnReset : MonoBehaviour
    {
        public FenceChunk[] chunks;
        public float reach = 60f;
        ArcadeVehicle player; VehicleRespawn respawn;

        public void Fill(FenceChunk[] c, float r) { chunks = c; reach = r; }

        void Start()
        {
            var sw = FindAnyObjectByType<VehicleSwitch>(); player = sw ? sw.GetComponent<ArcadeVehicle>() : FindAnyObjectByType<ArcadeVehicle>();
            respawn = player ? player.GetComponent<VehicleRespawn>() : null; if (respawn) respawn.Respawned += OnReset;
        }
        void OnDestroy() { if (respawn) respawn.Respawned -= OnReset; }

        void OnReset()
        {
            if (!player || chunks == null) return;
            bool near = false; foreach (var c in chunks) if (c && Near(c, player.Body.position)) { near = true; break; }
            if (near) foreach (var c in chunks) if (c) c.RestoreOwn();
        }
        bool Near(FenceChunk c, Vector3 p)
        {
            var b = c.GetComponent<Renderer>() ? c.GetComponent<Renderer>().bounds : new Bounds(c.transform.position, Vector3.zero);
            return b.SqrDistance(p) < reach * reach;
        }
    }
}
