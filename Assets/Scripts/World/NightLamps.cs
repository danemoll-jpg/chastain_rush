using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 35 (Part C1): the street lamps and the lots' light poles lit at night (Racer lights its vehicles and its course
    // markings, not street lamps: this part is new). Every lamp is a breakable panel (FenceChunk, BreakSounds.Kind.Lamp) whose last
    // piece is its arm and head: there a warm glow is drawn (instanced, gone once the lamp is knocked down), and the nearest
    // NearLights lamps to the camera carry a real point light (a small pool moved from lamp to lamp), all at the night level
    // (VehicleLights.Level, which WeatherEffects sets from the look: 0 by day, about half at dusk and dawn, 1 at night).
    public sealed class NightLamps : MonoBehaviour
    {
        const int NearLights = 8; const float LightRange = 24f, LightIntensity = 30f;   // the heads stand 5.6 to 9 m up (inverse square)
        struct Head { public FenceChunk chunk; public int panel; public Vector3 at; }
        readonly List<Head> heads = new List<Head>();
        readonly List<Matrix4x4> drawn = new List<Matrix4x4>();
        // Round 61 (C): the heads by 128 m cell, so a frame looks only at the cells within the fog (it looked at every lamp on the map)
        const float Cell = 128f;
        readonly Dictionary<Vector2Int, List<int>> cells = new Dictionary<Vector2Int, List<int>>();
        readonly List<(float d, Vector3 at)> best = new List<(float d, Vector3 at)>();
        Mesh mesh; Material glow; Light[] pool; bool scanned; float nextPick;
        public int Lamps => heads.Count;
        public int LitNear { get; private set; }
        static readonly Color Warm = new Color(1f, .78f, .48f);

        void Scan()
        {
            scanned = true;
            foreach (var c in FindObjectsByType<FenceChunk>(FindObjectsSortMode.None))
            {
                if (c.sound != BreakSounds.Kind.Lamp || c.panels == null || c.pieces == null) continue;
                for (int i = 0; i < c.panels.Length; i++)
                {
                    var pn = c.panels[i]; if (pn.pieceCount <= 0) continue;
                    var head = c.pieces[pn.firstPiece + pn.pieceCount - 1].centre;   // the arm and its head
                    heads.Add(new Head { chunk = c, panel = i, at = c.transform.TransformPoint(head) + Vector3.down * .12f });
                    var at = heads[heads.Count - 1].at; var k = new Vector2Int(Mathf.FloorToInt(at.x / Cell), Mathf.FloorToInt(at.z / Cell));
                    if (!cells.TryGetValue(k, out var l)) cells[k] = l = new List<int>(); l.Add(heads.Count - 1);
                }
            }
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); mesh = s.GetComponent<MeshFilter>().sharedMesh; Destroy(s);
            glow = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Lamp glow (night)", enableInstancing = true };
            pool = new Light[NearLights];
            for (int i = 0; i < NearLights; i++)
            {
                var l = new GameObject("Street lamp light").AddComponent<Light>(); l.transform.SetParent(transform, false);
                l.type = LightType.Point; l.range = LightRange; l.color = Warm; l.shadows = LightShadows.None; l.enabled = false;
                pool[i] = l;
            }
        }

        static bool Down(Head h) => !h.chunk || h.chunk.broken != null && h.panel < h.chunk.broken.Length && h.chunk.broken[h.panel];

        void LateUpdate()
        {
            float level = Racer.VehicleLights.Level;
            if (level <= .02f) { if (pool != null) foreach (var l in pool) if (l) l.enabled = false; LitNear = 0; return; }
            if (!scanned) Scan();
            var cam = Camera.main; if (!cam || heads.Count == 0) return;
            var c = cam.transform.position;
            // the glows: every lamp standing, within the fog
            glow.SetColor("_BaseColor", Warm * (1f + 3f * level));
            drawn.Clear(); float farM = RenderSettings.fogEndDistance + 50f, far = farM * farM;
            var size = new Vector3(.42f, .16f, .42f);
            int cx0 = Mathf.FloorToInt((c.x - farM) / Cell), cx1 = Mathf.FloorToInt((c.x + farM) / Cell), cz0 = Mathf.FloorToInt((c.z - farM) / Cell), cz1 = Mathf.FloorToInt((c.z + farM) / Cell);
            for (int cx = cx0; cx <= cx1; cx++)
                for (int cz = cz0; cz <= cz1; cz++)
                {
                    if (!cells.TryGetValue(new Vector2Int(cx, cz), out var list)) continue;
                    foreach (int hi in list)
                    {
                        var h = heads[hi];
                        if (Down(h) || (h.at - c).sqrMagnitude > far) continue;
                        drawn.Add(Matrix4x4.TRS(h.at, Quaternion.identity, size));
                        if (drawn.Count == 1023) { Graphics.RenderMeshInstanced(new RenderParams(glow), mesh, 0, drawn); drawn.Clear(); }
                    }
                }
            if (drawn.Count > 0) Graphics.RenderMeshInstanced(new RenderParams(glow), mesh, 0, drawn);
            // the real lights: the nearest standing lamps, picked four times a second
            if (Time.unscaledTime >= nextPick)
            {
                nextPick = Time.unscaledTime + .25f;
                best.Clear();
                for (int cx = Mathf.FloorToInt((c.x - 120f) / Cell); cx <= Mathf.FloorToInt((c.x + 120f) / Cell); cx++)
                    for (int cz = Mathf.FloorToInt((c.z - 120f) / Cell); cz <= Mathf.FloorToInt((c.z + 120f) / Cell); cz++)
                    {
                        if (!cells.TryGetValue(new Vector2Int(cx, cz), out var list)) continue;
                        foreach (int hi in list)
                        {
                            var h = heads[hi];
                            if (Down(h)) continue;
                            float d = (h.at - c).sqrMagnitude; if (d > 120f * 120f) continue;
                            if (best.Count < NearLights) best.Add((d, h.at));
                            else { int w = 0; for (int k = 1; k < best.Count; k++) if (best[k].d > best[w].d) w = k; if (d < best[w].d) best[w] = (d, h.at); }
                        }
                    }
                LitNear = best.Count;
                for (int i = 0; i < NearLights; i++) { pool[i].enabled = i < best.Count; if (i < best.Count) pool[i].transform.position = best[i].at + Vector3.down * .3f; }
            }
            foreach (var l in pool) if (l.enabled) l.intensity = LightIntensity * level;
        }

        void OnDestroy() { if (glow) Destroy(glow); }
    }
}
