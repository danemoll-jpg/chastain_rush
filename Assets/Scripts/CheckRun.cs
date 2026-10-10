using System;
using System.IO;
using UnityEngine;

namespace ChastainRush
{
    // Round 15, items 1 and 5: every automated run is quiet and keeps to its own folder.
    // A check run is an exe started with any "-exe..." argument, or an Editor run started with -batchmode / -executeMethod.
    // At start-up, before the first scene loads (so before anything can make a sound), it sets AudioListener.volume to 0.
    // A check that must hear something calls Hear(true) for that moment only and Hear(false) after, and says so in its report.
    // Its files (debug reports, the bump log, race records) go under Dir: the folder given after the -exe argument (for
    // -exeShots the second one), or after an Editor check's "-...Out" argument, else the system temp folder. The game Dan
    // launches himself has none of these arguments and is never muted.
    public static class CheckRun
    {
        public static bool Active { get; private set; }
        public static string Dir { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            Active = false; Dir = null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                bool exe = a.StartsWith("-exe", StringComparison.Ordinal);
                if (exe || a == "-batchmode" || a == "-executeMethod") Active = true;
                if (Dir != null || i + 1 >= args.Length) continue;
                if (a == "-exeShots" && i + 2 < args.Length) Dir = args[i + 2];
                else if (exe || (a.EndsWith("Out", StringComparison.Ordinal) && a.StartsWith("-", StringComparison.Ordinal))) Dir = args[i + 1];
            }
            if (!Active) return;
            if (string.IsNullOrEmpty(Dir)) Dir = Path.Combine(Path.GetTempPath(), "ChastainRushCheck");
            try { Dir = Path.GetFullPath(Dir); Directory.CreateDirectory(Dir); } catch (Exception) { }
            AudioListener.volume = 0f;
            Application.runInBackground = true;   // Round 16 (Dan: stay off his screen): a check keeps running minimized, unfocused
            Debug.Log("[Check] muted; check files go to " + Dir);
        }

        // Sound on for one moment of a check, then off again.
        public static void Hear(bool on)
        {
            if (!Active) return;
            AudioListener.volume = on ? 1f : 0f;
            Debug.Log("[Check] sound " + (on ? "ON" : "off"));
        }
    }
}
