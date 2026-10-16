using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ChastainRush;
using UnityEngine;
namespace Racer
{
    // Racer's ArcadeActivities (Free Roam's jumps, speed traps and smash runs, and 0.106's stunt scoring in ArcadeActivities.Stunts.cs), ported for
    // Chastain Rush in Round 36 (Part A: the stunts; Part B: the activities). Activity results never enter the race timer, penalty ledger or record
    // boards. Racer's pieces for its campaign, split-screen, police games, BalanceLog and the developer location HUD are left out (this game has
    // none of them); the race director is RaceController here (Free Roam = its Free stage), Racer's BreakableProp is the game's breakable panels
    // (FenceChunk), and the HUD lines are drawn by ActivityHud. One per player vehicle, made at start (Boot); Free Roam only.
    public sealed partial class ArcadeActivities:MonoBehaviour
    {
        [Serializable] public sealed class Best {public string key;public float value;public int medal;}
        [Serializable] public sealed class Archive {public int version=1;public List<Best> results=new();}
        public static ArcadeActivities Instance {get;private set;}
        public Archive Results {get;private set;}=new();
        public ActivityRecords Records {get;private set;}
        public ActivitySite[] Sites {get;private set;}=new ActivitySite[0];
        public ActivitySite Selected {get;private set;}
        public bool AttemptActive {get;private set;}
        public string Feedback {get;private set;}
        public int Awards {get;private set;}
        public GameObject PlayerObject=>car?car.gameObject:null;
        public ArcadeVehicle Car=>car;
        public float LastDistance {get;private set;}
        public float LastAirtime {get;private set;}
        public float LastSpeed {get;private set;}
        public float LastJumpAward {get;private set;}
        public ActivitySite LastAwardSite {get;private set;}
        public string LastJumpDiagnostic {get;private set;}
        public int SmashCount=>smashed.Count;
        public string Location {get{if(!Selected||!car)return "";var delta=Selected.transform.position-car.Body.position;int compass=Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,360)/45)%8;return $"{DisplayUnits.Distance(Vector3.ProjectOnPlane(delta,Vector3.up).magnitude)} {new[]{"N","NE","E","SE","S","SW","W","NW"}[compass]}";}}
        public string Hud=>Stunted(Time.time<feedbackUntil&&!(feedbackStunt&&TrickBox)?Feedback:AttemptActive?$"{Selected.title} / {Mathf.Max(0,deadline-Time.time):0}s / {Location}\n{(Selected.kind==ActivitySite.Kind.Smash?SmashLine:"Land a jump from the marked ramp")}":FreeRoam?AtStart:"");
        // 0.106 Part B: the open combo under the other lines (not while its own line is showing)
        string Stunted(string line){var s=TrickBox?"":StuntHud;if(string.IsNullOrEmpty(s)||Time.time<feedbackUntil&&Feedback!=null&&Feedback.Contains("COMBO"))return line;return string.IsNullOrEmpty(line)?s:line+"\n"+s;}
        // 0.96 Part B: the medal of the last scored result in Free Roam (-1 = none), for the HUD's medal badge, and the three
        // targets of a site as the medal displays write them (gold, silver, bronze)
        public int LastMedal{get;private set;}=-1;public string ResultValue{get;private set;}="";public bool ResultShowing=>LastMedal>=0&&Time.time<feedbackUntil;
        public int Misses{get;private set;}public string LastMiss{get;private set;}public ActivitySite LastMissSite{get;private set;}
        void Miss(ActivitySite site,string why){Misses++;LastMiss=why;LastMissSite=site;}
        public string[] TargetStrings(ActivitySite s){if(!s||!car)return null;s.Targets(configuration.profileId,out float b,out float si,out float g);
            return s.kind==ActivitySite.Kind.Jump?new[]{MedalUi.Feet(g),MedalUi.Feet(si),MedalUi.Feet(b)}:s.kind==ActivitySite.Kind.Speed?new[]{MedalUi.Mph(g),MedalUi.Mph(si),MedalUi.Mph(b)}:new[]{Mathf.RoundToInt(g)+" props",Mathf.RoundToInt(si)+" props",Mathf.RoundToInt(b)+" props"};}
        // 0.79 Part B: in Free Roam, the activity the player is at (within its start radius) and how to start it; nothing
        // otherwise (the nearest one and its distance stay on the map and in the pause menu).
        public ActivitySite AtSite{get{if(!car||Sites==null)return null;var p=car.Body.position;return Sites.Where(s=>s&&Vector3.Distance(p,s.transform.position)<s.radius).OrderBy(s=>Vector3.SqrMagnitude(p-s.transform.position)).FirstOrDefault();}}
        // being at a jump or smash site makes it the selected activity, so pause menu > Activities starts this one
        string AtStart{get{var site=AtSite;if(!site)return ParkPrompt;if(!AttemptActive&&site.kind!=ActivitySite.Kind.Speed)Selected=site;string menu=MenuInput.Controller?"Start":"Esc";
            return site.title+"\n"+(site.kind==ActivitySite.Kind.Speed?"Speed trap: drive through it, either way":site.kind==ActivitySite.Kind.Jump?$"Jump: land it to score  ·  {menu} > ACTIVITIES for a timed attempt":$"{menu} > ACTIVITIES > Start to begin");}}
        // 0.106 Part D: at the stunt park's entrance (Free Roam): its name, how to start the Stunt event, the best combo so far
        string ParkPrompt{get{if(!StuntPark.Current||StuntEvent.InEvent(car)||Vector3.Distance(car.Body.position,StuntPark.Entrance)>StuntPark.EntranceRadius)return "";string menu=MenuInput.Controller?"Start":"Esc";
            return StuntPark.Title+"\n"+$"Stunt event ({StuntProgress.Minutes} minutes): {menu} > STUNT EVENT"+(StuntProgress.BestCombo>0?$"  ·  best combo {StuntProgress.BestCombo:N0} pts":"");}}
        RaceController race;ArcadeVehicle car;VehicleConfiguration configuration;
        // Chastain Rush: Free Roam is the race controller's Free stage (no race running); the scoring runs only then
        public bool FreeRoam=>!race||race.State==RaceController.Stage.Free;
        readonly HashSet<(FenceChunk,int)> smashed=new();readonly Dictionary<ActivitySite,bool> armed=new();
        string path;Vector3 previous,takeoff,landing;float warm,air,stable,feedbackUntil,deadline,blockedUntil,impactSpeed;bool sampled,flying,invalid,touchedDown;
        ActivitySite jumpSite;string rejected;
        // 0.91 Part C: the first reason this flight cannot score (null = none so far)
        void Reject(string why){invalid=true;rejected??=why;}
        float contactImpact,minimumContactUp=1;
        bool oppositeAttempt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(Instance)return;var sw=FindAnyObjectByType<VehicleSwitch>();if(!sw)return;
            var a=sw.gameObject.AddComponent<ArcadeActivities>();a.Initialize(Path.GetDirectoryName(RaceController.RecordsPath()));
        }
        public static string Root=>Path.GetDirectoryName(RaceController.RecordsPath());
        public void Initialize(string root)
        {
            Instance=this;race=RaceController.Instance;car=GetComponent<ArcadeVehicle>();configuration=GetComponent<VehicleConfiguration>();path=Path.Combine(root,"activities-v1.json");
            if(File.Exists(path))try{Results=JsonUtility.FromJson<Archive>(File.ReadAllText(path))??new();if(Results.results==null)Results.results=new();}catch(Exception e){Debug.LogWarning("Activity save could not be read: "+e.Message);}
            Records=new ActivityRecords(root,Results);
            StuntProgress.Load(root);
            GetComponent<VehicleRespawn>().Respawned+=Recovered;FenceChunk.PanelBrokenBy+=Smash;
            var contacts=gameObject.AddComponent<ActivityLandingContact>();contacts.activities=this;
            if(!GetComponent<ActivityHud>())gameObject.AddComponent<ActivityHud>().activities=this;
            if(!GetComponent<TrickHud>())gameObject.AddComponent<TrickHud>().activities=this;
            SetSites(FindObjectsByType<ActivitySite>(FindObjectsSortMode.None));
        }
        void Start(){if(!configuration)configuration=GetComponent<VehicleConfiguration>();}
        // Round 36 (Part B): the sites made from the activities file
        public void SetSites(IEnumerable<ActivitySite> sites)
        {
            Sites=sites.Where(s=>s).OrderBy(s=>s.id).ToArray();Selected=Sites.FirstOrDefault(s=>s.kind!=ActivitySite.Kind.Speed);
            // Racer's ExplorationMap / RacingMiniMap activity markers: every site on the map and the minimap (Free Roam), coloured by kind
            foreach(var pl in mapPlaces)WorldMap.Places.Remove(pl);mapPlaces.Clear();
            foreach(var s in Sites){var pl=new WorldMap.Place{label=s.title,at=s.transform.position,small=true,color=s.kind==ActivitySite.Kind.Jump?new Color(.35f,.85f,1f):s.kind==ActivitySite.Kind.Speed?new Color(1f,.85f,.25f):new Color(1f,.45f,.35f)};mapPlaces.Add(pl);WorldMap.Places.Add(pl);}
        }
        readonly List<WorldMap.Place> mapPlaces=new();
        public const string Course="chastain-v1";
        string Suffix(ActivitySite s)=>"/activities-v2/"+configuration.profileId+(s.kind==ActivitySite.Kind.Speed&&oppositeAttempt?"/opposite":"/forward");
        public string Key(ActivitySite s)=>s.id+"/"+Course+Suffix(s);
        public string[] Keys(ActivitySite s)=>new[]{Key(s)};
        public Best PersonalBest(ActivitySite s)=>Results.results.FirstOrDefault(b=>b.key==Key(s));
        public static string Measurement(ActivitySite site,float value)=>site.kind==ActivitySite.Kind.Speed?MedalUi.Mph(value):site.kind==ActivitySite.Kind.Jump?MedalUi.Feet(value):value.ToString("0")+" props"; // 0.96 Part B: whole feet and mph
        public string Targets{get{if(!Selected)return "";Selected.Targets(configuration.profileId,out float b,out float s,out float g);return Selected.kind==ActivitySite.Kind.Jump?$"Bronze {DisplayUnits.Target(b)} / silver {DisplayUnits.Target(s)} / gold {DisplayUnits.Target(g)}":$"Bronze {Measurement(Selected,b)} / silver {Measurement(Selected,s)} / gold {Measurement(Selected,g)} / {Selected.Seconds:0}s";}}
        public void Cycle(int d=1){var choices=Sites.Where(s=>s.kind!=ActivitySite.Kind.Speed).ToArray();if(choices.Length==0)return;Cancel();Selected=choices[(Math.Max(0,Array.IndexOf(choices,Selected))+d+choices.Length)%choices.Length];}
        public void Select(ActivitySite s){if(!s||s==Selected)return;Cancel();Selected=s;}
        public void BeginAttempt()
        {
            if(!FreeRoam||!Selected)return;Cancel();smashed.Clear();RestoreSmashArea(Selected);AttemptActive=true;deadline=Time.time+Selected.Seconds;Message(Selected.title+" / attempt started"+(Selected.kind==ActivitySite.Kind.Smash?$"  ·  {SmashArea} marked props in {Selected.Seconds:0} s":""),3);
        }
        // Racer restores every prop (BreakableProp.RestoreRace) at an attempt's start; here the smash area's own chunks
        static void RestoreSmashArea(ActivitySite s){if(s==null||s.kind!=ActivitySite.Kind.Smash||s.propChunks==null)return;foreach(var c in s.propChunks.Distinct())if(c)c.RestoreOwn();}
        public void Cancel(){AttemptActive=false;smashed.Clear();warm=0;ResetFlight();}
        public void NewSession(){Cancel();armed.Clear();sampled=false;warm=0;blockedUntil=Time.time+1;Feedback=null;feedbackUntil=0;LastDistance=LastAirtime=LastSpeed=LastJumpAward=0;StuntSession();}
        // 0.111 Part B: a reset never cancels a smash attempt (the count and the clock go on)
        void Recovered(){bool smashing=AttemptActive&&Selected&&Selected.kind==ActivitySite.Kind.Smash;if(smashing)Message(Selected.title+" / reset: keep smashing ("+SmashCount+")",3);else{if(AttemptActive)Message("Attempt canceled by recovery / retry from the menu",4);Cancel();}armed.Clear();sampled=false;warm=0;blockedUntil=Time.time+2;if(ComboLinks>0)EndCombo(null);StuntReset();} // 0.106: a reset banks the open combo
        // 0.106 Part D: the Stunt event's end: the open combo counts
        public void EndStuntCombo(){if(ComboLinks>0)EndCombo(null);StuntReset();}
        void ResetFlight(){flying=false;invalid=touchedDown=false;air=stable=0;jumpSite=null;rejected=null;}
        // The jump the player is going for: Free Roam's timed attempt.
        ActivitySite TargetJump=>AttemptActive&&Selected&&Selected.kind==ActivitySite.Kind.Jump?Selected:null;
        // 0.91 Part C: an ordinary jump counts if the vehicle survives it, so a hard or glancing contact no longer rejects it
        public void SolidContact(Vector3 normal,float relativeSpeed,Vector3? point=null){StuntContact(normal,point,relativeSpeed);if(!flying)return;contactImpact=Mathf.Max(contactImpact,relativeSpeed);minimumContactUp=Mathf.Min(minimumContactUp,normal.y);}
        ActivitySite SiteAt(Vector3 at)=>Sites.Where(s=>s&&s.kind==ActivitySite.Kind.Jump&&Vector3.Distance(at,s.transform.position)<s.radius&&Vector3.Dot(car.Body.linearVelocity,s.forward.normalized)>2).OrderBy(s=>Vector3.SqrMagnitude(at-s.transform.position)).FirstOrDefault();
        void Smash(FenceChunk chunk,int panel,ArcadeVehicle source)
        {
            // 0.111 Part B: every prop of the smash area counts once whatever the vehicle is doing (airborne, tipped, sliding, just reset): no
            // grounded warm-up or reset block for smashes (jumps keep theirs). The attempt ends when every prop is down, or at its time limit.
            if(source!=car||!FreeRoam||!AttemptActive||!Selected||Selected.kind!=ActivitySite.Kind.Smash)return;
            if(!Selected.HasProp(chunk,panel)||!smashed.Add((chunk,panel)))return;
            SmashPop=Time.time;Message(Selected.title+" / +1 ("+smashed.Count+")"+(smashed.Count>=SmashArea?"  ·  ALL "+SmashArea+" DOWN":""),2);
            if(smashed.Count>=SmashArea)FinishSmash();
        }
        void FinishSmash(){var value=smashed.Count;AttemptActive=false;Award(Selected,value,"props");}
        void FixedUpdate()
        {
            if(!car||!configuration)return;
            if(!FreeRoam){if(sampled){NewSession();sampled=false;}return;}   // Chastain Rush: nothing scores in a race
            var p=car.Body.position;float dt=Time.fixedDeltaTime;bool grounded=car.GroundedWheels>=2;
            if(!sampled){previous=p;sampled=true;return;}
            float step=Vector3.Distance(p,previous);bool discontinuity=step>Mathf.Max(3,car.Body.linearVelocity.magnitude*dt*2+.3f);
            if(discontinuity){Recovered();previous=p;return;}
            if(Time.time<blockedUntil){previous=p;return;}
            StuntStep(p,dt,grounded); // 0.106 Part B
            if(AttemptActive&&Time.time>=deadline){if(Selected.kind==ActivitySite.Kind.Smash)FinishSmash();else{AttemptActive=false;Message("Jump attempt expired / retry from the menu",4);}}
            if(grounded&&!flying)warm=car.transform.up.y>.8f?warm+dt:0;
            if(!grounded&&!flying&&warm>=.5f&&Vector3.ProjectOnPlane(car.Body.linearVelocity,Vector3.up).magnitude>4)
            {
                flying=true;invalid=touchedDown=false;rejected=null;takeoff=previous;air=stable=0;impactSpeed=contactImpact=0;minimumContactUp=1;
                jumpSite=SiteAt(takeoff);
            }
            if(flying)
            {
                if(configuration.WipedOut)Reject("Wiped out on landing");
                // 0.91 Part C: a hop off the ramp's lip (under 0.25 s in the air) that takes off again before settling is not
                // the jump: the real flight starts here (0.90 measured the hop, and the jump after it was never scored).
                if(!grounded&&touchedDown&&air<.25f){takeoff=previous;air=0;touchedDown=false;impactSpeed=0;var site=SiteAt(takeoff);if(site)jumpSite=site;}
                if(!grounded){if(!touchedDown)air+=dt;stable=0;impactSpeed=Mathf.Max(impactSpeed,-car.Body.linearVelocity.y);}
                else
                {
                    // Freeze measurement at first touchdown. Subsequent settling or
                    // suspension bounces must not extend the jump's distance/airtime.
                    if(!touchedDown){landing=p;touchedDown=true;}stable+=dt;
                    // 0.91 Part C: a jump counts if the vehicle survives it: down on its wheels, upright, not wiped out, not in the water.
                    if(car.WaterImmersion>.05f)Reject("Landed in the water");
                    if(configuration.WipedOut)Reject("Wiped out on landing");
                    if(car.transform.up.y<.65f)Reject(car.transform.up.y<0?"Landed upside down":"Landed on your side");
                    if(stable>=.3f)
                    {
                        float distance=Vector3.ProjectOnPlane(landing-takeoff,Vector3.up).magnitude;
                        LastJumpDiagnostic=$"site={jumpSite?.id} invalid={invalid} why={rejected} verticalImpact={impactSpeed:F2} contactImpact={contactImpact:F2} contactUp={minimumContactUp:F2} up={car.transform.up.y:F2} wiped={configuration.WipedOut} air={air:F2} distance={distance:F2}";
                        bool tooShort=air<.25f||distance<3||(jumpSite&&Vector3.Dot(landing-takeoff,jumpSite.forward.normalized)<3);
                        bool hop=air<.25f&&!invalid;
                        bool tooLong=air>=12||distance>=250;
                        if(!invalid&&!tooShort&&!tooLong)
                        {
                            LastDistance=distance;LastAirtime=air;if(!StuntsOn)Message($"CLEAN JUMP / {DisplayUnits.Jump(distance)} / {air:0.00} s / {Mathf.RoundToInt(distance*10+air*100)} pts",4); // 0.106: in Free Roam the stunt line says it
                            if(jumpSite){AttemptActive=false;Award(jumpSite,distance,"m");}
                        }
                        else if(jumpSite&&!hop){AttemptActive=false;Miss(jumpSite,rejected??(tooShort?"Too short to count":"Left the course"));Message($"{jumpSite.title} / NOT SCORED: {rejected??(tooShort?"Too short to count":"Left the course")}",5);}
                        // a real flight beside the jump being attempted, but not from its ramp
                        else if(!tooShort&&TargetJump is ActivitySite target&&Vector3.Distance(takeoff,target.transform.position)<target.radius*3&&Vector3.Dot(landing-takeoff,target.forward.normalized)>3){Miss(target,"took off outside the marked area");Message($"{target.title} / NOT SCORED: Took off outside the marked area",5);}
                        ResetFlight();warm=hop?.5f:0; // a hop is no jump: the next takeoff still counts
                    }
                }
            }
            // A bump or valid airborne crossing must not disable a speed camera.
            if(!configuration.WipedOut)
            foreach(var site in Sites)
            {
                if(!site||site.kind!=ActivitySite.Kind.Speed)continue;
                var f=site.forward.normalized;float a=Vector3.Dot(previous-site.transform.position,f),b=Vector3.Dot(p-site.transform.position,f);
                if(Mathf.Abs(b)>25)armed[site]=true;
                bool crossing=a<0&&b>=0 || site.bothDirections&&a>0&&b<=0;
                if(!crossing||!armed.TryGetValue(site,out bool ready)||!ready)continue;
                float t=a/(a-b);var cross=Vector3.Lerp(previous,p,t);var lateral=Vector3.ProjectOnPlane(cross-site.transform.position,f);lateral.y=0;
                float speed=Vector3.ProjectOnPlane(car.Body.linearVelocity,Vector3.up).magnitude;
                if(lateral.magnitude>site.radius||speed<3||Mathf.Abs(Vector3.Dot(car.Body.linearVelocity.normalized,f))<.65f||Mathf.Abs(step/dt-speed)>Mathf.Max(4,speed*.35f))continue;
                armed[site]=false;oppositeAttempt=b<a;LastSpeed=speed;Award(site,speed,"m/s");
            }
            previous=p;
        }
        void Award(ActivitySite site,float value,string units)
        {
            if(!float.IsFinite(value)||value<=0)return;int medal=site.Medal(value,configuration.profileId);var best=PersonalBest(site);
            bool improved=best==null||value>best.value;
            if(site.kind!=ActivitySite.Kind.Smash)Records.Add(new ActivityRecords.Entry{id=Guid.NewGuid().ToString("N"),key=Key(site),site=site.id,vehicle=configuration.profileId,date=DateTime.UtcNow.ToString("o"),value=value,airtime=site.kind==ActivitySite.Kind.Jump?LastAirtime:0,medal=medal});
            if(site.kind==ActivitySite.Kind.Jump)LastJumpAward=value;
            LastAwardSite=site;
            var own=Results.results.FirstOrDefault(b=>b.key==Key(site));if(own==null){own=new Best{key=Key(site)};Results.results.Add(own);}own.value=Mathf.Max(own.value,value);own.medal=Mathf.Max(own.medal,medal);Awards++;
            string measurement=Measurement(site,value)+" / PB "+Measurement(site,Mathf.Max(own.value,best?.value??0));
            Message(site.title+" / "+measurement+"\n"+(improved?"NEW BEST":"personal best retained")+(site.kind==ActivitySite.Kind.Jump?$" / {LastAirtime:0.00}s":""),6);LastMedal=medal;ResultValue=Measurement(site,value);
            try{AtomicSave.Write(path,JsonUtility.ToJson(Results,true));}catch(Exception e){Message("Activity result could not be saved: "+e.Message,6);}
        }
        void Message(string value,float seconds){Feedback=value;feedbackUntil=Time.time+seconds;LastMedal=-1;feedbackStunt=false;}bool feedbackStunt;
        void OnDestroy(){foreach(var pl in mapPlaces)WorldMap.Places.Remove(pl);FenceChunk.PanelBrokenBy-=Smash;if(car&&car.TryGetComponent<VehicleRespawn>(out var respawn))respawn.Respawned-=Recovered;if(Instance==this)Instance=null;}
    }
    public sealed class ActivityLandingContact:MonoBehaviour {public ArcadeActivities activities;void OnCollisionEnter(Collision c){if(!activities||activities.PlayerObject!=gameObject)return;foreach(var contact in c.contacts)activities.SolidContact(contact.normal,Mathf.Abs(Vector3.Dot(c.relativeVelocity,contact.normal)),contact.point);}}
}
