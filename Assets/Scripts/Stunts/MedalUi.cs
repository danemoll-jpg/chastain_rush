using UnityEngine;
using UnityEngine.UI;

namespace Racer
{
    // 0.96 Part B (Chastain Rush, Round 36: copied, the campaign event helpers left out): medals as medals, everywhere. One small medal graphic (a coloured disc with a ribbon, gold / silver /
    // bronze, drawn here so there is no asset to ship; 128 px, so it stays sharp at 3840x2160 and at split-screen size) and
    // the few helpers every screen uses: a badge, and a row of three targets read highest first: [gold] 656 ft [silver]
    // 459 ft [bronze] 295 ft. Distances are whole feet and times tenths of a second on these displays.
    public static class MedalUi
    {
        public static readonly Color Gold = new(1f, .80f, .22f), Silver = new(.82f, .86f, .92f), Bronze = new(.86f, .52f, .24f);
        static readonly Sprite[] sprites = new Sprite[4];
        static Texture2D[] textures = new Texture2D[4];
        // 1 bronze, 2 silver, 3 gold; 0 = an empty ring ("no medal")
        public static Color Tint(int medal) => medal switch { 3 => Gold, 2 => Silver, 1 => Bronze, _ => new Color(.5f, .56f, .6f) };
        public static string Name(int medal) => new[] { "NO MEDAL", "BRONZE", "SILVER", "GOLD" }[Mathf.Clamp(medal, 0, 3)];
        public static string Word(int medal) => new[] { "no medal", "bronze", "silver", "gold" }[Mathf.Clamp(medal, 0, 3)];

