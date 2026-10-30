using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 84 (A): Woodstock Rush's HIGH MODE (Racer 1.2 / 1.2.0-review2, HighMode.cs), ported. One ON / OFF switch at the top of the title
    // and pause menus, remembered in settings.json (highMode), so the game starts in the mode last used. On: fewer menu choices
    // (GameMenus.High.cs), gutter bumpers along the car race courses (HighBumpers), forgiving driving (HighAssist), Easy rivals, the course's
    // own laps, its own records ("high-" boards, marked HIGH), its own TIME & WEATHER. A race keeps the mode it started with
    // (RaceController.High); Free Roam follows the switch at once. HighPrompt offers it to a player who keeps wrecking in normal mode.
    // Racer's Campaign.Testing (everything unlocked) has nothing to do here: nothing is locked in this game.
    public static class HighMode
    {
        public const string RecordPrefix = "high-";
        public static bool On => GameSettings.Settings.highMode;
        public static void Set(bool on)
        {
            if (GameSettings.Settings.highMode == on) return;
            GameSettings.Settings.highMode = on; if (!CheckRun.Active) GameSettings.SaveSettings();
        }
        // checks only: -highMode 1 / 0 switches it for the run (a check never writes Dan's settings)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Args()
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < a.Length; i++) if (a[i] == "-highMode") GameSettings.Settings.highMode = a[i + 1] == "1";
        }
        // split screen: each player's own choice (the setup's rows; each starts as the main menu's switch)
        public static bool P1High, P2High;
        // this player is in HIGH MODE now: a race's own (as it started), else Free Roam's (split screen: each player's)
        public static bool ForPlayer(int player)
        {
            var r = RaceController.Instance;
            if (r && r.State != RaceController.Stage.Free) return r.HighFor(player);
            return SplitScreen.Active ? (player == 1 ? P1High : P2High && !SplitScreen.P2Ai) : On;
        }
        // a player's own vehicle in HIGH MODE now (never an AI rival's or traffic)
        public static bool ForVehicle(ArcadeVehicle c)
        {
            if (!c) return false; var r = RaceController.Instance;
            int player = SplitScreen.Active ? SplitScreen.PlayerOf(c) : r && r.PlayerCar == c ? 1 : 0;
            return player > 0 && ForPlayer(player);
        }
        // the session now is in HIGH MODE (its records and boards are kept apart)
        public static bool Now => ForPlayer(1);
        // "No" to "Would you like to play HIGH MODE?": not asked again this session
        public static bool Declined;
        // TIME & WEATHER (Free Roam and races in HIGH MODE)
        public static readonly string[] TimeNames = { "Morning", "Day", "Sunset", "Night", "Day cycle" };
        public static readonly string[] WeatherNames = { "Clear", "Rain", "Snow", "Random" };
        public static int TimeChoice => Mathf.Clamp(GameSettings.Settings.highTime, 0, 4);
        public static int WeatherChoice => Mathf.Clamp(GameSettings.Settings.highWeather, 0, 3);
        static Weather rolled;
        // Random: a new weather for each race and each Free Roam start
        public static void Roll() => rolled = (Weather)Random.Range(0, 3);
        public static TimeOfDay RaceTime => TimeChoice switch { 0 => TimeOfDay.Dawn, 2 => TimeOfDay.Dusk, 3 => TimeOfDay.Night, _ => TimeOfDay.Day };
        public static Weather Weather => WeatherChoice == 3 ? rolled : (Weather)WeatherChoice;
        // Free Roam's sky held at this hour (-1: the normal day cycle)
        public static float FixedHour => TimeChoice switch { 0 => 6.3f, 1 => 12.5f, 2 => 19.4f, 3 => 23f, _ => -1 };
        // HIGH MODE's TIME & WEATHER apply now: Free Roam or a race in HIGH MODE, one player (split screen keeps its own setup's)
        public static bool LookApplies => !SplitScreen.Active && Now && !TrailerMode.Active;
        public static string Badge => "HIGH";
        public static readonly Color Colour = new(1f, .62f, .2f);
        // the air race's rings in HIGH MODE (Claude's adaptation for this game: Woodstock Rush has no air races)
        public const float RingScale = 1.5f;
    }
}
