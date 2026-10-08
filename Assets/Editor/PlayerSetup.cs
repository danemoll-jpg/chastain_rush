using Racer;
using UnityEditor;
using UnityEngine;

namespace ChastainRush
{
    // Puts the Racer car (Assets/Vehicles/PrototypeCar.prefab) and its chase camera into the generated scene.
    // Camera values are the ones Racer's gameplay scenes use, apart from the far clip, which covers the whole map.
    static class PlayerSetup
    {
        const string VehiclePrefabPath = "Assets/Vehicles/PrototypeCar.prefab";

        public static void Place(float x, float z, float headingDeg, Terrain terrain)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VehiclePrefabPath);
            if (prefab == null) throw new System.Exception($"Missing {VehiclePrefabPath}");

            float ground = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
            var rotation = Quaternion.Euler(0f, headingDeg, 0f);
            var spawn = new GameObject("PlayerSpawn").transform;
            spawn.SetPositionAndRotation(new Vector3(x, ground + 0.8f, z), rotation);

            var car = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            car.transform.SetPositionAndRotation(spawn.position, rotation);
            var respawn = car.GetComponent<VehicleRespawn>();
            respawn.spawnPoint = spawn;
            respawn.fallResetHeight = terrain.transform.position.y - 20f;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 2500f;
            camGo.AddComponent<AudioListener>();
            var chase = camGo.AddComponent<ChaseCamera>();
            chase.target = car.transform;
            camGo.transform.position = spawn.position + rotation * new Vector3(0f, 3.6f, -7.5f);
            camGo.transform.LookAt(spawn.position);
        }
    }
}
