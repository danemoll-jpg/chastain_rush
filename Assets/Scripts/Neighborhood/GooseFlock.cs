using System.Collections.Generic;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 7: the geese that live at the pond (and one heron). They are scenery with a mind of their own, not solid
    // obstacles: no colliders anywhere, so they can never stop, flip or launch the car. Behaviour:
    //   Graze  walk about their zone (bank, road beside the pond), now and then head down
    //   Swim   glide about on the water
    //   Flee   the car is close (distance grows with its speed): honk, flap, take off away from it and land in
    //          another part of the pond or on the far bank
    //   Away   wait until the car has been gone a while (more than ReturnDistance for ReturnSeconds), then come
    //          home: walking or swimming if it is near, flying if far
    // Driving through the flock (a goose within HitRadius of the car) costs a little speed, once per goose per few
    // seconds, and that goose takes off. Round 8: the honk is an audio clip, picked at random from honkClips (filled by
    // MapBuilder from Assets/Audio/Geese/goose_honk*.wav / .ogg / .mp3) with a little pitch variation and at most
    // MaxVoices playing at once. With no clips the geese are silent (the Round 7 synthesized honk is gone). Round 15: plus any
    // goose_honk* file in the drop-in folder (SoundDropIns), read each time the game starts.
    public sealed class GooseFlock : MonoBehaviour
    {
        public enum ZoneKind { Bank, Road, Water }
        [System.Serializable] public struct Zone { public ZoneKind kind; public Vector3 centre; public float radius; }

        public Material material;                 // ChastainRush/VertexLit
        public Vector2[] pondOutline;             // world x, z of the pond
        public float waterY;
        public Zone[] zones;
        public int[] gooseZones;                  // one entry per goose: index into zones (its home)
        public Vector3 heronPost;                 // where the heron stands (on the bank, at the water's edge)
        public bool heron = true;
        public int seed = 7;
        public AudioClip[] honkClips = new AudioClip[0];   // Round 8: set by MapBuilder from Assets/Audio/Geese
        public const int MaxVoices = 3;

        // tuning
        public const float HitRadius = 2.0f, HitSpeedLoss = 0.02f, HitCooldown = 3f;
        public const float ReturnDistance = 38f, ReturnSeconds = 6f;
        public const float ScareBase = 6f, ScarePerMps = 0.30f, ScareMax = 14f;

        enum Mode { Idle, Walk, Peck, Swim, Flee, Fly, Away, Return }

        sealed class Bird
        {
            public Transform t, wingL, wingR; public bool isHeron;
            public Mode mode; public int home;
            public Vector3 pos, target, flightStart; public float yaw, timer, flightT, flightDur, flightPeak, awayClock, nextHit, bob;
            public Zone landZone; public bool landing; public float speed;
        }

        readonly List<Bird> birds = new List<Bird>();
        ArcadeVehicle car;
        System.Random rng;
        AudioSource[] voices; float lastHonk;
        Mesh gooseBody, gooseWing, heronBody, heronWing;
        public int BirdCount => birds.Count;
        public int FlyingCount { get { int n = 0; foreach (var b in birds) if (b.mode == Mode.Fly || b.mode == Mode.Flee) n++; return n; } }
        public int ScatteredCount { get { int n = 0; foreach (var b in birds) if (b.mode == Mode.Fly || b.mode == Mode.Flee || b.mode == Mode.Away || b.mode == Mode.Return) n++; return n; } }
        public float TotalSpeedLost { get; private set; }
        public int HitsCount { get; private set; }
        public int HonksPlayed { get; private set; }
        public int VoiceCount => voices != null ? voices.Length : 0;

        void Start()
        {
            car = FindAnyObjectByType<ArcadeVehicle>();
            rng = new System.Random(seed);
            EnsureVoices();
            // Round 15, item 4: honks Dan drops into Sounds\Geese are added to the built-in ones, read at start-up (no rebuild)
            if (SoundDropIns.Instance != null && SoundDropIns.Instance.Loaded) AddDropIns();
            else SoundDropIns.Ready += AddDropIns;
            BuildModels();
            for (int i = 0; i < gooseZones.Length; i++) birds.Add(MakeBird(false, zones[gooseZones[i]], i));
            if (heron) birds.Add(MakeBird(true, new Zone { kind = ZoneKind.Bank, centre = heronPost, radius = 0.5f }, 99));
        }

        void OnDestroy() { SoundDropIns.Ready -= AddDropIns; }

        void AddDropIns()
        {
            SoundDropIns.Ready -= AddDropIns;
            var d = SoundDropIns.Instance; if (d == null || d.Honks.Count == 0) return;
            var list = new System.Collections.Generic.List<AudioClip>(honkClips); list.AddRange(d.Honks); honkClips = list.ToArray();
            EnsureVoices();
        }

        void EnsureVoices()
        {
            int clips = 0; foreach (var c in honkClips) if (c) clips++;
            if (clips > 0 && voices == null)
            {
                voices = new AudioSource[MaxVoices];
                for (int i = 0; i < voices.Length; i++)
                {
                    var go = new GameObject("honk " + i); go.transform.SetParent(transform, false);
                    var a = go.AddComponent<AudioSource>();
                    a.spatialBlend = 1f; a.minDistance = 6f; a.maxDistance = 90f; a.rolloffMode = AudioRolloffMode.Linear; a.volume = 0.8f; a.playOnAwake = false;
                    voices[i] = a;
                }
            }
        }

        // ---------------------------------------------------------------- models (procedural, low poly)

        void BuildModels()
        {
            gooseBody = new Mesh { name = "goose" }; gooseWing = new Mesh { name = "goose wing" };
            heronBody = new Mesh { name = "heron" }; heronWing = new Mesh { name = "heron wing" };
            var brown = new Color(0.40f, 0.34f, 0.27f); var breast = new Color(0.78f, 0.72f, 0.62f); var black = new Color(0.05f, 0.05f, 0.05f);
            var white = new Color(0.95f, 0.95f, 0.93f); var tan = new Color(0.62f, 0.54f, 0.43f);
            var g = new LP();
            g.Ellipsoid(new Vector3(0, 0.36f, 0), new Vector3(0.17f, 0.15f, 0.30f), brown, breast);
            g.Cone(new Vector3(0, 0.40f, -0.26f), new Vector3(0, 0.46f, -0.50f), 0.08f, black, 5);            // tail
            g.Quad(new Vector3(-0.06f, 0.33f, -0.29f), new Vector3(0.06f, 0.33f, -0.29f), new Vector3(0.05f, 0.40f, -0.31f), new Vector3(-0.05f, 0.40f, -0.31f), white);
            g.Tube(new Vector3(0, 0.44f, 0.20f), new Vector3(0, 0.80f, 0.32f), 0.055f, 0.04f, black, 6);       // neck
            g.Ellipsoid(new Vector3(0, 0.84f, 0.36f), new Vector3(0.05f, 0.045f, 0.08f), black, black);       // head
            g.Box(new Vector3(0, 0.825f, 0.45f), new Vector3(0.025f, 0.015f, 0.05f), black);                   // beak
            g.Quad(new Vector3(-0.045f, 0.80f, 0.34f), new Vector3(0.045f, 0.80f, 0.34f), new Vector3(0.04f, 0.83f, 0.37f), new Vector3(-0.04f, 0.83f, 0.37f), white); // chinstrap
            g.Box(new Vector3(-0.07f, 0.11f, 0.02f), new Vector3(0.012f, 0.11f, 0.012f), black);               // legs
            g.Box(new Vector3(0.07f, 0.11f, 0.02f), new Vector3(0.012f, 0.11f, 0.012f), black);
            g.Apply(gooseBody);
            var w = new LP();
            w.Quad(new Vector3(0, 0, 0.18f), new Vector3(0.72f, 0.02f, 0.10f), new Vector3(0.78f, 0.02f, -0.32f), new Vector3(0, 0, -0.22f), tan, true);
            w.Apply(gooseWing);

            var grey = new Color(0.55f, 0.60f, 0.65f); var pale = new Color(0.82f, 0.84f, 0.85f); var yellow = new Color(0.85f, 0.70f, 0.20f);
            var h = new LP();
            h.Ellipsoid(new Vector3(0, 0.78f, 0), new Vector3(0.10f, 0.11f, 0.26f), grey, pale);
            h.Tube(new Vector3(0, 0.84f, 0.18f), new Vector3(0, 1.20f, 0.22f), 0.03f, 0.025f, pale, 5);
            h.Ellipsoid(new Vector3(0, 1.23f, 0.24f), new Vector3(0.035f, 0.035f, 0.06f), pale, pale);
            h.Cone(new Vector3(0, 1.22f, 0.29f), new Vector3(0, 1.20f, 0.46f), 0.025f, yellow, 4);
            h.Tube(new Vector3(-0.05f, 0.0f, 0.0f), new Vector3(-0.05f, 0.70f, 0.0f), 0.012f, 0.012f, black, 4);
            h.Tube(new Vector3(0.05f, 0.0f, 0.0f), new Vector3(0.05f, 0.70f, 0.0f), 0.012f, 0.012f, black, 4);
            h.Apply(heronBody);
            var hw = new LP();
            hw.Quad(new Vector3(0, 0, 0.25f), new Vector3(1.0f, 0.03f, 0.15f), new Vector3(1.05f, 0.03f, -0.40f), new Vector3(0, 0, -0.30f), grey, true);
            hw.Apply(heronWing);
        }

        Bird MakeBird(bool isHeron, Zone zone, int index)
        {
            var go = new GameObject(isHeron ? "Heron" : "Goose " + index);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = isHeron ? heronBody : gooseBody;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = material; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var b = new Bird { t = go.transform, isHeron = isHeron, home = index };
            Transform Wing(string n, float side)
            {
                var wg = new GameObject(n); wg.transform.SetParent(go.transform, false);
                wg.transform.localPosition = new Vector3(side * (isHeron ? 0.10f : 0.13f), isHeron ? 0.88f : 0.46f, isHeron ? 0.05f : 0.05f);
                wg.transform.localScale = new Vector3(side, 1f, 1f);
                wg.AddComponent<MeshFilter>().sharedMesh = isHeron ? heronWing : gooseWing;
                var r = wg.AddComponent<MeshRenderer>(); r.sharedMaterial = material; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return wg.transform;
            }
            b.wingL = Wing("wingL", -1f); b.wingR = Wing("wingR", 1f);
            b.pos = RandomIn(zone, isHeron);
            b.yaw = (float)rng.NextDouble() * 360f;
            b.landZone = zone;
            b.mode = zone.kind == ZoneKind.Water ? Mode.Swim : Mode.Idle;
            b.timer = 1f + (float)rng.NextDouble() * 4f;
            b.target = b.pos;
            Place(b, 0f);
            return b;
        }

        // ---------------------------------------------------------------- the loop

        void Update()
        {
            if (car == null) { car = FindAnyObjectByType<ArcadeVehicle>(); if (car == null) return; }
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            Vector3 cp = car.transform.position;
            Vector3 cv = car.Body.linearVelocity; cv.y = 0f;
            float carSpeed = cv.magnitude;
            float scare = Mathf.Min(ScareMax, ScareBase + ScarePerMps * carSpeed);
            float lossThisFrame = 0f;
            foreach (var b in birds)
            {
                Vector3 d = b.pos - cp; d.y = 0f;
                float dist = d.magnitude;
                bool airborne = b.mode == Mode.Fly && b.pos.y > GroundY(b.pos.x, b.pos.z, b.landZone.kind == ZoneKind.Water) + 1.3f;
                // driving through the flock: a goose the car reaches (still on the ground, running or just lifting off) costs a little speed
                if (!airborne && !b.isHeron && dist < HitRadius && carSpeed > 1.5f && Time.time > b.nextHit && lossThisFrame < 0.05f)
                {
                    b.nextHit = Time.time + HitCooldown;
                    lossThisFrame += HitSpeedLoss;
                    var v = car.Body.linearVelocity;
                    car.Body.linearVelocity = new Vector3(v.x * (1f - HitSpeedLoss), v.y, v.z * (1f - HitSpeedLoss));
                    TotalSpeedLost += HitSpeedLoss;
                    HitsCount++;
                    if (b.mode != Mode.Fly && b.mode != Mode.Flee) Scatter(b, cp, cv, dist);
                    else if (b.mode == Mode.Flee) b.timer = 0f;
                }
                if (b.mode != Mode.Fly && b.mode != Mode.Flee)
                {
                    float need = b.isHeron ? scare * 1.6f : scare;
                    // a car at speed scatters them from afar; one barely moving only when it is close
                    if ((carSpeed > 2f && dist < need) || dist < 4f) Scatter(b, cp, cv, dist);
                }
                Step(b, dt, dist);
            }
        }

        void Step(Bird b, float dt, float carDist)
        {
            switch (b.mode)
            {
                case Mode.Idle:
                    b.timer -= dt;
                    FoldWings(b);
                    if (b.timer <= 0f)
                    {
                        if (b.isHeron) { b.timer = 6f + (float)rng.NextDouble() * 8f; break; }
                        b.target = PickTarget(b);
                        b.mode = rng.NextDouble() < 0.35 ? Mode.Peck : Mode.Walk; b.timer = 1f + (float)rng.NextDouble() * 2f;
                    }
                    break;
                case Mode.Peck:
                    b.timer -= dt;
                    FoldWings(b);
                    if (b.timer <= 0f) { b.mode = Mode.Walk; }
                    break;
                case Mode.Walk:
                    FoldWings(b);
                    if (MoveToward(b, b.target, 0.7f, 180f, dt)) { b.mode = Mode.Idle; b.timer = 2f + (float)rng.NextDouble() * 6f; }
                    break;
                case Mode.Swim:
                    FoldWings(b);
                    b.timer -= dt;
                    if (b.timer <= 0f || Vector3.Distance(b.pos, b.target) < 0.4f) { b.target = PickTarget(b); b.timer = 4f + (float)rng.NextDouble() * 8f; }
                    MoveToward(b, b.target, 0.45f, 70f, dt);
                    break;
                case Mode.Flee:   // the take-off run: a few quick steps and flapping before leaving the ground
                    Flap(b, dt, 14f);
                    b.timer -= dt;
                    MoveToward(b, b.target, 3.6f, 700f, dt, true);
                    if (b.timer <= 0f) Launch(b, b.target, false);
                    break;
                case Mode.Fly:
                    Flap(b, dt, 20f);
                    b.flightT += dt / b.flightDur;
                    float t = Mathf.Clamp01(b.flightT);
                    Vector3 flat = Vector3.Lerp(b.flightStart, b.target, t);
                    float arc = 4f * t * (1f - t) * b.flightPeak;
                    float ground = GroundY(flat.x, flat.z, b.landZone.kind == ZoneKind.Water || (b.landing && b.landZone.kind == ZoneKind.Water));
                    float y = Mathf.Lerp(b.flightStart.y, ground, t) + arc;
                    Vector3 np = new Vector3(flat.x, y, flat.z);
                    Vector3 dir = np - b.pos; dir.y = 0f;
                    if (dir.sqrMagnitude > 1e-6f) b.yaw = Mathf.LerpAngle(b.yaw, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, Mathf.Min(1f, dt * 8f));
                    b.pos = np;
                    b.t.SetPositionAndRotation(np, Quaternion.Euler(Mathf.Lerp(-18f, 12f, t), b.yaw, 0f));
                    if (b.flightT >= 1f)
                    {
                        b.pos = new Vector3(b.target.x, GroundY(b.target.x, b.target.z, b.landZone.kind == ZoneKind.Water), b.target.z);
                        b.mode = b.landing ? (b.landZone.kind == ZoneKind.Water ? Mode.Swim : Mode.Idle) : Mode.Away;
                        b.timer = 1f + (float)rng.NextDouble() * 3f;
                        if (b.landing) b.landing = false;
                        b.awayClock = 0f;
                        Place(b, 0f);
                    }
                    break;
                case Mode.Away:
                    FoldWings(b);
                    b.awayClock = carDist > ReturnDistance ? b.awayClock + dt : 0f;
                    if (b.awayClock >= ReturnSeconds + (b.home % 5) * 0.8f)
                    {
                        // home again: swim or walk if close, else fly
                        var hz = zones[Mathf.Clamp(b.isHeron ? 0 : gooseZones[b.home], 0, zones.Length - 1)];
                        if (b.isHeron) hz = new Zone { kind = ZoneKind.Bank, centre = heronPost, radius = 0.5f };
                        b.landZone = hz;
                        Vector3 goal = RandomIn(hz, b.isHeron);
                        if (Vector3.Distance(b.pos, goal) > 14f || (hz.kind == ZoneKind.Water) != (b.landZone.kind == ZoneKind.Water)) { Launch(b, goal, true); }
                        else { b.target = goal; b.mode = hz.kind == ZoneKind.Water ? Mode.Swim : Mode.Walk; }
                    }
                    break;
                case Mode.Return: break;
            }
        }

        // ---------------------------------------------------------------- scatter and flight

        void Scatter(Bird b, Vector3 carPos, Vector3 carVel, float dist)
        {
            if (b.mode == Mode.Flee || b.mode == Mode.Fly) return;
            // fly away from the car, across or along the pond, to somewhere at least 18 m off
            Vector3 away = b.pos - carPos; away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitSphere;
            away.y = 0f; away.Normalize();
            // bias sideways of the car's heading so a bird in the road does not fly the same line as the car
            if (carVel.sqrMagnitude > 4f) { var side = Vector3.Cross(Vector3.up, carVel.normalized); if (Vector3.Dot(side, away) < 0f) side = -side; away = (away * 0.7f + side * 0.5f).normalized; }
            Vector3 dest = Vector3.zero; bool ok = false;
            // prefer water far from the car for geese; the heron prefers another bank point
            for (int i = 0; i < 14 && !ok; i++)
            {
                float reach = 16f + (float)rng.NextDouble() * 22f;
                Vector3 tp = b.pos + Quaternion.Euler(0f, (float)(rng.NextDouble() - 0.5) * 70f, 0f) * away * reach;
                if (!b.isHeron && PointInPond(tp, 3f)) { dest = new Vector3(tp.x, waterY, tp.z); b.landZone = new Zone { kind = ZoneKind.Water, centre = dest, radius = 8f }; ok = true; }
                else if (b.isHeron && PointNearBank(tp)) { dest = tp; b.landZone = new Zone { kind = ZoneKind.Bank, centre = dest, radius = 2f }; ok = true; }
            }
            if (!ok)
            {
                dest = b.pos + away * 24f; dest.y = GroundY(dest.x, dest.z, false);
                b.landZone = new Zone { kind = ZoneKind.Bank, centre = dest, radius = 6f };
            }
            b.target = dest; b.landing = false;
            b.mode = Mode.Flee; b.timer = 0.45f + (float)rng.NextDouble() * 0.6f;
            Honk(b.pos);
        }

        void Launch(Bird b, Vector3 dest, bool landing)
        {
            b.flightStart = b.pos; b.target = dest; b.flightT = 0f; b.landing = landing;
            float dist = Vector3.Distance(new Vector3(b.pos.x, 0, b.pos.z), new Vector3(dest.x, 0, dest.z));
            b.flightDur = Mathf.Max(1.0f, dist / (b.isHeron ? 7f : 8f));
            b.flightPeak = Mathf.Clamp(1.6f + dist * 0.12f, 2f, 6f);
            b.mode = Mode.Fly;
        }

        void Honk(Vector3 at)
        {
            if (Time.time - lastHonk < 0.12f || voices == null) return;
            AudioSource v = null;
            foreach (var a in voices) if (!a.isPlaying) { v = a; break; }
            if (v == null) return;   // MaxVoices already honking: skip this one rather than cut one off
            AudioClip clip = null;
            for (int tries = 0; tries < 8 && !clip; tries++) clip = honkClips[rng.Next(honkClips.Length)];
            if (!clip) return;
            lastHonk = Time.time;
            v.transform.position = at;
            v.clip = clip;
            v.pitch = 0.93f + (float)rng.NextDouble() * 0.14f;
            v.Play();
            HonksPlayed++;
        }

        // ---------------------------------------------------------------- movement helpers

        bool MoveToward(Bird b, Vector3 target, float speed, float turnRate, float dt, bool run = false)
        {
            Vector3 d = target - b.pos; d.y = 0f;
            float dist = d.magnitude;
            if (dist < 0.15f) { Place(b, dt); return true; }
            float want = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            b.yaw = Mathf.MoveTowardsAngle(b.yaw, want, turnRate * dt);
            float aligned = Mathf.Cos(Mathf.DeltaAngle(b.yaw, want) * Mathf.Deg2Rad);
            float step = Mathf.Min(dist, speed * Mathf.Max(0f, aligned) * dt);
            Vector3 fwd = Quaternion.Euler(0f, b.yaw, 0f) * Vector3.forward;
            bool water = b.mode == Mode.Swim;
            if (run) water = false;
            Vector3 np = b.pos + fwd * step;
            np.y = (water || (run && PointInPond(np, 0f))) ? waterY + 0.02f : GroundY(np.x, np.z, false);
            if (water && !PointInPond(np, 0.8f)) { b.target = PickTarget(b); return false; }
            b.pos = np;
            Place(b, dt);
            return false;
        }

        void Place(Bird b, float dt)
        {
            float bob = b.mode == Mode.Swim ? Mathf.Sin(Time.time * 1.6f + b.home) * 0.012f : 0f;
            float pitch = b.mode == Mode.Peck ? 22f : 0f;
            b.t.SetPositionAndRotation(b.pos + Vector3.up * bob, Quaternion.Euler(pitch, b.yaw, 0f));
        }

        void FoldWings(Bird b) { b.wingL.localRotation = Quaternion.Euler(0f, 0f, 82f); b.wingR.localRotation = Quaternion.Euler(0f, 0f, -82f); }   // folded down along the body (the left wing mesh points to -x, the right to +x)
        void Flap(Bird b, float dt, float rate)
        {
            float a = Mathf.Sin(Time.time * rate + b.home * 0.7f) * 38f + (b.isHeron ? 10f : 4f);
            b.wingL.localRotation = Quaternion.Euler(0f, 0f, -a); b.wingR.localRotation = Quaternion.Euler(0f, 0f, a);
        }

        Vector3 PickTarget(Bird b)
        {
            var z = b.mode == Mode.Swim || b.landZone.kind == ZoneKind.Water ? b.landZone : zones[Mathf.Clamp(gooseZones.Length > b.home ? gooseZones[b.home] : 0, 0, zones.Length - 1)];
            if (b.isHeron) z = new Zone { kind = ZoneKind.Bank, centre = heronPost, radius = 0.5f };
            // a road bird sometimes ambles on to another zone of the flock: banks and road verge are neighbours
            return RandomIn(z, b.isHeron);
        }

        Vector3 RandomIn(Zone z, bool isHeron)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = z.radius * Mathf.Sqrt((float)rng.NextDouble());
                var p = z.centre + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (z.kind == ZoneKind.Water) { if (!PointInPond(p, 1.5f)) continue; p.y = waterY; return p; }
                if (!isHeron && PointInPond(p, -0.2f)) continue;   // on land: not in the water
                p.y = GroundY(p.x, p.z, false);
                return p;
            }
            var c = z.centre; c.y = GroundY(c.x, c.z, z.kind == ZoneKind.Water); return c;
        }

        float GroundY(float x, float z, bool water)
        {
            if (water) return waterY + 0.02f;
            if (Physics.Raycast(new Vector3(x, 400f, z), Vector3.down, out var hit, 800f, 1, QueryTriggerInteraction.Ignore)) return hit.point.y;
            return waterY;
        }

        // inset > 0: that many metres inside the outline
        public bool PointInPond(Vector3 p, float inset)
        {
            if (pondOutline == null || pondOutline.Length < 3) return false;
            if (!Inside(p.x, p.z)) return false;
            if (inset <= 0f) return true;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                if (!Inside(p.x + Mathf.Cos(a) * inset, p.z + Mathf.Sin(a) * inset)) return false;
            }
            return true;
        }
        bool PointNearBank(Vector3 p) => !PointInPond(p, 0f) && (PointInPond(p + Vector3.right * 3f, 0f) || PointInPond(p - Vector3.right * 3f, 0f) || PointInPond(p + Vector3.forward * 3f, 0f) || PointInPond(p - Vector3.forward * 3f, 0f));
        bool Inside(float x, float z)
        {
            bool inside = false;
            for (int i = 0, j = pondOutline.Length - 1; i < pondOutline.Length; j = i++)
            {
                var a = pondOutline[i]; var b2 = pondOutline[j];
                if ((a.y > z) != (b2.y > z) && x < (b2.x - a.x) * (z - a.y) / (b2.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        // ---------------------------------------------------------------- tiny mesh builder for the bird models

        sealed class LP
        {
            readonly List<Vector3> v = new List<Vector3>(); readonly List<Color> c = new List<Color>(); readonly List<int> t = new List<int>();
            int Add(Vector3 p, Color col) { v.Add(p); c.Add(col); return v.Count - 1; }
            public void Quad(Vector3 a, Vector3 b, Vector3 d, Vector3 e, Color col, bool both = true)
            {
                int i = Add(a, col); Add(b, col); Add(d, col); Add(e, col);
                t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2);
                if (both) { int j = Add(a, col); Add(b, col); Add(d, col); Add(e, col); t.Add(j); t.Add(j + 1); t.Add(j + 2); t.Add(j); t.Add(j + 2); t.Add(j + 3); } // own vertices, so the back face has its own normals
            }
            public void Box(Vector3 c0, Vector3 half, Color col)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    Face(c0, new Vector3(s * half.x, 0, 0), new Vector3(0, half.y, 0), new Vector3(0, 0, half.z), col, s);
                    Face(c0, new Vector3(0, s * half.y, 0), new Vector3(half.x, 0, 0), new Vector3(0, 0, half.z), col, -s);
                    Face(c0, new Vector3(0, 0, s * half.z), new Vector3(half.x, 0, 0), new Vector3(0, half.y, 0), col, s);
                }
            }
            void Face(Vector3 c0, Vector3 n, Vector3 a, Vector3 b, Color col, int flip)
            {
                int i = Add(c0 + n - a - b, col); Add(c0 + n + a - b, col); Add(c0 + n + a + b, col); Add(c0 + n - a + b, col);
                if (Vector3.Dot(Vector3.Cross(v[i + 1] - v[i], v[i + 2] - v[i]), n) >= 0f) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
                else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
            }
            public void Ellipsoid(Vector3 centre, Vector3 radii, Color top, Color bottom, int seg = 8, int rings = 4)
            {
                int start = v.Count;
                for (int r = 0; r <= rings; r++)
                {
                    float phi = Mathf.PI * r / rings;
                    for (int s = 0; s < seg; s++)
                    {
                        float th = 2f * Mathf.PI * s / seg;
                        var p = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th) * radii.x, Mathf.Cos(phi) * radii.y, Mathf.Sin(phi) * Mathf.Sin(th) * radii.z);
                        Add(centre + p, Color.Lerp(top, bottom, Mathf.Clamp01(0.5f - p.y / radii.y * 0.7f)));
                    }
                }
                for (int r = 0; r < rings; r++)
                    for (int s = 0; s < seg; s++)
                    {
                        int a = start + r * seg + s, b = start + r * seg + (s + 1) % seg, d = start + (r + 1) * seg + s, e = start + (r + 1) * seg + (s + 1) % seg;
                        t.Add(a); t.Add(b); t.Add(d); t.Add(b); t.Add(e); t.Add(d);
                    }
                // make winding outward: flip triangles whose normal points inward
                FixWinding(start, centre);
            }
            void FixWinding(int vStart, Vector3 centre)
            {
                for (int i = t.Count - 1; i >= 0; i -= 3)
                {
                    // only the triangles just added reference vertices >= vStart
                    if (t[i] < vStart || t[i - 1] < vStart || t[i - 2] < vStart) break;
                    var a = v[t[i - 2]]; var b = v[t[i - 1]]; var d = v[t[i]];
                    var n = Vector3.Cross(b - a, d - a);
                    if (Vector3.Dot(n, (a + b + d) / 3f - centre) < 0f) { int tmp = t[i - 1]; t[i - 1] = t[i]; t[i] = tmp; }
                }
            }
            public void Tube(Vector3 a, Vector3 b, float ra, float rb, Color col, int seg)
            {
                Vector3 axis = (b - a).normalized;
                Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized, w = Vector3.Cross(axis, u);
                int start = v.Count;
                for (int s = 0; s < seg; s++)
                {
                    float th = 2f * Mathf.PI * s / seg; var o = u * Mathf.Cos(th) + w * Mathf.Sin(th);
                    Add(a + o * ra, col); Add(b + o * rb, col);
                }
                var mid = (a + b) * 0.5f;
                for (int s = 0; s < seg; s++)
                {
                    int i0 = start + s * 2, i1 = start + ((s + 1) % seg) * 2;
                    t.Add(i0); t.Add(i0 + 1); t.Add(i1); t.Add(i1); t.Add(i0 + 1); t.Add(i1 + 1);
                }
                FixWindingAxis(start, a, b);
            }
            void FixWindingAxis(int vStart, Vector3 a, Vector3 b)
            {
                for (int i = t.Count - 1; i >= 0; i -= 3)
                {
                    if (t[i] < vStart || t[i - 1] < vStart || t[i - 2] < vStart) break;
                    var p0 = v[t[i - 2]]; var p1 = v[t[i - 1]]; var p2 = v[t[i]];
                    var n = Vector3.Cross(p1 - p0, p2 - p0);
                    var cen = (p0 + p1 + p2) / 3f; var axisPt = a + Vector3.Project(cen - a, (b - a).normalized);
                    if (Vector3.Dot(n, cen - axisPt) < 0f) { int tmp = t[i - 1]; t[i - 1] = t[i]; t[i] = tmp; }
                }
            }
            public void Cone(Vector3 from, Vector3 tip, float radius, Color col, int seg)
            {
                Vector3 axis = (tip - from).normalized;
                Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized, w = Vector3.Cross(axis, u);
                int start = v.Count;
                for (int s = 0; s < seg; s++) { float th = 2f * Mathf.PI * s / seg; Add(from + (u * Mathf.Cos(th) + w * Mathf.Sin(th)) * radius, col); }
                int tp = Add(tip, col);
                for (int s = 0; s < seg; s++) { t.Add(start + s); t.Add(start + (s + 1) % seg); t.Add(tp); }
                FixWindingAxis(start, from, tip);
            }
            public void Apply(Mesh m)
            {
                m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0);
                m.RecalculateNormals(); m.RecalculateBounds();
            }
        }
    }
}
