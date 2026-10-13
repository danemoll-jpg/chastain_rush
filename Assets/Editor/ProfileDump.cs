using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace ChastainRush
{
    // Round 24 Part A2, item A1: reads a profiler capture written by the exe (ProfileRunner, -exeProfileRaw) and lists where
    // the main thread's time went: each marker's total and self time per frame, averaged over the capture.
    //   Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod ChastainRush.ProfileDump.Run -profileRaw <file.raw>
    // Writes <file>.txt beside the capture.
    public static class ProfileDump
    {
        public static void Run()
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-profileRaw");
            string raw = Path.GetFullPath(a[i + 1]);
            if (!ProfilerDriver.LoadProfile(raw, false)) { Debug.LogError("[ProfileDump] could not load " + raw); EditorApplication.Exit(1); return; }
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex, frames = 0;
            var frameMs = new List<(int f, float ms)>();
            var total = new Dictionary<string, double>(); var self = new Dictionary<string, double>(); var calls = new Dictionary<string, double>();
            for (int f = first; f <= last; f++)
            {
                using (var v = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnTotalTime, false))
                {
                    if (v == null || !v.valid) continue;
                    frames++;
                    frameMs.Add((f, v.frameTimeMs));
                    var stack = new Stack<(int id, string path)>(); var kids = new List<int>();
                    stack.Push((v.GetRootItemID(), ""));
                    while (stack.Count > 0)
                    {
                        var (id, path) = stack.Pop();
                        v.GetItemChildren(id, kids);
                        foreach (int k in kids)
                        {
                            string n = v.GetItemName(k), p = path.Length == 0 ? n : path + " > " + n;
                            if (p.Count(c => c == '>') > 9) continue;
                            Add(total, p, v.GetItemColumnDataAsSingle(k, HierarchyFrameDataView.columnTotalTime));
                            Add(self, n, v.GetItemColumnDataAsSingle(k, HierarchyFrameDataView.columnSelfTime));
                            Add(calls, p, v.GetItemColumnDataAsSingle(k, HierarchyFrameDataView.columnCalls));
                            stack.Push((k, p));
                        }
                    }
                }
            }
            var sb = new StringBuilder();
            sb.AppendLine($"{raw}: {frames} frames (main thread), ms per frame");
            sb.AppendLine("-- self time, top 40");
            foreach (var kv in self.OrderByDescending(x => x.Value).Take(40)) sb.AppendLine($"{kv.Value / frames,9:0.00}  {kv.Key}");
            sb.AppendLine("-- total time by path, top 80 (calls per frame)");
            foreach (var kv in total.OrderByDescending(x => x.Value).Take(80)) sb.AppendLine($"{kv.Value / frames,9:0.00}  ({calls[kv.Key] / frames:0.#})  {kv.Key}");
            sb.AppendLine("-- the five slowest frames: their top self times");
            foreach (var (f, ms) in frameMs.OrderByDescending(x => x.ms).Take(5))
            {
                sb.AppendLine($"frame {f}: {ms:0.0} ms");
                using (var v = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnSelfTime, false))
                {
                    var one = new Dictionary<string, double>(); var st = new Stack<int>(); var kids = new List<int>(); st.Push(v.GetRootItemID());
                    while (st.Count > 0) { v.GetItemChildren(st.Pop(), kids); foreach (int k in kids) { Add(one, v.GetItemName(k), v.GetItemColumnDataAsSingle(k, HierarchyFrameDataView.columnSelfTime)); st.Push(k); } }
                    foreach (var kv in one.OrderByDescending(x => x.Value).Take(6)) sb.AppendLine($"   {kv.Value,8:0.00}  {kv.Key}");
                }
            }
            File.WriteAllText(Path.ChangeExtension(raw, ".txt"), sb.ToString());
            Debug.Log("[ProfileDump] wrote " + Path.ChangeExtension(raw, ".txt"));
            EditorApplication.Exit(0);
        }

        static void Add(Dictionary<string, double> d, string k, double v) { d.TryGetValue(k, out var o); d[k] = o + v; }
    }
}
