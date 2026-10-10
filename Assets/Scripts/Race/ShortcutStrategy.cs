using System;
using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    // Only route selection: no pace, forces, checkpoint or entitlement changes.
    // Chastain Rush, Round 15: Racer's, copied; its WoodlandRoute (a shortcut branch) is AiShortcut here.
    public sealed class ShortcutStrategy
    {
        readonly Dictionary<AiShortcut,int> decisions=new();
        System.Random random;
        public float Risk { get; private set; }
        public int Decisions { get; private set; }
        public string Diagnostic { get; private set; }
        public void Initialize(int seed)
        {
            random=new System.Random(seed);Risk=((float)random.NextDouble()*2-1)*.04f;
            decisions.Clear();Decisions=0;
        }
        public float Probability(float rank,float gap)
            => Mathf.Clamp(.12f+.34f*Mathf.Clamp01(rank)+.12f*Mathf.Clamp01(gap/200)+Risk,.06f,.65f);
        public bool Decided(AiShortcut route,int lap)=>decisions.TryGetValue(route,out int decidedLap)&&decidedLap==lap;
        public bool TryDecide(AiShortcut route,int lap,float rank,float gap,out bool shortcut)
        {
            shortcut=false;
            if(Decided(route,lap))return false;
            decisions[route]=lap;float probability=Probability(rank,gap);float roll=(float)random.NextDouble();
            shortcut=roll<probability;Decisions++;
            Diagnostic=$"route={route.Name}; lap={lap+1}; risk={Risk:F3}; rank={rank:F2}; gap={gap:F1}; probability={probability:F3}; roll={roll:F3}; choice={(shortcut?"shortcut":"main")}";
            return true;
        }
    }
}
