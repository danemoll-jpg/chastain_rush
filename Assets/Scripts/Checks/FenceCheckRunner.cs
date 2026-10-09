using System.Collections.Generic;
using System.IO;
using System.Text;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Development check only (added by Editor/FenceCheck.cs, never saved into the scene). Drives the real car through the real
    // ArcadeVehicle.Simulate step straight at a wooden privacy fence panel and at a white rail fence panel on level ground, and
    // reports what happened: did the panels break, how much speed did it cost, was the car stopped, flipped or launched;
    // then it starts a race (RaceController.StartRace) and checks that the broken fences are whole again.
    public sealed class FenceCheckRunner : MonoBehaviour
    {
        public string outputDir;
        ArcadeVehicle car;
        readonly StringBuilder log = new StringBuilder();
        int stage; float t; Vector3 start, aim, dir;
        float speedBefore, minSpeed, maxUp, maxTilt, preSpeed; int minGrounded; int brokenBefore; bool started;
        string[] kinds = { "Fence_Privacy", "Fence_Rail", "Fence_Picket" };
        int panelTried;

        void Start() { car = FindAnyObjectByType<ArcadeVehicle>(); car.enabled = false; }

        bool Pick(string kind, out FencePanel panel)
        {
            panel = null;
            // Round 8: a stable order (FindObjectsByType's order changes between runs, so the check tested a different panel each time)
            var all = new List<FencePanel>(FindObjectsByType<FencePanel>());
            all.Sort((x, y) => { var a = x.transform.position; var b = y.transform.position; int c0 = a.x.CompareTo(b.x); return c0 != 0 ? c0 : a.z.CompareTo(b.z); });
            foreach (var fp in all)
            {
                if (!fp.chunk.name.StartsWith(kind)) continue;
                var c = fp.transform.position; var along = fp.transform.forward;
                var normal = Vector3.Cross(Vector3.up, along).normalized;
                foreach (float side in new[] { 1f, -1f })
                {
                    var from = c + normal * side * 30f; from.y = 0f;
                    var gy = GroundQuery.Height(from.x, from.z);
                    // level, open approach: ground within 0.8 m over the 30 m, nothing solid between
                    if (Mathf.Abs(gy - c.y) > 1.0f) continue;
                    var o = new Vector3(from.x, gy + 0.9f, from.z);
                    var target = new Vector3(c.x, gy + 0.9f, c.z);
                    if (Physics.Raycast(o, (target - o).normalized, (target - o).magnitude - 0.8f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    // Round 8: and the car's start is not inside something solid (a ray from inside a house box does not hit it)
                    if (Physics.CheckBox(o, new Vector3(1.2f, 0.6f, 2.2f), Quaternion.LookRotation(-normal * side, Vector3.up), ~0, QueryTriggerInteraction.Ignore)) continue;
                    // and open ground for 14 m beyond the fence (a solid house or wall behind it would stop any car)
                    var behindDir = -normal * side;
                    if (Physics.Raycast(target + behindDir * 0.8f, behindDir, 14f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (Mathf.Abs(GroundQuery.Height(c.x + behindDir.x * 12f, c.z + behindDir.z * 12f) - c.y) > 1.2f) continue;
                    // no other fence panel of this chunk behind it is needed; just require this one is not already broken
                    start = new Vector3(from.x, gy + 0.8f, from.z); aim = c; dir = -normal * side;
                    panel = fp; return true;
                }
            }
            return false;
        }

        void FixedUpdate()
        {
            t += Time.fixedDeltaTime;
            if (stage >= kinds.Length * 2 && stage < 100) { RestoreStage(); return; }
            if (stage >= 100) return;
            if (!started)
            {
                string kind = kinds[stage / 2]; bool control = stage % 2 == 1;
                if (!control && !Pick(kind, out var fp)) { log.AppendLine($"{kind}: no level, open panel found"); stage += 2; return; }
                var rot = Quaternion.LookRotation(dir, Vector3.up);
                var body = car.Body; body.position = start; body.rotation = rot; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                car.transform.SetPositionAndRotation(start, rot); Physics.SyncTransforms(); car.ClearSteering();
                started = true; t = 0f; minSpeed = 99f; maxUp = -99f; maxTilt = 0f; minGrounded = 4; preSpeed = 0f;
                brokenBefore = FenceChunk.TotalBroken();
                log.AppendLine($"== {kind}{(control ? " CONTROL (same line, that panel already gone)" : "")}: panel at ({aim.x:0},{aim.z:0}), car starts 30 m away, aiming straight at it; broken before {brokenBefore}");
                return;
            }
            Vector3 to = new Vector3(aim.x - car.transform.position.x, 0f, aim.z - car.transform.position.z);
            float angle = Vector3.SignedAngle(new Vector3(car.transform.forward.x, 0f, car.transform.forward.z), dir, Vector3.up);
            float speed = car.ForwardSpeed;
            car.Simulate(speed < 15f ? 1f : 0f, 0f, Mathf.Clamp(angle / 25f, -1f, 1f), Time.fixedDeltaTime);
            if (t < 1.5f) return;
            float dist = Vector3.Dot(aim - car.transform.position, dir);
            if (dist > 3f) preSpeed = speed;
            int broken = FenceChunk.TotalBroken() - brokenBefore;
            if (broken > 0 || dist < 1f)
            {
                minSpeed = Mathf.Min(minSpeed, speed);
                maxUp = Mathf.Max(maxUp, car.Body.linearVelocity.y);
                maxTilt = Mathf.Max(maxTilt, Vector3.Angle(car.transform.up, Vector3.up));
                minGrounded = Mathf.Min(minGrounded, car.GroundedWheels);
            }
            if (dist < -10f || t > 14f)
            {
                int total = FenceChunk.TotalBroken() - brokenBefore;
                log.AppendLine($"   speed just before the fence {preSpeed:0.0} m/s, lowest after {minSpeed:0.0} m/s (cost {(1f - minSpeed / Mathf.Max(preSpeed, 0.1f)) * 100f:0}%), speed 10 m past {speed:0.0} m/s");
                log.AppendLine($"   panels broken {total} (flying pieces now {FenceChunk.MovingPieceCount}); peak upward speed {maxUp:0.00} m/s; max tilt {maxTilt:0.0} deg; fewest wheels grounded {minGrounded}; car kept moving: {speed > 5f}");
                stage++; started = false;
            }
        }

        void RestoreStage()
        {
            int before = FenceChunk.TotalBroken();
            var race = FindAnyObjectByType<RaceController>();
            if (race != null) race.StartRace(); else FenceChunk.RestoreAll();
            Physics.SyncTransforms();
            int after = FenceChunk.TotalBroken();
            log.AppendLine($"== race start: broken panels {before} -> {after} ({(race != null ? "RaceController.StartRace" : "RestoreAll")}), flying pieces {FenceChunk.MovingPieceCount}");
            File.WriteAllText(Path.Combine(outputDir, "fence_check.txt"), log.ToString());
            Debug.Log("[FenceCheck]\n" + log);
            stage = 100;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
