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
        bool shown = true;

        void Start() => Apply(false);

        void Update()
        {
            var race = RaceController.Instance;
            bool on = race != null && race.State != RaceController.Stage.Free && race.Route != null && race.Route.Reverse == reverse;
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
