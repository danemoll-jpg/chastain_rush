using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 61 (C, the map extension stage 2: "Make those distance-based ... trees and buildings beyond view distance not drawn"): the
    // terrain tiles past every camera's far clip are not drawn (Terrain.drawHeightmap off; their colliders stay, so AI, traffic and the
    // checks still drive on them). Unity looks at every tile every frame (TerrainManager.CullAllTerrains): with the map's 300 tiles that
    // grew with the map, while at most a dozen are ever in view (the fog is solid at 650 m). An orthographic camera (a top-down picture)
    // sees every tile. Made on its own when a scene with terrain loads; world, not game layer.
    public sealed class FarTerrain : MonoBehaviour
    {
        const float Margin = 120f;   // every frame (a few hundred rectangle tests): a fast travel never shows a missing tile
        Terrain[] tiles = new Terrain[0];
        Rect[] rects;
        public static int Drawn { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<FarTerrain>() || Terrain.activeTerrains.Length == 0) return;
            new GameObject("Far terrain").AddComponent<FarTerrain>();
        }

        void Start() => Collect();

        void Collect()
        {
            tiles = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            rects = new Rect[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                var p = tiles[i].transform.position; var s = tiles[i].terrainData.size;
                rects[i] = new Rect(p.x, p.z, s.x, s.z);
            }
        }

        static Camera[] cams = new Camera[8];
        static readonly List<(Vector2 at, float r)> views = new List<(Vector2, float)>();
        void Update()
        {
            views.Clear(); bool all = false;
            if (cams.Length < Camera.allCamerasCount) cams = new Camera[Camera.allCamerasCount + 4];
            int n = Camera.GetAllCameras(cams);
            for (int k = 0; k < n; k++)
            {
                var cam = cams[k];
                if (!cam || !cam.isActiveAndEnabled) continue;
                if (cam.orthographic) { all = true; break; }
                var p = cam.transform.position; views.Add((new Vector2(p.x, p.z), cam.farClipPlane + Margin));
            }
            if (views.Count == 0) all = true;
            int drawn = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (!tiles[i]) { Collect(); return; }
                bool on = all;
                if (!on)
                    foreach (var (at, r) in views)
                    {
                        var c = rects[i]; float dx = Mathf.Max(0f, Mathf.Max(c.xMin - at.x, at.x - c.xMax)), dz = Mathf.Max(0f, Mathf.Max(c.yMin - at.y, at.y - c.yMax));
                        if (dx * dx + dz * dz < r * r) { on = true; break; }
                    }
                if (tiles[i].drawHeightmap != on) tiles[i].drawHeightmap = on;
                // Round 63 (the view from the air): a tile more than 2 km from every camera is drawn coarser (pixel error 8, not the build's 3)
                if (on && !all) { float nearest = float.MaxValue; foreach (var (at, _) in views) { var c = rects[i]; float dx = Mathf.Max(0f, Mathf.Max(c.xMin - at.x, at.x - c.xMax)), dz = Mathf.Max(0f, Mathf.Max(c.yMin - at.y, at.y - c.yMax)); nearest = Mathf.Min(nearest, dx * dx + dz * dz); }
                    float err = nearest > 2000f * 2000f ? 8f : 3f; if (tiles[i].heightmapPixelError != err) tiles[i].heightmapPixelError = err; }
                if (on) drawn++;
            }
            Drawn = drawn;
        }
    }
}
