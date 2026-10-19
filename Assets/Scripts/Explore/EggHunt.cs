using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 48 (Dan, 2026-10-18 16:25: "lets go with goose eggs. We can have 100 of them spread around with tier unlocks instead of just one
    // unlock. We can worry about what exactly gets unlocked later"). Ported from Racer's ExplorationCollection (the acorn hunt):
    //   kept    found by driving through (the step's swept line within the egg's radius, a jump or reset is never a pass), saved at once
    //           beside the race records (AtomicSave; a check run's own folder), a token shown within 180 m that bobs and turns, Free Roam
    //           only, the count for a few seconds when one is found (EggBanner, Racer 0.95's AcornBanner), the total in the pause menu and on
    //           the map by area (EggAreas), a restart of the hunt that keeps every other save
    //   changed goose eggs (three in a straw nest) in place of acorns, 100 of them from Data/explore/eggs.json (Tools/mapgen/make_eggs.py,
    //           built into the scene by MapBuilder.Eggs.cs); a "ground" egg sits on the top surface under it (a roof, a deck, the ground),
    //           found at start; TIERS in place of Racer's one reward (10, 25, 50, 75, 100: a notice and saved; what each unlocks is decided
    //           later, nothing is locked by them); a restart keeps the tiers reached (as Racer kept its reward)
    //   left    VehicleUnlocks, UnlockNotice, the stunt park's own three acorns (they are in the 100 here), split-screen (no eggs there)
    public sealed class EggHunt : MonoBehaviour
    {
        [Serializable] public sealed class Site { public string id, area, how, kind, note; public Vector3 position; public float radius = 2.8f; }
        [Serializable] public sealed class AreaInfo { public string name, direction; }
        [Serializable] public sealed class Save { public int version = 1; public List<string> found = new List<string>(); public List<int> tiers = new List<int>(); }
        public Site[] sites = new Site[0];
        public AreaInfo[] areas = new AreaInfo[0];
        public int[] tiers = { 10, 25, 50, 75, 100 };

        public static EggHunt Instance { get; private set; }
        public static string SaveName = "goose-eggs-v1.json";
        Save data = new Save(); string path, error;
        Transform[] tokens; Vector3 previous; bool sampled; Material shell, straw;
        public int Found => data.found.Count(id => sites.Any(s => s.id == id));
        public int Total => sites.Length;
        public bool Discovered(string id) => data.found.Contains(id);
        public IReadOnlyList<int> TiersReached => data.tiers;
        public string Error => error;
        public string SavePath => path;
        public void ResetMovement() { sampled = false; }
        public static event Action<Site> Picked;   // checks
        public static event Action<int> TierReached;

        // Free Roam on the player's own (no race, no stunt event, no police, no split screen, no trailer)
        public static bool Hunting
        {
            get
            {
                var race = RaceController.Instance;
                return race && race.State == RaceController.Stage.Free && !StuntEvent.Current && !PoliceSession.Running && !SplitScreen.Active && !TrailerMode.Active;
            }
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (shell) Destroy(shell); if (straw) Destroy(straw); }

        void Start()
        {
            path = Path.Combine(Path.GetDirectoryName(RaceController.RecordsPath()) ?? Application.persistentDataPath, SaveName);
            try { if (File.Exists(path)) data = JsonUtility.FromJson<Save>(File.ReadAllText(path)) ?? new Save(); data.found ??= new List<string>(); data.tiers ??= new List<int>(); }
            catch (Exception e) { error = "Egg hunt save unavailable: " + e.Message; }
            Settle();
            shell = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.96f, .93f, .84f), enableInstancing = true };
            shell.EnableKeyword("_EMISSION"); shell.SetColor("_EmissionColor", new Color(.22f, .2f, .14f));
            straw = new Material(shell) { color = new Color(.74f, .6f, .32f) }; straw.SetColor("_EmissionColor", new Color(.08f, .06f, .02f));
            tokens = new Transform[sites.Length];
            for (int i = 0; i < sites.Length; i++)
            {
                var t = new GameObject("Goose egg / " + sites[i].id).transform; t.SetParent(transform, false); t.position = sites[i].position; tokens[i] = t;
                void Part(PrimitiveType type, Vector3 local, Vector3 scale, Material mat, Vector3 euler = default)
                {
                    var g = GameObject.CreatePrimitive(type); g.transform.SetParent(t, false); g.transform.localPosition = local; g.transform.localScale = scale; g.transform.localEulerAngles = euler;
                    g.GetComponent<Renderer>().sharedMaterial = mat; Destroy(g.GetComponent<Collider>());
                }
                // the nest: a low straw ring (a squashed cylinder and a rim of twigs), three big goose eggs in it (bigger than life, to be seen)
                Part(PrimitiveType.Cylinder, new Vector3(0, -.42f, 0), new Vector3(1.7f, .12f, 1.7f), straw);
                for (int k = 0; k < 10; k++) { float a = k * Mathf.PI * 2f / 10f; Part(PrimitiveType.Capsule, new Vector3(Mathf.Cos(a) * .8f, -.3f, Mathf.Sin(a) * .8f), new Vector3(.16f, .5f, .16f), straw, new Vector3(90f, -a * Mathf.Rad2Deg, 0f)); }
                Part(PrimitiveType.Sphere, new Vector3(0f, .05f, .05f), new Vector3(.55f, .78f, .55f), shell, new Vector3(0, 0, 8f));
                Part(PrimitiveType.Sphere, new Vector3(-.36f, -.05f, -.22f), new Vector3(.5f, .7f, .5f), shell, new Vector3(-12f, 0, -20f));
                Part(PrimitiveType.Sphere, new Vector3(.36f, -.06f, -.2f), new Vector3(.5f, .7f, .5f), shell, new Vector3(10f, 0, 22f));
                t.gameObject.SetActive(false);
            }
            if (Found > 0) Debug.Log($"[Round48] goose eggs: {Found} / {Total} found, tiers {string.Join(", ", data.tiers)} ({path})");
        }

        // a "ground" egg sits on the top surface under it (a roof, a deck, the ground): the nest's base on it
        void Settle()
        {
            int n = 0;
            foreach (var s in sites)
            {
                if (s.kind != "ground") continue;
                var hits = Physics.RaycastAll(new Vector3(s.position.x, 700f, s.position.z), Vector3.down, 1400f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance);
                foreach (var h in hits)
                {
                    if (h.collider.attachedRigidbody) continue; var nm = h.collider.name;
                    if (nm.IndexOf("trunk", StringComparison.OrdinalIgnoreCase) >= 0 || nm.IndexOf("tree", StringComparison.OrdinalIgnoreCase) >= 0 || nm.StartsWith("Barrier")) continue;
                    s.position = new Vector3(s.position.x, h.point.y + .55f, s.position.z); n++; break;
                }
            }
        }

        void FixedUpdate()
        {
            var car = Player(); if (!car || car.Body == null) { sampled = false; return; }
            var p = car.Body.position;
            if (!sampled) { previous = p; sampled = true; return; }
            var delta = p - previous; bool valid = delta.magnitude <= Mathf.Max(3f, car.Body.linearVelocity.magnitude * Time.fixedDeltaTime * 2f + .3f);
            if (Hunting && valid && error == null)
                for (int i = 0; i < sites.Length; i++)
                {
                    if (data.found.Contains(sites[i].id)) continue;
                    float t = delta.sqrMagnitude > .001f ? Mathf.Clamp01(Vector3.Dot(sites[i].position - previous, delta) / delta.sqrMagnitude) : 0f;
                    if (Vector3.Distance(previous + delta * t, sites[i].position) > sites[i].radius) continue;
                    Take(sites[i]);
                }
            previous = p;
        }

        public void Take(Site s)
        {
            if (data.found.Contains(s.id)) return;
            var tiersBefore = new List<int>(data.tiers);
            data.found.Add(s.id); int found = Found; int tier = tiers.Where(x => found >= x && !data.tiers.Contains(x)).DefaultIfEmpty(0).Max();
            if (tier > 0) foreach (var x in tiers) if (found >= x && !data.tiers.Contains(x)) data.tiers.Add(x);
            try
            {
                AtomicSave.Write(path, JsonUtility.ToJson(data, true));
                var area = EggAreas.At(this, s.position);
                EggBanner.Show(found, Total, area != null ? area.name : s.area, area != null ? area.Count(this) : "", tier, Array.IndexOf(tiers, tier) + 1);
                Picked?.Invoke(s); if (tier > 0) TierReached?.Invoke(tier);
                Debug.Log($"[Round48] goose egg {s.id} ({s.area}, {s.how}) found: {found} / {Total}{(tier > 0 ? $"; TIER {tier} reached" : "")}");
            }
            catch (Exception e) { data.found.Remove(s.id); data.tiers = tiersBefore; error = "Egg hunt could not save: " + e.Message; }
        }

        public bool Restart()
        {
            if (error != null) return false;
            var fresh = new Save { tiers = new List<int>(data.tiers) };
            try { AtomicSave.Write(path, JsonUtility.ToJson(fresh, true)); data = fresh; sampled = false; Debug.Log("[Round48] the egg hunt restarted (tiers kept)"); return true; }
            catch (Exception e) { error = "Egg hunt could not restart: " + e.Message; return false; }
        }

        public string Summary => $"Goose eggs: {Found} / {Total} found" + (data.tiers.Count > 0 ? $"   ·   tiers reached: {string.Join(", ", data.tiers.OrderBy(x => x))}" : "") + (error != null ? "\n" + error : "");

        void LateUpdate()
        {
            if (tokens == null) return;
            var car = Player(); bool on = Hunting && car;
            for (int i = 0; i < tokens.Length; i++)
            {
                bool visible = on && !data.found.Contains(sites[i].id) && (car.transform.position - sites[i].position).sqrMagnitude < 180f * 180f;
                if (tokens[i].gameObject.activeSelf != visible) tokens[i].gameObject.SetActive(visible);
                if (visible) { tokens[i].position = sites[i].position + Vector3.up * (Mathf.Sin(Time.time * 2f + i) * .12f); tokens[i].Rotate(0f, Time.deltaTime * 30f, 0f); }
            }
        }

        static ArcadeVehicle player;
        public static ArcadeVehicle Player()
        {
            if (player && player.gameObject.activeInHierarchy) return player;
            foreach (var v in FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)) { var vi = v.GetComponent<VehicleInput>(); if (vi && vi.enabled) { player = v; return v; } }
            return null;
        }
    }
}
