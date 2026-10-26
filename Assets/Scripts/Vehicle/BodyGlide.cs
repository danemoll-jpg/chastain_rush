using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 56 (C, Dan BUG-002 on Elaine Circle's climb: "something here that causes my vehicle to jerk to the right"; he then watched the check drive it:
    // "it is about half way up the hill to the ramp to the right side of the road"): traced in the exe, at 26 m/s where the climb steepens from 17 to 31 %
    // a car's suspension squeezes until its body's front corner dips 10 cm into the road, which kicks it up 0.3 m, rolls it 7 degrees and swings it right.
    // Inside these rectangles (x0, z0, x1, z1; placed by the map build from the track file) the terrain does not push on the body of a vehicle the right
    // way up (the wheels, which are raycasts, carry it; a vehicle on its side or roof keeps its contacts); VehicleSurfaceContacts reads them.
    public sealed class BodyGlide : MonoBehaviour
    {
        public Vector4[] rects;
        void OnEnable() => VehicleSurfaceContacts.Glide = rects ?? new Vector4[0];
        void OnDisable() => VehicleSurfaceContacts.Glide = new Vector4[0];
    }
}
