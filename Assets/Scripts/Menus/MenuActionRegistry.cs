using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Racer
{
    // Chastain Rush (Round 16): copied from Racer unchanged.
    // One entry owns the visible control's callback, enabled state and binding.
    public sealed class MenuActionRegistry
    {
        public sealed class Entry
        {
            public string Id,Label;
            public InputAction Binding;
            public Func<bool> Enabled;
            public Action Invoke;
            public bool Execute(){if(MenuInput.Blocked||Enabled?.Invoke()==false)return false;MenuInput.ConsumeThroughRelease(Binding);Invoke();return true;}
        }
        readonly Dictionary<string,Entry> entries=new();
        public Entry Register(string id,string label,InputAction binding,Action invoke,Func<bool> enabled=null)
        {var entry=new Entry{Id=id,Label=label,Binding=binding,Invoke=invoke,Enabled=enabled};entries[id]=entry;return entry;}
        public void Clear()=>entries.Clear();
    }
}
