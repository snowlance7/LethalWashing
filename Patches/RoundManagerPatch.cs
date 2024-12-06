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
    internal class RoundManagerPatch
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        public static bool DespawnPropsAtEndOfRoundPrefix(RoundManager __instance, bool despawnAllItems = false)
        {
            try
            {
                if (IsServerOrHost)
                {
                    GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
                    try
                    {
                        VehicleController[] array2 = UnityEngine.Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
                        for (int i = 0; i < array2.Length; i++)
                        {
                            if (!array2[i].magnetedToShip)
                            {
                                if (array2[i].NetworkObject != null)
                                {
                                    Debug.Log("Despawn vehicle");
                                    array2[i].NetworkObject.Despawn(destroy: false);
                                }
                            }
                            else
                            {
                                array2[i].CollectItemsInTruck();
                            }
                        }
                    }
                    catch (Exception arg)
                    {
                        Debug.LogError($"Error despawning vehicle: {arg}");
                    }
                    BeltBagItem[] array3 = UnityEngine.Object.FindObjectsByType<BeltBagItem>(FindObjectsSortMode.None);
                    for (int j = 0; j < array3.Length; j++)
                    {
                        if ((bool)array3[j].insideAnotherBeltBag && (array3[j].insideAnotherBeltBag.isInShipRoom || array3[j].insideAnotherBeltBag.isHeld))
                        {
                            array3[j].isInElevator = true;
                            array3[j].isInShipRoom = true;
                        }
                        if (array3[j].isInShipRoom || array3[j].isHeld)
                        {
                            for (int k = 0; k < array3[j].objectsInBag.Count; k++)
                            {
                                array3[j].objectsInBag[k].isInElevator = true;
                                array3[j].objectsInBag[k].isInShipRoom = true;
                            }
                        }
                    }
                    for (int l = 0; l < array.Length; l++)
                    {
                        if (array[l] == null)
                        {
                            continue;
                        }
                        if (despawnAllItems || (!array[l].isHeld && !array[l].isInShipRoom) || array[l].deactivated || (StartOfRound.Instance.allPlayersDead && array[l].itemProperties.isScrap && !WashingMachine.WashedObjects.Contains(array[l])))
                        {
                            if (array[l].isHeld && array[l].playerHeldBy != null)
                            {
                                array[l].playerHeldBy.DropAllHeldItemsAndSync();
                            }
                            NetworkObject component = array[l].gameObject.GetComponent<NetworkObject>();
                            if (component != null && component.IsSpawned)
                            {
                                Debug.Log("Despawning prop");
                                array[l].gameObject.GetComponent<NetworkObject>().Despawn();
                            }
                            else
                            {
                                Debug.Log("Error/warning: prop '" + array[l].gameObject.name + "' was not spawned or did not have a NetworkObject component! Skipped despawning and destroyed it instead.");
                                UnityEngine.Object.Destroy(array[l].gameObject);
                            }
                        }
                        else
                        {
                            array[l].scrapPersistedThroughRounds = true;
                        }
                        if (__instance.spawnedSyncedObjects.Contains(array[l].gameObject))
                        {
                            __instance.spawnedSyncedObjects.Remove(array[l].gameObject);
                        }
                    }
                    GameObject[] array4 = GameObject.FindGameObjectsWithTag("TemporaryEffect");
                    for (int m = 0; m < array4.Length; m++)
                    {
                        UnityEngine.Object.Destroy(array4[m]);
                    }
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return true;
            }
            return false;
        }
    }
}