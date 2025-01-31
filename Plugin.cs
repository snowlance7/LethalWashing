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
        public static ConfigEntry<int> configMaxItemsInMachine;
        public static ConfigEntry<bool> configEnableDebugging;
        public static ConfigEntry<bool> configUseDespawnScript;

        public static ConfigEntry<Vector3> configWorldPosition;
        public static ConfigEntry<Quaternion> configWorldRotation;
        public static ConfigEntry<Vector3> configGaletryWorldPosition;
        public static ConfigEntry<Quaternion> configGaletryWorldRotation;
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
            configMaxItemsInMachine = Config.Bind("General", "Max items in machine", 5, "How many items can fit in the machine at a time");
            configEnableDebugging = Config.Bind("General", "Enable Debugging", false, "Shows debugging logs in the console");
            configUseDespawnScript = Config.Bind("General", "Use Despawn Script", false, "In order to prevent items from despawning from the ship on team wipe by default, this mod will check if the item is worth a value of 0 to determine if the item should not despawn. If this is enabled, washing a scrap will attach a script to the item and it will check if the script is on the item to determine if it should not despawn instead. (reloading a save will cause the script to be removed so youll have to rewash items again)");

            configWorldPosition = Config.Bind("General", "World Position", new Vector3(-27.6681f, -2.5747f, -24.764f), "The world spawn position of the washing machine at the company. By default it spawns next to the sell counter.");
            configWorldRotation = Config.Bind("General", "World Rotation", Quaternion.Euler(0f, 90f, 0f), "The world spawn rotation of the washing machine at the company.");

            configGaletryWorldPosition = Config.Bind("General", "Galetry World Position", new Vector3(-65.2742f, 1.1536f, 20.3886f), "The world spawn position of the washing machine at Galetry. By default it spawns next to the sell counter.");
            configGaletryWorldRotation = Config.Bind("General", "Galetry World Rotation", Quaternion.Euler(0f, 180f, 0f), "The world spawn rotation of the washing machine at Galetry.");

            WashingMachine.worldPosition = configWorldPosition.Value;
            WashingMachine.worldRotation = configWorldRotation.Value;

            WashingMachine.worldPositionGaletry = configGaletryWorldPosition.Value;
            WashingMachine.worldRotationGaletry = configGaletryWorldRotation.Value;

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

        public static void logIfDebug(string message)
        {
            if (!configEnableDebugging.Value) { return; }
            LoggerInstance.LogDebug(message);
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
