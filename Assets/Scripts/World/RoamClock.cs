using UnityEngine;
using UnityEngine.UI;

namespace ChastainRush
{
    // Round 35 (Part C): the Free Roam day and clock, with the moon's phase, top left under the free-roam line (Racer's RaceHud
    // "Free Roam day and clock" panel, 0.79 Part A: its layout and colors, here on its own small canvas). Shown in Free Roam
    // when no menu or map is open.
    public sealed class RoamClock : MonoBehaviour
    {
        GameObject panel; Text day, time; Racer.MoonIcon moon;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (!FindAnyObjectByType<RoamClock>() && FindAnyObjectByType<Racer.ArcadeVehicle>()) new GameObject("Free Roam clock").AddComponent<RoamClock>(); }

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = 1;
            panel = new GameObject("Free Roam day and clock", typeof(RectTransform), typeof(Image)); var r = (RectTransform)panel.transform; r.SetParent(transform, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(14, -44); r.sizeDelta = new Vector2(150, 76);
            var image = panel.GetComponent<Image>(); image.color = new Color(.02f, .04f, .05f, .58f); image.raycastTarget = false;
            day = Label("Day", r, 19, new Color(.88f, .92f, .96f), new Vector2(12, -5), new Vector2(100, 24), false);
            time = Label("Time", r, 38, Color.white, new Vector2(10, -27), new Vector2(136, 46), true);
            moon = new GameObject("Moon phase", typeof(RectTransform)).AddComponent<Racer.MoonIcon>(); moon.transform.SetParent(r, false); moon.raycastTarget = false;
            moon.rectTransform.anchorMin = moon.rectTransform.anchorMax = moon.rectTransform.pivot = new Vector2(1, 1); moon.rectTransform.anchoredPosition = new Vector2(-10, -7); moon.rectTransform.sizeDelta = new Vector2(20, 20);
            panel.SetActive(false);
        }
        static Text Label(string name, RectTransform parent, int size, Color color, Vector2 at, Vector2 box, bool bold)
        {
            var t = new GameObject(name, typeof(RectTransform)).AddComponent<Text>(); t.rectTransform.SetParent(parent, false);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleLeft; t.raycastTarget = false;
            if (bold) t.fontStyle = FontStyle.Bold;
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = t.rectTransform.pivot = new Vector2(0, 1); t.rectTransform.anchoredPosition = at; t.rectTransform.sizeDelta = box;
            return t;
        }
        void LateUpdate()
        {
            var look = Racer.WorldLook.Current;
            bool show = look && look.Mode == "Free Roam" && !GameMenus.Open && !WorldMap.IsOpen;
            if (panel.activeSelf != show) panel.SetActive(show);
            if (show) { day.text = "Day " + look.Day; time.text = look.Clock; moon.Phase = look.MoonPhase; }
        }
    }
}
