using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 43 (Part D): Racer's Trailer Mode (0.77 Parts A and B, 0.79 Part H), ported. For filming footage and screenshots (also a photo
    // mode), in Free Roam and in races. F8 or the pause menu's TRAILER MODE page turns it on and off. It changes nothing about gameplay,
    // physics or AI. A race run in Trailer Mode does not count for records (Racer's rule). While it is on: nothing is drawn on screen
    // (H / L3 brings the HUD and the controls panel back, G the checkpoint arches and course arrows), the cameras of CameraViews.Trailer.cs
    // (Chase, Orbit, Side, Front, Fixed, Flyover, Free, Auto, First person), slow motion, a screenshot key, and in Free Roam the time of
    // day, clock, weather, moon and lightning on demand: none of it is saved; turning the mode off leaves the Free Roam clock and weather
    // as they were. The controls are listed on the on-screen panel (TrailerMode.Panel.cs) and the pause menu page.
    // Changes from Racer:
    //   - RaceFlow is this game's RaceController and GameMenus: "driving" is any time no menu, the map or the debug menu is open (Free Roam
    //     or a race); the race's records are kept out with RaceController.NoRecords.
    //   - the HUD here is IMGUI: every HUD's OnGUI returns while HudHidden (the race HUD, trick and activity HUDs, police, the debug panel,
    //     the frame-rate counter, the pipe express) and the HUD canvases (minimap, Free Roam clock, radio banner) are switched off. The
    //     OpenStreetMap credit stays (it must be visible); screenshots render the camera alone, so nothing of it is in them.
    //   - keys this game already uses do the Trailer Mode thing while it is on: P (frame-rate counter: F2 still), , and . (the radio:
    //     the clock here), the D-pad (radio), B on a controller (minimap), R3 (bug capture: F4 / ] still), LB (the fist wave: F still).
    //   - screenshots go to Screenshots beside DebugReports (a check run: the check's own folder), named ChastainRush_<time>.png.
    //   - Racer's TrailerRecorder (an Editor-only video recorder, never in a release build), the waypoint beacon (none here) and the
    //     split-screen exclusion's second view are left behind; not in split screen (as Racer).
    [DefaultExecutionOrder(900)]
    public sealed partial class TrailerMode : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public static TrailerMode Instance { get; private set; }
        // every HUD drawn with OnGUI asks this (true: draw nothing)
        public static bool HudHidden => CleanScreen || LoadingScreen.Holding || StartupTitle.Active;   // Round 51: and under the loading screen and the title (IMGUI draws over every canvas)
        static bool CleanScreen => Active && Instance && !Instance.HudShown && !GameMenus.Open && !WorldMap.OwnsInput;
        public bool HudShown { get; private set; }
        public bool GuidesShown { get; private set; }
        public bool HalfSpeed { get; private set; }
        public float Speed { get; private set; } = 1;
        public string LastScreenshot { get; private set; }
        public int ScreenshotsTaken => screenshotsTaken;
        public static string ScreenshotFolder => CheckRun.Active ? Path.Combine(CheckRun.Dir, "Screenshots") : Path.Combine(Application.persistentDataPath, "Screenshots");
        float baseFixed, volumeBefore = -1f; bool slowed, cleaned, freeHeld; bool screenshotRequested; int screenshotsTaken;
        bool slowHeldByTest;
        readonly List<Renderer> guides = new List<Renderer>(); bool guidesHidden;
        readonly List<Canvas> hudCanvases = new List<Canvas>(); float nextCanvasLook;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetProcess() { Active = false; Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance || !FindAnyObjectByType<ArcadeVehicle>()) return;   // the world scene
            var go = new GameObject("Trailer Mode"); var mode = go.AddComponent<TrailerMode>(); Instance = mode;
            mode.baseFixed = Time.fixedDeltaTime; mode.BuildPanel();
        }
        void OnDestroy() { if (Instance == this) { Restore(); Active = false; Instance = null; } }
        public void Toggle() { if (Active) End(); else Begin(); }
        public void Begin()
        {
            if (Active || SplitScreen.Active) return;   // Racer 0.90 Part D: not in split screen
            Active = true; baseFixed = Time.fixedDeltaTime; Began();
        }
        void Began()
        {
            HudShown = false; GuidesShown = false; HalfSpeed = false; PanelShown = true; PanelFocused = false;
            Views?.TrailerStarted();
            if (ConditionsAvailable) Conditions();
            WinnerShot.Skip();
        }
        public void End()
        {
            if (!Active) return;
            Active = false; Restore(); Views?.TrailerEnded();
        }
        // Everything Trailer Mode changed goes back: speed, sound level, screen, guides, vehicle controls, conditions.
        void Restore()
        {
            if (slowed) { slowed = false; Time.fixedDeltaTime = baseFixed; if (!GameMenus.Open && !WorldMap.OwnsInput) Time.timeScale = 1; if (volumeBefore >= 0f) AudioListener.volume = volumeBefore; volumeBefore = -1f; }
            Speed = 1; HalfSpeed = false;
            if (cleaned) { cleaned = false; foreach (var c in hudCanvases) if (c) c.enabled = true; }
            ShowGuides(true);
            if (freeHeld) { freeHeld = false; ReleaseVehicle(); }
            if (WorldLook.Current) WorldLook.Current.Trailer = null;
            HidePanel();
        }
        static CameraViews Views => Camera.main ? Camera.main.GetComponent<CameraViews>() : null;
        static RaceController Race => RaceController.Instance;
        static ArcadeVehicle Car => Race ? Race.PlayerCar : FindAnyObjectByType<ArcadeVehicle>();
        public static bool FreeRoam => !Race || Race.State == RaceController.Stage.Free;
        static bool MenuVisible => GameMenus.Open;
        static bool Driving => !GameMenus.Open && !WorldMap.OwnsInput;

        void Update()
        {
            var k = Keyboard.current; var g = Gamepad.current;
            if (k?.f8Key.wasPressedThisFrame == true && !GameMenus.Open && !WorldMap.OwnsInput) Toggle();
            if (!Active) return;
            if (SplitScreen.Active) { End(); return; }
            // Free Roam: the saved clock stands still from the moment the mode is on (the trailer clock carries on from the same hour)
            if (ConditionsAvailable && WorldLook.Current.Trailer == null) Conditions();
            // a race in Trailer Mode does not count (Racer: the same rule as Debug movement)
            if (Race && (Race.State == RaceController.Stage.Racing || Race.State == RaceController.Stage.Countdown)) Race.MarkNoRecords("TRAILER MODE");
            var views = Views;
            bool controls = Driving && !MenuInput.Blocked;
            // controller B: the panel's focus (a B that closed a menu, menu visible last frame, is ignored)
            if (g != null && g.buttonEast.wasPressedThisFrame && controls && !menuLastFrame) FocusPanel(!PanelFocused);
            float slowTarget = HalfSpeed ? .5f : 1;
            if (controls && views)
            {
                if (k != null)
                {
                    for (int i = 0; i < 9; i++) if (k[Key.Digit1 + i].wasPressedThisFrame) views.SelectTrailerCamera((CameraViews.Shot)i);
                    if (k.tabKey.wasPressedThisFrame) views.CameraAction();
                    if (k.hKey.wasPressedThisFrame) ToggleScreen();
                    if (k.gKey.wasPressedThisFrame) GuidesShown = !GuidesShown;
                    if (k.xKey.wasPressedThisFrame) HalfSpeed = !HalfSpeed;
                    if (k.pKey.wasPressedThisFrame || k.f12Key.wasPressedThisFrame) Screenshot();
                    if (k.f1Key.wasPressedThisFrame) TogglePanel();
                    if (k.zKey.isPressed) slowTarget = .25f;
                    if (FreeRoam)
                    {
                        if (k.tKey.wasPressedThisFrame) CycleTimeOfDay();
                        if (k.commaKey.wasPressedThisFrame) NudgeClock(-1);
                        if (k.periodKey.wasPressedThisFrame) NudgeClock(1);
                        if (k.kKey.wasPressedThisFrame) ToggleClock();
                        if (k.bKey.wasPressedThisFrame) CycleWeather();
                        if (k.oKey.wasPressedThisFrame) CycleMoon();
                        if (k.lKey.wasPressedThisFrame) LightningNow();
                    }
                }
                if (g != null)
                {
                    int shot = (int)views.TrailerCamera;
                    // while the panel has the controller's focus, the D-pad and A work the panel instead
                    if (!PanelFocused && g.dpad.right.wasPressedThisFrame) views.SelectTrailerCamera((CameraViews.Shot)((shot + 1) % 9));
                    if (!PanelFocused && g.dpad.left.wasPressedThisFrame) views.SelectTrailerCamera((CameraViews.Shot)((shot + 8) % 9));
                    if (!PanelFocused && g.buttonSouth.wasPressedThisFrame) views.CameraAction();
                    if (g.rightShoulder.wasPressedThisFrame) HalfSpeed = !HalfSpeed;
                    if (g.leftShoulder.isPressed) slowTarget = .25f;
                    if (g.rightStickButton.wasPressedThisFrame) Screenshot();
                    if (g.leftStickButton.wasPressedThisFrame) ToggleScreen();
                }
                bool free = views.TrailerCamera == CameraViews.Shot.Free;
                float dt = Time.unscaledDeltaTime;
                float fov = ((k?.equalsKey.isPressed == true ? 1 : 0) - (k?.minusKey.isPressed == true ? 1 : 0) + (!PanelFocused && g?.dpad.down.isPressed == true ? 1 : 0) - (!PanelFocused && g?.dpad.up.isPressed == true ? 1 : 0)) * 30 * dt;
                float far = ((k?.pageDownKey.isPressed == true ? 1 : 0) - (k?.pageUpKey.isPressed == true ? 1 : 0)) * .8f * dt;
                float up = ((k?.homeKey.isPressed == true ? 1 : 0) - (k?.endKey.isPressed == true ? 1 : 0)) * 3 * dt;
                if (g != null && !free) { var rs = g.rightStick.ReadValue(); if (rs.magnitude > .2f) { far += rs.x * .8f * dt; up += rs.y * 3 * dt; } }
                if (fov != 0 || far != 0 || up != 0) views.Adjust(fov, far, up);
                views.AutoHold = k?.leftShiftKey.isPressed == true && !free || g?.buttonWest.isPressed == true || TestHold;
                FreeCamera(views, free, k, g);
            }
            else if (views) { views.FreeMove = Vector3.zero; views.FreeLook = Vector2.zero; views.AutoHold = false; }
            if (slowHeldByTest) slowTarget = .25f;
            Slow(slowTarget);
        }
        // Free camera: WASD / left stick move, Q-E / triggers down-up, right mouse button or right stick look, Shift fast, Ctrl precise.
        // The vehicle's own controls are off meanwhile (it rolls to a stop) and come back afterwards.
        void FreeCamera(CameraViews views, bool free, Keyboard k, Gamepad g)
        {
            if (!free) { views.FreeMove = Vector3.zero; views.FreeLook = Vector2.zero; if (freeHeld) { freeHeld = false; ReleaseVehicle(); } return; }
            var input = Car ? Car.GetComponent<VehicleInput>() : null; if (input && input.enabled) input.enabled = false; freeHeld = true;
            float x = (k?.dKey.isPressed == true ? 1 : 0) - (k?.aKey.isPressed == true ? 1 : 0), z = (k?.wKey.isPressed == true ? 1 : 0) - (k?.sKey.isPressed == true ? 1 : 0);
            float y = (k?.eKey.isPressed == true ? 1 : 0) - (k?.qKey.isPressed == true ? 1 : 0);
            var stick = g?.leftStick.ReadValue() ?? Vector2.zero; x += stick.x; z += stick.y; y += (g?.rightTrigger.ReadValue() ?? 0) - (g?.leftTrigger.ReadValue() ?? 0);
            Vector2 look = (g?.rightStick.ReadValue() ?? Vector2.zero) * (100 * Time.unscaledDeltaTime);
            var m = Mouse.current; if (m?.rightButton.isPressed == true) look += m.delta.ReadValue() * .13f;
            x += TestMove.x; y += TestMove.y; z += TestMove.z; look += TestLook; TestLook = Vector2.zero;
            views.FreeMove = new Vector3(x, y, z); views.FreeLook = look;
            views.FreeSpeed = TestSpeed > 0 ? TestSpeed : k?.leftShiftKey.isPressed == true ? 45 : k?.leftCtrlKey.isPressed == true ? 3 : 14;
        }
        void ReleaseVehicle()
        {
            var input = Car ? Car.GetComponent<VehicleInput>() : null;
            if (input) input.enabled = !Race || Race.State != RaceController.Stage.Countdown;
        }
        // Slow motion: 0.25x while held, 0.5x toggled; eased in and out (about 0.3 s). The physics step shrinks with it so the motion
        // stays smooth (the race clock adds physics steps, so race times are unchanged); the game's sound is turned down, not slowed.
        void Slow(float target)
        {
            Speed = Mathf.MoveTowards(Speed, target, Time.unscaledDeltaTime * 2.5f);
            if (GameMenus.Open || WorldMap.OwnsInput || Time.timeScale == 0f) return;   // a menu, the map or a bug capture has paused the game
            if (Speed < .999f)
            {
                if (!slowed) volumeBefore = AudioListener.volume;
                slowed = true; Time.timeScale = Speed; Time.fixedDeltaTime = baseFixed * Speed;
                AudioListener.volume = volumeBefore * Mathf.Lerp(.3f, 1, (Speed - .25f) / .75f);
            }
            else if (slowed)
            {
                slowed = false; Speed = 1; Time.timeScale = 1; Time.fixedDeltaTime = baseFixed; AudioListener.volume = volumeBefore; volumeBefore = -1f;
            }
        }
        // checks only: input as if from the controls
        public void HoldSlowForTest(bool value) => slowHeldByTest = value;
        [NonSerialized] public bool TestHold; [NonSerialized] public Vector3 TestMove; [NonSerialized] public Vector2 TestLook; [NonSerialized] public float TestSpeed;
        public void ToggleHud() => HudShown = !HudShown;
        public void ToggleGuides() => GuidesShown = !GuidesShown;
        public void ToggleHalfSpeed() => HalfSpeed = !HalfSpeed;

        void LateUpdate()
        {
            // clean screen: the HUD canvases go (the IMGUI HUDs ask HudHidden); menus and the map still show when opened
            bool clean = CleanScreen;   // Round 51: Trailer Mode's own (HudHidden also covers the loading screen and title, which are canvases)
            if (clean && Time.unscaledTime >= nextCanvasLook) { nextCanvasLook = Time.unscaledTime + 1f; FindHudCanvases(); }
            if (clean || cleaned)
            {
                foreach (var c in hudCanvases) if (c) c.enabled = !clean;
                cleaned = clean;
            }
            ShowGuides(!Active || GuidesShown);
            if (screenshotRequested) { screenshotRequested = false; Capture(); }
            UpdatePanel(); menuLastFrame = MenuVisible || WorldMap.OwnsInput;
        }
        // the screen-space canvases that are HUD: everything but the menus, the map and this mode's own panel
        void FindHudCanvases()
        {
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas || c.renderMode == RenderMode.WorldSpace || hudCanvases.Contains(c) || c == panelCanvas) continue;
                if (c.GetComponentInParent<GameMenus>() || c.GetComponentInParent<WorldMap>() || c.name == "Menu canvas" || c.name == "World map") continue;
                hudCanvases.Add(c);
            }
        }
        // G: the checkpoint arches and beams (RaceMarkers) and course arrows / chevrons on the ground. Hidden with forceRenderingOff, so the
        // systems that show and hide them are not disturbed.
        float nextGuideLook;
        void ShowGuides(bool show)
        {
            if (show == !guidesHidden && (show || Time.unscaledTime < nextGuideLook)) return;
            if (!show && Time.unscaledTime >= nextGuideLook)
            {
                nextGuideLook = Time.unscaledTime + 2f;
                var markers = FindAnyObjectByType<RaceMarkers>();
                if (markers) foreach (var r in markers.GetComponentsInChildren<Renderer>(true)) if (!guides.Contains(r)) { guides.Add(r); r.forceRenderingOff = true; }
                if (guides.Count == 0 || !guidesHidden) foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (Guide(r) && !guides.Contains(r)) guides.Add(r);
            }
            guidesHidden = !show;
            foreach (var r in guides) if (r) r.forceRenderingOff = guidesHidden;
        }
        // course arrows and chevrons on the ground (not signs)
        public static bool Guide(Renderer r) => r.name.IndexOf("arrow", StringComparison.OrdinalIgnoreCase) >= 0 || r.name.IndexOf("chevron", StringComparison.OrdinalIgnoreCase) >= 0;

        // ---------- screenshots ----------
        public void Screenshot() => screenshotRequested = true;
        // the camera's own view at the window's full resolution, without any UI, as PNG in Screenshots beside DebugReports
        void Capture()
        {
            var cam = Camera.main; if (!cam) return;
            int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
            var rt = new RenderTexture(w, h, 24); var oldTarget = cam.targetTexture; var active = RenderTexture.active; Texture2D image = null;
            try
            {
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                image = new Texture2D(w, h, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, w, h), 0, 0); image.Apply();
                var raw = image.GetRawTextureData<byte>().ToArray(); var format = image.graphicsFormat;
                Directory.CreateDirectory(ScreenshotFolder);
                string path = Path.Combine(ScreenshotFolder, "ChastainRush_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff") + ".png");
                LastScreenshot = path; screenshotsTaken++;
                Task.Run(() => File.WriteAllBytes(path, ImageConversion.EncodeArrayToPNG(raw, format, (uint)w, (uint)h)));
            }
            catch (Exception e) { Debug.LogWarning("Screenshot failed: " + e.Message); }
            finally { cam.targetTexture = oldTarget; RenderTexture.active = active; rt.Release(); Destroy(rt); if (image) Destroy(image); }
        }
        public void OpenScreenshotFolder()
        {
            try { Directory.CreateDirectory(ScreenshotFolder); System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ScreenshotFolder) { UseShellExecute = true }); }
            catch (Exception e) { Debug.LogWarning("Could not open the folder: " + e.Message); }
        }

        // ---------- Free Roam conditions on demand ----------
        public bool ConditionsAvailable => Active && FreeRoam && WorldLook.Current && WorldLook.Current.Mode == "Free Roam";
        WorldLook.TrailerConditions Conditions()
        {
            var look = WorldLook.Current; if (!ConditionsAvailable) return null;
            return look.Trailer ??= new WorldLook.TrailerConditions { hour = look.Hour, weather = look.RoamWeather, phase = look.MoonPhase };
        }
        static readonly (string name, float hour)[] Times = { ("Dawn", 6.6f), ("Day", 12), ("Dusk", 19.2f), ("Night", 23.5f) };
        public static readonly string[] MoonNames = { "New", "Waxing crescent", "First quarter", "Waxing gibbous", "Full", "Waning gibbous", "Last quarter", "Waning crescent" };
        public void CycleTimeOfDay(int d = 1)
        {
            var c = Conditions(); if (c == null) return;
            if (d < 0) { int j = Array.FindLastIndex(Times, t => t.hour < c.hour - .01f); c.hour = Times[j < 0 ? Times.Length - 1 : j].hour; return; }
            int i = Array.FindIndex(Times, t => t.hour > c.hour + .01f); c.hour = Times[i < 0 ? 0 : i].hour;
        }
        public void NudgeClock(int hours) { var c = Conditions(); if (c != null) c.hour = Mathf.Repeat(Mathf.Round(c.hour) + hours, 24); }
        public void ToggleClock() { var c = Conditions(); if (c != null) c.paused = !c.paused; }
        public void CycleWeather(int d = 1) { var c = Conditions(); if (c != null) c.weather = (Weather)(((int)c.weather + d + 3) % 3); }
        public void CycleMoon(int d = 1) { var c = Conditions(); if (c != null) c.phase = Mathf.Repeat(Mathf.Round(c.phase * 8) + d, 8) / 8f; }
        // a strike now; lightning needs a storm, so this turns the weather to Rain first if it is not raining
        public void LightningNow()
        {
            var c = Conditions(); if (c == null) return;
            c.weather = Weather.Rain; WorldLook.Current.GetComponent<WeatherEffects>()?.StrikeNow();
        }
        public string ConditionsLabel
        {
            get
            {
                var look = WorldLook.Current; if (!look) return "";
                var c = look.Trailer; float hour = c?.hour ?? look.Hour; float phase = c?.phase ?? look.MoonPhase;
                return $"{Mathf.FloorToInt(hour) % 24:00}:{Mathf.FloorToInt(hour * 60) % 60:00} {(c != null && c.paused ? "(clock paused)" : "")} · {(c?.weather ?? look.RoamWeather)} · Moon {MoonNames[Mathf.RoundToInt(phase * 8) % 8].ToLowerInvariant()}";
            }
        }
        public string TimeLabel { get { var c = WorldLook.Current?.Trailer; float h = c?.hour ?? WorldLook.Current?.Hour ?? 12; return h < 4.6f || h >= 20.8f ? "Night" : h < 7.6f ? "Dawn" : h < 17.8f ? "Day" : "Dusk"; } }
        public bool ClockPaused => WorldLook.Current?.Trailer?.paused == true;
        public string WeatherLabel => (WorldLook.Current?.Trailer?.weather ?? WorldLook.Current?.RoamWeather ?? Weather.Clear).ToString();
        public string MoonLabel { get { var look = WorldLook.Current; float p = look ? (look.Trailer?.phase ?? look.MoonPhase) : .5f; return MoonNames[Mathf.RoundToInt(p * 8) % 8]; } }

        // ---------- help text (pause menu > TRAILER MODE > Controls) ----------
        public static string HelpText(bool controller, bool freeRoam)
        {
            string conditions = freeRoam ? "\nFree Roam: T time of day · , / . clock −1 / +1 h · K pause clock · B weather · O moon · L lightning" : "";
            return controller
                ? "TRAILER / PHOTO MODE   (off: pause menu > Trailer Mode, or F8)\n"
                  + "D-pad ← →  camera (Chase, Orbit, Side, Front, Fixed, Flyover, Free, Auto, First person)\n"
                  + "A  camera action: Fixed re-plant · Side other side · Auto next shot\nX (hold)  Auto: keep this shot\n"
                  + "LB (hold)  slow motion 0.25×   RB  0.5× on / off\nD-pad ↑ ↓  field of view   Right stick  distance / height\n"
                  + "R3  screenshot   L3  HUD and controls panel on / off   B  use the panel\nFree camera: left stick move · right stick look · RT / LT up / down\n"
                  + "Time of day, weather, moon, arrows and the Screenshots folder: pause menu > Trailer Mode"
                : "TRAILER / PHOTO MODE   (F8 on / off)    F1 controls panel\n"
                  + "1 Chase  2 Orbit  3 Side  4 Front  5 Fixed  6 Flyover  7 Free  8 Auto  9 First person\n"
                  + "Tab / same number again: Fixed re-plant · Side other side · Auto next shot\nLeft Shift (hold)  Auto: keep this shot\n"
                  + "Z (hold)  slow motion 0.25×   X  0.5× on / off\n− / =  field of view   PgUp / PgDn  distance   Home / End  height\n"
                  + "H  HUD and controls panel on / off   G  checkpoint arches and arrows   P / F12  screenshot   F  shake a fist\n"
                  + "Free camera: WASD move · Q / E down / up · right mouse look · Shift fast · Ctrl precise" + conditions;
        }
    }
}
