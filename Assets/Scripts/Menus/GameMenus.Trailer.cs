using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 43 (Part D): Racer's pause menu > Trailer Mode (RaceMenus.Trailer.cs): the mode on / off, its camera, slow motion, HUD and
    // guides, the Free Roam conditions (Trailer Mode only; nothing is saved), a screenshot, the Screenshots folder and the controls.
    // Controller players reach everything here that has no button of its own.
    public sealed partial class GameMenus
    {
        void RenderTrailer()
        {
            var mode = TrailerMode.Instance; var views = Camera.main ? Camera.main.GetComponent<CameraViews>() : null;
            if (!mode || SplitScreen.Active) { Clear("TRAILER / PHOTO MODE", SplitScreen.Active ? "Not in split screen." : "Not available here."); Row(0, "back", "Back", BackPage); return; }
            bool on = TrailerMode.Active, roam = TrailerMode.FreeRoam;
            string summary = on
                ? "On: nothing is drawn on screen while driving.\n" + (roam ? "Conditions: " + mode.ConditionsLabel + "\nNothing here is saved; turning it off leaves your Free Roam clock and weather as they were." : "This race does not count for records. Time and weather: Race Setup.")
                : "For filming and screenshots: a clean screen, extra cameras, slow motion" + (roam ? " and time / weather on demand." : ".") + "\nTurn it on, then Resume. A race in Trailer Mode does not count for records.";
            if (mode.LastScreenshot != null) summary += "\nLast screenshot: " + System.IO.Path.GetFileName(mode.LastScreenshot);
            Clear("TRAILER / PHOTO MODE", summary);
            int i = 0;
            Step(i++, "trailer-toggle", "Trailer / Photo Mode:   " + (on ? "On" : "Off") + "   (F8)", d => { mode.Toggle(); Show(); });
            if (on && views)
            {
                Step(i++, "trailer-camera", "Camera:   " + CameraViews.ShotNames[(int)views.TrailerCamera], d => { views.SelectTrailerCamera((CameraViews.Shot)(((int)views.TrailerCamera + d + 9) % 9)); Show(); });
                Toggle(i++, "trailer-slow", "Slow motion 0.5×", mode.HalfSpeed, () => { mode.ToggleHalfSpeed(); Show(); });
                Step(i++, "trailer-hud", "HUD:   " + (mode.HudShown ? "Shown" : "Hidden"), d => { mode.ToggleHud(); Show(); });
                Step(i++, "trailer-panel", "Controls panel:   " + (mode.PanelShown ? "Shown" : "Hidden") + "   (H hides it with the HUD)", d => { mode.TogglePanel(); Show(); });
                Step(i++, "trailer-guides", "Checkpoint arches and arrows:   " + (mode.GuidesShown ? "Shown" : "Hidden"), d => { mode.ToggleGuides(); Show(); });
                if (mode.ConditionsAvailable)
                {
                    var look = WorldLook.Current; float hour = look.Trailer?.hour ?? look.Hour;
                    Step(i++, "trailer-time", "Time of day:   " + mode.TimeLabel, d => { mode.CycleTimeOfDay(d); Show(); });
                    Step(i++, "trailer-clock", $"Clock {Mathf.FloorToInt(hour) % 24:00}:{Mathf.FloorToInt(hour * 60) % 60:00}   (−1 h / +1 h)", d => { mode.NudgeClock(d); Show(); });
                    Step(i++, "trailer-clock-run", "Clock:   " + (mode.ClockPaused ? "Paused" : "Running"), d => { mode.ToggleClock(); Show(); });
                    Step(i++, "trailer-weather", "Weather:   " + mode.WeatherLabel, d => { mode.CycleWeather(d); Show(); });
                    Step(i++, "trailer-moon", "Moon:   " + mode.MoonLabel, d => { mode.CycleMoon(d); Show(); });
                    Row(i++, "trailer-lightning", "Lightning strike now", () => { mode.LightningNow(); Show(); });
                }
                Row(i++, "trailer-shot", "Take screenshot (no menus or HUD)", () => { mode.Screenshot(); Show(); });
            }
            Row(i++, "trailer-folder", "Open Screenshots Folder", mode.OpenScreenshotFolder);
            Row(i++, "trailer-controls", "Controls", () => Navigate("trailer-help"));
            Row(i, "back", "Back", BackPage);
            details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = Mathf.Max(110, details.preferredHeight + 20);
        }
        void RenderTrailerHelp()
        {
            Clear("TRAILER MODE CONTROLS", TrailerMode.HelpText(MenuInput.Controller, TrailerMode.FreeRoam));
            details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = Mathf.Max(200, details.preferredHeight + 20);
            Row(0, "back", "Back", BackPage);
        }
    }
}
