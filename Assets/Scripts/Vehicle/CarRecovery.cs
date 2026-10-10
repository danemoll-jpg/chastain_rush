using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 10, item 7: where a reset (R / Y) puts the car, now that trees are solid. Racer's rule (VehicleRespawn.RecoverNearest,
    // "the nearest usable track point, facing the race direction", each candidate checked clear of every collider, so never
    // into vegetation), re-written for this game:
    //   in a race   the nearest point of the lap to the car, facing the race direction; then 3 m steps back and forth along
    //               the lap and 2 to 3.5 m to either side until the car's box there touches nothing but the ground
    //   otherwise   the last stable spot as before, unless a tree or anything else stands there or within 6 m ahead of it;
    //               then the nearest road, facing along it the way the car was heading, with the same clear check
    //   (Round 15) never a spot in a pond, lake, stream or the pool: the safe spot is not recorded there and Clear rejects it
    public static class CarRecovery
    {
        static readonly Collider[] hits = new Collider[16];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install() => VehicleRespawn.Recover = Find;

        public static bool Find(VehicleRespawn r, out Vector3 pos, out float yaw)
        {
            var at = r.transform.position;
            if (TreeTrunks.Instance) TreeTrunks.Instance.Refresh(at);
            var race = RaceController.Instance;
            if (race != null && race.Route != null && (race.State == RaceController.Stage.Racing || race.State == RaceController.Stage.Countdown))
            {
                var route = race.Route;
                float s = route.LapStationOf(at);
                foreach (float ds in new[] { 0f, -3f, 3f, -6f, 6f, -10f, 10f, -15f })
                    foreach (float off in new[] { 0f, -2f, 2f, -3.5f, 3.5f })
                    {
                        var p = route.PointAt(route.GateS + s + ds, out var f);
                        var right = Vector3.Cross(Vector3.up, f);
                        var q = RaceRoute.Ground(p.x + right.x * off, p.z + right.z * off);
                        float y = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                        if (Clear(r, q, y, false)) { pos = q; yaw = y; return true; }
                    }
            }
            else
            {
                if (Clear(r, r.SafePosition, r.SafeYaw, true)) { pos = r.SafePosition; yaw = r.SafeYaw; return true; }
                var road = RoadLookup.Instance;
                if (road != null && road.NearestPoint(at, out var c, out var t))
                {
                    var fwd = Quaternion.Euler(0f, r.transform.eulerAngles.y, 0f) * Vector3.forward;
                    var dir = new Vector3(t.x, 0f, t.y); if (Vector3.Dot(dir, fwd) < 0f) dir = -dir;
                    var right = Vector3.Cross(Vector3.up, dir);
                    float y = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    foreach (float ds in new[] { 0f, 4f, -4f, 8f, -8f })
                        foreach (float off in new[] { 0f, -2f, 2f })
                        {
                            var q = RaceRoute.Ground(c.x + dir.x * ds + right.x * off, c.y + dir.z * ds + right.z * off);
                            if (Clear(r, q, y, true)) { pos = q; yaw = y; return true; }
                        }
                }
            }
            pos = default; yaw = 0f;
            return false;   // VehicleRespawn falls back to the last stable spot
        }

        // nothing but the ground (and triggers) in the car's box there, and (ahead) no trunk or wall within 6 m
        static bool Clear(VehicleRespawn r, Vector3 q, float yawDeg, bool ahead)
        {
            if (ShallowWater.InWater(q + Vector3.up * 0.1f)) return false;   // Round 15: never back into the water
            var rot = Quaternion.Euler(0f, yawDeg, 0f);
            var own = r.GetComponent<Rigidbody>();
            int n = Physics.OverlapBoxNonAlloc(q + Vector3.up * 1.0f, new Vector3(1.3f, 0.7f, 2.4f), hits, rot, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h is TerrainCollider || h.attachedRigidbody == own) continue;
                return false;
            }
            if (ahead && Physics.SphereCast(q + Vector3.up * 0.8f, 0.9f, rot * Vector3.forward, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore) && !(hit.collider is TerrainCollider)) return false;
            return true;
        }
    }
}
