using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 36: where Racer's HUD panel shows ArcadeActivities.Hud (the activity at hand, an attempt's clock, a result and the stunt
    // lines) and the Stunt event's panel (its clock, points and next medal), this game's HUD draws them here: the event's panel top centre, the
    // activity lines under it, a result's medal beside them (Racer 0.96 Part B's badge). IMGUI as the race HUD (RaceController); not drawn under a
    // menu or the map.
    public sealed class ActivityHud : MonoBehaviour
    {
        public ArcadeActivities activities;
        GUIStyle line, panelText; Texture2D back;
        void OnDestroy() { if (back) Destroy(back); }
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || !activities || GameMenus.Open || WorldMap.IsOpen) return;
            var race = RaceController.Instance; if (race && race.State != RaceController.Stage.Free) return;
            if (SplitScreen.Active) return;   // Round 37: SplitHud draws each half's
            float s = Mathf.Max(1f, Screen.height / 800f); var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s;
            if (line == null)
            {
                line = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.UpperCenter, richText = true, wordWrap = true, normal = { textColor = Color.white } };
                panelText = new GUIStyle(line) { fontSize = 28, fontStyle = FontStyle.Bold };
                back = new Texture2D(1, 1); back.SetPixel(0, 0, new Color(.02f, .045f, .06f, .72f)); back.Apply();
            }
            float y = 44;
            var ev = StuntEvent.Current;
            if (ev && ev.State != StuntEvent.Phase.Done)
            {
                string p = ev.Panel; var size = panelText.CalcHeight(new GUIContent(p), 620);
                var r = new Rect((w - 640) / 2, y, 640, size + 16); GUI.DrawTexture(r, back);
                Shadow(new Rect(r.x + 10, r.y + 8, 620, size), p, panelText); y = r.yMax + 8;
            }
            string hud = activities.Hud;
            if (!string.IsNullOrEmpty(hud))
            {
                float hgt = line.CalcHeight(new GUIContent(hud), 900);
                var r = new Rect((w - 920) / 2, y, 920, hgt + 10);
                Shadow(new Rect(r.x + 10, r.y + 5, 900, hgt), hud, line);
                if (activities.ResultShowing) GUI.DrawTexture(new Rect(r.x + 830, r.y - 6, 54, 54), MedalUi.Sprite(activities.LastMedal).texture);
            }
            GUI.matrix = old;
        }
        static void Shadow(Rect r, string text, GUIStyle st)
        {
            var c = st.normal.textColor; st.normal.textColor = new Color(0, 0, 0, .85f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), System.Text.RegularExpressions.Regex.Replace(text, "<color=[^>]+>|</color>", ""), st);
            st.normal.textColor = c; GUI.Label(r, text, st);
        }
    }
}
