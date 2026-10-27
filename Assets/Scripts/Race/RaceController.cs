using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 5: the race, as little as it takes. One route, one car, a clock. Started with a key (T; D-pad up on a pad), so free
    // driving is the default and stays untouched: nothing here runs unless a race was started, apart from drawing the
    // start/finish gate and a one-line hint. Racer's RaceDirector / RaceFlow / Campaign are NOT imported; the rules below are
    // the ones Dan asked for, with Racer's rolling start (the clock starts when the car first crosses the start line, so a
    // lap is gate to gate and the 3-2-1 grid run-in is not part of any lap).
    //
    //   Round 16: started, restarted and left from the menus (Menus/GameMenus.cs: RACE > START RACE; the pause menu's RESTART /
    //   LEAVE RACE); the T / X keys and D-pad up / down are gone. R / Y still resets the car to its last stable spot.
    //
    // Rules (Round 6, as in Racer / Woodstock Rush): 3 laps; checkpoints are met in order; a checkpoint that is skipped costs a
    // 5 s time penalty (Racer's RaceProgress.Miss) and the lap STILL COUNTS, with the penalty added to the lap and to the total;
    // taking an official shortcut (RaceRoute.Shortcuts, shaped after Racer's WoodlandRoute) forgives the checkpoints it skips;
    // while a race runs, every road that leaves the course and is not part of a shortcut is closed by a barrier (RaceBarriers).
    // Times use the physics clock (Time.fixedDeltaTime), so a bug-capture pause (Time.timeScale 0) pauses the race too, and
    // crossings are interpolated inside the physics step. Gates are wide and forgiving: see RaceGate.Crossed.
    //
    // Round 15 (items 6 to 13): three AI drivers race too (Racer's RoadDriver, see RoadDriver.cs), under exactly these rules:
    // every car is a RaceEntrant with its own gates, laps, penalties and shortcuts. They get a vehicle each (random from the
    // eleven, or fixed in ai_drivers.json beside the race records) and Racer's quick-race names and paints (EMBER, GOLD, BLUE).
    // The grid is Racer's: the player at the back on the old grid spot, the three AI ahead of it at 8, 15 and 22 m, either
    // side of the lane, all held until GO. The race clock runs from GO; each racer's own clock starts at its first crossing
    // of the start line (the rolling start above). The finishing order is who crossed the line first, penalties added; if
    // an AI driver is still racing 60 s after the player has finished, its finish is estimated from its pace (Racer's
    // AiFinishEstimate) and marked as such.
    [DefaultExecutionOrder(50)]
    public sealed partial class RaceController : MonoBehaviour
    {
        public enum Stage { Free, Countdown, Racing, Finished }

        public static RaceController Instance { get; private set; }
        public static string DirectionOverride;   // command line -raceDirection, or set by a check; otherwise the route file decides
        public static string RecordsPathOverride; // checks write somewhere other than the real records
        public static int LapsOverride;           // race this many laps instead of the route file's (checks; Round 16: the race setup menu)
        public static readonly List<RouteShortcutJson> ExtraShortcuts = new List<RouteShortcutJson>(); // checks only: shortcuts not in the file
        public static bool EntryPlanesOff;   // checks only (Round 13): shortcuts are taken by their corridor alone
        public const float MissPenaltySeconds = 5f;   // Racer: RaceProgress.Miss forces 5 s per missed gate

        [Header("Set by MapBuilder (Chastain Rush > Build Map)")]
        [TextArea] public string embeddedRouteJson;   // the route file as it was when the scene was built
        public string routeFileName = "chastain_glen_loop.json";
        // Round 18 (item 5): every route of the one world (Data/routes/*.json), the Chastain Glen loop first; RouteOverride (a
        // file name, the race setup menu's Route row or a check's -route) picks one, the first otherwise
        public string[] routeFileNames;
        [TextArea] public string[] embeddedRouteJsons;
        public static string RouteOverride;
        public Material gateBlack, gateWhite, nextMaterial, laterMaterial, missedMaterial, shortcutMaterial;

        public Stage State { get; private set; } = Stage.Free;
        public RaceRoute Route { get; private set; }
        public RouteJson RouteData { get; private set; }
        // the player's race (Round 15: these read the player's RaceEntrant; the AI drivers have their own, in Entrants)
        public float RaceClock { get; private set; }    // Round 15: seconds since GO, the same for every racer
        public ArcadeVehicle PlayerCar => car;           // Round 43 (B1: the ghost)
        // Round 43 (Part D): Racer's rule: a race run in Trailer Mode does not count for records (no best times, Top 10 or ghost)
        public bool NoRecords { get; private set; }
        public void MarkNoRecords(string why) { if (NoRecords || State == Stage.Free) return; NoRecords = true; Log($"{why}: this race does not count for records"); }
        public CleanLapGhost Ghost { get; private set; } // Round 43 (B1): Racer's clean-lap ghost of the player's best lap
        public float Clock => P.ClockStarted ? (P.Finished ? P.FinalTotal - P.PenaltyTotal : RaceClock - P.StartAt) : 0f;   // seconds since the player first crossed the start line
        public float LapClock => P.ClockStarted && State != Stage.Free ? RaceClock - P.LapStart + P.LapPenalty : 0f;   // with this lap's penalties
        public float TotalClock => Clock + PenaltyTotal;                                                    // with all penalties
        public float PenaltyTotal => P.PenaltyTotal;     // seconds of penalty so far this race
        public float LapPenalty => P.LapPenalty;         // ... of which on the lap in progress
        public int MissedTotal => P.MissedTotal;         // checkpoints missed so far this race
        public IReadOnlyList<string> PenaltyLog => P.PenaltyLog;
        public RaceShortcut ActiveShortcut => P.ActiveShortcut;
        public IReadOnlyList<int> ForgivenThisLap => P.Forgiven;
        public RaceBarriers Barriers => barriers;
        public int CompletedLaps => P.CompletedLaps;
        public int NextCheckpoint => P.NextCheckpoint; // index into Route.Checkpoints; == Count once all are passed
        public bool ClockStarted => P.ClockStarted;
        public float CountdownRemaining { get; private set; }
        public float FinalTotal => P.FinalTotal;
        public string Message { get; private set; } = "";
        public IReadOnlyList<float> LapTimes => P.LapTimes;
        public IReadOnlyList<string> LapLog => P.LapLog;
        public IReadOnlyList<int> MissedThisLap => P.Missed;
        // Round 15: every racer (the player first), the AI drivers' road and shortcuts, and whether the AI drive now
        public readonly List<RaceEntrant> Entrants = new List<RaceEntrant>();
        public RaceEntrant Player => P;
        public RaceRoad AiRoad { get; private set; }
        public readonly List<AiShortcut> AiShortcuts = new List<AiShortcut>();
        // Round 21: the AI's held speeds at the jumps for the direction being raced (Noonday Creek reverse has its own jumps)
        public RouteAiZoneJson[] AiZones => RouteData == null ? null : Route != null && Route.Reverse && RouteData.ai_zones_reverse != null && RouteData.ai_zones_reverse.Length > 0 ? RouteData.ai_zones_reverse : Route != null && Route.Reverse ? null : RouteData.ai_zones;
        public bool AiRunning => (State == Stage.Racing || State == Stage.Finished) && aiActive;
        public static int AiCount = 3;                 // Dan: "I think we can start with 3 ai drivers"; checks may change it (-aiCount)
        public static string[] AiVehiclesOverride;     // checks: -aiVehicles id,id,id (otherwise ai_drivers.json, otherwise random)
        public static float PlayerFinishWait = 60f;    // after the player finishes, how long the AI drivers race on before their finishes are estimated
        public float PlayerFinishedAt { get; private set; }
        public float BestLap { get; private set; }
        public float BestTotal { get; private set; }
        public bool NewBestLap { get; private set; }
        public bool NewBestTotal { get; private set; }
        public int LapsInRace => Route != null ? Route.Laps : 3;
        public string Hint => "Start / Esc: menu";   // Round 16: the race keys are the menus now
        public event Action<string> Logged; // every race event, for the scripted check

        ArcadeVehicle car;
        VehicleInput input;
        VehicleRespawn respawn;
        bool wasInputEnabled, aiActive;
        float messageUntil, goFlashUntil, nextEstimateSample;
        internal readonly RaceEntrant P = new RaceEntrant { Name = "YOU", Seat = 1 };   // (Round 73: internal, for the air race's markers and minimap)
        RaceMarkers markers;
        RaceBarriers barriers;
        BoxCollider carBox;
        Texture2D arrow;
        GUIStyle small, big, huge, panel;
        Texture2D back;

        [Serializable] class RecordEntry { public string key; public float best_lap, best_total; }
        [Serializable] class RecordFile { public List<RecordEntry> entries = new List<RecordEntry>(); }
        RecordFile records = new RecordFile();

        // Round 34 (C1, C2): Racer's Top 10 boards (Race/RecordBoards.cs) beside race_records.json (a check's own folder in a check),
        // opened once; the player's every lap and finished race go on them (Racer's RaceFlow.LapCompleted; the AI's never do)
        static RecordBoards boards; static string boardsDir;
        public static RecordBoards Boards
        {
            get
            {
                string dir = Path.GetDirectoryName(RecordsPath());
                if (boards == null || boardsDir != dir) { boardsDir = dir; boards = new RecordBoards(dir, RecordsPath()); }
                return boards;
            }
        }
        public string BoardCategory => Route != null ? RecordBoards.Category(Route.RecordName, RecordDirection, Route.Laps) : "";
        // Round 35 (Part C): Racer's WeatherGrip.RecordTag: a race in rain or snow (with the grip setting On) keeps its times apart ("forward-rain")
        string RecordDirection => Route.Direction + Racer.WeatherGrip.RecordTag(Racer.WeatherGrip.ChosenFor());
        public string RecordDirectionTag => RecordDirection;   // Round 57 (F): a replay's direction as the records key it
        public readonly FinishPresentation FinishCards = new FinishPresentation();
        string attempt = ""; readonly List<int> lapRanks = new List<int>();
        public int LastLapRank { get; private set; }
        public int RaceRank { get; private set; }
        // every route and the directions it is raced in, for the Records page
        public static readonly Dictionary<string, string> RecordTitles = new Dictionary<string, string>();   // Round 54 (A1): records name -> route name
        public static string RecordTitle(string recordName) => RecordTitles.TryGetValue(recordName, out var t) ? t : recordName;
        public List<(string route, string direction)> RecordTracks()
        {
            var list = new List<(string, string)>();
            for (int i = 0; i < (routeFileNames?.Length ?? 0); i++)
            {
                try
                {
                    var j = JsonUtility.FromJson<RouteJson>(embeddedRouteJsons[i]); if (j == null || string.IsNullOrEmpty(j.name)) continue;
                    foreach (var d in new[] { "forward", "reverse" }) if (j.Allows(d) && (d == "forward" || j.directions != null && j.directions.Length > 0 || j.reverse_grid_back_m > 0f))
                    {
                        string rn = string.IsNullOrEmpty(j.records_as) ? j.name : j.records_as; RecordTitles[rn] = j.name;   // Round 54 (A1): kept under records_as, shown by name
                        list.Add((rn, d));
                        // Round 35 (Part C): a wet or snowy board, once it has a time
                        foreach (var w in new[] { "-rain", "-snow" }) { string key = rn + "|" + d + w; if (Boards != null && (Boards.Top(c => RecordBoards.LapCategory(c) == key, false).Count > 0 || Boards.Top(c => RecordBoards.LapCategory(c) == key, true).Count > 0)) list.Add((rn, d + w)); }
                    }
                }
                catch (Exception) { }
            }
            return list;
        }
        string VehicleId => car && car.GetComponent<VehicleConfiguration>() ? car.GetComponent<VehicleConfiguration>().profileId : "original";

        // ---------------------------------------------------------------- setup

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            NoondayTrailSwap.Run(Path.GetDirectoryName(RecordsPath()));   // Round 64 (A): once, the Noonday Creek Trail's saved directions swapped
            car = FindAnyObjectByType<ArcadeVehicle>();
            if (!car) { enabled = false; return; }
            input = car.GetComponent<VehicleInput>();
            respawn = car.GetComponent<VehicleRespawn>();
            carBox = car.GetComponentInChildren<BoxCollider>();
            P.Car = car; P.Box = carBox;
            if (respawn) respawn.Respawned += OnRespawned;
            Ghost = gameObject.AddComponent<CleanLapGhost>(); Ghost.Initialize(this, Path.GetDirectoryName(RecordsPath()));   // Round 43 (B1)
            gameObject.AddComponent<ReplayRecorder>();   // Round 57 (F): every race recorded
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-raceDirection") DirectionOverride = args[i + 1];
                if (args[i] == "-aiCount") AiCount = Mathf.Clamp(int.Parse(args[i + 1]), 0, 3);
                if (args[i] == "-aiVehicles") AiVehiclesOverride = args[i + 1].Split(',');
                if (args[i] == "-route") RouteOverride = args[i + 1].EndsWith(".json") ? args[i + 1] : args[i + 1] + ".json";   // Round 18
                if (args[i] == "-routeDir" && CheckRun.Active) UseRouteDir(args[i + 1]);   // Round 71 (checks): route files read from a folder, over the embedded copies
            }
            LoadRoute();
        }

        // Round 71 (checks only): every *.json in dir replaces the embedded copy of that name, or is added (a route not yet built into the scene)
        void UseRouteDir(string dir)
        {
            if (!Directory.Exists(dir)) return;
            var names = new List<string>(routeFileNames ?? new string[0]); var jsons = new List<string>(embeddedRouteJsons ?? new string[0]);
            foreach (var f in Directory.GetFiles(dir, "*.json"))
            {
                string n = Path.GetFileName(f); int k = names.IndexOf(n);
                if (k >= 0) jsons[k] = File.ReadAllText(f); else { names.Add(n); jsons.Add(File.ReadAllText(f)); }
                Debug.Log("[Race] route file from " + f);
            }
            routeFileNames = names.ToArray(); embeddedRouteJsons = jsons.ToArray();
        }

        // In the Editor the route file is read from Data/routes each time (so changing "direction" there needs no rebuild);
        // anywhere else, the copy MapBuilder embedded in the scene.
        // Round 18: the routes there are, as (file name, display name), the Chastain Glen loop first
        public List<(string file, string name)> Routes()
        {
            var list = new List<(string, string)>();
            if (routeFileNames == null || routeFileNames.Length == 0) { list.Add((routeFileName, RouteData != null && routeFileName == CurrentFile ? RouteData.name : routeFileName)); return list; }
            for (int i = 0; i < routeFileNames.Length; i++)
            {
                string name = routeFileNames[i];
                try { var j = JsonUtility.FromJson<RouteJson>(embeddedRouteJsons[i]); if (!string.IsNullOrEmpty(j.name)) name = j.name; } catch (Exception) { }
                list.Add((routeFileNames[i], name));
            }
            return list;
        }
        public string CurrentFile { get; private set; }
        string currentJson;
        // Round 57 (F): the embedded route file's text (the replay keeps its hash: a course changed since is said on the REPLAYS page), and the course
        // made from it (the race's barriers round a replay watched from the page)
        public string RouteJsonOf(string file)
        {
            if (routeFileNames == null || routeFileNames.Length == 0) return file == routeFileName ? embeddedRouteJson : null;
            int k = Array.IndexOf(routeFileNames, file); return k >= 0 ? embeddedRouteJsons[k] : null;
        }
        public string RouteHashOf(string file) { var j = RouteJsonOf(file); return j == null ? "" : ReplayData.Hash(j); }
        public RaceRoute RouteFor(string file, string direction)
        {
            var j = RouteJsonOf(file); if (string.IsNullOrEmpty(j)) return null;
            try { var d = JsonUtility.FromJson<RouteJson>(j); return RaceRoute.Create(d, d.Allows(direction) ? direction : d.direction); } catch (Exception) { return null; }
        }

        void LoadRoute()
        {
            string file = routeFileName, json = embeddedRouteJson;
            if (routeFileNames != null && routeFileNames.Length > 0)
            {
                int k = Mathf.Max(0, Array.IndexOf(routeFileNames, RouteOverride));
                file = routeFileNames[k]; json = embeddedRouteJsons[k];
            }
            CurrentFile = file; currentJson = json;
#if UNITY_EDITOR
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Data", "routes", file));
            if (File.Exists(path)) json = File.ReadAllText(path);
#endif
            if (string.IsNullOrEmpty(json)) { Debug.LogWarning("[Race] no route data; racing is off"); enabled = false; return; }
            RouteData = JsonUtility.FromJson<RouteJson>(json);
            if (ExtraShortcuts.Count > 0)
            {
                var all = new List<RouteShortcutJson>(RouteData.official_shortcuts ?? new RouteShortcutJson[0]);
                all.AddRange(ExtraShortcuts);
                RouteData.official_shortcuts = all.ToArray();
            }
            string dir = DirectionOverride;
            if (!string.IsNullOrEmpty(dir) && !RouteData.Allows(dir)) dir = RouteData.direction;   // Round 18: Noonday Creek is forward only (its jumps face one way)
            Route = RaceRoute.Create(RouteData, dir);
            if (LapsOverride > 0) Route.Laps = LapsOverride;
            LoadRecords();
            if (markers == null) markers = new GameObject("RaceMarkers").AddComponent<RaceMarkers>();
            if (!markers.GetComponent<AirGuideArrow>()) markers.gameObject.AddComponent<AirGuideArrow>();   // Round 73 (B2): the air race's 3D arrow
            markers.Build(this);
            markers.Refresh(this);
        }

        // ---------------------------------------------------------------- control

        public void StartRace()
        {
            if (!enabled || !car) return;
            LoadRoute(); // picks up a changed direction
            RaceRoute.Active = Route;   // Round 27: the underground race's heights for the grid, the AI and the resets (RaceRoute.Surface)
            EnsureAllowedVehicle();   // Round 18: Noonday Creek is for the small vehicles only
            RaceClock = 0f; PlayerFinishedAt = 0f; winnerCelebrated = false; parkSlots = 0; NoRecords = false;
            NewBestLap = NewBestTotal = false;
            attempt = Guid.NewGuid().ToString("N"); lapRanks.Clear(); LastLapRank = RaceRank = 0;   // Round 34 (C2): Racer's RaceFlow at the countdown
            Boards.BeginAttempt(); FinishCards.Begin(Boards, BoardCategory, BestLap, BestTotal);
            if (Ghost) Ghost.ResetSession();   // Round 43 (B1)
            if (State == Stage.Free) wasInputEnabled = input.enabled;
            State = Stage.Countdown; CountdownRemaining = 3f; attempt2 = Guid.NewGuid().ToString("N");
            RemoveAi();
            PlaceOnGrid();
            P.Reset(); P.Name = SplitScreen.Active ? SplitScreen.NameOf(1) : "YOU";   // Round 37: in split screen each player by name
            Entrants.Clear(); Entrants.Add(P);
            SpawnAi();   // Round 15: the AI drivers on the grid ahead of the player
            SeatPlayerTwo();   // Round 37: split screen's player 2
            input.enabled = false;
            Say("Get ready", 2f);
            if (barriers == null) barriers = new GameObject("RaceBarriers").AddComponent<RaceBarriers>();
            barriers.Build(Route);   // closed roads appear with the race ...
            FenceChunk.RestoreAll(); // Round 7: smashed fences are whole again when a race (re)starts
            BuildAiLine();   // Round 70 (B): the AI's racing line, once the barriers stand
            Log($"RACE START requested: {Route.Name}, {Route.Direction}, {Route.Laps} laps, lap {Route.LapLength:0} m, {Route.Checkpoints.Count} checkpoints, run-in {Route.RunInLength:0} m, {barriers.Count} road barriers, {Route.Shortcuts.Count} official shortcuts"
                + (barriers.Seals + barriers.Rails + barriers.Gates.Count > 0 ? $", {barriers.Seals} seals, {barriers.Rails} railings, {barriers.Gates.Count} switch gates" : ""));
            markers.Refresh(this);
            GetComponent<ReplayRecorder>()?.Begin(CurrentFile, currentJson, BoardCategory);   // Round 57 (F)
        }

        // Round 18: the race setup menu changed the route (free roam only): load it so the menu shows its name and length
        public void Reload() { if (State == Stage.Free && enabled) LoadRoute(); }

        public void EndRace()
        {
            if (State == Stage.Free) return;
            State = Stage.Free;
            input.enabled = true;
            Say("", 0f);
            if (barriers != null) barriers.Clear();   // ... and go away in free driving
            RaceRoute.Active = null;   // Round 27
            P.ActiveShortcut = null;
            RemoveAi();
            Log("RACE LEFT");
            markers.Refresh(this);
        }

        // Round 18: on a route with a vehicle list (Noonday Creek: motorcycles, the ATV, the mower), a player in anything else is
        // put in the first allowed vehicle (the race setup menu offers only those there; this covers a check or a saved choice)
        public bool PlayerVehicleAllowed { get { var cfg = car ? car.GetComponent<VehicleConfiguration>() : null; return RouteData == null || cfg == null || RouteData.AllowsVehicle(cfg.profileId); } }
        void EnsureAllowedVehicle()
        {
            if (PlayerVehicleAllowed) return;
            // Round 36: a stunt vehicle in a race goes back to the Street Classic (Racer's EligibleVehicle), or the trail's first vehicle
            var sw = car.GetComponent<VehicleSwitch>(); string id = RouteData.vehicles != null && RouteData.vehicles.Length > 0 ? RouteData.vehicles[0] : "original";
            if (sw && RouteData.AllowsVehicle(sw.RaceVehicle)) id = sw.RaceVehicle;   // Round 70 (D): the last ground vehicle chosen for races
            if (sw) sw.Select(id, VehicleSwitch.Pick.Forced); else car.GetComponent<VehicleConfiguration>()?.Apply(id);
            Log($"vehicle not allowed on {RouteData.name}: switched to {id}");
        }

        void PlaceOnGrid()
        {
            Vector3 g = Route.GridPosition;
            var plane = car.GetComponent<PlaneFlight>();
            if (plane && plane.enabled) { plane.Recovered(); g += Vector3.up * (PlaneFlight.WheelDrop - .2f); }   // Round 71: the plane stands on its wheels on the runway
            if (respawn) respawn.PlaceAt(g + Vector3.up * 0.3f, Route.GridYaw);
            else { car.Body.position = g + Vector3.up * 0.8f; car.Body.rotation = Quaternion.Euler(0, Route.GridYaw, 0); car.Body.linearVelocity = Vector3.zero; }
            P.PrevPos = car.transform.position;
        }

        void OnRespawned() { P.PrevPos = car.transform.position; } // a reset is a teleport: it must not count as crossing anything
        public void NotifyTeleported() { P.PrevPos = car.transform.position; if (Ghost) Ghost.Invalidate(); } // checks that move the car by hand say so, as a reset does (Round 43: and no ghost from that lap)

        // How far the car's body reaches sideways across a gate at the moment of crossing (half the car's box, turned to the gate).
        static float Reach(RaceEntrant e, RaceGate g)
        {
            var t = e.Car.transform; var box = e.Box;
            Vector3 right = g.Right, cr = t.right, cf = t.forward;
            float hx = box ? box.size.x * 0.5f * box.transform.lossyScale.x : 0.925f, hz = box ? box.size.z * 0.5f * box.transform.lossyScale.z : 1.85f;
            return Mathf.Abs(Vector3.Dot(right, cr)) * hx + Mathf.Abs(Vector3.Dot(right, cf)) * hz;
        }

        // ---------------------------------------------------------------- per frame

        void FixedUpdate()
        {
            if (State == Stage.Free || !car) return;
            float dt = Time.fixedDeltaTime;

            if (State == Stage.Countdown)
            {
                car.Body.linearVelocity = Vector3.zero; car.Body.angularVelocity = Vector3.zero;
                foreach (var h in Entrants) if (h.Seat == 2 && h.Driver == null && h.Car) { h.Car.Body.linearVelocity = Vector3.zero; h.Car.Body.angularVelocity = Vector3.zero; }   // Round 37
                if (!LoadingScreen.Holding) CountdownRemaining -= dt;   // Round 51: the countdown starts once the loading screen has gone
                if (CountdownRemaining <= 0f)
                {
                    State = Stage.Racing; input.enabled = true; goFlashUntil = Time.time + 1.0f; RaceClock = 0f;
                    Say("Cross the start line to start the clock", 6f);
                    Log("GO");
                    markers.Refresh(this);
                }
                foreach (var e in Entrants) if (e.Car) e.PrevPos = e.Car.transform.position;
                return;
            }

            float t0 = RaceClock;
            RaceClock += dt;
            foreach (var e in Entrants)
            {
                if (!e.Car) continue;
                if (e.Finished) { e.PrevPos = e.Car.transform.position; continue; }
                Step(e, t0, dt);
                // Round 27 (the underground race): a racer over a switch's plate opens its gate for everyone, for the rest of the race
                if (barriers != null && barriers.Gates.Count > 0 && barriers.TryOpen(e.Car.Body.position, e.Name, out var opened))
                {
                    Say($"{opened.Data.label} OPEN", 3f);
                    Log($"switch {opened.Data.id}: {opened.Data.label} opened by {e.Name} at {RaceClock:0.0} s");
                }
            }
            if (aiActive) TrackAi();
        }

        // the gates one racer passed during this step, in the order they happened
        void Step(RaceEntrant e, float t0, float dt)
        {
            Vector3 pos = e.Car.transform.position, prevPos = e.PrevPos;
            var hits = new List<(float f, int id)>();
            if (Route.Gate.Crossed(prevPos, pos, Reach(e, Route.Gate), out float fg)) hits.Add((fg, 0));
            else if (e.ClockStarted && e.NextCheckpoint >= Route.Checkpoints.Count && LineMissedWide(e, prevPos, pos, out float fw)) hits.Add((fw, 4000));   // Round 54 (B)
            for (int i = 0; i < Route.Checkpoints.Count; i++)
                if (Route.Checkpoints[i].Crossed(prevPos, pos, Reach(e, Route.Checkpoints[i]), out float fc)) hits.Add((fc, i + 1));
            if (Route.Air && e.IsHuman) AirRingPassedBy(e, prevPos, pos);   // Round 73 (B2 / B3): RING MISSED
            for (int i = 0; i < Route.Shortcuts.Count; i++)
            {
                var sc = Route.Shortcuts[i];
                if (!EntryPlanesOff && sc.Entry.Crossed(prevPos, pos, Reach(e, sc.Entry), out float fe)) hits.Add((fe, 1000 + i));
                if (e.ActiveShortcut == sc && sc.Exit.Crossed(prevPos, pos, Reach(e, sc.Exit), out float fx)) hits.Add((fx, 2000 + i));
                // Round 13: in the shortcut's corridor counts as taking it too (the entry plane can be passed round)
                else if (e.ActiveShortcut != sc && e.ClockStarted && sc.LastForgiven >= e.NextCheckpoint && sc.InCorridor(pos)) hits.Add((1f, 3000 + i));
            }
            // Round 34 (A6): whether this lap has really been driven: the racer has been on the far half of the loop (a quarter to three
            // quarters of a lap past the line), sampled twice a second until it has, or has passed a checkpoint
            if (e.ClockStarted && !e.LapAway && t0 >= e.NextAwayCheck)
            {
                e.NextAwayCheck = t0 + 0.5f;
                float st = Route.LapStationOf(pos), L = Route.LapLength;
                if (st > 0.25f * L && st < 0.75f * L) e.LapAway = true;
            }
            hits.Sort((a, b) => a.f.CompareTo(b.f));
            foreach (var h in hits)
            {
                float at = t0 + h.f * dt;
                crossing = Vector3.Lerp(prevPos, pos, h.f);   // Round 43 (B1): where the line was crossed, for the ghost
                if (h.id == 0) OnGate(e, at);
                else if (h.id == 4000) OnLineMissed(e, at);
                else if (h.id < 1000) OnCheckpoint(e, h.id - 1, at);
                else if (h.id < 2000) OnShortcutEntry(e, Route.Shortcuts[h.id - 1000], at, "its entry plane");
                else if (h.id < 3000) OnShortcutExit(e, Route.Shortcuts[h.id - 2000], at);
                else OnShortcutEntry(e, Route.Shortcuts[h.id - 3000], at, "its corridor");
                if (e.Finished) break;
            }
            e.PrevPos = pos;
        }

        // ---------------------------------------------------------------- rules

        // A checkpoint that was skipped: Racer's rule, 5 s on the lap and on the total, and the lap goes on counting.
        void Miss(RaceEntrant e, int index, float at)
        {
            e.Missed.Add(index);
            e.PenaltyTotal += MissPenaltySeconds; e.LapPenalty += MissPenaltySeconds; e.MissedTotal++;
            e.PenaltyLog.Add($"Lap {e.CompletedLaps + 1}  CP{index + 1}  +{MissPenaltySeconds:0} s  missed");
            Log(e, $"PENALTY +{MissPenaltySeconds:0} s: missed CP{index + 1} on lap {e.CompletedLaps + 1} at {at:0.000} (race penalty now {e.PenaltyTotal:0} s)");
        }

        void MissMessage(RaceEntrant e, List<int> skipped)
        {
            if (skipped.Count == 0 || !e.IsHuman) return;
            SayTo(e, $"Missed checkpoint{(skipped.Count > 1 ? "s" : "")} {string.Join(", ", skipped)}  +{skipped.Count * MissPenaltySeconds:0} s", 4f);
        }

        void OnCheckpoint(RaceEntrant e, int index, float at)
        {
            if (!e.ClockStarted) return;       // before the first start-line crossing there is no lap to be on
            if (index < e.NextCheckpoint) return; // already passed (or already missed / forgiven) this lap
            var skipped = new List<int>();
            for (int k = e.NextCheckpoint; k < index; k++) { Miss(e, k, at); skipped.Add(k + 1); }
            e.NextCheckpoint = index + 1; e.RingSkipped = -1;
            if (Route.Air && e.IsPlayer) markers.RingPassed(Route.Checkpoints[index]);   // Round 71: the ring's flash and chime
            Log(e, $"CHECKPOINT {index + 1} at {at:0.000}" + (skipped.Count > 0 ? $"; MISSED {string.Join(",", skipped)}" : ""));
            MissMessage(e, skipped);
            if (e.IsPlayer) markers.Refresh(this);
        }

        // Taking an official shortcut (Racer: a Branch entry grants the route's bypass list): the checkpoints it skips are
        // forgiven, with no penalty. Checkpoints before the first forgiven one that are still waiting were really missed.
        void OnShortcutEntry(RaceEntrant e, RaceShortcut sc, float at, string how)
        {
            if (!e.ClockStarted) return;
            if (sc.LastForgiven < e.NextCheckpoint) return;   // nothing left on this lap for it to forgive
            var skipped = new List<int>();
            for (int k = e.NextCheckpoint; k < sc.FirstForgiven; k++) { Miss(e, k, at); skipped.Add(k + 1); }
            var free = new List<int>();
            for (int k = Mathf.Max(e.NextCheckpoint, sc.FirstForgiven); k <= sc.LastForgiven; k++) { e.Forgiven.Add(k); free.Add(k + 1); }
            e.NextCheckpoint = sc.LastForgiven + 1;
            e.ActiveShortcut = sc;
            e.ShortcutsTaken.Add($"lap {e.CompletedLaps + 1} {sc.Id}");
            Log(e, $"SHORTCUT {sc.Id} entered at {at:0.000} ({how}): checkpoints {string.Join(",", free)} forgiven" + (skipped.Count > 0 ? $"; MISSED {string.Join(",", skipped)}" : ""));
            if (skipped.Count > 0) MissMessage(e, skipped);
            else if (e.IsHuman) SayTo(e, $"Shortcut: {sc.Name}  (checkpoint{(free.Count > 1 ? "s" : "")} {string.Join(", ", free)} skipped, no penalty)", 4f);
            if (e.IsPlayer) markers.Refresh(this);
        }

        // Round 43 (A1 d, Dan BUG-001: the reset "say you miss 3 checkpoints"): a race reset that puts the vehicle further on along the lap
        // than the lap point it was nearest to forgives the checkpoints in between (as a shortcut's are); one that puts it back forgives
        // nothing. Stations are the lap's own (from the line). Checkpoints still waiting before the first forgiven one were really missed.
        public void ForgiveReset(ArcadeVehicle car, float from, float to)
        {
            if (State != Stage.Racing || Route == null || !car) return;
            var e = Entrants.Find(x => x.Car == car); if (e == null || !e.ClockStarted || e.Finished) return;
            float L = Route.LapLength, span = Mathf.Repeat(to - from, L);
            if (span <= 0.01f || span > 0.5f * L) return;
            int first = -1, last = -1;
            for (int k = e.NextCheckpoint; k < Route.Checkpoints.Count; k++)
            {
                float d = Mathf.Repeat(Route.Checkpoints[k].S - from, L);
                if (d <= span) { if (first < 0) first = k; last = k; }
            }
            if (first < 0) return;
            var skipped = new List<int>(); var free = new List<int>();
            for (int k = e.NextCheckpoint; k < first; k++) { Miss(e, k, RaceClock); skipped.Add(k + 1); }
            for (int k = first; k <= last; k++) { e.Forgiven.Add(k); free.Add(k + 1); }
            e.NextCheckpoint = last + 1;
            Log(e, $"RESET at {RaceClock:0.000}: placed on from lap {from:0} m to {to:0} m; checkpoints {string.Join(",", free)} forgiven" + (skipped.Count > 0 ? $"; MISSED {string.Join(",", skipped)}" : ""));
            MissMessage(e, skipped);
            if (e.IsPlayer) markers.Refresh(this);
        }

        // Round 54 (B, Dan on Noonday Creek: missed the finish line on lap 2 and "it didnt advance my lap"): the line missed is a missed checkpoint
        // like any other. A racer waiting for the line (every checkpoint of the lap met or forgiven) who goes past it outside its arch, within
        // LineMissReach of it, on the lap's last or first stretch, has its lap counted there with the 5 s penalty; on the last lap that ends its race
        // there. A racer who never comes near the line (a cut through the yards) still has no lap until it crosses it, as before.
        public const float LineMissReach = 40f;
        bool LineMissedWide(RaceEntrant e, Vector3 from, Vector3 to, out float fraction)
        {
            fraction = 0f; var g = Route.Gate;
            Vector3 a = from - g.Position, b = to - g.Position;
            float za = Vector3.Dot(a, g.Forward), zb = Vector3.Dot(b, g.Forward);
            if (!(za < 0f && zb >= 0f)) return false;
            fraction = -za / (zb - za); var hit = Vector3.Lerp(a, b, fraction);
            float side = Mathf.Abs(Vector3.Dot(hit, g.Right));
            if (side > LineMissReach || hit.y < -g.Below - 10f || hit.y > g.Above + 10f) return false;
            float L = Route.LapLength, d = Route.LapStationOf(Vector3.Lerp(from, to, fraction));   // from the line, in travel order
            return d < 0.1f * L || d > 0.75f * L;   // near the line along the lap (not another stretch that passes its plane)
        }
        void OnLineMissed(RaceEntrant e, float at)
        {
            int n = Route.Checkpoints.Count;
            e.Missed.Add(n); e.PenaltyTotal += MissPenaltySeconds; e.LapPenalty += MissPenaltySeconds; e.MissedTotal++;
            bool last = e.CompletedLaps + 1 >= Route.Laps;
            e.PenaltyLog.Add($"Lap {e.CompletedLaps + 1}  {(last ? "finish" : "start / finish")} line  +{MissPenaltySeconds:0} s  missed");
            Log(e, $"PENALTY +{MissPenaltySeconds:0} s: went past the {(last ? "finish" : "start / finish")} line outside it on lap {e.CompletedLaps + 1} at {at:0.000} (race penalty now {e.PenaltyTotal:0} s)");
            CountLap(e, at, true);
        }

        void OnShortcutExit(RaceEntrant e, RaceShortcut sc, float at)
        {
            if (e.ActiveShortcut != sc) return;
            e.ActiveShortcut = null;
            Log(e, $"SHORTCUT {sc.Id} left at {at:0.000}");
        }

        Vector3 crossing;
        void OnGate(RaceEntrant e, float at)
        {
            if (Route.Air && e.IsPlayer) markers.RingPassed(Route.Gate);   // Round 71
            if (!e.ClockStarted)
            {
                if (e.IsPlayer && Ghost) Ghost.Boundary(at, crossing, e.Car.Body.rotation, false, false, true);   // Round 43 (B1): the ghost's recording starts
                e.ClockStarted = true; e.StartAt = e.LapStart = at; e.NextCheckpoint = 0; e.Missed.Clear(); e.Forgiven.Clear();
                e.PenaltyTotal = e.LapPenalty = 0f; e.MissedTotal = 0; e.PenaltyLog.Clear();
                if (e.IsHuman) SayTo(e, "Lap 1 - go!", 3f);
                if (e.IsPlayer) markers.Refresh(this);
                Log(e, "START LINE crossed: clock started");
                return;
            }
            // Round 34 (A6, Dan's Chastain Ridge race: lap 1 counted at 54 s with all six checkpoints missed): crossing the line again
            // without having gone round (backed over it and crossed again, or never left it) neither ends a lap nor costs anything
            if (!e.LapAway && e.NextCheckpoint == 0)
            {
                Log(e, $"START LINE crossed again at {at:0.000} with no lap driven: ignored");
                return;
            }
            CountLap(e, at, false);
        }

        // the lap ends: at the line, or (Round 54, B) at the next lap's checkpoint when the line was missed
        void CountLap(RaceEntrant e, float at, bool lineMissed)
        {
            var skipped = new List<int>();
            for (int k = e.NextCheckpoint; k < Route.Checkpoints.Count; k++) { Miss(e, k, at); skipped.Add(k + 1); }
            float raw = at - e.LapStart, lap = raw + e.LapPenalty;
            int lapNo = e.CompletedLaps + 1;
            e.LapTimes.Add(lap); e.CompletedLaps++;
            bool best = false;
            if (e.IsPlayer && !NoRecords) { best = BestLap <= 0f || lap < BestLap; if (best) { BestLap = lap; NewBestLap = true; SaveRecords(); } }
            if (e.IsPlayer && NoRecords) lapRanks.Add(0);
            else if (e.IsPlayer) { LastLapRank = Boards.Add(attempt + "/lap/" + lapNo, BoardCategory, false, lap, VehicleId, DateTime.UtcNow.ToString("o"), name: SplitScreen.Active ? SplitScreen.NameOf(1) : null); lapRanks.Add(LastLapRank); }   // Round 34 (C1)
            else if (e.Seat == 2 && e.IsHuman) Boards.Add(attempt2 + "/lap/" + lapNo, BoardCategory, false, lap, VehicleOf(e), DateTime.UtcNow.ToString("o"), name: SplitScreen.NameOf(2));   // Round 37: player 2's own, under their name
            string pen = e.LapPenalty > 0f ? $"  (includes +{e.LapPenalty:0} s penalty, {MissedText(e)})" : "";
            string fg = e.Forgiven.Count > 0 ? $"  (shortcut: CP{string.Join(",", e.Forgiven.ConvertAll(x => x + 1))} forgiven)" : "";
            e.LapLog.Add($"Lap {lapNo}: {Fmt(lap)}{pen}{fg}" + (best ? "  best lap" : ""));
            if (e.IsHuman)
            {
                if (lineMissed) SayTo(e, $"Missed the {(e.CompletedLaps >= Route.Laps ? "finish" : "start / finish")} line  +{MissPenaltySeconds:0} s     Lap {lapNo} {Fmt(lap)}", 5f);
                else if (skipped.Count > 0) SayTo(e, $"Missed checkpoint{(skipped.Count > 1 ? "s" : "")} {string.Join(", ", skipped)}  +{skipped.Count * MissPenaltySeconds:0} s     Lap {lapNo} {Fmt(lap)}", 5f);
                else SayTo(e, $"Lap {lapNo} {Fmt(lap)}" + (best ? "  best lap!" : ""), 4f);
            }
            Log(e, $"LAP {lapNo} COUNTED {lap:0.000} (clock {raw:0.000} + penalty {e.LapPenalty:0.0})" + (e.Missed.Count > 0 ? $"; {MissedText(e)}" : "") + (e.Forgiven.Count > 0 ? $"; forgiven {string.Join(",", e.Forgiven.ConvertAll(x => "CP" + (x + 1)))}" : ""));
            if (e.IsPlayer && Ghost) Ghost.Boundary(at, crossing, e.Car.Body.rotation, true, e.CompletedLaps >= Route.Laps, e.Missed.Count == 0 && !NoRecords);   // Round 43 (B1)
            GetComponent<ReplayRecorder>()?.Lap(e, lapNo, at - raw, at, e.IsPlayer && lapRanks.Count > 0 ? lapRanks[lapRanks.Count - 1] : 0);   // Round 57 (F): a new #1 lap keeps its replay
            if (e.CompletedLaps >= Route.Laps) { Finish(e, at); return; }
            e.LapStart = at; e.NextCheckpoint = 0; e.LapPenalty = 0f; e.Missed.Clear(); e.Forgiven.Clear(); e.ActiveShortcut = null; e.LapAway = false; e.RingSkipped = -1;
            if (e.IsPlayer) markers.Refresh(this);
        }

        string MissedText(RaceEntrant e) { var names = new List<string>(); foreach (int k in e.Missed) names.Add(k >= Route.Checkpoints.Count ? "the line" : "CP" + (k + 1)); return "missed " + string.Join(", ", names); }

        bool winnerCelebrated;   // Round 28 (C2)
        int parkSlots;           // Round 28: finished AI cars' places on the route's parking line (RoadDriver.DriveToPark)
        public int TakeParkSlot() => parkSlots++;
        void Finish(RaceEntrant e, float at)
        {
            e.Finished = true; e.FinalTotal = at - e.StartAt + e.PenaltyTotal;   // its clock plus every penalty
            e.FinishedAt = at + e.PenaltyTotal;
            Log(e, $"FINISH total {e.FinalTotal:0.000} (clock {e.FinalTotal - e.PenaltyTotal:0.000} + penalties {e.PenaltyTotal:0.0}); laps {string.Join(" ", e.LapTimes.ConvertAll(x => x.ToString("0.000")))}; place {Ordered().IndexOf(e) + 1}");
            // Round 28 (C2, Dan: "add the waved fist and celebrations from Woodstock Rush"): Racer's RaceDirector rule: when the first racer
            // finishes a race with others in it, the winner (the best total among those finished) celebrates (both fists up, pumped; in a
            // car a fist out of the window)
            if (!winnerCelebrated && Entrants.Count > 1)
            {
                winnerCelebrated = true;
                RaceEntrant w = null; foreach (var x in Entrants) if (x.Finished && x.Car && (w == null || x.FinalTotal < w.FinalTotal)) w = x;
                var g = w != null ? w.Car.GetComponent<RiderGestures>() : null;
                if (g && g.Celebrate()) Log(w, "celebrates the win");
            }
            if (e.Seat == 2 && e.IsHuman)
            {
                // Round 37: player 2's race on the Top 10 under their name (Racer's SplitRace.Record)
                e.RaceRank = Boards.Add(attempt2 + "/race", BoardCategory, true, e.FinalTotal, VehicleOf(e), DateTime.UtcNow.ToString("o"), name: SplitScreen.NameOf(2));
                Log(e, $"TOP 10: race {(e.RaceRank > 0 ? "#" + e.RaceRank : "not on the board")}");
                SayTo(e, $"Finished! Total {Fmt(e.FinalTotal)}", 10f);
            }
            if (!e.IsPlayer) { GetComponent<ReplayRecorder>()?.Finished(e, 0); return; }
            State = Stage.Finished; PlayerFinishedAt = RaceClock;
            float total = e.FinalTotal;
            bool bestTotal = !NoRecords && (BestTotal <= 0f || total < BestTotal);
            if (bestTotal) { BestTotal = total; NewBestTotal = true; }
            if (!NoRecords) SaveRecords();
            RaceRank = NoRecords ? 0 : Boards.Add(attempt + "/race", BoardCategory, true, total, VehicleId, DateTime.UtcNow.ToString("o"), name: SplitScreen.Active ? SplitScreen.NameOf(1) : null);   // Round 34 (C1, C2)
            FinishCards.Finish(e.LapTimes, lapRanks.ToArray(), attempt, total, RaceRank);
            GetComponent<ReplayRecorder>()?.Finished(e, RaceRank);   // Round 57 (F): a new #1 race keeps its replay
            Log($"TOP 10: race {(RaceRank > 0 ? "#" + RaceRank : "not on the board")}, laps {string.Join(" ", lapRanks.ConvertAll(x => x > 0 ? "#" + x : "-"))}");
            SayTo(e, $"Finished! Total {Fmt(total)}", 10f);
            markers.Refresh(this);
            // Round 43 (C1): Racer's winner shot before the results, in a race with opponents (not split screen): the best total among those finished
            if (Entrants.Count > 1 && !SplitScreen.Active) { RaceEntrant won = null; foreach (var x in Entrants) if (x.Finished && x.Car && (won == null || x.FinalTotal < won.FinalTotal)) won = x; if (won != null) { WinnerShot.Begin(this, won.Car, won.Name, won == P); Log(won, "the winner shot"); } }
        }

        // ---------------------------------------------------------------- Round 15: the AI drivers

        [Serializable] class AiDriverJson { public string name, vehicle; }
        [Serializable] class AiDriversJson { public string note; public AiDriverJson[] drivers; }
        // Racer's quick-race rivals: names and paints (RaceDirector.CreateCars)
        static readonly string[] AiNames = { "EMBER", "GOLD", "BLUE" };
        static readonly Color[] AiPaints = { new Color(.95f, .25f, .12f), new Color(.95f, .75f, .1f), new Color(.2f, .55f, 1f) };

        public static string AiSettingsPath()
        {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "local", "ai_drivers.json")); // git-ignored
#else
            return Path.Combine(Application.persistentDataPath, "ai_drivers.json");
#endif
        }

        // Each AI driver's vehicle, "random" (a new pick from the eleven every race) or a vehicle id, kept in ai_drivers.json.
        // Round 16: chosen in the menus (RACE > Opponent vehicles); the file stays where the choice is kept. A check never
        // reads or writes it (-aiVehicles instead).
        public static string AiDriverName(int i) => AiNames[Mathf.Clamp(i, 0, AiNames.Length - 1)];
        static string[] aiChoices;   // this session's choice (a check keeps it here only)
        public static string[] SavedAiChoices()
        {
            if (aiChoices != null) return aiChoices;
            var chosen = aiChoices = new[] { "random", "random", "random" };
            if (CheckRun.Active) return chosen;
            try
            {
                string path = AiSettingsPath();
                if (File.Exists(path))
                {
                    var f = JsonUtility.FromJson<AiDriversJson>(File.ReadAllText(path));
                    if (f?.drivers != null) for (int i = 0; i < chosen.Length && i < f.drivers.Length; i++) { var v = f.drivers[i]?.vehicle; if (!string.IsNullOrEmpty(v) && (v == "random" || VehicleProfile.IndexOf(v) >= 0)) chosen[i] = v; }
                }
                else SaveAiChoices(chosen);
            }
            catch (Exception ex) { Debug.LogWarning("[Race] ai_drivers.json unreadable, all random: " + ex.Message); }
            return chosen;
        }
        public static void SaveAiChoices(string[] chosen)
        {
            aiChoices = chosen;
            if (CheckRun.Active) return;
            try
            {
                string path = AiSettingsPath(); Directory.CreateDirectory(Path.GetDirectoryName(path));
                var def = new AiDriversJson { note = "AI drivers' vehicles (chosen in the menus: RACE > Opponent vehicles). \"random\" = a new pick from the eleven every race, or a vehicle id: " + string.Join(", ", Array.ConvertAll(VehicleProfile.All, p => p.Id)) + ".", drivers = new AiDriverJson[3] };
                for (int i = 0; i < 3; i++) def.drivers[i] = new AiDriverJson { name = AiNames[i], vehicle = i < chosen.Length ? chosen[i] : "random" };
                File.WriteAllText(path, JsonUtility.ToJson(def, true));
            }
            catch (Exception ex) { Debug.LogWarning("[Race] could not save ai_drivers.json: " + ex.Message); }
        }
        string[] AiVehicles(System.Random rng)
        {
            var picks = new string[AiCount];
            string[] chosen = AiVehiclesOverride ?? (CheckRun.Active ? null : SavedAiChoices());
            for (int i = 0; i < picks.Length; i++)
            {
                string id = chosen != null && i < chosen.Length ? chosen[i] : null;
                if (string.IsNullOrEmpty(id) || id == "random" || VehicleProfile.IndexOf(id) < 0 || !RouteData.AllowsVehicle(id))   // Round 18: only the route's vehicles
                {
                    var pool = Array.FindAll(VehicleProfile.All, v => RouteData.AllowsVehicle(v.Id));
                    id = pool[rng.Next(pool.Length)].Id;
                }
                picks[i] = id;
            }
            return picks;
        }

        // The road the AI drive (the lap, closed) and the shortcuts as they drive them, made at each race start.
        void BuildAiRoad()
        {
            if (AiRoad != null) Destroy(AiRoad.gameObject);   // a new road for each race (the direction may have changed)
            AiRoad = new GameObject("AI race road").AddComponent<RaceRoad>();
            var pts = new List<Vector3>(Route.Path); if (pts.Count > 1 && (pts[0] - pts[pts.Count - 1]).sqrMagnitude < 0.01f) pts.RemoveAt(pts.Count - 1);
            AiRoad.points = pts.ToArray();
            if (RouteData.trail && RouteData.ai_half_width_m > 0f) AiRoad.halfWidth = RouteData.ai_half_width_m;   // Round 18: a narrow trail
            AiRoad.Initialize();
            var widths = Route.OwnReverse ? RouteData.ai_widths_rev : !Route.Reverse ? RouteData.ai_widths : null;   // Round 62: the narrower stretches
            if (widths != null && widths.Length > 0) AiRoad.halfZones = widths.Select(w => new Vector3(w.s0, w.s1, w.half)).ToArray();
            AiShortcuts.Clear();
            foreach (var sc in Route.Shortcuts)
            {
                var line = new GameObject("AI shortcut " + sc.Id).AddComponent<RaceRoad>(); line.transform.SetParent(AiRoad.transform, false);
                line.openHighway = true; line.points = sc.Path; line.Initialize();
                var a = new AiShortcut { Id = sc.Id, Name = sc.Name, Line = line, FirstForgiven = sc.FirstForgiven, entrySpeed = sc.AiSpeed > 0f ? sc.AiSpeed : AiShortcutSpeed(sc.Id), profile = sc.AiProfile,
                                       take = sc.AiTake, slip = sc.AiSlip, slipProfile = sc.AiSlipProfile };
                a.entryRoad = AiRoad.Project(sc.Start, out _); a.exitRoad = AiRoad.Project(sc.End, out _);
                a.Allows = c => AiShortcutAllowed(sc.Id, c) && (barriers == null || barriers.ShortcutOpen(sc.Id));   // Round 27: never through a closed switch gate
                AiShortcuts.Add(a);
            }
            runIn = RunInLine();
        }
        AiShortcut runIn;

        // Round 70 (B, Dan: "possibly just follow the courses too exactly ... I think the ai should be not so slavish to the actual roads"): the AI's
        // racing line (RaceRoad.BuildLine) through the room either side of the lap's line: the lane less the car's half width, and past the lane's
        // edge, a meter at a time out to 7 m, the shoulder, grass or lot beside it while it is open: nearly level with the road (no bank, ditch or
        // curb drop), nothing standing there (a collider, a breakable, a trunk) and no water. Where the lap has a narrower width of its own (a bridge,
        // the mall) or runs off the terrain (a deck, a floor, a pipe) only its own width. Not on a trail route (its narrow trail mode) or underground.
        public static bool AiLineOff = Array.IndexOf(Environment.GetCommandLineArgs(), "-r70LineOff") >= 0;   // checks: the AI as before Round 70
        void BuildAiLine()
        {
            if (AiRoad == null || RouteData == null || RouteData.trail || Route.Underground || Route.Air || AiLineOff) return;
            RaceOnly.RefreshAll(); Physics.SyncTransforms();
            var trunks = TreeTrunks.Instance; var hits = new Collider[16]; const float CarHalf = 1.3f;
            var zones = AiZones;
            bool Open(Vector3 qp)
            {
                int k = Physics.OverlapCapsuleNonAlloc(qp + Vector3.up * .5f, qp + Vector3.up * 2.2f, 1.1f, hits, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < k; i++) { var h = hits[i]; if (h is TerrainCollider || h.attachedRigidbody) continue; return false; }
                return !(trunks && trunks.AnyTrunk(qp, 1.2f)) && !ShallowWater.InWater(qp + Vector3.up * .2f);
            }
            AiRoad.BuildLine((c, r, s, side) =>
            {
                // through a held zone (a jump's run-up, a narrow place) and 40 m before it: the lap's own line (a jump is taken square, in the middle)
                if (zones != null) foreach (var z in zones) if (Mathf.Repeat(s - (z.s0 - 40f), AiRoad.Length) <= z.s1 - z.s0 + 45f) return .3f;
                float half = AiRoad.HalfWidth(s), room = .3f;
                bool onGround = !float.IsNaN(GroundQuery.Height(c.x, c.z)) && Mathf.Abs(GroundQuery.Height(c.x, c.z) - c.y) <= 1.5f;
                // within the lane: as far as nothing stands in it (a closure's barrier where the lap turns off a road)
                for (float d = 1f; d <= half; d += 1f)
                {
                    var q = c + r * side * d; var qp = new Vector3(q.x, onGround ? GroundQuery.Height(q.x, q.z) : c.y, q.z);
                    if (!Open(qp)) return Mathf.Max(.3f, d - 1f - CarHalf);
                    room = Mathf.Max(.3f, d - CarHalf);
                }
                room = Mathf.Max(.3f, half - CarHalf);
                if (half < AiRoad.halfWidth - .01f || !onGround) return room;
                float y0 = GroundQuery.Height(c.x, c.z), prevY = y0;
                for (float d = half + .5f; d <= half + 7f; d += 1f)
                {
                    var q = c + r * side * d; float gy = GroundQuery.Height(q.x, q.z); if (float.IsNaN(gy)) break;
                    if (Mathf.Abs(gy - y0) > .8f + .12f * (d - half) || Mathf.Abs(gy - prevY) > .45f) break;
                    prevY = gy; if (!Open(new Vector3(q.x, gy, q.z))) break;
                    room = d - CarHalf;
                }
                return room;
            });
            Log("AI " + AiRoad.LineInfo);
        }

        // The speed an AI driver arrives with and holds through each shortcut (Racer's aiEntrySpeed), from the shortcut checks:
        // #2 is clean at 20 m/s in every vehicle (26 m/s and up a car hits a house: Rounds 12 to 14), #6 clears 1641 from 20 m/s.
        public static float AiShortcutSpeed(string id) => id.Contains("over_house") ? 22f : 20f;
        // Which vehicles an AI driver takes each shortcut in: only those that got through it in the Round 15 trials
        // (Round15Runner, "shortcuts"); the others stay on the road there.
        public static readonly Dictionary<string, string[]> AiShortcutNever = new Dictionary<string, string[]>();
        static bool AiShortcutAllowed(string id, ArcadeVehicle c)
        {
            var cfg = c ? c.GetComponent<VehicleConfiguration>() : null;
            return cfg == null || !AiShortcutNever.TryGetValue(id, out var never) || Array.IndexOf(never, cfg.profileId) < 0;
        }

        // Racer's grid: the player at the back on the old grid spot, the AI drivers ahead at 8, 15 and 22 m, either side of the
        // lane (2.2 m off the line), each moved toward the line if its spot is not clear.
        List<(Vector3 pos, Vector3 fwd)> GridSlots() => GridSlots(AiCount);
        List<(Vector3 pos, Vector3 fwd)> GridSlots(int count)
        {
            var line = new List<Vector3>();
            if (!Route.Reverse && RouteData.lead_in != null && RouteData.lead_in.Length >= 4)
            {
                for (int i = 0; i + 1 < RouteData.lead_in.Length; i += 2) line.Add(RaceRoute.Surface(RouteData.lead_in[i], RouteData.lead_in[i + 1]));
            }
            else line.AddRange(Route.SamplePath(Route.GateS - RouteData.reverse_grid_back_m, Route.GateS, 2f));
            var slots = new List<(Vector3, Vector3)>();
            for (int n = 0; n < count; n++)
            {
                // Round 37: the last of count slots for split screen's player 2 is beside the player (on the old grid spot's other side)
                bool beside = count > AiCount && n == count - 1;
                float d = beside ? 0f : 8f + n * 7f; float side = beside ? -2.6f : n % 2 == 0 ? 2.2f : -2.2f;
                var p = Along(line, d, out var f);
                if (beside) { p = Route.GridPosition; f = Quaternion.Euler(0, Route.GridYaw, 0) * Vector3.forward; }
                var right = Vector3.Cross(Vector3.up, f).normalized;
                Vector3 best = p;
                foreach (float k in new[] { 1f, 0.6f, 0.3f, 0f, -0.3f })
                {
                    var q = RaceRoute.Surface(p.x + right.x * side * k, p.z + right.z * side * k);
                    if (!Physics.CheckBox(q + Vector3.up * 1.0f, new Vector3(1.1f, 0.6f, 2.4f), Quaternion.LookRotation(f), ~0, QueryTriggerInteraction.Ignore)) { best = q; break; }
                }
                slots.Add((best, f));
            }
            return slots;
        }

        static Vector3 Along(List<Vector3> line, float d, out Vector3 fwd)
        {
            float acc = 0f;
            for (int i = 0; i + 1 < line.Count; i++)
            {
                var a = line[i]; var b = line[i + 1]; var ab = b - a; ab.y = 0f; float L = ab.magnitude;
                if (acc + L >= d || i + 2 == line.Count) { fwd = L > 1e-4f ? ab / L : Vector3.forward; return Vector3.Lerp(a, b, Mathf.Clamp01((d - acc) / Mathf.Max(L, 1e-4f))); }
                acc += L;
            }
            fwd = Vector3.forward; return line[0];
        }

        // forward races start on the entrance lane: the AI follow the lead-in to the lap, then 40 m of the lap
        AiShortcut RunInLine()
        {
            if (Route.Reverse || RouteData.lead_in == null || RouteData.lead_in.Length < 4) return null;
            var pts = new List<Vector3>();
            for (int i = 0; i + 1 < RouteData.lead_in.Length; i += 2) pts.Add(RaceRoute.Surface(RouteData.lead_in[i], RouteData.lead_in[i + 1]));
            float j = AiRoad.Project(pts[pts.Count - 1], out _);
            for (float d = 4f; d <= 40f; d += 4f) pts.Add(AiRoad.At(j + d, out _));
            var line = new GameObject("AI run-in").AddComponent<RaceRoad>(); line.transform.SetParent(AiRoad.transform, false);
            line.openHighway = true; line.points = pts.ToArray(); line.Initialize();
            return new AiShortcut { Id = "run-in", Name = "run-in", Line = line, FirstForgiven = int.MaxValue };
        }

        void SpawnAi()
        {
            if (AiCount <= 0) { aiActive = false; return; }
            if (Route.Air) { SpawnPilots(); return; }   // Round 71: the air race's AI pilots (RaceController.Air.cs)
            BuildAiRoad();
            var rng = new System.Random(Environment.TickCount);
            DriverVariation.Seed = rng.Next();
            var vehicles = AiVehicles(rng);
            var slots = GridSlots();
            var riders = RiderLook.Field(DriverVariation.Seed, Mathf.Max(3, AiCount), RiderLook.Player);
            for (int n = 0; n < AiCount; n++)
            {
                var clone = Instantiate(car.gameObject);
                clone.name = AiNames[n];
                // the player's own parts that an AI car must not have: the vehicle switch, the player's input and reset, any
                // check probes; its engine is heard through OtherEngines (Round 27: the nearest four AI engines, in 3D, Racer 0.113)
                foreach (var mb in clone.GetComponents<MonoBehaviour>())
                {
                    if (mb is VehicleInput || mb is VehicleRespawn) { mb.enabled = false; continue; }
                    if (mb is ArcadeVehicle || mb is VehicleConfiguration || mb is RampHold || mb is WaterFeedback || mb is RiderGestures || mb is VehicleLights) continue;
                    mb.enabled = false; if (mb is VehicleAudio) DestroyImmediate(mb); else Destroy(mb);   // Round 27 (B2): no VehicleAudio on an AI car; OtherEngines plays the nearest AI engines (Racer 0.113)
                }
                foreach (var src in clone.GetComponents<AudioSource>()) Destroy(src);
                foreach (Transform child in clone.transform) if (child.name == "Headlight") Destroy(child.gameObject);   // VehicleLights makes its own
                var ai = clone.GetComponent<ArcadeVehicle>();
                var cfg = clone.GetComponent<VehicleConfiguration>();
                cfg.riderLook = riders[n % riders.Length];
                cfg.Apply(vehicles[n]);
                cfg.SetPaint(AiPaints[n % AiPaints.Length]);
                ai.enabled = false; ai.Body.isKinematic = true;
                var (pos, fwd) = slots[n];
                var rot = Quaternion.LookRotation(fwd, Vector3.up);
                ai.Body.position = pos + Vector3.up * Mathf.Max(.4f, ai.suspensionLength - Physics.gravity.magnitude / ai.springStrength);
                ai.Body.rotation = rot; clone.transform.SetPositionAndRotation(ai.Body.position, rot);
                ai.Body.linearVelocity = ai.Body.angularVelocity = Vector3.zero; ai.ClearSteering();
                var e = new RaceEntrant { Name = AiNames[n], Car = ai, Box = clone.GetComponent<BoxCollider>() };
                var driver = clone.AddComponent<RoadDriver>();
                e.Driver = driver;
                driver.Initialize(this, ai, e, RoadDriver.Difficulty == 2 ? .97f + n * .015f : .94f + n * .025f, n);   // Racer's pace spread for rivals (Round 70: on Hard .97 to 1.0, the leader at full pace)
                driver.RunIn = runIn;
                e.Reset();
                Entrants.Add(e);
                if (TreeTrunks.Instance) TreeTrunks.Instance.Track(clone.transform);
                Log($"AI driver {e.Name}: {cfg.Profile.Name} ({cfg.profileId}) on the grid at ({pos.x:0.0}, {pos.z:0.0})");
            }
            aiActive = true; nextEstimateSample = 0f;
        }

        void RemoveAi()
        {
            for (int i = Entrants.Count - 1; i >= 0; i--)
            {
                var e = Entrants[i]; if (e.IsPlayer) continue;
                if (e.Seat == 2) { if (e.Driver) { e.Driver.enabled = false; Destroy(e.Driver); } if (e.Car) { e.Car.Body.isKinematic = false; e.Car.enabled = true; } Entrants.RemoveAt(i); continue; }   // Round 37: player 2's vehicle stays (split screen)
                if (e.Car) { if (TreeTrunks.Instance) TreeTrunks.Instance.Untrack(e.Car.transform); e.Car.gameObject.SetActive(false); Destroy(e.Car.gameObject); }
                Entrants.RemoveAt(i);
            }
            aiActive = false;
        }

        // Racer's AiFinishEstimate: each AI driver's pace is sampled every second; once the player has finished and
        // PlayerFinishWait has passed, any AI driver still racing is classified at its estimated finish.
        // Round 54 (C, Dan: "on Woodstock Rush there was the option to simulate the times of the rest of the ai racers when you were
        // done with the race"): Racer's COMPLETE RACE (RaceMenus.CanSimulateRemaining, RaceDirector.FinalizeUnfinishedAi). Once the player
        // has finished, the AI drivers still racing are classified now at their estimated finish (as the 60 s wait would) and stop where
        // they are, out of the way. Records are only ever the player's real finish (already saved at the line).
        public bool CanSimulateRemaining => State == Stage.Finished && !SplitScreen.Active && aiActive && Entrants.Exists(x => !x.IsPlayer && !x.IsHuman && !x.Finished && x.Car);
        bool completeNow;
        public void CompleteRace()
        {
            if (!CanSimulateRemaining) return;
            completeNow = true; TrackAi(); completeNow = false;
            foreach (var e in Entrants)
            {
                if (e.IsPlayer || !e.Estimated || !e.Car) continue;
                if (e.Driver) e.Driver.enabled = false;
                if (!e.Car.Body.isKinematic) e.Car.Body.linearVelocity = e.Car.Body.angularVelocity = Vector3.zero;
                e.Car.Body.isKinematic = true; e.Car.Body.detectCollisions = false;   // Racer: frozen in place, out of every contact
            }
            Log("COMPLETE RACE: the AI still racing are classified at their estimated finishes");
        }

        void TrackAi()
        {
            bool sample = RaceClock >= nextEstimateSample; if (sample) nextEstimateSample = RaceClock + 1f;
            bool estimate = State == Stage.Finished && (completeNow || RaceClock - PlayerFinishedAt >= PlayerFinishWait);
            bool any = false;
            foreach (var e in Entrants)
            {
                if (e.IsPlayer || e.Finished) continue;
                float remaining = Remaining(e);
                if (sample) e.Estimate.Sample(RaceClock, remaining, e.Car.topSpeed, e.Driver && e.Driver.StalledSeconds > 1f);
                if (estimate)
                {
                    var cfg = e.Car.GetComponent<VehicleConfiguration>();
                    float more = Route.Air ? remaining / AirPace(e) : (float)e.Estimate.Duration(RaceClock, remaining, cfg ? cfg.Profile : VehicleProfile.All[0], false, RoadDriver.Difficulty);   // Round 71: a plane's own pace
                    if (!e.ClockStarted) { e.ClockStarted = true; e.StartAt = RaceClock; }
                    e.Finished = e.Estimated = true;
                    e.FinalTotal = RaceClock + more - e.StartAt + e.PenaltyTotal; e.FinishedAt = RaceClock + more + e.PenaltyTotal;
                    Log(e, $"FINISH ESTIMATED total {e.FinalTotal:0.000} ({remaining:0} m to go at its pace)");
                    continue;
                }
                any = true;
            }
            if (!any && State == Stage.Finished) aiActive = false;
        }

        // distance still to drive to the finish, and progress (for the places): laps done x lap + how far into this lap
        float Into(RaceEntrant e)
        {
            float L = Route.LapLength, st = Route.LapStationOf(e.Car.transform.position);
            if (st > L - 60f && e.NextCheckpoint == 0) st -= L;   // just over the line, still read as the end of the last lap
            return st;
        }
        float Remaining(RaceEntrant e) => !e.ClockStarted ? Route.Laps * Route.LapLength + Vector3.Distance(e.Car.transform.position, Route.Gate.Position) : Mathf.Max(0f, (Route.Laps - e.CompletedLaps) * Route.LapLength - Into(e));
        public float Score(RaceEntrant e) => e.Finished ? 1e7f - e.FinishedAt : !e.ClockStarted ? -Vector3.Distance(e.Car.transform.position, Route.Gate.Position) : e.CompletedLaps * Route.LapLength + Into(e);
        public List<RaceEntrant> Ordered()
        {
            var list = new List<RaceEntrant>(Entrants);
            list.Sort((a, b) => Score(b).CompareTo(Score(a)));
            return list;
        }
        public int PlayerPlace => Ordered().IndexOf(P) + 1;

        // ---------------------------------------------------------------- records (best lap, best total)

        // Round 13: kept per vehicle. The Street Classic ("original", the retired prototype's own tuning) keeps the key the
        // records had before, so its best times carry on; every other vehicle adds its id
        string RecordKey { get { string v = car ? car.GetComponent<VehicleConfiguration>()?.profileId : null; return $"{Route.RecordName}|{RecordDirection}|{Route.Laps}" + (string.IsNullOrEmpty(v) || v == "original" ? "" : "|" + v); } }
        public void ReloadRecords() { if (Route != null) LoadRecords(); }   // Round 13: after a vehicle switch

        public static string RecordsPath()
        {
            if (!string.IsNullOrEmpty(RecordsPathOverride)) return RecordsPathOverride;
            if (CheckRun.Active) return Path.Combine(CheckRun.Dir, "race_records_check.json");   // Round 15, item 5
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "local", "race_records.json")); // git-ignored
#else
            return Path.Combine(Application.persistentDataPath, "race_records.json");
