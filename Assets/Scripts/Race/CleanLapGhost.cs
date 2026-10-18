using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 43 (Part B): Racer's CleanLapGhost, ported. The player's best clean lap on each route, direction and vehicle (Racer's key:
    // course / forward|reverse / vehicle / rules) is saved and raced against as a see-through car that is never solid. A clean lap is
    // one with no reset, no teleport (a jump in position) and no missed checkpoint; checkpoints an official shortcut forgives (and a
    // reset's own forgiven skip, Round 43 A1, still a reset) count as Racer's legal shortcuts. On or off in Race Setup and on the RECORDS
    // page's GHOST board (Racer's preference, off until turned on: CleanLapGhosts\preference.txt). Files: CleanLapGhosts\ beside the race
    // records (a check's own folder in a check). As in Racer: no ghost in split screen, none recorded or shown in Free Roam or the
    // Stunt event (it is not a race here); a lap is at most 20 minutes and 24,000 poses (one every 0.05 s).
    // Changes from Racer: RaceDirector / RaceFlow are this game's RaceController (its OnGate calls Boundary for the player); the clock
    // is RaceController.RaceClock (it stops while the game is paused); Racer's DebugMovementUsed / campaign checks are left behind
    // (this game has no campaign; a debug warp is a teleport); the visual is drawn from the race clock between physics steps.
    [DefaultExecutionOrder(1000)]
    public sealed class CleanLapGhost : MonoBehaviour
    {
        public const string Rules = "clean-lap-v1/chastain-rush-v1";
        [Serializable] public struct Pose { public float t; public Vector3 p; public Quaternion q; }
        [Serializable] public sealed class Lap { public string key, date; public double seconds; public List<Pose> poses = new List<Pose>(); }
        RaceController race; string root, key; Lap best; readonly List<Pose> recording = new List<Pose>(24000);
        Transform visual; Material material; double start; int misses, cursor; bool active, invalid, sampled; Vector3 previous; float nextSample;
        public bool Enabled { get; private set; }
        public string Status { get; private set; } = "No compatible ghost. Complete a clean lap.";
        public bool Showing => visual && visual.gameObject.activeSelf;   // checks
        public static CleanLapGhost Instance { get; private set; }
        ArcadeVehicle Car => race ? race.PlayerCar : null;
        string VehicleId => Car && Car.GetComponent<VehicleConfiguration>() ? Car.GetComponent<VehicleConfiguration>().profileId : "original";
        public string Key => race.Route == null ? "" : race.Route.Name + "/" + (race.Route.Reverse ? "reverse" : "forward") + "/" + VehicleId + "/" + Rules;
        public string Root => root;

        public void Initialize(RaceController owner, string saveRoot)
        {
            race = owner; root = Path.Combine(saveRoot, "CleanLapGhosts"); Instance = this;
            try { Enabled = File.Exists(Path.Combine(root, "preference.txt")) && File.ReadAllText(Path.Combine(root, "preference.txt")).Trim() == "on"; } catch { Enabled = false; }
            var respawn = Car ? Car.GetComponent<VehicleRespawn>() : null; if (respawn) respawn.Respawned += Invalidate;
        }
        public void Toggle()
        {
            Enabled = !Enabled;
            try { AtomicSave.Write(Path.Combine(root, "preference.txt"), Enabled ? "on" : "off"); } catch (Exception e) { Status = "Ghost preference could not save: " + e.Message; }
            if (!Enabled && visual) visual.gameObject.SetActive(false);
        }
        public void ResetSession() { active = false; recording.Clear(); sampled = false; cursor = 0; if (visual) visual.gameObject.SetActive(false); key = null; Refresh(); }
        public void Invalidate() { invalid = true; Status = "Clean-lap ghost rejected: reset or teleport. Next lap can qualify."; }
        string FileName => Path.Combine(root, Key.Replace('/', '_') + ".json");
        public void Refresh()
        {
            if (race == null || race.Route == null || key == Key) return;
            key = Key; best = null; cursor = 0;
            if (visual) Destroy(visual.gameObject);
            if (material) Destroy(material);
            try
            {
                if (File.Exists(FileName) && new FileInfo(FileName).Length < 12000000)
                {
                    var loaded = JsonUtility.FromJson<Lap>(File.ReadAllText(FileName));
                    if (loaded != null && loaded.key == key && loaded.seconds > 0 && loaded.poses.Count >= 2 && loaded.poses.Count <= 24002 && loaded.poses[0].t == 0 && loaded.poses.Zip(loaded.poses.Skip(1), (a, b) => b.t > a.t).All(x => x)) best = loaded;
                }
                Status = best != null ? "Clean-lap best ghost / " + RaceController.Fmt((float)best.seconds) : "No compatible clean-lap ghost yet.";
            }
            catch (Exception e) { Status = "Ghost unavailable: " + e.Message; }
        }
        // the saved ghosts of one route and direction: (vehicle, seconds, date), for the RECORDS page
        public List<(string vehicle, double seconds, string date)> Saved(string route, bool reverse)
        {
            var list = new List<(string, double, string)>();
            try
            {
                if (!Directory.Exists(root)) return list;
                string prefix = (route + "/" + (reverse ? "reverse" : "forward") + "/").Replace('/', '_');
                foreach (var f in Directory.GetFiles(root, "*.json"))
                {
                    if (!Path.GetFileName(f).StartsWith(prefix, StringComparison.Ordinal) || !f.EndsWith(Rules.Replace('/', '_') + ".json", StringComparison.Ordinal)) continue;
                    var lap = JsonUtility.FromJson<Lap>(File.ReadAllText(f)); if (lap == null || lap.seconds <= 0) continue;
                    var parts = lap.key.Split('/'); list.Add((parts.Length > 2 ? parts[2] : "?", lap.seconds, lap.date));
                }
            }
            catch (Exception) { }
            return list.OrderBy(x => x.Item2).ToList();
        }

        // the player crossed the line: 'completed' a lap (and 'clean': no checkpoint missed on it), the race 'finished'
        public void Boundary(double time, Vector3 crossing, Quaternion rotation, bool completed, bool finished, bool clean)
        {
            if (SplitScreen.Active) { active = false; invalid = true; return; }
            Refresh();
            if (completed && active)
            {
                double seconds = time - start;
                if (!invalid && clean && seconds > 1 && seconds <= 1200 && recording.Count > 10)
                {
                    recording.Add(new Pose { t = (float)seconds, p = crossing, q = rotation });
                    if (best == null || seconds < best.seconds)
                    {
                        var lap = new Lap { key = key, date = DateTime.UtcNow.ToString("o"), seconds = seconds, poses = new List<Pose>(recording) };
                        try { AtomicSave.Write(FileName, JsonUtility.ToJson(lap)); best = lap; Status = "NEW CLEAN-LAP GHOST / " + RaceController.Fmt((float)seconds); }
                        catch (Exception e) { Status = "Ghost could not be saved: " + e.Message; }
                    }
                }
                else Status = "Lap left out of clean-lap ghosts: reset, teleport, missed checkpoint or recording limit.";
            }
            active = !finished; invalid = false; start = time; cursor = 0; recording.Clear(); nextSample = 0;
            if (active) recording.Add(new Pose { t = 0, p = crossing, q = rotation });
        }

        void FixedUpdate()
        {
            if (!race || !Car) return;
            if (race.State != RaceController.Stage.Racing) { if (visual) visual.gameObject.SetActive(false); sampled = false; return; }
            var p = Car.Body.position;
            if (sampled && Vector3.Distance(p, previous) > Mathf.Max(3f, Car.Body.linearVelocity.magnitude * Time.fixedDeltaTime * 2f + .3f)) Invalidate();
            previous = p; sampled = true;
            if (!active) return;
            float t = (float)(race.RaceClock - start);
            if (t > 1200f || recording.Count >= 24000) invalid = true;
            if (!invalid && t >= nextSample && t > recording[recording.Count - 1].t) { recording.Add(new Pose { t = t, p = p, q = Car.Body.rotation }); nextSample = t + .05f; }
        }

        void LateUpdate()
        {
            if (!race || !Car) return; Refresh();
            bool show = Enabled && best != null && active && race.State == RaceController.Stage.Racing && !SplitScreen.Active;
            if (!show) { if (visual) visual.gameObject.SetActive(false); return; }
            if (!visual)
            {
                visual = new GameObject("Personal best clean-lap ghost / visual only").transform;
                var body = VehicleVisual.Build(visual, VehicleProfile.Find(VehicleId));
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.3f, .9f, 1f, .3f), renderQueue = 3000 };
                material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", 5); material.SetFloat("_DstBlend", 10); material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                foreach (var c in visual.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Destroy(c); }
                foreach (var r in body.GetComponentsInChildren<Renderer>(true)) { r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            }
            float t = (float)(race.RaceClock - start) + Mathf.Clamp(Time.time - Time.fixedTime, 0f, Time.fixedDeltaTime);
            var poses = best.poses;
            while (cursor + 1 < poses.Count - 1 && poses[cursor + 1].t < t) cursor++;
            var a = poses[cursor]; var b = poses[Mathf.Min(cursor + 1, poses.Count - 1)]; float u = Mathf.InverseLerp(a.t, b.t, t);
            visual.gameObject.SetActive(t <= best.seconds); visual.SetPositionAndRotation(Vector3.Lerp(a.p, b.p, u), Quaternion.Slerp(a.q, b.q, u));
        }

        void OnDestroy()
        {
            if (Car && Car.TryGetComponent<VehicleRespawn>(out var respawn)) respawn.Respawned -= Invalidate;
            if (visual) Destroy(visual.gameObject); if (material) Destroy(material);
            if (Instance == this) Instance = null;
        }
    }
}
