using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    // Surface geometry and immersion share the same local footprint. The bed is authored separately.
    public sealed class ShallowWater : MonoBehaviour
    {
        public bool round;
        // Chastain Rush, Round 15: a water area can also be an outline (world x, z) for the map's ponds, lakes and pool. The
        // surface is then surfaceY (Surface below) and Contains tests the outline; boxes (round or square) are as in Racer.
        public Vector2[] outline;
        public float surfaceY;
        Rect bounds;
        public static readonly List<ShallowWater> Active = new();
        void Awake() { if (outline != null && outline.Length > 2) { Vector2 lo = outline[0], hi = outline[0]; foreach (var q in outline) { lo = Vector2.Min(lo, q); hi = Vector2.Max(hi, q); } bounds = Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y); } }
        bool Outlined => outline != null && outline.Length > 2;
        // Chastain Rush, Round 15: true when p is in water (inside an area and below its surface); a reset never lands there
        public static bool InWater(Vector3 p, float above = 0.3f)
        {
            foreach (var w in Active) if (w && w.Contains(p) && p.y < w.Surface + above && p.y > w.Surface - 3f) return true;
            return false;
        }
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
                if (!Outlined && (s.x >= 4 || s.z >= 4) && Vector3.Angle(transform.up, Vector3.up) < 5)
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
        public float Surface => Outlined ? surfaceY : transform.position.y + transform.lossyScale.y * .5f;
        public bool Contains(Vector3 point)
        {
            if (Outlined)   // Chastain Rush, Round 15
            {
                if (!bounds.Contains(new Vector2(point.x, point.z))) return false;
                bool inside = false;
                for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
                    if ((outline[i].y > point.z) != (outline[j].y > point.z) && point.x < (outline[j].x - outline[i].x) * (point.z - outline[i].y) / (outline[j].y - outline[i].y) + outline[i].x) inside = !inside;
                return inside;
            }
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
    // Chastain Rush, Round 15: Racer's WaterFeedback (ripple rings and a splash on going in and out), copied. Its ambience
    // volume setting (RaceFlow) is left behind (always 1), and the splash is AmbientLife.Sound's water noise, copied below.
    public sealed class WaterFeedback : MonoBehaviour
    {
        static AudioClip SoftWater(float seconds,int seed)
        {
            const int rate=22050;var data=new float[(int)(rate*seconds)];var rng=new System.Random(seed);
            for(int i=0;i<data.Length;i++){float envelope=Mathf.Sin(Mathf.PI*i/data.Length);data[i]=((float)rng.NextDouble()*2-1)*.55f*envelope;}
            var clip=AudioClip.Create("Soft water",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        ArcadeVehicle car; LineRenderer[] rings; AudioSource sound; AudioClip splash;
        public int EntrySounds {get;private set;}
        public int ExitSounds {get;private set;}
        public bool FeedbackActive {get {if(sound&&sound.isPlaying)return true;if(rings!=null)foreach(var ring in rings)if(ring.enabled)return true;return false;}}
        float nextRipple, nextSound, previous; int index; readonly float[] born=new float[6];readonly Vector3[] centers=new Vector3[6];
        void Awake()
        {
            car=GetComponent<ArcadeVehicle>(); rings=new LineRenderer[6];
            // AI is cloned from the player. Replace inherited transient children and keep
            // our source below the vehicle's root-only engine-audio cleanup.
            foreach(Transform child in transform)if(child.name=="Water feedback"||child.name=="Water ripple"){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var pool=new GameObject("Water feedback");pool.transform.SetParent(transform,false);
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){color=new Color(.50f,.76f,.78f)};
            for(int i=0;i<rings.Length;i++)
            {
                var go=new GameObject("Water ripple");go.transform.SetParent(pool.transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.loop=true;line.positionCount=16;line.widthMultiplier=.035f;line.useWorldSpace=true;line.enabled=false;rings[i]=line;born[i]=-100;
            }
            sound=pool.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=1;sound.minDistance=5;sound.maxDistance=45;sound.dopplerLevel=0;
            splash=SoftWater(.32f,73);sound.clip=splash;
        }
        void Update()
        {
            // Frozen water: no ripples or splash sounds (the vehicle is on the ice).
            float wet=ShallowWater.Frozen?0:car.WaterImmersion;
            if((wet>.08f)!=(previous>.08f) && Time.time>nextSound)
            { sound.volume=.12f; sound.pitch=wet>previous?1:.8f;if(wet>previous)EntrySounds++;else ExitSounds++;sound.Play();nextSound=Time.time+.25f; }
            if(wet>.08f && car.Body.linearVelocity.magnitude>.4f && Time.time>nextRipple)
            {born[index]=Time.time;centers[index]=new Vector3(transform.position.x,car.WaterSurface+.035f,transform.position.z);index=(index+1)%rings.Length;nextRipple=Time.time+.22f;}
            for(int i=0;i<rings.Length;i++)
            {
                float age=Time.time-born[i];rings[i].enabled=wet>.01f&&age<.85f;
                if(!rings[i].enabled)continue;
                float r=.3f+age*1.7f;rings[i].widthMultiplier=.035f*(1-age/.85f);
                for(int j=0;j<16;j++){float a=j*Mathf.PI/8;rings[i].SetPosition(j,centers[i]+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));}
            }
            previous=wet;
        }
        public void Clear(){previous=0;nextRipple=nextSound=Time.time+.4f;if(sound)sound.Stop();if(rings!=null)for(int i=0;i<rings.Length;i++){rings[i].enabled=false;born[i]=-100;}}
        void OnDisable()=>Clear();
        void OnDestroy(){if(splash)Destroy(splash);if(rings!=null&&rings.Length>0&&rings[0])Destroy(rings[0].sharedMaterial);}
    }
}
