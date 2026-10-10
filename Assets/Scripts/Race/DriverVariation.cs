using UnityEngine;
namespace Racer
{
    // Judgment errors affect route targets and braking anticipation only. No body manipulation.
    // Chastain Rush, Round 15: Racer's, copied; the difficulty comes from RoadDriver.Difficulty (no RaceDirector).
    public sealed class DriverVariation
    {
        public static bool Disabled;
        public static int Seed;
        System.Random random;
        float next, start, duration, strength, sign;
        int kind;
        public int Events {get;private set;}
        public string EventName=>kind==0?"wide line":kind==1?"early braking":"jump alignment";
        public float Weight {get;private set;}
        public float Line {get;private set;}
        public float Judgment {get;private set;}=1;
        public void Initialize(int seed,int driver)
        {random=new System.Random(unchecked(seed+driver*7919));next=Time.time+12+(float)random.NextDouble()*24;}
        public void Step(RoadDriver driver,float station,float speed,bool protectedArea,bool jump)
        {
            Line=0;Judgment=1;Weight=0;
            if(Disabled||random==null||protectedArea){duration=0;return;}
            int skill=Mathf.Clamp(RoadDriver.Difficulty,0,2);   // Chastain Rush: no RaceDirector
            if(Time.time>=next && speed>12 && driver.Car.GroundedWheels>=2 && driver.Car.WaterImmersion==0)
            {
                var road=driver.DriveRoad;road.At(station+12,out var a);road.At(station+42,out var b);
                float turn=Vector3.SignedAngle(a,b,Vector3.up);
                if(Mathf.Abs(turn)>5 && Mathf.Abs(turn)<55 || jump)
                {
                    kind=jump?2:random.Next(2);start=Time.time;duration=skill==0?3.2f:skill==1?2.6f:2;
                    strength=(skill==0?1:skill==1?.65f:.4f)*Mathf.Lerp(.7f,1,(float)random.NextDouble());
                    sign=jump?(random.Next(2)==0?-1:1):-Mathf.Sign(turn);
                    next=Time.time+(skill==0?18:skill==1?30:48)+(float)random.NextDouble()*25;Events++;
                    if(!Application.isBatchMode)Debug.Log($"AI_JUDGMENT seed={Seed} driver={driver.name} event={Events} type={EventName} station={station:F1} speed={speed:F2}");
                }
            }
            if(duration<=0||Time.time-start>=duration)return;
            Weight=Mathf.Sin(Mathf.PI*(Time.time-start)/duration)*strength;
            if(kind==1)Judgment=1-.30f*Weight;
            else Line=sign*Weight*(kind==2?.28f:.65f);
        }
    }
}
