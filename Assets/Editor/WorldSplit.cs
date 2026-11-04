using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChastainRush
{
    // Round 92 (item 0): the player build's scene list. Dan's installed launcher refuses any release file of 2 GiB or more, and with the whole world in
    // one scene Unity wrote 2.19 GB into sharedassets0.assets (meshes, terrain) and 2.0 GB of static batches into level0.resS. Now:
    //   0  Boot          an empty scene whose WorldBoot loads the world scene at once
    //   1..k  Data i     never loaded: each refers to a share of the world scene's big assets (mesh packs, terrain tiles, textures) so Unity writes
    //                    that share to its own sharedassets<i>.assets (an asset goes to the first scene in the list that uses it)
    //   k+1 the world    ChastainPrototype, unchanged
    // Static batching is off (ProjectSettings: its combined meshes would be one file of the world scene, too big to split).
    public static class WorldSplit
    {
        const string Dir = "Assets/Generated/BuildScenes";
        // a share's size on disk; packed into the build a mesh pack takes about half its file, a terrain tile about four fifths
        const long ShareDiskBytes = 1_500_000_000L;
        public const long FileWarnBytes = 1_717_986_918L;    // 1.6 GiB: the aim for every file of a release
        public const long FileLimitBytes = 1_932_735_283L;   // 1.8 GiB: the publisher refuses a release with a file this size

        static readonly string[] Keep = { ".cs", ".unity", ".shader", ".shadergraph", ".hlsl", ".cginc", ".mat", ".physicmaterial", ".physicsmaterial", ".terrainlayer", ".asmdef", ".dll" };

        public static string[] Make(string worldScene)
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Generated", "BuildScenes");
            foreach (var old in Directory.GetFiles(Dir, "*.unity")) AssetDatabase.DeleteAsset(old.Replace('\\', '/'));
            var deps = AssetDatabase.GetDependencies(worldScene, true)
                .Where(p => p != worldScene && p.StartsWith("Assets/") && !Keep.Contains(Path.GetExtension(p).ToLowerInvariant()))
                .OrderBy(p => p, StringComparer.Ordinal).ToList();
            // shares in path order (the mesh packs in the order they were written: neighbours in the world end up together)
            var shares = new List<List<string>>(); long size = 0;
            foreach (var p in deps)
            {
                long b = new FileInfo(p).Length;
                if (shares.Count == 0 || size + b > ShareDiskBytes) { shares.Add(new List<string>()); size = 0; }
                shares[shares.Count - 1].Add(p); size += b;
            }
            var list = new List<string> { Scene("Boot", go => go.AddComponent<WorldBoot>().worldScene = Path.GetFileNameWithoutExtension(worldScene)) };
            for (int i = 0; i < shares.Count; i++)
            {
                var objs = shares[i].SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p)).Where(o => o != null && !(o is GameObject) && !(o is Component)).ToArray();
                list.Add(Scene($"Data{i + 1}", go => go.AddComponent<WorldDataShare>().assets = objs));
                Debug.Log($"[WorldSplit] Data{i + 1}: {shares[i].Count} files, {shares[i].Sum(p => new FileInfo(p).Length) / 1048576.0:0} MB on disk, {objs.Length} objects");
            }
            list.Add(worldScene);
            return list.ToArray();
        }

        static string Scene(string name, Action<GameObject> fill)
        {
            // a scene of its own (Single: the world scene is saved by the map build; the player build reads every scene from its file)
            for (int i = 0; i < SceneManager.sceneCount; i++) { var open = SceneManager.GetSceneAt(i); if (open.isDirty && !string.IsNullOrEmpty(open.path)) EditorSceneManager.SaveScene(open); }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject(name); fill(go);
            string path = $"{Dir}/{name}.unity";
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("could not save " + path);
            return path;
        }

        // after the build: every file of the data folder against the launcher's limit (the biggest listed in BUILD_INFO.txt)
        public static List<string> CheckFiles(string buildDir, out bool tooBig)
        {
            var lines = new List<string>(); tooBig = false;
            foreach (var f in new DirectoryInfo(buildDir).GetFiles("*", SearchOption.AllDirectories).OrderByDescending(f => f.Length).Take(8))
            {
                string mark = f.Length >= FileLimitBytes ? "  TOO BIG (the launcher's limit is 2 GiB; the publisher refuses 1.8 GiB)" : f.Length >= FileWarnBytes ? "  over the 1.6 GiB aim" : "";
                if (f.Length >= FileLimitBytes) tooBig = true;
                lines.Add($"  {Path.GetRelativePath(buildDir, f.FullName)}: {f.Length / 1048576.0:0} MB{mark}");
            }
            return lines;
        }
    }
}
