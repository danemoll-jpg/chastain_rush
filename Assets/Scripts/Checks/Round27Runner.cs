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
    // Round 27 check in the exe: ChastainRush.exe -exeRound27 <dir> [-r27Only settings] [-radio] -logFile <dir>\Player.log. Muted like every
    // check (CheckRun), its files in <dir> (its settings.json too: GameSettings keeps a check's settings in the check's folder).
    //   settings  (C1) the menus with a virtual gamepad, as Round 16: title > SETTINGS (Audio) > Master right > RB Display > RB Controls >
    //             LB > B > Audio's Music page > B > B; every page photographed; the saved file and the listener checked.
    public sealed partial class Round27Runner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        int fails, checks;
        Gamepad pad;
        GameMenus menus;

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); }
        static string Arg(string name) { var a = Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < a.Length; i++) if (a[i] == name) return a[i + 1]; return null; }
        static bool Only(string part) { var o = Arg("-r27Only"); return o == null || Array.IndexOf(o.Split(','), part) >= 0; }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            Application.runInBackground = true;
            report.AppendLine($"Round 27 check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}, muted (AudioListener.volume {AudioListener.volume:0}).");
            yield return new WaitForSecondsRealtime(3f);
            if (Only("settings")) yield return Settings();
            report.AppendLine();
            report.AppendLine(fails == 0 ? $"ALL {checks} CHECKS PASSED" : $"{fails} OF {checks} CHECKS FAILED");
            File.WriteAllText(Path.Combine(outputDir, "round27.txt"), report.ToString());
            Debug.Log("[Round27]\n" + report);
            if (pad != null) InputSystem.RemoveDevice(pad);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        IEnumerator Settings()
        {
            report.AppendLine("== C1 settings pages (a virtual gamepad)");
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            pad = InputSystem.AddDevice<Gamepad>("Round27 virtual pad");
            if (DeveloperHud.Instance) DeveloperHud.Instance.Visible = false;
            menus = GameMenus.Create();
            yield return new WaitForSecondsRealtime(1.5f);
            Check("title menu", menus.PageName.StartsWith("title/root"), menus.PageName);
            for (int i = 0; i < 3; i++) yield return Press(GamepadButton.DpadDown);
            Check("D-pad down three times reaches SETTINGS (the Screen row is gone from the title)", menus.FocusedLabel == "SETTINGS", menus.FocusedLabel);
            yield return Press(GamepadButton.South);
            yield return Shot("c1_settings_audio");
            Check("A opens SETTINGS on its Audio tab", menus.PageName == "title/settings-audio" && menus.Heading == "SETTINGS", menus.PageName);
            for (int i = 0; i < 3; i++) yield return Press(GamepadButton.DpadDown);
            float was = GameSettings.Settings.master;
            Check("the fourth row is Master", menus.FocusedLabel.StartsWith("Master"), menus.FocusedLabel);
            yield return Press(GamepadButton.DpadRight);
            float now = GameSettings.Settings.master;
            string file = File.Exists(GameSettings.FilePath) ? File.ReadAllText(GameSettings.FilePath) : "";
            Check("right on Master raises it 10 % and saves it (in the check's own folder)", Mathf.Abs(now - was - .1f) < .001f && file.Contains("\"master\": " + now.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)), $"{was:0.00} -> {now:0.00}; {GameSettings.FilePath}");
            Check("a check run stays muted", AudioListener.volume == 0f, $"listener {AudioListener.volume}");
            yield return Shot("c1_settings_audio_master");
            yield return Press(GamepadButton.RightShoulder);
            yield return Shot("c1_settings_display");
            Check("RB: the Display tab (Screen, Resolution, VSync, Frame cap)", menus.PageName == "title/settings-display", menus.PageName);
            yield return Press(GamepadButton.RightShoulder);
            yield return Shot("c1_settings_controls");
            Check("RB: the Controls tab", menus.PageName == "title/settings-controls", menus.PageName);
            yield return Press(GamepadButton.LeftShoulder);
            Check("LB: back to Display", menus.PageName == "title/settings-display", menus.PageName);
            yield return Press(GamepadButton.LeftShoulder);
            // Audio's Music row (the last value row is Music volume, then the Music page)
            Check("LB again: back to Audio", menus.PageName == "title/settings-audio", menus.PageName);
            for (int i = 0; i < 12 && menus.FocusedLabel != "Music"; i++) yield return Press(GamepadButton.DpadDown);
            Check("the Music row on Audio", menus.FocusedLabel == "Music", menus.FocusedLabel);
            yield return Press(GamepadButton.South);
            yield return Shot("c1_settings_music");
            Check("A opens LOCAL MUSIC", menus.PageName == "title/music" && menus.Heading == "LOCAL MUSIC", menus.PageName + " " + menus.Heading);
            yield return Press(GamepadButton.East);
            Check("B back to Audio", menus.PageName == "title/settings-audio", menus.PageName);
            yield return Press(GamepadButton.East);
            Check("B back to the title", menus.PageName == "title/root", menus.PageName);
        }

        IEnumerator Press(GamepadButton b)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(b));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return new WaitForSecondsRealtime(0.25f);
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
