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

#pragma warning disable 0649
        public static GameObject WashingMachinePrefab = null!;
        public Animator animator = null!;
        public Transform CoinSpawn = null!;
        public InteractTrigger trigger = null!;
        public AudioSource WashingMachineAudio = null!;
        public AudioClip DingSFX = null!;
        public AudioClip WashingSFX = null!;
        public AudioClip DoorOpenSFX = null!;
        public AudioClip DoorCloseSFX = null!;
        public Transform DrumPosition = null!;
#pragma warning restore 0649

        bool LocalPlayerHoldingScrap { get { return localPlayer.currentlyHeldObjectServer != null && localPlayer.currentlyHeldObjectServer.itemProperties.isScrap; } }

        GrabbableObject? itemInDrum = null;
        bool washing;
        float washTimer;

        // Configs
        float washTime = 10f;

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

            if (itemInDrum != null && itemInDrum.playerHeldBy != null)
            {
                itemInDrum = null;
            }

            if (!washing && itemInDrum == null) // Default state
            {
                trigger.interactable = true;
                if (LocalPlayerHoldingScrap)
                {
                    trigger.hoverTip = "Wash Scrap [E]";
                }
                else
                {
                    trigger.hoverTip = "Must be carrying scrap!";
                }
                return;
            }
            if (itemInDrum != null && washing)
            {
                trigger.interactable = true;
                trigger.holdTip = "Washing scrap - " + washTimer.ToString();
                return;
            }
            if (itemInDrum != null && !washing)
            {
                trigger.interactable = false;
            }
        }

        public void FinishWashScrap()
        {
            washing = false;
            animator.SetBool("doorOpen", true);
            // Spawn coin
            animator.SetBool("hatchOpen", true);
        }

        public void WashScrap()
        {
            if (LocalPlayerHoldingScrap && !washing && !itemInDrum)
            {
                washing = true;
                itemInDrum = localPlayer.currentlyHeldObjectServer;
                localPlayer.DiscardHeldObject(true, NetworkObject, DrumPosition.position);
                WashScrapServerRpc(itemInDrum.NetworkObject);
            }
        }
        
        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
        }

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
                washing = true;
                washTimer = washTime;
                animator.SetBool("doorOpen", false);
            }
        }
    }
}