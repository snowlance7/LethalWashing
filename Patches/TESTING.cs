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
    [HarmonyPatch]
    internal class TESTING : MonoBehaviour
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            SpawnCoin();
        }

        public static void SpawnCoin() // TODO: HOW TF DO I GET THIS TO WORK
        {
            CoinBehavior coin = GameObject.Instantiate(PluginInstance.CoinPrefab, localPlayer.playerEye.transform.position, Quaternion.identity, StartOfRound.Instance.propsContainer).GetComponentInChildren<CoinBehavior>();
            coin.NetworkObject.Spawn();

            coin.parentObject = null;
            coin.transform.SetParent(StartOfRound.Instance.propsContainer, worldPositionStays: true);
            coin.EnablePhysics(true);
            coin.fallTime = 0f;
            coin.startFallingPosition = coin.transform.parent.InverseTransformPoint(coin.transform.position);
            Vector3 fallPosition = coin.GetGrenadeThrowDestination(localPlayer.playerEye.transform);
            coin.targetFloorPosition = coin.transform.parent.InverseTransformPoint(fallPosition);
            coin.floorYRot = -1;
            logger.LogDebug(coin.startFallingPosition);
            logger.LogDebug(fallPosition);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            string msg = __instance.chatTextField.text;
            string[] args = msg.Split(" ");
            //logger.LogDebug(msg);

            switch (args[0])
            {
                case "/spawn":
                    GameObject.Instantiate(PluginInstance.WashingMachineRef.spawnableMapObject.prefabToSpawn, localPlayer.transform.position + localPlayer.transform.forward * 2f, Quaternion.identity).GetComponent<NetworkObject>().Spawn(true);
                    break;
                case "/refresh":
                    RoundManager.Instance.RefreshEnemiesList();
                    HoarderBugAI.RefreshGrabbableObjectsInMapList();
                    break;
                default:
                    break;
            }
        }
    }
}