using System;
using System.Collections.Generic;
using System.IO;
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
    public static class MapBuilder
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

        [Serializable] class Road { public string name; public string @class; public float width; public bool oneway; public float[] points; }
        [Serializable] class Building { public string type; public float height; public float[] footprint; }
        [Serializable] class Area { public string name; public float[] outline; }
        [Serializable] class Line { public string name; public string kind; public float[] points; }
        [Serializable] class Spawn { public float x; public float z; public float heading_deg; public string street; }
        [Serializable] class MapData { public string attribution; public float size_m; public Road[] roads; public Building[] buildings; public Area[] water; public Line[] streams; public Spawn spawn; }
        [Serializable] class TerrainMeta { public int resolution; public float size_m; public float min_height; public float max_height; }
        [Serializable] class OwnerMarker { public int index; public float x; public float z; }

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

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.50f, 0.52f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.25f);

            var terrain = BuildTerrain(meta, mats);
            var world = new GameObject("World").transform;
            BuildRoads(map, terrain, mats, world);
            BuildBuildings(map, terrain, mats, world, LoadOwner());
            BuildWater(map, terrain, mats, world);

            new GameObject("MapCredits").AddComponent<MapCredits>().text = "© OpenStreetMap contributors";
            PlayerSetup.Place(map.spawn.x, map.spawn.z, map.spawn.heading_deg, terrain);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[MapBuilder] Built {ScenePath}: {map.roads.Length} roads, {map.buildings.Length} buildings, " +
                      $"{map.water.Length} water areas, spawn on {map.spawn.street}");
        }

        // ------------------------------------------------------------ terrain

        static Terrain BuildTerrain(TerrainMeta meta, Materials mats)
        {
            int res = meta.resolution;
            var bytes = File.ReadAllBytes(Path.Combine(ProjectRoot, GeneratedDir, "terrain.r16"));
            if (bytes.Length != res * res * 2) throw new Exception($"terrain.r16 is {bytes.Length} bytes, expected {res * res * 2}");
            var heights = new float[res, res];
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    int i = (z * res + x) * 2;
                    heights[z, x] = (bytes[i] | (bytes[i + 1] << 8)) / 65535f;
                }

            var data = new TerrainData { heightmapResolution = res };
            data.size = new Vector3(meta.size_m, meta.max_height - meta.min_height, meta.size_m);
            data.SetHeights(0, 0, heights);
            data.terrainLayers = new[] { mats.GrassLayer };
            AssetDatabase.CreateAsset(data, GeneratedDir + "/TerrainData.asset");

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.position = new Vector3(0f, meta.min_height, 0f);
            var terrain = go.GetComponent<Terrain>();
            terrain.heightmapPixelError = 3f;
            terrain.basemapDistance = 2000f;
            terrain.drawTreesAndFoliage = false;
            if (mats.TerrainMaterial != null) terrain.materialTemplate = mats.TerrainMaterial;
            return terrain;
        }

        static float Ground(Terrain t, float x, float z) => t.SampleHeight(new Vector3(x, 0f, z)) + t.transform.position.y;

        // ------------------------------------------------------------ roads (visual only; the car drives on the flattened terrain)

        static void BuildRoads(MapData map, Terrain terrain, Materials mats, Transform parent)
        {
            var main = new MeshBuilder();
            var service = new MeshBuilder();
            foreach (var r in map.roads)
            {
                var pts = Densify(ToPoints(r.points), 2f);
                float lift = RoadLift.TryGetValue(r.@class, out var l) ? l : 0.06f;
                var mb = r.@class == "service" ? service : main;
                AddRibbon(mb, pts, r.width, lift, terrain);
                AddDisc(mb, pts[0], r.width * 0.5f, lift - 0.004f, terrain);
                AddDisc(mb, pts[pts.Count - 1], r.width * 0.5f, lift - 0.004f, terrain);
            }
            main.ToObject("Roads", mats.Asphalt, parent, false);
            service.ToObject("ServiceRoads", mats.ServiceAsphalt, parent, false);
        }

        static void AddRibbon(MeshBuilder mb, List<Vector2> pts, float width, float lift, Terrain t)
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

        static void AddDisc(MeshBuilder mb, Vector2 c, float radius, float lift, Terrain t)
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

        // ------------------------------------------------------------ buildings

        static OwnerMarker LoadOwner()
        {
            var path = Path.Combine(ProjectRoot, "local/owner_marker.json");
            return File.Exists(path) ? JsonUtility.FromJson<OwnerMarker>(File.ReadAllText(path)) : null;
        }

        static void BuildBuildings(MapData map, Terrain terrain, Materials mats, Transform parent, OwnerMarker owner)
        {
            const float cell = 256f;
            var chunks = new Dictionary<Vector2Int, MeshBuilder>();
            var bparent = new GameObject("Buildings").transform;
            bparent.SetParent(parent, false);
            for (int i = 0; i < map.buildings.Length; i++)
            {
                var b = map.buildings[i];
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
                float baseY = lo - 0.5f, topY = sum / ring.Count + b.height;
                if (owner != null && owner.index == i)
                {
                    var own = new MeshBuilder();
                    Extrude(own, ring, baseY, topY);
                    own.ToObject("OwnerHouse", mats.OwnerHouse, bparent, true);
                    AddBeacon(c, topY, mats, parent);
                    continue;
                }
                var key = new Vector2Int(Mathf.FloorToInt(c.x / cell), Mathf.FloorToInt(c.y / cell));
                if (!chunks.TryGetValue(key, out var mb)) chunks[key] = mb = new MeshBuilder();
                Extrude(mb, ring, baseY, topY);
            }
            foreach (var kv in chunks) kv.Value.ToObject($"Buildings_{kv.Key.x}_{kv.Key.y}", mats.Building, bparent, true);
        }

        static void Extrude(MeshBuilder mb, List<Vector2> ring, float baseY, float topY)
        {
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
            int roof = mb.Count;
            foreach (var p in ring) mb.Vertex(new Vector3(p.x, topY, p.y));
            foreach (var tri in EarClip(ring)) mb.Tri(roof + tri.x, roof + tri.z, roof + tri.y);
        }

        static void AddBeacon(Vector2 c, float topY, Materials mats, Transform parent)
        {
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "OwnerHouseBeacon";
            UnityEngine.Object.DestroyImmediate(beacon.GetComponent<Collider>());
            beacon.transform.SetParent(parent, false);
            beacon.transform.position = new Vector3(c.x, topY + 30f, c.y);
            beacon.transform.localScale = new Vector3(1.2f, 30f, 1.2f);
            beacon.GetComponent<MeshRenderer>().sharedMaterial = mats.Beacon;
            beacon.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------ water

        static void BuildWater(MapData map, Terrain terrain, Materials mats, Transform parent)
        {
            var mb = new MeshBuilder();
            foreach (var w in map.water)
            {
                var ring = ToPoints(w.outline);
                if (ring.Count < 3) continue;
                float level = WaterLevel(ring, terrain);
                int v = mb.Count;
                foreach (var p in ring) mb.Vertex(new Vector3(p.x, level + 0.15f, p.y));
                foreach (var tri in EarClip(ring)) mb.Tri(v + tri.x, v + tri.z, v + tri.y);
            }
            foreach (var s in map.streams) AddRibbon(mb, Densify(ToPoints(s.points), 2f), 2f, 0.04f, terrain);
            mb.ToObject("Water", mats.Water, parent, false);
        }

        // The lidar surface is hydro-flattened: open water is one flat level, so the median ground height inside the
        // outline is the water level. The outline's own lowest point usually sits below it, on the bank or a ditch.
        static float WaterLevel(List<Vector2> ring, Terrain terrain)
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

        class MeshBuilder
        {
            readonly List<Vector3> verts = new List<Vector3>();
            readonly List<int> tris = new List<int>();
            public int Count => verts.Count;
            public int Vertex(Vector3 v) { verts.Add(v); return verts.Count - 1; }
            public void Tri(int a, int b, int c) { tris.Add(a); tris.Add(b); tris.Add(c); }

            public GameObject ToObject(string name, Material mat, Transform parent, bool collider)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.isStatic = true;
                if (verts.Count == 0) return go;
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(verts);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/Meshes/{name}.asset");
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
                return go;
            }
        }

        class Materials
        {
            public readonly Material Asphalt, ServiceAsphalt, Building, OwnerHouse, Beacon, Water, TerrainMaterial;
            public readonly TerrainLayer GrassLayer;

            public Materials()
            {
                AssetDatabase.DeleteAsset(GeneratedDir + "/Meshes"); // drop chunks left over from a larger previous map
                Directory.CreateDirectory(Path.Combine(ProjectRoot, GeneratedDir, "Meshes"));
                AssetDatabase.Refresh();
                Asphalt = Make("Asphalt", new Color(0.18f, 0.18f, 0.19f), 0.25f);
                ServiceAsphalt = Make("ServiceAsphalt", new Color(0.30f, 0.30f, 0.30f), 0.2f);
                Building = Make("Building", new Color(0.80f, 0.76f, 0.68f), 0.1f);
                OwnerHouse = Make("OwnerHouse", new Color(1.0f, 0.45f, 0.05f), 0.2f);
                Beacon = Make("Beacon", new Color(1.0f, 0.55f, 0.1f), 0f, new Color(2.5f, 1.2f, 0.2f));
                Water = Make("Water", new Color(0.16f, 0.32f, 0.50f), 0.85f);

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
