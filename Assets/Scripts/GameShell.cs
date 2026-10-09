using System;
using System.Collections.Generic;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChastainRush
{
    // Round 8, item 1: what a stand-alone player needs around the game. New code (Racer's own shell lives in its menus and
    // launcher, which are not imported).
    //   Esc        press twice to quit (the first press shows a prompt; it is ignored while the debug menu or a bug comment is open)
    //   F2         frame-rate counter on / off: frames per second over the last second, the 1% low over the last 10 s, frame time
    // The command line "-exeCheck <dir>" adds the scripted exe check (Checks/ExeCheckRunner.cs); without it nothing else runs.
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
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-exeCheck") { var r = go.AddComponent<ExeCheckRunner>(); r.outputDir = args[i + 1]; }
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
            if (k.f2Key.wasPressedThisFrame) ShowFps = !ShowFps;
            var hud = DeveloperHud.Instance;
            bool hudBusy = hud != null && (hud.MenuOpen || hud.CommentOpen || hud.Capturing);
            if (k.escapeKey.wasPressedThisFrame && !hudBusy)
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
