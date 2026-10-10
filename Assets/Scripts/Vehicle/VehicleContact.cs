using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Racer
{
    // Deliberate arcade rule: only car/small-vehicle pairs change effective solver mass.
    // Immutable snapshots let the physics worker read IDs without accessing Unity objects.
    public static class VehicleContact
    {
        static volatile Dictionary<EntityId,bool> classes = new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
            classes=new();
            Physics.ContactModifyEvent-=Modify; Physics.ContactModifyEventCCD-=Modify;
            Physics.ContactModifyEvent+=Modify; Physics.ContactModifyEventCCD+=Modify;
        }
        public static void Register(Rigidbody body,bool small) { var next=new Dictionary<EntityId,bool>(classes); next[body.GetEntityId()]=small; classes=next; }
        public static void Unregister(Rigidbody body) { var next=new Dictionary<EntityId,bool>(classes); next.Remove(body.GetEntityId()); classes=next; }
        static void Modify(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
        {
            var snapshot=classes;
            for(int i=0;i<pairs.Length;i++)
            {
                var pair=pairs[i];
                if(!snapshot.TryGetValue(pair.bodyEntityId,out bool a) || !snapshot.TryGetValue(pair.otherBodyEntityId,out bool b) || a==b) continue;
                var mass=pair.massProperties;
                // Cars retain their trajectory against small vehicles, in either initiating direction.
                mass.inverseMassScale=a?1:0; mass.inverseInertiaScale=a?1:0;
                mass.otherInverseMassScale=b?1:0; mass.otherInverseInertiaScale=b?1:0;
                pair.massProperties=mass;
                for(int c=0;c<pair.contactCount;c++) pair.SetBounciness(c,0);
            }
        }
    }
}
