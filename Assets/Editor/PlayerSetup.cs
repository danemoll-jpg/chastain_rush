using Racer;
using UnityEditor;
using UnityEngine;

namespace ChastainRush
{
    // Puts the Racer car (Assets/Vehicles/PrototypeCar.prefab) and its chase camera into the generated scene.
    // Camera values are the ones Racer's gameplay scenes use, apart from the clip planes (fog hides everything past 650 m).
    static class PlayerSetup
    {
        const string VehiclePrefabPath = "Assets/Vehicles/PrototypeCar.prefab";

        public static void Place(float x, float z, float headingDeg, float ground, float terrainBaseY)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VehiclePrefabPath);
            if (prefab == null) throw new System.Exception($"Missing {VehiclePrefabPath}");

            var rotation = Quaternion.Euler(0f, headingDeg, 0f);
            var spawn = new GameObject("PlayerSpawn").transform;
            spawn.SetPositionAndRotation(new Vector3(x, ground + 0.8f, z), rotation);

            var car = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            car.transform.SetPositionAndRotation(spawn.position, rotation);
            var respawn = car.GetComponent<VehicleRespawn>();
            respawn.spawnPoint = spawn;
            respawn.fallResetHeight = terrainBaseY - 45f;   // Round 23 follow-ups: the pipe network's hub floor is 252 m, 20 m under the terrain's lowest point
            car.AddComponent<VehicleSwitch>();   // Round 13: Racer's vehicles (VehicleConfiguration, VehicleAudio), the Street Classic by default

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.25f;  // a larger near plane keeps depth precision for paint and roads at a distance
            cam.farClipPlane = 750f;    // the fog is solid by MapBuilder.FogEnd (650 m); trees stop at 700 m
            camGo.AddComponent<AudioListener>();
            var chase = camGo.AddComponent<ChaseCamera>();
            chase.target = car.transform;
            camGo.transform.position = spawn.position + rotation * new Vector3(0f, 3.6f, -7.5f);
            camGo.transform.LookAt(spawn.position);
        }
    }
}
