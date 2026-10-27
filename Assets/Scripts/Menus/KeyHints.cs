using Racer;

namespace ChastainRush
{
    // Round 74 (B, Dan: "on the map screen (and in general) can we just show controls for the device being played with and not also include keyboard
    // buttons unless these buttons are only on the keyboard"): every control hint in the game asks this which device is in use: the one that last
    // gave a deliberate input (Racer's MenuInput: a button on a pad, a key, a mouse move or click; it switches at once). Pad: the controller's
    // buttons; keyboard / mouse: the keys. A function only on the keyboard (F-keys, the debug keys) is shown with the keyboard only.
    public static class KeyHints
    {
        public static bool Pad => MenuInput.Controller;
        public static string Of(string pad, string keys) => Pad ? pad : keys;
    }
}
