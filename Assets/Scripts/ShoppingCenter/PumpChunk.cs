using UnityEngine;

namespace ChastainRush
{
    // Round 17: marks a FenceChunk whose panels are gas pumps, so breaking one sets off its explosion (PumpBlast). A
    // MonoBehaviour in a file of its own name, or a player build loses it (Round 8 note in FencePanel.cs).
    public sealed class PumpChunk : MonoBehaviour { }
}
