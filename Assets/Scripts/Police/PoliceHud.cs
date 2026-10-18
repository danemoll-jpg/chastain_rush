using System.Collections.Generic;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): the police modes' HUD (Racer's HiddenPoliceHud, SplitHud's police lines and RaceHud's police panel, drawn in
    // IMGUI as this game's HUDs are): the heat big at the top with its alert at each rise and the radio line (the words of the script; with no clip
    // recorded yet the text alone), the chase panel (clock, cops on you, ESCAPE / SEEN, the helicopter, BUST), the centre messages (CLOCKED 58 in a
    // 45, BUSTED, YOU LOST THEM, GO); Speed Patrol's radar, limit, points and pull-over meter; Cop vs Runner's role, clock and bust meter. One view
    // on the full screen, or each half in split screen. Not drawn under a menu or the map. The cops, the helicopter and clocked speeders on the
    // minimap and the big map (WorldMap.Places).
    public sealed class PoliceHud : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (!FindAnyObjectByType<PoliceHud>()) new GameObject("Police HUD").AddComponent<PoliceHud>(); }
        GUIStyle big, mid, line, panel; Texture2D back;
        readonly List<WorldMap.Place> marks = new();
        void OnDestroy() { if (back) Destroy(back); foreach (var m in marks) WorldMap.Places.Remove(m); }
        void LateUpdate() => UpdateMarks();
        void UpdateMarks()
        {
            var want = new List<(Vector3 at, Color c, string label)>();
            var g = GetawayChase.Current; if (g) { foreach (var (p, sees) in g.CopMarks) want.Add((p, sees ? new Color(1f, .25f, .2f) : new Color(.35f, .55f, 1f), "")); if (g.HeliMark.HasValue) want.Add((g.HeliMark.Value, new Color(1f, .95f, .4f), "")); }
            var sp = SpeedPatrol.Current; if (sp) foreach (var p in sp.ClockedMarks) want.Add((p, new Color(1f, .3f, .2f), ""));
            var pc = PoliceChase.Current; if (pc && pc.State == PoliceChase.Phase.Running) { var cop = pc.CarOf(pc.Cop); if (cop) want.Add((cop.Body.position, new Color(.35f, .55f, 1f), "")); }
            while (marks.Count < want.Count) { var m = new WorldMap.Place { label = "", small = true }; marks.Add(m); WorldMap.Places.Add(m); }
            while (marks.Count > want.Count) { WorldMap.Places.Remove(marks[marks.Count - 1]); marks.RemoveAt(marks.Count - 1); }
            for (int i = 0; i < want.Count; i++) { marks[i].at = want[i].at; marks[i].color = want[i].c; }
        }
        void Styles()
        {
            if (big != null) return;
            big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter, richText = true, normal = { textColor = new Color(1f, .78f, .28f) } };
            mid = new GUIStyle(big) { fontSize = 26, normal = { textColor = new Color(1f, .45f, .3f) } };
            line = new GUIStyle(big) { fontSize = 21, fontStyle = FontStyle.Normal, wordWrap = true, normal = { textColor = new Color(.68f, .84f, 1f) } };
            panel = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.UpperLeft, richText = true, normal = { textColor = Color.white } };
            back = new Texture2D(1, 1); back.SetPixel(0, 0, new Color(.025f, .055f, .07f, .84f)); back.Apply();
        }
        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || GameMenus.Open || WorldMap.IsOpen || TrailerMode.HudHidden) return;
            if (!PoliceKit.FreeRoam) return;
            bool any = GetawayChase.Current || SpeedPatrol.Current || PoliceChase.Current || Time.unscaledTime - HiddenPolice.FlashAt < 2.6f || Time.unscaledTime - HiddenPolice.LineAt < 4f;
            if (!any) return;
            Styles();
            float s = Mathf.Max(1f, Screen.height / 800f); var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            if (SplitScreen.Active && SplitScreen.Session) { for (int p = 1; p <= 2; p++) { var r = SplitScreen.Session.GuiRect(p); Draw(new Rect(r.x / s, r.y / s, r.width / s, r.height / s), p, true); } }
            else Draw(new Rect(0, 0, Screen.width / s, Screen.height / s), 1, false);
            GUI.matrix = old;
        }
        void Draw(Rect v, int player, bool half)
        {
            float top = v.y + (half ? 8 : 30); float cx = v.x + v.width / 2;
            Label(new Rect(v.x, top, v.width, 50), "", big);
            string centre = "";
            if (Time.unscaledTime - HiddenPolice.FlashAt < 2.6f) centre = "<color=#FF8A3A>" + HiddenPolice.Flash + "</color>";
            var g = GetawayChase.Current;
            if (g && g.State != GetawayChase.Phase.Done)
            {
                var r = g.RunnerOf(player) ?? g.Runners.FirstOrDefault();
                if (Time.unscaledTime - g.BannerAt < 3f && r != null && r.Done) centre = r.caught ? "<color=#FF5A4A>BUSTED</color>" : "<color=#7FFFB0>YOU LOST THEM</color>";
                if (g.State == GetawayChase.Phase.Starting) centre = "<color=#FFFFFF>" + Mathf.Max(1, Mathf.CeilToInt(g.StartLeft)) + "</color>";
                Label(new Rect(v.x, top, v.width, 50), $"HEAT {g.Heat}   <color=#FFC747>{g.HeatBars}</color>", big); float y = top + 50;
                if (g.AlertShown) { Label(new Rect(v.x, y, v.width, 36), g.Alert, mid); y += 38; }
                if (g.Radio != "") { Label(new Rect(v.x + 20, y, v.width - 40, 60), "<color=#7FB2FF>RADIO</color>  " + g.Radio, line); }
                if (r != null)
                {
                    var lines = new List<string> { "<b>GETAWAY</b>   " + PoliceKit.Mss(g.Clock) + (g.Embedded ? "" : " / " + PoliceSetup.Minutes + ":00"), $"COPS  <b>{r.copsChasing}</b> on you   ·   {g.Cops.Count(c => !c.block)} out" };
                    if (r.underground) lines.Add("<color=#9CFFB0>UNDERGROUND</color>   they can't follow");
                    if (!r.seen) { int e = Mathf.RoundToInt(r.escape * 10); lines.Add("ESCAPE  <color=#7FFFB0>" + new string('■', e) + "</color><color=#5A6066>" + new string('■', 10 - e) + "</color>   <color=#9CFFB0>hidden</color>"); } else lines.Add("<color=#FF5A4A>SEEN</color>   get out of sight");
                    if (g.Heli) lines.Add("AIR  " + (r.heliSees ? "<color=#FF5A4A>helicopter has you</color>" : "<color=#FFC747>helicopter overhead</color>"));
                    int b = Mathf.RoundToInt(r.bust * 10); lines.Add("BUST  <color=#FF5A4A>" + new string('■', b) + "</color><color=#5A6066>" + new string('■', 10 - b) + "</color>");
                    Panel(new Rect(v.x + 14, v.y + (half ? 70 : 150), 340, 22 + 25 * lines.Count), lines);
                }
            }
            else if (Time.unscaledTime - HiddenPolice.LineAt < 4f) Label(new Rect(v.x + 20, top, v.width - 40, 60), "<color=#7FB2FF>RADIO</color>  " + HiddenPolice.Line, line);
            var sp = SpeedPatrol.Current;
            if (sp && sp.State != SpeedPatrol.Phase.Done)
            {
                var c = sp.CopOf(player);
                if (c != null)
                {
                    if (sp.State == SpeedPatrol.Phase.Starting) centre = "GET READY";
                    float left = Mathf.Max(0, sp.Limit - sp.Clock);
                    var lines = new List<string> { $"<b>SPEED PATROL</b>   {PoliceKit.Mss(left)} left{(sp.State == SpeedPatrol.Phase.Overtime ? "  (finish your chase)" : "")}", $"POINTS  <b>{c.points}</b>   ·   caught {c.catches}", c.limit > 0 ? $"LIMIT  <b>{c.limit}</b> mph" : "LIMIT  —   (off the road)",
                        c.radar >= 0 ? $"RADAR  {(c.radarOver ? "<color=#FF5A4A>" : "<color=#9CFFB0>")}{c.radar:0} mph</color> in a {c.radarLimit}" : "RADAR  —", "LIGHTS  " + (c.siren ? "<color=#FF5A4A>ON</color>" : "off") + "   (RB / H)" };
                    float m = sp.MeterFor(player); if (m > 0) { int b = Mathf.RoundToInt(m * 10); lines.Add("PULL OVER  <color=#7FB2FF>" + new string('■', b) + "</color><color=#5A6066>" + new string('■', 10 - b) + "</color>"); }
                    Panel(new Rect(v.x + 14, v.y + (half ? 40 : 150), 360, 22 + 25 * lines.Count), lines);
                    if (Time.unscaledTime < c.lineUntil) Label(new Rect(v.x + 20, top, v.width - 40, 60), "<color=#7FB2FF>RADIO</color>  " + c.line, line);
                }
            }
            var pc = PoliceChase.Current;
            if (pc && pc.State != PoliceChase.Phase.Done && (player == pc.Cop || player == pc.Runner))
            {
                string role = pc.Role(player); if (pc.State == PoliceChase.Phase.Starting) centre = role == "COP" ? "YOU'RE THE COP" : "RUN!";
                if (pc.State == PoliceChase.Phase.Caught) centre = "<color=#FF5A4A>CAUGHT</color>"; if (pc.State == PoliceChase.Phase.Away) centre = "<color=#7FFFB0>GOT AWAY</color>";
                int b = Mathf.RoundToInt(pc.Meter * 10);
                var lines = new List<string> { $"<b>{(role == "COP" ? "COP" : "RUNNER")}</b>   round {pc.Round + 1} of {pc.Rounds}", $"TIME  {PoliceKit.Mss(pc.Clock)} / {PoliceSetup.Minutes}:00", "BUST  <color=#FF5A4A>" + new string('■', b) + "</color><color=#5A6066>" + new string('■', 10 - b) + "</color>" };
                if (role == "COP") lines.Add("LIGHTS  " + (pc.Siren ? "<color=#FF5A4A>ON</color>" : "off") + "   (RB / H)   ·   get within 10 m, make them stop");
                Panel(new Rect(v.x + 14, v.y + (half ? 40 : 150), 380, 22 + 25 * lines.Count), lines);
            }
            if (centre != "") Label(new Rect(v.x, v.y + v.height * .3f, v.width, 70), centre, new GUIStyle(big) { fontSize = half ? 40 : 54, normal = { textColor = Color.white } });
        }
        void Panel(Rect r, List<string> lines) { GUI.DrawTexture(r, back); Label(new Rect(r.x + 10, r.y + 6, r.width - 16, r.height - 8), string.Join("\n", lines), panel); }
        static void Label(Rect r, string text, GUIStyle st)
        {
            if (string.IsNullOrEmpty(text)) return;
            var c = st.normal.textColor; st.normal.textColor = new Color(0, 0, 0, .85f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), System.Text.RegularExpressions.Regex.Replace(text, "<color=[^>]+>|</color>", ""), st);
            st.normal.textColor = c; GUI.Label(r, text, st);
        }
    }
}
