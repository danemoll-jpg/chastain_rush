using UnityEngine;
namespace Racer
{
    // A final soft ceiling for exceptional overlapping impacts plus full-scale user music.
    // Ordinary levels are unchanged. No allocations, filesystem access or locks on the DSP thread.
    // Chastain Rush, Round 27 (B1): copied from Racer unchanged; LocalRadio puts it on the listener.
    public sealed class AudioCeiling:MonoBehaviour
    {
        void OnAudioFilterRead(float[] data,int channels)
        {
            for(int i=0;i<data.Length;i++)
            {float v=data[i],a=Mathf.Abs(v);if(a>.96f)data[i]=Mathf.Sign(v)*(.96f+.03f*(1-Mathf.Exp(-(a-.96f)/.03f)));}
        }
    }
}
