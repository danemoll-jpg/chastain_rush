using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChastainRush
{
    // Round 92 (item 0): the player build starts with a tiny Boot scene that loads the world scene at once. Between them stand a few "data"
    // scenes that are never loaded: each only refers to a share of the world's meshes and terrain, so Unity writes that share into its own
    // sharedassetsN.assets and no data file reaches 2 GiB (the installed launcher refuses any file that size; PlayerBuild.DataScenes).
    // Startup code marked AfterSceneLoad would run in the Boot scene: it calls Defer and runs once the world scene has loaded (the same
    // moment AfterSceneLoad gives it when the world scene is the first: after its objects' Awake, before their Start). In the Editor, Play
    // starts in the world scene and nothing is deferred.
    public sealed class WorldBoot : MonoBehaviour
    {
        public const string BootScene = "Boot";
        public string worldScene;

        static readonly List<Action> later = new List<Action>();
        public static bool Booting => SceneManager.GetActiveScene().name == BootScene;

        // true: the call waits for the world scene (the caller returns)
        public static bool Defer(Action a)
        {
            if (!Booting) return false;
            later.Add(a); return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetProcess() { later.Clear(); SceneManager.sceneLoaded -= Loaded; }

        void Awake()
        {
            SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
            SceneManager.LoadScene(worldScene, LoadSceneMode.Single);
        }

        static void Loaded(Scene s, LoadSceneMode mode)
        {
            if (s.name == BootScene) return;
            SceneManager.sceneLoaded -= Loaded;
            if (SceneManager.GetActiveScene() != s) SceneManager.SetActiveScene(s);
            var run = later.ToArray(); later.Clear();
            foreach (var a in run) { try { a(); } catch (Exception e) { Debug.LogException(e); } }
        }
    }
}
