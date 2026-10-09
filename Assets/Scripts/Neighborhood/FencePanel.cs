using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 8: moved out of FenceChunk.cs (was BreakableFence.cs). A MonoBehaviour must live in a file of its own name, or a
    // player build loses it ("script is missing") and writes a broken scene.
    // The trigger box of one panel (a child of its FenceChunk).
    public sealed class FencePanel : MonoBehaviour
    {
        public FenceChunk chunk;
        public int index;
        void OnTriggerEnter(Collider other) => Touch(other);
        void OnTriggerStay(Collider other) => Touch(other);
        void Touch(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (!rb) return;
            var car = rb.GetComponent<ArcadeVehicle>();
            if (car) chunk.Hit(index, car);
        }
    }
}
