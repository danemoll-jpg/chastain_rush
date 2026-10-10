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
                if (args[i] == "-exeRound17") { var r = go.AddComponent<Round17Runner>(); r.outputDir = args[i + 1]; }   // Round 17: Part A, the debug menu with a virtual gamepad
                if (args[i] == "-exeRound14") { var r = go.AddComponent<Round14Runner>(); r.outputDir = args[i + 1]; }   // Round 14: what slows the vehicles (items 2 and 5)
                if (args[i] == "-exeShortcut") { var r = go.AddComponent<ShortcutCheckRunner>(); r.outputDir = args[i + 1]; Application.runInBackground = true; }   // Round 10: the shortcut check in the exe (-shortcutOnly <id>, -shortcutSpeeds 20,30, -shortcutNoRace)
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
            if (k.f2Key.wasPressedThisFrame || (k.pKey.wasPressedThisFrame && !hudBusy)) ShowFps = !ShowFps;
            // Round 16: Esc opens the menus (QUIT GAME is there); press-twice-to-quit is left only for a run without them (a check)
            if (k.escapeKey.wasPressedThisFrame && !hudBusy && GameMenus.Instance == null)
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
            if (ShowFps)
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
