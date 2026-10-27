using UnityEngine;

namespace ChastainRush
{
    // Round 66 (4, Dan: "THE HELICOPTER'S BOUNDARY: near the edge a 'Leaving the survey area' warning; keep going and a strong headwind slows it and
    // swings it back (no crash, no teleport). A height ceiling"): within WarnFrom of the map's edge the readout warns; past the edge a headwind
    // grows with the distance out (it outruns the boost's push by about 300 m out), bleeds off the outward speed and swings the nose
    // back toward the map. At HardStop out nothing outward is left. The ceiling is Ceiling (HelicopterFlight.cs), well
    // under where the scenery's outer edge could show (World/EdgeScenery.cs: the ring reaches 9 km past the edge, the fog closes at 6.5 km).
    public sealed partial class HelicopterFlight
    {
        public const float WarnFrom = 300f, HardStop = 700f;
        public float Outside { get; private set; } = -1e9f;   // metres past the map's edge (negative: inside)
        public bool Leaving => Outside > -WarnFrom && !Landed;
        public float MaxOutside { get; set; } = -1e9f;        // checks

        void Boundary(Vector3 pos, Vector3 vH, ref Vector3 aH)
        {
            var land = MapEdge.Land; if (land == null) { Outside = -1e9f; return; }   // Round 76: the land's outline (MapLand), not the rectangle round it
            float o = -land.Distance(pos.x, pos.z);
            Outside = o; MaxOutside = Mathf.Max(MaxOutside, o);
            if (o <= 0f) return;
            var q = land.Nearest(pos.x, pos.z, 50f);
            var n = new Vector3(q.x, pos.y, q.y) - pos; n.y = 0f; if (n.sqrMagnitude < 1e-4f) return; n.Normalize();
            float vOut = Mathf.Max(0f, -Vector3.Dot(vH, n));
            float wind = Mathf.Lerp(0f, 60f, o / 500f) + vOut * Mathf.Clamp01(o / 250f) * .9f;
            aH += n * wind;
            if (o > HardStop && vOut > 0f) car.Body.linearVelocity += n * vOut;   // nothing outward left
            // the nose swung back toward the map all the time it is out (faster the farther out)
            float want = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            yawDeg = Mathf.MoveTowardsAngle(yawDeg, want, (20f + 50f * Mathf.Clamp01(o / 300f)) * Time.fixedDeltaTime);
        }

        static GUIStyle warnStyle;
        void BoundaryWarning(float W)
        {
            if (!Leaving) return;
            if (warnStyle == null) { warnStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; warnStyle.normal.textColor = new Color(1f, .82f, .3f); }
            var r = new Rect(W * .5f - 330, 120, 660, 76);
            var old = GUI.color; GUI.color = new Color(0, 0, 0, .5f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
            GUI.Label(new Rect(r.x, r.y + 4, r.width, 40), "LEAVING THE SURVEY AREA", warnStyle);
            var s = new GUIStyle(warnStyle) { fontSize = 18, fontStyle = FontStyle.Normal }; s.normal.textColor = Color.white;
            GUI.Label(new Rect(r.x, r.y + 42, r.width, 28), Outside > 0f ? "strong headwind ahead · turn back" : "the map ends ahead · turn back", s);
        }
    }
}
