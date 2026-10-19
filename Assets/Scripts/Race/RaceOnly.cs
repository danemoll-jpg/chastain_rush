using UnityEngine;

namespace ChastainRush
{
    // Round 20 follow-up (R1, Dan: "it has to has some risk"): things that stand only while a race runs in one direction. The west
    // lobe's back yards are fenced off so that the reverse lap's only way through is R1; three of those fences stand in shortcut #6's
    // corridor (forward races: its way out between 713 and 1052, and beside house 1641 that it jumps), so they are here only during a
    // reverse race. Free roam and forward races leave #6's corridor as it was. Its children are shown or hidden as a whole.
    public sealed class RaceOnly : MonoBehaviour
    {
        public bool reverse = true;   // the direction it stands in
        // Round 25 (Dan, BUG-001 at R1's side-yard fences: "can we only make these unbreakable in the race? Let them all be breakable in
        // free roam"): 'any' stands in a race in either direction; 'freeRoam' stands only outside races (the breakable copy)
        public bool any, freeRoam;
        // Round 40 (A4, BUG-004: "This reads as a shortcut when it isnt going this direction"): 'hideInReverseOf' stands everywhere except in
        // a reverse race of that route (S1's and S2's turn-off clues on the Chastain Ridge)
        public string hideInReverseOf;
        // Round 47 (A): 'showInReverseOf' stands only in a reverse race of that route (the Chastain Lakes drain's grate)
        public string showInReverseOf;
        // Round 49: the same for a forward race of that route (the reverse Chastain Lakes shortcuts' clues and the intake's grate)
        public string hideInForwardOf, showInForwardOf;
        bool shown = true;

        void Start() => Apply(!string.IsNullOrEmpty(hideInReverseOf) || !string.IsNullOrEmpty(hideInForwardOf));

        public static bool ReverseRaceOf(string route)
        {
            var race = RaceController.Instance;
            return race != null && race.State != RaceController.Stage.Free && race.Route != null && race.Route.Reverse && race.Route.Name == route;
        }
        public static bool ForwardRaceOf(string route)
        {
            var race = RaceController.Instance;
            return race != null && race.State != RaceController.Stage.Free && race.Route != null && !race.Route.Reverse && race.Route.Name == route;
        }

        void Update()
        {
            var race = RaceController.Instance;
            bool racing = race != null && race.State != RaceController.Stage.Free && race.Route != null;
            bool on = !string.IsNullOrEmpty(showInReverseOf) ? ReverseRaceOf(showInReverseOf)
                    : !string.IsNullOrEmpty(showInForwardOf) ? ForwardRaceOf(showInForwardOf)
                    : !string.IsNullOrEmpty(hideInReverseOf) ? !(racing && race.Route.Reverse && race.Route.Name == hideInReverseOf)
                    : !string.IsNullOrEmpty(hideInForwardOf) ? !ForwardRaceOf(hideInForwardOf)
                    : freeRoam ? !racing : racing && (any || race.Route.Reverse == reverse);
            if (on != shown) Apply(on);
        }

        void Apply(bool on)
        {
            shown = on;
            foreach (Transform t in transform) t.gameObject.SetActive(on);
        }

        public bool Shown => shown;   // for the checks
    }
}
