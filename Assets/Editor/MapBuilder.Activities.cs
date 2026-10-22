using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEditor;
using UnityEngine;

// Round 36 (Part B): Racer's Free Roam activities (ArcadeActivities: jumps, speed traps, smash runs) on this map (Dan: "We can probably add free
// roam events"). The sites are game layer: their own root in the scene ("Free roam activities"), made here from Data/activities/free_roam.json
// (Tools/mapgen/make_activities.py), never part of the world's pieces. A jump site stands at a launch the world already has (a BigJump, by id);
// a speed trap on its road with a camera post beside it (both directions, Racer's); a smash run's props are the built breakables of its kind in its
// area, the densest row of up to `count` of them (Racer's site lists its props). Built after the world (BuildRace's time).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class ActivityJson { public string id, title, kind, jump, props, note; public float x, z, fx, fz, radius, bronze, silver, gold, seconds; public int count; public float[] vb, vs, vg; }
        [Serializable] class ActivitiesJson { public ActivityJson[] sites; }

        // checks' shortcut: the sites alone put again into the built scene (new targets from the exe's runs) without a whole Build Map
        public static void RebuildActivitiesFromCommandLine()
        {
            try
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
                foreach (var g in scene.GetRootGameObjects().Where(g => g.name.StartsWith("Free roam activities"))) UnityEngine.Object.DestroyImmediate(g);
                var tiles = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
                var meta = LoadTerrainMeta();   // Round 59
                var grid = new TerrainGrid { Tiles = new Terrain[meta.tiles_x, meta.tiles_z], TileW = meta.tile_w_m, TileD = meta.tile_d_m, BaseY = meta.min_height, OriginX = meta.origin_x_m, OriginZ = meta.origin_z_m };
                foreach (var tr in tiles) { int i = Mathf.RoundToInt((tr.transform.position.x - meta.origin_x_m) / meta.tile_w_m), j = Mathf.RoundToInt((tr.transform.position.z - meta.origin_z_m) / meta.tile_d_m); grid.Tiles[i, j] = tr; }
                var house = AssetDatabase.LoadAssetAtPath<Material>(GeneratedDir + "/House.mat");
                BuildActivities(grid, null, house);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ScenePath);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        static void BuildActivities(TerrainGrid t, Materials mats, Material houseMat = null)
        {
            var path = Path.Combine(ProjectRoot, "Data/activities/free_roam.json");
            if (!File.Exists(path)) { Debug.LogWarning("[Round36] " + path + " missing (Tools/mapgen/make_activities.py): no Free Roam activities"); return; }
            var data = JsonUtility.FromJson<ActivitiesJson>(File.ReadAllText(path));
            var root = new GameObject("Free roam activities (game layer)").transform;
            var jumps = UnityEngine.Object.FindObjectsByType<BigJump>(FindObjectsSortMode.None);
            var chunks = UnityEngine.Object.FindObjectsByType<FenceChunk>(FindObjectsSortMode.None);
            var posts = new MeshBuilder(); var log = new List<string>();
            foreach (var s in data.sites)
            {
                var go = new GameObject(s.title); go.transform.SetParent(root, false);
                var site = go.AddComponent<ActivitySite>(); site.id = s.id; site.title = s.title; site.radius = s.radius; site.bronze = s.bronze; site.silver = s.silver; site.gold = s.gold; site.Seconds = s.seconds;
                if (s.vb != null && s.vb.Length == 4) { site.vehicleBronze = s.vb; site.vehicleSilver = s.vs; site.vehicleGold = s.vg; }
                if (s.kind == "jump")
                {
                    var j = jumps.FirstOrDefault(b => b.id == s.jump);
                    if (!j) { log.Add($"{s.id}: no launch {s.jump} in the world, left out"); UnityEngine.Object.DestroyImmediate(go); continue; }
                    site.kind = ActivitySite.Kind.Jump; go.transform.position = j.lip - j.dir * 6f; site.forward = j.dir; site.bothDirections = false;
                    log.Add($"{s.id} at {j.id}");
                }
                else if (s.kind == "speed")
                {
                    site.kind = ActivitySite.Kind.Speed; float y = Ground(t, s.x, s.z); go.transform.position = new Vector3(s.x, y, s.z); site.forward = new Vector3(s.fx, 0, s.fz); site.bothDirections = true;
                    // the camera post beside the road (both sides: Racer's two SPEED CAMERA signs)
                    var right = Vector3.Cross(Vector3.up, site.forward).normalized;
                    foreach (float side in new[] { -1f, 1f })
                    {
                        var at = new Vector3(s.x, 0, s.z) + right * side * (s.radius + 1.2f); at.y = Ground(t, at.x, at.z);
                        posts.Tint = new Color(.25f, .25f, .27f); Post(posts, at - Vector3.up * .1f, 3.4f, .12f, posts.Tint);
                        BoxF(posts, MakeFrame(at + Vector3.up * 3.3f, new Vector2(-right.x * side, -right.z * side), 1f), -.25f, .25f, 0f, .4f, -.35f, .35f, new Color(.95f, .76f, .12f));
                        BoxF(posts, MakeFrame(at + Vector3.up * 3.3f, new Vector2(-right.x * side, -right.z * side), 1f), -.1f, .1f, .12f, .28f, .35f, .42f, new Color(.05f, .05f, .06f));
                    }
                    log.Add($"{s.id} at ({s.x:0}, {s.z:0})");
                }
                else
                {
                    site.kind = ActivitySite.Kind.Smash;
                    var kind = (BreakSounds.Kind)Enum.Parse(typeof(BreakSounds.Kind), s.props);
                    var c0 = new Vector2(s.x, s.z);
                    var cands = new List<(FenceChunk c, int i, Vector3 p)>();
                    foreach (var c in chunks)
                    {
                        if (c.sound != kind || c.panels == null) continue;
                        for (int i = 0; i < c.panels.Length; i++) { var p = c.transform.TransformPoint(c.panels[i].centre); if (Vector2.Distance(new Vector2(p.x, p.z), c0) <= s.radius) cands.Add((c, i, p)); }
                    }
                    if (s.count > 0 && cands.Count > s.count)
                    {
                        // the densest row: the panel with the most others within 40 m, then the `count` nearest it
                        var best = cands.OrderByDescending(a => cands.Count(b => (b.p - a.p).sqrMagnitude < 1600f)).First();
                        cands = cands.OrderBy(b => (b.p - best.p).sqrMagnitude).Take(s.count).ToList();
                    }
                    if (cands.Count < 2) { log.Add($"{s.id}: only {cands.Count} {s.props} props in the area, left out"); UnityEngine.Object.DestroyImmediate(go); continue; }
                    site.propChunks = cands.Select(a => a.c).ToArray(); site.propPanels = cands.Select(a => a.i).ToArray();
                    var mid = cands.Aggregate(Vector3.zero, (sum, a) => sum + a.p) / cands.Count; go.transform.position = mid;
                    site.radius = Mathf.Max(s.radius > 60 ? 45f : s.radius, cands.Max(a => Vector3.Distance(a.p, mid)) + 10f);
                    if (site.gold > cands.Count) { site.gold = cands.Count; site.silver = Mathf.Min(site.silver, Mathf.Ceil(cands.Count * .7f)); site.bronze = Mathf.Min(site.bronze, Mathf.Ceil(cands.Count * .4f)); }
                    log.Add($"{s.id}: {cands.Count} {s.props} props, around ({mid.x:0}, {mid.z:0})");
                }
            }
            posts.ToObject("SpeedCameraPosts" + (mats == null ? "_" + DateTime.Now.Ticks : ""), mats != null ? mats.House : houseMat, root, false, true);
            Debug.Log($"[Round36] Free Roam activities: {root.childCount - 1} sites: " + string.Join("; ", log));
        }
    }
}
