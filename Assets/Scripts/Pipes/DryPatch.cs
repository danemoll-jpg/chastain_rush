using System.Collections.Generic;
using UnityEngine;

namespace ChastainRush
{
    // Round 19, item 3 (BUG-001, Dan: "Can we make it so the water doesnt slow you down for the shortcut...maybe put some rocks here or
    // something?"): a patch of ground standing just out of the water (P1's rock bar across Noonday Creek). ShallowWater measures
    // depth from a vehicle's wheels at full suspension reach, so a vehicle standing on a bar 0.12 m over the surface would still
    // count as wading; inside a dry patch it does not (ShallowWater.Sample / InWater skip it).
    public sealed class DryPatch : MonoBehaviour
    {
        public Vector2[] outline;   // x, z
        public float top;           // the bar's height; above top - 1 m counts as on it (a vehicle down in the water beside it still wades)
        static readonly List<DryPatch> all = new List<DryPatch>();
        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        public static bool Contains(Vector3 p)
        {
            foreach (var d in all) if (p.y > d.top - 1f && d.Inside(new Vector2(p.x, p.z))) return true;
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
