using UnityEngine;

namespace ChastainRush
{
    // Round 78 (A): the train's sounds, made in code like the geese's honk and Racer's TurkeyVoice (no recordings): the crossing bell (a struck bell twice
    // a second), the locomotive's horn (a three-note chord; the crossing pattern long, long, short, long) and the engines' rumble (a loop).
    public static class RailSounds
    {
        const int Rate = 44100;
        static AudioClip bell, horn, rumble;

        public static AudioClip Bell => bell ??= Make("Crossing bell", 1f, t =>
        {
            float u = Mathf.Repeat(t, .5f);   // struck twice a second
            float env = Mathf.Exp(-u * 9f);
            return env * (.55f * Mathf.Sin(2f * Mathf.PI * 1420f * t) + .3f * Mathf.Sin(2f * Mathf.PI * 2870f * t) + .15f * Mathf.Sin(2f * Mathf.PI * 4310f * t));
        });

        // long 1.6 s, long 1.6 s, short .7 s, long 2.6 s (gaps .35 s)
        static readonly float[] HornOn = { 0f, 1.6f, 1.95f, 3.55f, 3.9f, 4.6f, 4.95f, 7.55f };
        public static float HornLength => 7.7f;
        public static AudioClip Horn => horn ??= Make("Train horn", HornLength, t =>
        {
            float g = 0f;
            for (int k = 0; k < HornOn.Length; k += 2)
                if (t >= HornOn[k] && t < HornOn[k + 1]) g = Mathf.Min(1f, Mathf.Min((t - HornOn[k]) / .06f, (HornOn[k + 1] - t) / .1f));
            if (g <= 0f) return 0f;
            float v = 0f;
            foreach (float f in new[] { 311.1f, 370f, 493.9f })
                for (int h = 1; h <= 4; h++) v += Mathf.Sin(2f * Mathf.PI * f * h * t + h) / (h * 1.4f);
            return g * Mathf.Clamp(v * .22f, -1f, 1f);
        });

        public static AudioClip Rumble => rumble ??= Make("Train rumble", 2f, t =>
        {
            // two diesels idling up: 31 Hz firing with harmonics, a little roughness (whole periods in the 2 s loop)
            float v = .5f * Mathf.Sin(2f * Mathf.PI * 31f * t) + .3f * Mathf.Sin(2f * Mathf.PI * 62f * t + .7f) + .18f * Mathf.Sin(2f * Mathf.PI * 93f * t + 1.3f) + .1f * Mathf.Sin(2f * Mathf.PI * 186f * t);
            v *= .75f + .25f * Mathf.Sin(2f * Mathf.PI * 4f * t);
            return v * .45f + Noise(t) * .05f;
        });

        static float Noise(float t) { int i = (int)(t * Rate); i = (i << 13) ^ i; return 1f - ((i * (i * i * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f; }

        static AudioClip Make(string name, float seconds, System.Func<float, float> f)
        {
            int n = Mathf.RoundToInt(seconds * Rate); var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = f(i / (float)Rate);
            var c = AudioClip.Create(name, n, 1, Rate, false); c.SetData(d, 0); return c;
        }
    }
}
