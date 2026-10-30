using System.Collections.Generic;
using UnityEngine;

namespace Racer
{
    [DisallowMultipleComponent]
    public sealed class VehicleConfiguration : MonoBehaviour
    {
        static readonly HashSet<VehicleConfiguration> active = new();
        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);
        public string profileId = "original";
        // 0.75: this vehicle's rider (null = the player's, RiderLook.Player) and, for ambient traffic, always the classic
        // model. Set before Apply; clones do not inherit them.
        [System.NonSerialized] public RiderLook riderLook;
        [System.NonSerialized] public bool classicVisual;
        [SerializeField] string originalMotor;
        [SerializeField] Transform[] originalVisuals;
        [SerializeField] bool[] originalEnabled;
        [SerializeField] Vector3 originalSize, originalCenter;
        [SerializeField] float originalMass;
        public VehicleProfile Profile => VehicleProfile.Find(profileId);
        Transform generated;
        ArcadeVehicle motor;
        readonly List<Transform> wheels = new();
        float roll, crashUntil;
        float overturned, upright;
        Color? selectedPaint; int selectedScheme=-1;
        // 0.92 Part D: an index past the plain colours is the champion's scheme (gold with a number roundel)
        public void SetBodyColor(int index) { if(Profile.IsPolice||Profile.CupEdition) return;   // 0.111: a Cup edition keeps its livery (Round 36)
            if(index>=0 && index<=VehiclePaint.Champion) { SetPaint(VehiclePaint.Of(index)); selectedScheme=index; VehiclePaint.Scheme(transform,index); } }
        public void SetPaint(Color color) { selectedPaint=color; selectedScheme=-1; VehiclePaint.Apply(transform,color); VehiclePaint.Roundel(transform,false); }
        public Color? Paint => selectedPaint;   // Chastain Rush, Round 57 (F): the replay records each racer's paint
        public bool WipedOut => Time.time < crashUntil;
        public int VehicleContactEvents { get; private set; }
        public void Recover() { crashUntil=0; overturned=upright=0; }
        public void PrepareContacts()
        {
            if(!motor || Profile.Small) return;
            bool smallNearby=false;
            foreach(var other in active)
                if(other && other!=this && other.motor && other.gameObject.scene==gameObject.scene && other.Profile.Small && (other.motor.Body.position-motor.Body.position).sqrMagnitude<400) { smallNearby=true; break; }
            // Sweep CCD resolves a first impact before mass scaling on this Unity version.
            // Use speculative detection only in the local car/small-vehicle interaction region.
            var mode=smallNearby?CollisionDetectionMode.ContinuousSpeculative:CollisionDetectionMode.ContinuousDynamic;
            if(motor.Body.collisionDetectionMode!=mode) motor.Body.collisionDetectionMode=mode;
        }
        public void Apply(string id)
        {
            motor = GetComponent<ArcadeVehicle>();
            var box = GetComponent<BoxCollider>();
            if (string.IsNullOrEmpty(originalMotor))
            {
                originalMotor = JsonUtility.ToJson(motor);
                originalSize = box.size; originalCenter = box.center; originalMass = motor.Body.mass;
                var children = new List<Transform>();
                foreach (Transform child in transform) if(child.name!="Water feedback"&&child.name!="Water ripple") children.Add(child);
                originalVisuals = children.ToArray();
                originalEnabled = children.ConvertAll(t=>t.gameObject.activeSelf).ToArray();
            }
            JsonUtility.FromJsonOverwrite(originalMotor, motor);
            profileId = VehicleProfile.Find(id).Id;
            selectedPaint=null; selectedScheme=-1; VehiclePaint.Roundel(transform,false); UpgradeScale=new float[]{1,1,1,1};
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
                if(VehiclePaint.IsBodyPaint(renderer.sharedMaterial)) renderer.SetPropertyBlock(null);
            var p = Profile;
            for(int i=0;i<originalVisuals.Length;i++) if(originalVisuals[i]) originalVisuals[i].gameObject.SetActive(false);
            // Clones carry the generated hierarchy but not runtime field references.
            var old = transform.Find("Vehicle visual");
            if (old) { old.name="Retired visual"; old.gameObject.SetActive(false); Destroy(old.gameObject); }
            // 0.75: a clone made in the frame its source was rebuilt inherits that hidden, not-yet-destroyed copy; remove it.
            foreach (Transform child in transform) if (child.name=="Retired visual" && child!=old) Destroy(child.gameObject);
            wheels.Clear(); generated = null;
            if (profileId != "original")
            {
                motor.topSpeed=p.Speed; motor.acceleration=p.Acceleration; motor.maxGripAcceleration=p.Grip;
                motor.steeringResponse=p.Response; motor.wheelbase=p.Wheelbase;
                motor.yawResponse=p.Small?11:8; motor.lateralGrip=p.Small?11:8;
                motor.slowSteerAngle=p.Small?29:31; motor.fastSteerAngle=p.Small?9:9.5f;
                motor.braking=p.Small?27:24;
                float track=p.Motorcycle?.16f:p.Class=="ATV"?.6f:p.Class=="Mower"?.5f:p.Class=="Buggy"?.68f:.9f;   // Round 36: Racer 0.106's stunt buggy
                float axle=p.Wheelbase*.5f;
                motor.suspensionPoints=new[]{new Vector3(-track,0,axle),new Vector3(track,0,axle),new Vector3(-track,0,-axle),new Vector3(track,0,-axle)};
                motor.suspensionLength=p.Suspension>0?p.Suspension:p.Small?.65f:.8f;
                motor.uprightStrength=p.Upright>0?p.Upright:p.Motorcycle?19:22;
                motor.uprightDamping=p.Small?6:5;
                motor.airStability=p.Air>0?p.Air:p.Motorcycle?.1f:.18f;
                motor.centreOfMass=new(0,p.Small?-.28f:-.35f,0);
            }
            // 0.106 Part C: air steering for the stunt vehicles only; they may turn faster (a flip in about 1.2 s). Chastain Rush, Round 36
            motor.airControl=p.AirControl; motor.Body.maxAngularVelocity=p.AirControl>0?8:5;
            generated=VehicleVisual.Build(transform,p,wheels,p.IsPolice&&p.Small?RiderLook.Police:riderLook,classicVisual);
            if(p.IsPolice) VehiclePaint.Apply(transform,new Color(.03f,.03f,.035f)); // 0.99: police vehicles are black and white whoever drives them
            if(!GetComponent<RiderGestures>()) gameObject.AddComponent<RiderGestures>();
            // Chastain Rush, Round 63: the helicopter flies (HelicopterFlight); any other profile drives again
            var heli=GetComponent<ChastainRush.HelicopterFlight>();
            if(p.Class=="Helicopter"){ if(!heli) heli=gameObject.AddComponent<ChastainRush.HelicopterFlight>(); heli.Begin(generated); }
            else if(heli) heli.End();
            // Chastain Rush, Round 69: the plane flies itself (PlaneFlight; on its wheels it calls this vehicle's own drive)
            var plane=GetComponent<ChastainRush.PlaneFlight>();
            if(p.Class=="Plane"){ if(!plane) plane=gameObject.AddComponent<ChastainRush.PlaneFlight>(); plane.Begin(generated); }
            else if(plane) plane.End();
            box.size=profileId=="original"?originalSize:p.Size;
            box.center=profileId=="original"?originalCenter:new Vector3(0,.05f,0);
            motor.Body.mass=profileId=="original"?originalMass:p.Mass;
            // Speculative CCD keeps small-vehicle contacts in the mass-modifiable solver.
            // Cars retain their existing sweep CCD; ground/obstacle material rules are unchanged.
            motor.Body.collisionDetectionMode=p.Small?CollisionDetectionMode.ContinuousSpeculative:CollisionDetectionMode.ContinuousDynamic;
            motor.Body.centerOfMass=motor.centreOfMass;
            motor.Body.ResetInertiaTensor(); motor.ClearSteering(); roll=crashUntil=0;
            VehicleContactEvents=0;
            VehicleContact.Register(motor.Body, p.Small&&!p.SolidContact);
            VehicleSurfaceContacts.Register(box);
            foreach(var c in GetComponentsInChildren<Collider>()) c.hasModifiableContacts=true;
            var camera=FindAnyObjectByType<ChaseCamera>();
            if(camera && camera.target==transform) { camera.offset=p.Camera; camera.Snap(); }
        }
        // 0.90 campaign upgrades: scale the applied profile's top speed, acceleration, grip and handling (steering response).
        // Campaign runs only; any Apply puts the stock values back.
        public float[] UpgradeScale { get; private set; } = { 1, 1, 1, 1 };
        public void ApplyUpgrades(float speed, float acceleration, float grip, float handling)
        {
            if(!motor) motor=GetComponent<ArcadeVehicle>();
            motor.topSpeed*=speed; motor.acceleration*=acceleration; motor.maxGripAcceleration*=grip; motor.steeringResponse*=handling;
            UpgradeScale=new[]{speed,acceleration,grip,handling};
        }
        void LateUpdate()
        {
            if(!motor) return;
            roll += motor.ForwardSpeed * Time.deltaTime / .33f * Mathf.Rad2Deg;
            foreach(var wheel in wheels) if(wheel)
                wheel.localRotation=Quaternion.Euler(0,wheel.localPosition.z>0?motor.VisualSteering*27:0,0)*Quaternion.Euler(roll,0,90);
            if(generated && Profile.Motorcycle) generated.localRotation=Quaternion.Euler(0,0,-motor.VisualSteering*Mathf.Clamp(motor.ForwardSpeed,0,25)*Profile.Lean);
        }
        public void BuildPreview(Transform parent)
        {
            VehicleVisual.Build(parent,Profile,null,Profile.IsPolice&&Profile.Small?RiderLook.Police:riderLook,classicVisual);
            if(Profile.CupEdition) return;   // 0.111 (Round 36)
            if(selectedScheme>=0) VehiclePaint.Scheme(parent,selectedScheme); else if(selectedPaint.HasValue) VehiclePaint.Apply(parent,selectedPaint.Value);
        }
        void OnCollisionEnter(Collision collision)
        {
            if(collision.rigidbody && collision.rigidbody.GetComponent<ArcadeVehicle>()) VehicleContactEvents++;
            // Upright bumps and predicted CCD contacts are not a loss of control.
            // A sustained overturned state below handles meaningful recovery feedback.
        }
        void FixedUpdate()
        {
            if(!motor || !Profile.Small || Profile.Flies || motor.Body.isKinematic) return;   // Round 63: the helicopter has its own crash
            bool needsHelp=transform.up.y<.35f && motor.Body.linearVelocity.magnitude<5;
            overturned=needsHelp?overturned+Time.fixedDeltaTime:0;
            upright=transform.up.y>.7f && motor.GroundedWheels>=2?upright+Time.fixedDeltaTime:0;
            if(overturned>1.2f && !WipedOut && !ChastainRush.HighAssist.For(motor)) Wipeout();   // Round 84 (A): Racer 1.2: no wrecks in HIGH MODE (it flips itself upright)
            if(upright>.35f && WipedOut) Recover();   // Chastain Rush: no RaceFlow recovery message
        }
        void Wipeout()
        {
            if(WipedOut) return;
            crashUntil=float.PositiveInfinity;   // Chastain Rush: no RaceFlow wipeout message (R / Y resets the vehicle)
        }
        void OnDestroy() { if(motor && motor.Body) VehicleContact.Unregister(motor.Body); var box=GetComponent<BoxCollider>(); if(box) VehicleSurfaceContacts.Unregister(box); }
    }
}
