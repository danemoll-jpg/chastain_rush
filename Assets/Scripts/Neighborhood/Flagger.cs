using Racer;
using UnityEngine;

namespace ChastainRush
{
    // Round 53 (A, Dan 2026-10-21 01:07: "on the Lyndon drive shortcut, I want there to be a man, right before the ramp but to the left of it. He
    // should be waving a handheld stop sign, motioning to the driver to stop"): the road crew's flagger. A worker in a hard hat and a hi-vis vest,
    // a STOP / SLOW paddle on its pole in his right hand (STOP toward the oncoming driver), rocking it, his free arm out with the palm up toward the
    // driver, pumping in the stop motion, over and over. Built from primitives as the game's other people (AmbientLife); the paddle's two faces are
    // octagon / diamond meshes with their own materials (Assets/HandMade/RoadCrew/, Tools/handmade/make_stop_paddle.py), one-sided and depth-tested.
    // A vehicle coming at him: he steps back off the road onto the lawn (as the residents do) and comes back when it has gone. No collider: a vehicle
    // never stops on him. World, not race layer (he works there in Free Roam too).
    public sealed class Flagger : MonoBehaviour
    {
        public Vector3 facing = Vector3.back;     // toward the oncoming driver (x, z)
        public Vector3 stepAway = Vector3.left;   // where he steps to when a vehicle comes at him (off the road)
        public Material stopFace, slowFace;
        public int Dodges { get; private set; }   // checks
        Transform body, signArm, freeArm, forearm, paddle;
        Vector3 home, from, to; float dodgeAt = -10f;
        Material Mat(Color c) => new(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
        Transform Part(Transform parent, string name, PrimitiveType type, Vector3 p, Vector3 scale, Material mat)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = p; g.transform.localScale = scale;
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; return g.transform;
        }

