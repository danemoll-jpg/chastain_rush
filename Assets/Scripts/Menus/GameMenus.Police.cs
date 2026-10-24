using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 39 (Part D): Racer's police pages (RaceMenus.Police, .Getaway, .Patrol and their results), rewritten into this game's menus. POLICE is on
    // the Free Roam menu where players look (Racer had Police Chase under Split Screen: Dan's lesson): one page with the Game row (Getaway: you run, the
    // cops are AI; Speed Patrol: catch speeders for points; Cop vs Runner), Players 1 or 2 (two: split screen, the controllers as the SPLIT SCREEN
    // page joins them; in Getaway player 2 may be the AI, which runs too), the Difficulty (Getaway), the round length, the cop's vehicle (Patrol Car /
    // Patrol Cycle), START, Back. Free Roam only (a race is ended first). Controller first: left / right changes a row, A starts, B goes back. While a
    // mode runs the pause menu has RESUME, RESTART, LEAVE; at its end its results (and its Top 10) with REMATCH, Change setup, Back to Free Roam.
    public sealed partial class GameMenus
    {
        static readonly string[] CopVehicles = { "police", "policebike" };
        void OpenPoliceSetup()
        {
            var keyboard = (UnityEngine.InputSystem.InputDevice)UnityEngine.InputSystem.Keyboard.current; var opener = MenuInput.Controller && MenuInput.Pad != null ? MenuInput.Pad : keyboard;
            if (SplitScreen.P1Device == null || !SplitScreen.P1Device.added) SplitScreen.P1Device = opener;
            if (SplitScreen.P2Device != null && (!SplitScreen.P2Device.added || SplitScreen.P2Device == SplitScreen.P1Device)) SplitScreen.P2Device = null;
            if (SplitScreen.P2Device == null) SplitScreen.P2Ai = true;
            Navigate("police");
        }
        bool PoliceReady => !PoliceSetup.Two || (PoliceSetup.Mode == PoliceSetup.Game.Getaway && PoliceSetup.P2AiRunner) || (SplitScreen.P1Device != null && SplitScreen.P2Device != null && !SplitScreen.P2Ai);
        void RenderPoliceSetup()
        {
            var game = PoliceSetup.Mode; bool two = PoliceSetup.Two;
            string about = game == PoliceSetup.Game.Getaway ? "Escape the police. A cop is already behind you when it starts; more join the longer you stay free (heat 1 to 5: backups, roadblocks, the helicopter). Stay out of every cop's sight to escape; the round limit counts as escaped. The Getaway Top 10 keeps the longest times free."
                : game == PoliceSetup.Game.SpeedPatrol ? "You are the cop. Point the radar at a car to clock it; lights on (RB / H) near a clocked speeder to chase it; stay close to fill the pull-over meter. Catch: 100 + 10 per mph over. Hitting other traffic −50; stopping a car that was not speeding −25. Traffic is on."
                : two ? "Two players: one is the cop in the patrol car, the other runs; then the roles swap. Within 10 m and the runner under 7 m/s fills the bust meter. Nothing is recorded." : "You are the cop, chasing an AI runner (to be chased by AI cops, play Getaway). Within 10 m and the runner under 7 m/s fills the bust meter. Nothing is recorded.";
            Clear("POLICE", about + (two ? "\nTwo players, split screen: A or Start on another controller joins it as player 2." : "") + (splitNote != "" && two ? "   ·   " + splitNote : ""));
            details.fontSize = 17; details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = two ? 104 : 84;
            int n = 0;
            Step(n++, "police-game", "Game:   " + (game == PoliceSetup.Game.Getaway ? "Getaway   (you run; the cops are AI)" : game == PoliceSetup.Game.SpeedPatrol ? "Speed Patrol   (catch speeders for points)" : "Cop vs Runner"), d => { PoliceSetup.Mode = (PoliceSetup.Game)(((int)PoliceSetup.Mode + d + 3) % 3); Show(); });
            Step(n++, "police-players", "Players:   " + (two ? "2   (split screen)" : "1   (full screen)"), d => { PoliceSetup.Two = !PoliceSetup.Two; Show(); });
            int devices = -1;
            if (two)
            {
                devices = n;
                Step(n++, "split-p1", "Player 1:   " + (SplitScreen.P1Device == null ? "press A on a controller, or Enter" : SplitScreen.DeviceName(SplitScreen.P1Device)), d => CycleSlot(1, d));
                if (game == PoliceSetup.Game.Getaway)
                    Step(n++, "split-p2", "Player 2:   " + (PoliceSetup.P2AiRunner ? "the AI runs too   (left / right: a person)" : !SplitScreen.P2Ai && SplitScreen.P2Device != null ? SplitScreen.DeviceName(SplitScreen.P2Device) + "   (left / right: the AI)" : "press A on another controller   (left / right: the AI)"),
                        d => { PoliceSetup.P2AiRunner = !PoliceSetup.P2AiRunner; Show(); });
                else Step(n++, "split-p2", "Player 2:   " + (!SplitScreen.P2Ai && SplitScreen.P2Device != null ? SplitScreen.DeviceName(SplitScreen.P2Device) : "press A on another controller"), d => CycleSlot(2, d));
                Row(n++, "split-garage2", $"Player 2 garage…   {VehicleProfile.Find(SplitScreen.P2Vehicle).Name}", () => { splitGarage = true; garagePlayer = 2; OpenGarage("all"); });
                if (game == PoliceSetup.Game.CopVsRunner) Step(n++, "police-cop", "Cop first:   " + SplitScreen.NameOf(PoliceSetup.CopFirst) + "   (then the roles swap)", d => { PoliceSetup.CopFirst = 3 - PoliceSetup.CopFirst; Show(); });
            }
            if (game == PoliceSetup.Game.Getaway)
            {
                string[] names = { "Easy   (heat every 45 s, escape in 20 s, at most 4 cops)", "Normal   (heat every 30 s, escape in 30 s)", "Hard   (heat every 20 s, escape in 40 s)" };
                Step(n++, "police-difficulty", "Difficulty:   " + names[Mathf.Clamp(PoliceSetup.Difficulty, 0, 2)], d => { PoliceSetup.Difficulty = Mathf.Clamp(PoliceSetup.Difficulty + d, 0, 2); Show(); });
            }
            else Step(n++, "police-copvehicle", "Cop's vehicle:   " + VehicleProfile.Find(PoliceSetup.CopVehicle).Name, d => { int i = System.Array.IndexOf(CopVehicles, PoliceSetup.CopVehicle); PoliceSetup.CopVehicle = CopVehicles[((i < 0 ? 0 : i) + d + CopVehicles.Length) % CopVehicles.Length]; Show(); });
            var lim = PoliceSetup.Limits;
            Step(n++, "police-limit", "Round limit:   " + PoliceSetup.Minutes + " minutes", d => { int i = System.Array.IndexOf(lim, PoliceSetup.Minutes); PoliceSetup.Minutes = lim[Mathf.Clamp((i < 0 ? 1 : i) + d, 0, lim.Length - 1)]; Show(); });
            Row(n++, "police-garage", game == PoliceSetup.Game.Getaway ? "Your vehicle…   (you run in it)" : game == PoliceSetup.Game.CopVsRunner && two ? "Player 1 garage…   (the runner's vehicle)" : "Garage…", () => OpenGarage("all"));
            if (two) Step(n++, "split-layout", "Screen:   " + (SplitScreen.LeftRight ? "Left / right" : "Top / bottom (player 1 on top)"), d => { GameSettings.Settings.splitLeftRight = !GameSettings.Settings.splitLeftRight; if (!CheckRun.Active) GameSettings.SaveSettings(); Show(); });
            string go = game == PoliceSetup.Game.Getaway ? "START GETAWAY" : game == PoliceSetup.Game.SpeedPatrol ? "START PATROL" : "START CHASE";
            int start = n++; Row(start, "police-start", PoliceReady ? go : go + "   (waiting for player 2)", StartPolice);
            var colors = buttons[start].colors; colors.normalColor = new(.1f, .38f, .35f); buttons[start].colors = colors; buttons[start].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 54; buttons[start].interactable = PoliceReady;
            Row(n++, "police-back", "Back", BackPage);
            if (devices >= 0) { buttons[devices].transform.Find("Label").GetComponent<UnityEngine.UI.Text>().color = new Color(.3f, .95f, .81f); buttons[devices + 1].transform.Find("Label").GetComponent<UnityEngine.UI.Text>().color = new Color(1, .74f, .25f); }
        }
        void StartPolice()
        {
            if (!PoliceReady) return;
            if (car && vehicles && HelicopterFlight.Flying(car)) vehicles.Select("original");   // Round 63: the helicopter is Free Roam only
            StuntEvent.End();
            if (race && race.State != RaceController.Stage.Free) race.EndRace();
            if (SplitScreen.Active && !PoliceSession.OwnsSplit) SplitScreen.End();
            if (PoliceSetup.Two && PoliceSetup.Mode == PoliceSetup.Game.Getaway && PoliceSetup.P2AiRunner) SplitScreen.P2Ai = true;
            title = false; CloseMenu();
            PoliceSession.Launch();
        }
        void RenderPolicePause()
        {
            if (PoliceSession.Done) { RenderPoliceResults(); return; }
            string what = GetawayChase.Current ? $"Getaway   ·   heat {GetawayChase.Current.Heat}   ·   {PoliceKit.Mss(GetawayChase.Current.Clock)}" : SpeedPatrol.Current ? $"Speed Patrol   ·   {SpeedPatrol.Current.Cops.FirstOrDefault()?.points ?? 0} points" : "Cop vs Runner";
            Clear("PAUSED", what);
            int n = 0;
            Row(n++, "resume", "RESUME", CloseMenu);
            Row(n++, "police-restart", "RESTART", () => Confirm("RESTART?", "A new round from where you are.", "RESTART", () => { CloseMenu(); PoliceSession.Rematch(); }));
            Row(n++, "police-leave", "LEAVE", () => Confirm("LEAVE THE POLICE MODE?", "Back to Free Roam; this round is not recorded.", "LEAVE", () => { PoliceSession.Stop(); CloseMenu(); }));
            Row(n++, "settings", "SETTINGS", () => Navigate("settings-audio"));
            Row(n++, "quit", "QUIT GAME", () => Confirm("QUIT CHASTAIN RUSH?", "", "QUIT", Quit));
        }
        void ShowPoliceResults()
        {
            if (!started) return;
            if (!open) OpenMenu(false);
            pages.Clear(); page = "police-results"; modal = null; Show();
        }
        void RenderPoliceResults()
        {
            int n = 0; var g = GetawayChase.Current; var sp = SpeedPatrol.Current; var pc = PoliceChase.Current;
            if (g)
            {
                var humans = g.Runners.Where(r => r.human).ToList(); int w = g.Winner;
                string verdict = humans.Count < 2 ? (humans.Count > 0 ? g.Verdict(humans[0]) : "") : w == 0 ? "A DRAW" : PoliceKit.NameOf(w).ToUpperInvariant() + " STAYED FREE LONGEST";
                Clear("GETAWAY RESULTS", verdict + "   ·   " + PoliceSetup.Minutes + "-minute round   ·   " + PoliceSetup.DifficultyName);
                var widths = new[] { .2f, .26f, .14f, .12f, .14f, .14f }; int first = n;
                TableRow(n++, "header", new[] { "Runner", "Result", "Time free", "Top heat", "Dodged", "Blocks passed" }, widths, () => { });
                for (int i = 0; i < humans.Count; i++) { var r = humans[i]; int place = i < g.Places.Count ? g.Places[i] : 0; TableRow(n++, "getaway-run-" + r.player, new[] { PoliceKit.NameOf(r.player) + (place > 0 ? $"  (#{place})" : ""), g.Verdict(r), PoliceKit.Mss(r.freeSeconds), r.topHeat.ToString(), r.dodged.ToString(), r.blocks.ToString() }, widths, () => { }, humans.Count == 1 || r.player == w); }
                Table(first, n, 28, 18); first = n;
                var top = GetawayChase.Top(PoliceSetup.Minutes); var tw = new[] { .12f, .34f, .2f, .16f, .18f };
                TableRow(n++, "top-header", new[] { "Top 10", PoliceSetup.Minutes + " min", "Time free", "Heat", "Date" }, tw, () => { });
                for (int i = 0; i < top.Count; i++) { var e = top[i]; TableRow(n++, "getaway-top-" + i, new[] { "#" + (i + 1), e.name, PoliceKit.Mss(e.seconds), e.heat.ToString(), e.date }, tw, () => { }, g.Places.Contains(i + 1)); }
                if (top.Count == 0) TableRow(n++, "getaway-top-none", new[] { "", "No runs yet", "", "", "" }, tw, () => { });
                Table(first, n, 25, 17);
            }
            else if (sp)
            {
                int w = sp.Winner;
                string verdict = sp.Cops.Count < 2 ? "SCORE  " + (sp.Cops.Count > 0 ? sp.Cops[0].points : 0) : w == 0 ? "A DRAW" : PoliceKit.NameOf(w).ToUpperInvariant() + " WINS";
                Clear("SPEED PATROL RESULTS", verdict + "   ·   " + PoliceSetup.Minutes + "-minute round   ·   speeders that got away: " + sp.GotAway);
                var widths = new[] { .26f, .14f, .14f, .22f, .24f }; int first = n;
                TableRow(n++, "header", new[] { "Cop", "Points", "Catches", "Fastest caught", "Penalties" }, widths, () => { });
                for (int i = 0; i < sp.Cops.Count; i++)
                {
                    var c = sp.Cops[i]; int place = i < sp.Places.Count ? sp.Places[i] : 0;
                    TableRow(n++, "patrol-cop-" + c.player, new[] { PoliceKit.NameOf(c.player) + (place > 0 ? $"  (Top 10 #{place})" : ""), c.points.ToString(), c.catches.ToString(), c.catches > 0 ? $"{c.fastest:0} mph over" : "—", c.penalties == 0 ? "none" : $"{c.hits} hit, {c.noViolations} no violation" }, widths, () => { }, c.player == w || sp.Cops.Count == 1);
                }
                Table(first, n, 28, 18); first = n;
                var top = SpeedPatrol.Top(PoliceSetup.Minutes); var tw = new[] { .12f, .34f, .18f, .16f, .2f };
                TableRow(n++, "top-header", new[] { "Top 10", PoliceSetup.Minutes + " min", "Points", "Catches", "Date" }, tw, () => { });
                for (int i = 0; i < top.Count; i++) { var e = top[i]; TableRow(n++, "patrol-top-" + i, new[] { "#" + (i + 1), e.name, e.points.ToString(), e.catches.ToString(), e.date }, tw, () => { }, sp.Places.Contains(i + 1)); }
                if (top.Count == 0) TableRow(n++, "patrol-top-none", new[] { "", "No rounds yet", "", "", "" }, tw, () => { });
                Table(first, n, 25, 17);
            }
            else if (pc)
            {
                int w = pc.Winner;
                Clear("COP VS RUNNER RESULTS", (pc.Solo ? pc.SoloVerdict : w == 0 ? "A DRAW" : PoliceKit.NameOf(w).ToUpperInvariant() + " WINS") + "   ·   " + PoliceSetup.Minutes + "-minute rounds   ·   nothing is recorded");
                var widths = new[] { 1f }; int first = n;
                foreach (var r in pc.Runs) TableRow(n++, "chase-run-" + n, new[] { pc.Line(r) }, widths, () => { }, !pc.Solo && r.runner == w);
                Table(first, n, 28, 18);
            }
            else { Clear("POLICE", "No results."); Row(0, "back", "Back to Free Roam", CloseMenu); return; }
            Row(n, "rematch", "REMATCH", () => { CloseMenu(); PoliceSession.Rematch(); }); var cl = buttons[n].colors; cl.normalColor = new(.1f, .38f, .35f); buttons[n].colors = cl; buttons[n].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 54; n++;
            Row(n++, "change-setup", "Change setup", () => { PoliceSession.Stop(); pages.Clear(); page = "root"; OpenPoliceSetup(); });
            Row(n, "free", "Back to Free Roam", () => { PoliceSession.Stop(); CloseMenu(); });
            for (int k = n - 2; k <= n; k++) buttons[k].transform.SetAsLastSibling();
        }
            // RECORDS > the police Top 10s (Racer's Records > GETAWAY and > SPEED PATROL): one page each after the stunt and activity pages, the round
        // length on its own row (left / right); getaway-v1.json and speed-patrol-v1.json beside the race records
        int policeRecordMinutes = 5;
        static int[] RecordMinutes => PoliceSetup.Limits;
        void RenderPoliceRecords(int which, int pagesN)
        {
            Step(0, "record-track", which == 0 ? "GETAWAY TOP 10" : "SPEED PATROL TOP 10", d => { recordTrack = (recordTrack + d + pagesN) % pagesN; Show(); });
            Step(1, "record-minutes", policeRecordMinutes + "-minute rounds", d => { int i = System.Array.IndexOf(RecordMinutes, policeRecordMinutes); policeRecordMinutes = RecordMinutes[(Mathf.Max(0, i) + d + RecordMinutes.Length) % RecordMinutes.Length]; Show(); });
            Controls(0, 2);
            details.gameObject.SetActive(true); details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 28;
            details.text = which == 0 ? "GETAWAY · the longest free (caught or not), Free Roam > POLICE" : "SPEED PATROL · the most points, Free Roam > POLICE";
            int row = 2, first = 2;
            if (which == 0)
            {
                var top = GetawayChase.Top(policeRecordMinutes); var tw = new[] { .1f, .3f, .18f, .12f, .14f, .16f };
                TableRow(row++, "pr-header", new[] { "Rank", "Name", "Time free", "Heat", "Result", "Date" }, tw, () => { });
                for (int i = 0; i < top.Count; i++) { var e = top[i]; TableRow(row++, "pr-" + i, new[] { (i + 1).ToString(), e.name, PoliceKit.Mss(e.seconds), e.heat.ToString(), e.outcome, e.date }, tw, () => { }, e.name == PlayerNames.Player); }
                if (top.Count == 0) TableRow(row++, "pr-none", new[] { "", "No runs yet", "", "", "", "" }, tw, () => { });
            }
            else
            {
                var top = SpeedPatrol.Top(policeRecordMinutes); var tw = new[] { .1f, .36f, .18f, .16f, .2f };
                TableRow(row++, "pr-header", new[] { "Rank", "Name", "Points", "Catches", "Date" }, tw, () => { });
                for (int i = 0; i < top.Count; i++) { var e = top[i]; TableRow(row++, "pr-" + i, new[] { (i + 1).ToString(), e.name, e.points.ToString(), e.catches.ToString(), e.date }, tw, () => { }, e.name == PlayerNames.Player); }
                if (top.Count == 0) TableRow(row++, "pr-none", new[] { "", "No rounds yet", "", "", "" }, tw, () => { });
            }
            Table(first, row, 26, 17);
            Row(row, "back", "Back", BackPage); buttons[row].transform.SetAsLastSibling();
            details.transform.SetSiblingIndex(1);
        }
    }
}
