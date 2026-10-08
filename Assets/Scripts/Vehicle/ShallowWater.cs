using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    // Surface geometry and immersion share the same local footprint. The bed is authored separately.
    public sealed class ShallowWater : MonoBehaviour
    {
        public bool round;
        public static readonly List<ShallowWater> Active = new();
        void OnEnable() { if (!Active.Contains(this)) Active.Add(this); Freeze(Frozen); }
        void OnDisable() => Active.Remove(this);
        // 0.73 Snow: every body of water freezes (WorldLook sets this from the conditions). Vehicles then ride ON the ice:
        // a collidable top face, the same shape as the water, is switched on. The water still slows a vehicle exactly as
        // much as it does today (Sample measures the depth as if the vehicle were on the bed under the ice); no grip change.
        // Small drain footprints (water a few cm deep) and steep sloped channels (Fern creek: its face is buried except
        // where it hangs over drops) get no ice collider: a vehicle there stays on the ground as now.
        public static bool Frozen { get; private set; }
        public static void SetFrozen(bool on) { if (on == Frozen) return; Frozen = on; foreach (var w in Active) if (w) w.Freeze(on); }
        MeshCollider ice; bool iceChecked;
        public Collider Ice => ice && ice.enabled ? ice : null;
        void Freeze(bool on)
        {
            if (on && !iceChecked)
            {
                iceChecked = true;
                var s = transform.lossyScale;
                if ((s.x >= 4 || s.z >= 4) && Vector3.Angle(transform.up, Vector3.up) < 5)
                {
                    var go = new GameObject("Frozen water surface (ice)"); go.layer = 0; go.transform.SetParent(transform, false);
                    var v = new List<Vector3>(); var t = new List<int>();
                    if (round) { v.Add(new Vector3(0, .5f, 0)); for (int i = 0; i < 48; i++) { float a = i * Mathf.PI * 2 / 48; v.Add(new Vector3(Mathf.Cos(a) * .5f, .5f, Mathf.Sin(a) * .5f)); t.AddRange(new[] { 0, 1 + (i + 1) % 48, 1 + i }); } }
                    else { v.AddRange(new[] { new Vector3(-.5f, .5f, -.5f), new Vector3(-.5f, .5f, .5f), new Vector3(.5f, .5f, .5f), new Vector3(.5f, .5f, -.5f) }); t.AddRange(new[] { 0, 1, 2, 0, 2, 3 }); }
                    var mesh = new Mesh { name = "Ice surface" }; mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    ice = go.AddComponent<MeshCollider>(); ice.sharedMesh = mesh;
                }
            }
            if (ice) ice.enabled = on;
        }
        void OnDestroy() { if (ice) Destroy(ice.sharedMesh); }
        public float Surface => transform.position.y + transform.lossyScale.y * .5f;
        public bool Contains(Vector3 point)
        {
            var p = transform.InverseTransformPoint(point);
            return round ? p.x*p.x+p.z*p.z < .25f : Mathf.Abs(p.x)<.5f && Mathf.Abs(p.z)<.5f;
        }
        public static float Sample(ArcadeVehicle car, out float surface)
        {
            surface=0; float result=0;
            foreach(var water in Active)
            {
                if(!water || water.gameObject.scene!=car.gameObject.scene || !water.Contains(car.Body.position)) continue;
                // Use wheel/body immersion, never an x/z-only trigger. Overflying jumps stay dry.
                float bottom=car.Body.position.y-car.suspensionLength;
                // Frozen: a vehicle over the ice is measured as if it stood the same height above the bed under the ice,
                // so the slowdown (and the dry overflight rule) is exactly that of the unfrozen water.
                var ice=water.Ice;
                if(ice && ice.Raycast(new Ray(car.Body.position+Vector3.up*.5f,Vector3.down),out var top,car.suspensionLength+2f))
                {
                    float bed=float.NegativeInfinity;
                    foreach(var hit in Physics.RaycastAll(top.point+Vector3.up*.02f,Vector3.down,4,car.groundMask,QueryTriggerInteraction.Ignore))
                        if(hit.collider!=ice&&!hit.rigidbody&&hit.point.y<=top.point.y)bed=Mathf.Max(bed,hit.point.y);
                    if(bed>float.NegativeInfinity)bottom-=top.point.y-bed;
                }
                float depth=water.Surface-bottom;
                if(depth<=0 || bottom<water.Surface-1.6f) continue;
                float amount=Mathf.Clamp01(depth/.65f);
                if(amount>result){result=amount;surface=water.Surface;}
            }
            return result;
        }
    }
}
