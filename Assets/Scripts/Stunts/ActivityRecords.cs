using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace Racer
{
    // Racer's ActivityRecords, as it is (Chastain Rush, Round 36). Independent of race boards. Each crossing/landing owns exactly one attempt ID.
    public sealed class ActivityRecords
    {
        [Serializable] public sealed class Entry { public string id,key,site,vehicle,date; public float value,airtime; public int medal; public bool historical; }
        [Serializable] public sealed class Data { public int version=2; public bool migrated; public List<Entry> entries=new(); }
        public Data Archive {get;private set;}=new();
        public string Error {get;private set;}
        readonly string path;
        public ActivityRecords(string root,ArcadeActivities.Archive legacy)
        {
            path=Path.Combine(root,"activity-records-v2.json");
            try { if(File.Exists(path)) Archive=JsonUtility.FromJson<Data>(File.ReadAllText(path))??new(); }
            catch(Exception e){Error="Activity records could not be loaded: "+e.Message;return;}
            Archive.entries??=new();
            if(!Archive.migrated)
            {
                foreach(var b in legacy.results.Where(b=>float.IsFinite(b.value)&&b.value>0))
                {
                    var parts=b.key.Split('/');
                    Add(new Entry{id="legacy/"+b.key,key=b.key,site=parts[0],vehicle=parts[^1],value=b.value,medal=b.medal,historical=true},false);
                }
                Archive.migrated=true;if(legacy.results.Count>0)Save();   // Chastain Rush: loading writes nothing when there is nothing to carry over
            }
        }
        public List<Entry> Board(string key)=>Archive.entries.Where(e=>e.key==key).OrderByDescending(e=>e.value).Take(10).ToList();
        public List<Entry> Board(string[] keys)=>Archive.entries.Where(e=>keys.Contains(e.key)).OrderByDescending(e=>e.value).Take(10).ToList();
        public bool Add(Entry e,bool save=true)
        {
            if(Error!=null||string.IsNullOrEmpty(e.id)||!float.IsFinite(e.value)||e.value<=0||Archive.entries.Any(x=>x.id==e.id))return false;
            Archive.entries.Add(e);
            var keep=Board(e.key);Archive.entries.RemoveAll(x=>x.key==e.key&&!keep.Contains(x));
            if(save)Save();return true;
        }
        void Save(){try{AtomicSave.Write(path,JsonUtility.ToJson(Archive,true));}catch(Exception e){Error="Activity records could not be saved: "+e.Message;}}
    }
}
