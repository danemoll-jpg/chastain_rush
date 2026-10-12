using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ChastainRush
{
    // Round 15, item 4: sound files Dan can drop in without a rebuild. Each time the game starts it reads
    //   <persistentDataPath>\Sounds\Geese\goose_honk*.wav|ogg|mp3   added to the honks built into the game (Assets/Audio/Geese)
    //   <persistentDataPath>\Sounds\Breaks\wood*|metal*|glass*|water*|boom*.wav|ogg|mp3  each kind replaces the built-in break sound
    //                                                                            of that kind (BreakSounds)
    //   <persistentDataPath>\Sounds\Trampoline\trampoline*.wav|ogg|mp3   Round 20 follow-up: replaces the made bounce sound
    //                                                                            (Trampoline.cs; several: one at random per bounce)
    // For the exe that is %USERPROFILE%\AppData\LocalLow\Chastain Rush\Chastain Rush\Sounds\. The folders and a READ ME are
    // made on the first run of the game Dan starts himself (a check run only reads them, never writes there).
    public sealed class SoundDropIns : MonoBehaviour
    {
        public static SoundDropIns Instance { get; private set; }
        public static string Root => rootOverride ?? Path.Combine(Application.persistentDataPath, "Sounds");
        static string rootOverride;   // checks: -soundsRoot <dir> (a test folder; Dan's own is never touched by a check)
        public static readonly string[] Extensions = { ".wav", ".ogg", ".mp3" };
        public static readonly string[] BreakKinds = { "wood", "metal", "glass", "water", "boom" };   // Round 17: boom, a gas pump's explosion
        public bool Loaded { get; private set; }
        public readonly List<AudioClip> Honks = new List<AudioClip>();
        public readonly List<AudioClip> Herons = new List<AudioClip>();   // Round 17: heron*, the heron's own call (none = a silent heron)
        public readonly List<AudioClip> Bounces = new List<AudioClip>();   // Round 20 follow-up: trampoline*, the trampoline's bounce
        public readonly Dictionary<string, List<AudioClip>> Breaks = new Dictionary<string, List<AudioClip>>();
        public static event Action Ready;   // fired once every file has been read (also when there are none)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null || Application.isBatchMode) return;
            var go = new GameObject("SoundDropIns"); DontDestroyOnLoad(go);
            Instance = go.AddComponent<SoundDropIns>();
        }

        void Start()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-soundsRoot") rootOverride = args[i + 1];
            if (!CheckRun.Active || rootOverride != null) MakeFolders();
            StartCoroutine(LoadAll());
        }

        static void MakeFolders()
        {
            try
            {
                var geese = Path.Combine(Root, "Geese"); var breaks = Path.Combine(Root, "Breaks"); var tramp = Path.Combine(Root, "Trampoline");
                Directory.CreateDirectory(geese); Directory.CreateDirectory(breaks); Directory.CreateDirectory(tramp);
                var t = Path.Combine(tramp, "READ ME.txt");
                if (!File.Exists(t)) File.WriteAllText(t,
                    "Trampoline sound for Chastain Rush.\r\n\r\n" +
                    "A file here replaces the game's own bounce sound of the giant trampoline. Name it starting with trampoline,\r\n" +
                    "for example trampoline1.wav, trampoline_boing.mp3. With several files, one is picked at random for each bounce.\r\n" +
                    "WAV, OGG and MP3 all work. The game reads this folder each time it starts: no rebuild needed.\r\n" +
                    "Delete the files to go back to the game's own sound.\r\n");
                var g = Path.Combine(geese, "READ ME.txt");
                if (!File.Exists(g) || !File.ReadAllText(g).Contains("heron")) File.WriteAllText(g,   // Round 17: the heron line added to an older READ ME
                    "Goose honks for Chastain Rush.\r\n\r\n" +
                    "Drop sound files here named goose_honk followed by anything, for example goose_honk1.wav, goose_honk_long.ogg.\r\n" +
                    "WAV, OGG and MP3 all work. The game reads this folder each time it starts: no rebuild needed.\r\n" +
                    "Each honk is picked at random from these files (and any built into the game).\r\n" +
                    "With no goose file anywhere the geese stay silent.\r\n\r\n" +
                    "The heron never uses the goose honks. To give it a call of its own, drop a file here named heron followed\r\n" +
                    "by anything, for example heron1.wav. With no heron file the heron stays silent.\r\n");
                var b = Path.Combine(breaks, "READ ME.txt");
                if (!File.Exists(b) || !File.ReadAllText(b).Contains("boom")) File.WriteAllText(b,   // Round 17: the boom line added to an older READ ME
                    "Break sounds for Chastain Rush.\r\n\r\n" +
                    "A file here replaces the game's own sound of that kind. Name it starting with the kind:\r\n" +
                    "  wood...   wooden fences (privacy, picket, the white rail) and the mailbox's post\r\n" +
                    "  metal...  the pool's iron fence, mailboxes, street lamps, fire hydrants\r\n" +
                    "  glass...  windows and the sliding glass door, and the street lamp's glass\r\n" +
                    "  water...  the rush of water from a broken hydrant (played in a loop while it runs)\r\n" +
                    "  boom...   a gas pump's explosion at the shopping center\r\n" +
                    "for example wood1.wav, wood_crack.ogg, metal_clang.mp3. With several of one kind, one is picked at random.\r\n" +
                    "WAV, OGG and MP3 all work. The game reads this folder each time it starts: no rebuild needed.\r\n" +
                    "Delete a file to go back to the game's own sound.\r\n");
            }
            catch (Exception e) { Debug.LogWarning("[Sounds] could not make the drop-in folders: " + e.Message); }
        }

        IEnumerator LoadAll()
        {
            foreach (var path in Files(Path.Combine(Root, "Geese"), "goose_honk"))
                yield return Load(path, c => Honks.Add(c));
            foreach (var path in Files(Path.Combine(Root, "Geese"), "heron"))
                yield return Load(path, c => Herons.Add(c));
            foreach (var path in Files(Path.Combine(Root, "Trampoline"), "trampoline"))
                yield return Load(path, c => Bounces.Add(c));
            foreach (var kind in BreakKinds)
                foreach (var path in Files(Path.Combine(Root, "Breaks"), kind))
                    yield return Load(path, c => { if (!Breaks.TryGetValue(kind, out var l)) Breaks[kind] = l = new List<AudioClip>(); l.Add(c); });
            Loaded = true;
            Debug.Log($"[Sounds] drop-ins from {Root}: {Honks.Count} goose honks, {Herons.Count} heron calls, {Bounces.Count} trampoline bounces; breaks {string.Join("/", BreakKinds)}: " +
                      string.Join("/", Array.ConvertAll(BreakKinds, k => Breaks.TryGetValue(k, out var l) ? l.Count.ToString() : "0")));
            Ready?.Invoke();
        }

        static List<string> Files(string dir, string prefix)
        {
            var list = new List<string>();
            try
            {
                if (!Directory.Exists(dir)) return list;
                foreach (var f in Directory.GetFiles(dir))
                {
                    string name = Path.GetFileName(f).ToLowerInvariant(), ext = Path.GetExtension(name);
                    if (name.StartsWith(prefix) && Array.IndexOf(Extensions, ext) >= 0) list.Add(f);
                }
                list.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception e) { Debug.LogWarning("[Sounds] " + dir + ": " + e.Message); }
            return list;
        }

        static IEnumerator Load(string path, Action<AudioClip> done)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            var type = ext == ".wav" ? AudioType.WAV : ext == ".ogg" ? AudioType.OGGVORBIS : AudioType.MPEG;
            using (var req = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, type))
            {
                ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = false;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) { Debug.LogWarning($"[Sounds] {path}: {req.error}"); yield break; }
                var clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip == null || clip.length <= 0f) { Debug.LogWarning($"[Sounds] {path}: not a sound Unity can read"); yield break; }
                clip.name = Path.GetFileNameWithoutExtension(path);
                done(clip);
            }
        }
    }
}
