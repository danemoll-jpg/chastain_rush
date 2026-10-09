using UnityEngine;

namespace ChastainRush
{
    // Round 7: the pond's fountain. A ring of fine jets thrown up and out from a small floating head (an arc fan, as in the
    // photos), plus a low spray skirt on the water. Built in Awake from two particle systems so nothing needs authoring;
    // the material is the plain URP particle shader (set by the map builder so a build keeps it).
    public sealed class FountainSpray : MonoBehaviour
    {
        public Material material;
        public float waterY;
        public float height = 6.0f, spread = 5.5f;

        ParticleSystem jets, mist;
        public int Alive => (jets ? jets.particleCount : 0) + (mist ? mist.particleCount : 0);

        void Awake()
        {
            transform.position = new Vector3(transform.position.x, waterY, transform.position.z);
            jets = Make("jets", 700f, 2.2f, 0.22f, 0.38f);
            var s = jets.shape; s.shapeType = ParticleSystemShapeType.Cone; s.angle = 24f; s.radius = 0.12f; s.rotation = new Vector3(-90f, 0f, 0f);
            var m = jets.main; m.startSpeed = new ParticleSystem.MinMaxCurve(Mathf.Sqrt(2f * 9.81f * height) * 0.92f, Mathf.Sqrt(2f * 9.81f * height) * 1.05f); m.gravityModifier = 1f;
            mist = Make("mist", 90f, 1.4f, 1.0f, 1.8f);
            var ms = mist.shape; ms.shapeType = ParticleSystemShapeType.Circle; ms.radius = spread * 0.6f; ms.rotation = new Vector3(-90f, 0f, 0f);
            var mm = mist.main; mm.startSpeed = 0.1f; mm.gravityModifier = -0.02f;
            var c = mist.colorOverLifetime; c.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.22f, 0.25f), new GradientAlphaKey(0f, 1f) });
            c.color = g;
        }

        ParticleSystem Make(string name, float rate, float life, float sizeMin, float sizeMax)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = true; main.startLifetime = life; main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new Color(0.92f, 0.96f, 1f, 0.85f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 2500;
            var em = ps.emission; em.rateOverTime = rate;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material; r.renderMode = ParticleSystemRenderMode.Billboard; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();
            return ps;
        }
    }
}
