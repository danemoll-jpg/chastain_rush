using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 37 (Part B): Racer's SplitHud (0.90 Part D, 0.94 Parts A and B, 0.106 Part D, 0.108 Part B) for each half: the player's tag (bottom
    // left), the speed (bottom right); in a race the lap, place, lap and race time, penalties (top left), the next checkpoint's arrow and
    // distance (top centre), the countdown, GO, WRONG WAY-free messages of their own and FINISHED with the place (centre); in Free Roam the
    // day and clock, the direction and distance to the other player, their own jump / speed-trap / trick lines and, in the Stunt event, their
    // own clock, points and next medal. Sized for the half's shape. Racer's is uGUI on its RaceHud canvas; this game's race HUD is IMGUI
    // (RaceController), so this is IMGUI too, drawn into each camera's rectangle. The minimap (MiniMap) and the trick panel (TrickHud) have
    // one instance per player, placed in that player's half. The single-player HUD hides itself while split screen is on.
    public sealed class SplitHud : MonoBehaviour
    {
        public SplitSession session;
        GUIStyle small, big, huge, centre; Texture2D back, arrow;
        static readonly Color P1Tint = new Color(.3f, .95f, .81f), P2Tint = new Color(1, .74f, .25f);

        void Styles()
        {
            if (small != null) return;
            small = new GUIStyle(GUI.skin.label) { fontSize = 19, richText = true, normal = { textColor = Color.white }, wordWrap = false, clipping = TextClipping.Overflow };
            big = new GUIStyle(small) { fontSize = 28, fontStyle = FontStyle.Bold };
            huge = new GUIStyle(small) { fontSize = 110, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            centre = new GUIStyle(small) { fontSize = 28, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            back = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave }; back.SetPixel(0, 0, new Color(.025f, .055f, .07f, .78f)); back.Apply();
            arrow = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float u = (x - 31.5f) / 32f, v = y / 63f;
                    bool head = v > 0.45f && Mathf.Abs(u) < (1f - v) * 1.55f, stem = v <= 0.45f && v > 0.05f && Mathf.Abs(u) < 0.2f;
                    arrow.SetPixel(x, y, head || stem ? Color.white : Color.clear);
                }
            arrow.Apply();
        }
        void OnDestroy() { if (back) Destroy(back); if (arrow) Destroy(arrow); }

        // the divider between the halves (Racer's 4 px bar), under no menu
        void OnGUI()
        {
            if (!session || GameMenus.Open || WorldMap.IsOpen || Event.current.type != EventType.Repaint) return;
            Styles();
            var race = RaceController.Instance; bool racing = race && race.State != RaceController.Stage.Free && race.Route != null;
            if (racing && race.AllHumansFinished) return;   // the race's results panel (RaceController) has the screen
            for (int player = 1; player <= 2; player++)
            {
                var car = player == 1 ? session.P1 : session.P2; if (!car || player == 2 && SplitScreen.OneView) continue;
                var r = session.GuiRect(player);
                float s = Mathf.Clamp(Mathf.Min(r.height / 560f, r.width / 760f), .5f, 2.4f);
                var old = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(r.x, r.y, 0), Quaternion.identity, new Vector3(s, s, 1));
                float w = r.width / s, h = r.height / s;
                if (racing) Race(race, player, car, w, h); else Roam(player, car, w, h);
                Tag(player, car, w, h);
                GUI.matrix = old;
            }
            if (!SplitScreen.OneView)
            {
                bool lr = SplitScreen.LeftRight; float k = Mathf.Max(2, Screen.height / 360f);
                GUI.DrawTexture(lr ? new Rect(Screen.width * .5f - k, 0, k * 2, Screen.height) : new Rect(0, Screen.height * .5f - k, Screen.width, k * 2), Texture2D.blackTexture);
            }
        }

        void Tag(int player, ArcadeVehicle car, float w, float h)
        {
            if (!SplitScreen.OneView)
            {
                var t = new GUIStyle(small) { fontSize = 18, normal = { textColor = player == 1 ? P1Tint : P2Tint } };
                Shadow(new Rect(16, h - 34, 400, 26), $"<b>PLAYER {player}</b> · {SplitScreen.NameOf(player)}", t);
            }
            var box = new Rect(w - 164, h - 66, 150, 52); GUI.DrawTexture(box, back);
            Shadow(box, $"<b>{DisplayUnits.Mph(Mathf.Abs(car.ForwardSpeed)):0}</b> <size=16>mph</size>", new GUIStyle(big) { alignment = TextAnchor.MiddleCenter });
        }

        // ---------- a race: this player's own seat in it (RaceController's rules) ----------
        void Race(RaceController race, int player, ArcadeVehicle car, float w, float h)
        {
            var e = race.HumanEntrant(player); if (e == null) return;
            int laps = race.LapsInRace, place = race.Ordered().IndexOf(e) + 1, n = race.Entrants.Count;
            var panel = new Rect(14, 12, 300, 112 + (e.PenaltyTotal > 0 ? 22 : 0)); GUI.DrawTexture(panel, back);
            string lapT = e.Finished && e.LapTimes.Count > 0 ? RaceController.Fmt(e.LapTimes[e.LapTimes.Count - 1]) : e.ClockStarted ? RaceController.Fmt(race.LapClockOf(e)) : "--:--.---";
            string totT = e.ClockStarted ? RaceController.Fmt(e.Finished ? e.FinalTotal : race.TotalClockOf(e)) : "--:--.---";
            Shadow(new Rect(26, 16, 290, 34), e.Finished ? "<b>Finished</b>" : $"<b>LAP {Mathf.Min(e.CompletedLaps + 1, laps)}/{laps}</b>" + (n > 1 ? $"     <b>{place}</b>/{n}" : ""), big);
            Shadow(new Rect(26, 52, 290, 24), $"Lap    <b>{lapT}</b>", small);
            Shadow(new Rect(26, 76, 290, 24), $"Race  <b>{totT}</b>", small);
            if (e.PenaltyTotal > 0) Shadow(new Rect(26, 100, 290, 24), $"<color=#ffb040>Penalty  +{e.PenaltyTotal:0} s  ({e.MissedTotal} missed)</color>", small);
            // the next gate's arrow and distance (top centre)
            var t = race.TargetOf(e);
            if (t != null && !e.Finished)
            {
                Vector3 to = t.Position - car.transform.position; to.y = 0f; Vector3 fwd = car.transform.forward; fwd.y = 0f;
                float angle = Vector3.SignedAngle(fwd, to, Vector3.up); var c = new Vector2(w / 2f, 46f);
                var save = GUI.matrix; GUIUtility.RotateAroundPivot(angle, c);
                GUI.color = t.Id == 0 ? Color.white : new Color(0.45f, 1f, 0.3f); GUI.DrawTexture(new Rect(c.x - 22, c.y - 22, 44, 44), arrow); GUI.color = Color.white; GUI.matrix = save;
                string name = t.Id == 0 ? (e.ClockStarted ? "FINISH LINE" : "START LINE") : $"CHECKPOINT {t.Id} / {race.Route.Checkpoints.Count}";
                Shadow(new Rect(0, 72, w, 26), $"<b>{name}</b>   {to.magnitude:0} m", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            }
            // the centre: the countdown, GO, finished with the place, this player's own message
            string mid = ""; float size = 28;
            if (race.State == RaceController.Stage.Countdown) { mid = Mathf.CeilToInt(race.CountdownRemaining).ToString(); size = 96; }
            else if (race.GoShowing) { mid = "GO!"; size = 96; }
            else if (e.Finished) mid = $"<b>FINISHED   {Ordinal(race.Ordered().IndexOf(e) + 1)}</b>\n<size=22>{RaceController.Fmt(e.FinalTotal)}</size>" + (race.AllHumansFinished ? "" : "\n<size=18>waiting for the other player</size>");
            else if (e.Message.Length > 0 && Time.time < e.MessageUntil) mid = $"<b>{e.Message}</b>";
            if (mid != "") Shadow(new Rect(0, h * .3f, w, h * .3f), mid, new GUIStyle(centre) { fontSize = Mathf.RoundToInt(size) });
        }
        static string Ordinal(int n) => n + (n % 100 >= 11 && n % 100 <= 13 ? "th" : n % 10 == 1 ? "st" : n % 10 == 2 ? "nd" : n % 10 == 3 ? "rd" : "th");

        // ---------- Free Roam / the stunt park / the Stunt event (Racer's SplitHud.Roam) ----------
        void Roam(int player, ArcadeVehicle car, float w, float h)
        {
            var other = player == 1 ? session.P2 : session.P1;
            var cam = player == 1 ? session.Cam1 : session.Cam2;
            var ev = StuntEvent.Current; var acts = player == 1 ? ArcadeActivities.Instance : session.Activities2;
            var lines = new System.Collections.Generic.List<string>();
            var look = WorldLook.Current;
            var rider = ev ? ev.Of(player) : null;
            if (rider != null)
            {
                int sec = Mathf.CeilToInt(ev.State == StuntEvent.Phase.Starting ? ev.Limit : ev.Left);
                lines.Add($"<b>STUNT EVENT</b>   {sec / 60}:{sec % 60:00}   <b>{rider.points:N0}</b> pts");
                if (ev.Finishing(player)) lines.Add(rider.acts && rider.acts.ComboLinks > 0 ? "<color=#FFC747><b>COMBO OVERTIME</b></color>" : "<color=#FFC747><b>FINISH THE TRICK!</b></color>");
                int have = StuntProgress.MedalFor(rider.points); var tg = StuntProgress.Targets;
                lines.Add(Time.unscaledTime < rider.goalFlashUntil ? $"<b>{MedalUi.Name(rider.goalMedal)}!</b>" : have < 3 ? $"next: {MedalUi.Name(have + 1)} {tg[have]:N0}  ({tg[have] - rider.points:N0} to go)" : "<b>GOLD</b>");
                if (Time.unscaledTime < rider.lineUntil) lines.Add(rider.line.StartsWith("+") ? "<color=#9CFFB0>" + rider.line + "</color>" : "<color=#FFC747>" + rider.line + "</color>");
            }
            else lines.Add(SplitScreen.Mode == SplitScreen.Kind.StuntPark ? "<b>STUNT PARK</b>" : "<b>FREE ROAM</b>");
            if (look && look.Mode == "Free Roam" && rider == null) lines.Add($"Day {look.Day}   {look.Clock}");
            if (other && !SplitScreen.OneView) lines.Add(Toward(car.transform, cam ? cam.transform : car.transform, other.transform, SplitScreen.NameOf(3 - player)));
            float ph = 14 + 24 * lines.Count; var panel = new Rect(14, 12, 360, ph); GUI.DrawTexture(panel, back);
            for (int i = 0; i < lines.Count; i++) Shadow(new Rect(26, 18 + 24 * i, 360, 24), lines[i], small);
            // the centre: the event's start, GO and TIME, this player's own activity lines (jumps, speed traps, the open combo), a reset
            string mid = "";
            if (rider != null)
            {
                if (ev.State == StuntEvent.Phase.Starting) mid = "<size=26><b>STUNT EVENT</b></size>\n<size=19>" + StuntProgress.Minutes + " minutes · land on your wheels · chain tricks</size>";
                else if (ev.State == StuntEvent.Phase.Running && ev.Clock < 1.5f) mid = "<size=60><b>GO!</b></size>";
                else if (rider.closed && ev.State != StuntEvent.Phase.Done) mid = $"<size=26><b>WAITING FOR P{3 - player}</b></size>\n<size=21>Your final score: {rider.points:N0} pts</size>";
            }
            else if (Time.unscaledTime - session.StartedAt < 6) mid = "<size=20>Split-screen " + (SplitScreen.Mode == SplitScreen.Kind.StuntPark ? "stunt park" : "Free Roam") + ": jumps, speed traps and tricks score for each of you\n(timed activity attempts are for one player)</size>";
            string hud = acts ? acts.Hud : ""; if (hud.Contains("> ACTIVITIES") || hud.Contains("> STUNT EVENT")) hud = hud.Split('\n')[0];   // Racer: timed attempts are single-player only
            if (mid == "" && !string.IsNullOrEmpty(hud)) mid = "<size=20>" + hud + "</size>";
            if (mid != "") Shadow(new Rect(w * .1f, h * .14f, w * .8f, h * .3f), mid, new GUIStyle(centre) { fontSize = 22 });
            if (acts && acts.ResultShowing) GUI.DrawTexture(new Rect(w / 2 - 27, h * .14f + h * .3f, 54, 54), MedalUi.Sprite(acts.LastMedal).texture);
        }

        // Racer's SplitRoam.Toward: "→ Kyle  240 m", the direction (from this player's view) and distance to the other player
        public static string Toward(Transform from, Transform viewer, Transform to, string name)
        {
            if (!from || !to) return "";
            var d = to.position - from.position; d.y = 0; float dist = d.magnitude;
            var f = viewer ? Vector3.ProjectOnPlane(viewer.forward, Vector3.up).normalized : from.forward;
            float a = Vector3.SignedAngle(f, d, Vector3.up); string[] arrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };
            return $"{arrows[Mathf.RoundToInt(Mathf.Repeat(a, 360) / 45) % 8]}  {name}  {DisplayUnits.Distance(dist)}";
        }

        static void Shadow(Rect r, string text, GUIStyle st)
        {
            var c = st.normal.textColor; st.normal.textColor = new Color(0, 0, 0, .85f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), System.Text.RegularExpressions.Regex.Replace(text, "<color=[^>]+>|</color>", ""), st);
            st.normal.textColor = c; GUI.Label(r, text, st);
        }
    }
}
