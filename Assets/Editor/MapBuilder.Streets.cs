using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 7: concrete curbs, sidewalks with a grass strip, street lamps, hydrants, the white rail / wooden / picket / iron
// fences (breakable, see Scripts/Neighborhood/FenceChunk.cs) and the planted entrance island with its two signs.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // ------------------------------------------------------------ curbs, sidewalks, lamps, hydrants

        static void BuildStreets(HoodCtx c)
        {
            var hood = c.hood;
            var centres = hood.Houses.Select(h => h.footprintCentre).ToList();
            var lampBrown = new Color(0.27f, 0.22f, 0.18f); var lampHead = new Color(0.22f, 0.22f, 0.24f);
            int lampCount = 0, hydrantCount = 0;
            foreach (int ri in hood.Area.roads)
            {
                var r = c.map.roads[ri];
                var pts = Densify(ToPoints(r.points), 2f);
                int n = pts.Count; if (n < 2) continue;
                float hw = r.width * 0.5f;
                var nrm = new Vector2[n]; var arc = new float[n];
                for (int i = 0; i < n; i++)
                {
                    Vector2 dirIn = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                    Vector2 dirOut = i < n - 1 ? (pts[i + 1] - pts[i]).normalized : dirIn;
                    Vector2 t = (dirIn + dirOut).normalized; if (t.sqrMagnitude < 1e-6f) t = dirOut;
                    nrm[i] = new Vector2(-t.y, t.x);
                    if (i > 0) arc[i] = arc[i - 1] + Vector2.Distance(pts[i - 1], pts[i]);
                }
                foreach (float side in new[] { 1f, -1f })
                {
                    // --- curb: 18 cm wide, 12 cm proud of the road, just outside the pale edge strip; no collider
                    for (int i = 0; i + 1 < n; i++)
                    {
                        int j = i + 1;
                        Vector2 mid = (pts[i] + pts[j]) * 0.5f + (nrm[i] + nrm[j]).normalized * side * (hw + 0.55f);
                        if (c.roadIndex.Covers(mid, r, 1.3f) || c.OnDrive(mid, 0.3f)) continue;
                        var mb = c.Props.At(mid);
                        Vector3 Pt(int k, float off, float dy) { var p = pts[k] + nrm[k] * side * off; return new Vector3(p.x, Ground(c.terrain, p.x, p.y) + dy, p.y); }
                        mb.Tint = CurbConcrete;
                        Face(mb, Pt(i, hw + 0.45f, 0.20f), Pt(j, hw + 0.45f, 0.20f), Pt(j, hw + 0.63f, 0.20f), Pt(i, hw + 0.63f, 0.20f), Vector3.up);
                        mb.Tint = CurbFace;
                        Face(mb, Pt(i, hw + 0.45f, 0.08f), Pt(j, hw + 0.45f, 0.08f), Pt(j, hw + 0.45f, 0.20f), Pt(i, hw + 0.45f, 0.20f), new Vector3(-nrm[i].x * side, 0f, -nrm[i].y * side));
                        Face(mb, Pt(i, hw + 0.63f, -0.1f), Pt(j, hw + 0.63f, -0.1f), Pt(j, hw + 0.63f, 0.20f), Pt(i, hw + 0.63f, 0.20f), new Vector3(nrm[i].x * side, 0f, nrm[i].y * side));
                        c.CurbMetres += 2;
                    }
                    // --- sidewalk: 1.4 m wide, 1.25 m of grass between it and the curb; only along houses, never into the pond
                    var run = new List<Vector2>();
                    void Flush()
                    {
                        if (run.Count >= 2)
                        {
                            var w = new float[run.Count]; for (int k = 0; k < w.Length; k++) w[k] = 1.4f;
                            foreach (var piece in Runs(run, 96f))
                            {
                                var ww = new float[piece.Count]; for (int k = 0; k < ww.Length; k++) ww[k] = 1.4f;
                                c.Ground.Tint = ConcreteLight;
                                RibbonVar(c.Ground.At(piece[piece.Count / 2]), piece, ww, 0.035f, c.terrain);
                            }
                            c.WalkMetres += Mathf.RoundToInt(run.Count * 2f);
                        }
                        run.Clear();
                    }
                    for (int i = 0; i < n; i++)
                    {
                        Vector2 p = pts[i] + nrm[i] * side * (hw + 2.58f);
                        bool houseSide = false;
                        foreach (var hc in centres)
                        {
                            var d = hc - pts[i];
                            if (d.sqrMagnitude < 26f * 26f && Vector2.Dot(d, nrm[i] * side) > 2f) { houseSide = true; break; }
                        }
                        bool ok = houseSide && !c.roadIndex.Covers(p, r, 2.0f) && hood.DistToPond(p) > 1.5f && !Neighborhood.InRing(p, hood.ParkingRing);
                        if (ok) run.Add(p); else Flush();
                    }
                    Flush();
                    // --- lamps and hydrants in the grass strip, alternating sides along the street
                    float lampPeriod = 56f;
                    for (float s0 = 14f + 20f * Hash01(ri, 140) + (side > 0 ? 0f : lampPeriod * 0.5f); s0 < arc[n - 1] - 8f; s0 += lampPeriod)
                    {
                        int k = Array.FindIndex(arc, a => a >= s0); if (k < 1) continue;
                        Vector2 p = Vector2.Lerp(pts[k - 1], pts[k], Mathf.InverseLerp(arc[k - 1], arc[k], s0)) + nrm[k] * side * (hw + 1.3f);
                        if (c.roadIndex.Covers(p, r, 2.5f) || c.OnDrive(p, 1.2f) || hood.DistToPond(p) < 3f || Neighborhood.InRing(p, hood.ParkingRing)) continue;
                        if (hood.InCorridor(p, 1.5f)) { c.ShortcutCleared++; continue; }   // Round 8: no lamp on a shortcut's line
                        bool near = false; foreach (var hc in centres) if ((hc - p).sqrMagnitude < 28f * 28f) { near = true; break; }
                        if (!near) continue;
                        float gy = Ground(c.terrain, p.x, p.y);
                        var mb = c.Props.At(p);
                        Post(mb, new Vector3(p.x, gy - 0.05f, p.y), 5.7f, 0.14f, lampBrown);
                        Vector3 top = new Vector3(p.x, gy + 5.65f, p.y);
                        Vector2 toRoad = -nrm[k] * side;
                        Beam(mb, top, top + new Vector3(toRoad.x, 0.25f, toRoad.y) * 1.5f, 0.09f, 0.09f, lampBrown);
                        Beam(mb, top + new Vector3(toRoad.x, 0.2f, toRoad.y) * 1.0f, top + new Vector3(toRoad.x, 0.2f, toRoad.y) * 1.7f, 0.3f, 0.13f, lampHead);
                        hood.KeepOut(p, 0.8f);
                        lampCount++;
                    }
                    for (float s0 = 40f + 30f * Hash01(ri, 141) + (side > 0 ? 0f : 60f); s0 < arc[n - 1] - 10f; s0 += 130f)
                    {
                        int k = Array.FindIndex(arc, a => a >= s0); if (k < 1) continue;
                        Vector2 p = Vector2.Lerp(pts[k - 1], pts[k], Mathf.InverseLerp(arc[k - 1], arc[k], s0)) + nrm[k] * side * (hw + 1.1f);
                        if (c.roadIndex.Covers(p, r, 2.5f) || c.OnDrive(p, 1.5f) || hood.DistToPond(p) < 3f) continue;
                        if (hood.InCorridor(p, 1.5f)) { c.ShortcutCleared++; continue; }   // Round 8: no hydrant on a shortcut's line
                        bool near = false; foreach (var hc in centres) if ((hc - p).sqrMagnitude < 28f * 28f) { near = true; break; }
                        if (!near) continue;
                        Hydrant(c.Props.At(p), new Vector3(p.x, Ground(c.terrain, p.x, p.y), p.y), Hash01(ri, 142 + k));
                        hood.KeepOut(p, 0.8f);
                        hydrantCount++;
                    }
                }
            }
            c.Lamps = lampCount; c.Hydrants = hydrantCount;
        }

        static void Hydrant(MeshBuilder mb, Vector3 b, float h)
        {
            var body = h < 0.5f ? new Color(0.62f, 0.58f, 0.50f) : new Color(0.72f, 0.12f, 0.10f);
            var cap = Shade(body, 0.8f);
            Cyl(mb, b + Vector3.down * 0.05f, 0.62f, 0.14f, 0.12f, body, 8);
            Cyl(mb, b + Vector3.up * 0.55f, 0.1f, 0.15f, 0.15f, cap, 8);
            Cyl(mb, b + Vector3.up * 0.65f, 0.08f, 0.1f, 0.0f, cap, 8);
            Beam(mb, b + Vector3.up * 0.38f - Vector3.right * 0.2f, b + Vector3.up * 0.38f + Vector3.right * 0.2f, 0.09f, 0.09f, cap);
            Beam(mb, b + Vector3.up * 0.30f - Vector3.forward * 0.15f, b + Vector3.up * 0.30f + Vector3.forward * 0.15f, 0.12f, 0.12f, cap);
        }

        // ------------------------------------------------------------ fences

        static readonly Color[] WoodTones = { new Color(0.50f, 0.39f, 0.28f), new Color(0.58f, 0.48f, 0.36f), new Color(0.42f, 0.35f, 0.29f), new Color(0.62f, 0.52f, 0.40f) };

        sealed class FencePanelRec { public int first, count; public Vector3 centre, along; public float length, height; }

        static void BuildFences(HoodCtx c)
        {
            var cells = new Dictionary<Vector2Int, (MeshBuilder mb, List<FenceChunk.Piece> pieces, List<FenceChunk.Panel> panels, List<FencePanelRec> recs)>();
            var fRoot = new GameObject("Fences").transform; fRoot.SetParent(c.root, false);
            var mats = new Dictionary<FenceKind, Material>();
            int panelsBuilt = 0;
            foreach (var run in c.hood.Fences)
            {
                var kind = run.kind;
                float L = kind == FenceKind.Picket ? 2.0f : 2.4f;
                Color wood = Shade(PickOf(WoodTones, run.seed, 150), 1f);
                // walk the polyline in panels of length L, each between two posts
                var pts = run.pts;
                var arc = new List<float> { 0f };
                for (int i = 1; i < pts.Count; i++) arc.Add(arc[i - 1] + Vector2.Distance(pts[i - 1], pts[i]));
                float total = arc[arc.Count - 1];
                int count = Mathf.Max(1, Mathf.RoundToInt(total / L));
                float step = total / count;
                Vector2 At(float s)
                {
                    int k = arc.FindIndex(a => a >= s); if (k < 1) k = 1; if (k >= pts.Count) k = pts.Count - 1;
                    return Vector2.Lerp(pts[k - 1], pts[k], Mathf.InverseLerp(arc[k - 1], arc[k], s));
                }
                for (int i = 0; i < count; i++)
                {
                    Vector2 a = At(i * step), b = At((i + 1) * step), mid = (a + b) * 0.5f;
                    if (kind != FenceKind.Iron && (!c.hood.FenceOk(mid) || c.OnDrive(mid, 0.6f))) continue;
                    if (kind == FenceKind.Rail && c.roadIndex.Covers(mid, NoRoad, 2.5f)) continue;
                    float ga = Ground(c.terrain, a.x, a.y), gb = Ground(c.terrain, b.x, b.y);
                    var key = new Vector2Int(Mathf.FloorToInt(mid.x / 128f), Mathf.FloorToInt(mid.y / 128f));
                    var ck = ((int)kind) * 1000003 + key.x * 7919 + key.y;
                    if (!cells.TryGetValue(new Vector2Int(key.x * 4 + (int)kind, key.y), out var cell))
                        cells[new Vector2Int(key.x * 4 + (int)kind, key.y)] = cell = (new MeshBuilder(), new List<FenceChunk.Piece>(), new List<FenceChunk.Panel>(), new List<FencePanelRec>());
                    var rec = BuildFencePanel(cell.mb, cell.pieces, kind, new Vector3(a.x, ga, a.y), new Vector3(b.x, gb, b.y), wood, run.seed + i);
                    cell.panels.Add(new FenceChunk.Panel { firstPiece = rec.first, pieceCount = rec.count, centre = rec.centre });
                    cell.recs.Add(rec);
                    panelsBuilt++;
                }
                c.FenceRuns++;
            }
            foreach (var kv in cells)
            {
                var kind = (FenceKind)(kv.Key.x & 3);
                var cell = kv.Value;
                if (cell.panels.Count == 0) continue;
                var go = cell.mb.ToObject($"Fence_{kind}_{kv.Key.x >> 2}_{kv.Key.y}", c.mats.House, fRoot, false, kind == FenceKind.Privacy);
                GameObjectUtility.SetStaticEditorFlags(go, 0);  // the mesh is rewritten at runtime when a panel breaks
                var chunk = go.AddComponent<FenceChunk>();
                chunk.source = go.GetComponent<MeshFilter>().sharedMesh;
                chunk.material = c.mats.House;
                chunk.pieces = cell.pieces.ToArray();
                chunk.panels = cell.panels.ToArray();
                for (int i = 0; i < cell.recs.Count; i++)
                {
                    var rec = cell.recs[i];
                    var pg = new GameObject("p"); pg.transform.SetParent(go.transform, false);
                    pg.transform.SetPositionAndRotation(rec.centre, Quaternion.LookRotation(rec.along, Vector3.up));
                    var bc = pg.AddComponent<BoxCollider>(); bc.isTrigger = true;
                    bc.size = new Vector3(0.9f, rec.height, rec.length);
                    var fp = pg.AddComponent<FencePanel>(); fp.chunk = chunk; fp.index = i;
                }
            }
            c.FencePanels = panelsBuilt;
        }

        // one panel between two posts; the first post belongs to it. Returns its piece range.
        static FencePanelRec BuildFencePanel(MeshBuilder mb, List<FenceChunk.Piece> pieces, FenceKind kind, Vector3 a, Vector3 b, Color wood, int seed)
        {
            Vector3 d = b - a; float len = new Vector2(d.x, d.z).magnitude;
            Vector3 along = new Vector3(d.x, 0f, d.z).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, along);
            float lo = Mathf.Min(a.y, b.y) - 0.12f;
            var rec = new FencePanelRec { first = pieces.Count, length = len };
            void Piece(Action build)
            {
                int v0 = mb.Count;
                build();
                int cnt = mb.Count - v0;
                // the piece's centre: the average of its vertices
                Vector3 centre = Vector3.zero;
                var verts = ((MeshBuilder)mb).VertexRange(v0, cnt);
                foreach (var v in verts) centre += v;
                centre /= Mathf.Max(cnt, 1);
                pieces.Add(new FenceChunk.Piece { vertexStart = v0, vertexCount = cnt, centre = centre });
            }
            Vector3 P(float s, float y) => new Vector3(a.x + along.x * s, y, a.z + along.z * s);
            switch (kind)
            {
                case FenceKind.Privacy:
                {
                    float H = 1.85f;
                    for (int k = 0; k < 3; k++)
                    {
                        float y0 = lo + k * (H / 3f), y1 = y0 + H / 3f - 0.015f;
                        var col = Shade(wood, 0.92f + 0.14f * Hash01(seed, 160 + k));
                        Piece(() => Beam(mb, P(0.1f, (y0 + y1) / 2), P(len - 0.02f, (y0 + y1) / 2), 0.05f, y1 - y0, col, side));
                    }
                    Piece(() => Post(mb, P(0f, lo - 0.1f), H + 0.2f, 0.13f, Shade(wood, 0.82f)));
                    rec.height = H; rec.count = 4;
                    break;
                }
                case FenceKind.Picket:
                {
                    float H = 1.05f;
                    var col = Shade(wood, 1.12f);
                    Piece(() =>
                    {
                        int n = Mathf.Max(6, Mathf.RoundToInt(len / 0.17f));
                        for (int k = 0; k < n; k++)
                        {
                            float s = 0.12f + (len - 0.14f) * (k + 0.5f) / n;
                            Beam(mb, P(s, lo), P(s, lo + H - (k % 2 == 0 ? 0f : 0.0f)), 0.085f, 0.03f, Shade(col, 0.96f + 0.08f * Hash01(seed, 170 + k)), side);
                        }
                        Beam(mb, P(0.05f, lo + 0.25f), P(len, lo + 0.25f), 0.03f, 0.07f, Shade(col, 0.8f), side);
                        Beam(mb, P(0.05f, lo + 0.75f), P(len, lo + 0.75f), 0.03f, 0.07f, Shade(col, 0.8f), side);
                    });
                    Piece(() => Post(mb, P(0f, lo - 0.1f), H + 0.1f, 0.1f, Shade(wood, 0.85f)));
                    rec.height = H; rec.count = 2;
                    break;
                }
                case FenceKind.Rail:
                {
                    var white = new Color(0.94f, 0.94f, 0.92f);
                    for (int k = 0; k < 3; k++)
                    {
                        float y = lo + 0.42f + k * 0.40f;
                        Piece(() => Beam(mb, P(0.05f, y), P(len, y), 0.05f, 0.12f, white, side));
                    }
                    Piece(() => Post(mb, P(0f, lo - 0.1f), 1.45f, 0.14f, white));
                    rec.height = 1.35f; rec.count = 4;
                    break;
                }
                default: // Iron: slim black pickets, a top and bottom rail, a white post
                {
                    var black = new Color(0.07f, 0.07f, 0.08f); float H = 1.5f;
                    Piece(() =>
                    {
                        int n = Mathf.Max(8, Mathf.RoundToInt(len / 0.13f));
                        for (int k = 0; k < n; k++) { float s = 0.14f + (len - 0.16f) * (k + 0.5f) / n; Beam(mb, P(s, lo), P(s, lo + H), 0.022f, 0.022f, black, side); }
                    });
                    Piece(() =>
                    {
                        Beam(mb, P(0.05f, lo + 0.18f), P(len, lo + 0.18f), 0.03f, 0.05f, black, side);
                        Beam(mb, P(0.05f, lo + H - 0.12f), P(len, lo + H - 0.12f), 0.03f, 0.05f, black, side);
                    });
                    Piece(() => Post(mb, P(0f, lo - 0.1f), H + 0.15f, 0.14f, new Color(0.93f, 0.93f, 0.91f)));
                    rec.height = H; rec.count = 3;
                    break;
                }
            }
            rec.centre = P(len * 0.5f, lo + rec.height * 0.5f);
            rec.along = along;
            // panel centres are stored in the object's local space by the chunk (the chunk is at the origin)
            return rec;
        }
    }
}
