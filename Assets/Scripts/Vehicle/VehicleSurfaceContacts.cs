using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Racer
{
    // Round 10 (item 2j, "ramps must not slow the car"): copied from Racer's VehicleSurfaceContacts. On #6's kicker the car's
    // body touched the ramp near its foot and PhysX reported the contact with a normal tilted back against the travel (an
    // internal-edge normal of the mesh collider), which took 18 m/s down to 7 m/s in one step ("it got stuck at the bottom").
    // As in Racer, a contact from above on a registered surface uses that triangle's real face normal instead, never
    // removes the collision and never adds propulsion. Chastain Rush edits: the registered surfaces are the official
    // shortcuts' ramps, decks and floors (mesh colliders named "sc..."; all of them are ramps here), not Racer's course
    // names; Racer's "-rampLegacyContacts" test switch is dropped.
    public static class VehicleSurfaceContacts
    {
        struct Face { public Vector3 Normal, Point; }
        struct Box { public Vector3 Center, Half; }
        static volatile Dictionary<EntityId, Face[]> surfaces = new();
        static volatile Dictionary<EntityId, Box> vehicles = new();
        static volatile HashSet<EntityId> ramps = new();
        public static long RampCorrections;
        // Chastain Rush, Round 26 (item 4, driving on the walls): a drainage pipe's round barrel ("sc ... barrel") is driven on all the
        // way round. "From above" there is from above the VEHICLE (its own up), not the world's: on the walls and the ceiling the
        // faces' normals point sideways and down, so those contacts kept PhysX's internal-edge normals, tilted back against the travel,
        // and a body touching the wall it rode on lost 3 to 9 m/s a step (the Drifter Twin came off the wall). -wallRideRound25: as before
        static volatile HashSet<EntityId> barrels = new();
        static readonly bool Round25Walls = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-wallRideRound25") >= 0;
        public static long BarrelCorrections;
        // Round 14: Racer applies this to all of its driving ground (every "Ground_" mesh); here the ground is the terrain
        // (a TerrainCollider heightfield, which has no triangle list to read) and the bridge decks. Measured in the exe: the
        // small vehicles' bodies (speculative CCD, 15 to 22 cm clearance) touched the terrain in nearly every physics step and
        // PhysX gave some of those contacts horizontal normals, which took the Needle 600 from 30 to 12 m/s in one 20 ms step
        // and the Trail Four from 52 to 6 m/s in a second. Terrain contacts now get the heightfield's own surface normal (from a
        // copy of its heights, read on the physics worker without Unity calls), BridgeDecks its faces as the ramps do. Same rule
        // as Racer: only contacts from above on a face up to about 57 degrees steep, never removed, no propulsion.
        sealed class Field { public float[] H; public int Res; public float X, Z, StepX, StepZ; }
        static volatile Dictionary<EntityId, Field> fields = new();
        public static long TerrainCorrections;
        // Round 27 (A2): where the bridge decks are, as a grid of 1 m cells holding the deck's highest point in each. Terrain hidden
        // under a deck is not ground for a vehicle on top of that deck (its contact is ignored): the creek's banks under New Chastain
        // Road's decks, 0.1 to 1 m below them and rising at up to 30 degrees, were met as predicted contacts and kicked the Needle 600
        // up off the deck. A vehicle below a deck (driving under a bridge) keeps every terrain contact.
        static volatile Dictionary<long, float> deckCells = new();
        public static long UnderDeckIgnored;
        static long Cell(float x, float z) => ((long)Mathf.FloorToInt(x) << 32) ^ (uint)Mathf.FloorToInt(z);
        static bool initialized;
        static UnityEngine.SceneManagement.Scene registeredScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            surfaces = new(); vehicles = new(); ramps = new(); fields = new(); barrels = new(); deckCells = new(); UnderDeckIgnored = 0; initialized = false; registeredScene = default; RampCorrections = 0; TerrainCorrections = 0; BarrelCorrections = 0;
            Physics.ContactModifyEvent -= Correct;
            Physics.ContactModifyEvent += Correct;
            Physics.ContactModifyEventCCD -= CorrectCcd;
            Physics.ContactModifyEventCCD += CorrectCcd;
        }

        public static bool IsShortcutSurface(string n) => n.StartsWith("sc", System.StringComparison.Ordinal);

        public static void Register(BoxCollider box)
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-noSurfaceContacts") >= 0) { Debug.Log("[VehicleSurfaceContacts] off (-noSurfaceContacts)"); return; }   // checks only: measure without it
            if (!initialized || registeredScene != box.gameObject.scene)
            {
                var next = new Dictionary<EntityId, Face[]>(); var nextRamps = new HashSet<EntityId>(); var nextBarrels = new HashSet<EntityId>(); var nextDecks = new Dictionary<long, float>();
                foreach (var mesh in Object.FindObjectsByType<MeshCollider>())
                {
                    if (mesh.gameObject.scene != box.gameObject.scene) continue;
                    if (mesh.attachedRigidbody || !mesh.sharedMesh || !mesh.sharedMesh.isReadable) continue;
                    bool ramp = IsShortcutSurface(mesh.name);
                    if (!ramp && mesh.name != "BridgeDecks") continue;   // Round 14: the bridge decks are driving ground too
                    var vertices = mesh.sharedMesh.vertices; var indices = mesh.sharedMesh.triangles;
                    var t = mesh.transform; for (int v = 0; v < vertices.Length; v++) vertices[v] = t.TransformPoint(vertices[v]);
                    var faces = new Face[indices.Length / 3];
                    for (int i = 0; i < faces.Length; i++)
                    {
                        var a = vertices[indices[i * 3]];
                        var b = vertices[indices[i * 3 + 1]];
                        var c = vertices[indices[i * 3 + 2]];
                        faces[i] = new Face { Normal = Vector3.Cross(b - a, c - a).normalized, Point = a };
                    }
                    next[mesh.GetEntityId()] = faces;
                    if (mesh.name == "BridgeDecks")   // Round 27: every 1 m cell a deck triangle covers, with the deck's highest point there
                        for (int i = 0; i + 2 < indices.Length; i += 3)
                        {
                            Vector3 a = vertices[indices[i]], b = vertices[indices[i + 1]], c = vertices[indices[i + 2]];
                            float top = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
                            for (int cx = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))); cx <= Mathf.FloorToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))); cx++)
                                for (int cz = Mathf.FloorToInt(Mathf.Min(a.z, Mathf.Min(b.z, c.z))); cz <= Mathf.FloorToInt(Mathf.Max(a.z, Mathf.Max(b.z, c.z))); cz++)
                                {
                                    if (!InTriangle(new Vector2(cx + .5f, cz + .5f), a, b, c, .75f)) continue;
                                    long key = ((long)cx << 32) ^ (uint)cz;
                                    nextDecks[key] = nextDecks.TryGetValue(key, out float was) ? Mathf.Max(was, top) : top;
                                }
                        }
                    if (ramp) nextRamps.Add(mesh.GetEntityId());
                    if (ramp && !Round25Walls && mesh.name.EndsWith(" barrel", System.StringComparison.Ordinal)) nextBarrels.Add(mesh.GetEntityId());   // Round 26
                    mesh.hasModifiableContacts = true;
                }
                var nextFields = new Dictionary<EntityId, Field>();   // Round 14: the terrain tiles
                foreach (var tc in Object.FindObjectsByType<TerrainCollider>())
                {
                    if (tc.gameObject.scene != box.gameObject.scene || !tc.terrainData) continue;
                    var d = tc.terrainData; int res = d.heightmapResolution;
                    var h = d.GetHeights(0, 0, res, res); var flat = new float[res * res];
                    float y0 = tc.transform.position.y, sy = d.size.y;
                    for (int j = 0; j < res; j++) for (int i = 0; i < res; i++) flat[j * res + i] = y0 + h[j, i] * sy;
                    nextFields[tc.GetEntityId()] = new Field { H = flat, Res = res, X = tc.transform.position.x, Z = tc.transform.position.z, StepX = d.size.x / (res - 1), StepZ = d.size.z / (res - 1) };
                    tc.hasModifiableContacts = true;
                }
                surfaces = next; ramps = nextRamps; barrels = nextBarrels; fields = nextFields; deckCells = nextDecks; initialized = true; registeredScene = box.gameObject.scene;
                Debug.Log($"[VehicleSurfaceContacts] {nextRamps.Count} shortcut surfaces, {next.Count - nextRamps.Count} bridge deck meshes, {nextFields.Count} terrain tiles registered");
            }
            var copy = new Dictionary<EntityId, Box>(vehicles);
            copy[box.GetEntityId()] = new Box { Center = Vector3.Scale(box.center, box.transform.lossyScale), Half = Vector3.Scale(box.size * .5f, box.transform.lossyScale) };
            vehicles = copy;
            box.hasModifiableContacts = true;
        }

        public static void Unregister(BoxCollider box)
        { var copy = new Dictionary<EntityId, Box>(vehicles); copy.Remove(box.GetEntityId()); vehicles = copy; }

        static void Correct(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs) => CorrectPairs(pairs, false);
        // Unity exposes sweep-CCD normals inverted (Racer's note); only the registered ramps are handled there.
        static void CorrectCcd(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs) => CorrectPairs(pairs, true);

        static void CorrectPairs(NativeArray<ModifiableContactPair> pairs, bool ccd)
        {
            var terrain = surfaces; var bodies = vehicles; var rampIds = ramps; var tiles = fields; var barrelIds = barrels;
            for (int i = 0; i < pairs.Length; i++)
            {
                var pair = pairs[i];
                bool first = bodies.TryGetValue(pair.colliderEntityId, out var box);
                if (!first && !bodies.TryGetValue(pair.otherColliderEntityId, out box)) continue;
                var surfaceId = first ? pair.otherColliderEntityId : pair.colliderEntityId;
                if (ccd && !rampIds.Contains(surfaceId)) continue;
                var rotation = first ? pair.rotation : pair.otherRotation;
                var center = (first ? pair.position : pair.otherPosition) + rotation * box.Center;
                if (!ccd && tiles.TryGetValue(surfaceId, out var field)) { CorrectTerrain(ref pair, field, first, center, rotation, box); continue; }
                if (!terrain.TryGetValue(surfaceId, out var faces)) continue;
                bool barrel = barrelIds.Contains(surfaceId); var bodyUp = rotation * Vector3.up;   // Round 26
                for (int k = 0; k < pair.contactCount; k++)
                {
                    uint face = pair.GetFaceIndex(k);
                    if (face >= faces.Length) continue;
                    var surface = faces[face]; var n = surface.Normal;
                    // sides, undersides and obstacles keep their exact solver response (in a pipe's barrel: as seen from the vehicle)
                    if ((barrel ? Vector3.Dot(n, bodyUp) : n.y) < .55f || Vector3.Dot(center - surface.Point, n) < 0) continue;
                    if (barrel && n.y < .55f) System.Threading.Interlocked.Increment(ref BarrelCorrections);
                    var normal = first ? n : -n;
                    if (ccd) normal = -normal;
                    if (Vector3.Dot(pair.GetNormal(k), normal) > .98f) continue;
                    float radius = Mathf.Abs(Vector3.Dot(rotation * Vector3.right, n)) * box.Half.x
                        + Mathf.Abs(Vector3.Dot(rotation * Vector3.up, n)) * box.Half.y
                        + Mathf.Abs(Vector3.Dot(rotation * Vector3.forward, n)) * box.Half.z;
                    pair.SetNormal(k, normal);
                    // never deeper than PhysX measured it (Racer 0.85 Part C)
                    if (!ccd) pair.SetSeparation(k, Mathf.Max(pair.GetSeparation(k), Vector3.Dot(center - surface.Point, n) - radius));
                    if (rampIds.Contains(surfaceId)) System.Threading.Interlocked.Increment(ref RampCorrections);
                }
            }
        }

        // Round 14: the same correction against the terrain, with the heightfield's surface normal under the contact point
        static void CorrectTerrain(ref ModifiableContactPair pair, Field f, bool first, Vector3 center, Quaternion rotation, Box box)
        {
            Vector3 ax = rotation * Vector3.right, ay = rotation * Vector3.up, az = rotation * Vector3.forward;
            var decks = deckCells;
            for (int k = 0; k < pair.contactCount; k++)
            {
                // Round 27: terrain under a bridge deck, the vehicle on top of that deck: not ground
                var cp = pair.GetPoint(k);
                if (decks.TryGetValue(Cell(cp.x, cp.z), out float deckTop) && center.y > deckTop && cp.y < deckTop - .005f)
                { pair.IgnoreContact(k); System.Threading.Interlocked.Increment(ref UnderDeckIgnored); continue; }
                var n = Sample(f, pair.GetPoint(k), out _);
                if (n.y < .55f) continue;   // a steep bank where PhysX found the contact: its exact response
                // the body's lowest corner towards that surface, and the terrain's real height and slope right under it
                var corner = center - ax * (Mathf.Sign(Vector3.Dot(ax, n)) * box.Half.x) - ay * (Mathf.Sign(Vector3.Dot(ay, n)) * box.Half.y) - az * (Mathf.Sign(Vector3.Dot(az, n)) * box.Half.z);
                n = Sample(f, corner, out float ground);
                // sides of steep banks and anything under the surface keep their exact solver response
                if (n.y < .55f || center.y < ground) continue;
                // Round 27 (A2): terrain clearly below the body (15 cm or more straight down from its lowest corner) is only a predicted
                // contact, and it is predicted straight down: the normal up, the gap the vertical one. PhysX's own prediction used the
                // terrain's slope, so a bank under a bridge deck 0.5 to 1 m below the Needle 600, rising ahead at 30 degrees, closed on it
                // at 28 m/s along that slope at 56 m/s and kicked it up off the deck (vertical speed +5 m/s, 4 m/s lost; New Chastain
                // Road, x 2421 and 2441: Dan's "drop" at the bridge). Falling onto the ground still meets it with its vertical speed.
                float gap = corner.y - ground;
                if (gap >= .15f) n = Vector3.up;
                var normal = first ? n : -n;
                if (Vector3.Dot(pair.GetNormal(k), normal) <= .98f) pair.SetNormal(k, normal);
                // the gap is measured straight down to the terrain under the corner, not to the plane of a face further on:
                // a predicted contact with the bank below a bridge end (Needle 600, New Chastain Road, x 2421) is not a hit.
                // Never deeper than PhysX measured it (Racer 0.85 Part C)
                pair.SetSeparation(k, Mathf.Max(pair.GetSeparation(k), gap * n.y));
                System.Threading.Interlocked.Increment(ref TerrainCorrections);
            }
        }

        // a point in plan within a triangle grown by 'grow' m (a deck cell is kept if its middle is within 0.75 m of a deck triangle)
        static bool InTriangle(Vector2 p, Vector3 a3, Vector3 b3, Vector3 c3, float grow)
        {
            Vector2 a = new(a3.x, a3.z), b = new(b3.x, b3.z), c = new(c3.x, c3.z);
            float Side(Vector2 u, Vector2 v) { var e = v - u; float len = e.magnitude; return len < 1e-5f ? 0f : (e.x * (p.y - u.y) - e.y * (p.x - u.x)) / len; }
            float s0 = Side(a, b), s1 = Side(b, c), s2 = Side(c, a);
            bool ccw = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) > 0f;
            if (!ccw) { s0 = -s0; s1 = -s1; s2 = -s2; }
            return s0 >= -grow && s1 >= -grow && s2 >= -grow;
        }

        // the heightfield's height (bilinear) and surface normal at a point's x, z
        static Vector3 Sample(Field f, Vector3 p, out float height)
        {
            float fx = (p.x - f.X) / f.StepX, fz = (p.z - f.Z) / f.StepZ;
            int i = Mathf.Clamp((int)fx, 0, f.Res - 2), j = Mathf.Clamp((int)fz, 0, f.Res - 2);
            float a = Mathf.Clamp01(fx - i), b = Mathf.Clamp01(fz - j);
            float h00 = f.H[j * f.Res + i], h10 = f.H[j * f.Res + i + 1], h01 = f.H[(j + 1) * f.Res + i], h11 = f.H[(j + 1) * f.Res + i + 1];
            height = h00 * (1 - a) * (1 - b) + h10 * a * (1 - b) + h01 * (1 - a) * b + h11 * a * b;
            float dx = ((h10 - h00) * (1 - b) + (h11 - h01) * b) / f.StepX, dz = ((h01 - h00) * (1 - a) + (h11 - h10) * a) / f.StepZ;
            return new Vector3(-dx, 1f, -dz).normalized;
        }
    }
}