#endif
        }

        void LoadRecords()
        {
            records = new RecordFile(); BestLap = BestTotal = 0f;
            try { if (File.Exists(RecordsPath())) records = JsonUtility.FromJson<RecordFile>(File.ReadAllText(RecordsPath())) ?? new RecordFile(); }
            catch (Exception e) { Debug.LogWarning("[Race] records unreadable, starting empty: " + e.Message); }
            var mine = records.entries.Find(x => x.key == RecordKey);
            if (mine != null) { BestLap = mine.best_lap; BestTotal = mine.best_total; }
        }

        void SaveRecords()
        {
            try
            {
                var mine = records.entries.Find(x => x.key == RecordKey);
                if (mine == null) { mine = new RecordEntry { key = RecordKey }; records.entries.Add(mine); }
                mine.best_lap = BestLap; mine.best_total = BestTotal;
                string path = RecordsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(records, true));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
            }
            catch (Exception e) { Debug.LogWarning("[Race] could not save records: " + e.Message); }
        }

        // ---------------------------------------------------------------- text

        void Say(string text, float seconds) { Message = text; messageUntil = Time.time + seconds; }
        // Round 37: a racer's own line (the player's is the one-player HUD's message too)
        void SayTo(RaceEntrant e, string text, float seconds) { if (e.IsPlayer) Say(text, seconds); e.Message = text; e.MessageUntil = Time.time + seconds; }
        void Log(string text) { Logged?.Invoke(text); if (!Application.isBatchMode) Debug.Log("[Race] " + text); }
        void Log(RaceEntrant e, string text) => Log(e.IsPlayer ? text : e.Name + ": " + text);   // Round 15: the AI drivers' events carry their name

        public static string Fmt(float seconds)
        {
            if (seconds <= 0f) return "--:--.---";
            int m = (int)(seconds / 60f); float s = seconds - m * 60f;
            return $"{m}:{s:00.000}";
        }

        // The next gate the driver should aim for, and how far it is; used by the HUD arrow.
        public RaceGate Target => State == Stage.Free || Route == null ? null
            : !ClockStarted ? Route.Gate : AirAim(P) is RaceGate g ? g : NextCheckpoint < Route.Checkpoints.Count ? Route.Checkpoints[NextCheckpoint] : Route.Gate;
        // Round 73 (B2): in the air race, once a ring has been flown past outside it the guide points at the one after it (the missed one can still be
        // gone back for); null otherwise
        public RaceGate AirAim(RaceEntrant e)
        {
            if (Route == null || !Route.Air || e == null || e.RingSkipped < 0 || e.RingSkipped != e.NextCheckpoint) return null;
            return e.RingSkipped + 1 < Route.Checkpoints.Count ? Route.Checkpoints[e.RingSkipped + 1] : Route.Gate;
        }
        // the ring after the one the guide points at (shown dimmer), or null
        public RaceGate AirAfter(RaceEntrant e)
        {
            if (Route == null || !Route.Air || e == null || !e.ClockStarted) return Route != null && Route.Air && Route.Checkpoints.Count > 0 ? Route.Checkpoints[0] : null;
            int k = (e.RingSkipped >= 0 && e.RingSkipped == e.NextCheckpoint ? e.RingSkipped + 1 : e.NextCheckpoint) + 1;
            return k < Route.Checkpoints.Count ? Route.Checkpoints[k] : k == Route.Checkpoints.Count ? Route.Gate : null;
        }

        // ---------------------------------------------------------------- Round 37: split screen's player 2 (Racer's SplitRace.PlayerTwo)
        // Player 2 races in the race as a racer of its own, under the same rules: a human on their own device, or (Player 2: AI driver) Racer's AI
        // driving player 2's vehicle. On the grid player 2 takes the first slot ahead of the player (Racer: the first rival slot); the AI rivals
        // follow. Their laps and race go on the Top 10 under their name; the race is over for the menus when every human has finished.
        string attempt2 = "";
        public RaceEntrant HumanEntrant(int player) => player == 1 ? P : Entrants.Find(x => x.Seat == 2);
        public bool HumanFinished(int player) { var e = HumanEntrant(player); return e != null && e.Finished; }
        public bool AllHumansFinished { get { if (!P.Finished) return false; var e = HumanEntrant(2); return e == null || !e.IsHuman || e.Finished; } }
        public bool GoShowing => State == Stage.Racing && Time.time < goFlashUntil;
        public float LapClockOf(RaceEntrant e) => e.ClockStarted && State != Stage.Free ? RaceClock - e.LapStart + e.LapPenalty : 0f;
        public float TotalClockOf(RaceEntrant e) => (e.ClockStarted ? (e.Finished ? e.FinalTotal - e.PenaltyTotal : RaceClock - e.StartAt) : 0f) + e.PenaltyTotal;
        public RaceGate TargetOf(RaceEntrant e) => State == Stage.Free || Route == null ? null : !e.ClockStarted ? Route.Gate : e.NextCheckpoint < Route.Checkpoints.Count ? Route.Checkpoints[e.NextCheckpoint] : Route.Gate;
        static string VehicleOf(RaceEntrant e) => e.Car && e.Car.GetComponent<VehicleConfiguration>() ? e.Car.GetComponent<VehicleConfiguration>().profileId : "original";
        void SeatPlayerTwo()
        {
            var s = SplitScreen.Session; if (!s || !s.P2) return;
            var p2 = s.P2; s.StopCruise();
            // a vehicle the route allows (Noonday Creek: the trail's), as for player 1
            var cfg = p2.GetComponent<VehicleConfiguration>(); var sw = p2.GetComponent<VehicleSwitch>();
            if (cfg && RouteData != null && !RouteData.AllowsVehicle(cfg.profileId)) { string id = RouteData.vehicles != null && RouteData.vehicles.Length > 0 ? RouteData.vehicles[Mathf.Min(1, RouteData.vehicles.Length - 1)] : "original"; if (sw) sw.Select(id, VehicleSwitch.Pick.Forced); else cfg.Apply(id); }
            var slot = GridSlots(AiCount + 1)[AiCount];   // behind the rivals' slots: the player's own row
            var rot = Quaternion.LookRotation(slot.fwd, Vector3.up);
            var e = new RaceEntrant { Name = SplitScreen.NameOf(2), Car = p2, Box = p2.GetComponentInChildren<BoxCollider>(), Seat = 2 };
            var respawn = p2.GetComponent<VehicleRespawn>();
            if (SplitScreen.P2Ai)
            {
                if (AiRoad == null) BuildAiRoad();
                p2.enabled = false; p2.Body.isKinematic = true;
                p2.Body.position = slot.pos + Vector3.up * Mathf.Max(.4f, p2.suspensionLength - Physics.gravity.magnitude / p2.springStrength);
                p2.Body.rotation = rot; p2.transform.SetPositionAndRotation(p2.Body.position, rot); p2.Body.linearVelocity = p2.Body.angularVelocity = Vector3.zero; p2.ClearSteering();
                var driver = p2.gameObject.AddComponent<RoadDriver>(); e.Driver = driver;
                driver.Initialize(this, p2, e, .97f, AiCount); driver.RunIn = runIn;
                aiActive = true;
            }
            else if (respawn) respawn.PlaceAt(slot.pos + Vector3.up * .3f, rot.eulerAngles.y);
            e.Reset(); Entrants.Add(e);
            Log($"PLAYER 2 {e.Name} ({(SplitScreen.P2Ai ? "AI" : "human")}): {cfg?.Profile.Name} on the grid at ({slot.pos.x:0.0}, {slot.pos.z:0.0})");
        }

        public string DebugLine => State == Stage.Free ? null
            : $"Race {State}  lap {Mathf.Min(CompletedLaps + 1, LapsInRace)}/{LapsInRace}  next {(Target != null ? (Target.Id == 0 ? "start line" : "CP" + Target.Id) : "-")}  {Fmt(Clock)}  penalty +{PenaltyTotal:0}s";

        // ---------------------------------------------------------------- HUD (IMGUI, like MapCredits and the debug panel)

        void Styles()
        {
            if (small != null) return;
            small = new GUIStyle(GUI.skin.label) { fontSize = 17, richText = true, normal = { textColor = Color.white } };
            big = new GUIStyle(small) { fontSize = 30, fontStyle = FontStyle.Bold };
            huge = new GUIStyle(small) { fontSize = 120, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            panel = new GUIStyle(GUI.skin.box);
            back = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            back.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.62f)); back.Apply();
            panel.normal.background = back;
            arrow = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    // an upward arrow: triangle head on a stem
                    float u = (x - 31.5f) / 32f, v = y / 63f;
                    bool head = v > 0.45f && Mathf.Abs(u) < (1f - v) * 1.55f;
                    bool stem = v <= 0.45f && v > 0.05f && Mathf.Abs(u) < 0.2f;
                    arrow.SetPixel(x, y, head || stem ? Color.white : Color.clear);
                }
            arrow.Apply();
        }

        void Shadowed(Rect r, string text, GUIStyle style)
        {
            var old = style.normal.textColor;
            style.normal.textColor = new Color(0, 0, 0, 0.85f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), StripTags(text), style);
            style.normal.textColor = old; GUI.Label(r, text, style);
        }
        // Round 17, item 4: only the color tags go (the shadow is black); bold stays, else the shadow is narrower than the
        // bold text and shows as a second, offset copy of the line
        static string StripTags(string s) => System.Text.RegularExpressions.Regex.Replace(s, "</?color[^>]*>", "");

        void OnGUI()
        {
            if (Route == null || !car || GameMenus.Open || WorldMap.IsOpen || TrailerMode.HudHidden) return;   // Round 43: Trailer Mode's clean screen; Round 16: not drawn under a menu (Round 20: nor the map)
            if (SplitScreen.Active && !(State != Stage.Free && AllHumansFinished)) return;   // Round 37: each half's own HUD (SplitHud)
            Styles();
            float s = Mathf.Max(1f, Screen.height / 800f);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s, h = Screen.height / s;

            if (SplitScreen.Active) DrawResults(w, h);   // Round 37: split screen's race is over for both: the results alone
            else if (State == Stage.Free) DrawFree(w, h);
            else
            {
                DrawTimers(w, h);
                DrawPlace(w, h);
                DrawGuide(w, h);
                DrawCountdown(w, h);
                if (State == Stage.Finished && !WinnerShot.Active) DrawResults(w, h);   // Round 43 (C1): held back during the winner shot (Racer 0.82)
                if (Message.Length > 0 && Time.time < messageUntil && State != Stage.Finished)
                    Shadowed(new Rect(0, h * 0.74f, w, 40), $"<b>{Message}</b>", new GUIStyle(small) { fontSize = 26, alignment = TextAnchor.MiddleCenter, richText = true, normal = { textColor = Color.white } });
            }
            GUI.matrix = old;
        }

        void DrawFree(float w, float h)
        {
            string best = BestLap > 0f ? $"   best lap {Fmt(BestLap)}   best total {Fmt(BestTotal)}" : "";
            Shadowed(new Rect(14, 10, 900, 28), $"<b>Start / Esc</b> menu: race {Route.Name}, {Route.Laps} laps, {Route.Direction}{best}", small);
        }

        void DrawTimers(float w, float h)
        {
            var r = new Rect(14, 10, 330, 196);
            GUI.Box(r, GUIContent.none, panel);
            int lap = Mathf.Min(CompletedLaps + 1, LapsInRace);
            string lapText = State == Stage.Finished ? "Finished" : $"Lap {lap} / {LapsInRace}";
            Shadowed(new Rect(r.x + 12, r.y + 4, r.width, 40), lapText, big);
            string lapT = State == Stage.Finished && LapTimes.Count > 0 ? Fmt(LapTimes[LapTimes.Count - 1]) : ClockStarted ? Fmt(LapClock) : "--:--.---", totT = ClockStarted ? Fmt(State == Stage.Finished ? FinalTotal : TotalClock) : "--:--.---";
            Shadowed(new Rect(r.x + 12, r.y + 46, r.width, 26), $"Lap    <b>{lapT}</b>", small);
            Shadowed(new Rect(r.x + 12, r.y + 70, r.width, 26), $"Total  <b>{totT}</b>", small);
            // penalties: shown as soon as one is given (they are already inside the lap and total times above)
            string penText = PenaltyTotal > 0f ? $"<color=#ffb040>Penalty  +{PenaltyTotal:0} s  ({MissedTotal} missed)</color>" : "Penalty  none";
            Shadowed(new Rect(r.x + 12, r.y + 94, r.width, 26), penText, small);
            Shadowed(new Rect(r.x + 12, r.y + 122, r.width, 26), $"Best lap    {Fmt(BestLap)}", small);
            Shadowed(new Rect(r.x + 12, r.y + 146, r.width, 26), $"Best total  {Fmt(BestTotal)}", small);
            Shadowed(new Rect(r.x + 12, r.y + 170, r.width, 22), Hint, small);
        }

        // Round 15 (item 11): the player's place among the racers, top right, and the grid during the countdown
        static string Ordinal(int n) => n + (n % 100 >= 11 && n % 100 <= 13 ? "th" : n % 10 == 1 ? "st" : n % 10 == 2 ? "nd" : n % 10 == 3 ? "rd" : "th");
        void DrawPlace(float w, float h)
        {
            if (Entrants.Count < 2) return;
            var order = Ordered();
            var st = new GUIStyle(big) { fontSize = 54, alignment = TextAnchor.UpperRight };
            Shadowed(new Rect(w - 345, 6, 260, 64), Ordinal(order.IndexOf(P) + 1), st);
            Shadowed(new Rect(w - 70, 30, 56, 36), $"/ {order.Count}", new GUIStyle(small) { fontSize = 26, alignment = TextAnchor.UpperLeft });
            if (State != Stage.Countdown) return;
            // Racer's RaceNames.GridText: the grid, front first, the player's line in colour
            var r = new Rect(w - 360, 78, 340, 30 + 26 * order.Count);
            GUI.Box(r, GUIContent.none, panel);
            Shadowed(new Rect(r.x + 12, r.y + 4, r.width, 26), "<b>STARTING GRID</b>", small);
            for (int i = 0; i < order.Count; i++)
            {
                var e = order[i]; string line = $"{i + 1}   {e.Name}   <size=14>{e.VehicleName}</size>";
                Shadowed(new Rect(r.x + 12, r.y + 30 + 26 * i, r.width, 26), e.IsPlayer ? $"<color=#FFC747>{line}</color>" : line, small);
            }
        }

        // next-gate arrow and distance, top centre
        void DrawGuide(float w, float h)
        {
            var t = Target;
            if (t == null || State == Stage.Finished) return;
            Vector3 to = t.Position - car.transform.position; float dist3 = to.magnitude; to.y = 0f;
            Vector3 fwd = car.transform.forward; fwd.y = 0f;
            float angle = Vector3.SignedAngle(fwd, to, Vector3.up);
            if (Route.Air)
            {
                // Round 73 (B2): the air race's guide is the 3D arrow ahead of the plane (AirGuide); here the ring's name, its distance through the air,
                // and while a missed ring can still be gone back for, a line saying so
                string nm = (t.Id == 0 ? (ClockStarted ? "FINISH RING" : "START RING") : $"RING {t.Id} / {Route.Checkpoints.Count}") + AirHint(t);
                Shadowed(new Rect(0, 60, w, 34), $"<b>{nm}</b>   {dist3:0} m", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 24, richText = true, normal = { textColor = Color.white } });
                if (P.RingSkipped >= 0 && P.RingSkipped == P.NextCheckpoint)
                    Shadowed(new Rect(0, 94, w, 30), $"<color=#ff6050><b>RING {P.RingSkipped + 1} MISSED</b></color>   fly on (+{MissPenaltySeconds:0} s) or turn back for it", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 20, richText = true, normal = { textColor = Color.white } });
                return;
            }
            var c = new Vector2(w / 2f, 62f);
            var save = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, c);
            GUI.color = t.Id == 0 ? Color.white : new Color(0.45f, 1f, 0.3f);
            GUI.DrawTexture(new Rect(c.x - 28, c.y - 28, 56, 56), arrow);
            GUI.color = Color.white;
            GUI.matrix = save;
            string name = t.Id == 0 ? (ClockStarted ? "FINISH LINE" : "START LINE") : $"CHECKPOINT {t.Id} / {Route.Checkpoints.Count}";
            if (Route.Air) name = (t.Id == 0 ? (ClockStarted ? "FINISH RING" : "START RING") : $"RING {t.Id} / {Route.Checkpoints.Count}") + AirHint(t);   // Round 71: the ring's name and how far up or down
            Shadowed(new Rect(0, 94, w, 30), $"<b>{name}</b>   {to.magnitude:0} m", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 20, richText = true, normal = { textColor = Color.white } });
        }

        void DrawCountdown(float w, float h)
        {
            if (State == Stage.Countdown)
                Shadowed(new Rect(0, h * 0.25f, w, 160), Mathf.CeilToInt(CountdownRemaining).ToString(), huge);
            else if (Time.time < goFlashUntil)
                Shadowed(new Rect(0, h * 0.25f, w, 160), "GO!", huge);
        }

        void DrawResults(float w, float h)
        {
            // Round 15 (item 11): the finishing order with names, vehicles and times (penalties included), under the player's own lines
            var order = Ordered();
            float extra = Entrants.Count > 1 ? 40 + 26 * order.Count : 0;
            var r = new Rect((w - 860) / 2f, h * 0.12f, 860, 418 + extra);
            GUI.Box(r, GUIContent.none, panel);
            Shadowed(new Rect(r.x + 20, r.y + 10, r.width, 44), "FINISH", big);
            Shadowed(new Rect(r.x + 20, r.y + 60, r.width, 30), (SplitScreen.Active ? $"{SplitScreen.NameOf(1)}   " : "") + $"Total time   <b>{Fmt(FinalTotal)}</b>" + (NewBestTotal ? "   NEW BEST" : "") + (PenaltyTotal > 0f ? $"   (clock {Fmt(FinalTotal - PenaltyTotal)} + {PenaltyTotal:0} s penalties)" : ""), small);
            float best = 0f; foreach (var l in LapTimes) if (best <= 0f || l < best) best = l;
            Shadowed(new Rect(r.x + 20, r.y + 88, r.width, 30), $"Best lap this race   <b>{Fmt(best)}</b>" + (NewBestLap ? "   NEW BEST" : ""), small);
            // Round 34 (C2): Racer's finish cards: what the best lap and the total did on the Top 10
            string Card(string what, FinishPresentation.Result c) => c == null ? $"{what}   -" : $"<b>{what}</b>   " + (c.rank > 0 ? $"<color=#4CF2CF>{c.headline}   #{c.rank}</color>" : c.headline) + "   " + c.comparison.Replace("\n", "   ");
            Shadowed(new Rect(r.x + 20, r.y + 116, r.width - 30, 30), Card("TOP 10  BEST LAP", FinishCards.Lap), small);
            Shadowed(new Rect(r.x + 20, r.y + 144, r.width - 30, 30), Card("TOP 10  TOTAL RACE", FinishCards.Race), small);
            float y = r.y + 182;
            Shadowed(new Rect(r.x + 20, y, r.width, 26), PenaltyTotal > 0f ? $"<color=#ffb040>Penalties  +{PenaltyTotal:0} s  ({MissedTotal} missed checkpoint{(MissedTotal == 1 ? "" : "s")}, {MissPenaltySeconds:0} s each)</color>" : "Penalties  none", small); y += 28;
            foreach (var line in LapLog) { Shadowed(new Rect(r.x + 20, y, r.width - 30, 26), line, small); y += 24; }
            if (Entrants.Count > 1)
            {
                y += 12;
                Shadowed(new Rect(r.x + 20, y, r.width, 26), "<b>FINISHING ORDER</b>", small); y += 28;
                for (int i = 0; i < order.Count; i++)
                {
                    var e = order[i];
                    string time = e.Finished ? Fmt(e.FinalTotal) + (e.Estimated ? "  (estimated)" : "") + (e.PenaltyTotal > 0f ? $"  incl. +{e.PenaltyTotal:0} s penalty" : "") : "racing...";
                    string line = $"{i + 1}.  {e.Name,-6}  {e.VehicleName,-18}  {time}";
                    Shadowed(new Rect(r.x + 20, y, r.width - 30, 26), e.IsPlayer || e.Seat == 2 ? $"<color=#FFC747>{line}</color>" : line, small); y += 26;
                }
            }
            var p2 = HumanEntrant(2);
            if (SplitScreen.Active && p2 != null) Shadowed(new Rect(r.x + 20, r.yMax - 60, r.width, 26), $"{SplitScreen.NameOf(2)}   Total time   <b>{Fmt(p2.FinalTotal)}</b>" + (p2.Estimated ? " (estimated)" : "") + (p2.RaceRank > 0 ? $"   <color=#4CF2CF>Top 10 #{p2.RaceRank}</color>" : ""), small);
            Shadowed(new Rect(r.x + 20, r.yMax - 34, r.width, 26), "Start / Esc  menu: race again or free roam", small);
        }
    }
}
