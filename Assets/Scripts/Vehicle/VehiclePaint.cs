using UnityEngine;

namespace Racer
{
    public static class VehiclePaint
    {
        // Validation captures every unpainted renderer, including water LineRenderers.
        // Unity owns a nonempty block on those even before paint. Test invariance,
        // rather than incorrectly treating an engine-owned block as paint leakage.
        public static System.Collections.Generic.Dictionary<Renderer,string> UnpaintedSnapshot(Transform root)
        {
            var result=new System.Collections.Generic.Dictionary<Renderer,string>();
            foreach(var r in root.GetComponentsInChildren<Renderer>())
            {
                var m=r.sharedMaterial;if(!m||IsBodyPaint(m))continue;
                var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);
                var text=new System.Text.StringBuilder(m.GetEntityId()+"/"+m.color+"/"+b.isEmpty);
                var shader=m.shader;
                for(int i=0;i<shader.GetPropertyCount();i++)
                {
                    int id=shader.GetPropertyNameId(i);text.Append('|').Append(id).Append(':').Append(b.HasProperty(id));
                    switch(shader.GetPropertyType(i))
                    {
                        case UnityEngine.Rendering.ShaderPropertyType.Color: text.Append(b.GetColor(id));break;
                        case UnityEngine.Rendering.ShaderPropertyType.Vector: text.Append(b.GetVector(id));break;
                        case UnityEngine.Rendering.ShaderPropertyType.Texture: text.Append(b.GetTexture(id)?.GetEntityId().ToString()??"none");break;
                        default: text.Append(b.GetFloat(id).ToString("R",System.Globalization.CultureInfo.InvariantCulture));break;
                    }
                }
                text.Append(b.GetColor("_BaseColor")).Append(b.GetColor("_Color")).Append(b.GetFloat("_Smoothness")).Append(b.GetFloat("_Metallic"));
                result.Add(r,text.ToString());
            }
            return result;
        }
        public static bool UnpaintedUnchanged(Transform root,System.Collections.Generic.Dictionary<Renderer,string> before)
        {
            var after=UnpaintedSnapshot(root);if(before.Count!=after.Count)return false;
            foreach(var pair in before)if(!after.TryGetValue(pair.Key,out var value)||value!=pair.Value)return false;
            return true;
        }
        public static readonly string[] Names={"Teal","Red","Gold","Blue","White","Violet","Black"};
        public static readonly Color[] Colors={new(.12f,.64f,.65f),new(.9f,.13f,.09f),new(.96f,.67f,.08f),new(.12f,.36f,.95f),new(.88f,.9f,.92f),new(.6f,.19f,.8f),new(.018f,.021f,.025f)};
        public static bool IsBodyPaint(Material material) => material && material.name.IndexOf("paint",System.StringComparison.OrdinalIgnoreCase)>=0;
        // 0.92 Part D: winning the Woodstock Grand Championship gives the champion's scheme, usable on any owned vehicle:
        // a metallic gold with a white roundel carrying a black "1" on each side. Its index follows the seven plain colours
        // (which the rider's clothes also use, so they stay seven).
        public const int Champion=7;
        static readonly Color ChampionGold=new(.86f,.63f,.13f);
        public static int Count=>Colors.Length;   // Chastain Rush: no campaign, so no champion scheme
        public static string Name(int i)=>i==Champion?"Champion gold  #1":Names[Mathf.Clamp(i,0,Names.Length-1)];
        public static Color Of(int i)=>i==Champion?ChampionGold:Colors[Mathf.Clamp(i,0,Colors.Length-1)];
        public static int Next(int i,int d){int n=Count;return (Mathf.Clamp(i,0,n-1)+d+n*4)%n;}
        // A colour or the champion's scheme (the plain gold when the scheme is not earned).
        public static void Scheme(Transform root,int i)
        {
            if(i==Champion)i=2;   // Chastain Rush: no campaign
            Apply(root,Of(i),i==Champion?.7f:.22f);Roundel(root,i==Champion);
        }
        static Material roundelWhite,roundelBlack;
        // The number roundels, one on each side of the painted body (built from the paint's own mesh bounds), or none.
        public static void Roundel(Transform root,bool on)
        {
            var old=root.Find("Champion roundel");if(old){old.gameObject.SetActive(false);Object.Destroy(old.gameObject);}
            if(!on)return;
            bool any=false;var min=Vector3.one*1e6f;var max=-min;
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!IsBodyPaint(r.sharedMaterial)||!r.gameObject.activeInHierarchy)continue;var mf=r.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;var b=mf.sharedMesh.bounds;
                for(int k=0;k<8;k++){var c=root.InverseTransformPoint(r.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((k&1)*2-1,(k&2)-1,(k&4)/2-1))));min=Vector3.Min(min,c);max=Vector3.Max(max,c);any=true;}
            }
            if(!any)return;
            if(!roundelWhite){var shader=Shader.Find("Universal Render Pipeline/Unlit");roundelWhite=new Material(shader){name="Champion roundel"};roundelWhite.SetColor("_BaseColor",new Color(.97f,.97f,.94f));roundelBlack=new Material(shader){name="Champion number"};roundelBlack.SetColor("_BaseColor",new Color(.03f,.03f,.03f));}
            var holder=new GameObject("Champion roundel").transform;holder.SetParent(root,false);holder.gameObject.layer=root.gameObject.layer;
            var centre=(min+max)*.5f;float radius=Mathf.Clamp(Mathf.Min(max.y-min.y,max.z-min.z)*.28f,.09f,.32f);
            for(int side=-1;side<=1;side+=2)
            {
                var face=new GameObject("Side").transform;face.SetParent(holder,false);face.gameObject.layer=root.gameObject.layer;
                face.localPosition=new Vector3(side<0?min.x-.006f:max.x+.006f,centre.y,centre.z);face.localRotation=Quaternion.LookRotation(new Vector3(-side,0,0));
                Part(face,"Disc",Disc(radius),roundelWhite,0);
                Part(face,"Number",One(radius),roundelBlack,-.003f);
            }
        }
        static void Part(Transform face,string name,Mesh mesh,Material m,float z){var g=new GameObject(name);g.layer=face.gameObject.layer;g.transform.SetParent(face,false);g.transform.localPosition=new Vector3(0,0,z);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
        static Mesh Disc(float radius)
        {
            var v=new Vector3[33];var t=new int[96];for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;v[i+1]=new Vector3(Mathf.Cos(a),Mathf.Sin(a))*radius;t[i*3]=0;t[i*3+1]=1+(i+1)%32;t[i*3+2]=1+i;}
            var m=new Mesh{name="Roundel disc",vertices=v,triangles=t};m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        // a "1": the stem, the flag and the foot
        static Mesh One(float radius)
        {
            float u=radius/10;var boxes=new[]{(x:-1f,y:-6f,w:2.4f,h:12f),(x:-3.4f,y:3.2f,w:2.6f,h:2f),(x:-3.4f,y:-6f,w:6.8f,h:1.8f)};
            var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();
            foreach(var b in boxes){int s=v.Count;v.Add(new Vector3(b.x,b.y)*u);v.Add(new Vector3(b.x+b.w,b.y)*u);v.Add(new Vector3(b.x+b.w,b.y+b.h)*u);v.Add(new Vector3(b.x,b.y+b.h)*u);t.AddRange(new[]{s,s+2,s+1,s,s+3,s+2});}
            var m=new Mesh{name="Roundel number"};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        public static void Apply(Transform root,Color color)=>Apply(root,color,.22f);
        public static void Apply(Transform root,Color color,float metallic)
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if(!IsBodyPaint(renderer.sharedMaterial)) continue;
                var block=new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor",color); block.SetColor("_Color",color);
                block.SetFloat("_Smoothness",.55f); block.SetFloat("_Metallic",metallic); renderer.SetPropertyBlock(block);
            }
        }
    }
}
