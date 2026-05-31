using Dawn.Utils;
using Dusk;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LethalWashing.Plugin;

namespace LethalWashing
{
    internal class CoinBehavior : PhysicsProp
    {
        public AudioSource audioSource = null!;
        public Animator animator = null!;
        public List<AudioClip> coinDropSFXs = [];

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }
            FlipCoinServerRpc();
        }

        public override void OnHitGround()
        {
            base.OnHitGround();
            RoundManager.PlayRandomClip(audioSource, coinDropSFXs.ToArray());
        }

        [ServerRpc(RequireOwnership = false)]
        public void FlipCoinServerRpc()
        {
            if (!IsServer) { return; }
            FlipCoinClientRpc();
        }

        [ClientRpc]
        public void FlipCoinClientRpc()
        {
            audioSource.Play();
            animator.SetTrigger("flip");
        }

        [ClientRpc]
        public void SetScrapValueClientRpc(int setValueTo)
        {
            logger.LogDebug("SetScrapValueClientRpc");
            SetScrapValue(setValueTo);
        }
    }
}