        public static Sprite Sprite(int medal)
        {
            medal = Mathf.Clamp(medal, 0, 3);
            if (sprites[medal]) return sprites[medal];
            var tex = Draw(medal); textures[medal] = tex;
            return sprites[medal] = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100);
        }
        static float Seg(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return Vector2.Distance(p, a + ab * t); }
        static Texture2D Draw(int medal)
        {
            const int n = 128; var pixels = new Color32[n * n]; var baseColour = Tint(medal);
            var dark = baseColour * .62f; dark.a = 1; var light = Color.Lerp(baseColour, Color.white, .55f);
            var ribbonA = new Color(.78f, .12f, .14f); var ribbonB = new Color(.95f, .95f, .95f);
            var centre = new Vector2(64, 44); float r = 41;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                var p = new Vector2(x + .5f, y + .5f); var colour = new Color(0, 0, 0, 0);
                // ribbon: two slanted straps meeting above the disc, a pale stripe down each
                float left = Seg(p, new Vector2(40, 128), new Vector2(60, 72)), right = Seg(p, new Vector2(88, 128), new Vector2(68, 72));
                float strap = Mathf.Min(left, right);
                if (strap < 11.5f)
                {
                    float a = Mathf.Clamp01(11.5f - strap); var c = strap < 3.2f ? ribbonB : ribbonA;
                    if (strap > 9.5f) c *= .72f; c.a = a; colour = c;
                }
                float d = Vector2.Distance(p, centre);
                if (d < r + 1)
                {
                    float a = Mathf.Clamp01(r + .5f - d); Color c;
                    if (d > 33.5f) c = dark;                                            // rim
                    else
                    {
                        float shade = Mathf.Clamp01(Vector2.Dot((p - centre).normalized, new Vector2(-.6f, .8f)) * .5f + .5f);
                        c = Color.Lerp(baseColour * .88f, light, shade * (1 - d / 40f) * .9f);
                        if (d > 23.5f && d < 26f) c = Color.Lerp(c, dark, .55f);       // the stamped ring
                        if (medal > 0)
                        {
                            // a star in the middle (pale, then a darker edge)
                            float sa = Mathf.Atan2(p.y - centre.y, p.x - centre.x); float k = .5f + .5f * Mathf.Cos(5 * (sa - Mathf.PI / 2));
                            float starR = Mathf.Lerp(7f, 17f, Mathf.Pow(k, 1.4f));
                            if (d < starR) c = Color.Lerp(c, medal == 2 ? Color.white : Color.Lerp(light, Color.white, .6f), .85f);
                        }
                        else c = Color.Lerp(c, new Color(.1f, .14f, .17f), .65f);
                    }
                    c.a = 1; colour = Color.Lerp(colour, c, a) ; colour.a = Mathf.Max(colour.a, a);
                }
                pixels[y * n + x] = colour;
            }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "Medal " + Name(medal), wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixels32(pixels); tex.Apply(true); return tex;
        }

        // A medal as an Image (square, size px) under parent. The caller positions it.
        public static Image Badge(Transform parent, int medal, float size, string name = "Medal")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.sprite = Sprite(medal); image.preserveAspect = true; image.raycastTarget = false;
            if (medal == 0) image.color = new Color(1, 1, 1, .6f);
            ((RectTransform)go.transform).sizeDelta = new Vector2(size, size); return image;
        }

        // ---------- values, as the medal displays write them ----------
        public static string Feet(float metres) => Mathf.RoundToInt((float)DisplayUnits.Feet(metres)) + " ft";
        public static string Mph(float metresPerSecond) => Mathf.RoundToInt((float)DisplayUnits.Mph(metresPerSecond)) + " mph";
        public static string Time(double seconds) { int tenths = Mathf.RoundToInt((float)(seconds * 10)); return $"{tenths / 600}:{tenths / 10 % 60:00}.{tenths % 10}"; }
        // A row of icon + text pairs: [medal] text [medal] text ... Each pair's text is sized to fit. Under parent, with a
        // layout so it can sit in a vertical list (height = icon + 4). highlight = the medal (3/2/1) to draw bold, or 0.
        public static RectTransform Row(Transform parent, Font font, int[] medals, string[] texts, int fontSize, float icon, int highlight = 0, Color? textColour = null, string name = "Medal row", string lead = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement)); go.transform.SetParent(parent, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>(); layout.spacing = 6; layout.childAlignment = TextAnchor.MiddleLeft; layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(2, 2, 0, 0);
            go.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(icon, fontSize + 6) + 4;
            if (lead != null)
            {
                var l = new GameObject("Lead", typeof(RectTransform), typeof(Text)).GetComponent<Text>(); l.transform.SetParent(go.transform, false);
                l.font = font; l.fontSize = Mathf.Max(14, fontSize - 3); l.alignment = TextAnchor.MiddleLeft; l.raycastTarget = false; l.horizontalOverflow = HorizontalWrapMode.Overflow; l.verticalOverflow = VerticalWrapMode.Overflow; l.color = new Color(.62f, .78f, .86f); l.text = lead;
                var ll = l.gameObject.AddComponent<LayoutElement>(); ll.preferredWidth = Mathf.Ceil(l.preferredWidth) + 8; ll.minWidth = ll.preferredWidth;
            }
            for (int i = 0; i < medals.Length; i++)
            {
                var badge = Badge(go.transform, medals[i], icon); var le = badge.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = icon; le.minWidth = le.minHeight = icon;
                var t = new GameObject("Value", typeof(RectTransform), typeof(Text)).GetComponent<Text>(); t.transform.SetParent(go.transform, false);
                t.font = font; t.fontSize = fontSize; t.alignment = TextAnchor.MiddleLeft; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
                t.color = textColour ?? Color.white; t.text = texts[i]; if (highlight != 0 && medals[i] == highlight) t.fontStyle = FontStyle.Bold;
                var tl = t.gameObject.AddComponent<LayoutElement>(); tl.preferredWidth = Mathf.Ceil(t.preferredWidth) + 14; tl.minWidth = tl.preferredWidth;
            }
            return (RectTransform)go.transform;
        }
        public static RectTransform TargetsRow(Transform parent, Font font, string[] goldSilverBronze, int fontSize, float icon, int highlight = 0, Color? textColour = null, string lead = null)
            => Row(parent, font, new[] { 3, 2, 1 }, goldSilverBronze, fontSize, icon, highlight, textColour, "Medal targets", lead);
    }
}
