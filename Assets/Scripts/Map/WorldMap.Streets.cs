using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChastainRush
{
    // Round 75 (B, Dan, 2026-10-27 11:19: "when zoomed in, it would be great to get street names as well"): street names on the map screen, laid
    // along their roads past a zoom level: the interstates' shields first (I-75, I-575; US 41 beside its name), then the main roads, then the tertiary
    // ones, the side streets closest in. A name sits on a straight stretch of its road, turned along it and never upside down, apart from every
    // other label, and once per stretch of screen (a long road gets it again 360 px on). The names and lines come from the build
    // (Assets/Generated/Resources/MapStreets.json, MapBuilder.MapPicture.cs BakeMapStreets). Laid out again only when the view moves.
    public sealed partial class WorldMap
    {
        [System.Serializable] class StreetData { public string n, s; public int k; public float[] p; }
        [System.Serializable] class StreetsData { public StreetData[] streets; }
        sealed class Street { public string name, shield; public int rank; public Vector2[] p; public Rect box; public float length; }
        static List<Street> streets;
        RectTransform streetLayer;
        readonly List<Text> streetLabels = new List<Text>();
        readonly List<(Image box, Text text)> shieldLabels = new List<(Image, Text)>();
        Vector2 streetsCenter = new Vector2(-1, -1); float streetsZoom = -1f;
        // the zoom from which each rank is named (1 = the whole map, about 10 m a pixel; 16 = about 0.6 m a pixel)
        static readonly float[] RankZoom = { 1.6f, 2.6f, 4.5f, 6.5f };
        public int StreetLabelsShown { get; private set; }   // (the check's count)

        static void LoadStreets()
        {
            if (streets != null) return;
            streets = new List<Street>();
            var ta = Resources.Load<TextAsset>("MapStreets"); if (!ta) return;
            var d = JsonUtility.FromJson<StreetsData>(ta.text); if (d?.streets == null) return;
            foreach (var s in d.streets)
            {
                if (s.p == null || s.p.Length < 4) continue;
                var p = new Vector2[s.p.Length / 2]; for (int i = 0; i < p.Length; i++) p[i] = new Vector2(s.p[2 * i], s.p[2 * i + 1]);
                float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue, len = 0f;
                for (int i = 0; i < p.Length; i++) { x0 = Mathf.Min(x0, p[i].x); x1 = Mathf.Max(x1, p[i].x); z0 = Mathf.Min(z0, p[i].y); z1 = Mathf.Max(z1, p[i].y); if (i > 0) len += (p[i] - p[i - 1]).magnitude; }
                streets.Add(new Street { name = s.n, shield = s.s, rank = s.k, p = p, box = Rect.MinMaxRect(x0, z0, x1, z1), length = len });
            }
            // the longer roads of a rank first (they get their names in before a short piece of the same street)
            streets.Sort((a, b) => a.rank != b.rank ? a.rank.CompareTo(b.rank) : b.length.CompareTo(a.length));
        }

        void DrawStreets()
        {
            if (!picture) return;
            if (!streetLayer)
            {
                streetLayer = RectUI("Street names", picture.rectTransform, Vector2.zero, picture.rectTransform.rect.size);
                streetLayer.SetSiblingIndex(overlay.transform.GetSiblingIndex() + 1);   // over the lines, under the places and racers
            }
            if ((center - streetsCenter).sqrMagnitude < 1e-10f && Mathf.Abs(zoom - streetsZoom) < 1e-4f) return;
            streetsCenter = center; streetsZoom = zoom;
            LoadStreets();
            var size = picture.rectTransform.rect.size; float halfW = size.x / 2 - 6, halfH = size.y / 2 - 6;
            var placed = new List<Rect>(); var byName = new Dictionary<string, List<Vector2>>();
            int ni = 0, si = 0;
            // the view in world meters (with a margin: a road just outside can still carry a name inside)
            Vector3 lo = WorldPoint(center - Vector2.one * .5f / zoom), hi = WorldPoint(center + Vector2.one * .5f / zoom);
            var view = Rect.MinMaxRect(lo.x - 50, lo.z - 50, hi.x + 50, hi.z + 50);
            foreach (var st in streets)
            {
                if (zoom < RankZoom[Mathf.Clamp(st.rank, 0, 3)] || !st.box.Overlaps(view) || ni > 90) continue;
                var q = new Vector2[st.p.Length]; for (int i = 0; i < q.Length; i++) q[i] = ScreenPoint(new Vector3(st.p[i].x, 0f, st.p[i].y));
                var cum = new float[q.Length]; for (int i = 1; i < q.Length; i++) cum[i] = cum[i - 1] + (q[i] - q[i - 1]).magnitude;
                float total = cum[q.Length - 1];
                // the route shield: upright, on the road, every 420 px (an interstate shows its shield only; US 41 its name too)
                if (!string.IsNullOrEmpty(st.shield) && zoom >= RankZoom[0])
                    for (float s = 60f; s < total - 20f; s += 420f)
                    {
                        var at = Along(q, cum, s); if (Mathf.Abs(at.x) > halfW - 20 || Mathf.Abs(at.y) > halfH - 12) continue;
                        string key = "shield " + st.shield; if (Near(byName, key, at, 420f)) continue;
                        var box = new Rect(at.x - 24, at.y - 11, 48, 22); if (Hits(placed, box)) continue;
                        var sh = Shield(si++); bool inter = st.shield.StartsWith("I-");
                        sh.box.color = inter ? new Color(.13f, .3f, .72f, .95f) : new Color(.96f, .96f, .94f, .95f);
                        sh.text.text = st.shield; sh.text.color = inter ? Color.white : new Color(.08f, .08f, .08f);
                        sh.box.rectTransform.sizeDelta = new Vector2(sh.text.preferredWidth + 12f, 22f); sh.box.rectTransform.anchoredPosition = at; sh.box.gameObject.SetActive(true);
                        placed.Add(box); Remember(byName, key, at);
                    }
                if (string.IsNullOrEmpty(st.name)) continue;
                // the name: on a straight stretch as long as the text, turned along it
                var t = StreetText(ni); t.text = st.name; t.fontSize = st.rank <= 1 ? 14 : 13; t.fontStyle = st.rank <= 1 ? FontStyle.Bold : FontStyle.Normal;
                float lw = t.preferredWidth + 8f; if (lw > total) continue;
                bool any = false;
                for (float s = lw / 2; s <= total - lw / 2; s += any ? 360f : 24f)
                {
                    any = false;
                    var a = Along(q, cum, s - lw / 2); var b = Along(q, cum, s + lw / 2); var mid = Along(q, cum, s);
                    if ((b - a).magnitude < lw * .93f) continue;   // bent: the text would leave the road
                    if (Mathf.Abs(mid.x) > halfW - lw / 2 || Mathf.Abs(mid.y) > halfH - 10) continue;
                    if (Near(byName, st.name, mid, 360f)) continue;
                    float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg; if (ang > 90f) ang -= 180f; if (ang < -90f) ang += 180f;   // never upside down
                    float c = Mathf.Abs(Mathf.Cos(ang * Mathf.Deg2Rad)), sn = Mathf.Abs(Mathf.Sin(ang * Mathf.Deg2Rad));
                    var box = new Rect(mid.x - (lw * c + 16 * sn) / 2, mid.y - (lw * sn + 16 * c) / 2, lw * c + 16 * sn, lw * sn + 16 * c);
                    if (Hits(placed, box)) continue;
                    t = StreetText(ni++); t.text = st.name; t.fontSize = st.rank <= 1 ? 14 : 13; t.fontStyle = st.rank <= 1 ? FontStyle.Bold : FontStyle.Normal;
                    t.color = st.rank <= 1 ? new Color(1f, .97f, .86f) : new Color(.94f, .94f, .94f);
                    t.rectTransform.anchoredPosition = mid; t.rectTransform.localRotation = Quaternion.Euler(0, 0, ang); t.gameObject.SetActive(true);
                    placed.Add(box); Remember(byName, st.name, mid); any = true;
                    if (ni > 90) break;
                }
            }
            for (int i = ni; i < streetLabels.Count; i++) streetLabels[i].gameObject.SetActive(false);
            for (int i = si; i < shieldLabels.Count; i++) shieldLabels[i].box.gameObject.SetActive(false);
            StreetLabelsShown = ni + si;
        }
        static Vector2 Along(Vector2[] q, float[] cum, float s)
        {
            int i = 1; while (i < q.Length - 1 && cum[i] < s) i++;
            float seg = cum[i] - cum[i - 1]; return seg < 1e-4f ? q[i] : Vector2.Lerp(q[i - 1], q[i], Mathf.Clamp01((s - cum[i - 1]) / seg));
        }
        static bool Hits(List<Rect> placed, Rect r) { foreach (var p in placed) if (p.Overlaps(r)) return true; return false; }
        static bool Near(Dictionary<string, List<Vector2>> by, string key, Vector2 at, float d)
        { if (!by.TryGetValue(key, out var l)) return false; foreach (var p in l) if ((p - at).sqrMagnitude < d * d) return true; return false; }
        static void Remember(Dictionary<string, List<Vector2>> by, string key, Vector2 at) { if (!by.TryGetValue(key, out var l)) by[key] = l = new List<Vector2>(); l.Add(at); }
        Text StreetText(int i)
        {
            while (streetLabels.Count <= i) { var t = Label("Street " + streetLabels.Count, streetLayer, Vector2.zero, new Vector2(300, 18), 13); t.gameObject.SetActive(false); streetLabels.Add(t); }
            return streetLabels[i];
        }
        (Image box, Text text) Shield(int i)
        {
            while (shieldLabels.Count <= i)
            {
                var r = RectUI("Shield " + shieldLabels.Count, streetLayer, Vector2.zero, new Vector2(48, 22)); var img = r.gameObject.AddComponent<Image>(); img.raycastTarget = false;
                var o = r.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, .8f); o.effectDistance = new Vector2(1, -1);
                var t = Label("Text", r, Vector2.zero, new Vector2(80, 22), 13); t.fontStyle = FontStyle.Bold; Destroy(t.GetComponent<Outline>());   // (dark text on US 41's white)
                r.gameObject.SetActive(false); shieldLabels.Add((img, t));
            }
            return shieldLabels[i];
        }
    }
}
