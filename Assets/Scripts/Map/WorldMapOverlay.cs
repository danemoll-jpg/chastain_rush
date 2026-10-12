using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChastainRush
{
    // Round 20, item 2: the lines drawn over the map's picture (WorldMap.cs). From Racer's WorldMapCourseOverlay (a MaskableGraphic
    // whose mesh is the route as quads, a dark edge under a colored line, white arrows every 85 px in the direction of travel, a
    // short bar at each gate; points under 2 px apart merged; lines off the view skipped; dashed where underground). Chastain Rush:
    // fed this game's lines (the current route, the shortcuts and pipes layer, Round 20's proposals) instead of Racer's courses.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WorldMapOverlay : MaskableGraphic
    {
        public sealed class Path { public Vector3[] points; public Color tint; public bool closed, dashed, arrows = true; public float width = 2f; }
        public sealed class Gate { public Vector3 at, right; public Color tint; public bool start; }
        readonly List<Path> paths = new List<Path>(); readonly List<Gate> gates = new List<Gate>();
        Rect world; Vector2 center; float zoom = 1f;

        public void SetView(Rect worldRect, Vector2 at, float scale, List<Path> lines, List<Gate> gateBars)
        {
            world = worldRect; center = at; zoom = scale;
            paths.Clear(); paths.AddRange(lines); gates.Clear(); gates.AddRange(gateBars);
            SetVerticesDirty();
        }

        Vector2 Point(Vector3 p) { var uv = new Vector2((p.x - world.xMin) / world.width, (p.z - world.yMin) / world.height); return Vector2.Scale((uv - center) * zoom, rectTransform.rect.size); }
        static void Line(VertexHelper vh, Vector2 a, Vector2 b, Color tint, float width = 2)
        {
            var side = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f; int i = vh.currentVertCount;
            vh.AddVert(a - side, tint, Vector2.zero); vh.AddVert(a + side, tint, Vector2.zero); vh.AddVert(b + side, tint, Vector2.zero); vh.AddVert(b - side, tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
        void Route(VertexHelper vh, Path r)
        {
            var pts = r.points; if (pts == null || pts.Length < 2) return;
            float distance = 0, dash = 0; Vector3 from = pts[0]; var edge = rectTransform.rect.size * .5f + Vector2.one * 24; int count = pts.Length - (r.closed ? 0 : 1);
            for (int i = 0; i < count; i++)
            {
                var to = pts[(i + 1) % pts.Length]; var a = Point(from); var b = Point(to);
                if ((b - a).sqrMagnitude < 4 && i < count - 1) continue;
                if (Mathf.Abs(a.x) > edge.x && Mathf.Abs(b.x) > edge.x && Mathf.Sign(a.x) == Mathf.Sign(b.x) || Mathf.Abs(a.y) > edge.y && Mathf.Abs(b.y) > edge.y && Mathf.Sign(a.y) == Mathf.Sign(b.y)) { from = to; continue; }   // off the map view
                if (r.dashed) { dash += (b - a).magnitude; if (Mathf.FloorToInt(dash / 7) % 2 == 1) { from = to; continue; } }
                Line(vh, a, b, new Color(0, .06f, .07f, .9f), r.width + 3); Line(vh, a, b, r.tint, r.width); distance += (b - a).magnitude;
                if (r.arrows && distance > 85) { distance = 0; var f = (b - a).normalized; var side = new Vector2(-f.y, f.x); Line(vh, b - f * 8 + side * 4, b, Color.white); Line(vh, b - f * 8 - side * 4, b, Color.white); }
                from = to;
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach (var p in paths) Route(vh, p);
            foreach (var g in gates)
            {
                var p = Point(g.at); var r = new Vector2(g.right.x, g.right.z).normalized;
                if (g.start) { Line(vh, p - r * 10, p + r * 10, new Color(0, .06f, .07f, .9f), 9); Line(vh, p - r * 8, p + r * 8, Color.white, 5); }
                else { Line(vh, p - r * 7, p + r * 7, new Color(0, .06f, .07f, .9f), 7); Line(vh, p - r * 6, p + r * 6, g.tint, 4); }
            }
        }
    }
}
