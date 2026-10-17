using System.Collections.Generic;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // 0.120 Part A (Dan: "There is no counter for your current stunt and multiplier"): the trick panel. Where tricks score as an event (Racer: a Stunt
    // Race, the Stunt chapter, the Stunt event; Chastain Rush: the Stunt event and Free Roam in and round the stunt park: ArcadeActivities.TrickBox)
    // a panel on the left of the view shows the trick as it happens: in the air the tricks turned so far ("BACKFLIP + ROLL"), the flight's
    // multiplier ("x2": one for each trick type) and the points on the line; then "LANDED +1,240" or "BAILED"; on the ground the combo going (its
    // chain multiplier, its points and the 5 s to chain the next trick). The total points stay on the Stunt event's own panel (ActivityHud). Hidden
    // while nothing is going. Chastain Rush, Round 36: ported as it is for one player (split-screen left behind), not drawn under a menu or the map.
    public sealed class TrickHud : MonoBehaviour
    {
        public const float ResultSeconds = 2.5f;
        public ArcadeActivities activities;
        GUIStyle style; Texture2D back;
        void OnDestroy() { if (back) Destroy(back); }

        // the panel's lines (rich text; none: nothing going)
        public static List<string> Lines(ArcadeActivities a)
        {
            var lines = new List<string>(); if (!a || !a.TrickBox) return lines;
            var live = a.Live;
            if (live != null)
            {
                lines.Add(live.names.Count > 0 ? $"<b>{live.Names}</b>" + (live.types > 1 ? $"   <color=#FFC747><b>{live.Multiplier}</b></color>" : "") : "<color=#BFD8E0>IN THE AIR</color>");
                lines.Add($"<color=#FFE58A>+{live.points:N0}</color> <size={{small}}>on the line</size>");
            }
            else if (Time.time - a.LastResultAt < ResultSeconds && a.LastResult != "")
                lines.Add((a.LastResultKind == 1 ? "<color=#8CFFA8>" : a.LastResultKind == 2 ? "<color=#FF7A66>" : "<color=#FFB45A>") + "<b>" + a.LastResult + "</b></color>");
            if (a.ComboLinks > 0)
                lines.Add($"COMBO <color=#FFC747><b>x{a.ComboLinks}</b></color>  ·  {a.ComboTotal:N0}" + (live == null && a.ComboLinks < ArcadeActivities.MaxLinks ? $"  ·  <b>{a.ComboLeft:0.0}</b> s" : ""));
            return lines;
        }
        // what the panel shows now, as plain text (checks)
        public static string Text(ArcadeActivities a) => System.Text.RegularExpressions.Regex.Replace(string.Join(" / ", Lines(a)).Replace("{small}", "1"), "<[^>]+>", "");

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || !activities || GameMenus.Open || WorldMap.IsOpen) return;
            if (Racer.GetawayChase.Current || Racer.SpeedPatrol.Current || Racer.PoliceChase.Current) return;   // Round 39: the police modes' heat and radio own the top of the screen
            var cam = SplitScreen.Session ? (activities.Player == 2 ? SplitScreen.Session.Cam2 : SplitScreen.Session.Cam1) : Camera.main;   // Round 37: its player's half
            if (!cam || !cam.isActiveAndEnabled) return;
            var lines = Lines(activities); if (lines.Count == 0) return;
            var view = cam.pixelRect;
            int size = Mathf.Max(15, Mathf.RoundToInt(view.height / 30f)), small = Mathf.Max(12, Mathf.RoundToInt(size * .7f));
            if (style == null) { style = new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.UpperLeft, wordWrap = false, clipping = TextClipping.Overflow }; back = new Texture2D(1, 1); back.SetPixel(0, 0, new Color(.02f, .045f, .06f, .78f)); back.Apply(); }
            style.fontSize = size;
            string text = string.Join("\n", lines).Replace("{small}", small.ToString());
            var c = new GUIContent(text); var dims = style.CalcSize(c); float pad = size * .45f;
            float top = Screen.height - view.yMax + view.height * .42f;
            var box = new Rect(view.x + Mathf.Max(14, view.width * .012f), top, dims.x + pad * 2, dims.y + pad * 1.4f);
            GUI.DrawTexture(box, back);
            var inner = new Rect(box.x + pad, box.y + pad * .7f, dims.x, dims.y);
            var colour = style.normal.textColor;
            style.normal.textColor = new Color(0, 0, 0, .85f); GUI.Label(new Rect(inner.x + 1.5f, inner.y + 1.5f, inner.width, inner.height), System.Text.RegularExpressions.Regex.Replace(text, "<color=[^>]+>|</color>", ""), style);
            style.normal.textColor = Color.white; GUI.Label(inner, c, style); style.normal.textColor = colour;
        }
    }
}
