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

        bool LocalPlayerHoldingScrap { get { return localPlayer.currentlyHeldObjectServer != null && localPlayer.currentlyHeldObjectServer.itemProperties.isScrap && localPlayer.currentlyHeldObjectServer.GetComponent<NoDespawnScript>() == null; } }

        public List<GrabbableObject> itemsInDrum = [];
        //GrabbableObject? itemInDrum;
        bool washing;
        float washTimer;
        bool readyForNextWash;

        // Configs
        float washTime = 10f;
        bool multipleWashes = false;
        public static Vector3 worldPosition = new Vector3(-27.6681f, -2.5747f, -24.764f); // -27.6681 -2.5747 -24.764
        public static Quaternion worldRotation = Quaternion.Euler(0f, 90f, 0f); // 0 90 0

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (Instance != null)
            {
                Instance.NetworkObject.Despawn();
            }

            Instance = this;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Instance = null;
        }

        public void Start()
        {
            washTime = configWashTime.Value;
            multipleWashes = configMultipleWashes.Value;
            trigger.hoverTip = multipleWashes ? "Add Scrap [E]" : "Wash Scrap [E]";
            LoggerInstance.LogDebug("Washing machine spawned");
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

            doorCollider.enabled = !washing && itemsInDrum.Count > 0 && readyForNextWash;

            if (!washing && itemsInDrum.Count >= 0) // Default state
            {
                if (localPlayer.currentlyHeldObjectServer != null) // Player is holding item
                {
                    if (LocalPlayerHoldingScrap) // Player is holding scrap
                    {
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

                if (item.GetComponent<NoDespawnScript>() == null) { item.gameObject.AddComponent<NoDespawnScript>(); }

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
            yield return new WaitForSeconds(3f);

            foreach (var coinValue in coinValues)
            {
                if (coinValue <= 0) { continue; }
                CoinBehavior coin = GameObject.Instantiate(CoinPrefab, CoinSpawn.transform.position, Quaternion.identity).GetComponentInChildren<CoinBehavior>();
                coin.NetworkObject.Spawn();

                coin.startFallingPosition = CoinSpawn.position;
                Vector3 fallPosition = coin.GetGrenadeThrowDestination(CoinSpawn);
                SpawnCoinFromHatchClientRpc(coin.NetworkObject, coinValue, fallPosition);
                yield return new WaitForSeconds(1f);
            }

            DoAnimationClientRpc("hatchOpen", false);
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
                itemsInDrum.Add(item);
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
                washTimer = washTime;
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
        public void SpawnCoinFromHatchClientRpc(NetworkObjectReference netRef, int coinValue, Vector3 fallPosition)
        {
            if (netRef.TryGet(out NetworkObject netObj))
            {
                CoinBehavior coin = netObj.GetComponent<CoinBehavior>();
                if (coin == null) { LoggerInstance.LogError("Coin in hatch is null!"); return; }
                coin.SetScrapValue(coinValue);
                coin.transform.SetParent(StartOfRound.Instance.propsContainer, true);
                coin.startFallingPosition = coin.transform.position;
                coin.targetFloorPosition = fallPosition;
                coin.fallTime = 0f;
                coin.hasHitGround = false;
            }
        }
    }
}