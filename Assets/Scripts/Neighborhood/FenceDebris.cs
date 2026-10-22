using UnityEngine;

namespace ChastainRush
{
    // Round 61 (C): steps the breakables' flying pieces (FenceChunk.StepDebris), one object for the whole map, made when the first piece flies
    public sealed class FenceDebris : MonoBehaviour
    {
        static FenceDebris runner;
        public static void Ensure()
        {
            if (runner) return;
            var go = new GameObject("Fence debris runner"); runner = go.AddComponent<FenceDebris>();
        }
        void Update() { FenceChunk.StepDebris(Time.deltaTime); }
    }
}
