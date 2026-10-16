using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 27 (C1): Racer's settings pages (RaceMenus.Core RenderSettings / RenderControls, RaceMenus.Shell's tab switching), in this
    // game's menu. As in Racer: the tabs are the first rows of every settings page (the one shown ticked), Q / E or LB / RB step through
    // them, every value row is  ‹ value ›  changed with left / right, each change is applied and saved at once (Racer's Adjust), B goes
    // back. Racer's "Race / UI" volume is left out: this game has no race or menu tones. Tabs here: AUDIO, DISPLAY, CONTROLS (Racer's GAMEPLAY page has nothing that fits this game: hints, split screen, name tags,
    // weather grip, police). Audio: Master, Vehicle, Effects (Chastain Rush), Ambience, Music, and Racer's MUSIC page (station,
    // previous / next, music volume, the MUSIC LIBRARY page). Display: the Screen row (full screen or windowed, moved here from the menus),
    // Resolution, and Racer's VSync and Frame cap. Controls: what every button does, read only, with the current device's glyph.
    // Volumes and the radio's choices are GameSettings' (settings.json beside the race records); the screen is ScreenSetting's
    // (display.json); nothing is saved in a check run (it never opens these pages anyway).
    public sealed partial class GameMenus
    {
        static readonly string[] SettingsTabs = { "audio", "display", "gameplay", "controls" };   // Round 30 (A3): Racer's GAMEPLAY tab, for Traffic
        InputAction tabPrevious, tabNext;
        readonly List<(MenuGlyph glyph, UnityEngine.UI.Text key, string pad, string keyboard)> bindingRows = new();

        bool SettingsPage => page.StartsWith("settings-", StringComparison.Ordinal);

        void BuildSettingsInput()
        {
            tabPrevious = new InputAction("Previous category", InputActionType.Button, "<Keyboard>/q"); tabPrevious.AddBinding("<Gamepad>/leftShoulder"); tabPrevious.Enable();
            tabNext = new InputAction("Next category", InputActionType.Button, "<Keyboard>/e"); tabNext.AddBinding("<Gamepad>/rightShoulder"); tabNext.Enable();
        }
        void DisposeSettingsInput() { tabPrevious?.Dispose(); tabNext?.Dispose(); }

        // Racer's UpdateCore: Q / E, LB / RB step the settings tabs
        void UpdateSettingsTabs()
        {
            if (!SettingsPage || modal != null || tabPrevious == null) return;
            int d = tabNext.WasPressedThisFrame() ? 1 : tabPrevious.WasPressedThisFrame() ? -1 : 0;
            if (d == 0) return;
            int i = Array.IndexOf(SettingsTabs, page.Substring(9));
            page = "settings-" + SettingsTabs[(i + d + SettingsTabs.Length) % SettingsTabs.Length];
            MenuInput.ConsumeThroughRelease(); Show();
        }

        // Racer's Adjust: apply, save, show
        void Adjust(Action change) { change(); GameSettings.ApplySettings(); GameSettings.SaveSettings(); Show(); }
        void Toggle(int index, string id, string label, bool on, Action flip) => Step(index, id, label + ":   " + (on ? "On" : "Off"), d => flip());

        void RenderSettings()
        {
            ClearBindingRows();
            var s = GameSettings.Settings; var radio = LocalRadio.Current;
            if (page == "music")
            {
                if (!radio) { Clear("LOCAL MUSIC", "The radio is off in this run."); Row(0, "back", "Back", BackPage); return; }
                Clear("LOCAL MUSIC", radio.ChannelName + "\n" + radio.Song);
                Step(0, "channel", "Channel:   " + radio.ChannelName + " / Off", d => { radio.Toggle(); Show(); });
                Row(1, "previous", "Previous", () => { radio.Previous(); Show(); });
                Row(2, "next", "Next", () => { radio.Next(); Show(); });
                Step(3, "volume", $"Music {s.music:P0}", d => Adjust(() => s.music = Mathf.Clamp01(s.music + d * .1f)));
                Row(4, "library", "Music Library", () => Navigate("library"));
                Row(5, "back", "Back", BackPage);
                return;
            }
            if (page == "library")
            {
                if (!radio) { Clear("MUSIC LIBRARY", "The radio is off in this run."); Row(0, "back", "Back", BackPage); return; }
                string friendly = System.IO.Path.GetFileName(radio.Folder.TrimEnd('\\', '/'));
                Clear("MUSIC LIBRARY", (radio.Bundled ? "Bundled music" : "Custom: " + friendly) + "\n" + radio.Status + "\n" + radio.ScanStatus);
                Step(0, "source", "Source:   " + (radio.Bundled ? "Bundled" : "Custom"), d => { radio.SetSource(!radio.Bundled); Show(); });
                Row(1, "folder", "Choose Folder", () => { radio.ChooseFolder(); Show(); });
                Row(2, "rescan", "Rescan", () => { radio.Rescan(); Show(); });
                Row(3, "cancel-scan", "Cancel Scan", () => { radio.CancelScan(); Show(); });
                Row(4, "open-folder", "Open Folder on Computer", radio.OpenFolder);
                Row(5, "details", "Details", () => Confirm("MUSIC LIBRARY", radio.Folder + "\nMP3 / WAV / Ogg. Each folder in it is a station.\nAdd music files on your computer, then Rescan. See RADIO.md beside the game.", "OK", Show));
                Row(6, "back", "Back", BackPage);
                return;
            }
            Clear("SETTINGS", "");
            for (int i = 0; i < SettingsTabs.Length; i++)
            {
                string tab = SettingsTabs[i];
                Row(i, "tab-" + tab, (page == "settings-" + tab ? "✓ " : "") + tab.ToUpperInvariant(), () => { page = "settings-" + tab; Show(); });
            }
            int n = SettingsTabs.Length;
            if (page == "settings-audio")
            {
                Step(n++, "master", $"Master {s.master:P0}", d => Adjust(() => s.master = Mathf.Clamp01(s.master + d * .1f)));
                Step(n++, "vehicle", $"Vehicle {s.vehicle:P0}", d => Adjust(() => s.vehicle = Mathf.Clamp01(s.vehicle + d * .1f)));
                Step(n++, "effects", $"Effects {s.effects:P0}", d => Adjust(() => s.effects = Mathf.Clamp01(s.effects + d * .1f)));
                Step(n++, "ambience", $"Ambience {s.ambience:P0}", d => Adjust(() => s.ambience = Mathf.Clamp01(s.ambience + d * .1f)));
                Step(n++, "music-volume", $"Music {s.music:P0}", d => Adjust(() => s.music = Mathf.Clamp01(s.music + d * .1f)));
                Row(n++, "music", "Music", () => Navigate("music"));
                details.gameObject.SetActive(true);
                details.text = "Vehicle: your engine and the other racers'. Effects: breaking things, the hydrants, the pipe express, the trampoline. Ambience: geese, water, the pipes.";
                details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 58;
            }
            if (page == "settings-display")
            {
                details.gameObject.SetActive(true); details.text = "VSync uses your display refresh. The frame cap applies with VSync off."; details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 58;
                Step(n++, "screen", "Screen:   " + (ScreenSetting.Fullscreen ? "Full screen" : "Windowed"), d => { ScreenSetting.Set(!ScreenSetting.Fullscreen); Show(); });
                Step(n++, "resolution", "Resolution:   " + ScreenSetting.ResolutionLabel, d => { ScreenSetting.StepResolution(d); Show(); });
                Toggle(n++, "vsync", "VSync", s.vsync, () => Adjust(() => s.vsync = !s.vsync));
                Step(n++, "fps", "Frame cap: " + s.frameLimit + " FPS", d => Adjust(() => { int[] caps = { 30, 60, 120 }; s.frameLimit = caps[Mathf.Clamp(Array.IndexOf(caps, s.frameLimit) + d, 0, 2)]; }));
            }
            if (page == "settings-gameplay")   // Round 30 (A3)
            {
                details.gameObject.SetActive(true); details.text = "Traffic drives the roads in free roam and races (a race closes its own roads to it)."; details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 58;
                Step(n++, "traffic", "Traffic:   " + Traffic.LevelNames[Mathf.Clamp(s.traffic, 0, 3)], d => Adjust(() => s.traffic = Mathf.Clamp(s.traffic + d, 0, 3)));
                Row(n++, "player-name", "Player name:   " + PlayerNames.Player + "   (A: change)", () => OpenNameEntry(s.playerName, PlayerNames.SetPlayer));   // Round 34 (C2): Racer's Settings > Gameplay row
            }
            if (page == "settings-controls") { RenderControls(n); return; }
            Row(14, "back", "Back", BackPage);
        }

        // Racer's RenderControls: one row per control, its button drawn for the device in use (read only)
        void RenderControls(int row)
        {
            void Binding(string label, string pad, string keyboard)
            {
                int i = row++; Row(i, "binding-" + i, label, () => { });
                var button = buttons[i]; var labelText = button.transform.Find("Label").GetComponent<UnityEngine.UI.Text>(); labelText.rectTransform.offsetMin = new(86, 0);
                var icon = Rect("Current binding glyph", button.transform); icon.anchorMin = icon.anchorMax = new(0, .5f); icon.pivot = new(0, .5f); icon.anchoredPosition = new(12, 0); icon.sizeDelta = new(62, 34);
                var graphic = icon.gameObject.AddComponent<MenuGlyph>(); graphic.raycastTarget = false;
                var key = Label("Control", icon, 16, 0); Stretch(key.rectTransform, 0, 0, 0, 0); key.alignment = TextAnchor.MiddleCenter;
                bindingRows.Add((graphic, key, pad, keyboard));
            }
            var input = car ? car.GetComponent<VehicleInput>() : null;
            if (input)
                foreach (var a in input.CurrentBindings)
                    Binding("Driving / " + a.name, BindingPath(a, "<Gamepad>"), BindingPath(a, "<Keyboard>"));
            Binding("Menus / Select", "<Gamepad>/buttonSouth", "<Keyboard>/space");
            Binding("Menus / Back", "<Gamepad>/buttonEast", "<Keyboard>/escape");
            Binding("Menus / Navigate", "<Gamepad>/dpad", "<Keyboard>/arrows");
            Binding("Menus / Previous category", "<Gamepad>/leftShoulder", "<Keyboard>/q");
            Binding("Menus / Next category", "<Gamepad>/rightShoulder", "<Keyboard>/e");
            Binding("Pause / Resume", "<Gamepad>/start", "<Keyboard>/escape");
            Binding("Camera / Change view", "<Gamepad>/buttonWest", "<Keyboard>/v");
            Binding("Camera / Look around (hold the right mouse button and move the mouse)", "<Gamepad>/rightStick", "<Mouse>/rightButton");
            Binding("Minimap / On or off (free roam)", "<Gamepad>/buttonEast", "<Keyboard>/j");
            Binding("Map / Open or close", "<Gamepad>/select", "<Keyboard>/m");
            Binding("Radio / Previous", "<Gamepad>/dpad/left", "<Keyboard>/comma");
            Binding("Radio / Next", "<Gamepad>/dpad/right", "<Keyboard>/period");
            Binding("Radio / Current song", "<Gamepad>/dpad/up", "<Keyboard>/i");
            Binding("Radio / Channel or Off", "<Gamepad>/dpad/down", "<Keyboard>/n");
            Binding("Debug / Capture a bug", "<Gamepad>/rightStickPress", "<Keyboard>/f4");
            Row(row, "back", "Back", BackPage);
        }
        static string BindingPath(InputAction a, string device)
        {
            foreach (var b in a.bindings) if (!b.isComposite && b.effectivePath.StartsWith(device, StringComparison.Ordinal)) return b.effectivePath;
            return "";
        }
        void UpdateBindingRows()
        {
            bool pad = MenuInput.Controller;
            foreach (var r in bindingRows) { if (!r.glyph) continue; string p = pad ? r.pad : r.keyboard; r.glyph.SetPath(p); r.key.text = string.IsNullOrEmpty(p) ? "-" : MenuGlyph.Label(p); }
        }
        void ClearBindingRows()
        {
            foreach (var r in bindingRows) if (r.glyph) Destroy(r.glyph.gameObject);
            bindingRows.Clear();
            foreach (var b in buttons) { var t = b.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().rectTransform; t.offsetMin = new(10, 0); t.offsetMax = new(-10, 0); }
        }
    }
}
