using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 44 (Dan, 2026-10-18: the Chastain Lakes race's gazebo shortcut, "jump and land in a hole in the lake that will tunnel through and pop
    // out of manhole in the road"): East Chastain Lake's morning-glory drain (MapBuilder.LakesRace.cs). A vehicle that comes into the bell (off the
    // gazebo's end, or driving through the water) is taken down the shaft: pulled to its middle, falling faster than anything else falls (1.4 g,
    // no more than 17 m/s), its nose turned the way B28 leaves LD and dipped; near LD's floor it is set level, facing B28's mouth, and sent on into
    // it at 'exitSpeed' (then it drives B28 and climbs MH14's riser under its own power). Not a ramp: a scripted drop (the standing rule of
    // 2026-10-17 allows one for something that is not a ramp). Never a trap: a reset in the shaft or the room puts the vehicle back on the race's
    // line (CarRecovery), and anything that hangs in the shaft is let go after 4 s. World, Free Roam too. The water: a curtain pouring over the lip.
    public sealed class LakeDrain : MonoBehaviour
    {
        public Vector3 mouth;              // the bell's middle at the water's surface
        public float radius = 6.3f, shaftRadius = 4.2f, above = 3.5f, below = 2f;
        public float floorY;               // LD's floor
        public Vector3 exit;               // B28's mouth on LD's floor
        public float exitSpeed = 20f;
        public Material sprayMaterial;
        public static int Swallowed { get; private set; }
        public static event System.Action<ArcadeVehicle> Took;   // checks
        public static readonly List<LakeDrain> All = new List<LakeDrain>();
        sealed class Drop { public float t; public Vector3 last; }
        readonly Dictionary<ArcadeVehicle, Drop> down = new Dictionary<ArcadeVehicle, Drop>();
        readonly Dictionary<Rigidbody, float> recent = new Dictionary<Rigidbody, float>();

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void Start() { Curtain(); }

        public bool InBell(Vector3 p)
        {
            var d = p - mouth; d.y = 0f;
            return d.magnitude < radius && p.y > mouth.y - below && p.y < mouth.y + above;
        }

        void FixedUpdate()
        {
            foreach (var c in DrainPipe.Vehicles)
            {
                if (!c || c.Body == null || c.Body.isKinematic || down.ContainsKey(c) || !InBell(c.Body.position)) continue;
                if (recent.TryGetValue(c.Body, out float at) && Time.time - at < 3f) continue;
                down[c] = new Drop { t = 0f, last = c.Body.position }; recent[c.Body] = Time.time; Swallowed++; Took?.Invoke(c);
                BreakSounds.Play(c.Body.position, 16f, BreakSounds.Kind.Hydrant);   // a splash
            }
            if (down.Count == 0) return;
            float dt = Time.fixedDeltaTime;
            var flatOut = new Vector3(exit.x - mouth.x, 0f, exit.z - mouth.z).normalized;
            foreach (var kv in new List<KeyValuePair<ArcadeVehicle, Drop>>(down))
            {
                var c = kv.Key; var dr = kv.Value;
                if (!c || c.Body == null || c.Body.isKinematic || (c.Body.position - dr.last).sqrMagnitude > 12f * 12f) { down.Remove(c); continue; }   // gone, or reset
                var b = c.Body; dr.t += dt;
                var p = b.position;
                if (p.y < floorY + 3f || dr.t > 4f)
                {
                    // out near the bottom, in the room's air (3 m over its floor): level, facing B28, on into it, settling onto the floor
                    // (Round 44 check: let go at the floor itself, a vehicle still nose down met it and stalled there)
                    var to = new Vector3(exit.x - p.x, 0f, exit.z - p.z); var dir = to.sqrMagnitude > 1f ? to.normalized : flatOut;
                    b.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    b.angularVelocity = Vector3.zero;
                    b.linearVelocity = dir * exitSpeed + Vector3.down * 3f;
                    down.Remove(c); continue;
                }
                var toAxis = new Vector3(mouth.x - p.x, 0f, mouth.z - p.z);
                var h = Vector3.ClampMagnitude(toAxis * 3f, 8f);
                float vy = Mathf.Max(b.linearVelocity.y + Physics.gravity.y * 0.4f * dt, -17f);
                if (p.y > mouth.y - 0.5f) vy = Mathf.Min(vy, -2f);   // over the lip: straight in
                float toFloor = p.y - (floorY + 3f);
                if (toFloor < 6f) vy = Mathf.Max(vy, -Mathf.Lerp(5f, 17f, toFloor / 6f));   // braked over the last meters (the room)
                b.linearVelocity = new Vector3(h.x, vy, h.z);
                // the nose round to B28's way, dipped in the shaft and level again in the room, no spin
                float k = Mathf.Clamp01((mouth.y - p.y) / Mathf.Max(1f, mouth.y - floorY)), lvl = Mathf.Clamp01(toFloor / 6f);
                var want = Quaternion.LookRotation(flatOut, Vector3.up) * Quaternion.Euler(Mathf.Lerp(15f, 40f, k) * lvl, 0f, 0f);
                b.MoveRotation(Quaternion.Slerp(b.rotation, want, Mathf.Clamp01(dt * 5f)));
                b.angularVelocity = Vector3.zero;
                dr.last = b.position;
            }
        }

        // the water pouring over the lip all round into the shaft, and a little mist over it
        void Curtain()
        {
            var go = new GameObject("Lake drain water"); go.transform.SetParent(transform, false);
            go.transform.position = mouth + Vector3.up * 0.3f; go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = 1.4f; main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f); main.startColor = new Color(0.86f, 0.92f, 0.96f, 0.75f);
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 3000; main.gravityModifier = 1f; main.startSpeed = new ParticleSystem.MinMaxCurve(-1.6f, -0.8f);   // inward, off the lip
            var em = ps.emission; em.rateOverTime = 900f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = shaftRadius + 0.2f; sh.radiusThickness = 0f;
            var rd = go.GetComponent<ParticleSystemRenderer>(); rd.sharedMaterial = sprayMaterial ? sprayMaterial : Geyser.SprayMaterial; rd.renderMode = ParticleSystemRenderMode.Billboard;
            rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rd.receiveShadows = false;
            ps.Play();
        }
    }
}
