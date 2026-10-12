using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 18, item 7: the giant trampoline of reverse shortcut R3 (Dan: "a slide from a play ground as a ramp which would
    // bounce you onto a trampoline which would ultimately bounce you over the house"). Arcade, by design:
    //   the bounce   a vehicle that comes down on the mat (or passes low over it, off the play set's deck at speed) is thrown
    //                up and over house 1752: its velocity is set so that it reaches 'target' (a point on Chastain Glen Lane
    //                beyond the house) in 'flight' seconds, from wherever on the mat it touched. Every vehicle, every speed
    //   in the air  it is turned, smoothly, upright and to 'landYaw' (down the lane)
    //   landing     as it comes down to the lane (within 1.6 m of the ground, falling) its fall is caught and its speed across
    //                the ground is turned along the way it faces (less 3%): on its wheels, pointing down the lane
    //   slow        a vehicle that rolls onto the mat (not from the air) only hops a little and can drive off the mat's edge; one that misses the mat lands in the yard (a reset or the way round the house)
    //   look        the mat dips and springs back (a damped wobble) and a boing plays at the bounce
    public sealed class Trampoline : MonoBehaviour
    {
        public float radius = 6.5f, matY;
        public Vector3 target;
        public float flight = 4f, landYaw;
        public Transform mat;          // the drawn mat (look only); its collider stays put
        public static int Bounces, Landings;   // checks

        sealed class Flying { public ArcadeVehicle car; public float t, until; }
        readonly List<Flying> flying = new List<Flying>();
        readonly Dictionary<ArcadeVehicle, float> cooldown = new Dictionary<ArcadeVehicle, float>();
        static readonly List<ArcadeVehicle> cars = new List<ArcadeVehicle>();
        float wobble = -10f, nextScan;
        Vector3 matRest;
        AudioSource voice; AudioClip made;
        public static string LastBounceClip { get; private set; } = "";   // for the checks: the clip the last bounce played

        void Start()
        {
            if (mat) matRest = mat.localPosition;
            voice = gameObject.AddComponent<AudioSource>();
            voice.clip = Boing(); voice.spatialBlend = 1f; voice.minDistance = 8f; voice.maxDistance = 120f; voice.playOnAwake = false; voice.volume = 0.9f;
        }

        // a springy "boing": a tone that bends up then down with a fast vibrato, dying away (synthesized, as the goose's honk)
        static AudioClip Boing()
        {
            const int rate = 22050; int n = (int)(rate * 0.7f); var d = new float[n]; float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float f = 140f + 260f * Mathf.Exp(-t * 6f) * Mathf.Sin(t * 9f + 0.3f) + 120f * (1f - Mathf.Exp(-t * 4f)) + 18f * Mathf.Sin(t * 2f * Mathf.PI * 28f);
                ph += 2f * Mathf.PI * f / rate;
                d[i] = (Mathf.Sin(ph) * 0.7f + Mathf.Sin(ph * 2.01f) * 0.2f) * Mathf.Exp(-t * 4.5f) * Mathf.Clamp01(t * 200f) * 0.8f;
            }
            var clip = AudioClip.Create("Boing", n, 1, rate, false); clip.SetData(d, 0); return clip;
        }

        void FixedUpdate()
        {
            if (Time.time >= nextScan) { cars.Clear(); cars.AddRange(FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)); nextScan = Time.time + 1f; }
            var c = new Vector2(transform.position.x, transform.position.z);
            foreach (var car in cars)
            {
                if (!car || car.Body == null || car.Body.isKinematic) continue;   // the AI's cars have ArcadeVehicle off (RoadDriver steps them)
                if (cooldown.TryGetValue(car, out float cd) && Time.time < cd) continue;
                var p = car.Body.position; var v = car.Body.linearVelocity;
                if ((new Vector2(p.x, p.z) - c).magnitude > radius + 0.3f || p.y > matY + 1.9f || p.y < matY - 0.6f) continue;
                if (v.y > 1.5f) continue;
                float across = new Vector2(v.x, v.z).magnitude;
                cooldown[car] = Time.time + 1.0f;
                // the full throw only for a vehicle that comes in from the air (off the deck, falling or still flying over the
                // mat); one that rolls on, or sits on the mat and drives off, gets a little hop
                bool fromAir = v.y < -2.5f || (car.GroundedWheels == 0 && across >= 8f);
                Bounce(car, fromAir);
            }
            for (int i = flying.Count - 1; i >= 0; i--) if (!Fly(flying[i])) flying.RemoveAt(i);
            // the mat's look: a damped wobble after each bounce
            if (mat)
            {
                float t = Time.time - wobble;
                float dip = t >= 0f && t < 1.6f ? -0.55f * Mathf.Exp(-t * 3.2f) * Mathf.Cos(t * 15f) : 0f;
                mat.localPosition = matRest + Vector3.up * dip;
            }
        }

        void Bounce(ArcadeVehicle car, bool full)
        {
            var body = car.Body; var p = body.position;
            wobble = Time.time;
            if (voice)
            {
                // Round 20 follow-up (Dan: "I want to replace the trampolione sound"): a drop-in file replaces the made one
                // (Sounds\Trampoline\trampoline*, SoundDropIns; several: one at random per bounce, played at its own pitch)
                var drop = SoundDropIns.Instance != null ? SoundDropIns.Instance.Bounces : null;
                if (drop != null && drop.Count > 0) { voice.clip = drop[Random.Range(0, drop.Count)]; voice.pitch = 1f; }
                else { if (made == null) made = voice.clip; voice.clip = made; voice.pitch = full ? 1f : 1.3f; }
                voice.Play();
                LastBounceClip = voice.clip ? voice.clip.name : "";
            }
            if (!full)
            {
                // a little hop where it is (a slow arrival): a real trampoline's bounce, nowhere near the house
                var v = body.linearVelocity; body.linearVelocity = new Vector3(v.x * 0.6f, 4f, v.z * 0.6f);
                return;
            }
            var g = Physics.gravity;
            var launch = (target - p - 0.5f * g * flight * flight) / flight;   // reaches target in 'flight' seconds
            body.linearVelocity = launch; body.angularVelocity = Vector3.zero;
            flying.RemoveAll(f => f.car == car);
            flying.Add(new Flying { car = car, t = 0f, until = flight + 1.5f });
            Bounces++;
            Debug.Log($"[Trampoline] {car.name} bounced from ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}) at ({launch.x:0.0}, {launch.y:0.0}, {launch.z:0.0}) m/s toward ({target.x:0.0}, {target.z:0.0})");
        }

        // one physics step of a bounced vehicle's flight; false when it has landed (or given up)
        bool Fly(Flying f)
        {
            var car = f.car; if (!car) return false;
            var body = car.Body; f.t += Time.fixedDeltaTime;
            if (f.t > f.until) return false;
            // turned upright and down the lane, smoothly over the first two thirds of the flight
            var want = Quaternion.Euler(0f, landYaw, 0f);
            body.MoveRotation(Quaternion.Slerp(body.rotation, want, 1f - Mathf.Exp(-Time.fixedDeltaTime * 2.2f)));
            body.angularVelocity = Vector3.zero;
            var v = body.linearVelocity;
            if (f.t < 0.5f || v.y > -1f) return true;
            // coming down: catch it within 1.6 m of the ground under it
            if (!Physics.Raycast(body.position, Vector3.down, out var hit, 1.6f, ~0, QueryTriggerInteraction.Ignore) || hit.rigidbody == body) return true;   // from the center: the ray starts inside its own box
            var fwd = want * Vector3.forward; var n = hit.normal;
            float across = new Vector2(v.x, v.z).magnitude * 0.97f;
            var along = Vector3.ProjectOnPlane(fwd, n).normalized;
            body.MoveRotation(Quaternion.LookRotation(along, n));
            body.linearVelocity = along * across + n * -1.5f;
            body.angularVelocity = Vector3.zero;
            Landings++;
            Debug.Log($"[Trampoline] {car.name} landed at ({body.position.x:0.0}, {body.position.z:0.0}) after {f.t:0.00} s, {across:0.0} m/s down the lane");
            return false;
        }
    }
}
