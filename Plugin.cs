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
        public static Plugin PluginInstance = null!;
        public static ManualLogSource LoggerInstance = null!;
        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        public static PlayerControllerB localPlayer { get { return GameNetworkManager.Instance.localPlayerController; } }
        public static bool IsServerOrHost { get { return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost; } }
        public static PlayerControllerB PlayerFromId(ulong id) { return StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[id]]; }

        public SpawnableMapObjectDef WashingMachineRef = null!;
        public static AssetBundle ModAssets = null!;

        // Configs
        public static ConfigEntry<bool>? configHideCodeOnTerminal;


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

            // MainNest
            configHideCodeOnTerminal = Config.Bind("Nest", "Hide Code On Terminal", true, "If set to true, will make the code on the ship monitor for the nest be ??, requiring the player to find the nest physically to turn off its spawning.");


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
            LoggerInstance.LogDebug($"Registering WashingMachine");
            MapObjects.RegisterMapObject(WashingMachineRef);

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
