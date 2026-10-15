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

        public static string SavePath()
        {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "local", "vehicle.json")); // git-ignored
#else
            return Path.Combine(Application.persistentDataPath, "vehicle.json");
#endif
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
            if (forced != null) id = forced;
            else if (!checkRun)
                try { if (File.Exists(SavePath())) { var saved = JsonUtility.FromJson<Saved>(File.ReadAllText(SavePath())); id = saved?.vehicle ?? id; colour = saved?.colour ?? -1; } }
                catch (Exception e) { Debug.LogWarning("[Vehicle] saved choice unreadable: " + e.Message); }
            if (VehicleProfile.IndexOf(id) < 0) id = "original";
            config.Apply(id);
            if (colour >= 0 && colour < VehiclePaint.Count) { Colour = colour; config.SetBodyColor(colour); }
            if (RaceController.Instance) RaceController.Instance.ReloadRecords();
            Debug.Log($"[Vehicle] {config.Profile.Name} ({config.profileId})");
        }

        public void Step(int d)
        {
            int n = VehicleProfile.All.Length, i = Mathf.Max(0, VehicleProfile.IndexOf(config.profileId));
            Select(VehicleProfile.All[(i + d + n) % n].Id);
        }

        public void Select(string id)
        {
            var car = GetComponent<ArcadeVehicle>();
            // where it stands: same place and heading, lifted a little so a bigger body does not start in the ground, at rest
            var t = transform; var fwd = Vector3.ProjectOnPlane(t.forward, Vector3.up);
            t.SetPositionAndRotation(t.position + Vector3.up * 0.4f, Quaternion.LookRotation(fwd.sqrMagnitude > 1e-4f ? fwd : Vector3.forward, Vector3.up));
            car.Body.position = t.position; car.Body.rotation = t.rotation;
            car.Body.linearVelocity = Vector3.zero; car.Body.angularVelocity = Vector3.zero;
            config.Apply(id);
            if (Colour >= 0) config.SetBodyColor(Colour);
            if (RaceController.Instance) RaceController.Instance.ReloadRecords();
            Save();
            Debug.Log($"[Vehicle] switched to {config.Profile.Name} ({config.profileId})");
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
