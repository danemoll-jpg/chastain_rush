using System;
using System.Collections;
using System.IO;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // On-screen coordinates and bug capture, brought over from Racer's DeveloperLocationHud (Round 4, item 4).
    // Same keys as Racer:  F3 show / hide the coordinates panel,  F4 capture a bug (screenshot, then a comment box;
    // Enter saves, Shift+Enter is a new line, Esc cancels),  F6 menu (export the session as a ZIP, open the report
    // folder, start a new session).  Gamepad: the right stick's click captures (Round 20; Select before), A saves, B cancels, Start opens the menu.
    //
    // Rewritten, not copied: Racer's version is built on RaceDirector / RaceFlow, Campaign, MenuInput and MenuGlyph,
    // WorldLook, GetawayChase, VehicleConfiguration and uGUI (not in this project). What was cut: the race / lap /
    // checkpoint / course lines, the debug-fly camera and its "records disabled" flag, the campaign cheat buttons, the
    // Getaway log, and all of the uGUI canvas (this draws with IMGUI, like MapCredits). It starts by itself in Play
    // mode (RuntimeInitializeOnLoadMethod), so no scene change is needed, and not in batch mode.
    // Reports: local/debug_reports/<session>/ (git-ignored; in a player build, persistentDataPath/DebugReports).
    public sealed class DeveloperHud : MonoBehaviour
    {
        public static DeveloperHud Instance { get; private set; }
        static DebugReportSession session;
        static bool resumeChecked;

        public bool Visible { get; set; } = CheckRun.Active; // Round 17: hidden at start in the game Dan plays (F3 / [ or the debug menu shows it); a check run keeps it on
        public bool MenuOpen { get; private set; }
        public bool CommentOpen { get; private set; }
        public bool Capturing { get; private set; }
        public string LastExport { get; private set; }
        public string Status { get; private set; } = "";
        public string SessionLine => SessionText;   // Round 17: shown on the debug menu page (GameMenus)
        public string CommentText { get => comment; set => comment = value; } // for the scripted check

        ArcadeVehicle car;
        VehicleInput input;
        BumpLogger bumps;
        DebugReportSession.Bug pending;
        string comment = "";
        bool held, oldInput, oldAudio, oldCursorVisible;
        float oldScale;
        CursorLockMode oldLock;
        GUIStyle label, box, button, title, area;
        Texture2D back;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isBatchMode || Instance != null || FindAnyObjectByType<ArcadeVehicle>() == null) return;
            var go = new GameObject("DeveloperHud");
            go.AddComponent<BumpLogger>();
            go.AddComponent<DeveloperHud>();
        }

        // Where bug reports go: local/debug_reports in the project (git-ignored); a built player has no project folder.
        public static string ReportRoot()
        {
            if (CheckRun.Active) return Path.Combine(CheckRun.Dir, "debug_reports");   // Round 15, item 5: a check never writes Dan's own reports or bump log
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "local", "debug_reports"));
#else
            return Path.Combine(Application.persistentDataPath, "DebugReports");
