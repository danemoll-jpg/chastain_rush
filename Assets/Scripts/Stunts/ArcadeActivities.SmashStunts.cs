using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 38 (Part B; Dan: "Smashing through things should count for your stunts", 21:37: "fuel pumps should also count"): wherever tricks
    // score (Free Roam and the Stunt event, every vehicle), breaking something is a stunt:
    //   on the ground it links the combo like a trick (a new link, or, within SmashBurst of the last smash link, more points on that same link, so a run
    //     through a row of windows is one link and not nine), and it keeps the chain going (the 5 s gap starts again);
    //   in the air it is a trick type of its own, SMASH, for the flight's multiplier (a backflip through a window: BACKFLIP + SMASH x2), its points
    //     added to the flight's tricks; a bail loses them with the flight's other tricks.
    // Shown on the trick panel ("SMASH: STORE WINDOW  +150"). The Free Roam smash runs keep their own count (ArcadeActivities.Smash). The points, bigger
    // things more (decided here; Dan to judge in play): see Value.
    public sealed partial class ArcadeActivities
    {
        public const float SmashBurst = 1.2f;   // smashes this close together on the ground are one link
        // what a broken panel is and what it scores
        public static (string name, int points) Value(FenceChunk chunk)
        {
            if (!chunk) return ("SMASH", 50);
            if (chunk.GetComponent<PumpChunk>()) return ("GAS PUMP", 600);      // with its explosion: among the biggest
            if (chunk.GetComponent<HydrantChunk>()) return ("HYDRANT", 200);
            string n = chunk.name;
            if (n.StartsWith("StuntParkVaultCash")) return ("CASH CART", 300);    // Part C: in the vault
            if (n.StartsWith("StuntParkVault")) return ("VAULT DOOR", 1000);     // Part C: the bank's vault door, the biggest
            if (n.StartsWith("StuntParkShopCounters")) return ("COUNTER", 120);
            if (n.StartsWith("StuntParkAppliances")) return ("WASHER", 100);
            if (n.StartsWith("StuntParkWindows")) return ("STORE WINDOW", 150);
            if (n.StartsWith("StuntParkBankBoards")) return ("BOARDED DOOR", 150);
            if (n.StartsWith("StuntParkCheckouts")) return ("CHECKOUT", 150);
            if (n.StartsWith("StuntParkTellers")) return ("TELLER COUNTER", 200);
            if (n.StartsWith("StuntParkPartitions")) return ("SHOP WALL", 100);
            if (n.StartsWith("StuntParkShelves")) return ("SHELF", 120);
            if (n.StartsWith("StuntParkFurniture")) return ("FURNITURE", 60);
            if (n.StartsWith("StuntParkDisplays")) return ("DISPLAY", 80);
            if (n.StartsWith("StuntParkLights")) return ("FLOODLIGHT", 150);
            if (n.StartsWith("Fence_")) return ("FENCE", 25);
            if (n.Contains("PoolFurniture")) return ("PATIO FURNITURE", 50);
            switch (chunk.sound)
            {
                case BreakSounds.Kind.Mailbox: return ("MAILBOX", 75);
                case BreakSounds.Kind.Lamp: return ("STREET LAMP", 100);
                case BreakSounds.Kind.Sign: return ("SIGN", 75);
                case BreakSounds.Kind.Hydrant: return ("HYDRANT", 200);
                case BreakSounds.Kind.Glass: return ("WINDOW", 150);
                case BreakSounds.Kind.Iron: return ("RAILING", 50);
            }
            return ("SMASH", 50);
        }
        public const int TreePoints = 250;

        // the flight's smashes (SMASH, a trick type)
        int sSmashPoints, sSmashes; string sSmashName = "";
        // the last smash link on the ground (a burst merges into it)
        float smashLinkAt = -10f; int smashLinkCount; string smashLinkName = "";
        public int Smashes { get; private set; }   // smashes scored this session (checks)

        void SmashReset() { sSmashPoints = sSmashes = 0; sSmashName = ""; }
        // a smash in the last moment before a takeoff (crashing out through a window off a kicker: the nose meets the glass as the wheels leave)
        // belongs to the flight: taken back off the ground combo and made the flight's SMASH
        public const float TakeoffGrace = .3f;
        float groundSmashAt = -10f; int groundSmashPoints; string groundSmashName = ""; bool groundSmashNewLink;
        void TakeOff()
        {
            SmashReset();
            if (Time.time - groundSmashAt > TakeoffGrace || groundSmashPoints <= 0) return;
            ComboPoints = Mathf.Max(0, ComboPoints - groundSmashPoints);
            if (groundSmashNewLink && ComboLinks > 0) { ComboLinks--; if (comboTricks.Count > 0) comboTricks.RemoveAt(comboTricks.Count - 1); } else smashLinkCount = Mathf.Max(0, smashLinkCount - 1);
            sSmashPoints = groundSmashPoints; sSmashes = 1; sSmashName = groundSmashName; groundSmashPoints = 0; groundSmashAt = -10f;
        }

        // a panel broken by this vehicle, or a tree it snapped
        void SmashStunt(string name, int points)
        {
            if (!StuntsOn || !car) return;
            Smashes++;
            if (sFlying)
            {
                sSmashPoints += points; sSmashes++; sSmashName = name;
                Result($"SMASH: {name}  +{points:N0}", 1);
                return;
            }
            bool inPark = StuntPark.Current && StuntPark.OnPark(car.Body.position);
            if (ComboLinks > 0 && Time.time - smashLinkAt < SmashBurst && comboTricks.Count > 0 && comboTricks[comboTricks.Count - 1] == "SM")
            {
                // the same burst: more points on the same link
                smashLinkCount++; smashLinkAt = Time.time; ComboPoints += points; ComboInPark &= inPark; sSince = 0;
                groundSmashAt = Time.time; groundSmashPoints = points; groundSmashName = name; groundSmashNewLink = false;
                string line = smashLinkCount > 1 ? $"SMASH x{smashLinkCount}: {name}" : "SMASH: " + name;
                LastStunt = line; LastStuntPoints += points; Result($"{line}  +{points:N0}", 1);
                StuntSay($"{line} / +{points:N0}" + (ComboLinks > 1 ? $"\nCOMBO x{ComboLinks} · {ComboTotal:N0}" : ""), 2.5f);
                return;
            }
            smashLinkAt = Time.time; smashLinkCount = 1; smashLinkName = name;
            groundSmashAt = Time.time; groundSmashPoints = points; groundSmashName = name; groundSmashNewLink = true;
            Link(points, "SMASH: " + name, inPark, "SM");
            Result($"SMASH: {name}  +{points:N0}", 1);
        }
        void SmashPanel(FenceChunk chunk, int panel, ArcadeVehicle source) { if (source != car) return; var v = Value(chunk); SmashStunt(v.name, v.points); }
        void SmashTree(ArcadeVehicle source, Vector3 at) { if (source != car) return; SmashStunt("TREE", TreePoints); }
    }
}
