using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 40 (A7, the church leap): the two bends of the climb inside church 74 (round the altar, and onto the gallery under the steeple) hold a
    // vehicle to their curve, as a banked track would (the stunt park's loop's way): on the deck within the bend, going its way, the vehicle's
    // speed over the ground is turned along the bend (no faster than 'maxSpeed'), eased back toward the lane's middle, its heading with it. Arcade:
    // the bends' 3.6 m radius would otherwise take a crawl, and the leap is to be worth its trouble. A vehicle going the other way is left alone.
    public sealed class ChurchTurn : MonoBehaviour
    {
        public Vector3 center;                 // the bend's middle at the floor
        public float radius = 3.6f, halfLane = 2.3f, a0, a1, yLow, yHigh, maxSpeed = 15f;   // its arc (degrees, as sin a, cos a round the middle), the deck's heights over the floor there
        public static int Held { get; private set; }
        static readonly System.Collections.Generic.List<ChurchTurn> all = new System.Collections.Generic.List<ChurchTurn>();
        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }
        // in plan, on one of the held bends: the drivers' bend rule (no faster than the grip round the bend allows) does not apply there
        public static bool HeldAt(Vector3 p)
        {
            foreach (var b in all)
            {
                var d = new Vector2(p.x - b.center.x, p.z - b.center.z); if (Mathf.Abs(d.magnitude - b.radius) > b.halfLane) continue;
                float a = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; if (a < 0f) a += 360f; if (b.a1 >= 360f && a < b.a0 - 1f) a += 360f;
                if (a >= b.a0 - 15f && a <= b.a1 + 15f) return true;
            }
            return false;
        }

        void FixedUpdate()
        {
            foreach (var car in DrainPipe.Vehicles)
            {
                if (!car || car.Body == null || car.Body.isKinematic || car.GroundedWheels < 2) continue;
                var p = car.Body.position; var d = new Vector2(p.x - center.x, p.z - center.z); float r = d.magnitude;
                if (r < .1f || Mathf.Abs(r - radius) > halfLane + .3f) continue;
                float y = p.y - center.y; if (y < yLow - .3f || y > yHigh + 2.5f) continue;
                float a = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; if (a < 0f) a += 360f; if (a1 >= 360f && a < a0 - 1f) a += 360f;
                if (a < a0 - 4f || a > a1 + 4f) continue;
                var dn = d / r; var t = new Vector2(dn.y, -dn.x);   // the way round (the arc's angle growing)
                var v = car.Body.linearVelocity; var vh = new Vector2(v.x, v.z);
                if (Vector2.Dot(vh, t) < 1f) continue;
                float s = Mathf.Min(vh.magnitude, maxSpeed);
                var w = t * s + dn * Mathf.Clamp((radius - r) * 2f, -2f, 2f);
                car.Body.linearVelocity = new Vector3(w.x, v.y, w.y);
                var av = car.Body.angularVelocity; av.y = s / r; car.Body.angularVelocity = av;   // turning with the bend (clockwise from above)
                var fwd = Vector3.ProjectOnPlane(car.Body.rotation * Vector3.forward, Vector3.up);
                if (fwd.sqrMagnitude > 1e-4f)
                {
                    float turn = Vector3.SignedAngle(fwd.normalized, new Vector3(t.x, 0f, t.y), Vector3.up);
                    car.Body.MoveRotation(Quaternion.AngleAxis(turn * .35f, Vector3.up) * car.Body.rotation);
                }
                Held++;
            }
        }
    }
}
