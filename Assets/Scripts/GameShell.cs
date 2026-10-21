using System;
using System.Collections.Generic;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 8, item 1: what a stand-alone player needs around the game. New code (Racer's own shell lives in its menus and
    // launcher, which are not imported).
    //   Esc        Round 16: opens the menus (Menus/GameMenus.cs, QUIT GAME is there); press twice to quit only without them (a check)
    //   F2 or P    frame-rate counter on / off: frames per second over the last second, the 1% low over the last 10 s, frame time
    // The command line "-exeCheck <dir>" adds the scripted exe check (Checks/ExeCheckRunner.cs), "-exeShots <list.json> <dir>"
    // the Round 9 screenshots at given places (Checks/ExeShotsRunner.cs); without them nothing else runs.
    public sealed class GameShell : MonoBehaviour
    {
        public static GameShell Instance { get; private set; }
        public bool ShowFps { get; set; }
        public float Fps { get; private set; }
        public float LowFps { get; private set; }

        float quitPromptUntil;
        readonly Queue<float> window = new Queue<float>();   // frame times over the last ~10 s, for the 1% low
        float windowSum, fpsAcc, fpsTimer, lowTimer;
        int fpsFrames;
        GUIStyle style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null || FindAnyObjectByType<ArcadeVehicle>() == null) return;
            var go = new GameObject("GameShell");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameShell>();
            var carGo = FindAnyObjectByType<ArcadeVehicle>().gameObject;
            if (!carGo.GetComponent<RampHold>()) carGo.AddComponent<RampHold>();   // Round 10: held shortcut ramp tops
            WorldMap.Boot(go);
            Exploration.Boot(go);   // Round 48: the map's fog, places, travel and waypoint (Explore/Exploration.cs)   // Round 20 (item 2): the map, on View / Select and M (Map/WorldMap.cs)
            MiniMap.Boot(go);    // Round 24 A2 (A5): Racer's minimap (Map/MiniMap.cs)
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-exeCheck") { var r = go.AddComponent<ExeCheckRunner>(); r.outputDir = args[i + 1]; }
                if (args[i] == "-exeRound10") { var r = go.AddComponent<Round10Runner>(); r.outputDir = args[i + 1]; }   // Round 10: items 2i and 10
                if (args[i] == "-exeRound11") { var r = go.AddComponent<Round11Runner>(); r.outputDir = args[i + 1]; }   // Round 11: items 1, 3 and 10
                if (args[i] == "-exeRound12") { var r = go.AddComponent<Round12Runner>(); r.outputDir = args[i + 1]; }   // Round 12: items 1 to 9
                if (args[i] == "-exeRound13") { var r = go.AddComponent<Round13Runner>(); r.outputDir = args[i + 1]; }   // Round 13: the vehicles (item 9)
                if (args[i] == "-exeRound15") { var r = go.AddComponent<Round15Runner>(); r.outputDir = args[i + 1]; }   // Round 15: water, then the AI drivers
                if (args[i] == "-exeRound16") { var r = go.AddComponent<Round16Runner>(); r.outputDir = args[i + 1]; }   // Round 16: the menus with a virtual gamepad
                if (args[i] == "-exeRound27") { var r = go.AddComponent<Round27Runner>(); r.outputDir = args[i + 1]; }   // Round 27
                if (args[i] == "-exeRound28") { var r = go.AddComponent<Round28Runner>(); r.outputDir = args[i + 1]; }   // Round 28
                if (args[i] == "-exeRound30") { var r = go.AddComponent<Round30Runner>(); r.outputDir = args[i + 1]; }   // Round 30: traffic
                if (args[i] == "-exeRound32") { var r = go.AddComponent<Round32Runner>(); r.outputDir = args[i + 1]; }   // Round 32 (B2): the junction of BUG-010 in Heavy traffic
                if (args[i] == "-exeRound29") { var r = go.AddComponent<Round29Runner>(); r.outputDir = args[i + 1]; }   // Round 29: the rider page
                if (args[i] == "-exeRound17") { var r = go.AddComponent<Round17Runner>(); r.outputDir = args[i + 1]; }   // Round 17: Part A, the debug menu with a virtual gamepad
                if (args[i] == "-exeRound14") { var r = go.AddComponent<Round14Runner>(); r.outputDir = args[i + 1]; }   // Round 14: what slows the vehicles (items 2 and 5)
                if (args[i] == "-exeRound18") { var r = go.AddComponent<Round18Runner>(); r.outputDir = args[i + 1]; }   // Round 18: items 3 to 9 (the creek trail, P1, R1, R3, the race)
                if (args[i] == "-exeRound19") { var r = go.AddComponent<Round19Runner>(); r.outputDir = args[i + 1]; }   // Round 19: items 1 to 6 (the wider trail, big jumps, the rock bar, the pipe, R3's hint, R1)
                if (args[i] == "-exeRound20") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; }   // Round 20: BUG-001 and BUG-002, the map
                if (args[i] == "-exeRound21") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round21"; }   // Round 21 (Round20Runner.R21.cs)
                if (args[i] == "-exeRound26") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round26"; }   // Round 26 (Round20Runner.R26.cs)
                if (args[i] == "-exeRound33") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round33"; }   // Round 33: the Chastain Ridge race (Round20Runner.R33.cs)
                if (args[i] == "-exeRound35") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round35"; }   // Round 35 (Round20Runner.R35*.cs)
                if (args[i] == "-exeRound36") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round36"; }   // Round 36 (Round20Runner.R36*.cs)
                if (args[i] == "-exeRound39") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round39"; }   // Round 39 (Round20Runner.R39*.cs)
                if (args[i] == "-exeRound40") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round40"; }   // Round 40 (Round20Runner.R40*.cs)
                if (args[i] == "-exeRound43") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round43"; }   // Round 43 (Round20Runner.R43*.cs)
                if (args[i] == "-exeRound44") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round44"; }   // Round 44 (Round20Runner.R44.cs)
                if (args[i] == "-exeRound45") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round45"; }   // Round 45 (Round20Runner.R45.cs)
                if (args[i] == "-exeRound46") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round46"; }   // Round 46 (Round20Runner.R46.cs)
                if (args[i] == "-exeRound50") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round50"; }   // Round 50 (Round20Runner.R50.cs)
                if (args[i] == "-exeRound49") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round49"; }   // Round 49 (Round20Runner.R49.cs)
                if (args[i] == "-exeRound48") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round48"; }   // Round 48 (Round20Runner.R48.cs)
                if (args[i] == "-exeRound54") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round54"; }   // Round 54 (Round20Runner.R54.cs)
                if (args[i] == "-exeRound53") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round53"; }   // Round 53 (Round20Runner.R53.cs)
                if (args[i] == "-exeRound52") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round52"; }   // Round 52 (Round20Runner.R52.cs)
                if (args[i] == "-exeRound47") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round47"; }   // Round 47 (Round20Runner.R47.cs)
                if (args[i] == "-exeRound42") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round42"; }   // Round 42 (Round20Runner.R42.cs)
                if (args[i] == "-exeRound41") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round41"; }   // Round 41 (Round20Runner.R41*.cs)
                if (args[i] == "-exeRound38") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round38"; }   // Round 38 (Round20Runner.R38*.cs)
                if (args[i] == "-exeRound37") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round37"; }   // Round 37 (Round20Runner.R37*.cs)
                if (args[i] == "-exeRound34") { var r = go.AddComponent<Round20Runner>(); r.outputDir = args[i + 1]; r.tag = "round34"; }   // Round 34 (Round20Runner.R34*.cs)
                if (args[i] == "-exeRound51") { var r = go.AddComponent<Round51Runner>(); r.outputDir = args[i + 1]; }   // Round 51: the title, the loading screen, the tips
                if (args[i] == "-exeShortcut") { var r = go.AddComponent<ShortcutCheckRunner>(); r.outputDir = args[i + 1]; Application.runInBackground = true; }   // Round 10: the shortcut check in the exe (-shortcutOnly <id>, -shortcutSpeeds 20,30, -shortcutNoRace)
                if (args[i] == "-exeProfile") { var r = go.AddComponent<ProfileRunner>(); r.outputDir = args[i + 1]; }   // Round 24 A2: where the frame time goes
                if (args[i] == "-exeShots" && i + 2 < args.Length) { var r = go.AddComponent<ExeShotsRunner>(); r.listPath = args[i + 1]; r.outputDir = args[i + 2]; }   // Round 9
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            fpsAcc += dt; fpsFrames++;
            if (fpsAcc >= 1f) { Fps = fpsFrames / fpsAcc; fpsAcc = 0f; fpsFrames = 0; }
            window.Enqueue(dt); windowSum += dt;
            while (windowSum > 10f && window.Count > 1) windowSum -= window.Dequeue();
            lowTimer += dt;
            if (lowTimer > 0.5f && window.Count > 20)
            {
                lowTimer = 0f;
                var a = window.ToArray(); Array.Sort(a);
                LowFps = 1f / a[Mathf.Clamp(Mathf.FloorToInt(a.Length * 0.99f), 0, a.Length - 1)];
            }

            var k = Keyboard.current;
            if (k == null) return;
            var hud = DeveloperHud.Instance;
            bool hudBusy = hud != null && (hud.MenuOpen || hud.CommentOpen || hud.Capturing);
            // Round 9: P as well, for a keyboard that needs Fn for F2 (not while a bug comment is being typed)
            if (k.f2Key.wasPressedThisFrame || (k.pKey.wasPressedThisFrame && !hudBusy && !TrailerMode.Active)) ShowFps = !ShowFps;   // Round 43: P is Trailer Mode's screenshot while it is on
            // Round 16: Esc opens the menus (QUIT GAME is there); press-twice-to-quit is left only for a run without them (a check)
            if (k.escapeKey.wasPressedThisFrame && !hudBusy && GameMenus.Instance == null && !WorldMap.OwnsInput)
            {
                if (Time.unscaledTime < quitPromptUntil) Quit();
                else quitPromptUntil = Time.unscaledTime + 2.5f;
            }
            else if (k.anyKey.wasPressedThisFrame && !k.escapeKey.wasPressedThisFrame) quitPromptUntil = 0f;
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight, normal = { textColor = Color.white } };
            float s = Mathf.Max(1f, Screen.height / 800f);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s, h = Screen.height / s;
            if (ShowFps && !TrailerMode.HudHidden)   // Round 43 (Part D): Trailer Mode's clean screen
            {
                string t = $"{Fps:0} fps   1% low {LowFps:0}   {1000f / Mathf.Max(Fps, 1f):0.0} ms";
                Label(new Rect(w - 520, 8, 506, 30), t, style);
            }
            if (Time.unscaledTime < quitPromptUntil)
            {
                var c = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter, fontSize = 30 };
                Label(new Rect(0, h * 0.45f, w, 40), "Press Esc again to quit", c);
            }
            GUI.matrix = old;
        }

        static void Label(Rect r, string text, GUIStyle st)
        {
            var c = st.normal.textColor;
            st.normal.textColor = new Color(0f, 0f, 0f, 0.85f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, st);
            st.normal.textColor = c; GUI.Label(r, text, st);
        }
    }
}
