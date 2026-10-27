using System.Linq;
using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 71 (Dan, 2026-10-26: "a plane race ... the typical fly through the rings course ... a race with others or solo speed run"): the air race
    // under the race's own rules (RaceRoute.Air: its gates are rings in the sky; a ring missed is a missed checkpoint, 5 s, the lap still counts).
    //   The start: a standing take-off. The grid is on the runway (the route's lead_in): the player at the back on the centerline, the AI pilots ahead
    //   in two lanes 9 m either side, 45 and 90 m up the runway; all held until GO, then full throttle. (Chosen over an air start: the planes pass
    //   through each other in the race (no mid-air knock that ends two runs), so a runway start cannot pile up, and the take-off is half the fun.)
    //   The AI pilots: PlanePilot flies each one along the rings' line (Route.Path), by difficulty.
    //   A crash: a moment, then back in the air 150 m before the last ring passed (the start ring before the first), on the line, heading through it,
    //   at 70 m/s (AirReset; the reset button the same). Never on the ground, never stuck; the lap goes on (a clean-lap ghost is spoiled).
    public sealed partial class RaceController
    {
        public bool AirRace => Route != null && Route.Air;
        public bool IsAir(string file) { var j = RouteJsonOf(file); return j != null && j.Contains("\"air\": true"); }
        public const float AirResetBack = 150f, AirResetSpeed = 70f;

        void SpawnPilots()
        {
            var rng = new System.Random(System.Environment.TickCount);
            DriverVariation.Seed = rng.Next();
            var riders = RiderLook.Field(DriverVariation.Seed, Mathf.Max(3, AiCount), RiderLook.Player);
            var fwd = Quaternion.Euler(0, Route.GridYaw, 0) * Vector3.forward; var right = Vector3.Cross(Vector3.up, fwd);
            var planes = new List<ArcadeVehicle> { car };
            for (int n = 0; n < AiCount; n++)
            {
                var clone = Instantiate(car.gameObject); clone.name = AiNames[n];
                foreach (var mb in clone.GetComponents<MonoBehaviour>())
                {
                    if (mb is VehicleInput || mb is VehicleRespawn) { mb.enabled = false; continue; }
                    if (mb is ArcadeVehicle || mb is VehicleConfiguration || mb is RiderGestures || mb is VehicleLights || mb is PlaneFlight) continue;
                    mb.enabled = false; if (mb is VehicleAudio) DestroyImmediate(mb); else Destroy(mb);
                }
                foreach (var src in clone.GetComponents<AudioSource>()) Destroy(src);
                foreach (Transform child in clone.transform) if (child.name == "Headlight") Destroy(child.gameObject);
                var ai = clone.GetComponent<ArcadeVehicle>(); var cfg = clone.GetComponent<VehicleConfiguration>();
                var oldPlane = clone.GetComponent<PlaneFlight>(); if (oldPlane) oldPlane.End();   // started afresh by Apply (its own engine, 3D)
                cfg.riderLook = riders[n % riders.Length];
                // Round 74 (H): AI pilots fly either plane (the route's planes; the opponents page's choice for the slot if it is one of them, else random)
                var planeIds = (RouteData?.vehicles ?? new string[0]).Where(v => VehicleProfile.Find(v).IsPlane).ToArray(); if (planeIds.Length == 0) planeIds = new[] { VehicleProfile.Plane.Id };
                var savedAi = SavedAiChoices(); string pid = n < savedAi.Length && planeIds.Contains(savedAi[n]) ? savedAi[n] : planeIds[UnityEngine.Random.Range(0, planeIds.Length)];
                cfg.Apply(pid);
                cfg.SetPaint(AiPaints[n % AiPaints.Length]);
                float d = 45f + 45f * (n / 2), side = n % 2 == 0 ? 9f : -9f; if (n == 2) side = 0f;
                var p = Route.GridPosition + fwd * d + right * side;
                if (HelicopterFlight.GroundUnder(new Vector3(p.x, 2500f, p.z), out var hit)) p = hit.point;
                p += Vector3.up * (PlaneFlight.WheelDrop + .1f);
                var rot = Quaternion.LookRotation(fwd, Vector3.up);
                ai.enabled = false; ai.Body.isKinematic = true;
                ai.Body.position = p; ai.Body.rotation = rot; clone.transform.SetPositionAndRotation(p, rot);
                ai.ClearSteering();
                var e = new RaceEntrant { Name = AiNames[n], Car = ai, Box = clone.GetComponent<BoxCollider>() };
                var pilot = clone.AddComponent<PlanePilot>();
                pilot.Initialize(this, ai, e, n, side);
                e.Reset(); Entrants.Add(e); planes.Add(ai);
                Log($"AI pilot {e.Name}: {cfg.Profile.Name} on the runway at ({p.x:0.0}, {p.z:0.0}), pace {pilot.Pace:0.000}");
            }
            // the race's planes pass through each other (a touch in the air would end two runs: fun first)
            for (int i = 0; i < planes.Count; i++)
                for (int k = i + 1; k < planes.Count; k++)
                    foreach (var a in planes[i].GetComponentsInChildren<Collider>())
                        foreach (var b in planes[k].GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(a, b, true);
            aiActive = true; nextEstimateSample = 0f;
        }

        // a plane's pace so far (meters of the lap a second since it crossed the start), for an estimated finish
        float AirPace(RaceEntrant e)
        {
            float t = RaceClock - e.StartAt; float done = e.ClockStarted ? e.CompletedLaps * Route.LapLength + Into(e) : 0f;
            return t > 20f && done > 1000f ? Mathf.Clamp(done / t, 50f, 140f) : 95f;
        }

        // Round 73 (B2 / B3, Dan: "if you missed a checkpoint, it is impossible to tell which is next"): the ring a human is to fly through next, its
        // plane passed the race's way outside it (within AirMissNear of it): RING MISSED, the guide on to the next one at once; the missed ring can still
        // be gone back for (flown through either way: no penalty) until the next one is taken (then the usual 5 s, OnCheckpoint)
        public const float AirMissNear = 400f;
        void AirRingPassedBy(RaceEntrant e, Vector3 from, Vector3 to)
        {
            if (!e.ClockStarted || e.NextCheckpoint >= Route.Checkpoints.Count || e.RingSkipped == e.NextCheckpoint) return;
            var g = Route.Checkpoints[e.NextCheckpoint];
            Vector3 a = from - g.Position, b = to - g.Position; float za = Vector3.Dot(a, g.Forward), zb = Vector3.Dot(b, g.Forward);
            if (!(za < 0f && zb >= 0f)) return;
            var hit = Vector3.Lerp(a, b, -za / (zb - za)); float across = Vector3.Dot(hit, g.Right), up = hit.y;
            bool inside = g.Ring > 0f ? across * across + up * up <= g.Ring * g.Ring : Mathf.Abs(across) <= g.RingW && Mathf.Abs(up) <= g.RingH;
            if (inside || across * across + up * up > AirMissNear * AirMissNear) return;
            e.RingSkipped = e.NextCheckpoint;
            SayTo(e, $"RING {e.NextCheckpoint + 1} MISSED   fly on to ring {(e.NextCheckpoint + 2 <= Route.Checkpoints.Count ? (e.NextCheckpoint + 2).ToString() : "the finish")} (+{MissPenaltySeconds:0} s) or turn back for it", 4f);
            Log(e, $"RING {e.NextCheckpoint + 1} flown past outside it ({Mathf.Sqrt(across * across + up * up) - Mathf.Max(g.Ring, g.RingW):0} m out): the guide on to the next");
            if (e.IsPlayer) markers.Refresh(this);
        }

        // the guide's line: how far the next ring is above or below
        string AirHint(RaceGate t)
        {
            float dy = t.Position.y - car.transform.position.y;
            return Mathf.Abs(dy) < 12f ? "" : dy > 0f ? $"   ▲ {dy:0} m" : $"   ▼ {-dy:0} m";
        }

        // where a crashed (or reset) plane goes back into the air: on the line AirResetBack before the last ring it passed, heading through it
        bool AirSpot(RaceEntrant e, out Vector3 at, out float yaw, out float pitch)
        {
            float ringS = !e.ClockStarted || e.NextCheckpoint == 0 ? 0f : Route.Checkpoints[Mathf.Min(e.NextCheckpoint, Route.Checkpoints.Count) - 1].S;
            float s = Route.GateS + ringS - AirResetBack;
            at = Route.PointAt(s, out var f);
            var a = Route.PointAt(s - 10f, out _); var b = Route.PointAt(s + 10f, out _);
            yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(Mathf.Atan2(b.y - a.y, 20f) * Mathf.Rad2Deg, -25f, 25f);
            return true;
        }
        RaceEntrant EntrantOf(Component c) => c ? Entrants.Find(x => x.Car && x.Car.gameObject == c.gameObject) : null;

        // a crash's reset (PlaneFlight.CrashStep): straight into the air
        public bool AirReset(PlaneFlight plane)
        {
            if (!AirRace || (State != Stage.Racing && State != Stage.Finished) || !plane) return false;
            var e = EntrantOf(plane); if (e == null || !AirSpot(e, out var at, out float yaw, out float pitch)) return false;
            plane.AirStart(at, yaw, pitch, AirResetSpeed);
            Teleported(e);
            Log(e, $"CRASH RESET: back in the air at ({at.x:0}, {at.y:0}, {at.z:0}) before {(e.ClockStarted && e.NextCheckpoint > 0 ? "ring " + Mathf.Min(e.NextCheckpoint, Route.Checkpoints.Count) : "the start ring")}");
            return true;
        }
        // the reset button (CarRecovery): VehicleRespawn places it, then the plane goes into the air there (PlaneFlight.pendingAir)
        public bool AirReset(PlaneFlight plane, out Vector3 pos, out float yaw)
        {
            pos = Vector3.zero; yaw = 0f;
            if (!AirRace || (State != Stage.Racing && State != Stage.Finished) || !plane) return false;
            var e = EntrantOf(plane); if (e == null || !AirSpot(e, out var at, out yaw, out float pitch)) return false;
            pos = at; plane.pendingAir = (at, yaw, pitch, AirResetSpeed);
            Log(e, $"RESET: back in the air at ({at.x:0}, {at.y:0}, {at.z:0})");
            return true;
        }
        void Teleported(RaceEntrant e) { if (e.IsPlayer) NotifyTeleported(); else e.NotifyTeleported(); }
        public RaceEntrant EntrantFor(ArcadeVehicle c) => Entrants.Find(x => x.Car == c);
    }
}
