using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 10, item 9 (Dan: "when you knock over a hydrant, water squirts out like it does in movies"). When a hydrant's
    // panel breaks (FenceChunk.PanelBroken, on a chunk marked HydrantChunk), a tall column of water goes up where it stood,
    // with spray blowing off it and a dark wet patch spreading on the ground under it, until the race restarts (or one is
    // started) and the hydrants are whole again (FenceChunk.AllRestored). Particles only, the fountain's approach
    // (FountainSpray): no collider, so the water never pushes or slows the car.
    public sealed class HydrantWater : MonoBehaviour
    {
        public Material sprayMaterial, groundMaterial;
        public float columnHeight = 9f;
        readonly List<GameObject> jets = new List<GameObject>();
        public static int Running { get; private set; }
        const int RushVoices = 3;

        void OnEnable() { FenceChunk.PanelBroken += OnBroken; FenceChunk.AllRestored += StopAll; }
        void OnDisable() { FenceChunk.PanelBroken -= OnBroken; FenceChunk.AllRestored -= StopAll; }

        void OnBroken(FenceChunk chunk, int panel, Vector3 at)
        {
            if (!chunk.GetComponent<HydrantChunk>()) return;
            var go = new GameObject("Hydrant water");
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            float up = Mathf.Sqrt(2f * 9.81f * columnHeight);
            // the column: a narrow cone of fast drops falling back round it
            var column = Make(go.transform, "column", 520f, 2.6f, 0.25f, 0.5f, new Color(0.88f, 0.94f, 1f, 0.9f));
            var s = column.shape; s.shapeType = ParticleSystemShapeType.Cone; s.angle = 4f; s.radius = 0.08f; s.rotation = new Vector3(-90f, 0f, 0f);
            var m = column.main; m.startSpeed = new ParticleSystem.MinMaxCurve(up * 0.9f, up * 1.02f); m.gravityModifier = 1f;
            // the spray: wider, slower, bigger soft drops, and a mist that drifts
            var spray = Make(go.transform, "spray", 160f, 2.2f, 0.6f, 1.2f, new Color(0.92f, 0.96f, 1f, 0.55f));
            var ss = spray.shape; ss.shapeType = ParticleSystemShapeType.Cone; ss.angle = 18f; ss.radius = 0.15f; ss.rotation = new Vector3(-90f, 0f, 0f);
            var sm = spray.main; sm.startSpeed = new ParticleSystem.MinMaxCurve(up * 0.45f, up * 0.75f); sm.gravityModifier = 1f;
            var mist = Make(go.transform, "mist", 50f, 2.0f, 1.4f, 2.4f, new Color(1f, 1f, 1f, 0.25f));
            var ms = mist.shape; ms.shapeType = ParticleSystemShapeType.Circle; ms.radius = 2.2f; ms.rotation = new Vector3(-90f, 0f, 0f);
            var mm = mist.main; mm.startSpeed = 0.4f; mm.gravityModifier = -0.03f;
            // the wet patch: a dark disc on the ground, growing over a few seconds
            var patch = new GameObject("wet patch"); patch.transform.SetParent(go.transform, false);
            patch.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            patch.AddComponent<MeshFilter>().sharedMesh = Disc();
            var r = patch.AddComponent<MeshRenderer>(); r.sharedMaterial = groundMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            patch.AddComponent<Grow>();
            // Round 15, item 3: the rush of water while it runs, in 3D at the jet; at most RushVoices at once (the newest)
            var rush = go.AddComponent<AudioSource>();
            rush.clip = BreakSounds.Rush(); rush.loop = true; rush.spatialBlend = 1f; rush.dopplerLevel = 0f; rush.rolloffMode = AudioRolloffMode.Linear;
            rush.minDistance = 6f; rush.maxDistance = 60f; Racer.SoundMix.Track(rush, 0.45f, Racer.SoundMix.Kind.Effects); rush.priority = 90;   // Round 27: the Effects volume
            rush.time = Random.Range(0f, rush.clip.length * 0.9f); rush.Play();
            int playing = 0;
            for (int i = jets.Count - 1; i >= 0; i--) { var a = jets[i] ? jets[i].GetComponent<AudioSource>() : null; if (a && a.isPlaying && ++playing >= RushVoices) a.Stop(); }
            jets.Add(go); Running = jets.Count;
        }

        void StopAll()
        {
            foreach (var j in jets) if (j) Destroy(j);
            jets.Clear(); Running = 0;
        }

        ParticleSystem Make(Transform parent, string name, float rate, float life, float sizeMin, float sizeMax, Color colour)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = true; main.startLifetime = life; main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = colour; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 2000;
            var em = ps.emission; em.rateOverTime = rate;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = sprayMaterial; r.renderMode = ParticleSystemRenderMode.Billboard; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        static Mesh disc;
        static Mesh Disc()
        {
            if (disc) return disc;
            const int N = 20; var v = new Vector3[N + 1]; var c = new Color[N + 1]; var t = new int[N * 3];
            var wet = new Color(0.16f, 0.17f, 0.18f);
            v[0] = Vector3.zero; c[0] = wet;
            for (int i = 0; i < N; i++)
            {
                float a = Mathf.PI * 2f * i / N, rr = 1f + 0.18f * Mathf.Sin(i * 2.7f);
                v[i + 1] = new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr); c[i + 1] = wet;
                t[i * 3] = 0; t[i * 3 + 1] = (i + 1) % N + 1; t[i * 3 + 2] = i + 1;
            }
            disc = new Mesh { name = "wet patch", vertices = v, colors = c, triangles = t };
            disc.RecalculateNormals(); disc.RecalculateBounds();
            return disc;
        }

        sealed class Grow : MonoBehaviour
        {
            float t;
            void Update() { t += Time.deltaTime; float r = Mathf.Lerp(0.4f, 3.2f, Mathf.Clamp01(t / 6f)); transform.localScale = new Vector3(r, 1f, r); }
        }
    }
}
