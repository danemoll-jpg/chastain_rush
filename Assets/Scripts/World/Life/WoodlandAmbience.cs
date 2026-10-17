using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 38 (Part E): Racer's WoodlandAmbience (two voices for the whole map: a soft wind in the leaves, and now and then a bird
    // somewhere in the trees round the listener), ported. Racer weighs it by a band of its map (woods to the south, the commercial strip north); here
    // the map build bakes the tree cover in 40 m cells (MapBuilder.Life.cs, from the trees it planted) and the weight is the cover round the listener,
    // eased as Racer eases it. Racer's two sounds were written by its editor (Phase6Environment's soft-leaves loop and three bird chirps); here
    // the same kind of sound is made at start (a filtered noise loop with a cross-fade, chirp pulses). Quiet underground (the network has its own),
    // birds by day only, none in rain or snow (the rain has its own sound).
    [DisallowMultipleComponent]
    public sealed class WoodlandAmbience : MonoBehaviour
    {
        public byte[] cover; public Vector2 origin; public float cell = 40f; public int width, height;
        AudioSource windSource, birdSource; AudioClip wind; AudioClip[] birds;
        float nextBird, windLevel, nextUnder; bool under; readonly System.Random random = new(6018);
        public float WoodlandWeight { get; private set; }
        float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        public float CoverAt(Vector3 p)
        {
            if (cover == null || width < 2 || height < 2) return 0f;
            float fx = (p.x - origin.x) / cell - .5f, fz = (p.z - origin.y) / cell - .5f; int x = Mathf.Clamp(Mathf.FloorToInt(fx), 0, width - 2), z = Mathf.Clamp(Mathf.FloorToInt(fz), 0, height - 2);
            float a = Mathf.Clamp01(fx - x), b = Mathf.Clamp01(fz - z);
            float c00 = cover[z * width + x], c10 = cover[z * width + x + 1], c01 = cover[(z + 1) * width + x], c11 = cover[(z + 1) * width + x + 1];
            return Mathf.Lerp(Mathf.Lerp(c00, c10, a), Mathf.Lerp(c01, c11, a), b) / 255f;
        }
        void Awake()
        {
            wind = Leaves(); birds = new[] { Chirps(0), Chirps(1), Chirps(2) };
            windSource = gameObject.AddComponent<AudioSource>(); windSource.clip = wind; windSource.loop = true; windSource.spatialBlend = 0; windSource.volume = 0; windSource.playOnAwake = false; windSource.priority = 180; windSource.Play();
            var bird = new GameObject("Occasional woodland bird"); bird.transform.SetParent(transform, false);
            birdSource = bird.AddComponent<AudioSource>(); birdSource.spatialBlend = 1; birdSource.rolloffMode = AudioRolloffMode.Linear; birdSource.minDistance = 8; birdSource.maxDistance = 65; birdSource.dopplerLevel = 0; birdSource.playOnAwake = false; birdSource.priority = 190;
            nextBird = Time.time + Range(8, 15);
        }
        void OnDestroy() { if (wind) Destroy(wind); if (birds != null) foreach (var b in birds) if (b) Destroy(b); }
        void Update()
        {
            var listener = Camera.main ? Camera.main.transform : null; if (!listener) return;
            var pr = WorldLook.Current ? WorldLook.Current.Preset : null; bool wet = pr != null && (pr.rain > .5f || pr.snowfall > .5f); float hour = WorldLook.Current ? WorldLook.Current.Hour : 12f;
            if (Time.unscaledTime >= nextUnder) { nextUnder = Time.unscaledTime + .25f; under = DrainPipe.Inside(listener.position, out _) != null; }   // (a few times a second)
            bool underground = under;
            float target = underground ? 0f : Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.08f, .55f, CoverAt(listener.position)));
            WoodlandWeight = Mathf.MoveTowards(WoodlandWeight, target, Time.deltaTime * .12f);
            float volume = GameSettings.Settings.ambience * (underground ? 0f : 1f);
            windLevel = Mathf.MoveTowards(windLevel, Mathf.Lerp(.085f, .15f, WoodlandWeight), Time.deltaTime * .025f);
            windSource.volume = windLevel * volume;
            birdSource.volume = Mathf.Lerp(.025f, .13f, WoodlandWeight) * volume;
            if (Time.time < nextBird || birdSource.isPlaying) return;
            nextBird = Time.time + Range(12, 26);
            if (wet || hour < 6f || hour >= 20.5f || underground) return;
            float angle = Range(0, Mathf.PI * 2), radius = Range(18, 32);
            birdSource.transform.position = listener.position + new Vector3(Mathf.Cos(angle) * radius, Range(5, 10), Mathf.Sin(angle) * radius);
            birdSource.clip = birds[random.Next(birds.Length)]; birdSource.pitch = Range(.88f, 1.12f); birdSource.Play();
        }
        // a soft-leaves loop: low-passed noise with a slow swell, its ends cross-faded so it loops without a seam (Racer's Soft leaves.wav kind)
        static AudioClip Leaves()
        {
            const int rate = 22050; int n = rate * 6, fade = rate / 2; var raw = new float[n + fade]; var rng = new System.Random(11); float lp = 0, lp2 = 0;
            for (int i = 0; i < raw.Length; i++) { float w = (float)(rng.NextDouble() * 2 - 1); lp += (w - lp) * .06f; lp2 += (lp - lp2) * .12f; float t = i / (float)rate; raw[i] = lp2 * (2.2f + .9f * Mathf.Sin(t * .9f) + .5f * Mathf.Sin(t * 2.3f + 1)); }
            var data = new float[n]; for (int i = 0; i < n; i++) data[i] = raw[i]; for (int i = 0; i < fade; i++) { float k = i / (float)fade; data[i] = raw[i] * k + raw[n + i] * (1 - k); }
            var clip = AudioClip.Create("Soft leaves", n, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        // a few chirp pulses (Racer's Bird 0/1/2.wav kind)
        static AudioClip Chirps(int seed)
        {
            const int rate = 22050; float seconds = .9f + seed * .2f; var data = new float[(int)(rate * seconds)]; var rng = new System.Random(40 + seed); int notes = 3 + seed;
            for (int k = 0; k < notes; k++)
            {
                float on = .05f + k * (seconds - .2f) / notes, len = .07f + (float)rng.NextDouble() * .06f, f0 = 2600 + seed * 500 + (float)rng.NextDouble() * 800, f1 = f0 + (rng.NextDouble() < .5 ? 900 : -700); float phase = 0;
                for (int i = (int)(on * rate); i < Mathf.Min(data.Length, (int)((on + len) * rate)); i++) { float u = (i / (float)rate - on) / len; phase += 2 * Mathf.PI * Mathf.Lerp(f0, f1, u) / rate; data[i] += .35f * Mathf.Sin(Mathf.PI * u) * Mathf.Sin(phase); }
            }
            var clip = AudioClip.Create("Woodland bird " + seed, data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
    }
}
