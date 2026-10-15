using System.Text;
using UnityEngine;

namespace ChastainRush
{
    // Round 27 (A2): checks only. Added to the vehicle by ExeShotsRunner for a traced drive: every collision the body has in a
    // physics step (the collider's name, the normal, the impulse), read and cleared by the runner each step.
    public sealed class ContactTracer : MonoBehaviour
    {
        public readonly StringBuilder Step = new StringBuilder();
        void OnCollisionEnter(Collision c) => Note("enter", c);
        void OnCollisionStay(Collision c) => Note("stay", c);
        void Note(string kind, Collision c)
        {
            if (c.impulse.sqrMagnitude < 1e-4f && kind == "stay") return;
            var n = c.contactCount > 0 ? c.GetContact(0).normal : Vector3.zero;
            var p = c.contactCount > 0 ? c.GetContact(0).point : Vector3.zero;
            Step.Append($"{kind}:{c.collider.name}:n({n.x:0.00},{n.y:0.00},{n.z:0.00}):p({p.x:0.0},{p.y:0.00},{p.z:0.0}):J{c.impulse.magnitude:0.0} ");
        }
        public string Take() { var s = Step.ToString(); Step.Clear(); return s; }
    }
}
