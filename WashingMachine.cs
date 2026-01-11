using DigitalRuby.ThunderAndLightning;
using Dusk;
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
        public static WashingMachine? Instance { get; private set; }

#pragma warning disable CS8618
        public Animator animator;

        public Transform coinSpawn;
        public Transform drumPosition;

        public InteractTrigger drumTrigger;
        public InteractTrigger doorTrigger;

        public AudioSource audioSource;
        public AudioClip dingSFX;
        public AudioClip doorOpenSFX;
        public AudioClip doorCloseSFX;

        public Collider drumCollider;
        public Collider doorCollider;

        public PlaceableObjectsSurface placeableSurface;
        public PlaceableShipObject placeableShipObject;
#pragma warning restore CS8618

        bool localPlayerHoldingScrap { get { return localPlayer.isHoldingObject && localPlayer.currentlyHeldObjectServer != null && localPlayer.currentlyHeldObjectServer.itemProperties.isScrap; } }

        public List<GrabbableObject> itemsInDrum = [];
        bool washing => washTimer > 0;
        float washTimer;
        bool readyForNextWash;
        bool usable => TimeOfDay.Instance.daysUntilDeadline <= 0 || !usableOnlyOnCompanyDay;
        bool doorOpen;

        bool closedUntilCompanyDay = true;

        // Configs
        float washTime => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<float>("Wash Time").Value;
        bool usableOnlyOnCompanyDay => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<bool>("Usable Only On Company Day").Value;
        bool combineCoinValues => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<bool>("Combine Coin Values").Value;
        int coinsToSpawn => ContentHandler<LethalWashingContentHandler>.Instance.WashingMachine!.GetConfig<int>("Coins To Spawn").Value;


        public void Start()
        {
            Instance = this;
            logger.LogDebug("Washing Machine spawned at " + transform.position);
        }
        public override void OnDestroy()
        {
            Instance = null;
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

            doorCollider.enabled = !washing && doorOpen && itemsInDrum.Count > 0 && readyForNextWash && usable && localPlayer.currentlyHeldObjectServer == null;
            drumCollider.enabled = !washing && readyForNextWash && localPlayer.currentlyHeldObjectServer != null;
            drumTrigger.interactable = !washing && doorOpen && readyForNextWash && usable && localPlayerHoldingScrap;

            foreach (var item in itemsInDrum.ToList())
            {
                if (item.isHeld || item.playerHeldBy != null) { itemsInDrum.Remove(item); }
            }

            if (!readyForNextWash && itemsInDrum.Count <= 0)
            {
                readyForNextWash = true;
            }

            if (!usable)
            {
                if (itemsInDrum.Count <= 0 && doorOpen)
                {
                    OpenDoor(false);
                    closedUntilCompanyDay = true;
                }
                drumTrigger.disabledHoverTip = "No washing until company day";
            }

            if (usable && closedUntilCompanyDay)
            {
                closedUntilCompanyDay = false;
                OpenDoor(true);
            }

            if (!washing) // Default state
            {
                if (localPlayer.currentlyHeldObjectServer != null) // Player is holding item
                {
                    if (localPlayerHoldingScrap) // Player is holding scrap
                    {
                        drumTrigger.hoverTip = "Add Scrap [E]";
                        //drumCollider.enabled = true;
                        //drumTrigger.interactable = true;
                    }
                    else // Player is holding something but it isnt scrap
                    {
                        //drumCollider.enabled = true;
                        //drumTrigger.interactable = false;
                        drumTrigger.disabledHoverTip = "Requires scrap";
                    }
                }
                else // Player is holding nothing
                {
                    //drumCollider.enabled = false;
                    //drumTrigger.interactable = false;
                }
                return;
            }
            else // Items in drum and washing
            {
                //doorCollider.enabled = false;
                //drumCollider.enabled = true;
                //drumTrigger.interactable = false;
                drumTrigger.disabledHoverTip = "Washing scrap - " + ((int)washTimer).ToString();
                return;
            }
        }

        public void OpenDoor(bool open)
        {
            animator.SetBool("doorOpen", open);
            doorOpen = open;
        }

        public void FinishWashScrap()
        {
            if (itemsInDrum.Count == 0) { return; }
            OpenDoor(true);

            List<int> scrapValues = [];
            foreach (var item in itemsInDrum)
            {
                if (item.scrapValue > 0)
                {
                    scrapValues.Add(item.scrapValue);
                    item.SetScrapValue(0);
                }

                ScanNodeProperties? itemScanNode = item.gameObject.GetComponentInChildren<ScanNodeProperties>();
                if (itemScanNode != null)
                {
                    itemScanNode.subText = "";
                }
                else
                {
                    logger.LogWarning("Cant find scannode for " + item.itemProperties.name);
                }

                item.grabbable = true;
            }

            if (scrapValues.Count == 0) { logger.LogDebug("No scrap values"); return; }
            SpawnCoins(scrapValues);
        }

        void SpawnCoins(List<int> values)
        {
            if (combineCoinValues && coinsToSpawn > 0)
            {
                int totalValue = values.Sum();

                if (totalValue > 0)
                {
                    values.Clear();

                    int baseValue = totalValue / coinsToSpawn;
                    int remainder = totalValue % coinsToSpawn;

                    for (int i = 0; i < coinsToSpawn; i++)
                    {
                        values.Add(baseValue + (i < remainder ? 1 : 0));
                    }
                }
            }

            logger.LogDebug($"Spawning {values.Count} coins");
            StartCoroutine(SpawnCoinsCoroutine(values));
        }

        IEnumerator SpawnCoinsCoroutine(List<int> coinValues)
        {
            animator.SetBool("hatchOpen", true);
            yield return new WaitForSeconds(1.5f);

            foreach (var coinValue in coinValues)
            {
                if (coinValue <= 0) { continue; }
                SpawnCoin(coinValue);
                yield return new WaitForSeconds(0.1f);
            }

            animator.SetBool("hatchOpen", false);
        }

        void SpawnCoin(int value)
        {
            if (!IsServer) { return; }
            CoinBehavior coin = Utils.SpawnItem(LethalWashingKeys.Coin, coinSpawn.transform.position, coinSpawn.transform.rotation)!.GetComponentInChildren<CoinBehavior>();
            coin.SetScrapValueClientRpc(value);
        }

        public void StartWash() // InteractTrigger
        {
            WashScrapServerRpc();
        }

        public void AddScrapToWasher() // InteractTrigger
        {
            GrabbableObject item = localPlayer.currentlyHeldObjectServer;
            if (!localPlayerHoldingScrap || localPlayer.isGrabbingObjectAnimation) { return; }
            //localPlayer.DiscardHeldObject(true, null, drumPosition.position);
            localPlayer.DiscardHeldObject(true, NetworkObject, NetworkObject.transform.InverseTransformPoint(drumPosition.position), false);
            AddItemToDrumServerRpc(item.NetworkObject);
        }

        public void PlayDoorSFX() // Animation
        {
            if (!usable) { return; }
            if (washing)
            {
                audioSource.PlayOneShot(doorCloseSFX);
                audioSource.Play();
                readyForNextWash = false;
            }
            else
            {
                audioSource.Stop();
                audioSource.PlayOneShot(dingSFX);
                audioSource.PlayOneShot(doorOpenSFX);
            }
        }

        // RPCs

        [ServerRpc(RequireOwnership = false)]
        public void AddItemToDrumServerRpc(NetworkObjectReference netRef)
        {
            if (!IsServer) { return; }
            AddItemToDrumClientRpc(netRef);
        }

        [ClientRpc]
        public void AddItemToDrumClientRpc(NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject obj)) { return; }
            itemsInDrum.Add(obj);
        }

        [ServerRpc(RequireOwnership = false)]
        public void WashScrapServerRpc()
        {
            if (!IsServer) { return; }
            WashScrapClientRpc();
        }

        [ClientRpc]
        public void WashScrapClientRpc()
        {
            foreach (var item in itemsInDrum)
            {
                item.grabbable = false;
            }
            OpenDoor(false);
            washTimer = washTime;
        }
    }
}