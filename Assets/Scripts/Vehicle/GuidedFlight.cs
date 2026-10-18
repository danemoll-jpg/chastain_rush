using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 40 (A1 / A2, A7): an aimed flight to a landing, from wherever the vehicle is when it starts. The manhole plume on New Chastain
    // Road throws a vehicle from anywhere across its carriageway onto the forward pile's ramp face (a straight flight); the church leap's
    // window sends one curving round from heading south onto New Chastain Road heading west. In plan the vehicle follows a curve (a cubic
    // Hermite from its start, along its start direction, to the landing, along the landing direction; a straight line when the two agree)
    // at a steady speed along it; up and down it flies as anything thrown does (gravity only), launched at 'angle' with the speed that
    // comes down on the landing: u = sqrt(g L^2 / (2 (L tan(angle) - dy))) along the curve's length L. Arcade: its heading turns with the
    // curve, its speed along the curve is held (no drag, no air steering off the line), 'gravity' times as heavy as anything else (1: as
    // everything; the church leap 1.6, a quicker flight on the same curve), and a vehicle that hits something on the way, or
    // is reset, is let go at once (it flies on as it was). Only one flight at a time per vehicle.
    public sealed class GuidedFlight : MonoBehaviour
    {
        public static int Flights { get; private set; }
        public static event System.Action<ArcadeVehicle, string, float, float> Began;   // the vehicle, who sent it, speed along the curve, flight time planned
        public static event System.Action<ArcadeVehicle, string, Vector3, float> Ended;  // the vehicle, who sent it, where it was let go, seconds flown
        public string Source { get; private set; }
        public bool Flying => on;
        public float Planned => L / Mathf.Max(u, .1f);
        ArcadeVehicle car; Rigidbody body;
        readonly List<Vector2> pts = new List<Vector2>(); readonly List<float> cum = new List<float>();
        float L, u, t, extra; bool on; Vector3 lastSet; Vector3 lastPos;

        public static GuidedFlight Begin(ArcadeVehicle car, string source, float surfaceY, Vector3 target, Vector3 startDir, Vector3 endDir, float angleDeg, float gravity = 1f)
        {
            if (!car || car.Body == null) return null;
            var f = car.GetComponent<GuidedFlight>() ?? car.gameObject.AddComponent<GuidedFlight>();
            f.Plan(car, source, surfaceY, target, startDir, endDir, angleDeg, gravity);
            return f;
        }

        void Plan(ArcadeVehicle c, string source, float surfaceY, Vector3 target, Vector3 startDir, Vector3 endDir, float angleDeg, float gravity)
        {
            car = c; body = c.Body; Source = source;
            var p0 = new Vector2(body.position.x, body.position.z); var p1 = new Vector2(target.x, target.z);
            var d0 = new Vector2(startDir.x, startDir.z).normalized; var d1 = new Vector2(endDir.x, endDir.z).normalized;
            float chord = Vector2.Distance(p0, p1);
            pts.Clear(); cum.Clear();
            const int N = 120;
            for (int k = 0; k <= N; k++)
            {
                float s = k / (float)N, h00 = 2 * s * s * s - 3 * s * s + 1, h10 = s * s * s - 2 * s * s + s, h01 = -2 * s * s * s + 3 * s * s, h11 = s * s * s - s * s;
                pts.Add(h00 * p0 + h10 * chord * d0 + h01 * p1 + h11 * chord * d1);
                cum.Add(k == 0 ? 0f : cum[k - 1] + Vector2.Distance(pts[k - 1], pts[k]));
            }
            L = cum[N];
            extra = Mathf.Max(0f, gravity - 1f);
            float th = angleDeg * Mathf.Deg2Rad, dy = target.y - surfaceY, g = -Physics.gravity.y * Mathf.Max(1f, gravity);
            u = Mathf.Sqrt(g * L * L / Mathf.Max(1f, 2f * (L * Mathf.Tan(th) - dy)));
            var dir = Tangent(0f);
            body.linearVelocity = new Vector3(dir.x, 0f, dir.y) * u + Vector3.up * (u * Mathf.Tan(th));
            body.angularVelocity = Vector3.zero;
            Face(dir, 1f);
            lastSet = body.linearVelocity; lastPos = body.position; t = 0f; on = true; enabled = true;
            Flights++; Began?.Invoke(car, source, u, L / u);
        }

        Vector2 At(float s, out int i)
        {
            s = Mathf.Clamp(s, 0f, L); i = 0;
            while (i + 2 < cum.Count && cum[i + 1] < s) i++;
            return Vector2.Lerp(pts[i], pts[i + 1], Mathf.InverseLerp(cum[i], cum[i + 1], s));
        }
        Vector2 Tangent(float s) { At(Mathf.Min(s, L - .5f), out int i); var d = pts[i + 1] - pts[i]; return d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.up; }

        // the heading turned toward the curve's (about the world's up; pitch and roll left to the vehicle's own levelling in the air)
        void Face(Vector2 dir, float k)
        {
            var fwd = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, Vector3.up);
            if (fwd.sqrMagnitude < 1e-4f) return;
            float a = Vector3.SignedAngle(fwd.normalized, new Vector3(dir.x, 0f, dir.y), Vector3.up) * k;
            body.MoveRotation(Quaternion.AngleAxis(a, Vector3.up) * body.rotation);
            var w = body.angularVelocity; w.y = 0f; body.angularVelocity = w;
        }

        public static string LastEnd { get; private set; } = "";   // checks: why the last flight was let go (and what it touched)
        string touched = "";
        void OnCollisionEnter(Collision c) { if (on) touched = c.collider ? c.collider.name : "?"; }
        void Stop(string why) { if (!on) return; on = false; enabled = false; LastEnd = why + (touched.Length > 0 ? " (touched " + touched + ")" : "") + $" after {t:0.00} s at ({body.position.x:0}, {body.position.y:0}, {body.position.z:0})"; touched = ""; Ended?.Invoke(car, Source, body.position, t); }

        void FixedUpdate()
        {
            if (!on || body == null) { enabled = false; return; }
            float dt = Time.fixedDeltaTime;
            // let go: a reset (a jump in position), a knock (the speed it was given changed by more than a bump would), down on its wheels, or done
            if ((body.position - lastPos).sqrMagnitude > 15f * 15f || body.isKinematic) { Stop("moved"); return; }
            var v = body.linearVelocity;
            var hd = new Vector2(v.x - lastSet.x, v.z - lastSet.z);
            if (t > .1f && hd.magnitude > 5f) { Stop("hit"); return; }
            if (t > .3f && car.GroundedWheels > 0) { Stop("down"); return; }
            t += dt; float s = u * t;
            if (s >= L + 2f || t > L / u + 2.5f) { Stop("end"); return; }
            var want = At(s, out _); var dir = Tangent(s);
            var here = new Vector2(body.position.x, body.position.z);
            var fix = Vector2.ClampMagnitude((want - here) * 2f, 3f);   // back onto the curve gently (it drifts a little each step)
            var h = dir * u + fix;
            body.linearVelocity = new Vector3(h.x, v.y, h.y);
            if (extra > 0f) body.AddForce(Physics.gravity * extra, ForceMode.Acceleration);   // a heavier fall (the church leap: a quicker flight on the same curve)
            Face(dir, Mathf.Clamp01(dt * 6f));
            lastSet = body.linearVelocity; lastPos = body.position;
        }
    }
}
