using UnityEngine;
using UnityEngine.UI;

namespace ChastainRush
{
    // Round 51: the written "(c) OpenStreetMap contributors" over the cover (LoadingScreen.AddCover) shows only when the screen is
    // not the cover's shape, so the crop may have taken the painted credit off the edge; at 16:9 the cover's own credit is seen.
    public sealed class CreditWhenCropped : MonoBehaviour
    {
        public float aspect = 16f / 9f;
        Text text;
        void Awake() { text = GetComponent<Text>(); }
        void Update() { if (text) text.enabled = Mathf.Abs(Screen.width / (float)Mathf.Max(1, Screen.height) / aspect - 1f) > .01f; }
    }
}
