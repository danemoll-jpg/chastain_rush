using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 27 (C1): the volume settings applied to this game's sounds, as Racer does it (each setting a plain multiplier
    // its sounds read; Racer has no AudioMixer). Master is AudioListener.volume, Vehicle is VehicleAudio's and OtherEngines' own, Music is
    // the radio's. Here: Ambience (Racer's: the world's own sounds: geese, water, the pipes' streams and hum, splashes) and Effects
    // (Chastain Rush, Dan: "the engines (own and AI), effects and master": breaking things, the hydrants' jets, the pipe express, the
    // trampoline, a pump blowing up). Racer's "Race / UI" volume has nothing to drive here (no race or menu tones). A sound set once at
    // its start registers with Track and is set again whenever the settings change.
    public static class SoundMix
    {
        public enum Kind { Effects, Ambience }

        public static float Gain(Kind k) => k == Kind.Effects ? GameSettings.Settings.effects : GameSettings.Settings.ambience;

        static readonly List<(AudioSource source, float volume, Kind kind)> tracked = new();
        static bool hooked;

        public static void Track(AudioSource source, float volume, Kind kind)
        {
            if (!source) return;
            source.volume = volume * Gain(kind);
            tracked.Add((source, volume, kind));
            if (!hooked) { hooked = true; GameSettings.Changed += Apply; }
        }

        static void Apply()
        {
            tracked.RemoveAll(t => !t.source);
            foreach (var t in tracked) t.source.volume = t.volume * Gain(t.kind);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { tracked.Clear(); if (hooked) GameSettings.Changed -= Apply; hooked = false; }
    }
}
