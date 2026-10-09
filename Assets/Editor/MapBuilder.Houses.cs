using System;
using System.Collections.Generic;
using UnityEngine;

// Round 7: the four house styles of Chastain Glen (from the photos in local/reference_photos, which are never copied into
// the project: everything here is drawn from code), plus Dan's house, which is style A built with extra detail and fixed
// colours. A house is built in a local frame (u along the facade, v out toward the street, y up from the floor level) and
// is one vertex-coloured mesh. Windows, doors, garage doors, trim and shutters are on the street side; sides and back are
// plain, as Dan said.
//   A  two-storey, front gable over a two-car garage; brick-front or all siding (Dan's style, the commonest)
//   B  two-storey brick-front colonial, four full-height white columns, side garage wing with a front gable
//   C  one-storey ranch with a front-gable two-car garage (siding, sometimes a brick accent)
//   D  plain two-storey siding house with a side gable, a small porch roof and a one-car garage at the end
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        enum HouseStyle { A, B, C, D }

        sealed class HouseSpec
        {
            public int index, seed;
            public HouseStyle style;
            public bool brick, dan, entryGable, chimney;
            public float W, D;                    // facade width, depth
            public Frame frame;                   // origin = footprint centre at the floor level
            public float floorY, baseY;           // floor level and the lowest wall base (the foundation goes down to it)
            public Color brickC, sidingC, roofC, trimC, shutterC, doorC, glassC, garageC;
            public float garageU0, garageU1;      // the garage bay in u (it sits on the -u side of the frame)
            public Vector2 footprintCentre;
            public List<Vector2> ring;
            public Vector3 garageCentre;          // world, at the foot of the garage doors
            public MeshBuilder detail;           // windows, doors, garage doors, shutters: flat quads that do not need to cast shadows (a separate mesh with shadows off)
            public Vector2 nearestRoadPoint;
            public float roadHalf;
        }

        // ------------------------------------------------------------ palettes (from the photos)

        static readonly Color[] SidingPalette =
        {
            new Color(0.92f, 0.92f, 0.89f), // white
            new Color(0.86f, 0.80f, 0.64f), // cream
            new Color(0.55f, 0.58f, 0.63f), // grey
            new Color(0.47f, 0.57f, 0.47f), // sage green
            new Color(0.70f, 0.78f, 0.86f), // light blue
            new Color(0.78f, 0.74f, 0.66f), // greige
        };
        static readonly Color[] YellowSiding = { new Color(0.88f, 0.78f, 0.48f), new Color(0.90f, 0.86f, 0.62f), new Color(0.92f, 0.92f, 0.89f), new Color(0.86f, 0.80f, 0.64f) };
        static readonly Color[] BrickPalette = { new Color(0.50f, 0.19f, 0.14f), new Color(0.58f, 0.27f, 0.19f), new Color(0.45f, 0.20f, 0.17f) };
        static readonly Color[] ShingleRoofs = { new Color(0.34f, 0.35f, 0.37f), new Color(0.19f, 0.19f, 0.21f), new Color(0.31f, 0.24f, 0.19f), new Color(0.26f, 0.27f, 0.29f) };
        static readonly Color[] ShutterColours = { new Color(0.10f, 0.11f, 0.12f), new Color(0.12f, 0.22f, 0.16f), new Color(0.20f, 0.12f, 0.10f), new Color(0.11f, 0.15f, 0.25f) };
        static readonly Color TrimWhite = new Color(0.95f, 0.95f, 0.93f), GarageWhite = new Color(0.93f, 0.93f, 0.91f), GlassDark = new Color(0.16f, 0.20f, 0.26f);

        // ------------------------------------------------------------ choosing a style

        // by footprint size and shape, with a stable hash of the building's seed: wide shallow footprints become ranches,
        // the largest become colonials, the rest are mostly Dan's style with some plain two-storey houses and ranches
        static HouseStyle ChooseStyle(float area, float longSide, float shortSide, int seed)
        {
            float aspect = longSide / Mathf.Max(shortSide, 1f);
            float r = Hash01(seed, 31);
            if (aspect >= 1.42f && area < 175f) return HouseStyle.C;
            if (area >= 168f) return r < 0.55f ? HouseStyle.B : HouseStyle.A;
            if (r < 0.58f) return HouseStyle.A;
            if (r < 0.80f) return HouseStyle.D;
            return HouseStyle.C;
        }

        static void ChooseColours(HouseSpec h)
        {
            int s = h.seed;
            h.brickC = Shade(PickOf(BrickPalette, s, 41), 0.95f + 0.1f * Hash01(s, 47));
            h.sidingC = h.style == HouseStyle.D && Hash01(s, 43) < 0.35f ? PickOf(YellowSiding, s, 44) : PickOf(SidingPalette, s, 42);
            h.roofC = PickOf(ShingleRoofs, s, 45);
            h.trimC = TrimWhite;
            h.shutterC = PickOf(ShutterColours, s, 46);
            h.doorC = Hash01(s, 48) < 0.5f ? new Color(0.10f, 0.10f, 0.11f) : Hash01(s, 49) < 0.5f ? new Color(0.14f, 0.26f, 0.19f) : new Color(0.38f, 0.12f, 0.12f);
            h.glassC = Shade(GlassDark, 0.9f + 0.25f * Hash01(s, 50));
            h.garageC = GarageWhite;
            // brick fronts: style B always; A about half of the time; C sometimes has a brick accent; D never
            h.brick = h.style == HouseStyle.B || (h.style == HouseStyle.A && Hash01(s, 51) < 0.5f) || (h.style == HouseStyle.C && Hash01(s, 52) < 0.3f);
            h.entryGable = h.style == HouseStyle.A && Hash01(s, 53) < 0.55f;
            h.chimney = Hash01(s, 54) < 0.4f;
            // siding that is close to a brick or roof colour reads badly: keep brick-front houses' siding light
            if (h.brick && h.style == HouseStyle.A && h.sidingC.grayscale < 0.5f) h.sidingC = SidingPalette[0];
        }

        static void ApplyDan(HouseSpec h)
        {
            h.style = HouseStyle.A; h.dan = true; h.brick = true; h.entryGable = true; h.chimney = false;
            h.brickC = new Color(0.50f, 0.17f, 0.13f);       // red brick
            h.sidingC = new Color(0.86f, 0.88f, 0.89f);       // light siding on the upper side wall
            h.roofC = new Color(0.30f, 0.30f, 0.32f);         // grey shingle
            h.trimC = TrimWhite; h.shutterC = new Color(0.08f, 0.09f, 0.10f);
            h.doorC = new Color(0.07f, 0.07f, 0.08f); h.glassC = GlassDark; h.garageC = GarageWhite;
        }

        // the garage bay in u (it sits on the -u side of the frame); the same numbers the builders use
        static void GarageSpan(HouseSpec h)
        {
            float W = h.W; float u0 = -W / 2, u1;
            switch (h.style)
            {
                case HouseStyle.A: u1 = u0 + Mathf.Clamp(W * 0.47f, 5.7f, 6.7f); break;
                case HouseStyle.B: u1 = u0 + Mathf.Clamp(W * 0.42f, 5.6f, 6.6f); break;
                case HouseStyle.C: u1 = u0 + Mathf.Clamp(W * 0.42f, 5.6f, 6.4f); break;
                default: u1 = u0 + 3.4f; break;
            }
            h.garageU0 = u0; h.garageU1 = u1;
        }

        // ------------------------------------------------------------ the builder

        const float StoreyH = 2.85f;

        static void BuildHouse(MeshBuilder mb, HouseSpec h)
        {
            switch (h.style)
            {
                case HouseStyle.A: BuildA(mb, h); break;
                case HouseStyle.B: BuildB(mb, h); break;
                case HouseStyle.C: BuildC(mb, h); break;
                default: BuildD(mb, h); break;
            }
        }

        // -- shared front details ------------------------------------------------

        static void WindowF(MeshBuilder mb, Frame f, float uc, float yc, float w, float hgt, float v, HouseSpec h, bool shutters, bool muntins = true, float scale = 1f)
        {
            FrontQuad(mb, f, uc - w / 2 - 0.09f, uc + w / 2 + 0.09f, yc - hgt / 2 - 0.09f, yc + hgt / 2 + 0.09f, v + 0.02f, h.trimC);
            FrontQuad(mb, f, uc - w / 2, uc + w / 2, yc - hgt / 2, yc + hgt / 2, v + 0.04f, h.glassC);
            if (muntins)
            {
                FrontQuad(mb, f, uc - 0.025f, uc + 0.025f, yc - hgt / 2, yc + hgt / 2, v + 0.06f, h.trimC);
                FrontQuad(mb, f, uc - w / 2, uc + w / 2, yc - 0.025f + hgt * 0.12f, yc + 0.025f + hgt * 0.12f, v + 0.06f, h.trimC);
            }
            // sill
            FrontQuad(mb, f, uc - w / 2 - 0.15f, uc + w / 2 + 0.15f, yc - hgt / 2 - 0.16f, yc - hgt / 2 - 0.09f, v + 0.05f, h.trimC);
            if (shutters)
            {
                float sw = Mathf.Min(0.46f, w * 0.45f);
                FrontQuad(mb, f, uc - w / 2 - 0.09f - sw - 0.02f, uc - w / 2 - 0.11f, yc - hgt / 2 - 0.05f, yc + hgt / 2 + 0.05f, v + 0.03f, h.shutterC);
                FrontQuad(mb, f, uc + w / 2 + 0.11f, uc + w / 2 + 0.09f + sw + 0.02f, yc - hgt / 2 - 0.05f, yc + hgt / 2 + 0.05f, v + 0.03f, h.shutterC);
            }
        }

        static void GarageDoors(MeshBuilder mb, Frame f, float u0, float u1, int doors, float v, HouseSpec h)
        {
            float gh = 2.15f;
            FrontQuad(mb, f, u0, u1, 0f, gh + 0.22f, v + 0.02f, h.trimC);      // white surround
            float gap = 0.18f, dw = (u1 - u0 - 0.30f - gap * (doors - 1)) / doors;
            for (int i = 0; i < doors; i++)
            {
                float a = u0 + 0.15f + i * (dw + gap), b = a + dw;
                FrontQuad(mb, f, a, b, 0f, gh, v + 0.04f, h.garageC);
                for (int k = 1; k <= 3; k++) FrontQuad(mb, f, a, b, gh * k / 4f - 0.018f, gh * k / 4f + 0.018f, v + 0.055f, Shade(h.garageC, 0.78f));
                FrontQuad(mb, f, a + dw * 0.5f - 0.015f, a + dw * 0.5f + 0.015f, 0f, gh, v + 0.055f, Shade(h.garageC, 0.84f));
                // a window row in the top panel
                for (int k = 0; k < 4; k++) { float ua = a + dw * (0.10f + 0.22f * k); FrontQuad(mb, f, ua, ua + dw * 0.18f, gh - 0.42f, gh - 0.14f, v + 0.06f, h.glassC); }
            }
        }

        static void FrontDoor(MeshBuilder mb, Frame f, float uc, float v, HouseSpec h, bool sidelights, bool pediment)
        {
            float w = 1.0f, hgt = 2.15f;
            float side = sidelights ? 0.42f : 0f;
            FrontQuad(mb, f, uc - w / 2 - side - 0.14f, uc + w / 2 + side + 0.14f, 0f, hgt + 0.18f, v + 0.02f, h.trimC);
            FrontQuad(mb, f, uc - w / 2, uc + w / 2, 0f, hgt, v + 0.04f, h.doorC);
            FrontQuad(mb, f, uc - w / 2 + 0.17f, uc + w / 2 - 0.17f, 1.25f, 1.95f, v + 0.055f, Shade(h.doorC, 1.9f));
            FrontQuad(mb, f, uc + w / 2 - 0.2f, uc + w / 2 - 0.12f, 0.95f, 1.05f, v + 0.06f, new Color(0.8f, 0.7f, 0.3f)); // handle
            if (sidelights)
            {
                FrontQuad(mb, f, uc - w / 2 - side + 0.03f, uc - w / 2 - 0.03f, 0.35f, hgt - 0.1f, v + 0.04f, h.glassC);
                FrontQuad(mb, f, uc + w / 2 + 0.03f, uc + w / 2 + side - 0.03f, 0.35f, hgt - 0.1f, v + 0.04f, h.glassC);
            }
            // doorstep
            BoxF(mb, f, uc - w / 2 - side - 0.35f, uc + w / 2 + side + 0.35f, -0.12f, 0.0f, v, v + 1.0f, new Color(0.66f, 0.64f, 0.61f));
            if (pediment)
            {
                // a pediment over the door: white triangle on two pilasters
                float pw = w / 2 + side + 0.45f;
                BoxF(mb, f, uc - pw, uc + pw, hgt + 0.18f, hgt + 0.36f, v, v + 0.35f, h.trimC);
                mb.Tint = h.trimC;
                TriFace(mb, f.P(uc - pw, hgt + 0.36f, v + 0.35f), f.P(uc + pw, hgt + 0.36f, v + 0.35f), f.P(uc, hgt + 1.0f, v + 0.35f), f.F);
                BoxF(mb, f, uc - pw, uc - pw + 0.14f, 0f, hgt + 0.18f, v, v + 0.3f, h.trimC);
                BoxF(mb, f, uc + pw - 0.14f, uc + pw, 0f, hgt + 0.18f, v, v + 0.3f, h.trimC);
            }
        }

        static void WallLantern(MeshBuilder mb, Frame f, float u, float y, float v)
        {
            BoxF(mb, f, u - 0.07f, u + 0.07f, y - 0.14f, y + 0.14f, v, v + 0.16f, new Color(0.08f, 0.08f, 0.08f));
        }

        static void Chimney(MeshBuilder mb, Frame f, float u, float v, float yBase, float yTop, HouseSpec h)
        {
            BoxF(mb, f, u - 0.45f, u + 0.45f, yBase, yTop, v - 0.35f, v + 0.35f, h.brick ? h.brickC : Shade(h.brickC, 0.9f));
            BoxF(mb, f, u - 0.52f, u + 0.52f, yTop, yTop + 0.1f, v - 0.42f, v + 0.42f, new Color(0.62f, 0.60f, 0.57f));
        }

        // -- style A ---------------------------------------------------------------

        static void BuildA(MeshBuilder mb, HouseSpec h)
        {
            var d = h.detail ?? mb;
            var f = h.frame; float W = h.W, D = h.D;
            float yb = h.baseY - h.floorY;
            float proj = 1.3f;                                        // the garage stands this far in front of the entry
            float gW = Mathf.Clamp(W * 0.47f, 5.7f, 6.7f);
            float gU0 = -W / 2, gU1 = -W / 2 + gW;
            h.garageU0 = gU0; h.garageU1 = gU1;
            float hG = 3.95f, riseG = gW / 2 * 0.62f, hE = 2 * StoreyH - 0.05f;
            float vFront = D / 2 - proj;
            float hbM = (vFront + D / 2) / 2f, riseM = hbM * 0.52f;
            Color front = h.brick ? h.brickC : h.sidingC;
            // main two-storey body (siding sides and back, brick or siding front)
            BoxNoBottom(mb, f, gU0, W / 2, yb, hE, -D / 2, vFront, front, h.sidingC, h.sidingC);
            // garage block in front of it, with the gable over it
            BoxNoBottom(mb, f, gU0, gU1, yb, hG, vFront, D / 2, front, h.sidingC, h.sidingC);
            Gable(mb, f, vFront - 1.2f, D / 2, gU0, gU1, false, hG, riseG, 0.28f, 0.34f, h.roofC, front, false, true, h.trimC);
            Gable(mb, f, gU0, W / 2, -D / 2, vFront, true, hE, riseM, 0.5f, 0.4f, h.roofC, h.sidingC, true, true, h.trimC);
            if (h.entryGable)
            {
                float ue = (gU1 + W / 2) / 2f;
                float ew = Mathf.Min(3.4f, (W / 2 - gU1) * 0.55f);
                BoxNoBottom(mb, f, ue - ew / 2, ue + ew / 2, hE - 0.9f, hE + 0.0f, vFront, vFront + 0.5f, front, front, front);
                Gable(mb, f, vFront - 1.0f, vFront + 0.5f, ue - ew / 2, ue + ew / 2, false, hE - 0.9f, ew / 2 * 0.65f, 0.22f, 0.26f, h.roofC, front, false, true, h.trimC);
            }
            // trim band over the garage doors and a corner board
            BoxF(mb, f, gU0 - 0.03f, gU1 + 0.03f, 2.45f, 2.62f, vFront, D / 2 + 0.05f, h.trimC);
            GarageDoors(d, f, gU0 + 0.35f, gU1 - 0.35f, 2, D / 2, h);
            // gable window (one, in the gable) with dark shutters
            float gc = (gU0 + gU1) / 2;
            WindowF(d, f, gc, hG + 0.78f, 0.95f, 1.15f, D / 2, h, false);
            FrontQuad(d, f, gc - 0.52f, gc - 0.12f, hG + 0.2f, hG + 1.36f, D / 2 + 0.03f, h.shutterC);
            FrontQuad(d, f, gc + 0.12f, gc + 0.52f, hG + 0.2f, hG + 1.36f, D / 2 + 0.03f, h.shutterC);
            // entry
            float E = W / 2 - gU1, doorU = gU1 + E * 0.60f;
            float vEntry = vFront;
            FrontDoor(d, f, doorU, vEntry, h, true, h.dan);
            WindowF(d, f, gU1 + E * 0.17f + 0.35f, 1.55f, 1.15f, 1.35f, vEntry, h, true);
            if (E > 6.0f) WindowF(d, f, W / 2 - 0.95f, 1.55f, 1.0f, 1.35f, vEntry, h, true);
            // upstairs
            WindowF(d, f, gU1 + E * 0.22f + 0.35f, 4.1f, 1.0f, 1.4f, vEntry, h, true);
            if (!h.entryGable) WindowF(d, f, gU1 + E * 0.78f, 4.1f, 1.0f, 1.4f, vEntry, h, true);
            else
            {
                float ue = (gU1 + W / 2) / 2f;
                WindowF(d, f, ue, 4.15f, 1.2f, 1.1f, vFront + 0.5f, h, false);
                if (h.dan) { FrontQuad(d, f, ue - 0.62f, ue + 0.62f, 4.78f, 5.05f, vFront + 0.52f, h.trimC); FrontQuad(d, f, ue - 0.46f, ue + 0.46f, 4.83f, 4.98f, vFront + 0.54f, h.glassC); }
            }
            // one window on the garage side wall upstairs and one down, so the side is not blank
            SideQuad(d, f, gU0 - 0.02f, D / 2 - 6.5f, D / 2 - 5.4f, 4.0f, 5.2f, -1f, h.trimC);
            SideQuad(d, f, gU0 - 0.03f, D / 2 - 6.4f, D / 2 - 5.5f, 4.1f, 5.1f, -1f, h.glassC);
            if (h.chimney) Chimney(mb, f, W / 2 - 0.9f, -D / 2 + 1.6f, hE - 1.0f, hE + riseM + 0.9f, h);
            if (h.dan)
            {
                // wall lanterns by the garage and the door, gutters and downspouts at the garage corners
                WallLantern(d, f, (gU0 + gU1) / 2, 2.75f, D / 2);
                WallLantern(d, f, doorU - 1.5f, 1.8f, vEntry); WallLantern(d, f, doorU + 1.5f, 1.8f, vEntry);
                BoxF(mb, f, gU0 - 0.1f, gU0 - 0.0f, yb, hG, D / 2 - 0.1f, D / 2 + 0.05f, h.trimC);
                BoxF(mb, f, gU1 + 0.0f, gU1 + 0.1f, yb, hG, D / 2 - 0.1f, D / 2 + 0.05f, h.trimC);
                BoxF(mb, f, gU1 - 0.04f, gU1 + 0.06f, 0f, hE, vFront - 0.1f, vFront + 0.04f, h.trimC);
                // the arched (fan) window light over the entry window is the half-disc above; a brick sill course
                BoxF(mb, f, gU0 - 0.04f, W / 2 + 0.04f, -0.05f, 0.18f, -D / 2, D / 2 + 0.04f, Shade(h.brickC, 1.25f));
            }
        }

        // -- style B ---------------------------------------------------------------

        static void BuildB(MeshBuilder mb, HouseSpec h)
        {
            var d = h.detail ?? mb;
            var f = h.frame; float W = h.W, D = h.D;
            float yb = h.baseY - h.floorY;
            float wing = Mathf.Clamp(W * 0.42f, 5.6f, 6.6f);
            float gU0 = -W / 2, gU1 = -W / 2 + wing;
            h.garageU0 = gU0; h.garageU1 = gU1;
            float hE = 2 * StoreyH, hb = D / 2, rise = hb * 0.48f;
            Color front = h.brickC;
            BoxNoBottom(mb, f, gU1, W / 2, yb, hE, -D / 2, D / 2, front, h.sidingC, h.sidingC);
            Gable(mb, f, gU1, W / 2, -D / 2, D / 2, true, hE, rise, 1.0f, 0.45f, h.roofC, h.sidingC, true, true, h.trimC);
            // garage wing, one storey with its own front gable
            float wH = 3.35f, wRise = wing / 2 * 0.58f;
            BoxNoBottom(mb, f, gU0, gU1, yb, wH, -D / 2 + 1.2f, D / 2 + 0.4f, h.sidingC, h.sidingC, h.sidingC);
            Gable(mb, f, -D / 2 + 0.2f, D / 2 + 0.4f, gU0, gU1, false, wH, wRise, 0.3f, 0.3f, h.roofC, h.sidingC, false, true, h.trimC);
            GarageDoors(d, f, gU0 + 0.35f, gU1 - 0.35f, 2, D / 2 + 0.4f, h);
            FrontQuad(d, f, (gU0 + gU1) / 2 - 0.3f, (gU0 + gU1) / 2 + 0.3f, wH + 0.55f, wH + 1.05f, D / 2 + 0.43f, Shade(h.sidingC, 0.55f)); // round vent
            // four full-height columns across the main front, an entablature and the deep eave on top
            float m0 = gU1 + 0.2f, m1 = W / 2 - 0.2f, vc = D / 2 + 0.7f;
            for (int i = 0; i < 4; i++)
            {
                float u = Mathf.Lerp(m0 + 0.2f, m1 - 0.2f, i / 3f);
                BoxF(mb, f, u - 0.16f, u + 0.16f, yb, hE, vc - 0.16f, vc + 0.16f, h.trimC);
                BoxF(mb, f, u - 0.22f, u + 0.22f, 0f, 0.35f, vc - 0.22f, vc + 0.22f, h.trimC);
            }
            BoxF(mb, f, m0 - 0.1f, m1 + 0.1f, hE - 0.45f, hE, D / 2 - 0.0f, vc + 0.3f, h.trimC);
            // door in the middle of the main front, windows beside and above
            float mc = (gU1 + W / 2) / 2f;
            FrontDoor(d, f, mc, D / 2, h, true, true);
            float span = (W / 2 - gU1);
            WindowF(d, f, gU1 + span * 0.16f + 0.35f, 1.6f, 1.15f, 1.5f, D / 2, h, true);
            WindowF(d, f, W / 2 - span * 0.16f - 0.35f, 1.6f, 1.15f, 1.5f, D / 2, h, true);
            WindowF(d, f, gU1 + span * 0.2f + 0.3f, 4.15f, 1.0f, 1.45f, D / 2, h, true);
            WindowF(d, f, W / 2 - span * 0.2f - 0.3f, 4.15f, 1.0f, 1.45f, D / 2, h, true);
            WindowF(d, f, mc, 4.2f, 1.5f, 1.35f, D / 2, h, false);
            if (h.chimney) Chimney(mb, f, W / 2 - 0.9f, 0f, hE - 1.0f, hE + rise + 0.9f, h);
        }

        // -- style C ---------------------------------------------------------------

        static void BuildC(MeshBuilder mb, HouseSpec h)
        {
            var d = h.detail ?? mb;
            var f = h.frame; float W = h.W, D = h.D;
            float yb = h.baseY - h.floorY;
            float proj = 1.0f, gW = Mathf.Clamp(W * 0.42f, 5.6f, 6.4f);
            float gU0 = -W / 2, gU1 = -W / 2 + gW;
            h.garageU0 = gU0; h.garageU1 = gU1;
            float hE = 2.85f, vFront = D / 2 - proj, hb = (D - proj) / 2, rise = hb * 0.46f;
            BoxNoBottom(mb, f, gU0, W / 2, yb, hE, -D / 2, vFront, h.sidingC, h.sidingC, h.sidingC);
            Gable(mb, f, gU0, W / 2, -D / 2, vFront, true, hE, rise, 0.55f, 0.4f, h.roofC, h.sidingC, true, true, h.trimC);
            BoxNoBottom(mb, f, gU0, gU1, yb, hE + 0.15f, vFront, D / 2, h.sidingC, h.sidingC, h.sidingC);
            Gable(mb, f, vFront - 1.0f, D / 2, gU0, gU1, false, hE + 0.15f, gW / 2 * 0.62f, 0.28f, 0.32f, h.roofC, h.sidingC, false, true, h.trimC);
            GarageDoors(d, f, gU0 + 0.3f, gU1 - 0.3f, 2, D / 2, h);
            float gc = (gU0 + gU1) / 2;
            FrontQuad(d, f, gc - 0.38f, gc + 0.38f, hE + 0.7f, hE + 1.46f, D / 2 + 0.03f, h.trimC);                // round louvre vent
            FrontQuad(d, f, gc - 0.30f, gc + 0.30f, hE + 0.78f, hE + 1.38f, D / 2 + 0.05f, Shade(h.sidingC, 0.55f));
            float E = W / 2 - gU1, doorU = gU1 + E * 0.45f;
            if (h.brick) BoxF(mb, f, gU1, W / 2, yb, 1.0f, vFront - 0.0f, vFront + 0.1f, h.brickC);          // brick accent under the windows
            FrontDoor(d, f, doorU, vFront + (h.brick ? 0.1f : 0f), h, Hash01(h.seed, 61) < 0.5f, false);
            WindowF(d, f, gU1 + 1.0f, 1.55f, 1.3f, 1.3f, vFront, h, true);
            WindowF(d, f, W / 2 - 1.2f, 1.55f, 1.3f, 1.3f, vFront, h, true);
            if (h.chimney) Chimney(mb, f, W / 2 - 1.3f, -D / 2 + 1.5f, hE - 0.5f, hE + rise + 0.8f, h);
        }

        // -- style D ---------------------------------------------------------------

        static void BuildD(MeshBuilder mb, HouseSpec h)
        {
            var d = h.detail ?? mb;
            var f = h.frame; float W = h.W, D = h.D;
            float yb = h.baseY - h.floorY;
            float hE = 2 * StoreyH - 0.1f, hb = D / 2, rise = hb * 0.5f;
            float gU0 = -W / 2, gU1 = -W / 2 + 3.4f;                      // the one-car bay at the end of the facade
            h.garageU0 = gU0; h.garageU1 = gU1;
            BoxNoBottom(mb, f, -W / 2, W / 2, yb, hE, -D / 2, D / 2, h.sidingC, h.sidingC, h.sidingC);
            Gable(mb, f, -W / 2, W / 2, -D / 2, D / 2, true, hE, rise, 0.55f, 0.4f, h.roofC, h.sidingC, true, true, h.trimC);
            GarageDoors(d, f, gU0 + 0.3f, gU1 - 0.15f, 1, D / 2, h);
            float doorU = gU1 + (W / 2 - gU1) * 0.38f;
            FrontDoor(d, f, doorU, D / 2, h, false, false);
            // a small shed porch roof on two posts
            float pu0 = doorU - 1.3f, pu1 = doorU + 1.3f;
            BoxF(mb, f, pu0, pu1, 2.55f, 2.7f, D / 2, D / 2 + 1.5f, h.trimC);
            BoxF(mb, f, pu0, pu0 + 0.14f, 0f, 2.55f, D / 2 + 1.35f, D / 2 + 1.5f, h.trimC);
            BoxF(mb, f, pu1 - 0.14f, pu1, 0f, 2.55f, D / 2 + 1.35f, D / 2 + 1.5f, h.trimC);
            float wc1 = gU1 + (W / 2 - gU1) * 0.80f;
            WindowF(d, f, wc1, 1.55f, 1.15f, 1.35f, D / 2, h, true);
            WindowF(d, f, gU1 + 0.9f, 4.1f, 1.0f, 1.4f, D / 2, h, true);
            WindowF(d, f, doorU + 0.1f, 4.1f, 1.0f, 1.4f, D / 2, h, true);
            WindowF(d, f, wc1, 4.1f, 1.0f, 1.4f, D / 2, h, true);
            WindowF(d, f, gU0 + 1.7f, 4.1f, 1.0f, 1.4f, D / 2, h, true);
            if (h.chimney) Chimney(mb, f, W / 2 - 1.0f, 0f, hE - 1.0f, hE + rise + 0.9f, h);
        }
    }
}
