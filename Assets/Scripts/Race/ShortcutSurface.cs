using UnityEngine;

namespace ChastainRush
{
    // Round 10: marks a shortcut's ramp, deck, floor or stairs (MapBuilder.BuildPrism). "held": the car is held on its top
    // (RampHold), for a ramp whose rounded top a car would otherwise fly off at speed (Round 12: the stairs and kicker in
    // house 1627; the grass before them is held through LandingZone.hold).
    public sealed class ShortcutSurface : MonoBehaviour
    {
        public bool held;
        public bool landing;   // a landing deck or slope: RampHold keeps the car's speed when it touches down on it
    }
}
