using System;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 25 (B2, Dan: "some of the tunnels are really long. Part of the underground network was to make it quicker. Is there a way
    // to make the tunnels quicker (either a seamless warp or a scripted boost that quickly pushes you through without control ... This
    // wouldn't be everywhere. for example races would move normally"). THE PIPE EXPRESS, on a few long pipes (Data/pipes/network.json
    // 'express', Tools/mapgen/pipe_network.py; the marks at their ends: MapBuilder.Network.cs). Free roam only, the player only:
    //   Boost (the default)  driving in at either end (into the stretch, faster than 3 m/s), the water surges (a splash, a rush), the
    //                        controls lock and the vehicle is carried along the floor's middle line, speeding up to 45 m/s (a whoosh,
    //                        speed streaks), then let go at the far end at 20 m/s facing on out of it. Carried, not driven (the body
    //                        is moved kinematically): it cannot hit a wall, stall or stick; the camera follows as ever (ChaseCamera)
    //   Warp                 a short dark fade at the entry, then the vehicle is at the far end, moving out of it at 20 m/s
    //   Off                  the stretches drive as any pipe
    // Chosen on the debug menu's "Pipe express" row (GameMenus). During any race it is off and the stretches drive normally; an AI
    // driver (RoadDriver) never takes it.
    // Round 26 (item 3, Dan: "like boost. Would like to add it in more places anytime there is any distance"): a stretch on every run of
    // pipe over about 100 m (pipe_network.py), each with its own top speed ('top': no ride longer than about 12 s; E1 goes 211 m/s). The
    // speed-up and slow-down scale with it (Accel; the same in pipe_network.py's ride_time), the heading is taken over a longer stretch of
    // floor at speed, and the camera rides the stretch's own line behind the vehicle (View), so it is steady however fast it goes.
    public sealed class PipeExpress : MonoBehaviour
    {
        [Serializable] public sealed class Stretch { public string id, name; public Vector3[] floor; public float radius, top; }   // Round 26: top, its own top speed (m/s; 0: BoostSpeed)
        public Stretch[] stretches;
        public Material streak, splash;
        public enum Mode { Boost, Warp, Off }
        public static Mode Setting = Mode.Boost;
        public static readonly string[] ModeNames = { "Boost", "Warp", "Off" };
        public static readonly Color MapColor = new Color(0.05f, 0.85f, 0.95f);
        public const float BoostSpeed = 45f, ExitSpeed = 20f, Surge = 30f;
        public static PipeExpress Instance { get; private set; }
        public static int Rides { get; private set; }            // checks
        public static string LastRide { get; private set; } = "";
        public static float LastRideTime { get; private set; }  // checks (Round 26): seconds from the surge to letting go
        public static float Accel(float top) => Mathf.Max(Surge, top / 2.5f);   // Round 26: the speed-up and slow-down (pipe_network.py express_accel)
        float Top => on != null && on.top > 0f ? on.top : BoostSpeed;
        float rideStart;

        ArcadeVehicle car; bool wasEnabled; Stretch on; bool forward; float s, v, h0, cool, len; float[] arc;
        bool warping; float fade, fadeOut; Vector3 lastPos;
        AudioSource rush, whoosh; ParticleSystem streaks, drops; Texture2D black;

        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; if (on != null) Release(); }

        void Start()
        {
            black = new Texture2D(1, 1); black.SetPixel(0, 0, Color.black); black.Apply();
            rush = gameObject.AddComponent<AudioSource>(); rush.clip = Rush(); rush.loop = true; rush.spatialBlend = 0f; rush.volume = 0f; rush.playOnAwake = false;
            whoosh = gameObject.AddComponent<AudioSource>(); whoosh.clip = Whoosh(); whoosh.spatialBlend = 0f; Racer.SoundMix.Track(whoosh, 0.8f, Racer.SoundMix.Kind.Effects); whoosh.playOnAwake = false;   /* Round 27: the Effects volume */
        }

        static bool Allowed => Setting != Mode.Off && (RaceController.Instance == null || RaceController.Instance.State == RaceController.Stage.Free);

        void FixedUpdate()
        {
            if (stretches == null) return;
            if (on != null) { if (warping) car.Body.MovePosition(lastPos); else Carry(Time.fixedDeltaTime); return; }
            cool -= Time.fixedDeltaTime;
            if (cool > 0f || !Allowed) return;
            foreach (var c in DrainPipe.Vehicles)
            {
                if (!c || (!c.enabled && !CheckRun.Active) || c.Body == null || c.Body.isKinematic || c.GetComponent<RoadDriver>() != null) continue;
                var p = c.Body.position; var vel = c.Body.linearVelocity;
                foreach (var st in stretches)
                {
                    if (st.floor == null || st.floor.Length < 3) continue;
                    foreach (bool fwd in new[] { true, false })
                    {
                        var end = fwd ? st.floor[0] : st.floor[st.floor.Length - 1];
                        var next = fwd ? st.floor[2] : st.floor[st.floor.Length - 3];
                        var dir = next - end; dir.y = 0f; dir.Normalize();
                        var rel = p - end; float along = Vector3.Dot(new Vector3(rel.x, 0f, rel.z), dir);
                        if (along < -0.5f || along > 3.5f) continue;
                        float lat = (new Vector3(rel.x, 0f, rel.z) - dir * along).magnitude;
                        if (lat > st.radius * 0.8f || rel.y < -0.5f || rel.y > st.radius * 1.4f) continue;
                        if (Vector3.Dot(vel, dir) < 3f) continue;
                        Begin(c, st, fwd);
                        return;
                    }
                }
            }
        }

        void Begin(ArcadeVehicle c, Stretch st, bool fwd)
        {
            car = c; on = st; forward = fwd;
            arc = new float[st.floor.Length]; for (int i = 1; i < arc.Length; i++) arc[i] = arc[i - 1] + Vector3.Distance(st.floor[i - 1], st.floor[i]);
            len = arc[arc.Length - 1]; s = 0f;
            v = Mathf.Max(8f, Vector3.Dot(c.Body.linearVelocity, (Point(1f) - Point(0f)).normalized));
            h0 = Mathf.Clamp(c.Body.position.y - Point(0f).y, 0.3f, 1.4f);
            wasEnabled = car.enabled; car.enabled = false; car.Body.isKinematic = true; lastPos = car.Body.position; rideStart = Time.time;
            c.SurfaceUp = Vector3.up;   // Round 26: off any pipe wall (DrainPipe eases it to the floor before an express's end)
            Rides++; LastRide = $"{st.id} {st.name} ({(fwd ? "forward" : "back")}, {Setting})";
            if (Setting == Mode.Warp) { warping = true; fade = 0f; return; }
            whoosh.Play(); rush.Play();
            Splash(car.Body.position, 60);
            Streaks(true);
        }

        // Round 26: as Point, carried on straight past either end (the camera's points near the ends: no swing as they stop there)
        Vector3 PointX(float x)
        {
            if (x < 0f) return Point(0f) + (Point(0f) - Point(2f)).normalized * -x;
            if (x > len) return Point(len) + (Point(len) - Point(len - 2f)).normalized * (x - len);
            return Point(x);
        }

        // the floor's middle at distance x along the ride (from the end it was entered at)
        Vector3 Point(float x)
        {
            float a = forward ? x : len - x; a = Mathf.Clamp(a, 0f, len);
            int i = Array.BinarySearch(arc, a); if (i < 0) i = ~i; i = Mathf.Clamp(i, 1, arc.Length - 1);
            return Vector3.Lerp(on.floor[i - 1], on.floor[i], Mathf.InverseLerp(arc[i - 1], arc[i], a));
        }

        void Carry(float dt)
        {
            if (!car || !car.Body) { on = null; return; }
            if ((car.Body.position - lastPos).sqrMagnitude > 25f) { Release(false); return; }   // a reset moved it: let go where it is
            // Round 26: the stretch's own top speed; slowing to ExitSpeed just in time for the far end (was: from 45 m out, at 45 m/s)
            float left = len - s, acc = Accel(Top), brake = (v * v - ExitSpeed * ExitSpeed) / (2f * acc) + 3f;
            float target = left < brake ? ExitSpeed : Top;
            v = Mathf.MoveTowards(v, target, acc * dt);
            s = Mathf.Min(len, s + v * dt);
            float w = Mathf.Clamp(v * 0.12f, 1.5f, 14f);   // Round 26: the heading over more floor at speed (no flick at each bend's joints)
            var p = Point(s); var t = (Point(Mathf.Min(len, s + w)) - Point(Mathf.Max(0f, s - w))).normalized;
            var pos = p + Vector3.up * h0; var rot = Quaternion.LookRotation(t, Vector3.up);
            car.Body.MovePosition(pos); car.Body.MoveRotation(rot); lastPos = pos;
            if (rush) rush.volume = Mathf.MoveTowards(rush.volume, 0.55f * Mathf.Clamp01(v / BoostSpeed) * Racer.SoundMix.Gain(Racer.SoundMix.Kind.Effects), dt * 2f);   // Round 27: the Effects volume
            LastRideTime = Time.time - rideStart;
            if (s >= len - 1e-3f) Release();
        }

        void Release(bool atEnd = true)
        {
            if (car && car.Body)
            {
                var t = (Point(len) - Point(Mathf.Max(0f, len - 3f))).normalized;
                if (atEnd) { var pos = Point(len) + Vector3.up * h0; car.Body.position = pos; car.transform.position = pos; car.Body.rotation = Quaternion.LookRotation(t, Vector3.up); }
                car.Body.isKinematic = false;
                if (atEnd) car.Body.linearVelocity = t * ExitSpeed;
                car.Body.angularVelocity = Vector3.zero;
                car.enabled = wasEnabled;   // a scripted check's drive keeps the car in its own hands
            }
            on = null; car = null; cool = 1.5f; warping = false; Streaks(false);
            if (rush) rush.volume = 0f;
        }

        // Round 26: the chase camera while carried (DrainPipe.View asks first): on the stretch's own axis 12 m behind, looking 14 m ahead,
        // as DrainPipe.View places it; it moves with the vehicle exactly (no lag to catch up at 200 m/s, no jump where the pipe's pieces meet)
        public static bool View(Transform target, out Vector3 desired, out Vector3 lookAt)
        {
            desired = lookAt = default;
            var x = Instance; if (x == null || x.on == null || x.warping || !x.car || x.car.transform != target) return false;
            float r = x.on.radius;
            desired = x.Point(x.s - 12f) + Vector3.up * (r + 0.4f); lookAt = x.PointX(x.s + 14f) + Vector3.up * r;   // where DrainPipe.View has it (the camera
            // waits at the stretch's start until the vehicle is 12 m in: carried on straight back from there it could leave a bending pipe)
            return true;
        }

        // Round 26 (item 4): how far ahead along a pipe the nearest express entry is, for a vehicle at p moving with vel (DrainPipe eases a
        // vehicle off the wall onto the floor before it). Free roam and the player only, as the express itself
        public static float EntryAhead(ArcadeVehicle c, Vector3 p, Vector3 vel, float within)
        {
            var x = Instance; if (x == null || x.stretches == null || x.on != null || !Allowed || c.GetComponent<RoadDriver>() != null) return float.MaxValue;
            float best = float.MaxValue;
            foreach (var st in x.stretches)
            {
                if (st.floor == null || st.floor.Length < 3) continue;
                foreach (bool fwd in new[] { true, false })
                {
                    var end = fwd ? st.floor[0] : st.floor[st.floor.Length - 1];
                    var next = fwd ? st.floor[2] : st.floor[st.floor.Length - 3];
                    var dir = next - end; dir.y = 0f; dir.Normalize();
                    var rel = p - end; float along = -Vector3.Dot(new Vector3(rel.x, 0f, rel.z), dir);   // how far short of the entry
                    if (along < -1f || along > within || Vector3.Dot(vel, dir) < 1f) continue;
                    if ((new Vector3(rel.x, 0f, rel.z) + dir * along).magnitude > st.radius + 1f || Mathf.Abs(rel.y) > st.radius * 2.5f) continue;
                    best = Mathf.Min(best, Mathf.Max(0f, along));
                }
            }
            return best;
        }

        void Update()
        {
            if (fadeOut > 0f) fadeOut = Mathf.Max(0f, fadeOut - Time.unscaledDeltaTime);
            if (on == null || !warping) return;
            // the warp: dark in 0.25 s, then at the far end moving out of it, light again over 0.35 s
            fade += Time.deltaTime;
            if (fade < 0.25f) return;
            var t = (Point(len) - Point(Mathf.Max(0f, len - 3f))).normalized; var pos = Point(len) + Vector3.up * h0;
            car.Body.position = pos; car.transform.position = pos; car.Body.rotation = Quaternion.LookRotation(t, Vector3.up); car.transform.rotation = car.Body.rotation;
            s = len; Release();
            var cam = FindAnyObjectByType<ChaseCamera>(); if (cam) cam.Snap();
            fadeOut = 0.35f;
        }

        void OnGUI()
        {
            float a = warping ? Mathf.Clamp01(fade / 0.25f) : fadeOut / 0.35f;
            if (a <= 0f || !black) return;
            var old = GUI.color; GUI.color = new Color(1f, 1f, 1f, a); GUI.depth = -1000;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), black); GUI.color = old;
        }

        // ---- effects
        void Splash(Vector3 at, int n)
        {
            if (!drops)
            {
                var go = new GameObject("Pipe express surge"); go.transform.SetParent(transform, false);
                drops = go.AddComponent<ParticleSystem>(); drops.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = drops.main; main.loop = false; main.playOnAwake = false; main.startLifetime = 0.6f; main.startSize = 0.12f; main.gravityModifier = 1.1f; main.maxParticles = 300; main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = drops.emission; em.enabled = false; var sh = drops.shape; sh.enabled = false;
                var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = splash; r.renderMode = ParticleSystemRenderMode.Billboard;
                drops.Play();
            }
            for (int i = 0; i < n; i++)
                drops.Emit(new ParticleSystem.EmitParams { position = at + UnityEngine.Random.insideUnitSphere * 1.2f, velocity = UnityEngine.Random.insideUnitSphere * 4f + Vector3.up * 3f, startSize = UnityEngine.Random.Range(0.08f, 0.2f), startLifetime = UnityEngine.Random.Range(0.4f, 0.8f) }, 1);
        }

        void Streaks(bool play)
        {
            var cam = Camera.main;
            if (play && cam && !streaks)
            {
                var go = new GameObject("Pipe express streaks"); go.transform.SetParent(cam.transform, false); go.transform.localPosition = new Vector3(0f, 0f, 14f);
                streaks = go.AddComponent<ParticleSystem>(); streaks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = streaks.main; main.loop = true; main.playOnAwake = false; main.startLifetime = 0.35f; main.startSpeed = 0f; main.startSize = 0.05f; main.maxParticles = 400; main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.startColor = new Color(0.85f, 0.97f, 1f, 0.55f);
                var em = streaks.emission; em.rateOverTime = 160f;
                var sh = streaks.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 3.2f; sh.radiusThickness = 0.35f;
                var vel = streaks.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local; vel.z = new ParticleSystem.MinMaxCurve(-55f); vel.x = new ParticleSystem.MinMaxCurve(0f); vel.y = new ParticleSystem.MinMaxCurve(0f);
                var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = streak; r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.06f; r.lengthScale = 2f;
            }
            if (!streaks) return;
            if (play) streaks.Play(); else streaks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // ---- sounds, made here: the rush (rushing water, looping) and the whoosh at the start (a rising sweep of noise)
        static AudioClip Rush()
        {
            const int rate = 44100; int n = rate * 2; var d = new float[n]; var rnd = new System.Random(2501); float a = 0f, b = 0f;
            for (int i = 0; i < n; i++) { float w = (float)rnd.NextDouble() * 2f - 1f; a += (w - a) * 0.25f; b += (a - b) * 0.08f; d[i] = (a - b) * 0.9f * (0.85f + 0.15f * Mathf.Sin(i * 2f * Mathf.PI * 3f / n)); }
            int fadeN = rate / 20; for (int i = 0; i < fadeN; i++) { float t = i / (float)fadeN; d[i] = d[i] * t + d[n - fadeN + i] * (1f - t); }
            var clip = AudioClip.Create("Pipe express rush", n - fadeN, 1, rate, false); var d2 = new float[n - fadeN]; Array.Copy(d, d2, n - fadeN); clip.SetData(d2, 0); return clip;
        }
        static AudioClip Whoosh()
        {
            const int rate = 44100; int n = (int)(rate * 1.3f); var d = new float[n]; var rnd = new System.Random(2502); float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n, k = Mathf.Lerp(0.03f, 0.5f, Mathf.Sqrt(t)); float w = (float)rnd.NextDouble() * 2f - 1f; lp += (w - lp) * k;
                d[i] = lp * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 1.6f)) * (1f - t * 0.4f) * 0.9f;
            }
            var clip = AudioClip.Create("Pipe express whoosh", n, 1, rate, false); clip.SetData(d, 0); return clip;
        }
    }
}
