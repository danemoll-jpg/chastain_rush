using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Round 42 (Part A, Dan 2026-10-18: "Give this project a fast path that fits its generated pipeline"): QUICK BUILDS.
// The full Build Map (MapBuilder.Build) stays the reference. It is the planning (Plan: the ground graded, everything laid out; about a minute)
// and then the steps below, each a builder of the scene. While it runs it records, for every step, the objects it made (their place in the
// hierarchy) and every terrain sample (height or hole) it changed, before and after (BuildCache/steps/, git-ignored).
// A quick build rebuilds only some steps, on the saved scene:
//   1. open the saved scene; replay the planning on a terrain in memory (nothing in the scene touched; the planning is deterministic);
//   2. take the scene's own terrain and undo, step by step from the last, the samples every step from the first chosen one on changed: the
//      ground exactly as it was before that step;
//   3. delete the chosen steps' objects, run those steps again (from the current code and data) into the scene, each object back in its place
//      in the hierarchy; the steps not chosen in between and after get their recorded samples back (a sample a rebuilt step now changes
//      that a later step also changed is a conflict: reported, and a full build is then needed);
//   4. write the ground back into the scene's terrain, save. Then, if asked, the exe (PlayerBuild with the scene kept).
// Why steps and not a box of the map: the world's builders are map-wide (roads, houses, businesses and trees are meshes in 128 m cells
// mixing everything there), so rebuilding "everything in a box" means running nearly the whole build. A fix is almost always in one
// builder (the crypt, a roof ramp on the ridge, a pipe's exit): rebuilding that builder is the fast path. A change to the planning (the
// ground's grading, the layout of houses or roads) still needs the full build; so does a step not marked quick below (it reads what other
// steps made). The check that a quick build is right: ChastainRush.SceneDump of it and of a full build, compared (Tools/scene_diff.py).
//   Batch: Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod ChastainRush.MapBuilder.QuickFromCommandLine -quickParts <steps>
//          [-dumpOut <file>]       (ChastainRush.MapBuilder.QuickExeFromCommandLine: the same, then the exe, as PlayerBuild with the scene kept)
//   <steps>: step names (QuickSteps lists them; "church" = Businesses,ChurchCrypt), comma separated.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const string QuickTerrainRoot = "QuickBuild planning terrain (temporary)";
        static string QuickStep;   // the step a quick build is running (null in a full build)

        // every generated asset is written through this. A full build: AssetDatabase.CreateAsset, as always. A quick build: a mesh goes to
        // Meshes/q/<step>/ (never over a mesh another step's objects use); any other asset that exists already (a material, a texture, a physics
        // material) gets the new one's contents and keeps its file and GUID, so the objects of the steps not rebuilt still find it
        static T GenAsset<T>(T obj, string path) where T : UnityEngine.Object
        {
            if (QuickStep == null) { AssetDatabase.CreateAsset(obj, path); return obj; }
            string meshes = GeneratedDir + "/Meshes/";
            if (path.StartsWith(meshes))
            {
                string dir = GeneratedDir + "/Meshes/q/" + QuickStep;
                if (!AssetDatabase.IsValidFolder(GeneratedDir + "/Meshes/q")) AssetDatabase.CreateFolder(GeneratedDir + "/Meshes", "q");
                if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(GeneratedDir + "/Meshes/q", QuickStep);
                AssetDatabase.CreateAsset(obj, dir + "/" + path.Substring(meshes.Length)); return obj;
            }
            var old = AssetDatabase.LoadAssetAtPath<T>(path);
            if (old == null) { AssetDatabase.CreateAsset(obj, path); return obj; }
            string name = old.name; EditorUtility.CopySerialized(obj, old); old.name = name; EditorUtility.SetDirty(old);
            return old;
        }
        static string StepsDir => Path.Combine(ProjectRoot, "BuildCache", "steps");

        sealed class Step { public string name; public Action run; public bool quick; public Action prep; }

        static readonly Dictionary<string, string[]> QuickAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "church", new[] { "Businesses", "ChurchCrypt" } },   // church 74, its crypt (the crypt reads the church's hollow wings, made with the businesses)
        };

        // the builders, in order. quick: may be rebuilt alone (it reads only the planning; checked with SceneDump against a full build)
        static List<Step> Steps(Ctx c)
        {
            var map = c.map; var terrain = c.terrain; var mats = c.mats; var world = c.world; var hood = c.hood; var shop = c.shop; var rest = c.rest; var biz = c.biz;
            var creek = c.creek; var ridge = c.ridge; var pipes = c.pipes; var net = c.net; var lakes = c.lakes; var courson = c.courson;
            Step S(string name, Action run, bool quick = false, Action prep = null) => new Step { name = name, run = run, quick = quick, prep = prep };
            return new List<Step>
            {
                S("Roads", () =>
                {
                    BuildRoads(map, terrain, mats, world, c.paint, hood, biz);
                    BuildBridges(map, mats, world, c.paint, terrain);
                    c.paint.ToObjects("RoadPaint", mats.Paint, world, false);
                }),
                S("RoadData", () => { BuildRoadLookup(map); BuildTrafficNetwork(map); }),   // Round 30 (A1): the road graph the traffic drives on (MapBuilder.Traffic.cs)
                S("Buildings", () => BuildBuildings(map, terrain, mats, world, LoadOwner(), hood, shop, rest, biz)),
                S("Driveways", () => BuildDriveways(map, terrain, mats, world, hood)),
                S("Water", () => { BuildWater(map, terrain, mats, world, hood); RegisterWater(map, world, new Pavement(map)); }),   // Round 15 (item 2c)
                S("Trees", () =>
                {
                    // Round 10 (item 7): trees are solid now, so no trunk may stand on a road or a drive anywhere on the map (the area's
                    // own rules already keep them off its streets, drives and the shortcut lines): one standing within 0.6 m of a
                    // road's or a drive's edge is moved out to 1.5 m past the edge, or dropped if that is on pavement too
                    var pave = c.pave = new Pavement(map);
                    biz.Trees.AddRange(MedianTrees(map, ridge));   // Round 40 (B2): trees down New Chastain Road's median
                    var trees = c.trees = TreeBuilder.Build(Path.Combine(ProjectRoot, "Data/trees.bin"), (x, z) => terrain.Height(x, z), vScale, GeneratedDir, mats.Foliage, world,
                        (x, z) => hood.RejectTree(x, z) || pave.On(new Vector2(x, z), 0.6f) || Beds.Wet(x, z) || shop.RejectTree(x, z) || (creek != null && creek.RejectTree(x, z)) || pipes.RejectTree(x, z) || (net != null && net.RejectTree(x, z)) || rest.RejectTree(x, z) || biz.RejectTree(x, z) || lakes.RejectTree(x, z) || (courson != null && courson.RejectTree(x, z)) || (ridge != null && ridge.RejectTree(x, z)) || (LakesNow != null && LakesNow.RejectTree(x, z)) || (AddisonNow != null && AddisonNow.RejectTree(x, z)),   // Round 15: none in the water; Round 17: none on the parking lot; Round 18: off the trail's tread and out of the pipe cuts; Round 52: off Addison Heights' road edges
                        (x, z) => pave.MoveOff(hood.MoveTree(x, z)), biz.Trees);   // Round 31: and a few round the businesses
                    if (creek != null || ridge != null)   // Round 19 (1): the trees left within break_half_m of the Noonday Creek trail snap when hit (TreeTrunks); Round 33: and those past the Ridge leap's landing
                    {
                        var f = trees.Field; var br = new List<int>(); int nr = 0;
                        for (int i = 0; i < f.count; i++)
                        {
                            var t = TreeField.Unpack(f.data, i);
                            if (creek != null && creek.Breakable(t.x, t.z)) br.Add(i);
                            else if (ridge != null && ridge.Breakable(t.x, t.z)) { br.Add(i); nr++; }
                        }
                        f.breakable = br.ToArray();
                        Debug.Log($"[Round19] Noonday Creek trail: {br.Count - nr} trees within {creek?.Data.break_half_m ?? 0f:0.0} m of its line are breakable; [Round33] {nr} past the Ridge leap's landing");
                    }
                    trees.Field.gameObject.AddComponent<TreeTrunks>();
                    Debug.Log($"[Round10] solid trees: {pave.Moved} trunks moved off a road or drive, {pave.Dropped} on pavement with no room beside dropped; trunk colliders: a pool of 160 round the car (TreeTrunks)");
                }),
                S("Neighborhood", () => BuildNeighborhood(map, terrain, mats, world, hood, c.trees)),
                S("Rest", () => BuildRest(map, terrain, mats, world, rest)),   // Round 30 (B)
                S("Businesses", () => BuildBusinesses(map, terrain, mats, world, biz), true),   // Round 31 (4)
                S("ChurchCrypt", () => BuildChurchCrypt(terrain, mats, world), true),   // Round 41 (A): church 74 hollow, the crypt under it and the graveyard (MapBuilder.ChurchCrypt.cs)
                S("ChastainLakes", () => BuildChastainLakes(map, terrain, mats, world, lakes)),   // Round 32 (C2)
                S("CoursonSwim", () => BuildCoursonSwim(map, terrain, mats, world, courson)),   // Round 39 (A4)
                S("ShoppingCenter", () => BuildShoppingCenter(map, terrain, mats, world, shop)),   // Round 17 (Part B): lot, shop fronts, gas station, the bank
                S("StuntPark", () => { if (Park != null) BuildStuntPark(map, terrain, mats, world, Park, shop); }),   // Round 36 (Part A2): the stunt park, its stores and roofs
                S("Shortcuts", () => BuildShortcuts(hood, terrain, mats, world)),   // Round 8: the official shortcuts' ramps, decks and trail (MapBuilder.Shortcuts.cs)
                S("ReportTreesOnLines", () => { if (hood.ShortcutData != null) ReportTreesOnLines(hood, c.trees); }),
                S("CreekTrack", () => BuildCreekTrack(map, terrain, mats, world, creek)),   // Round 18 (3): tread, J4's kicker, logs, landing zones (after the shortcuts: their ramp material)
                // Round 33: the Chastain Ridge race's dirt piles and landing zones; Round 41: the roofer's ramp on house 1092 (its roof's mesh made by Rest:
                // a quick build makes it again from the planning)
                S("Ridge", () => BuildRidge(terrain, mats, world, ridge), true, () => QuickRoofRampHouse(rest)),
                S("Pipes", () => BuildPipes(terrain, mats, world, pipes), true),   // Round 18 (4)
                S("Network", () => BuildNetwork(terrain, mats, world, net, pipes)),   // Round 23 follow-ups: the network's pipes, chambers, the hub, manholes (MapBuilder.Network.cs)
                S("LakesRace", () => BuildLakesRace(map, terrain, mats, world)),   // Round 44: the Chastain Lakes race's trail, lake drain, spoil pile (MapBuilder.LakesRace.cs)
                S("Hydrants", () => BuildMoreHydrants(map, terrain, mats, world, hood, rest, biz, creek, ridge)),   // Round 36 (Parts C, D)
                S("Life", () =>
                {
                    try { BuildLife(map, terrain, world, hood, rest, biz, lakes, creek, ridge, c.trees.Field, LoadOwner()); }   // Round 38 (Part E): Racer's ambient life, where it lives (MapBuilder.Life.cs)
                    catch (Exception e) { Debug.LogError("[Round38] life not built: " + e); }
                }),
                S("MapPicture", () =>
                {
                    if (pipes.TreesRemoved > 0) Debug.Log($"[Round18] {pipes.TreesRemoved} trees kept out of the pipe cuts");
                    BakeMapPicture(map, terrain, shop, creek, c.trees.Field);   // Round 20 (2): the picture under the in-game map (MapBuilder.MapPicture.cs)
                }),
                S("GameLayer", () =>
                {
                    new GameObject("MapCredits").AddComponent<MapCredits>().text = "© OpenStreetMap contributors";
                    BuildRace(mats);
                    BuildActivities(terrain, mats);   // Round 36 (Part B): Racer's Free Roam activities, game layer (MapBuilder.Activities.cs)
                    BuildEggs();                      // Round 48: the goose egg hunt and the map's places, game layer (MapBuilder.Eggs.cs)
                    BuildPolice(map);                 // Round 39 (Part D): the police's roads and radio script, game layer (MapBuilder.Police.cs)
                    BuildProposalMarkers(terrain, world);   // Round 23 (Part B): TEMPORARY proposal markers, hidden until the debug menu shows them
                }),
                S("Player", () => PlayerSetup.Place(map.spawn.x, map.spawn.z, map.spawn.heading_deg, terrain.Height(map.spawn.x, map.spawn.z), terrain.BaseY)),
            };
        }

        // a quick build of Ridge without Rest: house 1092's mesh (for the roof ramp's supports), made from the planning as BuildRest makes it
        static void QuickRoofRampHouse(RestNeighborhoods rest)
        {
            if (RoofRampHouse != null || RoofRampHouseIndex < 0) return;
            var h = rest.Houses.FirstOrDefault(q => q.index == RoofRampHouseIndex); if (h == null) return;
            h.detail = new MeshBuilder();
            if (rest.Foundation.Contains(h.index)) FoundationTint = FoundationBlock;
            var tm = new MeshBuilder(); BuildHouse(tm, h); FoundationTint = null; RoofRampHouse = tm;
        }

        // ------------------------------------------------------------ what each step made and changed

        [Serializable] class StepRoot { public string path; public int index; }
        [Serializable] class StepRec { public string name; public bool quick, touchesOthers; public StepRoot[] roots; public int heights, holes; public float seconds; [NonSerialized] public List<Transform> made; }
        [Serializable] class StepManifest { public string built; public long sceneTicks; public StepRec[] steps; }

        sealed class StepRecorder
        {
            readonly Ctx c; readonly bool save;
            float[][,] h; bool[][,] holes;
            public readonly List<StepRec> Recs = new List<StepRec>();
            public StepRecorder(Ctx c, bool save) { this.c = c; this.save = save; if (save) Directory.CreateDirectory(StepsDir); Resync(); }

            IEnumerable<(int tx, int tz, TerrainData d)> Tiles()
            {
                for (int tz = 0; tz < c.terrain.Tiles.GetLength(1); tz++) for (int tx = 0; tx < c.terrain.Tiles.GetLength(0); tx++) yield return (tx, tz, c.terrain.Tiles[tx, tz].terrainData);
            }
            int Index(int tx, int tz) => tz * c.terrain.Tiles.GetLength(0) + tx;
            public void Resync()
            {
                int n = c.terrain.Tiles.Length; h = new float[n][,]; holes = new bool[n][,];
                foreach (var (tx, tz, d) in Tiles()) { h[Index(tx, tz)] = d.GetHeights(0, 0, d.heightmapResolution, d.heightmapResolution); holes[Index(tx, tz)] = d.GetHoles(0, 0, d.holesResolution, d.holesResolution); }
            }

            static HashSet<Transform> Roots(Ctx c)
            {
                var s = new HashSet<Transform>(c.scene.GetRootGameObjects().Select(g => g.transform));
                if (c.world) foreach (Transform t in c.world) s.Add(t);
                return s;
            }
            static int CountAll(Ctx c) => c.scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<Transform>(true).Length);

            public StepRec Run(Step st)
            {
                var before = Roots(c); int all0 = CountAll(c); var t0 = DateTime.Now;
                st.run();
                BuildClock.Lap(st.name);
                var made = Roots(c).Where(t => !before.Contains(t)).ToList();
                int inMade = made.Sum(t => t.GetComponentsInChildren<Transform>(true).Length);
                var rec = new StepRec { name = st.name, quick = st.quick, seconds = (float)(DateTime.Now - t0).TotalSeconds, touchesOthers = CountAll(c) - all0 != inMade };
                rec.roots = made.Select(t => new StepRoot { path = PathOf(t), index = t.GetSiblingIndex() }).ToArray(); rec.made = made;
                // the terrain samples it changed: before and after
                using (var hw = save ? new BinaryWriter(File.Create(Path.Combine(StepsDir, st.name + ".heights"))) : null)
                using (var ow = save ? new BinaryWriter(File.Create(Path.Combine(StepsDir, st.name + ".holes"))) : null)
                {
                    foreach (var (tx, tz, d) in Tiles())
                    {
                        int i = Index(tx, tz), res = d.heightmapResolution, hr = d.holesResolution;
                        var nh = d.GetHeights(0, 0, res, res); var oh = h[i];
                        for (int z = 0; z < res; z++) for (int x = 0; x < res; x++) if (nh[z, x] != oh[z, x]) { rec.heights++; hw?.Write((byte)tx); hw?.Write((byte)tz); hw?.Write((ushort)x); hw?.Write((ushort)z); hw?.Write(oh[z, x]); hw?.Write(nh[z, x]); }
                        var nb = d.GetHoles(0, 0, hr, hr); var ob = holes[i];
                        for (int z = 0; z < hr; z++) for (int x = 0; x < hr; x++) if (nb[z, x] != ob[z, x]) { rec.holes++; ow?.Write((byte)tx); ow?.Write((byte)tz); ow?.Write((ushort)x); ow?.Write((ushort)z); ow?.Write(ob[z, x]); ow?.Write(nb[z, x]); }
                        h[i] = nh; holes[i] = nb;
                    }
                }
                Recs.Add(rec);
                return rec;
            }

            public void SaveManifest()
            {
                var m = new StepManifest { built = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), sceneTicks = File.GetLastWriteTimeUtc(Path.Combine(ProjectRoot, ScenePath)).Ticks, steps = Recs.ToArray() };
                File.WriteAllText(Path.Combine(StepsDir, "manifest.json"), JsonUtility.ToJson(m, true));
            }
        }

        static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : "/" + t.name;

        // one recorded change of a terrain sample (a height, or a hole as 0 / 1)
        struct TerrainSample { public int tx, tz, x, z; public float before, after; }
        static List<TerrainSample> ReadSamples(string step, bool holes)
        {
            var list = new List<TerrainSample>(); var f = Path.Combine(StepsDir, step + (holes ? ".holes" : ".heights")); if (!File.Exists(f)) return list;
            using (var r = new BinaryReader(File.OpenRead(f)))
                while (r.BaseStream.Position < r.BaseStream.Length)
                {
                    var s = new TerrainSample { tx = r.ReadByte(), tz = r.ReadByte(), x = r.ReadUInt16(), z = r.ReadUInt16() };
                    if (holes) { s.before = r.ReadBoolean() ? 1f : 0f; s.after = r.ReadBoolean() ? 1f : 0f; } else { s.before = r.ReadSingle(); s.after = r.ReadSingle(); }
                    list.Add(s);
                }
            return list;
        }

        // ------------------------------------------------------------ the quick build

        public static void QuickFromCommandLine()
        {
            try { Quick(QuickArg("-quickParts") ?? ""); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void QuickExeFromCommandLine()
        {
            try { PlayerBuild.KeepScene = true; Quick(QuickArg("-quickParts") ?? ""); PlayerBuild.Build(); BuildClock.Lap("PlayerBuild (the exe)"); BuildClock.Done(Path.Combine(ProjectRoot, GeneratedDir, "quick_times.txt")); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        static string QuickArg(string name) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

        public static void Quick(string parts)
        {
            BuildClock.Start("Quick build " + parts);
            var mf = Path.Combine(StepsDir, "manifest.json");
            if (!File.Exists(mf)) throw new Exception("quick build: no step record (BuildCache/steps/manifest.json); run a full Build Map first");
            var man = JsonUtility.FromJson<StepManifest>(File.ReadAllText(mf));
            long ticks = File.GetLastWriteTimeUtc(Path.Combine(ProjectRoot, ScenePath)).Ticks;
            if (ticks != man.sceneTicks) throw new Exception("quick build: the saved scene is not the one the step record belongs to (it was built or saved since); run a full Build Map");
            var want = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in parts.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(q => q.Trim()))
                if (QuickAliases.TryGetValue(p, out var al)) foreach (var a in al) want.Add(a); else want.Add(p);
            var recs = man.steps.ToList();
            foreach (var w in want)
            {
                var r = recs.FirstOrDefault(q => string.Equals(q.name, w, StringComparison.OrdinalIgnoreCase));
                if (r == null) throw new Exception($"quick build: no step '{w}' (steps: {string.Join(", ", recs.Select(q => q.name))})");
                if (!r.quick || r.touchesOthers) throw new Exception($"quick build: step '{r.name}' cannot be rebuilt alone (it reads what other steps made{(r.touchesOthers ? ", or changes their objects" : "")}); run a full Build Map");
            }
            int first = recs.FindIndex(q => want.Contains(q.name));

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BuildClock.Lap("open the scene");
            var rootsBefore = new HashSet<GameObject>(scene.GetRootGameObjects());
            var c = new Ctx();
            Plan(c, true);
            // anything the planning made in the scene (the memory terrain's root aside) does not belong there
            foreach (var g in scene.GetRootGameObjects()) if (!rootsBefore.Contains(g) && g.name != QuickTerrainRoot) UnityEngine.Object.DestroyImmediate(g);
            var tmpRoot = scene.GetRootGameObjects().First(g => g.name == QuickTerrainRoot);
            var sceneTiles = SceneTiles(scene, c.terrain);
            // 2. the ground before the first chosen step: the scene's own, with every later step's samples undone (last first)
            foreach (var (tx, tz) in TileIds(c.terrain)) { var sd = sceneTiles[tx, tz]; var md = c.terrain.Tiles[tx, tz].terrainData; md.SetHeights(0, 0, sd.GetHeights(0, 0, sd.heightmapResolution, sd.heightmapResolution)); md.SetHoles(0, 0, sd.GetHoles(0, 0, sd.holesResolution, sd.holesResolution)); }
            for (int k = recs.Count - 1; k >= first; k--) ApplySamples(c.terrain, recs[k].name, false);
            BuildClock.Lap("the ground before " + recs[first].name);
            // 3. the chosen steps' objects out; the steps again; the others' samples back
            var chosen = Steps(c).Where(s => want.Contains(s.name)).ToDictionary(s => s.name);
            var targets = new List<(StepRec r, List<GameObject> objs)>();
            foreach (var r in recs.Where(q => want.Contains(q.name))) targets.Add((r, r.roots.Select(rt => FindAt(scene, rt)).ToList()));
            foreach (var (r, objs) in targets) { int miss = objs.Count(o => o == null); if (miss > 0) throw new Exception($"quick build: {miss} of step {r.name}'s objects are not in the scene where the record has them; run a full Build Map"); }
            // the references other objects hold into them (set again to the new objects in the same places afterwards)
            var held = SceneRefs.Into(scene.GetRootGameObjects(), new HashSet<Transform>(targets.SelectMany(q => q.objs).Select(o => o.transform)));
            BuildClock.Lap($"{held.Count} references into them noted");
            foreach (var (r, objs) in targets) foreach (var o in objs) UnityEngine.Object.DestroyImmediate(o);
            var rec = new StepRecorder(c, true); int conflicts = 0;
            for (int k = first; k < recs.Count; k++)
            {
                var r = recs[k];
                if (chosen.TryGetValue(r.name, out var st))
                {
                    rec.Resync(); st.prep?.Invoke();
                    QuickStep = r.name; AssetDatabase.DeleteAsset(GeneratedDir + "/Meshes/q/" + r.name);   // its meshes from an earlier quick build
                    StepRec nr; try { nr = rec.Run(st); } finally { QuickStep = null; }
                    nr.quick = r.quick;
                    // back in their places in the hierarchy (a step's objects keep the order they were made in)
                    var made = nr.made;
                    if (made.Count == r.roots.Length) for (int i = 0; i < made.Count; i++) made[i].SetSiblingIndex(r.roots[i].index);
                    else Debug.LogWarning($"[QuickBuild] {r.name} made {made.Count} objects where the full build made {r.roots.Length}: their places in the hierarchy differ from a full build's");
                    nr.roots = made.Select(t => new StepRoot { path = PathOf(t), index = t.GetSiblingIndex() }).ToArray();
                    recs[k] = nr;
                    Debug.Log($"[QuickBuild] {r.name}: {made.Count} objects (was {r.roots.Length}), {nr.heights} heights and {nr.holes} holes changed (was {r.heights}, {r.holes})");
                }
                else conflicts += ApplySamples(c.terrain, r.name, true);
            }
            int lost = SceneRefs.Relink(scene, held);
            if (lost > 0) Debug.LogWarning($"[QuickBuild] {lost} of {held.Count} references from other objects into the rebuilt ones found nothing in the same place: run a full Build Map");
            else Debug.Log($"[QuickBuild] {held.Count} references from other objects set again to the rebuilt ones");
            if (conflicts > 0) Debug.LogWarning($"[QuickBuild] {conflicts} terrain samples a rebuilt step changed were also changed by a later step: the result may differ from a full build (run one)");
            // 4. the ground into the scene's terrain
            int tilesChanged = 0;
            foreach (var (tx, tz) in TileIds(c.terrain))
            {
                var sd = sceneTiles[tx, tz]; var md = c.terrain.Tiles[tx, tz].terrainData; int res = sd.heightmapResolution, hr = sd.holesResolution;
                var a = md.GetHeights(0, 0, res, res); var b = sd.GetHeights(0, 0, res, res); var ha = md.GetHoles(0, 0, hr, hr); var hb = sd.GetHoles(0, 0, hr, hr);
                bool dh = false, dk = false;
                for (int z = 0; z < res && !dh; z++) for (int x = 0; x < res; x++) if (a[z, x] != b[z, x]) { dh = true; break; }
                for (int z = 0; z < hr && !dk; z++) for (int x = 0; x < hr; x++) if (ha[z, x] != hb[z, x]) { dk = true; break; }
                if (dh) sd.SetHeights(0, 0, a); if (dk) sd.SetHoles(0, 0, ha);
                if (dh || dk) { EditorUtility.SetDirty(sd); tilesChanged++; }
            }
            UnityEngine.Object.DestroyImmediate(tmpRoot);
            BuildClock.Lap($"the ground written ({tilesChanged} tiles changed)");
            // Round 43: the race's copies of the route files (data, read by the exe) taken afresh, as a full build's BuildRace does
            foreach (var go in scene.GetRootGameObjects())
                foreach (var race in go.GetComponentsInChildren<RaceController>(true))
                {
                    var files = race.routeFileNames ?? new string[0]; var dir = Path.Combine(ProjectRoot, "Data/routes");
                    race.embeddedRouteJsons = files.Select(f => File.Exists(Path.Combine(dir, f)) ? File.ReadAllText(Path.Combine(dir, f)) : "").ToArray();
                    var main = Path.Combine(dir, race.routeFileName ?? ""); if (File.Exists(main)) race.embeddedRouteJson = File.ReadAllText(main);
                    EditorUtility.SetDirty(race);
                    Debug.Log($"[QuickBuild] race routes taken afresh: {string.Join(", ", files)}");
                }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            man.steps = recs.ToArray(); man.built = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " (quick: " + parts + ")";
            man.sceneTicks = File.GetLastWriteTimeUtc(Path.Combine(ProjectRoot, ScenePath)).Ticks;
            File.WriteAllText(mf, JsonUtility.ToJson(man, true));
            BuildClock.Lap("saved");
            var dump = QuickArg("-dumpOut"); if (dump != null) { EditorSceneManager.OpenScene(ScenePath); SceneDump.Write(dump); BuildClock.Lap("scene dump (the saved scene, read back)"); }
            Debug.Log($"[QuickBuild] {parts}: done in {BuildClock.Total:0} s; {conflicts} conflicts");
            if (!PlayerBuild.KeepScene) BuildClock.Done(Path.Combine(ProjectRoot, GeneratedDir, "quick_times.txt"));
        }

        static IEnumerable<(int tx, int tz)> TileIds(TerrainGrid g) { for (int tz = 0; tz < g.Tiles.GetLength(1); tz++) for (int tx = 0; tx < g.Tiles.GetLength(0); tx++) yield return (tx, tz); }
        static TerrainData[,] SceneTiles(UnityEngine.SceneManagement.Scene scene, TerrainGrid g)
        {
            var root = scene.GetRootGameObjects().First(o => o.name == "Terrain").transform; var t = new TerrainData[g.Tiles.GetLength(0), g.Tiles.GetLength(1)];
            foreach (var (tx, tz) in TileIds(g)) t[tx, tz] = root.Find($"Terrain_{tx}_{tz}").GetComponent<Terrain>().terrainData;
            return t;
        }

        // a step's recorded samples applied to the memory terrain: undone (before) or redone (after). Redone, a sample that is not what it was
        // before that step in the full build counts as a conflict (a rebuilt step changed it too)
        static int ApplySamples(TerrainGrid g, string step, bool redo)
        {
            int conflicts = 0;
            foreach (bool holes in new[] { false, true })
            {
                var list = ReadSamples(step, holes); if (list.Count == 0) continue;
                foreach (var grp in list.GroupBy(s => (s.tx, s.tz)))
                {
                    var d = g.Tiles[grp.Key.tx, grp.Key.tz].terrainData;
                    if (!holes)
                    {
                        int res = d.heightmapResolution; var a = d.GetHeights(0, 0, res, res);
                        foreach (var s in grp) { if (redo && a[s.z, s.x] != s.before) conflicts++; a[s.z, s.x] = redo ? s.after : s.before; }
                        d.SetHeights(0, 0, a);
                    }
                    else
                    {
                        int hr = d.holesResolution; var a = d.GetHoles(0, 0, hr, hr);
                        foreach (var s in grp) { bool want = (redo ? s.after : s.before) > .5f; if (redo && a[s.z, s.x] != (s.before > .5f)) conflicts++; a[s.z, s.x] = want; }
                        d.SetHoles(0, 0, a);
                    }
                }
            }
            return conflicts;
        }

        // the object a record names: its path, the child at its recorded index when several share the name
        static GameObject FindAt(UnityEngine.SceneManagement.Scene scene, StepRoot rt)
        {
            var parts = rt.path.Split('/').Where(p => p.Length > 0).ToArray();
            Transform parent = null; IEnumerable<Transform> level = scene.GetRootGameObjects().Select(g => g.transform);
            for (int i = 0; i < parts.Length; i++)
            {
                var named = level.Where(t => t.name == parts[i]).ToList(); if (named.Count == 0) return null;
                Transform pick = named[0];
                if (i == parts.Length - 1 && named.Count > 1) pick = named.FirstOrDefault(t => t.GetSiblingIndex() == rt.index) ?? named[0];
                parent = pick; level = parent.Cast<Transform>();
            }
            return parent ? parent.gameObject : null;
        }
    }
}
