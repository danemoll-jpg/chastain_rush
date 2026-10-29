using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 81 (I, Dan: "I also want the steam deck stuff done in this round"): a Steam Deck graphics setting (Settings > Display > Steam Deck: Auto, On,
    // Off; settings.json "steamDeck" 0 / 1 / 2). Auto is on at the Deck's own 1280 x 800 screen or on its GPU (AMD Custom GPU 0405, "Van Gogh"). On: the
    // view (fog and far clip, FarView) to 60 %, traffic (Traffic.CarsFor) to 60 %, people out (AmbientLife) within 60 % of the distance. A check run
    // has it off unless it passes -steamDeck 1.
    public static class SteamDeck
    {
        public const float ViewScale = .6f, CarScale = .6f, PeopleScale = .6f;
        static int forced = -1;
        static bool? detected;
        public static bool Detected
        {
            get
            {
                if (detected == null)
                {
                    string gpu = SystemInfo.graphicsDeviceName ?? "";
                    detected = (Display.main.systemWidth == 1280 && Display.main.systemHeight == 800) || gpu.Contains("Custom GPU 0405") || gpu.Contains("Van Gogh") || gpu.Contains("Sephiroth");
                }
                return detected.Value;
            }
        }
        public static bool On
        {
            get
            {
                if (forced < 0) { forced = 3; var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, "-steamDeck"); if (i >= 0 && i + 1 < a.Length && int.TryParse(a[i + 1], out int v)) forced = v; }
                if (forced == 1) return true;
                if (forced == 0 || CheckRun.Active) return false;
                int m = GameSettings.Settings != null ? GameSettings.Settings.steamDeck : 0;
                return m == 1 || m == 0 && Detected;
            }
        }
        public static string Label => GameSettings.Settings == null ? "Auto" : GameSettings.Settings.steamDeck == 1 ? "On" : GameSettings.Settings.steamDeck == 2 ? "Off" : $"Auto ({(Detected ? "on" : "off")})";
    }
}
