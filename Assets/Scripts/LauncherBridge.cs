using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ChastainRush
{
    // Round 81 (J): Woodstock Rush's LauncherBridge (Racer/Assets/Scripts/LauncherBridge.cs), for Chastain Rush's launcher (Launcher/). The launcher starts
    // the game with -racerManagedMusic <its soundtrack> -racerPersonalMusic <songs kept from earlier soundtracks> -racerLauncherReady <file>
    // -racerLauncherNonce <text> (the same names as Woodstock Rush's launcher); once the game is up (the scene's GameShell running a few frames) this writes
    // the nonce to the ready file, so the launcher knows the start worked. Without those arguments (the game started directly) nothing happens.
    // Racer waited for its RaceFlow's save; the discovery log of its test save (-racerTestSave) is left behind: a launcher check run is a check
    // run here (CheckRun: its files in that folder).
    public sealed class LauncherBridge : MonoBehaviour
    {
        public static string Argument(string key) { var args = Environment.GetCommandLineArgs(); for (int i = 1; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1]; return null; }
        public static string ManagedMusic => Argument("-racerManagedMusic");
        public static string PersonalMusic => Argument("-racerPersonalMusic");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartBridge()
        { if (ChastainRush.WorldBoot.Defer(StartBridge)) return; 
            if (Argument("-racerLauncherReady") == null) return;
            var go = new GameObject("Launcher startup handshake"); DontDestroyOnLoad(go); go.AddComponent<LauncherBridge>();
        }

        IEnumerator Start()
        {
            var path = Argument("-racerLauncherReady"); var nonce = Argument("-racerLauncherNonce");
            if (string.IsNullOrEmpty(nonce) || nonce.Length > 128 || !Path.IsPathRooted(path)) { Destroy(gameObject); yield break; }
            float end = Time.realtimeSinceStartup + 90f; GameShell shell = null;
            while (Time.realtimeSinceStartup < end && !(shell = FindFirstObjectByType<GameShell>())) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            if (shell)
            {
                if (!Application.isBatchMode) yield return new WaitForEndOfFrame();
                try { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path + ".tmp", nonce); File.Move(path + ".tmp", path); }
                catch (Exception e) { Debug.LogWarning("Launcher ready signal unavailable: " + e.Message); }
                Debug.Log("[Launcher] ready signal written" + (ManagedMusic != null ? $"; soundtrack {ManagedMusic}, personal music {PersonalMusic}" : ""));
                ReplaceOldLauncher(path);
            }
            Destroy(gameObject);
        }

        // Round 92 (item 0): launcher 1 (installed with 0.81 to 0.91) cannot update itself and refuses any file of 2 GiB or more. This game carries the
        // new launcher (Launcher/ChastainRushLauncher.exe beside the exe, in the signed release); when the launcher that started it is one of launcher
        // 1's known builds, the running launcher's exe is renamed to .old (Windows allows renaming a running exe) and the new one copied in its place.
        // Its next start is the new launcher (which then updates itself from later releases and removes the .old file). Nothing else is touched.
        static readonly string[] OldLaunchers = { "f5648b527344a36369661d04a805b80ee089e340e658b366c6588283db2da9a3" };
        static void ReplaceOldLauncher(string readyPath)
        {
            string exe = null, old = null;
            try
            {
                var root = Directory.GetParent(Path.GetDirectoryName(readyPath))?.FullName; if (root == null) return;   // <root>/startup/<nonce>.ready
                exe = Path.Combine(root, "ChastainRushLauncher.exe"); old = exe + ".old";
                var shipped = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Launcher", "ChastainRushLauncher.exe");
                if (!File.Exists(exe) || !File.Exists(shipped)) return;
                if (Array.IndexOf(OldLaunchers, Sha256(exe)) < 0) return;   // only a known launcher 1
                if (File.Exists(old)) File.Delete(old);
                File.Move(exe, old); File.Copy(shipped, exe);
                Debug.Log("[Launcher] launcher 1 replaced by the launcher this release carries: " + exe);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Launcher] launcher not replaced: " + e.Message);
                try { if (exe != null && !File.Exists(exe) && File.Exists(old)) File.Move(old, exe); } catch (Exception) { }
            }
        }
        static string Sha256(string file)
        {
            using var sha = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(file);
            return BitConverter.ToString(sha.ComputeHash(f)).Replace("-", "").ToLowerInvariant();
        }
    }
}
