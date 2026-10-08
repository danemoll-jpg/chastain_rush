using UnityEngine;

namespace ChastainRush
{
    // OpenStreetMap's licence (ODbL) requires a visible credit wherever the map is shown.
    public class MapCredits : MonoBehaviour
    {
        public string text = "© OpenStreetMap contributors";
        GUIStyle style;

        void OnGUI()
        {
            if (style == null)
                style = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.LowerRight, normal = { textColor = Color.white } };
            var r = new Rect(0f, 0f, Screen.width - 10f, Screen.height - 6f);
            style.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
            GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, style);
            style.normal.textColor = Color.white;
            GUI.Label(r, text, style);
        }
    }
}
