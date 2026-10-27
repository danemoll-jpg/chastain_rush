using System;
using System.IO;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 13, item 7: choosing a vehicle. Round 16: the garage (Menus/GameMenus.Garage.cs) chooses it now; the D-pad /
    // , . switch and its on-screen hint are gone. A vehicle is swapped where it stands (VehicleConfiguration.Apply, Racer's
    // own swap). The vehicle and its colour are remembered between launches (vehicle.json next to the race records). The
    // Street Classic is the default. Checks (any -exe... argument, or the Editor's batch mode) neither read nor write the
    // saved choice; they drive the Street Classic, or the vehicle named by -vehicle <id>.
    public sealed class VehicleSwitch : MonoBehaviour
    {
        // Round 70 (D, Dan: "why does my vehicle keep getting changed every time I start the game?"): two choices are kept, the last Free
        // Roam vehicle ("roam", any, a flying one too) and the last ground vehicle for races ("race": the last vehicle chosen that races, in
        // Free Roam's garage or race setup's); the game starts in the Free Roam one, race setup puts on the race one (RaceVehicle). A vehicle
        // the game puts on by itself (a route's own vehicles, the police's, the stunt event's) is never remembered (Pick.Forced).
        // "color" in American English: the old file's "vehicle" / "colour" are read once (Old) and written over in the new shape.
        [Serializable] class Saved { public string roam, race, plane; public int color = -99; }   // Round 75 (A): "plane", the last plane chosen for air races
        [Serializable] class Old { public string vehicle; public int colour = -99; }
        public enum Pick { Roam, Race, Forced }
        string roamId, raceId, planeId;
        static void Read(string text, out string roam, out string race, out int color) => Read(text, out roam, out race, out _, out color);
        static void Read(string text, out string roam, out string race, out string plane, out int color)
        {
            var s = JsonUtility.FromJson<Saved>(text); var o = JsonUtility.FromJson<Old>(text);
            plane = s?.plane; if (VehicleProfile.Find(plane)?.IsPlane != true) plane = null;
            roam = !string.IsNullOrEmpty(s?.roam) ? s.roam : o?.vehicle;
            race = !string.IsNullOrEmpty(s?.race) ? s.race : o?.vehicle;
            color = s != null && s.color != -99 ? s.color : o != null && o.colour != -99 ? o.colour : -1;
            if (!string.IsNullOrEmpty(race) && (VehicleProfile.IndexOf(race) < 0 || !Races(race))) race = null;
        }
        public static bool Races(string id) { var p = VehicleProfile.Find(id); return p != null && !p.Flies && !p.Stunt; }
        // the vehicle race setup puts on: the last ground vehicle chosen for races (the Street Classic if none)
        public string RaceVehicle => !string.IsNullOrEmpty(raceId) && VehicleProfile.IndexOf(raceId) >= 0 && Races(raceId) ? raceId : "original";
        // Round 75 (A, Dan: "your garage becomes available ... I did a street race and then selected the new air course, and my only garage choices were
        // street vehicles"): street and air races each keep their own vehicle: the last plane chosen for air races (the Sky Hopper if none)
        public string PlaneVehicle => VehicleProfile.Find(planeId)?.IsPlane == true ? planeId : "plane";
        // the vehicle race setup and the race's start put on for a route: its type's last choice if the route takes it, else the route's first vehicle
        public string RaceVehicleFor(RouteJson rd)
        {
            string id = rd != null && rd.air ? PlaneVehicle : RaceVehicle;
            return rd == null || rd.AllowsVehicle(id) ? id : rd.vehicles != null && rd.vehicles.Length > 0 ? rd.vehicles[0] : "original";
        }
        VehicleConfiguration config;
        bool checkRun;
        public int Colour { get; private set; } = -1;   // Round 16: -1 = the vehicle's standard paint, else VehiclePaint's colour
        // Round 37 (split screen): whose vehicle this is; player 2's choice is kept apart, in vehicle2.json (SplitScreen.P2Vehicle / P2Colour)
        public int player = 1;

        // Round 70 (D, checks): -vehicleSave <file>: a check run reads and writes this file as the game reads and writes vehicle.json (restarts tested
        // without touching Dan's own choice)
        static string TestSave { get { var a = Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < a.Length; i++) if (a[i] == "-vehicleSave") return a[i + 1]; return null; } }
        public static string SavePath(int player = 1)
        {
            if (player == 1 && TestSave != null) return TestSave;
            string file = player == 2 ? "vehicle2.json" : "vehicle.json";
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "local", file)); // git-ignored
#else
            return Path.Combine(Application.persistentDataPath, file);
