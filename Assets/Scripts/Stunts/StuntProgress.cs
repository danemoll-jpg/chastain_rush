using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Racer
{
    // 0.106 Parts C and D: the stunt park's own save (stunt-park-v1.json beside the other saves): the best single combo in Free Roam (drop-in), the
    // Stunt event's Top 10 (every result, under the player's name), and whether a Gold has been won. Nothing else is recorded; the races' records
    // never see it. 0.108 Part D: and the Top 10 Best Combos (combos), in the same file; the single best combo is still kept and read as before.
    // Chastain Rush, Round 36: ported as it is (no unlock notice: nothing is locked here, the Gold is only recorded). A check run's file is in its
    // own folder (RaceController.RecordsPath), so a check never writes the player's.
    public static class StuntProgress
    {
        public const string File = "stunt-park-v1.json";
        public const int Minutes = 10;
        // the Stunt event's medals (points in 10 minutes): Bronze, Silver, Gold (Racer's, tuned on its 0.106 checks; Gold takes real effort)
        public static readonly int[] Targets = { 20000, 50000, 90000 };
        public static int MedalFor(int points) => points >= Targets[2] ? 3 : points >= Targets[1] ? 2 : points >= Targets[0] ? 1 : 0;
        [Serializable] public sealed class Entry { public string name, vehicle, date; public int points, medal, bestCombo; }
        // 0.108 Part D: the Top 10 Best Combos (the Stunt event and Free Roam in the park; links 0: the old single best, links not kept)
        [Serializable] public sealed class Combo { public string name, vehicle, tricks, date; public int points, links; }
        [Serializable] sealed class Data { public int version = 1; public bool goldEarned; public int bestCombo; public string bestComboVehicle, bestComboDate; public List<Entry> top = new(); public List<Combo> combos = new(); }
        static Data data = new(); static string path;
        public static bool GoldEarned => data.goldEarned;
        public static int BestCombo => data.bestCombo;
        public static string BestComboVehicle => data.bestComboVehicle;
        public static string BestComboDate => data.bestComboDate;
        public static IReadOnlyList<Entry> Top => data.top;
        public static string FilePath => path;
        public static void Load(string root)
        {
            data = new Data(); path = root != null ? Path.Combine(root, File) : null;
            try { if (path != null && System.IO.File.Exists(path)) data = JsonUtility.FromJson<Data>(System.IO.File.ReadAllText(path)) ?? new Data(); } catch (Exception) { data = new Data(); }
            data.top ??= new List<Entry>(); data.combos ??= new List<Combo>();
            // 0.108 Part D: a save from before the list starts it with what it kept. In memory; loading never writes.
            if (data.combos.Count == 0)
            {
                if (data.bestCombo > 0) data.combos.Add(new Combo { name = "", vehicle = data.bestComboVehicle, points = data.bestCombo, tricks = "", date = data.bestComboDate });
                foreach (var e in data.top) if (e.bestCombo > 0) data.combos.Add(new Combo { name = e.name, vehicle = e.vehicle, points = e.bestCombo, tricks = "", date = e.date });
                var seeded = data.combos; data.combos = seeded.OrderByDescending(x => x.points).ThenBy(x => seeded.IndexOf(x)).Take(10).ToList();
            }
        }
        public static IReadOnlyList<Combo> Combos => data.combos;
        // 0.109 Part B: the entry the last banked combo made in the Top 10 (null: none), so the Stunt event results can highlight this event's entries
        public static Combo LastComboEntry { get; private set; }
        // a banked combo: its Top 10 place (0 = none)
        public static int RecordComboTop(string name, string vehicle, int points, int links, string tricks)
        {
            LastComboEntry = null;
            if (points <= 0) return 0;
            var c = new Combo { name = name, vehicle = vehicle, points = points, links = links, tricks = tricks, date = DateTime.Now.ToString("yyyy-MM-dd") };
            var all = data.combos.Append(c).ToList(); var top = all.OrderByDescending(x => x.points).ThenBy(x => all.IndexOf(x)).Take(10).ToList();
            int place = top.IndexOf(c) + 1; if (place == 0) return 0;
            data.combos = top; LastComboEntry = c; Save(); return place;
        }
        static void Save() { try { if (path != null) AtomicSave.Write(path, JsonUtility.ToJson(data, true)); } catch (Exception e) { Debug.LogWarning("Stunt park progress not saved: " + e.Message); } }
        public static bool RecordCombo(int points, string vehicle)
        {
            if (points <= data.bestCombo) return false;
            data.bestCombo = points; data.bestComboVehicle = vehicle; data.bestComboDate = DateTime.Now.ToString("yyyy-MM-dd"); Save(); return true;
        }
        // a finished Stunt event: its entry (StuntEvent reads its place once it is in: PlaceOf)
        public static Entry Finish(string name, string vehicle, int points, int bestCombo)
        {
            var e = new Entry { name = name, vehicle = vehicle, points = points, medal = MedalFor(points), bestCombo = bestCombo, date = DateTime.Now.ToString("yyyy-MM-dd") };
            data.top.Add(e); var top = data.top.OrderByDescending(x => x.points).ThenBy(x => data.top.IndexOf(x)).Take(10).ToList(); data.top = top;
            if (e.medal >= 3) data.goldEarned = true;
            Save(); return e;
        }
        // 0.110 Part A: an entry's place in the Stunt event Top 10 now (0 = none, or pushed out)
        public static int PlaceOf(Entry e) => e == null ? 0 : data.top.IndexOf(e) + 1;
    }
}
