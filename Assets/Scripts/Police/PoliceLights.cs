using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): Racer's PoliceLights as it is (the lenses are the patrol car's sirenred / sirenblue slots, VehicleVisual); the
    // siren's volume is the Effects volume (SoundMix).
    // 0.94 Part C: the patrol car's light bar and siren. While Siren is on the red and blue lenses (Tools/Blender/police.py
    // slots sirenred / sirenblue) flash in turn, each with a coloured light over the roof (seen at night), and the siren wails.
    // The siren is one sound for both players (split-screen has one listener): louder the nearer the cop is to the runner
    // (Target), full within 15 m, quiet beyond 250 m.
    public sealed class PoliceLights : MonoBehaviour
    {
        public bool Siren;
        public bool Silent; // 0.96: lights only (the AI cops further from the runner)
        public Transform Target;
        public float Volume = .55f;
        Renderer[] red, blue; Light redLight, blueLight; AudioSource source; MaterialPropertyBlock block;
        static AudioClip wail;
        public static AudioClip Wail
        {
            get
            {
                if (wail) return wail;
                // a two-second rising and falling wail: a slightly square tone sweeping 650 - 1350 Hz and back
                int rate = 44100, n = rate * 2; var data = new float[n]; double phase = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / n; float f = Mathf.Lerp(650, 1350, .5f - .5f * Mathf.Cos(t * Mathf.PI * 2));
                    phase += 2 * Mathf.PI * f / rate; float s = Mathf.Sin((float)phase); data[i] = .45f * Mathf.Clamp(s * 1.6f, -1, 1);
                }
                wail = AudioClip.Create("Siren wail", n, 1, rate, false); wail.SetData(data, 0); return wail;
            }
        }
        public void Rebuild()
        {
            var list = GetComponentsInChildren<Renderer>(true);
            red = System.Array.FindAll(list, r => r.name.EndsWith("__sirenred")); blue = System.Array.FindAll(list, r => r.name.EndsWith("__sirenblue"));
            if (!redLight) redLight = MakeLight("Light bar red light", new Color(1, .1f, .08f), -.36f);
            if (!blueLight) blueLight = MakeLight("Light bar blue light", new Color(.15f, .3f, 1), .36f);
        }
        Light MakeLight(string name, Color c, float x)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.localPosition = new Vector3(x, 1.4f, -.3f);
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = 14; l.intensity = 0; l.shadows = LightShadows.None; return l;
        }
        void Start()
        {
            Rebuild(); block = new MaterialPropertyBlock();
            source = gameObject.AddComponent<AudioSource>(); source.clip = Wail; source.loop = true; source.spatialBlend = 0; source.playOnAwake = false; source.volume = 0; source.priority = 100;
        }
        void Set(Renderer[] rs, Color emission)
        {
            if (rs == null) return;
            foreach (var r in rs) { if (!r) continue; r.GetPropertyBlock(block); block.SetColor("_EmissionColor", emission); r.SetPropertyBlock(block); }
        }
        void Update()
        {
            if (red == null || (red.Length == 0 && Time.frameCount % 30 == 0)) Rebuild();
            // the pattern: red, blue, red, blue (quick double flashes), 1.2 s round
            float t = Mathf.Repeat(Time.time, 1.2f); bool r = Siren && (t < .12f || (t > .2f && t < .32f)), b = Siren && ((t > .6f && t < .72f) || (t > .8f && t < .92f));
            Set(red, r ? new Color(6, .4f, .3f) : Color.black); Set(blue, b ? new Color(.4f, .9f, 7) : Color.black);
            if (redLight) redLight.intensity = r ? 3.5f : 0; if (blueLight) blueLight.intensity = b ? 3.5f : 0;
            if (!source) return;
            float near = Target ? 1 - Mathf.InverseLerp(15, 250, Vector3.Distance(transform.position, Target.position)) : 1;
            float want = Siren && !Silent && !AudioListener.pause ? Volume * Mathf.Lerp(.25f, 1, near) * SoundMix.Gain(SoundMix.Kind.Effects) : 0;
            source.volume = Mathf.MoveTowards(source.volume, want, Time.unscaledDeltaTime * 2);
            if (source.volume > 0 && !source.isPlaying) source.Play(); else if (source.volume <= 0 && source.isPlaying) source.Stop();
        }
        void OnDisable() { Set(red, Color.black); Set(blue, Color.black); if (source) source.Stop(); }
    }
}
