using System;
using System.IO;
using UnityEngine;

namespace ChastainRush
{
    // Round 64 (A, Dan 2026-10-23: "I know it's picky but I wanted the reverse track to be the forward track"): the Noonday Creek Trail's two directions
    // swapped in Data/routes/noonday_creek_trail.json (Tools/mapgen/make_trail_route.py). Once, beside the race records, everything already saved under one
    // direction moves to the other, so each time stays with the way it was driven: the Top 10 boards (top-ten-v1.json), the race records
    // (race_records.json), the clean-lap ghosts (CleanLapGhosts\) and the replays (Replays\best\, Replays\saved\). A copy of each file changed is kept
    // (*.before-round64). A marker file says it is done.
    public static class NoondayTrailSwap
    {
        const string Track = "Noonday Creek Trail", Marker = "noonday-trail-directions-swapped-r64.txt";

        public static void Run(string dir)
        {
            try
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir) || File.Exists(Path.Combine(dir, Marker))) return;
                int n = 0;
                // the boards and the records: "Noonday Creek Trail|forward..." and "...|reverse..."
                foreach (var f in new[] { "top-ten-v1.json", "race_records.json" })
                {
                    string p = Path.Combine(dir, f); if (!File.Exists(p)) continue;
                    string s = File.ReadAllText(p), t = SwapText(s, Track + "|");
                    if (t != s) { File.Copy(p, p + ".before-round64", true); File.WriteAllText(p, t); n++; }
                }
                // the ghosts: "Noonday Creek Trail_forward_..." files, their key "Noonday Creek Trail/forward/..."
                string gd = Path.Combine(dir, "CleanLapGhosts");
                if (Directory.Exists(gd)) n += SwapFiles(gd, Track + "_", "*.json", (from, to) => { string s = File.ReadAllText(from); File.WriteAllText(to, SwapText(s, Track + "/")); });
                // the replays: "Noonday Creek Trail_forward.lap.crreplay" (best), "Noonday Creek Trail forward <date>.crreplay" (saved); their direction and category
                foreach (var (sub, prefix) in new[] { ("best", Track + "_"), ("saved", Track + " ") })
                {
                    string rd = Path.Combine(dir, "Replays", sub); if (!Directory.Exists(rd)) continue;
                    n += SwapFiles(rd, prefix, "*.crreplay", (from, to) =>
                    {
                        var d = ReplayData.Read(from); d.direction = Flip(d.direction); d.category = SwapText(d.category ?? "", Track + "|"); d.Write(to);
                    });
                }
                File.WriteAllText(Path.Combine(dir, Marker), $"Round 64: the Noonday Creek Trail's forward and reverse swapped in the saved times, ghosts and replays ({n} files) {DateTime.Now:o}\n");
                Debug.Log($"[Round64] the Noonday Creek Trail's saved directions swapped: {n} files");
            }
            catch (Exception e) { Debug.LogWarning("[Round64] swapping the Noonday Creek Trail's saved directions: " + e.Message); }
        }

        static string Flip(string dirTag) => string.IsNullOrEmpty(dirTag) ? dirTag : dirTag.StartsWith("forward") ? "reverse" + dirTag.Substring(7) : dirTag.StartsWith("reverse") ? "forward" + dirTag.Substring(7) : dirTag;
        // "<head>forward" <-> "<head>reverse" wherever head is followed by a direction
        static string SwapText(string s, string head) =>
            s.Replace(head + "forward", head + "\u0001").Replace(head + "reverse", head + "forward").Replace(head + "\u0001", head + "reverse");

        // files named <prefix>forward... and <prefix>reverse... in dir: each written under the other direction's name by 'write', the old ones kept as
        // *.before-round64 (one pass through a temp name, so a forward and a reverse file swap places)
        static int SwapFiles(string dir, string prefix, string pattern, Action<string, string> write)
        {
            int n = 0; var moved = new System.Collections.Generic.List<(string tmp, string to)>();
            foreach (var p in Directory.GetFiles(dir, pattern))
            {
                string name = Path.GetFileName(p); string to;
                if (name.StartsWith(prefix + "forward")) to = prefix + "reverse" + name.Substring(prefix.Length + 7);
                else if (name.StartsWith(prefix + "reverse")) to = prefix + "forward" + name.Substring(prefix.Length + 7);
                else continue;
                string tmp = p + ".r64tmp"; write(p, tmp); File.Copy(p, p + ".before-round64", true); File.Delete(p); moved.Add((tmp, Path.Combine(dir, to))); n++;
            }
            foreach (var (tmp, to) in moved) { if (File.Exists(to)) File.Delete(to); File.Move(tmp, to); }
            return n;
        }
    }
}
