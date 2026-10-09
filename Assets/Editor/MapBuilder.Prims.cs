using System;
using System.Collections.Generic;
using UnityEngine;

// Round 7: small low-poly building blocks shared by the houses, street furniture, pond and Dan's house. Everything
// is coloured per vertex (the same VertexLit material as the roofs and roads), so a house is one mesh.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // An oriented local frame: u runs along the facade (to the right as seen from the street, mirrored when M = -1),
        // v points out of the facade toward the street, y is height above O (the floor level).
        struct Frame
        {
            public Vector3 O, R, F; public float M;
            public Vector3 P(float u, float y, float v) => O + R * (u * M) + F * v + Vector3.up * y;
            public Vector3 Dir(float u, float y, float v) => R * (u * M) + F * v + Vector3.up * y;
        }

        static Frame MakeFrame(Vector3 origin, Vector2 front, float mirror)
        {
            var f = new Vector3(front.x, 0f, front.y).normalized;
            // observer standing in front, looking at the house (along -f): his right is cross(up, -f)
            var r = Vector3.Cross(Vector3.up, -f);
            return new Frame { O = origin, F = f, R = r, M = mirror };
        }

        static Color Shade(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), 1f);

        // axis-aligned (in the frame) box with all six faces
        static void BoxF(MeshBuilder mb, Frame f, float u0, float u1, float y0, float y1, float v0, float v1, Color c)
        {
            mb.Tint = c;
            Vector3 p(float u, float y, float v) => f.P(u, y, v);
            Face(mb, p(u0, y0, v1), p(u1, y0, v1), p(u1, y1, v1), p(u0, y1, v1), f.F);
            Face(mb, p(u0, y0, v0), p(u1, y0, v0), p(u1, y1, v0), p(u0, y1, v0), -f.F);
            Face(mb, p(u1, y0, v0), p(u1, y0, v1), p(u1, y1, v1), p(u1, y1, v0), f.Dir(1, 0, 0));
            Face(mb, p(u0, y0, v0), p(u0, y0, v1), p(u0, y1, v1), p(u0, y1, v0), f.Dir(-1, 0, 0));
            Face(mb, p(u0, y1, v0), p(u1, y1, v0), p(u1, y1, v1), p(u0, y1, v1), Vector3.up);
            Face(mb, p(u0, y0, v0), p(u1, y0, v0), p(u1, y0, v1), p(u0, y0, v1), Vector3.down);
        }

        // a box without the bottom (walls that stand on a foundation), used for the house bodies
        static void BoxNoBottom(MeshBuilder mb, Frame f, float u0, float u1, float y0, float y1, float v0, float v1, Color front, Color sides, Color back)
        {
            Vector3 p(float u, float y, float v) => f.P(u, y, v);
            mb.Tint = front; Face(mb, p(u0, y0, v1), p(u1, y0, v1), p(u1, y1, v1), p(u0, y1, v1), f.F);
            mb.Tint = back; Face(mb, p(u0, y0, v0), p(u1, y0, v0), p(u1, y1, v0), p(u0, y1, v0), -f.F);
            mb.Tint = sides; Face(mb, p(u1, y0, v0), p(u1, y0, v1), p(u1, y1, v1), p(u1, y1, v0), f.Dir(1, 0, 0));
            mb.Tint = sides; Face(mb, p(u0, y0, v0), p(u0, y0, v1), p(u0, y1, v1), p(u0, y1, v0), f.Dir(-1, 0, 0));
        }

        // a flat panel on the front plane (facing +v) at depth v
        static void FrontQuad(MeshBuilder mb, Frame f, float u0, float u1, float y0, float y1, float v, Color c)
        {
            mb.Tint = c;
            Face(mb, f.P(u0, y0, v), f.P(u1, y0, v), f.P(u1, y1, v), f.P(u0, y1, v), f.F);
        }

        // a flat panel on a side plane: u = u0 constant, spanning v0..v1; faces outward along sign (+1 or -1) of u
        static void SideQuad(MeshBuilder mb, Frame f, float u, float v0, float v1, float y0, float y1, float sign, Color c)
        {
            mb.Tint = c;
            Face(mb, f.P(u, y0, v0), f.P(u, y0, v1), f.P(u, y1, v1), f.P(u, y1, v0), f.Dir(sign, 0, 0));
        }

        // a pitched roof prism. The ridge runs along u (alongU) or along v. a0..a1 is the extent along the ridge, b0..b1
        // across it. Roof planes overhang by oh (and ohEnd past the gable ends); gable ends are closed with 'fill'.
        static void Gable(MeshBuilder mb, Frame f, float a0, float a1, float b0, float b1, bool alongU, float yE, float rise,
                          float oh, float ohEnd, Color roof, Color fill, bool closeA0, bool closeA1, Color? trim = null)
        {
            float bm = (b0 + b1) * 0.5f, hb = (b1 - b0) * 0.5f, slope = rise / Mathf.Max(hb, 0.1f);
            float yDrop = yE - slope * oh;
            Vector3 W(float a, float b, float y) => alongU ? f.P(a, y, b) : f.P(b, y, a);
            Vector3 outN(float sgnB) => alongU ? new Vector3(0, 0, 0) : Vector3.zero;
            Vector3 A0 = W(a0 - ohEnd, b0 - oh, yDrop), A1 = W(a1 + ohEnd, b0 - oh, yDrop), R1 = W(a1 + ohEnd, bm, yE + rise), R0 = W(a0 - ohEnd, bm, yE + rise);
            Vector3 B0 = W(a0 - ohEnd, b1 + oh, yDrop), B1 = W(a1 + ohEnd, b1 + oh, yDrop);
            mb.Tint = roof;
            Face(mb, A0, A1, R1, R0, Vector3.up);
            Face(mb, B0, B1, R1, R0, Vector3.up);
            // thin underside so the overhang is not see-through from low angles: skip (backface culled, seen only from below)
            mb.Tint = fill;
            if (closeA0) TriFace(mb, W(a0, b0, yE), W(a0, b1, yE), W(a0, bm, yE + rise), alongU ? f.Dir(-1, 0, 0) : -f.F);
            if (closeA1) TriFace(mb, W(a1, b0, yE), W(a1, b1, yE), W(a1, bm, yE + rise), alongU ? f.Dir(1, 0, 0) : f.F);
            if (trim.HasValue)
            {
                // fascia boards along the rakes of a closed end: thin white slabs
                mb.Tint = trim.Value;
                if (closeA0) Rake(mb, f, W(a0, b0, yE), W(a0, bm, yE + rise), W(a0, b1, yE), alongU ? f.Dir(-1, 0, 0) : -f.F);
                if (closeA1) Rake(mb, f, W(a1, b0, yE), W(a1, bm, yE + rise), W(a1, b1, yE), alongU ? f.Dir(1, 0, 0) : f.F);
            }
        }

        // two fascia boards (one per slope) along the edge of a gable end, standing a little proud of the wall
        static void Rake(MeshBuilder mb, Frame f, Vector3 e0, Vector3 apex, Vector3 e1, Vector3 outward)
        {
            outward = new Vector3(outward.x, 0f, outward.z).normalized;
            void Board(Vector3 a, Vector3 b)
            {
                Vector3 off = outward * 0.07f, up = Vector3.up * 0.26f;
                Vector3 a0 = a + off, b0 = b + off;
                // a slab: front face + top + underside
                Face(mb, a0, b0, b0 + up, a0 + up, outward);
                Face(mb, a0 + up, b0 + up, b0 + up - outward * 0.07f, a0 + up - outward * 0.07f, Vector3.up);
            }
            Board(e0, apex); Board(apex, e1);
        }

        // a pyramid-free hip is not needed: roofs here are gables (side gables, front gables, shed porches)

        // a tall thin box between two ground points (posts, poles), vertical
        static void Post(MeshBuilder mb, Vector3 bottom, float height, float w, Color c)
        {
            mb.Tint = c;
            float h = w * 0.5f;
            Vector3 a = bottom + new Vector3(-h, 0, -h), b = bottom + new Vector3(h, 0, -h), cc = bottom + new Vector3(h, 0, h), d = bottom + new Vector3(-h, 0, h);
            Vector3 up = Vector3.up * height;
            Face(mb, a, b, b + up, a + up, Vector3.back);
            Face(mb, d, cc, cc + up, d + up, Vector3.forward);
            Face(mb, b, cc, cc + up, b + up, Vector3.right);
            Face(mb, a, d, d + up, a + up, Vector3.left);
            Face(mb, a + up, b + up, cc + up, d + up, Vector3.up);
        }

        // a box between two arbitrary points with a square cross-section (rails, lamp arms, beams)
        static void Beam(MeshBuilder mb, Vector3 a, Vector3 b, float w, float h, Color c, Vector3 sideHint = default)
        {
            mb.Tint = c;
            Vector3 axis = (b - a); if (axis.sqrMagnitude < 1e-8f) return; axis.Normalize();
            Vector3 side = sideHint != default ? Vector3.ProjectOnPlane(sideHint, axis) : Vector3.Cross(Vector3.up, axis);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(Vector3.forward, axis);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
            side.Normalize();
            Vector3 up = Vector3.Cross(axis, side).normalized;
            Vector3 s = side * (w * 0.5f), u = up * (h * 0.5f);
            Vector3 p0 = a - s - u, p1 = a + s - u, p2 = a + s + u, p3 = a - s + u;
            Vector3 q0 = b - s - u, q1 = b + s - u, q2 = b + s + u, q3 = b - s + u;
            Face(mb, p0, p1, q1, q0, -up); Face(mb, p3, p2, q2, q3, up);
            Face(mb, p1, p2, q2, q1, side); Face(mb, p0, p3, q3, q0, -side);
            Face(mb, p0, p1, p2, p3, -axis); Face(mb, q0, q1, q2, q3, axis);
        }

        // low-poly sphere / dome (shrubs, crowns): rings of segments, upper half only if dome
        static void Blob(MeshBuilder mb, Vector3 centre, Vector3 radii, Color top, Color bottom, int seg = 8, int rings = 4, bool dome = false, float jitterSeed = 0f)
        {
            int start = mb.Count;
            int rEnd = dome ? rings / 2 : rings;
            for (int r = 0; r <= rEnd; r++)
            {
                float phi = Mathf.PI * r / rings;
                for (int s = 0; s < seg; s++)
                {
                    float th = Mathf.PI * 2f * s / seg + (r % 2) * (Mathf.PI / seg);
                    float j = 1f + 0.10f * (Mathf.PerlinNoise(jitterSeed + r * 1.7f, s * 1.3f + jitterSeed) - 0.5f);
                    var p = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th) * radii.x * j, Mathf.Cos(phi) * radii.y, Mathf.Sin(phi) * Mathf.Sin(th) * radii.z * j);
                    mb.Tint = Color.Lerp(bottom, top, Mathf.Clamp01(0.5f + p.y / radii.y * 0.5f));
                    mb.Vertex(centre + p);
                }
            }
            for (int r = 0; r < rEnd; r++)
                for (int s = 0; s < seg; s++)
                {
                    int a = start + r * seg + s, b = start + r * seg + (s + 1) % seg, d = start + (r + 1) * seg + s, e = start + (r + 1) * seg + (s + 1) % seg;
                    mb.Tri(a, b, d); mb.Tri(b, e, d);
                }
            // winding: outward normals face away from the centre
            // (the loop above gives outward faces for a sphere sampled with phi increasing downward and theta counter-clockwise)
            mb.Tint = Color.white;
        }

        // a short vertical cylinder / cone frustum
        static void Cyl(MeshBuilder mb, Vector3 bottom, float height, float r0, float r1, Color c, int seg = 8)
        {
            mb.Tint = c;
            int start = mb.Count;
            for (int s = 0; s < seg; s++)
            {
                float th = Mathf.PI * 2f * s / seg;
                mb.Vertex(bottom + new Vector3(Mathf.Cos(th) * r0, 0, Mathf.Sin(th) * r0));
                mb.Vertex(bottom + new Vector3(Mathf.Cos(th) * r1, height, Mathf.Sin(th) * r1));
            }
            for (int s = 0; s < seg; s++)
            {
                int i0 = start + s * 2, i1 = start + ((s + 1) % seg) * 2;
                mb.Tri(i0, i0 + 1, i1); mb.Tri(i1, i0 + 1, i1 + 1);
            }
            if (r1 > 0.001f)
            {
                int c0 = mb.Vertex(bottom + Vector3.up * height);
                for (int s = 0; s < seg; s++) { int i0 = start + s * 2 + 1, i1 = start + ((s + 1) % seg) * 2 + 1; mb.Tri(c0, i1, i0); }
            }
        }

        // hashing for stable variation
        static float Hash01(int seed, int salt) => (Mix(seed, salt) & 0xFFFFFF) / (float)0x1000000;
        static T PickOf<T>(T[] arr, int seed, int salt) => arr[(int)(Mix(seed, salt) % (uint)arr.Length)];
    }
}
