using UnityEngine;

namespace ChastainRush
{
    // Round 78 (E, Dan's BUG-003): the square's traffic lights lit by phase. Two groups (the two ways round the square) take turns: green 22 s, yellow 3 s,
    // then red while the other group has its turn (1 s all red between). The lamps are the map's own pieces (MapBuilder.MariettaSquare.cs), hidden by their
    // pole's breakable when it is knocked down. Look only: traffic does not obey them.
    public sealed class TrafficSignals : MonoBehaviour
    {
        public Renderer[] lamps; public int[] group, color;   // color 0 red, 1 yellow, 2 green
        static Material[] on, off;
        const float Green = 22f, Yellow = 3f, AllRed = 1f;
        int shown = -1;

        void Start()
        {
            if (on == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                Color[] lit = { new(1f, .12f, .06f), new(1f, .72f, .08f), new(.15f, 1f, .45f) };
                on = new Material[3]; off = new Material[3];
                for (int i = 0; i < 3; i++)
                {
                    on[i] = new Material(sh) { name = "Signal on " + i, color = lit[i] }; on[i].SetColor("_BaseColor", lit[i]);
                    var dim = lit[i] * .16f; dim.a = 1f; off[i] = new Material(sh) { name = "Signal off " + i, color = dim }; off[i].SetColor("_BaseColor", dim);
                }
            }
        }

        void Update()
        {
            float cyc = 2f * (Green + Yellow + AllRed), t = Mathf.Repeat(WorldClock.Now + 7f, cyc);   // Round 79: the world's clock (a replay's as it was)
            int phase = t < Green ? 0 : t < Green + Yellow ? 1 : t < Green + Yellow + AllRed ? 2 : t < 2f * Green + Yellow + AllRed ? 3 : t < 2f * (Green + Yellow) + AllRed ? 4 : 5;
            if (phase == shown) return; shown = phase;
            for (int i = 0; i < lamps.Length; i++)
            {
                if (!lamps[i]) continue;
                int g = group[i]; int lit;   // what this group shows
                if (g == 0) lit = phase == 0 ? 2 : phase == 1 ? 1 : 0; else lit = phase == 3 ? 2 : phase == 4 ? 1 : 0;
                lamps[i].sharedMaterial = color[i] == lit ? on[color[i]] : off[color[i]];
            }
        }
    }
}
