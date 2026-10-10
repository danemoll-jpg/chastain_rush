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
        static bool initialized;
        static UnityEngine.SceneManagement.Scene registeredScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            surfaces = new(); vehicles = new(); ramps = new(); fields = new(); initialized = false; registeredScene = default; RampCorrections = 0; TerrainCorrections = 0;
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
                var next = new Dictionary<EntityId, Face[]>(); var nextRamps = new HashSet<EntityId>();
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
                    if (ramp) nextRamps.Add(mesh.GetEntityId());
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
                surfaces = next; ramps = nextRamps; fields = nextFields; initialized = true; registeredScene = box.gameObject.scene;
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
            var terrain = surfaces; var bodies = vehicles; var rampIds = ramps; var tiles = fields;
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
                for (int k = 0; k < pair.contactCount; k++)
                {
                    uint face = pair.GetFaceIndex(k);
                    if (face >= faces.Length) continue;
                    var surface = faces[face]; var n = surface.Normal;
                    // sides, undersides and obstacles keep their exact solver response
                    if (n.y < .55f || Vector3.Dot(center - surface.Point, n) < 0) continue;
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
            for (int k = 0; k < pair.contactCount; k++)
            {
                var n = Sample(f, pair.GetPoint(k), out _);
                if (n.y < .55f) continue;   // a steep bank where PhysX found the contact: its exact response
                // the body's lowest corner towards that surface, and the terrain's real height and slope right under it
                var corner = center - ax * (Mathf.Sign(Vector3.Dot(ax, n)) * box.Half.x) - ay * (Mathf.Sign(Vector3.Dot(ay, n)) * box.Half.y) - az * (Mathf.Sign(Vector3.Dot(az, n)) * box.Half.z);
                n = Sample(f, corner, out float ground);
                // sides of steep banks and anything under the surface keep their exact solver response
                if (n.y < .55f || center.y < ground) continue;
                var normal = first ? n : -n;
                if (Vector3.Dot(pair.GetNormal(k), normal) <= .98f) pair.SetNormal(k, normal);
                // the gap is measured straight down to the terrain under the corner, not to the plane of a face further on:
                // a predicted contact with the bank below a bridge end (Needle 600, New Chastain Road, x 2421) is not a hit.
                // Never deeper than PhysX measured it (Racer 0.85 Part C)
                pair.SetSeparation(k, Mathf.Max(pair.GetSeparation(k), (corner.y - ground) * n.y));
                System.Threading.Interlocked.Increment(ref TerrainCorrections);
            }
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
