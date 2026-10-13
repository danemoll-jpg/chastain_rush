using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 23 follow-ups: the trigger in a manhole riser's top, just under its lid (ManholeLid): a vehicle coming up hits it and the lid flies
    // (its own file: Unity saves a component only when its class has a file of its name)
    public sealed class ManholeTrigger : MonoBehaviour
    {
        public ManholeLid owner;
        void OnTriggerEnter(Collider other) { var rb = other.attachedRigidbody; if (rb && rb.GetComponent<ArcadeVehicle>()) owner.Burst(rb); }
        void OnTriggerStay(Collider other) { var rb = other.attachedRigidbody; if (rb && rb.GetComponent<ArcadeVehicle>()) owner.Burst(rb); }
    }
}
