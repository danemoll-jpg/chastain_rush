using System;
using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 84 (A): Racer 1.2's HIGH MODE pages (RaceMenus.High.cs, with RaceMenus.Pick's small turning previews), ported into this game's menus.
    // - HIGH MODE: ON / OFF is the top row of the title, the Free Roam pause and a race's pause, in both modes (A, or left / right). In a race it asks
    //   RESTART or KEEP RACING (a race keeps the mode it started with). Split screen: a HIGH MODE row per player on the setup and the pause.
    // - HIGH MODE's menu (title and Free Roam pause): HIGH MODE, (RESUME DRIVING), FREE ROAM (1 or 2 players), RACE (pick a track and go: the course's
    //   own laps, 3 Easy rivals), CHASES & STUNTS (this game's police modes and the Stunt event, each starting at once with defaults: Easy, 5 minutes),
    //   VEHICLE (one scrolling row of every vehicle with its picture and three stat bars; also in a HIGH race's pause and results), TIME & WEATHER,
    //   REPLAYS, SETTINGS, QUIT GAME. Nothing is locked in this game, so "everything unlocked" needs nothing.
    // - "Would you like to play HIGH MODE?" (HighPrompt asks): YES / NO; B is No.
    public sealed partial class GameMenus
    {
        HighPrompt highPrompt;
        void EnsureHighPrompt() { if (!highPrompt && car) { highPrompt = gameObject.AddComponent<HighPrompt>(); highPrompt.Initialize(this, car); } }
        bool RaceRunning => race && (race.State == RaceController.Stage.Countdown || race.State == RaceController.Stage.Racing);
        static readonly Color HighRowColor = new(.30f, .16f, .05f), OffRowColor = new(.10f, .20f, .25f);
        static string HighLabel => "HIGH MODE:   " + (HighMode.On ? "ON" : "OFF");
        void ColorHighRow(int index, bool on)
        {
            buttons[index].transform.Find("Label").GetComponent<UnityEngine.UI.Text>().color = on ? HighMode.Colour : new Color(.82f, .88f, .88f);
            var c = buttons[index].colors; c.normalColor = on ? HighRowColor : OffRowColor; buttons[index].colors = c;
        }
        void HighRow(int index) { StepAct(index, "high-mode", HighLabel, d => SwitchHigh(), SwitchHigh); ColorHighRow(index, HighMode.On); }   // A switches it too (not a list)
        void SwitchHigh()
        {
            bool on = !HighMode.On; HighMode.Set(on);
            if (on && HighMode.WeatherChoice == 3) HighMode.Roll();
            if (RaceRunning && !SplitScreen.Active)
                Confirm2(on ? "RESTART IN HIGH MODE?" : "RESTART IN NORMAL MODE?", (on ? "HIGH MODE is on." : "HIGH MODE is off.") + " Restart this race now, or keep racing as it is: the change applies from the next race.",
                    "RESTART", StartRace, "KEEP RACING", () => { Show(); HighBadge.Notify((on ? "HIGH MODE" : "Normal mode") + " from the next race", 4); });
            else { HighBadge.Notify(on ? "HIGH MODE is on" : "HIGH MODE is off", 3); Show(); }
        }
        // split screen: a player's own HIGH MODE (A or left / right); in a race it asks to restart (or applies from the next race)
        void PlayerHighRow(int index, int player)
        {
            bool on = player == 1 ? HighMode.P1High : HighMode.P2High;
            StepAct(index, "high-p" + player, SplitScreen.NameOf(player) + ":   HIGH MODE " + (on ? "ON   (bumpers, no wrecks)" : "OFF"), d => SwitchPlayerHigh(player), () => SwitchPlayerHigh(player));
            ColorHighRow(index, on);
        }
        int PlayerHighRows(int n) { PlayerHighRow(n++, 1); if (!SplitScreen.P2Ai) PlayerHighRow(n++, 2); return n; }
        void SwitchPlayerHigh(int player)
        {
            bool on = !(player == 1 ? HighMode.P1High : HighMode.P2High); if (player == 1) HighMode.P1High = on; else HighMode.P2High = on;
            string who = SplitScreen.NameOf(player) + ": HIGH MODE " + (on ? "on" : "off");
            if (SplitScreen.Active && RaceRunning) Confirm2("RESTART THE RACE?", who + ". Restart this race now, or keep racing as it is: the change applies from the next race.", "RESTART", () => { CloseMenu(); race.StartRace(); }, "KEEP RACING", () => Show());
            else Show();
        }

        // ---------- the question (HighPrompt) ----------
        public void AskHighMode()
        {
            if (!started) return;
            if (!open) OpenMenu(false);
            string what = race && race.State != RaceController.Stage.Free ? "\nYes restarts this race in HIGH MODE." : "\nYes carries on in Free Roam in HIGH MODE.";
            Confirm2("WOULD YOU LIKE TO PLAY HIGH MODE?", "HIGH MODE is the simple, forgiving way to play: bumpers along the race courses, no wrecks, quicker resets." + what,
                "YES", AcceptHighMode, "NO", DeclineHighMode);
        }
        void AcceptHighMode()
        {
            HighMode.Set(true); if (HighMode.WeatherChoice == 3) HighMode.Roll();
            if (race && race.State != RaceController.Stage.Free) { StartRace(); return; }
            CloseMenu(); HighBadge.Notify("HIGH MODE is on", 4);
        }
        void DeclineHighMode() { HighMode.Declined = true; CloseMenu(); HighBadge.Notify("No problem. HIGH MODE is on the main menu any time.", 6); }

        // ---------- HIGH MODE's main menu (title and Free Roam pause) ----------
        static string TimeWeatherLine => HighMode.TimeNames[HighMode.TimeChoice] + "  ·  " + HighMode.WeatherNames[HighMode.WeatherChoice];
        string VehicleName => car ? VehicleProfile.Find(car.GetComponent<VehicleConfiguration>().profileId).Name : "";
        void RenderHighMain()
        {
            Clear("CHASTAIN RUSH   ·   HIGH MODE", "Simple and forgiving: bumpers along the race courses, no wrecks, quicker resets." + (title ? "" : EggLine));
            int n = 0; HighRow(n++);
            if (!title) Row(n++, "resume", "RESUME DRIVING", CloseMenu);
            Row(n++, "roam", "FREE ROAM", () => Navigate("high-roam"));
            Row(n++, "race", "RACE", () => Navigate("high-race"));
            Row(n++, "high-chases", "CHASES & STUNTS", OpenHighChases);
            Row(n++, "high-vehicle", "VEHICLE   ·   " + VehicleName, () => Navigate("high-vehicle"));
            Row(n++, "high-time", "TIME & WEATHER   ·   " + TimeWeatherLine, () => Navigate("high-time"));
            Row(n++, "replays", "REPLAYS", OpenReplays);
            Row(n++, "settings", "SETTINGS", () => Navigate("settings-audio"));
            Row(n++, "quit", "QUIT GAME", () => Confirm("QUIT CHASTAIN RUSH?", "", "QUIT", Quit));
        }
        // a race in HIGH MODE: its pause and its results (the vehicle one row away, the track another; no race setup)
        void RenderHighRacePause()
        {
            bool finished = race.State == RaceController.Stage.Finished;
            Clear(finished ? "RACE OVER   ·   HIGH MODE" : "PAUSED   ·   HIGH MODE", race.Route.Name + ", " + race.Route.Laps + " laps, " + race.Route.Direction + (RaceController.AiCount > 0 ? (race.Route.Air ? ", 3 Easy AI pilots" : ", 3 Easy AI drivers") : ""));
            int n = 0;
            if (!finished) Row(n++, "resume", "RESUME", CloseMenu);
            HighRow(n++);
            if (race.CanSimulateRemaining) Row(n++, "complete", "COMPLETE RACE", () => { race.CompleteRace(); CloseMenu(); });
            Row(n++, "restart", finished ? "RACE AGAIN" : "RESTART RACE", finished ? StartRace : () => Confirm("RESTART THE RACE?", "The race starts again from the grid.", "RESTART", StartRace));
            Row(n++, "high-vehicle", "VEHICLE   ·   " + (highPending != null ? VehicleProfile.Find(highPending).Name + "   (from the next race)" : VehicleName), () => Navigate("high-vehicle"));
            Row(n++, "high-race", "CHANGE TRACK", () => Navigate("high-race"));
            Row(n++, "leave", finished ? "FREE ROAM" : "LEAVE RACE", () => { LoadingScreen.Cover("FREE ROAM", "Chastain Glen, Marietta, Georgia", null); race.EndRace(); CloseMenu(); LoadingScreen.Started(); });
            if (finished && ReplayRecorder.Instance && (ReplayRecorder.Instance.Recording || ReplayRecorder.Instance.Last != null))
            { Row(n++, "watch-replay", "WATCH REPLAY", WatchLastReplay); Row(n++, "save-replay", "SAVE REPLAY", SaveLastReplay); }
            Row(n++, "records", "RECORDS", OpenRecords);
            Row(n++, "settings", "SETTINGS", () => Navigate("settings-audio"));
            Row(n++, "quit", "QUIT GAME", () => Confirm("QUIT CHASTAIN RUSH?", "", "QUIT", Quit));
        }

        bool RenderHighPages()
        {
            switch (page)
            {
                case "high-roam": RenderHighRoam(); return true;
                case "high-two": RenderHighTwo(); return true;
                case "high-race": RenderHighRace(); return true;
                case "high-chases": RenderHighChases(); return true;
                case "high-vehicle": RenderHighVehicle(); return true;
                case "high-time": RenderHighTime(); return true;
            }
            return false;
        }

        // ---------- FREE ROAM: 1 or 2 players ----------
        void RenderHighRoam()
        {
            Clear("FREE ROAM", "Drive the whole map: one player on the full screen, or two players in split screen.");
            Row(0, "roam-1p", "1 PLAYER", () =>
            {
                if (!title) { CloseMenu(); return; }
                title = false; pages.Clear(); LoadingScreen.Cover("FREE ROAM", "Chastain Glen, Marietta, Georgia", null); CloseMenu(); LoadingScreen.Started();
            });
            Row(1, "roam-2p", "2 PLAYERS   (split screen)", () => { PrepareHighPlayers(); SplitScreen.Mode = SplitScreen.Kind.FreeRoam; Navigate("high-two"); });
            Row(2, "back", "Back", BackPage);
        }
        void PrepareHighPlayers()
        {
            var keyboard = (InputDevice)Keyboard.current; var opener = MenuInput.Controller && MenuInput.Pad != null ? MenuInput.Pad : keyboard;
            if (SplitScreen.P1Device == null || !SplitScreen.P1Device.added) SplitScreen.P1Device = opener;
            if (SplitScreen.P2Device != null && (!SplitScreen.P2Device.added || SplitScreen.P2Device == SplitScreen.P1Device)) SplitScreen.P2Device = null;
            if (SplitScreen.P2Device == null) SplitScreen.P2Ai = true;
            VehicleSwitch.LoadSaved(2, out SplitScreen.P2Vehicle, out SplitScreen.P2Colour);
            if (VehicleProfile.IndexOf(SplitScreen.P2Vehicle) < 0) SplitScreen.P2Vehicle = "atv";
            HighMode.P1High = HighMode.P2High = HighMode.On;
        }
        string JoinLine(InputDevice d) => d != null ? SplitScreen.DeviceName(d) + "   ✓" : KeyHints.Of("press A on another controller", "press Enter") + " to join";
        void RenderHighTwo()
        {
            Clear("FREE ROAM   ·   2 PLAYERS", "Split screen; each player chooses HIGH MODE or not. Player 2: " + KeyHints.Of("A or Start on another controller", "Enter on the keyboard") + " joins." + (splitNote != "" ? "   ·   " + splitNote : ""));
            int n = 0;
            Step(n++, "split-p1", "Player 1:   " + JoinLine(SplitScreen.P1Device), d => CycleSlot(1, d));
            Step(n++, "split-p2", "Player 2:   " + (SplitScreen.P2Ai ? "the AI drives   (" + KeyHints.Of("A on another controller", "Enter") + " joins)" : JoinLine(SplitScreen.P2Device)), d => CycleSlot(2, d));
            n = PlayerHighRows(n);
            int start = n++; Row(start, "high-two-start", SplitScreen.Ready ? "START FREE ROAM" : "START FREE ROAM   (waiting for player 2)", () => { SplitScreen.Mode = SplitScreen.Kind.FreeRoam; StartSplit(); });
            StartColors(start, SplitScreen.Ready);
            Row(n++, "back", "Back", BackPage);
        }
        void StartColors(int i, bool ready)
        {
            var colors = buttons[i].colors; colors.normalColor = new(.1f, .38f, .35f); buttons[i].colors = colors; buttons[i].GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 54; buttons[i].interactable = ready;
        }

        // ---------- RACE: pick a track and go ----------
        // every course and direction it is raced in (the street courses first, then the air races)
        List<(string file, string name, string dir, bool air)> HighTracks()
        {
            var list = new List<(string, string, string, bool)>(); if (!race) return list;
            foreach (var (file, name) in race.Routes())
            {
                RouteJson j = null; try { j = JsonUtility.FromJson<RouteJson>(race.RouteJsonOf(file) ?? ""); } catch (Exception) { }
                if (j == null) continue;
                foreach (var d in new[] { "forward", "reverse" }) if (j.Allows(d) && (d == "forward" || j.directions != null && j.directions.Length > 0 || j.reverse_grid_back_m > 0f)) list.Add((file, name, d, j.air));
            }
            return list.OrderBy(t => t.Item4).ToList();
        }
        void RenderHighRace()
        {
            Clear("RACE", "Pick a track and go: its own laps, 3 Easy rivals, bumpers along the course. Air races: larger rings, and a crash puts you back in the air at once.");
            int n = 0;
            Row(n++, "race-2p", "2 PLAYERS   (split screen)…", () => { SplitScreen.Mode = SplitScreen.Kind.Race; OpenSplitSetup(); });
            string cur = race ? race.CurrentFile : "";
            foreach (var t in HighTracks())
            {
                var track = t; if (n >= buttons.Count - 1) break;
                Row(n++, "high-track-" + t.file + "-" + t.dir, (t.air ? "AIR   ·   " : "") + t.name + "   ·   " + (t.dir == "reverse" ? "Reverse" : "Forward"), () => StartHighRace(track.file, track.dir));
            }
            Row(n++, "back", "Back", BackPage);
            // the course last raced first in focus
            var last = buttons.Take(n).FirstOrDefault(b => b.name == "high-track-" + cur + "-" + Current.direction);
            if (last && !pageMemory.ContainsKey(PageKey)) pageMemory[PageKey] = last.name;
        }
        void StartHighRace(string file, string dir)
        {
            if (!race) return;
            Current.route = file; Current.direction = dir; if (race.IsAir(file)) Current.routeAir = file; else Current.routeStreet = file; SaveSettings();
            if (PoliceSession.Running) PoliceSession.Stop();
            StuntEvent.End();
            if (SplitScreen.Active) SplitScreen.End();
            if (race.State != RaceController.Stage.Free) race.EndRace();
            ApplySettings(); race.Reload();
            if (highPending != null && vehicles) { vehicles.Select(highPending, VehicleSwitch.Pick.Roam); highPending = null; }
            EnsureRouteVehicle();
            StartRace();
        }

        // ---------- CHASES & STUNTS ----------
        bool highSolo = true;
        void OpenHighChases() { PrepareHighPlayers(); highSolo = true; Navigate("high-chases"); }
        void RenderHighChases()
        {
            bool solo = highSolo; bool ready = solo || (!SplitScreen.P2Ai && SplitScreen.P2Device != null && SplitScreen.P1Device != null);
            Clear("CHASES & STUNTS", (solo ? "One player on the full screen." : "Two players, split screen.") + " Pick one and it starts: Easy, 5 minutes, your vehicle, time and weather.");
            int n = 0;
            Step(n++, "high-players", "Players:   " + (solo ? "1   (full screen)" : "2   (split screen)"), d => { highSolo = !highSolo; Show(); });
            if (!solo) { Step(n++, "split-p2", "Player 2:   " + (SplitScreen.P2Ai ? KeyHints.Of("press A on another controller", "press Enter") + " to join" : JoinLine(SplitScreen.P2Device)), d => CycleSlot(2, d)); n = PlayerHighRows(n); }
            string Wait(string s) => ready ? s : s + "   (waiting for player 2)";
            var games = new (string id, string label)[] { ("high-getaway", "RUN FROM THE COPS   ·   the police chase you"), ("high-cop", solo ? "BE THE COP   ·   catch the runner" : "BE THE COP   ·   one chases, one runs, then swap"),
                ("high-patrol", "CATCH SPEEDERS   ·   clock them, pull them over"), ("high-stunt", "STUNT EVENT   ·   flips and jumps at " + (StuntPark.Current ? StuntPark.Title : "the stunt park")) };
            for (int g = 0; g < games.Length; g++) { int game = g; Row(n, games[g].id, Wait(games[g].label), () => StartHighChase(game)); buttons[n].interactable = ready; n++; }
            Row(n, "back", "Back", BackPage);
        }
        void StartHighChase(int game)
        {
            bool solo = highSolo;
            if (!solo && (SplitScreen.P2Ai || SplitScreen.P2Device == null)) { splitNote = "Waiting for player 2"; Show(); return; }
            if (game == 3)
            {
                if (!StuntPark.Current) { Confirm("STUNT EVENT", "There is no stunt park in this map build.", "OK", () => Show()); return; }
                var cfg = car.GetComponent<VehicleConfiguration>(); if (cfg && cfg.Profile.Stunt) StuntChoice = cfg.profileId;
                if (race && race.State != RaceController.Stage.Free) race.EndRace();
                if (PoliceSession.Running) PoliceSession.Stop();
                StuntEvent.RivalCount = GameSettings.Settings.stuntRivals; StuntEvent.RivalDifficulty = 0;   // Easy
                if (solo)
                {
                    if (SplitScreen.Active) SplitScreen.End();
                    if (vehicles) vehicles.Select(StuntChoice, VehicleSwitch.Pick.Forced);
                    title = false; CloseMenu(); StuntEvent.Begin(car);
                }
                else { SplitScreen.Mode = SplitScreen.Kind.Stunt; StartSplit(); StuntEvent.RivalDifficulty = 0; }
                return;
            }
            PoliceSetup.Mode = game == 0 ? PoliceSetup.Game.Getaway : game == 1 ? PoliceSetup.Game.CopVsRunner : PoliceSetup.Game.SpeedPatrol;
            PoliceSetup.Two = !solo; PoliceSetup.P2AiRunner = false; PoliceSetup.Minutes = 5; PoliceSetup.Difficulty = 0; PoliceSetup.CopVehicle = "police"; PoliceSetup.CopFirst = 1;
            StartPolice();
        }

        // ---------- VEHICLE: one scrolling row with pictures and stat bars ----------
        string highPick, highPending;   // pending: chosen during a race, put on at the next start
        static VehicleProfile[] HighVehicleList => VehicleProfile.All.ToArray();
        static string Where(VehicleProfile p) => p.IsPlane ? "Free Roam and the air races" : p.Flies ? "Free Roam" : p.Stunt ? "Free Roam and the Stunt event" : p.Small ? "every course" : "not on the trails";
        void RenderHighVehicle()
        {
            var list = HighVehicleList; if (list.Length == 0) { BackPage(); return; }
            string now = highPending ?? car.GetComponent<VehicleConfiguration>().profileId;
            if (renderedKey != PageKey || !list.Any(v => v.Id == highPick)) highPick = (list.FirstOrDefault(v => v.Id == now) ?? list[0]).Id;
            int at = Array.FindIndex(list, v => v.Id == highPick); var p = list[at];
            Clear("VEHICLE", KeyHints.Of("Left / right: every vehicle.   A: drive it.", "Left / right: every vehicle.   Space: drive it.") + "   Color and rider stay as you last set them." + (RaceRunning ? "\nChosen now, it is yours from the next race." : ""));
            var strip = LaterGroup("Every vehicle", content, true, 262); strip.SetSiblingIndex(details.transform.GetSiblingIndex() + 1);
            var hl = strip.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>(); hl.spacing = 12; hl.childForceExpandWidth = false; hl.childAlignment = TextAnchor.MiddleCenter;
            minisUsed = 0;
            for (int k = -2; k <= 2; k++) HighCard(strip, list[((at + k) % list.Length + list.Length) % list.Length], k == 0);
            StepAct(0, "high-vehicle-pick", $"{p.Name}  ({p.Class})   ·   {at + 1} / {list.Length}", d => { highPick = list[(at + d + list.Length) % list.Length].Id; Show(); }, () => ChooseHighVehicle(highPick));
            Row(1, "back", "Back", BackPage);
        }
        void ChooseHighVehicle(string id)
        {
            if (RaceRunning) { highPending = id; HighBadge.Notify("The " + VehicleProfile.Find(id).Name + " from the next race", 3); BackPage(); return; }
            if (vehicles) vehicles.Select(id, VehicleSwitch.Pick.Roam);
            highPending = null; HighBadge.Notify("Driving the " + VehicleProfile.Find(id).Name, 3); BackPage();
        }
        void HighCard(RectTransform strip, VehicleProfile p, bool centre)
        {
            float width = centre ? 236 : 160;
            var cell = Rect("Vehicle " + p.Id, strip); cell.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = width;
            cell.gameObject.AddComponent<UnityEngine.UI.Image>().color = centre ? new Color(.12f, .32f, .35f) : new Color(.06f, .1f, .13f);
            var v = cell.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); v.padding = new RectOffset(6, 6, 6, 6); v.spacing = 3; v.childControlWidth = v.childControlHeight = true; v.childForceExpandHeight = false;
            var pic = Rect("Picture", cell); pic.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 128;
            var raw = MiniPreview(pic, p, Mathf.RoundToInt(width * 1.5f), 190); var r = raw.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var name = Label("Name", cell, centre ? 18 : 16, 24); name.alignment = TextAnchor.MiddleCenter; name.text = p.Name; name.color = centre ? new Color(.3f, .95f, .81f) : Color.white;
            foreach (int i in new[] { 0, 1, 3 })   // top speed, acceleration, handling
            {
                var row = Rect(GarageStats[i].label, cell); row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 18;
                var text = Label("Label", row, 14, 0); text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = new Vector2(.5f, 1); text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero; text.text = GarageStats[i].label; text.color = new Color(.86f, .86f, .8f);
                var track = Rect("Bar", row); track.anchorMin = new Vector2(.52f, .2f); track.anchorMax = new Vector2(1, .8f); track.offsetMin = track.offsetMax = Vector2.zero; track.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(1, 1, 1, .12f);
                var fill = Rect("Fill", track); fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(Mathf.Clamp01(StatFraction(i, p)), 1); fill.offsetMin = fill.offsetMax = Vector2.zero; fill.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.3f, 1, .88f);
            }
            var where = Label("Where", cell, 13, 18); where.alignment = TextAnchor.MiddleCenter; where.text = Where(p); where.color = new Color(.66f, .78f, .8f);
            tableCells.Add(cell.gameObject);
        }
        // Racer 0.92 Part B's small turning previews (RaceMenus.Pick): the garage's display model on a small camera each, far from the world
        sealed class Mini { public Camera cam; public RenderTexture rt; public GameObject model; public string key; public float yaw = 200, radius = 1; public Vector3 centre; public UnityEngine.UI.RawImage raw; }
        readonly List<Mini> minis = new(); int minisUsed;
        static readonly Vector3 MiniBase = new(20000, 20000, 20000);
        static Vector3 MiniFocus(int i) => MiniBase + Vector3.right * 200 * i;
        UnityEngine.UI.RawImage MiniPreview(Transform parent, VehicleProfile p, int width, int height)
        {
            if (minisUsed >= minis.Count)
            {
                var m = new Mini(); var go = new GameObject("Small vehicle preview camera " + minis.Count); go.transform.SetParent(transform, false); m.cam = go.AddComponent<Camera>();
                m.cam.cullingMask = 1 << 31; m.cam.clearFlags = CameraClearFlags.SolidColor; m.cam.backgroundColor = new Color(.06f, .1f, .13f); m.cam.fieldOfView = 30; m.cam.enabled = false; minis.Add(m);
            }
            int index = minisUsed++; var mini = minis[index];
            if (mini.key != p.Id || !mini.model)
            {
                if (mini.model) { mini.model.SetActive(false); Destroy(mini.model); }
                mini.model = new GameObject("Small preview model") { layer = 31 }; mini.model.transform.SetParent(transform, false); mini.model.transform.position = MiniFocus(index);
                VehicleVisual.Build(mini.model.transform, p);
                foreach (var t in mini.model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
                var shown = mini.model.GetComponentsInChildren<Renderer>().Where(rr => rr.enabled && !(rr is ParticleSystemRenderer)).ToArray();
                if (shown.Length > 0) { var b = shown[0].bounds; foreach (var rr in shown) b.Encapsulate(rr.bounds); mini.centre = b.center - mini.model.transform.position; mini.radius = Mathf.Max(.3f, b.extents.magnitude); }
                mini.key = p.Id;
            }
            if (!mini.rt || mini.rt.width != width || mini.rt.height != height)
            {
                if (mini.rt) { mini.cam.targetTexture = null; mini.rt.Release(); Destroy(mini.rt); }
                mini.rt = new RenderTexture(width, height, 16) { antiAliasing = 4, name = "Small vehicle preview" }; mini.cam.targetTexture = mini.rt;
            }
            mini.cam.aspect = width / (float)height;
            var raw = new GameObject("Vehicle preview", typeof(RectTransform)).AddComponent<UnityEngine.UI.RawImage>(); raw.transform.SetParent(parent, false);
            raw.texture = mini.rt; raw.raycastTarget = false; mini.raw = raw; return raw;
        }
        void UpdateMinis()
        {
            bool page = open && modal == null && this.page == "high-vehicle";
            for (int i = 0; i < minis.Count; i++)
            {
                var m = minis[i]; bool on = page && i < minisUsed && m.model;
                if (m.cam.enabled != on) m.cam.enabled = on; if (m.model && m.model.activeSelf != on) m.model.SetActive(on); if (!on) continue;
                m.yaw = Mathf.Repeat(m.yaw + 16 * Time.unscaledDeltaTime, 360);
                var turn = Quaternion.Euler(0, m.yaw, 0); var focus = MiniFocus(i);
                m.model.transform.SetPositionAndRotation(focus - turn * m.centre, turn);
                float half = m.cam.fieldOfView * .5f * Mathf.Deg2Rad, fit = Mathf.Min(half, Mathf.Atan(Mathf.Tan(half) * m.cam.aspect)), distance = m.radius * 1.04f / Mathf.Sin(fit);
                var look = Quaternion.Euler(11, 0, 0); m.cam.transform.SetPositionAndRotation(focus - look * Vector3.forward * distance, look);
                m.cam.nearClipPlane = .05f; m.cam.farClipPlane = distance + m.radius * 2 + 5;
            }
        }

        // ---------- TIME & WEATHER ----------
        void RenderHighTime()
        {
            Clear("TIME & WEATHER", "For Free Roam and races in HIGH MODE. Left / right changes a row.");
            var s = GameSettings.Settings;
            void Save() { if (!CheckRun.Active) GameSettings.SaveSettings(); Show(); }
            Step(0, "high-time-of-day", "Time of day:   " + HighMode.TimeNames[HighMode.TimeChoice], d => { s.highTime = (HighMode.TimeChoice + d + 5) % 5; Save(); });
            Step(1, "high-weather", "Weather:   " + HighMode.WeatherNames[HighMode.WeatherChoice], d => { s.highWeather = (HighMode.WeatherChoice + d + 4) % 4; if (s.highWeather == 3) HighMode.Roll(); Save(); });
            Row(2, "back", "Back", BackPage);
        }
    }
}
