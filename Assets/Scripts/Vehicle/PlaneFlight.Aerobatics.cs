using UnityEngine;

namespace ChastainRush
{
    // Round 91 (B, Dan: "Allow planes to do barrel roles and loops"): the player's plane in the air has no bank or pitch limit. The attitude is
    // turned as a whole (a quaternion), so it rolls all the way round and pulls a full loop; still arcade-easy: let go of the roll and the wings
    // come level the near way, let go of the pitch and the nose comes back to the horizon (as before). Too slow, the nose eases down for speed.
    // Nothing about being upside down crashes it: only meeting something does (PlaneFlight.Crash.cs). In HIGH MODE a roll or a loop that would end
    // in the ground is pulled out of by itself. AI pilots and scripted checks keep the old limited flight (Aerobatic false). A finished roll or
    // loop shows a small note at the top of the screen.
    public sealed partial class PlaneFlight
    {
        bool Aerobatic => !Scripted && !(vInput && !vInput.enabled) && !(car && car.Puppet);
        // Round 93 (B, Dan: "To roll lets make it so you have to press the shoulder buttons"): the free flight only while LB / RB (Shift) is held, and
        // after it is let go until the wings are level and the nose near the horizon again; then the limited bank-and-turn flight takes over
        public const float PlayerBankTurn = 16f;   // deg/s at full stick on top of the bank's turn (the shoulders' old rudder, now on the stick)
        public bool AeroHeld { get; private set; }
        public bool FreeFlight { get; private set; }   // checks: the free (roll and loop) flight is on
        bool FreeNow(float pIn, float bIn)
        {
            if (AeroHeld) { FreeFlight = true; return true; }
            if (!FreeFlight) return false;
            var att = Quaternion.Euler(-pitchDeg, yawDeg, -rollDeg); var f = att * Vector3.forward; float r = RollOf(att);
            float p = Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f)) * Mathf.Rad2Deg;
            if (Mathf.Abs(r) > 12f || p > MaxPitch || p < -MaxPitch - 10f || (att * Vector3.up).y < .5f) return true;   // still levelling
            // level again: hand the attitude to the limited flight as plain angles
            FreeFlight = false; var h = Ahead(att);
            pitchDeg = p; rollDeg = r; yawDeg = Mathf.Atan2(h.x, h.z) * Mathf.Rad2Deg;
            return false;
        }
        float rollAcc, pitchAcc, rollIdle, pitchIdle; string trickNote = ""; float trickAt = -9f;
        public int Rolls { get; private set; }   // checks
        public int Loops { get; private set; }
        public bool HighRescue { get; private set; }

        static float RollOf(Quaternion att) { var r = att * Vector3.right; var u = att * Vector3.up; return Mathf.Atan2(-r.y, u.y) * Mathf.Rad2Deg; }   // + banked right, +-180 upside down
        // the nose turned toward 'target' (a world direction) by at most 'deg', about the axis between them
        static Quaternion NoseToward(Quaternion att, Vector3 target, float deg)
        {
            var f = att * Vector3.forward; float a = Vector3.Angle(f, target); if (a < .01f) return att;
            var axis = Vector3.Cross(f, target); if (axis.sqrMagnitude < 1e-8f) axis = att * Vector3.right;
            return Quaternion.AngleAxis(Mathf.Min(a, deg), axis.normalized) * att;
        }
        // the horizon ahead: the nose's heading; nose straight up or down, the way the belly faces (where letting go brings it out upright)
        static Vector3 Ahead(Quaternion att)
        {
            var f = att * Vector3.forward; var h = new Vector3(f.x, 0f, f.z);
            if (h.sqrMagnitude < .04f) { var u = att * Vector3.up; h = new Vector3(-u.x, 0f, -u.z) * Mathf.Sign(f.y); }
            return h.sqrMagnitude > 1e-6f ? h.normalized : Vector3.forward;
        }
        static Vector3 Pitched(Vector3 ahead, float deg) => ahead * Mathf.Cos(deg * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(deg * Mathf.Deg2Rad);

        void FreeAttitude(Rigidbody body, float pIn, float bIn, float rud, float slow, float dt)
        {
            var att = Quaternion.Euler(-pitchDeg, yawDeg, -rollDeg);
            float pRate = PitchR * Mathf.Lerp(1f, .55f, Mathf.Clamp01(Airspeed / Top));
            bool pitching = Mathf.Abs(pIn) > .05f, rolling = Mathf.Abs(bIn) > .05f && Outside <= 0f;
            var f0 = att * Vector3.forward; var r0 = att * Vector3.right;
            // the stick: pitch about the wings, roll about the nose, as far as you hold it
            if (pitching) att = att * Quaternion.AngleAxis(-pIn * pRate * dt, Vector3.right);
            if (rolling) att = att * Quaternion.AngleAxis(-bIn * Roll * dt, Vector3.forward);
            // let go: the wings level the near way (quicker from upside down), the nose back to the horizon
            var f = att * Vector3.forward;
            if (!rolling && !pitching && Outside <= 0f && Mathf.Abs(f.y) < .9f)   // (not while the pitch is held: over a loop's top it is upside down on purpose)
            {
                float r = RollOf(att), step = Mathf.MoveTowards(r, 0f, (Mathf.Abs(r) > 90f ? 70f : 40f) * dt) - r;
                att = att * Quaternion.AngleAxis(-step, Vector3.forward);
            }
            if (!pitching) att = NoseToward(att, Ahead(att), 14f * dt);
            if (slow > 0f) att = NoseToward(att, Pitched(Ahead(att), -12f), 30f * slow * dt);
            f = att * Vector3.forward;
            if (body.position.y > Ceiling) { float lim = Mathf.Lerp(0f, -8f, (body.position.y - Ceiling) / 60f); if (Mathf.Asin(Mathf.Clamp(f.y, -1, 1)) * Mathf.Rad2Deg > lim) att = NoseToward(att, Pitched(Ahead(att), lim), 60f * dt); }
            // past the map's edge: the wide turn back (Boundary sets the roll it wants and lifts a dive)
            {
                float r = RollOf(att), r2 = r; float p0 = pitchDeg = Mathf.Asin(Mathf.Clamp((att * Vector3.forward).y, -1, 1)) * Mathf.Rad2Deg;
                Boundary(body.position, ref r2);
                if (Outside > 0f) { att = att * Quaternion.AngleAxis(-(Mathf.DeltaAngle(r, r2)), Vector3.forward); if (pitchDeg != p0) att = NoseToward(att, Pitched(Ahead(att), pitchDeg), Mathf.Abs(pitchDeg - p0)); }
            }
            // HIGH MODE: a roll or loop heading into the ground pulls out by itself
            HighRescue = false;
            if (HighMode.ForVehicle(car))
            {
                f = att * Vector3.forward; var u = att * Vector3.up; float down = Mathf.Max(0f, -f.y * Airspeed);
                if ((f.y < -.1f || u.y < 0f) && HeightAboveGround < Mathf.Max(30f, down * 3.5f))
                {
                    HighRescue = true; float r = RollOf(att);
                    att = att * Quaternion.AngleAxis(-(Mathf.MoveTowards(r, 0f, 200f * dt) - r), Vector3.forward);
                    att = NoseToward(att, Pitched(Ahead(att), 8f), 70f * dt);
                }
            }
            // the turn: the bank turns it (as the limited flight up to 70 degrees; on its side the most, upside down none), less the steeper the nose
            {
                f = att * Vector3.forward; float r = RollOf(att), a = Mathf.Abs(r);
                float eff = a <= 70f ? Mathf.Tan(a * Mathf.Deg2Rad) : a <= 110f ? Mathf.Tan(70f * Mathf.Deg2Rad) : Mathf.Tan(70f * Mathf.Deg2Rad) * (180f - a) / 70f;
                float level = Mathf.Sqrt(Mathf.Max(0f, 1f - f.y * f.y));
                float turn = Mathf.Rad2Deg * 9.81f * eff * Mathf.Sign(r) / Mathf.Max(Airspeed, 15f) * level;
                att = Quaternion.AngleAxis((turn + rud * RudderRate * level) * dt, Vector3.up) * att;
            }
            var e = att.eulerAngles; pitchDeg = -Mathf.DeltaAngle(0f, e.x); yawDeg = e.y; rollDeg = -Mathf.DeltaAngle(0f, e.z);
            Tricks(pitching ? pIn * pRate * dt : 0f, rolling ? bIn * Roll * dt : 0f, r0, dt);
        }

        // a full roll (the roll held round 330 degrees one way) or a full loop (the pitch held round 330 degrees with the wings level across: not a
        // tight turn on its side); let go for a moment, or turn the other way, and it starts again
        void Tricks(float dPitch, float dRoll, Vector3 right, float dt)
        {
            rollIdle = dRoll == 0f ? rollIdle + dt : 0f; pitchIdle = dPitch == 0f ? pitchIdle + dt : 0f;
            if (rollIdle > .8f || (dRoll != 0f && Mathf.Sign(dRoll) != Mathf.Sign(rollAcc) && Mathf.Abs(rollAcc) > 20f)) rollAcc = 0f;
            if (pitchIdle > .8f || (dPitch != 0f && Mathf.Sign(dPitch) != Mathf.Sign(pitchAcc) && Mathf.Abs(pitchAcc) > 20f)) pitchAcc = 0f;
            rollAcc += dRoll;
            if (Mathf.Abs(right.y) < .5f) pitchAcc += dPitch;
            if (Mathf.Abs(rollAcc) >= 330f) { Rolls++; Note(rollAcc > 0f ? "BARREL ROLL  >" : "<  BARREL ROLL"); rollAcc = 0f; }
            if (Mathf.Abs(pitchAcc) >= 330f) { Loops++; Note(pitchAcc > 0f ? "LOOP" : "OUTSIDE LOOP"); pitchAcc = 0f; }
        }
        void Note(string s) { trickNote = s; trickAt = Time.unscaledTime; Debug.Log($"[Plane] {s} at ({car.Body.position.x:0}, {car.Body.position.y:0}, {car.Body.position.z:0}), {Airspeed:0} m/s"); }
        void ResetTricks() { rollAcc = pitchAcc = 0f; }

        static GUIStyle trickStyle;
        void TrickGui(float W)
        {
            if (Time.unscaledTime - trickAt > 2.2f || trickNote.Length == 0) return;
            if (trickStyle == null) { trickStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; trickStyle.normal.textColor = new Color(1f, .86f, .35f); }
            float a = Mathf.Clamp01((2.2f - (Time.unscaledTime - trickAt)) / .4f); var oc = GUI.color;
            GUI.color = new Color(0, 0, 0, .45f * a); GUI.DrawTexture(new Rect(W * .5f - 190, 64, 380, 50), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, a); GUI.Label(new Rect(W * .5f - 190, 64, 380, 50), trickNote, trickStyle); GUI.color = oc;
        }
    }
}
