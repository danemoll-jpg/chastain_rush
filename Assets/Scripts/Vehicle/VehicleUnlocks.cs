using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 94 (C, Dan: "these could be unlockables from events or eggs. We could also hide some planes behind the eggs as well"). After Racer's
    // VehicleUnlocks (the acorn reward), rewritten for the goose egg tiers:
    //   a vehicle whose profile Unlock is "egg-N" is earned when N goose eggs are found (or the tier N was reached: a restarted hunt keeps its
    //   tiers); "campaign" is a reward of Round 95's events (locked until then). Every vehicle that existed before Round 94 stays unlocked.
    //   HIGH MODE, split screen and the debug menu's "Unlock all" (or -unlockAll on the command line) have everything. The AI may race any car.
    //   An unlock is announced once: the egg banner says what a tier gave, and a vehicle already earned when this build first starts (Dan's 19
    //   eggs: tier 10) gets the banner once in Free Roam. What was announced is kept in vehicle-unlocks-v1.json beside the race records.
    public static class VehicleUnlocks
    {
        [Serializable] class Saved { public List<string> announced = new(); }
        public const string SaveName = "vehicle-unlocks-v1.json";
        static bool unlockAll = Array.IndexOf(Environment.GetCommandLineArgs(), "-unlockAll") >= 0;
        public static bool UnlockAll { get => unlockAll; set { unlockAll = value; Debug.Log("[Unlocks] unlock all " + (value ? "on" : "off")); } }

        public static bool Lockable(VehicleProfile p) => p != null && p.Unlock != null && (p.Unlock.StartsWith("egg-") || p.Unlock == "campaign");
        public static int Tier(VehicleProfile p) => Lockable(p) && p.Unlock.StartsWith("egg-") && int.TryParse(p.Unlock.Substring(4), out int n) ? n : 0;
        public static bool Earned(VehicleProfile p)
        {
            if (!Lockable(p)) return true;
            int n = Tier(p); if (n <= 0) return false;   // "campaign": Round 95
            ReadEggs(out int found, out var tiers); return found >= n || tiers.Contains(n);
        }
        public static bool Open => UnlockAll || HighMode.On || SplitScreen.Active;
        public static bool Locked(VehicleProfile p) => Lockable(p) && !Open && !Earned(p);
        public static bool Locked(string id) => Locked(VehicleProfile.Find(id));
        public static string HowTo(VehicleProfile p)
        {
            int n = Tier(p);
            if (n > 0) { ReadEggs(out int found, out _); return $"Find {n} goose eggs in Free Roam ({Mathf.Min(found, n)} / {n})"; }
            return "A prize in the campaign's events (coming next)";
        }
        public static VehicleProfile[] ForTier(int tier) => VehicleProfile.All.Where(p => Tier(p) == tier).ToArray();
        // the table for the status and the pause menu: tier -> vehicles
        public static string Table => string.Join("; ", VehicleProfile.All.Where(p => Tier(p) > 0).GroupBy(Tier).OrderBy(g => g.Key).Select(g => $"{g.Key} eggs: {string.Join(", ", g.Select(p => p.Name))}"))
            + "; campaign: " + string.Join(", ", VehicleProfile.All.Where(p => p.Unlock == "campaign").Select(p => p.Name));

        // the egg save, read from the hunt when it runs, else from its file (a vehicle is chosen before the hunt starts)
        static float readAt = -100; static int found; static List<int> tiers = new();
        static void ReadEggs(out int f, out List<int> t)
        {
            var hunt = EggHunt.Instance;
            if (hunt && hunt.Total > 0) { f = hunt.Found; t = hunt.TiersReached.ToList(); return; }
            if (Time.realtimeSinceStartup - readAt > 2f)
            {
                readAt = Time.realtimeSinceStartup; found = 0; tiers = new List<int>();
                try
                {
                    var path = Path.Combine(Path.GetDirectoryName(RaceController.RecordsPath()) ?? Application.persistentDataPath, EggHunt.SaveName);
                    if (File.Exists(path)) { var s = JsonUtility.FromJson<EggHunt.Save>(File.ReadAllText(path)); found = s?.found?.Count ?? 0; tiers = s?.tiers ?? new List<int>(); }
                }
                catch (Exception e) { Debug.LogWarning("[Unlocks] egg save unreadable: " + e.Message); }
            }
            f = found; t = tiers;
        }

        // ---------- announcing (EggHunt.TierReached and once at start for vehicles earned before this build)
        static string SavePath => Path.Combine(Path.GetDirectoryName(RaceController.RecordsPath()) ?? Application.persistentDataPath, SaveName);
        static Saved Load() { try { if (File.Exists(SavePath)) return JsonUtility.FromJson<Saved>(File.ReadAllText(SavePath)) ?? new Saved(); } catch (Exception) { } return new Saved(); }
        static void Store(Saved s) { try { AtomicSave.Write(SavePath, JsonUtility.ToJson(s, true)); } catch (Exception e) { Debug.LogWarning("[Unlocks] could not save: " + e.Message); } }
        public static string LastAnnounced { get; private set; } = "";
        // the names a tier's banner shows (and marks announced)
        public static string Announce(int tier)
        {
            var got = ForTier(tier); if (got.Length == 0) return "";
            var s = Load(); foreach (var p in got) if (!s.announced.Contains(p.Id)) s.announced.Add(p.Id); Store(s);
            LastAnnounced = string.Join(", ", got.Select(p => p.Name)); Debug.Log($"[Unlocks] tier {tier}: unlocked {LastAnnounced}");
            return LastAnnounced;
        }
        // earned before and never announced (an older save): one banner in Free Roam
        public static void AnnounceEarlier()
        {
            if (HighMode.On) return;
            var s = Load(); var earned = VehicleProfile.All.Where(p => Tier(p) > 0 && Earned(p) && !s.announced.Contains(p.Id)).ToArray();
            if (earned.Length == 0) return;
            foreach (var p in earned) s.announced.Add(p.Id); Store(s);
            LastAnnounced = string.Join(", ", earned.Select(p => p.Name));
            EggBanner.ShowText("UNLOCKED:  " + LastAnnounced.ToUpperInvariant(), "Your goose eggs earned it: choose it in the garage", true);
            Debug.Log($"[Unlocks] earned before: {LastAnnounced}");
        }
    }
}
