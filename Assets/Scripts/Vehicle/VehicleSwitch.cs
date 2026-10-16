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
        [Serializable] class Saved { public string vehicle; public int colour = -1; }
        VehicleConfiguration config;
        bool checkRun;
        public int Colour { get; private set; } = -1;   // Round 16: -1 = the vehicle's standard paint, else VehiclePaint's colour
        // Round 37 (split screen): whose vehicle this is; player 2's choice is kept apart, in vehicle2.json (SplitScreen.P2Vehicle / P2Colour)
        public int player = 1;

        public static string SavePath(int player = 1)
        {
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
            try { if (File.Exists(SavePath(player))) { var saved = JsonUtility.FromJson<Saved>(File.ReadAllText(SavePath(player))); vehicle = saved?.vehicle ?? vehicle; colour = saved?.colour ?? colour; } }
            catch (Exception e) { Debug.LogWarning("[Vehicle] saved choice unreadable: " + e.Message); }
        }
        public static void SaveFor(int player, string vehicle, int colour)
        {
            if (CheckRunArgs) return;
            try { string path = SavePath(player); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(new Saved { vehicle = vehicle, colour = colour })); }
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
            else if (!checkRun)
                try { if (File.Exists(SavePath())) { var saved = JsonUtility.FromJson<Saved>(File.ReadAllText(SavePath())); id = saved?.vehicle ?? id; colour = saved?.colour ?? -1; } }
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

        public void Select(string id)
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
            Save();
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
            if (checkRun) return;
            try
            {
                string path = SavePath(); Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(new Saved { vehicle = config.profileId, colour = Colour }));
            }
            catch (Exception e) { Debug.LogWarning("[Vehicle] could not save the choice: " + e.Message); }
        }

    }
}
