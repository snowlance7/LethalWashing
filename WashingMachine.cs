using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static LethalWashing.Plugin;

namespace LethalWashing
{
    public class WashingMachine : NetworkBehaviour
    {
        private static ManualLogSource logger = LoggerInstance;
        public static List<GrabbableObject> WashedObjects = [];

#pragma warning disable 0649
        public GameObject CoinPrefab = null!;
        public Animator animator = null!;
        public Transform CoinSpawn = null!;
        public InteractTrigger trigger = null!;
        public AudioSource WashingMachineAudio = null!;
        public AudioClip DingSFX = null!;
        public AudioClip DoorOpenSFX = null!;
        public AudioClip DoorCloseSFX = null!;
        public Transform DrumPosition = null!;
        public Collider triggerCollider = null!;
#pragma warning restore 0649

        bool LocalPlayerHoldingScrap { get { return localPlayer.currentlyHeldObjectServer != null && localPlayer.currentlyHeldObjectServer.itemProperties.isScrap; } }

        GrabbableObject? itemInDrum;
        GrabbableObject? coinInHatch;
        bool washing;
        float washTimer;
        bool canWashScrap;

        // Configs
        float washTime = 10f;

        public void Start()
        {
            washTime = configWashTime.Value;
            logger.LogDebug("Washing machine spawned");
        }

        public void Update()
        {
            if (washTimer > 0)
            {
                washTimer -= Time.deltaTime;

                if (washTimer <= 0)
                {
                    FinishWashScrap();
                }
            }

            if (coinInHatch != null && coinInHatch.playerHeldBy != null)
            {
                coinInHatch = null;
                animator.SetBool("hatchOpen", false);
            }

            if (itemInDrum != null && itemInDrum.playerHeldBy != null)
            {
                itemInDrum = null;
            }

            if (!washing && itemInDrum == null) // Default state
            {
                triggerCollider.enabled = true;

                if (LocalPlayerHoldingScrap)
                {
                    if (coinInHatch != null)
                    {
                        trigger.interactable = false;
                        trigger.disabledHoverTip = "Take coin from hatch";
                        canWashScrap = false;
                        return;
                    }

                    trigger.interactable = true;
                    trigger.hoverTip = "Wash Scrap [E]";
                    canWashScrap = true;
                }
                else
                {
                    trigger.interactable = false;
                    trigger.disabledHoverTip = "Requires scrap";
                    canWashScrap = false;
                }
                return;
            }
            if (itemInDrum != null && washing) // Item in drum and washing
            {
                triggerCollider.enabled = true;
                trigger.interactable = false;
                trigger.disabledHoverTip = "Washing scrap - " + washTimer.ToString();
                canWashScrap = false;
                return;
            }
            if (itemInDrum != null && !washing) // Item in drum after washing
            {
                triggerCollider.enabled = false;
                trigger.interactable = false;
                trigger.disabledHoverTip = "";
                canWashScrap = false;
            }
        }

        public void FinishWashScrap()
        {
            if (itemInDrum == null) { logger.LogError("Item in washing machine is null!"); return; }
            washing = false;
            animator.SetBool("doorOpen", true);
            animator.SetBool("hatchOpen", true);
            int coinValue = itemInDrum.scrapValue;
            itemInDrum.SetScrapValue(0);
            //itemInDrum.itemProperties.isScrap = false;
            WashedObjects.Add(itemInDrum);

            ScanNodeProperties itemScanNode = itemInDrum.gameObject.GetComponentInChildren<ScanNodeProperties>();
            if (itemScanNode != null)
            {
                itemScanNode.subText = "";
            }
            else
            {
                logger.LogError("Scan node is missing for item!: " + itemInDrum.gameObject.name);
            }

            itemInDrum.grabbable = true;
            WashingMachineAudio.PlayOneShot(DingSFX, 1f);

            if (IsServerOrHost)
            {
                coinInHatch = GameObject.Instantiate(CoinPrefab, CoinSpawn).GetComponentInChildren<PhysicsProp>();
                coinInHatch.NetworkObject.Spawn(destroyWithScene: true);
                SpawnCoinInHatchClientRpc(coinInHatch.NetworkObject, coinValue);
            }
        }

        public void WashScrap()
        {
            if (LocalPlayerHoldingScrap && !washing && itemInDrum == null && coinInHatch == null)
            {
                washing = true;
                itemInDrum = localPlayer.currentlyHeldObjectServer;
                localPlayer.DiscardHeldObject(true, null, DrumPosition.position);
                WashScrapServerRpc(itemInDrum.NetworkObject);
            }
        }

        // Animation stuff
        public void PlayDoorSFX()
        {
            if (washing)
            {
                WashingMachineAudio.PlayOneShot(DoorCloseSFX);
                WashingMachineAudio.Play();
            }
            else
            {
                WashingMachineAudio.PlayOneShot(DoorOpenSFX);
                WashingMachineAudio.Stop();
            }
        }
        
        // RPCs
        [ServerRpc(RequireOwnership = false)]
        public void WashScrapServerRpc(NetworkObjectReference netRef)
        {
            if (IsServerOrHost)
            {
                WashScrapClientRpc(netRef);
            }
        }

        [ClientRpc]
        public void WashScrapClientRpc(NetworkObjectReference netRef)
        {
            if (netRef.TryGet(out NetworkObject netObj))
            {
                itemInDrum = netObj.GetComponent<GrabbableObject>();
                itemInDrum.grabbable = false;
                washing = true;
                washTimer = washTime;
                animator.SetBool("doorOpen", false);
            }
        }

        [ClientRpc]
        public void SpawnCoinInHatchClientRpc(NetworkObjectReference netRef, int coinValue)
        {
            if (netRef.TryGet(out NetworkObject netObj))
            {
                coinInHatch = netObj.GetComponent<PhysicsProp>();
                if (coinInHatch == null) { logger.LogError("Coin in hatch is null!"); return; }
                coinInHatch.fallTime = 1f;
                coinInHatch.SetScrapValue(coinValue);
            }
        }
    }
}