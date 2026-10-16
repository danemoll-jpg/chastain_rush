using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 27 (B1, C1): Racer's settings (RacerSave.Options, the settings part only: the volumes, VSync and the frame cap, and the radio's
    // choices; Racer keeps records and other options in the same file, this game keeps those in its own files), saved as Racer
    // does: settings.json written with AtomicSave (below), loaded with FromJsonOverwrite into the defaults so a missing field
    // keeps its default, all defaults if the version or a volume is out of range. Beside the race records (persistentDataPath).
    // A check run never reads or writes it: its settings live in the check's own folder (CheckRun.Dir), and it stays muted
    // (ApplySettings leaves AudioListener.volume to CheckRun there).
    public static class GameSettings
    {
        [Serializable] public sealed class Options
        {
            public int version = 1;
            public float master = .8f, ambience = 1, feedback = .65f, vehicle = .75f;
            public float music = .6f;
            public float effects = 1;   // Chastain Rush, Round 27 (C1): Dan's "effects" (SoundMix.Kind.Effects)
            public int frameLimit = 60;   // Racer's display settings (Round 27, C1)
            public bool vsync = true;
            public bool radioOn = true;
            public string musicFolder = "";
            public string musicSource = "";
            public bool musicRecursive = true;
            public string radioChannel = "";
            // Racer 0.75 (RacerSave.Options.rider): the player's rider (the garage's Rider page). Missing in older saves = the
            // default. Chastain Rush, Round 29.
            public RiderLook rider = new();
            // Chastain Rush, Round 30 (A3): traffic, 0 Off, 1 Light, 2 Normal (the default), 3 Heavy (Traffic/Traffic.cs)
            public int traffic = 2;
            // Racer 0.94 Part A (RacerSave.Options.playerName / knownNames): the player's name for the Top 10 (PlayerNames). Chastain Rush,
            // Round 34; nameAsked: the welcome has asked it once
            public string playerName = "";
            public string[] knownNames = new string[0];
            public bool nameAsked;
            // Racer 0.120 Part E (RacerSave.Options.stuntVehicleId): the Stunt event's vehicle, the default next time. Chastain Rush, Round 36
            public string stuntVehicleId = "stuntbuggy";
            // Racer 0.72 to 0.97 (RacerSave.Options): the Free Roam weather (0 Clear, 1 Rain, 2 Snow), its clock and calendar (a new game
            // starts on day 12, near a full moon, at 08:00), the lightning flash (Settings > Display) and the weather's grip (Settings >
            // Gameplay). Chastain Rush, Round 35 (Part C): the world's conditions; a race's are the race setup's (GameMenus)
            public int roamWeather = 0;
            public float roamHour = 8f;
            public int roamDay = 12;
            public bool lightningFlashes = true;
            public bool weatherGrip = true;
            // Racer 0.90 to 0.94 (RacerSave.Options.splitLeftRight; the second player's name, rider): split screen's layout (left / right, else
            // top / bottom) and player 2's name and rider. Chastain Rush, Round 37 (player 2's vehicle and color: vehicle2.json, VehicleSwitch)
            public bool splitLeftRight;
            public string player2Name = "";
            public RiderLook rider2 = new();
        }

        static Options current;
        public static Options Settings { get { if (current == null) Load(); return current; } }
        public static string DirectoryPath => ChastainRush.CheckRun.Active ? ChastainRush.CheckRun.Dir : Application.persistentDataPath;
        public static string FilePath => Path.Combine(DirectoryPath, "settings.json");
        public static event Action Changed;

        static void Load()
        {
            current = new Options();
            try
            {
                if (File.Exists(FilePath)) JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), current);
                bool bad(float v) => float.IsNaN(v) || v < 0 || v > 1;
                if (current.version != 1 || bad(current.master) || bad(current.ambience) || bad(current.feedback) || bad(current.vehicle) || bad(current.music) || bad(current.effects) || (current.frameLimit != 30 && current.frameLimit != 60 && current.frameLimit != 120)) current = new Options();
            }
            catch (Exception e) { Debug.LogWarning("[Settings] unreadable, defaults used: " + e.Message); current = new Options(); }
            current.rider = (current.rider ?? new RiderLook()).Valid(); RiderLook.Player = current.rider;   // Racer's RaceFlow: RiderLook.Player = Save.Settings.rider
            current.rider2 = (current.rider2 ?? new RiderLook()).Valid(); current.player2Name ??= "";   // Round 37: split screen's player 2
        }

        public static void SaveSettings()
        {
            try { AtomicSave.Write(FilePath, JsonUtility.ToJson(Settings, true)); }
            catch (Exception e) { Debug.LogWarning("[Settings] not saved: " + e.Message); }
            Changed?.Invoke();
        }

        public static void ApplySettings()
        {
            if (ChastainRush.CheckRun.Active) return;   // a check run stays muted (CheckRun) and keeps its own frame pacing
            AudioListener.volume = Settings.master;
            QualitySettings.vSyncCount = Settings.vsync ? 1 : 0;   // Racer's ApplySettings
            Application.targetFrameRate = Settings.frameLimit;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { current = null; ApplySettings(); RiderLook.Player = Settings.rider; }   // Round 29: the saved rider before any vehicle is built
    }

    // Racer's AtomicSave: replace the file in one step, keeping the previous one as .bak; Windows readers can briefly hold
    // either path after a replace, so it retries
    public static class AtomicSave
    {
        public static int Retries { get; private set; }
        public static string LastFailure { get; private set; }
        public static void Write(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    string temp = path + ".tmp"; File.WriteAllText(temp, contents);
                    if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
                    return;
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                {
                    LastFailure = error.GetType().Name + " / " + error.HResult + " / " + error.Message;
                    if (attempt >= 4) throw;
                    Retries++; Thread.Sleep(10 << attempt);
                }
            }
        }
    }
}
