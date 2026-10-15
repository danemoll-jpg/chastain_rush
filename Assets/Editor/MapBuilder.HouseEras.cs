using UnityEngine;

// Round 30 (Part B): four more house styles for the neighborhoods Dan has no styles for, drawn with the same kit as Chastain
// Glen's (MapBuilder.Houses.cs: walls, gables, windows, doors, garage doors, trim, one vertex-colored mesh, details in the
// shadowless detail mesh). Each is built in the house frame (u along the facade, v toward the street, y up from the floor).
//   E  1970s split-level: a one-story wing beside a taller split block with the garage in its lower level, brick below
//   F  2000s craftsman: two stories, a stone wainscot, a front-gable garage with carriage doors, a covered porch on stone
//      piers with tapered columns, shingles in the gables
//   G  townhome row: two-story units side by side under one roof, each with its own door, stoop and (in most rows) a one-car
//      garage, the fronts alternating brick and siding, a small gable over every other unit
//   H  1970s ranch with a carport: one low story under a shallow roof that runs on over an open carport (posts, a storage
//      closet at its back), a brick wainscot or all brick, a big picture window
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // -- style E: split-level ----------------------------------------------------

        static float SplitBlockWidth(HouseSpec h) => Mathf.Clamp(h.W * 0.46f, 6.2f, 8.4f);

        static void BuildE(MeshBuilder mb, HouseSpec h)
        {
            var d = h.detail ?? mb;
            var f = h.frame; float W = h.W, D = h.D;
            float yb = h.baseY - h.floorY;
            float bw = SplitBlockWidth(h), s = -W / 2 + bw;                       // the split block: -W/2 .. s
            h.garageU0 = -W / 2; h.garageU1 = -W / 2 + Mathf.Min(bw - 0.6f, 5.8f);
            float hWing = 2.7f, hBlock = 5.0f, wingFront = D / 2 - 0.6f;
            Color low = h.brick ? h.brickC : h.sidingC;
            // the one-story wing, a shallow side gable, set back a little
            BoxNoBottom(mb, f, s, W / 2, yb, hWing, -D / 2 + 0.4f, wingFront, h.sidingC, h.sidingC, h.sidingC);
            Gable(mb, f, s - 0.2f, W / 2, -D / 2 + 0.4f, wingFront, true, hWing, (D - 1f) / 2 * 0.32f, 0.6f, 0.45f, h.roofC, h.sidingC, false, true, h.trimC);
            if (h.brick) BoxF(mb, f, s, W / 2, yb, 0.95f, wingFront, wingFront + 0.08f, h.brickC);   // a brick wainscot under the windows
            // the split block: brick lower level (the garage), siding upper level, its own side gable a little higher
            BoxNoBottom(mb, f, -W / 2, s, yb, 2.45f, -D / 2, D / 2, low, low, low);
            BoxNoBottom(mb, f, -W / 2, s, 2.45f, hBlock, -D / 2, D / 2 + 0.35f, h.sidingC, h.sidingC, h.sidingC);   // the upper level hangs over the lower a little
            BoxF(mb, f, -W / 2 - 0.02f, s + 0.02f, 2.38f, 2.5f, D / 2 - 0.02f, D / 2 + 0.4f, h.trimC);
            Gable(mb, f, -W / 2, s, -D / 2, D / 2 + 0.35f, true, hBlock, (D + 0.35f) / 2 * 0.36f, 0.55f, 0.4f, h.roofC, h.sidingC, true, true, h.trimC);
            int doors = h.garageU1 - h.garageU0 > 5.2f ? 2 : 1;
            GarageDoors(d, f, h.garageU0 + 0.35f, h.garageU1 - 0.2f, doors, D / 2, h);
            // upper level windows over the garage, the entry in the wing with a picture window
            float bc = (-W / 2 + s) / 2;
            WindowF(d, f, bc - 1.25f, 3.75f, 1.2f, 1.2f, D / 2 + 0.35f, h, true);
            WindowF(d, f, bc + 1.25f, 3.75f, 1.2f, 1.2f, D / 2 + 0.35f, h, true);
            float doorU = s + 1.3f;
            FrontDoor(d, f, doorU, wingFront, h, false, false);
            float pw = Mathf.Min(3.0f, W / 2 - doorU - 1.6f);
            if (pw > 1.4f) WindowF(d, f, doorU + 1.0f + pw / 2 + 0.4f, 1.5f, pw, 1.35f, wingFront, h, true, false);
            if (h.chimney) Chimney(mb, f, W / 2 - 1.0f, 0f, hWing - 0.5f, hWing + 2.2f, h);
        }

        // -- style F: craftsman -----------------------------------------------------

        static void BuildF(MeshBuilder mb, HouseSpec h)
        {
            var d = h.detail ?? mb;
            var f = h.frame; float W = h.W, D = h.D;
            float yb = h.baseY - h.floorY;
            float proj = 1.6f, gW = Mathf.Clamp(W * 0.42f, 5.8f, 6.6f);
            float gU0 = -W / 2, gU1 = -W / 2 + gW;
            h.garageU0 = gU0; h.garageU1 = gU1;
            float hE = 2 * StoreyH - 0.05f, hG = 3.3f, vFront = D / 2 - proj;
            float hbM = (vFront + D / 2) / 2f, riseM = (vFront + D / 2) / 2f * 0.62f;
            Color stone = h.stoneC, shake = Shade(h.sidingC, 0.82f);
            // main two-story body, a steeper side gable than Glen's
            BoxNoBottom(mb, f, gU0, W / 2, yb, hE, -D / 2, vFront, h.sidingC, h.sidingC, h.sidingC);
            Gable(mb, f, gU0, W / 2, -D / 2, vFront, true, hE, riseM, 0.6f, 0.45f, h.roofC, shake, true, true, h.trimC);
            // the garage block forward, a steep front gable with shingles in it, stone piers either side of the doors
            BoxNoBottom(mb, f, gU0, gU1, yb, hG, vFront, D / 2, h.sidingC, h.sidingC, h.sidingC);
            Gable(mb, f, vFront - 1.2f, D / 2, gU0, gU1, false, hG, gW / 2 * 0.95f, 0.35f, 0.4f, h.roofC, shake, false, true, h.trimC);
            BoxF(mb, f, gU0 - 0.06f, gU0 + 0.45f, yb, 1.05f, D / 2 - 0.1f, D / 2 + 0.12f, stone);
            BoxF(mb, f, gU1 - 0.45f, gU1 + 0.06f, yb, 1.05f, D / 2 - 0.1f, D / 2 + 0.12f, stone);
            GarageDoors(d, f, gU0 + 0.5f, gU1 - 0.5f, h.garageBays, D / 2, h);
            FrontQuad(d, f, (gU0 + gU1) / 2 - 0.5f, (gU0 + gU1) / 2 + 0.5f, hG + 0.9f, hG + 1.5f, D / 2 + 0.03f, h.trimC);   // a gable vent
            // stone wainscot along the main front
            BoxF(mb, f, gU1, W / 2, yb, 1.0f, vFront - 0.02f, vFront + 0.1f, stone);
            BoxF(mb, f, gU1, W / 2, 1.0f, 1.08f, vFront - 0.02f, vFront + 0.14f, Shade(stone, 1.15f));
            // the covered porch: stone piers, tapered columns, a front gable over it
            float pu0 = gU1 + 0.25f, pu1 = Mathf.Min(W / 2 - 0.2f, pu0 + Mathf.Max(3.6f, (W / 2 - gU1) * 0.85f)), pd = 2.1f;
            BoxF(mb, f, pu0, pu1, -0.2f, 0.12f, vFront, vFront + pd, new Color(0.62f, 0.60f, 0.56f));   // the porch floor
            int cols = pu1 - pu0 > 6f ? 3 : 2;
            for (int k = 0; k < cols; k++)
            {
                float uc = Mathf.Lerp(pu0 + 0.35f, pu1 - 0.35f, cols == 1 ? .5f : k / (float)(cols - 1)), vc = vFront + pd - 0.35f;
                BoxF(mb, f, uc - 0.32f, uc + 0.32f, 0.12f, 1.0f, vc - 0.32f, vc + 0.32f, stone);
                BoxF(mb, f, uc - 0.2f, uc + 0.2f, 1.0f, 1.9f, vc - 0.2f, vc + 0.2f, h.trimC);
                BoxF(mb, f, uc - 0.15f, uc + 0.15f, 1.9f, 2.65f, vc - 0.15f, vc + 0.15f, h.trimC);
            }
            BoxF(mb, f, pu0, pu1, 2.65f, 2.85f, vFront + pd - 0.55f, vFront + pd - 0.15f, h.trimC);   // the beam
            Gable(mb, f, vFront - 0.6f, vFront + pd, pu0, pu1, false, 2.85f, (pu1 - pu0) / 2 * 0.6f, 0.3f, 0.3f, h.roofC, shake, false, true, h.trimC);
            // the entry under the porch, windows without shutters (upper sash divided), more upstairs
            float doorU = pu0 + (pu1 - pu0) * 0.38f;
            FrontDoor(d, f, doorU, vFront, h, Hash01(h.seed, 401) < 0.5f, false);
            WindowF(d, f, Mathf.Min(pu1 - 0.9f, doorU + 2.0f), 1.5f, 1.4f, 1.4f, vFront, h, false);
            float up0 = gU1 + 0.9f, up1 = W / 2 - 0.9f; int ups = up1 - up0 > 5f ? 3 : 2;
            for (int k = 0; k < ups; k++) WindowF(d, f, Mathf.Lerp(up0, up1, k / (float)(ups - 1)), 4.15f, 1.0f, 1.35f, vFront, h, false);
            if (h.chimney) Chimney(mb, f, W / 2 - 1.1f, -D / 2 + 1.6f, hE - 0.6f, hE + riseM + 0.9f, h);
        }

        // -- style G: townhome row ----------------------------------------------------

        static int TownUnits(HouseSpec h) => Mathf.Max(2, Mathf.RoundToInt(h.W / 6.8f));

        static void BuildG(MeshBuilder mb, HouseSpec h)
        {
            var d = h.detail ?? mb;
            var f = h.frame; float W = h.W, D = h.D;
            float yb = h.baseY - h.floorY;
            int n = TownUnits(h); float uw = W / n;
            bool garages = h.garageBays > 0 && D >= 10f;
            h.garageU0 = -W / 2 + 0.4f; h.garageU1 = -W / 2 + Mathf.Min(uw - 0.6f, 3.3f);
            float hE = 2 * StoreyH - 0.05f;
            for (int i = 0; i < n; i++)
            {
                float u0 = -W / 2 + i * uw, u1 = u0 + uw;
                bool brick = (i + (int)(Hash01(h.seed, 410) * 2)) % 2 == 0;
                Color front = brick ? h.brickC : (i % 3 == 1 ? Shade(h.sidingC, 0.88f) : h.sidingC);
                float set = (i % 2 == 0) ? 0f : 0.45f;                                                     // every other unit set back a little
                BoxNoBottom(mb, f, u0, u1, yb, hE, -D / 2, D / 2 - set, front, h.sidingC, h.sidingC);
                if (i > 0) BoxF(mb, f, u0 - 0.08f, u0 + 0.08f, 0f, hE + 0.25f, D / 2 - 0.8f, D / 2 + 0.05f, Shade(h.sidingC, 0.75f));   // the party wall's cap
                if (i % 2 == 0)   // a small front gable on every other unit
                    Gable(mb, f, D / 2 - 1.6f - set, D / 2 + 0.2f - set, u0 + 0.6f, u1 - 0.6f, false, hE - 0.2f, (uw - 1.2f) / 2 * 0.7f, 0.25f, 0.25f, h.roofC, front, false, true, h.trimC);
                float v = D / 2 - set;
                float gu1 = u0 + Mathf.Min(uw - 0.6f, 3.3f);
                if (garages) GarageDoors(d, f, u0 + 0.4f, gu1, 1, v, h);
                float doorU = garages ? gu1 + (u1 - gu1) / 2 : u0 + uw * 0.3f;
                FrontDoor(d, f, doorU, v, h, false, false);
                if (!garages) WindowF(d, f, u0 + uw * 0.7f, 1.55f, 1.2f, 1.35f, v, h, true);
                WindowF(d, f, u0 + uw * 0.3f, 4.15f, 1.0f, 1.35f, v, h, true);
                WindowF(d, f, u0 + uw * 0.72f, 4.15f, 1.0f, 1.35f, v, h, true);
                WallLantern(d, f, doorU + 0.75f, 1.9f, v);
            }
            Gable(mb, f, -W / 2, W / 2, -D / 2, D / 2, true, hE, D / 2 * 0.42f, 0.5f, 0.35f, h.roofC, h.sidingC, true, true, h.trimC);
        }

        // -- style H: ranch with a carport ------------------------------------------------

        static float CarportWidth(HouseSpec h) => Mathf.Clamp(h.W * 0.28f, 3.4f, 6.2f);

        static void BuildH(MeshBuilder mb, HouseSpec h)
        {
            var d = h.detail ?? mb;
            var f = h.frame; float W = h.W, D = h.D;
            float yb = h.baseY - h.floorY;
            float cw = CarportWidth(h), s = -W / 2 + cw;
            h.garageU0 = -W / 2; h.garageU1 = s;
            float hE = 2.65f, rise = D / 2 * 0.28f;
            Color front = h.brick && Hash01(h.seed, 420) < 0.5f ? h.brickC : h.sidingC;
            BoxNoBottom(mb, f, s, W / 2, yb, hE, -D / 2, D / 2, front, h.brick ? h.brickC : h.sidingC, h.sidingC);
            if (h.brick && front != h.brickC) BoxF(mb, f, s, W / 2, yb, 1.0f, D / 2 - 0.02f, D / 2 + 0.09f, h.brickC);   // brick below the windows
            // the roof runs on over the carport: posts at its open corners, a storage closet at its back
            Gable(mb, f, -W / 2, W / 2, -D / 2, D / 2, true, hE, rise, 0.65f, 0.45f, h.roofC, h.sidingC, true, true, h.trimC);
            BoxNoBottom(mb, f, -W / 2, s, yb, hE, -D / 2, -D / 2 + 2.2f, h.sidingC, h.sidingC, h.sidingC);
            FrontQuad(d, f, -W / 2 + 0.6f, -W / 2 + 1.5f, 0f, 2.05f, -D / 2 + 2.22f, Shade(h.sidingC, 0.8f));   // the closet door
            Color post = Hash01(h.seed, 421) < 0.5f ? h.trimC : new Color(0.12f, 0.12f, 0.13f);
            foreach (float v in new[] { D / 2 - 0.2f, 0f })
                BoxF(mb, f, -W / 2 + 0.05f, -W / 2 + 0.25f, 0f, hE, v - 0.1f, v + 0.1f, post);
            BoxF(mb, f, -W / 2, s, hE - 0.25f, hE, D / 2 - 0.25f, D / 2, h.trimC);   // the carport's front beam
            // the entry, the picture window (three panes), a pair of bedroom windows with shutters
            float doorU = s + 1.2f;
            FrontDoor(d, f, doorU, D / 2, h, false, false);
            float pc = doorU + 2.6f;
            if (pc + 1.6f < W / 2 - 1.8f)
            {
                WindowF(d, f, pc, 1.45f, 2.6f, 1.5f, D / 2, h, true, false);
                FrontQuad(d, f, pc - 0.47f, pc - 0.43f, 0.7f, 2.2f, D / 2 + 0.06f, h.trimC); FrontQuad(d, f, pc + 0.43f, pc + 0.47f, 0.7f, 2.2f, D / 2 + 0.06f, h.trimC);
            }
            WindowF(d, f, W / 2 - 1.2f, 1.6f, 1.1f, 1.0f, D / 2, h, true);
            if (h.chimney) Chimney(mb, f, (s + W / 2) / 2, -D / 2 + 1.3f, hE - 0.4f, hE + rise + 0.7f, h);
        }
    }
}
