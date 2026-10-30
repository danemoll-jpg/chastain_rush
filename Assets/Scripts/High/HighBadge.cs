using UnityEngine;

namespace ChastainRush
{
    // Round 84 (A1): Racer 1.2's HIGH badge on the HUD (Racer: RaceHud's highBadge, left of the speedometer), drawn here in IMGUI as this game's
    // HUDs are: an orange HIGH tag at the top of the screen while the session is in HIGH MODE (a HIGH race, or Free Roam with it on), not under a
    // menu, the map, Trailer Mode's clean screen or a replay. Split screen: each half's own tag (SplitHud draws the halves; this draws one per view).
    // Also a short notice line (Notify: "HIGH MODE is on", "No problem. HIGH MODE is on the main menu any time.").
    public sealed class HighBadge : MonoBehaviour
    {
        static HighBadge instance; static string note = ""; static float noteUntil;
        public static void Ensure() { if (!instance) instance = new GameObject("HIGH MODE badge").AddComponent<HighBadge>(); }
        public static void Notify(string text, float seconds) { Ensure(); note = text; noteUntil = Time.unscaledTime + seconds; }
        public static bool Shown { get; private set; }   // checks
        GUIStyle tag, line;
        void OnGUI()
        {
            Shown = false;
            if (GameMenus.Open || WorldMap.IsOpen || TrailerMode.HudHidden) return;
            tag ??= new GUIStyle(GUI.skin.box) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = HighMode.Colour, background = Texture2D.whiteTexture } };
            line ??= new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            float s = Mathf.Max(1f, Screen.height / 800f); var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s, h = Screen.height / s; var oldColor = GUI.backgroundColor;
            void Tag(float cx, float y) { GUI.backgroundColor = new Color(.18f, .09f, .02f, .88f); GUI.Box(new Rect(cx - 40, y, 80, 30), HighMode.Badge, tag); Shown = true; }
            if (SplitScreen.Active && SplitScreen.Session)
            {
                for (int p = 1; p <= 2; p++) if (HighMode.ForPlayer(p)) { var r = SplitScreen.Session.GuiRect(p); Tag((r.x + r.width * .5f) / s, r.y / s + 6); }
            }
            else if (HighMode.Now) Tag(w * .5f, 6);
            GUI.backgroundColor = oldColor;
            if (Time.unscaledTime < noteUntil && note != "")
            {
                var c = line.normal.textColor; line.normal.textColor = new Color(0, 0, 0, .85f); GUI.Label(new Rect(2, h * .3f + 2, w, 36), note, line);
                line.normal.textColor = c; GUI.Label(new Rect(0, h * .3f, w, 36), note, line);
            }
            GUI.matrix = old;
        }
    }
}
