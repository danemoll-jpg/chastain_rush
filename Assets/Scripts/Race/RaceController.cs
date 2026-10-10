using System;
using System.Collections.Generic;
using System.IO;
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
    //   T  start the race / restart it (from the countdown, mid-race or the results)     X  leave the race, back to free driving
    //   pad: D-pad up = start / restart, D-pad down = leave.   R / Y still resets the car to its last stable spot.
    //
    // Rules (Round 6, as in Racer / Woodstock Rush): 3 laps; checkpoints are met in order; a checkpoint that is skipped costs a
    // 5 s time penalty (Racer's RaceProgress.Miss) and the lap STILL COUNTS, with the penalty added to the lap and to the total;
    // taking an official shortcut (RaceRoute.Shortcuts, shaped after Racer's WoodlandRoute) forgives the checkpoints it skips;
    // while a race runs, every road that leaves the course and is not part of a shortcut is closed by a barrier (RaceBarriers).
    // Times use the physics clock (Time.fixedDeltaTime), so a bug-capture pause (Time.timeScale 0) pauses the race too, and
    // crossings are interpolated inside the physics step. Gates are wide and forgiving: see RaceGate.Crossed.
    [DefaultExecutionOrder(50)]
    public sealed class RaceController : MonoBehaviour
    {
        public enum Stage { Free, Countdown, Racing, Finished }

        public static RaceController Instance { get; private set; }
        public static string DirectionOverride;   // command line -raceDirection, or set by a check; otherwise the route file decides
        public static string RecordsPathOverride; // checks write somewhere other than the real records
        public static int LapsOverride;           // checks only: race this many laps instead of the route file's
        public static readonly List<RouteShortcutJson> ExtraShortcuts = new List<RouteShortcutJson>(); // checks only: shortcuts not in the file
        public static bool EntryPlanesOff;   // checks only (Round 13): shortcuts are taken by their corridor alone
        public const float MissPenaltySeconds = 5f;   // Racer: RaceProgress.Miss forces 5 s per missed gate

        [Header("Set by MapBuilder (Chastain Rush > Build Map)")]
        [TextArea] public string embeddedRouteJson;   // the route file as it was when the scene was built
        public string routeFileName = "chastain_glen_loop.json";
        public Material gateBlack, gateWhite, nextMaterial, laterMaterial, missedMaterial, shortcutMaterial;

        public Stage State { get; private set; } = Stage.Free;
        public RaceRoute Route { get; private set; }
        public RouteJson RouteData { get; private set; }
        public float Clock { get; private set; }        // seconds since the first crossing of the start line
        public float LapClock => clockStarted && State != Stage.Free ? Clock - lapStart + LapPenalty : 0f;   // with this lap's penalties
        public float TotalClock => Clock + PenaltyTotal;                                                    // with all penalties
        public float PenaltyTotal { get; private set; }  // seconds of penalty so far this race
        public float LapPenalty { get; private set; }    // ... of which on the lap in progress
        public int MissedTotal { get; private set; }     // checkpoints missed so far this race
        public IReadOnlyList<string> PenaltyLog => penaltyLog;
        public RaceShortcut ActiveShortcut { get; private set; }
        public IReadOnlyList<int> ForgivenThisLap => forgiven;
        public RaceBarriers Barriers => barriers;
        public int CompletedLaps { get; private set; }
        public int NextCheckpoint { get; private set; } // index into Route.Checkpoints; == Count once all are passed
        public bool ClockStarted => clockStarted;
        public float CountdownRemaining { get; private set; }
        public float FinalTotal { get; private set; }
        public string Message { get; private set; } = "";
        public IReadOnlyList<float> LapTimes => lapTimes;
        public IReadOnlyList<string> LapLog => lapLog;
        public IReadOnlyList<int> MissedThisLap => missed;
        public float BestLap { get; private set; }
        public float BestTotal { get; private set; }
        public bool NewBestLap { get; private set; }
        public bool NewBestTotal { get; private set; }
        public int LapsInRace => Route != null ? Route.Laps : 3;
        public string Hint => State == Stage.Free ? "T: start race" : "T: restart   X: leave race";
        public event Action<string> Logged; // every race event, for the scripted check

        ArcadeVehicle car;
        VehicleInput input;
        VehicleRespawn respawn;
        bool clockStarted, startPressed, endPressed, wasInputEnabled;
        float lapStart, messageUntil, goFlashUntil;
        Vector3 prevPos;
        readonly List<float> lapTimes = new List<float>();
        readonly List<string> lapLog = new List<string>();
        readonly List<int> missed = new List<int>();
        readonly List<int> forgiven = new List<int>();
        readonly List<string> penaltyLog = new List<string>();
        RaceMarkers markers;
        RaceBarriers barriers;
        BoxCollider carBox;
        Texture2D arrow;
        GUIStyle small, big, huge, panel;
        Texture2D back;

        [Serializable] class RecordEntry { public string key; public float best_lap, best_total; }
        [Serializable] class RecordFile { public List<RecordEntry> entries = new List<RecordEntry>(); }
        RecordFile records = new RecordFile();

        // ---------------------------------------------------------------- setup

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            car = FindAnyObjectByType<ArcadeVehicle>();
            if (!car) { enabled = false; return; }
            input = car.GetComponent<VehicleInput>();
            respawn = car.GetComponent<VehicleRespawn>();
            carBox = car.GetComponentInChildren<BoxCollider>();
            if (respawn) respawn.Respawned += OnRespawned;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-raceDirection") DirectionOverride = args[i + 1];
            LoadRoute();
        }

        // In the Editor the route file is read from Data/routes each time (so changing "direction" there needs no rebuild);
        // anywhere else, the copy MapBuilder embedded in the scene.
        void LoadRoute()
        {
            string json = embeddedRouteJson;
#if UNITY_EDITOR
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Data", "routes", routeFileName));
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
            Route = RaceRoute.Create(RouteData, DirectionOverride);
            if (LapsOverride > 0) Route.Laps = LapsOverride;
            LoadRecords();
            if (markers == null) markers = new GameObject("RaceMarkers").AddComponent<RaceMarkers>();
            markers.Build(this);
            markers.Refresh(this);
        }

        // ---------------------------------------------------------------- control

        public void StartRace()
        {
            if (!enabled || !car) return;
            LoadRoute(); // picks up a changed direction
            clockStarted = false; Clock = 0f; lapStart = 0f; CompletedLaps = 0; FinalTotal = 0f;
            NewBestLap = NewBestTotal = false;
            lapTimes.Clear(); lapLog.Clear(); missed.Clear(); forgiven.Clear(); penaltyLog.Clear(); NextCheckpoint = 0;
            PenaltyTotal = LapPenalty = 0f; MissedTotal = 0; ActiveShortcut = null;
            if (State == Stage.Free) wasInputEnabled = input.enabled;
            State = Stage.Countdown; CountdownRemaining = 3f;
            PlaceOnGrid();
            input.enabled = false;
            Say("Get ready", 2f);
            if (barriers == null) barriers = new GameObject("RaceBarriers").AddComponent<RaceBarriers>();
            barriers.Build(Route);   // closed roads appear with the race ...
            FenceChunk.RestoreAll(); // Round 7: smashed fences are whole again when a race (re)starts
            Log($"RACE START requested: {Route.Name}, {Route.Direction}, {Route.Laps} laps, lap {Route.LapLength:0} m, {Route.Checkpoints.Count} checkpoints, run-in {Route.RunInLength:0} m, {barriers.Count} road barriers, {Route.Shortcuts.Count} official shortcuts");
            markers.Refresh(this);
        }

        public void EndRace()
        {
            if (State == Stage.Free) return;
            State = Stage.Free;
            input.enabled = true;
            Say("", 0f);
            if (barriers != null) barriers.Clear();   // ... and go away in free driving
            ActiveShortcut = null;
            Log("RACE LEFT");
            markers.Refresh(this);
        }

        void PlaceOnGrid()
        {
            Vector3 g = Route.GridPosition;
            if (respawn) respawn.PlaceAt(g + Vector3.up * 0.3f, Route.GridYaw);
            else { car.Body.position = g + Vector3.up * 0.8f; car.Body.rotation = Quaternion.Euler(0, Route.GridYaw, 0); car.Body.linearVelocity = Vector3.zero; }
            prevPos = car.transform.position;
        }

        void OnRespawned() { prevPos = car.transform.position; } // a reset is a teleport: it must not count as crossing anything
        public void NotifyTeleported() { prevPos = car.transform.position; } // checks that move the car by hand say so, as a reset does

        // How far the car's body reaches sideways across a gate at the moment of crossing (half the car's box, turned to the gate).
        float Reach(RaceGate g)
        {
            Vector3 right = g.Right, cr = car.transform.right, cf = car.transform.forward;
            float hx = carBox ? carBox.size.x * 0.5f * carBox.transform.lossyScale.x : 0.925f, hz = carBox ? carBox.size.z * 0.5f * carBox.transform.lossyScale.z : 1.85f;
            return Mathf.Abs(Vector3.Dot(right, cr)) * hx + Mathf.Abs(Vector3.Dot(right, cf)) * hz;
        }

        // ---------------------------------------------------------------- per frame

        void Update()
        {
            var k = Keyboard.current; var g = Gamepad.current;
            if (Time.timeScale == 0f) return;
            var hud = DeveloperHud.Instance;
            if (hud != null && (hud.MenuOpen || hud.CommentOpen || hud.Capturing)) return; // typing a bug comment must not restart the race
            if ((k != null && k.tKey.wasPressedThisFrame) || (g != null && g.dpad.up.wasPressedThisFrame)) startPressed = true;
            if ((k != null && k.xKey.wasPressedThisFrame) || (g != null && g.dpad.down.wasPressedThisFrame)) endPressed = true;
        }

        void FixedUpdate()
        {
            if (startPressed) { startPressed = false; StartRace(); return; }
            if (endPressed) { endPressed = false; EndRace(); }
            if (State == Stage.Free || !car) return;
            float dt = Time.fixedDeltaTime;
            Vector3 pos = car.transform.position;

            if (State == Stage.Countdown)
            {
                car.Body.linearVelocity = Vector3.zero; car.Body.angularVelocity = Vector3.zero;
                float before = CountdownRemaining;
                CountdownRemaining -= dt;
                if (CountdownRemaining <= 0f)
                {
                    State = Stage.Racing; input.enabled = true; goFlashUntil = Time.time + 1.0f;
                    Say("Cross the start line to start the clock", 6f);
                    Log("GO");
                    markers.Refresh(this);
                }
                prevPos = pos;
                return;
            }

            float t0 = Clock;
            if (clockStarted && State == Stage.Racing) Clock += dt;

            // gates passed during this step, in the order they happened
            var hits = new List<(float f, int id)>();
            if (State == Stage.Racing)
            {
                if (Route.Gate.Crossed(prevPos, pos, Reach(Route.Gate), out float fg)) hits.Add((fg, 0));
                for (int i = 0; i < Route.Checkpoints.Count; i++)
                    if (Route.Checkpoints[i].Crossed(prevPos, pos, Reach(Route.Checkpoints[i]), out float fc)) hits.Add((fc, i + 1));
                for (int i = 0; i < Route.Shortcuts.Count; i++)
                {
                    var sc = Route.Shortcuts[i];
                    if (!EntryPlanesOff && sc.Entry.Crossed(prevPos, pos, Reach(sc.Entry), out float fe)) hits.Add((fe, 1000 + i));
                    if (ActiveShortcut == sc && sc.Exit.Crossed(prevPos, pos, Reach(sc.Exit), out float fx)) hits.Add((fx, 2000 + i));
                    // Round 13: in the shortcut's corridor counts as taking it too (the entry plane can be passed round)
                    else if (ActiveShortcut != sc && clockStarted && sc.LastForgiven >= NextCheckpoint && sc.InCorridor(pos)) hits.Add((1f, 3000 + i));
                }
                hits.Sort((a, b) => a.f.CompareTo(b.f));
                foreach (var h in hits)
                {
                    float at = t0 + h.f * dt;
                    if (h.id == 0) OnGate(at);
                    else if (h.id < 1000) OnCheckpoint(h.id - 1, at);
                    else if (h.id < 2000) OnShortcutEntry(Route.Shortcuts[h.id - 1000], at, "its entry plane");
                    else if (h.id < 3000) OnShortcutExit(Route.Shortcuts[h.id - 2000], at);
                    else OnShortcutEntry(Route.Shortcuts[h.id - 3000], at, "its corridor");
                    if (State != Stage.Racing) break;
                }
            }
            prevPos = pos;
        }

        // ---------------------------------------------------------------- rules

        // A checkpoint that was skipped: Racer's rule, 5 s on the lap and on the total, and the lap goes on counting.
        void Miss(int index, float at)
        {
            missed.Add(index);
            PenaltyTotal += MissPenaltySeconds; LapPenalty += MissPenaltySeconds; MissedTotal++;
            penaltyLog.Add($"Lap {CompletedLaps + 1}  CP{index + 1}  +{MissPenaltySeconds:0} s  missed");
            Log($"PENALTY +{MissPenaltySeconds:0} s: missed CP{index + 1} on lap {CompletedLaps + 1} at {at:0.000} (race penalty now {PenaltyTotal:0} s)");
        }

        void MissMessage(List<int> skipped)
        {
            if (skipped.Count == 0) return;
            Say($"Missed checkpoint{(skipped.Count > 1 ? "s" : "")} {string.Join(", ", skipped)}  +{skipped.Count * MissPenaltySeconds:0} s", 4f);
        }

        void OnCheckpoint(int index, float at)
        {
            if (!clockStarted) return;       // before the first start-line crossing there is no lap to be on
            if (index < NextCheckpoint) return; // already passed (or already missed / forgiven) this lap
            var skipped = new List<int>();
            for (int k = NextCheckpoint; k < index; k++) { Miss(k, at); skipped.Add(k + 1); }
            NextCheckpoint = index + 1;
            Log($"CHECKPOINT {index + 1} at {at:0.000}" + (skipped.Count > 0 ? $"; MISSED {string.Join(",", skipped)}" : ""));
            MissMessage(skipped);
            markers.Refresh(this);
        }

        // Taking an official shortcut (Racer: a Branch entry grants the route's bypass list): the checkpoints it skips are
        // forgiven, with no penalty. Checkpoints before the first forgiven one that are still waiting were really missed.
        void OnShortcutEntry(RaceShortcut sc, float at, string how)
        {
            if (!clockStarted) return;
            if (sc.LastForgiven < NextCheckpoint) return;   // nothing left on this lap for it to forgive
            var skipped = new List<int>();
            for (int k = NextCheckpoint; k < sc.FirstForgiven; k++) { Miss(k, at); skipped.Add(k + 1); }
            var free = new List<int>();
            for (int k = Mathf.Max(NextCheckpoint, sc.FirstForgiven); k <= sc.LastForgiven; k++) { forgiven.Add(k); free.Add(k + 1); }
            NextCheckpoint = sc.LastForgiven + 1;
            ActiveShortcut = sc;
            Log($"SHORTCUT {sc.Id} entered at {at:0.000} ({how}): checkpoints {string.Join(",", free)} forgiven" + (skipped.Count > 0 ? $"; MISSED {string.Join(",", skipped)}" : ""));
            if (skipped.Count > 0) MissMessage(skipped);
            else Say($"Shortcut: {sc.Name}  (checkpoint{(free.Count > 1 ? "s" : "")} {string.Join(", ", free)} skipped, no penalty)", 4f);
            markers.Refresh(this);
        }

        void OnShortcutExit(RaceShortcut sc, float at)
        {
            if (ActiveShortcut != sc) return;
            ActiveShortcut = null;
            Log($"SHORTCUT {sc.Id} left at {at:0.000}");
        }

        void OnGate(float at)
        {
            if (!clockStarted)
            {
                clockStarted = true; Clock = 0f; lapStart = 0f; NextCheckpoint = 0; missed.Clear(); forgiven.Clear();
                PenaltyTotal = LapPenalty = 0f; MissedTotal = 0; penaltyLog.Clear();
                Say("Lap 1 - go!", 3f);
                Log("START LINE crossed: clock started");
                markers.Refresh(this);
                return;
            }
            var skipped = new List<int>();
            for (int k = NextCheckpoint; k < Route.Checkpoints.Count; k++) { Miss(k, at); skipped.Add(k + 1); }
            float raw = at - lapStart, lap = raw + LapPenalty;
            int lapNo = CompletedLaps + 1;
            lapTimes.Add(lap); CompletedLaps++;
            bool best = BestLap <= 0f || lap < BestLap;
            if (best) { BestLap = lap; NewBestLap = true; SaveRecords(); }
            string pen = LapPenalty > 0f ? $"  (includes +{LapPenalty:0} s penalty, {MissedText()})" : "";
            string fg = forgiven.Count > 0 ? $"  (shortcut: CP{string.Join(",", forgiven.ConvertAll(x => x + 1))} forgiven)" : "";
            lapLog.Add($"Lap {lapNo}: {Fmt(lap)}{pen}{fg}" + (best ? "  best lap" : ""));
            if (skipped.Count > 0) Say($"Missed checkpoint{(skipped.Count > 1 ? "s" : "")} {string.Join(", ", skipped)}  +{skipped.Count * MissPenaltySeconds:0} s     Lap {lapNo} {Fmt(lap)}", 5f);
            else Say($"Lap {lapNo} {Fmt(lap)}" + (best ? "  best lap!" : ""), 4f);
            Log($"LAP {lapNo} COUNTED {lap:0.000} (clock {raw:0.000} + penalty {LapPenalty:0.0})" + (missed.Count > 0 ? $"; {MissedText()}" : "") + (forgiven.Count > 0 ? $"; forgiven {string.Join(",", forgiven.ConvertAll(x => "CP" + (x + 1)))}" : ""));
            if (CompletedLaps >= Route.Laps) { Finish(at); return; }
            lapStart = at; NextCheckpoint = 0; LapPenalty = 0f; missed.Clear(); forgiven.Clear(); ActiveShortcut = null;
            markers.Refresh(this);
        }

        string MissedText() { var names = new List<string>(); foreach (int k in missed) names.Add("CP" + (k + 1)); return "missed " + string.Join(", ", names); }

        void Finish(float at)
        {
            State = Stage.Finished; FinalTotal = at + PenaltyTotal;   // the clock plus every penalty
            at = FinalTotal;
            bool bestTotal = BestTotal <= 0f || at < BestTotal;
            if (bestTotal) { BestTotal = at; NewBestTotal = true; }
            SaveRecords();
            Say($"Finished! Total {Fmt(at)}", 10f);
            Log($"FINISH total {at:0.000} (clock {at - PenaltyTotal:0.000} + penalties {PenaltyTotal:0.0}); laps {string.Join(" ", lapTimes.ConvertAll(x => x.ToString("0.000")))}");
            markers.Refresh(this);
        }

        // ---------------------------------------------------------------- records (best lap, best total)

        string RecordKey => $"{Route.Name}|{Route.Direction}|{Route.Laps}";

        public static string RecordsPath()
        {
            if (!string.IsNullOrEmpty(RecordsPathOverride)) return RecordsPathOverride;
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
        void Log(string text) { Logged?.Invoke(text); if (!Application.isBatchMode) Debug.Log("[Race] " + text); }

        public static string Fmt(float seconds)
        {
            if (seconds <= 0f) return "--:--.---";
            int m = (int)(seconds / 60f); float s = seconds - m * 60f;
            return $"{m}:{s:00.000}";
        }

        // The next gate the driver should aim for, and how far it is; used by the HUD arrow.
        public RaceGate Target => State == Stage.Free || Route == null ? null
            : !clockStarted ? Route.Gate : NextCheckpoint < Route.Checkpoints.Count ? Route.Checkpoints[NextCheckpoint] : Route.Gate;

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
        static string StripTags(string s) => System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", "");

        void OnGUI()
        {
            if (Route == null || !car) return;
            Styles();
            float s = Mathf.Max(1f, Screen.height / 800f);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s, h = Screen.height / s;

            if (State == Stage.Free) DrawFree(w, h);
            else
            {
                DrawTimers(w, h);
                DrawGuide(w, h);
                DrawCountdown(w, h);
                if (State == Stage.Finished) DrawResults(w, h);
                if (Message.Length > 0 && Time.time < messageUntil && State != Stage.Finished)
                    Shadowed(new Rect(0, h * 0.74f, w, 40), $"<b>{Message}</b>", new GUIStyle(small) { fontSize = 26, alignment = TextAnchor.MiddleCenter, richText = true, normal = { textColor = Color.white } });
            }
            GUI.matrix = old;
        }

        void DrawFree(float w, float h)
        {
            string best = BestLap > 0f ? $"   best lap {Fmt(BestLap)}   best total {Fmt(BestTotal)}" : "";
            Shadowed(new Rect(14, 10, 900, 28), $"<b>T</b> start race: {Route.Name}, {Route.Laps} laps, {Route.Direction}{best}", small);
        }

        void DrawTimers(float w, float h)
        {
            var r = new Rect(14, 10, 330, 196);
            GUI.Box(r, GUIContent.none, panel);
            int lap = Mathf.Min(CompletedLaps + 1, LapsInRace);
            string lapText = State == Stage.Finished ? "Finished" : $"Lap {lap} / {LapsInRace}";
            Shadowed(new Rect(r.x + 12, r.y + 4, r.width, 40), lapText, big);
            string lapT = State == Stage.Finished && lapTimes.Count > 0 ? Fmt(lapTimes[lapTimes.Count - 1]) : clockStarted ? Fmt(LapClock) : "--:--.---", totT = clockStarted ? Fmt(State == Stage.Finished ? FinalTotal : TotalClock) : "--:--.---";
            Shadowed(new Rect(r.x + 12, r.y + 46, r.width, 26), $"Lap    <b>{lapT}</b>", small);
            Shadowed(new Rect(r.x + 12, r.y + 70, r.width, 26), $"Total  <b>{totT}</b>", small);
            // penalties: shown as soon as one is given (they are already inside the lap and total times above)
            string penText = PenaltyTotal > 0f ? $"<color=#ffb040>Penalty  +{PenaltyTotal:0} s  ({MissedTotal} missed)</color>" : "Penalty  none";
            Shadowed(new Rect(r.x + 12, r.y + 94, r.width, 26), penText, small);
            Shadowed(new Rect(r.x + 12, r.y + 122, r.width, 26), $"Best lap    {Fmt(BestLap)}", small);
            Shadowed(new Rect(r.x + 12, r.y + 146, r.width, 26), $"Best total  {Fmt(BestTotal)}", small);
            Shadowed(new Rect(r.x + 12, r.y + 170, r.width, 22), Hint, small);
        }

        // next-gate arrow and distance, top centre
        void DrawGuide(float w, float h)
        {
            var t = Target;
            if (t == null || State == Stage.Finished) return;
            Vector3 to = t.Position - car.transform.position; to.y = 0f;
            Vector3 fwd = car.transform.forward; fwd.y = 0f;
            float angle = Vector3.SignedAngle(fwd, to, Vector3.up);
            var c = new Vector2(w / 2f, 62f);
            var save = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, c);
            GUI.color = t.Id == 0 ? Color.white : new Color(0.45f, 1f, 0.3f);
            GUI.DrawTexture(new Rect(c.x - 28, c.y - 28, 56, 56), arrow);
            GUI.color = Color.white;
            GUI.matrix = save;
            string name = t.Id == 0 ? (clockStarted ? "FINISH LINE" : "START LINE") : $"CHECKPOINT {t.Id} / {Route.Checkpoints.Count}";
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
            var r = new Rect((w - 640) / 2f, h * 0.16f, 640, 390);
            GUI.Box(r, GUIContent.none, panel);
            Shadowed(new Rect(r.x + 20, r.y + 10, r.width, 44), "FINISH", big);
            Shadowed(new Rect(r.x + 20, r.y + 60, r.width, 30), $"Total time   <b>{Fmt(FinalTotal)}</b>" + (NewBestTotal ? "   NEW BEST" : "") + (PenaltyTotal > 0f ? $"   (clock {Fmt(FinalTotal - PenaltyTotal)} + {PenaltyTotal:0} s penalties)" : ""), small);
            float best = 0f; foreach (var l in lapTimes) if (best <= 0f || l < best) best = l;
            Shadowed(new Rect(r.x + 20, r.y + 88, r.width, 30), $"Best lap this race   <b>{Fmt(best)}</b>" + (NewBestLap ? "   NEW BEST" : ""), small);
            Shadowed(new Rect(r.x + 20, r.y + 116, r.width, 30), $"Record: best lap {Fmt(BestLap)}   best total {Fmt(BestTotal)}", small);
            float y = r.y + 154;
            Shadowed(new Rect(r.x + 20, y, r.width, 26), PenaltyTotal > 0f ? $"<color=#ffb040>Penalties  +{PenaltyTotal:0} s  ({MissedTotal} missed checkpoint{(MissedTotal == 1 ? "" : "s")}, {MissPenaltySeconds:0} s each)</color>" : "Penalties  none", small); y += 28;
            foreach (var line in lapLog) { Shadowed(new Rect(r.x + 20, y, r.width - 30, 26), line, small); y += 24; }
            Shadowed(new Rect(r.x + 20, r.yMax - 34, r.width, 26), "T  race again        X  free driving", small);
        }
    }
}
