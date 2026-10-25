using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 69: the plane's crashes, forgiving as the helicopter's (HelicopterFlight.Crash.cs): in the air the body into anything, a wing tip, the
    // tail or the nose meeting anything solid, a tree's crown (TreeCrowns), a hard landing or a wing / the nose into the ground, or a touch down
    // in water. Then the engine stops, it falls and tumbles; after CrashFor seconds it is put on the nearest open ground (HelicopterFlight.ResetSpot,
    // also the reset button's place), the throttle closed.
    public sealed partial class PlaneFlight
    {
        public const float CrashFor = 2.6f;
        float crashedAt = -1f;
        public bool Crashed => crashedAt >= 0f;
        public string CrashReason { get; private set; } = "";
        public int Crashes { get; private set; }
        // the strike points (Tools/Blender/plane.py): the wing tips, the tail's top and the stabilizer's tips, the nose
        static readonly Vector3[] Strikes = { new(5.5f, 1.2f, .15f), new(-5.5f, 1.2f, .15f), new(2.8f, 1.15f, .15f), new(-2.8f, 1.15f, .15f), new(0, 1.7f, -4.3f), new(1.7f, .33f, -3.9f), new(-1.7f, .33f, -3.9f), new(0, .1f, 3.7f) };
        static readonly Collider[] overlap = new Collider[8];

        void OnCollisionEnter(Collision c)
        {
            if (!enabled || Crashed || !car) return;
            float hit = c.relativeVelocity.magnitude; Vector3 n = c.contactCount > 0 ? c.GetContact(0).normal : Vector3.up;
            if (Airborne) { if (hit > 6f) Crash("hit " + c.collider.name); }
            else if (hit > 22f && n.y < .6f) Crash("hit " + c.collider.name);   // on the ground: only a big hit from the side (it is a car there)
        }
        bool Solid(Collider col) => col && col.attachedRigidbody != car.Body && !col.isTrigger;
        void CheckStrikes(Rigidbody body)
        {
            if (OverWater && HeightAboveGround < .15f) { Crash("touched down in water"); return; }
            foreach (var local in Strikes)
            {
                var p = transform.TransformPoint(local);
                int n = Physics.OverlapSphereNonAlloc(p, .4f, overlap, ~0, QueryTriggerInteraction.Ignore);
                for (int k = 0; k < n; k++) if (Solid(overlap[k])) { Crash((local.x > 1f ? "the right wing" : local.x < -1f ? "the left wing" : local.z > 0f ? "the nose" : "the tail") + " hit " + overlap[k].name); return; }
            }
            // the trees' crowns (no colliders): the wing's span at the wing's height, the body
            var wing = transform.TransformPoint(new Vector3(0f, 1.1f, .15f));
            if (TreeCrowns.Hit(wing, 5.2f, body.position, out string tree)) Crash(tree.Replace("rotor", "a wing"));
        }

        public void Crash(string why)
        {
            if (Crashed) return;
            crashedAt = Time.time; Crashes++; CrashReason = why; Airborne = false; Throttle = 0f;
            var body = car.Body; body.useGravity = true;
            body.angularVelocity += transform.forward * Random.Range(-2.5f, 2.5f) + transform.right * Random.Range(-1.5f, 1.5f);
            BreakSounds.Play(body.position, Mathf.Max(10f, body.linearVelocity.magnitude), BreakSounds.Kind.Iron);
            Debug.Log($"[Plane] crash: {why} at ({body.position.x:0.0}, {body.position.y:0.0}, {body.position.z:0.0}), {body.linearVelocity.magnitude:0.0} m/s");
        }
        void CrashStep(float dt)
        {
            var body = car.Body; body.useGravity = true; body.AddForce(-body.linearVelocity * .5f, ForceMode.Acceleration);
            if (Time.time - crashedAt < CrashFor) return;
            PutOnStrip();
            Recovered();
        }
        // a crash's or the reset button's place: the nearest strip it can take off from again (StripSpot), else lined up on the runway (the first
        // exe run: the helicopter's spot, a 12 m clearing in the woods, left it unable to go anywhere)
        public void PutOnStrip()
        {
            var body = car.Body; var respawn = GetComponent<VehicleRespawn>();
            if (StripSpot(body.position, out var at, out float yaw)) { if (respawn) respawn.PlaceAt(at, yaw); else { body.position = at; body.rotation = Quaternion.Euler(0, yaw, 0); } }
            else LineUp(Vector2.Distance(new Vector2(body.position.x, body.position.z), RunwayWest) < Vector2.Distance(new Vector2(body.position.x, body.position.z), RunwayEast));
        }
        // the nearest open ground with a straight, flat, clear run of StripLength ahead (a field, a lot, a straight road), within 1.5 km: in rings out
        // from p, each point tried in eight headings; the run's ground within 2.5 m of level with its start, no collider over it but the ground, no
        // tree within reach of a wing
        public const float StripLength = 130f;
        public static bool StripSpot(Vector3 p, out Vector3 at, out float yaw)
        {
            at = p; yaw = 0f;
            for (float r = 0; r <= 1500f; r += r < 60f ? 15f : 40f)
            {
                int steps = r == 0 ? 1 : Mathf.Max(8, Mathf.CeilToInt(2 * Mathf.PI * r / 40f));
                for (int i = 0; i < steps; i++)
                {
                    float a = i * 2 * Mathf.PI / steps; var q = new Vector3(p.x + Mathf.Cos(a) * r, 0, p.z + Mathf.Sin(a) * r);
                    if (!HelicopterFlight.GroundUnder(new Vector3(q.x, 2500f, q.z), out var hit) || hit.normal.y < .97f || ShallowWater.InWater(hit.point + Vector3.up * .2f, .5f)) continue;
                    if (!TreeCrowns.Clear(hit.point)) continue;
                    for (int h = 0; h < 8; h++)
                    {
                        float y = h * 45f; var f = Quaternion.Euler(0, y, 0) * Vector3.forward; bool ok = true;
                        for (float d = 15f; d <= StripLength && ok; d += 15f)
                        {
                            var s = hit.point + f * d;
                            if (!HelicopterFlight.GroundUnder(new Vector3(s.x, 2500f, s.z), out var sh) || Mathf.Abs(sh.point.y - hit.point.y) > 2.5f || sh.normal.y < .95f || ShallowWater.InWater(sh.point + Vector3.up * .2f, .5f) || !TreeCrowns.Clear(sh.point)) ok = false;
                        }
                        if (!ok) continue;
                        var mid = hit.point + f * (StripLength * .5f) + Vector3.up * 3.2f;
                        foreach (var c in Physics.OverlapBox(mid, new Vector3(6.5f, 2.5f, StripLength * .5f + 4f), Quaternion.Euler(0, y, 0), ~0, QueryTriggerInteraction.Ignore))
                            if (!(c is TerrainCollider) && c.attachedRigidbody == null && !c.GetComponentInParent<PlaneFlight>()) { ok = false; break; }
                        if (!ok) continue;
                        at = hit.point + Vector3.up * (WheelDrop + .1f); yaw = y; return true;
                    }
                }
            }
            return false;
        }

        // after a reset (the crash's or the reset button's): on its wheels, the throttle closed
        public void Recovered()
        {
            crashedAt = -1f; Airborne = false; Throttle = 0f; Airspeed = 0f; yawDeg = transform.eulerAngles.y;
            car.Body.useGravity = true; car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero;
        }
    }
}
