using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): what the police modes share in this game (Racer's are spread over RaceFlow, RaceDirector, SplitScreen and
    // SplitRoam): the setup (Racer's SplitScreen.Police* fields), the players' vehicles, placing and holding them, a patrol car made from the player's
    // vehicle (RaceController's AI recipe), what the players' cameras can see, the Top 10 files beside the race records.
    public static class PoliceSetup
    {
        public enum Game { Getaway, SpeedPatrol, CopVsRunner }
        public static Game Mode = Game.Getaway;
        public static bool Two;                 // two players, split screen
        public static bool P2AiRunner;          // Getaway for two: the AI runs as player 2 (it draws cops too, never ends the round)
        public static int Minutes = 5, Difficulty = 1, CopFirst = 1;
        public static string CopVehicle = "police";
        public static readonly int[] Limits = { 3, 5, 8 };
        public static string DifficultyName => new[] { "Easy", "Normal", "Hard" }[Mathf.Clamp(Difficulty, 0, 2)];
        public static string Name(Game g) => g == Game.Getaway ? "Getaway" : g == Game.SpeedPatrol ? "Speed Patrol" : "Cop vs Runner";
    }

    public static class PoliceKit
    {
        static ArcadeVehicle cached; static VehicleSwitch cachedSwitch; static int cachedAt = -1;
        public static ArcadeVehicle Player(int player = 1)
        {
            if (SplitScreen.Active) return player == 1 ? SplitScreen.P1Car : SplitScreen.P2Car;
            if (player != 1) return null;
            if (cachedAt == Time.frameCount || (cached && cached.isActiveAndEnabled && cachedSwitch && cachedSwitch.player == 1)) return cached;   // asked every frame by several scripts: the search once
            cachedAt = Time.frameCount; cachedSwitch = null;
            foreach (var sw in Object.FindObjectsByType<VehicleSwitch>(FindObjectsSortMode.None)) if (sw.player == 1) { var v = sw.GetComponent<ArcadeVehicle>(); if (v) { cachedSwitch = sw; return cached = v; } }
            foreach (var v in Object.FindObjectsByType<ArcadeVehicle>(FindObjectsSortMode.None)) { var vi = v.GetComponent<VehicleInput>(); if (vi && vi.enabled) return cached = v; }
            return cached = Object.FindAnyObjectByType<ArcadeVehicle>();
        }
        public static string NameOf(int player) => SplitScreen.Active ? SplitScreen.NameOf(player) : player == 1 ? PlayerNames.Player : "AI";
        public static bool FreeRoam => !RaceController.Instance || RaceController.Instance.State == RaceController.Stage.Free;

        // to a place (a reset's way: PlaceAt, the race told, the camera snapped)
        public static void Put(ArcadeVehicle car, Vector3 at, Vector3 forward)
        {
            if (!car) return; forward.y = 0; if (forward.sqrMagnitude < .01f) forward = Vector3.forward; float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            var respawn = car.GetComponent<VehicleRespawn>();
            if (respawn) respawn.PlaceAt(at + Vector3.up * .4f, yaw); else { car.Body.position = at + Vector3.up * .6f; car.Body.rotation = Quaternion.Euler(0, yaw, 0); car.transform.SetPositionAndRotation(car.Body.position, car.Body.rotation); }
            car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero; car.ClearSteering();
            if (RaceController.Instance) RaceController.Instance.NotifyTeleported();
            foreach (var c in Object.FindObjectsByType<ChaseCamera>(FindObjectsSortMode.None)) if (c.target == car.transform) c.Snap();
        }
        // still (a start, a reset hold, caught) or free; an AI player 2's road driver stops with it
        public static void Hold(HashSet<ArcadeVehicle> held, ArcadeVehicle car, bool on, bool aiDriven = false)
        {
            if (!car) return;
            var cruise = car.GetComponent<TrafficDriver>();
            if (on) { held.Add(car); car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero; car.Body.isKinematic = true; car.enabled = false; if (cruise) cruise.enabled = false; }
            else { held.Remove(car); car.Body.isKinematic = false; car.enabled = !aiDriven && !cruise; if (cruise) cruise.enabled = true; }
            var input = car.GetComponent<VehicleInput>(); if (input && !aiDriven && !cruise) input.enabled = !on;
        }

        // a patrol car (or the bike) made from the player's vehicle as RaceController makes its AI: no input, no reset, no sound of its own (OtherEngines
        // plays the nearest engines), black; held still until its driver takes it
        public static ArcadeVehicle MakePatrol(ArcadeVehicle template, string profileId, Vector3 at, Vector3 forward, string name)
        {
            var clone = Object.Instantiate(template.gameObject); clone.name = name;
            foreach (var mb in clone.GetComponents<MonoBehaviour>())
            {
                if (mb is VehicleInput || mb is VehicleRespawn) { mb.enabled = false; continue; }
                if (mb is ArcadeVehicle || mb is VehicleConfiguration || mb is RampHold || mb is WaterFeedback || mb is VehicleLights) continue;
                mb.enabled = false; if (mb is VehicleAudio) Object.DestroyImmediate(mb); else Object.Destroy(mb);
            }
            foreach (var src in clone.GetComponents<AudioSource>()) Object.Destroy(src);
            foreach (Transform child in clone.transform) if (child.name == "Headlight") Object.Destroy(child.gameObject);
            var car = clone.GetComponent<ArcadeVehicle>(); var cfg = clone.GetComponent<VehicleConfiguration>();
            cfg.classicVisual = false; cfg.riderLook = RiderLook.Police; cfg.Apply(profileId); cfg.SetPaint(new Color(.03f, .03f, .035f));
            car.enabled = false; forward.y = 0; if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            var rot = Quaternion.LookRotation(forward.normalized); var pos = at + Vector3.up * .5f;
            car.Body.position = pos; car.Body.rotation = rot; clone.transform.SetPositionAndRotation(pos, rot); car.Body.linearVelocity = car.Body.angularVelocity = Vector3.zero; car.ClearSteering();
            return car;
        }

        // a player's vehicle put on as a patrol car for a round (Speed Patrol, the cop in Cop vs Runner), and its own put back after
        public static void Wear(ArcadeVehicle car, string id, Color paint, out string was, out int wasColour)
        {
            was = null; wasColour = -1; if (!car) return; var cfg = car.GetComponent<VehicleConfiguration>(); var sw = car.GetComponent<VehicleSwitch>();
            was = cfg.profileId; wasColour = sw ? sw.Colour : -1; var v = car.Body.linearVelocity; cfg.Apply(id); cfg.SetPaint(paint); car.Body.linearVelocity = v;
        }
        public static void Unwear(ArcadeVehicle car, string was, int colour)
        {
            if (!car || string.IsNullOrEmpty(was)) return; var cfg = car.GetComponent<VehicleConfiguration>(); if (cfg.profileId == was) return;
            var v = car.Body.linearVelocity; cfg.Apply(was); if (colour >= 0) cfg.SetBodyColor(colour); car.Body.linearVelocity = v;
            foreach (var l in car.GetComponents<PoliceLights>()) Object.Destroy(l);
        }
        // a car on the roads under a traffic driver (SplitSession.Cruise's way, for any car): the nearest road open to traffic, its lane the way the car
        // faces; its old driver replaced
        public static TrafficDriver Cruise(ArcadeVehicle car)
        {
            var net = TrafficNetwork.Instance; var traffic = global::ChastainRush.Traffic.Instance; if (!net || !traffic || !car) return null;
            var old = car.GetComponent<TrafficDriver>(); if (old) Object.DestroyImmediate(old);
            var p = new Vector2(car.Body.position.x, car.Body.position.z); int best = -1; float bd = float.MaxValue; Vector2 bq = p, bt = Vector2.up;
            for (int e = 0; e < net.EdgeCount; e++)
            {
                if (net.Bridge(e) || net.Length[e] < 12) continue;
                for (int i = 1; i < net.PointCount(e); i++)
                {
                    Vector2 a = net.Point(e, i - 1), b = net.Point(e, i), ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f)); var q = a + ab * t; float d = (q - p).sqrMagnitude;
                    if (d < bd) { bd = d; best = e; bq = q; bt = ab.normalized; }
                }
            }
            if (best < 0 || bd > 80f * 80f) return null;
            var heading = new Vector2(car.transform.forward.x, car.transform.forward.z);
            int dir = net.Oneway(best) ? (net.Direction(best) == -1 ? -1 : 1) : Vector2.Dot(bt, heading) >= 0f ? 1 : -1;   // Round 80 (E): a way set at run time (the Express Lanes)
            var tan = bt * dir; var right = new Vector2(tan.y, -tan.x);
            float lane = net.Oneway(best) ? (net.Half(best) >= 4.5f ? net.Half(best) * .45f : 0f) : Mathf.Clamp(net.Half(best) * .5f, 1.3f, 2.6f);
            var at = bq + right * lane; var pos = RaceRoute.Surface(at.x, at.y) + Vector3.up * .7f;
            var input = car.GetComponent<VehicleInput>(); if (input) input.enabled = false; car.enabled = false;
            var drv = car.gameObject.AddComponent<TrafficDriver>(); drv.Initialize(traffic, car); drv.Place(best, dir, 0, pos, car.Body.linearVelocity.magnitude); return drv;
        }
        // is this point in sight of anyone's screen (the main camera and the split views): in the frustum within 450 m with a clear line (Racer's Seen)
        static readonly RaycastHit[] rays = new RaycastHit[24];
        public static bool LineOfSight(Vector3 eye, Vector3 target, Transform ignore)
        {
            var dir = target - eye; float dist = dir.magnitude; if (dist < 1) return true;
            int n = Physics.RaycastNonAlloc(eye, dir / dist, rays, dist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var col = rays[i].collider; if (!col) continue;
                if (ignore && col.transform.IsChildOf(ignore)) continue;
                if (col.GetComponentInParent<ArcadeVehicle>()) continue;   // cars never hide anyone
                return false;
            }
            return true;
        }
        public static bool Seen(Vector3 p)
        {
            var cams = new List<Camera>(); if (Camera.main) cams.Add(Camera.main); foreach (var c in SplitScreen.Views) if (c && !cams.Contains(c)) cams.Add(c);
            foreach (var cam in cams)
            {
                var v = cam.WorldToViewportPoint(p); if (v.z <= 0 || v.z > 450 || v.x < -.1f || v.x > 1.1f || v.y < -.1f || v.y > 1.1f) continue;
                if (LineOfSight(cam.transform.position, p + Vector3.up * 1.2f, null)) return true;
            }
            return false;
        }
        public static bool Underground(Vector3 p) => DrainPipe.Inside(p, out _) != null;

        // the Top 10 files beside the race records (a check run's in its own folder)
        public static string BoardPath(string file) { var dir = Path.GetDirectoryName(RaceController.RecordsPath()); return string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, file); }
        public static T Load<T>(string file) where T : new() { try { var p = BoardPath(file); if (p != null && File.Exists(p)) return JsonUtility.FromJson<T>(File.ReadAllText(p)) ?? new T(); } catch { } return new T(); }
        public static void Save(string file, object o) { try { var p = BoardPath(file); if (p == null) return; var tmp = p + ".tmp"; File.WriteAllText(tmp, JsonUtility.ToJson(o, true)); if (File.Exists(p)) File.Delete(p); File.Move(tmp, p); } catch (System.Exception e) { Debug.LogWarning("[Police] " + file + " not saved: " + e.Message); } }
        public static string Mss(float seconds) { int s = Mathf.FloorToInt(seconds); return $"{s / 60}:{s % 60:00}"; }
        public static float Mph(float mps) => mps * 2.23694f;
        public static float Mps(float mph) => mph / 2.23694f;
        // the posted limit at a point (PoliceRoads: OSM's or the class default) and whether the point is on that road
        public static int LimitAt(Vector3 p, out bool onRoad) { onRoad = false; var r = PoliceRoads.Instance; return r ? r.LimitAt(p, out onRoad, out _) : 25; }
    }
}