#endif
        }
        static bool CheckRunArgs { get { foreach (var a in Environment.GetCommandLineArgs()) if (a.StartsWith("-exe") || a == "-batchmode" || a == "-executeMethod") return true; return false; } }
        // Round 37: a player's saved vehicle and colour (a check run reads and writes none: player 2 keeps what SplitScreen holds)
        public static void LoadSaved(int player, out string vehicle, out int colour)
        {
            vehicle = player == 2 ? SplitScreen.P2Vehicle : "original"; colour = player == 2 ? SplitScreen.P2Colour : -1;
            if (CheckRunArgs) return;
            try { if (File.Exists(SavePath(player))) { Read(File.ReadAllText(SavePath(player)), out var roam, out _, out int c); vehicle = roam ?? vehicle; colour = c; } }
            catch (Exception e) { Debug.LogWarning("[Vehicle] saved choice unreadable: " + e.Message); }
        }
        public static void SaveFor(int player, string vehicle, int colour)
        {
            if (CheckRunArgs) return;
            try { string path = SavePath(player); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(new Saved { roam = vehicle, race = Races(vehicle) ? vehicle : null, color = colour })); }
            catch (Exception e) { Debug.LogWarning("[Vehicle] could not save the choice: " + e.Message); }
        }

        void Start()   // after ArcadeVehicle.Awake (Apply needs its body)
        {
            config = GetComponent<VehicleConfiguration>();
            if (!config) config = gameObject.AddComponent<VehicleConfiguration>();
            if (!GetComponent<VehicleAudio>()) gameObject.AddComponent<VehicleAudio>();
            if (!GetComponent<VehicleLights>()) gameObject.AddComponent<VehicleLights>();
            string id = "original", forced = null; int colour = -1;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-exe") || args[i] == "-batchmode" || args[i] == "-executeMethod") checkRun = true;
                if (args[i] == "-vehicle" && i + 1 < args.Length) forced = args[i + 1];
            }
            if (player == 2) { id = SplitScreen.P2Vehicle; colour = SplitScreen.P2Colour; }   // Round 37: player 2 (split screen)
            else if (forced != null) id = forced;
            else if (!checkRun || TestSave != null)
                try { if (File.Exists(SavePath())) { Read(File.ReadAllText(SavePath()), out roamId, out raceId, out planeId, out colour); id = roamId ?? id; } }
                catch (Exception e) { Debug.LogWarning("[Vehicle] saved choice unreadable: " + e.Message); }
            if (VehicleProfile.IndexOf(id) < 0) id = "original";
            config.Apply(id);
            if (colour >= 0 && colour < VehiclePaint.Count) { Colour = colour; config.SetBodyColor(colour); }
            if (RaceController.Instance && player == 1) RaceController.Instance.ReloadRecords();
            Debug.Log($"[Vehicle] {(player == 2 ? "player 2: " : "")}{config.Profile.Name} ({config.profileId})");
        }

        public void Step(int d)
        {
            int n = VehicleProfile.All.Length, i = Mathf.Max(0, VehicleProfile.IndexOf(config.profileId));
            Select(VehicleProfile.All[(i + d + n) % n].Id);
        }

        public void Select(string id) => Select(id, Pick.Roam);
        public void Select(string id, Pick pick)
        {
            if (!config) config = GetComponent<VehicleConfiguration>();   // Round 37: player 2's, chosen before its first frame
            var car = GetComponent<ArcadeVehicle>();
            // where it stands: same place and heading, lifted a little so a bigger body does not start in the ground, at rest
            var t = transform; var fwd = Vector3.ProjectOnPlane(t.forward, Vector3.up);
            t.SetPositionAndRotation(t.position + Vector3.up * 0.4f, Quaternion.LookRotation(fwd.sqrMagnitude > 1e-4f ? fwd : Vector3.forward, Vector3.up));
            car.Body.position = t.position; car.Body.rotation = t.rotation;
            car.Body.linearVelocity = Vector3.zero; car.Body.angularVelocity = Vector3.zero;
            config.Apply(id);
            if (Colour >= 0) config.SetBodyColor(Colour);
            if (RaceController.Instance && player == 1) RaceController.Instance.ReloadRecords();
            if (pick == Pick.Roam) roamId = id;
            if (pick != Pick.Forced && Races(id)) raceId = id;
            if (pick != Pick.Forced && VehicleProfile.Find(id)?.IsPlane == true) planeId = id;   // Round 75 (A)
            if (pick != Pick.Forced || player == 2) Save();
            if (player == 2) SplitScreen.Session?.Refit();
            Debug.Log($"[Vehicle] {(player == 2 ? "player 2 " : "")}switched to {config.Profile.Name} ({config.profileId})");
        }

        // Round 16: the garage's colour row (-1 = the standard paint: the vehicle is put on again to get it back)
        public void SetColour(int colour)
        {
            Colour = colour;
            if (colour >= 0) config.SetBodyColor(colour); else { var car = GetComponent<ArcadeVehicle>(); var v = car.Body.linearVelocity; config.Apply(config.profileId); car.Body.linearVelocity = v; }
            Save();
        }

        // Round 29: the rider changed on the garage's Rider page: the vehicle is put on again with the new rider (Racer's
        // RaceFlow.RebuildPlayerVisual), its paint and motion kept
        public void RebuildVisual()
        {
            var car = GetComponent<ArcadeVehicle>(); var v = car.Body.linearVelocity; var w = car.Body.angularVelocity;
            config.Apply(config.profileId); if (Colour >= 0) config.SetBodyColor(Colour);
            car.Body.linearVelocity = v; car.Body.angularVelocity = w;
        }

        void Save()
        {
            if (player == 2) { SplitScreen.P2Vehicle = config.profileId; SplitScreen.P2Colour = Colour; SaveFor(2, config.profileId, Colour); return; }   // Round 37
            if (checkRun && TestSave == null) return;
            try
            {
                string path = SavePath(); Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(new Saved { roam = roamId ?? config.profileId, race = raceId, plane = planeId, color = Colour }));
            }
            catch (Exception e) { Debug.LogWarning("[Vehicle] could not save the choice: " + e.Message); }
        }

    }
}
