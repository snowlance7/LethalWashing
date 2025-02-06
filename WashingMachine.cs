using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalWashing.Plugin;

namespace LethalWashing
{
    public class WashingMachine : NetworkBehaviour
    {
        public static WashingMachine? Instance;

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
        public InteractTrigger doorTrigger = null!;
        public Collider doorCollider = null!;
#pragma warning restore 0649

        bool LocalPlayerHoldingScrap { get { return localPlayer.currentlyHeldObjectServer != null && localPlayer.currentlyHeldObjectServer.itemProperties.isScrap; } }

        public List<GrabbableObject> itemsInDrum = [];
        bool washing;
        float washTimer;
        bool readyForNextWash;

        // Configs
        public static Vector3 worldPosition = new Vector3(-27.6681f, -2.5747f, -24.764f); // -27.6681 -2.5747 -24.764
        public static Quaternion worldRotation = Quaternion.Euler(0f, 90f, 0f); // 0 90 0
        public static Vector3 worldPositionGaletry = new Vector3(-65.2742f, 1.1536f, 20.3886f); // -65.2742 1.1536 20.3886
        public static Quaternion worldRotationGaletry = Quaternion.Euler(0f, 180f, 0f); // 0 180 0

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (Instance != null && Instance != this)
            {
                if (!IsServerOrHost) { return; }
                Instance.NetworkObject.Despawn();
                return;
            }

            Instance = this;
            LoggerInstance.LogDebug("Washing Machine spawned at " + transform.position);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (Instance == this)
            {
                Instance = null;
            }
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

            UpdateDrum();

            doorCollider.enabled = !washing && itemsInDrum.Count > 0 && readyForNextWash;

            if (!washing && itemsInDrum.Count >= 0) // Default state
            {
                if (localPlayer.currentlyHeldObjectServer != null) // Player is holding item
                {
                    if (LocalPlayerHoldingScrap) // Player is holding scrap
                    {
                        if (itemsInDrum.Count >= configMaxItemsInMachine.Value)
                        {
                            trigger.disabledHoverTip = "Washing machine is full";
                            triggerCollider.enabled = true;
                            trigger.interactable = false;
                            return;
                        }
                        trigger.hoverTip = "Add Scrap [E]";
                        triggerCollider.enabled = true;
                        trigger.interactable = true;
                    }
                    else // Player is holding something but it isnt scrap
                    {
                        triggerCollider.enabled = true;
                        trigger.interactable = false;
                        trigger.disabledHoverTip = "Requires scrap";
                    }
                }
                else // Player is holding nothing
                {
                    triggerCollider.enabled = false;
                    trigger.interactable = false;
                }
                return;
            }
            if (itemsInDrum.Count > 0 && washing) // Items in drum and washing
            {
                doorCollider.enabled = false;
                triggerCollider.enabled = true;
                trigger.interactable = false;
                trigger.disabledHoverTip = "Washing scrap - " + ((int)washTimer).ToString();
                return;
            }
        }

        public void UpdateDrum()
        {
            foreach (var item in itemsInDrum.ToList())
            {
                if (item.isHeld) { itemsInDrum.Remove(item); }
            }
        }

        public void FinishWashScrap()
        {
            if (itemsInDrum.Count == 0) { LoggerInstance.LogError("Items in washing machine is null!"); return; }
            washing = false;
            animator.SetBool("doorOpen", true);

            List<int> coinValues = [];
            foreach (var item in itemsInDrum)
            {
                if (item.scrapValue > 0)
                {
                    coinValues.Add(item.scrapValue);
                    item.SetScrapValue(0);
                }

                ScanNodeProperties itemScanNode = item.gameObject.GetComponentInChildren<ScanNodeProperties>();
                if (itemScanNode != null)
                {
                    itemScanNode.subText = "";
                }
                else
                {
                    LoggerInstance.LogError("Scan node is missing for item!: " + item.gameObject.name);
                }

                item.grabbable = true;
            }

            if (IsServerOrHost)
            {
                if (coinValues.Count == 0) { return; }
                StartCoroutine(SpawnCoinsCoroutine(coinValues));
            }
        }

        IEnumerator SpawnCoinsCoroutine(List<int> coinValues)
        {
            DoAnimationClientRpc("hatchOpen", true);
            yield return new WaitForSeconds(1.5f);

            foreach (var coinValue in coinValues)
            {
                if (coinValue <= 0) { continue; }
                SpawnCoin(coinValue);
                yield return new WaitForSeconds(0.1f);
            }

            DoAnimationClientRpc("hatchOpen", false);
        }

        public void SpawnCoin(int value)
        {
            CoinBehavior coin = GameObject.Instantiate(CoinPrefab, CoinSpawn.transform.position, CoinSpawn.transform.rotation).GetComponentInChildren<CoinBehavior>();
            coin.SetScrapValue(value);
            coin.NetworkObject.Spawn();
            SpawnCoinFromHatchClientRpc(coin.NetworkObject, value);
        }

        // Interact Trigger stuff

        public void StartWash()
        {
            WashScrapServerRpc();
        }

        public void AddScrapToWasher()
        {
            if (LocalPlayerHoldingScrap && !washing)
            {
                GrabbableObject item = localPlayer.currentlyHeldObjectServer;
                localPlayer.DiscardHeldObject(true, null, DrumPosition.position);
                AddItemToDrumServerRpc(item.NetworkObject);
            }
        }

        // Animation stuff

        public void PlayDoorSFX()
        {
            if (washing)
            {
                WashingMachineAudio.PlayOneShot(DoorCloseSFX);
                WashingMachineAudio.Play();
                washTimer = configWashTime.Value;
                readyForNextWash = false;
            }
            else
            {
                WashingMachineAudio.Stop();
                WashingMachineAudio.PlayOneShot(DingSFX);
                WashingMachineAudio.PlayOneShot(DoorOpenSFX);
                readyForNextWash = true;
            }
        }

        // RPCs

        [ServerRpc(RequireOwnership = false)]
        public void RemoveItemFromDrumServerRpc(NetworkObjectReference netRef)
        {
            if (!IsServerOrHost) { return; }
            RemoveItemFromDrumClientRpc(netRef);
        }

        [ClientRpc]
        public void RemoveItemFromDrumClientRpc(NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject obj)) { return; }
            itemsInDrum.Remove(obj);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddItemToDrumServerRpc(NetworkObjectReference netRef)
        {
            if (!IsServerOrHost) { return; }
            AddItemToDrumClientRpc(netRef);
        }

        [ClientRpc]
        public void AddItemToDrumClientRpc(NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject obj)) { return; }
            itemsInDrum.Add(obj);
        }

        [ClientRpc]
        public void DoAnimationClientRpc(string animationName, bool value)
        {
            animator.SetBool(animationName, value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void WashScrapServerRpc()
        {
            if (!IsServerOrHost) { return; }
            WashScrapClientRpc();
        }

        [ClientRpc]
        public void WashScrapClientRpc()
        {
            washing = true;
            foreach (var item in itemsInDrum)
            {
                item.grabbable = false;
            }
            animator.SetBool("doorOpen", false);
        }

        [ClientRpc]
        public void SpawnCoinFromHatchClientRpc(NetworkObjectReference netRef, int coinValue)
        {
            if (netRef.TryGet(out NetworkObject netObj))
            {
                CoinBehavior coin = netObj.GetComponent<CoinBehavior>();
                if (coin == null) { LoggerInstance.LogError("Coin in hatch is null!"); return; }
                coin.SetScrapValue(coinValue);
            }
        }
    }
}