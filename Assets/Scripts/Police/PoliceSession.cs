using System.Collections;
using System.Linq;
using ChastainRush;
using UnityEngine;

namespace Racer
{
    // Chastain Rush, Round 39 (Part D): starting and ending a police mode from the POLICE page (Racer did it through RaceFlow.StartSplit and its roam
    // scene; here the modes run in this Free Roam): one player on the full screen, or two (a split-screen Free Roam session begun for it, ended with
    // it). Free Roam only: a race ends any police mode (and none starts in a race).
    public sealed class PoliceSession : MonoBehaviour
    {
        public static bool Running => (GetawayChase.Current && !GetawayChase.Current.Embedded) || SpeedPatrol.Current || PoliceChase.Current;
        public static bool OwnsSplit;   // the split-screen session is the police mode's (ended with it)
        static PoliceSession runner;
        // a mode's end, for the menus' results (an embedded chase, a hidden cruiser's, has none)
        public static System.Action Finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            Finished = null;
            GetawayChase.Finished = g => { if (g && !g.Embedded) Finished?.Invoke(); };
            SpeedPatrol.Finished = _ => Finished?.Invoke();
            PoliceChase.Finished = _ => Finished?.Invoke();
        }
        public static void Launch()
        {
            Stop(false);
            if (!runner) { runner = new GameObject("Police session").AddComponent<PoliceSession>(); DontDestroyOnLoad(runner.gameObject); }
            runner.StartCoroutine(runner.Begin());
        }
        IEnumerator Begin()
        {
            if (PoliceSetup.Two && !SplitScreen.Active)
            {
                SplitScreen.P2Ai = PoliceSetup.Mode == PoliceSetup.Game.Getaway && PoliceSetup.P2AiRunner;
                SplitScreen.Begin(SplitScreen.Kind.FreeRoam); OwnsSplit = true;
                for (int i = 0; i < 30 && !SplitScreen.P2Car; i++) yield return null;
                yield return null;
            }
            switch (PoliceSetup.Mode)
            {
                case PoliceSetup.Game.Getaway: GetawayChase.Begin(); break;
                case PoliceSetup.Game.SpeedPatrol: SpeedPatrol.Begin(); break;
                default: PoliceChase.Begin(); break;
            }
        }
        // the mode ended (and its split screen, when it began one)
        public static void Stop(bool endSplit = true)
        {
            if (GetawayChase.Current && !GetawayChase.Current.Embedded) GetawayChase.End(); SpeedPatrol.End(); PoliceChase.End();
            if (endSplit && OwnsSplit) { SplitScreen.End(); OwnsSplit = false; }
            PoliceRadio.Clear();
        }
        public static void Rematch()
        {
            if (GetawayChase.Current) GetawayChase.Current.Rematch(); else if (SpeedPatrol.Current) SpeedPatrol.Current.Rematch(); else if (PoliceChase.Current) PoliceChase.Current.Rematch();
        }
        public static bool Done => (GetawayChase.Current && GetawayChase.Current.State == GetawayChase.Phase.Done) || (SpeedPatrol.Current && SpeedPatrol.Current.State == SpeedPatrol.Phase.Done) || (PoliceChase.Current && PoliceChase.Current.State == PoliceChase.Phase.Done);
    }
}
