using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 36 (Part A2): the stunt park on the shopping center's lot, built by the map build (MapBuilder.StuntPark.cs from
    // Data/tracks/stunt_park.json, Tools/mapgen/make_stunt_park.py). This is the run-time half of Racer 0.106's StuntPark: what the stunt
    // scoring (ArcadeActivities.Stunts), the vehicles (the loop) and the resets need to know about the park. Racer builds its park at load
    // from code; here the map build bakes it into the scene and fills these fields, so the queries are the same as Racer's:
    //   Inside / OnPark   the park's ground (the lot, the stores' insides and their roofs: a flight that takes off there counts wherever it lands)
    //   Loop / LoopTangent / LoopOrigin ...   the loop (its collider holds the wheels; ArcadeVehicle follows its line)
    //   BarrelLip / BarrelForward             the barrel-roll ramp's lip (a roll off it is a barrel roll)
    //   StartPos / StartRot / Entrance        the Stunt event's start line and the Free Roam prompt at the entrance
    //   ResetsNear                            recovery inside the park: the nearest clear spot on its floor
    // New here (Dan: "they come back when the player leaves the area or on a reset, so the park works again"): the park's breakables (the
    // stores' windows, the partitions, the displays) are put back when the player is more than RestoreAway from the park, or resets in it.
    public sealed class StuntPark : MonoBehaviour
    {
        public const string Title = "Chastain Corner Stunt Park";
        public static StuntPark Current { get; private set; }

        [Header("Where (map meters; filled by the map build)")]
        public Vector2[] outline;           // the park's ground in x / z: the floor, the stores and their roofs
        public Vector3 floorOrigin;         // the floor plane: y = origin.y + slope.x (x - origin.x) + slope.y (z - origin.z)
        public Vector2 floorSlope;
        public Vector3 startPos, entrance; public float startYaw, entranceRadius = 26f;
        public Vector3[] resetSpots; public float[] resetYaws;
        [Header("The loop and the barrel roll")]
        public Collider loopCollider;
        public Vector3 loopOrigin, loopForward = Vector3.right, loopSide = Vector3.back;
        public float loopRadius = 5f, loopShift = 6.5f, loopWidth = 5.6f;
        public Vector3 barrelLip, barrelForward = Vector3.right;
        public float roofY, storeFloorY;   // the stores' roof and floor (checks, the resets)
        public float bankRoofY;            // Round 38 (Part A): the bank's roof (0: the bank is not the park's)
        // Round 38 (Part D): the stunt rivals' lines (make_stunt_park.py AI_LINES; StuntRival drives them) and their places on the start line
        [System.Serializable] public sealed class AiLine { public string id; public Vector3[] points; public float[] speeds; public string[] next; public float[] weights; }
        [Header("The stunt rivals (Round 38)")]
        public AiLine[] aiLines; public Vector3[] aiGrid;
        [Header("Breakables that come back")]
        public FenceChunk[] breakables;
        public const float RestoreAway = 160f;

        void Awake() { Current = this; }
        void OnDestroy() { if (Current == this) Current = null; }

        // ---------------------------------------------------------------- Racer's static queries
        public static Vector3 StartPos => Current ? Current.startPos : Vector3.zero;
        public static Quaternion StartRot => Quaternion.Euler(0, Current ? Current.startYaw : 0, 0);
        public static Vector3 Entrance => Current ? Current.entrance : Vector3.zero;
        public static float EntranceRadius => Current ? Current.entranceRadius : 26f;
        public static Vector3 Centre { get { var c = Current; if (!c || c.outline == null || c.outline.Length == 0) return Vector3.zero; var m = Vector2.zero; foreach (var p in c.outline) m += p; m /= c.outline.Length; return new Vector3(m.x, c.FloorY(m.x, m.y), m.y); } }
        public float FloorY(float x, float z) => floorOrigin.y + floorSlope.x * (x - floorOrigin.x) + floorSlope.y * (z - floorOrigin.z);
        public static bool Inside(Vector3 p, float margin = 0)
        {
            var c = Current; if (!c || c.outline == null || c.outline.Length < 3) return false;
            var q = new Vector2(p.x, p.z); bool inside = false;
            for (int i = 0, j = c.outline.Length - 1; i < c.outline.Length; j = i++)
            {
                Vector2 a = c.outline[i], b = c.outline[j];
                if ((a.y > q.y) != (b.y > q.y) && q.x < (b.x - a.x) * (q.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            if (inside || margin <= 0) return inside;
            for (int i = 0, j = c.outline.Length - 1; i < c.outline.Length; j = i++)
            {
                Vector2 a = c.outline[j], b = c.outline[i], ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                if (Vector2.Distance(q, a + ab * t) <= margin) return true;
            }
            return false;
        }
        public static bool OnPark(Vector3 p) => Inside(p) && p.y > Current.FloorY(p.x, p.z) - 3;
        public static bool Loop(Collider c) => Current && Current.loopCollider && c == Current.loopCollider;
        // Round 37 (Part A): a collider of the park's ramps, landings, tables, loop or stores (MapBuilder.StuntPark.cs names them "Stunt park ..."),
        // cached per collider (ArcadeVehicle asks for every wheel, every physics step)
        static readonly System.Collections.Generic.Dictionary<EntityId, bool> surfaces = new();
        public static bool Surface(Collider c)
        {
            if (!c || c is TerrainCollider) return false; var id = c.GetEntityId();
            if (!surfaces.TryGetValue(id, out bool on)) surfaces[id] = on = c.name.StartsWith("Stunt park ", System.StringComparison.Ordinal);
            return on;
        }
        public static Vector3 LoopOrigin => Current ? Current.loopOrigin : Vector3.zero;
        public static Vector3 LoopForward => Current ? Current.loopForward : Vector3.right;
        public static Vector3 LoopSide => Current ? Current.loopSide : Vector3.back;
        public static float LoopRadius => Current ? Current.loopRadius : 5f;
        public static float LoopShift => Current ? Current.loopShift : 6.5f;
        public static Vector3 BarrelLip => Current ? Current.barrelLip : Vector3.zero;
        public static Vector3 BarrelForward => Current ? Current.barrelForward : Vector3.right;
        // the loop's line at a point (its ribbon moves sideways as it goes round, so the way out passes beside the way in): the direction along
        // the ribbon there and how far the point is from its centre line, sideways (ArcadeVehicle steers a vehicle on it along that line)
        public static Vector3 LoopTangent(Vector3 p, out float sideways)
        {
            var rel = p - LoopOrigin; float r = LoopRadius, fwd = Vector3.Dot(rel, LoopForward), up = rel.y, lat = Vector3.Dot(rel, LoopSide);
            float phi = Mathf.Atan2(fwd, r - up); if (phi < 0) phi += 2 * Mathf.PI;
            if (phi < .6f && lat > LoopShift * .5f) phi = 2 * Mathf.PI; else if (phi > 2 * Mathf.PI - .6f && lat < LoopShift * .5f) phi = 0;
            float t = phi / (2 * Mathf.PI), sm = t * t * (3 - 2 * t), dsm = 6 * t * (1 - t) / (2 * Mathf.PI);
            sideways = lat - LoopShift * sm;
            return (LoopForward * (r * Mathf.Cos(phi)) + Vector3.up * (r * Mathf.Sin(phi)) + LoopSide * (LoopShift * dsm)).normalized;
        }
        // recovery inside the park (Free Roam): the nearest clear spot on the floor, facing the way the map build gave it
        public static IEnumerable<(Vector3 at, Quaternion facing)> ResetsNear(Vector3 from)
        {
            var c = Current; if (!c || c.resetSpots == null) yield break;
            foreach (int i in Enumerable.Range(0, c.resetSpots.Length).OrderBy(i => (c.resetSpots[i] - from).sqrMagnitude))
                yield return (c.resetSpots[i] + Vector3.up * .7f, Quaternion.Euler(0, c.resetYaws != null && i < c.resetYaws.Length ? c.resetYaws[i] : 0, 0));
        }

        // ---------------------------------------------------------------- the breakables come back
        ArcadeVehicle player; VehicleRespawn respawn; bool away = true; float nextLook;
        void Start()
        {
            var sw = FindAnyObjectByType<VehicleSwitch>(); player = sw ? sw.GetComponent<ArcadeVehicle>() : FindAnyObjectByType<ArcadeVehicle>();
            respawn = player ? player.GetComponent<VehicleRespawn>() : null; if (respawn) respawn.Respawned += OnReset;
            // on the map and the minimap (Racer's ExplorationMap / RacingMiniMap show the stunt park)
            mapPlace = new WorldMap.Place { label = "STUNT PARK", at = entrance, color = new Color(1f, .55f, .12f), outline = outline.Select(p => new Vector3(p.x, 0, p.y)).ToArray() };
            WorldMap.Places.Add(mapPlace);
        }
        WorldMap.Place mapPlace;
        void OnDisable() { if (mapPlace != null) WorldMap.Places.Remove(mapPlace); }
        void OnReset() { if (player && Inside(player.Body.position, 30)) RestoreBreakables(); }
        void Update()
        {
            if (!player || Time.time < nextLook) return; nextLook = Time.time + .5f;
            bool far = !Inside(player.Body.position, RestoreAway);
            if (far && !away) RestoreBreakables();
            away = far;
        }
        public void RestoreBreakables() { if (breakables == null) return; int n = 0; foreach (var b in breakables) if (b && b.RestoreOwn()) n++; if (n > 0) Debug.Log($"[StuntPark] {n} breakable sets put back"); }
    }
}
