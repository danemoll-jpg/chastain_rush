using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 48 (Dan, 2026-10-19 01:43: "Woodstock Rush had an acorn hunt with a map that fills in as you explore, discovered destinations, fast
    // travel and waypoints ... I wanted everything else mentioned here"). The world map's exploration state, ported from Racer's ExplorationMap
    // (the map screen itself is this game's WorldMap: WorldMap.Explore.cs draws this):
    //   fog     the world in 20 m cells; a cell within 48 m of the player is explored (every 0.4 s while driving, never across a jump in
    //           position: a reset or a travel); saved on a worker thread every few seconds while new land is found, and on quit
    //   places  destinations (Data/explore/eggs.json "destinations": a road at each neighborhood, community and course, the stunt park, the
    //           lake trail, the hub) discovered within 45 m (and 15 m in height), saved; travel to a discovered one in Free Roam only
    //   waypoint anywhere on the map (explored or not): WaypointGuide's beam and the HUD line; not saved, Free Roam only (Racer 0.74)
    // Saved beside the race records as exploration-map-v1.json (a check run's own folder). A restart of the egg hunt keeps it (Racer's too).
    public sealed class Exploration : MonoBehaviour
    {
        [Serializable] public sealed class Destination { public string id, title; public Vector3 position; public float yaw; public bool fixedY; }
        [Serializable] public sealed class Data { public int version = 1; public string world = World; public List<int> visited = new List<int>(); public List<string> landmarks = new List<string>(); public float ox, oz; public int cols; }   // Round 59: ox, oz, cols the grid the cells were numbered on (0: the core map's, before the extension)
        public const string World = "chastain-v1";
        public const float Cell = 20f, Reveal = 48f, Discover = 45f;
        public Destination[] destinations = new Destination[0];

        public static Exploration Instance { get; private set; }
        public static string SaveName = "exploration-map-v1.json";
        Data data = new Data(); readonly HashSet<int> visited = new HashSet<int>(); string path, error, backgroundError;
        Rect world; int cols, rows; Vector3 previous; bool sampled, dirty; float nextReveal, nextSave;
        System.Threading.Tasks.Task pendingWrite;
        public int Version { get; private set; }   // bumps when the fog changes (the map redraws its fog texture)
        public int Columns => cols; public int Rows => rows; public Rect WorldRect => world;
        public Vector3? Waypoint { get; private set; }
        public string Message = "";
        public string Error => error;
        public int RevealedCount => visited.Count;
        public bool Discovered(string id) => data.landmarks.Contains(id);
        public static event Action<Destination> Arrived;   // checks

        public static void Boot(GameObject host) { if (!Instance) Instance = host.AddComponent<Exploration>(); }
        void Awake() { Instance = this; }

        void Start()
        {
            var e = GroundQuery.Extent(); e = e.width > 0 ? e : new Rect(0, 0, 3983, 1918);
            // Round 59: the cells on whole multiples of Cell from the world's origin (the map extension starts at negative x / z), so a cell found on
            // the core map is the same ground on any larger one
            float gx0 = Mathf.Floor(e.xMin / Cell) * Cell, gz0 = Mathf.Floor(e.yMin / Cell) * Cell;
            cols = Mathf.CeilToInt((e.xMax - gx0) / Cell); rows = Mathf.CeilToInt((e.yMax - gz0) / Cell);
            world = new Rect(gx0, gz0, cols * Cell, rows * Cell);
            var hunt = FindAnyObjectByType<EggHunt>(FindObjectsInactive.Include);
            var src = hunt ? hunt.GetComponent<ExplorationPlaces>() : FindAnyObjectByType<ExplorationPlaces>(FindObjectsInactive.Include);
            if (src) destinations = src.destinations;
            foreach (var d in destinations) if (!d.fixedY) d.position = RaceRoute.Surface(d.position.x, d.position.z);
            path = Path.Combine(Path.GetDirectoryName(RaceController.RecordsPath()) ?? Application.persistentDataPath, SaveName);
            try
            {
                if (File.Exists(path)) data = JsonUtility.FromJson<Data>(File.ReadAllText(path)) ?? new Data();
                if (data.world != World || data.version != 1) throw new IOException("Incompatible map data kept");
                data.visited ??= new List<int>(); data.landmarks ??= new List<string>();
                // Round 59: cells saved on another grid (the core map's: ox 0, oz 0, 200 columns) renumbered onto this one
                int oc = data.cols > 0 ? data.cols : Mathf.CeilToInt(3983f / Cell);
                int dx = Mathf.RoundToInt((data.ox - world.xMin) / Cell), dz = Mathf.RoundToInt((data.oz - world.yMin) / Cell);
                foreach (int c in data.visited) { int x = c % oc + dx, y = c / oc + dz; if (c >= 0 && x >= 0 && x < cols && y >= 0 && y < rows) visited.Add(y * cols + x); }
                if (data.cols != cols || data.ox != world.xMin || data.oz != world.yMin) dirty = true;
                data.ox = world.xMin; data.oz = world.yMin; data.cols = cols;
            }
            catch (Exception ex) { error = "Map save unavailable: " + ex.Message; }
            Version++;
        }

        // Round 63: a place found another way (the helipads: flown over or driven near, at any height)
        public void Find(string id) { if (error != null || Discovered(id)) return; data.landmarks.Add(id); dirty = true; Version++; Save(); }
        public bool Visited(Vector3 p)
        {
            int cx = Mathf.FloorToInt((p.x - world.xMin) / Cell), cy = Mathf.FloorToInt((p.z - world.yMin) / Cell);
            return cx >= 0 && cx < cols && cy >= 0 && cy < rows && visited.Contains(cy * cols + cx);
        }
        public bool CellVisited(int x, int y) => visited.Contains(y * cols + x);
        public void ResetMovement() { sampled = false; nextReveal = Time.time + 1f; }

        void FixedUpdate()
        {
            if (cols == 0) return;
            var car = EggHunt.Player(); if (!car || car.Body == null || SplitScreen.Active) { sampled = false; return; }
            var p = car.Body.position;
            if (!sampled) { previous = p; sampled = true; return; }
            bool continuous = Vector3.Distance(previous, p) <= Mathf.Max(3f, car.Body.linearVelocity.magnitude * Time.fixedDeltaTime * 2f + .3f); previous = p;
            if (!continuous) { nextReveal = Time.time + 1f; return; }
            if (Time.time < nextReveal) return; nextReveal = Time.time + .4f;
            RevealAt(p, Reveal + HelicopterFlight.RevealBonus(car));   // Round 63: wider from the air
            if (dirty && Time.unscaledTime > nextSave) { SaveInBackground(); nextSave = Time.unscaledTime + 3f; }
        }
        public void RevealAt(Vector3 p) => RevealAt(p, Reveal);
        public void RevealAt(Vector3 p, float reveal)
        {
            if (error != null) return;
            int cx = Mathf.FloorToInt((p.x - world.xMin) / Cell), cy = Mathf.FloorToInt((p.z - world.yMin) / Cell); int r = Mathf.CeilToInt(reveal / Cell);
            bool changed = false;
            for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(rows - 1, cy + r); y++)
                for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(cols - 1, cx + r); x++)
                    if (Vector2.Distance(new Vector2(world.xMin + (x + .5f) * Cell, world.yMin + (y + .5f) * Cell), new Vector2(p.x, p.z)) <= reveal) changed |= visited.Add(y * cols + x);
            foreach (var d in destinations)
                if (!Discovered(d.id) && Vector2.Distance(new Vector2(d.position.x, d.position.z), new Vector2(p.x, p.z)) < Discover && Mathf.Abs(d.position.y - p.y) < 15f)
                { data.landmarks.Add(d.id); changed = true; Debug.Log($"[Round48] destination discovered: {d.title}"); }
            if (changed) { dirty = true; Version++; }
        }

        void SaveInBackground()
        {
            if (!dirty || error != null || (pendingWrite != null && !pendingWrite.IsCompleted)) return;
            data.visited = visited.OrderBy(i => i).ToList(); string json = JsonUtility.ToJson(data), target = path; dirty = false;
            pendingWrite = System.Threading.Tasks.Task.Run(() => { try { AtomicSave.Write(target, json); } catch (Exception e) { backgroundError = e.Message; } });
        }
        public void Save()
        {
            try { pendingWrite?.Wait(); } catch (Exception) { }
            if (backgroundError != null && error == null) error = backgroundError;
            if (!dirty || error != null) return;
            try { data.visited = visited.OrderBy(i => i).ToList(); AtomicSave.Write(path, JsonUtility.ToJson(data)); dirty = false; } catch (Exception e) { error = e.Message; }
        }
        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() => Save();
        void OnDestroy() { Save(); if (Instance == this) Instance = null; }

        // ---------------------------------------------------------------- waypoints (Racer 0.74)
        public void SetWaypoint(Vector3 p)
        {
            float y = p.y;
            foreach (var h in Physics.RaycastAll(new Vector3(p.x, 700f, p.z), Vector3.down, 1400f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (h.collider.attachedRigidbody) continue; var n = h.collider.name;
                if (n.IndexOf("trunk", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("tree", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                y = h.point.y; break;
            }
            Waypoint = new Vector3(p.x, y, p.z); Message = Visited(p) ? "Waypoint set." : "Waypoint set in unexplored land.";
            if (!GetComponent<WaypointGuide>()) gameObject.AddComponent<WaypointGuide>();
        }
        public void ClearWaypoint() { if (Waypoint.HasValue) Message = "Waypoint cleared."; Waypoint = null; }

        // ---------------------------------------------------------------- travel (Racer's Travel, with this game's arrival: the road at the place,
        // its way along the road, the spot clear of traffic and parked things, as GameMenus' "Go there")
        public static bool CanTravel => EggHunt.Hunting;
        public bool Travel(Destination d)
        {
            if (!CanTravel) { Message = "Travel is in Free Roam only."; return false; }
            if (!Discovered(d.id)) { Message = "Discover a place by driving to it first."; return false; }
            var car = EggHunt.Player(); var respawn = car ? car.GetComponent<VehicleRespawn>() : null; if (!respawn) return false;
            var f = Quaternion.Euler(0f, d.yaw, 0f) * Vector3.forward;
            foreach (float back in new[] { 0f, 8f, 16f, -8f, 24f })
            {
                var p = d.position - f * back; var q = d.fixedY ? p : RaceRoute.Surface(p.x, p.z);
                bool clear = Physics.OverlapBox(q + Vector3.up * 1.2f, new Vector3(1.4f, .6f, 2.6f), Quaternion.LookRotation(f), ~0, QueryTriggerInteraction.Ignore)
                                    .All(c => c is TerrainCollider || c.attachedRigidbody == car.Body || c.name.StartsWith("Road") || c.name.StartsWith("Bridge"));
                if (!clear) continue;
                respawn.PlaceAt(q + Vector3.up * .3f, d.yaw); car.Body.linearVelocity = Vector3.zero;
                var race = RaceController.Instance; if (race) race.NotifyTeleported();
                ResetMovement(); EggHunt.Instance?.ResetMovement();
                Message = "Arrived at " + d.title + "."; Arrived?.Invoke(d); Save();
                Debug.Log($"[Round48] travel to {d.title} ({q.x:0.0}, {q.y:0.0}, {q.z:0.0})");
                return true;
            }
            Message = "Arrival blocked. Try again after traffic clears."; return false;
        }
    }

}
