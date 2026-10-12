using UnityEngine;
namespace Racer
{
    // Judgment errors affect route targets and braking anticipation only. No body manipulation.
    // Chastain Rush, Round 15: Racer's, copied; the difficulty comes from RoadDriver.Difficulty (no RaceDirector).
    // Chastain Rush, Round 21 (Dan: "AI should never drive perfectly 100% of the time"): two more bend mistakes besides Racer's wide
    // line and early braking: late braking (it carries too much speed into the bend and runs wide) and, on roads only, a drift onto
    // the curb on a straight (it clips a mailbox, lamp or hydrant there: they break). And a mistake at a jump (a route's ai_zones):
    // short (the run-up a little slow: it comes up short) or off line (it leaves the lip to one side). All more often on Easy than
    // Hard; none on a shortcut (R1 has its own sloppy try) and none that can leave it stuck (RoadDriver keeps the lines in bounds).
    public sealed class DriverVariation
    {
        public static bool Disabled;
        public static int Seed;
        System.Random random;
        float next, start, duration, strength, sign;
        int kind;
        public int Events {get;private set;}
        // Chastain Rush, Round 21: per kind, for the race checks' report
        public static readonly string[] Kinds={"wide line","early braking","jump off line","late braking","short jump","curb clip"};
        public readonly int[] Counts=new int[6];
        public int Kind=>kind;
        public string EventName=>Kinds[Mathf.Clamp(kind,0,Kinds.Length-1)];
        public float Weight {get;private set;}
        public float Line {get;private set;}
        public float Judgment {get;private set;}=1;
        public bool Curb=>duration>0&&kind==5&&Weight>0;   // Chastain Rush, Round 21: Line may take it past the lane's usual bounds, to the curb
        public void Initialize(int seed,int driver)
        {random=new System.Random(unchecked(seed+driver*7919));next=Time.time+12+(float)random.NextDouble()*24;System.Array.Clear(Counts,0,Counts.Length);}
        public void Step(RoadDriver driver,float station,float speed,bool protectedArea,bool jump,bool trail=false)
        {
            Line=0;Judgment=1;Weight=0;
            if(Disabled||random==null||protectedArea){duration=0;return;}
            int skill=Mathf.Clamp(RoadDriver.Difficulty,0,2);   // Chastain Rush: no RaceDirector
            if(Time.time>=next && speed>12 && driver.Car.GroundedWheels>=2 && driver.Car.WaterImmersion==0 && !jump)
            {
                var road=driver.DriveRoad;road.At(station+12,out var a);road.At(station+42,out var b);
                float turn=Vector3.SignedAngle(a,b,Vector3.up);
                bool bend=Mathf.Abs(turn)>5 && Mathf.Abs(turn)<55, straight=!trail&&Mathf.Abs(turn)<3;
                if(bend || (straight&&random.NextDouble()<.35))
                {
                    // Chastain Rush, Round 21: on a bend Racer's two plus late braking; on a road's straight, the curb
                    kind=bend?new[]{0,1,3}[random.Next(3)]:5;start=Time.time;duration=skill==0?3.2f:skill==1?2.6f:2;
                    strength=(skill==0?1:skill==1?.65f:.4f)*Mathf.Lerp(.7f,1,(float)random.NextDouble());
                    if(trail&&kind==3)strength*=.5f;   // a trail has trunks beside it: running wide there is a smaller error
                    sign=kind==5?Mathf.Sign(driver.Lane==0?1:driver.Lane):-Mathf.Sign(turn);
                    next=Time.time+(skill==0?18:skill==1?30:48)+(float)random.NextDouble()*25;Events++;Counts[kind]++;
                    if(!Application.isBatchMode)Debug.Log($"AI_JUDGMENT seed={Seed} driver={driver.name} event={Events} type={EventName} station={station:F1} speed={speed:F2}");
                }
            }
            if(duration<=0||Time.time-start>=duration)return;
            Weight=Mathf.Sin(Mathf.PI*(Time.time-start)/duration)*strength;
            if(kind==1)Judgment=1-.30f*Weight;
            else if(kind==3)Judgment=1+.45f*Weight;   // Chastain Rush, Round 21: brakes late, so runs wide
            else if(kind==5)Line=sign*Weight;         // fraction of the way from its lane to the curb (RoadDriver)
            else Line=sign*Weight*(kind==2?.28f:.65f);
        }

        // Chastain Rush, Round 21: a mistake at a jump, rolled once as the driver comes up to it (RoadDriver: within 90 m of an
        // ai_zone). 0: none; 4: short (shortOk: a zone where coming up short is safe); 2: off line. Returns the side (+1 / -1) too.
        public int RollJump(bool shortOk,out float side)
        {
            side=0;
            if(Disabled||random==null)return 0;
            int skill=Mathf.Clamp(RoadDriver.Difficulty,0,2);
            if(random.NextDouble()>=(skill==0?.16:skill==1?.09:.045))return 0;
            int k=shortOk&&random.Next(2)==0?4:2; side=random.Next(2)==0?-1:1;
            Events++;Counts[k]++;
            if(!Application.isBatchMode)Debug.Log($"AI_JUDGMENT seed={Seed} event={Events} type={Kinds[k]}");
            return k;
        }
        public void CountSlip(){Counts[4]++;Events++;}   // R1's sloppy try is a short jump too
    }
}
