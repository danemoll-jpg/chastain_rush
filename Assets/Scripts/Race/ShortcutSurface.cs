using UnityEngine;

namespace ChastainRush
{
    // Round 10: marks a shortcut's ramp, deck, floor or stairs (MapBuilder.BuildPrism). "held": the car is held on its top
    // (RampHold), for a ramp whose rounded top a car would otherwise fly off at speed (into the upstairs of house 1763, up
    // the stairs of house 1627).
    public sealed class ShortcutSurface : MonoBehaviour
    {
        public bool held;
        public bool landing;   // a landing deck or slope: RampHold keeps the car's speed when it touches down on it
    }
}
