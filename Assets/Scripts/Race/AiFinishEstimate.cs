using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Racer
{
    // Classification only. Never changes gate/lap progress or submits a record.
    // Chastain Rush, Round 15: Racer's, copied unchanged (forest = false here).
    public sealed class AiFinishEstimate
    {
        readonly Queue<(double time, float speed)> samples = new();
        double sampledAt = -1, lastMoving = -1;
        float previousRemaining;
        public void Sample(double now, float remaining, float topSpeed, bool recovering)
        {
            if (sampledAt < 0) { sampledAt=now; previousRemaining=remaining; return; }
            double dt=now-sampledAt;
            if(dt<1) return;
            float speed=(previousRemaining-remaining)/(float)dt;
            sampledAt=now; previousRemaining=remaining;
            // Reject resets, branch transitions, reverse, and stationary/crash samples.
            if(!recovering && dt<2 && speed>=3 && speed<=topSpeed*1.15f)
            { samples.Enqueue((now,speed)); lastMoving=now; }
            while(samples.Count>0 && now-samples.Peek().time>45) samples.Dequeue();
        }
        public double Duration(double now,float remaining,VehicleProfile profile,bool forest,int difficulty)
        {
            // Conservative cruising prior: course bends/water, vehicle capability, AI skill.
            float fallback=profile.Speed*(forest?.36f:.46f)*new[]{.80f,1f,1.08f}[Mathf.Clamp(difficulty,0,2)];
            var speeds=samples.Where(s=>now-s.time<=45).Select(s=>s.speed).OrderBy(s=>s).ToArray();
            float pace=speeds.Length>=6 && now-lastMoving<5 ? speeds[speeds.Length/2] : fallback;
            pace=Mathf.Clamp(pace,fallback*.55f,profile.Speed*.9f);
            return Math.Max(.02,Math.Max(0,remaining)/pace);
        }
    }
}
