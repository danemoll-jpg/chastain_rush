using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Racer
{
    // Original authored stylized meshes. Shared materials and per-material mesh merging
    // bound draw calls; every piece is visual only, with no collider changes.
    public static class VehicleVisual
    {
        static Material paint,rubber,glass,metal,lamps,tail,eyes,mouth,engine,chrome,interior,trim,cream,driverMat;
        // Garage "Model": true = the Blender-made models (0.73 Needle 600; 0.75 Trail Four, Street Classic, Longroof GT and
        // the customizable rider; Resources/VehicleModels, sources in SourceArt/Blender, scripts in Tools/Blender), false =
        // the classic generated ones (Bike / Car below, untouched). Applies to the player's and the AI vehicles; RaceFlow
        // sets it from the save. Ambient traffic always keeps the classic cars.
        public static bool NewModels = true;
        static Material[] hair,skins,shirts,trousers,shoes;
        static Material Mat(string name,Color color,float smooth=.3f)
        {var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color,enableInstancing=true};m.SetFloat("_Smoothness",smooth);return m;}
        static void Materials()
        {
            if(paint)return;
            paint=Mat("Garage body paint",new(.15f,.62f,.64f),.55f);rubber=Mat("Garage rubber",new(.035f,.043f,.054f));
            glass=Resources.Load<Material>("VehicleGlazing")??Mat("Garage glass",new(.18f,.3f,.37f,.28f),.85f);
            glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",5);glass.SetFloat("_DstBlend",10);glass.SetFloat("_ZWrite",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
            metal=Mat("Garage alloy",new(.48f,.56f,.60f),.65f);
            lamps=Mat("Garage headlamps",new(.95f,.91f,.7f));tail=Mat("Garage tail lamps",new(.65f,.025f,.028f));
            skins=new[]{Mat("Driver skin warm",new(.78f,.52f,.36f)),Mat("Driver skin deep",new(.39f,.22f,.15f)),Mat("Driver skin light",new(.91f,.69f,.53f))};
            shirts=new[]{Mat("Driver shirt blue",new(.14f,.36f,.70f)),Mat("Driver shirt cream",new(.83f,.80f,.65f)),Mat("Driver shirt green",new(.20f,.46f,.29f)),Mat("Driver shirt plum",new(.45f,.19f,.38f))};
            trousers=new[]{Mat("Driver trousers denim",new(.12f,.20f,.32f)),Mat("Driver trousers slate",new(.23f,.26f,.29f)),Mat("Driver trousers tan",new(.43f,.34f,.23f))};
            shoes=new[]{Mat("Driver shoes brown",new(.17f,.095f,.05f)),Mat("Driver shoes charcoal",new(.065f,.073f,.085f))};
            eyes=Mat("Driver eye whites",new(.96f,.95f,.90f));mouth=Mat("Driver smile",new(.24f,.075f,.065f));
            engine=Mat("Garage engine",new(.21f,.22f,.24f),.45f);
            chrome=Mat("Garage chrome",new(.78f,.80f,.83f),.82f);chrome.SetFloat("_Metallic",.6f);interior=Mat("Garage interior",new(.11f,.11f,.12f),.2f);
            trim=Mat("Rider trim",new(.10f,.08f,.06f),.25f);
            cream=Mat("Garage cream trim",new(.90f,.87f,.78f),.5f);driverMat=Mat("Traffic driver",new(.12f,.11f,.10f),.2f);
            hair=new[]{Mat("Driver hair chestnut",new(.18f,.065f,.028f)),Mat("Driver hair charcoal",new(.035f,.029f,.025f)),Mat("Driver hair gold",new(.62f,.36f,.09f))};
        }
        // 0.94 Part C: the patrol car's white doors and roof and its light bar lenses (PoliceLights flashes the lenses with
        // per-renderer emission)
        static Material white,sirenRed,sirenBlue;
        static Material White=>white?white:white=Mat("Patrol white",new(.93f,.93f,.91f),.55f);
        static Material SirenRed=>sirenRed?sirenRed:sirenRed=Lens("Light bar red",new(.75f,.05f,.05f));
        static Material SirenBlue=>sirenBlue?sirenBlue:sirenBlue=Lens("Light bar blue",new(.06f,.18f,.85f));
        static Material Lens(string name,Color c){var m=Mat(name,c,.8f);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;return m;}
        // look: the rider (null = the player's); classic: always the classic model (ambient traffic).
        public static Transform Build(Transform parent,VehicleProfile p,List<Transform> wheels=null,RiderLook look=null,bool classic=false)
        {
            Materials();var root=new GameObject("Vehicle visual").transform;root.SetParent(parent,false);root.gameObject.layer=parent.gameObject.layer;
            if(NewModels&&!classic&&NewModel(root,wheels,p,look??RiderLook.Player)){if(p.IsPolice)VehiclePaint.Apply(root,new Color(.03f,.03f,.035f));return root;} // 0.99: police vehicles are black and white in every preview too
            if(p.Small) Bike(root,p);else Car(root,p);
            // Merge fixed parts before adding independently rotating wheels.
            Merge(root);
            float[] tracks=p.Motorcycle?new[]{0f}:new[]{-p.Size.x*.5f,p.Size.x*.5f};
            foreach(float x in tracks)foreach(float z in new[]{-p.Wheelbase*.5f,p.Wheelbase*.5f})
            {
                var wheel=Part(root,"Wheel",new(x,-.2f,z),new(.66f,p.Motorcycle?.1f:.18f,.66f),rubber,PrimitiveType.Cylinder);
                wheel.localRotation=Quaternion.Euler(0,0,90);wheels?.Add(wheel);WheelDetail(wheel);
            }
            return root;
        }
        static void Car(Transform root,VehicleProfile p)
        {
            float w=p.Size.x,l=p.Size.z,h=p.Size.y;bool wagon=p.Id=="tourer";
            // Raised chassis/sills leave real visual wheel wells, with faceted arches.
            BeveledBody(root,new(0,.20f,0),new(w,h*.65f,l),.12f);
            Part(root,"Lower sill",new(0,-.08f,0),new(w*.92f,.25f,p.Wheelbase-.8f),paint);
            foreach(float end in new[]{-1f,1f})Part(root,"Bumper",new(0,-.10f,end*(l*.5f-.06f)),new(w*.96f,.16f,.15f),rubber);
            float front=.70f,rear=wagon?-1.52f:-.85f,roof=.98f;
            BeveledBody(root,new(0,roof,(front+rear)*.5f),new(w*.83f,.13f,front-rear),.05f);
            Part(root,"Windshield",new(0,.69f,front+.03f),new(w*.80f,.51f,.045f),glass).localRotation=Quaternion.Euler(16,0,0);
            Part(root,"Rear glass",new(0,.68f,rear-.03f),new(w*.8f,.48f,.045f),glass).localRotation=Quaternion.Euler(-12,0,0);
            foreach(float side in new[]{-1f,1f})
            {
                foreach(float z in new[]{front,rear,wagon?-.65f:-.5f})Part(root,"Window pillar",new(side*w*.415f,.7f,z),new(.065f,.57f,.085f),paint);
                Part(root,"Window sill",new(side*w*.46f,.4f,(front+rear)*.5f),new(.08f,.08f,front-rear+.15f),metal);
                Part(root,"Mirror",new(side*(w*.5f+.06f),.57f,.65f),new(.19f,.14f,.24f),paint);
                Part(root,"Door handle",new(side*(w*.5f+.006f),.22f,-.18f),new(.025f,.045f,.21f),metal);
                foreach(float axle in new[]{-p.Wheelbase*.5f,p.Wheelbase*.5f})Arch(root,side*w*.5f,axle,.41f);
                Part(root,"Headlamp",new(side*w*.33f,.23f,l*.5f+.005f),new(w*.23f,.17f,.045f),lamps);
                Part(root,"Tail lamp",new(side*w*.37f,.24f,-l*.5f-.005f),new(w*.17f,.16f,.045f),tail);
            }
            Part(root,"Grille",new(0,.06f,l*.5f+.006f),new(w*.42f,.14f,.04f),rubber);
            Part(root,"Rear plate",new(0,.1f,-l*.5f-.009f),new(.35f,.14f,.035f),metal);
            // Window openings expose a compact seated occupant, fully below the roof.
            Part(root,"Seat back",new(-.37f,.46f,.01f),new(.42f,.49f,.16f),rubber);
            Person(root,new(-.37f,.22f,.15f),.65f,false);
            Part(root,"Dashboard",new(0,.36f,.53f),new(w*.8f,.11f,.25f),rubber);
        }
        static void Bike(Transform root,VehicleProfile p)
        {
            bool moto=p.Motorcycle;float stance=moto?.22f:.48f;
            foreach(float side in new[]{-1f,1f})
            {
                Link(root,"Frame rail",new(side*stance,-.05f,-.68f),new(side*stance,.35f,.5f),.07f,metal);
                Link(root,"Swing arm",new(side*stance,-.12f,-.85f),new(side*stance,.06f,.1f),.075f,metal);
                Link(root,"Front fork",new(side*(moto?.14f:.51f),-.19f,.83f),new(side*.16f,.67f,.51f),.075f,metal);
                Part(root,"Footrest",new(side*(moto?.3f:.57f),.02f,-.15f),new(.2f,.065f,moto?.22f:.55f),rubber);
                Link(root,"Exhaust",new(side*stance,-.05f,.1f),new(side*stance,.1f,-.85f),.10f,metal);
            }
            Part(root,"Engine block",new(0,.05f,.06f),new(moto?.36f:.63f,.3f,.48f),rubber);
            for(int i=0;i<4;i++)Part(root,"Engine cooling fin",new(0,-.04f+i*.065f,.08f),new(moto?.41f:.67f,.02f,.40f),metal);
            Part(root,"Fuel tank",new(0,.40f,.33f),new(moto?.48f:.74f,.36f,.67f),paint,PrimitiveType.Sphere);
            Part(root,"Seat",new(0,.39f,-.37f),new(moto?.38f:.6f,.16f,.75f),rubber);
            Part(root,"Tail fairing",new(0,.30f,-.78f),new(moto?.38f:.85f,.18f,.28f),paint);
            Part(root,"Headlamp",new(0,.57f,.73f),new(moto?.28f:.6f,.22f,.14f),lamps);
            Part(root,"Tail lamp",new(0,.32f,-.94f),new(.26f,.10f,.035f),tail);
            Part(root,"Handlebar stem",new(0,.60f,.55f),new(.09f,.23f,.09f),metal);
            Part(root,"Handlebars",new(0,.72f,.58f),new(.92f,.065f,.065f),metal);
            foreach(float side in new[]{-1f,1f})Part(root,"Grip",new(side*.42f,.72f,.58f),new(.18f,.085f,.085f),rubber);
            if(moto){Arch(root,0,.82f,.39f);Arch(root,0,-.82f,.39f);}
            else foreach(float x in new[]{-.63f,.63f})foreach(float z in new[]{-.82f,.82f}){Part(root,"Fender",new(x,.19f,z),new(.48f,.13f,.72f),paint);}
            if(!moto){Part(root,"Front cargo rack",new(0,.40f,.91f),new(.95f,.065f,.30f),metal);Part(root,"Rear cargo rack",new(0,.4f,-.91f),new(.95f,.065f,.28f),metal);}
            Person(root,new(0,.5f,-.32f),1,true,moto?.32f:.55f);
        }
        // ---------- Blender models (0.73 Needle 600, 0.75 the rest) ----------
        // Each FBX holds objects "<Group>__<slot>". Body stays fixed; Front (Needle fork and bars, Trail Four handlebars)
        // turns about its steering axis; Steer (car steering wheel) turns about its column; Wheel* get one spinning pivot
        // each (front ones also steer, as the classic wheels do); the Needle's own 0.73 rider ("Rider") is replaced by the
        // parametric rider. Every slot gets the game's own shared material: paint is the body colour, lamps glow at night.
        static readonly Dictionary<string,GameObject> models=new();
        static GameObject Model(string name){if(!models.TryGetValue(name,out var m)){m=Resources.Load<GameObject>("VehicleModels/"+name);models[name]=m;}return m;}
        // Car steering column direction (Tools/Blender/cars.py); the steering pivots and seat points are in VehicleProfile.
        static readonly Vector3 CarColumn=new(0,-.4226f,.9063f);
        static bool NewModel(Transform root,List<Transform> wheels,VehicleProfile p,RiderLook look)
        {
            var asset=string.IsNullOrEmpty(p.Model)?null:Model(p.Model);
            if(!asset)return false;
            // Blender's default FBX axes arrive in Unity turned half a turn about the vertical (a rotation, not a mirror).
            var model=Object.Instantiate(asset,root,false);model.name=p.Name+" model";model.transform.localRotation=Quaternion.Euler(0,180,0);
            var pose=new GameObject("Steering pose").transform;pose.SetParent(root,false);pose.gameObject.AddComponent<VehiclePose>().bike=p.Small&&p.Pose!="Car";
            Transform front=null;
            // the bike front end / ATV bars turn about their steering axis (VehicleProfile.Front*)
            if(p.FrontGain>0){front=new GameObject(p.Motorcycle?"Front end":"Handlebars").transform;front.SetParent(root,false);front.localPosition=p.FrontPivot;var turn=front.gameObject.AddComponent<MotorcycleFrontEnd>();turn.axis=p.FrontAxis;turn.gain=p.FrontGain;}
            foreach(var r in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                var t=r.transform;var parts=t.name.Split(new[]{"__"},System.StringSplitOptions.None);if(parts.Length!=2)continue;
                string group=parts[0],slot=parts[1];
                if(group=="Rider"){t.gameObject.SetActive(false);Object.Destroy(t.gameObject);continue;}
                r.sharedMaterial=slot switch{"paint"=>paint,"cream"=>cream,"driver"=>driverMat,"metal"=>metal,"engine"=>engine,"rubber"=>rubber,"lamp"=>lamps,"tail"=>tail,"glass"=>glass,"chrome"=>chrome,"interior"=>interior,"white"=>White,"sirenred"=>SirenRed,"sirenblue"=>SirenBlue,_=>metal};
                if(group=="Front"&&front)t.SetParent(front,true);
                else if(group=="Steer")
                {
                    // Turns about the column with the visual steering (top of the wheel to the right for a right turn).
                    var pivot=new GameObject("Steering wheel").transform;pivot.SetParent(root,false);pivot.position=t.position;
                    var turn=pivot.gameObject.AddComponent<MotorcycleFrontEnd>();turn.axis=CarColumn;turn.gain=-40;t.SetParent(pivot,true);
                }
                else if(group.StartsWith("Wheel"))
                {
                    // One spinning pivot per wheel, oriented like the classic wheels (axle along local y after the
                    // Euler(roll,0,90) VehicleConfiguration applies), so wheel spin and steering need no other change.
                    var host=p.Motorcycle&&group=="WheelFront"&&front?front:root;var pivot=host.Find(group);
                    if(!pivot){pivot=new GameObject(group).transform;pivot.SetParent(host,false);pivot.position=t.position;pivot.localRotation=Quaternion.Euler(0,0,90);wheels?.Add(pivot);}
                    t.SetParent(pivot,true);
                }
            }
            Rider(pose,p.Pose,p.Pose=="Car"?p.Seat:Vector3.zero,look);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=root.gameObject.layer;
            return true;
        }
        // ---------- 0.81 Part C: the Blender traffic kit (Tools/Blender/traffic.py) ----------
        // One traffic body under its own root (visual only): slot materials as for the player's vehicles (lamps glow at
        // night, the driver silhouette is dark), the paint slot gets the traffic's own paint material (painted lists those
        // renderers for its colour), and each wheel gets a spinning pivot oriented like the classic traffic wheels.
        public static Transform TrafficModel(Transform parent,string name,List<Transform> wheels,List<Renderer> painted,Material paintMaterial)
        {
            Materials();var asset=Model(name);if(!asset)return null;
            var root=new GameObject(name).transform;root.SetParent(parent,false);
            var model=Object.Instantiate(asset,root,false);model.name=name+" model";model.transform.localRotation=Quaternion.Euler(0,180,0);
            foreach(var r in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                var t=r.transform;var parts=t.name.Split(new[]{"__"},System.StringSplitOptions.None);if(parts.Length!=2)continue;
                string group=parts[0],slot=parts[1];
                r.sharedMaterial=slot switch{"paint"=>paintMaterial,"driver"=>driverMat,"metal"=>metal,"engine"=>engine,"rubber"=>rubber,"lamp"=>lamps,"tail"=>tail,"glass"=>glass,"chrome"=>chrome,"interior"=>interior,_=>metal};
                if(slot=="paint")painted.Add(r);
                r.shadowCastingMode=group.StartsWith("Wheel")?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;
                if(group.StartsWith("Wheel"))
                {
                    var pivot=root.Find(group);
                    if(!pivot){pivot=new GameObject(group).transform;pivot.SetParent(root,false);pivot.position=t.position;pivot.localRotation=Quaternion.Euler(0,0,90);wheels.Add(pivot);}
                    t.SetParent(pivot,true);
                }
            }
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=parent.gameObject.layer;
            return root;
        }
        // 0.81 Part D: a person in the scripted scenes from the same parametric rider (poses Stand / Sit / Sled in Rider.fbx,
        // ground at the parent's origin, facing its +z). Returns the rider holder (with its RiderArms joints).
        public static Transform PersonFigure(Transform parent,string pose,RiderLook look)
        {
            Materials();var root=new GameObject(pose+" pose").transform;root.SetParent(parent,false);root.gameObject.layer=parent.gameObject.layer;
            Rider(root,pose,Vector3.zero,look);var holder=root.Find("Rider");
            if(holder)foreach(var t in holder.GetComponentsInChildren<Transform>(true))t.gameObject.layer=parent.gameObject.layer;
            return holder;
        }
        // ---------- 0.75 parametric rider ----------
        static GameObject riderAsset;static bool riderLoaded;
        static readonly Dictionary<string,Material> riderMaterials=new();
        static Material RiderMaterial(string kind,Color c)
        {
            string key=kind+ColorUtility.ToHtmlStringRGB(c);
            if(!riderMaterials.TryGetValue(key,out var m)||!m){m=Mat("Rider "+kind,c,kind=="hair"?.38f:kind=="skin"?.3f:kind=="leather"?.62f:.18f);riderMaterials[key]=m;}
            return m;
        }
        // Denim: the chosen swatch as a darker, bluer fabric (Blue gives the classic rider's jeans).
        static Color Denim(Color c)=>c*.42f+new Color(.05f,.06f,.08f);
        // 0.83 leather: the swatch a little deeper (black stays near-black, so the sheen reads)
        static Color Leather(Color c)=>c*.8f+new Color(.02f,.018f,.016f);
        static void Rider(Transform pose,string poseName,Vector3 seat,RiderLook look)
        {
            if(!riderLoaded){riderLoaded=true;riderAsset=Resources.Load<GameObject>("VehicleModels/Rider");}
            if(!riderAsset)return;
            var holder=new GameObject("Rider").transform;holder.SetParent(pose,false);holder.localPosition=seat;holder.localRotation=Quaternion.Euler(0,180,0);
            var colors=VehiclePaint.Colors;var arms=new List<(Transform part,string joint)>();
            foreach(Transform part in riderAsset.transform)
            {
                string name=part.name;int cut=name.IndexOf("__");if(cut<0)continue;
                var key=name.Substring(0,cut).Split('_');if(key.Length<4||key.Length>5||key[0]!=poseName||!look.Shows(key[1],key[2],key[3]))continue;
                var go=Object.Instantiate(part.gameObject,holder,false);go.name=name;
                // 0.78: arm parts (fifth key U / L / H + side) have their origin on the joint they turn about.
                if(key.Length==5)arms.Add((go.transform,key[4]));
                go.GetComponent<Renderer>().sharedMaterial=name.Substring(cut+2) switch{
                    "skin"=>RiderMaterial("skin",RiderLook.Skins[look.skin]),"hair"=>RiderMaterial("hair",RiderLook.HairColors[look.hairColor]),
                    "hat"=>RiderMaterial("hat",colors[look.hatColor]),"shirt"=>RiderMaterial(look.shirt==3?"leather":"shirt",look.shirt==3?Leather(colors[look.shirtColor]):colors[look.shirtColor]),
                    // 0.83: the T-shirt under the open leather jacket (white, or dark grey under a white jacket) and the emblem
                    "inner"=>RiderMaterial("shirt",look.shirtColor==4?new Color(.16f,.16f,.17f):colors[4]),"emblem"=>RiderMaterial("emblem",new Color(.93f,.93f,.9f)),
                    "pants"=>look.pants==0?RiderMaterial("jeans",Denim(colors[look.pantsColor])):RiderMaterial("shorts",colors[look.pantsColor]),
                    "trim"=>trim,"shoes"=>shoes[0],"eyes"=>eyes,"pupil"=>rubber,"mouth"=>mouth,_=>trim};
            }
            RiderArms.Rig(holder,poseName,arms);
        }
        static void Person(Transform root,Vector3 hip,float scale,bool bike,float footSpan=.32f)
        {
            var pose=new GameObject("Steering pose").transform;pose.SetParent(root,false);pose.gameObject.layer=root.gameObject.layer;
            var motion=pose.gameObject.AddComponent<VehiclePose>();motion.bike=bike;
            if(bike)foreach(Transform child in root.Cast<Transform>().Where(t=>t.name=="Handlebars"||t.name=="Grip").ToArray())child.SetParent(pose,false);
            root=pose;
            Vector3 P(float x,float y,float z)=>hip+new Vector3(x,y,z)*scale;
            int identity=0;foreach(char c in root.root.name)identity+=c;
            var skin=skins[identity%skins.Length];var shirt=shirts[identity%shirts.Length];
            var pants=trousers[(identity/3)%trousers.Length];var shoe=shoes[identity%shoes.Length];
            Part(root,"Seated hips",P(0,0,0),new Vector3(.42f,.22f,.3f)*scale,pants,PrimitiveType.Sphere);
            Part(root,"Shirt torso",P(0,.31f,.09f),new Vector3(.49f,.59f,.34f)*scale,shirt,PrimitiveType.Sphere).localRotation=Quaternion.Euler(bike?15:0,0,0);
            var hairColor=hair[identity%hair.Length];
            Part(root,"Face and head",P(0,.735f,.18f),new Vector3(.33f,.36f,.32f)*scale,skin,PrimitiveType.Sphere);
            Part(root,"Neck",P(0,.53f,.13f),new Vector3(.15f,.16f,.16f)*scale,skin,PrimitiveType.Sphere);
            {
                Part(root,"Hair cap",P(0,.876f,.15f),new Vector3(.35f,.13f,.32f)*scale,hairColor,PrimitiveType.Sphere);
                Part(root,"Swept fringe",P(-.055f,.835f,.297f),new Vector3(.24f,.105f,.07f)*scale,hairColor,PrimitiveType.Sphere).localRotation=Quaternion.Euler(0,0,-14);
                Part(root,"Hair back",P(0,.765f,.04f),new Vector3(.31f,.24f,.085f)*scale,hairColor,PrimitiveType.Sphere);
                foreach(float side in new[]{-1f,1f})Part(root,"Ear",P(side*.165f,.735f,.17f),new Vector3(.05f,.09f,.07f)*scale,skin,PrimitiveType.Sphere);
            }
            foreach(float side in new[]{-1f,1f})
            {
                Part(root,"Eye white",P(side*.073f,.774f,.321f),new Vector3(.080f,.055f,.032f)*scale,eyes,PrimitiveType.Sphere);
                Part(root,"Eye pupil",P(side*.073f,.774f,.338f),new Vector3(.031f,.039f,.014f)*scale,rubber,PrimitiveType.Sphere);
                Part(root,"Expressive brow",P(side*.073f,.816f,.313f),new Vector3(.091f,.022f,.026f)*scale,hairColor).localRotation=Quaternion.Euler(0,0,side*9);
            }
            Part(root,"Rounded nose",P(0,.725f,.344f),new Vector3(.057f,.079f,.067f)*scale,skin,PrimitiveType.Sphere);
            Part(root,"Smile",P(0,.670f,.322f),new Vector3(.103f,.022f,.023f)*scale,mouth,PrimitiveType.Sphere);
            foreach(float side in new[]{-1f,1f})
            {
                var shoulder=P(side*.24f,.46f,.09f);var elbow=P(side*.31f,.23f,.43f);
                var hand=bike?new Vector3(side*.40f,.72f,.58f):P(side*.24f,.37f,.66f);
                Link(root,"Upper sleeve",shoulder,elbow,.14f*scale,shirt);Link(root,"Forearm",elbow,hand,.115f*scale,skin);
                Part(root,"Hand on control",hand,Vector3.one*.145f*scale,skin,PrimitiveType.Sphere);
                var knee=P(side*(footSpan*.75f+.06f),-.17f,.36f);var foot=P(side*footSpan,-.46f,.08f);
                Link(root,"Thigh",P(side*.17f,0,0),knee,.18f*scale,pants);Link(root,"Shin",knee,foot,.15f*scale,pants);
                Part(root,"Shoe on control",foot+Vector3.forward*.06f*scale,new Vector3(.17f,.12f,.3f)*scale,shoe);
                if(!bike)Part(root,"Pedal",foot+new Vector3(0,-.07f,.12f)*scale,new Vector3(.18f,.035f,.18f)*scale,rubber);
            }
            if(!bike)
            {
                // Open rim preserves the hands/control location and the face sightline.
                var vertices=new List<Vector3>();var triangles=new List<int>();var rotation=Quaternion.Euler(65,0,0);var center=P(0,.37f,.68f);
                for(int i=0;i<16;i++)for(int side=0;side<4;side++){float a=i*Mathf.PI/8,r=side<2?.31f:.265f,y=side==0||side==3?-.025f:.025f;vertices.Add(center+rotation*new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r)*scale);}
                for(int i=0;i<16;i++)for(int side=0;side<4;side++){int a=i*4+side,b=i*4+(side+1)%4,c=(i+1)%16*4+side,d=(i+1)%16*4+(side+1)%4;triangles.AddRange(new[]{a,c,b,b,c,d});}
                MeshPart(root,"Open steering wheel rim",vertices,triangles,rubber);
                foreach(float angle in new[]{0f,120f,240f}){float a=angle*Mathf.Deg2Rad;Link(root,"Steering spoke",center,center+rotation*new Vector3(Mathf.Cos(a)*.27f,0,Mathf.Sin(a)*.27f)*scale,.027f*scale,rubber);}
            }
            Merge(root);
        }
        static void Arch(Transform root,float x,float z,float radius)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=12;i++)for(int side=0;side<2;side++)for(int edge=0;edge<2;edge++)
            {float a=i*Mathf.PI/12,r=radius+edge*.07f;vertices.Add(new(x+(side-.5f)*.16f,-.2f+Mathf.Sin(a)*r,z+Mathf.Cos(a)*r));}
            for(int i=0;i<12;i++)foreach(var pair in new[]{(0,1),(1,3),(3,2),(2,0)})
            {int a=i*4+pair.Item1,b=i*4+pair.Item2;triangles.AddRange(new[]{a,b,b+4,a,b+4,a+4});}
            MeshPart(root,"Continuous wheel arch",vertices,triangles,paint);
        }
        static void BeveledBody(Transform root,Vector3 center,Vector3 size,float bevel)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int ring=0;ring<4;ring++)
            {
                float inset=ring==0||ring==3?bevel:0;float x=size.x*.5f-inset,z=size.z*.5f-inset;
                float y=ring==0?-size.y*.5f:ring==1?-size.y*.5f+bevel:ring==2?size.y*.5f-bevel:size.y*.5f;
                foreach(var v in new[]{new Vector3(-x,0,-z+bevel),new(-x+bevel,0,-z),new(x-bevel,0,-z),new(x,0,-z+bevel),new(x,0,z-bevel),new(x-bevel,0,z),new(-x+bevel,0,z),new(-x,0,z-bevel)})vertices.Add(center+v+Vector3.up*y);
            }
            for(int ring=0;ring<3;ring++)for(int i=0;i<8;i++){int a=ring*8+i,b=ring*8+(i+1)%8;triangles.AddRange(new[]{a,b,b+8,a,b+8,a+8});}
            for(int i=1;i<7;i++){triangles.AddRange(new[]{0,i+1,i,24,24+i,25+i});}
            for(int i=0;i<triangles.Count;i+=3)(triangles[i+1],triangles[i+2])=(triangles[i+2],triangles[i+1]);
            MeshPart(root,"Beveled body",vertices,triangles,paint);
        }
        static void MeshPart(Transform root,string name,List<Vector3> vertices,List<int> triangles,Material material)
        {
            var go=new GameObject(name);go.layer=root.gameObject.layer;go.transform.SetParent(root,false);
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;go.AddComponent<VehicleMeshLifetime>().owned=mesh;
        }
        static void Link(Transform parent,string name,Vector3 a,Vector3 b,float width,Material mat)
        {var t=Part(parent,name,(a+b)*.5f,new(width,Vector3.Distance(a,b)*.5f,width),mat,PrimitiveType.Cylinder);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        public static void WheelDetail(Transform wheel)
        {
            Materials();if(wheel.Find("Wheel motion marker"))return;
            foreach(float side in new[]{-1f,1f})
            {
                Part(wheel,"Alloy rim",new(0,side*1.015f,0),new(.72f,.025f,.72f),metal,PrimitiveType.Cylinder);
                Part(wheel,"Hub inset",new(0,side*1.045f,0),new(.48f,.03f,.48f),rubber,PrimitiveType.Cylinder);
                for(int i=0;i<3;i++)Part(wheel,"Wheel motion marker",new(0,side*1.08f,0),new(.63f,.025f,.07f),metal).localRotation=Quaternion.Euler(0,i*60,0);
            }
            Merge(wheel);
        }
        static Transform Part(Transform parent,string name,Vector3 position,Vector3 size,Material mat,PrimitiveType shape=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;go.layer=parent.gameObject.layer;var collider=go.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;
        }
        static void Merge(Transform root)
        {
            var owner=root.GetComponentInParent<VehiclePose>();
            var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.transform!=root&&f.GetComponentInParent<VehiclePose>()==owner).ToArray();
            foreach(var group in filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial))
            {
                var g=new GameObject(group.Key.name+" mesh");g.layer=root.gameObject.layer;g.transform.SetParent(root,false);
                var mesh=new Mesh{name="Combined vehicle detail"};mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
                g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
                g.AddComponent<VehicleMeshLifetime>().owned=mesh;
            }
            foreach(var f in filters){f.gameObject.SetActive(false);Object.Destroy(f.gameObject);}
        }
    }
    public sealed class VehicleMeshLifetime:MonoBehaviour
    {
        // Instantiate shares mesh assets but does not copy this runtime ownership field.
        // Retiring a cloned hierarchy must never destroy the original racer's mesh.
        [System.NonSerialized] public Mesh owned;
        void OnDestroy(){if(owned)Object.Destroy(owned);}
    }
    // 0.73 Needle 600: the front end turns about the fork (steering) axis with the visual steering. Its wheel pivot sits on
    // the axle, which lies on that axis, so the wheel stays in the fork.
    // 0.75: also the Trail Four handlebars and the car steering wheels (axis and gain set by VehicleVisual).
    public sealed class MotorcycleFrontEnd:MonoBehaviour
    {
        public Vector3 axis=new(0,.76f,-.305f);public float gain=9;ArcadeVehicle car;
        void Start(){car=GetComponentInParent<ArcadeVehicle>();axis=axis.normalized;}
        void LateUpdate(){if(car)transform.localRotation=Turn(car.VisualSteering);}
        // 0.85: the same turn for the rider's hands (RiderGestures)
        public Vector3 Axis=>axis.normalized;
        public float Angle(float steer)=>steer*gain;
        public Quaternion Turn(float steer)=>Quaternion.AngleAxis(Angle(steer),Axis);
    }
    public sealed class VehiclePose:MonoBehaviour
    {
        public bool bike;ArcadeVehicle car;
        void Start(){car=GetComponentInParent<ArcadeVehicle>();}
        void LateUpdate(){if(car)transform.localRotation=Turn(car.VisualSteering,bike);}
        public static Quaternion Turn(float steer,bool bike)=>Quaternion.Euler(0,steer*(bike?3:1.5f),bike?-steer*2:0);
    }
}
