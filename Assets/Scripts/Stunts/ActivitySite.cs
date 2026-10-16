using ChastainRush;
using UnityEngine;
namespace Racer
{
    // Racer's ActivitySite (a Free Roam jump, speed trap or smash), as it is. Chastain Rush, Round 36: a smash site's props are breakable panels
    // here (FenceChunk + panel index, the game's breakables) rather than Racer's BreakableProp objects; the sites are made at run time from
    // the activities file (ArcadeActivities.Sites: game layer, not baked into the world).
    public sealed class ActivitySite:MonoBehaviour
    {
        public enum Kind { Jump, Smash, Speed }
        public string id,title;
        // 0.76 FreeRoamWorld: the record keys ("site/course") this same site had in the course scenes; their results are
        // shown as this site's own (read only, never changed).
        public string[] legacyRecords;
        public Kind kind;
        public float radius=25,bronze=6,silver=14,gold=22;
        public Vector3 forward=Vector3.forward;
        public bool bothDirections=true;
        public FenceChunk[] propChunks; public int[] propPanels;   // Chastain Rush: Racer's BreakableProp[] props
        public float Seconds=45;
        public float[] vehicleBronze,vehicleSilver,vehicleGold;
        public void Targets(string vehicle,out float b,out float s,out float g){var profile=VehicleProfile.Find(vehicle);int i=VehicleProfile.IndexOf(profile.Base??profile.Id);b=vehicleBronze?.Length==4&&i>=0&&i<4?vehicleBronze[i]:bronze;s=vehicleSilver?.Length==4&&i>=0&&i<4?vehicleSilver[i]:silver;g=vehicleGold?.Length==4&&i>=0&&i<4?vehicleGold[i]:gold;}
        public int Medal(float value,string vehicle){Targets(vehicle,out float b,out float s,out float g);return value>=g?3:value>=s?2:value>=b?1:0;}
        public int PropCount=>propChunks==null?0:propChunks.Length;
        public bool HasProp(FenceChunk c,int panel){if(propChunks==null)return false;for(int i=0;i<propChunks.Length;i++)if(propChunks[i]==c&&propPanels[i]==panel)return true;return false;}
        public Vector3 PropAt(int i)=>propChunks[i]?propChunks[i].transform.TransformPoint(propChunks[i].panels[propPanels[i]].centre):transform.position;
        public bool PropDown(int i)=>!propChunks[i]||propChunks[i].broken!=null&&propChunks[i].broken[propPanels[i]];
    }
}
