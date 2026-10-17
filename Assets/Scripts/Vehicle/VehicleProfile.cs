using UnityEngine;

namespace Racer
{
    // Stable IDs are save/record identities; changing tuning requires a rules version bump.
    public sealed class VehicleProfile
    {
        public string Id, Name, Class, Description;
        public float Speed, Acceleration, Grip, Response, Mass, Wheelbase, Pitch;
        public Vector3 Size, Camera;
        public bool Small => Class != "Car";
        public bool Motorcycle => Class == "Motorcycle";
        // 0.81: per-vehicle data that used to be keyed on the four original ids. Model = the Blender model
        // (Resources/VehicleModels), Pose = the rider pose in Rider.fbx, Seat = where a car places the Car pose (seat point H),
        // Front = the steering pivot (bike front end / ATV bars) with its axis and gain, Base = the original vehicle whose
        // per-vehicle tables (activity medal targets) this one uses. Upright / Air / Suspension / Lean: optional chassis
        // values (0 = the class default, so the original four are unchanged).
        public string Model, Pose = "Car", Base;
        public Vector3 Seat, FrontPivot, FrontAxis;
        public float FrontGain, Upright, Air, Suspension, Lean = .65f;
        // 0.88 Part D: Reward = locked until earned (VehicleUnlocks); SolidContact = holds its own against the cars in contact
        // (registered with the car contact class although it is a small vehicle for course access and handling).
        public bool Reward, SolidContact;
        // 0.99 Part E: what earns a Reward vehicle: "acorn" (the mower), "police-car" or "police-bike" (PoliceProgress)
        public string Unlock = "acorn";
        public bool IsPolice => Unlock != null && Unlock.StartsWith("police");
        // 0.106 Part C: a stunt vehicle (Unlock "stunt-..."): air steering (AirControl, ArcadeVehicle), Free Roam and the Stunt event only
        // (Chastain Rush, Round 36: never in a race or an AI roster; nothing is locked here, so all three are offered at once)
        public float AirControl;
        public bool Stunt => Unlock != null && Unlock.StartsWith("stunt");
        // 0.111 Part D: a Cup edition's livery (VehiclePaint.Livery). Chastain Rush, Round 36: only the Clay Hopper Cup (Racer's other four Cup
        // editions are championship prizes and there is no campaign here)
        public string Cup, CupBase; public Color Livery; public string[] Gold;
        public bool CupEdition => Cup != null;
        public const float CupBoost = 1.05f;
        static VehicleProfile[] all; public static VehicleProfile[] All => all ??= BuildAll(); // built on first use
        static VehicleProfile[] BuildAll() => new VehicleProfile[] {
            new() { Id="original", Name="Street Classic", Class="Car", Description="Balanced handling / high stability / strong contact", Speed=49, Acceleration=14.5f, Grip=25, Response=8, Mass=1200, Wheelbase=2.6f, Pitch=1, Size=new(1.85f,.65f,3.7f), Camera=new(0,3.6f,-7.5f),
                Model="StreetClassic", Seat=new(-.40f,.04f,-.15f) },
            new() { Id="tourer", Name="Longroof GT", Class="Car", Description="Long, fast cruiser / deliberate turn-in / strongest contact", Speed=52, Acceleration=13.5f, Grip=24, Response=6.5f, Mass=1500, Wheelbase=3, Pitch=.85f, Size=new(2,.8f,4.5f), Camera=new(0,3.8f,-8.2f),
                Model="LongroofGT", Seat=new(-.43f,.08f,-.10f) },
            new() { Id="moto", Name="Needle 600", Class="Motorcycle", Description="Fastest / quickest response / fragile on impacts and landings", Speed=61, Acceleration=18, Grip=32, Response=12, Mass=220, Wheelbase=1.65f, Pitch=1.4f, Size=new(.65f,.8f,2.25f), Camera=new(0,3,-6.5f),
                Model="Needle600", Pose="Moto", FrontPivot=new(0,-.2f,.825f), FrontAxis=new(0,.76f,-.305f), FrontGain=9 },
            new() { Id="atv", Name="Trail Four", Class="ATV", Description="Fast / responsive / planted stance / vulnerable to cars", Speed=56, Acceleration=17, Grip=30, Response=10.5f, Mass=340, Wheelbase=1.65f, Pitch=1.16f, Size=new(1.45f,.65f,2.35f), Camera=new(0,3.2f,-6.8f),
                Model="TrailFour", Pose="Atv", FrontPivot=new(0,.10f,.62f), FrontAxis=new(0,.60f,-.28f), FrontGain=14 },
            // 0.81 Part A: four more cars (Tools/Blender/cars81.py), inside the span the garage already covers.
            new() { Id="roadster", Name="Sundown Roadster", Class="Car", Description="1960s roadster, top down / light and nimble / quick steering", Speed=50, Acceleration=14.8f, Grip=26, Response=9, Mass=1000, Wheelbase=2.3f, Pitch=1.05f, Size=new(1.8f,.6f,3.9f), Camera=new(0,3.4f,-7.4f),
                Model="Roadster", Seat=new(-.36f,.02f,-.55f), Base="original" },
            new() { Id="fastback", Name="Highball Fastback", Class="Car", Description="1960s fastback / strongest acceleration / heavier steering", Speed=52, Acceleration=16, Grip=24, Response=6.8f, Mass=1400, Wheelbase=2.75f, Pitch=.9f, Size=new(2f,.75f,4.6f), Camera=new(0,3.7f,-8f),
                Model="Fastback", Seat=new(-.42f,.05f,-.20f), Base="tourer" },
            new() { Id="pebble", Name="Pebble Coupe", Class="Car", Description="1970s rear-engined compact / quickest turn-in / best car grip", Speed=49, Acceleration=14, Grip=27.5f, Response=8.8f, Mass=950, Wheelbase=2.2f, Pitch=1.1f, Size=new(1.7f,.65f,3.7f), Camera=new(0,3.4f,-7.2f),
                Model="Pebble", Seat=new(-.36f,.04f,-.05f), Base="original" },
            new() { Id="skyfin", Name="Skyfin Cruiser", Class="Car", Description="1950s finned cruiser / slow to turn / top stability and strongest contact", Speed=50.5f, Acceleration=13, Grip=23, Response=6, Mass=1750, Wheelbase=2.9f, Pitch=.8f, Size=new(2.1f,.8f,5f), Camera=new(0,3.9f,-8.6f),
                Model="Skyfin", Seat=new(-.43f,.08f,-.05f), Base="tourer" },
            // 0.81 Part B: two more motorcycles (Tools/Blender/bikes.py).
            new() { Id="scrambler", Name="Ridge Scrambler", Class="Motorcycle", Description="Dirt bike / long suspension / best on rough ground and landings / lower top speed", Speed=56, Acceleration=17.5f, Grip=31, Response=11.5f, Mass=200, Wheelbase=1.56f, Pitch=1.3f, Size=new(.7f,.9f,2.3f), Camera=new(0,3.2f,-6.6f),
                Model="Scrambler", Pose="Dirt", FrontPivot=new(0,-.16f,.80f), FrontAxis=new(0,.80f,-.33f), FrontGain=9, Base="moto", Upright=23, Air=.2f, Suspension=.75f, Lean=.6f },
            new() { Id="drifter", Name="Drifter Twin", Class="Motorcycle", Description="Cruiser / stable / strongest contact for a bike / slower to lean", Speed=58, Acceleration=16, Grip=30, Response=9.5f, Mass=320, Wheelbase=1.85f, Pitch=1.2f, Size=new(.8f,.8f,2.6f), Camera=new(0,3.1f,-7f),
                Model="Drifter", Pose="Cruiser", FrontPivot=new(0,-.20f,.95f), FrontAxis=new(0,.70f,-.42f), FrontGain=8, Base="moto", Upright=22, Air=.14f, Lean=.4f },
            // 0.88 Part D: the acorn reward (Tools/Blender/mower.py). Equal-best top speed, acceleration, grip and handling of
            // the eleven, a car's weight in contact, a strong upright and air stability so it lands flat; the cars' seated
            // pose on the centre line, hands on its wheel. Engine note: the shared engine audio pitched up (Pitch).
            new() { Id="mower", Name="Turf Rocket", Class="Mower", Description="Riding mower / the acorn reward / top speed, grip and handling, solid in contact", Speed=61, Acceleration=18, Grip=32, Response=12, Mass=760, Wheelbase=1.6f, Pitch=1.75f, Size=new(1.15f,.65f,2.1f), Camera=new(0,3.1f,-6.6f),
                Model="TurfRocket", Seat=new(0,.22f,-.52f), Base="moto", Upright=27, Air=.24f, Reward=true, SolidContact=true },
            // Chastain Rush (Round 13): Racer's two police vehicles (Police, PoliceBike) stay behind (Round 39: ported, kept out of All, below)
            // Round 36 (Part A1): Racer 0.106 Part C's two stunt vehicles (Tools/Blender/stuntbuggy.py, stuntbike.py) and 0.119 Part C's Clay
            // Hopper Cup, appended (saved choices are ids, so nothing earlier moves)
            StuntBuggy, StuntBike,
            StuntCup(MakeCup(StuntBuggy, "hoppercup", "Clay Hopper Cup", "cup-stunt", new(.86f,.4f,.06f), "chrome", "cream")),
        };
        // 0.111 Part D: a championship's prize: an existing vehicle's model and handling with its own livery (the cup's colour, gold trim and a gold
        // cup on each side, VehiclePaint.Livery) and about +5% on the five garage stats (top speed, acceleration, grip, handling, weight / contact).
        static VehicleProfile MakeCup(VehicleProfile b, string id, string name, string cup, Color livery, params string[] gold)
        {
            const float k = CupBoost;
            return new() { Id=id, Name=name, Class=b.Class, Description=b.Description.Split('/')[0].Trim()+" / championship edition: about 5% up on every stat", Speed=b.Speed*k, Acceleration=b.Acceleration*k, Grip=b.Grip*k, Response=b.Response*k, Mass=b.Mass*k,
                Wheelbase=b.Wheelbase, Pitch=b.Pitch, Size=b.Size, Camera=b.Camera, Model=b.Model, Pose=b.Pose, Base=b.Base??b.Id, Seat=b.Seat, FrontPivot=b.FrontPivot, FrontAxis=b.FrontAxis, FrontGain=b.FrontGain,
                Upright=b.Upright, Air=b.Air, Suspension=b.Suspension, Lean=b.Lean, SolidContact=b.SolidContact, Cup=cup, CupBase=b.Id, Livery=livery, Gold=gold };
        }
        static VehicleProfile StuntCup(VehicleProfile p) { p.Reward = true; p.Unlock = "stunt-cup"; p.AirControl = 1; return p; }
        public const string StuntCupId = "hoppercup";
        // a light buggy on long suspension (the ATV's family for the activity tables): open frame and roll cage, the cars' seated pose on the centre line
        public static readonly VehicleProfile StuntBuggy = new() { Id="stuntbuggy", Name="Clay Hopper", Class="Buggy", Description="Stunt buggy / long-travel suspension / steer it in the air: flips, rolls, spins", Speed=50, Acceleration=17.5f, Grip=29, Response=10.5f, Mass=520, Wheelbase=2.1f, Pitch=1.25f, Size=new(1.5f,.7f,2.8f), Camera=new(0,3.4f,-7f),
                Model="StuntBuggy", Seat=new(0,.06f,-.28f), Base="atv", Upright=24, Air=.16f, Suspension=.85f, Reward=true, Unlock="stunt-buggy", AirControl=1 };
        // a freestyle dirt bike on the Ridge Scrambler's frame and handling family, the Dirt rider pose
        public static readonly VehicleProfile StuntBike = new() { Id="stuntbike", Name="Skywhip 250", Class="Motorcycle", Description="Stunt motorcycle / freestyle dirt bike / steer it in the air: flips, rolls, spins", Speed=55, Acceleration=18, Grip=31, Response=11.5f, Mass=190, Wheelbase=1.56f, Pitch=1.35f, Size=new(.7f,.9f,2.3f), Camera=new(0,3.2f,-6.6f),
                Model="StuntBike", Pose="Dirt", FrontPivot=new(0,-.16f,.80f), FrontAxis=new(0,.80f,-.33f), FrontGain=9, Base="moto", Upright=23, Air=.2f, Suspension=.75f, Lean=.6f, Reward=true, Unlock="stunt-gold", AirControl=1 };
        // Round 39 (Part D): Racer 0.94's patrol car (Tools/Blender/police.py) and 0.99's police motorcycle (Tools/Blender/policebike.py): the police modes'
        // vehicles only (the cops, the player in Speed Patrol and as the cop in Cop vs Runner), kept out of All so never in the garage, races or rosters
        public static readonly VehicleProfile Police = new() { Id="police", Name="Patrol Car", Class="Car", Description="Police only / fast and planted / strong contact", Speed=55, Acceleration=16.5f, Grip=26, Response=8.2f, Mass=1550, Wheelbase=2.8f, Pitch=.9f, Size=new(2,.75f,4.7f), Camera=new(0,3.8f,-8.2f),
                Model="PatrolCar", Seat=new(-.42f,.05f,-.15f), Base="tourer", Reward=true, Unlock="police-car" };
        public static readonly VehicleProfile PoliceBike = new() { Id="policebike", Name="Patrol Cycle", Class="Motorcycle", Description="Police motorcycle / Needle 600 family / quick and nimble / fragile on impacts", Speed=58, Acceleration=17.5f, Grip=31, Response=11.5f, Mass=240, Wheelbase=1.65f, Pitch=1.4f, Size=new(.65f,.8f,2.25f), Camera=new(0,3,-6.5f),
                Model="PoliceBike", Pose="Moto", FrontPivot=new(0,-.2f,.825f), FrontAxis=new(0,.76f,-.305f), FrontGain=9, Base="moto", Reward=true, Unlock="police-bike" };
        public static VehicleProfile Find(string id)
        {
            if(id==Police.Id) return Police; if(id==PoliceBike.Id) return PoliceBike;   // Round 39
            foreach(var profile in All) if(profile.Id==id) return profile;
            return All[0];
        }
        public static int IndexOf(string id) => System.Array.FindIndex(All, p => p.Id == id);
    }
}
