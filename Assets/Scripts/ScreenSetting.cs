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
    // Round 27 (C1): the Settings > Display page's Screen and Resolution rows (the menus' Screen row moved there). Resolution: the
    // desktop's own (the default) or one of the display's sizes below it; full screen draws at that size and fills the screen, a window
    // is that size (with the desktop's own, a window is three quarters of it, as before).
    public static class ScreenSetting
    {
        [Serializable] sealed class Saved { public bool fullscreen = true; public int width, height; }
        public static bool Fullscreen { get; private set; } = true;
        static int width, height;   // 0: the desktop's own

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
            Fullscreen = true; width = height = 0;
            if (CheckRun.Active) return;
            try { if (File.Exists(FilePath())) { var s = new Saved(); JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath()), s); Fullscreen = s.fullscreen; width = s.width; height = s.height; } }
            catch (Exception e) { Debug.LogWarning("[Screen] display.json unreadable: " + e.Message); }
            Apply();
        }

        // the sizes offered: the desktop's own first, then the display's sizes below it, largest first
        static System.Collections.Generic.List<Vector2Int> Sizes()
        {
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            var list = new System.Collections.Generic.List<Vector2Int> { Vector2Int.zero };
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (v.x >= 1280 && v.x < w && v.y <= h && !list.Contains(v)) list.Add(v);
            }
            list.Sort((a, b) => a == Vector2Int.zero ? -1 : b == Vector2Int.zero ? 1 : (b.x * b.y).CompareTo(a.x * a.y));
            return list;
        }
        public static string ResolutionLabel => width <= 0 ? $"Desktop ({Display.main.systemWidth} x {Display.main.systemHeight})" : $"{width} x {height}";
        public static void StepResolution(int d)
        {
            var sizes = Sizes(); int i = Mathf.Max(0, sizes.IndexOf(new Vector2Int(Mathf.Max(0, width), Mathf.Max(0, height))));
            var next = sizes[(i + d + sizes.Count) % sizes.Count]; width = next.x; height = next.y;
            Set(Fullscreen);
        }

        public static void Set(bool fullscreen)
        {
            Fullscreen = fullscreen;
            if (!CheckRun.Active)
            {
                try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath())); File.WriteAllText(FilePath(), JsonUtility.ToJson(new Saved { fullscreen = fullscreen, width = width, height = height }, true)); }
                catch (Exception e) { Debug.LogWarning("[Screen] could not save display.json: " + e.Message); }
            }
            Apply();
        }

        static void Apply()
        {
            if (Application.isEditor) return;
            int w = Display.main.systemWidth, h = Display.main.systemHeight;   // the desktop's own resolution
            bool own = width <= 0 || height <= 0;
            if (Fullscreen) Screen.SetResolution(own ? w : width, own ? h : height, FullScreenMode.FullScreenWindow);
            else Screen.SetResolution(own ? Mathf.RoundToInt(w * 0.75f) : width, own ? Mathf.RoundToInt(h * 0.75f) : height, FullScreenMode.Windowed);
            Debug.Log($"[Screen] {(Fullscreen ? "full screen" : "windowed")}, {ResolutionLabel}, desktop {w} x {h}");
        }
    }
}
