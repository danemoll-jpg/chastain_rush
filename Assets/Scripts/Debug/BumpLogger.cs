using System;
using System.Collections.Generic;
using System.IO;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Records road bumps without anyone pressing anything (Round 4, item 4). While the player car drives, it watches for
    //   AIRBORNE  all four wheels off the ground for 0.2 s or more, after leaving the ground on a road, and
    //   JOLT      a sudden change in vertical speed (2 m/s or more within 0.1 s) while driving on a road,
    // and appends one line each (time, coordinates, nearest road name, speed) to local/bump_log.txt, with a LANDED line
    // after each AIRBORNE. The file is git-ignored; it is cleared by deleting it. Not active in batch mode (the scripted
    // drive checks must not write to it) or on a respawn / reset.
    [DefaultExecutionOrder(100)]
    public sealed class BumpLogger : MonoBehaviour
    {
        public const float AirborneAfter = 0.2f, JoltSpeed = 2f, JoltWindow = 0.1f, MinSpeed = 3f, JoltCooldown = 1f;

        public static string LogPath => Path.Combine(DeveloperHud.ReportRoot(), "..", "bump_log.txt");
        public string LastLine { get; private set; } = "";
        public int Events { get; private set; }

        ArcadeVehicle car;
        Rigidbody body;
        VehicleRespawn respawn;
        readonly Queue<(float t, float vy)> history = new Queue<(float, float)>();
        Vector3 lastPos, lastGroundedPos, takeoffPos;
        float airTime, peakY, nextJolt, landedAt = -10f;
        bool airborneLogged, wasAirborne;
        string takeoffRoad;

        void Awake()
        {
            car = FindAnyObjectByType<ArcadeVehicle>();
            if (!car) { enabled = false; return; }
            body = car.Body;
            respawn = car.GetComponent<VehicleRespawn>();
            if (respawn) respawn.Respawned += ResetState;
            lastPos = lastGroundedPos = car.transform.position;
            Append($"--- {DateTime.Now:yyyy-MM-dd HH:mm:ss} play session started ---");
        }

        void OnDestroy() { if (respawn) respawn.Respawned -= ResetState; }

        void ResetState()
        {
            history.Clear(); airTime = 0f; airborneLogged = wasAirborne = false;
            lastPos = lastGroundedPos = car.transform.position;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (dt <= 0f || Time.timeScale <= 0f) return;
            var pos = car.transform.position;
            if ((pos - lastPos).sqrMagnitude > 25f) { ResetState(); return; } // teleported (respawn or reset)
            lastPos = pos;
            float vy = body.linearVelocity.y;
            var v = body.linearVelocity; v.y = 0f;
            float hSpeed = v.magnitude;
            bool grounded = car.GroundedWheels > 0;

            history.Enqueue((Time.time, vy));
            while (history.Count > 0 && Time.time - history.Peek().t > JoltWindow + 0.001f) history.Dequeue();

            if (grounded)
            {
                if (wasAirborne)
                {
                    if (airborneLogged)
                        Log("LANDED", pos, hSpeed, vy, $"after {airTime:0.00} s in the air, peak {peakY - takeoffPos.y:+0.00;-0.00} m against takeoff, {Vector3.Distance(Flat(pos), Flat(takeoffPos)):0.0} m from takeoff", takeoffRoad);
                    wasAirborne = airborneLogged = false;
                    airTime = 0f;
                    landedAt = Time.time;
                    history.Clear();
                    return;
                }
                lastGroundedPos = pos;
                // a sudden change in vertical speed while driving on a road
                if (history.Count > 1 && hSpeed >= MinSpeed && Time.time >= nextJolt && Time.time - landedAt > 0.5f)
                {
                    float dv = vy - history.Peek().vy;
                    if (Mathf.Abs(dv) >= JoltSpeed && RoadLookup.Instance && RoadLookup.Instance.OnRoad(pos, out string road, out _))
                    {
                        nextJolt = Time.time + JoltCooldown;
                        Log("JOLT", pos, hSpeed, vy, $"vertical speed changed {dv:+0.0;-0.0} m/s in {JoltWindow:0.0} s", road);
                    }
                }
                return;
            }

            // no wheel on the ground
            if (!wasAirborne)
            {
                wasAirborne = true; airTime = 0f; takeoffPos = lastGroundedPos; peakY = pos.y; airborneLogged = false;
                takeoffRoad = RoadLookup.Instance && RoadLookup.Instance.OnRoad(lastGroundedPos, out string r, out _) ? r : null;
            }
            airTime += dt;
            peakY = Mathf.Max(peakY, pos.y);
            if (!airborneLogged && airTime >= AirborneAfter && takeoffRoad != null && hSpeed >= MinSpeed)
            {
                airborneLogged = true;
                Log("AIRBORNE", takeoffPos, hSpeed, vy, "left the ground (position is where it took off)", takeoffRoad);
            }
        }

        static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0f, p.z);

        void Log(string kind, Vector3 p, float hSpeed, float vy, string detail, string road)
        {
            float off = 0f;
            if (RoadLookup.Instance != null) RoadLookup.Instance.Nearest(p, out _, out off, out _);
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | t={Time.time:0.0}s | {kind,-8} | x={p.x:0.0} y={p.y:0.0} z={p.z:0.0} | {road ?? "(off road)"} ({off:0.0} m from centreline) | " +
                          $"{hSpeed:0.0} m/s ({hSpeed * 2.23694f:0} mph) vy={vy:+0.0;-0.0} m/s | {detail}";
            LastLine = line;
            Events++;
            Append(line);
        }

        static void Append(string line)
        {
            try
            {
                string path = Path.GetFullPath(LogPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch (Exception e) { Debug.LogWarning("Bump log not written: " + e.Message); }
        }
    }
}
