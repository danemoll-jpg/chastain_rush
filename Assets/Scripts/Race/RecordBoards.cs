using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 34 (C1, C4): Racer's RecordBoards (Racer/Assets/Scripts/RecordBoards.cs), the Top 10 boards, saved
    // as Racer saves them (top-ten-v1.json, AtomicSave, a file that cannot be read is never overwritten). Trimmed: Racer's
    // categories name its tracks, vehicle, AI roster, traffic and weather; here a board is a route and a direction, and a race
    // board also its lap count ("<route>|<direction>|laps<N>"; a lap board drops the lap count, as Racer's LapCategory), the
    // vehicle a field of each entry. Racer's Migrate (its legacy records-*.json files) is replaced by MigrateBests: the
    // best lap and best total this game kept per route, direction, laps and vehicle in race_records.json become legacy entries
    // with no name and no date (Owner gives them the player's name). Loading never writes: the carried entries are in memory
    // until the next record or name is saved. Describe, the era views and RecordView are left behind (Racer-only tracks).
    public sealed class RecordBoards
    {
        [Serializable] public sealed class Entry
        {
            public string id, category, vehicle, date;
            public string name;   // Racer 0.94 Part A: who set it (empty: the save's owner, Owner names them)
            public bool race, legacy;
            public double seconds;
            public long order;
        }
        [Serializable] public sealed class Data
        {
            public int version = 1;
            public long sequence;
            public List<Entry> entries = new();
            public List<string> received = new(), migrations = new();
        }
        [Serializable] class OldEntry { public string key; public float best_lap, best_total; }
        [Serializable] class OldFile { public List<OldEntry> entries = new(); }

        public const string FileName = "top-ten-v1.json";
        readonly string path;
        Data data = new();
        bool readFailed;
        public string FilePath => path;
        public string Error { get; private set; }
        readonly HashSet<string> newEntries = new();
        public bool IsNew(string id) => newEntries.Contains(id);
        public void BeginAttempt() => newEntries.Clear();

        public static string Category(string route, string direction, int laps) => $"{route}|{direction}|laps{laps}";
        public static string LapCategory(string category) => Regex.Replace(category ?? "", @"\|laps\d+$", "");
        public static int RaceLaps(string category) { var m = Regex.Match(category ?? "", @"\|laps(\d+)$"); return m.Success ? int.Parse(m.Groups[1].Value) : 0; }
        static bool Valid(double n) => n > 0 && n < 31536000 && !double.IsNaN(n) && !double.IsInfinity(n);

        // directory: where race_records.json is (a check run's own folder in a check)
        public RecordBoards(string directory, string bestsFile)
        {
            path = System.IO.Path.Combine(directory, FileName);
            try
            {
                if (File.Exists(path))
                {
                    var loaded = JsonUtility.FromJson<Data>(File.ReadAllText(path));
                    if (loaded == null || loaded.version != 1 || loaded.entries == null || loaded.received == null || loaded.migrations == null) throw new InvalidDataException();
                    data = loaded;
                }
                MigrateBests(bestsFile);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { Error = "Record boards could not be read; existing file preserved."; readFailed = true; }
        }

        // a board: best first, exact ties in the order they were set
        public IReadOnlyList<Entry> Board(string category, bool race) => data.entries.Where(e => e.category == (race ? category : LapCategory(category)) && e.race == race)
            .OrderBy(e => e.seconds).ThenBy(e => e.order).Take(10).ToArray();
        // Racer 0.83 Part G (display only): a track's Top 10 across every race length, optionally one vehicle; the vehicles with an entry there
        public IReadOnlyList<Entry> Top(Func<string, bool> onTrack, bool race, string vehicle = null) => data.entries.Where(e => e.race == race && onTrack(e.category) && (vehicle == null || e.vehicle == vehicle))
            .OrderBy(e => e.seconds).ThenBy(e => e.order).Take(10).ToArray();
        public string[] TopVehicles(Func<string, bool> onTrack, bool race) => data.entries.Where(e => e.race == race && onTrack(e.category)).Select(e => e.vehicle).Distinct().OrderBy(v => v, StringComparer.Ordinal).ToArray();
        public bool Any => data.entries.Count > 0;
        public int Count => data.entries.Count;

        // Racer 0.94 Part A: entries from before names belong to the save's owner. Chastain Rush: written straight away (C4: Dan's
        // records carry his name once he gives it)
        public void Owner(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            bool changed = false;
            foreach (var e in data.entries) if (string.IsNullOrEmpty(e.name)) { e.name = name; changed = true; }
            if (changed) Write();
        }

        // the rank on its board (1 to 10), or 0 (not on it, a repeat, or the boards could not be read)
        public int Add(string id, string category, bool race, double seconds, string vehicle, string date = null, bool legacy = false, string name = null, bool write = true)
        {
            if (readFailed || string.IsNullOrEmpty(id) || !Valid(seconds)) return 0;
            if (!race) category = LapCategory(category);
            if (data.received.Contains(id)) return 0;
            var entry = new Entry { id = id, category = category, race = race, seconds = seconds, vehicle = vehicle, date = date, legacy = legacy, name = name ?? (legacy ? "" : PlayerNames.Player), order = ++data.sequence };
            data.received.Add(id); data.entries.Add(entry);
            var ordered = Board(category, race); int rank = Array.FindIndex(ordered.ToArray(), e => e.id == id) + 1;
            var retained = new HashSet<string>(ordered.Select(e => e.id));
            data.entries.RemoveAll(e => e.category == category && e.race == race && !retained.Contains(e.id));
            if (!legacy && rank > 0) newEntries.Add(id);
            if (write) Write();
            return rank;
        }

        // C4: race_records.json's bests ("<route>|<direction>|<laps>[|<vehicle>]"; no vehicle = the Street Classic), once
        void MigrateBests(string file)
        {
            const string Mark = "race_records.json";
            if (data.migrations.Contains(Mark) || string.IsNullOrEmpty(file) || !File.Exists(file)) return;
            OldFile old;
            try { old = JsonUtility.FromJson<OldFile>(File.ReadAllText(file)); } catch (ArgumentException) { return; }
            if (old?.entries == null) return;
            foreach (var o in old.entries)
            {
                var k = (o.key ?? "").Split('|');
                if (k.Length < 3 || !int.TryParse(k[2], out int laps)) continue;
                string vehicle = k.Length > 3 ? k[3] : "original", cat = Category(k[0], k[1], laps);
                Add("legacy/" + o.key + "/lap", cat, false, o.best_lap, vehicle, null, true, "", false);
                Add("legacy/" + o.key + "/race", cat, true, o.best_total, vehicle, null, true, "", false);
            }
            data.migrations.Add(Mark);   // in memory: saved with the next record or name
        }

        void Write()
        {
            if (readFailed) return;
            try { AtomicSave.Write(path, JsonUtility.ToJson(data, true)); Error = null; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { Error = "Could not save record boards; retained in memory."; }
        }
    }

    // Racer 0.94 Part A (PlayerNames.cs): the player's name (asked once on the title menu, changed in Settings > Gameplay), the
    // names used before on this PC. Chastain Rush: kept in GameSettings (settings.json; a check run's own folder in a check).
    // Racer's Adopt (an old save is "Dan") is left out: the round asks the name and Dan's carried records wait for it.
    public static class PlayerNames
    {
        public const int MaxLength = 12;
        static GameSettings.Options Settings => GameSettings.Settings;
        public static string Player => string.IsNullOrEmpty(Settings?.playerName) ? "Player" : Settings.playerName;
        public static bool Named => !string.IsNullOrEmpty(Settings?.playerName);
        public static string Clean(string value) => new string((value ?? "").Where(c => !char.IsControl(c)).Take(MaxLength).ToArray()).Trim();
        public static void Remember(string name)
        {
            var s = Settings; name = Clean(name); if (s == null || name.Length == 0) return;
            var list = (s.knownNames ?? new string[0]).Where(n => n != name).ToList(); list.Insert(0, name);
            s.knownNames = list.Take(12).ToArray();
        }
        public static bool SetPlayer(string value)
        {
            var s = Settings; var name = Clean(value); if (s == null || name.Length == 0) return false;
            s.playerName = name; Remember(name); GameSettings.SaveSettings();
            ChastainRush.RaceController.Boards?.Owner(name);   // Chastain Rush (C4): the carried records take the name
            return true;
        }
        public static readonly Color Human = new(1f, .78f, .28f), Machine = new(.86f, .93f, 1f);
    }

    // Racer's FinishPresentation (read only): what a finished race's best lap and total did on the boards
    public sealed class FinishPresentation
    {
        public sealed class Result
        {
            public string headline, comparison, id;
            public double time;
            public int rank;
            public string Line => headline + "   " + (rank > 0 ? "#" + rank + "  " : "") + ChastainRush.RaceController.Fmt((float)time) + "   " + comparison.Replace("\n", "   ");
        }
        double priorLapLeader, priorRaceLeader, priorLapPb, priorRacePb;
        public Result Lap { get; private set; }
        public Result Race { get; private set; }
        public void Begin(RecordBoards boards, string category, double bestLap, double bestTotal)
        {
            Lap = Race = null;
            priorLapLeader = boards?.Board(category, false).FirstOrDefault()?.seconds ?? 0; priorRaceLeader = boards?.Board(category, true).FirstOrDefault()?.seconds ?? 0;
            priorLapPb = bestLap; priorRacePb = bestTotal;
        }
        // Chastain Rush: the ranks as the race's records were added (RaceController)
        public void Finish(IList<float> lapTimes, int[] lapRanks, string attempt, double total, int raceRank)
        {
            if (lapTimes.Count == 0) return;
            int best = Enumerable.Range(0, lapTimes.Count).OrderBy(i => lapTimes[i]).First();
            Lap = Evaluate(lapTimes[best], best < lapRanks.Length ? lapRanks[best] : 0, priorLapLeader, priorLapPb, attempt + "/lap/" + (best + 1));
            Race = Evaluate(total, raceRank, priorRaceLeader, priorRacePb, attempt + "/race");
        }
        public static Result Evaluate(double time, int rank, double leader, double pb, string id = "")
        {
            bool record = leader > 0 && time < leader;
            string headline = record ? "NEW COURSE RECORD!" : leader <= 0 && rank > 0 ? "FIRST COURSE RECORD!" : leader > 0 && time == leader ? "Matched Best" : rank >= 2 && rank <= 3 ? "PODIUM TIME!" : rank >= 1 && rank <= 10 ? "TOP 10 TIME!" : pb > 0 && time < pb ? "NEW PERSONAL BEST!" : "Finished";
            double prior = record ? leader : pb; string comparison = prior <= 0 ? "First recorded time." : (record ? "Previous Course Best " : "Your Previous Best ") + ChastainRush.RaceController.Fmt((float)prior);
            if (prior > time) { double delta = prior - time; comparison += "\n" + (delta < .001 ? "Less than 0.001s faster." : delta.ToString("0.000") + "s faster."); }
            return new Result { headline = headline, comparison = comparison, time = time, rank = rank, id = id };
        }
    }
}
