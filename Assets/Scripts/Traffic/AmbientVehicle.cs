using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    // Original low-poly traffic art. Four cached bodies; only an offscreen recycle
    // chooses a new body/paint. Motor, population, lanes and driver pace are unchanged.
    // Chastain Rush, Round 30 (A1): ported from Racer as it is (bodies, paints, the Blender kit TrafficSedan / Wagon / Pickup / Van
    // copied into Resources/VehicleModels). Edits: no split-screen (Offscreen looks through Camera.main only); Recycle also
    // takes a forced pick for a car placed while nobody can see it.
    public sealed class AmbientVehicle : MonoBehaviour
    {
        public int BodyType { get; private set; }
        public int PaintIndex { get; private set; }
        public int AppearanceRevision { get; private set; }
        public static readonly string[] BodyNames={"Sedan","Wagon / SUV","Pickup","Van"};
        public static readonly Color[] Paints={new(.78f,.8f,.79f),new(.13f,.17f,.21f),new(.48f,.51f,.53f),new(.45f,.09f,.07f),new(.08f,.22f,.38f),new(.18f,.29f,.22f),new(.64f,.55f,.39f),new(.88f,.85f,.73f),new(.3f,.19f,.14f)};
        static Material paint,glass,rubber,metal,tail;
        static readonly System.Random random=new();
        static readonly List<int> deck=new();
        static int previous=-1,previous2=-2,lastPaint=-1;
        readonly Transform[] bodies=new Transform[4];
        readonly List<Renderer>[] painted={new(),new(),new(),new()};
        // 0.81 Part C: the Blender traffic kit (sedan, wagon, pickup, van; Tools/Blender/traffic.py) shown with Model: New,
        // the classic bodies with Model: Classic. Same body types, colours, collider sizes and behaviour; visuals only.
        static readonly string[] Models={"TrafficSedan","TrafficWagon","TrafficPickup","TrafficVan"};
        readonly Transform[] modern=new Transform[4];
        readonly List<Renderer>[] modernPainted={new(),new(),new(),new()};
        readonly List<Transform> wheels=new();
        MaterialPropertyBlock block;
        float roll;
        ArcadeVehicle motor;
        static Material Mat(string name,Color color)=>new(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color,enableInstancing=true};
        public void Initialize()
        {
            block ??= new MaterialPropertyBlock();
            if(bodies[0])return;
            motor=GetComponent<ArcadeVehicle>();
            foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=false;
            if(!paint){paint=Mat("Ambient paint",Color.white);glass=Mat("Ambient glass",new(.065f,.12f,.16f));rubber=Mat("Ambient tires",new(.025f,.028f,.032f));metal=Mat("Ambient trim",new(.7f,.72f,.7f));tail=Mat("Ambient tail lamps",new(.5f,.025f,.025f));}
            for(int i=0;i<4;i++)
            {
                var root=new GameObject(BodyNames[i]).transform;root.SetParent(transform,false);bodies[i]=root;
                Part(i,"Lower body",new(0,0,0),new(1.85f,.55f,4.2f),paint);
                if(i<2)
                {
                    float length=i==0?1.85f:2.9f;
                    Part(i,"Glazed cabin",new(0,.57f,-.22f),new(1.65f,.65f,length),glass);
                    Part(i,"Painted roof",new(0,.93f,-.22f),new(1.72f,.12f,length+.08f),paint);
                    foreach(float x in new[]{-.84f,.84f})Part(i,"Window pillar",new(x,.56f,-.45f),new(.09f,.66f,.12f),paint);
                }
                else if(i==2)
                {
                    Part(i,"Pickup cab",new(0,.58f,.6f),new(1.72f,.7f,1.4f),glass);
                    Part(i,"Cab roof",new(0,.98f,.6f),new(1.8f,.13f,1.5f),paint);
                    Part(i,"Open bed floor",new(0,.31f,-1.1f),new(1.45f,.05f,1.6f),rubber);
                    foreach(float x in new[]{-.86f,.86f})Part(i,"Bed side",new(x,.53f,-1.15f),new(.14f,.5f,1.85f),paint);
                    Part(i,"Tailgate",new(0,.53f,-2.02f),new(1.85f,.5f,.14f),paint);
                }
                else
                {
                    Part(i,"Tall cargo box",new(0,.7f,-.3f),new(1.85f,1.4f,3.5f),paint);
                    Part(i,"Windshield",new(0,.96f,1.47f),new(1.62f,.62f,.035f),glass);
                    foreach(float x in new[]{-.94f,.94f})Part(i,"Cab side window",new(x,.96f,.96f),new(.025f,.6f,.85f),glass);
                    Part(i,"Rear door seam",new(0,.8f,-2.065f),new(.025f,1.12f,.025f),rubber);
                }
                Part(i,"Front bumper",new(0,-.08f,2.14f),new(1.85f,.15f,.1f),metal);
                foreach(float x in new[]{-.64f,.64f}){Part(i,"Headlight",new(x,.14f,2.12f),new(.4f,.19f,.035f),metal);Part(i,"Tail light",new(x,.12f,-2.12f),new(.26f,.2f,.035f),tail);}
                foreach(float x in new[]{-.92f,.92f})foreach(float z in new[]{-1.35f,1.35f})
                {var wheel=Part(i,"Wheel",new(x,-.22f,z),new(.66f,.12f,.66f),rubber,PrimitiveType.Cylinder);wheel.localRotation=Quaternion.Euler(0,0,90);wheels.Add(wheel);}
            }
            for(int i=0;i<4;i++){modern[i]=VehicleVisual.TrafficModel(transform,Models[i],wheels,modernPainted[i],paint);if(modern[i])modern[i].gameObject.SetActive(false);}
            Select();
        }
        Transform Part(int body,string name,Vector3 p,Vector3 size,Material material,PrimitiveType primitive=PrimitiveType.Cube)
        {
            var part=GameObject.CreatePrimitive(primitive);part.name=name;part.layer=gameObject.layer;
            part.transform.SetParent(bodies[body],false);part.transform.localPosition=p;part.transform.localScale=size;
            var collider=part.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            var renderer=part.GetComponent<Renderer>();renderer.sharedMaterial=material;if(material==paint)painted[body].Add(renderer);
            return part.transform;
        }
        void Select()
        {
            if(deck.Count==0){deck.AddRange(new[]{0,0,0,0,0,0,1,1,1,1,2,2,2,3,3,3});for(int i=deck.Count-1;i>0;i--){int j=random.Next(i+1);(deck[i],deck[j])=(deck[j],deck[i]);}}
            int at=deck.Count-1;
            if(previous==previous2&&deck[at]==previous){int other=deck.FindIndex(b=>b!=previous);if(other>=0)at=other;}
            BodyType=deck[at];deck.RemoveAt(at);previous2=previous;previous=BodyType;
            PaintIndex=random.Next(Paints.Length-1);if(PaintIndex>=lastPaint&&lastPaint>=0)PaintIndex++;lastPaint=PaintIndex;
            bool kit=VehicleVisual.NewModels&&modern[BodyType];
            for(int i=0;i<bodies.Length;i++){bodies[i].gameObject.SetActive(!kit&&i==BodyType);if(modern[i])modern[i].gameObject.SetActive(kit&&i==BodyType);}
            block.SetColor("_BaseColor",Paints[PaintIndex]);foreach(var r in kit?modernPainted[BodyType]:painted[BodyType])r.SetPropertyBlock(block);
            var box=GetComponent<BoxCollider>();
            if(box){float height=BodyType==3?1.72f:BodyType==2?1.32f:1.28f;box.size=new(2.08f,height,4.3f);box.center=new(0,(height-.55f)*.5f,0);motor.Body.ResetInertiaTensor();}
            AppearanceRevision++;
        }
        public static bool Offscreen(Vector3 point)
        {
            var camera=Camera.main;if(!camera)return false;var view=camera.WorldToViewportPoint(point);
            return view.z< -8||view.x<-.2f||view.x>1.2f||view.y<-.2f||view.y>1.2f;
        }
        public void Recycle(Vector3 destination,bool force=false)
        {if(force||Offscreen(transform.position)&&Offscreen(destination))Select();}
        void LateUpdate()
        {
            roll+=(motor?motor.ForwardSpeed:15)*Time.deltaTime/.33f*Mathf.Rad2Deg;
            foreach(var wheel in wheels)if(wheel.gameObject.activeInHierarchy)wheel.localRotation=Quaternion.Euler(roll,0,90);
        }
    }
}
