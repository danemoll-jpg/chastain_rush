using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 18, item 4: a drive-through drainage pipe (MapBuilder.Pipes.cs builds it from Data/pipes/pipes.json). Pipes are their
    // own layer under the terrain: an inlet cut down from the ground, a round concrete pipe under it, an outlet cut back up; the
    // terrain has holes only over the cuts. This component knows the pipe's floor, for:
    //   resets   a reset (R / Y) inside a pipe puts the vehicle back on the pipe's floor, facing along it (the way the vehicle
    //            was heading; in a race, the way the race goes through it), on the first spot clear of other vehicles
    //   lines    a race line through a pipe (a shortcut's path, the AI's line) takes its heights from the pipe's floor, not from
    //            the ground above it (RaceRoute)
    public sealed class DrainPipe : MonoBehaviour
    {
        public string id;
        public Vector3[] floor;          // the floor's centerline, a to b, 0.5 m apart (cuts and pipe)
        public float halfWidth = 2.4f;   // inside the cut walls; the pipe's own floor is narrower, its sides curve up
        public float clearHeight = 4.4f; // floor to the pipe's crown
        static readonly List<DrainPipe> all = new List<DrainPipe>();
        static readonly Collider[] hits = new Collider[16];

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        // the station (index into floor, fractional) and lateral offset of p, if p is inside this pipe's space
        bool Locate(Vector3 p, float lateralPad, out float station, out Vector3 at, out Vector3 along)
        {
            station = 0f; at = default; along = Vector3.forward;
            if (floor == null || floor.Length < 2) return false;
            float best = float.MaxValue; var q = new Vector2(p.x, p.z);
            for (int i = 0; i + 1 < floor.Length; i++)
            {
                var a = new Vector2(floor[i].x, floor[i].z); var ab = new Vector2(floor[i + 1].x, floor[i + 1].z) - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                float d = (a + ab * t - q).sqrMagnitude;
                if (d < best) { best = d; station = i + t; }
            }
            if (Mathf.Sqrt(best) > halfWidth + lateralPad) return false;
            int k = Mathf.Clamp((int)station, 0, floor.Length - 2);
            at = Vector3.Lerp(floor[k], floor[k + 1], station - k);
            along = floor[k + 1] - floor[k]; along.y = 0f; along.Normalize();
            return station > 0.01f && station < floor.Length - 1.01f;
        }

        public static DrainPipe Inside(Vector3 p, out float station)
        {
            station = 0f;
            foreach (var d in all)
                if (d.Locate(p, 0.6f, out station, out var at, out _) && p.y > at.y - 1.5f && p.y < at.y + d.clearHeight + 0.5f) return d;
            return null;
        }

        // a race line's height at (x, z) given the ground's: the floor's where the point is in a pipe or its cuts, and the
        // ground there stands above the floor (the pipe runs under it)
        public static float LineHeight(float x, float z, float groundY)
        {
            foreach (var d in all)
                if (d.Locate(new Vector3(x, 0f, z), 0.5f, out _, out var at, out _) && groundY > at.y + 0.3f) return at.y;
            return groundY;
        }

        public static bool Recover(Vector3 p, Vector3 heading, Vector3 raceForward, Rigidbody own, out Vector3 pos, out float yaw)
        {
            pos = default; yaw = 0f;
            var pipe = Inside(p, out float s);
            if (pipe == null) return false;
            pipe.Locate(p, 1f, out s, out _, out var along);
            var fwd = raceForward.sqrMagnitude > 0.01f ? raceForward : heading;
            float sign = Vector3.Dot(along, fwd) >= 0f ? 1f : -1f;
            foreach (float back in new[] { 0f, -4f, 4f, -8f, 8f, -12f })
            {
                float st = Mathf.Clamp(s - sign * back * 2f, 2f, pipe.floor.Length - 3f);   // floor points are 0.5 m apart
                int k = (int)st; var at = Vector3.Lerp(pipe.floor[k], pipe.floor[k + 1], st - k);
                var dir = (pipe.floor[k + 1] - pipe.floor[k]) * sign; dir.y = 0f; dir.Normalize();
                var rot = Quaternion.LookRotation(dir, Vector3.up);
                int n = Physics.OverlapBoxNonAlloc(at + Vector3.up * 1.0f, new Vector3(1.1f, 0.6f, 2.4f), hits, rot, ~0, QueryTriggerInteraction.Ignore);
                bool clear = true;
                for (int i = 0; i < n; i++) if (hits[i].attachedRigidbody != null && hits[i].attachedRigidbody != own) { clear = false; break; }   // the pipe itself is fine to stand in
                if (!clear) continue;
                pos = at; yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                return true;
            }
            return false;
        }
    }
}
