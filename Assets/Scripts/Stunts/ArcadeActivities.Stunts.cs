using System;
using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    // 0.106 Part B: stunt scoring, built on the jump scoring. In Free Roam (and the Stunt event, which runs on it) every flight is watched for tricks:
    // the vehicle's turn about its own axes from takeoff to touchdown gives flips (forward / back, each full 360), rolls (sideways 360) and spins
    // (flat 360s); the stunt park's loop and its barrel-roll ramp are tricks of their own. A flight counts when it lands on its wheels (two or more
    // wheels on the ground for 0.3 s, not wiped out): its jump points (as the clean jump's: 10 a metre and 100 a second in the air) and its tricks,
    // shown like "BACKFLIP + 360 SPIN / 412 pts". Landing on the roof or side, or wiping out, is a bail: that flight's tricks are lost ("BAILED")
    // and the combo ends. Combo: each scored flight (or loop) within the chain time on the ground of the last one adds a link; when the chain time
    // passes on the ground the combo is banked as its points times its links (x2, x3, ...). Works for every vehicle; the stunt vehicles can steer in
    // the air (ArcadeVehicle). Never in races; nothing changes in them.
    // 0.107 Part A: no bail on touching down at an angle. From the first touch (body or wheel) the vehicle has a recovery window (RecoverWindow):
    // back on its wheels and driving on (wheels down and upright for 0.3 s) is a landing with the tricks turned so far ("SKETCHY LANDING" when it
    // first touched 60 degrees or more over; full points). A bail only when the roof itself hits the ground hard (a contact on the top of the body,
    // more than about 100 degrees over), when the vehicle is still on its roof or side as the window ends, or a wipe-out. Part B: a flight that takes
    // off inside the park counts wherever it lands. Part D: 5 s on the ground to chain the next trick.
    // 0.108 Part D: every combo banked in the park (the Stunt event and Free Roam) can enter the Top 10 Best Combos (StuntProgress), with its links
    // and its tricks in short form (BF backflip, FF frontflip, R roll, BR barrel roll, 360 spin, LOOP, J a plain jump).
    // 0.120 Part B: a flight's points (its tricks and its jump) are multiplied by its number of different trick types (Score: flip, roll, spin): a
    // backflip x1, backflip + roll x2, backflip + roll + 360 x3; doing a type again adds its points, not multiplier. The chain multiplies the combo
    // on top. Part A: the live trick (Live, LastResult) for the trick panel (TrickHud). Part D: a flight that takes off outside the park while an
    // in-park combo is going no longer spoils that combo: in the Stunt event it is not counted on its own; in Free Roam the park combo is banked first.
    // Chastain Rush, Round 36 (Part A1): ported as it is; the split-screen, campaign and police parts are left out (none here), the player's name
    // from PlayerNames (Round 34). Free Roam everywhere on the map (Racer's rule), the Top 10 Best Combos from the park only.
    public sealed partial class ArcadeActivities
    {
        public const float ComboGap = 5f, MinStuntAir = .6f, MinStuntDistance = 4f, RecoverWindow = 1.5f, RoofUp = -.17f, RoofHit = 3f;
        public const int FlipPoints = 300, RollPoints = 250, SpinPoints = 150, LoopPoints = 500, BarrelPoints = 300, MaxLinks = 9;
        public bool StuntsOn => FreeRoam;
        // a combo banked: its points (after the multiplier) and whether every link of it was in the stunt park
        public event Action<int, bool> ComboBanked;
        public int ComboLinks { get; private set; }
        public int ComboPoints { get; private set; }
        public bool ComboInPark { get; private set; } = true;
        public int ComboTotal => ComboPoints * Mathf.Max(1, ComboLinks);
        public int BestCombo { get; private set; }           // this session's best banked combo
        public int Tricks { get; private set; }               // tricks landed this session
        public int Bails { get; private set; }
        public int Loops { get; private set; }
        public string LastStunt { get; private set; } = "";   // the last scored flight's line (checks)
        public int LastComboLinks { get; private set; }        // 0.108 Part D: the last banked combo's links, its tricks (short form) and Top 10 place
        public string LastComboTricks { get; private set; } = "";
        public int LastComboPlace { get; private set; }
        public int LastComboTotal { get; private set; }        // Chastain Rush: the last banked combo's points (checks)
        // 0.108 Part A: the time left to make the next trick of the combo going (0: none)
        public float ComboLeft => ComboLinks > 0 ? Mathf.Max(0, ComboGap - sSince) : 0;
        public string StuntFeedback => Time.time < feedbackUntil ? Feedback : null;
        public int LastStuntPoints { get; private set; }
        public Vector3 LastStuntRotation { get; private set; } // pitch, yaw, roll turned in that flight (degrees)
        public float LastTouchTilt { get; private set; } = -1;  // the last flight's tilt from upright at its first touch (degrees; checks)
        public string LastBail { get; private set; } = "";
        public bool LastSketchy { get; private set; }
        public float LastStuntAir { get; private set; }        // Chastain Rush: the last scored flight's air time and height over its takeoff (checks)
        public float LastStuntHeight { get; private set; }
        // 0.107 Part E: in the air or going round the loop (the Stunt event waits for this at 0:00)
        public bool StuntBusy => sFlying || InLoop;
        // the flight; sTouch: time since its first touch (-1: not touched yet)
        bool sFlying, sBarrel; float sGround, sAir, sStable, sSince, sTouch = -1, sTilt = -1, sTop; Vector3 sTurn, sTakeoff, sLanding;
        // the loop
        float loopAcc, loopPrev = float.NaN, loopOut = 1;
        readonly List<string> comboTricks = new();
        string StuntHud
        {
            get
            {
                if (!StuntsOn) return "";
                if (ComboLinks > 0) return $"COMBO x{ComboLinks}  ·  {ComboTotal:N0} pts" + (ComboLinks < MaxLinks ? $"   (next within {Mathf.Max(0, ComboGap - sSince):0.0} s)" : "");
                return "";
            }
        }
        // 0.120 Part B: one flight's score. live: while still in the air, a turn counts once it is within 60 degrees of full (at the landing a half
        // turn or more counts, as before)
        public sealed class FlightScore
        {
            public int points, tricks, jump, types; public readonly List<string> names = new(), shorts = new();
            public string Names => string.Join(" + ", names);
            public string Multiplier => types > 1 ? $"x{types}" : "";
        }
        // Chastain Rush, Round 38 (Part B): smashPoints / smashes, what the vehicle broke in this flight: SMASH, a trick type of its own
        public static FlightScore Score(Vector3 turn, bool barrel, float distance, float air, bool live = false, int smashPoints = 0, int smashes = 0)
        {
            var s = new FlightScore();
            int Turns(float d) => live ? (int)Mathf.Sign(d) * Mathf.FloorToInt((Mathf.Abs(d) + 60) / 360) : Mathf.RoundToInt(d / 360);
            int flips = Turns(turn.x), rolls = Turns(turn.z), spins = Mathf.FloorToInt((Mathf.Abs(turn.y) + 30) / 360);
            string Times(int n) => n == 2 ? "DOUBLE " : n >= 3 ? "TRIPLE " : "";
            if (flips != 0) { int n = Mathf.Abs(flips); s.names.Add(Times(n) + (flips < 0 ? "BACKFLIP" : "FRONTFLIP")); s.shorts.Add((n > 1 ? n.ToString() : "") + (flips < 0 ? "BF" : "FF")); s.tricks += FlipPoints * n; s.types++; }
            if (rolls != 0) { int n = Mathf.Abs(rolls); s.names.Add(Times(n) + (barrel ? "BARREL ROLL" : "ROLL")); s.shorts.Add((n > 1 ? n.ToString() : "") + (barrel ? "BR" : "R")); s.tricks += RollPoints * n + (barrel ? BarrelPoints : 0); s.types++; }
            if (spins > 0) { s.names.Add(spins * 360 + " SPIN"); s.shorts.Add((spins * 360).ToString()); s.tricks += SpinPoints * spins; s.types++; }
            if (smashes > 0) { s.names.Add(smashes > 1 ? $"SMASH x{smashes}" : "SMASH"); s.shorts.Add("SM"); s.tricks += smashPoints; s.types++; }   // Round 38
            s.jump = Mathf.RoundToInt(Mathf.Min(distance, 250) * 10 + Mathf.Min(air, 12) * 100);
            s.points = (s.tricks + s.jump) * Mathf.Max(1, s.types);
            return s;
        }
        // 0.120 Part A: the trick panel (TrickHud) carries this vehicle's trick lines in the Stunt event (Racer: also its Stunt Races and chapter);
        // Chastain Rush: also in Free Roam inside the park, where the tricks are what the place is for
        public bool TrickBox => StuntsOn && car && (StuntEvent.InEvent(car) || StuntPark.Current && StuntPark.Inside(car.Body.position, 20));
        // in the air: the tricks turned so far, the multiplier and the points on the line (null on the ground)
        public FlightScore Live => sFlying && sAir > .2f && car ? Score(sTurn, sBarrel, Vector3.ProjectOnPlane(car.Body.position - sTakeoff, Vector3.up).magnitude, sAir, true, sSmashPoints, sSmashes) : null;
        // the last flight's outcome ("LANDED +1,240", "BAILED", "NOT COUNTED"), for a moment after it
        public string LastResult { get; private set; } = ""; public float LastResultAt { get; private set; } = -100; public int LastResultKind { get; private set; } // 1 landed, 2 bailed, 3 not counted
        // 0.120 Part A: a stunt line; where the trick panel carries them (TrickBox) it is left out of the activity lines (Hud)
        void StuntSay(string value, float seconds) { Message(value, seconds); feedbackStunt = true; }
        void Result(string text, int kind) { LastResult = text; LastResultAt = Time.time; LastResultKind = kind; }
        public int LastFlightTypes { get; private set; }
        void StuntReset() { SmashReset(); sFlying = false; sBarrel = false; sGround = sAir = sStable = 0; sTouch = sTilt = -1; sTurn = Vector3.zero; loopAcc = 0; loopPrev = float.NaN; loopOut = 1; }
        void StuntSession() { StuntReset(); ComboLinks = ComboPoints = 0; ComboInPark = true; sSince = 0; comboTricks.Clear(); }

        void StuntStep(Vector3 p, float dt, bool grounded)
        {
            if (!StuntsOn) { if (ComboLinks > 0) EndCombo(null); StuntReset(); return; }
            if (StuntPark.Current && StuntPark.Current.loopCollider) LoopStep(p, dt);
            if (InLoop) { sFlying = false; sGround = Mathf.Max(sGround, .2f); return; } // round the loop the wheels may lift at the top: not a flight
            var w = car.transform.InverseTransformDirection(car.Body.angularVelocity) * (dt * Mathf.Rad2Deg);
            bool any = car.GroundedWheels > 0;
            if (!sFlying)
            {
                // the time on the ground since the last link (not counted while going round the loop)
                if (any) { sGround += dt; if (ComboLinks > 0 && !(StuntPark.Current && loopOut < .2f)) { sSince += dt; if (sSince >= ComboGap) EndCombo(null); } }
                else if (sGround >= .2f && car.Body.linearVelocity.magnitude > 3 && !configuration.WipedOut)
                {
                    TakeOff(); sFlying = true; sAir = 0; sStable = 0; sTouch = sTilt = -1; sTurn = Vector3.zero; sTakeoff = previous; sTop = previous.y;   // Round 38: TakeOff (a smash just before the takeoff is the flight's)
                    sBarrel = StuntPark.Current && Vector3.Distance(previous, StuntPark.BarrelLip) < 7 && Vector3.Dot(car.Body.linearVelocity, StuntPark.BarrelForward) > 3;
                }
                else sGround = 0;
                return;
            }
            sTurn += w; sTop = Mathf.Max(sTop, p.y);
            if (configuration.WipedOut) { Bail(car.transform.up.y < RoofUp ? "Landed on the roof" : "Landed on the side"); return; }
            if (any) Touch();
            if (sTouch >= 0) sTouch += dt;
            // on the wheels: two or more down and upright (or every wheel down, as on a steep ramp face)
            bool wheels = grounded && (car.transform.up.y > .5f || car.GroundedWheels >= car.suspensionPoints.Length);
            if (!wheels)
            {
                if (!grounded) sAir += dt;
                sStable = 0;
                if (sTouch >= RecoverWindow)
                {
                    // the window is over: still lying on the roof or side is a bail; bounced back up into the air, the next touch starts a new window
                    if (Lying()) { Bail(car.transform.up.y < RoofUp ? "Landed on the roof" : "Landed on the side"); return; }
                    sTouch = -1;
                }
                return;
            }
            if (sStable == 0) sLanding = p;
            sStable += dt; if (sStable < .3f) return;
            // down on the wheels
            sFlying = false; sGround = sStable; LastTouchTilt = sTilt; LastSketchy = sTilt >= 60; LastBail = "";
            if (sAir < MinStuntAir || Vector3.ProjectOnPlane(sLanding - sTakeoff, Vector3.up).magnitude < MinStuntDistance && Mathf.Abs(sTurn.y) < 300)
            {
                // a hop: no jump, and no break in the combo (Round 38: what it smashed counts as smashed on the ground)
                if (sSmashes > 0) { int pts = sSmashPoints; string nm = sSmashName; SmashReset(); SmashStunt(nm, pts); }
                return;
            }
            Landed();
        }
        // the tricks of a flight that ended on its wheels
        void Landed()
        {
            float distance = Vector3.ProjectOnPlane(sLanding - sTakeoff, Vector3.up).magnitude;
            var f = Score(sTurn, sBarrel, distance, sAir, false, sSmashPoints, sSmashes); SmashReset();
            Tricks += f.names.Count; LastStuntRotation = new Vector3(sTurn.x, sTurn.y, sTurn.z); LastFlightTypes = f.types; LastStuntAir = sAir; LastStuntHeight = sTop - sTakeoff.y;
            string line = f.names.Count > 0 ? f.Names + (f.types > 1 ? "  " + f.Multiplier : "") : $"{(LastSketchy ? "JUMP" : "CLEAN JUMP")} / {DisplayUnits.Jump(distance)} / {sAir:0.00} s";
            if (LastSketchy) line = "SKETCHY LANDING\n" + line;
            bool inPark = StuntPark.Current && StuntPark.OnPark(sTakeoff); // 0.107 Part B: took off in the park: counts wherever it lands
            // 0.120 Part D: took off outside the park with an in-park combo going (a hop down the bank after flying out): it never spoils that combo
            if (!inPark && StuntPark.Current && ComboLinks > 0 && ComboInPark)
            {
                if (StuntEvent.InEvent(car)) { Result("OUTSIDE THE PARK: NOT COUNTED", 3); StuntSay("Outside the park: not counted", 2.5f); return; }
                EndCombo(null);
            }
            Link(f.points, line, inPark, f.shorts.Count > 0 ? string.Join("+", f.shorts) : "J");
        }
        void Link(int points, string line, bool inPark, string trick)
        {
            if (ComboLinks < MaxLinks) ComboLinks++;
            comboTricks.Add(trick);
            ComboPoints += points; ComboInPark &= inPark; sSince = 0;
            LastStunt = line; LastStuntPoints = points; Result($"LANDED  +{points:N0}", 1);
            StuntSay($"{line} / {points:N0} pts" + (ComboLinks > 1 ? $"\nCOMBO x{ComboLinks} · {ComboTotal:N0}" : ""), 3.5f);
        }
        // the end of a combo: banked (points x links), or with a bail
        void EndCombo(string bail)
        {
            if (ComboLinks <= 0) { if (bail != null) StuntSay("BAILED / " + bail, 3.5f); return; }
            int total = ComboTotal; bool park = ComboInPark; int links = ComboLinks;
            ComboLinks = ComboPoints = 0; ComboInPark = true; sSince = 0;
            BestCombo = Mathf.Max(BestCombo, total); LastComboTotal = total;
            LastComboLinks = links; LastComboTricks = string.Join(" · ", comboTricks); comboTricks.Clear();
            // 0.108 Part D: the Top 10 Best Combos (in the park; in the Stunt event only while the player's round still counts)
            LastComboPlace = 0;
            if (park && StuntPark.Current && StuntEvent.Counts(car))
                LastComboPlace = Player == 2 && ChastainRush.SplitScreen.P2Ai ? 0 : StuntProgress.RecordComboTop(ChastainRush.SplitScreen.Active ? ChastainRush.SplitScreen.NameOf(Player) : PlayerNames.Player, configuration.profileId, total, links, LastComboTricks);   // Round 37: each player's name; never the AI's
            string top = LastComboPlace > 0 ? $"\nNEW TOP 10 COMBO — #{LastComboPlace}" : "";
            if (bail != null) StuntSay($"BAILED / {bail}\nCOMBO ENDED · x{links} · {total:N0} pts" + top, 4);
            else if (links > 1) StuntSay($"COMBO x{links} / {total:N0} pts" + top, 3.5f);
            else if (top != "") StuntSay($"{LastStunt} / {total:N0} pts" + top, 3.5f);
            // 0.120 Part A: the trick panel: the combo banked (its multiplier and points), or the bail that ended it
            if (bail != null) Result($"BAILED  ·  COMBO x{links} BANKED  +{total:N0}", 2); else if (links > 1 || top != "") Result($"COMBO x{links}  +{total:N0}" + (LastComboPlace > 0 ? $"  ·  TOP 10 #{LastComboPlace}" : ""), 1);
            ComboBanked?.Invoke(total, park);
            // the best single combo in Free Roam (not the Stunt event)
            if (!StuntEvent.InEvent(car) && Player == 1) StuntProgress.RecordCombo(total, configuration.profileId);
        }
        void Bail(string why)
        {
            if (!sFlying) return;
            bool tried = sAir >= MinStuntAir || sTurn.magnitude > 90;
            SmashReset(); sFlying = false; sGround = 0; Bails++; LastBail = why; LastTouchTilt = sTilt; Result("BAILED", 2);
            if (tried || ComboLinks > 0) EndCombo(why);
        }
        // the first touch of a flight (a wheel or the body): its tilt from upright, and the recovery window starts
        void Touch() { if (sTouch >= 0) return; sTouch = 0; sTilt = Vector3.Angle(car.transform.up, Vector3.up); }
        // lying on the roof or side: tipped 60 degrees or more with the ground just below
        bool Lying() => car.transform.up.y < .5f && car.gameObject.scene.GetPhysicsScene().Raycast(car.Body.worldCenterOfMass, Vector3.down, out _, 2.5f, car.groundMask, QueryTriggerInteraction.Ignore);
        // ActivityLandingContact: a body contact starts the recovery window; only the roof itself hitting the ground hard is an instant bail
        bool InLoop => StuntPark.Current && loopOut < .2f && loopAcc > 60;
        void StuntContact(Vector3 normal, Vector3? point, float speed)
        {
            if (!StuntsOn || !sFlying || InLoop) return;
            Touch();
            if (normal.y > .4f && point.HasValue && speed >= RoofHit && car.transform.up.y < RoofUp && OnRoof(point.Value)) Bail("Landed on the roof");
        }
        // a contact on the top of the body (not its nose or tail)
        bool OnRoof(Vector3 point)
        {
            var box = car.GetComponent<BoxCollider>(); var q = car.transform.InverseTransformPoint(point);
            if (!box) return Vector3.Dot(point - car.Body.worldCenterOfMass, car.transform.up) > .3f;
            return q.y > box.center.y + box.size.y * .3f && Mathf.Abs(q.z - box.center.z) < box.size.z * .4f;
        }
        // the loop: once round (330 degrees or more about its centre, staying in it), on the wheels
        void LoopStep(Vector3 p, float dt)
        {
            var rel = p - StuntPark.LoopOrigin; float r = StuntPark.LoopRadius;
            float fwd = Vector3.Dot(rel, StuntPark.LoopForward), up = rel.y, lat = Vector3.Dot(rel, StuntPark.LoopSide);
            float dist = new Vector2(fwd, up - r).magnitude;
            bool inside = lat > -4 && lat < StuntPark.LoopShift + 4 && dist > r - 3.2f && dist < r + 1.5f && Mathf.Abs(fwd) < r + 2.5f;
            if (!inside) { loopOut += dt; if (loopOut > .4f) { loopAcc = 0; loopPrev = float.NaN; } return; }
            loopOut = 0; float phi = Mathf.Atan2(fwd, r - up) * Mathf.Rad2Deg;
            if (!float.IsNaN(loopPrev)) { float d = Mathf.DeltaAngle(loopPrev, phi); if (d > 0) loopAcc += d; else if (d < -15) loopAcc = 0; }
            loopPrev = phi;
            if (loopAcc >= 330 && car.GroundedWheels >= 2 && !configuration.WipedOut)
            {
                loopAcc = float.NegativeInfinity; Loops++; Tricks++; sFlying = false; sGround = 1;
                Link(LoopPoints, "LOOP", true, "LOOP");
            }
        }
    }
}