        void Awake()
        {
            home = transform.position;
            body = new GameObject("Flagger body").transform; body.SetParent(transform, false);
            Material skin = Mat(new(.62f, .42f, .30f)), jeans = Mat(new(.17f, .22f, .32f)), shirt = Mat(new(.30f, .30f, .32f)), vest = Mat(new(.78f, .95f, .12f)),
                     stripe = Mat(new(.86f, .87f, .88f)), hat = Mat(new(.98f, .78f, .08f)), boot = Mat(new(.30f, .20f, .12f)), pole = Mat(new(.85f, .85f, .82f));
            foreach (int side in new[] { -1, 1 })
            {
                Part(body, "Leg", PrimitiveType.Capsule, new(side * .12f, .45f, 0), new(.17f, .40f, .19f), jeans);
                Part(body, "Boot", PrimitiveType.Cube, new(side * .12f, .07f, .05f), new(.2f, .14f, .32f), boot);
            }
            Part(body, "Torso", PrimitiveType.Capsule, new(0, 1.13f, 0), new(.44f, .35f, .30f), shirt);
            Part(body, "Vest", PrimitiveType.Capsule, new(0, 1.15f, 0), new(.47f, .30f, .33f), vest);
            Part(body, "Vest stripe", PrimitiveType.Cube, new(0, 1.05f, 0), new(.49f, .05f, .34f), stripe);
            Part(body, "Vest stripe", PrimitiveType.Cube, new(0, 1.22f, 0), new(.49f, .05f, .34f), stripe);
            Part(body, "Head", PrimitiveType.Sphere, new(0, 1.63f, 0), new(.27f, .31f, .28f), skin);
            Part(body, "Hard hat", PrimitiveType.Sphere, new(0, 1.74f, 0), new(.32f, .2f, .34f), hat);
            Part(body, "Hard hat brim", PrimitiveType.Cylinder, new(0, 1.71f, .02f), new(.38f, .012f, .40f), hat);
            // the sign arm (right): the pole held upright in front of him, the paddle over his head height
            signArm = new GameObject("Sign shoulder").transform; signArm.SetParent(body, false); signArm.localPosition = new(.25f, 1.36f, 0);
            Part(signArm, "Sleeve", PrimitiveType.Capsule, new(0, -.2f, .06f), new(.15f, .22f, .15f), shirt);
            Part(signArm, "Hand", PrimitiveType.Sphere, new(0, -.42f, .14f), new(.12f, .13f, .12f), skin);
            paddle = new GameObject("Paddle").transform; paddle.SetParent(signArm, false); paddle.localPosition = new(0, -.42f, .16f);
            Part(paddle, "Pole", PrimitiveType.Cylinder, new(0, .55f, 0), new(.035f, .95f, .035f), pole);
            var face = new GameObject("STOP face").transform; face.SetParent(paddle, false); face.localPosition = new(0, 1.72f, .025f);
            Octagon(face, stopFace);
            var back = new GameObject("SLOW face").transform; back.SetParent(paddle, false); back.localPosition = new(0, 1.72f, -.025f); back.localRotation = Quaternion.Euler(0, 180, 0);
            Octagon(back, slowFace);
            // the free arm (left): out toward the driver, palm up, pumping
            freeArm = new GameObject("Free shoulder").transform; freeArm.SetParent(body, false); freeArm.localPosition = new(-.26f, 1.38f, 0);
            Part(freeArm, "Upper arm", PrimitiveType.Capsule, new(0, -.15f, 0), new(.15f, .17f, .15f), shirt);
            forearm = new GameObject("Elbow").transform; forearm.SetParent(freeArm, false); forearm.localPosition = new(0, -.3f, 0);
            Part(forearm, "Forearm", PrimitiveType.Capsule, new(0, -.14f, 0), new(.13f, .16f, .13f), skin);
            Part(forearm, "Palm", PrimitiveType.Cube, new(0, -.32f, 0), new(.12f, .16f, .04f), skin);
            var lod = gameObject.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.006f, GetComponentsInChildren<Renderer>()) }); lod.RecalculateBounds();
            Face(facing);
        }

        // a paddle face: an octagon 0.61 m across its flats (a 24 in paddle; the SLOW side's orange diamond is printed on its own octagon), its
        // texture's square mapped onto it; one-sided, seen from its local +z (the STOP side toward the driver, the SLOW side back toward him)
        static void Octagon(Transform t, Material m)
        {
            const float R = .305f; const int n = 8;
            var v = new Vector3[n + 1]; var uv = new Vector2[n + 1]; var tri = new int[n * 3];
            v[0] = Vector3.zero; uv[0] = new Vector2(.5f, .5f);
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI / 8f + i * Mathf.PI * 2f / n, rv = R / Mathf.Cos(Mathf.PI / 8f);
                // seen from +z the viewer's right is -x, so u runs against x
                v[i + 1] = new Vector3(Mathf.Cos(a) * rv, Mathf.Sin(a) * rv, 0f); uv[i + 1] = new Vector2(.5f - Mathf.Cos(a) * rv / (2f * R), .5f + Mathf.Sin(a) * rv / (2f * R));
                tri[i * 3] = 0; tri[i * 3 + 1] = 1 + i; tri[i * 3 + 2] = 1 + (i + 1) % n;
            }
            var mesh = new Mesh { name = t.name, vertices = v, uv = uv, triangles = tri }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = t.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        void Face(Vector3 d) { d.y = 0f; if (d.sqrMagnitude > 1e-4f) body.rotation = Quaternion.LookRotation(d); }

        static float Ground(Vector3 p) => Physics.Raycast(p + Vector3.up * 2.5f, Vector3.down, out var hit, 6f, 1, QueryTriggerInteraction.Ignore) ? hit.point.y : p.y;

        void Update()
        {
            if (Time.timeScale == 0f) return;
            float t = Time.time;
            // a vehicle coming at him: off the road onto the lawn (0.5 s), wait, back (1.5 s)
            ArcadeVehicle near = null; float nd = 40f;
            foreach (var c in DrainPipe.Vehicles) { if (!c || !c.Body) continue; float d = Vector3.Distance(c.Body.position, transform.position); if (d < nd) { nd = d; near = c; } }
            if (near && t - dodgeAt > 4f)
            {
                var v = near.Body.linearVelocity; v.y = 0f; var at = transform.position - near.Body.position; at.y = 0f;
                if (v.magnitude > 3f && nd < 6f + v.magnitude * .8f && Vector3.Dot(v.normalized, at.normalized) > .8f && Vector3.Cross(v.normalized, at).magnitude < 2.6f)
                { dodgeAt = t; from = transform.position; to = home + stepAway.normalized * 3.4f; Dodges++; }
            }
            float since = t - dodgeAt;
            if (since < 4f)
            {
                var p = since < .5f ? Vector3.Lerp(from, to, since / .5f) : since < 2.5f ? to : Vector3.Lerp(to, home, (since - 2.5f) / 1.5f);
                p.y = Ground(p); transform.position = p;
            }
            else if ((transform.position - home).sqrMagnitude > 1e-4f) { var p = home; p.y = Ground(p); transform.position = p; }
            Face(near && nd < 25f ? near.Body.position - transform.position : facing);
            // the stop motion: the free arm out and up, the forearm pumping, palm to the driver; the paddle rocked a little side to side
            float pump = Mathf.Sin(t * 5.2f);
            freeArm.localRotation = Quaternion.Euler(-80f - 8f * pump, 0f, -12f);
            forearm.localRotation = Quaternion.Euler(-35f - 30f * Mathf.Max(0f, pump), 0f, 0f);
            signArm.localRotation = Quaternion.Euler(-20f, 0f, 6f * Mathf.Sin(t * 1.7f));
            paddle.localRotation = Quaternion.Euler(20f, 0f, 0f);
        }
    }
}
