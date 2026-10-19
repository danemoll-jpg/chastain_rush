using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ChastainRush
{
    // Round 51 check in the exe: ChastainRush.exe -exeRound51 <dir> [-r51Press key|pad] [-r51Only listen,roam,race,tips]
    // -logFile <dir>\Player.log. Muted like every check (CheckRun) except one brief listen (Hear) while the title speaks and
    // the theme starts. The one check run with the loading screen and the startup title (LoadingScreen.Allowed). It makes the
    // menus at once (as the game does after the scene loads), then: the launch behind the loading screen (bar samples, a
    // picture), the title (picture, the voice and theme measured at the listener), a key (virtual keyboard, Space) or controller
    // (virtual pad, A) press to the title menu, which must not also act on the menu; FREE ROAM and a RACE from the menus with a
    // virtual pad, each behind the cover with the bar moving (the race's countdown held, its route inset shown); the tips
    // cycling on a held screen. Pictures go to <dir>. Never reads or writes Dan's settings (the name question is skipped).
    public sealed class Round51Runner : MonoBehaviour
    {
        public string outputDir;
        readonly StringBuilder report = new StringBuilder();
        int fails, checks;
        Gamepad pad; Keyboard keys;
        GameMenus menus;
        HashSet<string> only;
        bool Part(string p) => only == null || only.Contains(p);

        void Check(string name, bool ok, string detail = "") { checks++; if (!ok) fails++; report.AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}{(detail.Length > 0 ? "  -  " + detail : "")}"); }
        void Note(string text) => report.AppendLine("      " + text);

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            var args = Environment.GetCommandLineArgs();
            string press = "pad";
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-r51Press") press = args[i + 1];
                if (args[i] == "-r51Only") only = new HashSet<string>(args[i + 1].Split(','));
            }
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;   // a check runs without window focus
            pad = InputSystem.AddDevice<Gamepad>("Round51 virtual pad");
            keys = InputSystem.AddDevice<Keyboard>("Round51 virtual keyboard");
            GameSettings.Settings.nameAsked = true;   // not the welcome's name page (in memory only, never saved)
            if (DeveloperHud.Instance) DeveloperHud.Instance.Visible = false;
            menus = GameMenus.Create();
            report.AppendLine($"Round 51 check, {DateTime.Now:yyyy-MM-dd HH:mm}, the exe, {Screen.width} x {Screen.height}, muted (AudioListener.volume {AudioListener.volume:0}). Press to continue by {press}.");

            // ---- the launch behind the loading screen
            var bars = new List<float>(); float t0 = Time.realtimeSinceStartup; bool shotLoad = false;
            Check("loading screen up at launch", LoadingScreen.Holding && LoadingScreen.LastTitle == "CHASTAIN RUSH", $"{LoadingScreen.LastTitle}, bar {LoadingScreen.Bar:0.00}");
            while (LoadingScreen.Holding && Time.realtimeSinceStartup - t0 < 70)
            {
                bars.Add(LoadingScreen.Bar);
                if (!shotLoad && LoadingScreen.Bar > .35f) { shotLoad = true; yield return Shot("01_loading_launch"); }
                yield return new WaitForSecondsRealtime(.1f);
            }
            Check("launch: the bar moves up to the end, then the screen goes", bars.Count > 2 && Rising(bars) && !LoadingScreen.Holding, $"{bars.Count} samples {Samples(bars)}; up {LoadingScreen.LastSeconds:0.00} s after the scene loaded");
            Check("launch: what is loading and a tip are shown", LoadingScreen.TipText.Length > 10, LoadingScreen.TipText);

            // ---- the title
            float goneAt = Time.realtimeSinceStartup;
            var title = StartupTitle.Instance;
            Check("the title is up over the title menu after the loading screen", title && StartupTitle.Active && menus.PageName == "title/root", $"{menus.PageName}");
            yield return new WaitForSecondsRealtime(.5f);
            yield return Shot("02_title");
            if (Part("listen") && title)
            {
                // the one brief listen: sound on while the voice speaks and the theme starts, measured at the listener, then off
                CheckRun.Hear(true);
                float voiceRms = 0, themeRms = 0; bool voicePlayed = false, themePlayed = false; float tl = Time.realtimeSinceStartup, voiceAt = -1, themeAt = -1;
                while (Time.realtimeSinceStartup - tl < 20 && !(themePlayed && Time.realtimeSinceStartup - goneAt > themeAt + 2.5f))
                {
                    float rms = OutputRms();
                    if (title.Voice && title.Voice.isPlaying) { if (!voicePlayed) voiceAt = Time.realtimeSinceStartup - goneAt; voicePlayed = true; voiceRms = Mathf.Max(voiceRms, rms); }
                    if (title.Theme && title.Theme.isPlaying && !title.Voice.isPlaying) { if (!themePlayed) themeAt = Time.realtimeSinceStartup - goneAt; themePlayed = true; themeRms = Mathf.Max(themeRms, rms); }
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(1.5f);
                if (title.Theme.clip) { var d = new float[4800]; title.Theme.clip.GetData(d, 48000 * 5); Note($"theme: time {title.Theme.time:0.00} s, volume {title.Theme.volume:0.00}, mute {title.Theme.mute}, enabled {title.Theme.enabled}, samples max {d.Max(Mathf.Abs):0.000}, {title.Theme.clip.frequency} Hz {title.Theme.clip.channels} ch, load {title.Theme.clip.loadState}, listener {AudioListener.volume} paused {AudioListener.pause}, rms now {OutputRms():0.000}"); }
                CheckRun.Hear(false);
                Note($"voice began {voiceAt:0.0} s, theme {themeAt:0.0} s after the loading screen went");
                Check("the voice speaks the title once (heard at the listener)", voicePlayed && voiceRms > .005f && title.VoiceStarts == 1, $"starts {title.VoiceStarts}, peak RMS {voiceRms:0.000}, clip {(title.Voice.clip ? title.Voice.clip.length : 0):0.00} s");
                Check("the theme plays after it, looped (heard at the listener)", themePlayed && themeRms > .005f && title.Theme.loop, $"played {themePlayed}, playing {title.Theme.isPlaying}, voice playing {title.Voice.isPlaying}, peak RMS {themeRms:0.000}, clip {(title.Theme.clip ? title.Theme.clip.length : 0):0.0} s, loop {title.Theme.loop}");
            }
            else yield return new WaitForSecondsRealtime(2.5f);
            Check("the prompt waits: still on the title", StartupTitle.Active, "");
            string focus0 = menus.FocusedLabel;
            if (press == "key") yield return PressKey(Key.Space); else yield return Press(GamepadButton.South);
            yield return new WaitForSecondsRealtime(.6f);
            Check($"one {(press == "key" ? "key (Space)" : "controller button (A)")} goes on to the title menu", !StartupTitle.Active && !StartupTitle.Blocking && menus.PageName == "title/root", $"{menus.PageName}");
            Check("the press did not also act on the menu", menus.PageName == "title/root" && menus.FocusedLabel == focus0, $"focus {focus0} -> {menus.FocusedLabel}");
            Check("the theme stops with the title", !title || !title.Theme || !title.Theme.isPlaying, "");
            yield return Shot("03_title_menu");

            // ---- Free Roam from the title menu
            if (Part("roam"))
            {
                yield return Press(GamepadButton.DpadDown);
                Check("D-pad down: FREE ROAM", menus.FocusedLabel == "FREE ROAM", menus.FocusedLabel);
                int shown = LoadingScreen.Shown;
                yield return PressWatching(GamepadButton.South, "04_loading_roam", bars);
                Check("FREE ROAM loads behind the cover, the bar moving", LoadingScreen.Shown == shown + 1 && LoadingScreen.LastTitle == "FREE ROAM" && Rising(bars) && bars.Count > 1, $"{bars.Count} samples {Samples(bars)}; up {LoadingScreen.LastSeconds:0.00} s");
                yield return new WaitForSecondsRealtime(.6f);
                Check("then Free Roam: the menu closed, the game running", menus.PageName == "closed" && Time.timeScale > 0, $"{menus.PageName}, timeScale {Time.timeScale}");
                yield return Shot("05_free_roam");
            }

            // ---- a race from the pause menu
            if (Part("race"))
            {
                var race = RaceController.Instance;
                if (menus.PageName == "closed") yield return Press(GamepadButton.Start);
                for (int i = 0; i < 12 && menus.FocusedLabel != "RACE"; i++) yield return Press(GamepadButton.DpadDown);
                yield return Press(GamepadButton.South);
                for (int i = 0; i < 16 && menus.FocusedLabel != "START RACE"; i++) yield return Press(GamepadButton.DpadDown);
                Check("race setup: START RACE", menus.FocusedLabel == "START RACE", $"{menus.PageName} {menus.FocusedLabel}");
                int shown = LoadingScreen.Shown;
                bool held = true; float countdown = 0;
                StartCoroutine(Watch(() => { if (race.State == RaceController.Stage.Countdown && LoadingScreen.Holding) { countdown = race.CountdownRemaining; held &= countdown >= 2.999f; } }));
                yield return PressWatching(GamepadButton.South, "06_loading_race", bars);
                Check("RACE loads behind the cover, the bar moving, its route on the map", LoadingScreen.Shown == shown + 1 && LoadingScreen.LastTitle == "RACE" && Rising(bars) && bars.Count > 1 && routeSeen, $"{bars.Count} samples {Samples(bars)}; route inset {routeSeen}; up {LoadingScreen.LastSeconds:0.00} s; {race.Route?.Name}");
                Check("the countdown waits for the screen", held && countdown > 2.99f, $"countdown under the screen {countdown:0.00} s");
                yield return new WaitForSecondsRealtime(1f);
                Check("then the countdown runs", race.State == RaceController.Stage.Countdown && race.CountdownRemaining < 2.9f || race.State == RaceController.Stage.Racing, $"{race.State} {race.CountdownRemaining:0.00}");
                yield return Shot("07_countdown");
            }

            // ---- the tips on a held screen (the work never reports, so it waits; 12 s), then let go
            if (Part("tips"))
            {
                int changes = LoadingScreen.TipChanges; var seen = new List<string> { LoadingScreen.TipText };
                LoadingScreen.Cover("TIPS", "Round 51 check: the screen held for 12 s", null);
                seen[0] = LoadingScreen.TipText;
                float th = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - th < 12) { if (seen[seen.Count - 1] != LoadingScreen.TipText) seen.Add(LoadingScreen.TipText); yield return null; }
                yield return Shot("08_loading_tip");
                LoadingScreen.Started();
                Check("the tips change every 5 s", LoadingScreen.TipChanges - changes >= 2 && seen.Count >= 3 && seen.Distinct().Count() == seen.Count, $"{LoadingScreen.TipChanges - changes} changes");
                foreach (var t in seen) Note(t);
                while (LoadingScreen.Holding) yield return null;
            }

            // ---- pictures of the loading screen, held (a real load is over in about 0.6 s): a race with its route, and Free Roam
            if (Part("pics"))
            {
                var race = RaceController.Instance; var r = race ? race.Route : null;
                LoadingScreen.Cover("RACE", r != null ? $"{r.Name}\n{r.Laps} laps, {r.Direction}\n3 AI drivers" : "", r);
                yield return new WaitForSecondsRealtime(1.2f); yield return Shot("09_held_loading_race", 0f);
                Check("held picture: the race's loading screen with its route", LoadingScreen.Holding && LoadingScreen.RouteShown, r?.Name ?? "");
                LoadingScreen.Started(); while (LoadingScreen.Holding) yield return null;
                LoadingScreen.Cover("FREE ROAM", "Chastain Glen, Marietta, Georgia", null);
                yield return new WaitForSecondsRealtime(1.2f); yield return Shot("10_held_loading_roam", 0f);
                LoadingScreen.Started(); while (LoadingScreen.Holding) yield return null;
            }

            report.AppendLine();
            report.AppendLine(fails == 0 ? $"ALL {checks} CHECKS PASSED" : $"{fails} OF {checks} CHECKS FAILED");
            File.WriteAllText(Path.Combine(outputDir, $"round51_check_{press}.txt"), report.ToString());
            Debug.Log("[Round51]\n" + report);
            InputSystem.RemoveDevice(pad); InputSystem.RemoveDevice(keys);
            Application.Quit(fails == 0 ? 0 : 1);
        }

        bool routeSeen;
        // press, then sample the bar while the loading screen is up (a picture once it is half way)
        IEnumerator PressWatching(GamepadButton b, string shot, List<float> bars)
        {
            bars.Clear(); routeSeen = false;
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(b));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            float t0 = Time.realtimeSinceStartup; bool shotTaken = false;
            while (!LoadingScreen.Holding && Time.realtimeSinceStartup - t0 < 2) yield return null;
            while (LoadingScreen.Holding && Time.realtimeSinceStartup - t0 < 30)
            {
                bars.Add(LoadingScreen.Bar); routeSeen |= LoadingScreen.RouteShown;
                if (!shotTaken && LoadingScreen.Bar > .4f) { shotTaken = true; yield return Shot(shot, 0f); }
                yield return null;
            }
        }
        IEnumerator Watch(Action a) { while (true) { a(); yield return new WaitForFixedUpdate(); } }

        static bool Rising(List<float> v) { for (int i = 1; i < v.Count; i++) if (v[i] < v[i - 1] - 1e-4f) return false; return v.Count > 0 && v[v.Count - 1] > v[0]; }
        static string Samples(List<float> v) => v.Count == 0 ? "" : string.Join(" ", Enumerable.Range(0, 6).Select(i => v[Mathf.Min(v.Count - 1, i * (v.Count - 1) / 5)].ToString("0.00")));
        static float OutputRms()
        {
            var buf = new float[1024]; AudioListener.GetOutputData(buf, 0);
            double s = 0; foreach (var x in buf) s += x * x; return (float)Math.Sqrt(s / buf.Length);
        }

        IEnumerator Press(GamepadButton b)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(b));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return new WaitForSecondsRealtime(0.25f);
        }
        IEnumerator PressKey(Key k)
        {
            InputSystem.QueueStateEvent(keys, new KeyboardState(k));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(keys, new KeyboardState());
            yield return new WaitForSecondsRealtime(0.25f);
        }
        IEnumerator Shot(string name, float wait = .3f)
        {
            if (wait > 0) yield return new WaitForSecondsRealtime(wait);
            yield return new WaitForEndOfFrame();
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(outputDir, $"{name}_{Screen.width}x{Screen.height}.png"), tex.EncodeToPNG());
            Destroy(tex);
        }
    }
}
