using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 15, item 3: a sound when something breaks. Racer's SmashAudio, copied: original Foley made in code (no files),
    // seeded so it is the same every run, a few bounded 3D voices shared by every prop, loudness from the hit speed
    // (0.24 at 1 m/s to 0.62 at 32 m/s, times Racer's vehicle volume 0.75), nothing past 75 m from the camera.
    // Changes for this game (Chastain Rush):
    //   - Racer plays only while racing (RaceFlow); here also in free roam.
    //   - what breaks here: wooden fences (Wood), the pool's iron fence (Iron = Racer's ChainLink), a mailbox (Racer's
    //     Mailbox clang with Wood for the post), a street lamp (Racer's Sign metal with Glass), a hydrant (Racer's Mailbox
    //     clang pitched down, then HydrantRush, a loop of rushing water while the jet runs), windows and the sliding door (Glass).
    //   - a hit of two layers (mailbox, lamp) takes two voices at once; six voices instead of Racer's four.
    //   - a drop-in file (SoundDropIns, Sounds\Breaks\wood*, metal*, glass*, water*) replaces the made sound of that kind.
    public sealed class BreakSounds : MonoBehaviour
    {
        public enum Kind { Wood, Iron, Mailbox, Lamp, Glass, Hydrant }
        enum Surface { Wood, ChainLink, Mailbox, Sign, Glass }   // Racer's SmashAudio.Surface
        static BreakSounds instance;
        static readonly float[] WoodDelays = { .055f, .12f, .20f, .29f }, MetalDelays = { .08f, .17f, .29f, .43f, .61f };
        const int Voices = 6;
        readonly AudioSource[] voices = new AudioSource[Voices];
        readonly float[] gains = new float[Voices];
        static AudioClip[,] shared;
        static AudioClip rush;
        float next;
        public int Events { get; private set; }
        public static int Played => instance ? instance.Events : 0;

        public static void Play(Vector3 at, float speed, Kind kind)
        {
            if (!instance) instance = new GameObject("Break sounds").AddComponent<BreakSounds>();
            instance.Emit(at, speed, kind);
        }

        void Awake()
        {
            instance = this;
            for (int i = 0; i < Voices; i++)
            {
                var child = new GameObject("Break voice " + i); child.transform.SetParent(transform);
                var v = voices[i] = child.AddComponent<AudioSource>();
                v.playOnAwake = false; v.spatialBlend = .8f; v.dopplerLevel = 0; v.minDistance = 14; v.maxDistance = 75; v.rolloffMode = AudioRolloffMode.Linear; v.priority = 80;
            }
            if (shared == null || !shared[0, 0]) { shared = new AudioClip[5, 3]; for (int i = 0; i < 5; i++) for (int j = 0; j < 3; j++) { shared[i, j] = Synthesize((Surface)i, j); shared[i, j].hideFlags = HideFlags.DontUnloadUnusedAsset; } }
        }

        // Racer's SmashAudio.Synthesize, unchanged
        static AudioClip Synthesize(Surface kind, int variant)
        {
            const int rate = 22050; float duration = kind == Surface.Wood ? .55f : 1.0f;
            var data = new float[(int)(rate * duration)]; var random = new System.Random(3500 + (int)kind * 37 + variant * 103);
            float low = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, n = (float)random.NextDouble() * 2 - 1; low = Mathf.Lerp(low, n, .17f);
                float sample;
                if (kind == Surface.Wood)
                {
                    sample = (n * .65f + low * .35f) * Mathf.Exp(-t * 24) + Mathf.Sin(t * (480 + variant * 70)) * Mathf.Exp(-t * 45) * .35f;
                    foreach (float delay in WoodDelays) if (t > delay) sample += n * .16f * Mathf.Exp(-(t - delay) * 55);
                }
                else if (kind == Surface.Glass)
                {
                    sample = n * .72f * Mathf.Exp(-t * 38);
                    for (int partial = 1; partial <= 7; partial++) sample += Mathf.Sin(t * 2 * Mathf.PI * (1400 + variant * 117) * Mathf.Sqrt(partial)) * .09f * Mathf.Exp(-t * (8 + partial));
                    foreach (float delay in MetalDelays) if (t > delay) sample += n * .23f * Mathf.Exp(-(t - delay) * 60);
                }
                else
                {
                    float baseHz = kind == Surface.Mailbox ? 330 : kind == Surface.Sign ? 520 : 780;
                    sample = n * .36f * Mathf.Exp(-t * 28);
                    for (int partial = 1; partial <= 5; partial++) sample += Mathf.Sin(t * 2 * Mathf.PI * (baseHz + variant * 43) * partial * 1.13f) * .14f / partial * Mathf.Exp(-t * (5 + partial * 2));
                    foreach (float delay in MetalDelays) if (t > delay) sample += (n * .18f + low * .1f) * Mathf.Exp(-(t - delay) * 32);
                }
                data[i] = Mathf.Clamp(sample, -.85f, .85f) * Mathf.Min(1, i / 24f);
            }
            var clip = AudioClip.Create("Original " + kind + " " + variant, data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }

        // Chastain Rush: the hydrant's water, a loop of filtered noise (the water noise of Racer's AmbientLife.Sound, longer and
        // made seamless by a cross-fade of its two halves)
        public static AudioClip Rush()
        {
            var drop = DropIn("water"); if (drop) return drop;
            if (rush) return rush;
            const int rate = 22050; int n = rate * 2; var raw = new float[n * 2]; var rng = new System.Random(81); float low = 0, lower = 0;
            for (int i = 0; i < raw.Length; i++) { float w = (float)rng.NextDouble() * 2 - 1; low = Mathf.Lerp(low, w, .35f); lower = Mathf.Lerp(lower, w, .06f); raw[i] = low * .5f + lower * .9f + w * .12f; }
            var data = new float[n];
            for (int i = 0; i < n; i++) { float k = i / (float)n; data[i] = Mathf.Clamp(raw[i] * k + raw[i + n] * (1 - k), -.9f, .9f) * .8f; }
            rush = AudioClip.Create("Hydrant rush", n, 1, rate, false); rush.SetData(data, 0); rush.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return rush;
        }

        static AudioClip DropIn(string kind)
        {
            var d = SoundDropIns.Instance;
            if (d == null || !d.Breaks.TryGetValue(kind, out List<AudioClip> l) || l.Count == 0) return null;
            return l[Random.Range(0, l.Count)];
        }

        AudioClip ClipFor(Surface s)
        {
            var drop = DropIn(s == Surface.Wood ? "wood" : s == Surface.Glass ? "glass" : "metal");
            return drop ? drop : shared[(int)s, Events % 3];
        }

        void Emit(Vector3 at, float speed, Kind kind)
        {
            if (Time.time < next) return;
            var listener = Camera.main;
            if (listener && Vector3.Distance(at, listener.transform.position) > 75) return;
            next = Time.time + .075f; Events++;
            float gain = Mathf.Lerp(.24f, .62f, Mathf.InverseLerp(1, 32, speed)) * .75f;
            switch (kind)
            {
                case Kind.Wood: Voice(at, Surface.Wood, gain, 1f); break;
                case Kind.Iron: Voice(at, Surface.ChainLink, gain, 1f); break;
                case Kind.Mailbox: Voice(at, Surface.Mailbox, gain, 1f); Voice(at, Surface.Wood, gain * .6f, 1.1f); break;
                case Kind.Lamp: Voice(at, Surface.Sign, gain, 1f); Voice(at, Surface.Glass, gain * .8f, 1f); break;
                case Kind.Glass: Voice(at, Surface.Glass, gain, 1f); break;
                case Kind.Hydrant: Voice(at, Surface.Mailbox, gain * 1.1f, .72f); break;
            }
        }

        void Voice(Vector3 at, Surface s, float gain, float pitch)
        {
            int slot = System.Array.FindIndex(voices, v => !v.isPlaying);
            if (slot < 0) { slot = Events % voices.Length; voices[slot].Stop(); }   // the bound: the oldest-numbered voice gives way
            var source = voices[slot]; source.transform.position = at;
            source.clip = ClipFor(s); source.pitch = pitch * (.94f + (Events % 5) * .035f);
            gains[slot] = gain; source.volume = gain; source.Play();
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
