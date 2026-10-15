using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 19, item 4b (BUG-002, Dan: "have a small stream of water in it"): the thin stream down a drainage pipe's floor
    // (MapBuilder.Pipes.cs builds its ribbon). Look and sound only, it never slows anyone (it is no ShallowWater):
    //   flow     the water's texture slides down the ribbon (b to a: the pond drains to the creek)
    //   trickle  a looping trickle made here (soft filtered noise with little bubbles), from the point of the stream nearest the
    //            listener, so it is heard all the way through the pipe and fades out past the mouths
    //   splash   a vehicle crossing the stream faster than 3 m/s throws up a few droplets at its wheels
    public sealed class PipeStream : MonoBehaviour
    {
        public Vector3[] line;          // the stream's middle, a to b, 0.5 m apart
        public Material water, splash;
        const float FlowSpeed = 0.35f;  // texture repeats (3 m each) per second
        AudioSource source; ParticleSystem drops;

        readonly Dictionary<ArcadeVehicle, float> emitDebt = new Dictionary<ArcadeVehicle, float>();
        static AudioClip clip;

        void Start()
        {
            var sgo = new GameObject("Pipe stream trickle"); sgo.transform.SetParent(transform, false);   // its own object: it moves to the listener's nearest point (on the ribbon's own, it dragged the ribbon away)
            source = sgo.AddComponent<AudioSource>();
            source.clip = clip ? clip : (clip = Trickle());
            source.loop = true; source.spatialBlend = 1f; source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 2f; source.maxDistance = 22f; Racer.SoundMix.Track(source, 0.32f, Racer.SoundMix.Kind.Ambience);   /* Round 27: the Ambience volume */ source.dopplerLevel = 0f;
            source.Play();
            var go = new GameObject("Pipe stream splashes"); go.transform.SetParent(transform, false);
            drops = go.AddComponent<ParticleSystem>();
            drops.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = drops.main; main.loop = false; main.playOnAwake = false; main.startLifetime = 0.45f; main.startSize = 0.07f; main.gravityModifier = 1.2f; main.maxParticles = 400; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = drops.emission; em.enabled = false;
            var sh = drops.shape; sh.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = splash; r.renderMode = ParticleSystemRenderMode.Billboard;
            drops.Play();
        }

        // Round 23 follow-ups: switched off by NetworkCells when far: its sound stops with it
        void OnEnable() { if (source && !source.isPlaying) source.Play(); }
        void OnDisable() { if (source) source.Stop(); }

        void Update()
        {
            if (water) water.SetTextureOffset("_BaseMap", new Vector2(0f, Time.time * FlowSpeed));
            if (line == null || line.Length < 2) return;
            var listener = Camera.main ? Camera.main.transform.position : transform.position;
            source.transform.position = Nearest(listener, out _);
            // splashes
            foreach (var car in DrainPipe.Vehicles)   // Round 23 follow-ups: the shared search
            {
                if (!car || car.Body == null || car.GroundedWheels == 0) continue;
                var p = car.Body.position; var q = Nearest(p, out float d);
                if (d > 1.2f || Mathf.Abs(p.y - q.y) > 1.6f) continue;
                float speed = car.Body.linearVelocity.magnitude; if (speed < 3f) continue;
                emitDebt.TryGetValue(car, out float debt); debt += Time.deltaTime * Mathf.Min(140f, speed * 7f);
                var v = car.Body.linearVelocity;
                while (debt >= 1f)
                {
                    debt -= 1f;
                    var side = car.transform.right * (Random.value < 0.5f ? -0.55f : 0.55f);
                    var at = new Vector3(q.x, q.y + 0.05f, q.z) + side + car.transform.forward * Random.Range(-1.2f, 0.4f);
                    var vel = -v * 0.08f + side.normalized * Random.Range(0.6f, 1.8f) + Vector3.up * Random.Range(1.2f, 2.8f);
                    drops.Emit(new ParticleSystem.EmitParams { position = at, velocity = vel, startSize = Random.Range(0.04f, 0.1f), startLifetime = Random.Range(0.3f, 0.55f) }, 1);
                }
                emitDebt[car] = debt;
            }
        }

        Vector3 Nearest(Vector3 p, out float dist)
        {
            float best = float.MaxValue; Vector3 at = line[0];
            for (int i = 0; i + 1 < line.Length; i++)
            {
                var a = line[i]; var ab = line[i + 1] - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                var c = a + ab * t; float d = (c - p).sqrMagnitude;
                if (d < best) { best = d; at = c; }
            }
            dist = Mathf.Sqrt(new Vector2(p.x - at.x, p.z - at.z).sqrMagnitude);
            return at;
        }

        // 4 s of soft running water: brown noise through a gentle low-pass, slow swells, and little bubbles (short rising chirps)
        static AudioClip Trickle()
        {
            const int rate = 44100; int n = rate * 4; var data = new float[n];
            var rnd = new System.Random(1919); float b = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float w = (float)rnd.NextDouble() * 2f - 1f;
                b = Mathf.Clamp(b + w * 0.06f, -1f, 1f) * 0.995f; lp += (w * 0.5f + b - lp) * 0.18f;
                float swell = 0.65f + 0.35f * Mathf.Sin(i * 2f * Mathf.PI / n * 3f) * Mathf.Sin(i * 2f * Mathf.PI / n * 5f + 1f);
                data[i] = lp * 0.5f * swell;
            }
            for (int k = 0; k < 70; k++)   // bubbles
            {
                int at = rnd.Next(n - rate / 10); float f0 = 500f + 900f * (float)rnd.NextDouble(), len = 0.03f + 0.05f * (float)rnd.NextDouble(), amp = 0.06f + 0.1f * (float)rnd.NextDouble();
                int m = (int)(len * rate); float ph = 0f;
                for (int i = 0; i < m; i++) { float t = i / (float)m; ph += 2f * Mathf.PI * f0 * (1f + 1.5f * t) / rate; data[at + i] += Mathf.Sin(ph) * amp * Mathf.Sin(Mathf.PI * t); }
            }
            int fade = rate / 20; for (int i = 0; i < fade; i++) { float t = i / (float)fade; data[i] = data[i] * t + data[n - fade + i] * (1f - t); }   // loop seam
            var c = AudioClip.Create("Pipe trickle", n - fade, 1, rate, false); var d2 = new float[n - fade]; System.Array.Copy(data, d2, n - fade); c.SetData(d2, 0);
            return c;
        }
    }
}
