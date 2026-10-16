using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 23 follow-ups (Dan, 2026-10-12: "Love the idea of pop-out manhole covers"): a pipe of the network ends in a riser that
    // climbs at 50 degrees along a street to a big round iron lid, flush with the road (MapBuilder.Network.cs). Round 25 (A1): the
    // lid is now a hatch of the street itself with a 0.9 m cover drawn on it, and the collider (lidCollider) the terrain's own surface
    // over the hole, on its own object (it does not fly). From the street the
    // lid is solid (a car drives over it). A vehicle coming up the riser hits the trigger just under the lid first: the lid flies
    // off (up, tumbling, a clang) and the vehicle bursts out onto the street. Never a trap: too slow to reach the top, the vehicle
    // rolls back down the riser (or a reset puts it on the pipe's floor). The lid comes back a few seconds later, once nothing is
    // over the hole.
    public sealed class ManholeLid : MonoBehaviour
    {
        public Transform lid;
        public Collider lidCollider;
        public Vector3 outward;          // along the street, the way the riser comes out
        public float radius = 2f;
        public Vector2 slope;            // Round 25 (A1): the street's grade at the hole (dy/dx, dy/dz)
        public static int Bursts { get; private set; }   // checks
        Vector3 home; Quaternion homeRot; Vector3 vel, spin; float flying = -1f, gone = -1f;
        MeshRenderer lidRenderer;

        void Start() { home = lid.position; homeRot = lid.rotation; lidRenderer = lid.GetComponent<MeshRenderer>(); }

        public void Burst(Rigidbody body)
        {
            if (flying >= 0f || gone >= 0f) return;
            if (body.linearVelocity.y < 1.5f) return;   // only a vehicle coming up from below
            // Round 25 (A1): and only from below the street (a car climbing a sloping street over the hole, its body in the trigger's
            // top, burst it, dropped into the riser and was thrown out: the JOLT Dan felt)
            var d = body.position - home; if (body.position.y > home.y + slope.x * d.x + slope.y * d.z + 0.15f) return;
            Bursts++;
            if (lidCollider) lidCollider.enabled = false;
            float up = Mathf.Clamp(body.linearVelocity.y * 1.3f, 8f, 18f);
            vel = Vector3.up * up + outward * (2f + Random.Range(0f, 2f)) + Vector3.Cross(Vector3.up, outward) * Random.Range(-2.5f, 2.5f);
            spin = new Vector3(Random.Range(-540f, 540f), Random.Range(-200f, 200f), Random.Range(-540f, 540f));
            flying = 0f;
            BreakSounds.Play(home, Mathf.Max(8f, body.linearVelocity.magnitude), BreakSounds.Kind.Iron);
        }

        // Round 36 (Part D1): a rigged hydrant knocked over blows the cover off from above (a bang, the water, the cover flying), and the hole stays
        // open `openFor` seconds (the way down the riser into the network), then closes once nothing is over it; a reset closes it at once
        public float openFor = 5f;
        public bool Open => flying >= 0f || gone >= 0f;
        public static int Blown { get; private set; }   // checks
        public void BlowOpen(float seconds)
        {
            if (flying >= 0f || gone >= 0f) return;
            Blown++; openFor = seconds;
            if (lidCollider) lidCollider.enabled = false;
            vel = Vector3.up * Random.Range(13f, 17f) + outward * Random.Range(1f, 3f) + Vector3.Cross(Vector3.up, outward) * Random.Range(-2f, 2f);
            spin = new Vector3(Random.Range(-720f, 720f), Random.Range(-300f, 300f), Random.Range(-720f, 720f));
            flying = 0f;
            BreakSounds.Play(home, 22f, BreakSounds.Kind.Iron);
            Geyser.Spray(home, 2.5f, 9f);
        }
        public void CloseNow() { if (!(flying >= 0f || gone >= 0f)) return; flying = -1f; gone = -1f; lid.SetPositionAndRotation(home, homeRot); if (lidCollider) lidCollider.enabled = true; if (lidRenderer) lidRenderer.enabled = true; openFor = 5f; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (flying >= 0f)
            {
                flying += dt; vel += Physics.gravity * dt; lid.position += vel * dt; lid.rotation = Quaternion.Euler(spin * dt) * lid.rotation;
                if (lid.position.y < home.y - 0.2f && vel.y < 0f) { lid.position = new Vector3(lid.position.x, home.y + 0.05f, lid.position.z); vel = Vector3.zero; spin = Vector3.zero; }
                if (flying > 3.5f) { flying = -1f; gone = 0f; if (lidRenderer) lidRenderer.enabled = false; }
            }
            else if (gone >= 0f)
            {
                gone += dt;
                if (gone > openFor && !Occupied())
                {
                    gone = -1f; openFor = 5f; lid.SetPositionAndRotation(home, homeRot); if (lidCollider) lidCollider.enabled = true; if (lidRenderer) lidRenderer.enabled = true;
                }
            }
        }

        bool Occupied()
        {
            foreach (var c in Physics.OverlapSphere(home, radius + 2.5f, ~0, QueryTriggerInteraction.Ignore))
                if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) return true;
            return false;
        }
    }
}
