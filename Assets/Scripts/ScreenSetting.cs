using System;
using System.IO;
using UnityEngine;

namespace ChastainRush
{
    // Round 24 A3, item B1 (Dan: "allow game to release in fullscreen", and "can we add the option to open in full screen"): the
    // game Dan launches opens full screen at his desktop's resolution, or windowed if he last chose that in the menu's
    // "Screen" row. The choice is saved in display.json beside the race records and applied at start-up, so it wins over the
    // window mode and size Unity remembers from the last run (the minimized 1280 x 720 check runs left that windowed).
    // A check run never applies or saves it (its -screen-* arguments stand). Alt+Enter still switches for the moment.
    public static class ScreenSetting
    {
        [Serializable] sealed class Saved { public bool fullscreen = true; }
        public static bool Fullscreen { get; private set; } = true;

        static string FilePath()
        {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "local", "display.json"));   // git-ignored
#else
            return Path.Combine(Application.persistentDataPath, "display.json");
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Fullscreen = true;
            if (CheckRun.Active) return;
            try { if (File.Exists(FilePath())) { var s = new Saved(); JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath()), s); Fullscreen = s.fullscreen; } }
            catch (Exception e) { Debug.LogWarning("[Screen] display.json unreadable: " + e.Message); }
            Apply();
        }

        public static void Set(bool fullscreen)
        {
            Fullscreen = fullscreen;
            if (!CheckRun.Active)
            {
                try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath())); File.WriteAllText(FilePath(), JsonUtility.ToJson(new Saved { fullscreen = fullscreen }, true)); }
                catch (Exception e) { Debug.LogWarning("[Screen] could not save display.json: " + e.Message); }
            }
            Apply();
        }

        static void Apply()
        {
            if (Application.isEditor) return;
            int w = Display.main.systemWidth, h = Display.main.systemHeight;   // the desktop's own resolution
            if (Fullscreen) Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
            else Screen.SetResolution(Mathf.RoundToInt(w * 0.75f), Mathf.RoundToInt(h * 0.75f), FullScreenMode.Windowed);
            Debug.Log($"[Screen] {(Fullscreen ? "full screen" : "windowed")}, desktop {w} x {h}");
        }
    }
}
