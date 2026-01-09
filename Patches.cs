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
        static bool despawningProps = false;

        [HarmonyPrefix, HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Despawn))]
        public static bool DespawnPrefix(NetworkObject __instance)
        {
            try
            {
                if (!despawningProps) { return true; }
                if (StartOfRound.Instance.firingPlayersCutsceneRunning) { return true; }
                if (!__instance.TryGetComponent(out GrabbableObject grabObj)) { return true; }
                if (StartOfRound.Instance.isChallengeFile || (!grabObj.isHeld && !grabObj.isInShipRoom) || grabObj.deactivated) { return true; }
                if (grabObj.scrapValue > 0) { return true; }
                return false;
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        public static void DespawnPropsAtEndOfRoundPrefix(RoundManager __instance)
        {
            try
            {
                if (!IsServerOrHost) { return; }
                despawningProps = true;
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        public static void DespawnPropsAtEndOfRoundPostfix(RoundManager __instance)
        {
            try
            {
                if (!IsServerOrHost) { return; }
                despawningProps = false;
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnShipLandedMiscEvents))]
        public static void OnShipLandedMiscEventsPostfix()
        {
            try
            {
                Plugin.Instance.OnShipLanded.Invoke();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}