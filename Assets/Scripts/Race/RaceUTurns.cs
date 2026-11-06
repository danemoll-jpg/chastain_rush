using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 93 (C2, Dan on Wings & Wheels: "since there is a uturn lets make it more obvious"): a route's U-turns (route "u_turns": the apex's map point
    // and how far either side of it the turn reaches). Race time only (the race layer): RaceBarriers.UTurns.cs stands a row of barriers along the
    // turn's outside and a U-turn board beyond the apex facing the drivers coming up to it; the minimap draws the turn in amber with an arrowhead.
    public static class RaceUTurns
    {
        // s: the apex's lap station; side +1: the turn's outside is on the right of the way of travel (a left U-turn)
        public struct Turn { public float s, side, reach, room; public Vector3 apex; }
        public static List<Turn> Of(RaceRoute r)
        {
            var list = new List<Turn>(); var u = r?.Data?.u_turns; if (u == null) return list;
            foreach (var t in u)
            {
                if (t.pos == null || t.pos.Length < 2) continue;
                float s = r.LapStationOf(new Vector3(t.pos[0], 0f, t.pos[1]));
                r.PointAt(r.GateS + s - 25f, out var f0); var p = r.PointAt(r.GateS + s, out var fm);
                bool right = Vector3.Cross(f0, fm).y > 0f;   // (turning right: the outside on the left)
                list.Add(new Turn { s = s, side = right ? -1f : 1f, apex = p, reach = t.reach > 0f ? t.reach : 30f, room = t.room });
            }
            return list;
        }
    }
}
