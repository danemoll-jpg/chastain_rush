using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChastainRush
{
    // Round 11 (BUG-007, Dan: "is the reception ramp really necesarry? I think it will be ok without it"): shortcut #6 has
    // no landing deck any more; the car comes down on the ground behind house 1641. That patch of ground is a landing zone
    // (an outline in plan, MapBuilder from Data/shortcuts): RampHold treats a touchdown on the ground inside it as it treated
    // one on the deck (the car's speed is kept and turned along the ground).
    public sealed class LandingZone : MonoBehaviour
    {
        public Vector2[] outline;   // x, z
        // Round 12: a held zone. The car is kept on the grass inside it as on a held ramp (RampHold): shortcut #2's run down to
        // house 1627, where the line makes an S over the crest that a car at racing speed would otherwise fly off, still turning
        public bool hold;
        // Round 68: a catch slope (the Kennesaw Mountain race's huge jumps): a vehicle coming down onto it body first is set on its wheels (RampHold)
        public bool catchSlope;
        static readonly List<LandingZone> all = new List<LandingZone>();

        void OnEnable() { all.Add(this); Rects(); }
        void OnDisable() { all.Remove(this); Rects(); }
        static void Rects()   // Round 68: the catch slopes' bounds for VehicleSurfaceContacts
        {
            var r = new List<Vector4>();
            foreach (var z in all)
                if (z.catchSlope && z.outline != null && z.outline.Length > 2)
                    r.Add(new Vector4(z.outline.Min(q => q.x), z.outline.Min(q => q.y), z.outline.Max(q => q.x), z.outline.Max(q => q.y)));
            Racer.VehicleSurfaceContacts.CatchRects = r.ToArray();
        }

        public static bool Contains(Vector3 p)
        {
            foreach (var z in all) if (z.Inside(new Vector2(p.x, p.z))) return true;
            return false;
        }

        public static bool Holds(Vector3 p)   // Round 12
        {
            foreach (var z in all) if (z.hold && z.Inside(new Vector2(p.x, p.z))) return true;
            return false;
        }

        public static bool Catches(Vector3 p)   // Round 68
        {
            foreach (var z in all) if (z.catchSlope && z.Inside(new Vector2(p.x, p.z))) return true;
            return false;
        }

        bool Inside(Vector2 q)
        {
            if (outline == null) return false;
            bool c = false;
            for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
            {
                Vector2 a = outline[i], b = outline[j];
                if ((a.y > q.y) != (b.y > q.y) && q.x < (b.x - a.x) * (q.y - a.y) / (b.y - a.y) + a.x) c = !c;
            }
            return c;
        }
    }
}
