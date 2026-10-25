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
            // Round 63: the helicopter goes to the nearest open ground under it, upright, rotor spooling up
            var heli = r.GetComponent<HelicopterFlight>();
            if (heli && heli.enabled) { bool ok = HelicopterFlight.ResetSpot(at, out pos, out yaw); if (ok) pos.y -= .5f; heli.Recovered(); if (ok) return true; }
            // Round 69: the plane the same way, on its wheels, the throttle closed
            var plane = r.GetComponent<PlaneFlight>();
            if (plane && plane.enabled)
            {
                plane.Recovered();
                if (PlaneFlight.StripSpot(at, out pos, out yaw)) return true;
                plane.LineUp(true); pos = plane.transform.position; yaw = plane.transform.eulerAngles.y; return true;
            }
            if (TreeTrunks.Instance) TreeTrunks.Instance.Refresh(at);
            var race = RaceController.Instance;
            bool racing = race != null && race.Route != null && (race.State == RaceController.Stage.Racing || race.State == RaceController.Stage.Countdown);
            // Round 28 (B4, Dan: "Reset button during the race leaves me turned in the wrong direction. It should be following the rule to
            // reset straight forward"): in the underground race the pipes' own reset (below) faced the vehicle the way it was pointing (it
            // only knew the race's way on a shortcut's line, never on the lap) and the hub's ledges and junctions are pipes to it. Now: the
            // nearest point of the race's line (the lap or a shortcut's, height counting: the lap passes over itself), a few meters back
            // or on, on its own height, facing the way the race goes, upright
            if (racing && race.Route.Underground && UndergroundSpot(r, race.Route, at, out pos, out yaw)) return true;
            // Round 18 (item 4): inside a drainage pipe or its cuts, back on the pipe's floor, facing along it: the way the race
            // goes through it (a shortcut's line there), else the way the vehicle was heading
            if (DrainPipe.Inside(at, out _) != null)
            {
                var raceFwd = Vector3.zero;
                if (racing)
                    foreach (var sc in race.Route.Shortcuts)
                        for (int i = 0; i + 1 < sc.Path.Length; i++)
                        {
                            var a = sc.Path[i]; var b = sc.Path[i + 1];
                            var ab = new Vector2(b.x - a.x, b.z - a.z); var q = new Vector2(at.x - a.x, at.z - a.z);
                            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q, ab) / ab.sqrMagnitude);
                            if ((q - ab * t).magnitude < 4f) { raceFwd = new Vector3(ab.x, 0f, ab.y); break; }
                        }
                if (DrainPipe.Recover(at, r.transform.forward, raceFwd, r.GetComponent<Rigidbody>(), out pos, out yaw)) return true;
            }
            // Round 36 (Racer 0.106 Part A, 0.107 Part C): inside the stunt park (its floor, the stores, the roofs) back onto its floor at the
            // nearest clear spot, facing as the map build set it; in the Stunt event any reset is back into the park
            var vehicle = r.GetComponent<ArcadeVehicle>();
            if (!racing && StuntPark.Current && (StuntEvent.InEvent(vehicle) || StuntPark.Inside(at, 6f)))
                foreach (var (spot, facing) in StuntPark.ResetsNear(at))
                    if (Clear(r, spot, facing.eulerAngles.y, false)) { pos = spot; yaw = facing.eulerAngles.y; return true; }
            if (racing)
            {
                var route = race.Route;
                float s = route.LapStationOf(at);
                // Round 33 (the Ridge leap is part of the lap): the lap's line crosses the hollow in the air; a reset anywhere from the
                // lip to the landing puts the vehicle on New Chastain Road past the landing, facing on (never in the hollow under the flight)
                // Round 39: and in the reverse Chastain Ridge, from the climb's foot to its landing: on Chastain Ridge Drive past the landing
                var skips = route.Data == null ? null : route.OwnReverse ? route.Data.reset_skip_rev : !route.Reverse ? route.Data.reset_skip : null;
                float gs = route.Data == null ? 0f : route.OwnReverse ? route.Data.gate_s_rev : route.Data.gate_s;
                // Round 43 (A1 c, Dan BUG-001: "The reset will put you well past the location and say you miss 3 checkpoints"): a skip with a
                // 'short_area' (the reverse Chastain Ridge's plume) goes on past the landing only for a vehicle in the plume's flight; a reset
                // anywhere in that area (New Chastain Road from the checkpoint before the plume to past it, both carriageways, the median, the
                // verges) puts the vehicle short of the cover, facing it, whatever lap point it was nearest
                var flight = r.GetComponent<GuidedFlight>(); bool flying = flight && flight.Flying;
                float s0 = s;
                if (skips != null)
                    foreach (var k in skips)
                    {
                        bool shortArea = k.short_area != null && k.short_area.Length >= 6;
                        if (shortArea && !flying && Inside(k.short_area, at.x, at.z)) { s = Mathf.Repeat(k.short_s - gs, route.LapLength); break; }
                        float a = Mathf.Repeat(k.s0 - gs, route.LapLength), b = Mathf.Repeat(k.s1 - gs, route.LapLength);
                        if (Mathf.Repeat(s - a, route.LapLength) <= Mathf.Repeat(b - a, route.LapLength) && (!shortArea || flying || !Inside(k.short_area, at.x, at.z)))
                        { s = Mathf.Repeat(k.to_s - gs, route.LapLength); break; }
                    }
                foreach (float ds in new[] { 0f, -3f, 3f, -6f, 6f, -10f, 10f, -15f })
                    foreach (float off in new[] { 0f, -2f, 2f, -3.5f, 3.5f })
                    {
                        var p = route.PointAt(route.GateS + s + ds, out var f);
                        var right = Vector3.Cross(Vector3.up, f);
                        var q = RaceRoute.Surface(p.x + right.x * off, p.z + right.z * off);
                        float y = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                        if (Clear(r, q, y, false))
                        {
                            pos = q; yaw = y;
                            // Round 43 (A1 d): the checkpoints this placement itself jumps over (from the lap point the vehicle was nearest to
                            // where it is put) are forgiven, never charged
                            race.ForgiveReset(vehicle, s0, Mathf.Repeat(s + ds, route.LapLength));
                            return true;
                        }
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
                            var q = RaceRoute.Surface(c.x + dir.x * ds + right.x * off, c.y + dir.z * ds + right.z * off);
                            if (Clear(r, q, y, true)) { pos = q; yaw = y; return true; }
                        }
                }
            }
            pos = default; yaw = 0f;
            return false;   // VehicleRespawn falls back to the last stable spot
        }

        static bool Inside(float[] poly, float x, float z)
        {
            bool c = false; int n = poly.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = poly[2 * i], zi = poly[2 * i + 1], xj = poly[2 * j], zj = poly[2 * j + 1];
                if ((zi > z) != (zj > z) && x < (xj - xi) * (z - zi) / (zj - zi) + xi) c = !c;
            }
            return c;
        }

        static bool UndergroundSpot(VehicleRespawn r, RaceRoute route, Vector3 at, out Vector3 pos, out float yaw)
        {
            var p = route.NearestOnRoute(at, out var f, out var sc, out float along);
            Vector3 On(float ds, out Vector3 fw)
            {
                if (sc == null) { var q = route.PointAt(route.GateS + route.LapStationOf(p) + ds, out fw); return q; }
                // along the shortcut's line
                var path = sc.Path; float acc = 0f, want = Mathf.Max(0f, along + ds);
                for (int i = 0; i + 1 < path.Length; i++)
                {
                    var ab = path[i + 1] - path[i]; float L = new Vector2(ab.x, ab.z).magnitude;
                    if (acc + L >= want || i + 2 == path.Length) { fw = new Vector3(ab.x, 0f, ab.z).normalized; return Vector3.Lerp(path[i], path[i + 1], Mathf.Clamp01((want - acc) / Mathf.Max(L, 1e-4f))); }
                    acc += L;
                }
                fw = f; return p;
            }
            float st = sc == null ? route.LapStationOf(p) : 0f, L = route.LapLength;
            foreach (float ds in new[] { 0f, -3f, 3f, -6f, 6f, -10f, 10f })
                foreach (float off in new[] { 0f, -1.2f, 1.2f })
                {
                    if (sc == null && ((st < 15f && st + ds < 1f) || (st > L - 15f && st + ds > L - 1f))) continue;   // never back across the line
                    var q = On(ds, out var fw); if (fw.sqrMagnitude < 0.5f) fw = f;
                    q += Vector3.Cross(Vector3.up, fw).normalized * off;
                    float y = Mathf.Atan2(fw.x, fw.z) * Mathf.Rad2Deg;
                    if (ClearNarrow(r, q, y)) { pos = q; yaw = y; return true; }
                }
            pos = default; yaw = 0f; return false;
        }

        // the underground race's check: a narrower box than Clear's (a 5 m pipe's curved floor rises inside Clear's), from 0.5 m up
        static bool ClearNarrow(VehicleRespawn r, Vector3 q, float yawDeg)
        {
            if (ShallowWater.InWater(q + Vector3.up * 0.1f)) return false;
            var own = r.GetComponent<Rigidbody>();
            int n = Physics.OverlapBoxNonAlloc(q + Vector3.up * 1.2f, new Vector3(0.9f, 0.6f, 2.2f), hits, Quaternion.Euler(0f, yawDeg, 0f), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) { var h = hits[i]; if (h is TerrainCollider || h.attachedRigidbody == own) continue; return false; }
            return true;
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
