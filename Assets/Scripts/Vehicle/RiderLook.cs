using System.Linq;
using UnityEngine;

namespace Racer
{
    // 0.75 rider customization (cosmetic only; no record category changes). One parametric rider built in Blender
    // (Tools/Blender/rider.py -> Resources/VehicleModels/Rider.fbx) and assembled from parts by VehicleVisual, so every
    // option combines freely. Colours for hat, shirt and pants reuse the vehicle swatches (VehiclePaint).
    // The player's look is saved in settings.json; every AI gets a random look per race (Field).
    [System.Serializable]
    public sealed class RiderLook
    {
        public int body, skin = 2, hair, hairColor = 1, hat = 1, hatColor = 6, shirt, shirtColor = 3, pants, pantsColor = 3;
        // 0.83: the chest emblem (a white ring with a bird, Rider.fbx "Emblem"), used by Dan in the scripted scenes; not a
        // garage option (0 = none for the player and the AI).
        public int emblem;

        public static readonly string[] Bodies = { "Man", "Woman" };
        public static readonly string[] SkinNames = { "Very light", "Light", "Medium", "Tan", "Brown", "Dark" };
        public static readonly Color[] Skins = { new(.95f, .79f, .67f), new(.91f, .69f, .53f), new(.78f, .52f, .36f), new(.60f, .39f, .25f), new(.39f, .22f, .15f), new(.24f, .14f, .09f) };
        public static readonly string[] Hairs = { "Short", "Medium", "Long", "Ponytail", "Bald" };
        public static readonly string[] HairColorNames = { "Black", "Dark brown", "Brown", "Auburn", "Red", "Blonde", "Grey", "White" };
        public static readonly Color[] HairColors = { new(.03f, .025f, .022f), new(.18f, .065f, .028f), new(.36f, .20f, .09f), new(.45f, .13f, .05f), new(.72f, .24f, .07f), new(.80f, .62f, .30f), new(.52f, .52f, .52f), new(.88f, .87f, .84f) };
        public static readonly string[] Hats = { "None", "Flat cap", "Baseball cap", "Beanie", "Cowboy hat" };
        public static readonly string[] Shirts = { "T-shirt", "Long sleeve", "Jacket", "Leather jacket" };
        public static readonly string[] Pants = { "Jeans", "Shorts" };
        // Part keys in the FBX object names: <Pose>_<Category>_<Option>_<Body>__<slot>.
        static readonly string[] HairKeys = { "Short", "Medium", "Long", "Ponytail", "Bald" }, HatKeys = { "None", "FlatCap", "Baseball", "Beanie", "Cowboy" },
            ShirtKeys = { "Tee", "Long", "Jacket", "Leather" }, PantsKeys = { "Jeans", "Shorts" };

        // The default is the 0.73 rider's identity: man, short dark-brown hair, medium skin, flat cap, blue T-shirt, jeans.
        public static RiderLook Player = new();
        // 0.99 Part E: the police bike's rider: a black jacket and black jeans, no hair (the white helmet is in the bike's model); not a garage option
        public static readonly RiderLook Police = new() { body = 0, skin = 2, hair = 4, hairColor = 0, hat = 0, shirt = 2, shirtColor = 6, pants = 0, pantsColor = 6 };

        public RiderLook Copy() => (RiderLook)MemberwiseClone();
        public bool SameAs(RiderLook o) => o != null && body == o.body && skin == o.skin && hair == o.hair && hairColor == o.hairColor && hat == o.hat
            && hatColor == o.hatColor && shirt == o.shirt && shirtColor == o.shirtColor && pants == o.pants && pantsColor == o.pantsColor && emblem == o.emblem;
        static int Wrap(int v, int n) => ((v % n) + n) % n;
        // Repairs out-of-range values from an edited or damaged settings file.
        public RiderLook Valid()
        {
            body = Wrap(body, 2); skin = Wrap(skin, Skins.Length); hair = Wrap(hair, Hairs.Length); hairColor = Wrap(hairColor, HairColors.Length);
            hat = Wrap(hat, Hats.Length); hatColor = Wrap(hatColor, VehiclePaint.Colors.Length); shirt = Wrap(shirt, Shirts.Length);
            shirtColor = Wrap(shirtColor, VehiclePaint.Colors.Length); pants = Wrap(pants, Pants.Length); pantsColor = Wrap(pantsColor, VehiclePaint.Colors.Length);
            emblem = Wrap(emblem, 2);
            return this;
        }

