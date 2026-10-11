using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 17 Part A check in the exe (ChastainRush.exe -exeRound17 <dir>), muted like every check (CheckRun), its files in
    // <dir>. A virtual gamepad only, like Round16Runner: the garage reads "Color"; the debug panel starts hidden (as in the
    // game Dan plays); Start > DEBUG > show the panel > B > B; Start with the panel up lands in the debug menu; its "Game
    // menu" row; B out; Capture bug from the debug menu (then B cancels the comment). The heron makes no goose sound.
    public sealed class Round17Runner : MonoBehaviour
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
            report.AppendLine($"Round 17 Part A check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}, muted. A virtual gamepad only.");
            yield return new WaitForSecondsRealtime(3f);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            pad = InputSystem.AddDevice<Gamepad>("Round17 virtual pad");
            var hud = DeveloperHud.Instance;
            hud.Visible = false;   // as in the game Dan plays (a check run starts it shown)
            menus = GameMenus.Create();
            yield return new WaitForSecondsRealtime(1.5f);

            // (1) American English: the garage's color row
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);   // GARAGE
            yield return Press(GamepadButton.DpadDown);
            Check("garage: the color row reads \"Color\"", menus.FocusedLabel.StartsWith("Color:"), menus.FocusedLabel);
            yield return Shot("A1_garage_color");
            yield return Press(GamepadButton.East);
            yield return Press(GamepadButton.DpadUp);   // FREE ROAM
            yield return Press(GamepadButton.South);
            yield return new WaitForSecondsRealtime(1f);
            Check("free roam, debug panel hidden", menus.PageName == "closed" && !hud.Visible, $"{menus.PageName}, panel {hud.Visible}");
            yield return Shot("A4_hud_line_once");

            // (2) Start with the panel hidden: the regular menu; DEBUG; show the panel; B; B
            yield return Press(GamepadButton.Start);
            Check("Start with the panel hidden opens the regular menu", menus.PageName == "free/root", menus.PageName);
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.DpadDown);
            Check("D-pad reaches DEBUG", menus.FocusedLabel == "DEBUG", menus.FocusedLabel);
            yield return Shot("A2_pause_debug_row");
            yield return Press(GamepadButton.South);
            Check("A on DEBUG opens the debug menu", menus.PageName == "free/debug", menus.PageName);
            Check("the panel row has focus and reads Hidden", menus.FocusedLabel.Contains("Debug panel") && menus.FocusedLabel.Contains("Hidden"), menus.FocusedLabel);
            yield return Press(GamepadButton.South);
            Check("A does not change the panel row", !hud.Visible, menus.FocusedLabel);
            yield return Press(GamepadButton.DpadRight);
            Check("right shows the panel", hud.Visible && menus.FocusedLabel.Contains("Shown"), menus.FocusedLabel);
            yield return Shot("A2_debug_menu");
            yield return Press(GamepadButton.East);
            Check("B goes back to the regular menu", menus.PageName == "free/root", menus.PageName);
            yield return Press(GamepadButton.East);
            Check("B resumes, panel on screen", menus.PageName == "closed" && hud.Visible && Time.timeScale > 0f, menus.PageName);
            yield return Shot("A2_panel_shown");

            // Start with the panel up: the debug menu; its Game menu row; B out
            yield return Press(GamepadButton.Start);
            Check("Start with the panel up opens the debug menu", menus.PageName == "free/debug", menus.PageName);
            for (int i = 0; i < 10 && menus.FocusedLabel != "Game menu"; i++) yield return Press(GamepadButton.DpadDown);   // Round 18: by label (rows added above it)
            Check("D-pad reaches \"Game menu\"", menus.FocusedLabel == "Game menu", menus.FocusedLabel);
            yield return Press(GamepadButton.South);
            Check("Game menu row opens the regular menu", menus.PageName == "free/root", menus.PageName);
            yield return Press(GamepadButton.East);
            yield return Press(GamepadButton.Start);
            Check("Start again: the debug menu", menus.PageName == "free/debug", menus.PageName);
            yield return Press(GamepadButton.East);
            Check("B out of the debug menu resumes", menus.PageName == "closed" && Time.timeScale > 0f, menus.PageName);

            // Capture bug from the debug menu, the panel hidden
            yield return Press(GamepadButton.Start);
            // the page remembers its last row (Racer's focus memory): walk to the rows by their labels
            for (int i = 0; i < 8 && !menus.FocusedLabel.StartsWith("Debug panel"); i++) yield return Press(GamepadButton.DpadDown);
            yield return Press(GamepadButton.DpadRight);   // panel row: hide
            for (int i = 0; i < 8 && menus.FocusedLabel != "Capture bug"; i++) yield return Press(GamepadButton.DpadDown);
            Check("D-pad reaches Capture bug", menus.FocusedLabel == "Capture bug", menus.FocusedLabel);
            yield return Press(GamepadButton.South);
            yield return new WaitForSecondsRealtime(0.5f);
            Check("Capture bug: the menu closes and the comment box opens (panel hidden)", menus.PageName == "closed" && hud.CommentOpen && !hud.Visible, $"{menus.PageName}, comment {hud.CommentOpen}");
            yield return Shot("A2_capture_comment");
            yield return Press(GamepadButton.East);
            Check("B cancels the comment", !hud.CommentOpen && Time.timeScale > 0f, $"comment {hud.CommentOpen}");

            // (3) the heron: no goose sound
            var flock = FindAnyObjectByType<GooseFlock>();
            if (flock)
            {
                var honk = typeof(GooseFlock).GetMethod("Honk", BindingFlags.Instance | BindingFlags.NonPublic);
                int before = flock.HonksPlayed;
                honk.Invoke(flock, new object[] { flock.transform.position, true });
                Check("the heron's call plays nothing with no heron* file", flock.HonksPlayed == before && SoundDropIns.Instance != null && SoundDropIns.Instance.Herons.Count == 0, $"honks {before} -> {flock.HonksPlayed}");
                yield return new WaitForSecondsRealtime(0.3f);
                honk.Invoke(flock, new object[] { flock.transform.position, false });
                Check("a goose still honks", flock.HonksPlayed == before + 1 || flock.VoiceCount == 0, $"honks -> {flock.HonksPlayed}, voices {flock.VoiceCount}");
            }
            else Check("goose flock found", false);

            report.AppendLine();
            report.AppendLine(fails == 0 ? $"ALL {checks} CHECKS PASSED" : $"{fails} OF {checks} CHECKS FAILED");
            File.WriteAllText(Path.Combine(outputDir, "round17_partA.txt"), report.ToString());
            Debug.Log("[Round17]\n" + report);
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
