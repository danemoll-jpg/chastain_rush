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
        static bool initialized;
        static UnityEngine.SceneManagement.Scene registeredScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            surfaces = new(); vehicles = new(); ramps = new(); initialized = false; registeredScene = default; RampCorrections = 0;
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
                    if (!IsShortcutSurface(mesh.name)) continue;
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
                    nextRamps.Add(mesh.GetEntityId());
                    mesh.hasModifiableContacts = true;
                }
                surfaces = next; ramps = nextRamps; initialized = true; registeredScene = box.gameObject.scene;
                Debug.Log($"[VehicleSurfaceContacts] {next.Count} shortcut surfaces registered");
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
            var terrain = surfaces; var bodies = vehicles; var rampIds = ramps;
            for (int i = 0; i < pairs.Length; i++)
            {
                var pair = pairs[i];
                bool first = bodies.TryGetValue(pair.colliderEntityId, out var box);
                if (!first && !bodies.TryGetValue(pair.otherColliderEntityId, out box)) continue;
                var surfaceId = first ? pair.otherColliderEntityId : pair.colliderEntityId;
                if (ccd && !rampIds.Contains(surfaceId)) continue;
                if (!terrain.TryGetValue(surfaceId, out var faces)) continue;
                var rotation = first ? pair.rotation : pair.otherRotation;
                var center = (first ? pair.position : pair.otherPosition) + rotation * box.Center;
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
    }
}
