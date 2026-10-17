using System;
using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 38 (Part E): Racer's Wildlife (a songbird, a squirrel, a frog, a deer, a coyote, wild turkeys: all of them live in Georgia's
    // suburbs and woods; Racer has no rabbits, so none here), ported: its animals as they are (primitives, no colliders), their idling and walking,
    // their calls (Racer's recordings, Assets/Audio/Wildlife), fleeing a vehicle that comes within 22 m (birds) or 15 m along their escape (away from
    // the nearest road or trail), retired once gone and out of sight. Racer authors a few habitats along its course and picks up to 8 of them a
    // visit; here the map build bakes habitats across the map (MapBuilder.Life.cs: the woods along Noonday Creek and the green spaces, the yards'
    // edges for birds and squirrels, the ponds' banks for frogs; turkeys in threes), and the animals are lent to the habitats near the views, up to
    // Cap at once, picked out of sight (Racer's rule: never pop into view). Day and night (new): songbirds and squirrels by day, frogs from dusk,
    // deer and turkeys by day and dusk, coyotes at dusk and night; songbirds and squirrels shelter in rain and snow. Never underground.
    public sealed class Wildlife : MonoBehaviour
    {
        public enum Species { Bird, Squirrel, Frog, Deer, Coyote, Turkey }
        [Serializable] public struct Habitat { public Species species; public Vector3 position, escape; }
        public Habitat[] habitats = Array.Empty<Habitat>();
        public AudioClip[] birdCalls, squirrelCalls, frogCalls, deerCalls, coyoteCalls;
        public const int Cap = 10; public const float Near = 170f, Far = 230f;
        public static Wildlife Current { get; private set; }
        public int Sightings { get; private set; }
        public int Calls { get; private set; }
        public int Flights { get; private set; }   // checks: animals that fled a vehicle
        public int ActiveCount { get { int n = 0; foreach (var a in animals) if (a.site >= 0) n++; return n; } }
        public static float QuietUntil;
        sealed class Animal { public Species species; public Transform root, head, tail, left, right; public Transform[] legs; public int site = -1; public bool seen; public float phase, flee = -100, nextCall; public Vector3 start; }
        readonly List<Animal> animals = new(); readonly List<Material> materials = new(); readonly HashSet<int> used = new(), retired = new();
        AudioSource voice; System.Random rng = new(62061); float nextThink, nextVoice, nextPick; AudioClip turkeyCall;
        float Range(float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
        Material Mat(Color c) { var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c, enableInstancing = true }; materials.Add(m); return m; }
        Transform Part(Transform parent, string name, Vector3 p, Vector3 scale, Material mat, PrimitiveType type = PrimitiveType.Sphere)
        { var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, false); g.transform.localPosition = p; g.transform.localScale = scale; g.GetComponent<Collider>().enabled = false; Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial = mat; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return g.transform; }
        Material blue, chest, brown, tailM, green, cream, black, beak, coyoteFur, feather, bronze, red;
        void Awake()
        {
            Current = this; QuietUntil = 0;
            blue = Mat(new(.12f, .30f, .48f)); chest = Mat(new(.75f, .40f, .19f)); brown = Mat(new(.36f, .19f, .09f)); tailM = Mat(new(.48f, .27f, .13f)); green = Mat(new(.25f, .42f, .10f)); cream = Mat(new(.77f, .76f, .44f)); black = Mat(new(.015f, .02f, .015f)); beak = Mat(new(.68f, .51f, .15f)); coyoteFur = Mat(new(.43f, .40f, .31f));
            feather = Mat(new(.17f, .12f, .075f)); bronze = Mat(new(.38f, .25f, .12f)); red = Mat(new(.68f, .08f, .07f));
            // the pool: of each species enough for the cap (turkeys come in threes)
            foreach (Species s in Enum.GetValues(typeof(Species))) { if (!habitats.Any(h => h.species == s)) continue; int n = s == Species.Turkey ? 3 : s == Species.Bird || s == Species.Squirrel ? 4 : 2; for (int i = 0; i < n; i++) animals.Add(Build(s)); }
            voice = new GameObject("Wildlife spatial voice").AddComponent<AudioSource>(); voice.transform.SetParent(transform, false); voice.playOnAwake = false; voice.spatialBlend = 1; voice.rolloffMode = AudioRolloffMode.Linear; voice.minDistance = 12; voice.maxDistance = 65; voice.dopplerLevel = 0; voice.priority = 80;
            turkeyCall = TurkeyVoice.Create();
        }
        void OnDestroy() { if (Current == this) Current = null; foreach (var m in materials) if (m) Destroy(m); if (turkeyCall) Destroy(turkeyCall); }
        // Racer's animals (Wildlife.Awake), one each
        Animal Build(Species species)
        {
            var a = new Animal { species = species, root = new GameObject("Wildlife " + species).transform, phase = animals.Count * 2.39996f }; a.root.SetParent(transform, false);
            if (species == Species.Bird)
            {
                Part(a.root, "Blue songbird body", new(0, .32f, 0), new(.30f, .34f, .52f), blue); Part(a.root, "Rust breast", new(0, .31f, .16f), new(.25f, .27f, .24f), chest);
                a.head = Part(a.root, "Bird head", new(0, .55f, .20f), Vector3.one * .25f, blue); Part(a.root, "Pointed beak", new(0, .53f, .37f), new(.08f, .07f, .20f), beak);
                a.left = Part(a.root, "Left wing", new(-.18f, .34f, -.06f), new(.16f, .10f, .43f), blue); a.right = Part(a.root, "Right wing", new(.18f, .34f, -.06f), new(.16f, .10f, .43f), blue);
                a.tail = Part(a.root, "Tail feathers", new(0, .29f, -.35f), new(.22f, .055f, .32f), blue); a.tail.localRotation = Quaternion.Euler(-20, 0, 0);
                foreach (int side in new[] { -1, 1 }) Part(a.root, "Bird shin", new(side * .085f, .13f, .015f), new(.035f, .20f, .035f), beak);
                foreach (int side in new[] { -1, 1 }) { Part(a.root, "Eye", new(side * .105f, .59f, .27f), Vector3.one * .045f, black); Part(a.root, "Bird foot", new(side * .085f, .06f, .03f), new(.035f, .12f, .16f), beak); }
            }
            else if (species == Species.Turkey)
            {
                Part(a.root, "Bronze turkey body", new(0, .63f, 0), new(.65f, .70f, 1.05f), feather);
                a.head = new GameObject("Pecking turkey neck and head").transform; a.head.SetParent(a.root, false); a.head.localPosition = new(0, .72f, .39f);
                Part(a.head, "Bare red neck", new(0, .18f, .05f), new(.14f, .45f, .17f), red); Part(a.head, "Blue gray head", new(0, .44f, .09f), new(.20f, .23f, .24f), blue);
                Part(a.head, "Pale pointed bill", new(0, .41f, .25f), new(.08f, .08f, .19f), beak); Part(a.head, "Red wattle", new(.035f, .30f, .19f), new(.07f, .24f, .07f), red);
                foreach (int side in new[] { -1, 1 }) Part(a.head, "Turkey eye", new(side * .095f, .47f, .16f), Vector3.one * .035f, black);
                a.tail = new GameObject("Broad barred turkey tail").transform; a.tail.SetParent(a.root, false); a.tail.localPosition = new(0, .70f, -.46f);
                for (int i = -4; i <= 4; i++) { var pivot = new GameObject("Fan feather").transform; pivot.SetParent(a.tail, false); pivot.localRotation = Quaternion.Euler(-20, 0, i * 14); Part(pivot, "Bronze feather", new(0, .35f, 0), new(.14f, .78f, .07f), bronze); Part(pivot, "Dark feather bar", new(0, .59f, -.01f), new(.145f, .085f, .08f), feather); Part(pivot, "Buff feather tip", new(0, .70f, 0), new(.14f, .09f, .08f), cream); }
                Part(a.root, "Folded left wing", new(-.31f, .67f, -.08f), new(.16f, .39f, .76f), bronze); Part(a.root, "Folded right wing", new(.31f, .67f, -.08f), new(.16f, .39f, .76f), bronze);
                a.legs = new Transform[2]; for (int i = 0; i < 2; i++) { var leg = new GameObject("Turkey walking leg").transform; leg.SetParent(a.root, false); leg.localPosition = new(i == 0 ? -.16f : .16f, .40f, .05f); Part(leg, "Scaled leg", new(0, -.18f, 0), new(.055f, .39f, .055f), beak); Part(leg, "Three toe foot", new(0, -.36f, .065f), new(.16f, .035f, .23f), beak); a.legs[i] = leg; }
                a.left = a.legs[0]; a.right = a.legs[1];
            }
            else if (species == Species.Squirrel)
            {
                Part(a.root, "Squirrel haunch", new(0, .29f, -.09f), new(.36f, .46f, .47f), brown); Part(a.root, "Cream belly", new(0, .38f, .11f), new(.24f, .31f, .23f), cream);
                Part(a.root, "Squirrel chest and neck", new(0, .46f, .08f), new(.28f, .37f, .32f), brown);
                foreach (int side in new[] { -1, 1 }) { Part(a.root, "Squirrel forearm", new(side * .13f, .40f, .17f), new(.13f, .23f, .20f), brown); Part(a.root, "Squirrel ankle", new(side * .16f, .14f, .015f), new(.14f, .23f, .20f), brown); }
                a.head = Part(a.root, "Squirrel head", new(0, .61f, .14f), new(.27f, .26f, .30f), brown); Part(a.root, "Muzzle", new(0, .57f, .29f), new(.18f, .12f, .17f), tailM); Part(a.root, "Nose", new(0, .60f, .36f), Vector3.one * .055f, black);
                a.tail = new GameObject("Curled bushy tail").transform; a.tail.SetParent(a.root, false); a.tail.localPosition = new(0, .15f, -.23f);
                Part(a.tail, "Tail base", new(0, .20f, -.17f), new(.22f, .5f, .26f), tailM).localRotation = Quaternion.Euler(-24, 0, 0); Part(a.tail, "Tail plume", new(0, .60f, -.22f), new(.36f, .58f, .34f), tailM); Part(a.tail, "Tail curl", new(0, .84f, -.09f), new(.32f, .28f, .29f), tailM);
                foreach (int side in new[] { -1, 1 }) { Part(a.root, "Upright ear", new(side * .095f, .78f, .11f), new(.09f, .17f, .09f), brown); Part(a.root, "Dark eye", new(side * .119f, .65f, .23f), Vector3.one * .055f, black); Part(a.root, "Hind paw", new(side * .17f, .055f, .04f), new(.12f, .10f, .27f), brown); }
                a.left = Part(a.root, "Foraging paw", new(-.13f, .36f, .24f), new(.085f, .19f, .09f), brown); a.right = Part(a.root, "Other paw", new(.13f, .36f, .24f), new(.085f, .19f, .09f), brown);
            }
            else if (species == Species.Deer || species == Species.Coyote)
            {
                bool deer = species == Species.Deer; float h = deer ? 1.02f : .63f; var fur = deer ? tailM : coyoteFur;
                Part(a.root, "Lean woodland body", new(0, h, 0), new(deer ? .46f : .35f, deer ? .58f : .39f, deer ? 1.20f : 1.05f), fur);
                Part(a.root, "Light chest", new(0, h - .12f, .40f), new(.29f, .36f, .19f), cream);
                a.head = new GameObject(deer ? "Deer head" : "Coyote head").transform; a.head.SetParent(a.root, false); a.head.localPosition = new(0, h + (deer ? .51f : .17f), .58f);
                Part(a.root, "Sloped neck", new(0, h + .22f, .43f), new(.26f, deer ? .72f : .32f, .31f), fur).localRotation = Quaternion.Euler(-24, 0, 0);
                Part(a.head, "Long head", Vector3.zero, new(.25f, .30f, .36f), fur); Part(a.head, "Pointed muzzle", new(0, -.065f, .22f), new(.17f, .14f, .30f), fur); Part(a.head, "Dark nose", new(0, -.06f, .37f), new(.095f, .075f, .075f), black);
                foreach (int side in new[] { -1, 1 })
                {
                    Part(a.head, "Upright woodland ear", new(side * .14f, .25f, -.025f), new(.13f, deer ? .34f : .25f, .10f), fur).localRotation = Quaternion.Euler(-12, 0, -side * 22);
                    Part(a.head, "Ear lining", new(side * .15f, .25f, .024f), new(.075f, deer ? .23f : .15f, .022f), cream).localRotation = Quaternion.Euler(-12, 0, -side * 22);
                    Part(a.head, "Alert eye", new(side * .119f, .037f, .115f), Vector3.one * .047f, black);
                }
                a.tail = Part(a.root, deer ? "Short white flag tail" : "Bushy low coyote tail", new(0, h - .04f, -.65f), new(deer ? .14f : .19f, deer ? .25f : .24f, deer ? .20f : .61f), deer ? cream : fur); a.tail.localRotation = Quaternion.Euler(deer ? -25 : -35, 0, 0);
                var legs = new List<Transform>();
                foreach (int side in new[] { -1, 1 }) foreach (float z in new[] { -.40f, .36f })
                    {
                        var leg = new GameObject("Walking leg pivot").transform; leg.SetParent(a.root, false); leg.localPosition = new(side * .16f, h - .13f, z);
                        float length = h - .16f; Part(leg, "Slender leg", new(0, -length * .5f, 0), new(deer ? .075f : .11f, length, deer ? .09f : .13f), fur);
                        Part(leg, deer ? "Cloven hoof" : "Paw", new(0, -length, .035f), new(.105f, .075f, .15f), deer ? black : fur); legs.Add(leg);
                    }
                a.legs = legs.ToArray(); a.left = a.legs[0]; a.right = a.legs[1];
            }
            else
            {
                Part(a.root, "Frog body", new(0, .18f, -.04f), new(.43f, .27f, .46f), green); a.head = Part(a.root, "Broad frog head", new(0, .26f, .17f), new(.43f, .23f, .26f), green); a.tail = Part(a.root, "Calling throat", new(0, .18f, .27f), new(.26f, .16f, .13f), cream);
                a.left = Part(a.root, "Folded left haunch", new(-.26f, .15f, -.14f), new(.24f, .25f, .35f), green); a.right = Part(a.root, "Folded right haunch", new(.26f, .15f, -.14f), new(.24f, .25f, .35f), green);
                foreach (int side in new[] { -1, 1 }) { Part(a.root, "Front leg", new(side * .20f, .11f, .23f), new(.095f, .22f, .12f), green); Part(a.root, "Back ankle", new(side * .31f, .08f, -.13f), new(.10f, .17f, .14f), green); }
                foreach (int side in new[] { -1, 1 }) { Part(a.root, "Raised eye", new(side * .145f, .38f, .18f), Vector3.one * .14f, green); Part(a.root, "Frog pupil", new(side * .145f, .40f, .24f), new(.085f, .06f, .045f), black); Part(a.root, "Webbed front foot", new(side * .22f, .025f, .29f), new(.17f, .045f, .16f), green); Part(a.root, "Long back foot", new(side * .34f, .025f, -.13f), new(.15f, .045f, .26f), green); }
            }
            var facial = new List<Transform>(); foreach (Transform child in a.root) if (child != a.head && (child.name.Contains("eye") || child.name == "Eye" || child.name.Contains("pupil") || child.name.Contains("ear") || child.name == "Muzzle" || child.name == "Nose" || child.name == "Pointed beak")) facial.Add(child); foreach (var child in facial) child.SetParent(a.head, true);
            a.root.localScale = Vector3.one * (species == Species.Frog ? 1.4f : species >= Species.Deer ? 1 : 1.25f);
            var lod = a.root.gameObject.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.003f, a.root.GetComponentsInChildren<Renderer>()) }); lod.RecalculateBounds();
            a.root.gameObject.SetActive(false); return a;
        }

        // ---------------------------------------------------------------- which habitats have an animal now
        readonly List<Vector3> views = new(); readonly List<Camera> cams = new();
        // checks: the animals out now (where, which way they run)
        public IEnumerable<(Species species, Vector3 at, Vector3 escape)> Out { get { foreach (var a in animals) if (a.site >= 0 && a.flee < 0) yield return (a.species, a.root.position, habitats[a.site].escape); } }
        bool InView(Vector3 p)
        {
            foreach (var cam in cams)
            {
                if (!cam || !cam.isActiveAndEnabled) continue;
                var v = cam.WorldToViewportPoint(p + Vector3.up * .5f); if (v.z > 0 && v.z < 260f && v.x > -.15f && v.x < 1.15f && v.y > -.2f && v.y < 1.2f) return true;
            }
            return false;
        }
        float NearestView(Vector3 p) { float d = float.MaxValue; foreach (var v in views) d = Mathf.Min(d, (v - p).sqrMagnitude); return Mathf.Sqrt(d); }
        public static bool Active(Species s, float hour, bool wet)
        {
            bool day = hour >= 6.5f && hour < 20f, dusk = hour >= 17.5f && hour < 22f || hour >= 5f && hour < 8f, night = hour >= 20f || hour < 6f;
            switch (s)
            {
                case Species.Bird: case Species.Squirrel: return day && !wet;
                case Species.Frog: return dusk || night || wet;
                case Species.Deer: case Species.Turkey: return day || dusk;
                default: return dusk || night;   // coyotes
            }
        }
        void Pick()
        {
            views.Clear(); cams.Clear(); foreach (var cam in Camera.allCameras) if (cam && cam.isActiveAndEnabled && !cam.targetTexture) { views.Add(cam.transform.position); cams.Add(cam); }
            if (views.Count == 0) return;
            float hour = WorldLook.Current ? WorldLook.Current.Hour : 12f; var pr = WorldLook.Current ? WorldLook.Current.Preset : null; bool wet = pr != null && (pr.rain > .5f || pr.snowfall > .5f);
            bool underground = Camera.main && DrainPipe.Inside(Camera.main.transform.position, out _) != null;
            // an animal whose habitat is now too far, or out of its hours, goes (out of sight); one that fled is retired until its habitat is far
            foreach (var a in animals)
            {
                if (a.site < 0) continue; var h = habitats[a.site];
                bool keep = !underground && NearestView(h.position) < Far && Active(a.species, hour, wet) && a.flee < 0;
                if (a.flee >= 0 && Time.time - a.flee > 4f && !InView(a.root.position)) { retired.Add(a.site); keep = false; }
                if (keep || InView(a.root.position)) continue;
                used.Remove(a.site); a.site = -1; a.root.gameObject.SetActive(false);
            }
            foreach (var r in retired.ToList()) if (NearestView(habitats[r].position) > Far) retired.Remove(r);
            if (underground) return;
            int n = ActiveCount; if (n >= Cap) return;
            var cand = new List<int>();
            for (int i = 0; i < habitats.Length; i++) { var h = habitats[i]; if (NearestView(h.position) > Near) continue; if (used.Contains(i) || retired.Contains(i) || !Active(h.species, hour, wet) || InView(h.position)) continue; cand.Add(i); }
            foreach (int i in cand.OrderBy(_ => rng.Next()))
            {
                if (n >= Cap) break; var h = habitats[i];
                var a = animals.FirstOrDefault(x => x.site < 0 && x.species == h.species); if (a == null) continue;
                a.site = i; used.Add(i); a.flee = -100; a.seen = false; a.nextCall = Time.time + Range(1, 12);
                a.root.position = h.position; a.root.rotation = Quaternion.Euler(0, Range(0, 360), 0); a.root.gameObject.SetActive(true); n++;
            }
        }
        public void ReserveSound() { QuietUntil = Time.time + 4; if (voice) voice.Stop(); }
        bool Call(Species species, Vector3 at)
        {
            if (Time.time < QuietUntil || voice.isPlaying || Time.time < nextVoice) return false;
            var clips = species == Species.Turkey ? new[] { turkeyCall } : species == Species.Bird ? birdCalls : species == Species.Squirrel ? squirrelCalls : species == Species.Deer ? deerCalls : species == Species.Coyote ? coyoteCalls : frogCalls; if (clips == null || clips.Length == 0) return false;
            voice.transform.position = at; voice.clip = clips[rng.Next(clips.Length)]; voice.pitch = species == Species.Coyote ? 1 : Range(.94f, 1.06f); voice.volume = .72f * GameSettings.Settings.ambience; voice.Play(); Calls++; nextVoice = Time.time + Range(7, 14); return true;
        }
        public static double Ms; static readonly System.Diagnostics.Stopwatch watch = new();   // checks: this script's time, summed (ms)
        void Update() { watch.Restart(); Step(); Ms += watch.Elapsed.TotalMilliseconds; }
        void Step()
        {
            if (Time.timeScale == 0) return;
            if (Time.unscaledTime >= nextPick) { nextPick = Time.unscaledTime + .5f; Pick(); }
            if (Time.time < nextThink) return; nextThink = Time.time + 1 / 30f;
            var cars = DrainPipe.Vehicles;
            foreach (var a in animals)
            {
                if (a.site < 0) continue; var site = habitats[a.site];
                float distance = NearestView(a.root.position); if (distance > 160) continue;
                bool visible = InView(a.root.position);
                if (visible && distance < 65 && !a.seen) { a.seen = true; Sightings++; }
                float t = Time.time + a.phase;
                if (a.legs != null)
                {
                    bool walk = a.flee >= 0 || Mathf.Sin(t * .31f) > .2f;
                    float swing = walk ? Mathf.Sin(t * (a.flee >= 0 ? 12 : 3)) * (a.flee >= 0 ? 30 : 14) : 0;
                    for (int leg = 0; leg < a.legs.Length; leg++) a.legs[leg].localRotation = Quaternion.Euler(swing * (leg == 0 || leg == 3 ? 1 : -1), 0, 0);
                    a.tail.localRotation = Quaternion.Euler(-30, Mathf.Sin(t) * 9, 0);
                    if (a.flee < 0 && walk) { var p = site.position + site.escape * (1 + Mathf.Sin(t * .31f)) * 1.1f; if (Physics.Raycast(p + Vector3.up * 5, Vector3.down, out var hit, 12, 1, QueryTriggerInteraction.Ignore)) p.y = hit.point.y + .03f; a.root.SetPositionAndRotation(p, Quaternion.LookRotation(site.escape)); }
                }
                a.head.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.2f) * 9, Mathf.Sin(t * .7f) * 22, 0);
                if (a.species == Species.Turkey && a.flee < 0) a.head.localRotation = Quaternion.Euler(Mathf.Max(0, Mathf.Sin(t * 2.8f)) * 78, Mathf.Sin(t * .7f) * 12, 0);
                if (a.species == Species.Squirrel) a.tail.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.7f) * 8, Mathf.Sin(t * .8f) * 12, 0);
                if (a.species == Species.Frog) a.tail.localScale = new Vector3(.26f, .16f, .13f) * (1 + .12f * Mathf.Sin(t * 2.2f));
                if (a.flee < 0 && distance < 35 && Time.time > a.nextCall) { if (Call(a.species, a.root.position)) a.nextCall = Time.time + Range(20, 45); }
                if (a.flee < 0)
                    foreach (var c in cars)
                    {
                        if (!c || !c.Body) continue;
                        if (Vector3.Distance(c.Body.position, a.root.position) < (a.species == Species.Bird ? 22 : 15) && c.Body.linearVelocity.magnitude > 2) { a.flee = Time.time; a.start = a.root.position; Flights++; break; }
                    }
                if (a.flee >= 0)
                {
                    float age = Time.time - a.flee; var direction = site.escape.normalized; float length = a.species == Species.Bird ? Mathf.Min(age * 5, 55) : a.legs != null ? Mathf.Min(age * 4.5f, 24) : Mathf.Min(age * 1.5f, 5);
                    var p = a.start + direction * length;
                    if (a.species == Species.Bird) { p.y += Mathf.Min(age * 3, 30); float flap = Mathf.Sin(t * 31) * 65; a.left.localRotation = Quaternion.Euler(0, 0, flap); a.right.localRotation = Quaternion.Euler(0, 0, -flap); }
                    else { if (Physics.Raycast(p + Vector3.up * 8, Vector3.down, out var ground, 20, 1, QueryTriggerInteraction.Ignore)) p.y = ground.point.y + .03f; p.y += age < 3.3f ? Mathf.Abs(Mathf.Sin(age * (a.species == Species.Frog ? 5 : 13))) * (a.species == Species.Frog ? .45f : .12f) : 0; a.left.localRotation = Quaternion.Euler(Mathf.Sin(t * 13) * 22, 0, 0); a.right.localRotation = Quaternion.Euler(-Mathf.Sin(t * 13) * 22, 0, 0); }
                    a.root.SetPositionAndRotation(p, Quaternion.LookRotation(direction));
                    if (age > 4 && !visible && distance > 35) { a.root.gameObject.SetActive(false); retired.Add(a.site); used.Remove(a.site); a.site = -1; }
                }
                else if (a.species == Species.Bird) { a.left.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t) * 4); a.right.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(t) * 4); }
            }
            voice.volume = .72f * GameSettings.Settings.ambience;
        }
    }

    // Racer's TurkeyVoice (original synthesized short gobble), as it is
    public static class TurkeyVoice
    {
        public static AudioClip Create() { const int rate = 44100; var data = new float[(int)(rate * 1.05f)]; float phase = 0; for (int i = 0; i < data.Length; i++) { float t = i / (float)rate; float syllable = Mathf.Repeat(t * 17, 1); float env = Mathf.Sin(Mathf.PI * syllable); env *= env * Mathf.Sin(Mathf.PI * t / 1.05f); phase += 2 * Mathf.PI * (430 - 170 * syllable + 35 * Mathf.Sin(t * 91)) / rate; data[i] = env * (Mathf.Sin(phase) + .38f * Mathf.Sin(phase * 2.02f) + .17f * Mathf.Sin(phase * 3.1f)) * .35f; } var clip = AudioClip.Create("Wild turkey short gobble", data.Length, 1, rate, false); clip.SetData(data, 0); return clip; }
    }
}
