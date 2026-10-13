using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Round 23 follow-ups, BUG-005 (Dan, in the creek channel at BF: "why would the water stop here? Should be flowing"): the creek's
// flat water used to end as a still sheet short of the mouth, with only a thin trickle in each barrel. With 'flow_in' the creek's
// own water ends where the apron begins (MapBuilder.Pipes.cs, TrimStreamsAtCut) and runs on from there as MOVING water: down over
// the apron, splitting to the two mouths and down both barrels, a ribbon as wide as half the creek each, with the trickle's sound
// and splashes (PipeStream). Look and sound only: it is never deep and never slows anyone (no ShallowWater over it).
// Also here: an outfall's lip (a network exit over water, 'outfall'): a concrete kicker in the cut's mouth that throws a vehicle
// leaving the pipe out over the pond.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static Material creekFlow;

        static void BuildFlowIn(TerrainGrid terrain, Transform parent, PipePlan p)
        {
            if (!creekFlow)
            {
                creekFlow = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "CreekFlow" };
                creekFlow.SetTexture("_BaseMap", pipeWater.GetTexture("_BaseMap")); creekFlow.SetColor("_BaseColor", new Color(0.36f, 0.52f, 0.62f)); creekFlow.SetFloat("_Smoothness", 0.92f);
                AssetDatabase.CreateAsset(creekFlow, GeneratedDir + "/CreekFlow.mat");
            }
            float Surface(Vector2 q, float lift)
            {
                float s = Vector2.Dot(q - p.A, p.U), lat = Vector2.Dot(q - p.A, p.Nl);
                if (s >= -0.8f && Mathf.Abs(lat) <= p.HalfAt(Mathf.Clamp(s, 0f, p.L)) - 0.05f) return p.FloorAt(Mathf.Clamp(s, 0f, p.L), lat) + 0.02f + lift;   // on the cut's floor, in a barrel
                return Ground(terrain, q.x, q.y) + lift;
            }
            float halfCreek = StreamHalf * 0.5f + 0.25f;
            int ribbons = 0; float total = 0f;
            foreach (float bl in p.Lats)
            {
                float side = Mathf.Sign(bl);
                // the line: from 2 m back up the creek, past the creek's end, to just in front of this barrel's mouth, down the barrel
                var start = p.FlowFrom - p.FlowDir * 2.5f + p.Nl * (side * halfCreek * 0.5f);
                var ctrl = new List<Vector2> { start, p.FlowFrom + p.Nl * (side * halfCreek * 0.5f), p.P(-4f, bl * 0.6f), p.P(-1f, bl), p.P(p.S1, bl), p.P(p.L - 0.3f, bl) };
                var line = Densify(ctrl, 0.5f);
                for (int it = 0; it < 3; it++)   // a light smoothing of the bends
                {
                    var sm = new List<Vector2>(line);
                    for (int i = 1; i + 1 < line.Count; i++) sm[i] = (line[i - 1] + line[i] * 2f + line[i + 1]) * 0.25f;
                    line = sm;
                }
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>(); var mid = new List<Vector3>();
                float run = 0f; int rows = 0;
                for (int i = 0; i < line.Count; i++)
                {
                    if (i > 0) run += Vector2.Distance(line[i], line[i - 1]);
                    var t = (line[Mathf.Min(i + 1, line.Count - 1)] - line[Mathf.Max(i - 1, 0)]).normalized; var n = new Vector2(-t.y, t.x);
                    float sAt = Vector2.Dot(line[i] - p.A, p.U);
                    float half = Mathf.Lerp(halfCreek * 0.55f, sAt > 0f ? (p.R < 2f ? 0.6f : 0.85f) : 1.1f, Mathf.Clamp01(run / 8f));
                    half *= 1f + 0.06f * Mathf.Sin(run * 0.9f);
                    // the surface: at the creek's own level where it starts (over the static water), then down the apron at 1 in 5 until it
                    // meets the ground and the floor, on which it lies (3 cm up) the rest of the way
                    float fromCreek = Mathf.Max(0f, Vector2.Dot(line[i] - p.FlowFrom, p.FlowDir));
                    foreach (float k in new[] { -1f, -0.5f, 0f, 0.5f, 1f })
                    {
                        var q = line[i] + n * (k * half);
                        float y = Mathf.Max(Surface(q, 0.03f + 0.01f * (1f - Mathf.Abs(k))), run < 2.6f ? p.FlowSurf + 0.01f : p.FlowSurf + 0.01f - (fromCreek) * 0.2f);
                        v.Add(new Vector3(q.x, y, q.y)); uv.Add(new Vector2(k * 0.5f + 0.5f, -run / 3f));
                    }
                    mid.Add(v[v.Count - 3] + Vector3.up * 0.02f);
                    if (rows > 0) for (int k = 0; k < 4; k++) { int a0 = (rows - 1) * 5 + k, b0 = rows * 5 + k; tr.Add(a0); tr.Add(b0); tr.Add(a0 + 1); tr.Add(a0 + 1); tr.Add(b0); tr.Add(b0 + 1); }
                    rows++;
                }
                { var n0 = Vector3.Cross(v[tr[1]] - v[tr[0]], v[tr[2]] - v[tr[0]]); if (n0.y < 0f) for (int i = 0; i < tr.Count; i += 3) { int x = tr[i + 1]; tr[i + 1] = tr[i + 2]; tr[i + 2] = x; } }
                string name = $"Pipe {p.J.id}{(p.J.twin ? (bl < 0f ? "R" : "L") : "")} creek flowing in";
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tr, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/Meshes/{name}.asset");
                var go = new GameObject(name); go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = creekFlow; mr.shadowCastingMode = ShadowCastingMode.Off;
                var ps = go.AddComponent<PipeStream>(); ps.line = mid.ToArray(); ps.water = creekFlow; ps.splash = pipeSplash;
                ribbons++; total += run;
            }
            Debug.Log($"[Round23+] pipe {p.J.id} (BUG-005): the creek's water runs on as moving water from ({p.FlowFrom.x:0.0}, {p.FlowFrom.y:0.0}) over the apron into its {ribbons} barrel(s), {total / Mathf.Max(1, ribbons):0} m each, to b");
        }

        // an outfall: a concrete kicker in the cut's mouth (the last 5.5 m before a), rising 1.2 m on a curve (about 24 degrees at its
        // lip): a vehicle leaving the network there is thrown out over the water
        static void BuildOutfallLip(Transform parent, PipePlan p, Materials mats)
        {
            const float From = 0.6f, To = 6.1f, Rise = 1.2f;
            var mb = new MeshBuilder(); var concrete = new Color(0.60f, 0.59f, 0.56f); float hw = p.Half - 0.15f;
            float H(float s) { float t = Mathf.Clamp01((To - s) / (To - From)); return Rise * t * t; }
            Vector3 W(float s, float lat, float y) { var q = p.P(s, lat); return new Vector3(q.x, y, q.y); }
            int steps = 11;
            for (int i = 0; i < steps; i++)
            {
                float s0 = Mathf.Lerp(To, From, i / (float)steps), s1 = Mathf.Lerp(To, From, (i + 1) / (float)steps);
                for (int j = 0; j < 8; j++)
                {
                    float l0 = Mathf.Lerp(-hw, hw, j / 8f), l1 = Mathf.Lerp(-hw, hw, (j + 1) / 8f);
                    mb.Tint = Shade(concrete, 0.9f + 0.08f * ((i + j) & 1));
                    Face(mb, W(s0, l0, p.FloorAt(s0, l0) + 0.02f + H(s0)), W(s1, l0, p.FloorAt(s1, l0) + 0.02f + H(s1)), W(s1, l1, p.FloorAt(s1, l1) + 0.02f + H(s1)), W(s0, l1, p.FloorAt(s0, l1) + 0.02f + H(s0)), Vector3.up);
                }
            }
            // its front, straight down at the lip (facing the water), and its two sides
            var u3 = new Vector3(p.U.x, 0f, p.U.y); var n3 = new Vector3(p.Nl.x, 0f, p.Nl.y);
            mb.Tint = Shade(concrete, 0.8f);
            Face(mb, W(From, -hw, p.Y(From) - 0.3f), W(From, hw, p.Y(From) - 0.3f), W(From, hw, p.Y(From) + 0.02f + Rise), W(From, -hw, p.Y(From) + 0.02f + Rise), -u3);
            foreach (float sd in new[] { -1f, 1f })
                for (int i = 0; i < steps; i++)
                {
                    float s0 = Mathf.Lerp(To, From, i / (float)steps), s1 = Mathf.Lerp(To, From, (i + 1) / (float)steps);
                    Face(mb, W(s0, sd * hw, p.Y(s0)), W(s1, sd * hw, p.Y(s1)), W(s1, sd * hw, p.Y(s1) + 0.02f + H(s1)), W(s0, sd * hw, p.Y(s0) + 0.02f + H(s0)), n3 * sd);
                }
            var go = mb.ToObject("sc Pipe " + p.J.id + " outfall lip", mats.Concrete, parent, false);
            AddCollider(go, mb, "Pipe " + p.J.id + " outfall lip collider");
            Debug.Log($"[Round23+] outfall {p.J.id}: a lip {Rise:0.0} m high over its last {To - From:0.0} m, its mouth floor at {p.Y(0f):0.00} m");
        }
    }
}
