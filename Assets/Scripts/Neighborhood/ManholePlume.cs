using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 40 (A1 / A2, Dan 2026-10-17 12:45: "a manhole cover that blows up as the first driver gets close to it and you can't bypass the plume.
    // This way it will look like you drive straight but won't matter because you can't miss the water plume"): a street manhole whose cover blows
    // off (Round 36 D1's bang, water and flying cover: ManholeLid.BlowOpen) and a plume of water erupts that covers the whole carriageway, from
    // past its right-hand curb to the far side of the median ('right' / 'left' of the way it runs, 'halfLength' along it). A vehicle that
    // drives into it is thrown on an aimed flight (GuidedFlight) to 'target', whatever its line in: on New Chastain Road, west over the hollow
    // and house 706 onto the forward pile's ramp face at Chastain Ridge Drive's dead end. Traffic and police cars go through it unthrown.
    // World: the cover and the plume. When it blows is the race's (a route lists the plumes it arms in a direction: plumes / plumes_rev):
    //   - armed in a race (the reverse Chastain Ridge): it blows when the first racer comes within 'trigger' of it (or drives into it first),
    //     and gushes for the rest of the race;
    //   - in a race that does not arm it (the forward Chastain Ridge, every other route): it never blows;
    //   - Free Roam (Getaway and the other activities included): it never blows, the cover stays shut (Round 42, BUG-003, Dan: "thought this
    //     only come on during the race...i am in free roam"; Round 40 had it blow in Free Roam too).
    // Round 43 (A1, Dan BUG-001: "you can go to the left of this and miss the hydrant"): a second cover in the southeast-bound carriageway,
    // abeam the first, in the same 'group': whichever blows first blows the other, and the two waters together cover both carriageways,
    // the median and the verge beyond the far curb, so there is no way past. Both throw to the same landing.
    public sealed class ManholePlume : MonoBehaviour
    {
        public string id;
        public string group;   // Round 43 (A1): the covers of one group blow together (New Chastain Road: one in each carriageway)
        public ManholeLid cover;
        public Vector3 along = Vector3.forward;   // the way the carriageway runs here (flat, unit)
        public float right = 5f, left = 9f, halfLength = 3.5f, angle = 40f, trigger = 80f, height = 16f;
        public Vector3 target, targetDir = Vector3.forward;
        public Material sprayMaterial;
        public static int Throws { get; private set; }
        public static readonly List<ManholePlume> All = new List<ManholePlume>();
        public bool Erupting => left0 > 0f;
        public bool Armed { get; private set; }
        public float EruptedAt { get; private set; } = -1f;   // checks: Time.time it blew
        float left0; bool racing; GameObject water; readonly Dictionary<Rigidbody, float> recent = new Dictionary<Rigidbody, float>();

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); Shut(); }

        Vector3 Side => Vector3.Cross(Vector3.up, along).normalized;   // to the right of the way it runs
        // where p is: along the way (- before it), and to the right (+) of its middle
        void Where(Vector3 p, out float a, out float s) { var d = p - transform.position; a = d.x * along.x + d.z * along.z; s = Vector3.Dot(d, Side); }
        public bool InPlume(Vector3 p)
        {
            Where(p, out float a, out float s); float dy = p.y - transform.position.y;
            return a >= -halfLength && a <= halfLength && s <= right && s >= -left && dy > -2f && dy < 4f;
        }
        static bool Thrown(ArcadeVehicle c) => c && !c.GetComponent<TrafficDriver>() && !c.GetComponent<CopDriver>();

        bool ArmedBy(RaceController race)
        {
            var rd = race.RouteData; if (rd == null || race.Route == null) return false;
            var ids = race.Route.Reverse ? rd.plumes_rev : rd.plumes;
            return ids != null && System.Array.IndexOf(ids, id) >= 0;
        }

        public void Erupt(float seconds)
        {
            if (Erupting) { left0 = Mathf.Max(left0, seconds); return; }
            Erupted?.Invoke(this, seconds);   // Round 57 (F): the replay
            left0 = seconds; EruptedAt = Time.time;
            if (cover) cover.BlowOpen(seconds > 1e5f ? 1e7f : seconds);
            BreakSounds.Play(transform.position, 24f, BreakSounds.Kind.Hydrant);
            water = Curtain();
            foreach (var o in All.ToArray()) if (o != this && !string.IsNullOrEmpty(group) && o.group == group && o.Armed) o.Erupt(seconds);
        }
        // Round 57 (F): a race replay's plumes
        public static event System.Action<ManholePlume, float> Erupted;
        public static void ReplayErupt(Vector3 at, float seconds) { foreach (var m in All) if (m && (m.transform.position - at).sqrMagnitude < 4f) { m.Erupt(seconds); return; } }
        public static void ShutAll() { foreach (var m in All.ToArray()) if (m && m.Erupting) m.Shut(); }
        public void Shut()
        {
            left0 = 0f; EruptedAt = -1f;
            if (water) Destroy(water);
            if (cover) cover.CloseNow();
        }

        void FixedUpdate()
        {
            var race = RaceController.Instance;
            bool r = race && race.State != RaceController.Stage.Free && race.RouteData != null;
            if (r != racing) { racing = r; Shut(); }
            Armed = r && ArmedBy(race);
            if (!Armed) { if (Erupting) Shut(); return; }   // Round 42 (BUG-003): only a race that arms it; never in Free Roam
            if (!Erupting)
                foreach (var e in race.Entrants)
                {
                    if (e == null || !e.Car) continue;
                    Where(e.Car.Body.position, out float a, out float s);
                    if (a < halfLength && a > -trigger && Mathf.Abs(s) < 25f) { Erupt(1e9f); break; }
                }
            foreach (var c in DrainPipe.Vehicles)
            {
                if (!c || c.Body == null || c.Body.isKinematic || !Thrown(c) || !InPlume(c.Body.position)) continue;
                // a racer that gets here before it has blown (the first one) still gets it: it blows now
                if (!Erupting) Erupt(1e9f);
                if (recent.TryGetValue(c.Body, out float at) && Time.time - at < 2.5f) continue;
                var f = c.GetComponent<GuidedFlight>(); if (f && f.Flying) continue;
                recent[c.Body] = Time.time;
                GuidedFlight.Begin(c, "plume " + id, transform.position.y, target, target - c.Body.position, target - c.Body.position, angle);
                Throws++;
                BreakSounds.Play(c.Body.position, 14f, BreakSounds.Kind.Hydrant);
            }
        }

        // the water: a curtain across the whole carriageway (a box of spray shot up), a column over the hole, and mist round them
        GameObject Curtain()
        {
            var go = new GameObject("Manhole plume water " + id); go.transform.SetParent(transform, false);
            float w = right + left, up = Mathf.Sqrt(2f * 9.81f * height);
            var mid = Side * ((right - left) * .5f);
            Spray(go.transform, mid, new Vector3(w, .2f, halfLength * 1.6f), 1100f, 2.6f, .45f, 1.0f, new Color(.88f, .94f, 1f, .85f), up * .8f, up * 1.0f);
            Spray(go.transform, Vector3.zero, new Vector3(1.2f, .2f, 1.2f), 500f, 3.0f, .4f, .9f, new Color(.9f, .95f, 1f, .95f), up * 1.0f, up * 1.15f);
            Spray(go.transform, mid, new Vector3(w + 2f, .2f, halfLength * 2.4f), 220f, 2.2f, 1.0f, 2.2f, new Color(.93f, .96f, 1f, .45f), up * .35f, up * .6f);
            return go;
        }
        void Spray(Transform parent, Vector3 offset, Vector3 box, float rate, float life, float s0, float s1, Color col, float v0, float v1)
        {
            var go = new GameObject("spray"); go.transform.SetParent(parent, false); go.transform.localPosition = offset;
            go.transform.rotation = Quaternion.LookRotation(along, Vector3.up);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = life; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 4000; main.gravityModifier = 1f; main.startSpeed = new ParticleSystem.MinMaxCurve(v0, v1);
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(box.x, box.z, box.y); sh.rotation = new Vector3(-90f, 0f, 0f);
            var rd = go.GetComponent<ParticleSystemRenderer>(); rd.sharedMaterial = sprayMaterial ? sprayMaterial : Geyser.SprayMaterial; rd.renderMode = ParticleSystemRenderMode.Billboard;
            rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rd.receiveShadows = false;
            ps.Play();
        }
    }
}