        // Garage rows, in order: field index -> label and step.
        public const int Fields = 10;
        public string Label(int field) => field switch
        {
            0 => "Body: " + Bodies[body], 1 => "Skin tone: " + SkinNames[skin], 2 => "Hair: " + Hairs[hair], 3 => "Hair colour: " + HairColorNames[hairColor],
            4 => "Hat: " + Hats[hat], 5 => "Hat colour: " + VehiclePaint.Names[hatColor], 6 => "Shirt: " + Shirts[shirt], 7 => "Shirt colour: " + VehiclePaint.Names[shirtColor],
            8 => "Pants: " + Pants[pants], _ => "Pants colour: " + VehiclePaint.Names[pantsColor]
        };
        public void Step(int field, int d)
        {
            switch (field)
            {
                case 0: body = Wrap(body + d, 2); break;
                case 1: skin = Wrap(skin + d, Skins.Length); break;
                case 2: hair = Wrap(hair + d, Hairs.Length); break;
                case 3: hairColor = Wrap(hairColor + d, HairColors.Length); break;
                case 4: hat = Wrap(hat + d, Hats.Length); break;
                case 5: hatColor = Wrap(hatColor + d, VehiclePaint.Colors.Length); break;
                case 6: shirt = Wrap(shirt + d, Shirts.Length); break;
                case 7: shirtColor = Wrap(shirtColor + d, VehiclePaint.Colors.Length); break;
                case 8: pants = Wrap(pants + d, Pants.Length); break;
                default: pantsColor = Wrap(pantsColor + d, VehiclePaint.Colors.Length); break;
            }
        }

        // Whether a rider part "<Category>_<Option>_<Body>" belongs to this look. A hat hides the hair above the hat band
        // (HairTop), so hair and hat never pass through each other.
        public bool Shows(string category, string option, string bodyKey)
        {
            if (bodyKey != "Any" && bodyKey != Bodies[body]) return false;
            return category switch
            {
                "Base" => true,
                "Shirt" => option == ShirtKeys[shirt],
                "Pants" => option == PantsKeys[pants],
                "Hair" => option == HairKeys[hair],
                "HairTop" => option == HairKeys[hair] && hat == 0,
                "Hat" => hat > 0 && option == HatKeys[hat],
                // on the T-shirt / long sleeve centred, on the jacket on the left chest; none on the open leather jacket
                "Emblem" => emblem == 1 && option == (shirt < 2 ? "Tee" : shirt == 2 ? "Jacket" : ""),
                _ => false
            };
        }

        public static RiderLook Random(System.Random r) => new()
        {
            body = r.Next(2), skin = r.Next(Skins.Length), hair = r.Next(Hairs.Length), hairColor = r.Next(HairColors.Length), hat = r.Next(Hats.Length),
            hatColor = r.Next(VehiclePaint.Colors.Length), shirt = r.Next(Shirts.Length), shirtColor = r.Next(VehiclePaint.Colors.Length),
            pants = r.Next(Pants.Length), pantsColor = r.Next(VehiclePaint.Colors.Length)
        };

        // AI riders for one race: random, varied across the field (different shirt colours, both bodies when there are two
        // or more riders), and never the player's exact look.
        public static RiderLook[] Field(int seed, int count, RiderLook player)
        {
            var r = new System.Random(unchecked(seed * 31 + 7)); var field = new RiderLook[count];
            for (int i = 0; i < count; i++)
            {
                RiderLook pick = null;
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    var look = Random(r); pick ??= look;
                    bool clash = look.SameAs(player) || (look.shirtColor == player.shirtColor && look.shirt == player.shirt)
                        || field.Take(i).Any(o => o.shirtColor == look.shirtColor || o.SameAs(look));
                    if (!clash) { pick = look; break; }
                }
                field[i] = pick;
            }
            if (count >= 2 && field.All(l => l.body == field[0].body)) field[r.Next(count)].body = 1 - field[0].body;
            return field;
        }
    }
}
