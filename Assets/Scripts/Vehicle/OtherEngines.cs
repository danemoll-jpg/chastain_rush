using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    // 0.113 Part E: the other vehicles' engines (rivals, traffic, Getaway cops). VehicleAudio's engine model (the same two synthesized
    // loops, rev estimate from that car's own speed and pedal, the profile's pitch, so bikes / ATVs / the mower sound like themselves),
    // played from where the car is. Only the nearest few play: 4 rivals / cops and 2 traffic, through a pool of 6 voices that fade in
    // and out when the set changes. The player's own engine (VehicleAudio, 2D) is not touched; any car with a VehicleAudio is skipped.
    // Follows the Vehicle volume; silent outside the race like the player's engine; the listener pause stops them in the pause menu.
    // Chastain Rush, Round 27 (B2): ported. Edits: the cars are RaceController's AI entrants (no RaceFlow / RaceDirector, traffic, cops or
    // split-screen here, so the 2 traffic voices are never used); it plays from the countdown until the AI stop (RaceController.Stage
    // Countdown, Racing, Finished); the Vehicle volume is GameSettings'; no weather ducking. Round 15 gave each AI car a whole
    // VehicleAudio of its own (6 looping voices each, always playing); those are gone, as Racer has it.
    public sealed class OtherEngines : MonoBehaviour
    {
        public const int RivalVoices = 4, TrafficVoices = 2;
        public const float MinDistance = 7, Audible = 140, Quieter = .7f;
        sealed class Voice
        {
            public Transform at; public AudioSource idle, load;
            public ArcadeVehicle car, next; public float level, rpm = .85f, pedal;
        }
        readonly List<Voice> voices = new();
        readonly List<ArcadeVehicle> candidates = new();
        readonly HashSet<ArcadeVehicle> wanted = new();
        float nextPick, nextLog;
        public static OtherEngines Current { get; private set; }
        public static bool Disabled; // checks only (the frame time without them); never set in play
        public int Playing { get; private set; }
        public string Info { get; private set; } = ""; // checks: what the last pick saw
        public IEnumerable<ArcadeVehicle> Heard { get { foreach (var v in voices) if (v.car && v.level > .01f) yield return v.car; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Current) return;
            var g = new GameObject("Other vehicles' engines"); DontDestroyOnLoad(g); Current = g.AddComponent<OtherEngines>();
        }
        void Awake()
        {
            var idleClip = VehicleSoundSynthesis.Shared("Engine idle", 0); var loadClip = VehicleSoundSynthesis.Shared("Engine load", 1);
            for (int i = 0; i < RivalVoices + TrafficVoices; i++)
            {
                var t = new GameObject("Engine voice " + i).transform; t.SetParent(transform, false);
                AudioSource Make(AudioClip clip)
                {
                    var s = t.gameObject.AddComponent<AudioSource>(); s.clip = clip; s.loop = true; s.playOnAwake = false; s.volume = 0;
                    s.dopplerLevel = 0; s.spread = 0; s.priority = 140; s.rolloffMode = AudioRolloffMode.Logarithmic; s.minDistance = MinDistance; s.maxDistance = Audible;
                    s.spatialBlend = 1; s.timeSamples = Random.Range(0, clip.samples); return s;
                }
                voices.Add(new Voice { at = t, idle = Make(idleClip), load = Make(loadClip) });
            }
        }
        void Update()
        {
            var race = ChastainRush.RaceController.Instance;   // Chastain Rush: Racer's flow.Race
            var st = race ? race.State : ChastainRush.RaceController.Stage.Free;
            bool engine = race && st != ChastainRush.RaceController.Stage.Free;
            if (!engine || Disabled) { foreach (var v in voices) Stop(v); Playing = 0; return; }
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (Time.unscaledTime >= nextPick) { nextPick = Time.unscaledTime + .25f; Pick(race); }
            float volume = GameSettings.Settings.vehicle * Quieter;   // Chastain Rush: no split-screen or weather ducking
            Playing = 0;
            foreach (var v in voices)
            {
                // a voice whose car left the set fades out first, then takes its new car
                bool keep = v.car && v.car.gameObject.activeInHierarchy && wanted.Contains(v.car);
                v.level = Mathf.MoveTowards(v.level, keep ? 1 : 0, dt * (keep ? 1.5f : 2.5f));
                if (v.level <= 0 && !keep) { v.car = v.next; v.next = null; v.rpm = .85f; v.pedal = 0; }
                if (!v.car || !v.car.Body) { Stop(v); continue; }
                var car = v.car; v.at.position = car.Body.position;
                float speed = Mathf.Abs(car.ForwardSpeed);
                float pedal = Throttle(car);
                v.pedal = Mathf.MoveTowards(v.pedal, pedal, dt * 3);
                float revs = .82f + Mathf.Sqrt(Mathf.Clamp01(speed / Mathf.Max(1, car.topSpeed))) * 1.25f + v.pedal * .22f;
                v.rpm = Mathf.MoveTowards(v.rpm, revs, dt * (pedal > .05f ? 1.25f : .65f));
                var configuration = car.GetComponent<VehicleConfiguration>(); float pitch = configuration ? configuration.Profile.Pitch : 1;
                v.idle.pitch = v.rpm * pitch; v.load.pitch = v.rpm * pitch * 1.005f;
                float gain = v.level * volume;
                if (Camera.main) gain *= Mathf.InverseLerp(Audible, Audible * .75f, Vector3.Distance(Camera.main.transform.position, car.Body.position));
                v.idle.volume = Mathf.Lerp(.17f, .10f, v.pedal) * gain; v.load.volume = .16f * v.pedal * gain;
                if (!v.idle.isPlaying) { v.idle.Play(); v.load.Play(); }
                if (gain > .001f) Playing++;
            }
        }
        static float Throttle(ArcadeVehicle car)
        {
            var road = car.GetComponent<RoadDriver>(); if (road && road.enabled) return road.LastThrottle;
            return 0;
        }
        static void Stop(Voice v)
        {
            v.level = 0; v.car = v.next = null; v.idle.volume = v.load.volume = 0;
            if (v.idle.isPlaying) { v.idle.Stop(); v.load.Stop(); }
        }
        void Pick(ChastainRush.RaceController race)
        {
            candidates.Clear();
            ArcadeVehicle player = null;
            foreach (var e in race.Entrants) if (e.IsPlayer) player = e.Car;
            bool Eligible(ArcadeVehicle c) => c && c != player && c.gameObject.activeInHierarchy && c.Body && !HasOwnAudio(c);
            foreach (var e in race.Entrants) if (!e.IsPlayer && Eligible(e.Car)) candidates.Add(e.Car);
            wanted.Clear();
            Vector3 at = player && player.Body ? player.Body.position : Camera.main ? Camera.main.transform.position : Vector3.zero;
            candidates.Sort((a, b) => (a.Body.position - at).sqrMagnitude.CompareTo((b.Body.position - at).sqrMagnitude));
            int n = 0;
            foreach (var c in candidates)
            {
                if (n >= RivalVoices || (c.Body.position - at).sqrMagnitude > Audible * Audible) break;
                if (wanted.Add(c)) n++;
            }
            Info = $"state {race.State}; AI {candidates.Count}, wanted {wanted.Count}";
            if (ChastainRush.CheckRun.Active && Time.unscaledTime >= nextLog) { nextLog = Time.unscaledTime + 10f; Debug.Log($"[OtherEngines] {Info}, voices playing {Playing}"); }   // checks
            // every wanted car not yet on a voice goes to a free voice, or to one fading out (as its next car)
            foreach (var car in wanted)
            {
                bool placed = false;
                foreach (var v in voices) if (v.car == car || v.next == car) { placed = true; break; }
                if (placed) continue;
                foreach (var v in voices) if (!v.car) { v.car = car; v.level = 0; v.rpm = .85f; v.pedal = 0; placed = true; break; }
                if (placed) continue;
                foreach (var v in voices) if (!wanted.Contains(v.car) && !v.next) { v.next = car; break; }
            }
        }
        static bool HasOwnAudio(ArcadeVehicle c) { var a = c.GetComponent<VehicleAudio>(); return a && a.enabled; }
    }
}
