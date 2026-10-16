using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 36 (Part A1): Racer's RaceMenus.Stunt (0.106 Part D, 0.109 Part B, 0.120 Part E), one player: the Stunt event's setup (STUNT EVENT on
    // the title and the Free Roam menus), the pause menu while it runs, its results with the Top 10 Best Combos and the Stunt event Top 10, and
    // (GameMenus.Records.cs) the two boards on the RECORDS page. Racer's split-screen rows (players, devices, names, the screen layout) and the
    // event's own time of day and weather are left behind: the event runs in Free Roam, under Free Roam's clock and weather.
    public sealed partial class GameMenus
    {
        static string[] StuntTargetStrings => new[] { StuntProgress.Targets[2].ToString("N0") + " pts", StuntProgress.Targets[1].ToString("N0") + " pts", StuntProgress.Targets[0].ToString("N0") + " pts" };
        static VehicleProfile[] StuntVehicles => VehicleProfile.All.Where(v => v.Stunt).ToArray();
        static string StuntChoice
        {
            get { var id = GameSettings.Settings.stuntVehicleId; return StuntVehicles.Any(v => v.Id == id) ? id : VehicleProfile.StuntBuggy.Id; }
            set { if (GameSettings.Settings.stuntVehicleId == value) return; GameSettings.Settings.stuntVehicleId = value; if (!CheckRun.Active) GameSettings.SaveSettings(); }
        }
        void OpenStuntSetup()
        {
            if (!StuntPark.Current) { Confirm("STUNT EVENT", "There is no stunt park in this map build.", "OK", () => Show()); return; }
            // the vehicle being driven, if it is a stunt vehicle, is the one shown first (as Racer's "newest driven vehicle the event allows")
            var cfg = car.GetComponent<VehicleConfiguration>(); if (cfg && cfg.Profile.Stunt) StuntChoice = cfg.profileId;
            Navigate("stunt");
        }
        void RenderStuntSetup()
        {
            Clear("STUNT EVENT", StuntPark.Title + ", on the shopping center's lot off Canton Road: " + StuntProgress.Minutes + " minutes to score as many points as you can."
                + " Jumps, flips, rolls, spins, the loop and the barrel roll, into the stores and up on the roofs; chain them within " + ArcadeActivities.ComboGap.ToString("0.#") + " s on the ground for a combo multiplier. Land on your wheels: a bail loses that jump's tricks.");
            details.fontSize = 18; details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 84;
            var medals = LaterGroup("Stunt medals", content, false, 40); medals.SetSiblingIndex(details.transform.GetSiblingIndex() + 1);
            MedalUi.TargetsRow(medals, details.font, StuntTargetStrings, 22, 30, 0, Color.white, "MEDALS");
            string Vehicle(string id) { var p = VehicleProfile.Find(id); return $"{p.Name} (stunt {p.Class.ToLowerInvariant()})"; }
            Step(0, "stunt-vehicle", "Vehicle:   " + Vehicle(StuntChoice), d => { var list = StuntVehicles; int i = System.Array.FindIndex(list, v => v.Id == StuntChoice); StuntChoice = list[((i < 0 ? 0 : i) + d + list.Length) % list.Length].Id; if (vehicles) vehicles.Select(StuntChoice); Show(); });
            Row(1, "stunt-garage", "Garage…", () => { if (vehicles && car.GetComponent<VehicleConfiguration>().profileId != StuntChoice) vehicles.Select(StuntChoice); OpenGarage("stunt"); });
            Row(2, "stunt-start", "START STUNT EVENT", StartStunt);
            var colors = buttons[2].colors; colors.normalColor = new(.1f, .38f, .35f); buttons[2].colors = colors; buttons[2].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 54;
            Row(3, "stunt-back", "Back", BackPage);
        }
        void StartStunt()
        {
            // the garage may have put on another stunt vehicle: that is the choice
            var cfg = car.GetComponent<VehicleConfiguration>(); if (cfg.Profile.Stunt) StuntChoice = cfg.profileId; else if (vehicles) vehicles.Select(StuntChoice);
            title = false; CloseMenu();
            StuntEvent.Begin(car);
        }
        void RenderStuntPause()
        {
            var e = StuntEvent.Current;
            if (e.State == StuntEvent.Phase.Done) { RenderStuntResults(); return; }
            Clear("PAUSED", $"Stunt event at {StuntPark.Title}   ·   {(int)e.Left / 60}:{(int)e.Left % 60:00} left   ·   {e.Points:N0} pts");
            int n = 0;
            Row(n++, "resume", "RESUME", CloseMenu);
            Row(n++, "stunt-restart", "RESTART STUNT EVENT", () => Confirm("RESTART THE STUNT EVENT?", "The clock and the points start again from the start line.", "RESTART", () => { CloseMenu(); e.Rematch(); }));
            Row(n++, "stunt-leave", "LEAVE STUNT EVENT", () => Confirm("LEAVE THE STUNT EVENT?", "Back to Free Roam; this round is not recorded.", "LEAVE", () => { StuntEvent.End(); CloseMenu(); }));
            Row(n++, "records", "RECORDS", OpenRecords);
            Row(n++, "settings", "SETTINGS", () => Navigate("settings-audio"));
            Row(n++, "quit", "QUIT GAME", () => Confirm("QUIT CHASTAIN RUSH?", "", "QUIT", Quit));
        }
        // the results open by themselves when the round ends (StuntEvent.Finished)
        int stuntResultTab;
        void ShowStuntResults(StuntEvent e)
        {
            stuntResultTab = 0;
            if (!started) return;
            if (!open) OpenMenu(false);
            pages.Clear(); page = "stunt-results"; modal = null; Show();
        }
        void RenderStuntResults()
        {
            var e = StuntEvent.Current;
            if (!e) { Clear("STUNT EVENT", "No results."); Row(0, "back", "Back", BackPage); return; }
            Clear("STUNT EVENT RESULTS", $"SCORE  {e.Points:N0}   ·   {StuntProgress.Minutes} minutes at {StuntPark.Title}   ·   {VehicleProfile.Find(e.Vehicle).Name}");
            var medals = LaterGroup("Stunt medals", content, false, 40); medals.SetSiblingIndex(details.transform.GetSiblingIndex() + 1);
            MedalUi.TargetsRow(medals, details.font, StuntTargetStrings, 22, 30, e.Medal, Color.white, "MEDALS");
            int fresh = e.Entered.Count(c => StuntProgress.Combos.Contains(c));
            int n = 0;
            Step(n++, "stunt-tab", stuntResultTab == 0 ? "TOP 10 BEST COMBOS" + (fresh > 0 ? $"  ({fresh} new)" : "") : "STUNT EVENT TOP 10", d => { stuntResultTab = 1 - stuntResultTab; Show(); });
            int first = n; var widths = new[] { .24f, .14f, .14f, .18f, .14f, .16f };
            TableRow(n++, "header", new[] { "Player", "Points", "Medal", "Best combo", "Tricks", "Bails" }, widths, () => { });
            TableRow(n++, "stunt-player", new[] { (SplitScreen.Active ? SplitScreen.NameOf(1) : PlayerNames.Player) + (e.Place > 0 ? $"  (Top 10 #{e.Place})" : ""), e.Points.ToString("N0"), MedalUi.Name(e.Medal), e.Best > 0 ? e.Best.ToString("N0") : "—", e.Acts ? e.Acts.Tricks.ToString() : "0", e.Acts ? e.Acts.Bails.ToString() : "0" }, widths, () => { }, true);
            var r2 = e.Of(2);   // Round 37: the split-screen event's player 2
            if (r2 != null) TableRow(n++, "stunt-player2", new[] { SplitScreen.NameOf(2) + (r2.place > 0 ? $"  (Top 10 #{r2.place})" : ""), r2.points.ToString("N0"), MedalUi.Name(r2.medal), r2.best > 0 ? r2.best.ToString("N0") : "—", r2.acts ? r2.acts.Tricks.ToString() : "0", r2.acts ? r2.acts.Bails.ToString() : "0" }, widths, () => { }, true);
            if (r2 != null) details.text = (e.Winner == 0 ? "A DRAW" : SplitScreen.NameOf(e.Winner).ToUpperInvariant() + " WINS") + "   ·   " + details.text;
            Table(first, n, 28, 18);
            first = n;
            n = stuntResultTab == 0 ? ComboTable(n, e) : StuntTopTable(n, e.Place);
            Table(first, n, 25, 17);
            Row(n, "rematch", "REMATCH", () => { CloseMenu(); e.Rematch(); }); colors(n); n++;
            Row(n++, "change-setup", "Change setup", () => { StuntEvent.End(); pages.Clear(); page = "stunt"; Show(); });
            Row(n, "free", "Back to Free Roam", () => { StuntEvent.End(); CloseMenu(); });
            for (int k = n - 2; k <= n; k++) buttons[k].transform.SetAsLastSibling();
            void colors(int i) { var c = buttons[i].colors; c.normalColor = new(.1f, .38f, .35f); buttons[i].colors = c; buttons[i].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 54; }
        }
        // 0.109 Part B: the Top 10 Best Combos (rows from n): this event's entries highlighted with their place ("NEW #3")
        int ComboTable(int n, StuntEvent e)
        {
            var top = StuntProgress.Combos; var w = new[] { .09f, .11f, .07f, .31f, .15f, .14f, .13f };
            TableRow(n++, "combo-top-header", new[] { "#", "Points", "Links", "Tricks", "Vehicle", "Name", "Date" }, w, () => { });
            for (int i = 0; i < top.Count; i++)
            {
                var c = top[i]; bool mine = e != null && e.Entered.Contains(c); string tricks = string.IsNullOrEmpty(c.tricks) ? "—" : c.tricks.Length > 36 ? c.tricks.Substring(0, 35) + "…" : c.tricks;
                TableRow(n++, "combo-top-" + i, new[] { (mine ? "NEW #" : "#") + (i + 1), c.points.ToString("N0"), c.links > 0 ? "×" + c.links : "—", tricks, VehicleProfile.Find(c.vehicle).Name, string.IsNullOrEmpty(c.name) ? PlayerNames.Player : c.name, c.date ?? "—" }, w, () => { }, mine);
            }
            if (top.Count == 0) TableRow(n++, "combo-top-none", new[] { "", "", "", "No combos yet: chain tricks at the stunt park", "", "", "" }, w, () => { });
            return n;
        }
        // the Stunt event Top 10 (rows from n); mine: the place to highlight
        int StuntTopTable(int n, int mine)
        {
            var top = StuntProgress.Top; var tw = new[] { .1f, .26f, .16f, .12f, .2f, .16f };
            TableRow(n++, "stunt-top-header", new[] { "Top 10", "Name", "Points", "Medal", "Vehicle", "Date" }, tw, () => { });
            for (int i = 0; i < top.Count; i++)
            {
                var t = top[i]; TableRow(n++, "stunt-top-" + i, new[] { "#" + (i + 1), t.name, t.points.ToString("N0"), MedalUi.Name(t.medal), VehicleProfile.Find(t.vehicle).Name, t.date }, tw, () => { }, mine == i + 1);
            }
            if (top.Count == 0) TableRow(n++, "stunt-top-none", new[] { "", "No Stunt event results yet", "", "", "", "" }, tw, () => { });
            return n;
        }
    }
}
