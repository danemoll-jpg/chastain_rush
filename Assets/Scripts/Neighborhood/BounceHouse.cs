using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 54 (A3, Dan BUG-004 off Suholden Circle: "lets put a shortcut through here too. something creative."): a rented inflatable bounce
    // castle in a back yard at the foot of a bluff (MapBuilder.Round54.cs), the reverse Addison Heights shortcut. Arcade, as R3's trampoline
    // (Trampoline.cs) and allowed for something creative that is not a ramp:
    //   the throw   a vehicle that drives in through its arch at speed (minSpeed or more, into the castle) is thrown up over the bluff and the hill:
    //               its velocity set so that it reaches 'target' (on Suholden Circle, past the hill) in 'flight' seconds, from wherever it went in
    //   in the air  turned, smoothly, upright and to 'landYaw' (along the street)
    //   landing     caught as it comes down to the street (within 1.6 m, falling), its speed across the ground turned along the way it faces
    //   slow        a vehicle that comes in slower, or from the side or the back, is bounced back out (a boing, a little hop): the yard, the
    //               bluff and its shrubs are where it is then
    // It has no collider: the castle is soft; its walls are only drawn.
    public sealed class BounceHouse : MonoBehaviour
    {
        public Vector3 front;          // the arch's middle on the ground
        public Vector3 inward;         // horizontal, into the castle through the arch
        public float halfWidth = 3f, depth = 6f, height = 3.2f, minSpeed = 13f;
        public Vector3 target;
        public float flight = 3.6f, landYaw;
        public Transform look;         // the drawn castle: it squashes and springs back at each bounce
        public static int Throws, Bumps, Landings;   // checks

        sealed class Flying { public ArcadeVehicle car; public float t, until; }
        readonly List<Flying> flying = new List<Flying>();
        readonly Dictionary<ArcadeVehicle, float> cooldown = new Dictionary<ArcadeVehicle, float>();
        static readonly List<ArcadeVehicle> cars = new List<ArcadeVehicle>();
        float wobble = -10f, nextScan;
        Vector3 lookScale = Vector3.one;
        AudioSource voice;

        void Start()
        {
            inward.y = 0f; inward.Normalize();
            if (look) lookScale = look.localScale;
            voice = gameObject.AddComponent<AudioSource>();
            voice.clip = Trampoline.Boing(); voice.spatialBlend = 1f; voice.minDistance = 8f; voice.maxDistance = 120f; voice.playOnAwake = false; SoundMix.Track(voice, 0.9f, SoundMix.Kind.Effects);
        }

        void FixedUpdate()
        {
            if (Time.time >= nextScan) { cars.Clear(); cars.AddRange(DrainPipe.Vehicles); nextScan = Time.time + 1f; }
            var right = Vector3.Cross(Vector3.up, inward);
            foreach (var car in cars)
            {
                if (!car || car.Body == null || car.Body.isKinematic) continue;
                if (cooldown.TryGetValue(car, out float cd) && Time.time < cd) continue;
                var p = car.Body.position - front; float along = Vector3.Dot(p, inward), lat = Vector3.Dot(p, right);
                if (along < -0.5f || along > depth || Mathf.Abs(lat) > halfWidth + 0.4f || p.y < -1f || p.y > height + 2.8f) continue;   // the yard rises to it: cars arrive in the air
                cooldown[car] = Time.time + 1.0f;
                var v = car.Body.linearVelocity; float into = Vector3.Dot(v, inward);
                if (into >= minSpeed) Throw(car); else Bump(car);
            }
            for (int i = flying.Count - 1; i >= 0; i--) if (!Fly(flying[i])) flying.RemoveAt(i);
            if (look)
            {
                float t = Time.time - wobble;
                float k = t >= 0f && t < 1.4f ? 0.12f * Mathf.Exp(-t * 3.5f) * Mathf.Cos(t * 14f) : 0f;
                look.localScale = new Vector3(lookScale.x * (1f + k), lookScale.y * (1f - k), lookScale.z * (1f + k));
            }
        }

        void Boing(float pitch) { wobble = Time.time; if (voice) { voice.pitch = pitch; voice.Play(); } }

        void Bump(ArcadeVehicle car)
        {
            // back out the way it came in, a little hop: a real bounce castle's
            var v = car.Body.linearVelocity; var h = new Vector3(v.x, 0f, v.z);
            car.Body.linearVelocity = -h * 0.45f + Vector3.up * 3.5f;
            Bumps++; Boing(1.3f);
        }

        void Throw(ArcadeVehicle car)
        {
            var body = car.Body; var p = body.position;
            var g = Physics.gravity;
            var launch = (target - p - 0.5f * g * flight * flight) / flight;   // reaches target in 'flight' seconds
            body.linearVelocity = launch; body.angularVelocity = Vector3.zero;
            flying.RemoveAll(f => f.car == car);
            flying.Add(new Flying { car = car, t = 0f, until = flight + 1.5f });
            Throws++; Boing(1f);
            Debug.Log($"[BounceHouse] {car.name} thrown from ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}) at ({launch.x:0.0}, {launch.y:0.0}, {launch.z:0.0}) m/s toward ({target.x:0.0}, {target.z:0.0})");
        }

        // one physics step of a thrown vehicle's flight (Trampoline.Fly); false when it has landed (or given up)
        bool Fly(Flying f)
        {
            var car = f.car; if (!car) return false;
            var body = car.Body; f.t += Time.fixedDeltaTime;
            if (f.t > f.until) return false;
            var want = Quaternion.Euler(0f, landYaw, 0f);
            body.MoveRotation(Quaternion.Slerp(body.rotation, want, 1f - Mathf.Exp(-Time.fixedDeltaTime * 2.2f)));
            body.angularVelocity = Vector3.zero;
            var v = body.linearVelocity;
            if (f.t < 0.5f || v.y > -1f) return true;
            if (!Physics.Raycast(body.position, Vector3.down, out var hit, 1.6f, ~0, QueryTriggerInteraction.Ignore) || hit.rigidbody == body) return true;
            var fwd = want * Vector3.forward; var n = hit.normal;
            float across = new Vector2(v.x, v.z).magnitude * 0.97f;
            var along = Vector3.ProjectOnPlane(fwd, n).normalized;
            body.MoveRotation(Quaternion.LookRotation(along, n));
            body.linearVelocity = along * across + n * -1.5f;
            body.angularVelocity = Vector3.zero;
            Landings++;
            Debug.Log($"[BounceHouse] {car.name} landed at ({body.position.x:0.0}, {body.position.z:0.0}) after {f.t:0.00} s, {across:0.0} m/s along the street");
            return false;
        }
    }
}
