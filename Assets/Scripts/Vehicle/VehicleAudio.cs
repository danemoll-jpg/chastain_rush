using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    [DisallowMultipleComponent, RequireComponent(typeof(ArcadeVehicle))]
    public sealed class VehicleAudio : MonoBehaviour
    {
        ArcadeVehicle car;
        VehicleInput input;
        VehicleRespawn respawn;
        // Chastain Rush: no RaceFlow; the race controller here never pauses or silences the vehicle
        readonly AudioSource[] voices = new AudioSource[6];
        readonly AudioClip[] clips = new AudioClip[7];
        readonly float[] levels = new float[5];
        readonly Dictionary<Mesh, (int[] indices, Color[] colors)> surfaces = new();
        float rpm = .85f, load, road, nextImpact, airborne, downward, impactGain;
        bool wasGrounded;
        public float Slip { get; private set; }
        public float RoadWeight => road;
        public int ImpactCount { get; private set; }
        public static float Volume => GameSettings.Settings.vehicle;   // Chastain Rush, Round 27: Racer's Settings.vehicle (GameSettings; default .75)
        // Chastain Rush, Round 15 (item 11): an AI driver's vehicle makes its engine sound too, heard in 3D from where it is
        // (Racer removes the engine audio from its rivals). Its pedals come from its RoadDriver instead of the player's input.
        [System.NonSerialized] public RoadDriver driver;

        void Start()
        {
            car = GetComponent<ArcadeVehicle>(); input = GetComponent<VehicleInput>();
            respawn = GetComponent<VehicleRespawn>();
            if (respawn) respawn.Respawned += ResetAudio;
            string[] names = { "Engine idle", "Engine load", "Road rolling", "Loose ground", "Tire slip", "Body impact", "Prop clatter" };
            for (int i = 0; i < clips.Length; i++) clips[i] = VehicleSoundSynthesis.Create(names[i], i);
            for (int i = 0; i < voices.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>(); voices[i] = source;
                source.playOnAwake = false; source.spatialBlend = 0; source.dopplerLevel = 0;
                source.priority = i < 2 ? 100 : 150; source.volume = 0;
                if (driver) { source.spatialBlend = 1; source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 8; source.maxDistance = 140; source.priority = i < 2 ? 160 : 220; }   // Round 15: an AI car, in 3D
                if (i < 5) { source.clip = clips[i]; source.loop = true; source.Play(); }
            }
            ResetAudio();
        }
        bool Racing => true;   // Chastain Rush: free roam and races both drive
        void Update()
        {
            if (!car || !car.Body) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            float volume = Volume;   // Chastain Rush: Racer's Settings.vehicle (Round 27)
            float speed = Racing ? Mathf.Abs(car.ForwardSpeed) : 0;
            bool grounded = Racing && car.GroundedWheels >= 2;
            float pedal = driver ? (car.ForwardSpeed < -.6f ? driver.LastBrake : driver.LastThrottle) : Racing && input ? (car.ForwardSpeed < -.6f ? input.BrakeReverse : input.Throttle) : 0;   // Chastain Rush: the AI's pedals (Round 15)
            load = Mathf.MoveTowards(load, pedal, dt * 3);
            // Audio-only rev estimate; no forces, gear changes or motor edits.
            float revs = .82f + Mathf.Sqrt(Mathf.Clamp01(speed / car.topSpeed)) * 1.25f + load * .22f;
            rpm = Mathf.MoveTowards(rpm, revs, dt * (pedal > .05f ? 1.25f : .65f));
            var configuration = GetComponent<VehicleConfiguration>();
            float pitch = configuration ? configuration.Profile.Pitch : 1;
            voices[0].pitch = rpm * pitch; voices[1].pitch = rpm * pitch * 1.005f;
            bool engine = true;
            SetLevel(0, engine ? Mathf.Lerp(.17f, .10f, load) : 0, volume, dt);
            SetLevel(1, engine ? .16f * load : 0, volume, dt);
            Slip = grounded ? Mathf.Abs(Vector3.Dot(car.Body.linearVelocity, transform.right)) : 0;
            float slipLevel = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.4f, 5.5f, Slip)) * Mathf.InverseLerp(5, 14, speed);
            float brakingSlip=grounded&&car.ForwardSpeed>.6f?Mathf.InverseLerp(.5f,1,driver?driver.LastBrake:input?input.BrakeReverse:0)*Mathf.InverseLerp(9,24,speed):0;
            slipLevel=Mathf.Max(slipLevel,brakingSlip*.85f);
            if (grounded) road = Mathf.MoveTowards(road, SampleRoad(), dt * 4);
            float rolling = grounded ? Mathf.InverseLerp(1, 30, speed) : 0;
            SetLevel(2, .07f * rolling * road, volume, dt, !grounded);
            SetLevel(3, (.10f * rolling + .07f * slipLevel) * (1 - road), volume, dt, !grounded);
            SetLevel(4, .25f * slipLevel * Mathf.Lerp(.12f, 1, road), volume, dt, !grounded);
            voices[2].pitch = Mathf.Lerp(.7f, 1.3f, rolling);
            voices[3].pitch = Mathf.Lerp(.8f, 1.2f, rolling);
            voices[4].pitch = Mathf.Lerp(.92f, 1.08f, slipLevel)*(configuration&&configuration.Profile.Small?1.13f:1);
            voices[5].volume = volume * impactGain;
            if (!Racing) { airborne = downward = 0; wasGrounded = false; voices[5].Stop(); }
        }
        void FixedUpdate()
        {
            if (!car || !Racing) return;
            bool grounded = car.GroundedWheels >= 2;
            if (!grounded) { airborne += Time.fixedDeltaTime; downward = Mathf.Max(downward, -car.Body.linearVelocity.y); }
            else
            {
                if (!wasGrounded && airborne > .12f && downward > 2) Impact(downward, false);
                airborne = downward = 0;
            }
            wasGrounded = grounded;
        }
        void SetLevel(int index, float target, float volume, float dt, bool immediate = false)
        {
            levels[index] = immediate ? 0 : Mathf.MoveTowards(levels[index], target, dt * .4f);
            // 0.76: the engine dips a little while thunder rolls (WeatherEffects.Duck), so the thunder stands out.
            voices[index].volume = levels[index] * volume * (1 - .3f * WeatherEffects.Duck);   // 0.76: the engine dips a little while thunder rolls (Chastain Rush, Round 35)
        }
        public void Silence()
        {
            for(int i=0;i<levels.Length;i++) levels[i]=0;
            foreach(var voice in voices) if(voice) voice.volume=0;
            if(voices[5]) voices[5].Stop();
            load=airborne=downward=0;
        }
        float SampleRoad()
        {
            if (!Physics.Raycast(transform.position, -transform.up, out var hit, car.suspensionLength + .3f, car.groundMask, QueryTriggerInteraction.Ignore)) return road;
            if (hit.collider is MeshCollider mc && mc.sharedMesh && mc.sharedMesh.isReadable && mc.sharedMesh.name.StartsWith("Ground_"))
            {
                var mesh = mc.sharedMesh;
                if (!surfaces.TryGetValue(mesh, out var data)) surfaces[mesh] = data = (mesh.triangles, mesh.colors);
                int k = hit.triangleIndex * 3;
                if (k >= 0 && k + 2 < data.indices.Length && data.colors.Length == mesh.vertexCount)
                {
                    var b = hit.barycentricCoordinate;
                    Color c = data.colors[data.indices[k]] * b.x + data.colors[data.indices[k + 1]] * b.y + data.colors[data.indices[k + 2]] * b.z;
                    return 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.28f, .36f, c.r));
                }
            }
            // Authored ramps and concrete are hard surfaces; unknown terrain stays restrained.
            return hit.collider.name.Contains("Ramp") || hit.collider.name.Contains("Road") ? 1 : 0;
        }
        void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.isTrigger) return;   // Chastain Rush: breakables here are triggers (no BreakableProp)
            float closing = 0;
            for (int i = 0; i < collision.contactCount; i++) closing = Mathf.Max(closing, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(i).normal)));
            if (closing > 2.5f) Impact(closing, false);
        }
        public void Impact(float strength, bool prop)
        {
            if (!Racing || voices[5] == null || Time.time < nextImpact) return;
            nextImpact = Time.time + .22f; ImpactCount++;
            voices[5].Stop(); voices[5].pitch = Random.Range(.92f, 1.08f);
            voices[5].clip = clips[prop ? 6 : 5];
            impactGain = Mathf.Lerp(.09f, .22f, Mathf.InverseLerp(2, 18, strength));
            voices[5].volume = Volume * impactGain;
            voices[5].Play();
        }
        void ResetAudio()
        {
            airborne = downward = load = Slip = 0; wasGrounded = false;
            nextImpact = Time.time + .3f;
            if (voices[5]) voices[5].Stop();
            for (int i = 2; i < 5; i++) { levels[i] = 0; if (voices[i]) voices[i].volume = 0; }
        }
        void OnDestroy()
        {
            if (respawn) respawn.Respawned -= ResetAudio;
            foreach (var source in voices) if (source) Destroy(source);
            foreach (var clip in clips) if (clip) Destroy(clip);
        }
    }
}
