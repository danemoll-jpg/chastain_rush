using UnityEngine;

namespace ChastainRush
{
    // Round 80 (E): an Express Lanes gate's arm (a kinematic body that turns with its hinge): its trigger tells its gate it was hit (ExpressGate.Hit)
    public sealed class ExpressGateArm : MonoBehaviour
    {
        public ExpressGate gate;
        void OnTriggerEnter(Collider c) { if (gate) gate.Hit(c); }
    }
}
