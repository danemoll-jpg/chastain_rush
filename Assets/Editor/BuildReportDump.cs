using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ChastainRush
{
    // Round 92 (item 0): what fills each file of the last player build (Library/LastBuild.buildreport), by type and by source asset.
    // Editor batch: -executeMethod ChastainRush.BuildReportDump.Run -dumpOut <file>
    public static class BuildReportDump
    {
        public static void Run()
        {
            string outPath = Arg("-dumpOut") ?? "build_report_dump.txt";
            try
            {
                var report = BuildReport.GetLatestReport();
                if (report == null) throw new Exception("no last build report");
                using var w = new StreamWriter(outPath);
                foreach (var pa in report.packedAssets.OrderByDescending(p => p.overhead + (ulong)p.contents.Sum(c => (long)c.packedSize)))
                {
                    ulong total = (ulong)pa.contents.Sum(c => (long)c.packedSize);
                    w.WriteLine($"=== {pa.shortPath}  {total / 1048576.0:0.0} MB  {pa.contents.Length} objects");
                    foreach (var g in pa.contents.GroupBy(c => c.type?.Name ?? "?").OrderByDescending(g => g.Sum(c => (long)c.packedSize)).Take(15))
                        w.WriteLine($"  type {g.Key,-28} {g.Sum(c => (long)c.packedSize) / 1048576.0,10:0.0} MB  {g.Count()}");
                    foreach (var g in pa.contents.GroupBy(c => c.sourceAssetPath).OrderByDescending(g => g.Sum(c => (long)c.packedSize)).Take(40))
                        w.WriteLine($"  src  {g.Sum(c => (long)c.packedSize) / 1048576.0,10:0.0} MB  {g.Count(),6}  {g.Key}");
                    // the biggest single objects
                    foreach (var c in pa.contents.OrderByDescending(c => c.packedSize).Take(10))
                        w.WriteLine($"  obj  {c.packedSize / 1048576.0,10:0.0} MB  {c.type?.Name}  {c.sourceAssetPath}");
                }
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText(outPath, e.ToString()); EditorApplication.Exit(1); }
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }
    }
}
