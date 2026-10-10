using System;
using UnityEngine;

namespace Racer
{
    // Original synthesis for Racer, dedicated to CC0. No sampled recordings.
    public static class VehicleSoundSynthesis
    {
        public static AudioClip Create(string name, int kind)
        {
            const int rate = 44100;
            bool impact = kind >= 5;
            int count = impact ? rate / 2 : rate * 4;
            var samples = new float[count];
            var random = new System.Random(20260918 + kind);
            float low = 0, slower = 0;
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / rate;
                float noise = (float)(random.NextDouble() * 2 - 1);
                low += (noise - low) * (kind == 2 ? .035f : .16f);
                slower += (low - slower) * .025f;
                double phase = 2 * Math.PI * 48 * t + .10 * Math.Sin(2 * Math.PI * 7 * t);
                float combustion = (float)(.40 * Math.Sin(phase) + .22 * Math.Sin(phase * 2 + .4)
                    + .13 * Math.Sin(phase * 3) + .07 * Math.Sin(phase * 5 + .8));
                samples[i] = kind switch
                {
                    0 => combustion * (float)(.88 + .12 * Math.Sin(2 * Math.PI * 12 * t)) + slower * .35f,
                    1 => combustion * .6f + (float)(.12 * Math.Sin(phase * 4) + .08 * Math.Sin(phase * 7)) + low * .3f,
                    2 => slower * 2.2f,
                    3 => low * (float)(.9 + .3 * Math.Sin(2 * Math.PI * 19 * t)),
                    4 => (noise - low) * .12f + (float)(.10 * Math.Sin(2 * Math.PI * 730 * t + 2 * Math.Sin(2 * Math.PI * 23 * t))) + low * .25f,
                    _ => (low * .7f + (float)Math.Sin(2 * Math.PI * (85 * t - 35 * t * t)) * .45f)
                        * (float)(Math.Exp(-t * (kind == 6 ? 16 : 11)) * Math.Min(1, t / .005))
                };
            }
            if (!impact)
            {
                // Equal-length overlap, then trim: continuous noise and harmonic loop junction.
                const int overlap = 2205;
                int length = count - overlap;
                for (int i = 0; i < overlap; i++)
                {
                    float blend = i / (float)(overlap - 1);
                    samples[i] = Mathf.Lerp(samples[length + i], samples[i], blend);
                }
                Array.Resize(ref samples, length);
                // Join the final 0.7 ms to the first sample without a hard endpoint step.
                for (int i = length - 32; i < length; i++)
                    samples[i] = Mathf.Lerp(samples[i], samples[0], Mathf.SmoothStep(0, 1, (i - length + 32) / 31f));
            }
            else for (int i = count - 441; i < count; i++) samples[i] *= (count - 1 - i) / 440f;
            float peak = .001f;
            foreach (float sample in samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
            for (int i = 0; i < samples.Length; i++) samples[i] *= .65f / peak;
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
