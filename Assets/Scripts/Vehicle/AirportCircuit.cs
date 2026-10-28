using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 69: the airport's life: one Sky Hopper that waits on the apron, taxis to runway 09's end, takes off east, flies a left-hand circuit at
    // 300 m round the airport, lands on 09 and taxis back; then waits again (a cycle of about seven minutes). Visual only: no collider, no physics.
    // Round 79 (A): where it is comes from the world's clock (WorldClock) through a table of times along its path, so a replay shows it as it was.
    public sealed class AirportCircuit : MonoBehaviour
    {
        readonly List<(Vector3 p, float v)> path = new(); float[] cum; Transform plane, prop; float propAngle; readonly List<float> tS = new(), tT = new(); float travel;
        const float Wait = 240f, FirstWait = 40f;
        void Start()
        {
            var go = new GameObject("Circuit plane"); go.transform.SetParent(transform, false);
            var v = VehicleVisual.Build(go.transform, VehicleProfile.Plane, null, RiderLook.Player); plane = go.transform;
            foreach (var t in v.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Rotor disc").ToArray()) if (t) Destroy(t.gameObject);
            prop = v.Find("Propeller");
            VehiclePaint.Apply(plane, new Color(.82f, .16f, .12f));
            float G(float x, float z) => HelicopterFlight.GroundUnder(new Vector3(x, 2500f, z), out var h) ? h.point.y + PlaneFlight.WheelDrop : 400f;
            void P(float x, float z, float v_, float up = -1f) => path.Add((new Vector3(x, up < 0f ? G(x, z) : G(x, z) + up, z), v_));
            // the apron, the taxiway north and west, onto the runway's west end (09); the take-off roll east and the climb out
            P(-3366f, -2068f, 0f); P(-3366f, -2040f, 6f); P(-3440f, -2002f, 8f); P(-3543f, -1994f, 8f); P(-3548f, -1925f, 6f); P(-3600f, -1906f, 5f); P(-3660f, -1908f, 4f);
            P(-3600f, -1906f, 10f); P(-3300f, -1892f, 30f); P(-3050f, -1880f, 38f, 8f); P(-2700f, -1865f, 45f, 60f); P(-2300f, -1846f, 50f, 160f);
            // the circuit (left-hand: north of the runway), back down onto 09 from the west
            P(-1900f, -1830f, 55f, 260f); P(-1700f, -1500f, 55f, 300f); P(-2600f, -1300f, 60f, 300f); P(-3800f, -1350f, 60f, 300f); P(-4500f, -1600f, 55f, 250f);
            P(-4400f, -1900f, 48f, 160f); P(-4100f, -1915f, 40f, 70f); P(-3800f, -1912f, 35f, 18f); P(-3660f, -1908f, 30f, 1f); P(-3500f, -1901f, 26f); P(-3200f, -1888f, 12f);
            P(-3160f, -1886f, 6f); P(-3155f, -1930f, 6f); P(-3153f, -1976f, 6f); P(-3200f, -2010f, 6f); P(-3366f, -2040f, 5f); P(-3366f, -2068f, 0f);
            cum = new float[path.Count]; for (int i = 1; i < path.Count; i++) cum[i] = cum[i - 1] + (path[i].p - path[i - 1].p).magnitude;
            // the time at each point along the path (its speed from the points', at least 3 m/s, in 2 m steps)
            float tt = 0f; tS.Add(0f); tT.Add(0f);
            for (float a = 0f; a < cum[cum.Length - 1]; a += 2f) { float step = Mathf.Min(2f, cum[cum.Length - 1] - a); tt += step / Speed(a + step * .5f); tS.Add(a + step); tT.Add(tt); }
            travel = tt;
            Place(0f);
        }
        float Speed(float at) { int i = Seg(at); float t = Mathf.InverseLerp(cum[i], cum[i + 1], at); return Mathf.Max(3f, Mathf.Lerp(path[i].v, path[i + 1].v, t)); }
        void Update()
        {
            if (cum == null) return;
            // the cycle: waiting on the apron (Wait s), then round the path; at the clock's start FirstWait s of the wait are left
            float c = Mathf.Repeat(WorldClock.Now + (Wait - FirstWait), Wait + travel), dt = Time.deltaTime;
            if (c < Wait) { propAngle += 300f * dt; if (prop) prop.localRotation = Quaternion.Euler(0, 0, propAngle); Place(0f); return; }
            float tm = c - Wait; int lo = 0, hi = tT.Count - 1; while (hi - lo > 1) { int m = (lo + hi) >> 1; if (tT[m] <= tm) lo = m; else hi = m; }
            float s = Mathf.Lerp(tS[lo], tS[hi], Mathf.InverseLerp(tT[lo], tT[hi], tm)), v = Speed(s);
            propAngle += (v > 20f ? 3000f : 1200f) * dt; if (prop) prop.localRotation = Quaternion.Euler(0, 0, propAngle);
            Place(s);
        }
        int Seg(float at) { int i = 0; while (i + 2 < cum.Length && cum[i + 1] < at) i++; return i; }
        Vector3 At(float at) { int i = Seg(at); return Vector3.Lerp(path[i].p, path[i + 1].p, Mathf.InverseLerp(cum[i], cum[i + 1], Mathf.Clamp(at, 0f, cum[cum.Length - 1]))); }
        void Place(float at)
        {
            var a = At(at); var b = At(at + 25f); var c = At(at + 50f); var d = b - a; if (d.sqrMagnitude < 1e-4f) return;
            var h1 = new Vector3(b.x - a.x, 0, b.z - a.z); var h2 = new Vector3(c.x - b.x, 0, c.z - b.z);
            float turn = h1.sqrMagnitude > 1e-4f && h2.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(h1, h2, Vector3.up) : 0f;
            bool air = a.y - (HelicopterFlight.GroundUnder(new Vector3(a.x, 2500f, a.z), out var g) ? g.point.y : a.y) > PlaneFlight.WheelDrop + 1f;
            var look = Quaternion.LookRotation(air ? d : h1.sqrMagnitude > 1e-4f ? h1 : d, Vector3.up);
            plane.SetPositionAndRotation(a, look * Quaternion.Euler(0, 0, air ? Mathf.Clamp(-turn * 1.6f, -35f, 35f) : 0f));
        }
    }
}
