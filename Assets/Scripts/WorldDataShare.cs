using UnityEngine;

namespace ChastainRush
{
    // a data scene's only object: the assets it hands to its own sharedassets file (never loaded in the game)
    public sealed class WorldDataShare : MonoBehaviour
    {
        public UnityEngine.Object[] assets;
    }
}
