using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 36 (Part D2, Dan: "the fire hydrants should maybe somehow be tied into the underground area"; he picked the geysers): a storm grate in
    // the street, a little way down from a hydrant (HydrantLinks). Knock that hydrant over and a column of water shoots up out of the grate for
    // `seconds`; drive into it and it throws the vehicle high (to `height` over the grate), roughly upright (its turn stopped, its heading kept),
    // its speed turned along the street so it comes down on the street ahead (big air: the tricks count as anywhere in Free Roam). World: the
    // grate; when it may erupt is HydrantLinks' (Free Roam only for now). Particles only for the look (HydrantWater's way), the launch a check of
    // the vehicles near it (DrainPipe.Vehicles), no collider.
    public sealed class Geyser : MonoBehaviour
    {
        public Vector3 along = Vector3.forward;    // the street's direction here (flat, unit)
        public float height = 13f, seconds = 7f, radius = 1.7f;
        public Material sprayMaterial;
        public static Material SprayMaterial;      // set by the first geyser (Spray below uses it)
        public static int Launches { get; private set; }
        public static event System.Action<Geyser, ArcadeVehicle> Launched;
        public bool Erupting => left > 0f;
        float left; GameObject column; readonly Dictionary<Rigidbody, float> recent = new();
        void Awake() { if (sprayMaterial && !SprayMaterial) SprayMaterial = sprayMaterial; }

        public void Erupt()
        {
            left = seconds;
            if (column) Destroy(column);
            column = Column(transform.position, height + 2f, seconds + 1.5f);
        }
        void FixedUpdate()
        {
            if (left <= 0f) return;
            left -= Time.fixedDeltaTime;
            foreach (var car in DrainPipe.Vehicles)
            {
                if (!car || car.Body == null || car.Body.isKinematic) continue;
                var d = car.Body.position - transform.position; if (d.y < -1f || d.y > 4f) continue;
                if (new Vector2(d.x, d.z).magnitude > radius + .6f) continue;
                if (recent.TryGetValue(car.Body, out float at) && Time.time - at < 2.5f) continue;
                recent[car.Body] = Time.time;
                // the throw: up to `height`, the speed along the street kept (the way the vehicle was going; at least 8 m/s), across it taken out
                var v = car.Body.linearVelocity; float va = Vector3.Dot(v, along); if (Mathf.Abs(va) < 8f) va = va >= 0f ? 8f : -8f;
                float vy = Mathf.Sqrt(2f * 9.81f * height);
                car.Body.linearVelocity = along * va + Vector3.up * vy; car.Body.angularVelocity = Vector3.zero;
                var fwd = Vector3.ProjectOnPlane(car.transform.forward, Vector3.up); if (fwd.sqrMagnitude > 1e-3f) car.Body.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                Launches++; Launched?.Invoke(this, car);
                BreakSounds.Play(transform.position, 12f, BreakSounds.Kind.Hydrant);
            }
        }
        // a column of water (the hydrant's jet, taller) for `life` seconds, then it falls away
        static GameObject Column(Vector3 at, float h, float life)
        {
            var go = new GameObject("Geyser water"); go.transform.position = at;
            float up = Mathf.Sqrt(2f * 9.81f * h);
            var ps = Particles(go.transform, 700f, 2.8f, .35f, .8f, new Color(.88f, .94f, 1f, .9f), 6f, .25f, up * .92f, up * 1.02f);
            Particles(go.transform, 160f, 2.4f, .8f, 1.6f, new Color(.92f, .96f, 1f, .5f), 20f, .4f, up * .45f, up * .7f);
            var m = ps.main; go.AddComponent<Fade>().life = life;
            return go;
        }
        // Part D1: the burst of water out of a blown manhole (short)
        public static void Spray(Vector3 at, float seconds, float h) { var c = Column(at, h, seconds); }
        static ParticleSystem Particles(Transform parent, float rate, float life, float s0, float s1, Color col, float angle, float r, float v0, float v1)
        {
            var go = new GameObject("spray"); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = life; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 3000; main.gravityModifier = 1f; main.startSpeed = new ParticleSystem.MinMaxCurve(v0, v1);
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = angle; sh.radius = r; sh.rotation = new Vector3(-90f, 0f, 0f);
            var rd = go.GetComponent<ParticleSystemRenderer>(); rd.sharedMaterial = SprayMaterial; rd.renderMode = ParticleSystemRenderMode.Billboard; rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rd.receiveShadows = false;
            ps.Play(); return ps;
        }
        sealed class Fade : MonoBehaviour
        {
            public float life; float t;
            void Update() { t += Time.deltaTime; if (t > life) { foreach (var p in GetComponentsInChildren<ParticleSystem>()) { var e = p.emission; e.enabled = false; } } if (t > life + 3.5f) Destroy(gameObject); }
        }
    }
}
