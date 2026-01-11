using BepInEx.Logging;
using Dusk;
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
        static string[] teamWipeBlacklist = [];
        static bool despawningProps = false;

        [HarmonyPrefix, HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Despawn))]
        public static bool DespawnPrefix(NetworkObject __instance)
        {
            try
            {
                if (!despawningProps) { return true; }
                if (StartOfRound.Instance.firingPlayersCutsceneRunning) { return true; }
                if (!__instance.TryGetComponent(out GrabbableObject grabObj)) { return true; }
                if (teamWipeBlacklist.Contains(grabObj.itemProperties.name)) { return true; }
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
                despawningProps = Configs.PreventDespawnOnTeamWipe;
                if (!despawningProps) { return; }
                teamWipeBlacklist = Configs.TeamWipeBlacklist.Replace(" ", "").Split(",");
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
            despawningProps = false;
        }
    }
}