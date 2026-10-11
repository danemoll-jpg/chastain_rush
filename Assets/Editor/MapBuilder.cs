using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Builds the prototype scene from generated map data. Everything it writes goes to Assets/Generated
// (git-ignored) and is rebuilt from scratch on every run, so nothing hand-made should live there.
//   Data/map.json                 roads, buildings, water (Tools/mapgen/mapgen.py)
//   Assets/Generated/terrain.r16  heightmap with roads already flattened (same script)
//   local/owner_marker.json       optional, never committed: which building to mark
// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod ChastainRush.MapBuilder.BuildFromCommandLine
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const string GeneratedDir = "Assets/Generated";
        public const string ScenePath = GeneratedDir + "/ChastainPrototype.unity";

        static readonly Dictionary<string, float> RoadLift = new Dictionary<string, float>
        {
            { "primary", 0.09f }, { "trunk", 0.09f }, { "motorway", 0.09f }, { "secondary", 0.08f },
            { "tertiary", 0.07f }, { "primary_link", 0.075f }, { "secondary_link", 0.07f },
            { "tertiary_link", 0.065f }, { "residential", 0.06f }, { "unclassified", 0.06f },
            { "living_street", 0.06f }, { "service", 0.05f },
        };

        [Serializable] class Road { public string name; public string @class; public float width; public bool oneway; public float[] points; public long osm_id; }
        [Serializable] class Building { public string type; public float height; public float[] footprint; public string roof; public float pitch_deg; public int seed; }
        [Serializable] class Driveway { public float[] points; public float width; public string source; }
        [Serializable] class Area { public string name; public float[] outline; }
        [Serializable] class Line { public string name; public string kind; public float[] points; }
        [Serializable] class Spawn { public float x; public float z; public float heading_deg; public string street; }
        [Serializable] class Bridge { public string name; public string @class; public float width; public bool oneway; public float[] points; public float[] deck_y; }
        [Serializable] class MapData { public string attribution; public float size_x_m; public float size_z_m; public float vertical_scale = 1f; public Road[] roads; public Bridge[] bridges; public Building[] buildings; public Area[] water; public Line[] streams; public Driveway[] driveways; public Spawn spawn; }
        [Serializable] class TerrainMeta { public int grid_x; public int grid_z; public int tiles_x; public int tiles_z; public int tile_resolution; public float tile_w_m; public float tile_d_m; public float min_height; public float max_height; }

        // The terrain is a grid of tiles that share their edge samples; this finds the tile under a point.
        sealed class TerrainGrid
        {
            public Terrain[,] Tiles;
            public float TileW, TileD, BaseY;
            public Terrain At(float x, float z)
            {
                int i = Mathf.Clamp(Mathf.FloorToInt(x / TileW), 0, Tiles.GetLength(0) - 1);
                int j = Mathf.Clamp(Mathf.FloorToInt(z / TileD), 0, Tiles.GetLength(1) - 1);
                return Tiles[i, j];
            }
            public float Height(float x, float z) => At(x, z).SampleHeight(new Vector3(x, 0f, z)) + BaseY;
        }
        [Serializable] class OwnerMarker { public int index; public float x; public float z; }

        static float vScale = 1f;

        static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        [MenuItem("Chastain Rush/Build Map")]
        public static void BuildMenu() => Build();

        public static void BuildFromCommandLine()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void Build()
        {
            ProjectSetup.EnsureUrp();
            var map = JsonUtility.FromJson<MapData>(File.ReadAllText(Path.Combine(ProjectRoot, "Data/map.json")));
            var meta = JsonUtility.FromJson<TerrainMeta>(File.ReadAllText(Path.Combine(ProjectRoot, GeneratedDir, "terrain.json")));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var mats = new Materials();

            BuildSky(mats);

            // vertical_scale (config.json) is already baked into the terrain heights and building heights by mapgen;
            // the builder only applies it to the small offsets it adds itself, so everything stays one pure y-scale.
            vScale = map.vertical_scale > 0f ? map.vertical_scale : 1f;
            var terrain = BuildTerrain(meta, mats);
            SmoothJunctionSeams(map, terrain);   // Round 17 (5): BUG-001 / BUG-002, the two junction seams Dan pointed at (MapBuilder.Round17.cs)
            var shop = ShoppingCenter.Find(map);   // Round 17 (Part B): the shopping center (MapBuilder.ShoppingCenter.cs)
            GradeLot(map, terrain, shop);          // its parking lot graded before anything samples the ground
            JoinServiceRoads(map, terrain, shop);  // Round 18 (A): BUG-001, the service roads run into the lot and round the back
            var world = new GameObject("World").transform;
            var paint = new Chunks();
            // Round 7: the Chastain Glen area is dressed by hand-made / styled content (MapBuilder.Neighborhood.cs); its
            // houses and driveways are not built here
            var hood = Neighborhood.Load(map, terrain);
            hood.RegradeAndReport();   // Round 9 (BUG-005): the buried house's lot is cut down to it, before anything samples the ground
            CarveWaterBeds(map, terrain);   // Round 15 (item 2): a bed under every pond, lake and stream (MapBuilder.Water.cs)
            BuryCulvert(map, hood, terrain);   // Round 16 (A): the pond's culvert headwall set into the bank (MapBuilder.Pond.cs)
            BuildRoads(map, terrain, mats, world, paint, hood);
            BuildBridges(map, mats, world, paint);
            paint.ToObjects("RoadPaint", mats.Paint, world, false);
            BuildRoadLookup(map);
            BuildBuildings(map, terrain, mats, world, LoadOwner(), hood, shop);
            BuildDriveways(map, terrain, mats, world, hood);
            BuildWater(map, terrain, mats, world, hood);
            RegisterWater(map, world, new Pavement(map));   // Round 15 (item 2c)
            // Round 10 (item 7): trees are solid now, so no trunk may stand on a road or a drive anywhere on the map (the area's
            // own rules already keep them off its streets, drives and the shortcut lines): one standing within 0.6 m of a
            // road's or a drive's edge is moved out to 1.5 m past the edge, or dropped if that is on pavement too
            var pave = new Pavement(map);
            var trees = TreeBuilder.Build(Path.Combine(ProjectRoot, "Data/trees.bin"), (x, z) => terrain.Height(x, z), vScale, GeneratedDir, mats.Foliage, world,
                (x, z) => hood.RejectTree(x, z) || pave.On(new Vector2(x, z), 0.6f) || Beds.Wet(x, z) || shop.RejectTree(x, z),   // Round 15: none in the water; Round 17: none on the parking lot
                (x, z) => pave.MoveOff(hood.MoveTree(x, z)));
            trees.Field.gameObject.AddComponent<TreeTrunks>();
            Debug.Log($"[Round10] solid trees: {pave.Moved} trunks moved off a road or drive, {pave.Dropped} on pavement with no room beside dropped; trunk colliders: a pool of 160 round the car (TreeTrunks)");
            BuildNeighborhood(map, terrain, mats, world, hood, trees);
            BuildShoppingCenter(map, terrain, mats, world, shop);   // Round 17 (Part B): lot, shop fronts, gas station, the bank
            BuildShortcuts(hood, terrain, mats, world);   // Round 8: the official shortcuts' ramps, decks and trail (MapBuilder.Shortcuts.cs)
            if (hood.ShortcutData != null) ReportTreesOnLines(hood, trees);

            new GameObject("MapCredits").AddComponent<MapCredits>().text = "© OpenStreetMap contributors";
            BuildRace(mats);
            PlayerSetup.Place(map.spawn.x, map.spawn.z, map.spawn.heading_deg, terrain.Height(map.spawn.x, map.spawn.z), terrain.BaseY);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[MapBuilder] Trees: {trees.Trees} ({trees.Woods} in woods, {trees.Yards} in yards); per species " +
                      string.Join(", ", TreeField.SpeciesNames.Select((nm, i) => nm + " " + trees.PerSpecies[i])) + $"; kit triangles: {trees.KitTriangles}");
            Debug.Log($"[MapBuilder] Built {ScenePath}: {map.roads.Length} roads, {map.buildings.Length} buildings, " +
                      $"{map.bridges.Length} bridges, {map.water.Length} water areas, {meta.tiles_x}x{meta.tiles_z} terrain tiles, spawn on {map.spawn.street}, vertical scale {vScale:0.##}");
        }

        // ------------------------------------------------------------ terrain

        static TerrainGrid BuildTerrain(TerrainMeta meta, Materials mats)
        {
            int res = meta.tile_resolution, cells = res - 1;
            var bytes = File.ReadAllBytes(Path.Combine(ProjectRoot, GeneratedDir, "terrain.r16"));
            if (bytes.Length != (long)meta.grid_x * meta.grid_z * 2) throw new Exception($"terrain.r16 is {bytes.Length} bytes, expected {(long)meta.grid_x * meta.grid_z * 2}");
            foreach (var guid in AssetDatabase.FindAssets("TerrainData t:TerrainData", new[] { GeneratedDir })) AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            var grid = new TerrainGrid { Tiles = new Terrain[meta.tiles_x, meta.tiles_z], TileW = meta.tile_w_m, TileD = meta.tile_d_m, BaseY = meta.min_height };
            var root = new GameObject("Terrain").transform;
            var heights = new float[res, res];
            for (int tz = 0; tz < meta.tiles_z; tz++)
                for (int tx = 0; tx < meta.tiles_x; tx++)
                {
                    // tiles share their edge row/column of samples, so neighbours meet at exactly the same height
                    for (int z = 0; z < res; z++)
                        for (int x = 0; x < res; x++)
                        {
                            int i = ((tz * cells + z) * meta.grid_x + tx * cells + x) * 2;
                            heights[z, x] = (bytes[i] | (bytes[i + 1] << 8)) / 65535f;
                        }
                    var data = new TerrainData { heightmapResolution = res };
                    data.size = new Vector3(meta.tile_w_m, meta.max_height - meta.min_height, meta.tile_d_m);
                    data.SetHeights(0, 0, heights);
                    data.terrainLayers = new[] { mats.GrassLayer };
                    AssetDatabase.CreateAsset(data, $"{GeneratedDir}/TerrainData_{tx}_{tz}.asset");

                    var go = Terrain.CreateTerrainGameObject(data);
                    go.name = $"Terrain_{tx}_{tz}";
                    go.transform.SetParent(root, false);
                    go.transform.position = new Vector3(tx * meta.tile_w_m, meta.min_height, tz * meta.tile_d_m);
                    var terrain = go.GetComponent<Terrain>();
                    terrain.heightmapPixelError = 3f;
                    terrain.basemapDistance = 2000f;
                    terrain.drawTreesAndFoliage = false;
                    terrain.groupingID = 1;
                    terrain.allowAutoConnect = true;
                    if (mats.TerrainMaterial != null) terrain.materialTemplate = mats.TerrainMaterial;
                    grid.Tiles[tx, tz] = terrain;
                }
            for (int tz = 0; tz < meta.tiles_z; tz++)
                for (int tx = 0; tx < meta.tiles_x; tx++)
                    grid.Tiles[tx, tz].SetNeighbors(
                        tx > 0 ? grid.Tiles[tx - 1, tz] : null, tz < meta.tiles_z - 1 ? grid.Tiles[tx, tz + 1] : null,
                        tx < meta.tiles_x - 1 ? grid.Tiles[tx + 1, tz] : null, tz > 0 ? grid.Tiles[tx, tz - 1] : null);
            return grid;
        }

        static float Ground(TerrainGrid t, float x, float z) => t.Height(x, z);

        // ------------------------------------------------------------ sky, sun and distance fog

        // The fog colour and the sky's horizon colour are the same, so the far edge of the map fades into the sky.
        // Fog is linear: clear for the first FogStart metres, solid by FogEnd (trees stop at TreeField.drawDistance,
        // the camera's far clip is just past it).
        public const float FogStart = 100f, FogEnd = 650f;
        static readonly Color HazeColor = new Color(0.74f, 0.80f, 0.88f);

        static void BuildSky(Materials mats)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.96f, 0.88f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = sun;
            Vector3 toSun = -sun.transform.forward;
            mats.Sky.SetVector("_SunDir", new Vector4(toSun.x, toSun.y, toSun.z, 0f));
            RenderSettings.skybox = mats.Sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.68f, 0.84f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.55f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.25f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = HazeColor;
            RenderSettings.fogStartDistance = FogStart;
            RenderSettings.fogEndDistance = FogEnd;
        }

        // ------------------------------------------------------------ roads (visual only; the car drives on the flattened terrain)

        // Other roads, for trimming road paint where a side road joins or crosses (the paint stops at the side road's edge).
        sealed class RoadIndex
        {
            struct Seg { public Vector2 a, b; public float half; public Road road; }
            readonly List<Seg> segs = new List<Seg>();
            readonly Dictionary<Vector2Int, List<int>> grid = new Dictionary<Vector2Int, List<int>>();
            const float Cell = 32f;
            public RoadIndex(Road[] roads)
            {
                foreach (var r in roads)
                {
                    var p = ToPoints(r.points);
                    for (int i = 0; i + 1 < p.Count; i++)
                    {
                        var s = new Seg { a = p[i], b = p[i + 1], half = r.width * 0.5f, road = r };
                        int id = segs.Count;
                        segs.Add(s);
                        float pad = s.half + 1f;
                        int x0 = Mathf.FloorToInt((Mathf.Min(s.a.x, s.b.x) - pad) / Cell), x1 = Mathf.FloorToInt((Mathf.Max(s.a.x, s.b.x) + pad) / Cell);
                        int z0 = Mathf.FloorToInt((Mathf.Min(s.a.y, s.b.y) - pad) / Cell), z1 = Mathf.FloorToInt((Mathf.Max(s.a.y, s.b.y) + pad) / Cell);
                        for (int x = x0; x <= x1; x++)
                            for (int z = z0; z <= z1; z++)
                            {
                                var key = new Vector2Int(x, z);
                                if (!grid.TryGetValue(key, out var l)) grid[key] = l = new List<int>();
                                l.Add(id);
                            }
                    }
                }
            }

            static readonly string[] Directions = { "Northeast", "Northwest", "Southeast", "Southwest", "North", "South", "East", "West", "NE", "NW", "SE", "SW" };
            static string BaseName(string n)
            {
                var words = (n ?? "").Split(' ');
                int len = words.Length;
                while (len > 1 && Array.IndexOf(Directions, words[len - 1]) >= 0) len--;
                return string.Join(" ", words, 0, len);
            }
            static bool SameStreet(Road a, Road b) => a == b || (a.@class == b.@class && BaseName(a.name) == BaseName(b.name));

            // true if p lies on the pavement of some other road (not a continuation of 'self')
            public bool Covers(Vector2 p, Road self, float margin = 0.1f)
            {
                if (!grid.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell)), out var l)) return false;
                foreach (int id in l)
                {
                    var s = segs[id];
                    if (SameStreet(s.road, self)) continue;
                    var ab = s.b - s.a;
                    float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                    if ((s.a + ab * t - p).magnitude < s.half + margin) return true;
                }
                return false;
            }
        }

        static void BuildRoads(MapData map, TerrainGrid terrain, Materials mats, Transform parent, Chunks paint, Neighborhood hood)
        {
            var main = new Chunks();
            var service = new Chunks();
            var center = new Chunks { Tint = LotAsphalt };   // Round 18 (2a): the shopping center's service roads, the lot's color
            var others = new RoadIndex(map.roads);
            for (int ri = 0; ri < map.roads.Length; ri++)
            {
                var r = map.roads[ri];
                var pts = Densify(ToPoints(r.points), 2f);
                float lift = (RoadLift.TryGetValue(r.@class, out var l) ? l : 0.06f) * vScale;
                // Round 7: a service way inside Chastain Glen (the crossover through the entrance island) is paved like the
                // street it joins. In the lighter service shade it read as a pale patch on the carriageway (01_spawn.png).
                var mb = IsCenterRoad(r) ? center : r.@class == "service" && !hood.IsAreaRoad(ri) ? service : main;
                var self = r;
                if (hood.IsStreetRoad(ri))
                {
                    // Round 11: in Chastain Glen the surface is a grid that stays clear of the lawn (four cells across), the end
                    // caps likewise (a cul-de-sac's end is its bulb), and paint stops on any other road that does not continue this one
                    int road = ri; float hw = r.width * 0.5f;
                    foreach (var run in Runs(pts, 96f))
                    {
                        if (!hood.Bounds.Contains(run[run.Count / 2])) { AddRibbon(mb.At(run[run.Count / 2]), run, r.width, lift, terrain); continue; }
                        var dense = Densify(run, 0.75f);
                        RibbonGridMax(mb.At(run[run.Count / 2]), dense, Enumerable.Repeat(r.width, dense.Count).ToArray(), lift, terrain, 6, true);
                    }
                    foreach (var e in new[] { pts[0], pts[pts.Count - 1] })
                        if (!hood.InBulbZone(e, -0.5f)) DiscGrid(mb.At(e), e, hw, lift - 0.004f, terrain, 20);
                    AddPaint(paint, pts, r.@class, r.width, r.oneway, (p, s) => Ground(terrain, p.x, p.y) + lift,
                             p => hood.OtherRoadCovers(road, p, 0.1f) || hood.InCornerZone(road, p, hw - 0.7f) || hood.InBulbZone(p, 0.45f) || hood.InMedianFill(p) || JoinPaintStops(self.osm_id, p, hw));   // Round 18 (A): road b is one of these
                    continue;
                }
                foreach (var run in Runs(pts, 96f)) AddRibbon(mb.At(run[run.Count / 2]), run, r.width, lift, terrain);
                AddDisc(mb.At(pts[0]), pts[0], r.width * 0.5f, lift - 0.004f, terrain);
                AddDisc(mb.At(pts[pts.Count - 1]), pts[pts.Count - 1], r.width * 0.5f, lift - 0.004f, terrain);
                AddPaint(paint, pts, r.@class, r.width, r.oneway, (p, s) => Ground(terrain, p.x, p.y) + lift, p => others.Covers(p, self) || JoinPaintStops(self.osm_id, p, self.width * 0.5f));   // Round 18 (A)
            }
            BuildStreetSurfaces(hood, terrain, main, paint);   // Round 11: curb returns and cul-de-sac bulbs (MapBuilder.Round11.cs)
            main.ToObjects("Roads", mats.Asphalt, parent, false); // flat on the ground: no shadows to cast
            service.ToObjects("ServiceRoads", mats.ServiceAsphalt, parent, false);
            center.ToObjects("ShoppingCenterRoads", mats.Driveway, parent, false);   // the lot's own material and tint
        }

        // Round 5: the race controller. The route itself is data (Data/routes/*.json, from Tools/mapgen/make_route.py), laid over
        // the one world; the scene only carries a copy of it (the Editor reads the file afresh) and the marker materials.
        static void BuildRace(Materials mats)
        {
            string path = Path.Combine(ProjectRoot, "Data/routes/chastain_glen_loop.json");
            var go = new GameObject("Race");
            var race = go.AddComponent<RaceController>();
            race.routeFileName = Path.GetFileName(path);
            race.embeddedRouteJson = File.Exists(path) ? File.ReadAllText(path) : "";
            race.gateBlack = mats.GateBlack; race.gateWhite = mats.GateWhite;
            race.nextMaterial = mats.RaceNext; race.laterMaterial = mats.RaceLater; race.missedMaterial = mats.RaceMissed;
            race.shortcutMaterial = mats.RaceShortcut;
            if (race.embeddedRouteJson.Length == 0) Debug.LogWarning("[MapBuilder] no race route at " + path + " (run Tools/mapgen/make_route.py); racing will be off");
        }

        // Road names for the debug HUD, bug reports and bump log (Scripts/Debug/RoadLookup.cs): every road and bridge
        // centreline, serialized into the scene.
        static void BuildRoadLookup(MapData map)
        {
            var names = new List<string>(); var halves = new List<float>();
            var road = new List<int>(); var a = new List<Vector2>(); var b = new List<Vector2>();
            void Add(string name, float width, float[] flat)
            {
                int id = names.Count;
                names.Add(name ?? ""); halves.Add(width * 0.5f);
                var p = ToPoints(flat);
                for (int i = 0; i + 1 < p.Count; i++) { road.Add(id); a.Add(p[i]); b.Add(p[i + 1]); }
            }
            foreach (var r in map.roads) Add(r.name, r.width, r.points);
            foreach (var br in map.bridges) Add(br.name, br.width, br.points);
            new GameObject("RoadLookup").AddComponent<RoadLookup>().Fill(names, halves, road, a, b);
        }

        static void AddRibbon(MeshBuilder mb, List<Vector2> pts, float width, float lift, TerrainGrid t)
        {
            float hw = width * 0.5f;
            int start = mb.Count;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 dirIn = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                Vector2 dirOut = i < pts.Count - 1 ? (pts[i + 1] - pts[i]).normalized : dirIn;
                Vector2 tangent = (dirIn + dirOut).normalized;
                if (tangent.sqrMagnitude < 1e-6f) tangent = dirOut;
                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                float miter = Mathf.Min(hw / Mathf.Max(0.35f, Vector2.Dot(normal, new Vector2(-dirOut.y, dirOut.x))), hw * 2.5f);
                Vector2 left = pts[i] + normal * miter, right = pts[i] - normal * miter;
                mb.Vertex(new Vector3(left.x, Ground(t, left.x, left.y) + lift, left.y));
                mb.Vertex(new Vector3(right.x, Ground(t, right.x, right.y) + lift, right.y));
                if (i > 0)
                {
                    int a = start + (i - 1) * 2;
                    mb.Tri(a, a + 2, a + 1);
                    mb.Tri(a + 1, a + 2, a + 3);
                }
            }
        }

        static void AddDisc(MeshBuilder mb, Vector2 c, float radius, float lift, TerrainGrid t)
        {
            const int n = 14;
            int centre = mb.Vertex(new Vector3(c.x, Ground(t, c.x, c.y) + lift, c.y));
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                float x = c.x + Mathf.Cos(a) * radius, z = c.y + Mathf.Sin(a) * radius;
                mb.Vertex(new Vector3(x, Ground(t, x, z) + lift, z));
            }
            for (int i = 0; i < n; i++) mb.Tri(centre, centre + 1 + (i + 1) % n, centre + 1 + i);
        }

        // ------------------------------------------------------------ road paint
        // Every road gets a pale edge strip just outside the asphalt (the visible edge between road and grass).
        // Roads that have lines in real life (primary, secondary, tertiary, trunk, motorway and their links) also get
        // white edge lines; two-way ones a double yellow centre line; one-way and motorway carriageways white dashed
        // lane lines. Quiet residential, unclassified and service roads get only the edge strip. Paint is cut where
        // another road's pavement covers it (junctions) and sits on a depth offset, so it does not flicker.

        static readonly Color EdgeStrip = new Color(0.60f, 0.58f, 0.54f), PaintWhite = new Color(0.93f, 0.93f, 0.90f), PaintYellow = new Color(0.95f, 0.76f, 0.10f);
        const float PaintLift = 0.03f;

        static bool IsMarked(string cls) => cls == "primary" || cls == "secondary" || cls == "tertiary" || cls == "trunk" || cls == "motorway" || cls.EndsWith("_link");

        static void AddPaint(Chunks paint, List<Vector2> pts, string cls, float width, bool oneway, Func<Vector2, float, float> height, Func<Vector2, bool> blocked)
        {
            int n = pts.Count;
            if (n < 2) return;
            float hw = width * 0.5f;
            var nrm = new Vector2[n];
            var arc = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 dirIn = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                Vector2 dirOut = i < n - 1 ? (pts[i + 1] - pts[i]).normalized : dirIn;
                Vector2 t = (dirIn + dirOut).normalized;
                if (t.sqrMagnitude < 1e-6f) t = dirOut;
                nrm[i] = new Vector2(-t.y, t.x);
                if (i > 0) arc[i] = arc[i - 1] + Vector2.Distance(pts[i - 1], pts[i]);
            }
            float edgeW = cls == "service" ? 0.30f : 0.45f;
            Strip(paint, pts, nrm, arc, hw, hw + edgeW, EdgeStrip, height, blocked);
            Strip(paint, pts, nrm, arc, -hw - edgeW, -hw, EdgeStrip, height, blocked);
            if (!IsMarked(cls)) return;
            Strip(paint, pts, nrm, arc, hw - 0.55f, hw - 0.40f, PaintWhite, height, blocked);
            Strip(paint, pts, nrm, arc, -hw + 0.40f, -hw + 0.55f, PaintWhite, height, blocked);
            if (cls.EndsWith("_link")) return;
            bool twoWay = !oneway && cls != "motorway";
            if (twoWay)
            {
                if (width < 7f) return;
                Strip(paint, pts, nrm, arc, -0.22f, -0.10f, PaintYellow, height, blocked);
                Strip(paint, pts, nrm, arc, 0.10f, 0.22f, PaintYellow, height, blocked);
                int perDir = Mathf.Max(1, Mathf.RoundToInt((width - 1f) / 3.5f / 2f));
                float lw = (width - 1f) / (2 * perDir);
                for (int k = 1; k < perDir; k++)
                {
                    Dashes(paint, pts, nrm, arc, k * lw, PaintWhite, height, blocked);
                    Dashes(paint, pts, nrm, arc, -k * lw, PaintWhite, height, blocked);
                }
            }
            else
            {
                int lanes = Mathf.Max(1, Mathf.RoundToInt((width - 1f) / 3.5f));
                float lw = (width - 1f) / lanes;
                for (int k = 1; k < lanes; k++) Dashes(paint, pts, nrm, arc, -hw + 0.5f + k * lw, PaintWhite, height, blocked);
            }
        }

        // A continuous band between two offsets from the centre line (left of travel is positive).
        static void Strip(Chunks chunks, List<Vector2> pts, Vector2[] nrm, float[] arc, float offA, float offB, Color colour,
                          Func<Vector2, float, float> height, Func<Vector2, bool> blocked)
        {
            chunks.Tint = colour;
            float mid = (offA + offB) * 0.5f;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                int j = i + 1;
                var m = (pts[i] + pts[j]) * 0.5f + (nrm[i] + nrm[j]).normalized * mid;
                if (blocked(m)) continue;
                Vector3 V(int k, float off)
                {
                    var p = pts[k] + nrm[k] * off;
                    return new Vector3(p.x, height(p, arc[k]) + PaintLift, p.y);
                }
                Face(chunks.At(m), V(i, offA), V(j, offA), V(j, offB), V(i, offB), Vector3.up);
            }
        }

        // A dashed line at an offset from the centre line: 3 m paint, 6 m gap.
        static void Dashes(Chunks chunks, List<Vector2> pts, Vector2[] nrm, float[] arc, float off, Color colour,
                           Func<Vector2, float, float> height, Func<Vector2, bool> blocked, float dash = 3f, float gap = 6f, float w = 0.14f)
        {
            chunks.Tint = colour;
            float total = arc[arc.Length - 1];
            int seg = 0;
            Vector2 PosAt(float s, out Vector2 normal)
            {
                while (seg + 2 < arc.Length && arc[seg + 1] < s) seg++;
                float span = Mathf.Max(arc[seg + 1] - arc[seg], 1e-4f);
                float f = Mathf.Clamp01((s - arc[seg]) / span);
                normal = Vector2.Lerp(nrm[seg], nrm[seg + 1], f).normalized;
                return Vector2.Lerp(pts[seg], pts[seg + 1], f);
            }
            for (float s0 = gap * 0.5f; s0 + dash <= total; s0 += dash + gap)
            {
                seg = 0;
                var p0 = PosAt(s0, out var n0);
                var p1 = PosAt(s0 + dash, out var n1);
                var c = (p0 + p1) * 0.5f + (n0 + n1).normalized * off;
                if (blocked(c)) continue;
                Vector3 V(Vector2 p, Vector2 nn, float o, float s)
                {
                    var q = p + nn * o;
                    return new Vector3(q.x, height(q, s) + PaintLift, q.y);
                }
                Face(chunks.At(c), V(p0, n0, off - w / 2, s0), V(p1, n1, off - w / 2, s0 + dash), V(p1, n1, off + w / 2, s0 + dash), V(p0, n0, off + w / 2, s0), Vector3.up);
            }
        }

        // ------------------------------------------------------------ bridges (deck and parapets; unlike the roads these have colliders)

        // mapgen holds each bridge level across the water: deck_y is the deck height every ~2 m. The car drives on
        // the deck mesh; the bare-earth channel stays on the terrain underneath.
        static void BuildBridges(MapData map, Materials mats, Transform parent, Chunks paint)
        {
            var top = new MeshBuilder();
            var body = new MeshBuilder();
            const float parapetH = 0.7f, parapetW = 0.35f, slab = 0.7f;
            foreach (var b in map.bridges)
            {
                var pts = ToPoints(b.points);
                int n = pts.Count;
                if (n < 2) continue;
                float lift = (RoadLift.TryGetValue(b.@class, out var l) ? l : 0.06f) * vScale;
                float hw = b.width * 0.5f;
                var nrm = new Vector2[n];
                for (int i = 0; i < n; i++)
                {
                    Vector2 t = (pts[Mathf.Min(i + 1, n - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                    nrm[i] = new Vector2(-t.y, t.x); // left of travel
                }
                Vector3 P(int i, float off, float dy) => new Vector3(pts[i].x + nrm[i].x * off, b.deck_y[i] + lift + dy, pts[i].y + nrm[i].y * off);
                Vector3 N(int i, float sgn) => new Vector3(nrm[i].x * sgn, 0f, nrm[i].y * sgn);
                for (int i = 0; i + 1 < n; i++)
                {
                    int j = i + 1;
                    Face(top, P(i, -hw, 0f), P(j, -hw, 0f), P(j, hw, 0f), P(i, hw, 0f), Vector3.up);
                    foreach (float sgn in new[] { -1f, 1f })
                    {
                        float inner = sgn * hw, outer = sgn * (hw + parapetW);
                        Face(body, P(i, inner, 0f), P(j, inner, 0f), P(j, inner, parapetH), P(i, inner, parapetH), -N(i, sgn));
                        Face(body, P(i, inner, parapetH), P(j, inner, parapetH), P(j, outer, parapetH), P(i, outer, parapetH), Vector3.up);
                        Face(body, P(i, outer, -slab), P(j, outer, -slab), P(j, outer, parapetH), P(i, outer, parapetH), N(i, sgn));
                    }
                    Face(body, P(i, -hw - parapetW, -slab), P(j, -hw - parapetW, -slab), P(j, hw + parapetW, -slab), P(i, hw + parapetW, -slab), Vector3.down);
                }
                // paint on the deck: the deck height is deck_y (every point), interpolated along the arc length
                var arcs = new float[n];
                for (int i = 1; i < n; i++) arcs[i] = arcs[i - 1] + Vector2.Distance(pts[i - 1], pts[i]);
                var deck = b.deck_y;
                float DeckAt(float s)
                {
                    int k = Array.BinarySearch(arcs, s);
                    if (k < 0) k = ~k;
                    k = Mathf.Clamp(k, 1, n - 1);
                    float f = Mathf.Clamp01((s - arcs[k - 1]) / Mathf.Max(arcs[k] - arcs[k - 1], 1e-4f));
                    return Mathf.Lerp(deck[k - 1], deck[k], f);
                }
                AddPaint(paint, pts, b.@class, b.width, b.oneway, (p, s) => DeckAt(s) + lift, p => false);
            }
            var root = new GameObject("Bridges").transform;
            root.SetParent(parent, false);
            top.ToObject("BridgeDecks", mats.Asphalt, root, true);
            body.ToObject("BridgeStructure", mats.Concrete, root, true);
        }

        // Adds a quad whose triangles wind so that its front face points along 'outward'.
        static void Face(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) { var t = b; b = d; d = t; }
            int v = mb.Vertex(a);
            mb.Vertex(b); mb.Vertex(c); mb.Vertex(d);
            mb.Tri(v, v + 1, v + 2);
            mb.Tri(v, v + 2, v + 3);
        }

        static void TriFace(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) { var t = b; b = c; c = t; }
            int v = mb.Vertex(a);
            mb.Vertex(b); mb.Vertex(c);
            mb.Tri(v, v + 1, v + 2);
        }

        // ------------------------------------------------------------ buildings
        // Detached houses (and other pitched-roof types: mapgen sets "roof": "gable") get walls up to the eaves and a
        // pitched roof on the footprint's best-fit rectangle: a gable along the long side, or a hip roof when the
        // footprint is nearly square. Footprints that are not close to a rectangle get a pyramid roof. Everything else
        // (commercial, industrial, apartments, small sheds) keeps a flat roof. Wall and roof colours come from small
        // palettes, picked by the building's stable seed (mapgen: a hash of where it stands), so they never reshuffle.

        static readonly Color[] WallPalette =
        {
            new Color(0.86f, 0.82f, 0.70f), new Color(0.88f, 0.88f, 0.85f), new Color(0.70f, 0.71f, 0.72f), new Color(0.76f, 0.66f, 0.50f),
            new Color(0.62f, 0.70f, 0.78f), new Color(0.64f, 0.72f, 0.60f), new Color(0.58f, 0.30f, 0.24f), new Color(0.55f, 0.45f, 0.36f),
        };
        static readonly Color[] RoofPalette =
        {
            new Color(0.20f, 0.20f, 0.21f), new Color(0.28f, 0.20f, 0.16f), new Color(0.36f, 0.36f, 0.37f), new Color(0.13f, 0.13f, 0.14f), new Color(0.40f, 0.26f, 0.18f),
        };
        static readonly Color[] FlatWallPalette =
        {
            new Color(0.78f, 0.74f, 0.66f), new Color(0.70f, 0.70f, 0.70f), new Color(0.62f, 0.50f, 0.42f), new Color(0.82f, 0.80f, 0.74f),
        };
        static readonly Color[] FlatRoofPalette = { new Color(0.42f, 0.42f, 0.43f), new Color(0.50f, 0.49f, 0.47f), new Color(0.34f, 0.34f, 0.35f) };

        static uint Mix(int seed, int salt)
        {
            uint x = (uint)seed * 2654435761u + (uint)salt * 40503u;
            x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; x *= 3266489917u; x ^= x >> 16;
            return x;
        }
        static Color PickColour(Color[] palette, int seed, int salt) => palette[Mix(seed, salt) % (uint)palette.Length];

        static OwnerMarker LoadOwner()
        {
            var path = Path.Combine(ProjectRoot, "local/owner_marker.json");
            return File.Exists(path) ? JsonUtility.FromJson<OwnerMarker>(File.ReadAllText(path)) : null;
        }

        static readonly Color OwnerWall = new Color(1.0f, 0.45f, 0.05f), OwnerRoof = new Color(0.62f, 0.22f, 0.04f);

        static void BuildBuildings(MapData map, TerrainGrid terrain, Materials mats, Transform parent, OwnerMarker owner, Neighborhood hood, ShoppingCenter shop)
        {
            const float cell = 256f;
            var chunks = new Dictionary<Vector2Int, MeshBuilder>();
            var bparent = new GameObject("Buildings").transform;
            bparent.SetParent(parent, false);
            int gable = 0, hip = 0, pyramid = 0, flat = 0;
            for (int i = 0; i < map.buildings.Length; i++)
            {
                var b = map.buildings[i];
                if (hood.OwnsBuilding(i)) continue; // Round 7: styled houses in the area, Dan's house hand-built
                if (shop.Owns(i)) continue;          // Round 17: the gas station's canopy and the bank are built with the shopping center
                var ring = ToPoints(b.footprint);
                if (ring.Count < 3) continue;
                float lo = float.MaxValue, sum = 0f;
                Vector2 c = Vector2.zero;
                foreach (var p in ring)
                {
                    float g = Ground(terrain, p.x, p.y);
                    lo = Mathf.Min(lo, g);
                    sum += g;
                    c += p;
                }
                c /= ring.Count;
                float baseY = lo - 0.5f * vScale, topY = sum / ring.Count + b.height;
                bool isOwner = owner != null && owner.index == i;
                bool pitched = b.roof == "gable";
                Color wall = isOwner ? OwnerWall : pitched ? PickColour(WallPalette, b.seed, 1) : PickColour(FlatWallPalette, b.seed, 1);
                Color roofC = isOwner ? OwnerRoof : pitched ? PickColour(RoofPalette, b.seed, 2) : PickColour(FlatRoofPalette, b.seed, 2);
                MeshBuilder mb;
                if (isOwner) mb = new MeshBuilder();
                else
                {
                    var key = new Vector2Int(Mathf.FloorToInt(c.x / cell), Mathf.FloorToInt(c.y / cell));
                    if (!chunks.TryGetValue(key, out mb)) chunks[key] = mb = new MeshBuilder();
                }
                int shape = pitched ? AddPitched(mb, ring, baseY, topY, b.height, b.pitch_deg, wall, roofC) : -1;
                if (shape < 0) { Extrude(mb, ring, baseY, topY, wall, roofC, true); flat++; }
                else if (shape == 0) gable++; else if (shape == 1) hip++; else pyramid++;
                if (isOwner)
                {
                    mb.ToObject("OwnerHouse", mats.House, bparent, true);   // Round 10: no beacon over it
                }
            }
            foreach (var kv in chunks) kv.Value.ToObject($"Buildings_{kv.Key.x}_{kv.Key.y}", mats.House, bparent, true);
            Debug.Log($"[MapBuilder] Roofs: {gable} gable, {hip} hip, {pyramid} pyramid (footprint not close to a rectangle), {flat} flat");
        }

        // Walls to the eaves plus the roof. Returns 0 gable, 1 hip, 2 pyramid; -1 if the footprint is too small or odd.
        static int AddPitched(MeshBuilder mb, List<Vector2> ring, float baseY, float topY, float height, float pitchDeg, Color wall, Color roof)
        {
            float area = Mathf.Abs(PolyArea(ring));
            float tan = Mathf.Tan(Mathf.Clamp(pitchDeg, 10f, 45f) * Mathf.Deg2Rad);
            bool box = BestRectangle(ring, out Vector2 c, out Vector2 U, out Vector2 V, out float hl, out float hw);
            float rect = box ? area / (4f * hl * hw) : 0f;
            if (box && hw >= 1.8f && rect >= 0.72f)
            {
                float rise = Mathf.Clamp(tan * hw * vScale, 0.5f, 0.45f * height);
                float eaveY = topY - rise;
                Extrude(mb, ring, baseY, eaveY, wall, roof, false);
                const float oh = 0.35f;
                float slope = rise / hw, dropY = eaveY - slope * oh;
                bool hipRoof = hl / hw < 1.35f;
                float endX = hipRoof ? hl + oh : hl + 0.25f, ridgeHalf = hipRoof ? Mathf.Max(hl - hw, 0.25f) : hl + 0.25f;
                Vector3 W(float a, float b2, float y) { var q = c + U * a + V * b2; return new Vector3(q.x, y, q.y); }
                Vector3 A = W(endX, hw + oh, dropY), B = W(endX, -hw - oh, dropY), C = W(-endX, -hw - oh, dropY), D = W(-endX, hw + oh, dropY);
                Vector3 R0 = W(ridgeHalf, 0f, topY), R1 = W(-ridgeHalf, 0f, topY);
                var up = Vector3.up;
                Vector3 nPos = new Vector3(V.x * slope, 1f, V.y * slope), nNeg = new Vector3(-V.x * slope, 1f, -V.y * slope);
                mb.Tint = roof;
                Face(mb, A, D, R1, R0, nPos);
                Face(mb, B, C, R1, R0, nNeg);
                if (hipRoof)
                {
                    float endSlope = rise / Mathf.Max(endX - ridgeHalf, 0.3f);
                    TriFace(mb, A, B, R0, new Vector3(U.x * endSlope, 1f, U.y * endSlope));
                    TriFace(mb, C, D, R1, new Vector3(-U.x * endSlope, 1f, -U.y * endSlope));
                }
                else
                {
                    mb.Tint = wall;
                    TriFace(mb, W(hl, hw, eaveY), W(hl, -hw, eaveY), W(hl, 0f, topY), new Vector3(U.x, 0f, U.y));
                    TriFace(mb, W(-hl, hw, eaveY), W(-hl, -hw, eaveY), W(-hl, 0f, topY), new Vector3(-U.x, 0f, -U.y));
                }
                mb.Tint = Color.white;
                return hipRoof ? 1 : 0;
            }
            if (area < 25f) return -1;
            {
                // not a rectangle: a low pyramid over the footprint, apex at its centre
                float perim = 0f;
                for (int i = 0; i < ring.Count; i++) perim += Vector2.Distance(ring[i], ring[(i + 1) % ring.Count]);
                float inradius = 2f * area / Mathf.Max(perim, 1f);
                float rise = Mathf.Clamp(tan * inradius * vScale, 0.5f, 0.4f * height);
                float eaveY = topY - rise;
                Extrude(mb, ring, baseY, eaveY, wall, roof, false);
                Vector2 centre = Vector2.zero;
                foreach (var p in ring) centre += p;
                centre /= ring.Count;
                var apex = new Vector3(centre.x, topY, centre.y);
                mb.Tint = roof;
                for (int i = 0; i < ring.Count; i++)
                {
                    var a = ring[i]; var b2 = ring[(i + 1) % ring.Count];
                    TriFace(mb, new Vector3(a.x, eaveY, a.y), new Vector3(b2.x, eaveY, b2.y), apex, Vector3.up);
                }
                mb.Tint = Color.white;
                return 2;
            }
        }

        static float PolyArea(List<Vector2> r)
        {
            float a = 0f;
            for (int i = 0; i < r.Count; i++) { var p = r[i]; var q = r[(i + 1) % r.Count]; a += p.x * q.y - q.x * p.y; }
            return a * 0.5f;
        }

        // Smallest-area rectangle that has a side along one of the footprint's edges. U is the long axis.
        static bool BestRectangle(List<Vector2> ring, out Vector2 centre, out Vector2 U, out Vector2 V, out float hl, out float hw)
        {
            centre = Vector2.zero; U = Vector2.right; V = Vector2.up; hl = hw = 0f;
            float best = float.MaxValue;
            for (int i = 0; i < ring.Count; i++)
            {
                var d = ring[(i + 1) % ring.Count] - ring[i];
                if (d.sqrMagnitude < 1e-4f) continue;
                d.Normalize();
                var p = new Vector2(-d.y, d.x);
                float u0 = float.MaxValue, u1 = float.MinValue, v0 = float.MaxValue, v1 = float.MinValue;
                foreach (var q in ring)
                {
                    float a = Vector2.Dot(q, d), b = Vector2.Dot(q, p);
                    u0 = Mathf.Min(u0, a); u1 = Mathf.Max(u1, a); v0 = Mathf.Min(v0, b); v1 = Mathf.Max(v1, b);
                }
                float area = (u1 - u0) * (v1 - v0);
                if (area >= best) continue;
                best = area;
                centre = d * ((u0 + u1) * 0.5f) + p * ((v0 + v1) * 0.5f);
                if (u1 - u0 >= v1 - v0) { U = d; V = p; hl = (u1 - u0) * 0.5f; hw = (v1 - v0) * 0.5f; }
                else { U = p; V = -d; hl = (v1 - v0) * 0.5f; hw = (u1 - u0) * 0.5f; }
            }
            return best < float.MaxValue && hw > 0.1f;
        }

        static void Extrude(MeshBuilder mb, List<Vector2> ring, float baseY, float topY, Color wall, Color roof, bool flatRoof)
        {
            mb.Tint = wall;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
                int v = mb.Count;
                mb.Vertex(new Vector3(a.x, baseY, a.y));
                mb.Vertex(new Vector3(b.x, baseY, b.y));
                mb.Vertex(new Vector3(b.x, topY, b.y));
                mb.Vertex(new Vector3(a.x, topY, a.y));
                // ring is counter-clockwise seen from above, so outward faces wind like this
                mb.Tri(v, v + 2, v + 1);
                mb.Tri(v, v + 3, v + 2);
            }
            if (flatRoof)
            {
                mb.Tint = roof;
                int top = mb.Count;
                foreach (var p in ring) mb.Vertex(new Vector3(p.x, topY, p.y));
                foreach (var tri in EarClip(ring)) mb.Tri(top + tri.x, top + tri.z, top + tri.y);
            }
            mb.Tint = Color.white;
        }

        // Round 10: every road and drive of the map as segments with a half width, for keeping tree trunks off them
        sealed class Pavement
        {
            readonly List<(Vector2 a, Vector2 b, float half)> segs = new List<(Vector2, Vector2, float)>();
            readonly Dictionary<Vector2Int, List<int>> grid = new Dictionary<Vector2Int, List<int>>();
            const float Cell = 32f;
            public int Moved, Dropped;
            public Pavement(MapData map)
            {
                foreach (var r in map.roads) Add(ToPoints(r.points), r.width * 0.5f);
                if (map.driveways != null) foreach (var d in map.driveways) Add(ToPoints(d.points), Mathf.Max(1.5f, d.width * 0.5f));
            }
            void Add(List<Vector2> p, float half)
            {
                for (int i = 0; i + 1 < p.Count; i++)
                {
                    int id = segs.Count; segs.Add((p[i], p[i + 1], half));
                    float pad = half + 3f;
                    for (int x = Mathf.FloorToInt((Mathf.Min(p[i].x, p[i + 1].x) - pad) / Cell); x <= Mathf.FloorToInt((Mathf.Max(p[i].x, p[i + 1].x) + pad) / Cell); x++)
                        for (int z = Mathf.FloorToInt((Mathf.Min(p[i].y, p[i + 1].y) - pad) / Cell); z <= Mathf.FloorToInt((Mathf.Max(p[i].y, p[i + 1].y) + pad) / Cell); z++)
                        {
                            var key = new Vector2Int(x, z);
                            if (!grid.TryGetValue(key, out var l)) grid[key] = l = new List<int>();
                            l.Add(id);
                        }
                }
            }
            // the segment whose edge p is furthest inside (most negative clearance)
            bool Worst(Vector2 p, float margin, out Vector2 foot, out float half, out float d)
            {
                foot = p; half = 0f; d = 0f; float best = float.MaxValue;
                if (!grid.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell)), out var l)) return false;
                foreach (int id in l)
                {
                    var s = segs[id]; var ab = s.b - s.a;
                    float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                    var c = s.a + ab * t; float dd = (c - p).magnitude, clear = dd - s.half;
                    if (clear < margin && clear < best) { best = clear; foot = c; half = s.half; d = dd; }
                }
                return best < margin;
            }
            public bool On(Vector2 p, float margin) => Worst(p, margin, out _, out _, out _);
            // Round 15: how far p is from the nearest pavement edge (negative on it), up to 'range'; bridges can be added
            public float Clearance(Vector2 p, float range) => Worst(p, range, out _, out float half, out float d) ? d - half : range;
            public void AddLines(IEnumerable<(List<Vector2> p, float half)> lines) { foreach (var (p, h) in lines) Add(p, h); }
            public Vector2 MoveOff(Vector2 p)
            {
                if (!Worst(p, 0.6f, out var foot, out float half, out float d)) return p;
                var away = d > 1e-3f ? (p - foot) / d : Vector2.right;
                var np = foot + away * (half + 1.5f);
                if (On(np, 0.6f)) { Dropped++; return p; }   // the reject test drops it
                Moved++; return np;
            }
        }

        // ------------------------------------------------------------ driveways (visual only: nothing is levelled)

        static void BuildDriveways(MapData map, TerrainGrid terrain, Materials mats, Transform parent, Neighborhood hood)
        {
            var mb = new Chunks { Tint = new Color(0.60f, 0.59f, 0.57f) };
            float lift = 0.05f * vScale;
            int osm = 0, generated = 0;
            for (int di = 0; di < map.driveways.Length; di++)
            {
                var d = map.driveways[di];
                if (hood.OwnsDriveway(di)) continue; // Round 7: the area's driveways come with its houses
                var pts = Densify(ToPoints(d.points), 1.5f);
                if (pts.Count < 2) continue;
                foreach (var run in Runs(pts, 96f)) AddRibbon(mb.At(run[run.Count / 2]), run, d.width, lift, terrain);
                if (d.source == "osm") osm++; else generated++;
            }
            mb.ToObjects("Driveways", mats.Driveway, parent, false);
            Debug.Log($"[MapBuilder] Driveways: {osm} from OSM, {generated} generated");
        }

        // ------------------------------------------------------------ water

        static void BuildWater(MapData map, TerrainGrid terrain, Materials mats, Transform parent, Neighborhood hood)
        {
            var mb = new MeshBuilder();
            var pave = new Pavement(map);
            for (int wi = 0; wi < map.water.Length; wi++)
            {
                var w = map.water[wi];
                if (hood.OwnsPond(wi)) continue; // Round 7: the pond is built with its surroundings
                var ring = ToPoints(w.outline);
                if (ring.Count < 3) continue;
                float level = Beds.Level.TryGetValue(wi, out float lv) ? lv : WaterLevel(ring, terrain);   // Round 15: measured before the bed was cut
                int v = mb.Count;
                foreach (var p in ring) mb.Vertex(new Vector3(p.x, level + PondLift, p.y));
                foreach (var tri in EarClip(ring)) mb.Tri(v + tri.x, v + tri.z, v + tri.y);
            }
            // Round 15: a stream is drawn level across at its surface, as wide as the water in its channel, and not on pavement
            foreach (var (pts, surf) in Beds.Streams)
            {
                var run = new List<Vector3>();
                for (int i = 0; i <= pts.Count; i++)
                {
                    bool dry = i == pts.Count || pave.Clearance(pts[i], 6f) < 1f;
                    if (!dry) { run.Add(new Vector3(pts[i].x, surf[i], pts[i].y)); continue; }
                    if (run.Count >= 2) AddLevelRibbon(mb, run, StreamHalf * 2f);
                    run.Clear();
                }
            }
            mb.ToObject("Water", mats.Water, parent, false);
        }

        // Round 15: a ribbon at the given heights (level across), for the streams
        static void AddLevelRibbon(MeshBuilder mb, List<Vector3> pts, float width)
        {
            int start = mb.Count;
            for (int i = 0; i < pts.Count; i++)
            {
                var a = pts[Mathf.Max(i - 1, 0)]; var b = pts[Mathf.Min(i + 1, pts.Count - 1)];
                var t = new Vector2(b.x - a.x, b.z - a.z).normalized; var n = new Vector3(-t.y, 0f, t.x) * (width * 0.5f);
                mb.Vertex(pts[i] + n); mb.Vertex(pts[i] - n);
                if (i > 0) { int k = start + (i - 1) * 2; mb.Tri(k, k + 2, k + 1); mb.Tri(k + 1, k + 2, k + 3); }
            }
        }

        // The lidar surface is hydro-flattened: open water is one flat level, so the median ground height inside the
        // outline is the water level. The outline's own lowest point usually sits below it, on the bank or a ditch.
        static float WaterLevel(List<Vector2> ring, TerrainGrid terrain)
        {
            Vector2 min = ring[0], max = ring[0];
            foreach (var p in ring) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            var samples = new List<float>();
            for (float x = min.x; x <= max.x; x += 2f)
                for (float z = min.y; z <= max.y; z += 2f)
                    if (InPolygon(new Vector2(x, z), ring)) samples.Add(Ground(terrain, x, z));
            if (samples.Count == 0)
            {
                float lo = float.MaxValue;
                foreach (var p in ring) lo = Mathf.Min(lo, Ground(terrain, p.x, p.y));
                return lo;
            }
            samples.Sort();
            return samples[samples.Count / 2];
        }

        static bool InPolygon(Vector2 p, List<Vector2> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        // ------------------------------------------------------------ geometry helpers

        static List<Vector2> ToPoints(float[] flat)
        {
            var list = new List<Vector2>(flat.Length / 2);
            for (int i = 0; i + 1 < flat.Length; i += 2) list.Add(new Vector2(flat[i], flat[i + 1]));
            return list;
        }

        static List<Vector2> Densify(List<Vector2> pts, float step)
        {
            var o = new List<Vector2> { pts[0] };
            for (int i = 1; i < pts.Count; i++)
            {
                float d = Vector2.Distance(pts[i - 1], pts[i]);
                if (d < 0.01f) continue;
                int n = Mathf.Max(1, Mathf.CeilToInt(d / step));
                for (int k = 1; k <= n; k++) o.Add(Vector2.Lerp(pts[i - 1], pts[i], k / (float)n));
            }
            if (o.Count == 1) o.Add(pts[pts.Count - 1] + Vector2.right * 0.01f);
            return o;
        }

        // Triangulates a simple counter-clockwise polygon. Returns index triples (CCW).
        static List<Vector3Int> EarClip(List<Vector2> poly)
        {
            var result = new List<Vector3Int>();
            var idx = new List<int>();
            for (int i = 0; i < poly.Count; i++) idx.Add(i);
            int guard = poly.Count * poly.Count + 10;
            while (idx.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int ia = idx[(i + idx.Count - 1) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                    Vector2 a = poly[ia], b = poly[ib], c = poly[ic];
                    if (Cross(b - a, c - b) <= 1e-7f) continue; // reflex or degenerate
                    bool contains = false;
                    foreach (int j in idx)
                        if (j != ia && j != ib && j != ic && InTriangle(poly[j], a, b, c)) { contains = true; break; }
                    if (contains) continue;
                    result.Add(new Vector3Int(ia, ib, ic));
                    idx.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break; // self-intersecting outline: fan the rest
            }
            for (int i = 1; i + 1 < idx.Count; i++) result.Add(new Vector3Int(idx[0], idx[i], idx[i + 1]));
            return result;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c) =>
            Cross(b - a, p - a) >= 0f && Cross(c - b, p - b) >= 0f && Cross(a - c, p - c) >= 0f;

        // Meshes split into 128 m cells, so the renderer can cull what is out of view (one mesh for the whole map is
        // drawn, and its vertices processed, every frame).
        sealed class Chunks
        {
            const float Cell = 128f;
            readonly Dictionary<Vector2Int, MeshBuilder> cells = new Dictionary<Vector2Int, MeshBuilder>();
            public Color Tint = Color.white;
            public MeshBuilder At(Vector2 p)
            {
                var key = new Vector2Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell));
                if (!cells.TryGetValue(key, out var mb)) cells[key] = mb = new MeshBuilder();
                mb.Tint = Tint;
                return mb;
            }
            public void ToObjects(string name, Material mat, Transform parent, bool castShadows)
            {
                var root = new GameObject(name).transform;
                root.SetParent(parent, false);
                foreach (var kv in cells) kv.Value.ToObject($"{name}_{kv.Key.x}_{kv.Key.y}", mat, root, false, castShadows);
            }
        }

        // pieces of a polyline of about 'length' metres each (sharing end points), so each can go to its own chunk
        static IEnumerable<List<Vector2>> Runs(List<Vector2> pts, float length)
        {
            var run = new List<Vector2> { pts[0] };
            float acc = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                run.Add(pts[i]);
                acc += Vector2.Distance(pts[i - 1], pts[i]);
                if (acc >= length && i < pts.Count - 1) { yield return run; run = new List<Vector2> { pts[i] }; acc = 0f; }
            }
            if (run.Count >= 2) yield return run;
        }

        class MeshBuilder
        {
            readonly List<Vector3> verts = new List<Vector3>();
            readonly List<Color> colours = new List<Color>();
            readonly List<int> tris = new List<int>();
            public Color Tint = Color.white; // colour of the vertices added from now on
            public int Count => verts.Count;
            public List<Vector3> Verts => verts; public List<Color> Colours => colours; public List<int> Tris => tris;   // Round 8: for clipping
            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(verts); mesh.SetColors(colours); mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                return mesh;
            }
            public List<Vector3> VertexRange(int start, int count) => verts.GetRange(start, count);
            public int Vertex(Vector3 v) { verts.Add(v); colours.Add(Tint); return verts.Count - 1; }
            public void Tri(int a, int b, int c) { tris.Add(a); tris.Add(b); tris.Add(c); }

            public GameObject ToObject(string name, Material mat, Transform parent, bool collider, bool castShadows = true)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                // static for batching and occlusion, but not a GI contributor: no lightmaps are baked, and the
                // VertexLit shader takes its ambient from the probe (SampleSH), which lightmapped objects don't get
                GameObjectUtility.SetStaticEditorFlags(go, (StaticEditorFlags)int.MaxValue & ~StaticEditorFlags.ContributeGI);
                if (verts.Count == 0) return go;
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(verts);
                mesh.SetColors(colours);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/Meshes/{name}.asset");
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = mat;
                renderer.receiveGI = ReceiveGI.LightProbes;
                if (!castShadows) renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
                return go;
            }
        }

        class Materials
        {
            public readonly Material Asphalt, Concrete, ServiceAsphalt, House, Paint, Driveway, Sidewalk, Foliage, Sky, Beacon, Water, TerrainMaterial;
            public readonly Material GateBlack, GateWhite, RaceNext, RaceLater, RaceMissed, RaceShortcut; // Round 5 race markers (unlit); Round 8 shortcut entries
            public readonly TerrainLayer GrassLayer;

            public Materials()
            {
                AssetDatabase.DeleteAsset(GeneratedDir + "/Meshes"); // drop chunks left over from a larger previous map
                Directory.CreateDirectory(Path.Combine(ProjectRoot, GeneratedDir, "Meshes"));
                AssetDatabase.Refresh();
                // ChastainRush/VertexLit: lit by the sun, shadows and fog, coloured per vertex (roads, houses, paint, trees).
                // Roads, paint and driveways carry a depth offset so that what lies on the ground does not flicker with it.
                Asphalt = MakeVertexLit("Asphalt", new Color(0.18f, 0.18f, 0.19f), 0f, -1f, -1f);
                Concrete = MakeVertexLit("Concrete", new Color(0.55f, 0.55f, 0.53f), 0f, 0f, 0f);
                ServiceAsphalt = MakeVertexLit("ServiceAsphalt", new Color(0.30f, 0.30f, 0.30f), 0f, -1f, -1f);
                House = MakeVertexLit("House", Color.white, 0f, 0f, 0f);
                Paint = MakeVertexLit("RoadPaint", Color.white, 0f, -3f, -3f);
                Driveway = MakeVertexLit("Driveway", Color.white, 0f, -1f, -1f);
                Sidewalk = MakeVertexLit("Sidewalk", Color.white, 0f, -2f, -2f);   // Round 11: drawn over the drives it crosses
                // Round 15: instancing and the leaf tint are set before the asset is written; set after, they lived only in memory,
                // and the exe drew no trees (and nothing after them) whenever the material was read back from disk
                Foliage = MakeVertexLit("Foliage", Color.white, 1f, 0f, 0f, m => { m.enableInstancing = true; m.SetFloat("_LeafTint", 1f); });
                Sky = new Material(Shader.Find("ChastainRush/SkyGradient")) { name = "Sky" };
                Sky.SetColor("_Horizon", HazeColor);
                AssetDatabase.CreateAsset(Sky, GeneratedDir + "/Sky.mat");
                Beacon = Make("Beacon", new Color(1.0f, 0.55f, 0.1f), 0f, new Color(2.5f, 1.2f, 0.2f));
                Water = Make("Water", new Color(0.16f, 0.32f, 0.50f), 0.85f);
                GateBlack = MakeUnlit("GateBlack", new Color(0.05f, 0.05f, 0.05f));
                GateWhite = MakeUnlit("GateWhite", new Color(0.95f, 0.95f, 0.95f));
                RaceNext = MakeUnlit("RaceNext", new Color(0.35f, 1.0f, 0.2f));
                RaceLater = MakeUnlit("RaceLater", new Color(1.0f, 0.62f, 0.1f));
                RaceMissed = MakeUnlit("RaceMissed", new Color(0.9f, 0.1f, 0.1f));
                RaceShortcut = MakeUnlit("RaceShortcut", new Color(0.15f, 0.65f, 1.0f));

                var tex = new Texture2D(4, 4);
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = new Color(0.36f, 0.46f, 0.27f, 0f); // URP terrain reads smoothness from alpha
                tex.SetPixels(px);
                tex.Apply();
                AssetDatabase.CreateAsset(tex, GeneratedDir + "/GrassFlat.asset");
                GrassLayer = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(10f, 10f) };
                AssetDatabase.CreateAsset(GrassLayer, GeneratedDir + "/Grass.terrainlayer");

                var terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
                if (terrainShader != null)
                {
                    TerrainMaterial = new Material(terrainShader);
                    AssetDatabase.CreateAsset(TerrainMaterial, GeneratedDir + "/Terrain.mat");
                }
            }

            static Material MakeVertexLit(string name, Color tint, float sway, float offsetFactor, float offsetUnits, Action<Material> setup = null)
            {
                var shader = Shader.Find("ChastainRush/VertexLit");
                if (shader == null) throw new Exception("Shader ChastainRush/VertexLit not found");
                var m = new Material(shader) { name = name };
                m.SetColor("_BaseColor", tint);
                m.SetFloat("_Sway", sway);
                m.SetFloat("_OffsetFactor", offsetFactor);
                m.SetFloat("_OffsetUnits", offsetUnits);
                setup?.Invoke(m);
                AssetDatabase.CreateAsset(m, $"{GeneratedDir}/{name}.mat");
                return m;
            }

            static Material MakeUnlit(string name, Color color)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                var m = new Material(shader) { name = name, color = color };
                m.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(m, $"{GeneratedDir}/{name}.mat");
                return m;
            }

            static Material Make(string name, Color color, float smoothness, Color? emission = null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var m = new Material(shader) { name = name, color = color };
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Glossiness", smoothness);
                if (emission.HasValue)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", emission.Value);
                }
                AssetDatabase.CreateAsset(m, $"{GeneratedDir}/{name}.mat");
                return m;
            }
        }
    }
}
