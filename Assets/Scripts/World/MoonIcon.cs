using UnityEngine;

namespace Racer
{
    // 0.79 Part A: a small moon-phase disc beside the Free Roam day (0 new, 0.25 first quarter, 0.5 full, 0.75 last quarter):
    // the dark disc, and the lit part bounded by the terminator (an ellipse across the disc).
    // Chastain Rush, Round 35 (Part C): Racer's MoonIcon, copied unchanged (RoamClock shows it).
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MoonIcon : UnityEngine.UI.MaskableGraphic
    {
        float phase = .5f;
        public float Phase { get => phase; set { value = Mathf.Repeat(value, 1); if (Mathf.Abs(value - phase) < .002f) return; phase = value; SetVerticesDirty(); } }
        static readonly Color Dark = new(.16f, .18f, .22f, .9f), Lit = new(.95f, .93f, .82f, 1);
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; var c = r.center; float radius = Mathf.Min(r.width, r.height) * .5f; const int n = 24;
            Band(vh, c, radius, n, y => -1, y => 1, Dark);
            float k = Mathf.Cos(phase * Mathf.PI * 2); bool waxing = phase < .5f;
            // waxing: lit from the terminator to the right edge; waning: from the left edge to the terminator
            if (waxing) Band(vh, c, radius * .94f, n, y => k, y => 1, Lit); else Band(vh, c, radius * .94f, n, y => -1, y => -k, Lit);
        }
        // horizontal strips of the disc between x = left(y) and x = right(y), in units of the half-chord at height y
        static void Band(UnityEngine.UI.VertexHelper vh, Vector2 c, float radius, int n, System.Func<float, float> left, System.Func<float, float> right, Color col)
        {
            for (int i = 0; i < n; i++)
            {
                float y0 = -1 + 2f * i / n, y1 = -1 + 2f * (i + 1) / n; float h0 = Mathf.Sqrt(1 - y0 * y0), h1 = Mathf.Sqrt(1 - y1 * y1);
                var a = c + new Vector2(left(y0) * h0, y0) * radius; var b = c + new Vector2(right(y0) * h0, y0) * radius;
                var d = c + new Vector2(right(y1) * h1, y1) * radius; var e = c + new Vector2(left(y1) * h1, y1) * radius;
                int v = vh.currentVertCount; vh.AddVert(a, col, Vector2.zero); vh.AddVert(b, col, Vector2.zero); vh.AddVert(d, col, Vector2.zero); vh.AddVert(e, col, Vector2.zero);
                vh.AddTriangle(v, v + 1, v + 2); vh.AddTriangle(v, v + 2, v + 3);
            }
        }
    }
}
