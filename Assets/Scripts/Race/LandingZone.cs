using System.Collections.Generic;
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
        static readonly List<LandingZone> all = new List<LandingZone>();

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        public static bool Contains(Vector3 p)
        {
            foreach (var z in all) if (z.Inside(new Vector2(p.x, p.z))) return true;
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
