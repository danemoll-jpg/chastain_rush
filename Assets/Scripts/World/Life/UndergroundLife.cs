using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 38 (Part E): Racer's UndergroundLife (its storm drain's rats that scurry off to the side recesses when a vehicle comes in,
    // with their squeaks; the cave's quiet drips), ported to the pipe network: the map build puts a rat encounter just inside the network's main
    // entrance (BF, the twin pipes off the creek) and at a few other open mouths, and quiet drips in the junction chambers and the hub
    // (MapBuilder.Life.cs). Fixed, non-colliding scenery: no spawns, navigation or vehicle forces (Racer's). Racer's rats run only in its races;
    // here any time a vehicle comes in (Free Roam too). The rats are made here at start (Racer's rat: body, muzzle, ears, eyes, tail).
    public sealed class UndergroundLife : MonoBehaviour
    {
        public Vector3[] homes = new Vector3[0], refuges = new Vector3[0];
        public Vector3 entrance, forward;
        public bool cave;   // drips only
        Transform[] rats; AudioSource sound; AudioClip clip;
        float started = -100, nextSound, awaySince = -1; bool armed = true;
        public int Encounters { get; private set; }
        public int Sounds { get; private set; }
        static Material fur, pink, black;
        void Start()
        {
            if (!fur) { fur = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.36f, .30f, .23f), enableInstancing = true }; pink = new Material(fur) { color = new Color(.78f, .55f, .55f) }; black = new Material(fur) { color = new Color(.02f, .02f, .02f) }; }
            rats = new Transform[homes.Length];
            for (int i = 0; i < homes.Length; i++)
            {
                var r = new GameObject("Scurrying drain rat " + (i + 1)).transform; r.SetParent(transform, false); r.position = homes[i]; r.rotation = Quaternion.LookRotation(Flat(refuges[i] - homes[i]));
                Body(r, "Rat body", Vector3.zero, new(.24f, .21f, .46f), fur); Body(r, "Rat pointed muzzle", new(0, .005f, .26f), new(.14f, .14f, .23f), fur);
                foreach (int s in new[] { -1, 1 }) { Body(r, "Rat ear", new(s * .07f, .1f, .16f), new(.09f, .10f, .05f), pink); Body(r, "Rat eye", new(s * .055f, .05f, .3f), Vector3.one * .025f, black); }
                Body(r, "Rat tail", new(0, -.03f, -.42f), new(.026f, .026f, .42f), pink, PrimitiveType.Cube);
                r.localScale = Vector3.one * 1.75f; rats[i] = r;
            }
            var audioObject = new GameObject("Localized underground audio"); audioObject.transform.SetParent(transform, false);
            sound = audioObject.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.spatialBlend = 1;
            sound.minDistance = cave ? 4 : 16; sound.maxDistance = cave ? 25 : 48; sound.rolloffMode = AudioRolloffMode.Linear; sound.dopplerLevel = 0;
            if (!cave) sound.priority = 80;
            const int rate = 22050; float duration = cave ? 1.3f : 2.2f; var samples = new float[(int)(rate * duration)]; var rng = new System.Random(cave ? 37 : 83);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                if (cave) { float pulse = t % .37f; float env = Mathf.Exp(-pulse * 38); samples[i] = .28f * env * Mathf.Sin(2 * Mathf.PI * (1500 * pulse - 800 * pulse * pulse)); }
                else
                {
                    float chirp = 0;
                    foreach (float onset in new[] { .03f, .47f, 1.12f }) { float u = (t - onset) / .16f; if (u > 0 && u < 1) chirp += .48f * Mathf.Sin(Mathf.PI * u) * Mathf.Sin(2 * Mathf.PI * (1900 * (t - onset) + 650 * (t - onset) * (t - onset))); }
                    float scratch = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * 83 + 1.4f * Mathf.Sin(t * 21))), 4);
                    samples[i] = chirp + .30f * (float)(rng.NextDouble() * 2 - 1) * scratch * (1 - t / duration * .65f);
                }
                samples[i] *= Mathf.Clamp01(t * 60) * Mathf.Clamp01((duration - t) * 8);
            }
            clip = AudioClip.Create(cave ? "Quiet cave drips" : "Rat squeaks and scurry", samples.Length, 1, rate, false); clip.SetData(samples, 0); sound.clip = clip; nextSound = Time.time + 7;
        }
        void Body(Transform parent, string name, Vector3 p, Vector3 s, Material m, PrimitiveType type = PrimitiveType.Sphere) { var g = GameObject.CreatePrimitive(type); g.name = name; Destroy(g.GetComponent<Collider>()); g.transform.SetParent(parent, false); g.transform.localPosition = p; g.transform.localScale = s; g.GetComponent<Renderer>().sharedMaterial = m; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward; }
        void PlayAt(Vector3 p) { sound.transform.position = p; sound.volume = (cave ? .11f : .85f) * GameSettings.Settings.ambience; sound.Play(); Sounds++; }
        Vector3 RatCenter() { var p = Vector3.zero; int count = 0; foreach (var rat in rats) if (rat && rat.gameObject.activeSelf) { p += rat.position; count++; } return count > 0 ? p / count : entrance; }
        ArcadeVehicle Nearest(out float d)
        {
            ArcadeVehicle best = null; d = float.MaxValue;
            foreach (var c in DrainPipe.Vehicles) { if (!c || !c.Body) continue; float x = Vector3.Distance(c.Body.position, entrance); if (x < d) { d = x; best = c; } }
            return best;
        }
        void Update()
        {
            if (Time.timeScale == 0) return;
            var car = Nearest(out float distance); if (!car) return;
            if (cave) { if (distance < 24 && Time.time > nextSound) { PlayAt(entrance); nextSound = Time.time + 8 + Mathf.PingPong(Time.time, 5); } return; }
            var p = car.Body.position; float along = Vector3.Dot(p - entrance, forward);
            if (armed && distance < 15 && along > -12 && along < 6 && Vector3.Dot(car.Body.linearVelocity, forward) > 1) { armed = false; started = Time.time; Encounters++; }
            if (!armed)
            {
                float age = Time.time - started;
                if (age >= .15f && Sounds < Encounters) PlayAt(RatCenter());
                for (int i = 0; i < rats.Length; i++)
                {
                    float t = Mathf.Clamp01((age - .15f - i * .10f) / 2.6f); var target = Vector3.Lerp(homes[i], refuges[i], t);
                    rats[i].position = target + Vector3.up * (Mathf.Sin(age * 55 + i) * .018f * (1 - t)); rats[i].rotation = Quaternion.LookRotation(Flat(refuges[i] - homes[i]));
                    rats[i].gameObject.SetActive(t < 1);
                }
                if (sound.isPlaying) { sound.transform.position = RatCenter(); sound.volume = .85f * GameSettings.Settings.ambience; }
                if (distance > 65) { if (awaySince < 0) awaySince = Time.time; if (Time.time - awaySince > 20) { armed = true; for (int i = 0; i < rats.Length; i++) { rats[i].position = homes[i]; rats[i].gameObject.SetActive(true); } } } else awaySince = -1;
            }
        }
        void OnDestroy() { if (clip) Destroy(clip); }
    }
}
