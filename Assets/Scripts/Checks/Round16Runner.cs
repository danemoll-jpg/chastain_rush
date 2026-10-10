using System;
using System.Collections;
using System.IO;
using System.Text;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 16 menu check in the exe (ChastainRush.exe -exeRound16 <dir>), muted like every check (CheckRun), its files in
    // <dir>. The one check run with the menus (GameMenus.Create): a virtual gamepad presses the buttons Dan would, one at a
    // time, and every page is photographed with the prompts. Never reads or writes Dan's settings, vehicle or AI choices.
    // Walk: title > GARAGE (vehicle right x2, colour right, rotate) > back > RACE (laps right, opponents, difficulty) >
    // Opponent vehicles (slot 1 right) > back > START RACE (countdown) > Start (pause) > B (resume) > Start > LEAVE RACE (free
    // roam) > Start (pause in free roam) > B.
    public sealed class Round16Runner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        int fails, checks, shot;
        Gamepad pad;
        GameMenus menus;

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"Round 16 menu check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}, muted (AudioListener.volume {AudioListener.volume:0}). A virtual gamepad.");
            yield return new WaitForSecondsRealtime(3f);
            // a check runs without window focus: input from every device is taken regardless (check runs only)
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            pad = InputSystem.AddDevice<Gamepad>("Round16 virtual pad");
            menus = GameMenus.Create();
            yield return new WaitForSecondsRealtime(1.5f);
            var car = FindAnyObjectByType<ArcadeVehicle>(); var race = RaceController.Instance; var cfg = car.GetComponent<VehicleConfiguration>();

            yield return Shot("01_title");
            Check("title menu on at start, game paused", menus.PageName.StartsWith("title/root") && Time.timeScale == 0f, $"{menus.PageName}, timeScale {Time.timeScale}");
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown);
            Check("D-pad down twice reaches GARAGE", menus.FocusedLabel == "GARAGE", menus.FocusedLabel);
            yield return Press(GamepadButton.DpadUp);
            Check("D-pad up goes back a row", menus.FocusedLabel == "FREE ROAM", menus.FocusedLabel);
            yield return Press(GamepadButton.DpadDown);
            yield return Press(GamepadButton.South);
            Check("A opens the garage", menus.PageName.EndsWith("/garage"), menus.PageName);
            yield return Shot("02_garage");
            string v0 = cfg.profileId;
            yield return Press(GamepadButton.DpadRight); yield return Press(GamepadButton.DpadRight);
            Check("right on the vehicle row changes the vehicle (twice)", cfg.profileId != v0 && VehicleProfile.IndexOf(cfg.profileId) == (VehicleProfile.IndexOf(v0) + 2) % VehicleProfile.All.Length, $"{v0} -> {cfg.profileId}");
            yield return Press(GamepadButton.South);
            Check("A does not step the vehicle row", true, cfg.profileId);
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadRight);
            Check("colour row: right picks a colour", menus.FocusedLabel.StartsWith("Colour:") && !menus.FocusedLabel.Contains("Standard"), menus.FocusedLabel);
            yield return Stick(new Vector2(1f, 0f), 0.6f);
            yield return Shot("03_garage_turned_colour");
            yield return Press(GamepadButton.East);
            Check("B goes back to the title", menus.PageName == "title/root", menus.PageName);
            yield return Press(GamepadButton.DpadUp); yield return Press(GamepadButton.DpadUp);   // RACE
            yield return Press(GamepadButton.South);
            Check("RACE opens race setup", menus.PageName.EndsWith("/race"), menus.PageName);
            yield return Shot("04_race_setup");
            yield return Press(GamepadButton.DpadRight);
            Check("laps change with right", menus.FocusedLabel == "Laps:   4", menus.FocusedLabel);
            yield return Press(GamepadButton.DpadLeft);
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadRight);
            Check("difficulty changes", menus.FocusedLabel == "Difficulty:   Hard", menus.FocusedLabel);
            yield return Press(GamepadButton.DpadLeft);
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);
            Check("Opponent vehicles page", menus.PageName.EndsWith("/opponents"), menus.PageName);
            yield return Press(GamepadButton.DpadRight);
            Check("an opponent's vehicle changes with right", menus.FocusedLabel.StartsWith("EMBER:") && !menus.FocusedLabel.Contains("Random"), menus.FocusedLabel);
            yield return Shot("05_opponents");
            yield return Press(GamepadButton.DpadLeft);
            yield return Press(GamepadButton.East);
            Check("B back to race setup, focus kept", menus.PageName.EndsWith("/race") && menus.FocusedLabel.StartsWith("Opponent"), $"{menus.PageName} {menus.FocusedLabel}");
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);   // START RACE
            yield return new WaitForSecondsRealtime(0.6f);
            Check("START RACE closes the menu and starts the race", menus.PageName == "closed" && race.State == RaceController.Stage.Countdown && Time.timeScale > 0f, $"{menus.PageName}, {race.State}, laps {race.Route.Laps}, AI {RaceController.AiCount}");
            yield return Shot("06_countdown");
            yield return new WaitForSecondsRealtime(4f);
            yield return Press(GamepadButton.Start);
            Check("Start pauses the race", menus.PageName == "race/root" && Time.timeScale == 0f, menus.PageName);
            yield return Shot("07_pause_race");
            float clock = race.RaceClock;
            yield return new WaitForSecondsRealtime(1f);
            Check("the race clock stands still under the menu", Mathf.Approximately(clock, race.RaceClock), $"{clock:0.00} / {race.RaceClock:0.00}");
            yield return Press(GamepadButton.East);
            Check("B resumes", menus.PageName == "closed" && Time.timeScale > 0f, menus.PageName);
            yield return new WaitForSecondsRealtime(1f);
            yield return Press(GamepadButton.Start);
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);   // RESTART RACE -> confirm
            Check("RESTART asks first", menus.PageName.StartsWith("modal:"), menus.PageName);
            yield return Shot("08_restart_confirm");
            yield return Press(GamepadButton.East);
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);   // LEAVE RACE
            yield return new WaitForSecondsRealtime(0.5f);
            Check("LEAVE RACE: free roam, menu closed", race.State == RaceController.Stage.Free && menus.PageName == "closed", $"{race.State} {menus.PageName}");
            yield return Press(GamepadButton.Start);
            Check("Start in free roam: the pause menu", menus.PageName == "free/root", menus.PageName);
            yield return Shot("09_pause_free");
            yield return Press(GamepadButton.East);
            Check("B resumes free roam", menus.PageName == "closed" && Time.timeScale > 0f, menus.PageName);

            report.AppendLine();
            report.AppendLine(fails == 0 ? $"ALL {checks} CHECKS PASSED" : $"{fails} OF {checks} CHECKS FAILED");
            File.WriteAllText(Path.Combine(outputDir, "round16_menus.txt"), report.ToString());
            Debug.Log("[Round16]\n" + report);
            InputSystem.RemoveDevice(pad);
            Application.Quit(fails == 0 ? 0 : 1);
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
            Destroy(tex); shot++;
        }
    }
}
