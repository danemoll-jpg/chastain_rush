using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 29 check in the exe (ChastainRush.exe -exeRound29 <dir> -logFile <dir>\Player.log), muted like every check
    // (CheckRun), its files (settings.json among them) in <dir>; never Dan's settings. A virtual gamepad walks title > GARAGE >
    // Rider..., steps the rows, B back; then three riders, each on its own vehicle, photographed on the page and in the world
    // (front view and first person).
    public sealed class Round29Runner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        int fails, checks;
        Gamepad pad;
        GameMenus menus;

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"Round 29 rider page check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}, muted. A virtual gamepad.");
            yield return new WaitForSecondsRealtime(3f);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            pad = InputSystem.AddDevice<Gamepad>("Round29 virtual pad");
            if (DeveloperHud.Instance) DeveloperHud.Instance.Visible = false;
            menus = GameMenus.Create();
            yield return new WaitForSecondsRealtime(1.5f);
            var car = FindAnyObjectByType<ArcadeVehicle>(); var vehicles = car.GetComponent<VehicleSwitch>();

            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);
            Check("GARAGE opens", menus.PageName.EndsWith("/garage"), menus.PageName);
            yield return FocusTo("Rider");
            Check("the garage has a Rider… row", menus.FocusedLabel.StartsWith("Rider"), menus.FocusedLabel);
            yield return Press(GamepadButton.South);
            Check("A opens the Rider page", menus.PageName.EndsWith("/rider") && menus.Heading == "RIDER", menus.PageName);
            yield return Shot("01_rider_page");
            yield return FocusTo("Hair:");
            string before = menus.FocusedLabel;
            for (int i = 0; i < 5; i++) yield return Press(GamepadButton.DpadRight);
            Check("right on Hair steps through the styles (5 steps from Short: Mohawk)", menus.FocusedLabel == "Hair: Mohawk", $"{before} -> {menus.FocusedLabel}");
            yield return Press(GamepadButton.South);
            Check("A does not step a row", menus.FocusedLabel == "Hair: Mohawk", menus.FocusedLabel);
            yield return Stick(new Vector2(1f, 0f), 0.8f);
            yield return Shot("02_rider_mohawk_turned");
            yield return Press(GamepadButton.East);
            Check("B goes back to the garage, focus on Rider…", menus.PageName.EndsWith("/garage") && menus.FocusedLabel.StartsWith("Rider"), $"{menus.PageName} {menus.FocusedLabel}");
            string saved = File.Exists(GameSettings.FilePath) ? File.ReadAllText(GameSettings.FilePath) : "";
            Check("saved in settings.json (the check's own copy)", saved.Contains("\"rider\"") && saved.Contains("\"hair\": 5"), GameSettings.FilePath);
            yield return Press(GamepadButton.East); yield return Press(GamepadButton.DpadUp); yield return Press(GamepadButton.South);   // title > FREE ROAM
            Check("free roam", menus.PageName == "closed", menus.PageName);

            var riders = new (string name, string vehicle, RiderLook look)[]
            {
                ("a", "moto", new RiderLook { body = 1, skin = 3, hair = 3, hairColor = 4, hat = 0, shirt = 3, shirtColor = 0, pants = 0, pantsColor = 6 }),
                ("b", "original", new RiderLook { body = 0, skin = 5, hair = 5, hairColor = 5, hat = 0, shirt = 2, shirtColor = 1, pants = 1, pantsColor = 2 }),
                ("c", "atv", new RiderLook { body = 0, skin = 0, hair = 9, hairColor = 2, hat = 2, hatColor = 0, shirt = 0, shirtColor = 5, pants = 0, pantsColor = 3 }),
            };
            var views = FindAnyObjectByType<CameraViews>();
            foreach (var (name, vehicle, look) in riders)
            {
                vehicles.Select(vehicle);
                yield return new WaitForSecondsRealtime(1f);
                yield return Press(GamepadButton.Start);
                yield return FocusTo("GARAGE"); yield return Press(GamepadButton.South);
                yield return FocusTo("Rider"); yield return Press(GamepadButton.South);
                menus.SetRider(look);
                yield return new WaitForSecondsRealtime(0.5f);
                Check($"rider {name} on the page ({vehicle})", menus.PageName.EndsWith("/rider") && RiderLook.Player.SameAs(look.Copy().Valid()), menus.PageName);
                yield return Shot($"03_rider_{name}_page");
                yield return Press(GamepadButton.Start);   // Start from a sub-page goes back a page; B / Start until closed
                for (int i = 0; i < 4 && menus.PageName != "closed"; i++) yield return Press(GamepadButton.East);
                Check($"rider {name}: menu closed, the vehicle wears the rider", menus.PageName == "closed", menus.PageName);
                yield return new WaitForSecondsRealtime(1f);
                yield return RiderShot($"04_rider_{name}_outside", car);
                if (views) { views.SetView(CameraViews.View.FirstPerson); yield return new WaitForSecondsRealtime(1.2f); }
                yield return Shot($"05_rider_{name}_first_person");
                if (views) views.SetView(CameraViews.View.Chase);
            }

            report.AppendLine();
            report.AppendLine(fails == 0 ? $"ALL {checks} CHECKS PASSED" : $"{fails} OF {checks} CHECKS FAILED");
            File.WriteAllText(Path.Combine(outputDir, "round29_rider.txt"), report.ToString());
            Debug.Log("[Round29]\n" + report);
            InputSystem.RemoveDevice(pad);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        // a picture of the rider from outside: a camera of its own ahead and to the side of the vehicle, looking at the rider
        IEnumerator RiderShot(string name, ArcadeVehicle car)
        {
            var rider = car.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Rider");
            var at = rider ? rider.position + Vector3.up * .5f : car.transform.position + Vector3.up * 1.2f;
            var cam = new GameObject("Rider picture camera").AddComponent<Camera>(); cam.depth = 100; cam.fieldOfView = 40;
            var from = at + car.transform.forward * 3.2f + car.transform.right * 2.0f + Vector3.up * .4f;
            cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from)); cam.nearClipPlane = .05f;
            yield return Shot(name);
            Destroy(cam.gameObject);
        }
        IEnumerator FocusTo(string prefix)
        {
            for (int i = 0; i < 16 && !menus.FocusedLabel.StartsWith(prefix); i++) yield return Press(GamepadButton.DpadDown);
        }
        IEnumerator Press(GamepadButton b)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(b));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return new WaitForSecondsRealtime(0.25f);
        }
        IEnumerator Stick(Vector2 right, float seconds)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = right });
            yield return new WaitForSecondsRealtime(seconds);
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(0.3f);
            yield return new WaitForEndOfFrame();
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }
    }
}
