using Racer;
using UnityEngine;
using UnityEngine.UI;

namespace ChastainRush
{
    // Round 48: an egg pickup can not be missed (Racer 0.95's AcornBanner): a banner at the top center (under the activities' prompt) for about 4 s with its own chime,
    // "GOOSE EGG FOUND   23 / 100" and under it the area with its count ("Chastain Lakes   5 / 9"); its own canvas scaled from 1080p,
    // so it is the same size on a 4K screen; a dark backing reads day and night. Not over a menu, the map, in split screen or in Trailer
    // Mode. New here: a TIER (10, 25, 50, 75, 100 eggs) shows the banner longer, gold, "TIER 2 REACHED: 25 EGGS" with a fanfare.
    public sealed class EggBanner : MonoBehaviour
    {
        public const float Seconds = 4f, TierSeconds = 7f;
        RectTransform panel; Text title, area; Image back, accent; float until; bool tierShown; AudioSource source; AudioClip chime, fanfare;
        static EggBanner current;
        public static string Shown => current && current.panel && current.panel.gameObject.activeSelf ? current.title.text + " / " + current.area.text : "";
        public static string Last { get; private set; } = "";

        public static void Show(int found, int total, string areaName, string areaCount, int tier, int tierNumber)
        {
            if (!current) current = new GameObject("Egg banner").AddComponent<EggBanner>();
            current.Set(found, total, areaName, areaCount, tier, tierNumber);
        }

        void Build()
        {
            DontDestroyOnLoad(gameObject);
            var go = new GameObject("Egg banner canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>(); panel.SetParent(go.transform, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, 1); panel.anchoredPosition = new Vector2(0, -175); panel.sizeDelta = new Vector2(820, 124);   // under the activities' prompt at the top center
            back = panel.GetComponent<Image>(); back.raycastTarget = false;
            accent = new GameObject("Accent", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); accent.transform.SetParent(panel, false); accent.raycastTarget = false;
            var a = accent.rectTransform; a.anchorMin = new Vector2(0, 0); a.anchorMax = new Vector2(1, 0); a.pivot = new Vector2(.5f, 0); a.sizeDelta = new Vector2(0, 6);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text Line(string name, int size, float top, float height)
            {
                var t = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>(); t.transform.SetParent(panel, false);
                t.font = font; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Overflow;
                var r = t.rectTransform; r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(.5f, 1); r.anchoredPosition = new Vector2(0, -top); r.sizeDelta = new Vector2(-24, height);
                var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, .9f); o.effectDistance = new Vector2(1.6f, -1.6f); return t;
            }
            title = Line("Found", 46, 10, 58); title.fontStyle = FontStyle.Bold; area = Line("Area", 28, 70, 40);
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0; source.ignoreListenerPause = true;
            chime = Synth.Notes("Egg found", new[] { 1318.5f, 1760f, 2637f }, .11f, .42f);
            fanfare = Synth.Notes("Egg tier", new[] { 784f, 988f, 1175f, 1568f, 2093f }, .13f, .8f);
            panel.gameObject.SetActive(false);
        }

        void Set(int found, int total, string areaName, string areaCount, int tier, int tierNumber)
        {
            if (!panel) Build();
            tierShown = tier > 0;
            title.text = tierShown ? $"TIER {tierNumber} REACHED:  {tier} EGGS" : $"GOOSE EGG FOUND   {found} / {total}";
            area.text = tierShown ? $"Goose eggs {found} / {total}   ·   {(string.IsNullOrEmpty(areaName) ? "" : areaName + "  " + areaCount)}" : string.IsNullOrEmpty(areaName) ? "" : $"{areaName}   {areaCount}";
            Last = title.text + " / " + area.text;
            until = Time.unscaledTime + (tierShown ? TierSeconds : Seconds);
            if (source) source.PlayOneShot(tierShown ? fanfare : chime, .5f * SoundMix.Gain(SoundMix.Kind.Effects));
        }

        void LateUpdate()
        {
            if (!panel) return;
            bool on = Time.unscaledTime < until && !GameMenus.Open && !WorldMap.IsOpen && !SplitScreen.Active && !TrailerMode.HudHidden;
            if (panel.gameObject.activeSelf != on) panel.gameObject.SetActive(on); if (!on) return;
            float a = Mathf.Clamp01((until - Time.unscaledTime) / .4f);
            back.color = new Color(.02f, .04f, .05f, .86f * a); accent.color = tierShown ? new Color(1f, .82f, .25f, a) : new Color(.95f, .93f, .82f, a);
            title.color = tierShown ? new Color(1f, .84f, .35f, a) : new Color(1f, .97f, .88f, a); area.color = new Color(.93f, .96f, .9f, a);
        }
        void OnDestroy() { if (chime) Destroy(chime); if (fanfare) Destroy(fanfare); if (current == this) current = null; }
    }

    // short original tones (sine with soft harmonics and a quick decay); CC0, no audio assets (Racer's Synth, copied)
    public static class Synth
    {
        public static AudioClip Notes(string name, float[] hz, float each, float last)
        {
            const int rate = 44100; int n = Mathf.CeilToInt(rate * (each * (hz.Length - 1) + last)); var data = new float[n];
            for (int k = 0; k < hz.Length; k++)
            {
                int start = Mathf.RoundToInt(rate * each * k); float length = k == hz.Length - 1 ? last : each * 1.6f; int count = Mathf.Min(n - start, Mathf.CeilToInt(rate * length));
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)rate, env = Mathf.Min(1, t / .006f) * Mathf.Exp(-t * (k == hz.Length - 1 ? 5 : 9));
                    data[start + i] += (Mathf.Sin(2 * Mathf.PI * hz[k] * t) + .3f * Mathf.Sin(4 * Mathf.PI * hz[k] * t) + .12f * Mathf.Sin(6 * Mathf.PI * hz[k] * t)) * env * .42f;
                }
            }
            var clip = AudioClip.Create(name, n, 1, rate, false); clip.SetData(data, 0); return clip;
        }
    }
}
