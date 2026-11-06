using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 93 (C2): each U-turn of the route (RaceUTurns): a row of barriers along the turn's outside, from 'reach' m before the apex to 'reach' m
    // after, at the course's paved edge plus SetBack (no block within LineClearM of the course's line: the row guides you round, never across it;
    // one box for each short straight run, so WallGlance turns a glancing car along it), and a U-turn board on the outside beyond the apex, turned
    // to face the drivers 70 m before it (Resources/RaceSigns/UTurnLeft|Right.png: Tools/handmade/make_uturn_sign.py). Nothing solid on the board.
    public sealed partial class RaceBarriers
    {
        public int UTurnBlocks, UTurnBoards;   // checks
        void BuildUTurns(RaceRoute route)
        {
            UTurnBlocks = UTurnBoards = 0;
            var roads = RoadLookup.Instance;
            float Edge(Vector3 p) => Mathf.Clamp(roads && roads.Nearest(p, out _, out float d, out float h) && d < h + .5f ? h : 4f, 3f, 9f) + SetBack;
            foreach (var t in RaceUTurns.Of(route))
            {
                var root = new GameObject($"U-turn at lap {t.s:0} m").transform; root.SetParent(transform, false);
                var run = new List<Vector3>(); Vector3 runDir = Vector3.zero; Vector3? last = null; int n = 0;
                for (float u = -t.reach; u <= t.reach; u += .5f)
                {
                    var p = route.PointAt(route.GateS + t.s + u, out var f); var right = Vector3.Cross(Vector3.up, f);
                    // Round 94 (D, Dan: "the U-turn ... widened or eased so a bike at race speed gets round without hitting the barrier"): the row stands
                    // the turn's room further out round the apex (full there, easing to none at the row's ends), so a bike running wide has run-off
                    // (attempt 2: on the way out of the turn the full room holds for half the reach, then eases: the bike ran wide on the exit)
                    float k = u <= 0f ? u / t.reach : Mathf.Max(0f, u - t.reach * .5f) / (t.reach * .5f);
                    float ease = Mathf.Cos(Mathf.Clamp(k, -1f, 1f) * Mathf.PI * .5f); ease *= ease;
                    var q = p + right * t.side * (Edge(p) + t.room * ease);
                    if (last.HasValue && new Vector2(q.x - last.Value.x, q.z - last.Value.z).magnitude < Block) continue;
                    var dir = last.HasValue ? new Vector3(q.x - last.Value.x, 0f, q.z - last.Value.z).normalized : new Vector3(f.x, 0f, f.z).normalized;
                    last = q;
                    var q2 = new Vector2(q.x, q.z); var d2 = new Vector2(dir.x, dir.z) * Block * .5f;
                    if (OnLine(route, q2 - d2, q2 + d2, q.x, q.z) < LineClearM) { if (run.Count > 0) Solid(root, run, Block, runDir); continue; }
                    // a new box every 4 blocks or where the row bends more than 12 degrees
                    if (run.Count > 0 && (run.Count >= 4 || Vector3.Angle(runDir, dir) > 12f)) Solid(root, run, Block, runDir);
                    if (run.Count == 0) runDir = dir;
                    float gy = RaceRoute.Surface(q.x, q.z).y; run.Add(new Vector3(q.x, gy, q.z));
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = "U-turn block"; cube.transform.SetParent(root, false);
                    cube.transform.SetPositionAndRotation(new Vector3(q.x, gy + Height * .5f - .5f, q.z), Quaternion.LookRotation(dir, Vector3.up));
                    cube.transform.localScale = new Vector3(Depth, Height + 1f, Block);
                    var rd = cube.GetComponent<Renderer>(); rd.sharedMaterial = n++ % 2 == 0 ? red : white; rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    Destroy(cube.GetComponent<Collider>());
                    UTurnBlocks++;
                }
                if (run.Count > 0) Solid(root, run, Block, runDir);
                // the board: beyond the apex on the outside, facing the drivers coming up to the turn
                {
                    var p = route.PointAt(route.GateS + t.s, out var f); var right = Vector3.Cross(Vector3.up, f);
                    var at = p + right * t.side * (Edge(p) + t.room + 4f); at.y = RaceRoute.Surface(at.x, at.z).y;
                    var from = route.PointAt(route.GateS + t.s - 70f, out _); var face = new Vector3(from.x - at.x, 0f, from.z - at.z).normalized;
                    var tex = Resources.Load<Texture2D>(t.side > 0f ? "RaceSigns/UTurnLeft" : "RaceSigns/UTurnRight");
                    var board = new GameObject("U-turn board").transform; board.SetParent(root, false);
                    board.SetPositionAndRotation(at, Quaternion.LookRotation(face, Vector3.up));   // +z toward the drivers: the face is on that side
                    const float W = 4.0f, H = 3.0f, Bottom = 1.6f;
                    var mesh = new Mesh { name = "U-turn board face" };
                    mesh.vertices = new[] { new Vector3(-W / 2, Bottom, .1f), new Vector3(W / 2, Bottom, .1f), new Vector3(-W / 2, Bottom + H, .1f), new Vector3(W / 2, Bottom + H, .1f) };
                    // seen from +z (the drivers), local +x is on their left: u runs from their left (u 0 at x +W/2) to their right
                    mesh.uv = new[] { new Vector2(1, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1) };
                    mesh.triangles = new[] { 0, 1, 2, 1, 3, 2 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    var fo = new GameObject("Face"); fo.transform.SetParent(board, false); fo.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mat = Unlit(Color.white); if (tex) { mat.SetTexture("_BaseMap", tex); mat.mainTexture = tex; }
                    var mr = fo.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    // the back panel and two posts (gray, nothing solid)
                    var gray = Unlit(new Color(.35f, .36f, .38f));
                    void Box(string nm, Vector3 c, Vector3 size) { var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = nm; b.transform.SetParent(board, false); b.transform.localPosition = c; b.transform.localScale = size; b.GetComponent<Renderer>().sharedMaterial = gray; Destroy(b.GetComponent<Collider>()); }
                    Box("Back", new Vector3(0f, Bottom + H / 2, 0f), new Vector3(W + .1f, H + .1f, .15f));
                    Box("Post", new Vector3(-W / 2 + .5f, (Bottom + .4f) / 2 - .4f, -.15f), new Vector3(.18f, Bottom + .4f, .18f));
                    Box("Post", new Vector3(W / 2 - .5f, (Bottom + .4f) / 2 - .4f, -.15f), new Vector3(.18f, Bottom + .4f, .18f));
                    UTurnBoards++;
                }
            }
        }
    }
}
