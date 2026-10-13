using UnityEngine;

namespace ChastainRush
{
    // Round 23 follow-ups, the hub (Dan: "a main hub that is a big open chasm with water flowing in from all the sources"): a pipe's
    // water falling off a ledge (MapBuilder.NetworkHub.cs builds the sheets). Look and sound only, it never pushes or slows:
    //   the sheet   the water's texture runs down it, fast
    //   the mist    spray at the foot of each fall
    //   the roar    a loop of rushing water made here, from the foot nearest the listener (heard across the hub)
    public sealed class NetworkWaterfall : MonoBehaviour
    {
        public Material material, splash;
        public Vector3[] bottoms;
        AudioSource roar; ParticleSystem mist; float emit;
        static AudioClip clip;

        void Start()
        {
            var a = new GameObject("Waterfall roar"); a.transform.SetParent(transform, false);
            roar = a.AddComponent<AudioSource>(); roar.clip = clip ? clip : (clip = Roar());
            roar.loop = true; roar.spatialBlend = 1f; roar.rolloffMode = AudioRolloffMode.Linear; roar.minDistance = 4f; roar.maxDistance = 70f; roar.volume = 0.55f; roar.dopplerLevel = 0f; roar.Play();
            var m = new GameObject("Waterfall mist"); m.transform.SetParent(transform, false);
            mist = m.AddComponent<ParticleSystem>(); mist.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = mist.main; main.loop = false; main.playOnAwake = false; main.startLifetime = 1.4f; main.startSize = 0.6f; main.gravityModifier = -0.05f; main.maxParticles = 300; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = mist.emission; em.enabled = false; var sh = mist.shape; sh.enabled = false;
            var r = m.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = splash; r.renderMode = ParticleSystemRenderMode.Billboard;
            mist.Play();
        }

        void OnEnable() { if (roar && !roar.isPlaying) roar.Play(); }
        void OnDisable() { if (roar) roar.Stop(); }

        void Update()
        {
            if (material) material.SetTextureOffset("_BaseMap", new Vector2(0f, Time.time * 1.6f));
            if (bottoms == null || bottoms.Length == 0) return;
            var listener = Camera.main ? Camera.main.transform.position : transform.position;
            Vector3 near = bottoms[0]; foreach (var b in bottoms) if ((b - listener).sqrMagnitude < (near - listener).sqrMagnitude) near = b;
            if (roar) roar.transform.position = near;
            emit += Time.deltaTime * 30f;
            while (emit >= 1f && mist)
            {
                emit -= 1f; var b = bottoms[Random.Range(0, bottoms.Length)];
                mist.Emit(new ParticleSystem.EmitParams { position = b + new Vector3(Random.Range(-1.2f, 1.2f), 0.2f, Random.Range(-1.2f, 1.2f)), velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(0.8f, 2.4f), Random.Range(-1f, 1f)), startSize = Random.Range(0.3f, 0.9f), startLifetime = Random.Range(0.8f, 1.6f) }, 1);
            }
        }

        // 5 s of rushing water: white noise through two low-passes, a slow surge
        static AudioClip Roar()
        {
            const int rate = 44100; int n = rate * 5; var d = new float[n]; var rnd = new System.Random(2323); float a = 0f, b = 0f;
            for (int i = 0; i < n; i++)
            {
                float w = (float)rnd.NextDouble() * 2f - 1f; a += (w - a) * 0.35f; b += (a - b) * 0.25f;
                float surge = 0.8f + 0.2f * Mathf.Sin(i * 2f * Mathf.PI / n * 2f) * Mathf.Sin(i * 2f * Mathf.PI / n * 7f + 0.5f);
                d[i] = (a * 0.55f + b * 0.6f) * surge;
            }
            int fade = rate / 10; for (int i = 0; i < fade; i++) { float t = i / (float)fade; d[i] = d[i] * t + d[n - fade + i] * (1f - t); }
            var c = AudioClip.Create("Waterfall roar", n - fade, 1, rate, false); var d2 = new float[n - fade]; System.Array.Copy(d, d2, n - fade); c.SetData(d2, 0); return c;
        }
    }
}
