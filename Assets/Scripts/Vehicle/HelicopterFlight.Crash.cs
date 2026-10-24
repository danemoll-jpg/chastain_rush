using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 63 (Dan: "rotor or body hitting trees, buildings, poles: a crash that is forgiving (spin down and reset after a moment, onto the nearest
    // open spot), never stuck"). What crashes it: the body hitting anything at speed (OnCollisionEnter), the main rotor's blades meeting anything solid
    // (points round the disc), or a tree's crown (the trees have no colliders above the trunk: TreeCrowns, from TreeField's own data), the tail rotor,
    // or setting down in water. Then: no lift, the rotor winds down, it falls and tumbles; after CrashFor seconds it is put on the nearest open ground
    // (ResetSpot, also the reset button's place for the helicopter), upright, rotor spooling up again.
    public sealed partial class HelicopterFlight
    {
        public const float CrashFor = 2.6f, RotorRadius = 5.2f;
        float crashedAt = -1;
        public string CrashReason { get; private set; } = "";
        public int Crashes { get; private set; }
        static readonly Vector3 HubLocal = new Vector3(0, 1.56f, -.30f), TailHubLocal = new Vector3(-.24f, 1.28f, -6.38f);
        static readonly Collider[] overlap = new Collider[8];

        void OnCollisionEnter(Collision c)
        {
            if (!enabled || Crashed || !car) return;
            float hit = c.relativeVelocity.magnitude; Vector3 n = c.contactCount > 0 ? c.GetContact(0).normal : Vector3.up;
            // a touch down (a roof, a lot, the ground) is not a crash unless it is hard; anything from the side at speed is
            bool fromBelow = n.y > .7f;
            if (fromBelow ? hit > 11f : hit > 6.5f) Crash((fromBelow ? "hard landing on " : "hit ") + c.collider.name);
        }

        bool Solid(Collider col) => col && col.attachedRigidbody != car.Body && !col.isTrigger;
        void CheckStrikes(Rigidbody body)
        {
            if (Spool < .5f) return;
            if (OverWater && HeightAboveGround < .15f) { Crash("set down in water"); return; }
            if (Landed) return;
            var hub = transform.TransformPoint(HubLocal);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f; var dir = transform.rotation * new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                foreach (float r in new[] { .55f, .92f })
                {
                    int n = Physics.OverlapSphereNonAlloc(hub + dir * (RotorRadius * r), .35f, overlap, ~0, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < n; k++) if (Solid(overlap[k])) { Crash("rotor hit " + overlap[k].name); return; }
                }
            }
            var tail = transform.TransformPoint(TailHubLocal);
            int m = Physics.OverlapSphereNonAlloc(tail, .8f, overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < m; k++) if (Solid(overlap[k])) { Crash("tail rotor hit " + overlap[k].name); return; }
            if (TreeCrowns.Hit(hub, RotorRadius * .85f, body.position, out string tree)) Crash(tree);
        }

        public void Crash(string why)
        {
            if (Crashed) return;
            crashedAt = Time.time; Crashes++; CrashReason = why; Landed = false; Boosting = false;
            var body = car.Body; body.angularVelocity += transform.up * Random.Range(-2.5f, 2.5f) + transform.right * Random.Range(-1f, 1f);
            BreakSounds.Play(body.position, Mathf.Max(10f, body.linearVelocity.magnitude), BreakSounds.Kind.Iron);
            Debug.Log($"[Heli] crash: {why} at ({body.position.x:0.0}, {body.position.y:0.0}, {body.position.z:0.0}), {body.linearVelocity.magnitude:0.0} m/s");
        }

        void CrashStep(float dt)
        {
            Spool = Mathf.MoveTowards(Spool, 0f, dt / 1.6f);
            var body = car.Body; body.AddForce(-body.linearVelocity * .4f, ForceMode.Acceleration);   // a little air drag on the way down
            if (Time.time - crashedAt < CrashFor) return;
            var respawn = GetComponent<VehicleRespawn>();
            if (ResetSpot(body.position, out var at, out float yaw))
            { if (respawn) respawn.PlaceAt(at, yaw); else { body.position = at; body.rotation = Quaternion.Euler(0, yaw, 0); } }
            else if (respawn) respawn.PlaceAt(respawn.SafePosition, respawn.SafeYaw);
            Recovered();
        }
        // after a reset (the crash's or the reset button's): upright, on the ground, the rotor spooling up from half
        public void Recovered()
        {
            crashedAt = -1; Landed = true; Spool = Mathf.Min(Spool, .5f); yawDeg = transform.eulerAngles.y; holdY = car.Body.position.y;
            car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero;
        }

        // the nearest open ground to p: rings outward to 600 m; the ground under each point (the first thing under it: a roof, a lot, the
        // terrain), not water, not steep, the helicopter's box and its rotor's sweep clear of everything but the ground itself
        public static bool ResetSpot(Vector3 p, out Vector3 at, out float yaw)
        {
            yaw = 0; at = p;
            for (float r = 0; r <= 600f; r += r < 30f ? 6f : 15f)
            {
                int steps = r == 0 ? 1 : Mathf.Max(8, Mathf.CeilToInt(2 * Mathf.PI * r / 12f));
                for (int i = 0; i < steps; i++)
                {
                    float a = i * 2 * Mathf.PI / steps; var q = new Vector3(p.x + Mathf.Cos(a) * r, 0, p.z + Mathf.Sin(a) * r);
                    if (!GroundUnder(new Vector3(q.x, 2500f, q.z), out var hit)) continue;
                    if (hit.normal.y < .94f || ShallowWater.InWater(hit.point + Vector3.up * .2f, .5f) || DrainPipe.Inside(hit.point, out _) != null) continue;
                    var ground = hit.collider;
                    bool clear = true;
                    foreach (var c in Physics.OverlapBox(hit.point + Vector3.up * 2.6f, new Vector3(6f, 2.2f, 6f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                        if (c != ground && !(c is TerrainCollider) && !c.GetComponentInParent<HelicopterFlight>()) { clear = false; break; }
                    if (!clear || !TreeCrowns.Clear(hit.point)) continue;   // no tree within the rotor's or the tail's reach, at any height (it climbs out)
                    at = hit.point + Vector3.up * (SkidDrop + .05f); yaw = a * Mathf.Rad2Deg; return true;
                }
            }
            return false;
        }
        // the first lasting ground under a point (the terrain, a roof, a deck, a lot): not a helicopter, a vehicle, or a pooled
        // tree trunk (TreeTrunks moves its colliders round the vehicles: a spot on one was left in the air once it moved)
        public static bool GroundUnder(Vector3 from, out RaycastHit hit)
        {
            for (int i = 0; i < 8; i++)
            {
                if (!Physics.Raycast(from, Vector3.down, out hit, 5000f, ~0, QueryTriggerInteraction.Ignore)) return false;
                var c = hit.collider; if (!c.attachedRigidbody && !TreeTrunks.IsTrunk(c) && !c.GetComponentInParent<HelicopterFlight>()) return true;
                from = hit.point + Vector3.down * .05f; if (c.GetComponentInParent<HelicopterFlight>()) from.y -= 6f;
            }
            hit = default; return false;
        }
    }

    // Round 63: the trees' crowns for the helicopter (TreeField draws them without colliders; TreeTrunks gives only the nearest trunks one): every
    // tree sorted once into 32 m cells, then a rotor disc or the body tested against the crowns near it (TreeField's own sizes: the crown from half
    // way up the trunk, crownH tall, radius wide). Breakable trees that have snapped still count (a rare miss on the side of a crash).
    public static class TreeCrowns
    {
        const float Cell = 32f;
        static TreeField field; static int[] start, order; static float x0, z0; static int cols, rows;
        static void Build()
        {
            field = Object.FindAnyObjectByType<TreeField>(); if (!field || field.data == null || field.count == 0) { field = null; return; }
            float x1 = float.MinValue, z1 = float.MinValue; x0 = z0 = float.MaxValue;
            for (int i = 0; i < field.count; i++) { var t = TreeField.Unpack(field.data, i); x0 = Mathf.Min(x0, t.x); z0 = Mathf.Min(z0, t.z); x1 = Mathf.Max(x1, t.x); z1 = Mathf.Max(z1, t.z); }
            cols = Mathf.FloorToInt((x1 - x0) / Cell) + 1; rows = Mathf.FloorToInt((z1 - z0) / Cell) + 1;
            var count = new int[cols * rows + 1]; var cellOf = new int[field.count];
            for (int i = 0; i < field.count; i++) { var t = TreeField.Unpack(field.data, i); int c = Mathf.FloorToInt((t.z - z0) / Cell) * cols + Mathf.FloorToInt((t.x - x0) / Cell); cellOf[i] = c; count[c + 1]++; }
            for (int c = 0; c < cols * rows; c++) count[c + 1] += count[c];
            start = (int[])count.Clone(); order = new int[field.count]; var fill = (int[])count.Clone();
            for (int i = 0; i < field.count; i++) order[fill[cellOf[i]]++] = i;
        }
        // a spot the helicopter can sit on and climb straight up from: no trunk within the tail's reach (7.5 m) and no crown the rotor's disc would
        // meet on the way up, whatever its height
        public static bool Clear(Vector3 p)
        {
            if (field == null || start == null) { Build(); if (field == null) return true; }
            float reach = HelicopterFlight.RotorRadius + 14f;
            int cx0 = Mathf.Max(0, Mathf.FloorToInt((p.x - reach - x0) / Cell)), cx1 = Mathf.Min(cols - 1, Mathf.FloorToInt((p.x + reach - x0) / Cell));
            int cz0 = Mathf.Max(0, Mathf.FloorToInt((p.z - reach - z0) / Cell)), cz1 = Mathf.Min(rows - 1, Mathf.FloorToInt((p.z + reach - z0) / Cell));
            for (int cz = cz0; cz <= cz1; cz++)
                for (int cx = cx0; cx <= cx1; cx++)
                    for (int k = start[cz * cols + cx]; k < start[cz * cols + cx + 1]; k++)
                    {
                        var t = TreeField.Unpack(field.data, order[k]); float d = new Vector2(t.x - p.x, t.z - p.z).magnitude;
                        if (d < Mathf.Max(7.5f, HelicopterFlight.RotorRadius * .85f + t.radius * .05f * .55f + .8f)) return false;
                    }
            return true;
        }
        // a disc (centre hub, radius r, flat) or the body (a point) inside a crown
        public static bool Hit(Vector3 hub, float r, Vector3 bodyAt, out string what)
        {
            what = null;
            if (field == null || start == null) { Build(); if (field == null) return false; }
            float reach = r + 14f;
            int cx0 = Mathf.Max(0, Mathf.FloorToInt((hub.x - reach - x0) / Cell)), cx1 = Mathf.Min(cols - 1, Mathf.FloorToInt((hub.x + reach - x0) / Cell));
            int cz0 = Mathf.Max(0, Mathf.FloorToInt((hub.z - reach - z0) / Cell)), cz1 = Mathf.Min(rows - 1, Mathf.FloorToInt((hub.z + reach - z0) / Cell));
            for (int cz = cz0; cz <= cz1; cz++)
                for (int cx = cx0; cx <= cx1; cx++)
                {
                    int c = cz * cols + cx;
                    for (int k = start[c]; k < start[c + 1]; k++)
                    {
                        var t = TreeField.Unpack(field.data, order[k]);
                        float radius = t.radius * .05f, crownH = t.crownH * .01f, trunkH = t.trunkH * .1f;
                        float bottom = t.y + trunkH * .5f + crownH * .15f, top = t.y + trunkH * .5f + crownH * .9f;
                        float dHub = new Vector2(t.x - hub.x, t.z - hub.z).magnitude;
                        if (hub.y > bottom && hub.y < top && dHub < r + radius * .55f) { what = "rotor hit a tree"; return true; }
                        float dBody = new Vector2(t.x - bodyAt.x, t.z - bodyAt.z).magnitude;
                        if (bodyAt.y > bottom - 1f && bodyAt.y < top && dBody < radius * .7f + 1f) { what = "flew into a tree"; return true; }
                    }
                }
            return false;
        }
    }
}
