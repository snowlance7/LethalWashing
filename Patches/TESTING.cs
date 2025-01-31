using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalWashing.Plugin;

/* bodyparts
 * 0 head
 * 1 right arm
 * 2 left arm
 * 3 right leg
 * 4 left leg
 * 5 chest
 * 6 feet
 * 7 right hip
 * 8 crotch
 * 9 left shoulder
 * 10 right shoulder */

namespace LethalWashing
{
    //[HarmonyPatch]
    internal class TESTING : MonoBehaviour
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {

        }

        public static void SpawnCoin(bool fromMachine, int amount = 1)
        {
            if (WashingMachine.Instance != null && fromMachine)
            {
                WashingMachine.Instance.SpawnCoin(amount);
                return;
            }
            if (fromMachine) return;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            string msg = __instance.chatTextField.text;
            string[] args = msg.Split(" ");
            //logIfDebug(msg);

            switch (args[0])
            {
                case "/spawn":
                    GameObject.Instantiate(PluginInstance.WashingMachineRef.spawnableMapObject.prefabToSpawn, localPlayer.transform.position + localPlayer.transform.forward * 2f, Quaternion.identity).GetComponent<NetworkObject>().Spawn(true);
                    break;
                case "/refresh":
                    RoundManager.Instance.RefreshEnemiesList();
                    HoarderBugAI.RefreshGrabbableObjectsInMapList();
                    break;
                case "/levels":
                    foreach (var level in StartOfRound.Instance.levels)
                    {
                        logger.LogDebug(level.levelID);
                        logger.LogDebug(level.name);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}