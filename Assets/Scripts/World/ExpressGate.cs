using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 80 (E): one Express Lanes gate (ExpressLanes), as Dan's photo: a red and white striped swing arm with red lamps along its top and a red DO NOT
    // ENTER banner hung under it, swung across the ramp when the ramp is closed and back along its side when it is open (30 degrees a second). The arm is
    // a trigger: a vehicle that hits it breaks it (no speed lost: fun first); it comes back when no vehicle has been within 40 m of it for 8 s.
    public sealed class ExpressGate : MonoBehaviour
    {
        public Transform hinge;          // turns about the vertical: local yaw 0 = across the ramp (closed), openYaw = swung back
        public float openYaw = -88f;
        public GameObject arm;           // the arm, its lamps and the banner (hidden while broken), with the trigger
        public bool usableSouth, usableNorth;   // the ramp is used while the lanes run that way
        public int index;
        public float Yaw { get; private set; }
        public bool Broken { get; private set; }
        public bool IsOpen => Yaw < openYaw * .5f;
        bool wantOpen; float quietSince;
        static readonly Color[] Bits = { new Color(.85f, .1f, .1f), new Color(.95f, .95f, .93f) };

        public void Want(bool open) => wantOpen = open;
        public void Snap() { Yaw = wantOpen ? openYaw : 0f; if (hinge) hinge.localRotation = Quaternion.Euler(0f, Yaw, 0f); }

        void Update()
        {
            Yaw = Mathf.MoveTowards(Yaw, wantOpen ? openYaw : 0f, 30f * Time.deltaTime);
            if (hinge) hinge.localRotation = Quaternion.Euler(0f, Yaw, 0f);
            if (!Broken) return;
            if (WorldClock.Replaying) return;   // a replay puts it back itself (ExpressLanes.RestoreAll at a seek)
            bool near = false; var at = transform.position;
            foreach (var v in ArcadeVehicle.Live) if (v && v.isActiveAndEnabled && (v.transform.position - at).sqrMagnitude < 40f * 40f) { near = true; break; }
            if (near) quietSince = Time.time; else if (Time.time - quietSince > 8f) Restore();
        }

        public void Hit(Collider c)   // ExpressGateArm: its trigger
        {
            if (Broken || WorldClock.Replaying) return;
            var body = c.attachedRigidbody; if (!body || body.isKinematic) return;
            if (!body.GetComponent<ArcadeVehicle>() && !body.GetComponentInParent<ArcadeVehicle>()) return;
            if (body.linearVelocity.magnitude < 1f) return;
            Break(body.linearVelocity, true, true);
        }

        public void Break(Vector3 vel, bool show, bool record)
        {
            if (Broken) return;
            Broken = true; quietSince = Time.time;
            if (arm) arm.SetActive(false);
            if (show)
            {
                // the arm in pieces, thrown along the hit
                var a = hinge ? hinge.position : transform.position; var along = hinge ? hinge.right : transform.right;
                if (vel.sqrMagnitude < 1f) vel = -transform.forward * 12f;
                for (int i = 0; i < 6; i++)
                {
                    var p = GameObject.CreatePrimitive(PrimitiveType.Cube); p.name = "Express gate piece";
                    p.transform.localScale = new Vector3(.9f, .12f, .12f);
                    p.transform.SetPositionAndRotation(a + along * (.6f + i * .9f) + Vector3.up * 1.05f, Quaternion.LookRotation(Vector3.Cross(along, Vector3.up), Vector3.up));
                    var r = p.GetComponent<Renderer>(); r.material.color = Bits[i % 2]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var rb = p.AddComponent<Rigidbody>(); rb.mass = 6f;
                    rb.linearVelocity = vel * Random.Range(.35f, .7f) + Vector3.up * Random.Range(2f, 5f) + Random.insideUnitSphere * 2f;
                    rb.angularVelocity = Random.insideUnitSphere * 8f;
                    Physics.IgnoreCollision(p.GetComponent<Collider>(), FindPlayerCollider() ?? p.GetComponent<Collider>(), true);
                    Destroy(p, 6f);
                }
                BreakSounds.Play(a, vel.magnitude, BreakSounds.Kind.Sign);
            }
            if (record) ExpressLanes.Broke(this);
        }
        static Collider FindPlayerCollider() { var b = Traffic.Instance ? Traffic.Instance.PlayerBody : null; return b ? b.GetComponent<Collider>() : null; }

        public void Restore() { if (!Broken) return; Broken = false; if (arm) arm.SetActive(true); }
    }
}
