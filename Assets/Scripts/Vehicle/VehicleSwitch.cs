using System;
using System.IO;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 13, item 7: choosing a vehicle, TEMPORARY until the menus (Round 15). While no race is running, D-pad left / right
    // (or the , and . keys) step through Racer's eleven player vehicles (VehicleProfile.All, police left behind); the vehicle
    // is swapped where it stands (VehicleConfiguration.Apply, Racer's own swap) and its name shows for a couple of seconds.
    // The vehicle driven last is remembered between launches (vehicle.json next to the race records). The Street Classic is
    // the default. Checks (any -exe... argument, or the Editor's batch mode) neither read nor write the saved choice; they
    // drive the Street Classic, or the vehicle named by -vehicle <id>.
    public sealed class VehicleSwitch : MonoBehaviour
    {
        [Serializable] class Saved { public string vehicle; }
        VehicleConfiguration config;
        string shown; float shownUntil;
        bool checkRun;
        GUIStyle style;

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
            string id = "original", forced = null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-exe") || args[i] == "-batchmode" || args[i] == "-executeMethod") checkRun = true;
                if (args[i] == "-vehicle" && i + 1 < args.Length) forced = args[i + 1];
            }
            if (forced != null) id = forced;
            else if (!checkRun)
                try { if (File.Exists(SavePath())) id = JsonUtility.FromJson<Saved>(File.ReadAllText(SavePath()))?.vehicle ?? id; }
                catch (Exception e) { Debug.LogWarning("[Vehicle] saved choice unreadable: " + e.Message); }
            if (VehicleProfile.IndexOf(id) < 0) id = "original";
            config.Apply(id);
            if (RaceController.Instance) RaceController.Instance.ReloadRecords();
            Debug.Log($"[Vehicle] {config.Profile.Name} ({config.profileId})");
        }

        void Update()
        {
            var race = RaceController.Instance;
            if (race && race.State != RaceController.Stage.Free) return;
            var k = Keyboard.current; var g = Gamepad.current;
            int step = 0;
            if ((k != null && k.periodKey.wasPressedThisFrame) || (g != null && g.dpad.right.wasPressedThisFrame)) step = 1;
            if ((k != null && k.commaKey.wasPressedThisFrame) || (g != null && g.dpad.left.wasPressedThisFrame)) step = -1;
            if (step != 0) Step(step);
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
            shown = config.Profile.Name; shownUntil = Time.unscaledTime + 2.5f;
            if (RaceController.Instance) RaceController.Instance.ReloadRecords();
            if (!checkRun)
                try
                {
                    string path = SavePath(); Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, JsonUtility.ToJson(new Saved { vehicle = config.profileId }));
                }
                catch (Exception e) { Debug.LogWarning("[Vehicle] could not save the choice: " + e.Message); }
            Debug.Log($"[Vehicle] switched to {config.Profile.Name} ({config.profileId})");
        }

        void OnGUI()
        {
            if (shown == null || Time.unscaledTime > shownUntil) return;
            float s = Screen.height / 1080f;
            style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            style.fontSize = Mathf.RoundToInt(44 * s);
            var r = new Rect(0, Screen.height * 0.28f, Screen.width, 70 * s);
            style.normal.textColor = new Color(0, 0, 0, 0.75f); GUI.Label(new Rect(r.x + 3 * s, r.y + 3 * s, r.width, r.height), shown, style);
            style.normal.textColor = Color.white; GUI.Label(r, shown, style);
            style.fontSize = Mathf.RoundToInt(22 * s);
            var r2 = new Rect(0, r.yMax, Screen.width, 34 * s);
            style.normal.textColor = new Color(1, 1, 1, 0.85f); GUI.Label(r2, "D-pad left / right  ( , . )  to change vehicle", style);
        }
    }
}
