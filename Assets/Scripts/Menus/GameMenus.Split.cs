using System;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace ChastainRush
{
    // Round 37 (Part B): Racer's RaceMenus.Split (0.90 Part D, 0.91 Part B, 0.92 Parts B and F, 0.94 Parts A to C, 0.106 Part D), its setup screen,
    // its pause menu and its results, rewritten into this game's menus. SPLIT SCREEN on the title and the Free Roam menus. Player 1 is the device
    // that opened the screen (a controller, or the keyboard with the mouse); player 2 joins by pressing A or Start on another controller (or Enter
    // on the keyboard when player 1 is on a controller), or is the AI. Left / right on a player's row changes their device (player 2 also through
    // the AI). While the setup is up only player 1's device works its rows (A from another device joins it instead). Each player has their own
    // garage (vehicle, color, rider) and name; player 2's name is typed on the on-screen keyboard with player 2's own controller. B goes back.
    // Chastain Rush edits: the race's route, laps, direction, opponents, difficulty, time and weather are the race setup's own rows (the same
    // settings, race_settings.json); traffic is Settings > Gameplay's; the screen layout is kept in settings.json (splitLeftRight).
    public sealed partial class GameMenus
    {
        int garagePlayer = 1;   // whose garage (and rider page) is open: 2 = player 2's (split screen)
        static readonly string[] SplitModes = { "Race", "Free Roam", "Stunt park", "Stunt event" };

        void OpenSplitSetup()
        {
            var keyboard = (InputDevice)Keyboard.current; var opener = MenuInput.Controller && MenuInput.Pad != null ? MenuInput.Pad : keyboard;
            if (SplitScreen.P1Device == null || !SplitScreen.P1Device.added) SplitScreen.P1Device = opener;
            if (SplitScreen.P2Device != null && (!SplitScreen.P2Device.added || SplitScreen.P2Device == SplitScreen.P1Device)) SplitScreen.P2Device = null;
            if (SplitScreen.P2Device == null) SplitScreen.P2Ai = true;
            VehicleSwitch.LoadSaved(2, out SplitScreen.P2Vehicle, out SplitScreen.P2Colour);
            if (VehicleProfile.IndexOf(SplitScreen.P2Vehicle) < 0) SplitScreen.P2Vehicle = "atv";
            if (page != "split") Navigate("split"); else Show();
        }
        // The devices a player slot can take: the keyboard and every connected controller, never the other slot's (null = the AI, player 2)
        static InputDevice[] FreeDevices(InputDevice other) => new InputDevice[] { Keyboard.current }.Concat(Gamepad.all).Where(d => d != null && d.added && d != other).ToArray();
        static bool Assigned(InputDevice d) => d == SplitScreen.P1Device || (d == SplitScreen.P2Device && !SplitScreen.P2Ai);
        bool SplitSetupPage => open && (page == "split" || page == "garage" && garagePlayer > 0 && splitGarage || page == "rider" && splitGarage) && modal == null;
        bool splitGarage;
        // A or Start on a controller that no player holds (or Enter on a keyboard nobody holds) joins it as player 2 (Racer's UpdateSplitJoin)
        void UpdateSplitJoin()
        {
            if (!open || (page != "split" && page != "police") || modal != null) return;   // Round 39: the POLICE page joins player 2 too
            if (SplitScreen.P1Device != null && !SplitScreen.P1Device.added) { SplitScreen.P1Device = null; Show(); return; }
            if (SplitScreen.P2Device != null && !SplitScreen.P2Device.added) { SplitScreen.P2Device = null; SplitScreen.P2Ai = true; Show(); return; }
            foreach (var pad in Gamepad.all) if (!Assigned(pad) && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)) { Join(pad); return; }
            var k = Keyboard.current; if (k != null && !Assigned(k) && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) { Join(k); return; }
        }
        void Join(InputDevice d)
        {
            if (SplitScreen.P1Device == null) SplitScreen.P1Device = d; else { SplitScreen.P2Device = d; SplitScreen.P2Ai = false; }
            MenuInput.ConsumeThroughRelease(); splitNote = (SplitScreen.P1Device == d ? "Player 1" : "Player 2") + " joined: " + SplitScreen.DeviceName(d); Show();
        }
        string splitNote = "";
        void CycleSlot(int player, int d)
        {
            var other = player == 1 ? (SplitScreen.P2Ai ? null : SplitScreen.P2Device) : SplitScreen.P1Device;
            var choices = FreeDevices(other).ToList(); if (player == 2) choices.Insert(0, null);
            var current = player == 1 ? SplitScreen.P1Device : SplitScreen.P2Ai ? null : SplitScreen.P2Device;
            if (choices.Count < 2) { splitNote = "No other device: connect another controller"; Show(); return; }
            var next = choices[(Mathf.Max(0, choices.IndexOf(current)) + d + choices.Count) % choices.Count];
            if (player == 1) SplitScreen.P1Device = next; else { SplitScreen.P2Device = next; SplitScreen.P2Ai = next == null; }
            Show();
        }
        // Racer 0.91 Part B: the menus answer only these devices (the mouse too with the keyboard); none = every device
        void RestrictMenuDevices(params InputDevice[] devices)
        {
            ReadOnlyArray<InputDevice>? only = null;
            var list = (devices ?? new InputDevice[0]).Where(d => d != null && d.added).Distinct().ToList();
            if (list.Count > 0) { if (list.Any(d => d is Keyboard) && Mouse.current != null) list.Add(Mouse.current); only = new ReadOnlyArray<InputDevice>(list.ToArray()); }
            if (ownedUiActions) ownedUiActions.devices = only;
            if (menuActions) menuActions.devices = only;
        }
        bool restricted;
        void UpdateSplitDevices()
        {
            InputDevice who = null;
            if (open && modal == null && (page == "split" || page == "police")) who = SplitScreen.P1Device;
            else if (open && KeyboardPage && keyboardFor == 2) who = SplitScreen.P2Device;
            else if (open && modal == null && splitGarage && (page == "garage" || page == "rider")) who = garagePlayer == 2 && SplitScreen.P2Device != null ? SplitScreen.P2Device : SplitScreen.P1Device;
            if (who != null) { RestrictMenuDevices(who); restricted = true; }
            else if (restricted) { restricted = false; RestrictMenuDevices(); }
        }
        int keyboardFor;

        void RenderSplit()
        {
            splitGarage = false; garagePlayer = 1; ResetSplitRowColors();
            var mode = SplitScreen.Mode; bool raceMode = mode == SplitScreen.Kind.Race;
            Clear("SPLIT SCREEN", "Two players on this PC, each with their own controller (the keyboard goes with player 1). Player 2 can be the AI."
                + "\nA or Start on another controller joins it as player 2." + (splitNote != "" ? "   ·   " + splitNote : ""));
            details.fontSize = 18; details.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 58;
            int n = 0;
            Step(n++, "split-p1", "Player 1:   " + (SplitScreen.P1Device == null ? "press A on a controller, or Enter" : SplitScreen.DeviceName(SplitScreen.P1Device)), d => CycleSlot(1, d));
            Step(n++, "split-p2", "Player 2:   " + (SplitScreen.P2Ai ? "AI driver   (A on another controller joins)" : SplitScreen.DeviceName(SplitScreen.P2Device)), d => CycleSlot(2, d));
            Step(n++, "split-mode", "Mode:   " + SplitModes[(int)mode], d => { SplitScreen.Mode = (SplitScreen.Kind)(((int)SplitScreen.Mode + d + 4) % 4); FixSplitVehicles(); Show(); });
            StepAct(n++, "split-p1-name", "Player 1 name:   " + PlayerNames.Player + "   (A: new name)", d => { var nm = NextKnownName(PlayerNames.Player, d, SplitScreen.P2Ai ? null : SplitScreen.NameOf(2)); if (nm != PlayerNames.Player) { GameSettings.Settings.playerName = nm; if (!CheckRun.Active) GameSettings.SaveSettings(); } Show(); },
                () => { keyboardFor = 1; OpenNameEntry(PlayerNames.Named ? PlayerNames.Player : "", v => { if (!SplitScreen.P2Ai && v == SplitScreen.NameOf(2)) { keyboardError = "Player 2 already has that name."; return false; } GameSettings.Settings.playerName = v; PlayerNames.Remember(v); if (!CheckRun.Active) GameSettings.SaveSettings(); return true; }, null, "PLAYER 1'S NAME"); });
            if (!SplitScreen.P2Ai)
                StepAct(n++, "split-p2-name", "Player 2 name:   " + SplitScreen.NameOf(2) + "   (A: new name, on player 2's controller)", d => { var nm = NextKnownName(SplitScreen.NameOf(2), d, PlayerNames.Player); GameSettings.Settings.player2Name = nm; if (!CheckRun.Active) GameSettings.SaveSettings(); Show(); },
                    () => { keyboardFor = 2; OpenNameEntry("", v => { if (v == PlayerNames.Player) { keyboardError = "Player 1 already has that name."; return false; } GameSettings.Settings.player2Name = v; PlayerNames.Remember(v); if (!CheckRun.Active) GameSettings.SaveSettings(); return true; }, null, "PLAYER 2'S NAME"); });
            Row(n++, "split-garage1", $"Player 1 garage…   {VehicleProfile.Find(car.GetComponent<VehicleConfiguration>().profileId).Name}", () => { splitGarage = true; garagePlayer = 1; OpenGarage(SplitGarageFor); });
            Row(n++, "split-garage2", $"Player 2 garage…   {VehicleProfile.Find(SplitScreen.P2Vehicle).Name}", () => { splitGarage = true; garagePlayer = 2; OpenGarage(SplitGarageFor); });
            if (raceMode)
            {
                var routes = race ? race.Routes() : new System.Collections.Generic.List<(string file, string name)>();
                int at = race ? Mathf.Max(0, routes.FindIndex(x => x.file == (string.IsNullOrEmpty(Current.route) ? race.routeFileName : Current.route))) : 0;
                var rd = race ? race.RouteData : null; bool oneWay = rd != null && !rd.Allows("reverse");
                Step(n++, "split-route", $"Route:   {(routes.Count > 0 ? routes[at].name : "-")}", d => { if (routes.Count < 2) return; Current.route = routes[(at + d + routes.Count) % routes.Count].file; SaveSettings(); ApplySettings(); race.Reload(); FixSplitVehicles(); Show(); });
                Step(n++, "split-laps", $"Laps:   {Current.laps}", d => { Current.laps = Mathf.Clamp(Current.laps + d, 1, 9); SaveSettings(); ApplySettings(); Show(); });
                Step(n++, "split-direction", oneWay ? "Direction:   Forward (one way only)" : $"Direction:   {(Current.direction == "reverse" ? "Reverse" : "Forward")}", d => { if (oneWay) return; Current.direction = Current.direction == "reverse" ? "forward" : "reverse"; SaveSettings(); ApplySettings(); Show(); });
                Step(n++, "split-rivals", $"AI rivals:   {(Current.opponents ? "3 AI drivers" : "None")}", d => { Current.opponents = !Current.opponents; SaveSettings(); ApplySettings(); Show(); });
                Step(n++, "split-difficulty", $"Difficulty:   {DifficultyNames[Current.difficulty]}", d => { Current.difficulty = Mathf.Clamp(Current.difficulty + d, 0, 2); SaveSettings(); ApplySettings(); Show(); });
                var order = LookPresets.MenuOrder; int ti = Mathf.Max(0, Array.IndexOf(order, (TimeOfDay)Mathf.Clamp(Current.timeOfDay, 0, 3)));
                Step(n++, "split-time", "Time of Day:   " + order[ti], d => { Current.timeOfDay = (int)order[(ti + d + order.Length) % order.Length]; SaveSettings(); ApplySettings(); Show(); });
                Step(n++, "split-weather", "Weather:   " + (Weather)Mathf.Clamp(Current.weather, 0, 2), d => { Current.weather = (Mathf.Clamp(Current.weather, 0, 2) + d + 3) % 3; SaveSettings(); ApplySettings(); Show(); });
            }
            else RoamWeatherRow(n++);
            Step(n++, "split-layout", "Screen:   " + (SplitScreen.LeftRight ? "Left / right" : "Top / bottom (player 1 on top)"), d => { GameSettings.Settings.splitLeftRight = !GameSettings.Settings.splitLeftRight; if (!CheckRun.Active) GameSettings.SaveSettings(); SplitScreen.Session?.Relayout(); Show(); });
            int start = n++; Row(start, "split-start", SplitScreen.Ready ? "START" : "START   (waiting for player 2)", StartSplit);
            var colors = buttons[start].colors; colors.normalColor = new(.1f, .38f, .35f); buttons[start].colors = colors; buttons[start].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 54; buttons[start].interactable = SplitScreen.Ready;
            Row(n++, "split-back", "Back", BackPage);
            buttons[0].transform.Find("Label").GetComponent<UnityEngine.UI.Text>().color = new Color(.3f, .95f, .81f);
            buttons[1].transform.Find("Label").GetComponent<UnityEngine.UI.Text>().color = new Color(1, .74f, .25f);
        }
        void ResetSplitRowColors() { foreach (var b in buttons) b.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().color = Color.white; }
        string SplitGarageFor => SplitScreen.Mode == SplitScreen.Kind.Stunt ? "stunt" : SplitScreen.Mode == SplitScreen.Kind.Race ? "race" : "all";
        // the next name used before on this PC (not the other player's)
        static string NextKnownName(string current, int d, string other)
        {
            var names = (GameSettings.Settings.knownNames ?? new string[0]).Where(k => !string.IsNullOrEmpty(k) && k != other).ToList(); if (names.Count == 0) return current;
            int i = names.IndexOf(current); return names[((i < 0 ? 0 : i) + d + names.Count) % names.Count];
        }
        // each player's vehicle is one the mode allows: stunt vehicles in the Stunt event; a race's route vehicles and no stunt vehicle in a race
        void FixSplitVehicles()
        {
            bool stunt = SplitScreen.Mode == SplitScreen.Kind.Stunt, raceMode = SplitScreen.Mode == SplitScreen.Kind.Race; var rd = race ? race.RouteData : null;
            bool Ok(string id) { var p = VehicleProfile.Find(id); return stunt ? p.Stunt : raceMode ? !p.Stunt && (rd == null || rd.AllowsVehicle(id)) : true; }
            string Pick(string fallback) => VehicleProfile.All.Where(p => Ok(p.Id)).Select(p => p.Id).DefaultIfEmpty(fallback).First();
            var cfg = car.GetComponent<VehicleConfiguration>();
            if (!Ok(cfg.profileId) && vehicles) vehicles.Select(stunt ? StuntChoice : Ok("original") ? "original" : Pick("original"));
            if (!Ok(SplitScreen.P2Vehicle)) { SplitScreen.P2Vehicle = stunt ? "stuntbike" : Ok("atv") ? "atv" : Pick("atv"); VehicleSwitch.SaveFor(2, SplitScreen.P2Vehicle, SplitScreen.P2Colour); }
        }

        void StartSplit()
        {
            if (!SplitScreen.Ready) return;
            FixSplitVehicles(); ApplySettings();
            StuntEvent.End();
            if (race && race.State != RaceController.Stage.Free) race.EndRace();
            title = false; CloseMenu();
            var mode = SplitScreen.Mode;
            if (mode == SplitScreen.Kind.StuntPark || mode == SplitScreen.Kind.Stunt)
            {
                var respawn = car.GetComponent<VehicleRespawn>(); if (respawn && StuntPark.Current) respawn.PlaceAt(StuntPark.StartPos, StuntPark.Current.startYaw);
            }
            var s = SplitScreen.Begin(mode);
            if (mode == SplitScreen.Kind.Race) { if (race) race.StartRace(); }
            else if (mode == SplitScreen.Kind.Stunt) StuntEvent.Begin(s.P1, s.P2, s.Activities2);
            else if (SplitScreen.P2Ai) { if (mode == SplitScreen.Kind.StuntPark) s.StuntRide(); else s.Cruise(); }   // Round 38: in the park, the stunt AI
        }
        public void EndSplit(bool toSetup)
        {
            SplitScreen.End();
            if (toSetup) { if (!open) OpenMenu(false); pages.Clear(); page = "root"; OpenSplitSetup(); }
            else CloseMenu();
        }
        // a player's controller went (SplitSession): the pause menu, with the message
        public void PauseForSplit() { if (!started) return; if (!open) OpenMenu(false); else Show(); }
        public void RefreshIfOpen() { if (open) Show(); }

        // Racer's RenderSplitPause: resume (not while a controller is missing), restart, the setup, end, settings, quit
        void RenderSplitPause()
        {
            var s = SplitScreen.Session; string missing = s ? s.MissingText : null;
            var stage = race ? race.State : RaceController.Stage.Free; var ev = StuntEvent.Current;
            bool racing = stage != RaceController.Stage.Free, finished = racing && race.AllHumansFinished;
            Clear(finished ? "SPLIT SCREEN / RACE OVER" : ev ? "STUNT EVENT / PAUSED" : "SPLIT SCREEN / PAUSED", missing ?? $"{SplitModes[(int)SplitScreen.Mode]}   ·   {SplitScreen.NameOf(1)} and {SplitScreen.NameOf(2)}   ·   either player's controller works this menu.");
            int n = 0;
            Row(n++, "resume", finished ? "SEE THE RESULTS" : "RESUME", CloseMenu); buttons[0].interactable = missing == null;
            if (racing) Row(n++, "restart", finished ? "RACE AGAIN" : "RESTART RACE", () => { if (finished) { CloseMenu(); race.StartRace(); } else Confirm("RESTART THE RACE?", "Both players go back to the grid.", "RESTART", () => { CloseMenu(); race.StartRace(); }); });
            if (ev && ev.State != StuntEvent.Phase.Done) Row(n++, "stunt-restart", "RESTART THE EVENT", () => Confirm("RESTART THE STUNT EVENT?", "A new round; the points so far are cleared.", "RESTART", () => { CloseMenu(); ev.Rematch(); }));
            if (!racing && !ev && StuntPark.Current && !SplitScreen.OneView) Row(n++, "split-stunt", "STUNT EVENT FOR TWO", () => { var ss = SplitScreen.Session; CloseMenu(); var rs = car.GetComponent<VehicleRespawn>(); if (rs) rs.PlaceAt(StuntPark.StartPos, StuntPark.Current.startYaw); if (SplitScreen.P2Ai) ss.StuntRide(); StuntEvent.Begin(ss.P1, ss.P2, ss.Activities2); });   // Round 38: an AI player 2 rides the park's lines
            Row(n++, "change-setup", "CHANGE SETUP", () => Confirm("END AND CHANGE THE SETUP?", "Both players return to the split-screen setup.", "CHANGE SETUP", () => EndSplit(true)));
            Row(n++, "end-split", "END SPLIT SCREEN", () => Confirm("END SPLIT SCREEN?", "Back to one player in Free Roam.", "END", () => EndSplit(false)));
            Row(n++, "records", "RECORDS", OpenRecords);
            Row(n++, "settings", "SETTINGS", () => Navigate("settings-audio"));
            Row(n++, "quit", "QUIT GAME", () => Confirm("QUIT CHASTAIN RUSH?", "", "QUIT", Quit));
        }

        // ---------- player 2's garage and rider page (Racer's RaceMenus.Pick, each player's garage) ----------
        void RenderGarage2()
        {
            var profile = VehicleProfile.Find(SplitScreen.P2Vehicle);
            var all = VehicleProfile.All.Where(v => garageFor == "stunt" ? v.Stunt : garageFor == "race" ? !v.Stunt : true).ToArray(); int at = Mathf.Max(0, Array.FindIndex(all, v => v.Id == profile.Id));
            Clear("PLAYER 2: " + profile.Name + " / " + profile.Class, profile.Description);
            Step(0, "vehicle", $"{profile.Name}   ·   {at + 1} / {all.Length}", d => { SplitScreen.P2Vehicle = all[(at + d + all.Length) % all.Length].Id; VehicleSwitch.SaveFor(2, SplitScreen.P2Vehicle, SplitScreen.P2Colour); Show(); });
            int colour = SplitScreen.P2Colour;
            Step(1, "colour", "Color:    " + (colour < 0 ? "Standard" : VehiclePaint.Name(colour)), d =>
            {
                int next = colour < 0 ? (d > 0 ? 0 : VehiclePaint.Count - 1) : colour + d;
                SplitScreen.P2Colour = next < 0 || next >= VehiclePaint.Count ? -1 : next; VehicleSwitch.SaveFor(2, SplitScreen.P2Vehicle, SplitScreen.P2Colour); Show();
            });
            Row(2, "rider", "Rider…", () => Navigate("rider"));
            Row(3, "done", "Done", BackPage);
            ShowGarageStats(profile); statBlock.SetSiblingIndex(buttons[1].transform.GetSiblingIndex() + 1);
            EnterGarageView();
            if (previewRoot) Destroy(previewRoot);
            previewRoot = new GameObject("Garage display model"); previewRoot.transform.SetParent(transform, false); previewRoot.transform.position = PreviewFocus;
            BuildPlayerTwoPreview(previewRoot.transform);
            foreach (var t in previewRoot.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            FramePreview(false);
        }
        void RenderRider2()
        {
            var look = GameSettings.Settings.rider2 ??= new RiderLook();
            Clear("PLAYER 2'S RIDER", "Choose player 2's rider. Left / right changes a row.");
            Row(0, "randomize", "Randomize", () => { var random = new System.Random(); RiderLook l; do l = RiderLook.Random(random); while (l.SameAs(look)); GameSettings.Settings.rider2 = l; if (!CheckRun.Active) GameSettings.SaveSettings(); Show(); });
            Row(1, "rider-back", "Back", BackPage);
            for (int i = 0; i < RiderLook.Fields; i++) { int field = i; Step(2 + i, "rider-" + i, look.Label(i), d => { look.Step(field, d); if (!CheckRun.Active) GameSettings.SaveSettings(); Show(); }); }
            EnterGarageView();
            if (previewRoot) Destroy(previewRoot);
            previewRoot = new GameObject("Garage display model"); previewRoot.transform.SetParent(transform, false); previewRoot.transform.position = PreviewFocus;
            BuildPlayerTwoPreview(previewRoot.transform);
            foreach (var t in previewRoot.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            FramePreview(true);
        }
        static void BuildPlayerTwoPreview(Transform parent)
        {
            var p = VehicleProfile.Find(SplitScreen.P2Vehicle);
            VehicleVisual.Build(parent, p, null, p.IsPolice && p.Small ? RiderLook.Police : (GameSettings.Settings.rider2 ?? new RiderLook()).Valid(), false);
            if (p.CupEdition) return;
            int c = SplitScreen.P2Colour; if (c >= 0) { VehiclePaint.Apply(parent, VehiclePaint.Of(c)); VehiclePaint.Scheme(parent, c); }
        }
        VehicleProfile GarageProfile => garagePlayer == 2 && splitGarage ? VehicleProfile.Find(SplitScreen.P2Vehicle) : car.GetComponent<VehicleConfiguration>().Profile;
    }
}
