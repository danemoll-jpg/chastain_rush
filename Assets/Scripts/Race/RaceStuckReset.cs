using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 93 (D1, Dan's picture: a check race's bike stood upright on its tail against a building's wall, wheels off the ground, and no reset came for
    // 20+ minutes): in a race, a player's car or bike that is wedged gets a reset as an AI's does. Wedged: on its tail or nose, on its side or roof, hung
    // with no wheel down and barely moving, or barely moving with the throttle on, for StuckFor (5 s). Then "Press Y to reset" (R on the keyboard) on the
    // race's message line; in HIGH MODE the reset comes at once, otherwise by itself at AutoAfter (8 s). The reset is the reset button's (CarRecovery:
    // before the next checkpoint not yet passed, no penalty). Not the planes or the helicopter (their own crashes), not before the clock starts.
    public sealed class RaceStuckReset : MonoBehaviour
    {
        public const float StuckFor = 5f, AutoAfter = 8f;
        public static int Prompts, AutoResets;   // checks
        ArcadeVehicle car; VehicleRespawn respawn; VehicleInput input; VehicleConfiguration config;
        float stuck; bool said;
        public int Player = 1;

        public static void Ensure(ArcadeVehicle c, int player)
        {
            if (!c) return; var s = c.GetComponent<RaceStuckReset>(); if (!s) s = c.gameObject.AddComponent<RaceStuckReset>(); s.Player = player;
        }
        void Awake() { car = GetComponent<ArcadeVehicle>(); respawn = GetComponent<VehicleRespawn>(); input = GetComponent<VehicleInput>(); config = GetComponent<VehicleConfiguration>(); }
        void OnEnable() { if (respawn) respawn.Respawned += Clear; }
        void OnDisable() { if (respawn) respawn.Respawned -= Clear; }
        void Clear() { stuck = 0f; said = false; }

        void FixedUpdate()
        {
            var race = RaceController.Instance;
            bool racing = car && respawn && race && race.State == RaceController.Stage.Racing && !car.Body.isKinematic && !car.Puppet
                          && !(config && config.Profile != null && config.Profile.Flies) && race.EntrantFor(car) is RaceEntrant me && me.ClockStarted && !me.Finished;
            if (!racing) { Clear(); return; }
            float v = car.Body.linearVelocity.magnitude; var t = transform;
            bool onEnd = Mathf.Abs(t.forward.y) > .7f && v < 3f;                 // on its tail or nose
            bool over = t.up.y < .45f && v < 3f;                                  // on its side or roof (HIGH rights it itself first)
            bool hung = car.GroundedWheels == 0 && v < 1.5f;                     // wheels off the ground, not flying
            bool spinning = v < 1.2f && input && input.Throttle > .3f;            // the throttle on, going nowhere
            stuck = onEnd || over || hung || spinning ? stuck + Time.fixedDeltaTime : 0f;
            if (stuck < StuckFor) { if (stuck == 0f) said = false; return; }
            bool high = race.HighFor(Player);
            if (!said)
            {
                said = true; Prompts++;
                race.SayTo(race.EntrantFor(car), KeyHints.Pad ? "Stuck? Press Y to reset" : "Stuck? Press R to reset", AutoAfter - StuckFor + 1f);
                Debug.Log($"[Race] stuck at ({car.Body.position.x:0.0}, {car.Body.position.y:0.0}, {car.Body.position.z:0.0}) {StuckFor:0} s ({(onEnd ? "on its end" : over ? "over" : hung ? "hung, no wheel down" : "throttle on, not moving")}): {(high ? "reset (HIGH)" : "reset offered")}");
            }
            if (high || stuck >= AutoAfter) { AutoResets++; Debug.Log($"[Race] stuck: reset by itself at ({car.Body.position.x:0.0}, {car.Body.position.z:0.0})"); stuck = 0f; respawn.ResetSoon(0f); }
        }
    }
}
