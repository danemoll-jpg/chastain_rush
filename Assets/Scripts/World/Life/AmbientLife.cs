using System;
using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 38 (Part E; Dan picked "Life in the world"): Racer's AmbientLife (residents doing their actions: a football thrown round a
    // triangle, two with coffee mugs talking, people walking by the road), ported to this map. Racer authors a few dozen places on one street; this map
    // is about eight times Woodstock's, so the map build bakes groups everywhere they fit (MapBuilder.Life.cs: yards, porches, driveways, the pool decks
    // at Chastain Lakes and the Enclave, the shops' walks) and a pool of figures is lent to the groups near the player (and split screen's other view):
    //   who is out: LifeSchedule (time of day and weather; new: Racer's HouseholdSchedule is a save-backed shuffle of its Dan / Kyle visits, left
    //     behind with them); a group comes and goes only out of view (no popping), as Racer's Wildlife picks its animals
    //   the figures: Racer's ambient residents as they are (capsules and spheres, its shirts; no colliders), its actions: the football trio, the
    //     coffee pair, the walkers, the gesturers (Racer's smoking pair at Kyle's and the Dan / Kyle / brother figures are left behind)
    //   a vehicle coming: Racer's residents do not react; here (they have no colliders, and a vehicle passing through one looks wrong) they turn to
    //     watch a vehicle near them and step smartly out of the way of one coming at them, then walk back
    //   never on a road, a race or shortcut line, a jump or the stunt park (the map build keeps them off), never in a race's way
    // The cost: one pool (PoolSize figures), their animation at 30 Hz and none past 220 m (Racer's), each figure culled by its LODGroup when tiny.
    public sealed class AmbientLife : MonoBehaviour
    {
        public enum Kind : byte { Yard, Porch, Drive, Pool, Shop }
        [Serializable] public struct Group { public Vector3 at; public float yaw; public Kind kind; public byte count; public byte action; public Vector3 walk; }   // action 0 football, 1 coffee, 3 walkers, 4 gesturing; walk: the walkers' way (direction x half length)
        public Group[] groups = new Group[0];
        public const int PoolSize = 36; public const float Near = 150f, Far = 200f;
        public static AmbientLife Current { get; private set; }
        public int Out { get; private set; }          // checks: groups out now (with figures)
        public int Dodges { get; private set; }       // checks: times a resident stepped out of a vehicle's way
        sealed class Person { public Transform root, arm, otherArm, leftLeg, rightLeg, prop; public int group = -1, slot; public Vector3 home, dodgeTo, dodgeFrom; public float phase, dodgeAt = -10f; }
        readonly List<Person> people = new(); readonly Dictionary<int, List<Person>> active = new(); readonly List<Transform> balls = new();
        internal Material skin, hair, trousers, propMat; internal Material[] shirts, skins;
        float nextPick, nextAnimation;
        Material Mat(Color c) => new(Shader.Find("Universal Render Pipeline/Lit")) { color = c, enableInstancing = true };
        internal Transform Part(Transform parent, string name, PrimitiveType type, Vector3 p, Vector3 scale, Material mat)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, false); g.transform.localPosition = p; g.transform.localScale = scale; g.GetComponent<Collider>().enabled = false; Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial = mat; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return g.transform;
        }
        void Awake()
        {
            Current = this;
            skins = new[] { Mat(new(.62f, .40f, .26f)), Mat(new(.85f, .66f, .52f)), Mat(new(.42f, .27f, .17f)), Mat(new(.74f, .53f, .38f)) };
            skin = skins[0]; hair = Mat(new(.12f, .08f, .05f)); trousers = Mat(new(.16f, .20f, .26f)); propMat = Mat(new(.83f, .79f, .67f));
            shirts = new[] { Mat(new(.5f, .18f, .12f)), Mat(new(.17f, .32f, .47f)), Mat(new(.56f, .49f, .19f)), Mat(new(.28f, .39f, .24f)), Mat(new(.85f, .85f, .82f)), Mat(new(.45f, .25f, .50f)) };
            for (int i = 0; i < PoolSize; i++) Create(i);
            for (int i = 0; i < 4; i++) { var b = Part(transform, "Football", PrimitiveType.Sphere, Vector3.zero, new(.36f, .21f, .21f), shirts[0]); b.gameObject.SetActive(false); balls.Add(b); }
        }
        void OnDestroy() { if (Current == this) Current = null; foreach (var m in shirts.Concat(skins).Concat(new[] { hair, trousers, propMat })) if (m) Destroy(m); }
        // Racer's ambient resident (AmbientLife.Create), with a mug in its hand (put away unless its group's action has one)
        void Create(int n)
        {
            var p = new Person { root = new GameObject("Ambient resident").transform, phase = n * 1.713f }; p.root.SetParent(transform, false);
            float s = .92f + n % 3 * .08f; p.root.localScale = new Vector3(s, .96f + n % 3 * .04f, 1f);
            var shirt = shirts[n % shirts.Length]; var sk = skins[(n * 7) % skins.Length];
            Part(p.root, "Torso", PrimitiveType.Capsule, new(0, 1.13f, 0), new(.42f, .34f, .28f), shirt);
            Part(p.root, "Head", PrimitiveType.Sphere, new(0, 1.63f, 0), new(.28f, .32f, .29f), sk);
            Part(p.root, "Hair", PrimitiveType.Sphere, new(0, 1.75f, -.015f), new(.29f, .13f, .29f), hair);
            p.leftLeg = Part(p.root, "Left leg", PrimitiveType.Capsule, new(-.12f, .45f, 0), new(.16f, .40f, .18f), trousers);
            p.rightLeg = Part(p.root, "Right leg", PrimitiveType.Capsule, new(.12f, .45f, 0), new(.16f, .40f, .18f), trousers);
            foreach (int side in new[] { -1, 1 }) Part(p.root, "Shoe", PrimitiveType.Cube, new(side * .12f, .065f, .04f), new(.19f, .13f, .30f), hair);
            p.arm = new GameObject("Gesture shoulder").transform; p.arm.SetParent(p.root, false); p.arm.localPosition = new(.24f, 1.36f, 0);
            Part(p.arm, "Sleeve", PrimitiveType.Capsule, new(0, -.16f, 0), new(.15f, .19f, .15f), shirt);
            Part(p.arm, "Hand", PrimitiveType.Sphere, new(0, -.39f, .01f), new(.13f, .14f, .13f), sk);
            p.otherArm = Part(p.root, "Other arm", PrimitiveType.Capsule, new(-.26f, 1.08f, 0), new(.15f, .26f, .15f), shirt);
            p.prop = Part(p.arm, "Coffee mug", PrimitiveType.Cylinder, new(0, -.37f, .12f), new(.13f, .095f, .13f), propMat); Part(p.prop, "Handle", PrimitiveType.Cube, new(.65f, 0, 0), new(.4f, .65f, .3f), propMat);
            var lod = p.root.gameObject.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.004f, p.root.GetComponentsInChildren<Renderer>()) }); lod.RecalculateBounds();
            p.root.gameObject.SetActive(false); people.Add(p);
        }

        // ---------------------------------------------------------------- who is out, near which view
        readonly List<Vector3> views = new(); readonly List<Camera> cams = new();
        public string Info { get; private set; } = "";   // checks: the last pick
        public IEnumerable<int> OutGroups => active.Keys;   // checks
        void Views()
        {
            views.Clear(); cams.Clear();
            foreach (var cam in Camera.allCameras) if (cam && cam.isActiveAndEnabled && !cam.targetTexture) { views.Add(cam.transform.position); cams.Add(cam); }
            if (views.Count == 0 && Camera.main) { views.Add(Camera.main.transform.position); cams.Add(Camera.main); }
        }
        bool InView(Vector3 p)
        {
            foreach (var cam in cams)
            {
                if (!cam || !cam.isActiveAndEnabled) continue;
                var v = cam.WorldToViewportPoint(p + Vector3.up); if (v.z > 0 && v.z < 260f && v.x > -.15f && v.x < 1.15f && v.y > -.25f && v.y < 1.25f) return true;
            }
            return false;
        }
        float NearestView(Vector3 p) { float d = float.MaxValue; foreach (var v in views) d = Mathf.Min(d, (v - p).sqrMagnitude); return Mathf.Sqrt(d); }
        void Pick()
        {
            Views(); if (views.Count == 0) return;
            float hour = WorldLook.Current ? WorldLook.Current.Hour : 12f; var pr = WorldLook.Current ? WorldLook.Current.Preset : null;
            bool wet = pr != null && (pr.rain > .5f || pr.snowfall > .5f); int day = WorldLook.Current ? WorldLook.Current.Day : 1;
            bool underground = Camera.main && DrainPipe.Inside(Camera.main.transform.position, out _) != null;
            // groups out that are now too far or no longer out go (when not in view)
            foreach (var g in active.Keys.ToList())
            {
                var gr = groups[g]; bool keep = !underground && NearestView(gr.at) < Far && LifeSchedule.Out(gr.kind, g, hour, day, wet);
                if (keep || InView(gr.at)) continue;
                foreach (var p in active[g]) { p.group = -1; p.root.gameObject.SetActive(false); }
                active.Remove(g);
            }
            if (underground) { Out = active.Count; return; }
            // new groups near a view, out of sight, while figures are free
            int free = people.Count(p => p.group < 0), near = 0, outNear = 0, seen = 0;
            if (free > 0)
            {
                var cand = new List<(int g, float d)>();
                for (int g = 0; g < groups.Length; g++)
                {
                    if (active.ContainsKey(g)) continue; var gr = groups[g];
                    float d = NearestView(gr.at); if (d > Near) continue; near++;
                    if (!LifeSchedule.Out(gr.kind, g, hour, day, wet)) continue; outNear++;
                    cand.Add((g, d));
                }
                foreach (var (g, _) in cand.OrderBy(c => c.d))
                {
                    var gr = groups[g]; if (gr.count > free) continue; if (InView(gr.at)) { seen++; continue; }
                    if (gr.action == 0 && !balls.Any(b => !b.gameObject.activeSelf)) continue;
                    var list = new List<Person>();
                    for (int k = 0; k < gr.count; k++)
                    {
                        var p = people.First(x => x.group < 0); p.group = g; p.slot = k; p.home = Home(gr, k); p.dodgeAt = -10f;
                        p.root.position = p.home; p.root.rotation = Quaternion.Euler(0, gr.yaw, 0);
                        p.prop.gameObject.SetActive(gr.action == 1); p.root.gameObject.SetActive(true); list.Add(p);
                    }
                    if (gr.action == 0) balls.First(b => !b.gameObject.activeSelf).gameObject.SetActive(true);
                    active[g] = list; free -= gr.count; if (free <= 0) break;
                }
            }
            // balls in use: one per football trio out
            int trios = active.Keys.Count(g => groups[g].action == 0); int on = 0; foreach (var b in balls) { if (b.gameObject.activeSelf && on >= trios) b.gameObject.SetActive(false); else if (b.gameObject.activeSelf) on++; }
            Out = active.Count; Info = $"hour {hour:0.0}{(wet ? " wet" : "")}, views {views.Count}, {near} groups within {Near:0} m, {outNear} of them out by the hour, {seen} waiting out of sight, {active.Count} out, {free} figures free";
        }
        // where member k of a group stands: a pair 1.6 m apart facing each other, a trio on a triangle 6 m across, walkers along their way
        static Vector3 Home(Group g, int k)
        {
            var f = Quaternion.Euler(0, g.yaw, 0) * Vector3.forward; var r = Vector3.Cross(Vector3.up, f);
            if (g.action == 0) { float a = k * 2.0944f; return g.at + (f * Mathf.Cos(a) + r * Mathf.Sin(a)) * 3.2f; }
            if (g.action == 3) return g.at + g.walk * (k == 0 ? -.4f : .4f) + r * (k == 0 ? .4f : -.4f);
            return g.at + r * (g.count == 1 ? 0f : k == 0 ? -.8f : .8f);
        }
        static float Ground(Vector3 p) { return Physics.Raycast(p + Vector3.up * 2.5f, Vector3.down, out var hit, 6f, 1, QueryTriggerInteraction.Ignore) ? hit.point.y + .02f : p.y; }

        // ---------------------------------------------------------------- Racer's animation, and the vehicles
        public static double Ms; static readonly System.Diagnostics.Stopwatch watch = new();   // checks: this script's time, summed (ms)
        void Update() { watch.Restart(); Step(); Ms += watch.Elapsed.TotalMilliseconds; }
        void Step()
        {
            if (Time.timeScale == 0) return;
            if (Time.unscaledTime >= nextPick) { nextPick = Time.unscaledTime + .5f; Pick(); }
            if (Time.time < nextAnimation) return; nextAnimation = Time.time + 1 / 30f;
            var cars = DrainPipe.Vehicles;
            foreach (var kv in active)
            {
                var gr = groups[kv.Key]; var list = kv.Value;
                if (NearestView(gr.at) > 220f) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    var p = list[i]; float t = Time.time + p.phase; float gesture = .5f + .5f * Mathf.Sin(t * .9f);
                    // a vehicle near: watch it; one coming at them: step out of its way (and back after)
                    ArcadeVehicle near = null; float nd = 15f; foreach (var c in cars) { if (!c || !c.Body) continue; float d = Vector3.Distance(c.Body.position, p.root.position); if (d < nd) { nd = d; near = c; } }
                    if (near && Time.time - p.dodgeAt > 4f)
                    {
                        var v = near.Body.linearVelocity; v.y = 0; var to = p.root.position - near.Body.position; to.y = 0;
                        if (v.magnitude > 3f && nd < 6f + v.magnitude * .8f && Vector3.Dot(v.normalized, to.normalized) > .6f)
                        {
                            var side = Vector3.Cross(Vector3.up, v.normalized); if (Vector3.Dot(side, to) < 0) side = -side;
                            p.dodgeAt = Time.time; p.dodgeFrom = p.root.position; p.dodgeTo = p.root.position + side * 3.2f; Dodges++;
                        }
                    }
                    float since = Time.time - p.dodgeAt;
                    if (since < 4f)
                    {
                        // a quick sidestep (0.5 s), a moment, then back home (1.5 s)
                        var at = since < .5f ? Vector3.Lerp(p.dodgeFrom, p.dodgeTo, since / .5f) : since < 2.5f ? p.dodgeTo : Vector3.Lerp(p.dodgeTo, p.home, (since - 2.5f) / 1.5f);
                        at.y = Ground(at); p.root.position = at;
                        float step = Mathf.Sin(t * 14) * (since < .5f || since > 2.5f ? 25 : 0); p.leftLeg.localRotation = Quaternion.Euler(step, 0, 0); p.rightLeg.localRotation = Quaternion.Euler(-step, 0, 0);
                        if (near) { var look = near.Body.position - p.root.position; look.y = 0; if (look.sqrMagnitude > .01f) p.root.rotation = Quaternion.LookRotation(look); }
                        continue;
                    }
                    Vector3 face;
                    if (gr.action == 0) face = list[(i + 1) % list.Count].home;
                    else if (gr.count == 2 && gr.action != 3) face = list[1 - i].home;
                    else face = p.home + Quaternion.Euler(0, gr.yaw, 0) * Vector3.forward;
                    if (near && nd < 12f && near.Body.linearVelocity.magnitude > 2f) face = near.Body.position;   // watching it go by
                    var dir = face - p.root.position; dir.y = 0;
                    if (gr.action != 3 && dir.sqrMagnitude > .01f) p.root.rotation = Quaternion.Euler(0, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + Mathf.Sin(t * .6f) * 9, 0);
                    p.arm.localRotation = Quaternion.Euler(gr.action == 0 ? -75 - 25 * Mathf.Sin(t * 1.6f) : gr.action == 1 ? -20 - 115 * Mathf.Pow(gesture, 5) : Mathf.Sin(t * 2) * 18, 0, 0);
                    if (gr.action == 0)
                    {
                        int thrower = Mathf.FloorToInt(Time.time / 2.6f) % list.Count; float moment = Mathf.Repeat(Time.time / 2.6f, 1); bool catchNext = i == (thrower + 1) % list.Count;
                        p.arm.localRotation = Quaternion.Euler(i == thrower ? -70 - 65 * Mathf.Sin(Mathf.Clamp01(moment / .35f) * Mathf.PI) : catchNext ? -70 - 30 * Mathf.Sin(moment * Mathf.PI) : -15, 0, 0); p.otherArm.localRotation = Quaternion.Euler(catchNext ? -60 : 0, 0, 0);
                    }
                    if (gr.action == 3)
                    {
                        // walking their way and back (Racer's roadside walk, longer)
                        float walk = Mathf.Sin(t * .12f); var point = p.home + gr.walk * walk; point.y = Ground(point); p.root.position = point;
                        var wd = gr.walk * (Mathf.Cos(t * .12f) > 0 ? 1 : -1); if (wd.sqrMagnitude > .01f) p.root.rotation = Quaternion.LookRotation(new Vector3(wd.x, 0, wd.z));
                        p.leftLeg.localRotation = Quaternion.Euler(Mathf.Sin(t * 5) * 22, 0, 0); p.rightLeg.localRotation = Quaternion.Euler(-Mathf.Sin(t * 5) * 22, 0, 0);
                    }
                    else { p.leftLeg.localRotation = p.rightLeg.localRotation = Quaternion.identity; if ((p.root.position - p.home).sqrMagnitude > .01f) p.root.position = p.home; }
                }
                if (gr.action == 0 && list.Count == 3)
                {
                    int bi = 0; foreach (var g in active.Keys) { if (g == kv.Key) break; if (groups[g].action == 0) bi++; }
                    var ball = balls.Where(b => b.gameObject.activeSelf).Skip(bi).FirstOrDefault();
                    if (ball) { float cycle = Time.time / 2.6f; int k = Mathf.FloorToInt(cycle) % 3; float u = Mathf.Clamp01((Mathf.Repeat(cycle, 1) - .20f) / .65f); ball.position = Vector3.Lerp(list[k].root.position, list[(k + 1) % 3].root.position, u) + Vector3.up * (.95f + Mathf.Sin(u * Mathf.PI) * 1.1f); ball.rotation = Quaternion.Euler(Time.time * 190, 0, 45); }
                }
            }
        }
    }

    // Chastain Rush, Round 38 (Part E): who is out, by the time of day and the weather (new: Racer's HouseholdSchedule is a save-backed shuffle bag of
    // its Dan / Kyle visits). A share of each kind of place is out, each group decided by its own number per day and hour (so a group stays or goes
    // with the hour, not at random every frame); fewer at night; nobody in the yards, on the pool decks or the drives in rain or snow (the porches
    // and the shops' walks keep a few).
    public static class LifeSchedule
    {
        public static float Share(AmbientLife.Kind kind, float hour, bool wet)
        {
            float day = hour >= 9 && hour < 18 ? 1f : hour >= 7 && hour < 9 ? .5f : hour >= 18 && hour < 21 ? .7f : hour >= 21 && hour < 23 ? .2f : .03f;
            switch (kind)
            {
                case AmbientLife.Kind.Yard: return wet ? 0f : day * .45f;
                case AmbientLife.Kind.Porch: return (wet ? .3f : 1f) * Mathf.Max(day, hour >= 21 && hour < 23 ? .25f : 0f) * .35f;
                case AmbientLife.Kind.Drive: return wet ? 0f : day * .3f;
                case AmbientLife.Kind.Pool: return wet || hour < 10 || hour >= 19 ? 0f : .9f;
                default: return (wet ? .3f : 1f) * day * .6f;   // the shops' walks
            }
        }
        public static bool Out(AmbientLife.Kind kind, int group, float hour, int day, bool wet)
        {
            unchecked { uint h = (uint)(group * 73856093) ^ (uint)(day * 19349663) ^ (uint)((int)hour * 83492791); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (h & 0xffff) / 65535f < Share(kind, hour, wet); }
        }
    }
}
