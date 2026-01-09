/*
using BepInEx.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using UnityEngine.Video;
using static SCP2006.Plugin;

namespace SCP2006
{
    internal class TVSetUnlockable : NetworkBehaviour
    {
        public static TVSetUnlockable? Instance { get; private set; }

#pragma warning disable CS8618
        public VideoPlayer tvPlayer;
        public GameObject tvPlayerCanvas;
        public AudioSource tvAudio;
        public Light tvLight;
        public PlaceableShipObject placeableShipObject;
        public Transform sitPositonSCP2006;
        public InteractTrigger tapeInsertTrigger;

        public Transform insertTapePosition;
        public Transform placeTapePosition;
        public Collider tapeInsertCollider;
        public AudioClip tapeInsertSFX;
        public AudioClip tapeEjectSFX;
#pragma warning restore CS8618

        public static UnityEvent onTapeEjected = new();

        public VHSTapeBehavior? tapeInVHS;
        public bool inUse
        {
            get => placeableShipObject.inUse;
            set => placeableShipObject.inUse = value;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (Instance != null && Instance != this)
            {
                if (!IsServer) { return; }
                NetworkObject.Despawn(true);
                return;
            }
            Instance = this;
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
            tapeInsertCollider.enabled = localPlayer.currentlyHeldObjectServer is VHSTapeBehavior || tapeInVHS;
            tapeInsertTrigger.hoverTip = tapeInVHS ? "Eject Tape: [E]" : "Insert Tape: [E]";
        }

        public override void OnDestroy()
        {
            if (tapeInVHS != null)
            {
                inUse = false;

                onTapeEjected.Invoke();
                tapeInVHS.grabbable = true;
                tapeInVHS.grabbableToEnemies = true;
                tapeInVHS.parentObject = null;
                tapeInVHS.startFallingPosition = tapeInVHS.transform.parent.InverseTransformPoint(tapeInVHS.transform.position);
                tapeInVHS.targetFloorPosition = tapeInVHS.transform.parent.InverseTransformPoint(tapeInVHS.GetItemFloorPosition());
                tapeInVHS.fallTime = 0f;
            }
            base.OnDestroy();
        }

        public void OnVHSTrigger() // InteractTrigger
        {
            if (tapeInVHS)
            {
                EjectVHSServerRpc();
            }
            else if (localPlayer.currentlyHeldObjectServer is VHSTapeBehavior)
            {
                GrabbableObject tape = localPlayer.currentlyHeldObjectServer;
                localPlayer.DiscardHeldObject(placeObject: true, parentObjectTo: NetworkObject, placePosition: insertTapePosition.position, matchRotationOfParent: true);
                InsertVHSServerRpc(tape.NetworkObject);
            }
        }

        public void InsertVHS(VHSTapeBehavior tape)
        {
            tape.transform.position = insertTapePosition.position;
            tape.transform.rotation = insertTapePosition.rotation;
            tape.animator.SetTrigger("insert");
            tapeInVHS = tape;
            tvAudio.PlayOneShot(tapeInsertSFX, 1f);

            tapeInVHS.grabbable = false;
            tapeInVHS.grabbableToEnemies = false;

            tvPlayerCanvas.SetActive(true);
            tvPlayer.Play();
            tvLight.enabled = true;
        }

        public void EjectVHS()
        {
            if (tapeInVHS == null) { logger.LogError("tapeInVHS is null"); return; }
            tapeInVHS.transform.position = placeTapePosition.position;
            tapeInVHS.parentObject = placeTapePosition;
            tapeInVHS.transform.SetParent(StartOfRound.Instance.propsContainer, worldPositionStays: true);

            tapeInVHS.grabbable = true;
            tapeInVHS.grabbableToEnemies = true;

            tapeInVHS = null;
            tvAudio.PlayOneShot(tapeEjectSFX, 1f);

            tvPlayer.Stop();
            tvPlayerCanvas.SetActive(false);
            tvLight.enabled = false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void EjectVHSServerRpc()
        {
            if (!IsServer) { return; }
            if (tapeInVHS == null) { return; }
            onTapeEjected.Invoke();
            EjectVHSClientRpc();
        }

        [ClientRpc]
        public void EjectVHSClientRpc()
        {
            EjectVHS();
        }

        [ServerRpc(RequireOwnership = false)]
        public void InsertVHSServerRpc(NetworkObjectReference tapeNetRef)
        {
            if (!IsServer) { return; }
            InsertVHSClientRpc(tapeNetRef);
        }

        [ClientRpc]
        public void InsertVHSClientRpc(NetworkObjectReference tapeNetRef)
        {
            if (!tapeNetRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out VHSTapeBehavior tape)) { return; }
            InsertVHS(tape);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetTVSetInUseServerRpc(bool value)
        {
            if (!IsServer) { return; }
            SetTVSetInUseClientRpc(value);
        }

        [ClientRpc]
        public void SetTVSetInUseClientRpc(bool value)
        {
            inUse = value;
        }
    }
}
*/