using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 17 (Dan: "I want the gas pumps to be breakable", "I would like explosions but not really do any damage to anything
    // else"). When a gas pump's panel breaks (FenceChunk.PanelBroken, on a chunk marked PumpChunk), it explodes for show where
    // it stood: a flash, a fireball rolling up, flames for a few seconds, a bang (BreakSounds.Boom, or a drop-in boom*) and a
    // column of smoke that drifts off. Particles, a light and a sound only, HydrantWater's approach: no collider, no force, no
    // damage, so nothing is pushed, broken or slowed by it (the car included; the pump itself costs the little speed any
    // breakable does). Cleared when everything is restored (a race starts); each blast also cleans itself up after 16 s.
    public sealed class PumpBlast : MonoBehaviour
    {
        public Material particleMaterial;
        readonly List<GameObject> blasts = new List<GameObject>();
        public static int Count { get; private set; }

        void OnEnable() { FenceChunk.PanelBroken += OnBroken; FenceChunk.AllRestored += StopAll; }
        void OnDisable() { FenceChunk.PanelBroken -= OnBroken; FenceChunk.AllRestored -= StopAll; }

        void OnBroken(FenceChunk chunk, int panel, Vector3 at)
        {
            if (!chunk.GetComponent<PumpChunk>()) return;
            var go = new GameObject("Pump explosion");
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            // the fireball: one burst, fast at first, rising and darkening as it burns out
            var ball = Make(go.transform, "fireball", 1.2f, 2.2f, 4.8f, new Color(1f, 0.72f, 0.25f, 0.95f));
            var bs = ball.shape; bs.shapeType = ParticleSystemShapeType.Sphere; bs.radius = 0.6f;
            var bm = ball.main; bm.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f); bm.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 8f); bm.gravityModifier = -0.35f; bm.duration = 0.3f; bm.loop = false;
            var be = ball.emission; be.rateOverTime = 0f; be.SetBursts(new[] { new ParticleSystem.Burst(0f, 70) });
            Fade(ball, new Color(1f, 0.95f, 0.6f), new Color(0.75f, 0.18f, 0.05f));
            var bv = ball.limitVelocityOverLifetime; bv.enabled = true; bv.limit = 2.5f; bv.dampen = 0.25f;
            // flames where the pump stood, for a few seconds
            var fire = Make(go.transform, "flames", 0.9f, 0.8f, 1.8f, new Color(1f, 0.6f, 0.15f, 0.9f));
            var fs = fire.shape; fs.shapeType = ParticleSystemShapeType.Circle; fs.radius = 0.7f; fs.rotation = new Vector3(-90f, 0f, 0f);
            var fm = fire.main; fm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f); fm.gravityModifier = -0.2f; fm.duration = 5f; fm.loop = false;
            var fe = fire.emission; fe.rateOverTime = 45f;
            Fade(fire, new Color(1f, 0.85f, 0.4f), new Color(0.6f, 0.12f, 0.04f));
            // the smoke: dark, growing, drifting up and off for longer than the flames
            var smoke = Make(go.transform, "smoke", 5f, 2f, 4.5f, new Color(0.18f, 0.17f, 0.16f, 0.6f));
            var ss = smoke.shape; ss.shapeType = ParticleSystemShapeType.Circle; ss.radius = 1.0f; ss.rotation = new Vector3(-90f, 0f, 0f);
            var sm = smoke.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6.5f); sm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 3.5f); sm.gravityModifier = -0.04f; sm.duration = 9f; sm.loop = false;
            var se = smoke.emission; se.rateOverTime = 22f;
            var sz = smoke.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
            var sc = smoke.colorOverLifetime; sc.enabled = true;
            var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            sc.color = grad;
            // the flash
            var lgo = new GameObject("flash"); lgo.transform.SetParent(go.transform, false); lgo.transform.localPosition = Vector3.up * 1.5f;
            var light = lgo.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1f, 0.6f, 0.25f); light.range = 28f; light.intensity = 9f; light.shadows = LightShadows.None;
            lgo.AddComponent<Flash>();
            // the bang
            var bang = go.AddComponent<AudioSource>();
            bang.clip = BreakSounds.Boom(); bang.spatialBlend = 0.85f; bang.dopplerLevel = 0f; bang.rolloffMode = AudioRolloffMode.Linear;
            bang.minDistance = 20f; bang.maxDistance = 300f; Racer.SoundMix.Track(bang, 1f, Racer.SoundMix.Kind.Effects);   /* Round 27: the Effects volume */ bang.priority = 40; bang.Play();
            Destroy(go, 16f);
            blasts.Add(go); Count++;
        }

        void StopAll()
        {
            foreach (var b in blasts) if (b) Destroy(b);
            blasts.Clear();
        }

        ParticleSystem Make(Transform parent, string name, float life, float sizeMin, float sizeMax, Color colour)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = true; main.startLifetime = life; main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = colour; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 600;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = particleMaterial; r.renderMode = ParticleSystemRenderMode.Billboard; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        static void Fade(ParticleSystem ps, Color hot, Color cool)
        {
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(hot, 0f), new GradientColorKey(cool, 0.6f), new GradientColorKey(new Color(0.15f, 0.14f, 0.13f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        sealed class Flash : MonoBehaviour
        {
            float t; Light l;
            void Awake() { l = GetComponent<Light>(); }
            void Update() { t += Time.deltaTime; l.intensity = Mathf.Lerp(9f, 0f, t / 0.45f); if (t > 0.5f) Destroy(gameObject); }
        }
    }
}
