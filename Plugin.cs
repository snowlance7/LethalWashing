using AmazingAssets.TerrainToMesh;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Extras;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace LethalWashing
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin PluginInstance;
        public static ManualLogSource LoggerInstance;
        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        public static PlayerControllerB localPlayer { get { return GameNetworkManager.Instance.localPlayerController; } }
        public static bool IsServerOrHost { get { return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost; } }
        public static PlayerControllerB PlayerFromId(ulong id) { return StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[id]]; }

        public SpawnableMapObjectDef WashingMachineRef = null!;
        public static AssetBundle ModAssets;
        public GameObject CoinPrefab;

        // Configs
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static ConfigEntry<float> configWashTime;
        public static ConfigEntry<Vector3> configWorldPosition;
        public static ConfigEntry<Quaternion> configWorldRotation;
        public static ConfigEntry<bool> configMultipleWashes;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


        private void Awake()
        {
            if (PluginInstance == null)
            {
                PluginInstance = this;
            }

            LoggerInstance = PluginInstance.Logger;

            harmony.PatchAll();

            InitializeNetworkBehaviours();

            // Configs

            // General
            configWashTime = Config.Bind("General", "Wash Time", 10f, "Time it takes for the washing machine to finish washing scrap.");
            configWorldPosition = Config.Bind("General", "World Position", new Vector3(-27.6681f, -2.5747f, -24.764f), "The world spawn position of the washing machine at the company. By default it spawns next to the sell counter.");
            configWorldRotation = Config.Bind("General", "World Rotation", Quaternion.Euler(0f, 90f, 0f), "The world spawn rotation of the washing machine at the company.");
            configMultipleWashes = Config.Bind("General", "Multiple Washes", false, "Allows for you to wash multiple items at once");

            WashingMachine.worldPosition = configWorldPosition.Value;
            WashingMachine.worldRotation = configWorldRotation.Value;

            // Loading Assets
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "washing_assets"));
            if (ModAssets == null)
            {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }
            LoggerInstance.LogDebug($"Got AssetBundle at: {Path.Combine(sAssemblyLocation, "washing_assets")}");

            WashingMachineRef = ModAssets.LoadAsset<SpawnableMapObjectDef>("Assets/ModAssets/WashingMachine.asset");
            if (WashingMachineRef == null) { LoggerInstance.LogError("Error: Couldnt get WashingMachine from assets"); return; }
            LoggerInstance.LogDebug("Registering WashingMachine network prefab...");
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(WashingMachineRef.spawnableMapObject.prefabToSpawn);
            LethalLib.Modules.Utilities.FixMixerGroups(WashingMachineRef.spawnableMapObject.prefabToSpawn);
            LoggerInstance.LogDebug($"Registering WashingMachine");
            MapObjects.RegisterMapObject(WashingMachineRef);

            Item Coin = ModAssets.LoadAsset<Item>("Assets/ModAssets/CoinItem.asset");
            if (Coin == null) { LoggerInstance.LogError("Error: Couldnt get CoinItem from assets"); return; }
            LoggerInstance.LogDebug($"Got Coin prefab");

            CoinPrefab = Coin.spawnPrefab;
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(Coin.spawnPrefab);
            LethalLib.Modules.Utilities.FixMixerGroups(Coin.spawnPrefab);
            LethalLib.Modules.Items.RegisterScrap(Coin);

            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        private static void InitializeNetworkBehaviours()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            LoggerInstance.LogDebug("Finished initializing network behaviours");
        }
    }
}
