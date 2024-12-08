using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalWashing.Plugin;

namespace LethalWashing
{
    [HarmonyPatch]
    internal class Patches
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        [HarmonyPrefix, HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Despawn))]
        public static bool DespawnPrefix(NetworkObject __instance)
        {
            try
            {
                if (StartOfRound.Instance.firingPlayersCutsceneRunning) { return true; }
                if (!__instance.TryGetComponent<GrabbableObject>(out GrabbableObject grabObj)) { return true; }
                if (!grabObj.TryGetComponent<NoDespawnScript>(out NoDespawnScript despawnScript)) { return true; }
                if (StartOfRound.Instance.isChallengeFile || (!grabObj.isHeld && !grabObj.isInShipRoom) || grabObj.deactivated) { return true; }
                return false;
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        public static void DespawnPropsAtEndOfRoundPatch(RoundManager __instance)
        {
            try
            {
                if (!IsServerOrHost) { return; }
                if (__instance.currentLevel.levelID != 3) { return; }
                if (WashingMachine.Instance == null) { return; }
                WashingMachine.Instance.NetworkObject.Despawn();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevelWait))]
        public static void LoadNewLevelWaitPrefix(RoundManager __instance)
        {
            try
            {
                if (!IsServerOrHost) { return; }
                if (__instance.currentLevel.levelID != 3) { return; }

                UnityEngine.GameObject.Instantiate(PluginInstance.WashingMachineRef.spawnableMapObject.prefabToSpawn, WashingMachine.worldPosition, WashingMachine.worldRotation).GetComponent<NetworkObject>().Spawn(true);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}