#endif
        }

        public static DebugReportSession Session
        {
            get
            {
                if (!resumeChecked && session == null)
                {
                    resumeChecked = true;
                    try { session = DebugReportSession.ResumeLatest(ReportRoot()); }
                    catch (Exception e) { Debug.LogWarning("Debug session resume skipped: " + e.Message); }
                }
                return session;
            }
        }

        string SessionText => Session == null ? "Session: NEW / Reports 0 - next F4 starts BUG-001"
            : "Session: " + session.Id + " " + (session.Closed ? "CLOSED" : "OPEN") + " / Reports " + session.Count;

        // Round 13: "Street Classic (original)", the name and Racer's profile id
        string VehicleName() { var c = car ? car.GetComponent<VehicleConfiguration>() : null; return c ? $"{c.Profile.Name} ({c.profileId})" : "(no vehicle profile)"; }

        void Awake()
        {
            Instance = this;
            car = FindAnyObjectByType<ArcadeVehicle>();
            input = car ? car.GetComponent<VehicleInput>() : null;
            bumps = GetComponent<BumpLogger>();
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Release();
            Instance = null;
        }

        // ---------------------------------------------------------------- input

        void Update()
        {
            if (!car) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            if (CommentOpen)
            {
                if (g != null && g.buttonSouth.wasPressedThisFrame) SaveComment();
                if (g != null && g.buttonEast.wasPressedThisFrame) CancelComment();
                return;
            }
            if (Capturing) return;
            // Round 9: a second key for each that never needs Fn on Dan's keyboard: [ panel, ] capture, \ menu
            if (k != null && (k.f3Key.wasPressedThisFrame || k.leftBracketKey.wasPressedThisFrame)) { Visible = !Visible; if (!Visible && MenuOpen) CloseMenu(); }
            // Round 17: capture works with the panel hidden too (the panel is drawn on the captured frame either way)
            // Round 20 (item 2): the controller's View / Select is the map's now (as in Woodstock Rush); bug capture took the right
            // stick's click (free while driving); not while the map is open (its own center button)
            if (!GameMenus.Open && !WorldMap.OwnsInput && ((k != null && (k.f4Key.wasPressedThisFrame || k.rightBracketKey.wasPressedThisFrame)) || (g != null && g.rightStickButton.wasPressedThisFrame))) CaptureBug();
            bool f6 = k != null && (k.f6Key.wasPressedThisFrame || k.backslashKey.wasPressedThisFrame);
            // Round 17: with the game menus (the game Dan plays) F6 / \ opens their controller-driven debug page; the IMGUI menu
            // below is left for check runs, which have no game menus. Start is the game menu's (Round 16).
            if (f6 && GameMenus.Instance != null) { GameMenus.Instance.OpenDebug(); return; }
            if (f6 || (g != null && g.startButton.wasPressedThisFrame && GameMenus.Instance == null)) { if (MenuOpen) CloseMenu(); else OpenMenu(); }
            if (MenuOpen && ((k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame))) CloseMenu();
        }

        // Pause the game and give the mouse back while a dialog is open.
        void Hold()
        {
            if (held) return;
            held = true;
            oldScale = Time.timeScale; oldAudio = AudioListener.pause; oldInput = input && input.enabled;
            oldCursorVisible = Cursor.visible; oldLock = Cursor.lockState;
            Time.timeScale = 0f; AudioListener.pause = true; if (input) input.enabled = false;
            Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        }

        void Release()
        {
            if (!held) return;
            held = false;
            Time.timeScale = oldScale; AudioListener.pause = oldAudio; if (input) input.enabled = oldInput;
            Cursor.visible = oldCursorVisible; Cursor.lockState = oldLock;
        }

        public void OpenMenu() { if (Capturing || CommentOpen) return; MenuOpen = true; Hold(); }
        public void CloseMenu() { MenuOpen = false; if (!CommentOpen) Release(); }

        // ---------------------------------------------------------------- capture

        void EnsureSession()
        {
            if (Session != null && !session.Closed) return;
            session = new DebugReportSession(ReportRoot());
        }

        public void CaptureBug()
        {
            if (!car || Capturing || CommentOpen) return;
            try { EnsureSession(); } catch (Exception e) { Status = "Debug report: " + e.Message; return; }
            var cam = Camera.main;
            var t = cam ? cam.transform : car.transform;
            string roadName = null;
            float roadDistance = 0f;
            RoadLookup.Instance?.Nearest(car.transform.position, out roadName, out roadDistance, out _);
            pending = new DebugReportSession.Bug
            {
                id = session.NextId, timestamp = DateTimeOffset.Now.ToString("o"),
                position = t.position, rotation = t.eulerAngles, heading = car.transform.eulerAngles.y, vehiclePosition = car.transform.position,
                road = roadName, roadDistance = roadDistance, groundedWheels = car.GroundedWheels,
                pipePlace = PipeWarp.Instance ? PipeWarp.Instance.NearestUnderground(car.transform.position) : null,   // Round 25 (B1)
                speedMps = Mathf.Abs(car.ForwardSpeed), verticalSpeedMps = car.Body.linearVelocity.y,
                version = Application.version, buildGuid = Application.buildGUID,
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, viewpoint = "Chase camera",
                vehicle = VehicleName(),   // Round 13
            };
            pending.screenshot = "Screenshots/" + pending.id + ".png";
            MenuOpen = false;
            Capturing = true;
            StartCoroutine(CaptureView());
        }

        IEnumerator CaptureView()
        {
            yield return new WaitForEndOfFrame(); // the finished frame, with the coordinates panel on it, and no dialog
            Texture2D image = null;
            try
            {
                // (ScreenCapture is not among this project's built-in modules, so read the finished frame directly)
                image = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                image.Apply();
                if (!image) throw new IOException("Could not read the rendered view.");
                File.WriteAllBytes(Path.Combine(session.DirectoryPath, pending.screenshot), image.EncodeToPNG());
                Capturing = false;
                CommentOpen = true;
                comment = "";
                Hold();
            }
            catch (Exception e) { Capturing = false; pending = null; Status = "Debug report: " + e.Message; Release(); }
            finally { if (image) Destroy(image); }
        }

        public void SaveComment()
        {
            if (!CommentOpen || pending == null) return;
            try
            {
                pending.comment = comment ?? "";
                session.Save(pending);
                Status = pending.id + " saved / " + session.Count + " reports";
                pending = null;
                CommentOpen = false;
                Release();
            }
            catch (Exception e) { Status = "Save failed (your screenshot and comment are kept): " + e.Message; }
        }

        public void CancelComment()
        {
            if (!CommentOpen) return;
            try { if (pending != null && !session.Data.bugs.Contains(pending)) File.Delete(Path.Combine(session.DirectoryPath, pending.screenshot)); }
            catch (Exception e) { Status = e.Message; }
            pending = null;
            CommentOpen = false;
            Release();
        }

        // Round 17: for the debug menu page in GameMenus
        public void ExportReport() => Export();
        public void OpenReportFolder() => OpenFolder();
        public void StartNewSession() => NewSession();

        void Export()
        {
            if (Session == null || session.Closed || session.Count == 0) { Status = "Nothing to export yet - capture a bug with F4 first."; return; }
            try { LastExport = session.Export(); Status = "Session CLOSED. ZIP: " + LastExport; }
            catch (Exception e) { Status = "Export failed: " + e.Message; }
        }

        void OpenFolder()
        {
            string dir = Session != null ? session.DirectoryPath : ReportRoot();
            try
            {
                Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir) { UseShellExecute = true });
                Status = "Report folder: " + dir;
            }
            catch (Exception e) { Status = "Could not open the folder: " + e.Message; }
        }

        void NewSession()
        {
            try { Session?.Close(); session = null; resumeChecked = true; Status = "Session closed; history kept. The next F4 starts a new session at BUG-001."; }
            catch (Exception e) { Status = e.Message; }
        }

        // ---------------------------------------------------------------- drawing

        void Styles()
        {
            if (label != null) return;
            back = new Texture2D(1, 1);
            back.SetPixel(0, 0, new Color(0.025f, 0.055f, 0.07f, 0.92f));
            back.Apply();
            label = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, normal = { textColor = Color.white } };
            title = new GUIStyle(label) { fontSize = 20, normal = { textColor = new Color(0.3f, 0.95f, 0.81f) } };
            box = new GUIStyle(GUI.skin.box) { normal = { background = back }, padding = new RectOffset(12, 12, 10, 10) };
            button = new GUIStyle(GUI.skin.button) { fontSize = 16 };
            area = new GUIStyle(GUI.skin.textArea) { fontSize = 16, wordWrap = true };
        }

        void OnGUI()
        {
            if (!car) return;
            Styles();
            float s = Mathf.Max(1f, Screen.height / 800f);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s, h = Screen.height / s;
            if (((Visible && !GameMenus.Open && !WorldMap.IsOpen) || Capturing) && !MenuOpen && !CommentOpen) DrawPanel(w, h);   // Round 17: not over a game menu (Round 20: nor the map); always on a captured frame
            if (MenuOpen) DrawMenu(w, h);
            if (CommentOpen) DrawComment(w, h);
            GUI.matrix = old;
        }

        void DrawPanel(float w, float h)
        {
            var p = car.transform.position;
            string road = "no road within 40 m";
            if (RoadLookup.Instance != null && RoadLookup.Instance.Nearest(p, out string name, out float d, out float half))
                road = $"{name}, {d:0.0} m from center ({(d <= half + 1.5f ? "on road" : "off road")})";
            if (PipeWarp.Instance && PipeWarp.Instance.NearestUnderground(p) is string place) road = "pipes: " + place;   // Round 25 (B1)
            float mps = Mathf.Abs(car.ForwardSpeed);
            string text = $"DEBUG\nX {p.x:F2}   Y {p.y:F2}   Z {p.z:F2}\nHeading {car.transform.eulerAngles.y:F1}°   {mps * 2.23694f:F1} mph ({mps:F1} m/s)\n" +
                          $"{road}\n{VehicleName()}\nWheels on ground {car.GroundedWheels}   vertical {car.Body.linearVelocity.y:+0.0;-0.0} m/s\n" +
                          (Racer.WorldLook.Current ? Racer.WorldLook.Current.Conditions + "   grip " + car.GripScale.ToString("P0") + "\n" : "") +   // Round 35 (Part C): the conditions and the weather's grip
                          (RaceController.Instance?.DebugLine is string raceLine ? raceLine + "\n" : "") + // Round 5: race state shows in bug screenshots
                          $"{SessionText}\n" +
                          (bumps != null ? $"Bump log: {bumps.Events} events" + (bumps.Events > 0 ? "\nLast: " + Trim(bumps.LastLine, 110) : "") + "\n" : "") +
                          (string.IsNullOrEmpty(Status) ? "" : Status + "\n") +
                          "F3 or [ hide / F4, ] or R3 capture bug / F6, \\ or Start: debug menu / F2 or P fps / M or View: map";
            float height = label.CalcHeight(new GUIContent(text), 430f) + 14f;
            var r = new Rect(w - 450f - 12f, h - height - 12f, 450f, height);
            GUI.Box(r, GUIContent.none, box);
            GUI.Label(new Rect(r.x + 10f, r.y + 6f, r.width - 20f, r.height - 12f), text, label);
        }

        static string Trim(string line, int max) => line.Length <= max ? line : line.Substring(0, max) + "...";

        void DrawMenu(float w, float h)
        {
            GUI.Box(new Rect(0, 0, w, h), GUIContent.none, box);
            var r = new Rect((w - 560f) / 2f, (h - 520f) / 2f, 560f, 520f);
            GUILayout.BeginArea(r, box);
            GUILayout.Label("DEBUG / INSPECTION", title);
            GUILayout.Label(SessionText, label);
            GUILayout.Space(6);
            if (GUILayout.Button("Resume   (F6 / \\ / Esc)", button, GUILayout.Height(36))) CloseMenu();
            if (GUILayout.Button("Capture Bug   (F4 / ])", button, GUILayout.Height(36))) { CloseMenuForCapture(); }
            if (GUILayout.Button("Export Bug Report ZIP", button, GUILayout.Height(36))) Export();
            if (GUILayout.Button("Open Debug Report Folder", button, GUILayout.Height(36))) OpenFolder();
            if (GUILayout.Button("START NEW DEBUG SESSION", button, GUILayout.Height(36))) NewSession();
            if (GUILayout.Button("Hide Debug HUD   (F3 / [)", button, GUILayout.Height(36))) { Visible = false; CloseMenu(); }
            GUILayout.Space(6);
            GUILayout.Label("Reports: " + ReportRoot() + "\nBump log: " + Path.GetFullPath(BumpLogger.LogPath), label);
            if (!string.IsNullOrEmpty(Status)) GUILayout.Label(Status, label);
            GUILayout.EndArea();
        }

        void CloseMenuForCapture() { MenuOpen = false; Release(); CaptureBug(); }

        void DrawComment(float w, float h)
        {
            GUI.Box(new Rect(0, 0, w, h), GUIContent.none, box);
            // Enter saves, Shift+Enter is a new line: handle the key before the text area sees it
            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && !e.shift) { e.Use(); SaveComment(); return; }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { e.Use(); CancelComment(); return; }
            var r = new Rect((w - 640f) / 2f, (h - 440f) / 2f, 640f, 440f);
            GUILayout.BeginArea(r, box);
            GUILayout.Label("BUG CAPTURED", title);
            if (pending != null)
                GUILayout.Label($"{pending.id}   {(string.IsNullOrEmpty(pending.road) ? "no road within 40 m" : pending.road)}\ncar {pending.vehiclePosition.x:F1}, {pending.vehiclePosition.y:F1}, {pending.vehiclePosition.z:F1}", label);
            GUILayout.Label("Describe the problem:", label);
            GUI.SetNextControlName("comment");
            comment = GUILayout.TextArea(comment ?? "", 4000, area, GUILayout.Height(180));
            if (Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != "comment") GUI.FocusControl("comment");
            GUILayout.Space(6);
            if (GUILayout.Button("Save   Enter", button, GUILayout.Height(36))) SaveComment();
            if (GUILayout.Button("Cancel   Esc / B", button, GUILayout.Height(36))) CancelComment();
            GUILayout.Label("Shift+Enter: new line. The screenshot is already saved.", label);
            if (!string.IsNullOrEmpty(Status)) GUILayout.Label(Status, label);
            GUILayout.EndArea();
        }
    }
}
