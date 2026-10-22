using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 21 follow-ups, item 1 (Dan: "can we have the pipe be clear and you can see fish swimming around?"): the life and the sound
    // round P1's clear stretch (MapBuilder.ClearPipe.cs builds the glass, the trench of water round it, its bed, weeds and light).
    //   fish    schools of small silver fish, sunfish and big fish (carp and bass), drifting along the stretch and round the tube on
    //           paths that keep them in the water and off the glass; the big ones drift slowly and now and then dart. Cheap: one
    //           instanced draw per kind (ChastainRush/Fish wags the tails), no physics, drawn only when the camera is within 90 m
    //   sound   with the listener in the clear stretch the world goes muffled (a low-pass on the listener) and a low underwater
    //           hum with bubbles fades in; both go back as it leaves. Check runs are muted as always (CheckRun)
    // Round 25 (A8, Dan: "I want to make sure that also there always fish and other life visible. Maybe turtles and snakes or ducks, or
    // geese swimming on top if this isnt to taxing"):
    //   the way  fish follow the tube's own line (a network glass stretch curves: 'path', with the deep water's half width 'rim' and its
    //            bed 'bottom' at each point; MapBuilder.ClearPipe.cs), no longer a straight line from end to end
    //   always   on a long stretch, a school or a fish more than 45 m along from the camera is moved to 20 to 35 m ahead of or behind
    //            it (in the haze, out of sight), so there is always life round you
    //   more     turtles (some swimming slowly, some resting on the bed or on a sunken log: 'rests'), a water snake swimming at the
    //            surface, ducks and geese paddling on top (from below: their bodies' shapes through the surface; from the bank: birds
    //            on the pond). All instanced with the fish's shader, no physics, nothing solid; moved and drawn only while the stretch
    //            is on (its pipe cell: NetworkCells)
    public sealed class PipeAquarium : MonoBehaviour
    {
        public Vector3 a0, a1;              // the tube's axis at the stretch's two ends
        public float radius = 2.5f, surfaceY, bottomY, topHalf = 8f, bottomHalf = 3.8f;
        public Material[] fishMaterials;    // small, sunfish, big; Round 25: turtle, snake, water bird
        // Round 58 (C): other tubes' glass near this stretch (capsules: axis a..b, radius r to the glass's outside): a fish or a critter inside one
        // is not drawn (it would swim inside another pipe)
        public Vector3[] avoidA, avoidB; public float[] avoidR;
        bool Avoided(Vector3 q)
        {
            if (avoidA == null) return false;
            for (int i = 0; i < avoidA.Length; i++) { var a = avoidA[i]; var ab = avoidB[i] - a; float t = Mathf.Clamp01(Vector3.Dot(q - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); if ((a + ab * t - q).sqrMagnitude < avoidR[i] * avoidR[i]) return true; }
            return false;
        }
        public Vector3[] path; public float[] rim, bottom;   // Round 25: the axis every meter, the deep water's half width there, its bed
        public Vector3[] rests; public float[] restYaw;       // Round 25: turtles resting on the bed or a log
        public static PipeAquarium Instance { get; private set; }
        public int FishCount => fish.Count;
        public int LifeCount => fish.Count + critters.Count;

        sealed class Fish { public int kind, school; public float s, phi, r, speed, size, phase, dart, nextDart, dir; public Vector3 pos, vel; }
        sealed class School { public float s, phi, dir, speed, phiSpeed, radiusK; }
        // Round 25: a turtle, a snake or a bird: on (s, lateral) (swimming turtles keep to an oval round the tube like the fish)
        sealed class Critter { public int kind; public float s, lat, heading, speed, size, phase, phi, r, dir; public int rest = -1; public Vector3 pos, vel; }
        readonly List<Fish> fish = new List<Fish>(); readonly List<School> schools = new List<School>(); readonly List<Critter> critters = new List<Critter>();
        const int Kinds = 7;   // 0 small, 1 sunfish, 2 big, 3 turtle, 4 snake, 5 goose, 6 duck
        static readonly int[] MatOf = { 0, 1, 2, 3, 4, 5, 5 };
        Mesh[] meshes; readonly List<Matrix4x4>[] mats = new List<Matrix4x4>[Kinds];
        Vector3 along; float len; AudioSource hum; AudioLowPassFilter lowPass; float inside;
        float[] pathS; readonly System.Random rnd = new System.Random(2121);

        static PipeAquarium lowOwner;
        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; if (lowPass) lowPass.cutoffFrequency = 22000f; }

        float R01() => (float)rnd.NextDouble();

        // a check run's "before" (ChastainRush.exe -exeProfile ... -aquariumPlain): the fish as in Round 24, no other life, for the frame rate
        static bool Plain => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-aquariumPlain") >= 0;

        void Start()
        {
            along = a1 - a0; along.y = 0f; along = along.sqrMagnitude > 1e-6f ? along.normalized : Vector3.forward;
            if (path == null || path.Length < 2) { path = new[] { a0, a1 }; rim = new[] { topHalf, topHalf }; bottom = new[] { bottomY, bottomY }; }
            pathS = new float[path.Length]; for (int i = 1; i < path.Length; i++) pathS[i] = pathS[i - 1] + Vector3.Distance(path[i - 1], path[i]);
            len = Mathf.Max(0.1f, pathS[path.Length - 1]);
            for (int k = 0; k < Kinds; k++) mats[k] = new List<Matrix4x4>();
            meshes = new[] { FishMesh(0), FishMesh(1), FishMesh(2), TurtleMesh(), SnakeMesh(), GooseFlock.SwimmingGoose(), GooseFlock.Duck() };
            // Round 24 Part B (open water): as many fish per meter on a long stretch under a lake as on P1's 27 m; Round 25: more of them
            int schoolsN = Mathf.Clamp(Mathf.RoundToInt(len / 18f), 4, 24), sunN = Mathf.Clamp(Mathf.RoundToInt(len / 12f), 5, 34), bigN = Mathf.Clamp(Mathf.RoundToInt(len / 25f), 3, 16);
            if (Plain) { schoolsN = Mathf.Clamp(Mathf.RoundToInt(len / 30f), 4, 14); sunN = Mathf.Clamp(Mathf.RoundToInt(len / 20f), 5, 18); bigN = Mathf.Clamp(Mathf.RoundToInt(len / 35f), 3, 10); }
            for (int k = 0; k < schoolsN; k++)
            {
                var sc = new School { s = len * (0.15f + 0.7f * R01()), phi = R01() * 6.283f, dir = R01() < 0.5f ? -1f : 1f, speed = 0.7f + 0.8f * R01(), phiSpeed = (R01() - 0.5f) * 0.25f, radiusK = 0.75f + 0.2f * R01() };
                schools.Add(sc);
                for (int i = 0; i < 12; i++) fish.Add(new Fish { kind = 0, school = k, size = 0.17f + 0.08f * R01(), phase = R01() * 6.283f, s = (R01() - 0.5f) * 2.6f, phi = (R01() - 0.5f) * 0.5f, r = (R01() - 0.5f) * 0.18f });
            }
            for (int i = 0; i < sunN; i++) fish.Add(new Fish { kind = 1, school = -1, size = 0.24f + 0.08f * R01(), phase = R01() * 6.283f, s = len * R01(), phi = R01() * 6.283f, r = 0.8f + 0.15f * R01(), speed = 0.5f + 0.4f * R01(), dir = R01() < 0.5f ? -1f : 1f, nextDart = 3f + 6f * R01() });
            for (int i = 0; i < bigN; i++) fish.Add(new Fish { kind = 2, school = -1, size = 0.65f + 0.3f * R01(), phase = R01() * 6.283f, s = len * R01(), phi = R01() * 6.283f, r = 0.86f + 0.08f * R01(), speed = 0.35f + 0.2f * R01(), dir = R01() < 0.5f ? -1f : 1f, nextDart = 5f + 8f * R01() });
            foreach (var f in fish) { f.pos = Where(f, Time.time); f.vel = along; }
            // Round 25: turtles (resting ones on the build's spots, the rest swimming), a snake or two, birds on top
            if (Plain) { hum = gameObject.AddComponent<AudioSource>(); hum.clip = Hum(); hum.loop = true; hum.spatialBlend = 0f; hum.volume = 0f; hum.playOnAwake = false; hum.Play(); return; }
            int turtles = Mathf.Clamp(Mathf.RoundToInt(len / 40f), 2, 8), resting = rests != null ? Mathf.Min(rests.Length, turtles / 2 + 1) : 0;
            for (int i = 0; i < turtles; i++)
                critters.Add(new Critter { kind = 3, rest = i < resting ? i : -1, s = len * R01(), phi = -0.4f - 1.2f * R01(), r = 0.7f + 0.2f * R01(), speed = 0.2f + 0.15f * R01(), dir = R01() < 0.5f ? -1f : 1f, size = 0.38f + 0.14f * R01(), phase = R01() * 6.283f });
            int snakes = Mathf.Clamp(Mathf.RoundToInt(len / 60f), 1, 4);
            for (int i = 0; i < snakes; i++) critters.Add(new Critter { kind = 4, s = len * R01(), lat = (R01() - 0.5f) * 4f, speed = 0.45f + 0.2f * R01(), dir = R01() < 0.5f ? -1f : 1f, size = 1.2f + 0.4f * R01(), phase = R01() * 6.283f });
            int birds = Mathf.Clamp(Mathf.RoundToInt(len / 25f), 3, 12);
            for (int i = 0; i < birds; i++) critters.Add(new Critter { kind = i % 3 == 0 ? 5 : 6, s = len * R01(), lat = (R01() - 0.5f) * 6f, heading = R01() * 6.283f, speed = 0.18f + 0.15f * R01(), size = 1f + 0.1f * R01(), phase = R01() * 6.283f });
            // the sound: a hum that fades in inside, and a low-pass on the listener
            hum = gameObject.AddComponent<AudioSource>(); hum.clip = Hum(); hum.loop = true; hum.spatialBlend = 0f; hum.volume = 0f; hum.playOnAwake = false; hum.Play();
        }

        // the stretch's frame at s: the axis point, left (level), the deep water's half width and bed
        void Frame(float s, out Vector3 c, out Vector3 sd, out float rimAt, out float botAt)
        {
            s = Mathf.Clamp(s, 0f, len);
            int i = System.Array.BinarySearch(pathS, s); if (i < 0) i = ~i; i = Mathf.Clamp(i, 1, path.Length - 1);
            float t = Mathf.InverseLerp(pathS[i - 1], pathS[i], s);
            c = Vector3.Lerp(path[i - 1], path[i], t); var al = path[i] - path[i - 1]; al.y = 0f; al = al.sqrMagnitude > 1e-6f ? al.normalized : along;
            sd = Vector3.Cross(Vector3.up, al).normalized; rimAt = Mathf.Lerp(rim[i - 1], rim[i], t); botAt = Mathf.Lerp(bottom[i - 1], bottom[i], t);
        }

        // the deep water's half width at a height (the bowl: ClearFrame.BowlY, its shape u^1.7 from 0.8 R out to the rim)
        float HalfAt(float rimAt, float botAt, float y)
        {
            float b0 = Mathf.Min(radius * 0.8f, rimAt - 0.6f), top = surfaceY - 0.2f;
            float u = Mathf.Pow(Mathf.Clamp01((y - botAt) / Mathf.Max(0.1f, top - botAt)), 1f / 1.7f);
            return b0 + u * (rimAt - b0);
        }

        // a point on a fish's path: along the stretch (s), round the tube on an oval that keeps to the deep water (phi, r 0..1)
        Vector3 Oval(float s, float phi, float rk)
        {
            Frame(s, out var c, out var sd, out float rimAt, out float botAt);
            float axisY = c.y, up = surfaceY - 0.4f - axisY, down = axisY - (botAt + 0.45f);
            float co = Mathf.Cos(phi), sn = Mathf.Sin(phi);
            float h = (sn >= 0f ? up : down) * sn * rk;
            float halfAt = HalfAt(rimAt, botAt, axisY + h) - 0.6f;
            float lat = co * Mathf.Min(halfAt, radius + (rimAt > 12f ? 4.5f : 2.6f)) * rk;   // Round 24 Part B: wider round the tube in open water
            var q = new Vector2(lat, h); float min = radius + 0.55f;   // never against the glass
            if (q.magnitude < min) q = q.normalized * min;
            return c + sd * q.x + Vector3.up * q.y;
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

        // the camera's place along the stretch (s), and how far it is from the axis there
        float CameraS(Vector3 cp, out float off)
        {
            float best = float.MaxValue, bs = 0f;
            for (int i = 0; i + 1 < path.Length; i++)
            {
                var a = path[i]; var ab = path[i + 1] - a; float t = Mathf.Clamp01(Vector3.Dot(cp - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                float d = (a + ab * t - cp).sqrMagnitude; if (d < best) { best = d; bs = Mathf.Lerp(pathS[i], pathS[i + 1], t); }
            }
            off = Mathf.Sqrt(best); return bs;
        }

        // Round 25: something more than 45 m along from the camera goes to 20 to 35 m ahead of or behind it (inside the stretch)
        float Near(float s, float camS)
        {
            if (Mathf.Abs(s - camS) <= 45f || len < 60f) return s;
            float sgn = R01() < 0.5f ? -1f : 1f, ns = camS + sgn * (20f + 15f * R01());
            if (ns < 2f || ns > len - 2f) ns = camS - sgn * (20f + 15f * R01());
            return Mathf.Clamp(ns, 2f, len - 2f);
        }

        void Update()
        {
            var cam = Camera.main; if (!cam || meshes == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f), t = Time.time;
            var cp = cam.transform.position;
            float camS = CameraS(cp, out float camOff);
            // the sound: in the clear stretch (inside the glass)
            bool inTube = camS > 0.01f && camS < len - 0.01f && camOff < radius + 0.2f;
            inside = Mathf.MoveTowards(inside, inTube ? 1f : 0f, dt * 1.6f);
            if (hum) hum.volume = 0.22f * inside * Racer.SoundMix.Gain(Racer.SoundMix.Kind.Ambience);   // Round 27: the Ambience volume
            if (!lowPass) { var l = FindAnyObjectByType<AudioListener>(); if (l) lowPass = l.GetComponent<AudioLowPassFilter>() ?? l.gameObject.AddComponent<AudioLowPassFilter>(); }
            // Round 23 follow-ups: several aquariums (P1's and the network's glass stretches): the one you are in owns the filter
            if (lowPass && (inside > 0.001f || lowOwner == this)) { lowOwner = inside > 0.001f ? this : null; lowPass.cutoffFrequency = Mathf.Lerp(22000f, 1100f, inside); lowPass.enabled = inside > 0.001f; }
            if (camOff > 90f) return;
            // schools: along the stretch, turning at its ends, drifting round
            for (int k = 0; k < schools.Count; k++)
            {
                var sc = schools[k];
                sc.s += sc.dir * sc.speed * dt; sc.phi += sc.phiSpeed * dt;
                if (sc.s > len - 2.5f) sc.dir = -1f; if (sc.s < 2.5f) sc.dir = 1f;
                float ns = Near(sc.s, camS); if (ns != sc.s) { sc.s = ns; foreach (var f in fish) if (f.school == k) f.pos = Where(f, t); }
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
                    float ns = Near(f.s, camS); if (ns != f.s) { f.s = ns; f.pos = Where(f, t); }
                }
                var p = Where(f, t); var vel = (p - f.pos) / Mathf.Max(dt, 1e-4f);
                if (vel.sqrMagnitude > 0.01f) f.vel = Vector3.Slerp(f.vel, vel.normalized, Mathf.Clamp01(dt * 6f));
                f.pos = p;
            }
            foreach (var c in critters) MoveCritter(c, dt, t, camS);
            foreach (var l in mats) l.Clear();
            foreach (var f in fish)
            {
                var fwd = f.vel; fwd.y *= 0.4f; if (fwd.sqrMagnitude < 1e-4f) fwd = along;
                if (Avoided(f.pos)) continue;   // Round 58 (C)
                mats[f.kind].Add(Matrix4x4.TRS(f.pos, Quaternion.LookRotation(fwd.normalized, Vector3.up), Vector3.one * f.size));
            }
            foreach (var c in critters)
            {
                var fwd = c.vel; fwd.y = c.kind == 3 && c.rest < 0 ? fwd.y * 0.3f : 0f; if (fwd.sqrMagnitude < 1e-4f) fwd = along;
                if (Avoided(c.pos)) continue;   // Round 58 (C)
                mats[c.kind].Add(Matrix4x4.TRS(c.pos, Quaternion.LookRotation(fwd.normalized, Vector3.up), Vector3.one * c.size));
            }
            for (int k = 0; k < Kinds; k++)
            {
                int mi = MatOf[k];
                if (mats[k].Count > 0 && fishMaterials != null && mi < fishMaterials.Length && fishMaterials[mi])
                    Graphics.RenderMeshInstanced(new RenderParams(fishMaterials[mi]) { shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows = false }, meshes[k], 0, mats[k]);
            }
        }

        // Round 25: a turtle swims on an oval round the tube like the fish (low, slow) or rests; a snake swims at the surface, winding;
        // a bird paddles on the surface, wandering, turning back from the shore and the stretch's ends
        void MoveCritter(Critter c, float dt, float t, float camS)
        {
            Vector3 p;
            if (c.kind == 3 && c.rest >= 0 && rests != null && c.rest < rests.Length)
            {
                p = rests[c.rest] + Vector3.up * (0.02f * Mathf.Sin(t * 0.5f + c.phase));
                float yaw = (restYaw != null && c.rest < restYaw.Length ? restYaw[c.rest] : 0f) + 6f * Mathf.Sin(t * 0.2f + c.phase);
                c.vel = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward; c.pos = p; return;
            }
            if (c.kind == 3)
            {
                c.s += c.dir * c.speed * dt; if (c.s > len - 2f) c.dir = -1f; if (c.s < 2f) c.dir = 1f;
                c.s = Near(c.s, camS); c.phi += 0.04f * Mathf.Sin(t * 0.21f + c.phase) * dt;
                p = Oval(c.s, c.phi, c.r);
            }
            else if (c.kind == 4)
            {
                c.s += c.dir * c.speed * dt; if (c.s > len - 2f) c.dir = -1f; if (c.s < 2f) c.dir = 1f;
                c.s = Near(c.s, camS);
                Frame(c.s, out var ctr, out var sd, out float rimAt, out _);
                float lim = Mathf.Max(0.5f, rimAt - 1.2f), wl = Mathf.Clamp(c.lat + 1.6f * Mathf.Sin(t * 0.35f + c.phase), -lim, lim);
                p = new Vector3(ctr.x, surfaceY - 0.05f, ctr.z) + sd * wl;
            }
            else
            {
                c.heading += (Mathf.PerlinNoise(t * 0.07f, c.phase) - 0.5f) * dt * 1.2f;
                var dir2 = new Vector2(Mathf.Cos(c.heading), Mathf.Sin(c.heading));   // (along, lateral)
                Frame(c.s, out _, out _, out float rimAt, out _);
                float lim = Mathf.Max(0.5f, rimAt - 1.6f);
                c.s += dir2.x * c.speed * dt; c.lat += dir2.y * c.speed * dt;
                if (Mathf.Abs(c.lat) > lim && Mathf.Sign(dir2.y) == Mathf.Sign(c.lat)) c.heading = Mathf.Atan2(-dir2.y, dir2.x);
                if ((c.s > len - 3f && dir2.x > 0f) || (c.s < 3f && dir2.x < 0f)) c.heading = Mathf.Atan2(dir2.y, -dir2.x);
                c.lat = Mathf.Clamp(c.lat, -lim, lim); float ns = Near(c.s, camS); if (ns != c.s) { c.s = ns; c.lat = (R01() - 0.5f) * 2f * lim; c.pos = Vector3.zero; }
                Frame(c.s, out var ctr, out var sd, out _, out _);
                float water = c.kind == 5 ? 0.30f : 0.20f;   // the waterline on the model
                p = new Vector3(ctr.x, surfaceY - water + 0.01f * Mathf.Sin(t * 1.3f + c.phase), ctr.z) + sd * c.lat;
            }
            if (c.pos == Vector3.zero) { c.pos = p; c.vel = along; return; }
            var v = (p - c.pos) / Mathf.Max(dt, 1e-4f);
            if (v.sqrMagnitude > 0.0004f) c.vel = Vector3.Slerp(c.vel.sqrMagnitude > 0f ? c.vel : v.normalized, v.normalized, Mathf.Clamp01(dt * 3f));
            c.pos = p;
        }

        // Round 25: a turtle 1 long (nose at +z 0.5): a low domed shell, a head, four flippers and a tail (olive and brown)
        static Mesh TurtleMesh()
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var tr = new List<int>();
            var shell = new Color(0.30f, 0.33f, 0.18f); var under = new Color(0.62f, 0.56f, 0.34f); var skin = new Color(0.32f, 0.36f, 0.24f);
            const int rings = 5, round = 12;
            for (int i = 0; i <= rings; i++)
            {
                float u = i / (float)rings, h = Mathf.Cos(u * Mathf.PI * 0.5f), rr = Mathf.Sin(u * Mathf.PI * 0.5f);
                for (int j = 0; j < round; j++)
                {
                    float a = j / (float)round * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Sin(a) * 0.30f * rr, h * 0.16f, Mathf.Cos(a) * 0.36f * rr)); c.Add(Color.Lerp(shell * 1.15f, shell * 0.8f, u) * (0.9f + 0.1f * ((i + j) & 1)));
                }
            }
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < round; j++) { int a = i * round + j, b = i * round + (j + 1) % round; tr.Add(a); tr.Add(b); tr.Add(a + round); tr.Add(b); tr.Add(b + round); tr.Add(a + round); }
            int rimStart = rings * round, centre = v.Count; v.Add(new Vector3(0f, -0.03f, 0f)); c.Add(under);
            for (int j = 0; j < round; j++) { tr.Add(centre); tr.Add(rimStart + (j + 1) % round); tr.Add(rimStart + j); }
            void Tri(Vector3 p0, Vector3 p1, Vector3 p2, Color col) { int s = v.Count; v.Add(p0); v.Add(p1); v.Add(p2); c.Add(col); c.Add(col); c.Add(col); tr.Add(s); tr.Add(s + 1); tr.Add(s + 2); tr.Add(s); tr.Add(s + 2); tr.Add(s + 1); }
            // the head (a small wedge out front), the flippers, the tail
            Tri(new Vector3(-0.07f, 0.03f, 0.33f), new Vector3(0.07f, 0.03f, 0.33f), new Vector3(0f, 0.04f, 0.50f), skin);
            Tri(new Vector3(-0.06f, 0.03f, 0.33f), new Vector3(0f, 0.09f, 0.40f), new Vector3(0f, 0.04f, 0.50f), skin);
            Tri(new Vector3(0.06f, 0.03f, 0.33f), new Vector3(0f, 0.09f, 0.40f), new Vector3(0f, 0.04f, 0.50f), skin);
            foreach (float sx in new[] { -1f, 1f })
            {
                Tri(new Vector3(sx * 0.24f, 0.0f, 0.18f), new Vector3(sx * 0.27f, 0.0f, 0.05f), new Vector3(sx * 0.46f, -0.01f, 0.20f), skin);
                Tri(new Vector3(sx * 0.22f, 0.0f, -0.18f), new Vector3(sx * 0.25f, 0.0f, -0.26f), new Vector3(sx * 0.36f, -0.01f, -0.36f), skin);
            }
            Tri(new Vector3(-0.03f, 0.01f, -0.34f), new Vector3(0.03f, 0.01f, -0.34f), new Vector3(0f, 0.0f, -0.46f), skin);
            var m = new Mesh { name = "Turtle" }; m.SetVertices(v); m.SetColors(c); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            return m;
        }

        // Round 25: a water snake 1 long (nose at +z 0.5): a thin tube tapering to the tail, a slightly wider head; dark with bands
        // (the fish shader's wag winds it)
        static Mesh SnakeMesh()
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var tr = new List<int>();
            const int rings = 24, round = 6;
            for (int i = 0; i < rings; i++)
            {
                float u = i / (float)(rings - 1), z = 0.5f - u, r = (u < 0.08f ? 0.028f + 0.1f * u : 0.034f * (1f - 0.85f * u * u)) * (u > 0.97f ? 0.3f : 1f);
                var col = (i % 4 < 2) ? new Color(0.20f, 0.18f, 0.13f) : new Color(0.36f, 0.30f, 0.20f);
                for (int j = 0; j < round; j++) { float a = j / (float)round * Mathf.PI * 2f; v.Add(new Vector3(Mathf.Sin(a) * r * 1.2f, Mathf.Cos(a) * r, z)); c.Add(Mathf.Cos(a) > 0f ? col : col * 1.6f); }
            }
            for (int i = 0; i + 1 < rings; i++)
                for (int j = 0; j < round; j++) { int a = i * round + j, b = i * round + (j + 1) % round; tr.Add(a); tr.Add(b); tr.Add(a + round); tr.Add(b); tr.Add(b + round); tr.Add(a + round); }
            int nose = v.Count; v.Add(new Vector3(0f, 0f, 0.53f)); c.Add(new Color(0.20f, 0.18f, 0.13f));
            for (int j = 0; j < round; j++) { tr.Add(nose); tr.Add((j + 1) % round); tr.Add(j); }
            var m = new Mesh { name = "Water snake" }; m.SetVertices(v); m.SetColors(c); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            return m;
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
