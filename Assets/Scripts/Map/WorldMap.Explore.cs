using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ChastainRush
{
    // Round 48: the parts of Racer's ExplorationMap that Round 20 left behind, on this game's map (Explore/Exploration.cs keeps their state):
    //   fog          unexplored land dimmed (a cell texture over the picture, its uvRect the picture's)
    //   egg areas    each area's regions outlined in its color, its name and count ("Chastain Lakes  3 / 9", a check mark when done), the
    //                list of areas with their directions at the side; D-pad up / down (Page Up / Page Down) steps through them, centered
    //   destinations the discovered places, LB / RB (Q / E) step through them; X (T): travel there, A to confirm, B to cancel (Free Roam only)
    //   waypoint     Y (F): set at the cursor or the chosen place, again on it to clear; Backspace / left stick click: clear
    // Round 49 (D): the pointer (WorldMap.cs) chooses: A / a click on a discovered place asks to travel there, on an egg area shows it; X travels to
    // the place under the pointer (else the chosen one); Y sets the waypoint where the pointer is (on a place: that place)
    public sealed partial class WorldMap
    {
        InputAction prevPlace, nextPlace, travelAction, waypointAction, clearWaypointAction, areaPrev, areaNext, confirmYes;
        RawImage fog; Texture2D fogTex; int fogVersion = -1; Text areaList, confirmText, waypointMark; GameObject confirm;
        readonly List<Text> placeLabels = new List<Text>(), areaLabels = new List<Text>();
        int selectedPlace = -1, selectedArea = -1, pendingTravel = -1;
        public bool Confirming => pendingTravel >= 0;
        public int SelectedPlace => selectedPlace;

        void BuildExploreControls()
        {
            InputAction B(string name, string keyboard, string controller) { var a = mapActions.AddAction(name, InputActionType.Button); a.AddBinding(keyboard); a.AddBinding(controller); return a; }
            prevPlace = B("Previous place", "<Keyboard>/q", "<Gamepad>/leftShoulder"); nextPlace = B("Next place", "<Keyboard>/e", "<Gamepad>/rightShoulder");
            travelAction = B("Travel", "<Keyboard>/t", "<Gamepad>/buttonWest"); waypointAction = B("Waypoint", "<Keyboard>/f", "<Gamepad>/buttonNorth");
            clearWaypointAction = B("Clear waypoint", "<Keyboard>/backspace", "<Gamepad>/leftStickPress");
            areaPrev = B("Previous egg area", "<Keyboard>/pageUp", "<Gamepad>/dpad/up"); areaNext = B("Next egg area", "<Keyboard>/pageDown", "<Gamepad>/dpad/down");
            confirmYes = B("Confirm", "<Keyboard>/enter", "<Gamepad>/buttonSouth");
        }

        void BuildExploreUI(RectTransform pic, float w, float h)
        {
            var fr = RectUI("Fog", pic, Vector2.zero, new Vector2(w, h)); fog = fr.gameObject.AddComponent<RawImage>(); fog.raycastTarget = false;
            fr.SetSiblingIndex(0);   // under the lines and the labels
            waypointMark = Label("Waypoint", pic, Vector2.zero, new Vector2(30, 30), 26); waypointMark.color = new Color(1f, .82f, .2f); waypointMark.text = "✚"; waypointMark.fontStyle = FontStyle.Bold;
            var lr = RectUI("Egg areas list back", pic, new Vector2(w / 2 - 128, -h / 2 + 150), new Vector2(250, 290)); var bg = lr.gameObject.AddComponent<Image>(); bg.color = new Color(.02f, .04f, .05f, .92f); bg.raycastTarget = false;
            areaList = Label("Egg areas list", lr, new Vector2(-117, 138), new Vector2(236, 280), 12, TextAnchor.UpperLeft); areaList.rectTransform.pivot = new Vector2(0, 1); areaList.supportRichText = true;
            areaList.horizontalOverflow = HorizontalWrapMode.Wrap; areaList.verticalOverflow = VerticalWrapMode.Truncate;
            confirm = RectUI("Confirm travel", panel.transform, Vector2.zero, new Vector2(4000, 4000)).gameObject; confirm.AddComponent<Image>().color = new Color(.02f, .04f, .06f, .93f);
            confirmText = Label("Travel question", confirm.transform, Vector2.zero, new Vector2(900, 160), 30); confirm.SetActive(false);
        }

        // true when the input was the exploration's (the map's own input then waits a frame)
        bool UpdateExplore()
        {
            var ex = Exploration.Instance;
            if (Confirming)
            {
                if (confirmYes.WasPressedThisFrame() || travelAction.WasPressedThisFrame()) { MenuInput.ConsumeThroughRelease(); ConfirmTravel(); }
                else if (backAction.WasPressedThisFrame()) { MenuInput.ConsumeThroughRelease(backAction); CancelTravel(); }
                return true;
            }
            if (!ex) return false;
            if (prevPlace.WasPressedThisFrame()) StepPlace(-1);
            else if (nextPlace.WasPressedThisFrame()) StepPlace(1);
            else if (travelAction.WasPressedThisFrame()) { int at = PlaceUnderPointer(); RequestTravel(at >= 0 ? at : selectedPlace); }
            else if (waypointAction.WasPressedThisFrame()) ToggleWaypoint();
            else if (clearWaypointAction.WasPressedThisFrame()) ex.ClearWaypoint();
            else if (areaNext.WasPressedThisFrame()) StepArea(1);
            else if (areaPrev.WasPressedThisFrame()) StepArea(-1);
            else return false;
            return true;
        }

        int[] KnownPlaces() { var ex = Exploration.Instance; return ex ? Enumerable.Range(0, ex.destinations.Length).Where(i => ex.Discovered(ex.destinations[i].id)).ToArray() : new int[0]; }
        public void StepPlace(int d)
        {
            var ex = Exploration.Instance; var known = KnownPlaces();
            if (known.Length == 0) { selectedPlace = -1; if (ex) ex.Message = "Discover places by driving to them."; return; }
            int at = System.Array.IndexOf(known, selectedPlace); selectedPlace = known[(at + d + known.Length) % known.Length];
            center = Normalized(ex.destinations[selectedPlace].position); zoom = Mathf.Max(zoom, 3f); ClampView(); PointAt(ex.destinations[selectedPlace].position);   // Round 49: the pointer on it
        }
        public void StepArea(int d)
        {
            var all = EggAreas.Of(EggHunt.Instance); if (all.Length == 0) return;
            SelectArea(selectedArea < 0 ? (d > 0 ? 0 : all.Length - 1) : (selectedArea + d + all.Length) % all.Length);
        }
        public void SelectArea(int i)
        {
            var all = EggAreas.Of(EggHunt.Instance); if (i < 0 || i >= all.Length || picture == null) return;
            selectedArea = i; selectedPlace = -1; var b = all[selectedArea].bounds; center = Normalized(b.center);
            var ext = Vector2.Scale(Normalized(b.max) - Normalized(b.min), picture.rectTransform.rect.size);
            zoom = Mathf.Clamp(Mathf.Min(picture.rectTransform.rect.width / Mathf.Max(1, ext.x), picture.rectTransform.rect.height / Mathf.Max(1, ext.y)) * .8f, 1f, 8f); ClampView(); PointAt(b.center);
        }
        // Round 49: where the pointer is (on a discovered place: that place); again on it clears it
        public void ToggleWaypoint()
        {
            var ex = Exploration.Instance; if (!ex) return; int at = PlaceUnderPointer(); var p = at >= 0 ? ex.destinations[at].position : PointerWorld;
            if (ex.Waypoint.HasValue && (ScreenPoint(ex.Waypoint.Value) - pointer).magnitude < 12f) ex.ClearWaypoint(); else ex.SetWaypoint(p);
        }

        // Round 49: what is under the pointer: a discovered place within 16 (picture units) of it, else an egg area whose outline holds it (Free Roam)
        public int PlaceUnderPointer()
        {
            var ex = Exploration.Instance; if (!ex || picture == null) return -1; int best = -1; float bd = 16f;
            for (int i = 0; i < ex.destinations.Length; i++)
            {
                if (!ex.Discovered(ex.destinations[i].id)) continue;
                float dd = (ScreenPoint(ex.destinations[i].position) - pointer).magnitude; if (dd < bd) { bd = dd; best = i; }
            }
            return best;
        }
        public int AreaUnderPointer()
        {
            var hunt = EggHunt.Instance; if (!hunt || (race != null && race.State != RaceController.Stage.Free)) return -1;
            var all = EggAreas.Of(hunt); var q = PointerWorld; var p2 = new Vector2(q.x, q.z);
            for (int i = 0; i < all.Length; i++) foreach (var r in all[i].regions) if (r.outline != null && r.outline.Length > 2 && Inside(p2, r.outline)) return i;
            return -1;
        }
        static bool Inside(Vector2 p, Vector3[] ring)
        {
            bool c = false;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
                if ((ring[i].z > p.y) != (ring[j].z > p.y) && p.x < (ring[j].x - ring[i].x) * (p.y - ring[i].z) / (ring[j].z - ring[i].z) + ring[i].x) c = !c;
            return c;
        }
        // A / Enter / a click: a place under the pointer asks to travel there (Free Roam), an egg area shows it
        public void ActAtPointer()
        {
            var ex = Exploration.Instance; int at = PlaceUnderPointer();
            if (at >= 0) { selectedPlace = at; RequestTravel(at); return; }
            int ai = AreaUnderPointer();
            if (ai >= 0) { SelectArea(ai); return; }
            if (ex) ex.Message = "Nothing to choose here: point at a discovered place or an egg area.";
        }
        string UnderPointerText()
        {
            var ex = Exploration.Instance; int at = PlaceUnderPointer();
            if (at >= 0) return $"   ·   {ex.destinations[at].title}  (A: travel)";
            int ai = AreaUnderPointer(); if (ai >= 0) return $"   ·   {EggAreas.Of(EggHunt.Instance)[ai].name}  (A: show)";
            return "";
        }
        public void RequestTravel(int index)
        {
            var ex = Exploration.Instance; if (!ex) return;
            if (index < 0 || index >= ex.destinations.Length) { ex.Message = KeyHints.Of("Point at a discovered place first (or LB / RB).", "Point at a discovered place first (or Q / E)."); return; }
            if (!Exploration.CanTravel) { ex.Message = "Travel is in Free Roam only."; return; }
            pendingTravel = index; confirmText.text = $"Travel to {ex.destinations[index].title}?\n\n<size=22>{KeyHints.Of("A: travel      B: cancel", "Enter: travel      Esc: cancel")}</size>"; confirmText.supportRichText = true;
            confirm.SetActive(true); confirm.transform.SetAsLastSibling(); MenuInput.ConsumeThroughRelease();
        }
        public bool ConfirmTravel()
        {
            var ex = Exploration.Instance; if (pendingTravel < 0 || !ex) return false; int i = pendingTravel; CancelTravel();
            if (!ex.Travel(ex.destinations[i])) return false;
            Close(); return true;
        }
        public void CancelTravel() { pendingTravel = -1; if (confirm) confirm.SetActive(false); }

        void DrawExplore(List<WorldMapOverlay.Path> lines, bool free)
        {
            var ex = Exploration.Instance; var hunt = EggHunt.Instance;
            // the fog: one texel per cell, dark where unexplored
            if (ex && ex.Columns > 0)
            {
                if (!fogTex || fogTex.width != ex.Columns || fogTex.height != ex.Rows) { fogTex = new Texture2D(ex.Columns, ex.Rows, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }; fogVersion = -1; }
                if (fogVersion != ex.Version)
                {
                    var px = new Color32[ex.Columns * ex.Rows]; var dark = new Color32(4, 8, 10, 165); var clear = new Color32(0, 0, 0, 0);
                    for (int y = 0; y < ex.Rows; y++) for (int x = 0; x < ex.Columns; x++) px[y * ex.Columns + x] = ex.CellVisited(x, y) ? clear : dark;
                    fogTex.SetPixels32(px); fogTex.Apply(false); fogVersion = ex.Version; fog.texture = fogTex;
                }
                // the fog grid covers whole cells from the world's corner: its uv is the picture's scaled to the cells' extent
                var gr = ex.WorldRect; float gw = ex.Columns * Exploration.Cell, gh = ex.Rows * Exploration.Cell;
                var uv = picture.uvRect; fog.uvRect = new Rect((world.xMin + uv.x * world.width - gr.xMin) / gw, (world.yMin + uv.y * world.height - gr.yMin) / gh, uv.width * world.width / gw, uv.height * world.height / gh);
            }
            // the egg areas: outlines, names and counts (Free Roam; a race shows its course)
            int ai = 0; var all = EggAreas.Of(hunt); var list = new System.Text.StringBuilder();
            if (hunt && free)
            {
                list.Append($"<b>GOOSE EGGS  {hunt.Found} / {hunt.Total}</b>   {KeyHints.Of("D-pad ↑ ↓", "PgUp / PgDn")}\n");
                for (int i = 0; i < all.Length; i++)
                {
                    var a = all[i]; bool done = a.Done(hunt); var tint = done ? new Color(.75f, .77f, .75f, .8f) : a.tint;
                    foreach (var r in a.regions)
                    {
                        lines.Add(new WorldMapOverlay.Path { points = r.outline, tint = new Color(tint.r, tint.g, tint.b, i == selectedArea ? 1f : .7f), closed = true, arrows = false, width = i == selectedArea ? 3.5f : 2f });
                        if (!r.named) continue;
                        var t = Pooled(areaLabels, ai++, "Area", 14); t.text = (done ? "✓ " : "") + a.name + "  " + a.Count(hunt); t.color = tint; t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter; t.rectTransform.pivot = new Vector2(.5f, .5f);
                        Marker(t, r.label);
                    }
                    list.Append(i == selectedArea ? "<color=#FFFFFF><b>▶ " : $"<color=#{ColorUtility.ToHtmlStringRGB(tint)}>   ").Append(a.name).Append("  ").Append(a.Count(hunt)).Append(done ? "  ✓" : "").Append(i == selectedArea ? "</b>" : "").Append("</color>\n");
                    if (i == selectedArea) list.Append("<color=#9FB7B4>").Append(a.direction).Append("</color>\n");
                }
                if (hunt.TiersReached.Count > 0) list.Append($"\n<color=#FFD24D>Tiers reached: {string.Join(", ", hunt.TiersReached.OrderBy(x => x))}</color>");
            }
            areaList.transform.parent.gameObject.SetActive(hunt && free); areaList.text = list.ToString(); areaList.transform.parent.SetAsLastSibling();   // over the region names
            for (int i = ai; i < areaLabels.Count; i++) areaLabels[i].gameObject.SetActive(false);
            // the discovered places (the chosen one ◆) and the waypoint
            int pi = 0, hover = PlaceUnderPointer();   // Round 49: the place under the pointer shown as the chosen one
            if (ex)
                for (int i = 0; i < ex.destinations.Length; i++)
                {
                    var d = ex.destinations[i]; if (!ex.Discovered(d.id)) continue; bool hi = i == selectedPlace || i == hover;
                    var t = Pooled(placeLabels, pi++, "Place", hi ? 15 : 13); t.text = (hi ? "◆ " : "● ") + d.title;
                    t.color = hi ? Color.white : new Color(.85f, .95f, 1f, .9f); t.fontStyle = hi ? FontStyle.Bold : FontStyle.Normal;
                    t.alignment = TextAnchor.MiddleLeft; t.rectTransform.pivot = new Vector2(0.04f, .5f);
                    if (Marker(t, d.position) && zoom < 2f && !hi) t.text = "●";
                }
            for (int i = pi; i < placeLabels.Count; i++) placeLabels[i].gameObject.SetActive(false);
            if (ex && ex.Waypoint.HasValue) { Marker(waypointMark, ex.Waypoint.Value); waypointMark.transform.SetAsLastSibling(); } else waypointMark.gameObject.SetActive(false);
        }

        string ExploreStatus()
        {
            var ex = Exploration.Instance; if (!ex) return "";
            string wp = ex.Waypoint.HasValue ? $"Waypoint {DisplayUnits.Distance(Vector3.ProjectOnPlane(ex.Waypoint.Value - (car ? car.Body.position : Vector3.zero), Vector3.up).magnitude)}" : "";
            string place = selectedPlace >= 0 ? ex.destinations[selectedPlace].title + (Exploration.CanTravel ? KeyHints.Of("  (X: travel)", "  (T: travel)") : "  (travel in Free Roam)") : $"{KnownPlaces().Length} places found";
            return $"{place}{(wp.Length > 0 ? "   ·   " + wp : "")}{(ex.Message.Length > 0 ? "   ·   " + ex.Message : "")}";
        }
    }
}
