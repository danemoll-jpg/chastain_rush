using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace ChastainRush
{
    // Round 42 (Part A): how long each step of a map build takes. Every Lap logs "[BuildTime] <step> <s> s (total <s> s)"; Done writes
    // the list, slowest first, to Assets/Generated/build_times.txt (git-ignored, rebuilt with the scene).
    public static class BuildClock
    {
        static readonly Stopwatch total = new Stopwatch(), step = new Stopwatch();
        static readonly List<(string name, double s)> laps = new List<(string, double)>();
        static string title = "";

        public static void Start(string what)
        {
            title = what; laps.Clear(); total.Restart(); step.Restart();
        }

        public static void Lap(string name)
        {
            double s = step.Elapsed.TotalSeconds; step.Restart();
            laps.Add((name, s));
            Debug.Log($"[BuildTime] {name} {s:0.0} s (total {total.Elapsed.TotalSeconds:0.0} s)");
        }

        public static double Total => total.Elapsed.TotalSeconds;

        public static void Done(string file)
        {
            var lines = new List<string> { $"{title}: {total.Elapsed.TotalSeconds:0.0} s" };
            lines.AddRange(laps.OrderByDescending(l => l.s).Select(l => $"  {l.s,7:0.0} s  {l.name}"));
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllLines(file, lines);
            Debug.Log("[BuildTime] " + string.Join("\n", lines));
        }
    }
}
