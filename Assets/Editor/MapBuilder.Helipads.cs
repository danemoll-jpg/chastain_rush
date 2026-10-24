using UnityEngine;

// Round 63 (Dan: "a helipad at Cobb County International Airport and one on Wellstar Kennestone's roof (it has a real one), each with a parked
// helicopter to take"): the two pads, where OpenStreetMap has them (raw/osm_ext2.json, aeroway=helipad: the airport's six pads on the apron by
// the hangars, way 1409772186 the north-east one; Kennestone's rooftop pad, way 1076463453, layer 4, on hospital building 7877). World: a pad is
// a real thing (the painted square, its yellow circle and white H; on the roof a raised deck with a collider, so the roof's own shape under it
// does not matter); the parked helicopters and the swap are the game layer (Vehicle/HelipadStands.cs), placed here from the same list.
// The height: the first solid thing under the pad in the built scene (the apron, the roof), so this step runs after Buildings, Businesses, Airport.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly (string id, string title, Vector2 at, float yaw, float size, bool roof)[] HelipadList =
        {
            ("helipad-airport", "Airport helipad", new Vector2(-3591.0f, -2057.6f), 0f, 14f, false),
            ("helipad-kennestone", "Kennestone helipad", new Vector2(1735.0f, -6709.3f), 0f, 17.8f, true),
        };

        static void BuildHelipads(Materials mats, Transform world)
        {
            Physics.SyncTransforms();
            var root = new GameObject("Helipads").transform; root.SetParent(world, false);
            var stands = new GameObject("Helipad stands").AddComponent<HelipadStands>();   // game layer, its own root
            var pads = new System.Collections.Generic.List<HelipadStands.Pad>();
            foreach (var (id, title, at, yaw, size, roof) in HelipadList)
            {
                float half = size * .5f, top = float.MinValue, low = float.MaxValue; string under = "";
                for (int i = -2; i <= 2; i++)
                    for (int j = -2; j <= 2; j++)
                    {
                        var p = new Vector3(at.x + i * half * .5f, 3000f, at.y + j * half * .5f);
                        if (!Physics.Raycast(p, Vector3.down, out var hit, 6000f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        top = Mathf.Max(top, hit.point.y); low = Mathf.Min(low, hit.point.y); if (i == 0 && j == 0) under = hit.collider.name;
                    }
                if (top == float.MinValue) { Debug.LogWarning($"[Round63] {title}: nothing under ({at.x}, {at.y}); not built"); continue; }
                var f = MakeFrame(new Vector3(at.x, 0, at.y), new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad)), 1f);
                var deck = new MeshBuilder(); var paint = new MeshBuilder();
                float surface;
                if (roof)
                {
                    // a deck on short legs over the roof: its top 0.4 m over the roof's highest point under it, a skirt down to the lowest
                    surface = top + .4f; f.O.y = surface;
                    BoxF(deck, f, -half, half, -(surface - low) - .1f, 0f, -half, half, new Color(.55f, .56f, .57f));
                    BoxF(deck, f, -half - .25f, half + .25f, -.12f, .05f, -half - .25f, -half, new Color(.85f, .45f, .10f));   // the safety rail's base, all four sides
                    BoxF(deck, f, -half - .25f, half + .25f, -.12f, .05f, half, half + .25f, new Color(.85f, .45f, .10f));
                    BoxF(deck, f, -half - .25f, -half, -.12f, .05f, -half, half, new Color(.85f, .45f, .10f));
                    BoxF(deck, f, half, half + .25f, -.12f, .05f, -half, half, new Color(.85f, .45f, .10f));
                }
                else surface = top; f.O.y = surface;
                // the paint, 2 cm over the surface: a white edge, the yellow circle, the white H (the airport's: a dark square under them on the apron)
                float y = roof ? .02f : .05f * vScale + .03f;   // the apron's pavement is 0.05 m (scaled) over the ground (MapBuilder.Airport.cs), its lines 0.015 m more
                if (!roof) Flat(paint, f, -half, half, -half, half, y - .01f, new Color(.30f, .31f, .32f));
                float e = .35f; var white = new Color(.93f, .93f, .90f); var yellow = new Color(.95f, .78f, .12f);
                Flat(paint, f, -half, half, half - e, half, y, white); Flat(paint, f, -half, half, -half, -half + e, y, white);
                Flat(paint, f, -half, -half + e, -half, half, y, white); Flat(paint, f, half - e, half, -half, half, y, white);
                float r1 = half * .78f, r0 = r1 - .5f; paint.Tint = yellow;
                for (int k = 0; k < 48; k++)
                {
                    float a0 = k * Mathf.PI * 2 / 48, a1 = (k + 1) * Mathf.PI * 2 / 48;
                    var q0 = f.P(Mathf.Cos(a0) * r0, y + .002f, Mathf.Sin(a0) * r0); var q1 = f.P(Mathf.Cos(a0) * r1, y + .002f, Mathf.Sin(a0) * r1);
                    var q2 = f.P(Mathf.Cos(a1) * r1, y + .002f, Mathf.Sin(a1) * r1); var q3 = f.P(Mathf.Cos(a1) * r0, y + .002f, Mathf.Sin(a1) * r0);
                    int b = paint.Vertex(q0); paint.Vertex(q1); paint.Vertex(q2); paint.Vertex(q3); TriUp(paint, b, b + 1, b + 2); TriUp(paint, b, b + 2, b + 3);
                }
                float hh = half * .42f, hw = half * .30f, bar = half * .09f;
                Flat(paint, f, -hw, -hw + bar * 1.6f, -hh, hh, y + .004f, white); Flat(paint, f, hw - bar * 1.6f, hw, -hh, hh, y + .004f, white); Flat(paint, f, -hw, hw, -bar * .8f, bar * .8f, y + .004f, white);
                var parent = new GameObject("Helipad " + title).transform; parent.SetParent(root, false);
                if (roof) deck.ToObject($"Helipad deck {id}", mats.Concrete, parent, true);
                paint.ToObject($"Helipad paint {id}", mats.Paint, parent, false, false);
                pads.Add(new HelipadStands.Pad { id = id, title = title, centre = new Vector3(at.x, surface + (roof ? 0f : .05f * vScale), at.y), yaw = yaw, half = half });
                Debug.Log($"[Round63] {title}: at ({at.x:0.0}, {surface:0.00}, {at.y:0.0}), {size:0.0} m{(roof ? $", a deck {surface - low:0.0} m over the roof's lowest point under it" : "")}, on {under}");
            }
            stands.pads = pads.ToArray();
        }

        // a flat quad in the frame (u0..u1 across, v0..v1 along) at height y over the frame's origin, facing up
        static void Flat(MeshBuilder mb, Frame f, float u0, float u1, float v0, float v1, float y, Color c)
        {
            mb.Tint = c; int b = mb.Vertex(f.P(u0, y, v0)); mb.Vertex(f.P(u1, y, v0)); mb.Vertex(f.P(u1, y, v1)); mb.Vertex(f.P(u0, y, v1));
            TriUp(mb, b, b + 1, b + 2); TriUp(mb, b, b + 2, b + 3);
        }
    }
}
