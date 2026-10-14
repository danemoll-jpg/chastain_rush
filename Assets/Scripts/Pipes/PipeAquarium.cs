using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 21 follow-ups, item 1 (Dan: "can we have the pipe be clear and you can see fish swimming around?"): the life and the sound
    // round P1's clear stretch (MapBuilder.ClearPipe.cs builds the glass, the trench of water round it, its bed, weeds and light).
    //   fish    four schools of small silver fish, a few sunfish and three big fish (carp and bass), drifting along the stretch and
    //           round the tube on paths that keep them in the water and off the glass; the big ones drift slowly and now and then
    //           dart. Cheap: one instanced draw per kind (ChastainRush/Fish wags the tails), no physics, drawn only when the camera
    //           is within 90 m
    //   sound   with the listener in the clear stretch the world goes muffled (a low-pass on the listener) and a low underwater
    //           hum with bubbles fades in; both go back as it leaves. Check runs are muted as always (CheckRun)
    public sealed class PipeAquarium : MonoBehaviour
    {
        public Vector3 a0, a1;              // the tube's axis at the stretch's two ends
        public float radius = 2.5f, surfaceY, bottomY, topHalf = 8f, bottomHalf = 3.8f;
        public Material[] fishMaterials;    // small, sunfish, big
        public static PipeAquarium Instance { get; private set; }
        public int FishCount => fish.Count;

        sealed class Fish { public int kind, school; public float s, phi, r, speed, size, phase, dart, nextDart, dir; public Vector3 pos, vel; }
        sealed class School { public float s, phi, dir, speed, phiSpeed, radiusK; }
        readonly List<Fish> fish = new List<Fish>(); readonly List<School> schools = new List<School>();
        Mesh[] meshes; readonly List<Matrix4x4>[] mats = { new List<Matrix4x4>(), new List<Matrix4x4>(), new List<Matrix4x4>() };
        Vector3 along, side; float len; AudioSource hum; AudioLowPassFilter lowPass; float inside;

        static PipeAquarium lowOwner;
        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; if (lowPass) lowPass.cutoffFrequency = 22000f; }

        void Start()
        {
            along = a1 - a0; len = along.magnitude; along /= Mathf.Max(len, 1e-3f);
            side = Vector3.Cross(Vector3.up, along).normalized;
            meshes = new[] { FishMesh(0), FishMesh(1), FishMesh(2) };
            var rnd = new System.Random(2121);
            float R() => (float)rnd.NextDouble();
            // Round 24 Part B (item 5, open water): as many fish per meter on a long stretch under a lake as on P1's 27 m
            int schoolsN = Mathf.Clamp(Mathf.RoundToInt(len / 30f), 4, 14), sunN = Mathf.Clamp(Mathf.RoundToInt(len / 20f), 5, 18), bigN = Mathf.Clamp(Mathf.RoundToInt(len / 35f), 3, 10);
            for (int k = 0; k < schoolsN; k++)
            {
                var sc = new School { s = len * (0.15f + 0.7f * R()), phi = R() * 6.283f, dir = R() < 0.5f ? -1f : 1f, speed = 0.7f + 0.8f * R(), phiSpeed = (R() - 0.5f) * 0.25f, radiusK = 0.75f + 0.2f * R() };
                schools.Add(sc);
                for (int i = 0; i < 12; i++) fish.Add(new Fish { kind = 0, school = k, size = 0.17f + 0.08f * R(), phase = R() * 6.283f, s = (R() - 0.5f) * 2.6f, phi = (R() - 0.5f) * 0.5f, r = (R() - 0.5f) * 0.18f });
            }
            for (int i = 0; i < sunN; i++) fish.Add(new Fish { kind = 1, school = -1, size = 0.24f + 0.08f * R(), phase = R() * 6.283f, s = len * R(), phi = R() * 6.283f, r = 0.8f + 0.15f * R(), speed = 0.5f + 0.4f * R(), dir = R() < 0.5f ? -1f : 1f, nextDart = 3f + 6f * R() });
            for (int i = 0; i < bigN; i++) fish.Add(new Fish { kind = 2, school = -1, size = 0.65f + 0.3f * R(), phase = R() * 6.283f, s = len * R(), phi = R() * 6.283f, r = 0.86f + 0.08f * R(), speed = 0.35f + 0.2f * R(), dir = R() < 0.5f ? -1f : 1f, nextDart = 5f + 8f * R() });
            foreach (var f in fish) { f.pos = Where(f, Time.time); f.vel = along; }
            // the sound: a hum that fades in inside, and a low-pass on the listener
            hum = gameObject.AddComponent<AudioSource>(); hum.clip = Hum(); hum.loop = true; hum.spatialBlend = 0f; hum.volume = 0f; hum.playOnAwake = false; hum.Play();
        }

        // a point on a fish's path: along the stretch (s), round the tube on an oval that keeps to the trench's water (phi, r 0..1)
        Vector3 Oval(float s, float phi, float rk)
        {
            float axisY = a0.y, up = surfaceY - 0.4f - axisY, down = axisY - (bottomY + 0.45f);
            float c = Mathf.Cos(phi), sn = Mathf.Sin(phi);
            float h = (sn >= 0f ? up : down) * sn * rk;
            float halfAt = Mathf.Lerp(bottomHalf, topHalf, Mathf.InverseLerp(bottomY, surfaceY, axisY + h)) - 0.6f;
            float lat = c * Mathf.Min(halfAt, radius + (topHalf > 12f ? 4.5f : 2.6f)) * rk;   // Round 24 Part B: wider round the tube in open water
            var q = new Vector2(lat, h); float min = radius + 0.55f;   // never against the glass
            if (q.magnitude < min) q = q.normalized * min;
            return a0 + along * Mathf.Clamp(s, 0.6f, len - 0.6f) + side * q.x + Vector3.up * q.y;
        }

        Vector3 Where(Fish f, float t)
        {
            if (f.school >= 0)
            {
                var sc = schools[f.school];
                return Oval(sc.s + f.s + 0.3f * Mathf.Sin(t * 0.9f + f.phase), sc.phi + f.phi + 0.06f * Mathf.Sin(t * 1.3f + f.phase), Mathf.Clamp(sc.radiusK + f.r, 0.55f, 1f));
            }
            return Oval(f.s, f.phi, f.r);
        }

        void Update()
        {
            var cam = Camera.main; if (!cam || meshes == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f), t = Time.time;
            var cp = cam.transform.position;
            // the sound: in the clear stretch (inside the glass)
            float sIn = Vector3.Dot(cp - a0, along); var radial = cp - (a0 + along * sIn); bool inTube = sIn > -1f && sIn < len + 1f && radial.magnitude < radius + 0.2f;
            inside = Mathf.MoveTowards(inside, inTube ? 1f : 0f, dt * 1.6f);
            if (hum) hum.volume = 0.22f * inside;
            if (!lowPass) { var l = FindAnyObjectByType<AudioListener>(); if (l) lowPass = l.GetComponent<AudioLowPassFilter>() ?? l.gameObject.AddComponent<AudioLowPassFilter>(); }
            // Round 23 follow-ups: several aquariums (P1's and the network's glass stretches): the one you are in owns the filter
            if (lowPass && (inside > 0.001f || lowOwner == this)) { lowOwner = inside > 0.001f ? this : null; lowPass.cutoffFrequency = Mathf.Lerp(22000f, 1100f, inside); lowPass.enabled = inside > 0.001f; }
            if ((cp - (a0 + a1) * 0.5f).sqrMagnitude > 90f * 90f) return;
            // schools: along the stretch, turning at its ends, drifting round
            foreach (var sc in schools)
            {
                sc.s += sc.dir * sc.speed * dt; sc.phi += sc.phiSpeed * dt;
                if (sc.s > len - 2.5f) sc.dir = -1f; if (sc.s < 2.5f) sc.dir = 1f;
            }
            foreach (var f in fish)
            {
                if (f.school < 0)
                {
                    // drift, and now and then a dart (fast for half a second), turning at the ends
                    f.nextDart -= dt; if (f.nextDart <= 0f) { f.dart = 0.6f; f.nextDart = (f.kind == 2 ? 7f : 4f) + 6f * Random.value; if (Random.value < 0.35f) f.dir = -f.dir; }
                    float v = f.speed * (f.dart > 0f ? (f.kind == 2 ? 5f : 6f) : 1f); f.dart -= dt;
                    f.s += f.dir * v * dt; f.phi += 0.05f * Mathf.Sin(t * 0.3f + f.phase) * dt * (f.dart > 0f ? 4f : 1f);
                    if (f.s > len - 1.5f) { f.dir = -1f; f.s = len - 1.5f; } if (f.s < 1.5f) { f.dir = 1f; f.s = 1.5f; }
                }
                var p = Where(f, t); var vel = (p - f.pos) / Mathf.Max(dt, 1e-4f);
                if (vel.sqrMagnitude > 0.01f) f.vel = Vector3.Slerp(f.vel, vel.normalized, Mathf.Clamp01(dt * 6f));
                f.pos = p;
            }
            foreach (var l in mats) l.Clear();
            foreach (var f in fish)
            {
                var fwd = f.vel; fwd.y *= 0.4f; if (fwd.sqrMagnitude < 1e-4f) fwd = along;
                mats[f.kind].Add(Matrix4x4.TRS(f.pos, Quaternion.LookRotation(fwd.normalized, Vector3.up), Vector3.one * f.size));
            }
            for (int k = 0; k < 3; k++)
                if (mats[k].Count > 0 && fishMaterials != null && k < fishMaterials.Length && fishMaterials[k])
                    Graphics.RenderMeshInstanced(new RenderParams(fishMaterials[k]) { shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows = false }, meshes[k], 0, mats[k]);
        }

        // a fish 1 long (nose at +z 0.5): a spindle body, a forked tail, a back fin; colored per vertex (back dark, belly pale)
        static Mesh FishMesh(int kind)
        {
            Color back, belly, fin; float w, h;
            switch (kind)
            {
                case 0: back = new Color(0.36f, 0.45f, 0.50f); belly = new Color(0.86f, 0.88f, 0.85f); fin = new Color(0.55f, 0.60f, 0.62f); w = 0.13f; h = 0.22f; break;   // a small silver fish
                case 1: back = new Color(0.30f, 0.45f, 0.30f); belly = new Color(0.95f, 0.62f, 0.25f); fin = new Color(0.32f, 0.40f, 0.30f); w = 0.14f; h = 0.42f; break;   // a sunfish (a deep body, an orange breast)
                default: back = new Color(0.42f, 0.36f, 0.22f); belly = new Color(0.80f, 0.70f, 0.45f); fin = new Color(0.45f, 0.30f, 0.20f); w = 0.17f; h = 0.27f; break;   // a carp / bass
            }
            var v = new List<Vector3>(); var c = new List<Color>(); var tr = new List<int>();
            const int rings = 9, round = 10;
            for (int i = 0; i < rings; i++)
            {
                float z = 0.5f - i / (float)(rings - 1) * 0.82f;   // nose 0.5 to the tail's root -0.32
                float u = i / (float)(rings - 1), k = Mathf.Sin(Mathf.PI * Mathf.Lerp(0.08f, 0.92f, u)) * (1f - 0.55f * u);
                for (int j = 0; j < round; j++)
                {
                    float a = j / (float)round * Mathf.PI * 2f; float y = Mathf.Cos(a);
                    v.Add(new Vector3(Mathf.Sin(a) * w * 0.5f * k, y * h * 0.5f * k, z));
                    c.Add(Color.Lerp(belly, back, y * 0.5f + 0.5f) * (0.9f + 0.1f * Mathf.Sin(z * 20f)));
                }
            }
            for (int i = 0; i + 1 < rings; i++)
                for (int j = 0; j < round; j++)
                {
                    int a = i * round + j, b = i * round + (j + 1) % round, a2 = a + round, b2 = b + round;
                    tr.Add(a); tr.Add(b); tr.Add(a2); tr.Add(b); tr.Add(b2); tr.Add(a2);
                }
            int nose = v.Count; v.Add(new Vector3(0f, 0f, 0.53f)); c.Add(back);
            for (int j = 0; j < round; j++) { tr.Add(nose); tr.Add((j + 1) % round); tr.Add(j); }
            void Tri(Vector3 p0, Vector3 p1, Vector3 p2, Color col) { int s = v.Count; v.Add(p0); v.Add(p1); v.Add(p2); c.Add(col); c.Add(col); c.Add(col); tr.Add(s); tr.Add(s + 1); tr.Add(s + 2); tr.Add(s); tr.Add(s + 2); tr.Add(s + 1); }
            // the tail: two lobes from the root
            Tri(new Vector3(0f, 0f, -0.3f), new Vector3(0f, h * 0.55f, -0.5f), new Vector3(0f, h * 0.1f, -0.44f), fin);
            Tri(new Vector3(0f, 0f, -0.3f), new Vector3(0f, -h * 0.1f, -0.44f), new Vector3(0f, -h * 0.55f, -0.5f), fin);
            Tri(new Vector3(0f, 0f, -0.3f), new Vector3(0f, h * 0.1f, -0.44f), new Vector3(0f, -h * 0.1f, -0.44f), fin);
            // the back fin
            Tri(new Vector3(0f, h * 0.45f, 0.15f), new Vector3(0f, h * 0.75f, -0.02f), new Vector3(0f, h * 0.35f, -0.18f), fin);
            var m = new Mesh { name = "Fish " + kind }; m.SetVertices(v); m.SetColors(c); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds();
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            return m;
        }

        // 6 s of being under water: a deep soft rumble, slow swells, and now and then a few bubbles rising
        static AudioClip Hum()
        {
            const int rate = 44100; int n = rate * 6; var data = new float[n];
            var rnd = new System.Random(2122); float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float w = (float)rnd.NextDouble() * 2f - 1f; lp += (w - lp) * 0.02f; lp2 += (lp - lp2) * 0.05f;
                float swell = 0.7f + 0.3f * Mathf.Sin(i * 2f * Mathf.PI / n * 2f);
                data[i] = lp2 * 6f * swell + 0.04f * Mathf.Sin(i * 2f * Mathf.PI * 55f / rate) * swell;
            }
            for (int k = 0; k < 26; k++)
            {
                int at = rnd.Next(n - rate / 4); float f0 = 260f + 420f * (float)rnd.NextDouble(), len = 0.05f + 0.07f * (float)rnd.NextDouble(), amp = 0.05f + 0.07f * (float)rnd.NextDouble();
                int m = (int)(len * rate); float ph = 0f;
                for (int i = 0; i < m; i++) { float t = i / (float)m; ph += 2f * Mathf.PI * f0 * (1f + 2f * t) / rate; data[at + i] += Mathf.Sin(ph) * amp * Mathf.Sin(Mathf.PI * t); }
            }
            int fade = rate / 10; for (int i = 0; i < fade; i++) { float t = i / (float)fade; data[i] = data[i] * t + data[n - fade + i] * (1f - t); }
            var clip = AudioClip.Create("Underwater hum", n - fade, 1, rate, false); var d2 = new float[n - fade]; System.Array.Copy(data, d2, n - fade); clip.SetData(d2, 0);
            return clip;
        }
    }
}
